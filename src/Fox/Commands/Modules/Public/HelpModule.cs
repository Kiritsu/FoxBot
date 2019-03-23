using DSharpPlus;
using DSharpPlus.Entities;
using Fox.Commands.Checks;
using Fox.Entities;
using Fox.Extensions;
using Fox.Services;
using Qmmands;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fox.Commands.Modules
{
    [Name("Help"), Hidden]
    public sealed class HelpModule : FoxModuleBase
    {
        private readonly CommandService _commands;

        public HelpModule(CommandService cmds)
        {
            _commands = cmds;
        }

        [Command("Help")]
        public async Task HelpAsync()
        {
            var modules = _commands.GetAllModules().Where(x => x.Aliases.Count > 0 && x.Commands.Count > 0 && x.Parent == null && !x.Attributes.Any(y => y is HiddenAttribute)).ToList();
            var theoricalCommands = _commands.GetAllModules().Where(x => x.Aliases.Count == 0 && !x.Attributes.Any(y => y is HiddenAttribute)).SelectMany(x => x.Commands);
            var commands = new List<Command>();
            foreach (var command in theoricalCommands)
            {
                if (commands.All(x => x.FullAliases[0] != command.FullAliases[0]))
                {
                    commands.Add(command);
                }
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = ConfigurationService.EmbedColor,
                Title = "Help",
                Description = $"Here's the list of every module. If you want to see every command contained in a module, please type: `{Context.Prefix}help <module_name>`\n\nPrefixes: {string.Join(", ", DbContext.Guild.Prefixes.Select(x => $"`{x}`"))}",
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{modules.Count} module{modules.Plural()} and {commands.Count} command{commands.Plural()} available."
                }
            };

            embed.AddField("Modules", string.Join(", ", modules.Select(x => $"`{x.Name}`")));
            embed.AddField("Commands", string.Join(", ", commands.Select(x => $"`{x.Name}`")));

            await RespondAsync(embed: embed.Build());
        }

        [Command("Help")]
        public Task HelpAsync([Remainder] string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return HelpAsync();
            }

            var cmds = _commands.FindCommands(command).ToImmutableArray();

            if (cmds.Length == 0)
            {
                var module = _commands.FindModules(command).FirstOrDefault()?.Module;

                if (module is null)
                {
                    var cmdArgs = command.Split(' ').ToList();
                    cmdArgs.RemoveAt(cmdArgs.Count - 1);

                    return HelpAsync(string.Join(' ', cmdArgs));
                }

                var emb = new DiscordEmbedBuilder
                {
                    Color = ConfigurationService.EmbedColor,
                    Title = "Help",
                    Description = $"Here's the list of every command in that module. If you want to see how to use a specific command contained in that module, please type: `{Context.Prefix}help {module.FullAliases[0].ToLowerInvariant()} <command_name>`",
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"{module.Commands.Count} command{module.Commands.Plural()} available."
                    }
                };

                if (module.Submodules.Count > 0)
                {
                    emb.AddField($"Submodule{module.Submodules.Plural()}", string.Join(", ", module.Submodules.Select(x => $"`{x.Aliases[0]}`")));
                }

                if (module.Commands.Count > 0)
                {
                    emb.AddField($"Command{module.Commands.Plural()}", string.Join(", ", module.Commands.Select(x => $"`{x.Aliases[0]}`")));
                }

                var modChecks = CommandUtilities.GetAllChecks(module).Cast<FoxCheckBaseAttribute>().ToImmutableArray();
                if (modChecks.Length > 0)
                {
                    emb.AddField($"Module requirement{modChecks.Plural()}", string.Join("\n", modChecks.Select(x => $"- `{x?.Name} {x?.Details}`")));
                }

                return RespondAsync(embed: emb);
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = ConfigurationService.EmbedColor,
                Title = "Help"
            };

            var builder = new StringBuilder();
            foreach (var cmd in cmds)
            {
                builder.AppendLine(Formatter.Bold(cmd.Command.Description ?? "Undocumented yet."));
                builder.AppendLine($"`{Context.Prefix}{cmd.Command.Name} {string.Join(" ", cmd.Command.Parameters.Select(x => $"[{x.Name}]"))}`".ToLowerInvariant());
                foreach (var param in cmd.Command.Parameters)
                {
                    builder.AppendLine($"`[{param.Name}]`: {param.Description ?? "Undocumented yet."}");
                }
                builder.AppendLine();
            }

            embed.AddField("Usages", builder.ToString());

            var defaultCmd = cmds.FirstOrDefault().Command;

            var checks = CommandUtilities.GetAllChecks(defaultCmd.Module).Cast<FoxCheckBaseAttribute>().ToImmutableArray();
            if (checks.Length > 0)
            {
                embed.AddField($"Module requirement{checks.Plural()}", string.Join("\n", checks.Select(x => $"- `{x?.Name} {x?.Details}`")));
            }

            if (defaultCmd.Checks.Count > 0)
            {
                embed.AddField($"Command requirement{defaultCmd.Checks.Plural()}", string.Join("\n", defaultCmd.Checks.Cast<FoxCheckBaseAttribute>().Select(x => $"- `{x?.Name} {x?.Details}`")));
            }

            return RespondAsync(embed: embed);
        }
    }
}
