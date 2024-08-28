using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using BililiveDebugPlugin.DB.Model;
using BililiveDebugPlugin.InteractionGame.mode;
using BililiveDebugPlugin.InteractionGame.plugs;
using BililiveDebugPlugin.InteractionGameUtils;
using conf.Squad;
using InteractionGame;
using Utils;
using SettlementData = InteractionGame.UserData;

namespace BililiveDebugPlugin.InteractionGame.Data
{
    public class Aoe4DataConfig
    {
        public const int VILLAGER_ID = 199;
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
        public static readonly string PPJ = "泡泡机";
        //public static readonly string FriendShip = "友谊的小船";
        public static readonly string SignTicket = "签到券";
        public static readonly string TiDu = "提督";
        public static readonly string JianZhang = "舰长";
        public static readonly string StrawberryCake = "草莓蛋糕";
        public static readonly string LovePillow = "爱心抱枕";
        public static readonly string GoDuck = "冲鸭";
        public static readonly string DaCallForYou = "为你打call";
        public static readonly string MovieTicket = "电影票";

        public static readonly Dictionary<string, ItemData> ItemDatas = new Dictionary<string, ItemData>()
        {
            {NiuWa,         ItemData.Create(NiuWa         ,EItemType.Gift,1) },
            {GanBao  ,      ItemData.Create(GanBao        ,EItemType.Gift,299) },
            {BbTang ,       ItemData.Create(BbTang         ,EItemType.Gift,2) },
            {ZheGe    ,     ItemData.Create(ZheGe          ,EItemType.Gift,10) },
            {XiaoCake ,     ItemData.Create(XiaoCake      ,EItemType.Gift,15) },
            {XiaoFuDie,     ItemData.Create(XiaoFuDie     ,EItemType.Gift,6) },
            {QingShu  ,     ItemData.Create(QingShu        ,EItemType.Gift,52) },
            {Gaobai   ,     ItemData.Create(Gaobai         ,EItemType.Gift,220) },
            {ShuiJing ,     ItemData.Create(ShuiJing       ,EItemType.Gift,20) },
            {Xinghe ,       ItemData.Create(Xinghe           ,EItemType.Gift,199) },
            {PPJ ,           ItemData.Create(PPJ              ,EItemType.Gift,50) },
            //{FriendShip ,    ItemData.Create(FriendShip       ,EItemType.Gift,52) },
            {DaCall,        ItemData.Create(DaCall          ,EItemType.Gift,5)     },
            {KuaKua ,        ItemData.Create(KuaKua         ,EItemType.Gift,330)  },
            {ShuiJingBall , ItemData.Create(ShuiJingBall    ,EItemType.Gift,1000)  },
            {SignTicket ,   ItemData.Create(SignTicket      ,EItemType.Ticket,200)  },
            {DaCallForYou , ItemData.Create(DaCallForYou ,  EItemType.Gift,99)  },
            {MovieTicket ,  ItemData.Create(MovieTicket ,   EItemType.Gift,20)  },
            //{ GoDuck , ItemData.Create(GoDuck               ,EItemType.Gift,99) },
        };

        public enum SpawnSquadType
        {
            Auto,
            GoldBoom,
            AutoGoldBoom,
            HonorBoom,
            Gift
        }

        public static readonly int[] RandomPool = { 100,102,104,106,107,109,110,111,113,117,118,122 };
        public static SquadData GetSquadPure(int index,int level,int g)
        {
            SquadData sd = null;
            if ((sd = SquadDataMgr.GetInstance().GetCountrySpecialSquad(index, level, g)) != null)
                return HandleHide(sd);
            sd = conf.Squad.SquadDataMgr.GetInstance().Get(level * 10_0000 + index);
            if (sd != null && sd.GetOverload(g, out var v))
                return v;
            if(sd != null && sd.Type_e == conf.Squad.EType.Placeholder)
            {
                Locator.Instance.Get<IContext>().PrintGameMsg($"{SettingMgr.GetCountry(g)}没有配置特殊单位{(char)index}");
                return null;
            }
            return HandleHide(sd);
        }
        public static SquadData GetMaxLevelSquad(int sid, int g)
        {
            var sd = GetSquadPure(sid, 1, g);
            while(sd.NextLevelRef != null)
                sd = sd.NextLevelRef;
            if (sd != null)
                return sd;
            return null;
        }
        private static SquadData HandleHide(SquadData squad)
        {
            if (squad == null || squad.Type_e == EType.Hide)
                return null;
            return squad;
        }
        public static SquadData GetSquadBySid(int sid,int g)
        {
            var index = sid % 10_0000;
            var level = sid / 10_0000;
            return GetSquadPure(index, level, g);
        }
        
        public static SquadData GetSquad(int index,int group,int level)
        {
            if(index == RandomIdx)
            {
                var key = RandomIdx * 10 + group;
                if (RandomSquad.TryGetValue(key, out var v))
                {
                    return v;
                }
                else
                {
                    RandomSquad.TryAdd(key, v = conf.Squad.SquadDataMgr.GetInstance().RandomGiftSquad);
                    return v;
                }
            }
            return GetSquadPure(index, level,group);
        }

        public static bool CanSpawnSquad(string uid, SpawnSquadType type)
        {
            switch (type)
            {
                case SpawnSquadType.Auto:
                case SpawnSquadType.GoldBoom:
                case SpawnSquadType.HonorBoom:
                case SpawnSquadType.AutoGoldBoom:
                case SpawnSquadType.Gift:
                    return true;
            }
            return true;
        }

        public static int SquadCount => conf.Squad.SquadDataMgr.GetInstance().NormalSquadDatas.Count;

        public static int RandomIdx { get; private set; } = 116;

        public static readonly TimeSpan OneTimesGameTime = TimeSpan.FromMinutes(SettingMgr.GetInt(9, 30));

        public static int OneTimesSpawnSquadCount => SettingMgr.GetInt(5, 50);

        public static int SquadLimit => SettingMgr.GetInt(3,850);
        public static int AutoSquadLimit => SquadLimit - SettingMgr.GetInt(4, 120);


       

    }
}
