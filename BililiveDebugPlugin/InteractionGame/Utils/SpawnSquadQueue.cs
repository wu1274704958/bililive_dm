using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows;
using BililiveDebugPlugin.InteractionGame;
using BililiveDebugPlugin.InteractionGame.Data;
using BililiveDebugPlugin.InteractionGame.Parser;
using conf.Squad;
using InteractionGame;
using Utils;

namespace BililiveDebugPlugin.InteractionGameUtils
{
    public class SpawnSquadQueue : ISpawnSquadQueue
    {
        private IContext debugPlugin;
        private class CheckSCCxt {
            public int g;
            public SpawnSquadActionBound action;
            public Action<int, SpawnSquadActionBound> func;

            public CheckSCCxt(int g, SpawnSquadActionBound action, Action<int, SpawnSquadActionBound> func)
            {
                this.g = g;
                this.action = action;
                this.func = func;
            }

            public void OnResult(int c,int g)
            {
                func.Invoke(g, action);
            }
        }
        private Aoe4GameState GameState => _gameState ?? (_gameState = Locator.Instance.Get<Aoe4GameState>());
        private Aoe4GameState _gameState = null;
        private ConcurrentDictionary<int, CheckSCCxt> _checkSCCxtMap = new ConcurrentDictionary<int, CheckSCCxt>();
        public override bool IsGameEnd()
        {
            if(debugPlugin == null) debugPlugin = Locator.Instance.Get<IContext>();
            return debugPlugin.IsGameStart() != 1;
        }

        protected override bool IsGroupLimit(int count, int remaining)
        {
            if(count < Aoe4DataConfig.OneTimesSpawnSquadCount)
                return count > remaining;
            return remaining < Aoe4DataConfig.OneTimesSpawnSquadCount;
        }

        protected override int RemainingQuantity(int group)
        {
            return Aoe4DataConfig.SquadLimit - GameState.GetSquadCount(group);
        }

        //public override void AppendAction(ISpawnSquadAction action)
        //{
        //    var g = action.GetGroup();
        //    GameState.CheckNewSquadCount(g, this, (count, g_) => base.AppendAction(action));
        //}

        //protected override void PeekQueueAndSpawn(int g, SpawnSquadActionBound action)
        //{
        //    CheckSCCxt v = null;
        //    if (_checkSCCxtMap.TryGetValue(g,out v))
        //    {
        //        if (!GameState.HasCheckSquadCountTask(g, v))
        //        {
        //            v.action = action;
        //            GameState.CheckNewSquadCount(g, v, v.OnResult);
        //        }
        //    }
        //    else
        //    {
        //        _checkSCCxtMap.TryAdd(g, v = new CheckSCCxt(g, action, base.PeekQueueAndSpawn));
        //        GameState.CheckNewSquadCount(g, v, v.OnResult);
        //    }
        //}
    }

    public enum EFallbackType:int
    {
        None = 0,
        Honor = 1,
        Gift = 2,
    }
    public class HonorSpawnSquadFallback : ISpawnFallback
    {
        protected EFallbackType _type = EFallbackType.Honor;
        protected int _origin = 0;
        protected int _honor = 0;
        protected UserData _user;

        public HonorSpawnSquadFallback(int honor, UserData user)
        {
            _honor = _origin = honor;
            _user = user;
        }

        public virtual void Fallback()
        {
            if (_honor > 0)
            {
                DB.DBMgr.Instance.AddHonor(_user.Id, _honor);
                Locator.Instance.Get<IContext>().PrintGameMsg($"{_user.NameColored}补偿了{_honor}荣誉");
            }
        }

        public int GetType()
        {
            return (int)_type;
        }

        public void SetPercentage(double percentage)
        {
            _honor = (int)Math.Floor(_origin * percentage);
        }
    }

    public class EmptySpawnSquadFallback : ISpawnFallback
    {
        public void Fallback()
        {
        }

        public int GetType()
        {
            return (int)EFallbackType.None;
        }

        public void SetPercentage(double percentage)
        {
            
        }
    }

    public class GiftSpawnSquadFallback : ISpawnFallback
    {
        protected EFallbackType _type = EFallbackType.Gift;
        protected double _count = 0;
        protected double _origin = 0;
        protected string Gift;
        protected UserData _user;
        protected int _price = 0;
        public GiftSpawnSquadFallback(int count, string gift, UserData user, int price)
        {
            _count = _origin = count;
            Gift = gift;
            _user = user;
            _price = price;
        }

