using System;
using System.Threading.Tasks;
using DSharpPlus;
using Fox.Entities;
using Qmmands;

namespace Fox.Commands.Checks
{
    public sealed class RequireBotPermissionsAttribute : FoxCheckBaseAttribute
    {
        public Permissions Permissions { get; }
        public override string Name { get; set; }
        public override string Details { get; set; }

        public RequireBotPermissionsAttribute(Permissions permissions)
        {
            Permissions = permissions;
            Name = "Bot permissions";
            Details = $"({permissions.ToPermissionString()})";
        }

        public override Task<CheckResult> CheckAsync(ICommandContext context, IServiceProvider provider)
        {
            if (!(context is FoxCommandContext ctx))
            {
                return Task.FromResult(new CheckResult("Invalid command context."));
            }

            if (ctx.Guild == null)
            {
                return Task.FromResult(CheckResult.Successful);
            }

            if (ctx.Guild.CurrentMember.IsOwner)
            {
                return Task.FromResult(CheckResult.Successful);
            }

            var perms = ctx.Guild.CurrentMember.PermissionsIn(ctx.Channel);

            if (perms.HasPermission(Permissions.Administrator))
            {
                return Task.FromResult(CheckResult.Successful);
            }

            if (perms.HasPermission(Permissions))
            {
                return Task.FromResult(CheckResult.Successful);
            }

            return Task.FromResult(new CheckResult($"I need the following permissions: {Permissions.ToPermissionString()}"));
        }
    }
}


