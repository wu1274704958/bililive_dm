using BililiveDebugPlugin.InteractionGame.Data;
using Interaction;
using InteractionGame;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using Utils;

namespace BililiveDebugPlugin.InteractionGame
{
    public enum EAoe4State  { 
        Default = 0,
        ExecExtern = 1,
        VillagerState = 2,
        SquadGroupCount = 3,
        DefineKeepHp = 4
    }

    public struct Aoe4StateData
    {
        public int R;public int G;public int B;

        public override string ToString()
        {
            return $"{R},{G},{B}";
        }

        public bool Equals(Aoe4StateData other)
        {
            return R == other.R && G == other.G && B == other.B;
        }

        public override bool Equals(object obj)
        {
            return obj is Aoe4StateData other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = R;
                hashCode = (hashCode * 397) ^ G;
                hashCode = (hashCode * 397) ^ B;
                return hashCode;
            }
        }
    }
    
    public class Aoe4GameState : IGameStateObserver<EAoe4State, Aoe4StateData>
    {

        const int HORZRES = 8;
        const int VERTRES = 10;
        const int LOGPIXELSX = 88;
        const int LOGPIXELSY = 90;
        const int DESKTOPVERTRES = 117;
        const int DESKTOPHORZRES = 118;
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("Aoe4GS.dll")]
        static extern uint AGS_GetColor(int nXPos, int nYPos);

        [DllImport("Aoe4GS.dll")]
        static extern int AGS_Init(IntPtr hwnd);

        [DllImport("Aoe4GS.dll")]
        static extern void AGS_Stop();

        struct LPRECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        };

        [DllImport("user32.dll")]
        static extern int GetWindowRect(IntPtr hWnd, ref LPRECT lpRect);

        [DllImport("user32.dll")]
        static extern int GetClientRect(IntPtr hWnd, ref LPRECT lpRect);


        private ConcurrentDictionary<int,int> _squadCountByGroup = new ConcurrentDictionary<int, int>();

        private ConcurrentDictionary<int, ISquadCountObserver> _observers =
            new ConcurrentDictionary<int, ISquadCountObserver>();
        private int[] _lockGroupSquadCount = new int[8];
        private int _SquadCheckId = 0;
        private ConcurrentDictionary<int, ConcurrentBag<(int, Action<int,int>)>> _ckSquadCountDict = new ConcurrentDictionary<int, ConcurrentBag<(int, Action<int, int>)>>();
        private DateTime _lastUpdate = DateTime.Now;
        private int _squadCountChecking = 0;
        private ConcurrentQueue<(int,object,Action<int, int>)> _squadCountCheckQueue = new ConcurrentQueue<(int, object, Action<int, int>)> ();
        private int _lastSquadCountNid = -1;
        private int _lastCheckGroup = 0;
        private int[] _squadCountNotifyFailedTimes = new int[8];
        private int Aoe4Width = 1920;
        private int Aoe4Height = 1080;
        private double _screenScalingFactorX => 1;// (float)GetDeviceCaps(GetDC(IntPtr.Zero), DESKTOPHORZRES) / (float)GetDeviceCaps(GetDC(IntPtr.Zero), HORZRES);
        private double _screenScalingFactorY => 1;// (float)GetDeviceCaps(GetDC(IntPtr.Zero), DESKTOPVERTRES) / (float)GetDeviceCaps(GetDC(IntPtr.Zero), VERTRES);

        [DllImport("gdi32.dll", EntryPoint = "GetDeviceCaps", SetLastError = true)]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        private IntPtr Aoe4Hwnd = IntPtr.Zero;
        private LPRECT _Aoe4RECT;
        private LPRECT _Aoe4ClientRECT;
        private int[] _CkSquadCountExecCallbackLock = new int[8];
        private IContext _cxt;

        //public Aoe4StateData CheckState(EAoe4State state)
        //{
        //    int x = ((int)state * (int)(12 * _screenScalingFactorX)) + (int)(3 * _screenScalingFactorX);
        //    int y = (int)(2 * _screenScalingFactorY);
        //    System.Drawing.Point p = new System.Drawing.Point(x, y);//取置顶点坐标 
        //    IntPtr hdc = GetDC(IntPtr.Zero);
        //    uint pixel = GetPixel(hdc, x, y);
        //    ReleaseDC(IntPtr.Zero, hdc);
        //    return new Aoe4StateData()
        //    {
        //        R = (int)(pixel & 0x000000FF),
        //        G = (int)(pixel & 0x0000FF00) >> 8,
        //        B = (int)(pixel & 0x00FF0000) >> 16
        //    };
        //}
        public Aoe4StateData CheckState(EAoe4State state)
        {
            int x = ((int)state * (int)(12 * _screenScalingFactorX)) + (int)(6 * _screenScalingFactorX) + (Aoe4Width >= 2560 ? 0 : 1 );
            int y = (int)(6 * _screenScalingFactorY) + +(Aoe4Width >= 2560 ? 0 : 31);
            uint pixel = AGS_GetColor(x, y);
            return new Aoe4StateData()
            {
                B = (int)((pixel >> 24) & 255),
                G = (int)((pixel >> 16) & 255),
                R = (int)((pixel >> 8) & 255),
            };
        }

        public Aoe4StateData CheckState(EAoe4State state,IntPtr hwnd)
        {
            //int x = ((int)state * 2) + 2;
            //int y = 2;
            //Point p = new Point(x, y);//取置顶点坐标 
            //IntPtr hdc = GetDC(hwnd);
            uint pixel = 0;// GetPixel(hdc, x, y);
            //ReleaseDC(hwnd, hdc);
            return new Aoe4StateData()
            {
                R = (int)(pixel & 0x000000FF),
                G = (int)(pixel & 0x0000FF00) >> 8,
                B = (int)(pixel & 0x00FF0000) >> 16
            };
        }

        public void Init()
        {
            ResetLockGroupSquadCount();
            ResetSquadCountCheck();
            var ls = WindowEnumerator.FindAll((w) => w.Title.Contains(Aoe4DataConfig.Aoe4WinTitle));
            if (ls.Count > 0)
            {
                Aoe4Hwnd = ls[0].Hwnd;
                AGS_Init(Aoe4Hwnd);
                LPRECT pRECT = new LPRECT();
                GetWindowRect(Aoe4Hwnd, ref _Aoe4RECT);
                GetClientRect(Aoe4Hwnd, ref _Aoe4ClientRECT);
                Aoe4Width = _Aoe4ClientRECT.right;
                Aoe4Height = _Aoe4ClientRECT.bottom;
            }
            _cxt = Locator.Instance.Get<IContext>();
        }

        private void ResetSquadCountCheck()
        {
            Interlocked.Exchange(ref _SquadCheckId, 0);
            Interlocked.Exchange(ref _lastSquadCountNid, -1);
        }

        private void ResetLockGroupSquadCount()
        {
            for(int i = 0; i < _lockGroupSquadCount.Length; i++)
            {
                Interlocked.Exchange(ref _lockGroupSquadCount[i], 0);
                Interlocked.Exchange(ref _CkSquadCountExecCallbackLock[i], 0);
            }
        }

        private void LockSquadCount(int g,int v = 5)
        {
            if (g < 0 || g >= _lockGroupSquadCount.Length) return;
            Interlocked.Exchange(ref _lockGroupSquadCount[g], v);
        }
        private bool CheckIsLockedSquadCountAndMoveNext(int g)
        {
            if (g >= _lockGroupSquadCount.Length) return true;
            var v = _lockGroupSquadCount[g];
            if(v > 0)
            {
                Interlocked.Exchange(ref _lockGroupSquadCount[g], v - 1);
                return true;
            }
            return false;
        }
        private bool CheckIsLockedSquadCount(int g)
        {
            if (g >= _lockGroupSquadCount.Length) return true;
            var v = _lockGroupSquadCount[g];
            if (v > 0)
                return true;
            return false;
        }

        public void Stop()
        {
            AGS_Stop();
        }

        public void CheckNewSquadCount(int g,object who = null,Action<int,int> on = null)
        {
            if(!HasCheckSquadCountTask(g))
            {
                CheckNewSquadCountInter(g, who,on);
            }
            else
            {
                AddCheckSquadCountCallback(g, who,on);
            }
        }

        protected void CheckNewSquadCountInter(int g, object who = null, Action<int, int> on = null)
        {
            if (_squadCountCheckQueue.Count >= 3)
            {
                Locator.Instance.Get<IContext>().Log($"WARN!!! _squadCountCheckQueue has {_squadCountCheckQueue.Count}");
            }
            if (_squadCountChecking > 0)
            {
                _squadCountCheckQueue.Enqueue((g, who, on));
                return;
            }
            AddCheckSquadCountCallback(g, who, on);
            ExecGetSquadCount(g, _SquadCheckId);
            Interlocked.Exchange(ref _squadCountChecking, g + 1);
        }

        public bool AddCheckSquadCountCallback(int g,object who,Action<int,int> on)
        {
            while (_CkSquadCountExecCallbackLock[g] == 1) { }
            if (_CkSquadCountExecCallbackLock[g] == 2)
                return false;
            Interlocked.Exchange(ref _CkSquadCountExecCallbackLock[g],1);
            if (_ckSquadCountDict.TryGetValue(g, out var v))
            {
                if (who != null)
                    v.Add((who.GetHashCode(), on));
            }
            else
            {
                if (who != null)
                {
                    var list = new ConcurrentBag<(int, Action<int, int>)>();
                    list.Add((who.GetHashCode(), on));
                    _ckSquadCountDict.TryAdd(g, list);
                }
            }
            Interlocked.Exchange(ref _CkSquadCountExecCallbackLock[g], 0);
            return true;
        }

        private void ExecGetSquadCount(int g,int nid)
        {
            //Locator.Instance.Get<DebugPlugin>().messageDispatcher.GetBridge().AppendExecCode($@"NSC_UpdateToColor(PLAYERS[{g + 1}],{nid})");
            Locator.Instance.Get<DebugPlugin>().messageDispatcher.GetBridge().ClickLeftMouse(((int)EAoe4State.SquadGroupCount * (int)(12 * _screenScalingFactorX)) + (int)(6 * _screenScalingFactorX),
            (int)( 15 * _screenScalingFactorY));
        }
        public bool HasCheckSquadCountTask(int g,object who)
        {
            if (_ckSquadCountDict.TryGetValue(g, out var v))
            {
                foreach(var it in v)
                {
                    if(it.Item1 == who.GetHashCode())
                        return true;
                }
            }
            foreach (var c in _squadCountCheckQueue)
            {
                if (c.Item1 == g && c.Item2.GetHashCode() == who.GetHashCode())
                    return true;
            }
            return false;
        }
        public bool HasCheckSquadCountTask(object who)
        {
            foreach(var v in _ckSquadCountDict)
            {
                foreach (var it in v.Value)
                {
                    if(it.Item1 == who.GetHashCode()) return true;
                }
            }
            foreach(var c in _squadCountCheckQueue)
            {
                if(c.Item2.GetHashCode() == who.GetHashCode())
                    return true;
            }
            return false;
        }
        public bool HasCheckSquadCountTask(int g)
        {
            if (_ckSquadCountDict.TryGetValue(g, out var v) && !v.IsEmpty)
                return true;
            foreach(var c in _squadCountCheckQueue)
            {
                if(c.Item1 == g)
                    return true;
            }
            return false;
        }

        private int NextCkSquadCountNid(int g)
        {
            var nid = _SquadCheckId + 1;
            if (nid > 31)
                nid = 0;
            Interlocked.Exchange(ref _SquadCheckId, nid);
            return nid;
        }

        public void OnClear()
        {
            _squadCountByGroup.Clear();
            ResetLockGroupSquadCount();
            ResetSquadCountCheck();
            _ckSquadCountDict.Clear();
            _lastUpdate = DateTime.Now;
            _squadCountChecking = 0;
            for(int i = 0;i <  _squadCountCheckQueue.Count;i++)
                Interlocked.Exchange(ref _squadCountNotifyFailedTimes[i], 0);
            while (!_squadCountCheckQueue.IsEmpty)
                _squadCountCheckQueue.TryDequeue(out _);
        }

        public void OnTick()
        {
            if(_cxt.IsGameStart() != 1) 
                return;

            var c = CheckState(EAoe4State.SquadGroupCount);
            if(c.R == 255 && c.B == 0 && c.G ==0)
            {
                ExecGetSquadCount(_squadCountChecking - 1, _SquadCheckId);
                return;
            }    
            var g = (c.R & 0x7) - 1;
            if (g < Aoe4DataConfig.GroupCount && g >= 0) 
            //if (c.r != LastGroup)
            {

                if (!CheckIsLockedSquadCountAndMoveNext(g))
                {
                    var nid = c.R >> 3;
                    var old = 0;
                    var count = ParseInt(c.G, c.B);
                    if (CheckNewSquadCountVaild(g,count,nid))
                    {
                        Interlocked.Exchange(ref _squadCountChecking, 0);
                        Interlocked.Exchange(ref _lastSquadCountNid, nid);
                        Interlocked.Exchange(ref _squadCountNotifyFailedTimes[g], 0);
                        //Locator.Instance.Get<IContext>().Log($"Get squad count g = {g} nid = {nid} count = {count}");
                        if (!_squadCountByGroup.TryAdd(g, count))
                        {
                            old = _squadCountByGroup[g];
                            _squadCountByGroup[g] = count;
                        }
                        NotifySquadCountChanged(g, count, old);
                        NotifyCheckSquadCount(g, nid, count);
                        NextCkSquadCountNid(g);
                    }
                    else if(_squadCountChecking > 0)
                    {
                        Interlocked.Exchange(ref _squadCountNotifyFailedTimes[g], _squadCountNotifyFailedTimes[g] + 1);
                        if (_squadCountNotifyFailedTimes[g] % 4 == 0)
                        {
                            ExecGetSquadCount(_squadCountChecking - 1, _SquadCheckId);
                        }
                    }
                }
                //else
                //{
                //    Locator.Instance.Get<IContext>().Log($"Squad Count Locked {g}");
                //}
            }
            
            if(_squadCountChecking == 0 && _squadCountCheckQueue.TryDequeue(out var msg))
            {
                CheckNewSquadCountInter(msg.Item1,msg.Item2,msg.Item3);
            }
            if ((DateTime.Now - _lastUpdate).TotalMilliseconds >= 1000)
            {
                _lastUpdate = DateTime.Now;
                if(!HasCheckSquadCountTask(_lastCheckGroup))
                    CheckNewSquadCountInter(_lastCheckGroup, this, null);
                Interlocked.Exchange(ref _lastCheckGroup, _lastCheckGroup + 1);
                if(_lastCheckGroup >= Aoe4DataConfig.GroupCount)
                    Interlocked.Exchange(ref _lastCheckGroup, 0);
            }
        }

        private bool IsLargeLooped(int a, int b, int range)
        {
            if (a > b)
                return true;
            else if (a < b && a < range && b > 31 - range)
            {
                return true;
            }
            return false;
        }

        private bool CheckNewSquadCountVaild(int g, int count, int nid)
        {
            if(!IsLargeLooped(nid,_lastSquadCountNid,5))
            {
                if(nid != _lastSquadCountNid)
                    _cxt.Log($"CheckNewSquadCountVaild g={g} nid={nid} < last={_lastSquadCountNid}");
                return false;
            }
            var old = 0;
            if (!_squadCountByGroup.TryGetValue(g, out old))
                old = 0;
            if (count < old && old - count >= 120)
            {
                if (_squadCountNotifyFailedTimes[g] >= 18)
                {
                    _cxt.Log($"CheckNewSquadCountVaild 到失败次数上限放行 g={g} Big different old{old} new{count}");
                    Interlocked.Exchange(ref _squadCountNotifyFailedTimes[g], 0);
                    return true;
                }
                _cxt.Log($"CheckNewSquadCountVaild g={g} Big different old{old} new{count}");
                return false;
            }
            return true;
        }

        private bool NotifyCheckSquadCount(int g, int nid, int count)
        {
            while (_CkSquadCountExecCallbackLock[g] != 0) { }
            Interlocked.Exchange(ref _CkSquadCountExecCallbackLock[g], 2);
            if (_ckSquadCountDict.TryGetValue(g, out var v))
            {
                while (v.TryTake(out var val))
                {
                    val.Item2?.Invoke(count,g);
                }
            }
            Interlocked.Exchange(ref _CkSquadCountExecCallbackLock[g], 0);
            return true;
        }

        private int ParseInt(int h, int l)
        {
            return h << 8 | l;
        }

        public Aoe4StateData GetData(int x, int y, IntPtr hwnd)
        {
            //Point p = new Point(x, y);//取置顶点坐标 
            //IntPtr hdc = GetDC(hwnd);
            uint pixel = 0;
            //ReleaseDC(hwnd, hdc);
            return new Aoe4StateData()
            {
                R = (int)(pixel & 0x000000FF),
                G = (int)(pixel & 0x0000FF00) >> 8,
                B = (int)(pixel & 0x00FF0000) >> 16
            };
        }
        public bool OnSpawnSquad(int group, int count,int lockTime = 0)
        {
            var old = 0;
            var f = true;
            if (lockTime > 0)
                LockSquadCount(group, 20);// lockTime);
            if (!_squadCountByGroup.TryAdd(group, count))
            {
                old = _squadCountByGroup[group];
                _squadCountByGroup[group] = old + count;
            }
            NotifySquadCountChanged(group, _squadCountByGroup[group], old);
            return f;
        }

        public int GetSquadCount(int group)
        {
            if (_squadCountByGroup.TryGetValue(group, out var r))
                return r;
            return 0;
        }

        public void AddObserver(ISquadCountObserver observer)
        {   
            _observers.TryAdd(observer.GetHashCode(),observer);
        }
        
        public void RemoveObserver(ISquadCountObserver observer)
        {
            _observers.TryRemove(observer.GetHashCode(),out _);
        }

        private void NotifySquadCountChanged(int group, int count, int oldCount)
        {
            foreach(var o in _observers)
            {
                o.Value.SquadCountChanged(group,oldCount,count);
            }
        }

        public void CloseDisconnectPopup()
        {
            var c = CheckState(EAoe4State.VillagerState);
            if (c.R < 250 && c.R > 10 && c.G == 0 && c.B == 0)
            {
                int x = (int)((1550 / 2560.0) * Aoe4Width);
                int y = (int)((835 / 1440.0) * Aoe4Height);
                Locator.Instance.Get<DebugPlugin>().messageDispatcher.GetBridge().ClickLeftMouse(x,y);
            }
        }
    }
}
