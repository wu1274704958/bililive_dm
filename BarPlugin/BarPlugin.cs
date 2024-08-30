using System.Collections.Generic;
using BilibiliDM_PluginFramework;
using InteractionGame;
using InteractionGame.Context;
using ProtoBuf;
using Utils;

namespace BililiveDebugPlugin
{
    [ProtoBuf.ProtoContract]
    public class EventCueData
    {
        [ProtoBuf.ProtoMember(1)] 
        public string Msg;
    }
    public class BarPlugin : DMPlugin
    {
        private bool IsStart = false;
        private BarContext m_context;
        private MainPage mp;

        public BarPlugin()
        {
            PluginAuth = "Eqd";
            PluginName = "Bar";
            PluginCont = "1274704958@qq.com";
            PluginVer = "v0.0.2";
            PluginDesc = "它看着很像F12";
        }

        public override void Admin()
        {
            base.Admin();
            mp = new MainPage(Locator.Instance.Get<IContext>());
            mp.Show();
            //SendSettlement("蓝方获胜", new List<UserData>
            //{
            //    new UserData(){ Group = 1,Score = 6777 ,Icon = "22222",Name = "hahah2",Soldier_num = 12},
            //    new UserData(){ Group = 2,Score = 345 ,Icon = "767822",Name = "2123ds",Soldier_num = 92},
            //});
        }

        public override void Start()
        {
            base.Start();
            IsStart = true;
            m_context = new BarContext(this);
            m_context.OnInit();
            m_context.OnStart();
        }

        public override void Stop()
        {
            RealStop();
            base.Stop();
            IsStart = false;
        }

        private void RealStop()
        {
            if (!IsStart) 
                return;
            if(m_context != null)
            {
                m_context.OnStop();
                m_context.OnDestroy();
            }
            m_context = null;
        }

        public override void DeInit()
        {
            RealStop();
            base.DeInit();
        }
    }
}