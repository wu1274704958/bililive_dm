using BililiveDebugPlugin.InteractionGame.Data;
using Interaction;
using InteractionGame;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Documents;
using System.Windows.Forms;
using Utils;

namespace BililiveDebugPlugin.InteractionGame
{
    public enum EAoe4State  { 
        Default = 0,
        ExecExtern = 1,
        VillagerState = 2,
        SquadGroupCount = 3
    }

    public struct Aoe4StateData
    {
        public int R;public int G;public int B;

        public override string ToString()
        {
            return $"{R},{G},{B}";
        }
    }
    
    class Aoe4GameState : IGameStateObserver<EAoe4State, Aoe4StateData>
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
        private int _squadCountNotifyFailedTimes = 0; 
        private double _screenScalingFactorX => Screen.PrimaryScreen.Bounds.Width == 1920 ? 1.0 : 1.5;// (float)GetDeviceCaps(GetDC(IntPtr.Zero), DESKTOPHORZRES) / (float)GetDeviceCaps(GetDC(IntPtr.Zero), HORZRES);
        private double _screenScalingFactorY => Screen.PrimaryScreen.Bounds.Width == 1920 ? 1.0 : 1.5;// (float)GetDeviceCaps(GetDC(IntPtr.Zero), DESKTOPVERTRES) / (float)GetDeviceCaps(GetDC(IntPtr.Zero), VERTRES);

        [DllImport("gdi32.dll", EntryPoint = "GetDeviceCaps", SetLastError = true)]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        private IntPtr Aoe4Hwnd = IntPtr.Zero;

        public Aoe4StateData CheckState(EAoe4State state)
        {
            int x = ((int)state * (int)(12 * _screenScalingFactorX)) + (int)(3 * _screenScalingFactorX);
            int y = (int)(2 * _screenScalingFactorY);
            Point p = new Point(x,y);//取置顶点坐标 
            IntPtr hdc = GetDC(Aoe4Hwnd);
            uint pixel = GetPixel(hdc, x, y);
            ReleaseDC(Aoe4Hwnd, hdc);
            return new Aoe4StateData() {
                R = (int)(pixel & 0x000000FF), 
                G = (int)(pixel & 0x0000FF00) >> 8,
                B = (int)(pixel & 0x00FF0000) >> 16 };
        }

        public Aoe4StateData CheckState(EAoe4State state,IntPtr hwnd)
        {
            int x = ((int)state * 2) + 2;
            int y = 2;
            Point p = new Point(x, y);//取置顶点坐标 
            IntPtr hdc = GetDC(hwnd);
            uint pixel = GetPixel(hdc, x, y);
            ReleaseDC(hwnd, hdc);
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
            return;
            var ls = WindowEnumerator.FindAll((w) => w.Title.Contains(Aoe4DataConfig.Aoe4WinTitle));
            if (ls.Count > 0) Aoe4Hwnd = ls[0].Hwnd;
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

        public void Stop()
        {

        }

        public void CheckNewSquadCount(int g,object who = null,Action<int,int> on = null)
        {
            if(_squadCountCheckQueue.Count >= 3)
            {
                Locator.Instance.Get<IContext>().Log($"WARN!!! _squadCountCheckQueue has {_squadCountCheckQueue.Count}");
            }
            if(_squadCountChecking > 0)
            {
                _squadCountCheckQueue.Enqueue((g, who, on));
                return;
            }
            var nid = _SquadCheckId;
            if(_ckSquadCountDict.TryGetValue(g,out var v))
            {
                if(who != null)
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
            ExecGetSquadCount(g, nid);
            Interlocked.Exchange(ref _squadCountChecking, g + 1);
        }

        private void ExecGetSquadCount(int g,int nid)
        {
            Locator.Instance.Get<DebugPlugin>().messageDispatcher.GetBridge().AppendExecCode($@"NSC_UpdateToColor(PLAYERS[{g + 1}],{nid})");
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
            Interlocked.Exchange(ref _squadCountNotifyFailedTimes, 0);
            while (!_squadCountCheckQueue.IsEmpty)
                _squadCountCheckQueue.TryDequeue(out _);
        }

        public void OnTick()
        {
            var c = CheckState(EAoe4State.SquadGroupCount);
            var g = (c.R & 0x7) - 1;
            if (g < Aoe4DataConfig.GroupCount && g >= 0) 
            //if (c.r != LastGroup)
            {

                if (!CheckIsLockedSquadCountAndMoveNext(g))
                {
                    var nid = c.R >> 3;
                    var old = 0;
                    var count = ParseInt(c.G, c.B);
                    if (nid != _lastSquadCountNid)
                    {
                        Interlocked.Exchange(ref _squadCountChecking, 0);
                        Interlocked.Exchange(ref _lastSquadCountNid, nid);
                        Interlocked.Exchange(ref _squadCountNotifyFailedTimes, 0);
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
                        Interlocked.Exchange(ref _squadCountNotifyFailedTimes, _squadCountNotifyFailedTimes + 1);
                        if (_squadCountNotifyFailedTimes >= 16)
                        {
                            Locator.Instance.Get<IContext>().Log($"Squad Count failed g={_squadCountChecking - 1} need={_SquadCheckId} queue={_squadCountCheckQueue.Count} nid={nid} last={_lastSquadCountNid}");
                            Interlocked.Exchange(ref _squadCountNotifyFailedTimes,0);
                            ExecGetSquadCount(_squadCountChecking - 1, _SquadCheckId);
                        }
                    }
                }
                else
                {
                    Locator.Instance.Get<IContext>().Log($"Squad Count Locked {g}");
                }
            }
            
            if(_squadCountChecking == 0 && _squadCountCheckQueue.TryDequeue(out var msg))
            {
                CheckNewSquadCount(msg.Item1,msg.Item2,msg.Item3);
            }
            if ((DateTime.Now - _lastUpdate).TotalMilliseconds >= 1000)
            {
                _lastUpdate = DateTime.Now;
                if(!HasCheckSquadCountTask(_lastCheckGroup))
                    CheckNewSquadCount(_lastCheckGroup, this, null);
                Interlocked.Exchange(ref _lastCheckGroup, _lastCheckGroup + 1);
                if(_lastCheckGroup >= Aoe4DataConfig.GroupCount)
                    Interlocked.Exchange(ref _lastCheckGroup, 0);
            }
        }

        private bool NotifyCheckSquadCount(int g, int nid, int count)
        {
            if (_ckSquadCountDict.TryGetValue(g, out var v))
            {
                while (v.TryTake(out var val))
                {
                    val.Item2?.Invoke(count,g);
                }
            }
            return true;
        }

        private int ParseInt(int h, int l)
        {
            return h << 8 | l;
        }

        public Aoe4StateData GetData(int x, int y, IntPtr hwnd)
        {
            Point p = new Point(x, y);//取置顶点坐标 
            IntPtr hdc = GetDC(hwnd);
            uint pixel = GetPixel(hdc, x, y);
            ReleaseDC(hwnd, hdc);
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
            if(lockTime > 0)
                LockSquadCount(group,lockTime);
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
    }
}
