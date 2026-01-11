namespace Acode.Application.Tests.Audit.Commands;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Audit;
using Acode.Application.Audit.Commands;
using Acode.Domain.Audit;
using FluentAssertions;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for EndAuditSessionCommand and handler.
/// Verifies session ending and finalization.
/// </summary>
public sealed class EndAuditSessionCommandTests
{
    [Fact]
    public async Task Should_EndSession()
    {
        // Arrange
        var logger = Substitute.For<IAuditLogger>();
        var sessionId = SessionId.New();
        var session = new AuditSession(sessionId, "local_only");
        var handler = new EndAuditSessionCommandHandler(logger);
        var command = new EndAuditSessionCommand(session, "audit-service");

        // Act
        await handler.HandleAsync(command);

        // Assert
        session.IsActive.Should().BeFalse();
        session.EndedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_LogSessionEndEvent()
    {
        // Arrange
        var logger = Substitute.For<IAuditLogger>();
        var sessionId = SessionId.New();
        var session = new AuditSession(sessionId, "local_only");
        var handler = new EndAuditSessionCommandHandler(logger);
        var command = new EndAuditSessionCommand(session, "audit-service");

        // Act
        await handler.HandleAsync(command);

        // Assert
        await logger.Received(1).LogAsync(
            Arg.Is<AuditEventType>(t => t == AuditEventType.SessionEnd),
            Arg.Any<AuditSeverity>(),
            Arg.Is<string>(s => s == "audit-service"),
            Arg.Any<IDictionary<string, object>>(),
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_IncludeSessionMetricsInEventData()
    {
        // Arrange
        var logger = Substitute.For<IAuditLogger>();
        IDictionary<string, object>? capturedData = null;

        logger.LogAsync(
            Arg.Any<AuditEventType>(),
            Arg.Any<AuditSeverity>(),
            Arg.Any<string>(),
            Arg.Do<IDictionary<string, object>>(d => capturedData = d),
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var sessionId = SessionId.New();
        var session = new AuditSession(sessionId, "local_only");
        session.RecordEvent();
        session.RecordEvent();

        var handler = new EndAuditSessionCommandHandler(logger);
        var command = new EndAuditSessionCommand(session, "test");

        // Act
        await handler.HandleAsync(command);

        // Assert
        capturedData.Should().NotBeNull();
        capturedData.Should().ContainKey("session_id");
        capturedData.Should().ContainKey("event_count");
        capturedData.Should().ContainKey("duration_seconds");
        capturedData!["event_count"].Should().Be(2);
    }

    [Fact]
    public void Should_RequireSession()
    {
        // Arrange & Act
        var action = () => new EndAuditSessionCommand(null!, "source");

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Should_RequireSource()
    {
        // Arrange
        var sessionId = SessionId.New();
        var session = new AuditSession(sessionId, "local_only");

        // Act
        var action = () => new EndAuditSessionCommand(session, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Should_ThrowIfSessionAlreadyEnded()
    {
        // Arrange
        var logger = Substitute.For<IAuditLogger>();
        var sessionId = SessionId.New();
        var session = new AuditSession(sessionId, "local_only");
        session.End(); // End it first

        var handler = new EndAuditSessionCommandHandler(logger);
        var command = new EndAuditSessionCommand(session, "test");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(command));
    }
}
