using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using BilibiliDM_PluginFramework;
using ProtoBuf;
using Utils;
using BililiveDebugPlugin.InteractionGame.mode;
using conf.Squad;
using InteractionGame.plugs.config;
using InteractionGame.plugs;

namespace InteractionGame
{
    using Msg = DanmakuModel;
    using MsgType = MsgTypeEnum;


    public interface IPlayerParserObserver
    {
        void OnAddGroup(UserData userData,int g);
        void OnChangeGroup(UserData userData,int old,int n);
        void OnClear();
    }
    
    public interface IPlayerPreJoinObserver
    {
        Msg OnPreJoin(Msg m);
    }

    public abstract class IDyPlayerParser
    {
        private Dictionary<string, int> PlayerGroupMap;
        private ConcurrentDictionary<string, int> GroupDict = new ConcurrentDictionary<string, int>();
        private ConcurrentDictionary<string, int> TargetDict = new ConcurrentDictionary<string, int>();
        private ConcurrentDictionary<int, int> GroupCount = new ConcurrentDictionary<int, int>();
        private ConcurrentDictionary<Int64,IPlayerParserObserver> _observers = new ConcurrentDictionary<long, IPlayerParserObserver>();
        private ConcurrentDictionary<Int64,IPlayerPreJoinObserver> _preJoinObservers = new ConcurrentDictionary<long, IPlayerPreJoinObserver>();
        private Regex mSelectGrouRegex;
        protected IContext InitCtx;
        protected IGameState _gameState;
        private readonly object m_LockChooseGroup = new object();

        public virtual void Init(IContext it)
        {
            InitCtx = it;
            PlayerGroupMap = GetPlayerGroupMap();
            mSelectGrouRegex = SelectGrouRegex;
            _gameState = Locator.Instance.Get<IGameState>();
            SetupGroupCount();
        }

        private void SetupGroupCount()
        {
            for(int i = 0;i < GetGroupCount(); i++)
            {
                GroupCount.TryAdd(i, 0);
            }
        }

        public virtual void OnClear()
        {
            GroupDict.Clear();
            TargetDict.Clear();
            GroupCount.Clear();
            SetupGroupCount();
            foreach (var it in _observers)
            {
                it.Value.OnClear();
            }
        }

        public List<string> GetUsersByGroup(int g)
        {
            var res = ObjPoolMgr.Instance.Get<List<string>>(null,a=>a?.Clear()).Get();
            foreach (var it in GroupDict)
            {
                if (it.Value == g)
                    res.Add(it.Key);
            }
            return res;
        }
        protected virtual Dictionary<string,int> GetPlayerGroupMap()
        {
            return Locator.Instance.Get<IConstConfig>().GroupNameMap;
        }
        public List<int> GetLeastGroupList()
        {
            var res = new List<int>();
            var ls = GroupCount.ToList();
            ls.Sort((a,b) => a.Value - b.Value);
            for(int i = 0;i < ls.Count;i++)
            {
                if (ls[i].Value == ls[0].Value)
                    res.Add(ls[i].Key);
            }
            return res;
        }
        protected virtual Regex SelectGrouRegex => new Regex("(.+)");
        protected virtual void ParseChooseGroup(string uid,string con,string uName)
        {
            var match = mSelectGrouRegex.Match(con);
            var oldGroup = GetGroupById(uid);
            if (match.Groups.Count == 2 && PlayerGroupMap.TryGetValue(match.Groups[1].Value,out var v))
            {
                SetGroup(uid, v);
                //if(Appsetting.Current.PrintBarrage)
                {
                    InitCtx.PrintGameMsg($"{SettingMgr.GetColorWrap(uName,v)}选择加入{match.Groups[1].Value}方");
                    if (oldGroup != -1 && oldGroup != v)
                        InitCtx.GetResourceMgr().RemoveAllVillagers(uid);
                    if (GetTarget(uid) == v)
                    {
                        TargetDict[uid] = -1;
                    }
                }
            }
            else
            {
                TryParseChangTarget(uid,con,uName);
            }
        }
        protected virtual int ParseJoinGroup(string uid,string con, DyMsgOrigin msgOrigin)
        {
            if (con.StartsWith("加") || (msgOrigin.barType == MsgType.GiftSend ||
                (msgOrigin.barType == MsgType.Interact && msgOrigin.msg.InteractType == InteractTypeEnum.Like) ||
                msgOrigin.barType == MsgType.GuardBuy))
            {
                lock (this)
                {
                    return ChooseGroupSystem(uid, msgOrigin);
                }
            }
            return -1;
        }

