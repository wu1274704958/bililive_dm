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
        }

        public void OnStop()
        {
            m_AppendMsgAction = null;
        }

        public void OnTick()
        {
            if (AutoAppendMsgOnIdle)
            {
                var span = DateTime.Now - m_AutoAppendMsgTime;
                if (span.TotalSeconds >= 20)
                {
                    AppendRandomMsg(0, 0, 7, 1, 20);
                    AppendRandomMsg(1, 0, 7, 1, 20);
                    m_AutoAppendMsgTime = DateTime.Now;
                }
            }
        }

        private void AppendRandomMsg(int p, int sid_s, int sid_e, int num_s, int num_e)
        {
            var num = new Random((int)DateTime.Now.Ticks).Next(num_s, num_e);
            for(int i = 0;i < num;i++)
            {
                m_AppendMsgAction?.Invoke(new DyMsg(){ Player = p, Msg = new Random((int)DateTime.Now.Ticks).Next(sid_s,sid_e + 1) });
            }
        }

        public void OnAppendMsg(DyMsg msg)
        {
            m_AutoAppendMsgTime = DateTime.Now;
        }
    }
}