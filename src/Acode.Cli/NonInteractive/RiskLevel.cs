// <copyright file="RiskLevel.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Risk level for actions requiring approval.
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// Low risk actions like reading files.
    /// </summary>
    Low,

    /// <summary>
    /// Medium risk actions like writing files.
    /// </summary>
    Medium,

    /// <summary>
    /// High risk actions like deleting files.
    /// </summary>
    High,

    /// <summary>
    /// Critical risk actions like executing commands or git operations.
    /// </summary>
    Critical,
}
