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
            DefAoe4Bridge<DebugPlugin>, DebugPlugin> messageDispatcher;
        private MainPage mp;
        private Action<DyMsg> m_AppendMsgAction;
        private DateTime m_AutoAppendMsgTime;
        private Random m_Rand;
        private int LastState = 0;
        public static readonly bool AutoAppendMsgOnIdle = false;
        public static readonly int EndDelay = 7000;
        public static readonly Dictionary<string, int> ColorMapIndex = new Dictionary<string, int>
        {
            { "蓝",0 },
            { "红",1 },
            { "绿",2 },
            { "黄",3 },
        };
        public static readonly List<string> SquadNameMap = new List<string>
        {
            "长矛兵","长弓兵","中国武士","弩手","骑士","长剑武士","箭塔象","蜂窝炮","乌尔班巨炮"
        };
        private IGameStateObserver<EAoe4State, Aoe4StateData> m_GameState = new Aoe4GameState();
        private List<(Action, DateTime)> TaskList = new List<(Action, DateTime)>();
        public DebugPlugin()
        {
            ReceivedDanmaku += OnReceivedDanmaku;
            PluginAuth = "CopyLiu";
            PluginName = "開發員小工具";
            PluginCont = "copyliu@gmail.com";
            PluginVer = "v0.0.2";
            PluginDesc = "它看着很像F12";
        }
        public static string GetSquadName(int id)
        {
            var ls = SquadNameMap;
            string name = "";
            if (id >= 0 && id < ls.Count)
                name = ls[id];
            return name;
        }
        public Aoe4StateData CheckState(EAoe4State state)
        {
            return m_GameState.CheckState(state);
        }


        private void OnReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
            if (messageDispatcher.Demand(e.Danmaku, e.Danmaku.MsgType))
                messageDispatcher.Dispatch(e.Danmaku, e.Danmaku.MsgType);
        }


        public override void Admin()
        {
            base.Admin();
            messageDispatcher = new MessageDispatcher<PlayerBirthdayParser<DebugPlugin>, MsgGiftParser<DebugPlugin>,DefAoe4Bridge<DebugPlugin>, DebugPlugin>();
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
                if (span.TotalSeconds >= 90)
                {
                    AppendRandomMsg(0, 0, 7, 1, 20);
                    AppendRandomMsg(1, 0, 7, 1, 20);
                    m_AutoAppendMsgTime = DateTime.Now;
                }
            }

            var d = m_GameState.CheckState(EAoe4State.Default);
            if (LastState != 2 && d.r == 2)
            {
                PrintGameMsg($"{GetColorById(d.g)}方获胜！！！积分展示制作中，敬请期待");
                var data = messageDispatcher.MsgParser.GetSortedUserData();
                messageDispatcher.MsgParser.ClearUserData();
                //todo show settlement
                Thread.Sleep(EndDelay);
            }
            LastState = d.r;
            for(int i = TaskList.Count - 1;i >= 0;i--)
            {
                if (TaskList[i].Item2 >= DateTime.Now)
                {
                    TaskList[i].Item1.Invoke();
                    TaskList.RemoveAt(i);
                }
            }
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
                AppendMsg(new DyMsg(){ Player = p,  Msg = Msg});
            }
        }
        
        public void AppendMsg(DyMsg msg)
        {
            m_AppendMsgAction?.Invoke(msg);
        }
        public void AppendMsg(DyMsg msg, float delay)
        {
            TaskList.Add((() =>
            {
                m_AppendMsgAction?.Invoke(msg);
            }, DateTime.Now + TimeSpan.FromSeconds(delay)));
        }

        public void OnAppendMsg(DyMsg msg)
        {
            m_AutoAppendMsgTime = DateTime.Now;
        }

        public void PrintGameMsg(string text)
        {
            messageDispatcher.Aoe4Bridge.ExecPrintMsg(text);
        }
    }
}