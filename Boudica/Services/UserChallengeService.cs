﻿using Boudica.Commands;
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
        protected IMongoCollection<MongoDB.Models.UserChallenge> _userChallengeCollection;
        public UserChallengeService(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
#if DEBUG
            string name = typeof(MongoDB.Models.UserChallenge).Name + "Test";
#else
            string name = typeof(UserChallenge).Name;
#endif
            _userChallengeCollection = _mongoDBContext.GetCollection<MongoDB.Models.UserChallenge>(name);
        }

        public async Task<MongoDB.Models.UserChallenge> CreateUserChallenge(ulong challengerId, string challengerName, ulong contenderId, string contenderName, int wager, Enums.UserChallenges challenge)
        {
            MongoDB.Models.UserChallenge userChallenge = new MongoDB.Models.UserChallenge(challengerId, challengerName, contenderId, contenderName, wager, challenge);
            long sessionId = await _userChallengeCollection.CountDocumentsAsync(x => true);
            userChallenge.SessionId = sessionId;
            userChallenge.ExpiredDateTime = DateTime.UtcNow.AddMinutes(5);
            await _userChallengeCollection.InsertOneAsync(userChallenge);
            return userChallenge;
        }

        public async Task<CommandResult> AcceptUserChallenge(long sessionId, ulong contenderId)
        {
            MongoDB.Models.UserChallenge userChallenge = await _userChallengeCollection.Find(x => x.SessionId == sessionId).FirstOrDefaultAsync();
            if (userChallenge == null) return new CommandResult(false, "Could not find a challenge with this session id");
            if (userChallenge.Contender.UserId != contenderId) return new CommandResult(false, "You were not challenged for this session id");
            if (userChallenge.ExpiredDateTime < DateTime.UtcNow) return new CommandResult(false, "This challenge has expired. Be faster next time.. shouldn't be hard for you ;)");
            var builder = Builders<MongoDB.Models.UserChallenge>.Filter;
            var updateBuilder = Builders<MongoDB.Models.UserChallenge>.Update;
            var filter = builder.Eq(x => x.SessionId, sessionId);
            UpdateResult result = await _userChallengeCollection.UpdateOneAsync(filter,
                updateBuilder.Set(x => x.Accepted, true), new UpdateOptions() { IsUpsert = false }); ;

            if (result.IsAcknowledged == false)
                return new CommandResult(false, "Could not update Challenge.. Jonnys issue not yours");

            return new CommandResult(true, string.Empty);
        }

        public async Task<MongoDB.Models.UserChallenge> GetUserChallenge(long sessionId)
        {
            return await _userChallengeCollection.Find(x => x.SessionId == sessionId).FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateContendersAnswer(long sessionId, string guess)
        {
            var updateBuilder = Builders<MongoDB.Models.UserChallenge>.Update;
            var result = await _userChallengeCollection.UpdateOneAsync(x => x.SessionId == sessionId, updateBuilder.Set(x => x.Contender.Answer, guess));
            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateChallengersAnswer(long sessionId, string guess)
        {
            var updateBuilder = Builders<MongoDB.Models.UserChallenge>.Update;
            var result = await _userChallengeCollection.UpdateOneAsync(x => x.SessionId == sessionId, updateBuilder.Set(x => x.Challenger.Answer, guess));
            return result.IsAcknowledged;
        }
        public async Task<bool> UpdateChallengeMessageDetails(long sessionId, ulong guildId, ulong channelId, ulong messageId)
        {
            var updateBuilder = Builders<MongoDB.Models.UserChallenge>.Update;
            var result = await _userChallengeCollection.UpdateOneAsync(x => x.SessionId == sessionId, 
                updateBuilder.Set(x => x.GuildId, guildId)
                .Set(x => x.ChannelId, channelId)
                .Set(x => x.MessageId, messageId));
            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateChallengeWinnerId(long sessionId, ulong winnerId)
        {
            var updateBuilder = Builders<MongoDB.Models.UserChallenge>.Update;
            var result = await _userChallengeCollection.UpdateOneAsync(x => x.SessionId == sessionId,
                updateBuilder.Set(x => x.WinnerId, winnerId));
            return result.IsAcknowledged;
        }
        public async Task<bool> MarkChallengeAsCompleted(long sessionId, ulong winnerId)
        {
            MongoDB.Models.UserChallenge userChallenge = await _userChallengeCollection.Find(x => x.SessionId == sessionId).FirstOrDefaultAsync();
            if (userChallenge == null) return false;
            var builder = Builders<MongoDB.Models.UserChallenge>.Filter;
            var updateBuilder = Builders<MongoDB.Models.UserChallenge>.Update;
            var filter = builder.Eq(x => x.SessionId, sessionId);
            UpdateResult result = await _userChallengeCollection.UpdateOneAsync(filter,
                updateBuilder
                .Set(x => x.Accepted, true)
                .Set(x => x.WinnerId, winnerId), 
                new UpdateOptions() { IsUpsert = false });

            return result.IsAcknowledged;
        }
        public async Task<bool> UpdateClosedChallenge(long sessionId)
        {
            var updateBuilder = Builders<MongoDB.Models.UserChallenge>.Update;
            var result = await _userChallengeCollection.UpdateOneAsync(x => x.SessionId == sessionId,
                updateBuilder.Set(x => x.IsClosed, true));
            return result.IsAcknowledged;
        }
        public async Task<List<MongoDB.Models.UserChallenge>> GetExpiredChallenges()
        {
            return await _userChallengeCollection.Find(x => x.ExpiredDateTime < DateTime.UtcNow && x.Accepted == false && x.IsClosed == false).ToListAsync();
        }
    }
}
