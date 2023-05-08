using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Classes
{
    public class RaidReminderMessage
    {
        public ulong UserId { get; set; }
        public DateTime DateTimePlanned { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int PlayerCount { get; set; }
    }
}
