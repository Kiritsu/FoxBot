using System;
using System.Collections.Generic;
using Fox.Databases.Entities.Models;

namespace Fox.Databases.Entities
{
    public sealed class UserEntity : Entity
    {
        public long Gold { get; set; }

        public long Experience { get; set; }

        public DateTime RewardCooldown { get; set; }

        public bool IsBlacklisted { get; set; }

        public List<UserPlaylist> Playlists { get; set; }
    }
}
