using Boudica.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class PlayerPollVote
    {
        public ulong Id { get; set; }
        public string Username { get; set; }
        public PollOption VotedPollOption { get; set; }
        public DateTime DateTimeVoted { get; set; }
        public int BetAmount { get; set; }
        public PlayerPollVote(ulong userId, string username, PollOption option, int betAmount = 0)
        {
            Id = userId;
            Username = username;
            VotedPollOption = option;
            DateTimeVoted = DateTime.UtcNow;
            BetAmount = betAmount;
        }
    }
}
