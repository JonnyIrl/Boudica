using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Database.Models
{
    [Table("GuardianInventory")]
    public class GuardianInventory
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int GuardianId { get; set;}
        public int ItemId { get; set; }
        public virtual Guardian Guardian { get; set; }
        public virtual Item Item { get; set; }
    }
}
