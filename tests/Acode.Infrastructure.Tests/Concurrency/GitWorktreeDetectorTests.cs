// tests/Acode.Infrastructure.Tests/Concurrency/GitWorktreeDetectorTests.cs
#pragma warning disable CA2007 // xUnit tests should use ConfigureAwait(true)

namespace Acode.Infrastructure.Tests.Concurrency;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Worktree;
using Acode.Infrastructure.Concurrency;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Tests for <see cref="GitWorktreeDetector"/>.
/// Verifies Git worktree detection from filesystem metadata.
/// </summary>
public sealed class GitWorktreeDetectorTests : IDisposable
{
    private readonly string _testRoot;

    public GitWorktreeDetectorTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), $"acode-worktree-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    [Fact]
    public async Task DetectAsync_WithGitDirectory_ReturnsWorktree()
    {
        // Arrange
        var worktreeRoot = Path.Combine(_testRoot, "my-project");
        var gitDir = Path.Combine(worktreeRoot, ".git");
        var subDir = Path.Combine(worktreeRoot, "src", "subfolder");

        Directory.CreateDirectory(gitDir);
        Directory.CreateDirectory(subDir);

        var detector = new GitWorktreeDetector(NullLogger<GitWorktreeDetector>.Instance);

        // Act - Detect from subdirectory
        var result = await detector.DetectAsync(subDir, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Path.Should().Be(worktreeRoot);
        result.Id.Should().Be(WorktreeId.FromPath(worktreeRoot));
    }

    [Fact]
    public async Task DetectAsync_WithGitFile_ReturnsWorktree()
    {
        // Arrange - Worktree with .git file (not directory)
        var worktreeRoot = Path.Combine(_testRoot, "feature-auth");
        var gitFile = Path.Combine(worktreeRoot, ".git");
        var subDir = Path.Combine(worktreeRoot, "src");

        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(gitFile, "gitdir: /path/to/main/repo/.git/worktrees/feature-auth");

        var detector = new GitWorktreeDetector(NullLogger<GitWorktreeDetector>.Instance);

        // Act
        var result = await detector.DetectAsync(subDir, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Path.Should().Be(worktreeRoot);
        result.Id.Should().Be(WorktreeId.FromPath(worktreeRoot));
    }

    [Fact]
    public async Task DetectAsync_WithoutGit_ReturnsNull()
    {
        // Arrange - Directory with no .git
        var normalDir = Path.Combine(_testRoot, "not-a-repo", "src");
        Directory.CreateDirectory(normalDir);

        var detector = new GitWorktreeDetector(NullLogger<GitWorktreeDetector>.Instance);

        // Act
        var result = await detector.DetectAsync(normalDir, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DetectAsync_FromWorktreeRoot_ReturnsWorktree()
    {
        // Arrange - Detect from root directory itself
        var worktreeRoot = Path.Combine(_testRoot, "my-project");
        var gitDir = Path.Combine(worktreeRoot, ".git");
        Directory.CreateDirectory(gitDir);

        var detector = new GitWorktreeDetector(NullLogger<GitWorktreeDetector>.Instance);

        // Act
        var result = await detector.DetectAsync(worktreeRoot, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Path.Should().Be(worktreeRoot);
    }

    [Fact]
    public async Task DetectAsync_WalksUpDirectoryTree()
    {
        // Arrange - Deep subdirectory
        var worktreeRoot = Path.Combine(_testRoot, "my-project");
        var gitDir = Path.Combine(worktreeRoot, ".git");
        var deepDir = Path.Combine(worktreeRoot, "a", "b", "c", "d", "e");

        Directory.CreateDirectory(gitDir);
        Directory.CreateDirectory(deepDir);

        var detector = new GitWorktreeDetector(NullLogger<GitWorktreeDetector>.Instance);

        // Act - Detect from 5 levels deep
        var result = await detector.DetectAsync(deepDir, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Path.Should().Be(worktreeRoot);
    }

    [Fact]
    public async Task DetectAsync_WithNonExistentPath_ReturnsNull()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testRoot, "does-not-exist");
        var detector = new GitWorktreeDetector(NullLogger<GitWorktreeDetector>.Instance);

        // Act
        var result = await detector.DetectAsync(nonExistentPath, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DetectAsync_StopsAtFilesystemRoot()
    {
        // Arrange - Start from temp directory (no git repo above it)
        var detector = new GitWorktreeDetector(NullLogger<GitWorktreeDetector>.Instance);

        // Act - Walk up to filesystem root
        var result = await detector.DetectAsync(_testRoot, CancellationToken.None);

        // Assert - Should not find anything
        result.Should().BeNull();
    }
}
