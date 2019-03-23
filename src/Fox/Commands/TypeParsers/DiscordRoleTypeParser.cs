using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Fox.Entities;
using Qmmands;

namespace Fox.Commands.TypeParsers
{
    public sealed class DiscordRoleTypeParser : TypeParser<DiscordRole>
    {
        public override Task<TypeParserResult<DiscordRole>> ParseAsync(Parameter parameter, string value, ICommandContext context, IServiceProvider provider)
        {
            if (!(context is FoxCommandContext ctx))
            {
                return Task.FromResult(new TypeParserResult<DiscordRole>("A role cannot exist in that context."));
            }

            if (ctx.Guild == null)
            {
                return Task.FromResult(new TypeParserResult<DiscordRole>("This command must be used in a guild."));
            }

            DiscordRole role = null;
            if ((value.Length > 3 && value[0] == '<' && value[1] == '@' && value[2] == '&' && value[value.Length - 1] == '>' && ulong.TryParse(value.Substring(3, value.Length - 4), out var id))
                || ulong.TryParse(value, out id))
            {
                role = ctx.Guild.Roles.FirstOrDefault(x => x.Id == id);
            }

            if (role == null)
            {
                role = ctx.Guild.Roles.FirstOrDefault(x => x.Name == value);
            }

            return role == null
                ? Task.FromResult(new TypeParserResult<DiscordRole>("No role found matching the input."))
                : Task.FromResult(new TypeParserResult<DiscordRole>(role));
        }
    }
}
