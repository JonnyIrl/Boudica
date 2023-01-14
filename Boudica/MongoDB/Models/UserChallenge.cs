using Boudica.Enums;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class UserChallenge
    {
        [BsonId]
        public Guid Id { get; set; }
        public long SessionId { get; set; }
        public UserChallengeUser Challenger { get; set; }
        public UserChallengeUser Contender { get; set; }
        public Challenge ChallengeType { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public bool Accepted { get; set; }
        public ulong? WinnerId { get; set; }
        public int Wager { get; set; }
        public DateTime ExpiredDateTime { get; set; }

        public UserChallenge(ulong challengerId, string challengerName, ulong contenderId, string contenderName, int wager, Challenge challenge)
        {
            Challenger = new UserChallengeUser()
            {
                UserId = challengerId,
                UserName = challengerName,
                Answer = null
            };
            Contender = new UserChallengeUser()
            {
                UserId = contenderId,
                UserName = contenderName,
                Answer = null
            };
            Wager = wager;
            ChallengeType = challenge;
        }
    }
}
