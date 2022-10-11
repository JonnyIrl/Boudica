using Discord;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class Recruit
    {
        [BsonId]
        public ulong Id { get; set; }
        public ulong GuildId { get;set; }
        public string DisplayName { get; set; }
        public RecruitChecklist RecruitChecklist { get; set; }
        public bool AwardedGlimmer { get; set; }

        public Recruit(IGuildUser user)
        {
            Id = user.Id;
            DisplayName = user.DisplayName;
            GuildId = user.GuildId;
            RecruitChecklist = new RecruitChecklist();
        }
    }
}
