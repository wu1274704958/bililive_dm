using BililiveDebugPlugin.InteractionGame.Data;
using InteractionGame;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace BililiveDebugPlugin.InteractionGame.Resource
{
    public class Aoe4ResMgr<C> : IResourceMgr<C>
        where C : class, IContext
    {
        long[] IdxMapId = new long[256];
        ConcurrentDictionary<long, int> MapTable = new ConcurrentDictionary<long, int>();
        ConcurrentDictionary<int, Utils.TimeLinerInteger> VillagerCountMap = new ConcurrentDictionary<int,Utils.TimeLinerInteger>();
        ConcurrentDictionary<long, Utils.TimeLinerInteger> AutoAddMap = new ConcurrentDictionary<long, Utils.TimeLinerInteger>();
        readonly int VillagerResFactor = 1;
        readonly int AutoAddResFactor = 1;
        private readonly int MaxVillagerCount = 50;
        int ExpectInfoIdx = 0;
        WindowInfo _windowInfo = null;

        public Aoe4ResMgr() {
            ClearIdxMapID();
        }

        public override void Init(C it, ILocalMsgDispatcher<C> dispatcher)
        {
            base.Init(it, dispatcher);
            _windowInfo = m_MsgDispatcher.GetBridge().GetWindowInfo();
        }

        private void ClearIdxMapID()
        {
            for(int i = 0; i < IdxMapId.Length; i++)
            {
                IdxMapId[i] = 0;
            }
        }

        public override int GetResource(long id, int ty = 0)
        {
            return GetAutoResource(id) + GetVillagerGatherRes(id);
        }

        private int GetVillagerGatherRes(long id)
        {
            lock (MapTable)
            {
                if (MapTable.TryGetValue(id, out var v))
                {
                    return GetVillagerGatherRes(v);
                }
            }
            return 0;
        }

        private int GetVillagerGatherRes(int k)
        {
            lock(VillagerCountMap)
            {
                if(VillagerCountMap.TryGetValue(k,out var v))
                {
                    return v.val;
                }
            }
            return 0;
        }

        private void UpdateVillagerGatherRes(int k,int c)
        {
            lock (VillagerCountMap)
            {
                if (VillagerCountMap.TryGetValue(k, out var v))
                {
                    if(c <= 0)
                    {
                        lock (IdxMapId)
                        {
                            if (IdxMapId[k] != 0) MapTable.TryRemove(IdxMapId[k], out _);
                            VillagerCountMap.TryRemove(k, out v);
                            IdxMapId[k] = 0;
                            return;
                        }
                    }
                    v.SetNewFactor(c * VillagerResFactor);
                }
                else if(c > 0)
                {
                    lock (IdxMapId)
                    {
                        if (IdxMapId[k] != 0)
                        {
                            VillagerCountMap[k] = new Utils.TimeLinerInteger(0,c * VillagerResFactor);
                        }
                    }
                }
            }
        }

        public override void AddAutoResourceById(long id, float addFactor = 1f)
        {
            Utils.TimeLinerInteger v = null;
            AutoAddMap.TryAdd(id, v = new Utils.TimeLinerInteger(Aoe4DataConfig.OriginResource, AutoAddResFactor));
            v.AddFactor = addFactor;
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


        private int GetAutoResource(long id)
        {
            if(AutoAddMap.TryGetValue(id,out var v))
            {
                lock (v) {
                    return v.val;
                }
            }
            return 0;
        }

        public override void OnClear()
        {
            MapTable.Clear();
            VillagerCountMap.Clear(); 
            AutoAddMap.Clear();
            lock(IdxMapId)
            {
                for(var k = 0; k < IdxMapId.Length; k++)
                    IdxMapId[k] = 0;
            }
            ExpectInfoIdx = 0;
        }

        public override void OnTick(float delta)
        {
            if(_windowInfo == null && _windowInfo.Hwnd == IntPtr.Zero) return;
            var d = InitCtx.CheckState(EAoe4State.VillagerState);
            if(d.r == ExpectInfoIdx)
            {
                UpdateVillagerGatherRes(d.g, d.b);
                MoveNextExpectInfoIdx(d.g);
            }
        }

        private void MoveNextExpectInfoIdx(int vid)
        {
            var n = ExpectInfoIdx + 1;
            if (n > 255)
                n = 0;
            Interlocked.Exchange(ref ExpectInfoIdx, n);
            m_MsgDispatcher.GetBridge().ExecTryRemoveVillagersCountNotify(vid,n);
        }

        public override bool SpawnVillager(long id, int num)
        {
            if(MapTable.TryGetValue(id,out var v) && VillagerCountMap.TryGetValue(v,out var vc))
            {
                if(vc.Factor / VillagerResFactor >= MaxVillagerCount)
                {
                    var ud = m_MsgDispatcher.GetMsgParser().GetUserData(id);
                    InitCtx.PrintGameMsg($"{ud.Name}村民超出上限,产出村民失败");
                    return false;
                }
                RealSpawnVillager(id,v,num);
            }
            else
            {
                if(MapTable.Count >= 256) return false;
                var vid = GetNewVillagerId(id);
                if(vid == -1) return false;
                MapTable.TryAdd(id, vid);
                RealSpawnVillager(id, vid, num);
            }
            return true;
        }

        private void RealSpawnVillager(long id,int vid, int num)
        {
            var target = 0;// m_MsgDispatcher.GetPlayerParser().GetTarget(id);
            var self = m_MsgDispatcher.GetPlayerParser().GetGroupById(id);
            if (self != -1 && target != -1)
            {
                m_MsgDispatcher.GetBridge().ExecSpawnVillagers(self + 1, vid, num);
            }
        }

        private int GetNewVillagerId(long id)
        {
            lock(IdxMapId){
                for (int i = 0; i < IdxMapId.Length; i++) {
                    if (IdxMapId[i] == 0)
                    {
                        IdxMapId[i] = id;
                        return i;
                    }
                }
            }
            return -1;
        }

        public override void AddResource(long id, int c)
        {
            if(AutoAddMap.TryGetValue(c, out var v))
            {
                lock (v)
                {
                    v.Append(c);
                }
            }
        }

        public override bool RemoveResource(long id, int r)
        {
            int f = 0;
            if (AutoAddMap.TryGetValue(id, out var v)) ++f;
            Utils.TimeLinerInteger integer = null;
            if (MapTable.TryGetValue(id, out var vid) && VillagerCountMap.TryGetValue(vid, out integer)) ++f;
            if(f == 0) return false;
            if(f == 1)
            {
                lock(v)
                {
                    if (v.val >= r)
                    {
                        v.Sub(r);
                        return true;
                    }
                }
                return false;
            }else if(f == 2)
            {
                lock (v)
                {
                    if (v.val >= r)
                    {
                        v.Sub(r);
                        return true;
                    }
                    var rest = r - v.val;
                    lock (integer)
                    {
                        if (integer.val >= rest)
                        {
                            v.Sub(r - rest);
                            integer.Sub(rest);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override void RemoveAllVillagers(long id)
        {
            lock (IdxMapId)
            {
                if(MapTable.TryGetValue(id, out var vid))
                {
                    VillagerCountMap.TryGetValue(vid, out var v2);
                    if(v2 != null)
                    {
                        lock(v2)
                        {
                            v2.SetNewFactor(0);
                        }
                        m_MsgDispatcher.GetBridge().ExecCheckVillagerCount(vid);
                    }
                    else
                    {
                        MapTable.TryRemove(id,out _);
                        VillagerCountMap.TryRemove(vid,out _);
                        IdxMapId[vid] = 0;
                    }
                }
                return;
            }
        }

        public override int GetVillagerCount(long id)
        {
            if(MapTable.TryGetValue(id, out var vid) && VillagerCountMap.TryGetValue(vid,out var v))
            {
                return v.Factor / VillagerResFactor;
            }
            return 0;
        }

        public override void Foreach(int ty, Action<long, int> action)
        {
            foreach (var it in AutoAddMap)
            {
                lock (it.Value)
                {
                    action.Invoke(it.Key, it.Value.val + GetVillagerGatherRes(it.Key));
                }
            }
        }

        public override int PlayerCount()
        {
            return AutoAddMap.Count;
        }
    }
}
