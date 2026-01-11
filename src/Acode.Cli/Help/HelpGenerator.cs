namespace Acode.Cli.Help;

using System.Text;
using Acode.Cli.Commands;

/// <summary>
/// Generates formatted help text for CLI commands.
/// </summary>
/// <remarks>
/// Implements the help template format from Task 010a specification.
/// Generates NAME, DESCRIPTION, USAGE, OPTIONS, EXAMPLES, and SEE ALSO sections.
/// </remarks>
public sealed class HelpGenerator : IHelpGenerator
{
    private readonly ICommandRouter _router;
    private HelpOptions _options = HelpOptions.Default;

    /// <summary>
    /// Initializes a new instance of the <see cref="HelpGenerator"/> class.
    /// </summary>
    /// <param name="router">Command router for accessing registered commands.</param>
    public HelpGenerator(ICommandRouter router)
    {
        _router = router ?? throw new ArgumentNullException(nameof(router));
    }

    /// <inheritdoc/>
    public void Configure(HelpOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public string GenerateGlobalHelp()
    {
        var sb = new StringBuilder();

        sb.AppendLine();
        sb.AppendLine(FormatHeader("Acode - Agentic Coding Bot"));
        sb.AppendLine(new string('=', Math.Min(26, _options.TerminalWidth)));
        sb.AppendLine();

        sb.AppendLine("Usage: acode <command> [options]");
        sb.AppendLine();

        sb.AppendLine("Available commands:");
        sb.AppendLine();

        var commands = _router
            .ListCommands()
            .Where(c => IsCommandVisible(c))
            .OrderBy(c => c.Name)
            .ToList();

        var maxNameLength = commands.Count > 0 ? commands.Max(c => c.Name.Length) : 10;

        sb.AppendLine($"{"Command".PadRight(maxNameLength + 2)}Description");
        sb.AppendLine(new string('-', Math.Min(60, _options.TerminalWidth)));

        foreach (var command in commands)
        {
            var name = command.Name.PadRight(maxNameLength + 2);
            var description = TruncateToWidth(
                command.Description,
                _options.TerminalWidth - maxNameLength - 4
            );
            sb.AppendLine($"{name}{description}");
        }

        sb.AppendLine();
        sb.AppendLine("Use 'acode <command> --help' for detailed help on a specific command.");

        return sb.ToString();
    }

    /// <inheritdoc/>
    public string GenerateCommandHelp(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sb = new StringBuilder();
        var metadata = TryGetMetadata(command);

        sb.AppendLine();
        sb.AppendLine(FormatSectionHeader("NAME"));
        sb.AppendLine($"  acode {command.Name} - {command.Description}");
        sb.AppendLine();

        sb.AppendLine(FormatSectionHeader("DESCRIPTION"));
        sb.AppendLine($"  {WrapText(command.Description, _options.TerminalWidth - 4, 2)}");
        sb.AppendLine();

        sb.AppendLine(FormatSectionHeader("USAGE"));
        if (metadata != null)
        {
            sb.AppendLine($"  {metadata.Usage}");
        }
        else
        {
            sb.AppendLine($"  acode {command.Name} [options]");
        }

        sb.AppendLine();

        if (metadata?.Options.Count > 0)
        {
            sb.AppendLine(FormatSectionHeader("OPTIONS"));
            foreach (var option in metadata.Options)
            {
                var optionStr = option.GetFormattedOption();
                sb.AppendLine($"  {optionStr}");
                sb.AppendLine($"      {option.Description}");
                if (option.DefaultValue != null)
                {
                    sb.AppendLine($"      Default: {option.DefaultValue}");
                }
            }

            sb.AppendLine();
        }

        if (metadata?.Examples.Count > 0)
        {
            sb.AppendLine(FormatSectionHeader("EXAMPLES"));
            foreach (var example in metadata.Examples)
            {
                sb.AppendLine($"  $ {example.CommandLine}");
                sb.AppendLine($"    {example.Description}");
                sb.AppendLine();
            }
        }

        if (metadata?.RelatedCommands.Count > 0)
        {
            sb.AppendLine(FormatSectionHeader("SEE ALSO"));
            sb.AppendLine(
                $"  {string.Join(", ", metadata.RelatedCommands.Select(c => $"acode {c}"))}"
            );
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string TruncateToWidth(string text, int maxWidth)
    {
        if (maxWidth <= 3)
        {
            return "...";
        }

        if (text.Length <= maxWidth)
        {
            return text;
        }

        return text[..(maxWidth - 3)] + "...";
    }

    private static string WrapText(string text, int maxWidth, int indent)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        if (maxWidth <= 0)
        {
            maxWidth = 80;
        }

        var words = text.Split(' ');
        var lines = new List<string>();
        var currentLine = new StringBuilder();
        var indentStr = new string(' ', indent);

        foreach (var word in words)
        {
            if (currentLine.Length + word.Length + 1 > maxWidth && currentLine.Length > 0)
            {
                lines.Add(currentLine.ToString());
                currentLine.Clear();
            }

            if (currentLine.Length > 0)
            {
                currentLine.Append(' ');
            }

            currentLine.Append(word);
        }

        if (currentLine.Length > 0)
        {
            lines.Add(currentLine.ToString());
        }

        return string.Join($"\n{indentStr}", lines);
    }

    private static bool IsCommandVisible(ICommand command)
    {
        if (command is IHasMetadata metadataCommand)
        {
            return metadataCommand.Metadata.IsVisible;
        }

        return true;
    }

    private static CommandMetadata? TryGetMetadata(ICommand command)
    {
        if (command is IHasMetadata metadataCommand)
        {
            return metadataCommand.Metadata;
        }

        return null;
    }

    private string FormatHeader(string text)
    {
        if (_options.UseColors)
        {
            return $"\u001b[1m{text}\u001b[0m";
        }

        return text;
    }

    private string FormatSectionHeader(string text)
    {
        if (_options.UseColors)
        {
            return $"\u001b[1m{text}:\u001b[0m";
        }

        return $"{text}:";
    }
}
