using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB
{
    public interface IRecordId
    {
        [BsonId]
        int Id { get; set; }
    }
}
