using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using BililiveDebugPlugin.DB.Model;
using BililiveDebugPlugin.InteractionGameUtils;
using Utils;
using SettlementData = InteractionGame.UserData;

namespace BililiveDebugPlugin.InteractionGame.Data
{
    public enum ESquadType : uint
    {
        Normal = 0,
        Villager = 1,
        SiegeAttacker = 2,
        BuildingAttacker = 3,
    }
    public class Aoe4DataConfig
    {
        public const int VILLAGER_ID = 199;

        public struct SquadData
        {
            public int Id;
            public string Name;
            public int QuickSuccessionNum;
            public int Score => _mScore;
            private int _mPrice;
            public int Price => _mScore;
            public ESquadType SquadType;
            public float TrainTime;
            private int _mScore;
            public SquadData(string name, int score, ESquadType squadType = ESquadType.Normal)
            {
                Name = name;
                QuickSuccessionNum = 1;
                _mScore = score;
                _mPrice = score;
                SquadType = squadType;
                TrainTime = score * 0.333333f;
                Id = 0;
            }
            public bool Invaild => Score == 0;
        }
        //"长矛兵","长弓兵","中国武士","弩手","骑士","长剑武士","箭塔象","蜂窝炮","乌尔班巨炮","战象","火枪","诸葛弩","火长矛"
        // 10,9, 7,9, 5, 8,3,2,1,1,5,9,5
        public static readonly Dictionary<int, SquadData> SquadDatas = new Dictionary<int, SquadData>()
        {
            { 0, new SquadData ("长矛兵",        3         )},
            { 1, new SquadData ("步弓手",        4         )},
            { 2, new SquadData ("关刀",          6        )},
            { 3, new SquadData ("弩手",          6        )},
            { 4, new SquadData ("骑士",          9       )},
            { 5, new SquadData ("长剑武士",      6          )},
            { 6, new SquadData ("箭塔象",        26        )},
            { 7, new SquadData ("蜂窝炮",        66        )},
            { 8, new SquadData ("手推炮",        79        , ESquadType.BuildingAttacker)},
            { 9, new SquadData ("枪塔象",        32        )},
            { 10, new SquadData("教士",         15        )},
            { 11, new SquadData("火枪",         18        )},
            { 12, new SquadData("诸葛弩",        4         )},
            { 13, new SquadData("火长矛",        7         , ESquadType.SiegeAttacker )},
            { 14, new SquadData("弩车",          47       , ESquadType.SiegeAttacker  )},
            { 15, new SquadData("古拉姆",        7         )},
            { 16, new SquadData("拍车",          63       )},
            { 17, new SquadData("掷弹兵",        8         )},
            { 18, new SquadData("军乐队",        25        )},
            { 19, new SquadData("战象",          27       )},
            { 20, new SquadData("日本武士",      9          )},
            { 21, new SquadData("怯薛",          13        )},
            { 22, new SquadData("苏丹亲兵",      16         )},
            { 23, new SquadData("骆驼骑兵",      6         )},
            { 24, new SquadData("骑手",           5      , ESquadType.SiegeAttacker)},
            { 25, new SquadData("冲车",           30      )},
            { 26, new SquadData("旗本武士",       13      )},
            { 27, new SquadData("旗本射手",       12      )},
            { 28, new SquadData("旗本骑士",       11     , ESquadType.SiegeAttacker)},
            { 29, new SquadData("旗本骑士",       23     , ESquadType.SiegeAttacker)},
            { 30, new SquadData("武僧",           6      )},

            { 100, new SquadData("风琴炮",       80         )},
            { 101, new SquadData("国王",         100         )},
            { 102, new SquadData("精锐古拉姆",     9          ) },

            { 104, new SquadData("射击军",              19)},
            { 105, new SquadData("教士",                10)},
            { 106, new SquadData("乌尔班巨炮",           160 ,  ESquadType.BuildingAttacker)},

            { 107, new SquadData("精锐掷弹兵",                   12)},
            { 108, new SquadData("长管炮",                       120,  ESquadType.SiegeAttacker      )},
            { 109, new SquadData("皇家长管炮",                   140,   ESquadType.SiegeAttacker       )},
            { 110, new SquadData("精锐长弓兵",                   6       )},

            { 111, new SquadData("精锐苏丹亲兵",              16)},
            { 113, new SquadData("大筒兵",                     26)},
            { 117, new SquadData("精锐旗本武士",              15)},
            { 118, new SquadData("精锐旗本射手",              14)},
            { 119, new SquadData("精锐旗本骑士",              15, ESquadType.SiegeAttacker    )},
            { 122, new SquadData("沙漠掠夺者",              19)},
        };
        public static readonly ConcurrentDictionary<int, SquadData> RandomSquad = new ConcurrentDictionary<int, SquadData>();



        public static readonly string NiuWa = "牛哇牛哇";
        public static readonly string GanBao = "干杯";
        public static readonly string BbTang = "棒棒糖";
        public static readonly string ZheGe     = "这个好诶";
        public static readonly string XiaoCake   = "小蛋糕";
        public static readonly string XiaoFuDie   = "小蝴蝶";
        public static readonly string QingShu   = "情书";
        public static readonly string Gaobai    = "告白花束";
        public static readonly string ShuiJing   = "水晶之恋";
        public static readonly string Xinghe = "星河入梦";
        //public static readonly string DM = "动鳗电池";
        public static readonly string KuaKua = "花式夸夸";
        public static readonly string ShuiJingBall = "星愿水晶球";
        public static readonly string DaCall = "打call";
        //public static readonly string PPJ = "泡泡机";
        //public static readonly string FriendShip = "友谊的小船";
        public static readonly string SignTicket = "签到券";

