using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Boudica.MongoDB;
using Boudica.MongoDB.Models;
using Discord;

namespace Boudica.Services
{
    public class ActivityService
    {
        private readonly IMongoDBContext _mongoDBContext;
        protected IMongoCollection<Raid> _raidCollection;
        protected IMongoCollection<MongoDB.Models.Fireteam> _fireteamCollection;

        public ActivityService(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
            _raidCollection = _mongoDBContext.GetCollection<MongoDB.Models.Raid>(typeof(MongoDB.Models.Raid).Name);
            _fireteamCollection = _mongoDBContext.GetCollection<MongoDB.Models.Fireteam>(typeof(MongoDB.Models.Fireteam).Name);
        }

        #region MongoDB Raid
        public async Task<MongoDB.Models.Raid> CreateRaidAsync(MongoDB.Models.Raid raid)
        {
            if (raid.CreatedByUserId <= 0) throw new ArgumentNullException("CreatedByUserId must be provided");
            if (raid.ChannelId <= 0) throw new ArgumentNullException("ChannelId must be provided");

            int nextId = await _mongoDBContext.GetNextId(_raidCollection);
            if (nextId > 0)
            {
                raid.Id = nextId;
                await _raidCollection.InsertOneAsync(raid);
            }
          
            return await Task.FromResult(raid);
        }

        public async Task<MongoDB.Models.Raid> UpdateRaidAsync(MongoDB.Models.Raid raid)
        {
            if (raid.Id <= 0) throw new ArgumentNullException("Id must be provided to update");
            var builder = Builders<MongoDB.Models.Raid>.Filter;
            var updateBuilder = Builders<MongoDB.Models.Raid>.Update;
            var filter = builder.Eq(x => x.Id, raid.Id);
            return await _raidCollection.FindOneAndReplaceAsync(filter, raid, new FindOneAndReplaceOptions<MongoDB.Models.Raid, MongoDB.Models.Raid>() { ReturnDocument = ReturnDocument.After });
        }

        public async Task<MongoDB.Models.Raid> GetMongoRaidAsync(int raidId)
        {
            if (raidId <= 0) throw new ArgumentNullException("Id must be provided to update");
            return await (await _raidCollection.FindAsync(x => x.Id == raidId)).FirstOrDefaultAsync();
        }
        public async Task<IList<Raid>> FindAllOpenRaids()
        {
            return await (await _raidCollection.FindAsync(x => x.DateTimeClosed == DateTime.MinValue)).ToListAsync();
        }

        public async Task<List<Raid>> FindAllClosedRaids()
        {
            return await (await _raidCollection.FindAsync(x => x.DateTimeClosed != DateTime.MinValue)).ToListAsync();
        }
        #endregion

        #region Mongo Fireteam
        public async Task<MongoDB.Models.Fireteam> CreateFireteamAsync(MongoDB.Models.Fireteam fireteam)
        {
            if (fireteam.CreatedByUserId <= 0) throw new ArgumentNullException("CreatedByUserId must be provided");
            if (fireteam.ChannelId <= 0) throw new ArgumentNullException("ChannelId must be provided");

            int nextId = await _mongoDBContext.GetNextId(_fireteamCollection);
            if (nextId > 0)
            {
                fireteam.Id = nextId;
                await _fireteamCollection.InsertOneAsync(fireteam);
            }

            return await Task.FromResult(fireteam);
        }

        public async Task<MongoDB.Models.Fireteam> UpdateFireteamAsync(MongoDB.Models.Fireteam fireteam)
        {
            if (fireteam.Id <= 0) throw new ArgumentNullException("Id must be provided to update");
            var builder = Builders<MongoDB.Models.Fireteam>.Filter;
            var updateBuilder = Builders<MongoDB.Models.Fireteam>.Update;
            var filter = builder.Eq(x => x.Id, fireteam.Id);
            return await _fireteamCollection.FindOneAndReplaceAsync(filter, fireteam, new FindOneAndReplaceOptions<MongoDB.Models.Fireteam, MongoDB.Models.Fireteam>() { ReturnDocument = ReturnDocument.After });
        }

        public async Task<MongoDB.Models.Fireteam> GetMongoFireteamAsync(int fireteamId)
        {
            if (fireteamId <= 0) throw new ArgumentNullException("Id must be provided to update");
            return await (await _fireteamCollection.FindAsync(x => x.Id == fireteamId)).FirstOrDefaultAsync();
        }
        public async Task<IList<Fireteam>> FindAllOpenFireteams()
        {
            return await (await _fireteamCollection.FindAsync(x => x.DateTimeClosed == DateTime.MinValue)).ToListAsync();
        }
        #endregion
    }

}
