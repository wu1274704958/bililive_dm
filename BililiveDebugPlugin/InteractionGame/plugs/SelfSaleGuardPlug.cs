using BililiveDebugPlugin.DB;
using BililiveDebugPlugin.InteractionGame.Data;
using InteractionGame;
using Utils;
using Msg = BilibiliDM_PluginFramework.DanmakuModel;

namespace BililiveDebugPlugin.InteractionGame.plugs
{
    public class SelfSaleGuardPlug : IPlug<EGameAction>, IPlayerPreJoinObserver
    {
        public static readonly string[] GuardLevelName = new string[]{ "","",Aoe4DataConfig.TiDu,Aoe4DataConfig.JianZhang };
        public override void Start()
        {
            base.Start();
            Locator.Instance.Get<IDyPlayerParser<DebugPlugin>>().AddPreJoinObserver(this);
        }

        public override void Tick()
        {
            
        }

        public override void Notify(EGameAction m)
        {
            
        }

        public Msg OnPreJoin(Msg m)
        {
            for (int i = 2; i <= 3; i++)
            {
                var it = DBMgr.Instance.GetItem(m.OpenID, GuardLevelName[i]);
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