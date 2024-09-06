using BililiveDebugPlugin.DB;
using conf.Gift;
using conf.plugin;
using InteractionGame;
using InteractionGame.Context;
using InteractionGame.Parser;
using InteractionGame.plugs;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Windows.Documents;
using Utils;

namespace BarPlugin.InteractionGame.plugs.bar
{
    public class GiftMgr : IPlug<EGameAction>, IGiftMgr
    {
        private IContext _context;
        public enum EFuncId : int
        {
            AddGold = 1,
            ChangeTower = 2,
            AddHonor = 3,
            RandomGift = 4,
            RandomSquad = 5,
        }
        protected Dictionary<EFuncId, Func<UserData, AnyArray, GiftItem,bool>> FuncDict = new Dictionary<EFuncId, Func<UserData, AnyArray,GiftItem, bool>>();
        protected Random _random = new Random();
        private IGlobalConfig _globalConfig;

        public override void Init()
        {
            Locator.Deposit<IGiftMgr>(this);
            base.Init();
            AddApplyFunc(EFuncId.AddGold, AddGoldFunc);
            AddApplyFunc(EFuncId.ChangeTower, ChangeTower);
            AddApplyFunc(EFuncId.AddHonor, AddHonor);
            AddApplyFunc(EFuncId.RandomGift, RandomGift);
            AddApplyFunc(EFuncId.RandomSquad, RandomSquad);
        }

        private bool RandomSquad(UserData data, AnyArray array,GiftItem giftItem)
        {
            Dictionary<int, int> squad = null;
            Dictionary<int, KeyValuePair<int, int>> squadRandom = null;
            Dictionary<int, int> probability = null;
            if (array.TryGet<string>(0, out var squadKey) && _globalConfig.GetConfig<Dictionary<int, int>>(squadKey, out squad)) ;
            if (array.TryGet<string>(0, out squadKey) && _globalConfig.GetConfig<Dictionary<int, KeyValuePair<int, int>>>(squadKey, out squadRandom)) ;
            if (array.TryGet<string>(1, out var probabilityKey) && _globalConfig.GetConfig<Dictionary<int, int>>(probabilityKey, out probability)) ;
            int squadId = 0;
            int count = 0;
            if (squad != null)
            {
                var g = RandomGift(squad, probability);
                squadId = g.Key;
                count = g.Value;
            }
            else if (squadRandom != null)
            {
                var g = RandomGift(squadRandom, probability);
                squadId = g.Key;
                count = _random.Next(g.Value.Key, g.Value.Value);
            }
            var sd = Locator.Get<ISquadMgr>().GetSquadById(squadId);
            if (sd == null)
                return false;
            _context.GetMsgParser().SendSpawnSquadQueue(data, sd, count, giftItem.Price, giftItem.Id, count);
            return true;
        }

        private bool RandomGift(UserData data, AnyArray array, GiftItem giftItem)
        {
            Dictionary<string, int> gifts = null;
            Dictionary<string, KeyValuePair<int, int>> giftRandom = null;
            Dictionary<string, int> probability = null;
            if (array.TryGet<bool>(0, out var apply, false)) ;
            if (array.TryGet<string>(1, out var giftKey) && _globalConfig.GetConfig<Dictionary<string, int>>(giftKey, out gifts)) ;
            if (array.TryGet<string>(1, out giftKey) && _globalConfig.GetConfig<Dictionary<string, KeyValuePair<int,int>>>(giftKey, out giftRandom)) ;
            if (array.TryGet<string>(2, out var probabilityKey) && _globalConfig.GetConfig<Dictionary<string,int>>(probabilityKey, out probability)) ;
            string gift = null;
            int count = 0;
            if(gifts != null)
            {
                var g = RandomGift(gifts, probability);
                gift = g.Key;
                count = g.Value;
            }else if(giftRandom != null)
            {
                var g = RandomGift(giftRandom, probability);
                gift = g.Key;
                count = _random.Next(g.Value.Key, g.Value.Value);
            }
            if (gift != null && count > 0)
            {
                if (apply)
                    ApplyGift(gift, data, count);
                else
                    GiveGift(gift, data, count);
                return true;
            }
            return false;
        }

