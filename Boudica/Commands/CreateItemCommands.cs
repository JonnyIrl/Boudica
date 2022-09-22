using Boudica.Services;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Commands
{
    public class CreateItemCommands: ModuleBase
    {
        private readonly IConfiguration _config;

        public CreateItemCommands(IServiceProvider services)
        {
            _config = services.GetRequiredService<IConfiguration>();
        }
    }
}
