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
using BililiveDebugPlugin.InteractionGameUtils;
using Utils;
using static InteractionGame.Utils;

namespace BililiveDebugPlugin.InteractionGame.Parser
{
    using Msg = DanmakuModel;
    using MsgType = MsgTypeEnum;
    public class PlayerBirthdayParser<IT> : IDyPlayerParser<IT>
         where IT : class, IContext
    {
        public override bool Demand(Msg msg, MsgType barType)
        {
            return StaticMsgDemand.Demand(msg, barType) || barType == MsgType.Welcome;
        }
        
        public override int Parse(DyMsgOrigin msgOrigin)
        {
            if (msgOrigin == null) return 0;
            if (msgOrigin.msg.CommentText == null)
                msgOrigin.msg.CommentText = "";
            // 系统选择阵营
            var uid = msgOrigin.msg.UserID_long;
            var con = msgOrigin.msg.CommentText.Trim();
            var uName = msgOrigin.msg.UserName;
            int g = -1;
            if (!HasGroup(uid))
            {
                g = ParseJoinGroup(uid, con, msgOrigin);
                if (g != -1)
                {
                    InitCtx.PrintGameMsg($"{uName}加入{DebugPlugin.GetColorById(g + 1)}方");
                    m_MsgDispatcher.GetResourceMgr().AddAutoResourceById(uid, Aoe4DataConfig.PlayerResAddFactorArr[msgOrigin.msg.GuardLevel]);
                }
                else
                {
                    InitCtx.PrintGameMsg($"{uName}请先发“加入”，加入游戏");
                }
            }
            else
            {
                g = GetGroupById(uid);
                TryParseChangTarget(uid, con, uName, false);
            }

            return g;
            // 自由选择阵营
            ParseChooseGroup(uid, con, uName);
            var v = GetGroupById(uid);
            var str = "";
            if (v == -1)
            {
                v = new Random((int)DateTime.Now.Ticks).Next(0, Aoe4DataConfig.GroupCount);
                str = "随机加入";
                SetGroup(msgOrigin.msg.UserID_long, v);
                SetTarget(msgOrigin.msg.UserID_long, -1);
            }
            if (msgOrigin.barType == MsgType.Welcome)
            {
                InitCtx.PrintGameMsg($"欢迎{msgOrigin.msg.UserName}进入直播间，{str}阵营{DebugPlugin.GetColorById(v + 1)}方");
            }
            m_MsgDispatcher.GetResourceMgr().AddAutoResourceById(msgOrigin.msg.UserID_long);
            return v;
        }
        public static DateTime GetDateTimeFromSeconds(long sec)
        {
            DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
            TimeSpan time = TimeSpan.FromSeconds(sec);
            return startTime.Add(time);
        }

        public override int GetGroupExclude(int g)
        {
            return Aoe4DataConfig.GetGroupExclude(g);
        }

        public override int GetGroupCount()
        {
            return Aoe4DataConfig.GroupCount;
        }
    }

