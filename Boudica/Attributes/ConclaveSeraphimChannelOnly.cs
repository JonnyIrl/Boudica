using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Attributes
{
    internal class ConclaveSeraphimChannelOnly : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
#if DEBUG
            ulong seraphimId = 958852217186713683;
            ulong conclaveId = 1014433494216228905;
            ulong ivoryTower = 1006681250616049665;
#else
            ulong seraphimId = 779155150286618654;
            ulong conclaveId = 1006680537479512095;
            ulong ivoryTower = 1006681250616049665;
#endif
            if (context.Channel.Id != seraphimId && context.Channel.Id != conclaveId && context.Channel.Id != ivoryTower)
            {
                return await Task.FromResult(PreconditionResult.FromError($"You can only issue this command in <#{conclaveId}>, <#{seraphimId}> and <#{ivoryTower}>"));
            }

            return await Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}
