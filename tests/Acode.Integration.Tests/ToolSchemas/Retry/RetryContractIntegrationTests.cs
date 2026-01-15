namespace Acode.Integration.Tests.ToolSchemas.Retry;

using Acode.Application.ToolSchemas.Retry;
using Acode.Infrastructure.ToolSchemas.Retry;
using FluentAssertions;

/// <summary>
/// Integration tests verifying the full retry contract flow.
/// </summary>
/// <remarks>
/// Spec Reference: Testing Requirements lines 2299-2506.
/// Tests full flow: validate → track → format → create ToolResult.
/// </remarks>
public sealed class RetryContractIntegrationTests
{
    private readonly RetryConfiguration config;
    private readonly ErrorFormatter formatter;
    private readonly RetryTracker tracker;
    private readonly EscalationFormatter escalationFormatter;

    public RetryContractIntegrationTests()
    {
        this.config = new RetryConfiguration
        {
            MaxAttempts = 3,
            MaxMessageLength = 2000,
            MaxErrorsShown = 10,
            MaxValuePreview = 100,
            IncludeHints = true,
            IncludeActualValues = true,
            RedactSecrets = true,
            RelativizePaths = true,
        };
        this.formatter = new ErrorFormatter(this.config);
        this.tracker = new RetryTracker(this.config.MaxAttempts);
        this.escalationFormatter = new EscalationFormatter();
    }

