using Boudica.Services;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Commands
{
    public class CrucibleCommands : ModuleBase
    {
        private CronService _cronService;
        public CrucibleCommands(IServiceProvider services)
        {
            _cronService = services.GetRequiredService<CronService>();
        }

        [Command("create trials task")]
        public async Task CreateTrialsTask()
        {
            await _cronService.CreateTrialsTask();
        }

        private List<string> TrialsMaps = new List<string>()
        {
            "Altar of Flame",
            "Bannerfall",
            "Burnout",
            "Cathedral of Dusk",
            "Disjunction",
            "Distant Shore",
            "Endless Vale",
            "Eternity",
            "Exodus Blue",
            "Fragment",
            "Javelin-4",
            "Midtown",
            "Pacifica",
            "Radiant Cliffs",
            "Rusted Lands",
            "The Dead Cliffs",
            "The Fortress",
            "Twilight Gap",
            "Vostok",
            "Widow’s Court",          
            "Wormhaven",      
        };
    }
}
