using Boudica.Classes;
using Boudica.Enums;
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
    public class PollCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly PollService _pollService;
        private readonly GuardianService _guardianService;
        public PollCommands(IServiceProvider services)
        {
            _pollService = services.GetRequiredService<PollService>();
            _guardianService = services.GetRequiredService<GuardianService>();
        }

        [DefaultMemberPermissions(GuildPermission.ModerateMembers)]
        [SlashCommand("create-poll", "Creates a Poll that will award glimmer to winners")]
        public async Task CreatePoll(
            [Summary("entryGlimmerAmount", "The amount of glimmer each entry will get")]
            int entryGlimmerAmount,
            [Summary("winningGlimmerAmount", "The amount of glimmer each winner will get")]
            int winningGlimmerAmount,
            [Summary("question", "The question that will be displayed for people to vote on")] 
            string question,
            string option1,
            string option2,
            string option3 = null,
            string option4 = null,
            string option5 = null,
            string option6 = null,
            string option7 = null,
            string option8 = null,
            string option9 = null,
            string option10 = null,
            string option11 = null,
            string option12 = null,
            string option13 = null,
            string option14 = null,
            string option15 = null,
            string option16 = null,
            string option17 = null,
            string option18 = null,
            string option19 = null,
            string option20 = null
            )
        {
            if (IsPollValid(question, option1, option2) == false)
            {
                await RespondAsync("Invalid poll. You must give a question and 2 options minimum");
                return;
            }
            List<string> validOptions = GetValidOptionsList(option1, option2, option3, option4, option5, option6, option7, option8, option9, option10, option11, option12, option13, option14, option15, option16, option17, option18, option19, option20);
            List<PollOption> pollOptions = GetPollOptions(validOptions.Count);
            Poll createdPoll = await _pollService.CreatePollAsync(CreatePoll(question, validOptions, pollOptions, entryGlimmerAmount, winningGlimmerAmount));
            List<Emoji> emojiOptions = GetEmojiListOfOptions(validOptions.Count);
            EmbedBuilder embedBuilder = CreatePollEmbed(question, validOptions, emojiOptions, createdPoll.Id);

            await RespondAsync(embed: embedBuilder.Build());
            IUserMessage message = await GetOriginalResponseAsync();
            if(message == null)
            {
                await FollowupAsync("Could not find message.. your poll will not work correctly, please delete it and try again");
                return;
            }
            await _pollService.UpdatePollMessageId(createdPoll.Id, message.Id);
        }

        [SlashCommand("poll-vote", "Vote on an open Poll")]
        public async Task PollVote([Summary("pollId", "The Id of the poll")]long pollId, PollOption option)
        {
            Poll existingPoll = await _pollService.GetPoll(pollId);
            if(existingPoll == null)
            {
                await RespondAsync("Could not find poll to vote on", ephemeral: true);
                return;
            }
            if(existingPoll.IsClosed)
            {
                await RespondAsync("This poll is closed", ephemeral: true);
                return;
            }
            if(existingPoll.CreatedOptions.FirstOrDefault(x => x.PollOption == option) == null)
            {
                await RespondAsync("This is not a valid option to vote on", ephemeral: true);
                return;
            }
            PlayerPollVote existingVote = existingPoll.Votes.FirstOrDefault(x => x.Id == Context.User.Id);
            if (existingVote != null)
            {
                await RespondAsync($"You have already voted on this poll for {existingPoll.CreatedOptions[(int)existingVote.VotedPollOption]}");
                return;
            }
            Result result = await _pollService.AddPlayerPollVote(Context.User.Id, Context.User.Username, pollId, option);
            if (result.Success == false)
                await RespondAsync(result.Message, ephemeral: true);
            else
                await RespondAsync($"You vote for {existingPoll.CreatedOptions[(int)option].DisplayText} has been counted!", ephemeral: true);
        }

        [DefaultMemberPermissions(GuildPermission.ModerateMembers)]
        [SlashCommand("close-poll", "Close an open Poll")]
        public async Task ClosePoll([Summary("pollId", "The Id of the poll")] long pollId)
        {
            Poll existingPoll = await _pollService.GetPoll(pollId);
            if (existingPoll == null)
            {
                await RespondAsync("Could not find poll to close", ephemeral: true);
                return;
            }
            if (existingPoll.IsClosed)
            {
                await RespondAsync("This poll is already closed", ephemeral: true);
            }

            existingPoll.IsClosed = true;
            EmbedBuilder embedBuilder = CreateClosedPollEmbed(ref existingPoll);
            await _pollService.ClosePoll(existingPoll);
            await RespondAsync(embed: embedBuilder.Build());

            ITextChannel channel = (ITextChannel) Context.Guild.GetChannel(existingPoll.ChannelId);
            if (channel == null) return;
            IUserMessage message = (IUserMessage) await channel.GetMessageAsync(existingPoll.MessageId);
            if(message == null) return;
            IEmbed embed = message.Embeds.FirstOrDefault();
            if (embed == null) return;
            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = "This Poll is now closed - " + embed.Title;
            builder.Color = Color.Red;
            builder.Description = embed.Description;
            foreach(EmbedField field in embed.Fields)
                builder.Fields.Add(new EmbedFieldBuilder() { Name = field.Name, Value = field.Value });
            if(embed.Footer != null)
                builder.Footer = new EmbedFooterBuilder() { Text = embed.Footer.Value.ToString() };
            await message.ModifyAsync(x =>
            {
                x.Embed = builder.Build();
            });
        }

        private EmbedBuilder CreateClosedPollEmbed(ref Poll poll)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.Title = $"Results of the **{poll.Question}** Poll";
            embedBuilder.Color = Color.Green;
            var groupedResults = poll.Votes.GroupBy(x => x.VotedPollOption).OrderByDescending(x => x.Count());
            StringBuilder sb = new StringBuilder();

            int lastGroupResult = groupedResults.First().Count();
            List<PollOption> drawingOptions = new List<PollOption>();
            foreach(var groupResult in groupedResults.Skip(1))
            {
                if (groupResult.Count() == lastGroupResult)
                {
                    drawingOptions.Add(groupResult.First().VotedPollOption);
                    //drawStringBuilder.Append($"{poll.CreatedOptions[(int)groupResult.First().VotedPollOption]}");
                }
                else break;
            }
            //There was a draw
            if(drawingOptions.Count > 0)
            {
                sb.Append("There was a draw between the following choices:\n");
                drawingOptions.Add(groupedResults.First().First().VotedPollOption);
                List<string> results = new List<string>();
                drawingOptions = drawingOptions.OrderBy(x => x).ToList();
                foreach(PollOption option in drawingOptions)
                {
                    results.Add(poll.CreatedOptions[(int)option].DisplayText);
                }
                string drawString = string.Join(", ", results);
                sb.AppendLine(drawString);
                sb.AppendLine("");
                sb.AppendLine($"The following players have all been awarded {poll.WinnerGlimmerAmount} Glimmer!");
                List<PlayerPollVote> winningPlayerPollVotes = poll.Votes.Where(x => drawingOptions.Contains(x.VotedPollOption) != false).ToList();
                sb.AppendLine(string.Join(", ", winningPlayerPollVotes.Select(x => x.Username).ToList()));
                sb.AppendLine("");
                sb.AppendLine($"Everyone else has been awarded {poll.EntryGlimmerAmount} Glimmer!");
                embedBuilder.Description += sb.ToString();
                poll.WinningOptions = drawingOptions;
            }
            //One winning option
            else
            {
                sb.AppendLine($"The following players have all been awarded {poll.WinnerGlimmerAmount} Glimmer for winning the Poll!");
                List<PlayerPollVote> winningPlayerPollVotes = poll.Votes.Where(x => x.VotedPollOption == groupedResults.First().First().VotedPollOption).ToList();
                sb.AppendLine(string.Join(", ", winningPlayerPollVotes.Select(x => x.Username).ToList()));
                sb.AppendLine("");
                sb.AppendLine($"Everyone else has been awarded {poll.EntryGlimmerAmount} Glimmer!");
                embedBuilder.Description += sb.ToString();
                poll.WinningOptions = new List<PollOption>();
                poll.WinningOptions.Add(groupedResults.First().First().VotedPollOption);
            }
            StringBuilder breakdownStringBuilder = new StringBuilder();
            foreach (CreatedPollOption pollOption in poll.CreatedOptions)
            {
                var matchingGroupResult = groupedResults.FirstOrDefault(x => x.FirstOrDefault()?.VotedPollOption == pollOption.PollOption);
                if(matchingGroupResult != null)
                {
                    breakdownStringBuilder.AppendLine($"{pollOption.DisplayText} had a total of {matchingGroupResult.Count()} votes");
                }
                else
                {
                    breakdownStringBuilder.AppendLine($"{pollOption.DisplayText} had a total of 0 votes");
                }
            }
            embedBuilder.AddField("Vote Breakdown", breakdownStringBuilder.ToString());
            return embedBuilder;
        }

        private Poll CreatePoll(string question, List<string> validOptions, List<PollOption> pollOptions, int entryCount, int winningCount)
        {
            Poll poll = new Poll();
            poll.CreatedOptions = new List<CreatedPollOption>();
            for(int i = 0; i < validOptions.Count; i++)
            {
                poll.CreatedOptions.Add(new CreatedPollOption(validOptions[i], pollOptions[i]));
            }
            poll.Question = question;
            poll.Votes = new List<PlayerPollVote>();
            poll.GuildId = Context.Guild.Id;
            poll.ChannelId = Context.Channel.Id;
            poll.EntryGlimmerAmount = entryCount;
            poll.WinnerGlimmerAmount = winningCount;
            return poll;
        }

        private EmbedBuilder CreatePollEmbed(string question, List<string> validOptions, List<Emoji> emojis, long pollId)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.Title = question;
            embedBuilder.Description = $"Use **/poll-vote** with an Id of {pollId} and pick your answer from the list to vote!";
            embedBuilder.AddField("Options", GetEmbedOptions(validOptions, emojis));
            embedBuilder.Footer = new EmbedFooterBuilder() { Text = $"Poll Id {pollId}\nUse /close-poll with the PollId {pollId} to close this poll" };
            embedBuilder.Color = Color.Blue;
            return embedBuilder;
        }

        private string GetEmbedOptions(List<string> validOptions, List<Emoji> emojis)
        {
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < validOptions.Count; i++)
            {
                sb.AppendLine($"{emojis[i]} - {validOptions[i]}");
            }
            return sb.ToString();
        }

        private bool IsPollValid(string question, string option1, string option2)
        {
            return string.IsNullOrEmpty(question) == false && string.IsNullOrEmpty(option1) == false && string.IsNullOrEmpty(option2) == false;
        }

        private List<Emoji> GetEmojiListOfOptions(int count)
        {
            List<Emoji> alphabetList = new List<Emoji>();
            alphabetList = new List<Emoji>();
            alphabetList.Add(new Emoji("🇦"));
            alphabetList.Add(new Emoji("🇧"));
            alphabetList.Add(new Emoji("🇨"));
            alphabetList.Add(new Emoji("🇩"));
            //alphabetList.Add(new Emoji("🇪"));
            //alphabetList.Add(new Emoji("🇫"));
            //alphabetList.Add(new Emoji("🇬"));
            //alphabetList.Add(new Emoji("🇭"));
            //alphabetList.Add(new Emoji("🇮")); 
            //alphabetList.Add(new Emoji("🇯"));
            //alphabetList.Add(new Emoji("🇰"));
            //alphabetList.Add(new Emoji("🇱"));
            //alphabetList.Add(new Emoji("🇲"));
            //alphabetList.Add(new Emoji("🇳"));
            //alphabetList.Add(new Emoji("🇴"));
            //alphabetList.Add(new Emoji("🇵"));
            //alphabetList.Add(new Emoji("🇶"));
            //alphabetList.Add(new Emoji("🇷"));
            //alphabetList.Add(new Emoji("🇸"));
            //alphabetList.Add(new Emoji("🇹"));
            //alphabetList.Add(new Emoji("🇺"));
            //alphabetList.Add(new Emoji("🇻"));
            return alphabetList.Take(count).ToList();
        }

        private List<PollOption> GetPollOptions(int count)
        {
            List<PollOption> pollOptions = new List<PollOption>();
            pollOptions = new List<PollOption>();
            for (int i = 0; i < count; i++)
                pollOptions.Add((PollOption)i);

            return pollOptions;
        }

        private List<string> GetValidOptionsList(
            string option1,
            string option2,
            string option3 = null,
            string option4 = null,
            string option5 = null,
            string option6 = null,
            string option7 = null,
            string option8 = null,
            string option9 = null,
            string option10 = null,
            string option11 = null,
            string option12 = null,
            string option13 = null,
            string option14 = null,
            string option15 = null,
            string option16 = null,
            string option17 = null,
            string option18 = null,
            string option19 = null,
            string option20 = null)
        {
            List<string> validOptions = new List<string>();
            validOptions.Add(option1);
            validOptions.Add(option2);

            if (string.IsNullOrEmpty(option3) == false)
                validOptions.Add(option3);
            if (string.IsNullOrEmpty(option4) == false)
                validOptions.Add(option4);

            //if (string.IsNullOrEmpty(option5) == false)
            //    validOptions.Add(option5);
            //if (string.IsNullOrEmpty(option6) == false)
            //    validOptions.Add(option6);
            //if (string.IsNullOrEmpty(option7) == false)
            //    validOptions.Add(option7);
            //if (string.IsNullOrEmpty(option8) == false)
            //    validOptions.Add(option8);
            //if (string.IsNullOrEmpty(option9) == false)
            //    validOptions.Add(option9);
            //if (string.IsNullOrEmpty(option10) == false)
            //    validOptions.Add(option10);
            //if (string.IsNullOrEmpty(option11) == false)
            //    validOptions.Add(option11);
            //if (string.IsNullOrEmpty(option12) == false)
            //    validOptions.Add(option12);
            //if (string.IsNullOrEmpty(option13) == false)
            //    validOptions.Add(option13);
            //if (string.IsNullOrEmpty(option14) == false)
            //    validOptions.Add(option14);
            //if (string.IsNullOrEmpty(option15) == false)
            //    validOptions.Add(option15);
            //if (string.IsNullOrEmpty(option16) == false)
            //    validOptions.Add(option16);
            //if (string.IsNullOrEmpty(option17) == false)
            //    validOptions.Add(option17);
            //if (string.IsNullOrEmpty(option18) == false)
            //    validOptions.Add(option18);
            //if (string.IsNullOrEmpty(option19) == false)
            //    validOptions.Add(option19);
            //if (string.IsNullOrEmpty(option20) == false)
            //    validOptions.Add(option20);

            return validOptions;
        }
    }
}
