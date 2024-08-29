using InteractionGame.Resource;
using System;

namespace InteractionGame
{
    public enum EGameState
    {
        Started,
        Ended,
        Loading
    }
    public interface IContext
    {
        void Log(string text);
        void OnInit();
        void OnDestroy();
        void OnStart();
        void OnStop();
        void OnTick(float delta);
        void PrintGameMsg(string text);
        void SendTestDanMu<T>(object sender, T dm);
        EGameState IsGameStart();
        void SendMsgToOverlay<T>(short id, T msg) where T : class;
        void SendMsgToGame<T>(string id, T msg) where T : class;
        void RegisterOnRecvGameMsg<T>(string key, Action<string, object> callback);
        IGameBridge GetBridge();
        IDyMsgParser GetMsgParser();
        IDyPlayerParser GetPlayerParser();
        IResourceMgr GetResourceMgr();
    }

    public interface ILocalMsgDispatcher< MSG, MSG_TY>
    {
        void Start();
        void OnStartGame();
        bool Demand(MSG msg, MSG_TY barType);
        void Dispatch(MSG msg, MSG_TY barType);
        void Stop();
        void Init(IContext it);
        IGameBridge GetBridge();
        IDyMsgParser GetMsgParser();
        IDyPlayerParser GetPlayerParser();
        IResourceMgr GetResourceMgr();
        void Clear();
        void SetIsEmit(bool v);
    }
}
