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
        public SquadData 
    }
    public class SyncGameStartConfigData
    {
        
    }
    class BarSquadMgr : IPlug<EGameAction>,ISquadMgr
    {
        private ConcurrentDictionary<int,Pair<List<SquadData>,int>> SlotDict = new ConcurrentDictionary<int, Pair<List<SquadData>,int>>();
        private ConcurrentDictionary<int,SquadData> SlotMap = new ConcurrentDictionary<int, SquadData>();
        private Random random;
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
                case EGameAction.GameStart:
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

        }

        private void RandomSlot()
        {
            random = new Random((int)(new DateTime().Ticks / 1000));

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
            LoadSquad();
        }

        private void LoadSquad()
        {
            foreach(var it in SquadDataMgr.GetInstance().Dict)
            {
                var slot = it.Value.Slot;
                if (!SlotDict.ContainsKey(slot))
                    SlotDict[slot] = new Pair<List<SquadData>, int>( new List<SquadData>(),0 );
                SlotDict[slot].first.Add(it.Value);
                SlotDict[slot].second += it.Value.RandomProbability;
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
