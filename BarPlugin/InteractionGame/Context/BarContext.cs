using BarPlugin.InteractionGame.plugs;
using BilibiliDM_PluginFramework;
using BililiveDebugPlugin.InteractionGame.Parser;
using BililiveDebugPlugin.InteractionGame.plugs;
using BililiveDebugPlugin.InteractionGame.Resource;
using BililiveDebugPlugin.InteractionGame.Settlement;
using BililiveDebugPlugin.InteractionGameUtils;
using conf;
using Interaction;
using InteractionGame.plugs.bar;
using InteractionGame.plugs.bar.config;
using InteractionGame.plugs.config;
using InteractionGame.Resource;
using System;
using System.Threading;
using Utils;

namespace InteractionGame.Context
{
    using MessageDispatcherType = MessageDispatcher<
        PlayerBirthdayParser<BarContext>,
        MsgGiftParser<BarContext>,
        DefAoe4Bridge<BarContext>,
        Aoe4BaoBingResMgr<BarContext>, BarContext>;

    public enum EMsgTy : short
    {
        None = 0,
        Settlement = 1,
        AddPlayer = 2,
        ClearAllPlayer = 3,
        UpdatePlayerGold = 4,
        ShowPlayerAction = 5,
        RemoveGroup = 6,
        UpdateGroupLevel = 7,
        StartGame = 8,
        SquadCountChanged = 9,
        ShowLargeTips = 10,
        SyncSquadConfig = 11,
        EventCue = 12,
        DefenseKeepHp = 13,
    }

    public class EventCueData
    {
        public string Msg;
    }
    public class GameEndData
    {
        public int winner;
    }
    public class BarContext : BaseContext
    {
        public MessageDispatcherType messageDispatcher { get; private set; }

        private EGameState gameState = EGameState.Ended;
        private DMPlugin dMPlugin = null;
        private OverlayCommPlug overlayComm;
        private GameCommPlug gameComm;
        private ISettlement<BarContext> settlement;

        public BarContext(DMPlugin dMPlugin)
        {
            this.dMPlugin = dMPlugin;
        }

        public override void OnInit()
        {
            base.OnInit();
            
            dMPlugin.ReceivedDanmaku += OnReceivedDanmaku;
            
            var _ = BililiveDebugPlugin.DB.DBMgr.Instance;
            ConfigMgr.Init();

            messageDispatcher = new MessageDispatcherType();
            
            //m_PlugMgr.Add(1000 * 30, new AutoForceStopPlug());
            m_PlugMgr.Add(1000, new SquadCapacityUIPlug());
            m_PlugMgr.Add(1000 * 60, new AutoDownLivePlug());
            m_PlugMgr.Add(-1, new SyncSquadConfig());
            m_PlugMgr.Add(-1, new SelfSaleGuardPlug());
            //m_PlugMgr.Add(300, new DefineKeepDamagedSpawnSquadPlug());
            //m_PlugMgr.Add(100, new EveryoneTowerPlug());
            //m_PlugMgr.Add(-1, new DbTransfarPlug());
            m_PlugMgr.Add(-1, new GameModeManager());
            m_PlugMgr.Add(-1, new ConstConfigPlug<ConstConfig>());
            m_PlugMgr.Add(100,overlayComm = new OverlayCommPlug());
            m_PlugMgr.Add(100,gameComm = new GameCommPlug());

            RegisterOnRecvGameMsg<NoArgs>("start", OnGameStart);
            RegisterOnRecvGameMsg<GameEndData>("end", OnGameEnd);

            settlement = new Aoe4Settlement<BarContext>();
            messageDispatcher.Init(this);
        }

        private void OnGameEnd(string arg1, object arg2)
        {
            if(arg2 is GameEndData data)
            {
                SendMsgToGame<NoArgs>("restart", null);
                gameState = EGameState.Ended;

                DoSettlement(data.winner);
            }
        }

        public void DoSettlement(int winner, bool sleep = true)
        {
            settlement.ShowSettlement(this, winner);
            messageDispatcher.Clear();
            m_PlugMgr.Notify(EGameAction.GameStop);
            if (sleep) 
                Thread.Sleep((int)Locator.Instance.Get<IConstConfig>().EndDelay);
        }

        private void OnGameStart(string arg1, object arg2)
        {
            gameState = EGameState.Started;
            m_PlugMgr.Notify(EGameAction.GameStart);
        }

        public override void OnStart()
        {
            base.OnStart();
            messageDispatcher.Start();     
        }

        private void OnReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
            if (gameState == EGameState.Started && (messageDispatcher?.Demand(e.Danmaku, e.Danmaku.MsgType) ?? false))
                messageDispatcher.Dispatch(e.Danmaku, e.Danmaku.MsgType);
        }

        public override void OnStop()
        {
            messageDispatcher.Stop();
            base.OnStop();
        }

        public override void OnDestroy()
        {
            gameComm = null;
            overlayComm = null;
            dMPlugin.ReceivedDanmaku -= OnReceivedDanmaku;
            BililiveDebugPlugin.DB.DBMgr.Instance.Dispose();
            base.OnDestroy();
        }

        public override IAoe4Bridge<BarContext> GetBridge<BarContext>()
        {
            return messageDispatcher.GetBridge() as IAoe4Bridge<BarContext>;
        }

        public override IDyMsgParser<BarContext> GetMsgParser<BarContext>()
        {
            return messageDispatcher.GetMsgParser() as IDyMsgParser<BarContext>;
        }

        public override IDyPlayerParser<BarContext> GetPlayerParser<BarContext>()
        {
            return messageDispatcher.GetPlayerParser() as IDyPlayerParser<BarContext>;
        }

        public override IResourceMgr<T> GetResourceMgr<T>()
        {
            return messageDispatcher.GetResourceMgr() as IResourceMgr<T>;
        }

        public override EGameState IsGameStart()
        {
            return gameState;
        }

        public override void Log(string text)
        {
            dMPlugin.Log(text);
        }

        public override void OnTick(float delta)
        {
            m_PlugMgr.Tick(delta);
        }

        public override void PrintGameMsg(string text)
        {
            SendMsgToOverlay<EventCueData>((short)EMsgTy.EventCue, new EventCueData { Msg = text });
        }

        public override void RegisterOnRecvGameMsg<T>(string key, Action<string, object> callback)
        {
            gameComm.RegisterOnRecvGameMsg<T>(key, callback);
        }

        public override void SendMsgToGame<T>(string id, T msg)
        {
            gameComm.SendMsgToGame(id, msg);
        }

        public override void SendMsgToOverlay<T>(short id, T msg)
        {
            overlayComm.SendMsgToOverlay<T>(id, msg);
        }

        public override void SendTestDanMu<T>(object sender, T dm)
        {
            if(dm is ReceivedDanmakuArgs msg)
            {
                OnReceivedDanmaku(sender, msg);
            }
        }
    }
}
