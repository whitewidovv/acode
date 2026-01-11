// tests/Acode.Infrastructure.Tests/Sync/SyncEngineTests.cs
#pragma warning disable CA2007 // ConfigureAwait not required in test code

namespace Acode.Infrastructure.Tests.Sync;

using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Sync;
using Acode.Infrastructure.Sync;
using FluentAssertions;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for SyncEngine.
/// Verifies background sync orchestration, status tracking, and lifecycle management.
/// </summary>
public sealed class SyncEngineTests
{
    [Fact]
    public async Task StartAsync_SetsRunningStatus()
    {
        // Arrange
        var repository = Substitute.For<IOutboxRepository>();
        var engine = new SyncEngine(repository, pollingIntervalMs: 5000);

        // Act
        await engine.StartAsync(CancellationToken.None);

        // Assert
        var status = await engine.GetStatusAsync(CancellationToken.None);
        status.IsRunning.Should().BeTrue();
        status.IsPaused.Should().BeFalse();
        status.StartedAt.Should().NotBeNull();

        await engine.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_StopsEngine()
    {
        // Arrange
        var repository = Substitute.For<IOutboxRepository>();
        var engine = new SyncEngine(repository, pollingIntervalMs: 5000);
        await engine.StartAsync(CancellationToken.None);

        // Act
        await engine.StopAsync(CancellationToken.None);

        // Assert
        var status = await engine.GetStatusAsync(CancellationToken.None);
        status.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task StopAsync_WhenNotStarted_DoesNotThrow()
    {
        // Arrange
        var repository = Substitute.For<IOutboxRepository>();
        var engine = new SyncEngine(repository, pollingIntervalMs: 5000);

        // Act
        var act = async () => await engine.StopAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PauseAsync_SetsPausedStatus()
    {
        // Arrange
        var repository = Substitute.For<IOutboxRepository>();
        var engine = new SyncEngine(repository, pollingIntervalMs: 5000);
        await engine.StartAsync(CancellationToken.None);

        // Act
        await engine.PauseAsync(CancellationToken.None);

        // Assert
        var status = await engine.GetStatusAsync(CancellationToken.None);
        status.IsPaused.Should().BeTrue();
        status.IsRunning.Should().BeTrue("engine is still running but paused");

        await engine.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ResumeAsync_ClearsPausedStatus()
    {
        // Arrange
        var repository = Substitute.For<IOutboxRepository>();
        var engine = new SyncEngine(repository, pollingIntervalMs: 5000);
        await engine.StartAsync(CancellationToken.None);
        await engine.PauseAsync(CancellationToken.None);

        // Act
        await engine.ResumeAsync(CancellationToken.None);

        // Assert
        var status = await engine.GetStatusAsync(CancellationToken.None);
        status.IsPaused.Should().BeFalse();
        status.IsRunning.Should().BeTrue();

        await engine.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsCorrectPendingCount()
    {
        // Arrange
        var repository = Substitute.For<IOutboxRepository>();
        repository.GetPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<System.Collections.Generic.IReadOnlyList<Domain.Sync.OutboxEntry>>(
                new System.Collections.Generic.List<Domain.Sync.OutboxEntry>
                {
                    Domain.Sync.OutboxEntry.Create("Test", "1", "Insert", "{}"),
                    Domain.Sync.OutboxEntry.Create("Test", "2", "Insert", "{}"),
                    Domain.Sync.OutboxEntry.Create("Test", "3", "Insert", "{}")
                }));

        var engine = new SyncEngine(repository, pollingIntervalMs: 5000);

        // Act
        var status = await engine.GetStatusAsync(CancellationToken.None);

        // Assert
        status.PendingOutboxCount.Should().Be(3);
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyStarted_DoesNotThrow()
    {
        // Arrange
        var repository = Substitute.For<IOutboxRepository>();
        var engine = new SyncEngine(repository, pollingIntervalMs: 5000);
        await engine.StartAsync(CancellationToken.None);

        // Act
        var act = async () => await engine.StartAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync("should be idempotent");

        await engine.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task PauseAsync_WhenNotStarted_DoesNotThrow()
    {
        // Arrange
        var repository = Substitute.For<IOutboxRepository>();
        var engine = new SyncEngine(repository, pollingIntervalMs: 5000);

        // Act
        var act = async () => await engine.PauseAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync("should handle pause when not started");
    }

    [Fact]
    public async Task ResumeAsync_WhenNotPaused_DoesNotThrow()
    {
        // Arrange
        var repository = Substitute.For<IOutboxRepository>();
        var engine = new SyncEngine(repository, pollingIntervalMs: 5000);
        await engine.StartAsync(CancellationToken.None);

        // Act
        var act = async () => await engine.ResumeAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync("should be idempotent");

        await engine.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SyncNowAsync_WhenStopped_DoesNotThrow()
    {
        // Arrange
        var repository = Substitute.For<IOutboxRepository>();
        repository.GetPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<System.Collections.Generic.IReadOnlyList<Domain.Sync.OutboxEntry>>(
                new System.Collections.Generic.List<Domain.Sync.OutboxEntry>()));

        var engine = new SyncEngine(repository, pollingIntervalMs: 5000);

        // Act
        var act = async () => await engine.SyncNowAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync("should allow manual sync when stopped");
    }

    [Fact]
    public async Task GetStatusAsync_WhenNeverStarted_ReturnsStoppedStatus()
    {
        // Arrange
        var repository = Substitute.For<IOutboxRepository>();
        repository.GetPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<System.Collections.Generic.IReadOnlyList<Domain.Sync.OutboxEntry>>(
                new System.Collections.Generic.List<Domain.Sync.OutboxEntry>()));

        var engine = new SyncEngine(repository, pollingIntervalMs: 5000);

        // Act
        var status = await engine.GetStatusAsync(CancellationToken.None);

        // Assert
        status.IsRunning.Should().BeFalse();
        status.IsPaused.Should().BeFalse();
        status.StartedAt.Should().BeNull();
        status.LastSyncAt.Should().BeNull();
        status.TotalProcessed.Should().Be(0);
        status.TotalFailed.Should().Be(0);
    }

    [Fact]
    public async Task Dispose_StopsEngineGracefully()
    {
        // Arrange
        var repository = Substitute.For<IOutboxRepository>();
        var engine = new SyncEngine(repository, pollingIntervalMs: 5000);
        await engine.StartAsync(CancellationToken.None);

        // Act
        engine.Dispose();

        // Assert - should not throw
        var status = await engine.GetStatusAsync(CancellationToken.None);
        status.IsRunning.Should().BeFalse();
    }
}
