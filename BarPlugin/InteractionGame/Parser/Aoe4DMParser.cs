using BilibiliDM_PluginFramework;
using BililiveDebugPlugin.InteractionGame.Data;
using InteractionGame;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Text;
using BililiveDebugPlugin.DB;
using BililiveDebugPlugin.InteractionGame.Resource;
using System.Threading;
using System.Linq;
using BililiveDebugPlugin.DB.Model;
using BililiveDebugPlugin.InteractionGameUtils;
using conf.Squad;
using Utils;
using static InteractionGame.Utils;
using ProtoBuf.WellKnownTypes;
using UserData = InteractionGame.UserData;
using BililiveDebugPlugin.InteractionGame.plugs;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using InteractionGame.plugs.config;
using InteractionGame.plugs;
using BarPlugin.InteractionGame.plugs;

namespace BililiveDebugPlugin.InteractionGame.Parser
{
    using Msg = DanmakuModel;
    using MsgType = MsgTypeEnum;

    public class MsgGiftParser<IT> : IDyMsgParser, IPlayerParserObserver
        where IT : class, IContext
    {
        private IConstConfig _config;
        private ISquadMgr _squadMgr;
        public ObjectPool<List<(string, int)>> SquadListPool { get; private set; } = new ObjectPool<List<(string, int)>>(
            () => new List<(string, int)>(), (a) => a.Clear());
        private Regex BooHonorRegex = new Regex("z([0-9a-wA-W]+)[x,\\*,×]([0-9]+)");
        private Regex BooHonorRegex2 = new Regex("z([0-9a-wA-W]+)");
        private Regex SendBagGiftRegex = new Regex("送(.+)[x,\\*,×]([0-9]+)");
        private Regex SendBagGiftRegex2 = new Regex("送(.+)");
        private SpawnSquadQueue m_SpawnSquadQueue;

        public override bool Demand(Msg msg, MsgType barType)
        {
            return StaticMsgDemand.Demand(msg, barType);
        }

        public override void Init(IContext it)
        {
            AddSubMsgParse(new AutoSpawnSquadSubMsgParser());
            AddSubMsgParse(new SignInSubMsgParser());
            AddSubMsgParse(new GroupUpLevel());
            AddSubMsgParse(new AdminParser());
            AddSubMsgParse(new BossGameModeParser());
            base.Init(it);
            _config = Locator.Instance.Get<IConstConfig>();
            InitCtx.GetPlayerParser().AddObserver(this);
            Locator.Instance.Deposit(this,m_SpawnSquadQueue = new SpawnSquadQueue());
            Locator.Instance.Deposit(this);
        }

        public override void OnTick(float delat)
        {
            base.OnTick(delat);
            m_SpawnSquadQueue.Tick();
        }

        public override (int, int) Parse(DyMsgOrigin msgOrigin)
        {
            var baseRes = base.Parse(msgOrigin);
            if (baseRes.Item1 > 0) return (0,0);
            var uid = msgOrigin.msg.OpenID;
            var uName = msgOrigin.msg.UserName;
            var user = GetUserData(uid);
            if (msgOrigin.barType == MsgType.Comment)
            {
                var con = msgOrigin.msg.CommentText.Trim();//.ToLower();
                Match match = null;//new Regex("^([0-9a-z]+)$").Match(con);
                if (false && match.Groups.Count == 2)
                {
                    // var resMgr = m_MsgDispatcher.GetResourceMgr();
                    // var list = SquadListPool.Get();
                    // Utils.StringToDictAndForeach(match.Groups[1].Value, (item) =>
                    // {
                    //     if (item.Key >= Aoe4DataConfig.SquadCount) return;
                    //     var sd = Aoe4DataConfig.GetSquad(item.Key);
                    //     if (sd.Invaild) return;
                    //     int id = item.Key;
                    //     int c = item.Value > sd.QuickSuccessionNum ? sd.QuickSuccessionNum : item.Value;
                    //     var res = resMgr.GetResource(uid);
                    //     if (res < sd.Price * c)
                    //         c = res / sd.Price;
                    //     if (c > 0 && resMgr.RemoveResource(uid, sd.Price * c))
                    //     {
                    //         InitCtx.PrintGameMsg($"{msgOrigin.msg.UserName}选择出{c}个{sd.Name}");
                    //         UpdateUserData(msgOrigin.msg.UserID_long, c * sd.Score, c, msgOrigin.msg.UserName, msgOrigin.msg.UserFace);
                    //         if (sd.SquadType != ESquadType.Normal)
                    //             SendSpawnSquad(msgOrigin.msg.UserID_long, id, c, sd);
                    //         else
                    //             list.Add((id, c));
                    //     }
                    //     else
                    //     {
                    //         InitCtx.PrintGameMsg($"{msgOrigin.msg.UserName}金矿不足出兵失败，剩余数量{res}");
                    //     }
                    // });
                    // SendSpawnSquad(uid, list, 1,true);
                }
                else if (con.StartsWith("查"))
                {
                    var resMgr = InitCtx.GetResourceMgr();
                    var res = resMgr.GetResource(msgOrigin.msg.OpenID);
                    var c = DB.DBMgr.Instance.GetUser(uid)?.Honor ?? 0;
                    var canSign = DB.DBMgr.Instance.CanSign(uid);
                    InitCtx.PrintGameMsg($"{user?.NameColored ?? uName}金矿{(int)res},功勋{c}{(canSign ? ",可签到" : "")}");
                }
                else if (con.StartsWith("背包"))
                {
                    PrintAllItems(uid, user?.NameColored ?? uName);
                }
                else if ((match = BooHonorRegex.Match(con)).Success && match.Groups.Count == 3 && int.TryParse(match.Groups[2].Value, out var c))
                {
                    if (user != null && _squadMgr.CanSpawnSquad(user.Id,SpawnSquadType.HonorBoom)) BoomHonor(match.Groups[1].Value,user,c);
                }
                else if ((match = BooHonorRegex2.Match(con)).Success && match.Groups.Count == 2)
                {
                    if (user != null && _squadMgr.CanSpawnSquad(user.Id, SpawnSquadType.HonorBoom)) BoomHonor(match.Groups[1].Value,user);
                }else if ((match = SendBagGiftRegex.Match(con)).Success && match.Groups.Count == 3 &&
                          int.TryParse(match.Groups[2].Value, out c))
                {
                    if(user != null)SendBagGift(match.Groups[1].Value,user,c);
                }else if ((match = SendBagGiftRegex2.Match(con)).Success && match.Groups.Count == 2)
                {
                    if(user != null)SendBagGift(match.Groups[1].Value,user);
                }
                return (0, 0);
            }
            if (msgOrigin.barType == MsgType.GiftSend)
            {
                var ud = GetUserData(msgOrigin.msg.OpenID);
                return SendGift(msgOrigin.msg.GiftName,msgOrigin.msg.GiftCount,msgOrigin.msg.GiftBatteryCount,ud,1);
            }
            if (msgOrigin.barType == MsgType.Interact && msgOrigin.msg.InteractType == InteractTypeEnum.Like && user != null)
            {
                if (user == null) return(0,0);
                InitCtx.GetResourceMgr().AddResource(uid, user.FansLevel + 15);
                InitCtx.PrintGameMsg($"{user.NameColored}点赞获得{user.FansLevel + 15}金");
                return (0,0);
            }
            if (msgOrigin.barType == MsgTypeEnum.GuardBuy)
            {
                if (msgOrigin.msg.UserGuardLevel >= 1)
                {
                    var ud = GetUserData(msgOrigin.msg.OpenID);
                    ud.SetGuardLevel(msgOrigin.msg.UserGuardLevel);
                    var c = 4 - ud.GuardLevel;
                    AddGift(ud, Aoe4DataConfig.Gaobai, 5 * c);
                    AddGift(ud, Aoe4DataConfig.GanBao, 5 * c);
                    AddGift(ud, Aoe4DataConfig.QingShu, 20 * c);
                    AddGift(ud, Aoe4DataConfig.ZheGe, 30 * c);
                    AddGift(ud, Aoe4DataConfig.Xinghe, 5 * c);
                    AddHonor(ud, 1000 * (int)Math.Pow(10,c - 1),false);
                    AddInitAttr(ud);
                    if(msgOrigin.msg.UserGuardLevel <= 2)
                    {
                        AddGift(ud, Aoe4DataConfig.DaCall,          400 * c);
                        AddGift(ud, Aoe4DataConfig.XiaoFuDie,       400 * c);
                        AddGift(ud, Aoe4DataConfig.ShuiJingBall,    10 * c);
                        AddGift(ud, Aoe4DataConfig.KuaKua,          30 * c);
                    }
                }
            }
            return (0, 0);
        }
        
        private void AddInitAttr(UserData ud,StringBuilder sb = null)
        {
            var addAttr = Locator.Instance.Get<IConstConfig>().GetOnPlayerJoinAttributeAddition(ud.RealGuardLevel);
            if (addAttr > 0)
            {
                var dv = ud.AddDamageMultiple(addAttr);
                var hv = ud.AddHpMultiple(addAttr);
                if(sb == null)
                    InitCtx.PrintGameMsg($"{ud.Name}后续部队增加{dv * 10}%伤害、{hv * 10}%血量");
                else
                    sb.Append($"{ud.Name}后续部队增加{dv * 10}%伤害、{hv * 10}%血量，");
            }
        }


        private void PrintAllItems(string uid,string uName)
        {
            var sb = ObjPoolMgr.Instance.Get<StringBuilder>().Get();
            var ls = DBMgr.Instance.GetUserItems(uid,EItemType.Gift | EItemType.Ticket );
            for (int i = 0; i < ls.Count; i++)
            {
                sb.Append($"{ls[i].Name}*{ls[i].Count}");
                if (i < ls.Count - 1)
                    sb.Append(',');
            }
            InitCtx.PrintGameMsg($"{uName}有[{SettingMgr.GetColorWrap(sb.ToString(),2)}]");
            sb.Clear();
            ObjPoolMgr.Instance.Get<StringBuilder>().Return(sb);
        }

        private void SendBagGift(string gift, UserData ud,int c = 1)
        {
            foreach (var it in Aoe4DataConfig.ItemDatas)
            {
                var item = it.Value;
                if (it.Key.StartsWith(gift))
                {
                    if (DB.DBMgr.Instance.DepleteItem(ud.Id, item.Name, c, out var rest) > 0)
                    {
                        SendGift(it.Key, c, item.Price, ud,isRealGift:false);
                        InitCtx.PrintGameMsg($"{ud.NameColored}使用了{c}个{it.Key}，剩余{rest}个");
                    }
                    else
                        InitCtx.PrintGameMsg($"{ud.NameColored}{it.Key}数量不足");
                    break;
                }
            }
        }

        private void BoomHonor(string s, UserData ud, int max = 10)
        {
            var squad = SquadGroup.FromString(s,ud.Group,ud.Id);
            if (squad.IsEmpty) return;
            var user = DB.DBMgr.Instance.GetUser(ud.Id);
            if(user == null) return;
            if(user.Honor < max)
            {
                InitCtx.PrintGameMsg($"{ud.NameColored}没有足够的功勋");
                return;
            }
            var price = (double)squad.price / _config.HonorGoldFactor;
            var c = max / price;
            var count = Math.Truncate(c);
            var dec = c - count;
            if (DB.DBMgr.Instance.DepleteHonor(ud.Id, max))
            {
                InitCtx.PrintGameMsg($"{ud.NameColored}消耗{max}功勋，出了{squad.num * count}个兵");
                var rest = dec * squad.price;
                if(rest > 0)
                {
                    InitCtx.PrintGameMsg($"{ud.NameColored}获得{rest:.00}个金矿");
                    //m_MsgDispatcher.GetResourceMgr().AddResource(ud.Id, rest);
                }
                SpawnManySquadQueue(ud.Id, squad, (int)count, honor: max, restGold:rest, upLevelgold: (int)count * squad.price);
                //GetSubMsgParse<GroupUpLevel<IT>>().NotifyDepleteGold(ud.Group,(int)count * squad.price);
            }
            //SquadData.Recycle(squad);
        }
        private (int, int) SendGift(string giftName, int giftCount,int battery, UserData u,double transHonorfactor = 0.5,bool isRealGift = true)
        {
            /*int c = giftCount;
            int t = 0;
            int id = -1;
            ushort addedAttr = MergeByte(1, 1);
            ushort addedAttrForGroup = MergeByte(1, 1);
            List<(int, int)> Squad = ObjPoolMgr.Instance.Get<List<(int, int)>>(null, DefObjectRecycle.OnListRecycle).Get();
            switch (giftName)
            {
                case "辣条": c *= 15; transHonorfactor = -0.01; break;
                case "人气票": c *= 60; transHonorfactor = -0.01; break;
                case "PK票": c *= 20; transHonorfactor = -0.01; break;
                case "小花花": c *= 30; transHonorfactor = -0.01; break;
                // case "打call": c *= 110; break;
                case "牛哇":
                case "牛哇牛哇": id = 110001; t = 1; c *= 3; break;
                case "干杯": id = 110005; t = 1; c *= 68; break;
                case "棒棒糖": id = 110003; t = 1; c *= 5; addedAttr = MergeByte(2, 3); break;
                case "这个好诶": id = 110004; t = 1; c *= 16; break;
                case "小蛋糕": id = 110016; t = 1; c *= 7; break;
                //case "小蝴蝶": id = 105; t = 1; c *= 8; break;
                case "情书":id = 110007; t = 1;c *= 12; break;
                case "告白花束": Squad.Add((110011, 120)); Squad.Add((200050, 100)); Squad.Add((110007, 36)); t = 3; break;
                case "水晶之恋": id = 110011; t = 1; c *= 21; break;
                case "星河入梦": Squad.Add((300108, 100)); Squad.Add((300109, 50)); Squad.Add((300110, 100));
                    Squad.Add((100100, 100)); t = 3;
                    addedAttrForGroup = addedAttr = MergeByte(3, 2); break;
                case "星愿水晶球": id = 110002; 
                    //if(Aoe4DataConfig.Get(52,2,u.Group) == null)
                    //    Squad.Add((100104, 45));
                    //else
                    //    Squad.Add((200052, 100));
                    //Squad.Add((200050, 100)); Squad.Add((300049, 100)); Squad.Add((200053, 100)); t = 3;
                    //addedAttr = global::InteractionGame.Utils.MergeByte(20, 100);
                    //addedAttrForGroup = global::InteractionGame.Utils.MergeByte(6, 40);
                    break;
                case "花式夸夸": id = 110012; t = 1; c *= 350;addedAttr = MergeByte(3, 5);break;
                case "打call":
                    {
                        if(u.HpMultiple + c > 255)
                        {
                            var sub = u.HpMultiple + c - 255;
                            DB.DBMgr.Instance.AddGiftItem(u.Id,giftName, sub);
                            c -= sub;
                        }
                        t = -1;
                        if (c > 0)
                        {
                            var v = u.AddHpMultiple(1 * c);
                            InitCtx.PrintGameMsg($"{u.NameColored}后续部队增加{v * 10}%血量");
                        }
                        break;
                    }
                case "小蝴蝶":
                    {
                        if (u.DamageMultiple + c > 255)
                        {
                            var sub = u.DamageMultiple + c - 255;
                            DB.DBMgr.Instance.AddGiftItem(u.Id, giftName, sub);
                            c -= sub;
                        }
                        t = -1;
                        if (c > 0)
                        {
                            var v = u.AddDamageMultiple(1 * c);
                            InitCtx.PrintGameMsg($"{u.NameColored}后续部队增加{v * 10}%伤害");
                        }
                        break;
                    }
                case "为你打call":
                    {
                        int count = c;
                        id = 100098; t = 1; c *= 160;
                        if (isRealGift)
                        {
                            var r = DB.DBMgr.Instance.AddLimitedItemEx(u.Id, Aoe4DataConfig.JianZhang, 33, 9999, TimeSpan.FromDays(1 * count));
                            if (r > 0)
                                InitCtx.PrintGameMsg($"{u.NameColored}获得{1 * count}天舰长3倍体验卡,下局生效");
                            AddGift(u, Aoe4DataConfig.NiuWa, 30 * count);
                        }
                        break;
                    }
                case "泡泡机":
                    {
                        AddGift(u, Aoe4DataConfig.XiaoFuDie, 9 * c);
                        transHonorfactor = -1.0f;
                        t = -1;
                        break;
                    }
                default:
                    if (transHonorfactor < 0.0)
                        return (0,0);
                    transHonorfactor = 1.0f;
                    //c *= (battery * 20);
                    t = -1;
                    break;
            }
            int upLevelGold = 0; ;
            int giveHonor = 0;
            int giveHonorMult = global::InteractionGame.Utils.GetNewYearActivity() > 0 ? 2 : 1;
            if (transHonorfactor > 0.0f && battery > 0)
            {
                if (t > 0)
                {
                    upLevelGold = (int)(battery * giftCount * _config.HonorGoldFactor);
                    giveHonor = 0;// (int)Math.Ceiling(transHonorfactor * battery * giftCount * giveHonorMult);
                }
                else {
                    //GetSubMsgParse<GroupUpLevel<IT>>().NotifyDepleteGold(u.Group, (int)(battery * giftCount * Aoe4DataConfig.HonorGoldFactor));
                    var v = (long)Math.Ceiling(transHonorfactor * battery * giftCount * giveHonorMult);
                    AddHonor(u, v);
                }
            }
            Action spawnOneSquad = () =>
            {
                var sd = Aoe4DataConfig.GetSquadBySid(id,u.Group);
                InitCtx.PrintGameMsg($"{u.NameColored}出了{c}个{sd.Name}");
                SendSpawnSquadQueue(u, id, c, sd,battery,giftName,giftCount,giveHonor:giveHonor,upLevelgold:upLevelGold,attribute: addedAttr,priority:10);
            };
            if(t > 0 && !Aoe4DataConfig.CanSpawnSquad(u.Id,Aoe4DataConfig.SpawnSquadType.Gift))
            {
                AddGift(u, giftName, giftCount);
                goto End;
            }
            switch (t)
            {
                case 0:
                    InitCtx.PrintGameMsg($"{u.NameColored}获得{c}个金矿");
                    InitCtx.GetResourceMgr().AddResource(u.Id, c);
                    break;
                case 1:
                    {
                        spawnOneSquad();
                        break;
                    }
                case 3:
                    {
                        if (Squad.Count > 0)
                        {
                            SquadGroup squad = null;
                            SpawnManySquadQueue(u.Id, squad = SquadGroup.FromData(Squad, u.Group).SetAddedAttr(addedAttrForGroup), c, battery, giftName,
                                giftCount, giveHonor: giveHonor, upLevelgold: upLevelGold,priority:10);
                            InitCtx.PrintGameMsg($"{u.NameColored}出了{giftName}*{c}");
                        }
                        if (id >= 0)
                        {
                            var sd = Aoe4DataConfig.GetSquadBySid(id, u.Group);
                            InitCtx.PrintGameMsg($"{u.NameColored}出了{c}个{sd.Name}");
                            SendSpawnSquadQueue(u, id, c, sd, 0, null, 0, giveHonor: 0, upLevelgold: 0, attribute: addedAttr, priority: 10);
                        }
                        break;
                    }
            }
            End:
            ObjPoolMgr.Instance.Get<List<(int,int)>>().Return(Squad);*/
            return (0, 0);
        }

        private void AddHonor(UserData u, long v,bool hasAddition = true)
        {
            if (hasAddition && u.GuardLevel > 0) 
                v += (long)Math.Ceiling(v * Locator.Instance.Get<IConstConfig>().GetPlayerHonorAddition(u.RealGuardLevel));
            if (DB.DBMgr.Instance.AddHonor(u,v) > 0)
                InitCtx.PrintGameMsg($"{u.NameColored}获得{v}功勋");
        }
        private void AddGift(UserData u, string g,int c)
        {
            if(DB.DBMgr.Instance.AddGiftItem(u,g,c) > 0)
                InitCtx.PrintGameMsg($"{u.NameColored}获得{g}*{c}");
        }

        public override void SendSpawnSquad(UserData u, int c, SquadData sd)
        {
            var target = InitCtx.GetPlayerParser().GetTarget(u.Id);
            var self = InitCtx.GetPlayerParser().GetGroupById(u.Id);
            InitCtx.GetBridge().ExecSpawnSquad(u,  sd , c, target);
            Locator.Instance.Get<IGameState>().OnSpawnSquad(self, c * sd.GetCountMulti());
        }
        
        public override void SendSpawnSquadQueue(UserData u, int sid, int c, SquadData sd,int price = 0,string giftName = null,int giftCount = 0,int honor = 0,
            int restGold = 0, int upLevelgold = 0, int giveHonor = 0,ushort attribute = 0, int priority = 0)
        {
            ISpawnSquadAction action = null;
            if (price > 0 && giftName != null)
            {
                action = new SpawnSingleSquadAction<GiftSpawnSquadFallback>(u,c,sd,new GiftSpawnSquadFallback(giftCount,giftName,u,price),
                    restGold,upLevelgold,giveHonor,attribute:attribute).SetPriority(priority);
            }else if (honor > 0)
            {
                action = new SpawnSingleSquadAction<HonorSpawnSquadFallback>(u, c, sd, new HonorSpawnSquadFallback(honor, u),
                    restGold, upLevelgold, giveHonor, attribute: attribute).SetPriority(priority);
            }
            else
            {
                action = new SpawnSingleSquadAction<EmptySpawnSquadFallback>(u,c,sd,null, restGold, upLevelgold, giveHonor,
                    attribute: attribute).SetPriority(priority);
            }
            Locator.Instance.Get<ISpawnSquadQueue>(this).AppendAction(action);
        }


        public override int SendSpawnSquad(UserData u, List<(SquadData,int)> group,int groupCount,int multiple = 1)
        {
            if (group.Count == 0) return 0;
            var target = InitCtx.GetPlayerParser().GetTarget(u.Id);
            var self = InitCtx.GetPlayerParser().GetGroupById(u.Id);
            int rc = InitCtx.GetBridge().ExecSpawnGroup(u, group,multiple,target);
            Locator.Instance.Get<IGameState>().OnSpawnSquad(self, rc);
            return rc;
        }
        

        public override void SpawnManySquadQueue(string uid, SquadGroup v, int c,int price = 0,string giftName = null,int giftCount = 0,int honor = 0,
            double restGold = 0, double upLevelgold = 0, int giveHonor = 0,bool notRecycle = false,int priority = 0)
        {
            var id = 0;
            var u = int.TryParse(uid, out id) && id < 0 ? new UserData(uid,"-","-",(int)(Math.Abs(id) - 1),0,0) : GetUserData(uid);
            ISpawnSquadAction action = null;
            if (price > 0 && giftName != null)
            {
                action = new SpawnGroupSquadAction<GiftSpawnSquadFallback>(u,v,c,
                    new GiftSpawnSquadFallback(giftCount,giftName,u,price), restGold, upLevelgold, giveHonor,notRecycle).SetPriority(priority);
            }else if (honor > 0)
            {
                action = new SpawnGroupSquadAction<HonorSpawnSquadFallback>(u,v,c,
                    new HonorSpawnSquadFallback(honor,u), restGold, upLevelgold, giveHonor, notRecycle).SetPriority(priority);
            }
            else
            {
                action = new SpawnGroupSquadAction<EmptySpawnSquadFallback>(u,v,c,null, restGold, upLevelgold, giveHonor, notRecycle)
                    .SetPriority(priority);
            }
            Locator.Instance.Get<ISpawnSquadQueue>(this).AppendAction(action);
            //UpdateUserData(uid, v.score * c, v.num * c, null, null);
        }

        public void OnAddGroup(UserData userData, int g)
        {
            if (userData.GuardLevel > 0)
            {
                StringBuilder sb = new StringBuilder();
                AddInitAttr(userData,sb);
                sb = sb.Remove(sb.Length - 1,1);
                LargeTips.Show(LargePopTipsDataBuilder.Create(userData.Name,$"加入{Locator.Instance.Get<IConstConfig>().GetGroupName(g + 1)}方")
                    .SetBottom(sb.ToString()).SetBottomColor(LargeTips.Cyan).SetLeftColor(LargeTips.Yellow).SetRightColor(LargeTips.GetGroupColor(g)));
            }
        }

        public void OnChangeGroup(UserData userData, int old, int n)
        {

        }

        public void OnClear()
        {

        }

        public override void Clear()
        {
            base.Clear();
            m_SpawnSquadQueue.OnClear();
        }
    }

