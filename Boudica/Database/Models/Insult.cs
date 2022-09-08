using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Database.Models
{
    [Table(nameof(Insult))]
    public class Insult
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public DateTime DateTimeLastInsulted { get; set; }
    }
}
