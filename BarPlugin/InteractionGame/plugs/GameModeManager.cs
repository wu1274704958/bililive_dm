using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using BililiveDebugPlugin.InteractionGame.mode;
using InteractionGame.Context;
using Utils;

namespace BililiveDebugPlugin.InteractionGame.plugs
{
    public class GameModeManager : IPlug<EGameAction>
    {
        protected Dictionary<Type, IGameMode> GameModes = new Dictionary<Type, IGameMode>()
        {
            {typeof(BaseGameMode),new BaseGameMode()},
            {typeof(BossGameMode),new BossGameMode()}
        };
        protected Type DefGameMode = typeof(BaseGameMode);
        protected Type CurrentGameMode = null;
        protected (Type,object) NextGameMode = (null,null);
        public override void Init()
        {
            base.Init();
            foreach (var gm in GameModes)
                gm.Value.Init();
            Locator.Deposit(this);
        }

        public override void Start()
        {
            base.Start();
            foreach (var gm in GameModes)
                gm.Value.Start();
            ChangeMode(DefGameMode,null);
        }
        public void ChangeMode<T>(object args)
        where T:IGameMode
        {
            ChangeMode(typeof(T),args);
        }
        public void SetNextGameMode<T>(object args)
            where T:IGameMode
        {
            NextGameMode = (typeof(T), args);
        }

        public T GetGameMode<T>()
        where T:IGameMode
        {
            if (GameModes.TryGetValue(typeof(T), out var v))
            {
                return (T)v;
            }
            return default(T);
        }
        public void ChangeMode(Type gameMode,object args)
        {
            if (CurrentGameMode != null)
            {
                QuitGameMode(CurrentGameMode);
                CurrentGameMode = null;
            }
            if (GameModes.TryGetValue(gameMode, out var v))
            {
                v.OnResume(args);
                CurrentGameMode = gameMode;
                Locator.DepositOrExchange(v);
                if (v.NextBackToDefault())
                    NextGameMode = (DefGameMode,null);
            }
        }

        private void QuitGameMode(Type gameMode)
        {
            if (GameModes.TryGetValue(gameMode, out var v))
            {
                v.OnPause();
            }
        }

        public override void Dispose()
        {
            foreach (var gm in GameModes)
                gm.Value.Stop();
            foreach (var gm in GameModes)
                gm.Value.Dispose();
            base.Dispose();
        }

        public override void Tick()
        {
            
        }

        public override void Notify(EGameAction m)
        {
            switch (m)
            {
                case EGameAction.GameStart:
                    if (NextGameMode.Item1 != null)
                    {
                        var next = NextGameMode;
                        NextGameMode = (null, null);
                        ChangeMode(next.Item1, next.Item2);
                    }
                    break;
                case EGameAction.GameStop:
                    break;
            }
        }
    }
}