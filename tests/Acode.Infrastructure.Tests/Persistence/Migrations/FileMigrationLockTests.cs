// tests/Acode.Infrastructure.Tests/Persistence/Migrations/FileMigrationLockTests.cs
#pragma warning disable CA2007 // ConfigureAwait not required in test code

namespace Acode.Infrastructure.Tests.Persistence.Migrations;

using Acode.Infrastructure.Persistence.Migrations;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for FileMigrationLock class.
/// Verifies file-based locking for migration concurrency control.
/// </summary>
public sealed class FileMigrationLockTests : IDisposable
{
    private readonly string _testLockDir;
    private readonly FileMigrationLock _sut;

    public FileMigrationLockTests()
    {
        _testLockDir = Path.Combine(Path.GetTempPath(), "acode-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testLockDir);

        var lockFilePath = Path.Combine(_testLockDir, "migrations.lock");
        _sut = new FileMigrationLock(lockFilePath, TimeSpan.FromSeconds(5));
    }

    public void Dispose()
    {
        _sut.DisposeAsync().AsTask().Wait();

        if (Directory.Exists(_testLockDir))
        {
            Directory.Delete(_testLockDir, recursive: true);
        }
    }

    [Fact]
    public async Task TryAcquireAsync_FirstAttempt_ReturnsTrue()
    {
        // Act
        var result = await _sut.TryAcquireAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenLockHeld_ReturnsFalse()
    {
        // Arrange
        var lockFilePath = Path.Combine(_testLockDir, "concurrent.lock");
        var lock1 = new FileMigrationLock(lockFilePath, TimeSpan.FromSeconds(1));
        var lock2 = new FileMigrationLock(lockFilePath, TimeSpan.FromSeconds(1));

        await lock1.TryAcquireAsync();

        // Act
        var result = await lock2.TryAcquireAsync();

        // Assert
        result.Should().BeFalse();

        // Cleanup
        await lock1.DisposeAsync();
        await lock2.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_ReleasesLock()
    {
        // Arrange
        var lockFilePath = Path.Combine(_testLockDir, "disposable.lock");
        var lock1 = new FileMigrationLock(lockFilePath, TimeSpan.FromSeconds(5));
        var lock2 = new FileMigrationLock(lockFilePath, TimeSpan.FromSeconds(5));

        await lock1.TryAcquireAsync();
        await lock1.DisposeAsync();

        // Act
        var result = await lock2.TryAcquireAsync();

        // Assert
        result.Should().BeTrue();

        // Cleanup
        await lock2.DisposeAsync();
    }

    [Fact]
    public async Task GetLockInfoAsync_WhenLockAcquired_ReturnsInfo()
    {
        // Arrange
        await _sut.TryAcquireAsync();

        // Act
        var lockInfo = await _sut.GetLockInfoAsync();

        // Assert
        lockInfo.Should().NotBeNull();
        lockInfo!.LockId.Should().NotBeNullOrEmpty();
        lockInfo.HolderId.Should().NotBeNullOrEmpty();
        lockInfo.AcquiredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
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
    public async Task ForceReleaseAsync_RemovesLock()
    {
        // Arrange
        var lockFilePath = Path.Combine(_testLockDir, "forceable.lock");
        var lock1 = new FileMigrationLock(lockFilePath, TimeSpan.FromSeconds(5));
        var lock2 = new FileMigrationLock(lockFilePath, TimeSpan.FromSeconds(5));

        await lock1.TryAcquireAsync();

        // Act
        await lock2.ForceReleaseAsync();
        var result = await lock2.TryAcquireAsync();

        // Assert
        result.Should().BeTrue();

        // Cleanup
        await lock1.DisposeAsync();
        await lock2.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenLockFileStale_AcquiresLock()
    {
        // Arrange - Create a stale lock file (older than timeout)
        var lockFilePath = Path.Combine(_testLockDir, "stale.lock");
        var staleLock = new FileMigrationLock(lockFilePath, TimeSpan.FromMilliseconds(100));
        await staleLock.TryAcquireAsync();
        await staleLock.DisposeAsync();

        // Manually create old lock file
        var lockContent = System.Text.Json.JsonSerializer.Serialize(new
        {
            LockId = "stale-lock",
            HolderId = "old-process",
            AcquiredAt = DateTime.UtcNow.AddMinutes(-10),
            MachineName = Environment.MachineName
        });
        await File.WriteAllTextAsync(lockFilePath, lockContent);

        var newLock = new FileMigrationLock(lockFilePath, TimeSpan.FromSeconds(5));

        // Act
        var result = await newLock.TryAcquireAsync();

        // Assert
        result.Should().BeTrue();

        // Cleanup
        await newLock.DisposeAsync();
    }
}
