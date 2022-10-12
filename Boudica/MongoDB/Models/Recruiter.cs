using Discord;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class Recruiter
    {
        [BsonId]
        public Guid Id { get; set; }
        public ulong UserId { get; set; }
        public string DisplayName { get; set; }
        public ulong GuildId { get; set; }
        public Recruit Recruit { get; set; }
        public DateTime RecruitedDateTime { get; set; }
        public bool ProbationPassed { get; set; }

        public Recruiter(IGuildUser recruiter, IGuildUser recruit)
        {
            UserId = recruiter.Id;
            DisplayName = recruiter.DisplayName;
            GuildId = recruiter.GuildId;
            Recruit = new Recruit(recruit);
            RecruitedDateTime = DateTime.UtcNow;
        }
    }
}
