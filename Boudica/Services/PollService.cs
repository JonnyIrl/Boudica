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
    public class PollService
    {
        private readonly IMongoDBContext _mongoDBContext;
        protected IMongoCollection<Poll> _pollCollection;
        public PollService(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
#if DEBUG
            string name = typeof(MongoDB.Models.Poll).Name + "Test";
#else
            string name = typeof(Poll).Name;
#endif
            _pollCollection = _mongoDBContext.GetCollection<Poll>(name);
        }

        public async Task<Poll> GetPoll(long pollId)
        {
            return await _pollCollection.Find(x => x.Id == pollId).FirstOrDefaultAsync();
        }

        public async Task<Poll> CreatePollAsync(Poll poll)
        {
            if (poll == null) throw new NullReferenceException("Poll was null");
            if (string.IsNullOrEmpty(poll.Question)) throw new ArgumentNullException("You must provide a value for Question");
            if (poll.CreatedOptions == null || poll.CreatedOptions.Count == 0) throw new ArgumentNullException("You must provide a value for CreatedOptions");
            poll.Id = await _pollCollection.CountDocumentsAsync(x => true);
            poll.DateTimeCreated = DateTime.UtcNow;
            await _pollCollection.InsertOneAsync(poll);
            return poll;
        }

        public async Task<bool> UpdatePollMessageId(long pollId, ulong messageId)
        {
            var updateBuilder = Builders<Poll>.Update;
            var result = await _pollCollection.UpdateOneAsync(x => x.Id == pollId, updateBuilder.Set(x => x.MessageId, messageId));
            return result.IsAcknowledged;
        }

        public async Task<Result> AddPlayerPollVote(ulong userId, string userName, long pollId, PollOption votedOption)
        {
            Poll existingPoll = await _pollCollection.Find(x => x.Id == pollId).FirstOrDefaultAsync();
            if (existingPoll == null) return new Result(false, "Could not find poll");
            PlayerPollVote existingVote = existingPoll.Votes.FirstOrDefault(x => x.Id == userId);
            if (existingVote != null) return new Result(false, "You have already voted on this poll");
            var updateBuilder = Builders<Poll>.Update;
            var result = await _pollCollection.UpdateOneAsync(x => x.Id == pollId, updateBuilder.Push(x => x.Votes, new PlayerPollVote(userId, userName, votedOption)));
            return new(result.IsAcknowledged, $"Update result {result.IsAcknowledged}");
        }

        public async Task<bool> ClosePoll(Poll poll)
        {
            var updateBuilder = Builders<Poll>.Update;
            var result = await _pollCollection.UpdateOneAsync(
                x => x.Id == poll.Id, 
                updateBuilder
                .Set(x => x.IsClosed, true)
                .Set(x => x.WinningOptions, poll.WinningOptions)
                .Set(x => x.DateTimeClosed, DateTime.UtcNow));
            return result.IsAcknowledged;
        }
    }
}
