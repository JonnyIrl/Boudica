using Boudica.Database;
using Boudica.Database.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Services
{
    public class GuardianService
    {
        private readonly DVSContext _db;
        public GuardianService(DVSContext dbContext)
        {
            _db = dbContext;
        }

        public async Task<Guardian> Create(ulong userId)
        {
            Guardian guardian = new Guardian()
            {
                UserId = userId.ToString(),
                ReputationLevel = 0,
                Glimmer = 0
            };

            await _db.Guardians.AddAsync(guardian);
            await _db.SaveChangesAsync();
            return guardian;
        }

        public async Task<Guardian> Get(ulong userId)
        {
            Guardian guardian = await _db.Guardians.FirstOrDefaultAsync(x => x.UserId == userId.ToString());
            if(guardian == null)
            {
                guardian = await Create(userId);
            }

            return guardian;
        }

        public async Task<bool> IncreaseGlimmer(ulong userId, int amount)
        {
            Guardian guardian = await Get(userId);
            if(guardian == null)
            {
                throw new NullReferenceException("Guardian not found");
            }

            guardian.Glimmer += amount;
            _db.Update(guardian);
            await _db.SaveChangesAsync();
            return true;
        }


    }
}
