using Boudica.Classes;
using Boudica.Database;
using Boudica.Database.Models;
using Boudica.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Services
{
    public class GuardianReputationService
    {
        private readonly DVSContext _db;
        private readonly GuardianService _guardianService;
        public GuardianReputationService(IServiceProvider services, DVSContext dbContext)
        {
            _db = dbContext;
            _guardianService = services.GetRequiredService<GuardianService>();
        }

        public async Task<bool> AddGuardianReputation(int amount, ulong userId)
        {
            Guardian existingGuardian = await _guardianService.Get(userId);
            if(existingGuardian == null) throw new ArgumentNullException(nameof(existingGuardian));

            GuardianReputation guardianReputation = new GuardianReputation()
            {
                Amount = amount,
                GuardianId = existingGuardian.Id,
                DateTimeAwarded = DateTime.UtcNow,
            };

            await _db.AddAsync(guardianReputation);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetGuardianReputation(int guardianId)
        {
            var results = _db.GuardianReputation.Include(x => x.Guardian).Where(x => x.GuardianId == guardianId);
            return await results.SumAsync(x => x.Amount);
        }

        public async Task<List<GuardianLeaderboard>> GetGuardianReputationLeaderboard(Filter filter)
        {
            DateTime start = DateTime.Now;
            DateTime end = DateTime.Now;
            switch (filter)
            {
                case Filter.Daily:
                    start = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                    end = start.AddDays(1);
                    break;

                case Filter.Weekly:
                    start = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + (int)DayOfWeek.Monday);
                    end = start.AddDays(7);
                    break;

                case Filter.Monthly:
                    int daysinMonth = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
                    start = DateTime.Parse(DateTime.Now.AddDays(-daysinMonth).ToString("yyyy-MM-dd 00:00:00"));
                    end = start.AddDays(daysinMonth);
                    break;
            }

            var results = _db.GuardianReputation.Include(x => x.Guardian).Where(x => x.DateTimeAwarded >= start && x.DateTimeAwarded < end).GroupBy(x => x.GuardianId);
            List<GuardianLeaderboard> guardianLeaderboards = new List<GuardianLeaderboard>();
            foreach(var result in results)
            {
                int total = result.Sum(x => x.Amount);
                string userId = result.First().Guardian.UserId;
                GuardianLeaderboard leaderboard = new GuardianLeaderboard()
                {
                    Amount = total,
                    UserId = userId
                };
                guardianLeaderboards.Add(leaderboard);
            }

            return guardianLeaderboards;
        }
    }
}
