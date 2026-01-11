// <copyright file="NoneApprovalPolicy.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Approval policy that rejects all approval requests.
/// </summary>
/// <remarks>
/// FR-020: Policy "none" MUST reject all approvals.
/// </remarks>
public sealed class NoneApprovalPolicy : IApprovalPolicy
{
    /// <inheritdoc/>
    public string Name => "none";

    /// <inheritdoc/>
    public ApprovalDecision Evaluate(ApprovalRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ApprovalDecision.Reject;
    }
}
