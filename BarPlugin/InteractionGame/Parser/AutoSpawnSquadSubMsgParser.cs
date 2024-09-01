using InteractionGame.plugs.config;
using InteractionGame.plugs;
using InteractionGame;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Utils;
using conf.Squad;

namespace InteractionGame.Parser
{
    using MsgType = BilibiliDM_PluginFramework.MsgTypeEnum; 
    public class AutoSpawnSquadSubMsgParser : ISubMsgParser, IPlayerParserObserver
    {

        private ConcurrentDictionary<string, SquadGroup> m_Dict = new ConcurrentDictionary<string, SquadGroup>();
        private ConcurrentDictionary<string, DateTime> m_SendGiftTimeDict = new ConcurrentDictionary<string, DateTime>();
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
                if (_squadMgr.CanSpawnSquad(it, SpawnSquadType.Auto) && m_Dict.TryGetValue(it, out var v))
                {
                    var ud = m_Owner.GetUserData(it);
                    if (gameState.GetSquadCount(ud.Group) >= _config.AutoSquadCountLimit)
                        continue;
                    lock (v)
                    {
                        var sp = DateTime.Now - v.lastSpawnTime;
                        if (sp.TotalSeconds >= v.spawnTime)
                        {
                            SpawnSquad(it, v);
                            v.lastSpawnTime = v.lastSpawnTime.AddSeconds(v.spawnTime);
                        }
                    }
                }
            }

            if (DateTime.Now - m_AutoBoomCkTime > TimeSpan.FromSeconds(2.0f))
            {
                foreach (var it in m_Dict)
                {
                    if (!_squadMgr.CanSpawnSquad(it.Key, SpawnSquadType.AutoGoldBoom))
                        continue;
                    var lastSendGiftTime = GetSendGiftTime(it.Key);
                    if ((DateTime.Now - lastSendGiftTime).TotalMinutes < 1.5)
                        continue;
                    var gold = m_Owner.InitCtx.GetResourceMgr().GetResource(it.Key);
                    if (gold >= 3000)
                    {
                        AutoBoom(it.Key, it.Value);
                    }
                }
                m_AutoBoomCkTime = DateTime.Now;
            }
        }

        private void AutoBoom(string id, SquadGroup value)
        {
            var ud = m_Owner.GetUserData(id);
            Boom(id, value, ud.NameColored, 999999, true, tag: "自动");
        }

        private DateTime GetSendGiftTime(string id)
        {
            if (m_SendGiftTimeDict.TryGetValue(id, out var v))
            {
                return v;
            }
            return Common.MinDateTime;
        }

        private void SpawnSquad(string uid, SquadGroup v)
        {
            if (v.Invaild)
            {
                m_Owner.InitCtx.PrintGameMsg($"{v.uName}请重新设置自动出兵配置");
                return;
            }
            var u = m_Owner.GetUserData(uid);
            foreach (var it in v.specialSquad)
            {
                m_Owner.SendSpawnSquad(u, it.Item2, it.Item1);
            }
            int rc = m_Owner.SendSpawnSquad(u, v.squad, v.normalCount, 1);
            //m_Owner.InitCtx.PrintGameMsg($"{v.uName}自动出兵");
            m_Owner.UpdateUserData(uid, v.score, v.specialCount + rc, null, null);
        }


        public void AddDefaultAutoSquad(UserData data, int sdId, int c)
        {
            var squad = SquadGroup.FromString("0", data.Group, data.Id);
            squad.lastSpawnTime = DateTime.Now;
            squad.uName = data.NameColored;
            m_Dict.TryAdd(data.Id, squad);
        }

        public bool Parse(DyMsgOrigin msg)
        {
            if (msg.barType == MsgType.GiftSend)
            {
                m_SendGiftTimeDict[msg.msg.OpenID] = DateTime.Now;
                return false;
            }
            if (msg.barType == MsgType.Comment)
            {
                var uid = msg.msg.OpenID;
                var user = m_Owner.GetUserData(uid);
                if (user == null) return false;
                var lower = msg.msg.CommentText;//.ToLower();
                var match = Reg.Match(lower);
                if (match.Groups.Count == 2)
                {
                    if (match.Groups[1].Value.Length == 0)
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
                    (spawnTime, isEmpty) = ParseStr2SquadGroup(match.Groups[1].Value, uid, needAdd, squad);
                    if (isEmpty)
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
                }
                else if (lower.StartsWith("暴") || lower.StartsWith("爆"))
                {
                    if (!_squadMgr.CanSpawnSquad(uid, SpawnSquadType.GoldBoom))
                        return true;
                    var maxCount = 5000;
                    bool isNew = false;
                    if (!m_Dict.TryGetValue(uid, out var squad))
                    {
                        m_Owner.InitCtx.PrintGameMsg($"{msg.msg.UserName}需要先设置自动出兵才能暴兵");
                        return true;
                    }
                    if (lower.Length > 1 && ((match = Reg.Match(lower.Substring(1))).Success
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
                    else if (lower.Length > 1 && (match = BoomOnlyCountReg.Match(lower.Substring(1))).Success)
                    {
                        if (match.Groups.Count > 1 && int.TryParse(match.Groups[1].Value, out var c_))
                            maxCount = c_;
                    }
                    Boom(uid, squad, user.NameColored, maxCount, !isNew);
                    return true;
                }
            }
            return false;
        }

        private (double, bool) ParseStr2SquadGroup(string str, string uid, bool needAdd, SquadGroup squad)
        {
            double spawnTime = 0.0;
            bool isEmpty = false;
            lock (squad)
            {
                if (!needAdd) squad.Reset();
                squad.StringTag = str;
                Utils.StringToDictAndForeach(str.Substring(0,1), (item) =>
                {
                    var u = m_Owner.InitCtx.GetMsgParser().GetUserData(uid);
                    var sd = _squadMgr.GetSquadBySlot(item.Key, u);
                    if (sd == null) return;
                    if (sd.SquadType_e == ESquadType.Normal)
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
            return (spawnTime, isEmpty);
        }

        private void Boom(string uid, int maxCount, string uName = null)
        {
            if (m_Dict.TryGetValue(uid, out var squad))
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
                        m_Owner.InitCtx.PrintGameMsg($"{squad.uName}暴兵{squad.StringTag}x{c}组");
                        m_Owner.SpawnManySquadQueue(uid, squad.Clone() as SquadGroup, c, upLevelgold: c * squad.price, notRecycle: false);
                    }
                }
            }
            else
            {
                m_Owner.InitCtx.PrintGameMsg($"{uName}需要先设置自动出兵才能暴兵");
            }
        }

        private void Boom(string uid, SquadGroup squad, string uName = null, int maxCount = 5000, bool needClone = false, string tag = null)
        {
            if (squad != null && !squad.IsEmpty)
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
            AddDefaultAutoSquad(userData, 48, 3);
        }

        public void OnChangeGroup(UserData userData, int old, int n)
        {

        }

        public float GetSpawnProgress(string uid)
        {
            if (m_Dict.TryGetValue(uid, out var v))
            {
                lock (v)
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
                if (squadGroup.specialSquad[i].Item1.RealId == old.RealId)
                    return true;
            }

            return false;
        }

        public void Start()
        {

        }
    }
}