        public int ChooseGroupSystem(string uid, DyMsgOrigin msgOrigin)
        {
            int g = Locator.Instance.Get<IGameMode>().GetPlayerGroup(uid);
            if (g == -1)
            {
                lock (m_LockChooseGroup)
                {
                    g = GetLeastGroup();
                    SetGroup(uid, g);
                }
            }
            else
            {
                SetGroup(uid, g);
            }
            if (g == GetTarget(uid))
            {
                SetTarget(uid, -1);
            }
            if (g > -1)
            {
                msgOrigin.msg = OnPlayerPreJoin(msgOrigin.msg);
                InitCtx.GetResourceMgr().AddAutoResourceById(uid, Locator.Instance.Get<IConstConfig>().GetOnPlayerJoinGoldAddition(msgOrigin.msg.GuardLevel));
                OnAddGroup(new UserData(uid, msgOrigin.msg.UserName, msgOrigin.msg.UserFace, g, msgOrigin.msg.GuardLevel, Utils.GetFansLevel(msgOrigin)), g);
            }
            return g;
        }

        private int GetLeastGroup()
        {
            int v = Int32.MaxValue; 
            int g = 0;
            bool allSame = false;
            foreach (var it in GroupCount)
            {
                var realVal = Locator.Instance.Get<IGameMode>().OverrideGetPlayerCount(it.Key, it.Value);
                if (realVal < v)
                {
                    allSame = v == Int32.MaxValue;
                    g = it.Key;
                    v = realVal;
                }else if(realVal > v)
                {
                    allSame = false;
                }
            }
            if(allSame)
            {
                return new Random().Next(0, _gameState.GroupCount);
            }
            return g;
        }

        public int GetGroupCount()
        {
            return _gameState.GroupCount;
        }
        public int GetGroupExclude(int g)
        {
            for (int i = 0; i < _gameState.GroupCount; i++)
            {
                if (g != i)
                    return i;
            }
            return -1;
        }
        protected bool TryParseChangTarget(string uid, string con, string uName,bool autoChangeGroup = true)
        {
            var match = new Regex("攻(.+)").Match(con);
            if (match.Groups.Count == 2 && PlayerGroupMap.TryGetValue(match.Groups[1].Value, out var v))
            {
                var self = GetGroupById(uid);
                var tar = SettingMgr.GetColorWrap(match.Groups[1].Value, v >= 10 ? v - 10 : v);
                if (autoChangeGroup && (self == v || self < 0))
                {
                    SetGroup(uid,GetGroupExclude(v));
                }
                if (self > -1)
                    uName = SettingMgr.GetColorWrap(uName, self);
                if (!autoChangeGroup && (self == v || self < 0))
                {
                    if(self == v) InitCtx.PrintGameMsg($"{uName}不能以自己为目标");
                    if(self < 0) InitCtx.PrintGameMsg($"{uName}未加入游戏无法选择目标");
                    return false;
                }
                //m_MsgDispatcher.GetBridge().ExecSetCustomTarget(self + 1, v + 1);
                SetTarget(uid, v);
                InitCtx.PrintGameMsg($"{uName}选择{tar}方作为进攻目标");
                //m_MsgDispatcher.GetMsgParser().SendAllSquadAttack(v, uid);
                return true;
            }
            return false;
        }
        public virtual int GetGroupById(string id)
        {
            if(GroupDict.TryGetValue(id,out var r))
            {
                return r;
            }
            return -1;
        }
        public virtual void SetGroup(string id,int g)
        {
            var c = Locator.Instance.Get<IGameMode>().GetSeatCountOfPlayer(id,g);
            if (!GroupDict.TryAdd(id, g))
            {
                if(GroupDict[id] == g) return;
                if(GroupDict[id] >= 0 && GroupCount.ContainsKey(g))
                {
                    GroupCount[g] -= c;
                }
                GroupDict[id] = g;
            }
            if (!GroupCount.TryAdd(g, c))
            {
                GroupCount[g] += c;
            }
        }
        public bool HasGroup(string id)
        {
            return GroupDict.ContainsKey(id);
        }
        public int GetTarget(string id)
        {
            if(TargetDict.TryGetValue(id, out var r))
            {  
                return r; 
            }
            return -1;
        }
        public int SetTarget(string id,int t)
        {
            if(t < 0)
            {
                TargetDict.TryRemove(id, out _);
                return -1;
            }
            TargetDict[id] = t;
            return t;
        }

