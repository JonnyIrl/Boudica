using Boudica.Database;
using Boudica.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Boudica.MongoDB;

namespace Boudica.Services
{
    public class ActivityService
    {
        private readonly DVSContext _db;
        private readonly IMongoDBContext _mongoDBContext;
        protected IMongoCollection<MongoDB.Models.Raid> _raidCollection;

        public ActivityService(DVSContext database, IMongoDBContext mongoDBContext)
        {
            _db = database;
            _mongoDBContext = mongoDBContext;
            string name = typeof(MongoDB.Models.Raid).Name;
            _raidCollection = _mongoDBContext.GetCollection<MongoDB.Models.Raid>(name);
        }

        #region SQL Raid
        public async Task<Raid> CreateRaidAsync(Raid raid)
        {
            if (string.IsNullOrEmpty(raid.CreatedByUserId)) throw new ArgumentNullException("CreatedByUserId must be provided");
            if (string.IsNullOrEmpty(raid.ChannelId)) throw new ArgumentNullException("ChannelId must be provided");

            await _db.Raids.AddAsync(raid);
            await _db.SaveChangesAsync(true);
            return await Task.FromResult(raid);
        }

        public async Task<Raid> UpdateRaidAsync(Raid raid)
        {
            if(raid.Id <= 0) throw new ArgumentNullException("Id must be provided to update");
            if (string.IsNullOrEmpty(raid.CreatedByUserId)) throw new ArgumentNullException("CreatedByUserId must be provided");
            if (string.IsNullOrEmpty(raid.MessageId)) throw new ArgumentNullException("MessageId must be provided");
            if (string.IsNullOrEmpty(raid.ChannelId)) throw new ArgumentNullException("ChannelId must be provided");

            _db.Raids.Update(raid);
            await _db.SaveChangesAsync(true);
            return await Task.FromResult(raid);
        }

        public async Task<Raid> GetRaidAsync(int raidId)
        {
            if (raidId <= 0) throw new ArgumentNullException("Id must be provided to update");

            Raid existingRaid = await _db.Raids.FirstOrDefaultAsync(x => x.Id == raidId);
            return await Task.FromResult(existingRaid);
        }

        public async Task<IList<Raid>> FindAllOpenRaids()
        {
            return await _db.Raids.Where(x => x.DateTimeClosed == null).ToListAsync();
        }
        #endregion

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

        //public async Task<IList<MongoDB.Models.Raid>> FindAllOpenRaids()
        //{
        //    return await _db.Raids.Where(x => x.DateTimeClosed == null).ToListAsync();
        //}
        #endregion

        #region Fireteam
        public async Task<Fireteam> CreateFireteamAsync(Fireteam raid)
        {
            if (string.IsNullOrEmpty(raid.CreatedByUserId)) throw new ArgumentNullException("CreatedByUserId must be provided");
            if (string.IsNullOrEmpty(raid.ChannelId)) throw new ArgumentNullException("ChannelId must be provided");

            await _db.Fireteams.AddAsync(raid);
            await _db.SaveChangesAsync(true);
            return await Task.FromResult(raid);
        }

        public async Task<Fireteam> UpdateFireteamAsync(Fireteam fireteam)
        {
            if (fireteam.Id <= 0) throw new ArgumentNullException("Id must be provided to update");
            if (string.IsNullOrEmpty(fireteam.CreatedByUserId)) throw new ArgumentNullException("CreatedByUserId must be provided");
            if (string.IsNullOrEmpty(fireteam.MessageId)) throw new ArgumentNullException("MessageId must be provided");
            if (string.IsNullOrEmpty(fireteam.ChannelId)) throw new ArgumentNullException("ChannelId must be provided");

            _db.Fireteams.Update(fireteam);
            await _db.SaveChangesAsync(true);
            return await Task.FromResult(fireteam);
        }

        public async Task<Fireteam> GetFireteamAsync(int fireteamId)
        {
            if (fireteamId <= 0) throw new ArgumentNullException("Id must be provided to update");

            Fireteam existingRaid = await _db.Fireteams.FirstOrDefaultAsync(x => x.Id == fireteamId);
            return await Task.FromResult(existingRaid);
        }

        public async Task<IList<Fireteam>> FindAllOpenFireteams()
        {
            return await _db.Fireteams.Where(x => x.DateTimeClosed == null).ToListAsync();
        }
        #endregion
    }

}
