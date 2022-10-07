using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class SpamGif
    {
        [BsonId]
        public ulong Id { get; set; }
        public string DisplayName { get; set; }
        public DateTime DateTimeLastUsed { get; set; }
    }
}