        public virtual void Stop()
        {
            InitCtx = null;
            OnClear();
        }
        public abstract int Parse(DyMsgOrigin msgOrigin);
        public abstract bool Demand(Msg msg, MsgType barType);
        public void AddObserver(IPlayerParserObserver observer)
        {
            _observers.TryAdd(observer.GetHashCode(), observer);
        }
        public void RmObserver(IPlayerParserObserver observer)
        {
            _observers.TryRemove(observer.GetHashCode(), out _);
        }
       
        public void OnAddGroup(UserData userdata, int g)
        {
            InitCtx.GetMsgParser().TryAddUser(userdata);
            foreach (var it in _observers)
            {
                it.Value.OnAddGroup(userdata, g);
            }
        }
        public void OnChangeGroup(UserData userdata,int old, int g)
        {
            foreach (var it in _observers)
            {
                it.Value.OnChangeGroup(userdata, old, g);
            }
        }
        
        public void AddPreJoinObserver(IPlayerPreJoinObserver observer)
        {
            _preJoinObservers.TryAdd(observer.GetHashCode(), observer);
        }
        public void RmPreJoinObserver(IPlayerPreJoinObserver observer)
        {
            _preJoinObservers.TryRemove(observer.GetHashCode(), out _);
        }
        
        protected Msg OnPlayerPreJoin(Msg msg)
        {
            foreach(var it in _preJoinObservers)
            {
                msg = it.Value.OnPreJoin(msg);
            }
            return msg;
        }
    }
    public interface ISubMsgParser
    {
        void Init(IDyMsgParser owner);
        void Start();
        void OnStartGame();
        void Stop();
        bool Parse(DyMsgOrigin msg);
        void OnTick(float delat);
        void OnClear();
    }
    public abstract class IDyMsgParser
    {
        protected ConcurrentDictionary<string,UserData> UserDataDict = new ConcurrentDictionary<string,UserData>();
        public IContext InitCtx { get;protected set; }
        protected List<ISubMsgParser> subMsgParsers = new List<ISubMsgParser>();
        
        public virtual void Init(IContext it)
        {
            InitCtx = it;
            foreach (var subMsgParser in subMsgParsers)
                subMsgParser.Init(this);
        }
        public virtual void Start()
        {
            foreach (var subMsgParser in subMsgParsers)
                subMsgParser.Start();
        }

        public virtual void OnStartGame()
        {
            foreach (var subMsgParser in subMsgParsers)
                subMsgParser.OnStartGame();
        }
        public virtual void Stop()
        {
            foreach (var subMsgParser in subMsgParsers)
                subMsgParser.Stop();
            InitCtx = null;
            UserDataDict.Clear();
        }
        public virtual (int, int) Parse(DyMsgOrigin msgOrigin)
        {
            int n = 0;
            lock (subMsgParsers)
            {
                foreach (var subMsgParser in subMsgParsers)
                {
                    if (subMsgParser.Parse(msgOrigin))
                        ++n;
                }
            }
            return (n,0);
        }
        public abstract bool Demand(Msg msg, MsgType barType);

