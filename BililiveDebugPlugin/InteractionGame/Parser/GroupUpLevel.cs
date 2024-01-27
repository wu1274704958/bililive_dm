using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows.Documents;
using BililiveDebugPlugin.InteractionGame.Data;
using InteractionGame;
using ProtoBuf;

namespace BililiveDebugPlugin.InteractionGame.Parser
{
    [ProtoBuf.ProtoContract]
    class UpdateGroupLevel
    {
        [ProtoMember(1)]
        public int Group;
        [ProtoMember(2)]
        public int Level;
        [ProtoMember(3)]
        public string Desc;
        [ProtoMember(4)]
        public float Progress;
    }

    class LevelCxt
    {
        public int All;
        public int NowLevel;
        public int NowConf;
    }

    class LevelConfig
    {
        public List<string> Technology;
        public string Desc;
        public float GoldAddFactor;
        public int Price;

        public LevelConfig(int price,List<string> technology, string desc, float goldAddFactor )
        {
            Price = price;
            Technology = technology;
            Desc = desc;
            GoldAddFactor = goldAddFactor;
        }
    }
    public class GroupUpLevel<IT> : ISubMsgParser<IDyMsgParser<IT>, IT>
        where IT : class,IContext 
    {
        private IDyMsgParser<IT> m_Owner;   
        private ConcurrentDictionary<int,LevelCxt> GroupLevel = new ConcurrentDictionary<int, LevelCxt>();

        private static readonly List<LevelConfig> LevelConfigs = new List<LevelConfig>()
        {
            new LevelConfig(3200,new List<string>()
                {
                    "UPG.COMMON.UPGRADE_MELEE_DAMAGE_I",
                    "UPG.COMMON.UPGRADE_MELEE_ARMOR_I",
                    "UPG.COMMON.UPGRADE_RANGED_DAMAGE_I",
                    "UPG.COMMON.UPGRADE_RANGED_ARMOR_I"
                }
                , "1攻1防，+10%攻击力，+10%血量，+40%金币产出",0.4f ),
            
            new LevelConfig(10000,new List<string>()
                {
                    "UPG.COMMON.UPGRADE_MELEE_DAMAGE_II",
                    "UPG.COMMON.UPGRADE_MELEE_ARMOR_II",
                    "UPG.COMMON.UPGRADE_RANGED_DAMAGE_II",
                    "UPG.COMMON.UPGRADE_RANGED_ARMOR_II"
                }
                , "2攻2防，+20%攻击力，+20%血量，+90%金币产出",0.5f ),
            new LevelConfig(18000,new List<string>()
                {
                    "UPG.COMMON.UPGRADE_MELEE_DAMAGE_III",
                    "UPG.COMMON.UPGRADE_MELEE_ARMOR_III",
                    "UPG.COMMON.UPGRADE_RANGED_DAMAGE_III",
                    "UPG.COMMON.UPGRADE_RANGED_ARMOR_III"
                }
                , "3攻3防，+30%攻击力，+30%血量，+150%金币产出",0.6f ),
        };
        public void Init(IDyMsgParser<IT> owner)
        {
            m_Owner = owner;
        }

        public void Stop()
        {
            
        }

        public bool Parse(DyMsgOrigin msg)
        {
            return false;
        }

        public void OnTick(float delat)
        {
            
        }

        public void OnClear()
        {
            for (int i = 0; i < Aoe4DataConfig.GroupCount; ++i)
            {
                SendGroupLevel(i, 1,"",0.0f);
            }
            GroupLevel.Clear();
        }

        private void SendGroupLevel(int g, int l,string desc,float progress)
        {
            var msg = new UpdateGroupLevel()
            {
                Group = g,
                Level = l,
                Desc = desc,
                Progress = progress
            };
            m_Owner.InitCtx.SendMsgToOverlay((short)EMsgTy.UpdateGroupLevel,msg);
        }

        public void NotifyDepleteGold(int g, int gold)
        {
            if (!GroupLevel.ContainsKey(g))
                GroupLevel.TryAdd(g, new LevelCxt(){ All = 0, NowLevel = 1, NowConf = 0 });
            var v = GroupLevel[g];
            lock (v)
            {
                GroupLevel[g].All += gold;
                CheckUp(g, GroupLevel[g]);
            }
        }

        private void CheckUp(int g,LevelCxt c)
        {
            if(c.NowConf >= LevelConfigs.Count) return;
            var conf = LevelConfigs[c.NowConf];
            if(c.All >= conf.Price)
            {
                Up(g, c,conf);
                CheckUp(g,c);
            }
            else
            {
                SendGroupLevel(g,c.NowConf + 1,conf.Desc,GetProgress(c,conf));
            }
        }

        private void Up(int g, LevelCxt c,LevelConfig config)
        {
            c.NowLevel = config.Price;
            c.NowConf++;
            
            LiveGameUtils.ForeachUsersByGroup(m_Owner.InitCtx,g,(id) => Utils.Locator.Instance.Get<DebugPlugin>().messageDispatcher.GetResourceMgr().AddAutoResourceAddFactor(id, config.GoldAddFactor),
                (u) =>
                {
                    u.AddHpMultiple(1);
                    u.AddDamageMultiple(1);
                });
            foreach (var t in config.Technology)
            {
                GivePlayerUpgrade(g,t);   
            }
            SendGroupLevel(g,c.NowConf + 1,config.Desc,GetProgress(c,config));
        }

        private float GetProgress(LevelCxt c, LevelConfig conf)
        {
            if(c.NowConf >= LevelConfigs.Count) return 1.0f;
            return ((float)c.All / LevelConfigs[c.NowConf].Price);
        }

        public void GivePlayerUpgrade(int g, string upg)
        {
            m_Owner.m_MsgDispatcher.GetBridge().AppendExecCode($"GiveAbility(PLAYERS[{g + 1}].id, nil, nil, {upg});");
        }
    }
}