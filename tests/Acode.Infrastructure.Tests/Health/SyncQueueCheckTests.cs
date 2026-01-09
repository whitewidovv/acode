// tests/Acode.Infrastructure.Tests/Health/SyncQueueCheckTests.cs
#pragma warning disable CA2007 // ConfigureAwait not required in test code

namespace Acode.Infrastructure.Tests.Health;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Health;
using Acode.Application.Sync;
using Acode.Domain.Sync;
using Acode.Infrastructure.Health.Checks;
using FluentAssertions;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for SyncQueueCheck.
/// Verifies sync queue health checking with queue depth and lag thresholds.
/// </summary>
public sealed class SyncQueueCheckTests
{
    [Fact]
    public async Task CheckAsync_WhenQueueEmpty_ReturnsHealthy()
    {
        // Arrange
        var repository = Substitute.For<IOutboxRepository>();
        repository.GetPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OutboxEntry>>(new List<OutboxEntry>()));

        var check = new SyncQueueCheck(repository);

        // Act
        var result = await check.CheckAsync(CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("empty");
        result.Details.Should().ContainKey("QueueDepth");
        result.Details!["QueueDepth"].Should().Be(0);
    }

    [Fact]
    public async Task CheckAsync_WhenQueueBelowDegradedThreshold_ReturnsHealthy()
    {
        // Arrange
        var repository = Substitute.For<IOutboxRepository>();
        var entries = Enumerable.Range(1, 50)
            .Select(i => OutboxEntry.Create("Test", $"id-{i}", "Insert", "{}"))
            .ToList();

        repository.GetPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OutboxEntry>>(entries));

        var check = new SyncQueueCheck(repository, degradedThreshold: 100, unhealthyThreshold: 500);

        // Act
        var result = await check.CheckAsync(CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("50 entries");
    }

    [Fact]
    public async Task CheckAsync_WhenQueueAtDegradedThreshold_ReturnsDegraded()
    {
        // Arrange
        var repository = Substitute.For<IOutboxRepository>();
        var entries = Enumerable.Range(1, 100)
            .Select(i => OutboxEntry.Create("Test", $"id-{i}", "Insert", "{}"))
            .ToList();

        repository.GetPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OutboxEntry>>(entries));

        var check = new SyncQueueCheck(repository, degradedThreshold: 100, unhealthyThreshold: 500);

        // Act
        var result = await check.CheckAsync(CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("elevated");
        result.Suggestion.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CheckAsync_WhenQueueAtUnhealthyThreshold_ReturnsUnhealthy()
    {
        // Arrange
        var repository = Substitute.For<IOutboxRepository>();
        var entries = Enumerable.Range(1, 500)
            .Select(i => OutboxEntry.Create("Test", $"id-{i}", "Insert", "{}"))
            .ToList();

        repository.GetPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OutboxEntry>>(entries));

        var check = new SyncQueueCheck(repository, degradedThreshold: 100, unhealthyThreshold: 500);

        // Act
        var result = await check.CheckAsync(CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.ErrorCode.Should().Be("SYNC_QUEUE_CRITICAL");
        result.Description.Should().Contain("critically high");
    }

    [Fact]
    public async Task CheckAsync_WhenSyncLagExceedsThreshold_ReturnsUnhealthy()
    {
        // Arrange
        var repository = Substitute.For<IOutboxRepository>();

        // Create an entry from 10 minutes ago (exceeds 5 minute default threshold)
        var oldEntry = OutboxEntry.Create("Test", "old-id", "Insert", "{}");
        var createdAtProperty = typeof(OutboxEntry).GetProperty("CreatedAt");
        createdAtProperty!.SetValue(oldEntry, DateTimeOffset.UtcNow.AddMinutes(-10));

        repository.GetPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OutboxEntry>>(new List<OutboxEntry> { oldEntry }));

        var check = new SyncQueueCheck(repository, lagThreshold: TimeSpan.FromMinutes(5));

        // Act
        var result = await check.CheckAsync(CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.ErrorCode.Should().Be("SYNC_LAG_CRITICAL");
        result.Description.Should().Contain("lag critically high");
        result.Details.Should().ContainKey("SyncLag");
    }

    [Fact]
    public async Task CheckAsync_WhenRepositoryThrowsException_ReturnsUnhealthy()
    {
        // Arrange
        var repository = Substitute.For<IOutboxRepository>();
        repository.GetPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<OutboxEntry>>(x => throw new InvalidOperationException("Database error"));

        var check = new SyncQueueCheck(repository);

        // Act
        var result = await check.CheckAsync(CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.ErrorCode.Should().Be("SYNC_QUEUE_CHECK_FAILED");
        result.Description.Should().Contain("Database error");
    }

    [Fact]
    public async Task CheckAsync_IncludesDetailsInResult()
    {
        // Arrange
        var repository = Substitute.For<IOutboxRepository>();
        var entries = Enumerable.Range(1, 10)
            .Select(i => OutboxEntry.Create("Test", $"id-{i}", "Insert", "{}"))
            .ToList();

        repository.GetPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OutboxEntry>>(entries));

        var check = new SyncQueueCheck(repository);

        // Act
        var result = await check.CheckAsync(CancellationToken.None);

        // Assert
        result.Details.Should().NotBeNull();
        result.Details.Should().ContainKey("QueueDepth");
        result.Details.Should().ContainKey("SyncLag");
        result.Details!["QueueDepth"].Should().Be(10);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SyncQueueCheck(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithInvalidDegradedThreshold_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var repository = Substitute.For<IOutboxRepository>();

        // Act
        var act = () => new SyncQueueCheck(repository, degradedThreshold: 0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithUnhealthyThresholdLessThanDegraded_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var repository = Substitute.For<IOutboxRepository>();

        // Act
        var act = () => new SyncQueueCheck(repository, degradedThreshold: 100, unhealthyThreshold: 50);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Name_ReturnsExpectedValue()
    {
        // Arrange
        var repository = Substitute.For<IOutboxRepository>();
        var check = new SyncQueueCheck(repository);

        // Act & Assert
        check.Name.Should().Be("Sync Queue");
    }

    [Fact]
    public async Task CheckAsync_RecordsDuration()
    {
        // Arrange
        var repository = Substitute.For<IOutboxRepository>();
        repository.GetPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OutboxEntry>>(new List<OutboxEntry>()));

        var check = new SyncQueueCheck(repository);

        // Act
        var result = await check.CheckAsync(CancellationToken.None);

        // Assert
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }
}
