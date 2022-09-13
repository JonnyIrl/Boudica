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
                    Insult newInsult = new Insult() { DateTimeLastInsulted = DateTime.UtcNow, UserId = userId.ToString() };
                    await _db.Insult.AddAsync(newInsult);
                    await _db.SaveChangesAsync(true);
                }
                else
                {
                    existingInsult.DateTimeLastInsulted = DateTime.UtcNow;
                    _db.Insult.Update(existingInsult);
                    await _db.SaveChangesAsync(true);
                }

                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
        }
    }
}
