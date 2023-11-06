using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BilibiliDM_PluginFramework;
using BililiveDebugPlugin;

namespace InteractionGame 
{
    using Msg = DanmakuModel;
    using MsgType = MsgTypeEnum;
    public abstract class IDyPlayerParser<IT>
        where IT : class,IContext
    {
        private Dictionary<string, int> PlayerGroupMap;
        private ConcurrentDictionary<long, int> GroupDict = new ConcurrentDictionary<long, int>();
        private ConcurrentDictionary<long, int> TargetDict = new ConcurrentDictionary<long, int>();
        private Regex mSelectGrouRegex;
        protected IT InitCtx;
        protected ILocalMsgDispatcher<IT> m_MsgDispatcher;

        public void Init(IT it,ILocalMsgDispatcher<IT> dispatcher)
        {
            InitCtx = it;
            m_MsgDispatcher = dispatcher;
            PlayerGroupMap = GetPlayerGroupMap();
            mSelectGrouRegex = SelectGrouRegex;
        }
        protected virtual Dictionary<string,int> GetPlayerGroupMap()
        {
            return DebugPlugin.ColorMapIndex;
        }
        protected virtual Regex SelectGrouRegex => new Regex("(.+)");
        protected virtual void ParseChooseGroup(long uid,string con,string uName)
        {
            var match = mSelectGrouRegex.Match(con);
            if (match.Groups.Count == 2 && PlayerGroupMap.TryGetValue(match.Groups[1].Value,out var v))
            {
                if(!GroupDict.TryAdd(uid, v))
                {
                    GroupDict[uid] = v;
                }
                //if(Appsetting.Current.PrintBarrage)
                {
                    InitCtx.PrintGameMsg($"{uName}选择加入{match.Groups[1].Value}方");
                }
            }
            else
            {
                TryParseChangTarget(uid,con,uName);
            }
        }
        protected bool TryParseChangTarget(long uid, string con, string uName)
        {
            var match = new Regex("攻(.+)").Match(con);
            if (match.Groups.Count == 2 && PlayerGroupMap.TryGetValue(match.Groups[1].Value, out var v))
            {
                var self = GetGroupById(uid);
                if (self != v)
                {
                    //m_MsgDispatcher.GetBridge().ExecSetCustomTarget(self + 1, v + 1);
                    TargetDict[uid] = v;
                    InitCtx.PrintGameMsg($"{uName}选择{match.Groups[1].Value}方作为进攻目标");
                    return true;
                }
            }
            return false;
        }
        public virtual int GetGroupById(long id)
        {
            if(GroupDict.TryGetValue(id,out var r))
            {
                return r;
            }
            return -1;
        }
        protected virtual void SetGroup(long id,int g)
        {
            if (!GroupDict.TryAdd(id, g))
            {
                GroupDict[id] = g;
            }
        }
        public bool HasGroup(long id)
        {
            return GroupDict.ContainsKey(id);
        }
        public int GetTarget(long id)
        {
            if(TargetDict.TryGetValue(id, out var r))
            {  
                return r; 
            }
            return -1;
        }

        public virtual void Stop()
        {
            InitCtx = null;
            m_MsgDispatcher = null;
            GroupDict.Clear();
        }
        public abstract int Parse(DyMsgOrigin msgOrigin);
        public abstract bool Demand(Msg msg, MsgType barType);
        
    }
    public abstract class IDyMsgParser<IT>
        where IT : class,IContext
    {
        protected Dictionary<long,UserData> UserDataDict = new Dictionary<long,UserData>();
        protected IT InitCtx;
        protected ILocalMsgDispatcher<IT> m_MsgDispatcher;

        public void Init(IT it,ILocalMsgDispatcher<IT> dispatcher)
        {
            InitCtx = it;
            m_MsgDispatcher = dispatcher;
        }
        public virtual void Stop()
        {
            InitCtx = null;
            m_MsgDispatcher = null;
            UserDataDict.Clear();
        }
        public abstract (int, int) Parse(DyMsgOrigin msgOrigin);
        public abstract bool Demand(Msg msg, MsgType barType);

        public virtual void ClearUserData()
        {
            UserDataDict.Clear();
        }
        public List<UserData> GetSortedUserData()
        {
            var ls = UserDataDict.Values.ToList();
            ls.Sort((a,b) => b.score.CompareTo(a.score));
            return ls;
        }
        protected void UpdateUserData(long id,int score,int soldier_num,string name,string icon)
        {
            if (UserDataDict.ContainsKey(id))
            {
                UserDataDict[id].score += score;
                UserDataDict[id].soldier_num += soldier_num;
            }
            else
            {
                UserDataDict.Add(id,new UserData()
                {
                    name = name,
                    icon = icon,
                    score = score,
                    soldier_num = soldier_num
                });
            }
        }
    }
    public class StaticMsgDemand
    {
        public static bool Demand(Msg msg, MsgType barType)
        {
            return (barType == MsgType.Interact || barType == MsgType.GiftSend ||
                barType == MsgType.Comment);
        }
    }
    public class UserData
    {
        public string name;
        public string icon;
        public long score;
        public int soldier_num;
    }
    public class PlayerBirthdayParser<IT> : IDyPlayerParser<IT>
         where IT : class,IContext
    {
        public override bool Demand(Msg msg, MsgType barType)
        {
            return StaticMsgDemand.Demand(msg, barType);
        }

        public override int Parse(DyMsgOrigin msgOrigin)
        {
            ParseChooseGroup(msgOrigin.msg.UserID_long,msgOrigin.msg.CommentText.Trim(),msgOrigin.msg.UserName);
            var v = GetGroupById(msgOrigin.msg.UserID_long);
            if(v == -1)
            {
                v = new Random((int)DateTime.Now.Ticks).Next(0,2);
                SetGroup(msgOrigin.msg.UserID_long, v);
            }
            if (msgOrigin.barType == MsgType.Welcome)
            {
                InitCtx.PrintGameMsg($"欢迎{msgOrigin.msg.UserName}进入直播间，阵营{DebugPlugin.GetColorById(v)}方");
            }
            return v;
        }
        public static DateTime GetDateTimeFromSeconds(long sec)
        {
            DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
            TimeSpan time = TimeSpan.FromSeconds(sec);
            return startTime.Add(time);
        }
    }

