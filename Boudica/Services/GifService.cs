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
    public class GifService
    {
        private readonly IMongoDBContext _mongoDBContext;
        protected IMongoCollection<SpamGif> _spamGifCollection;
        public GifService(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
            string name = typeof(SpamGif).Name;
            _spamGifCollection = _mongoDBContext.GetCollection<SpamGif>(name);
        }

        public async Task<SpamGif> Get(ulong userId)
        {
            SpamGif result = await (await _spamGifCollection.FindAsync(x => x.Id == userId)).FirstOrDefaultAsync();
            return result;
        }

        public async Task<bool> UpsertUsersSpamGif(ulong userId, string displayName)
        {
            var builder = Builders<SpamGif>.Filter;
            var updateBuilder = Builders<SpamGif>.Update;
            var filter = builder.Eq(x => x.Id, userId);
            var update = updateBuilder.Set(x => x.DateTimeLastUsed, DateTime.UtcNow).Set(x => x.DisplayName, displayName);
            var result = await _spamGifCollection.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<SpamGif, SpamGif>() { IsUpsert = true, ReturnDocument = ReturnDocument.After });
            return result != null;
        }
    }
}
