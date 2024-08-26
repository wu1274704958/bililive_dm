using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using BilibiliDM_PluginFramework;
using BililiveDebugPlugin.InteractionGame.Data;
using BililiveDebugPlugin.InteractionGameUtils;
using conf.Squad;
using InteractionGame;
using Utils;

namespace BililiveDebugPlugin.InteractionGame.Parser
{

    public interface ISquadUpLevelListener
    {
        void OnSquadUpLevel(string uid, short sid, byte lvl,SquadData old,SquadData @new);
    }
    public class SquadUpLevelSubParser<IT> : ISubMsgParser<IDyMsgParser<IT>, IT>
        where IT : class, IContext
    {
        private IDyMsgParser<IT> m_Owner;

        private ConcurrentDictionary<string, ConcurrentDictionary<short, byte>> UserSquadLevelDict =
            new ConcurrentDictionary<string, ConcurrentDictionary<short, byte>>();
        private static readonly Regex _regex = new Regex("升([0-9a-wA-W]*)");
        private DebugPlugin _cxt;
        private IDyMsgParser<DebugPlugin> _msgParser;

        private ConcurrentDictionary<int, ISquadUpLevelListener> _listeners =
            new ConcurrentDictionary<int, ISquadUpLevelListener>();
        public void Init(IDyMsgParser<IT> owner)
        {
            m_Owner = owner;
            Locator.Instance.Deposit(this);
            _cxt = Locator.Instance.Get<DebugPlugin>();
            _msgParser = _cxt.messageDispatcher.GetMsgParser();
        }

        public void OnStartGame()
        {
            
        }

        public void Stop()
        {
            
        }
        
        public void AddListener(ISquadUpLevelListener listener)
        {
            _listeners.TryAdd(listener.GetHashCode(), listener);
        }

        public void RemoveListener(ISquadUpLevelListener listener)
        {
            _listeners.TryRemove(listener.GetHashCode(),out _);
        }
        
        public void NotifySquadUpLevel(string uid, short sid, byte lvl,SquadData old,SquadData @new)
        {
            foreach (var it in _listeners)
            {
                it.Value.OnSquadUpLevel(uid, sid, lvl,old,@new);
            }
        }

        public bool Parse(DyMsgOrigin msg)
        {
            Match match = null;
            if(msg.barType == MsgTypeEnum.Comment &&
               (match = _regex.Match(msg.msg.CommentText)).Success)
            {
                global::InteractionGame.Utils.StringToDictAndForeach(match.Groups[1].Value, v =>
                {
                    for (int i = 0; i < v.Value; ++i)
                    {
                        var lvl = GetSquadLevel(msg.msg.OpenID, v.Key);
                        var ud = _msgParser.GetUserData(msg.msg.OpenID);
                        var sd = Aoe4DataConfig.GetSquadPure(v.Key, lvl, ud.Group);
                        if (sd == null) return;
                        if (!sd.RealHasNextLevel(ud.Group))
                        {
                            _cxt.PrintGameMsg($"{ud.NameColored}{sd.Name}已是最高级");
                            return;
                        }
                        UpSquadLevel(ud, msg.msg.UserName, sd);
                    }
                });
            }
            return false;
        }

        private void UpSquadLevel(UserData ud,string name, SquadData sd)
        {
            double price = 0.0;
            if (Consume(ud.Id, (price = sd.RealUpLevelPrice(ud.Group)),out int consumeHonor,out int consumeGold))
            {
                _msgParser.GetSubMsgParse<GroupUpLevel<DebugPlugin>>().NotifyDepleteGold(ud.Group, (int)price);
                _msgParser.UpdateUserData(ud.Id, price,0);
                var next = sd.RealNextLevelRef(ud.Group);
                SetSquadLevel(ud.Id,sd.Sid, next.Level);
                NotifySquadUpLevel(ud.Id,(short)sd.Sid,(byte)next.Level,sd,next);
                LargePopTipsDataBuilder.Create($"{name}", $"升至{next.Name}")
                    .SetBottom(string.Concat(consumeHonor > 0 ? $"消耗{consumeHonor}功勋" : "", $"消耗{consumeGold}金币"))
                    .SetLeftColor(LargeTips.GetGroupColor(ud.Group)).SetRightColor(LargeTips.Yellow).Show();
            }else
                _cxt.PrintGameMsg($"{ud.NameColored}升级{sd.Name}失败，资源不足");
        }

        private void SetSquadLevel(string uid, int sid, int level)
        {
            if (UserSquadLevelDict.TryGetValue(uid, out var dict))
            {
                if (dict.ContainsKey((short)sid))
                    dict[(short)sid] = (byte)level;
                else
                    dict.TryAdd((short)sid, (byte)level);
            }
            else
            {
                dict = new ConcurrentDictionary<short, byte>();
                dict.TryAdd((short)sid, (byte)level);
                UserSquadLevelDict.TryAdd(uid, dict);
            }
        }

        private bool Consume(string uid, double price,out int consumeHonor,out int consumeGold)
        {
            consumeHonor = 0;
            consumeGold = 0;
            var resMgr = _cxt.messageDispatcher.GetResourceMgr();
            var res = resMgr.GetResource(uid);
            if (res >= price && resMgr.RemoveResource(uid,price))
            {
                consumeGold = (int)price;
                return true;
            }
            else
            {
                var honor = (price - res) / Aoe4DataConfig.HonorGoldFactor; 
                if(honor < 1)
                    return false;
                var restGold = (honor - Math.Truncate(honor)) * Aoe4DataConfig.HonorGoldFactor;
                var f = 0;

                if (!DB.DBMgr.Instance.DepleteHonor(uid, (int)honor))
                    return false;
                if (resMgr.RemoveResource(uid, res))
                {
                    consumeGold = (int)res;
                    consumeHonor = (int)honor;
                    if (restGold > 0)
                        resMgr.AddResource(uid, restGold);
                    return true;
                }
                else
                    DB.DBMgr.Instance.AddHonor(uid, (int)honor);
            }
            return false;
        }

        public void OnTick(float delat)
        {
            
        }
        
        public int GetSquadLevel(string uid, int squadId)
        {
            if (!UserSquadLevelDict.ContainsKey(uid))
                return 1;
            if (!UserSquadLevelDict[uid].ContainsKey((short)squadId))
                return 1;
            return UserSquadLevelDict[uid][(short)squadId];
        }

        public void OnClear()
        {
            UserSquadLevelDict.Clear();
        }

        public void Start()
        {

        }
    }
}