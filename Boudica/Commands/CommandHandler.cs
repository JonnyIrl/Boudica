using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Boudica.Classes;
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
        // setup fields to be set later in the constructor
        private readonly IConfiguration _config;
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        private readonly ActivityService _activityService;
        private readonly GuardianService _guardianService;
        private char Prefix = ';';

        private static List<ulong> _manualRemovedReactionList = new List<ulong>();
        private object _lock = new object();
        //private char SlashPrefix = '/';

        private Emoji _jEmoji = new Emoji("🇯");
        private Emoji _sEmoji = new Emoji("🇸");
        private Emote _glimmerEmote = null;
#if DEBUG
        private const ulong glimmerId = 1009200271475347567;
#else
        private const ulong glimmerId = 728197708074188802;
#endif
        public CommandHandler(IServiceProvider services)
        {
            // juice up the fields with these services
            // since we passed the services in, we can use GetRequiredService to pass them into the fields set earlier
            _config = services.GetRequiredService<IConfiguration>();
            _commands = services.GetRequiredService<CommandService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _activityService = services.GetRequiredService<ActivityService>();
            _guardianService = services.GetRequiredService<GuardianService>();
            _services = services;
            Emote.TryParse($"<:misc_glimmer:{glimmerId}>", out _glimmerEmote);

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


        public async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
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
                    lock (_lock)
                    {
                        _manualRemovedReactionList.Add(user.Id);
                    }
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

            else if (reaction.Emote.Name == "🇸")
            {
                var user = await reaction.Channel.GetUserAsync(reaction.UserId) as SocketGuildUser;
                if (user == null || user.IsBot)
                {
                    return;
                }
                ActivityResponse result = await AddSubstituteToActivityV2(message, user);
                if (result.Success == false)
                {
                    var originalMessage = await message.GetOrDownloadAsync();
                    lock (_lock)
                    {
                        _manualRemovedReactionList.Add(user.Id);
                    }
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

            else if (reaction.Emote.Name == "misc_glimmer")
            {
                var originalMessage = await message.GetOrDownloadAsync();
                if (originalMessage != null)
                {
                    ulong authorId = originalMessage.Author.Id;
                    //Stop person adding increasing their own
                    if(authorId != reaction.UserId)
                        await _guardianService.IncreaseGlimmerAsync(authorId, 1);
                }
            }
        }
        public async Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            if (reaction.Emote.Name == "🇯")
            {
                lock (_lock)
                {
                    if (_manualRemovedReactionList.Contains(reaction.UserId))
                    {
                        _manualRemovedReactionList.RemoveAll(x => x == reaction.UserId);
                        return;
                    }
                }

                var user = await reaction.Channel.GetUserAsync(reaction.UserId) as SocketGuildUser;
                if (user == null || user.IsBot)
                {
                    return;
                }
                ActivityResponse result = await RemovePlayerFromActivityV2(message, user);
                return;
            }

            else if (reaction.Emote.Name == "🇸")
            {
                lock (_lock)
                {
                    if (_manualRemovedReactionList.Contains(reaction.UserId))
                    {
                        _manualRemovedReactionList.RemoveAll(x => x == reaction.UserId);
                        return;
                    }
                }
                var user = await reaction.Channel.GetUserAsync(reaction.UserId) as SocketGuildUser;
                if (user == null || user.IsBot)
                {
                    return;
                }
                ActivityResponse result = await RemoveSubFromActivityV2(message, user);
                return;
            }

            else if (reaction.Emote.Name == "misc_glimmer")
            {
                var originalMessage = await message.GetOrDownloadAsync();
                if (originalMessage != null)
                {
                    ulong authorId = originalMessage.Author.Id;
                    //Stop person decreasing their own
                    if (authorId != reaction.UserId)
                        await _guardianService.RemoveGlimmerAsync(authorId, 1);
                }
            }

        }
        private async Task<ActivityResponse> AddPlayerToActivityV2(Cacheable<IUserMessage, ulong> message, SocketGuildUser user)
        {
            ActivityResponse activityResponse = new ActivityResponse(false, false);
            var originalMessage = await message.GetOrDownloadAsync();
            var embeds = originalMessage.Embeds.ToList();
            if(embeds.Any() == false)
            {
                activityResponse.Success = true;
                return activityResponse;
            }
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
                    Raid existingRaid = await _activityService.GetMongoRaidAsync(activityType.Id);
                    if (existingRaid == null)
                    {
                        return activityResponse;
                    }

                    //Already joined
                    ActivityUser existingPlayer = existingRaid.Players.FirstOrDefault(x => x.UserId == user.Id);
                    if (existingPlayer != null)
                    {
                        //If the player hasn't already reacted then add them.
                        if (existingPlayer.Reacted == false)
                        {
                            existingPlayer.Reacted = true;
                            //Need to update the message to include the person has reacted.
                            await UpdateRaidMessage(originalMessage, embed, existingRaid);
                        }
                        activityResponse.Success = true;
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
                        activityResponse.PreviousReaction = true;
                    }

                    //Now Add them to the Players list
                    existingRaid.Players.Add(new ActivityUser(user.Id, user.Username, true));
                    existingRaid = await _activityService.UpdateRaidAsync(existingRaid);
                    if (existingRaid.Players.Count == existingRaid.MaxPlayerCount)
                    {
                        activityResponse.IsFull = true;
                        activityResponse.FullMessage = $"Raid Id {existingRaid.Id} is now full. Feel free to Sub or watch if a slot becomes available!";
                    }

                    await UpdateRaidMessage(originalMessage, embed, existingRaid);

                    activityResponse.Success = true;
                    return activityResponse;
                }
                else if (activityType.Activity == ActivityTypes.Fireteam)
                {
                    Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(activityType.Id);
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
                    ActivityUser existingPlayer = existingFireteam.Players.FirstOrDefault(x => x.UserId == user.Id);
                    if (existingPlayer != null)
                    {
                        //If the player hasn't already reacted then add them.
                        if (existingPlayer.Reacted == false)
                        {
                            existingPlayer.Reacted = true;
                            //Need to update the message to include the person has reacted.
                            await UpdateFireteamMessage(originalMessage, embed, existingFireteam);
                        }
                        activityResponse.Success = true;
                        return activityResponse;
                    }

                    //Now Add them to the Players list
                    existingFireteam.Players.Add(new ActivityUser(user.Id, user.Username, true));
                    existingFireteam = await _activityService.UpdateFireteamAsync(existingFireteam);
                    if (existingFireteam.Players.Count == existingFireteam.MaxPlayerCount)
                    {
                        activityResponse.IsFull = true;
                        activityResponse.FullMessage = $"Fireteam Id {existingFireteam.Id} is now full. Feel free to Sub or watch if a slot becomes available!";
                    }

                    await UpdateFireteamMessage(originalMessage, embed, existingFireteam);

                    activityResponse.Success = true;
                    return activityResponse;
                }

                return activityResponse;
            }
            return activityResponse;
        }

        private async Task UpdateRaidMessage(IUserMessage originalMessage, IEmbed? embed, Raid existingRaid)
        {
            var modifiedEmbed = new EmbedBuilder();
            AddActivityUsersField(modifiedEmbed, "Players", existingRaid.Players);
            AddActivityUsersField(modifiedEmbed, "Subs", existingRaid.Substitutes);

            EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingRaid);

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
        }

        private async Task UpdateFireteamMessage(IUserMessage originalMessage, IEmbed? embed, Fireteam existingFireteam)
        {
            var modifiedEmbed = new EmbedBuilder();
            AddActivityUsersField(modifiedEmbed, "Players", existingFireteam.Players);
            AddActivityUsersField(modifiedEmbed, "Subs", existingFireteam.Substitutes);

            EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingFireteam);

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
        }

        private async Task<ActivityResponse> RemovePlayerFromActivityV2(Cacheable<IUserMessage, ulong> message, SocketGuildUser user)
        {
            ActivityResponse activityResponse = new ActivityResponse(false, false);
            var originalMessage = await message.GetOrDownloadAsync();
            var embeds = originalMessage.Embeds.ToList();
            if (embeds.Any() == false)
            {
                activityResponse.Success = true;
                return activityResponse;
            }
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
                    else
                    {
                        activityResponse.Success = true;
                        return activityResponse;
                    }

                    existingRaid = await _activityService.UpdateRaidAsync(existingRaid);

                    var modifiedEmbed = new EmbedBuilder();
                    AddActivityUsersField(modifiedEmbed, "Players", existingRaid.Players);
                    AddActivityUsersField(modifiedEmbed, "Subs", existingRaid.Substitutes);

                    EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingRaid);

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


                    existingFireteam = await _activityService.UpdateFireteamAsync(existingFireteam);

                    var modifiedEmbed = new EmbedBuilder();
                    AddActivityUsersField(modifiedEmbed, "Players", existingFireteam.Players);
                    AddActivityUsersField(modifiedEmbed, "Subs", existingFireteam.Substitutes);

                    EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingFireteam);

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
        private async Task<ActivityResponse> RemoveSubFromActivityV2(Cacheable<IUserMessage, ulong> message, SocketGuildUser user)
        {
            ActivityResponse activityResponse = new ActivityResponse(false, false);
            var originalMessage = await message.GetOrDownloadAsync();
            var embeds = originalMessage.Embeds.ToList();
            if (embeds.Any() == false)
            {
                activityResponse.Success = true;
                return activityResponse;
            }
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
                    else
                    {
                        activityResponse.Success = true;
                        return activityResponse;
                    }

                    existingRaid = await _activityService.UpdateRaidAsync(existingRaid);

                    var modifiedEmbed = new EmbedBuilder();
                    AddActivityUsersField(modifiedEmbed, "Players", existingRaid.Players);
                    AddActivityUsersField(modifiedEmbed, "Subs", existingRaid.Substitutes);

                    EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingRaid);

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
                    else
                    {
                        activityResponse.Success = true;
                        return activityResponse;
                    }

                    existingFireteam = await _activityService.UpdateFireteamAsync(existingFireteam);

                    var modifiedEmbed = new EmbedBuilder();
                    AddActivityUsersField(modifiedEmbed, "Players", existingFireteam.Players);
                    AddActivityUsersField(modifiedEmbed, "Subs", existingFireteam.Substitutes);

                    EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingFireteam);

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
        private async Task<ActivityResponse> AddSubstituteToActivityV2(Cacheable<IUserMessage, ulong> message, SocketGuildUser user)
        {
            ActivityResponse activityResponse = new ActivityResponse(false, false);
            var originalMessage = await message.GetOrDownloadAsync();
            var embeds = originalMessage.Embeds.ToList();
            if (embeds.Any() == false)
            {
                activityResponse.Success = true;
                return activityResponse;
            }
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
                    AddActivityUsersField(modifiedEmbed, "Subs", existingRaid.Substitutes);

                    EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingRaid);

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
                    existingFireteam = await _activityService.UpdateFireteamAsync(existingFireteam);

                    var modifiedEmbed = new EmbedBuilder();
                    AddActivityUsersField(modifiedEmbed, "Players", existingFireteam.Players);
                    AddActivityUsersField(modifiedEmbed, "Subs", existingFireteam.Substitutes);

                    EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingFireteam);

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
            const string PlayersTitle = "Players";
            const string SubstitutesTitle = "Subs";
            if(activityUsers == null || activityUsers.Count == 0)
            {
                embed.AddField(title, "-");
                return embed;
            }

            StringBuilder sb = new StringBuilder();
            foreach(ActivityUser user in activityUsers)
            {
                if(_glimmerEmote != null && title != SubstitutesTitle && user.Reacted)
                    sb.AppendLine($"{_glimmerEmote} {user.DisplayName}");
                else 
                    sb.AppendLine(user.DisplayName);
            }

            embed.AddField(title, sb.ToString().Trim());
            return embed;
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
