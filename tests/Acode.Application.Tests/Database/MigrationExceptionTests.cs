// tests/Acode.Application.Tests/Database/MigrationExceptionTests.cs
namespace Acode.Application.Tests.Database;

using Acode.Application.Database;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for MigrationException error codes and factory methods.
/// Verifies structured error handling for all migration error scenarios.
/// </summary>
public sealed class MigrationExceptionTests
{
    [Fact]
    public void ExecutionFailed_ShouldCreateExceptionWithCorrectErrorCode()
    {
        // Arrange & Act
        var exception = MigrationException.ExecutionFailed("Failed to apply migration", null);

        // Assert
        exception.ErrorCode.Should().Be("ACODE-MIG-001");
        exception.Message.Should().Contain("Failed to apply migration");
    }

    [Fact]
    public void LockTimeout_ShouldCreateExceptionWithCorrectErrorCode()
    {
        // Arrange & Act
        var exception = MigrationException.LockTimeout(TimeSpan.FromSeconds(60));

        // Assert
        exception.ErrorCode.Should().Be("ACODE-MIG-002");
        exception.Message.Should().Contain("60");
    }

    [Fact]
    public void ChecksumMismatch_ShouldCreateExceptionWithCorrectErrorCode()
    {
        // Arrange & Act
        var exception = MigrationException.ChecksumMismatch("001", "abc123", "def456");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-MIG-003");
        exception.Message.Should().Contain("001");
        exception.Message.Should().Contain("abc123");
        exception.Message.Should().Contain("def456");
    }

    [Fact]
    public void MissingDownScript_ShouldCreateExceptionWithCorrectErrorCode()
    {
        // Arrange & Act
        var exception = MigrationException.MissingDownScript("005");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-MIG-004");
        exception.Message.Should().Contain("005");
    }

    [Fact]
    public void RollbackFailed_ShouldCreateExceptionWithCorrectErrorCode()
    {
        // Arrange & Act
        var exception = MigrationException.RollbackFailed("003", "Constraint violation", null);

        // Assert
        exception.ErrorCode.Should().Be("ACODE-MIG-005");
        exception.Message.Should().Contain("003");
        exception.Message.Should().Contain("Constraint violation");
    }

    [Fact]
    public void VersionGapDetected_ShouldCreateExceptionWithCorrectErrorCode()
    {
        // Arrange & Act
        var exception = MigrationException.VersionGapDetected(new[] { "001", "003" }, "002");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-MIG-006");
        exception.Message.Should().Contain("002");
    }

    [Fact]
    public void DatabaseConnectionFailed_ShouldCreateExceptionWithCorrectErrorCode()
    {
        // Arrange
        var innerException = new InvalidOperationException("Connection refused");

        // Act
        var exception = MigrationException.DatabaseConnectionFailed("Connection timeout", innerException);

        // Assert
        exception.ErrorCode.Should().Be("ACODE-MIG-007");
        exception.Message.Should().Contain("Connection timeout");
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void BackupFailed_ShouldCreateExceptionWithCorrectErrorCode()
    {
        // Arrange & Act
        var exception = MigrationException.BackupFailed("Insufficient disk space", null);

        // Assert
        exception.ErrorCode.Should().Be("ACODE-MIG-008");
        exception.Message.Should().Contain("Insufficient disk space");
    }

    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange & Act
        var exception = new MigrationException("ACODE-MIG-999", "Test message");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-MIG-999");
        exception.Message.Should().Be("Test message");
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithInnerException_ShouldSetInnerException()
    {
        // Arrange
        var innerException = new ArgumentException("Inner error");

        // Act
        var exception = new MigrationException("ACODE-MIG-999", "Test message", innerException);

        // Assert
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void ErrorCode_ShouldNotBeNull()
    {
        // Arrange & Act
        var exception = MigrationException.ExecutionFailed("test", null);

        // Assert
        exception.ErrorCode.Should().NotBeNullOrWhiteSpace();
    }
}
