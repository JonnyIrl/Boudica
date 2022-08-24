using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Classes
{
    public class PlayerName
    {
        public ulong UserId { get; set; }

        public PlayerName(ulong userId)
        {
            UserId = userId;
        }
    }
}
