using Boudica.MongoDB;
using Boudica.MongoDB.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Services
{
    public class MiscService
    {
        private readonly IMongoDBContext _mongoDBContext;
        protected IMongoCollection<DayOneSignup> _dayOneCollection;
        public MiscService(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
#if DEBUG
            string name = typeof(DayOneSignup).Name + "Test";
#else
            string name = typeof(DayOneSignup).Name;
#endif
            _dayOneCollection = _mongoDBContext.GetCollection<DayOneSignup>(name);
        }

        public async Task<bool> AlreadySignedUp(ulong id)
        {
            return await _dayOneCollection.Find(x => x.Id == id).FirstOrDefaultAsync() != null;
        }

        public async Task<bool> SignUp(ulong id, string userName, bool willLFG)
        {
            DayOneSignup signup = new DayOneSignup(id, userName, willLFG);
            signup.DateTimeSignedUp = DateTime.UtcNow;
            await _dayOneCollection.InsertOneAsync(signup);
            return true;
        }
    }
}
