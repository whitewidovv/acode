namespace Acode.Application.Cli;

using Acode.Cli;

/// <summary>
/// Defines the contract for CLI commands.
/// </summary>
/// <remarks>
/// All CLI commands implement this interface to provide consistent
/// command execution, help documentation, and metadata.
/// FR-019 through FR-027: Core command definitions.
/// </remarks>
public interface ICommand
{
    /// <summary>
    /// Gets the primary name of the command.
    /// </summary>
    /// <remarks>
    /// Command names must be lowercase alphanumeric (FR-002).
    /// Examples: "run", "config", "models", "chat".
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Gets alternative names (aliases) for the command.
    /// </summary>
    /// <remarks>
    /// Aliases provide shortcuts (e.g., "cfg" for "config").
    /// Can be null or empty if no aliases exist.
    /// </remarks>
    string[]? Aliases { get; }

    /// <summary>
    /// Gets a brief description of what the command does.
    /// </summary>
    /// <remarks>
    /// Description appears in help output and command listings.
    /// Should be one sentence, no period at end.
    /// Example: "Start an agent run with a task description".
    /// </remarks>
    string Description { get; }

    /// <summary>
    /// Executes the command with the provided context.
    /// </summary>
    /// <param name="context">Command execution context with configuration and output.</param>
    /// <returns>Exit code indicating success or failure type.</returns>
    /// <remarks>
    /// Commands should:
    /// - Validate inputs and return InvalidArguments (2) on error.
    /// - Check cancellation token periodically.
    /// - Write output to context.Output.
    /// - Return appropriate exit code (0 = success).
    /// </remarks>
    Task<ExitCode> ExecuteAsync(CommandContext context);

    /// <summary>
    /// Gets the full help text for this command.
    /// </summary>
    /// <returns>Formatted help text including usage, options, and examples.</returns>
    /// <remarks>
    /// FR-028 through FR-035: Help system requirements.
    /// Help should include:
    /// - Command description.
    /// - Usage syntax.
    /// - All options with descriptions.
    /// - Examples.
    /// - Related commands.
    /// </remarks>
    string GetHelp();
}
