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
    public class RaidGroupService
    {
        private readonly DVSContext _db;
        public RaidGroupService(DVSContext database)
        {
            _db = database;
        }

        public async Task<bool> AddPlayerToRaidGroup(int raidId, ulong userId)
        {
            RaidGroup raidGroup = new RaidGroup()
            {
                RaidId = raidId,
                UserId = userId.ToString(),
                IsPlayer = true,
                IsSubstitute = false
            };

            await _db.RaidGroups.AddAsync(raidGroup);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task RemovePlayerFromRaidGroup(int raidId, ulong userId)
        {
            RaidGroup existing = await _db.RaidGroups.FirstOrDefaultAsync(x => x.RaidId == raidId && x.UserId == userId.ToString());
            if (existing != null)
            {
                _db.Remove(existing);
                await _db.SaveChangesAsync();
            }
        }
    }
}
