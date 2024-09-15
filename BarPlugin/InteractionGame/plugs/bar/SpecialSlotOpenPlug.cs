using BililiveDebugPlugin.InteractionGameUtils;
using conf.SpecialSlot;
using conf.Squad;
using InteractionGame.Context;
using InteractionGame.plugs.config;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace InteractionGame.plugs.bar
{
    public class SpecialSlotOpenPlug : IPlug<EGameAction>
    {
        private Action<(int, GroupRewardData)> listener;
        private List<conf.SpecialSlot.SpecialSlot> SpecialSlots = new List<SpecialSlot>();
        private ConcurrentDictionary<int,int> GroupSlotState = new ConcurrentDictionary<int,int>();
        private ISquadMgr squadMgr;
        private IContext context;

        public override void Init()
        {
            base.Init();
            Locator.Deposit(this);
            SpecialSlots.Clear();
            foreach(var it in conf.SpecialSlot.SpecialSlotMgr.GetInstance().Dict)
            {
                SpecialSlots.Add(it.Value);
            }
            SpecialSlots.Sort((a, b) => b.Slot - a.Slot);
        }

        public override void Start()
        {
            base.Start();
            squadMgr = Locator.Get<ISquadMgr>();
            listener = Locator.Get<KillUnitRewardPlug>().GroupRewardChangedDispatcher.AddListener(OnGroupRewardChanged);
            context = Locator.Get<IContext>();
        }

        public override void Stop()
        {
            Locator.Get<KillUnitRewardPlug>().GroupRewardChangedDispatcher.RemoveListener(listener);
            base.Stop();
        }

        private void OnGroupRewardChanged((int, GroupRewardData) d)
        {
            if (!GroupSlotState.ContainsKey(d.Item1))
                GroupSlotState.TryAdd(d.Item1, 0);
            foreach(var it in SpecialSlots)
            {
                if((it.Group == -1 || it.Group == d.Item1) && it.Slot > GroupSlotState[d.Item1] && it.TestExpr.EvaluateExpr<bool>(d.Item2))
                {
                    GroupSlotState[d.Item1] = it.Slot;
                    OnGroupRewardTestSuccess(d.Item1,it,d.Item2);
                }
            }
        }

        public void OnGroupRewardTestSuccess(int g,SpecialSlot it, GroupRewardData data)
        {
            SquadData sd = null;
            if((sd = squadMgr.RandomSpecialSlot(g, it.Slot)) != null)
            {
                var addedTips = "";
                var added = it.AddExpr.EvaluateExpr<double>(data,it.Slot);
                if (added > 0)
                {
                    context.GetResourceMgr().AddAutoResourceAddFactor(g,Convert.ToSingle(added));
                    addedTips = $",获得金币加成{(int)(added*100)}%";
                }
                LargePopTipsDataBuilder.Create(Locator.Get<IConstConfig>().GetGroupName(g + 1)+"方", $"解锁卡槽{(char)it.Slot}")
                    .SetLeftColor(LargeTips.GetGroupColor(g))
                    .SetRightColor(LargeTips.Yellow)
                    .SetBottom($"卡槽{(char)it.Slot}[<color=yellow>{sd.Name}</color>]可用{addedTips}")
                    .SetBottomColor(LargeTips.Cyan)
                    .Show();
                squadMgr.SendSpecialSlot();
            }
        }

        public override void OnReceiveNotify(EGameAction m, object args = null)
        {
            switch(m)
            {
                case EGameAction.GameStop:
                    GroupSlotState.Clear();
                    break;
            }
        }

        public override void Tick()
        {
            throw new NotImplementedException();
        }
    }
}
