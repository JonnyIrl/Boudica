using Boudica.MongoDB.Models;
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
    public class Suspended : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            MiscService miscService = services.GetRequiredService<MiscService>();
            SuspendedUser suspendedUser = await miscService.IsSuspended(context.User.Id);
            if (suspendedUser != null)
            {
                return await Task.FromResult(PreconditionResult.FromError($"You are suspended from making this command until {suspendedUser.DateTimeSuspendedUntil.ToString("dd/MM/yyyy HH:mm")}"));
            }
            else
            {
                return await Task.FromResult(PreconditionResult.FromSuccess());
            }
        }
    }
}
