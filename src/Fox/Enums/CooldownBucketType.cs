using System;

namespace Fox.Enums
{
    [Flags]
    public enum CooldownBucketType
    {
        Guild = 1,
        Channel = 2,
        User = 4,
        Global = 8
    }
}
