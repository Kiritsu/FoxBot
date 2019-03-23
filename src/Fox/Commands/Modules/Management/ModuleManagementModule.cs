using DSharpPlus;
using Fox.Databases.Entities.Models;
using Fox.Databases.Enums;
using Fox.Entities;
using Fox.Exceptions;
using Qmmands;
using System;
using System.Linq;
using System.Threading.Tasks;
using Fox.Commands.Checks;
using System.Text;

namespace Fox.Commands.Module
{
    [Group("Module"), Name("Module"), RequireUserPermissions(Permissions.ManageChannels)]
    public sealed class ModuleManagementModule : FoxModuleBase
    {
        private readonly CommandService _cmd;

        public ModuleManagementModule(CommandService cmd)
        {
            _cmd = cmd;
        }

        [Command("List")]
        [Description("Lists the different bot's modules.")]
        public Task ListAsync()
        {
            return RespondAsync(string.Join(", ", _cmd.GetAllModules().Where(x => IsValidModule(x.Name)).Select(x => $"`{x.Name}`")));
        }

        [Command("State")]
        [Description("Shows the state of the specified module in the current channel.")]
        public Task StateAsync([Description("Module name")] string module)
        {
            if (!IsValidModule(module))
            {
                throw new FoxException("The provided module was invalid.");
            }

            var cfg = GetOrAdd(module);

            return SimpleEmbedAsync($"The module `{module}` is `{cfg.State}`.");
        }

        [Command("States")]
        [Description("Shows the states of every module in the current channel.")]
        public Task StatesAsync()
        {
            var str = new StringBuilder();

            foreach (var module in _cmd.GetAllModules().Where(x => IsValidModule(x.Name)))
            {
                var cfg = GetOrAdd(module.Name);
                str.AppendLine($"The module `{module.Name}` is `{cfg.State}`.");
            }

            return SimpleEmbedAsync(str.ToString());
        }

        [Command("Enable")]
        [Description("Enables the specified module in the current channel.")]
        public Task EnableAsync([Description("Module name")] string module)
        {
            if (!IsValidModule(module))
            {
                throw new FoxException("The provided module was invalid or protected.");
            }

            var cfg = GetOrAdd(module);
            cfg.State = ModuleState.Enabled;

            DbContext.UpdateChannel();

            return SimpleEmbedAsync($"The module `{module}` has been enabled.");
        }

        [Command("EnableAll")]
        [Description("Enables the module in every channel.")]
        public Task EnableAllAsync([Description("Module name")] string module)
        {
            if (!IsValidModule(module))
            {
                throw new FoxException("The provided module was invalid or protected.");
            }

            foreach (var chan in Context.Guild.Channels.Where(x => x.Type == ChannelType.Text))
            {
                var cfg = Db.ChannelManager.GetOrCreate(chan);

                if (!cfg.Modules.Any(x => x.Key.Equals(module, StringComparison.OrdinalIgnoreCase)))
                {
                    cfg.Modules.TryAdd(module, new ModuleConfiguration
                    {
                        Name = module,
                        Permissions = Permissions.None,
                        State = ModuleState.Enabled
                    });
                }

                var mod = cfg.Modules.FirstOrDefault(x => x.Key.Equals(module, StringComparison.OrdinalIgnoreCase));
                mod.Value.State = ModuleState.Enabled;

                Db.ChannelManager.Update(cfg);
            }

            return SimpleEmbedAsync($"The module `{module}` has been enabled everywhere.");
        }

        [Command("Disable")]
        [Description("Disables the specified module in the current channel.")]
        public Task DisableAsync([Description("Module name")] string module)
        {
            if (!IsValidModule(module))
            {
                throw new FoxException("The provided module was invalid or protected.");
            }

            var cfg = GetOrAdd(module);
            cfg.State = ModuleState.Disabled;

            DbContext.UpdateChannel();

            return SimpleEmbedAsync($"The module `{module}` has been disabled.");
        }

        [Command("DisableAll")]
        [Description("Disables the module in every channel.")]
        public Task DisableAllAsync([Description("Module name")] string module)
        {
            if (!IsValidModule(module))
            {
                throw new FoxException("The provided module was invalid or protected.");
            }

            foreach (var chan in Context.Guild.Channels.Where(x => x.Type == ChannelType.Text))
            {
                var cfg = Db.ChannelManager.GetOrCreate(chan);

                if (!cfg.Modules.Any(x => x.Key.Equals(module, StringComparison.OrdinalIgnoreCase)))
                {
                    cfg.Modules.TryAdd(module, new ModuleConfiguration
                    {
                        Name = module,
                        Permissions = Permissions.None,
                        State = ModuleState.Disabled
                    });
                }

                var mod = cfg.Modules.FirstOrDefault(x => x.Key.Equals(module, StringComparison.OrdinalIgnoreCase));
                mod.Value.State = ModuleState.Disabled;

                Db.ChannelManager.Update(cfg);
            }

            return SimpleEmbedAsync($"The module `{module}` has been disabled everywhere.");
        }

        [Command("Protect")]
        [Description("Protects the specified module in the current channel for users with the specified Discord Permission. Use 'module permissions' for more information.")]
        public Task ProtectAsync([Description("Module name")] string module, [Description("See 'module permissions'")] Permissions? permissions)
        {
            if (!permissions.HasValue)
            {
                throw new FoxException("You must give a specific discord permission to limit the module to user who have that permission.");
            }

            if (!IsValidModule(module))
            {
                throw new FoxException("The provided module was invalid.");
            }

            var cfg = GetOrAdd(module);
            cfg.Permissions = permissions.Value;
            cfg.State = ModuleState.Protected;

            DbContext.UpdateChannel();

            return SimpleEmbedAsync($"The module `{module}` is now restricted to users with the following permission: {permissions.Value.ToPermissionString()}");
        }

        [Command("Permissions")]
        [Description("Shows the different available permissions related to the protect command.")]
        public Task PermissionsAsync()
        {
            return RespondAsync(string.Join(", ", Enum.GetNames(typeof(Permissions)).Select(x => $"`{x}`")));
        }

        private bool IsValidModule(string module)
        {
            return _cmd.GetAllModules().Any(x => x.Name.Equals(module, StringComparison.OrdinalIgnoreCase) && !x.Name.Equals("Module"));
        }

        private ModuleConfiguration GetOrAdd(string module)
        {
            if (!DbContext.Channel.Modules.Any(x => x.Key.Equals(module, StringComparison.OrdinalIgnoreCase)))
            {
                DbContext.Channel.Modules.TryAdd(module, new ModuleConfiguration
                {
                    Name = module,
                    Permissions = Permissions.None,
                    State = ModuleState.Enabled
                });

                DbContext.UpdateChannel();
            }

            return DbContext.Channel.Modules.FirstOrDefault(x => x.Key.Equals(module, StringComparison.OrdinalIgnoreCase)).Value;
        }
    }
}
