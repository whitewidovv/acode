namespace Acode.Cli;

/// <summary>
/// Routes command-line arguments to registered command handlers.
/// </summary>
/// <remarks>
/// The router maintains a registry of commands indexed by name and aliases.
/// Uses O(1) lookup for command resolution and Levenshtein distance for
/// fuzzy matching to suggest corrections for typos.
/// </remarks>
public sealed class CommandRouter : ICommandRouter
{
    private readonly Dictionary<string, ICommand> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ICommand> _commandList = new();

    /// <inheritdoc/>
    public void RegisterCommand(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_commands.ContainsKey(command.Name))
        {
            throw new InvalidOperationException($"Command '{command.Name}' is already registered.");
        }

        // Register by primary name
        _commands[command.Name] = command;
        _commandList.Add(command);

        // Register aliases
        if (command.Aliases != null)
        {
            foreach (var alias in command.Aliases)
            {
                if (_commands.ContainsKey(alias))
                {
                    throw new InvalidOperationException($"Alias '{alias}' conflicts with existing command or alias.");
                }

                _commands[alias] = command;
            }
        }
    }

    /// <inheritdoc/>
    public async Task<ExitCode> RouteAsync(string[] args, CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(context);

        if (args.Length == 0)
        {
            await WriteErrorAsync(context.Output, "No command specified. Use --help to see available commands.").ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }

        var commandName = args[0];
        var command = GetCommand(commandName);

        if (command == null)
        {
            await WriteUnknownCommandErrorAsync(context.Output, commandName).ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }

        // Create updated context with remaining args (excluding command name)
        var commandArgs = args.Length > 1 ? args[1..] : Array.Empty<string>();
        var commandContext = context with { Args = commandArgs };

        // Execute command
        return await command.ExecuteAsync(commandContext).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public ICommand? GetCommand(string commandName)
    {
        ArgumentNullException.ThrowIfNull(commandName);

        _commands.TryGetValue(commandName, out var command);
        return command;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ICommand> ListCommands()
    {
        return _commandList.AsReadOnly();
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> SuggestCommands(string commandName, int maxSuggestions = 3)
    {
        ArgumentNullException.ThrowIfNull(commandName);

        if (maxSuggestions <= 0)
        {
            return Array.Empty<string>();
        }

        var suggestions = _commandList
            .Select(c => new
            {
                Command = c,
                Distance = LevenshteinDistance(commandName.ToLowerInvariant(), c.Name.ToLowerInvariant()),
            })
            .Where(x => x.Distance <= 3) // Threshold: max 3 edits
            .OrderBy(x => x.Distance)
            .Take(maxSuggestions)
            .Select(x => x.Command.Name)
            .ToList();

        return suggestions;
    }

    /// <summary>
    /// Calculates the Levenshtein distance between two strings.
    /// </summary>
    /// <param name="source">First string.</param>
    /// <param name="target">Second string.</param>
    /// <returns>Number of single-character edits (insertions, deletions, substitutions).</returns>
    /// <remarks>
    /// Used for fuzzy command matching to suggest corrections for typos.
    /// Example: LevenshteinDistance("chatt", "chat") = 1.
    /// </remarks>
    private static int LevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
        {
            return target?.Length ?? 0;
        }

        if (string.IsNullOrEmpty(target))
        {
            return source.Length;
        }

        var distance = new int[source.Length + 1, target.Length + 1];

        // Initialize first column (deletions from source)
        for (int i = 0; i <= source.Length; i++)
        {
            distance[i, 0] = i;
        }

        // Initialize first row (insertions into source)
        for (int j = 0; j <= target.Length; j++)
        {
            distance[0, j] = j;
        }

        // Calculate distances
        for (int i = 1; i <= source.Length; i++)
        {
            for (int j = 1; j <= target.Length; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;

                distance[i, j] = Math.Min(
                    Math.Min(
                        distance[i - 1, j] + 1,      // Deletion
                        distance[i, j - 1] + 1),     // Insertion
                    distance[i - 1, j - 1] + cost);  // Substitution
            }
        }

        return distance[source.Length, target.Length];
    }

    private static async Task WriteErrorAsync(TextWriter output, string message)
    {
        await output.WriteLineAsync($"Error: {message}").ConfigureAwait(false);
    }

    private async Task WriteUnknownCommandErrorAsync(TextWriter output, string commandName)
    {
        await output.WriteLineAsync($"Error: Unknown command '{commandName}'.").ConfigureAwait(false);

        var suggestions = SuggestCommands(commandName);
        if (suggestions.Count > 0)
        {
            await output.WriteLineAsync().ConfigureAwait(false);
            await output.WriteLineAsync("Did you mean:").ConfigureAwait(false);
            foreach (var suggestion in suggestions)
            {
                await output.WriteLineAsync($"  {suggestion}").ConfigureAwait(false);
            }
        }
    }
}
