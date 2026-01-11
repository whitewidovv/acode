using System.Net;

namespace Acode.Domain.Validation;

/// <summary>
/// Validates endpoints against operating mode restrictions.
/// </summary>
/// <remarks>
/// Per Task 001.b FR-001b-56 to FR-001b-75:
/// Validates URIs and IP addresses against denylist, allowlist, and operating mode rules.
/// Enforces HC-01 (no external LLM APIs) and mode-specific constraints.
/// </remarks>
public interface IEndpointValidator
{
    /// <summary>
    /// Validates a URI endpoint against operating mode restrictions.
    /// </summary>
    /// <param name="endpoint">Endpoint URI to validate.</param>
    /// <param name="mode">Current operating mode.</param>
    /// <returns>Validation result indicating whether endpoint is allowed.</returns>
    EndpointValidationResult Validate(Uri endpoint, Modes.OperatingMode mode);

    /// <summary>
    /// Validates an IP address against operating mode restrictions.
    /// </summary>
    /// <param name="ip">IP address to validate.</param>
    /// <param name="mode">Current operating mode.</param>
    /// <returns>Validation result indicating whether IP is allowed.</returns>
    EndpointValidationResult ValidateIp(IPAddress ip, Modes.OperatingMode mode);
}
