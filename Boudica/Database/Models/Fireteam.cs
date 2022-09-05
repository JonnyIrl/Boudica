using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Database.Models
{
    [Table(nameof(Fireteam))]
    public class Fireteam
    {
        public int Id { get; set; }
        public string CreatedByUserId { get; set; }
        public int Slots { get; set; }
        public DateTime? DateTimeCreated { get; set; }
        public DateTime? DateTimeClosed { get; set; }
        public string MessageId { get; set; }
        public string ChannelId { get; set; }
    }
}
