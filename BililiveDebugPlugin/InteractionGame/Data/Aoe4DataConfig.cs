using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BililiveDebugPlugin.DB.Model;
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
        public const int VillagerID = 199;

        public struct SquadData
        {
            public string Name;
            public int QuickSuccessionNum;
            public int Score => m_Score;
            public int m_Price;
            public int Price => m_Score;
            public ESquadType SquadType;
            public float TrainTime;
            private int m_Score;
            public SquadData(string name, int score, ESquadType squadType = ESquadType.Normal)
            {
                Name = name;
                QuickSuccessionNum = 1;
                m_Score = score;
                m_Price = score;
                SquadType = squadType;
                TrainTime = score * 0.333333f;
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
            { 4, new SquadData ("骑士",          12       )},
            { 5, new SquadData ("长剑武士",      6          )},
            { 6, new SquadData ("箭塔象",        26        )},
            { 7, new SquadData ("蜂窝炮",        44        )},
            { 8, new SquadData ("手推炮",        52        , ESquadType.BuildingAttacker)},
            { 9, new SquadData ("枪塔象",        32        )},
            { 10, new SquadData("教士",         15        )},
            { 11, new SquadData("火枪",         18        )},
            { 12, new SquadData("诸葛弩",        4         )},
            { 13, new SquadData("火长矛",        7         , ESquadType.SiegeAttacker )},
            { 14, new SquadData("弩车",          39       , ESquadType.SiegeAttacker  )},
            { 15, new SquadData("古拉姆",        7         )},
            { 16, new SquadData("拍车",          42       )},
            { 17, new SquadData("掷弹兵",        8         )},
            { 18, new SquadData("军乐队",        25        )},
            { 19, new SquadData("战象",          27       )},
            { 20, new SquadData("日本武士",      9          )},
            { 21, new SquadData("突骑",          8        )},
            { 22, new SquadData("苏丹亲兵",      16         )},
            { 23, new SquadData("骆驼骑兵",      7         )},
            { 24, new SquadData("骑手",           5      , ESquadType.SiegeAttacker)},
            { 25, new SquadData("冲车",           35      )},
            { 26, new SquadData("旗本武士",       20      )},
            { 27, new SquadData("旗本射手",       21      )},
            { 28, new SquadData("旗本骑士",       23     , ESquadType.SiegeAttacker)},

            { 100, new SquadData("风琴炮",       44         )},
            { 101, new SquadData("国王",         100         )},
            { 102, new SquadData("精锐古拉姆",     9          ) },

            { 104, new SquadData("射击军",              20)},
            { 105, new SquadData("教士",                10)},
            { 106, new SquadData("乌尔班巨炮",           70 ,  ESquadType.BuildingAttacker)},

            { 107, new SquadData("精锐掷弹兵",                   12)},
            { 108, new SquadData("长管炮",                       60,  ESquadType.SiegeAttacker      )},
            { 109, new SquadData("皇家长管炮",                   70,   ESquadType.SiegeAttacker       )},
            { 110, new SquadData("精锐长弓兵",                   6       )},

            { 111, new SquadData("精锐苏丹亲兵",              16)},
            { 113, new SquadData("大筒兵",                     20)},
            { 117, new SquadData("精锐旗本武士",              25)},
            { 118, new SquadData("精锐旗本射手",              26)},
            { 119, new SquadData("精锐旗本骑士",              27, ESquadType.SiegeAttacker    )},
        };
        
        public static readonly string NiuWa = "牛哇牛哇";
        public static readonly string GanBao = "干杯";
        public static readonly string BBTang = "棒棒糖";
        public static readonly string ZheGe     = "这个好诶";
        public static readonly string XiaoCake   = "小蛋糕";
        public static readonly string XiaoFuDie   = "小蝴蝶";
        public static readonly string QingShu   = "情书";
        public static readonly string Gaobai    = "告白花束";
        public static readonly string ShuiJing   = "水晶之恋";
        public static readonly string Xinghe = "星河入梦";
        //public static readonly string DM = "动鳗电池";
        //public static readonly string KuaKua = "花式夸夸";
        public static readonly string ShuiJingBall = "星愿水晶球";
        public static readonly string PPJ = "泡泡机";
        public static readonly string FriendShip = "友谊的小船";

        public static readonly Dictionary<string, ItemData> ItemDatas = new Dictionary<string, ItemData>()
        {
            {NiuWa,         ItemData.Create(NiuWa         ,EItemType.Gift,1) },
            {GanBao  ,      ItemData.Create(GanBao        ,EItemType.Gift,66) },
            {BBTang ,       ItemData.Create(BBTang         ,EItemType.Gift,2) },
            {ZheGe    ,     ItemData.Create(ZheGe          ,EItemType.Gift,10) },
            {XiaoCake ,     ItemData.Create(XiaoCake      ,EItemType.Gift,15) },
            {XiaoFuDie,     ItemData.Create(XiaoFuDie     ,EItemType.Gift,6) },
            {QingShu  ,     ItemData.Create(QingShu        ,EItemType.Gift,52) },
            {Gaobai   ,     ItemData.Create(Gaobai         ,EItemType.Gift,220) },
            {ShuiJing ,     ItemData.Create(ShuiJing       ,EItemType.Gift,20) },
            {Xinghe ,       ItemData.Create(Xinghe           ,EItemType.Gift,199) },
            {PPJ ,           ItemData.Create(PPJ              ,EItemType.Gift,50) },
            {FriendShip ,    ItemData.Create(FriendShip       ,EItemType.Gift,52) },
            {ShuiJingBall , ItemData.Create(ShuiJingBall    ,EItemType.Gift,1000) },
        };

        public static SquadData GetSquad(int index)
        {
            if (SquadDatas.TryGetValue(index, out var v))
                return v;
            else
                return new SquadData("None", 0);
        }
        public static int SquadCount => SquadDatas.Count - 15;
        public static readonly TimeSpan OneTimesGameTime = TimeSpan.FromHours(1);

        public static readonly int OneTimesSpawnSquadCount = 470;

        public static readonly int SquadLimit = 1100;
        public static readonly int AutoSquadLimit = SquadLimit - 200;

        public static readonly long HonorGoldFactor = 20;
        public static readonly int AutoGoldLimit = 3000;
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
            var f = (b ? WinSettlementHonorFactor : LoseSettlementHonorFactor) + (isLeastGroup ? LeastGroupSettlementHonorFactor : 0.0)
                 + (i < 3 ? 0.0001 * (3 - i) : 0.0);
            var r = (long)Math.Floor(user.Score * f) + (b ? WinSettlementHonorAdd : LoseSettlementHonorAdd);
            if (user.GuardLevel > 0) r += (long)Math.Ceiling(r * PlayerResAddFactorArr[user.GuardLevel]);
            if (user.FansLevel > 0) r += (int)(r * (user.FansLevel / 20));
            return r;
        }

        public static long LoseSettlementHonorAdd = 2;
        public static long WinSettlementHonorAdd = 5;
        public static double LoseSettlementHonorFactor = 0.0003;
        public static double WinSettlementHonorFactor = 0.0005;
        private static readonly double LeastGroupSettlementHonorFactor = 0.0002;

        public static readonly float[] PlayerResAddFactorArr = new float[] { 0.0f, 2.0f, 1.2f, 0.6f };
        public static readonly int[] PlayerAddAttributeArr = new int[] { 0, 3, 2, 1 };
    }
}
