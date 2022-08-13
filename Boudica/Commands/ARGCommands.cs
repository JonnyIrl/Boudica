using Boudica.Database;
using Boudica.Services;
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
    public class ARGCommands : ModuleBase
    {
        private readonly GuardianService _db;
        private readonly IConfiguration _config;

        public ARGCommands(IServiceProvider services)
        {
            _db = services.GetRequiredService<GuardianService>();
            _config = services.GetRequiredService<IConfiguration>();
        }

        [Command("increase")]
        public async Task IncreaseGlimmer([Remainder] string args)
        {
            if (args == null || args.Contains("glimmer") == false)
            {
                await ReplyAsync("Invalid command, example is ;increase glimmer 50 @Person");
                return;
            }
            var split = args.Split(" ");
            if (split.Length < 3) 
            {
                await ReplyAsync("Invalid command, example is ;increase glimmer 50 @Person");
                return;
            }

            if (int.TryParse(split[1], out int result))
            {
                string userIdString = split[2].Replace("@", string.Empty).Replace(">", string.Empty).Replace("<", string.Empty);
                if (ulong.TryParse(userIdString, out ulong userId))
                {
                    await _db.IncreaseGlimmer(userId, result);
                    return;
                }
                else
                {
                    await ReplyAsync("Invalid command, example is ;increase glimmer 50 @Person");
                    return;
                }
            }

            await ReplyAsync("Invalid command, example is ;increase glimmer 50 @Person");

        }
    }
}
