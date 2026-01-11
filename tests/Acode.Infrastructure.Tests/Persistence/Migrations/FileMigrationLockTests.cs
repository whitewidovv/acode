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

        // Give time for file handles to be fully released
        Thread.Sleep(50);

        if (Directory.Exists(_testLockDir))
        {
            try
            {
                Directory.Delete(_testLockDir, recursive: true);
            }
            catch (IOException)
            {
                // Ignore cleanup errors - may be locked by other parallel tests
            }
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
        // Arrange - Create a stale lock file without an active FileStream
        // On Windows, we can't delete a file that's actively held by another process,
        // so we simulate a stale/orphaned lock file instead
        var lockFilePath = Path.Combine(_testLockDir, "forceable.lock");
        var lockContent = System.Text.Json.JsonSerializer.Serialize(new
        {
            LockId = Guid.NewGuid().ToString(),
            HolderId = "old-process-12345",
            AcquiredAt = DateTime.UtcNow.AddMinutes(-5),
            MachineName = Environment.MachineName
        });
        await File.WriteAllTextAsync(lockFilePath, lockContent);

        var lock2 = new FileMigrationLock(lockFilePath, TimeSpan.FromSeconds(5));

        // Act
        await lock2.ForceReleaseAsync();
        var result = await lock2.TryAcquireAsync();

        // Assert
        result.Should().BeTrue();

        // Cleanup
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
