using System;
using BililiveDebugPlugin.InteractionGame;
using BililiveDebugPlugin.InteractionGame.Data;
using InteractionGame;
using InteractionGame.Context;
using InteractionGame.plugs.config;
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
        [ProtoBuf.ProtoMember(3)]
        public string MapName { get; set; }
    }
    public class AutoForceStopPlug : IPlug<EGameAction>
    {
        private DateTime _startTime = DateTime.Now;
        //Outter useful
        private DateTime _safeStartTime = DateTime.Now;
        public DateTime StartTime => _safeStartTime;
        private IConstConfig _config;
        public override void Init()
        {
            base.Init();
            Locator.Instance.Deposit(this);
            _config = Locator.Instance.Get<IConstConfig>();
        }
        public override void Tick()
        {
            if(DateTime.Now - _startTime > _config.OneTimesGameTime)
            {
                Locator.Instance.Get<IContext>().GetBridge().ForceFinish();
                _startTime = DateTime.Now;
            }
        }

        public override void Notify(EGameAction m)
        {
            switch (m)
            {
                case EGameAction.GameStart:
                    _safeStartTime = _startTime = DateTime.Now;
                    Locator.Instance.Get<IContext>().SendMsgToOverlay((short)EMsgTy.StartGame,new StartGameData()
                    {  
                        StartTime = _startTime,
                        GameTime = _config.OneTimesGameTime,
                        MapName = conf.Squad.SettingMgr.GetInstance().Get((int)conf.Squad.ESettingType.MapName).StrVal
                    });
                    break;
                case EGameAction.GameStop:
                    break;
            }
        }
    }
}