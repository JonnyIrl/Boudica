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
        public PollOption WinningOption { get; set; }
    }
}
