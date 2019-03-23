using System;
using System.Linq;
using System.Threading.Tasks;
using Fox.Entities;
using Qmmands;

namespace Fox.Commands.Checks
{
    public sealed class RequireDjAttribute : FoxCheckBaseAttribute
    {
        public override string Name { get; set; } = "DJ Mode Only";
        public override string Details { get; set; } = "The module can only be used by people with the role 'DJ'";

        public override Task<CheckResult> CheckAsync(ICommandContext context, IServiceProvider provider)
        {
            if (!(context is FoxCommandContext ctx))
            {
                return Task.FromResult(new CheckResult("Invalid command context."));
            }

            if (ctx.DatabaseContext.Guild.Music.DjOnly && !ctx.Member.Roles.Any(x => x.Name.Equals("DJ", StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult(new CheckResult("The DJ Only Mode is enabled. You need a role named 'DJ' to use the module."));
            }

            return Task.FromResult(CheckResult.Successful);
        }
    }
}
