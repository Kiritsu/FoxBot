using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Fox.Commands.Checks;
using Fox.Entities;
using Fox.Extensions;
using Fox.Services;
using Qmmands;
using static Fox.Services.PaginatorService;

namespace Fox.Commands.Modules
{
    [Name("Misc"), CheckModuleState]
    public sealed class MiscModule : FoxModuleBase
    {
        [Command("Invite")]
        [Description("Returns the different bot OAuth2 Authorize URLs.")]
        public Task InviteAsync()
        {
            return RespondAsync(new DiscordEmbedBuilder()
                               .WithColor(ConfigurationService.EmbedColor)
                               .WithTitle("Bot Invitation URLs")
                               .WithDescription($"[Full Permissions](https://discordapp.com/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&scope=bot&permissions=8), " +
                                                $"[Permissionless](https://discordapp.com/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&scope=bot)"));
        }

        [Command("Ping")]
        [Description("Returns the current ping/latency.")]
        public async Task PingAsync([Description("Host on which you wanna ping.")] string host = null)
        {
            host = host ?? "google.com";

            long distant = 0;
            long local = 0;

            var pingIns = new Ping();

            try
            {
                distant = (await pingIns.SendPingAsync(host)).RoundtripTime;

                local = (await pingIns.SendPingAsync("localhost")).RoundtripTime;
            }
            catch (Exception)
            {
                host += ": timed out";
            }

            var emb = new DiscordEmbedBuilder()
                .WithColor(ConfigurationService.EmbedColor)
                .WithDescription($":heart:  |  {Context.Client.Ping}ms " +
                    $"\n:fox:  |  {local}ms (localhost)" +
                    $"\n:earth_americas:  |  {distant}ms ({host})")
                .WithTitle("Current latency : (websocket, local host, defined host, messages)");

            var sw = Stopwatch.StartNew();
            var message = await RespondAsync(emb).ConfigureAwait(false);
            sw.Stop();

            emb.Description += $"\n:e_mail:  |  {sw.ElapsedMilliseconds}ms";

            for (var i = 0; i < 5; i++)
            {
                sw.Restart();
                await message.ModifyAsync(
                    embed: Optional<DiscordEmbed>.FromValue(emb)).ConfigureAwait(false);
                sw.Stop();

                emb.Description += $", {sw.ElapsedMilliseconds}ms";
            }
        }

        [Command("GuildId", "IdOf")]
        [Description("Returns the current guild id.")]
        public Task GuildIdAsync()
        {
            return RespondAsync($"Id of the guild: {Context.Guild.Id}");
        }

        [Command("ChannelId", "IdOf")]
        [Description("Returns the given channel id.")]
        public Task ChannelIdAsync(DiscordChannel chn = null)
        {
            chn = chn ?? Context.Channel;
            return RespondAsync($"Id of the channel: {chn.Mention}: {chn.Id}");
        }

        [Command("UserId", "IdOf")]
        [Description("Returns the given user id.")]
        public Task UserIdAsync([Remainder] SkeletonUser user = null)
        {
            user = user ?? new SkeletonUser(Context.User);
            return RespondAsync($"Id of the user: `{user.FormatUser()}`: {user.Id}");
        }

        [Command("RoleId", "IdOf")]
        [Description("Returns the given role id.")]
        public Task RoleIdAsync([Remainder] DiscordRole role)
        {
            return RespondAsync($"Id of the role: `{role.Id}`");
        }

        [Command("Avatar")]
        [Description("Returns the given or current user's avatar.")]
        public Task AvatarAsync([Remainder] SkeletonUser user = null)
        {
            user = user ?? new SkeletonUser(Context.User);
            return RespondAsync(embed: new DiscordEmbedBuilder().WithImageUrl(user.AvatarUrl));
        }

        [Command("GuildInfo", "Info")]
        [Description("Returns the current guild's informations.")]
        public async Task GuildInfoAsync()
        {
            var roles = "No role.";
            if (Context.Guild.Roles.Any())
            {
                roles = string.Join(", ", Context.Guild.Roles.OrderByDescending(x => x.Position).Select(x => x.Name));
            }

            await Context.Guild.GetAllMembersAsync();

            var emb = new DiscordEmbedBuilder
            {
                ThumbnailUrl = Context.Guild.IconUrl
            }.AddField("__Name__", Context.Guild.Name, true)
            .AddField("__Id__", Context.Guild.Id.ToString(), true)
            .AddField("__Owner__", Context.Guild.Owner.FormatUser(), true)
            .AddField("__Creation Date__", Context.Guild.CreationTimestamp.ToString("G"), true)
            .AddField("__Members__", Context.Guild.Members.Count.ToString(), true)
            .AddField("__Bots__", Context.Guild.Members.Where(x => x.IsBot).Count().ToString(), true)
            .AddField("__Roles__", Context.Guild.Roles.Count.ToString(), true)
            .AddField("__Channels__", Context.Guild.Channels.Count.ToString(), true)
            .AddField("__Role List__", roles, true)
            .AddField("__Icon Url__", Context.Guild.IconUrl, true);

            await RespondAsync(emb);
        }

