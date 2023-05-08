using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class UnsubscribeReminder
    {
        [BsonId]
        public ulong GuardianId { get; set; }
        public DateTime DateTimeUnsubscribed { get; set; }
    }
}
