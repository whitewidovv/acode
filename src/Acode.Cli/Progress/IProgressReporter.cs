// <copyright file="IProgressReporter.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

namespace Acode.Cli.Progress;

/// <summary>
/// Interface for reporting progress in non-interactive mode.
/// </summary>
/// <remarks>
/// FR-045 through FR-050: Progress reporting requirements.
/// </remarks>
public interface IProgressReporter
{
    /// <summary>
    /// Gets or sets the progress reporting interval.
    /// </summary>
    TimeSpan Interval { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether progress is suppressed (quiet mode).
    /// </summary>
    bool IsSuppressed { get; set; }

    /// <summary>
    /// Reports progress information.
    /// </summary>
    /// <param name="progress">The progress information.</param>
    void Report(ProgressInfo progress);

    /// <summary>
    /// Starts the progress reporter timer.
    /// </summary>
    void StartReporting();

    /// <summary>
    /// Stops the progress reporter timer.
    /// </summary>
    void StopReporting();
}

/// <summary>
/// Progress information for reporting.
/// </summary>
/// <param name="PercentComplete">The percentage complete (0-100).</param>
/// <param name="Message">The progress message.</param>
/// <param name="CurrentStep">The current step name.</param>
/// <param name="TotalSteps">The total number of steps.</param>
/// <param name="CurrentStepNumber">The current step number.</param>
public sealed record ProgressInfo(
    int PercentComplete,
    string Message,
    string? CurrentStep = null,
    int? TotalSteps = null,
    int? CurrentStepNumber = null
);
