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

        [Command("list open raid")]
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
            foreach(Raid openRaid in openRaids)
            {
                //ITextChannel message = await Context.Guild.GetTextChannelAsync(ulong.Parse(openRaid.ChannelId));
                //if (message == null) continue;
                double daysOld = Math.Round(DateTime.UtcNow.Subtract((DateTime)openRaid?.DateTimeCreated).TotalDays, 2);
                sb.AppendLine($"Raid Id {openRaid.Id} | {daysOld} days open");
            }

            embedBuilder.WithDescription(sb.ToString());
            await ReplyAsync(null, false, embedBuilder.Build());
        }
    }
}
