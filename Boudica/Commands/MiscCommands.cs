using Discord;
using Discord.Commands;
using Discord.Rest;
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
                fileName = "Images/Gifs/heads.gif";
            }
            else
            {
                fileName = "Images/Gifs/tails.gif";
            }
            await Context.Channel.SendFileAsync(fileName);
            await Task.Delay(3000);
            if(number == 0 && args.ToLower() == "heads")
            {
                await ReplyAsync("You guessed correctly <@" + Context.User.Id + ">!");
            }
            else if (number == 1 && args.ToLower() == "tails")
            {
                await ReplyAsync("You guessed correctly <@" + Context.User.Id + ">!");
            }
            else
            {
                await ReplyAsync("Better luck next time <@" + Context.User.Id + ">!");
            }
        }
    }
}
