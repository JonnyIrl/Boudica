using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class DramaReport
    {
        [BsonId]
        public int Id { get; set; }

        public DateTime Start { get; set; }
    }
}
