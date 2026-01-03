using Acode.Application.Security;
using Acode.Domain.Security.PathProtection;
using Acode.Infrastructure.Security;

namespace Acode.Cli.Commands;

/// <summary>
/// Command for security-related operations.
/// </summary>
public sealed class SecurityCommand
{
    private readonly IProtectedPathValidator _pathValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityCommand"/> class.
    /// </summary>
    public SecurityCommand()
    {
        _pathValidator = new ProtectedPathValidator();
    }

    /// <summary>
    /// Shows current security status.
    /// </summary>
    /// <returns>Exit code (0 = success).</returns>
    public int ShowStatus()
    {
        Console.WriteLine("Security Status:");
        Console.WriteLine("  Operating Mode: LocalOnly (default)");
        Console.WriteLine("  Protected Paths: Enforced");
        Console.WriteLine("  Secret Redaction: Enabled");
        Console.WriteLine("  Audit Logging: Enabled");
        Console.WriteLine($"  Denylist Entries: {DefaultDenylist.Entries.Count}");
        return 0;
    }

    /// <summary>
    /// Shows all denylist entries.
    /// </summary>
    /// <returns>Exit code (0 = success).</returns>
    public int ShowDenylist()
    {
        Console.WriteLine("Protected Paths Denylist:");
        Console.WriteLine();

        foreach (var entry in DefaultDenylist.Entries)
        {
            Console.WriteLine($"  Pattern:  {entry.Pattern}");
            Console.WriteLine($"  Category: {entry.Category}");
            Console.WriteLine($"  Risk ID:  {entry.RiskId}");
            Console.WriteLine($"  Reason:   {entry.Reason}");
            Console.WriteLine();
        }

        Console.WriteLine($"Total: {DefaultDenylist.Entries.Count} protected patterns");
        return 0;
    }

    /// <summary>
    /// Checks if a path is protected.
    /// </summary>
    /// <param name="path">Path to check.</param>
    /// <returns>Exit code (0 = allowed, 1 = blocked).</returns>
    public int CheckPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));

        var result = _pathValidator.Validate(path);

        if (result.IsProtected)
        {
            Console.WriteLine($"BLOCKED: {path}");
            Console.WriteLine($"  Pattern:  {result.MatchedPattern}");
            Console.WriteLine($"  Category: {result.Category}");
            Console.WriteLine($"  Risk ID:  {result.RiskId}");
            Console.WriteLine($"  Reason:   {result.Reason}");
            return 1;
        }

        Console.WriteLine($"ALLOWED: {path}");
        return 0;
    }
}
