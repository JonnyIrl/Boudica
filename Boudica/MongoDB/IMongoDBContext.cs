using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB
{
    public interface IMongoDBContext
    {
        IMongoCollection<T> GetCollection<T>(string collectionName);
        Task<int> GetNextId<T>(string collectionName) where T : class, IRecordId;
        Task<int> GetNextId<T>(IMongoCollection<T> collection) where T : class, IRecordId;
    }
}
