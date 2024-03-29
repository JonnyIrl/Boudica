﻿using Boudica.MongoDB.Models;
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
    public class CronTaskUnitTest
    {
        private readonly CronService _cronService;
        public CronTaskUnitTest()
        {
            _cronService = new CronService();
        }

        [TestMethod]
        public void RealWorldCheck()
        {
            DateTime lastTriggered = DateTime.Parse("2022-10-14 06:00");
            DateTime triggerDateTime = DateTime.Parse("2022-10-07 06:25");

            List<CronTask> tasks = new List<CronTask>();

            tasks.Add(GetTrialsCronTaskWeekly(lastTriggered, triggerDateTime));

            lastTriggered = DateTime.MinValue;
            triggerDateTime = DateTime.Parse("2022-10-21 06:25");
            tasks.Add(GetTrialsCronTaskWeekly(lastTriggered, triggerDateTime));

            //triggerDateTime = DateTime.UtcNow.AddDays(-7).AddSeconds(-30);
            //tasks.Add(GetTrialsCronTaskWeekly(lastTriggered, triggerDateTime));

            tasks = _cronService.FilterTaskList(tasks);
            Assert.IsTrue(tasks.Count == 2);
        }

        [TestMethod]
        public void TrialsVoteLock_Daily_IssuedToday_Success()
        {
            DateTime lastTriggered = DateTime.UtcNow;
            DateTime triggerDateTime = DateTime.UtcNow.AddDays(-7);

            CronTask task = GetTrialsCronTaskDaily(lastTriggered, triggerDateTime);

            List<CronTask> tasks = _cronService.FilterTaskList(new List<CronTask> { task });
            Assert.IsTrue(tasks.Any() == false);
        }

        [TestMethod]
        public void TrialsVoteLock_Daily_IssuedOneDayAgo_Success()
        {
            DateTime lastTriggered = DateTime.UtcNow.AddDays(-1);
            DateTime triggerDateTime = DateTime.UtcNow.AddDays(-7);

            CronTask task = GetTrialsCronTaskDaily(lastTriggered, triggerDateTime);

            List<CronTask> tasks = _cronService.FilterTaskList(new List<CronTask> { task });
            Assert.IsTrue(tasks.Any());
        }

        [TestMethod]
        public void TrialsVoteLock_Daily_IssuedTwoDayAgo_Success()
        {
            DateTime lastTriggered = DateTime.UtcNow.AddDays(-2);
            DateTime triggerDateTime = DateTime.UtcNow.AddDays(-7);

            CronTask task = GetTrialsCronTaskDaily(lastTriggered, triggerDateTime);

            List<CronTask> tasks = _cronService.FilterTaskList(new List<CronTask> { task });
            Assert.IsTrue(tasks.Any());
        }

        [TestMethod]
        public void TrialsVoteLock_Daily_IssuedThreeDayAgo_Success()
        {
            DateTime lastTriggered = DateTime.UtcNow.AddDays(-3);
            DateTime triggerDateTime = DateTime.UtcNow.AddDays(-7);

            CronTask task = GetTrialsCronTaskDaily(lastTriggered, triggerDateTime);

            List<CronTask> tasks = _cronService.FilterTaskList(new List<CronTask> { task });
            Assert.IsTrue(tasks.Any());
        }

        [TestMethod]
        public void TrialsVoteLock_Daily_NeverIssued_Success()
        {
            DateTime lastTriggered = DateTime.MinValue;
            DateTime triggerDateTime = DateTime.UtcNow.AddDays(-7);

            CronTask task = GetTrialsCronTaskDaily(lastTriggered, triggerDateTime);

            List<CronTask> tasks = _cronService.FilterTaskList(new List<CronTask> { task });
            Assert.IsTrue(tasks.Any());
        }

        #region Weekly Test Cases
        [TestMethod]
        public void TrialsVote_Weekly_IssuedToday_Success()
        {
            CronTask task = GetTrialsCronTaskWeekly(DateTime.Parse(DateTime.UtcNow.Date.ToString("yyyy-MM-dd") + " 08:00"), 
                DateTime.Parse(DateTime.UtcNow.AddDays(-7).Date.ToString("yyyy-MM-dd") + " 00:00"));

            List<CronTask> tasks = _cronService.FilterTaskList(new List<CronTask> { task });
            Assert.IsTrue(tasks.Any() == false);
        }

        [TestMethod]
        public void TrialsVote_Weekly_IssuedOneDaysAgo_Success()
        {
            CronTask task = GetTrialsCronTaskWeekly(DateTime.Parse(DateTime.UtcNow.AddDays(-1).Date.ToString("yyyy-MM-dd") + " 08:00"), 
                DateTime.Parse(DateTime.UtcNow.AddDays(-7).Date.ToString("yyyy-MM-dd") + " 00:00"));

            List<CronTask> tasks = _cronService.FilterTaskList(new List<CronTask> { task });
            Assert.IsTrue(tasks.Any() == false);
        }

        [TestMethod]
        public void TrialsVote_Weekly_IssuedTwoDaysAgo_Success()
        {
            CronTask task = GetTrialsCronTaskWeekly(DateTime.Parse(DateTime.UtcNow.AddDays(-2).Date.ToString("yyyy-MM-dd") + " 08:00"),
                DateTime.Parse(DateTime.UtcNow.AddDays(-7).Date.ToString("yyyy-MM-dd") + " 00:00"));

            List<CronTask> tasks = _cronService.FilterTaskList(new List<CronTask> { task });
            Assert.IsTrue(tasks.Any() == false);
        }

        [TestMethod]
        public void TrialsVote_Weekly_IssuedThreeDaysAgo_Success()
        {
            CronTask task = GetTrialsCronTaskWeekly(DateTime.Parse(DateTime.UtcNow.AddDays(-3).Date.ToString("yyyy-MM-dd") + " 08:00"),
                DateTime.Parse(DateTime.UtcNow.AddDays(-7).Date.ToString("yyyy-MM-dd") + " 00:00"));

            List<CronTask> tasks = _cronService.FilterTaskList(new List<CronTask> { task });
            Assert.IsTrue(tasks.Any() == false);
        }

        [TestMethod]
        public void TrialsVote_Weekly_IssuedFourDaysAgo_Success()
        {
            CronTask task = GetTrialsCronTaskWeekly(DateTime.Parse(DateTime.UtcNow.AddDays(-4).Date.ToString("yyyy-MM-dd") + " 08:00"),
                DateTime.Parse(DateTime.UtcNow.AddDays(-7).Date.ToString("yyyy-MM-dd") + " 00:00"));

            List<CronTask> tasks = _cronService.FilterTaskList(new List<CronTask> { task });
            Assert.IsTrue(tasks.Any() == false);
        }

        [TestMethod]
        public void TrialsVote_Weekly_IssuedFiveDaysAgo_Success()
        {
            CronTask task = GetTrialsCronTaskWeekly(DateTime.Parse(DateTime.UtcNow.AddDays(-5).Date.ToString("yyyy-MM-dd") + " 08:00"),
                DateTime.Parse(DateTime.UtcNow.AddDays(-7).Date.ToString("yyyy-MM-dd") + " 00:00"));

            List<CronTask> tasks = _cronService.FilterTaskList(new List<CronTask> { task });
            Assert.IsTrue(tasks.Any() == false);
        }

        [TestMethod]
        public void TrialsVote_Weekly_IssuedSixDaysAgo_Success()
        {
            CronTask task = GetTrialsCronTaskWeekly(DateTime.Parse(DateTime.UtcNow.AddDays(-6).Date.ToString("yyyy-MM-dd") + " 08:00"),
                DateTime.Parse(DateTime.UtcNow.AddDays(-7).Date.ToString("yyyy-MM-dd") + " 00:00"));

            List<CronTask> tasks = _cronService.FilterTaskList(new List<CronTask> { task });
            Assert.IsTrue(tasks.Any() == false);
        }

        [TestMethod]
        public void TrialsVote_Weekly_IssuedSevenDaysAgo_Success()
        {
            DateTime dateTimeLastTriggered = DateTime.UtcNow.AddDays(-7);
            DateTime triggerDateTime = DateTime.UtcNow.AddDays(7).AddSeconds(-30);

            CronTask task = GetTrialsCronTaskWeekly(dateTimeLastTriggered, triggerDateTime);

            bool totalDaysResult = DateTime.UtcNow.Subtract(dateTimeLastTriggered).TotalDays < 7;
            bool timeOfDayResult = DateTime.UtcNow.TimeOfDay < triggerDateTime.TimeOfDay;
            bool dayResult = DateTime.UtcNow.DayOfWeek != task.RecurringAttribute.DayOfWeek;

            List<CronTask> tasks = _cronService.FilterTaskList(new List<CronTask> { task });
            Assert.IsTrue(tasks.Any());
        }

        [TestMethod]
        public void TrialsVote_IssuedEightDaysAgo_Success()
        {
            CronTask task = GetTrialsCronTaskWeekly(DateTime.UtcNow.AddDays(-8), DateTime.UtcNow.AddDays(7).AddSeconds(-30));

            List<CronTask> tasks = _cronService.FilterTaskList(new List<CronTask> { task });
            Assert.IsTrue(tasks.Any());
        }

        [TestMethod]
        public void TrialsVote_Weekly_IssuedEightDaysAgo_WrongDay_Success()
        {
            DateTime dateTimeLastTriggered = DateTime.Parse(DateTime.UtcNow.AddDays(-8).Date.ToString("yyyy-MM-dd") + " 08:00");
            DateTime triggerDateTime = DateTime.Parse(DateTime.UtcNow.AddDays(-17).Date.ToString("yyyy-MM-dd") + " 00:00");

            CronTask task = GetTrialsCronTaskWeekly(dateTimeLastTriggered, triggerDateTime);

            bool totalDaysResult = DateTime.UtcNow.Subtract(dateTimeLastTriggered).TotalDays < 7;
            bool timeOfDayResult = DateTime.UtcNow.TimeOfDay < triggerDateTime.TimeOfDay;
            bool dayResult = DateTime.UtcNow.DayOfWeek != task.RecurringAttribute.DayOfWeek;

            List<CronTask> tasks = _cronService.FilterTaskList(new List<CronTask> { task });
            Assert.IsTrue(tasks.Any() == false);
        }
        #endregion

        [TestMethod]
        public void UnixTimestamp()
        {
            //1666976400
            //FindFridayDate().ToString("yyyy-MM-dd")
            DateTime closingDateTime = DateTime.Parse("2022-10-28" + " 17:00:00");
            closingDateTime = DateTime.SpecifyKind(closingDateTime, DateTimeKind.Utc);
            long offset = ((DateTimeOffset)closingDateTime).ToUnixTimeSeconds();
            int breakHere = 0;
            Assert.AreEqual(1666976400, offset);
        }

        private DateTime FindFridayDate()
        {
            DateTime now = DateTime.UtcNow;
            switch (now.DayOfWeek)
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

        private CronTask GetTrialsCronTaskWeekly(DateTime dateTimeLastTriggered, DateTime triggerDateTime)
        {
            return new CronTask()
            {
                Name = "TrialsVote",
                DateTimeLastTriggered = dateTimeLastTriggered,
                TriggerDateTime = triggerDateTime,
                RecurringAttribute = new CronRecurringAttribute()
                {
                    DayOfWeek = triggerDateTime.DayOfWeek,
                    RecurringWeekly = true
                }
            };
        }

        private CronTask GetTrialsCronTaskDaily(DateTime dateTimeLastTriggered, DateTime triggerDateTime)
        {
            return new CronTask()
            {
                Name = "TrialsVote",
                DateTimeLastTriggered = dateTimeLastTriggered,
                TriggerDateTime = triggerDateTime,
                RecurringAttribute = new CronRecurringAttribute()
                {
                    DayOfWeek = triggerDateTime.DayOfWeek,
                    RecurringDaily = true
                }
            };
        }
    }
}
