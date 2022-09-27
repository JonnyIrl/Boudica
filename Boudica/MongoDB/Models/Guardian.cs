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
    }
}
