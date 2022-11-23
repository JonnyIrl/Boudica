using Boudica.Helpers;
using Boudica.MongoDB.Models;
using Boudica.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Boudica.Commands
{
    [DefaultMemberPermissions(GuildPermission.ModerateMembers)]
    [Group("recruit", "Commands for recruits")]
    public class AdminCommands: InteractionModuleBase<SocketInteractionContext>
    {
        private readonly ActivityService _activityService;
        private readonly HiringService _hiringService;
        private readonly GuardianService _guardianService;
        //private readonly APIService _apIService;

        private readonly Emoji _successEmoji;
        private readonly Emoji _failureEmoji;

        private const int CreatorPoints = 5;
        public AdminCommands(IServiceProvider services)
        {
            _activityService = services.GetRequiredService<ActivityService>();
            _hiringService = services.GetRequiredService<HiringService>();
            _guardianService = services.GetRequiredService<GuardianService>();
            //_apIService = services.GetRequiredService<APIService>();

            _successEmoji = new Emoji("✅");
            _failureEmoji = new Emoji("❌");
        }

        [SlashCommand("help", "To find all the commands related to recruiting")]
        public async Task RecruitHelp()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("**New Recruit**  /recruit new @Recruiter @NewJoiner");
            sb.AppendLine("**Recruit Progress** /recruit progress @NewJoiner");
            sb.AppendLine("**All Recruit Progress**  /recruit progress-all");
            sb.AppendLine("**Add Warning to Recruit**  /recruit warn @NewJoiner");
            sb.AppendLine("**Add Note to Recruit**  /recruit note @NewJoiner This player posted an offensive gif");

            await RespondAsync(embed: EmbedHelper.CreateSuccessReply(sb.ToString()).Build());
        }

        [SlashCommand("new", "Create a new recruit")]
        public async Task CreateNewRecruit(SocketGuildUser recruiter, SocketGuildUser newJoiner)
        {
            if(recruiter == null || recruiter.IsBot || newJoiner == null || newJoiner.IsBot)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command, only supply the recruiter and the recruit i.e. /recruit new @Recruiter @NewPerson").Build());
                return;
            }
            List<IGuildUser> guildUsers = new List<IGuildUser>()
            {
                recruiter,
                newJoiner,
            };

            if(guildUsers == null || guildUsers.Count != 2)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Invalid command, only supply the recruiter and the recruit i.e. /recruit new @Recruiter @NewPerson").Build());
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

            await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"Created successfully. {newRecruiter.Recruit.DisplayName} has been recruited by {newRecruiter.DisplayName}. You can check their progress by using the /recruit progress <@{newRecruiter.Recruit.Id}>").Build());
        }

        [SlashCommand("progress", "Check a recruits progress")]
        public async Task RecruitProgress(SocketGuildUser recruit)
        {
            if (recruit == null || recruit.IsBot)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find a recruit record or you may have issued an invalid command ensure it is as follows.  /recruit progress @Recruit").Build());
                return;
            }


            Recruiter existingRecruiter = await _hiringService.FindRecruit(recruit.Id);
            if (existingRecruiter == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply($"{recruit.DisplayName} has not been recruited by anybody").Build());
                return;
            }

            if (existingRecruiter.Recruit.RecruitChecklist.JoinedPost == false)
            {
                if (await _activityService.RecruitJoinedRaid(existingRecruiter.Recruit.Id) || await _activityService.RecruitJoinedFireteam(existingRecruiter.Recruit.Id))
                {
                    existingRecruiter.Recruit.RecruitChecklist.JoinedPost = true;
                    await _hiringService.UpdateRecruit(existingRecruiter.Recruit);
                }
            }

            EmbedBuilder embedBuilder = CreateEmbedForRecruit(existingRecruiter);
            await RespondAsync(embed: embedBuilder.Build());
        }

        [SlashCommand("progress-all", "All recruits progress")]
        public async Task AllRecruitProgress()
        {
            await DeferAsync();
            List<Recruiter> allRecruiters = await _hiringService.FindAllRecruits(Context.Guild.Id);
            foreach(Recruiter recruiter in allRecruiters)
            {
                if (recruiter.Recruit.RecruitChecklist.JoinedPost) continue;
                if(await _activityService.RecruitJoinedRaid(recruiter.Recruit.Id) || await _activityService.RecruitJoinedFireteam(recruiter.Recruit.Id))
                {
                    recruiter.Recruit.RecruitChecklist.JoinedPost = true;
                    await _hiringService.UpdateRecruit(recruiter.Recruit);
                }    
            }

            if (allRecruiters == null || allRecruiters.Count == 0)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply($"There are currently no recruits").Build());
                return;
            }

            foreach (Recruiter recruiter in allRecruiters)
            {
                EmbedBuilder embedBuilder = CreateEmbedForRecruit(recruiter);
                await ReplyAsync(embed: embedBuilder.Build());
                await Task.Delay(150);
            }

            await RespondAsync("Success", ephemeral: true);
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

        [SlashCommand("enroll", "Enroll a recruit as full member")] 
        public async Task RecruitPassedProbation(SocketGuildUser recruit)
        {
            if (recruit == null || recruit.IsBot)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find a recruit record or you may have issued an invalid command ensure it is as follows.  /recruit enroll @Recruit").Build());
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

        [SlashCommand("warn", "Increase the warning count for a Recruit")]
        public async Task RecruitWarning(SocketGuildUser recruit)
        {
            if (recruit == null || recruit.IsBot)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find a recruit record or you may have issued an invalid command ensure it is as follows.  /recruit warn @Recruit").Build());
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

        [SlashCommand("note", "Add a note against a Recruit")]
        public async Task RecruitNote(SocketGuildUser recruit, string note)
        {
            if (recruit == null || recruit.IsBot)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find a recruit record or you may have issued an invalid command ensure it is as follows.  /recruit note @Recruit").Build());
                return;
            }

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

        [DefaultMemberPermissions(GuildPermission.KickMembers)]
        [SlashCommand("delete", "Delete a recruit.")]
        public async Task DeleteRecruit(SocketGuildUser recruit)
        {
            if (recruit == null || recruit.IsBot)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find a recruit record or you may have issued an invalid command ensure it is as follows.  /recruit delete @Recruit").Build());
                return;
            }

            Recruiter existingRecruiter = await _hiringService.FindRecruit(recruit.Id);
            if (existingRecruiter == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply($"{recruit.DisplayName} has not been recruited by anybody").Build());
                return;
            }

            
            bool result = await _hiringService.DeleteRecruit(existingRecruiter.Recruit);
            if (result)
            {
                await RespondAsync(embed: EmbedHelper.CreateSuccessReply("Deleted successfully").Build());
                return;
            }
            else
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply($"Could not delete.. something went wrong.. blame Jonny").Build());
            }
        }

        [SlashCommand("testapi", "jonny test")]
        public async Task TestAPI()
        {
            await RespondAsync("yes", ephemeral: true);
            //await _apIService.Test();
        }

        [SlashCommand("link", "Link your Bungie account to your Discord account.")]
        public async Task Link()
        {
            //await DeferAsync();
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by JonnyIrl"
            };
            var auth = new EmbedAuthorBuilder()
            {
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl(),
                Name = "Account Linking"
            };
            var embed = new EmbedBuilder()
            {
                Color = Color.Green,
                Footer = foot,
                Author = auth
            };
            var plainTextBytes = Encoding.UTF8.GetBytes($"{Context.User.Id}");
            string state = Convert.ToBase64String(plainTextBytes);

            string clientId = "42024";

            embed.Title = $"Click here to start the linking process.";
            embed.Url = $"https://www.bungie.net/en/OAuth/Authorize?client_id={clientId}&response_type=code&state={state}";
            embed.Description = $"- Linking allows you to start participate in daily/weekly challenges and more. It will be required to participate in the new Lightfall clan event\n" +
                $"- After linking is complete, you'll receive another DM from me to confirm.\n" +
                $"- Experienced a name change? Relinking will update your name with our data.";

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Link with Boudica", style: ButtonStyle.Link, url: $"https://www.bungie.net/en/OAuth/Authorize?client_id={clientId}&response_type=code&state={state}", row: 0);

            await RespondAsync(embed: embed.Build(), components: buttonBuilder.Build(), ephemeral: true);
        }


    }
}
