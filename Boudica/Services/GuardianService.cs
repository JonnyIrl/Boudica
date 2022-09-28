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
    public class GuardianService
    {
        private readonly IMongoDBContext _mongoDBContext;
        protected IMongoCollection<Guardian> _guardianCollection;
        public GuardianService(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
            string name = typeof(Guardian).Name;
            _guardianCollection = _mongoDBContext.GetCollection<Guardian>(name);
        }

        public async Task<List<Guardian>> GetLeaderboard()
        {
            var builder = Builders<Guardian>.Filter;
            var sort = Builders<Guardian>.Sort.Descending(x => x.Glimmer);
            var filter = builder.Gt(x => x.Id, 0);
            return await _guardianCollection.Find(x => x.Id > 0, new FindOptions()).Sort(sort).Limit(10).ToListAsync();
        }

        public async Task<bool> IncreaseGlimmerAsync(ulong userId, int count)
        {
            if (userId <= 0) throw new ArgumentNullException("Id must be provided to update");
            var builder = Builders<Guardian>.Filter;
            var updateBuilder = Builders<Guardian>.Update;
            var filter = builder.Eq(x => x.Id, userId);
            Console.WriteLine($"Increasing Glimmer for {userId} by {count}");
            UpdateResult result = await _guardianCollection.UpdateOneAsync(filter, updateBuilder.SetOnInsert("Id", userId).Inc("Glimmer", count), new UpdateOptions() { IsUpsert = true });
            return result.IsAcknowledged;
        }

        public async Task<bool> RemoveGlimmerAsync(ulong userId, int count)
        {
            if (userId <= 0) throw new ArgumentNullException("Id must be provided to update");
            var builder = Builders<Guardian>.Filter;
            var updateBuilder = Builders<Guardian>.Update;
            var filter = builder.Eq(x => x.Id, userId);
            Console.WriteLine($"Decreasing Glimmer for {userId} by {count}");
            UpdateResult result = await _guardianCollection.UpdateOneAsync(filter, updateBuilder.SetOnInsert("Id", userId).Inc("Glimmer", count * -1), new UpdateOptions() { IsUpsert = true });
            return result.IsAcknowledged;
        }

        public async Task<bool> ResetAllGlimmer()
        {
            var updateBuilder = Builders<Guardian>.Update;
            UpdateResult result = await _guardianCollection.UpdateManyAsync(x => x.Id > 0, updateBuilder.Set("Glimmer", 0));
            return result.IsAcknowledged;
        }
    }
}
