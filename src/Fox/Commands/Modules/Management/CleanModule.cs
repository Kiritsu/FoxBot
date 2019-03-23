using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Fox.Commands.Checks;
using Fox.Entities;
using Fox.Exceptions;
using Qmmands;

namespace Fox.Commands.Modules
{
    [Name("Clean"), CheckModuleState]
    [RequireBotPermissions(Permissions.ManageMessages)]
    [RequireUserPermissions(Permissions.ManageMessages)]
    public sealed class CleanModule : FoxModuleBase
    {
        public static TimeSpan TwoWeeks => TimeSpan.FromDays(14);

        [Command("Clean")]
        [Description("Removes the last 100 messages sent by the bot.")]
        public async Task CleanAsync()
        {
            var messages = await Context.Channel.GetMessagesAsync();
            var filteredMessages = messages.Where(x => DateTime.UtcNow - x.Timestamp < TwoWeeks && x.Author == Context.Guild.CurrentMember).ToImmutableArray();

            await PruneAsync(filteredMessages, Context.Channel, 100);
        }

        [Command("Clean")]
        [Description("Removes the last 'count' messages sent in this channel.")]
        public async Task CleanAsync([Description("Amount of messages to remove")] int count)
        {
            var messages = await Context.Channel.GetMessagesAsync(count);
            var filteredMessages = messages.Where(x => DateTime.UtcNow - x.Timestamp < TwoWeeks).ToImmutableArray();

            await PruneAsync(filteredMessages, Context.Channel, count);
        }

        [Command("Clean")]
        [Description("Removes the last 'count' messages sent by the specified 'user' in this channel.")]
        public async Task CleanAsync([Description("Amount of messages to remove")] int count, [Description("User affected by the filter.")] DiscordUser user)
        {
            var messages = await Context.Channel.GetMessagesAsync(count);
            var filteredMessages = messages.Where(x => DateTime.UtcNow - x.Timestamp < TwoWeeks && x.Author == user).ToImmutableArray();

            await PruneAsync(filteredMessages, Context.Channel, count);
        }

        [Command("Clean")]
        [Description("Removes the last 'count' messages with the specified type of clean.")]
        public async Task CleanAsync([Description("Amount of messages to remove")] int count, [Description("Type of message. Bot File or Embed.")] CleanMessageType type)
        {
            var messages = await Context.Channel.GetMessagesAsync(count);
            var filteredMessages = messages.Where(x => DateTime.UtcNow - x.Timestamp < TwoWeeks).ToImmutableArray();

            switch (type)
            {
                case CleanMessageType.Bot:
                    filteredMessages = messages.Where(x => x.Author.IsBot).ToImmutableArray();
                    break;
                case CleanMessageType.File:
                    filteredMessages = messages.Where(x => x.Attachments.Count > 0).ToImmutableArray();
                    break;
                case CleanMessageType.Embed:
                    filteredMessages = messages.Where(x => x.Embeds.Count > 0).ToImmutableArray();
                    break;
            }

            await PruneAsync(filteredMessages, Context.Channel, count);
        }

        public async Task PruneAsync(ImmutableArray<DiscordMessage> messages, DiscordChannel channel, int baseAmount)
        {
            if (messages.Length <= 0)
            {
                throw new FoxException("Uh, I've found no messages to delete. Note that they must be newer than 2 weeks!");
            }

            await channel.DeleteMessagesAsync(messages);
            await SimpleEmbedAsync($"`{messages.Length}/{baseAmount}` messages have been deleted.");
        }

        public enum CleanMessageType
        {
            Bot,
            File,
            Embed,
            All
        }
    }
}
