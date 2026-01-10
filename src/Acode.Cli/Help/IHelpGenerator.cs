namespace Acode.Cli.Help;

/// <summary>
/// Generates help text for commands.
/// </summary>
public interface IHelpGenerator
{
    /// <summary>
    /// Generates the global help text showing all available commands.
    /// </summary>
    /// <returns>Formatted help text.</returns>
    string GenerateGlobalHelp();

    /// <summary>
    /// Generates detailed help for a specific command.
    /// </summary>
    /// <param name="command">The command to generate help for.</param>
    /// <returns>Formatted help text.</returns>
    string GenerateCommandHelp(ICommand command);

    /// <summary>
    /// Configures the help generator options.
    /// </summary>
    /// <param name="options">Help generation options.</param>
    void Configure(HelpOptions options);
}

/// <summary>
/// Configuration options for help generation.
/// </summary>
/// <param name="TerminalWidth">Width of the terminal in columns.</param>
/// <param name="UseColors">Whether to use ANSI color codes.</param>
/// <param name="UseUnicode">Whether to use Unicode characters.</param>
public sealed record HelpOptions(
    int TerminalWidth = 80,
    bool UseColors = true,
    bool UseUnicode = true
)
{
    /// <summary>
    /// Default options for most terminals.
    /// </summary>
    public static readonly HelpOptions Default = new();

    /// <summary>
    /// Options for plain text output (no colors, ASCII only).
    /// </summary>
    public static readonly HelpOptions Plain = new(UseColors: false, UseUnicode: false);
}
