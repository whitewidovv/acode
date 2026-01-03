# Task 019.b: Implement run_tests Wrapper

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 019 (Language Runners), Task 019.a (Layout Detection), Task 018 (Command Runner)  

---

## Description

Task 019.b implements the `run_tests` wrapper. The wrapper provides a unified interface for executing tests across different languages and frameworks. The agent MUST be able to run tests regardless of the underlying test framework.

Test execution is how the agent validates code changes. When the agent modifies code, it MUST run tests to verify correctness. The wrapper abstracts framework differences so the agent uses one consistent interface.

The wrapper MUST support .NET test frameworks: xUnit, NUnit, MSTest. Detection MUST be automatic based on project references. The correct test runner MUST be invoked for each framework.

The wrapper MUST support Node.js test frameworks: Jest, Mocha, Vitest. Detection MUST use package.json scripts and dependencies. The configured test script MUST be executed.

Test result parsing is MANDATORY. Raw output MUST be converted to structured results. Pass/fail counts, test names, durations, and failure messages MUST be extracted.

The wrapper MUST support filtering. Run specific tests by name pattern. Run tests in specific files. Run tests with specific tags. Filtering MUST work across all frameworks.

The wrapper MUST support parallel execution where the framework allows. .NET supports parallel test classes. Jest supports parallel files. Configuration MUST control parallelism.

Timeout enforcement is MANDATORY. Tests that hang MUST be killed. Partial results MUST be returned. The agent MUST NOT wait indefinitely.

Coverage collection is OPTIONAL but supported. When enabled, coverage data MUST be collected and stored. Coverage report format MUST be configurable.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Test Runner | Framework that executes tests |
| Test Framework | Library providing test APIs |
| xUnit | .NET testing framework |
| NUnit | .NET testing framework |
| MSTest | Microsoft's .NET test framework |
| Jest | JavaScript testing framework |
| Mocha | JavaScript testing framework |
| Vitest | Vite-based testing framework |
| Test Result | Structured execution outcome |
| Filter | Pattern to select tests |
| Coverage | Code execution measurement |
| TRX | Visual Studio test result format |
| JUnit XML | Standard test result format |
| TAP | Test Anything Protocol format |

---

## Out of Scope

The following items are explicitly excluded from Task 019.b:

- **Project detection** - See Task 019.a
- **Build execution** - See Task 019
- **Repo contract commands** - See Task 019.c
- **Python test runners** - Future versions
- **IDE test integration** - CLI only
- **Test generation** - Execution only
- **Debugging** - No debugger support
- **Performance profiling** - Basic timing only

---

## Functional Requirements

### Wrapper Interface

- FR-001: Define ITestRunner interface
- FR-002: RunTestsAsync method MUST exist
- FR-003: Accept test path parameter
- FR-004: Accept filter options parameter
- FR-005: Accept timeout parameter
- FR-006: Return TestRunResult
- FR-007: Support cancellation token

### Test Run Result

- FR-008: Define TestRunResult record
- FR-009: Store total test count
- FR-010: Store passed count
- FR-011: Store failed count
- FR-012: Store skipped count
- FR-013: Store duration
- FR-014: Store individual test results
- FR-015: Store raw output
- FR-016: Store coverage data (optional)

### Individual Test Result

- FR-017: Define TestResult record
- FR-018: Store test name
- FR-019: Store test namespace/class
- FR-020: Store pass/fail/skip status
- FR-021: Store duration
- FR-022: Store failure message
- FR-023: Store stack trace

### .NET Test Execution

- FR-024: Execute `dotnet test` command
- FR-025: Pass `--filter` for test filtering
- FR-026: Pass `--logger trx` for structured output
- FR-027: Parse TRX file for results
- FR-028: Support `--no-build` option
- FR-029: Support `--configuration` option
- FR-030: Support parallel execution

### .NET Framework Detection

- FR-031: Detect xUnit from package reference
- FR-032: Detect NUnit from package reference
- FR-033: Detect MSTest from package reference
- FR-034: Set appropriate test adapter

### Node.js Test Execution

- FR-035: Execute `npm test` command
- FR-036: Support custom test script name
- FR-037: Pass filter to test runner
- FR-038: Parse test output for results
- FR-039: Support `--passWithNoTests`
- FR-040: Support parallel execution

### Node.js Framework Detection

