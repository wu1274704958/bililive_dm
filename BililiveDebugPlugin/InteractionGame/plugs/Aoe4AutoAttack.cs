using BililiveDebugPlugin.InteractionGame.Data;
using Interaction;
using InteractionGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace BililiveDebugPlugin.InteractionGame.plugs
{
    internal class Aoe4AutoAttack : IPlug<EGameAction>
    {
        private int[] _time = new int[8];
        public override void Init()
        {
            base.Init();
            //Locator.Instance.Get<Aoe4GameState>().AddObserver(this);

        }
        public override void Notify(EGameAction m)
        {

        }

        

        private bool IsOppositeManySquad(int g)
        {
            for(int i = 0; i < Aoe4DataConfig.GroupCount;++i)
            {
                //if (i == g) continue;
                if(Locator.Instance.Get<Aoe4GameState>().GetSquadCount(i) >= 800)
                    return true;
            }
            return false;
        }

        public void SendAllSquadAttack<IT>(IAoe4Bridge<IT> bridge, int self,int target, long uid, bool isMove = false)
            where IT : class,IContext
        {
            if (target < 0)
            {
                bridge.ExecAllSquadMove(self + 1, uid);
            }
            else
            {
                bridge.ExecAllSquadMoveWithTarget(self + 1, target + 1, uid, isMove ? 1 : 0);
            }
        }

        public override void Tick()
        {
            for (int g = 0; g < Aoe4DataConfig.GroupCount; ++g)
            {
                if (IsOppositeManySquad(g))
                {
                    //if (_time[g] <= 0)
                    //{
                        //Interlocked.Exchange(ref _time[g], 3);
                        LiveGameUtils.ForeachUsersByGroup(Locator.Instance.Get<IContext>(), g, (uid) =>
                        {
                            var pp = Locator.Instance.Get<IDyPlayerParser<DebugPlugin>>();
                            var target = pp.GetTarget(uid);
                            var self = pp.GetGroupById(uid);
                            SendAllSquadAttack(Locator.Instance.Get<DebugPlugin>().messageDispatcher.GetBridge(), self, target, uid);
                        }, null);
                    //}
                    //else
                    //{
                    //    Interlocked.Exchange(ref _time[g], _time[g] - 1);
                    //}
                }
            }
        }
    }
}
