// <copyright file="IApprovalPolicy.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Interface for approval policies in non-interactive mode.
/// </summary>
/// <remarks>
/// FR-017 through FR-024: Approval handling requirements.
/// </remarks>
public interface IApprovalPolicy
{
    /// <summary>
    /// Gets the name of this policy.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Evaluates an approval request according to this policy.
    /// </summary>
    /// <param name="request">The approval request to evaluate.</param>
    /// <returns>The decision for this request.</returns>
    ApprovalDecision Evaluate(ApprovalRequest request);
}