    public class MsgGiftParser<IT> : IDyMsgParser<IT>, IPlayerParserObserver
        where IT : class, IContext
    {
        public Utils.ObjectPool<List<(int, int)>> SquadListPool { get; private set; } = new Utils.ObjectPool<List<(int, int)>>(
            () => new List<(int, int)>(), (a) => a.Clear());
        private Regex BooHonorRegex = new Regex("z([0-9a-w]+)x([0-9]+)");
        private Regex BooHonorRegex2 = new Regex("z([0-9a-w]+)");
        private Regex SendBagGiftRegex = new Regex("送(.+)x([0-9]+)");
        private Regex SendBagGiftRegex2 = new Regex("送(.+)");
        private SpawnSquadQueue m_SpawnSquadQueue;

        public override bool Demand(Msg msg, MsgType barType)
        {
            return StaticMsgDemand.Demand(msg, barType);
        }

        public override void Init(IT it, ILocalMsgDispatcher<IT> dispatcher)
        {
            AddSubMsgParse(new AutoSpawnSquadSubMsgParser<IT>());
            AddSubMsgParse(new SignInSubMsgParser<IT>());
            AddSubMsgParse(new GroupUpLevel<IT>());
            base.Init(it, dispatcher);
            m_MsgDispatcher.GetPlayerParser().AddObserver(this);
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
            var uid = msgOrigin.msg.UserID_long;
            var uName = msgOrigin.msg.UserName;
            if (msgOrigin.barType == MsgType.Comment)
            {
                var con = msgOrigin.msg.CommentText.Trim().ToLower();
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
                    var resMgr = m_MsgDispatcher.GetResourceMgr();
                    var res = resMgr.GetResource(msgOrigin.msg.UserID_long);
                    var c = DB.DBMgr.Instance.GetUser(uid)?.Honor ?? 0;
                    InitCtx.PrintGameMsg($"{msgOrigin.msg.UserName}金矿{res},功勋{c}");
                }
                else if (false && (con[0] == '防' || con[0] == '撤'))
                {
                    var self = m_MsgDispatcher.GetPlayerParser().GetGroupById(uid);
                    if (self > -1)
                    {
                        var tar = con.Contains("金") ? self + 10 : self;
                        m_MsgDispatcher.GetPlayerParser().SetTarget(uid, tar);
                        InitCtx.PrintGameMsg($"{uName}选择{con}");
                        SendAllSquadAttack(tar, uid, con[0] == '撤');
                    }
                }
                else if (con[0] == '攻')
                {
                    var isGold = con.Length > 1 && con[1] == '金';
                    if (!isGold && con.Length > 1) return (0,0);
                    var pp = m_MsgDispatcher.GetPlayerParser();
                    var target = pp.GetTarget(uid);
                    var self = pp.GetGroupById(uid);
                    if(target == self || target == self + 10)
                    {
                        target = pp.SetTarget(uid,pp.GetGroupExclude(self) + (isGold ? 10 : 0));
                    }
                    if (isGold && target >= 0 && target < 10)
                    {
                        target = pp.SetTarget(uid, target + 10);
                    }
                    SendAllSquadAttack(target, uid);
                    InitCtx.PrintGameMsg($"{uName}选择{con}");
                }else if (con.StartsWith("背包"))
                {
                    PrintAllItems(uid,uName);
                }
                else if ((match = BooHonorRegex.Match(con)).Success && match.Groups.Count == 3 && int.TryParse(match.Groups[2].Value, out var c))
                {
                    var ud = GetUserData(uid);
                    BoomHonor(match.Groups[1].Value,ud,c);
                }
                else if ((match = BooHonorRegex2.Match(con)).Success && match.Groups.Count == 2)
                {
                    var ud = GetUserData(uid);
                    BoomHonor(match.Groups[1].Value,ud);
                }else if ((match = SendBagGiftRegex.Match(con)).Success && match.Groups.Count == 3 &&
                          int.TryParse(match.Groups[2].Value, out c))
                {
                    var ud = GetUserData(uid);
                    if(ud != null)SendBagGift(match.Groups[1].Value,ud,c);
                }else if ((match = SendBagGiftRegex2.Match(con)).Success && match.Groups.Count == 2)
                {
                    var ud = GetUserData(uid);
                    if(ud != null)SendBagGift(match.Groups[1].Value,ud);
                }
                return (0, 0);
            }
            if (msgOrigin.barType == MsgType.GiftSend)
            {
                var ud = GetUserData(msgOrigin.msg.UserID_long);
                return SendGift(msgOrigin.msg.GiftName,msgOrigin.msg.GiftCount,msgOrigin.msg.GiftBatteryCount,ud,1);
            }
            if (msgOrigin.barType == MsgType.Interact && msgOrigin.msg.InteractType == InteractTypeEnum.Like)
            {
                var r = new Random((int)DateTime.Now.Ticks);
                var id = r.Next(0, Aoe4DataConfig.SquadCount);
                var sd = Aoe4DataConfig.GetSquad(id);
                var c = r.Next(1, sd.QuickSuccessionNum + 1);
                InitCtx.PrintGameMsg($"{msgOrigin.msg.UserName}点赞随机出了{c}个{sd.Name}");
                var u = GetUserData(msgOrigin.msg.UserID_long);
                //UpdateUserData(msgOrigin.msg.UserID_long, c * sd.Score, c, msgOrigin.msg.UserName, msgOrigin.msg.UserFace);
                //SendSpawnSquad(u, id, c, sd);
                Locator.Instance.Get<ISpawnSquadQueue>(this).AppendAction(new SpawnSingleSquadAction<EmptySpawnSquadFallback>(u,id,c,sd,null,0,0,0));
                return (0, 0);
            }
            if (msgOrigin.barType == MsgTypeEnum.GuardBuy)
            {
                if (msgOrigin.msg.UserGuardLevel == 3)
                {
                    var ud = GetUserData(msgOrigin.msg.UserID_long);
                    AddGift(ud, Aoe4DataConfig.Gaobai, 5);
                    AddGift(ud, Aoe4DataConfig.GanBao, 20);
                    AddGift(ud, Aoe4DataConfig.QingShu, 20);
                    AddGift(ud, Aoe4DataConfig.ZheGe, 30);
                    AddGift(ud, Aoe4DataConfig.Xinghe, 5);
                    AddHonor(ud, 1000);
                    GivePlayerUpgrade(ud, "UPG.COMMON.UPGRADE_RANGED_INCENDIARY");
                    m_MsgDispatcher.GetResourceMgr().AddAutoResourceAddFactor(ud.Id,Aoe4DataConfig.PlayerResAddFactorArr[msgOrigin.msg.UserGuardLevel]);
                }
            }
            return (0, 0);
        }

