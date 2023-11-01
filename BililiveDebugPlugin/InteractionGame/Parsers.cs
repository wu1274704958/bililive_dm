using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using BilibiliDM_PluginFramework;
using System.Windows.Interop;

namespace InteractionGame 
{
    using Msg = DanmakuModel;
    using MsgType = MsgTypeEnum;
    public abstract class IDyPlayerParser<IT>
        where IT : IContext
    {
        private Dictionary<string, int> PlayerGroupMap;
        private ConcurrentDictionary<long, int> GroupDict = new ConcurrentDictionary<long, int>();
        private Regex mSelectGrouRegex;
        protected IT InitCtx;
        public void Init(IT it)
        {
            InitCtx = it;
            PlayerGroupMap = GetPlayerGroupMap();
            mSelectGrouRegex = SelectGrouRegex;
        }
        protected virtual Dictionary<string,int> GetPlayerGroupMap()
        {
            return new Dictionary<string, int>
            {
                { "蓝",0 },
                { "红",1 },
                { "绿",2 },
                { "黄",3 },
            };
        }
        protected virtual Regex SelectGrouRegex => new Regex("(.+)");
        protected virtual void ParseChooseGroup(Msg msg)
        {
            if (msg.CommentText == null) return;
            var con = msg.CommentText.Trim();
            var match = mSelectGrouRegex.Match(con);
            if (match.Groups.Count == 2 && PlayerGroupMap.TryGetValue(match.Groups[1].Value,out var v))
            {
                if(!GroupDict.TryAdd(msg.UserID_long, v))
                {
                    GroupDict[msg.UserID_long] = v;
                }
                //if(Appsetting.Current.PrintBarrage)
                {
                    InitCtx.Log($"{msg.UserName}选择了{match.Groups[1].Value}");
                }
            }
        }
        protected virtual int GetGroupById(long id)
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
        public abstract int Parse(DyMsgOrigin msgOrigin);
        public abstract bool Demand(Msg msg, MsgType barType);
        
    }
    public abstract class IDyMsgParser<IT>
        where IT : IContext
    {
        protected IT InitCtx;
        public void Init(IT it)
        {
            InitCtx = it;
        }
        public abstract (int, int) Parse(DyMsgOrigin msgOrigin);
        public abstract bool Demand(Msg msg, MsgType barType);
    }
    public class StaticMsgDemand
    {
        public static bool Demand(Msg msg, MsgType barType)
        {
            return (barType == MsgType.Interact || barType == MsgType.GiftSend ||
                barType == MsgType.Comment);
        }
    }
    public class PlayerBirthdayParser<IT> : IDyPlayerParser<IT>
         where IT : IContext
    {
        public override bool Demand(Msg msg, MsgType barType)
        {
            return StaticMsgDemand.Demand(msg, barType);
        }

        public override int Parse(DyMsgOrigin msgOrigin)
        {
            ParseChooseGroup(msgOrigin.msg);
            var v = GetGroupById(msgOrigin.msg.UserID_long);
            if(v == -1)
            {
                v = new Random((int)DateTime.Now.Ticks).Next(0,2);
                SetGroup(msgOrigin.msg.UserID_long, v);
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
        where IT : IContext
    {                                                                             //0, 1, 2,3 ,4, 5,6
        protected static readonly List<int> QuickSuccessionTable = new List<int>(){ 16,15,9,12,5,10,3 };
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
                        if (v >= 0 && v <= 5 && (id == v || id == -1))
                        {
                            ++c;
                            if(id == -1) id = v;
                        }
                        if (c >= QuickSuccessionTable[id]) break;
                    }
                    //if(Appsetting.Current.PrintBarrage)
                    {
                        InitCtx.Log($"{msgOrigin.msg.UserName}选择出{c}个{id}");
                    }
                    return (id, c);
                }
                return (0, 0);
            }
            if(msgOrigin.barType == MsgType.GiftSend)
            {
                switch(msgOrigin.msg.GiftName)
                {
                    case "小花花": return (6, msgOrigin.msg.GiftCount);
                    case "打call": return (7, msgOrigin.msg.GiftCount);
                    case "干杯": return (8, msgOrigin.msg.GiftCount);
                }
            }
            if (msgOrigin.barType == MsgType.Interact && msgOrigin.msg.InteractType == InteractTypeEnum.Like)
            {
                return (0, 6);
            }
            return (0, 0);
        }
    }
}
