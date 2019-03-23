using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Fox.Commands.Checks;
using Fox.Databases.Entities.Models;
using Fox.Entities;
using Fox.Exceptions;
using Fox.Extensions;
using Fox.Services;
using Qmmands;
using static Fox.Services.PaginatorService;

namespace Fox.Commands.Modules.Music
{
    [Name("Playlist"), Group("Playlist"), CheckLavalinkState]
    [Description("Modules used to manage playlists for the music module.")]
    [RunMode(RunMode.Parallel)]
    public sealed class PlaylistModule : FoxModuleBase
    {
        private readonly MusicService _music;

        public PlaylistModule(MusicService music)
        {
            _music = music;
        }

        [Command("List")]
        [Description("Lists your different playlists.")]
        public Task ListAsync()
        {
            var playlists = DbContext.User.Playlists;
            var embed = new DiscordEmbedBuilder().StylizeFor(Context.Member).WithTitle($"You have {playlists.Count} playlists.");
            foreach (var playlist in playlists)
            {
                var maxLength = TimeSpan.FromTicks(playlist.Tracks.Sum(x => x.Duration.Ticks)).ToString("g");
                embed.AddField(playlist.Name, $"{playlist.Tracks.Count} songs found.\nPlaylist duration: {maxLength}", false);
            }
            embed.WithDescription($"For more information, use: `{Context.Prefix}playlist show <name>`");
            return RespondAsync(embed);
        }

        [Command("Show")]
        [Description("Shows the different songs of your playlist.")]
        public Task ShowAsync([Remainder] string name)
        {
            var playlists = DbContext.User.Playlists;
            var playlist = playlists.FirstOrDefault(x => x.Name == name);

            if (playlist is null)
            {
                throw new FoxException($"Couldn't find any playlist with name `{name}`");
            }

            var page = 1;
            var pages = new List<Page>();
            var str = new StringBuilder();
            var index = 0;
            for (var i = 0; i < playlist.Tracks.Count; i += 10)
            {
                var currents = playlist.Tracks.Skip(i).Take(10);

                foreach (var track in currents)
                {
                    str.AppendLine($"[#{index}]: {track.Title} - {track.Author}\nSong duration: {track.Duration.Humanize()}\n");
                    index++;
                }

                pages.Add(new Page
                {
                    Embed = new DiscordEmbedBuilder().StylizeFor(Context.Member).WithDescription(str.ToString()).WithTitle($"Page {page}/{(playlist.Tracks.Count / 10) + 1}")
                });

                str.Clear();
                page++;
            }

            return PaginateAsync(pages);
        }

        [Command("Save")]
        [Description("Saves the current queue as a playlist.")]
        public Task SaveAsync([Remainder] string name)
        {
            if (!_music.Players.TryGetValue(Context.Guild.Id, out var player))
            {
                throw new FoxException("It looks like the music service is not started. There's nothing to save.");
            }

            var playlists = DbContext.User.Playlists;

            if (playlists.Any(x => x.Name == name))
            {
                throw new FoxException($"Unable to add a new playlist with name `{name}`. Does it already exist?");
            }

            var tracks = new List<UserPlaylistSong>();
            foreach (var song in player.Tracks)
            {
                tracks.Add(new UserPlaylistSong
                {
                    Author = song.Author,
                    Title = song.Title,
                    Url = song.Uri.ToString(),
                    Duration = song.Length
                });
            }

            playlists.Add(new UserPlaylist
            {
                Name = name,
                Tracks = tracks
            });

            DbContext.UpdateUser();

            return SimpleEmbedAsync($"The current queue, with {player.Tracks.Count} songs, has been saved in your playlists list with the name `{name}`.");
        }

        [Command("Remove")]
        [Description("Removes a playlist from your playlists list.")]
        public Task RemoveAsync([Remainder] string name)
        {
            if (!_music.Players.TryGetValue(Context.Guild.Id, out var player))
            {
                throw new FoxException("It looks like the music service is not started. There's nothing to save.");
            }

            var playlists = DbContext.User.Playlists;

            if (!playlists.Any(x => x.Name == name))
            {
                throw new FoxException($"You don't have any playlist with that name.");
            }

            playlists.RemoveAll(x => x.Name == name);
            DbContext.UpdateUser();

            return SimpleEmbedAsync($"Your playlist with name `{name}` has been removed.");
        }

        [Command("Rename")]
        [Description("Renames a playlist.")]
        public Task RenameAsync(string name, string newName)
        {
            var playlists = DbContext.User.Playlists;
            var playlist = playlists.FirstOrDefault(x => x.Name == name);

            if (playlist is null)
            {
                throw new FoxException($"Couldn't find any playlist with name `{name}`. If your playlist has spaces, please wrap the name between `\"`");
            }

            playlist.Name = newName;

            DbContext.UpdateUser();

            return RespondAsync(new DiscordEmbedBuilder().StylizeFor(Context.Member).WithDescription("Your playlist's name has successfully been changed."));
        }

        [Command("Play")]
        [Description("Plays the playlist of your playlists list.")]
        [RequireDj]
        public async Task PlayAsync([Remainder] string name)
        {
            var playlists = DbContext.User.Playlists;
            var playlist = playlists.FirstOrDefault(x => x.Name == name);

            if (playlist is null)
            {
                throw new FoxException($"Couldn't find any playlist with name `{name}`");
            }

            if (!_music.Players.TryGetValue(Context.Guild.Id, out var player))
            {
                if (Context.Member.VoiceState?.Channel is null)
                {
                    throw new FoxException("You have to be in a voice channel to use this command.");
                }

                await _music.JoinChannelAsync(Context, Context.Member.VoiceState?.Channel);
            }

            var message = await SimpleEmbedAsync("Your playlist is being loaded, please wait. This message will be removed when it's been loaded.");

            player = _music.Players.FirstOrDefault(x => x.Key == Context.Guild.Id).Value;
            foreach (var track in playlist.Tracks)
            {
                player.Tracks.Add(await _music.ResolveAsync(track.Url));
            }

            await message.DeleteAsync();

            await _music.PlayAsync(Context);
        }
    }
}
