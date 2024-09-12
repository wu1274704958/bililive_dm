using NCalc;
using NCalc.Domain;
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

        public static T EvaluateExpr<T>(this string self,params object[] args)
        {
            var expr = new Expression(self);
            expr.EvaluateFunction += (name, param) =>
            {
                if (name == "Get")
                {
                    if(param != null && param.Parameters.Length == 2 )
                    {
                        var p1 = param.Parameters[0].Evaluate();
                        var p2 = param.Parameters[1].Evaluate().ToString();
                        if (p1 != null)
                        {
                            var ty = p1.GetType();
                            var prop = ty.GetProperty(p2);
                            if (prop != null)
                            {
                                var res = prop.GetValue(p1, null);
                                param.Result = res;
                                return;
                            }
                            var field = ty.GetField(p2);
                            if (field != null)
                            {
                                var res = field.GetValue(p1);
                                param.Result = res;
                                return;
                            }
                        }
                    }
                }
            };
            if (args != null && args.Length > 0)
            {
                for(int i = 0;i < args.Length;++i)
                    expr.Parameters[$"v{i + 1}"] = args[i];
            }
            return (T)expr.Evaluate();
        }
    }
}
