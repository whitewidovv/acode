namespace Acode.Infrastructure.Tests.Tools;

using Acode.Application.Tools.Retry;
using Acode.Domain.Tools;
using Acode.Infrastructure.Tools;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for RetryTracker implementation.
/// FR-007b: Validation error retry contract.
/// </summary>
public sealed class RetryTrackerTests
{
    private readonly RetryConfiguration config;
    private readonly RetryTracker sut;

    public RetryTrackerTests()
    {
        this.config = RetryConfiguration.Default;
        this.sut = new RetryTracker(this.config);
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new RetryTracker(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetAttemptCount_WithNoAttempts_ReturnsZero()
    {
        // Act
        var count = this.sut.GetAttemptCount("tool-call-123");

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void RecordAttempt_FirstAttempt_SetsCountToOne()
    {
        // Arrange
        var errors = new[] { new SchemaValidationError("path", "VAL-001", "Error", ErrorSeverity.Error) };

        // Act
        this.sut.RecordAttempt("tool-call-123", errors);

        // Assert
        this.sut.GetAttemptCount("tool-call-123").Should().Be(1);
    }

    [Fact]
    public void RecordAttempt_MultipleAttempts_IncrementsCount()
    {
        // Arrange
        var errors = new[] { new SchemaValidationError("path", "VAL-001", "Error", ErrorSeverity.Error) };

        // Act
        this.sut.RecordAttempt("tool-call-123", errors);
        this.sut.RecordAttempt("tool-call-123", errors);
        this.sut.RecordAttempt("tool-call-123", errors);

        // Assert
        this.sut.GetAttemptCount("tool-call-123").Should().Be(3);
    }

    [Fact]
    public void GetHistory_WithNoAttempts_ReturnsEmptyList()
    {
        // Act
        var history = this.sut.GetHistory("tool-call-123");

        // Assert
        history.Should().BeEmpty();
    }

    [Fact]
    public void GetHistory_WithAttempts_ReturnsAllAttempts()
    {
        // Arrange
        var errors1 = new[] { new SchemaValidationError("path1", "VAL-001", "Error 1", ErrorSeverity.Error) };
        var errors2 = new[] { new SchemaValidationError("path2", "VAL-002", "Error 2", ErrorSeverity.Error) };

        // Act
        this.sut.RecordAttempt("tool-call-123", errors1);
        this.sut.RecordAttempt("tool-call-123", errors2);
        var history = this.sut.GetHistory("tool-call-123");

        // Assert
        history.Should().HaveCount(2);
        history[0].AttemptNumber.Should().Be(1);
        history[1].AttemptNumber.Should().Be(2);
    }

    [Fact]
    public void HasExceededMaxRetries_BelowLimit_ReturnsFalse()
    {
        // Arrange
        var errors = new[] { new SchemaValidationError("path", "VAL-001", "Error", ErrorSeverity.Error) };
        this.sut.RecordAttempt("tool-call-123", errors);
        this.sut.RecordAttempt("tool-call-123", errors);

        // Act & Assert (default is 3 retries)
        this.sut.HasExceededMaxRetries("tool-call-123").Should().BeFalse();
    }

    [Fact]
    public void HasExceededMaxRetries_AtLimit_ReturnsFalse()
    {
        // Arrange
        var errors = new[] { new SchemaValidationError("path", "VAL-001", "Error", ErrorSeverity.Error) };
        this.sut.RecordAttempt("tool-call-123", errors);
        this.sut.RecordAttempt("tool-call-123", errors);
        this.sut.RecordAttempt("tool-call-123", errors);

        // Act & Assert (exactly at limit of 3)
        this.sut.HasExceededMaxRetries("tool-call-123").Should().BeFalse();
    }

    [Fact]
    public void HasExceededMaxRetries_AboveLimit_ReturnsTrue()
    {
        // Arrange
        var errors = new[] { new SchemaValidationError("path", "VAL-001", "Error", ErrorSeverity.Error) };
        this.sut.RecordAttempt("tool-call-123", errors);
        this.sut.RecordAttempt("tool-call-123", errors);
        this.sut.RecordAttempt("tool-call-123", errors);
        this.sut.RecordAttempt("tool-call-123", errors);

        // Act & Assert (4 > 3, exceeded)
        this.sut.HasExceededMaxRetries("tool-call-123").Should().BeTrue();
    }

    [Fact]
    public void Clear_RemovesTrackingState()
    {
        // Arrange
        var errors = new[] { new SchemaValidationError("path", "VAL-001", "Error", ErrorSeverity.Error) };
        this.sut.RecordAttempt("tool-call-123", errors);
        this.sut.RecordAttempt("tool-call-123", errors);

        // Act
        this.sut.Clear("tool-call-123");

        // Assert
        this.sut.GetAttemptCount("tool-call-123").Should().Be(0);
        this.sut.GetHistory("tool-call-123").Should().BeEmpty();
    }

    [Fact]
    public void Clear_UnknownToolCall_DoesNotThrow()
    {
        // Act
        var act = () => this.sut.Clear("unknown-tool-call");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void TracksMultipleToolCallsIndependently()
    {
        // Arrange
        var errors = new[] { new SchemaValidationError("path", "VAL-001", "Error", ErrorSeverity.Error) };

        // Act
        this.sut.RecordAttempt("call-1", errors);
        this.sut.RecordAttempt("call-1", errors);
        this.sut.RecordAttempt("call-2", errors);

        // Assert
        this.sut.GetAttemptCount("call-1").Should().Be(2);
        this.sut.GetAttemptCount("call-2").Should().Be(1);
    }

    [Fact]
    public void RecordAttempt_PreservesTimestamp()
    {
        // Arrange
        var errors = new[] { new SchemaValidationError("path", "VAL-001", "Error", ErrorSeverity.Error) };
        var before = DateTimeOffset.UtcNow;

        // Act
        this.sut.RecordAttempt("tool-call-123", errors);
        var after = DateTimeOffset.UtcNow;

        // Assert
        var history = this.sut.GetHistory("tool-call-123");
        history[0].Timestamp.Should().BeOnOrAfter(before);
        history[0].Timestamp.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void RecordAttempt_PreservesErrors()
    {
        // Arrange
        var errors = new SchemaValidationError[]
        {
            new("path1", "VAL-001", "Error 1", ErrorSeverity.Error),
            new("path2", "VAL-002", "Error 2", ErrorSeverity.Warning)
        };

        // Act
        this.sut.RecordAttempt("tool-call-123", errors);
        var history = this.sut.GetHistory("tool-call-123");

        // Assert
        history[0].Errors.Should().HaveCount(2);
        history[0].Errors.Should().Contain(e => e.Path == "path1");
        history[0].Errors.Should().Contain(e => e.Path == "path2");
    }

    [Fact]
    public void Tracker_ImplementsIRetryTracker()
    {
        // Assert
        this.sut.Should().BeAssignableTo<IRetryTracker>();
    }

    [Fact]
    public void HasExceededMaxRetries_WithNoAttempts_ReturnsFalse()
    {
        // Act & Assert
        this.sut.HasExceededMaxRetries("nonexistent").Should().BeFalse();
    }
}
