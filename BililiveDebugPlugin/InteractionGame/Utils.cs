using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InteractionGame
{
    public class Utils
    {
        public static int CharToInt(char c)
        {
            int sub = c >= '0' && c <= '9' ? '0' : c >= 'a' && c <= 'z' ? ('a' - 10) : 0;
            return sub > 0 ? c - sub : -1;
        }
        private static Dictionary<int, int> TmpDict = new Dictionary<int, int>();
        public static Dictionary<int, int> StringToDict(string s)
        {
            TmpDict.Clear();
            for (int i = 0; i < s.Length; i++)
            {
                var v = CharToInt(s[i]);
                if (v < 0) continue;
                if (TmpDict.ContainsKey(v))
                    TmpDict[v] = TmpDict[v] + 1;
                else
                    TmpDict[v] = 1;
            }
            return TmpDict;
        }

        public static void StringToDictAndForeach(string s, Action<KeyValuePair<int, int>> f)
        {
            lock (TmpDict)
            {
                TmpDict.Clear();
                for (int i = 0; i < s.Length; i++)
                {
                    var v = CharToInt(s[i]);
                    if (v < 0) continue;
                    if (TmpDict.ContainsKey(v))
                        TmpDict[v] = TmpDict[v] + 1;
                    else
                        TmpDict[v] = 1;
                }
                foreach (var v in TmpDict)
                {
                    f?.Invoke(v);
                }
            }
        }
    }
}

