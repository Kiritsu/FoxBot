using System.Collections.Generic;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using Fox.Databases.Entities.Models;

namespace Fox.Commands.Modules.Models
{
    public sealed class MusicPlayer
    {
        public int Index { get; set; }

        public List<LavalinkTrack> Tracks { get; set; }

        public DiscordChannel VoiceChannel { get; set; }

        public DiscordChannel TextChannel { get; set; }

        public DiscordMessage Message { get; set; }

        public MusicConfiguration Settings { get; set; }
    }
}
