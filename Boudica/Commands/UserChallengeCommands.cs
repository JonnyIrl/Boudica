using Boudica.Classes;
using Boudica.Enums;
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
using System.Threading.Tasks;

namespace Boudica.Commands
{
    public class UserChallengeCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly UserChallengeService _userChallengeService;
        private readonly GuardianService _guardianService;
        private static bool _subscribed = false;
        public UserChallengeCommands(IServiceProvider services, CommandHandler handler)
        {
            _userChallengeService = services.GetRequiredService<UserChallengeService>();
            _guardianService = services.GetRequiredService<GuardianService>();
            if (!_subscribed)
            {
                handler.OnAcceptChallengeButtonClicked += OnAcceptChallengeButtonClicked;
                handler.OnEnterGuessChallengeButtonClicked += OnEnterGuessChallengeButtonClicked;
                handler.OnEnterGuessModalSubmitted += OnEnterGuessModalSubmitted;
                _subscribed = true;
            }
        }

        private async Task<Result> OnEnterGuessModalSubmitted(SocketModal modal, long sessionId, string guess)
        {
            UserChallenge userChallenge = await _userChallengeService.GetUserChallenge(sessionId);
            if (userChallenge == null)
            {
                return new Result(false, "Could not find challenge to accept");
            }

            switch(userChallenge.ChallengeType)
            {
                case Challenge.RandomNumber:
                    return new Result(false, "this is not an option yet");
                default:
                    return new Result(false, "Something went wrong");
            }
        }

        private async Task<Result> RockPaperScissorsGame(SocketMessageComponent component, UserChallenge userChallenge, string guess)
        {
            if(guess.ToLower() == "rock")
            {
                if (userChallenge.Challenger.UserId == component.User.Id)
                    await _userChallengeService.UpdateChallengersAnswer(userChallenge.SessionId, ((int)RockPaperScissors.Rock).ToString());
                else
                    await _userChallengeService.UpdateContendersAnswer(userChallenge.SessionId, ((int)RockPaperScissors.Rock).ToString());
            }
            else if(guess.ToLower() == "paper")
            {
                if (userChallenge.Challenger.UserId == component.User.Id)
                    await _userChallengeService.UpdateChallengersAnswer(userChallenge.SessionId, ((int)RockPaperScissors.Paper).ToString());
                else
                    await _userChallengeService.UpdateContendersAnswer(userChallenge.SessionId, ((int)RockPaperScissors.Paper).ToString());
            }
            else if(guess.ToLower() == "scissors")
            {
                if (userChallenge.Challenger.UserId == component.User.Id)
                    await _userChallengeService.UpdateChallengersAnswer(userChallenge.SessionId, ((int)RockPaperScissors.Scissors).ToString());
                else
                    await _userChallengeService.UpdateContendersAnswer(userChallenge.SessionId, ((int)RockPaperScissors.Scissors).ToString());
            }
            else
            {
                return new Result(false, "You must select an option");
            }

            UserChallenge updatedUserChallenge = await _userChallengeService.GetUserChallenge(userChallenge.SessionId);
            if(updatedUserChallenge != null && 
                string.IsNullOrEmpty(updatedUserChallenge.Contender.Answer) == false && 
                string.IsNullOrEmpty(updatedUserChallenge.Challenger.Answer) == false)
            {
                IUserMessage message = (IUserMessage)await component.Channel.GetMessageAsync(updatedUserChallenge.MessageId, CacheMode.AllowDownload);
                if(message == null)
                {
                    await component.RespondAsync("Could not find message");
                }
                else
                {
                    await component.RespondAsync("Your guess has been entered and the results are in.. check the original message to see the winner!", ephemeral: true);
                    Console.WriteLine("Getting winner text");
                    string winnersText = GetRockPaperScissorsWinnerText(ref updatedUserChallenge);                    
                    await message.ModifyAsync(x =>
                    {
                        x.Components = null;
                        x.Embed = EmbedHelper.CreateSuccessReply($"The guesses are in...\n\n" +
                        $"{GetUserChallengeUserMessage(updatedUserChallenge.ChallengeType, updatedUserChallenge.Challenger)}\n" +
                        $"{GetUserChallengeUserMessage(updatedUserChallenge.ChallengeType, updatedUserChallenge.Contender)}\n\n" +
                        $"{winnersText}").Build();
                        x.Content = null;
                    });

                    if (updatedUserChallenge.WinnerId != null)
                    {
                        if (updatedUserChallenge.Challenger.UserId == updatedUserChallenge.WinnerId)
                        {
                            await _guardianService.IncreaseGlimmerAsync(updatedUserChallenge.Challenger.UserId, updatedUserChallenge.Challenger.UserName, updatedUserChallenge.Wager);
                            //Loser gets decreased the wager amount
                            await _guardianService.IncreaseGlimmerAsync(updatedUserChallenge.Contender.UserId, updatedUserChallenge.Contender.UserName, updatedUserChallenge.Wager * -1);
                        }
                        else
                        {
                            await _guardianService.IncreaseGlimmerAsync(updatedUserChallenge.Contender.UserId, updatedUserChallenge.Contender.UserName, updatedUserChallenge.Wager);
                            //Loser gets decreased the wager amount
                            await _guardianService.IncreaseGlimmerAsync(updatedUserChallenge.Challenger.UserId, updatedUserChallenge.Challenger.UserName, updatedUserChallenge.Wager * -1);
                        }

                        await _userChallengeService.UpdateChallengeWinnerId(updatedUserChallenge.SessionId, (ulong)updatedUserChallenge.WinnerId);
                    }
                    else
                    {
                        await _userChallengeService.UpdateChallengeWinnerId(updatedUserChallenge.SessionId, 0);
                    }
                }
            }
            else
            {
                await component.RespondAsync($"{component.User.Username}, your guess has been entered! Keep an eye on the orignal post to see the winner!", ephemeral: true);
            }

            return new Result(true, string.Empty);
        }

