// <copyright file="PreflightCheckResult.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Result of a single pre-flight check.
/// </summary>
/// <param name="CheckName">The name of the check.</param>
/// <param name="Passed">Whether the check passed.</param>
/// <param name="Message">The result message.</param>
public sealed record PreflightCheckResult(string CheckName, bool Passed, string Message);
