using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Commands
{
    [Group("create")]
    public class ActivityCommands : ModuleBase
    {
        [Command("raid")]
        public async Task CreateRaidCommand([Remainder] string args)
        {
            if (args == null)
            {
                await ReplyAsync("Invalid command arguments, supply the raid and a description for your raid e.g. create raid Vow of Disciple Tuesday 28th 6pm");
            }
            var embed = new EmbedBuilder();

            string[] split = args.Split('\n');
            bool title = false;
            if (split.Length > 1)
            {
                embed.Title = split[0];
                title = true;
            }

            embed.WithColor(new Color(0, 255, 0));
            StringBuilder sb = new StringBuilder();
            int startingPostion = title ? 1 : 0;

            for (int i = startingPostion; i < split.Length; i++)
            {
                sb.AppendLine(split[i]);
            }


            sb.AppendLine();

            var user = Context.User;

            sb.AppendLine($"Players");
            sb.AppendLine($"<@{user.Id}>");

            sb.AppendLine("");
            sb.AppendLine("Subs");

            embed.Description = sb.ToString();
            embed.Footer = new EmbedFooterBuilder()
            {
                Text = $"Use J to Join | Use S to Sub.\nA max of 6 players may join a raid"
            };


            // this will reply with the embed
            IUserMessage newMessage = await ReplyAsync(null, false, embed.Build());
            await newMessage.AddReactionsAsync(new List<IEmote>()
            {
                new Emoji("🇯"),
                new Emoji("🇸")
            });
        }
    }
}
