using System;
using System.Collections.Generic;

namespace WordGame.Network.Models
{
    [Serializable]
    public class RankingData
    {
        public List<RankingEntry> ranking;
        public RankingEntry userRank;
    }

    [Serializable]
    public class RankingEntry
    {
        public string userId;
        public string username;
        public string displayName;
        public string avatarUrl;
        public int totalXP;
        public int totalScore;
        public int gamesPlayed;
        public int gamesWon;
        public int rank;
    }
}
