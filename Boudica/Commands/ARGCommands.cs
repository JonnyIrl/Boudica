using Boudica.Database;
using Boudica.Database.Models;
using Boudica.Helpers;
using Boudica.Services;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Commands
{
    public class ARGCommands : ModuleBase
    {
        private readonly GuardianService _guardianService;
        private readonly ItemService _itemService;
        private readonly IConfiguration _config;

        public ARGCommands(IServiceProvider services)
        {
            _guardianService = services.GetRequiredService<GuardianService>();
            _itemService = services.GetRequiredService<ItemService>();
            _config = services.GetRequiredService<IConfiguration>();
        }

        [Command("increase")]
        public async Task IncreaseGlimmer([Remainder] string args)
        {
            if (args == null || args.Contains("glimmer") == false)
            {
                await ReplyAsync("Invalid command, example is ;increase glimmer 50 @Person");
                return;
            }
            var split = args.Split(" ");
            if (split.Length < 3) 
            {
                await ReplyAsync("Invalid command, example is ;increase glimmer 50 @Person");
                return;
            }

            if (int.TryParse(split[1], out int result))
            {
                string userIdString = split[2].Replace("@", string.Empty).Replace(">", string.Empty).Replace("<", string.Empty);
                if (ulong.TryParse(userIdString, out ulong userId))
                {
                    await _guardianService.IncreaseGlimmer(userId, result);
                    return;
                }
                else
                {
                    await ReplyAsync("Invalid command, example is ;increase glimmer 50 @Person");
                    return;
                }
            }

            await ReplyAsync("Invalid command, example is ;increase glimmer 50 @Person");

        }

        [Command("leaderboard")]
        public async Task GetLeaderboard()
        {
            List<Guardian> guardians = await _guardianService.GetLeaderboard();
            if(guardians.Any() == false)
            {
                await ReplyAsync("There is nobody in the leaderboards... yet");
                return;
            }

            var embed = new EmbedBuilder
            {
                Title = "Leaderboard",
                Color = Color.Blue
            };

            StringBuilder stringBuilder = new StringBuilder();
            int rank = 1;
            foreach(Guardian guardian in guardians)
            {
                stringBuilder.AppendLine($"{GetRank(rank)} <@{guardian.UserId}>");
                rank++;
            }
            embed.Description = stringBuilder.ToString();

            embed.WithFooter(footer => footer.Text = "Increase your rank by completing discord tasks and earning glimmer");
            await ReplyAsync(embed: embed.Build());
        }

        [Command("create")]
        public async Task Create([Remainder] string args)
        {
            if (args == null || args.Contains("item") == false)
            {
                await ReplyAsync("Invalid command, example is ;create item itemJson");
                return;
            }
            var split = args.Split("item");
            await CreateItem(split[split.Length - 1]);
        }

        private async Task CreateItem(string itemJson)
        {
            bool result = await _itemService.CreateItem(itemJson);
            if(result)
            {
                await ReplyAsync(embed: EmbedHelper.CreateSuccessReply("Item was created!").Build());
            }
            else
            {
                await ReplyAsync(embed: EmbedHelper.CreateFailedReply("Failed to create item!").Build());
            }
        }


        private string GetRank(int rank)
        {
            switch(rank)
            {
                case 1:
                    return "1st.";
                case 2:
                    return "2nd.";
                case 3:
                    return "3rd.";
                case 4:
                    return "4th.";
                case 5:
                    return "5th.";
                case 6:
                    return "6th.";
                case 7:
                    return "7th.";
                case 8:
                    return "8th.";
                case 9:
                    return "9th.";
                case 10:
                    return "10th.";
            }

            return "N/A";
        }
    }
}
