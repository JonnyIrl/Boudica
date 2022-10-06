using Discord;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class CronTask
    {
        [BsonId]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime DateTimeLastTriggered { get; set; }
        public DateTime TriggerDateTime { get; set; }
        public CronRecurringAttribute RecurringAttribute { get; set; }
        public ulong GuidId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong LastMessageId { get; set; }
        public CronEmbedAttributes EmbedAttributes { get; set; }
    }

    public class CronRecurringAttribute
    {
        public DayOfWeek DayOfWeek { get; set; }
        public bool RecurringWeekly;
        public bool RecurringDaily;
    }
}
