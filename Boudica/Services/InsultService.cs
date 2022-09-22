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
        protected IMongoCollection<MongoDB.Models.Insult> _insultCollection;
        public InsultService(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
            string name = typeof(MongoDB.Models.Insult).Name;
            _insultCollection = _mongoDBContext.GetCollection<MongoDB.Models.Insult>(name);
        }

        public async Task<MongoDB.Models.Insult> Get(ulong userId)
        {
            MongoDB.Models.Insult result = await (await _insultCollection.FindAsync(x => x.Id == userId)).FirstOrDefaultAsync();
            return result;
        }

        public async Task<bool> UpsertUsersInsult(ulong userId)
        {
            var builder = Builders<MongoDB.Models.Insult>.Filter;
            var updateBuilder = Builders<MongoDB.Models.Insult>.Update;
            var filter = builder.Eq(x => x.Id, userId);
            var update = updateBuilder.Set(x => x.DateTimeLastInsulted, DateTime.UtcNow);
            var result = await _insultCollection.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<MongoDB.Models.Insult, MongoDB.Models.Insult>() { IsUpsert = true, ReturnDocument = ReturnDocument.After });
            return result != null;
        }
    }
}
