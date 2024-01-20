using System;
using System.Collections.Generic;
using System.Windows;
using BililiveDebugPlugin.InteractionGame;
using BililiveDebugPlugin.InteractionGame.Data;
using BililiveDebugPlugin.InteractionGame.Parser;
using InteractionGame;
using Utils;

namespace BililiveDebugPlugin.InteractionGameUtils
{
    public class SpawnSquadQueue : ISpawnSquadQueue
    {
        protected override bool IsGroupLimit(int count, int remaining)
        {
            if(count < Aoe4DataConfig.OneTimesSpawnSquadCount)
                return count > remaining;
            return remaining >= Aoe4DataConfig.OneTimesSpawnSquadCount;
        }

        protected override int RemainingQuantity(int group)
        {
            return Aoe4DataConfig.SquadLimit - Locator.Instance.Get<Aoe4GameState>().GetSquadCount(group);
        }
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
        protected int _honor = 0;
        protected UserData _user;

        public HonorSpawnSquadFallback(int honor, UserData user)
        {
            _honor = honor;
            _user = user;
        }

        public virtual void Fallback()
        {
            if (_honor > 0)
            {
                DB.DBMgr.Instance.AddHonor(_user.Id, _honor);
                Locator.Instance.Get<IContext>().Log($"{_user.Name}补偿了{_honor}荣誉");
            }
        }

        public int GetType()
        {
            return (int)_type;
        }

        public void SetPercentage(double percentage)
        {
            _honor = (int)Math.Floor(_honor * percentage);
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
        protected string Gift;
        protected UserData _user;
        protected int _price = 0;
        public GiftSpawnSquadFallback(int count, string gift, UserData user, int price)
        {
            _count = count;
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
                Locator.Instance.Get<IContext>().Log($"{_user.Name}补偿了{count}个{Gift}");
            }

            int honor = (int)(Math.Floor(_count - count) * _price);
            if (honor > 0)
            {
                DB.DBMgr.Instance.AddHonor(_user.Id, honor);
                Locator.Instance.Get<IContext>().Log($"{_user.Name}补偿了{honor}荣誉");
            }
        }

        public int GetType()
        {
            return (int)_type;
        }

        public void SetPercentage(double percentage)
        {
            _count = _count * percentage;
        }
    }

    public static class SpawnSquad
    {
        public static void SendSpawnSquad(this ISpawnSquadAction a,UserData u, int sid, int c, Aoe4DataConfig.SquadData sd)
        {
            var m_MsgDispatcher = Locator.Instance.Get<ILocalMsgDispatcher<DebugPlugin>>();
            if(m_MsgDispatcher == null) return;
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
            Locator.Instance.Get<IDyMsgParser<DebugPlugin>>().UpdateUserData(u.Id,sd.Score * c ,c);
        }
        public static void SendSpawnSquad(this ISpawnSquadAction a,UserData u, List<(int,int)> group,int score,int squadNum,int multiple = 1)
        {
            if (group.Count == 0) return;
            var m_MsgDispatcher = Locator.Instance.Get<ILocalMsgDispatcher<DebugPlugin>>();
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
            Locator.Instance.Get<IDyMsgParser<DebugPlugin>>().UpdateUserData(u.Id,score * multiple ,squadNum * multiple);
        }
        
    }

    public abstract class BaseSpawnSquadAction : ISpawnSquadAction
    {
        protected int _restGold;
        protected int _upLevelgold;
        protected int _givHonor;
        protected bool _notRecycle = false;

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

        private void AddHonor(UserData u, long v)
        {

            if (u.GuardLevel > 0) v += (long)Math.Ceiling(v * Aoe4DataConfig.PlayerResAddFactorArr[u.GuardLevel]);
            if (DB.DBMgr.Instance.AddHonor(u, v) > 0)
                Locator.Instance.Get<IContext>().PrintGameMsg($"{u.Name}获得{v}功勋");
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
        public int _sid;
        public int _count;
        public Aoe4DataConfig.SquadData _squad;
        protected FT _fallback;

        public SpawnSingleSquadAction(UserData user, int sid, int count, Aoe4DataConfig.SquadData squad, FT fallback,
            int restGold,int upLevelgold,int giveHonor,bool notRecycle = false)
        {
            _user = user;
            _sid = sid;
            _count = count;
            _squad = squad;
            _fallback = fallback;

            _restGold = restGold;
            _upLevelgold = upLevelgold;
            _givHonor = giveHonor;
            _notRecycle = notRecycle;
        }

        protected override double SpawnInternal(int max)
        {
            int count = (int)(_count * _Percentage);
            while (count > 0 && max > 0)
            {
                var sc = Math.Min(count, Aoe4DataConfig.OneTimesSpawnSquadCount);
                if(sc > max) sc = max;
                this.SendSpawnSquad(_user, _sid, sc, _squad);
                count -= sc;
                max -= sc;
            }
            return (double)(_count - count) / (double)_count;
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
        public SquadData _Squad;
        public int _count;
        protected FT _fallback;
        protected double _specialPercentage = 1;
        protected double _normalPercentage = 1;
        private readonly double _constSpecialPercentage;

        public SpawnGroupSquadAction(UserData user, SquadData squad, int count, FT fallback,
            int restGold, int upLevelgold, int giveHonor, bool notRecycle = false)
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
            if(count > 0)
                specialPercentage = SpawnSpecialCount(ref max, ref count);
            count = (int)(_Squad.normalCount * _normalPercentage * _count);
            if(count > 0 && max > 0)
                normalPercentage = SpawnNormalCount(ref max, ref count);
            _specialPercentage -= specialPercentage;
            _normalPercentage -= normalPercentage;
            return specialPercentage * _constSpecialPercentage  + normalPercentage * (1 - _constSpecialPercentage);
        }

        private double SpawnSpecialCount(ref int max, ref int count)
        {
            var all = _Squad.specialCount * _count;
            while (count > 0 && max > 0)
            {
                var sc = Math.Min(count, Aoe4DataConfig.OneTimesSpawnSquadCount);
                if(sc > max) sc = max;
                var c = sc / _Squad.specialCount;
                if(c <= 0) break;
                foreach (var it in _Squad.specialSquad)
                    this.SendSpawnSquad(_user,it.Item1, c * it.Item2, Aoe4DataConfig.GetSquad(it.Item1));
                count -= c * _Squad.specialCount;
                max -= c * _Squad.specialCount;
            }
            return (double)(all - count) / all;
        }
        private double SpawnNormalCount(ref int max, ref int count)
        {
            var all = _Squad.normalCount * _count;
            while (count > 0 && max > 0)
            {
                var sc = Math.Min(count, Aoe4DataConfig.OneTimesSpawnSquadCount);
                if(sc > max) sc = max;
                var c = sc / _Squad.normalCount;
                if(c <= 0) break;
                this.SendSpawnSquad(_user, _Squad.squad,_Squad.normalScore, _Squad.normalCount, c);
                count -= c * _Squad.normalCount;
                max -= c * _Squad.normalCount;
            }
            return (double)(all - count) / all;
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
            return (int)(_Squad.specialCount * _specialPercentage + _Squad.normalCount * _normalPercentage);
        }

        public override void OnDestroy()
        {
            if(!_notRecycle && _Squad != null)
                SquadData.Recycle(_Squad);
        }
        public override object GetUser()
        {
            return _user;
        }
    }
}