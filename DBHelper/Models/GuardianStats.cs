using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBHelper.Models
{
    public class GuardianStats
    {
        public ulong Id { get; set; }
        public string Username { get; set; }
        public int CompletedRaidCount { get; set; } = 0;
        public int FailedRaidCount { get; set; } = 0;
        public int CompletedFireteamCount { get; set; } = 0;
        public int FailedFireteamCount { get; set; } = 0;
        public int InsultIssuedCount { get; set; } = 0;
        public int InsultReceivedCount { get; set; } = 0;
        public int AwardsIssuedCount { get; set; } = 0;
        public int AwardsReceivedCount { get; set; } = 0;
        public int SuperSubCount { get; set; } = 0;
        public int ComplimentIssuedCount { get; set; } = 0;
        public int ComplimentReceivedCount { get; set; } = 0;
        public int DailyGiftCount { get; set; } = 0;
        public int TrialsVoteCount { get; set; } = 0;
        public int HigherOrLowerCount { get; set; } = 0;
        public int AddedToActivityCount { get; set; } = 0;
    }
}
