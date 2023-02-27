using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Classes
{
    public class UserRoleExport
    {
        public ulong Id { get; set; }
        public string Username { get; set; }
        public bool Raid { get; set; }
        public bool Nightfall { get; set; }
        public bool Crucible { get; set; }
        public bool Gambit { get; set; }
        public bool Activity { get; set; }
        public bool Dungeon { get; set; }

        public override string ToString()
        {
            return $"{Id},{Username},{Raid},{Nightfall},{Crucible},{Gambit},{Activity},{Dungeon}";
        }
    }
}
