using System;
using BililiveDebugPlugin.InteractionGame;
using BililiveDebugPlugin.InteractionGame.Data;
using Utils;

namespace BililiveDebugPlugin.InteractionGameUtils
{
    [ProtoBuf.ProtoContract]
    public class StartGameData
    {
        [ProtoBuf.ProtoMember(1)]
        public DateTime StartTime { get; set; }
        [ProtoBuf.ProtoMember(2)]
        public TimeSpan GameTime { get; set; }
    }
    public class AutoForceStopPlug : IPlug<EGameAction>
    {
        public DateTime _startTime = DateTime.Now;
        public override void Tick()
        {
            if(DateTime.Now - _startTime > Aoe4DataConfig.OneTimesGameTime)
            {
                Locator.Instance.Get<DebugPlugin>().messageDispatcher.GetBridge().AppendExecCode("ForceStopGame();");
            }
        }

        public override void Notify(EGameAction m)
        {
            switch (m)
            {
                case EGameAction.GameStart:
                    _startTime = DateTime.Now;
                    Locator.Instance.Get<DebugPlugin>().SendMsg.SendMsg((short)EMsgTy.StartGame,new StartGameData(){  StartTime = _startTime,GameTime = Aoe4DataConfig.OneTimesGameTime});
                    break;
                case EGameAction.GameStop:
                    break;
            }
        }
    }
}