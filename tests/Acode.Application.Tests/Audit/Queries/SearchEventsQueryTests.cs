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
/// Tests for SearchEventsQuery and handler.
/// Verifies searching events with multiple filter criteria.
/// </summary>
public sealed class SearchEventsQueryTests
{
    [Fact]
    public async Task Should_SearchWithAllFilters()
    {
        // Arrange
        var repository = Substitute.For<IAuditEventSearchRepository>();
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;
        var eventType = AuditEventType.CommandStart;
        var minSeverity = AuditSeverity.Warning;
        var searchText = "test";

        repository.SearchEventsAsync(
            Arg.Is<DateTimeOffset?>(d => d == fromDate),
            Arg.Is<DateTimeOffset?>(d => d == toDate),
            Arg.Is<AuditEventType?>(t => t == eventType),
            Arg.Is<AuditSeverity?>(s => s == minSeverity),
            Arg.Is<string?>(t => t == searchText),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AuditEvent>>(Array.Empty<AuditEvent>()));

        var handler = new SearchEventsQueryHandler(repository);
        var query = new SearchEventsQuery(fromDate, toDate, eventType, minSeverity, searchText);

        // Act
        await handler.HandleAsync(query);

        // Assert
        await repository.Received(1).SearchEventsAsync(
            Arg.Is<DateTimeOffset?>(d => d == fromDate),
            Arg.Is<DateTimeOffset?>(d => d == toDate),
            Arg.Is<AuditEventType?>(t => t == eventType),
            Arg.Is<AuditSeverity?>(s => s == minSeverity),
            Arg.Is<string?>(t => t == searchText),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_SearchWithOnlyDateRange()
    {
        // Arrange
        var repository = Substitute.For<IAuditEventSearchRepository>();
        var fromDate = DateTimeOffset.UtcNow.AddDays(-1);
        var toDate = DateTimeOffset.UtcNow;

        repository.SearchEventsAsync(
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<AuditEventType?>(),
            Arg.Any<AuditSeverity?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AuditEvent>>(Array.Empty<AuditEvent>()));

        var handler = new SearchEventsQueryHandler(repository);
        var query = new SearchEventsQuery(fromDate, toDate);

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_SearchWithNoFilters()
    {
        // Arrange
        var repository = Substitute.For<IAuditEventSearchRepository>();
        repository.SearchEventsAsync(
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<AuditEventType?>(),
            Arg.Any<AuditSeverity?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AuditEvent>>(Array.Empty<AuditEvent>()));

        var handler = new SearchEventsQueryHandler(repository);
        var query = new SearchEventsQuery();

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Should_RequireRepository()
    {
        // Arrange & Act
        var action = () => new SearchEventsQueryHandler(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Should_ReturnMatchingEvents()
    {
        // Arrange
        var repository = Substitute.For<IAuditEventSearchRepository>();
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
                EventType = AuditEventType.CommandStart,
                Severity = AuditSeverity.Warning,
                Source = "TestSource",
                OperatingMode = "LocalOnly",
                Data = new Dictionary<string, object>(),
            },
        };
        repository.SearchEventsAsync(
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<AuditEventType?>(),
            Arg.Any<AuditSeverity?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AuditEvent>>(events));

        var handler = new SearchEventsQueryHandler(repository);
        var query = new SearchEventsQuery();

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        result.Should().HaveCount(1);
        result[0].EventType.Should().Be(AuditEventType.CommandStart);
    }
}
