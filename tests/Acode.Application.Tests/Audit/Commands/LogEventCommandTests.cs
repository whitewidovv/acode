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
/// Tests for LogEventCommand and handler.
/// Verifies event logging functionality.
/// </summary>
public sealed class LogEventCommandTests
{
    [Fact]
    public async Task Should_LogEvent()
    {
        // Arrange
        var logger = Substitute.For<IAuditLogger>();
        var handler = new LogEventCommandHandler(logger);
        var eventData = new Dictionary<string, object> { ["key"] = "value" };
        var command = new LogEventCommand(
            AuditEventType.FileRead,
            AuditSeverity.Info,
            "file-service",
            eventData);

        // Act
        await handler.HandleAsync(command);

        // Assert
        await logger.Received(1).LogAsync(
            Arg.Is<AuditEventType>(t => t == AuditEventType.FileRead),
            Arg.Is<AuditSeverity>(s => s == AuditSeverity.Info),
            Arg.Is<string>(src => src == "file-service"),
            Arg.Is<IDictionary<string, object>>(d => d == eventData),
            Arg.Any<IDictionary<string, object>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_PassContext()
    {
        // Arrange
        var logger = Substitute.For<IAuditLogger>();
        var handler = new LogEventCommandHandler(logger);
        var eventData = new Dictionary<string, object> { ["file"] = "test.txt" };
        var context = new Dictionary<string, object> { ["user"] = "testuser" };
        var command = new LogEventCommand(
            AuditEventType.FileWrite,
            AuditSeverity.Info,
            "file-service",
            eventData,
            context);

        // Act
        await handler.HandleAsync(command);

        // Assert
        await logger.Received(1).LogAsync(
            Arg.Any<AuditEventType>(),
            Arg.Any<AuditSeverity>(),
            Arg.Any<string>(),
            Arg.Any<IDictionary<string, object>>(),
            Arg.Is<IDictionary<string, object>?>(c => c == context),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Should_RequireEventData()
    {
        // Arrange & Act
        var action = () => new LogEventCommand(
            AuditEventType.FileRead,
            AuditSeverity.Info,
            "source",
            null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Should_RequireSource()
    {
        // Arrange
        var eventData = new Dictionary<string, object>();

        // Act
        var action = () => new LogEventCommand(
            AuditEventType.FileRead,
            AuditSeverity.Info,
            null!,
            eventData);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Should_HandleNullContext()
    {
        // Arrange
        var logger = Substitute.For<IAuditLogger>();
        var handler = new LogEventCommandHandler(logger);
        var eventData = new Dictionary<string, object> { ["test"] = "value" };
        var command = new LogEventCommand(
            AuditEventType.CommandStart,
            AuditSeverity.Debug,
            "cli",
            eventData,
            context: null);

        // Act
        await handler.HandleAsync(command);

        // Assert
        await logger.Received(1).LogAsync(
            Arg.Any<AuditEventType>(),
            Arg.Any<AuditSeverity>(),
            Arg.Any<string>(),
            Arg.Any<IDictionary<string, object>>(),
            Arg.Is<IDictionary<string, object>?>(c => c == null),
            Arg.Any<CancellationToken>());
    }
}
