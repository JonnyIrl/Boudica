using Boudica.Attributes;
using Boudica.Classes;
using Boudica.Enums;
using Boudica.Helpers;
using Boudica.MongoDB.Models;
using Boudica.Services;
using Boudica.Structs;
using CoreHtmlToImage;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = Discord.Color;

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
        private readonly BotChallengeService _botChallengeService;
        private readonly MiscService _miscService;

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
            _botChallengeService = services.GetRequiredService<BotChallengeService>();
            _miscService = services.GetRequiredService<MiscService>();
        }

        [SlashCommand("insult", "Choose a player to insult")]
        [Suspended]
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
                if (usersLastInsult != null && usersLastInsult.DateTimeLastInsulted.AddHours(ConfigHelper.HourOffset).Date == ConfigHelper.GetDateTime().Date)
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
        [Suspended]
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
        [BotChannelOnly]
        [Suspended]
        public async Task JokeCommand()
        {
            //await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"Jokes currently going through filtering..").Build());
            //return;
            string joke = Jokes.GetRandomJoke();
            await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"{joke}").Build());
        }

        [SlashCommand("coinflip", "Random coinflip")]
        [Suspended]
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
        [BotChannelOnly]
        [Suspended]
        public async Task DailyGift()
        {
            DailyGift dailyGift = await _dailyGiftService.Get(Context.User.Id);
            if (dailyGift != null && dailyGift.DateTimeLastGifted.AddHours(ConfigHelper.HourOffset).Date == ConfigHelper.GetDateTime().Date)
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
            int amount = random.Next(10, 21);
            await _guardianService.IncreaseGlimmerAsync(Context.User.Id, Context.User.Username, amount);
            await _dailyGiftService.UpsertUsersDailyGift(Context.User.Id);
            await _historyService.InsertHistoryRecord(Context.User.Id, null, HistoryType.DailyGift, amount);
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

        [SlashCommand("award-glimmer", "Command to give X amount of glimmer to somebody")]
        [DefaultMemberPermissions(GuildPermission.KickMembers)]
        [ClanAdminOrMe]
        public async Task AdminAward(SocketGuildUser user, [Summary("glimmerAmount", "Amount of Glimmer to add or deduct away")]int glimmerAmount)
        {
            if(user.IsBot)
            {
                await RespondAsync("Bots cannot be awarded", ephemeral: true);
                return;
            }
            await _historyService.InsertHistoryRecord(Context.User.Id, user.Id, HistoryType.AdminAward, glimmerAmount);
            if (glimmerAmount <= 0)
            {
                await _guardianService.RemoveGlimmerAsync(user.Id, glimmerAmount);
                await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"{user.Username}, you have been deducted {glimmerAmount} Glimmer!").Build());
            }
            else
            {
                await _guardianService.IncreaseGlimmerAsync(user.Id, user.Username, glimmerAmount);
                await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"{user.Username}, you have been awarded {glimmerAmount} Glimmer!").Build());
            }
            
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

        [SlashCommand("my-profile", "My profile command - Test")]
        public async Task MyProfile()
        {
            await DeferAsync();
            await FollowupWithFileAsync(await ConvertHtmlToImage());
        }

        public async Task<string> ConvertHtmlToImage()
        {
            string html = string.Empty;
            using (StreamReader sr = new StreamReader(BoudicaConfig.ProfileDirectory + "index.html"))
            {
                html = sr.ReadToEnd();
            }
            html = await SwapoutHTMLValues(html);

            string htmlFile = GetHtmlFileName(Context.User.Id);
            using (StreamWriter sw = new StreamWriter(htmlFile, false))
            {
                await sw.WriteAsync(html);
            }

            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true
            });
            var page = await browser.NewPageAsync();
            await page.GoToAsync("file:///" + htmlFile);
            await page.SetViewportAsync(new ViewPortOptions
            {
                Width = 520,
                Height = 300
            });
            await page.ScreenshotAsync(GetImageFileName(Context.User.Id));
            await browser.DisposeAsync();


            return GetImageFileName(Context.User.Id);
        }

        private async Task<string> SwapoutHTMLValues(string html)
        {
            const string ProfileNameReplacement = "%Username%";
            const string ProfilePictureUrlReplacement = "%ProfilePictureUrl%";
            const string GlimmerReplacement = "%GlimmerCount%";
            const string RaidReplacement = "%RaidCount%";
            const string FireteamReplacement = "%FireteamCount%";
            const string AwardReplacement = "%AwardCount%";
            const string GlimmerWonReplacement = "%GlimmerWon%";
            const string GlimmerLostReplacement = "%GlimmerLost%";
            const string WinRateReplacement = "%WinRate%";
            int glimmerCount = await _guardianService.GetGuardianGlimmer(Context.User.Id);
            long raidCount = await _activityService.GetRaidCount(Context.User.Id);
            long fireteamCount = await _activityService.GetFireteamCount(Context.User.Id);
            long awardCount = await _historyService.GetAwardedCountAsync(Context.User.Id);
            UserBotChallengeResult userBotChallengeResult = GetUsersWonGlimmer(await _botChallengeService.GetUsersChallengesAsync(Context.User.Id));
            return html
                .Replace(ProfilePictureUrlReplacement, Context.User.GetAvatarUrl(Discord.ImageFormat.Png, 64))
                .Replace(ProfileNameReplacement, Context.User.Username)
                .Replace(GlimmerReplacement, glimmerCount.ToString())
                .Replace(RaidReplacement, raidCount.ToString())
                .Replace(FireteamReplacement, fireteamCount.ToString())
                .Replace(AwardReplacement, awardCount.ToString())
                .Replace(GlimmerWonReplacement, userBotChallengeResult.GlimmerWon.ToString())
                .Replace(GlimmerLostReplacement, userBotChallengeResult.GlimmerLost.ToString())
                .Replace(WinRateReplacement, GetWinRatePercentage(userBotChallengeResult).ToString() + "%");
        }

        private int GetWinRatePercentage(UserBotChallengeResult result)
        {
            if (result.GamesLost + result.GamesWon == 0) return 0;
            return (int) Math.Round((100.0f / (result.GamesWon + result.GamesLost)) * result.GamesWon);
        }

        private UserBotChallengeResult GetUsersWonGlimmer(List<BotChallenge> botChallenges)
        {
            UserBotChallengeResult result = new UserBotChallengeResult();
            foreach (BotChallenge botChallenge in botChallenges)
            {
                if(botChallenge.WinnerId == null)
                {
                    result.GamesLost++;
                    result.GlimmerLost += botChallenge.Wager;
                }
                else
                {
                    result.GamesWon++;
                    result.GlimmerWon += botChallenge.Wager;
                }
            }
            return result;
        }

        private string GetHtmlFileName(ulong userId)
        {
            return BoudicaConfig.ProfileDirectory + userId + ".html";
        }

        private string GetImageFileName(ulong userId)
        {
            return BoudicaConfig.ProfileDirectory + userId + ".png";
        }

        [SlashCommand("member-stats", "Only useable by Jonny")]
        [OnlyMe]
        public async Task MemberStats()
        {
            await DeferAsync();
            const string Raid = "Raid Fanatics";
            const string Nightfall = "Nightfall Enthusiasts";
            const string Gambit = "Gambit Hustlers";
            const string Crucible = "Crucible Contenders";
            const string Activity = "Activity Aficionados";
            const string Dungeon = "Dungeon Challengers";
            IRole raidRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == Raid);
            IRole nightFallRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == Nightfall);
            IRole gambitRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == Gambit);
            IRole crucibleRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == Crucible);
            IRole activityRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == Activity);
            IRole dungeonRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == Dungeon);

            if (raidRole == null || nightFallRole == null || gambitRole == null || crucibleRole == null || activityRole == null || dungeonRole == null)
            {
                await FollowupAsync("No role found");
                return;
            }

            List<SocketGuildUser> users = Context.Guild.Users.ToList();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Id, Username, Raid, Nightfall, Gambit, Crucible, Acticity, Dunegon");
            foreach(SocketGuildUser user in users)
            {
                UserRoleExport userExport = new UserRoleExport();
                userExport.Id = user.Id;
                userExport.Username = user.Username.Replace(",", string.Empty);
                if (user.Roles.Contains(raidRole))
                    userExport.Raid = true;
                if (user.Roles.Contains(nightFallRole))
                    userExport.Nightfall= true;
                if (user.Roles.Contains(gambitRole))
                    userExport.Gambit = true;
                if (user.Roles.Contains(crucibleRole))
                    userExport.Crucible = true;
                if (user.Roles.Contains(activityRole))
                    userExport.Activity = true;
                if (user.Roles.Contains(dungeonRole))
                    userExport.Dungeon = true;

                sb.AppendLine(userExport.ToString());
            }

            using(StreamWriter sw = new StreamWriter("MemberStats.txt"))
            {
                sw.Write(sb.ToString());
            }

            await FollowupAsync("Done!", ephemeral: true);
        }

        [SlashCommand("echo", "Boudica will echo your message in a channel")]
        [DefaultMemberPermissions(GuildPermission.KickMembers)]
        public async Task Echo(string channel, string message)
        {
            if(string.IsNullOrEmpty(message))
            {
                await RespondAsync("You must send a message", ephemeral: true);
                return;
            }
            if(string.IsNullOrEmpty(channel))
            {
                await RespondAsync("Invalid Channel", ephemeral: true);
                return;
            }    
            string channelIdString = channel.Replace("<#", string.Empty).Replace(">", string.Empty);
            if(ulong.TryParse(channelIdString, out ulong channelId) == false)
            {
                await RespondAsync("Invalid Channel", ephemeral: true);
                return;
            }
            ITextChannel textChannel = Context.Guild.GetTextChannel(channelId);
            if(textChannel == null)
            {
                await RespondAsync("Invalid Channel", ephemeral: true);
                return;
            }
            await textChannel.SendMessageAsync(message);
            await RespondAsync($"{Context.User.Username} said the following in {channel}:\n{message}");
            await _historyService.InsertHistoryRecord(_historyService.CreateHistoryRecord(Context.User.Id, null, HistoryType.Echo));
        }

        //[SlashCommand("signup", "Sign up for Day One Raid")]
        //public async Task SignUpDayOne([Summary("happyToLFG", "You are happy or not to LFG if there is not enough players to fill a full group")]bool happyToLFG)
        //{
        //    bool alreadySignedUp = await _miscService.AlreadySignedUp(Context.User.Id);
        //    if (alreadySignedUp)
        //    {
        //        await RespondAsync("You are already signed up", ephemeral: true);
        //        return;
        //    }

        //    try
        //    {
        //        await _miscService.SignUp(Context.User.Id, Context.User.Username, happyToLFG);
        //        await RespondAsync("Successfully signed up! We will let you know your team in advance you don't have to do anything else. If you find a team before hand please let a Clan Admin know!", ephemeral: true);
        //    }
        //    catch
        //    {
        //        await RespondAsync("Could not sign up.. something went wrong talk to Jonny", ephemeral: true);
        //    }

        //}

        [SlashCommand("suspend", "This will suspend the user from making any commands for an amount of time")]
        [DefaultMemberPermissions(GuildPermission.KickMembers)]
        public async Task SuspendUser(SocketGuildUser userToSuspend, [Summary("hoursToSuspend", "Hours to suspend the user for")]int hoursToSuspend)
        {
            if(userToSuspend.IsBot)
            {
                await RespondAsync("You can't suspend a bot");
                return;
            }
            if(hoursToSuspend <= 0)
            {
                await RespondAsync("Enter a valid amount of hours to suspend", ephemeral: true);
                return;
            }

            SuspendedUser suspendedUser = await _miscService.SuspendUser(userToSuspend.Id, hoursToSuspend, Context.User.Id);
            if(suspendedUser == null)
            {
                await RespondAsync("Could not suspend User, try again or report issue to Jonny", ephemeral: true);
                return; 
            }
            else
            {
                await RespondAsync($"<@{userToSuspend.Id}> has been suspended from making commands until {suspendedUser.DateTimeSuspendedUntil.ToString("dd/MM/yyyy HH:mm")}");
                await _historyService.InsertHistoryRecord(_historyService.CreateHistoryRecord(Context.User.Id, userToSuspend.Id, HistoryType.Suspended));
            }
        }

        [SlashCommand("drama-report", "Days since last clan drama.")]

        [DefaultMemberPermissions(GuildPermission.KickMembers)]
        [ConclaveSeraphimChannelOnly]
        public async Task LastDramaReport()
        {
            DramaReport report = await _miscService.GetDramaReport();
            if (report == null)
            {
                await _miscService.ResetDramaReport();
                report = new DramaReport() { Start = DateTime.UtcNow };
            }

            int dayDifference = (int)DateTime.UtcNow.Subtract(report.Start).TotalDays;
            if (dayDifference < 0) dayDifference = 0;
            StringBuilder sb = new StringBuilder();
            if (dayDifference == 1)
                sb.Append($"It has been {dayDifference} day since the last clan drama.. ");
            else
                sb.Append($"It has been {dayDifference} days since the last clan drama.. ");

            switch (dayDifference)
            {
                case 0:
                    sb.Append("oof wonder what happened this time?");
                    break;
                case 1:
                    sb.Append("forward march!");
                    break;
                case 2:
                    sb.Append("making progress!");
                    break;
                case 3:
                    sb.Append("keep going!");
                    break;
                case 4:
                    sb.Append("steady progress!");
                    break;
                case 5:
                    sb.Append("it's a miracle!");
                    break;
                case 6:
                    sb.Append("nearly a full week keep up the good work!");
                    break;
                case 7:
                    sb.Append("one week! I don't know how you did it but keep doing it!");
                    break;
                case 8:
                    sb.Append("now you're just showing off!");
                    break;
                case 9:
                    sb.Append("did Zodiac leave?");
                    break;
                case 10:
                    sb.Append("did pegging get banned?");
                    break;
                case 11:
                    sb.Append("don't let anybody get drunk!");
                    break;
                case 12:
                    sb.Append("ok, now you're just showing off!");
                    break;
                case 13:
                    sb.Append("ok this is unheard of!");
                    break;
                case 14:
                    sb.Append("two weeks!?! Seriously??? oh look up.. is that.. is that a.. pig??");
                    break;
                default:
                    sb.Append("I've nothing more left to say, either the clan is dead or everyone else is, either way no drama woohoo!!");
                    break;
            }

            await RespondAsync(sb.ToString());
        }

        
        [SlashCommand("reset-drama-report", "Reset the Days since last clan drama.")]
        [DefaultMemberPermissions(GuildPermission.KickMembers)]
        [ConclaveSeraphimChannelOnly]
        public async Task ResetDramaReport()
        {
            await _miscService.ResetDramaReport();
            await RespondAsync("*sigh* who did it now? Days since last drama has been reset");
        }

    }
}
