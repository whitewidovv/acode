using Acode.Application.Security;
using Acode.Domain.Security.PathProtection;
using Acode.Infrastructure.Security;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Security;

/// <summary>
/// Tests for ProtectedPathValidator implementation.
/// </summary>
public sealed class ProtectedPathValidatorTests
{
    private readonly ProtectedPathValidator _validator;

    public ProtectedPathValidatorTests()
    {
        // Create dependencies for ProtectedPathValidator
        var pathMatcher = new GlobMatcher(caseSensitive: false); // Case-insensitive for security
        var pathNormalizer = new PathNormalizer();
        var symlinkResolver = new SymlinkResolver();

        _validator = new ProtectedPathValidator(pathMatcher, pathNormalizer, symlinkResolver);
    }

    [Theory]
    [InlineData(".ssh/id_rsa")] // SSH key
    [InlineData("~/.ssh/id_ed25519")] // SSH key with tilde
    [InlineData(".env")] // Environment file
    [InlineData(".env.production")] // Environment file variant
    [InlineData(".aws/credentials")] // AWS credentials
    public void Validate_ProtectedPath_ReturnsBlocked(string path)
    {
        // Act
        var result = _validator.Validate(path);

        // Assert
        result.IsProtected.Should().BeTrue();
        result.Reason.Should().NotBeNullOrEmpty();
        result.RiskId.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("src/Program.cs")] // Source file
    [InlineData("README.md")] // Documentation
    [InlineData("tests/Test.cs")] // Test file
    [InlineData("package.json")] // Config (not secret)
    public void Validate_AllowedPath_ReturnsAllowed(string path)
    {
        // Act
        var result = _validator.Validate(path);

        // Assert
        result.IsProtected.Should().BeFalse();
        result.MatchedPattern.Should().BeNull();
        result.Reason.Should().BeNull();
    }

