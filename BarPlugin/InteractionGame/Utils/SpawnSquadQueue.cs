using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using BililiveDebugPlugin.InteractionGame.Parser;
using conf.Squad;
using InteractionGame;
using InteractionGame.plugs.config;
using Utils;

namespace BililiveDebugPlugin.InteractionGameUtils
{

    public abstract class SpawnSquadQueueWithMerge : ISpawnSquadQueue
    {
        public override void AppendAction(ISpawnSquadAction action)
        {
            base.AppendAction(HandleMerge(action));
        }

        private ISpawnSquadAction HandleMerge(ISpawnSquadAction action)
        {
            int count = action.GetCount();
            if(count >= 400)
            {
                var mult = Math.Min(count / 200, 5);
                var rest = action.Merge(mult);
                var user = action.GetUser() as UserData;
                Locator.Get<IContext>().PrintGameMsg($"{user.NameColored}触发合批x{mult}倍属性");
                if (rest != null)
                    base.AppendAction(rest);
            }
            return action;
        }
    }

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
        private IGameState GameState => _gameState ?? (_gameState = Locator.Get<IGameState>());
        private IGameState _gameState = null;
        private ConcurrentDictionary<int, CheckSCCxt> _checkSCCxtMap = new ConcurrentDictionary<int, CheckSCCxt>();
        public override bool IsGameEnd()
        {
            if(debugPlugin == null) debugPlugin = Locator.Get<IContext>();
            return debugPlugin.IsGameStart() != EGameState.Started;
        }

        protected override bool IsGroupLimit(int count, int remaining)
        {
            if(count < Locator.Get<IConstConfig>().OneTimesSpawnSquadCount)
                return count > remaining;
            return remaining < Locator.Get<IConstConfig>().OneTimesSpawnSquadCount;
        }

