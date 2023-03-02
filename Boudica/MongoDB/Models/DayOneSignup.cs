using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class DayOneSignup
    {
        public ulong Id { get; set; }
        public string UserName { get; set; }
        public DateTime DateTimeSignedUp { get; set; }
        public bool OkToLFG { get; set; }

        public DayOneSignup(ulong id, string userName, bool lfg)
        {
            Id = id;
            UserName = userName;
            OkToLFG = lfg;
        }
    }
}
