using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Lifetime;
using System.Threading;
using System.Windows.Threading;
using BilibiliDM_PluginFramework;
using BililiveDebugPlugin.InteractionGame;
using BililiveDebugPlugin.InteractionGame.Data;
using BililiveDebugPlugin.InteractionGame.Parser;
using BililiveDebugPlugin.InteractionGame.Resource;
using Interaction;
using InteractionGame;
using Newtonsoft.Json;

namespace BililiveDebugPlugin
{
    public class DebugPlugin : DMPlugin ,IContext,IPlayerParserObserver
    {
        class RankMsg
        {
            public string Title;
            public List<UserData> Items;
        }
        class GoldInfo
        {
            public long Id;
            public int Gold;
            public float Progress;
        }
        class GoldInfoArr
        {
            public List<GoldInfo> Items = new List<GoldInfo>();
        }
        enum EMsgTy:short
        {
            None = 0,
            Settlement = 1,
            AddPlayer = 2,
            ClearAllPlayer = 3,
            UpdatePlayerGold = 4,
            ShowPlayerAction = 5,
            RemoveGroup = 6
        }
        private bool IsStart = false;
        public MessageDispatcher<
            PlayerBirthdayParser<DebugPlugin>,
            MsgGiftParser<DebugPlugin>,
            DefAoe4Bridge<DebugPlugin>,
            Aoe4BaoBingResMgr<DebugPlugin>, DebugPlugin> messageDispatcher { get; private set; }
        private MainPage mp;
        private Action<DyMsg> m_AppendMsgAction;
        private DateTime m_AutoAppendMsgTime;
        private Random m_Rand;
        private int LastState = 0;
        public static readonly bool AutoAppendMsgOnIdle = false;
        public static readonly int EndDelay = 9000;
        public static readonly Dictionary<string, int> ColorMapIndex = new Dictionary<string, int>
        {
            { "蓝",0 },
            { "红",1 },
            { "绿",2 },
            { "黄",3 },

            { "蓝金",10 },
            { "红金",11 },
            { "绿金",12 },
            { "黄金",13 },
        };
        private IGameStateObserver<EAoe4State, Aoe4StateData> m_GameState = new Aoe4GameState();
        private List<(Action, DateTime)> TaskList = new List<(Action, DateTime)>();
        SM_SendMsg SendMsg = new SM_SendMsg();
        private int IsDispatch = 0;
        private int GameSt = 1;
        private DateTime UpdatePlayerGoldTime = DateTime.Now;
        private TimeSpan UpdatePlayerInterval = TimeSpan.FromSeconds(1);
        private Utils.ObjectPool<GoldInfo> GoldInfoPool = new Utils.ObjectPool<GoldInfo>(()=>new GoldInfo());
        private Utils.ObjectPool<GoldInfoArr> GoldInfoArrPool;

        private void OnRetGoldInfoArr(GoldInfoArr a)
        {
            for(int i = 0;i < a.Items.Count;++i)
            {
                GoldInfoPool.Return(a.Items[i]);
            }
            a.Items.Clear();
        }

        public DebugPlugin()
        {
            ReceivedDanmaku += OnReceivedDanmaku;
            PluginAuth = "CopyLiu";
            PluginName = "開發員小工具";
            PluginCont = "copyliu@gmail.com";
            PluginVer = "v0.0.2";
            PluginDesc = "它看着很像F12";

            GoldInfoArrPool = new Utils.ObjectPool<GoldInfoArr>(() => new GoldInfoArr(), OnRetGoldInfoArr);
        }
        public static string GetSquadName(int id)
        {
            var sd = Aoe4DataConfig.GetSquad(id);
            if (sd.Invaild) return "";
            return sd.Name;
        }
        public Aoe4StateData CheckState(EAoe4State state)
        {
            return m_GameState.CheckState(state);
        }
        public Aoe4StateData CheckState(EAoe4State state, IntPtr hwnd)
        {
            return m_GameState.CheckState(state, hwnd);
        }
        public IGameStateObserver<EAoe4State, Aoe4StateData> GetGameState()
        {
            return m_GameState;
        }

        private void OnReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
            if (IsDispatch == 0 && LastState == 1 && (messageDispatcher?.Demand(e.Danmaku, e.Danmaku.MsgType) ?? false))
                messageDispatcher.Dispatch(e.Danmaku, e.Danmaku.MsgType);
        }


        public override void Admin()
        {
            base.Admin();
            mp = new MainPage(this);
            mp.Show();
            //SendSettlement("蓝方获胜", new List<UserData>
            //{
            //    new UserData(){ Group = 1,Score = 6777 ,Icon = "22222",Name = "hahah2",Soldier_num = 12},
            //    new UserData(){ Group = 2,Score = 345 ,Icon = "767822",Name = "2123ds",Soldier_num = 92},
            //});
        }

        public override void Start()
        {
            base.Start();
            IsStart = true;

            messageDispatcher = new MessageDispatcher<PlayerBirthdayParser<DebugPlugin>,
                MsgGiftParser<DebugPlugin>, DefAoe4Bridge<DebugPlugin>,
                Aoe4BaoBingResMgr<DebugPlugin>, DebugPlugin>();
            messageDispatcher.Init(this);
            messageDispatcher.Start();
            messageDispatcher.GetPlayerParser().AddObserver(this);
            m_GameState.Init();
            SendMsg.Init();
            SendMsg.SendMessage((short)EMsgTy.ClearAllPlayer, "{}");
            Log("Start ...");
        }

