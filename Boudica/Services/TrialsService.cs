using Boudica.MongoDB;
using Boudica.MongoDB.Models;
using Discord;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Services
{
    public class TrialsService
    {
        private readonly IMongoDBContext _mongoDBContext;
        protected IMongoCollection<TrialsVote> _trialsCollection;
        private readonly int AwardedGlimmerAmount = 3;

        public TrialsService(IMongoDBContext mongoDBContext, IServiceProvider services)
        {
            _mongoDBContext = mongoDBContext;
#if DEBUG
            _trialsCollection = _mongoDBContext.GetCollection<TrialsVote>("TrialsVoteTest");
#else
            _trialsCollection = _mongoDBContext.GetCollection<TrialsVote>(typeof(TrialsVote).Name);
#endif
        }

        public async Task<bool> CreateWeeklyTrialsVote()
        {
            if (DateTime.UtcNow.DayOfWeek != DayOfWeek.Friday) return false;
            TrialsVote trialsVote = await GetThisWeeksVote();
            if (trialsVote != null) return false;

            trialsVote = new TrialsVote()
            {
                WeeklyVoteDate = DateTime.UtcNow.Date,
                PlayerVotes = new List<PlayerVote>(),
                IsLocked = false
            };

            await _trialsCollection.InsertOneAsync(trialsVote);
            return trialsVote.Id != Guid.Empty;
        }

        public async Task<bool> UpdateMessageId(ulong messageId)
        {
            TrialsVote trialsVote = await GetThisWeeksVote();
            if (trialsVote == null) return false;
            var updateBuilder = Builders<TrialsVote>.Update;
            await _trialsCollection.UpdateOneAsync(x => x.Id == trialsVote.Id, updateBuilder.Set(x => x.MessageId, messageId));
            return true;
        }

        public async Task<TrialsVote> LockTrialsVote()
        {
            TrialsVote trialsVote = await GetThisWeeksVote();
            if (trialsVote == null) return null;
            trialsVote.IsLocked = true;
            trialsVote.DateTimeLocked = DateTime.UtcNow;

            var updateBuilder = Builders<TrialsVote>.Update;
            await _trialsCollection.FindOneAndReplaceAsync(x => x.Id == trialsVote.Id, trialsVote);
            return trialsVote;
        }

        public async Task<List<PlayerVote>> GetWinningTrialsGuesses(string correctEmojiName)
        {
            TrialsVote trialsVote = await GetThisWeeksVote();
            if (trialsVote == null) return null;
            return trialsVote.PlayerVotes.Where(x => x.VotedEmoteName == correctEmojiName).OrderBy(x => x.DateTimeVoted).ToList();
        }

        public async Task<List<PlayerVote>> GetAllTrialsGuesses()
        {
            TrialsVote trialsVote = await GetThisWeeksVote();
            if (trialsVote == null) return null;
            return trialsVote.PlayerVotes.OrderBy(x => x.DateTimeVoted).ToList();
        }


        public async Task<TrialsVote> GetThisWeeksVote()
        {
            DateTime friday = FindFridayDate();
            TrialsVote existingVote = await _trialsCollection.Find(x => x.WeeklyVoteDate == friday.Date).FirstOrDefaultAsync();
            return existingVote;
        }

        public async Task<bool> AddPlayersVote(ulong userId, string userName, string emoteName)
        {
            TrialsVote existingVote = await _trialsCollection.Find(x => x.WeeklyVoteDate == DateTime.UtcNow.Date).FirstOrDefaultAsync();
            if (existingVote == null) return false;
            if(existingVote.IsLocked) return false;

            PlayerVote existingPlayerVote = existingVote.PlayerVotes.FirstOrDefault(x => x.Id == userId);
            if (existingPlayerVote != null) return false;

            existingPlayerVote = new PlayerVote()
            {
                Id = userId,
                DateTimeVoted = DateTime.UtcNow,
                Username = userName,
                VotedEmoteName = emoteName
            };

            var updateBuilder = Builders<TrialsVote>.Update;
            var updateResult = await _trialsCollection.UpdateOneAsync(x => x.Id == existingVote.Id, updateBuilder.AddToSet("PlayerVotes", existingPlayerVote));

            return updateResult.IsAcknowledged;
        }

        private DateTime FindFridayDate()
        {
            DateTime now = DateTime.UtcNow;
            switch(now.DayOfWeek)
            {
                case DayOfWeek.Sunday:
                    return now.AddDays(-2);
                case DayOfWeek.Monday:
                    return now.AddDays(-3);
                case DayOfWeek.Tuesday:
                    return now.AddDays(-4);
                case DayOfWeek.Wednesday:
                    return now.AddDays(-5);
                case DayOfWeek.Thursday:
                    return now.AddDays(-6);
                case DayOfWeek.Friday:
                    return now;
                case DayOfWeek.Saturday:
                    return now.AddDays(-1);
            }

            return now;
        }
    }
}
