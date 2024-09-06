using BilibiliDM_PluginFramework;
using conf.Squad;
using InteractionGame.Context;
using InteractionGame.plugs.config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using Utils;

namespace InteractionGame.Parser.bar
{
    using Msg = DanmakuModel;
    using MsgType = MsgTypeEnum;
    public class JoinGameData
    {
        public string Id;
        public int Group;
        public string Name;
        public int Type;
    }
    public class PlayerMsgParser : IDyPlayerParser
    {

        private IConstConfig _config;
        public override bool Demand(DanmakuModel msg, MsgTypeEnum barType)
        {
            return StaticMsgDemand.Demand(msg, barType);
        }

        public override void Init(IContext it)
        {
            base.Init(it);
            _config = Locator.Get<IConstConfig>();
        }

        public override int Parse(DyMsgOrigin msgOrigin)
        {
            if (msgOrigin == null) return 0;
            if (msgOrigin.msg.CommentText == null)
                msgOrigin.msg.CommentText = "";
            // 系统选择阵营
            var uid = msgOrigin.msg.OpenID;
            if (uid == null)
            {
                InitCtx.Log($"uid == null raw = {msgOrigin.msg.RawData}");
                return -1;
            }
            var con = msgOrigin.msg.CommentText.Trim();
            var uName = msgOrigin.msg.UserName;
            int g = -1;
            if (!HasGroup(uid))
            {
                g = ParseJoinGroup(uid, con, msgOrigin);
                if (g != -1)
                {
                    InitCtx.PrintGameMsg($"{SettingMgr.GetColorWrap(uName, g)}加入{_config.GetGroupName(g + 1)}方");
                }
                else
                {
                    InitCtx.PrintGameMsg($"{uName}请先发“j”，加入游戏");
                }
                return -1;
            }
            else
            {
                g = GetGroupById(uid);
                if (TryParseChangTarget(uid, con, uName, false))
                    return -1;
            }

            return g;
        }

        protected override int ParseJoinGroup(string uid, string con, DyMsgOrigin msgOrigin)
        {
            if (con.StartsWith("j") || (msgOrigin.barType == MsgType.GiftSend ||
                (msgOrigin.barType == MsgType.Interact && msgOrigin.msg.InteractType == InteractTypeEnum.Like) ||
                msgOrigin.barType == MsgType.GuardBuy))
            {
                lock (this)
                {
                    var g = ChooseGroupSystem(uid, msgOrigin);
                    if(con != null && g > -1)
                    {
                        int op = 0;
                        if (con.Length > 1 && int.TryParse(con.Substring(1), out op)) ;
                        if (op >= 10)
                            op = 0;
                        op = _config.GetPureGuardLevel(msgOrigin.msg.UserGuardLevel) * 10 + op;
                        InitCtx.SendMsgToGame<JoinGameData>(EGameMsg.SJoin, new JoinGameData()
                        {
                            Group = g, Name = msgOrigin.msg.UserName,Type = op,Id = uid
                        });
                    }
                    return g;
                }
            }
            return -1;
        }
    }
}
