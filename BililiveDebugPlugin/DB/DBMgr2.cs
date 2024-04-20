using System;
using System.Collections.Generic;
using System.Text;
using BililiveDebugPlugin.DB.Model2;
using BililiveDebugPlugin.InteractionGame.Data;
using Utils;
using UserData = InteractionGame.UserData;

namespace BililiveDebugPlugin.DB
{
    using SettlementData = UserData;
    public partial class DBMgr2 : Singleton<DBMgr2>
    {
        private IFreeSql m_fsql;

        public DBMgr2()
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
        public Model2.UserData GetUserByName(string name)
        {
            return m_fsql.Select<Model2.UserData>().Where((a) => a.Name == name).ToOne();
        }

       
       

        public void ForeachUsers(Action<Model.UserData> action)
        {
            m_fsql.Select<Model.UserData>().ToList().ForEach(action);
        }

        public int ClearAllUserScore()
        {
            return m_fsql.Update<Model.UserData>()
              .SetDto(new { Score = 0 })
              .Where(a => true)
              .ExecuteAffrows();
        }

        public int ClearSignInDate(long id)
        {
            return m_fsql.Update<Model.UserData>(id).Set(a => a.SignTime, new DateTime(1997, 1, 1)).ExecuteAffrows();
        }

       

      
        
        public SystemData GetSystemDataOrCreate(long id,out bool isNew)
        {
            var r = m_fsql.Select<SystemData>().Where((a) => a.Id == id).ToOne();
            isNew = r == null;
            if (isNew)
                r = new SystemData()
                {
                    Id = id,
                    StrValue = "",
                    IntValue = 0,
                    LongValue = 0,
                    DateTimeValue = new DateTime(1990, 1, 1)
                };
            return r;
        }
        public bool SetSysValue<T>(long id,T v,out bool isNew)
        {
            var d = GetSystemDataOrCreate(id, out isNew);
            if (v is int iv)
            {
                d.IntValue = iv;
                if(!isNew)
                    return m_fsql.Update<SystemData>(id).Set(a => a.IntValue, iv).ExecuteAffrows() == 1;
            }
            else if (v is long lv)
            {
                d.LongValue = lv;
                if(!isNew)
                    return m_fsql.Update<SystemData>(id).Set(a => a.LongValue, lv).ExecuteAffrows() == 1;
            }
            else if (v is string sv)
            {
                d.StrValue = sv;
                if(!isNew)
                    return m_fsql.Update<SystemData>(id).Set(a => a.StrValue, sv).ExecuteAffrows() == 1;
            }
            else if (v is DateTime dt)
            {
                d.DateTimeValue = dt;
                if(!isNew)
                    return m_fsql.Update<SystemData>(id).Set(a => a.DateTimeValue, dt).ExecuteAffrows() == 1;
            }
            else
                return false;
            return m_fsql.Insert(d).ExecuteAffrows() == 1;
        }
        public List<T> GetListForSys<T>(long id)
        {
            var r = m_fsql.Select<SystemData>().Where((a) => a.Id == id).ToOne();
            if (r == null || string.IsNullOrEmpty(r.StrValue))
                return null;
            try
            {
                var ls = Newtonsoft.Json.JsonConvert.DeserializeObject<List<T>>(r.StrValue);
                return ls;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public bool SetListForSys<T>(long id, List<T> ls)
        {
            string str = null;
            try
            {
                str = Newtonsoft.Json.JsonConvert.SerializeObject(ls);
            }
            catch (Exception ex)
            {
                return false;
            }
            return SetSysValue(id, str, out _);
        }

    }
}