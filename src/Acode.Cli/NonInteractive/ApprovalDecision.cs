// <copyright file="ApprovalDecision.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Result of an approval policy evaluation.
/// </summary>
public enum ApprovalDecision
{
    /// <summary>
    /// The action is approved.
    /// </summary>
    Approve,

    /// <summary>
    /// The action is rejected.
    /// </summary>
    Reject,

    /// <summary>
    /// The action requires explicit user approval.
    /// </summary>
    RequireExplicit,
}
