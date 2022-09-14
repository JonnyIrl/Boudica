using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class Raid
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public ulong CreatedByUserId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public byte MaxPlayerCount { get; set; }
        public DateTime DateTimeCreated { get; set; }
        public DateTime DateTimeClosed { get; set; }
        public List<ActivityUser> Players { get; set; }
        public List<ActivityUser> Substitutes { get; set; }

    }
}
