using System;
using System.Collections.Generic;
using BilibiliDM_PluginFramework;

using BililiveDebugPlugin.InteractionGameUtils;
using InteractionGame;
using InteractionGame.plugs;
using Utils;

namespace BililiveDebugPlugin.InteractionGame.Parser
{
    using MsgType = MsgTypeEnum;
    public class SignInSubMsgParser : ISubMsgParser
    {

        private IDyMsgParser m_Owner;
        private Random rand;
        private IActivityMgr activity;

        public void Init(IDyMsgParser owner)
        {
            m_Owner = owner;
            rand = new Random((int)DateTime.Now.Ticks);
            activity = Locator.Get<IActivityMgr>();
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
                        activity.ApplyActivity(conf.Activity.EItemType.SignIn,u);
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