    public class AutoSpawnSquadSubMsgParser : ISubMsgParser , IPlayerParserObserver
    {
        
        private ConcurrentDictionary<string,SquadGroup> m_Dict = new ConcurrentDictionary<string,SquadGroup>();
        private ConcurrentDictionary<string,DateTime> m_SendGiftTimeDict = new ConcurrentDictionary<string, DateTime>();
        private IDyMsgParser m_Owner;
        private DateTime m_AutoBoomCkTime = DateTime.Now;
        private readonly Regex Reg = new Regex("^([0-9a-wA-W]*)$");
        private readonly Regex BoomWithCountReg = new Regex("^([0-9a-wA-W]+)[x,\\*,×]([0-9]+)$");
        private readonly Regex BoomOnlyCountReg = new Regex("^[x,\\*,×]([0-9]+)$");
        private IConstConfig _config;
        private ISquadMgr _squadMgr;

        public void Init(IDyMsgParser owner)
        {
            m_Owner = owner;
            m_Owner.InitCtx.GetPlayerParser().AddObserver(this);
            _config = Locator.Instance.Get<IConstConfig>();
            _squadMgr = Locator.Instance.Get<ISquadMgr>();
        }

        public void OnTick(float delat)
        {

            var gameState = Locator.Instance.Get<IGameState>();
            foreach (var it in m_Dict.Keys)
            {
                if(_squadMgr.CanSpawnSquad(it, SpawnSquadType.Auto) && m_Dict.TryGetValue(it,out var v))
                {
                    var ud = m_Owner.GetUserData(it);
                    if (gameState.GetSquadCount(ud.Group) >= _config.AutoSquadCountLimit)
                        continue;
                    lock (v)
                    {
                        var sp = DateTime.Now - v.lastSpawnTime;
                        if (sp.TotalSeconds >= v.spawnTime)
                        {
                            SpawnSquad(it,v);
                            v.lastSpawnTime = v.lastSpawnTime.AddSeconds(v.spawnTime);
                        }
                    }
                }
            }

            if (DateTime.Now - m_AutoBoomCkTime > TimeSpan.FromSeconds(2.0f))
            {
                foreach (var it in m_Dict)
                {
                    if(!_squadMgr.CanSpawnSquad(it.Key, SpawnSquadType.AutoGoldBoom))
                        continue;
                    var lastSendGiftTime = GetSendGiftTime(it.Key);
                    if ((DateTime.Now - lastSendGiftTime).TotalMinutes < 1.5)
                        continue;
                    var gold = m_Owner.InitCtx.GetResourceMgr().GetResource(it.Key);
                    if(gold >= 3000)
                    {
                        AutoBoom(it.Key,it.Value);
                    }
                }
                m_AutoBoomCkTime = DateTime.Now;
            }
        }

