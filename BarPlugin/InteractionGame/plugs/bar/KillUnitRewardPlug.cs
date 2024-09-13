using InteractionGame.Context;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace InteractionGame.plugs.bar
{
    public class KillUnitRewardData
    {
        public string id;
        public float reward = 0;
        public int killCount = 0;
    }
    public struct GroupRewardData
    {
        public float Reward;
        public int KillCount;
    }

    public class KillUnitRewardPlug : IPlug<EGameAction>
    {
        private IContext _context;
        private ConcurrentDictionary<string,int> KillUnitCountDict = new ConcurrentDictionary<string, int>();
        private ConcurrentDictionary<int, GroupRewardData> GroupRewardDict = new ConcurrentDictionary<int, GroupRewardData>();
        public EventDispatcher<(int, GroupRewardData)> GroupRewardChangedDispatcher { get; private set; } = new EventDispatcher<(int, GroupRewardData)>();

        public override void OnReceiveNotify(EGameAction m,object args = null)
        {
            switch (m)
            {
                case EGameAction.GameStop:
                    KillUnitCountDict.Clear();
                    GroupRewardDict.Clear();
                    break;
            }
        }

        public override void Start()
        {
            Locator.Deposit(this);
            base.Start();
            _context = Locator.Get<IContext>();
            _context.RegisterOnRecvGameMsg<List<KillUnitRewardData>>(EGameMsg.BUnitReward, OnKillUnitReward);
        }
        public override void Stop()
        {
            base.Stop();
            Locator.Remove<KillUnitRewardPlug>();
        }

        private void OnKillUnitReward(string arg1, object arg2)
        {
            if(arg2 is List<KillUnitRewardData> list)
            {
                foreach(var data in list)
                {
                    var user = _context.GetMsgParser().GetUserData(data.id);
                    if (user != null)
                    {
                        UpdateGroupReward(data, user);
                        _context.GetResourceMgr().AddResource(user.Id, data.reward);
                        if (KillUnitCountDict.TryGetValue(user.Id, out var count))
                            KillUnitCountDict[user.Id] = count + data.killCount;
                        else
                            KillUnitCountDict[user.Id] = data.killCount;
                    }
                }
            }
        }

        private void UpdateGroupReward(KillUnitRewardData data, UserData user)
        {
            if (!GroupRewardDict.ContainsKey(user.Group))
                GroupRewardDict.TryAdd(user.Group, new GroupRewardData() { Reward = data.reward,KillCount = data.killCount });
            else
            {
                var old = GroupRewardDict[user.Group];
                GroupRewardDict[user.Group] = new GroupRewardData() { Reward = old.Reward + data.reward, KillCount = old.KillCount + data.killCount };
            }
            GroupRewardChangedDispatcher.Dispatch((user.Group, GroupRewardDict[user.Group]));
        }

        public List<(string,int)> GetCurrentKillListSorted()
        {
            List<(string, int)> res = new List<(string, int)> ();
            foreach(var it in KillUnitCountDict)
            {
                res.Add((it.Key, it.Value));
            }
            res.Sort((a,b) => b.Item2 - a.Item2);
            return res;
        }

        public override void Tick()
        {
            throw new NotImplementedException();
        }
    }
}
