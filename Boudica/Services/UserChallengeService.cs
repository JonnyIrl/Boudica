using Boudica.Commands;
using Boudica.Enums;
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
    public class UserChallengeService
    {
        private readonly IMongoDBContext _mongoDBContext;
        protected IMongoCollection<UserChallenge> _userChallengeCollection;
        public UserChallengeService(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
            string name = typeof(UserChallenge).Name;
            _userChallengeCollection = _mongoDBContext.GetCollection<UserChallenge>(name);
        }

        public async Task<UserChallenge> CreateUserChallenge(ulong challengerId, string challengerName, ulong contenderId, string contenderName, int wager, Challenge challenge)
        {
            UserChallenge userChallenge = new UserChallenge(challengerId, challengerName, contenderId, contenderName, wager, challenge);
            long sessionId = await _userChallengeCollection.CountDocumentsAsync(x => true);
            userChallenge.SessionId = sessionId;
            userChallenge.ExpiredDateTime = DateTime.UtcNow.AddMinutes(1);
            await _userChallengeCollection.InsertOneAsync(userChallenge);
            return userChallenge;
        }

        public async Task<CommandResult> AcceptUserChallenge(long sessionId, ulong contenderId)
        {
            UserChallenge userChallenge = await _userChallengeCollection.Find(x => x.SessionId == sessionId).FirstOrDefaultAsync();
            if (userChallenge == null) return new CommandResult(false, "Could not find a challenge with this session id");
            if (userChallenge.ContenderId != contenderId) return new CommandResult(false, "You were not challenged for this session id");
            if (userChallenge.ExpiredDateTime < DateTime.UtcNow) return new CommandResult(false, "This challenge has expired. Be faster next time.. shouldn't be hard for you ;)");
            var builder = Builders<UserChallenge>.Filter;
            var updateBuilder = Builders<UserChallenge>.Update;
            var filter = builder.Eq(x => x.SessionId, sessionId);
            UpdateResult result = await _userChallengeCollection.UpdateOneAsync(filter,
                updateBuilder.Set(x => x.Accepted, true), new UpdateOptions() { IsUpsert = false });;

            if(result.IsAcknowledged == false)
                return new CommandResult(false, "Could not update Challenge.. Jonnys issue not yours");

            return new CommandResult(true, string.Empty);
        }

        public async Task<bool> MarkChallengeAsCompleted(long sessionId, ulong winnerId)
        {
            UserChallenge userChallenge = await _userChallengeCollection.Find(x => x.SessionId == sessionId).FirstOrDefaultAsync();
            if (userChallenge == null) return false;
            var builder = Builders<UserChallenge>.Filter;
            var updateBuilder = Builders<UserChallenge>.Update;
            var filter = builder.Eq(x => x.SessionId, sessionId);
            UpdateResult result = await _userChallengeCollection.UpdateOneAsync(filter,
                updateBuilder
                .Set(x => x.Accepted, true)
                .Set(x => x.WinnerId, winnerId), 
                new UpdateOptions() { IsUpsert = false });

            return result.IsAcknowledged;
        }
    }
}
