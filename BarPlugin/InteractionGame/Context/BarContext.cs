using BarPlugin.InteractionGame.plugs.bar;
using BilibiliDM_PluginFramework;
using BililiveDebugPlugin.InteractionGame.Parser;
using BililiveDebugPlugin.InteractionGame.plugs;
using BililiveDebugPlugin.InteractionGame.Resource;
using BililiveDebugPlugin.InteractionGame.Settlement;
using conf;
using InteractionGame.gameBridge;
using InteractionGame.Parser.bar;
using InteractionGame.plugs.bar;
using InteractionGame.plugs.bar.config;
using InteractionGame.plugs.config;
using InteractionGame.Resource;
using InteractionGameUtils;
using System;
using System.Threading;
using Utils;

namespace InteractionGame.Context
{
    using MessageDispatcherType = MessageDispatcher<
        PlayerMsgParser,
        MsgGiftParser<BarContext>,
        BarBridge,
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
    public static class EGameMsg
    {
        public static readonly string BPreStart = "preStart";
        public static readonly string BStart = "start";
        public static readonly string BEnd = "end";
        public static readonly string BUnitReward = "unitReward";
        public static readonly string BUnitDestroyed = "unitDestroyed";

        public static readonly string SJoin = "join";
        public static readonly string SSpawn = "spawn";
        public static readonly string SForceFinish = "forceFinish";

    }

    public class EventCueData
    {
        public string Msg;
    }
    public class GameEndData
    {
        public int winner;
    }
    public class GamePreStartData
    {
        public int teamCount;
        public string mapName;
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
            
            m_PlugMgr.Add(1000 * 30, new AutoForceStopPlug());
            m_PlugMgr.Add(1000, new SquadCapacityUIPlug());
            m_PlugMgr.Add(1000 * 60, new AutoDownLivePlug());
            m_PlugMgr.Add(-1, new SyncGameConfig());
            m_PlugMgr.Add(-1, new SelfSaleGuardPlug());
            //m_PlugMgr.Add(300, new DefineKeepDamagedSpawnSquadPlug());
            //m_PlugMgr.Add(100, new EveryoneTowerPlug());
            //m_PlugMgr.Add(-1, new DbTransfarPlug());
            m_PlugMgr.Add(-1, new GameModeManager());
            m_PlugMgr.Add(-1, new ConstConfig());
            m_PlugMgr.Add(100,overlayComm = new OverlayCommPlug());
            m_PlugMgr.Add(100,gameComm = new GameCommPlug());
            m_PlugMgr.Add(-1, new BarGameState());
            m_PlugMgr.Add(-1, new BarSquadMgr());
            m_PlugMgr.Add(-1, new GiftMgr());

            RegisterOnRecvGameMsg<GamePreStartData>(EGameMsg.BPreStart, OnGamePreStart);
            RegisterOnRecvGameMsg<NoArgs>(EGameMsg.BStart, OnGameStart);
            RegisterOnRecvGameMsg<GameEndData>(EGameMsg.BEnd, OnGameEnd);

            settlement = new BarSettlement<BarContext>();
            messageDispatcher.Init(this);
        }

        private void OnGamePreStart(string arg1, object arg2)
        {
            m_PlugMgr.Notify(EGameAction.GamePreStart);
        }

        private void OnGameStart(string arg1, object arg2)
        {
            gameState = EGameState.Started;
            m_PlugMgr.Notify(EGameAction.GameStart);
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

        public override IGameBridge GetBridge()
        {
            return messageDispatcher.GetBridge();
        }

        public override IDyMsgParser GetMsgParser()
        {
            return messageDispatcher.GetMsgParser();
        }

        public override IDyPlayerParser GetPlayerParser()
        {
            return messageDispatcher.GetPlayerParser();
        }

        public override IResourceMgr GetResourceMgr()
        {
            return messageDispatcher.GetResourceMgr();
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
