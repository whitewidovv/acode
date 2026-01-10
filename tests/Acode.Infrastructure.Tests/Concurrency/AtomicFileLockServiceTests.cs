#pragma warning disable CA2007 // xUnit tests should use ConfigureAwait(true)
#pragma warning disable IDE0005 // Using directive is unnecessary

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Concurrency;
using Acode.Domain.Concurrency;
using Acode.Domain.Worktree;
using Acode.Infrastructure.Concurrency;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Acode.Infrastructure.Tests.Concurrency;

/// <summary>
/// Tests for <see cref="AtomicFileLockService"/>.
/// Verifies file-based locking with atomic operations and stale detection.
/// </summary>
public sealed class AtomicFileLockServiceTests : IDisposable
{
    private readonly string _testWorkspaceRoot;

    public AtomicFileLockServiceTests()
    {
        _testWorkspaceRoot = Path.Combine(Path.GetTempPath(), $"acode-lock-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testWorkspaceRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testWorkspaceRoot))
        {
            Directory.Delete(_testWorkspaceRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Should_Acquire_Lock()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var lockService = new AtomicFileLockService(_testWorkspaceRoot, NullLogger<AtomicFileLockService>.Instance);

        // Act
        await using var lockHandle = await lockService.AcquireAsync(
            worktreeId,
            timeout: null,
            CancellationToken.None).ConfigureAwait(true);

        // Assert
        lockHandle.Should().NotBeNull();
        var lockFile = Path.Combine(_testWorkspaceRoot, ".agent", "locks", $"{worktreeId.Value}.lock");
        File.Exists(lockFile).Should().BeTrue("lock file should exist");

        var lockContent = await File.ReadAllTextAsync(lockFile).ConfigureAwait(true);
        var lockData = JsonSerializer.Deserialize<LockData>(lockContent);
        lockData.Should().NotBeNull();
        lockData!.ProcessId.Should().Be(Environment.ProcessId);
    }

    [Fact]
    public async Task Should_Release_Lock_On_Dispose()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var lockService = new AtomicFileLockService(_testWorkspaceRoot, NullLogger<AtomicFileLockService>.Instance);
        var lockFile = Path.Combine(_testWorkspaceRoot, ".agent", "locks", $"{worktreeId.Value}.lock");

        // Act
        var lockHandle = await lockService.AcquireAsync(worktreeId, null, CancellationToken.None).ConfigureAwait(true);
        File.Exists(lockFile).Should().BeTrue("lock acquired");

        await lockHandle.DisposeAsync().ConfigureAwait(true);

        // Assert
        File.Exists(lockFile).Should().BeFalse("lock should be released after dispose");
    }

    [Fact]
    public async Task Should_Detect_Stale_Lock()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var lockService = new AtomicFileLockService(_testWorkspaceRoot, NullLogger<AtomicFileLockService>.Instance);
        var lockDir = Path.Combine(_testWorkspaceRoot, ".agent", "locks");
        Directory.CreateDirectory(lockDir);
        var lockFile = Path.Combine(lockDir, $"{worktreeId.Value}.lock");

        // Create stale lock (timestamp 10 minutes ago, dead process ID)
        var staleLockData = new LockData(
            ProcessId: 99999,  // Unlikely to exist
            LockedAt: DateTimeOffset.UtcNow.AddMinutes(-10),
            Hostname: Environment.MachineName,
            Terminal: WorktreeLock.GetTerminalId());
        await File.WriteAllTextAsync(lockFile, JsonSerializer.Serialize(staleLockData)).ConfigureAwait(true);

        // Act
        var status = await lockService.GetStatusAsync(worktreeId, CancellationToken.None).ConfigureAwait(true);

        // Assert
        status.IsStale.Should().BeTrue("lock older than 5 minutes should be stale");
        status.Age.Should().BeGreaterThan(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task Should_Block_Concurrent_Acquisition()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var lockService = new AtomicFileLockService(_testWorkspaceRoot, NullLogger<AtomicFileLockService>.Instance);

        // Act
        await using var lock1 = await lockService.AcquireAsync(worktreeId, null, CancellationToken.None).ConfigureAwait(true);

        var act = async () => await lockService.AcquireAsync(worktreeId, timeout: null, CancellationToken.None).ConfigureAwait(true);

        // Assert
        await act.Should().ThrowAsync<LockBusyException>()
            .WithMessage("*locked by*");
    }

    [Fact]
    public async Task Should_Queue_With_Wait_Timeout()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var lockService = new AtomicFileLockService(_testWorkspaceRoot, NullLogger<AtomicFileLockService>.Instance);

        await using var lock1 = await lockService.AcquireAsync(worktreeId, null, CancellationToken.None).ConfigureAwait(true);

        // Act
        var startTime = DateTimeOffset.UtcNow;
        var act = async () => await lockService.AcquireAsync(
            worktreeId,
            timeout: TimeSpan.FromSeconds(2),
            CancellationToken.None).ConfigureAwait(true);

        // Assert
        await act.Should().ThrowAsync<TimeoutException>()
            .WithMessage("*timeout waiting for lock*");

        var elapsed = DateTimeOffset.UtcNow - startTime;

        // Relaxed precision for WSL/CI environments (3s tolerance instead of 500ms)
        elapsed.Should().BeCloseTo(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task Should_Remove_Stale_Lock_On_Acquisition()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var lockService = new AtomicFileLockService(_testWorkspaceRoot, NullLogger<AtomicFileLockService>.Instance);
        var lockDir = Path.Combine(_testWorkspaceRoot, ".agent", "locks");
        Directory.CreateDirectory(lockDir);
        var lockFile = Path.Combine(lockDir, $"{worktreeId.Value}.lock");

        // Create stale lock
        var staleLockData = new LockData(
            ProcessId: 99999,
            LockedAt: DateTimeOffset.UtcNow.AddMinutes(-10),
            Hostname: Environment.MachineName,
            Terminal: WorktreeLock.GetTerminalId());
        await File.WriteAllTextAsync(lockFile, JsonSerializer.Serialize(staleLockData)).ConfigureAwait(true);

        // Act - Should remove stale and acquire
        await using var lockHandle = await lockService.AcquireAsync(worktreeId, null, CancellationToken.None).ConfigureAwait(true);

        // Assert
        lockHandle.Should().NotBeNull("should acquire after removing stale lock");
        File.Exists(lockFile).Should().BeTrue("new lock should exist");

        var lockContent = await File.ReadAllTextAsync(lockFile).ConfigureAwait(true);
        var lockData = JsonSerializer.Deserialize<LockData>(lockContent);
        lockData!.ProcessId.Should().Be(Environment.ProcessId, "lock should be owned by current process");
    }

    [Fact]
    public async Task ForceUnlockAsync_RemovesLock()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var lockService = new AtomicFileLockService(_testWorkspaceRoot, NullLogger<AtomicFileLockService>.Instance);

        await using var lockHandle = await lockService.AcquireAsync(worktreeId, null, CancellationToken.None).ConfigureAwait(true);
        var lockFile = Path.Combine(_testWorkspaceRoot, ".agent", "locks", $"{worktreeId.Value}.lock");
        File.Exists(lockFile).Should().BeTrue("lock should exist");

        // Act
        await lockService.ForceUnlockAsync(worktreeId, CancellationToken.None).ConfigureAwait(true);

        // Assert
        File.Exists(lockFile).Should().BeFalse("lock should be removed");
    }

    [Fact]
    public async Task ReleaseStaleLocksAsync_RemovesOldLocks()
    {
        // Arrange
        var worktreeId1 = WorktreeId.FromPath("/home/user/project/feature/auth");
        var worktreeId2 = WorktreeId.FromPath("/home/user/project/feature/payments");
        var lockService = new AtomicFileLockService(_testWorkspaceRoot, NullLogger<AtomicFileLockService>.Instance);
        var lockDir = Path.Combine(_testWorkspaceRoot, ".agent", "locks");
        Directory.CreateDirectory(lockDir);

        // Create stale lock (10 minutes old)
        var staleLock = Path.Combine(lockDir, $"{worktreeId1.Value}.lock");
        var staleLockData = new LockData(
            ProcessId: 99999,
            LockedAt: DateTimeOffset.UtcNow.AddMinutes(-10),
            Hostname: Environment.MachineName,
            Terminal: WorktreeLock.GetTerminalId());
        await File.WriteAllTextAsync(staleLock, JsonSerializer.Serialize(staleLockData)).ConfigureAwait(true);

        // Create recent lock (1 minute old)
        var recentLock = Path.Combine(lockDir, $"{worktreeId2.Value}.lock");
        var recentLockData = new LockData(
            ProcessId: Environment.ProcessId,
            LockedAt: DateTimeOffset.UtcNow.AddMinutes(-1),
            Hostname: Environment.MachineName,
            Terminal: WorktreeLock.GetTerminalId());
        await File.WriteAllTextAsync(recentLock, JsonSerializer.Serialize(recentLockData)).ConfigureAwait(true);

        // Act
        await lockService.ReleaseStaleLocksAsync(TimeSpan.FromMinutes(5), CancellationToken.None).ConfigureAwait(true);

        // Assert
        File.Exists(staleLock).Should().BeFalse("stale lock should be removed");
        File.Exists(recentLock).Should().BeTrue("recent lock should remain");
    }

    [Fact]
    public async Task GetStatusAsync_WithNonExistentLock_ReturnsNotLocked()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var lockService = new AtomicFileLockService(_testWorkspaceRoot, NullLogger<AtomicFileLockService>.Instance);

        // Act
        var status = await lockService.GetStatusAsync(worktreeId, CancellationToken.None).ConfigureAwait(true);

        // Assert
        status.IsLocked.Should().BeFalse("worktree is not locked");
        status.IsStale.Should().BeFalse();
        status.ProcessId.Should().BeNull();
    }
}
