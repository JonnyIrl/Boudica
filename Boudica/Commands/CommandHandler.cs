using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Boudica.Classes;
using Boudica.Database.Models;
using Boudica.Helpers;
using Boudica.MongoDB.Models;
using Boudica.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Boudica.Commands
{
    public class CommandHandler
    {
        private const string RaidIsClosed = "This raid is now closed";
        private const string ActivityIsClosed = "This activity is now closed";
        private const string AMaxOf = "A max of ";
        private const string PlayersMayJoin = "players may join";
        // setup fields to be set later in the constructor
        private readonly IConfiguration _config;
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        private readonly ActivityService _activityService;
        private readonly RaidGroupService _raidGroupService;
        private char Prefix = ';';

        private static List<ulong> _manualRemovedReactionList = new List<ulong>();
        private object _lock = new object();
        //private char SlashPrefix = '/';

        private Emoji _jEmoji = new Emoji("🇯");
        private Emoji _sEmoji = new Emoji("🇸");

        public CommandHandler(IServiceProvider services)
        {
            // juice up the fields with these services
            // since we passed the services in, we can use GetRequiredService to pass them into the fields set earlier
            _config = services.GetRequiredService<IConfiguration>();
            _commands = services.GetRequiredService<CommandService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _activityService = services.GetRequiredService<ActivityService>();
            _raidGroupService = services.GetRequiredService<RaidGroupService>();
            _services = services;

            // get prefix from the configuration file
            Prefix = Char.Parse(_config["Prefix"]);

            // take action when we execute a command
            _commands.CommandExecuted += CommandExecutedAsync;

            _client.SlashCommandExecuted += SlashCommandHandler;

            // take action when we receive a message (so we can process it, and see if it is a valid command)
            _client.MessageReceived += MessageReceivedAsync;

            //Listen for Reactions
            _client.ReactionAdded += ReactionAddedAsync;
            _client.ReactionRemoved += ReactionRemovedAsync;

            //Listen for modals
            _client.ModalSubmitted += ModalSubmitted;
        }

        private async Task ModalSubmitted(SocketModal arg)
        {
            List<SocketMessageComponentData> components = arg.Data.Components.ToList();
            StringBuilder sb = new StringBuilder();
            foreach(SocketMessageComponentData component in components)
            {
                sb.AppendLine(component.CustomId + " - " + component.Value);
            }

            await arg.RespondAsync(sb.ToString());
        }

        public async Task InitializeAsync()
        {
            // register modules that are public and inherit ModuleBase<T>.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        // this class is where the magic starts, and takes actions upon receiving messages
        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            // ensures we don't process system/other bot messages
            if (!(rawMessage is SocketUserMessage message))
            {
                return;
            }

            if (message.Source != MessageSource.User)
            {
                return;
            }

            // sets the argument position away from the prefix we set
            var argPos = 0;

            // determine if the message has a valid prefix, and adjust argPos based on prefix
            if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasCharPrefix(Prefix, ref argPos)))
            {
                //Task.Run(() => { ReplyWhereIsXur(message); });
                return;
            }

            var context = new SocketCommandContext(_client, message);

            // execute command if one is found that matches
            await _commands.ExecuteAsync(context, argPos, _services);
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            //TODO
        }

        public async Task ReplyWhereIsXur(SocketUserMessage message)
        {
            string content = message.Content;
            if (string.IsNullOrEmpty(content)) return;
            if (content.Contains("<@244209636897456129>") == false) return;
            if (content.ToLower().Contains("xur") && content.ToLower().Contains("where"))
            {
                var context = new SocketCommandContext(_client, message);
                await context.Channel.SendMessageAsync(null, false, EmbedHelper.CreateSuccessReply($"Here you go {message.Author.Username}, as you were too lazy to look yourself, let me help you!\n\nhttps://letmegooglethat.com/?q=where+the+fuck+is+Xur&l=1").Build());
            }
        }
        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // if a command isn't found, log that info to console and exit this method
            if (!command.IsSpecified)
            {
                if (command.GetValueOrDefault() == null)
                {
                    Console.WriteLine($"Command failed to execute for [] <-> []!");
                    return;
                }
                else
                {
                    Console.WriteLine($"Command failed to execute for {context.User.Username} <-> {context.Message.Content}!");
                }
            }


            // log success to the console and exit this method
            if (result.IsSuccess)
            {
                Console.WriteLine($"Command {context.Message.Content} executed for -> {context.User.Username}");
                return;
            }


            // failure scenario, let's let the user know
            await context.Channel.SendMessageAsync($"Sorry, ... something went wrong, blame Jonny!");
        }

        public async Task TestReactionAddedAsync(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            if (reaction.Emote.Name == "🇯")
            {
                var user = await reaction.Channel.GetUserAsync(reaction.UserId) as SocketGuildUser;
                if (user == null || user.IsBot)
                {
                    return;
                }
                ActivityResponse result = await AddPlayerToActivityV2(message, user);
                if (result.Success == false)
                {
                    var originalMessage = await message.GetOrDownloadAsync();
                    await originalMessage.RemoveReactionAsync(reaction.Emote, user);
                    if (result.IsFull)
                    {
                        await originalMessage.ReplyAsync(null, false, EmbedHelper.CreateFailedReply($"<@{user.Id}>, sorry " + result.FullMessage).Build());
                    }
                }
                else if (result.Success && result.PreviousReaction)
                {
                    var originalMessage = await message.GetOrDownloadAsync();
                    await originalMessage.RemoveReactionAsync(Emoji.Parse(":regional_indicator_s:"), user);
                }
                return;
            }

            if (reaction.Emote.Name == "🇸")
            {
                var user = await reaction.Channel.GetUserAsync(reaction.UserId) as SocketGuildUser;
                if (user == null || user.IsBot)
                {
                    return;
                }
                ActivityResponse result = await AddSubToActivityV2(message, user);
                if (result.Success == false)
                {
                    var originalMessage = await message.GetOrDownloadAsync();
                    await originalMessage.RemoveReactionAsync(reaction.Emote, user);
                }
                else if (result.Success && result.PreviousReaction)
                {
                    var originalMessage = await message.GetOrDownloadAsync();
                    await originalMessage.RemoveReactionAsync(Emoji.Parse(":regional_indicator_j:"), user);
                }

                return;
            }

            //if (reaction.Emote.Name == "👍")
            //{
            //    var originalMessage = await message.GetOrDownloadAsync();
            //    var embeds = originalMessage.Embeds.ToList();
            //    var embed = embeds?.First();
            //    if (embed != null)
            //    {
            //        EmbedBuilder newEmbed = new EmbedBuilder();
            //        newEmbed.Color = embed.Color;
            //        newEmbed.Title = embed.Title;
            //        newEmbed.Description = embed.Description + "\n\nI see you reacted!";
            //        await originalMessage.ModifyAsync(x =>
            //        {
            //            x.Embed = newEmbed.Build();
            //        });
            //    }
            //}
        }

        public async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            if (reaction.Emote.Name == "🇯")
            {
                var user = await reaction.Channel.GetUserAsync(reaction.UserId) as SocketGuildUser;
                if (user == null || user.IsBot)
                {
                    return;
                }
                ActivityResponse result = await TestAddPlayerToActivityV2(message, user);
                if (result.Success == false)
                {
                    var originalMessage = await message.GetOrDownloadAsync();
                    await originalMessage.RemoveReactionAsync(reaction.Emote, user);
                    if(result.IsFull)
                    {
                        await originalMessage.ReplyAsync(null, false, EmbedHelper.CreateFailedReply($"<@{user.Id}>, sorry " + result.FullMessage).Build());
                    }
                }
                else if (result.Success && result.PreviousReaction)
                {
                    var originalMessage = await message.GetOrDownloadAsync();
                    lock (_lock)
                    {
                        _manualRemovedReactionList.Add(user.Id);
                    }
                    await originalMessage.RemoveReactionAsync(Emoji.Parse(":regional_indicator_s:"), user);
                }
                return;
            }

            if (reaction.Emote.Name == "🇸")
            {
                var user = await reaction.Channel.GetUserAsync(reaction.UserId) as SocketGuildUser;
                if (user == null || user.IsBot)
                {
                    return;
                }
                ActivityResponse result = await TestAddSubstituteToActivityV2(message, user);
                if (result.Success == false)
                {
                    var originalMessage = await message.GetOrDownloadAsync();
                    await originalMessage.RemoveReactionAsync(reaction.Emote, user);
                }
                else if (result.Success && result.PreviousReaction)
                {
                    var originalMessage = await message.GetOrDownloadAsync();
                    lock (_lock)
                    {
                        _manualRemovedReactionList.Add(user.Id);
                    }
                    await originalMessage.RemoveReactionAsync(Emoji.Parse(":regional_indicator_j:"), user);
                }

                return;
            }

            //if (reaction.Emote.Name == "👍")
            //{
            //    var originalMessage = await message.GetOrDownloadAsync();
            //    var embeds = originalMessage.Embeds.ToList();
            //    var embed = embeds?.First();
            //    if (embed != null)
            //    {
            //        EmbedBuilder newEmbed = new EmbedBuilder();
            //        newEmbed.Color = embed.Color;
            //        newEmbed.Title = embed.Title;
            //        newEmbed.Description = embed.Description + "\n\nI see you reacted!";
            //        await originalMessage.ModifyAsync(x =>
            //        {
            //            x.Embed = newEmbed.Build();
            //        });
            //    }
            //}
        }
        public async Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            if (reaction.Emote.Name == "🇯")
            {
                lock (_lock)
                {
                    if (_manualRemovedReactionList.Contains(reaction.UserId))
                    {
                        _manualRemovedReactionList.Remove(reaction.UserId);
                        return;
                    }
                }

                var user = await reaction.Channel.GetUserAsync(reaction.UserId) as SocketGuildUser;
                if (user == null || user.IsBot)
                {
                    return;
                }
                ActivityResponse result = await TestRemovePlayerFromActivityV2(message, user); //await RemovePlayerFromActivityV2(message, reaction.UserId);
                return;
            }

            if (reaction.Emote.Name == "🇸")
            {
                lock (_lock)
                {
                    if (_manualRemovedReactionList.Contains(reaction.UserId))
                    {
                        _manualRemovedReactionList.Remove(reaction.UserId);
                        return;
                    }
                }
                var user = await reaction.Channel.GetUserAsync(reaction.UserId) as SocketGuildUser;
                if (user == null || user.IsBot)
                {
                    return;
                }
                ActivityResponse result = await TestRemoveSubFromActivityV2(message, user); //await RemoveSubFromActivityV2(message, reaction.UserId);
                return;
            }
        }
        private async Task<ActivityResponse> TestAddPlayerToActivityV2(Cacheable<IUserMessage, ulong> message, SocketGuildUser user)
        {
            ActivityResponse activityResponse = new ActivityResponse(false, false);
            var originalMessage = await message.GetOrDownloadAsync();
            var embeds = originalMessage.Embeds.ToList();
            var embed = embeds?.First();
            if (embed != null)
            {
                #region Check Closed
                if (embed.Title == RaidIsClosed || embed.Title == ActivityIsClosed)
                {
                    return activityResponse;
                }
                #endregion


                ActivityType activityType = new ActivityType();
                activityType.Parse(embed);
                if (activityType.Activity == ActivityTypes.Unknown)
                    return activityResponse;

                if (activityType.Activity == ActivityTypes.Raid)
                {
                    MongoDB.Models.Raid existingRaid = await _activityService.GetMongoRaidAsync(activityType.Id);
                    if (existingRaid == null)
                    {
                        return activityResponse;
                    }

                    if (existingRaid.MaxPlayerCount == existingRaid.Players.Count)
                    {
                        return activityResponse;
                    }

                    //Player is a Sub
                    ActivityUser subUser = existingRaid.Substitutes?.Find(x => x.UserId == user.Id);
                    if (subUser != null)
                    {
                        existingRaid.Substitutes?.Remove(subUser);
                    }
                    //Already Joined
                    else if (existingRaid.Players.FirstOrDefault(x => x.UserId == user.Id) != null)
                    {
                        activityResponse.Success = true;
                        return activityResponse;
                    }

                    //Now Add them to the Players list
                    existingRaid.Players.Add(new ActivityUser(user.Id, user.Username));
                    existingRaid = await _activityService.UpdateRaidAsync(existingRaid);
                    if (existingRaid.Players.Count == existingRaid.MaxPlayerCount)
                    {
                        activityResponse.IsFull = true;
                        activityResponse.FullMessage = $"Raid Id {existingRaid.Id} is now full. Feel free to Sub or watch if a slot becomes available!";
                    }

                    var modifiedEmbed = new EmbedBuilder();
                    AddActivityUsersField(modifiedEmbed, "Players", existingRaid.Players);
                    AddActivityUsersField(modifiedEmbed, "Substitutes", existingRaid.Substitutes);

                    EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, embed);

                    await originalMessage.ModifyAsync(x =>
                    {
                        x.Embed = modifiedEmbed.Build();
                    });

                    if (existingRaid.Players.Count == existingRaid.MaxPlayerCount)
                    {
                        try
                        {
                            await originalMessage.ReplyAsync($"<@{existingRaid.CreatedByUserId}>, your Raid is now full!");
                        }
                        catch (Exception ex)
                        {

                        }
                    }

                    activityResponse.Success = true;
                    return activityResponse;
                }
                else if (activityType.Activity == ActivityTypes.Fireteam)
                {
                    MongoDB.Models.Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(activityType.Id);
                    if (existingFireteam == null)
                    {
                        return activityResponse;
                    }

                    if (existingFireteam.MaxPlayerCount == existingFireteam.Players.Count)
                    {
                        return activityResponse;
                    }

                    //Player is a Sub
                    ActivityUser subUser = existingFireteam.Substitutes?.Find(x => x.UserId == user.Id);
                    if (subUser != null)
                    {
                        existingFireteam.Substitutes?.Remove(subUser);
                        activityResponse.PreviousReaction = true;
                    }
                    //Already Joined
                    else if (existingFireteam.Players.FirstOrDefault(x => x.UserId == user.Id) != null)
                    {
                        activityResponse.Success = true;
                        return activityResponse;
                    }

                    //Now Add them to the Players list
                    existingFireteam.Players.Add(new ActivityUser(user.Id, user.Username));
                    existingFireteam = await _activityService.UpdateRaidAsync(existingFireteam);
                    if (existingFireteam.Players.Count == existingFireteam.MaxPlayerCount)
                    {
                        activityResponse.IsFull = true;
                        activityResponse.FullMessage = $"Fireteam Id {existingFireteam.Id} is now full. Feel free to Sub or watch if a slot becomes available!";
                    }

                    var modifiedEmbed = new EmbedBuilder();
                    AddActivityUsersField(modifiedEmbed, "Players", existingFireteam.Players);
                    AddActivityUsersField(modifiedEmbed, "Substitutes", existingFireteam.Substitutes);

                    EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, embed);

                    await originalMessage.ModifyAsync(x =>
                    {
                        x.Embed = modifiedEmbed.Build();
                    });

                    if (existingFireteam.Players.Count == existingFireteam.MaxPlayerCount)
                    {
                        try
                        {
                            await originalMessage.ReplyAsync($"<@{existingFireteam.CreatedByUserId}>, your Fireteam is now full!");
                        }
                        catch (Exception ex)
                        {

                        }
                    }

                    activityResponse.Success = true;
                    return activityResponse;
                }

                return activityResponse;
            }
            return activityResponse;
        }

        private async Task<ActivityResponse> TestRemovePlayerFromActivityV2(Cacheable<IUserMessage, ulong> message, SocketGuildUser user)
        {
            ActivityResponse activityResponse = new ActivityResponse(false, false);
            var originalMessage = await message.GetOrDownloadAsync();
            var embeds = originalMessage.Embeds.ToList();
            var embed = embeds?.First();
            if (embed != null)
            {
                #region Check Closed
                if (embed.Title == RaidIsClosed || embed.Title == ActivityIsClosed)
                {
                    return activityResponse;
                }
                #endregion


                ActivityType activityType = new ActivityType();
                activityType.Parse(embed);
                if (activityType.Activity == ActivityTypes.Unknown)
                    return activityResponse;

                if (activityType.Activity == ActivityTypes.Raid)
                {
                    MongoDB.Models.Raid existingRaid = await _activityService.GetMongoRaidAsync(activityType.Id);
                    if (existingRaid == null)
                    {
                        return activityResponse;
                    }

                    //Player is a Sub
                    ActivityUser playerUser = existingRaid.Players?.Find(x => x.UserId == user.Id);
                    if (playerUser != null)
                    {
                        existingRaid.Players?.Remove(playerUser);
                    }

                    existingRaid = await _activityService.UpdateRaidAsync(existingRaid);

                    var modifiedEmbed = new EmbedBuilder();
                    AddActivityUsersField(modifiedEmbed, "Players", existingRaid.Players);
                    AddActivityUsersField(modifiedEmbed, "Subs", existingRaid.Substitutes);

                    EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, embed);

                    await originalMessage.ModifyAsync(x =>
                    {
                        x.Embed = modifiedEmbed.Build();
                    });

                    if (existingRaid.Players.Count == existingRaid.MaxPlayerCount - 1)
                    {
                        try
                        {
                            IRole role = user.Guild.Roles.FirstOrDefault(x => x.Name == "Raid Fanatics");
                            if (role != null)
                            {
                                await originalMessage.ReplyAsync(role.Mention + " A slot has now opened up!");
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }

                    await originalMessage.ReplyAsync(null, false, EmbedHelper.CreateInfoReply($"<@{user.Id}> has left Raid Id {existingRaid.Id}").Build());

                    activityResponse.Success = true;
                    return activityResponse;
                }
                else if (activityType.Activity == ActivityTypes.Fireteam)
                {
                    MongoDB.Models.Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(activityType.Id);
                    if (existingFireteam == null)
                    {
                        return activityResponse;
                    }

                    ActivityUser playerUser = existingFireteam.Players?.Find(x => x.UserId == user.Id);
                    if (playerUser != null)
                    {
                        existingFireteam.Players?.Remove(playerUser);
                    }
                    //Player got stuck in bad loop where said was active but wasn't.
                    else
                    {
                        activityResponse.Success = true;
                        return activityResponse;
                    }


                    existingFireteam = await _activityService.UpdateRaidAsync(existingFireteam);

                    var modifiedEmbed = new EmbedBuilder();
                    AddActivityUsersField(modifiedEmbed, "Players", existingFireteam.Players);
                    AddActivityUsersField(modifiedEmbed, "Subs", existingFireteam.Substitutes);

                    EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, embed);

                    await originalMessage.ModifyAsync(x =>
                    {
                        x.Embed = modifiedEmbed.Build();
                    });

                    if (existingFireteam.Players.Count == existingFireteam.MaxPlayerCount - 1)
                    {
                        try
                        {
                            IRole role = user.Guild.Roles.FirstOrDefault(x => x.Name == "Raid Fanatics");
                            if (role != null)
                            {
                                await originalMessage.ReplyAsync(role.Mention + " A slot has now opened up!");
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }

                    await originalMessage.ReplyAsync(null, false, EmbedHelper.CreateInfoReply($"<@{user.Id}> has left Fireteam Id {existingFireteam.Id}").Build());

                    activityResponse.Success = true;
                    return activityResponse;
                }

                return activityResponse;
            }
            return activityResponse;
        }

        private async Task<ActivityResponse> TestRemoveSubFromActivityV2(Cacheable<IUserMessage, ulong> message, SocketGuildUser user)
        {
            ActivityResponse activityResponse = new ActivityResponse(false, false);
            var originalMessage = await message.GetOrDownloadAsync();
            var embeds = originalMessage.Embeds.ToList();
            var embed = embeds?.First();
            if (embed != null)
            {
                #region Check Closed
                if (embed.Title == RaidIsClosed || embed.Title == ActivityIsClosed)
                {
                    return activityResponse;
                }
                #endregion


                ActivityType activityType = new ActivityType();
                activityType.Parse(embed);
                if (activityType.Activity == ActivityTypes.Unknown)
                    return activityResponse;

                if (activityType.Activity == ActivityTypes.Raid)
                {
                    MongoDB.Models.Raid existingRaid = await _activityService.GetMongoRaidAsync(activityType.Id);
                    if (existingRaid == null)
                    {
                        return activityResponse;
                    }

                    //Player is a Sub
                    ActivityUser subUser = existingRaid.Substitutes?.Find(x => x.UserId == user.Id);
                    if (subUser != null)
                    {
                        existingRaid.Substitutes?.Remove(subUser);
                    }

                    existingRaid = await _activityService.UpdateRaidAsync(existingRaid);

                    var modifiedEmbed = new EmbedBuilder();
                    AddActivityUsersField(modifiedEmbed, "Players", existingRaid.Players);
                    AddActivityUsersField(modifiedEmbed, "Subs", existingRaid.Substitutes);

                    EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, embed);

                    await originalMessage.ModifyAsync(x =>
                    {
                        x.Embed = modifiedEmbed.Build();
                    });

                    activityResponse.Success = true;
                    return activityResponse;
                }
                else if (activityType.Activity == ActivityTypes.Fireteam)
                {
                    MongoDB.Models.Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(activityType.Id);
                    if (existingFireteam == null)
                    {
                        return activityResponse;
                    }

                    ActivityUser subUser = existingFireteam.Substitutes?.Find(x => x.UserId == user.Id);
                    if (subUser != null)
                    {
                        existingFireteam.Substitutes?.Remove(subUser);
                    }

                    existingFireteam = await _activityService.UpdateRaidAsync(existingFireteam);

                    var modifiedEmbed = new EmbedBuilder();
                    AddActivityUsersField(modifiedEmbed, "Players", existingFireteam.Players);
                    AddActivityUsersField(modifiedEmbed, "Subs", existingFireteam.Substitutes);

                    EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, embed);

                    await originalMessage.ModifyAsync(x =>
                    {
                        x.Embed = modifiedEmbed.Build();
                    });

                    activityResponse.Success = true;
                    return activityResponse;
                }

                return activityResponse;
            }
            return activityResponse;
        }


        private async Task<ActivityResponse> TestAddSubstituteToActivityV2(Cacheable<IUserMessage, ulong> message, SocketGuildUser user)
        {
            ActivityResponse activityResponse = new ActivityResponse(false, false);
            var originalMessage = await message.GetOrDownloadAsync();
            var embeds = originalMessage.Embeds.ToList();
            var embed = embeds?.First();
            if (embed != null)
            {
                #region Check Closed
                if (embed.Title == RaidIsClosed || embed.Title == ActivityIsClosed)
                {
                    return activityResponse;
                }
                #endregion


                ActivityType activityType = new ActivityType();
                activityType.Parse(embed);
                if (activityType.Activity == ActivityTypes.Unknown)
                    return activityResponse;

                if (activityType.Activity == ActivityTypes.Raid)
                {
                    MongoDB.Models.Raid existingRaid = await _activityService.GetMongoRaidAsync(activityType.Id);
                    if (existingRaid == null)
                    {
                        return activityResponse;
                    }

                    //User is already a Player
                    ActivityUser playerUser = existingRaid.Players?.Find(x => x.UserId == user.Id);
                    if (playerUser != null)
                    {
                        existingRaid.Players?.Remove(playerUser);
                        activityResponse.PreviousReaction = true;
                    }
                    //Already Joined
                    else if (existingRaid.Substitutes.FirstOrDefault(x => x.UserId == user.Id) != null)
                    {
                        activityResponse.Success = true;
                        return activityResponse;
                    }

                    //Now Add them to the Substitues list
                    existingRaid.Substitutes.Add(new ActivityUser(user.Id, user.Username));
                    existingRaid = await _activityService.UpdateRaidAsync(existingRaid);

                    var modifiedEmbed = new EmbedBuilder();
                    AddActivityUsersField(modifiedEmbed, "Players", existingRaid.Players);
                    AddActivityUsersField(modifiedEmbed, "Substitutes", existingRaid.Substitutes);

                    EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, embed);

                    await originalMessage.ModifyAsync(x =>
                    {
                        x.Embed = modifiedEmbed.Build();
                    });

                    activityResponse.Success = true;
                    return activityResponse;
                }
                else if (activityType.Activity == ActivityTypes.Fireteam)
                {
                    MongoDB.Models.Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(activityType.Id);
                    if (existingFireteam == null)
                    {
                        return activityResponse;
                    }

                    //User is already a Player
                    ActivityUser playerUser = existingFireteam.Players?.Find(x => x.UserId == user.Id);
                    if (playerUser != null)
                    {
                        existingFireteam.Players?.Remove(playerUser);
                        activityResponse.PreviousReaction = true;
                    }
                    //Already Joined
                    else if (existingFireteam.Substitutes.FirstOrDefault(x => x.UserId == user.Id) != null)
                    {
                        activityResponse.Success = true;
                        return activityResponse;
                    }

                    //Now Add them to the Substitues list
                    existingFireteam.Substitutes.Add(new ActivityUser(user.Id, user.Username));
                    existingFireteam = await _activityService.UpdateRaidAsync(existingFireteam);

                    var modifiedEmbed = new EmbedBuilder();
                    AddActivityUsersField(modifiedEmbed, "Players", existingFireteam.Players);
                    AddActivityUsersField(modifiedEmbed, "Substitutes", existingFireteam.Substitutes);

                    EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, embed);

                    await originalMessage.ModifyAsync(x =>
                    {
                        x.Embed = modifiedEmbed.Build();
                    });

                    activityResponse.Success = true;
                    return activityResponse;
                }

                return activityResponse;
            }
            return activityResponse;
        }

        private EmbedBuilder AddActivityUsersField(EmbedBuilder embed, string title, List<ActivityUser> activityUsers)
        {
            if(activityUsers == null || activityUsers.Count == 0)
            {
                embed.AddField(title, "-");
                return embed;
            }

            StringBuilder sb = new StringBuilder();
            foreach(ActivityUser user in activityUsers)
            {
                sb.AppendLine(user.DisplayName);
            }

            embed.AddField(title, sb.ToString().Trim());
            return embed;
        }
        
        private async Task<ActivityResponse> AddPlayerToActivityV2(Cacheable<IUserMessage, ulong> message, SocketGuildUser user)
        {
            ActivityResponse activityResponse = new ActivityResponse(false, false);
            var originalMessage = await message.GetOrDownloadAsync();
            var embeds = originalMessage.Embeds.ToList();
            var embed = embeds?.First();
            bool atMaxPlayersNow = false;
            if (embed != null)
            {
                if(embed.Title == RaidIsClosed || embed.Title == ActivityIsClosed)
                {
                    return activityResponse;
                }

                var modifiedEmbed = new EmbedBuilder();
                var playerCountField = embed.Fields.FirstOrDefault(x => x.Name == "Players");
                var footer = embed?.Footer.Value.Text;
                string[] split = footer.Split("\n");
                int playerCount = 0;
                int maxPlayerCount = 0;
                if (string.IsNullOrEmpty(footer))
                {
                    playerCount = playerCountField.Value.Count(x => x == '@');
                    if (playerCount >= 6)
                    {
                        activityResponse.FullMessage = split[0] + " is now full. Feel free to Sub or watch if a slot becomes available!";
                        activityResponse.IsFull = true;
                        return activityResponse;
                    }
                    if (playerCount + 1 == 6) atMaxPlayersNow = true;
                }
                else
                {
                    playerCount = playerCountField.Value.Count(x => x == '@');
                    string test = split[split.Length - 1];
                    test = test.Replace(AMaxOf, string.Empty);
                    if (int.TryParse(test[0].ToString(), out maxPlayerCount))
                    {
                        if (playerCount >= maxPlayerCount)
                        {
                            activityResponse.FullMessage = split[0] + " is now full. Feel free to Sub or watch if a slot becomes available!";
                            activityResponse.IsFull = true;
                            return activityResponse;
                        }

                        if (playerCount + 1 == maxPlayerCount) atMaxPlayersNow = true;
                    }
                    else
                    {
                        if (playerCount >= 6)
                        {
                            activityResponse.FullMessage = split[0] + " is now full. Feel free to Sub or watch if a slot becomes available!";
                            activityResponse.IsFull = true;
                            return activityResponse;
                        }

                        if (playerCount + 1 == 6) atMaxPlayersNow = true;
                    }
                }
                var field = embed.Fields.Where(x => x.Name == "Subs").FirstOrDefault();
                //Player is a Sub
                if (field.Value.Contains(user.Id.ToString()))
                {
                    activityResponse.PreviousReaction = true;
                    modifiedEmbed = new EmbedBuilder();
                    foreach (EmbedField embedField in embed.Fields)
                    {
                        if (embedField.Name == "Subs")
                        {
                            modifiedEmbed.AddField("Subs", RemovePlayerNameFromEmbedText(embedField, user.Id.ToString()));
                        }
                        else if (embedField.Name == "Players")
                        {
                            modifiedEmbed.AddField(embedField.Name, AddPlayerNameToEmbedText(embedField, user.Id.ToString()));
                        }
                        else
                            modifiedEmbed.AddField(embedField.Name, embedField.Value);
                    }
                }
                //Add the Player
                else
                {
                    var playerField = embed.Fields.Where(x => x.Name == "Players").FirstOrDefault();
                    //Player is a Player - This is an edge case that happens when the raid is created
                    if (playerField.Value.Contains(user.Id.ToString()))
                    {
                        activityResponse.Success = true;
                        return activityResponse;
                    }
                    modifiedEmbed = new EmbedBuilder();
                    foreach (EmbedField embedField in embed.Fields)
                    {
                        if (embedField.Name == "Players")
                        {
                            modifiedEmbed.AddField(embedField.Name, AddPlayerNameToEmbedText(embedField, user.Id.ToString()));
                        }
                        else
                            modifiedEmbed.AddField(embedField.Name, embedField.Value);
                    }
                }

                EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, embed);

                string idText = split[0];
                string fullText = string.Empty;
                Tuple<bool, int> result = GetRaidAndIdFromText(idText);
                if(result != null)
                {
                    //Raid
                    if(result.Item1)
                    {
                        await AddToRaidGroup(result.Item2, user.Id);
                        fullText = $"your raid Id {result.Item2} is now full!";
                    }
                    else
                    {
                        await AddToFireteamGroup(result.Item2, user.Id);
                        fullText = $"your fireteam Id {result.Item2} is now full!";
                    }
                }

                await originalMessage.ModifyAsync(x =>
                {
                    x.Embed = modifiedEmbed.Build();
                });

                if (atMaxPlayersNow)
                {
                    try
                    {
                        await originalMessage.ReplyAsync($"@{embed.Author.Value}, {fullText}");
                    }
                    catch (Exception ex)
                    {

                    }
                }

                activityResponse.Success = true;
                return activityResponse;
            }

            return activityResponse;
        }

        private async Task<ActivityResponse> RemovePlayerFromActivityV2(Cacheable<IUserMessage, ulong> message, ulong userId)
        {
            ActivityResponse activityResponse = new ActivityResponse(false, false);
            var originalMessage = await message.GetOrDownloadAsync();
            var embeds = originalMessage.Embeds.ToList();
            var embed = embeds?.First();
            if (embed != null)
            {
                if (embed.Title == RaidIsClosed || embed.Title == ActivityIsClosed)
                {
                    return activityResponse;
                }

                var modifiedEmbed = new EmbedBuilder();
                var playerField = embed.Fields.Where(x => x.Name == "Players").FirstOrDefault();
                //Player is a Player - This is an edge case that happens when the raid is created
                if (playerField.Value.Contains(userId.ToString()))
                {
                    modifiedEmbed = new EmbedBuilder();
                    foreach (EmbedField embedField in embed.Fields)
                    {
                        if (embedField.Name == "Players")
                        {
                            modifiedEmbed.AddField(embedField.Name, RemovePlayerNameFromEmbedText(embedField, userId.ToString()));
                        }
                        else
                            modifiedEmbed.AddField(embedField.Name, embedField.Value);
                    }

                    EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, embed);

                    await originalMessage.ModifyAsync(x =>
                    {
                        x.Embed = modifiedEmbed.Build();
                    });

                    try
                    {
                        var footer = embed?.Footer.Value.Text;
                        string[] split = footer?.Split("\n");
                        if (split != null)
                        {
                            string idText = split[0];
                            Tuple<bool, int> result = GetRaidAndIdFromText(idText);
                            if (result != null)
                            {
                                //Raid
                                if (result.Item1)
                                {
                                    await RemoveFromRaidGroup(result.Item2, userId);
                                }
                                else
                                {
                                    await RemoveFromFireteamGroup(result.Item2, userId);
                                }
                            }


                            await originalMessage.ReplyAsync(null, false, EmbedHelper.CreateInfoReply($"<@{userId}> has left {split[0]}").Build());
                        }
                    }
                    catch(Exception ex)
                    {

                    }

                    activityResponse.Success = true;
                    return activityResponse;
                }
            }
            return activityResponse;
        }

        private async Task<ActivityResponse> RemoveSubFromActivityV2(Cacheable<IUserMessage, ulong> message, ulong userId)
        {
            ActivityResponse activityResponse = new ActivityResponse(false, false);
            var originalMessage = await message.GetOrDownloadAsync();
            var embeds = originalMessage.Embeds.ToList();
            var embed = embeds?.First();
            if (embed != null)
            {
                if (embed.Title == RaidIsClosed || embed.Title == ActivityIsClosed)
                {
                    return activityResponse;
                }

                var subField = embed.Fields.Where(x => x.Name == "Subs").FirstOrDefault();
                //Player is a Player - This is an edge case that happens when the raid is created
                if (subField.Value.Contains(userId.ToString()))
                {
                    var modifiedEmbed = new EmbedBuilder();
                    modifiedEmbed.Title = embed.Title;
                    modifiedEmbed.Description = embed.Description;
                    modifiedEmbed.Color = embed.Color;
                    foreach (EmbedField embedField in embed.Fields)
                    {
                        if (embedField.Name == "Subs")
                        {
                            modifiedEmbed.AddField(embedField.Name, RemovePlayerNameFromEmbedText(embedField, userId.ToString()));
                        }
                        else
                            modifiedEmbed.AddField(embedField.Name, embedField.Value);
                    }

                    EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, embed);

                    await originalMessage.ModifyAsync(x =>
                    {
                        x.Embed = modifiedEmbed.Build();
                    });

                    activityResponse.Success = true;
                    return activityResponse;
                }
            }
            return activityResponse;
        }

        private async Task<ActivityResponse> AddSubToActivityV2(Cacheable<IUserMessage, ulong> message, SocketGuildUser user)
        {
            ActivityResponse activityResponse = new ActivityResponse(false, false);
            var originalMessage = await message.GetOrDownloadAsync();
            var embeds = originalMessage.Embeds.ToList();
            var embed = embeds?.First();
            if (embed != null)
            {
                if (embed.Title == RaidIsClosed || embed.Title == ActivityIsClosed)
                {
                    return activityResponse;
                }

                var modifiedEmbed = new EmbedBuilder();
                var field = embed.Fields.Where(x => x.Name == "Players").FirstOrDefault();
                //Already a Player
                if (field.Value.Contains(user.Id.ToString()))
                {
                    activityResponse.PreviousReaction = true;
                    modifiedEmbed = new EmbedBuilder();
                    foreach (EmbedField embedField in embed.Fields)
                    {
                        if (embedField.Name == "Players")
                        {
                            modifiedEmbed.AddField("Players", RemovePlayerNameFromEmbedText(embedField, user.Id.ToString()));
                        }
                        else if (embedField.Name == "Subs")
                        {
                            modifiedEmbed.AddField(embedField.Name, AddPlayerNameToEmbedText(embedField, user.Id.ToString()));
                        }
                        else
                            modifiedEmbed.AddField(embedField.Name, embedField.Value);
                    }

                    string idText = GetFooterIdTextFromEmbed(embed);
                    Tuple<bool, int> result = GetRaidAndIdFromText(idText);
                    if (result != null)
                    {
                        //Raid
                        if (result.Item1)
                        {
                            await RemoveFromRaidGroup(result.Item2, user.Id);
                        }
                        else
                        {
                            await RemoveFromFireteamGroup(result.Item2, user.Id);
                        }
                    }

                }
                //Add the Player
                else
                {
                    modifiedEmbed = new EmbedBuilder();
                    foreach (EmbedField embedField in embed.Fields)
                    {
                        if (embedField.Name == "Subs")
                        {
                            modifiedEmbed.AddField(embedField.Name, AddPlayerNameToEmbedText(embedField, user.Id.ToString()));
                        }
                        else
                            modifiedEmbed.AddField(embedField.Name, embedField.Value);
                    }
                }

                EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, embed);

                await originalMessage.ModifyAsync(x =>
                {
                    x.Embed = modifiedEmbed.Build();
                });
                activityResponse.Success = true;
                return activityResponse;
            }

            return activityResponse;
        }

        private Tuple<bool, int> GetRaidAndIdFromText(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            if(text.Contains("Raid"))
            {
                int id = int.Parse(text.Split(" ")[2]);
                return new Tuple<bool, int>(true, id);
            }
            else if(text.Contains("Fireteam"))
            {
                int id = int.Parse(text.Split(" ")[2]);
                return new Tuple<bool, int>(false, id);
            }

            return null;
        }

        private string GetFooterIdTextFromEmbed(IEmbed embed)
        {
            if (embed == null) return string.Empty;
            if (embed.Footer == null) return string.Empty;
            if (embed.Footer.HasValue == false) return string.Empty;
            string[] split = embed.Footer.Value.Text.Split("\n");
            if(split.Length == 0) return string.Empty;
            if(split[0].Contains("Id") == false) return string.Empty;
            return split[0];
        }

        private async Task AddToRaidGroup(int raidId, ulong userId)
        {
            await _raidGroupService.AddPlayerToRaidGroup(raidId, userId);
        }

        private async Task AddToFireteamGroup(int fireteamId, ulong userId)
        {

        }

        private async Task RemoveFromRaidGroup(int raidId, ulong userId)
        {
            await _raidGroupService.RemovePlayerFromRaidGroup(raidId, userId);
        }

        private async Task RemoveFromFireteamGroup(int fireteamId, ulong userId)
        {

        }

        public string RemovePlayerNameFromEmbedText(EmbedField embedField, string userId)
        {
            string replaceValue = string.Empty;
            if (embedField.Value.Contains($"\n<@{userId}>"))
            {
                replaceValue = embedField.Value.Replace($"\n<@{userId}>", string.Empty);
            }
            else
            {
                replaceValue = embedField.Value.Replace($"<@{userId}>", string.Empty);
            }
            if (replaceValue.StartsWith("-")) replaceValue.Replace("-", string.Empty);
            if (replaceValue == string.Empty) replaceValue = "-";
            return replaceValue;
        }

        public string AddPlayerNameToEmbedText(EmbedField embedField, string userId)
        {
            string embedValue = embedField.Value;
            if (embedValue.StartsWith("-")) embedValue = embedValue.Replace("-", string.Empty);
            if (embedValue.Contains(userId.ToString()) == false)
            {
                embedValue += $"\n<@{userId}>";
            }
            return embedValue;
        }
    }

    public class ActivityType
    {
        public ActivityTypes Activity { get; set; }
        public int Id { get; set; }

        public void Parse(IEmbed embed)
        {
            if (embed == null) return;
            if (embed.Footer.HasValue == false) return;
            var footerText = embed.Footer.Value.Text;
            string[] split = footerText.Split("\n");
            if(split.Length == 0) return;
            string text = split[0];
            if (string.IsNullOrEmpty(text)) return;
            int.TryParse(text.Split(" ")[2], out int id);
            if (id <= 0) return;
            Id = id;
            if (text.Contains("Raid"))
            {
                Activity = ActivityTypes.Raid;
            }
            else if (text.Contains("Fireteam"))
            {
                Activity = ActivityTypes.Fireteam;
            }
        }
        
    }

    public enum ActivityTypes
    {
        Unknown = 0,
        Raid = 1,
        Fireteam = 2
    }
}
