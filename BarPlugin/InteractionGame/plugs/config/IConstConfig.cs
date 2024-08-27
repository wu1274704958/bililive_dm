using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteractionGame.plugs.config
{
    public interface IConstConfig
    {
        string OverlayCommKey { get; }
        string GameCommKey { get; }
        uint OverlayCommSize { get; }
        uint GameCommSize { get; }
        uint EndDelay { get; }
        Dictionary<string, int> GroupNameMap { get; }
        string GetGroupName(int id);
        int GetGroupIdByName(string name);
    }
}
