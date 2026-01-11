// tests/Acode.Infrastructure.Tests/Persistence/Migrations/MigrationExecutorTests.cs
#pragma warning disable CA2007 // ConfigureAwait not required in test code

namespace Acode.Infrastructure.Tests.Persistence.Migrations;

using Acode.Application.Database;
using Acode.Infrastructure.Persistence.Migrations;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

/// <summary>
/// Tests for MigrationExecutor class.
/// Verifies migration execution with transaction support, rollback handling, and timing.
/// </summary>
public sealed class MigrationExecutorTests
{
    private readonly MigrationExecutor _sut;
    private readonly System.Data.IDbConnection _connectionMock;
    private readonly System.Data.IDbTransaction _transactionMock;
    private readonly System.Data.IDbCommand _commandMock;
    private readonly IMigrationRepository _repositoryMock;
    private readonly ILogger<MigrationExecutor> _loggerMock;

    public MigrationExecutorTests()
    {
        _connectionMock = Substitute.For<System.Data.IDbConnection>();
        _transactionMock = Substitute.For<System.Data.IDbTransaction>();
        _commandMock = Substitute.For<System.Data.IDbCommand>();
        _repositoryMock = Substitute.For<IMigrationRepository>();
        _loggerMock = Substitute.For<ILogger<MigrationExecutor>>();

        // Setup connection to return transaction
        _connectionMock.BeginTransaction().Returns(_transactionMock);
        _connectionMock.CreateCommand().Returns(_commandMock);
        _commandMock.Transaction = _transactionMock;

        _sut = new MigrationExecutor(_connectionMock, _repositoryMock, _loggerMock);
    }

    [Fact]
    public async Task ApplyAsync_WithValidMigration_ExecutesSuccessfully()
    {
        // Arrange
        var migration = new MigrationFile
        {
            Version = "001",
            UpContent = "CREATE TABLE test (id INTEGER);",
            Checksum = "abc123",
            Source = MigrationSource.Embedded
        };

        _commandMock.ExecuteNonQuery().Returns(1);

        // Act
        var result = await _sut.ApplyAsync(migration);

        // Assert
        result.Success.Should().BeTrue();
        result.Version.Should().Be("001");
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        result.ErrorMessage.Should().BeNull();

        _transactionMock.Received(1).Commit();
        await _repositoryMock.Received(1).RecordMigrationAsync(
            Arg.Is<AppliedMigration>(m => m.Version == "001" && m.Status == MigrationStatus.Applied),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAsync_WithFailedExecution_RollsBackTransaction()
    {
        // Arrange
        var migration = new MigrationFile
        {
            Version = "002",
            UpContent = "INVALID SQL;",
            Checksum = "def456",
            Source = MigrationSource.File
        };

        _commandMock.ExecuteNonQuery().Throws(new InvalidOperationException("SQL error"));

        // Act
        var result = await _sut.ApplyAsync(migration);

        // Assert
        result.Success.Should().BeFalse();
        result.Version.Should().Be("002");
        result.ErrorMessage.Should().Contain("SQL error");
        result.Exception.Should().NotBeNull();

        _transactionMock.Received(1).Rollback();
        _transactionMock.DidNotReceive().Commit();
        await _repositoryMock.DidNotReceive().RecordMigrationAsync(
            Arg.Any<AppliedMigration>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAsync_RecordsExecutionDuration()
    {
        // Arrange
        var migration = new MigrationFile
        {
            Version = "003",
            UpContent = "CREATE TABLE users (id INTEGER);",
            Checksum = "ghi789",
            Source = MigrationSource.Embedded
        };

        _commandMock.ExecuteNonQuery().Returns(callInfo =>
        {
            Thread.Sleep(50); // Simulate execution time
            return 1;
        });

        // Act
        var result = await _sut.ApplyAsync(migration);

        // Assert
        result.Duration.Should().BeGreaterOrEqualTo(TimeSpan.FromMilliseconds(40));
        await _repositoryMock.Received(1).RecordMigrationAsync(
            Arg.Is<AppliedMigration>(m => m.Duration >= TimeSpan.FromMilliseconds(40)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAsync_StoresCorrectChecksum()
    {
        // Arrange
        var migration = new MigrationFile
        {
            Version = "004",
            UpContent = "CREATE INDEX idx_test ON test(id);",
            Checksum = "checksum-004",
            Source = MigrationSource.File
        };

        // Act
        await _sut.ApplyAsync(migration);

        // Assert
        await _repositoryMock.Received(1).RecordMigrationAsync(
            Arg.Is<AppliedMigration>(m => m.Checksum == "checksum-004"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RollbackAsync_WithValidMigration_ExecutesSuccessfully()
    {
        // Arrange
        var migration = new MigrationFile
        {
            Version = "005",
            UpContent = "CREATE TABLE test (id INTEGER);",
            DownContent = "DROP TABLE test;",
            Checksum = "jkl012",
            Source = MigrationSource.File
        };

        _commandMock.ExecuteNonQuery().Returns(1);

        // Act
        var result = await _sut.RollbackAsync(migration);

        // Assert
        result.Success.Should().BeTrue();
        result.Version.Should().Be("005");

        _transactionMock.Received(1).Commit();
        await _repositoryMock.Received(1).RemoveMigrationAsync("005", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RollbackAsync_WithoutDownScript_ReturnsFailure()
    {
        // Arrange
        var migration = new MigrationFile
        {
            Version = "006",
            UpContent = "CREATE TABLE test (id INTEGER);",
            DownContent = null,
            Checksum = "mno345",
            Source = MigrationSource.Embedded
        };

        // Act
        var result = await _sut.RollbackAsync(migration);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("down script");

        _transactionMock.DidNotReceive().Commit();
        _transactionMock.DidNotReceive().Rollback();
        await _repositoryMock.DidNotReceive().RemoveMigrationAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAsync_ExecutesSQLContent()
    {
        // Arrange
        var migration = new MigrationFile
        {
            Version = "007",
            UpContent = "CREATE TABLE users (id INTEGER);",
            Checksum = "pqr678",
            Source = MigrationSource.File
        };

        // Act
        await _sut.ApplyAsync(migration);

        // Assert
        _commandMock.Received().CommandText = "CREATE TABLE users (id INTEGER);";
        _commandMock.Received(1).ExecuteNonQuery();
    }

    [Fact]
    public async Task RollbackAsync_WithFailedExecution_RollsBackTransaction()
    {
        // Arrange
        var migration = new MigrationFile
        {
            Version = "008",
            UpContent = "CREATE TABLE test (id INTEGER);",
            DownContent = "DROP TABLE test;",
            Checksum = "stu901",
            Source = MigrationSource.File
        };

        _commandMock.ExecuteNonQuery().Throws(new InvalidOperationException("Rollback failed"));

        // Act
        var result = await _sut.RollbackAsync(migration);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Rollback failed");

        _transactionMock.Received(1).Rollback();
        _transactionMock.DidNotReceive().Commit();
        await _repositoryMock.DidNotReceive().RemoveMigrationAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
