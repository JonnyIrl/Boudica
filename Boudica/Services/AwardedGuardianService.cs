using Boudica.MongoDB;
using Boudica.MongoDB.Models;
using Microsoft.Extensions.DependencyInjection;
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
        private SettingsService _settingsService;
        private GuardianService _guardianService;
        private readonly int AwardedGlimmerAmount = 3;

        public AwardedGuardianService(IMongoDBContext mongoDBContext, IServiceProvider services)
        {
            _mongoDBContext = mongoDBContext;
            _awardedGuardiansCollection = _mongoDBContext.GetCollection<AwardedGuardians>(typeof(AwardedGuardians).Name);
            _settingsService = services.GetRequiredService<SettingsService>();
            _guardianService = services.GetRequiredService<GuardianService>();

            int parsedDbAmount = _settingsService.GetValueNumber("AwardedGlimmerAmount").Result;
            if (parsedDbAmount > 0) AwardedGlimmerAmount = parsedDbAmount;
        }

        public async Task<Tuple<bool, string>> CanAwardGlimmerToGuardian(ulong userId, ulong targetGuardianId)
        {
            AwardedGuardians userAwardedResult = await (await _awardedGuardiansCollection.FindAsync(x => x.Id == userId)).FirstOrDefaultAsync();
            //Make sure person only awards once per day
            if(userAwardedResult != null && userAwardedResult.DateTimeLastAwarded.Date == DateTime.UtcNow.Date)
                return new Tuple<bool, string>(false, "You can only award once per day");
            List<AwardedGuardians> results = await (await _awardedGuardiansCollection.FindAsync(x => x.AwardedGuardiansId == targetGuardianId)).ToListAsync();
            if (results.Any() == false) 
                return new Tuple<bool, string>(true, string.Empty);
            //Only award person once per day.
            if (results.Any(x => x.DateTimeLastAwarded.Date == DateTime.UtcNow.Date)) 
                return new Tuple<bool, string>(false, "This player has already been awarded glimmer today");
            //Can't award same person twice in a row.
            if (userAwardedResult != null && userAwardedResult.AwardedGuardiansId == targetGuardianId) 
                return new Tuple<bool, string>(false, "You cannot award the same player glimmer for 2 consecutive days");
            return new Tuple<bool, string>(true, string.Empty);
        }

        public async Task<bool> AwardGuardian(ulong userId, ulong awardedGuardianId, string userName, int multiplier = 1)
        {
            if (userId <= 0 || awardedGuardianId <= 0) throw new ArgumentNullException("Id must be provided to update");
            var builder = Builders<AwardedGuardians>.Filter;
            var updateBuilder = Builders<AwardedGuardians>.Update;
            var filter = builder.Eq(x => x.Id, userId);

            Console.WriteLine($"Awarding {AwardedGlimmerAmount} Glimmer from {userId} to {awardedGuardianId}");
            UpdateResult result = await _awardedGuardiansCollection.UpdateOneAsync(filter, 
                updateBuilder
                .Set("Id", userId)
                .Set("AwardedGuardiansId", awardedGuardianId)
                .Set("DateTimeLastAwarded", DateTime.UtcNow), 
                new UpdateOptions() { IsUpsert = true });
            bool success = result.IsAcknowledged;
            if (!success) return false;

            success = await _guardianService.IncreaseGlimmerAsync(awardedGuardianId, userName, AwardedGlimmerAmount * multiplier);
            return success;
        }

    }
}
