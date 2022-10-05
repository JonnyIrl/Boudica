using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Commands
{
    public class CrucibleCommands
    {
        public CrucibleCommands(IServiceProvider services)
        {

        }

        private List<string> TrialsMaps = new List<string>()
        {
            "Altar of Flame",
            "Bannerfall",
            "Convergence",
            "Distant Shore",
            "Eternity",
            "Endless Vale",
            "The Burnout",
            "The Dead Cliffs",
            "Javelin-4",
            "Midtown",
            "Wormhaven"
        };
    }
}
