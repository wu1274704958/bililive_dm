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
    class BarSquadMgr : IPlug<EGameAction>,ISquadMgr
    {
        private ConcurrentDictionary<int,Pair<List<SquadData>,int>> SlotDict = new ConcurrentDictionary<int, Pair<List<SquadData>,int>>();
        private ConcurrentDictionary<int,SquadData> SlotMap = new ConcurrentDictionary<int, SquadData>();
        private Random random = new Random((int)(new DateTime().Ticks / 1000));
        private IContext _context;

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
            return null;
        }

        public override void Notify(EGameAction m)
        {
            switch (m)
            {
                case EGameAction.GamePreStart:
                    RandomSlot();
                    SendSlotToOverlay();
                    break;
                case EGameAction.GameStop:
                    SlotMap.Clear();
                    break;
            }
        }

        private void SendSlotToOverlay()
        {
            var data = new SyncSquadConfigData();
            foreach(var it in  SlotMap)
            {
                data.Slots.Add(new SlotData
                {
                    Slot = it.Key,
                    Squad = it.Value
                });
            }
            data.Slots.Sort((a,b) => a.Slot - b.Slot);
            _context.SendMsgToOverlay((short)EMsgTy.SyncSquadConfig, data);
        }

        private void RandomSlot()
        {
            foreach (var it in SlotDict)
            {
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
            SendSlotToOverlay();
        }

        private void LoadSquad()
        {
            foreach (var it in SquadDataMgr.GetInstance().Dict)
            {
                switch (it.Value.Type_e)
                {
                    case EType.Normal:
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
    }
}
