namespace Acode.Application.Security.Commands;

/// <summary>
/// Command to check if a path is protected by security rules.
/// </summary>
public sealed record CheckPathCommand
{
    /// <summary>
    /// Gets the path to validate.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the file operation to perform (optional).
    /// If specified, validates against operation-specific rules.
    /// </summary>
    public FileOperation? Operation { get; init; }
}
