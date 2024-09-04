using BilibiliDM_PluginFramework;
using InteractionGame;
using InteractionGame.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace BililiveDebugPlugin.InteractionGame.plugs
{
    internal class DbTransfarPlug : IPlug<EGameAction>,IPlayerPreJoinObserver 
    {
        private IContext _cxt;

        public override void Start()
        {
            base.Start();
            Locator.Instance.Get<IContext>().GetPlayerParser().AddPreJoinObserver(this);
            _cxt = Locator.Instance.Get<IContext>();
        }
        public override void Notify(EGameAction m)
        {

        }

        public override void Tick()
        {
            
        }

        public DanmakuModel OnPreJoin(DanmakuModel m)
        {
            var id = Utility.TryTransfarUserData(m.UserName, m.OpenID);
            _cxt.Log($"迁移{id}:{m.UserName}>>{m.OpenID}");
            if(id > 0)
            {
                int count = Utility.TryTransfarItems(id, m.OpenID);
                _cxt.Log($"迁移物品{id}:{m.UserName}>>{m.OpenID}x{count}");
            }
            return m;
        }
    }
}