        private string GetRockPaperScissorsWinnerText(ref UserChallenge userChallenge)
        {
            if(userChallenge.Challenger.Answer == userChallenge.Contender.Answer)
            {
                return "The winner is.... **nobody** it's a draw!";
            }
            else if(
                (RockPaperScissors)int.Parse(userChallenge.Challenger.Answer) == RockPaperScissors.Rock &&
                (RockPaperScissors)int.Parse(userChallenge.Contender.Answer) == RockPaperScissors.Paper)
            {
                userChallenge.WinnerId = userChallenge.Contender.UserId;
                return $"The winner is.... **{userChallenge.Contender.UserName}**! Enjoy the {userChallenge.Wager} Glimmer.";
            }
            else if (
                (RockPaperScissors)int.Parse(userChallenge.Challenger.Answer) == RockPaperScissors.Rock &&
                (RockPaperScissors)int.Parse(userChallenge.Contender.Answer) == RockPaperScissors.Scissors)
            {
                userChallenge.WinnerId = userChallenge.Challenger.UserId;
                return $"The winner is.... **{userChallenge.Challenger.UserName}**! Enjoy the {userChallenge.Wager} Glimmer.";
            }

            else if (
                (RockPaperScissors)int.Parse(userChallenge.Challenger.Answer) == RockPaperScissors.Paper &&
                (RockPaperScissors)int.Parse(userChallenge.Contender.Answer) == RockPaperScissors.Rock)
            {
                userChallenge.WinnerId = userChallenge.Challenger.UserId;
                return $"The winner is.... **{userChallenge.Challenger.UserName}**! Enjoy the {userChallenge.Wager} Glimmer.";
            }
            else if (
                (RockPaperScissors)int.Parse(userChallenge.Challenger.Answer) == RockPaperScissors.Paper &&
                (RockPaperScissors)int.Parse(userChallenge.Contender.Answer) == RockPaperScissors.Scissors)
            {
                userChallenge.WinnerId = userChallenge.Contender.UserId;
                return $"The winner is.... **{userChallenge.Contender.UserName}**! Enjoy the {userChallenge.Wager} Glimmer.";
            }
            else if (
                 (RockPaperScissors)int.Parse(userChallenge.Challenger.Answer) == RockPaperScissors.Scissors &&
                 (RockPaperScissors)int.Parse(userChallenge.Contender.Answer) == RockPaperScissors.Rock)
            {
                userChallenge.WinnerId = userChallenge.Contender.UserId;
                return $"The winner is.... **{userChallenge.Contender.UserName}**! Enjoy the {userChallenge.Wager} Glimmer.";
            }
            else
            {
                userChallenge.WinnerId = userChallenge.Challenger.UserId;
                return $"The winner is.... **{userChallenge.Challenger.UserName}**! Enjoy the {userChallenge.Wager} Glimmer.";
            }
        }

