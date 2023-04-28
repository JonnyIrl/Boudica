using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class Guardian
    {
        [BsonId]
        public ulong Id { get; set; }
        public int Glimmer { get; set; }
        public int RankScore { get; set; }
        public string Username { get; set; }
        public string BungieMembershipId { get; set; } = "-1";
        public string BungieMembershipType { get; set; } = "-1";
        public string UniqueBungieName { get; set; } = "Guardian#0000";
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime AccessExpiration { get; set; } = DateTime.Now;
        public DateTime RefreshExpiration { get; set; } = DateTime.Now;
        public List<GuardianCharacter> GuardianCharacters = new List<GuardianCharacter>();
    }
}
