using Boudica.Classes;
using Boudica.Helpers;
using Boudica.MongoDB.Models;
using Boudica.Services;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Boudica.Commands
{
    public class ARGCommands : ModuleBase
    {
        private readonly IConfiguration _config;
        private readonly GuardianService _guardianService;
        private readonly AwardedGuardianService _awardedGuardianService;

        public ARGCommands(IServiceProvider services)
        {
            _guardianService = services.GetRequiredService<GuardianService>();
            _awardedGuardianService = services.GetRequiredService<AwardedGuardianService>();
            //_itemService = services.GetRequiredService<ItemService>();
            //_eververseService = services.GetRequiredService<EververseService>();
            //_inventoryService = services.GetRequiredService<InventoryService>();
            //_config = services.GetRequiredService<IConfiguration>();
        }

        //[Command("increase")]
        //public async Task IncreaseGlimmer([Remainder] string args)
        //{
        //    if (args == null || args.Contains("glimmer") == false)
        //    {
        //        await ReplyAsync("Invalid command, example is ;increase glimmer 50 @Person");
        //        return;
        //    }
        //    var split = args.Split(" ");
        //    if (split.Length < 3) 
        //    {
        //        await ReplyAsync("Invalid command, example is ;increase glimmer 50 @Person");
        //        return;
        //    }

        //    if (int.TryParse(split[1], out int result))
        //    {
        //        string userIdString = split[2].Replace("@", string.Empty).Replace(">", string.Empty).Replace("<", string.Empty);
        //        if (ulong.TryParse(userIdString, out ulong userId))
        //        {
        //            await _guardianService.IncreaseGlimmer(userId, result);
        //            return;
        //        }
        //        else
        //        {
        //            await ReplyAsync("Invalid command, example is ;increase glimmer 50 @Person");
        //            return;
        //        }
        //    }

        //    await ReplyAsync("Invalid command, example is ;increase glimmer 50 @Person");

        //}

        [Command("leaderboard")]
        public async Task GetLeaderboard()
        {
            List<Guardian> guardians = await _guardianService.GetLeaderboard(Context.User.Id);
            if (guardians.Any() == false)
            {
                await ReplyAsync("There is nobody in the leaderboards... yet");
                return;
            }
            ulong glimmerId = 0;
#if DEBUG
            glimmerId = 1009200271475347567;
#else
            glimmerId = 728197708074188802;
#endif
            bool parsedEmote = Emote.TryParse($"<:misc_glimmer:{glimmerId}>", out Emote glimmerEmote);

            var embed = new EmbedBuilder
            {
                Color = Color.Blue
            };

            StringBuilder stringBuilder = new StringBuilder();
            foreach (Guardian guardian in guardians)
            {
                IGuildUser user = await Context.Guild.GetUserAsync(guardian.Id);
                if (user == null) continue;

                //Bold the person who issued the command
                string userId = user.Id == Context.User.Id ? $"**{user.DisplayName}**" : $"{user.DisplayName}";

                if(parsedEmote == false)
                    stringBuilder.AppendLine($"{userId} {string.Format("{0:n0}", guardian.Glimmer)} Glimmer");
                else
                    stringBuilder.AppendLine($"{userId} {glimmerEmote} {string.Format("{0:n0}", guardian.Glimmer)}");
            }

            embed.Description = stringBuilder.ToString();

            embed.WithFooter(footer => footer.Text = "Increase your Glimmer by creating and joining activities. Glimmer can be used in the Lightfall clan event.");
            await ReplyAsync(embed: embed.Build());
        }

        [Command("award")]
        public async Task AwardPlayer([Remainder] string args)
        {
            ActivityUser user = await GetFirstMentionedUser(args);
            if(user == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command, provide a user to get rewarded like ;award @User").Build());
                return;
            }

            if(user.UserId == Context.User.Id)
            {
                //Reverse Uno
                await ReplyAsync("https://media.giphy.com/media/Wt6kNaMjofj1jHkF7t/giphy.gif");
                await Task.Delay(500);
                await _guardianService.RemoveGlimmerAsync(user.UserId, 3);
                await ReplyAsync($"<@{user.UserId}>, your fellow clanmate has awarded you some glimmer! *SIKE*, you have lost 3 glimmer.. nice try!");
                return;
            }

            Tuple<bool, string> canAwardPlayer = await _awardedGuardianService.CanAwardGlimmerToGuardian(Context.User.Id, user.UserId);
            if(canAwardPlayer.Item1 == false)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply(canAwardPlayer.Item2).Build());
                return;
            }

            await _awardedGuardianService.AwardGuardian(Context.User.Id, user.UserId, user.DisplayName);
            await ReplyAsync($"<@{user.UserId}>, your fellow clanmate has awarded you some glimmer!");
        }

        [RequireUserPermission(GuildPermission.KickMembers)]
        [Command("supersub")]
        public async Task AwardSuperSubPlayer([Remainder] string args)
        {
            ActivityUser user = await GetFirstMentionedUser(args);
            if (user == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command, provide a user to get rewarded like ;supersub @User").Build());
                return;
            }

            if (user.UserId == Context.User.Id)
            {
                await _guardianService.RemoveGlimmerAsync(user.UserId, 9);
                await ReplyAsync($"<@{user.UserId}>, you have lost 9 glimmer!");
                return;
            }

            await _awardedGuardianService.AwardGuardian(Context.User.Id, user.UserId, user.DisplayName, 2);
            await ReplyAsync($"<@{user.UserId}>, your fellow clanmate has awarded you some glimmer for being a super sub!");
        }

        private async Task<ActivityUser> GetFirstMentionedUser(string args)
        {
            string sanitisedSplit = Regex.Replace(args, @"[(?<=\<)(.*?)(?=\>)]", string.Empty);
            List<ActivityUser> activityUsers = new List<ActivityUser>();
            if (sanitisedSplit.Contains("@") == false) return null;
            string[] users = sanitisedSplit.Split('@');
            foreach (string user in users)
            {
                string sanitisedUser = string.Empty;
                int space = user.IndexOf(' ');
                if (space == -1)
                    sanitisedUser = user.Trim();
                else
                {
                    sanitisedUser = user.Substring(0, space).Trim();
                }
                if (string.IsNullOrEmpty(sanitisedUser)) continue;
                if (IsDigitsOnly(sanitisedUser) == false) continue;

                if (ulong.TryParse(sanitisedUser, out ulong userId))
                {
                    IGuildUser guildUser = await Context.Guild.GetUserAsync(userId);
                    if (guildUser != null)
                    {
                        if (activityUsers.FirstOrDefault(x => x.UserId == userId) == null)
                            return new ActivityUser(userId, guildUser.DisplayName);
                    }
                }
            }

            return null;
        }

        bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return true;
        }

        //[Command("createitem")]
        //public async Task Create([Remainder] string args)
        //{
        //    if (args == null || args.Contains("item") == false)
        //    {
        //        await ReplyAsync("Invalid command, example is ;create item itemJson");
        //        return;
        //    }
        //    var split = args.Split("item");
        //    await CreateItem(split[split.Length - 1]);
        //}

        //private async Task CreateItem(string itemJson)
        //{
        //    bool result = await _itemService.CreateItem(itemJson);
        //    if(result)
        //    {
        //        await ReplyAsync(embed: EmbedHelper.CreateSuccessReply("Item was created!").Build());
        //    }
        //    else
        //    {
        //        await ReplyAsync(embed: EmbedHelper.CreateFailedReply("Failed to create item!").Build());
        //    }
        //}

        private string GetRank(int rank)
        {
            switch (rank)
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


        //[Command("inventory")]
        //public async Task Inventory()
        //{
        //    Guardian guardian = await _guardianService.Get(Context.User.Id);
        //    if(guardian == null)
        //    {
        //        await ReplyAsync("Could not find Guardian.. blame Jonny");
        //        return;
        //    }

        //    List<Item> inventoryItems = await _inventoryService.GetAllItems(Context.User.Id);
        //    if(inventoryItems == null || inventoryItems.Any() == false)
        //    {
        //        await ReplyAsync(embed: EmbedHelper.CreateFailedReply("You don't have anything in your inventory. You can buy items from Eververse!").Build());
        //        return;
        //    }

        //    await ReplyAsync(embed: CreateInventoryBuilder(inventoryItems, guardian.Glimmer).Build());

        //}

        //[Command("eververse")]
        //public async Task Eververse()
        //{
        //    List<Eververse> allItems = await _eververseService.GetAll();
        //    await ReplyAsync(embed: CreateEververseBuilder(allItems).Build());
        //}

        //[Command("eververse")]
        //public async Task Eververse([Remainder] string args)
        //{
        //    if (args != null && args.Contains("primary"))
        //    {
        //        List<Eververse> primaryItems = await _eververseService.GetAllPrimaryWeapons();
        //        IUserMessage message = await ReplyAsync(embed: CreateEververseBuilder(primaryItems).Build());
        //        return;
        //    }

        //    if (args != null && args.Contains("buy"))
        //    {
        //        var split = args.Split("buy");
        //        int.TryParse(split[split.Length - 1], out int itemId);
        //        if(itemId == 0)
        //        {
        //            await ReplyAsync(embed: EmbedHelper.CreateFailedReply("Incorrect item id or command issued. The command should appear like ;eververse buy 1").Build());
        //            return;
        //        }

        //        ResponseResult responseResult = await _eververseService.PurchaseItem(Context.User.Id, itemId);
        //        if(responseResult.Success == false)
        //        {
        //            await ReplyAsync(embed: EmbedHelper.CreateFailedReply(responseResult.Message).Build());
        //            return;
        //        }
        //        else
        //        {
        //            await ReplyAsync(embed: EmbedHelper.CreateSuccessReply(responseResult.Message).Build());
        //            return;
        //        }


        //        List<Eververse> primaryItems = await _eververseService.GetAllPrimaryWeapons();
        //        IUserMessage message = await ReplyAsync(embed: CreateEververseBuilder(primaryItems).Build());
        //        return;
        //    }

        //    //var split = args.Split("item");
        //    //await CreateItem(split[split.Length - 1]);
        //}

        //private EmbedBuilder CreateEververseBuilder(List<Eververse> eververseItems)
        //{
        //    var embedBuilder = new EmbedBuilder();
        //    embedBuilder.WithAuthor(new EmbedAuthorBuilder() { Name = "Tess", IconUrl = "https://www.bungie.net/common/destiny2_content/icons/b1dc8f752214a7b2c4013926c45f307f.png" });
        //    embedBuilder.WithDescription("Welcome to my store, if you would like to buy anything type ;buy item {itemId}");
        //    Emote.TryParse("<:misc_glimmer:1009200271475347567>", out Emote parsedGlimmerEmote);
        //    if (eververseItems.FirstOrDefault(x => x.Item.IsPrimary) != null)
        //    {
        //        StringBuilder stringBuilder = new StringBuilder();
        //        foreach (var item in eververseItems.Where(x => x.Item.IsPrimary))
        //        {
        //            embedBuilder.AddField($"{item.Item.DisplayName}", $"Type: {item.Item.GetType()}\nId: {item.Item.Id}\n {parsedGlimmerEmote} {string.Format("{0:n0}", item.Price)}", true);
        //        }
        //        return embedBuilder;
        //    }

        //    return null;
        //}

        //private EmbedBuilder CreateInventoryBuilder(List<Item> inventoryItems, int glimmerAmount)
        //{
        //    Emote.TryParse("<:misc_glimmer:1009200271475347567>", out Emote parsedGlimmerEmote);
        //    var embedBuilder = new EmbedBuilder();
        //    embedBuilder.WithDescription($"This is your inventory guardian!\n{parsedGlimmerEmote} {string.Format("{0:n0}", glimmerAmount)}");
        //    foreach (Item item in inventoryItems.OrderBy(x => x.IsPrimary).ThenBy(x => x.IsSecondary).ThenBy(x => x.IsSuper))
        //    {
        //        embedBuilder.AddField($"{item.DisplayName}", $"Type: {item.GetType()}\nId: {item.Id}", true);
        //    }
        //    return embedBuilder;
        //}
    }
}
