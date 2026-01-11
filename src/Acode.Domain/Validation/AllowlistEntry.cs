namespace Acode.Domain.Validation;

/// <summary>
/// Entry in the endpoint allowlist.
/// Defines a host and optional port restrictions that are explicitly allowed.
/// </summary>
/// <remarks>
/// Per Task 001.b FR-001b-41 to FR-001b-55:
/// Allowlist entries permit specific endpoints (like localhost:11434 for Ollama)
/// even when the operating mode would otherwise block them.
/// </remarks>
public record AllowlistEntry
{
    private static readonly string[] _localhostEquivalents = { "localhost", "127.0.0.1", "::1", "[::1]" };

    /// <summary>
    /// Gets the host to allow (e.g., "localhost", "127.0.0.1", "::1").
    /// </summary>
    public required string Host { get; init; }

    /// <summary>
    /// Gets the allowed ports (null = any port allowed).
    /// If specified, only URIs with matching ports will match this entry.
    /// </summary>
    public int[]? Ports { get; init; }

    /// <summary>
    /// Gets the reason why this host is allowed (for documentation/auditing).
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Gets the operating mode required for this entry (optional).
    /// If specified, this entry only applies in the specified mode.
    /// </summary>
    public Modes.OperatingMode? RequireMode { get; init; }

    /// <summary>
    /// Checks if the given URI matches this allowlist entry.
    /// </summary>
    /// <param name="uri">URI to check.</param>
    /// <returns>True if URI matches this entry, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">If uri is null.</exception>
    public bool Matches(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        // Check if host matches (with localhost equivalence)
        if (!HostMatches(uri.Host))
        {
            return false;
        }

        // Check port restriction if specified
        if (Ports is not null && Ports.Length > 0)
        {
            // If URI doesn't have an explicit port, it's using default (80 for HTTP, 443 for HTTPS)
            var uriPort = uri.Port;
            if (!Ports.Contains(uriPort))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="other">The object to compare with the current object.</param>
    /// <returns>True if objects are equal, false otherwise.</returns>
    /// <remarks>
    /// Provides value equality for Ports array.
    /// </remarks>
    public virtual bool Equals(AllowlistEntry? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Host == other.Host
            && Reason == other.Reason
            && RequireMode == other.RequireMode
            && PortsEqual(Ports, other.Ports);
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>Hash code.</returns>
    public override int GetHashCode()
    {
        var portsHash = Ports is not null
            ? Ports.Aggregate(0, (acc, port) => HashCode.Combine(acc, port))
            : 0;

        return HashCode.Combine(Host, Reason, RequireMode, portsHash);
    }

    private bool HostMatches(string uriHost)
    {
        // Case-insensitive comparison
        var normalizedUriHost = uriHost.ToLowerInvariant();
        var normalizedEntryHost = Host.ToLowerInvariant();

        // Direct match
        if (normalizedUriHost == normalizedEntryHost)
        {
            return true;
        }

        // Localhost equivalence: localhost, 127.0.0.1, and ::1 are treated as equivalent
        if (IsLocalhostEquivalent(normalizedEntryHost) && IsLocalhostEquivalent(normalizedUriHost))
        {
            return true;
        }

        return false;
    }

    private static bool IsLocalhostEquivalent(string host)
    {
        // Strip brackets from IPv6 if present
        var normalizedHost = host.TrimStart('[').TrimEnd(']');
        return _localhostEquivalents.Any(equiv =>
            equiv.TrimStart('[').TrimEnd(']').Equals(normalizedHost, StringComparison.OrdinalIgnoreCase));
    }

    private static bool PortsEqual(int[]? ports1, int[]? ports2)
    {
        if (ports1 is null && ports2 is null)
        {
            return true;
        }

        if (ports1 is null || ports2 is null)
        {
            return false;
        }

        if (ports1.Length != ports2.Length)
        {
            return false;
        }

        for (int i = 0; i < ports1.Length; i++)
        {
            if (ports1[i] != ports2[i])
            {
                return false;
            }
        }

        return true;
    }
}
