using Boudica.Database;
using Boudica.Database.Models;
using Boudica.Helpers;
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

        private const int CreatorPoints = 5;
        public MiscCommands(IServiceProvider services)
        {
            _insultService = services.GetRequiredService<InsultService>();
        }


        [Command("insult", RunMode = RunMode.Async)]
        public async Task InsultCommand([Remainder] string args)
        {
            if (args == null || (args.Contains("@") == false))
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command, supply a single users name like ;insult @SpecificUser").Build());
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
                        await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("You can only insult once per day, use it wisely!").Build());
                        return;
                    }

                    string insult = Insults.GetRandomInsult();
                    if (insult.Contains(ReverseUnoCard))
                    {
                        insult = insult.Replace("{userId}", $"<@{Context.User.Id}>");
                        await ReplyAsync(null, false, EmbedHelper.CreateSuccessReply($"{insult}").Build());
                    }
                    else
                    {
                        await ReplyAsync(null, false, EmbedHelper.CreateSuccessReply($"<@{userId}> {insult}").Build());
                    }

                    await _insultService.UpsertUsersInsult(userId, usersLastInsult);
                }
                else
                {
                    await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command, supply a single users name like ;insult @SpecificUser").Build());
                    return;
                }
            }
            catch (Exception ex)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command, supply a single users name like ;insult @SpecificUser").Build());
            }
        }

        [Command("compliment")]
        public async Task ComplimentCommand([Remainder] string args)
        {
            if (args == null || (args.Contains("@") == false))
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command, supply a single users name like ;compliment @SpecificUser").Build());
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
                        await ReplyAsync(null, false, EmbedHelper.CreateFailedReply($"<@{userId}> self praise is no praise, sorry not sorry!").Build());
                    }
                    else
                    {
                        string compliment = Compliments.GetRandomCompliment();
                        await ReplyAsync(null, false, EmbedHelper.CreateSuccessReply($"<@{userId}> {compliment}").Build());
                    }

                }
                else
                {
                    await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command, supply a single users name like ;compliment @SpecificUser").Build());
                    return;
                }
            }
            catch (Exception ex)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command, supply a single users name like ;compliment @SpecificUser").Build());
            }
        }

        [Command("shush")]
        public async Task ShushCommand([Remainder] string args)
        {
            if (args == null || (args.Contains("@") == false))
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command, supply a single users name like ;shush @SpecificUser").Build());
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
                        await ReplyAsync(null, false, EmbedHelper.CreateFailedReply($"With great power comes great responsibility. You have been timed out for 30 seconds get rekd!").Build());
                    }
                }
                else
                {
                    await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command, supply a single users name like ;shush @SpecificUser").Build());
                    return;
                }
            }
            catch (Exception ex)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command, supply a single users name like ;shush @SpecificUser").Build());
            }
        }


        [Command("coinflip")]
        public async Task CoinflipCommand()
        {
            await ReplyAsync("Invalid command, supply either heads or tails after the command like ;coinflip heads");
        }

        [Command("coinflip")]
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
