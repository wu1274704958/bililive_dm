using BilibiliDM_PluginFramework;
using BililiveDebugPlugin.InteractionGame.Data;
using InteractionGame;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

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
                g = ParseJoinGroup(uid, con, uName);
                if(g > -1)
                {
                    OnAddGroup(new UserData(uid, uName, msgOrigin.msg.UserFace, g), g);
                }
            }
            if (g == -1)
            {
                TryParseChangTarget(uid,con,uName,false);
                g = GetGroupById(uid);
                if (g < 0)
                {
                    InitCtx.PrintGameMsg($"{uName}请先发“加入”，加入游戏");
                }
            }
            else
            {
                InitCtx.PrintGameMsg($"{uName}加入{DebugPlugin.GetColorById(g + 1)}方");
                m_MsgDispatcher.GetResourceMgr().AddAutoResourceById(uid);
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

    public class MsgGiftParser<IT> : IDyMsgParser<IT>
        where IT : class, IContext
    {
        public Utils.ObjectPool<List<(int, int)>> SquadListPool { get; private set; } = new Utils.ObjectPool<List<(int, int)>>(
            () => new List<(int, int)>(), (a) => a.Clear());

        public override bool Demand(Msg msg, MsgType barType)
        {
            return StaticMsgDemand.Demand(msg, barType);
        }

        public override void Init(IT it, ILocalMsgDispatcher<IT> dispatcher)
        {
            AddSubMsgParse(new AutoSpawnSquadSubMsgParser<IT>());
            base.Init(it, dispatcher);
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
                var match = new Regex("^([0-9a-z]+)$").Match(con);
                if (false && match.Groups.Count == 2)
                {
                    var resMgr = m_MsgDispatcher.GetResourceMgr();
                    var list = SquadListPool.Get();
                    Utils.StringToDictAndForeach(match.Groups[1].Value, (item) =>
                    {
                        if (item.Key >= Aoe4DataConfig.SquadCount) return;
                        var sd = Aoe4DataConfig.GetSquad(item.Key);
                        if (sd.Invaild) return;
                        int id = item.Key;
                        int c = item.Value > sd.QuickSuccessionNum ? sd.QuickSuccessionNum : item.Value;
                        var res = resMgr.GetResource(uid);
                        if (res < sd.Price * c)
                            c = res / sd.Price;
                        if (c > 0 && resMgr.RemoveResource(uid, sd.Price * c))
                        {
                            InitCtx.PrintGameMsg($"{msgOrigin.msg.UserName}选择出{c}个{sd.Name}");
                            UpdateUserData(msgOrigin.msg.UserID_long, c * sd.Score, c, msgOrigin.msg.UserName, msgOrigin.msg.UserFace);
                            if (sd.SquadType != ESquadType.Normal)
                                SendSpawnSquad(msgOrigin.msg.UserID_long, id, c, sd);
                            else
                                list.Add((id, c));
                        }
                        else
                        {
                            InitCtx.PrintGameMsg($"{msgOrigin.msg.UserName}金矿不足出兵失败，剩余数量{res}");
                        }
                    });
                    SendSpawnSquad(uid, list, 1,true);
                }
                else if (con.StartsWith("查"))
                {
                    var resMgr = m_MsgDispatcher.GetResourceMgr();
                    var res = resMgr.GetResource(msgOrigin.msg.UserID_long);
                    //var c = resMgr.GetVillagerCount(msgOrigin.msg.UserID_long);
                    //InitCtx.PrintGameMsg($"{msgOrigin.msg.UserName}金矿剩余数量{res},采矿村民{c}");
                    InitCtx.PrintGameMsg($"{msgOrigin.msg.UserName}金矿剩余数量{res}");
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
                }else if (con[0] == '攻')
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
                }
                return (0, 0);
            }
            if (msgOrigin.barType == MsgType.GiftSend)
            {
                int c = msgOrigin.msg.GiftCount;
                int t = 0;
                int id = 0;
                List<(int,int)> SpecialSquad = SquadListPool.Get();
                List<(int, int)> Squad = SquadListPool.Get();
                switch (msgOrigin.msg.GiftName)
                {
                    case "辣条": c *= 10; break;
                    case "小花花": c *= 20; break;
                    case "打call": c *= 110; break;
                    case "牛哇牛哇": id = 100; t = 1; break;
                    case "干杯": id = 106; t = 1; c *= 24; break;
                    case "棒棒糖": id = 102; t = 1; c *= 2; break;
                    case "这个好诶": id = 104; t = 1; c *= 12; break;
                    case "小蛋糕": id = 107; t = 1; c *= 18; break;
                    case "小蝴蝶": id = 105; t = 1; c *= 8; break;
                    case "情书":id = 108; t = 1;c *= 18; break;
                    case "告白花束": Squad.Add((113, 120)); Squad.Add((116, 100)); SpecialSquad.Add((109, 36)); t = 3; break;
                    case "水晶之恋": id = 113; t = 1; c *= 17; break;
                    default:
                        c *= (msgOrigin.msg.GiftBatteryCount * 20);
                        break;
                }
                switch (t)
                {
                    case 0:
                        InitCtx.PrintGameMsg($"{msgOrigin.msg.UserName}获得{c}个金矿");
                        m_MsgDispatcher.GetResourceMgr().AddResource(msgOrigin.msg.UserID_long, c);
                        break;
                    case 1:
                        {
                            var sd = Aoe4DataConfig.GetSquad(id);
                            InitCtx.PrintGameMsg($"{uName}出了{c}个{sd.Name}");
                            UpdateUserData(msgOrigin.msg.UserID_long, c * sd.Score, c, uName, msgOrigin.msg.UserFace);
                            SendSpawnSquad(msgOrigin.msg.UserID_long, id, c, sd);
                            break;
                        }
                    case 3:
                        {
                            if(SpecialSquad.Count > 0 || Squad.Count > 0)
                            {
                                SpawnManySquad(uid, SpecialSquad, Squad, c);
                                InitCtx.PrintGameMsg($"{uName}出了{msgOrigin.msg.GiftName}*{c}");
                            }
                        }
                        break;
                }
                SquadListPool.Return(SpecialSquad);
                SquadListPool.Return(Squad);
                return (0, 0);
            }
            if (msgOrigin.barType == MsgType.Interact && msgOrigin.msg.InteractType == InteractTypeEnum.Like)
            {
                var r = new Random((int)DateTime.Now.Ticks);
                var id = r.Next(0, Aoe4DataConfig.SquadCount);
                var sd = Aoe4DataConfig.GetSquad(id);
                var c = r.Next(1, sd.QuickSuccessionNum + 1);
                InitCtx.PrintGameMsg($"{msgOrigin.msg.UserName}点赞随机出了{c}个{sd.Name}");
                UpdateUserData(msgOrigin.msg.UserID_long, c * sd.Score, c, msgOrigin.msg.UserName, msgOrigin.msg.UserFace);
                SendSpawnSquad(msgOrigin.msg.UserID_long, id, c, sd);
                return (0, 0);
            }
            return (0, 0);
        }

        public void SendSpawnSquad(long uid, int sid, int c, Aoe4DataConfig.SquadData sd)
        {
            if (sid == Aoe4DataConfig.VillagerID)
            {
                m_MsgDispatcher.GetResourceMgr().SpawnVillager(uid, c);
                return;
            }
            var target = m_MsgDispatcher.GetPlayerParser().GetTarget(uid);
            var self = m_MsgDispatcher.GetPlayerParser().GetGroupById(uid);
            var attackTy = sd.SquadType == ESquadType.SiegeAttacker ? ((int)ESquadType.SiegeAttacker) : 0;
            if (target < 0)
            {
                m_MsgDispatcher.GetBridge().ExecSpawnSquad(self + 1, sid, c, uid, attackTy );
            }
            else
            {
                m_MsgDispatcher.GetBridge().ExecSpawnSquadWithTarget(self + 1, sid, target + 1, c, uid,attackTy);
            }
        }


        public void SendSpawnSquad(long uid, List<(int,int)> group,int multiple = 1,bool recovery = false)
        {
            if (group.Count == 0) return;
            var target = m_MsgDispatcher.GetPlayerParser().GetTarget(uid);
            var self = m_MsgDispatcher.GetPlayerParser().GetGroupById(uid);
            if (target < 0)
            {
                m_MsgDispatcher.GetBridge().ExecSpawnGroup(self + 1, group, uid,multiple);
            }
            else
            {
                m_MsgDispatcher.GetBridge().ExecSpawnGroupWithTarget(self + 1, target + 1, group, uid,multiple);
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

        private void SpawnManySquad(long uid, List<(int,int)> specialSquad,List<(int,int)> squad, int c)
        {
            int score = 0;
            int num = 0;
            foreach (var it in specialSquad)
            {
                var sd = Aoe4DataConfig.GetSquad(it.Item1);
                score += sd.Score * it.Item2;
                num += it.Item2;
                SendSpawnSquad(uid, it.Item1, it.Item2 * c, sd);
            }
            foreach (var it in squad)
            {
                var sd = Aoe4DataConfig.GetSquad(it.Item1);
                score += sd.Score * it.Item2;
                num += it.Item2;
            }
            SendSpawnSquad(uid, squad, c, false);
            UpdateUserData(uid, score * c, num * c, null, null);
        }

    }



    public class AutoSpawnSquadSubMsgParser<IT> : ISubMsgParser<IDyMsgParser<IT>, IT> , IPlayerParserObserver
        where IT : class,IContext 
    {
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
            }
        }
        private ConcurrentDictionary<long,SquadData> m_Dict = new ConcurrentDictionary<long,SquadData>();
        private IDyMsgParser<IT> m_Owner;
        public Utils.ObjectPool<List<(int, int)>> SquadListPool { get; private set; } = new Utils.ObjectPool<List<(int, int)>>(
            () => new List<(int, int)>(), (a) => a.Clear());

        public void Init(IDyMsgParser<IT> owner)
        {
            m_Owner = owner;
            m_Owner.m_MsgDispatcher.GetPlayerParser().AddObserver(this);
        }

        public void OnTick(float delat)
        {
            foreach (var it in m_Dict.Keys)
            {
                if(m_Dict.TryGetValue(it,out var v))
                {
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
        }

        private void SpawnSquad(long uid,SquadData v)
        {
            var owner = (m_Owner as MsgGiftParser<IT>);
            foreach (var it in v.specialSquad)
            {
                var sd = Aoe4DataConfig.GetSquad(it.Item1);
                owner?.SendSpawnSquad(uid, it.Item1, it.Item2, sd);
            }
            owner?.SendSpawnSquad(uid, v.squad, 1,false);
            //m_Owner.InitCtx.PrintGameMsg($"{v.uName}自动出兵");
            m_Owner.UpdateUserData(uid, v.score, v.num, null, null);
        }
        private void SpawnManySquad(long uid,SquadData v,int c)
        {
            var owner = (m_Owner as MsgGiftParser<IT>);
            foreach (var it in v.specialSquad)
            {
                var sd = Aoe4DataConfig.GetSquad(it.Item1);
                owner?.SendSpawnSquad(uid, it.Item1, it.Item2 * c, sd);
            }
            owner?.SendSpawnSquad(uid, v.squad, c,false);
            m_Owner.InitCtx.PrintGameMsg($"{v.uName}暴兵{c}组");
            m_Owner.UpdateUserData(uid, v.score * c, v.num * c, null, null);
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
                var match = new Regex("^([0-9a-z]*)$").Match(lower);
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
                        Utils.StringToDictAndForeach(match.Groups[1].Value, (item) =>
                        {
                            if (item.Key >= Aoe4DataConfig.SquadCount) return;
                            var sd = Aoe4DataConfig.GetSquad(item.Key);
                            if (sd.Invaild || sd.TrainTime < 0.0f) return;
                            if(sd.SquadType == ESquadType.Normal)
                                squad.squad.Add((item.Key, item.Value));
                            else
                                squad.specialSquad.Add((item.Key, item.Value));
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
                    if (m_Dict.TryGetValue(uid, out var squad))
                    {
                        var maxCount = 5000;
                        if((lower.Length > 1 && int.TryParse(lower.Substring(1), out var _maxCount)))
                        {
                            if (_maxCount > 0) maxCount = _maxCount;
                        }

                        var resMgr = m_Owner.m_MsgDispatcher.GetResourceMgr();
                        var g = resMgr.GetResource(uid);
                        var c = g / squad.price;
                        if(c > maxCount) c = maxCount;
                        if (c == 0)
                        {
                            m_Owner.InitCtx.PrintGameMsg($"{uName}没有足够的资源暴兵");
                        }
                        else
                        {
                            if (resMgr.RemoveResource(uid,c * squad.price))
                            {
                                SpawnManySquad(uid, squad, c);
                            }
                        }
                    }
                    else
                    {
                        m_Owner.InitCtx.PrintGameMsg($"{uName}需要先设置自动出兵才能暴兵");
                    }
                    
                }
            }
            return false;
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

}
