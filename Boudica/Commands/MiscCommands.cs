using Boudica.Helpers;
using Boudica.MongoDB.Models;
using Boudica.Services;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Commands
{
    public class MiscCommands : ModuleBase
    {
        private const string ReverseUnoCard = "Reverse uno card";

        private readonly InsultService _insultService;
        private readonly AwardedGuardianService awardedGuardianService;

        private const int CreatorPoints = 5;
        public MiscCommands(IServiceProvider services)
        {
            _insultService = services.GetRequiredService<InsultService>();
            awardedGuardianService = services.GetRequiredService<AwardedGuardianService>();
        }

        [Command("insult")]
        public async Task InsultCommand([Remainder] string args)
        {
            if (args == null || (args.Contains("@") == false))
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command, supply a single users name like ;insult @SpecificUser").Build());
                return;
            }

            int startIndex = args.IndexOf("<@");
            int endIndex = args.IndexOf(">");
            try
            {
                if (ulong.TryParse(args.Substring(startIndex + 2, endIndex - (startIndex + 2)), out ulong userId))
                {
                    Insult usersLastInsult = await _insultService.Get(Context.User.Id);
                    if(usersLastInsult != null && usersLastInsult.DateTimeLastInsulted.Date == DateTime.UtcNow.Date)
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
                        await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"<@{userId}> {insult}").Build());
                    }

                    await _insultService.UpsertUsersInsult(Context.User.Id);
                }
                else
                {
                    await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command, supply a single users name like ;insult @SpecificUser").Build());
                    return;
                }
            }
            catch (Exception ex)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command, supply a single users name like ;insult @SpecificUser").Build());
            }
        }

        [Command("compliment")]
        [Alias("complement")]
        public async Task ComplimentCommand([Remainder] string args)
        {
            if (args == null || (args.Contains("@") == false))
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command, supply a single users name like ;compliment @SpecificUser").Build());
                return;
            }

            int startIndex = args.IndexOf("<@");
            int endIndex = args.IndexOf(">");
            try
            {
                if (ulong.TryParse(args.Substring(startIndex + 2, endIndex - (startIndex + 2)), out ulong userId))
                {
                    if (userId == Context.User.Id)
                    {
                        await RespondAsync(embed: EmbedHelper.CreateFailedReply($"<@{userId}> self praise is no praise, sorry not sorry!").Build());
                    }
                    else
                    {
                        string compliment = Compliments.GetRandomCompliment();
                        await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"<@{userId}> {compliment}").Build());
                    }

                }
                else
                {
                    await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command, supply a single users name like ;compliment @SpecificUser").Build());
                    return;
                }
            }
            catch (Exception ex)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command, supply a single users name like ;compliment @SpecificUser").Build());
            }
        }

        [Command("joke")]
        public async Task JokeCommand()
        {
            //await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"Jokes currently going through filtering..").Build());
            //return;
            string joke = Jokes.GetRandomJoke();
            await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"{joke}").Build());
        }

        [Command("shush")]
        public async Task ShushCommand([Remainder] string args)
        {
            if (args == null || (args.Contains("@") == false))
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command, supply a single users name like ;shush @SpecificUser").Build());
                return;
            }

            int startIndex = args.IndexOf("<@");
            int endIndex = args.IndexOf(">");
            try
            {
                if (ulong.TryParse(args.Substring(startIndex + 2, endIndex - (startIndex + 2)), out ulong userId))
                {
                    IGuildUser guildUser = await Context.Guild.GetCurrentUserAsync();
                    if (guildUser != null)
                    {
                        await guildUser.SetTimeOutAsync(TimeSpan.FromSeconds(30));
                        await RespondAsync(embed: EmbedHelper.CreateFailedReply($"With great power comes great responsibility. You have been timed out for 30 seconds get rekd!").Build());
                    }
                }
                else
                {
                    await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command, supply a single users name like ;shush @SpecificUser").Build());
                    return;
                }
            }
            catch (Exception ex)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command, supply a single users name like ;shush @SpecificUser").Build());
            }
        }

        [Command("coinflip", RunMode =RunMode.Async)]
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

            IUserMessage userMessage = await Context.Channel.SendMessageAsync(embed: embed);
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

        [Command("coinflip", RunMode = RunMode.Async)]
        public async Task CoinflipCommand([Remainder] string args)
        {
            if (args == null || (args.ToLower() != "heads" && args.ToLower() != "tails"))
            {
                await ReplyAsync("Invalid command, supply either heads or tails after the command like +coinflip heads");
                return;
            }

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

            IUserMessage userMessage = await Context.Channel.SendMessageAsync(embed: embed);
            await Task.Delay(3000);

            await userMessage.ModifyAsync(x =>
            {
                EmbedBuilder embed = new EmbedBuilder();
                embed.ImageUrl = $"{fileName}";
                if (number == 0 && args.ToLower() == "heads")
                {
                    embed.WithDescription("You guessed correctly <@" + Context.User.Id + ">!");
                    embed.WithColor(Color.Green);
                }
                else if (number == 1 && args.ToLower() == "tails")
                {
                    embed.WithDescription("You guessed correctly <@" + Context.User.Id + ">!");
                    embed.WithColor(Color.Green);
                }
                else
                {
                    embed.WithDescription("Better luck next time <@" + Context.User.Id + ">!");
                    embed.WithColor(Color.Red);
                }
                x.Embed = embed.Build();
            });
        }
    }
}
