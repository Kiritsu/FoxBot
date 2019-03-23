using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Fox.Commands.Checks;
using Fox.Entities;
using Fox.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Qmmands;

namespace Fox.Commands.Modules
{
    [Name("Developer"), RequireOwner, Hidden]
    public sealed class DeveloperModule : FoxModuleBase
    {
        private readonly ConfigurationService _config;
        private readonly MusicService _music;

        public DeveloperModule(ConfigurationService cfg, MusicService music)
        {
            _config = cfg;
            _music = music;
        }

        [Command("StartLavalink")]
        public Task StartLavalinkAsync()
        {
            return _music.StartLavalinkAsync();
        }

        [Command("God")]
        public Task GodAsync()
        {
            _config.BypassChecks = !_config.BypassChecks;

            return RespondAsync(_config.BypassChecks ? "You will now bypass every check regarding your rights." : "You won't bypass checks anymore.");
        }

        [Command("Eval")]
        public async Task EvalAsync([Remainder] string code)
        {
            var cb1 = code.IndexOf("```", StringComparison.Ordinal) + 3;
            var cb2 = code.LastIndexOf("```", StringComparison.Ordinal);
            code = code.Substring(cb1, cb2 - cb1);

            var embed = new DiscordEmbedBuilder
            {
                Color = ConfigurationService.EmbedColor
            }.WithDescription("Evaluating... Please wait.");

            var msg = await RespondAsync(embed: embed);

            var options = ScriptOptions.Default
                .WithImports("System", "System.Linq", "System.Net.Http", "System.Threading.Tasks", "System.Collections.Generic", "System.Text")
                .WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location)));

            var sw = Stopwatch.StartNew();
            var script = CSharpScript.Create(code, options, typeof(FoxCommandContext));
            var compilation = script.Compile();
            sw.Stop();

            var compileTime = sw.ElapsedMilliseconds;

            if (compilation.Any(err => err.Severity == DiagnosticSeverity.Error))
            {
                embed.WithDescription($"Unable to compile the given code. Compilation ended in {compileTime}ms with {compilation.Length} errors.");

                foreach (var error in compilation.Take(10))
                {
                    var lineSpan = error.Location.GetLineSpan();
                    embed.AddField($"Error `{error.Id}` at line(s) {lineSpan.StartLinePosition.Line} to {lineSpan.EndLinePosition.Line}: {lineSpan.Span.Start}-{lineSpan.Span.End}", error.GetMessage());
                }

                await msg.ModifyAsync(embed: embed.Build());
                return;
            }

            sw.Restart();
            ScriptState<object> response = null;
            Exception ex;

            try
            {
                response = await script.RunAsync(Context);
                ex = response.Exception;
            }
            catch (Exception e)
            {
                ex = e;
            }

            sw.Stop();

            embed.WithFooter($"Compilation: {compileTime}ms | Execution: {sw.ElapsedMilliseconds}ms");

            if (ex is null)
            {
                embed.WithDescription("Execution completed with success!");

                var result = response.ReturnValue;
                embed.AddField("Return type", result?.GetType().Name ?? "None");
                embed.AddField("Return value", result?.ToString() ?? "None");

                await msg.ModifyAsync(embed: embed.Build());
                return;
            }

            embed.WithDescription("Execution ran into a problem!");
            embed.AddField("Exception", ex.GetType().Name);
            embed.AddField("Message", ex.Message);

            await msg.ModifyAsync(embed: embed.Build());
        }
    }
}
