using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Fox.Entities;

namespace Fox.Services
{
    //todo: create a service that could handle multiple paginations and avoid creation of multiple event hook.
    public sealed class PaginatorService
    {
        public DiscordClient Client { get; }
        public DiscordChannel Channel { get; }
        public DiscordUser User { get; }

        public ImmutableArray<Page> Pages { get; }

        public DiscordMessage Message { get; private set; }

        private int _cursor;
        private bool _stopped;
        private bool _extraEmojis;

        private readonly bool _hasPermission;

        private TimeSpan _timeout = TimeSpan.FromMinutes(5);

        public PaginatorService(FoxCommandContext ctx, ImmutableArray<Page> pages)
        {
            Client = ctx.Client;
            Channel = ctx.Channel;
            User = ctx.User;
            Pages = pages;
            _cursor = 0;
            _stopped = false;
            _hasPermission = ctx.Guild.CurrentMember.PermissionsIn(ctx.Channel).HasPermission(Permissions.ManageMessages) || ctx.Guild.CurrentMember.PermissionsIn(ctx.Channel).HasPermission(Permissions.Administrator);
        }

        public Task SendAsync(TimeSpan timeout, bool extraEmojis = true)
        {
            _timeout = timeout;

            return SendAsync(extraEmojis);
        }

        public async Task<PaginatorService> SendAsync(bool extraEmojis = true)
        {
            _extraEmojis = extraEmojis;

            if (!(Message is null))
            {
                await Message.DeleteAsync();
            }

            var page = Pages[_cursor];

            Client.MessageReactionAdded += ReactionAdded;

            Message = await Channel.SendMessageAsync(page.Message, false, page.Embed);
            await Message.CreateReactionAsync(DiscordEmoji.FromUnicode("⏮"));
            await Message.CreateReactionAsync(DiscordEmoji.FromUnicode("⏪"));
            await Message.CreateReactionAsync(DiscordEmoji.FromUnicode("⏹"));
            await Message.CreateReactionAsync(DiscordEmoji.FromUnicode("⏩"));
            await Message.CreateReactionAsync(DiscordEmoji.FromUnicode("⏭"));

            if (extraEmojis)
            {
                await Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🔠"));
                await Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🔢"));
            }

            _ = Task.Run(async () =>
            {
                await Task.Delay(_timeout);
                await EndAsync();
            });

            return this;
        }

        private Task NextPageAsync()
        {
            _cursor++;
            return UpdatePageAsync();
        }

        private Task PreviousPageAsync()
        {
            _cursor--;
            return UpdatePageAsync();
        }

        private Task SetPageAsync(int page)
        {
            _cursor = page;
            return UpdatePageAsync();
        }

        private async Task EndAsync()
        {
            if (_stopped)
            {
                return;
            }

            if (_hasPermission)
            {
                await Message.DeleteAllReactionsAsync();
            }

            _stopped = true;
            Client.MessageReactionAdded -= ReactionAdded;
        }

        private async Task UpdatePageAsync()
        {
            if (Message is null || _stopped)
            {
                return;
            }

            if (_cursor == Pages.Length)
            {
                _cursor = 0;
            }

            if (_cursor < 0)
            {
                _cursor = Pages.Length - 1;
            }

            var page = Pages[_cursor];

            await Message.ModifyAsync(page.Message, page.Embed);
        }

        private async Task ReactionAdded(MessageReactionAddEventArgs e)
        {
            if (_stopped)
            {
                return;
            }

            if (e.User.Id != User.Id || (Message != null && e.Message.Id != Message.Id))
            {
                return;
            }

            if (_hasPermission)
            {
                await e.Message.DeleteReactionAsync(e.Emoji, e.User);
            }

            switch (e.Emoji.Name)
            {
                case "⏪":
                    await PreviousPageAsync();
                    break;
                case "⏩":
                    await NextPageAsync();
                    break;
                case "⏹":
                    await EndAsync();
                    break;
                case "⏮":
                    await SetPageAsync(0);
                    break;
                case "⏭":
                    await SetPageAsync(Pages.Length - 1);
                    break;
                case "🔠" when _extraEmojis:
                    _ = HandleIdentifierAsync();
                    break;
                case "🔢" when _extraEmojis:
                    _ = HandlePageAsync();
                    break;
            }
        }

        private async Task HandleIdentifierAsync()
        {
            var tcs = new TaskCompletionSource<int>();

            Client.MessageCreated += MessageCreated;

            var confirmMessage = await Channel.SendMessageAsync($"{User.Mention} | Please provide a valid `Identifier`. Write `Cancel` to cancel. This will timeout after 30 seconds.");

            var trigger = tcs.Task;
            var delay = Task.Delay(TimeSpan.FromSeconds(30));
            var task = await Task.WhenAny(trigger, delay);

            await confirmMessage.DeleteAsync();

            Client.MessageCreated -= MessageCreated;

            if (task != trigger)
            {
                return;
            }

            var pageId = await trigger;
            if (pageId == -1)
            {
                return;
            }

            await SetPageAsync(pageId);

            Task MessageCreated(MessageCreateEventArgs e)
            {
                if (e.Channel != Channel)
                {
                    return Task.CompletedTask;
                }

                if (e.Author != User)
                {
                    return Task.CompletedTask;
                }

                if (e.Message.Content.Equals("Cancel", StringComparison.OrdinalIgnoreCase))
                {
                    tcs.SetResult(-1);
                    return e.Message.DeleteAsync();
                }
                else
                {
                    var page = Pages.FirstOrDefault(x => x.Identifier != null && x.Identifier.Equals(e.Message.Content, StringComparison.OrdinalIgnoreCase));
                    if (page is null)
                    {
                        return Task.CompletedTask;
                    }

                    tcs.SetResult(Pages.IndexOf(page));
                    return e.Message.DeleteAsync();
                }
            }
        }

        private async Task HandlePageAsync()
        {
            var tcs = new TaskCompletionSource<int>();

            Client.MessageCreated += MessageCreated;

            var confirmMessage = await Channel.SendMessageAsync($"{User.Mention} | Please provide a valid `Page`. Write `Cancel` to cancel. This will timeout after 30 seconds.");

            var trigger = tcs.Task;
            var delay = Task.Delay(TimeSpan.FromSeconds(30));
            var task = await Task.WhenAny(trigger, delay);

            await confirmMessage.DeleteAsync();

            Client.MessageCreated -= MessageCreated;

            if (task != trigger)
            {
                return;
            }

            var pageId = await trigger;
            if (pageId == -1)
            {
                return;
            }

            await SetPageAsync(pageId);

            Task MessageCreated(MessageCreateEventArgs e)
            {
                if (e.Channel != Channel)
                {
                    return Task.CompletedTask;
                }

                if (e.Author != User)
                {
                    return Task.CompletedTask;
                }

                if (e.Message.Content.Equals("Cancel", StringComparison.OrdinalIgnoreCase))
                {
                    tcs.SetResult(-1);
                    return e.Message.DeleteAsync();
                }
                else
                {
                    if (!int.TryParse(e.Message.Content, out var page))
                    {
                        return Task.CompletedTask;
                    }

                    if (page <= 0 || page >= Pages.Length)
                    {
                        return Task.CompletedTask;
                    }

                    tcs.SetResult(page - 1);
                    return e.Message.DeleteAsync();
                }
            }
        }

        public sealed class Page
        {
            public string Message { get; set; }
            public DiscordEmbed Embed { get; set; }
            public string Identifier { get; set; }
        }
    }
}