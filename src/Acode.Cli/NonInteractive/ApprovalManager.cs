// <copyright file="ApprovalManager.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Manages approval requests in non-interactive mode.
/// </summary>
/// <remarks>
/// FR-017 through FR-024: Approval handling implementation.
/// </remarks>
public sealed class ApprovalManager
{
    private readonly NonInteractiveOptions _options;
    private readonly IApprovalPolicy? _policy;
    private readonly ILogger<ApprovalManager>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovalManager"/> class.
    /// </summary>
    /// <param name="options">Non-interactive options.</param>
    /// <param name="logger">Optional logger for approval decisions.</param>
    public ApprovalManager(NonInteractiveOptions options, ILogger<ApprovalManager>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
        _logger = logger;

        // Create policy based on options
        if (!string.IsNullOrEmpty(options.ApprovalPolicy))
        {
            _policy = ApprovalPolicyFactory.Create(options.ApprovalPolicy);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovalManager"/> class.
    /// </summary>
    /// <param name="options">Non-interactive options.</param>
    /// <param name="approvalPolicy">The approval policy to use.</param>
    /// <param name="logger">Optional logger for approval decisions.</param>
    public ApprovalManager(
        NonInteractiveOptions options,
        IApprovalPolicy? approvalPolicy,
        ILogger<ApprovalManager>? logger = null
    )
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
        _policy = approvalPolicy;
        _logger = logger;
    }

    /// <summary>
    /// Requests approval for an action.
    /// </summary>
    /// <param name="request">The approval request.</param>
    /// <returns>The approval decision.</returns>
    /// <exception cref="ApprovalRequiredException">
    /// Thrown when approval is required but cannot be obtained in non-interactive mode.
    /// </exception>
    public ApprovalDecision RequestApproval(ApprovalRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // FR-017: --yes MUST auto-approve all prompts
        if (_options.Yes)
        {
            LogDecision(request, ApprovalDecision.Approve, "auto-approved via --yes flag");
            return ApprovalDecision.Approve;
        }

        // FR-018: --no-approve MUST reject all prompts
        if (_options.NoApprove)
        {
            LogDecision(request, ApprovalDecision.Reject, "rejected via --no-approve flag");
            return ApprovalDecision.Reject;
        }

        // Apply policy if available
        if (_policy != null)
        {
            var decision = _policy.Evaluate(request);
            LogDecision(request, decision, $"evaluated by {_policy.Name} policy");

            // FR-024: Missing approval in non-interactive MUST fail
            if (decision == ApprovalDecision.RequireExplicit && _options.NonInteractive)
            {
                throw new ApprovalRequiredException(
                    $"Action '{request.ActionType}' requires approval but running in non-interactive mode"
                )
                {
                    ActionType = request.ActionType,
                    RiskLevel = request.RiskLevel,
                };
            }

            return decision;
        }

        // FR-024: Missing approval in non-interactive MUST fail
        if (_options.NonInteractive)
        {
            throw new ApprovalRequiredException(
                $"Action '{request.ActionType}' requires approval but running in non-interactive mode without an approval policy"
            )
            {
                ActionType = request.ActionType,
                RiskLevel = request.RiskLevel,
            };
        }

        // Default to require explicit approval in interactive mode
        return ApprovalDecision.RequireExplicit;
    }

    private void LogDecision(ApprovalRequest request, ApprovalDecision decision, string reason)
    {
        if (_logger == null)
        {
            return;
        }

        // FR-023: Approval decisions MUST be logged
        _logger.LogInformation(
            "Approval decision for {ActionType} (risk: {RiskLevel}): {Decision} - {Reason}",
            request.ActionType,
            request.RiskLevel,
            decision,
            reason
        );
    }
}
