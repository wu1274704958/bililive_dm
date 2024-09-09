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
                        var p1 = param.Parameters[0].ParsedExpression.ToString();
                        var p2 = param.Parameters[1].ParsedExpression.ToString();
                        if (int.TryParse(p1.Substring(2,p1.Length - 3),out var idx) && idx < args.Length)
                        {
                            p2 = p2.Substring(1, p2.Length - 2);
                            var ty = args[idx - 1].GetType();
                            var prop = ty.GetProperty(p2);
                            if (prop != null)
                            {
                                var res = prop.GetValue(args[idx - 1], null);
                                param.Result = res;
                                return;
                            }
                            var field = ty.GetField(p2);
                            if (field != null)
                            {
                                var res = field.GetValue(args[idx - 1]);
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
