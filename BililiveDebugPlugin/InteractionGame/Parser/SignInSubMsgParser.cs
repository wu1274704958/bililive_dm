using System;
using System.Collections.Generic;
using BilibiliDM_PluginFramework;
using BililiveDebugPlugin.InteractionGame.Data;
using BililiveDebugPlugin.InteractionGameUtils;
using InteractionGame;

namespace BililiveDebugPlugin.InteractionGame.Parser
{
    using MsgType = MsgTypeEnum;
    public class SignInSubMsgParser<IT> : ISubMsgParser<IDyMsgParser<IT>, IT>
        where IT : class,IContext 
    {
        
        private IDyMsgParser<IT> m_Owner;

        public void Init(IDyMsgParser<IT> owner)
        {
            m_Owner = owner;
        }
        
        private struct ActivityData
        {
            public int GiftProbability;
            public int AddGiftCount;
            public int HonorMultiplier;
            public List<(string,int)> SpecialGift;

            public ActivityData(int giftProbability, int addGiftCount, int honorMultiplier, List<(string,int)> specialGift)
            {
                GiftProbability = giftProbability;
                AddGiftCount = addGiftCount;
                HonorMultiplier = honorMultiplier;
                SpecialGift = specialGift;
            }
        }
        
        private static readonly List<(string,int)> SpecialGifts24 = new List<(string,int)>()
        {
            (Aoe4DataConfig.QingShu, 4),(Aoe4DataConfig.Gaobai,5),(Aoe4DataConfig.GanBao,5),(Aoe4DataConfig.Xinghe,4),(Aoe4DataConfig.NiuWa,100),
            (Aoe4DataConfig.DaCall,40),(Aoe4DataConfig.XiaoFuDie,40),(Aoe4DataConfig.KuaKua,3),(Aoe4DataConfig.ShuiJingBall,2)
        };
        private static readonly List<(string,int)> SpecialGifts34 = new List<(string,int)>()
        {
            (Aoe4DataConfig.QingShu, 3),(Aoe4DataConfig.Gaobai,2),(Aoe4DataConfig.GanBao,3),(Aoe4DataConfig.Xinghe,1),(Aoe4DataConfig.NiuWa,20),
            (Aoe4DataConfig.DaCall,20),(Aoe4DataConfig.XiaoFuDie,20),(Aoe4DataConfig.KuaKua,2),(Aoe4DataConfig.ZheGe,15),(Aoe4DataConfig.BbTang,26),
            (Aoe4DataConfig.ShuiJingBall,1)
        };

        private static readonly Dictionary<int, ActivityData> ActivityDatas =
            new Dictionary<int, ActivityData>
            {
                {1, new ActivityData(1, 3, 4, null)},
                {2, new ActivityData(1, 2, 3, null)},
                {3, new ActivityData(3, 1, 2, null)},
                
                {21, new ActivityData(1, 8, 6, SpecialGifts24)},
                {31, new ActivityData(1, 3, 4, SpecialGifts34)},
                
                {22, new ActivityData(1, 6, 5, SpecialGifts24)},
                {32, new ActivityData(1, 2, 3, SpecialGifts34)},
                
                {23, new ActivityData(2, 4, 4, SpecialGifts24)},
                {33, new ActivityData(2, 1, 2, SpecialGifts34)},
            };
        private ActivityData GetNewYearActivityData(int uGuardLevel, int getNewYearActivity)
        {
            var key = uGuardLevel * 10 + getNewYearActivity;
            if(ActivityDatas.TryGetValue(key, out var activityData))
                return activityData;
            return new ActivityData(0, 0, 1, null);
        }
        public bool Parse(DyMsgOrigin msg)
        {
            if(msg.barType == MsgType.Comment)
            {
                if (msg.msg.CommentText.Trim().StartsWith("签"))
                {
                    var u = m_Owner.m_MsgDispatcher.GetMsgParser().GetUserData(msg.msg.UserID_long);
                    if (DB.DBMgr.Instance.SignIn(u) || DB.DBMgr.Instance.DepleteItem(msg.msg.UserID_long,Aoe4DataConfig.SignTicket,1,out _) > 0)
                    {
                        var now = DateTime.Now;
                        var r = new Random((int)now.Ticks);
                        ActivityData activityData = GetNewYearActivityData(u.GuardLevel,global::InteractionGame.Utils.GetNewYearActivity());
                        var probability = 160 - (u.GuardLevel > 0 ? (4 - u.GuardLevel) * 30 : 0) - u.FansLevel;
                        if(activityData.GiftProbability > 0) probability = activityData.GiftProbability;
                        if(probability <= 20) probability = 1;
                        var giftCount = new Random((int)now.Ticks).Next(1,1 + activityData.AddGiftCount);
                        if (r.Next(0, probability) == 0)
                        {
                            if (activityData.SpecialGift != null)
                            {
                                var l = r.Next(0, activityData.SpecialGift.Count);
                                var rr = activityData.SpecialGift[l];
                                if (Aoe4DataConfig.ItemDatas.TryGetValue(rr.Item1, out var it))
                                {
                                    giftCount *= rr.Item2;
                                    if (DB.DBMgr.Instance.AddGiftItem(u, it.Name, giftCount) > 0)
                                    {
                                        //m_Owner.InitCtx.PrintGameMsg($"恭喜{u.Name}签到成功获得{it.Name}*{giftCount}");
                                        LargeTips.Show(LargePopTipsDataBuilder.Create($"恭喜{u.Name}", "签到成功")
                                        .SetBottom($"获得{it.Name}*{giftCount}").SetLeftColor(LargeTips.GetGroupColor(u.Group)).SetRightColor(LargeTips.Yellow).SetBottomColor(LargeTips.Cyan).SetShowTime(3.6f));
                                    }
                                }
                            }
                            else
                            {
                                var l = r.Next(0, Aoe4DataConfig.ItemDatas.Count);
                                foreach (var it in Aoe4DataConfig.ItemDatas)
                                {
                                    if (l == 0)
                                    {
                                        var mult = it.Value.Price < 50 ? 5 : 1;
                                        giftCount *= mult;  
                                        if(it.Key == Aoe4DataConfig.SignTicket)
                                            giftCount = 1;
                                        if(DB.DBMgr.Instance.AddGiftItem(u, it.Key, giftCount) > 0)
                                            m_Owner.InitCtx.PrintGameMsg($"恭喜{u.NameColored}签到成功获得{it.Value.Name}*{giftCount}");
                                        break;
                                    }
                                    --l;
                                }
                            }
                        }
                        else
                        {
                            var c = r.Next(10 + msg.msg.FansMedalLevel , 51 + (u.GuardLevel > 0 ? (4 - u.GuardLevel) * 100 : 0) + u.FansLevel * 2);
                            c *= activityData.HonorMultiplier;
                            if (DB.DBMgr.Instance.AddHonor(u, c) > 0)
                            {
                                if(c > 100)
                                    LargeTips.Show(LargePopTipsDataBuilder.Create($"恭喜{u.Name}", "签到成功")
                                        .SetBottom($"获得{c}功勋").SetLeftColor(LargeTips.GetGroupColor(u.Group)).SetRightColor(LargeTips.Yellow).SetBottomColor(LargeTips.Cyan).SetShowTime(3.6f));
                                else
                                    m_Owner.InitCtx.PrintGameMsg($"恭喜{u.NameColored}签到成功获得{c}功勋");
                            }
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