    [Fact]
    public void Should_Format_Error_For_ToolResult()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new()
            {
                ErrorCode = ErrorCode.RequiredFieldMissing,
                FieldPath = "/path",
                Message = "Field 'path' is required",
                Severity = ErrorSeverity.Error,
            },
        };

        // Act
        var formatted = this.formatter.FormatErrors("read_file", errors, 1, 3);

        // Assert - ToolResult would have IsError=true with this content
        formatted.Should().NotBeNullOrEmpty();
        formatted.Should().Contain("Validation failed for tool 'read_file'");
        formatted.Should().Contain("attempt 1/3");
        formatted.Should().Contain("[VAL-001]");
        formatted.Should().Contain("/path");
    }

    [Fact]
    public void Should_Track_Retry_Across_Turns()
    {
        // Arrange
        var toolCallId = "tool-call-001";
        var errors = new List<ValidationError>
        {
            new()
            {
                ErrorCode = ErrorCode.TypeMismatch,
                FieldPath = "/timeout",
                Message = "Expected integer, got string",
                Severity = ErrorSeverity.Error,
            },
        };

        // Act - First turn
        var attempt1 = this.tracker.IncrementAttempt(toolCallId);
        var formatted1 = this.formatter.FormatErrors("run_command", errors, attempt1, this.config.MaxAttempts);
        this.tracker.RecordError(toolCallId, formatted1);

        // Act - Second turn
        var attempt2 = this.tracker.IncrementAttempt(toolCallId);
        var formatted2 = this.formatter.FormatErrors("run_command", errors, attempt2, this.config.MaxAttempts);
        this.tracker.RecordError(toolCallId, formatted2);

        // Assert
        attempt1.Should().Be(1);
        attempt2.Should().Be(2);
        formatted1.Should().Contain("attempt 1/3");
        formatted2.Should().Contain("attempt 2/3");

        var history = this.tracker.GetHistory(toolCallId);
        history.Should().HaveCount(2);
        history[0].Should().Contain("attempt 1/3");
        history[1].Should().Contain("attempt 2/3");
    }

    [Fact]
    public void Should_Escalate_After_Max_Retries()
    {
        // Arrange
        var toolCallId = "tool-call-002";
        var errors = new List<ValidationError>
        {
            new()
            {
                ErrorCode = ErrorCode.PatternMismatch,
                FieldPath = "/email",
                Message = "Email format invalid",
                Severity = ErrorSeverity.Error,
            },
        };

        // Simulate 3 attempts (max)
        for (int i = 0; i < this.config.MaxAttempts; i++)
        {
            var attempt = this.tracker.IncrementAttempt(toolCallId);
            var formatted = this.formatter.FormatErrors("send_email", errors, attempt, this.config.MaxAttempts);
            this.tracker.RecordError(toolCallId, formatted);
        }

        // Act - Check if exceeded and escalate
        var exceeded = this.tracker.HasExceededMaxRetries(toolCallId);
        var history = this.tracker.GetHistory(toolCallId);
        var escalation = this.escalationFormatter.FormatEscalation(
            "send_email",
            toolCallId,
            history,
            this.config.MaxAttempts);

        // Assert
        exceeded.Should().BeTrue();
        escalation.Should().Contain("ESCALATION REQUIRED");
        escalation.Should().Contain("'send_email'");
        escalation.Should().Contain("3 attempts");
        escalation.Should().Contain("Attempt 1:");
        escalation.Should().Contain("Attempt 2:");
        escalation.Should().Contain("Attempt 3:");
        escalation.Should().Contain("Human intervention required");
    }

    [Fact]
    public async Task Should_Handle_Concurrent_Tool_Calls()
    {
        // Arrange
        const int concurrentCalls = 100;
        var tracker = new RetryTracker(maxAttempts: 10);
        var tasks = new List<Task<int>>();

        // Act - 100 concurrent tool calls, each incrementing once
        for (int i = 0; i < concurrentCalls; i++)
        {
            var toolCallId = $"concurrent-call-{i}";
            tasks.Add(Task.Run(() => tracker.IncrementAttempt(toolCallId)));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - Each call should have attempt 1
        results.Should().HaveCount(concurrentCalls);
        results.Should().AllSatisfy(r => r.Should().Be(1));

        // Each tool call tracked independently
        for (int i = 0; i < concurrentCalls; i++)
        {
            var toolCallId = $"concurrent-call-{i}";
            tracker.GetCurrentAttempt(toolCallId).Should().Be(1);
        }
    }

    [Fact]
    public void Should_Apply_Sanitization_In_Full_Flow()
    {
        // Arrange - Error with sensitive data
        var errors = new List<ValidationError>
        {
            new()
            {
                ErrorCode = ErrorCode.TypeMismatch,
                FieldPath = "/api_key",
                Message = "Invalid API key format",
                Severity = ErrorSeverity.Error,
                ActualValue = "sk-abcdefghijklmnopqrstuvwxyz123456789",
            },
            new()
            {
                ErrorCode = ErrorCode.TypeMismatch,
                FieldPath = "/password",
                Message = "Password too short",
                Severity = ErrorSeverity.Error,
                ActualValue = "mysecretpassword123",
            },
        };

        // Act
        var formatted = this.formatter.FormatErrors("configure_api", errors, 1, 3);

        // Assert - Sensitive values should be redacted
        formatted.Should().NotContain("sk-abcdefghijklmnopqrstuvwxyz123456789");
        formatted.Should().NotContain("mysecretpassword123");
        formatted.Should().Contain("[REDACTED:");
    }

    [Fact]
    public void Should_Clear_Tracking_On_Success()
    {
        // Arrange
        var toolCallId = "success-after-retry";

        // Simulate 2 failed attempts
        this.tracker.IncrementAttempt(toolCallId);
        this.tracker.RecordError(toolCallId, "Error 1");
        this.tracker.IncrementAttempt(toolCallId);
        this.tracker.RecordError(toolCallId, "Error 2");

        // Act - Success on third attempt, clear tracking
        this.tracker.Clear(toolCallId);

        // Assert
        this.tracker.GetCurrentAttempt(toolCallId).Should().Be(0);
        this.tracker.GetHistory(toolCallId).Should().BeEmpty();
        this.tracker.HasExceededMaxRetries(toolCallId).Should().BeFalse();
    }

    [Fact]
    public void Should_Integrate_All_Components_In_Full_Flow()
    {
        // Arrange
        var toolCallId = "full-flow-test";
        var toolName = "write_file";

        var errors = new List<ValidationError>
        {
            new()
            {
                ErrorCode = ErrorCode.RequiredFieldMissing,
                FieldPath = "/content",
                Message = "Field 'content' is required",
                Severity = ErrorSeverity.Error,
            },
            new()
            {
                ErrorCode = ErrorCode.StringLengthViolation,
                FieldPath = "/path",
                Message = "Path exceeds maximum length",
                Severity = ErrorSeverity.Warning,
                ActualValue = new string('x', 300),
                ExpectedValue = "max 255 characters",
            },
        };

        // Act - Full retry loop
        string? lastFormatted = null;
        while (!this.tracker.HasExceededMaxRetries(toolCallId))
        {
            var attempt = this.tracker.IncrementAttempt(toolCallId);
            lastFormatted = this.formatter.FormatErrors(toolName, errors, attempt, this.config.MaxAttempts);
            this.tracker.RecordError(toolCallId, lastFormatted);

            if (this.tracker.HasExceededMaxRetries(toolCallId))
            {
                break;
            }
        }

        var history = this.tracker.GetHistory(toolCallId);
        var escalation = this.escalationFormatter.FormatEscalation(toolName, toolCallId, history, this.config.MaxAttempts);

        // Assert
        this.tracker.GetCurrentAttempt(toolCallId).Should().Be(this.config.MaxAttempts);
        history.Should().HaveCount(this.config.MaxAttempts);
        escalation.Should().Contain("ESCALATION REQUIRED");
        escalation.Should().Contain($"'{toolName}'");
        escalation.Should().Contain("Validation History");
        escalation.Should().Contain("Recommended Actions");

        // Verify last formatted message
        lastFormatted.Should().NotBeNull();
        lastFormatted.Should().Contain($"attempt {this.config.MaxAttempts}/{this.config.MaxAttempts}");
        lastFormatted.Should().Contain("[VAL-001]"); // Error first
        lastFormatted.Should().Contain("[VAL-009]"); // Warning second
    }
}
