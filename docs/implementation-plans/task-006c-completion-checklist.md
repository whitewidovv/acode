# Task 006c - 100% Completion Checklist
## Load/Health-Check Endpoints + Error Handling

## INSTRUCTIONS FOR FRESH CONTEXT AGENT

**Your Mission**: Complete task-006c (Load/Health-Check Endpoints + Error Handling) to 100% specification compliance - all 80 acceptance criteria must be semantically complete with tests.

**Current Status**:
- **Completion**: ~19% by AC count, ~15% by file count
- **Major Issue**: Health model incomplete (bool vs 4-state enum), entire subsystems missing (Metrics, Error parsing)
- **Gap Analysis**: See docs/implementation-plans/task-006c-gap-analysis.md for detailed findings

**How to Use This File**:
1. Read ENTIRE file first (understand full scope ~1600+ lines)
2. Read the task spec: docs/tasks/refined-tasks/Epic 01/task-006c-loadhealth-check-endpoints-error-handling.md (806 lines)
3. Work through Phases 0-6 sequentially
4. For each gap item:
   - Mark as [🔄] when starting work
   - Follow TDD strictly: RED → GREEN → REFACTOR
   - Run tests after each change
   - Mark as [✅] when complete with evidence
5. Update this file after EACH completed item (not batched)
6. Commit after each logical unit of work
7. When context low (<10k tokens): commit, update progress, stop

**Status Legend**:
- `[ ]` = TODO (not started)
- `[🔄]` = IN PROGRESS (actively working on this)
- `[✅]` = COMPLETE (implemented + tested + verified)

**Critical Rules** (CLAUDE.md Section 3):
- NO deferrals - implement EVERYTHING in this task
- NO placeholders - full implementations only
- NO "TODO" comments in production code
- TESTS FIRST - always RED before GREEN
- VERIFY SEMANTICALLY - presence ≠ completeness
- COMMIT FREQUENTLY - after each logical unit

**Context Management**:
- If context runs low, commit and update this file with [🔄] status
- Mark exactly what's partially done and where to resume
- Next session picks up from this file

**Key Spec Sections** (must read before coding):
- Implementation Prompt: lines 668-806 (file structure + code examples)
- Testing Requirements: lines 549-604 (all test files/methods)
- Acceptance Criteria: lines 429-546 (80 ACs to verify)
- Functional Requirements: lines 82-203 (83 FRs to implement)
- User Manual: lines 247-427 (expected behavior + config)

---

## PHASE 0: VERIFY EXISTING IMPLEMENTATION (DO FIRST)

**Goal**: Read all existing files and document semantic completeness before adding new code.

### Gap 0.1: Read and Verify VllmHealthChecker.cs

**Status**: [ ]

**File**: src/Acode.Infrastructure/Vllm/Health/VllmHealthChecker.cs (78 lines)

**What to Check**:
1. What dependencies exist? (VllmClientConfiguration, HttpClient)
2. What methods exist? (IsHealthyAsync, GetHealthStatusAsync)
3. Does it return four-state HealthStatus enum or just boolean?
4. Does it query /v1/models fallback endpoint?
5. Does it have ILogger dependency?
6. Does it integrate with VllmMetricsClient?
7. Does it check response time thresholds?

