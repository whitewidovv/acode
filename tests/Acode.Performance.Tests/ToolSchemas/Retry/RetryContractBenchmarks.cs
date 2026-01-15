using Acode.Application.ToolSchemas.Retry;
using Acode.Infrastructure.ToolSchemas.Retry;
using BenchmarkDotNet.Attributes;

namespace Acode.Performance.Tests.ToolSchemas.Retry;

/// <summary>
/// Performance benchmarks for retry contract components.
/// Validates performance targets from Task 007b spec.
/// </summary>
/// <remarks>
/// Spec Reference: Testing Requirements lines 2604-2745.
/// NFR-001: Error formatting must complete in less than 1ms average.
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class RetryContractBenchmarks
{
    private ErrorFormatter formatter = null!;
    private RetryTracker tracker = null!;
    private EscalationFormatter escalationFormatter = null!;
    private List<ValidationError> singleError = null!;
    private List<ValidationError> multipleErrors = null!;
    private List<string> validationHistory = null!;

    [GlobalSetup]
    public void Setup()
    {
        var config = new RetryConfiguration
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

        this.formatter = new ErrorFormatter(config);
        this.tracker = new RetryTracker(config.MaxAttempts);
        this.escalationFormatter = new EscalationFormatter();

        // Setup test data
        this.singleError = new List<ValidationError>
        {
            new()
            {
                ErrorCode = ErrorCode.RequiredFieldMissing,
                FieldPath = "/path",
                Message = "Field 'path' is required",
                Severity = ErrorSeverity.Error,
            },
        };

        this.multipleErrors = new List<ValidationError>();
        for (int i = 0; i < 10; i++)
        {
            this.multipleErrors.Add(new ValidationError
            {
                ErrorCode = ErrorCode.TypeMismatch,
                FieldPath = $"/field{i}",
                Message = $"Error message for field {i}",
                Severity = (ErrorSeverity)(i % 3),
                ActualValue = $"actual_value_{i}",
                ExpectedValue = "expected_type",
            });
        }

        this.validationHistory = new List<string>
        {
            "First error message from attempt 1",
            "Second error message from attempt 2",
            "Third error message from attempt 3",
        };
    }

    /// <summary>
    /// Benchmark: Format single error.
    /// Target: less than 1ms.
    /// </summary>
    [Benchmark]
    public string FormatSingleError()
    {
        return this.formatter.FormatErrors("read_file", this.singleError, 1, 3);
    }

    /// <summary>
    /// Benchmark: Format multiple errors (10 errors).
    /// Target: less than 1ms.
    /// </summary>
    [Benchmark]
    public string FormatMultipleErrors()
    {
        return this.formatter.FormatErrors("tool", this.multipleErrors, 1, 3);
    }

    /// <summary>
    /// Benchmark: Increment attempt counter.
    /// Target: O(1) - constant time regardless of tracked tool calls.
    /// </summary>
    [Benchmark]
    public int IncrementAttempt()
    {
        return this.tracker.IncrementAttempt("benchmark-call");
    }

    /// <summary>
    /// Benchmark: Get current attempt (lookup).
    /// Target: O(1) - constant time lookup.
    /// </summary>
    [Benchmark]
    public int GetCurrentAttempt()
    {
        return this.tracker.GetCurrentAttempt("benchmark-call");
    }

    /// <summary>
    /// Benchmark: Record error to history.
    /// Target: less than 0.1ms.
    /// </summary>
    [Benchmark]
    public void RecordError()
    {
        this.tracker.RecordError("record-bench", "Error message to record");
    }

    /// <summary>
    /// Benchmark: Retrieve history.
    /// Target: O(1) lookup + copy time.
    /// </summary>
    [Benchmark]
    public IReadOnlyList<string> GetHistory()
    {
        return this.tracker.GetHistory("benchmark-call");
    }

    /// <summary>
    /// Benchmark: Format escalation message.
    /// Target: less than 1ms.
    /// </summary>
    [Benchmark]
    public string FormatEscalation()
    {
        return this.escalationFormatter.FormatEscalation("tool", "call-123", this.validationHistory, 3);
    }

    /// <summary>
    /// Benchmark: 1000 consecutive format operations.
    /// Target: total less than 1000ms (average less than 1ms each).
    /// </summary>
    [Benchmark]
    public void ThousandFormatOperations()
    {
        for (int i = 0; i < 1000; i++)
        {
            _ = this.formatter.FormatErrors("tool", this.multipleErrors, (i % 3) + 1, 3);
        }
    }

    /// <summary>
    /// Benchmark: 10000 tracker lookups.
    /// Target: total less than 100ms (confirms O(1) lookup).
    /// </summary>
    [Benchmark]
    public void TenThousandLookups()
    {
        for (int i = 0; i < 10000; i++)
        {
            _ = this.tracker.GetCurrentAttempt($"lookup-{i % 100}");
        }
    }
}
