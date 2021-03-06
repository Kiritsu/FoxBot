﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Fox.Entities;
using Fox.Extensions;
using Qmmands;

namespace Fox.Commands.TypeParsers
{
    public sealed class DiscordUserTypeParser : TypeParser<DiscordUser>
    {
        public override Task<TypeParserResult<DiscordUser>> ParseAsync(Parameter parameter, string value, ICommandContext context, IServiceProvider provider)
        {
            if (!(context is FoxCommandContext ctx))
            {
                return Task.FromResult(new TypeParserResult<DiscordUser>("A user cannot exist in that context."));
            }

            var users = new List<DiscordUser>(ctx.Client.Guilds.SelectMany(x => x.Value.Members));
            if (ctx.Guild != null)
            {
                users.AddRange(ctx.Guild.Members);
            }
            else if (ctx.Channel is DiscordDmChannel chn)
            {
                users.AddRange(chn.Recipients);
            }

            users = users.DistinctBy(x => x.Id).ToList();

            DiscordUser user = null;
            if ((value.Length > 3 && value[0] == '<' && value[1] == '@' && value[value.Length - 1] == '>' && ulong.TryParse(value[2] == '!' ? value.Substring(3, value.Length - 4) : value.Substring(2, value.Length - 3), out var id))
                || ulong.TryParse(value, out id))
            {
                user = users.FirstOrDefault(x => x.Id == id);
            }

            if (user == null)
            {
                var hashIndex = value.LastIndexOf('#');
                if (hashIndex != -1 && hashIndex + 5 == value.Length)
                {
                    user = users.FirstOrDefault(x => x.Username.Equals(value.Substring(0, value.Length - 5), StringComparison.OrdinalIgnoreCase) && x.Discriminator.Equals(value.Substring(hashIndex + 1), StringComparison.OrdinalIgnoreCase));
                }
            }

            if (user == null)
            {
                IReadOnlyList<DiscordUser> matchingUsers = ctx.Guild != null
                    ? users.Where(x => x.Username.Equals(value, StringComparison.OrdinalIgnoreCase) || ((x is DiscordMember mbr) && mbr.Nickname != null && mbr.Nickname.Equals(value, StringComparison.OrdinalIgnoreCase))).DistinctBy(x => x.Id).ToImmutableArray()
                    : users.Where(x => x.Username.Equals(value, StringComparison.OrdinalIgnoreCase)).DistinctBy(x => x.Id).ToImmutableArray();

                if (matchingUsers.Count > 1)
                {
                    return Task.FromResult(new TypeParserResult<DiscordUser>("Multiple matches found. Mention the user or use their ID."));
                }

                if (matchingUsers.Count == 1)
                {
                    user = matchingUsers[0];
                }
            }

            return user == null
                ? Task.FromResult(new TypeParserResult<DiscordUser>($"The user '{value}' was not found."))
                : Task.FromResult(new TypeParserResult<DiscordUser>(user));
        }
    }
}
