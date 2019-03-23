using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using Fox.Commands.Checks;
using Fox.Entities;
using Qmmands;

namespace Fox.Commands.Modules
{
    [Group("Prefix"), Name("Prefix"), CheckModuleState]
    [RequireUserPermissions(Permissions.ManageGuild)]
    [Description("Modules that manages prefix for the current guild.")]
    public sealed class PrefixModule : FoxModuleBase
    {
        [Command("Add")]
        [Description("Adds the specified prefix in guild's prefixes.")]
        public Task AddAsync([Description("Prefix to add.")] string prefix)
        {
            if (DbContext.Guild.Prefixes.Contains(prefix))
            {
                return RespondAsync("That prefix already exists.");
            }

            DbContext.Guild.Prefixes.Add(prefix);
            DbContext.UpdateGuild();

            return RespondAsync("That prefix has successfully been added to this guild's prefixes.");
        }

        [Command("Remove")]
        [Description("Removes the specified prefix from guild's prefixes.")]
        public Task RemoveAsync([Description("Prefix to remove.")] string prefix)
        {
            if (!DbContext.Guild.Prefixes.Contains(prefix))
            {
                return RespondAsync("That prefix doesn't exist.");
            }

            if (DbContext.Guild.Prefixes.Count == 1)
            {
                return RespondAsync("This is the last prefix, you can't remove it.");
            }

            DbContext.Guild.Prefixes.Remove(prefix);
            DbContext.UpdateGuild();

            return RespondAsync("That prefix has successfully been removed from this guild's prefixes.");
        }

        [Command("Show")]
        [Description("Shows the different prefixes in that guild.")]
        public Task ShowAsync()
        {
            return RespondAsync($"Enabled prefixes for that guild: {string.Join(", ", DbContext.Guild.Prefixes.Select(x => $"`{x}`"))}");
        }
    }
}
