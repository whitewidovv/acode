// <copyright file="NonInteractiveProgressReporter.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;

namespace Acode.Cli.Progress;

/// <summary>
/// Progress reporter for non-interactive mode.
/// </summary>
/// <remarks>
/// FR-045: Progress MUST go to stderr.
/// FR-046: Progress MUST include timestamp.
/// FR-047: Progress MUST be machine-parseable.
/// FR-048: Progress frequency MUST be configurable.
/// FR-049: Default progress interval: 10 seconds.
/// FR-050: --quiet MUST suppress progress.
/// </remarks>
public sealed class NonInteractiveProgressReporter : IProgressReporter, IDisposable
{
    private readonly TextWriter _output;
    private readonly ILogger<NonInteractiveProgressReporter>? _logger;
    private readonly ProgressInterval _progressInterval;
    private readonly Timer? _timer;

    private ProgressInfo? _lastProgress;
    private DateTimeOffset _lastReportTime;
    private bool _isRunning;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="NonInteractiveProgressReporter"/> class.
    /// </summary>
    /// <param name="output">The output stream (typically stderr).</param>
    /// <param name="interval">The progress reporting interval.</param>
    /// <param name="logger">Optional logger.</param>
    public NonInteractiveProgressReporter(
        TextWriter? output = null,
        ProgressInterval? interval = null,
        ILogger<NonInteractiveProgressReporter>? logger = null
    )
    {
        _output = output ?? Console.Error;
        _progressInterval = interval ?? new ProgressInterval();
        _logger = logger;
        _timer = new Timer(OnTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <inheritdoc/>
    public TimeSpan Interval
    {
        get => _progressInterval.Interval;
        set => _progressInterval.Interval = value;
    }

    /// <inheritdoc/>
    public bool IsSuppressed { get; set; }

    /// <inheritdoc/>
    public void StartReporting()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_isRunning)
        {
            return;
        }

        _isRunning = true;
        _lastReportTime = DateTimeOffset.UtcNow;
        _timer?.Change(Interval, Interval);
        _logger?.LogDebug("Progress reporter started with interval {Interval}", Interval);
    }

    /// <inheritdoc/>
    public void StopReporting()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (!_isRunning)
        {
            return;
        }

        _isRunning = false;
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        _logger?.LogDebug("Progress reporter stopped");
    }

    /// <inheritdoc/>
    public void Report(ProgressInfo progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        _lastProgress = progress;

        if (!IsSuppressed && ShouldReport())
        {
            WriteProgress(progress);
            _lastReportTime = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Forces an immediate progress report, bypassing the interval check.
    /// </summary>
    /// <remarks>
    /// Used for testing and for reporting important milestones that should not be delayed.
    /// Still respects the IsSuppressed flag.
    /// </remarks>
    /// <param name="progress">The progress information to report.</param>
    public void ForceReport(ProgressInfo progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        _lastProgress = progress;

        if (!IsSuppressed)
        {
            WriteProgress(progress);
            _lastReportTime = DateTimeOffset.UtcNow;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _timer?.Dispose();
        _isDisposed = true;
    }

    private bool ShouldReport()
    {
        var elapsed = DateTimeOffset.UtcNow - _lastReportTime;
        return elapsed >= Interval;
    }

    private void WriteProgress(ProgressInfo progress)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var stepInfo =
            progress.CurrentStepNumber.HasValue && progress.TotalSteps.HasValue
                ? $" [{progress.CurrentStepNumber}/{progress.TotalSteps}]"
                : string.Empty;

        var line =
            $"[{timestamp}] [INFO] Progress: {progress.PercentComplete}%{stepInfo} - {progress.Message}";

        try
        {
            _output.WriteLine(line);
            _output.Flush();
        }
        catch (IOException ex)
        {
            _logger?.LogWarning(ex, "Failed to write progress output");
        }
    }

    private void OnTimerElapsed(object? state)
    {
        if (!_isRunning || IsSuppressed || _lastProgress is null)
        {
            return;
        }

        WriteProgress(_lastProgress);
        _lastReportTime = DateTimeOffset.UtcNow;
    }
}
