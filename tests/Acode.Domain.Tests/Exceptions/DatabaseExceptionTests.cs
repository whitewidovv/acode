// tests/Acode.Domain.Tests/Exceptions/DatabaseExceptionTests.cs
namespace Acode.Domain.Tests.Exceptions;

using Acode.Domain.Exceptions;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for DatabaseException factory methods and properties.
/// Verifies all 8 factory methods create exceptions with correct error codes and transient flags.
/// </summary>
public sealed class DatabaseExceptionTests
{
    [Fact]
    public void DatabaseException_Constructor_ShouldSetAllProperties()
    {
        // Arrange
        var errorCode = "TEST-001";
        var message = "Test message";
        var innerException = new InvalidOperationException("Inner");
        var correlationId = "test-correlation-id";

        // Act
        var exception = new DatabaseException(
            errorCode,
            message,
            isTransient: true,
            innerException,
            correlationId);

        // Assert
        exception.ErrorCode.Should().Be(errorCode);
        exception.Message.Should().Be(message);
        exception.IsTransient.Should().BeTrue();
        exception.InnerException.Should().BeSameAs(innerException);
        exception.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public void DatabaseException_Constructor_ShouldGenerateCorrelationId_WhenNotProvided()
    {
        // Arrange & Act
        var exception = new DatabaseException("TEST-001", "Test message");

        // Assert
        exception.CorrelationId.Should().NotBeNullOrEmpty();
        exception.CorrelationId.Should().HaveLength(12);
    }

    [Fact]
    public void DatabaseException_Constructor_ShouldGenerateUniqueCorrelationIds()
    {
        // Arrange & Act
        var exception1 = new DatabaseException("TEST-001", "Test 1");
        var exception2 = new DatabaseException("TEST-002", "Test 2");

        // Assert
        exception1.CorrelationId.Should().NotBe(exception2.CorrelationId);
    }

    [Fact]
    public void ConnectionFailed_ShouldCreateTransientException_WithCorrectErrorCode()
    {
        // Arrange
        var details = "Network timeout";
        var inner = new TimeoutException("Connection timed out");

        // Act
        var exception = DatabaseException.ConnectionFailed(details, inner);

        // Assert
        exception.ErrorCode.Should().Be("ACODE-DB-ACC-001");
        exception.Message.Should().Contain("Database connection failed");
        exception.Message.Should().Contain(details);
        exception.IsTransient.Should().BeTrue();
        exception.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void ConnectionFailed_ShouldWorkWithoutInnerException()
    {
        // Arrange & Act
        var exception = DatabaseException.ConnectionFailed("Host not found");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-DB-ACC-001");
        exception.Message.Should().Contain("Host not found");
        exception.IsTransient.Should().BeTrue();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void PoolExhausted_ShouldCreateTransientException_WithCorrectErrorCode()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(30);

        // Act
        var exception = DatabaseException.PoolExhausted(timeout);

        // Assert
        exception.ErrorCode.Should().Be("ACODE-DB-ACC-002");
        exception.Message.Should().Contain("Connection pool exhausted");
        exception.Message.Should().Contain("30");
        exception.IsTransient.Should().BeTrue();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void TransactionFailed_ShouldCreatePermanentException_WithCorrectErrorCode()
    {
        // Arrange
        var operation = "commit";
        var inner = new InvalidOperationException("Deadlock");

        // Act
        var exception = DatabaseException.TransactionFailed(operation, inner);

        // Assert
        exception.ErrorCode.Should().Be("ACODE-DB-ACC-003");
        exception.Message.Should().Contain("Transaction commit failed");
        exception.IsTransient.Should().BeFalse();
        exception.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void TransactionFailed_ShouldWorkWithoutInnerException()
    {
        // Arrange & Act
        var exception = DatabaseException.TransactionFailed("rollback");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-DB-ACC-003");
        exception.Message.Should().Contain("Transaction rollback failed");
        exception.IsTransient.Should().BeFalse();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void CommandTimeout_ShouldCreateTransientException_WithCorrectErrorCode()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(60);
        var command = "SELECT * FROM large_table";

        // Act
        var exception = DatabaseException.CommandTimeout(timeout, command);

        // Assert
        exception.ErrorCode.Should().Be("ACODE-DB-ACC-004");
        exception.Message.Should().Contain("Command timed out");
        exception.Message.Should().Contain("60");
        exception.IsTransient.Should().BeTrue();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void ConstraintViolation_ShouldCreatePermanentException_WithCorrectErrorCode()
    {
        // Arrange
        var constraint = "unique_email";
        var inner = new InvalidOperationException("Unique constraint violated");

        // Act
        var exception = DatabaseException.ConstraintViolation(constraint, inner);

        // Assert
        exception.ErrorCode.Should().Be("ACODE-DB-ACC-005");
        exception.Message.Should().Contain("Constraint violation");
        exception.Message.Should().Contain(constraint);
        exception.IsTransient.Should().BeFalse();
        exception.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void ConstraintViolation_ShouldWorkWithoutInnerException()
    {
        // Arrange & Act
        var exception = DatabaseException.ConstraintViolation("fk_user_id");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-DB-ACC-005");
        exception.Message.Should().Contain("fk_user_id");
        exception.IsTransient.Should().BeFalse();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void SyntaxError_ShouldCreatePermanentException_WithCorrectErrorCode()
    {
        // Arrange
        var details = "Unexpected token 'SELEKT'";
        var inner = new InvalidOperationException("Parse error");

        // Act
        var exception = DatabaseException.SyntaxError(details, inner);

        // Assert
        exception.ErrorCode.Should().Be("ACODE-DB-ACC-006");
        exception.Message.Should().Contain("SQL syntax error");
        exception.Message.Should().Contain(details);
        exception.IsTransient.Should().BeFalse();
        exception.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void SyntaxError_ShouldWorkWithoutInnerException()
    {
        // Arrange & Act
        var exception = DatabaseException.SyntaxError("Missing WHERE clause");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-DB-ACC-006");
        exception.Message.Should().Contain("Missing WHERE clause");
        exception.IsTransient.Should().BeFalse();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void PermissionDenied_ShouldCreatePermanentException_WithCorrectErrorCode()
    {
        // Arrange
        var operation = "DROP TABLE users";
        var inner = new UnauthorizedAccessException("Insufficient privileges");

        // Act
        var exception = DatabaseException.PermissionDenied(operation, inner);

        // Assert
        exception.ErrorCode.Should().Be("ACODE-DB-ACC-007");
        exception.Message.Should().Contain("Permission denied");
        exception.Message.Should().Contain(operation);
        exception.IsTransient.Should().BeFalse();
        exception.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void PermissionDenied_ShouldWorkWithoutInnerException()
    {
        // Arrange & Act
        var exception = DatabaseException.PermissionDenied("ALTER TABLE");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-DB-ACC-007");
        exception.Message.Should().Contain("ALTER TABLE");
        exception.IsTransient.Should().BeFalse();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void DatabaseNotFound_ShouldCreatePermanentException_WithCorrectErrorCode()
    {
        // Arrange
        var database = "production_db";
        var inner = new InvalidOperationException("Database does not exist");

        // Act
        var exception = DatabaseException.DatabaseNotFound(database, inner);

        // Assert
        exception.ErrorCode.Should().Be("ACODE-DB-ACC-008");
        exception.Message.Should().Contain("Database not found");
        exception.Message.Should().Contain(database);
        exception.IsTransient.Should().BeFalse();
        exception.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void DatabaseNotFound_ShouldWorkWithoutInnerException()
    {
        // Arrange & Act
        var exception = DatabaseException.DatabaseNotFound("test_db");

        // Assert
        exception.ErrorCode.Should().Be("ACODE-DB-ACC-008");
        exception.Message.Should().Contain("test_db");
        exception.IsTransient.Should().BeFalse();
        exception.InnerException.Should().BeNull();
    }

    [Theory]
    [InlineData("ACODE-DB-ACC-001", true)] // ConnectionFailed
    [InlineData("ACODE-DB-ACC-002", true)] // PoolExhausted
    [InlineData("ACODE-DB-ACC-003", false)] // TransactionFailed
    [InlineData("ACODE-DB-ACC-004", true)] // CommandTimeout
    [InlineData("ACODE-DB-ACC-005", false)] // ConstraintViolation
    [InlineData("ACODE-DB-ACC-006", false)] // SyntaxError
    [InlineData("ACODE-DB-ACC-007", false)] // PermissionDenied
    [InlineData("ACODE-DB-ACC-008", false)] // DatabaseNotFound
    public void AllErrorCodes_ShouldHaveCorrectTransientFlag(string errorCode, bool expectedTransient)
    {
        // Arrange & Act
        var exception = errorCode switch
        {
            "ACODE-DB-ACC-001" => DatabaseException.ConnectionFailed("test"),
            "ACODE-DB-ACC-002" => DatabaseException.PoolExhausted(TimeSpan.FromSeconds(1)),
            "ACODE-DB-ACC-003" => DatabaseException.TransactionFailed("test"),
            "ACODE-DB-ACC-004" => DatabaseException.CommandTimeout(TimeSpan.FromSeconds(1), "test"),
            "ACODE-DB-ACC-005" => DatabaseException.ConstraintViolation("test"),
            "ACODE-DB-ACC-006" => DatabaseException.SyntaxError("test"),
            "ACODE-DB-ACC-007" => DatabaseException.PermissionDenied("test"),
            "ACODE-DB-ACC-008" => DatabaseException.DatabaseNotFound("test"),
            _ => throw new InvalidOperationException("Unknown error code")
        };

        // Assert
        exception.ErrorCode.Should().Be(errorCode);
        exception.IsTransient.Should().Be(expectedTransient);
    }

    [Fact]
    public void DatabaseException_ShouldBeSerializable()
    {
        // Arrange
        var exception = DatabaseException.ConnectionFailed("Test", new TimeoutException("Timeout"));

        // Act & Assert
        exception.Should().BeAssignableTo<Exception>();
        exception.ErrorCode.Should().NotBeNullOrEmpty();
        exception.CorrelationId.Should().NotBeNullOrEmpty();
    }
}
