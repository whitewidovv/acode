namespace Acode.Cli.Commands;

/// <summary>
/// Command that displays help information for all available commands.
/// </summary>
/// <remarks>
/// Lists all registered commands with their descriptions.
/// Users can invoke with: acode help, acode --help, acode -h, or acode ?.
/// </remarks>
public sealed class HelpCommand : ICommand
{
    private readonly ICommandRouter _router;

    /// <summary>
    /// Initializes a new instance of the <see cref="HelpCommand"/> class.
    /// </summary>
    /// <param name="router">Command router to query for registered commands.</param>
    public HelpCommand(ICommandRouter router)
    {
        ArgumentNullException.ThrowIfNull(router);
        _router = router;
    }

    /// <inheritdoc/>
    public string Name => "help";

    /// <inheritdoc/>
    public string[] Aliases => new[] { "--help", "-h", "?" };

    /// <inheritdoc/>
    public string Description => "Display help information for available commands";

    /// <inheritdoc/>
    public async Task<ExitCode> ExecuteAsync(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Formatter.WriteHeading("Acode - Agentic Coding Bot");

        context.Formatter.WriteMessage("Usage: acode <command> [options]");
        context.Formatter.WriteBlankLine();
        context.Formatter.WriteMessage("Available commands:");
        context.Formatter.WriteBlankLine();

        var commands = _router.ListCommands();

        // Build table data
        var rows = commands
            .Select(c => new[] { c.Name, c.Description })
            .ToList();

        if (rows.Count > 0)
        {
            context.Formatter.WriteTable(new[] { "Command", "Description" }, rows);
        }
        else
        {
            context.Formatter.WriteMessage("No commands registered.", MessageType.Warning);
        }

        context.Formatter.WriteBlankLine();
        context.Formatter.WriteMessage("Use 'acode <command> --help' for detailed help on a specific command.");

        await Task.CompletedTask.ConfigureAwait(false);
        return ExitCode.Success;
    }

    /// <inheritdoc/>
    public string GetHelp()
    {
        return @"Usage: acode help

Displays a list of all available commands with brief descriptions.

Aliases:
  help, --help, -h, ?

Examples:
  acode help
  acode --help
  acode -h";
    }
}
