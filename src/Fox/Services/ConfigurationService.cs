using System;
using System.IO;
using DSharpPlus.Entities;
using Newtonsoft.Json;

namespace Fox.Services
{
    public sealed class ConfigurationService
    {
        [JsonProperty("prefix")]
        public string Prefix { get; set; }

        [JsonProperty("api_keys")]
        public ApiKeys Keys { get; set; }

        [JsonProperty("status")]
        public Status BotStatus { get; set; }

        [JsonProperty("db_password")]
        public string DbPassword { get; set; }

        [JsonProperty("bypass_checks")]
        public bool BypassChecks { get; set; }

        [JsonProperty("owner_ids")]
        public ulong[] OwnerIds { get; set; }

        public static DiscordColor EmbedColor { get; } = new DiscordColor(0xFF9933);

        public static ConfigurationService Initialize()
        {
            if (!File.Exists("credentials.json"))
            {
                throw new FileNotFoundException(
                    "The file 'credentials.json' was not found and is required in order to run the bot. " +
                    "Please fill credentials.json.example and rename into credentials.json!");
            }

            return JsonConvert.DeserializeObject<ConfigurationService>(File.ReadAllText("credentials.json"));
        }
    }

    public sealed class ApiKeys
    {
        [JsonProperty("discord")]
        public string Discord { get; set; }

        [JsonProperty("osu")]
        public string Osu { get; set; }

        [JsonProperty("riot")]
        public string Riot { get; set; }
    }

    public sealed class Status
    {
        [JsonProperty("random")]
        public bool Random { get; set; }

        [JsonProperty("playing")]
        public string[] Playings { get; set; }

        [JsonProperty("watching")]
        public string[] Watchings { get; set; }

        [JsonProperty("listening")]
        public string[] Listenings { get; set; }

        [JsonProperty("delay")]
        public int DelayInt { get; set; }

        public TimeSpan Delay
            => TimeSpan.FromSeconds(DelayInt);
    }
}
