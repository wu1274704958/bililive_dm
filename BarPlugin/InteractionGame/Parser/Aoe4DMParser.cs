using BilibiliDM_PluginFramework;

using InteractionGame;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using BililiveDebugPlugin.DB;
using BililiveDebugPlugin.DB.Model;
using BililiveDebugPlugin.InteractionGameUtils;
using conf.Squad;
using Utils;
using UserData = InteractionGame.UserData;
using InteractionGame.plugs.config;
using InteractionGame.plugs;
using InteractionGame.Parser;

namespace BililiveDebugPlugin.InteractionGame.Parser
{
    using Msg = DanmakuModel;
    using MsgType = MsgTypeEnum;

    public class MsgGiftParser<IT> : IDyMsgParser, IPlayerParserObserver
        where IT : class, IContext
    {
        private IConstConfig _config;
        private ISquadMgr _squadMgr;
        private IActivityMgr _activityMgr;
        private IGiftMgr _giftMgr;
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
            AddSubMsgParse(new SignInSubMsgParser(),false);
            //AddSubMsgParse(new GroupUpLevel());
            AddSubMsgParse(new AdminParser(),false);
            AddSubMsgParse(new BossGameModeParser());
            AddSubMsgParse(new PopularityTicketActivityParser(), false);
            base.Init(it);
            _config = Locator.Get<IConstConfig>();
            InitCtx.GetPlayerParser().AddObserver(this);
            Locator.Deposit(this,m_SpawnSquadQueue = new SpawnSquadQueue());
            Locator.Deposit(this);

            _giftMgr = Locator.Get<IGiftMgr>();
            _squadMgr = Locator.Get<ISquadMgr>();
            _activityMgr = Locator.Get<IActivityMgr>();
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
                if (con.StartsWith("查"))
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
                _giftMgr.ApplyGift(msgOrigin.msg.GiftName, ud, msgOrigin.msg.GiftCount);
                InitCtx.PrintGameMsg($"{user.NameColored}使用了{msgOrigin.msg.GiftName}*{msgOrigin.msg.GiftCount}");
            }
            if (msgOrigin.barType == MsgType.Interact && msgOrigin.msg.InteractType == InteractTypeEnum.Like && user != null)
            {
                if (user == null) return(0,0);
                _activityMgr.ApplyActivity(conf.Activity.EItemType.DoLike, user);
                return (0,0);
            }
            if (msgOrigin.barType == MsgTypeEnum.GuardBuy)
            {
                if (msgOrigin.msg.UserGuardLevel >= 1)
                {
                    foreach(var i in _config.GuardLevelListSorted)
                    {
                        if(i.Key == msgOrigin.msg.UserGuardLevel)
                        {
                            _giftMgr.ApplyGift(_activityMgr.GetOverride(conf.Activity.EItemType.Gift,user,i.Value),user,1);
                            break;
                        }
                    }
                }
            }
            return (0, 0);
        }
        
        private void AddInitAttr(UserData ud,StringBuilder sb = null)
        {
            var addAttr = Locator.Get<IConstConfig>().GetOnPlayerJoinAttributeAddition(ud.RealGuardLevel);
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
            foreach (var it in conf.Gift.GiftItemMgr.GetInstance().Dict)
            {
                var item = it.Value;
                if (it.Key.StartsWith(gift))
                {
                    if (DB.DBMgr.Instance.DepleteItem(ud.Id, item.Id, c, out var rest) > 0)
                    {
                        _giftMgr.ApplyGift(_activityMgr.GetOverride(conf.Activity.EItemType.Gift,ud,item.Id),ud, c);
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
            var squad = SquadGroup.FromString(s.Substring(0,1),ud.Group,ud.Id);
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

        private void AddHonor(UserData u, long v,bool hasAddition = true)
        {
            if (hasAddition && u.GuardLevel > 0) 
                v += (long)Math.Ceiling(v * Locator.Get<IConstConfig>().GetPlayerHonorAddition(u.RealGuardLevel));
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
            Locator.Get<IGameState>().OnSpawnSquad(self, c * sd.GetCountMulti());
        }
        
        public override void SendSpawnSquadQueue(UserData u,SquadData sd,int c,int price = 0,string giftName = null,int giftCount = 0,int honor = 0,
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
            Locator.Get<ISpawnSquadQueue>(this).AppendAction(action);
        }


        public override int SendSpawnSquad(UserData u, List<(SquadData,int)> group,int groupCount,int multiple = 1)
        {
            if (group.Count == 0) return 0;
            var target = InitCtx.GetPlayerParser().GetTarget(u.Id);
            var self = InitCtx.GetPlayerParser().GetGroupById(u.Id);
            int rc = InitCtx.GetBridge().ExecSpawnGroup(u, group, target,multiple);
            Locator.Get<IGameState>().OnSpawnSquad(self, rc);
            return rc;
        }
        

        public override void SpawnManySquadQueue(string uid, SquadGroup v, int c,int price = 0,string giftName = null,int giftCount = 0,int honor = 0,
            double restGold = 0, double upLevelgold = 0, int giveHonor = 0,bool notRecycle = false,int priority = 0)
        {
            var u = GetUserData(uid);
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
            Locator.Get<ISpawnSquadQueue>(this).AppendAction(action);
            //UpdateUserData(uid, v.score * c, v.num * c, null, null);
        }

        public void OnAddGroup(UserData userData, int g)
        {
            if (userData.GuardLevel > 0)
            {
                StringBuilder sb = new StringBuilder();
                AddInitAttr(userData,sb);
                if(sb.Length > 0)
                    sb = sb.Remove(sb.Length - 1,1);
                LargeTips.Show(LargePopTipsDataBuilder.Create(userData.Name,$"加入{Locator.Get<IConstConfig>().GetGroupName(g + 1)}方")
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
}
