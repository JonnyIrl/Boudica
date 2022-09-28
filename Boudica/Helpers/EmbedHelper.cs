using Boudica.MongoDB.Models;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Helpers
{
    public static class EmbedHelper
    {

        public static Color InfoColor = new Color(255, 191, 0);
        public static EmbedBuilder CreateSuccessReply(string description)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.Description = description;
            builder.Color = Color.Green;
            return builder;
        }

        public static EmbedBuilder CreateFailedReply(string description)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.Description = description;
            builder.Color = Color.Red;
            return builder;
        }

        public static EmbedBuilder CreateInfoReply(string description)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.Description = description;
            builder.Color = InfoColor;
            return builder;
        }

        public static void UpdateAuthorOnEmbed(EmbedBuilder modifiedEmbed, IEmbed? embed)
        {
            if (embed == null) return;
            EmbedAuthor? author = embed.Author;
            if (author != null)
            {
                EmbedAuthor realAuthor = (EmbedAuthor)embed.Author;
                modifiedEmbed.Author = new EmbedAuthorBuilder()
                {
                    Name = realAuthor.Name,
                    IconUrl = realAuthor.IconUrl,
                    Url = realAuthor.Url
                };
            }
        }

        public static void UpdateTitleColorOnEmbed(EmbedBuilder modifiedEmbed, IEmbed? embed)
        {
            if (embed == null) return;
            modifiedEmbed.Title = embed.Title;
            modifiedEmbed.Color = embed.Color;
        }

        public static void UpdateDescriptionTitleColorOnEmbed(EmbedBuilder modifiedEmbed, IEmbed? embed)
        {
            if (embed == null) return;
            modifiedEmbed.Description = embed.Description;
            modifiedEmbed.Title = embed.Title;
            modifiedEmbed.Color = embed.Color;
        }

        public static void UpdateFooterOnEmbed(EmbedBuilder modifiedEmbed, Raid raid)
        {
            modifiedEmbed.Footer = new EmbedFooterBuilder()
            {
                Text = $"Raid Id {raid.Id}\nUse J to Join | Use S to Sub.\nA max of 6 players may join a raid\n{GetGlimmerMessage(raid.Players)}"
            };
        }

        public static void UpdateFooterOnEmbed(EmbedBuilder modifiedEmbed, Fireteam fireteam)
        {
            modifiedEmbed.Footer = new EmbedFooterBuilder()
            {
                Text = $"Fireteam Id {fireteam.Id}\nUse J to Join | Use S to Sub.\nA max of {fireteam.MaxPlayerCount} players may join a fireteam\n{GetGlimmerMessage(fireteam.Players)}"
            };
        }

        private static string GetGlimmerMessage(List<ActivityUser> activityUsers)
        {
            if (activityUsers == null || activityUsers.Any() == false) return string.Empty;
            StringBuilder sb = new StringBuilder();
            if (activityUsers.Count(x => x.Reacted) <= 0) return string.Empty;
            sb.AppendJoin(", ", activityUsers.Where(x => x.Reacted).Select(x => x.DisplayName));
            sb.Append(" will get Glimmer for completing this activity.");
            return sb.ToString();
        }

        public static void UpdateFieldsOnEmbed(EmbedBuilder modifiedEmbed, IEmbed? embed)
        {
            if (embed == null) return;
            if (embed.Fields == null) return;
            foreach(EmbedField field in embed.Fields)
            {
                modifiedEmbed.AddField(field.Name, field.Value);
            }
        }
    }
}
