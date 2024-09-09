using System;
using InteractionGame;
using InteractionGame.Context;
using InteractionGame.plugs.config;
using Utils;

namespace BililiveDebugPlugin.InteractionGame.plugs
{
    public class SyncGameConfigData
    {
        public string MapName;
        public int GroupCount;
        public int StartTime;
        public int GameTime;
    }

    public class SyncGameConfig : IPlug<EGameAction>
    {
        public override void Tick()
        {
            
        }

        public override void OnReceiveNotify(EGameAction m,object args = null)
        {
            switch (m)
            {
                case EGameAction.GameStart:
                    SendMsg();
                    break;
                case EGameAction.GameStop:
                    break;
            }
        }

        public override void Init()
        {
            base.Init();
            
        }

        public override void Start()
        {
            base.Start();
        }

        public void SendMsg()
        {
            Locator.Get<IContext>().SendMsgToOverlay((short)EMsgTy.StartGame, new SyncGameConfigData()
            {
                GroupCount = Locator.Get<IGameState>().GroupCount,
                MapName = Locator.Get<IGameState>().MapName,
                StartTime = DateTime.Now.ToSecond(),
                GameTime = (int)Locator.Get<IConstConfig>().OneTimesGameTime.TotalSeconds
            });
        }
    }
}