        private void AutoBoom(string id, SquadGroup value)
        {
            var ud = m_Owner.GetUserData(id);
            Boom(id, value,ud.NameColored,999999,true,tag:"自动");
        }

        private DateTime GetSendGiftTime(string id)
        {
            if(m_SendGiftTimeDict.TryGetValue(id,out var v))
            {
                return v;
            }
            return Common.MinDateTime;
        }

        private void SpawnSquad(string uid,SquadGroup v)
        {
            if(v.Invaild)
            {
                m_Owner.InitCtx.PrintGameMsg($"{v.uName}请重新设置自动出兵配置");
                return;
            }
            var u = m_Owner.GetUserData(uid);
            foreach (var it in v.specialSquad)
            {
                m_Owner.SendSpawnSquad(u,  it.Item2, it.Item1);
            }
            int rc = m_Owner.SendSpawnSquad(u, v.squad, v.normalCount,1);
            //m_Owner.InitCtx.PrintGameMsg($"{v.uName}自动出兵");
            m_Owner.UpdateUserData(uid, v.score, v.specialCount + rc, null, null);
        }
        

        public void AddDefaultAutoSquad(UserData data,int sdId,int c)
        {
            var squad = SquadGroup.FromString("h", data.Group, data.Id);
            squad.lastSpawnTime = DateTime.Now;
            squad.uName = data.NameColored;
            m_Dict.TryAdd(data.Id, squad);
        }

