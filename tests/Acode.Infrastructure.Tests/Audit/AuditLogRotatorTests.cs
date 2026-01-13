namespace Acode.Infrastructure.Tests.Audit;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Acode.Domain.Audit;
using Acode.Infrastructure.Audit;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for AuditLogRotator.
/// Verifies log rotation, cleanup, and storage limit enforcement.
/// </summary>
public sealed class AuditLogRotatorTests : IDisposable
{
    private readonly string _testDir;
    private readonly AuditLogRotator _rotator;

    public AuditLogRotatorTests()
    {
        _testDir = Path.Combine(
            Path.GetTempPath(),
            $"audit_rotation_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);

        _rotator = new AuditLogRotator(
            new AuditConfiguration
            {
                MaxFileSize = 1024, // 1KB for testing
                RetentionDays = 90,
                MaxTotalStorage = 10 * 1024, // 10KB
            });
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public async Task Should_Rotate_OnSizeLimit()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "session_001.jsonl");

        // Create file larger than limit
        var largeContent = string.Join(
            "\n",
            Enumerable.Range(0, 100).Select(i => $"{{\"event\":{i}}}"));
        await File.WriteAllTextAsync(logPath, largeContent);

        var originalSize = new FileInfo(logPath).Length;
        originalSize.Should().BeGreaterThan(1024);

        // Act
        var result = await _rotator.RotateIfNeededAsync(logPath);

        // Assert
        result.RotationOccurred.Should().BeTrue();
        File.Exists(logPath + ".1")
            .Should()
            .BeTrue(because: "rotated file should exist with .1 suffix");
    }

