using Boudica.Attributes;
using Boudica.Classes;
using Boudica.Enums;
using Boudica.Helpers;
using Boudica.MongoDB;
using Boudica.MongoDB.Models;
using Boudica.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GiphyDotNet.Manager;
using GiphyDotNet.Model.Parameters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Boudica.Commands
{
    public class ARGCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IConfiguration _config;
        private readonly GuardianService _guardianService;
        private readonly AwardedGuardianService _awardedGuardianService;
        private readonly APIService _apiService;
        private readonly GifService _gifService;
        private readonly Giphy _giphy;

        public ARGCommands(IServiceProvider services, IConfiguration configuration)
        {
            _guardianService = services.GetRequiredService<GuardianService>();
            _awardedGuardianService = services.GetRequiredService<AwardedGuardianService>();
            _apiService = services.GetRequiredService<APIService>();
            _gifService = services.GetRequiredService<GifService>();
            _giphy = new Giphy(configuration[nameof(Mongosettings.GiphyApiKey)]);
            //_itemService = services.GetRequiredService<ItemService>();
            //_eververseService = services.GetRequiredService<EververseService>();
            //_inventoryService = services.GetRequiredService<InventoryService>();
            //_config = services.GetRequiredService<IConfiguration>();
        }

        [SlashCommand("guardian", "Display Guardian Information")]
        [AccountLinked]
        public async Task GetGuardianInformation()
        {
            await DeferAsync();
            bool result = await PopulateGuardianInformation();
            if(result)
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"Success!"; });
        }

        private async Task<bool> PopulateGuardianInformation()
        {
            Guardian guardian = await _guardianService.GetGuardian(Context.User.Id);
            Tuple<bool, string> result = await _apiService.GetGuardianCharacterInformation(guardian.BungieMembershipType, guardian.BungieMembershipId);
            if (result.Item1 == false)
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message =>
                {
                    message.Content = result.Item2;
                });
                return false;
            }

            try
            {
                dynamic item = JsonConvert.DeserializeObject(result.Item2);
                for (int i = 0; i < item.Response.profile.data.characterIds.Count; i++)
                {
                    try
                    {
                        string charId = $"{item.Response.profile.data.characterIds[i]}";
                        guardian.GuardianCharacters.Add(new GuardianCharacter()
                        {
                            GuardianClass = (GuardianClass)item.Response.characters.data[$"{charId}"].classType,
                            Id = charId
                        });
                    }
                    catch (Exception x)
                    {
                        Console.WriteLine($"{x}");
                    }
                }

                if (guardian.GuardianCharacters.Count == 0)
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = $"No guardian found."; });
                    return false;
                }

                await _guardianService.UpdateGuardian(guardian);
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception in {nameof(GetGuardianInformation)}");
                Console.Error.WriteLine(ex);
                await Context.Interaction.ModifyOriginalResponseAsync(message => { message.Content = "Something went wrong.."; });
                return false;
            }
        }

        [SlashCommand("activity-history", "Get Guardian History")]
        [AccountLinked]
        public async Task GetActivityHistory()
        {
            await DeferAsync();
            Guardian guardian = await _guardianService.GetGuardian(Context.User.Id);
            if(guardian.GuardianCharacters.Count == 0)
            {
                bool populateResult = await PopulateGuardianInformation();
                if (populateResult == false) return;
                guardian = await _guardianService.GetGuardian(Context.User.Id);
            }
            Tuple<bool, string> result = await _apiService.GetCharacterActivity(guardian.BungieMembershipType, guardian.BungieMembershipId, guardian.GuardianCharacters[0].Id, guardian.AccessToken);
            if (result.Item1 == false)
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message =>
                {
                    message.Content = result.Item2;
                });
                return;
            }

            BungieActivityResponse response = JsonConvert.DeserializeObject<BungieActivityResponse>(result.Item2);
            StringBuilder sb = new StringBuilder();
            foreach (BungieActivity bungieActivity in response?.Response?.BungieActivities)
            {
                if (APIService.Activities == null) break;
                var match = APIService.Activities.FirstOrDefault(x => x.Key == bungieActivity.BungieActivityDetails.ReferenceId);

                if (match.Key > 0)
                {
                    sb.AppendLine($"" +
                        $"Activity Name: {match.Value} | " +
                        //$"Description: {match.Value.DisplayProperties.Description} | " +
                        $"Kills: {bungieActivity.Values.FirstOrDefault(x => x.Value.StatId == "kills").Value.Basic.DisplayValue} | " +
                        $"Completed {bungieActivity.Values.FirstOrDefault(x => x.Value.StatId == "completed").Value.Basic.DisplayValue}");
                }
            }
            int breakHere = 0;
            if(sb.Length > 0)
                await Context.Interaction.ModifyOriginalResponseAsync(x => x.Content = sb.ToString());
            else
                await Context.Interaction.ModifyOriginalResponseAsync(x => x.Content = "Nothing to see here");
        }

        [SlashCommand("manifest-info", "Get Guardian History")]
        [AccountLinked]
        public async Task GetManifestInfo()
        {
            await DeferAsync();
           
            bool result = await _apiService.DownloadNewManifestFiles();
            if (result== false)
            {
                await Context.Interaction.ModifyOriginalResponseAsync(message =>
                {
                    message.Content = "Failed";
                });
                return;
            }
            await Context.Interaction.ModifyOriginalResponseAsync(message =>
            {
                message.Content = "Success";
            });

            //BungieActivityResponse response = JsonConvert.DeserializeObject<BungieActivityResponse>(result.Item2);
            //StringBuilder sb = new StringBuilder();
            //foreach (BungieActivity bungieActivity in response?.Response?.BungieActivities)
            //{
            //    if (ManifestHelper.DestinyActivityDefinitions == null) break;
            //var match = ManifestHelper.DestinyActivityDefinitions.FirstOrDefault(x => x.ActivityModeHashes.FirstOrDefault(y => y == bungieActivity.BungieActivityDetails.ReferenceId) != null);

            //if(match != null)
            //{
            //    sb.AppendLine($"" +
            //        $"Activity Name: {match.DisplayProperties.Name} | " +
            //        $"Description: {match.DisplayProperties.Description} | " +
            //        $"Kills: {bungieActivity.Values.FirstOrDefault(x => x.Value.StatId == StatId.Kills)} | " +
            //        $"Completed {bungieActivity.Values.FirstOrDefault(x => x.Value.StatId == StatId.Completed)}");
            //}
        }



        //[Command("increase")]
        //public async Task IncreaseGlimmer([Remainder] string args)
        //{
        //    if (args == null || args.Contains("glimmer") == false)
        //    {
        //        await RespondAsync("Invalid command, example is ;increase glimmer 50 @Person");
        //        return;
        //    }
        //    var split = args.Split(" ");
        //    if (split.Length < 3) 
        //    {
        //        await RespondAsync("Invalid command, example is ;increase glimmer 50 @Person");
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
        //            await RespondAsync("Invalid command, example is ;increase glimmer 50 @Person");
        //            return;
        //        }
        //    }

        //    await RespondAsync("Invalid command, example is ;increase glimmer 50 @Person");

        //}

        //[Command("rat")]
        //public async Task RatGif()
        //{
        //    SpamGif usersLastGif = await _gifService.Get(Context.User.Id);
        //    if (usersLastGif != null && usersLastGif.DateTimeLastUsed.Date == DateTime.UtcNow.Date)
        //    {
        //        await RespondAsync(embed: EmbedHelper.CreateFailedReply("You can only use the gif command once per day!").Build());
        //        return;
        //    }

        //    var gifresult = await _giphy.RandomGif(new RandomParameter()
        //    {
        //        Tag = "rat rodent"
        //    });

        //    if (gifresult != null && gifresult.Data != null && gifresult.Data.Url != null)
        //    { 
        //        await RespondAsync(gifresult?.Data?.Url);
        //        await _gifService.UpsertUsersSpamGif(Context.User.Id, Context.User.Username);
        //    }

        //}

        [SlashCommand("leaderboard", "Display current glimmer leaderboard")]
        public async Task GetLeaderboard(bool fullLeaderboard = false)
        {
            List<Guardian> guardians = new List<Guardian>();
            if (fullLeaderboard)
            {
                guardians = await _guardianService.GetFullLeaderboard(Context.User.Id);
            }
            else
            { 
                guardians = await _guardianService.GetLeaderboard(Context.User.Id); 
            }

            if (guardians.Any() == false)
            {
                await RespondAsync("There is nobody in the leaderboards... yet");
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
                IGuildUser user = Context.Guild.GetUser(guardian.Id);
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
            await RespondAsync(embed: embed.Build());
        }


        [SlashCommand("award", "Award a player with 3 Glimmer daily")]
        public async Task AwardPlayer(SocketGuildUser guildUser, string reasonForAward = null)
        {
            if (guildUser == null || guildUser.IsBot)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command, provide a user to get rewarded like /award @User").Build());
                return;
            }

            ActivityUser user = new ActivityUser(guildUser.Id, guildUser.DisplayName);
            if(user == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command, provide a user to get rewarded like /award @User").Build());
                return;
            }

            if(user.UserId == Context.User.Id)
            {
                //Reverse Uno
                await ReplyAsync("https://media.giphy.com/media/Wt6kNaMjofj1jHkF7t/giphy.gif");
                await _guardianService.RemoveGlimmerAsync(user.UserId, 3);
                await RespondAsync($"<@{user.UserId}>, your fellow clanmate has awarded you some glimmer! *SIKE*, you have lost 3 glimmer.. nice try!");
                return;
            }

            Tuple<bool, string> canAwardPlayer = await _awardedGuardianService.CanAwardGlimmerToGuardian(Context.User.Id, user.UserId);
            if(canAwardPlayer.Item1 == false)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply(canAwardPlayer.Item2).Build());
                return;
            }

            await _awardedGuardianService.AwardGuardian(Context.User.Id, user.UserId, user.DisplayName);
            try
            {
                if(string.IsNullOrEmpty(reasonForAward))
                    await RespondAsync($"<@{user.UserId}>, your fellow clanmate has awarded you some glimmer!");
                else
                    await RespondAsync($"<@{user.UserId}>, your fellow clanmate has awarded you some glimmer for {reasonForAward}");
            }
            catch(Exception ex)
            {
                if (string.IsNullOrEmpty(reasonForAward))
                    await ReplyAsync($"<@{user.UserId}>, your fellow clanmate has awarded you some glimmer!");
                else
                    await ReplyAsync($"<@{user.UserId}>, your fellow clanmate has awarded you some glimmer for {reasonForAward}");
            }
        }

        [RequireUserPermission(GuildPermission.KickMembers)]
        [SlashCommand("supersub", "Award a Player with 6 Glimmer for being a Super Sub!")]
        public async Task AwardSuperSubPlayer(SocketGuildUser guildUser, string reason = null)
        {
            if(guildUser == null || guildUser.IsBot)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command, provide a user to get rewarded like /supersub @User").Build());
                return;
            }
            ActivityUser user = new ActivityUser(guildUser.Id, guildUser.DisplayName);
            if (user == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command, provide a user to get rewarded like /supersub @User").Build());
                return;
            }

            if (user.UserId == Context.User.Id)
            {
                await _guardianService.RemoveGlimmerAsync(user.UserId, 9);
                await RespondAsync($"<@{user.UserId}>, you have lost 9 glimmer!");
                return;
            }

            await _awardedGuardianService.AwardGuardian(Context.User.Id, user.UserId, user.DisplayName, 2, true);
            if(string.IsNullOrEmpty(reason))
                await RespondAsync($"<@{user.UserId}>, your fellow clanmate has awarded you some glimmer for being a super sub!");
            else
                await RespondAsync($"<@{user.UserId}>, your fellow clanmate has awarded you some glimmer for being a super sub and {reason}!");
        }    



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
        //        await RespondAsync("Could not find Guardian.. blame Jonny");
        //        return;
        //    }

        //    List<Item> inventoryItems = await _inventoryService.GetAllItems(Context.User.Id);
        //    if(inventoryItems == null || inventoryItems.Any() == false)
        //    {
        //        await RespondAsync(embed: EmbedHelper.CreateFailedReply("You don't have anything in your inventory. You can buy items from Eververse!").Build());
        //        return;
        //    }

        //    await RespondAsync(embed: CreateInventoryBuilder(inventoryItems, guardian.Glimmer).Build());

        //}

        //[Command("eververse")]
        //public async Task Eververse()
        //{
        //    List<Eververse> allItems = await _eververseService.GetAll();
        //    await RespondAsync(embed: CreateEververseBuilder(allItems).Build());
        //}

        //[Command("eververse")]
        //public async Task Eververse([Remainder] string args)
        //{
        //    if (args != null && args.Contains("primary"))
        //    {
        //        List<Eververse> primaryItems = await _eververseService.GetAllPrimaryWeapons();
        //        IUserMessage message = await RespondAsync(embed: CreateEververseBuilder(primaryItems).Build());
        //        return;
        //    }

        //    if (args != null && args.Contains("buy"))
        //    {
        //        var split = args.Split("buy");
        //        int.TryParse(split[split.Length - 1], out int itemId);
        //        if(itemId == 0)
        //        {
        //            await RespondAsync(embed: EmbedHelper.CreateFailedReply("Incorrect item id or command issued. The command should appear like ;eververse buy 1").Build());
        //            return;
        //        }

        //        ResponseResult responseResult = await _eververseService.PurchaseItem(Context.User.Id, itemId);
        //        if(responseResult.Success == false)
        //        {
        //            await RespondAsync(embed: EmbedHelper.CreateFailedReply(responseResult.Message).Build());
        //            return;
        //        }
        //        else
        //        {
        //            await RespondAsync(embed: EmbedHelper.CreateSuccessReply(responseResult.Message).Build());
        //            return;
        //        }


        //        List<Eververse> primaryItems = await _eververseService.GetAllPrimaryWeapons();
        //        IUserMessage message = await RespondAsync(embed: CreateEververseBuilder(primaryItems).Build());
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
