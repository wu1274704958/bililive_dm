using InteractionGame.Resource;
using System;
using System.Diagnostics;
using System.Threading;
using Utils;

namespace InteractionGame.Context
{
    public enum EGameAction
    {
        GamePreStart,
        GameStart,
        PreSettlement,
        GameStop,
        ConfigReload,
    }
    public abstract class BaseContext : IContext
    {
        private int State = 0;
        public bool IsInit => State >= 1;
        public bool IsStart => State == 2;
        public PlugMgr<EGameAction> m_PlugMgr { get; protected set; } = new PlugMgr<EGameAction>();

        public abstract IGameBridge GetBridge();
        public abstract IDyMsgParser GetMsgParser();
        public abstract IDyPlayerParser GetPlayerParser();
        public abstract IResourceMgr GetResourceMgr();

        public abstract EGameState IsGameStart();

        public abstract void Log(string text);

        public virtual void OnInit()
        {
            Debug.Assert(State == 0);
            Interlocked.Exchange(ref State, 1);

            m_PlugMgr.Init();
            Locator.Deposit<IContext>(this);
            Locator.Deposit<PlugMgr<EGameAction>>(m_PlugMgr);
        }

        public virtual void OnDestroy()
        {
            Locator.Remove<IContext>();
            Locator.Remove<PlugMgr<EGameAction>>();
            m_PlugMgr.Dispose();
            Debug.Assert(State == 1);
            Interlocked.Exchange(ref State, 0);
        }

        public virtual void OnStart()
        {
            Debug.Assert(State == 1);
            Interlocked.CompareExchange(ref State, 2, 1);
            m_PlugMgr.Start();
        }

        public virtual void OnStop()
        {
            m_PlugMgr.Stop();
            Debug.Assert(State == 2);
            Interlocked.CompareExchange(ref State, 1, 2);
        }

        public abstract void OnTick(float delta);

        public abstract void PrintGameMsg(string text);

        public abstract void SendMsgToOverlay<T>(short id, T msg) where T : class;

        public abstract void SendTestDanMu<T>(object sender, T dm);

        public abstract void SendMsgToGame<T>(string id, T msg) where T : class;
        public abstract void RegisterOnRecvGameMsg<T>(string key, Action<string, object> callback);
    }
}
