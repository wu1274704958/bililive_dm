using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteractionGame.Parser
{
    public interface IGlobalConfig
    {
        bool GetConfig<T>(string id, out T v);
    }
}
