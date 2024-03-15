using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public static class Utils
    {
        public static DateTime GetDateTimeBySec(int sec)
        {
            return new DateTime(1970, 1, 1).ToLocalTime() + TimeSpan.FromSeconds(sec);
        }
        public static int ToSecond(this DateTime self)
        {
            return (int)(self - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
        }
    }
}
