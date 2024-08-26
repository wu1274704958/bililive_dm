using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public static class Common
    {
        public static DateTime GetDateTimeBySec(int sec)
        {
            return MinDateTime + TimeSpan.FromSeconds(sec);
        }
        public static int ToSecond(this DateTime self)
        {
            return (int)(self - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
        }
        public readonly static DateTime MinDateTime = new DateTime(1970, 1, 1).ToLocalTime();
    }
}
