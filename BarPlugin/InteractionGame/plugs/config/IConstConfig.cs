using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteractionGame.plugs.config
{
    public interface IConstConfig
    {
        string OverlayCommKey { get; }
        string GameCommKey { get; }
        uint OverlayCommSize { get; }
        uint GameCommSize { get; }
        uint EndDelay { get; }
        Dictionary<string, int> GroupNameMap { get; }
        string GetGroupName(int id);
        int GetGroupIdByName(string name);
        int GetPureGuardLevel(int level);
        float GetOnPlayerJoinGoldAddition(int guardLevel);
        List<KeyValuePair<int, string>> GuardLevelListSorted { get; }
        int GetOnPlayerJoinAttributeAddition(int guardLevel);
        float GetPlayerHonorAddition(int guardLevel);
        float GetPlayerHonorAdditionForSettlement(int guardLevel);
        bool IsTestId(string id);
        long HonorGoldFactor { get; }
        int AutoGoldLimit { get; }
        int OriginResource { get; }
        int AddResFactor { get; }

        TimeSpan OneTimesGameTime {  get; }
        int OneTimesSpawnSquadCount { get; }
        int SquadCountLimit { get; }
        int AutoSquadCountLimit { get; }
    }
}
