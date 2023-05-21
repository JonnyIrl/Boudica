using Boudica.MongoDB;
using DBHelper.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;

namespace DBHelper
{
    internal class Program
    {
        private static IConfiguration _config;
        private static ActivityHelper _activityHelper;
        static void Main(string[] args)
        {
            Console.WriteLine("Starting");

            var _builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(path: "config.json");

            // build the configuration and assign to _config          
            _config = _builder.Build();

            MongoDBContext database = new MongoDBContext(_config[nameof(Mongosettings.MongoReleaseConnectionString)], _config[nameof(Mongosettings.MongoReleaseDatabaseName)]);

            _activityHelper = new ActivityHelper(database);

            DateTime startOfSeason = DateTime.Parse("2023-02-28 00:00:00");

            Console.WriteLine("Getting Raid Stats");
            _activityHelper.GetRaidStats(startOfSeason, DateTime.Now);
            Console.WriteLine("Finished getting Raid Stats");
            using (StreamWriter writer = new StreamWriter("raidStats.txt"))
            {
                writer.Write(JsonConvert.SerializeObject(_activityHelper.GuardianStats));
            }

            Console.WriteLine("Getting Fireteam Stats");
            _activityHelper.GetFireteamStats(startOfSeason, DateTime.Now);
            Console.WriteLine("Finsihed getting Fireteam Stats");
            using (StreamWriter writer = new StreamWriter("fireteamStats.txt"))
            {
                writer.Write(JsonConvert.SerializeObject(_activityHelper.GuardianStats));
            }

            Console.WriteLine("Getting History Stats");
            _activityHelper.GetHistoryDetails(startOfSeason, DateTime.Now);
            Console.WriteLine("Finsihed getting History Stats");
            using (StreamWriter writer = new StreamWriter("historyStats.txt"))
            {
                writer.Write(JsonConvert.SerializeObject(_activityHelper.GuardianStats));
            }

            Console.WriteLine("Getting all stats and writing");
            List<GuardianStats> stats = _activityHelper.GuardianStats;
            using (StreamWriter sw = new StreamWriter("GuardianStats.csv"))
            {
                sw.WriteLine("Id, Name, Completed Raids, Incomplete Raids, Completed Fireteams, Incomplete Fireteams, Insults Issued, Insult Received, Awards Issued, Awards Received, Super Sub, Compliments Issued, Compliments Received, Daily Gifts, Trials Votes, Higher or Lowers played, Added to Activity Count");
                foreach (GuardianStats g in stats)
                {
                    if (string.IsNullOrEmpty(g.Username))
                        g.Username = _activityHelper.GetGuardian(g.Id).Username;

                    if(g.Username.Contains(","))
                        g.Username = g.Username.Replace(",", string.Empty);
                    sw.WriteLine($"{g.Id},{g.Username},{g.CompletedRaidCount},{g.FailedRaidCount},{g.CompletedFireteamCount},{g.FailedFireteamCount},{g.InsultIssuedCount},{g.InsultReceivedCount},{g.AwardsIssuedCount},{g.AwardsReceivedCount},{g.SuperSubCount},{g.ComplimentIssuedCount},{g.ComplimentReceivedCount},{g.DailyGiftCount},{g.TrialsVoteCount},{g.HigherOrLowerCount},{g.AddedToActivityCount}");
                }
            }

            Console.WriteLine("Finished");
            Console.ReadKey();

        }
    }
}