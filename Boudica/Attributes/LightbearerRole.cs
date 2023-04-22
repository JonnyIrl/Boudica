using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Attributes
{
    internal class LightbearerRole : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            const string Lightbearer = "Lightbearer";
#if DEBUG
            const string WelcomeChannelId = "958852217186713683";
#else 
            const string WelcomeChannelId = "530536477151592469";
#endif

            IGuildUser guildUser = (IGuildUser)(context.User);
            IRole lightbearerRole = context.Guild.Roles.FirstOrDefault(x => x.Name == Lightbearer);
            if (lightbearerRole != null && guildUser.RoleIds.Contains(lightbearerRole.Id))
                return await Task.FromResult(PreconditionResult.FromSuccess());
            else
                return await Task.FromResult(PreconditionResult.FromError($"You must have {lightbearerRole.Mention} to do this. You can react with this role in <#{WelcomeChannelId}> to get it."));
        }
    }
}
