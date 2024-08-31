using System;
using InteractionGame;
using InteractionGame.Context;
using InteractionGame.plugs.config;
using Utils;

namespace InteractionGameUtils
{
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
        }
        public override void Start()
        {
            base.Start();
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
                    break;
                case EGameAction.GameStop:
                    break;
            }
        }
    }
}