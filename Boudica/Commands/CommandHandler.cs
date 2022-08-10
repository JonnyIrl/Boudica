using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
            if (_client.GetUser(reaction.UserId).IsBot) return;
            if (reaction.Emote.Name == "🇯")
            {
                bool result = await AddPlayerToActivity(message, reaction.UserId);
                if (result == false)
                {
                    var originalMessage = await message.GetOrDownloadAsync();
                    IUser user = await _client.GetUserAsync(reaction.UserId);
                    await originalMessage.RemoveReactionAsync(reaction.Emote, user);
                }
                return;
            }

            if (reaction.Emote.Name == "🇸")
            {
                bool result = await AddSubToActivity(message, reaction.UserId);
                if(result == false)
                {
                    var originalMessage = await message.GetOrDownloadAsync();
                    IUser user = await _client.GetUserAsync(reaction.UserId);
                    await originalMessage.RemoveReactionAsync(reaction.Emote, user);
                }

                return;
            }

            if (reaction.Emote.Name == "👍")
            {
                var originalMessage = await message.GetOrDownloadAsync();
                var embeds = originalMessage.Embeds.ToList();
                var embed = embeds?.First();
                if (embed != null)
                {
                    EmbedBuilder newEmbed = new EmbedBuilder();
                    newEmbed.Color = embed.Color;
                    newEmbed.Title = embed.Title;
                    newEmbed.Description = embed.Description + "\n\nI see you reacted!";
                    await originalMessage.ModifyAsync(x =>
                    {
                        x.Embed = newEmbed.Build();
                    });
                }
            }
        }

        private async Task<bool> AddPlayerToActivity(Cacheable<IUserMessage, ulong> message, ulong userId)
        {
            var originalMessage = await message.GetOrDownloadAsync();
            var embeds = originalMessage.Embeds.ToList();
            var embed = embeds?.First();
            if (embed != null)
            {
                //Check the user is already part of the 
                if (embed.Description.Contains(userId.ToString()))
                {
                    int subIndex = embed.Description.IndexOf("Subs");
                    int userIdIndex = embed.Description.IndexOf(userId.ToString());
                    //if group has no subs
                    if (subIndex == -1)
                        return false;
                    //if user is already part of it.
                    if (userIdIndex < subIndex)
                        return false;

                    //Player is currently a sub
                    if(userIdIndex > subIndex)
                    {
                        var modifiedEmbed = new EmbedBuilder();
                        modifiedEmbed.Description = embed.Description.Replace($"<@{userId}>", string.Empty);
                        modifiedEmbed.Title = embed.Title;
                        modifiedEmbed.Color = embed.Color;
                        if (embed.Footer != null)
                        {
                            modifiedEmbed.Footer = new EmbedFooterBuilder()
                            {
                                Text = embed.Footer.Value.ToString()
                            };
                        }
                        
                        await originalMessage.ModifyAsync(x =>
                        {
                            x.Embed = modifiedEmbed.Build();
                        });
                        return true;
                    }

                }

                var newEmbed = new EmbedBuilder();
                if (embed.Description.Contains("Subs"))
                {
                    int indexToReplace = FindIndexToInsertPlayerId(embed.Description);
                    string newDescription = InsertNewPlayerName(embed.Description, indexToReplace, userId.ToString());
                    newEmbed.Description = newDescription;
                    newEmbed.Title = embed.Title;
                    newEmbed.Color = embed.Color;
                    if (embed.Footer != null)
                    {
                        newEmbed.Footer = new EmbedFooterBuilder()
                        {
                            Text = embed.Footer.Value.ToString()
                        };
                    }
                }


                await originalMessage.ModifyAsync(x =>
                {
                    x.Embed = newEmbed.Build();
                });
                return true;
            }

            return true;
        }

        private async Task<bool> AddSubToActivity(Cacheable<IUserMessage, ulong> message, ulong userId)
        {
            var originalMessage = await message.GetOrDownloadAsync();
            var embeds = originalMessage.Embeds.ToList();
            var embed = embeds?.First();
            if (embed != null)
            {
                var newEmbed = new EmbedBuilder();
                if (embed.Description.Contains("Subs"))
                {
                    if (embed.Description.Contains(userId.ToString()))
                    {
                        int playerIndex = embed.Description.IndexOf($"<@{userId.ToString()}>");
                        int subsIndex = embed.Description.IndexOf("Subs");
                        
                        //Player is a main player
                        if(playerIndex < subsIndex)
                        {
                            return false;
                        }
                    }

                    int indexToReplace = FindIndexToInsertSubPlayerId(embed.Description);
                    string newDescription = InsertNewPlayerName(embed.Description, indexToReplace, userId.ToString());
                    newEmbed.Description = newDescription;
                    newEmbed.Title = embed.Title;
                    newEmbed.Color = embed.Color;
                    if (embed.Footer != null)
                    {
                        newEmbed.Footer = new EmbedFooterBuilder()
                        {
                            Text = embed.Footer.Value.ToString()
                        };
                    }
                }
                else
                {
                    return false;
                }


                await originalMessage.ModifyAsync(x =>
                {
                    x.Embed = newEmbed.Build();
                });

                return true;
            }

            return false;
        }

        public int FindIndexToInsertPlayerId(string embedDescription)
        {
            if (string.IsNullOrEmpty(embedDescription)) return -1;
            string[] split = embedDescription.Split("Subs");
            int lastFoundIndex = -1;
            foreach(string text in split)
            {
                if (text.Contains("Subs")) break;
                if (text.Contains(">"))
                    lastFoundIndex = text.LastIndexOf(">");
            }
            if (lastFoundIndex == -1) return -1;
            // + 1 to return the index just after the >
            return (lastFoundIndex + 1);
        }

        public int FindIndexToInsertSubPlayerId(string embedDescription)
        {
            if (string.IsNullOrEmpty(embedDescription)) return -1;
            int index = embedDescription.LastIndexOf("Subs");
            if (index == -1) return -1;
            //Bring us to the end of "Subs"
            index += 4;
            int lastSubIndex = embedDescription.LastIndexOf(">");

            //Then we have no Subs
            if (lastSubIndex < index)
            {
                return index;
            }

            return (lastSubIndex + 1);
        }


        public string InsertNewPlayerName(string embedDescription, int subStringIndex, string userId)
        {
            if(string.IsNullOrEmpty(embedDescription)) return string.Empty;
            if(subStringIndex <= 0) return string.Empty;

            string subString = embedDescription.Substring(0, subStringIndex);
            embedDescription = embedDescription.Replace(subString, subString + $"\n<@{userId}>");
            return embedDescription;
        }


        public async Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            if (_client.GetUser(reaction.UserId).IsBot) return;
            if (reaction.Emote.Name == "👍")
            {
                var originalMessage = await message.GetOrDownloadAsync();
                var embeds = originalMessage.Embeds.ToList();
                var embed = embeds?.First();
                if (embed != null)
                {
                    EmbedBuilder newEmbed = new EmbedBuilder();
                    newEmbed.Color = embed.Color;
                    newEmbed.Title = embed.Title;
                    newEmbed.Description = embed.Description.Replace("\n\nI see you reacted!", string.Empty);
                    await originalMessage.ModifyAsync(x =>
                    {
                        x.Embed = newEmbed.Build();
                    });
                }
            }
        }
    }
}