        public static readonly Dictionary<string, ItemData> ItemDatas = new Dictionary<string, ItemData>()
        {
            {NiuWa,         ItemData.Create(NiuWa         ,EItemType.Gift,1) },
            {GanBao  ,      ItemData.Create(GanBao        ,EItemType.Gift,66) },
            {BbTang ,       ItemData.Create(BbTang         ,EItemType.Gift,2) },
            {ZheGe    ,     ItemData.Create(ZheGe          ,EItemType.Gift,10) },
            {XiaoCake ,     ItemData.Create(XiaoCake      ,EItemType.Gift,15) },
            {XiaoFuDie,     ItemData.Create(XiaoFuDie     ,EItemType.Gift,6) },
            {QingShu  ,     ItemData.Create(QingShu        ,EItemType.Gift,52) },
            {Gaobai   ,     ItemData.Create(Gaobai         ,EItemType.Gift,220) },
            {ShuiJing ,     ItemData.Create(ShuiJing       ,EItemType.Gift,20) },
            {Xinghe ,       ItemData.Create(Xinghe           ,EItemType.Gift,199) },
            //{PPJ ,           ItemData.Create(PPJ              ,EItemType.Gift,50) },
            //{FriendShip ,    ItemData.Create(FriendShip       ,EItemType.Gift,52) },
            {DaCall,        ItemData.Create(DaCall          ,EItemType.Gift,5)     },
            {KuaKua ,        ItemData.Create(KuaKua          ,EItemType.Gift,330)  },
            {ShuiJingBall , ItemData.Create(ShuiJingBall    ,EItemType.Gift,1000)  },
            {SignTicket ,   ItemData.Create(SignTicket      ,EItemType.Ticket,100)  },
        };

        public static readonly int[] RandomPool = { 100,102,104,106,107,109,110,111,113,117,118,122 };
        public static SquadData GetSquadPure(int index)
        {
            if (SquadDatas.TryGetValue(index, out var v))
                return v;
            else
                return new SquadData();
        }
        public static SquadData GetSquad(int index,int group,out int sid)
        {
            sid = index;
            SquadData v;
            if(index == RandomIdx)
            {
                var key = RandomIdx * 10 + group;
                if (RandomSquad.TryGetValue(key, out v))
                {
                    sid = v.Id;
                    return v;
                }
                else
                {
                    RandomSquad.TryAdd(key, v = ToRandomSquad());
                    sid = v.Id;
                    return v;
                }
            }
            if (SquadDatas.TryGetValue(index, out v))
                return v;
            else
                return v;
        }

        private static SquadData ToRandomSquad()
        {
            var id = RandomPool[new Random(DateTime.Now.Millisecond).Next(0, RandomPool.Length)];
            var v = SquadDatas[id];
            v.Id = id;
            return v;
        }

        public static int SquadCount => SquadDatas.Count - 16;

        public static int RandomIdx { get; private set; } = 29;

        public static readonly TimeSpan OneTimesGameTime = TimeSpan.FromHours(1);

        public static readonly int OneTimesSpawnSquadCount = 300;

        public static readonly int SquadLimit = 900;
        public static readonly int AutoSquadLimit = SquadLimit - 160;

        public static readonly long HonorGoldFactor = 20;
        public static readonly int AutoGoldLimit = 4000;
        public static readonly int OriginResource = 50;
        public static readonly int BaoBingOriginResource = 0;
        public static readonly int GroupCount = 2;
        public static readonly int BaoBingAddResFactor = 1;

        public static int GetGroupExclude(int g)
        {
            for(int i = 0; i < GroupCount; i++)
            {
                if(g != i)
                    return i;
            }
            return -1;
        }

        public static long CalcHonorSettlement(SettlementData user, bool b,bool isLeastGroup,int i)
        {
            RandomSquad.Clear();
            var minutes = (DateTime.Now - Locator.Instance.Get<AutoForceStopPlug>().StartTime).TotalMinutes;
            var f = (b ? WinSettlementHonorFactor : LoseSettlementHonorFactor) + (isLeastGroup ? LeastGroupSettlementHonorFactor : 0.0)
                 + (i < 3 ? 0.0003 * (3 - i) : 0.0) + (minutes / 10 / 1000);
            if (user.FansLevel > 0) f += (user.FansLevel / 2000);
            if (user.GuardLevel > 0) f += f * SettlementPlayerResAddFactorArr[user.GuardLevel];
            var r = (long)Math.Floor(user.Score * f) + (b ? WinSettlementHonorAdd : LoseSettlementHonorAdd);
            var activityMult = global::InteractionGame.Utils.GetNewYearActivity() > 0 ? 2 : 1;
            
            return r * activityMult;
        }

        public static long LoseSettlementHonorAdd = 3;
        public static long WinSettlementHonorAdd = 10;
        public static double LoseSettlementHonorFactor = 0.0003;
        public static double WinSettlementHonorFactor = 0.0010;
        private static readonly double LeastGroupSettlementHonorFactor = 0.0005;

        public static readonly float[] SettlementPlayerResAddFactorArr = new float[] { 0.0f, 65.0f, 6.6f, 0.6f };
        public static readonly float[] PlayerResAddFactorArr = new float[] { 0.0f, 65.0f, 4.6f, 0.6f };
        public static readonly int[] PlayerAddAttributeArr = new int[] { 0, 140, 10, 1 };
        internal static readonly string Aoe4WinTitle = "Age of Empires IV";
    }
}
