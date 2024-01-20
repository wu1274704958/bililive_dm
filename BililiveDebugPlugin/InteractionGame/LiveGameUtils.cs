using System.Collections.Generic;
using System.Windows.Documents;
using InteractionGame;
using Utils;

namespace BililiveDebugPlugin.InteractionGame
{
    public class LiveGameUtils
    {
        public static void AddGoldAddFactorByGroup<IT>(IT cxt,int id, float addFactor)
            where IT : class, IContext
        {
            var c = cxt as DebugPlugin;
            var ls = c.messageDispatcher.GetPlayerParser().GetUsersByGroup(id);
            foreach (var it in ls)
            {
                c.messageDispatcher.GetResourceMgr().AddAutoResourceAddFactor(it,addFactor);
            }
            ObjPoolMgr.Instance.Get<List<long>>().Return(ls);
        }
    }

    
}