        private void PrintAllItems(long uid,string uName)
        {
            var sb = ObjPoolMgr.Instance.Get<StringBuilder>().Get();
            var ls = DBMgr.Instance.GetUserItems(uid, 10);
            for (int i = 0; i < ls.Count; i++)
            {
                sb.Append($"{ls[i].Name}*{ls[i].Count}");
                if (i < ls.Count - 1)
                    sb.Append(',');
            }
            InitCtx.PrintGameMsg($"{uName}有{sb}");
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
                        SendGift(gift, c, item.Price, ud);
                        InitCtx.PrintGameMsg($"{ud.Name}使用了{c}个{gift}，剩余{rest}个");
                    }
                    else
                        InitCtx.PrintGameMsg($"{ud.Name}{gift}数量不足");
                }
            }
        }

        private void BoomHonor(string s, UserData ud, int max = 10)
        {
            var squad = SquadData.FromString(s);
            if (squad.IsEmpty) return;
            var user = DB.DBMgr.Instance.GetUser(ud.Id);
            if(user == null) return;
            if(user.Honor < max)
            {
                InitCtx.PrintGameMsg($"{ud.Name}没有足够的功勋");
                return;
            }
            var price = (double)squad.price / Aoe4DataConfig.HonorGoldFactor;
            var c = max / price;
            var count = Math.Truncate(c);
            var dec = c - count;
            if (DB.DBMgr.Instance.DepleteHonor(ud.Id, max))
            {
                InitCtx.PrintGameMsg($"{ud.Name}消耗{max}功勋，出了{squad.num * count}个兵");
                var rest = (int)Math.Ceiling(dec * Aoe4DataConfig.HonorGoldFactor);
                if(rest > 0)
                {
                    InitCtx.PrintGameMsg($"{ud.Name}获得{rest}个金矿");
                    //m_MsgDispatcher.GetResourceMgr().AddResource(ud.Id, rest);
                }
                SpawnManySquadQueue(ud.Id, squad, (int)count, honor: max, restGold:rest, upLevelgold: (int)count * squad.price);
                //GetSubMsgParse<GroupUpLevel<IT>>().NotifyDepleteGold(ud.Group,(int)count * squad.price);
            }
            //SquadData.Recycle(squad);
        }

        private (int, int) SendGift(string giftName, int giftCount,int battery, UserData u,double transHonorfactor = 0.5)
        {
            int c = giftCount;
            int t = 0;
            int id = 0;
            List<(int,int)> SpecialSquad = ObjPoolMgr.Instance.Get<List<(int, int)>>(null, SquadData.OnClearList).Get();
            List<(int, int)> Squad = ObjPoolMgr.Instance.Get<List<(int, int)>>(null, null).Get();
            switch (giftName)
            {
                case "辣条": c *= 15; transHonorfactor = -0.01; break;
                case "人气票": c *= 26; break;
                case "PK票": c *= 20; transHonorfactor = -0.01; break;
                // case "小花花": c *= 20; break;
                // case "打call": c *= 110; break;
                case "牛哇牛哇": id = 100; t = 1; break;
                case "干杯": id = 106; t = 1; c *= 18; break;
                case "棒棒糖": id = 102; t = 1; c *= 2; break;
                case "这个好诶": id = 104; t = 1; c *= 12; break;
                case "小蛋糕": id = 107; t = 1; c *= 18; break;
                case "小蝴蝶": id = 105; t = 1; c *= 8; break;
                case "情书":id = 108; t = 1;c *= 18; break;
                case "告白花束": Squad.Add((113, 120)); Squad.Add((116, 100)); SpecialSquad.Add((109, 36)); t = 3; break;
                case "水晶之恋": id = 113; t = 1; c *= 17; break;
                case "星河入梦": Squad.Add((117, 100)); Squad.Add((118, 50)); SpecialSquad.Add((119, 100)); SpecialSquad.Add((14, 100)); t = 3; break;
                case "动鳗电池":
                    {
                        t = -1;
                        var a = (u.Op1 >> 8) & 15;
                        var d = (u.Op1 >> 12) & 15;
                        a += 1;
                        d += 1;
                        var op = u.Op1 | (a << 8);
                        op |= (d << 12);
                        Interlocked.Exchange(ref u.Op1, op);
                        break;
                    }
                case "花式夸夸":
                    {
                        t = -1;
                        var a = (u.Op1 >> 16) & 15;
                        var d = (u.Op1 >> 20) & 15;
                        a += 1;
                        d += 1;
                        var op = u.Op1 | (a << 16);
                        op |= (d << 20);
                        Interlocked.Exchange(ref u.Op1, op);
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
            if (transHonorfactor > 0.0f && battery > 0)
            {
                upLevelGold = (int)(battery * giftCount * Aoe4DataConfig.HonorGoldFactor);
                giveHonor = (int)Math.Ceiling(transHonorfactor * battery * giftCount);
                //GetSubMsgParse<GroupUpLevel<IT>>().NotifyDepleteGold(u.Group, (int)(battery * giftCount * Aoe4DataConfig.HonorGoldFactor));
                //var v = (long)Math.Ceiling(transHonorfactor * battery * giftCount);
                //AddHonor(u, v);
            }
            switch (t)
            {
                case 0:
                    InitCtx.PrintGameMsg($"{u.Name}获得{c}个金矿");
                    m_MsgDispatcher.GetResourceMgr().AddResource(u.Id, c);
                    break;
                case 1:
                    {
                        var sd = Aoe4DataConfig.GetSquad(id);
                        InitCtx.PrintGameMsg($"{u.Name}出了{c}个{sd.Name}");
                        SendSpawnSquadQueue(u, id, c, sd,battery,giftName,giftCount,giveHonor:giveHonor,upLevelgold:upLevelGold);
                        break;
                    }
                case 3:
                    {
                        if(SpecialSquad.Count > 0 || Squad.Count > 0)
                        {
                            SquadData squad = null;
                            SpawnManySquadQueue(u.Id, squad = SquadData.FromData(Squad, SpecialSquad), c,battery,giftName,giftCount, giveHonor: giveHonor, upLevelgold: upLevelGold);
                            InitCtx.PrintGameMsg($"{u.Name}出了{giftName}*{c}");
                        }
                    }
                    break;
            }
            return (0, 0);
        }

        private void AddHonor(UserData u, long v)
        {
            if (u.GuardLevel > 0) v += (long)Math.Ceiling(v * Aoe4DataConfig.PlayerResAddFactorArr[u.GuardLevel]);
            if (DB.DBMgr.Instance.AddHonor(u,v) > 0)
                InitCtx.PrintGameMsg($"{u.Name}获得{v}功勋");
        }
        private void AddGift(UserData u, string g,int c)
        {
            if(DB.DBMgr.Instance.AddGiftItem(u,g,c) > 0)
                InitCtx.PrintGameMsg($"{u.Name}获得{g}*{c}");
        }

        public void SendSpawnSquad(UserData u, int sid, int c, Aoe4DataConfig.SquadData sd)
        {
            if (sid == Aoe4DataConfig.VillagerID)
            {
                m_MsgDispatcher.GetResourceMgr().SpawnVillager(u.Id, c);
                return;
            }
            var target = m_MsgDispatcher.GetPlayerParser().GetTarget(u.Id);
            var self = m_MsgDispatcher.GetPlayerParser().GetGroupById(u.Id);
            var attackTy = sd.SquadType >= ESquadType.SiegeAttacker ? ((int)sd.SquadType) : 0;
            if (target < 0)
            {
                m_MsgDispatcher.GetBridge().ExecSpawnSquad(self + 1, sid, c, u.Id, attackTy,u?.Op1 ?? 0);
            }
            else
            {
                m_MsgDispatcher.GetBridge().ExecSpawnSquadWithTarget(self + 1, sid, target + 1, c, u.Id,attackTy,u?.Op1 ?? 0);
            }
        }
        
        public void SendSpawnSquadQueue(UserData u, int sid, int c, Aoe4DataConfig.SquadData sd,int price = 0,string giftName = null,int giftCount = 0,int honor = 0,
            int restGold = 0, int upLevelgold = 0, int giveHonor = 0)
        {
            ISpawnSquadAction action = null;
            if (price > 0 && giftName != null)
            {
                action = new SpawnSingleSquadAction<GiftSpawnSquadFallback>(u,sid,c,sd,new GiftSpawnSquadFallback(giftCount,giftName,u,price),
                    restGold,upLevelgold,giveHonor);
            }else if (honor > 0)
            {
                action = new SpawnSingleSquadAction<HonorSpawnSquadFallback>(u,sid,c,sd,new HonorSpawnSquadFallback(honor,u),
                    restGold, upLevelgold, giveHonor);
            }
            else
            {
                action = new SpawnSingleSquadAction<EmptySpawnSquadFallback>(u,sid,c,sd,null, restGold, upLevelgold, giveHonor);
            }
            Locator.Instance.Get<ISpawnSquadQueue>(this).AppendAction(action);
        }


        public void SendSpawnSquad(UserData u, List<(int,int)> group,int multiple = 1,bool recovery = false)
        {
            if (group.Count == 0) return;
            var target = m_MsgDispatcher.GetPlayerParser().GetTarget(u.Id);
            var self = m_MsgDispatcher.GetPlayerParser().GetGroupById(u.Id);
            if (target < 0)
            {
                m_MsgDispatcher.GetBridge().ExecSpawnGroup(self + 1, group, u.Id,multiple,op1:u?.Op1 ?? 0);
            }
            else
            {
                m_MsgDispatcher.GetBridge().ExecSpawnGroupWithTarget(self + 1, target + 1, group, u.Id,multiple,op1:u?.Op1 ?? 0);
            }
            if(recovery)SquadListPool.Return(group);
        }


        public override void SendAllSquadAttack(int target, long uid, bool isMove = false)
        {
            var self = m_MsgDispatcher.GetPlayerParser().GetGroupById(uid);
            if (target < 0)
            {
                m_MsgDispatcher.GetBridge().ExecAllSquadMove(self + 1, uid);
            }
            else
            {
                m_MsgDispatcher.GetBridge().ExecAllSquadMoveWithTarget(self + 1, target + 1, uid, isMove ? 1 : 0);
            }
        }

        public void GivePlayerUpgrade(UserData u, string upg)
        {
            m_MsgDispatcher.GetBridge().AppendExecCode($"GiveAbility(PLAYERS[{u.Group + 1}].id, nil, nil, {upg});");
        }
        

        public void SpawnManySquadQueue(long uid, SquadData v, int c,int price = 0,string giftName = null,int giftCount = 0,int honor = 0,
            int restGold = 0, int upLevelgold = 0, int giveHonor = 0,bool notRecycle = false)
        {
            var u = GetUserData(uid);
            ISpawnSquadAction action = null;
            if (price > 0 && giftName != null)
            {
                action = new SpawnGroupSquadAction<GiftSpawnSquadFallback>(u,v,c,new GiftSpawnSquadFallback(giftCount,giftName,u,price), restGold, upLevelgold, giveHonor,notRecycle);
            }else if (honor > 0)
            {
                action = new SpawnGroupSquadAction<HonorSpawnSquadFallback>(u,v,c,new HonorSpawnSquadFallback(honor,u), restGold, upLevelgold, giveHonor, notRecycle);
            }
            else
            {
                action = new SpawnGroupSquadAction<EmptySpawnSquadFallback>(u,v,c,null, restGold, upLevelgold, giveHonor, notRecycle);
            }
            Locator.Instance.Get<ISpawnSquadQueue>(this).AppendAction(action);
            //UpdateUserData(uid, v.score * c, v.num * c, null, null);
        }

        public void OnAddGroup(UserData userData, int g)
        {
            if (userData.GuardLevel > 0)
                GivePlayerUpgrade(userData, "UPG.COMMON.UPGRADE_RANGED_INCENDIARY");
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

    public class SquadData
    {
        public List<(int, int)> squad;
        public List<(int, int)> specialSquad;
        public double spawnTime;
        public DateTime lastSpawnTime;
        public int score;
        public int price;
        public string uName;
        public int num;
        public int specialCount;
        public int normalCount => num - specialCount;
        public int specialScore;
        public int normalScore => score - specialScore;
        public bool IsEmpty => squad.Count == 0 && specialSquad.Count == 0;
        public void Reset()
        {
            squad.Clear();
            specialSquad.Clear();
            spawnTime = 0.0f;
            lastSpawnTime = DateTime.Now;
            score = 0;
            price = 0;
            num = 0;
            specialCount = 0;
            specialScore = 0;
        }

        public static void OnClearList(List<(int,int)> list)
        {
            list.Clear();
        }
        public static SquadData FromString(string s)
        {
            var squad = new SquadData();
            squad.squad = ObjPoolMgr.Instance.Get<List<(int, int)>>(null, OnClearList).Get();
            squad.specialSquad = ObjPoolMgr.Instance.Get<List<(int, int)>>().Get();
            StringToDictAndForeach(s, (item) =>
            {
                if (item.Key >= Aoe4DataConfig.SquadCount) return;
                var sd = Aoe4DataConfig.GetSquad(item.Key);
                if (sd.Invaild || sd.TrainTime < 0.0f) return;
                if(sd.SquadType == ESquadType.Normal)
                    squad.squad.Add((item.Key, item.Value));
                else
                {
                    squad.specialSquad.Add((item.Key, item.Value));
                    squad.specialCount += item.Value;
                    squad.specialScore += sd.Score * item.Value;
                }
                squad.spawnTime += sd.TrainTime * item.Value;
                squad.score += sd.Score * item.Value;
                squad.num += item.Value;
                squad.price += sd.Price * item.Value;
            });
            return squad;
        }

        public static SquadData FromData(List<(int,int)> squad,List<(int,int)> specialSquad)
        {
            SquadData data = new SquadData();
            data.squad = squad;
            data.specialSquad = specialSquad;
            Action<(int,int)> f = (item) =>
            {
                var sd = Aoe4DataConfig.GetSquad(item.Item1);
                if (sd.Invaild || sd.TrainTime < 0.0f) return;
                data.spawnTime += sd.TrainTime * item.Item2;
                data.score += sd.Score * item.Item2;
                data.num += item.Item2;
                data.price += sd.Price * item.Item2;
                if (sd.SquadType != ESquadType.Normal)
                {
                    data.specialCount += item.Item2;
                    data.specialScore += sd.Score * item.Item2;
                }
            };
            foreach( var it in squad )
               f(it);
            foreach(var it in specialSquad)
                f(it);
            return data;
        }

        public static void Recycle(SquadData d)
        {
            if(d.squad != null)
                ObjPoolMgr.Instance.Get<List<(int, int)>>().Return(d.squad);
            if(d.specialSquad != null)
                ObjPoolMgr.Instance.Get<List<(int, int)>>().Return(d.specialSquad);
        }
    }

    public class AutoSpawnSquadSubMsgParser<IT> : ISubMsgParser<IDyMsgParser<IT>, IT> , IPlayerParserObserver
        where IT : class,IContext 
    {
        
        private ConcurrentDictionary<long,SquadData> m_Dict = new ConcurrentDictionary<long,SquadData>();
        private IDyMsgParser<IT> m_Owner;
        public Utils.ObjectPool<List<(int, int)>> SquadListPool { get; private set; } = new Utils.ObjectPool<List<(int, int)>>(
            () => new List<(int, int)>(), (a) => a.Clear());
        private DateTime m_AutoBoomCkTime = DateTime.Now;

        public void Init(IDyMsgParser<IT> owner)
        {
            m_Owner = owner;
            m_Owner.m_MsgDispatcher.GetPlayerParser().AddObserver(this);
        }

        public void OnTick(float delat)
        {
            var gameState = Locator.Instance.Get<Aoe4GameState>();
            foreach (var it in m_Dict.Keys)
            {
                if(m_Dict.TryGetValue(it,out var v))
                {
                    var ud = m_Owner.GetUserData(it);
                    if (gameState.GetSquadCount(ud.Group) >= Aoe4DataConfig.AutoSquadLimit)
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
                    var gold = m_Owner.m_MsgDispatcher.GetResourceMgr().GetResource(it.Key);
                    if(gold >= Aoe4DataConfig.AutoGoldLimit)
                    {
                        var c = 1000 / it.Value.price;
                        Boom(it.Key,c);
                    }
                }
                m_AutoBoomCkTime = DateTime.Now;
            }
        }

        private void SpawnSquad(long uid,SquadData v)
        {
            var owner = (m_Owner as MsgGiftParser<IT>);
            var u = owner.GetUserData(uid);
            foreach (var it in v.specialSquad)
            {
                var sd = Aoe4DataConfig.GetSquad(it.Item1);
                owner?.SendSpawnSquad(u, it.Item1, it.Item2, sd);
            }
            owner?.SendSpawnSquad(u, v.squad, 1,false);
            //m_Owner.InitCtx.PrintGameMsg($"{v.uName}自动出兵");
            m_Owner.UpdateUserData(uid, v.score, v.num, null, null);
        }
        

        public void AddDefaultAutoSquad(UserData data,int sdId,int c)
        {
            var squad = new SquadData();
            squad.uName = data.Name;
            squad.squad = SquadListPool.Get();
            squad.specialSquad = SquadListPool.Get();
            squad.lastSpawnTime = DateTime.Now;
            var sd = Aoe4DataConfig.GetSquad(sdId);
            squad.squad.Add((sdId, c));
            squad.score = sd.Score * c;
            squad.price = sd.Price * c;
            squad.spawnTime = sd.TrainTime * c;
            squad.num = c;
            m_Dict.TryAdd(data.Id, squad);
        }

        public bool Parse(DyMsgOrigin msg)
        {
            if(msg.barType == MsgType.Comment)
            {
                var uid = msg.msg.UserID_long;
                var uName = msg.msg.UserName;
                var lower = msg.msg.CommentText.ToLower();
                var match = new Regex("^([0-9a-w]*)$").Match(lower);
                if (match.Groups.Count == 2)
                {
                    if(match.Groups[1].Value.Length == 0)
                    {
                        if (m_Dict.TryRemove(uid, out var squadData))
                        {
                            SquadListPool.Return(squadData.squad);
                            SquadListPool.Return(squadData.specialSquad);
                            m_Owner.InitCtx.PrintGameMsg($"{uName}取消自动出兵");
                        }
                        return true;
                    }
                    bool needAdd = false;
                    if (!m_Dict.TryGetValue(uid, out var squad))
                    {
                        squad = new SquadData();
                        squad.uName = uName;
                        squad.squad = SquadListPool.Get();
                        squad.specialSquad = SquadListPool.Get();
                        squad.lastSpawnTime = DateTime.Now;
                        needAdd = true;
                    }
                    var spawnTime = 0.0;
                    var isEmpty = false;
                    lock (squad)
                    {
                        if(!needAdd)squad.Reset();
                        StringToDictAndForeach(match.Groups[1].Value, (item) =>
                        {
                            if (item.Key >= Aoe4DataConfig.SquadCount) return;
                            var sd = Aoe4DataConfig.GetSquad(item.Key);
                            if (sd.Invaild || sd.TrainTime < 0.0f) return;
                            if(sd.SquadType == ESquadType.Normal)
                                squad.squad.Add((item.Key, item.Value));
                            else
                            {
                                squad.specialSquad.Add((item.Key, item.Value));
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
                    if(isEmpty)
                    {
                        if (m_Dict.TryRemove(uid, out var squadData))
                        {
                            SquadListPool.Return(squadData.squad);
                            SquadListPool.Return(squadData.specialSquad);
                            m_Owner.InitCtx.PrintGameMsg($"{uName}取消自动出兵");
                        }
                        return true;
                    }
                    if (needAdd) m_Dict.TryAdd(uid, squad);
                    m_Owner.InitCtx.PrintGameMsg($"{uName}设置自动出兵，出兵时间{spawnTime}秒");
                    m_Owner.UpdateUserData(uid, 1, 0, uName, msg.msg.UserFace);
                    return true;
                }else if (lower.StartsWith("暴") || lower.StartsWith("爆"))
                {
                    var maxCount = 5000;
                    if((lower.Length > 1 && int.TryParse(lower.Substring(1), out var _maxCount)))
                    {
                        if (_maxCount > 0) maxCount = _maxCount;
                    }
                    Boom(uid,maxCount,uName);
                    return true;
                }
            }
            return false;
        }

        private void Boom(long uid,int maxCount,string uName = null)
        {
            if (m_Dict.TryGetValue(uid, out var squad))
            {
                var resMgr = m_Owner.m_MsgDispatcher.GetResourceMgr();
                var g = resMgr.GetResource(uid);
                var c = g / squad.price;
                if(c > maxCount) c = maxCount;
                if (c == 0)
                {
                    m_Owner.InitCtx.PrintGameMsg($"{squad.uName}没有足够的资源暴兵");
                }
                else
                {
                    if (resMgr.RemoveResource(uid,c * squad.price))
                    {
                        (m_Owner as MsgGiftParser<IT>).SpawnManySquadQueue(uid, squad, c,upLevelgold: c * squad.price,notRecycle:true);
                        m_Owner.InitCtx.PrintGameMsg($"{squad.uName}暴兵{c}组");
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

        public void Stop()
        {
            m_Owner = null;
        }

        public void OnClear()
        {
            foreach (var it in m_Dict)
                SquadListPool.Return(it.Value.squad);
            m_Dict.Clear();
        }

        public void OnAddGroup(UserData userData, int g)
        {
            AddDefaultAutoSquad(userData,0,3);
        }

        public void OnChangeGroup(UserData userData, int old, int n)
        {

        }

        public float GetSpawnProgress(long uid)
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
    }
public class SignInSubMsgParser<IT> : ISubMsgParser<IDyMsgParser<IT>, IT>
        where IT : class,IContext 
    {
        
        private IDyMsgParser<IT> m_Owner;

        public void Init(IDyMsgParser<IT> owner)
        {
            m_Owner = owner;
        }
        


        public bool Parse(DyMsgOrigin msg)
        {
            if(msg.barType == MsgType.Comment)
            {
                if (msg.msg.CommentText.Trim().StartsWith("签"))
                {
                    var u = m_Owner.m_MsgDispatcher.GetMsgParser().GetUserData(msg.msg.UserID_long);
                    if (DB.DBMgr.Instance.SignIn(u))
                    {
                        var probability = 200 - (msg.msg.GuardLevel > 0 ? (4 - msg.msg.GuardLevel) * 20 : 0);
                        var now = DateTime.Now;
                        var r = new Random((int)now.Ticks);
                        var isYearFirstDay = DateTime.Now.DayOfYear == 1;
                        var c = (isYearFirstDay ? 2 : 1) * (msg.msg.GuardLevel > 0 ? 3 : 1);
                        if (r.Next(0, probability) == 99 || isYearFirstDay)
                        {
                            var l = r.Next(0, Aoe4DataConfig.ItemDatas.Count);
                            foreach (var it in Aoe4DataConfig.ItemDatas)
                            {
                                if (l == 0)
                                {
                                    
                                    if(DB.DBMgr.Instance.AddGiftItem(u, it.Key, c) > 0)
                                        m_Owner.InitCtx.PrintGameMsg($"恭喜{u.Name}签到成功获得{it.Value.Name}*{c}");
                                    break;
                                }
                                --l;
                            }
                        }
                        else
                        {
                            c = r.Next(10, 51 + (msg.msg.GuardLevel > 0 ? (4 - msg.msg.GuardLevel) * 100 : 0));
                            if(DB.DBMgr.Instance.AddHonor(u, c) > 0)
                                m_Owner.InitCtx.PrintGameMsg($"恭喜{u.Name}签到成功获得{c}功勋");
                        }
                    }
                    else
                    {
                        m_Owner.InitCtx.PrintGameMsg($"{u.Name}今天已经签过到了，明天再来吧");
                    }

                    return true;
                }
            }
            return false;
        }

        public void OnTick(float delat)
        {
            
        }

        public void Stop()
        {
            m_Owner = null;
        }

        public void OnClear()
        {
            
        }
        
    }
}
