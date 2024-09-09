using InteractionGame.Context;
using InteractionGame.plugs.config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace BarPlugin.InteractionGame.plugs
{
    public class ConstConfigPlug<T> : IPlug<EGameAction>
        where T : IConstConfig,new()
    {
        public override void OnReceiveNotify(EGameAction m,object args = null)
        {

        }

        public override void Tick()
        {

        }

        public override void Init()
        {
            base.Init();
            Locator.Deposit<IConstConfig>(new T());
        }

        public override void Dispose()
        {
            Locator.Remove<IConstConfig>();
            base.Dispose();
        }
    }
}
