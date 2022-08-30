using Boudica.Database;
using Boudica.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Boudica.Services
{
    public class ActivityService
    {
        private readonly DVSContext _db;

        public ActivityService(DVSContext database)
        {
            _db = database;
        }

        public async Task<Raid> CreateRaidAsync(Raid raid)
        {
            if (string.IsNullOrEmpty(raid.CreatedByUserId)) throw new ArgumentNullException("CreatedByUserId must be provided");
            if (string.IsNullOrEmpty(raid.ChannelId)) throw new ArgumentNullException("ChannelId must be provided");

            await _db.Raids.AddAsync(raid);
            await _db.SaveChangesAsync();
            return await Task.FromResult(raid);
        }

        public async Task<Raid> UpdateRaidAsync(Raid raid)
        {
            if(raid.Id <= 0) throw new ArgumentNullException("Id must be provided to update");
            if (string.IsNullOrEmpty(raid.CreatedByUserId)) throw new ArgumentNullException("CreatedByUserId must be provided");
            if (string.IsNullOrEmpty(raid.MessageId)) throw new ArgumentNullException("MessageId must be provided");
            if (string.IsNullOrEmpty(raid.ChannelId)) throw new ArgumentNullException("ChannelId must be provided");

            _db.Raids.Update(raid);
            await _db.SaveChangesAsync();
            return await Task.FromResult(raid);
        }

        public async Task<Raid> GetRaidAsync(int raidId)
        {
            if (raidId <= 0) throw new ArgumentNullException("Id must be provided to update");

            Raid existingRaid = await _db.Raids.FirstOrDefaultAsync(x => x.Id == raidId);
            return await Task.FromResult(existingRaid);
        }
    }

}
