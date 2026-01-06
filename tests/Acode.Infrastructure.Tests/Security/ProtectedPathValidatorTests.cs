using Acode.Application.Security;
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
        _validator = new ProtectedPathValidator();
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
}
