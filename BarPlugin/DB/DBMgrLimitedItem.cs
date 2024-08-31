using BililiveDebugPlugin.DB.Model;
using InteractionGame.plugs;
using System;
using System.Collections.Generic;
using Utils;
using UserData = InteractionGame.UserData;

namespace BililiveDebugPlugin.DB
{
    using SettlementData = UserData;
    public partial class DBMgr : Singleton<DBMgr>
    {
        protected ItemData HandleLimitedItem(ItemData itemData)
        {
            if(itemData == null)
                return null;
            if((itemData.Type & EItemType.LimitedTime) == EItemType.LimitedTime && IsExpired(itemData))
            {
                m_fsql.Delete<ItemData>(itemData.Id).ExecuteAffrows();
                return null;
            }
            return itemData;
        }
        protected List<ItemData> HandleLimitedItems(List<ItemData> itemDatas)
        {
            for (int i = itemDatas.Count - 1; i >= 0; i--)
            {
                itemDatas[i] = HandleLimitedItem(itemDatas[i]);
                if(itemDatas[i] == null)
                    itemDatas.RemoveAt(i);
            }
            return itemDatas;
        }

        private bool IsExpired(ItemData itemData)
        {
            var limitedTime = Common.GetDateTimeBySec(itemData.Ext);
            return DateTime.Now >= limitedTime;
        }

        public ItemData GetItem(string id, string name)
        {
            return HandleLimitedItem(m_fsql.Select<ItemData>().Where((a) => a.OwnerId == id && a.Name == name).ToOne());
        }
        public ItemData GetItem(string id, string name,int count)
        {
            return HandleLimitedItem(m_fsql.Select<ItemData>().Where((a) => a.OwnerId == id && a.Name == name && a.Count == count).ToOne());
        }

        public int AddGiftItem(SettlementData data, string name, int num)
        {
            int ret = 0;
            if (!Locator.Instance.Get<IGiftMgr>().GetItem(name, out var item))
                return 0;
            var newItem = ItemData.Create(item,num,data.Id);
            var d = GetUserOrCreate(data.Id, data.Name, data.Icon, out var isNew);
            if (isNew)
            {
                ret += m_fsql.Insert(d).ExecuteAffrows();
                if (ret > 0)
                    ret += m_fsql.Insert(newItem).ExecuteAffrows();
            }
            else
            {
                var itemData = GetItem(data.Id, name);
                if (itemData == null)
                    ret += m_fsql.Insert(newItem).ExecuteAffrows();
                else
                    ret += ChangeItemCount(itemData, num, out _);
            }
            return ret;
        }
        public int AddGiftItem(string id, string name, int num)
        {
            int ret = 0;
            if (!Locator.Instance.Get<IGiftMgr>().GetItem(name, out var item))
                return 0;


            var itemData = GetItem(id, name);
            if (itemData == null)
            {
                var newItem = ItemData.Create(item, num, id);
                ret += m_fsql.Insert(newItem).ExecuteAffrows();
            }
            else
                ret += ChangeItemCount(itemData, num, out _);
            return ret;
        }

        public int DepleteItem(string id, string name, int num, out int newCount)
        {
            newCount = 0;
            var d = GetUser(id);
            if (d == null)
                return 0;
            var itemData = GetItem(id, name);
            if (itemData == null)
                return 0;
            return ChangeItemCount(itemData, -num, out newCount);
        }

        private int ChangeItemCount(ItemData data, int offNum, out int newCount)
        {
            newCount = data.Count + offNum;
            if (newCount > 0)
                return m_fsql.Update<ItemData>(data.Id).Set(a => a.Count, newCount).ExecuteAffrows();
            else if (newCount == 0)
                return m_fsql.Delete<ItemData>(data.Id).ExecuteAffrows();
            else
                return -1;
        }
        private int ChangeItemExt(ItemData data, int ext)
        {
            return m_fsql.Update<ItemData>(data.Id).Set(a => a.Ext, ext).ExecuteAffrows();
        }

        public List<ItemData> GetUserItems(string id, int limit = 100)
        {
            return HandleLimitedItems(m_fsql.Select<ItemData>().Where((a) => a.OwnerId == id).Limit(limit).ToList());
        }
        
        public List<ItemData> GetUserItems(string id,EItemType type, int limit = 100)
        {
            var res = m_fsql.Select<ItemData>().Where((a) => a.OwnerId == id && (a.Type & type) == a.Type).Limit(limit)
                .ToList();
            if((type & EItemType.LimitedTime) == EItemType.LimitedTime)
                return HandleLimitedItems(res);
            return res;
        }

        public int AddLimitedItem(string uid, string name, int num,int price, TimeSpan duration)
        {
            int ret = 0;
            var itemData = GetItem(uid, name);
            if (itemData == null)
            {
                var newItem = ItemData.Create(name, EItemType.LimitedTime, price, (DateTime.Now + duration).ToSecond(),
                    num, uid);
                ret += m_fsql.Insert(newItem).ExecuteAffrows();
            }
            else
                ret += ChangeItemExt(itemData, itemData.Ext + (int)duration.TotalSeconds);
            return ret;
        }
        public int AddLimitedItem(string uid, string name, int num, int price,int month = 1)
        {
            int ret = 0;
            var itemData = GetItem(uid, name);
            var now = DateTime.Now;
            var endTime = now.AddMonths(month);
            if (itemData == null)
            {
                var newItem = ItemData.Create(name, EItemType.LimitedTime, price, endTime.ToSecond(),
                    num, uid);
                ret += m_fsql.Insert(newItem).ExecuteAffrows();
            }
            else
                ret += ChangeItemExt(itemData, itemData.Ext + (int)(endTime - now).TotalSeconds);
            return ret;
        }
        public int AddLimitedItemEx(string uid,string name, int numAsTag, int price,int month = 1)
        {
            int ret = 0;
            var itemData = GetItem(uid, name,numAsTag);
            var now = DateTime.Now;
            var endTime = now.AddMonths(month);
            if (itemData == null)
            {
                var newItem = ItemData.Create(name, EItemType.LimitedTime, price, endTime.ToSecond(),
                    numAsTag, uid);
                ret += m_fsql.Insert(newItem).ExecuteAffrows();
            }
            else
                ret += ChangeItemExt(itemData, itemData.Ext + (int)(endTime - now).TotalSeconds);
            return ret;
        }

        public int AddLimitedItemEx(string uid, string name, int numAsTag, int price, TimeSpan duration)
        {
            int ret = 0;
            var itemData = GetItem(uid, name,numAsTag);
            if (itemData == null)
            {
                var newItem = ItemData.Create(name, EItemType.LimitedTime, price, (DateTime.Now + duration).ToSecond(),
                    numAsTag, uid);
                ret += m_fsql.Insert(newItem).ExecuteAffrows();
            }
            else
                ret += ChangeItemExt(itemData, itemData.Ext + (int)duration.TotalSeconds);
            return ret;
        }
    }
}
