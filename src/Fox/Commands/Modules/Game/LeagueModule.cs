using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Fox.Entities;
using Fox.Extensions;
using Fox.Services;
using MingweiSamuel.Camille.Enums;
using Qmmands;

namespace Fox.Commands.Modules.Game
{
    [Group("Lol", "Leagueoflegends", "Rito", "Riot"), Name("LeagueOfLegends")]
    public sealed class LeagueModule : FoxModuleBase
    {
        private readonly RiotService _riot;

        public LeagueModule(RiotService riot)
        {
            _riot = riot;
        }

        [Command("Region")]
        [Description("Returns the current region in the current channel.")]
        public Task RegionAsync()
        {
            var region = DbContext.Channel.Region;
            return SimpleEmbedAsync($"The current region is `{region.Key}` on platform `{region.Platform}`.");
        }

        [Command("SetRegion")]
        [Description("Changes the region in the current channel.")]
        public Task SetRegionAsync(Region region)
        {
            DbContext.Channel.RiotRegion = region.Key;
            DbContext.UpdateChannel();
            return SimpleEmbedAsync($"The current region has been changed to `{region.Key}` on platform `{region.Platform}`.");
        }

        [Command("FullProfile")]
        [Description("Returns the full profile.")]
        public async Task FullProfileAsync(string username)
        {
            var msg = await SimpleEmbedAsync("Loading... Please wait.");

            var pages = new List<PaginatorService.Page>
            {
                new PaginatorService.Page { Embed = await _riot.CreateEmbedForProfileAsync(Context, username), Identifier = "Profile" }
            };

            pages.AddRange(await _riot.CreatePaginatorPagesForSummonerLeaguesAsync(Context, username));
            pages.AddRange(await _riot.CreatePaginatorPagesForChampionMasteriesAsync(Context, username));

            await msg.DeleteAsync();
            await PaginateAsync(pages);
        }

        [Command("Profile", "Summoner")]
        [Description("Shows the League of Legends account profile for the given username.")]
        public async Task ProfileAsync([Remainder] string username)
        {
            var msg = await SimpleEmbedAsync("Loading... Please wait.");

            var embed = await _riot.CreateEmbedForProfileAsync(Context, username);

            await msg.ModifyAsync(embed: embed.StylizeFor(Context.Member).Build());
        }

        [Command("Masteries", "ChampionMasteries")]
        [Description("Shows your champion masteries.")]
        public async Task Masteries([Remainder] string username)
        {
            var msg = await SimpleEmbedAsync("Loading... Please wait.");

            var pages = await _riot.CreatePaginatorPagesForChampionMasteriesAsync(Context, username);

            await msg.DeleteAsync();
            await PaginateAsync(pages);
        }

        [Command("Leagues", "League")]
        [Description("Shows your leagues.")]
        public async Task LeaguesAsync([Remainder] string username)
        {
            var msg = await SimpleEmbedAsync("Loading... Please wait.");

            var pages = await _riot.CreatePaginatorPagesForSummonerLeaguesAsync(Context, username);

            await msg.DeleteAsync();
            await PaginateAsync(pages, false);
        }
    }
}
