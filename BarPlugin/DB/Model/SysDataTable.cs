namespace BililiveDebugPlugin.DB.Model
{
    public enum ESysDataTy:long
    {
        None = 0,
        ScoreRankSettlementTime = 1,
        SeasonTime = 2,
        SingleScoreRank = 3,
        MonthlyCumulativeScoreRank = 4,
        MonthlyCumulativeKillRank = 5,
        MonthlySingleScoreRank = 6,
        MonthlySingleKillRank = 7,

        MonthlyCumulativeScoreRankExpiredTime = 8,
        MonthlyCumulativeKillRankExpiredTime = 9,
        MonthlySingleScoreRankExpiredTime = 10,
        MonthlySingleKillRankExpiredTime = 11,
    }
}