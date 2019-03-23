using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Fox.Commands.Checks;
using Fox.Commands.TypeParsers;
using Fox.Entities;
using Fox.Exceptions;
using Fox.Extensions;
using Qmmands;
using Module = Qmmands.Module;

namespace Fox.Services
{
    public sealed class CommandHandlerService
    {
        private readonly Regex _reminderRegex = new Regex(".*?remind (?<target>.+?) in (?<days>[0-9]+ (day))?s? ?(?<hours>[0-9]+ (hour|hr))?s? ?(?<minutes>[0-9]+ (minute|min))?s? ?(?<seconds>[0-9]+ (seconds|secs))?.*?(?<prep>[^ ]+) (?<reason>.+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private readonly IServiceProvider _services;
        private readonly DiscordClient _client;
        private readonly CommandService _commands;
        private readonly LogService _logger;

        public CommandHandlerService(DiscordClient client, CommandService commands, LogService logger, IServiceProvider services)
        {
            _services = services;
            _client = client;
            _commands = commands;
            _logger = logger;
        }

        public void Initialize()
        {
            _commands.AddModules(Assembly.GetEntryAssembly());

            _client.MessageCreated += OnMessageReceivedAsync;

            _commands.CommandExecuted += OnCommandExecuted;
            _commands.CommandErrored += OnCommandErrored;

            _commands.AddTypeParser(new DiscordMemberTypeParser());
            _commands.AddTypeParser(new DiscordGuildTypeParser());
            _commands.AddTypeParser(new DiscordUserTypeParser());
            _commands.AddTypeParser(new DiscordChannelTypeParser());
            _commands.AddTypeParser(new DiscordRoleTypeParser());
            _commands.AddTypeParser(new SkeletonUserTypeParser());
            _commands.AddTypeParser(new UriTypeParser());
            _commands.AddTypeParser(new TimeSpanTypeParser());
            _commands.AddTypeParser(new RegionTypeParser());
        }

        private async Task ExecuteReminderAsync(MessageCreateEventArgs e)
        {
            if (!e.Message.MentionedUsers.Contains(e.Client.CurrentUser) || e.Author == e.Client.CurrentUser)
            {
                return;
            }

            var result = _reminderRegex.Match(e.Message.Content);
            if (!result.Success)
            {
                return;
            }

            var ts = result.Groups.ToTimeSpan();
            if (ts == TimeSpan.Zero)
            {
                return;
            }

            var reason = result.Groups["reason"].ToString();
            var target = result.Groups["target"].ToString().Equals("me", StringComparison.OrdinalIgnoreCase) ? e.Author.Mention : result.Groups["target"].ToString();

            await e.Channel.SendMessageAsync($"OK. I have set your reminder. See you in {ts.Humanize()}.");
            await Task.Delay(ts);
            await e.Channel.SendMessageAsync($"Hey {target} ({ts.Humanize()} ago): {reason}");
        }

        private Task ExecuteCustomCommandAsync(FoxContext ctxBase)
        {
            var cc = ctxBase.DatabaseContext.Guild.CustomCommands.FirstOrDefault(x => x.Name.Equals(ctxBase.Message.Content, StringComparison.OrdinalIgnoreCase));

            if (cc is null)
            {
                return Task.CompletedTask;
            }

            cc.TimeUsed++;

            ctxBase.DatabaseContext.UpdateGuild();

            return ctxBase.RespondAsync(cc.Response.Replace("@everyone", "everyone").Replace("@here", "here"));
        }

        private async Task OnMessageReceivedAsync(MessageCreateEventArgs e)
        {
            if (e.Author.IsBot)
            {
                return;
            }

            try
            {
                var ctxBase = new FoxContext(e, _services);

                _ = Task.Run(() => ExecuteReminderAsync(e));
                _ = Task.Run(() => ExecuteCustomCommandAsync(ctxBase));

                if (ctxBase.DatabaseContext.User.IsBlacklisted)
                {
                    return;
                }

                var prefixes = ctxBase.DatabaseContext.Guild.Prefixes;

                if (ctxBase.Message.MentionedUsers.Contains(ctxBase.Client.CurrentUser) && ctxBase.Message.Content.Contains("prefix", StringComparison.OrdinalIgnoreCase))
                {
                    await ctxBase.RespondAsync($"Prefixes for this guild: {string.Join(", ", prefixes.Select(x => $"`{x}`"))}");
                }

                if (!CommandUtilities.HasAnyPrefix(e.Message.Content, prefixes, StringComparison.OrdinalIgnoreCase,
                    out var prefix, out var content))
                {
                    return;
                }

                var context = new FoxCommandContext(ctxBase, prefix);
                var result = await _commands.ExecuteAsync(content, context, _services);

                if (result.IsSuccessful)
                {
                    return;
                }

                await HandleCommandErroredAsync(result, context);
            }
            catch (Exception ex)
            {
                _logger.Print(LogLevel.Critical, "Handler", ex.StackTrace);
            }
        }

        private Task OnCommandExecuted(Command command, CommandResult result, ICommandContext context, IServiceProvider services)
        {
            if (context is FoxCommandContext ctx)
            {
                var str = new StringBuilder($"{ctx.User.Id} - {ctx.Guild.Id} ::> Command executed: {command}");

                if (result is FoxResult res && !string.IsNullOrWhiteSpace(res.Message))
                {
                    str.Append($" - [{res.Message}]");
                }

                _logger.Print(LogLevel.Info, "Fox", str.ToString());
            }

            return Task.CompletedTask;
        }

        private async Task OnCommandErrored(ExecutionFailedResult result, ICommandContext context, IServiceProvider services)
        {
            if (!(context is FoxCommandContext ctx))
            {
                return;
            }

            var str = new StringBuilder();
            switch (result.Exception)
            {
                case FoxException ex:
                    str.AppendLine(ex.Message);
                    break;
                case UnauthorizedException _:
                    str.AppendLine("I don't have enough power to perform this action. (please check the hierarchy of the bot is correct)");
                    break;
                case BadRequestException _:
                    str.AppendLine("The requested action has been stopped by Discord.");
                    break;
                case ArgumentException ex:
                    str.AppendLine($"{ex.Message}\n");
                    str.AppendLine($"You sure you didn't fail the command? Please do `{ctx.Prefix}help {result.Command.FullAliases[0]}`");
                    break;
                default:
                    _logger.Print(LogLevel.Error, "Fox", $"{result.Exception.GetType()} occured. Error message: {result.Exception.Message} Stack trace:\n{result.Exception.StackTrace}");
                    break;
            }

            if (str.Length == 0)
            {
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = ConfigurationService.EmbedColor,
                Title = "Something went wrong!"
            };

            embed.AddField(Formatter.Underline("Command"), result.Command.Name, true);
            embed.AddField(Formatter.Underline("Author"), ctx.User.FormatUser(), true);
            embed.AddField(Formatter.Underline("Error(s)"), str.ToString());
            embed.WithFooter($"Type '{ctx.Prefix}help {ctx.Command.FullAliases[0].ToLowerInvariant()}' for more information.");

            await ctx.RespondAsync("", false, embed);
        }

        private async Task HandleCommandErroredAsync(IResult result, FoxCommandContext ctx)
        {
            if (result is CommandNotFoundResult)
            {
                string cmdName;
                var toLev = "";
                var index = 0;
                var split = ctx.Message.Content.Substring(ctx.Prefix.Length).Split(_commands.Separator, StringSplitOptions.RemoveEmptyEntries);

                do
                {
                    toLev += (index == 0 ? "" : _commands.Separator) + split[index];

                    cmdName = toLev.Levenshtein(_commands);
                    index++;
                } while (string.IsNullOrWhiteSpace(cmdName) && index < split.Length);

                if (string.IsNullOrWhiteSpace(cmdName))
                {
                    return;
                }

                string cmdParams = null;
                while (index < split.Length)
                {
                    cmdParams += " " + split[index++];
                }

                var tryResult = await _commands.ExecuteAsync(cmdName + cmdParams, ctx, _services);

                if (tryResult.IsSuccessful)
                {
                    return;
                }

                await HandleCommandErroredAsync(tryResult, ctx);
            }

            var str = new StringBuilder();

            Command command = null;
            Module module = null;
            switch (result)
            {
                case ChecksFailedResult err:
                    command = err.Command;
                    module = err.Module;
                    str.AppendLine("The following check(s) failed:");
                    foreach ((var check, var error) in err.FailedChecks)
                    {
                        str.AppendLine($"[`{((FoxCheckBaseAttribute)check).Name}`]: `{error}`");
                    }
                    break;
                case TypeParseFailedResult err:
                    command = err.Parameter.Command;
                    str.AppendLine(err.Reason);
                    break;
                case ArgumentParseFailedResult err:
                    command = err.Command;
                    str.AppendLine($"The syntax of the command `{command.FullAliases[0]}` was wrong.");
                    break;
                case OverloadsFailedResult err:
                    command = err.FailedOverloads.First().Key;
                    str.AppendLine($"I can't find any valid overload for the command `{command.Name}`.");
                    foreach (var overload in err.FailedOverloads)
                    {
                        str.AppendLine($" -> `{overload.Value.Reason}`");
                    }
                    break;
                case ParameterChecksFailedResult err:
                    command = err.Parameter.Command;
                    module = err.Parameter.Command.Module;
                    str.AppendLine("The following parameter check(s) failed:");
                    foreach ((var check, var error) in err.FailedChecks)
                    {
                        str.AppendLine($"[`{check.Parameter.Name}`]: `{error}`");
                    }
                    break;
                case ExecutionFailedResult _: //this should be handled in the CommandErrored event or in the FoxResult case.
                case CommandNotFoundResult _: //this is handled at the beginning of this method with levenshtein thing.
                    break;
                case FoxResult err:
                    command = err.Command; //ctx.Command is not null because a FoxResult is returned in execution step.
                    str.AppendLine(err.Message);
                    break;
                case CommandOnCooldownResult err:
                    command = err.Command;
                    var remainingTime = err.Cooldowns.OrderByDescending(x => x.RetryAfter).FirstOrDefault();
                    str.AppendLine($"You're being rate limited! Please retry after {remainingTime.RetryAfter.Humanize()}.");
                    break;
                default:
                    str.AppendLine($"Unknown error: {result}");
                    break;
            }

            if (str.Length == 0)
            {
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = ConfigurationService.EmbedColor,
                Title = "Something went wrong!"
            };

            embed.WithFooter($"Type '{ctx.Prefix}help {command?.FullAliases[0] ?? ctx.Command?.FullAliases[0] ?? ""}' for more information.");

            embed.AddField(Formatter.Underline("Command/Module"), command?.Name ?? ctx.Command?.Name ?? module?.Name ?? "Unknown command...", true);
            embed.AddField(Formatter.Underline("Author"), ctx.User.FormatUser(), true);
            embed.AddField(Formatter.Underline("Error(s)"), str.ToString());

            _logger.Print(LogLevel.Error, "Fox", $"{ctx.User.Id} - {ctx.Guild.Id} ::> Command errored: {command?.Name ?? "-unknown command-"}");
            await ctx.RespondAsync("", false, embed.Build());
        }
    }
}
