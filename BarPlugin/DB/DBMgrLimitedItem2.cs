using BililiveDebugPlugin.DB.Model2;
using System;
using System.Collections.Generic;
using Utils;
using UserData = InteractionGame.UserData;

namespace BililiveDebugPlugin.DB
{
    public partial class DBMgr2 : Singleton<DBMgr2>
    {
        protected ItemData HandleLimitedItem(ItemData itemData)
        {
            if(itemData == null)
                return null;
            if((itemData.Type & Model.EItemType.LimitedTime) == Model.EItemType.LimitedTime && IsExpired(itemData))
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

        public ItemData GetItem(long id, string name)
        {
            return HandleLimitedItem(m_fsql.Select<ItemData>().Where((a) => a.OwnerId == id && a.Name == name).ToOne());
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

        public List<ItemData> GetUserItems(long id, int limit = 100)
        {
            return HandleLimitedItems(m_fsql.Select<ItemData>().Where((a) => a.OwnerId == id).Limit(limit).ToList());
        }
        
        public List<ItemData> GetUserItems(long id,Model.EItemType type, int limit = 100)
        {
            var res = m_fsql.Select<ItemData>().Where((a) => a.OwnerId == id && (a.Type & type) == a.Type).Limit(limit)
                .ToList();
            if((type & Model.EItemType.LimitedTime) == Model.EItemType.LimitedTime)
                return HandleLimitedItems(res);
            return res;
        }

        public int AddLimitedItem(long uid, string name, int num,int price, TimeSpan duration)
        {
            int ret = 0;
            var itemData = GetItem(uid, name);
            if (itemData == null)
            {
                var newItem = ItemData.Create(name, Model.EItemType.LimitedTime, price, (DateTime.Now + duration).ToSecond(),
                    num, uid);
                ret += m_fsql.Insert(newItem).ExecuteAffrows();
            }
            else
                ret += ChangeItemExt(itemData, itemData.Ext + (int)duration.TotalSeconds);
            return ret;
        }
        public int AddLimitedItem(long uid, string name, int num, int price,int month = 1)
        {
            int ret = 0;
            var itemData = GetItem(uid, name);
            var now = DateTime.Now;
            var endTime = now.AddMonths(month);
            if (itemData == null)
            {
                var newItem = ItemData.Create(name, Model.EItemType.LimitedTime, price, endTime.ToSecond(),
                    num, uid);
                ret += m_fsql.Insert(newItem).ExecuteAffrows();
            }
            else
                ret += ChangeItemExt(itemData, itemData.Ext + (int)(endTime - now).TotalSeconds);
            return ret;
        }
    }
}