- FR-041: Detect Jest from dependencies
- FR-042: Detect Mocha from dependencies
- FR-043: Detect Vitest from dependencies
- FR-044: Configure appropriate flags

### Result Parsing

- FR-045: Parse TRX format (.NET)
- FR-046: Parse JUnit XML format
- FR-047: Parse JSON reporter output
- FR-048: Parse TAP format
- FR-049: Handle parse failures gracefully
- FR-050: Return raw output on parse failure

### Filtering

- FR-051: Filter by test name pattern
- FR-052: Filter by test class/describe
- FR-053: Filter by file path
- FR-054: Filter by tag/trait
- FR-055: Combine multiple filters

### Execution Control

- FR-056: Enforce timeout per test run
- FR-057: Kill hung process on timeout
- FR-058: Capture partial results on timeout
- FR-059: Retry failed tests option
- FR-060: Stop on first failure option

### Coverage (Optional)

- FR-061: Enable coverage collection
- FR-062: Configure coverage format
- FR-063: Store coverage results
- FR-064: Report coverage summary

---

## Non-Functional Requirements

### Performance

- NFR-001: Wrapper overhead MUST be < 100ms
- NFR-002: Result parsing MUST complete < 50ms
- NFR-003: Large result files (10MB) MUST parse < 500ms

### Reliability

- NFR-004: Wrapper MUST NOT crash on test failure
- NFR-005: Parse errors MUST NOT lose raw output
- NFR-006: Timeout MUST be enforced reliably

### Accuracy

- NFR-007: Pass/fail counts MUST match actual
- NFR-008: Durations MUST be within 100ms accuracy
- NFR-009: All test names MUST be captured

---

## User Manual Documentation

### Overview

The `run_tests` wrapper provides unified test execution across .NET and Node.js projects. It abstracts framework differences and provides consistent structured results.

### Configuration

```yaml
# .agent/config.yml
testing:
  # Default timeout (seconds)
  default_timeout_seconds: 300
  
  # Stop on first failure
  fail_fast: false
  
  # Retry count for flaky tests
  retry_count: 0
  
  # Parallel execution
  parallel: true
  
  # Coverage collection
  coverage:
    enabled: false
    format: cobertura
    
  # Framework-specific settings
  dotnet:
    configuration: Debug
    no_build: false
    verbosity: minimal
    
  node:
    test_script: test
    reporter: default
```

### CLI Commands

```bash
# Run all tests
acode test

# Run tests in specific project
acode test ./tests/MyApp.Tests

# Run tests matching pattern
acode test --filter "UserService"

# Run tests with timeout
acode test --timeout 120

# Run tests and stop on first failure
acode test --fail-fast

# Run tests with coverage
acode test --coverage

# Run tests in verbose mode
acode test --verbose
```

### Test Result Output

```bash
$ acode test

Running tests for MyApp.Tests...

Test Run Summary
────────────────
Total:   47
Passed:  45
Failed:   2
Skipped:  0
Duration: 3.2s

Failed Tests:
  ✗ UserServiceTests.GetById_WhenNotFound_ReturnsNull
    Expected: null
    Actual: threw NotFoundException
    at UserServiceTests.cs:45
    
  ✗ OrderServiceTests.Create_WithInvalidItems_Throws
    Timeout after 5000ms
    at OrderServiceTests.cs:123
```

### JSON Output

```bash
$ acode test --json

{
  "summary": {
    "total": 47,
    "passed": 45,
    "failed": 2,
    "skipped": 0,
    "durationMs": 3200
  },
  "tests": [
    {
      "name": "GetById_WhenExists_ReturnsUser",
      "fullName": "MyApp.Tests.UserServiceTests.GetById_WhenExists_ReturnsUser",
      "status": "passed",
      "durationMs": 45
    },
    {
      "name": "GetById_WhenNotFound_ReturnsNull",
      "fullName": "MyApp.Tests.UserServiceTests.GetById_WhenNotFound_ReturnsNull",
      "status": "failed",
      "durationMs": 12,
      "errorMessage": "Expected: null\nActual: threw NotFoundException",
      "stackTrace": "at UserServiceTests.cs:45"
    }
  ]
}
```

### Filter Syntax

| Framework | Filter Syntax |
|-----------|---------------|
| .NET | `--filter "FullyQualifiedName~UserService"` |
| .NET | `--filter "Category=Unit"` |
| Jest | `--testNamePattern="UserService"` |
| Mocha | `--grep "UserService"` |

