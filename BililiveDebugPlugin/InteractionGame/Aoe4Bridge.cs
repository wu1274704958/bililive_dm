using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using BililiveDebugPlugin.InteractionGame;
using InteractionGame;

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
        void OnTick();

        void AppendExecCode(string code);
        void ExecSetCustomTarget(int self, int target);
        void ExecSpawnSquad(int self, int squadId,int num);
        void ExecSpawnSquadWithTarget(int self, int squadId,int target,int num);
        void ExecPrintMsg(string msg);
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
        private IT _context;
        private Int32 GameNextIdx = -1;
        private Int32 CurrentWriteIdx = -1;
        private Int32 ExpectNextIdx = 0;
        private Dictionary<int,FileInfo> SavedFileDict = new Dictionary<int, FileInfo>();
        private static DirectoryInfo LuaDir = new DirectoryInfo("E:\\AOEs");
        private List<int> m_TmpList = new List<int>();
        public int ScreenWidth =>  _ScreenWidth == 0 ? _ScreenWidth = Screen.PrimaryScreen.Bounds.Width : _ScreenWidth;
        private const int MAX_ExecutedIdx = 255;
        private StringBuilder m_ExecCode = new StringBuilder();
        private static readonly int ButtonWidth = 30;
        private static readonly int ClickOffset = 10;
        private static readonly int ReverseMin = 100_0000;
        private ILocalMsgDispatcher<IT> m_MsgDispatcher;
        private static readonly int NextAdded = 1;

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
            foreach (var f in fs)
            {
                var ss = f.Name.Split('.');
                if(int.TryParse(ss[0], out var idx) && idx <= MAX_ExecutedIdx && idx >= 0)
                {
                    f.Delete();
                }
            }
        }

        public void OnTick()
        {
            if (_context != null && _windowInfo != null)
            {
                var state = _context.CheckState(EAoe4State.ExecExtern);
                if(!(Math.Abs(state.r - ExpectNextIdx) < NextAdded))
                {
                    _context.Log($"Game Exec Id {state.r} != ExpectNextIdx {ExpectNextIdx}");
                }
                else
                {
                    Interlocked.Exchange(ref GameNextIdx, state.r);
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
            lock (m_ExecCode)
            {
                m_ExecCode.AppendLine(code);
            }
        }

        public void ExecSetCustomTarget(int self, int target)
        {
            AppendExecCode($"PLAYERS[{self}].custom_target = {target};");
        }
        public void ExecSpawnSquad(int self, int squadId, int num)
        {
            AppendExecCode($"SpawnAndAttackTargetEx({self},{squadId},{num});");
        }
        public void ExecSpawnSquadWithTarget(int self, int squadId, int target, int num)
        {
            AppendExecCode($"SpawnAndAttackTargetEx2({self},{squadId},{target},{num});");
        }

        public bool NeedFlush()
        {
            lock (m_ExecCode)
            {
                return m_ExecCode.Length > 0 && IsLargeLooped(GameNextIdx, CurrentWriteIdx, 20);
            };
        }

        public void flush()
        {
            bool needClickMsg = false;
            lock (m_ExecCode)
            {
                if(m_ExecCode.Length == 0) return;
                FileInfo fi = null;
                StreamWriter stream = null;
                try
                {
                    fi = new FileInfo($"{LuaDir.FullName}\\{GameNextIdx}.lua");
                    stream = fi.CreateText();
                    Interlocked.Exchange(ref ExpectNextIdx, GetNextIdx(GameNextIdx));
                    stream.WriteLine($"_mod.ExecIdx = {ExpectNextIdx};");
                    stream.Write(m_ExecCode);
                    stream.Flush();
                    m_ExecCode.Clear();
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

        private void ClickLeftMouse(int x,int y)
        {
            DefAoe4BridgeUtil.SetCursorPos(x, y);
            Thread.Sleep(10);
            DefAoe4BridgeUtil.SendMessage(_windowInfo.Hwnd, 0x201, IntPtr.Zero, new IntPtr(x + (y << 16)));
            DefAoe4BridgeUtil.SendMessage(_windowInfo.Hwnd, 0x202, IntPtr.Zero, new IntPtr(x + (y << 16)));
        }

        private WindowInfo FindWindow()
        {
            var ls = WindowEnumerator.FindAll((w) => w.Title.Contains("Age of Empires IV"));
            if(ls.Count > 0) return ls[0];
            return null;
        }

        public void ExecPrintMsg(string msg)
        {
            AppendExecCode($"UI_CreateEventCueClickable(-1, 10, -1, 0, \"{msg}\", \"\", \"low_priority\", \"\", \"sfx_ui_event_queue_low_priority_play\", 255, 255, 255, 255, ECV_Queue, nothing);");
        }
    }
}


