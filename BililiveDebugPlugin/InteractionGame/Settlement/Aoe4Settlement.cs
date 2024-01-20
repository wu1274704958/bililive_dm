using System;
using System.Collections.Generic;
using System.Linq;
using BililiveDebugPlugin.DB.Model;
using InteractionGame;
using UserData = InteractionGame.UserData;
using ProtoBuf;
using Utils;
using System.Security.Cryptography;
using BililiveDebugPlugin.InteractionGame.Data;

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
        public void ShowSettlement(IT it,int winGroup)
        {
            var messageDispatcher = (it as DebugPlugin)?.messageDispatcher; 
            var sendMsg =  (it as DebugPlugin)?.SendMsg; 
            //PrintGameMsg($"{GetColorById(d.g)}方获胜！！!");
            if(winGroup > 0 && winGroup <= Aoe4DataConfig.GroupCount)
                messageDispatcher?.MsgParser.AddWinScore(winGroup, 300);
            var data = messageDispatcher?.MsgParser.GetSortedUserData();
            sendMsg?.SendMsg<object>((short)EMsgTy.ClearAllPlayer, null);
            DB.DBMgr.Instance.OnSettlement(data, winGroup - 1,messageDispatcher.PlayerParser.GetLeastGroupList());
            //todo show settlement
            SendSettlement(sendMsg, data, winGroup - 1);
            Locator.Instance.Get<DebugPlugin>().Log("Wait Send Message clean");
            sendMsg?.waitClean();
            messageDispatcher.Clear();
            Locator.Instance.Get<Aoe4GameState>().OnClear();
        }
        
        private void SendSettlement(SM_SendMsg sendMsg, List<UserData> data,int win)
        {
            var scoreList = DB.DBMgr.Instance.GetSortedUsersByScore();
            PretreatmentScore(scoreList);
            TrySettlementScore(scoreList);
            var honorList = DB.DBMgr.Instance.GetSortedUsersByHonor();
            PretreatmentScore(honorList);
            RankMsg rankMsg = new RankMsg()
            {
                Title = win >= 0 ? $"{DebugPlugin.GetColorById(win + 1)}方获胜" : "平局",
                Items = data.Take(10).ToList(),
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
    }
}