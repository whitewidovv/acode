namespace Acode.Application.Tests.Audit.Queries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Audit;
using Acode.Application.Audit.Queries;
using Acode.Domain.Audit;
using FluentAssertions;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for ListSessionsQuery and handler.
/// Verifies session listing and filtering.
/// </summary>
public sealed class ListSessionsQueryTests
{
    [Fact]
    public async Task Should_ReturnAllSessions()
    {
        // Arrange
        var repository = Substitute.For<IAuditSessionRepository>();
        var sessions = new[]
        {
            new AuditSessionInfo(SessionId.New(), "LocalOnly", DateTimeOffset.UtcNow),
            new AuditSessionInfo(SessionId.New(), "Burst", DateTimeOffset.UtcNow.AddMinutes(-5)),
        };
        repository.GetSessionsAsync(
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AuditSessionInfo>>(sessions));

        var handler = new ListSessionsQueryHandler(repository);
        var query = new ListSessionsQuery();

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(s => s.OperatingMode == "LocalOnly");
        result.Should().Contain(s => s.OperatingMode == "Burst");
    }

    [Fact]
    public async Task Should_FilterByDateRange()
    {
        // Arrange
        var repository = Substitute.For<IAuditSessionRepository>();
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;

        repository.GetSessionsAsync(
            Arg.Is<DateTimeOffset?>(d => d == fromDate),
            Arg.Is<DateTimeOffset?>(d => d == toDate),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AuditSessionInfo>>(Array.Empty<AuditSessionInfo>()));

        var handler = new ListSessionsQueryHandler(repository);
        var query = new ListSessionsQuery(fromDate, toDate);

        // Act
        await handler.HandleAsync(query);

        // Assert
        await repository.Received(1).GetSessionsAsync(
            Arg.Is<DateTimeOffset?>(d => d == fromDate),
            Arg.Is<DateTimeOffset?>(d => d == toDate),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnEmptyList_WhenNoSessions()
    {
        // Arrange
        var repository = Substitute.For<IAuditSessionRepository>();
        repository.GetSessionsAsync(
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AuditSessionInfo>>(Array.Empty<AuditSessionInfo>()));

        var handler = new ListSessionsQueryHandler(repository);
        var query = new ListSessionsQuery();

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Should_RequireRepository()
    {
        // Arrange & Act
        var action = () => new ListSessionsQueryHandler(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Should_OrderSessionsByStartTime()
    {
        // Arrange
        var repository = Substitute.For<IAuditSessionRepository>();
        var now = DateTimeOffset.UtcNow;
        var sessions = new[]
        {
            new AuditSessionInfo(SessionId.New(), "LocalOnly", now.AddMinutes(-10)),
            new AuditSessionInfo(SessionId.New(), "Burst", now.AddMinutes(-5)),
            new AuditSessionInfo(SessionId.New(), "Airgapped", now),
        };
        repository.GetSessionsAsync(
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AuditSessionInfo>>(sessions));

        var handler = new ListSessionsQueryHandler(repository);
        var query = new ListSessionsQuery();

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Should().HaveCount(3);
        result.ElementAt(0).StartedAt.Should().Be(now.AddMinutes(-10));
        result.ElementAt(1).StartedAt.Should().Be(now.AddMinutes(-5));
        result.ElementAt(2).StartedAt.Should().Be(now);
    }
}
