using Boudica.Classes;
using Boudica.Helpers;
using Boudica.MongoDB.Models;
using Boudica.Services;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Boudica.Commands
{
    public class ActivityCommands : ModuleBase
    {
        private readonly ActivityService _activityService;

        private const int CreatorPoints = 5;
        public ActivityCommands(IServiceProvider services)
        {
            _activityService = services.GetRequiredService<ActivityService>();
        }

        #region Raid
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

        private async Task<string> CheckAlertRaidCommandIsValid(string args)
        {
            if (args == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid id located in the footer of a raid e.g.\n\n;roll call/alert/rollcall/ raid 16").Build());
                return null;
            }

            if (args.ToLower().StartsWith("id"))
            {
                args = args.Substring(2).TrimStart();
            }

            string[] split = args.Split(" ");
            if (split.Length >= 2)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid id located in the footer of a raid e.g.\n\n;roll call/alert/rollcall/ raid 16").Build());
                return null;
            }

            int.TryParse(split[0], out int raidId);
            if (raidId <= 0)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid id located in the footer of a raid e.g.\n\n;roll call/alert/rollcall/ raid 16").Build());
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

        private async Task<bool> CheckCanAlert(Raid existingRaid)
        {

            if (existingRaid == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find a Raid with that Id").Build());
                return false;
            }

            if (existingRaid.DateTimeClosed != DateTime.MinValue)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("This Raid is already closed").Build());
                return false;
            }
            if (existingRaid.CreatedByUserId != Context.User.Id)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Only the person who created the Raid can alert the players").Build());
                return false;
            }

            if (existingRaid.DateTimeAlerted.Date == DateTime.UtcNow.Date)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("You can only roll call/rollcall/alert once per day").Build());
                return false;
            }

            return true;
        }
        #endregion

        #region Mongo Raid
        [Command("create raid")]
        public async Task CreateRaidCommand([Remainder] string args)
        {
            if (args == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid and a description for your raid e.g. create raid Vow of Disciple Tuesday 28th 6pm").Build());
                return;
            }

            List<ActivityUser> addedUsers = await AddPlayersToNewActivity(args);
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
            if (addedUsers.Any())
            {
                newRaid.Players.AddRange(addedUsers);
            }
            newRaid = await _activityService.CreateRaidAsync(newRaid);
            if (newRaid.Id <= 0)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("I couldn't create the raid because Jonny did something wrong!").Build());
                return;
            }


            var embed = new EmbedBuilder();
            if (args.Contains("|"))
                args = args.Substring(0, args.LastIndexOf("|"));

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
            string description = sb.ToString();
            foreach(ActivityUser activityUser in addedUsers)
            {
                description = description.Replace($"<@{activityUser.UserId}>", string.Empty);
            }
            description = description.Trim();
            embed.Description = description;

            AddActivityUsersField(embed, "Players", newRaid.Players);
            AddActivityUsersField(embed, "Subs", newRaid.Substitutes);

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

            MongoDB.Models.Raid existingRaid = await _activityService.GetMongoRaidAsync(raidId);
            bool existingRaidResult = await CheckExistingRaidIsValid(existingRaid);
            if (existingRaidResult == false) return;

            if (Context.Guild.Id != existingRaid.GuidId)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find message to edit").Build());
                return;
            }
            ITextChannel channel = await Context.Guild.GetTextChannelAsync(existingRaid.ChannelId);
            if (channel == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find channel where message is").Build());
                return;
            }
            IUserMessage message = (IUserMessage)await channel.GetMessageAsync(existingRaid.MessageId, CacheMode.AllowDownload);
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

        [Command("alert raid")]
        [Alias("rollcall raid", "roll call raid")]
        public async Task AlertRaid([Remainder] string args)
        {
            string result = await CheckAlertRaidCommandIsValid(args);
            if (string.IsNullOrEmpty(result)) return;

            string[] split = result.Split(" ");
            int raidId = int.Parse(split[0]);

            MongoDB.Models.Raid existingRaid = await _activityService.GetMongoRaidAsync(raidId);
            bool exisingRaidResult = await CheckCanAlert(existingRaid);
            if (exisingRaidResult == false) return;

            existingRaid.DateTimeAlerted = DateTime.UtcNow;
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

            StringBuilder sb = new StringBuilder();
            foreach(ActivityUser player in existingRaid.Players)
            {
                if (player.UserId == Context.User.Id) continue;
                sb.Append($"<@{player.UserId}> ");
            }

            sb.Append("Are you all still ok for this raid?");
            await message.ReplyAsync(sb.ToString());
        }
        #endregion

        #region Fireteam
        [Command("create fireteam")]
        public async Task CreateFireteamCommand([Remainder] string args)
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

            List<ActivityUser> addedUsers = await AddPlayersToNewActivity(args, fireteamSize - 1);
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

            if (addedUsers.Any())
            {
                newFireteam.Players.AddRange(addedUsers);
            }
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

            string description = sb.ToString();
            foreach (ActivityUser activityUser in addedUsers)
            {
                description = description.Replace($"<@{activityUser.UserId}>", string.Empty);
            }
            description = description.Trim();
            embed.Description = description;

            AddActivityUsersField(embed, "Players", newFireteam.Players);
            AddActivityUsersField(embed, "Subs", newFireteam.Substitutes);

            embed.Footer = new EmbedFooterBuilder()
            {
                Text = $"Fireteam Id {newFireteam.Id}\nUse J to Join | Use S to Sub.\nA max of {newFireteam.MaxPlayerCount} players may join this fireteam"
            };

            IUserMessage newMessage;

            newMessage = await ReplyAsync(null, false, embed.Build());


            newFireteam.MessageId = newMessage.Id;
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

            MongoDB.Models.Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(fireteamId);
            bool existingFireteamResult = await CheckExistingFireteamIsValid(existingFireteam);
            if (existingFireteamResult == false) return;

            ITextChannel channel = await Context.Guild.GetTextChannelAsync(existingFireteam.ChannelId, CacheMode.AllowDownload);
            if(channel == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find channel where message is").Build());
                return;
            }

            IUserMessage message = (IUserMessage)await Context.Channel.GetMessageAsync(existingFireteam.MessageId, CacheMode.AllowDownload);
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

            await ReplyAsync(null, false, EmbedHelper.CreateSuccessReply($"The fireteam Id {fireteamId} has been edited!").Build());
        }

        [Command("close fireteam")]
        public async Task CloseFireteam([Remainder] string args)
        {
            string result = await CheckCloseFireteamCommandIsValid(args);
            if (string.IsNullOrEmpty(result)) return;

            string[] split = result.Split(" ");
            int fireteamId = int.Parse(split[0]);

            MongoDB.Models.Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(fireteamId);
            bool existingFireteamResult = await CheckExistingFireteamIsValid(existingFireteam);
            if (existingFireteamResult == false) return;

            if (existingFireteam.DateTimeClosed == DateTime.MinValue)
            {
                existingFireteam.DateTimeClosed = DateTime.Now;
                await _activityService.UpdateFireteamAsync(existingFireteam);
            }

            ITextChannel channel = await Context.Guild.GetTextChannelAsync(existingFireteam.ChannelId);
            if (channel == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find channel where message is").Build());
                return;
            }

            IUserMessage message = (IUserMessage)await channel.GetMessageAsync(existingFireteam.MessageId, CacheMode.AllowDownload);
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

        private async Task<bool> CheckExistingFireteamIsValid(MongoDB.Models.Fireteam existingRaid)
        {
            if (existingRaid == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find a Fireteam with that Id").Build());
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
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Only the Guardian who created the Fireteam or an Admin can edit/close a raid").Build());
                return false;
            }

            return true;
        }
        private async Task<List<ActivityUser>> AddPlayersToNewActivity(string args, int maxCount = 5)
        {
            string sanitisedSplit = Regex.Replace(args, @"[(?<=\<)(.*?)(?=\>)]", string.Empty);
            List<ActivityUser> activityUsers = new List<ActivityUser>();
            if (sanitisedSplit.Contains("@") == false) return activityUsers;
            string[] users = sanitisedSplit.Split('@');     
            foreach(string user in users)
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
                    if (userId == Context.User.Id) continue;
                    IGuildUser guildUser = await Context.Guild.GetUserAsync(userId);
                    if (guildUser != null)
                    {
                        if(activityUsers.FirstOrDefault(x => x.UserId == userId) == null)
                            activityUsers.Add(new ActivityUser(userId, guildUser.DisplayName));
                    }
                }               
            }

            while (activityUsers.Count > maxCount)
            {
                activityUsers.RemoveAt(activityUsers.Count -1);
            }
            return activityUsers;
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
        private EmbedBuilder AddActivityUsersField(EmbedBuilder embed, string title, List<ActivityUser> activityUsers)
        {
            if (activityUsers == null || activityUsers.Count == 0)
            {
                embed.AddField(title, "-");
                return embed;
            }

            StringBuilder sb = new StringBuilder();
            foreach (ActivityUser user in activityUsers)
            {
                sb.AppendLine(user.DisplayName);
            }

            embed.AddField(title, sb.ToString().Trim());
            return embed;
        }
        #endregion

    }
}
