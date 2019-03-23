using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.Net;
using Fox.Commands.Modules.Models;
using Fox.Entities;
using Fox.Exceptions;
using Fox.Extensions;
using static Fox.Services.PaginatorService;

namespace Fox.Services
{
    public sealed class MusicService
    {
        public ConcurrentDictionary<ulong, MusicPlayer> Players { get; }

        private LogService Logger { get; }

        private LavalinkExtension Lavalink { get; }
        private LavalinkNodeConnection Node { get; set; }

        public bool Started { get; set; }

        public MusicService(DiscordClient client, LavalinkExtension lavalink, LogService logger)
        {
            Players = new ConcurrentDictionary<ulong, MusicPlayer>();
            Lavalink = lavalink;
            Logger = logger;
            client.VoiceStateUpdated += Client_VoiceStateUpdated;
        }

        public async Task Client_VoiceStateUpdated(VoiceStateUpdateEventArgs e)
        {
            if (e.User != e.Client.CurrentUser)
            {
                return;
            }

            var connection = Node.GetConnection(e.Guild);
            if (connection is null)
            {
                return;
            }

            if (!Players.TryGetValue(e.Guild.Id, out var player))
            {
                return;
            }

            if (e.After is null && !(e.Before is null))
            {
                await e.Before.Channel.ConnectAsync(Node);
            } 
            else if (!(e.After is null) && !(e.Before is null))
            {
                player.VoiceChannel = e.After.Channel;
                await player.TextChannel.SendMessageAsync(embed: new DiscordEmbedBuilder().Stylize().WithDescription($"I'm now playing music in the channel `{player.VoiceChannel.Name}`"));
            }
        }

        public async Task StartLavalinkAsync()
        {
            if (Started)
            {
                return;
            }

            try
            {
                Node = await Lavalink.ConnectAsync(new LavalinkConfiguration
                {
                    Password = "1234",
                    RestEndpoint = new ConnectionEndpoint
                    {
                        Hostname = "localhost",
                        Port = 2333
                    },
                    SocketEndpoint = new ConnectionEndpoint
                    {
                        Hostname = "localhost",
                        Port = 2333
                    }
                });

                await Logger.PrintAsync(LogLevel.Info, "Lavalink", "Lavalink has been started.");

                Started = true;
            }
            catch (WebSocketException)
            {
                await Logger.PrintAsync(LogLevel.Error, "Lavalink", "Lavalink couldn't be started.");
                Started = false;
            }
        }

        public async Task JoinChannelAsync(FoxCommandContext ctx, DiscordChannel channel)
        {
            var connection = Node.GetConnection(ctx.Guild);
            if (connection != null)
            {
                throw new FoxException($"The music service is already started. Stop it first with `{ctx.Prefix}music stop`.");
            }

            connection = await Node.ConnectAsync(channel);

            Players.TryRemove(ctx.Guild.Id, out var _);
            Players.TryAdd(ctx.Guild.Id, new MusicPlayer
            {
                Index = 0,
                Message = null,
                Settings = ctx.DatabaseContext.Guild.Music,
                TextChannel = ctx.Channel,
                VoiceChannel = channel,
                Tracks = new List<LavalinkTrack>()
            });

            connection.PlaybackFinished += Connection_PlaybackFinished;

            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().Stylize().WithDescription($"Joined `{channel.Name}`. I am ready to play a song."));
        }

        public async Task LeaveChannelAsync(DiscordChannel channel)
        {
            var connection = Node.GetConnection(channel.Guild);
            if (connection is null)
            {
                throw new FoxException($"The music service is not started yet.");
            }

            Players.TryRemove(channel.Guild.Id, out var _);
            connection.Disconnect();

            await channel.SendMessageAsync(embed: new DiscordEmbedBuilder().Stylize().WithDescription($"The music service has been stopped."));
        }

        public Task SetVolumeAsync(FoxCommandContext ctx, int volume)
        {
            var connection = Node.GetConnection(ctx.Guild);
            if (connection is null)
            {
                throw new FoxException($"The music service is not started yet.");
            }

            if (!Players.TryGetValue(ctx.Guild.Id, out var player))
            {
                throw new FoxException($"The player was not found. It should not happen.");
            }

            player.Settings.Volume = volume;
            ctx.DatabaseContext.UpdateGuild();
            connection.SetVolume(player.Settings.Volume);

            return ctx.RespondAsync(embed: new DiscordEmbedBuilder().StylizeFor(ctx.Member).WithDescription($"The volume has been updated. ({player.Settings.Volume}/150)"));
        }

