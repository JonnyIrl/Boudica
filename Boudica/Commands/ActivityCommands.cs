using Boudica.Classes;
using Boudica.Database.Models;
using Boudica.Helpers;
using Boudica.Services;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Commands
{
    public class ActivityCommands : ModuleBase
    {
        private readonly ActivityService _activityService;
        private readonly GuardianService _guardianService;
        private readonly GuardianReputationService _guardianReputationService;
        private readonly RaidGroupService _raidGroupService;

        private const int CreatorPoints = 5;
        public ActivityCommands(IServiceProvider services)
        {
            _activityService = services.GetRequiredService<ActivityService>();
            _guardianService = services.GetRequiredService<GuardianService>();
            _guardianReputationService = services.GetRequiredService<GuardianReputationService>();
            _raidGroupService = services.GetRequiredService<RaidGroupService>();
        }

        #region Raid
        [Command("create raid")]
        public async Task CreateRaidCommand([Remainder] string args)
        {
            if (args == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid and a description for your raid e.g. create raid Vow of Disciple Tuesday 28th 6pm").Build());
                return;
            }

            Raid newRaid = new Raid()
            {
                DateTimeCreated = DateTime.UtcNow,
                CreatedByUserId = Context.User.Id.ToString(),
                ChannelId = Context.Channel.Id.ToString()
            };
            newRaid = await _activityService.CreateRaidAsync(newRaid);
            if (newRaid.Id <= 0)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("I couldn't create the raid because Jonny did something wrong!").Build());
                return;
            }

            await _raidGroupService.AddPlayerToRaidGroup(newRaid.Id, Context.User.Id);


            var embed = new EmbedBuilder();

            string[] split = args.Split('\n');
            bool title = false;

            embed.WithColor(new Color(0, 255, 0));
            StringBuilder sb = new StringBuilder();
            int startingPostion = title ? 1 : 0;

            for (int i = startingPostion; i < split.Length; i++)
            {
                sb.AppendLine(split[i]);
            }

            embed.WithAuthor(Context.User);
            sb.AppendLine();
            sb.AppendLine();

            var user = Context.User;

            embed.Description = sb.ToString();

            embed.AddField("Players", $"<@{user.Id}>");
            embed.AddField("Subs", "-");

            embed.Footer = new EmbedFooterBuilder()
            {
                Text = $"Raid Id {newRaid.Id}\nUse J to Join | Use S to Sub.\nA max of 6 players may join a raid"
            };

            IUserMessage newMessage;
            IRole role = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Raid Fanatics");
            if(role != null)
            {
                // this will reply with the embed
                newMessage = await ReplyAsync(role.Mention, false, embed.Build());
            }
            else
            {
                // this will reply with the embed
                newMessage = await ReplyAsync(null, false, embed.Build());
            }

            
            newRaid.MessageId = newMessage.Id.ToString();
            await _activityService.UpdateRaidAsync(newRaid);

            await newMessage.PinAsync();
            await newMessage.AddReactionsAsync(new List<IEmote>()
            {
                new Emoji("🇯"),
                new Emoji("🇸"),
            });
        }

        [Command("edit raid")]
        public async Task EditRaid()
        {
            await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid id located in the footer of a raid e.g.\n\n;edit raid 16 This is a new description").Build());
        }

        [Command("edit raid")]
        public async Task EditRaid([Remainder] string args)
        {
            string result = await CheckEditRaidCommandIsValid(args);
            if (string.IsNullOrEmpty(result)) return;

            string[] split = result.Split(" ");
            int raidId = int.Parse(split[0]);

            Raid existingRaid = await _activityService.GetRaidAsync(raidId);
            bool existingRaidResult = await CheckExistingRaidIsValid(existingRaid);
            if(existingRaidResult == false) return;

            IUserMessage message = (IUserMessage) await Context.Channel.GetMessageAsync(ulong.Parse(existingRaid.MessageId), CacheMode.AllowDownload);
            if (message == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find message to edit").Build());
                return;
            }

            string description = result.Remove(0, raidId.ToString().Length + 1);

            var modifiedEmbed = new EmbedBuilder();
            var embed = message.Embeds.FirstOrDefault();
            modifiedEmbed.Description = description;
            EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateTitleColorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFieldsOnEmbed(modifiedEmbed, embed);
            await message.ModifyAsync(x =>
            {
                x.Embed = modifiedEmbed.Build();
            });

            await message.ReplyAsync(null, false, EmbedHelper.CreateSuccessReply($"The raid Id {raidId} has been edited!").Build());
        }

        [Command("close raid")]
        public async Task CloseRaid()
        {
            await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid id located in the footer of a raid e.g.\n\n;close raid 16").Build());
        }

        [Command("close raid")]
        public async Task CloseRaid([Remainder] string args)
        {
            string result = await CheckCloseRaidCommandIsValid(args);
            if (string.IsNullOrEmpty(result)) return;

            string[] split = result.Split(" ");
            int raidId = int.Parse(split[0]);

            Raid existingRaid = await _activityService.GetRaidAsync(raidId);
            bool exisingRaidResult = await CheckExistingRaidIsValid(existingRaid);
            if (exisingRaidResult == false) return;

            existingRaid.DateTimeClosed = DateTime.Now;
            await _activityService.UpdateRaidAsync(existingRaid);

            ITextChannel channel = await Context.Guild.GetTextChannelAsync(ulong.Parse(existingRaid.ChannelId));
            if(channel == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find channel where message is").Build());
                return;
            }
            IUserMessage message = (IUserMessage) await channel.GetMessageAsync(ulong.Parse(existingRaid.MessageId), CacheMode.AllowDownload);
            if (message == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find message to close").Build());
                return;
            }
            var modifiedEmbed = new EmbedBuilder();
            var embed = message.Embeds.FirstOrDefault();
            EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFieldsOnEmbed(modifiedEmbed, embed);
            modifiedEmbed.Title = "This raid is now closed";
            modifiedEmbed.Color = Color.Red;
            await message.UnpinAsync();
            await message.ModifyAsync(x =>
            {
                x.Embed = modifiedEmbed.Build();
            });

            await message.ReplyAsync(null, false, EmbedHelper.CreateSuccessReply($"The raid Id {raidId} has been closed!").Build());
        }

        private async Task AwardReputation(Raid existingRaid)
        {

        }

        private async Task<string> CheckEditRaidCommandIsValid(string args)
        {
            if (args == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid id located in the footer of a raid e.g.\n\n;edit raid 16 This is a new description").Build());
                return null;
            }

            if (args.ToLower().StartsWith("id"))
            {
                args = args.Substring(2).TrimStart();
            }

            string[] split = args.Split(" ");
            if (split.Length < 2)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid id located in the footer of a raid e.g.\n\n;edit raid 16 This is a new description").Build());
                return null;
            }

            int.TryParse(split[0], out int raidId);
            if (raidId <= 0)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid id located in the footer of a raid e.g.\n\n;edit raid 16 This is a new description").Build());
                return null;
            }

            return args;
        }

        private async Task<string> CheckCloseRaidCommandIsValid(string args)
        {
            if (args == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid id located in the footer of a raid e.g.\n\n;close raid 16").Build());
                return null;
            }

            if (args.ToLower().StartsWith("id"))
            {
                args = args.Substring(2).TrimStart();
            }

            string[] split = args.Split(" ");
            if (split.Length >= 2)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid id located in the footer of a raid e.g.\n\n;close raid 16").Build());
                return null;
            }

            int.TryParse(split[0], out int raidId);
            if (raidId <= 0)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid id located in the footer of a raid e.g.\n\n;close raid 16").Build());
                return null;
            }

            return args;
        }

        private async Task<bool> CheckExistingRaidIsValid(Raid existingRaid)
        {
            if (existingRaid == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find a Raid with that Id").Build());
                return false;
            }

            if (existingRaid.DateTimeClosed != null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("This raid is already closed").Build());
                return false;
            }

            if (existingRaid.CreatedByUserId != Context.User.Id.ToString())
            {
                IGuildUser guildUser = await Context.Guild.GetCurrentUserAsync();
                if(guildUser != null)
                {
                    if(guildUser.GuildPermissions.ModerateMembers)
                    {
                        return true;
                    }
                }
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Only the Guardian who created the raid or an Admin can edit/close a raid").Build());
                return false;
            }

            return true;
        }

        private async Task<bool> CheckExistingRaidIsValid(MongoDB.Models.Raid existingRaid)
        {
            if (existingRaid == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find a Raid with that Id").Build());
                return false;
            }

            if (existingRaid.DateTimeClosed != DateTime.MinValue)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("This raid is already closed").Build());
                return false;
            }

            if (existingRaid.CreatedByUserId != Context.User.Id)
            {
                IGuildUser guildUser = await Context.Guild.GetCurrentUserAsync();
                if (guildUser != null)
                {
                    if (guildUser.GuildPermissions.ModerateMembers)
                    {
                        return true;
                    }
                }
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Only the Guardian who created the raid or an Admin can edit/close a raid").Build());
                return false;
            }

            return true;
        }
        #endregion

        #region Mongo Raid
        [Command("testcreate raid")]
        public async Task TestCreateRaidCommand([Remainder] string args)
        {
            if (args == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid and a description for your raid e.g. create raid Vow of Disciple Tuesday 28th 6pm").Build());
                return;
            }

            MongoDB.Models.Raid newRaid = new MongoDB.Models.Raid()
            {
                DateTimeCreated = DateTime.UtcNow,
                CreatedByUserId = Context.User.Id,
                GuidId = Context.Guild.Id,
                ChannelId = Context.Channel.Id,
                MaxPlayerCount = 6,
                Players = new List<MongoDB.Models.ActivityUser>()
                {
                    new MongoDB.Models.ActivityUser(Context.User.Id, Context.User.Username)
                }
            };
            newRaid = await _activityService.CreateRaidAsync(newRaid);
            if (newRaid.Id <= 0)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("I couldn't create the raid because Jonny did something wrong!").Build());
                return;
            }


            var embed = new EmbedBuilder();

            string[] split = args.Split('\n');
            bool title = false;

            embed.WithColor(new Color(0, 255, 0));
            StringBuilder sb = new StringBuilder();
            int startingPostion = title ? 1 : 0;

            for (int i = startingPostion; i < split.Length; i++)
            {
                sb.AppendLine(split[i]);
            }

            embed.WithAuthor(Context.User);
            sb.AppendLine();
            sb.AppendLine();

            var user = Context.User;

            embed.Description = sb.ToString();

            embed.AddField("Players", $"<@{user.Id}>");
            embed.AddField("Subs", "-");

            embed.Footer = new EmbedFooterBuilder()
            {
                Text = $"Raid Id {newRaid.Id}\nUse J to Join | Use S to Sub.\nA max of 6 players may join a raid"
            };

            IUserMessage newMessage;
            IRole role = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Raid Fanatics");
            if (role != null)
            {
                // this will reply with the embed
                newMessage = await ReplyAsync(role.Mention, false, embed.Build());
            }
            else
            {
                // this will reply with the embed
                newMessage = await ReplyAsync(null, false, embed.Build());
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

        [Command("testclose raid")]
        public async Task TestCloseRaid([Remainder] string args)
        {
            string result = await CheckCloseRaidCommandIsValid(args);
            if (string.IsNullOrEmpty(result)) return;

            string[] split = result.Split(" ");
            int raidId = int.Parse(split[0]);

            MongoDB.Models.Raid existingRaid = await _activityService.GetMongoRaidAsync(raidId);
            bool exisingRaidResult = await CheckExistingRaidIsValid(existingRaid);
            if (exisingRaidResult == false) return;

            existingRaid.DateTimeClosed = DateTime.UtcNow;
            await _activityService.UpdateRaidAsync(existingRaid);

            ITextChannel channel = await Context.Guild.GetTextChannelAsync(existingRaid.ChannelId);
            if (channel == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find channel where message is").Build());
                return;
            }
            IUserMessage message = (IUserMessage)await channel.GetMessageAsync(existingRaid.MessageId, CacheMode.AllowDownload);
            if (message == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find message to close").Build());
                return;
            }
            var modifiedEmbed = new EmbedBuilder();
            var embed = message.Embeds.FirstOrDefault();
            EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFieldsOnEmbed(modifiedEmbed, embed);
            modifiedEmbed.Title = "This raid is now closed";
            modifiedEmbed.Color = Color.Red;
            await message.UnpinAsync();
            await message.ModifyAsync(x =>
            {
                x.Embed = modifiedEmbed.Build();
            });

            await message.ReplyAsync(null, false, EmbedHelper.CreateSuccessReply($"The raid Id {raidId} has been closed!").Build());
        }
        #endregion

        #region Fireteam
        [Command("testcreate fireteam")]
        public async Task TestCreateFireteamCommand([Remainder] string args)
        {
            if (args == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the total slots and a description for your fireteam e.g. ;create fireteam 3 Duality Dungeon ASAP will create a fireteam that a total of 3 people (including you) can join").Build());
                return;
            }

            string[] sizeSplit = args.Split(" ");

            if (int.TryParse(sizeSplit[0], out int fireteamSize) == false || fireteamSize > 6 || fireteamSize <= 1)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the the total slots and a description for your fireteam e.g. ;create fireteam 3 Duality Dungeon ASAP will create a fireteam that a total of 3 people (including you) can join").Build());
                return;
            }

            if (string.IsNullOrEmpty(args.Remove(0, 1)))
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, you must supply a description for your fireteam e.g. ;create fireteam 3 Duality Dungeon ASAP will create a fireteam that a total of 3 people (including you) can join").Build());
                return;
            }

            MongoDB.Models.Fireteam newFireteam = new MongoDB.Models.Fireteam()
            {
                DateTimeCreated = DateTime.UtcNow,
                CreatedByUserId = Context.User.Id,
                GuidId = Context.Guild.Id,
                ChannelId = Context.Channel.Id,
                MaxPlayerCount = (byte)fireteamSize,
                Players = new List<MongoDB.Models.ActivityUser>()
                {
                    new MongoDB.Models.ActivityUser(Context.User.Id, Context.User.Username)
                }
            };
            newFireteam = await _activityService.CreateFireteamAsync(newFireteam);
            if (newFireteam.Id <= 0)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("I couldn't create the fireteam because Jonny did something wrong!").Build());
                return;
            }


            var embed = new EmbedBuilder();

            string[] split = args.Split('\n');
            bool title = false;

            embed.WithColor(new Color(0, 255, 0));
            StringBuilder sb = new StringBuilder();
            int startingPostion = title ? 1 : 0;

            for (int i = startingPostion; i < split.Length; i++)
            {
                sb.AppendLine(split[i]);
            }

            embed.WithAuthor(Context.User);
            sb.AppendLine();
            sb.AppendLine();

            var user = Context.User;

            embed.Description = sb.ToString();

            embed.AddField("Players", $"<@{user.Id}>");
            embed.AddField("Subs", "-");

            embed.Footer = new EmbedFooterBuilder()
            {
                Text = $"Fireteam Id {newFireteam.Id}\nUse J to Join | Use S to Sub.\nA max of {newFireteam.MaxPlayerCount} players may join this fireteam"
            };

            IUserMessage newMessage;
            //IRole role = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Raid Fanatics");
            //if (role != null)
            //{
            //    // this will reply with the embed
            //    newMessage = await ReplyAsync(role.Mention, false, embed.Build());
            //}
            //else
            //{
                // this will reply with the embed
                newMessage = await ReplyAsync(null, false, embed.Build());
            //}


            newFireteam.MessageId = newMessage.Id;
            await _activityService.UpdateRaidAsync(newFireteam);

            await newMessage.PinAsync();
            await newMessage.AddReactionsAsync(new List<IEmote>()
            {
                new Emoji("🇯"),
                new Emoji("🇸"),
            });
        }
        [Command("create fireteam")]
        public async Task CreateFireteamCommand([Remainder] string args)
        {
            if (args == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the total slots and a description for your fireteam e.g. ;create fireteam 3 Duality Dungeon ASAP will create a fireteam that a total of 3 people (including you) can join").Build());
                return;
            }

            string[] sizeSplit = args.Split(" ");

            if(int.TryParse(sizeSplit[0], out int fireteamSize) == false || fireteamSize > 6 || fireteamSize <= 1)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the the total slots and a description for your fireteam e.g. ;create fireteam 3 Duality Dungeon ASAP will create a fireteam that a total of 3 people (including you) can join").Build());
                return;
            }

            if (string.IsNullOrEmpty(args.Remove(0, 1)))
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, you must supply a description for your fireteam e.g. ;create fireteam 3 Duality Dungeon ASAP will create a fireteam that a total of 3 people (including you) can join").Build());
                return;
            }

            Fireteam newFireteam = new Fireteam()
            {
                DateTimeCreated = DateTime.UtcNow,
                CreatedByUserId = Context.User.Id.ToString(),
                ChannelId = Context.Channel.Id.ToString()
            };
            newFireteam = await _activityService.CreateFireteamAsync(newFireteam);
            if (newFireteam.Id <= 0)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("I couldn't create the fireteam because Jonny did something wrong!").Build());
                return;
            }

            var embed = new EmbedBuilder();
            string description = args.Remove(0, fireteamSize.ToString().Length + 1);
            string[] split = description.Split('\n');
            bool title = false;

            embed.WithColor(new Color(0, 255, 0));
            StringBuilder sb = new StringBuilder();
            int startingPostion = title ? 1 : 0;

            for (int i = startingPostion; i < split.Length; i++)
            {
                sb.AppendLine(split[i]);
            }

            embed.WithAuthor(Context.User);
            sb.AppendLine();
            sb.AppendLine();

            var user = Context.User;

            embed.Description = sb.ToString();

            embed.AddField("Players", $"<@{user.Id}>");
            embed.AddField("Subs", "-");

            embed.Footer = new EmbedFooterBuilder()
            {
                Text = $"Fireteam Id {newFireteam.Id}\nUse J to Join | Use S to Sub.\nA max of {fireteamSize} players may join this fireteam"
            };


            // this will reply with the embed
            IUserMessage newMessage = await ReplyAsync(null, false, embed.Build());

            newFireteam.MessageId = newMessage.Id.ToString();
            await _activityService.UpdateFireteamAsync(newFireteam);

            await newMessage.PinAsync();
            await newMessage.AddReactionsAsync(new List<IEmote>()
            {
                new Emoji("🇯"),
                new Emoji("🇸"),
            });
        }

        [Command("edit fireteam")]
        public async Task EditFireteam([Remainder] string args)
        {
            string result = await CheckEditFireteamCommandIsValid(args);
            if (string.IsNullOrEmpty(result)) return;

            string[] split = result.Split(" ");
            int fireteamId = int.Parse(split[0]);

            Fireteam existingFireteam = await _activityService.GetFireteamAsync(fireteamId);
            bool existingFireteamResult = await CheckExistingFireteamIsValid(existingFireteam);
            if (existingFireteamResult == false) return;

            IUserMessage message = (IUserMessage)await Context.Channel.GetMessageAsync(ulong.Parse(existingFireteam.MessageId), CacheMode.AllowDownload);
            if (message == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find message to edit").Build());
                return;
            }

            string description = result.Remove(0, fireteamId.ToString().Length + 1);

            var modifiedEmbed = new EmbedBuilder();
            var embed = message.Embeds.FirstOrDefault();
            modifiedEmbed.Description = description;
            EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateTitleColorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFieldsOnEmbed(modifiedEmbed, embed);
            await message.ModifyAsync(x =>
            {
                x.Embed = modifiedEmbed.Build();
            });

            await ReplyAsync(null, false, EmbedHelper.CreateSuccessReply("The fireteam has been edited!").Build());
        }

        [Command("close fireteam")]
        public async Task CloseFireteam([Remainder] string args)
        {
            string result = await CheckCloseFireteamCommandIsValid(args);
            if (string.IsNullOrEmpty(result)) return;

            string[] split = result.Split(" ");
            int fireteamId = int.Parse(split[0]);

            Fireteam existingFireteam = await _activityService.GetFireteamAsync(fireteamId);
            bool existingFireteamResult = await CheckExistingFireteamIsValid(existingFireteam);
            if (existingFireteamResult == false) return;

            if (existingFireteam.DateTimeClosed == null || existingFireteam.DateTimeClosed == DateTime.MinValue)
            {
                existingFireteam.DateTimeClosed = DateTime.Now;
                await _activityService.UpdateFireteamAsync(existingFireteam);
            }

            ITextChannel channel = await Context.Guild.GetTextChannelAsync(ulong.Parse(existingFireteam.ChannelId));
            if (channel == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find channel where message is").Build());
                return;
            }

            IUserMessage message = (IUserMessage)await channel.GetMessageAsync(ulong.Parse(existingFireteam.MessageId), CacheMode.AllowDownload);
            if (message == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find message to edit").Build());
                return;
            }
            var modifiedEmbed = new EmbedBuilder();
            var embed = message.Embeds.FirstOrDefault();
            if(embed?.Title == "This activity is now closed")
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("This Fireteam is already closed").Build());
                return;
            }

            EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFieldsOnEmbed(modifiedEmbed, embed);
            modifiedEmbed.Title = "This activity is now closed";
            modifiedEmbed.Color = Color.Red;
            await message.UnpinAsync();
            await message.ModifyAsync(x =>
            {
                x.Embed = modifiedEmbed.Build();
            });

            await ReplyAsync(null, false, EmbedHelper.CreateSuccessReply("The Fireteam has been closed!").Build());
        }

        private async Task<string> CheckEditFireteamCommandIsValid(string args)
        {
            if (args == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the fireteam id located in the footer of a raid e.g.\n\n;edit fireteam 16 This is a new description").Build());
                return null;
            }

            if (args.ToLower().StartsWith("id"))
            {
                args = args.Substring(2).TrimStart();
            }

            string[] split = args.Split(" ");
            if (split.Length < 2)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the fireteam id located in the footer of a raid e.g.\n\n;edit fireteam 16 This is a new description").Build());
                return null;
            }

            int.TryParse(split[0], out int raidId);
            if (raidId <= 0)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the fireteam id located in the footer of a raid e.g.\n\n;edit fireteam 16 This is a new description").Build());
                return null;
            }

            return args;
        }

        private async Task<string> CheckCloseFireteamCommandIsValid(string args)
        {
            if (args == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the fireteam id located in the footer of a raid e.g.\n\n;close fireteam 16").Build());
                return null;
            }

            if (args.ToLower().StartsWith("id"))
            {
                args = args.Substring(2).TrimStart();
            }

            string[] split = args.Split(" ");
            if (split.Length >= 2)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the fireteam id located in the footer of a raid e.g.\n\n;close fireteam 16").Build());
                return null;
            }

            int.TryParse(split[0], out int raidId);
            if (raidId <= 0)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the fireteam id located in the footer of a raid e.g.\n\n;close fireteam 16").Build());
                return null;
            }

            return args;
        }

        private async Task<bool> CheckExistingFireteamIsValid(Fireteam existingRaid)
        {
            if (existingRaid == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find a Fireteam with that Id").Build());
                return false;
            }

            if (existingRaid.CreatedByUserId != Context.User.Id.ToString())
            {
                IGuildUser guildUser = await Context.Guild.GetCurrentUserAsync();
                if (guildUser != null)
                {
                    if (guildUser.GuildPermissions.ModerateMembers)
                    {
                        return true;
                    }
                }
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Only the Guardian who created the Fireteam or an Admin can edit/close a raid").Build());
                return false;
            }

            return true;
        }
        #endregion

    }
}
