using System.IO;
using Utils;

namespace conf
{
    public sealed class ConfigMgr
    {
        private static readonly string ConfigPath = "E:\\code\\bililive_dm\\resource\\data";

        public static void Init()
        {
            conf.Squad.SquadDataMgr.InitInstance(new FileInfo(Path.Combine(ConfigPath, "SquadData.dat")));
        }

        public static void ReloadAll()
        {
            conf.Squad.SquadDataMgr.Reload();
        }
    }
}