        private int GetProbability<K>(K key,Dictionary<K,int> probability)
        {
            if (probability == null)
                return 1;
            if (probability.TryGetValue(key, out var v))
                return v;
            else
                return 1;
        }
        private int GetAllProbability<K,V>(Dictionary<K, V> gifts, Dictionary<K, int> probability)
        {
            if (probability == null)
                return gifts.Count;
            else
            {
                var allProb = 0;
                foreach (var it in gifts)
                {
                    if (probability.TryGetValue(it.Key, out var v))
                        allProb += v;
                    else
                        allProb += 1;
                }
                return allProb;
            }
        }

        private KeyValuePair<K,V> RandomGift<K,V>(Dictionary<K,V> gifts, Dictionary<K,int> probability)
        {
            var allProb = GetAllProbability(gifts, probability);
            var rand = _random.Next(allProb);
            foreach (var it in gifts)
            {
                if (rand <= 0)
                    return it;
                rand -= GetProbability(it.Key, probability);
            }
            return default;
        }

        private bool GetValueByAnyArray(UserData data,AnyArray array,out int res)
        {
            res = 0;
            if (array == null || array.Count == 0)
                return false;
            int honor = 0;
            int v = 0;
            int v2 = 0;
            string expr = null;
            if (array.Count == 1 && array.TryGet<int>(0, out v))
                honor = v;
            if (array.Count == 2 && array.TryGet<int>(0, out v) && array.TryGet<int>(1, out v2))
                honor = _random.Next((int)v, (int)v2);
            if (array.Count == 1 && array.TryGet<string>(0, out expr))
                honor = expr.EvaluateExpr<int>(data);
            if (array.Count == 2 && array.TryGet<int>(0, out v) && array.TryGet<string>(1, out expr))
                honor = expr.EvaluateExpr<int>(data, v);
            if (array.Count == 3 && array.TryGet<int>(0, out v) && array.TryGet<int>(1, out v2) && array.TryGet<string>(2, out expr))
                honor = expr.EvaluateExpr<int>(data, _random.Next(v, v2));

            if (TryGetMultiplyingPower(data, array, array.Count - 1, out var multiplying))
                honor = (int)(multiplying * honor);
            res = honor;
            return true;
        }

        private bool AddHonor(UserData data, AnyArray array, GiftItem giftItem)
        {
            if (GetValueByAnyArray(data,array,out int honor))
            {
                _context.PrintGameMsg($"{data.NameColored}获得了{honor}功勋");
                DBMgr.Instance.AddHonor(data.Id, honor);
                return true;
            }
            return false;
        }

        private bool TryGetMultiplyingPower(UserData data, AnyArray array, int idx, out float multiplying)
        {
            multiplying = 1.0f;
            if (data.RealGuardLevel <= 0)
                return false;
            if(array.Count > idx && array.TryGet<string>(idx,out var id))
            {
                if (_globalConfig.GetConfig<Dictionary<int, float>>(id, out var config) &&
                    config.TryGetValue(data.RealGuardLevel, out var res))
                {
                    multiplying = res;
                    return true;
                }
            }
            return false;
        }

        private bool ChangeTower(UserData user, AnyArray args, GiftItem giftItem)
        {
            if (args.TryGet<int>(0, out var towerUnit))
            {
                var tower = Locator.Get<ISquadMgr>().GetSquadById(towerUnit);
                if (tower == null)
                    return false;
                _context.PrintGameMsg($"{user.NameColored}更换了{tower.Name}");
                _context.GetBridge().ChangeTower(user, tower);
                return true;
            }
            return false;
        }
        private bool AddGoldFunc(UserData user, AnyArray args, GiftItem giftItem)
        {
            if (GetValueByAnyArray(user,args,out var gold))
            {
                _context.GetResourceMgr().AddResource(user.Id, gold);
                _context.PrintGameMsg($"{user.NameColored}获得{gold}g");
                return true;
            }
            return false;
        }

