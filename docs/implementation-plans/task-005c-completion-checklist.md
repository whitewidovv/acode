# Task 005c - Gap Analysis and Implementation Checklist

## INSTRUCTIONS FOR CONTINUING AGENTS

This checklist identifies ALL gaps between the task specification and current implementation.
Work through gaps sequentially following TDD (RED → GREEN → REFACTOR).
Mark gaps as [🔄] when starting, [✅] when complete with evidence.

## WHAT EXISTS (Already Complete)

✅ **docs/ollama-setup.md** - Complete documentation with all required sections:
   - Prerequisites (Ollama installation, model download)
   - Quick Start (under 50 lines)
   - Configuration (all settings documented with defaults)
   - Troubleshooting (all 6 common issues addressed)
   - Version Compatibility (tested versions listed)
   - Diagnostic Commands

✅ **scripts/smoke-test-ollama.ps1** - Complete PowerShell script with:
   - All 5 test functions (HealthCheck, ModelList, Completion, Streaming, ToolCalling)
   - Proper exit codes (0 success, 1 test failure, 2 config error)
   - Verbose and quiet modes
   - Parameter support (endpoint, model, timeout, skip-tool-test)

✅ **scripts/smoke-test-ollama.sh** - Complete Bash equivalent script

✅ **src/Acode.Cli/Commands/ProvidersCommand.cs** - File exists but is a STUB (NotImplementedException)

## GAPS IDENTIFIED (What's Missing)

### Gap #1: TestResult Domain Model
**Status**: [ ]
**File to Create**: `src/Acode.Infrastructure/Ollama/SmokeTest/Output/TestResult.cs`
**Why Needed**: FR-071 to FR-077 require structured test result output
**Implementation**:
```csharp
namespace Acode.Infrastructure.Ollama.SmokeTest.Output;

public sealed record TestResult
{
    required public string TestName { get; init; }
    required public bool Passed { get; init; }
    required public TimeSpan ElapsedTime { get; init; }
    public string? ErrorMessage { get; init; }
    public string? DiagnosticHint { get; init; }
}

public sealed record SmokeTestResults
{
    required public List<TestResult> Results { get; init; }
    required public DateTime CheckedAt { get; init; }
    required public TimeSpan TotalDuration { get; init; }
    public bool AllPassed => Results.All(r => r.Passed);
    public int PassedCount => Results.Count(r => r.Passed);
    public int FailedCount => Results.Count(r => !r.Passed);
}
```
**Success Criteria**: Models can represent test results and aggregate status
**Evidence**: [To be filled when complete]

---

### Gap #2: TestResult Tests (RED Phase)
**Status**: [ ]
**File to Create**: `tests/Acode.Infrastructure.Tests/Ollama/SmokeTest/Output/TestResultTests.cs`
**Why Needed**: TDD requires tests before implementation
**Required Tests**:
1. TestResult_HasRequiredProperties
2. TestResult_SupportsFailureWithError
3. SmokeTestResults_CalculatesAllPassed
4. SmokeTestResults_CountsPassedAndFailed
**Success Criteria**: All tests pass after Gap #1 implemented
**Evidence**: [To be filled when complete]

---

### Gap #3: ITestReporter Interface and Implementations
**Status**: [ ]
**Files to Create**:
- `src/Acode.Infrastructure/Ollama/SmokeTest/Output/ITestReporter.cs`
- `src/Acode.Infrastructure/Ollama/SmokeTest/Output/TextTestReporter.cs`
- `src/Acode.Infrastructure/Ollama/SmokeTest/Output/JsonTestReporter.cs`
**Why Needed**: FR-076, FR-077 require multiple output formats
**Implementation Pattern**:
```csharp
public interface ITestReporter
{
    void Report(SmokeTestResults results, bool verbose = false);
}

public sealed class TextTestReporter : ITestReporter
{
    // Formats output as human-readable text with colors
}

public sealed class JsonTestReporter : ITestReporter
{
    // Formats output as JSON for parsing
}
```
**Success Criteria**: Can format test results in text and JSON formats
**Evidence**: [To be filled when complete]

---

### Gap #4: TestReporter Tests (RED Phase)
**Status**: [ ]
**File to Create**: `tests/Acode.Infrastructure.Tests/Ollama/SmokeTest/Output/TestReporterTests.cs`
**Why Needed**: TDD requires tests for output formatting
**Required Tests**:
1. TextReporter_FormatsPassedTest
2. TextReporter_FormatsFailedTest
3. TextReporter_FormatsSummary
4. JsonReporter_ProducesValidJson
**Success Criteria**: All tests pass after Gap #3 implemented
**Evidence**: [To be filled when complete]

---

