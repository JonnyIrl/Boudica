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
                await ReplyAsync("Invalid command arguments, supply the raid and a description for your raid e.g. create raid Vow of Disciple Tuesday 28th 6pm");
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
            if (split.Length > 1)
            {
                embed.Title = split[0];
                title = true;
            }

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

            await newMessage.AddReactionsAsync(new List<IEmote>()
            {
                new Emoji("🇯"),
                new Emoji("🇸"),
            });
        }

        [Command("edit raid")]
        public async Task EditRaid([Remainder] string args)
        {
            if (args == null)
            {
                await ReplyAsync("Invalid command arguments, supply the raid id located in the footer of a raid e.g. ;edit raid 6 This is a new description");
                return;
            }

            string[] split = args.Split(" ");
            if(split.Length < 2)
            {
                await ReplyAsync("Invalid command arguments, supply the raid id located in the footer of a raid e.g. ;edit raid 6 This is a new description");
                return;
            }

            int.TryParse(split[0], out int raidId);
            if(raidId <= 0)
            {
                await ReplyAsync("Invalid command arguments, supply the raid id located in the footer of a raid e.g. ;edit raid 6 This is a new description");
                return;
            }

            Raid existingRaid = await _activityService.GetRaidAsync(raidId);
            if(existingRaid == null)
            {
                await ReplyAsync("Could not find a Raid with that Id");
                return;
            }

            if (existingRaid.DateTimeClosed != null)
            {
                await ReplyAsync("This raid is already closed and cannot be edited");
                return;
            }

            if(existingRaid.CreatedByUserId != Context.User.Id.ToString())
            {
                await ReplyAsync("Only the Guardian who created the raid or an Admin can edit a raid");
                return;
            }

            IUserMessage message = (IUserMessage) await Context.Channel.GetMessageAsync(ulong.Parse(existingRaid.MessageId), CacheMode.AllowDownload);
            if (message == null)
            {
                await ReplyAsync("Could not find message to edit");
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
        }
    }
}
