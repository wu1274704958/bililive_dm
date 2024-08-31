using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing.Design;
using System.Text;
using System.Windows.Documents;

using BililiveDebugPlugin.InteractionGame.mode;
using BililiveDebugPlugin.InteractionGameUtils;
using InteractionGame;
using InteractionGame.Context;
using InteractionGame.plugs.config;
using ProtoBuf;
using Utils;

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
        public int Level;
        public float GoldAddFactor;
        public int Price;
        public int AddHp;
        public int AddDamage;
        private StringBuilder _descStringBuilder = new StringBuilder();

        public LevelConfig(int level,int price,List<string> technology, float goldAddFactor,int addHp,int addDamage)
        {
            Level = level;
            Price = price;
            Technology = technology;
            GoldAddFactor = goldAddFactor;
            AddDamage = addDamage;
            AddHp = addHp;
        }

        public string GetDesc(int goldMultiplier = 1, int damageMultiplier = 1, int hpMultiplier = 1)
        {
            _descStringBuilder.Clear();
            _descStringBuilder.Append($"{Level}攻{Level}防，");
            if (AddDamage > 0)
                _descStringBuilder.Append($"+{AddDamage * damageMultiplier * 10}%攻击力，");
            if(AddHp > 0)
                _descStringBuilder.Append($"+{AddHp * hpMultiplier * 10}%血量，");
            _descStringBuilder.Append($"+{GoldAddFactor * goldMultiplier * 100}%金币产出");
            return _descStringBuilder.ToString();
        }
        public string GetDesc((int,int,int) multiplier)
        {
            return GetDesc(multiplier.Item1, multiplier.Item2, multiplier.Item3);
        }
    }
    public class GroupUpLevel : ISubMsgParser , IPlayerParserObserver
    {
        private IDyMsgParser m_Owner;   
        private ConcurrentDictionary<int,LevelCxt> GroupLevel = new ConcurrentDictionary<int, LevelCxt>();
        private IContext _cxt;
        private static readonly List<LevelConfig> LevelConfigs = new List<LevelConfig>()
        {
            new LevelConfig(1,2000,new List<string>()
                {
                    "UPG.COMMON.UPGRADE_MELEE_DAMAGE_I",
                    "UPG.COMMON.UPGRADE_MELEE_ARMOR_I",
                    "UPG.COMMON.UPGRADE_RANGED_DAMAGE_I",
                    "UPG.COMMON.UPGRADE_RANGED_ARMOR_I",
                }
                , 0.62f,0,1 ),
            
            new LevelConfig(2,10000,new List<string>()
                {
                    "UPG.COMMON.UPGRADE_MELEE_DAMAGE_II",
                    "UPG.COMMON.UPGRADE_MELEE_ARMOR_II",
                    "UPG.COMMON.UPGRADE_RANGED_DAMAGE_II",
                    "UPG.COMMON.UPGRADE_RANGED_ARMOR_II",
                }
                , 0.63f,0,2 ),
            new LevelConfig(3,18000,new List<string>()
                {
                    "UPG.COMMON.UPGRADE_MELEE_DAMAGE_III",
                    "UPG.COMMON.UPGRADE_MELEE_ARMOR_III",
                    "UPG.COMMON.UPGRADE_RANGED_DAMAGE_III",
                    "UPG.COMMON.UPGRADE_RANGED_ARMOR_III"
                }
                , 0.64f,0,3 ),
        };
        public void Init(IDyMsgParser owner)
        {
            m_Owner = owner;
        }

        public void OnStartGame()
        {
            for (int i = 0; i < Locator.Instance.Get<IGameState>().GroupCount; ++i)
            {
                var level = Locator.Instance.Get<IGameMode>().StartGroupLevel(i);
                GroupLevel.TryAdd(i, new LevelCxt(){ All = 0, NowLevel = level + 1, NowConf = level  });
                SendGroupLevel(i, level + 1,"",0.0f);
            }
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
                var (goldMultiplier, damageMultiplier, hpMultiplier) = GetGiftMultiplier();
                Up(g, c,conf,goldMultiplier,damageMultiplier,hpMultiplier);
                CheckUp(g,c);
            }
            else
            {
                SendGroupLevel(g,c.NowConf + 1,conf.GetDesc(GetGiftMultiplier()),GetProgress(c,conf));
            }
        }
        
        private (int,int,int) GetGiftMultiplier()
        {
            if(global::InteractionGame.Utils.GetNewYearActivity() > 0)
                return (2,1,1);
            return (1,1,1);
        }

        private void Up(int g, LevelCxt c,LevelConfig config,int goldMultiplier = 1, int damageMultiplier = 1, int hpMultiplier = 1)
        {
            c.NowLevel = config.Price;
            c.NowConf++;
            var desc = config.GetDesc(goldMultiplier,damageMultiplier,hpMultiplier);
            LargeTips.Show(LargePopTipsDataBuilder.Create($"恭喜{Locator.Instance.Get<IConstConfig>().GetGroupName(g + 1)}方",$"升至{GetLevelStr(c.NowConf + 1)}本")
                .SetBottom(desc).SetBottomColor(LargeTips.Cyan).SetLeftColor(LargeTips.GetGroupColor(g)).SetRightColor(LargeTips.Yellow));
            
            LiveGameUtils.ForeachUsersByGroup(m_Owner.InitCtx,g,(id) =>
                    _cxt.GetResourceMgr().AddAutoResourceAddFactor(id, config.GoldAddFactor * goldMultiplier),
                (u) =>
                {
                    u.AddHpMultiple(config.AddHp * hpMultiplier);
                    u.AddDamageMultiple(config.AddDamage * damageMultiplier);
                });
            foreach (var t in config.Technology)
            {
                GivePlayerUpgrade(g,t);   
            }
            SendGroupLevel(g,c.NowConf + 1,desc,GetProgress(c,config));
        }

        private float GetProgress(LevelCxt c, LevelConfig conf)
        {
            if(c.NowConf >= LevelConfigs.Count) return 1.0f;
            return ((float)c.All / LevelConfigs[c.NowConf].Price);
        }

        public void GivePlayerUpgrade(int g, string upg)
        {
            //m_Owner..GetBridge().AppendExecCode($"GiveAbility(PLAYERS[{g + 1}].id, nil, nil, {upg});");
        }
        private string GetLevelStr(int l)
        {
            switch (l)
            {
                case 1:return "I";
                case 2:return "II";
                case 3:return "III";
                case 4:return "IV";
                case 5:return "V";
                case 6:return "VI";
                case 7:return "VII";
            }

            return "";
        }

        public void Start()
        {
            _cxt = Locator.Instance.Get<IContext>();
            m_Owner.InitCtx.GetPlayerParser().AddObserver(this);
        }

        public void OnAddGroup(UserData userData, int g)
        {
            if(GroupLevel.TryGetValue(g,out var levelCxt) && levelCxt.NowConf >= 1)
            {
                var (goldMultiplier, damageMultiplier, hpMultiplier) = GetGiftMultiplier();
                for (int i = 0;i <= Math.Min(levelCxt.NowConf - 1, LevelConfigs.Count - 1);i++)
                {
                    var conf = LevelConfigs[i];
                    _cxt.GetResourceMgr().AddAutoResourceAddFactor(userData.Id, conf.GoldAddFactor * goldMultiplier);
                    userData.AddHpMultiple(conf.AddHp * hpMultiplier);
                    userData.AddDamageMultiple(conf.AddDamage * damageMultiplier);
                }
            }
        }

        public void OnChangeGroup(UserData userData, int old, int n)
        {

        }
    }

       
    }