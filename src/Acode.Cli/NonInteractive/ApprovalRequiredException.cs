// <copyright file="ApprovalRequiredException.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Exception thrown when approval is required but not available in non-interactive mode.
/// </summary>
/// <remarks>
/// FR-024: Missing approval in non-interactive MUST fail.
/// </remarks>
[System.Serializable]
public sealed class ApprovalRequiredException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovalRequiredException"/> class.
    /// </summary>
    public ApprovalRequiredException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovalRequiredException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public ApprovalRequiredException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovalRequiredException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ApprovalRequiredException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Gets the action type that required approval.
    /// </summary>
    public string? ActionType { get; init; }

    /// <summary>
    /// Gets the risk level of the action.
    /// </summary>
    public RiskLevel? RiskLevel { get; init; }
}
