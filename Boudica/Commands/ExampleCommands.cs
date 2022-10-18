using Boudica.Enums;
using Discord;
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

        [SlashCommand("ask", "Ask a question to find your answer!")]
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
        }


    }
}
