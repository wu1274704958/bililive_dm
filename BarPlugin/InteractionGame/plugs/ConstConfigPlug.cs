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
        public override void Notify(EGameAction m)
        {

        }

        public override void Tick()
        {

        }

        public override void Init()
        {
            base.Init();
            Locator.Instance.Deposit<IConstConfig>(new T());
        }

        public override void Dispose()
        {
            Locator.Instance.Remove<IConstConfig>();
            base.Dispose();
        }
    }
}
