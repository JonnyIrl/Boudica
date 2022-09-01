using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Database.Models
{
    [Table(nameof(GuardianReputation))]
    public class GuardianReputation
    {
        public int Id { get; set; }
        public int GuardianId { get; set; }
        public DateTime DateTimeAwarded { get; set; }
        public int Amount { get; set; }
        public virtual Guardian Guardian { get; set; }
    }
}
