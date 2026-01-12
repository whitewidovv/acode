using Acode.Application.Security;
using Acode.Application.Security.Commands;
using Acode.Domain.Security.PathProtection;
using Acode.Infrastructure.Security;
using FluentAssertions;

namespace Acode.Integration.Tests.Security;

/// <summary>
/// End-to-end integration tests for path protection validation flow.
/// Gap #26 - Integration tests for full validation flow.
/// </summary>
public sealed class PathProtectionIntegrationTests
{
    private readonly CheckPathHandler _handler;

    public PathProtectionIntegrationTests()
    {
        // Create full validator stack
        var pathMatcher = new GlobMatcher(caseSensitive: false);
        var pathNormalizer = new PathNormalizer();
        var symlinkResolver = new SymlinkResolver();
        var validator = new ProtectedPathValidator(pathMatcher, pathNormalizer, symlinkResolver);

        _handler = new CheckPathHandler(validator);
    }

    [Theory]
    [InlineData(".ssh/id_rsa")]
    [InlineData(".ssh/id_ed25519")]
    [InlineData("~/.ssh/authorized_keys")]
    public void EndToEnd_SshKeyPath_IsBlocked(string path)
    {
        // Arrange
        var command = new CheckPathCommand { Path = path };

        // Act
        var result = _handler.Handle(command);

        // Assert - SSH keys should be blocked
        result.IsProtected.Should().BeTrue(
            because: "SSH key paths should be blocked for security");
        result.Category.Should().Be(PathCategory.SshKeys);
        result.RiskId.Should().Be("RISK-I-003");
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().Be("ACODE-SEC-003-001");
    }

    [Theory]
    [InlineData("src/Program.cs")]
    [InlineData("README.md")]
    [InlineData("package.json")]
    [InlineData("tests/UnitTest.cs")]
    public void EndToEnd_NormalSourceFile_IsAllowed(string path)
    {
        // Arrange
        var command = new CheckPathCommand { Path = path };

        // Act
        var result = _handler.Handle(command);

        // Assert - Normal source files should be allowed
        result.IsProtected.Should().BeFalse(
            because: "normal source files should not be blocked");
        result.Error.Should().BeNull();
    }

    [Theory]
    [InlineData(".env")]
    [InlineData(".env.production")]
    [InlineData(".env.local")]
    public void EndToEnd_EnvironmentFile_IsBlocked(string path)
    {
        // Arrange
        var command = new CheckPathCommand { Path = path };

        // Act
        var result = _handler.Handle(command);

        // Assert - Environment files should be blocked
        result.IsProtected.Should().BeTrue(
            because: "environment files contain secrets");
        result.Category.Should().Be(PathCategory.EnvironmentFiles);
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().Be("ACODE-SEC-003-004");
    }

    [Theory]
    [InlineData(".ssh/foo/../id_rsa")]
    [InlineData(".aws/bar/../credentials")]
    public void EndToEnd_DirectoryTraversal_IsBlocked(string path)
    {
        // Arrange
        var command = new CheckPathCommand { Path = path };

        // Act - Path normalization should resolve .. and still block
        var result = _handler.Handle(command);

        // Assert - Traversal attacks should be normalized and blocked
        result.IsProtected.Should().BeTrue(
            because: "directory traversal should be normalized and detected");
        result.Error.Should().NotBeNull();
    }

    [Fact]
    public void EndToEnd_WithFileOperation_ValidatesCorrectly()
    {
        // Arrange - Check path with specific operation
        var command = new CheckPathCommand
        {
            Path = ".ssh/id_rsa",
            Operation = FileOperation.Read
        };

        // Act
        var result = _handler.Handle(command);

        // Assert - Operation-specific validation should work
        result.IsProtected.Should().BeTrue(
            because: "SSH keys should be blocked regardless of operation");
        result.Error.Should().NotBeNull();
    }

    [Theory]
    [InlineData("secret.pem")]
    [InlineData("api.key")]
    [InlineData("credentials.p12")]
    public void EndToEnd_SecretFileExtensions_AreBlocked(string path)
    {
        // Arrange
        var command = new CheckPathCommand { Path = path };

        // Act
        var result = _handler.Handle(command);

        // Assert - Secret file extensions should be blocked
        result.IsProtected.Should().BeTrue(
            because: "files with secret extensions should be blocked");
        result.Category.Should().Be(PathCategory.SecretFiles);
        result.Error.Should().NotBeNull();
    }

    [Theory]
    [InlineData(".aws/credentials")]
    [InlineData(".gcloud/credentials.json")]
    [InlineData(".azure/accessTokens.json")]
    public void EndToEnd_CloudCredentials_AreBlocked(string path)
    {
        // Arrange
        var command = new CheckPathCommand { Path = path };

        // Act
        var result = _handler.Handle(command);

        // Assert - Cloud credentials should be blocked
        result.IsProtected.Should().BeTrue(
            because: "cloud credentials should be protected");
        result.Category.Should().Be(PathCategory.CloudCredentials);
        result.RiskId.Should().Be("RISK-I-003");
        result.Error.Should().NotBeNull();
    }
}
