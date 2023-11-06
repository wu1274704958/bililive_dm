using System;
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
        public Aoe4StateData CheckState(EAoe4State state)
        {
            int x = ((int)state * 20) + 10;
            int y = 10;
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
            int x = ((int)state * 20) + 10;
            int y = 10;
            Point p = new Point(x, y);//取置顶点坐标 
            IntPtr hdc = GetDC(hwnd);
            uint pixel = GetPixel(hdc, x, y);
            ReleaseDC(IntPtr.Zero, hdc);
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
    }
}
