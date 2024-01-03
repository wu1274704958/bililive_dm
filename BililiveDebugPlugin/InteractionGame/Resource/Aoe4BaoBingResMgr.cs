using InteractionGame;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BililiveDebugPlugin.InteractionGame.Data;

namespace BililiveDebugPlugin.InteractionGame.Resource
{
    public class Aoe4BaoBingResMgr<C> : IResourceMgr<C>
        where C : class, IContext
    {
        ConcurrentDictionary<long, Utils.TimeLinerInteger> AutoAddMap = new ConcurrentDictionary<long, Utils.TimeLinerInteger>();
        public override void AddAutoResourceById(long id, float addFactor = 1f)
        {
            Utils.TimeLinerInteger v = null;
            AutoAddMap.TryAdd(id, v = new Utils.TimeLinerInteger(Aoe4DataConfig.BaoBingOriginResource, Aoe4DataConfig.BaoBingAddResFactor, Aoe4DataConfig.AutoGoldLimit));
            v.AddFactor = addFactor;
        }

        public override void AddResource(long id, int c)
        {
            if(AutoAddMap.TryGetValue(id, out var v))
            {
                lock (v)
                {
                    v.Append(c);
                }
            }
        }

        public override void ChangeAutoResourceAddFactor(long id, float addFactor)
        {
            if (AutoAddMap.TryGetValue(id, out var v))
            {
                lock (v)
                {
                    v.AddFactor = addFactor;
                }
            }
        }

        public override void Foreach(int ty, Action<long, int> action)
        {
            foreach(var it in AutoAddMap)
            {
                lock (it.Value)
                {
                    action.Invoke(it.Key, it.Value.val);
                }
            }
        }

        public override int GetResource(long id, int ty = 0)
        {
            if(AutoAddMap.TryGetValue(id,out var v))
            {
                lock (v) {
                    return v.val;
                }
            }
            return 0;
        }

        public override int GetVillagerCount(long id)
        {
            return 0;
        }

        public override void OnClear()
        {
            AutoAddMap.Clear();
        }

        public override void OnTick(float delta)
        {
            
        }

        public override int PlayerCount()
        {
            return AutoAddMap.Count;
        }

        public override void RemoveAllVillagers(long id)
        {
            
        }

        public override bool RemoveResource(long id, int r)
        {
            if(AutoAddMap.TryGetValue(id,out var v))
            {
                lock (v) {
                    if (v.val >= r)
                    {
                        v.Sub(r);
                        return true;
                    }
                }
            }
            return false;
        }

        public override bool SpawnVillager(long id, int num)
        {
            return false;
        }
    }
}
