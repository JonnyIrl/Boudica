using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Commands
{
    public class HelpCommands : ModuleBase
    {
        private const string Prefix = ";";
        [Command("help")]
        public async Task HelpCommand()
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = "Here's a list of commands!";
            builder.Description = $"\n";
            builder.AddField("Create Raid", $"{Prefix}create raid some text to describe your raid");
            builder.AddField("Edit Raid", $"{Prefix}edit raid Id some new text here");
            builder.AddField("Close Raid", $"{Prefix}close raid Id");
            builder.AddField("Create Fireteam", $"{Prefix}create fireteam (the number of players between 2 and 6 inclusive) some text to describe your fireteam");
            builder.AddField("Edit Fireteam", $"{Prefix}edit fireteam Id some new text here");
            builder.AddField("Close Fireteam", $"{Prefix}close fireteam Id");
            builder.AddField("Insult", $"{Prefix}insult @Somebody");
            builder.AddField("Coinflip", $"{Prefix}coinflip heads (or tails)");
            builder.AddField("Magic 8 Ball", $"{Prefix}ask whatever question you may have");
            builder.WithColor(Color.Green);
            await ReplyAsync(null, false, builder.Build());
        }
    }
}
