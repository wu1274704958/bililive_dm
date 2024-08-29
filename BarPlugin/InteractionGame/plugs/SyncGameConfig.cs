using System;
using System.Collections.Generic;
using BililiveDebugPlugin.InteractionGame.Data;
using conf.Squad;
using InteractionGame;
using InteractionGame.Context;
using InteractionGame.plugs.config;
using ProtoBuf;
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

        public override void Notify(EGameAction m)
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
            Locator.Instance.Get<IContext>().SendMsgToOverlay((short)EMsgTy.StartGame, new SyncGameConfigData()
            {
                GroupCount = Locator.Instance.Get<IGameState>().GroupCount,
                MapName = Locator.Instance.Get<IGameState>().MapName,
                StartTime = DateTime.Now.ToSecond(),
                GameTime = (int)Locator.Instance.Get<IConstConfig>().OneTimesGameTime.TotalSeconds
            });
        }
    }
}