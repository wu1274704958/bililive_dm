using BilibiliDM_PluginFramework;
using InteractionGame;
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
            Locator.Instance.Get<IDyPlayerParser<DebugPlugin>>().AddPreJoinObserver(this);
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
            var id = global::InteractionGame.Utils.TryTransfarUserData(m.UserName, m.OpenID);
            _cxt.Log($"迁移{id}:{m.UserName}>>{m.OpenID}");
            if(id > 0)
            {
                int count = global::InteractionGame.Utils.TryTransfarItems(id, m.OpenID);
                _cxt.Log($"迁移物品{id}:{m.UserName}>>{m.OpenID}x{count}");
            }
            return m;
        }
    }
}
