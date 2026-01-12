namespace Acode.Application.Tests.Audit.Queries;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Audit;
using Acode.Application.Audit.Queries;
using Acode.Domain.Audit;
using FluentAssertions;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for GetAuditStatsQuery and handler.
/// Verifies retrieval of audit statistics.
/// </summary>
public sealed class GetAuditStatsQueryTests
{
    [Fact]
    public async Task Should_ReturnStatistics()
    {
        // Arrange
        var repository = Substitute.For<IAuditStatsRepository>();
        var stats = new AuditStatistics
        {
            TotalSessions = 10,
            TotalEvents = 150,
            EventsByType = new Dictionary<AuditEventType, int>
            {
                [AuditEventType.SessionStart] = 10,
                [AuditEventType.CommandStart] = 100,
                [AuditEventType.SessionEnd] = 10,
            },
            EventsBySeverity = new Dictionary<AuditSeverity, int>
            {
                [AuditSeverity.Info] = 120,
                [AuditSeverity.Warning] = 20,
                [AuditSeverity.Error] = 10,
            },
            OldestEventTimestamp = DateTimeOffset.UtcNow.AddDays(-30),
            NewestEventTimestamp = DateTimeOffset.UtcNow,
        };
        repository.GetStatisticsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(stats));

        var handler = new GetAuditStatsQueryHandler(repository);
        var query = new GetAuditStatsQuery();

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.TotalSessions.Should().Be(10);
        result.TotalEvents.Should().Be(150);
        result.EventsByType.Should().HaveCount(3);
        result.EventsBySeverity.Should().HaveCount(3);
    }

    [Fact]
    public async Task Should_ReturnEmptyStats_WhenNoData()
    {
        // Arrange
        var repository = Substitute.For<IAuditStatsRepository>();
        var emptyStats = new AuditStatistics
        {
            TotalSessions = 0,
            TotalEvents = 0,
            EventsByType = new Dictionary<AuditEventType, int>(),
            EventsBySeverity = new Dictionary<AuditSeverity, int>(),
            OldestEventTimestamp = null,
            NewestEventTimestamp = null,
        };
        repository.GetStatisticsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(emptyStats));

        var handler = new GetAuditStatsQueryHandler(repository);
        var query = new GetAuditStatsQuery();

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.TotalSessions.Should().Be(0);
        result.TotalEvents.Should().Be(0);
    }

    [Fact]
    public void Should_RequireRepository()
    {
        // Arrange & Act
        var action = () => new GetAuditStatsQueryHandler(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Should_IncludeTimestampRange()
    {
        // Arrange
        var repository = Substitute.For<IAuditStatsRepository>();
        var oldest = DateTimeOffset.UtcNow.AddDays(-90);
        var newest = DateTimeOffset.UtcNow;
        var stats = new AuditStatistics
        {
            TotalSessions = 5,
            TotalEvents = 50,
            EventsByType = new Dictionary<AuditEventType, int>(),
            EventsBySeverity = new Dictionary<AuditSeverity, int>(),
            OldestEventTimestamp = oldest,
            NewestEventTimestamp = newest,
        };
        repository.GetStatisticsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(stats));

        var handler = new GetAuditStatsQueryHandler(repository);
        var query = new GetAuditStatsQuery();

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.OldestEventTimestamp.Should().Be(oldest);
        result.NewestEventTimestamp.Should().Be(newest);
    }
}