### Gap #5: ISmokeTest Interface
**Status**: [ ]
**File to Create**: `src/Acode.Infrastructure/Ollama/SmokeTest/ISmokeTest.cs`
**Why Needed**: Common interface for all test types
**Implementation**:
```csharp
namespace Acode.Infrastructure.Ollama.SmokeTest;

public interface ISmokeTest
{
    string Name { get; }
    Task<TestResult> RunAsync(SmokeTestOptions options, CancellationToken cancellationToken);
}

public sealed record SmokeTestOptions
{
    required public string Endpoint { get; init; }
    required public string Model { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(60);
    public bool SkipToolTest { get; init; } = false;
}
```
**Success Criteria**: Interface allows polymorphic test execution
**Evidence**: [To be filled when complete]

---

### Gap #6: HealthCheckTest Implementation
**Status**: [ ]
**File to Create**: `src/Acode.Infrastructure/Ollama/SmokeTest/Tests/HealthCheckTest.cs`
**Why Needed**: FR-060, FR-061 - verify /api/tags connectivity
**Implementation Pattern**:
```csharp
public sealed class HealthCheckTest : ISmokeTest
{
    public string Name => "Health Check";

    public async Task<TestResult> RunAsync(SmokeTestOptions options, CancellationToken ct)
    {
        // Call /api/tags with 5 second timeout
        // Return TestResult with pass/fail and timing
    }
}
```
**Success Criteria**: Can detect if Ollama is reachable
**Evidence**: [To be filled when complete]

---

### Gap #7: ModelListTest Implementation
**Status**: [ ]
**File to Create**: `src/Acode.Infrastructure/Ollama/SmokeTest/Tests/ModelListTest.cs`
**Why Needed**: FR-062, FR-063 - verify model enumeration
**Implementation**: Similar to HealthCheckTest but parses model list
**Success Criteria**: Can list models and verify at least one exists
**Evidence**: [To be filled when complete]

---

### Gap #8: CompletionTest Implementation
**Status**: [ ]
**File to Create**: `src/Acode.Infrastructure/Ollama/SmokeTest/Tests/CompletionTest.cs`
**Why Needed**: FR-064, FR-065, FR-066 - verify non-streaming completion
**Implementation**: Send simple prompt, verify response and finish reason
**Success Criteria**: Can complete a simple prompt and verify response
**Evidence**: [To be filled when complete]

---

### Gap #9: StreamingTest Implementation
**Status**: [ ]
**File to Create**: `src/Acode.Infrastructure/Ollama/SmokeTest/Tests/StreamingTest.cs`
**Why Needed**: FR-067, FR-068 - verify streaming completion
**Implementation**: Send prompt with streaming, verify multiple chunks received
**Success Criteria**: Can stream response and verify chunks
**Evidence**: [To be filled when complete]

---

### Gap #10: ToolCallTest Implementation
**Status**: [ ]
**File to Create**: `src/Acode.Infrastructure/Ollama/SmokeTest/Tests/ToolCallTest.cs`
**Why Needed**: FR-069, FR-070 - verify tool calling (deferred to 007d)
**Implementation**: STUB that skips test with message "Requires Task 007d"
**Success Criteria**: Test exists but returns skipped status
**Evidence**: [To be filled when complete]

---

### Gap #11: Smoke Test Tests (RED Phase)
**Status**: [ ]
**File to Create**: `tests/Acode.Infrastructure.Tests/Ollama/SmokeTest/Tests/SmokeTestTests.cs`
**Why Needed**: TDD requires unit tests for each test class
**Required Tests** (for each test class):
1. TestName_IsCorrect
2. RunAsync_Passes_WhenOllamaResponds
3. RunAsync_Fails_WhenOllamaUnreachable
4. RunAsync_RespectsTimeout
**Success Criteria**: All tests pass after implementing test classes
**Evidence**: [To be filled when complete]

---

### Gap #12: OllamaSmokeTestRunner Implementation
**Status**: [ ]
**File to Create**: `src/Acode.Infrastructure/Ollama/SmokeTest/OllamaSmokeTestRunner.cs`
**Why Needed**: FR-039 to FR-051 - orchestrate all tests
**Implementation Pattern**:
```csharp
public sealed class OllamaSmokeTestRunner
{
    private readonly List<ISmokeTest> _tests;

    public OllamaSmokeTestRunner()
    {
        _tests = new List<ISmokeTest>
        {
            new HealthCheckTest(),
            new ModelListTest(),
            new CompletionTest(),
            new StreamingTest(),
            new ToolCallTest()
        };
    }

    public async Task<SmokeTestResults> RunAsync(SmokeTestOptions options, CancellationToken ct)
    {
        // Run tests sequentially, stop on health check failure
        // Return aggregate results
    }
}
```
**Success Criteria**: Can orchestrate all tests and aggregate results
**Evidence**: [To be filled when complete]

---

