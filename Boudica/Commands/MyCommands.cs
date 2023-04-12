using Boudica.Helpers;
using Boudica.MongoDB.Models;
using Boudica.Services;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Commands
{
    public class MyCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly ActivityService _activityService;

        public MyCommands(IServiceProvider services)
        {
            _activityService = services.GetRequiredService<ActivityService>();
        }

        [SlashCommand("my", "Display a list of raids or fireteams you are in that are still open")]
        public async Task MyCommand(MyActivityTypes activityType)
        {
            switch (activityType)
            {
                case MyActivityTypes.Raid:
                     await GetMyRaids();
                    break;
                case MyActivityTypes.Fireteam:
                    await GetMyFireteams();
                    break;
            }
        }

        public async Task GetMyRaids()
        {
            List<Raid> openRaids = await _activityService.FindAllOpenRaids(Context.Guild.Id);
            openRaids.RemoveAll(x => (x.CreatedByUserId != Context.User.Id && x.Players.Any(x => x.UserId == Context.User.Id) == false) || x.GuidId != Context.Guild.Id || (x.DateTimePlanned == DateTime.MinValue || x.DateTimePlanned < DateTime.UtcNow));
            if (openRaids.Any() == false)
            {
                await RespondAsync(embed: EmbedHelper.CreateSuccessReply("You are in no open raids").Build(), ephemeral: true);
                return;
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                if (openRaids.Count > 1)
                    sb.AppendLine("You have the following Raids:\n\n");
                else
                    sb.AppendLine("You have the following Raid:\n\n");
                foreach (Raid openRaid in openRaids.OrderBy(x => x.DateTimePlanned))
                {
                    if (openRaid.DateTimePlanned == DateTime.MinValue) continue;
                    long unixTime = ((DateTimeOffset)openRaid.DateTimePlanned).ToUnixTimeSeconds();
                    sb.AppendLine($"{openRaid.Title}. <t:{unixTime}:F> <t:{unixTime}:R>");
                    sb.AppendLine("");
                };

                if(sb.Length == 0)
                {
                    await RespondAsync(embed: EmbedHelper.CreateSuccessReply("You are in no open raids").Build(), ephemeral: true);
                    return;
                }
                else
                {
                    await RespondAsync(embed: EmbedHelper.CreateSuccessReply(sb.ToString()).Build(), ephemeral: true);
                }
            }
        }

        public async Task GetMyFireteams()
        {
            List<Fireteam> openFireteams = await _activityService.FindAllOpenFireteams(Context.Guild.Id);
            openFireteams.RemoveAll(x => (x.CreatedByUserId != Context.User.Id && x.Players.Any(x => x.UserId == Context.User.Id) == false) || x.GuidId != Context.Guild.Id);
            if (openFireteams.Any() == false)
            {
                await RespondAsync(embed: EmbedHelper.CreateSuccessReply("You are in no open fireteams").Build(), ephemeral: true);
                return;
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                foreach (Fireteam openFireteam in openFireteams)
                {
                    ITextChannel channel = Context.Guild.GetTextChannel(openFireteam.ChannelId);
                    if (channel == null)
                    {
                        continue;
                    }
                    IUserMessage message = (IUserMessage)await channel.GetMessageAsync(openFireteam.MessageId, CacheMode.AllowDownload);
                    if (message == null)
                    {
                        continue;
                    }

                    var embed = message.Embeds.FirstOrDefault();
                    if (embed == null) continue;
                    sb.AppendLine($"Fireteam Id {openFireteam.Id}");
                    sb.AppendLine(embed.Description);
                    sb.AppendLine("");
                };

                if (sb.Length == 0)
                {
                    await RespondAsync(embed: EmbedHelper.CreateSuccessReply("You are in no open fireteams").Build(), ephemeral: true);
                    return;
                }
                else
                {
                    await RespondAsync(embed: EmbedHelper.CreateSuccessReply("Here are your current open Fireteams\n\n" + sb.ToString()).Build(), ephemeral: true);
                }
            }
        }
    }

    public enum MyActivityTypes
    {
        [ChoiceDisplay("Raids")]
        Raid = 1,
        [ChoiceDisplay("Fireteams")]
        Fireteam = 2
    }
}
