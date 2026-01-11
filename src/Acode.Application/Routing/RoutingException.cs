namespace Acode.Application.Routing;

using System;
using System.Collections.Generic;

/// <summary>
/// Exception thrown when routing fails due to configuration, availability, or constraint issues.
/// </summary>
/// <remarks>
/// Error codes follow ACODE-RTE-XXX pattern.
/// </remarks>
public sealed class RoutingException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RoutingException"/> class.
    /// </summary>
    /// <param name="errorCode">The error code (e.g., ACODE-RTE-001).</param>
    /// <param name="message">The error message.</param>
    /// <param name="attemptedModels">Models that were attempted before failure.</param>
    /// <param name="innerException">The inner exception, if any.</param>
    public RoutingException(
        string errorCode,
        string message,
        IReadOnlyList<string>? attemptedModels,
        Exception? innerException = null
    )
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        AttemptedModels = attemptedModels ?? Array.Empty<string>();
    }

    /// <summary>
    /// Gets the error code for this routing exception.
    /// </summary>
    /// <remarks>
    /// <para>Error codes:</para>
    /// <list type="bullet">
    /// <item><description>ACODE-RTE-001: No available model for role.</description></item>
    /// <item><description>ACODE-RTE-002: Invalid model ID format.</description></item>
    /// <item><description>ACODE-RTE-003: Operating mode constraint violation.</description></item>
    /// <item><description>ACODE-RTE-004: Fallback chain exhausted.</description></item>
    /// <item><description>ACODE-RTE-005: Invalid routing configuration.</description></item>
    /// <item><description>ACODE-RTE-006: No model supports required capabilities.</description></item>
    /// </list>
    /// </remarks>
    public string ErrorCode { get; }

    /// <summary>
    /// Gets the models that were attempted before this exception was thrown.
    /// </summary>
    public IReadOnlyList<string> AttemptedModels { get; }

    /// <summary>
    /// Gets a suggestion for resolving the issue.
    /// </summary>
    public string? Suggestion { get; init; }
}
