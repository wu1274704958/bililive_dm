using BililiveDebugPlugin.DB;
using conf.Gift;
using conf.plugin;
using InteractionGame;
using InteractionGame.Context;
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
        }
        protected Dictionary<EFuncId, Func<UserData, AnyArray,bool>> FuncDict = new Dictionary<EFuncId, Func<UserData, AnyArray, bool>>();
        protected Random _random = new Random();

        public override void Init()
        {
            Locator.Instance.Deposit<IGiftMgr>(this);
            base.Init();
            AddApplyFunc(EFuncId.AddGold, AddGoldFunc);
            AddApplyFunc(EFuncId.ChangeTower, ChangeTower);
            AddApplyFunc(EFuncId.AddHonor, AddHonor);
        }

        private bool AddHonor(UserData data, AnyArray array)
        {
            var honor = 0;

            if(array.Count == 1 && array.TryGet<int>(0,out var v))
                honor = v;
            if (array.Count == 2 && array.TryGet<int>(0, out var b) && array.TryGet<int>(1, out var e))
                honor = _random.Next(b,e);

            if(honor > 0)
            {
                _context.PrintGameMsg($"{data.NameColored}获得了{honor}功勋");
                DBMgr.Instance.AddHonor(data.Id, honor);
                return true;
            }
            return false;
        }

        private bool ChangeTower(UserData user, AnyArray args)
        {
            if (args.TryGet<int>(0, out var towerUnit))
            {
                var tower = Locator.Instance.Get<ISquadMgr>().GetSquadById(towerUnit);
                if (tower == null)
                    return false;
                _context.PrintGameMsg($"{user.NameColored}更换了{tower.Name}");
                _context.GetBridge().ChangeTower(user, tower);
                return true;
            }
            return false;
        }
        private bool AddGoldFunc(UserData user, AnyArray args)
        {
            var gold = 0;
            if (args.Count == 1 && args.TryGet<int>(0, out var v))
                gold = v;
            if (args.Count == 2 && args.TryGet<int>(0, out var b) && args.TryGet<int>(1, out var e))
                gold = _random.Next(b, e);
            if (gold > 0)
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
            _context = Locator.Instance.Get<IContext>();
        }

        public void AddApplyFunc(EFuncId id, Func<UserData, AnyArray, bool> func)
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
                        ApplyFunction(func.Key,func.Value, user);
                    }
                }
                return true;
            }
            return false;
        }

        private bool ApplyFunction(int funcId,AnyArray args, UserData user)
        {
            if(FuncDict.TryGetValue((EFuncId)funcId,out var func))
            {
                return func(user,args);
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
