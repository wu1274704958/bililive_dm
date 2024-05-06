using InteractionGame;
using System;
using System.Windows;
using System.Windows.Input;
using BililiveDebugPlugin.InteractionGame.Data;
using System.Text.RegularExpressions;
using BilibiliDM_PluginFramework;
using conf;
using Utils;
using BililiveDebugPlugin.InteractionGame.plugs;
using UserData = BililiveDebugPlugin.DB.Model.UserData;
using conf.Squad;
using BililiveDebugPlugin.InteractionGameUtils;
using BililiveDebugPlugin.InteractionGame.Settlement;
using BililiveDebugPlugin.InteractionGame;

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
                if(TestIn.Text == "TestRe")
                {
                    Aoe4Settlement<DebugPlugin>.ClickRestart();
                    return;
                }
                if ((match = new Regex("AddSign ([0-9]+) ([0-9]+)").Match(TestIn.Text)).Success)
                {
                    if (int.TryParse(match.Groups[2].Value, out var count))
                    {
                        m_Cxt.Log($"AddSign {DB.DBMgr.Instance.AddGiftItem(match.Groups[1].Value, Aoe4DataConfig.SignTicket, count)}");
                    }
                    return;
                }
                if ((match = new Regex("AddUser ([0-9]+) ([0-9]+)").Match(TestIn.Text)).Success)
                {
                    if (int.TryParse(match.Groups[1].Value, out var count) && int.TryParse(match.Groups[2].Value,out var g))
                    {
                        var r = new Random();
                        for(int i = 1;i <= count;i++)
                            (m_Cxt as DebugPlugin).OnAddGroup(new global::InteractionGame.UserData(i.ToString(),$"name{i}","",g,0), g);
                    }
                    return;
                }
                if ((match = new Regex("AddTest ([0-9]+)").Match(TestIn.Text)).Success)
                {
                    if (int.TryParse(match.Groups[1].Value, out var count))
                    {
                        AddTestUser(count);
                    }
                    return;
                }
                if ((match = new Regex("Vip ([0-9a-z]+) ([0-9]+) ([0-9]*)").Match(TestIn.Text)).Success)
                {
                    if (int.TryParse(match.Groups[2].Value, out var lvl))
                    {
                        AddVipAndGift(match, match.Groups[1].Value, lvl);
                    }
                    return;
                }
                if ((match = new Regex("Vipng ([0-9a-z]+) ([0-9]+) ([0-9]*)").Match(TestIn.Text)).Success)
                {
                    var c = 1;
                    if (int.TryParse(match.Groups[2].Value, out var lvl))
                    {
                        if (match.Groups.Count > 3 && int.TryParse(match.Groups[3].Value, out c)) { }
                        AddVip(match, match.Groups[1].Value, lvl,c,out _);
                    }
                    return;
                }
                if ((match = new Regex("Squad ([0-9]+) ([0-9]*) ([0-9]*)").Match(TestIn.Text)).Success)
                {
                    var sid = 0;
                    var c = 1;
                    var g = 0;
                    if (int.TryParse(match.Groups[1].Value,out sid))
                    {
                        var _ = match.Groups.Count > 2 && int.TryParse(match.Groups[2].Value, out c);
                        _ = match.Groups.Count > 3 && int.TryParse(match.Groups[3].Value, out g);
                        var sd = SquadDataMgr.GetInstance().Get(sid);
                        if (sd != null)
                            SpawnSquad.SendSpawnSquad(null,GetTestUserByGroup(g), c, sd);
                    }
                    return;
                }
                if ((match = new Regex("援 ([0-9]+) ([0-9]*)").Match(TestIn.Text)).Success)
                {
                    int g = 0;
                    if (match.Groups.Count >= 3 && int.TryParse(match.Groups[2].Value, out var _g)) 
                        g = _g;
                    if (int.TryParse(match.Groups[1].Value, out var lvl))
                    {
                        if(conf.Reinforcements.ReinforcementsDataMgr.GetInstance().Dict.TryGetValue(lvl, out var data))
                            Locator.Instance.Get<DefineKeepDamagedSpawnSquadPlug>().DoSpawnSquad(g, data);
                    }
                    return;
                }
                if (TestIn.Text == "TransfarSys")
                {
                    TransfarSys();
                    return;
                }

                if (TestIn.Text == "Reload")
                {
                    ConfigMgr.ReloadAll();
                    Locator.Instance.Get<SyncSquadConfig>().SendMsg();
                    return;
                }

                if (TestIn.Text == "Settlement")
                {
                    Locator.Instance.Get<DebugPlugin>().DoSettlement(2,0,false);
                    return;
                }
                var m = new BilibiliDM_PluginFramework.DanmakuModel();
                var ss = TestIn.Text.Split(' ');
                if (ss.Length == 1)
                {
                    m.OpenID = 1.ToString();
                    m.CommentText = TestIn.Text;
                }
                else
                {
                    m.OpenID = ss[0].Trim();
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
                        var id = m.CommentText.Substring(9);
                        m_Cxt.Log($"ClearSign {DB.DBMgr.Instance.ClearSignInDate(id.Trim())}");
                        return;
                    }
                    var c = 0;
                    //DB.DBMgr.Instance.ForeachUsers((u) => c += DB.DBMgr.Instance.ClearSignInDate(u.Id));
                    m_Cxt.Log($"ClearSign {c}");
                    return;
                }
                m.UserName = string.Format("name_{0}", m.OpenID);
                //m.GuardLevel = m.UserGuardLevel = (int)(m.UserID_long <= 3 ? m.UserID_long : 0);


                var a = new BilibiliDM_PluginFramework.ReceivedDanmakuArgs() { Danmaku = m };
                m_Cxt.SendTestDanMu(this,a);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ex.ToString()));
            }
        }

        private void AddTestUser(int count)
        {
            for(int i = 1;i <= count; i++)
            {
                var m = new BilibiliDM_PluginFramework.DanmakuModel();
                m.OpenID = i.ToString();
                m.CommentText = "加塔";
                m.UserName = $"name_{i}";
                m.MsgType = BilibiliDM_PluginFramework.MsgTypeEnum.Comment;
                m_Cxt.SendTestDanMu(this, new BilibiliDM_PluginFramework.ReceivedDanmakuArgs() { Danmaku = m });
            }
            
        }

        private void TransfarSys()
        {
            var sys = DB.DBMgr2.Instance.Fsql.Select<DB.Model.SystemData>().ToList();
            var c = 0;
            foreach(var v in sys)
            {
                c += DB.DBMgr.Instance.Fsql.Insert(v).ExecuteAffrows();
            }
            m_Cxt.Log($"TransfarSys {c}");
        }

        private void AddVipAndGift(Match match, string id, int lvl)
        {
            var c = 1;
            if (match.Groups.Count > 3 && int.TryParse(match.Groups[3].Value, out c))
                ;
            var r = AddVip(match, id, lvl,c,out var ud);
            m_Cxt.Log($"AddLimitedItem {r}");
            if (r > 0)
            {
                var d = GetDmData(ud, MsgTypeEnum.GuardBuy);
                d.Danmaku.GuardLevel = d.Danmaku.UserGuardLevel = lvl;
                var mult = lvl > 10 ? lvl % 10 : 1;
                for (int i = 0; i < c * mult; ++i)
                    m_Cxt.SendTestDanMu(this, d);
            }
        }

        private int AddVip(Match match, string id, int lvl,int c,out UserData ud)
        {
            ud = DB.DBMgr.Instance.GetUser(id);
            string name = null;
            switch (lvl > 10 ? lvl / 10 : lvl)
            {
                case 2: name = Aoe4DataConfig.TiDu; break;
                case 3: name = Aoe4DataConfig.JianZhang; break;
            }
            if (ud == null || name == null)
            {
                m_Cxt.Log($"没有找到用户{match.Groups[1].Value}");
                return 0;
            }
            var r = DB.DBMgr.Instance.AddLimitedItem(id, name, lvl, 9999, c);
            m_Cxt.Log($"AddLimitedItem {r}");
            return r;
        }

        private  global::InteractionGame.UserData GetTestUserByGroup(int g)
        {
            return new global::InteractionGame.UserData("-1", "", "", g, 0);
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

        private BilibiliDM_PluginFramework.ReceivedDanmakuArgs GetDmData(UserData ud,
            BilibiliDM_PluginFramework.MsgTypeEnum type = BilibiliDM_PluginFramework.MsgTypeEnum.Comment
            )
        {
            var m = new BilibiliDM_PluginFramework.DanmakuModel();
            m.OpenID = ud.Id;
            m.UserName = ud.Name;
            m.MsgType = type;
            return new BilibiliDM_PluginFramework.ReceivedDanmakuArgs() { Danmaku = m };
        }

        private void CloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}