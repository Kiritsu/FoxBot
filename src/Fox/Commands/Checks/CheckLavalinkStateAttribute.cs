using System;
using System.Threading.Tasks;
using Fox.Entities;
using Fox.Services;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Fox.Commands.Checks
{
    public sealed class CheckLavalinkStateAttribute : FoxCheckBaseAttribute
    {
        public override string Name { get; set; } = "Lavalink Status";
        public override string Details { get; set; }

        public override Task<CheckResult> CheckAsync(ICommandContext context, IServiceProvider provider)
        {
            if (!(context is FoxCommandContext ctx))
            {
                return Task.FromResult(new CheckResult("Invalid command context."));
            }

            if (!provider.GetService<MusicService>().Started)
            {
                return Task.FromResult(new CheckResult("Lavalink is not started. Please contact my owner for more informations."));
            }

            return Task.FromResult(CheckResult.Successful);
        }
    }
}
