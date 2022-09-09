using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Database.Models
{
    [Table(nameof(RaidGroup))]
    public class RaidGroup
    {
        public int Id { get; set; }
        public int RaidId { get; set; }
        public string UserId { get; set; }
        public bool IsPlayer { get; set; }
        public bool IsSubstitute { get; set; }

        public virtual Raid Raid { get; set; }
    }
}
