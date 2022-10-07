using Discord;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class TrialsVote
    {
        [BsonId]
        public Guid Id { get; set; }
        public DateTime WeeklyVoteDate { get; set; }
        public List<PlayerVote> PlayerVotes { get; set; }
        public string ConfirmedMap { get; set; }
        public bool IsLocked { get; set; }
        public DateTime DateTimeLocked { get; set; }
        public ulong MessageId { get; set; }
    }
}
