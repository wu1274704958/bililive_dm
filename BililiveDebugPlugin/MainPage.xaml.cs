using InteractionGame;
using System;
using System.Windows;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace BililiveDebugPlugin
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Window
    {

        IContext m_Cxt = null;
        public MainPage(IContext context)
        {
            InitializeComponent();
            m_Cxt = context; 
        }

        private void Test_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var m = new BilibiliDM_PluginFramework.DanmakuModel();
                var ss = TestIn.Text.Split(' ');
                if (ss.Length == 1)
                {
                    m.UserID_long = 1;
                    m.CommentText = TestIn.Text;
                }
                else
                {
                    m.UserID_long = long.Parse(ss[0]);
                    m.CommentText = ss[1];
                }
                m.MsgType = BilibiliDM_PluginFramework.MsgTypeEnum.Comment;
                if (m.CommentText[0] == '给')
                {
                    m.GiftName = m.CommentText.Substring(1);
                    m.MsgType = BilibiliDM_PluginFramework.MsgTypeEnum.GiftSend;
                    m.GiftCount = 1;
                }
                if (m.CommentText[0] == '舰')
                {
                    m.GiftName = m.CommentText.Substring(1);
                    m.UserGuardLevel = m.GuardLevel = int.Parse(m.GiftName);
                    m.MsgType = BilibiliDM_PluginFramework.MsgTypeEnum.GuardBuy;
                }
                m.UserName = string.Format("name_{0}", m.UserID_long);
                
                var a = new BilibiliDM_PluginFramework.ReceivedDanmakuArgs() { Danmaku = m };
                m_Cxt.SendTestDanMu(this,a);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ex.ToString()));
            }
        }

        private void TestGetColor(object sender, RoutedEventArgs e)
        {
            var ss = TestIn.Text.Split(' ');
            if(ss.Length >= 2 && int.TryParse(ss[0],out var x) && int.TryParse(ss[1],out var y))
            {
                var d = (m_Cxt as BililiveDebugPlugin.DebugPlugin);
                var w = d.messageDispatcher.Aoe4Bridge.GetWindowInfo();
                var c = d?.GetGameState().GetData(x, y,w.Hwnd);
                Text.Content = c.ToString();
            }
        }

        private void CloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}