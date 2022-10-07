﻿using Boudica.MongoDB;
using Boudica.MongoDB.Models;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Services
{
    public class CronService
    {
        private readonly Timer _actionTimer;
        private const int FiveMinutes = 300000;
        private const int ThirtySeconds = 30000;

        private readonly IMongoDBContext _mongoDBContext;
        private readonly DiscordSocketClient _client;
        protected IMongoCollection<CronTask> _cronTaskCollection;

        private List<Emoji> _alphabetList;

        public CronService(IMongoDBContext mongoDBContext, IServiceProvider services)
        {
            //_mongoDBContext = mongoDBContext;
            //_cronTaskCollection = _mongoDBContext.GetCollection<CronTask>(typeof(CronTask).Name);
            //_client = services.GetRequiredService<DiscordSocketClient>();
            //PopulateAlphabetList();
            //if (_actionTimer == null)
            //{
            //    _actionTimer = new Timer(TimerElapsed, null, ThirtySeconds, FiveMinutes);
            //}
        }

        private void PopulateAlphabetList()
        {
            _alphabetList = new List<Emoji>();
            _alphabetList.Add(new Emoji("🇦"));
            _alphabetList.Add(new Emoji("🇧"));
            _alphabetList.Add(new Emoji("🇨"));
            _alphabetList.Add(new Emoji("🇩"));
            _alphabetList.Add(new Emoji("🇪"));
            _alphabetList.Add(new Emoji("🇫"));
            _alphabetList.Add(new Emoji("🇬"));
            _alphabetList.Add(new Emoji("🇭"));
            _alphabetList.Add(new Emoji("🇮"));
            _alphabetList.Add(new Emoji("🇰"));
            _alphabetList.Add(new Emoji("🇱"));
            _alphabetList.Add(new Emoji("🇲"));
            _alphabetList.Add(new Emoji("🇳"));
            _alphabetList.Add(new Emoji("🇴"));
            _alphabetList.Add(new Emoji("🇵"));
            _alphabetList.Add(new Emoji("🇶"));
            _alphabetList.Add(new Emoji("🇷"));
            _alphabetList.Add(new Emoji("🇹"));
            _alphabetList.Add(new Emoji("🇺"));
            _alphabetList.Add(new Emoji("🇻"));
            _alphabetList.Add(new Emoji("🇼"));
            _alphabetList.Add(new Emoji("🇽"));
            _alphabetList.Add(new Emoji("🇾"));
            _alphabetList.Add(new Emoji("🇿"));
        }

        public async void TimerElapsed(object state)
        {
            try
            {
                List<CronTask> tasks = await GetTasksToAction();
                if(tasks.Count == 0)
                {
                    Console.WriteLine("No tasks to be sent");
                }

                foreach(CronTask task in tasks)
                {
                    SocketGuild guild = _client.GetGuild(task.GuidId);
                    if (guild == null)
                    {
                        task.DateTimeLastTriggered = DateTime.UtcNow;
                        await MarkTaskAsProcessed(task);
                        continue;
                    }
                    SocketTextChannel channel = guild.GetTextChannel(task.ChannelId);
                    if(channel == null)
                    {
                        task.DateTimeLastTriggered = DateTime.UtcNow;
                        await MarkTaskAsProcessed(task);
                        continue;
                    }

                    EmbedBuilder embed = new EmbedBuilder();
                    embed.Title = task.EmbedAttributes.Title;
                    embed.Description = task.EmbedAttributes.Description;
                    string[] rgb = task.EmbedAttributes.ColorCode.Split(",");
                    embed.Color = new Color(int.Parse(rgb[0]), int.Parse(rgb[1]), int.Parse(rgb[2]));
                    embed.AddField(task.EmbedAttributes.EmbedFieldBuilder);
                    IUserMessage message = await channel.SendMessageAsync(null, false, embed.Build());
                    await message.AddReactionsAsync(_alphabetList.Take(TrialsMaps.Count));
                }
            }
            catch (Exception ex)
            {

            }
        }

        private async Task<List<CronTask>> GetTasksToAction()
        {
            DateTime utcNow = DateTime.UtcNow;
            List<CronTask> tasks = await _cronTaskCollection.Find(x => true).ToListAsync();
            tasks.RemoveAll(x =>
                //Find Daily tasks that need to be run
                (x.RecurringAttribute.RecurringDaily && utcNow > x.TriggerDateTime && x.DateTimeLastTriggered.Day != utcNow.Day && utcNow.TimeOfDay >= x.TriggerDateTime.TimeOfDay)
                ||
                //Find Weekly tasks that need to be run
                x.RecurringAttribute.RecurringWeekly && utcNow.Subtract(x.DateTimeLastTriggered).TotalDays >= 7 && utcNow.TimeOfDay >= x.TriggerDateTime.TimeOfDay && utcNow.Day == x.TriggerDateTime.Day);
            return tasks;
        }

        private async Task MarkTaskAsProcessed(CronTask task)
        {
            await _cronTaskCollection.FindOneAndReplaceAsync(x => x.Id == task.Id, task);
        }



        public async Task<bool> CreateTrialsTask()
        {
            CronTask existingTask = await _cronTaskCollection.Find(x => x.Name == "TrialsVote").FirstOrDefaultAsync();
            if(existingTask == null)
            {
                CronTask task = new CronTask();
                task.RecurringAttribute = new CronRecurringAttribute()
                {
                    RecurringWeekly = true,
                    DayOfWeek = DayOfWeek.Friday
                };
                task.TriggerDateTime = DateTime.ParseExact("2022-10-07 09:00:00", "yyyy-MM-dd HH:mm:ss", null);
                task.Name = "TrialsVote";
                task.EmbedAttributes = CreateTrialsVoteEmbedAttributes();
                task.GuidId = 958852217186713680;
                task.ChannelId = 1014433494216228905;
                await _cronTaskCollection.InsertOneAsync(task);
                return await _cronTaskCollection.Find(x => x.Name == "TrialsVote").FirstOrDefaultAsync() != null;
            }

            return false;
        }
        private CronEmbedAttributes CreateTrialsVoteEmbedAttributes()
        {
            CronEmbedAttributes cronEmbedAttributes = new CronEmbedAttributes();
            cronEmbedAttributes.Title = "Trials Vote";
            cronEmbedAttributes.Description = "Vote for the map you think will be the Trials map. Only your first vote will count";
            cronEmbedAttributes.ColorCode = "21,142,2";
            StringBuilder sb = new StringBuilder();

            for(int i = 0; i < TrialsMaps.Count; i++)
            {
                sb.AppendLine($"{_alphabetList[i]} {TrialsMaps[i]}");
            }
            cronEmbedAttributes.EmbedFieldBuilder = new EmbedFieldBuilder() { Name = "Maps", Value = sb.ToString() };
            return cronEmbedAttributes;
        }
        private List<string> TrialsMaps = new List<string>()
        {
            "Altar of Flame",
            "Bannerfall",
            "Burnout",
            "Cathedral of Dusk",
            "Disjunction",
            "Distant Shore",
            "Endless Vale",
            "Eternity",
            "Exodus Blue",
            "Fragment",
            "Javelin-4",
            "Midtown",
            "Pacifica",
            "Radiant Cliffs",
            "Rusted Lands",
            "The Dead Cliffs",
            "The Fortress",
            "Twilight Gap",
            "Vostok",
            "Widow’s Court",
            "Wormhaven",
        };
    }
}