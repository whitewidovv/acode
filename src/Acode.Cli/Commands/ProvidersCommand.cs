namespace Acode.Cli.Commands;

using System;
using System.Linq;
using System.Threading.Tasks;
using Acode.Infrastructure.Ollama.SmokeTest;
using Acode.Infrastructure.Ollama.SmokeTest.Output;

/// <summary>
/// Command for managing model providers (smoke tests, health checks).
/// </summary>
/// <remarks>
/// Task 005c: Implements smoke-test subcommand for Ollama provider.
/// </remarks>
public sealed class ProvidersCommand : ICommand
{
    /// <inheritdoc/>
    public string Name => "providers";

    /// <inheritdoc/>
    public string[]? Aliases => null;

    /// <inheritdoc/>
    public string Description => "Run smoke tests and health checks for model providers";

    /// <inheritdoc/>
    public async Task<ExitCode> ExecuteAsync(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Require at least one argument (subcommand)
        if (context.Args.Length == 0)
        {
            await context.Output.WriteLineAsync(this.GetHelp()).ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }

        var subcommand = context.Args[0].ToLowerInvariant();

        return subcommand switch
        {
            "smoke-test" => await this.ExecuteSmokeTestAsync(context).ConfigureAwait(false),
            _ => await this.HandleUnknownSubcommandAsync(context, subcommand).ConfigureAwait(false)
        };
    }

    /// <inheritdoc/>
    public string GetHelp()
    {
        return @"Usage: acode providers smoke-test <provider> [options]

Runs smoke tests for model providers to verify connectivity and basic functionality.

Arguments:
  <provider>              Provider to test (currently supported: ollama)

Options:
  --endpoint <url>        Provider endpoint URL (default: http://localhost:11434)
  --model <name>          Model name to test (default: llama3.2:latest)
  --timeout <seconds>     Timeout in seconds (default: 60)
  --skip-tool-test        Skip tool calling test
  --verbose               Show detailed test output

Examples:
  acode providers smoke-test ollama
  acode providers smoke-test ollama --verbose
  acode providers smoke-test ollama --model llama3.2:latest --timeout 30
  acode providers smoke-test ollama --endpoint http://custom:11434 --skip-tool-test

Exit Codes:
  0   All tests passed
  4   One or more tests failed (runtime error)";
    }

    private async Task<ExitCode> ExecuteSmokeTestAsync(CommandContext context)
    {
        // Require provider argument
        if (context.Args.Length < 2)
        {
            await context.Output.WriteLineAsync("Error: Provider argument required.").ConfigureAwait(false);
            await context.Output.WriteLineAsync(this.GetHelp()).ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }

        var provider = context.Args[1].ToLowerInvariant();

        // Only Ollama is supported for now
        if (provider != "ollama")
        {
            await context.Output.WriteLineAsync($"Error: Only 'ollama' provider is supported in this release.").ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }

        // Parse options
        var options = this.ParseSmokeTestOptions(context.Args.Skip(2).ToArray());
        var verbose = this.HasFlag(context.Args, "--verbose");

        // Run smoke tests
        var runner = new OllamaSmokeTestRunner();

        if (verbose)
        {
            await context.Output.WriteLineAsync($"Running Ollama Smoke Tests...").ConfigureAwait(false);
            await context.Output.WriteLineAsync($"  Endpoint: {options.Endpoint}").ConfigureAwait(false);
            await context.Output.WriteLineAsync($"  Model: {options.Model}").ConfigureAwait(false);
            await context.Output.WriteLineAsync($"  Timeout: {options.Timeout.TotalSeconds}s").ConfigureAwait(false);
            await context.Output.WriteLineAsync($"  Skip Tool Test: {options.SkipToolTest}").ConfigureAwait(false);
            await context.Output.WriteLineAsync().ConfigureAwait(false);
        }

        var results = await runner.RunAsync(options, context.CancellationToken).ConfigureAwait(false);

        // Format output
        var reporter = new TextTestReporter(verbose);
        reporter.Report(results, context.Output);

        // Return exit code based on test results
        return results.AllPassed ? ExitCode.Success : ExitCode.RuntimeError;
    }

    private SmokeTestOptions ParseSmokeTestOptions(string[] args)
    {
        var endpoint = this.GetFlagValue(args, "--endpoint") ?? "http://localhost:11434";
        var model = this.GetFlagValue(args, "--model") ?? "llama3.2:latest";
        var timeoutStr = this.GetFlagValue(args, "--timeout") ?? "60";
        var skipToolTest = this.HasFlag(args, "--skip-tool-test");

        if (!int.TryParse(timeoutStr, out var timeoutSeconds) || timeoutSeconds <= 0)
        {
            timeoutSeconds = 60;
        }

        return new SmokeTestOptions
        {
            Endpoint = endpoint,
            Model = model,
            Timeout = TimeSpan.FromSeconds(timeoutSeconds),
            SkipToolTest = skipToolTest
        };
    }

    private string? GetFlagValue(string[] args, string flag)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(flag, StringComparison.OrdinalIgnoreCase))
            {
                var value = args[i + 1];

                // Validate that the value is not itself a flag (starts with "--")
                // This prevents misinterpretation when user omits a value, e.g., "--model --timeout 30"
                if (value.StartsWith("--", StringComparison.Ordinal))
                {
                    return null;
                }

                return value;
            }
        }

        return null;
    }

    private bool HasFlag(string[] args, string flag)
    {
        return args.Any(arg => arg.Equals(flag, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<ExitCode> HandleUnknownSubcommandAsync(CommandContext context, string subcommand)
    {
        await context.Output.WriteLineAsync($"Error: Unknown subcommand '{subcommand}'.").ConfigureAwait(false);
        await context.Output.WriteLineAsync().ConfigureAwait(false);
        await context.Output.WriteLineAsync(this.GetHelp()).ConfigureAwait(false);
        return ExitCode.InvalidArguments;
    }
}