        public virtual void Clear()
        {
            UserDataDict.Clear();
            lock (subMsgParsers)
            {
                foreach (var subMsgParser in subMsgParsers)
                    subMsgParser.OnClear();
            }
        }
        public List<UserData> GetSortedUserData()
        {
            var ls = UserDataDict.Values.ToList();
            ls.Sort((a,b) => b.Score.CompareTo(a.Score));
            return ls;
        }
        public UserData GetUserData(string id)
        {
            if(UserDataDict.TryGetValue(id, out var data))
                return data;
            return null;
        }
        public void UpdateUserData(string id,double score,int soldier_num,string name = null,string icon = null,int group = -1,int guardLv = 0,int fansLv = 0)
        {
            if (UserDataDict.ContainsKey(id))
            {
                UserDataDict[id].Score += score;
                UserDataDict[id].Soldier_num += soldier_num;
                if(group != -1)
                    UserDataDict[id].Group = group;
            }
            else
            {
                UserDataDict.TryAdd(id,new UserData(id,name,icon,group,guardLv,fansLv)
                {
                    Score = score,
                    Soldier_num = soldier_num,
                });
            }
        }

        public void TryAddUser(UserData userData) {
            UserDataDict.TryAdd (userData.Id, userData);
        }
        public void AddWinScore(int g, int score)
        {
            foreach (var key in UserDataDict)
            {
                key.Value.Group = InitCtx.GetPlayerParser().GetGroupById(key.Key);
                if(g == key.Value.Group + 1)
                {
                    key.Value.Score += score;
                }
            }
        }
        public virtual void OnTick(float delat) {
            lock (subMsgParsers)
            {
                foreach (var subMsgParser in subMsgParsers)
                    subMsgParser.OnTick(delat);
            }
        }
        public void AddSubMsgParse(ISubMsgParser msgParser)
        {
            lock (subMsgParsers)
            {
                subMsgParsers.Add(msgParser);
            }
        }
        public void RmSubMsgParse(ISubMsgParser msgParser)
        {
            lock (subMsgParsers)
            {
                subMsgParsers.Remove(msgParser);
            }
        }
        public T GetSubMsgParse<T>()
            where T : class
        {
            lock (subMsgParsers)
            {
                foreach (var subMsgParser in subMsgParsers)
                {
                    if(subMsgParser is T)
                    {
                        return (T)subMsgParser;
                    }
                }
            }
            return null;
        }
        public abstract void SendSpawnSquad(UserData u, int c, SquadData sd);
        public abstract void SendSpawnSquadQueue(UserData u, int sid, int c, SquadData sd, int price = 0, string giftName = null, int giftCount = 0, int honor = 0,
            int restGold = 0, int upLevelgold = 0, int giveHonor = 0, ushort attribute = 0, int priority = 0);
        public abstract int SendSpawnSquad(UserData u, List<(SquadData, int)> group, int groupCount, int multiple = 1);
        public abstract void SpawnManySquadQueue(string uid, SquadGroup v, int c, int price = 0, string giftName = null, int giftCount = 0, int honor = 0,
            double restGold = 0, double upLevelgold = 0, int giveHonor = 0, bool notRecycle = false, int priority = 0);
    }
    public class StaticMsgDemand
    {
        public static bool Demand(Msg msg, MsgType barType)
        {
            return (barType == MsgType.Interact || barType == MsgType.GiftSend ||
                barType == MsgType.Comment || barType == MsgType.GuardBuy);
        }
    }
    [ProtoContract]
    public class UserData
    {
        [ProtoMember(1)]
        public string Id;
        public int Id_int => int.TryParse(Id,out var idNum) ? idNum : Int32.MaxValue;
        [ProtoMember(2)]
        public string Name;
        [ProtoMember(3)]
        public string Icon;
        [ProtoMember(4)]
        public double Score;
        [ProtoMember(5)]
        public int Soldier_num;
        [ProtoMember(6)]
        public int Group;
        [ProtoMember(7)]
        public long Honor;
        [ProtoMember(8)]
        public int Op1 = 0;
        public int Op1Heigh = 0;
        public int GuardLevel { get; protected set; } = 0;
        [ProtoMember(9)]
        public int RealGuardLevel { get; protected set; } = 0;
        public int FansLevel;
        //public DateTime JoinTime;


