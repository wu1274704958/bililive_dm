using conf.Squad;
using InteractionGame.Context;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace InteractionGame.plugs.bar
{
    public class SlotData
    {
        public int Slot;
        public SquadData Squad;
    }
    public class SyncSquadConfigData
    {
        public List<SlotData> Slots = new List<SlotData>();
    }
    class BarSquadMgr : IPlug<EGameAction>, ISquadMgr
    {
        private ConcurrentDictionary<int,Pair<List<SquadData>,int>> SlotDict = new ConcurrentDictionary<int, Pair<List<SquadData>,int>>();
        private ConcurrentDictionary<int,SquadData> SlotMap = new ConcurrentDictionary<int, SquadData>();
        private Random random = new Random();
        private IContext _context;
        private static readonly int MaxNormalSlot = '9';
        private ConcurrentDictionary<int, SquadData> SpecialSlotMap = new ConcurrentDictionary<int, SquadData>();

        public int SlotCount => SlotMap.Count;

        public bool CanSpawnSquad(string uid, SpawnSquadType type)
        {
            return true;
        }

        public SquadData GetSquadById(int id)
        {
            return SquadDataMgr.GetInstance().Get(id);
        }

        public SquadData GetSquadBySlot(int slot, UserData user)
        {
            if(SlotMap.TryGetValue(slot,out var v))
                return v;
            if (SpecialSlotMap.TryGetValue(MapSpecialSlot(user.Group, slot), out v))
                return v;
            return null;
        }

        public override void OnReceiveNotify(EGameAction m,object args = null)
        {
            switch (m)
            {
                case EGameAction.GamePreStart:
                    RandomSlot();
                    SendSlotToOverlay(SlotMap,EMsgTy.SyncSquadConfig);
                    break;
                case EGameAction.GameStop:
                    SlotMap.Clear();
                    SpecialSlotMap.Clear();
                    break;
                case EGameAction.ConfigReload:
                    SlotDict.Clear();
                    LoadSquad();
                    break;
            }
        }

        private void SendSlotToOverlay(ConcurrentDictionary<int, SquadData> map,EMsgTy msg)
        {
            var data = new SyncSquadConfigData();
            foreach(var it in map)
            {
                data.Slots.Add(new SlotData
                {
                    Slot = it.Key,
                    Squad = it.Value
                });
            }
            data.Slots.Sort((a,b) => a.Slot - b.Slot);
            _context.SendMsgToOverlay((short)msg, data);
        }

        private void RandomSlot()
        {
            foreach (var it in SlotDict)
            {
                if(it.Key <= MaxNormalSlot)
                    SlotMap[it.Key] = RandomSlot(it.Value.first, it.Value.second);
            }
        }

        private SquadData RandomSlot(List<SquadData> list, int totalProbability)
        {
            var n = random.Next(totalProbability);
            if (n == 0)
                return list[0];
            var index = 0;
            while (n > 0)
            {
                n -= list[index].RandomProbability;
                ++index;
            }
            return list[index];
        }
        private SquadData RandomSlot(int slot)
        {
            if(SlotDict.TryGetValue(slot, out var data))
                return RandomSlot(data.first, data.second);
            return null;
        }

        public override void Init()
        {
            base.Init();
            Locator.Deposit<ISquadMgr>(this);
            LoadSquad();
            
        }

        public override void Start()
        {
            base.Start();
            _context = Locator.Get<IContext>();
        }

        public void TestSendSlot()
        {
            if(SlotMap.Count == 0)
                RandomSlot();
            SendSlotToOverlay(SlotMap, EMsgTy.SyncSquadConfig);
        }

        private void LoadSquad()
        {
            foreach (var it in SquadDataMgr.GetInstance().Dict)
            {
                switch (it.Value.Type_e)
                {
                    case EType.Normal:
                    case EType.Special:
                        var slot = it.Value.Slot;
                        if (!SlotDict.ContainsKey(slot))
                            SlotDict[slot] = new Pair<List<SquadData>, int>(new List<SquadData>(), 0);
                        SlotDict[slot].first.Add(it.Value);
                        SlotDict[slot].second += it.Value.RandomProbability;
                        break;
                }
            }
        }

        public override void Tick()
        {
            throw new NotImplementedException();
        }

        public bool ValidSlot(int slot, UserData user)
        {
            return SlotMap.ContainsKey(slot);
        }

        private int MapSpecialSlot(int group,int slot) => (group * 1000) + slot;

        public bool RandomSpecialSlot(int group, int slot)
        {
            var key = MapSpecialSlot(group, slot);
            var sd = RandomSlot(slot);
            if (sd == null)
                return false;
            SpecialSlotMap[key] = sd;
            return true;
        }

        public void SendSpecialSlot()
        {
            if (SpecialSlotMap.Count == 0)
                return;
            SendSlotToOverlay(SpecialSlotMap, EMsgTy.SyncSpecialSquadConfig);
        }
    }
}
