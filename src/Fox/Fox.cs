using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Lavalink;
using Fox.Databases;
using Fox.Databases.Migrations;
using Fox.Entities;
using Fox.Enums;
using Fox.Services;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Fox
{
    internal sealed class Fox
    {
        private static void Main()
        {
            new Fox().MainAsync().GetAwaiter().GetResult();
        }

        private async Task MainAsync()
        {
            var services = ConfigureServices();

            var lgr = services.GetRequiredService<LogService>();
            lgr.Print(LogLevel.Info, "Fox", "Fox is starting... Please wait a few moment.");

            var client = services.GetRequiredService<DiscordClient>();

            var cmd = services.GetRequiredService<CommandHandlerService>();
            cmd.Initialize();

            var events = services.GetRequiredService<EventHandlerService>();
            events.Initialize();

            var migrations = services.GetRequiredService<MigrationHelperService>();

            await migrations.MigrateCustomCommandsAsync();

            await client.ConnectAsync();

            await Task.Delay(Timeout.Infinite);
        }

        private IServiceProvider ConfigureServices()
        {
            var cfg = ConfigurationService.Initialize();

            var client = new DiscordClient(new DiscordConfiguration
            {
                Token = cfg.Keys.Discord
            });

            var cmds = new CommandService(new CommandServiceConfiguration
            {
                CooldownBucketKeyGenerator = GenerateBucketKey
            });

            var lavalink = client.UseLavalink();

            return new ServiceCollection()
                    .AddSingleton(cfg)
                    .AddSingleton(client)
                    .AddSingleton(cmds)
                    .AddSingleton<LogService>()
                    .AddSingleton<CommandHandlerService>()
                    .AddSingleton<EventHandlerService>()
                    .AddSingleton<FoxDb>()
                    .AddSingleton<Random>()
                    .AddSingleton<MusicService>()
                    .AddSingleton(lavalink)
                    .AddSingleton<MigrationHelperService>()
                    .AddSingleton<RiotService>()
                    .BuildServiceProvider();
        }

        public object GenerateBucketKey(Command command, object bucketType, ICommandContext context, IServiceProvider provider)
        {
            if (!(context is FoxCommandContext ctx))
            {
                throw new InvalidOperationException("Invalid command context.");
            }

            ctx.Command = ctx.Command ?? command;

            if (bucketType is CooldownBucketType bucket)
            {
                var obj = "";

                switch (bucket)
                {
                    case CooldownBucketType.Guild:
                        obj += ctx.Guild?.Id ?? ctx.User.Id;
                        break;
                    case CooldownBucketType.Channel:
                        obj += ctx.Channel.Id;
                        break;
                    case CooldownBucketType.User:
                        obj += ctx.User.Id;
                        break;
                    case CooldownBucketType.Global:
                        obj += command;
                        break;
                    default:
                        throw new InvalidOperationException("Unknown bucket type.");
                }

                return obj;
            }

            throw new InvalidOperationException("Unknown bucket type.");
        }
    }
}
