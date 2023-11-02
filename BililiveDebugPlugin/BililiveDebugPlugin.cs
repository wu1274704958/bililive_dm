using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Lifetime;
using System.Threading;
using System.Windows.Threading;
using BilibiliDM_PluginFramework;
using BililiveDebugPlugin.InteractionGame;
using Interaction;
using InteractionGame;

namespace BililiveDebugPlugin
{
    public class DebugPlugin : DMPlugin ,IContext
    {
        private MessageDispatcher<
            PlayerBirthdayParser<DebugPlugin>,
            MsgGiftParser<DebugPlugin>,
            Interaction.DefAoe4Bridge, DebugPlugin> messageDispatcher;
        private MainPage mp;
        private Action<DyMsg> m_AppendMsgAction;
        private DateTime m_AutoAppendMsgTime;
        private Random m_Rand;
        private int LastState = 0;
        public static readonly bool AutoAppendMsgOnIdle = true;
        public static readonly int EndDelay = 7000;
        public static readonly Dictionary<string, int> ColorMapIndex = new Dictionary<string, int>
        {
            { "蓝",0 },
            { "红",1 },
            { "绿",2 },
            { "黄",3 },
        };
        private Aoe4GameState m_GameState = new Aoe4GameState();
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
            m_GameState.Init();
            Log("Start ...");
            //mp = new MainPage();
            //mp.Show();
        }

        public override void Start()
        {
            base.Start();
        }

        public override void Stop()
        {
            base.Stop();
            messageDispatcher.Stop();
            m_GameState.Stop();
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

            var d = m_GameState.CheckState(EAoe4State.Default);
            if (LastState != 2 && d.r == 2)
            {
                Log($"{GetColorById(d.g)}方获胜！！！");
                Thread.Sleep(EndDelay);
            }
            LastState = d.r;
        }

        public static string GetColorById(int id)
        {
            foreach(var v in ColorMapIndex)
            {
                if(v.Value + 1 ==  id)
                {
                    return v.Key;
                }
            }
            return "";
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