        public UserData(string id, string name, string icon, int group, int guardLvl, int fansLevel = 0)
        {
            Id = id;
            Name = name;
            Icon = icon;
            Group = group;
            SetGuardLevel(guardLvl);
            FansLevel = fansLevel;
            //JoinTime = DateTime.Now;
        }

        public void SetGuardLevel(int guardLvl)
        {
            if(guardLvl >= 10)
            {
                GuardLevel = guardLvl / 10;
                RealGuardLevel = guardLvl;
            }
            else
            {
                RealGuardLevel = GuardLevel = guardLvl;
            }
        }

        public string NameColored => SettingMgr.GetColorWrap(Name, Group);
        public int HpMultiple => (Op1 >> 16) & 255;
        public int DamageMultiple => (Op1 >> 24) & 255;
        public int AddHpMultiple(int n)
        {
            if (n == 0) return 0;
            var op = Op1;
            var high_l = (op >> 16) & 255;
            high_l += n;
            if(high_l > 255) high_l = 255;
            op = (int)(op & 0xFF00_0000) | (high_l << 16) | (op & 0xFFFF);
            Interlocked.Exchange(ref Op1, op);
            return high_l;
        }
        public int AddDamageMultiple(int n)
        {
            if (n == 0) return 0;
            var op = Op1;
            var high_h = (op >> 24) & 255;
            high_h += n;
            if(high_h > 255) high_h = 255;
            op = (high_h << 24) | (op & 0x00FF_0000) | (op & 0xFFFF);
            Interlocked.Exchange(ref Op1, op);
            return high_h;
        }
        public long AppendSquadAttribute(ushort attr)
        {
            return ((long)Op1Heigh << 32) | (long)(Op1 | attr);
        }
        public long AppendSquadAttribute(ushort attr,byte configAddHp,byte configAddDamage)
        {
            var oph = Utils.AddByte(Op1Heigh, configAddHp,16);
            oph |= Utils.AddByte(Op1Heigh, configAddDamage, 24);
            return ((long)oph << 32) | (long)(Op1 | attr);
        }
    }

    public class SquadGroup : ICloneable, IDisposable
    {
        public List<(SquadData, int)> squad;
        public List<(SquadData, int)> specialSquad;
        public double spawnTime;
        public DateTime lastSpawnTime;
        public double score;
        public double price;
        public string uName;//user.NameColored
        public int num;
        public int specialCount;
        public int normalCount => num - specialCount;
        public double specialScore;
        public double normalScore => score - specialScore;
        public bool IsEmpty => squad.Count == 0 && specialSquad.Count == 0;

        public bool Invaild => squad == null || specialSquad == null;

        public ushort AddedAttr = 0;
        public string StringTag = null;
        public void Reset()
        {
            squad?.Clear();
            specialSquad?.Clear();
            spawnTime = 0.0f;
            lastSpawnTime = DateTime.Now;
            score = 0;
            price = 0;
            num = 0;
            specialCount = 0;
            specialScore = 0;
            AddedAttr = 0;
            StringTag = null;
        }

        public SquadGroup SetAddedAttr(ushort attr)
        {
            AddedAttr = attr;
            return this;
        }

