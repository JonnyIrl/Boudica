using Discord;
using Discord.Commands;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Commands
{
    public class TestCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private const string Prefix = ";";
        //[Command("test role")]
        //public async Task WriteId([Remainder] string args)
        //{
        //    args.Replace("<", string.Empty).Replace(">", string.Empty).Replace("@", string.Empty);
        //    await ReplyAsync(args);
        //}

        public SelectMenuComponent GetModalOptions()
        {
            return new SelectMenuBuilder()
            .WithPlaceholder("Select a raid")
            .WithCustomId("raid-menu")
            .WithMinValues(1)
            .WithMaxValues(1)
            .AddOption(null, "opt-kf", "Kings Fall")
            .AddOption(null, "opt-vow", "Vow of the Disciple")
            .AddOption(null, "opt-dsc", "Deep Stone Crypt")
            .AddOption(null, "opt-vog", "Vault of Glass")
            .AddOption(null, "opt-gos", "Garden of Salvation")
            .AddOption(null, "opt-lw", "Last Wish")
            .Build();
        }

        [SlashCommand("create raid", "Create a raid using a modal")]
        public async Task CreateRaid()
        {
            var mb = new ModalBuilder()
                .WithTitle("Create a Raid")
                .WithCustomId("raid_menu")
                .AddComponents(new List<IMessageComponent>() { GetModalOptions() }, 0)
                .AddTextInput("When?", "when", placeholder: "Today 7pm", required: true)
                .AddTextInput("Description", "description", TextInputStyle.Paragraph, placeholder: "Some information about the raid - not required");

            await Context.Interaction.RespondWithModalAsync(mb.Build());
        }
    }
}
