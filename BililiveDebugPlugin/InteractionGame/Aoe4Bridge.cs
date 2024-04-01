using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using BililiveDebugPlugin.InteractionGame;
using BililiveDebugPlugin.InteractionGame.Data;
using InteractionGame;
using System.Collections.Concurrent;
using Utils;
using System.Text.RegularExpressions;
using conf.Squad;

namespace Interaction
{
    public struct DyMsg
    {
        public int Player;
        public int Msg;
    }

    public interface IAoe4Bridge<IT>
        where IT : class,IContext
    {
        void SendMsg(DyMsg msg);
        void Stop();
        void Init(IT it,ILocalMsgDispatcher<IT> dispatcher);
        void OnTick(float delta);
        void OnClear();
        WindowInfo GetWindowInfo();
        void AppendExecCode(string code);
        void ExecSetCustomTarget(int self, int target);
        void ExecSpawnSquad(int self, string squadId,int num, long uid,int attackTy = 0, int op1 = 1);
        void ExecSpawnSquadWithTarget(int self, string squadId,int target,int num, long uid,int attackTy = 0, int op1 = 1);
        int ExecSpawnGroup(int self, List<(string, int)> group, long uid,double multiple = 1, int attackTy = 0, int op1 = 1);
        int ExecSpawnGroupWithTarget(int self, int target, List<(string, int)> group, long uid,double multiple = 1, int attackTy = 0, int op1 = 1);
        void ExecPrintMsg(string msg);
        void ExecSpawnVillagers(int self, int vid, int num);
        void ExecTryRemoveVillagersCountNotify(int vid,int next);
        void ExecAllSquadMove(int self, long uid);
        void ExecAllSquadMoveWithTarget(int self, int target, long uid,int attackTy = 0);
        void ExecCheckVillagerCount(int vid);
        void TryStartGame();
        void ClickLeftMouse(int x, int y);
        void FlushAppend();
        void SendKeyEvent(int key);
        bool NeedFlush();
        void flush();
        void FroceOverrideCurrentMsg(string msg);

    }
    public class DefAoe4BridgeUtil
    {
        [DllImport("user32.dll", EntryPoint = "SendMessageA")]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int SetCursorPos(int x, int y);
    }
    
