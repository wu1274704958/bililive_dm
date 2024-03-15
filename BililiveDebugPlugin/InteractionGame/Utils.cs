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
    public class Utils
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

        public static ushort Merge(byte hD, byte lHP)
        {
            return (ushort)(hD << 8 | lHP);
        }
        public static int Merge(ushort h, ushort l)
        {
            return h << 16 | l;
        }

        public static int GetFansLevel(DyMsgOrigin msgOrigin)
        {
            if(msgOrigin.msg.FansMedalName != null && msgOrigin.msg.FansMedalName.Equals("回回炮"))
                return msgOrigin.msg.FansMedalLevel;
            return 0;
        }
        
        public static int GetNewYearActivity()
        {
            var now = DateTime.Now;
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
    }
}

