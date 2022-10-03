using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class AwardedGuardians
    {
        [BsonId]
        public ulong Id { get; set; }
        public ulong AwardedGuardiansId { get; set; }
        public DateTime DateTimeLastAwarded { get; set; }
    }
}
