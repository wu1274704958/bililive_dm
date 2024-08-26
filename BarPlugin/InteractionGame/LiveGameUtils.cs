using System;
using System.Collections.Generic;
using System.Windows.Documents;
using InteractionGame;
using Utils;

namespace BililiveDebugPlugin.InteractionGame
{
    public class LiveGameUtils
    {
        public static void ForeachUsersByGroup<IT>(IT cxt,int id,Action<string> a1, Action<UserData> a2)
            where IT : class, IContext
        {
            var c = cxt as DebugPlugin;
            var pp = c.messageDispatcher.GetPlayerParser();
            var mp = c.messageDispatcher.GetMsgParser();
            var ls = pp.GetUsersByGroup(id);
            foreach (var it in ls)
            {
                a1(it);
                if(a2 != null)
                {
                    var u = mp.GetUserData(it);
                    if (u != null)
                        a2(u);
                }
            }
            ObjPoolMgr.Instance.Get<List<string>>().Return(ls);
        }
    }

    
}