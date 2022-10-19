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
        private List<Emoji> _alphabetList;
        private const string CrucibleRole = "Crucible Contenders";

        public CrucibleCommands(IServiceProvider services)
        {
            _cronService = services.GetRequiredService<CronService>();
            _trialsService = services.GetRequiredService<TrialsService>();
            _guardianService = services.GetRequiredService<GuardianService>();

            PopulateAlphabetList();
        }

        private void PopulateAlphabetList()
        {
            _alphabetList = new List<Emoji>();
            _alphabetList.Add(new Emoji("🇦"));
            _alphabetList.Add(new Emoji("🇧"));
            _alphabetList.Add(new Emoji("🇨"));
            _alphabetList.Add(new Emoji("🇩"));
            _alphabetList.Add(new Emoji("🇪"));
            _alphabetList.Add(new Emoji("🇫"));
            _alphabetList.Add(new Emoji("🇬"));
            _alphabetList.Add(new Emoji("🇭"));
            _alphabetList.Add(new Emoji("🇮"));
            _alphabetList.Add(new Emoji("🇰"));
            _alphabetList.Add(new Emoji("🇱"));
            _alphabetList.Add(new Emoji("🇲"));
            _alphabetList.Add(new Emoji("🇳"));
            _alphabetList.Add(new Emoji("🇴"));
            _alphabetList.Add(new Emoji("🇵"));
            _alphabetList.Add(new Emoji("🇶"));
            _alphabetList.Add(new Emoji("🇷"));
            _alphabetList.Add(new Emoji("🇹"));
            _alphabetList.Add(new Emoji("🇺"));
            _alphabetList.Add(new Emoji("🇻"));
            _alphabetList.Add(new Emoji("🇼"));
            _alphabetList.Add(new Emoji("🇽"));
            _alphabetList.Add(new Emoji("🇾"));
            _alphabetList.Add(new Emoji("🇿"));
        }

        [SlashCommand("create-trials-vote", "Create trials vote")]
        public async Task CreateTrialsTask()
        {
            if (Context.User.Id != 244209636897456129)
            {
                await RespondAsync("Failed - Only Jonny can do this command", ephemeral: true);
                return;
            }

            bool createdTrialsVote = await _trialsService.CreateWeeklyTrialsVote();
            if(createdTrialsVote == false)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Failed to create weekly trials vote").Build());
            }

            EmbedBuilder embed = new EmbedBuilder();
            CronEmbedAttributes attributes = CreateTrialsVoteEmbedAttributes();
            embed.Title = attributes.Title;
            embed.Description = attributes.Description;
            string[] rgb = attributes.ColorCode.Split(",");
            embed.Color = new Color(int.Parse(rgb[0]), int.Parse(rgb[1]), int.Parse(rgb[2]));
            embed.AddField(attributes.EmbedFieldBuilder) ;

            IUserMessage message;
            IRole role = Context.Guild.Roles.FirstOrDefault(x => x.Name == CrucibleRole);
            if (role != null)
            {
                await RespondAsync(role.Mention, embed: embed.Build());
                message = await GetOriginalResponseAsync();
            }
            else
            {
                await RespondAsync(embed: embed.Build());
                message = await GetOriginalResponseAsync();
            }

            await _trialsService.UpdateMessageId(message.Id);
            await message.PinAsync();
            await message.AddReactionsAsync(_alphabetList.Take(TrialsMaps.Count));
        }

        [SlashCommand("lock-trials-vote", "Lock trials vote")]
        public async Task LockTrialsTask()
        {
            if (Context.User.Id != 244209636897456129)
            {
                await RespondAsync("Failed - Only Jonny can do this command", ephemeral: true);
                return;
            }

            TrialsVote trialsVote = await _trialsService.LockTrialsVote();
            if (trialsVote == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Failed to lock").Build());
                return;
            }

            await RespondAsync(embed: EmbedHelper.CreateSuccessReply("Voting is now closed").Build());
            

            IUserMessage message = (IUserMessage)await Context.Channel.GetMessageAsync(trialsVote.MessageId, CacheMode.AllowDownload);
            if (message == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find message to close").Build());
                return;
            }

            EmbedBuilder embed = new EmbedBuilder();
            CronEmbedAttributes attributes = CreateTrialsVoteLockAttributes();
            embed.Title = attributes.Title;
            embed.Description = attributes.Description;
            string[] rgb = attributes.ColorCode.Split(",");
            embed.Color = new Color(int.Parse(rgb[0]), int.Parse(rgb[1]), int.Parse(rgb[2]));
            embed.AddField(attributes.EmbedFieldBuilder);

            await message.ModifyAsync(x =>
            {
                x.Embed = embed.Build();
            });

            await RespondAsync("Success", ephemeral: true);
        }

        [SlashCommand("confirm-trials-map", "Confirm weekly Trials Map")]
        public async Task ConfirmTrialsMap(string args)
        {
            if (Context.User.Id != 244209636897456129)
            {
                await RespondAsync("Failed - Only Jonny can do this command", ephemeral: true);
                return;
            }

            if (Emoji.TryParse(args, out Emoji confirmedEmoji) == false)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find emoji").Build());
                return;
            }

            int index = _alphabetList.IndexOf(confirmedEmoji);
            if (index == -1)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find emoji in list").Build());
                return;
            }

            string trialsMap = TrialsMaps[index];
            List<PlayerVote> winningPlayerVotes = await _trialsService.GetWinningTrialsGuesses(confirmedEmoji.Name);
            if (winningPlayerVotes == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find trials vote").Build());
                return;
            }
            else if (winningPlayerVotes.Count == 0)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply($"Nobody correctly guessed {trialsMap}. Better luck next week!").Build());
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($"Congratulations to <@{winningPlayerVotes[0].Id}>, you were first to guess the correct map **{trialsMap}**!");
                sb.AppendLine("");
                sb.AppendJoin(", ", winningPlayerVotes.Select(x => x.Username));
                sb.Append(" have been awarded 5 Glimmer for guessing correctly, everyone else has received 1 Glimmer for entering!");

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
                    await _guardianService.IncreaseGlimmerAsync(playerVote.Id, playerVote.Username, 1);
                }

                foreach (PlayerVote playerVote in winningPlayerVotes)
                {
                    await _guardianService.IncreaseGlimmerAsync(playerVote.Id, playerVote.Username, 5);
                }

                await RespondAsync(sb.ToString());
            }
        }

        //TODO
        //public string GetLastWeeksMap()
        //{

        //}

        private CronEmbedAttributes CreateTrialsVoteEmbedAttributes()
        {
            CronEmbedAttributes cronEmbedAttributes = new CronEmbedAttributes();
            cronEmbedAttributes.Title = "Trials Vote";
            cronEmbedAttributes.Description = "Vote for the map you think will be the Trials map. Only your first vote will count";
            cronEmbedAttributes.ColorCode = "21,142,2";
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < TrialsMaps.Count; i++)
            {
                sb.AppendLine($"{_alphabetList[i]} - {TrialsMaps[i]}");
            }
            cronEmbedAttributes.EmbedFieldBuilder = new EmbedFieldBuilder() { Name = "Maps", Value = sb.ToString(), IsInline = true };
            return cronEmbedAttributes;
        }
        private CronEmbedAttributes CreateTrialsVoteLockAttributes()
        {
            CronEmbedAttributes cronEmbedAttributes = new CronEmbedAttributes();
            cronEmbedAttributes.Title = "Trials Voting has now ended";
            cronEmbedAttributes.Description = "";
            cronEmbedAttributes.ColorCode = "237,34,19";
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < TrialsMaps.Count; i++)
            {
                sb.AppendLine($"{_alphabetList[i]} - {TrialsMaps[i]}");
            }
            cronEmbedAttributes.EmbedFieldBuilder = new EmbedFieldBuilder() { Name = "Maps", Value = sb.ToString(), IsInline = true };
            return cronEmbedAttributes;
        }
        private List<string> TrialsMaps = new List<string>()
        {
            "Altar of Flame",
            "Bannerfall",
            "Burnout",
            "Cathedral of Dusk",
            "Disjunction",
            "Distant Shore",
            //"Endless Vale",
            "Eternity",
            "Exodus Blue",
            "Fragment",
            "Javelin-4",
            "Midtown",
            "Pacifica",
            "Radiant Cliffs",
            "Rusted Lands",
            "The Dead Cliffs",
            "The Fortress",
            "Twilight Gap",
            "Vostok",
            "Widow’s Court",
            "Wormhaven",
        };
    }
}
