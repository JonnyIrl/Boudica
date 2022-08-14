using Boudica.Database;
using Boudica.Database.Models;
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