        public override void Stop()
        {
            base.Stop();
            RealStop();
            IsStart = false;
        }

        private void RealStop()
        {
            if (!IsStart) return;
            messageDispatcher.Stop();
            m_GameState.Stop();
            SendMsg.Dispose();
            messageDispatcher = null;
        }

        public override void DeInit()
        {
            base.DeInit();
            RealStop();
        }

        public void OnInit(Action<DyMsg> appendMsgAction)
        {
            m_AppendMsgAction = appendMsgAction;
            m_AutoAppendMsgTime = DateTime.Now;
            m_Rand = new Random((int)DateTime.Now.Ticks);
        }

        public void OnStop()
        {
            m_AppendMsgAction = null;
        }

        public void OnTick(float delta)
        {
            var now = DateTime.Now;
            //Log($"OnTick");

            
            var d = m_GameState.CheckState(EAoe4State.Default);
            switch(GameSt)
            {
                case 0:
                    if (LastState != 2 && d.r == 2)
                    {
                        GameSt = 1;
                        Interlocked.Exchange(ref LastState, d.r);
                        //PrintGameMsg($"{GetColorById(d.g)}方获胜！！!");
                        messageDispatcher.MsgParser.AddWinScore(d.g, 300);
                        var data = messageDispatcher.MsgParser.GetSortedUserData();
                        messageDispatcher.MsgParser.ClearUserData();
                        //todo show settlement
                        SendSettlement($"{GetColorById(d.g)}方获胜", data);
                        messageDispatcher.Clear();
                        Thread.Sleep(EndDelay);
                    }
                    else
                        Interlocked.Exchange(ref LastState, d.r);
                    Interlocked.Exchange(ref IsDispatch, d.b);
                    break;
                case 1:
                    if (d.r == 1 && d.g == 0 && d.b == 0)
                    {
                        GameSt = 0;
                    }
                    else
                    {
                        messageDispatcher.GetBridge().TryStartGame();
                    }
                    break;
            }
            for(int i = TaskList.Count - 1;i >= 0;i--)
            {
                if (TaskList[i].Item2 >= DateTime.Now)
                {
                    TaskList[i].Item1.Invoke();
                    TaskList.RemoveAt(i);
                }
            }

            if((now - UpdatePlayerGoldTime) >= UpdatePlayerInterval)
            {
                UpdatePlayerGold();
                UpdatePlayerGoldTime = now;
            }
            SendMsg.flush();
        }

        private void SendSettlement(string v, List<UserData> data)
        {
            RankMsg rankMsg = new RankMsg()
            {
                Title = v,
                Items = data
            };
            SendMsg.SendMsg((short)EMsgTy.Settlement, rankMsg);
        }

        public static string GetColorById(int id)
        {
            foreach(var v in ColorMapIndex)
            {
                if(v.Value + 1 ==  id)
                {
                    return v.Key;
                }
            }
            return "";
        }
        private void AppendRandomMsg(int p, int sid_s, int sid_e, int num_s, int num_e)
        {
            var num = m_Rand.Next(num_s, num_e);
            //Log($"AppendRandomMsg num = {num}");
            for (int i = 0;i < num;i++)
            {
                var Msg = m_Rand.Next(sid_s, sid_e + 1);
                //Log($"AppendRandomMsg Msg = {Msg}");
                AppendMsg(new DyMsg(){ Player = p,  Msg = Msg});
            }
        }
        
        public void AppendMsg(DyMsg msg)
        {
            m_AppendMsgAction?.Invoke(msg);
        }
        public void AppendMsg(DyMsg msg, float delay)
        {
            TaskList.Add((() =>
            {
                m_AppendMsgAction?.Invoke(msg);
            }, DateTime.Now + TimeSpan.FromSeconds(delay)));
        }

        public void OnAppendMsg(DyMsg msg)
        {
            m_AutoAppendMsgTime = DateTime.Now;
        }

        public void PrintGameMsg(string text)
        {
            messageDispatcher.Aoe4Bridge.ExecPrintMsg(text);
        }

        public void SendTestDanMu(object sender, ReceivedDanmakuArgs e)
        {
            OnReceivedDanmaku(sender, e);
        }
        private void UpdatePlayerGold()
        {
            var resMgr = messageDispatcher.GetResourceMgr();
            if (resMgr.PlayerCount() == 0) return;
            var arr = GoldInfoArrPool.Get();
            var autoSpawn = messageDispatcher.GetMsgParser().GetSubMsgParse<AutoSpawnSquadSubMsgParser<DebugPlugin>>();
            resMgr.Foreach(0, (id, c) =>
            {
                var it = GoldInfoPool.Get();
                it.Id = id;
                it.Gold = c;
                it.Progress = autoSpawn.GetSpawnProgress(id);
                arr.Items.Add(it);
            });
            SendMsg.SendMsg((short)EMsgTy.UpdatePlayerGold, arr);
            GoldInfoArrPool.Return(arr);
        }
        public void OnAddGroup(UserData userData, int g)
        {
            SendMsg.SendMsg((short)EMsgTy.AddPlayer, userData);
        }

        public void OnChangeGroup(UserData userData, int old, int n)
        {

        }

        public void OnClear()
        {
            SendMsg.SendMessage((short)EMsgTy.ClearAllPlayer, "{}");
        }

        public int IsGameStart()
        {
            return LastState;
        }
    }
}