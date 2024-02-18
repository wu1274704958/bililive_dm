using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Utils
{
    public abstract class IPlug<M>
    {
        private int State = 0;
        public bool IsInit => State == 1;

        public virtual void Init()
        {
            Interlocked.Exchange(ref State, 1);
        }

        public abstract void Tick();

        public virtual void Dispose()
        {
            Interlocked.Exchange(ref State, 0);
        }

        public abstract void Notify(M m);
    }

    public class PlugCxt<M>
    {
        public List<IPlug<M>> Plugs = new List<IPlug<M>>();
        public DateTime LastTick = DateTime.Now;
    }

    public class PlugMgr<M>
    {
        public bool IsInit => State == 1;
        private int State = 0;
        protected ConcurrentDictionary<int, PlugCxt<M>> _plugs = new ConcurrentDictionary<int, PlugCxt<M>>();
        protected ConcurrentDictionary<int, IPlug<M>> _NotTickPlugs = new ConcurrentDictionary<int, IPlug<M>>();

        public virtual void Init()
        {
            Interlocked.Exchange(ref State, 1);
            foreach (var plug in _plugs)
            {
                foreach (var p in plug.Value.Plugs)
                {
                    p.Init();
                }
            }
            foreach (var plug in _NotTickPlugs)
            {
                plug.Value.Init();
            }
        }

        public virtual void Dispose()
        {
            Interlocked.Exchange(ref State, 0);
            foreach (var plug in _plugs)
            {
                foreach (var p in plug.Value.Plugs)
                {
                    p.Dispose();
                }
            }
            foreach (var plug in _NotTickPlugs)
            {
                plug.Value.Dispose();
            }
        }

        public void Notify(M m)
        {
            foreach (var plug in _plugs)
            {
                foreach (var p in plug.Value.Plugs)
                {
                    p.Notify(m);
                }
            }
            foreach (var plug in _NotTickPlugs)
            {
                plug.Value.Notify(m);
            }
        }

        public void Tick(float delta)
        {
            var now = DateTime.Now;
            foreach (var plug in _plugs)
            {
                var cxt = plug.Value;
                if (cxt.Plugs.Count == 0)
                    continue;
                if (now - cxt.LastTick > TimeSpan.FromMilliseconds(plug.Key))
                {
                    cxt.LastTick = now;
                    foreach (var p in cxt.Plugs)
                    {
                        p.Tick();
                    }
                }
            }
        }

        public void Add(int second, IPlug<M> plug)
        {
            if (second < 0)
            {
                if(!_NotTickPlugs.ContainsKey(plug.GetHashCode()))
                {
                    _NotTickPlugs.TryAdd(plug.GetHashCode(), plug);
                    if (IsInit && !plug.IsInit)
                        plug.Init();
                }
                return;
            }
            var cxt = _plugs.GetOrAdd(second, _ => new PlugCxt<M>());
            cxt.Plugs.Add(plug);
            if (IsInit && !plug.IsInit)
                plug.Init();
        }

        public bool Remove(int second, IPlug<M> plug)
        {
            if(second < 0)
            {
                return _plugs.TryRemove(plug.GetHashCode(), out _);
            }
            if (!_plugs.TryGetValue(second, out PlugCxt<M> cxt)) return false;
            for (int i = 0; i < cxt.Plugs.Count; i++)
            {
                if (cxt.Plugs[i] == plug)
                {
                    if (cxt.Plugs[i].IsInit)
                        cxt.Plugs[i].Dispose();
                    cxt.Plugs.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }
    }
}