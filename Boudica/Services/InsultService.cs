using Boudica.Database;
using Boudica.Database.Models;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Boudica.Classes;

namespace Boudica.Services
{
    public class InsultService
    {
        private readonly DVSContext _db;
        public InsultService(DVSContext database)
        {
            _db = database;
        }

        public async Task<Insult> Get(ulong userId)
        {
            return await _db.Insult.FirstOrDefaultAsync(x => x.UserId == userId.ToString());
        }

        public async Task<bool> UpsertUsersInsult(ulong userId, Insult existingInsult)
        {
            try
            {
                if (existingInsult == null)
                {
                    await _db.Insult.AddAsync(new Insult() { DateTimeLastInsulted = DateTime.UtcNow, UserId = userId.ToString() });
                }
                else
                {
                    existingInsult.DateTimeLastInsulted = DateTime.UtcNow;
                    _db.Insult.Update(existingInsult);
                }

                await _db.SaveChangesAsync(true);
                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
        }
    }
}
