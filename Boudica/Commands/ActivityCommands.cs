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
        private readonly GuardianService _guardianService;
        public delegate void AddRemoveReactionDelegate(ulong userId, bool addReaction);
        public event AddRemoveReactionDelegate OnAddRemoveReaction;

        private Emoji _jEmoji = new Emoji("🇯");
        private Emoji _sEmoji = new Emoji("🇸");
        private Emote _glimmerEmote;
        private List<Emoji> _successFailEmotes = null;

        private const ulong RaidChannel = 530529729321631785;
        private const string RaidRole = "Raid Fanatics";

        private const ulong DungeonChannel = 530529123349823528;
        private const string DungeonRole = "Dungeon Challengers";

        private const ulong VanguardChannel = 530530338099691530;
        private const string VanguardRole = "Nightfall Enthusiasts";

        private const ulong CrucibleChannel = 530529088620724246;
        private const string CrucibleRole = "Crucible Contenders";

        private const ulong GambitChannel = 552184673749696512;
        private const string GambitRole = "Gambit Hustlers";

        private const ulong MiscChannel = 530528672172736515;
        private const string MiscRole = "Activity Aficionados";


#if DEBUG
        private const ulong glimmerId = 1009200271475347567;
#else
        private const ulong glimmerId = 728197708074188802;
