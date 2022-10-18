using Boudica.Classes;
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
    public class ActivityCommands : ActivityHelper
    {  
        public ActivityCommands(IServiceProvider services, CommandHandler handler): base(services, handler)
        {

        }

        #region Mongo Raid
        [SlashCommand("raid", "Create a Raid")]
        public async Task CreateRaidCommand(string raidDescription)
        {
            if (string.IsNullOrEmpty(raidDescription))
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command arguments, supply a description for your raid e.g. /create raid Vow of Disciple Tuesday 28th 6pm").Build());
                return;
            }

            List<ActivityUser> addedUsers = AddPlayersToNewActivity(raidDescription);
            Raid newRaid = new Raid()
            {
                DateTimeCreated = DateTime.UtcNow,
                CreatedByUserId = Context.User.Id,
                GuidId = Context.Guild.Id,
                ChannelId = Context.Channel.Id,
                MaxPlayerCount = 6,
                Players = new List<ActivityUser>()
                {
                    new ActivityUser(Context.User.Id, Context.User.Username, true)
                }
            };
            if (addedUsers.Any())
            {
                newRaid.Players.AddRange(addedUsers);
            }
            newRaid = await _activityService.CreateRaidAsync(newRaid);
            if (newRaid.Id <= 0)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("I couldn't create the raid because Jonny did something wrong!").Build());
                return;
            }

            var embed = new EmbedBuilder();
            embed.WithColor(new Color(0, 255, 0));
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(raidDescription);
            embed.WithAuthor(Context.User);
            sb.AppendLine();
            sb.AppendLine();

            var user = Context.User;
            string description = sb.ToString();
            foreach(ActivityUser activityUser in addedUsers)
            {
                description = description.Replace($"<@{activityUser.UserId}>", string.Empty);
            }
            description = description.Trim();
            embed.Description = description;

            AddActivityUsersField(embed, "Players", newRaid.Players);
            AddActivityUsersField(embed, "Subs", newRaid.Substitutes);

            EmbedHelper.UpdateFooterOnEmbed(embed, newRaid);

            IUserMessage newMessage;
            IRole role = GetRoleForChannel(Context.Channel.Id);
            if (role != null)
            {
                // this will reply with the embed
                await Context.Interaction.ModifyOriginalResponseAsync(message =>
                {
                    message.Embed = embed.Build();
                    message.Content = role.Mention;
                });
                newMessage = await GetOriginalResponseAsync();
            }
            else
            {
                // this will reply with the embed
                await Context.Interaction.ModifyOriginalResponseAsync(message =>
                {
                    message.Embed = embed.Build();
                });
                newMessage = await GetOriginalResponseAsync();
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

        #endregion

        #region Fireteam
        [SlashCommand("fireteam", "Create a Fireteam")]
        public async Task CreateFireteamCommand(int fireteamSize, string args)
        {
            if (args == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command arguments, supply the total slots and a description for your fireteam e.g. ;create fireteam 3 Duality Dungeon ASAP will create a fireteam that a total of 3 people (including you) can join").Build());
                return;
            }

            string[] sizeSplit = args.Split(" ");

            if (fireteamSize > 6 || fireteamSize <= 1)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command arguments, supply the the total slots and a description for your fireteam e.g. ;create fireteam 3 Duality Dungeon ASAP will create a fireteam that a total of 3 people (including you) can join").Build());
                return;
            }

            if (string.IsNullOrEmpty(args.Remove(0, 1)))
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command arguments, you must supply a description for your fireteam e.g. ;create fireteam 3 Duality Dungeon ASAP will create a fireteam that a total of 3 people (including you) can join").Build());
                return;
            }

            List<ActivityUser> addedUsers = AddPlayersToNewActivity(args, fireteamSize - 1);
            Fireteam newFireteam = new Fireteam()
            {
                DateTimeCreated = DateTime.UtcNow,
                CreatedByUserId = Context.User.Id,
                GuidId = Context.Guild.Id,
                ChannelId = Context.Channel.Id,
                MaxPlayerCount = (byte)fireteamSize,
                Players = new List<ActivityUser>()
                {
                    new ActivityUser(Context.User.Id, Context.User.Username, true)
                }
            };

            if (addedUsers.Any())
            {
                newFireteam.Players.AddRange(addedUsers);
            }
            newFireteam = await _activityService.CreateFireteamAsync(newFireteam);
            if (newFireteam.Id <= 0)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("I couldn't create the fireteam because Jonny did something wrong!").Build());
                return;
            }

            var embed = new EmbedBuilder();
            embed.WithColor(new Color(0, 255, 0));
            StringBuilder sb = new StringBuilder();
            //Remove the number for the size of the fireteam from the string for the Description
            sb.AppendLine(args);
            sb.AppendLine();
            sb.AppendLine();
            string description = sb.ToString();
            embed.WithAuthor(Context.User);

            foreach (ActivityUser activityUser in addedUsers)
            {
                description = description.Replace($"<@{activityUser.UserId}>", string.Empty);
            }
            description = description.Trim();
            embed.Description = description;

            AddActivityUsersField(embed, "Players", newFireteam.Players);
            AddActivityUsersField(embed, "Subs", newFireteam.Substitutes);

            EmbedHelper.UpdateFooterOnEmbed(embed, newFireteam);

            IUserMessage newMessage;
            IRole role = GetRoleForChannel(Context.Channel.Id);
            if (role != null)
            {
                await RespondAsync(role.Mention, embed: embed.Build());
                newMessage = await GetOriginalResponseAsync();
            }
            else
            {
                await RespondAsync(embed: embed.Build());
                newMessage = await GetOriginalResponseAsync();
            }

            newFireteam.MessageId = newMessage.Id;
            await _activityService.UpdateFireteamAsync(newFireteam);

            await newMessage.PinAsync();
            await newMessage.AddReactionsAsync(new List<IEmote>()
            {
                new Emoji("🇯"),
                new Emoji("🇸"),
            });
        }
        
        private async Task CalculateGlimmerForActivity(List<ActivityUser> activityUsers, ulong creatorId)
        {
            if (activityUsers == null) return;
            int increaseAmount = 1 * activityUsers.Count;
            foreach(ActivityUser user in activityUsers)
            {
                if (user.UserId == creatorId)
                {
                    await _guardianService.IncreaseGlimmerAsync(user.UserId, user.DisplayName, increaseAmount + 3);
                    Console.WriteLine($"Increased Glimmer for {user.DisplayName} by {increaseAmount + 3}");
                }
                else if(user.Reacted)
                {
                    await _guardianService.IncreaseGlimmerAsync(user.UserId, user.DisplayName, increaseAmount);
                    Console.WriteLine($"Increased Glimmer for {user.DisplayName} by {increaseAmount}");
                }
            }
        }

        private IRole GetRoleForChannel(ulong channelId)
        {
            switch(channelId)
            {
                case RaidChannel:
                    return Context.Guild.Roles.FirstOrDefault(x => x.Name == RaidRole);
                case VanguardChannel:
                    return Context.Guild.Roles.FirstOrDefault(x => x.Name == VanguardRole);
                case CrucibleChannel:
                    return Context.Guild.Roles.FirstOrDefault(x => x.Name == CrucibleRole);
                case GambitChannel:
                    return Context.Guild.Roles.FirstOrDefault(x => x.Name == GambitRole);
                case MiscChannel:
                    return Context.Guild.Roles.FirstOrDefault(x => x.Name == MiscRole);
                case DungeonChannel:
                    return Context.Guild.Roles.FirstOrDefault(x => x.Name == DungeonRole);
            }

            return null;
        }
        #endregion

    }

    [Group("edit", "Edit an activity")]
    public class EditActivityCommands : ActivityHelper
    {
        public EditActivityCommands(IServiceProvider services, CommandHandler handler): base(services, handler)
        {

        }

        [SlashCommand("raid", "Edit a Raid")]
        public async Task EditRaid(int raidId, string newDescription)
        {
            Raid existingRaid = await _activityService.GetMongoRaidAsync(raidId);
            bool existingRaidResult = await CheckExistingRaidIsValid(existingRaid, false);
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

            var modifiedEmbed = new EmbedBuilder();
            var embed = message.Embeds.FirstOrDefault();
            modifiedEmbed.Description = newDescription;
            EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateTitleColorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingRaid);
            EmbedHelper.UpdateFieldsOnEmbed(modifiedEmbed, embed);
            await message.ModifyAsync(x =>
            {
                x.Embed = modifiedEmbed.Build();
            });

            await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"The raid Id {raidId} has been edited!").Build());
        }

        [SlashCommand("fireteam", "Edit a Fireteam")]
        public async Task EditFireteam(int fireteamId, string newDescription)
        {
            Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(fireteamId);
            bool existingFireteamResult = await CheckExistingFireteamIsValid(existingFireteam);
            if (existingFireteamResult == false) return;

            ITextChannel channel = Context.Guild.GetTextChannel(existingFireteam.ChannelId);
            if (channel == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find channel where message is").Build());
                return;
            }

            IUserMessage message = (IUserMessage)await Context.Channel.GetMessageAsync(existingFireteam.MessageId, CacheMode.AllowDownload);
            if (message == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find message to edit").Build());
                return;
            }

            var modifiedEmbed = new EmbedBuilder();
            var embed = message.Embeds.FirstOrDefault();
            modifiedEmbed.Description = newDescription;
            EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateTitleColorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingFireteam);
            EmbedHelper.UpdateFieldsOnEmbed(modifiedEmbed, embed);
            await message.ModifyAsync(x =>
            {
                x.Embed = modifiedEmbed.Build();
            });

            await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"The fireteam Id {fireteamId} has been edited!").Build());
        }
    }

    [Group("close", "Close an activity")]
    public class CloseActivityCommands : ActivityHelper
    {
        public CloseActivityCommands(IServiceProvider services, CommandHandler handler) : base(services, handler)
        {

        }

        [SlashCommand("fireteam", "Close a Fireteam")]
        public async Task CloseFireteam(int fireteamId)
        {
            Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(fireteamId);
            bool existingFireteamResult = await CheckExistingFireteamIsValid(existingFireteam);
            if (existingFireteamResult == false) return;

            if (existingFireteam.DateTimeClosed == DateTime.MinValue)
            {
                existingFireteam.DateTimeClosed = DateTime.UtcNow;
                await _activityService.UpdateFireteamAsync(existingFireteam);
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
            var modifiedEmbed = new EmbedBuilder();
            var embed = message.Embeds.FirstOrDefault();
            if (embed?.Title == "This activity is now closed")
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("This Fireteam is already closed").Build());
                return;
            }

            EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingFireteam);
            EmbedHelper.UpdateFieldsOnEmbed(modifiedEmbed, embed);
            modifiedEmbed.Title = "This activity is now closed";
            modifiedEmbed.Color = Color.Red;
            await message.UnpinAsync();
            await message.ModifyAsync(x =>
            {
                x.Embed = modifiedEmbed.Build();
            });

            if (existingFireteam.DateTimeClosed != DateTime.MinValue)
            {
                await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"Fireteam {fireteamId} has been closed! <@{existingFireteam.CreatedByUserId}> did this activity get completed?").Build());
                IUserMessage responseMessage = await GetOriginalResponseAsync();
                if (responseMessage != null) await responseMessage.AddReactionsAsync(_successFailEmotes);
            }
            else
            {
                existingFireteam.AwardedGlimmer = true;
                await _activityService.UpdateFireteamAsync(existingFireteam);
                await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"Fireteam {fireteamId} has been closed!").Build());
            }
        }

        [DefaultMemberPermissions(GuildPermission.KickMembers)]
        [SlashCommand("fireteam-forceclose", "Force close a Fireteam")]
        public async Task ForceCloseFireteam(int fireteamId)
        {
            Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(fireteamId);
            bool existingFireteamResult = await CheckExistingFireteamIsValid(existingFireteam);
            if (existingFireteamResult == false) return;

            if (existingFireteam.DateTimeClosed == DateTime.MinValue)
            {
                existingFireteam.DateTimeClosed = DateTime.UtcNow;
                await _activityService.UpdateFireteamAsync(existingFireteam);
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
            var modifiedEmbed = new EmbedBuilder();
            var embed = message.Embeds.FirstOrDefault();
            if (embed?.Title == "This activity is now closed")
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("This Fireteam is already closed").Build());
                return;
            }

            EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingFireteam);
            EmbedHelper.UpdateFieldsOnEmbed(modifiedEmbed, embed);
            modifiedEmbed.Title = "This activity is now closed";
            modifiedEmbed.Color = Color.Red;
            await message.UnpinAsync();
            await message.ModifyAsync(x =>
            {
                x.Embed = modifiedEmbed.Build();
            });

            existingFireteam.AwardedGlimmer = true;
            await _activityService.UpdateFireteamAsync(existingFireteam);
            await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"Fireteam {fireteamId} has been closed!").Build());
        }


        [SlashCommand("raid", "Close a Raid")]
        public async Task CloseRaid(int raidId)
        {
            Raid existingRaid = await _activityService.GetMongoRaidAsync(raidId);
            bool exisingRaidResult = await CheckExistingRaidIsValid(existingRaid, false);
            if (exisingRaidResult == false) return;

            existingRaid.DateTimeClosed = DateTime.UtcNow;
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
            var modifiedEmbed = new EmbedBuilder();
            var embed = message.Embeds.FirstOrDefault();
            EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingRaid);
            EmbedHelper.UpdateFieldsOnEmbed(modifiedEmbed, embed);
            modifiedEmbed.Title = "This raid is now closed";
            modifiedEmbed.Color = Color.Red;
            await message.UnpinAsync();
            await message.ModifyAsync(x =>
            {
                x.Embed = modifiedEmbed.Build();
            });

            if (existingRaid.DateTimeClosed != DateTime.MinValue)
            {
                await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"Raid {raidId} has been closed! <@{existingRaid.CreatedByUserId}> did this activity get completed?").Build());
                IUserMessage responseMessage = await GetOriginalResponseAsync();
                if(responseMessage != null) 
                    await responseMessage.AddReactionsAsync(_successFailEmotes);
            }
            else
            {
                existingRaid.AwardedGlimmer = true;
                await _activityService.UpdateRaidAsync(existingRaid);
                await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"Raid Id {raidId} has been closed!").Build());
            }
        }

        [DefaultMemberPermissions(GuildPermission.KickMembers)]
        [SlashCommand("raid-forceclose", "Used to force close raids")]
        public async Task ForceCloseRaid(int raidId)
        {
            Raid existingRaid = await _activityService.GetMongoRaidAsync(raidId);
            bool exisingRaidResult = await CheckExistingRaidIsValid(existingRaid, true);
            if (exisingRaidResult == false) return;

            existingRaid.DateTimeClosed = DateTime.UtcNow;
            existingRaid.AwardedGlimmer = true;
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
            var modifiedEmbed = new EmbedBuilder();
            var embed = message.Embeds.FirstOrDefault();
            EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingRaid);
            EmbedHelper.UpdateFieldsOnEmbed(modifiedEmbed, embed);
            modifiedEmbed.Title = "This raid is now closed";
            modifiedEmbed.Color = Color.Red;
            await message.UnpinAsync();
            await message.ModifyAsync(x =>
            {
                x.Embed = modifiedEmbed.Build();
            });

            await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"Raid Id {raidId} has been closed!").Build());
        }

    }

    public class OtherActivityCommands : ActivityHelper
    {
        public OtherActivityCommands(IServiceProvider services, CommandHandler handler): base(services, handler)
        {

        }

        [SlashCommand("add-player", "Add Player to activity")]
        public async Task AddToActivity(Enums.ActivityType activityType, int id, string playersToAdd)
        {
            switch(activityType)
            {
                case Enums.ActivityType.Raid:
                    await AddPlayerToRaid(id, playersToAdd);
                    break;

                case Enums.ActivityType.Fireteam:
                    break;
            }
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
            foreach (ActivityUser userToRemove in usersToAdd)
            {
                IGuildUser guildUser = Context.Guild.GetUser(userToRemove.UserId);
                if (guildUser != null)
                {

                    ActivityUser existingUser = existingRaid.Players.FirstOrDefault(x => x.UserId == userToRemove.UserId);
                    //Already added
                    if (existingUser != null)
                    {
                        continue;
                    }
                    else if (existingRaid.Players.Count < existingRaid.MaxPlayerCount)
                    {
                        sb.Append($"<@{userToRemove.UserId}>");
                        existingRaid.Players.Add(new ActivityUser(guildUser.Id, guildUser.DisplayName));
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

            await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"{sb.ToString()} has been added to the Raid").Build());
        }

        [SlashCommand("alert", "Alert all members of an activity")]
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
        private async Task<bool> CheckCanAlertFireteam(Fireteam existingFireteam)
        {

            if (existingFireteam == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find a Raid with that Id").Build());
                return false;
            }

            if (existingFireteam.DateTimeClosed != DateTime.MinValue)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("This Raid is already closed").Build());
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

        [SlashCommand("remove-player", "Remove a player from an activity")]
        public async Task RemovePlayerFromFireteam(Enums.ActivityType activityType, int id, string playersToRemove)
        {
            switch(activityType)
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

            foreach (ActivityUser userToRemove in usersToRemove)
            {
                IGuildUser guildUser = Context.Guild.GetUser(userToRemove.UserId);
                if (guildUser != null)
                {
                    sb.Append($"<@{userToRemove.UserId}>");
                    existingRaid.Players.RemoveAll(x => x.UserId == userToRemove.UserId);
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

            await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"{sb.ToString()} has been removed from the Raid").Build());
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
            foreach (ActivityUser userToRemove in usersToRemove)
            {
                IGuildUser guildUser = Context.Guild.GetUser(userToRemove.UserId);
                if (guildUser != null)
                {
                    sb.Append($"<@{userToRemove.UserId}>");
                    existingFireteam.Players.RemoveAll(x => x.UserId == userToRemove.UserId);
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

            await message.ReplyAsync(null, false, EmbedHelper.CreateSuccessReply($"{sb.ToString()} has been removed from the Fireteam").Build());
            await RespondAsync("Success", ephemeral: true);

        }
        private ActivityUser CreateActivityUser(SocketGuildUser user)
        {
            if (user != null && user.IsBot == false)
            {
                return new ActivityUser(user.Id, user.DisplayName);
            }

            return null;
        }
    }
}
