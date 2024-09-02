using System;
using System.Collections.Generic;
using System.Linq;
using BililiveDebugPlugin.DB.Model;
using InteractionGame;
using UserData = InteractionGame.UserData;
using ProtoBuf;
using Utils;
using InteractionGame.Context;
using InteractionGame.plugs.config;
using BililiveDebugPlugin.InteractionGame.mode;
using InteractionGame.plugs.bar;

namespace BililiveDebugPlugin.InteractionGame.Settlement
{
    class SettlementUser
    {
        public string Name;
        public string Icon;
        public int Group;
        public int Rank;
        public long Score;

        public void Reset()
        {
            Name = Icon = null;
            Group = -1;
            Rank = 0;
            Score = 0;
        }
    }
    class RankMsg
    {
        public string Title;
        public int WinGroup;
        public List<SettlementUser> CurrentScoreRank;
        public List<SettlementUser> CurrentKillRank;

        public List<SettlementUser> MonthlyCumulativeScoreRank;
        public List<SettlementUser> MonthlyCumulativeKillRank;

        public List<SettlementUser> MonthlySingleScoreRank;
        public List<SettlementUser> MonthlySingleKillRank;
    }
    public class BarSettlement<IT> : ISettlement<IT>
        where IT : class, IContext
    {

        public class SingleGameScoreData
        {
            public string id;
            public long score;

            public SingleGameScoreData(string id, long score)
            {
                this.id = id;
                this.score = score;
            }
        }
        public static readonly int Max = 20;
        private List<DB.Model.UserData> _tmpSingleGameScoreRank = new List<DB.Model.UserData>();
        private List<SingleGameScoreData> _tmpOriginSingleGameScoreRank = new List<SingleGameScoreData>();
        private Dictionary<string, (long, int)> _tmpLLDict = new Dictionary<string, (long, int)>();
        private IConstConfig _config;
        private IContext _context;
        private IDyMsgParser _msgParser;

        private List<(string,int)> _tmpCurrentKillListSorted;

        public void ShowSettlement(IT it,int winGroup)
        {
            //PrintGameMsg($"{GetColorById(d.g)}方获胜！！!");
            if(winGroup >= 0 && winGroup < Locator.Instance.Get<IGameState>().GroupCount)
                it.GetMsgParser().AddWinScore(winGroup, _config.WinGroupAddedScore);
            var data = it.GetMsgParser().GetSortedUserData();
            PreSettlement(it,data);
            var leastGroupList = it.GetPlayerParser().GetLeastGroupList();
            DB.DBMgr.Instance.OnSettlement(data, winGroup,
                (user,rank) => CalculatHonorSettlement(user, winGroup == user.Group, leastGroupList.Contains(user.Group), rank));
            //todo show settlement
            AfterSettlement();
            SendSettlement( data, winGroup - 1);

            
        }

        private void AfterSettlement()
        {
            _tmpSingleGameScoreRank = ConveToSettlementSingleRankData(_tmpOriginSingleGameScoreRank);
        }

        private void PreSettlement(IT it, List<UserData> data)
        {
            if(IsNewSeason(it))
            {
                DB.DBMgr.Instance.ClearAllUserScore();
            }
            _tmpOriginSingleGameScoreRank = RefreshSingleGameScoreRank(data);
        }

        private List<SingleGameScoreData> RefreshSingleGameScoreRank(List<UserData> data)
        {
            var rankList = GetSingleGameScoreRank();
            if (data.Count > 0)
            {
                var isChange = true;
                if (rankList.Count >= Max && rankList[rankList.Count - 1].score > data[0].Score)
                    isChange = false;
                if (isChange)
                {
                    _tmpLLDict.Clear();
                    for (int i = 0;i < rankList.Count;++i)
                        _tmpLLDict.Add(rankList[i].id, (rankList[i].score,i));
                    foreach (var d in data)
                    {
                        if (_config.IsTestId(d.Id))
                            continue;
                        if (_tmpLLDict.TryGetValue(d.Id, out var oldScore))
                        {
                            if(d.Score > oldScore.Item1)
                                rankList[oldScore.Item2].score = (long)d.Score;
                        }
                        else
                            rankList.Add(new SingleGameScoreData(d.Id, (long)d.Score));
                    }
                }
                if (isChange)
                {
                    rankList.Sort((a, b) => b.score.CompareTo(a.score));
                    while(rankList.Count > Max)
                        rankList.RemoveAt(rankList.Count - 1);
                    SaveSingleGameScoreRank(rankList);
                }
            }
            return rankList;
        }

        private List<DB.Model.UserData> ConveToSettlementSingleRankData(List<SingleGameScoreData> rankList)
        {
            var data = _tmpSingleGameScoreRank;
            data.Clear();
            foreach (SingleGameScoreData s in rankList)
            {
                if (string.IsNullOrEmpty(s.id))
                    continue;
                var ud = DB.DBMgr.Instance.GetUser(s.id);
                if (ud == null) continue;
                ud.Honor = s.score;
                data.Add(ud);
            }
            return data;
        }

        private void SaveSingleGameScoreRank(List<SingleGameScoreData> rankList)
        {
            DB.DBMgr.Instance.SetListForSys<SingleGameScoreData>((long)ESysDataTy.SingleScoreRank, rankList);
        }

        private List<SingleGameScoreData> GetSingleGameScoreRank()
        {
            var ls = DB.DBMgr.Instance.GetListForSys<SingleGameScoreData>((long)ESysDataTy.SingleScoreRank);
            if(ls == null)
                ls = new List<SingleGameScoreData>();
            return ls;
        }

        private bool IsNewSeason(IT it)
        {
            var now = DateTime.Now;
            var last = GetLastSavedSeasonTime();
            if(now.Day == 1 && now.AddMonths(-3).Month == last.Month)
            {
                DB.DBMgr.Instance.SetSysValue((long)ESysDataTy.SeasonTime, new DateTime(now.Year,now.Month,1), out _);
                it.Log($"Is new season {now}");   
                return true;
            }
            return false;
        }

        private DateTime GetLastSavedSeasonTime()
        {
            var d = DB.DBMgr.Instance.GetSystemDataOrCreate((long)ESysDataTy.SeasonTime, out var isNew);
            if(isNew)
            {
                var now = DateTime.Now;
                d.DateTimeValue = new DateTime(now.Year, now.Month, 1);
                DB.DBMgr.Instance.SetSysValue((long)ESysDataTy.SeasonTime, d.DateTimeValue, out _);
            }
            return d.DateTimeValue;
        }
        private void SendSettlement( List<UserData> data,int win)
        {
            RankMsg rankMsg = new RankMsg()
            {
                Title = win >= 0 ? $"{Locator.Instance.Get<IConstConfig>().GetGroupName(win + 1)}方获胜" : "平局",
                WinGroup = win,
                CurrentScoreRank = GetCurrentScoreRank(data),
                CurrentKillRank = GetCurrentKillRank(),
            };
            Locator.Instance.Get<IContext>().SendMsgToOverlay((short)EMsgTy.Settlement, rankMsg);
            RecycleSettlementMsg(rankMsg);
        }

        private List<SettlementUser> GetCurrentKillRank()
        {
            _tmpCurrentKillListSorted = Locator.Instance.Get<KillUnitRewardPlug>().GetCurrentKillListSorted();
            List<SettlementUser> res = new List<SettlementUser>();
            foreach(var item in _tmpCurrentKillListSorted)
            {
                var it = GetSettlementUserByUser(_msgParser.GetUserData(item.Item1));
                it.Score = item.Item2;
                res.Add(it);
            }
            return res;
        }

        private void RecycleSettlementMsg(RankMsg rankMsg)
        {
            RecycleSettlementUser(rankMsg.CurrentKillRank);
            RecycleSettlementUser(rankMsg.MonthlySingleKillRank);
            RecycleSettlementUser(rankMsg.MonthlyCumulativeKillRank);
            RecycleSettlementUser(rankMsg.CurrentScoreRank);
            RecycleSettlementUser(rankMsg.MonthlySingleScoreRank);
            RecycleSettlementUser(rankMsg.MonthlyCumulativeScoreRank);
        }

        private List<SettlementUser> GetCurrentScoreRank(List<UserData> data)
        {
            var res = new List<SettlementUser>();
            foreach(UserData item in data)
            {
                var ud = GetSettlementUserByUser(item);
                ud.Score = (long)item.Score;
                res.Add(ud);
            }
            return res;
        }

        private ObjectPool<SettlementUser> userPool => ObjPoolMgr.Instance.Get<SettlementUser>(null,u=>u.Reset());

        private SettlementUser GetSettlementUserByUser(UserData item)
        {
            var user = userPool.Get();
            user.Name = item.Name;
            user.Icon = item.Icon;
            user.Group = item.Group;
            user.Rank = item.Rank;
            return user;
        }

        private void RecycleSettlementUser(List<SettlementUser> users)
        {
            foreach (var user in users)
            {
                userPool.Return(user);
            }
            users.Clear();
        }

        private void TrySettlementScore(List<DB.Model.UserData> scoreList)
        {
            if (NeedSettlementHistoryScoreRank())
            {
                for (int i = 0; i < scoreList.Count; i++)
                {
                    var h = (10 - i) * 100;
                    scoreList[i].Ext = h;
                    DB.DBMgr.Instance.AddHonor(scoreList[i].Id, h);
                }
            }
        }

        private bool NeedSettlementHistoryScoreRank()
        {
            var now = DateTime.Now;
            if (now.DayOfWeek == DayOfWeek.Sunday && now.Hour >= 21)
            {
                var d = DB.DBMgr.Instance.GetSystemDataOrCreate((long)ESysDataTy.ScoreRankSettlementTime,out _);
                if(d.DateTimeValue.Year == now.Year && d.DateTimeValue.Month == now.Month && d.DateTimeValue.Day == now.Day) return false;
                DB.DBMgr.Instance.SetSysValue((long)ESysDataTy.ScoreRankSettlementTime, now,out _);
                return true;
            }
            return false;
        }


        private static long LoseSettlementHonorAdd = 3;
        private static long WinSettlementHonorAdd = 10;
        private static double LoseSettlementHonorFactor = 0.0005;
        private static double WinSettlementHonorFactor = 0.0012;
        private static readonly double LeastGroupSettlementHonorFactor = 0.0005;

        public long CalculatHonorSettlement(UserData user, bool win, bool isLeastGroup, int rank)
        {
            var config = Locator.Instance.Get<IConstConfig>();
            var f = (win ? WinSettlementHonorFactor : LoseSettlementHonorFactor) + (isLeastGroup ? LeastGroupSettlementHonorFactor : 0.0);
            if (user.FansLevel > 0) f += (user.FansLevel / 1000);
            if (user.GuardLevel > 0) f += f * config.GetPlayerHonorAdditionForSettlement(user.GuardLevel);
            f += f * Locator.Instance.Get<IGameMode>().GetSettlementHonorMultiplier(user.Id, win);
            var r = (long)Math.Floor(user.Score * f) + (win ? WinSettlementHonorAdd : LoseSettlementHonorAdd);
            var activityMult = global::InteractionGame.Utils.GetNewYearActivity() > 0 ? 2 : 1;

            return r * activityMult;
        }

        public void Init()
        {
            _config = Locator.Instance.Get<IConstConfig> ();
            _context = Locator.Instance.Get<IContext>();
            _msgParser = _context.GetMsgParser ();
        }

        public void Dispose()
        {

        }
    }
}