        public Task PauseAsync(FoxCommandContext ctx)
        {
            var connection = Node.GetConnection(ctx.Guild);
            if (connection is null)
            {
                throw new FoxException($"The music service is not started yet.");
            }

            if (!Players.TryGetValue(ctx.Guild.Id, out var _))
            {
                throw new FoxException($"The player was not found. It should not happen.");
            }

            connection.Pause();

            return ctx.RespondAsync(embed: new DiscordEmbedBuilder().StylizeFor(ctx.Member).WithDescription("The player has been paused."));
        }

        public Task ResumeAsync(FoxCommandContext ctx)
        {
            var connection = Node.GetConnection(ctx.Guild);
            if (connection is null)
            {
                throw new FoxException($"The music service is not started yet.");
            }

            if (!Players.TryGetValue(ctx.Guild.Id, out var _))
            {
                throw new FoxException($"The player was not found. It should not happen.");
            }

            connection.Resume();

            return ctx.RespondAsync(embed: new DiscordEmbedBuilder().StylizeFor(ctx.Member).WithDescription("The player has been resumed."));
        }

        public async Task<LavalinkTrack> ResolveAsync(string url)
        {
            var track = await Node.GetTracksAsync(url);
            return track.Tracks.First();
        }

        public Task SkipAsync(FoxCommandContext ctx, int index = -1)
        {
            var connection = Node.GetConnection(ctx.Guild);
            if (connection is null)
            {
                throw new FoxException($"The music service is not started yet.");
            }

            if (!Players.TryGetValue(ctx.Guild.Id, out var player))
            {
                throw new FoxException($"The player was not found. It should not happen.");
            }

            if (index != -1)
            {
                if (index < 0 || index >= player.Tracks.Count)
                {
                    index = 0;
                }

                player.Index = index;
            }

            connection.Stop();

            return Task.CompletedTask;
        }

        public Task QueueAsync(FoxCommandContext ctx)
        {
            var connection = Node.GetConnection(ctx.Guild);
            if (connection is null)
            {
                throw new FoxException($"The music service is not started yet.");
            }

            if (!Players.TryGetValue(ctx.Guild.Id, out var player))
            {
                throw new FoxException($"The player was not found. It should not happen.");
            }

            var tracks = player.Tracks;
            var page = 1;
            var pages = new List<Page>();
            var str = new StringBuilder();
            var index = 0;
            for (var i = 0; i < tracks.Count; i += 10)
            {
                var currents = tracks.Skip(i).Take(10);

                foreach (var track in currents)
                {
                    str.AppendLine($"`[#{index}] - {track.Format().Replace("`", "'")}`");
                    index++;
                }

                pages.Add(new Page
                {
                    Embed = new DiscordEmbedBuilder().StylizeFor(ctx.Member).WithDescription(str.ToString()).WithTitle($"Page {page}/{(tracks.Count / 10) + 1}")
                });

                str.Clear();
                page++;
            }

            var paginator = new PaginatorService(ctx, pages.ToImmutableArray());
            return paginator.SendAsync(false);
        }

        public async Task ForceJoinAsync(FoxCommandContext ctx)
        {
            var connection = Node.GetConnection(ctx.Guild);
            if (connection is null)
            {
                throw new FoxException($"The music service is not started yet.");
            }

            if (!Players.TryGetValue(ctx.Guild.Id, out var player))
            {
                throw new FoxException($"The player was not found. It should not happen.");
            }

            if (!(ctx.Guild.CurrentMember.VoiceState?.Channel is null))
            {
                throw new FoxException($"I swear I'm already in a voice channel. Please manually move me if you want to change the voice channel.");
            }

            if (ctx.Member.VoiceState?.Channel is null)
            {
                throw new FoxException("You must be in a voice channel to use this command.");
            }

            await ctx.Member.VoiceState?.Channel.ConnectAsync(Node);

            player.VoiceChannel = ctx.Member.VoiceState.Channel;

            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().StylizeFor(ctx.Member).WithDescription($"Joined `{player.VoiceChannel.Name}`. I am ready to play a song."));
        }

