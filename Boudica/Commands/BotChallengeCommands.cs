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
        private readonly BotChallengeService _botChallengeService;
        private readonly GuardianService _guardianService;
        private readonly HistoryService _historyService;
        private static bool _subscribed = false;
        public BotChallengeCommands(IServiceProvider services, CommandHandler handler)
        {
            _botChallengeService = services.GetRequiredService<BotChallengeService>();
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

            BotChallenge newChallenge = await _botChallengeService.CreateBotChallenge(Context.User.Id, Context.User.Username, wager, challenge);
            CommandResult result = await _botChallengeService.AcceptBotChallenge(newChallenge.SessionId);
            if(result.Success == false)
            {
                await RespondAsync($"Something went wrong.. {result.Message}");
                return;
            }

            Random random = new Random();
            EmbedBuilder embed = CreateHigherOrLowerEmbed(RoundNumber.FirstRound, random.Next(1, 13), wager);
            await RespondAsync(embed: embed.Build());
        }

        private EmbedBuilder UpdateRoundGuess(RoundNumber roundNumber, bool correct, int wagerAmount, Embed existingEmbed)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithTitle(existingEmbed.Title);
            embedBuilder.WithDescription(existingEmbed.Description);
            embedBuilder.Color = correct ? Color.Green : Color.Red;
            foreach(EmbedField field in existingEmbed.Fields)
            {
                embedBuilder.AddField(field.Name, field.Value);
            }

            if (roundNumber == RoundNumber.GameOverRound)
            {
                if (correct)
                {
                    embedBuilder.Fields[embedBuilder.Fields.Count - 1].Value += $"\n\nCorrect!! Congratulations on winning {wagerAmount} Glimmer!";
                    embedBuilder.Color = Color.Green;
                }
                else
                {
                    embedBuilder.Fields[embedBuilder.Fields.Count - 1].Value += $"\n\nIncorrect!! Sorry, better luck next time you have lost {wagerAmount} Glimmer!";
                    embedBuilder.Color = Color.Red;
                }              
            }
            else
            {
                if (correct)
                {
                    embedBuilder.Fields[embedBuilder.Fields.Count - 1].Value += $"\nCorrect, good guess!";
                    embedBuilder.Color = Color.Green;
                }
                else
                {
                    embedBuilder.Fields[embedBuilder.Fields.Count - 1].Value += $"\nIncorrect!! Sorry, better luck next time you have lost {wagerAmount} Glimmer!";
                    embedBuilder.Color = Color.Red;
                }
            }

            return embedBuilder;
        }
        private EmbedBuilder GenerateNextRoundGuess(RoundNumber roundNumber, int randomNumber, EmbedBuilder existingEmbedBuilder)
        {
            existingEmbedBuilder.AddField($"Round {(roundNumber + 1)}", $"Do you think the next number is Higher or Lower than **{randomNumber}**");
            existingEmbedBuilder.WithColor(Color.LightOrange);
            return existingEmbedBuilder;
        }
        private EmbedBuilder CreateHigherOrLowerEmbed(RoundNumber roundNumber, int randomNumber, int wagerAmount)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithTitle($"Higher Or Lower Challenge for {wagerAmount} Glimmer!");
            embedBuilder.WithDescription("Rules: A random number between 1-12 will be displayed below.\nPress the Higher Button if you think the next number will be higher or press the Lower Button if you think the number will be lower. After 3 rounds you will be awarded Glimmer.");
            embedBuilder.AddField($"Round {(roundNumber + 1)}", $"Do you think the next number is Higher or Lower than **{randomNumber}**");
            embedBuilder.WithColor(Color.LightOrange);
            var higherOrLowerButtons = new 
            return embedBuilder;
        }
    }
}
