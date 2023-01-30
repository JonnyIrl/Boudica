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
        public PollCommands(IServiceProvider services)
        {
            _pollService = services.GetRequiredService<PollService>();
        }

        [SlashCommand("create-poll", "Creates a Poll that will award glimmer to winners")]
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
            alphabetList.Add(new Emoji("🇪"));
            alphabetList.Add(new Emoji("🇫"));
            alphabetList.Add(new Emoji("🇬"));
            alphabetList.Add(new Emoji("🇭"));
            alphabetList.Add(new Emoji("🇮"));
            alphabetList.Add(new Emoji("🇰"));
            alphabetList.Add(new Emoji("🇱"));
            alphabetList.Add(new Emoji("🇲"));
            alphabetList.Add(new Emoji("🇳"));
            alphabetList.Add(new Emoji("🇴"));
            alphabetList.Add(new Emoji("🇵"));
            alphabetList.Add(new Emoji("🇶"));
            alphabetList.Add(new Emoji("🇷"));
            alphabetList.Add(new Emoji("🇹"));
            alphabetList.Add(new Emoji("🇺"));
            alphabetList.Add(new Emoji("🇻"));
            alphabetList.Add(new Emoji("🇼"));
            alphabetList.Add(new Emoji("🇽"));
            alphabetList.Add(new Emoji("🇾"));
            alphabetList.Add(new Emoji("🇿"));
            return alphabetList.Take(count).ToList();
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
            if (string.IsNullOrEmpty(option5) == false)
                validOptions.Add(option5);
            if (string.IsNullOrEmpty(option6) == false)
                validOptions.Add(option6);
            if (string.IsNullOrEmpty(option7) == false)
                validOptions.Add(option7);
            if (string.IsNullOrEmpty(option8) == false)
                validOptions.Add(option8);
            if (string.IsNullOrEmpty(option9) == false)
                validOptions.Add(option9);
            if (string.IsNullOrEmpty(option10) == false)
                validOptions.Add(option10);
            if (string.IsNullOrEmpty(option11) == false)
                validOptions.Add(option11);
            if (string.IsNullOrEmpty(option12) == false)
                validOptions.Add(option12);
            if (string.IsNullOrEmpty(option13) == false)
                validOptions.Add(option13);
            if (string.IsNullOrEmpty(option14) == false)
                validOptions.Add(option14);
            if (string.IsNullOrEmpty(option15) == false)
                validOptions.Add(option15);
            if (string.IsNullOrEmpty(option16) == false)
                validOptions.Add(option16);
            if (string.IsNullOrEmpty(option17) == false)
                validOptions.Add(option17);
            if (string.IsNullOrEmpty(option18) == false)
                validOptions.Add(option18);
            if (string.IsNullOrEmpty(option19) == false)
                validOptions.Add(option19);
            if (string.IsNullOrEmpty(option20) == false)
                validOptions.Add(option20);

            return validOptions;
        }
    }
}