        public static void OnClearList(List<(string, int)> list)
        {
            list.Clear();
        }
        public static SquadGroup FromString(string s, int g, string uid)
        {
            var squad = new SquadGroup();
            squad.StringTag = s;
            squad.squad = ObjPoolMgr.Instance.Get<List<(SquadData, int)>>(null, DefObjectRecycle.OnListRecycle).Get();
            squad.specialSquad = ObjPoolMgr.Instance.Get<List<(SquadData, int)>>(null, DefObjectRecycle.OnListRecycle).Get();
            var squadMgr = Locator.Instance.Get<ISquadMgr>();
            var user = Locator.Instance.Get<IContext>().GetMsgParser().GetUserData(uid);
            Utils.StringToDictAndForeach(s, (item) =>
            {
                var sd = squadMgr.GetSquadBySlot(item.Key, user);
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
            return squad;
        }

        public static SquadGroup FromData(List<(int, int)> squad, UserData user, ushort addedAttr = 0)
        {
            SquadGroup group = new SquadGroup();
            group.AddedAttr = addedAttr;
            group.Init();
            var squadMgr = Locator.Instance.Get<ISquadMgr>();
            Action<(int, int)> f = (item) =>
            {
                var sd = squadMgr.GetSquadBySlot(item.Item1, user);
                if (sd == null) return;
                group.spawnTime += sd.TrainTime * item.Item2;
                group.score += sd.Score * item.Item2;
                group.num += item.Item2;
                group.price += sd.Price * item.Item2;
                if (sd.SquadType_e != ESquadType.Normal)
                {
                    group.specialCount += item.Item2;
                    group.specialScore += sd.Score * item.Item2;
                    group.specialSquad.Add((sd, item.Item2));
                }
                else
                {
                    group.squad.Add((sd, item.Item2));
                }
            };
            foreach (var it in squad)
                f(it);
            return group;
        }
        public static SquadGroup FromData(Dictionary<int, int> squad, UserData user, ushort addedAttr = 0)
        {
            SquadGroup group = new SquadGroup();
            group.AddedAttr = addedAttr;
            group.Init();
            var squadMgr = Locator.Instance.Get<ISquadMgr>();
            Action<int,int> f = (id,count) =>
            {
                var sd = squadMgr.GetSquadBySlot(id, user);
                if (sd == null) return;
                group.spawnTime += sd.TrainTime * count;
                group.score += sd.Score * count;
                group.num += count;
                group.price += sd.Price * count;
                if (sd.SquadType_e != ESquadType.Normal)
                {
                    group.specialCount += count;
                    group.specialScore += sd.Score * count;
                    group.specialSquad.Add((sd, count));
                }
                else
                {
                    group.squad.Add((sd, count));
                }
            };
            foreach (var it in squad)
                f(it.Key,it.Value);
            return group;
        }

        public void Init()
        {
            squad = ObjPoolMgr.Instance.Get<List<(SquadData, int)>>(null, DefObjectRecycle.OnListRecycle).Get();
            specialSquad = ObjPoolMgr.Instance.Get<List<(SquadData, int)>>(null, DefObjectRecycle.OnListRecycle).Get();
        }

        public static void Recycle(SquadGroup d)
        {
            if (d.squad != null)
                ObjPoolMgr.Instance.Get<List<(SquadData, int)>>().Return(d.squad);
            if (d.specialSquad != null)
                ObjPoolMgr.Instance.Get<List<(SquadData, int)>>().Return(d.specialSquad);
            d.squad = null; d.specialSquad = null;
            d.Reset();
        }

        public object Clone()
        {
            SquadGroup oth = new SquadGroup();
            oth.Init();
            oth.uName = uName;
            oth.price = price;
            oth.score = score;
            oth.AddedAttr = AddedAttr;
            oth.lastSpawnTime = lastSpawnTime;
            oth.spawnTime = spawnTime;
            oth.num = num;
            oth.specialCount = specialCount;
            oth.specialScore = specialScore;
            oth.StringTag = StringTag;
            foreach (var it in squad)
                oth.squad.Add(it);
            foreach (var it in specialSquad)
                oth.specialSquad.Add(it);
            return oth;
        }

        public void Dispose()
        {
            Recycle(this);
        }
    }

}
