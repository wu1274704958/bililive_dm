using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using BililiveDebugPlugin.InteractionGameUtils;
using InteractionGame;
using Utils;

namespace BililiveDebugPlugin.InteractionGame.mode
{
    public class BossGameMode : BaseGameMode, IPlayerParserObserver
    {
        protected ConcurrentDictionary<string, int> SeatCountOfPlayer = new ConcurrentDictionary<string, int>();
        private IContext _cxt;

        public override void Start()
        {
            base.Start();
            Locator.Get<IContext>().GetPlayerParser().AddObserver(this);
            _cxt = Locator.Get<IContext>();
        }

        public void SetSeatCount(string oid, int v)
        {
            SeatCountOfPlayer[oid] = v;
        }
        public override int GetSeatCountOfPlayer(string id, int g)
        {
            if (SeatCountOfPlayer.TryGetValue(id, out var v))
                return v;
            if (g == 0)
                return 3;
            return 1;
        }

        public override bool NextBackToDefault()
        {
            return true;
        }

        public override void OnPause()
        {
            base.OnPause();
            SeatCountOfPlayer.Clear();
        }

        public override void OnResume(object args)
        {
            base.OnResume(args);
            if (args != null && args is ConcurrentDictionary<string, int> dict)
                SeatCountOfPlayer = dict;
            LargeTips.Show(LargePopTipsDataBuilder.Create("Boss","战")
                .SetLeftColor(LargeTips.Cyan)
                .SetRightColor(LargeTips.Cyan)
                //.SetBottom(GetBossName())
                //.SetBottomColor(LargeTips.Yellow)
            );
        }

        private string GetBossName()
        {
            return null;
        }

        public override int GetPlayerGroup(string id)
        {
            if (SeatCountOfPlayer.ContainsKey(id))
                return 0;
            return -1;
        }

        public override int OverrideGetPlayerCount(int g, int count)
        {
            if (g == 0 && count == 0)
                return 99;
            return count;
        }
        public override int StartGroupLevel(int g)
        {
            return 3;
        }

        public void OnAddGroup(UserData userData, int g)
        {
            if(SeatCountOfPlayer.ContainsKey(userData.Id) && userData.GuardLevel != 2)
            {
                _cxt.GetResourceMgr().AddAutoResourceAddFactor(userData.Id, 2.6f);
            }
        }

        public void OnChangeGroup(UserData userData, int old, int n)
        {

        }

        public void OnClear()
        {

        }

        public override float GetSettlementHonorMultiplier(string id, bool win)
        {
            return win ? 2.0f : 1f;
        }
    }
}