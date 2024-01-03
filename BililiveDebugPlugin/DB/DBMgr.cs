using System;
using System.Collections.Generic;
using System.Text;
using BililiveDebugPlugin.DB.Model;
using BililiveDebugPlugin.InteractionGame.Data;
using InteractionGame;
using UserData = InteractionGame.UserData;

namespace BililiveDebugPlugin.DB
{
    using SettlementData = UserData;
    public class DBMgr : Singleton<DBMgr>
    {
        private IFreeSql m_fsql;

        public DBMgr()
        {
            m_fsql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.Sqlite, @"Data Source=E:/Aoe4DB/live_game.db")
                .UseAutoSyncStructure(true) //自动同步实体结构到数据库，FreeSql不会扫描程序集，只有CRUD时才会生成表。
                .Build();
        }
        public IFreeSql Fsql => m_fsql;
        
        public void Dispose()
        {
            m_fsql?.Dispose();
            m_fsql = null;
            _Dispose();
        }

        public string PrintUsers(int limit = 100)
        {
            var ls = m_fsql.Select<Model.UserData>()
                .Limit(limit).ToList();
            return PrintUsers(ls);
        }
        public string PrintUsers(List<Model.UserData> ls)
        {
            var sb = new StringBuilder();
            foreach (var data in ls)
            {
                sb.Append(data.ToString());
            }
            return sb.ToString();
            
        }
        public Model.UserData GetUser(long id)
        {
            return m_fsql.Select<Model.UserData>().Where((a) => a.Id == id).ToOne();
        }
        public Model.UserData GetUserOrCreate(long id,string name,string icon,out bool isNew)
        {
            var r = m_fsql.Select<Model.UserData>().Where((a) => a.Id == id).ToOne();
            isNew = r == null;
            if (isNew)
                r = new Model.UserData()
                {
                    Id = id,
                    Name = name,
                    Icon = icon,
                    Score = 0,
                    Honor = 0,
                    WinTimes = 0,
                    SpawnSoldierNum = 0,
                    UserType = Model.EUserType.User,
                    Ext = 0,
                    SignTime = new DateTime(1990, 1, 1),
                    UpdateTime = DateTime.Now
                };
            return r;
        }

        public int OnSettlement(List<SettlementData> data,int winGroup,List<int> leastGroups)
        {
            int r = 0;
            int i = 0;
            foreach (var v in data)
            {
                v.Honor += Aoe4DataConfig.CalcHonorSettlement(v,v.Group == winGroup,leastGroups.Contains(v.Group),i);
                r += OnSettlement(v,v.Group == winGroup);
                ++i;
            }
            return r;
        }

        public int OnSettlement(SettlementData data,bool win)
        {
            var d = GetUserOrCreate(data.Id,data.Name,data.Icon,out var isNew);
            d.Score += data.Score;
            d.SpawnSoldierNum += data.Soldier_num;
            if (isNew)
            {
                d.Honor += data.Honor;
                d.WinTimes += win ? 1 : 0;
            }else
            {
                d.UpdateTime = DateTime.Now;
            }
            if (isNew)
                return m_fsql.Insert(d).ExecuteAffrows();
            else
            {
                var update = m_fsql.Update<Model.UserData>(data.Id)
                    .Set(a => a.Score,d.Score )
                    .Set(a => a.UpdateTime,d.UpdateTime )
                    .Set(a => a.SpawnSoldierNum,d.SpawnSoldierNum);
                    if(data.Name != d.Name)
                        update.Set(a => a.Name,data.Name );
                    if(data.Icon != d.Icon)
                        update.Set(a => a.Icon,data.Icon );
                    if(data.Honor > 0)
                        update.Set(a => a.Honor,d.Honor+data.Honor );
                    if(win)
                        update.Set(a => a.WinTimes, d.WinTimes + 1);
                    return update.ExecuteAffrows();
            }
        }
        
        public int AddHonor(SettlementData data,long honor)
        {
            var d = GetUserOrCreate(data.Id,data.Name,data.Icon,out var isNew);
            d.Honor += honor;
            if(isNew)
                return m_fsql.Insert(d).ExecuteAffrows();
            else
                return m_fsql.Update<Model.UserData>(data.Id).Set(a => a.Honor, d.Honor).ExecuteAffrows();
        }
        public bool DepleteHonor(long id,long honor)
        {
            var d = GetUser(id);
            if (d == null || d.Honor < honor)
                return false;
            d.Honor -= honor;
            return m_fsql.Update<Model.UserData>(id).Set(a => a.Honor, d.Honor).ExecuteAffrows() == 1;
        }
        
        public List<Model.UserData> GetSortedUsersByScore(int limit = 10)
        {
            return m_fsql.Select<Model.UserData>().OrderByDescending((a) => a.Score).Limit(limit).ToList();
        }
        public List<Model.UserData> GetSortedUsersByHonor(int limit = 10)
        {
            return m_fsql.Select<Model.UserData>().OrderByDescending((a) => a.Honor).Limit(limit).ToList();
        }
        
        public ItemData GetItem(long id,string name)
        {
            return m_fsql.Select<ItemData>().Where((a) => a.OwnerId == id && a.Name == name).ToOne();
        }

        public int AddGiftItem(SettlementData data, string name, int num)
        {
            int ret = 0;
            if (!Aoe4DataConfig.ItemDatas.TryGetValue(name, out var item))
                return 0;
            var newItem = ItemData.Create(item, num, data.Id);
            var d = GetUserOrCreate(data.Id,data.Name,data.Icon,out var isNew);
            if (isNew)
            {
                ret += m_fsql.Insert(d).ExecuteAffrows();
                if (ret > 0)
                    ret += m_fsql.Insert(newItem).ExecuteAffrows();
            }
            else
            {
                var itemData = GetItem(data.Id,name);
                if (itemData == null)
                    ret += m_fsql.Insert(newItem).ExecuteAffrows();
                else
                    ret += ChangeItemCount(itemData,num,out _);
            }
            return ret;
        }
        
        public int DepleteItem(long id,string name,int num,out int newCount)
        {
            newCount = 0;
            var d = GetUser(id);
            if (d == null)
                return 0;
            var itemData = GetItem(id,name);
            if (itemData == null)
                return 0;
            return ChangeItemCount(itemData,-num,out newCount);
        }

        private int ChangeItemCount(ItemData data, int offNum,out int newCount)
        {
            newCount = data.Count + offNum;
            if(newCount > 0)
                return m_fsql.Update<ItemData>(data.Id).Set(a => a.Count, newCount).ExecuteAffrows();       
            else if(newCount == 0)
                return m_fsql.Delete<ItemData>(data.Id).ExecuteAffrows();
            else
                return -1;
        }

        public List<ItemData> GetUserItems(long id,int limit = 100)
        {
            return m_fsql.Select<ItemData>().Where((a) => a.OwnerId == id).Limit(limit).ToList();
        }

        public bool SignIn(SettlementData data)
        {
            var d = GetUserOrCreate(data.Id,data.Name,data.Icon,out var isNew);
            if (isNew)
            {
                d.SignTime = DateTime.Now;
                return m_fsql.Insert(d).ExecuteAffrows() == 1;
            }
            else
            {
                var now = DateTime.Now;
                if (now > d.SignTime && (now.Day != d.SignTime.Day || now.Year != d.SignTime.Year ))
                {
                    return m_fsql.Update<Model.UserData>(d.Id).Set(a => a.SignTime, now).ExecuteAffrows() == 1;
                }
                else
                    return false;
            }
        }
        
    }
}