using Boudica.Enums;
using Boudica.Helpers;
using Boudica.MongoDB.Models;
using Boudica.Services;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Commands
{
    public class MiscCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private const string ReverseUnoCard = "Reverse uno card";

        private readonly InsultService _insultService;
        private readonly AwardedGuardianService _awardedGuardianService;
        private readonly GuardianService _guardianService;
        private readonly ActivityService _activityService;
        private readonly DailyGiftService _dailyGiftService;
        private readonly HistoryService _historyService;

        private const int CreatorPoints = 5;

        private static int _lastMessageIndex = 0;
        private static List<Raid> _raids = null;
        private static List<Fireteam> _fireteams = null;
        public MiscCommands(IServiceProvider services)
        {
            _insultService = services.GetRequiredService<InsultService>();
            _awardedGuardianService = services.GetRequiredService<AwardedGuardianService>();
            _guardianService = services.GetRequiredService<GuardianService>();
            _dailyGiftService = services.GetRequiredService<DailyGiftService>();
            _activityService = services.GetRequiredService<ActivityService>();
            _historyService = services.GetRequiredService<HistoryService>();
        }

        [SlashCommand("insult", "Choose a player to insult")]
        public async Task InsultCommand(SocketGuildUser user)
        {
            if (user == null || user.IsBot)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command, supply a single users name like /insult @SpecificUser").Build());
                return;
            }

            try
            {
                Insult usersLastInsult = await _insultService.Get(Context.User.Id);
                if (usersLastInsult != null && usersLastInsult.DateTimeLastInsulted.Date == DateTime.UtcNow.Date)
                {
                    await RespondAsync(embed: EmbedHelper.CreateFailedReply("You can only insult once per day, use it wisely!").Build());
                    return;
                }

                string insult = Insults.GetRandomInsult();
                if (insult.Contains(ReverseUnoCard))
                {
                    insult = insult.Replace("{userId}", $"<@{Context.User.Id}>");
                    await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"{insult}").Build());
                }
                else
                {
                    await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"<@{user.Id}> {insult}").Build());
                }

                await _insultService.UpsertUsersInsult(Context.User.Id);
                await _historyService.InsertHistoryRecord(Context.User.Id, user.Id, HistoryType.Insult);
            }
            catch (Exception ex)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command, supply a single users name like /insult @SpecificUser").Build());
            }
        }

        [SlashCommand("compliment", "Compliment a Player")]
        public async Task ComplimentCommand(SocketGuildUser user)
        {
            if (user == null || user.IsBot)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command, supply a single users name like /compliment @SpecificUser").Build());
                return;
            }

            try
            {
                    if (user.Id == Context.User.Id)
                    {
                        await RespondAsync(embed: EmbedHelper.CreateFailedReply($"<@{user.Id}> self praise is no praise, sorry not sorry!").Build());
                    }
                    else
                    {
                        string compliment = Compliments.GetRandomCompliment();
                    await _historyService.InsertHistoryRecord(Context.User.Id, user.Id, HistoryType.Compliment);
                    await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"<@{user.Id}> {compliment}").Build());
                    }
            }
            catch (Exception ex)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command, supply a single users name like ;compliment @SpecificUser").Build());
            }
        }

        [SlashCommand("joke", "Posts a random joke")]
        public async Task JokeCommand()
        {
            //await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"Jokes currently going through filtering..").Build());
            //return;
            string joke = Jokes.GetRandomJoke();
            await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"{joke}").Build());
        }

        [SlashCommand("coinflip", "Random coinflip")]
        public async Task CoinflipCommand()
        {
            Random random = new Random();
            int number = random.Next(0, 2);

            string fileName;
            if (number == 0)
            {
                fileName = "https://media.giphy.com/media/WyzyENjdELhS4EnuYb/giphy.gif";
            }
            else
            {
                fileName = "https://media.giphy.com/media/K7OLwCi2fWQ1YaTItI/giphy.gif";
            }
            var embed = new EmbedBuilder()
            {
                ImageUrl = $"{fileName}",
                Description = "Flipping coin...",
                Color = Color.Orange
            }.Build();

            await RespondAsync(embed: embed);
            IUserMessage userMessage = await GetOriginalResponseAsync();
            await Task.Delay(3000);

            await userMessage.ModifyAsync(x =>
            {
                EmbedBuilder embed = new EmbedBuilder();
                embed.ImageUrl = $"{fileName}";
                if (number == 0 )
                {
                    embed.WithDescription("**Heads!**");
                    embed.WithColor(Color.Green);
                }
                else if (number == 1)
                {
                    embed.WithDescription("**Tails!**");
                    embed.WithColor(Color.Green);
                }
                x.Embed = embed.Build();
            });
        }       

        [SlashCommand("daily-gift", "Every day, get a free daily gift of 1-10 Glimmer!")]
        public async Task DailyGift()
        {
            DailyGift dailyGift = await _dailyGiftService.Get(Context.User.Id);
            if (dailyGift != null && dailyGift.DateTimeLastGifted.Date == DateTime.UtcNow.Date)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("You can only get one gift per day.").Build(), ephemeral: true);
                return;
            }

            if (DateTime.UtcNow.Date == DateTime.Parse("2023-01-01 00:00:00").Date)
            {
                await DailyGift_NewYears();
                return;
            }

            //const string OneGlimmer = "https://i.imgur.com/bRbtWlE.gif";
            //const string TwoGlimmer = "https://i.imgur.com/KHkbILH.gif";
            //const string ThreeGlimmer = "https://i.imgur.com/8M22qQz.gif";
            //const string FourGlimmer = "https://i.imgur.com/iwXpEno.gif";
            //const string FiveGlimmer = "https://i.imgur.com/tvjtrm4.gif";
            //const string SixGlimmer = "https://i.imgur.com/wlxZVcq.gif";
            //const string SevenGlimmer = "https://i.imgur.com/LbYqjld.gif";
            //const string EightGlimmer = "https://i.imgur.com/v8e27qk.gif";
            //const string NineGlimmer = "https://i.imgur.com/iQ4k7ur.gif";
            //const string TenGlimmer = "https://i.imgur.com/KsRZ6eA.gif";

           
            Random random = new Random();
            int amount = random.Next(1, 11);
            await _guardianService.IncreaseGlimmerAsync(Context.User.Id, Context.User.Username, amount);
            await _dailyGiftService.UpsertUsersDailyGift(Context.User.Id);
            await _historyService.InsertHistoryRecord(Context.User.Id, null, HistoryType.DailyGift);
            await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"You have received {amount} Glimmer as your daily gift!").Build());
            //switch(amount)
            //{
            //    case 1:
            //        await RespondAsync(OneGlimmer);
            //        break;
            //    case 2:
            //        await RespondAsync(TwoGlimmer);
            //        break;
            //    case 3:
            //        await RespondAsync(ThreeGlimmer);
            //        break;
            //    case 4:
            //        await RespondAsync(FourGlimmer);
            //        break;
            //    case 5:
            //        await RespondAsync(FiveGlimmer);
            //        break;
            //    case 6:
            //        await RespondAsync(SixGlimmer);
            //        break;
            //    case 7:
            //        await RespondAsync(SevenGlimmer);
            //        break;
            //    case 8:
            //        await RespondAsync(EightGlimmer);
            //        break;
            //    case 9:
            //        await RespondAsync(NineGlimmer);
            //        break;
            //    case 10:
            //        await RespondAsync(TenGlimmer);
            //        break;
            //    default:
            //        await RespondAsync(embed: EmbedHelper.CreateFailedReply("Something went wrong..").Build());
            //        break;
            //}
            //Thread.Sleep(3000);
            //await FollowupAsync(text: string.Empty, embed: EmbedHelper.CreateSuccessReply($"You have received {amount} Glimmer as your daily gift!").Build());
        }

        public async Task DailyGift_NewYears()
        {
            await DeferAsync();
            if (_raids == null)
            {
                ulong guidId = Context.Guild.Id;
                _raids = await _activityService.GetAllRaids(guidId);
            }

            if (_fireteams == null)
            {
                ulong guidId = Context.Guild.Id;
                _fireteams = await _activityService.GetAllFireteams(guidId);
            }

            AwardedGuardians lastAwardedGuardian = await _awardedGuardianService.GetLastAwardedGuardian(Context.User.Id);
            string lastAwardedGuardianName = "Nobody :(";
            if(lastAwardedGuardian != null)
            {
                lastAwardedGuardianName = $"<@{lastAwardedGuardian.AwardedGuardiansId}>";
            }

            EmbedBuilder embedBuilder = CreateNewYearsEmbed(lastAwardedGuardianName);
            Thread.Sleep(3000);
            await FollowupAsync(embed: embedBuilder.Build());          

            await _guardianService.IncreaseGlimmerAsync(Context.User.Id, Context.User.Username, 25);
            await _dailyGiftService.UpsertUsersDailyGift(Context.User.Id);
        }

        private static string GetNewYearsMessage()
        {
            List<string> messages = new List<string>()
            {
                "Happy New Year! May the coming year be full of grand adventures and opportunities.",
                "Life is short. Dream big and make the most of 2023!",
                "Happy New Year! 2023 is the beginning of a new chapter. This is your year. Make it happen.",
                "Life is an adventure that's full of beautiful destinations. Wishing you many wonderful memories made in 2023.",
                "Wishing you a Happy New Year, bursting with fulfilling and exciting opportunities. And remember, if opportunity doesn't knock, build a door!",
                "May the New Year bring you happiness, peace, and prosperity. Wishing you a joyous 2023!",
                "It is time to forget the past and celebrate a new start. Happy New Year!",
                "Happy New Year! I hope all your endeavors in 2023 are successful.",
                "Sending you the very best of wishes for the new year. May it be full of bright opportunities!",
                "As the sun sets on another year, I wish you great company and good cheer.",
                "Happy New Year! Let's toast to yesterday's achievements and tomorrow's bright future.",
                "On the road to success, the rule is always to look ahead. May you reach your destination and may your journey be wonderful. Happy New Year!",
                "Happy New Year! I hope all your dreams come true in 2023. Onwards and upwards!",
                "Wishing you and your family a happy new year filled with hope, health, and happiness - with a generous sprinkle of fun!",
                "Wishing you a blessed New Year! When I count my blessings, I count you twice.",
                "Each year is a gift that holds hope for new adventures. May your New Year be filled with exploration, discovery, and growth.",
                "Happy New Year! Wishing you lots of love and laughter in 2023 and success in reaching your goals!",
                "I hope your New Year celebrations are full of love and laughter. Wishing you all a fun-filled and healthy 2023!",
                "May the closeness of your loved ones, family, and friends fill your heart with joy. Happy New Year!",
                "In the New Year, never forget to thank your past years, because they enabled you to reach today! Without the stairs of the past, you cannot arrive at the future!",
                "A New Year is like a blank book, and the pen is in your hands. It is your chance to write a beautiful story for yourself. Happy New Year!",
                "As the New Year dawns, I hope it is filled with the promises of a brighter tomorrow. Happy New Year!",
                "Another year has passed, another year has come. I wish for you that, with every year, you achieve all of your dreams. Happy New Year!",
                "Wishing you 12 months of success, 52 weeks of laughter, 365 days of fun, 8,760 hours of joy, 525,600 minutes of good luck, and 31,536,000 seconds of happiness.",
                "Every end marks a new beginning. Keep your spirits and determination unshaken, and you shall always walk the glory road. With courage, faith and great effort, you shall achieve everything you desire. I wish you a Happy New Year.",
            };

            if(_lastMessageIndex < messages.Count)
            {
                int lastMessage = _lastMessageIndex;
                _lastMessageIndex++;
                return messages[lastMessage];
            }
            else
            {
                Random random = new Random();
                return messages[random.Next(0, messages.Count)];
            }
        }

        private EmbedBuilder CreateNewYearsEmbed(string lastAwardedGuardian)
        {
            string userMessage = $"{Context.User.Username} - ";
            string newYearsMessage = GetNewYearsMessage();
            int createdRaidCount = _raids.Count(x => x.CreatedByUserId == Context.User.Id);
            int joinedRaidCount = _raids.Count(x => x.Players.FirstOrDefault(x => x.UserId == Context.User.Id) != null);
            
            int createdFireteamCount = _fireteams.Count(x => x.CreatedByUserId == Context.User.Id);
            int joinedFireteamCount = _fireteams.Count(x => x.Players.FirstOrDefault(x => x.UserId == Context.User.Id) != null);

            StringBuilder glimmerBuilder = new StringBuilder();
            glimmerBuilder.AppendLine("");
            glimmerBuilder.AppendLine("You have been awarded 25 Glimmer to kick off the New Year!");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Created {createdRaidCount} Raids.");
            sb.AppendLine($"Joined {joinedRaidCount} Raids.");
            sb.AppendLine($"Created {createdFireteamCount} Fireteams.");
            sb.AppendLine($"Joined {joinedFireteamCount} Fireteams.");
            sb.AppendLine($"Last Awarded {lastAwardedGuardian}");

            EmbedBuilder builder = new EmbedBuilder();
            builder.WithColor(Color.Green);
            if(userMessage.Length + newYearsMessage.Length < 256)
            {
                builder.WithTitle(userMessage + newYearsMessage);
                builder.WithDescription(glimmerBuilder.ToString());
            }
            else
            {
                builder.WithDescription(userMessage + newYearsMessage + glimmerBuilder.ToString());
            }

            builder.AddField("Here are your stats for last year.", sb.ToString());
            return builder;
        }
    }
}
