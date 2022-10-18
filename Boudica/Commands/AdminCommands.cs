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
        private readonly GuardianService _guardianService;

        private readonly Emoji _successEmoji;
        private readonly Emoji _failureEmoji;

        private const int CreatorPoints = 5;
        public AdminCommands(IServiceProvider services)
        {
            _activityService = services.GetRequiredService<ActivityService>();
            _hiringService = services.GetRequiredService<HiringService>();
            _guardianService = services.GetRequiredService<GuardianService>();

            _successEmoji = new Emoji("✅");
            _failureEmoji = new Emoji("❌");
        }

        [Command("recruit help")]
        [RequireUserPermission(Discord.GuildPermission.KickMembers)]
        public async Task RecruitHelp()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("**New Recruit**  ;new recruit @Recruiter @NewJoiner");
            sb.AppendLine("**Recruit Progress** ;recruit progress @NewJoiner");
            sb.AppendLine("**All Recruit Progress**  ;all recruit progress");
            sb.AppendLine("**Add Warning to Recruit**  ;recruit warn @NewJoiner");
            sb.AppendLine("**Add Note to Recruit**  ;recruit note @NewJoiner This player posted an offensive gif");

            await RespondAsync(embed: EmbedHelper.CreateSuccessReply(sb.ToString()).Build());
        }

        [Command("new recruit")]
        [RequireUserPermission(Discord.GuildPermission.KickMembers)]
        public async Task CreateNewRecruit([Remainder] string args)
        {
            List<IGuildUser> guildUsers = await GetTaggedRecruiterAndRecruit(args);
            if(guildUsers == null || guildUsers.Count != 2)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command, only supply the recruiter and the recruit i.e. ;new recruit @Recruiter @NewPerson").Build());
                return;
            }

            if(guildUsers[1].Id == Context.User.Id)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command, you cannot recruit yourself").Build());
                return;
            }
            const int recruiterIndex = 0;
            const int recruitIndex = 1;
            Recruiter existingRecruiter = await _hiringService.FindRecruit(guildUsers[recruitIndex].Id);
            if(existingRecruiter != null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply($"{existingRecruiter.Recruit.DisplayName} has already been recruited by {existingRecruiter.DisplayName}").Build());
                return;
            }

            Recruiter newRecruiter = await _hiringService.InsertNewRecruit(guildUsers[recruiterIndex], guildUsers[recruitIndex]);
            if(newRecruiter == null || newRecruiter.Id == Guid.Empty)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Failed to insert new recruit.. it's not your fault, it's Jonny's").Build());
                return;
            }

            await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"Created successfully. {newRecruiter.Recruit.DisplayName} has been recruited by {newRecruiter.DisplayName}. You can check their progress by using the ;recruit progress <@{newRecruiter.Recruit.Id}>").Build());
        }

        [Command("recruit progress")]
        [RequireUserPermission(Discord.GuildPermission.KickMembers)]
        public async Task RecruitProgress([Remainder] string args)
        {
            IGuildUser recruit = await GetTaggedRecruit(args);
            if (recruit == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find a recruit record or you may have issued an invalid command ensure it is as follows.  ;recruit progress @Recruit").Build());
                return;
            }


            Recruiter existingRecruiter = await _hiringService.FindRecruit(recruit.Id);
            if (existingRecruiter == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply($"{recruit.DisplayName} has not been recruited by anybody").Build());
                return;
            }

            EmbedBuilder embedBuilder = CreateEmbedForRecruit(existingRecruiter);
            await RespondAsync(embed: embedBuilder.Build());
        }

        [Command("all recruit progress")]
        [RequireUserPermission(Discord.GuildPermission.KickMembers)]
        public async Task AllRecruitProgress()
        {
            List<Recruiter> allRecruiters = await _hiringService.FindAllRecruits();
            if (allRecruiters == null || allRecruiters.Count == 0)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply($"There are currently no recruits").Build());
                return;
            }


            foreach (Recruiter recruiter in allRecruiters)
            {
                EmbedBuilder embedBuilder = CreateEmbedForRecruit(recruiter);
                await RespondAsync(embed: embedBuilder.Build());
                await Task.Delay(150);
            }
        }

        private EmbedBuilder CreateEmbedForRecruit(Recruiter recruiter)
        {
            StringBuilder sb = new StringBuilder();
            RecruitChecklist checkList = recruiter.Recruit.RecruitChecklist;
            //sb.AppendLine("Created a Post ");
            //sb.Append(checkList.CreatedPost ? _successEmoji : _failureEmoji);
            sb.AppendLine(checkList.CreatedPost ? $" {_successEmoji} Created a Post" : $"{_failureEmoji} Created a Post ");

            //sb.AppendLine("Joined a Post ");
            //sb.Append(checkList.JoinedPost ? _successEmoji : _failureEmoji);
            sb.AppendLine(checkList.JoinedPost ? $"{_successEmoji} Joined a Post" : $"{_failureEmoji} Joined a Post");

            int totalDays = (int)DateTime.UtcNow.Subtract(checkList.DateTimeJoined).TotalDays;
            //sb.AppendLine("Been in the clan for 30 days");
            //sb.Append(totalDays >= 30 ? _successEmoji : _failureEmoji + $" {(30 - totalDays)} Days left");
            sb.AppendLine(totalDays >= 30 ? $"{_successEmoji} Been in the clan for 30 days " : $"{_failureEmoji} Been in the clan for 30 days - {(30 - totalDays)} Days left");

            sb.AppendLine();
            sb.AppendLine("**Extra Information**");
            sb.AppendLine($"Warning Count {checkList.WarningCount}");
            if (checkList.Notes.Count > 0)
            {
                sb.AppendLine("**Notes**");
                checkList.Notes.ForEach(note =>
                {
                    sb.AppendLine(note);
                });
            }
            else
            {
                sb.AppendLine("No notes have been added");
            }

            var embedBuilder = new EmbedBuilder();
            embedBuilder.Title = $"{recruiter.Recruit.DisplayName}";
            embedBuilder.Description = $"{recruiter.Recruit.DisplayName} was recruited by {recruiter.DisplayName}";
            embedBuilder.AddField("Checklist", sb.ToString());
            if (checkList.JoinedPost && checkList.CreatedPost && totalDays > 30)
                embedBuilder.WithColor(Color.Green);
            else if (checkList.JoinedPost == false && checkList.CreatedPost == false && totalDays < 30)
                embedBuilder.WithColor(Color.Green);
            else if (checkList.JoinedPost || checkList.CreatedPost && totalDays < 30)
                embedBuilder.WithColor(Color.Orange);
            else if (checkList.WarningCount >= 3)
                embedBuilder.WithColor(Color.Red);
            else
                embedBuilder.WithColor(Color.Blue);

            return embedBuilder;
        }

        [Command("recruit passed")]
        [RequireUserPermission(Discord.GuildPermission.KickMembers)]
        public async Task RecruitPassedProbation([Remainder] string args)
        {
            IGuildUser recruit = await GetTaggedRecruit(args);
            if (recruit == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find a recruit record or you may have issued an invalid command ensure it is as follows.  ;recruit warning @Recruit").Build());
                return;
            }


            Recruiter existingRecruiter = await _hiringService.FindRecruit(recruit.Id);
            if (existingRecruiter == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply($"{recruit.DisplayName} has not been recruited by anybody").Build());
                return;
            }

            if (existingRecruiter.Recruit.RecruitChecklist.CreatedPost == false ||
                 existingRecruiter.Recruit.RecruitChecklist.JoinedPost == false ||
                 DateTime.UtcNow.Subtract(existingRecruiter.Recruit.RecruitChecklist.DateTimeJoined).TotalDays < 30)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply($"{recruit.DisplayName} has not completed all their tasks").Build());
                return;
            }
            bool result = await _hiringService.UpdateProbationPassed(existingRecruiter.Recruit);
            if (result)
            {
                await _guardianService.IncreaseGlimmerAsync(existingRecruiter.UserId, existingRecruiter.DisplayName, 50);
                await _guardianService.IncreaseGlimmerAsync(existingRecruiter.Recruit.Id, existingRecruiter.Recruit.DisplayName, 50);
                await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"{existingRecruiter.Recruit.DisplayName} has passed their probation! {existingRecruiter.Recruit.DisplayName} has been awarded 50 Glimmer and {existingRecruiter.DisplayName} has earned 50 Glimmer for recruiting this player.").Build());
                return;
            }
            else
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply($"Something went wrong.. blame Jonny").Build());
            }
        }

        [Command("recruit warn")]
        [Alias("recruit warning")]
        [RequireUserPermission(Discord.GuildPermission.KickMembers)]
        public async Task RecruitWarning([Remainder] string args)
        {
            IGuildUser recruit = await GetTaggedRecruit(args);
            if (recruit == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find a recruit record or you may have issued an invalid command ensure it is as follows.  ;recruit warning @Recruit").Build());
                return;
            }


            Recruiter existingRecruiter = await _hiringService.FindRecruit(recruit.Id);
            if (existingRecruiter == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply($"{recruit.DisplayName} has not been recruited by anybody").Build());
                return;
            }

            existingRecruiter.Recruit.RecruitChecklist.WarningCount += 1;
            bool result = await _hiringService.UpdateRecruit(existingRecruiter.Recruit);
            if(result)
            {
                await RespondAsync(embed: EmbedHelper.CreateSuccessReply("Increased warning count successfully").Build());
                return;
            }
            else
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply($"Something went wrong.. blame Jonny").Build());
            }
        }

        [Command("recruit note")]
        [RequireUserPermission(Discord.GuildPermission.KickMembers)]
        public async Task RecruitNote([Remainder] string args)
        {
            IGuildUser recruit = await GetTaggedRecruit(args);
            if (recruit == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find a recruit record or you may have issued an invalid command ensure it is as follows.  ;recruit warning @Recruit").Build());
                return;
            }

            string note = args.Replace($"<@{recruit.Id}>", string.Empty).Trim();
            if(string.IsNullOrWhiteSpace(note))
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Supply a note about a recruit").Build());
                return;
            }

            Recruiter existingRecruiter = await _hiringService.FindRecruit(recruit.Id);
            if (existingRecruiter == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply($"{recruit.DisplayName} has not been recruited by anybody").Build());
                return;
            }

            existingRecruiter.Recruit.RecruitChecklist.Notes.Add($"Added By {Context.User.Username} at {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}\n{note}");
            bool result = await _hiringService.UpdateRecruit(existingRecruiter.Recruit);
            if (result)
            {
                await RespondAsync(embed: EmbedHelper.CreateSuccessReply("Added note successfully").Build());
                return;
            }
            else
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply($"Something went wrong.. blame Jonny").Build());
            }
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
            await RespondAsync(embed: embedBuilder.Build());
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
            await RespondAsync(embed: embedBuilder.Build());
        }

        [Command("testmethod")]
        [RequireUserPermission(Discord.GuildPermission.KickMembers)]
        public async Task TestMethod()
        {
            bool result = await _activityService.CreatedRaidThisWeek(850688118113173515);
            int breakHere = 0;
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
                    IGuildUser guildUser = await Context.Guild.GetUserAsync(userId);
                    if (guildUser != null && guildUser.IsBot == false)
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
                    IGuildUser guildUser = await Context.Guild.GetUserAsync(userId);
                    if (guildUser != null && guildUser.IsBot == false)
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
