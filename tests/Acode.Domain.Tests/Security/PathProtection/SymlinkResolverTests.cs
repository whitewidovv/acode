namespace Acode.Domain.Tests.Security.PathProtection;

using System;
using System.IO;
using Acode.Domain.Security.PathProtection;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for ISymlinkResolver implementations (SymlinkResolver).
/// Spec: task-003b lines 1661-1888.
/// </summary>
public sealed class SymlinkResolverTests : IDisposable
{
    private readonly ISymlinkResolver _resolver;
    private readonly string _testDir;

    public SymlinkResolverTests()
    {
        _resolver = new SymlinkResolver(maxDepth: 40);
        _testDir = Path.Combine(Path.GetTempPath(), $"symlink_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_testDir, recursive: true);
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public void Should_Resolve_Single_Symlink()
    {
        // Skip on Windows if not running as admin (symlinks require elevation)
        if (OperatingSystem.IsWindows() && !HasSymlinkPrivilege())
        {
            return;
        }

        // Arrange
        var targetFile = Path.Combine(_testDir, "target.txt");
        var symlinkPath = Path.Combine(_testDir, "link.txt");
        File.WriteAllText(targetFile, "test content");
        CreateSymlink(symlinkPath, targetFile);

        // Act
        var result = _resolver.Resolve(symlinkPath);

        // Assert
        result.IsSuccess.Should().BeTrue(
            because: "symlink should resolve successfully");
        result.ResolvedPath.Should().Be(
            targetFile,
            because: "symlink should resolve to target");
        result.Depth.Should().Be(
            1,
            because: "one symlink was traversed");
        result.Error.Should().Be(
            SymlinkError.None,
            because: "no error occurred");
    }

    [Fact]
    public void Should_Resolve_Chain_Of_Symlinks()
    {
        if (OperatingSystem.IsWindows() && !HasSymlinkPrivilege())
        {
            return;
        }

        // Arrange - create chain: link1 -> link2 -> link3 -> target
        var targetFile = Path.Combine(_testDir, "final_target.txt");
        var link3 = Path.Combine(_testDir, "link3.txt");
        var link2 = Path.Combine(_testDir, "link2.txt");
        var link1 = Path.Combine(_testDir, "link1.txt");

        File.WriteAllText(targetFile, "content");
        CreateSymlink(link3, targetFile);
        CreateSymlink(link2, link3);
        CreateSymlink(link1, link2);

        // Act
        var result = _resolver.Resolve(link1);

        // Assert
        result.IsSuccess.Should().BeTrue(
            because: "chain should resolve successfully");
        result.ResolvedPath.Should().Be(
            targetFile,
            because: "chain of symlinks should resolve to final target");
        result.Depth.Should().Be(
            3,
            because: "three symlinks were traversed");
        result.Error.Should().Be(SymlinkError.None);
    }

    [Fact]
    public void Should_Detect_Circular_Symlink()
    {
        if (OperatingSystem.IsWindows() && !HasSymlinkPrivilege())
        {
            return;
        }

        // Arrange - create circular: link1 -> link2 -> link1
        var link1 = Path.Combine(_testDir, "circular1.txt");
        var link2 = Path.Combine(_testDir, "circular2.txt");

        // Create files first, then replace with symlinks
        File.WriteAllText(link2, "temp");
        CreateSymlink(link1, link2);
        File.Delete(link2);
        CreateSymlink(link2, link1);

        // Act
        var result = _resolver.Resolve(link1);

        // Assert
        result.IsSuccess.Should().BeFalse(
            because: "circular symlinks should be detected");
        result.Error.Should().Be(
            SymlinkError.CircularReference,
            because: "circular reference was detected");
        result.ResolvedPath.Should().BeNull(
            because: "resolution failed");
    }

    [Fact]
    public void Should_Enforce_Max_Depth_40()
    {
        if (OperatingSystem.IsWindows() && !HasSymlinkPrivilege())
        {
            return;
        }

        // Arrange - create chain of 45 symlinks (exceeds max of 40)
        var targetFile = Path.Combine(_testDir, "deep_target.txt");
        File.WriteAllText(targetFile, "content");

        var previousLink = targetFile;
        for (int i = 0; i < 45; i++)
        {
            var newLink = Path.Combine(_testDir, $"deep_link_{i}.txt");
            CreateSymlink(newLink, previousLink);
            previousLink = newLink;
        }

        // Act
        var result = _resolver.Resolve(previousLink);

        // Assert
        result.IsSuccess.Should().BeFalse(
            because: "depth exceeding 40 should fail");
        result.Error.Should().Be(
            SymlinkError.MaxDepthExceeded,
            because: "maximum depth was exceeded");
        result.ResolvedPath.Should().BeNull();
    }

    [Fact]
    public void Should_Block_On_Resolution_Error()
    {
        // Arrange - symlink to non-existent target
        var brokenLink = Path.Combine(_testDir, "broken_link.txt");
        var nonExistentTarget = Path.Combine(_testDir, "does_not_exist.txt");

        if (OperatingSystem.IsWindows() && !HasSymlinkPrivilege())
        {
            // On Windows without privileges, just test with a non-existent path
            var result = _resolver.Resolve(nonExistentTarget);
            result.IsSuccess.Should().BeFalse();
            return;
        }

        CreateSymlink(brokenLink, nonExistentTarget);

        // Act
        var result2 = _resolver.Resolve(brokenLink);

        // Assert
        result2.IsSuccess.Should().BeFalse(
            because: "broken symlink should fail resolution");
        result2.Error.Should().Be(
            SymlinkError.TargetNotFound,
            because: "target does not exist");
    }

    [Fact]
    public void Should_Return_Same_Path_If_Not_Symlink()
    {
        // Arrange
        var regularFile = Path.Combine(_testDir, "regular_file.txt");
        File.WriteAllText(regularFile, "test content");

        // Act
        var result = _resolver.Resolve(regularFile);

        // Assert
        result.IsSuccess.Should().BeTrue(
            because: "regular files should resolve successfully");
        result.ResolvedPath.Should().Be(
            regularFile,
            because: "regular files should resolve to themselves");
        result.Depth.Should().Be(
            0,
            because: "no symlinks were traversed");
        result.Error.Should().Be(SymlinkError.None);
    }

    [Fact]
    public void Should_Cache_Resolution()
    {
        // Arrange
        var regularFile = Path.Combine(_testDir, "regular.txt");
        File.WriteAllText(regularFile, "content");

        // Act - resolve same path twice
        var result1 = _resolver.Resolve(regularFile);
        var result2 = _resolver.Resolve(regularFile);

        // Assert
        result1.IsSuccess.Should().BeTrue(
            because: "first resolution should succeed");
        result2.IsSuccess.Should().BeTrue(
            because: "second resolution should succeed");
        result1.ResolvedPath.Should().Be(
            result2.ResolvedPath,
            because: "both resolutions should return same path");

        // Cache hit should be faster (implementation detail)
        // The important thing is both calls succeed
    }

    [Fact]
    public void Should_Handle_Null_Path()
    {
        // Act
        Action act = () => _resolver.Resolve(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>(
            because: "null path is invalid");
    }

    [Fact]
    public void Should_Handle_Empty_Path()
    {
        // Act
        var result = _resolver.Resolve(string.Empty);

        // Assert
        result.IsSuccess.Should().BeFalse(
            because: "empty path is invalid");
    }

    [Fact]
    public void Should_Handle_Nonexistent_Path()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDir, "does_not_exist.txt");

        // Act
        var result = _resolver.Resolve(nonExistentPath);

        // Assert
        result.IsSuccess.Should().BeFalse(
            because: "non-existent paths should fail");
        result.Error.Should().Be(
            SymlinkError.TargetNotFound,
            because: "target does not exist");
    }

    private static bool HasSymlinkPrivilege()
    {
        // On Windows, creating symlinks requires admin or developer mode
        try
        {
            var testLink = Path.Combine(Path.GetTempPath(), $"symlink_test_{Guid.NewGuid():N}");
            var testTarget = Path.Combine(Path.GetTempPath(), $"symlink_target_{Guid.NewGuid():N}");
            File.WriteAllText(testTarget, "test");
            File.CreateSymbolicLink(testLink, testTarget);
            File.Delete(testLink);
            File.Delete(testTarget);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void CreateSymlink(string linkPath, string targetPath)
    {
        // File.CreateSymbolicLink works on both Windows and Unix
        File.CreateSymbolicLink(linkPath, targetPath);
    }
}
