using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace BililiveDebugPlugin.InteractionGameUtils
{
    public interface ISpawnFallback
    {
        void Fallback();
        int GetType();
        void SetPercentage(double percentage);
    }
    
    public class SpawnResult
    {
        public ESpawnResult Result;
        public double Percentage;
        public ISpawnFallback Fallback;
    }
    public enum ESpawnResult
    {
        SpawnedAll,
        SpawnedSome,
    }
    public abstract class ISpawnSquadAction
    {
        public double _Percentage { get;protected set; } = 1;
        protected abstract double SpawnInternal(int max);
        protected ISpawnFallback _fallback;

        public virtual SpawnResult Spawn(int max)
        {
            var res = SpawnInternal(max);
            if(res > 0)OnSpawned(res);
            _Percentage -= res;
            if (_Percentage < 0.0001)
            {
                OnSpawnedAll();
                return new SpawnResult() { Result = ESpawnResult.SpawnedAll, Percentage = 1, Fallback = null };
            }
            else
                return new SpawnResult() { Result = ESpawnResult.SpawnedSome, Percentage = _Percentage, Fallback = GetFallback() };
        }

        protected abstract void OnSpawnedAll();

        protected abstract void OnSpawned(double res);

        protected abstract ISpawnFallback GenerateFallback();

        public ISpawnFallback GetFallback()
        {
            if(_fallback == null)
                _fallback = GenerateFallback();
            _fallback?.SetPercentage(_Percentage);
            return _fallback;
        }
        public abstract int GetGroup();
        public abstract int GetCount();
        public abstract void OnDestroy();
        public abstract object GetUser();
    }
    public class SpawnSquadActionBound
    {
        public ISpawnSquadAction Action;
        public ISpawnFallback Fallback;

        public SpawnSquadActionBound(ISpawnSquadAction action, ISpawnFallback fallback)
        {
            Action = action;
            Fallback = fallback;
        }
    }
    
    public abstract class ISpawnSquadQueue
    {
        protected ConcurrentQueue<SpawnSquadActionBound> Actions =
            new ConcurrentQueue<SpawnSquadActionBound>();

        public void AppendAction(ISpawnSquadAction action)
        {
            var group = action.GetGroup();
            var remaining = RemainingQuantity(group);
            if(remaining <= 0 && IsGroupLimit(action.GetCount(),remaining))
                Actions.Enqueue(new SpawnSquadActionBound(action, action.GetFallback()));
            else
            {
                SpawnNew(action,remaining);
            }
        }

        private void SpawnNew(ISpawnSquadAction action,int remaining)
        {
            var res = action.Spawn(remaining);
            if (res.Result == ESpawnResult.SpawnedSome)
            {
                Actions.Enqueue(new SpawnSquadActionBound(action, res.Fallback));
            }
            else
            {
                action.OnDestroy();
            }
        }
        
        public void Tick()
        {
            int remaining = 0;
            while (Actions.TryPeek(out var action) && 
                   (remaining = RemainingQuantity(action.Action.GetGroup())) > 0 
                   && !IsGroupLimit(action.Action.GetCount(),remaining))
            {
                SpawnInQueue(action,remaining);
            }
        }

        private bool SpawnInQueue(SpawnSquadActionBound action,int remaining)
        {
            var res = action.Action.Spawn(remaining);
            if (res.Result == ESpawnResult.SpawnedSome)
            {
                action.Fallback = res.Fallback;
            }else if (res.Result == ESpawnResult.SpawnedAll)
            {
                action.Action.OnDestroy();
                Actions.TryDequeue(out _);
                return true;
            }
            return false;
        }

        protected abstract bool IsGroupLimit(int count,int remaining);
        protected abstract int RemainingQuantity(int group);

        public virtual void OnClear()
        {
            while (Actions.TryDequeue(out var action))
            {
                action.Fallback?.Fallback();
            }
        }
    

    }
}