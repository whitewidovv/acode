// <copyright file="IPreflightCheck.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Interface for individual pre-flight checks.
/// </summary>
public interface IPreflightCheck
{
    /// <summary>
    /// Gets the name of this check.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of this check.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Runs the check asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The check result.</returns>
    Task<PreflightCheckResult> RunAsync(CancellationToken cancellationToken = default);
}
