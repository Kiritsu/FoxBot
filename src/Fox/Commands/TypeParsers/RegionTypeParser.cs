using MingweiSamuel.Camille.Enums;
using Qmmands;
using System;
using System.Threading.Tasks;

namespace Fox.Commands.TypeParsers
{
    public sealed class RegionTypeParser : TypeParser<Region>
    {
        public override Task<TypeParserResult<Region>> ParseAsync(Parameter parameter, string value, ICommandContext context, IServiceProvider provider)
        {
            try
            {
                var region = Region.Get(value);

                return Task.FromResult(new TypeParserResult<Region>(region));
            }
            catch (Exception)
            {
                return Task.FromResult(new TypeParserResult<Region>($"Couldn't find a region named `{value}`."));
            }
        }
    }
}
