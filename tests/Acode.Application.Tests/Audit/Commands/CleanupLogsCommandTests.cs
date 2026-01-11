namespace Acode.Application.Tests.Audit.Commands;

using System;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Audit;
using Acode.Application.Audit.Commands;
using FluentAssertions;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for CleanupLogsCommand and handler.
/// Verifies log cleanup and retention enforcement.
/// </summary>
public sealed class CleanupLogsCommandTests
{
    [Fact]
    public async Task Should_CleanupExpiredLogs()
    {
        // Arrange
        var cleanupService = Substitute.For<ILogCleanupService>();
        var handler = new CleanupLogsCommandHandler(cleanupService);
        var command = new CleanupLogsCommand("/logs", retentionDays: 30);

        // Act
        await handler.HandleAsync(command);

        // Assert
        await cleanupService.Received(1).CleanupExpiredLogsAsync(
            Arg.Is<string>(p => p == "/logs"),
            Arg.Is<int>(d => d == 30),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_EnforceStorageLimits()
    {
        // Arrange
        var cleanupService = Substitute.For<ILogCleanupService>();
        var handler = new CleanupLogsCommandHandler(cleanupService);
        var command = new CleanupLogsCommand("/logs", retentionDays: 90);

        // Act
        await handler.HandleAsync(command);

        // Assert
        await cleanupService.Received(1).EnforceStorageLimitAsync(
            Arg.Is<string>(p => p == "/logs"),
            Arg.Any<long>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Should_RequireLogDirectory()
    {
        // Arrange & Act
        var action = () => new CleanupLogsCommand(null!, retentionDays: 30);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Should_RequirePositiveRetentionDays()
    {
        // Arrange & Act
        var action = () => new CleanupLogsCommand("/logs", retentionDays: 0);

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task Should_ReturnCleanupStatistics()
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
            Arg.Any<CancellationToken>()).Returns(Task.FromResult(2L * 1024 * 1024));

        var handler = new CleanupLogsCommandHandler(cleanupService);
        var command = new CleanupLogsCommand("/logs", retentionDays: 30);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.FilesDeleted.Should().Be(5);
        result.BytesFreed.Should().Be(2L * 1024 * 1024);
    }
}
