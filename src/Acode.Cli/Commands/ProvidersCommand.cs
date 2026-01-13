namespace Acode.Cli.Commands;

using System;
using System.Threading.Tasks;

/// <summary>
/// Command for managing model providers (list, health, test).
/// </summary>
/// <remarks>
/// STUB: Implementation deferred to CLI integration epic.
/// See task-004c Gap #31 - this is a placeholder for future implementation.
///
/// Planned subcommands:
/// - acode providers list: List all registered providers
/// - acode providers health: Check provider health status
/// - acode providers test {id}: Test connection to specific provider
///
/// Will integrate with IProviderRegistry to display provider information.
/// </remarks>
public sealed class ProvidersCommand : ICommand
{
    /// <inheritdoc/>
    public string Name => "providers";

    /// <inheritdoc/>
    public string[]? Aliases => null;

    /// <inheritdoc/>
    public string Description => "Manage model providers (list, health, test)";

    /// <inheritdoc/>
    public Task<ExitCode> ExecuteAsync(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        throw new NotImplementedException(
            "TODO: Implement in CLI integration epic. " +
            "This command should call IProviderRegistry.ListProviders() and format output. " +
            "See docs/configuration/providers.md for expected behavior.");
    }

    /// <inheritdoc/>
    public string GetHelp()
    {
        return @"Usage: acode providers <subcommand>

Manages model provider registry operations.

Subcommands:
  list                  List all registered providers
  health                Check health status of all providers
  test <provider-id>    Test connection to specific provider

Examples:
  acode providers list
  acode providers health
  acode providers test ollama

Note: This command is a stub. Full implementation coming in CLI integration epic.";
    }
}
