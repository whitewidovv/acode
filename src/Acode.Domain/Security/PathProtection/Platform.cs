namespace Acode.Domain.Security.PathProtection;

/// <summary>
/// Operating system platforms for path protection rules.
/// Different platforms have different path conventions and protected locations.
/// </summary>
public enum Platform
{
    /// <summary>
    /// Microsoft Windows.
    /// Uses backslashes, drive letters, and Windows-specific paths.
    /// Examples: C:\Windows\, %USERPROFILE%\.ssh\.
    /// </summary>
    Windows,

    /// <summary>
    /// Linux operating system.
    /// Uses forward slashes, case-sensitive paths.
    /// Examples: /etc/, /var/log/, ~/.ssh/.
    /// </summary>
    Linux,

    /// <summary>
    /// Apple macOS.
    /// Uses forward slashes, has macOS-specific directories.
    /// Examples: /System/, /Library/, ~/Library/.
    /// </summary>
    MacOS,

    /// <summary>
    /// All platforms - rule applies universally.
    /// Used for platform-agnostic patterns like .env files.
    /// </summary>
    All
}
