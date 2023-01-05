using Boudica.MongoDB;
using Boudica.MongoDB.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Services
{
    public class DailyGiftService
    {
        private readonly IMongoDBContext _mongoDBContext;
        protected IMongoCollection<DailyGift> _dailyGiftCollection;
        public DailyGiftService(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
#if DEBUG
            string name = typeof(DailyGift).Name + "Test";
#else
            string name = typeof(DailyGift).Name;
#endif
            _dailyGiftCollection = _mongoDBContext.GetCollection<DailyGift>(name);
        }

        public async Task<DailyGift> Get(ulong userId)
        {
            return await _dailyGiftCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();
        }

        public async Task<bool> UpsertUsersDailyGift(ulong userId)
        {
            var builder = Builders<DailyGift>.Filter;
            var updateBuilder = Builders<DailyGift>.Update;
            var filter = builder.Eq(x => x.Id, userId);
            var update = updateBuilder.Set(x => x.DateTimeLastGifted, DateTime.UtcNow);
            var result = await _dailyGiftCollection.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<DailyGift, DailyGift>() { IsUpsert = true, ReturnDocument = ReturnDocument.After });
            return result != null;
        }
    }
}
