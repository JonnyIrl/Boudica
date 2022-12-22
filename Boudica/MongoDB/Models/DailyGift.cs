using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class DailyGift
    {
        [BsonId]
        public ulong Id { get; set; }
        public DateTime DateTimeLastGifted { get; set; }
    }
}
