using Acode.Application.Configuration;
using Acode.Application.DependencyInjection;
using Acode.Application.PromptPacks;
using Acode.Cli.Commands;
using Acode.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Acode.Cli;

/// <summary>
/// Entry point for the Acode CLI application.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code (0 for success).</returns>
    public static int Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        // Setup DI
        var services = new ServiceCollection();
        services.AddAcodeApplication();
        services.AddAcodeInfrastructure();
        var serviceProvider = services.BuildServiceProvider();

        // Initialize command router
        var router = new CommandRouter();

        // Register commands
        router.RegisterCommand(new HelpCommand(router));
        router.RegisterCommand(new VersionCommand());

        // Register config command
        var loader = serviceProvider.GetRequiredService<IConfigLoader>();
        var validator = serviceProvider.GetRequiredService<IConfigValidator>();
        router.RegisterCommand(new ConfigCommand(loader, validator));

        // Register prompts command
        var packRegistry = serviceProvider.GetRequiredService<IPromptPackRegistry>();
        var packLoader = serviceProvider.GetRequiredService<IPromptPackLoader>();
        var packValidator = serviceProvider.GetRequiredService<IPackValidator>();
        router.RegisterCommand(new PromptsCommand(packRegistry, packLoader, packValidator));

        // Parse global flags
        // FR-001: --json flag MUST enable JSONL mode
        // FR-002: ACODE_JSON=1 env var MUST enable JSONL mode
        var useJson = args.Contains("--json") ||
                      string.Equals(Environment.GetEnvironmentVariable("ACODE_JSON"), "1", StringComparison.Ordinal);
        var noColor = args.Contains("--no-color");

        // Remove global flags from args
        args = args.Where(a => a != "--json" && a != "--no-color").ToArray();

        // If no arguments, show help
        if (args.Length == 0)
        {
            args = new[] { "help" };
        }

        // Select formatter based on flags and TTY detection
        IOutputFormatter formatter;
        if (useJson)
        {
            formatter = new JsonLinesFormatter(Console.Out);
        }
        else
        {
            var enableColors = !noColor && Console.IsOutputRedirected == false;
            formatter = new ConsoleFormatter(Console.Out, enableColors);
        }

        // Create command context
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = Array.Empty<string>(), // Will be populated by router
            Formatter = formatter,
            Output = Console.Out,
            CancellationToken = CancellationToken.None,
        };

        // Route and execute command
        var exitCode = router.RouteAsync(args, context).GetAwaiter().GetResult();

        return (int)exitCode;
    }
}
