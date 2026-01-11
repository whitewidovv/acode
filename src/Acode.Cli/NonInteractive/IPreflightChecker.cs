// <copyright file="IPreflightChecker.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.NonInteractive;

/// <summary>
/// Interface for pre-flight checks before running commands.
/// </summary>
/// <remarks>
/// FR-071 through FR-076: Pre-flight check requirements.
/// </remarks>
public interface IPreflightChecker
{
    /// <summary>
    /// Runs all pre-flight checks asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of all pre-flight checks.</returns>
    Task<PreflightResult> RunAllChecksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a check to the pre-flight checklist.
    /// </summary>
    /// <param name="check">The check to add.</param>
    void AddCheck(IPreflightCheck check);
}
