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
/// Tests for GetSessionEventsQuery and handler.
/// Verifies retrieval of events for a specific session.
/// </summary>
public sealed class GetSessionEventsQueryTests
{
    [Fact]
    public async Task Should_ReturnSessionEvents()
    {
        // Arrange
        var repository = Substitute.For<IAuditSessionRepository>();
        var sessionId = SessionId.New();
        var events = new[]
        {
            new AuditEvent
            {
                SchemaVersion = "1.0",
                EventId = EventId.New(),
                Timestamp = DateTimeOffset.UtcNow,
                SessionId = sessionId,
                CorrelationId = CorrelationId.New(),
                EventType = AuditEventType.SessionStart,
                Severity = AuditSeverity.Info,
                Source = "TestSource",
                OperatingMode = "LocalOnly",
                Data = new Dictionary<string, object>(),
            },
            new AuditEvent
            {
                SchemaVersion = "1.0",
                EventId = EventId.New(),
                Timestamp = DateTimeOffset.UtcNow.AddSeconds(5),
                SessionId = sessionId,
                CorrelationId = CorrelationId.New(),
                EventType = AuditEventType.CommandStart,
                Severity = AuditSeverity.Info,
                Source = "TestSource",
                OperatingMode = "LocalOnly",
                Data = new Dictionary<string, object>(),
            },
        };
        repository.GetSessionEventsAsync(
            Arg.Is<SessionId>(s => s == sessionId),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AuditEvent>>(events));

        var handler = new GetSessionEventsQueryHandler(repository);
        var query = new GetSessionEventsQuery(sessionId);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Should().HaveCount(2);
        result.All(e => e.SessionId == sessionId).Should().BeTrue();
    }

    [Fact]
    public async Task Should_ReturnEmptyList_WhenNoEvents()
    {
        // Arrange
        var repository = Substitute.For<IAuditSessionRepository>();
        var sessionId = SessionId.New();
        repository.GetSessionEventsAsync(
            Arg.Any<SessionId>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AuditEvent>>(Array.Empty<AuditEvent>()));

        var handler = new GetSessionEventsQueryHandler(repository);
        var query = new GetSessionEventsQuery(sessionId);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Should_RequireSessionId()
    {
        // Arrange & Act
        var action = () => new GetSessionEventsQuery(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Should_RequireRepository()
    {
        // Arrange & Act
        var action = () => new GetSessionEventsQueryHandler(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Should_OrderEventsByTimestamp()
    {
        // Arrange
        var repository = Substitute.For<IAuditSessionRepository>();
        var sessionId = SessionId.New();
        var now = DateTimeOffset.UtcNow;
        var events = new[]
        {
            new AuditEvent
            {
                SchemaVersion = "1.0",
                EventId = EventId.New(),
                Timestamp = now.AddSeconds(-10),
                SessionId = sessionId,
                CorrelationId = CorrelationId.New(),
                EventType = AuditEventType.SessionStart,
                Severity = AuditSeverity.Info,
                Source = "TestSource",
                OperatingMode = "LocalOnly",
                Data = new Dictionary<string, object>(),
            },
            new AuditEvent
            {
                SchemaVersion = "1.0",
                EventId = EventId.New(),
                Timestamp = now,
                SessionId = sessionId,
                CorrelationId = CorrelationId.New(),
                EventType = AuditEventType.CommandStart,
                Severity = AuditSeverity.Info,
                Source = "TestSource",
                OperatingMode = "LocalOnly",
                Data = new Dictionary<string, object>(),
            },
        };
        repository.GetSessionEventsAsync(
            Arg.Any<SessionId>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AuditEvent>>(events));

        var handler = new GetSessionEventsQueryHandler(repository);
        var query = new GetSessionEventsQuery(sessionId);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Should().HaveCount(2);
        result.ElementAt(0).Timestamp.Should().Be(now.AddSeconds(-10));
        result.ElementAt(1).Timestamp.Should().Be(now);
    }
}
