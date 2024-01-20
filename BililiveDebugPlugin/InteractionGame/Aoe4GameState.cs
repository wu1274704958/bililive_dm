using BililiveDebugPlugin.InteractionGame.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace BililiveDebugPlugin.InteractionGame
{
    public enum EAoe4State : int { 
        Default = 0,
        ExecExtern = 1,
        VillagerState = 2,
        SquadGroupCount = 3
    }

    public struct Aoe4StateData
    {
        public int r;public int g;public int b;

        public override string ToString()
        {
            return $"{r},{g},{b}";
        }
    }

    class Aoe4GameState : IGameStateObserver<EAoe4State, Aoe4StateData>
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);
        private ConcurrentDictionary<int,int> SquadCountByGroup = new ConcurrentDictionary<int, int>();
        private int LastGroup = -1;
        public Aoe4StateData CheckState(EAoe4State state)
        {
            int x = ((int)state * 16) + 6;
            int y = 4;
            Point p = new Point(x,y);//取置顶点坐标 
            IntPtr hdc = GetDC(IntPtr.Zero);
            uint pixel = GetPixel(hdc, x, y);
            ReleaseDC(IntPtr.Zero, hdc);
            return new Aoe4StateData() {
                r = (int)(pixel & 0x000000FF), 
                g = (int)(pixel & 0x0000FF00) >> 8,
                b = (int)(pixel & 0x00FF0000) >> 16 };
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
                r = (int)(pixel & 0x000000FF),
                g = (int)(pixel & 0x0000FF00) >> 8,
                b = (int)(pixel & 0x00FF0000) >> 16
            };
        }

        public void Init()
        {

        }

        public void Stop()
        {

        }

        public void OnClear()
        {
            SquadCountByGroup.Clear();
            LastGroup = -1;
        }

        public void OnTick()
        {
            var c = CheckState(EAoe4State.SquadGroupCount);
            if (c.r - 1 >= Aoe4DataConfig.GroupCount) return;
            if (c.r != LastGroup)
            {
                var count = ParseInt(c.g,c.b);
                if(!SquadCountByGroup.TryAdd(c.r - 1,count))
                    SquadCountByGroup[c.r - 1] = count;
                LastGroup = c.r;
            }
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
                r = (int)(pixel & 0x000000FF),
                g = (int)(pixel & 0x0000FF00) >> 8,
                b = (int)(pixel & 0x00FF0000) >> 16
            };
        }
        public bool OnSpawnSquad(int group, int count)
        {
            if (!SquadCountByGroup.TryAdd(group, count))
            {
                var curr = SquadCountByGroup[group];
                return SquadCountByGroup.TryUpdate(group, curr + count,curr );
            }
            return true;
        }

        public int GetSquadCount(int group)
        {
            if (SquadCountByGroup.TryGetValue(group, out var r))
                return r;
            return 0;
        }
    }
}
