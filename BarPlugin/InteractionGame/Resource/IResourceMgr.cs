using InteractionGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace InteractionGame.Resource
{
    public abstract class IResourceMgr
    {
        protected IContext InitCtx;
        public virtual void Init(IContext it)
        {
            InitCtx = it;
        }
        public virtual void Stop()
        {
            InitCtx = null;
        }
        public abstract double GetResource(string id,int ty = 0);
        public abstract int GetVillagerCount(string id);
        public abstract void OnTick(float delta);
        public abstract void OnClear();

        public abstract void AddAutoResourceById(string id,float addFactor = 1f);
        public abstract bool SpawnVillager(string id,int num);
        public abstract void AddResource(string id, double c);
        public abstract bool RemoveResource(string id,double r);
        public abstract void RemoveAllVillagers(string id);
        public abstract void Foreach(int ty,Action<string,double> action);
        public abstract int PlayerCount();
        public abstract void ChangeAutoResourceAddFactor(string id,float addFactor);
        public abstract void AddAutoResourceAddFactor(string id,float addFactor);
        public abstract void AddAutoResourceAddFactor(int group, float addFactor);
    }
}
