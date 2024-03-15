using System;
using System.Collections.Generic;

namespace conf.Squad
{
    public partial class SettingMgr
    {
        public static IReadOnlyList<System.String> ContryList => GetInstance().Get(1).Country;
        public static IReadOnlyList<System.String> ColorList => GetInstance().Get(2).Country;
        public static System.String GetCountry(int g)
        {
            if(g >= 0 && g < ContryList.Count)
                return ContryList[g];
            throw new Exception("Can't find Country id = " + g);
            return null;
        }
        public static string GetColor(int g)
        {
            if (g >= 0 && g < ColorList.Count)
                return ColorList[g];
            return "red";
        }
        public static string GetColorWrap(string s,int g)
        {
            return $"<color={GetColor(g)}>{s}</color>";
        }
    }
}