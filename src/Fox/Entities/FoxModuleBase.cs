using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Fox.Databases;
using Fox.Extensions;
using Fox.Services;
using Qmmands;
using static Fox.Services.PaginatorService;

namespace Fox.Entities
{
    public abstract class FoxModuleBase : ModuleBase<FoxCommandContext>
    {
        public FoxDbContext DbContext => Context.DatabaseContext;

        public FoxDb Db => Context.Database;

        protected override Task BeforeExecutedAsync(Command command)
        {
            Context.Command = command;

            return Task.CompletedTask;
        }

        public Task<DiscordMessage> RespondAsync(string message = "", bool tts = false, DiscordEmbed embed = default)
        {
            return Context.RespondAsync(message, tts, embed);
        }

        public Task<DiscordMessage> RespondAsync(DiscordEmbedBuilder embed)
        {
            embed = embed.StylizeFor(Context.Member);

            return RespondAsync("", false, embed);
        }

        public Task<DiscordMessage> SimpleEmbedAsync(string message)
        {
            return RespondAsync("", false, new DiscordEmbedBuilder().StylizeFor(Context.User).WithDescription(message));
        }

        public Task<PaginatorService> PaginateAsync(IReadOnlyList<Page> pages, bool extraEmojis = true)
        {
            var paginator = new PaginatorService(Context, pages.ToImmutableArray());
            return paginator.SendAsync(extraEmojis);
        }
    }
}
