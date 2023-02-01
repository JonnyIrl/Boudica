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


        //public async Task<BotChallenge> CreateBotChallenge(ulong challengerId, string challengerName, int wager, BotChallenges challenge)
        //{
        //    MongoDB.Models.BotChallenge BotChallenge = new BotChallenge(challengerId, challengerName, wager, challenge);
        //    long sessionId = await _botChallengeCollection.CountDocumentsAsync(x => true);
        //    BotChallenge.SessionId = sessionId;
        //    BotChallenge.ExpiryDateTime = DateTime.UtcNow.AddMinutes(5);
        //    await _botChallengeCollection.InsertOneAsync(BotChallenge);
        //    return BotChallenge;
        //}

        //public async Task<CommandResult> AcceptBotChallenge(long sessionId)
        //{
        //    BotChallenge botChallenge = await _botChallengeCollection.Find(x => x.SessionId == sessionId).FirstOrDefaultAsync();
        //    if (botChallenge == null) return new CommandResult(false, "Could not find a challenge with this session id");
        //    if (botChallenge.ExpiryDateTime < DateTime.UtcNow) return new CommandResult(false, "This challenge has expired. Be faster next time.. shouldn't be hard for you ;)");
        //    var builder = Builders<BotChallenge>.Filter;
        //    var updateBuilder = Builders<BotChallenge>.Update;
        //    var filter = builder.Eq(x => x.SessionId, sessionId);
        //    UpdateResult result = await _botChallengeCollection.UpdateOneAsync(filter,
        //        updateBuilder
        //        .Set(x => x.Accepted, true)
        //        .Set(x => x.CurrentRound, (int)RoundNumber.FirstRound),
        //        new UpdateOptions() { IsUpsert = false }); ;

        //    if (result.IsAcknowledged == false)
        //        return new CommandResult(false, "Could not update Challenge.. Jonnys issue not yours");

        //    return new CommandResult(true, string.Empty);
        //}

        //public async Task<BotChallenge> GetBotChallenge(long sessionId)
        //{
        //    return await _botChallengeCollection.Find(x => x.SessionId == sessionId).FirstOrDefaultAsync();
        //}

        //public async Task<bool> UpdateChallengersAnswer(long sessionId, string guess)
        //{
        //    var updateBuilder = Builders<BotChallenge>.Update;
        //    var result = await _botChallengeCollection.UpdateOneAsync(x => x.SessionId == sessionId, updateBuilder.Set(x => x.Challenger.Answer, guess));
        //    return result.IsAcknowledged;
        //}
    }
}
