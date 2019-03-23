using System;
using System.Threading.Tasks;
using DSharpPlus;
using Fox.Commands.Checks;
using Fox.Entities;
using Fox.Exceptions;
using Fox.Services;
using Qmmands;

namespace Fox.Commands.Modules.Music
{
    [Name("Music"), Group("Music"), CheckLavalinkState]
    [Description("Modules used to play music in your voice channel.")]
    [RunMode(RunMode.Parallel)]
    public sealed class MusicModule : FoxModuleBase
    {
        private readonly MusicService _music;

        public MusicModule(MusicService music)
        {
            _music = music;
        }

        [Command("DjMode", "DjOnly", "Dj")]
        [Description("Toggles on/off the dj role only mode. If enabled, only members with a role 'DJ' can use the module.")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public Task DjModeAsync()
        {
            DbContext.Guild.Music.DjOnly = !DbContext.Guild.Music.DjOnly;
            DbContext.UpdateGuild();

            return SimpleEmbedAsync($"DJ Mode Only has been {(DbContext.Guild.Music.DjOnly ? "enabled" : "disabled")}.");
        }

        [Command("Join", "Start")]
        [Description("Starts the music player for this guild.")]
        [RequireDj]
        public Task JoinAsync()
        {
            if (Context.Member.VoiceState?.Channel is null)
            {
                throw new FoxException("You have to be in a voice channel to use this command.");
            }

            return _music.JoinChannelAsync(Context, Context.Member.VoiceState.Channel);
        }

        [Command("Stop", "Leave")]
        [Description("Stops the music player for this guild.")]
        [RequireDj]
        public Task StopAsync()
        {
            if (Context.Guild.CurrentMember.VoiceState?.Channel != null && Context.Member.VoiceState?.Channel != Context.Guild.CurrentMember.VoiceState?.Channel)
            {
                throw new FoxException("You have to be in **my** voice channel to use this command.");
            }

            return _music.LeaveChannelAsync(Context.Channel);
        }

        [Command("Pause", "Freeze")]
        [Description("Pauses the music player for this guild.")]
        [RequireDj]
        public Task PauseAsync()
        {
            if (Context.Guild.CurrentMember.VoiceState?.Channel != null && Context.Member.VoiceState?.Channel != Context.Guild.CurrentMember.VoiceState?.Channel)
            {
                throw new FoxException("You have to be in **my** voice channel to use this command.");
            }

            return _music.PauseAsync(Context);
        }

        [Command("Resume", "Restart")]
        [Description("Resumes the music player for this guild.")]
        [RequireDj]
        public Task ResumeAsync()
        {
            if (Context.Guild.CurrentMember.VoiceState?.Channel != null && Context.Member.VoiceState?.Channel != Context.Guild.CurrentMember.VoiceState?.Channel)
            {
                throw new FoxException("You have to be in **my** voice channel to use this command.");
            }

            return _music.ResumeAsync(Context);
        }

        [Command("Volume", "Vol")]
        [Description("Changes the volume of the music player for this guild.")]
        [RequireDj]
        public Task VolumeAsync(int volume = 75)
        {
            if (Context.Guild.CurrentMember.VoiceState?.Channel != null && Context.Member.VoiceState?.Channel != Context.Guild.CurrentMember.VoiceState?.Channel)
            {
                throw new FoxException("You have to be in **my** voice channel to use this command.");
            }

            return _music.SetVolumeAsync(Context, volume);
        }

        [Command("Next", "Skip", "Jump")]
        [Description("Skip the current song for this guild's song queue.")]
        [RequireDj]
        public Task NextAsync([Description("Optional index of the song")] int index = 0)
        {
            if (Context.Guild.CurrentMember.VoiceState?.Channel != null && Context.Member.VoiceState?.Channel != Context.Guild.CurrentMember.VoiceState?.Channel)
            {
                throw new FoxException("You have to be in **my** voice channel to use this command.");
            }

            return _music.SkipAsync(Context, index - 1);
        }

        [Command("Loop", "Repeat")]
        [Description("Toggles on/off the queue repeatition of the music player for this guild.")]
        [RequireDj]
        public Task LoopAsync()
        {
            if (Context.Guild.CurrentMember.VoiceState?.Channel != null && Context.Member.VoiceState?.Channel != Context.Guild.CurrentMember.VoiceState?.Channel)
            {
                throw new FoxException("You have to be in **my** voice channel to use this command.");
            }

            return _music.UpdateLoopSettingsAsync(Context);
        }

        [Command("Shuffle")]
        [Description("Shuffles the queue of the music player for this guild.")]
        [RequireDj]
        public Task ShuffleAsync()
        {
            if (Context.Guild.CurrentMember.VoiceState?.Channel != null && Context.Member.VoiceState?.Channel != Context.Guild.CurrentMember.VoiceState?.Channel)
            {
                throw new FoxException("You have to be in **my** voice channel to use this command.");
            }

            return _music.ShuffleAsync(Context);
        }

        [Command("Play", "Add")]
        [Description("Adds a song to the queue of the music player for this guild.")]
        [RequireDj]
        public async Task PlayAsync(Uri uri)
        {
            await _music.AddSongAsync(Context, uri);
        }

        [Command("Play", "Add")]
        [Description("Adds a song to the queue of the music player for this guild.")]
        [RequireDj]
        public Task PlayAsync([Remainder] string keywords)
        {
            return _music.AddSongAsync(Context, keywords);
        }

        [Command("Queue", "List")]
        [Description("Lists the current music player's queue.")]
        public Task QueueAsync()
        {
            return _music.QueueAsync(Context);
        }

        [Command("Remove")]
        [Description("Removes every song matching the given string from the music player's queue.")]
        [RequireDj]
        public Task RemoveAsync([Description("Text that the songs to remove must match with.")] [Remainder] string predicate)
        {
            return _music.RemoveSongsAsync(Context, predicate);
        }

        [Command("NowPlaying", "Np")]
        [Description("Shows the song being played at the current moment.")]
        public Task NowPlayingAsync()
        {
            return _music.NowPlayingAsync(Context);
        }

        [Command("ForceJoin")]
        [Description("Forces the bot to join the current user's voice channel.")]
        [RequireDj]
        public Task ForceJoinAsync()
        {
            return _music.ForceJoinAsync(Context);
        }

        [Command("Seek")]
        [Description("Seeks the current song to the specified timestamp.")]
        [RequireDj]
        public Task SeekAsync(TimeSpan ts)
        {
            return _music.SeekAsync(Context, ts);
        }
    }
}
