using System;
using System.Threading.Tasks;
using Qmmands;

namespace Fox.Commands.TypeParsers
{
    public sealed class UriTypeParser : TypeParser<Uri>
    {
        public override Task<TypeParserResult<Uri>> ParseAsync(Parameter parameter, string value, ICommandContext context, IServiceProvider provider)
        {
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                return Task.FromResult(new TypeParserResult<Uri>(uri));
            }

            return Task.FromResult(new TypeParserResult<Uri>("The given URL was not valid. Try add `http://` if it's not done already."));
        }
    }
}
