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
using Boudica.MongoDB.Models;

namespace Boudica.Services
{
    public class InsultService
    {
        private readonly IMongoDBContext _mongoDBContext;
        protected IMongoCollection<Insult> _insultCollection;
        public InsultService(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
            string name = typeof(Insult).Name;
            _insultCollection = _mongoDBContext.GetCollection<Insult>(name);
        }

        public async Task<Insult> Get(ulong userId)
        {
            Insult result = await (await _insultCollection.FindAsync(x => x.Id == userId)).FirstOrDefaultAsync();
            return result;
        }

        public async Task<bool> UpsertUsersInsult(ulong userId)
        {
            var builder = Builders<Insult>.Filter;
            var updateBuilder = Builders<Insult>.Update;
            var filter = builder.Eq(x => x.Id, userId);
            var update = updateBuilder.Set(x => x.DateTimeLastInsulted, DateTime.UtcNow);
            var result = await _insultCollection.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<Insult, Insult>() { IsUpsert = true, ReturnDocument = ReturnDocument.After });
            return result != null;
        }
    }
}
