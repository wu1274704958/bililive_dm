﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using BilibiliDM_PluginFramework;
using BiliDMLib;

namespace Bililive_dm
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainOverlay overlay;
        public IDanmakuWindow fulloverlay;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int GWL_EXSTYLE = (-20);

        [DllImport("user32", EntryPoint = "SetWindowLong")]
        private static extern uint SetWindowLong(IntPtr hwnd, int nIndex, uint dwNewLong);

        [DllImport("user32", EntryPoint = "GetWindowLong")]
        private static extern uint GetWindowLong(IntPtr hwnd, int nIndex);

        private StoreModel settings = null;
        private DanmakuLoader b = new BiliDMLib.DanmakuLoader();
        private DispatcherTimer timer;
        private DispatcherTimer timer_magic;
        private const int _maxCapacity = 100;

        private ObservableCollection<string> _messageQueue = new ObservableCollection<string>();
        private ObservableCollection<SessionItem> SessionItems=new ObservableCollection<SessionItem>();

        private  Queue<DanmakuModel> _danmakuQueue=new Queue<DanmakuModel>();

        private Thread ProcDanmakuThread;

        private StaticModel Static=new StaticModel();





#region Runtime settings

        private bool fulloverlay_enabled = false;
        private bool overlay_enabled = true;
        private bool savelog_enabled = true;
        private bool sendssp_enabled = true;
        private bool showvip_enabled = true;
        private bool showerror_enabled = true;

#endregion

        public MainWindow()
        {
            InitializeComponent();
            DateTime dt = new DateTime(2000, 1, 1);
            Assembly assembly = Assembly.GetExecutingAssembly();
            String version = assembly.FullName.Split(',')[1];

            String fullversion = version.Split('=')[1];
            int dates = int.Parse(fullversion.Split('.')[2]);

            int seconds = int.Parse(fullversion.Split('.')[3]);
            dt = dt.AddDays(dates);
            dt = dt.AddSeconds(seconds * 2);
            if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
            {
                this.Title += "   版本号: " +
                System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
            }
            else
            {
                this.Title += "   *傻逼版本*";
            }
            this.Title += "   编译时间: " + dt.ToString();

            InitPlugins();
            this.Closed += MainWindow_Closed;
            web.Source=new Uri("http://soft.ceve-market.org/bilibili_dm/app.htm?"+DateTime.Now.Ticks); //fuck you IE cache
            b.Disconnected += b_Disconnected;
            b.ReceivedDanmaku += b_ReceivedDanmaku;
            b.ReceivedRoomCount += b_ReceivedRoomCount;
            try
            {
                IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User |
                                                                            IsolatedStorageScope.Domain |
                                                                            IsolatedStorageScope.Assembly, null, null);
                System.Xml.Serialization.XmlSerializer settingsreader =
                    new System.Xml.Serialization.XmlSerializer(typeof (StoreModel));
                StreamReader reader = new StreamReader(new IsolatedStorageFileStream(
                    "settings.xml", FileMode.Open, isoStore));
                settings = (StoreModel) settingsreader.Deserialize(reader);
                reader.Close();
                
            }
            catch (Exception)
            {
                settings=new StoreModel();
                
            }
            settings.SaveConfig();
            settings.toStatic();
            OptionDialog.LayoutRoot.DataContext = settings;

            timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, FuckMicrosoft,
                this.Dispatcher);
            timer.Start();
            
            DataGrid2.ItemsSource = SessionItems;
//            fulloverlay.Show();
           
            this.log.DataContext = _messageQueue;