        public bool Parse(DyMsgOrigin msg)
        {
            if(msg.barType == MsgType.GiftSend)
            {
                m_SendGiftTimeDict[msg.msg.OpenID] = DateTime.Now;
                return false;
            }
            if(msg.barType == MsgType.Comment)
            {
                var uid = msg.msg.OpenID;
                var user = m_Owner.GetUserData(uid);
                if(user == null) return false;
                var lower = msg.msg.CommentText;//.ToLower();
                var match = Reg.Match(lower);
                if (match.Groups.Count == 2)
                {
                    if(match.Groups[1].Value.Length == 0)
                    {
                        if (m_Dict.TryRemove(uid, out var squadData))
                        {
                            SquadGroup.Recycle(squadData);
                            m_Owner.InitCtx.PrintGameMsg($"{user.NameColored}取消自动出兵");
                        }
                        return true;
                    }
                    bool needAdd = false;
                    if (!m_Dict.TryGetValue(uid, out var squad))
                    {
                        squad = new SquadGroup();
                        squad.Init();
                        squad.uName = user.NameColored;
                        squad.lastSpawnTime = DateTime.Now;
                        needAdd = true;
                    }
                    var spawnTime = 0.0;
                    var isEmpty = false;
                    (spawnTime,isEmpty) = ParseStr2SquadGroup(match.Groups[1].Value, uid,needAdd,squad);
                    if(isEmpty)
                    {
                        if (m_Dict.TryRemove(uid, out var squadData))
                        {
                            squadData.Dispose();
                            m_Owner.InitCtx.PrintGameMsg($"{user.NameColored}取消自动出兵");
                        }
                        return true;
                    }
                    if (needAdd) m_Dict.TryAdd(uid, squad);
                    m_Owner.InitCtx.PrintGameMsg($"{user.NameColored}设置自动出兵，出兵时间{spawnTime:.00}秒");
                    m_Owner.UpdateUserData(uid, 1, 0, user.Name, msg.msg.UserFace);
                    return true;
                }else if (lower.StartsWith("暴") || lower.StartsWith("爆"))
                {
                    if (!_squadMgr.CanSpawnSquad(uid, SpawnSquadType.GoldBoom))
                        return true;
                    var maxCount = 5000;
                    bool isNew = false;
                    if(!m_Dict.TryGetValue(uid,out var squad))
                    {
                        m_Owner.InitCtx.PrintGameMsg($"{msg.msg.UserName}需要先设置自动出兵才能暴兵");
                        return true;
                    }
                    if(lower.Length > 1 && ((match = Reg.Match(lower.Substring(1))).Success 
                        || (match = BoomWithCountReg.Match(lower.Substring(1))).Success))
                    {
                        var sg = new SquadGroup();
                        sg.Init();
                        sg.uName = user.NameColored;
                        sg.lastSpawnTime = DateTime.Now;
                        var (spawnTime, isEmpty) = ParseStr2SquadGroup(match.Groups[1].Value, uid, true, sg);
                        if (!isEmpty)
                        {
                            squad = sg;
                            isNew = true;
                        }
                        if (match.Groups.Count > 2 && int.TryParse(match.Groups[2].Value, out var c_))
                            maxCount = c_;
                    }
                    else if(lower.Length > 1 && (match = BoomOnlyCountReg.Match(lower.Substring(1))).Success)
                    {
                        if (match.Groups.Count > 1 && int.TryParse(match.Groups[1].Value, out var c_))
                            maxCount = c_;
                    }
                    Boom(uid,squad,user.NameColored,maxCount,!isNew);
                    return true;
                }
            }
            return false;
        }

