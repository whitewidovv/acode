// <copyright file="TimeoutManagerTests.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

using Acode.Cli.NonInteractive;
using FluentAssertions;

namespace Acode.Cli.Tests.NonInteractive;

/// <summary>
/// Unit tests for <see cref="TimeoutManager"/>.
/// </summary>
public sealed class TimeoutManagerTests : IDisposable
{
    private TimeoutManager? _sut;

    /// <summary>
    /// FR-025: --timeout MUST set global timeout.
    /// </summary>
    [Fact]
    public void Should_Set_Timeout_Duration()
    {
        // Arrange
        var timeout = TimeSpan.FromMinutes(5);

        // Act
        _sut = new TimeoutManager(timeout);

        // Assert
        _sut.Timeout.Should().Be(timeout);
    }

    /// <summary>
    /// FR-028: Timeout 0 MUST mean no timeout.
    /// </summary>
    [Fact]
    public void Should_Have_Infinite_Remaining_With_Zero_Timeout()
    {
        // Arrange
        _sut = new TimeoutManager(TimeSpan.Zero);

        // Act
        _sut.Start();

        // Assert
        _sut.Remaining.Should().Be(Timeout.InfiniteTimeSpan);
        _sut.IsExpired.Should().BeFalse();
    }

    /// <summary>
    /// FR-030: Remaining time MUST be tracked.
    /// </summary>
    [Fact]
    public void Should_Track_Remaining_Time()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(10);
        _sut = new TimeoutManager(timeout);

        // Act
        _sut.Start();

        // Assert
        _sut.Remaining.Should().BeCloseTo(timeout, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Should detect when timeout is expired.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task Should_Detect_Expired_Timeout()
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds(50);
        _sut = new TimeoutManager(timeout);

        // Act
        _sut.Start();
        await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(true);

        // Assert
        _sut.IsExpired.Should().BeTrue();
        _sut.Remaining.Should().Be(TimeSpan.Zero);
    }

    /// <summary>
    /// Remaining should be infinite before start.
    /// </summary>
    [Fact]
    public void Should_Have_Infinite_Remaining_Before_Start()
    {
        // Arrange
        _sut = new TimeoutManager(TimeSpan.FromMinutes(5));

        // Act
        var remaining = _sut.Remaining;

        // Assert
        remaining.Should().Be(Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Start should be idempotent.
    /// </summary>
    [Fact]
    public void Should_Allow_Multiple_Start_Calls()
    {
        // Arrange
        _sut = new TimeoutManager(TimeSpan.FromMinutes(5));

        // Act
        _sut.Start();
        _sut.Start();

        // Assert - should not throw
        _sut.IsExpired.Should().BeFalse();
    }

    /// <summary>
    /// Cancel should work correctly.
    /// </summary>
    [Fact]
    public void Should_Cancel_Timeout()
    {
        // Arrange
        _sut = new TimeoutManager(TimeSpan.FromMinutes(5));
        _sut.Start();

        // Act
        _sut.Cancel();

        // Assert
        _sut.Token.IsCancellationRequested.Should().BeTrue();
    }

    /// <summary>
    /// Dispose should work correctly.
    /// </summary>
    [Fact]
    public void Should_Dispose_Correctly()
    {
        // Arrange
        _sut = new TimeoutManager(TimeSpan.FromMinutes(5));
        _sut.Start();

        // Act
        _sut.Dispose();

        // Assert
        var act = () => _sut.Start();
        act.Should().Throw<ObjectDisposedException>();
    }

    /// <summary>
    /// WaitAsync should complete when cancelled.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task WaitAsync_Should_Complete_On_Cancellation()
    {
        // Arrange
        _sut = new TimeoutManager(TimeSpan.FromMinutes(5));
        _sut.Start();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        // Act
        var waitTask = _sut.WaitAsync(cts.Token);
        await waitTask.ConfigureAwait(true);

        // Assert - should complete without throwing
        waitTask.IsCompleted.Should().BeTrue();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _sut?.Dispose();
    }
}
