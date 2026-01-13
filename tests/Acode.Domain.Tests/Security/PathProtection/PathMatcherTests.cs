namespace Acode.Domain.Tests.Security.PathProtection;

using System.Diagnostics;
using Acode.Domain.Security.PathProtection;
using FluentAssertions;

/// <summary>
/// Comprehensive tests for IPathMatcher implementations (GlobMatcher).
/// Spec: task-003b lines 1129-1387.
/// </summary>
public class PathMatcherTests
{
    private readonly IPathMatcher _matcher;
    private readonly IPathMatcher _caseSensitiveMatcher;
    private readonly IPathMatcher _caseInsensitiveMatcher;

    public PathMatcherTests()
    {
        _matcher = new GlobMatcher(caseSensitive: true);
        _caseSensitiveMatcher = new GlobMatcher(caseSensitive: true);
        _caseInsensitiveMatcher = new GlobMatcher(caseSensitive: false);
    }

    [Theory]
    [InlineData("~/.ssh/id_rsa", "~/.ssh/id_rsa", true)]
    [InlineData("~/.ssh/id_rsa", "~/.ssh/id_ed25519", false)]
    [InlineData("/etc/passwd", "/etc/passwd", true)]
    [InlineData("/etc/passwd", "/etc/shadow", false)]
    public void Should_Match_Exact_Path(string pattern, string path, bool expected)
    {
        // Act
        var result = _matcher.Matches(pattern, path);

        // Assert
        result.Should().Be(
            expected,
            because: $"pattern '{pattern}' should{(expected ? string.Empty : " not")} match '{path}'");
    }

    [Theory]
    [InlineData("~/.ssh/", "~/.ssh/id_rsa", true)]
    [InlineData("~/.ssh/", "~/.ssh/config", true)]
    [InlineData("~/.aws/", "~/.aws/credentials", true)]
    [InlineData("~/.ssh/", "~/.gnupg/secring.gpg", false)]
    [InlineData("/etc/", "/etc/passwd", true)]
    [InlineData("/etc/", "/etc/ssh/sshd_config", true)]
    public void Should_Match_Directory_Prefix(string pattern, string path, bool expected)
    {
        // Act
        var result = _matcher.Matches(pattern, path);

        // Assert
        result.Should().Be(
            expected,
            because: $"directory pattern '{pattern}' should{(expected ? string.Empty : " not")} match '{path}'");
    }

    [Theory]
    [InlineData("~/.ssh/id_*", "~/.ssh/id_rsa", true)]
    [InlineData("~/.ssh/id_*", "~/.ssh/id_ed25519", true)]
    [InlineData("~/.ssh/id_*", "~/.ssh/id_ecdsa", true)]
    [InlineData("~/.ssh/id_*", "~/.ssh/config", false)]
    [InlineData("*.env", "production.env", true)]
    [InlineData("*.env", ".env", false)] // * doesn't match empty
    [InlineData("*.log", "app.log", true)]
    [InlineData("*.log", "app.txt", false)]
    public void Should_Match_Single_Glob(string pattern, string path, bool expected)
    {
        // Act
        var result = _matcher.Matches(pattern, path);

        // Assert
        result.Should().Be(
            expected,
            because: $"single glob '{pattern}' should{(expected ? string.Empty : " not")} match '{path}'");
    }

    [Theory]
    [InlineData("**/.env", ".env", true)]
    [InlineData("**/.env", "src/.env", true)]
    [InlineData("**/.env", "src/config/.env", true)]
    [InlineData("**/.env", "deeply/nested/path/.env", true)]
    [InlineData("**/.env", ".env.local", false)]
    [InlineData("**/node_modules/**", "node_modules/package/index.js", true)]
    [InlineData("**/node_modules/**", "src/node_modules/dep/lib.js", true)]
    public void Should_Match_Double_Glob(string pattern, string path, bool expected)
    {
        // Act
        var result = _matcher.Matches(pattern, path);

        // Assert
        result.Should().Be(
            expected,
            because: $"double glob '{pattern}' should{(expected ? string.Empty : " not")} match '{path}'");
    }

    [Theory]
    [InlineData("id_?sa", "id_rsa", true)]
    [InlineData("id_?sa", "id_dsa", true)]
    [InlineData("id_?sa", "id_ecdsa", false)] // ? matches exactly one char
    [InlineData("?.env", "a.env", true)]
    [InlineData("?.env", ".env", false)]
    public void Should_Match_Question_Mark(string pattern, string path, bool expected)
    {
        // Act
        var result = _matcher.Matches(pattern, path);

        // Assert
        result.Should().Be(
            expected,
            because: $"question mark pattern '{pattern}' should{(expected ? string.Empty : " not")} match '{path}'");
    }

