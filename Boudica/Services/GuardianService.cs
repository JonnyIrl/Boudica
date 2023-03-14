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

        public async Task<List<Guardian>> GetLeaderboard(ulong userId)
        {
            var builder = Builders<Guardian>.Filter;
            var sort = Builders<Guardian>.Sort.Descending(x => x.Glimmer);
            var filter = builder.Gt(x => x.Id, 0);
            var leaderboardList = await _guardianCollection.Find(x => x.Id > 0, new FindOptions()).Sort(sort).Limit(10).ToListAsync();
            //If Top 10 contains the person who issued the command
            if (leaderboardList.FirstOrDefault(x => x.Id == userId) != null) 
                return leaderboardList;

            Guardian issuedCommandGuardian = await (await _guardianCollection.FindAsync(x => x.Id == userId)).FirstOrDefaultAsync();
            if(issuedCommandGuardian == null)
            {
                issuedCommandGuardian = new Guardian() {  Glimmer = 0, Id = userId };
                await _guardianCollection.InsertOneAsync(issuedCommandGuardian);
            }

            leaderboardList.Add(issuedCommandGuardian);
            return leaderboardList;
        }

        public async Task<List<Guardian>> GetFullLeaderboard(ulong userId)
        {
            var builder = Builders<Guardian>.Filter;
            var sort = Builders<Guardian>.Sort.Descending(x => x.Glimmer);
            var filter = builder.Gt(x => x.Id, 0);
            var leaderboardList = await _guardianCollection.Find(x => x.Id > 0, new FindOptions()).Sort(sort).ToListAsync();
            //If Top 10 contains the person who issued the command
            if (leaderboardList.FirstOrDefault(x => x.Id == userId) != null)
                return leaderboardList;

            Guardian issuedCommandGuardian = await (await _guardianCollection.FindAsync(x => x.Id == userId)).FirstOrDefaultAsync();
            if (issuedCommandGuardian == null)
            {
                issuedCommandGuardian = new Guardian() { Glimmer = 0, Id = userId };
                await _guardianCollection.InsertOneAsync(issuedCommandGuardian);
            }

            leaderboardList.Add(issuedCommandGuardian);
            return leaderboardList;
        }

        public async Task<bool> IncreaseGlimmerAsync(ulong userId, string username, int count)
        {
            if (userId <= 0) throw new ArgumentNullException("Id must be provided to update");
            var builder = Builders<Guardian>.Filter;
            var updateBuilder = Builders<Guardian>.Update;
            var filter = builder.Eq(x => x.Id, userId);
            Console.WriteLine($"Increasing Glimmer for {userId} by {count}");
#if DEBUG
            return true;
#else
            UpdateResult result = await _guardianCollection.UpdateOneAsync(filter, 
                updateBuilder.SetOnInsert("Id", userId)
                .Set("Username", username)
                .Inc("Glimmer", count), new UpdateOptions() { IsUpsert = true });
            return result.IsAcknowledged;
#endif
        }

        public async Task<bool> RemoveGlimmerAsync(ulong userId, int count)
        {
            if (userId <= 0) throw new ArgumentNullException("Id must be provided to update");
            var builder = Builders<Guardian>.Filter;
            var updateBuilder = Builders<Guardian>.Update;
            var filter = builder.Eq(x => x.Id, userId);
            Console.WriteLine($"Decreasing Glimmer for {userId} by {count}");
#if DEBUG
            return true;
#else
            Guardian existingGuardian = await _guardianCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();
            UpdateResult result;
            if(count <= 0)
            {
                count = count * -1;
            }

            if (existingGuardian == null || (existingGuardian.Glimmer - count) < 0)
            {
                result = await _guardianCollection.UpdateOneAsync(filter, updateBuilder.SetOnInsert("Id", userId).Set("Glimmer", 0), new UpdateOptions() { IsUpsert = true });
            }
            else
                result = await _guardianCollection.UpdateOneAsync(filter, updateBuilder.SetOnInsert("Id", userId).Inc("Glimmer", count * -1), new UpdateOptions() { IsUpsert = true });
            return result.IsAcknowledged;
#endif
        }

        public async Task<bool> ResetAllGlimmer()
        {
            //var updateBuilder = Builders<Guardian>.Update;
            //UpdateResult result = await _guardianCollection.UpdateManyAsync(x => x.Id > 0, updateBuilder.Set("Glimmer", 0));
            //return result.IsAcknowledged;
            return true;
        }

        public async Task CreateGuardian(ulong userId, string displayName)
        {
            await IncreaseGlimmerAsync(userId, displayName, 0);
        }

        public async Task<Guardian> GetGuardian(ulong userId)
        {
            return await _guardianCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();
        }

        public async Task<int> GetGuardianGlimmer(ulong userId)
        {
            Guardian guardian = await _guardianCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();
            if (guardian == null) return 0;
            return guardian.Glimmer;
        }

        public async Task<Guardian> InsertGuardian(Guardian guardian)
        {
            try
            {
                await _guardianCollection.InsertOneAsync(guardian);
                return guardian;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to insert guardian", ex);
                return null;
            }
        }

        public async Task<Guardian> UpdateGuardian(Guardian guardian)
        {
            if (guardian.GuardianCharacters == null) guardian.GuardianCharacters = new List<GuardianCharacter>();
            return  await _guardianCollection.FindOneAndUpdateAsync(x => x.Id == guardian.Id, new UpdateDefinitionBuilder<Guardian>()
                .Set(x => x.UniqueBungieName, guardian.UniqueBungieName)
                .Set(x => x.BungieMembershipId, guardian.BungieMembershipId)
                .Set(x => x.BungieMembershipType, guardian.BungieMembershipType)
                .Set(x => x.RefreshToken, guardian.RefreshToken)
                .Set(x => x.RefreshExpiration, guardian.RefreshExpiration)
                .Set(x => x.AccessToken, guardian.AccessToken)
                .Set(x => x.AccessExpiration, guardian.AccessExpiration)
                .Set(x => x.GuardianCharacters, guardian.GuardianCharacters)
                );
        }

        public async Task<bool> UpdateGuardianTokens(ulong userId, string accessToken, string refreshToken, DateTime accessExpiration, DateTime tokenRefresh)
        {
            var builder = Builders<Guardian>.Filter;
            var updateBuilder = Builders<Guardian>.Update;
            var filter = builder.Eq(x => x.Id, userId);
            var result = await _guardianCollection.UpdateOneAsync(filter, 
                updateBuilder
                .Set(x => x.AccessToken, accessToken)
                .Set(x => x.RefreshToken, refreshToken)
                .Set(x => x.AccessExpiration, accessExpiration)
                .Set(x => x.RefreshToken, refreshToken)
                , new UpdateOptions() { IsUpsert = true });

            return result.ModifiedCount > 0;
        }
    }
}
