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
    public class KillUnitRewardPlug : IPlug<EGameAction>
    {
        private IContext _context;
        private ConcurrentDictionary<string,int> KillUnitCountDict = new ConcurrentDictionary<string, int>();

        public override void Notify(EGameAction m)
        {
            switch (m)
            {
                case EGameAction.GameStop:
                    KillUnitCountDict.Clear();
                    break;
            }
        }

        public override void Start()
        {
            Locator.Deposit(this);
            base.Start();
            _context = Locator.Get<IContext>();
            _context.RegisterOnRecvGameMsg<KillUnitRewardData>(EGameMsg.BUnitReward, OnKillUnitReward);
        }
        public override void Stop()
        {
            base.Stop();
            Locator.Remove<KillUnitRewardPlug>();
        }

        private void OnKillUnitReward(string arg1, object arg2)
        {
            if(arg2 is KillUnitRewardData data)
            {
                var user = _context.GetMsgParser().GetUserData(data.id);
                if(user != null)
                {
                    _context.GetResourceMgr().AddResource(user.Id, data.reward);
                    if (KillUnitCountDict.TryGetValue(user.Id, out var count))
                        KillUnitCountDict[user.Id] = count + data.killCount;
                    else
                        KillUnitCountDict[user.Id] = data.killCount;
                }
            }
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
