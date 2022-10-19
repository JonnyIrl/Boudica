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

        private const int CreatorPoints = 5;
        public MiscCommands(IServiceProvider services)
        {
            _insultService = services.GetRequiredService<InsultService>();
            awardedGuardianService = services.GetRequiredService<AwardedGuardianService>();
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
    }
}
