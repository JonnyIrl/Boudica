using Boudica.Enums;
using Boudica.MongoDB.Models;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Helpers
{
    public static class ModalHelper
    {
        public static Modal EditRaidModal(Raid newRaid, string existingTitle, string existingDescription)
        {
            //$"{(int)ButtonCustomId.EditRaid}-{newRaid.Id}"
            ModalBuilder builder = new ModalBuilder()
                .WithCustomId($"{(int)CustomId.EditRaid}-{newRaid.Id}")
                .WithTitle("Edit Raid")
                .AddTextInput("Title", $"{(int)ModalInputType.InputTitle}", TextInputStyle.Short, value: existingTitle, required: false, maxLength: 250)
                .AddTextInput("Description", $"{(int)ModalInputType.InputDescription}", TextInputStyle.Paragraph, value: existingDescription, required: false, maxLength: 400);
            return builder.Build();
        }

        public static Modal CreateRaidModal()
        {
            ModalBuilder builder = new ModalBuilder()
                .WithCustomId($"{(int)CustomId.CreateRaid}-0")
                .WithTitle("Create Raid")
                .AddTextInput("Title", $"{(int)ModalInputType.InputTitle}", TextInputStyle.Short, required: false, maxLength: 250)
                .AddTextInput("Description", $"{(int)ModalInputType.InputDescription}", TextInputStyle.Paragraph, required: false, maxLength: 400)
                .AddTextInput("Alert Channel", $"{(int)ModalInputType.AlertChannel}", TextInputStyle.Short, required: true, maxLength: 3, value: "Yes");
            return builder.Build();
        }

        public static Modal CreateRaidModal(string existingPlayers)
        {
            ModalBuilder builder = new ModalBuilder()
                .WithCustomId($"{(int)CustomId.CreateRaid}-0")
                .WithTitle("Create Raid")
                .AddTextInput("Title", $"{(int)ModalInputType.InputTitle}", TextInputStyle.Short, required: false, maxLength: 250)
                .AddTextInput("Description", $"{(int)ModalInputType.InputDescription}", TextInputStyle.Paragraph, required: false, maxLength: 400)
                .AddTextInput("Alert Channel", $"{(int)ModalInputType.AlertChannel}", TextInputStyle.Short, required: true, maxLength: 3, value: "Yes")
                .AddTextInput("Existing Players (dont change anything here)", $"{(int)ModalInputType.ExistingPlayers}", TextInputStyle.Short, required: false, value: existingPlayers);
            return builder.Build();
        }

        public static Modal CreateFireteamModal()
        {
            ModalBuilder builder = new ModalBuilder()
                .WithCustomId($"{(int)CustomId.CreateFireteam}-0")
                .WithTitle("Create Fireteam")
                .AddTextInput("Title", $"{(int)ModalInputType.InputTitle}", TextInputStyle.Short, required: false, maxLength: 250)
                .AddTextInput("Description", $"{(int)ModalInputType.InputDescription}", TextInputStyle.Paragraph, required: false, maxLength: 400)
                .AddTextInput("Fireteam Size (Number between 2 and 6)", $"{(int)ModalInputType.FireteamSize}", TextInputStyle.Paragraph, minLength: 1, maxLength: 1, required: true)
                .AddTextInput("Alert Channel", $"{(int)ModalInputType.AlertChannel}", TextInputStyle.Short, required: true, maxLength: 3, value: "Yes");
            return builder.Build();
        }

        public static Modal CreateFireteamModal(string existingPlayers)
        {
            ModalBuilder builder = new ModalBuilder()
                .WithCustomId($"{(int)CustomId.CreateFireteam}-0")
                .WithTitle("Create Fireteam")
                .AddTextInput("Title", $"{(int)ModalInputType.InputTitle}", TextInputStyle.Short, required: false, maxLength: 250)
                .AddTextInput("Description", $"{(int)ModalInputType.InputDescription}", TextInputStyle.Paragraph, required: false, maxLength: 400)
                .AddTextInput("Fireteam Size (Number between 2 and 6)", $"{(int)ModalInputType.FireteamSize}", TextInputStyle.Paragraph, minLength: 1, maxLength: 1, required: true)
                .AddTextInput("Alert Channel", $"{(int)ModalInputType.AlertChannel}", TextInputStyle.Short, required: true, maxLength: 3, value: "Yes")
                .AddTextInput("Existing Players (dont change anything here)", $"{(int)ModalInputType.ExistingPlayers}", TextInputStyle.Short, required: false, value: existingPlayers);
            return builder.Build();
        }

        public static Modal EditFireteamModal(Fireteam newFireteam, string existingTitle, string existingDescription)
        {
            ModalBuilder builder = new ModalBuilder()
                 .WithCustomId($"{(int)CustomId.EditFireteam}-{newFireteam.Id}")
                 .WithTitle("Edit Fireteam")
                 .AddTextInput("Title", $"{(int)ModalInputType.InputTitle}", TextInputStyle.Short, value: existingTitle, required: false, maxLength: 250)
                 .AddTextInput("Description", $"{(int)ModalInputType.InputDescription}", TextInputStyle.Paragraph, value: existingDescription, required: false, maxLength: 400);
            return builder.Build();
        }

        public static Modal EditRockPaperScissorsModal(long sessionId)
        {
            ModalBuilder builder = new ModalBuilder()
                 .WithCustomId($"{(int)CustomId.EnterGuess}-{sessionId}")
                 .WithTitle("Rock Paper Scissors Guess")
                 .AddTextInput("For Rock type R, For Paper type P, For Scissors type S",
                 $"{(int)ModalInputType.Guess}", TextInputStyle.Short, required: true, maxLength: 1);
            return builder.Build();
        }
    }
}