#endif

        private const int CreatorPoints = 5;
        public ActivityCommands(IServiceProvider services)
        {
            _activityService = services.GetRequiredService<ActivityService>();
            _guardianService = services.GetRequiredService<GuardianService>();
            if (Emote.TryParse($"<:misc_glimmer:{glimmerId}>", out _glimmerEmote) == false)
                _glimmerEmote = null;

            if(_successFailEmotes == null)
            {
                _successFailEmotes = new List<Emoji>();
                _successFailEmotes.Add(new Emoji("✅"));
                _successFailEmotes.Add(new Emoji("❌"));
            }
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

        private async Task<bool> CheckExistingRaidIsValid(Raid existingRaid, bool forceClose)
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

            if (forceClose)
            {
                IGuildUser guildUser = await Context.Guild.GetCurrentUserAsync();
                if (guildUser != null)
                {
                    if (guildUser.GuildPermissions.ModerateMembers)
                    {
                        return true;
                    }
                }
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Only a Moderator or Admin can edit/close a raid with this command.").Build());
                return false;
            }
            else
            {
                if (existingRaid.CreatedByUserId != Context.User.Id)
                {
                    await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Only the Guardian who created the raid can edit/close a raid").Build());
                    return false;
                }
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
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("I couldn't create the raid because Jonny did something wrong!").Build());
                return;
            }

            var embed = new EmbedBuilder();
            embed.WithColor(new Color(0, 255, 0));
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(args);
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

            Raid existingRaid = await _activityService.GetMongoRaidAsync(raidId);
            bool existingRaidResult = await CheckExistingRaidIsValid(existingRaid, false);
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
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingRaid);
            EmbedHelper.UpdateFieldsOnEmbed(modifiedEmbed, embed);
            await message.ModifyAsync(x =>
            {
                x.Embed = modifiedEmbed.Build();
            });

            await message.ReplyAsync(null, false, EmbedHelper.CreateSuccessReply($"The raid Id {raidId} has been edited!").Build());
        }

        [Command("remove player raid")]
        public async Task RemovePlayerFromRaid([Remainder] string args)
        {
            string result = await CheckEditRaidCommandIsValid(args);
            if (string.IsNullOrEmpty(result)) return;

            string[] split = result.Split(" ");
            int raidId = int.Parse(split[0]);

            Raid existingRaid = await _activityService.GetMongoRaidAsync(raidId);
            int oldPlayerCount = existingRaid.Players.Count;
            bool existingRaidResult = await CheckExistingRaidIsValid(existingRaid, false);
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

            List<ActivityUser> usersToRemove = await AddPlayersToNewActivity(args);
            StringBuilder sb = new StringBuilder();
            foreach (ActivityUser userToRemove in usersToRemove)
            {
                IGuildUser guildUser = await Context.Guild.GetUserAsync(userToRemove.UserId);
                if (guildUser != null)
                {
                    sb.Append($"<@{userToRemove.UserId}>");
                    existingRaid.Players.RemoveAll(x => x.UserId == userToRemove.UserId);
                    await message.RemoveReactionAsync(_jEmoji, guildUser);
                }
            }

            if(oldPlayerCount == existingRaid.Players.Count)
            {
                await message.ReplyAsync(null, false, EmbedHelper.CreateFailedReply($"Could not find player to remove from Raid {existingRaid.Id}").Build());
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

            await message.ReplyAsync(null, false, EmbedHelper.CreateSuccessReply($"{sb.ToString()} has been removed from the Raid").Build());
        }

        [Command("add player raid")]
        public async Task AddPlayerToRaid([Remainder] string args)
        {
            string result = await CheckEditRaidCommandIsValid(args);
            if (string.IsNullOrEmpty(result)) return;

            string[] split = result.Split(" ");
            int raidId = int.Parse(split[0]);

            MongoDB.Models.Raid existingRaid = await _activityService.GetMongoRaidAsync(raidId);
            int oldPlayerCount = existingRaid.Players.Count;
            bool existingRaidResult = await CheckExistingRaidIsValid(existingRaid, false);
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

            if(existingRaid.Players.Count == existingRaid.MaxPlayerCount)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("This raid is already full!").Build());
                return;
            }

            List<ActivityUser> usersToAdd = await AddPlayersToNewActivity(args);
            StringBuilder sb = new StringBuilder();
            foreach (ActivityUser userToRemove in usersToAdd)
            {
                IGuildUser guildUser = await Context.Guild.GetUserAsync(userToRemove.UserId);
                if (guildUser != null)
                {
                    
                    ActivityUser existingUser = existingRaid.Players.FirstOrDefault(x => x.UserId == userToRemove.UserId);
                    //Already added
                    if (existingUser != null)
                    {
                        continue;
                    }
                    else if(existingRaid.Players.Count < existingRaid.MaxPlayerCount)
                    {
                        sb.Append($"<@{userToRemove.UserId}>");
                        existingRaid.Players.Add(new ActivityUser(guildUser.Id, guildUser.DisplayName));
                    }
                }
            }

            if (oldPlayerCount == existingRaid.Players.Count)
            {
                await message.ReplyAsync(null, false, EmbedHelper.CreateFailedReply($"Could not find player to add to Raid {existingRaid.Id} or player already exists").Build());
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

            await message.ReplyAsync(null, false, EmbedHelper.CreateSuccessReply($"{sb.ToString()} has been added to the Raid").Build());
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
            bool exisingRaidResult = await CheckExistingRaidIsValid(existingRaid, false);
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
                await (await ReplyAsync(null, false, EmbedHelper.CreateSuccessReply($"Raid {raidId} has been closed! <@{existingRaid.CreatedByUserId}> did this activity get completed?").Build())).AddReactionsAsync(_successFailEmotes);
            }
            else
            {
                existingRaid.AwardedGlimmer = true;
                await _activityService.UpdateRaidAsync(existingRaid);
                await message.ReplyAsync(null, false, EmbedHelper.CreateSuccessReply($"Raid Id {raidId} has been closed!").Build());
            }
        }

        [Command("test close raid")]
        public async Task TestCloseRaid([Remainder] string args)
        {
            string result = await CheckCloseRaidCommandIsValid(args);
            if (string.IsNullOrEmpty(result)) return;

            string[] split = result.Split(" ");
            int raidId = int.Parse(split[0]);

            MongoDB.Models.Raid existingRaid = await _activityService.GetMongoRaidAsync(raidId);
            bool exisingRaidResult = await CheckExistingRaidIsValid(existingRaid, false);
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
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingRaid);
            EmbedHelper.UpdateFieldsOnEmbed(modifiedEmbed, embed);
            await message.UnpinAsync();
            await message.ModifyAsync(x =>
            {
                x.Embed = modifiedEmbed.Build();
            });

            await (await ReplyAsync(null, false, EmbedHelper.CreateSuccessReply($"Fireteam {raidId} has been closed! <@{existingRaid.CreatedByUserId}> did this activity get completed?").Build())).AddReactionsAsync(_successFailEmotes);
        }

        [Command("forceclose raid")]
        public async Task ForceCloseRaid([Remainder] string args)
        {
            string result = await CheckCloseRaidCommandIsValid(args);
            if (string.IsNullOrEmpty(result)) return;

            string[] split = result.Split(" ");
            int raidId = int.Parse(split[0]);

            MongoDB.Models.Raid existingRaid = await _activityService.GetMongoRaidAsync(raidId);
            bool exisingRaidResult = await CheckExistingRaidIsValid(existingRaid, true);
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
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingRaid);
            EmbedHelper.UpdateFieldsOnEmbed(modifiedEmbed, embed);
            modifiedEmbed.Title = "This raid is now closed";
            modifiedEmbed.Color = Color.Red;
            await message.UnpinAsync();
            await message.ModifyAsync(x =>
            {
                x.Embed = modifiedEmbed.Build();
            });

            await message.ReplyAsync(null, false, EmbedHelper.CreateSuccessReply($"Raid Id {raidId} has been closed!").Build());
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

        //[Command("test raid")]
        //public async Task TestRaid([Remainder] string args)
        //{
        //    string result = await CheckCloseRaidCommandIsValid(args);
        //    if (string.IsNullOrEmpty(result)) return;

        //    string[] split = result.Split(" ");
        //    int raidId = int.Parse(split[0]);

        //    MongoDB.Models.Raid existingRaid = await _activityService.GetMongoRaidAsync(raidId);
        //    if (existingRaid == null) return;

        //    ITextChannel channel = await Context.Guild.GetTextChannelAsync(existingRaid.ChannelId);
        //    if (channel == null)
        //    {
        //        await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find channel where message is").Build());
        //        return;
        //    }
        //    IUserMessage message = (IUserMessage)await channel.GetMessageAsync(existingRaid.MessageId, CacheMode.AllowDownload);
        //    if (message == null)
        //    {
        //        await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find message to close").Build());
        //        return;
        //    }

        //    Task.Run(async () =>
        //    {
        //        var listOfReactions = await message.GetReactionUsersAsync(_jEmoji, 100).ToListAsync();
        //        List<string> userNames = new List<string>();
        //        listOfReactions.ForEach(x => userNames.AddRange(x.Where(x => x.IsBot == false).Select(x => x.Mention.Replace("<@!", string.Empty).Replace(">", string.Empty))));
        //        await CalculateGlimmerForRaid(existingRaid, userNames);
        //    });
        //}

        [Command("backfill raids")]
        public async Task TestRaid()
        {
            if (Context.User.Id != 244209636897456129) 
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Only Jonny can do this command.. loser").Build());    
                return; 
            }

           List<Raid> closedRaids = await _activityService.FindAllClosedRaids();
            if (closedRaids == null) return;

            foreach (Raid closedRaid in closedRaids)
            {
                ITextChannel channel = await Context.Guild.GetTextChannelAsync(closedRaid.ChannelId);
                if (channel == null)
                {
                    continue;
                }
                IUserMessage message = (IUserMessage)await channel.GetMessageAsync(closedRaid.MessageId, CacheMode.AllowDownload);
                if (message == null)
                {
                    continue;
                }
                Task.Run(async () =>
                {
                    await CalculateGlimmerForActivity(closedRaid.Players, closedRaid.CreatedByUserId);
                });
            }
        }

        [Command("reset all glimmer")]
        public async Task ResetAllGlimmer()
        {
            if (Context.User.Id != 244209636897456129)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Only Jonny can do this command.. loser").Build());
                return;
            }

            Emoji failedEmote = new Emoji("❌");
            Emoji successEmote = new Emoji("✅");

            bool result = await _guardianService.ResetAllGlimmer();
            await Context.Message.AddReactionAsync(result ? successEmote : failedEmote);
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
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("I couldn't create the fireteam because Jonny did something wrong!").Build());
                return;
            }

            var embed = new EmbedBuilder();
            embed.WithColor(new Color(0, 255, 0));
            StringBuilder sb = new StringBuilder();
            //Remove the number for the size of the fireteam from the string for the Description
            sb.AppendLine(args.Substring(fireteamSize.ToString().Length + 1));
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
                newMessage = await ReplyAsync(role.Mention, false, embed.Build());
            }
            else
            {
                newMessage = await ReplyAsync(null, false, embed.Build());
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

        [Command("edit fireteam")]
        public async Task EditFireteam([Remainder] string args)
        {
            string result = await CheckEditFireteamCommandIsValid(args);
            if (string.IsNullOrEmpty(result)) return;

            string[] split = result.Split(" ");
            int fireteamId = int.Parse(split[0]);

            Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(fireteamId);
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
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingFireteam);
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

            Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(fireteamId);
            bool existingFireteamResult = await CheckExistingFireteamIsValid(existingFireteam);
            if (existingFireteamResult == false) return;

            if (existingFireteam.DateTimeClosed == DateTime.MinValue)
            {
                existingFireteam.DateTimeClosed = DateTime.UtcNow;
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
                await (await ReplyAsync(null, false, EmbedHelper.CreateSuccessReply($"Fireteam {fireteamId} has been closed! <@{existingFireteam.CreatedByUserId}> did this activity get completed?").Build())).AddReactionsAsync(_successFailEmotes);
            }
            else
            {
                existingFireteam.AwardedGlimmer = true;
                await _activityService.UpdateFireteamAsync(existingFireteam);
                await ReplyAsync(null, false, EmbedHelper.CreateSuccessReply($"Fireteam {fireteamId} has been closed!").Build());
            }      
        }

        [Command("test close fireteam")]
        public async Task TestCloseFireteam([Remainder] string args)
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
            if (embed?.Title == "This activity is now closed")
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("This Fireteam is already closed").Build());
                return;
            }

            EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingFireteam);
            EmbedHelper.UpdateFieldsOnEmbed(modifiedEmbed, embed);
            await message.UnpinAsync();
            await message.ModifyAsync(x =>
            {
                x.Embed = modifiedEmbed.Build();
            });

            await (await ReplyAsync(null, false, EmbedHelper.CreateSuccessReply($"Fireteam {fireteamId} has been closed! <@{existingFireteam.CreatedByUserId}> did this activity get completed?").Build())).AddReactionsAsync(_successFailEmotes);
        }

        [Command("remove player fireteam")]
        public async Task RemovePlayerFromFireteam([Remainder] string args)
        {
            string result = await CheckEditFireteamCommandIsValid(args);
            if (string.IsNullOrEmpty(result)) return;

            string[] split = result.Split(" ");
            int fireteamId = int.Parse(split[0]);

            Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(fireteamId);
            int oldPlayerCount = existingFireteam.Players.Count;
            bool existingFireteamResult = await CheckExistingFireteamIsValid(existingFireteam);
            if (existingFireteamResult == false) return;

            if (Context.Guild.Id != existingFireteam.GuidId)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find message to edit").Build());
                return;
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

            List<ActivityUser> usersToRemove = await AddPlayersToNewActivity(args);
            StringBuilder sb = new StringBuilder();
            foreach (ActivityUser userToRemove in usersToRemove)
            {
                IGuildUser guildUser = await Context.Guild.GetUserAsync(userToRemove.UserId);
                if (guildUser != null)
                {
                    sb.Append($"<@{userToRemove.UserId}>");
                    existingFireteam.Players.RemoveAll(x => x.UserId == userToRemove.UserId);
                    await message.RemoveReactionAsync(_jEmoji, guildUser);
                }
            }

            if (oldPlayerCount == existingFireteam.Players.Count)
            {
                await message.ReplyAsync(null, false, EmbedHelper.CreateFailedReply($"Could not find player to remove from Fireteam {existingFireteam.Id}").Build());
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

        private async Task<bool> CheckExistingFireteamIsValid(Fireteam existingFireteam)
        {
            if (existingFireteam == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find a Fireteam with that Id").Build());
                return false;
            }

            if (existingFireteam.CreatedByUserId != Context.User.Id)
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
            const string PlayersTitle = "Players";
            const string SubstitutesTitle = "Subs";
            if (activityUsers == null || activityUsers.Count == 0)
            {
                embed.AddField(title, "-");
                return embed;
            }

            StringBuilder sb = new StringBuilder();
            foreach (ActivityUser user in activityUsers)
            {
                if (_glimmerEmote != null && title != SubstitutesTitle && user.Reacted)
                    sb.AppendLine($"{_glimmerEmote} {user.DisplayName}");
                else
                    sb.AppendLine(user.DisplayName);
            }

            embed.AddField(title, sb.ToString().Trim());
            return embed;
        }
        private async Task CalculateGlimmerForActivity(List<ActivityUser> activityUsers, ulong creatorId)
        {
            if (activityUsers == null) return;
            int increaseAmount = 1 * activityUsers.Count;
            foreach(ActivityUser user in activityUsers)
            {
                if (user.UserId == creatorId)
                {
                    await _guardianService.IncreaseGlimmerAsync(user.UserId, increaseAmount + 3);
                    Console.WriteLine($"Increased Glimmer for {user.DisplayName} by {increaseAmount + 3}");
                }
                else if(user.Reacted)
                {
                    await _guardianService.IncreaseGlimmerAsync(user.UserId, increaseAmount);
                    Console.WriteLine($"Increased Glimmer for {user.DisplayName} by {increaseAmount}");
                }
            }
        }

        [Command("test")]
        public async Task TestCommand([Remainder] string args)
        {

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
}
