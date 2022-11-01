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
                .AddTextInput("Title", $"{(int)ModalInputType.InputTitle}", TextInputStyle.Short, value: existingTitle, maxLength: 250)
                .AddTextInput("Description", $"{(int)ModalInputType.InputDescription}", TextInputStyle.Paragraph, value: existingDescription, maxLength: 400);
            return builder.Build();
        }

        public static Modal CreateRaidModal()
        {
            ModalBuilder builder = new ModalBuilder()
                .WithCustomId($"{(int)ButtonCustomId.CreateRaid}-0")
                .WithTitle("Create Raid")
                .AddTextInput("Title", $"{(int)ModalInputType.InputTitle}", TextInputStyle.Short, maxLength: 250)
                .AddTextInput("Description", $"{(int)ModalInputType.InputDescription}", TextInputStyle.Paragraph, maxLength: 400);
            return builder.Build();
        }
    }
}
