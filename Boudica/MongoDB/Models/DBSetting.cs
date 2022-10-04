using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class DBSetting
    {
        [BsonId]
        public string Id { get; set; }
        public string Value { get; set; }
    }
}
