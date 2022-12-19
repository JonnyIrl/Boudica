using Discord;
using Discord.Interactions;
using Discord.Interactions.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Classes
{
    public class InteractionModuleMock : IInteractionModuleMock
    {
        public void AfterExecute(ICommandInfo command)
        {
            return;
        }

        public Task AfterExecuteAsync(ICommandInfo command)
        {
            return Task.CompletedTask;
        }

        public void BeforeExecute(ICommandInfo command)
        {
            return;
        }

        public Task BeforeExecuteAsync(ICommandInfo command)
        {
            return Task.CompletedTask;
        }

        public void Construct(ModuleBuilder builder, InteractionService commandService)
        {
            return;
        }

        public void OnModuleBuilding(InteractionService commandService, ModuleInfo module)
        {
            return;
        }

        public void SetContext(IInteractionContext context)
        {
            return;
        }
    }
}
