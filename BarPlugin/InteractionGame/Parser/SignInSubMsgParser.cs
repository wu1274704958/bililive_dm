using System;
using System.Collections.Generic;
using BilibiliDM_PluginFramework;

using BililiveDebugPlugin.InteractionGameUtils;
using InteractionGame;

namespace BililiveDebugPlugin.InteractionGame.Parser
{
    using MsgType = MsgTypeEnum;
    public class SignInSubMsgParser : ISubMsgParser
    {

        private IDyMsgParser m_Owner;
        private Random rand;

        public void Init(IDyMsgParser owner)
        {
            m_Owner = owner;
            rand = new Random((int)DateTime.Now.Ticks);
        }

        public bool Parse(DyMsgOrigin msg)
        {
            if (msg.barType == MsgType.Comment)
            {
                if (msg.msg.CommentText.Trim().StartsWith("签"))
                {
                    var u = m_Owner.InitCtx.GetMsgParser().GetUserData(msg.msg.OpenID);
                    if (DB.DBMgr.Instance.SignIn(u) || DB.DBMgr.Instance.DepleteItem(msg.msg.OpenID, Aoe4DataConfig.SignTicket, 1, out _) > 0)
                    {

                        var c = rand.Next(130 + msg.msg.FansMedalLevel, 250 + (u.GuardLevel > 0 ? (4 - u.GuardLevel) * 50 : 0) + u.FansLevel * 2);
                        if (c > 0 && DB.DBMgr.Instance.AddHonor(u, c) > 0)
                        {
                            if (c > 100)
                                LargeTips.Show(LargePopTipsDataBuilder.Create($"恭喜{u.Name}", "签到成功")
                                    .SetBottom($"获得{c}功勋").SetLeftColor(LargeTips.GetGroupColor(u.Group)).SetRightColor(LargeTips.Yellow).SetBottomColor(LargeTips.Cyan).SetShowTime(3.6f));
                            else
                                m_Owner.InitCtx.PrintGameMsg($"恭喜{u.NameColored}签到成功获得{c}功勋");
                        }
                    }
                    else
                    {
                        m_Owner.InitCtx.PrintGameMsg($"{u.NameColored}今天已经签过到了，明天再来吧");
                    }
                    return true;
                }
            }
            return false;
        }

        public void OnTick(float delat)
        {

        }

        public void OnStartGame()
        {

        }

        public void Stop()
        {
            m_Owner = null;
        }

        public void OnClear()
        {

        }

        public void Start()
        {

        }
    }
}