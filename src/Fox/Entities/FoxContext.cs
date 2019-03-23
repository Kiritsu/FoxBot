using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Fox.Databases;
using Fox.Services;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Fox.Entities
{
    public class FoxContext : ICommandContext
    {
        public MessageCreateEventArgs MessageCreateEventArgs { get; }

        public IServiceProvider Services { get; }

        public CommandService CommandService { get; }

        public ConfigurationService Configuration { get; }

        public FoxDbContext DatabaseContext { get; }

        public FoxDb Database { get; }

        public DiscordClient Client { get; }

        public DiscordGuild Guild { get; }

        public DiscordChannel Channel { get; }

        public DiscordUser User { get; }

        public DiscordMember Member { get; }

        public DiscordMessage Message { get; }

        public FoxContext(MessageCreateEventArgs args, IServiceProvider services)
        {
            MessageCreateEventArgs = args;

            Services = services;

            Client = args.Client;
            Guild = args.Guild;
            Channel = args.Channel;
            User = args.Author;
            Member = User as DiscordMember;
            Message = args.Message;

            Configuration = services.GetRequiredService<ConfigurationService>();

            CommandService = services.GetRequiredService<CommandService>();
            Database = services.GetRequiredService<FoxDb>();
            DatabaseContext = new FoxDbContext(this);
        }

        public FoxContext(FoxContext context)
        {
            MessageCreateEventArgs = context.MessageCreateEventArgs;

            Services = context.Services;

            Client = context.Client;
            Guild = context.Guild;
            Channel = context.Channel;
            User = context.User;
            Member = context.Member;
            Message = context.Message;

            Configuration = context.Configuration;

            CommandService = context.CommandService;
            Database = context.Database;
            DatabaseContext = context.DatabaseContext;
        }

        public Task<DiscordMessage> RespondAsync(string message = "", bool tts = false, DiscordEmbed embed = default)
        {
            return Channel.SendMessageAsync(message, tts, embed);
        }
    }
}