        [Command("UserInfo", "Info")]
        [Description("Returns the given or current user's informations.")]
        public Task UserInfoAsync([Remainder] DiscordMember user)
        {
            var history = "";
            var list = Context.Guild.Members.OrderBy(x => x.JoinedAt).ToList();
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Id != user.Id)
                {
                    continue;
                }

                history = i - 1 >= 0
                    ? $"({i - 1}) " + list[i - 1].FormatUser() + $" -> **({i}) " + list[i].FormatUser() + "**"
                    : $"**({i}) " + list[i].FormatUser() + "**";

                if (i + 1 < list.Count)
                {
                    history += $" -> ({i + 1}) " + list[i + 1].FormatUser();
                }
            }

            var roles = "No role.";
            if (user.Roles.Any())
            {
                roles = string.Join(", ", user.Roles.OrderByDescending(x => x.Position).Select(x => x.Name));
            }

            var emb = new DiscordEmbedBuilder
            {
                ThumbnailUrl = user.AvatarUrl
            }.AddField("__Username__", $"{user.FormatUser()} ({user.Username})", false)
            .AddField("__Id__", user.Id.ToString(), false)
            .AddField("__Status__", user.Presence is null ? "Offline/Invisible" : user.Presence.Status.ToString(), true)
            .AddField("__Game__", user.Presence is null ? "Not playing." : !string.IsNullOrWhiteSpace(user.Presence.Activity.Name) ? $"{user.Presence.Activity.ActivityType} {user.Presence.Activity.Name}" : "Not playing.", true)
            .AddField("__Join Date__", user.JoinedAt.ToString("G"), true)
            .AddField("__Creation Date__", user.CreationTimestamp.ToString("G"), true)
            .AddField("__Join Position__", history, false)
            .AddField("__Account Type__", user.IsBot ? "Bot" : "User", true)
            .AddField("__Hierarchy__", user.IsOwner ? "Owner" : $"Member ({user.Hierarchy})", true)
            .AddField("__Roles__", roles, true);

            return RespondAsync(emb);
        }

        [Command("Emotes", "Emojis")]
        [Description("Returns guild's custom emojis")]
        public Task EmotesAsync()
        {
            var pages = new List<Page>();
            var emojis = Context.Guild.Emojis.Where(x => x.Roles.Count == 0 || x.Roles.Any(y => Context.Guild.CurrentMember.Roles.Any(z => y == z.Id))).ToList();

            var page = 1;
            for (var i = 0; i < emojis.Count; i += 10)
            {
                var currents = emojis.Skip(i).Take(10);

                pages.Add(new Page
                {
                    Embed = new DiscordEmbedBuilder().StylizeFor(Context.User).WithDescription(string.Join(", ", currents.Select(x => "[<" + (x.IsAnimated ? "a" : "") + $":{x.Name}:{x.Id}>](https://cdn.discordapp.com/emojis/{x.Id})"))).WithTitle($"Page {page}/{(emojis.Count / 10) + 1}")
                });

                page++;
            }

            return PaginateAsync(pages);
        }

        [Command("Status")]
        [Description("Displays bot status")]
        public Task StatusAsync()
        {
            var embed = new DiscordEmbedBuilder();

            embed.AddField("__ID__", Context.Client.CurrentUser.Id.ToString(), true);
            embed.AddField("__Owner__", Context.Client.CurrentApplication.Owner.FormatUser(), true);
            embed.AddField("__Version__", "2.1", true);
            embed.AddField("__Guilds__", Context.Client.Guilds.Count.ToString(), true);
            embed.AddField("__Channels__", Context.Client.Guilds.SelectMany(x => x.Value.Channels).DistinctBy(x => x.Id).Count().ToString(), true);
            embed.AddField("__Users__", Context.Client.Guilds.SelectMany(x => x.Value.Members).DistinctBy(x => x.Id).Count().ToString(), true);
            embed.AddField("__Ping__", $"{Context.Client.Ping} ms", true);
            embed.AddField("__Ram__", $"{GetHeapSize()} MB", true);
            embed.AddField("\u200B", "\u200B", true);
            embed.AddField("__Uptime__", (DateTime.Now - Process.GetCurrentProcess().StartTime).Humanize(), true);

            return RespondAsync(embed);
        }

        [Command("Levenshtein")]
        [Description("Toggles on/off the auto-correct of commands.")]
        public Task LevenshteinAsync()
        {
            DbContext.Guild.Levenshtein = !DbContext.Guild.Levenshtein;
            DbContext.UpdateGuild();

            return SimpleEmbedAsync($"Levenshtein has been {(DbContext.Guild.Levenshtein ? "enabled" : "disabled")} on that guild.");
        }

        private static string GetHeapSize()
        {
            return Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString(CultureInfo.CurrentCulture);
        }
    }
}
