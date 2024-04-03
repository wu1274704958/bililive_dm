using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Threading;
using System.Windows.Threading;
using BilibiliDM_PluginFramework;
using BililiveDebugPlugin.InteractionGame;
using BililiveDebugPlugin.InteractionGame.Data;
using BililiveDebugPlugin.InteractionGame.Parser;
using BililiveDebugPlugin.InteractionGame.plugs;
using BililiveDebugPlugin.InteractionGame.Resource;
using BililiveDebugPlugin.InteractionGame.Settlement;
using BililiveDebugPlugin.InteractionGameUtils;
using conf;
using Interaction;
using InteractionGame;
using Newtonsoft.Json;
using ProtoBuf;
using Utils;

namespace BililiveDebugPlugin
{
    [ProtoContract]
    class GoldInfo
    {
        [ProtoMember(1)]
        public long Id;
        [ProtoMember(2)]
        public int Gold;
        [ProtoMember(3)]
        public float Progress;
    }
    [ProtoContract]
    class GoldInfoArr
    {
        [ProtoMember(1)]
        public List<GoldInfo> Items = new List<GoldInfo>();
    }
    [ProtoBuf.ProtoContract]
    public class EventCueData
    {
        [ProtoBuf.ProtoMember(1)] 
        public string Msg;
    }
    public enum EGameAction
    {
        GameStart,
        GameStop
    }
    public class DebugPlugin : DMPlugin ,IContext,IPlayerParserObserver
    {
        private bool IsStart = false;
        public MessageDispatcher<
            PlayerBirthdayParser<DebugPlugin>,
            MsgGiftParser<DebugPlugin>,
            DefAoe4Bridge<DebugPlugin>,
            Aoe4BaoBingResMgr<DebugPlugin>, DebugPlugin> messageDispatcher { get; private set; }
        public PlugMgr<EGameAction> m_PlugMgr { get;private set; } = new PlugMgr<EGameAction>();
        private MainPage mp;
        private Action<DyMsg> m_AppendMsgAction;
        private DateTime m_AutoAppendMsgTime;
        private Random m_Rand;
        private int LastState = 0;
        public static readonly bool AutoAppendMsgOnIdle = false;
        public static readonly int EndDelay = 14000;
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
        public SM_SendMsg SendMsg { private set; get; } = new SM_SendMsg();
        private int IsDispatch = 0;
        private int GameSt = 1;
        private DateTime UpdatePlayerGoldTime = DateTime.Now;
        private TimeSpan UpdatePlayerInterval = TimeSpan.FromSeconds(1);
        private DateTime CheckIsBlackPopTime = DateTime.Now;
        private Utils.ObjectPool<GoldInfo> GoldInfoPool = new Utils.ObjectPool<GoldInfo>(()=>new GoldInfo());
        private Utils.ObjectPool<GoldInfoArr> GoldInfoArrPool;
        private Aoe4Settlement<DebugPlugin> m_Settlement = new Aoe4Settlement<DebugPlugin>();

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
            if (LastState == 1 && (messageDispatcher?.Demand(e.Danmaku, e.Danmaku.MsgType) ?? false))
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
            var _ = DB.DBMgr.Instance;
            ConfigMgr.Init();
            messageDispatcher = new MessageDispatcher<PlayerBirthdayParser<DebugPlugin>,
                MsgGiftParser<DebugPlugin>, DefAoe4Bridge<DebugPlugin>,
                Aoe4BaoBingResMgr<DebugPlugin>, DebugPlugin>();
            m_PlugMgr.Add(1000 * 30,new AutoForceStopPlug());
            m_PlugMgr.Add(1000, new SquadCapacityUIPlug());
            m_PlugMgr.Add(1000 * 60,new AutoDownLivePlug());
            m_PlugMgr.Add(-1, new SyncSquadConfig());
            m_PlugMgr.Add(-1, new SelfSaleGuardPlug());
            m_PlugMgr.Add(300,new DefineKeepDamagedSpawnSquadPlug());
            m_PlugMgr.Add(100,new EveryoneTowerPlug());
            //m_PlugMgr.Add(2300, new Aoe4AutoAttack());
            Locator.Instance.Deposit(m_GameState);
            Locator.Instance.Deposit(this);
            Locator.Instance.Deposit(messageDispatcher);
            Locator.Instance.Deposit(m_PlugMgr);
            Locator.Instance.Deposit(SendMsg);
            m_GameState.Init();
            SendMsg.Init();
            m_PlugMgr.Init();
            messageDispatcher.Init(this);
            messageDispatcher.Start();
            m_PlugMgr.Start();
            m_GameState.Start();
            Log("Start ...");
            SendMsg.SendMessage((short)EMsgTy.ClearAllPlayer, null);
            messageDispatcher.GetPlayerParser().AddObserver(this);
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
            m_PlugMgr.Dispose();
            DB.DBMgr.Instance.Dispose();
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
        private void CheckIsBlackPop(DateTime now)
        {
            if((now - CheckIsBlackPopTime).TotalSeconds >= 1)
            {
                CheckIsBlackPopTime = now;
                m_GameState.CloseDisconnectPopup();
            }
        }
        public void OnTick(float delta)
        {
            var now = DateTime.Now;
            //Log($"OnTick");

            
            var d = m_GameState.CheckState(EAoe4State.Default);
            switch(GameSt)
            {
                case 0:
                    if (LastState != 2 && d.R == 2)
                    {
                        DoSettlement(d.R,d.G);
                    }
                    else
                        Interlocked.Exchange(ref LastState, d.R);
                    Interlocked.Exchange(ref IsDispatch, d.B);
                    m_GameState.OnTick();
                    m_PlugMgr.Tick(0.1f);
                    break;
                case 1:
                    if (d.R == 1 && d.G == 0 && d.B == 0)
                    {
                        GameSt = 0;
                        m_PlugMgr.Notify(EGameAction.GameStart);
                    }
                    else
                    {
                        messageDispatcher.GetBridge().TryStartGame();
                    }
                    break;
            }
            CheckIsBlackPop(now);
            for (int i = TaskList.Count - 1;i >= 0;i--)
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
            SendMsg.waitClean();
        }
        
        public void DoSettlement(int r = 2,int g = 0,bool sleep = true)
        {
            GameSt = 1;
            Interlocked.Exchange(ref LastState, r);
            m_Settlement.ShowSettlement(this,g);
            m_PlugMgr.Notify(EGameAction.GameStop);
            Log("Game end send msg fulsh b");
            SendMsg.waitClean();
            Log("Game end send msg fulsh e");
            if(sleep)Thread.Sleep(EndDelay);
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
            //messageDispatcher.Aoe4Bridge.ExecPrintMsg(text);
            SendMsg.SendMsg((short)EMsgTy.EventCue, new EventCueData() { Msg = text });
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
                it.Gold = (int)c;
                it.Progress = autoSpawn.GetSpawnProgress(id);
                if (it.Progress > 1.0) it.Progress = 1.0f;
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
            SendMsg.SendMessage((short)EMsgTy.ClearAllPlayer, null);
        }

        public int IsGameStart()
        {
            return LastState;
        }

        public int IsOverload()
        {
            return IsDispatch;
        }

        public void SendMsgToOverlay<T>(short id, T msg)
            where T : class
        {
            if (msg is byte[] s)
            {
                SendMsg.SendMessage(id,s);
                return;
            }
            SendMsg.SendMsg<T>(id, msg);
        }
    }
}