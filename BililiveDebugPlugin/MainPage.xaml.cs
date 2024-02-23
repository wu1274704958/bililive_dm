﻿using InteractionGame;
using System;
using System.Windows;
using System.Windows.Input;
using BililiveDebugPlugin.InteractionGame.Data;
using System.Text.RegularExpressions;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace BililiveDebugPlugin
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Window
    {

        IContext m_Cxt = null;
        private bool m_DragDown = false;
        private Point m_DragDownPos = new Point(0,0);

        public MainPage(IContext context)
        {
            InitializeComponent();
            m_Cxt = context; 
            //让BackgroundRectangle可以点击移动整个窗口
            //BackgroundRectangle.AllowDrop = true;
            //BackgroundRectangle.MouseDown += BackgroundRectangle_MouseDown;
            //BackgroundRectangle.MouseUp += BackgroundRectangle_MouseUp;
            //BackgroundRectangle.MouseMove += BackgroundRectangle_MouseMove;
            TestIn.KeyUp += OnInputKeyUp;
        }

        private void OnInputKeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                Test_Click(null,null);
                TestIn.Text = "";
            }
        }

        private void BackgroundRectangle_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_DragDown)
            {
                var p = e.GetPosition(BackgroundRectangle) - m_DragDownPos;
                Left += p.X;
                Top += p.Y;
                m_DragDownPos = e.GetPosition(BackgroundRectangle);
            }
        }

        private void BackgroundRectangle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                m_DragDown = false;
            }
        }

        private void BackgroundRectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                m_DragDown = true;
                m_DragDownPos = e.GetPosition(BackgroundRectangle);
            }
        }
        

        private void Test_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Match match = null;
                if ((match = new Regex("AddSign ([0-9]+) ([0-9]+)").Match(TestIn.Text)).Success)
                {
                    if (long.TryParse(match.Groups[1].Value, out var id) && int.TryParse(match.Groups[2].Value, out var count))
                    {
                        m_Cxt.Log($"AddSign {DB.DBMgr.Instance.AddGiftItem(id, Aoe4DataConfig.SignTicket, count)}");
                    }
                    return;
                }
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
                    if (Aoe4DataConfig.ItemDatas.TryGetValue(m.GiftName, out var it))
                    {
                        m.GiftPrice = it.Price * 100;
                        m.MsgType = BilibiliDM_PluginFramework.MsgTypeEnum.GiftSend;
                        m.GiftCount = 1;
                    }else
                        return;
                    
                }
                if (m.CommentText[0] == '舰')
                {
                    m.GiftName = m.CommentText.Substring(1);
                    m.UserGuardLevel = m.GuardLevel = int.Parse(m.GiftName);
                    m.MsgType = BilibiliDM_PluginFramework.MsgTypeEnum.GuardBuy;
                }
                if (m.CommentText.StartsWith("ClearSign"))
                {
                    if(m.CommentText.Length > 9)
                    {
                        var id = int.Parse(m.CommentText.Substring(9));
                        m_Cxt.Log($"ClearSign {DB.DBMgr.Instance.ClearSignInDate(id)}");
                        return;
                    }
                    var c = 0;
                    DB.DBMgr.Instance.ForeachUsers((u) => c += DB.DBMgr.Instance.ClearSignInDate(u.Id));
                    m_Cxt.Log($"ClearSign {c}");
                    return;
                }
                m.UserName = string.Format("name_{0}", m.UserID_long);
                m.GuardLevel = m.UserGuardLevel = (int)(m.UserID_long <= 3 ? m.UserID_long : 0);


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