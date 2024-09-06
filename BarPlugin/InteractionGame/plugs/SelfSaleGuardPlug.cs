using BililiveDebugPlugin.DB;
using InteractionGame;
using InteractionGame.Context;
using InteractionGame.plugs.config;
using Utils;
using Msg = BilibiliDM_PluginFramework.DanmakuModel;

namespace BililiveDebugPlugin.InteractionGame.plugs
{
    public class SelfSaleGuardPlug : IPlug<EGameAction>, IPlayerPreJoinObserver
    {
        private IConstConfig _config;
        public override void Start()
        {
            base.Start();
            _config = Locator.Get<IConstConfig>();
            Locator.Get<IContext>().GetPlayerParser().AddPreJoinObserver(this);
        }

        public override void Tick()
        {
            
        }

        public override void Notify(EGameAction m)
        {
            
        }

        public Msg OnPreJoin(Msg m)
        {
            foreach(var i in _config.GuardLevelListSorted)
            {
                var it = DBMgr.Instance.GetItem(m.OpenID, i.Value,i.Key);
                if(it != null)
                {
                    m.UserGuardLevel = m.GuardLevel = it.Count;
                    break;
                }
            }
            return m;
        }
    }
}