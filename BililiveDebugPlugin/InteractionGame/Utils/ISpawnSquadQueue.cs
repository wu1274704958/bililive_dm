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
        protected ConcurrentDictionary<int, ConcurrentQueue<SpawnSquadActionBound>> Actions =
            new ConcurrentDictionary<int, ConcurrentQueue<SpawnSquadActionBound>>();

        public void AppendAction(ISpawnSquadAction action)
        {
            var group = action.GetGroup();
            var remaining = RemainingQuantity(group);
            if (remaining <= 0 && IsGroupLimit(action.GetCount(), remaining))
            {
                ConcurrentQueue<SpawnSquadActionBound> queue = GetQueue(group);
                queue.Enqueue(new SpawnSquadActionBound(action, action.GetFallback()));
            }
            else
            {
                SpawnNew(action,remaining);
            }
        }
        
        private ConcurrentQueue<SpawnSquadActionBound> GetQueue(int group)
        {
            if (!Actions.TryGetValue(group, out var queue))
                if(!Actions.TryAdd(group,queue = new ConcurrentQueue<SpawnSquadActionBound>()))
                    queue = Actions[group];
            return queue;
        }

        private void SpawnNew(ISpawnSquadAction action,int remaining)
        {
            var res = action.Spawn(remaining);
            if (res.Result == ESpawnResult.SpawnedSome)
            {
                GetQueue(action.GetGroup()).Enqueue(new SpawnSquadActionBound(action, res.Fallback));
            }
            else
            {
                action.OnDestroy();
            }
        }

        public abstract bool IsGameEnd();
        
        public void Tick()
        {
            int remaining = 0;
            foreach (var queue in Actions)
            {
                while (queue.Value.TryPeek(out var action) && 
                       (remaining = RemainingQuantity(action.Action.GetGroup())) > 0 
                       && !IsGroupLimit(action.Action.GetCount(),remaining))
                {
                    if (IsGameEnd())
                        break;

                    if (!SpawnInQueue(action, remaining))
                        break;
                }
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
                var group = action.Action.GetGroup();
                action.Action.OnDestroy();
                GetQueue(group).TryDequeue(out _);
                return true;
            }
            return false;
        }

        protected abstract bool IsGroupLimit(int count,int remaining);
        protected abstract int RemainingQuantity(int group);

        public virtual void OnClear()
        {
            foreach (var queue in Actions)
            {
                while (queue.Value.TryDequeue(out var action))
                {
                    action.Fallback?.Fallback();
                }
            }
        }
    

    }
}