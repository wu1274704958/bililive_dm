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
        private IContext _context;
        public override void Init()
        {
            base.Init();
            Locator.Deposit(this);
        }
        public override void Start()
        {
            base.Start();
            _config = Locator.Get<IConstConfig>();
            _context = Locator.Get<IContext>();
        }
        public override void Tick()
        {
            if(_context.IsGameStart() == EGameState.Started && DateTime.Now - _startTime > _config.OneTimesGameTime)
            {
                Locator.Get<IContext>().GetBridge().ForceFinish();
                _startTime = DateTime.Now;
            }
        }

        public override void OnReceiveNotify(EGameAction m,object args = null)
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