        public virtual void Fallback()
        {
            int count = (int)Math.Floor(_count);
            if (count > 0)
            {
                DB.DBMgr.Instance.AddGiftItem(_user.Id, Gift, count);
                Locator.Instance.Get<IContext>().PrintGameMsg($"{_user.NameColored}补偿了{count}个{Gift}");
            }

            int honor = (int)((_count - count) * _price);
            if (honor > 0)
            {
                DB.DBMgr.Instance.AddHonor(_user.Id, honor);
                Locator.Instance.Get<IContext>().PrintGameMsg($"{_user.NameColored}补偿了{honor}荣誉");
            }
        }

        public int GetType()
        {
            return (int)_type;
        }

        public void SetPercentage(double percentage)
        {
            _count = _origin * percentage;
        }
    }

    public static class SpawnSquad
    {
        public static void SendSpawnSquad(this ISpawnSquadAction a, UserData u, int c, SquadData sd, ushort attribute = 0,
            bool log = false)
        {
            var m_MsgDispatcher = Locator.Instance.Get<ILocalMsgDispatcher<DebugPlugin>>();
            if(m_MsgDispatcher == null) return;
            if (sd.Sid == Aoe4DataConfig.VILLAGER_ID)
            {
                if(u.Id < 0) return;
                m_MsgDispatcher.GetResourceMgr().SpawnVillager(u.Id, c);
                return;
            }
            var target = u.Id < 0 ? -1 : m_MsgDispatcher.GetPlayerParser().GetTarget(u.Id);
            var self = u.Id < 0 ? u.Group : m_MsgDispatcher.GetPlayerParser().GetGroupById(u.Id);
            Locator.Instance.Get<Aoe4GameState>().OnSpawnSquad(self, c * sd.GetCountMulti(), 5);
            var attackTy = sd.GetAttackType();
            var op = u?.AppendSquadAttribute(attribute,sd.GetAddHp(self),sd.GetAddDamage(self)) ?? 0;
            if (target < 0)
            {
                m_MsgDispatcher.GetBridge().ExecSpawnSquad(self + 1, sd.GetBlueprint(u.Group), c, u.Id, attackTy,op);
            }
            else
            {
                m_MsgDispatcher.GetBridge().ExecSpawnSquadWithTarget(self + 1, sd.GetBlueprint(u.Group), target + 1, c, u.Id,attackTy,op);
            }
            if(u.Id > 0)
                Locator.Instance.Get<IDyMsgParser<DebugPlugin>>().UpdateUserData(u.Id,sd.RealScore(u.Group) * c ,c);
            if (log)
                Locator.Instance.Get<IContext>().Log($"-Spawn g = {self} num = {c}");
        }

        public static int GetAttackType(this SquadData self)
        {
            if(self.SquadType >= (int)ESquadType.SiegeAttacker && self.SquadType_e != ESquadType.MultiSquad)
                return self.SquadType;
            return 0;
        }
        public static int GetCountMulti(this SquadData self)
        {
            return 1;
            if(self.SquadType_e == ESquadType.MultiSquad)
                return self.SquadCount;
            return 1;
        }
        public static int SendSpawnSquad(this ISpawnSquadAction a,UserData u, List<(SquadData, int)> group,double score,int squadNum,double multiple = 1.0,ushort attribute = 0,
            bool log = false)
        {
            var rc = 0;
            if (group.Count == 0) return 0;
            var m_MsgDispatcher = Locator.Instance.Get<ILocalMsgDispatcher<DebugPlugin>>();
            var target = u.Id < 0 ? -1 : m_MsgDispatcher.GetPlayerParser().GetTarget(u.Id);
            var self = u.Id < 0 ? u.Group : m_MsgDispatcher.GetPlayerParser().GetGroupById(u.Id);
            
            var op = u?.AppendSquadAttribute(attribute) ?? 0;
            if (target < 0)
            {
                rc = m_MsgDispatcher.GetBridge().ExecSpawnGroup(self + 1, group, u.Id,multiple,op1:op);
            }
            else
            {
                rc = m_MsgDispatcher.GetBridge().ExecSpawnGroupWithTarget(self + 1, target + 1, group, u.Id,multiple,op1:op);
            }
            if(rc > 0)
                Locator.Instance.Get<Aoe4GameState>().OnSpawnSquad(self, rc, 5);
            if (u.Id > 0)
                Locator.Instance.Get<IDyMsgParser<DebugPlugin>>().UpdateUserData(u.Id,(int)(score * multiple) ,rc);
            if (log)
                Locator.Instance.Get<IContext>().Log($"--Spawn g = {self} num = {rc}");
            return rc;
        }
        
    }

