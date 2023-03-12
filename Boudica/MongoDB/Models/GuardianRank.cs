using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class GuardianRank
    {
        public ulong Id { get; set; }
        public string Username { get; set; }
        public int XP { get; set; }
        public DateTime DateTimeLastInteractedWithDiscord { get; set; }
    }
}