**Document Findings**:
- [ ] Dependencies: (list what's injected)
- [ ] Methods: (list public methods)
- [ ] Health states: bool IsHealthy vs HealthStatus enum
- [ ] Fallback endpoint: Yes / No
- [ ] Logging: Yes / No
- [ ] Metrics: Yes / No
- [ ] Thresholds: Yes / No

**Success Criteria**:
- [ ] File read completely
- [ ] Gaps documented

---

### Gap 0.2: Read and Verify VllmHealthStatus.cs

**Status**: [ ]

**File**: src/Acode.Infrastructure/Vllm/Health/VllmHealthStatus.cs (49 lines)

**What to Check**:
1. What properties exist?
2. Is it a HealthStatus enum or a data class?
3. Does it have Status property with enum Healthy/Degraded/Unhealthy/Unknown?
4. Does it have Models list?
5. Does it have Load property (VllmLoadStatus)?

**Document Findings**:
- [ ] Properties: (list all properties)
- [ ] Status type: bool vs enum
- [ ] Models included: Yes / No
- [ ] Load included: Yes / No

**Success Criteria**:
- [ ] File read completely
- [ ] Decision made: Rename to VllmHealthResult? Or add HealthStatus enum separately?

---

### Gap 0.3: Verify All Exception Files

**Status**: [ ]

**Files**: src/Acode.Infrastructure/Vllm/Exceptions/*.cs (9 files)

**What to Check**:
- [ ] VllmException.cs - Has ErrorCode, RequestId, Timestamp, IsTransient?
- [ ] VllmConnectionException.cs - ErrorCode ACODE-VLM-001, IsTransient=true?
- [ ] VllmTimeoutException.cs - ErrorCode ACODE-VLM-002, IsTransient=true?
- [ ] VllmAuthException.cs - ErrorCode ACODE-VLM-011, IsTransient=false?
- [ ] VllmModelNotFoundException.cs - ErrorCode ACODE-VLM-003, IsTransient=false?
- [ ] VllmRateLimitException.cs - ErrorCode ACODE-VLM-012, IsTransient=true?
- [ ] VllmRequestException.cs - ErrorCode ACODE-VLM-004, IsTransient=false?
- [ ] VllmServerException.cs - ErrorCode ACODE-VLM-005, IsTransient=true?
- [ ] VllmOutOfMemoryException.cs - EXISTS? ErrorCode ACODE-VLM-013?
- [ ] IVllmException.cs - INTERFACE EXISTS?

**Document Findings**:
```
VllmException: [✅ Complete / ⚠️ Incomplete]
VllmConnectionException: [✅ Complete / ⚠️ Incomplete]
...
VllmOutOfMemoryException: [❌ Missing / ✅ Exists]
IVllmException: [❌ Missing / ✅ Exists]
```

**Success Criteria**:
- [ ] All 9 files verified
- [ ] Missing files identified

---

### Gap 0.4: Check VllmClientConfiguration for Health Config

**Status**: [ ]

**File**: src/Acode.Infrastructure/Vllm/Client/VllmClientConfiguration.cs

**What to Check**:
- [ ] HealthCheckTimeoutSeconds property exists?
- [ ] HealthyThresholdMs property exists?
- [ ] DegradedThresholdMs property exists?
- [ ] LoadMonitoring section exists?
- [ ] MetricsEndpoint property exists?

**Document Findings**:
- Only HealthCheckTimeoutSeconds exists (default 5s)
- Need separate VllmHealthConfiguration class

**Success Criteria**:
- [ ] Config gaps documented

---

### Gap 0.5: Check Test Coverage

**Status**: [ ]

**File**: tests/Acode.Infrastructure.Tests/Vllm/Health/VllmHealthCheckerTests.cs (111 lines, 5 tests)

**Count Tests**:
```bash
grep -c "\[Fact\]" VllmHealthCheckerTests.cs
# Expected: 5
```

**Document Test Names**:
- [ ] Test 1: (name)
- [ ] Test 2: (name)
- [ ] Test 3: (name)
- [ ] Test 4: (name)
- [ ] Test 5: (name)

**What's Missing**:
- [ ] Tests for Degraded status
- [ ] Tests for /v1/models fallback
- [ ] Tests for logging
- [ ] Tests for load status
- [ ] All metrics tests (VllmMetricsParserTests.cs)
- [ ] All error tests (VllmErrorParserTests.cs, VllmErrorClassifierTests.cs, VllmExceptionMapperTests.cs)

**Success Criteria**:
- [ ] Current tests documented
- [ ] Missing test files identified

---

## PHASE 1: FIX HEALTH STATUS CORE

**Goal**: Complete health checking with four-state model, thresholds, configuration, logging.
**ACs Covered**: AC-001 through AC-014, AC-070 through AC-075

### Gap 1.1: Create HealthStatus Enum

**Status**: [ ]

**File to Create**: src/Acode.Infrastructure/Vllm/Health/HealthStatus.cs

**Spec Reference**: Lines 270-276, FR-008 through FR-012

**TDD Steps**:

**RED**:
```csharp
// In VllmHealthCheckerTests.cs
[Fact]
public void Should_Return_Healthy_When_Fast_Response()
{
    // This test will fail until HealthStatus enum exists
    var status = HealthStatus.Healthy;  // Doesn't compile yet

    status.Should().Be(HealthStatus.Healthy);
}
```

Run: `dotnet test --filter "Should_Return_Healthy_When_Fast_Response"`
Expected: Compilation error (HealthStatus doesn't exist)

**GREEN**:
```csharp
namespace Acode.Infrastructure.Vllm.Health;

/// <summary>
/// Health status values for vLLM provider.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// Provider is responding normally (response time < 1s).
    /// </summary>
    Healthy,

    /// <summary>
    /// Provider is responding but slow or overloaded (response time > 5s).
    /// </summary>
    Degraded,

    /// <summary>
    /// Provider is not responding or returning errors.
    /// </summary>
    Unhealthy,

    /// <summary>
    /// Cannot determine status (timeout or connection failure).
    /// </summary>
    Unknown
}
```

Run test: Expected GREEN

**Success Criteria**:
- [ ] HealthStatus.cs created
- [ ] All 4 states defined
- [ ] XML documentation complete
- [ ] Test passes

---

### Gap 1.2: Create VllmHealthConfiguration

**Status**: [ ]

**File to Create**: src/Acode.Infrastructure/Vllm/Health/VllmHealthConfiguration.cs

**Spec Reference**: Lines 675, 699-700, User Manual lines 279-300, FR-004, FR-026

**TDD Steps**:

**RED**:
```csharp
[Fact]
public void Should_Have_Default_Values()
{
    var config = new VllmHealthConfiguration();  // Doesn't exist yet

    config.HealthEndpoint.Should().Be("/health");
    config.TimeoutSeconds.Should().Be(10);
    config.HealthyThresholdMs.Should().Be(1000);
    config.DegradedThresholdMs.Should().Be(5000);
}
```

Run test: Expected RED (class doesn't exist)

**GREEN**:
```csharp
namespace Acode.Infrastructure.Vllm.Health;

/// <summary>
/// Configuration for vLLM health checking.
/// </summary>
public sealed class VllmHealthConfiguration
{
    /// <summary>
    /// Gets or sets the health check endpoint.
    /// </summary>
    public string HealthEndpoint { get; set; } = "/health";

    /// <summary>
    /// Gets or sets the health check timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets the response time threshold for healthy status in milliseconds.
    /// </summary>
    public int HealthyThresholdMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the response time threshold for degraded status in milliseconds.
    /// </summary>
    public int DegradedThresholdMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the load monitoring configuration.
    /// </summary>
    public LoadMonitoringConfiguration LoadMonitoring { get; set; } = new();

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    public void Validate()
    {
        if (TimeoutSeconds <= 0)
            throw new ArgumentException("TimeoutSeconds must be greater than 0.", nameof(TimeoutSeconds));

        if (HealthyThresholdMs <= 0)
            throw new ArgumentException("HealthyThresholdMs must be greater than 0.", nameof(HealthyThresholdMs));

        if (DegradedThresholdMs <= HealthyThresholdMs)
            throw new ArgumentException("DegradedThresholdMs must be greater than HealthyThresholdMs.", nameof(DegradedThresholdMs));
    }
}

/// <summary>
/// Configuration for load monitoring.
/// </summary>
public sealed class LoadMonitoringConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether load monitoring is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the metrics endpoint for Prometheus metrics.
    /// </summary>
    public string MetricsEndpoint { get; set; } = "/metrics";

    /// <summary>
    /// Gets or sets the queue depth threshold for overload detection.
    /// </summary>
    public int QueueThreshold { get; set; } = 10;

    /// <summary>
    /// Gets or sets the GPU utilization threshold percentage for overload detection.
    /// </summary>
    public double GpuThresholdPercent { get; set; } = 95.0;
}
```

Run test: Expected GREEN

**More Tests** (~5 tests):
- Should_Validate_Configuration
- Should_Throw_On_Invalid_Timeout
- Should_Throw_On_Invalid_Thresholds
- Should_Allow_Disabling_Load_Monitoring
- etc.

**Success Criteria**:
- [ ] VllmHealthConfiguration.cs created
- [ ] LoadMonitoringConfiguration nested class created
- [ ] Validate() method implemented
- [ ] ~6 tests passing
- [ ] AC-004, AC-026 verified

---

### Gap 1.3: Rename/Restructure VllmHealthStatus → VllmHealthResult

**Status**: [ ]

**Current File**: src/Acode.Infrastructure/Vllm/Health/VllmHealthStatus.cs (49 lines)
**Action**: Rename and add properties

**Spec Reference**: Lines 722-728, AC-007, AC-013, AC-014, AC-027-031, AC-021-026

**TDD Steps**:

**RED**:
```csharp
[Fact]
public void VllmHealthResult_Should_Have_Status_Enum()
{
    var result = new VllmHealthResult
    {
        Status = HealthStatus.Healthy,  // Not HealthStatus enum yet
        Endpoint = "http://localhost:8000",
        ResponseTime = TimeSpan.FromMilliseconds(45),
        Models = new[] { "llama-3.2-8b" },
        Load = null
    };

    result.Status.Should().Be(HealthStatus.Healthy);
    result.Models.Should().HaveCount(1);
}
```

Run test: Expected RED (Status is bool, not enum)

**GREEN**:

Rename file: `VllmHealthStatus.cs` → `VllmHealthResult.cs`

```csharp
namespace Acode.Infrastructure.Vllm.Health;

/// <summary>
/// Represents the health status result of a vLLM server.
/// </summary>
public sealed class VllmHealthResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VllmHealthResult"/> class.
    /// </summary>
    /// <param name="status">The health status.</param>
    /// <param name="endpoint">The server endpoint.</param>
    /// <param name="responseTime">Response time.</param>
    /// <param name="errorMessage">Error message if unhealthy.</param>
    /// <param name="models">Loaded models.</param>
    /// <param name="load">Load status.</param>
    public VllmHealthResult(
        HealthStatus status,
        string endpoint,
        TimeSpan responseTime,
        string? errorMessage = null,
        string[]? models = null,
        VllmLoadStatus? load = null)
    {
        Status = status;
        Endpoint = endpoint;
        ResponseTime = responseTime;
        ErrorMessage = errorMessage;
        Models = models ?? Array.Empty<string>();
        Load = load;
        CheckedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the health status.
    /// </summary>
    public HealthStatus Status { get; }

    /// <summary>
    /// Gets the server endpoint.
    /// </summary>
    public string Endpoint { get; }

    /// <summary>
    /// Gets the response time.
    /// </summary>
    public TimeSpan ResponseTime { get; }

    /// <summary>
    /// Gets the error message if unhealthy.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the loaded models.
    /// </summary>
    public string[] Models { get; }

    /// <summary>
    /// Gets the load status.
    /// </summary>
    public VllmLoadStatus? Load { get; }

    /// <summary>
    /// Gets the timestamp when the check was performed.
    /// </summary>
    public DateTimeOffset CheckedAt { get; }

    /// <summary>
    /// Creates an Unknown health result.
    /// </summary>
    public static VllmHealthResult Unknown(string endpoint, string reason)
    {
        return new VllmHealthResult(
            HealthStatus.Unknown,
            endpoint,
            TimeSpan.Zero,
            errorMessage: reason);
    }

    /// <summary>
    /// Creates an Unhealthy health result.
    /// </summary>
    public static VllmHealthResult Unhealthy(string endpoint, TimeSpan responseTime, string message)
    {
        return new VllmHealthResult(
            HealthStatus.Unhealthy,
            endpoint,
            responseTime,
            errorMessage: message);
    }
}
```

Run test: Expected GREEN

**Update All References**:
- [ ] Update VllmHealthChecker.cs to use VllmHealthResult
- [ ] Update VllmHealthCheckerTests.cs to use VllmHealthResult
- [ ] Fix compilation errors

**Success Criteria**:
- [ ] File renamed
- [ ] Status property changed from bool to HealthStatus enum
- [ ] Models property added
- [ ] Load property added
- [ ] Factory methods added
- [ ] All tests updated and passing
- [ ] AC-007, AC-013, AC-014 verified

---

### Gap 1.4: Update VllmHealthChecker with Threshold Checking

**Status**: [ ]

**File**: src/Acode.Infrastructure/Vllm/Health/VllmHealthChecker.cs

**Changes Needed**:
1. Add VllmHealthConfiguration dependency (replace VllmClientConfiguration for health config)
2. Add ILogger<VllmHealthChecker> dependency
3. Implement DetermineStatus() method with threshold checking
4. Add logging for check start, result, errors, status transitions
5. Query /v1/models fallback endpoint
6. Return VllmHealthResult (not VllmHealthStatus)

**TDD Steps**:

**RED**:
```csharp
[Fact]
public async Task Should_Return_Degraded_When_Slow_Response()
{
    // Mock slow response (>5s)
    var config = new VllmHealthConfiguration
    {
        HealthyThresholdMs = 1000,
        DegradedThresholdMs = 5000
    };
    var checker = new VllmHealthChecker(config, logger: null!);

    // Simulate 6-second response time
    var result = await checker.GetHealthStatusAsync(CancellationToken.None);

    result.Status.Should().Be(HealthStatus.Degraded);
    result.ResponseTime.Should().BeGreaterThan(TimeSpan.FromSeconds(5));
}
```

Run test: Expected RED (DetermineStatus doesn't check thresholds)

**GREEN**:

Update VllmHealthChecker.cs:

```csharp
public sealed class VllmHealthChecker
{
    private readonly VllmHealthConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<VllmHealthChecker> _logger;
    private HealthStatus? _previousStatus;

    public VllmHealthChecker(
        VllmHealthConfiguration config,
        ILogger<VllmHealthChecker> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config.Validate();

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_config.Endpoint ?? "http://localhost:8000"),
            Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
        };
    }

    public async Task<VllmHealthResult> GetHealthStatusAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting health check for vLLM at {Endpoint}", _httpClient.BaseAddress);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await _httpClient.GetAsync(
                _config.HealthEndpoint,
                cancellationToken).ConfigureAwait(false);

            stopwatch.Stop();
            var responseTime = stopwatch.Elapsed;

            var status = DetermineStatus(response.StatusCode, responseTime);

            var models = await GetLoadedModelsAsync(cancellationToken);
            var load = _config.LoadMonitoring.Enabled
                ? await GetLoadStatusAsync(cancellationToken)
                : null;

            LogStatusResult(status, responseTime);
            CheckStatusTransition(status);

            return new VllmHealthResult(
                status,
                _httpClient.BaseAddress!.ToString(),
                responseTime,
                errorMessage: response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}",
                models: models,
                load: load);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TimeoutException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Health check timed out for {Endpoint}", _httpClient.BaseAddress);
            return VllmHealthResult.Unknown(_httpClient.BaseAddress!.ToString(), "Health check timed out");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Health check failed for {Endpoint}: {Error}",
                _httpClient.BaseAddress, ex.Message);
            return VllmHealthResult.Unhealthy(
                _httpClient.BaseAddress!.ToString(),
                stopwatch.Elapsed,
                ex.Message);
        }
    }

    private HealthStatus DetermineStatus(HttpStatusCode code, TimeSpan responseTime)
    {
        if (code != HttpStatusCode.OK)
            return HealthStatus.Unhealthy;

        if (responseTime > TimeSpan.FromMilliseconds(_config.DegradedThresholdMs))
            return HealthStatus.Degraded;

        if (responseTime <= TimeSpan.FromMilliseconds(_config.HealthyThresholdMs))
            return HealthStatus.Healthy;

        // Between healthy and degraded thresholds - consider healthy
        return HealthStatus.Healthy;
    }

    private async Task<string[]> GetLoadedModelsAsync(CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync("/v1/models", ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return Array.Empty<string>();

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            // Parse JSON: { "data": [{ "id": "model-name" }] }
            var doc = JsonDocument.Parse(json);
            var models = doc.RootElement
                .GetProperty("data")
                .EnumerateArray()
                .Select(m => m.GetProperty("id").GetString() ?? string.Empty)
                .Where(id => !string.IsNullOrEmpty(id))
                .ToArray();

            return models;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query /v1/models endpoint");
            return Array.Empty<string>();
        }
    }

    private Task<VllmLoadStatus?> GetLoadStatusAsync(CancellationToken ct)
    {
        // TODO: Implement in Phase 4 (Metrics subsystem)
        return Task.FromResult<VllmLoadStatus?>(null);
    }

    private void LogStatusResult(HealthStatus status, TimeSpan responseTime)
    {
        _logger.LogInformation(
            "Health check complete: Status={Status}, ResponseTime={ResponseTime}ms",
            status,
            responseTime.TotalMilliseconds);
    }

    private void CheckStatusTransition(HealthStatus newStatus)
    {
        if (_previousStatus.HasValue && _previousStatus != newStatus)
        {
            _logger.LogWarning(
                "Health status changed: {PreviousStatus} → {CurrentStatus}",
                _previousStatus.Value,
                newStatus);
        }

        _previousStatus = newStatus;
    }

    public async Task<bool> IsModelLoadedAsync(string modelId, CancellationToken ct)
    {
        var models = await GetLoadedModelsAsync(ct);
        return models.Contains(modelId);
    }
}
```

Run test: Expected GREEN

**More Tests** (~8 tests):
- Should_Return_Healthy_When_Fast (<1s)
- Should_Return_Degraded_When_Slow (>5s)
- Should_Return_Unhealthy_On_Non200
- Should_Return_Unknown_On_Timeout
- Should_Query_Models_Endpoint
- Should_Check_Specific_Model_Loaded
- Should_Log_Health_Checks
- Should_Log_Status_Transitions

**Success Criteria**:
- [ ] VllmHealthConfiguration dependency added
- [ ] ILogger dependency added
- [ ] DetermineStatus() with threshold checking
- [ ] GetLoadedModelsAsync() implemented
- [ ] IsModelLoadedAsync() implemented
- [ ] Logging for all events
- [ ] Status transition tracking
- [ ] ~13 tests passing (5 existing + 8 new)
- [ ] AC-001 through AC-014 verified
- [ ] AC-070 through AC-075 verified

---

## PHASE 2: COMPLETE EXCEPTION HIERARCHY

**Goal**: Add IVllmException interface and VllmOutOfMemoryException.
**ACs Covered**: AC-055, AC-056, AC-062

### Gap 2.1: Create IVllmException Interface

**Status**: [ ]

**File to Create**: src/Acode.Infrastructure/Vllm/Exceptions/IVllmException.cs

**Spec Reference**: AC-062, FR-062

**TDD Steps**:

**RED**:
```csharp
[Fact]
public void All_Exceptions_Should_Implement_IVllmException()
{
    var exception = new VllmConnectionException("Test");

    exception.Should().BeAssignableTo<IVllmException>();
}
```

Run test: Expected RED (interface doesn't exist)

**GREEN**:
```csharp
namespace Acode.Infrastructure.Vllm.Exceptions;

/// <summary>
/// Interface for all vLLM exceptions.
/// </summary>
public interface IVllmException
{
    /// <summary>
    /// Gets the error code for this exception.
    /// </summary>
    string ErrorCode { get; }

    /// <summary>
    /// Gets or sets the request ID associated with this error.
    /// </summary>
    string? RequestId { get; set; }

    /// <summary>
    /// Gets the timestamp when this exception was created.
    /// </summary>
    DateTime Timestamp { get; }

    /// <summary>
    /// Gets a value indicating whether this error is transient and may succeed on retry.
    /// </summary>
    bool IsTransient { get; }
}
```

Update VllmException.cs to implement interface:
```csharp
public class VllmException : Exception, IVllmException
{
    // Existing implementation already has all required properties
}
```

Run test: Expected GREEN

**Success Criteria**:
- [ ] IVllmException.cs created
- [ ] VllmException implements IVllmException
- [ ] All derived exceptions inherit interface
- [ ] Test passes
- [ ] AC-062 verified

---

### Gap 2.2: Create VllmOutOfMemoryException

**Status**: [ ]

**File to Create**: src/Acode.Infrastructure/Vllm/Exceptions/VllmOutOfMemoryException.cs

**Spec Reference**: AC-055, FR-055, lines 784, 768

**TDD Steps**:

**RED**:
```csharp
[Fact]
public void VllmOutOfMemoryException_Should_Have_Correct_ErrorCode()
{
    var exception = new VllmOutOfMemoryException("CUDA out of memory");

    exception.ErrorCode.Should().Be("ACODE-VLM-013");
    exception.IsTransient.Should().BeTrue();
}
```

Run test: Expected RED (class doesn't exist)

**GREEN**:
```csharp
namespace Acode.Infrastructure.Vllm.Exceptions;

/// <summary>
/// Exception thrown when vLLM encounters CUDA out of memory error.
/// </summary>
public sealed class VllmOutOfMemoryException : VllmException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VllmOutOfMemoryException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public VllmOutOfMemoryException(string message)
        : base("ACODE-VLM-013", message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VllmOutOfMemoryException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public VllmOutOfMemoryException(string message, Exception innerException)
        : base("ACODE-VLM-013", message, innerException)
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Transient because may succeed after memory is freed or smaller request is made.
    /// </remarks>
    public override bool IsTransient => true;
}
```

Run test: Expected GREEN

**Create Test File**: tests/Acode.Infrastructure.Tests/Vllm/Exceptions/VllmOutOfMemoryExceptionTests.cs

**More Tests** (~3 tests):
- Should_Have_ErrorCode_ACODE_VLM_013
- Should_Be_Transient
- Should_Accept_InnerException

**Success Criteria**:
- [ ] VllmOutOfMemoryException.cs created
- [ ] Error code ACODE-VLM-013
- [ ] IsTransient = true
- [ ] ~3 tests passing
- [ ] AC-055 verified

---

## PHASE 3: ERROR SUBSYSTEM

**Goal**: Build error parsing, classification, and exception mapping.
**ACs Covered**: AC-032 through AC-047, AC-048 through AC-056

### Gap 3.1: Create VllmErrorParser

**Status**: [ ]

**File to Create**: src/Acode.Infrastructure/Vllm/Health/Errors/VllmErrorParser.cs

**Spec Reference**: FR-032 through FR-038, AC-032 through AC-038

**Required Functionality**:
- Parse vLLM error responses (OpenAI format)
- Extract: error.message, error.type, error.code, error.param
- Handle missing optional fields
- Handle malformed JSON

**vLLM Error Format**:
```json
{
  "error": {
    "message": "Model 'nonexistent' not found",
    "type": "invalid_request_error",
    "code": "model_not_found",
    "param": "model"
  }
}
```

**TDD Steps**:

**RED**:
```csharp
[Fact]
public void Should_Parse_Error_Response()
{
    var json = @"{
        ""error"": {
            ""message"": ""Model not found"",
            ""type"": ""invalid_request_error"",
            ""code"": ""model_not_found"",
            ""param"": ""model""
        }
    }";

    var parser = new VllmErrorParser();  // Doesn't exist yet
    var error = parser.Parse(json);

    error.Message.Should().Be("Model not found");
    error.Type.Should().Be("invalid_request_error");
    error.Code.Should().Be("model_not_found");
    error.Param.Should().Be("model");
}
```

Run test: Expected RED

**GREEN**:

First create data class:
```csharp
/// <summary>
/// Parsed vLLM error information.
/// </summary>
public sealed class VllmErrorInfo
{
    public string Message { get; init; } = string.Empty;
    public string? Type { get; init; }
    public string? Code { get; init; }
    public string? Param { get; init; }
}
```

Then create parser:
```csharp
namespace Acode.Infrastructure.Vllm.Health.Errors;

/// <summary>
/// Parses vLLM error responses (OpenAI format).
/// </summary>
public sealed class VllmErrorParser
{
    /// <summary>
    /// Parses a vLLM error response JSON.
    /// </summary>
    /// <param name="json">The error response JSON.</param>
    /// <returns>Parsed error information.</returns>
    public VllmErrorInfo Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new VllmErrorInfo { Message = "Empty error response" };

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("error", out var errorElement))
                return new VllmErrorInfo { Message = "No error object in response" };

            var message = errorElement.TryGetProperty("message", out var msgProp)
                ? msgProp.GetString() ?? "Unknown error"
                : "Unknown error";

            var type = errorElement.TryGetProperty("type", out var typeProp)
                ? typeProp.GetString()
                : null;

            var code = errorElement.TryGetProperty("code", out var codeProp)
                ? codeProp.GetString()
                : null;

            var param = errorElement.TryGetProperty("param", out var paramProp)
                ? paramProp.GetString()
                : null;

            return new VllmErrorInfo
            {
                Message = message,
                Type = type,
                Code = code,
                Param = param
            };
        }
        catch (JsonException)
        {
            return new VllmErrorInfo { Message = "Malformed error response JSON" };
        }
    }
}
```

Run test: Expected GREEN

**More Tests** (~6 tests):
- Should_Extract_All_Fields
- Should_Handle_Missing_Type
- Should_Handle_Missing_Code
- Should_Handle_Missing_Param
- Should_Handle_Missing_Error_Object
- Should_Handle_Malformed_JSON
- Should_Handle_Empty_String

**Success Criteria**:
- [ ] VllmErrorInfo.cs created (data class)
- [ ] VllmErrorParser.cs created
- [ ] Parse() method implemented
- [ ] ~7 tests passing
- [ ] AC-032 through AC-038 verified

---

### Gap 3.2: Create VllmErrorClassifier

**Status**: [ ]

**File to Create**: src/Acode.Infrastructure/Vllm/Health/Errors/VllmErrorClassifier.cs

**Spec Reference**: FR-039 through FR-047, AC-039 through AC-047

**Required Functionality**:
- Classify HTTP status codes as transient or permanent
- 400, 401, 403, 404 → Permanent
- 429, 500, 502, 503, 504 → Transient
- Connection errors → Transient
- Timeouts → Transient

**TDD Steps**:

**RED**:
```csharp
[Fact]
public void Should_Classify_400_As_Permanent()
{
    var classifier = new VllmErrorClassifier();  // Doesn't exist yet

    var isTransient = classifier.IsTransient(HttpStatusCode.BadRequest);

    isTransient.Should().BeFalse("400 errors are permanent");
}
```

Run test: Expected RED

**GREEN**:
```csharp
namespace Acode.Infrastructure.Vllm.Health.Errors;

/// <summary>
/// Classifies vLLM errors as transient or permanent.
/// </summary>
public sealed class VllmErrorClassifier
{
    /// <summary>
    /// Determines if an HTTP status code represents a transient error.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>True if transient (should retry), false if permanent.</returns>
    public bool IsTransient(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            // Transient - server-side issues
            HttpStatusCode.TooManyRequests => true,  // 429
            HttpStatusCode.InternalServerError => true,  // 500
            HttpStatusCode.BadGateway => true,  // 502
            HttpStatusCode.ServiceUnavailable => true,  // 503
            HttpStatusCode.GatewayTimeout => true,  // 504

            // Permanent - client-side issues
            HttpStatusCode.BadRequest => false,  // 400
            HttpStatusCode.Unauthorized => false,  // 401
            HttpStatusCode.Forbidden => false,  // 403
            HttpStatusCode.NotFound => false,  // 404

            // Other 4xx - permanent
            _ when ((int)statusCode >= 400 && (int)statusCode < 500) => false,

            // Other 5xx - transient
            _ when ((int)statusCode >= 500 && (int)statusCode < 600) => true,

            // Unknown - conservative (permanent)
            _ => false
        };
    }

    /// <summary>
    /// Determines if an exception represents a transient error.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>True if transient (should retry), false if permanent.</returns>
    public bool IsTransient(Exception exception)
    {
        return exception switch
        {
            TimeoutException => true,
            HttpRequestException => true,
            SocketException => true,
            IOException => true,
            _ => false
        };
    }
}
```

Run test: Expected GREEN

**More Tests** (~9 tests):
- Should_Classify_400_As_Permanent
- Should_Classify_401_As_Permanent
- Should_Classify_403_As_Permanent
- Should_Classify_404_As_Permanent
- Should_Classify_429_As_Transient
- Should_Classify_500_As_Transient
- Should_Classify_502_503_504_As_Transient
- Should_Classify_Timeout_As_Transient
- Should_Classify_Connection_Errors_As_Transient

**Success Criteria**:
- [ ] VllmErrorClassifier.cs created
- [ ] IsTransient(HttpStatusCode) method
- [ ] IsTransient(Exception) method
- [ ] ~9 tests passing
- [ ] AC-039 through AC-047 verified

---

### Gap 3.3: Create VllmExceptionMapper

**Status**: [ ]

**File to Create**: src/Acode.Infrastructure/Vllm/Health/Errors/VllmExceptionMapper.cs

**Spec Reference**: FR-048 through FR-056, AC-048 through AC-056

**Required Functionality**:
- Map HTTP status + error type to exception class
- Include original response in exception
- Set RequestId from headers

**TDD Steps**:

**RED**:
```csharp
[Fact]
public void Should_Map_401_To_VllmAuthException()
{
    var mapper = new VllmExceptionMapper();  // Doesn't exist yet
    var errorInfo = new VllmErrorInfo { Message = "Unauthorized" };

    var exception = mapper.MapException(
        HttpStatusCode.Unauthorized,
        errorInfo,
        requestId: "req-123");

    exception.Should().BeOfType<VllmAuthException>();
    exception.ErrorCode.Should().Be("ACODE-VLM-011");
    exception.RequestId.Should().Be("req-123");
}
```

Run test: Expected RED

**GREEN**:
```csharp
namespace Acode.Infrastructure.Vllm.Health.Errors;

/// <summary>
/// Maps vLLM errors to exception types.
/// </summary>
public sealed class VllmExceptionMapper
{
    /// <summary>
    /// Maps an HTTP status code and error info to an appropriate exception.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="errorInfo">Parsed error information.</param>
    /// <param name="requestId">The request ID (optional).</param>
    /// <returns>The appropriate exception.</returns>
    public VllmException MapException(
        HttpStatusCode statusCode,
        VllmErrorInfo errorInfo,
        string? requestId = null)
    {
        VllmException exception = statusCode switch
        {
            HttpStatusCode.Unauthorized => new VllmAuthException(errorInfo.Message),
            HttpStatusCode.TooManyRequests => new VllmRateLimitException(errorInfo.Message),
            HttpStatusCode.BadRequest when errorInfo.Code == "model_not_found" =>
                new VllmModelNotFoundException(errorInfo.Message),
            HttpStatusCode.NotFound =>
                new VllmModelNotFoundException(errorInfo.Message),
            HttpStatusCode.BadRequest =>
                new VllmRequestException(errorInfo.Message),

            _ when ((int)statusCode >= 500 && (int)statusCode < 600) =>
                new VllmServerException(errorInfo.Message),

            _ => new VllmRequestException(errorInfo.Message)
        };

        exception.RequestId = requestId;
        return exception;
    }

    /// <summary>
    /// Maps an exception to a vLLM exception.
    /// </summary>
    /// <param name="exception">The original exception.</param>
    /// <param name="requestId">The request ID (optional).</param>
    /// <returns>The appropriate vLLM exception.</returns>
    public VllmException MapException(Exception exception, string? requestId = null)
    {
        VllmException vllmException = exception switch
        {
            TimeoutException => new VllmTimeoutException(exception.Message, exception),
            HttpRequestException => new VllmConnectionException(exception.Message, exception),
            SocketException => new VllmConnectionException(exception.Message, exception),
            _ when exception.Message.Contains("CUDA out of memory", StringComparison.OrdinalIgnoreCase) =>
                new VllmOutOfMemoryException(exception.Message, exception),
            _ => new VllmServerException(exception.Message, exception)
        };

        vllmException.RequestId = requestId;
        return vllmException;
    }
}
```

Run test: Expected GREEN

**More Tests** (~8 tests):
- Should_Map_401_To_Auth
- Should_Map_404_To_ModelNotFound
- Should_Map_429_To_RateLimit
- Should_Map_400_To_Request
- Should_Map_500_To_Server
- Should_Map_Timeout_To_Timeout
- Should_Map_Connection_To_Connection
- Should_Map_CUDA_OOM_To_OOM
- Should_Set_RequestId

**Success Criteria**:
- [ ] VllmExceptionMapper.cs created
- [ ] MapException(HttpStatusCode, ...) method
- [ ] MapException(Exception, ...) method
- [ ] ~9 tests passing
- [ ] AC-048 through AC-056 verified

---

## PHASE 4: METRICS SUBSYSTEM

**Goal**: Build load monitoring with Prometheus metrics parsing.
**ACs Covered**: AC-015 through AC-026

### Gap 4.1: Create VllmLoadStatus Data Class

**Status**: [ ]

**File to Create**: src/Acode.Infrastructure/Vllm/Health/VllmLoadStatus.cs

**Spec Reference**: FR-021 through FR-026, AC-021 through AC-026

**TDD Steps**:

**RED**:
```csharp
[Fact]
public void VllmLoadStatus_Should_Calculate_Overload()
{
    var loadStatus = new VllmLoadStatus  // Doesn't exist yet
    {
        RunningRequests = 5,
        WaitingRequests = 12,  // > threshold of 10
        GpuUtilizationPercent = 98,  // > 95%
        LoadScore = 85
    };

    loadStatus.IsOverloaded.Should().BeTrue();
    loadStatus.OverloadReason.Should().Contain("queue");
}
```

Run test: Expected RED

**GREEN**:
```csharp
namespace Acode.Infrastructure.Vllm.Health;

/// <summary>
/// Represents the load status of a vLLM server.
/// </summary>
public sealed class VllmLoadStatus
{
    /// <summary>
    /// Gets the number of currently running requests.
    /// </summary>
    public int RunningRequests { get; init; }

    /// <summary>
    /// Gets the number of requests waiting in queue.
    /// </summary>
    public int WaitingRequests { get; init; }

    /// <summary>
    /// Gets the GPU cache utilization percentage.
    /// </summary>
    public double GpuUtilizationPercent { get; init; }

    /// <summary>
    /// Gets the overall load score (0-100).
    /// </summary>
    public int LoadScore { get; init; }

    /// <summary>
    /// Gets a value indicating whether the server is overloaded.
    /// </summary>
    public bool IsOverloaded { get; init; }

    /// <summary>
    /// Gets the reason for overload status.
    /// </summary>
    public string? OverloadReason { get; init; }

    /// <summary>
    /// Creates a load status with overload detection.
    /// </summary>
    public static VllmLoadStatus Create(
        int runningRequests,
        int waitingRequests,
        double gpuUtilizationPercent,
        int queueThreshold,
        double gpuThreshold)
    {
        var loadScore = CalculateLoadScore(runningRequests, waitingRequests, gpuUtilizationPercent);

        var isOverloaded = false;
        string? reason = null;

        if (waitingRequests > queueThreshold)
        {
            isOverloaded = true;
            reason = $"Request queue exceeds threshold ({waitingRequests} > {queueThreshold})";
        }
        else if (gpuUtilizationPercent > gpuThreshold)
        {
            isOverloaded = true;
            reason = $"GPU utilization exceeds threshold ({gpuUtilizationPercent:F1}% > {gpuThreshold}%)";
        }

        return new VllmLoadStatus
        {
            RunningRequests = runningRequests,
            WaitingRequests = waitingRequests,
            GpuUtilizationPercent = gpuUtilizationPercent,
            LoadScore = loadScore,
            IsOverloaded = isOverloaded,
            OverloadReason = reason
        };
    }

    private static int CalculateLoadScore(int running, int waiting, double gpuUtilization)
    {
        // Simple load score: weighted average
        var queueScore = Math.Min((running + waiting) * 10, 100);
        var gpuScore = gpuUtilization;

        return (int)((queueScore * 0.5) + (gpuScore * 0.5));
    }
}
```

Run test: Expected GREEN

**More Tests** (~4 tests):
- Should_Not_Be_Overloaded_When_Normal
- Should_Be_Overloaded_When_Queue_High
- Should_Be_Overloaded_When_GPU_High
- Should_Calculate_Load_Score

**Success Criteria**:
- [ ] VllmLoadStatus.cs created
- [ ] Create() factory method
- [ ] Overload detection logic
- [ ] ~5 tests passing
- [ ] AC-021 through AC-026 verified

---

### Gap 4.2: Create VllmMetricsParser

**Status**: [✅]

**File to Create**: src/Acode.Infrastructure/Vllm/Health/Metrics/VllmMetricsParser.cs

**Spec Reference**: FR-016 through FR-020, AC-016 through AC-020

**Required Functionality**:
- Parse Prometheus text format
- Extract vllm_num_requests_running
- Extract vllm_num_requests_waiting
- Extract vllm_gpu_cache_usage_perc

**Prometheus Format Example**:
```
# HELP vllm_num_requests_running Number of requests currently running
# TYPE vllm_num_requests_running gauge
vllm_num_requests_running 5
# HELP vllm_num_requests_waiting Number of requests waiting
# TYPE vllm_num_requests_waiting gauge
vllm_num_requests_waiting 12
# HELP vllm_gpu_cache_usage_perc GPU cache usage percentage
# TYPE vllm_gpu_cache_usage_perc gauge
vllm_gpu_cache_usage_perc 67.5
```

**TDD Steps**:

**RED**:
```csharp
[Fact]
public void Should_Parse_Prometheus_Format()
{
    var prometheus = @"
vllm_num_requests_running 5
vllm_num_requests_waiting 12
vllm_gpu_cache_usage_perc 67.5
";

    var parser = new VllmMetricsParser();  // Doesn't exist yet
    var metrics = parser.Parse(prometheus);

    metrics.RunningRequests.Should().Be(5);
    metrics.WaitingRequests.Should().Be(12);
    metrics.GpuUtilizationPercent.Should().BeApproximately(67.5, 0.1);
}
```

Run test: Expected RED

**GREEN**:

First create data class:
```csharp
/// <summary>
/// Parsed vLLM metrics.
/// </summary>
public sealed class VllmMetrics
{
    public int RunningRequests { get; init; }
    public int WaitingRequests { get; init; }
    public double GpuUtilizationPercent { get; init; }
}
```

Then create parser:
```csharp
namespace Acode.Infrastructure.Vllm.Health.Metrics;

/// <summary>
/// Parses Prometheus metrics from vLLM.
/// </summary>
public sealed class VllmMetricsParser
{
    /// <summary>
    /// Parses Prometheus text format metrics.
    /// </summary>
    /// <param name="prometheusText">The metrics text.</param>
    /// <returns>Parsed metrics.</returns>
    public VllmMetrics Parse(string prometheusText)
    {
        var runningRequests = 0;
        var waitingRequests = 0;
        var gpuUtilization = 0.0;

        if (string.IsNullOrWhiteSpace(prometheusText))
            return new VllmMetrics();

        var lines = prometheusText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Skip comments
            if (trimmed.StartsWith('#'))
                continue;

            if (trimmed.StartsWith("vllm_num_requests_running"))
            {
                runningRequests = ExtractValue<int>(trimmed);
            }
            else if (trimmed.StartsWith("vllm_num_requests_waiting"))
            {
                waitingRequests = ExtractValue<int>(trimmed);
            }
            else if (trimmed.StartsWith("vllm_gpu_cache_usage_perc"))
            {
                gpuUtilization = ExtractValue<double>(trimmed);
            }
        }

        return new VllmMetrics
        {
            RunningRequests = runningRequests,
            WaitingRequests = waitingRequests,
            GpuUtilizationPercent = gpuUtilization
        };
    }

    private static T ExtractValue<T>(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return default!;

        var valueStr = parts[^1];  // Last part is the value

        if (typeof(T) == typeof(int))
            return (T)(object)int.Parse(valueStr);
        if (typeof(T) == typeof(double))
            return (T)(object)double.Parse(valueStr);

        return default!;
    }
}
```

Run test: Expected GREEN

**More Tests** (~7 tests):
- Should_Parse_Running_Requests
- Should_Parse_Waiting_Requests
- Should_Parse_GPU_Usage
- Should_Handle_Missing_Metrics
- Should_Handle_Malformed_Prometheus
- Should_Handle_Empty_String
- Should_Skip_Comments

**Success Criteria**:
- [✅] VllmMetrics.cs created (data class)
- [✅] VllmMetricsParser.cs created
- [✅] Parse() method implemented
- [✅] 10 tests passing (exceeds 8 minimum):
  - Should_Parse_Prometheus_Format
  - Should_Parse_Running_Requests
  - Should_Parse_Waiting_Requests
  - Should_Parse_GPU_Usage
  - Should_Handle_Missing_Metrics
  - Should_Handle_Malformed_Prometheus
  - Should_Handle_Empty_String
  - Should_Skip_Comments
  - Should_Handle_Null_String
  - Should_Handle_Whitespace_Only
- [✅] AC-016 through AC-020 verified

---

### Gap 4.3: Create VllmMetricsClient

**Status**: [ ]

**File to Create**: src/Acode.Infrastructure/Vllm/Health/Metrics/VllmMetricsClient.cs

**Spec Reference**: FR-015, AC-015

**Required Functionality**:
- Query /metrics endpoint
- Return raw Prometheus text
- Handle connection failures gracefully

**TDD Steps**:

**RED**:
```csharp
[Fact]
public async Task Should_Query_Metrics_Endpoint()
{
    var client = new VllmMetricsClient("http://localhost:8000");  // Doesn't exist yet

    var metrics = await client.GetMetricsAsync(CancellationToken.None);

    // Will fail in test environment, but verifies contract
    metrics.Should().NotBeNull();
}
```

Run test: Expected RED

**GREEN**:
```csharp
namespace Acode.Infrastructure.Vllm.Health.Metrics;

/// <summary>
/// Client for querying vLLM Prometheus metrics.
/// </summary>
public sealed class VllmMetricsClient
{
    private readonly HttpClient _httpClient;
    private readonly string _metricsEndpoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="VllmMetricsClient"/> class.
    /// </summary>
    /// <param name="baseUrl">The vLLM base URL.</param>
    /// <param name="metricsEndpoint">The metrics endpoint path.</param>
    public VllmMetricsClient(string baseUrl, string metricsEndpoint = "/metrics")
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _metricsEndpoint = metricsEndpoint;
    }

    /// <summary>
    /// Gets Prometheus metrics from vLLM.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Raw Prometheus text, or empty string on failure.</returns>
    public async Task<string> GetMetricsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(_metricsEndpoint, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return string.Empty;

            var text = await response.Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            return text;
        }
        catch
        {
            return string.Empty;
        }
    }
}
```

Run test: Expected GREEN (will return empty string if no server)

**More Tests** (~3 tests):
- Should_Return_Empty_On_Connection_Failure
- Should_Return_Empty_On_Non_200
- Should_Not_Throw_Exceptions

**Success Criteria**:
- [ ] VllmMetricsClient.cs created
- [ ] GetMetricsAsync() method
- [ ] ~4 tests passing
- [ ] AC-015 verified

---

### Gap 4.4: Integrate Metrics into VllmHealthChecker

**Status**: [ ]

**File**: src/Acode.Infrastructure/Vllm/Health/VllmHealthChecker.cs

**Changes**:
1. Add VllmMetricsClient and VllmMetricsParser dependencies
2. Implement GetLoadStatusAsync() method (currently stubbed)
3. Call metrics client when LoadMonitoring.Enabled

**TDD Steps**:

Update constructor to inject metrics dependencies:
```csharp
public VllmHealthChecker(
    VllmHealthConfiguration config,
    ILogger<VllmHealthChecker> logger,
    VllmMetricsClient? metricsClient = null,
    VllmMetricsParser? metricsParser = null)
{
    _config = config;
    _logger = logger;
    _metricsClient = metricsClient;
    _metricsParser = metricsParser ?? new VllmMetricsParser();
    // ...
}
```

Implement GetLoadStatusAsync():
```csharp
private async Task<VllmLoadStatus?> GetLoadStatusAsync(CancellationToken ct)
{
    if (_metricsClient == null)
        return null;

    try
    {
        var prometheusText = await _metricsClient.GetMetricsAsync(ct);
        if (string.IsNullOrEmpty(prometheusText))
            return null;

        var metrics = _metricsParser.Parse(prometheusText);

        var loadStatus = VllmLoadStatus.Create(
            metrics.RunningRequests,
            metrics.WaitingRequests,
            metrics.GpuUtilizationPercent,
            _config.LoadMonitoring.QueueThreshold,
            _config.LoadMonitoring.GpuThresholdPercent);

        return loadStatus;
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to get load status");
        return null;
    }
}
```

**Test**:
```csharp
[Fact]
public async Task Should_Include_Load_Status_When_Metrics_Available()
{
    var config = new VllmHealthConfiguration
    {
        LoadMonitoring = { Enabled = true }
    };
    var metricsClient = Substitute.For<VllmMetricsClient>("http://localhost:8000");
    var metricsParser = new VllmMetricsParser();

    metricsClient.GetMetricsAsync(Arg.Any<CancellationToken>())
        .Returns(Task.FromResult("vllm_num_requests_running 5\nvllm_num_requests_waiting 2\nvllm_gpu_cache_usage_perc 45.0"));

    var checker = new VllmHealthChecker(config, logger, metricsClient, metricsParser);
    var result = await checker.GetHealthStatusAsync(CancellationToken.None);

    result.Load.Should().NotBeNull();
    result.Load!.RunningRequests.Should().Be(5);
}
```

**Success Criteria**:
- [ ] Metrics dependencies added to VllmHealthChecker
- [ ] GetLoadStatusAsync() implemented
- [ ] ~2 tests for metrics integration
- [ ] AC-015 through AC-026 all verified

---

## PHASE 5: FINAL INTEGRATION

**Goal**: Verify all 80 ACs, complete remaining items, prepare for audit.

### Gap 5.1: Verify All AC Requirements

**Status**: [ ]

**Process**:
1. Go through spec lines 429-546 (all 80 ACs)
2. For each AC, verify implementation exists and test passes
3. Document any remaining gaps

**ACs to Check**:
- [ ] AC-001 through AC-007: Health check queries
- [ ] AC-008 through AC-014: Health status
- [ ] AC-015 through AC-020: Metrics integration
- [ ] AC-021 through AC-026: Load status
- [ ] AC-027 through AC-031: Model availability
- [ ] AC-032 through AC-038: Error parsing
- [ ] AC-039 through AC-047: Error classification
- [ ] AC-048 through AC-056: Exception mapping
- [ ] AC-057 through AC-062: Exception content
- [ ] AC-063 through AC-069: CLI integration (belongs to Task 004.c)
- [ ] AC-070 through AC-075: Logging
- [ ] AC-076 through AC-080: Performance

**Success Criteria**:
- [ ] All 80 ACs verified
- [ ] Any gaps documented and fixed

---

### Gap 5.2: CLI Integration Note

**Status**: [ ]

**Important**: CLI commands (AC-063 through AC-069) belong to Task 004.c (Provider Registry), but they MUST use VllmHealthChecker.

**What This Task Provides**:
- [x] VllmHealthChecker.GetHealthStatusAsync() method
- [x] VllmHealthResult with all required data
- [ ] Provider Registry will format output

**Verification**:
- [ ] VllmHealthChecker.GetHealthStatusAsync() is public
- [ ] VllmHealthResult has all properties for CLI display
- [ ] Method signature matches expected usage

**Success Criteria**:
- [ ] Health checker is usable by Provider Registry
- [ ] AC-063 through AC-069 marked as "Provided for Task 004.c"

---

### Gap 5.3: DI Registration

**Status**: [ ]

**File**: src/Acode.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs (or equivalent)

**Add Registrations**:
```csharp
// Health checking
services.AddSingleton<VllmHealthConfiguration>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    // Load from config.yml
    return config.GetSection("Model:Providers:Vllm:Health").Get<VllmHealthConfiguration>()
        ?? new VllmHealthConfiguration();
});
services.AddSingleton<VllmHealthChecker>();
services.AddSingleton<VllmMetricsClient>();
services.AddSingleton<VllmMetricsParser>();

// Error handling
services.AddSingleton<VllmErrorParser>();
services.AddSingleton<VllmErrorClassifier>();
services.AddSingleton<VllmExceptionMapper>();
```

**Success Criteria**:
- [ ] All components registered in DI
- [ ] Configuration loaded from .agent/config.yml
- [ ] DI resolves VllmHealthChecker successfully

---

## PHASE 6: FINAL VERIFICATION AND AUDIT

**Goal**: 100% AC compliance and audit.

### Gap 6.1: Run All Tests

**Status**: [ ]

**Commands**:
```bash
dotnet test --filter "FullyQualifiedName~Vllm.Health" --verbosity normal
dotnet test --filter "FullyQualifiedName~Vllm.Exceptions" --verbosity normal

# Expected: ~45 tests passing
```

**Success Criteria**:
- [ ] All tests passing (0 failures)
- [ ] Test count >= 40
- [ ] No skipped tests (except conditional integration tests)

---

### Gap 6.2: Verify All Files Exist Per Spec

**Status**: [ ]

**Expected Files** (16 production + 8 test = 24 total):

**Production**:
- [ ] HealthStatus.cs (enum)
- [ ] VllmHealthConfiguration.cs
- [ ] VllmHealthResult.cs (renamed from VllmHealthStatus)
- [ ] VllmHealthChecker.cs (updated)
- [ ] VllmLoadStatus.cs
- [ ] Metrics/VllmMetricsClient.cs
- [ ] Metrics/VllmMetricsParser.cs
- [ ] Errors/VllmErrorParser.cs
- [ ] Errors/VllmErrorClassifier.cs
- [ ] Errors/VllmExceptionMapper.cs
- [ ] Exceptions/IVllmException.cs
- [ ] Exceptions/VllmOutOfMemoryException.cs
- [ ] (8 existing exception files updated with interface)

**Tests**:
- [ ] VllmHealthCheckerTests.cs (updated, ~13 tests)
- [ ] VllmHealthConfigurationTests.cs (~6 tests)
- [ ] VllmMetricsParserTests.cs (~8 tests)
- [ ] VllmErrorParserTests.cs (~7 tests)
- [ ] VllmErrorClassifierTests.cs (~9 tests)
- [ ] VllmExceptionMapperTests.cs (~9 tests)
- [ ] VllmOutOfMemoryExceptionTests.cs (~3 tests)
- [ ] VllmHealthIntegrationTests.cs (optional, ~3 tests)

---

### Gap 6.3: Verify All Acceptance Criteria

**Status**: [ ]

**All 80 ACs** from spec lines 429-546:

**Health Check Queries** (7 ACs):
- [ ] AC-001 through AC-007

**Health Status** (7 ACs):
- [ ] AC-008 through AC-014

**Metrics Integration** (6 ACs):
- [ ] AC-015 through AC-020

**Load Status** (6 ACs):
- [ ] AC-021 through AC-026

**Model Availability** (5 ACs):
- [ ] AC-027 through AC-031

**Error Parsing** (7 ACs):
- [ ] AC-032 through AC-038

**Error Classification** (9 ACs):
- [ ] AC-039 through AC-047

**Exception Mapping** (8 ACs):
- [ ] AC-048 through AC-056

**Exception Content** (6 ACs):
- [ ] AC-057 through AC-062

**CLI Integration** (7 ACs):
- [ ] AC-063 through AC-069 (provided for Task 004.c)

**Logging** (6 ACs):
- [ ] AC-070 through AC-075

**Performance** (5 ACs):
- [ ] AC-076 through AC-080

---

### Gap 6.4: Build with Zero Errors/Warnings

**Status**: [ ]

**Command**:
```bash
dotnet clean
dotnet build --configuration Release
```

**Success Criteria**:
- [ ] Build: succeeded
- [ ] 0 Error(s)
- [ ] 0 Warning(s)

---

### Gap 6.5: Create Audit Report

**Status**: [ ]

**File**: docs/audits/task-006c-audit-report.md

**Success Criteria**:
- [ ] Audit report created
- [ ] All 80 ACs documented as complete
- [ ] All verification checks passed

---

### Gap 6.6: Create PR

**Status**: [ ]

**Commands**:
```bash
git add .
git commit -m "feat(task-006c): implement health checking and error handling

- Four-state health model (Healthy/Degraded/Unhealthy/Unknown)
- Response time threshold checking
- /v1/models fallback endpoint query
- Load monitoring with Prometheus metrics
- Comprehensive error handling subsystem
- IVllmException interface for all exceptions
- VllmOutOfMemoryException for CUDA OOM
- Error parser, classifier, and exception mapper
- Structured logging with ILogger
- 45+ tests covering all subsystems

All 80 ACs verified complete.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"

git push origin feature/task-006a-fix-gaps

gh pr create --title "Task 006c: Load/Health-Check Endpoints + Error Handling" --body "..."
```

**Success Criteria**:
- [ ] All work committed
- [ ] PR created
- [ ] PR includes test results and audit report

---

## COMPLETION CRITERIA

**Task is COMPLETE when ALL of the following are true:**

- [ ] All Phase 0-6 gaps fixed
- [ ] All 80 acceptance criteria verified as ✅
- [ ] All ~40-45 tests passing (0 failures)
- [ ] Build succeeds with 0 errors, 0 warnings
- [ ] Audit report created and complete
- [ ] PR created with comprehensive description
- [ ] VllmHealthChecker has four-state health model
- [ ] Load monitoring with metrics parsing
- [ ] Error handling subsystem complete
- [ ] All exceptions implement IVllmException
- [ ] Structured logging throughout

**DO NOT mark task complete until ALL checkboxes above are ✅**

---

## NOTES

- Health checking was ~19% complete (basic liveness only)
- Entire load monitoring subsystem was missing (metrics)
- Error handling was ~40% complete (exceptions exist, parsers missing)
- Exception hierarchy mostly complete, missing IVllmException interface and VllmOutOfMemoryException
- No logging implemented (no ILogger dependency)
- Test coverage ~10% (5 tests vs 40-45 expected)
- CLI commands belong to Task 004.c but use this task's health checker
- This task provides the health checking API that Provider Registry consumes

---

**END OF COMPLETION CHECKLIST**
