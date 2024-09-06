using conf.Squad;
using InteractionGame.Context;
using InteractionGame.plugs.config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace InteractionGame.plugs.bar.config
{
    public class ConstConfig : IPlug<EGameAction>,IConstConfig
    {
        public string OverlayCommKey => "BarOverlayMem";

        public string GameCommKey => "BarGameMem";
        public uint OverlayCommSize => 1024 * 1024 * 20;
        public uint GameCommSize => 1024 * 1024 * 10;

        public uint EndDelay => 28000;

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
        ///----------------------------------------------------
        private TimeSpan _oneTimesGameTime;
        private int _oneTimesSpawnSquadCount;
        private int _squadCountLimit;
        private int _autoSquadCountLimit;
        ///----------------------------------------------------
        public TimeSpan OneTimesGameTime => _oneTimesGameTime;
        public int OneTimesSpawnSquadCount => _oneTimesSpawnSquadCount;
        public int SquadCountLimit => _squadCountLimit;
        public int AutoSquadCountLimit => _autoSquadCountLimit;

        public int WinGroupAddedScore => 500;

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

        public override void Tick()
        {
            throw new NotImplementedException();
        }

        public override void Notify(EGameAction m)
        {
            if(m == EGameAction.GameStop)
                InitData();
        }

        public override void Init()
        {
            base.Init();
            Locator.Deposit<IConstConfig>(this);
            InitData();
        }

        public override void Start()
        {
            base.Start();
        }

        public override void Dispose()
        {
            Locator.Remove<IConstConfig>();
            base.Dispose();
        }

        private void InitData()
        {
            _oneTimesGameTime = TimeSpan.FromMinutes(SettingMgr.GetInt(9, 30));
            _oneTimesSpawnSquadCount = SettingMgr.GetInt(5, 50);
            _squadCountLimit = SettingMgr.GetInt(3, 850);
            _autoSquadCountLimit = _squadCountLimit - SettingMgr.GetInt(4, 120);
        }
    }
}
