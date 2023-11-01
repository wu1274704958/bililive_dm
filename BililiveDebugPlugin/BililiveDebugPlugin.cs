using System;
using System.Windows.Threading;
using BilibiliDM_PluginFramework;
using Interaction;
using InteractionGame;

namespace BililiveDebugPlugin
{
    public class DebugPlugin : DMPlugin ,IContext
    {
        private MessageDispatcher<InteractionGame.PlayerBirthdayParser<DebugPlugin>,
            InteractionGame.MsgGiftParser<DebugPlugin>,
            Interaction.DefAoe4Bridge, DebugPlugin> messageDispatcher;

        private Action<DyMsg> m_AppendMsgAction;
        private DateTime m_AutoAppendMsgTime;
        private Random m_Rand;
        public static readonly bool AutoAppendMsgOnIdle = true;
        public DebugPlugin()
        {
            ReceivedDanmaku += OnReceivedDanmaku;
            PluginAuth = "CopyLiu";
            PluginName = "開發員小工具";
            PluginCont = "copyliu@gmail.com";
            PluginVer = "v0.0.2";
            PluginDesc = "它看着很像F12";
        }


        private void OnReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
            if (messageDispatcher.Demand(e.Danmaku, e.Danmaku.MsgType))
                messageDispatcher.Dispatch(e.Danmaku, e.Danmaku.MsgType);
        }


        public override void Admin()
        {
            base.Admin();
            messageDispatcher = new MessageDispatcher<PlayerBirthdayParser<DebugPlugin>, MsgGiftParser<DebugPlugin>, Interaction.DefAoe4Bridge, DebugPlugin>();
            messageDispatcher.Init(this);
            messageDispatcher.Start();
            Log("Start ...");
        }

        public override void Start()
        {
            base.Start();
        }

        public override void Stop()
        {
            base.Stop();
            messageDispatcher.Stop();
            messageDispatcher = null;
        }

        public void OnInit(Action<DyMsg> appendMsgAction)
        {
            m_AppendMsgAction = appendMsgAction;
            m_AutoAppendMsgTime = DateTime.Now;
            m_Rand = new Random((int)DateTime.Now.Ticks);
        }

        public void OnStop()
        {
            m_AppendMsgAction = null;
        }

        public void OnTick()
        {
            //Log($"OnTick");
            if (AutoAppendMsgOnIdle)
            {
                var span = DateTime.Now - m_AutoAppendMsgTime;
                if (span.TotalSeconds >= 60)
                {
                    //Log($"AppendRandomMsg {m_AppendMsgAction != null}");
                    AppendRandomMsg(0, 0, 7, 1, 20);
                    AppendRandomMsg(1, 0, 7, 1, 20);
                    m_AutoAppendMsgTime = DateTime.Now;
                }
            }
        }

        private void AppendRandomMsg(int p, int sid_s, int sid_e, int num_s, int num_e)
        {
            var num = m_Rand.Next(num_s, num_e);
            //Log($"AppendRandomMsg num = {num}");
            for (int i = 0;i < num;i++)
            {
                var Msg = m_Rand.Next(sid_s, sid_e + 1);
                //Log($"AppendRandomMsg Msg = {Msg}");
                m_AppendMsgAction?.Invoke(new DyMsg(){ Player = p,  Msg = Msg});
            }
        }

        public void OnAppendMsg(DyMsg msg)
        {
            m_AutoAppendMsgTime = DateTime.Now;
        }
    }
}