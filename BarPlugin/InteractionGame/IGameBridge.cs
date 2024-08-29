using conf.Squad;
using System.Collections.Generic;

namespace InteractionGame
{
    public interface IGameBridge
    {
        void Stop();
        void Init(IContext it);
        void OnTick(float delta);
        void OnClear();
        void ExecSpawnSquad(UserData user, SquadData squad, int count, int target = -1, object opt = null);
        int ExecSpawnGroup(UserData user, List<(SquadData, int)> group, int target = -1, double multiple = 1, object opt = null);
        void ForceFinish();
    }
}
