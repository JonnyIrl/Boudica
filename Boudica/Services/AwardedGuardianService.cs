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
    public  class AwardedGuardianService
    {
        private readonly IMongoDBContext _mongoDBContext;
        protected IMongoCollection<AwardedGuardians> _awardedGuardiansCollection;

        public AwardedGuardianService(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
            _awardedGuardiansCollection = _mongoDBContext.GetCollection<AwardedGuardians>(typeof(AwardedGuardians).Name);
        }

        public async Task<bool> CanAwardGlimmerToGuardian(ulong targetGuardianId)
        {
            AwardedGuardians result = await (await _awardedGuardiansCollection.FindAsync(x => x.AwardedGuardiansId == targetGuardianId)).FirstOrDefaultAsync();
            if (result == null) return true;
            //Only award person once per day
            if (result.DateTimeLastAwarded.Date == DateTime.UtcNow.Date) return false;
            return true;
        }

        public async Task<bool> AwardGuardian(ulong userId, ulong awardedGuardianId)
        {
            if (userId <= 0 || awardedGuardianId <= 0) throw new ArgumentNullException("Id must be provided to update");
            var builder = Builders<AwardedGuardians>.Filter;
            var updateBuilder = Builders<AwardedGuardians>.Update;
            var filter = builder.Eq(x => x.Id, userId);

            Console.WriteLine($"Awarding Glimmer from {userId} to {awardedGuardianId}");
            UpdateResult result = await _awardedGuardiansCollection.UpdateOneAsync(filter, 
                updateBuilder.Set("Id", userId)
                .Set("AwardedGuardiansId", awardedGuardianId)
                .Set("DateTimeLastAwarded", DateTime.UtcNow), new UpdateOptions() { IsUpsert = true });
            return result.IsAcknowledged;
        }

    }
}
