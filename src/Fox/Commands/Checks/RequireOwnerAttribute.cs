using System;
using System.Linq;
using System.Threading.Tasks;
using Fox.Entities;
using Qmmands;

namespace Fox.Commands.Checks
{
    public class RequireOwnerAttribute : FoxCheckBaseAttribute
    {
        public override string Name { get; set; } = "Bot owner";
        public override string Details { get; set; }

        public override async Task<CheckResult> CheckAsync(ICommandContext context, IServiceProvider provider)
        {
            if (!(context is FoxCommandContext ctx))
            {
                return new CheckResult("Invalid command context.");
            }

            if (ctx.Configuration.BypassChecks && ctx.Configuration.OwnerIds.Contains(ctx.User.Id))
            {
                return CheckResult.Successful;
            }

            var app = await ctx.Client.GetCurrentApplicationAsync();

            return ctx.User == app.Owner
                ? CheckResult.Successful
                : new CheckResult("The command can only be ran by the owner of the bot.");
        }
    }
}
