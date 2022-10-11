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
    public class AdminCommands: ModuleBase
    {
        private readonly ActivityService _activityService;
        private readonly HiringService _hiringService;

        private const int CreatorPoints = 5;
        public AdminCommands(IServiceProvider services)
        {
            _activityService = services.GetRequiredService<ActivityService>();
            _hiringService = services.GetRequiredService<HiringService>();
        }

        [Command("new recruit")]
        [RequireUserPermission(Discord.GuildPermission.KickMembers)]
        public async Task CreateNewRecruit([Remainder] string args)
        {
            List<IGuildUser> guildUsers = await GetTaggedRecruiterAndRecruit(args);
            if(guildUsers == null || guildUsers.Count != 2)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Invalid command, only supply the recruiter and the recruit i.e. ;new recruit @SuperRedFalcon @NewPerson").Build());
                return;
            }
            const int recruiterIndex = 0;
            const int recruitIndex = 1;
            Recruiter existingRecruiter = await _hiringService.FindRecruit(guildUsers[recruitIndex].Id);
            if(existingRecruiter != null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply($"{existingRecruiter.Recruit.DisplayName} has already been recruited by {existingRecruiter.DisplayName}").Build());
                return;
            }

            Recruiter newRecruiter = await _hiringService.InsertNewRecruit(guildUsers[recruiterIndex], guildUsers[recruitIndex]);
            if(newRecruiter == null || newRecruiter.Id == Guid.Empty)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Failed to insert new recruit.. it's not your fault, it's Jonny's").Build());
                return;
            }

            await ReplyAsync(null, false, EmbedHelper.CreateSuccessReply($"Created successfully. {newRecruiter.Recruit.DisplayName} has been recruited by {newRecruiter.DisplayName}. You can check their progress by using the ;recruit progress @NewRecruit").Build());
        }

        [Command("recruit progress")]
        [RequireUserPermission(Discord.GuildPermission.KickMembers)]
        public async Task RecruitProgress([Remainder] string args)
        {
            IGuildUser recruit = await GetTaggedRecruit(args);
            if (recruit == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find a recruit record or you may have issued an invalid command ensure it is as follows.  ;recruit progress @Recruit").Build());
                return;
            }


            Recruiter existingRecruiter = await _hiringService.FindRecruit(recruit.Id);
            if (existingRecruiter == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply($"{recruit.DisplayName} has not been recruited by anybody").Build());
                return;
            }

            //TODO Show Checklist
        }


        [Command("list open raids")]
        [RequireUserPermission(Discord.GuildPermission.KickMembers)]
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
                if (openRaid.GuidId != Context.Guild.Id) 
                    continue;
                ITextChannel channel = await Context.Guild.GetTextChannelAsync(openRaid.ChannelId);
                if (channel == null) continue;
                IMessage message = await channel.GetMessageAsync(openRaid.MessageId);
                if(message == null) continue;

                double daysOld = Math.Round(DateTime.UtcNow.Subtract(openRaid.DateTimeCreated).TotalDays, 0);
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
        [RequireUserPermission(Discord.GuildPermission.KickMembers)]
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
                if (openFireteam.GuidId != Context.Guild.Id)
                    continue;
                ITextChannel channel = await Context.Guild.GetTextChannelAsync(openFireteam.ChannelId);
                if (channel == null) continue;
                IMessage message = await channel.GetMessageAsync(openFireteam.MessageId);
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


        private async Task<List<IGuildUser>> GetTaggedRecruiterAndRecruit(string args)
        {
            string sanitisedSplit = Regex.Replace(args, @"[(?<=\<)(.*?)(?=\>)]", string.Empty);
            List<IGuildUser> guildUsers = new List<IGuildUser>();
            if (sanitisedSplit.Contains("@") == false) return guildUsers;
            string[] users = sanitisedSplit.Split('@');
            foreach (string user in users)
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
                        if (guildUsers.FirstOrDefault(x => x.Id == userId) == null)
                            guildUsers.Add(guildUser);
                    }

                    if(guildUsers.Count == 2)
                    {
                        return guildUsers;
                    }
                }
            }

            return guildUsers;
        }

        private async Task<IGuildUser> GetTaggedRecruit(string args)
        {
            string sanitisedSplit = Regex.Replace(args, @"[(?<=\<)(.*?)(?=\>)]", string.Empty);
            List<IGuildUser> guildUsers = new List<IGuildUser>();
            if (sanitisedSplit.Contains("@") == false) return null;
            string[] users = sanitisedSplit.Split('@');
            foreach (string user in users)
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
                        if (guildUsers.FirstOrDefault(x => x.Id == userId) == null)
                            return guildUser;
                    }
                }
            }

            return null;
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
    }
}
