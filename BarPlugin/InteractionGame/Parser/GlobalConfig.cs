using conf.CommonConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteractionGame.Parser
{
    public class GlobalConfig : IGlobalConfig
    {
        public bool GetConfig<T>(string id, out T v)
        {
            v = default(T);
            var c = CommonConfigMgr.GetInstance().Get(id);
            if (c == null)
                return false;
            var fields = c.GetType().GetFields();
            foreach( var field in fields )
            {
                var fieldValue = field.GetValue(c);
                if (fieldValue == null) continue;
                if(fieldValue.GetType() == typeof(T))
                {
                    v = (T)fieldValue;
                    return true;
                }
            }
            return false;
        }
    }
}
