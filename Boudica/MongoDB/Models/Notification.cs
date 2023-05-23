using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class Notification
    {
        public ObjectId Id { get; set; }
        public string AnnouncementText { get; set; }
        public ulong ChannelIdToAnnounceIn { get; set; }
        public DateTime DateTimeAnnounced { get; set; }
    }
}
