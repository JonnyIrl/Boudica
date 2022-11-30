using Boudica.Services;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Attributes
{
    public class AccountLinked: PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            APIService apiService = services.GetRequiredService<APIService>();
            if(await apiService.IsExistingLinkedUser(context.User.Id) == false)
            {
                return await Task.FromResult(PreconditionResult.FromError("You need to link your Destiny account, get started with the /link command."));
            }
            else
            {
                if(await apiService.RefreshCode(await apiService.GetLinkedUser(context.User.Id)) == null)
                {
                    return await Task.FromResult(PreconditionResult.FromError("Could not refresh code for user, try again or if the issue persists use the /link command again."));
                }

                return await Task.FromResult(PreconditionResult.FromSuccess());
            }
        }
    }
}
