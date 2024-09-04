using System.IO;
using Utils;

namespace conf
{
    public sealed class ConfigMgr
    {
        public static readonly string ConfigPath = "E:\\code\\bililive_dm\\resource\\bar";

        public static void Init()
        {
            conf.Squad.SquadDataMgr.InitInstance(new FileInfo(Path.Combine(ConfigPath, "SquadData.dat")));
            conf.Squad.SettingMgr.InitInstance(new FileInfo(Path.Combine(ConfigPath, "Setting.dat")));
            conf.Reinforcements.ReinforcementsDataMgr.InitInstance(new FileInfo(Path.Combine(ConfigPath, "ReinforcementsData.dat")));
            conf.Gift.GiftItemMgr.InitInstance(new FileInfo(Path.Combine(ConfigPath, "GiftItem.dat")));
            conf.CommonConfig.CommonConfigMgr.InitInstance(new FileInfo(Path.Combine(ConfigPath, "CommonConfig.dat")));
            conf.Reinforcements.ReinforcementsDataMgr.GetInstance().OnLoaded();
        }

        public static void ReloadAll()
        {
            conf.Squad.SquadDataMgr.Reload();
            conf.Squad.SettingMgr.Reload();
            conf.Reinforcements.ReinforcementsDataMgr.Reload();
            conf.Gift.GiftItemMgr.Reload();
            conf.Reinforcements.ReinforcementsDataMgr.GetInstance().OnLoaded();
        }

        public static void ReloadReinforcements()
        {
            conf.Reinforcements.ReinforcementsDataMgr.Reload();
            conf.Reinforcements.ReinforcementsDataMgr.GetInstance().OnLoaded();
        }
    }
}