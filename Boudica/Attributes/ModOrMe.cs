using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Attributes
{
    public class ModOrMe : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            const string Moderator = "Moderator";
            const ulong MyId = 244209636897456129;
            IGuildUser guildUser = (IGuildUser)(context.User);
            IRole modRole = context.Guild.Roles.FirstOrDefault(x => x.Name == Moderator);
            if(context.User.Id == MyId || (modRole != null && guildUser.RoleIds.Contains(modRole.Id)))
                return await Task.FromResult(PreconditionResult.FromSuccess());
            else
                return await Task.FromResult(PreconditionResult.FromError($"You do not have permission to issue this command"));
        }
    }
}
