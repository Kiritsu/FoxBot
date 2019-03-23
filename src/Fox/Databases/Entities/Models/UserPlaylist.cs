using System.Collections.Generic;

namespace Fox.Databases.Entities.Models
{
    public class UserPlaylist
    {
        public List<UserPlaylistSong> Tracks { get; set; }

        public string Name { get; set; }
    }
}