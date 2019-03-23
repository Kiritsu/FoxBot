using System.Collections.Generic;
using Fox.Databases.Entities.Models;

namespace Fox.Databases.Entities
{
    public sealed class GuildEntity : Entity
    {
        public ICollection<string> Prefixes { get; set; }

        public ICollection<CustomCommand> CustomCommands { get; set; }

        public MusicConfiguration Music { get; set; }

        public bool Levenshtein { get; set; }
    }
}
