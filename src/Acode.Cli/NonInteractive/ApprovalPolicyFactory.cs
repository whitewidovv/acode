// <copyright file="ApprovalPolicyFactory.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Factory for creating approval policies by name.
/// </summary>
/// <remarks>
/// FR-019: --approval-policy MUST accept policy name.
/// </remarks>
public static class ApprovalPolicyFactory
{
    /// <summary>
    /// Creates an approval policy by name.
    /// </summary>
    /// <param name="policyName">The name of the policy to create.</param>
    /// <returns>The created approval policy.</returns>
    /// <exception cref="ArgumentException">Thrown when the policy name is unknown.</exception>
    public static IApprovalPolicy Create(string policyName)
    {
        ArgumentNullException.ThrowIfNull(policyName);

        return policyName.ToLowerInvariant() switch
        {
            "none" => new NoneApprovalPolicy(),
            "low-risk" => new LowRiskApprovalPolicy(),
            "all" => new AllApprovalPolicy(),
            _ => throw new ArgumentException(
                $"Unknown approval policy: {policyName}",
                nameof(policyName)
            ),
        };
    }

    /// <summary>
    /// Gets the list of valid policy names.
    /// </summary>
    /// <returns>An array of valid policy names.</returns>
    public static string[] GetValidPolicyNames() => ["none", "low-risk", "all"];
}
