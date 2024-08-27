using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Interop;
using BilibiliDM_PluginFramework;
using BililiveDebugPlugin.InteractionGame.Data;
using BililiveDebugPlugin.InteractionGame.mode;
using BililiveDebugPlugin.InteractionGame.plugs;
using BililiveDebugPlugin.InteractionGameUtils;
using InteractionGame;
using Utils;

namespace BililiveDebugPlugin.InteractionGame.Parser
{
    public class BossGameModeParser : ISubMsgParser
    {
        private TickGroup _tickGroup = new TickGroup();
        private int _state = 0;
        private GameModeManager _gameModeMgr;
        private IContext _cxt;
        protected ConcurrentDictionary<string, int> SeatCountOfPlayer;
        protected static readonly Regex regex = new Regex("送(.+)");

        public void Init(IDyMsgParser owner)
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

        public bool TurnOnBoss(string id,string uName)
        {
            if (_state != 2)
            {
                if (SeatCountOfPlayer == null)
                    SeatCountOfPlayer = new ConcurrentDictionary<string, int>();
                SeatCountOfPlayer[id] = 7;
                _cxt.PrintGameMsg($"{uName}下一局是Boss");
                if (_state == 1)
                    return true;
                _gameModeMgr.SetNextGameMode<BossGameMode>(SeatCountOfPlayer);
                Interlocked.Exchange(ref _state, 1);
                LargeTips.Show(LargePopTipsDataBuilder.Create("下一局", "Boss战")
                    .SetLeftColor(LargeTips.Cyan)
                    .SetRightColor(LargeTips.Yellow)
                    .SetBottom("20秒钟后自动结束本局，期间其他人可以加入Boss阵营")
                    .SetBottomColor(LargeTips.Cyan));
                _tickGroup.Add(new DelayTask(OnForceNextBossFight, TimeSpan.FromSeconds(20)));
                return true;
            }
            return false;
        }

        public bool Parse(DyMsgOrigin msg)
        {
            var ud = Locator.Instance.Get<IContext>().GetMsgParser().GetUserData(msg.msg.OpenID);
            if (_state != 2 && (
                (msg.barType == MsgTypeEnum.GiftSend && IsStartBossGift(msg.msg.GiftName)) || 
                (ud != null && ud.GuardLevel == 2 && msg.msg.CommentText == "boss")
                ))
            {
                TurnOnBoss(msg.msg.OpenID,msg.msg.UserName);
                //return true;
            }
            Match match = null;
            if(_state != 2 && (msg.barType == MsgTypeEnum.Comment && (match = regex.Match(msg.msg.CommentText)) != null && IsStartBossGift(match.Groups[1].Value)) &&
                DB.DBMgr.Instance.GetItem(msg.msg.OpenID,match.Groups[1].Value).Count >= 1)
            {
                TurnOnBoss(msg.msg.OpenID, msg.msg.UserName);
                //return true;
            }
            return false;
        }

        private bool IsStartBossGift(string giftName)
        {
            switch (giftName)
            {
                case "电影票":
                case "棉花糖":
                case "BW蛋糕":
                case "BW权杖":
                case "时空之站":
                case "BW舞台":
                case "浪漫城堡":
                    return true;
            }
            return false;
        }

        private void OnForceNextBossFight()
        {
            Interlocked.Exchange(ref _state, 2);
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