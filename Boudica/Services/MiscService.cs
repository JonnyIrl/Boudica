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
        protected IMongoCollection<DramaReport> _dramaCollection;
        protected IMongoCollection<UnsubscribeReminder> _unsubscribedReminderCollection;

        public MiscService(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
#if DEBUG
            string dayOneSignupName = typeof(DayOneSignup).Name + "Test";
            string suspendedUserName = typeof(SuspendedUser).Name + "Test";
            string dramaName = typeof(DramaReport).Name + "Test";
            string unsubscribedName = typeof(UnsubscribeReminder).Name + "Test";
#else
            string dayOneSignupName = typeof(DayOneSignup).Name;
            string suspendedUserName = typeof(SuspendedUser).Name;
            string dramaName = typeof(DramaReport).Name;
            string unsubscribedName = typeof(UnsubscribeReminder).Name;
#endif
            _dayOneCollection = _mongoDBContext.GetCollection<DayOneSignup>(dayOneSignupName);
            _suspendedUserCollection = _mongoDBContext.GetCollection<SuspendedUser>(suspendedUserName);
            _dramaCollection = mongoDBContext.GetCollection<DramaReport>(dramaName);
            _unsubscribedReminderCollection = mongoDBContext.GetCollection<UnsubscribeReminder>(unsubscribedName);
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

        public async Task<DramaReport> GetDramaReport()
        {
            DramaReport report = await _dramaCollection.Find(x => x.Id > 0).FirstOrDefaultAsync();
            if(report == null)
            {
                report = new DramaReport() { Id = 1, Start = DateTime.UtcNow };
                await _dramaCollection.InsertOneAsync(report);
            }

            return report;
        }

        public async Task<DramaReport> ResetDramaReport()
        {
            DramaReport report = await _dramaCollection.Find(x => x.Id > 0).FirstOrDefaultAsync();
            if (report == null)
            {
                report = new DramaReport() { Start = DateTime.UtcNow };
                await _dramaCollection.InsertOneAsync(report);
            }
            else
            {
                var builder = Builders<DramaReport>.Update;
                var update = builder.Set(x => x.Start, DateTime.UtcNow);
                await _dramaCollection.UpdateOneAsync(x => x.Id > 0, update);
            }

            return report;
        }

        public async Task<bool> IsUserUnsubscribed(ulong guardianId)
        {
            return await _unsubscribedReminderCollection.Find(x => x.GuardianId == guardianId).AnyAsync();
        }

        public async Task<bool> UnsubscribeUser(ulong userId)
        {
            if (await IsUserUnsubscribed(userId)) return true;
            await _unsubscribedReminderCollection.InsertOneAsync(new UnsubscribeReminder() { GuardianId = userId, DateTimeUnsubscribed = DateTime.UtcNow });
            return await IsUserUnsubscribed(userId);
        }
    }
}
