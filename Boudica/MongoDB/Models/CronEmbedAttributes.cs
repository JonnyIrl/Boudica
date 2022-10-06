using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class CronEmbedAttributes
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public EmbedFieldBuilder EmbedFieldBuilder { get; set; }
        public string ColorCode { get; set; }
    }
}
