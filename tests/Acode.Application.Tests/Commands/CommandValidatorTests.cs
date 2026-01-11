using Acode.Application.Commands;
using Acode.Domain.Commands;
using FluentAssertions;

namespace Acode.Application.Tests.Commands;

/// <summary>
/// Tests for CommandValidator implementation.
/// Covers validation requirements per Task 002.c spec lines 826-851 (UT-002c-10 through UT-002c-15).
/// </summary>
public class CommandValidatorTests
{
    private readonly ICommandValidator _validator = new CommandValidator();
    private readonly string _repositoryRoot = "/repo";

    // UT-002c-10: Validate working directory → Path validated
    [Fact]
    public void ValidateWorkingDirectory_WithValidRelativePath_ReturnsSuccess()
    {
        // Arrange
        var cwd = "src/app";

        // Act
        var result = _validator.ValidateWorkingDirectory(cwd, _repositoryRoot);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateWorkingDirectory_WithCurrentDirectory_ReturnsSuccess()
    {
        // Arrange
        var cwd = ".";

        // Act
        var result = _validator.ValidateWorkingDirectory(cwd, _repositoryRoot);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    // UT-002c-11: Reject absolute path → Returns error
    [Fact]
    public void ValidateWorkingDirectory_WithAbsolutePath_ReturnsFailure()
    {
        // Arrange
        var cwd = "/absolute/path";

        // Act
        var result = _validator.ValidateWorkingDirectory(cwd, _repositoryRoot);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("absolute path");
    }

    [Fact]
    public void ValidateWorkingDirectory_WithWindowsAbsolutePath_ReturnsFailure()
    {
        // Arrange
        var cwd = "C:\\absolute\\path";

        // Act
        var result = _validator.ValidateWorkingDirectory(cwd, _repositoryRoot);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("absolute path");
    }

    // UT-002c-12: Reject path traversal → Returns error
    [Fact]
    public void ValidateWorkingDirectory_WithPathTraversal_ReturnsFailure()
    {
        // Arrange
        var cwd = "../outside";

        // Act
        var result = _validator.ValidateWorkingDirectory(cwd, _repositoryRoot);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("path traversal");
    }

    [Fact]
    public void ValidateWorkingDirectory_WithPathTraversalInMiddle_ReturnsFailure()
    {
        // Arrange
        var cwd = "src/../../../etc";

        // Act
        var result = _validator.ValidateWorkingDirectory(cwd, _repositoryRoot);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("path traversal");
    }

    // UT-002c-14: Validate timeout value → Positive integer required
    [Fact]
    public void ValidateTimeout_WithPositiveValue_ReturnsSuccess()
    {
        // Arrange
        var timeout = 300;

        // Act
        var result = _validator.ValidateTimeout(timeout);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateTimeout_WithZero_ReturnsSuccess()
    {
        // Arrange
        var timeout = 0;

        // Act
        var result = _validator.ValidateTimeout(timeout);

        // Assert
        result.IsValid.Should().BeTrue("zero means no timeout");
    }

    [Fact]
    public void ValidateTimeout_WithNegativeValue_ReturnsFailure()
    {
        // Arrange
        var timeout = -1;

        // Act
        var result = _validator.ValidateTimeout(timeout);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("non-negative");
    }

    // UT-002c-15: Validate retry value → Non-negative integer required
    [Fact]
    public void ValidateRetry_WithZero_ReturnsSuccess()
    {
        // Arrange
        var retry = 0;

        // Act
        var result = _validator.ValidateRetry(retry);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateRetry_WithPositiveValue_ReturnsSuccess()
    {
        // Arrange
        var retry = 3;

        // Act
        var result = _validator.ValidateRetry(retry);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateRetry_WithNegativeValue_ReturnsFailure()
    {
        // Arrange
        var retry = -1;

        // Act
        var result = _validator.ValidateRetry(retry);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("non-negative");
    }

    [Fact]
    public void ValidateRetry_WithValueGreaterThan10_ReturnsFailure()
    {
        // Arrange
        var retry = 11;

        // Act
        var result = _validator.ValidateRetry(retry);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("maximum");
    }

    // UT-002c-13: Parse environment variables → Env vars extracted
    [Fact]
    public void ValidateEnvironment_WithValidVariables_ReturnsSuccess()
    {
        // Arrange
        var env = new Dictionary<string, string>
        {
            { "NODE_ENV", "production" },
            { "API_KEY", "secret" }
        };

        // Act
        var result = _validator.ValidateEnvironment(env);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateEnvironment_WithEmptyDictionary_ReturnsSuccess()
    {
        // Arrange
        var env = new Dictionary<string, string>();

        // Act
        var result = _validator.ValidateEnvironment(env);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateEnvironment_WithEmptyValue_ReturnsSuccess()
    {
        // Arrange
        var env = new Dictionary<string, string>
        {
            { "EMPTY_VAR", string.Empty }
        };

        // Act
        var result = _validator.ValidateEnvironment(env);

        // Assert
        result.IsValid.Should().BeTrue("empty values are allowed");
    }

    [Fact]
    public void ValidateEnvironment_WithInvalidName_ReturnsFailure()
    {
        // Arrange - env var names cannot contain =
        var env = new Dictionary<string, string>
        {
            { "INVALID=NAME", "value" }
        };

        // Act
        var result = _validator.ValidateEnvironment(env);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().ContainEquivalentOf("invalid");
    }

    [Fact]
    public void Validate_WithValidSpec_ReturnsSuccess()
    {
        // Arrange
        var spec = new CommandSpec
        {
            Run = "npm test",
            Cwd = "src",
            Timeout = 300,
            Retry = 2,
            Env = new Dictionary<string, string> { { "NODE_ENV", "test" } }
        };

        // Act
        var result = _validator.Validate(spec, _repositoryRoot);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithInvalidWorkingDirectory_ReturnsFailure()
    {
        // Arrange
        var spec = new CommandSpec
        {
            Run = "npm test",
            Cwd = "../outside"
        };

        // Act
        var result = _validator.Validate(spec, _repositoryRoot);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("working directory");
    }

    [Fact]
    public void Validate_WithInvalidTimeout_ReturnsFailure()
    {
        // Arrange
        var spec = new CommandSpec
        {
            Run = "npm test",
            Timeout = -1
        };

        // Act
        var result = _validator.Validate(spec, _repositoryRoot);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("timeout");
    }

    [Fact]
    public void Validate_WithInvalidRetry_ReturnsFailure()
    {
        // Arrange
        var spec = new CommandSpec
        {
            Run = "npm test",
            Retry = -1
        };

        // Act
        var result = _validator.Validate(spec, _repositoryRoot);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("retry");
    }

    [Fact]
    public void Validate_WithInvalidEnvironment_ReturnsFailure()
    {
        // Arrange
        var spec = new CommandSpec
        {
            Run = "npm test",
            Env = new Dictionary<string, string>
            {
                { "INVALID=VAR", "value" }
            }
        };

        // Act
        var result = _validator.Validate(spec, _repositoryRoot);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("environment");
    }
}
