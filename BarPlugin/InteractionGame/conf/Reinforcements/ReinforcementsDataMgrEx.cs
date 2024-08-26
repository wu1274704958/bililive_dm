using System.Collections.Generic;
using System.Linq;

namespace conf.Reinforcements
{
    public partial class ReinforcementsDataMgr
    {
        private List<KeyValuePair<int, ReinforcementsData>> _list;
        public IReadOnlyList<KeyValuePair<int, ReinforcementsData>> List => _list;

        public void OnLoaded()
        {
            _list = Dict.ToList();
            _list.Sort((a, b) => b.Key.CompareTo(a.Key));
        }
    }
}