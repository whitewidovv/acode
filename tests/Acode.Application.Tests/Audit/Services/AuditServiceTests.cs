namespace Acode.Application.Tests.Audit.Services;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Audit;
using Acode.Application.Audit.Commands;
using Acode.Application.Audit.Services;
using Acode.Domain.Audit;
using FluentAssertions;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for AuditService.
/// Verifies orchestration of audit operations and session lifecycle.
/// </summary>
public sealed class AuditServiceTests
{
    [Fact]
    public async Task Should_StartSession()
    {
        // Arrange
        var logger = Substitute.For<IAuditLogger>();
        var startHandler = new StartAuditSessionCommandHandler(logger);
        var service = new AuditService(startHandler);

        // Act
        var session = await service.StartSessionAsync("LocalOnly", "TestSource");

        // Assert
        session.Should().NotBeNull();
        session.OperatingMode.Should().Be("LocalOnly");
        await logger.Received(1).LogAsync(
            Arg.Any<AuditEventType>(),
            Arg.Any<AuditSeverity>(),
            Arg.Any<string>(),
            Arg.Any<IDictionary<string, object>>(),
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_EndSession()
    {
        // Arrange
        var logger = Substitute.For<IAuditLogger>();
        var startHandler = new StartAuditSessionCommandHandler(logger);
        var endHandler = new EndAuditSessionCommandHandler(logger);
        var service = new AuditService(startHandler, endHandler);

        var session = new AuditSession(SessionId.New(), "LocalOnly");

        // Act
        await service.EndSessionAsync(session, "TestSource");

        // Assert
        await logger.Received(1).LogAsync(
            Arg.Is<AuditEventType>(t => t == AuditEventType.SessionEnd),
            Arg.Any<AuditSeverity>(),
            Arg.Any<string>(),
            Arg.Any<IDictionary<string, object>>(),
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_LogEvent()
    {
        // Arrange
        var logger = Substitute.For<IAuditLogger>();
        var logEventHandler = new LogEventCommandHandler(logger);
        var service = new AuditService(logEventHandler: logEventHandler);

        var eventData = new Dictionary<string, object> { ["test"] = "value" };

        // Act
        await service.LogEventAsync(
            AuditEventType.CommandStart,
            AuditSeverity.Info,
            "TestSource",
            eventData);

        // Assert
        await logger.Received(1).LogAsync(
            Arg.Is<AuditEventType>(t => t == AuditEventType.CommandStart),
            Arg.Is<AuditSeverity>(s => s == AuditSeverity.Info),
            Arg.Is<string>(src => src == "TestSource"),
            Arg.Any<IDictionary<string, object>>(),
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_CleanupLogs()
    {
        // Arrange
        var cleanupService = Substitute.For<ILogCleanupService>();
        cleanupService.CleanupExpiredLogsAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult(5));
        cleanupService.EnforceStorageLimitAsync(
            Arg.Any<string>(),
            Arg.Any<long>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult(1024L * 1024));

        var cleanupHandler = new CleanupLogsCommandHandler(cleanupService);
        var service = new AuditService(cleanupHandler: cleanupHandler);

        // Act
        var result = await service.CleanupLogsAsync("/logs", retentionDays: 30);

        // Assert
        result.Should().NotBeNull();
        result.FilesDeleted.Should().Be(5);
        result.BytesFreed.Should().Be(1024L * 1024);
    }

    [Fact]
    public void Should_RequireStartHandler()
    {
        // Arrange
        var service = new AuditService();

        // Act & Assert
        var action = async () => await service.StartSessionAsync("LocalOnly", "TestSource");
        action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Should_PropagateCorrelationId()
    {
        // Arrange
        var logger = Substitute.For<IAuditLogger>();
        var startHandler = new StartAuditSessionCommandHandler(logger);
        var service = new AuditService(startHandler);

        // Act
        var session = await service.StartSessionAsync("LocalOnly", "TestSource");

        // Assert
        await logger.Received(1).LogAsync(
            Arg.Any<AuditEventType>(),
            Arg.Any<AuditSeverity>(),
            Arg.Any<string>(),
            Arg.Any<IDictionary<string, object>>(),
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_TrackActiveSession()
    {
        // Arrange
        var logger = Substitute.For<IAuditLogger>();
        var startHandler = new StartAuditSessionCommandHandler(logger);
        var endHandler = new EndAuditSessionCommandHandler(logger);
        var service = new AuditService(startHandler, endHandler);

        // Act
        var session = await service.StartSessionAsync("LocalOnly", "TestSource");
        var activeSessionId = service.GetActiveSessionId();

        // Assert
        activeSessionId.Should().Be(session.SessionId);
    }

    [Fact]
    public async Task Should_ClearActiveSession_WhenEnded()
    {
        // Arrange
        var logger = Substitute.For<IAuditLogger>();
        var startHandler = new StartAuditSessionCommandHandler(logger);
        var endHandler = new EndAuditSessionCommandHandler(logger);
        var service = new AuditService(startHandler, endHandler);

        var session = await service.StartSessionAsync("LocalOnly", "TestSource");

        // Act
        await service.EndSessionAsync(session, "TestSource");
        var activeSessionId = service.GetActiveSessionId();

        // Assert
        activeSessionId.Should().BeNull();
    }
}
