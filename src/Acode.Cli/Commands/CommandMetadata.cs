namespace Acode.Cli.Commands;

/// <summary>
/// Contains metadata about a command for help generation and discovery.
/// </summary>
/// <param name="Name">The primary name of the command.</param>
/// <param name="Description">A brief description of what the command does.</param>
/// <param name="Usage">The usage pattern (e.g., "acode run [options] TASK").</param>
/// <param name="Aliases">Alternative names for the command.</param>
/// <param name="Options">Command-line options the command accepts.</param>
/// <param name="Examples">Example usages of the command.</param>
/// <param name="RelatedCommands">Related commands the user might want to know about.</param>
/// <param name="IsVisible">Whether the command appears in help listings.</param>
/// <param name="Group">Category/group for organizing commands in help.</param>
public sealed record CommandMetadata(
    string Name,
    string Description,
    string Usage,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<CommandOption> Options,
    IReadOnlyList<CommandExample> Examples,
    IReadOnlyList<string> RelatedCommands,
    bool IsVisible = true,
    string? Group = null
)
{
    /// <summary>
    /// Creates metadata with minimal required properties.
    /// </summary>
    /// <param name="name">Command name.</param>
    /// <param name="description">Command description.</param>
    /// <returns>CommandMetadata with empty collections for optional properties.</returns>
    public static CommandMetadata Create(string name, string description) =>
        new(
            Name: name,
            Description: description,
            Usage: $"acode {name}",
            Aliases: Array.Empty<string>(),
            Options: Array.Empty<CommandOption>(),
            Examples: Array.Empty<CommandExample>(),
            RelatedCommands: Array.Empty<string>()
        );

    /// <summary>
    /// Creates a builder for constructing metadata fluently.
    /// </summary>
    /// <param name="name">Command name.</param>
    /// <param name="description">Command description.</param>
    /// <returns>A CommandMetadataBuilder instance.</returns>
    public static CommandMetadataBuilder Builder(string name, string description) =>
        new(name, description);
}
