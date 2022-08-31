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
        public ActivityCommands(IServiceProvider services)
        {
            _activityService = services.GetRequiredService<ActivityService>();
        }

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

            var embed = new EmbedBuilder();

            string[] split = args.Split('\n');
            bool title = false;
            //if (split.Length > 1)
            //{
            //    embed.Title = split[0];
            //    title = true;
            //}

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


            // this will reply with the embed
            IUserMessage newMessage = await ReplyAsync(null, false, embed.Build());
            
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

            await ReplyAsync(null, false, EmbedHelper.CreateSuccessReply("The raid has been edited!").Build());
        }

        [Command("close raid")]
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

            await ReplyAsync(null, false, EmbedHelper.CreateSuccessReply("The raid has been closed!").Build());
        }

        private async Task<bool> CheckEditRaidCommandIsValid(string args)
        {
            if (args == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid id located in the footer of a raid e.g. ;edit raid 6 This is a new description").Build());
                return false;
            }

            string[] split = args.Split(" ");
            if (split.Length < 2)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid id located in the footer of a raid e.g. ;edit raid 6 This is a new description").Build());
                return false;
            }

            int.TryParse(split[0], out int raidId);
            if (raidId <= 0)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid id located in the footer of a raid e.g. ;edit raid 6 This is a new description").Build());
                return false;
            }

            return true;
        }

        private async Task<bool> CheckCloseRaidCommandIsValid(string args)
        {
            if (args == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid id located in the footer of a raid e.g. ;close raid 6").Build());
                return false;
            }

            string[] split = args.Split(" ");
            if (split.Length >= 2)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid id located in the footer of a raid e.g. ;close raid 6").Build());
                return false;
            }

            int.TryParse(split[0], out int raidId);
            if (raidId <= 0)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command arguments, supply the raid id located in the footer of a raid e.g. ;close raid 6").Build());
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
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Only the Guardian who created the raid or an Admin can edit/close a raid").Build());
                return false;
            }

            return true;
        }

    }
}
