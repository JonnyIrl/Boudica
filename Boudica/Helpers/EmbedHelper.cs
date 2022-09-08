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

        public static void UpdateFooterOnEmbed(EmbedBuilder modifiedEmbed, IEmbed? embed)
        {
            if (embed == null) return;
            if (embed.Footer != null)
            {
                modifiedEmbed.Footer = new EmbedFooterBuilder()
                {
                    Text = embed.Footer.Value.ToString()
                };
            }
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
