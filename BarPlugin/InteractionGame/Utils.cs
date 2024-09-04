using BililiveDebugPlugin.DB;
using conf.Squad;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Utils;

namespace InteractionGame
{
    public class Utility
    {
        public static int CharToInt(char c)
        {
            return (int)c;
            // int sub = c >= '0' && c <= '9' ? '0' : c >= 'a' && c <= 'z' ? ('a' - 10) : 0;
            // return sub > 0 ? c - sub : -1;
        }
        

        public static void StringToDictAndForeach(string s, Action<KeyValuePair<int, int>> f)
        {
            var dict = ObjPoolMgr.Instance.Get<Dictionary<int, int>>(null,DefObjectRecycle.OnDictRecycle).Get();
            for (int i = 0; i < s.Length; i++)
            {
                var v = CharToInt(s[i]);
                if (v < 0) continue;
                if (dict.ContainsKey(v))
                    dict[v] = dict[v] + 1;
                else
                    dict[v] = 1;
            }
            foreach (var v in dict)
            {
                f?.Invoke(v);
            }
            ObjPoolMgr.Instance.Get<Dictionary<int, int>>(null, DefObjectRecycle.OnDictRecycle).Return(dict);
        }

        public static ushort MergeByte(byte hD, byte lHP)
        {
            return (ushort)(hD << 8 | lHP);
        }

        public static ushort AttrMult(ushort attr,int multiple)
        {
            var height = attr >> 8;
            var low = attr & 0xFF;
            if (height > 0)
                height *= multiple;
            else
                height += (multiple - 1);
            if(low > 0)
                low *= multiple;
            else
                low += (multiple - 1);
            return MergeByte((byte)height, (byte)low);
        }
        public static int MergeShort(ushort h, ushort l)
        {
            return h << 16 | l;
        }

        public static int GetFansLevel(DyMsgOrigin msgOrigin)
        {
            var fans = "回回炮";
            var d = SettingMgr.GetInstance().Get(6);
            if (d != null && d.Country != null && d.Country.Count >= 1)
                fans = d.Country[0];
            if(msgOrigin.msg.FansMedalName != null && msgOrigin.msg.FansMedalName.Equals(fans))
                return msgOrigin.msg.FansMedalLevel;
            return 0;
        }
        
        public static int GetNewYearActivity()
        {
            var now = DateTime.Now;
            if (now.Month == 8 && (now.Day == 13 || now.Day == 14))
                return 2;
            if(now.DayOfYear == 1)
                return 1; // 4
            var chineseDate = new ChineseLunisolarCalendar();
            var doy = chineseDate.GetDayOfYear(now);
            if(doy == 15)
                return 1;
            if (doy < 4)
                return doy;// 1 2 3
            return 0;
        }

        public static int AddByte(int op,byte v,int pos)
        {
            var src = (op >> pos) & 255;
            src += v;
            var res = op | (255 << pos);
            res &= ((src & 255) << pos);
            return res;
        }

        public static long TryTransfarUserData(string name,string openid)
        {
            var ud = DBMgr2.Instance.GetUserByName(name);
            if(ud == null || ud.Ext == 1) return 0;
            var newUd = DBMgr.Instance.GetUser(openid);
            if (newUd != null)
            {
                DBMgr.Instance.AddHonor(openid,ud.Honor);
                DBMgr2.Instance.Fsql.Update<BililiveDebugPlugin.DB.Model2.UserData>(ud.Id)
                    .Set(a => a.Ext, 1).ExecuteAffrows();
                return ud.Id;
            }
            var ud2 = new BililiveDebugPlugin.DB.Model.UserData()
            {
                Id = openid,
                Name = name,
                Icon = ud.Icon,
                Score = ud.Score,
                Honor = ud.Honor,
                WinTimes = ud.WinTimes,
                SpawnSoldierNum = ud.SpawnSoldierNum,
                UserType = ud.UserType,
                Ext = ud.Ext,
                SignTime = ud.SignTime,
                UpdateTime = ud.UpdateTime,
            };
            DBMgr.Instance.Fsql.Insert(ud2).ExecuteAffrows();
            DBMgr2.Instance.Fsql.Update<BililiveDebugPlugin.DB.Model2.UserData>(ud.Id)
                .Set(a => a.Ext, 1).ExecuteAffrows();
            return ud.Id;
        }

        public static int TryTransfarItems(long id,string openid)
        {
            var ls = DBMgr2.Instance.GetUserItems(id,999999);
            int c = 0;
            if(ls == null || ls.Count == 0) return 0;
            foreach(var item in ls)
            {
                var it = new BililiveDebugPlugin.DB.Model.ItemData()
                {
                    Id = item.Id,
                    OwnerId = openid,
                    Name = item.Name,
                    Count = item.Count,
                    Type = item.Type,
                    Price = item.Price,
                    Ext = item.Ext,
                };
                c += DBMgr.Instance.Fsql.Insert(it).ExecuteAffrows();
            }
            return c;
        }
    }
}

