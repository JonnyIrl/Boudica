using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Attributes
{
    public class BotChannelOnly : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
#if DEBUG
            ulong botChannelId = 958852217186713683;
#else
            ulong botChannelId = 1072104741359853649;
#endif
            if (context.Channel.Id != botChannelId)
            {
                return await Task.FromResult(PreconditionResult.FromError($"You can only issue this command in <#{botChannelId}>"));
            }

            return await Task.FromResult(PreconditionResult.FromSuccess());           
        }
    }
}
