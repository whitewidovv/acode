using System.Collections.Frozen;

namespace Acode.Domain.Validation;

/// <summary>
/// Default allowlist for local endpoints.
/// </summary>
/// <remarks>
/// Per Task 001.b FR-001b-41 to FR-001b-55:
/// Default allowlist includes localhost (127.0.0.1, localhost, ::1)
/// on port 11434 for Ollama local inference.
/// </remarks>
public static class DefaultAllowlist
{
    private static readonly FrozenSet<AllowlistEntry> _defaultEntries;

    static DefaultAllowlist()
    {
        _defaultEntries = new AllowlistEntry[]
        {
            new()
            {
                Host = "localhost",
                Ports = new[] { 11434 },
                Reason = "Ollama local inference server"
            },
            new()
            {
                Host = "127.0.0.1",
                Ports = new[] { 11434 },
                Reason = "Ollama local inference server (IPv4 loopback)"
            },
            new()
            {
                Host = "::1",
                Ports = new[] { 11434 },
                Reason = "Ollama local inference server (IPv6 loopback)"
            },
        }.ToFrozenSet();
    }

    /// <summary>
    /// Gets the default allowlist entries.
    /// </summary>
    /// <returns>Immutable set of default allowlist entries.</returns>
    public static IReadOnlyList<AllowlistEntry> GetDefaultEntries()
    {
        return _defaultEntries.ToList().AsReadOnly();
    }

    /// <summary>
    /// Checks if a URI is allowed by the default allowlist.
    /// </summary>
    /// <param name="uri">URI to check.</param>
    /// <returns>True if URI matches any default allowlist entry, false otherwise.</returns>
    public static bool IsAllowed(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        return _defaultEntries.Any(entry => entry.Matches(uri));
    }
}
