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
        protected IMongoCollection<SuspendedUser> _suspendedUserCollection;

        public MiscService(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
#if DEBUG
            string dayOneSignupName = typeof(DayOneSignup).Name + "Test";
            string suspendedUserName = typeof(SuspendedUser).Name + "Test";
#else
            string dayOneSignupName = typeof(DayOneSignup).Name;
            string suspendedUserName = typeof(SuspendedUser).Name;
#endif
            _dayOneCollection = _mongoDBContext.GetCollection<DayOneSignup>(dayOneSignupName);
            _suspendedUserCollection = _mongoDBContext.GetCollection<SuspendedUser>(suspendedUserName);
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

        public async Task<SuspendedUser> IsSuspended(ulong id)
        {
            SuspendedUser suspendedUser = await _suspendedUserCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (suspendedUser == null) return null;
            if(suspendedUser.DateTimeSuspendedUntil > DateTime.UtcNow) return suspendedUser;
            await _suspendedUserCollection.DeleteOneAsync(x => x.Id == id);
            return null;
        }

        public async Task<SuspendedUser> SuspendUser(ulong id, int hours, ulong suspendedById)
        {
            SuspendedUser suspendedUser = await _suspendedUserCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (suspendedUser != null)
            {
                suspendedUser.DateTimeSuspendedUntil = DateTime.UtcNow.AddHours(hours);
                var updateBuilder = Builders<SuspendedUser>.Update;
                var update = updateBuilder
                    .Set(x => x.DateTimeSuspendedUntil, DateTime.UtcNow.AddHours(hours))
                    .Set(x => x.SuspendedByUserId, suspendedById);
                await _suspendedUserCollection.UpdateOneAsync(x => x.Id == id, update);
            }
            else
            {
                suspendedUser = new SuspendedUser(id, DateTime.Now.AddHours(hours), suspendedById);
                await _suspendedUserCollection.InsertOneAsync(suspendedUser);
            }

            return suspendedUser;
        }
    }
}
