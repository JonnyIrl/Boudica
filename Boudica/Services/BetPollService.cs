using Boudica.Classes;
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
    public class BetPollService
    {
        private readonly IMongoDBContext _mongoDBContext;
        protected IMongoCollection<BetPoll> _betPollCollection;
        public BetPollService(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
#if DEBUG
            string name = typeof(BetPoll).Name + "Test";
#else
            string name = typeof(BetPoll).Name;
#endif
            _betPollCollection = _mongoDBContext.GetCollection<BetPoll>(name);
        }

        public async Task<BetPoll> GetBetPoll(long pollId)
        {
            return await _betPollCollection.Find(x => x.Id == pollId).FirstOrDefaultAsync();
        }

        public async Task<BetPoll> CreateBetPollAsync(BetPoll poll)
        {
            if (poll == null) throw new NullReferenceException("Poll was null");
            if (string.IsNullOrEmpty(poll.Question)) throw new ArgumentNullException("You must provide a value for Question");
            if (poll.CreatedOptions == null || poll.CreatedOptions.Count == 0) throw new ArgumentNullException("You must provide a value for CreatedOptions");
            poll.Id = await _betPollCollection.CountDocumentsAsync(x => true);
            poll.DateTimeCreated = DateTime.UtcNow;
            await _betPollCollection.InsertOneAsync(poll);
            return poll;
        }

        public async Task<bool> UpdateBetPollMessageId(long pollId, ulong messageId)
        {
            var updateBuilder = Builders<BetPoll>.Update;
            var result = await _betPollCollection.UpdateOneAsync(x => x.Id == pollId, updateBuilder.Set(x => x.MessageId, messageId));
            return result.IsAcknowledged;
        }

        public async Task<Result> AddPlayerBetPollVote(ulong userId, string userName, long pollId, PollOption votedOption, int betAmount)
        {
            BetPoll existingPoll = await _betPollCollection.Find(x => x.Id == pollId).FirstOrDefaultAsync();
            if (existingPoll == null) return new Result(false, "Could not find poll");
            PlayerPollVote existingVote = existingPoll.Votes.FirstOrDefault(x => x.Id == userId);
            if (existingVote != null) return new Result(false, "You have already voted on this poll");
            var updateBuilder = Builders<BetPoll>.Update;
            var result = await _betPollCollection.UpdateOneAsync(x => x.Id == pollId, 
                updateBuilder.Push(x => x.Votes, new PlayerPollVote(userId, userName, votedOption, betAmount)));
            return new(result.IsAcknowledged, $"Update result {result.IsAcknowledged}");
        }

        public async Task<bool> LockBetPoll(long betPollId)
        {
            var updateBuilder = Builders<BetPoll>.Update;
            var result = await _betPollCollection.UpdateOneAsync(
                x => x.Id == betPollId,
                updateBuilder
                .Set(x => x.IsLocked, true));
            return result.IsAcknowledged;
        }

        public async Task<bool> CloseBetPoll(BetPoll poll)
        {
            var updateBuilder = Builders<BetPoll>.Update;
            var result = await _betPollCollection.UpdateOneAsync(
                x => x.Id == poll.Id,
                updateBuilder
                .Set(x => x.IsClosed, true)
                .Set(x => x.WinningOption, poll.WinningOption)
                .Set(x => x.DateTimeClosed, DateTime.UtcNow));
            return result.IsAcknowledged;
        }
    }
}
