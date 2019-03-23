using System;
using System.Threading.Tasks;
using Fox.Entities;
using Qmmands;

namespace Fox.Commands.TypeParsers
{
    public sealed class SkeletonUserTypeParser : TypeParser<SkeletonUser>
    {
        public override async Task<TypeParserResult<SkeletonUser>> ParseAsync(Parameter parameter, string value, ICommandContext context, IServiceProvider provider)
        {
            if (!(context is FoxCommandContext ctx))
            {
                return new TypeParserResult<SkeletonUser>("An user (skeleton) cannot exist in that context.");
            }

            if (!ulong.TryParse(value, out var id))
            {
                var userTypeParser = new DiscordUserTypeParser();
                var result = await userTypeParser.ParseAsync(parameter, value, context, provider);
                if (result.IsSuccessful)
                {
                    return new TypeParserResult<SkeletonUser>(new SkeletonUser(result.Value));
                }

                var memberTypeParser = new DiscordMemberTypeParser();
                var memberResult = await memberTypeParser.ParseAsync(parameter, value, context, provider);
                if (memberResult.IsSuccessful)
                {
                    return new TypeParserResult<SkeletonUser>(new SkeletonUser(memberResult.Value));
                }
            }

            try
            {
                var user = await ctx.Client.GetUserAsync(id);
                return new TypeParserResult<SkeletonUser>(new SkeletonUser(user));
            }
            catch
            {
                return new TypeParserResult<SkeletonUser>("The given ID was invalid and the user couldn't be found.");
            }
        }
    }
}
