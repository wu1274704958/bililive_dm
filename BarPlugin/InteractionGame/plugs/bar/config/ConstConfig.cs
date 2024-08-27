using InteractionGame.plugs.config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteractionGame.plugs.bar.config
{
    public class ConstConfig : IConstConfig
    {
        public string OverlayCommKey => "BarOverlayMem";

        public string GameCommKey => "BarGameMem";
        public uint OverlayCommSize => 1024 * 1024 * 20;
        public uint GameCommSize => 1024 * 1024 * 10;

        public uint EndDelay => 14000;

        public Dictionary<string, int> GroupNameMap { get; } = new Dictionary<string, int>
        {
            { "蓝",0 },
            { "红",1 },
            { "绿",2 },
            { "黄",3 },
        };

        public int GetGroupIdByName(string name)
        {
            if (GroupNameMap.TryGetValue(name, out var id)) return id;
            return -1;
        }

        public string GetGroupName(int id)
        {
            foreach (var v in GroupNameMap)
            {
                if (v.Value + 1 == id)
                {
                    return v.Key;
                }
            }
            return "";
        }
    }
}
