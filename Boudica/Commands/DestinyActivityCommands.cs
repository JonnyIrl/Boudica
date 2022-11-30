using Boudica.MongoDB.Models;
using Discord.Interactions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Commands
{
    public class DestinyActivityCommands: InteractionModuleBase<SocketInteractionContext>
    {
        
        public DestinyActivityCommands(IServiceProvider services, IConfiguration configuration)
        {

        }

        private async Task GetRecentActivities(Guardian guardian)
        {
            if (guardian == null) return;



        }

    }
}
