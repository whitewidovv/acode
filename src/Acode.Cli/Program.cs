using Acode.Application.Configuration;
using Acode.Application.DependencyInjection;
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

        // Handle version/help
        if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
        {
            ShowHelp();
            return 0;
        }

        if (args[0] == "--version" || args[0] == "-v")
        {
            ShowVersion();
            return 0;
        }

        // Setup DI
        var services = new ServiceCollection();
        services.AddAcodeApplication();
        services.AddAcodeInfrastructure();
        var serviceProvider = services.BuildServiceProvider();

        // Route commands
        if (args[0] == "config" && args.Length >= 2)
        {
            var loader = serviceProvider.GetRequiredService<IConfigLoader>();
            var validator = serviceProvider.GetRequiredService<IConfigValidator>();
            var configCommand = new ConfigCommand(loader, validator);

            var configPath = ".agent/config.yml";

            if (args[1] == "validate")
            {
                return configCommand.ValidateAsync(configPath).GetAwaiter().GetResult();
            }

            if (args[1] == "show")
            {
                var format = "yaml";
                if (args.Length >= 3 && args[2] == "--format" && args.Length >= 4)
                {
                    format = args[3];
                }

                return configCommand.ShowAsync(configPath, format).GetAwaiter().GetResult();
            }
        }

        Console.WriteLine($"Unknown command: {args[0]}");
        Console.WriteLine("Run 'acode --help' for usage information.");
        return 1;
    }

    private static void ShowVersion()
    {
        Console.WriteLine("Acode - Agentic Coding Bot");
        Console.WriteLine("Version: 0.1.0-alpha");
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Acode - Agentic Coding Bot");
        Console.WriteLine();
        Console.WriteLine("Usage: acode <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  config validate              Validate configuration file");
        Console.WriteLine("  config show                  Show configuration");
        Console.WriteLine("  config show --format json    Show configuration as JSON");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -h, --help                   Show help");
        Console.WriteLine("  -v, --version                Show version");
    }
}
