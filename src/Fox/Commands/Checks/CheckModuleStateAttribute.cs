using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using Fox.Databases.Entities.Models;
using Fox.Databases.Enums;
using Fox.Entities;
using Qmmands;

namespace Fox.Commands.Checks
{
    public sealed class CheckModuleStateAttribute : FoxCheckBaseAttribute
    {
        public override string Name { get; set; } = "Module State";
        public override string Details { get; set; } = "(Module can be enabled, disabled, protected)";

        public override Task<CheckResult> CheckAsync(ICommandContext context, IServiceProvider provider)
        {
            if (!(context is FoxCommandContext ctx))
            {
                return Task.FromResult(new CheckResult("Invalid command context."));
            }

            if (ctx.Configuration.BypassChecks && ctx.Configuration.OwnerIds.Contains(ctx.User.Id))
            {
                return Task.FromResult(CheckResult.Successful);
            }

            if (ctx.Guild == null)
            {
                return Task.FromResult(CheckResult.Successful);
            }

            if (ctx.DatabaseContext.Channel.Modules.TryGetValue(Module.Name, out var config))
            {
                switch (config.State)
                {
                    case ModuleState.Enabled:
                        return Task.FromResult(CheckResult.Successful);
                    case ModuleState.Protected:
                        return Task.FromResult(ctx.Member.PermissionsIn(ctx.Channel).HasPermission(config.Permissions)
                            ? CheckResult.Successful
                            : new CheckResult($"The module is disabled and restricted to users with following permissions: {config.Permissions.ToPermissionString()}. Use '{ctx.Prefix}help module' for more information."));
                    default:
                        return Task.FromResult(new CheckResult($"The module is disabled. Use '{ctx.Prefix}help module' for more information."));
                }
            }
            
            config = new ModuleConfiguration
            {
                Permissions = Permissions.None,
                State = ModuleState.Enabled,
                Name = Module.Name
            };

            ctx.DatabaseContext.Channel.Modules.TryAdd(Module.Name, config);
            ctx.Database.ChannelManager.Update(ctx.DatabaseContext.Channel);

            return Task.FromResult(new CheckResult());
        }
    }
}
