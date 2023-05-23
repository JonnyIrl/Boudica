using Boudica.MongoDB;
using Boudica.MongoDB.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Services
{
    public class NotificationService
    {
        private readonly IMongoDBContext _mongoDBContext;
        protected IMongoCollection<Notification> _notificationCollection;

        public NotificationService()
        {

        }

        public NotificationService(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
#if DEBUG
            _notificationCollection = _mongoDBContext.GetCollection<Notification>(typeof(Notification).Name + "Test");
#else
            _notificationCollection = _mongoDBContext.GetCollection<Notification>(typeof(Notification).Name);
#endif
        }

        public async Task<bool> CreateNotification(string announcementText, ulong channelId)
        {
            Notification newNotification = new Notification()
            {
                AnnouncementText = announcementText,
                ChannelIdToAnnounceIn = channelId,
                DateTimeAnnounced = DateTime.MinValue
            };
            await _notificationCollection.InsertOneAsync(newNotification);
            return newNotification.Id != ObjectId.Empty;
        }

        public async Task<List<Notification>> GetAllNotificationsToAnnounce()
        {
            return await _notificationCollection.Find(x => x.DateTimeAnnounced == DateTime.MinValue).ToListAsync();
        }

        public async Task<bool> MarkNotificationAsAnnounced(ObjectId id)
        {
            var update = Builders<Notification>.Update.Set(x => x.DateTimeAnnounced, DateTime.UtcNow);
            var result = await _notificationCollection.UpdateOneAsync(x => x.Id == id, update, new UpdateOptions() { IsUpsert = false });
            return result.IsAcknowledged;
        }
    }
}
