using DSharpPlus.Entities;

namespace Fox.Extensions
{
    public static class DiscordUserExtensions
    {
        public static string FormatUser(this DiscordUser user)
        {
            return user.Username + "#" + user.Discriminator;
        }
    }
}
