using Boudica.Enums;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Commands
{
    public class ExampleCommands : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService Commands { get; set; }
        private CommandHandler _handler;

        public ExampleCommands(CommandHandler handler)
        {
            _handler = handler;
        }


        [Command("hello")]
        public async Task HelloCommand()
        {
            // initialize empty string builder for reply
            var sb = new StringBuilder();

            // get user info from the Context
            var user = Context.User;

            // build out the reply
            sb.AppendLine($"You are -> []");
            sb.AppendLine("I must now say, World!");

            // send simple string reply
            await ReplyAsync(sb.ToString());
        }

        [SlashCommand("8ball", "find your answer!")]
        public async Task AskEightBall(string question)
        {
            // now to create a list of possible replies
            var replies = new List<string>();
            // add our possible replies
            replies.Add("yes");
            replies.Add("no");
            replies.Add("maybe");
            replies.Add("hazzzzy....");

            // get the answer
            var answer = replies[new Random().Next(replies.Count - 1)];

            // reply with the answer
            await RespondAsync($"You asked: **{question}**, and your answer is: **{answer}**");
            IUserMessage message = await GetOriginalResponseAsync();
            if (message != null)
            {
                await message.ModifyAsync(x => x.Content = "This is a modified text");
            }
        }


        [SlashCommand("create", "Select your activity")]
        public async Task TestSlashCommand(Enums.ActivityType activityType, RaidName raidName, DateTime when, string description)
        {
            switch(activityType)
            {
                case Enums.ActivityType.Fireteam:
                    break;
                case Enums.ActivityType.Raid:
                     await Test2(raidName, when, description);
                    break;
            }

            await RespondAsync();
            
        }

        [SlashCommand("raid", "raidraid")]
        private async Task Test2(RaidName raidName, DateTime when, string description)
        {
            RaidName selectedRaidName = raidName;
            DateTime raidDateTime = when;
            string result = description;

            int breakHere = 0;
        }
    }
}
