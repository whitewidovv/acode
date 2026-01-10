// <copyright file="ProgressReporterTests.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

using Acode.Cli.Progress;
using FluentAssertions;

namespace Acode.Cli.Tests.Progress;

/// <summary>
/// Unit tests for <see cref="NonInteractiveProgressReporter"/>.
/// </summary>
public sealed class ProgressReporterTests : IDisposable
{
    private readonly StringWriter _output;
    private NonInteractiveProgressReporter? _reporter;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressReporterTests"/> class.
    /// </summary>
    public ProgressReporterTests()
    {
        _output = new StringWriter();
    }

    /// <summary>
    /// FR-045: Progress MUST go to stderr.
    /// </summary>
    [Fact]
    public void Should_Write_Progress_To_Output()
    {
        // Arrange - Use minimum valid interval (1 second)
        _reporter = new NonInteractiveProgressReporter(
            _output,
            new ProgressInterval(TimeSpan.FromSeconds(1))
        );
        _reporter.StartReporting();

        // Act - Force immediate output by reporting after interval elapsed simulation
        // We simulate elapsed time by calling Report with ForceWrite
        _reporter.ForceReport(new ProgressInfo(50, "Processing..."));
        _output.Flush();

        // Assert
        _output.ToString().Should().Contain("Progress: 50%");
        _output.ToString().Should().Contain("Processing...");
    }

    /// <summary>
    /// FR-046: Progress MUST include timestamp.
    /// </summary>
    [Fact]
    public void Should_Include_Timestamp()
    {
        // Arrange - Use minimum valid interval (1 second)
        _reporter = new NonInteractiveProgressReporter(
            _output,
            new ProgressInterval(TimeSpan.FromSeconds(1))
        );
        _reporter.StartReporting();

        // Act - Force immediate output
        _reporter.ForceReport(new ProgressInfo(25, "Working"));
        _output.Flush();

        // Assert
        _output.ToString().Should().MatchRegex(@"\[\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z\]");
    }

    /// <summary>
    /// FR-048: Progress frequency MUST be configurable.
    /// </summary>
    [Fact]
    public void Should_Allow_Configurable_Interval()
    {
        // Arrange
        _reporter = new NonInteractiveProgressReporter(_output);

        // Act
        _reporter.Interval = TimeSpan.FromSeconds(30);

        // Assert
        _reporter.Interval.Should().Be(TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// FR-049: Default progress interval: 10 seconds.
    /// </summary>
    [Fact]
    public void Should_Have_Default_Interval()
    {
        // Arrange & Act
        _reporter = new NonInteractiveProgressReporter(_output);

        // Assert
        _reporter.Interval.Should().Be(TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// FR-050: --quiet MUST suppress progress.
    /// </summary>
    [Fact]
    public void Should_Suppress_Progress_When_Quiet()
    {
        // Arrange - Use minimum valid interval (1 second)
        _reporter = new NonInteractiveProgressReporter(
            _output,
            new ProgressInterval(TimeSpan.FromSeconds(1))
        );
        _reporter.IsSuppressed = true;
        _reporter.StartReporting();

        // Act - Attempt forced reports (should still be suppressed)
        _reporter.ForceReport(new ProgressInfo(50, "Processing..."));
        _reporter.ForceReport(new ProgressInfo(75, "More processing..."));
        _output.Flush();

        // Assert
        _output.ToString().Should().BeEmpty();
    }

    /// <summary>
    /// Should start and stop correctly.
    /// </summary>
    [Fact]
    public void Should_Start_And_Stop()
    {
        // Arrange
        _reporter = new NonInteractiveProgressReporter(_output);

        // Act & Assert - should not throw
        _reporter.StartReporting();
        _reporter.StopReporting();
    }

    /// <summary>
    /// Should be idempotent.
    /// </summary>
    [Fact]
    public void Should_Handle_Multiple_Start_Stop_Calls()
    {
        // Arrange
        _reporter = new NonInteractiveProgressReporter(_output);

        // Act & Assert - should not throw
        _reporter.StartReporting();
        _reporter.StartReporting();
        _reporter.StopReporting();
        _reporter.StopReporting();
    }

    /// <summary>
    /// Should throw after dispose.
    /// </summary>
    [Fact]
    public void Should_Throw_After_Dispose()
    {
        // Arrange
        _reporter = new NonInteractiveProgressReporter(_output);
        _reporter.Dispose();

        // Act
        var act = () => _reporter.StartReporting();

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    /// <summary>
    /// Should throw on null progress.
    /// </summary>
    [Fact]
    public void Should_Throw_On_Null_Progress()
    {
        // Arrange
        _reporter = new NonInteractiveProgressReporter(_output);

        // Act
        var act = () => _reporter.Report(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("progress");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _reporter?.Dispose();
        _output.Dispose();
    }
}
