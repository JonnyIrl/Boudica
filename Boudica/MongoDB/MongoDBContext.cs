using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            bool isMongoLive = _database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(5000);
            if (isMongoLive)
            {
                // connected
                using (StreamWriter sw = new StreamWriter(Directory.GetCurrentDirectory() + "/mongoConnection.txt", true))
                {
                    sw.WriteLine("Connected " + DateTime.UtcNow.ToString());
                }
            }
            else
            {
                // couldn't connect
                int breakHere = 0;
                using (StreamWriter sw = new StreamWriter(Directory.GetCurrentDirectory() + "/mongoConnection.txt", true))
                {
                    sw.WriteLine("Could not connect " + DateTime.UtcNow.ToString());
                }
            }
        }

        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            return _database.GetCollection<T>(collectionName);
        }

        public async Task<int> GetNextId<T>(string collectionName) where T : class, IRecordId
        {
            if (string.IsNullOrEmpty(collectionName)) return -1;
            IMongoCollection<T> collection = _database.GetCollection<T>(collectionName);
            T item = await collection.Find(doc => doc.Id > 0).SortByDescending(x => x.Id).Limit(1).FirstOrDefaultAsync();
            if (item == null) return 1;
            return item.Id + 1;
        }

        public async Task<int> GetNextId<T>(IMongoCollection<T> collection) where T : class, IRecordId
        {
            T item = await collection.Find(doc => doc.Id > 0).SortByDescending(x => x.Id).Limit(1).FirstOrDefaultAsync();
            if (item == null) return 1;
            return item.Id + 1;
        }
    }
}
