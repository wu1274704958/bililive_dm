
using ConsoleHotKey;
using System;
using System.Threading;
using System.Windows.Forms;
using BilibiliDM_PluginFramework;
using InteractionGame.Resource;

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

    public class MessageDispatcher<PP,MP,B,R,IT> : ILocalMsgDispatcher<Msg,MsgType>
        where PP : IDyPlayerParser,new()
        where MP : IDyMsgParser,new()
        where B : IGameBridge, new()
        where R : IResourceMgr,new()
        where IT:class,IContext
    {
        private Int32 IsRunning = 0;
        private Int32 IsEmit = 0;
        private Thread thread;
        private int MY_HOTKEY_ID_S = 1;
        private int MY_HOTKEY_ID_E = 2;
        private int MY_HOTKEY_ID_R = 3;
        PP pp = new PP();
        MP mp = new MP();
        B bridge = new B();
        R resMgr = new R();
        IT InitCtx;

        public PP PlayerParser => pp;
        public MP MsgParser => mp;
        public B Aoe4Bridge => bridge;
        public MessageDispatcher()
        {

        }

        public void Start()
        {
            if (IsRunning == 1) return;
            IsRunning = 1;
            IsEmit = 0;
            SetupGlobalKeyMapListener();
            thread = new Thread(ThreadBody);
            thread.Start();

            mp.Start();
        }

        public void OnStartGame()
        {
            mp.OnStartGame();
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
                }
            }
        }

        public void Stop()
        {
            Interlocked.Exchange(ref IsRunning, 0);
            thread.Join();
            bridge.Stop();
            pp.Stop();
            mp.Stop();
            resMgr.Stop();
        }


       
        private void ThreadBody()
        {
            DateTime startTime = DateTime.Now;
            while (IsRunning == 1)
            {
                if (IsEmit == 0 && InitCtx.IsGameStart() != EGameState.Started) continue;
                //if (queue.TryDequeue(out var msg))
                //{
                //    bridge.SendMsg(msg);
                //    Thread.Sleep(5);
                //}
                //else
                {
                    Thread.Sleep(100);
                }
                var now = DateTime.Now;
                var sp = now - startTime;
                var sec = (float)sp.TotalSeconds;
                startTime = now;
                InitCtx.OnTick(sec);
                bridge.OnTick(sec);
                resMgr.OnTick(sec);
                mp.OnTick(sec);
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
            //InitCtx.Log($"player = {p}");
            if (p < 0) return;
            if (mp.Demand(msg, barType))
            {
                var (m, count) = mp.Parse(omsg);
                //InitCtx.Log($"msg = {m} count = {count}");
                //for (int i = 0; i < count; i++)
                //{
                //    DyMsg dyMsg = new DyMsg()
                //    {
                //        Player = p,
                //        Msg = m
                //    };
                //    AppendMsg(dyMsg);
                //    InitCtx.OnAppendMsg(dyMsg);
                //}
            }
        }

        public void Init(IContext it)
        {
            InitCtx = it as IT;
            bridge.Init(it);
            pp.Init(it);
            mp.Init(it);
            resMgr.Init(it);
        }

        public IGameBridge GetBridge()
        {
            return bridge;
        }

        public IDyMsgParser GetMsgParser()
        {
            return mp;
        }

        public IDyPlayerParser GetPlayerParser()
        {
           return pp;
        }

        public IResourceMgr GetResourceMgr()
        {
            return resMgr;
        }

        public void Clear()
        {
            resMgr.OnClear();
            bridge.OnClear();
            pp.OnClear();
            mp.Clear();
        }

        public void SetIsEmit(bool v)
        {
            Interlocked.Exchange(ref IsEmit, v ? 1 : 0);
        }
    }
}
