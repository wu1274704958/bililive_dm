using System;
using System.Windows.Threading;
using BilibiliDM_PluginFramework;
using InteractionGame;

namespace BililiveDebugPlugin
{
    public class DebugPlugin : DMPlugin ,IContext
    {
        private MessageDispatcher<InteractionGame.PlayerBirthdayParser<DebugPlugin>,
            InteractionGame.MsgGiftParser<DebugPlugin>,
            Interaction.DefAoe4Bridge, DebugPlugin> messageDispatcher;

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
    }
}