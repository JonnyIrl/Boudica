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
    public class BettingPollCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly BetPollService _betPollService;
        private readonly GuardianService _guardianService;
        public BettingPollCommands(IServiceProvider services)
        {
            _betPollService = services.GetRequiredService<BetPollService>();
            _guardianService = services.GetRequiredService<GuardianService>();
        }

        [DefaultMemberPermissions(GuildPermission.ModerateMembers)]
        [SlashCommand("create-bet", "Creates a Bet Poll that will award glimmer to winners")]
        public async Task CreatePoll(
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
            BetPoll createdPoll = await _betPollService.CreateBetPollAsync(CreateBetPoll(question, validOptions, pollOptions));
            List<Emoji> emojiOptions = GetEmojiListOfOptions(validOptions.Count);
            EmbedBuilder embedBuilder = CreatePollEmbed(question, validOptions, emojiOptions, createdPoll.Id);

            await RespondAsync(embed: embedBuilder.Build());
            IUserMessage message = await GetOriginalResponseAsync();
            if (message == null)
            {
                await FollowupAsync("Could not find message.. your poll will not work correctly, please delete it and try again");
                return;
            }
            await _betPollService.UpdateBetPollMessageId(createdPoll.Id, message.Id);
        }

        [SlashCommand("bet-vote", "Vote on an open Poll")]
        public async Task PollVote([Summary("pollId", "The Id of the poll")] long pollId, 
            PollOption option, 
            [Summary("betAmount", "The amount of glimmer to bet, you will get double for winning")] int betAmount)
        {
            if(betAmount <= 0)
            {
                await RespondAsync("You have to bet an amount greater than 0", ephemeral: true);
                return;
            }
            BetPoll existingPoll = await _betPollService.GetBetPoll(pollId);
            if (existingPoll == null)
            {
                await RespondAsync("Could not find poll to vote on", ephemeral: true);
                return;
            }
            if (existingPoll.IsLocked)
            {
                await RespondAsync("This poll is locked", ephemeral: true);
                return;
            }
            if (existingPoll.IsClosed)
            {
                await RespondAsync("This poll is closed", ephemeral: true);
                return;
            }
            if (existingPoll.CreatedOptions.FirstOrDefault(x => x.PollOption == option) == null)
            {
                await RespondAsync("This is not a valid option to vote on", ephemeral: true);
                return;
            }
            PlayerPollVote existingVote = existingPoll.Votes.FirstOrDefault(x => x.Id == Context.User.Id);
            if (existingVote != null)
            {
                await RespondAsync($"You have already voted on this poll for {(existingPoll.CreatedOptions[(int)existingVote.VotedPollOption]).ToString()}");
                return;
            }
            int guardianGlimmer = await _guardianService.GetGuardianGlimmer(Context.User.Id);
            if(guardianGlimmer < betAmount)
            {
                await RespondAsync($"You do not have enough glimmer to make this bet, your max glimmer is {guardianGlimmer}", ephemeral: true);
                return;
            }

            Result result = await _betPollService.AddPlayerBetPollVote(Context.User.Id, Context.User.Username, pollId, option, betAmount);
            if (result.Success == false)
                await RespondAsync(result.Message, ephemeral: true);
            else
                //await RespondAsync($"Your vote for {existingPoll.CreatedOptions[(int)option].DisplayText} has been counted for {betAmount} Glimmer! If you win, you will get {(betAmount * 2)} Glimmer. Best of luck!");
                await RespondAsync($"Your vote has been counted. Best of luck!");

                await _guardianService.RemoveGlimmerAsync(Context.User.Id, (betAmount));
        }

        [DefaultMemberPermissions(GuildPermission.ModerateMembers)]
        [SlashCommand("lock-bet", "Lock a Betting Poll to prevent further votes")]
        public async Task LockPoll([Summary("pollId", "The Id of the poll")] long pollId)
        {
            BetPoll existingPoll = await _betPollService.GetBetPoll(pollId);
            if (existingPoll == null)
            {
                await RespondAsync("Could not find poll to lock", ephemeral: true);
                return;
            }
            if (existingPoll.IsLocked)
            {
                await RespondAsync("This poll is already locked", ephemeral: true);
            }
            if (existingPoll.IsClosed)
            {
                await RespondAsync("This poll is already closed", ephemeral: true);
            }

            await _betPollService.LockBetPoll(existingPoll.Id);
            await RespondAsync(text: "Bet Poll has been locked");

            ITextChannel channel = (ITextChannel)Context.Guild.GetChannel(existingPoll.ChannelId);
            if (channel == null) return;
            IUserMessage message = (IUserMessage)await channel.GetMessageAsync(existingPoll.MessageId);
            if (message == null) return;
            IEmbed embed = message.Embeds.FirstOrDefault();
            if (embed == null) return;
            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = "This Poll is now locked - " + embed.Title;
            builder.Color = Color.Orange;
            builder.Description = embed.Description;
            foreach (EmbedField field in embed.Fields)
                builder.Fields.Add(new EmbedFieldBuilder() { Name = field.Name, Value = field.Value });
            if (embed.Footer != null)
                builder.Footer = new EmbedFooterBuilder() { Text = embed.Footer.Value.ToString() };
            await message.ModifyAsync(x =>
            {
                x.Embed = builder.Build();
            });
        }


        [DefaultMemberPermissions(GuildPermission.ModerateMembers)]
        [SlashCommand("close-bet", "Close a Betting Poll")]
        public async Task ClosePoll([Summary("pollId", "The Id of the poll")] long pollId, PollOption winningOption)
        {
            BetPoll existingPoll = await _betPollService.GetBetPoll(pollId);
            if (existingPoll == null)
            {
                await RespondAsync("Could not find poll to close", ephemeral: true);
                return;
            }
            if (existingPoll.IsClosed)
            {
                await RespondAsync("This poll is already closed", ephemeral: true);
                return;
            }

            if(existingPoll.CreatedOptions.FirstOrDefault(x => x.PollOption == winningOption) == null)
            {
                await RespondAsync("This is not a valid option to win", ephemeral: true);
                return;
            }

            existingPoll.IsClosed = true;
            EmbedBuilder embedBuilder = await CreateClosedPollEmbed(existingPoll, winningOption);
            existingPoll.WinningOption = winningOption;
            await _betPollService.CloseBetPoll(existingPoll);
            await RespondAsync(embed: embedBuilder.Build());

            ITextChannel channel = (ITextChannel)Context.Guild.GetChannel(existingPoll.ChannelId);
            if (channel == null) return;
            IUserMessage message = (IUserMessage)await channel.GetMessageAsync(existingPoll.MessageId);
            if (message == null) return;
            IEmbed embed = message.Embeds.FirstOrDefault();
            if (embed == null) return;
            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = "This Poll is now closed - " + existingPoll.Question;
            builder.Color = Color.Red;
            builder.Description = embed.Description;
            foreach (EmbedField field in embed.Fields)
                builder.Fields.Add(new EmbedFieldBuilder() { Name = field.Name, Value = field.Value });
            if (embed.Footer != null)
                builder.Footer = new EmbedFooterBuilder() { Text = embed.Footer.Value.ToString() };
            await message.ModifyAsync(x =>
            {
                x.Embed = builder.Build();
            });
        }

        private async Task<EmbedBuilder> CreateClosedPollEmbed(BetPoll poll, PollOption winningOption)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.Title = $"Results of the {poll.Question} Poll";
            embedBuilder.Description = $"The winning option was **{poll.CreatedOptions.First(x => x.PollOption == winningOption).DisplayText}**";
            embedBuilder.Color = Color.Green;
            List<PlayerPollVote> winningPlayerPollVotes = poll.Votes.Where(x => x.VotedPollOption == winningOption).ToList();
            List<PlayerPollVote> losingPlayerPollVotes = poll.Votes.Where(x => x.VotedPollOption != winningOption).ToList();
            StringBuilder winnersStringBuilder = new StringBuilder();
            StringBuilder losersStringBuilder = new StringBuilder();
            if (winningPlayerPollVotes.Count == 0)
                winnersStringBuilder.AppendLine("Nobody won.. unlucky!");
            else
            {
                foreach(PlayerPollVote winner in winningPlayerPollVotes)
                {
                    await _guardianService.IncreaseGlimmerAsync(winner.Id, winner.Username, (winner.BetAmount * 2));
                    winnersStringBuilder.AppendLine($"{winner.Username}, you have won {(winner.BetAmount * 2)} Glimmer");
                }
            }

            if (losingPlayerPollVotes.Count == 0)
                losersStringBuilder.AppendLine("Nobody lost!");
            else
            {
                foreach (PlayerPollVote loser in losingPlayerPollVotes)
                {
                    //Glimmer already taken off the user so no need to do it twice
                    losersStringBuilder.AppendLine($"{loser.Username}, you have lost {(loser.BetAmount)} Glimmer");
                }
            }
            
            embedBuilder.AddField("Winners", winnersStringBuilder.ToString());
            embedBuilder.AddField("Losers", losersStringBuilder.ToString());

            return embedBuilder;
        }

        private BetPoll CreateBetPoll(string question, List<string> validOptions, List<PollOption> pollOptions)
        {
            BetPoll poll = new BetPoll();
            poll.CreatedOptions = new List<CreatedPollOption>();
            for (int i = 0; i < validOptions.Count; i++)
            {
                poll.CreatedOptions.Add(new CreatedPollOption(validOptions[i], pollOptions[i]));
            }
            poll.Question = question;
            poll.Votes = new List<PlayerPollVote>();
            poll.GuildId = Context.Guild.Id;
            poll.ChannelId = Context.Channel.Id;
            return poll;
        }

        private EmbedBuilder CreatePollEmbed(string question, List<string> validOptions, List<Emoji> emojis, long pollId)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.Title = question;
            embedBuilder.Description = $"Use **/bet-vote** with an Id of {pollId} and pick your answer from the list, along with your glimmer bet to vote!";
            embedBuilder.AddField("Options", GetEmbedOptions(validOptions, emojis));
            embedBuilder.Footer = new EmbedFooterBuilder() { Text = $"Poll Id {pollId}\nUse /lock-bet with the PollId {pollId} to close this poll" };
            embedBuilder.Color = Color.Blue;
            return embedBuilder;
        }

        private string GetEmbedOptions(List<string> validOptions, List<Emoji> emojis)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < validOptions.Count; i++)
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
