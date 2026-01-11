namespace Acode.Application.Commands;

/// <summary>
/// Detects the current platform and selects platform-specific command variants.
/// Supports platform detection per Task 002.c FR-002c-111 through FR-002c-120.
/// </summary>
public static class PlatformDetector
{
    /// <summary>
    /// Gets the current platform identifier.
    /// </summary>
    /// <returns>Platform identifier: "windows", "linux", or "macos".</returns>
    /// <remarks>
    /// Per FR-002c-112: Platform identifiers must be windows, linux, or macos.
    /// Per FR-002c-117: Platform detection must be deterministic.
    /// Per PERF-002c-03: Detection must complete in under 1ms.
    /// </remarks>
    public static string GetCurrentPlatform()
    {
        if (OperatingSystem.IsWindows())
        {
            return "windows";
        }

        if (OperatingSystem.IsLinux())
        {
            return "linux";
        }

        if (OperatingSystem.IsMacOS())
        {
            return "macos";
        }

        // Fallback to linux for unknown Unix-like systems
        return "linux";
    }

    /// <summary>
    /// Selects the appropriate command based on platform variants.
    /// </summary>
    /// <param name="defaultCommand">The default command to use if no platform variant exists.</param>
    /// <param name="platforms">Optional dictionary of platform-specific command variants.</param>
    /// <returns>The selected command (either platform variant or default).</returns>
    /// <remarks>
    /// Per FR-002c-114: Platform variant must override default command.
    /// Per FR-002c-115: Missing platform variant must use default.
    /// </remarks>
    public static string SelectCommand(
        string defaultCommand,
        IReadOnlyDictionary<string, string>? platforms)
    {
        ArgumentNullException.ThrowIfNull(defaultCommand);

        // No variants defined, use default
        if (platforms == null || platforms.Count == 0)
        {
            return defaultCommand;
        }

        // Try to find variant for current platform
        var currentPlatform = GetCurrentPlatform();
        if (platforms.TryGetValue(currentPlatform, out var variant))
        {
            return variant;
        }

        // No variant for current platform, use default
        return defaultCommand;
    }
}