        protected override int RemainingQuantity(int group)
        {
            return Locator.Get<IConstConfig>().SquadCountLimit - GameState.GetSquadCount(group);
        }

        
    }

    public enum EFallbackType:int
    {
        None = 0,
        Honor = 1,
        Gift = 2,
    }


    public abstract class BaseSpawnFallback : ISpawnFallback
    {
        private double percentageScale = 1.0;

        public double PercentageScale { get => percentageScale; set => percentageScale = value; }

        public abstract void Fallback();
        public abstract void SetPercentage(double percentage);

        public abstract int Type();
    }


    public class HonorSpawnSquadFallback : BaseSpawnFallback
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

        public override void Fallback()
        {
            if (_honor > 0)
            {
                DB.DBMgr.Instance.AddHonor(_user.Id, _honor);
                Locator.Get<IContext>().PrintGameMsg($"{_user.NameColored}补偿了{_honor}功勋");
            }
        }

        public override int Type()
        {
            return (int)_type;
        }

        public override void SetPercentage(double percentage)
        {
            _honor = (int)Math.Floor(_origin * (percentage * PercentageScale));
        }
    }

    public class EmptySpawnSquadFallback : ISpawnFallback
    {
        public void Fallback()
        {
        }

        public int Type()
        {
            return (int)EFallbackType.None;
        }

        public void SetPercentage(double percentage)
        {
            
        }
    }

    public class GiftSpawnSquadFallback : BaseSpawnFallback
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

        public override void Fallback()
        {
            int count = (int)Math.Floor(_count);
            if (count > 0)
            {
                DB.DBMgr.Instance.AddGiftItem(_user.Id, Gift, count);
                Locator.Get<IContext>().PrintGameMsg($"{_user.NameColored}补偿了{count}个{Gift}");
            }

            int honor = (int)((_count - count) * _price);
            if (honor > 0)
            {
                DB.DBMgr.Instance.AddHonor(_user.Id, honor);
                Locator.Get<IContext>().PrintGameMsg($"{_user.NameColored}补偿了{honor}功勋");
            }
        }

        public override int Type()
        {
            return (int)_type;
        }

        public override void SetPercentage(double percentage)
        {
            _count = _origin * (percentage * PercentageScale);
        }
    }

    public static class SpawnSquad
    {
        public static void SendSpawnSquad(this ISpawnSquadAction a, UserData u, int c, SquadData sd, ushort attribute = 0,
            bool log = false,double scoreScale = 1)
        {
            var cxt = Locator.Get<IContext>();
            var target = u.Id_int < 0 ? -1 : cxt.GetPlayerParser().GetTarget(u.Id);
            var self = u.Id_int < 0 ? u.Group : cxt.GetPlayerParser().GetGroupById(u.Id);
            Locator.Get<IGameState>().OnSpawnSquad(self, c * sd.GetCountMulti());
            cxt.GetBridge().ExecSpawnSquad(u, sd, c, target);
            if(u.Id_int > 0 && sd.Score > 0)
                cxt.GetMsgParser().UpdateUserData(u.Id,sd.Score * c * scoreScale,c);
            if (log)
                Locator.Get<IContext>().Log($"-Spawn g = {self} num = {c}");
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
            bool log = false,double scoreScale = 1)
        {
            var rc = 0;
            if (group.Count == 0) return 0;
            var cxt = Locator.Get<IContext>();
            var target = u.Id_int < 0 ? -1 : cxt.GetPlayerParser().GetTarget(u.Id);
            var self = u.Id_int < 0 ? u.Group : cxt.GetPlayerParser().GetGroupById(u.Id);
            
            var op = u?.AppendSquadAttribute(attribute) ?? 0;
            rc = cxt.GetBridge().ExecSpawnGroup(u, group,target, multiple);
            if(rc > 0)
                Locator.Get<IGameState>().OnSpawnSquad(self, rc);
            if (u.Id_int > 0 && score > 0)
                cxt.GetMsgParser().UpdateUserData(u.Id,(int)(score * multiple * scoreScale) ,rc);
            if (log)
                Locator.Get<IContext>().Log($"--Spawn g = {self} num = {rc}");
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
        protected int _priority = 0;
        public bool IsGreedy { get => _isGreedy; set => _isGreedy = value; }

        protected override void OnSpawned(double res)
        {
            var user = GetUser() as UserData;
            if (user == null) return;
            if (_givHonor > 0)
            {
                var v = (long)Math.Ceiling(res * _givHonor * GetPercentageScale());
                AddHonor(user, v);
            }
            if(_upLevelgold > 0)
            {
                var v = Math.Ceiling(res * _upLevelgold * GetPercentageScale());
                Locator.Get<IContext>().GetMsgParser().GetSubMsgParse<GroupUpLevel>()?.NotifyDepleteGold(user.Group, (int)v);
            }
        }

        public BaseSpawnSquadAction SetIsGreedy(bool isGreedy)
        {
            _isGreedy = isGreedy;
            return this;
        }
        public BaseSpawnSquadAction SetPriority(int priority)
        {
            _priority = priority;
            return this;
        }

        private void AddHonor(UserData u, long v)
        {
            if (u.GuardLevel > 0) 
                v += (long)Math.Ceiling(v * Locator.Get<IConstConfig>().GetPlayerHonorAddition(u.RealGuardLevel));
            if (DB.DBMgr.Instance.AddHonor(u, v) > 0)
                Locator.Get<IContext>().PrintGameMsg($"{u.NameColored}获得{v}功勋");
        }

        protected override void OnSpawnedAll()
        {
            if (_restGold > 0)
            {
                var user = GetUser() as UserData;
                if (user == null) return;
                Locator.Get<IContext>().GetResourceMgr().AddResource(user.Id, _restGold);
            }
        }

        public override int GetPriority()
        {
            return _priority;
        }

        protected abstract double GetPercentageScale();
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
                if (!IsGreedy && @out >= Locator.Get<IConstConfig>().OneTimesSpawnSquadCount)
                    break;
            }
            return (double)@out / (double)_count;
        }
        protected void SpawnInternalStep(ref int count,ref int max,ref int @out)
        {
            var sc = Math.Min(count, Locator.Get<IConstConfig>().OneTimesSpawnSquadCount);
            if (sc > max) sc = max;
            this.SendSpawnSquad(_user, sc, _squad, _attribute, true,scoreScale:GetPercentageScale());
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

        public override ISpawnSquadAction Merge(int multiple)
        {
            _Percentage = _Percentage / multiple;
            if (_fallback != null && _fallback is BaseSpawnFallback fallback)
            {
                fallback.PercentageScale = multiple;
                fallback.SetPercentage(_Percentage); 
            }
            _attribute = Utility.AttrMult(_attribute, multiple);
            return null;
        }

        protected override double GetPercentageScale()
        {
            if (_fallback != null && _fallback is BaseSpawnFallback fallback)
            {
                return fallback.PercentageScale;
            }
            return 1;
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
            if(count > 0 && max > 0 && (IsGreedy || sumOut < Locator.Get<IConstConfig>().OneTimesSpawnSquadCount))
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
                var sc = Math.Min(count, Locator.Get<IConstConfig>().OneTimesSpawnSquadCount);
                if(sc > max) sc = max;
                var c = (double)sc / _Squad.specialCount;                
                if (c <= 0) break;
                var rc = 0;
                foreach (var it in _Squad.specialSquad)
                {
                    var realCount = (int)Math.Round(c * it.Item2);
                    if (realCount <= 0) continue;
                    rc += realCount;
                    this.SendSpawnSquad(_user, realCount, it.Item1, _Squad.AddedAttr, true,scoreScale:GetPercentageScale());
                }
                if (rc <= 0)
                    return _specialPercentage;
                count -= rc;
                max -= rc;
                @out += rc;
                sumOut += rc;
                if (!IsGreedy && sumOut >= Locator.Get<IConstConfig>().OneTimesSpawnSquadCount)
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
                var sc = Math.Min(count, Locator.Get<IConstConfig>().OneTimesSpawnSquadCount);
                if(sc > max) sc = max;
                var c = (double)sc / _Squad.normalCount;
                if(c <= 0) break;
                var rc = this.SendSpawnSquad(_user, _Squad.squad,_Squad.normalScore, _Squad.normalCount, c, _Squad.AddedAttr,true,
                    scoreScale:GetPercentageScale());
                if (rc <= 0)
                    return _normalPercentage;
                count -= rc;
                max -= rc;
                @out += rc;
                sumOut += rc;
                if (!IsGreedy && sumOut >= Locator.Get<IConstConfig>().OneTimesSpawnSquadCount)
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
        public override ISpawnSquadAction Merge(int multiple)
        {
            _specialPercentage /= multiple;
            _normalPercentage /= multiple;
            if (_fallback != null && _fallback is BaseSpawnFallback fallback)
            {
                fallback.PercentageScale = multiple;
                _Percentage = _specialPercentage * _constSpecialPercentage + _normalPercentage * (1.0 - _constSpecialPercentage);
                fallback.SetPercentage(_Percentage);
            }
            _Squad.AddedAttr = Utility.AttrMult(_Squad.AddedAttr, multiple);
            return null;
        }

        protected override double GetPercentageScale()
        {
            if (_fallback != null && _fallback is BaseSpawnFallback fallback)
            {
                return fallback.PercentageScale;
            }
            return 1;
        }
    }
}