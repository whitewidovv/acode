// <copyright file="SignalHandlerTests.cs" company="Acode">
// Copyright (c) Acode. All rights reserved.
// </copyright>

using Acode.Cli.NonInteractive;
using FluentAssertions;

namespace Acode.Cli.Tests.NonInteractive;

/// <summary>
/// Unit tests for <see cref="SignalHandler"/>.
/// </summary>
public sealed class SignalHandlerTests
{
    /// <summary>
    /// FR-060: SIGPIPE MUST NOT crash.
    /// </summary>
    [Fact]
    public void Should_Handle_SIGPIPE()
    {
        // Arrange
        var handler = new SignalHandler(isInteractive: false);
        var pipeErrorReceived = false;

        handler.PipeError += (sender, args) => pipeErrorReceived = true;
        handler.Register();

        // Act
        handler.OnBrokenPipe();

        // Assert
        pipeErrorReceived.Should().BeTrue("SIGPIPE should be handled gracefully");
    }

    /// <summary>
    /// FR-062: Shutdown MUST have maximum duration.
    /// Interactive: 30s, Non-interactive: 10s.
    /// </summary>
    [Fact]
    public void Should_Use_Shorter_Grace_Period_In_NonInteractive()
    {
        // Arrange
        var interactiveHandler = new SignalHandler(isInteractive: true);
        var nonInteractiveHandler = new SignalHandler(isInteractive: false);

        // Act
        var interactiveGrace = interactiveHandler.GracePeriod;
        var nonInteractiveGrace = nonInteractiveHandler.GracePeriod;

        // Assert
        interactiveGrace.Should().Be(TimeSpan.FromSeconds(30));
        nonInteractiveGrace
            .Should()
            .Be(TimeSpan.FromSeconds(10), "non-interactive mode should have shorter grace period");
    }

    /// <summary>
    /// Request shutdown should trigger event.
    /// </summary>
    [Fact]
    public void Should_Trigger_Shutdown_Event()
    {
        // Arrange
        var handler = new SignalHandler(isInteractive: false);
        var shutdownRequested = false;

        handler.ShutdownRequested += (sender, args) => shutdownRequested = true;
        handler.Register();

        // Act
        handler.RequestShutdown();

        // Assert
        shutdownRequested.Should().BeTrue();
    }

    /// <summary>
    /// Shutdown should be idempotent.
    /// </summary>
    [Fact]
    public void Should_Allow_Multiple_Shutdown_Requests()
    {
        // Arrange
        var handler = new SignalHandler(isInteractive: false);
        var shutdownCount = 0;

        handler.ShutdownRequested += (sender, args) => shutdownCount++;
        handler.Register();

        // Act
        handler.RequestShutdown();
        handler.RequestShutdown();
        handler.RequestShutdown();

        // Assert
        shutdownCount.Should().Be(1, "shutdown should only be requested once");
    }

    /// <summary>
    /// WaitForShutdownAsync should complete promptly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task WaitForShutdownAsync_Should_Complete()
    {
        // Arrange
        var handler = new SignalHandler(isInteractive: false);
        handler.Register();
        handler.RequestShutdown();

        // Act
        var startTime = DateTimeOffset.UtcNow;
        await handler.WaitForShutdownAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(true);
        var duration = DateTimeOffset.UtcNow - startTime;

        // Assert
        duration.Should().BeLessThan(TimeSpan.FromSeconds(2));
    }

    /// <summary>
    /// Register/Unregister should be idempotent.
    /// </summary>
    [Fact]
    public void Should_Allow_Multiple_Register_Unregister_Calls()
    {
        // Arrange
        var handler = new SignalHandler(isInteractive: false);

        // Act & Assert - should not throw
        handler.Register();
        handler.Register();
        handler.Unregister();
        handler.Unregister();
    }

    /// <summary>
    /// SignalEventArgs should store signal type.
    /// </summary>
    [Fact]
    public void SignalEventArgs_Should_Store_SignalType()
    {
        // Arrange & Act
        var args = new SignalEventArgs("SIGINT");

        // Assert
        args.SignalType.Should().Be("SIGINT");
    }

    /// <summary>
    /// SignalEventArgs should validate null.
    /// </summary>
    [Fact]
    public void SignalEventArgs_Should_Throw_On_Null()
    {
        // Act
        var act = () => new SignalEventArgs(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("signalType");
    }
}
