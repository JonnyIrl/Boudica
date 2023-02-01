using Boudica.Enums;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class Poll
    {
        [BsonId]
        public long Id { get; set; }
        public string Question { get; set; }
        public List<PlayerPollVote> Votes { get; set; }
        public List<CreatedPollOption> CreatedOptions { get; set; }
        public List<PollOption> WinningOptions { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public bool IsClosed { get; set; }
        public int EntryGlimmerAmount { get; set; }
        public int WinnerGlimmerAmount { get; set; }
        public DateTime DateTimeCreated { get; set; }
        public DateTime DateTimeClosed { get; set; }
    }
}
