using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using BililiveDebugPlugin.InteractionGame.Data;
using BililiveDebugPlugin.InteractionGame.Parser;
using BililiveDebugPlugin.InteractionGameUtils;
using conf.Reinforcements;
using conf.Squad;
using InteractionGame;
using Utils;

namespace BililiveDebugPlugin.InteractionGame.plugs
{

    [ProtoBuf.ProtoContract]
    public class GroupDefenceKeepHpData
    {
        [ProtoBuf.ProtoMember(1)] public int Group;
        [ProtoBuf.ProtoMember(2)] public int Count;
    }
    public class DefineKeepDamagedSpawnSquadPlug : IPlug<EGameAction>
    {
        protected ConcurrentDictionary<int,(int,int)> _defineKeepHp = new ConcurrentDictionary<int, (int,int)>();
        protected Aoe4GameState _gameState;
        protected IContext _cxt;
        private Aoe4StateData _last = new Aoe4StateData() {  R = 0, G = 0, B = 0 };
        public readonly static double Max = 65535.0;

        public override void Init()
        {
            base.Init();
            InitDefineKeepHp();
        }

        public override void Start()
        {
            base.Start();
            _gameState = Locator.Instance.Get<Aoe4GameState>();
            _cxt = Locator.Instance.Get<IContext>();
            Locator.Instance.Deposit(this);
        }

        private void InitDefineKeepHp()
        {
            for (int i = 0; i < Aoe4DataConfig.GroupCount; i++)
            {
                if (!_defineKeepHp.ContainsKey(i))
                    _defineKeepHp.TryAdd(i, (-1,100));
                else
                    _defineKeepHp[i] = (-1,100);
            }
        }

        public override void Tick()
        {
            var d = _gameState.CheckState(EAoe4State.DefineKeepHp);
            if (d.R > 0 && d.R <= Aoe4DataConfig.GroupCount && !_last.Equals(d))
            {
                var old = _defineKeepHp[d.R - 1];
                var @new = (d.G << 8) | d.B;
                _defineKeepHp[d.R - 1] = (@new,old.Item2);
                //if(old.Item1 != @new)
                OnDefineKeepHpChanged(d.R - 1, old.Item1 / Max, @new / Max);
                CheckNeedUpdateHp();
                _last = d;
            }
        }

        private void OnDefineKeepHpChanged(int g, double old, double @new)
        {
            CheckNeedSpawnSquad(g);
            SendDefenseKeepHpChange(g, _defineKeepHp[g].Item1);
        }

        private void SendDefenseKeepHpChange(int g, int item1)
        {
            _cxt.SendMsgToOverlay((short)EMsgTy.DefenseKeepHp,new GroupDefenceKeepHpData() { Count = item1,Group = g });   
        }

        private void CheckNeedSpawnSquad(int g)
        {
            if (_defineKeepHp.TryGetValue(g, out var v))
            {
                if (v.Item1 < 0) return;
                foreach (var it in conf.Reinforcements.ReinforcementsDataMgr.GetInstance().List)
                {
                    if ((int)(v.Item1 / Max * 100) <= it.Key && it.Key < v.Item2)
                    {
                        _defineKeepHp[g] = (v.Item1,it.Key);
                        DoSpawnSquad(g,it.Value);
                    }
                }
            }
        }

        public void DoSpawnSquad(int g, ReinforcementsData v)
        {
            List<(int, int)> Squad = ObjPoolMgr.Instance.Get<List<(int, int)>>(null, DefObjectRecycle.OnListRecycle).Get();
            ushort addedAttrForGroup = global::InteractionGame.Utils.Merge(v.DamageAdded, v.HpAdded);
            foreach (var it in v.SquadConf)
            {
                var sd = Aoe4DataConfig.GetMaxLevelSquad(it.Key, g);
                if(sd != null)
                    Squad.Add((sd.RealId, (int)(it.Value / sd.RealPrice(g))));
            }
            
            if (Squad.Count > 0)
            {
                SquadGroup squad = null;
                Locator.Instance.Get<MsgGiftParser<DebugPlugin>>().SpawnManySquadQueue((-(g + 1)).ToString(), squad = SquadGroup.FromData(Squad,g).SetAddedAttr(addedAttrForGroup), 
                    1, 0, null, 0, giveHonor: 0, upLevelgold: 0);
                LargeTips.Show(LargePopTipsDataBuilder.Create($"{DebugPlugin.GetColorById(g + 1)}方", $"{v.Name}抵达！")
                .SetLeftColor(LargeTips.GetGroupColor(g)).SetRightColor(LargeTips.Yellow));
            }
        }

        public override void Notify(EGameAction m)
        {
            switch (m)
            {
                case EGameAction.GameStart:
                    CheckNeedUpdateHp();
                    break;
                case EGameAction.GameStop:
                    InitDefineKeepHp();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(m), m, null);
            }
        }

        private void CheckNeedUpdateHp()
        {
            foreach (var it in _defineKeepHp)
            {
                if (it.Value.Item1 < 0)
                {
                    Locator.Instance.Get<DebugPlugin>().messageDispatcher.GetBridge().AppendExecCode($"DKH_Update({it.Key + 1});");
                    break;
                }
            }
        }
    }
}