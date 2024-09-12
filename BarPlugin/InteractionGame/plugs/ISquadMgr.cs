using conf.Squad;
using InteractionGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteractionGame.plugs
{
    public enum SpawnSquadType
    {
        Auto,
        GoldBoom,
        AutoGoldBoom,
        HonorBoom,
        Gift
    }
    public interface ISquadMgr
    {
        SquadData GetSquadBySlot(int slot, UserData user);
        SquadData GetSquadById(int id);
        bool ValidSlot(int slot,UserData user);
        bool CanSpawnSquad(string uid, SpawnSquadType type);
        int SlotCount { get; }
        bool RandomSpecialSlot(int group, int slot);
    }
}
