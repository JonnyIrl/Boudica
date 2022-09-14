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
using Boudica.MongoDB;
using MongoDB.Driver;

namespace Boudica.Services
{
    public class InsultService
    {
        private readonly DVSContext _db;
        private readonly IMongoDBContext _mongoDBContext;
        protected IMongoCollection<MongoDB.Models.Insult> _insultCollection;
        public InsultService(DVSContext database, IMongoDBContext mongoDBContext)
        {
            _db = database;
            _mongoDBContext = mongoDBContext;
            string name = typeof(MongoDB.Models.Insult).Name;
            _insultCollection = _mongoDBContext.GetCollection<MongoDB.Models.Insult>(name);
        }

        public async Task<MongoDB.Models.Insult> GetTest(ulong userId)
        {
            MongoDB.Models.Insult result = await (await _insultCollection.FindAsync(x => x.Id == userId)).FirstOrDefaultAsync();
            return result;
        }

        public async Task<bool> UpsertUsersInsultTest(ulong userId)
        {
            var builder = Builders<MongoDB.Models.Insult>.Filter;
            var updateBuilder = Builders<MongoDB.Models.Insult>.Update;
            var filter = builder.Eq(x => x.Id, userId);
            var update = updateBuilder.Set(x => x.DateTimeLastInsulted, DateTime.UtcNow);
            var result = await _insultCollection.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<MongoDB.Models.Insult, MongoDB.Models.Insult>() { IsUpsert = true, ReturnDocument = ReturnDocument.After });
            return result != null;
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
