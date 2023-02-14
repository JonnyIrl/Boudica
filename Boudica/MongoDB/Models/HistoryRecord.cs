using Boudica.Enums;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class HistoryRecord
    {
        [BsonId]
        public Guid Id { get; set; }
        public ulong UserId { get; set; }
        public ulong? TargetUserId { get; set; }
        public HistoryType HistoryType { get; set; }
        public DateTime DateTimeInserted { get; set; }
        public int? Amount { get; set; }
    }
}