        private string GetUserChallengeUserMessage(Challenge challenge, UserChallengeUser userChallengeUser)
        {
            switch (challenge)
            {
                case Challenge.RockPaperScissors:
                    return $"{userChallengeUser.UserName} guessed {(RockPaperScissors)int.Parse(userChallengeUser.Answer)}.";
                case Challenge.RandomNumber:
                    break;
                default:
                    return "Something went wrong";
            }

            return string.Empty;
        }

        private async Task<Result> OnEnterGuessChallengeButtonClicked(SocketMessageComponent component, long sessionId, string guess)
        {
            UserChallenge userChallenge = await _userChallengeService.GetUserChallenge(sessionId);
            if (userChallenge == null)
            {
                return new Result(false, "Could not find challenge to accept");
            }
            else if (userChallenge.Contender.UserId == component.User.Id)
            {
                if (string.IsNullOrEmpty(userChallenge.Contender.Answer))
                {
                    switch (userChallenge.ChallengeType)
                    {
                        case Challenge.RockPaperScissors:
                            return await RockPaperScissorsGame(component, userChallenge, guess);
                        case Challenge.RandomNumber:
                            return new Result(false, "this is not an option yet");
                        default:
                            return new Result(false, "Something went wrong");
                    }
                }
                else
                {
                    await component.RespondAsync("You cannot change your guess", ephemeral: true);
                }
            }
            else if (userChallenge.Challenger.UserId == component.User.Id)
            {
                if (string.IsNullOrEmpty(userChallenge.Challenger.Answer))
                {
                    switch (userChallenge.ChallengeType)
                    {
                        case Challenge.RockPaperScissors:
                            return await RockPaperScissorsGame(component, userChallenge, guess);
                        case Challenge.RandomNumber:
                            return new Result(false, "this is not an option yet");
                        default:
                            return new Result(false, "Something went wrong");
                    }
                }
                else
                {
                    await component.RespondAsync("You cannot change your guess", ephemeral: true);
                }
            }
            else
            {
                await component.RespondAsync("You are not part of this challenge");
            }

            return new Result(true, string.Empty);
        }

