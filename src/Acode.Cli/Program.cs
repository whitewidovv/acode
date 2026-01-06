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

        // Setup DI
        var services = new ServiceCollection();
        services.AddAcodeApplication();
        services.AddAcodeInfrastructure();
        var serviceProvider = services.BuildServiceProvider();

        // Initialize command router
        var router = new CommandRouter();

        // Register commands that implement ICommand
        router.RegisterCommand(new HelpCommand(router));
        router.RegisterCommand(new VersionCommand());

        // Handle config command (legacy implementation from Task 002)
        if (args.Length >= 1 && args[0] == "config")
        {
            var loader = serviceProvider.GetRequiredService<IConfigLoader>();
            var validator = serviceProvider.GetRequiredService<IConfigValidator>();
            var configCommand = new ConfigCommand(loader, validator);
            var repositoryRoot = Directory.GetCurrentDirectory();

            if (args.Length >= 2 && args[1] == "validate")
            {
                return configCommand.ValidateAsync(repositoryRoot).GetAwaiter().GetResult();
            }

            if (args.Length >= 2 && args[1] == "show")
            {
                var format = "yaml";
                if (args.Length >= 4 && args[2] == "--format")
                {
                    format = args[3];
                }

                return configCommand.ShowAsync(repositoryRoot, format).GetAwaiter().GetResult();
            }

            Console.WriteLine("Unknown config subcommand. Use 'acode config validate' or 'acode config show'.");
            return (int)ExitCode.InvalidArguments;
        }

        // If no arguments, show help
        if (args.Length == 0)
        {
            args = new[] { "help" };
        }

        // Create command context
        var context = new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = Array.Empty<string>(), // Will be populated by router
            Output = Console.Out,
            CancellationToken = CancellationToken.None,
        };

        // Route and execute command
        var exitCode = router.RouteAsync(args, context).GetAwaiter().GetResult();

        return (int)exitCode;
    }
}
