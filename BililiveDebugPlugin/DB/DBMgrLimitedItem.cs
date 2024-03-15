using BililiveDebugPlugin.DB.Model;
using BililiveDebugPlugin.InteractionGame.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace BililiveDebugPlugin.DB
{
    using SettlementData = UserData;
    public partial class DBMgr : Singleton<DBMgr>
    {
        protected ItemData HandleLimitedItem(ItemData itemData)
        {
            if(itemData == null)
                return null;
            if(itemData.Type == EItemType.LimitedTime && IsExpired(itemData))
            {
                m_fsql.Delete<ItemData>(itemData.Id).ExecuteAffrows();
                return null;
            }
            return itemData;
        }

        private bool IsExpired(ItemData itemData)
        {
            var limitedTime = Utils.Utils.GetDateTimeBySec(itemData.Ext);
            return DateTime.Now >= limitedTime;
        }

        public ItemData GetItem(long id, string name)
        {
            return HandleLimitedItem(m_fsql.Select<ItemData>().Where((a) => a.OwnerId == id && a.Name == name).ToOne());
        }

        public int AddGiftItem(SettlementData data, string name, int num)
        {
            int ret = 0;
            if (!Aoe4DataConfig.ItemDatas.TryGetValue(name, out var item))
                return 0;
            var newItem = ItemData.Create(item, num, data.Id);
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
        public int AddGiftItem(long id, string name, int num)
        {
            int ret = 0;
            if (!Aoe4DataConfig.ItemDatas.TryGetValue(name, out var item))
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

        public int DepleteItem(long id, string name, int num, out int newCount)
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

        public List<ItemData> GetUserItems(long id, int limit = 100)
        {
            return m_fsql.Select<ItemData>().Where((a) => a.OwnerId == id).Limit(limit).ToList();
        }
    }
}
