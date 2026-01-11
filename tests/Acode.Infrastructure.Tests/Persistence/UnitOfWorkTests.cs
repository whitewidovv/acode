// tests/Acode.Infrastructure.Tests/Persistence/UnitOfWorkTests.cs
#pragma warning disable CA2007 // ConfigureAwait not required in test code

namespace Acode.Infrastructure.Tests.Persistence;

using System.Data;
using Acode.Infrastructure.Persistence.Transactions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for UnitOfWork transaction management.
/// Verifies commit, rollback, and disposal behavior.
/// </summary>
public sealed class UnitOfWorkTests
{
    [Fact]
    public void Constructor_ShouldBeginTransaction_WithSpecifiedIsolationLevel()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();
        connection.BeginTransaction(IsolationLevel.ReadCommitted).Returns(transaction);

        // Act
        var uow = new UnitOfWork(connection, IsolationLevel.ReadCommitted, NullLogger<UnitOfWork>.Instance);

        // Assert
        uow.Connection.Should().BeSameAs(connection);
        uow.Transaction.Should().BeSameAs(transaction);
        connection.Received(1).BeginTransaction(IsolationLevel.ReadCommitted);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenConnectionIsNull()
    {
        // Arrange & Act
        var act = () => new UnitOfWork(null!, IsolationLevel.ReadCommitted, NullLogger<UnitOfWork>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();
        connection.BeginTransaction(Arg.Any<IsolationLevel>()).Returns(transaction);

        // Act
        var act = () => new UnitOfWork(connection, IsolationLevel.ReadCommitted, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task CommitAsync_ShouldCommitTransaction()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();
        connection.BeginTransaction(Arg.Any<IsolationLevel>()).Returns(transaction);

        var uow = new UnitOfWork(connection, IsolationLevel.ReadCommitted, NullLogger<UnitOfWork>.Instance);

        // Act
        await uow.CommitAsync(CancellationToken.None);

        // Assert
        transaction.Received(1).Commit();
    }

    [Fact]
    public async Task CommitAsync_ShouldThrowInvalidOperationException_WhenAlreadyCommitted()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();
        connection.BeginTransaction(Arg.Any<IsolationLevel>()).Returns(transaction);

        var uow = new UnitOfWork(connection, IsolationLevel.ReadCommitted, NullLogger<UnitOfWork>.Instance);
        await uow.CommitAsync(CancellationToken.None);

        // Act
        var act = async () => await uow.CommitAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already been committed or rolled back*");
    }

    [Fact]
    public async Task RollbackAsync_ShouldRollbackTransaction()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();
        connection.BeginTransaction(Arg.Any<IsolationLevel>()).Returns(transaction);

        var uow = new UnitOfWork(connection, IsolationLevel.ReadCommitted, NullLogger<UnitOfWork>.Instance);

        // Act
        await uow.RollbackAsync(CancellationToken.None);

        // Assert
        transaction.Received(1).Rollback();
    }

    [Fact]
    public async Task RollbackAsync_ShouldThrowInvalidOperationException_WhenAlreadyRolledBack()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();
        connection.BeginTransaction(Arg.Any<IsolationLevel>()).Returns(transaction);

        var uow = new UnitOfWork(connection, IsolationLevel.ReadCommitted, NullLogger<UnitOfWork>.Instance);
        await uow.RollbackAsync(CancellationToken.None);

        // Act
        var act = async () => await uow.RollbackAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already been committed or rolled back*");
    }

    [Fact]
    public async Task DisposeAsync_ShouldRollbackTransaction_WhenNotCompleted()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();
        connection.BeginTransaction(Arg.Any<IsolationLevel>()).Returns(transaction);

        var uow = new UnitOfWork(connection, IsolationLevel.ReadCommitted, NullLogger<UnitOfWork>.Instance);

        // Act
        await uow.DisposeAsync();

        // Assert
        transaction.Received(1).Rollback();
        transaction.Received(1).Dispose();
    }

    [Fact]
    public async Task DisposeAsync_ShouldNotRollback_WhenAlreadyCommitted()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();
        connection.BeginTransaction(Arg.Any<IsolationLevel>()).Returns(transaction);

        var uow = new UnitOfWork(connection, IsolationLevel.ReadCommitted, NullLogger<UnitOfWork>.Instance);
        await uow.CommitAsync(CancellationToken.None);

        // Act
        await uow.DisposeAsync();

        // Assert
        transaction.Received(1).Commit(); // From CommitAsync
        transaction.DidNotReceive().Rollback(); // Should not rollback after commit
        transaction.Received(1).Dispose();
    }

    [Fact]
    public async Task DisposeAsync_ShouldDisposeConnection()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();
        connection.BeginTransaction(Arg.Any<IsolationLevel>()).Returns(transaction);

        var uow = new UnitOfWork(connection, IsolationLevel.ReadCommitted, NullLogger<UnitOfWork>.Instance);

        // Act
        await uow.DisposeAsync();

        // Assert
        connection.Received(1).Dispose();
    }

    [Fact]
    public async Task DisposeAsync_ShouldBeIdempotent()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();
        connection.BeginTransaction(Arg.Any<IsolationLevel>()).Returns(transaction);

        var uow = new UnitOfWork(connection, IsolationLevel.ReadCommitted, NullLogger<UnitOfWork>.Instance);

        // Act
        await uow.DisposeAsync();
        await uow.DisposeAsync(); // Second dispose should be safe

        // Assert - Dispose should only be called once
        transaction.Received(1).Dispose();
        connection.Received(1).Dispose();
    }

    [Fact]
    public async Task CommitAsync_ShouldThrowDatabaseException_WhenTransactionCommitFails()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();
        connection.BeginTransaction(Arg.Any<IsolationLevel>()).Returns(transaction);
        transaction.When(t => t.Commit()).Do(_ => throw new InvalidOperationException("Commit failed"));

        var uow = new UnitOfWork(connection, IsolationLevel.ReadCommitted, NullLogger<UnitOfWork>.Instance);

        // Act
        var act = async () => await uow.CommitAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Domain.Exceptions.DatabaseException>()
            .Where(ex => ex.ErrorCode == "ACODE-DB-ACC-003" && ex.Message.Contains("commit"));
    }

    [Fact]
    public async Task RollbackAsync_ShouldThrowDatabaseException_WhenTransactionRollbackFails()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        var transaction = Substitute.For<IDbTransaction>();
        connection.BeginTransaction(Arg.Any<IsolationLevel>()).Returns(transaction);
        transaction.When(t => t.Rollback()).Do(_ => throw new InvalidOperationException("Rollback failed"));

        var uow = new UnitOfWork(connection, IsolationLevel.ReadCommitted, NullLogger<UnitOfWork>.Instance);

        // Act
        var act = async () => await uow.RollbackAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Domain.Exceptions.DatabaseException>()
            .Where(ex => ex.ErrorCode == "ACODE-DB-ACC-003" && ex.Message.Contains("rollback"));
    }
}
