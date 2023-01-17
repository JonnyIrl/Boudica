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
    public class BotChallengeService
    {
        private readonly IMongoDBContext _mongoDBContext;
        protected IMongoCollection<BotChallenge> _botChallengeCollection;
        public BotChallengeService(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
#if DEBUG
            string name = typeof(MongoDB.Models.BotChallenge).Name + "Test";
#else
            string name = typeof(BotChallenge).Name;
#endif
            _botChallengeCollection = _mongoDBContext.GetCollection<BotChallenge>(name);
        }

        public async Task<BotChallenge> CreateBotChallenge(ulong challengerId, string challengerName, int wager, BotChallenges challenge)
        {
            MongoDB.Models.BotChallenge BotChallenge = new BotChallenge(challengerId, challengerName,  wager, challenge);
            long sessionId = await _botChallengeCollection.CountDocumentsAsync(x => true);
            BotChallenge.SessionId = sessionId;
            BotChallenge.ExpiryDateTime = DateTime.UtcNow.AddMinutes(5);
            await _botChallengeCollection.InsertOneAsync(BotChallenge);
            return BotChallenge;
        }

        public async Task<CommandResult> AcceptBotChallenge(long sessionId, ulong contenderId)
        {
            BotChallenge botChallenge = await _botChallengeCollection.Find(x => x.SessionId == sessionId).FirstOrDefaultAsync();
            if (botChallenge == null) return new CommandResult(false, "Could not find a challenge with this session id");
            if (botChallenge.ExpiryDateTime < DateTime.UtcNow) return new CommandResult(false, "This challenge has expired. Be faster next time.. shouldn't be hard for you ;)");
            var builder = Builders<BotChallenge>.Filter;
            var updateBuilder = Builders<BotChallenge>.Update;
            var filter = builder.Eq(x => x.SessionId, sessionId);
            UpdateResult result = await _botChallengeCollection.UpdateOneAsync(filter,
                updateBuilder.Set(x => x.Accepted, true), new UpdateOptions() { IsUpsert = false }); ;

            if (result.IsAcknowledged == false)
                return new CommandResult(false, "Could not update Challenge.. Jonnys issue not yours");

            return new CommandResult(true, string.Empty);
        }

        public async Task<BotChallenge> GetBotChallenge(long sessionId)
        {
            return await _botChallengeCollection.Find(x => x.SessionId == sessionId).FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateChallengersAnswer(long sessionId, string guess)
        {
            var updateBuilder = Builders<BotChallenge>.Update;
            var result = await _botChallengeCollection.UpdateOneAsync(x => x.SessionId == sessionId, updateBuilder.Set(x => x.Challenger.Answer, guess));
            return result.IsAcknowledged;
        }
        public async Task<bool> UpdateChallengeMessageDetails(long sessionId, ulong guildId, ulong channelId, ulong messageId)
        {
            var updateBuilder = Builders<BotChallenge>.Update;
            var result = await _botChallengeCollection.UpdateOneAsync(x => x.SessionId == sessionId,
                updateBuilder.Set(x => x.GuildId, guildId)
                .Set(x => x.ChannelId, channelId)
                .Set(x => x.MessageId, messageId));
            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateChallengeWinnerId(long sessionId, ulong winnerId)
        {
            var updateBuilder = Builders<BotChallenge>.Update;
            var result = await _botChallengeCollection.UpdateOneAsync(x => x.SessionId == sessionId,
                updateBuilder.Set(x => x.WinnerId, winnerId));
            return result.IsAcknowledged;
        }
        public async Task<bool> MarkChallengeAsCompleted(long sessionId, ulong winnerId)
        {
            BotChallenge BotChallenge = await _botChallengeCollection.Find(x => x.SessionId == sessionId).FirstOrDefaultAsync();
            if (BotChallenge == null) return false;
            var builder = Builders<BotChallenge>.Filter;
            var updateBuilder = Builders<BotChallenge>.Update;
            var filter = builder.Eq(x => x.SessionId, sessionId);
            UpdateResult result = await _botChallengeCollection.UpdateOneAsync(filter,
                updateBuilder
                .Set(x => x.Accepted, true)
                .Set(x => x.WinnerId, winnerId),
                new UpdateOptions() { IsUpsert = false });

            return result.IsAcknowledged;
        }
        public async Task<bool> UpdateClosedChallenge(long sessionId)
        {
            var updateBuilder = Builders<BotChallenge>.Update;
            var result = await _botChallengeCollection.UpdateOneAsync(x => x.SessionId == sessionId,
                updateBuilder.Set(x => x.IsClosed, true));
            return result.IsAcknowledged;
        }
        public async Task<List<BotChallenge>> GetExpiredChallenges()
        {
            return await _botChallengeCollection.Find(x => x.ExpiryDateTime < DateTime.UtcNow && x.Accepted == true && x.IsClosed == false).ToListAsync();
        }
    }
}
