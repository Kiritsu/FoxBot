using System;
using DSharpPlus.Entities;
using Fox.Services;

namespace Fox.Extensions
{
    public static class DiscordEmbedBuilderExtensions
    {
        public static DiscordEmbedBuilder StylizeFor(this DiscordEmbedBuilder embed, DiscordUser user)
        {
            embed.WithColor(ConfigurationService.EmbedColor);
            embed.WithFooter($"Performed by {user.Username}#{user.Discriminator}", user.AvatarUrl);
            embed.WithTimestamp(DateTimeOffset.UtcNow);

            return embed;
        }

        public static DiscordEmbedBuilder Stylize(this DiscordEmbedBuilder embed)
        {
            embed.WithColor(ConfigurationService.EmbedColor);
            embed.WithTimestamp(DateTimeOffset.UtcNow);

            return embed;
        }
    }
}
