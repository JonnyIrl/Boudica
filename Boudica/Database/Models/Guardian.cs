using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Database.Models
{
    [Table(nameof(Guardian))]
    public class Guardian
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int Glimmer { get; set; }
        public int ReputationLevel { get; set; }
        [NotMapped]
        public int DiscordUserId { get; set; }
    }
}
