// tests/Acode.Infrastructure.Tests/Persistence/Migrations/PostgreSqlAdvisoryLockTests.cs
#pragma warning disable CA2007 // ConfigureAwait not required in test code

namespace Acode.Infrastructure.Tests.Persistence.Migrations;

using System.Data;
using Acode.Infrastructure.Persistence.Migrations;
using FluentAssertions;
using NSubstitute;
using Xunit;

/// <summary>
/// Tests for PostgreSqlAdvisoryLock class.
/// Verifies PostgreSQL advisory lock acquisition and release.
/// </summary>
public sealed class PostgreSqlAdvisoryLockTests
{
    private readonly IDbConnection _connectionMock;
    private readonly IDbCommand _commandMock;
    private readonly IDataReader _readerMock;
    private readonly PostgreSqlAdvisoryLock _sut;

    public PostgreSqlAdvisoryLockTests()
    {
        _connectionMock = Substitute.For<IDbConnection>();
        _commandMock = Substitute.For<IDbCommand>();
        _readerMock = Substitute.For<IDataReader>();

        _connectionMock.CreateCommand().Returns(_commandMock);
        _commandMock.ExecuteReader().Returns(_readerMock);

        _sut = new PostgreSqlAdvisoryLock(_connectionMock, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task TryAcquireAsync_WhenLockAvailable_ReturnsTrue()
    {
        // Arrange
        _readerMock.Read().Returns(true);
        _readerMock.GetBoolean(0).Returns(true);

        // Act
        var result = await _sut.TryAcquireAsync();

        // Assert
        result.Should().BeTrue();
        _commandMock.Received(1).ExecuteReader();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenLockHeld_ReturnsFalse()
    {
        // Arrange - pg_try_advisory_lock returns false
        _readerMock.Read().Returns(true);
        _readerMock.GetBoolean(0).Returns(false);

        // Act
        var result = await _sut.TryAcquireAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryAcquireAsync_UsesCorrectLockId()
    {
        // Arrange
        _readerMock.Read().Returns(true);
        _readerMock.GetBoolean(0).Returns(true);

        // Act
        await _sut.TryAcquireAsync();

        // Assert
        _commandMock.Received().CommandText = Arg.Is<string>(s => s.Contains("pg_try_advisory_lock"));
    }

    [Fact]
    public async Task DisposeAsync_ReleasesLock()
    {
        // Arrange
        _readerMock.Read().Returns(true);
        _readerMock.GetBoolean(0).Returns(true);
        await _sut.TryAcquireAsync();

        // Reset call count
        _commandMock.ClearReceivedCalls();

        // Act
        await _sut.DisposeAsync();

        // Assert
        _commandMock.Received().CommandText = Arg.Is<string>(s => s.Contains("pg_advisory_unlock"));
        _commandMock.Received(1).ExecuteNonQuery();
    }

    [Fact]
    public async Task DisposeAsync_WhenLockNotAcquired_DoesNotAttemptRelease()
    {
        // Arrange - lock was never acquired
        _readerMock.Read().Returns(true);
        _readerMock.GetBoolean(0).Returns(false);
        await _sut.TryAcquireAsync();

        _commandMock.ClearReceivedCalls();

        // Act
        await _sut.DisposeAsync();

        // Assert
        _commandMock.DidNotReceive().ExecuteNonQuery();
    }

    [Fact]
    public async Task GetLockInfoAsync_WhenLockAcquired_ReturnsInfo()
    {
        // Arrange
        _readerMock.Read().Returns(true);
        _readerMock.GetBoolean(0).Returns(true);
        await _sut.TryAcquireAsync();

        // Act
        var lockInfo = await _sut.GetLockInfoAsync();

        // Assert
        lockInfo.Should().NotBeNull();
        lockInfo!.LockId.Should().NotBeNullOrEmpty();
        lockInfo.HolderId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetLockInfoAsync_WhenNoLock_ReturnsNull()
    {
        // Act
        var lockInfo = await _sut.GetLockInfoAsync();

        // Assert
        lockInfo.Should().BeNull();
    }

    [Fact]
    public async Task ForceReleaseAsync_CallsUnlockEvenIfNotHeld()
    {
        // Act
        await _sut.ForceReleaseAsync();

        // Assert
        _commandMock.Received().CommandText = Arg.Is<string>(s => s.Contains("pg_advisory_unlock_all"));
        _commandMock.Received(1).ExecuteNonQuery();
    }
}
