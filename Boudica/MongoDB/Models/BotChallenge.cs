using Boudica.Enums;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class BotChallenge
    {
        [BsonId]
        public Guid Id { get; set; }
        public long SessionId { get; set; }
        public UserChallengeUser Challenger { get; set; }
        public BotChallenges ChallengeType { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public bool Accepted { get; set; }
        public ulong? WinnerId { get; set; }
        public int Wager { get; set; }
        public DateTime ExpiryDateTime { get; set; }
        public List<BotRound> Rounds { get; set; }
        public int CurrentRound { get; set; }
        public bool IsClosed { get; set; }

        public BotChallenge(ulong challengerId, string challengerName, int wager, BotChallenges challenge)
        {
            Challenger = new UserChallengeUser()
            {
                UserId = challengerId,
                UserName = challengerName,
                Answer = null
            };
            Wager = wager;
            ChallengeType = challenge;
            CurrentRound = 0;
            Rounds = new List<BotRound>()
            {
                new BotRound(RoundNumber.FirstRound),
                new BotRound(RoundNumber.SecondRound),
                new BotRound(RoundNumber.FinalRound),
                new BotRound(RoundNumber.GameOverRound)
            };
        }
    }
}