        private async Task<Result> OnAcceptChallengeButtonClicked(SocketMessageComponent component, long sessionId)
        {
            UserChallenge userChallenge = await _userChallengeService.GetUserChallenge(sessionId);
            if(userChallenge == null)
            {
                return new Result(false, "Could not find challenge to accept");
            }
            if(userChallenge.Contender.UserId != component.User.Id)
            {
                return new Result(false, "You were not challenged so you cannot accept");
            }

            CommandResult result = await _userChallengeService.AcceptUserChallenge(sessionId, component.User.Id);
            if(!result.Success)
            {
                if(userChallenge.ExpiredDateTime < DateTime.UtcNow)
                {
                    IUserMessage expiredMessage = (IUserMessage)await component.Channel.GetMessageAsync(userChallenge.MessageId, CacheMode.AllowDownload);
                    if (expiredMessage != null)
                    {
                        await expiredMessage.DeleteAsync();
                    }
                }
                return new Result(false, result.Message);
            }

            await component.RespondAsync("You have accepted the challenge, check the original message and press the Enter Guess button", ephemeral: true);

            IUserMessage message = (IUserMessage) await component.Channel.GetMessageAsync(userChallenge.MessageId, CacheMode.AllowDownload);
            if (message == null)
            {
                return new Result(false, "Could not find message to accept");
            }

            if (userChallenge.ChallengeType == Challenge.RandomNumber)
            {
                await message.ModifyAsync(x =>
                {
                    var enterGuessButton = new ComponentBuilder()
                    .WithButton("Enter Guess", $"{(int)ButtonCustomId.EnterGuess}-{userChallenge.SessionId}", ButtonStyle.Primary);
                    x.Components = enterGuessButton.Build();
                    x.Content = $"<@{userChallenge.Challenger.UserId}> your challenge has been accepted. Both players please press the button below to enter your guess!";
                    x.Embed = x.Embed;
                });
            }
            else if(userChallenge.ChallengeType == Challenge.RockPaperScissors)
            {
                await message.ModifyAsync(x =>
                {
                    var selectMenuBuilder = new SelectMenuBuilder()
                    {
                        
                        CustomId = $"{(int)SelectMenuCustomId.RockPaperScissors}-{userChallenge.SessionId}",
                        Placeholder = "Select an option!",
                        MaxValues = 1,
                        MinValues = 1
                    };
                    selectMenuBuilder.AddOption("Rock", $"{RockPaperScissors.Rock}");
                    selectMenuBuilder.AddOption("Paper", $"{RockPaperScissors.Paper}");
                    selectMenuBuilder.AddOption("Scissors", $"{RockPaperScissors.Scissors}");

                    x.Components = new ComponentBuilder().WithSelectMenu(selectMenuBuilder).Build();
                    x.Content = $"<@{userChallenge.Challenger.UserId}> your challenge has been accepted. Both players please press the button below to enter your guess!";
                    x.Embed = x.Embed;
                });
            }

            return new Result(true, string.Empty);
        }

        [SlashCommand("challenge", "Challenge a User")]
        public async Task CreateUserChallenge(
            [Summary("personToChallenge", "Person to Challenge")] SocketGuildUser personToChallenge,
            [Summary("wager", "The amount to bet")] int wager,
            [Summary("challenge", "Choose a Challenge")] Challenge challenge)
        {
            if(challenge == Challenge.RandomNumber)
            {
                await RespondAsync("Current a work in progress and will be released soon..");
                return;
            }
            Guardian challenger = await _guardianService.GetGuardian(Context.User.Id);
            Guardian contender = await _guardianService.GetGuardian(personToChallenge.Id);
            if(challenger.Glimmer < wager)
            {
                await RespondAsync("You do not have enough glimmer to bet");
                return;
            }
            if(contender.Glimmer < wager)
            {
                await RespondAsync($"{personToChallenge.Username} does not have enough glimmer to accept");
                return;
            }


            UserChallenge userChallenge = await _userChallengeService.CreateUserChallenge(
                Context.User.Id, 
                Context.User.Username, 
                personToChallenge.Id, 
                personToChallenge.Username, 
                wager, 
                challenge);

            EmbedBuilder embedBuilder = CreateChallengeEmbed(challenge);
            var acceptButton = new ComponentBuilder()
                .WithButton("Accept Challenge", $"{(int)ButtonCustomId.AcceptChallenge}-{userChallenge.SessionId}", ButtonStyle.Success);
            await RespondAsync(text: $"<@{personToChallenge.Id}> click the accept button if you accept the challenge. With a wager of **{wager}** Glimmer!", embed: embedBuilder.Build(), components: acceptButton.Build());
            IUserMessage newMessage = await GetOriginalResponseAsync();
            if(newMessage != null)
            {
                await _userChallengeService.UpdateChallengeMessageDetails(userChallenge.SessionId, Context.Guild.Id, Context.Channel.Id, newMessage.Id);
            }
        }