        private (double,bool) ParseStr2SquadGroup(string str,string uid,bool needAdd,SquadGroup squad)
        {
            double spawnTime = 0.0;
            bool isEmpty = false;
            lock (squad)
            {
                if(!needAdd)squad.Reset();
                squad.StringTag = str;
                StringToDictAndForeach(str, (item) =>
                {
                    var u = m_Owner.InitCtx.GetMsgParser().GetUserData(uid);
                    var sd = _squadMgr.GetSquadBySlot(item.Key,u);
                    if (sd == null) return;
                    if(sd.SquadType_e == ESquadType.Normal)
                        squad.squad.Add((sd, item.Value));
                    else
                    {
                        squad.specialSquad.Add((sd, item.Value));
                        squad.specialCount += item.Value;
                        squad.specialScore += sd.Score * item.Value;
                    }
                    squad.spawnTime += sd.TrainTime * item.Value;
                    squad.score += sd.Score * item.Value;
                    squad.num += item.Value;
                    squad.price += sd.Price * item.Value;
                });
                spawnTime = squad.spawnTime;
                isEmpty = squad.IsEmpty;
            }
            return (spawnTime,isEmpty);
        }

        private void Boom(string uid,int maxCount,string uName = null)
        {
            if (m_Dict.TryGetValue(uid, out var squad))
            {
                var resMgr = m_Owner.InitCtx.GetResourceMgr();
                var g = resMgr.GetResource(uid);
                var c = (int)(g / squad.price);
                if(c > maxCount) c = maxCount;
                if (c == 0)
                {
                    m_Owner.InitCtx.PrintGameMsg($"{squad.uName}没有足够的资源暴兵");
                }
                else
                {
                    if (resMgr.RemoveResource(uid,c * squad.price))
                    {
                        m_Owner.InitCtx.PrintGameMsg($"{squad.uName}暴兵{squad.StringTag}x{c}组");
                        m_Owner.SpawnManySquadQueue(uid, squad.Clone() as SquadGroup, c,upLevelgold: c * squad.price,notRecycle:false);
                    }
                }
            }
            else
            {
                m_Owner.InitCtx.PrintGameMsg($"{uName}需要先设置自动出兵才能暴兵");
            }
        }

