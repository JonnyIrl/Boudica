using Boudica.MongoDB;
using Boudica.MongoDB.Models;
using Discord;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Services
{
    public class HiringService
    {
        private readonly IMongoDBContext _mongoDBContext;
        protected IMongoCollection<Recruiter> _recruiterCollection;
        private const string CollectionName = "Recruiting";

        public HiringService(IMongoDBContext mongoDBContext, IServiceProvider services)
        {
            _mongoDBContext = mongoDBContext;
#if DEBUG
            _recruiterCollection = _mongoDBContext.GetCollection<Recruiter>(CollectionName);
#else
            _recruiterCollection = _mongoDBContext.GetCollection<Recruiter>(CollectionName);
#endif
        }

        public async Task<Recruiter> FindRecruit(ulong recruitUserId)
        {
            Recruiter existing = await _recruiterCollection.Find(x => x.Recruit.Id == recruitUserId).FirstOrDefaultAsync();
            if(existing != null)
            {
                return existing;
            }

            return null;
        }

        public async Task<List<Recruiter>> FindAllRecruits(ulong guildId)
        {
            return await _recruiterCollection.Find(x => x.GuildId == guildId && x.ProbationPassed == false).ToListAsync();
        }

        public async Task<Recruiter> InsertNewRecruit(IGuildUser recruiter, IGuildUser recruit)
        {
            if (recruiter == null || recruit == null) return null;
            Recruiter existing = await FindRecruit(recruit.Id);
            if(existing != null) return existing;

            Recruiter newRecruiter = new Recruiter(recruiter, recruit);
            await _recruiterCollection.InsertOneAsync(newRecruiter);
            return newRecruiter;
        }

        public async Task<bool> UpdateCreatedPost(ulong recruitId)
        {
            var updateBuilder = Builders<Recruiter>.Update;
            var updateResult = await _recruiterCollection.UpdateOneAsync(x => x.Recruit.Id == recruitId, updateBuilder.Set(x => x.Recruit.RecruitChecklist.CreatedPost, true), new UpdateOptions() { IsUpsert = false });
            return updateResult.ModifiedCount > 0;
        }

        public async Task<bool> UpdateJoinedPost(ulong recruitId, DateTime dateTimeLastJoined)
        {
            var updateBuilder = Builders<Recruiter>.Update;
            var updateResult = await _recruiterCollection.UpdateOneAsync(x => x.Recruit.Id == recruitId,             
                updateBuilder
                .Set(x => x.Recruit.RecruitChecklist.CreatedPost, true)
                .Set(x => x.Recruit.RecruitChecklist.DateTimeLastActivityJoined, dateTimeLastJoined)
                , new UpdateOptions() { IsUpsert = false });
            return updateResult.ModifiedCount > 0;
        }

        public async Task<bool> UpdateProbationPassed(Recruit recruit)
        {
            var updateBuilder = Builders<Recruiter>.Update;
            var updateResult = await _recruiterCollection.UpdateOneAsync(x => x.Recruit.Id == recruit.Id, updateBuilder.Set(x => x.ProbationPassed, true));
            return updateResult.ModifiedCount > 0;
        }

        public async Task<bool> UpdateRecruit(Recruit recruit)
        {
            var updateBuilder = Builders<Recruiter>.Update;
            var updateResult = await _recruiterCollection.UpdateOneAsync(x => x.Recruit.Id == recruit.Id, updateBuilder.Set(x => x.Recruit, recruit));
            return updateResult.ModifiedCount > 0;
        }

        public async Task<bool> DeleteRecruit(Recruit recruit)
        {
            var deleteResult = await _recruiterCollection.DeleteOneAsync(x => x.Recruit.Id == recruit.Id);
            return deleteResult.DeletedCount > 0;
        }

    }
}
