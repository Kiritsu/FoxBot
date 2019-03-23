using Fox.Entities;

namespace Fox.Extensions
{
    public static class SkeletonUserExtensions
    {
        public static string FormatUser(this SkeletonUser user)
        {
            return user.Username + "#" + user.Discriminator;
        }
    }
}
