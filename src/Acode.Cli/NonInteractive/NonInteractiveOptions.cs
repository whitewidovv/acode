// <copyright file="NonInteractiveOptions.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Options for non-interactive mode configuration.
/// </summary>
public sealed class NonInteractiveOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to force non-interactive mode.
    /// </summary>
    /// <remarks>
    /// FR-003: --non-interactive MUST force non-interactive mode.
    /// </remarks>
    public bool NonInteractive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to auto-approve all prompts.
    /// </summary>
    /// <remarks>
    /// FR-017: --yes MUST auto-approve all prompts.
    /// </remarks>
    public bool Yes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to reject all approvals.
    /// </summary>
    /// <remarks>
    /// FR-018: --no-approve MUST reject all prompts.
    /// </remarks>
    public bool NoApprove { get; set; }

    /// <summary>
    /// Gets or sets the approval policy name.
    /// </summary>
    /// <remarks>
    /// FR-019: --approval-policy MUST accept policy name.
    /// Valid values: "none", "low-risk", "all".
    /// </remarks>
    public string? ApprovalPolicy { get; set; }

    /// <summary>
    /// Gets or sets the global timeout in seconds.
    /// </summary>
    /// <remarks>
    /// FR-025: --timeout MUST set global timeout.
    /// FR-027: Default timeout MUST be 3600 seconds (1 hour).
    /// FR-028: Timeout 0 MUST mean no timeout.
    /// </remarks>
    public int TimeoutSeconds { get; set; } = 3600;

    /// <summary>
    /// Gets or sets a value indicating whether to suppress progress output.
    /// </summary>
    /// <remarks>
    /// FR-050: --quiet MUST suppress progress.
    /// </remarks>
    public bool Quiet { get; set; }

    /// <summary>
    /// Gets or sets the progress reporting interval in seconds.
    /// </summary>
    /// <remarks>
    /// FR-048: Progress frequency MUST be configurable.
    /// FR-049: Default progress interval: 10 seconds.
    /// </remarks>
    public int ProgressIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets a value indicating whether to skip pre-flight checks.
    /// </summary>
    /// <remarks>
    /// FR-076: --skip-preflight MUST bypass checks.
    /// </remarks>
    public bool SkipPreflight { get; set; }
}
