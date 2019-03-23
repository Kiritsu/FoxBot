using System;
using DSharpPlus.EventArgs;
using Qmmands;

namespace Fox.Entities
{
    public sealed class FoxCommandContext : FoxContext
    {
        public string Prefix { get; }

        public Command Command { get; set; }

        public FoxCommandContext(MessageCreateEventArgs args, string prefix, IServiceProvider services) : base(args, services)
        {
            Prefix = prefix;
        }

        public FoxCommandContext(FoxContext baseContext, string prefix) : base(baseContext)
        {
            Prefix = prefix;
        }
    }
}