        private void Boom(string uid, SquadGroup squad, string uName = null,int maxCount = 5000,bool needClone = false,string tag = null)
        {
            if (squad != null && !squad.IsEmpty )
            {
                var resMgr = m_Owner.InitCtx.GetResourceMgr();
                var g = resMgr.GetResource(uid);
                var c = (int)(g / squad.price);
                if (c > maxCount) c = maxCount;
                if (c == 0)
                {
                    m_Owner.InitCtx.PrintGameMsg($"{squad.uName}没有足够的资源暴兵");
                }
                else
                {
                    if (resMgr.RemoveResource(uid, c * squad.price))
                    {
                        m_Owner.InitCtx.PrintGameMsg($"{squad.uName}{(tag != null ? tag : "")}暴兵{squad.StringTag}x{c}组");
                        var ud = m_Owner.GetUserData(uid);
                        m_Owner.SpawnManySquadQueue(tag != null ? (-(ud.Group + 1)).ToString() : uid, needClone ? squad.Clone() as SquadGroup : squad, c, upLevelgold: c * squad.price, notRecycle: false);
                        //m_Owner.GetSubMsgParse<GroupUpLevel<IT>>().NotifyDepleteGold(
                        //    m_Owner.m_MsgDispatcher.GetPlayerParser().GetGroupById(uid),c * squad.price);
                    }
                }
            }
            else
            {
                m_Owner.InitCtx.PrintGameMsg($"{uName}需要先设置自动出兵才能暴兵");
            }
        }

