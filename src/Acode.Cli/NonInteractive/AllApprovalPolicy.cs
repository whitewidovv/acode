// <copyright file="AllApprovalPolicy.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Approval policy that approves all actions regardless of risk level.
/// </summary>
/// <remarks>
/// FR-022: Policy "all" MUST approve all actions.
/// </remarks>
public sealed class AllApprovalPolicy : IApprovalPolicy
{
    /// <inheritdoc/>
    public string Name => "all";

    /// <inheritdoc/>
    public ApprovalDecision Evaluate(ApprovalRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ApprovalDecision.Approve;
    }
}
