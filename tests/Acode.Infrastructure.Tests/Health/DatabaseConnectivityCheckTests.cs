// tests/Acode.Infrastructure.Tests/Health/DatabaseConnectivityCheckTests.cs
#pragma warning disable CA2007 // ConfigureAwait not required in test code

namespace Acode.Infrastructure.Tests.Health;

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Health;
using Acode.Infrastructure.Health.Checks;
using FluentAssertions;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for DatabaseConnectivityCheck.
/// Verifies database connectivity health checking.
/// </summary>
public sealed class DatabaseConnectivityCheckTests
{
    [Fact]
    public async Task CheckAsync_WhenDatabaseResponds_ReturnsHealthy()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var command = Substitute.For<IDbCommand>();

        connection.State.Returns(ConnectionState.Open);
        connection.CreateCommand().Returns(command);
        command.ExecuteScalar().Returns(1);

        var check = new DatabaseConnectivityCheck(connection);

        // Act
        var result = await check.CheckAsync(CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Name.Should().Be("Database Connectivity");
        result.Description.Should().Contain("responding");
    }

    [Fact]
    public async Task CheckAsync_WhenConnectionClosed_OpensConnection()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var command = Substitute.For<IDbCommand>();

        connection.State.Returns(ConnectionState.Closed);
        connection.CreateCommand().Returns(command);
        command.ExecuteScalar().Returns(1);

        var check = new DatabaseConnectivityCheck(connection);

        // Act
        await check.CheckAsync(CancellationToken.None);

        // Assert
        connection.Received(1).Open();
    }

    [Fact]
    public async Task CheckAsync_WhenQueryThrowsException_ReturnsUnhealthy()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var command = Substitute.For<IDbCommand>();

        connection.State.Returns(ConnectionState.Open);
        connection.CreateCommand().Returns(command);
        command.ExecuteScalar().Returns(x => throw new InvalidOperationException("Connection timeout"));

        var check = new DatabaseConnectivityCheck(connection);

        // Act
        var result = await check.CheckAsync(CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.ErrorCode.Should().Be("DB_CONNECTION_FAILED");
        result.Description.Should().Contain("Connection timeout");
        result.Suggestion.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CheckAsync_WhenQueryReturnsUnexpectedResult_ReturnsUnhealthy()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var command = Substitute.For<IDbCommand>();

        connection.State.Returns(ConnectionState.Open);
        connection.CreateCommand().Returns(command);
        command.ExecuteScalar().Returns(null);

        var check = new DatabaseConnectivityCheck(connection);

        // Act
        var result = await check.CheckAsync(CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.ErrorCode.Should().Be("DB_UNEXPECTED_RESULT");
    }

    [Fact]
    public async Task CheckAsync_RecordsDuration()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var command = Substitute.For<IDbCommand>();

        connection.State.Returns(ConnectionState.Open);
        connection.CreateCommand().Returns(command);
        command.ExecuteScalar().Returns(1);

        var check = new DatabaseConnectivityCheck(connection);

        // Act
        var result = await check.CheckAsync(CancellationToken.None);

        // Assert
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void Constructor_WithNullConnection_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new DatabaseConnectivityCheck(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Name_ReturnsExpectedValue()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var check = new DatabaseConnectivityCheck(connection);

        // Act & Assert
        check.Name.Should().Be("Database Connectivity");
    }
}
