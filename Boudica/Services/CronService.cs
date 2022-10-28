using Boudica.Enums;
using Boudica.Helpers;
using Boudica.MongoDB;
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
        private readonly TrialsService _trialsService;
        private const int FiveMinute = 300000;
        private const int OneMinute = 60000;
        private const int ThirtySeconds = 10000;

        private readonly IMongoDBContext _mongoDBContext;
        private readonly DiscordSocketClient _client;
        protected IMongoCollection<CronTask> _cronTaskCollection;
        private const string CrucibleRole = "Crucible Contenders";
#if DEBUG
        private const ulong GuildId = 958852217186713680;
        private const ulong ChannelId = 1014433494216228905;
        
#else
        private const ulong GuildId = 530462081636368395;
        private const ulong ChannelId = 530529088620724246;
#endif

        public CronService()
        {

        }

        public CronService(IMongoDBContext mongoDBContext, IServiceProvider services)
        {
            _mongoDBContext = mongoDBContext;
#if DEBUG
            _cronTaskCollection = _mongoDBContext.GetCollection<CronTask>(typeof(CronTask).Name + "Test");
#else
            _cronTaskCollection = _mongoDBContext.GetCollection<CronTask>(typeof(CronTask).Name);
#endif
            _trialsService = services.GetRequiredService<TrialsService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            if (_actionTimer == null)
            {
                _actionTimer = new Timer(TimerElapsed, null, OneMinute, FiveMinute);
            }
        }

        //private void PopulateAlphabetList()
        //{
        //    _alphabetList = new List<Emoji>();
        //    _alphabetList.Add(new Emoji("🇦"));
        //    _alphabetList.Add(new Emoji("🇧"));
        //    _alphabetList.Add(new Emoji("🇨"));
        //    _alphabetList.Add(new Emoji("🇩"));
        //    _alphabetList.Add(new Emoji("🇪"));
        //    _alphabetList.Add(new Emoji("🇫"));
        //    _alphabetList.Add(new Emoji("🇬"));
        //    _alphabetList.Add(new Emoji("🇭"));
        //    _alphabetList.Add(new Emoji("🇮"));
        //    _alphabetList.Add(new Emoji("🇰"));
        //    _alphabetList.Add(new Emoji("🇱"));
        //    _alphabetList.Add(new Emoji("🇲"));
        //    _alphabetList.Add(new Emoji("🇳"));
        //    _alphabetList.Add(new Emoji("🇴"));
        //    _alphabetList.Add(new Emoji("🇵"));
        //    _alphabetList.Add(new Emoji("🇶"));
        //    _alphabetList.Add(new Emoji("🇷"));
        //    _alphabetList.Add(new Emoji("🇹"));
        //    _alphabetList.Add(new Emoji("🇺"));
        //    _alphabetList.Add(new Emoji("🇻"));
        //    _alphabetList.Add(new Emoji("🇼"));
        //    _alphabetList.Add(new Emoji("🇽"));
        //    _alphabetList.Add(new Emoji("🇾"));
        //    _alphabetList.Add(new Emoji("🇿"));
        //}

        public async void TimerElapsed(object state)
        {
            try
            {
                List<CronTask> tasks = await GetTasksToAction();
                if (tasks.Count == 0)
                {
                    Console.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss") + " No tasks to be sent");
                    return;
                }

                foreach (CronTask task in tasks)
                {
                    SocketGuild guild = _client.GetGuild(task.GuidId);
                    if (guild == null)
                    {
                        task.DateTimeLastTriggered = DateTime.UtcNow;
                        await MarkTaskAsProcessed(task);
                        continue;
                    }
                    SocketTextChannel channel = guild.GetTextChannel(task.ChannelId);
                    if (channel == null)
                    {
                        task.DateTimeLastTriggered = DateTime.UtcNow;
                        await MarkTaskAsProcessed(task);
                        continue;
                    }

                    task.DateTimeLastTriggered = DateTime.UtcNow;
                    await MarkTaskAsProcessed(task);

                    if (task.Name == "TrialsVote")
                    {
                        bool createdTrialsVote = await _trialsService.CreateWeeklyTrialsVote();
                        if (createdTrialsVote == false)
                        {
                            await channel.SendMessageAsync(embed: EmbedHelper.CreateFailedReply("Failed to create weekly trials vote").Build());
                            return;
                        }

                        EmbedBuilder embed = new EmbedBuilder();
                        CronEmbedAttributes attributes = CreateTrialsVoteEmbedAttributes();
                        task.EmbedAttributes = attributes;
                        embed.Title = task.EmbedAttributes.Title;
                        embed.Description = task.EmbedAttributes.Description;
                        string[] rgb = task.EmbedAttributes.ColorCode.Split(",");
                        embed.Color = new Color(int.Parse(rgb[0]), int.Parse(rgb[1]), int.Parse(rgb[2]));
                        embed.AddField(task.EmbedAttributes.EmbedFieldBuilder);
                        await SendTrialsVoteMessage(task, guild, channel, embed);
                    }
                    else if(task.Name == "TrialsVoteLock")
                    {
                        EmbedBuilder embed = new EmbedBuilder();
                        CronEmbedAttributes attributes = CreateTrialsVoteLockAttributes();
                        task.EmbedAttributes = attributes;
                        embed.Title = task.EmbedAttributes.Title;
                        embed.Description = task.EmbedAttributes.Description;
                        string[] rgb = task.EmbedAttributes.ColorCode.Split(",");
                        embed.Color = new Color(int.Parse(rgb[0]), int.Parse(rgb[1]), int.Parse(rgb[2]));
                        embed.AddField(task.EmbedAttributes.EmbedFieldBuilder);
                        await SendTrialsLockVoteMessage(task, channel, embed);
                    }
                    else
                    {
                        if (task.LastMessageId > 0)
                        {
                            IUserMessage message = (IUserMessage)await channel.GetMessageAsync(task.LastMessageId);
                            if (message == null) return;
                            task.LastMessageId = message.Id;
                            await MarkTaskAsProcessed(task);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception happened in CronService");
                Console.WriteLine(ex.ToString());
            }
        }

        private async Task SendTrialsVoteMessage(CronTask task, SocketGuild guild, SocketTextChannel channel, EmbedBuilder embed)
        {
            IUserMessage message;
            IRole role = guild.Roles.FirstOrDefault(x => x.Name == CrucibleRole);
            if (role != null)
            {
                message = await channel.SendMessageAsync(role.Mention, false, embed.Build());
            }
            else
            {
                message = await channel.SendMessageAsync(null, false, embed.Build());
            }

            task.LastMessageId = message.Id;
            await MarkTaskAsProcessed(task);

            await _trialsService.UpdateMessageId(message.Id);
            await message.PinAsync();
        }

        private async Task SendTrialsLockVoteMessage(CronTask task, SocketTextChannel channel, EmbedBuilder embed)
        {
            TrialsVote trialsVote = await _trialsService.LockTrialsVote();
            if (trialsVote == null)
            {
                await channel.SendMessageAsync(embed: EmbedHelper.CreateFailedReply("Failed to lock").Build());
                return;
            }

            CronTask trialsVoteTask = await _cronTaskCollection.Find(x => x.Name == "TrialsVote").FirstOrDefaultAsync();
            if (trialsVoteTask == null) return;

            IUserMessage message = (IUserMessage)await channel.GetMessageAsync(trialsVoteTask.LastMessageId);
            if (message == null) return;
            task.LastMessageId = message.Id;
            await MarkTaskAsProcessed(task);

            await message.ModifyAsync(x =>
            {
                x.Embed = embed.Build();
            });
            await message.UnpinAsync();
        }

        private async Task<List<CronTask>> GetTasksToAction()
        {
            List<CronTask> tasks = await _cronTaskCollection.Find(x => true).ToListAsync();
            return FilterTaskList(tasks);
        }

        public List<CronTask> FilterTaskList(List<CronTask> tasks)
        {
            DateTime utcNow = DateTime.UtcNow;
            tasks.RemoveAll(x =>
                //Find Daily tasks that need to be run
                (x.RecurringAttribute.RecurringDaily &&           
                (utcNow < x.TriggerDateTime || 
                x.DateTimeLastTriggered.DayOfWeek == utcNow.DayOfWeek || 
                utcNow.TimeOfDay < x.TriggerDateTime.TimeOfDay))
                ||
                //Find Weekly tasks that need to be run
                x.RecurringAttribute.RecurringWeekly && 
                //DateTime.Now from last time triggered is less than 7 days then remove
                (utcNow.Subtract(x.DateTimeLastTriggered).TotalDays < 7.0f || 
                //TimeOfDay is less than when it was supposed to be issued
                utcNow.TimeOfDay < x.TriggerDateTime.TimeOfDay || 
                //It's not the Day of Week it's supposed to be triggered on
                utcNow.DayOfWeek != x.RecurringAttribute.DayOfWeek));
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
                task.TriggerDateTime = DateTime.ParseExact("2022-10-14 09:00:00", "yyyy-MM-dd HH:mm:ss", null);
                task.Name = "TrialsVote";
                task.EmbedAttributes = CreateTrialsVoteEmbedAttributes();
                task.GuidId = GuildId;
                task.ChannelId = ChannelId;
                await _cronTaskCollection.InsertOneAsync(task);
                return await _cronTaskCollection.Find(x => x.Name == "TrialsVote").FirstOrDefaultAsync() != null;
            }

            return false;
        }

        public async Task<bool> CreateTrialsLockTask()
        {
            CronTask existingTask = await _cronTaskCollection.Find(x => x.Name == "TrialsVoteLock").FirstOrDefaultAsync();
            if (existingTask == null)
            {
                CronTask task = new CronTask();
                task.RecurringAttribute = new CronRecurringAttribute()
                {
                    RecurringWeekly = true,
                    DayOfWeek = DayOfWeek.Friday
                };
                task.TriggerDateTime = DateTime.ParseExact("2022-10-14 17:55:00", "yyyy-MM-dd HH:mm:ss", null);
                task.Name = "TrialsVoteLock";
                task.EmbedAttributes = CreateTrialsVoteLockAttributes();
                task.GuidId = GuildId;
                task.ChannelId = ChannelId;
                await _cronTaskCollection.InsertOneAsync(task);
                return await _cronTaskCollection.Find(x => x.Name == "TrialsVoteLock").FirstOrDefaultAsync() != null;
            }

            return false;
        }
        private CronEmbedAttributes CreateTrialsVoteEmbedAttributes()
        {
            DateTime closingDateTime = DateTime.Parse(FindFridayDate().ToString("yyyy-MM-dd") + " 17:00:00");
            closingDateTime = DateTime.SpecifyKind(closingDateTime, DateTimeKind.Utc);
            long offset = ((DateTimeOffset)closingDateTime).ToUnixTimeSeconds();

            CronEmbedAttributes cronEmbedAttributes = new CronEmbedAttributes();
            cronEmbedAttributes.Title = "Trials Vote";
            cronEmbedAttributes.Description = $"Use **/trials-vote** to vote for the map you think will be the Trials map. Only your first vote will count. \n\nVoting will end <t:{offset}:R>";
            cronEmbedAttributes.ColorCode = "21,142,2";
            StringBuilder sb = new StringBuilder();

            Enum.GetValues<TrialsMap>().ToList().ForEach(x =>
            {
                sb.AppendLine(x.ToName());
            });

            cronEmbedAttributes.EmbedFieldBuilder = new EmbedFieldBuilder() { Name = "Maps", Value = sb.ToString(), IsInline = true };
            return cronEmbedAttributes;
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

        private CronEmbedAttributes CreateTrialsVoteLockAttributes()
        {
            CronEmbedAttributes cronEmbedAttributes = new CronEmbedAttributes();
            cronEmbedAttributes.Title = "Trials Voting has now ended";
            cronEmbedAttributes.Description = "";
            cronEmbedAttributes.ColorCode = "237,34,19";
            StringBuilder sb = new StringBuilder();

            Enum.GetValues<TrialsMap>().ToList().ForEach(x =>
            {
                sb.AppendLine(x.ToName());
            });

            cronEmbedAttributes.EmbedFieldBuilder = new EmbedFieldBuilder() { Name = "Maps", Value = sb.ToString(), IsInline = true };
            return cronEmbedAttributes;
        }
    }
}
