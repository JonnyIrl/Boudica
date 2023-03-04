using Boudica.Attributes;
using Boudica.Classes;
using Boudica.Enums;
using Boudica.Helpers;
using Boudica.MongoDB.Models;
using Boudica.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Boudica.Commands
{
    [Group("create", "Create an activity")]
    [Suspended]
    public class CreateActivityCommands : ActivityHelper
    {
        private static bool _subscribed = false;
        public CreateActivityCommands(IServiceProvider services, CommandHandler handler): base(services, handler)
        {       
            if (!_subscribed)
            {
                handler.OnCreateRaidModalSubmitted += OnCreateRaidModalSubmitted;
                handler.OnCreateFireteamModalSubmitted += OnCreateFireteamModalSubmitted;
                _subscribed = true;
            }
        }

        private async Task<Result> OnCreateFireteamModalSubmitted(SocketModal modal, ITextChannel channel, string title, string description, string fireteamSize, bool alertChannel, string existingPlayers)
        {
            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(description))
            {
                return new Result(false, "You must supply a title or description");
            }

            if (int.TryParse(fireteamSize, out int fireteamSizeResult) == false)
            {
                return new Result(false, "Fireteam size must only be a number between 2 and 6");
            }
            if (fireteamSizeResult > 6 || fireteamSizeResult <= 1)
            {
                return new Result(false, "Fireteam size has to be between 2 and 6 ");
            }

            IGuildUser guildUser = modal.User as IGuildUser;
            if (guildUser == null)
            {
                return new Result(false, "Could not find user");
            }

            Fireteam newFireteam = new Fireteam()
            {
                DateTimeCreated = DateTime.UtcNow,
                CreatedByUserId = guildUser.Id,
                GuidId = guildUser.Guild.Id,
                ChannelId = channel.Id,
                MaxPlayerCount = (byte)fireteamSizeResult,
                Players = new List<ActivityUser>()
                {
                    new ActivityUser(guildUser.Id, guildUser.Username, true)
                }
            };

            if (string.IsNullOrEmpty(existingPlayers) == false)
            {
                List<ActivityUser> usersToAdd = await AddPlayersToNewActivityModal(modal, existingPlayers, fireteamSizeResult);
                foreach(ActivityUser users in usersToAdd)
                {
                    if (newFireteam.Players.Count < fireteamSizeResult)
                        newFireteam.Players.Add(users);
                }
            }

            newFireteam = await _activityService.CreateFireteamAsync(newFireteam);
            if (newFireteam.Id <= 0)
            {
                return new Result(false, "I couldn't create the fireteam because Jonny did something wrong!");
            }

            var embed = new EmbedBuilder();
            embed.WithColor(new Color(0, 255, 0));
            StringBuilder sb = new StringBuilder();
            //Remove the number for the size of the fireteam from the string for the Description
            embed.WithAuthor(guildUser);
            embed.Title = title;
            embed.Description = description;

            AddActivityUsersField(embed, "Players", newFireteam.Players);
            AddActivityUsersField(embed, "Subs", newFireteam.Substitutes);

            EmbedHelper.UpdateFooterOnEmbed(embed, newFireteam);

            var selectMenuBuilder = new SelectMenuBuilder()
            {

                CustomId = $"{(int)CustomId.CloseFireteam}-{newFireteam.Id}",
                Placeholder = "Close Fireteam Options",
                MaxValues = 1,
                MinValues = 1
            };
            selectMenuBuilder.AddOption("Close Fireteam - Completed Successfully", $"{(int)ClosedActivityType.CloseFireteamSuccess}");
            selectMenuBuilder.AddOption("Close Fireteam - Did not complete", $"{(int)ClosedActivityType.CloseFireteamFailure}");
            selectMenuBuilder.AddOption("Force Close (Admin use only)", $"{(int)ClosedActivityType.ForceCloseFireteam}");

            var componentBuilder = new ComponentBuilder()
                   .WithButton("Edit Fireteam", $"{(int)CustomId.EditFireteam}-{newFireteam.Id}", ButtonStyle.Primary)
                   .WithButton("Alert Fireteam", $"{(int)CustomId.FireteamAlert}-{newFireteam.Id}", ButtonStyle.Primary);
            componentBuilder.WithSelectMenu(selectMenuBuilder);

            IUserMessage newMessage;
            IRole role = GetRoleForChannelModal(guildUser, channel.Id);
            if (role != null && newFireteam.Players.Count != newFireteam.MaxPlayerCount && alertChannel)
            {
                await modal.RespondAsync(role.Mention, embed: embed.Build(), components: componentBuilder.Build());
                newMessage = await modal.GetOriginalResponseAsync();
            }
            else
            {
                await modal.RespondAsync(embed: embed.Build(), components: componentBuilder.Build());
                newMessage = await modal.GetOriginalResponseAsync();
            }

            newFireteam.MessageId = newMessage.Id;
            await _activityService.UpdateFireteamAsync(newFireteam);

            await newMessage.PinAsync();
            await newMessage.AddReactionsAsync(new List<IEmote>()
            {
                new Emoji("🇯"),
                new Emoji("🇸"),
            });


            return new Result(true, string.Empty);
        }

        private async Task<Result> OnCreateRaidModalSubmitted(SocketModal modal, ITextChannel channel, string title, string description, DateTime dateTimePlanned, bool alertChannel, string existingPlayers)
        {
            if(string.IsNullOrEmpty(title) && string.IsNullOrEmpty(description))
            {
                return new Result(false, "You must supply a title or description");
            }

            IGuildUser guildUser = modal.User as IGuildUser;
            if(guildUser == null)
            {
                return new Result(false, "Could not find user");
            }
            Raid newRaid = null;
            try
            {
                newRaid = new Raid()
                {
                    DateTimeCreated = DateTime.UtcNow,
                    CreatedByUserId = guildUser.Id,
                    GuidId = guildUser.Guild.Id,
                    ChannelId = channel.Id,
                    MaxPlayerCount = 6,
                    DateTimePlanned = dateTimePlanned,
                    Players = new List<ActivityUser>()
                    {
                        new ActivityUser(guildUser.Id, guildUser.Username, true)
                    }
                };

                if (string.IsNullOrEmpty(existingPlayers) == false)
                {
                    List<ActivityUser> usersToAdd = await AddPlayersToNewActivityModal(modal, existingPlayers);
                    foreach (ActivityUser users in usersToAdd)
                    {
                        if (newRaid.Players.Count < 6)
                            newRaid.Players.Add(users);
                    }
                }

                newRaid = await _activityService.CreateRaidAsync(newRaid);
                if (newRaid.Id <= 0)
                {
                    return new Result(false, "I couldn't create the raid because Jonny did something wrong!");
                }

                var embed = new EmbedBuilder();
                embed.WithColor(new Color(0, 255, 0));
                embed.WithAuthor(guildUser);

                embed.Title = title.Trim();
                if(dateTimePlanned != DateTime.MinValue)
                {
                    long unixTime = ((DateTimeOffset)dateTimePlanned).ToUnixTimeSeconds();
                    embed.Title += $"\n<t:{unixTime}:F> <t:{unixTime}:R>";
                }
                embed.Description = description.Trim();

                AddActivityUsersField(embed, "Players", newRaid.Players);
                AddActivityUsersField(embed, "Subs", newRaid.Substitutes);

                EmbedHelper.UpdateFooterOnEmbed(embed, newRaid);

                var selectMenuBuilder = new SelectMenuBuilder()
                {

                    CustomId = $"{(int)CustomId.CloseRaid}-{newRaid.Id}",
                    Placeholder = "Close Raid Options",
                    MaxValues = 1,
                    MinValues = 1
                };
                selectMenuBuilder.AddOption("Close Raid - Completed Successfully", $"{(int)ClosedActivityType.CloseRaidSuccess}");
                selectMenuBuilder.AddOption("Close Raid - Did not complete", $"{(int)ClosedActivityType.CloseRaidFailure}");
                selectMenuBuilder.AddOption("Force Close (Admin use only)", $"{(int)ClosedActivityType.ForceCloseRaid}");

                var componentBuilder = new ComponentBuilder()
                    .WithButton("Edit Raid", $"{(int)CustomId.EditRaid}-{newRaid.Id}", ButtonStyle.Primary)
                    .WithButton("Alert Raid", $"{(int)CustomId.RaidAlert}-{newRaid.Id}", ButtonStyle.Primary)
                    .WithSelectMenu(selectMenuBuilder);


                IUserMessage newMessage;
                IRole role = GetRoleForChannelModal(guildUser, channel.Id);
                if (role != null && newRaid.Players.Count != newRaid.MaxPlayerCount && alertChannel)
                {
                    // this will reply with the embed
                    await modal.RespondAsync(role.Mention, embed: embed.Build(), components: componentBuilder.Build());
                    newMessage = await modal.GetOriginalResponseAsync();
                }
                else
                {
                    // this will reply with the embed
                    await modal.RespondAsync(null, embed: embed.Build(), components: componentBuilder.Build());
                    newMessage = await modal.GetOriginalResponseAsync();
                }


                newRaid.MessageId = newMessage.Id;
                await _activityService.UpdateRaidAsync(newRaid);

                await newMessage.PinAsync();
                await newMessage.AddReactionsAsync(new List<IEmote>()
                {
                    new Emoji("🇯"),
                    new Emoji("🇸"),
                });
            }
            catch (Exception ex)
            {
                //Didn't get to post the raid into the chat so therefore delete
                if (newRaid != null && newRaid.MessageId == 0)
                {
                    Console.WriteLine($"Exception creating raid - {ex.Message} - {ex.StackTrace} - {ex.InnerException}");
                    await _activityService.DeleteRaidAsync(newRaid.Id);
                    return new Result(false, "Failed to create the raid!");
                }
                Console.WriteLine("Exception creating raid", ex);
            }

            return new Result(true, string.Empty);
        }

        [SlashCommand("raid", "Create a Raid")]
        public async Task CreateRaidCommand([Summary("playersToAdd", "Players to be automatically added")] string playersToAdd = null)
        {
            if(string.IsNullOrEmpty(playersToAdd))
                await Context.Interaction.RespondWithModalAsync(ModalHelper.CreateRaidModal(), new RequestOptions() { RetryMode = RetryMode.AlwaysFail, Timeout = 5000 });
            else
                await Context.Interaction.RespondWithModalAsync(ModalHelper.CreateRaidModal(playersToAdd), new RequestOptions() { RetryMode = RetryMode.AlwaysFail, Timeout = 5000 });
        }

        [SlashCommand("fireteam", "Create a Fireteam")]
        public async Task CreateFireteamCommand(
            [Summary("playersToAdd", "Players to be automatically added")] string playersToAdd = null)
        {

            if (string.IsNullOrEmpty(playersToAdd))
                await Context.Interaction.RespondWithModalAsync(ModalHelper.CreateFireteamModal(), new RequestOptions() { RetryMode = RetryMode.AlwaysFail, Timeout = 5000 });
            else
                await Context.Interaction.RespondWithModalAsync(ModalHelper.CreateFireteamModal(playersToAdd), new RequestOptions() { RetryMode = RetryMode.AlwaysFail, Timeout = 5000 });
        }

        private IRole GetRoleForChannelModal(IGuildUser user, ulong channelId)
        {
            switch (channelId)
            {
                case RaidChannel:
                    return user.Guild.Roles.FirstOrDefault(x => x.Name == RaidRole);
                case VanguardChannel:
                    return user.Guild.Roles.FirstOrDefault(x => x.Name == VanguardRole);
                case CrucibleChannel:
                    return user.Guild.Roles.FirstOrDefault(x => x.Name == CrucibleRole);
                case GambitChannel:
                    return user.Guild.Roles.FirstOrDefault(x => x.Name == GambitRole);
                case MiscChannel:
                    return user.Guild.Roles.FirstOrDefault(x => x.Name == MiscRole);
                case DungeonChannel:
                    return user.Guild.Roles.FirstOrDefault(x => x.Name == DungeonRole);
            }

            return null;
        }
    }

    [Group("edit", "Edit an activity")]
    [Suspended]
    public class EditActivityCommands : ActivityHelper
    {
        private static bool _subscribed = false;
        public EditActivityCommands(IServiceProvider services, CommandHandler handler): base(services, handler)
        {
            if (!_subscribed)
            {
                handler.OnEditRaidModalSubmitted += OnEditRaidModalSubmitted;
                handler.OnEditRaidButtonClicked += OnEditRaidButtonClick;

                handler.OnEditFireteamModalSubmitted += OnEditFireteamModalSubmitted;
                handler.OnEditFireteamButtonClicked += OnEditFireteamButtonClick;
                _subscribed = true;
            }
        }

        private async Task<Result> OnEditRaidModalSubmitted(ITextChannel channel, string title, string description, DateTime dateTimePlanned, int raidId)
        {
            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(description))
            {
                return new Result(false, "You must supply a title or description");
            }

            Raid existingRaid = await _activityService.GetMongoRaidAsync(raidId);
            if (existingRaid == null)
            {
                return new Result(false, "Could not find Raid to edit");
            }
            existingRaid.DateTimePlanned = dateTimePlanned;
            await _activityService.UpdateRaidAsync(existingRaid);

            IUserMessage message = (IUserMessage)await channel.GetMessageAsync(existingRaid.MessageId, CacheMode.AllowDownload);
            if (message == null)
            {
                return new Result(false, "Could not find message to edit");
            }

            if (dateTimePlanned != DateTime.MinValue)
            {
                long unixTime = ((DateTimeOffset)dateTimePlanned).ToUnixTimeSeconds();
                title += $"\n<t:{unixTime}:F> <t:{unixTime}:R>";
            }
            

            Task.Run(async () =>
            {
                var modifiedEmbed = new EmbedBuilder();
                var embed = message.Embeds.FirstOrDefault();
                modifiedEmbed.Title = title;
                modifiedEmbed.Description = description;
                EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                EmbedHelper.UpdateColorOnEmbed(modifiedEmbed, embed);
                EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingRaid);
                EmbedHelper.UpdateFieldsOnEmbed(modifiedEmbed, embed);
                await message.ModifyAsync(x =>
                {
                    x.Embed = modifiedEmbed.Build();
                });

                await message.ReplyAsync($"Raid {raidId} has been edited!");
            });
           
            return new Result(true, string.Empty);
        }

        private async Task<Result> OnEditRaidButtonClick(SocketMessageComponent component, int raidId)
        {
            Raid existingRaid = await _activityService.GetMongoRaidAsync(raidId);
            Result existingRaidResult = await CheckExistingRaidIsValidButtonClick(component, existingRaid, false);
            if (existingRaidResult.Success == false) return existingRaidResult;

            if (component.GuildId != existingRaid.GuidId)
            {
                return new Result(false, "Could not find message to edit");
            }
            IUserMessage message = component.Message as IUserMessage;
            if (message == null)
            {
                return new Result(false, "Could not find message to edit");
            }

            var embed = message.Embeds.FirstOrDefault();
            if(embed == null)
            {
                return new Result(false, "Failed");
            }

            string title = embed.Title;
            //Check to see if we already have a DateTime value set
            int dateTimePlannedIndex = embed.Title.IndexOf("\n");
            if (dateTimePlannedIndex != -1)
                title = embed.Title.Remove(dateTimePlannedIndex);
            await component.RespondWithModalAsync(
                ModalHelper.EditRaidModal(
                    existingRaid,
                    title,
                    embed.Description,
                    existingRaid.DateTimePlanned == DateTime.MinValue ? string.Empty : existingRaid.DateTimePlanned.ToString("dd/MM HH:mm")));

            return new Result(true, string.Empty);
        }

        private async Task<Result> OnEditFireteamButtonClick(SocketMessageComponent component, int fireteamId)
        {
            SocketGuildUser user = component.User as SocketGuildUser;
            Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(fireteamId);
            Result existingFireteamResult = await CheckExistingFireteamIsValidButtonClick(component, existingFireteam, user);
            if (existingFireteamResult.Success == false) return existingFireteamResult;

            if (component.GuildId != existingFireteam.GuidId)
            {
                return new Result(false, "Could not find message to edit");
            }
            IUserMessage message = component.Message as IUserMessage;
            if (message == null)
            {
                return new Result(false, "Could not find message to edit");
            }

            var embed = message.Embeds.FirstOrDefault();
            if (embed == null)
            {
                return new Result(false, "Failed");
            }

            await component.RespondWithModalAsync(ModalHelper.EditFireteamModal(existingFireteam, embed.Title, embed.Description));
            return new Result(true, string.Empty);
        }
        private async Task<Result> OnEditFireteamModalSubmitted(ITextChannel channel, string title, string description, int fireteamId)
        {
            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(description))
            {
                return new Result(false, "You must supply a title or description");
            }

            Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(fireteamId);
            if (existingFireteam == null)
            {
                return new Result(false, "Could not find Raid to edit");
            }

            IUserMessage message = (IUserMessage)await channel.GetMessageAsync(existingFireteam.MessageId, CacheMode.AllowDownload);
            if (message == null)
            {
                return new Result(false, "Could not find message to edit"); ;
            }

            Task.Run(async () =>
            {
                var modifiedEmbed = new EmbedBuilder();
                var embed = message.Embeds.FirstOrDefault();
                modifiedEmbed.Title = title;
                modifiedEmbed.Description = description;
                EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                EmbedHelper.UpdateColorOnEmbed(modifiedEmbed, embed);
                EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingFireteam);
                EmbedHelper.UpdateFieldsOnEmbed(modifiedEmbed, embed);
                await message.ModifyAsync(x =>
                {
                    x.Embed = modifiedEmbed.Build();
                });

                await message.ReplyAsync($"Fireteam {existingFireteam.Id} has been edited!");
            });

            return new Result(true, string.Empty);
        }
    }

    [Group("close", "Close an activity")]
    [Suspended]
    public class CloseActivityCommands : ActivityHelper
    {
        private readonly HiringService _hiringService;
        private static bool _subscribed = false;
        public CloseActivityCommands(IServiceProvider services, CommandHandler handler) : base(services, handler)
        {
            _hiringService = services.GetRequiredService<HiringService>();
            if (!_subscribed)
            {
                handler.OnCloseRaidMenuItemSelected += OnCloseRaidMenuItemSelected;
                handler.OnCloseFireteamMenuItemSelected += OnCloseFireteamMenuItemSelected;
                _subscribed = true;
            }
        }

        private async Task<Result> OnCloseRaidMenuItemSelected(SocketMessageComponent component, int raidId, ClosedActivityType activityType)
        {
            return await CloseRaidButtonClick(component, raidId, activityType);
        }

        private async Task<Result> OnCloseFireteamMenuItemSelected(SocketMessageComponent component, int fireteamId, ClosedActivityType activityType)
        {
            return await CloseFireteamButtonClick(component, fireteamId, activityType);
        }

        //[SlashCommand("fireteam", "Close a Fireteam")]
        //public async Task CloseFireteam(int fireteamId)
        //{
        //    Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(fireteamId);
        //    bool existingFireteamResult = await CheckExistingFireteamIsValid(existingFireteam);
        //    if (existingFireteamResult == false) return;

        //    if (existingFireteam.DateTimeClosed == DateTime.MinValue)
        //    {
        //        existingFireteam.DateTimeClosed = DateTime.UtcNow;
        //        await _activityService.UpdateFireteamAsync(existingFireteam);
        //    }

        //    ITextChannel channel = Context.Guild.GetTextChannel(existingFireteam.ChannelId);
        //    if (channel == null)
        //    {
        //        await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find channel where message is").Build());
        //        return;
        //    }

        //    IUserMessage message = (IUserMessage)await channel.GetMessageAsync(existingFireteam.MessageId, CacheMode.AllowDownload);
        //    if (message == null)
        //    {
        //        await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find message to edit").Build());
        //        return;
        //    }
        //    var modifiedEmbed = new EmbedBuilder();
        //    var embed = message.Embeds.FirstOrDefault();
        //    if (embed?.Title == "This activity is now closed")
        //    {
        //        await RespondAsync(embed: EmbedHelper.CreateFailedReply("This Fireteam is already closed").Build());
        //        return;
        //    }

        //    EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
        //    EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
        //    EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingFireteam);
        //    EmbedHelper.UpdateFieldsOnEmbed(modifiedEmbed, embed);
        //    modifiedEmbed.Title = "This activity is now closed";
        //    modifiedEmbed.Color = Color.Red;
        //    await message.UnpinAsync();
        //    await message.ModifyAsync(x =>
        //    {
        //        x.Embed = modifiedEmbed.Build();
        //        x.Components = null;
        //    });

        //    //Has to be more than 1 player in a Raid/Fireteam in order to award glimmer
        //    if (existingFireteam.DateTimeClosed != DateTime.MinValue && existingFireteam.Players.Count > 1)
        //    {
        //        await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"Fireteam {fireteamId} has been closed! <@{existingFireteam.CreatedByUserId}> did this activity get completed?").Build());
        //        IUserMessage responseMessage = await GetOriginalResponseAsync();
        //        if (responseMessage != null) await responseMessage.AddReactionsAsync(_successFailEmotes);
        //    }
        //    else
        //    {
        //        existingFireteam.AwardedGlimmer = true;
        //        await _activityService.UpdateFireteamAsync(existingFireteam);
        //        await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"Fireteam {fireteamId} has been closed!").Build());
        //    }
        //}

        public async Task<Result> CloseRaidButtonClick(SocketMessageComponent component, int raidId, ClosedActivityType activityType)
        {
            SocketGuildUser user = component.User as SocketGuildUser;
            if (user == null)
            {
                return new Result(false, "Failed, could not find user");
            }
            Raid existingRaid = await _activityService.GetMongoRaidAsync(raidId);
            if (existingRaid == null)
            {
                return new Result(false, "Could not find raid to close");
            }
            //If force closing pass true
            Result exisingRaidResult = await CheckExistingRaidIsValidButtonClick(component, existingRaid, user, activityType == ClosedActivityType.ForceCloseRaid ? true : false);
            if (exisingRaidResult.Success == false) return exisingRaidResult;

            existingRaid.DateTimeClosed = DateTime.UtcNow;
            await _activityService.UpdateRaidAsync(existingRaid);

            StringBuilder updateMessageText = new StringBuilder();
            switch (activityType)
            {
                case ClosedActivityType.ForceCloseRaid:
                    updateMessageText.Append($"The activity was force closed by {user.Username} so no Glimmer has been awarded");
                    break;
                case ClosedActivityType.CloseRaidFailure:
                    updateMessageText.Append("The activity did not complete so no Glimmer has been awarded.");
                    break;
                case ClosedActivityType.CloseRaidSuccess:
                    Tuple<int, bool> glimmerResult = await CalculateGlimmerForActivity(existingRaid.Players, existingRaid.CreatedByUserId, true);
                    updateMessageText.AppendJoin(", ", existingRaid.Players.Where(x => x.Reacted).Select(x => x.DisplayName));
                    updateMessageText.Append($" received {glimmerResult.Item1} Glimmer for completing this activity.");
                    if (glimmerResult.Item2 == false)
                    {
                        Console.WriteLine("glimmerResult.Item2 == false");
                        string creatorName = existingRaid.Players.FirstOrDefault(x => x.UserId == existingRaid.CreatedByUserId)?.DisplayName;
                        if (string.IsNullOrEmpty(creatorName) == false)
                            updateMessageText.Append($" {creatorName} received a first-time weekly bonus of 3 Glimmer for creating the activity");
                    }
                    break;
            }
            IUserMessage message = component.Message as IUserMessage;
            if (message == null)
            {
                return new Result(false, "Could not find message to close");
            }
            var embed = message.Embeds.FirstOrDefault();
            if (message != null && embed != null && !string.IsNullOrEmpty(embed.Title))
            {
                await component.RespondAsync(embed: EmbedHelper.CreateSuccessReply($"Raid {embed.Title} has been closed!").Build());
            }
            else
            {
                await component.RespondAsync(embed: EmbedHelper.CreateSuccessReply($"Raid Id {raidId} has been closed!").Build());
            }
            existingRaid.AwardedGlimmer = true;
            await _activityService.UpdateRaidAsync(existingRaid);

           
            var modifiedEmbed = new EmbedBuilder();
            string originalTitle = embed.Title;
            EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingRaid);
            EmbedHelper.UpdateFieldsOnEmbed(modifiedEmbed, embed);
            modifiedEmbed.Title = "This raid is now closed";
            modifiedEmbed.Color = Color.Red;
            modifiedEmbed.Description = $"{updateMessageText.ToString()}";
            await message.UnpinAsync();
            await message.ModifyAsync(x =>
            {
                x.Embed = modifiedEmbed.Build();
                x.Components = null;
            });

            return new Result(true, string.Empty);
        }
        public async Task<Result> CloseFireteamButtonClick(SocketMessageComponent component, int fireteamId, ClosedActivityType activityType)
        {
            SocketGuildUser user = component.User as SocketGuildUser;
            if (user == null)
            {
                return new Result(false, "Failed, could not find user");
            }
            Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(fireteamId);
            Result exisingFireteamResult = await CheckExistingFireteamIsValidButtonClick(component, existingFireteam, user, activityType == ClosedActivityType.ForceCloseFireteam ? true : false);
            if (exisingFireteamResult.Success == false) return exisingFireteamResult;

            existingFireteam.DateTimeClosed = DateTime.UtcNow;
            await _activityService.UpdateFireteamAsync(existingFireteam);

            StringBuilder updateMessageText = new StringBuilder();
            switch (activityType)
            {
                case ClosedActivityType.ForceCloseFireteam:
                    updateMessageText.Append($"The activity was force closed by {user.Username} so no Glimmer has been awarded");
                    break;
                case ClosedActivityType.CloseFireteamFailure:
                    updateMessageText.Append("The activity did not complete so no Glimmer has been awarded.");
                    break;
                case ClosedActivityType.CloseFireteamSuccess:
                    Tuple<int, bool> glimmerResult = await CalculateGlimmerForActivity(existingFireteam.Players, existingFireteam.CreatedByUserId, true);
                    updateMessageText.AppendJoin(", ", existingFireteam.Players.Where(x => x.Reacted).Select(x => x.DisplayName));
                    updateMessageText.Append($" received {glimmerResult.Item1} Glimmer for completing this activity.");
                    if (glimmerResult.Item2 == false)
                    {
                        string creatorName = existingFireteam.Players.FirstOrDefault(x => x.UserId == existingFireteam.CreatedByUserId)?.DisplayName;
                        if (string.IsNullOrEmpty(creatorName) == false)
                            updateMessageText.Append($" {creatorName} received a first-time weekly bonus of 3 Glimmer for creating the activity");
                    }
                    break;
            }

            IUserMessage message = component.Message as IUserMessage;
            if (message == null)
            {
                return new Result(false, "Could not find message to close");
            }
            var embed = message.Embeds.FirstOrDefault();
            if (embed != null && !string.IsNullOrEmpty(embed.Title))
            {
                await component.RespondAsync(embed: EmbedHelper.CreateSuccessReply($"Fireteam {embed.Title} has been closed!").Build());
            }
            else
            {
                await component.RespondAsync(embed: EmbedHelper.CreateSuccessReply($"Fireteam Id {fireteamId} has been closed!").Build());
            }
            existingFireteam.AwardedGlimmer = true;
            await _activityService.UpdateFireteamAsync(existingFireteam);

            var modifiedEmbed = new EmbedBuilder();
            string originalTitle = embed.Title;
            EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingFireteam);
            EmbedHelper.UpdateFieldsOnEmbed(modifiedEmbed, embed);
            modifiedEmbed.Title = "This fireteam is now closed";
            modifiedEmbed.Color = Color.Red;
            modifiedEmbed.Description = $"{updateMessageText.ToString()}";
            await message.UnpinAsync();
            await message.ModifyAsync(x =>
            {
                x.Embed = modifiedEmbed.Build();
                x.Components = null;
            });

            return new Result(true, string.Empty);
        }

        private async Task<Tuple<int, bool>> CalculateGlimmerForActivity(List<ActivityUser> activityUsers, ulong creatorId, bool isRaid)
        {
            if (activityUsers == null) return new Tuple<int, bool>(-1, false);
            //int increaseAmount = (1 * activityUsers.Count(x => x.Reacted));
            int increaseAmount = 20;
            bool awardedThisWeek = false;
            foreach (ActivityUser user in activityUsers)
            {
                if (user.UserId == creatorId)
                {
                    await _hiringService.UpdateCreatedPost(user.UserId);

                    if (isRaid)
                        awardedThisWeek = await CreatedRaidThisWeek(creatorId);
                    else
                        awardedThisWeek = await CreatedFireteamThisWeek(creatorId);

                    //Give bonus of 3 for first activity each week.
                    if (awardedThisWeek == false)
                    {
                        //await _guardianService.IncreaseGlimmerAsync(user.UserId, user.DisplayName, increaseAmount + 3);
                        await _guardianService.IncreaseGlimmerAsync(user.UserId, user.DisplayName, increaseAmount);
                        Console.WriteLine($"Increased Glimmer for {user.DisplayName} by {increaseAmount}");
                    }
                    else
                    {
                        await _guardianService.IncreaseGlimmerAsync(user.UserId, user.DisplayName, increaseAmount);
                        Console.WriteLine($"Increased Glimmer for {user.DisplayName} by {increaseAmount}");
                    }
                }
                else if (user.Reacted)
                {
                    await _hiringService.UpdateJoinedPost(user.UserId, DateTime.UtcNow);
                    await _guardianService.IncreaseGlimmerAsync(user.UserId, user.DisplayName, increaseAmount);
                    Console.WriteLine($"Increased Glimmer for {user.DisplayName} by {increaseAmount}");
                }
            }

            return new Tuple<int, bool>(increaseAmount, awardedThisWeek);
        }

        private async Task<bool> CreatedRaidThisWeek(ulong userId)
        {
            return await _activityService.CreatedRaidThisWeek(userId);
        }

        private async Task<bool> CreatedFireteamThisWeek(ulong userId)
        {
            return await _activityService.CreatedFireteamThisWeek(userId);
        }
    }

    public class OtherActivityCommands : ActivityHelper
    {
        private static bool _subscribed = false;
        private readonly HistoryService _historyService;
        public OtherActivityCommands(IServiceProvider services, CommandHandler handler): base(services, handler)
        {
            if (!_subscribed)
            {
                handler.OnAlertRaidButtonClicked += OnAlertRaidButtonClicked;
                handler.OnAlertFireteamButtonClicked += OnAlertFireteamButtonClicked;
                _subscribed = true;
            }

            _historyService = services.GetRequiredService<HistoryService>();
        }

        private async Task<Result> OnAlertRaidButtonClicked(SocketMessageComponent component, int raidId)
        {
            Raid existingRaid = await _activityService.GetMongoRaidAsync(raidId);
            Result exisingRaidResult = await CheckCanAlertRaidButtonClick(component.User.Id, existingRaid);
            if (exisingRaidResult.Success == false) return exisingRaidResult;

            existingRaid.DateTimeAlerted = DateTime.UtcNow;
            await _activityService.UpdateRaidAsync(existingRaid);

            IUserMessage message = component.Message as IUserMessage;
            if (message == null)
            {
                return new Result(false, "Could not find message to alert");
            }

            StringBuilder sb = new StringBuilder();
            foreach (ActivityUser player in existingRaid.Players)
            {
                if (player.UserId == component.User.Id) continue;
                sb.Append($"<@{player.UserId}> ");
            }

            sb.Append("Are you all still ok for this raid?");
            await message.ReplyAsync(sb.ToString());
            return new Result(true, "Success");
        }

        private async Task<Result> OnAlertFireteamButtonClicked(SocketMessageComponent component, int fireteamId)
        {
            Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(fireteamId);
            Result exisingFireteamResult = await CheckCanAlertFireteamButtonClick(component.User.Id, existingFireteam);
            if (exisingFireteamResult.Success == false) return exisingFireteamResult;

            IUserMessage message = component.Message as IUserMessage;
            if (message == null)
            {
                return new Result(false, "Could not find message to alert");
            }

            StringBuilder sb = new StringBuilder();
            foreach (ActivityUser player in existingFireteam.Players)
            {
                if (player.UserId == component.User.Id) continue;
                sb.Append($"<@{player.UserId}> ");
            }

            if (string.IsNullOrEmpty(sb.ToString()))
            {
                return new Result(false, "There are no players to alert");
            }
            else
            {
                existingFireteam.DateTimeAlerted = DateTime.UtcNow;
                await _activityService.UpdateFireteamAsync(existingFireteam);
            }

            sb.Append("Are you all still ok for this activity?");
            await message.ReplyAsync(sb.ToString());
            return new Result(true, "Success");
        }

        [SlashCommand("add-player", "Add Player to activity")]
        [Suspended]
        public async Task AddToActivity(Enums.ActivityType activityType, int id, string playersToAdd)
        {
            switch(activityType)
            {
                case Enums.ActivityType.Raid:
                    await AddPlayerToRaid(id, playersToAdd);
                    return;

                case Enums.ActivityType.Fireteam:
                    await AddPlayerToFireteam(id, playersToAdd);
                    return;
            }

            await RespondAsync("Something went wrong", ephemeral: true);
        }
        public async Task AddPlayerToRaid(int raidId, string playersToAdd)
        {
            Raid existingRaid = await _activityService.GetMongoRaidAsync(raidId);
            int oldPlayerCount = existingRaid.Players.Count;
            bool existingRaidResult = await CheckExistingRaidIsValid(existingRaid, true);
            if (existingRaidResult == false) return;

            if (Context.Guild.Id != existingRaid.GuidId)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find message to edit").Build());
                return;
            }
            ITextChannel channel = Context.Guild.GetTextChannel(existingRaid.ChannelId);
            if (channel == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find channel where message is").Build());
                return;
            }
            IUserMessage message = (IUserMessage)await channel.GetMessageAsync(existingRaid.MessageId, CacheMode.AllowDownload);
            if (message == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find message to edit").Build());
                return;
            }

            if (existingRaid.Players.Count == existingRaid.MaxPlayerCount)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("This raid is already full!").Build());
                return;
            }

            List<ActivityUser> usersToAdd = AddPlayersToNewActivity(playersToAdd);
            StringBuilder sb = new StringBuilder();
            foreach (ActivityUser userToAdd in usersToAdd)
            {
                IGuildUser guildUser = Context.Guild.GetUser(userToAdd.UserId);
                if (guildUser != null)
                {

                    ActivityUser existingUser = existingRaid.Players.FirstOrDefault(x => x.UserId == userToAdd.UserId);
                    //Already added
                    if (existingUser != null)
                    {
                        continue;
                    }
                    else if (existingRaid.Players.Count < existingRaid.MaxPlayerCount)
                    {
                        sb.Append($"<@{userToAdd.UserId}>");
                        existingRaid.Players.Add(new ActivityUser(guildUser.Id, guildUser.DisplayName));
                        await _historyService.InsertHistoryRecord(Context.User.Id, guildUser.Id, HistoryType.AddPlayer);
                    }
                }
            }

            if (oldPlayerCount == existingRaid.Players.Count)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply($"Could not find player to add to Raid {existingRaid.Id} or player already exists").Build());
                return;
            }

            await _activityService.UpdateRaidAsync(existingRaid);

            var modifiedEmbed = new EmbedBuilder();
            var embed = message.Embeds.FirstOrDefault();
            EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingRaid);
            AddActivityUsersField(modifiedEmbed, "Players", existingRaid.Players);
            AddActivityUsersField(modifiedEmbed, "Subs", existingRaid.Substitutes);

            await message.ModifyAsync(x =>
            {
                x.Embed = modifiedEmbed.Build();
            });

            await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"{sb.ToString()} has been added to the Raid | {modifiedEmbed.Description}").Build());
        }
        public async Task AddPlayerToFireteam(int fireteamId, string playersToAdd)
        {
            Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(fireteamId);
            int oldPlayerCount = existingFireteam.Players.Count;
            bool existingFireteamResult = await CheckExistingFireteamIsValid(existingFireteam);
            if (existingFireteamResult == false) return;

            if (Context.Guild.Id != existingFireteam.GuidId)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find message to edit").Build());
                return;
            }
            ITextChannel channel = Context.Guild.GetTextChannel(existingFireteam.ChannelId);
            if (channel == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find channel where message is").Build());
                return;
            }
            IUserMessage message = (IUserMessage)await channel.GetMessageAsync(existingFireteam.MessageId, CacheMode.AllowDownload);
            if (message == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find message to edit").Build());
                return;
            }

            if (existingFireteam.Players.Count == existingFireteam.MaxPlayerCount)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("This fireteam is already full!").Build());
                return;
            }

            List<ActivityUser> usersToAdd = AddPlayersToNewActivity(playersToAdd);
            StringBuilder sb = new StringBuilder();
            foreach (ActivityUser userToAdd in usersToAdd)
            {
                IGuildUser guildUser = Context.Guild.GetUser(userToAdd.UserId);
                if (guildUser != null)
                {

                    ActivityUser existingUser = existingFireteam.Players.FirstOrDefault(x => x.UserId == userToAdd.UserId);
                    //Already added
                    if (existingUser != null)
                    {
                        continue;
                    }
                    else if (existingFireteam.Players.Count < existingFireteam.MaxPlayerCount)
                    {
                        sb.Append($"<@{userToAdd.UserId}>");
                        existingFireteam.Players.Add(new ActivityUser(guildUser.Id, guildUser.DisplayName));
                        await _historyService.InsertHistoryRecord(Context.User.Id, guildUser.Id, HistoryType.AddPlayer);
                    }
                }
            }

            if (oldPlayerCount == existingFireteam.Players.Count)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply($"Could not find player to add to Fireteam {existingFireteam.Id} or player already exists").Build());
                return;
            }

            await _activityService.UpdateFireteamAsync(existingFireteam);

            var modifiedEmbed = new EmbedBuilder();
            var embed = message.Embeds.FirstOrDefault();
            EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingFireteam);
            AddActivityUsersField(modifiedEmbed, "Players", existingFireteam.Players);
            AddActivityUsersField(modifiedEmbed, "Subs", existingFireteam.Substitutes);

            await message.ModifyAsync(x =>
            {
                x.Embed = modifiedEmbed.Build();
            });

            await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"{sb.ToString()} has been added to the Fireteam | {modifiedEmbed.Description}").Build());
        }

        [SlashCommand("remove-player", "Remove a player from an activity")]
        [Suspended]
        public async Task RemovePlayer(Enums.ActivityType activityType, int id, string playersToRemove)
        {
            switch (activityType)
            {
                case Enums.ActivityType.Raid:
                    await RemovePlayerFromRaid(id, playersToRemove);
                    return;
                case Enums.ActivityType.Fireteam:
                    await RemovePlayerFromFireteam(id, playersToRemove);
                    return;
            }

            await RespondAsync(embed: EmbedHelper.CreateFailedReply("Something went wrong, ensure you choose Raid or Fireteam and supply a valid id along with players to remove").Build());
        }
        public async Task RemovePlayerFromRaid(int raidId, string playersToRemove)
        {
            Raid existingRaid = await _activityService.GetMongoRaidAsync(raidId);
            int oldPlayerCount = existingRaid.Players.Count;
            bool existingRaidResult = await CheckExistingRaidIsValid(existingRaid, true);
            if (existingRaidResult == false)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find raid with that Id").Build());
                return;
            }

            if (Context.Guild.Id != existingRaid.GuidId)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find message to edit").Build());
                return;
            }
            ITextChannel channel = Context.Guild.GetTextChannel(existingRaid.ChannelId);
            if (channel == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find channel where message is").Build());
                return;
            }
            IUserMessage message = (IUserMessage)await channel.GetMessageAsync(existingRaid.MessageId, CacheMode.AllowDownload);
            if (message == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find message to edit").Build());
                return;
            }

            List<ActivityUser> usersToRemove = AddPlayersToNewActivity(playersToRemove);
            StringBuilder sb = new StringBuilder();
            List<ulong> removeIds = new List<ulong>();

            foreach (ActivityUser userToRemove in usersToRemove)
            {
                IGuildUser guildUser = Context.Guild.GetUser(userToRemove.UserId);
                if (guildUser != null)
                {
                    sb.Append($"<@{userToRemove.UserId}>");
                    if (existingRaid.Players.FirstOrDefault(x => x.UserId == userToRemove.UserId) != null)
                    {
                        existingRaid.Players.RemoveAll(x => x.UserId == userToRemove.UserId);
                        removeIds.Add(userToRemove.UserId);
                    }
                    _commandHandler.AddPlayerToManualEmoteList(guildUser.Id);
                    await message.RemoveReactionAsync(_jEmoji, guildUser);
                }
            }

            if (oldPlayerCount == existingRaid.Players.Count)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply($"Could not find player to remove from Raid {existingRaid.Id}").Build());
                return;
            }
          
            await _activityService.UpdateRaidAsync(existingRaid);

            var modifiedEmbed = new EmbedBuilder();
            var embed = message.Embeds.FirstOrDefault();
            EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingRaid);
            AddActivityUsersField(modifiedEmbed, "Players", existingRaid.Players);
            AddActivityUsersField(modifiedEmbed, "Subs", existingRaid.Substitutes);

            await message.ModifyAsync(x =>
            {
                x.Embed = modifiedEmbed.Build();
            });

            await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"{sb.ToString()} has been removed from the Raid {existingRaid.Id}").Build());
            removeIds.ForEach(async x =>
            {
                await _historyService.InsertHistoryRecord(Context.User.Id, x, HistoryType.RemovePlayer);
            });
        }
        private async Task RemovePlayerFromFireteam(int fireteamId, string playersToRemove)
        {
            Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(fireteamId);
            int oldPlayerCount = existingFireteam.Players.Count;
            bool existingFireteamResult = await CheckExistingFireteamIsValid(existingFireteam);
            if (existingFireteamResult == false) return;

            if (Context.Guild.Id != existingFireteam.GuidId)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find message to edit").Build());
                return;
            }
            ITextChannel channel = Context.Guild.GetTextChannel(existingFireteam.ChannelId);
            if (channel == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find channel where message is").Build());
                return;
            }
            IUserMessage message = (IUserMessage)await channel.GetMessageAsync(existingFireteam.MessageId, CacheMode.AllowDownload);
            if (message == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find message to edit").Build());
                return;
            }

            List<ActivityUser> usersToRemove = AddPlayersToNewActivity(playersToRemove);
            StringBuilder sb = new StringBuilder();
            List<ulong> removeIds = new List<ulong>();
            foreach (ActivityUser userToRemove in usersToRemove)
            {
                IGuildUser guildUser = Context.Guild.GetUser(userToRemove.UserId);
                if (guildUser != null)
                {
                    sb.Append($"<@{userToRemove.UserId}>");
                    if (existingFireteam.Players.FirstOrDefault(x => x.UserId == userToRemove.UserId) != null)
                    {
                        existingFireteam.Players.RemoveAll(x => x.UserId == userToRemove.UserId);
                        removeIds.Add(userToRemove.UserId);
                    }
                    _commandHandler.AddPlayerToManualEmoteList(guildUser.Id);
                    await message.RemoveReactionAsync(_jEmoji, guildUser);
                }
            }

            if (oldPlayerCount == existingFireteam.Players.Count)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply($"Could not find player to remove from Fireteam {existingFireteam.Id}").Build());
                return;
            }

            await _activityService.UpdateFireteamAsync(existingFireteam);

            var modifiedEmbed = new EmbedBuilder();
            var embed = message.Embeds.FirstOrDefault();
            EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingFireteam);
            AddActivityUsersField(modifiedEmbed, "Players", existingFireteam.Players);
            AddActivityUsersField(modifiedEmbed, "Subs", existingFireteam.Substitutes);

            await message.ModifyAsync(x =>
            {
                x.Embed = modifiedEmbed.Build();
            });

            await message.ReplyAsync(null, false, EmbedHelper.CreateSuccessReply($"{sb.ToString()} has been removed from the Fireteam {existingFireteam.Id}").Build());
            await RespondAsync("Success", ephemeral: true);
            removeIds.ForEach(async x =>
            {
                await _historyService.InsertHistoryRecord(Context.User.Id, x, HistoryType.RemovePlayer);
            });
        }
        private ActivityUser CreateActivityUser(SocketGuildUser user)
        {
            if (user != null && user.IsBot == false)
            {
                return new ActivityUser(user.Id, user.DisplayName);
            }

            return null;
        }

        [SlashCommand("alert", "Alert all members of an activity")]
        [Suspended]
        public async Task AlertActivity(Enums.ActivityType activityType, int id)
        {
            switch (activityType)
            {
                case Enums.ActivityType.Raid:
                    await AlertRaid(id);
                    return;
                case Enums.ActivityType.Fireteam:
                    await AlertFireteam(id);
                    return;
            }

            await RespondAsync(embed: EmbedHelper.CreateFailedReply("Something went wrong, ensure you choose Raid or Fireteam and supply a valid id").Build());
        }
        private async Task AlertRaid(int raidId)
        {
            Raid existingRaid = await _activityService.GetMongoRaidAsync(raidId);
            bool exisingRaidResult = await CheckCanAlertRaid(existingRaid);
            if (exisingRaidResult == false) return;

            existingRaid.DateTimeAlerted = DateTime.UtcNow;
            await _activityService.UpdateRaidAsync(existingRaid);

            ITextChannel channel = Context.Guild.GetTextChannel(existingRaid.ChannelId);
            if (channel == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find channel where message is").Build());
                return;
            }
            IUserMessage message = (IUserMessage)await channel.GetMessageAsync(existingRaid.MessageId, CacheMode.AllowDownload);
            if (message == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find message to close").Build());
                return;
            }

            StringBuilder sb = new StringBuilder();
            foreach (ActivityUser player in existingRaid.Players)
            {
                if (player.UserId == Context.User.Id) continue;
                sb.Append($"<@{player.UserId}> ");
            }

            sb.Append("Are you all still ok for this raid?");
            await message.ReplyAsync(sb.ToString());
            await RespondAsync("Success", ephemeral: true);
        }
        private async Task AlertFireteam(int fireteamId)
        {
            Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(fireteamId);
            bool exisingRaidResult = await CheckCanAlertFireteam(existingFireteam);
            if (exisingRaidResult == false) return;

            existingFireteam.DateTimeAlerted = DateTime.UtcNow;
            await _activityService.UpdateFireteamAsync(existingFireteam);

            ITextChannel channel = Context.Guild.GetTextChannel(existingFireteam.ChannelId);
            if (channel == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find channel where message is").Build());
                return;
            }
            IUserMessage message = (IUserMessage)await channel.GetMessageAsync(existingFireteam.MessageId, CacheMode.AllowDownload);
            if (message == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find message to close").Build());
                return;
            }

            StringBuilder sb = new StringBuilder();
            foreach (ActivityUser player in existingFireteam.Players)
            {
                if (player.UserId == Context.User.Id) continue;
                sb.Append($"<@{player.UserId}> ");
            }

            sb.Append("Are you all still ok for this activity?");
            await message.ReplyAsync(sb.ToString());
            await RespondAsync("Success", ephemeral: true);
        }
        private async Task<bool> CheckCanAlertRaid(Raid existingRaid)
        {

            if (existingRaid == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find a Raid with that Id").Build());
                return false;
            }

            if (existingRaid.DateTimeClosed != DateTime.MinValue)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("This Raid is already closed").Build());
                return false;
            }
            if (existingRaid.CreatedByUserId != Context.User.Id)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Only the person who created the Raid can alert the players").Build());
                return false;
            }

            if (existingRaid.DateTimeAlerted.Date == DateTime.UtcNow.Date)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("You can only roll call/rollcall/alert once per day").Build());
                return false;
            }

            return true;
        }
        private async Task<Result> CheckCanAlertRaidButtonClick(ulong userId, Raid existingRaid)
        {
            if (existingRaid == null)
            {
                return new Result(false, "Could not find a Raid with that Id");
            }

            if (existingRaid.DateTimeClosed != DateTime.MinValue)
            {
                return new Result(false, "This Raid is already closed");
            }
            if (existingRaid.CreatedByUserId != userId)
            {
                return new Result(false, "Only the person who created the Raid can alert the players");
            }

            if (existingRaid.DateTimeAlerted.Date == DateTime.UtcNow.Date)
            {
                return new Result(false, "You can only alert once per day");
            }

            return new Result(true, string.Empty);
        }
        private async Task<bool> CheckCanAlertFireteam(Fireteam existingFireteam)
        {

            if (existingFireteam == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find a Fireteam with that Id").Build());
                return false;
            }

            if (existingFireteam.DateTimeClosed != DateTime.MinValue)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("This Fireteam is already closed").Build());
                return false;
            }
            if (existingFireteam.CreatedByUserId != Context.User.Id)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Only the person who created the Fireteam can alert the players").Build());
                return false;
            }

            if (existingFireteam.DateTimeAlerted.Date == DateTime.UtcNow.Date)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("You can only alert once per day").Build());
                return false;
            }

            return true;
        }
        private async Task<Result> CheckCanAlertFireteamButtonClick(ulong userId, Fireteam existingFireteam)
        {
            if (existingFireteam == null)
            {
                return new Result(false, "Could not find a Fireteam with that Id");
            }

            if (existingFireteam.DateTimeClosed != DateTime.MinValue)
            {
                return new Result(false, "This Fireteam is already closed");
            }
            if (existingFireteam.CreatedByUserId != userId)
            {
                return new Result(false, "Only the person who created the Fireteam can alert the players");
            }

            if (existingFireteam.DateTimeAlerted.Date == DateTime.UtcNow.Date)
            {
                return new Result(false, "You can only alert once per day");
            }

            return new Result(true, string.Empty);
        }


        [SlashCommand("list-open-raids", "List all open Raids")]
        [RequireUserPermission(Discord.GuildPermission.KickMembers)]
        public async Task ListOpenRaids()
        {
            //await DeferAsync();

            IList<Raid> openRaids = await _activityService.FindAllOpenRaids(Context.Guild.Id);
            openRaids = openRaids.OrderBy(x => x.DateTimeCreated).ToList();
            if (openRaids == null || openRaids.Count == 0)
            {
                await RespondAsync("There are no open raids!");
                return;
            }

            EmbedBuilder embedBuilder = new EmbedBuilder();
            StringBuilder sb = new StringBuilder();
            embedBuilder.WithTitle("Below is a list of open raids. The command to close the raid is at the beginning of each.");
            foreach (Raid openRaid in openRaids)
            {
                if (openRaid.GuidId != Context.Guild.Id)
                    continue;
                ITextChannel channel = Context.Guild.GetTextChannel(openRaid.ChannelId);
                if (channel == null) continue;
                IMessage message = await channel.GetMessageAsync(openRaid.MessageId);
                if (message == null) continue;

                double daysOld = Math.Round(DateTime.UtcNow.Subtract(openRaid.DateTimeCreated).TotalDays, 0);
                sb.AppendLine($"/close raid {openRaid.Id} | {daysOld} days open | Created By <@{openRaid.CreatedByUserId}> |\n{message.Embeds.First().Description}\n\n");
            }

            if (sb.Length == 0)
            {
                await RespondAsync("There are no open raids!");
                return;
            }

            embedBuilder.Description = sb.ToString();
            embedBuilder.WithDescription(sb.ToString());
            await RespondAsync(embed: embedBuilder.Build());
        }

        [SlashCommand("list-open-fireteams", "List all open Fireteams")]
        [RequireUserPermission(Discord.GuildPermission.KickMembers)]
        public async Task ListOpenFireteams()
        {
            //await DeferAsync();

            IList<Fireteam> openFireteams = await _activityService.FindAllOpenFireteams(Context.Guild.Id);
            openFireteams = openFireteams.OrderBy(x => x.DateTimeCreated).ToList();
            if (openFireteams == null || openFireteams.Count == 0)
            {
                await RespondAsync("There are no open fireteams!");
                return;
            }

            EmbedBuilder embedBuilder = new EmbedBuilder();
            StringBuilder sb = new StringBuilder();
            embedBuilder.WithTitle("Below is a list of open Fireteam. The command to close the fireteam is at the beginning of each.");
            foreach (Fireteam openFireteam in openFireteams)
            {
                if (openFireteam.GuidId != Context.Guild.Id)
                    continue;
                ITextChannel channel = Context.Guild.GetTextChannel(openFireteam.ChannelId);
                if (channel == null) continue;
                IMessage message = await channel.GetMessageAsync(openFireteam.MessageId);
                if (message == null) continue;

                double daysOld = Math.Round(DateTime.UtcNow.Subtract((DateTime)openFireteam?.DateTimeCreated).TotalDays, 0);
                sb.AppendLine($"/close fireteam {openFireteam.Id} | {daysOld} days open | Created By <@{openFireteam.CreatedByUserId}> |\n{message.Embeds.First().Description}\n\n");
            }

            if (sb.Length == 0)
            {
                await RespondAsync("There are no open fireteams!");
                return;
            }

            embedBuilder.Description = sb.ToString();
            embedBuilder.WithDescription(sb.ToString());
            await RespondAsync(embed: embedBuilder.Build());
        }

        [SlashCommand("last-activity", "Gets a list of all members last activities")]
        [ModOrMe]
        public async Task LastActivityList()
        {
            await DeferAsync();
            const string Moderator = "Moderator";
            const string ClanAdmin = "Clan Admin";
            IRole modRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == Moderator);
            if(modRole == null)
            {
                await FollowupAsync("No mod role found");
                return;
            }
            IRole clanAdminRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == ClanAdmin);
            if (clanAdminRole == null)
            {
                await FollowupAsync("No clan admin role found");
                return;
            }
            List<Raid> allRaids = await _activityService.GetAllRaids(Context.Guild.Id);
            allRaids = allRaids.OrderByDescending(x => x.Id).ToList();

            List<Fireteam> allFireteams = await _activityService.GetAllFireteams(Context.Guild.Id);
            allFireteams = allFireteams.OrderByDescending(x => x.Id).ToList();

            List<SocketGuildUser> users = Context.Guild.Users.ToList();
            users = users.ToList();
            StringBuilder clanAdminsStringBuilder = new StringBuilder();
            StringBuilder clanAdminsStringBuilder2 = new StringBuilder();
            StringBuilder moderatorsStringBuilder = new StringBuilder();
            StringBuilder moderatorsStringBuilder2 = new StringBuilder();
            StringBuilder membersStringBuilder = new StringBuilder();
            StringBuilder membersStringBuilder2 = new StringBuilder();
            StringBuilder membersStringBuilder3 = new StringBuilder();
            StringBuilder membersStringBuilder4 = new StringBuilder();
            List<LastActivityUser> activityUsers = new List<LastActivityUser>();
            foreach (SocketGuildUser user in users)
            {
                if (user.IsBot) continue;
                Raid lastRaid = allRaids.FirstOrDefault(x => x.CreatedByUserId == user.Id || x.Players.FirstOrDefault(y => y.UserId == user.Id) != null);
                Fireteam lastFireteam = allFireteams.FirstOrDefault(x => x.CreatedByUserId == user.Id || x.Players.FirstOrDefault(y => y.UserId == user.Id) != null);
                activityUsers.Add(new LastActivityUser(user.Username, lastRaid, lastFireteam, user.Roles));
            }
            foreach (LastActivityUser lastActivityUser in activityUsers.OrderByDescending(x => x.LastActivityDateTime))
            { 
                if (lastActivityUser.Roles.FirstOrDefault(x => x.Id == clanAdminRole.Id) != null)
                {
                    clanAdminsStringBuilder.AppendLine($"{lastActivityUser.Username} - {lastActivityUser.GetLastActivityText()}");

                }
                else if (lastActivityUser.Roles.FirstOrDefault(x => x.Id == modRole.Id) != null)
                {
                    moderatorsStringBuilder.AppendLine($"{lastActivityUser.Username} - {lastActivityUser.GetLastActivityText()}");
                }
                else
                {
                    if(membersStringBuilder.Length < 900) 
                        membersStringBuilder.AppendLine($"{lastActivityUser.Username} - {lastActivityUser.GetLastActivityText()}");
                    if(membersStringBuilder.Length >= 900)
                    {
                        membersStringBuilder2.AppendLine($"{lastActivityUser.Username} - {lastActivityUser.GetLastActivityText()}");
                    }
                    if(membersStringBuilder2.Length > 900)
                    {
                        membersStringBuilder3.AppendLine($"{lastActivityUser.Username} - {lastActivityUser.GetLastActivityText()}");
                    }
                }
            }

            string resultClanAdmin = clanAdminsStringBuilder.ToString();
            string resultModerator = moderatorsStringBuilder.ToString();
            if (string.IsNullOrEmpty(resultClanAdmin)) resultClanAdmin = "No clan admins";
            if (string.IsNullOrEmpty(resultModerator)) resultModerator = "No moderators";

            EmbedBuilder embed = new EmbedBuilder();
            embed.Title = "Members last activities";
            embed.Color = Color.Blue;
            embed.AddField("Clan Admins", resultClanAdmin);
            embed.AddField("Moderators", resultModerator);
            embed.AddField("Members", membersStringBuilder.ToString());
            if(membersStringBuilder2.Length > 0)
            {
                embed.AddField("Members Continued", membersStringBuilder2.ToString());
            }
            if (membersStringBuilder3.Length > 0)
            {
                embed.AddField("Members Continued", membersStringBuilder3.ToString());
            }
            await FollowupAsync(embed: embed.Build());
        }
    }
}
