using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Fox.Commands.Checks;
using Fox.Databases.Entities.Models;
using Fox.Entities;
using Fox.Exceptions;
using Fox.Extensions;
using Qmmands;

namespace Fox.Commands.Modules.Public
{
    [Group("CustomCommand", "cc"), Name("CustomCommand"), CheckModuleState]
    public sealed class CustomCommandsModule : FoxModuleBase
    {
        [Command("Stats")]
        [Description("Show simple CustomCommands stats in the current guild.")]
        public Task StatsAsync()
        {
            var cc = DbContext.Guild.CustomCommands;

            var str = new StringBuilder();
            foreach (var custom in cc.OrderByDescending(x => x.TimeUsed).Take(5))
            {
                str.AppendLine($"`{custom.Name}` used `{custom.TimeUsed}` times, created by <@!{custom.AuthorId}>");
            }

            var emb = new DiscordEmbedBuilder().StylizeFor(Context.Member);
            emb.WithDescription($"This guild has {cc.Count} custom commands." +
                $"\nYou have {cc.Where(x => x.AuthorId == Context.Member.Id).Count()} custom commands on this guild." +
                $"\nThe most used custom commands are:" +
                $"\n{str}");

            return RespondAsync(emb);
        }

        [Command("List")]
        [Description("Lists every custom command in the current guild.")]
        public Task ListAsync()
        {
            var cc = DbContext.Guild.CustomCommands;

            var str = new StringBuilder();
            foreach (var custom in cc.OrderByDescending(x => x.TimeUsed))
            {
                str.AppendLine($"`{custom.Name}` used `{custom.TimeUsed}` times, created by <@!{custom.AuthorId}>");
            }

            return SimpleEmbedAsync(str.ToString());
        }

        [Command("Info")]
        [Description("Gives some informations about the specified name")]
        public Task InfoAsync([Remainder] string name)
        {
            var cc = DbContext.Guild.CustomCommands;
            var custom = cc.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (custom is null)
            {
                throw new FoxException("Unknown custom command.");
            }

            return SimpleEmbedAsync($"Information of the custom command: `{custom.Name}`\n\nCreated by: <@!{custom.AuthorId}>\nUse amount: {custom.TimeUsed}\nResponse: `{custom.Response}`");
        }

        [Command("Edit")]
        [Description("Edits the specified custom command")]
        public Task EditAsync(string name, [Remainder] string response)
        {
            var cc = DbContext.Guild.CustomCommands;

            if (!cc.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && x.AuthorId == Context.Member.Id))
            {
                throw new FoxException("A custom command with that name doesn't exist yet or is not your.");
            }

            var custom = cc.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            custom.Response = response;

            DbContext.UpdateGuild();

            return SimpleEmbedAsync("Your custom command has been edited.");
        }

        [Command("Create")]
        [Description("Creates a custom command.")]
        public Task CreateAsync(string name, [Remainder] string response)
        {
            var cc = DbContext.Guild.CustomCommands;

            if (cc.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new FoxException("A custom command with that name already exists.");
            }

            cc.Add(new CustomCommand
            {
                AuthorId = Context.Member.Id,
                GuildId = Context.Guild.Id,
                Name = name,
                Response = response,
                TimeUsed = 0
            });

            DbContext.UpdateGuild();

            return SimpleEmbedAsync("Your custom command has been created.");
        }

        [Command("Remove")]
        [Description("Removes a custom command.")]
        public Task RemoveAsync([Remainder] string name)
        {
            var cc = DbContext.Guild.CustomCommands;

            if (!cc.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new FoxException("A custom command with that name doesn't exist yet.");
            }

            var c = cc.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (Context.Member.Id == c.AuthorId)
            {
                cc.Remove(c);
            }
            else if (Context.Member.PermissionsIn(Context.Channel).HasPermission(Permissions.ManageGuild) ||
                Context.Member.PermissionsIn(Context.Channel).HasPermission(Permissions.Administrator))
            {
                cc.Remove(c);
            }
            else
            {
                return SimpleEmbedAsync("The command doesn't belong to you or you don't have permission.");
            }

            DbContext.UpdateGuild();

            return SimpleEmbedAsync("Your custom command has been removed.");
        }
    }
}
