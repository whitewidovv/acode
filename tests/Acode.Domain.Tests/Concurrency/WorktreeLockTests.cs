// tests/Acode.Domain.Tests/Concurrency/WorktreeLockTests.cs
namespace Acode.Domain.Tests.Concurrency;

using System;
using System.Diagnostics;
using Acode.Domain.Concurrency;
using Acode.Domain.Worktree;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for WorktreeLock domain entity.
/// Verifies lock creation, stale detection, and terminal ID generation.
/// </summary>
public sealed class WorktreeLockTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesLock()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var processId = 12345;
        var lockedAt = DateTimeOffset.UtcNow.AddMinutes(-2);
        var hostname = "dev-machine";
        var terminal = "/dev/ttys001";

        // Act
        var lock_ = new WorktreeLock(worktreeId, processId, lockedAt, hostname, terminal);

        // Assert
        lock_.WorktreeId.Should().Be(worktreeId);
        lock_.ProcessId.Should().Be(processId);
        lock_.LockedAt.Should().Be(lockedAt);
        lock_.Hostname.Should().Be(hostname);
        lock_.Terminal.Should().Be(terminal);
    }

    [Fact]
    public void Age_ReturnsTimeSinceLockedAt()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var lockedAt = DateTimeOffset.UtcNow.AddMinutes(-3);
        var lock_ = new WorktreeLock(worktreeId, 12345, lockedAt, "host", "term");

        // Act
        var age = lock_.Age;

        // Assert
        age.Should().BeCloseTo(TimeSpan.FromMinutes(3), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void IsStale_WithAgeGreaterThanThreshold_ReturnsTrue()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var lockedAt = DateTimeOffset.UtcNow.AddMinutes(-10);  // 10 minutes ago
        var lock_ = new WorktreeLock(worktreeId, 12345, lockedAt, "host", "term");
        var threshold = TimeSpan.FromMinutes(5);

        // Act
        var isStale = lock_.IsStale(threshold);

        // Assert
        isStale.Should().BeTrue("lock is 10 minutes old, threshold is 5 minutes");
    }

    [Fact]
    public void IsStale_WithAgeLessThanThreshold_ReturnsFalse()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var lockedAt = DateTimeOffset.UtcNow.AddMinutes(-2);  // 2 minutes ago
        var lock_ = new WorktreeLock(worktreeId, 12345, lockedAt, "host", "term");
        var threshold = TimeSpan.FromMinutes(5);

        // Act
        var isStale = lock_.IsStale(threshold);

        // Assert
        isStale.Should().BeFalse("lock is only 2 minutes old, threshold is 5 minutes");
    }

    [Fact]
    public void IsOwnedByCurrentProcess_WithCurrentProcessId_ReturnsTrue()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var currentProcessId = Environment.ProcessId;
        var lock_ = new WorktreeLock(worktreeId, currentProcessId, DateTimeOffset.UtcNow, "host", "term");

        // Act
        var isOwned = lock_.IsOwnedByCurrentProcess();

        // Assert
        isOwned.Should().BeTrue("lock process ID matches current process");
    }

    [Fact]
    public void IsOwnedByCurrentProcess_WithDifferentProcessId_ReturnsFalse()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var differentProcessId = Environment.ProcessId + 999;  // Different process
        var lock_ = new WorktreeLock(worktreeId, differentProcessId, DateTimeOffset.UtcNow, "host", "term");

        // Act
        var isOwned = lock_.IsOwnedByCurrentProcess();

        // Assert
        isOwned.Should().BeFalse("lock process ID does not match current process");
    }

    [Fact]
    public void CreateForCurrentProcess_SetsProcessIdAndHostname()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");

        // Act
        var lock_ = WorktreeLock.CreateForCurrentProcess(worktreeId);

        // Assert
        lock_.WorktreeId.Should().Be(worktreeId);
        lock_.ProcessId.Should().Be(Environment.ProcessId);
        lock_.Hostname.Should().Be(Environment.MachineName);
        lock_.LockedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        lock_.Terminal.Should().NotBeNullOrWhiteSpace("terminal ID should be set");
    }

    [Fact]
    public void CreateForCurrentProcess_OnUnix_SetsTerminalFromEnvironment()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");

        // Act
        var lock_ = WorktreeLock.CreateForCurrentProcess(worktreeId);

        // Assert
        if (!OperatingSystem.IsWindows())
        {
            // Unix systems use TTY environment variable or default
            lock_.Terminal.Should().MatchRegex(@"^(/dev/|session-)\S+", "Unix terminal should be /dev/ttys* or session-*");
        }
        else
        {
            // Windows uses session ID
            lock_.Terminal.Should().StartWith("session-", "Windows terminal should be session-{id}");
        }
    }

    [Fact]
    public void CreateForCurrentProcess_OnWindows_SetsSessionId()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");

        // Act
        var lock_ = WorktreeLock.CreateForCurrentProcess(worktreeId);

        // Assert
        if (OperatingSystem.IsWindows())
        {
            var sessionId = Process.GetCurrentProcess().SessionId;
            lock_.Terminal.Should().Be($"session-{sessionId}", "Windows should use session ID as terminal");
        }
    }
}
