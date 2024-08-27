using Interaction;
using InteractionGame.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        IAoe4Bridge<T> GetBridge<T>() where T: class,IContext;
        IDyMsgParser<T> GetMsgParser<T>() where T:class,IContext;
        IDyPlayerParser<T> GetPlayerParser<T>() where T : class, IContext;
        IResourceMgr<T> GetResourceMgr<T>() where T : class, IContext;
    }

    public interface ILocalMsgDispatcher<IT, MSG, MSG_TY>
        where IT : class, IContext
    {
        void Start();
        void OnStartGame();
        bool Demand(MSG msg, MSG_TY barType);
        void Dispatch(MSG msg, MSG_TY barType);
        void Stop();
        void Init(IT it);
        IAoe4Bridge<IT> GetBridge();
        IDyMsgParser<IT> GetMsgParser();
        IDyPlayerParser<IT> GetPlayerParser();
        IResourceMgr<IT> GetResourceMgr();
        void Clear();
        void SetIsEmit(bool v);
    }
}
