// <copyright file="LowRiskApprovalPolicy.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Approval policy that approves only low-risk actions.
/// </summary>
/// <remarks>
/// FR-021: Policy "low-risk" MUST approve low-risk only.
/// </remarks>
public sealed class LowRiskApprovalPolicy : IApprovalPolicy
{
    /// <inheritdoc/>
    public string Name => "low-risk";

    /// <inheritdoc/>
    public ApprovalDecision Evaluate(ApprovalRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.RiskLevel switch
        {
            RiskLevel.Low => ApprovalDecision.Approve,
            _ => ApprovalDecision.Reject,
        };
    }
}
