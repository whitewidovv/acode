using System.Reflection;

namespace Acode.Cli.Commands;

/// <summary>
/// Command that displays version information for Acode.
/// </summary>
/// <remarks>
/// Shows the current version of the Acode CLI application.
/// Version is read from the assembly's informational version attribute.
/// </remarks>
public sealed class VersionCommand : ICommand
{
    /// <inheritdoc/>
    public string Name => "version";

    /// <inheritdoc/>
    public string[] Aliases => new[] { "--version", "-v" };

    /// <inheritdoc/>
    public string Description => "Display version information";

    /// <inheritdoc/>
    public async Task<ExitCode> ExecuteAsync(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var version = GetVersion();
        context.Formatter.WriteMessage($"Acode version {version}");

        await Task.CompletedTask.ConfigureAwait(false);
        return ExitCode.Success;
    }

    /// <inheritdoc/>
    public string GetHelp()
    {
        return @"Usage: acode version

Displays the current version of the Acode CLI application.

Aliases:
  version, --version, -v

Examples:
  acode version
  acode --version
  acode -v";
    }

    private static string GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "0.0.0";

        return version;
    }
}
