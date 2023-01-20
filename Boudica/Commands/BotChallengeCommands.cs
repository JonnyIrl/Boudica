using Boudica.Classes;
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
                handler.OnHigherButtonClicked += OnHigherButtonClicked; ;
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
            if(existingChallenge == null)
            {
                return new Result(false, "Could not find challenge");
            }
            BotRound currentRound = existingChallenge.Rounds[existingChallenge.CurrentRound];
            currentRound.Guess = higherGuess ? 1 : 0;
            await _botChallengeService.UpdateRoundInformation((RoundNumber)existingChallenge.CurrentRound, currentRound, existingChallenge.SessionId);

            IUserMessage existingMessage = await GetChallengeMessage(component, existingChallenge.MessageId);
            if(existingMessage == null)
            {
                return new Result(false, "Could not find message to update");
            }

            BotRound botRound = existingChallenge.Rounds[existingChallenge.CurrentRound];
            int newRandomNumber = GenerateRandomNumber(botRound.Number);
            if(higherGuess)
            {
                if(newRandomNumber >= botRound.Number && higherGuess)
                {
                    //Game is over as they guessed correctly
                    if(existingChallenge.CurrentRound == (int)RoundNumber.FinalRound)
                    {
                        await GuessCorrect(component, RoundNumber.GameOverRound, existingChallenge.Wager, existingMessage.Embeds.First(), existingMessage);
                        return new Result(true, string.Empty);
                    }
                    await GuessCorrect(component, (RoundNumber)existingChallenge.CurrentRound, existingChallenge.Wager, existingMessage.Embeds.First(), existingMessage);
                    existingChallenge.CurrentRound++;
                    existingChallenge.Rounds[existingChallenge.CurrentRound].Number = newRandomNumber;
                    await _botChallengeService.UpdateNextRoundInformation((RoundNumber)existingChallenge.CurrentRound, existingChallenge.SessionId);
                    await _botChallengeService.UpdateRoundInformation((RoundNumber)existingChallenge.CurrentRound, existingChallenge.Rounds[existingChallenge.CurrentRound], existingChallenge.SessionId);
                }
            }
            else
            {
                if(newRandomNumber <= botRound.Number)
                {

                }
            }
        }

        private async Task<Result> GuessCorrect(SocketMessageComponent component, RoundNumber roundNumber, int wagerAmount, IEmbed existingEmbed, IUserMessage existingMessage)
        {
            EmbedBuilder embedBuilder = UpdateRoundGuess(roundNumber, true, wagerAmount, existingEmbed);
            await component.RespondAsync($"Correct.. moving on to Round {((int)roundNumber+1)}" , ephemeral: true);
            await existingMessage.ModifyAsync(x =>
            {
                x.Embed = embedBuilder.Build();
                x.Components = x.Components;
            });
            return new Result(true, string.Empty);
        }

        private async Task<Result> GuessIncorrect()
        {
            return new Result(true, string.Empty);
        }

        private async Task<IUserMessage> GetChallengeMessage(SocketMessageComponent component, ulong messageId)
        {
            return await component.Channel.GetMessageAsync(messageId, CacheMode.AllowDownload) as IUserMessage;
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
        }

        private EmbedBuilder UpdateRoundGuess(RoundNumber roundNumber, bool correct, int wagerAmount, IEmbed existingEmbed)
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
