using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BililiveDebugPlugin.InteractionGame
{
    public interface ISquadCountObserver
    {
        void SquadCountChanged(int g, int old, int n);
    }
    public interface IGameState
    {
        void OnSpawnSquad(int group, int count);
        int GetSquadCount(int group);
        void AddObserver(ISquadCountObserver observer);
        void RemoveObserver(ISquadCountObserver observer);
    }
}
