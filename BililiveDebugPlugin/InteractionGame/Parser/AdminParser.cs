using System;
using BilibiliDM_PluginFramework;
using BililiveDebugPlugin.DB.Model;
using BililiveDebugPlugin.InteractionGame.Data;
using InteractionGame;

namespace BililiveDebugPlugin.InteractionGame.Parser
{
    public class AdminParser<IT> : ISubMsgParser<IDyMsgParser<IT>, IT>
        where IT : class,IContext 
    {
        
        private IDyMsgParser<IT> m_Owner;

        public void Init(IDyMsgParser<IT> owner)
        {
            m_Owner = owner;
        }
        


        public bool Parse(DyMsgOrigin msg)
        {
            if(msg.barType == MsgTypeEnum.Comment && (DB.DBMgr.Instance.GetUser(msg.msg.UserID_long)?.UserType).GetValueOrDefault(EUserType.None) == EUserType.Admin)
            {
                switch (msg.msg.CommentText)
                {
                    case "[吃瓜]":
                        DB.DBMgr.Instance.AddHonor(msg.msg.UserID_long, 100);return true;
                    case "[dog]":
                        DB.DBMgr.Instance.AddGiftItem(msg.msg.UserID_long, Aoe4DataConfig.GanBao, 1);return true;
                    case "[手机]":
                        DB.DBMgr.Instance.AddGiftItem(msg.msg.UserID_long, Aoe4DataConfig.QingShu, 1);return true;
                    case "妙啊":
                        DB.DBMgr.Instance.AddGiftItem(msg.msg.UserID_long, Aoe4DataConfig.Gaobai, 1);return true;
                }
            }
            return false;
        }

        public void OnTick(float delat)                  
        {
            
        }

        public void Stop()
        {
            m_Owner = null;
        }

        public void OnClear()
        {
            
        }
        
    }
}