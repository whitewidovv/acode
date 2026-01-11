namespace Acode.Cli.Commands;

/// <summary>
/// Represents a usage example for a command.
/// </summary>
/// <param name="CommandLine">The example command line.</param>
/// <param name="Description">Description of what the example demonstrates.</param>
public sealed record CommandExample(string CommandLine, string Description)
{
    /// <summary>
    /// Gets a formatted string representation of the example.
    /// </summary>
    /// <param name="indent">Number of spaces to indent.</param>
    /// <returns>Formatted example string.</returns>
    public string GetFormatted(int indent = 0)
    {
        var prefix = new string(' ', indent);
        return $"{prefix}$ {CommandLine}\n{prefix}  {Description}";
    }
}