    public abstract class BaseSpawnSquadAction : ISpawnSquadAction
    {
        protected double _restGold;
        protected double _upLevelgold;
        protected int _givHonor;
        protected bool _notRecycle = false;
        protected bool _isGreedy = false;
        public bool IsGreedy { get => _isGreedy; set => _isGreedy = value; }

        protected override void OnSpawned(double res)
        {
            var user = GetUser() as UserData;
            if (user == null) return;
            if (_givHonor > 0)
            {
                var v = (long)Math.Ceiling(res * _givHonor);
                AddHonor(user, v);
            }
            if(_upLevelgold > 0)
            {
                var v = Math.Ceiling(res * _upLevelgold);
                Locator.Instance.Get<IDyMsgParser<DebugPlugin>>().GetSubMsgParse<GroupUpLevel<DebugPlugin>>().NotifyDepleteGold(user.Group, (int)v);
            }
        }

        public BaseSpawnSquadAction SetIsGreedy(bool isGreedy)
        {
            _isGreedy = isGreedy;
            return this;
        }

        private void AddHonor(UserData u, long v)
        {

            if (u.GuardLevel > 0) v += (long)Math.Ceiling(v * Aoe4DataConfig.PlayerHonorResAddFactorArr[u.RealGuardLevel]);
            if (DB.DBMgr.Instance.AddHonor(u, v) > 0)
                Locator.Instance.Get<IContext>().PrintGameMsg($"{u.NameColored}获得{v}功勋");
        }

        protected override void OnSpawnedAll()
        {
            if (_restGold > 0)
            {
                var user = GetUser() as UserData;
                if (user == null) return;
                Locator.Instance.Get<DebugPlugin>().messageDispatcher.GetResourceMgr().AddResource(user.Id, _restGold);
            }
        }
    }

    public class SpawnSingleSquadAction<FT> : BaseSpawnSquadAction
        where FT: ISpawnFallback
    {
        public UserData _user;
        public int _count;
        public SquadData _squad;
        public ushort _attribute = 0;
        protected FT _fallback;

        public SpawnSingleSquadAction(UserData user,int count, SquadData squad, FT fallback,
            int restGold,int upLevelgold,int giveHonor,bool notRecycle = false, ushort attribute = 0)
        {
            _user = user;
            _count = count;
            _squad = squad;
            _fallback = fallback;

            _restGold = restGold;
            _upLevelgold = upLevelgold;
            _givHonor = giveHonor;
            _notRecycle = notRecycle;
            _attribute = attribute;
        }

        protected override double SpawnInternal(int max)
        {
            int count = (int)(_count * _Percentage);
            int @out = 0;
            while (count > 0 && max > 0)
            {
                SpawnInternalStep(ref count, ref max, ref @out);
                if (!IsGreedy && @out >= Aoe4DataConfig.OneTimesSpawnSquadCount)
                    break;
            }
            return (double)@out / (double)_count;
        }
        protected void SpawnInternalStep(ref int count,ref int max,ref int @out)
        {
            var sc = Math.Min(count, Aoe4DataConfig.OneTimesSpawnSquadCount);
            if (sc > max) sc = max;
            this.SendSpawnSquad(_user, sc, _squad, _attribute, true);
            count -= sc;
            max -= sc;
            @out += sc;
        }

        protected override ISpawnFallback GenerateFallback()
        {
            return _fallback;
        }

        public override int GetGroup()
        {
            return _user.Group;
        }

        public override int GetCount()
        {
            return (int)(_count * _Percentage);
        }

        public override void OnDestroy()
        {

        }

        public override object GetUser()
        {
            return _user;
        }
    }
    