        public async Task NowPlayingAsync(FoxCommandContext ctx)
        {
            var connection = Node.GetConnection(ctx.Guild);
            if (connection is null)
            {
                throw new FoxException($"The music service is not started yet.");
            }

            if (!Players.TryGetValue(ctx.Guild.Id, out var player))
            {
                throw new FoxException($"The player was not found. It should not happen.");
            }

            if (player.Message != null)
            {
                try
                {
                    await player.Message.DeleteAsync();
                }
                catch
                {
                    Logger.Print(LogLevel.Warning, "Lavalink", "Unable to remove Music Player's message.");
                }
            }

            var embed = new DiscordEmbedBuilder().StylizeFor(ctx.Member);
            var track = connection.CurrentState.CurrentTrack;

            embed.AddField("Title", track.Title, false);
            embed.AddField("Author", track.Author, true);
            embed.AddField("Duration", $"{connection.CurrentState.PlaybackPosition.ToString("g")} / {track.Length.ToString("g")}", true);
            embed.AddField("Position", $"{player.Index + 1}/{player.Tracks.Count}", true);
            embed.AddField("URL", track.Uri.ToString(), false);
            embed.AddField("Volume", $"{player.Settings.Volume}/150", true);
            embed.AddField("Loop", $"{(player.Settings.Loop ? "Enabled" : "Disabled")}", true);
            embed.AddField("Dj Mode", $"{(player.Settings.DjOnly ? "Enabled" : "Disabled")}", true);

            player.Message = await ctx.RespondAsync(embed: embed);
        }   

        public Task RemoveSongsAsync(FoxCommandContext ctx, string pred)
        {
            var connection = Node.GetConnection(ctx.Guild);
            if (connection is null)
            {
                throw new FoxException($"The music service is not started yet.");
            }

            if (!Players.TryGetValue(ctx.Guild.Id, out var player))
            {
                throw new FoxException($"The player was not found. It should not happen.");
            }

            var amount = player.Tracks.RemoveAll(x => x.Title.Contains(pred, StringComparison.OrdinalIgnoreCase));

            return ctx.RespondAsync(embed: new DiscordEmbedBuilder().StylizeFor(ctx.Member).WithDescription($"{amount} songs have been removed from the queue."));
        }

        public async Task PlayAsync(FoxCommandContext ctx)
        {
            var connection = Node.GetConnection(ctx.Guild);
            if (connection is null)
            {
                if (ctx.Member.VoiceState?.Channel is null)
                {
                    throw new FoxException("You must be in a voice channel to continue.");
                }

                await JoinChannelAsync(ctx, ctx.Member.VoiceState?.Channel);
            }

            if (!Players.TryGetValue(ctx.Guild.Id, out var player))
            {
                throw new FoxException($"The player was not found. It should not happen.");
            }

            await PlayAsync(connection, player);
        }

        public async Task PlayAsync(LavalinkGuildConnection connection, MusicPlayer player)
        {
            if (connection.CurrentState.CurrentTrack.Uri != null)
            {
                return;
            }

            if (player.Index >= player.Tracks.Count || player.Index < 0)
            {
                player.Index = 0;
            }

            var track = player.Tracks[player.Index];

            connection.Play(track);

            var embed = new DiscordEmbedBuilder().Stylize();

            embed.AddField("Title", track.Title, false);
            embed.AddField("Author", track.Author, true);
            embed.AddField("Duration", track.Length.ToString("g"), true);
            embed.AddField("Position", $"{player.Index + 1}/{player.Tracks.Count}", true);
            embed.AddField("URL", track.Uri.ToString(), false);
            embed.AddField("Volume", $"{player.Settings.Volume}/150", true);
            embed.AddField("Loop", $"{(player.Settings.Loop ? "Enabled" : "Disabled")}", true);
            embed.AddField("Dj Mode", $"{(player.Settings.DjOnly ? "Enabled" : "Disabled")}", true);

            player.Message = await player.TextChannel.SendMessageAsync(embed: embed);
        }

        public async Task AddSongAsync(FoxCommandContext ctx, string keywords)
        {
            var tracks = await Node.GetTracksAsync(keywords);
            await AddSongAsync(ctx, tracks.Tracks.First().Uri);
        }

