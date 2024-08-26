using BililiveDebugPlugin.InteractionGame;
using InteractionGame;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Utils;

namespace BililiveDebugPlugin.InteractionGameUtils
{
    public interface ISpawnFallback
    {
        void Fallback();
        int Type();
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
#if DEBUG
        private IContext _context;
        private IContext context => _context == null ? (_context = Locator.Instance.Get<IContext>()) : _context;
#endif

        public virtual SpawnResult Spawn(int max)
        {
            var res = SpawnInternal(max);
#if DEBUG
            var ud = GetUser() as global::InteractionGame.UserData;
            context.Log($"{ud.Name}[Count = {GetCount()}] [Percentage = {_Percentage}] [Curr = {res}]");
#endif
            if (res > 0)OnSpawned(res);
            _Percentage -= res;
            if (GetCount() <= 0)
            {
                OnSpawnedAll();
                return new SpawnResult() { Result = ESpawnResult.SpawnedAll, Percentage = 0, Fallback = null };
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
        public abstract int GetPriority();
        public abstract ISpawnSquadAction Merge(int multiple);
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
        protected ConcurrentDictionary<int, LinkedList<SpawnSquadActionBound>> Actions =
            new ConcurrentDictionary<int, LinkedList<SpawnSquadActionBound>>();
        protected DateTime _tickSpawnTime = DateTime.Now;
        protected static readonly TimeSpan TickSpawnInterval = TimeSpan.FromMilliseconds(1000);

        public virtual void AppendAction(ISpawnSquadAction action)
        {
            
            var a = new LinkedList<int>();
            //var gs = Locator.Instance.Get<Aoe4GameState>();
            var group = action.GetGroup();
            //if (gs.HasCheckSquadCountTask(group, this))
            //    return;
            
                var remaining = RemainingQuantity(group);
                var count = action.GetCount();
                bool limit = remaining <= 0 ? true : IsGroupLimit(count, remaining);
                //Locator.Instance.Get<IContext>().Log($"AppendAction g={group},remaining={remaining},count={count},limit={limit}");
                if (limit)
                {
                    AppendActionByPriority(action, action.GetPriority());
                }
                else
                {
                    SpawnNew(action, remaining);
                }
            
        }

        private void AppendActionByPriority(ISpawnSquadAction action, int priority,ISpawnFallback fallback = null)
        {
            if(fallback == null)
                fallback = action.GetFallback();
            var obj = new SpawnSquadActionBound(action, fallback);
            LinkedList<SpawnSquadActionBound> queue = GetQueue(action.GetGroup());
            lock (queue)
            {
                if(queue.Count == 0)
                {
                    queue.AddLast(obj);
                    return;
                }
                var curr = queue.First;
                for(;;)
                {
                    if (curr == null || priority > curr.Value.Action.GetPriority())
                        break;
                    curr = curr.Next;
                }
                if(curr == null)
                    queue.AddLast(obj);
                else
                {
                    queue.AddBefore(curr, obj);
                }
            }
        }

        private LinkedList<SpawnSquadActionBound> GetQueue(int group)
        {
            if (!Actions.TryGetValue(group, out var queue))
                return Actions[group] = new LinkedList<SpawnSquadActionBound>();
            return queue;
        }

        private void SpawnNew(ISpawnSquadAction action,int remaining)
        {
            var res = action.Spawn(remaining);
            if (res.Result == ESpawnResult.SpawnedSome)
            {
                AppendActionByPriority(action,action.GetPriority(),res.Fallback);
            }
            else
            {
                action.OnDestroy();
            }
        }

        public abstract bool IsGameEnd();
        
        public virtual void Tick()
        {
            var now = DateTime.Now;
            var canSpawn = (now - _tickSpawnTime) >= TickSpawnInterval;
            
            if (!canSpawn || IsGameEnd()) 
                return;
            
            _tickSpawnTime = now;

            foreach (var queue in Actions)
            {
                lock (queue.Value)
                {
                    if (queue.Value.First != null)
                    {
                        PeekQueueAndSpawn(queue.Key, queue.Value.First.Value);
                    }
                }
            }
        }

        protected virtual void PeekQueueAndSpawn(int group, SpawnSquadActionBound action)
        {
            int remaining = 0;
            int count = 0;
            bool limit = false;
            if (!IsGameEnd() && (remaining = RemainingQuantity(group)) > 0 &&
            !(limit = IsGroupLimit(count = action.Action.GetCount(), remaining)))
            {
                //Locator.Instance.Get<IContext>().Log($"Squad Queue tick g={group},remaining={remaining},count={count},limit={limit},{action.Action.GetType().Name}");
                SpawnInQueue(action, remaining);
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
                var queue = GetQueue(group);
                lock(queue) { queue.Remove(action); };
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
                lock (queue.Value)
                {
                    foreach(var action  in queue.Value)
                    {
                        action.Fallback?.Fallback();
                        action.Action?.OnDestroy();
                    }
                    queue.Value.Clear();
                }
            }
        }
    

    }
}