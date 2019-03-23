using DSharpPlus.Entities;

namespace Fox.Extensions
{
    public static class DiscordMemberExtensions
    {
        public static string FormatUser(this DiscordMember mbr)
        {
            return mbr.DisplayName + "#" + mbr.Discriminator;
        }
    }
}
