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

        public async Task<Recruiter> InsertNewRecruit(IGuildUser recruiter, IGuildUser recruit)
        {
            if (recruiter == null || recruit == null) return null;
            Recruiter existing = await FindRecruit(recruit.Id);
            if(existing != null) return existing;

            Recruiter newRecruiter = new Recruiter(recruiter, recruit);
            await _recruiterCollection.InsertOneAsync(newRecruiter);
            return newRecruiter;
        }

        public async Task<bool> UpdateRecruit(Recruit recruit)
        {
            var updateBuilder = Builders<Recruiter>.Update;
            var updateResult = await _recruiterCollection.UpdateOneAsync(x => x.Recruit.Id == recruit.Id, updateBuilder.Set(x => x.Recruit, recruit));
            return updateResult.ModifiedCount > 0;
        }

    }
}
