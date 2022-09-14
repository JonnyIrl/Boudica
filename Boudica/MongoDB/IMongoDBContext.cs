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
    }
}