        public override void Start()
        {
            base.Start();
            _context = Locator.Get<IContext>();
            _globalConfig = Locator.Get<IGlobalConfig>();
        }

        public void AddApplyFunc(EFuncId id, Func<UserData, AnyArray, GiftItem,bool> func)
        {
            FuncDict.Add(id, func);
        }

        public bool ApplyGift(string gift, UserData user, int count = 1)
        {
            if (giftItemMgr.Dict.TryGetValue(gift, out var giftItem))
            {
                if (giftItem.Gifts != null)
                    GiveGift(giftItem.Gifts, user);
                if (giftItem.ApplyGifts != null)
                    ApplyGift(giftItem.ApplyGifts, user);
                if (giftItem.SpawnSquad != null)
                    ApplySpawnSquad(giftItem,count,giftItem.SpawnSquad,user);
                if(giftItem.Functions != null)
                {
                    foreach (var func in giftItem.Functions)
                    {
                        ApplyFunction(func.Key,func.Value, user,giftItem);
                    }
                }
                return true;
            }
            return false;
        }

        private bool ApplyFunction(int funcId,AnyArray args, UserData user, GiftItem giftItem)
        {
            if(FuncDict.TryGetValue((EFuncId)funcId,out var func))
            {
                return func(user,args,giftItem);
            }
            return false;
        }

        private void ApplySpawnSquad(GiftItem giftItem,int count,Dictionary<int, int> spawnSquad, UserData user)
        {
            _context.GetMsgParser().SpawnManySquadQueue(user.Id,SquadGroup.FromData(spawnSquad,user),count,giftItem.Price,giftItem.Id,count);
        }

        public bool GiveGift(string gift,UserData user, int count = 1)
        {
            if(giftItemMgr.Dict.TryGetValue(gift, out var giftItem))
            {
                _context.PrintGameMsg($"{user.NameColored}获得{gift}*{count}");
                if((giftItem.ItemType & (int)EItemType.LimitedTime) != 0)
                {
                    DBMgr.Instance.AddLimitedItem(user.Id, gift, giftItem.Ext, giftItem.Price, TimeSpan.FromMilliseconds(giftItem.Duration.TotalMilliseconds * count));
                }else
                if ((giftItem.ItemType & (int)EItemType.LimitedMonth) != 0)
                {
                    var month = (giftItem.Duration.Days / 30) * count;
                    if (month > 0)
                    {
                        DBMgr.Instance.AddLimitedItem(user.Id, gift, giftItem.Ext, giftItem.Price, month);
                    }
                }
                else
                {
                    DBMgr.Instance.AddGiftItem(user.Id, gift, count);
                }
                return true;
            }
            return false;
        }

        public override void Notify(EGameAction m)
        {

        }

        public override void Tick()
        {
            throw new NotImplementedException();
        }
        public GiftItemMgr giftItemMgr => conf.Gift.GiftItemMgr.GetInstance();
        public bool VaildGift(string gift)
        {
            return giftItemMgr.Dict.ContainsKey(gift);
        }

        public bool ApplyGift(Dictionary<string, int> gifts, UserData user)
        {
            var res = false;
            foreach(var it in gifts)
            {
                res = ApplyGift(it.Key,user,it.Value);
            }
            return res;
        }

        public bool GiveGift(Dictionary<string, int> gifts, UserData user)
        {
            var res = false;
            foreach (var it in gifts)
            {
                res = GiveGift(it.Key, user, it.Value);
            }
            return res;
        }

        public bool GetItem(string gift,out conf.Gift.GiftItem item)
        {
            return giftItemMgr.Dict.TryGetValue(gift, out item);
        }

        public void EnumerateGifts(Action<GiftItem> func)
        {
            foreach (var item in giftItemMgr.Dict)
            {
                func.Invoke(item.Value);
            }
        }
    }
}
