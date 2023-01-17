using Boudica.Enums;
using Boudica.Helpers;
using Boudica.MongoDB;
using Boudica.MongoDB.Models;
using Boudica.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Commands
{
    public class BotChallengeCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly BotChallengeService _userChallengeService;
        private readonly GuardianService _guardianService;
        private readonly HistoryService _historyService;
        private static bool _subscribed = false;
        public BotChallengeCommands(IServiceProvider services, CommandHandler handler)
        {
            _userChallengeService = services.GetRequiredService<BotChallengeService>();
            _guardianService = services.GetRequiredService<GuardianService>();
            _historyService = services.GetRequiredService<HistoryService>();
            if (!_subscribed)
            {
                //handler.OnAcceptChallengeButtonClicked += OnAcceptChallengeButtonClicked;
                //handler.OnEnterGuessChallengeButtonClicked += OnEnterGuessChallengeButtonClicked;
                //handler.OnEnterGuessModalSubmitted += OnEnterGuessModalSubmitted;
                _subscribed = true;
            }
        }

        [SlashCommand("challenge-boudica", "Challenge Boudica")]
        public async Task ChallengeBoudica(
            [Summary("wager", "The amount to bet")] int wager,
            [Summary("challenge", "Choose a Challenge")] BotChallenges challenge)
        {
            if (wager <= 0)
            {
                await RespondAsync("You must enter a valid number to bet");
                return;
            }
            Guardian challenger = await _guardianService.GetGuardian(Context.User.Id);
            if (challenger.Glimmer < wager)
            {
                await RespondAsync("You do not have enough glimmer to bet");
                return;
            }


        }
    }
}
