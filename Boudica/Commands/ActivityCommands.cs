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

        private const int CreatorPoints = 5;
        public ActivityCommands(IServiceProvider services)
        {
            _activityService = services.GetRequiredService<ActivityService>();
            _guardianService = services.GetRequiredService<GuardianService>();
            _guardianReputationService = services.GetRequiredService<GuardianReputationService>();
        }

        #region Raid
        [Command("create raid", RunMode = RunMode.Async)]
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

            //TODO Add RaidGroupInfo
            //Task.Run(async () =>
            //{
            //    await 
            //});
        }

        [Command("edit raid", RunMode = RunMode.Async)]
        public async Task EditRaid()
        {
            await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid id located in the footer of a raid e.g.\n\n;edit raid 16 This is a new description").Build());
        }

        [Command("edit raid", RunMode = RunMode.Async)]
        public async Task EditRaid([Remainder] string args)
        {
            bool result = await CheckEditRaidCommandIsValid(args);
            if (result == false) return;

            string[] split = args.Split(" ");
            int raidId = int.Parse(split[0]);

            Raid existingRaid = await _activityService.GetRaidAsync(raidId);
            result = await CheckExistingRaidIsValid(existingRaid);
            if(result == false) return;

            IUserMessage message = (IUserMessage) await Context.Channel.GetMessageAsync(ulong.Parse(existingRaid.MessageId), CacheMode.AllowDownload);
            if (message == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find message to edit").Build());
                return;
            }

            string description = args.Remove(0, raidId.ToString().Length + 1);

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

        [Command("close raid", RunMode = RunMode.Async)]
        public async Task CloseRaid([Remainder] string args)
        {
            bool result = await CheckCloseRaidCommandIsValid(args);
            if (result == false) return;

            string[] split = args.Split(" ");
            int raidId = int.Parse(split[0]);

            Raid existingRaid = await _activityService.GetRaidAsync(raidId);
            result = await CheckExistingRaidIsValid(existingRaid);
            if (result == false) return;

            existingRaid.DateTimeClosed = DateTime.Now;
            await _activityService.UpdateRaidAsync(existingRaid);

            IUserMessage message = (IUserMessage)await Context.Channel.GetMessageAsync(ulong.Parse(existingRaid.MessageId), CacheMode.AllowDownload);
            if (message == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find message to edit").Build());
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

        private async Task<bool> CheckEditRaidCommandIsValid(string args)
        {
            if (args == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid id located in the footer of a raid e.g.\n\n;edit raid 16 This is a new description").Build());
                return false;
            }

            string[] split = args.Split(" ");
            if (split.Length < 2)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid id located in the footer of a raid e.g.\n\n;edit raid 16 This is a new description").Build());
                return false;
            }

            int.TryParse(split[0], out int raidId);
            if (raidId <= 0)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid id located in the footer of a raid e.g.\n\n;edit raid 16 This is a new description").Build());
                return false;
            }

            return true;
        }

        private async Task<bool> CheckCloseRaidCommandIsValid(string args)
        {
            if (args == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid id located in the footer of a raid e.g.\n\n;close raid 16").Build());
                return false;
            }

            string[] split = args.Split(" ");
            if (split.Length >= 2)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid id located in the footer of a raid e.g.\n\n;close raid 16").Build());
                return false;
            }

            int.TryParse(split[0], out int raidId);
            if (raidId <= 0)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid id located in the footer of a raid e.g.\n\n;close raid 16").Build());
                return false;
            }

            return true;
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
            bool result = await CheckEditFireteamCommandIsValid(args);
            if (result == false) return;

            string[] split = args.Split(" ");
            int fireteamId = int.Parse(split[0]);

            Fireteam existingFireteam = await _activityService.GetFireteamAsync(fireteamId);
            result = await CheckExistingFireteamIsValid(existingFireteam);
            if (result == false) return;

            IUserMessage message = (IUserMessage)await Context.Channel.GetMessageAsync(ulong.Parse(existingFireteam.MessageId), CacheMode.AllowDownload);
            if (message == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find message to edit").Build());
                return;
            }

            string description = args.Remove(0, fireteamId.ToString().Length + 1);

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
            bool result = await CheckCloseFireteamCommandIsValid(args);
            if (result == false) return;

            string[] split = args.Split(" ");
            int fireteamId = int.Parse(split[0]);

            Fireteam existingFireteam = await _activityService.GetFireteamAsync(fireteamId);
            result = await CheckExistingFireteamIsValid(existingFireteam);
            if (result == false) return;

            existingFireteam.DateTimeClosed = DateTime.Now;
            await _activityService.UpdateFireteamAsync(existingFireteam);

            IUserMessage message = (IUserMessage)await Context.Channel.GetMessageAsync(ulong.Parse(existingFireteam.MessageId), CacheMode.AllowDownload);
            if (message == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find message to edit").Build());
                return;
            }
            var modifiedEmbed = new EmbedBuilder();
            var embed = message.Embeds.FirstOrDefault();
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

        private async Task<bool> CheckEditFireteamCommandIsValid(string args)
        {
            if (args == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the fireteam id located in the footer of a raid e.g.\n\n;edit fireteam 16 This is a new description").Build());
                return false;
            }

            string[] split = args.Split(" ");
            if (split.Length < 2)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the fireteam id located in the footer of a raid e.g.\n\n;edit fireteam 16 This is a new description").Build());
                return false;
            }

            int.TryParse(split[0], out int raidId);
            if (raidId <= 0)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the fireteam id located in the footer of a raid e.g.\n\n;edit fireteam 16 This is a new description").Build());
                return false;
            }

            return true;
        }

        private async Task<bool> CheckCloseFireteamCommandIsValid(string args)
        {
            if (args == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the fireteam id located in the footer of a raid e.g.\n\n;close fireteam 16").Build());
                return false;
            }

            string[] split = args.Split(" ");
            if (split.Length >= 2)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the fireteam id located in the footer of a raid e.g.\n\n;close fireteam 16").Build());
                return false;
            }

            int.TryParse(split[0], out int raidId);
            if (raidId <= 0)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the fireteam id located in the footer of a raid e.g.\n\n;close fireteam 16").Build());
                return false;
            }

            return true;
        }

        private async Task<bool> CheckExistingFireteamIsValid(Fireteam existingRaid)
        {
            if (existingRaid == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find a Fireteam with that Id").Build());
                return false;
            }

            if (existingRaid.DateTimeClosed != null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("This Fireteam is already closed").Build());
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
