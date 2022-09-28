using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class ActivityUser
    {
        public ulong UserId { get; set; }
        public string DisplayName { get; set; }
        public DateTime DateTimeJoined { get; set; }
        public bool Reacted { get; set; }

        public ActivityUser(ulong userId, string displayName, bool reacted = false)
        {
            UserId = userId;
            DisplayName = displayName;
            DateTimeJoined = DateTime.UtcNow;
            Reacted = reacted;
        }
    }
}
