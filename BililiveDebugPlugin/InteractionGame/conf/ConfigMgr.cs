﻿using System.IO;
using Utils;

namespace conf
{
    public sealed class ConfigMgr
    {
        private static readonly string ConfigPath = "E:\\code\\bililive_dm\\resource\\data";

        public static void Init()
        {
            conf.Squad.SquadDataMgr.InitInstance(new FileInfo(Path.Combine(ConfigPath, "SquadData.dat")));
            conf.Squad.SettingMgr.InitInstance(new FileInfo(Path.Combine(ConfigPath, "Setting.dat")));
            conf.Reinforcements.ReinforcementsDataMgr.InitInstance(new FileInfo(Path.Combine(ConfigPath, "ReinforcementsData.dat")));
            conf.Squad.SquadDataMgr.GetInstance().OnLoaded();
            conf.Reinforcements.ReinforcementsDataMgr.GetInstance().OnLoaded();
        }

        public static void ReloadAll()
        {
            conf.Squad.SquadDataMgr.Reload();
            conf.Squad.SettingMgr.Reload();
            conf.Reinforcements.ReinforcementsDataMgr.Reload();
            conf.Squad.SquadDataMgr.GetInstance().OnLoaded();
            conf.Reinforcements.ReinforcementsDataMgr.GetInstance().OnLoaded();
        }

        public static void ReloadReinforcements()
        {
            conf.Reinforcements.ReinforcementsDataMgr.Reload();
            conf.Reinforcements.ReinforcementsDataMgr.GetInstance().OnLoaded();
        }
    }
}