    [Fact]
    public async Task Should_Not_Rotate_WhenUnderSizeLimit()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "session_small.jsonl");
        await File.WriteAllTextAsync(logPath, "{\"event\":1}"); // Small file

        // Act
        var result = await _rotator.RotateIfNeededAsync(logPath);

        // Assert
        result.RotationOccurred.Should().BeFalse();
        File.Exists(logPath + ".1").Should().BeFalse();
    }

    [Fact]
    public async Task Should_Preserve_Permissions()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "session_perm.jsonl");
        await File.WriteAllTextAsync(logPath, new string('x', 2000));

        // Set permissions (Unix only)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            File.SetUnixFileMode(
                logPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        // Act
        await _rotator.RotateIfNeededAsync(logPath);

        // Assert
        var rotatedPath = logPath + ".1";
        File.Exists(rotatedPath).Should().BeTrue();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var mode = File.GetUnixFileMode(rotatedPath);
            mode.Should().HaveFlag(UnixFileMode.UserRead);
            mode.Should().HaveFlag(UnixFileMode.UserWrite);
            mode.Should().NotHaveFlag(UnixFileMode.OtherRead);
            mode.Should().NotHaveFlag(UnixFileMode.OtherWrite);
        }
    }

    [Fact]
    public async Task Should_Be_Atomic()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "session_atomic.jsonl");
        var events = Enumerable.Range(0, 50)
            .Select(i => $"{{\"event\":{i}}}\n")
            .ToList();

        await File.WriteAllTextAsync(logPath, string.Join(string.Empty, events));

        // Count lines before
        var linesBefore = File.ReadAllLines(logPath).Length;

        // Act
        await _rotator.RotateIfNeededAsync(logPath);

        // Assert - rotated file should have all original lines
        var rotatedPath = logPath + ".1";
        if (File.Exists(rotatedPath))
        {
            var linesAfter = File.ReadAllLines(rotatedPath).Length;
            linesAfter.Should().Be(
                linesBefore,
                because: "rotation must not lose events");
        }
    }

    [Fact]
    public async Task Should_Delete_ExpiredLogs()
    {
        // Arrange - create old log files
        var oldLogPath = Path.Combine(_testDir, "2023-01-01T00-00-00Z_sess_old.jsonl");
        await File.WriteAllTextAsync(oldLogPath, "{\"event\":\"old\"}");

        // Make file appear old
        File.SetLastWriteTime(oldLogPath, DateTime.Now.AddDays(-100));

        var recentLogPath = Path.Combine(_testDir, "2024-01-01T00-00-00Z_sess_recent.jsonl");
        await File.WriteAllTextAsync(recentLogPath, "{\"event\":\"recent\"}");

        // Act
        var deleted = await _rotator.CleanupExpiredLogsAsync(_testDir, retentionDays: 90);

        // Assert
        deleted.Should().Contain(oldLogPath);
        File.Exists(oldLogPath)
            .Should()
            .BeFalse(because: "logs older than 90 days should be deleted");
        File.Exists(recentLogPath)
            .Should()
            .BeTrue(because: "recent logs should be kept");
    }

    [Fact]
    public async Task Should_Respect_StorageLimit()
    {
        // Arrange - create files exceeding limit
        for (int i = 0; i < 5; i++)
        {
            var path = Path.Combine(_testDir, $"session_{i:D3}.jsonl");
            await File.WriteAllTextAsync(path, new string('x', 3000)); // 3KB each = 15KB total
            File.SetLastWriteTime(path, DateTime.Now.AddDays(-i)); // Older files first
        }

        // Act - enforce 10KB limit
        var deleted = await _rotator.EnforceStorageLimitAsync(_testDir, maxBytes: 10 * 1024);

        // Assert
        deleted.Should().NotBeEmpty(
            because: "oldest files should be deleted to meet storage limit");

        var remainingSize = Directory.GetFiles(_testDir, "*.jsonl")
            .Sum(f => new FileInfo(f).Length);

        remainingSize.Should().BeLessThanOrEqualTo(
            10 * 1024,
            because: "total storage should not exceed limit");
    }

    [Fact]
    public async Task Should_Number_RotatedFiles_Sequentially()
    {
        // Arrange
        var logPath = Path.Combine(_testDir, "session_seq.jsonl");

        // Create and rotate multiple times
        for (int i = 0; i < 3; i++)
        {
            await File.WriteAllTextAsync(logPath, new string('x', 2000));
            await _rotator.RotateIfNeededAsync(logPath);
        }

        // Assert
        File.Exists(logPath + ".1").Should().BeTrue();
        File.Exists(logPath + ".2").Should().BeTrue();
        File.Exists(logPath + ".3").Should().BeTrue();
    }

    [Fact]
    public async Task Should_Log_Deletion_Before_Delete()
    {
        // Arrange
        var oldLogPath = Path.Combine(_testDir, "old_session.jsonl");
        await File.WriteAllTextAsync(oldLogPath, "{\"event\":\"old\"}");
        File.SetLastWriteTime(oldLogPath, DateTime.Now.AddDays(-100));

        var deletionLog = new List<string>();
        _rotator.OnBeforeDelete += path => deletionLog.Add(path);

        // Act
        await _rotator.CleanupExpiredLogsAsync(_testDir, retentionDays: 90);

        // Assert
        deletionLog.Should().Contain(
            oldLogPath,
            because: "deletion should be logged before execution");
    }

    [Fact]
    public async Task Should_Handle_NonExistentFile()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDir, "does_not_exist.jsonl");

        // Act
        var result = await _rotator.RotateIfNeededAsync(nonExistentPath);

        // Assert
        result.RotationOccurred.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Skip_NonJsonlFiles()
    {
        // Arrange - create non-.jsonl files
        var txtPath = Path.Combine(_testDir, "notes.txt");
        await File.WriteAllTextAsync(txtPath, "Some notes");
        File.SetLastWriteTime(txtPath, DateTime.Now.AddDays(-100));

        // Act
        var deleted = await _rotator.CleanupExpiredLogsAsync(_testDir, retentionDays: 90);

        // Assert
        deleted.Should().BeEmpty(
            because: "non-.jsonl files should not be deleted");
        File.Exists(txtPath).Should().BeTrue();
    }
}
