namespace Acode.Cli;

/// <summary>
/// Defines the contract for routing command-line input to command handlers.
/// </summary>
/// <remarks>
/// The command router is responsible for:
/// - Parsing raw command-line arguments.
/// - Finding the matching command by name or alias.
/// - Suggesting corrections for typos (fuzzy matching).
/// - Delegating execution to the selected command.
/// </remarks>
public interface ICommandRouter
{
    /// <summary>
    /// Registers a command with the router.
    /// </summary>
    /// <param name="command">The command to register.</param>
    /// <remarks>
    /// Commands must have unique names. Registering a duplicate name
    /// will throw InvalidOperationException.
    /// </remarks>
    void RegisterCommand(ICommand command);

    /// <summary>
    /// Routes the command-line arguments to the appropriate command and executes it.
    /// </summary>
    /// <param name="args">Command-line arguments (excluding program name).</param>
    /// <param name="context">Execution context for the command.</param>
    /// <returns>Exit code from command execution.</returns>
    /// <remarks>
    /// Routing logic:
    /// 1. Parse args to extract command name.
    /// 2. Look up command by name or alias (O(1) lookup).
    /// 3. If not found, suggest similar commands using Levenshtein distance.
    /// 4. Validate command-specific arguments.
    /// 5. Execute command with context.
    /// 6. Return exit code.
    ///
    /// FR-008: Unknown options MUST error with suggestion.
    /// FR-038: Invalid arguments return exit code 2.
    /// </remarks>
    Task<ExitCode> RouteAsync(string[] args, CommandContext context);

    /// <summary>
    /// Gets a registered command by name or alias.
    /// </summary>
    /// <param name="commandName">Command name or alias to look up.</param>
    /// <returns>The matching command, or null if not found.</returns>
    ICommand? GetCommand(string commandName);

    /// <summary>
    /// Lists all registered commands.
    /// </summary>
    /// <returns>Read-only list of all registered commands.</returns>
    IReadOnlyList<ICommand> ListCommands();

    /// <summary>
    /// Suggests commands similar to the given name (typo correction).
    /// </summary>
    /// <param name="commandName">The potentially misspelled command name.</param>
    /// <param name="maxSuggestions">Maximum number of suggestions to return.</param>
    /// <returns>List of suggested command names, ordered by similarity.</returns>
    /// <remarks>
    /// Uses Levenshtein distance algorithm to find similar command names.
    /// Example: "chatt" → suggests "chat".
    /// Returns empty list if no similar commands found (distance > threshold).
    /// </remarks>
    IReadOnlyList<string> SuggestCommands(string commandName, int maxSuggestions = 3);
}