        public void OnStartGame()
        {
            
        }

        public void Stop()
        {
            m_Owner = null;
        }

        public void OnClear()
        {
            foreach (var it in m_Dict)
            {
                it.Value.Dispose();
            }
            m_Dict.Clear();
        }

        public void OnAddGroup(UserData userData, int g)
        {
            AddDefaultAutoSquad(userData,48,3);
        }

        public void OnChangeGroup(UserData userData, int old, int n)
        {

        }

        public float GetSpawnProgress(string uid)
        {
            if (m_Dict.TryGetValue(uid, out var v))
            {
                lock(v)
                {
                    return ((float)((float)(DateTime.Now - v.lastSpawnTime).TotalSeconds / v.spawnTime));
                }
            }
            return 0.0f;
        }

        public void OnSquadUpLevel(string uid, short sid, byte lvl, SquadData old, SquadData @new)
        {
            if (m_Dict.TryGetValue(uid, out var v))
            {
                var ud = m_Owner.GetUserData(uid);
                if (CkContains(v, ud, old, @new))
                {
                    ParseStr2SquadGroup(v.StringTag, uid, false, v);
                }
            }
        }

        private bool CkContains(SquadGroup squadGroup, UserData ud, SquadData old, SquadData @new)
        {
            for (int i = 0; i < squadGroup.squad.Count; i++)
            {
                if (squadGroup.squad[i].Item1.RealId == old.RealId)
                    return true;
            }

            for (int i = 0; i < squadGroup.specialSquad.Count; i++)
            {
                if(squadGroup.specialSquad[i].Item1.RealId == old.RealId)
                    return true;
            }

            return false;
        }

        public void Start()
        {

        }
    }

}
