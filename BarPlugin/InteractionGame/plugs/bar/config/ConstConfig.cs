using InteractionGame.plugs.config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteractionGame.plugs.bar.config
{
    public class ConstConfig : IConstConfig
    {
        public string OverlayCommKey => "BarOverlayMem";

        public string GameCommKey => "BarGameMem";
        public uint OverlayCommSize => 1024 * 1024 * 20;
        public uint GameCommSize => 1024 * 1024 * 10;

        public uint EndDelay => 14000;

        public Dictionary<string, int> GroupNameMap { get; } = new Dictionary<string, int>
        {
            { "蓝",0 },
            { "红",1 },
            { "绿",2 },
            { "黄",3 },
        };

        public List<KeyValuePair<int, string>> GuardLevelListSorted =>
            new List<KeyValuePair<int, string>>(){
                new KeyValuePair<int, string>( 2, "提督" ),
                new KeyValuePair<int, string>(33, "舰长" ),
                new KeyValuePair<int, string>(3, "舰长")
            };

        public long HonorGoldFactor => 20;

        public int AutoGoldLimit => 4000;

        public int OriginResource => 0;

        public int AddResFactor => 2;

        public int GetGroupIdByName(string name)
        {
            if (GroupNameMap.TryGetValue(name, out var id)) return id;
            return -1;
        }

        public string GetGroupName(int id)
        {
            foreach (var v in GroupNameMap)
            {
                if (v.Value + 1 == id)
                {
                    return v.Key;
                }
            }
            return "";
        }

        public int GetOnPlayerJoinAttributeAddition(int guardLevel)
        {
            return 0;
        }

        public float GetOnPlayerJoinGoldAddition(int guardLevel)
        {
            return 0.0f;
        }

        public float GetPlayerHonorAddition(int guardLevel)
        {
            return 0.0f;
        }

        public float GetPlayerHonorAdditionForSettlement(int guardLevel)
        {
            return 0.0f;
        }

        public int GetPureGuardLevel(int guardLvl)
        {
            if (guardLvl < 0 || guardLvl > 3)
                guardLvl = 0;
            if (guardLvl >= 10)
            {
                return guardLvl / 10;
            }
            return guardLvl;
        }

        public bool IsTestId(string id)
        {
            return int.TryParse(id, out var idNum) && idNum < 100;
        }
    }
}