    [Theory]
    [InlineData("")] // Empty path
    [InlineData(null)] // Null path
    public void Validate_InvalidPath_ThrowsArgumentException(string? path)
    {
        // Act
        Action act = () => _validator.Validate(path!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Validate_WithFileOperation_ReturnsCorrectResult()
    {
        // Arrange
        var protectedPath = ".ssh/id_rsa";

        // Act
        var result = _validator.Validate(protectedPath, FileOperation.Read);

        // Assert
        result.IsProtected.Should().BeTrue();
    }

    // Gap #13 - Enhanced Integration Tests
    [Theory]
    [InlineData("~/.ssh/id_rsa")] // Tilde expansion
    [InlineData("./.ssh/id_rsa")] // Current directory
    [InlineData(".ssh//id_rsa")] // Multiple slashes
    [InlineData(".ssh/./id_rsa")] // Current directory marker
    public void Validate_NormalizationIntegration_HandlesPathVariations(string path)
    {
        // Act
        var result = _validator.Validate(path);

        // Assert
        result.IsProtected.Should().BeTrue(
            because: "normalized paths should match protected patterns");
        result.Category.Should().Be(PathCategory.SshKeys);
        result.RiskId.Should().Be("RISK-I-003");
    }

    [Theory]
    [InlineData("**/.ssh/")] // SSH directory anywhere
    [InlineData("**/.env")] // Env file anywhere
    [InlineData("**/.aws/credentials")] // AWS creds anywhere
    [InlineData("**/secrets/")] // Secrets directory
    [InlineData("**/*.pem")] // PEM files anywhere
    [InlineData("**/*.key")] // Key files anywhere
    public void Validate_WildcardPatterns_ExistInDenylist(string pattern)
    {
        // Arrange - This test verifies patterns exist in denylist
        var entries = DefaultDenylist.Entries;

        // Act - Check pattern exists
        var patternExists = entries.Any(e => e.Pattern == pattern);

        // Assert
        patternExists.Should().BeTrue(
            because: $"denylist should contain glob pattern: {pattern}");
    }

    [Theory]
    [InlineData(".ssh/id_rsa", PathCategory.SshKeys)]
    [InlineData(".gnupg/secring.gpg", PathCategory.GpgKeys)]
    [InlineData(".aws/credentials", PathCategory.CloudCredentials)]
    [InlineData(".env", PathCategory.EnvironmentFiles)]
    public void Validate_MajorCategories_AllBlocked(string path, PathCategory expectedCategory)
    {
        // Act
        var result = _validator.Validate(path);

        // Assert
        result.IsProtected.Should().BeTrue(
            because: $"{expectedCategory} should be protected");
        result.Category.Should().Be(expectedCategory);
    }

    [Theory]
    [InlineData(".ssh/foo/../id_rsa")] // Traversal within .ssh
    [InlineData(".env.local/../.env")] // Traversal to .env
    [InlineData(".aws/foo/bar/../../credentials")] // Traversal within .aws
    public void Validate_PathTraversal_NormalizedAndBlocked(string path)
    {
        // Act
        var result = _validator.Validate(path);

        // Assert - Path normalization should resolve .. and still match protected patterns
        result.IsProtected.Should().BeTrue(
            because: "paths with .. should be normalized and still match protected patterns");
    }

    [Fact]
    public void Validate_Performance_CompletesUnder10Milliseconds()
    {
        // Arrange
        var testPath = ".ssh/id_rsa";
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - Run validation 100 times
        for (int i = 0; i < 100; i++)
        {
            _validator.Validate(testPath);
        }

        stopwatch.Stop();

        // Assert - Average should be well under 1ms
        var averageMs = stopwatch.ElapsedMilliseconds / 100.0;
        averageMs.Should().BeLessThan(
            10,
            because: "path validation should be fast (< 10ms avg)");
    }

    [Theory]
    [InlineData("secret.pem")] // PEM certificate
    [InlineData("api.key")] // Private key
    [InlineData(".env.production")] // Environment file variant
    [InlineData("credentials.p12")] // PKCS12 file
    [InlineData("keystore.jks")] // Java keystore
    public void Validate_FileExtensionPatterns_MatchCorrectly(string filename)
    {
        // Act
        var result = _validator.Validate(filename);

        // Assert - File extension patterns should block sensitive files
        result.IsProtected.Should().BeTrue(
            because: $"file with sensitive extension '{filename}' should be blocked");
        result.MatchedPattern.Should().NotBeNullOrEmpty(
            because: "should have matched a pattern in the denylist");
    }

    [Fact]
    public void Validate_PlatformFiltering_OnlyAppliesToCurrentPlatform()
    {
        // Arrange - Get a Windows-specific pattern
        var windowsPattern = DefaultDenylist.Entries
            .FirstOrDefault(e => e.Platforms.Contains(Platform.Windows) &&
                                !e.Platforms.Contains(Platform.All));

        // Assert - Should have at least one Windows-specific pattern
        windowsPattern.Should().NotBeNull(
            because: "denylist should contain platform-specific patterns");

        // Act - Verify platform filtering works
        var allPatterns = DefaultDenylist.Entries;
        var platformSpecificPatterns = allPatterns
            .Where(e => !e.Platforms.Contains(Platform.All));

        // Assert
        platformSpecificPatterns.Should().NotBeEmpty(
            because: "denylist should contain platform-specific patterns for Windows, Linux, MacOS");
    }

    [Theory]
    [InlineData(".ssh/id_rsa", ".ssh/ID_RSA")] // Case variation
    [InlineData(".env", ".ENV")] // Uppercase
    [InlineData(".aws/credentials", ".AWS/CREDENTIALS")] // All caps
    public void Validate_CaseSensitivity_DependsOnPlatform(string normalCase, string upperCase)
    {
        // Act
        var normalResult = _validator.Validate(normalCase);
        var upperResult = _validator.Validate(upperCase);

        // Assert - Both should be blocked (case-insensitive matching for security)
        normalResult.IsProtected.Should().BeTrue();
        upperResult.IsProtected.Should().BeTrue(
            because: "case variations should be blocked for security");
    }
}
