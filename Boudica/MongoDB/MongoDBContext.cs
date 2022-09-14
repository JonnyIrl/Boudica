using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB
{
    public class MongoDBContext : IMongoDBContext
    {
        private IMongoDatabase _database { get; set; }
        private IMongoClient _mongoClient { get; set; }
        public IClientSessionHandle SessionHandle { get; set; }

        public MongoDBContext(IConfiguration configuration)
        {
#if DEBUG
            _mongoClient = new MongoClient(configuration[nameof(Mongosettings.MongoDebugConnectionString)]);
            _database = _mongoClient.GetDatabase(configuration[nameof(Mongosettings.MongoDebugDatabaseName)]);
#else
            _mongoClient = new MongoClient(configuration[nameof(Mongosettings.MongoReleaseConnectionString)]);
            _database = _mongoClient.GetDatabase(configuration[nameof(Mongosettings.MongoReleaseDatabaseName)]);
#endif
            bool isMongoLive = _database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000);
            if (isMongoLive)
            {
                // connected
                int breakHere = 0;
            }
            else
            {
                // couldn't connect
                int breakHere = 0;
            }
        }

        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            return _database.GetCollection<T>(collectionName);
        }
    }
}