### Framework Detection

The wrapper automatically detects the test framework:

**.NET Detection:**
- xUnit: `<PackageReference Include="xunit" />`
- NUnit: `<PackageReference Include="NUnit" />`
- MSTest: `<PackageReference Include="MSTest.TestFramework" />`

**Node.js Detection:**
- Jest: `"jest"` in devDependencies
- Mocha: `"mocha"` in devDependencies
- Vitest: `"vitest"` in devDependencies

### Troubleshooting

#### Tests Not Found

**Problem:** No tests discovered

**Solutions:**
1. Verify test project is built
2. Check test framework package is installed
3. Verify test class/method attributes are correct
4. Run with `--verbose` to see discovery output

#### Parse Failure

**Problem:** Results not structured

**Solutions:**
1. Check test framework version
2. Verify TRX/JSON output is enabled
3. Check for corrupted output file
4. Raw output WILL be available

#### Timeout

**Problem:** Test run times out

**Solutions:**
1. Increase `--timeout` value
2. Check for infinite loops in tests
3. Check for external dependencies
4. Run specific test to isolate

---

## Acceptance Criteria

### Interface

- [ ] AC-001: ITestRunner interface MUST be defined
- [ ] AC-002: RunTestsAsync method MUST exist
- [ ] AC-003: TestRunResult MUST contain all required fields

### .NET Execution

- [ ] AC-004: `dotnet test` MUST execute successfully
- [ ] AC-005: TRX output MUST be generated
- [ ] AC-006: TRX MUST be parsed correctly
- [ ] AC-007: Filter MUST work

### Node.js Execution

- [ ] AC-008: `npm test` MUST execute successfully
- [ ] AC-009: Output MUST be parsed
- [ ] AC-010: Filter MUST work

### Framework Detection

- [ ] AC-011: xUnit MUST be detected
- [ ] AC-012: NUnit MUST be detected
- [ ] AC-013: Jest MUST be detected

### Result Parsing

- [ ] AC-014: Pass/fail counts MUST be accurate
- [ ] AC-015: Test names MUST be captured
- [ ] AC-016: Failure messages MUST be captured
- [ ] AC-017: Durations MUST be captured

### Execution Control

- [ ] AC-018: Timeout MUST be enforced
- [ ] AC-019: Partial results MUST be returned on timeout
- [ ] AC-020: Fail-fast MUST work

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Runners/Testing/
├── TestRunnerTests.cs
│   ├── Should_Run_Tests()
│   ├── Should_Return_Results()
│   └── Should_Handle_Failure()
│
├── DotNetTestExecutorTests.cs
│   ├── Should_Build_Command()
│   ├── Should_Apply_Filter()
│   └── Should_Enable_TRX_Output()
│
├── NodeTestExecutorTests.cs
│   ├── Should_Build_Command()
│   ├── Should_Apply_Filter()
│   └── Should_Detect_Framework()
│
├── TrxParserTests.cs
│   ├── Should_Parse_Passed_Tests()
│   ├── Should_Parse_Failed_Tests()
│   └── Should_Handle_Malformed()
│
└── FilterBuilderTests.cs
    ├── Should_Build_DotNet_Filter()
    └── Should_Build_Jest_Filter()
```

### Integration Tests

```
Tests/Integration/Runners/Testing/
├── DotNetTestIntegrationTests.cs
│   ├── Should_Run_Real_XUnit_Tests()
│   └── Should_Parse_Real_TRX()
│
└── NodeTestIntegrationTests.cs
    └── Should_Run_Real_Jest_Tests()
```

### E2E Tests

```
Tests/E2E/Runners/Testing/
├── TestRunnerE2ETests.cs
│   ├── Should_Run_Tests_Via_CLI()
│   └── Should_Filter_Tests_Via_CLI()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Wrapper overhead | 50ms | 100ms |
| TRX parse 1MB | 100ms | 200ms |
| JSON parse 1MB | 50ms | 100ms |

---

## User Verification Steps

### Scenario 1: Run .NET Tests

1. Create .NET test project with xUnit
2. Run `acode test`
3. Verify: Tests execute and results displayed

### Scenario 2: Run Node.js Tests

1. Create Node.js project with Jest
2. Run `acode test`
3. Verify: Tests execute and results displayed

### Scenario 3: Filter Tests

1. Create project with multiple tests
2. Run `acode test --filter "Specific"`
3. Verify: Only matching tests run

