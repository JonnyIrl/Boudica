using Boudica.Database.Models;
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
    public class AdminCommands: ModuleBase
    {
        private readonly ActivityService _activityService;
        private readonly GuardianService _guardianService;
        private readonly GuardianReputationService _guardianReputationService;
        private readonly RaidGroupService _raidGroupService;

        private const int CreatorPoints = 5;
        public AdminCommands(IServiceProvider services)
        {
            _activityService = services.GetRequiredService<ActivityService>();
            _guardianService = services.GetRequiredService<GuardianService>();
            _guardianReputationService = services.GetRequiredService<GuardianReputationService>();
            _raidGroupService = services.GetRequiredService<RaidGroupService>();
        }

        [Command("list open raids")]
        [RequireUserPermission(Discord.GuildPermission.ModerateMembers)]
        public async Task ListOpenRaids()
        {
            IList<Raid> openRaids = await _activityService.FindAllOpenRaids();
            openRaids = openRaids.OrderBy(x => x.DateTimeCreated).ToList();
            if(openRaids == null || openRaids.Count == 0)
            {
                await ReplyAsync("There are no open raids!");
                return;
            }

            EmbedBuilder embedBuilder = new EmbedBuilder();
            StringBuilder sb = new StringBuilder();
            embedBuilder.WithTitle("Below is a list of open raids. The command to close the raid is at the beginning of each.");
            foreach(Raid openRaid in openRaids)
            {
                ITextChannel channel = await Context.Guild.GetTextChannelAsync(ulong.Parse(openRaid.ChannelId));
                if (channel == null) continue;
                IMessage message = await channel.GetMessageAsync(ulong.Parse(openRaid.MessageId));
                if(message == null) continue;

                double daysOld = Math.Round(DateTime.UtcNow.Subtract((DateTime)openRaid?.DateTimeCreated).TotalDays, 0);
                sb.AppendLine($";close raid {openRaid.Id} | {daysOld} days open | Created By <@{openRaid.CreatedByUserId}> |\n{message.Embeds.First().Description}\n\n");
            }

            if (sb.Length == 0)
            {
                await ReplyAsync("There are no open raids!");
                return;
            }

            embedBuilder.Description = sb.ToString();
            embedBuilder.WithDescription(sb.ToString());
            await ReplyAsync(null, false, embedBuilder.Build());
        }

        [Command("list open fireteams")]
        [RequireUserPermission(Discord.GuildPermission.ModerateMembers)]
        public async Task ListOpenFireteams()
        {
            IList<Fireteam> openFireteams = await _activityService.FindAllOpenFireteams();
            openFireteams = openFireteams.OrderBy(x => x.DateTimeCreated).ToList();
            if (openFireteams == null || openFireteams.Count == 0)
            {
                await ReplyAsync("There are no open fireteams!");
                return;
            }

            EmbedBuilder embedBuilder = new EmbedBuilder();
            StringBuilder sb = new StringBuilder();
            embedBuilder.WithTitle("Below is a list of open Fireteam. The command to close the fireteam is at the beginning of each.");
            foreach (Fireteam openFireteam in openFireteams)
            {
                ITextChannel channel = await Context.Guild.GetTextChannelAsync(ulong.Parse(openFireteam.ChannelId));
                if (channel == null) continue;
                IMessage message = await channel.GetMessageAsync(ulong.Parse(openFireteam.MessageId));
                if (message == null) continue;

                double daysOld = Math.Round(DateTime.UtcNow.Subtract((DateTime)openFireteam?.DateTimeCreated).TotalDays, 0);
                sb.AppendLine($";close fireteam {openFireteam.Id} | {daysOld} days open | Created By <@{openFireteam.CreatedByUserId}> |\n{message.Embeds.First().Description}\n\n");
            }

            if (sb.Length == 0)
            {
                await ReplyAsync("There are no open fireteams!");
                return;
            }

            embedBuilder.Description = sb.ToString();
            embedBuilder.WithDescription(sb.ToString());
            await ReplyAsync(null, false, embedBuilder.Build());
        }
    }
}