        [SlashCommand("accept-challenge", "Accept a challenge")]
        public async Task AcceptChallenge([Summary("challengeId", "Challenge Id")]int challengeId)
        {
            CommandResult result = await _userChallengeService.AcceptUserChallenge(challengeId, Context.User.Id);
            if(result.Success == false)
            {
                await RespondAsync(result.Message);
                return;
            }
            else
            {
                UserChallenge userChallenge = await _userChallengeService.GetUserChallenge(challengeId);
                await RespondAsync($"<@{userChallenge.Challenger.UserId}>, your challenge has been accepted!\n\nUse {GenerateUserChallengeCommand(userChallenge)} to play!");
            }
        }

        public async Task RockPaperScissors2(long challengeId, RockPaperScissors guess)
        {
            UserChallenge userChallenge = await _userChallengeService.GetUserChallenge(challengeId);
            if (userChallenge == null)
            {
                await RespondAsync("Could not find challenge");
                return;
            }
            else if (userChallenge.Contender.UserId == Context.User.Id)
            {
                if (string.IsNullOrEmpty(userChallenge.Contender.Answer))
                {
                    userChallenge.Contender.Answer = ((int)guess).ToString();
                    await RespondAsync("Your guess has been locked in!");
                }
                else
                {
                    await RespondAsync("You cannot change your guess", ephemeral: true);
                }
            }
            else if (userChallenge.Challenger.UserId == Context.User.Id)
            {
                if (string.IsNullOrEmpty(userChallenge.Challenger.Answer))
                {
                    userChallenge.Challenger.Answer = ((int)guess).ToString();
                    await RespondAsync("Your guess has been locked in!");
                }
                else
                {
                    await RespondAsync("You cannot change your guess", ephemeral: true);
                }
            }
            else
            {
                await RespondAsync("You are not part of this challenge");
                return;
            }

            userChallenge = await _userChallengeService.GetUserChallenge(challengeId);
            if(string.IsNullOrEmpty(userChallenge.Contender.Answer) == false && string.IsNullOrEmpty(userChallenge.Challenger.Answer) == false)
            {

            }
        }

        private string GenerateUserChallengeCommand(UserChallenge userChallenge)
        {
            switch(userChallenge.ChallengeType)
            {
                case Challenge.RockPaperScissors:
                    return $"/challenge-rps {userChallenge.SessionId} Rock/Paper/Scissors";
                case Challenge.RandomNumber:
                    return $"/challenge-random {userChallenge.SessionId} Your Guess";
                default:
                    return "Something went wrong";
            }
        }

        private EmbedBuilder CreateChallengeEmbed(Challenge challenge)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithTitle(GetEmbedChallengeTitle(challenge));
            embedBuilder.WithDescription(GetEmbedDescription(challenge));
            embedBuilder.WithColor(Color.Orange);
            return embedBuilder;
        }

        private string GetEmbedChallengeTitle(Challenge challenge)
        {
            switch (challenge)
            {
                case Challenge.RockPaperScissors:
                    return "Rock Paper Scissors";
                case Challenge.RandomNumber:
                    return "Random Number Game";
                default:
                    return "Something went wrong..";
            }
        }

        private string GetEmbedDescription(Challenge challenge)
        {
            switch (challenge)
            {
                case Challenge.RockPaperScissors:
                    return "Both players will enter their guess, a winner will be decided. In the event of a draw both players will get their glimmer back!";
                case Challenge.RandomNumber:
                    return "Both players will enter a number between 1-50 inclusive. The person who guess correctly or is closest to the random number wins";
                default:
                    return "Something went wrong..";
            }
        }
    }
}
