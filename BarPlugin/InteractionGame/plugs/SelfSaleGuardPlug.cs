using BililiveDebugPlugin.DB;
using BililiveDebugPlugin.InteractionGame.Data;
using InteractionGame;
using InteractionGame.Context;
using InteractionGame.plugs.config;
using Utils;
using Msg = BilibiliDM_PluginFramework.DanmakuModel;

namespace BililiveDebugPlugin.InteractionGame.plugs
{
    public class SelfSaleGuardPlug : IPlug<EGameAction>, IPlayerPreJoinObserver
    {
        public static readonly string[] GuardLevelName = new string[]{ "","",Aoe4DataConfig.TiDu,Aoe4DataConfig.JianZhang };
        private IConstConfig _config;
        public override void Start()
        {
            base.Start();
            _config = Locator.Instance.Get<IConstConfig>();
            Locator.Instance.Get<IContext>().GetPlayerParser().AddPreJoinObserver(this);
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