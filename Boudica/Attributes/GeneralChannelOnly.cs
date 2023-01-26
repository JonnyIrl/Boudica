using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Attributes
{
    public class GeneralChannelOnly : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
#if DEBUG
            ulong generalChannelId = 958852217186713683;
#else
            ulong generalChannelId = 530528343666458663;
#endif
            if (context.Channel.Id != generalChannelId)
            {
                return await Task.FromResult(PreconditionResult.FromError($"You can only issue this command in <#{generalChannelId}>"));
            }

            return await Task.FromResult(PreconditionResult.FromSuccess());           
        }
    }
}
