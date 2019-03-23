using System;

namespace Fox.Databases.Entities.Models
{
    public sealed class UserPlaylistSong
    {
        public string Url { get; set; }

        public string Title { get; set; }

        public string Author { get; set; }

        public TimeSpan Duration { get; set; }
    }
}
