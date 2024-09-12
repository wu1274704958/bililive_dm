using InteractionGame.Context;
using System.IO;
using Utils;

namespace conf
{
    public class ConfigMgr : IPlug<EGameAction>
    {
        public static readonly string ConfigPath = "D:\\code\\bililive_dm\\resource\\bar";
        protected bool NeedReload = false;
        public ConfigMgr() {
            InitTable();
        }

        public override void Init()
        {
            base.Init();
            Locator.Deposit(this);
        }

        public override void Dispose()
        {
            base.Dispose();
            Locator.Remove<ConfigMgr>();
        }

        public static void InitTable()
        {
            conf.Squad.SquadDataMgr.InitInstance(new FileInfo(Path.Combine(ConfigPath, "SquadData.dat")));
            conf.Squad.SettingMgr.InitInstance(new FileInfo(Path.Combine(ConfigPath, "Setting.dat")));
            conf.Reinforcements.ReinforcementsDataMgr.InitInstance(new FileInfo(Path.Combine(ConfigPath, "ReinforcementsData.dat")));
            conf.Gift.GiftItemMgr.InitInstance(new FileInfo(Path.Combine(ConfigPath, "GiftItem.dat")));
            conf.CommonConfig.CommonConfigMgr.InitInstance(new FileInfo(Path.Combine(ConfigPath, "CommonConfig.dat")));
            conf.Activity.ActivityItemMgr.InitInstance(new FileInfo(Path.Combine(ConfigPath, "ActivityItem.dat")));
        }

        public static void ReloadAll()
        {
            conf.Squad.SquadDataMgr.Reload();
            conf.Squad.SettingMgr.Reload();
            conf.Reinforcements.ReinforcementsDataMgr.Reload();
            conf.Gift.GiftItemMgr.Reload();
            conf.CommonConfig.CommonConfigMgr.Reload();
            conf.Activity.ActivityItemMgr.Reload();
        }

        public void Reload()
        {
            NeedReload = true;
        }

        public override void OnReceiveNotify(EGameAction m,object args = null)
        {
            if(m == EGameAction.GameStop && NeedReload)
            {
                ReloadAll();
                NeedReload = false;
                Notify(EGameAction.ConfigReload);
            }
        }

        public override void Tick()
        {
            throw new System.NotImplementedException();
        }
    }
}