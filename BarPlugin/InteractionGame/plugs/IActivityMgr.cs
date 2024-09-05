using conf.Activity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteractionGame.plugs
{
    public interface IActivityMgr
    {
        int ApplyActivity(EItemType type,UserData user);
        bool GetMultiplier(EItemType type,UserData user,out float v);
        void RefreshActivity(EItemType type);
        string GetOverride(EItemType type, UserData user,string str);
    }
}
