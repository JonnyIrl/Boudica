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
        private readonly AwardedGuardianService awardedGuardianService;
        private readonly GuardianService _guardianService;
        private readonly DailyGiftService _dailyGiftService;

        private const int CreatorPoints = 5;
        public MiscCommands(IServiceProvider services)
        {
            _insultService = services.GetRequiredService<InsultService>();
            awardedGuardianService = services.GetRequiredService<AwardedGuardianService>();
            _guardianService = services.GetRequiredService<GuardianService>();
            _dailyGiftService = services.GetRequiredService<DailyGiftService>();
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

        [SlashCommand("daily-gift", "Every day, get a free daily gift of 2-10 Glimmer!")]
        public async Task DailyGift()
        {
            const string OneGlimmer = "https://i.imgur.com/bRbtWlE.gif";
            const string TwoGlimmer = "https://i.imgur.com/KHkbILH.gif";
            const string ThreeGlimmer = "https://i.imgur.com/8M22qQz.gif";
            const string FourGlimmer = "https://i.imgur.com/iwXpEno.gif";
            const string FiveGlimmer = "https://i.imgur.com/tvjtrm4.gif";
            const string SixGlimmer = "https://i.imgur.com/wlxZVcq.gif";
            const string SevenGlimmer = "https://i.imgur.com/LbYqjld.gif";
            const string EightGlimmer = "https://i.imgur.com/v8e27qk.gif";
            const string NineGlimmer = "https://i.imgur.com/iQ4k7ur.gif";
            const string TenGlimmer = "https://i.imgur.com/KsRZ6eA.gif";

            DailyGift dailyGift = await _dailyGiftService.Get(Context.User.Id);
            if(dailyGift != null && dailyGift.DateTimeLastGifted.Date == DateTime.UtcNow.Date)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("You can only get one gift per day.").Build());
                return;
            }
            Random random = new Random();
            int amount = random.Next(2, 11);
            await _guardianService.IncreaseGlimmerAsync(Context.User.Id, Context.User.Username, amount);
            await _dailyGiftService.UpsertUsersDailyGift(Context.User.Id);
            switch(amount)
            {
                case 1:
                    await RespondAsync(OneGlimmer);
                    break;
                case 2:
                    await RespondAsync(TwoGlimmer);
                    break;
                case 3:
                    await RespondAsync(ThreeGlimmer);
                    break;
                case 4:
                    await RespondAsync(FourGlimmer);
                    break;
                case 5:
                    await RespondAsync(FiveGlimmer);
                    break;
                case 6:
                    await RespondAsync(SixGlimmer);
                    break;
                case 7:
                    await RespondAsync(SevenGlimmer);
                    break;
                case 8:
                    await RespondAsync(EightGlimmer);
                    break;
                case 9:
                    await RespondAsync(NineGlimmer);
                    break;
                case 10:
                    await RespondAsync(TenGlimmer);
                    break;
                default:
                    await RespondAsync(embed: EmbedHelper.CreateFailedReply("Something went wrong..").Build());
                    break;
            }
            Thread.Sleep(5000);
            await FollowupAsync(embed: EmbedHelper.CreateSuccessReply($"You have received {amount} Glimmer as your daily gift!").Build());
        }
    }
}
