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
        public ulong ChallengerId { get; set; }
        public string ChallengerName { get; set; }
        public ulong ContenderId { get; set; }
        public string ContenderName { get; set; }
        public Challenge ChallengeType { get; set; }
        public bool Accepted { get; set; }
        public ulong WinnerId { get; set; }
        public int Wager { get; set; }
        public DateTime ExpiredDateTime { get; set; }

        public UserChallenge(ulong challengerId, string challengerName, ulong contenderId, string contenderName, int wager, Challenge challenge)
        {
            ChallengerId = challengerId;
            ChallengerName = challengerName;
            ContenderId = contenderId;
            ContenderName = contenderName;
            Wager = wager;
            ChallengeType = challenge;
        }
    }
}
