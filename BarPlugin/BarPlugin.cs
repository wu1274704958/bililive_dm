using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Threading;
using System.Windows.Threading;
using BilibiliDM_PluginFramework;
using BililiveDebugPlugin.InteractionGame;
using BililiveDebugPlugin.InteractionGame.Data;
using BililiveDebugPlugin.InteractionGame.Parser;
using BililiveDebugPlugin.InteractionGame.plugs;
using BililiveDebugPlugin.InteractionGame.Resource;
using BililiveDebugPlugin.InteractionGame.Settlement;
using BililiveDebugPlugin.InteractionGameUtils;
using conf;
using Interaction;
using InteractionGame;
using InteractionGame.Context;
using Newtonsoft.Json;
using ProtoBuf;
using Utils;

namespace BililiveDebugPlugin
{
    [ProtoContract]
    class GoldInfo
    {
        [ProtoMember(1)]
        public string Id;
        [ProtoMember(2)]
        public int Gold;
        [ProtoMember(3)]
        public float Progress;
    }
    [ProtoContract]
    class GoldInfoArr
    {
        [ProtoMember(1)]
        public List<GoldInfo> Items = new List<GoldInfo>();
    }
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