### Gap #13: SmokeTestRunner Tests (RED Phase)
**Status**: [ ]
**File to Create**: `tests/Acode.Infrastructure.Tests/Ollama/SmokeTest/OllamaSmokeTestRunnerTests.cs`
**Why Needed**: TDD for test runner orchestration
**Required Tests**:
1. RunAsync_ExecutesAllTests
2. RunAsync_StopsAfterHealthCheckFailure
3. RunAsync_SkipsToolTestWhenFlagged
4. RunAsync_ReturnsAggregateResults
**Success Criteria**: All tests pass
**Evidence**: [To be filled when complete]

---

### Gap #14: ProvidersCommand Implementation
**Status**: [ ]
**File to Modify**: `src/Acode.Cli/Commands/ProvidersCommand.cs`
**Why Needed**: FR-052 to FR-059 - CLI integration for smoke tests
**Current State**: File exists but throws NotImplementedException
**Required Changes**:
1. Parse subcommand (smoke-test)
2. Parse provider argument (ollama)
3. Parse flags (--verbose, --skip-tool-test, --model, --timeout, --output)
4. Load configuration from .agent/config.yml
5. Create OllamaSmokeTestRunner
6. Execute tests and format output
7. Return proper exit code
**Implementation Pattern**:
```csharp
public async Task<ExitCode> ExecuteAsync(CommandContext context)
{
    // Parse: acode providers smoke-test ollama [options]
    // Extract subcommand and provider
    // Parse flags
    // Load config
    // Run tests
    // Format output
    // Return exit code
}
```
**Success Criteria**: `acode providers smoke-test ollama` runs all tests
**Evidence**: [To be filled when complete]

---

### Gap #15: ProvidersCommand Tests (RED Phase)
**Status**: [ ]
**File to Create**: `tests/Acode.Cli.Tests/Commands/ProvidersCommandTests.cs`
**Why Needed**: TDD for CLI command
**Required Tests**:
1. SmokeTest_Ollama_RunsAllTests
2. SmokeTest_ParsesVerboseFlag
3. SmokeTest_ParsesSkipToolTestFlag
4. SmokeTest_ParsesModelFlag
5. SmokeTest_ParsesTimeoutFlag
6. SmokeTest_LoadsConfiguration
7. SmokeTest_ReturnsExitCode0OnSuccess
8. SmokeTest_ReturnsExitCode1OnFailure
**Success Criteria**: All tests pass
**Evidence**: [To be filled when complete]

---

### Gap #16: Integration Tests
**Status**: [ ]
**File to Create**: `tests/Acode.Integration.Tests/Ollama/SmokeTest/SmokeTestIntegrationTests.cs`
**Why Needed**: Verify end-to-end smoke test functionality
**Required Tests**:
1. SmokeTest_CompleteAllTests_WhenOllamaRunning
2. SmokeTest_FailsGracefully_WhenOllamaNotRunning
**Success Criteria**: Integration tests pass
**Evidence**: [To be filled when complete]

---

## IMPLEMENTATION ORDER (TDD)

Following RED → GREEN → REFACTOR cycle:

**Phase 1: Domain Models**
1. Gap #1: TestResult models (GREEN - no tests needed for simple records)
2. Gap #2: TestResult tests (RED then verify GREEN)

**Phase 2: Output Formatting**
3. Gap #3: ITestReporter interface and implementations (GREEN after tests)
4. Gap #4: TestReporter tests (RED then verify GREEN)

**Phase 3: Test Infrastructure**
5. Gap #5: ISmokeTest interface and SmokeTestOptions (GREEN)
6. Gap #6-10: Individual test implementations (GREEN after tests)
7. Gap #11: Smoke test unit tests (RED then verify GREEN)

**Phase 4: Test Runner**
8. Gap #12: OllamaSmokeTestRunner (GREEN after tests)
9. Gap #13: SmokeTestRunner tests (RED then verify GREEN)

**Phase 5: CLI Integration**
10. Gap #14: ProvidersCommand implementation (GREEN after tests)
11. Gap #15: ProvidersCommand tests (RED then verify GREEN)

**Phase 6: Integration**
12. Gap #16: Integration tests (RED then verify GREEN)

## DEFERRED ITEMS (Dependencies on 005a/005b/007d)

- **ToolCallTest Full Implementation**: Currently a stub that skips. Full implementation depends on Task 007d (tool calling support). When 007d is complete, update ToolCallTest to actually test tool calling functionality.

## COMPLETION CRITERIA

Task 005c is complete when:
- ✅ All 16 gaps are marked complete with evidence
- ✅ All unit tests pass
- ✅ All integration tests pass
- ✅ `dotnet build` succeeds with 0 warnings
- ✅ `acode providers smoke-test ollama` command works (assuming Ollama running)
- ✅ Audit passes (docs/AUDIT-GUIDELINES.md)
- ✅ PR created and approved

## PROGRESS TRACKING

- **Total Gaps**: 16
- **Completed**: 0
- **In Progress**: 0
- **Remaining**: 16
- **Completion**: 0%

---

**Last Updated**: 2026-01-13 (Initial gap analysis)
