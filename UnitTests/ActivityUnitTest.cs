using Boudica.MongoDB;
using Boudica.MongoDB.Models;
using Boudica.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestClass]
    public class ActivityUnitTest
    {
        [TestMethod]
        public void CreatedActivityThisWeek()
        {
            List<Raid> raids = new List<Raid>();
            Raid differentCreatedByUser = CreateRaid(1019141857848078357, DateTime.Parse("2022-10-27 10:00:00"), DateTime.MinValue, null);
            const ulong userId = 244209636897456129;
            Raid newRaid = CreateRaid(userId, DateTime.Parse("2022-10-27 10:00:00"), DateTime.MinValue, null);
            Raid oldRaid = CreateRaid(userId, DateTime.Parse("2022-10-25 10:00:00"), DateTime.Parse("2022-10-26 10:00:00"), null);

            //No old raids at all for user should return true
            ActivityService activityService = new ActivityService();
                                                                    //Sunday
            DateTime startOfWeek = DateTimeExtensions.StartOfWeek(DateTime.Parse("2022-10-30 10:00:00"), DayOfWeek.Monday);
            Assert.AreEqual(startOfWeek.Date, DateTime.Parse("2022-10-24 00:00:00").Date);

            raids.Add(differentCreatedByUser);
            Raid match = raids.Find(x => x.DateTimeClosed >= startOfWeek && x.CreatedByUserId == userId);
            Assert.IsNull(match);

            //Still not closed
            raids.Add(newRaid);
            match = raids.Find(x => x.DateTimeClosed >= startOfWeek && x.CreatedByUserId == userId);
            Assert.IsNull(match);

            raids.Add(oldRaid);
            match = raids.Find(x => x.DateTimeClosed >= startOfWeek && x.CreatedByUserId == userId);
            Assert.IsNotNull(match);
        }


        private Raid CreateRaid(ulong userId, DateTime created, DateTime closed, List<ActivityUser> additionalPlayers)
        {
            Raid newRaid = new Raid()
            {
                CreatedByUserId = userId,
                DateTimeCreated = created,
                DateTimeClosed = closed,
            };

            if (additionalPlayers != null && additionalPlayers.Any())
                newRaid.Players.AddRange(additionalPlayers);

            return newRaid;
        }
    }
}
