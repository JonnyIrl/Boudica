using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Boudica.Classes;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Boudica.Commands
{
    public class CommandHandler
    {
        // setup fields to be set later in the constructor
        private readonly IConfiguration _config;
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;

        private Emoji _jEmoji = new Emoji("🇯");
        private Emoji _sEmoji = new Emoji("🇸");

        public CommandHandler(IServiceProvider services)
        {
            // juice up the fields with these services
            // since we passed the services in, we can use GetRequiredService to pass them into the fields set earlier
            _config = services.GetRequiredService<IConfiguration>();
            _commands = services.GetRequiredService<CommandService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            // take action when we execute a command
            _commands.CommandExecuted += CommandExecutedAsync;

            // take action when we receive a message (so we can process it, and see if it is a valid command)
            _client.MessageReceived += MessageReceivedAsync;

            //Listen for Reactions
            _client.ReactionAdded += ReactionAddedAsync;
            _client.ReactionRemoved += ReactionRemovedAsync;
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

            // get prefix from the configuration file
            char prefix = Char.Parse(_config["Prefix"]);

            // determine if the message has a valid prefix, and adjust argPos based on prefix
            if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasCharPrefix(prefix, ref argPos)))
            {
                return;
            }

            var context = new SocketCommandContext(_client, message);

            // execute command if one is found that matches
            await _commands.ExecuteAsync(context, argPos, _services);
        }

        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // if a command isn't found, log that info to console and exit this method
            if (!command.IsSpecified)
            {
                Console.WriteLine($"Command failed to execute for [] <-> []!");
                return;
            }


            // log success to the console and exit this method
            if (result.IsSuccess)
            {
                Console.WriteLine($"Command [] executed for -> []");
                return;
            }


            // failure scenario, let's let the user know
            await context.Channel.SendMessageAsync($"Sorry, ... something went wrong -> []!");
        }

        public async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            IUser user = await _client.GetUserAsync(reaction.UserId);
            if (user.IsBot)
            {
                return;
            }

            if (reaction.Emote.Name == "🇯")
            {
                ActivityResponse result = await AddPlayerToActivityV2(message, reaction.UserId);
                if (result.Success == false)
                {
                    var originalMessage = await message.GetOrDownloadAsync();
                    await originalMessage.RemoveReactionAsync(reaction.Emote, user);
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
                ActivityResponse result = await AddSubToActivityV2(message, reaction.UserId);
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

        private async Task<ActivityResponse> AddPlayerToActivityV2(Cacheable<IUserMessage, ulong> message, ulong userId)
        {
            ActivityResponse activityResponse = new ActivityResponse(false, false);
            var originalMessage = await message.GetOrDownloadAsync();
            var embeds = originalMessage.Embeds.ToList();
            var embed = embeds?.First();
            if (embed != null)
            {
                var modifiedEmbed = new EmbedBuilder();
                var playerCountField = embed.Fields.FirstOrDefault(x => x.Name == "Players");
                if(playerCountField.Value.Count(x => x == '@') >= 6)
                {
                    return activityResponse;
                }
                var field = embed.Fields.Where(x => x.Name == "Subs").FirstOrDefault();
                //Player is a Sub
                if (field.Value.Contains(userId.ToString()))
                {
                    activityResponse.PreviousReaction = true;
                    modifiedEmbed = new EmbedBuilder();
                    foreach (EmbedField embedField in embed.Fields)
                    {
                        if (embedField.Name == "Subs")
                        {
                            modifiedEmbed.AddField("Subs", RemovePlayerNameFromEmbedText(embedField, userId.ToString()));
                        }
                        else if (embedField.Name == "Players")
                        {
                            modifiedEmbed.AddField(embedField.Name, AddPlayerNameToEmbedText(embedField, userId.ToString()));
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
                    if (playerField.Value.Contains(userId.ToString()))
                    {
                        activityResponse.Success = true;
                        return activityResponse;
                    }
                    modifiedEmbed = new EmbedBuilder();
                    foreach (EmbedField embedField in embed.Fields)
                    {
                        if (embedField.Name == "Players")
                        {
                            modifiedEmbed.AddField(embedField.Name, AddPlayerNameToEmbedText(embedField, userId.ToString()));
                        }
                        else
                            modifiedEmbed.AddField(embedField.Name, embedField.Value);
                    }
                }

                UpdateAuthorOnEmbed(modifiedEmbed, embed);
                UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                UpdateFooterOnEmbed(modifiedEmbed, embed);

                await originalMessage.ModifyAsync(x =>
                {
                    x.Embed = modifiedEmbed.Build();
                });
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

                    UpdateAuthorOnEmbed(modifiedEmbed, embed);
                    UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                    UpdateFooterOnEmbed(modifiedEmbed, embed);

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

        private async Task<ActivityResponse> RemoveSubFromActivityV2(Cacheable<IUserMessage, ulong> message, ulong userId)
        {
            ActivityResponse activityResponse = new ActivityResponse(false, false);
            var originalMessage = await message.GetOrDownloadAsync();
            var embeds = originalMessage.Embeds.ToList();
            var embed = embeds?.First();
            if (embed != null)
            {
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

                    UpdateAuthorOnEmbed(modifiedEmbed, embed);
                    UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                    UpdateFooterOnEmbed(modifiedEmbed, embed);

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

        private async Task<ActivityResponse> AddSubToActivityV2(Cacheable<IUserMessage, ulong> message, ulong userId)
        {
            ActivityResponse activityResponse = new ActivityResponse(false, false);
            var originalMessage = await message.GetOrDownloadAsync();
            var embeds = originalMessage.Embeds.ToList();
            var embed = embeds?.First();
            if (embed != null)
            {
                var modifiedEmbed = new EmbedBuilder();
                var field = embed.Fields.Where(x => x.Name == "Players").FirstOrDefault();
                //Already a Player
                if (field.Value.Contains(userId.ToString()))
                {
                    activityResponse.PreviousReaction = true;
                    modifiedEmbed = new EmbedBuilder();
                    foreach (EmbedField embedField in embed.Fields)
                    {
                        if (embedField.Name == "Players")
                        {
                            modifiedEmbed.AddField("Players", RemovePlayerNameFromEmbedText(embedField, userId.ToString()));
                        }
                        else if (embedField.Name == "Subs")
                        {
                            modifiedEmbed.AddField(embedField.Name, AddPlayerNameToEmbedText(embedField, userId.ToString()));
                        }
                        else
                            modifiedEmbed.AddField(embedField.Name, embedField.Value);
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
                            modifiedEmbed.AddField(embedField.Name, AddPlayerNameToEmbedText(embedField, userId.ToString()));
                        }
                        else
                            modifiedEmbed.AddField(embedField.Name, embedField.Value);
                    }
                }

                UpdateAuthorOnEmbed(modifiedEmbed, embed);
                UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                UpdateFooterOnEmbed(modifiedEmbed, embed);

                await originalMessage.ModifyAsync(x =>
                {
                    x.Embed = modifiedEmbed.Build();
                });
                activityResponse.Success = true;
                return activityResponse;
            }

            return activityResponse;
        }

        public async Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            if (_client.GetUser(reaction.UserId).IsBot) return;

            if (reaction.Emote.Name == "🇯")
            {
                ActivityResponse result = await RemovePlayerFromActivityV2(message, reaction.UserId);
                return;
            }

            if (reaction.Emote.Name == "🇸")
            {
                ActivityResponse result = await RemoveSubFromActivityV2(message, reaction.UserId);
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
            //        newEmbed.Description = embed.Description.Replace("\n\nI see you reacted!", string.Empty);
            //        await originalMessage.ModifyAsync(x =>
            //        {
            //            x.Embed = newEmbed.Build();
            //        });
            //    }
        }

        public void UpdateAuthorOnEmbed(EmbedBuilder modifiedEmbed, IEmbed? embed)
        {
            if (embed == null) return;
            EmbedAuthor? author = embed.Author;
            if (author != null)
            {
                EmbedAuthor realAuthor = (EmbedAuthor) embed.Author;
                modifiedEmbed.Author = new EmbedAuthorBuilder()
                {
                    Name = realAuthor.Name,
                    IconUrl = realAuthor.IconUrl,
                    Url = realAuthor.Url
                };
            }
        }

        public void UpdateDescriptionTitleColorOnEmbed(EmbedBuilder modifiedEmbed, IEmbed? embed)
        {
            if (embed == null) return;
            modifiedEmbed.Description = embed.Description;
            modifiedEmbed.Title = embed.Title;
            modifiedEmbed.Color = embed.Color;
        }

        public void UpdateFooterOnEmbed(EmbedBuilder modifiedEmbed, IEmbed? embed)
        {
            if(embed == null) return;
            if (embed.Footer != null)
            {
                modifiedEmbed.Footer = new EmbedFooterBuilder()
                {
                    Text = embed.Footer.Value.ToString()
                };
            }
        }

        public string RemovePlayerNameFromEmbedText(EmbedField embedField, string userId)
        {
            string replaceValue = embedField.Value.Replace($"<@{userId}>", string.Empty);
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
}