        public async Task AddSongAsync(FoxCommandContext ctx, Uri url)
        {
            var connection = Node.GetConnection(ctx.Guild);
            if (connection is null)
            {
                await JoinChannelAsync(ctx, ctx.Member.VoiceState?.Channel);
                connection = Node.GetConnection(ctx.Guild);
            }

            if (!Players.TryGetValue(ctx.Guild.Id, out var player))
            {
                throw new FoxException($"The player was not found. It should not happen.");
            }

            if (ctx.Member.VoiceState?.Channel is null)
            {
                throw new FoxException("You have to be in a voice channel to use this command.");
            }

            if (ctx.Member.VoiceState?.Channel != player.VoiceChannel)
            {
                throw new FoxException("You have to be in **my** voice channel to use this command.");
            }

            var embed = new DiscordEmbedBuilder().StylizeFor(ctx.Member);

            var tracks = await Node.GetTracksAsync(url);
            switch (tracks.LoadResultType)
            {
                case LavalinkLoadResultType.TrackLoaded:
                    player.Tracks.Add(tracks.Tracks.First());
                    embed.WithDescription($"A track has been added to the queue.\n`{tracks.Tracks.First().Format()}`");
                    break;
                case LavalinkLoadResultType.PlaylistLoaded:
                    player.Tracks.AddRange(tracks.Tracks);
                    embed.WithDescription($"The playlist `{tracks.PlaylistInfo.Name}` ({tracks.Tracks.Count()} tracks) has been added to the queue.");
                    break;
                case LavalinkLoadResultType.SearchResult:
                case LavalinkLoadResultType.NoMatches:
                case LavalinkLoadResultType.LoadFailed:
                    embed.WithDescription($"An error occured when trying to load the song `{url}`.");
                    return;
            }

            await ctx.RespondAsync(embed: embed);

            if (connection.CurrentState?.CurrentTrack.Title is null)
            {
                await PlayAsync(ctx);
            }
        }

        public async Task UpdateLoopSettingsAsync(FoxCommandContext ctx)
        {
            var connection = Node.GetConnection(ctx.Guild);
            if (connection is null)
            {
                throw new FoxException($"The music service is not started yet.");
            }

            if (!Players.TryGetValue(ctx.Guild.Id, out var player))
            {
                throw new FoxException($"The player was not found. It should not happen.");
            }

            player.Settings.Loop = !player.Settings.Loop;
            ctx.DatabaseContext.UpdateGuild();

            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().StylizeFor(ctx.Member).WithDescription(player.Settings.Loop ? "The playlist will be played indefinitely." : "The player will stop after the last song."));
        }

        public async Task ShuffleAsync(FoxCommandContext ctx)
        {
            var connection = Node.GetConnection(ctx.Guild);
            if (connection is null)
            {
                throw new FoxException($"The music service is not started yet.");
            }

            if (!Players.TryGetValue(ctx.Guild.Id, out var player))
            {
                throw new FoxException($"The player was not found. It should not happen.");
            }

            player.Tracks.Shuffle();

            await ctx.RespondAsync(embed: new DiscordEmbedBuilder().StylizeFor(ctx.Member).WithDescription("The queue has been shuffled."));
        }

        public async Task SeekAsync(FoxCommandContext ctx, TimeSpan ts)
        {
            var connection = Node.GetConnection(ctx.Guild);
            if (connection is null)
            {
                throw new FoxException($"The music service is not started yet.");
            }

            if (!Players.TryGetValue(ctx.Guild.Id, out var _))
            {
                throw new FoxException($"The player was not found. It should not happen.");
            }

            connection.Seek(ts);

            await ctx.RespondAsync(embed : new DiscordEmbedBuilder().StylizeFor(ctx.Member).WithDescription($"Seeking to {ts.Humanize()}"));
        }

        private async Task Connection_PlaybackFinished(TrackFinishEventArgs e)
        {
            if (!Players.TryGetValue(e.Player.Guild.Id, out var player))
            {
                return;
            }

            if (player.Message != null)
            {
                try
                {
                    await player.Message.DeleteAsync();
                }
                catch
                {
                    Logger.Print(LogLevel.Warning, "Lavalink", "Unable to remove Music Player's message.");
                }
            }

            if (e.Reason == TrackEndReason.Cleanup)
            {
                await player.TextChannel.SendMessageAsync("The song ended with the lavalink reason CLEANUP. It should not happen. Try switching to another Discord Voice Region.");
            }

            if (player.Tracks.Count == 0)
            {
                await LeaveChannelAsync(player.TextChannel);
                return;
            }

            player.Index++;

            if (player.Index == player.Tracks.Count && player.Settings.Loop)
            {
                player.Index = 0;

                await PlayAsync(e.Player, player);
            }
            else if (player.Index == player.Tracks.Count && !player.Settings.Loop)
            {
                await LeaveChannelAsync(player.TextChannel);
            }
            else
            {
                await PlayAsync(e.Player, player);
            }
        }
    }
}
