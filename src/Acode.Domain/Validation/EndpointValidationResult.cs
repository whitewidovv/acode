namespace Acode.Domain.Validation;

/// <summary>
/// Result of endpoint validation.
/// Indicates whether an endpoint is allowed and why.
/// </summary>
/// <param name="IsAllowed">True if endpoint is allowed.</param>
/// <param name="Reason">Human-readable explanation.</param>
/// <param name="ViolatedConstraint">Constraint ID if denied (e.g., "HC-01").</param>
/// <remarks>
/// Per Task 001.b, this is used to enforce "no external LLM API" validation rules.
/// </remarks>
public sealed record EndpointValidationResult(
    bool IsAllowed,
    string Reason,
    string? ViolatedConstraint = null)
{
    /// <summary>
    /// Implicit conversion to bool for easy checking.
    /// </summary>
    /// <param name="result">Validation result.</param>
    public static implicit operator bool(EndpointValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsAllowed;
    }

    /// <summary>
    /// Create an allowed result.
    /// </summary>
    /// <param name="reason">Reason why endpoint is allowed.</param>
    /// <returns>Validation result indicating allowed.</returns>
    public static EndpointValidationResult Allowed(string reason)
    {
        return new EndpointValidationResult(true, reason, null);
    }

    /// <summary>
    /// Create a denied result.
    /// </summary>
    /// <param name="violatedConstraint">Constraint ID that was violated.</param>
    /// <param name="reason">Reason why endpoint is denied.</param>
    /// <returns>Validation result indicating denied.</returns>
    public static EndpointValidationResult Denied(string violatedConstraint, string reason)
    {
        return new EndpointValidationResult(false, reason, violatedConstraint);
    }
}
