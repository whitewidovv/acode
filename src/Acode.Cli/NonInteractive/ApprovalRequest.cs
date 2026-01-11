// <copyright file="ApprovalRequest.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Request for approval of an action.
/// </summary>
/// <param name="ActionType">The type of action being requested.</param>
/// <param name="RiskLevel">The risk level of the action.</param>
/// <param name="Context">Additional context about the request.</param>
public sealed record ApprovalRequest(
    string ActionType,
    RiskLevel RiskLevel,
    IReadOnlyDictionary<string, object> Context
);
