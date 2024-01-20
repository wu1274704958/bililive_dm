using InteractionGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace BililiveDebugPlugin.InteractionGame.Resource
{
    public abstract class IResourceMgr<C>
        where C: class,IContext
    {
        protected C InitCtx;
        protected ILocalMsgDispatcher<C> m_MsgDispatcher;
        public virtual void Init(C it, ILocalMsgDispatcher<C> dispatcher)
        {
            InitCtx = it;
            m_MsgDispatcher = dispatcher;
        }
        public virtual void Stop()
        {
            InitCtx = null;
            m_MsgDispatcher = null;
        }
        public abstract int GetResource(long id,int ty = 0);
        public abstract int GetVillagerCount(long id);
        public abstract void OnTick(float delta);
        public abstract void OnClear();

        public abstract void AddAutoResourceById(long id,float addFactor = 1f);
        public abstract bool SpawnVillager(long id,int num);
        public abstract void AddResource(long id, int c);
        public abstract bool RemoveResource(long id,int r);
        public abstract void RemoveAllVillagers(long id);
        public abstract void Foreach(int ty,Action<long,int> action);
        public abstract int PlayerCount();
        public abstract void ChangeAutoResourceAddFactor(long id,float addFactor);
        public abstract void AddAutoResourceAddFactor(long id,float addFactor);
    }
}
