namespace Acode.Domain.Tests.Security.PathProtection;

using System;
using System.IO;
using System.Linq;
using Acode.Domain.Security.PathProtection;
using FluentAssertions;

/// <summary>
/// Comprehensive tests for IPathNormalizer implementations (PathNormalizer).
/// Spec: task-003b lines 1391-1648.
/// </summary>
public class PathNormalizerTests
{
    private readonly IPathNormalizer _normalizer;

    public PathNormalizerTests()
    {
        _normalizer = new PathNormalizer();
    }

    [Theory]
    [InlineData("~/documents/file.txt")]
    [InlineData("~/.ssh/id_rsa")]
    [InlineData("~/.aws/credentials")]
    public void Should_Expand_Tilde(string path)
    {
        // Act
        var result = _normalizer.Normalize(path);

        // Assert
        result.Should().NotContain(
            "~",
            because: "tilde should be expanded to home directory");
        result.Should().NotBeNullOrWhiteSpace();

        if (OperatingSystem.IsWindows())
        {
            result.Should().StartWith(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        }
        else
        {
            result.Should().StartWith(
                Environment.GetEnvironmentVariable("HOME"));
        }
    }

    [Theory]
    [InlineData("%USERPROFILE%\\.ssh\\id_rsa")]
    [InlineData("%USERPROFILE%\\.aws\\credentials")]
    public void Should_Expand_UserProfile(string path)
    {
        // Skip on non-Windows
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Act
        var result = _normalizer.Normalize(path);

        // Assert
        result.Should().NotContain(
            "%USERPROFILE%",
            because: "USERPROFILE should be expanded");
        result.Should().StartWith(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    }

    [Theory]
    [InlineData("$HOME/.ssh/id_rsa")]
    [InlineData("$HOME/.aws/credentials")]
    public void Should_Expand_Home(string path)
    {
        // Skip on Windows
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        // Act
        var result = _normalizer.Normalize(path);

        // Assert
        result.Should().NotContain(
            "$HOME",
            because: "HOME variable should be expanded");
        result.Should().StartWith(
            Environment.GetEnvironmentVariable("HOME"));
    }

    [Theory]
    [InlineData("/home/user/../root/.ssh", "/root/.ssh")]
    [InlineData("./src/../tests/unit", "/tests/unit")]
    [InlineData("/etc/ssh/../passwd", "/etc/passwd")]
    [InlineData("~/../../../etc/passwd", "/etc/passwd")]
    public void Should_Resolve_DotDot(string path, string expectedEnd)
    {
        // Act
        var result = _normalizer.Normalize(path);

        // Assert
        result.Should().NotContain(
            "..",
            because: "parent directory references must be resolved");
#pragma warning disable CA1062 // Test parameters from InlineData are never null
        result.Should().EndWith(
            expectedEnd.Replace("/", Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal),
            because: "path should resolve to expected location");
#pragma warning restore CA1062
    }

    [Theory]
    [InlineData("./src/./tests/./unit", "src/tests/unit")]
    [InlineData("/etc/./ssh/./config", "/etc/ssh/config")]
    public void Should_Remove_Dot(string path, string expected)
    {
        // Act
        var result = _normalizer.Normalize(path);

        // Assert
        result.Should().NotContain(
            "/./",
            because: "single dot references should be removed");
#pragma warning disable CA1062 // Test parameters from InlineData are never null
        result.Should().Contain(
            expected.Replace("/", Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal));
#pragma warning restore CA1062
    }

    [Theory]
    [InlineData("~/.ssh//id_rsa")]
    [InlineData("/etc///passwd")]
    [InlineData("./src////file.cs")]
    public void Should_Collapse_Slashes(string path)
    {
        // Act
        var result = _normalizer.Normalize(path);

        // Assert
        result.Should().NotContain(
            "//",
            because: "multiple consecutive slashes should be collapsed");
    }

    [Theory]
    [InlineData("~/.ssh/")]
    [InlineData("/etc/ssh/")]
    [InlineData("./src/")]
    public void Should_Remove_Trailing_Slash(string path)
    {
        // Act
        var result = _normalizer.Normalize(path);

        // Assert
        result.Should().NotEndWith(
            "/",
            because: "trailing slashes should be removed for consistency");
        result.Should().NotEndWith(
            "\\",
            because: "trailing backslashes should be removed for consistency");
    }

    [Theory]
    [InlineData("C:\\Users\\test\\.ssh\\id_rsa")]
    [InlineData("C:\\Windows\\System32\\config")]
    public void Should_Convert_Backslash_On_Windows(string path)
    {
        // Act
        var result = _normalizer.Normalize(path);

        // Assert - on Windows, should use consistent separator
        if (OperatingSystem.IsWindows())
        {
            // All separators should be consistent
            var separatorCount = result.Count(c => c == '/' || c == '\\');
            var primarySep = result.Count(c => c == Path.DirectorySeparatorChar);
            primarySep.Should().Be(
                separatorCount,
                because: "all separators should be the platform separator");
        }
    }

    [Fact]
    public void Should_Handle_Very_Long_Paths()
    {
        // Arrange
        var longPath = "/home/user/" + string.Join("/", Enumerable.Repeat("subdir", 100)) + "/file.txt";

        // Act
        var result = _normalizer.Normalize(longPath);

        // Assert
        result.Should().NotBeNullOrWhiteSpace(
            because: "long paths should be handled without truncation");
        result.Should().Contain("file.txt");
    }

    [Theory]
    [InlineData("/home/用户/documents/文件.txt")]
    [InlineData("/home/пользователь/docs/файл.txt")]
    [InlineData("/home/user/données/ñoño.txt")]
    public void Should_Handle_Unicode(string path)
    {
        // Act
        var result = _normalizer.Normalize(path);

        // Assert
        result.Should().NotBeNullOrWhiteSpace(
            because: "Unicode paths should be preserved");

        // Unicode characters should remain intact
        result.Length.Should().BeGreaterThan(10);
    }

    [Theory]
    [InlineData("/home/user/file with spaces.txt")]
    [InlineData("/home/user/file\ttab.txt")]
    [InlineData("/home/user/file'quote.txt")]
    public void Should_Handle_Special_Characters(string path)
    {
        // Act
        var result = _normalizer.Normalize(path);

        // Assert
        result.Should().NotBeNullOrWhiteSpace(
            because: "special characters should be handled");
    }

    [Fact]
    public void Should_Reject_Null_Byte()
    {
        // Arrange
        var pathWithNull = "/home/user/file\0.txt";

        // Act
        Action act = () => _normalizer.Normalize(pathWithNull);

        // Assert
        act.Should().Throw<ArgumentException>(
            because: "null bytes in paths are a security risk");
    }

    [Fact]
    public void Should_Handle_Empty_Path()
    {
        // Act
        var result = _normalizer.Normalize(string.Empty);

        // Assert
        result.Should().BeEmpty(
            because: "empty paths should return empty");
    }

    [Fact]
    public void Should_Handle_Null_Path()
    {
        // Act
        Action act = () => _normalizer.Normalize(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
