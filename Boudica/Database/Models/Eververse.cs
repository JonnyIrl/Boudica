using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Database.Models
{
    [Table("Eververse")]
    public class Eververse
    {
        public int Id { get; set; }
        public int Price { get; set; }
        public virtual Item Item { get; set; }
    }
}
