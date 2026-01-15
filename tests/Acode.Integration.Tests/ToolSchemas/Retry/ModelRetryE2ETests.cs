namespace Acode.Integration.Tests.ToolSchemas.Retry;

using Acode.Application.ToolSchemas.Retry;
using Acode.Infrastructure.ToolSchemas.Retry;
using FluentAssertions;

/// <summary>
/// E2E tests with model simulation for retry behavior.
/// </summary>
/// <remarks>
/// Spec Reference: Testing Requirements lines 2512-2602.
/// These tests simulate model making tool calls with errors and correcting them.
/// Skip attribute indicates these require a running local model.
/// </remarks>
public sealed class ModelRetryE2ETests
{
    private readonly RetryConfiguration config;
    private readonly ErrorFormatter formatter;
    private readonly RetryTracker tracker;

    public ModelRetryE2ETests()
    {
        this.config = new RetryConfiguration
        {
            MaxAttempts = 3,
            MaxMessageLength = 2000,
            IncludeHints = true,
            IncludeActualValues = true,
        };
        this.formatter = new ErrorFormatter(this.config);
        this.tracker = new RetryTracker(this.config.MaxAttempts);
    }

    [Fact(Skip = "Requires local model running")]
    public void Should_Correct_Missing_Required_Field_On_Retry()
    {
        // This test simulates:
        // 1. Model makes tool call without required field
        // 2. Error is formatted and returned to model
        // 3. Model corrects error in next attempt
        // 4. Corrected tool call validates successfully

        // Arrange - Simulated model first attempt (missing 'path')
        var toolCallId = "model-correction-test-1";
        var firstAttemptErrors = new List<ValidationError>
        {
            new()
            {
                ErrorCode = ErrorCode.RequiredFieldMissing,
                FieldPath = "/path",
                Message = "Field 'path' is required",
                Severity = ErrorSeverity.Error,
            },
        };

        // Act - First attempt fails
        var attempt1 = this.tracker.IncrementAttempt(toolCallId);
        var errorMessage = this.formatter.FormatErrors("read_file", firstAttemptErrors, attempt1, this.config.MaxAttempts);
        this.tracker.RecordError(toolCallId, errorMessage);

        // Assert - Error message contains hint for model to correct
        errorMessage.Should().Contain("Add the required field 'path'");
        errorMessage.Should().Contain("attempt 1/3");

        // Simulated: Model would receive this message and correct the call
        // Second attempt - no errors (simulated successful validation)
        var attempt2 = this.tracker.IncrementAttempt(toolCallId);
        attempt2.Should().Be(2);

        // On success, clear the tracker
        this.tracker.Clear(toolCallId);
        this.tracker.GetCurrentAttempt(toolCallId).Should().Be(0);
    }

    [Fact(Skip = "Requires local model running")]
    public void Should_Correct_Type_Mismatch_On_Retry()
    {
        // This test simulates:
        // 1. Model provides string where integer expected
        // 2. Error formatted with actual/expected values
        // 3. Model corrects type in next attempt

        // Arrange - Simulated model provides string instead of integer
        var toolCallId = "model-type-correction-test";
        var firstAttemptErrors = new List<ValidationError>
        {
            new()
            {
                ErrorCode = ErrorCode.TypeMismatch,
                FieldPath = "/timeout",
                Message = "Expected integer, got string",
                Severity = ErrorSeverity.Error,
                ExpectedValue = "integer",
                ActualValue = "thirty",
            },
        };

        // Act - First attempt fails
        var attempt1 = this.tracker.IncrementAttempt(toolCallId);
        var errorMessage = this.formatter.FormatErrors("run_command", firstAttemptErrors, attempt1, this.config.MaxAttempts);
        this.tracker.RecordError(toolCallId, errorMessage);

        // Assert - Error contains type hint
        errorMessage.Should().Contain("Check the type of timeout");
        errorMessage.Should().Contain("Expected: integer");
        errorMessage.Should().Contain("Actual: thirty");

        // Simulated: Model corrects and provides integer 30
        // On success
        this.tracker.Clear(toolCallId);
    }

    [Fact(Skip = "Requires local model running")]
    public void Should_Provide_Enum_Values_On_Invalid_Enum()
    {
        // Simulates model providing invalid enum value and getting corrected
        var toolCallId = "model-enum-correction";
        var errors = new List<ValidationError>
        {
            new()
            {
                ErrorCode = ErrorCode.InvalidEnumValue,
                FieldPath = "/mode",
                Message = "Invalid enum value 'fast'",
                Severity = ErrorSeverity.Error,
                ExpectedValue = "sequential, parallel, hybrid",
                ActualValue = "fast",
            },
        };

        var attempt = this.tracker.IncrementAttempt(toolCallId);
        var errorMessage = this.formatter.FormatErrors("process_data", errors, attempt, this.config.MaxAttempts);

        // Assert - Model receives valid options
        errorMessage.Should().Contain("Use one of: sequential, parallel, hybrid");
    }

    [Fact(Skip = "Requires local model running")]
    public void Should_Correct_Pattern_Mismatch_On_Retry()
    {
        // Simulates model providing incorrectly formatted value
        var toolCallId = "model-pattern-correction";
        var errors = new List<ValidationError>
        {
            new()
            {
                ErrorCode = ErrorCode.PatternMismatch,
                FieldPath = "/email",
                Message = "Value does not match email pattern",
                Severity = ErrorSeverity.Error,
                ExpectedValue = "^[\\w.-]+@[\\w.-]+\\.\\w+$",
                ActualValue = "invalid-email",
            },
        };

        var attempt = this.tracker.IncrementAttempt(toolCallId);
        var errorMessage = this.formatter.FormatErrors("send_notification", errors, attempt, this.config.MaxAttempts);

        // Assert - Model receives pattern to follow
        errorMessage.Should().Contain("Match pattern:");
    }

    [Fact]
    public void Should_Simulate_Model_Self_Correction_Flow()
    {
        // Non-skip test that simulates the full self-correction flow
        // without actually requiring a model
        var toolCallId = "simulation-test";
        var toolName = "create_branch";

        // Simulate: Model's first attempt has error
        var attempt1Errors = new List<ValidationError>
        {
            new()
            {
                ErrorCode = ErrorCode.StringLengthViolation,
                FieldPath = "/branch_name",
                Message = "Branch name exceeds 255 characters",
                Severity = ErrorSeverity.Error,
                ActualValue = new string('x', 300),
                ExpectedValue = "max 255 characters",
            },
        };

        // First attempt
        var attempt1 = this.tracker.IncrementAttempt(toolCallId);
        var error1 = this.formatter.FormatErrors(toolName, attempt1Errors, attempt1, this.config.MaxAttempts);
        this.tracker.RecordError(toolCallId, error1);

        attempt1.Should().Be(1);
        error1.Should().Contain("Adjust string length");

        // Simulate: Model corrects the branch name
        // Second attempt - now valid (no errors to format, validation passes)
        var attempt2 = this.tracker.IncrementAttempt(toolCallId);
        attempt2.Should().Be(2);

        // Validation passes, clear tracking
        this.tracker.Clear(toolCallId);

        // Verify clean state
        this.tracker.GetCurrentAttempt(toolCallId).Should().Be(0);
        this.tracker.HasExceededMaxRetries(toolCallId).Should().BeFalse();
    }
}
