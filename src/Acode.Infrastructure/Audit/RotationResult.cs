namespace Acode.Infrastructure.Audit;

/// <summary>
/// Result of a log rotation operation.
/// </summary>
public sealed record RotationResult
{
    /// <summary>
    /// Gets a value indicating whether rotation occurred.
    /// </summary>
    public required bool RotationOccurred { get; init; }

    /// <summary>
    /// Gets the path to the rotated file, if rotation occurred.
    /// </summary>
    public string? RotatedPath { get; init; }
}
