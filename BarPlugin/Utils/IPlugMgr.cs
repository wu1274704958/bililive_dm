using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Utils
{
    public abstract class IPlug<M>
    {
        private int State = 0;
        public bool IsInit => State >= 1;
        public bool IsStart => State == 2;

        public virtual void Init()
        {
            Debug.Assert(State == 0);
            Interlocked.Exchange(ref State, 1);
        }

        public abstract void Tick();

        public virtual void Dispose()
        {
            Debug.Assert(State == 1);
            Interlocked.Exchange(ref State, 0);
        }

        public abstract void Notify(M m);

        public virtual void Start()
        {
            Debug.Assert(State == 1);
            Interlocked.CompareExchange(ref State, 2, 1);
        }

        public virtual void Stop()
        {
            Debug.Assert(State == 2);
            Interlocked.CompareExchange(ref State, 1, 2);
        }
    }

    public class PlugCxt<M>
    {
        public List<IPlug<M>> Plugs = new List<IPlug<M>>();
        public DateTime LastTick = DateTime.Now;
    }

    public class PlugMgr<M> : IPlug<M>
    {
        
        protected ConcurrentDictionary<int, PlugCxt<M>> _plugs = new ConcurrentDictionary<int, PlugCxt<M>>();
        protected ConcurrentDictionary<int, IPlug<M>> _NotTickPlugs = new ConcurrentDictionary<int, IPlug<M>>();

        public override void Init()
        {
            base.Init();
            foreach (var plug in _plugs)
            {
                foreach (var p in plug.Value.Plugs)
                {
                    if(!p.IsInit)
                        p.Init();
                }
            }
            foreach (var plug in _NotTickPlugs)
            {
                if(!plug.Value.IsInit)
                    plug.Value.Init();
            }
        }
        
        public override void Start()
        {
            base.Start();
            foreach (var plug in _plugs)
            {
                foreach (var p in plug.Value.Plugs)
                {
                    if(!p.IsStart)
                        p.Start();
                }
            }
            foreach (var plug in _NotTickPlugs)
            {
                if(!plug.Value.IsStart)
                    plug.Value.Start();
            }
        }

        public override void Stop()
        {
            foreach (var plug in _plugs)
            {
                foreach (var p in plug.Value.Plugs)
                {
                    if(p.IsStart)
                        p.Stop();
                }
            }
            foreach (var plug in _NotTickPlugs)
            {
                if(plug.Value.IsStart)
                    plug.Value.Stop();
            }
            base.Stop();
        }

        public override void Dispose()
        {
            foreach (var plug in _plugs)
            {
                foreach (var p in plug.Value.Plugs)
                {
                    if(p.IsInit)
                        p.Dispose();
                }
            }
            foreach (var plug in _NotTickPlugs)
            {
                if(plug.Value.IsInit)
                    plug.Value.Dispose();
            }
            base.Dispose();
        }

        public override void Notify(M m)
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

        public override void Tick()
        {
            throw new NotImplementedException();
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
                    if (IsStart && !plug.IsStart)
                        plug.Start();
                }
                return;
            }
            var cxt = _plugs.GetOrAdd(second, _ => new PlugCxt<M>());
            cxt.Plugs.Add(plug);
            if (IsInit && !plug.IsInit)
                plug.Init();
            if (IsStart && !plug.IsStart)
                plug.Start();
        }

        public bool Remove(int second, IPlug<M> plug)
        {
            if(second < 0)
            {
                var res = _NotTickPlugs.TryRemove(plug.GetHashCode(), out var p);
                if(p.IsStart)
                    p.Stop();
                if(p.IsInit)
                    p.Dispose();
                return res;
            }
            if (!_plugs.TryGetValue(second, out PlugCxt<M> cxt)) return false;
            for (int i = 0; i < cxt.Plugs.Count; i++)
            {
                if (cxt.Plugs[i] == plug)
                {
                    if (cxt.Plugs[i].IsStart)
                        cxt.Plugs[i].Stop();
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