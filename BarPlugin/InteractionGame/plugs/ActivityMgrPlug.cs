using conf.Activity;
using InteractionGame.Context;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace InteractionGame.plugs
{
    public class ActivityMgrPlug : IPlug<EGameAction>, IActivityMgr
    {
        private ConcurrentDictionary<EItemType, ConcurrentBag<conf.Activity.ActivityItem>> 
            activities = new ConcurrentDictionary<EItemType, ConcurrentBag<ActivityItem>>();
        private IGiftMgr giftMgr = null;

        public override void Init()
        {
            base.Init();
            Locator.Instance.Deposit<IActivityMgr>(this);
        }
        public override void Start()
        {
            base.Start();
            giftMgr = Locator.Instance.Get<IGiftMgr>(); 
        }
        public int ApplyActivity(EItemType type, UserData user)
        {
            if (activities.TryGetValue(type,out var v))
            {
                int count = 0;
                foreach(var it in v)
                {
                    giftMgr.ApplyGift(it.Gifts, user);
                    count++;
                }
                return count;
            }
            return 0;
        }

        public bool GetMultiplier(EItemType type, UserData user,out float res)
        {
            res = 0.0f;
            if (activities.TryGetValue(type,out var v))
            {
                foreach (var it in v)
                {
                    if (TryGetMultiplier(it, user, out var r))
                        res += r;
                }
                return true;
            }
            return false;
        }

        private bool TryGetMultiplier(ActivityItem it, UserData user, out float r)
        {
            if (it.Multiplier != null && it.Multiplier.TryGetValue(user.RealGuardLevel, out var v))
            {
                r = v;
                return true;
            }
            r = 0.0f;
            return false;
        }

        public override void Notify(EGameAction m)
        {
            switch (m)
            {
                case EGameAction.PreSettlement:
                    RefreshActivity(EItemType.Settlement);
                    break;
                case EGameAction.GameStart:
                    RefreshActivity(EItemType.SignIn);
                    break;
            }
        }

        public void RefreshActivity(EItemType type)
        {
            if(activities.ContainsKey(type))
                activities.TryRemove(type, out var _);

            var tmp = new List<ActivityItem>();
            var lastPriority = 0;
            var cumulative = false;
            foreach (var it in conf.Activity.ActivityItemMgr.GetInstance().Dict)
            {
                var checkTime = tmp.Count == 0 || it.Value.Priority > lastPriority || (cumulative && it.Value.Cumulative);
                if(checkTime && InActivity(it.Value))
                {
                    if(tmp.Count > 0 && !it.Value.Cumulative)
                        tmp.Clear();
                    tmp.Add(it.Value);
                    lastPriority = it.Value.Priority;
                    cumulative = it.Value.Cumulative;
                }
            }
            if(tmp.Count > 0)
            {
                activities.TryAdd(type, new ConcurrentBag<ActivityItem>());
                foreach (var it in tmp)
                    activities[type].Add(it);
            }
        }

        private bool InActivity(ActivityItem value)
        {
            var now = DateTime.Now;

            if (value.CyclePeriodType_e == ECyclePeriodType.Always)
                return true;

            if (value.LunarCalendar)
                return InActivityLunarCalendar(value);

            return InActivity(value.StartTime, value.EndTime, DateTime.Now, value.CyclePeriodType_e);
        }

        private bool InActivity(DateTime startTime,DateTime endTime,DateTime now,ECyclePeriodType cyclePeriodType)
        {
            switch (cyclePeriodType)
            {
                case ECyclePeriodType.Daily:
                    {
                        var start = new DateTime(1, 1, 1, startTime.Hour, startTime.Minute, startTime.Second);
                        var end = new DateTime(1, 1, 1, endTime.Hour, endTime.Minute, endTime.Second);
                        var n = new DateTime(1, 1, 1, now.Hour, now.Minute, now.Second);
                        return n >= start && n <= end;
                    }
                case ECyclePeriodType.Monthly:
                    {
                        try
                        {
                            var n = new DateTime(startTime.Year, startTime.Month, now.Day, now.Hour, now.Minute, now.Second);
                            return n >= startTime && n <= endTime;
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            return false;
                        }
                    }
                case ECyclePeriodType.Annually:
                    {
                        try
                        {
                            var n = new DateTime(startTime.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
                            return n >= startTime && n <= endTime;
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            return false;
                        }
                    }
            }
            return false;
        }

        private DateTime GetLunarCalendarDate(DateTime date)
        {
            var chineseCalendar = new ChineseLunisolarCalendar();
            int lunarYear = chineseCalendar.GetYear(date);
            int lunarMonth = chineseCalendar.GetMonth(date);
            int lunarDay = chineseCalendar.GetDayOfMonth(date);

            return new DateTime(lunarYear, lunarMonth, lunarDay, date.Hour, date.Minute, date.Second);
        }

        private bool InActivityLunarCalendar(ActivityItem value)
        {
            var now = GetLunarCalendarDate(DateTime.Now);

            return InActivity(value.StartTime, value.EndTime, now, value.CyclePeriodType_e);
        }

        public override void Tick()
        {
            RefreshActivity(EItemType.SignIn);
        }
    }
}
