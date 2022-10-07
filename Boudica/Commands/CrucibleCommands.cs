using Boudica.Helpers;
using Boudica.MongoDB.Models;
using Boudica.Services;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Commands
{
    public class CrucibleCommands : ModuleBase
    {
        private CronService _cronService;
        private readonly TrialsService _trialsService;
        private List<Emoji> _alphabetList;

        public CrucibleCommands(IServiceProvider services)
        {
            _cronService = services.GetRequiredService<CronService>();
            _trialsService = services.GetRequiredService<TrialsService>();
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

        [Command("create trials vote")]
        public async Task CreateTrialsTask()
        {
            if (Context.User.Id != 244209636897456129) return;

            bool createdTrialsVote = await _trialsService.CreateWeeklyTrialsVote();
            if(createdTrialsVote == false)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Failed to create weekly trials vote").Build());
            }

            EmbedBuilder embed = new EmbedBuilder();
            CronEmbedAttributes attributes = CreateTrialsVoteEmbedAttributes();
            embed.Title = attributes.Title;
            embed.Description = attributes.Description;
            string[] rgb = attributes.ColorCode.Split(",");
            embed.Color = new Color(int.Parse(rgb[0]), int.Parse(rgb[1]), int.Parse(rgb[2]));
            embed.AddField(attributes.EmbedFieldBuilder) ;
            IUserMessage message = await ReplyAsync(null, false, embed.Build());

            await _trialsService.UpdateMessageId(message.Id);
            await message.AddReactionsAsync(_alphabetList.Take(TrialsMaps.Count));
        }

        [Command("lock trials vote")]
        public async Task LockTrialsTask()
        {
            if (Context.User.Id != 244209636897456129) return;

            TrialsVote trialsVote = await _trialsService.LockTrialsVote();
            if (trialsVote == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Failed to lock").Build());
                return;
            }

            await ReplyAsync(null, false, EmbedHelper.CreateSuccessReply("Voting is now closed").Build());
            

            IUserMessage message = (IUserMessage)await Context.Channel.GetMessageAsync(trialsVote.MessageId, CacheMode.AllowDownload);
            if (message == null)
            {
                await ReplyAsync(null, false, EmbedHelper.CreateFailedReply("Could not find message to close").Build());
                return;
            }
            
            var embedBuilder = new EmbedBuilder();
            embedBuilder.Color = Color.Red;
            embedBuilder.Title = "Trials Voting is now closed";
            embedBuilder.Description = string.Empty;

            await message.ModifyAsync(x =>
            {
                x.Embed = embedBuilder.Build();
            });
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
        private List<string> TrialsMaps = new List<string>()
        {
            "Altar of Flame",
            //"Bannerfall",
            "Burnout",
            "Cathedral of Dusk",
            "Disjunction",
            "Distant Shore",
            "Endless Vale",
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