    public class DefAoe4Bridge<IT> : IAoe4Bridge<IT>
        where IT:class,IContext
    {
        private WindowInfo _windowInfo = null;
        private int _ScreenWidth = 0;
        private int _ScreenHeight = 0;
        private IT _context;
        private Int32 GameNextIdx = -1;
        private Int32 CurrentWriteIdx = -1;
        private Int32 ExpectNextIdx = 0;
        private Dictionary<int,FileInfo> SavedFileDict = new Dictionary<int, FileInfo>();
        private static DirectoryInfo LuaDir = new DirectoryInfo("E:\\AOEs");
        private List<int> m_TmpList = new List<int>();
        public int ScreenWidth =>  _ScreenWidth == 0 ? _ScreenWidth = Screen.PrimaryScreen.Bounds.Width : _ScreenWidth;
        public int ScreenHeight => _ScreenHeight == 0 ? _ScreenHeight = Screen.PrimaryScreen.Bounds.Height : _ScreenHeight;   
        private const int MAX_ExecutedIdx = 255;
        private Utils.ObjectPool<StringBuilder> SbPool = new Utils.ObjectPool<StringBuilder>(()=> new StringBuilder(),(sb) => sb?.Clear());
        private ConcurrentQueue<StringBuilder> MsgQueue = new ConcurrentQueue<StringBuilder>();
        private StringBuilder m_ExecCode = null;
        private Object m_ExecCodeLock = new object();
        private static readonly int MsgMaxLength = 500;
        private static readonly int ButtonWidth = 30;
        private static readonly int ClickOffset = 10;
        private static readonly int ReverseMin = 100_0000;
        private ILocalMsgDispatcher<IT> m_MsgDispatcher;
        private static readonly int NextAdded = 1;
        private Utils.ObjectPool<StringBuilder> sbPool = new Utils.ObjectPool<StringBuilder>(()=>new StringBuilder(),(a)=>a.Clear());

        public void SendMsg(DyMsg msg)
        {
            if(_windowInfo == null)
                _windowInfo = FindWindow();
            if (_windowInfo != null)
            {
                SendMsgReal(msg);
            }
        }

        public void Stop()
        {
            Interlocked.Exchange(ref GameNextIdx, -1); 
            Interlocked.Exchange(ref CurrentWriteIdx, -1);
            Interlocked.Exchange(ref ExpectNextIdx, 0);
            _context = null;
            m_MsgDispatcher = null;
            m_TmpList.Clear();
            SavedFileDict.Clear();
        }

        public void Init(IT it,ILocalMsgDispatcher<IT> dispatcher)
        {
            Interlocked.Exchange(ref GameNextIdx, -1);
            Interlocked.Exchange(ref CurrentWriteIdx, -1);
            Interlocked.Exchange(ref ExpectNextIdx, 0);
            _context = it;
            m_MsgDispatcher = dispatcher;

            DelAllLuaFiles();
            _windowInfo = FindWindow();
        }

        private void DelAllLuaFiles()
        {
            var fs = LuaDir.GetFiles();
            var reg = new Regex("run([0-9]+).lua");
            foreach (var f in fs)
            {
                var match = reg.Match(f.Name);
                if(match.Success && int.TryParse(match.Groups[1].ToString(), out var idx) && idx <= MAX_ExecutedIdx && idx >= 0)
                {
                    f.Delete();
                }
            }
        }

        public void OnTick(float delta)
        {
            if (_context != null && _windowInfo != null)
            {
                var state = _context.CheckState(EAoe4State.ExecExtern);
                if(!(Math.Abs(state.R - ExpectNextIdx) < NextAdded))
                {
                    //_context.Log($"Game Exec Id {state.r} != ExpectNextIdx {ExpectNextIdx}");
                }
                else
                {
                    Interlocked.Exchange(ref GameNextIdx, state.R);
                }
                CheckAndRemoveExecutedFile();
                if(NeedFlush())
                {
                    flush();
                }
            }
        }

        public void AppendExecCode(string code)
        {
            lock (m_ExecCodeLock)
            {
                if (m_ExecCode != null)
                {
                    m_ExecCode.AppendLine(code);
                    if (m_ExecCode.Length >= MsgMaxLength)
                    {
                        MsgQueue.Enqueue(m_ExecCode);
                        m_ExecCode = null;
                    }
                }
                else
                {
                    m_ExecCode = sbPool.Get();
                    m_ExecCode.AppendLine(code);
                }
            }
        }

        public void ExecSetCustomTarget(int self, int target)
        {
            AppendExecCode($"PLAYERS[{self}].custom_target = {target};");
        }
        public void ExecSpawnSquad(int self, string squadId, int num,long uid, int attackTy = 0,int op1 = 1)
        {
            AppendExecCode($"SpawnAndAttackTargetEx({self},'{squadId}',{num},{uid},{attackTy},{{op1 = {op1}}});");
            //Locator.Instance.Get<Aoe4GameState>().OnSpawnSquad(self - 1, num);
        }
        public void ExecSpawnSquadWithTarget(int self, string squadId, int target, int num,long uid, int attackTy = 0, int op1 = 1)
        {
            AppendExecCode($"SpawnAndAttackTargetEx2({self},'{squadId}',{target},{num},{uid},{attackTy},{{op1 = {op1}}});");
            //Locator.Instance.Get<Aoe4GameState>().OnSpawnSquad(self - 1, num);
        }
        public int ExecSpawnGroup(int self, List<(string, int)> group, long uid, double multiple = 1, int attackTy = 0, int op1 = 1)
        {
            if (group.Count == 0) return 0;
            var groupStr = ToSpawnSquadTable(group,multiple);
            if (groupStr.Item1.Length <= 2) return 0;
            AppendExecCode($"SpawnGroupAndAttackTargetEx({self},{groupStr.Item1},{uid},{attackTy},{{op1 = {op1}}});");
            return groupStr.Item2;
            //Locator.Instance.Get<Aoe4GameState>().OnSpawnSquad(self - 1, groupStr.Item2);
        }
        public int ExecSpawnGroupWithTarget(int self, int target, List<(string, int)> group, long uid, double multiple = 1, int attackTy = 0, int op1 = 1)
        {
            if (group.Count == 0) return 0;
            var groupStr = ToSpawnSquadTable(group,multiple);
            if (groupStr.Item1.Length <= 2) return 0;
            AppendExecCode($"SpawnGroupAndAttackTargetEx2({self},{target},{groupStr.Item1},{uid},{attackTy},{{op1 = {op1}}});");
            return groupStr.Item2;
            //Locator.Instance.Get<Aoe4GameState>().OnSpawnSquad(self - 1, groupStr.Item2);
        }

        private (string,int) ToSpawnSquadTable(List<(string, int)> group,double multiple = 1)
        {
            var sb = sbPool.Get();
            //{{sbp = SBP.GAIA.GAIA_HERDABLE_SHEEP, numSquads = 2} ,{sbp = SBP.GAIA.GAIA_HUNTABLE_WOLF , numSquads = 3}}
            sb.Append('{');
            int num = 0;
            foreach (var it in group)
            {
                //var sd = Aoe4DataConfig.GetSquadPure(it.Item1);
                //if (sd.SquadType_e == ESquadType.Villager || sd.SquadType_e == ESquadType.SiegeAttacker) continue;
                int c = (int)Math.Round(it.Item2 * multiple);
                if (c <= 0) 
                    continue;
                sb.Append($"{{sbp=BP_GetSquadBlueprint('{it.Item1}'),numSquads={c}}},");
                num += c;
            }
            sb.Append('}');
            var s = sb.ToString();
            sbPool.Return(sb);
            return (s,num);
        }

        public void ExecAllSquadMove(int self,long uid)
        {
            AppendExecCode($"AllAttackTargetEx({self},{uid});");
        }
        public void ExecAllSquadMoveWithTarget(int self, int target, long uid, int attackTy = 0)
        {
            AppendExecCode($"AllAttackTargetEx2({self},{target},{uid},{attackTy});");
        }


        public bool NeedFlush()
        {
            //int overloadVal = 0;
            //if ((overloadVal = _context.IsOverload()) != 0)
            //{
            //    _context.Log($"Game overloaded {overloadVal}");
            //    return false;
            //}

            lock (m_ExecCodeLock)
            {
                if (MsgQueue.TryPeek(out var msg) || m_ExecCode != null)
                {
                    var can = IsLargeLooped(GameNextIdx, CurrentWriteIdx, 20);
                    if (can && MsgQueue.Count == 0)
                    {
                        MsgQueue.Enqueue(m_ExecCode);
                        m_ExecCode = null;
                    }
                    return can;
                }
            }
            return false;
        }

        public void flush()
        {
            bool needClickMsg = false;
            if (MsgQueue.TryDequeue(out var msg))
            {
                FileInfo fi = null;
                StreamWriter stream = null;
                try
                {
                    fi = new FileInfo($"{LuaDir.FullName}\\run{GameNextIdx}.lua");
                    stream = fi.CreateText();
                    Interlocked.Exchange(ref ExpectNextIdx, GetNextIdx(GameNextIdx));
                    stream.Write(msg);
                    stream.WriteLine($"_mod.ExecIdx = {ExpectNextIdx};");
                    stream.Flush();
                    SbPool.Return(msg);
                    Interlocked.Exchange(ref CurrentWriteIdx, GameNextIdx);
                    needClickMsg = true;
                }
                finally
                {
                    if(fi != null)
                        SavedFileDict.Add(GameNextIdx, fi);
                    stream?.Close();
                }
            };
            if (needClickMsg)
            {
                //_context.AppendMsg(new DyMsg(){Player = 1, Msg = ReverseMin},0.1f);
            }
        }

        private int GetNextIdx(int i)
        {
            var r = i + NextAdded;
            if (r > MAX_ExecutedIdx)
                r = 0;
            return r;
        }

        private void CheckAndRemoveExecutedFile()
        {
            foreach (var f in SavedFileDict)
            {
                if (IsLessLooped(f.Key, GameNextIdx,20))
                {
                    f.Value.Delete();
                    m_TmpList.Add(f.Key);
                }
            }

            if (m_TmpList.Count > 0)
            {
                foreach (var k in m_TmpList)
                {
                    SavedFileDict.Remove(k);
                }

                m_TmpList.Clear();
            }
        }

        private bool IsLessLooped(int a, int b, int range)
        {
            if (a < b)
                return true;
            else if(a > b && b < range && a > MAX_ExecutedIdx - range)
            {
                return true;
            }
            return false;
        }
        private bool IsLargeLooped(int a, int b, int range)
        {
            if (a > b)
                return true;
            else if (a < b && a < range && b > MAX_ExecutedIdx - range)
            {
                return true;
            }
            return false;
        }
        private int SubLooped(int a,int b,int range)
        {
            if (a > b)
                return a - b;
            else if (a < b && a < range && b > MAX_ExecutedIdx - range)
            {
                return (a - 0) + (MAX_ExecutedIdx - b) + 1;
            }
            return a - b;
        }

        private void SendMsgReal(DyMsg msg)
        {
            bool isLeft = msg.Msg > ReverseMin;
            int x = isLeft ? ((msg.Msg - ReverseMin) * ButtonWidth) + ClickOffset : 
                            ScreenWidth - (msg.Msg * ButtonWidth) - ClickOffset;
            int y = (msg.Player * ButtonWidth) + ClickOffset;
            ClickLeftMouse(x, y);
        }

        public void ClickLeftMouse(int x,int y)
        {
            DefAoe4BridgeUtil.SendMessage(_windowInfo.Hwnd, 0x200, IntPtr.Zero, new IntPtr(x + (y << 16)));
            Thread.Sleep(30);
            DefAoe4BridgeUtil.SendMessage(_windowInfo.Hwnd, 0x201, IntPtr.Zero, new IntPtr(x + (y << 16)));
            Thread.Sleep(30);
            DefAoe4BridgeUtil.SendMessage(_windowInfo.Hwnd, 0x202, IntPtr.Zero, new IntPtr(x + (y << 16)));
        }

        private WindowInfo FindWindow()
        {
            var ls = WindowEnumerator.FindAll((w) => w.Title.Contains(Aoe4DataConfig.Aoe4WinTitle));
            if(ls.Count > 0) return ls[0];
            return null;
        }

        public void ExecPrintMsg(string msg)
        {
            AppendExecCode($"UI_CreateEventCue( \"{msg}\", \"\",  \"\", \"\", \"\", ECV_Queue, 10);");
        }

        public void ExecSpawnVillagers(int self, int vid, int num)
        {
            AppendExecCode($"SpawnAndGatherGold(PLAYERS[{self}],{vid},PLAYERS[{self}],{num});");
        }

        public void ExecTryRemoveVillagersCountNotify(int vid, int next)
        {
            AppendExecCode($"TryRmVillagerCountNotify(_mod,{vid},{next});");
        }

        public void ExecCheckVillagerCount(int vid)
        {
            AppendExecCode($"CheckVillagerCount(_mod,{vid});");
        }


        public void OnClear()
        {
            Interlocked.Exchange(ref GameNextIdx, -1);
            Interlocked.Exchange(ref CurrentWriteIdx, -1);
            Interlocked.Exchange(ref ExpectNextIdx, 0);
            SavedFileDict.Clear();
            ClearMsgQueue();
            m_TmpList.Clear();
            lock (m_ExecCodeLock)
            {
                if (m_ExecCode != null)
                {
                    SbPool.Return(m_ExecCode);
                    m_ExecCode = null;
                }
            }
            DelAllLuaFiles();
        }

        public WindowInfo GetWindowInfo()
        {
            return _windowInfo;
        }

        public void TryStartGame()
        {
            if (_windowInfo == null) return;
            DefAoe4BridgeUtil.SendMessage(_windowInfo.Hwnd, 0x0100, new IntPtr(97), IntPtr.Zero);
            Thread.Sleep(10);
            DefAoe4BridgeUtil.SendMessage(_windowInfo.Hwnd, 0x0101, new IntPtr(97), IntPtr.Zero);
        }

        public void FlushAppend()
        {
            lock (m_ExecCodeLock)
            {
                if (m_ExecCode != null)
                {
                    MsgQueue.Enqueue(m_ExecCode);
                    m_ExecCode = null;
                }
            }
        }

        public void SendKeyEvent(int key)
        {
            if (_windowInfo == null) return;
            DefAoe4BridgeUtil.SendMessage(_windowInfo.Hwnd, 0x0100, new IntPtr(key), IntPtr.Zero);
            Thread.Sleep(10);
            DefAoe4BridgeUtil.SendMessage(_windowInfo.Hwnd, 0x0101, new IntPtr(key), IntPtr.Zero);
        }

        public void FroceOverrideCurrentMsg(string msg)
        {
            ClearMsgQueue();
            var sb = sbPool.Get();
            sb.Clear();
            sb.Append(msg);
            MsgQueue.Enqueue(sb);
            while (NeedFlush())
                flush();
        }

        private void ClearMsgQueue()
        {
            while (!MsgQueue.IsEmpty)
            {
                if (MsgQueue.TryDequeue(out var sb))
                    sbPool.Return(sb);
            }
        }
    }
}


