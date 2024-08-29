using System;
using System.Collections.Generic;

namespace conf.Squad
{
    public partial class SquadData
    {
        public String GetBlueprint(int g)
        {
            if (GetOverload(g, out var data))
                return data.GetBlueprint(g);
            return IsBase ? String.Concat(PB, SettingMgr.GetCountry(g)) : PB;
        }

        public bool GetOverload(int g,out SquadData data)
        {
            if (OverloadId > 0)
            {
                data = SquadDataMgr.GetInstance().Get(OverloadId);
                return data != null;
            }
            data = null;
            return false;
        }
        public int RealId => OverloadId > 0 ? OverloadId : Id;
        public int Slot => RealId / 10000;
    }
}