    public class SpawnGroupSquadAction<FT> : BaseSpawnSquadAction
        where FT: ISpawnFallback
    {
        public UserData _user;
        public SquadGroup _Squad;
        public int _count;
        protected FT _fallback;
        protected double _specialPercentage = 1;
        protected double _normalPercentage = 1;
        private readonly double _constSpecialPercentage;

        public SpawnGroupSquadAction(UserData user, SquadGroup squad, int count, FT fallback,
            double restGold, double upLevelgold, int giveHonor, bool notRecycle = false)
        {
            _user = user;
            _Squad = squad;
            _count = count;
            _fallback = fallback;

            _restGold = restGold;
            _upLevelgold = upLevelgold;
            _givHonor = giveHonor;
            _notRecycle = notRecycle;

            _specialPercentage = (double)squad.specialCount / (double)squad.num;
            _normalPercentage = 1 - _specialPercentage;
            _constSpecialPercentage = _specialPercentage;
            _specialPercentage = _normalPercentage = 1;

        }

        protected override double SpawnInternal(int max)
        {
            var count = (int)(_Squad.specialCount * _specialPercentage * _count);
            var specialPercentage = 0.0;
            var normalPercentage = 0.0;
            var sumOut = 0;
            if(count > 0)
                specialPercentage = SpawnSpecialCount(ref max, ref count,ref sumOut);
            count = (int)(_Squad.normalCount * _normalPercentage * _count);
            if(count > 0 && max > 0 && (IsGreedy || sumOut < Aoe4DataConfig.OneTimesSpawnSquadCount))
                normalPercentage = SpawnNormalCount(ref max, ref count,ref sumOut);
            _specialPercentage -= specialPercentage;
            _normalPercentage -= normalPercentage;
            return specialPercentage * _constSpecialPercentage  + normalPercentage * (1.0 - _constSpecialPercentage);
        }

        private double SpawnSpecialCount(ref int max, ref int count,ref int sumOut)
        {
            var all = _Squad.specialCount * _count;
            var @out = 0;
            while (count > 0 && max > 0)
            {
                var sc = Math.Min(count, Aoe4DataConfig.OneTimesSpawnSquadCount);
                if(sc > max) sc = max;
                var c = (double)sc / _Squad.specialCount;                
                if (c <= 0) break;
                var rc = 0;
                foreach (var it in _Squad.specialSquad)
                {
                    var realCount = (int)Math.Round(c * it.Item2);
                    if (realCount <= 0) continue;
                    rc += realCount;
                    this.SendSpawnSquad(_user, realCount, it.Item1, _Squad.AddedAttr, true);
                }
                if (rc <= 0)
                    return _specialPercentage;
                count -= rc;
                max -= rc;
                @out += rc;
                sumOut += rc;
                if (!IsGreedy && sumOut >= Aoe4DataConfig.OneTimesSpawnSquadCount)
                    break;
            }
            return (double)@out / all;
        }
        private double SpawnNormalCount(ref int max, ref int count, ref int sumOut)
        {
            var all = _Squad.normalCount * _count;
            var @out = 0;
            while (count > 0 && max > 0)
            {
                var sc = Math.Min(count, Aoe4DataConfig.OneTimesSpawnSquadCount);
                if(sc > max) sc = max;
                var c = (double)sc / _Squad.normalCount;
                if(c <= 0) break;
                var rc = this.SendSpawnSquad(_user, _Squad.squad,_Squad.normalScore, _Squad.normalCount, c, _Squad.AddedAttr,true);
                if (rc <= 0)
                    return _normalPercentage;
                count -= rc;
                max -= rc;
                @out += rc;
                sumOut += rc;
                if (!IsGreedy && sumOut >= Aoe4DataConfig.OneTimesSpawnSquadCount)
                    break;
            }
            return (double)@out / all;
        }

        protected override ISpawnFallback GenerateFallback()
        {
            return _fallback;
        }

        public override int GetGroup()
        {
            return _user.Group;
        }

        public override int GetCount()
        {
            return (int)((_Squad.specialCount * _count) * _specialPercentage) + (int)((_Squad.normalCount * _count) * _normalPercentage);
        }

        public override void OnDestroy()
        {
            if(!_notRecycle && _Squad != null)
                SquadGroup.Recycle(_Squad);
        }
        public override object GetUser()
        {
            return _user;
        }
    }
}