using Boudica.Classes;
using Boudica.Enums;
using Boudica.Extensions;
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
                handler.OnHigherButtonClicked += OnHigherButtonClicked;
                handler.OnLowerButtonClicked += OnLowerButtonClicked;
                //handler.OnEnterGuessChallengeButtonClicked += OnEnterGuessChallengeButtonClicked;
                //handler.OnEnterGuessModalSubmitted += OnEnterGuessModalSubmitted;
                _subscribed = true;
            }
        }

        private async Task<Result> OnHigherButtonClicked(SocketMessageComponent component, long sessionId)
        {
            return await HigherOrLower(component, sessionId, true);
        }

        private async Task<Result> OnLowerButtonClicked(SocketMessageComponent component, long sessionId)
        {
            return await HigherOrLower(component, sessionId, false);
        }

        private async Task<Result> HigherOrLower(SocketMessageComponent component, long sessionId, bool higherGuess)
        {
            BotChallenge existingChallenge = await _botChallengeService.GetBotChallenge(sessionId);
            if (existingChallenge == null)
            {
                return new Result(false, "Could not find challenge");
            }
            if(existingChallenge.Challenger.UserId != component.User.Id)
            {
                return new Result(false, "Only the person who created the challenge can compete");
            }

            BotRound currentRound = existingChallenge.Rounds[existingChallenge.CurrentRound - 1];
            currentRound.Guess = higherGuess ? 1 : 0;
            await _botChallengeService.UpdateRoundInformation((RoundNumber)existingChallenge.CurrentRound, currentRound, existingChallenge.SessionId);

            IUserMessage existingMessage = await GetChallengeMessage(component, existingChallenge.MessageId);
            if (existingMessage == null)
            {
                return new Result(false, "Could not find message to update");
            }

            BotRound botRound = existingChallenge.Rounds[existingChallenge.CurrentRound - 1];
            int newRandomNumber = GenerateRandomNumber(botRound.Number);
            //Guessed Higher and got correctly                          //Guessed Lower and got correctly
            if (newRandomNumber >= botRound.Number && higherGuess || newRandomNumber <= botRound.Number && higherGuess == false)
            {
                //Game is over as they guessed correctly
                if (existingChallenge.CurrentRound == (int)RoundNumber.FinalRound)
                {
                    await GuessCorrect(component, higherGuess, RoundNumber.GameOverRound, existingChallenge.Wager, newRandomNumber, existingMessage.Embeds.First(), existingMessage);
                    await _botChallengeService.MarkChallengeAsCompleted(existingChallenge.SessionId, component.User.Id);
                    return new Result(true, string.Empty);
                }

                await GuessCorrect(component, higherGuess, (RoundNumber)existingChallenge.CurrentRound, existingChallenge.Wager, newRandomNumber, existingMessage.Embeds.First(), existingMessage);
                existingChallenge.CurrentRound++;
                existingChallenge.Rounds[existingChallenge.CurrentRound - 1].Number = newRandomNumber;
                await _botChallengeService.UpdateNextRoundInformation((RoundNumber)existingChallenge.CurrentRound, existingChallenge.SessionId);
                await _botChallengeService.UpdateRoundInformation((RoundNumber)existingChallenge.CurrentRound, existingChallenge.Rounds[existingChallenge.CurrentRound - 1], existingChallenge.SessionId);
                return new Result(true, string.Empty);
            }
            else
            {
                await GuessIncorrect(component, higherGuess, (RoundNumber)existingChallenge.CurrentRound, existingChallenge.Wager, newRandomNumber, existingMessage.Embeds.First(), existingMessage);
                await _botChallengeService.UpdateClosedChallenge(existingChallenge.SessionId);
                return new Result(true, string.Empty);
            }
        }
        

        private async Task<Result> GuessCorrect(SocketMessageComponent component, bool higher, RoundNumber roundNumber, int wagerAmount, int newRandomNumber,  IEmbed existingEmbed, IUserMessage existingMessage)
        {
            EmbedBuilder embedBuilder = UpdateRoundGuess(roundNumber, higher, true, wagerAmount, newRandomNumber, existingEmbed);
            if (roundNumber == RoundNumber.GameOverRound)
            {
                await _guardianService.IncreaseGlimmerAsync(component.User.Id, component.User.Username, wagerAmount);
                await component.RespondAsync($"Congatulations! You have won {wagerAmount} Glimmer!", ephemeral: true);
            }
            else
                await component.RespondAsync($"Correct.. moving on to {((RoundNumber)((int)roundNumber + 1)).ToName()}", ephemeral: true);

            await existingMessage.ModifyAsync(x =>
            {
                x.Embed = embedBuilder.Build();
                x.Components = roundNumber == RoundNumber.GameOverRound ? null : x.Components;
            });
            return new Result(true, string.Empty);
        }

        private async Task<Result> GuessIncorrect(SocketMessageComponent component, bool higher, RoundNumber roundNumber, int wagerAmount, int newRandomNumber, IEmbed existingEmbed, IUserMessage existingMessage)
        {
            EmbedBuilder embedBuilder = UpdateRoundGuess(roundNumber, higher, false, wagerAmount, newRandomNumber, existingEmbed);
            await _guardianService.IncreaseGlimmerAsync(component.User.Id, component.User.Username, wagerAmount * -1);
            await component.RespondAsync($"Unlucky, you were incorrect, you have lost {wagerAmount} Glimmer, better luck next time!", ephemeral: true);
            await existingMessage.ModifyAsync(x =>
            {
                x.Embed = embedBuilder.Build();
                x.Components = null;
            });
            return new Result(true, string.Empty);
        }

        private async Task<IUserMessage> GetChallengeMessage(SocketMessageComponent component, ulong messageId)
        {
            return await component.Channel.GetMessageAsync(messageId, CacheMode.AllowDownload) as IUserMessage;
        }

        [SlashCommand("challenge-boudica", "Challenge Boudica")]
        public async Task ChallengeBoudica(
            [Summary("wager", "The amount to bet and potentially win!")] int wager,
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

            int randomNumber = GenerateRandomNumber();
            await _botChallengeService.UpdateRoundInformation(RoundNumber.FirstRound, new BotRound(RoundNumber.FirstRound, randomNumber), newChallenge.SessionId);

            EmbedBuilder embed = CreateHigherOrLowerEmbed(RoundNumber.FirstRound, randomNumber, wager);
            var higherOrLowerButtons = new ComponentBuilder()
               .WithButton("Higher", $"{(int)ButtonCustomId.Higher}-{newChallenge.SessionId}", ButtonStyle.Success)
               .WithButton("Lower", $"{(int)ButtonCustomId.Lower}-{newChallenge.SessionId}", ButtonStyle.Danger);
            await RespondAsync(embed: embed.Build(), components: higherOrLowerButtons.Build());
            IUserMessage newMessage = await GetOriginalResponseAsync();
            if (newMessage != null)
            {
                await _botChallengeService.UpdateChallengeMessageDetails(newChallenge.SessionId, Context.Guild.Id, Context.Channel.Id, newMessage.Id);
            }   
            await _historyService.InsertHistoryRecord(Context.User.Id, null, HistoryType.BotChallengeHigherOrLower);
        }

        private EmbedBuilder UpdateRoundGuess(RoundNumber roundNumber, bool higher, bool correct, int wagerAmount, int newRandomNumber, IEmbed existingEmbed)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithTitle(existingEmbed.Title);
            embedBuilder.WithDescription(existingEmbed.Description);
            embedBuilder.Color = correct ? Color.Green : Color.Red;
            string actionType = higher ? "Higher" : "Lower";
            foreach(EmbedField field in existingEmbed.Fields)
            {
                embedBuilder.AddField(field.Name, field.Value);
            }

            if (roundNumber == RoundNumber.GameOverRound)
            {
                if (correct)
                {
                    embedBuilder.Fields[embedBuilder.Fields.Count - 1].Value += $"\n\nCorrect, you guessed {actionType}!! The next number was **{newRandomNumber}**. Congratulations on winning {wagerAmount} Glimmer!";
                    embedBuilder.Color = Color.Green;
                }
                else
                {
                    embedBuilder.Fields[embedBuilder.Fields.Count - 1].Value += $"\nIncorrect, you guessed {actionType}!! The next number was **{newRandomNumber}**. Sorry better luck next time you have lost {wagerAmount} Glimmer!";
                    embedBuilder.Color = Color.Red;
                }              
            }
            else
            {
                if (correct)
                {
                    embedBuilder.Fields[embedBuilder.Fields.Count - 1].Value += $"\nCorrect, good guess with {actionType}!";
                    embedBuilder.Color = Color.Green;
                    GenerateNextRoundGuess(roundNumber, newRandomNumber, embedBuilder);
                }
                else
                {
                    embedBuilder.Fields[embedBuilder.Fields.Count - 1].Value += $"\nIncorrect, you guessed {actionType}!! The next number was **{newRandomNumber}**. Sorry better luck next time you have lost {wagerAmount} Glimmer!";
                    embedBuilder.Color = Color.Red;
                }
            }

            return embedBuilder;
        }
        private EmbedBuilder GenerateNextRoundGuess(RoundNumber roundNumber, int randomNumber, EmbedBuilder existingEmbedBuilder)
        {
            existingEmbedBuilder.AddField($"{((RoundNumber) ((int)roundNumber + 1)).ToName()}", $"Do you think the next number is Higher or Lower than **{randomNumber}**");
            existingEmbedBuilder.WithColor(Color.LightOrange);
            return existingEmbedBuilder;
        }
        private EmbedBuilder CreateHigherOrLowerEmbed(RoundNumber roundNumber, int randomNumber, int wagerAmount)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithTitle($"Higher Or Lower Challenge for {wagerAmount} Glimmer!");
            embedBuilder.WithDescription("Rules: A random number between 1-12 will be displayed below.\nPress the Higher Button if you think the next number will be higher or press the Lower Button if you think the number will be lower. After 3 rounds you will be awarded Glimmer.");
            embedBuilder.AddField($"{roundNumber.ToName()}", $"Do you think the next number is Higher or Lower than **{randomNumber}**");
            embedBuilder.WithColor(Color.LightOrange);
            return embedBuilder;
        }

        private int GenerateRandomNumber(int previousNumber = -1)
        {
            Random random = new Random();
            int randomNumber = random.Next(1, 13);
            while (randomNumber == previousNumber)
            {
                randomNumber = random.Next(1, 13);
            }

            return randomNumber;
        }
    }
}
