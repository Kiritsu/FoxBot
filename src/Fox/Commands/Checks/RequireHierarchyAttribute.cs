using DSharpPlus.Entities;
using Fox.Entities;
using Fox.Extensions;
using Qmmands;
using System;
using System.Threading.Tasks;

namespace Fox.Commands.Checks
{
    public sealed class RequireHierarchyAttribute : ParameterCheckBaseAttribute
    { 
        public object Target { get; private set; }

        public override Task<CheckResult> CheckAsync(object argument, ICommandContext context, IServiceProvider provider)
        {
            if (!(context is FoxCommandContext ctx))
            {
                return Task.FromResult(CheckResult.Unsuccessful("Invalid command context."));
            }

            if (!(argument is DiscordMember mbr))
            {
                return Task.FromResult(CheckResult.Unsuccessful("The argument was not a DiscordMember"));
            }

            return ctx.Member.Hierarchy > mbr.Hierarchy && ctx.Guild.CurrentMember.Hierarchy > mbr.Hierarchy ? Task.FromResult(CheckResult.Successful) : Task.FromResult(CheckResult.Unsuccessful($"Sorry. {mbr.FormatUser()} is protected."));
        }
    }
}
