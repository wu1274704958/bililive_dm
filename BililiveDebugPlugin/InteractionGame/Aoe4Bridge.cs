using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Interaction
{
    public struct DyMsg
    {
        public int Player;
        public int Msg;
    }
    public interface IAoe4Bridge
    {
        void SendMsg(DyMsg msg);
    }
    public class DefAoe4Bridge : IAoe4Bridge
    {
        private WindowInfo _windowInfo = null;
        private int _ScreenWidth = 0;

        public int ScreenWidth =>  _ScreenWidth == 0 ? _ScreenWidth = Screen.PrimaryScreen.Bounds.Width : _ScreenWidth;
        public void SendMsg(DyMsg msg)
        {
            if(_windowInfo == null)
                _windowInfo = FindWindow();
            if (_windowInfo != null)
            {
                SendMsgReal(msg);
            }
        }
        
        private void SendMsgReal(DyMsg msg)
        {
            int x = ScreenWidth - (msg.Msg * 30) - 10;
            int y = (msg.Player * 30) + 10;
            ClickLeftMouse(x, y);
        }

        private void ClickLeftMouse(int x,int y)
        {
            SetCursorPos(x, y);
            Thread.Sleep(10);
            SendMessage(_windowInfo.Hwnd, 0x201, IntPtr.Zero, new IntPtr(x + (y << 16)));
            SendMessage(_windowInfo.Hwnd, 0x202, IntPtr.Zero, new IntPtr(x + (y << 16)));
        }

        private WindowInfo FindWindow()
        {
            var ls = WindowEnumerator.FindAll((w) => w.Title.Contains("Age of Empires IV"));
            if(ls.Count > 0) return ls[0];
            return null;
        }
        
        [DllImport("user32.dll", EntryPoint = "SendMessageA")]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int SetCursorPos(int x, int y);


    }
}