    [Theory]
    [InlineData("[abc].txt", "a.txt", true)]
    [InlineData("[abc].txt", "b.txt", true)]
    [InlineData("[abc].txt", "d.txt", false)]
    [InlineData("[!abc].txt", "d.txt", true)]
    [InlineData("[!abc].txt", "a.txt", false)]
    public void Should_Match_Character_Class(string pattern, string path, bool expected)
    {
        // Act
        var result = _matcher.Matches(pattern, path);

        // Assert
        result.Should().Be(
            expected,
            because: $"character class '{pattern}' should{(expected ? string.Empty : " not")} match '{path}'");
    }

    [Theory]
    [InlineData("[a-z].txt", "m.txt", true)]
    [InlineData("[a-z].txt", "5.txt", false)]
    [InlineData("[0-9].log", "5.log", true)]
    [InlineData("[0-9].log", "a.log", false)]
    [InlineData("[a-zA-Z].txt", "M.txt", true)]
    public void Should_Match_Character_Range(string pattern, string path, bool expected)
    {
        // Act
        var result = _matcher.Matches(pattern, path);

        // Assert
        result.Should().Be(
            expected,
            because: $"character range '{pattern}' should{(expected ? string.Empty : " not")} match '{path}'");
    }

    [Theory]
    [InlineData("C:\\Windows\\", "c:\\windows\\system32")]
    [InlineData(".ENV", ".env")]
    [InlineData("~/.SSH/", "~/.ssh/id_rsa")]
    public void Should_Be_Case_Insensitive_On_Windows(string pattern, string path)
    {
        // Act
        var result = _caseInsensitiveMatcher.Matches(pattern, path);

        // Assert
        result.Should().BeTrue(
            because: "Windows paths should be case-insensitive");
    }

    [Theory]
    [InlineData(".ENV", ".env", false)]
    [InlineData("~/.SSH/", "~/.ssh/id_rsa", false)]
    [InlineData("~/.ssh/", "~/.ssh/id_rsa", true)]
    public void Should_Be_Case_Sensitive_On_Unix(string pattern, string path, bool expected)
    {
        // Act
        var result = _caseSensitiveMatcher.Matches(pattern, path);

        // Assert
        result.Should().Be(
            expected,
            because: "Unix paths should be case-sensitive");
    }

    [Theory]
    [InlineData("~/.ssh/", "~/.ssh")]
    [InlineData("~/.ssh", "~/.ssh/")]
    public void Should_Handle_Trailing_Slash(string pattern, string path)
    {
        // Act
        var result = _matcher.Matches(pattern, path);

        // Assert
        result.Should().BeTrue(
            because: "trailing slash variations should be handled");
    }

    [Theory]
    [InlineData("~/.ssh//id_rsa", "~/.ssh/id_rsa")]
    [InlineData("~/.ssh/id_rsa", "~/.ssh//id_rsa")]
    public void Should_Handle_Multiple_Slashes(string pattern, string path)
    {
        // Act
        var result = _matcher.Matches(pattern, path);

        // Assert
        result.Should().BeTrue(
            because: "multiple consecutive slashes should be normalized");
    }

    [Fact]
    public void Should_Not_Backtrack()
    {
        // Arrange - pathological pattern that causes exponential backtracking in naive implementations
        var pattern = "a]]]]]]***********************b";
        var path = "a]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]c";

        // Act
        var sw = Stopwatch.StartNew();
        _ = _matcher.Matches(pattern, path);
        sw.Stop();

        // Assert - should complete quickly even with pathological input
        sw.ElapsedMilliseconds.Should().BeLessThan(
            100,
            because: "glob matcher must use linear-time algorithm to prevent ReDoS");
    }

    [Fact]
    public void Should_Complete_In_Under_1ms()
    {
        // Arrange
        var testCases = new[]
        {
            ("~/.ssh/id_*", "~/.ssh/id_rsa"),
            ("**/.env", "src/config/.env"),
            ("~/.aws/credentials", "~/.aws/credentials"),
            ("C:\\Windows\\System32\\**", "C:\\Windows\\System32\\drivers\\etc\\hosts")
        };

        foreach (var (pattern, path) in testCases)
        {
            // Warm up
            _ = _matcher.Matches(pattern, path);

            // Act
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                _ = _matcher.Matches(pattern, path);
            }

            sw.Stop();

            // Assert
            var avgMs = sw.Elapsed.TotalMilliseconds / 1000;
            avgMs.Should().BeLessThan(
                1,
                because: $"single path check for '{pattern}' should complete in under 1ms");
        }
    }
}
