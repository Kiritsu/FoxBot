#pragma warning disable IDE0046
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Fox.Services
{
    public sealed class EventHandlerService
    {
        private readonly MusicService _music;
        private readonly DiscordClient _client;
        private readonly LogService _logger;
        private readonly ConfigurationService _config;
        private readonly Random _rng;

        public EventHandlerService(DiscordClient client, LogService logger, ConfigurationService cfg, Random rng, MusicService music)
        {
            _client = client;
            _logger = logger;
            _config = cfg;
            _rng = rng;
            _music = music;
        }

        public void Initialize()
        {
            _client.ClientErrored += OnClientErrored;
            _client.Ready += OnReadyAsync;
        }

        private Task OnReadyAsync(ReadyEventArgs e)
        {
            _ = Task.Run(() => UpdateGamesAsync(e.Client));

            _logger.Print(LogLevel.Info, "Fox", "Fox is ready.");

            return _music.StartLavalinkAsync();
        }

        private async Task UpdateGamesAsync(DiscordClient client)
        {
            var messages = new List<string>();
            messages.AddRange(_config.BotStatus.Playings);
            messages.AddRange(_config.BotStatus.Listenings);
            messages.AddRange(_config.BotStatus.Watchings);

            var msgs = messages.AsReadOnly();

            var index = 0;

            while (true)
            {
                if (_config.BotStatus.Random)
                {
                    index = _rng.Next(msgs.Count);
                }
                else
                {
                    index += 1;

                    if (index == msgs.Count)
                    {
                        index = 0;
                    }
                }

                var status = GetStatusFromIndexPosition(index);

                await client.UpdateStatusAsync(new DiscordActivity(msgs[index], status));

                if (_config.BotStatus.Delay == TimeSpan.Zero)
                {
                    _logger.Print(LogLevel.Warning, "Auto Status", "Your delay is not properly set, disabling the auto status.");
                    return;
                }

                await Task.Delay(_config.BotStatus.Delay);
            }
        }

        private ActivityType GetStatusFromIndexPosition(int index)
        {
            if (index < _config.BotStatus.Playings.Length)
            {
                return ActivityType.Playing;
            }

            if (index < _config.BotStatus.Playings.Length + _config.BotStatus.Listenings.Length)
            {
                return ActivityType.ListeningTo;
            }

            if (index < _config.BotStatus.Playings.Length + _config.BotStatus.Listenings.Length + _config.BotStatus.Watchings.Length)
            {
                return ActivityType.Watching;
            }

            return ActivityType.Playing;
        }

        private Task OnClientErrored(ClientErrorEventArgs e)
        {
            var ex = e.Exception;
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
            }

            _logger.Print(LogLevel.Critical, e.EventName, $"{ex.Message}: {ex.InnerException}");

            return Task.CompletedTask;
        }
    }
}
