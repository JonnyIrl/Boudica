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
                .WithCustomId($"{(int)ButtonCustomId.EditRaid}-{newRaid.Id}")
                .WithTitle("Edit Raid")
                .AddTextInput("Title", $"{(int)ModalInputType.InputTitle}", TextInputStyle.Short, value: existingTitle, required: false, maxLength: 250)
                .AddTextInput("Description", $"{(int)ModalInputType.InputDescription}", TextInputStyle.Paragraph, value: existingDescription, required: false, maxLength: 400);
            return builder.Build();
        }

        public static Modal CreateRaidModal()
        {
            ModalBuilder builder = new ModalBuilder()
                .WithCustomId($"{(int)ButtonCustomId.CreateRaid}-0")
                .WithTitle("Create Raid")
                .AddTextInput("Title", $"{(int)ModalInputType.InputTitle}", TextInputStyle.Short, required: false, maxLength: 250)
                .AddTextInput("Description", $"{(int)ModalInputType.InputDescription}", TextInputStyle.Paragraph, required: false, maxLength: 400);
            return builder.Build();
        }

        public static Modal CreateFireteamModal()
        {
            ModalBuilder builder = new ModalBuilder()
                .WithCustomId($"{(int)ButtonCustomId.CreateFireteam}-0")
                .WithTitle("Create Fireteam")
                .AddTextInput("Title", $"{(int)ModalInputType.InputTitle}", TextInputStyle.Short, required: false, maxLength: 250)
                .AddTextInput("Description", $"{(int)ModalInputType.InputDescription}", TextInputStyle.Paragraph, required: false, maxLength: 400)
                .AddTextInput("Fireteam Size (Number between 2 and 6)", $"{(int)ModalInputType.FireteamSize}", TextInputStyle.Paragraph, minLength: 1, maxLength: 1, required: true);
            return builder.Build();
        }

        public static Modal EditFireteamModal(Fireteam newFireteam, string existingTitle, string existingDescription)
        {
            ModalBuilder builder = new ModalBuilder()
                 .WithCustomId($"{(int)ButtonCustomId.EditFireteam}-{newFireteam.Id}")
                 .WithTitle("Edit Fireteam")
                 .AddTextInput("Title", $"{(int)ModalInputType.InputTitle}", TextInputStyle.Short, value: existingTitle, required: false, maxLength: 250)
                 .AddTextInput("Description", $"{(int)ModalInputType.InputDescription}", TextInputStyle.Paragraph, value: existingDescription, required: false, maxLength: 400);
            return builder.Build();
        }
    }
}
