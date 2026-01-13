using Acode.Domain.Security.PathProtection;

namespace Acode.Infrastructure.Security.PathProtection;

/// <summary>
/// Detects the current platform and provides platform-specific path handling information.
/// </summary>
public static class PlatformPathDetector
{
    /// <summary>
    /// Detects the current operating system platform.
    /// </summary>
    /// <returns>The current platform.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown when the platform is not supported.</exception>
    public static Platform DetectCurrentPlatform()
    {
        if (OperatingSystem.IsWindows())
        {
            return Platform.Windows;
        }

        if (OperatingSystem.IsLinux())
        {
            return Platform.Linux;
        }

        if (OperatingSystem.IsMacOS())
        {
            return Platform.MacOS;
        }

        throw new PlatformNotSupportedException("Unsupported platform");
    }

    /// <summary>
    /// Determines if the current platform uses case-sensitive file paths.
    /// </summary>
    /// <returns>True if the platform is case-sensitive (Linux, macOS); false otherwise (Windows).</returns>
    public static bool IsCaseSensitivePlatform()
    {
        var platform = DetectCurrentPlatform();
        return platform != Platform.Windows;
    }
}
