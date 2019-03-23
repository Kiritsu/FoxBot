using System;

namespace Fox.Databases.Entities
{
    public sealed class MemberEntity : Entity
    {
        public ulong GuildId { get; set; }

        public long Experience { get; set; }

        public DateTime RewardCooldown { get; set; }
    }
}