//            log.ScrollToEnd();
            //            for (int i = 0; i < 150; i++)
            //            {
            //                logging("投喂记录不会在弹幕模式上出现, 这不是bug");
            //            }
            PluginGrid.ItemsSource = Plugins;

            if (DateTime.Today.Month == 4 && DateTime.Today.Day == 1)
            {
                //MAGIC!
                timer_magic=new DispatcherTimer(new TimeSpan(0,30,0),DispatcherPriority.Normal, (sender, args) =>
                {
                    var query=this.Plugins.Where(p => p.PluginName.Contains("点歌"));
                    if (query.Any())
                    {
                        query.First().MainReceivedDanMaku(new ReceivedDanmakuArgs()
                        {
                            Danmaku = new DanmakuModel()
                            {
                                MsgType = MsgTypeEnum.Comment,
                                CommentText = "点歌 34376018",
                                CommentUser = "弹幕姬",
                                isAdmin = true,
                                isVIP = true,
                                
                            }
                        });
                    }
                },this.Dispatcher);
                timer_magic.Start();
            }

            


            ProcDanmakuThread = new Thread(() =>
            {
                while (true)
                {
                    lock (_danmakuQueue)
                    {
                        int count = 0;
                        if (_danmakuQueue.Any())
                        {
                            count = (int) Math.Ceiling(_danmakuQueue.Count/30.0);
                        }

                        for (int i = 0; i < count; i++)
                        {
                            if (_danmakuQueue.Any())
                            {
                                var danmaku = _danmakuQueue.Dequeue();
                                ProcDanmaku(danmaku);
                                if (danmaku.MsgType==MsgTypeEnum.Comment)
                                { lock (Static)
                                {
                                    Static.DanmakuCountShow += 1;
                                    Static.AddUser(danmaku.CommentUser);
                                }}
                            }
                            
                        }


                    }
                    
                    Thread.Sleep(30);
                }
            });
            ProcDanmakuThread.IsBackground = true;
            ProcDanmakuThread.Start();
            StaticPanel.DataContext = Static;


            for (int i = 0; i < 100; i++)
            {
                _messageQueue.Add("");
            }
            logging("投喂记录不会在弹幕模式上出现, 这不是bug");
            logging("可以点击日志复制到剪贴板");
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded  (object sender, RoutedEventArgs e)
        {
            Full.IsChecked = fulloverlay_enabled;
            SideBar.IsChecked = overlay_enabled;
            SaveLog.IsChecked = savelog_enabled;
            SSTP.IsChecked = sendssp_enabled;
            ShowItem.IsChecked=showvip_enabled;
            ShowError.IsChecked=showerror_enabled;
            ScrollViewer sc=log.Template.FindName("LogScroll", log) as ScrollViewer;
            sc?.ScrollToEnd();

            var shit = new Thread(() =>
            {
                int bbb = 5;
                while (true)
                {
                    Random r = new Random();
                    
                    lock (_danmakuQueue)
                    {

                        for (int i = 0; i < bbb; i++)
                        {
                            string a1 = r.NextDouble().ToString();
                            string b1 = r.NextDouble().ToString();
                            _danmakuQueue.Enqueue(new DanmakuModel()

                            {
                                CommentUser = "asf",
                                CommentText = b1,
                                MsgType = MsgTypeEnum.Comment
                            });
                        }
                    }
                    lock (Static)
                    {
                        Static.DanmakuCountRaw += bbb;
                    }

                    Thread.Sleep(1000);
                }
            }
             );
            shit.IsBackground = true;
//            shit.Start();
        }

        private void MainWindow_Closed  (object sender, EventArgs e)
        {
            foreach (var dmPlugin in Plugins)
            {
                try
                {
                    dmPlugin.DeInit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                    "插件" + dmPlugin.PluginName + "遇到了不明錯誤: 日誌已經保存在桌面, 請有空發給該插件作者 " + dmPlugin.PluginAuth + ", 聯繫方式 " + dmPlugin.PluginCont);
                    try
                    {
                        string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);


                        using (StreamWriter outfile = new StreamWriter(path + @"\B站彈幕姬插件" + dmPlugin.PluginName + "錯誤報告.txt"))
                        {
                            outfile.WriteLine("請有空發給聯繫方式 " + dmPlugin.PluginCont + " 謝謝");
                            outfile.Write(ex.ToString());
                        }

                    }
                    catch (Exception)
                    {

                    }
                }
                
            }
        }

        ~MainWindow()
        {
            if (fulloverlay != null)
            {
                fulloverlay.Dispose();
                fulloverlay = null;
            }
        }

        private void FuckMicrosoft(object sender, EventArgs eventArgs)
        {
            if (fulloverlay != null)
            {
                fulloverlay.ForceTopmost();
            }
            if (overlay != null)
            {
                overlay.Topmost = false;
                overlay.Topmost = true;
            }
        }

        private void OpenFullOverlay()
        {
            var win8Version = new Version(6, 2, 9200);
            bool isWin8OrLater = Environment.OSVersion.Platform == PlatformID.Win32NT
                              && Environment.OSVersion.Version >= win8Version;
            if (isWin8OrLater && Store.WtfEngineEnabled)
                fulloverlay = new WtfDanmakuWindow();
            else
                fulloverlay = new WpfDanmakuOverlay();
            settings.PropertyChanged += fulloverlay.OnPropertyChanged;
            fulloverlay.Show();
        }

        private void OpenOverlay()
        {
            overlay = new MainOverlay();
            overlay.Deactivated += overlay_Deactivated;
            overlay.SourceInitialized += delegate
            {
                IntPtr hwnd = new WindowInteropHelper(overlay).Handle;
                uint extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
            };
            overlay.Background = Brushes.Transparent;
            overlay.ShowInTaskbar = false;
            overlay.Topmost = true;
            overlay.Top = SystemParameters.WorkArea.Top + Store.MainOverlayXoffset;
            overlay.Left = SystemParameters.WorkArea.Right - Store.MainOverlayWidth + Store.MainOverlayYoffset;
            overlay.Height = SystemParameters.WorkArea.Height;
            overlay.Width = Store.MainOverlayWidth;
        }

        private void overlay_Deactivated(object sender, EventArgs e)
        {
            if (sender is MainOverlay)
            {
                (sender as MainOverlay).Topmost = true;
            }
        }

        private async void connbtn_Click(object sender, RoutedEventArgs e)
        {
            int roomid;
            try
            {
                roomid = Convert.ToInt32(this.romid.Text.Trim());

            }
            catch (Exception)
            {
                MessageBox.Show("请输入房间号,房间号是!数!字!");
                return;
            }
            if (roomid > 0)
            {

                this.connbtn.IsEnabled = false;
                this.disconnbtn.IsEnabled = false;
                var connectresult = false;
                logging("正在连接");
                connectresult = await b.ConnectAsync(roomid);
                while (!connectresult && sender==null && AutoReconnect.IsChecked==true)
                {
                    logging("正在连接");
                    connectresult = await b.ConnectAsync(roomid);
                    
                }
                
                
                if (connectresult)
                {

                    errorlogging("連接成功");
                    AddDMText("彈幕姬報告", "連接成功", true);
                    SendSSP("連接成功");
                    Ranking.Clear();
                    
                    foreach (var dmPlugin in Plugins)
                    {
                     
                        new Thread(()=> {
                                            try
                                            {
                                                dmPlugin.MainConnected(roomid);
                                            }
                                            catch (Exception ex)
                                            {
                                                Utils.PluginExceptionHandler(ex, dmPlugin);
                                            }
                                            }).Start();
                    }
                }
                else
                {
                    logging("連接失敗");
                    SendSSP("連接失敗");
                    AddDMText("彈幕姬報告", "連接失敗", true);

                    this.connbtn.IsEnabled = true;
                }
                this.disconnbtn.IsEnabled = true;
            }
            else
            {
                MessageBox.Show("ID非法");
            }
        }

        private void b_ReceivedRoomCount(object sender, ReceivedRoomCountArgs e)
        {
//            logging("當前房間人數:" + e.UserCount);
//            AddDMText("當前房間人數", e.UserCount+"", true);
            //AddDMText(e.Danmaku.CommentUser, e.Danmaku.CommentText);
            if (this.CheckAccess())
            {
                OnlineBlock.Text = e.UserCount + "";
                
                
            }
            else
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    OnlineBlock.Text = e.UserCount + "";
                   
                }));
            }
            foreach (var dmPlugin in Plugins)
            {
                if (dmPlugin.Status)
                new Thread(() =>
                {
                    try
                    {
                        dmPlugin.MainReceivedRoomCount(e);
                    }
                    catch(Exception ex)
                    {
                        Utils.PluginExceptionHandler(ex, dmPlugin);
                    }
                }).Start();
            }

            SendSSP("当前房间人数:" + e.UserCount);
        }

        private ObservableCollection<GiftRank> Ranking = new ObservableCollection<GiftRank>();

        private void b_ReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
            foreach (var dmPlugin in Plugins)
            {
                if (dmPlugin.Status)
                    new Thread(() => {
                                         try
                                         {
                                             dmPlugin.MainReceivedDanMaku(e);
                                         }
                                         catch (Exception ex)
                                         {
                            Utils.PluginExceptionHandler(ex, dmPlugin);
                        }
                    }).Start();
            }

            if (e.Danmaku.MsgType == MsgTypeEnum.Comment)
            {
                lock (Static)
                {
                    Static.DanmakuCountRaw += 1;
                }
            }

            lock (_danmakuQueue)
            {
                var danmakuModel = e.Danmaku;
                _danmakuQueue.Enqueue(danmakuModel);
            }
            

        }

        private  void ProcDanmaku(DanmakuModel danmakuModel)
        {
            switch (danmakuModel.MsgType)
            {
                case MsgTypeEnum.Comment:
                    logging("收到彈幕:" + (danmakuModel.isAdmin ? "[管]" : "") + (danmakuModel.isVIP ? "[爷]" : "") +
                            danmakuModel.CommentUser + " 說: " + danmakuModel.CommentText);

                    AddDMText(
                        (danmakuModel.isAdmin ? "[管]" : "") + (danmakuModel.isVIP ? "[爷]" : "") + danmakuModel.CommentUser,
                        danmakuModel.CommentText);
                    SendSSP(string.Format(@"\_q{0}\n\_q\f[height,20]{1}",
                        (danmakuModel.isAdmin ? "[管]" : "") + (danmakuModel.isVIP ? "[爷]" : "") + danmakuModel.CommentUser,
                        danmakuModel.CommentText));

                    break;
                case MsgTypeEnum.GiftTop:
                    foreach (var giftRank in danmakuModel.GiftRanking)
                    {
                        var query = Ranking.Where(p => p.uid == giftRank.uid);
                        if (query.Any())
                        {
                            var f = query.First();
                            this.Dispatcher.BeginInvoke(new Action(() => f.coin = giftRank.coin));
                        }
                        else
                        {
                            this.Dispatcher.BeginInvoke(new Action(() => Ranking.Add(new GiftRank()
                            {
                                uid = giftRank.uid,
                                coin = giftRank.coin,
                                UserName = giftRank.UserName
                            })));
                        }
                    }
                    break;
                case MsgTypeEnum.GiftSend:
                {
                    var query = SessionItems.Where(p => p.UserName == danmakuModel.GiftUser && p.Item == danmakuModel.GiftName);
                    if (query.Any())
                    {
                        this.Dispatcher.BeginInvoke(
                            new Action(() => query.First().num += Convert.ToDecimal(danmakuModel.GiftNum)));
                    }
                    else
                    {
                        this.Dispatcher.BeginInvoke(new Action(() => SessionItems.Add(
                            new SessionItem()
                            {
                                Item = danmakuModel.GiftName,
                                UserName = danmakuModel.GiftUser,
                                num = Convert.ToDecimal(danmakuModel.GiftNum)
                            }
                            )));
                    }
                    logging("收到道具:" + danmakuModel.GiftUser + " 赠送的: " + danmakuModel.GiftName + " x " + danmakuModel.GiftNum);
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (ShowItem.IsChecked == true)
                        {
                            AddDMText("收到道具",
                                danmakuModel.GiftUser + " 赠送的: " + danmakuModel.GiftName + " x " + danmakuModel.GiftNum, true);
                        }
                    }));
                    break;
                }
                case MsgTypeEnum.Welcome:
                {
                    logging("欢迎老爷" + (danmakuModel.isAdmin ? "和管理" : "") + ": " + danmakuModel.CommentUser + " 进入直播间");
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (ShowItem.IsChecked == true)
                        {
                            AddDMText("欢迎老爷" + (danmakuModel.isAdmin ? "和管理" : ""),
                                danmakuModel.CommentUser + " 进入直播间", true);
                        }
                    }));

                    break;
                }
            }
        }

        private void SendSSP(string msg)
        {
            if (SSTP.Dispatcher.CheckAccess())
            {

                if (SSTP.IsChecked == true)
                {
                    SSTPProtocol.SendSSPMsg(msg);
                }

            }
            else
            {
                SSTP.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => SendSSP(msg)));
            }
            
        }

        private void b_Disconnected(object sender, DisconnectEvtArgs args)
        {
            foreach (var dmPlugin in Plugins)
            {
                    new Thread(() => {
                                         try
                                         {
                                             dmPlugin.MainDisconnected();
                                         }
                                         catch (Exception ex)
                                         {
                            Utils.PluginExceptionHandler(ex, dmPlugin);
                        }
                    }).Start();
            }
            
            errorlogging("連接被斷開: 开发者信息" + args.Error);
            AddDMText("彈幕姬報告", "連接被斷開", true);
            SendSSP("連接被斷開");
            if (this.CheckAccess())
            {
                if (AutoReconnect.IsChecked == true && args.Error != null)
                {
                    errorlogging("正在自动重连...");
                    AddDMText("彈幕姬報告", "正在自动重连", true);
                    connbtn_Click(null, null);
                }
                else
                {
                    this.connbtn.IsEnabled = true;
                }
            }
            else
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (AutoReconnect.IsChecked == true && args.Error != null)
                    {
                        errorlogging("正在自动重连...");
                        AddDMText("彈幕姬報告", "正在自动重连", true);
                        connbtn_Click(null, null);
                    }
                    else
                    {
                        this.connbtn.IsEnabled = true;
                    }
                }));
            }
        }

        public void errorlogging(string text)
        {
            if (!showerror_enabled) return;
            if (ShowError.Dispatcher.CheckAccess())
            {

                logging(text);

            }
            else
            {
                ShowError.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => errorlogging(text)));
            }
        }

        public void logging(string text)
        {

            
            if (log.Dispatcher.CheckAccess())
            {
                
                if (_messageQueue.Count >= _maxCapacity)
                {
                    _messageQueue.RemoveAt(0);
                }

                _messageQueue.Add(DateTime.Now.ToString("T")+" : " +text);
//                this.log.Text = string.Join("\n", _messageQueue);
//                log.CaretIndex = this.log.Text.Length;


                if (savelog_enabled) { 
                try
                {
                    string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);


                    path = System.IO.Path.Combine(path, "弹幕姬");
                    System.IO.Directory.CreateDirectory(path);
                    using (StreamWriter outfile = new StreamWriter(System.IO.Path.Combine(path, DateTime.Now.ToString("yyyy-MM-dd") + ".txt"),true))
                    {
                        outfile.WriteLine(DateTime.Now.ToString("T") + " : " + text);
                    }
                }
                catch (Exception ex)
                {
                }
                }

            }
            else
            {
                log.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => logging(text)));
            }
        }

        public void AddDMText(string user, string text, bool warn = false,bool foreceenablefullscreen=false)
        {
            if (showerror_enabled && warn)
            {
                return;
            }
            if (!overlay_enabled && !fulloverlay_enabled) return;
            if (this.Dispatcher.CheckAccess())
            {
               
                if (this.SideBar.IsChecked == true)
                {
                    DanmakuTextControl c = new DanmakuTextControl();

                    c.UserName.Text = user;
                    if (warn)
                    {
                        c.UserName.Foreground = Brushes.Red;
                    }
                    c.Text.Text = text;
                    c.ChangeHeight();
                    var sb = (Storyboard) c.Resources["Storyboard1"];
                    //Storyboard.SetTarget(sb,c);
                    sb.Completed += sb_Completed;
                    overlay.LayoutRoot.Children.Add(c);
                }
                if (this.Full.IsChecked == true && (!warn || foreceenablefullscreen))
                {
                    fulloverlay.AddDanmaku(DanmakuType.Scrolling, text, 0xFFFFFFFF);
                }
            }
            else
            {
                log.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => AddDMText(user, text)));
            }
        }

        private void sb_Completed(object sender, EventArgs e)
        {
            var s = sender as ClockGroup;
            if (s == null) return;
            var c = Storyboard.GetTarget(s.Children[2].Timeline) as DanmakuTextControl;
            if (c != null)
            {
                overlay.LayoutRoot.Children.Remove(c);
            }
        }

        public void Test_OnClick(object sender, RoutedEventArgs e)
        {
//            logging("投喂记录不会在弹幕模式上出现, 这不是bug");
            Random ran = new Random();

            int n = ran.Next(100);
            if (n > 98)
            {
                AddDMText("彈幕姬報告", "這不是個測試", false);
            }
            else{
            AddDMText("彈幕姬報告", "這是一個測試", false);
            }
            SendSSP("彈幕姬測試");
            foreach (var dmPlugin in Plugins)
            {
                if (dmPlugin.Status)
                    new Thread(() => {
                        try
                        {
                            var m = new ReceivedDanmakuArgs()
                            {
                                Danmaku =
                                    new DanmakuModel()
                                    {
                                        CommentText = "插件彈幕測試",
                                        CommentUser = "彈幕姬",
                                        MsgType = MsgTypeEnum.Comment
                                    },
                            };
                            dmPlugin.MainReceivedDanMaku(m);
                        }
                        catch (Exception ex)
                        {
                            Utils.PluginExceptionHandler(ex, dmPlugin);
                        }
                    }).Start();
            }
          
//            logging(DateTime.Now.Ticks+"");
        }

        private void Full_Checked(object sender, RoutedEventArgs e)
        {
            //            overlay.Show();
            fulloverlay_enabled = true;
            OpenFullOverlay();
            fulloverlay.Show();
        }

        private void SideBar_Checked(object sender, RoutedEventArgs e)
        {
            overlay_enabled = true;
            OpenOverlay();
            overlay.Show();
        }

        private void SideBar_Unchecked(object sender, RoutedEventArgs e)
        {
            overlay_enabled = false;
            overlay.Close();
        }

        private void Full_Unchecked(object sender, RoutedEventArgs e)
        {
            fulloverlay_enabled = false;
            fulloverlay.Close();
        }

      

        private void Disconnbtn_OnClick(object sender, RoutedEventArgs e)
        {
            b.Disconnect();
            this.connbtn.IsEnabled = true;
            foreach (var dmPlugin in Plugins)
            {
                new Thread(() => {
                                     try
                                     {
                                         dmPlugin.MainDisconnected();
                                     }
                                     catch (Exception ex)
                                     {
                        Utils.PluginExceptionHandler(ex, dmPlugin);
                    }
                }).Start();
            }
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void ClearMe_OnClick(object sender, RoutedEventArgs e)
        {
            SessionItems.Clear();
            
        }
        private void ClearMe2_OnClick(object sender, RoutedEventArgs e)
        {
            lock (Static)
            {
                Static.DanmakuCountShow = 0;
            }

        }
        private void ClearMe3_OnClick(object sender, RoutedEventArgs e)
        {
            lock (Static)
            {
                Static.ClearUser();
            }

        }

        private void ClearMe4_OnClick(object sender, RoutedEventArgs e)
        {
            lock (Static)
            {
                Static.DanmakuCountRaw = 0;
            }


        }
        private void Plugin_Enable(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;

            var contextMenu = (ContextMenu)menuItem.Parent;

            var item = (DataGrid)contextMenu.PlacementTarget;
            if (item.SelectedCells.Count == 0) return;
            var plugin = item.SelectedCells[0].Item as DMPlugin;
            if (plugin == null) return;

            try
            {
                if (!plugin.Status) plugin.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "插件" + plugin.PluginName + "遇到了不明錯誤: 日誌已經保存在桌面, 請有空發給該插件作者 " + plugin.PluginAuth + ", 聯繫方式 " + plugin.PluginCont);
                try
                {
                    string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);


                    using (StreamWriter outfile = new StreamWriter(path + @"\B站彈幕姬插件" + plugin.PluginName + "錯誤報告.txt"))
                    {
                        outfile.WriteLine("請有空發給聯繫方式 " + plugin.PluginCont + " 謝謝");
                        outfile.WriteLine(DateTime.Now+" "+ plugin.PluginName + " " + plugin.PluginVer);
                        outfile.Write(ex.ToString());
                    }

                }
                catch (Exception)
                {

                }
            }
        }
        private void Plugin_Disable(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;

            var contextMenu = (ContextMenu)menuItem.Parent;

            var item = (DataGrid)contextMenu.PlacementTarget;
            if (item.SelectedCells.Count == 0) return;
            var plugin = item.SelectedCells[0].Item as DMPlugin;
            if (plugin == null) return;

            try
            {
                if (plugin.Status) plugin.Stop();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "插件"+plugin.PluginName+"遇到了不明錯誤: 日誌已經保存在桌面, 請有空發給該插件作者 "+plugin.PluginAuth+", 聯繫方式 "+plugin.PluginCont);
                try
                {
                    string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);


                    using (StreamWriter outfile = new StreamWriter(path + @"\B站彈幕姬插件" + plugin.PluginName + "錯誤報告.txt"))
                    {
                        outfile.WriteLine("請有空發給聯繫方式 " + plugin.PluginCont + " 謝謝");
                        outfile.WriteLine(DateTime.Now + " " + plugin.PluginName+" "+plugin.PluginVer);
                        outfile.Write(ex.ToString());
                    }

                }
                catch (Exception)
                {
                    
                }
            }

        }
        private void Plugin_admin(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;

            var contextMenu = (ContextMenu)menuItem.Parent;

            var item = (DataGrid)contextMenu.PlacementTarget;
            if (item.SelectedCells.Count == 0) return;
            var plugin = item.SelectedCells[0].Item as DMPlugin;
            if (plugin == null) return;

            try
            {
                 plugin.Admin();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "插件" + plugin.PluginName + "遇到了不明錯誤: 日誌已經保存在桌面, 請有空發給該插件作者 " + plugin.PluginAuth + ", 聯繫方式 " + plugin.PluginCont);
                try
                {
                    string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);


                    using (StreamWriter outfile = new StreamWriter(path + @"\B站彈幕姬插件" + plugin.PluginName + "錯誤報告.txt"))
                    {
                        outfile.WriteLine(DateTime.Now + " " + "請有空發給聯繫方式 " + plugin.PluginCont + " 謝謝");
                        outfile.WriteLine(plugin.PluginName + " " + plugin.PluginVer);
                        outfile.Write(ex.ToString());
                    }

                }
                catch (Exception)
                {

                }
            }
        }
        ObservableCollection<DMPlugin> Plugins=new ObservableCollection<DMPlugin>();
        void InitPlugins()
        {
            Plugins.Add(new MobileService());
            string path = "";
            try
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);


                path = System.IO.Path.Combine(path, "弹幕姬","Plugins");
                System.IO.Directory.CreateDirectory(path);
                


            }
            catch (Exception ex)
            {
                return;
            }
            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                try
                {
                    var dll=Assembly.LoadFrom(file);
                    foreach (var exportedType in dll.GetExportedTypes())
                    {
                        if (exportedType.BaseType == typeof (DMPlugin))
                        {
                           var plugin= (DMPlugin)Activator.CreateInstance(exportedType);
                            
                            Plugins.Add(plugin);
                        }
                    }
                }
                catch (Exception)
                {
                    
                }
            }
            
        }

        private void WindowTop_OnChecked(object sender, RoutedEventArgs e)
        {
            Topmost = WindowTop.IsChecked==true;
        }

        private void SaveLog_OnChecked(object sender, RoutedEventArgs e)
        {
            this.savelog_enabled = true;
        }

        private void SaveLog_OnUnchecked(object sender, RoutedEventArgs e)
        {
            savelog_enabled = false;
        }

        private void ShowItem_OnChecked(object sender, RoutedEventArgs e)
        {
            showvip_enabled = true;
        }

        private void ShowItem_OnUnchecked(object sender, RoutedEventArgs e)
        {
            showvip_enabled = false;
        }

        private void SSTP_OnChecked(object sender, RoutedEventArgs e)
        {
            sendssp_enabled = true;
        }

        private void SSTP_OnUnchecked(object sender, RoutedEventArgs e)
        {
            sendssp_enabled = false;
        }

        private void ShowError_OnChecked(object sender, RoutedEventArgs e)
        {
            showerror_enabled = true;
        }

        private void ShowError_OnUnchecked(object sender, RoutedEventArgs e)
        {
            showerror_enabled = false;
        }

        private void UIElement_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var textBlock = sender as TextBlock;
                if (textBlock != null)
                {
                    Clipboard.SetText(textBlock.Text);
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => { MessageBox.Show("本行记录已复制到剪贴板"); }));
                    
                }
                
            }
            catch (Exception)
            {
               
            }
        }
    }
}