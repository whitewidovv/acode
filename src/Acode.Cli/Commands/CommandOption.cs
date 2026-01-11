namespace Acode.Cli.Commands;

/// <summary>
/// Represents a command-line option for a command.
/// </summary>
/// <param name="LongName">The long form of the option (e.g., "verbose").</param>
/// <param name="ShortName">The short form of the option (e.g., 'v'). Can be null.</param>
/// <param name="Description">Description of what the option does.</param>
/// <param name="ValuePlaceholder">Placeholder for the value (e.g., "path"). Null for boolean flags.</param>
/// <param name="DefaultValue">Default value as a string, or null if no default.</param>
/// <param name="IsRequired">Indicates whether the option is required.</param>
/// <param name="Group">Optional grouping for help display.</param>
public sealed record CommandOption(
    string LongName,
    char? ShortName,
    string Description,
    string? ValuePlaceholder = null,
    string? DefaultValue = null,
    bool IsRequired = false,
    string? Group = null
)
{
    /// <summary>
    /// Gets a value indicating whether this option accepts a value.
    /// </summary>
    public bool AcceptsValue => ValuePlaceholder != null;

    /// <summary>
    /// Gets the formatted option string for help display.
    /// </summary>
    /// <returns>Formatted string like "-v, --verbose" or "--config path".</returns>
    public string GetFormattedOption()
    {
        var parts = new List<string>();

        if (ShortName.HasValue)
        {
            parts.Add($"-{ShortName.Value}");
        }

        var longForm = $"--{LongName}";
        if (ValuePlaceholder != null)
        {
            longForm += $" <{ValuePlaceholder}>";
        }

        parts.Add(longForm);

        return string.Join(", ", parts);
    }
}
