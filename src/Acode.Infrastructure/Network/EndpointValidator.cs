using System.Net;
using Acode.Domain.Modes;
using Acode.Domain.Validation;

namespace Acode.Infrastructure.Network;

/// <summary>
/// Validates endpoints against operating mode restrictions.
/// </summary>
/// <remarks>
/// Per Task 001.b FR-001b-56 to FR-001b-90:
/// Validates URIs and IP addresses against denylist, allowlist, and mode rules.
/// Enforces HC-01 (no external LLM APIs in LocalOnly/Airgapped)
/// and HC-02 (no network in Airgapped mode).
/// </remarks>
public class EndpointValidator : IEndpointValidator
{
    private readonly IAllowlistProvider? _allowlistProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointValidator"/> class.
    /// </summary>
    /// <param name="allowlistProvider">Optional allowlist provider (defaults to DefaultAllowlist).</param>
    public EndpointValidator(IAllowlistProvider? allowlistProvider = null)
    {
        _allowlistProvider = allowlistProvider;
    }

    /// <inheritdoc/>
    public EndpointValidationResult Validate(Uri endpoint, OperatingMode mode)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        // Per FR-001b-49: Check allowlist FIRST (takes precedence)
        // Exception: Airgapped mode blocks everything
        if (mode != OperatingMode.Airgapped && IsAllowlisted(endpoint))
        {
            return EndpointValidationResult.Allowed($"Endpoint {endpoint.Host} is in the allowlist");
        }

        // Airgapped mode: deny ALL network access (HC-02)
        if (mode == OperatingMode.Airgapped)
        {
            return EndpointValidationResult.Denied(
                "HC-02",
                $"Airgapped mode prohibits all network access. Endpoint: {endpoint.Host}");
        }

        // Burst mode: allow all (external LLM APIs explicitly permitted)
        if (mode == OperatingMode.Burst)
        {
            return EndpointValidationResult.Allowed("Burst mode allows external endpoints");
        }

        // LocalOnly mode: Check denylist for external LLM APIs
        if (LlmApiDenylist.IsDenied(endpoint))
        {
            var reason = $"External LLM API '{endpoint.Host}' is denied in LocalOnly mode. Switch to Burst mode to use external APIs, or use local inference (localhost:11434).";
            return EndpointValidationResult.Denied("HC-01", reason);
        }

        // LocalOnly mode: Allow non-LLM endpoints that aren't on denylist
        return EndpointValidationResult.Allowed($"Endpoint {endpoint.Host} is not on the denylist");
    }

    /// <inheritdoc/>
    public EndpointValidationResult ValidateIp(IPAddress ip, OperatingMode mode)
    {
        ArgumentNullException.ThrowIfNull(ip);

        // Airgapped mode: deny ALL
        if (mode == OperatingMode.Airgapped)
        {
            return EndpointValidationResult.Denied(
                "HC-02",
                $"Airgapped mode prohibits all network access. IP: {ip}");
        }

        // Burst mode: allow all
        if (mode == OperatingMode.Burst)
        {
            return EndpointValidationResult.Allowed("Burst mode allows external IPs");
        }

        // LocalOnly mode: Check if loopback
        if (IPAddress.IsLoopback(ip))
        {
            return EndpointValidationResult.Allowed($"Loopback IP {ip} is allowed");
        }

        // LocalOnly mode: Deny external IPs
        return EndpointValidationResult.Denied(
            "HC-01",
            $"External IP {ip} is denied in LocalOnly mode. Use localhost/127.0.0.1 for local services.");
    }

    private bool IsAllowlisted(Uri uri)
    {
        // Use injected provider if available, otherwise use default
        if (_allowlistProvider is not null)
        {
            return _allowlistProvider.IsAllowed(uri);
        }

        return DefaultAllowlist.IsAllowed(uri);
    }
}
