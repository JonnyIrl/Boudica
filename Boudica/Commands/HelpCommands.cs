using Boudica.Helpers;
using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Commands
{
    public class HelpCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private const string Prefix = "/";

        [SlashCommand("link", "Link your Bungie account to your Discord account.")]
        public async Task Link()
        {
            //await DeferAsync();
            var foot = new EmbedFooterBuilder()
            {
                Text = $"Powered by Boudica"
            };
            var auth = new EmbedAuthorBuilder()
            {
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl(),
                Name = "Account Linking"
            };
            var embed = new EmbedBuilder()
            {
                Color = Color.Green,
                Footer = foot,
                Author = auth
            };
            var plainTextBytes = Encoding.UTF8.GetBytes($"{Context.User.Id}");
            string state = Convert.ToBase64String(plainTextBytes);

            string clientId = BoudicaConfig.BungieClientId;
            embed.Title = $"Click here to start the linking process.";
            embed.Url = $"https://www.bungie.net/en/OAuth/Authorize?client_id={clientId}&response_type=code&state={state}";
            embed.Description = $"- Linking allows you to participate in daily/weekly challenges and more. It will be required to participate in the new Lightfall clan event\n" +
                $"- After linking is complete, you'll receive another DM from me to confirm.\n" +
                $"- Experienced a name change? Relinking will update your name.";

            var buttonBuilder = new ComponentBuilder()
                .WithButton("Link with Boudica", style: ButtonStyle.Link, url: $"https://www.bungie.net/en/OAuth/Authorize?client_id={clientId}&response_type=code&state={state}", row: 0);

            await RespondAsync(embed: embed.Build(), components: buttonBuilder.Build(), ephemeral: true);
        }


        [SlashCommand("help", "Show all available commands")]
        public async Task HelpCommand(bool showResultsOnlyToMe = true)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = "Here's a list of commands!";
            builder.Description = $"\n";
            builder.AddField("Raid Commands", RaidCommands());
            builder.AddField("Fireteam Commands", FireteamCommands());
            builder.AddField("Loadout Commands", LoadoutCommands());
            builder.AddField("Other Commands", MiscCommands());
            builder.WithColor(Color.Green);
            await RespondAsync(embed: builder.Build(), ephemeral: showResultsOnlyToMe);
        }

        private string RaidCommands()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"- Create Raid: {Prefix}create raid some text to describe your raid");
            sb.AppendLine($"- Create Raid with existing Players: {Prefix}create raid some text to describe your raid @Person1 @Person2");
            sb.AppendLine($"- Edit Raid: {Prefix}edit raid (Id Number) some new text here");
            sb.AppendLine($"- Add Player to Raid: {Prefix}add-player raid (Id number) @Player");
            sb.AppendLine($"- Remove Player from Raid: {Prefix}remove-player raid (Id number) @Player");
            sb.AppendLine($"- Close Raid: {Prefix}close raid (Id number)");
            sb.AppendLine($"- Alert Raid: {Prefix}alert raid (Id number) - This will @ all the members of the Raid to make sure they are still good to raid.");
            return sb.ToString();
        }

        private string FireteamCommands()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"- Create Fireteam: {Prefix}create fireteam (the number of players between 2 and 6 inclusive) some text to describe your fireteam");
            sb.AppendLine($"- Create Fireteam with existing Players: {Prefix}create fireteam (the number of players between 2 and 6 inclusive) some text to describe your fireteam @Person1 @Person2");
            sb.AppendLine($"- Edit Fireteam: {Prefix}edit fireteam (Id number) some new text here");
            sb.AppendLine($"- Add Player to Fireteam: {Prefix}add-player fireteam (Id number) @Player");
            sb.AppendLine($"- Remove Player from Fireteam: {Prefix}remove-player fireteam (Id number) @Player");
            sb.AppendLine($"- Close Fireteam: {Prefix}close fireteam (Id number)");
            return sb.ToString();
        }

        private string MiscCommands()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"- Glimmer Leaderboard: {Prefix}leaderboard");
            sb.AppendLine($"- Award Player: {Prefix}award @Somebody");
            sb.AppendLine($"- Insult Somebody: {Prefix}insult @Somebody");
            sb.AppendLine($"- Compliment Somebody: {Prefix}compliment @Somebody");
            sb.AppendLine($"- Random Joke: {Prefix}joke");
            sb.AppendLine($"- Coinflip: {Prefix}coinflip");
            sb.AppendLine($"- Magic 8 Ball: {Prefix}ask whatever question you may have");
            return sb.ToString();
        }

        private string LoadoutCommands()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"- Random Loadout PvE: {Prefix}random-loadout-pve");
            return sb.ToString();
        }
    }
}
