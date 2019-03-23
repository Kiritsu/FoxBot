using DSharpPlus.Lavalink;

namespace Fox.Extensions
{
    public static class LavalinkTrackExtensions
    {
        public static string Format(this LavalinkTrack track)
        {
            return $"{track.Title} | {track.Author} - [{track.Length}]";
        }
    }
}
