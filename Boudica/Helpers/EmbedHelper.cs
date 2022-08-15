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
    }
}
