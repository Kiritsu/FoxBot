using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Fox.Commands.Checks;
using Fox.Entities;
using Fox.Exceptions;
using Fox.Extensions;
using Qmmands;
using System.Linq;
using System.Threading.Tasks;

namespace Fox.Commands.Modules
{
    [Name("Moderation")]
    public sealed class ModerationModule : FoxModuleBase
    {
        [Command("Kick", "Mimimi", "Fu")]
        [Description("Kicks the user from the guild.")]
        [RequireBotPermissions(Permissions.KickMembers)]
        [RequireUserPermissions(Permissions.KickMembers)]
        public async Task KickAsync([Description("Member to kick.")] [RequireHierarchy] DiscordMember target, [Description("Reason of the kick.")] [Remainder] string reason = null)
        {
            if (target == Context.Client.CurrentUser)
            {
                throw new FoxException("Sorry, I can't kick myself.");
            }

            await target.RemoveAsync(reason);

            await SimpleEmbedAsync($"{target.FormatUser()} has been kicked from the guild. Reason: {reason ?? "Not specified."}");
        }

        [Command("Ban", "Voteofftheisland")]
        [Description("Bans the user from the guild.")]
        [RequireBotPermissions(Permissions.BanMembers)]
        [RequireUserPermissions(Permissions.BanMembers)]
        public async Task BanAsync([Description("Member to ban.")] [RequireHierarchy] DiscordMember target, [Description("Reason of the ban.")] [Remainder] string reason = null)
        {
            if (target == Context.Client.CurrentUser)
            {
                throw new FoxException("Sorry, I can't ban myself.");
            }

            await target.BanAsync(7, reason);

            await SimpleEmbedAsync($"{target.FormatUser()} has been banned from the guild. Reason: {reason ?? "Not specified."}");
        }

        [Command("Hackban")]
        [Description("Hackbans the user from the guild.")]
        [RequireBotPermissions(Permissions.BanMembers)]
        [RequireUserPermissions(Permissions.BanMembers)]
        public async Task HackbanAsync(SkeletonUser target, [Description("Reason of the ban.")] [Remainder] string reason = null)
        {
            if (Context.Guild.Members.Any(x => x.Id == target.Id))
            {
                throw new FoxException($"You can't hackban that target because they're on this guild. Please use `{Context.Prefix}ban <target> [reason]` instead.");
            }

            await Context.Guild.BanMemberAsync(target.Id, 7, reason);

            await SimpleEmbedAsync($"{target.FormatUser()} has been hackbanned from the guild. Reason: {reason ?? "Not specified."}");
        }

        [Command("Softban")]
        [Description("Softbans the user from the guild.")]
        [RequireBotPermissions(Permissions.BanMembers)]
        [RequireUserPermissions(Permissions.BanMembers)]
        public async Task SoftbanAsync([Description("Member to softban.")] [RequireHierarchy] DiscordMember target, [Description("Reason of the softban.")] [Remainder] string reason = null)
        {
            if (target == Context.Client.CurrentUser)
            {
                throw new FoxException("Sorry, I can't softban myself.");
            }

            await target.BanAsync(7, reason);
            await target.UnbanAsync(reason);

            await SimpleEmbedAsync($"{target.FormatUser()} has been softbanned from the guild. Reason: {reason ?? "Not specified."}");
        }

        [Command("Unban", "Delban", "Deleteban")]
        [Description("Removes a ban on the current guild.")]
        [RequireBotPermissions(Permissions.BanMembers)]
        [RequireUserPermissions(Permissions.BanMembers)]
        public async Task UnbanAsync([Description("Member to unban")] SkeletonUser target, [Description("Reason of the unban")] [Remainder] string reason = null)
        {
            try
            {
                await Context.Guild.UnbanMemberAsync(target.Id, reason ?? "Not specified.");
                await SimpleEmbedAsync($"{target.FormatUser()} has been unbanned from this guild. Reason: {reason ?? "Not specified."}");
            }
            catch (NotFoundException)
            {
                await SimpleEmbedAsync($"{target.FormatUser()} is not banned yet.");
            }
        }
    }
}
