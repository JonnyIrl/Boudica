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
    public class CreateActivityCommands : ActivityHelper
    {  
        public CreateActivityCommands(IServiceProvider services, CommandHandler handler): base(services, handler)
        {
            handler.OnCreateRaidModalSubmitted += OnCreateRaidModalSubmitted;
            handler.OnCreateFireteamModalSubmitted += OnCreateFireteamModalSubmitted;
        }

        private async Task<Result> OnCreateFireteamModalSubmitted(SocketModal modal, ITextChannel channel, string title, string description, string fireteamSize)
        {
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

            var buttons = new ComponentBuilder()
                   .WithButton("Edit Fireteam", $"{(int)ButtonCustomId.EditFireteam}-{newFireteam.Id}", ButtonStyle.Primary)
                   .WithButton("Alert Fireteam", $"{(int)ButtonCustomId.FireteamAlert}-{newFireteam.Id}", ButtonStyle.Primary)
                   .WithButton("Close Fireteam", $"{(int)ButtonCustomId.CloseFireteam}-{newFireteam.Id}", ButtonStyle.Danger);

            IUserMessage newMessage;
            IRole role = GetRoleForChannel(channel.Id);
            if (role != null && newFireteam.Players.Count != newFireteam.MaxPlayerCount)
            {
                await modal.RespondAsync(role.Mention, embed: embed.Build(), components: buttons.Build());
                newMessage = await modal.GetOriginalResponseAsync();
            }
            else
            {
                await modal.RespondAsync(embed: embed.Build(), components: buttons.Build());
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

        private async Task<Result> OnCreateRaidModalSubmitted(SocketModal modal, ITextChannel channel, string title, string description)
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
                    Players = new List<ActivityUser>()
                    {
                        new ActivityUser(guildUser.Id, guildUser.Username, true)
                    }
                };
                newRaid = await _activityService.CreateRaidAsync(newRaid);
                if (newRaid.Id <= 0)
                {
                    return new Result(false, "I couldn't create the raid because Jonny did something wrong!");
                }

                var embed = new EmbedBuilder();
                embed.WithColor(new Color(0, 255, 0));
                embed.WithAuthor(guildUser);

                embed.Title = title.Trim();
                embed.Description = description.Trim();

                AddActivityUsersField(embed, "Players", newRaid.Players);
                AddActivityUsersField(embed, "Subs", newRaid.Substitutes);

                EmbedHelper.UpdateFooterOnEmbed(embed, newRaid);

                var buttons = new ComponentBuilder()
                    .WithButton("Edit Raid", $"{(int)ButtonCustomId.EditRaid}-{newRaid.Id}", ButtonStyle.Primary)
                    .WithButton("Alert Raid", $"{(int)ButtonCustomId.RaidAlert}-{newRaid.Id}", ButtonStyle.Primary)
                    .WithButton("Close Raid", $"{(int)ButtonCustomId.CloseRaid}-{newRaid.Id}", ButtonStyle.Danger);


                IUserMessage newMessage;
                IRole role = GetRoleForChannel(channel.Id);
                if (role != null && newRaid.Players.Count != newRaid.MaxPlayerCount)
                {
                    // this will reply with the embed
                    await modal.RespondAsync(role.Mention, embed: embed.Build(), components: buttons.Build());
                    newMessage = await modal.GetOriginalResponseAsync();
                }
                else
                {
                    // this will reply with the embed
                    await modal.RespondAsync(embed: embed.Build(), components: buttons.Build());
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
                    await _activityService.DeleteRaidAsync(newRaid.Id);
                    return new Result(false, "Failed to create the raid!");
                }
                Console.Error.WriteLine("Exception creating raid", ex);
            }

            return new Result(true, string.Empty);
        }

        [SlashCommand("raid", "Create a Raid")]
        public async Task CreateRaidCommand(string raidDescription = null)
        {
            if (string.IsNullOrEmpty(raidDescription))
            {
                await RespondWithModalAsync(ModalHelper.CreateRaidModal());
                return;
            }
            Raid newRaid = null;
            try
            {
                List<ActivityUser> addedUsers = AddPlayersToNewActivity(raidDescription);
                newRaid = new Raid()
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
                foreach (ActivityUser activityUser in addedUsers)
                {
                    description = description.Replace($"<@{activityUser.UserId}>", string.Empty);
                }
                description = description.Trim();
                embed.Description = description;

                AddActivityUsersField(embed, "Players", newRaid.Players);
                AddActivityUsersField(embed, "Subs", newRaid.Substitutes);

                EmbedHelper.UpdateFooterOnEmbed(embed, newRaid);

                var buttons = new ComponentBuilder()
                    .WithButton("Edit Raid", $"{(int)ButtonCustomId.EditRaid}-{newRaid.Id}", ButtonStyle.Primary)
                    .WithButton("Alert Raid", $"{(int)ButtonCustomId.RaidAlert}-{newRaid.Id}", ButtonStyle.Primary)
                    .WithButton("Close Raid", $"{(int)ButtonCustomId.CloseRaid}-{newRaid.Id}", ButtonStyle.Danger);


                IUserMessage newMessage;
                IRole role = GetRoleForChannel(Context.Channel.Id);
                if (role != null && newRaid.Players.Count != newRaid.MaxPlayerCount)
                {
                    // this will reply with the embed
                    await RespondAsync(role.Mention, embed: embed.Build(), components: buttons.Build());
                    newMessage = await GetOriginalResponseAsync();
                }
                else
                {
                    // this will reply with the embed
                    await RespondAsync(embed: embed.Build(), components: buttons.Build());
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
            catch(Exception ex)
            {
                //Didn't get to post the raid into the chat so therefore delete
                if(newRaid != null && newRaid.MessageId == 0)
                {
                    await _activityService.DeleteRaidAsync(newRaid.Id);
                    await RespondAsync("Failed to create the raid!");
                }
                Console.Error.WriteLine("Exception creating raid", ex);
            }
        }

        [SlashCommand("fireteam", "Create a Fireteam")]
        public async Task CreateFireteamCommand(int fireteamSize = -1, string description = null)
        {
            if (fireteamSize == -1)
            {
                await RespondWithModalAsync(ModalHelper.CreateFireteamModal());
                return;
            }
            if (string.IsNullOrEmpty(description))
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command arguments, supply the total slots and a description for your fireteam e.g. ;create fireteam 3 Duality Dungeon ASAP will create a fireteam that a total of 3 people (including you) can join").Build());
                return;
            }

            if (fireteamSize > 6 || fireteamSize <= 1)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command arguments, supply the the total slots and a description for your fireteam e.g. ;create fireteam 3 Duality Dungeon ASAP will create a fireteam that a total of 3 people (including you) can join").Build());
                return;
            }

            List<ActivityUser> addedUsers = AddPlayersToNewActivity(description, fireteamSize - 1);
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
            sb.AppendLine(description);
            sb.AppendLine();
            sb.AppendLine();
            string result = sb.ToString();
            embed.WithAuthor(Context.User);

            foreach (ActivityUser activityUser in addedUsers)
            {
                result = result.Replace($"<@{activityUser.UserId}>", string.Empty);
            }
            result = result.Trim();
            embed.Description = result;

            AddActivityUsersField(embed, "Players", newFireteam.Players);
            AddActivityUsersField(embed, "Subs", newFireteam.Substitutes);

            EmbedHelper.UpdateFooterOnEmbed(embed, newFireteam);

            IUserMessage newMessage;
            IRole role = GetRoleForChannel(Context.Channel.Id);
            if (role != null && newFireteam.Players.Count != newFireteam.MaxPlayerCount)
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
    }

    [Group("edit", "Edit an activity")]
    public class EditActivityCommands : ActivityHelper
    {
        public EditActivityCommands(IServiceProvider services, CommandHandler handler): base(services, handler)
        {
            handler.OnEditRaidModalSubmitted += OnEditRaidModalSubmitted;
            handler.OnEditRaidButtonClicked += OnEditRaidButtonClick;

            handler.OnEditFireteamModalSubmitted += OnEditFireteamModalSubmitted;
            handler.OnEditFireteamButtonClicked += OnEditFireteamButtonClick;
        }

        private async Task<Result> OnEditRaidModalSubmitted(ITextChannel channel, string title, string description, int raidId)
        {
            Raid existingRaid = await _activityService.GetMongoRaidAsync(raidId);
            if (existingRaid == null)
            {
                return new Result(false, "Could not find Raid to edit");
            }

            IUserMessage message = (IUserMessage)await channel.GetMessageAsync(existingRaid.MessageId, CacheMode.AllowDownload);
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

            await component.RespondWithModalAsync(ModalHelper.EditRaidModal(existingRaid, embed.Title, embed.Description));
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
            handler.OnCloseRaidButtonClicked += OnCloseRaidButtonClicked;
            handler.OnCloseFireteamButtonClicked += OnCloseFireteamButtonClicked;
        }

        private async Task<Result> OnCloseRaidButtonClicked(SocketMessageComponent component, int raidId)
        {
            return await CloseRaidButtonClick(raidId, component);
        }

        private async Task<Result> OnCloseFireteamButtonClicked(SocketMessageComponent component, int fireteamId)
        {
            return await CloseFireteamButtonClick(fireteamId, component);
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

        public async Task<Result> CloseRaidButtonClick(int raidId, SocketMessageComponent component)
        {
            SocketGuildUser user = component.User as SocketGuildUser;
            if(user == null)
            {
                return new Result(false, "Failed, could not find user");
            }
            Raid existingRaid = await _activityService.GetMongoRaidAsync(raidId);
            Result exisingRaidResult = await CheckExistingRaidIsValidButtonClick(component, existingRaid, user, false);
            if (exisingRaidResult.Success == false) return exisingRaidResult;

            existingRaid.DateTimeClosed = DateTime.UtcNow;
            await _activityService.UpdateRaidAsync(existingRaid);

            IUserMessage message = component.Message as IUserMessage;
            if (message == null)
            {
                return new Result(false, "Could not find message to close");
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
                x.Components = null;
            });

            if (existingRaid.DateTimeClosed != DateTime.MinValue)
            {
                await component.RespondAsync(embed: EmbedHelper.CreateSuccessReply($"Raid {raidId} has been closed! <@{existingRaid.CreatedByUserId}> did this activity get completed?").Build());
                IUserMessage responseMessage = await component.GetOriginalResponseAsync();
                if (responseMessage != null)
                    await responseMessage.AddReactionsAsync(_successFailEmotes);
                return new Result(true, string.Empty);
            }
            else
            {
                existingRaid.AwardedGlimmer = true;
                await _activityService.UpdateRaidAsync(existingRaid);
                await component.RespondAsync(embed: EmbedHelper.CreateSuccessReply($"Raid Id {raidId} has been closed!").Build());
                return new Result(true, string.Empty);
            }
        }

        public async Task<Result> CloseFireteamButtonClick(int fireteamId, SocketMessageComponent component)
        {
            SocketGuildUser user = component.User as SocketGuildUser;
            if (user == null)
            {
                return new Result(false, "Failed, could not find user");
            }
            Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(fireteamId);
            Result exisingFireteamResult = await CheckExistingFireteamIsValidButtonClick(component, existingFireteam, user);
            if (exisingFireteamResult.Success == false) return exisingFireteamResult;

            existingFireteam.DateTimeClosed = DateTime.UtcNow;
            await _activityService.UpdateFireteamAsync(existingFireteam);

            IUserMessage message = component.Message as IUserMessage;
            if (message == null)
            {
                return new Result(false, "Could not find message to close");
            }
            var modifiedEmbed = new EmbedBuilder();
            var embed = message.Embeds.FirstOrDefault();
            EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingFireteam);
            EmbedHelper.UpdateFieldsOnEmbed(modifiedEmbed, embed);
            modifiedEmbed.Title = "This fireteam is now closed";
            modifiedEmbed.Color = Color.Red;
            await message.UnpinAsync();
            await message.ModifyAsync(x =>
            {
                x.Embed = modifiedEmbed.Build();
                x.Components = null;
            });

            if (existingFireteam.DateTimeClosed != DateTime.MinValue)
            {
                await component.RespondAsync(embed: EmbedHelper.CreateSuccessReply($"Fireteam {existingFireteam} has been closed! <@{existingFireteam.CreatedByUserId}> did this activity get completed?").Build());
                IUserMessage responseMessage = await component.GetOriginalResponseAsync();
                if (responseMessage != null)
                    await responseMessage.AddReactionsAsync(_successFailEmotes);
                return new Result(true, string.Empty);
            }
            else
            {
                existingFireteam.AwardedGlimmer = true;
                await _activityService.UpdateFireteamAsync(existingFireteam);
                await component.RespondAsync(embed: EmbedHelper.CreateSuccessReply($"Fireteam Id {existingFireteam.Id} has been closed!").Build());
                return new Result(true, string.Empty);
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
            handler.OnAlertRaidButtonClicked += OnAlertRaidButtonClicked;
            handler.OnAlertFireteamButtonClicked += OnAlertFireteamButtonClicked;
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
            foreach (ActivityUser userToRemove in usersToAdd)
            {
                IGuildUser guildUser = Context.Guild.GetUser(userToRemove.UserId);
                if (guildUser != null)
                {

                    ActivityUser existingUser = existingFireteam.Players.FirstOrDefault(x => x.UserId == userToRemove.UserId);
                    //Already added
                    if (existingUser != null)
                    {
                        continue;
                    }
                    else if (existingFireteam.Players.Count < existingFireteam.MaxPlayerCount)
                    {
                        sb.Append($"<@{userToRemove.UserId}>");
                        existingFireteam.Players.Add(new ActivityUser(guildUser.Id, guildUser.DisplayName));
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

            await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"{sb.ToString()} has been removed from the Raid {existingRaid.Id}").Build());
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

            await message.ReplyAsync(null, false, EmbedHelper.CreateSuccessReply($"{sb.ToString()} has been removed from the Fireteam {existingFireteam.Id}").Build());
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
    }
}
