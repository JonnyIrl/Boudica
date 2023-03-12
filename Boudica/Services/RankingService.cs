using Boudica.MongoDB.Models;
using Boudica.MongoDB;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Services
{
    public class RankingService
    {
        private readonly IMongoDBContext _mongoDBContext;
        protected IMongoCollection<Insult> _insultCollection;
        public RankingService(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
#if DEBUG
            string name = typeof(Insult).Name + "Test";
#else
            string name = typeof(Insult).Name;
#endif
            _insultCollection = _mongoDBContext.GetCollection<Insult>(name);
        }
    }
}