### Scenario 4: Handle Failures

1. Create project with failing test
2. Run `acode test`
3. Verify: Failure details shown

### Scenario 5: Timeout

1. Create test with infinite loop
2. Run `acode test --timeout 5`
3. Verify: Killed after 5 seconds

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Testing/
│   ├── ITestRunner.cs
│   ├── TestRunResult.cs
│   ├── TestResult.cs
│   └── TestFilter.cs
│
src/AgenticCoder.Infrastructure/
├── Testing/
│   ├── TestRunnerService.cs
│   ├── DotNetTestExecutor.cs
│   ├── NodeTestExecutor.cs
│   ├── Parsers/
│   │   ├── TrxParser.cs
│   │   ├── JUnitParser.cs
│   │   └── JsonReporterParser.cs
│   └── Filters/
│       ├── DotNetFilterBuilder.cs
│       └── JestFilterBuilder.cs
```

### ITestRunner Interface

```csharp
namespace AgenticCoder.Domain.Testing;

public interface ITestRunner
{
    Task<TestRunResult> RunTestsAsync(
        string projectPath,
        TestOptions? options = null,
        CancellationToken ct = default);
}

public record TestOptions
{
    public string? Filter { get; init; }
    public TimeSpan? Timeout { get; init; }
    public bool FailFast { get; init; }
    public bool CollectCoverage { get; init; }
    public bool NoBuild { get; init; }
}
```

### TestRunResult Record

```csharp
public record TestRunResult
{
    public required int TotalTests { get; init; }
    public required int PassedTests { get; init; }
    public required int FailedTests { get; init; }
    public required int SkippedTests { get; init; }
    public required TimeSpan Duration { get; init; }
    public required IReadOnlyList<TestResult> Tests { get; init; }
    public string? RawOutput { get; init; }
    public bool TimedOut { get; init; }
    public string? CoverageSummary { get; init; }
}

public record TestResult
{
    public required string Name { get; init; }
    public required string FullName { get; init; }
    public required TestStatus Status { get; init; }
    public required TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
    public string? StackTrace { get; init; }
}

public enum TestStatus
{
    Passed,
    Failed,
    Skipped
}
```

### DotNetTestExecutor Class

```csharp
public class DotNetTestExecutor
{
    private readonly ICommandExecutor _executor;
    private readonly TrxParser _parser;
    
    public async Task<TestRunResult> ExecuteAsync(
        string projectPath,
        TestOptions options,
        CancellationToken ct)
    {
        var trxPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.trx");
        
        var command = new Command
        {
            Executable = "dotnet",
            Arguments = BuildArguments(projectPath, options, trxPath),
            WorkingDirectory = projectPath,
            Timeout = options.Timeout ?? TimeSpan.FromMinutes(5)
        };
        
        var result = await _executor.ExecuteAsync(command, ct: ct);
        
        if (File.Exists(trxPath))
        {
            return _parser.Parse(trxPath, result);
        }
        
        return CreateFailedResult(result);
    }
    
    private IReadOnlyList<string> BuildArguments(
        string path, 
        TestOptions options, 
        string trxPath)
    {
        var args = new List<string> { "test", path };
        args.Add($"--logger:trx;LogFileName={trxPath}");
        
        if (!string.IsNullOrEmpty(options.Filter))
            args.Add($"--filter:{options.Filter}");
            
        if (options.NoBuild)
            args.Add("--no-build");
            
        return args;
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-TST-001 | Test execution failed |
| ACODE-TST-002 | Test parse failed |
| ACODE-TST-003 | Test timeout |
| ACODE-TST-004 | Framework not detected |

### Implementation Checklist

1. [ ] Create ITestRunner interface
2. [ ] Create TestRunResult and TestResult records
3. [ ] Implement DotNetTestExecutor
4. [ ] Implement TrxParser
5. [ ] Implement NodeTestExecutor
6. [ ] Implement JSON parser
7. [ ] Add filter builders
8. [ ] Add CLI integration
9. [ ] Write unit tests for all components
10. [ ] Write integration tests

### Rollout Plan

1. **Phase 1:** Interface and result models
2. **Phase 2:** .NET test executor
3. **Phase 3:** TRX parser
4. **Phase 4:** Node.js test executor
5. **Phase 5:** Filter builders
6. **Phase 6:** CLI integration

---

**End of Task 019.b Specification**