﻿using InteractionGame;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BililiveDebugPlugin.InteractionGame.Data;
using Utils;

namespace BililiveDebugPlugin.InteractionGame.Resource
{
    public class Aoe4BaoBingResMgr<C> : IResourceMgr<C>
        where C : class, IContext
    {
        ConcurrentDictionary<string, TimeLinerInteger> AutoAddMap = new ConcurrentDictionary<string, TimeLinerInteger>();
        public override void AddAutoResourceById(string id, float addFactor = 1f)
        {
            TimeLinerInteger v = null;
            AutoAddMap.TryAdd(id, v = new TimeLinerInteger(Aoe4DataConfig.BaoBingOriginResource, Aoe4DataConfig.BaoBingAddResFactor, Aoe4DataConfig.AutoGoldLimit));
            v.AddFactor = addFactor;
        }

        public override void AddResource(string id, double c)
        {
            if(AutoAddMap.TryGetValue(id, out var v))
            {
                lock (v)
                {
                    v.Append(c);
                }
            }
        }

        public override void ChangeAutoResourceAddFactor(string id, float addFactor)
        {
            if (AutoAddMap.TryGetValue(id, out var v))
            {
                lock (v)
                {
                    v.AddFactor = addFactor;
                }
            }
        }

        public override void AddAutoResourceAddFactor(string id, float addFactor)
        {
            if (AutoAddMap.TryGetValue(id, out var v))
            {
                lock (v)
                {
                    v.AddFactor += addFactor;
                }
            }
        }

        public override void Foreach(int ty, Action<string, double> action)
        {
            foreach(var it in AutoAddMap)
            {
                lock (it.Value)
                {
                    action.Invoke(it.Key, it.Value.val);
                }
            }
        }

        public override double GetResource(string id, int ty = 0)
        {
            if(AutoAddMap.TryGetValue(id,out var v))
            {
                lock (v) {
                    return v.val;
                }
            }
            return 0;
        }

        public override int GetVillagerCount(string id)
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

        public override void RemoveAllVillagers(string id)
        {
            
        }

        public override bool RemoveResource(string id, double r)
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

        public override bool SpawnVillager(string id, int num)
        {
            return false;
        }
    }
}
