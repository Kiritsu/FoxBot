﻿using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Fox.Entities;
using Qmmands;

namespace Fox.Commands.TypeParsers
{
    public sealed class DiscordChannelTypeParser : TypeParser<DiscordChannel>
    {
        public override Task<TypeParserResult<DiscordChannel>> ParseAsync(Parameter parameter, string value, ICommandContext context, IServiceProvider provider)
        {
            if (!(context is FoxCommandContext ctx))
            {
                return Task.FromResult(new TypeParserResult<DiscordChannel>("A channel cannot exist in that context."));
            }

            if (ctx.Guild is null)
            {
                return Task.FromResult(new TypeParserResult<DiscordChannel>("This command must be used in a guild."));
            }

            DiscordChannel channel = null;
            if ((value.Length > 3 && value[0] == '<' && value[1] == '#' && value[value.Length - 1] == '>' && ulong.TryParse(value.Substring(2, value.Length - 3), out var id))
                || ulong.TryParse(value, out id))
            {
                channel = ctx.Guild.Channels.FirstOrDefault(x => x.Id == id);
            }

            if (channel is null)
            {
                channel = ctx.Guild.Channels.FirstOrDefault(x => x.Name == value);
            }

            if (channel is null && value.StartsWith('#'))
            {
                channel = ctx.Guild.Channels.FirstOrDefault(x => x.Type == ChannelType.Text && x.Name == value.Substring(1));
            }

            return channel == null
                ? Task.FromResult(new TypeParserResult<DiscordChannel>("No channel found matching the input."))
                : Task.FromResult(new TypeParserResult<DiscordChannel>(channel));
        }
    }
}