    public class MsgGiftParser<IT> : IDyMsgParser<IT>
        where IT : class,IContext
    {                                                                             //0, 1, 2,3 ,4, 5,6
        protected static readonly List<int> QuickSuccessionTable = new List<int>(){ 10,9, 7,9, 5, 8,3,2,1,1,1 };
        public override bool Demand(Msg msg, MsgType barType)
        {
            return StaticMsgDemand.Demand(msg, barType);
        }

        public override (int,int) Parse(DyMsgOrigin msgOrigin)
        {
            if(msgOrigin.barType == MsgType.Comment)
            {
                var con = msgOrigin.msg.CommentText.Trim();
                var match = new Regex("([0-9]+)").Match(con);
                if (match.Groups.Count == 2)
                {
                    int id = -1;
                    int c = 0;
                    for (int i = 0; i < match.Groups[1].Length;++i)
                    {
                        int v = match.Groups[1].Value[i] - 48; 
                        if (v >= 0 && v <= 8 && (id == v || id == -1))
                        {
                            ++c;
                            if(id == -1) id = v;
                        }
                        if (id >= 0 && id < QuickSuccessionTable.Count && c >= QuickSuccessionTable[id]) break;
                    }
                    //if(Appsetting.Current.PrintBarrage)
                    {
                        
                        InitCtx.PrintGameMsg($"{msgOrigin.msg.UserName}选择出{c}个{BililiveDebugPlugin.DebugPlugin.GetSquadName(id)}");
                        UpdateUserData(msgOrigin.msg.UserID_long, c * (id + 1), c, msgOrigin.msg.UserName, "");
                        var target = m_MsgDispatcher.GetPlayerParser().GetTarget(msgOrigin.msg.UserID_long);
                        var self = m_MsgDispatcher.GetPlayerParser().GetGroupById(msgOrigin.msg.UserID_long);
                        if (target < 0)
                        {
                            m_MsgDispatcher.GetBridge().ExecSpawnSquad(self + 1, id,c);
                        }
                        else
                        {
                            m_MsgDispatcher.GetBridge().ExecSpawnSquadWithTarget(self + 1, id,target + 1, c);
                        }
                    }
                }
                return (0,0);
            }
            if(msgOrigin.barType == MsgType.GiftSend)
            {
                int id = 0;
                switch(msgOrigin.msg.GiftName)
                {
                    case "小花花":id = 7;break;  
                    case "牛哇牛哇": id = 8;break;  
                    //case "干杯": return (8, msgOrigin.msg.GiftCount);
                }
                InitCtx.PrintGameMsg($"{msgOrigin.msg.UserName}选择出{msgOrigin.msg.GiftCount}个{BililiveDebugPlugin.DebugPlugin.GetSquadName(id)}");
                UpdateUserData(msgOrigin.msg.UserID_long, msgOrigin.msg.GiftCount * (id + 1), msgOrigin.msg.GiftCount, msgOrigin.msg.UserName, "");
                return (id, msgOrigin.msg.GiftCount);
            }
            if (msgOrigin.barType == MsgType.Interact && msgOrigin.msg.InteractType == InteractTypeEnum.Like)
            {
                return (0, 6);
            }
            return (0, 0);
        }
    }
}
