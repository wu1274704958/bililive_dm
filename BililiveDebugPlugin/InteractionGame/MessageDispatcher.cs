
using ConsoleHotKey;
using Interaction;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BilibiliDM_PluginFramework;
using BililiveDebugPlugin.InteractionGame;

namespace InteractionGame
{
    using Msg = DanmakuModel;
    using MsgType = MsgTypeEnum;
    public class DyMsgOrigin 
    {
        public MsgType barType;
        public Msg msg;
        public DyMsgOrigin(Msg m, MsgType barType)
        {
            this.barType = barType;
            this.msg = m;
        }
     
    }

    public interface IContext
    {
        void Log(string text);
        void OnInit(Action<DyMsg> appendMsgAction);
        void OnStop();
        void OnTick();
        void OnAppendMsg(DyMsg msg);
        Aoe4StateData CheckState(EAoe4State state);
        void AppendMsg(DyMsg msg);
        void AppendMsg(DyMsg msg,float delay);
        void PrintGameMsg(string text);
    }

    public interface ILocalMsgDispatcher<IT>
        where IT:class,IContext
    {
        void Start();
        bool Demand(Msg msg, MsgType barType);
        void Dispatch(Msg msg, MsgType barType);
        void Stop();
        void Init(IT it);
        IAoe4Bridge<IT> GetBridge();
        IDyMsgParser<IT> GetMsgParser();
        IDyPlayerParser<IT> GetPlayerParser();
    }

    public class MessageDispatcher<PP,MP,B,IT> : ILocalMsgDispatcher<IT>
        where PP : IDyPlayerParser<IT>,new()
        where MP : IDyMsgParser<IT>,new()
        where B : IAoe4Bridge<IT>,new()
        where IT:class,IContext
    {
        private Int32 IsRunning = 0;
        private Int32 IsEmit = 0;
        private Thread thread;
        private ConcurrentQueue<DyMsg> queue;
        private int MY_HOTKEY_ID_S = 1;
        private int MY_HOTKEY_ID_E = 2;
        private int MY_HOTKEY_ID_R = 3;
        PP pp = new PP();
        MP mp = new MP();
        B bridge = new B();
        IT InitCtx;

        public PP PlayerParser => pp;
        public MP MsgParser => mp;
        public B Aoe4Bridge => bridge;
        public MessageDispatcher()
        {
            queue = new ConcurrentQueue<DyMsg>();

        }

        public void Start()
        {
            if (IsRunning == 1) return;
            IsRunning = 1;
            IsEmit = 0;
            SetupGlobalKeyMapListener();
            thread = new Thread(ThreadBody);
            thread.Start();
        }

        private void SetupGlobalKeyMapListener()
        {
            InitCtx.Log("注册全局快捷键");
            MY_HOTKEY_ID_S = HotKeyManager.RegisterHotKey(Keys.PageUp, KeyModifiers.Alt | KeyModifiers.Control);
            MY_HOTKEY_ID_E = HotKeyManager.RegisterHotKey(Keys.PageDown, KeyModifiers.Alt | KeyModifiers.Control);
            MY_HOTKEY_ID_R = HotKeyManager.RegisterHotKey(Keys.R, KeyModifiers.Alt | KeyModifiers.Control);
            HotKeyManager.HotKeyPressed += new EventHandler<HotKeyEventArgs>(HotKeyManager_HotKeyPressed);
        }

        private void HotKeyManager_HotKeyPressed(object sender, HotKeyEventArgs e)
        {
            if((e.Modifiers & KeyModifiers.Alt) == KeyModifiers.Alt && (e.Modifiers & KeyModifiers.Control) == KeyModifiers.Control)
            {
                switch(e.Key)
                {
                    case Keys.PageUp:
                        Interlocked.Exchange(ref IsEmit, IsEmit == 0 ? 1 : 0);
                        InitCtx.Log($"IsEmit = {IsEmit}");
                        break;
                    case Keys.PageDown:
                        Interlocked.Exchange(ref IsRunning, 0);
                        break;
                    case Keys.R:
                        while(!queue.IsEmpty)
                            queue.TryDequeue(out _);
                        break;
                }
            }
        }

        public void AppendMsg(DyMsg msg)
        {
            queue.Enqueue(msg);
        }
        public void Stop()
        {
            Interlocked.Exchange(ref IsRunning, 0);
            thread.Join();
            bridge.Stop();
            pp.Stop();
            mp.Stop();
            InitCtx.OnStop();
        }
       
        private void ThreadBody()
        {
            while (IsRunning == 1)
            {
                if (IsEmit == 0) continue;
                if (queue.TryDequeue(out var msg))
                {
                    bridge.SendMsg(msg);
                    Thread.Sleep(5);
                }
                else
                {
                    Thread.Sleep(100);
                }
                InitCtx.OnTick();
                bridge.OnTick();
            }
            InitCtx.Log("取消注册全局快捷键");
            HotKeyManager.UnregisterHotKey(MY_HOTKEY_ID_E);
            HotKeyManager.UnregisterHotKey(MY_HOTKEY_ID_S);
            HotKeyManager.UnregisterHotKey(MY_HOTKEY_ID_R);

        }

        public bool Demand(Msg msg, MsgType barType)
        {
            return pp.Demand(msg, barType) || mp.Demand(msg,barType);
        }

        public void Dispatch(Msg msg, MsgType barType)
        {
            var omsg = new DyMsgOrigin(msg, barType);
            int p = -1;
            if(pp.Demand(msg,barType))
                p = pp.Parse(omsg);
            InitCtx.Log($"player = {p}");
            if (p < 0) return;
            if (mp.Demand(msg, barType))
            {
                var (m, count) = mp.Parse(omsg);
                InitCtx.Log($"msg = {m} count = {count}");
                for (int i = 0; i < count; i++)
                {
                    DyMsg dyMsg = new DyMsg()
                    {
                        Player = p,
                        Msg = m
                    };
                    AppendMsg(dyMsg);
                    InitCtx.OnAppendMsg(dyMsg);
                }
            }
        }

        public void Init(IT it)
        {
            InitCtx = it;
            pp.Init(it,this);
            mp.Init(it,this);
            bridge.Init(it,this);
            InitCtx.OnInit(AppendMsg);
        }

        public IAoe4Bridge<IT> GetBridge()
        {
            return bridge;
        }

        public IDyMsgParser<IT> GetMsgParser()
        {
            return mp;
        }

        public IDyPlayerParser<IT> GetPlayerParser()
        {
           return pp;
        }
    }
}
