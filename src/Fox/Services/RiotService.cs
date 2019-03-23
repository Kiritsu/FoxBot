using DSharpPlus;
using DSharpPlus.Entities;
using Fox.Entities;
using MingweiSamuel.Camille;
using MingweiSamuel.Camille.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fox.Services
{
    public sealed class RiotService
    {
        public RiotApi Api { get; }

        public DiscordEmoji ChestEmote { get; }
        public DiscordEmoji Red { get; }
        public DiscordEmoji BlackFire { get; }
        public DiscordEmoji Fire { get; }
        public DiscordEmoji BlackNew { get; }
        public DiscordEmoji New { get; }
        public DiscordEmoji BlackTrophy { get; }
        public DiscordEmoji Trophy { get; }
        public DiscordEmoji BlackStar { get; }
        public DiscordEmoji Star { get; }

        private readonly DiscordClient _client;

        public RiotService(ConfigurationService config, DiscordClient client)
        {
            Api = RiotApi.NewInstance(config.Keys.Riot);
            _client = client;

            Red = DiscordEmoji.FromUnicode("🚫");
            ChestEmote = DiscordEmoji.FromGuildEmote(_client, 556432679269040147);

            BlackFire = DiscordEmoji.FromGuildEmote(_client, 556912883154550823);
            BlackStar = DiscordEmoji.FromGuildEmote(_client, 556902522028163092);
            BlackTrophy = DiscordEmoji.FromGuildEmote(_client, 556901212029452308);
            BlackNew = DiscordEmoji.FromGuildEmote(_client, 556902437257216040);

            Fire = DiscordEmoji.FromUnicode("🔥");
            Star = DiscordEmoji.FromUnicode("⭐");
            Trophy = DiscordEmoji.FromUnicode("🏆");
            New = DiscordEmoji.FromUnicode("🆕");
        }

        public async Task<DiscordEmbedBuilder> CreateEmbedForProfileAsync(FoxCommandContext ctx, string username)
        {
            var region = ctx.DatabaseContext.Channel.Region;
            var profile = await Api.SummonerV4.GetBySummonerNameAsync(region, username);
            var championMasteryScore = await Api.ChampionMasteryV4.GetChampionMasteryScoreAsync(region, profile.Id);
            var championMasteries = (await Api.ChampionMasteryV4.GetAllChampionMasteriesAsync(region, profile.Id)).Take(5);

            var embed = new DiscordEmbedBuilder
            {
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = $"League of Legends - Profile [{region.Key}]"
                },
                Color = ConfigurationService.EmbedColor,
                Description = "This page shows your basic `League of Legends` summoner profile informations.",
                ThumbnailUrl = $"https://ddragon.leagueoflegends.com/cdn/9.5.1/img/profileicon/{profile.ProfileIconId}.png"
            };

            embed.AddField("Username", profile.Name, true);
            embed.AddField("Level", profile.SummonerLevel.ToString(), true);
            embed.AddField($"Top 5 Champions ({championMasteryScore} of champion mastery score)", string.Join("\n", championMasteries.Select(x => $"{(x.ChestGranted ? ChestEmote.ToString() : Red.ToString())} Lv.`{x.ChampionLevel}` | `{((Champion)x.ChampionId).Name()}` - `{x.ChampionPoints.ToString("N0")}` pts {(x.ChampionLevel >= 5 && x.ChampionLevel < 7 ? $" - `{x.TokensEarned}/{x.ChampionLevel - 3}` tokens" : "")}")), true);

            return embed;
        }

        public async Task<List<PaginatorService.Page>> CreatePaginatorPagesForChampionMasteriesAsync(FoxCommandContext ctx, string username)
        {
            var region = ctx.DatabaseContext.Channel.Region;
            var profile = await Api.SummonerV4.GetBySummonerNameAsync(region, username);

            var championMasteries = await Api.ChampionMasteryV4.GetAllChampionMasteriesAsync(region, profile.Id);

            var maxPage = championMasteries.Length + 1;

            var embed = new DiscordEmbedBuilder
            {
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = $"League of Legends - Champion Masteries [{region.Key}]"
                },
                Color = ConfigurationService.EmbedColor,
                Description = "This is a paginated command. Use the arrows to switch from the different pages. They contain your champion masteries informations.\n\nAdd a reaction to the 🔠 Emoji and type your champion name to switch to its stats.",
                ThumbnailUrl = $"https://ddragon.leagueoflegends.com/cdn/9.5.1/img/profileicon/{profile.ProfileIconId}.png",
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{username} | Paginator - Page 1/{maxPage}"
                }
            };

            var pages = new List<PaginatorService.Page>
            {
                new PaginatorService.Page { Embed = embed.Build() }
            };

            embed.Description = "";

            var currentPage = 2;

            foreach (var championMastery in championMasteries)
            {
                var championIdentifier = ((Champion)championMastery.ChampionId).Identifier();

                embed.ClearFields();
                embed.ThumbnailUrl = $"http://ddragon.leagueoflegends.com/cdn/9.5.1/img/champion/{championIdentifier}.png";
                embed.Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{username} | Paginator - Page {currentPage}/{maxPage}"
                };

                embed.AddField("Level", championMastery.ChampionLevel.ToString(), true);
                embed.AddField("Points", championMastery.ChampionPoints.ToString("N0"), true);
                embed.AddField("Chest", $"{(!championMastery.ChestGranted ? "Not" : "")} Granted", true);

                if (championMastery.ChampionLevel >= 5 && championMastery.ChampionLevel < 7)
                {
                    embed.AddField("Tokens", $"{championMastery.TokensEarned}/{championMastery.ChampionLevel - 3} tokens for next level.", true);
                }

                embed.AddField("Last play time", DateTimeOffset.FromUnixTimeMilliseconds(championMastery.LastPlayTime).ToString("G"), true);

                currentPage++;

                pages.Add(new PaginatorService.Page { Embed = embed.Build(), Identifier = championIdentifier });
            }

            return pages;
        }

        public async Task<List<PaginatorService.Page>> CreatePaginatorPagesForSummonerLeaguesAsync(FoxCommandContext ctx, string username)
        {
            var region = ctx.DatabaseContext.Channel.Region;
            var profile = await Api.SummonerV4.GetBySummonerNameAsync(region, username);
            var leagues = await Api.LeagueV4.GetAllLeaguePositionsForSummonerAsync(region, profile.Id);

            var maxPage = leagues.Length + 1;

            var embed = new DiscordEmbedBuilder
            {
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = $"League of Legends - Summoner Leagues [{region.Key}]"
                },
                Description = "This is a paginated command. Use the arrows to switch from the different pages. They contain your different leagues informations.\n\nThe trophy emoji mean you've been in this league for 100 games. The fire emoji mean you're on a win streak. The NEW emoji mean you're new to this league.",
                Color = ConfigurationService.EmbedColor,
                ThumbnailUrl = $"https://ddragon.leagueoflegends.com/cdn/9.5.1/img/profileicon/{profile.ProfileIconId}.png",
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{username} | Paginator - Page 1/{maxPage}"
                }
            };

            var pages = new List<PaginatorService.Page>
            {
                new PaginatorService.Page { Embed = embed.Build() }
            };

            embed.Description = "";

            var currentPage = 2;

            foreach (var league in leagues)
            {
                embed.ClearFields();

                embed.ThumbnailUrl = $"https://riot.alnmrc.com/emblems/{league.Tier}.png";
                embed.Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{username} | Paginator - Page {currentPage}/{maxPage}"
                };

                embed.AddField("League Name", league.LeagueName, true);
                embed.AddField("Queue Type", league.QueueType, true);

                embed.AddField("Tier", $"{league.Tier} {league.Rank} [{league.LeaguePoints} LP]\n{league.Wins}W {league.Losses}D\nWin Ratio {Math.Round((double)league.Wins / (league.Wins + league.Losses), 2) * 100}%", true);
                embed.AddField("Extra", $"{(league.Veteran ? Trophy : BlackTrophy)}, {(league.HotStreak ? Fire : BlackFire)}, {(league.FreshBlood ? New : BlackNew)}{(league.Inactive ? "\n\nYou're inactive. Warning, you are able to lose some LP/Tier/Ranks." : "")}", true);

                if (league.MiniSeries != null)
                {
                    embed.AddField($"Serie Progress (BO{league.MiniSeries.Target})", $"{league.MiniSeries.Progress}");
                }

                currentPage++;

                pages.Add(new PaginatorService.Page { Embed = embed.Build() });
            }

            return pages;
        }
    }
}
