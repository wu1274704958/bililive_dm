using System;
using System.Collections.Concurrent;
using System.Threading;
using BilibiliDM_PluginFramework;
using BililiveDebugPlugin.InteractionGame.Data;
using BililiveDebugPlugin.InteractionGame.mode;
using BililiveDebugPlugin.InteractionGame.plugs;
using BililiveDebugPlugin.InteractionGameUtils;
using InteractionGame;
using Utils;

namespace BililiveDebugPlugin.InteractionGame.Parser
{
    public class BossGameModeParser<IT> : ISubMsgParser<IDyMsgParser<IT>, IT>
        where IT:class,IContext
    {
        private TickGroup _tickGroup = new TickGroup();
        private int _state = 0;
        private GameModeManager _gameModeMgr;
        private IContext _cxt;
        protected ConcurrentDictionary<string, int> SeatCountOfPlayer;

        public void Init(IDyMsgParser<IT> owner)
        {
            _tickGroup.Reset();
        }

        public void Start()
        {
            _gameModeMgr = Locator.Instance.Get<GameModeManager>();
            _cxt = Locator.Instance.Get<IContext>();
        }

        public void OnStartGame()
        {
            
        }

        public void Stop()
        {
            
        }

        public bool Parse(DyMsgOrigin msg)
        {
            var ud = Locator.Instance.Get<IDyMsgParser<DebugPlugin>>().GetUserData(msg.msg.OpenID);
            if (_state != 2 && (
                (msg.barType == MsgTypeEnum.GiftSend && IsStartBossGift(msg.msg.GiftName)) || 
                (ud != null && ud.GuardLevel == 2 && msg.msg.CommentText == "boss")
                ))
            {
                if(SeatCountOfPlayer == null)
                    SeatCountOfPlayer = new ConcurrentDictionary<string, int>();
                SeatCountOfPlayer[msg.msg.OpenID] = 7;
                _cxt.PrintGameMsg($"{msg.msg.UserName}下一局是Boss");
                if (_state == 1)
                {
                    return true;
                }
                _gameModeMgr.SetNextGameMode<BossGameMode>(SeatCountOfPlayer);
                Interlocked.Exchange(ref _state, 1);
                LargeTips.Show(LargePopTipsDataBuilder.Create("下一局","Boss战")
                    .SetLeftColor(LargeTips.Cyan)
                    .SetRightColor(LargeTips.Yellow)
                    .SetBottom("20秒钟后自动结束本局，期间其他人可以加入Boss阵营")
                    .SetBottomColor(LargeTips.Cyan));
                _tickGroup.Add(new DelayTask(OnForceNextBossFight,TimeSpan.FromSeconds(20)));
                return true;
            }

            return false;
        }

        private bool IsStartBossGift(string giftName)
        {
            switch (giftName)
            {
                case "电影票":
                case "棉花糖":
                case "樱花花环":
                case "花笺传情":
                case "樱花之恋":
                case "樱花列车":
                case "浪漫城堡":
                    return true;
            }
            return false;
        }

        private void OnForceNextBossFight()
        {
            Interlocked.Exchange(ref _state, 2);
            (_cxt as DebugPlugin).messageDispatcher.GetBridge().ForceFinish();
        }

        public void OnTick(float delat)
        {
            _tickGroup.update();
        }

        public void OnClear()
        {
            SeatCountOfPlayer = null;
            Interlocked.Exchange(ref _state, 0);
            _tickGroup.ClearEx();
        }
    }
}