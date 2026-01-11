namespace Acode.Application.Tests.Audit.Commands;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acode.Application.Audit;
using Acode.Application.Audit.Commands;
using Acode.Domain.Audit;
using FluentAssertions;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for StartAuditSessionCommand and handler.
/// Verifies session creation and initialization.
/// </summary>
public sealed class StartAuditSessionCommandTests
{
    [Fact]
    public async Task Should_CreateNewSession()
    {
        // Arrange
        var logger = Substitute.For<IAuditLogger>();
        var handler = new StartAuditSessionCommandHandler(logger);
        var command = new StartAuditSessionCommand("local_only", "audit-service");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().NotBeNull();
        result.SessionId.Value.Should().StartWith("sess_");
    }

    [Fact]
    public async Task Should_LogSessionStartEvent()
    {
        // Arrange
        var logger = Substitute.For<IAuditLogger>();
        var handler = new StartAuditSessionCommandHandler(logger);
        var command = new StartAuditSessionCommand("local_only", "audit-service");

        // Act
        await handler.HandleAsync(command);

        // Assert
        await logger.Received(1).LogAsync(
            Arg.Is<AuditEventType>(t => t == AuditEventType.SessionStart),
            Arg.Any<AuditSeverity>(),
            Arg.Is<string>(s => s == "audit-service"),
            Arg.Any<IDictionary<string, object>>(),
            Arg.Any<IDictionary<string, object>?>());
    }

    [Fact]
    public async Task Should_IncludeSessionIdInEventData()
    {
        // Arrange
        var logger = Substitute.For<IAuditLogger>();
        IDictionary<string, object>? capturedData = null;

        logger.LogAsync(
            Arg.Any<AuditEventType>(),
            Arg.Any<AuditSeverity>(),
            Arg.Any<string>(),
            Arg.Do<IDictionary<string, object>>(d => capturedData = d),
            Arg.Any<IDictionary<string, object>?>()).Returns(Task.CompletedTask);

        var handler = new StartAuditSessionCommandHandler(logger);
        var command = new StartAuditSessionCommand("local_only", "test");

        // Act
        await handler.HandleAsync(command);

        // Assert
        capturedData.Should().NotBeNull();
        capturedData.Should().ContainKey("session_id");
    }

    [Fact]
    public void Should_RequireOperatingMode()
    {
        // Arrange & Act
        var action = () => new StartAuditSessionCommand(null!, "source");

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Should_RequireSource()
    {
        // Arrange & Act
        var action = () => new StartAuditSessionCommand("local_only", null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Should_ReturnSessionWithTimestamp()
    {
        // Arrange
        var logger = Substitute.For<IAuditLogger>();
        var handler = new StartAuditSessionCommandHandler(logger);
        var command = new StartAuditSessionCommand("local_only", "test");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.StartedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Should_SetSessionOperatingModeFromCommand()
    {
        // Arrange
        var logger = Substitute.For<IAuditLogger>();
        var handler = new StartAuditSessionCommandHandler(logger);
        var command = new StartAuditSessionCommand("burst", "test-source");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.OperatingMode.Should().Be("burst");
    }
}
