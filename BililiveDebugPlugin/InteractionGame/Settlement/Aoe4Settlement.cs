﻿using System;
using System.Collections.Generic;
using System.Linq;
using BililiveDebugPlugin.DB.Model;
using InteractionGame;
using UserData = InteractionGame.UserData;
using ProtoBuf;
using Utils;
using System.Security.Cryptography;
using BililiveDebugPlugin.InteractionGame.Data;
using System.Threading;

namespace BililiveDebugPlugin.InteractionGame.Settlement
{
    [ProtoContract]
    class RankMsg
    {
        [ProtoMember(1)]
        public string Title;
        [ProtoMember(2)]
        public int WinGroup;
        [ProtoMember(3)]
        public List<UserData> Items;
        [ProtoMember(4)]
        public List<DB.Model.UserData> HonorItems;
        [ProtoMember(5)]
        public List<DB.Model.UserData> ScoreItems;
    }
    public class Aoe4Settlement<IT> : ISettlement<IT>
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

        public void ShowSettlement(IT it,int winGroup)
        {
            var messageDispatcher = (it as DebugPlugin)?.messageDispatcher; 
            var sendMsg =  (it as DebugPlugin)?.SendMsg; 
            //PrintGameMsg($"{GetColorById(d.g)}方获胜！！!");
            if(winGroup > 0 && winGroup <= Aoe4DataConfig.GroupCount)
                messageDispatcher?.MsgParser.AddWinScore(winGroup, 300);
            var data = messageDispatcher?.MsgParser.GetSortedUserData();
            sendMsg?.SendMsg<object>((short)EMsgTy.ClearAllPlayer, null);
            PreSettlement(it,data);
            DB.DBMgr.Instance.OnSettlement(data, winGroup - 1,messageDispatcher.PlayerParser.GetLeastGroupList());
            //todo show settlement
            AfterSettlement();
            SendSettlement(sendMsg, data, winGroup - 1);
            //var bridge = messageDispatcher.GetBridge();
            //bridge.FroceOverrideCurrentMsg("Mod_Restart()");
            Locator.Instance.Get<DebugPlugin>().Log("Wait Send Message clean");
            sendMsg?.waitClean();
            Locator.Instance.Get<DebugPlugin>().Log("messageDispatcher clean");
            messageDispatcher.Clear();
            Locator.Instance.Get<DebugPlugin>().Log("Aoe4GameState clean");
            Locator.Instance.Get<Aoe4GameState>().OnClear();
            Locator.Instance.Get<DebugPlugin>().Log("Settlement end");
            ClickRestart();
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
                        if (Aoe4DataConfig.IsTestId(d.Id))
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

        private bool SingleGameScoreRankContain(List<SingleGameScoreData> rankList, string id, out int idx)
        {
            for(int i = 0;i < rankList.Count;++i)
            {
                if (rankList[i].id == id)
                {
                    idx = i;
                    return true;
                }
            }
            idx = -1;
            return false;
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

        public static void ClickRestart()
        {
            var gs = Locator.Instance.Get<Aoe4GameState>();
            var bridge = Locator.Instance.Get<DebugPlugin>().messageDispatcher.GetBridge();
            bridge.SendKeyEvent(0x1B);
            Thread.Sleep(1000);
            bridge.ClickLeftMouse(1920 / 2,(int)(1080 * 0.643f));
            Thread.Sleep(1000);
            bridge.ClickLeftMouse((int)(1920 * 0.492f) , (int)(1080 * 0.548f));
        }
        
        private void SendSettlement(SM_SendMsg sendMsg, List<UserData> data,int win)
        {
            var scoreList = DB.DBMgr.Instance.GetSortedUsersByScore(Max);
            PretreatmentScore(scoreList);
            TrySettlementScore(scoreList);
            var honorList = _tmpSingleGameScoreRank;
            PretreatmentScore(honorList);
            RankMsg rankMsg = new RankMsg()
            {
                Title = win >= 0 ? $"{DebugPlugin.GetColorById(win + 1)}方获胜" : "平局",
                Items = data.Take(Max).ToList(),
                ScoreItems = scoreList,
                HonorItems = honorList,
                WinGroup = win
            };
            sendMsg.SendMsg((short)EMsgTy.Settlement, rankMsg);
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

        private void PretreatmentScore(List<DB.Model.UserData> scoreList)
        {
            foreach (var it in scoreList)
            {
                it.Ext = 0;
            }
        }

        public void Restart()
        {
            ClickRestart();
        }
    }
}