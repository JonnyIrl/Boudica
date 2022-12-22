using Boudica.Helpers;
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
    public class CrucibleCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private CronService _cronService;
        private readonly TrialsService _trialsService;
        private readonly GuardianService _guardianService;
        private const string CrucibleRole = "Crucible Contenders";

        public CrucibleCommands(IServiceProvider services)
        {
            _cronService = services.GetRequiredService<CronService>();
            _trialsService = services.GetRequiredService<TrialsService>();
            _guardianService = services.GetRequiredService<GuardianService>();
        }

        [SlashCommand("trials-vote", "Vote on this weeks Trials map!")]
        public async Task VoteForTrialsMap(Enums.TrialsMap trialsMap)
        {
            TrialsVote currentVote = await _trialsService.GetThisWeeksVote();
            if (currentVote == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("There is currently no trials vote yet.. keep checking Crucible channel!").Build(), ephemeral: true);
                return;
            }
            else if (currentVote.IsLocked)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Sorry, this weeks vote is closed!").Build(), ephemeral: true);
                return;
            }
            PlayerVote existingPlayerVote = currentVote.PlayerVotes.FirstOrDefault(x => x.Id == Context.User.Id);
            if (existingPlayerVote != null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply($"You already voted this week for {existingPlayerVote.TrialsMap.ToName()}!").Build(), ephemeral: true);
            }
            else
            {
                bool result = await _trialsService.AddPlayersVote(Context.User.Id, Context.User.Username, trialsMap);
                if (result)
                {
                    await RespondAsync(embed: EmbedHelper.CreateSuccessReply($"Your vote for {trialsMap.ToName()} has been counted. Thank you for voting!").Build(), ephemeral: true);
                }
                else
                {
                    await RespondAsync(embed: EmbedHelper.CreateFailedReply("Failed to register your vote, try again!").Build(), ephemeral: true);
                }
            }     
        }

        [SlashCommand("confirm-trials-map", "Confirm weekly Trials Map")]
        public async Task ConfirmTrialsMap(Enums.TrialsMap trialsMap)
        {
            if (Context.User.Id != 244209636897456129)
            {
                await RespondAsync("Failed - Only Jonny can do this command", ephemeral: true);
                return;
            }

            List<PlayerVote> winningPlayerVotes = await _trialsService.GetWinningTrialsGuesses(trialsMap);
            if (winningPlayerVotes == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find trials vote").Build());
                return;
            }
            else if (winningPlayerVotes.Count == 0)
            {
                List<PlayerVote> allPlayerVotes = await _trialsService.GetAllTrialsGuesses();
                foreach (PlayerVote playerVote in allPlayerVotes)
                {
                    await _guardianService.IncreaseGlimmerAsync(playerVote.Id, playerVote.Username, 5 * 2);
                }
                await RespondAsync(embed: EmbedHelper.CreateFailedReply($"Nobody correctly guessed {trialsMap}... sooo you're all winners! 10 Glimmer for everyone!!").Build());
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($"Congratulations to <@{winningPlayerVotes[0].Id}>, you were first to guess the correct map **{trialsMap}**!");
                sb.AppendLine("");
                sb.AppendJoin(", ", winningPlayerVotes.Select(x => x.Username));
                sb.Append(" have been awarded 10 Glimmer for guessing correctly, everyone else has received 5 Glimmer for entering!");

                List<PlayerVote> allPlayerVotes = await _trialsService.GetAllTrialsGuesses();
                List<PlayerVote> oneGlimmerPlayers = new List<PlayerVote>();
                foreach(PlayerVote playerVote in allPlayerVotes)
                {
                    if (winningPlayerVotes.FirstOrDefault(x => x.Id == playerVote.Id) != null)
                        continue;

                    oneGlimmerPlayers.Add(playerVote);
                }

                foreach(PlayerVote playerVote in oneGlimmerPlayers)
                {
                    await _guardianService.IncreaseGlimmerAsync(playerVote.Id, playerVote.Username, 5);
                }

                foreach (PlayerVote playerVote in winningPlayerVotes)
                {
                    await _guardianService.IncreaseGlimmerAsync(playerVote.Id, playerVote.Username, 10);
                }

                await RespondAsync(sb.ToString());
            }
        }
    }
}
