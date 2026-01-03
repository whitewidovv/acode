# Task 019.b: Implement run_tests Wrapper

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 019 (Language Runners), Task 019.a (Layout Detection), Task 018 (Command Runner)  

---

## Description

### Overview

Task 019.b implements the unified `run_tests` wrapper that enables the Agentic Coding Bot to execute tests across different languages and frameworks through a single, consistent interface. When the agent modifies code, it MUST run tests to validate correctness—this wrapper abstracts away framework-specific invocation details (dotnet test vs npm test vs pytest), output formats (TRX vs JUnit XML vs TAP), and filtering syntax differences, presenting a unified API that returns structured, queryable test results.

### Business Value

1. **Validation Loop**: The agent's ability to verify code changes through tests is CRITICAL for autonomous operation—without reliable test execution, the agent cannot confirm its modifications work correctly
2. **Framework Agnosticism**: Developers use xUnit, NUnit, MSTest, Jest, Mocha, Vitest and more—the wrapper ensures the agent works with ANY supported framework without per-project configuration
3. **Structured Results**: Machine-readable test results (pass/fail counts, individual test outcomes, failure messages) enable the agent to programmatically analyze failures and fix them
4. **Consistent Developer Experience**: Users see the same test output format regardless of underlying framework, simplifying interpretation and comparison
5. **Timeout Protection**: Tests that hang don't block the agent indefinitely—timeout enforcement ensures operations complete or fail cleanly
6. **CI/CD Parity**: The wrapper produces output compatible with CI systems, enabling seamless integration

### Scope

This task delivers:

1. **ITestRunner Interface**: Core abstraction for running tests with options for filtering, timeout, and coverage
2. **TestRunResult Model**: Structured result containing counts, durations, individual test outcomes, and optional coverage data
3. **.NET Test Execution**: Invocation of `dotnet test` with TRX logging and result parsing for xUnit/NUnit/MSTest
4. **Node.js Test Execution**: Invocation of npm/yarn/pnpm test scripts with output parsing for Jest/Mocha/Vitest
5. **Result Parsers**: TRX parser for .NET, JSON reporter parser for Jest, TAP parser for Node.js frameworks
6. **Filter Builders**: Framework-specific filter syntax generation from a unified filter model
7. **Timeout and Cancellation**: Reliable process termination with partial result capture
8. **CLI Command**: `acode test` command with filtering, timeout, coverage, and output format options

### Integration Points

| Component | Integration Type | Purpose |
|-----------|------------------|---------|
| Task-018 (Command Runner) | Dependency | Executes test processes |
| Task-019a (Layout Detection) | Dependency | Locates test projects |
| Task-019 (Language Runners) | Parent | Test runner is a specialized language runner |
| Task-018c (Artifacts) | Consumer | Stores test result artifacts |
| Task-005 (CLI Architecture) | Integration | CLI test command |
| Task-002a (Config) | Configuration | Test runner settings |

### Failure Modes

| Failure | Detection | Recovery |
|---------|-----------|----------|
| Test framework not detected | No matching packages | Return error with framework installation instructions |
| TRX parse failure | XmlException | Return raw output, log warning, agent can still see failures |
| Test process crash | Non-zero exit without results | Return execution error with stderr content |
| Test timeout | CancellationToken triggered | Kill process, return partial results with timeout flag |
| All tests fail | FailedTests == TotalTests | Return full results (this is valid, not an error) |
| No tests found | TotalTests == 0 | Return warning but success (configurable behavior) |

### Assumptions

1. Test projects are already built (or --no-build is not specified)
2. Test frameworks are properly installed via package references
3. .NET SDK and Node.js runtime are available on PATH
4. File system access allows writing temporary TRX files
5. Test output can be captured via stdout/stderr redirection

### Security Considerations

1. **Process Isolation**: Tests run in separate processes, not in agent's process
2. **Timeout Enforcement**: Prevents malicious or broken tests from blocking indefinitely
3. **Output Size Limits**: Large test outputs truncated to prevent memory exhaustion
4. **No Arbitrary Execution**: Only recognized test commands executed, not arbitrary scripts

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

### Test Runner Interface (FR-019B-01 to FR-019B-15)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-019B-01 | Define `ITestRunner` interface | Must Have |
| FR-019B-02 | `RunTestsAsync` method MUST accept project path | Must Have |
| FR-019B-03 | `RunTestsAsync` method MUST accept `TestOptions` | Must Have |
| FR-019B-04 | `RunTestsAsync` method MUST accept `CancellationToken` | Must Have |
| FR-019B-05 | `RunTestsAsync` method MUST return `TestRunResult` | Must Have |
| FR-019B-06 | Support detecting test framework from project | Must Have |
| FR-019B-07 | Support explicit framework specification in options | Should Have |
| FR-019B-08 | Support multiple test projects in single run | Should Have |
| FR-019B-09 | Aggregate results across multiple projects | Should Have |
| FR-019B-10 | Support running specific test file | Should Have |
| FR-019B-11 | Support running specific test class | Should Have |
| FR-019B-12 | Support running specific test method | Should Have |
| FR-019B-13 | Support dry-run mode (list tests only) | Could Have |
| FR-019B-14 | Support verbose output mode | Should Have |
| FR-019B-15 | Return framework name used in result | Should Have |

### Test Run Result Model (FR-019B-16 to FR-019B-35)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-019B-16 | Define `TestRunResult` sealed record | Must Have |
| FR-019B-17 | Store total test count | Must Have |
| FR-019B-18 | Store passed test count | Must Have |
| FR-019B-19 | Store failed test count | Must Have |
| FR-019B-20 | Store skipped test count | Must Have |
| FR-019B-21 | Store total duration | Must Have |
| FR-019B-22 | Store list of individual `TestResult` | Must Have |
| FR-019B-23 | Store raw stdout output | Must Have |
| FR-019B-24 | Store raw stderr output | Should Have |
| FR-019B-25 | Store timeout flag if timed out | Must Have |
| FR-019B-26 | Store coverage summary if collected | Should Have |
| FR-019B-27 | Store coverage file path if collected | Should Have |
| FR-019B-28 | Store exit code | Should Have |
| FR-019B-29 | Store start and end timestamps | Should Have |
| FR-019B-30 | Store framework used | Should Have |
| FR-019B-31 | Store project path | Must Have |
| FR-019B-32 | Provide `Success` computed property (failed == 0) | Must Have |
| FR-019B-33 | Provide `HasFailures` computed property | Must Have |
| FR-019B-34 | Support serialization to JSON | Should Have |
| FR-019B-35 | Result is immutable after creation | Must Have |

### Individual Test Result (FR-019B-36 to FR-019B-52)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-019B-36 | Define `TestResult` sealed record | Must Have |
| FR-019B-37 | Store test name (method name) | Must Have |
| FR-019B-38 | Store fully qualified name (namespace.class.method) | Must Have |
| FR-019B-39 | Store class/describe name | Should Have |
| FR-019B-40 | Store namespace | Should Have |
| FR-019B-41 | Store `TestStatus` (Passed, Failed, Skipped) | Must Have |
| FR-019B-42 | Store duration | Must Have |
| FR-019B-43 | Store failure message | Must Have |
| FR-019B-44 | Store stack trace | Must Have |
| FR-019B-45 | Store expected value (for assertion failures) | Should Have |
| FR-019B-46 | Store actual value (for assertion failures) | Should Have |
| FR-019B-47 | Store stdout output for test | Could Have |
| FR-019B-48 | Store stderr output for test | Could Have |
| FR-019B-49 | Store test file path | Should Have |
| FR-019B-50 | Store test line number | Could Have |
| FR-019B-51 | Store test traits/tags | Should Have |
| FR-019B-52 | Store retry count if retried | Could Have |

### .NET Test Execution (FR-019B-53 to FR-019B-72)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-019B-53 | Execute `dotnet test` command | Must Have |
| FR-019B-54 | Pass project/solution path to command | Must Have |
| FR-019B-55 | Add `--logger:trx` for TRX output | Must Have |
| FR-019B-56 | Specify TRX output path in temp directory | Must Have |
| FR-019B-57 | Parse TRX file for structured results | Must Have |
| FR-019B-58 | Pass `--filter` when filter specified | Must Have |
| FR-019B-59 | Support `--no-build` option | Should Have |
| FR-019B-60 | Support `--configuration Debug/Release` | Should Have |
| FR-019B-61 | Support `--verbosity` option | Should Have |
| FR-019B-62 | Support `--blame` for crash detection | Could Have |
| FR-019B-63 | Support `--collect "Code Coverage"` | Should Have |
| FR-019B-64 | Support `--settings` for runsettings file | Could Have |
| FR-019B-65 | Support parallel execution control | Should Have |
| FR-019B-66 | Pass `--results-directory` for output location | Should Have |
| FR-019B-67 | Clean up TRX files after parsing | Should Have |
| FR-019B-68 | Handle solution-level test execution | Should Have |
| FR-019B-69 | Handle project-level test execution | Must Have |
| FR-019B-70 | Handle directory with multiple test projects | Should Have |
| FR-019B-71 | Support .NET Framework projects (not just Core) | Could Have |
| FR-019B-72 | Detect and report build failures before test | Should Have |

### .NET Framework Detection (FR-019B-73 to FR-019B-82)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-019B-73 | Detect xUnit from `xunit` package reference | Must Have |
| FR-019B-74 | Detect xUnit from `xunit.v3` package reference | Should Have |
| FR-019B-75 | Detect NUnit from `NUnit` package reference | Must Have |
| FR-019B-76 | Detect NUnit from `NUnit3TestAdapter` | Must Have |
| FR-019B-77 | Detect MSTest from `MSTest.TestFramework` | Must Have |
| FR-019B-78 | Detect MSTest from `Microsoft.VisualStudio.TestPlatform.TestFramework` | Should Have |
| FR-019B-79 | Detect TUnit from `TUnit` package reference | Could Have |
| FR-019B-80 | Report detected framework in result | Should Have |
| FR-019B-81 | Use detection from Task-019a when available | Should Have |
| FR-019B-82 | Fall back to parsing .csproj if detection unavailable | Should Have |

### Node.js Test Execution (FR-019B-83 to FR-019B-100)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-019B-83 | Execute `npm test` command (default) | Must Have |
| FR-019B-84 | Support `npm run <custom-script>` | Should Have |
| FR-019B-85 | Support `yarn test` command | Should Have |
| FR-019B-86 | Support `pnpm test` command | Should Have |
| FR-019B-87 | Pass filter pattern to test runner | Must Have |
| FR-019B-88 | Parse test output for results | Must Have |
| FR-019B-89 | Support `--passWithNoTests` for Jest | Should Have |
| FR-019B-90 | Support `--forceExit` for Jest | Should Have |
| FR-019B-91 | Support `--json` reporter for Jest | Must Have |
| FR-019B-92 | Support `--reporter json` for Mocha | Should Have |
| FR-019B-93 | Support `--reporter json` for Vitest | Should Have |
| FR-019B-94 | Support parallel execution flags | Should Have |
| FR-019B-95 | Support coverage with `--coverage` | Should Have |
| FR-019B-96 | Detect Jest configuration from jest.config.js | Should Have |
| FR-019B-97 | Handle workspace-level test execution | Should Have |
| FR-019B-98 | Handle package-level test execution | Must Have |
| FR-019B-99 | Support custom test command from config | Should Have |
| FR-019B-100 | Handle missing test script gracefully | Should Have |

### Result Parsing (FR-019B-101 to FR-019B-115)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-019B-101 | Define `ITestResultParser` interface | Must Have |
| FR-019B-102 | Implement TRX parser for .NET | Must Have |
| FR-019B-103 | Implement JUnit XML parser | Should Have |
| FR-019B-104 | Implement Jest JSON parser | Must Have |
| FR-019B-105 | Implement TAP format parser | Could Have |
| FR-019B-106 | Implement Mocha JSON parser | Should Have |
| FR-019B-107 | Implement Vitest JSON parser | Should Have |
| FR-019B-108 | Handle parse failures gracefully | Must Have |
| FR-019B-109 | Return raw output when parse fails | Must Have |
| FR-019B-110 | Extract all test names | Must Have |
| FR-019B-111 | Extract all failure messages | Must Have |
| FR-019B-112 | Extract all stack traces | Must Have |
| FR-019B-113 | Calculate accurate durations | Must Have |
| FR-019B-114 | Handle large result files (10MB+) | Should Have |
| FR-019B-115 | Stream parsing for very large files | Could Have |

### Filtering (FR-019B-116 to FR-019B-128)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-019B-116 | Define `TestFilter` model | Must Have |
| FR-019B-117 | Support filter by test name pattern | Must Have |
| FR-019B-118 | Support filter by class/describe name | Should Have |
| FR-019B-119 | Support filter by namespace | Should Have |
| FR-019B-120 | Support filter by file path | Should Have |
| FR-019B-121 | Support filter by trait/tag | Should Have |
| FR-019B-122 | Support negation (exclude pattern) | Should Have |
| FR-019B-123 | Combine multiple filters with AND | Should Have |
| FR-019B-124 | Define `IFilterBuilder` interface | Must Have |
| FR-019B-125 | Implement .NET filter builder | Must Have |
| FR-019B-126 | Implement Jest filter builder | Must Have |
| FR-019B-127 | Implement Mocha filter builder | Should Have |
| FR-019B-128 | Validate filter syntax before execution | Should Have |

### Execution Control (FR-019B-129 to FR-019B-142)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-019B-129 | Enforce timeout per test run | Must Have |
| FR-019B-130 | Configurable timeout (default 5 minutes) | Must Have |
| FR-019B-131 | Kill hung process on timeout | Must Have |
| FR-019B-132 | Capture partial results on timeout | Must Have |
| FR-019B-133 | Set `TimedOut` flag in result | Must Have |
| FR-019B-134 | Support fail-fast (stop on first failure) | Should Have |
| FR-019B-135 | Support retry for failed tests | Could Have |
| FR-019B-136 | Configurable retry count | Could Have |
| FR-019B-137 | Track retry attempts in result | Could Have |
| FR-019B-138 | Support cancellation via token | Must Have |
| FR-019B-139 | Clean shutdown on cancellation | Must Have |
| FR-019B-140 | Support parallelism control | Should Have |
| FR-019B-141 | Log test execution start/end | Should Have |
| FR-019B-142 | Report progress during long runs | Could Have |

### Coverage (FR-019B-143 to FR-019B-150)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-019B-143 | Support enabling coverage collection | Should Have |
| FR-019B-144 | Configure coverage format (cobertura, lcov) | Should Have |
| FR-019B-145 | Store coverage report file | Should Have |
| FR-019B-146 | Parse coverage summary | Could Have |
| FR-019B-147 | Include line coverage percentage | Could Have |
| FR-019B-148 | Include branch coverage percentage | Could Have |
| FR-019B-149 | Support coverage thresholds | Could Have |
| FR-019B-150 | Report coverage in result summary | Should Have |

---

## Non-Functional Requirements

### Performance (NFR-019B-01 to NFR-019B-10)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-019B-01 | Test runner startup overhead | <200ms | Must Have |
| NFR-019B-02 | TRX file parsing | <100ms for 1,000 tests | Must Have |
| NFR-019B-03 | Jest JSON parsing | <100ms for 1,000 tests | Must Have |
| NFR-019B-04 | JUnit XML parsing | <100ms for 1,000 tests | Should Have |
| NFR-019B-05 | Filter construction | <10ms | Must Have |
| NFR-019B-06 | Memory for 10,000 test results | <50MB heap | Should Have |
| NFR-019B-07 | Memory for large TRX file (10MB) | <100MB peak | Should Have |
| NFR-019B-08 | Process monitoring overhead | <1% CPU | Must Have |
| NFR-019B-09 | Result aggregation | <50ms for 10 projects | Should Have |
| NFR-019B-10 | Temp file cleanup | <50ms | Should Have |

### Reliability (NFR-019B-11 to NFR-019B-18)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-019B-11 | Test runner crash handling | Graceful recovery | Must Have |
| NFR-019B-12 | Orphan process prevention | 100% cleanup | Must Have |
| NFR-019B-13 | Timeout enforcement accuracy | ±1 second | Must Have |
| NFR-019B-14 | Partial result capture on failure | Must capture passed | Must Have |
| NFR-019B-15 | File lock handling for TRX | Retry 3x with 100ms delay | Should Have |
| NFR-019B-16 | Parse failure recovery | Return raw output | Must Have |
| NFR-019B-17 | Temp file cleanup on crash | Best effort | Should Have |
| NFR-019B-18 | Consecutive test runs | No interference | Must Have |

### Security (NFR-019B-19 to NFR-019B-26)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-019B-19 | Command injection prevention | Strict argument quoting | Must Have |
| NFR-019B-20 | Filter pattern validation | Reject malicious | Must Have |
| NFR-019B-21 | Path traversal prevention | Canonicalize paths | Must Have |
| NFR-019B-22 | No credential logging | Strip from output | Must Have |
| NFR-019B-23 | Temp file permissions | Current user only | Should Have |
| NFR-019B-24 | Environment variable isolation | No leakage | Should Have |
| NFR-019B-25 | TRX file validation | Check XML structure | Should Have |
| NFR-019B-26 | Maximum output capture | Limit 10MB | Should Have |

### Maintainability (NFR-019B-27 to NFR-019B-36)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-019B-27 | New framework addition effort | <1 day | Should Have |
| NFR-019B-28 | New parser addition effort | <2 hours | Should Have |
| NFR-019B-29 | Test coverage for parsers | >90% | Must Have |
| NFR-019B-30 | Test coverage for runners | >85% | Must Have |
| NFR-019B-31 | Max cyclomatic complexity | 10 per method | Should Have |
| NFR-019B-32 | Max method lines | 50 lines | Should Have |
| NFR-019B-33 | Code duplication | <3% | Should Have |
| NFR-019B-34 | XML documentation | 100% public | Must Have |
| NFR-019B-35 | Async consistency | All I/O async | Must Have |
| NFR-019B-36 | Interface segregation | Single responsibility | Should Have |

### Observability (NFR-019B-37 to NFR-019B-46)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-019B-37 | Log test run start | Info level | Must Have |
| NFR-019B-38 | Log test run completion | Info with summary | Must Have |
| NFR-019B-39 | Log framework detection | Debug level | Should Have |
| NFR-019B-40 | Log command executed | Debug with redaction | Should Have |
| NFR-019B-41 | Log timeout triggered | Warning level | Must Have |
| NFR-019B-42 | Log parse failures | Warning with context | Must Have |
| NFR-019B-43 | Log filter applied | Debug level | Should Have |
| NFR-019B-44 | Structured test count metrics | Available | Should Have |
| NFR-019B-45 | Structured duration metrics | Available | Should Have |
| NFR-019B-46 | Correlation ID in logs | UUID per run | Should Have |

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

### Interface & Models (AC-019B-01 to AC-019B-12)

- [ ] AC-019B-01: `ITestRunner` interface exists in `AgenticCoder.Domain.Testing`
- [ ] AC-019B-02: `RunTestsAsync` method signature accepts project path, `TestOptions`, and `CancellationToken`
- [ ] AC-019B-03: `TestRunResult` sealed record is defined with all required properties
- [ ] AC-019B-04: `TestResult` sealed record captures name, status, duration, failure details
- [ ] AC-019B-05: `TestStatus` enum includes Passed, Failed, Skipped values
- [ ] AC-019B-06: `TestOptions` record includes Filter, Timeout, FailFast, CollectCoverage, NoBuild
- [ ] AC-019B-07: `TestFilter` model supports name patterns, class patterns, and tags
- [ ] AC-019B-08: All models are immutable after creation
- [ ] AC-019B-09: All models support JSON serialization
- [ ] AC-019B-10: `TestRunResult.Success` computed property returns `FailedTests == 0`
- [ ] AC-019B-11: `TestRunResult.HasFailures` computed property returns `FailedTests > 0`
- [ ] AC-019B-12: All public members have XML documentation

### .NET Test Execution (AC-019B-13 to AC-019B-28)

- [ ] AC-019B-13: `DotNetTestExecutor` invokes `dotnet test` via `ICommandExecutor`
- [ ] AC-019B-14: Project/solution path is passed as first argument
- [ ] AC-019B-15: TRX logger is enabled via `--logger:trx;LogFileName=<path>`
- [ ] AC-019B-16: TRX output goes to system temp directory with unique filename
- [ ] AC-019B-17: Filter is passed via `--filter` when specified
- [ ] AC-019B-18: `--no-build` is passed when `NoBuild` option is true
- [ ] AC-019B-19: `--configuration` is passed when configuration specified
- [ ] AC-019B-20: Timeout is enforced and process killed if exceeded
- [ ] AC-019B-21: TRX file is parsed after execution completes
- [ ] AC-019B-22: TRX file is cleaned up after parsing
- [ ] AC-019B-23: Build failure is detected and reported with clear message
- [ ] AC-019B-24: Exit code is captured in result
- [ ] AC-019B-25: Raw stdout is captured in result
- [ ] AC-019B-26: Raw stderr is captured in result
- [ ] AC-019B-27: xUnit, NUnit, MSTest frameworks work correctly
- [ ] AC-019B-28: Solution-level test run executes all test projects

### Node.js Test Execution (AC-019B-29 to AC-019B-42)

- [ ] AC-019B-29: `NodeTestExecutor` invokes `npm test` by default
- [ ] AC-019B-30: Custom test script can be specified in options
- [ ] AC-019B-31: Filter pattern is passed to Jest via `--testNamePattern`
- [ ] AC-019B-32: Filter pattern is passed to Mocha via `--grep`
- [ ] AC-019B-33: JSON reporter is enabled for Jest via `--json`
- [ ] AC-019B-34: JSON reporter is enabled for Mocha via `--reporter json`
- [ ] AC-019B-35: JSON output is parsed for structured results
- [ ] AC-019B-36: Timeout is enforced and process killed if exceeded
- [ ] AC-019B-37: `yarn test` works when yarn.lock exists
- [ ] AC-019B-38: `pnpm test` works when pnpm-lock.yaml exists
- [ ] AC-019B-39: Missing test script is detected and reported
- [ ] AC-019B-40: Raw stdout is captured in result
- [ ] AC-019B-41: Raw stderr is captured in result
- [ ] AC-019B-42: Jest, Mocha, Vitest frameworks work correctly

### Framework Detection (AC-019B-43 to AC-019B-52)

- [ ] AC-019B-43: xUnit detected from `xunit` package reference in .csproj
- [ ] AC-019B-44: NUnit detected from `NUnit` package reference in .csproj
- [ ] AC-019B-45: MSTest detected from `MSTest.TestFramework` package reference
- [ ] AC-019B-46: TUnit detected from `TUnit` package reference
- [ ] AC-019B-47: Jest detected from `jest` in devDependencies
- [ ] AC-019B-48: Mocha detected from `mocha` in devDependencies
- [ ] AC-019B-49: Vitest detected from `vitest` in devDependencies
- [ ] AC-019B-50: Detected framework is recorded in result
- [ ] AC-019B-51: Integration with Task-019a detection is used when available
- [ ] AC-019B-52: Explicit framework override takes precedence

### Result Parsing (AC-019B-53 to AC-019B-68)

- [ ] AC-019B-53: `TrxParser` parses TRX XML format correctly
- [ ] AC-019B-54: `JestJsonParser` parses Jest JSON output correctly
- [ ] AC-019B-55: `MochaJsonParser` parses Mocha JSON output correctly
- [ ] AC-019B-56: Total test count matches actual in source file
- [ ] AC-019B-57: Passed test count is accurate
- [ ] AC-019B-58: Failed test count is accurate
- [ ] AC-019B-59: Skipped test count is accurate
- [ ] AC-019B-60: All test names are extracted
- [ ] AC-019B-61: Fully qualified names include namespace.class.method
- [ ] AC-019B-62: Duration per test is captured in milliseconds
- [ ] AC-019B-63: Total duration is calculated correctly
- [ ] AC-019B-64: Failure messages are extracted verbatim
- [ ] AC-019B-65: Stack traces are extracted with line numbers
- [ ] AC-019B-66: Expected/actual values extracted for assertion failures
- [ ] AC-019B-67: Parse failure returns raw output instead of structured
- [ ] AC-019B-68: Malformed files don't crash parser

### Filtering (AC-019B-69 to AC-019B-78)

- [ ] AC-019B-69: `DotNetFilterBuilder` constructs valid filter expressions
- [ ] AC-019B-70: `JestFilterBuilder` constructs valid regex patterns
- [ ] AC-019B-71: `MochaFilterBuilder` constructs valid grep patterns
- [ ] AC-019B-72: Name pattern filter works for all frameworks
- [ ] AC-019B-73: Class/describe filter works for all frameworks
- [ ] AC-019B-74: Tag/trait filter works for .NET frameworks
- [ ] AC-019B-75: Multiple filters combine with AND logic
- [ ] AC-019B-76: Negation filter works (exclude pattern)
- [ ] AC-019B-77: Filter validation rejects invalid syntax
- [ ] AC-019B-78: Empty filter runs all tests

### Execution Control (AC-019B-79 to AC-019B-90)

- [ ] AC-019B-79: Timeout is enforced within ±1 second accuracy
- [ ] AC-019B-80: Process is killed on timeout (not just cancelled)
- [ ] AC-019B-81: `TimedOut` flag is set when timeout occurs
- [ ] AC-019B-82: Partial results are captured before timeout
- [ ] AC-019B-83: Cancellation token is respected
- [ ] AC-019B-84: Clean shutdown occurs on cancellation
- [ ] AC-019B-85: No orphan processes left after timeout/cancellation
- [ ] AC-019B-86: FailFast stops on first failure for .NET
- [ ] AC-019B-87: FailFast stops on first failure for Node.js
- [ ] AC-019B-88: Parallel execution can be disabled
- [ ] AC-019B-89: Consecutive runs don't interfere
- [ ] AC-019B-90: Test run start/end is logged at Info level

### Coverage (AC-019B-91 to AC-019B-96)

- [ ] AC-019B-91: Coverage collection can be enabled via option
- [ ] AC-019B-92: Cobertura format is supported for .NET
- [ ] AC-019B-93: Istanbul format is supported for Node.js
- [ ] AC-019B-94: Coverage file path is stored in result
- [ ] AC-019B-95: Coverage summary is parsed (line %, branch %)
- [ ] AC-019B-96: Coverage is optional and off by default

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Domain/Testing/
├── TestRunResultTests.cs
│   ├── Success_WhenNoFailures_ReturnsTrue()
│   ├── Success_WhenHasFailures_ReturnsFalse()
│   ├── HasFailures_WhenNoFailures_ReturnsFalse()
│   ├── HasFailures_WhenHasFailures_ReturnsTrue()
│   ├── TotalTests_SumsAllCounts()
│   └── Duration_ReturnsCorrectTimespan()
│
├── TestResultTests.cs
│   ├── Status_Passed_IsCorrect()
│   ├── Status_Failed_IsCorrect()
│   ├── Status_Skipped_IsCorrect()
│   ├── FullName_CombinesNamespaceClassMethod()
│   └── ErrorMessage_NullWhenPassed()
│
├── TestFilterTests.cs
│   ├── NamePattern_SetCorrectly()
│   ├── ClassPattern_SetCorrectly()
│   ├── Tags_MultipleAllowed()
│   └── Validate_RejectsInvalidPattern()
│
└── TestOptionsTests.cs
    ├── DefaultTimeout_Is5Minutes()
    ├── FailFast_DefaultsFalse()
    ├── CollectCoverage_DefaultsFalse()
    └── NoBuild_DefaultsFalse()
```

```
Tests/Unit/Infrastructure/Testing/Executors/
├── DotNetTestExecutorTests.cs
│   ├── ExecuteAsync_InvokesDotnetTest()
│   ├── ExecuteAsync_PassesProjectPath()
│   ├── ExecuteAsync_EnablesTrxLogger()
│   ├── ExecuteAsync_WhenFilterProvided_AddsFilterArg()
│   ├── ExecuteAsync_WhenNoBuild_AddsNoBuildFlag()
│   ├── ExecuteAsync_WhenConfiguration_AddsConfigArg()
│   ├── ExecuteAsync_WhenTimeout_EnforcesLimit()
│   ├── ExecuteAsync_WhenTimeoutExceeded_KillsProcess()
│   ├── ExecuteAsync_WhenTimeoutExceeded_SetsTimedOutFlag()
│   ├── ExecuteAsync_WhenTrxExists_ParsesResult()
│   ├── ExecuteAsync_WhenTrxMissing_ReturnsRawOutput()
│   ├── ExecuteAsync_CleanupsTrxFile()
│   ├── ExecuteAsync_WhenBuildFails_ReportsError()
│   ├── ExecuteAsync_CapturesExitCode()
│   ├── ExecuteAsync_CapturesStdout()
│   └── ExecuteAsync_CapturesStderr()
│
├── NodeTestExecutorTests.cs
│   ├── ExecuteAsync_InvokesNpmTest()
│   ├── ExecuteAsync_WhenYarnLock_UsesYarn()
│   ├── ExecuteAsync_WhenPnpmLock_UsesPnpm()
│   ├── ExecuteAsync_WhenCustomScript_UsesScript()
│   ├── ExecuteAsync_WhenJest_EnablesJsonReporter()
│   ├── ExecuteAsync_WhenMocha_EnablesJsonReporter()
│   ├── ExecuteAsync_WhenFilterProvided_PassesToRunner()
│   ├── ExecuteAsync_WhenTimeout_EnforcesLimit()
│   ├── ExecuteAsync_WhenTimeoutExceeded_KillsProcess()
│   ├── ExecuteAsync_WhenNoTestScript_ReportsError()
│   ├── ExecuteAsync_CapturesStdout()
│   └── ExecuteAsync_CapturesStderr()
│
└── FrameworkDetectorTests.cs
    ├── DetectDotNet_WhenXunitPackage_ReturnsXunit()
    ├── DetectDotNet_WhenNUnitPackage_ReturnsNunit()
    ├── DetectDotNet_WhenMSTestPackage_ReturnsMstest()
    ├── DetectDotNet_WhenTUnitPackage_ReturnsTunit()
    ├── DetectDotNet_WhenMultiple_ReturnsFirst()
    ├── DetectNode_WhenJestDep_ReturnsJest()
    ├── DetectNode_WhenMochaDep_ReturnsMocha()
    ├── DetectNode_WhenVitestDep_ReturnsVitest()
    └── DetectNode_WhenNone_ReturnsNull()
```

```
Tests/Unit/Infrastructure/Testing/Parsers/
├── TrxParserTests.cs
│   ├── Parse_WhenValidTrx_ReturnsResult()
│   ├── Parse_ExtractsTotalCount()
│   ├── Parse_ExtractsPassedCount()
│   ├── Parse_ExtractsFailedCount()
│   ├── Parse_ExtractsSkippedCount()
│   ├── Parse_ExtractsTestNames()
│   ├── Parse_ExtractsFullyQualifiedNames()
│   ├── Parse_ExtractsDuration()
│   ├── Parse_ExtractsFailureMessages()
│   ├── Parse_ExtractsStackTraces()
│   ├── Parse_WhenMalformed_ReturnsRawOutput()
│   ├── Parse_WhenEmpty_ReturnsZeroCounts()
│   ├── Parse_WhenLargeFile_HandlesEfficiently()
│   └── Parse_WhenFileLocked_RetriesAndSucceeds()
│
├── JestJsonParserTests.cs
│   ├── Parse_WhenValidJson_ReturnsResult()
│   ├── Parse_ExtractsTotalCount()
│   ├── Parse_ExtractsPassedCount()
│   ├── Parse_ExtractsFailedCount()
│   ├── Parse_ExtractsTestNames()
│   ├── Parse_ExtractsDescribePath()
│   ├── Parse_ExtractsDuration()
│   ├── Parse_ExtractsFailureMessages()
│   ├── Parse_WhenMalformed_ReturnsRawOutput()
│   └── Parse_WhenEmpty_ReturnsZeroCounts()
│
├── MochaJsonParserTests.cs
│   ├── Parse_WhenValidJson_ReturnsResult()
│   ├── Parse_ExtractsTotalCount()
│   ├── Parse_ExtractsPassedCount()
│   ├── Parse_ExtractsFailedCount()
│   ├── Parse_ExtractsTestNames()
│   ├── Parse_ExtractsDuration()
│   ├── Parse_ExtractsFailureMessages()
│   └── Parse_WhenMalformed_ReturnsRawOutput()
│
└── JUnitParserTests.cs
    ├── Parse_WhenValidXml_ReturnsResult()
    ├── Parse_ExtractsTotalCount()
    ├── Parse_ExtractsPassedCount()
    ├── Parse_ExtractsFailureMessages()
    └── Parse_WhenMalformed_ReturnsRawOutput()
```

```
Tests/Unit/Infrastructure/Testing/Filters/
├── DotNetFilterBuilderTests.cs
│   ├── Build_WhenNamePattern_CreatesFilter()
│   ├── Build_WhenClassPattern_CreatesFilter()
│   ├── Build_WhenTagPattern_CreatesTraitFilter()
│   ├── Build_WhenMultiple_CombinesWithAnd()
│   ├── Build_WhenNegation_AddsNotOperator()
│   ├── Build_EscapesSpecialCharacters()
│   ├── Validate_WhenValid_ReturnsTrue()
│   └── Validate_WhenInvalid_ReturnsFalse()
│
├── JestFilterBuilderTests.cs
│   ├── Build_WhenNamePattern_CreatesRegex()
│   ├── Build_WhenDescribePattern_CreatesPath()
│   ├── Build_EscapesRegexCharacters()
│   ├── Validate_WhenValid_ReturnsTrue()
│   └── Validate_WhenInvalid_ReturnsFalse()
│
└── MochaFilterBuilderTests.cs
    ├── Build_WhenNamePattern_CreatesGrep()
    ├── Build_EscapesGrepCharacters()
    └── Validate_WhenValid_ReturnsTrue()
```

```
Tests/Unit/Infrastructure/Testing/
└── TestRunnerServiceTests.cs
    ├── RunTestsAsync_WhenDotNet_UsesDotNetExecutor()
    ├── RunTestsAsync_WhenNode_UsesNodeExecutor()
    ├── RunTestsAsync_WhenUnknown_ThrowsNotSupported()
    ├── RunTestsAsync_PropagatesCancellation()
    ├── RunTestsAsync_LogsStart()
    ├── RunTestsAsync_LogsCompletion()
    ├── RunTestsAsync_LogsFailureSummary()
    └── RunTestsAsync_AggregatesMultipleProjects()
```

### Integration Tests

```
Tests/Integration/Infrastructure/Testing/
├── DotNetTestIntegrationTests.cs
│   ├── Should_Run_XUnit_Tests_In_Sample_Project()
│   ├── Should_Run_NUnit_Tests_In_Sample_Project()
│   ├── Should_Run_MSTest_Tests_In_Sample_Project()
│   ├── Should_Filter_Tests_By_Name()
│   ├── Should_Filter_Tests_By_Class()
│   ├── Should_Parse_TRX_Output_Correctly()
│   ├── Should_Capture_Failed_Test_Details()
│   ├── Should_Enforce_Timeout()
│   ├── Should_Return_Partial_Results_On_Timeout()
│   ├── Should_Handle_Build_Failure()
│   └── Should_Handle_No_Tests_Found()
│
├── NodeTestIntegrationTests.cs
│   ├── Should_Run_Jest_Tests_In_Sample_Project()
│   ├── Should_Run_Mocha_Tests_In_Sample_Project()
│   ├── Should_Run_Vitest_Tests_In_Sample_Project()
│   ├── Should_Filter_Tests_By_Name()
│   ├── Should_Parse_JSON_Output_Correctly()
│   ├── Should_Capture_Failed_Test_Details()
│   ├── Should_Use_Yarn_When_Lock_Exists()
│   ├── Should_Use_Pnpm_When_Lock_Exists()
│   ├── Should_Enforce_Timeout()
│   └── Should_Handle_No_Test_Script()
│
└── CoverageIntegrationTests.cs
    ├── Should_Collect_Coverage_For_DotNet()
    ├── Should_Collect_Coverage_For_Jest()
    ├── Should_Parse_Coverage_Summary()
    └── Should_Store_Coverage_File_Path()
```

### E2E Tests

```
Tests/E2E/CLI/
└── TestCommandE2ETests.cs
    ├── Should_Run_All_Tests_Via_CLI()
    ├── Should_Filter_Tests_Via_CLI()
    ├── Should_Show_Test_Summary()
    ├── Should_Show_Failed_Test_Details()
    ├── Should_Show_Stack_Traces()
    ├── Should_Respect_Timeout_Flag()
    ├── Should_Respect_FailFast_Flag()
    ├── Should_Output_JSON_When_Requested()
    ├── Should_Exit_NonZero_On_Failure()
    └── Should_Exit_Zero_On_Success()
```

### Performance Benchmarks

| Benchmark | Method | Target | Maximum |
|-----------|--------|--------|---------|
| Wrapper overhead | `Benchmark_TestRunnerStartup` | 100ms | 200ms |
| TRX parse 100 tests | `Benchmark_TrxParse_100` | 10ms | 25ms |
| TRX parse 1,000 tests | `Benchmark_TrxParse_1000` | 50ms | 100ms |
| TRX parse 10,000 tests | `Benchmark_TrxParse_10000` | 200ms | 500ms |
| Jest JSON parse 100 tests | `Benchmark_JestParse_100` | 5ms | 15ms |
| Jest JSON parse 1,000 tests | `Benchmark_JestParse_1000` | 25ms | 75ms |
| Filter construction | `Benchmark_FilterBuild` | 1ms | 10ms |
| Result aggregation 10 projects | `Benchmark_Aggregate_10` | 10ms | 50ms |
| Memory 1,000 results | `Benchmark_Memory_1000` | 5MB | 15MB |
| Memory 10,000 results | `Benchmark_Memory_10000` | 35MB | 50MB |

### Coverage Requirements

| Component | Minimum | Target |
|-----------|---------|--------|
| `TestRunResult` | 100% | 100% |
| `TestResult` | 100% | 100% |
| `TestOptions` | 100% | 100% |
| `TestFilter` | 95% | 100% |
| `DotNetTestExecutor` | 85% | 95% |
| `NodeTestExecutor` | 85% | 95% |
| `TrxParser` | 90% | 98% |
| `JestJsonParser` | 90% | 98% |
| `MochaJsonParser` | 90% | 98% |
| `DotNetFilterBuilder` | 95% | 100% |
| `JestFilterBuilder` | 95% | 100% |
| `TestRunnerService` | 85% | 95% |
| **Overall** | **90%** | **95%** |

---

## User Verification Steps

### Scenario 1: Run .NET Tests with xUnit

**Objective:** Verify .NET test execution with xUnit framework

**Setup:**
```bash
mkdir TestScenario1 && cd TestScenario1
dotnet new xunit -n SampleTests
echo 'public class Tests { [Fact] public void PassingTest() { Assert.True(true); } [Fact] public void FailingTest() { Assert.Equal(1, 2); } }' > SampleTests/UnitTest1.cs
dotnet build
```

**Test Command:**
```bash
acode test ./SampleTests
```

**Expected Output:**
```
Detected .NET test framework: xUnit
Running tests for SampleTests.csproj...

Test Run Summary
────────────────
Total:   2
Passed:  1
Failed:  1
Skipped: 0
Duration: 1.2s

Failed Tests:
  ✗ Tests.FailingTest
    Assert.Equal() Failure
    Expected: 1
    Actual:   2
    at Tests.FailingTest() in UnitTest1.cs:line 3
```

---

### Scenario 2: Run Node.js Tests with Jest

**Objective:** Verify Node.js test execution with Jest framework

**Setup:**
```bash
mkdir TestScenario2 && cd TestScenario2
npm init -y
npm install --save-dev jest
echo '{"scripts":{"test":"jest"}}' > package.json
echo 'test("passes", () => expect(1+1).toBe(2)); test("fails", () => expect(1+1).toBe(3));' > sample.test.js
```

**Test Command:**
```bash
acode test .
```

**Expected Output:**
```
Detected Node.js test framework: Jest
Running tests for package.json...

Test Run Summary
────────────────
Total:   2
Passed:  1
Failed:  1
Skipped: 0
Duration: 0.8s

Failed Tests:
  ✗ fails
    expect(received).toBe(expected)
    Expected: 3
    Received: 2
    at Object.<anonymous> (sample.test.js:1:45)
```

---

### Scenario 3: Filter Tests by Name Pattern

**Objective:** Verify test filtering by name pattern works

**Setup:** Use existing .NET or Node.js test project with multiple tests

**Test Command:**
```bash
acode test ./SampleTests --filter "Passing"
```

**Expected Output:**
```
Filter applied: FullyQualifiedName~Passing
Running tests for SampleTests.csproj...

Test Run Summary
────────────────
Total:   1
Passed:  1
Failed:  0
Skipped: 0
Duration: 0.5s

All tests passed!
```

**Verification Checklist:**
- [ ] Only tests matching pattern are executed
- [ ] Test count reflects filtered subset
- [ ] Filter is logged in output

---

### Scenario 4: Handle Test Timeout

**Objective:** Verify timeout enforcement kills hung tests

**Setup:**
```bash
mkdir TestScenario4 && cd TestScenario4
dotnet new xunit -n TimeoutTests
cat > TimeoutTests/UnitTest1.cs << 'EOF'
public class Tests { 
    [Fact] 
    public void HangingTest() { 
        Thread.Sleep(TimeSpan.FromMinutes(10)); 
    } 
}
EOF
dotnet build
```

**Test Command:**
```bash
acode test ./TimeoutTests --timeout 5
```

**Expected Output:**
```
Running tests for TimeoutTests.csproj...

⚠ Test run timed out after 5 seconds

Test Run Summary
────────────────
Total:   1
Passed:  0
Failed:  0
Skipped: 0
Timed Out: Yes
Duration: 5.0s

Warning: Test execution was terminated due to timeout.
Partial results may be incomplete.
```

**Verification Checklist:**
- [ ] Process killed after 5 seconds
- [ ] TimedOut flag is true
- [ ] No orphan processes remain
- [ ] Exit code is non-zero

---

### Scenario 5: Verify JSON Output Format

**Objective:** Verify JSON output for programmatic consumption

**Setup:** Use existing test project

**Test Command:**
```bash
acode test ./SampleTests --json
```

**Expected Output:**
```json
{
  "summary": {
    "total": 2,
    "passed": 1,
    "failed": 1,
    "skipped": 0,
    "durationMs": 1200,
    "timedOut": false
  },
  "framework": "xUnit",
  "projectPath": "./SampleTests",
  "tests": [
    {
      "name": "PassingTest",
      "fullName": "SampleTests.Tests.PassingTest",
      "status": "passed",
      "durationMs": 45
    },
    {
      "name": "FailingTest",
      "fullName": "SampleTests.Tests.FailingTest",
      "status": "failed",
      "durationMs": 12,
      "errorMessage": "Assert.Equal() Failure\nExpected: 1\nActual: 2",
      "stackTrace": "at Tests.FailingTest() in UnitTest1.cs:line 3"
    }
  ]
}
```

**Verification Checklist:**
- [ ] Output is valid JSON
- [ ] All test results included
- [ ] Failure details present
- [ ] Duration in milliseconds

---

### Scenario 6: Fail-Fast Mode

**Objective:** Verify fail-fast stops execution on first failure

**Setup:** Create project with multiple tests where first test fails

**Test Command:**
```bash
acode test ./SampleTests --fail-fast
```

**Expected Output:**
```
Running tests for SampleTests.csproj...
Fail-fast enabled: stopping on first failure

Test Run Summary
────────────────
Total:   1
Passed:  0
Failed:  1
Skipped: 0
Duration: 0.3s

Stopped on first failure (fail-fast mode).
```

**Verification Checklist:**
- [ ] Execution stops after first failure
- [ ] Remaining tests not executed
- [ ] Clear message about fail-fast

---

### Scenario 7: NUnit Framework Detection

**Objective:** Verify NUnit is detected and executed correctly

**Setup:**
```bash
mkdir TestScenario7 && cd TestScenario7
dotnet new nunit -n NUnitTests
dotnet build
```

**Test Command:**
```bash
acode test ./NUnitTests
```

**Expected Output:**
```
Detected .NET test framework: NUnit
Running tests for NUnitTests.csproj...

Test Run Summary
────────────────
Total:   1
Passed:  1
Failed:  0
Skipped: 0
Duration: 0.8s

All tests passed!
```

**Verification Checklist:**
- [ ] Framework detected as NUnit
- [ ] Tests execute successfully
- [ ] Results parsed correctly

---

### Scenario 8: Coverage Collection

**Objective:** Verify test coverage can be collected

**Setup:** Use existing .NET test project

**Test Command:**
```bash
acode test ./SampleTests --coverage
```

**Expected Output:**
```
Running tests for SampleTests.csproj...
Collecting code coverage...

Test Run Summary
────────────────
Total:   2
Passed:  1
Failed:  1
Duration: 2.1s

Coverage Summary
────────────────
Line Coverage:   78.5%
Branch Coverage: 65.0%
Coverage Report: ./TestResults/coverage.cobertura.xml
```

**Verification Checklist:**
- [ ] Coverage is collected
- [ ] Coverage file is created
- [ ] Summary shows percentages
- [ ] File path is accessible

---

### Scenario 9: Solution-Level Test Execution

**Objective:** Verify all test projects in solution are executed

**Setup:**
```bash
mkdir TestScenario9 && cd TestScenario9
dotnet new sln -n MySolution
dotnet new xunit -n Tests.Unit
dotnet new xunit -n Tests.Integration
dotnet sln add Tests.Unit Tests.Integration
dotnet build
```

**Test Command:**
```bash
acode test ./MySolution.sln
```

**Expected Output:**
```
Detected solution with 2 test projects
Running tests for MySolution.sln...

Project: Tests.Unit
  Total: 1, Passed: 1, Failed: 0

Project: Tests.Integration
  Total: 1, Passed: 1, Failed: 0

Aggregated Test Run Summary
────────────────────────────
Total:   2
Passed:  2
Failed:  0
Skipped: 0
Duration: 2.5s

All tests passed!
```

**Verification Checklist:**
- [ ] Both projects executed
- [ ] Results aggregated correctly
- [ ] Total counts sum both projects

---

### Scenario 10: Verbose Mode

**Objective:** Verify verbose mode shows detailed execution information

**Setup:** Use existing test project

**Test Command:**
```bash
acode test ./SampleTests --verbose
```

**Expected Output:**
```
[DEBUG] Detecting test framework...
[DEBUG] Found package reference: xunit v2.6.1
[DEBUG] Framework detected: xUnit
[DEBUG] Building command: dotnet test ./SampleTests --logger:trx;LogFileName=C:\temp\abc123.trx
[DEBUG] Working directory: C:\TestScenario1\SampleTests
[DEBUG] Timeout: 300000ms
[DEBUG] Starting process...
[INFO] Running tests for SampleTests.csproj...
[DEBUG] Process exited with code 1
[DEBUG] TRX file found: C:\temp\abc123.trx
[DEBUG] Parsing TRX output...
[DEBUG] Parsed 2 test results
[DEBUG] Cleaning up TRX file...

Test Run Summary
────────────────
Total:   2
Passed:  1
Failed:  1
Duration: 1.2s
```

**Verification Checklist:**
- [ ] Debug messages visible
- [ ] Command shown
- [ ] File paths visible
- [ ] Parse details shown

---

## Implementation Prompt

You are implementing a unified test runner wrapper for the Agentic Coder Bot that executes tests across .NET and Node.js projects, providing consistent structured results regardless of the underlying test framework.

### File Structure

```
src/AgenticCoder.Domain/
├── Testing/
│   ├── ITestRunner.cs                    # Main interface
│   ├── ITestResultParser.cs              # Parser abstraction
│   ├── ITestFilterBuilder.cs             # Filter builder abstraction
│   ├── TestRunResult.cs                  # Aggregate result model
│   ├── TestResult.cs                     # Individual test result
│   ├── TestStatus.cs                     # Passed/Failed/Skipped enum
│   ├── TestOptions.cs                    # Execution options
│   ├── TestFilter.cs                     # Filter specification
│   ├── TestFramework.cs                  # Framework enum
│   └── CoverageSummary.cs                # Coverage data model

src/AgenticCoder.Application/
├── Testing/
│   └── TestRunnerService.cs              # Orchestration service

src/AgenticCoder.Infrastructure/
├── Testing/
│   ├── Executors/
│   │   ├── DotNetTestExecutor.cs         # .NET test execution
│   │   ├── NodeTestExecutor.cs           # Node.js test execution
│   │   └── FrameworkDetector.cs          # Auto-detect framework
│   ├── Parsers/
│   │   ├── TrxParser.cs                  # .NET TRX format
│   │   ├── JestJsonParser.cs             # Jest JSON output
│   │   ├── MochaJsonParser.cs            # Mocha JSON output
│   │   ├── VitestJsonParser.cs           # Vitest JSON output
│   │   └── JUnitParser.cs                # JUnit XML format
│   └── Filters/
│       ├── DotNetFilterBuilder.cs        # .NET filter expressions
│       ├── JestFilterBuilder.cs          # Jest regex patterns
│       └── MochaFilterBuilder.cs         # Mocha grep patterns

src/AgenticCoder.CLI/
├── Commands/
│   └── TestCommand.cs                    # CLI test command

Tests/Unit/Domain/Testing/
├── TestRunResultTests.cs
├── TestResultTests.cs
├── TestFilterTests.cs
└── TestOptionsTests.cs

Tests/Unit/Infrastructure/Testing/
├── Executors/
│   ├── DotNetTestExecutorTests.cs
│   ├── NodeTestExecutorTests.cs
│   └── FrameworkDetectorTests.cs
├── Parsers/
│   ├── TrxParserTests.cs
│   ├── JestJsonParserTests.cs
│   ├── MochaJsonParserTests.cs
│   └── JUnitParserTests.cs
└── Filters/
    ├── DotNetFilterBuilderTests.cs
    ├── JestFilterBuilderTests.cs
    └── MochaFilterBuilderTests.cs

Tests/Integration/Infrastructure/Testing/
├── DotNetTestIntegrationTests.cs
├── NodeTestIntegrationTests.cs
└── CoverageIntegrationTests.cs

Tests/E2E/CLI/
└── TestCommandE2ETests.cs
```

### Domain Models

```csharp
// src/AgenticCoder.Domain/Testing/ITestRunner.cs
namespace AgenticCoder.Domain.Testing;

/// <summary>
/// Unified interface for executing tests across different frameworks.
/// </summary>
public interface ITestRunner
{
    /// <summary>
    /// Executes tests in the specified project or solution.
    /// </summary>
    /// <param name="projectPath">Path to project, solution, or directory.</param>
    /// <param name="options">Test execution options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregate test run result.</returns>
    Task<TestRunResult> RunTestsAsync(
        string projectPath,
        TestOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/AgenticCoder.Domain/Testing/TestRunResult.cs
namespace AgenticCoder.Domain.Testing;

/// <summary>
/// Aggregate result of a test run containing all test outcomes.
/// </summary>
public sealed record TestRunResult
{
    /// <summary>Total number of tests executed.</summary>
    public required int TotalTests { get; init; }
    
    /// <summary>Number of tests that passed.</summary>
    public required int PassedTests { get; init; }
    
    /// <summary>Number of tests that failed.</summary>
    public required int FailedTests { get; init; }
    
    /// <summary>Number of tests that were skipped.</summary>
    public required int SkippedTests { get; init; }
    
    /// <summary>Total duration of test execution.</summary>
    public required TimeSpan Duration { get; init; }
    
    /// <summary>Individual test results.</summary>
    public required IReadOnlyList<TestResult> Tests { get; init; }
    
    /// <summary>Raw console output from test runner.</summary>
    public string? RawOutput { get; init; }
    
    /// <summary>Raw stderr output from test runner.</summary>
    public string? RawErrorOutput { get; init; }
    
    /// <summary>True if test run was terminated due to timeout.</summary>
    public bool TimedOut { get; init; }
    
    /// <summary>Test framework that was used.</summary>
    public TestFramework? Framework { get; init; }
    
    /// <summary>Project path that was tested.</summary>
    public required string ProjectPath { get; init; }
    
    /// <summary>Process exit code.</summary>
    public int ExitCode { get; init; }
    
    /// <summary>Coverage summary if collection was enabled.</summary>
    public CoverageSummary? Coverage { get; init; }
    
    /// <summary>Start timestamp of test run.</summary>
    public DateTimeOffset StartTime { get; init; }
    
    /// <summary>End timestamp of test run.</summary>
    public DateTimeOffset EndTime { get; init; }
    
    /// <summary>True if all tests passed (no failures).</summary>
    public bool Success => FailedTests == 0 && !TimedOut;
    
    /// <summary>True if any tests failed.</summary>
    public bool HasFailures => FailedTests > 0;
}
```

```csharp
// src/AgenticCoder.Domain/Testing/TestResult.cs
namespace AgenticCoder.Domain.Testing;

/// <summary>
/// Result of an individual test execution.
/// </summary>
public sealed record TestResult
{
    /// <summary>Test method name.</summary>
    public required string Name { get; init; }
    
    /// <summary>Fully qualified name (namespace.class.method).</summary>
    public required string FullName { get; init; }
    
    /// <summary>Class or describe block name.</summary>
    public string? ClassName { get; init; }
    
    /// <summary>Namespace.</summary>
    public string? Namespace { get; init; }
    
    /// <summary>Test execution status.</summary>
    public required TestStatus Status { get; init; }
    
    /// <summary>Duration of this specific test.</summary>
    public required TimeSpan Duration { get; init; }
    
    /// <summary>Error message if test failed.</summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>Stack trace if test failed.</summary>
    public string? StackTrace { get; init; }
    
    /// <summary>Expected value for assertion failures.</summary>
    public string? Expected { get; init; }
    
    /// <summary>Actual value for assertion failures.</summary>
    public string? Actual { get; init; }
    
    /// <summary>Source file path.</summary>
    public string? FilePath { get; init; }
    
    /// <summary>Source line number.</summary>
    public int? LineNumber { get; init; }
    
    /// <summary>Test traits or tags.</summary>
    public IReadOnlyDictionary<string, string>? Traits { get; init; }
}

/// <summary>
/// Test execution status.
/// </summary>
public enum TestStatus
{
    Passed,
    Failed,
    Skipped
}
```

```csharp
// src/AgenticCoder.Domain/Testing/TestOptions.cs
namespace AgenticCoder.Domain.Testing;

/// <summary>
/// Options for test execution.
/// </summary>
public sealed record TestOptions
{
    /// <summary>Filter pattern to select specific tests.</summary>
    public TestFilter? Filter { get; init; }
    
    /// <summary>Maximum time to wait for tests to complete.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
    
    /// <summary>Stop execution on first failure.</summary>
    public bool FailFast { get; init; }
    
    /// <summary>Collect code coverage.</summary>
    public bool CollectCoverage { get; init; }
    
    /// <summary>Skip build before running tests.</summary>
    public bool NoBuild { get; init; }
    
    /// <summary>Build configuration (Debug/Release).</summary>
    public string? Configuration { get; init; }
    
    /// <summary>Enable verbose output.</summary>
    public bool Verbose { get; init; }
    
    /// <summary>Explicit framework override.</summary>
    public TestFramework? Framework { get; init; }
    
    /// <summary>Custom test script for Node.js.</summary>
    public string? TestScript { get; init; }
}
```

```csharp
// src/AgenticCoder.Domain/Testing/TestFilter.cs
namespace AgenticCoder.Domain.Testing;

/// <summary>
/// Specification for filtering tests to run.
/// </summary>
public sealed record TestFilter
{
    /// <summary>Pattern to match test names.</summary>
    public string? NamePattern { get; init; }
    
    /// <summary>Pattern to match class/describe names.</summary>
    public string? ClassPattern { get; init; }
    
    /// <summary>Pattern to match namespace.</summary>
    public string? NamespacePattern { get; init; }
    
    /// <summary>Tags/traits to include.</summary>
    public IReadOnlyList<string>? Tags { get; init; }
    
    /// <summary>Patterns to exclude.</summary>
    public IReadOnlyList<string>? Exclude { get; init; }
    
    /// <summary>True if any filter criteria is set.</summary>
    public bool HasCriteria => 
        !string.IsNullOrEmpty(NamePattern) ||
        !string.IsNullOrEmpty(ClassPattern) ||
        !string.IsNullOrEmpty(NamespacePattern) ||
        (Tags?.Count > 0) ||
        (Exclude?.Count > 0);
}
```

```csharp
// src/AgenticCoder.Domain/Testing/TestFramework.cs
namespace AgenticCoder.Domain.Testing;

/// <summary>
/// Supported test frameworks.
/// </summary>
public enum TestFramework
{
    // .NET Frameworks
    XUnit,
    NUnit,
    MSTest,
    TUnit,
    
    // Node.js Frameworks
    Jest,
    Mocha,
    Vitest
}
```

### Infrastructure Implementation

```csharp
// src/AgenticCoder.Infrastructure/Testing/Executors/DotNetTestExecutor.cs
namespace AgenticCoder.Infrastructure.Testing.Executors;

public sealed class DotNetTestExecutor
{
    private readonly ICommandExecutor _commandExecutor;
    private readonly ITestResultParser _trxParser;
    private readonly ILogger<DotNetTestExecutor> _logger;
    
    public DotNetTestExecutor(
        ICommandExecutor commandExecutor,
        TrxParser trxParser,
        ILogger<DotNetTestExecutor> logger)
    {
        _commandExecutor = commandExecutor;
        _trxParser = trxParser;
        _logger = logger;
    }
    
    public async Task<TestRunResult> ExecuteAsync(
        string projectPath,
        TestOptions options,
        TestFramework framework,
        CancellationToken cancellationToken)
    {
        var trxPath = Path.Combine(
            Path.GetTempPath(), 
            $"acode-test-{Guid.NewGuid():N}.trx");
        
        try
        {
            var startTime = DateTimeOffset.UtcNow;
            
            var command = BuildCommand(projectPath, options, trxPath);
            _logger.LogDebug("Executing: {Command}", command);
            
            var result = await _commandExecutor.ExecuteAsync(
                command,
                workingDirectory: Path.GetDirectoryName(projectPath),
                timeout: options.Timeout,
                cancellationToken: cancellationToken);
            
            var endTime = DateTimeOffset.UtcNow;
            
            if (File.Exists(trxPath))
            {
                _logger.LogDebug("Parsing TRX file: {Path}", trxPath);
                var parsed = await _trxParser.ParseAsync(trxPath, cancellationToken);
                
                return parsed with
                {
                    ProjectPath = projectPath,
                    Framework = framework,
                    RawOutput = result.StandardOutput,
                    RawErrorOutput = result.StandardError,
                    ExitCode = result.ExitCode,
                    StartTime = startTime,
                    EndTime = endTime,
                    TimedOut = result.TimedOut
                };
            }
            
            _logger.LogWarning("TRX file not found, returning raw output");
            return CreateFallbackResult(projectPath, framework, result, startTime, endTime);
        }
        finally
        {
            if (File.Exists(trxPath))
            {
                try { File.Delete(trxPath); }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to cleanup TRX file");
                }
            }
        }
    }
    
    private CommandSpec BuildCommand(string projectPath, TestOptions options, string trxPath)
    {
        var args = new List<string> { "test", projectPath };
        
        // TRX logger
        args.Add($"--logger:trx;LogFileName={trxPath}");
        
        // Filter
        if (options.Filter?.HasCriteria == true)
        {
            var filterExpr = BuildFilterExpression(options.Filter);
            args.Add($"--filter:{filterExpr}");
        }
        
        // Options
        if (options.NoBuild)
            args.Add("--no-build");
            
        if (!string.IsNullOrEmpty(options.Configuration))
            args.Add($"--configuration:{options.Configuration}");
            
        if (options.CollectCoverage)
            args.Add("--collect:\"Code Coverage\"");
            
        if (options.Verbose)
            args.Add("--verbosity:detailed");
        
        return new CommandSpec("dotnet", args);
    }
    
    private string BuildFilterExpression(TestFilter filter)
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrEmpty(filter.NamePattern))
            parts.Add($"FullyQualifiedName~{EscapeFilter(filter.NamePattern)}");
            
        if (!string.IsNullOrEmpty(filter.ClassPattern))
            parts.Add($"ClassName~{EscapeFilter(filter.ClassPattern)}");
            
        if (filter.Tags?.Count > 0)
        {
            foreach (var tag in filter.Tags)
                parts.Add($"Category={EscapeFilter(tag)}");
        }
        
        return string.Join("&", parts);
    }
    
    private string EscapeFilter(string value) =>
        value.Replace("\"", "\\\"").Replace("'", "\\'");
}
```

```csharp
// src/AgenticCoder.Infrastructure/Testing/Parsers/TrxParser.cs
namespace AgenticCoder.Infrastructure.Testing.Parsers;

public sealed class TrxParser : ITestResultParser
{
    private readonly ILogger<TrxParser> _logger;
    
    public TrxParser(ILogger<TrxParser> logger)
    {
        _logger = logger;
    }
    
    public async Task<TestRunResult> ParseAsync(
        string filePath, 
        CancellationToken cancellationToken)
    {
        try
        {
            var doc = await LoadTrxWithRetryAsync(filePath, cancellationToken);
            var ns = XNamespace.Get("http://microsoft.com/schemas/VisualStudio/TeamTest/2010");
            
            var results = doc.Descendants(ns + "UnitTestResult")
                .Select(ParseTestResult)
                .ToList();
            
            var counters = doc.Descendants(ns + "Counters").FirstOrDefault();
            
            return new TestRunResult
            {
                TotalTests = (int?)counters?.Attribute("total") ?? results.Count,
                PassedTests = (int?)counters?.Attribute("passed") ?? results.Count(r => r.Status == TestStatus.Passed),
                FailedTests = (int?)counters?.Attribute("failed") ?? results.Count(r => r.Status == TestStatus.Failed),
                SkippedTests = (int?)counters?.Attribute("notExecuted") ?? results.Count(r => r.Status == TestStatus.Skipped),
                Duration = ParseDuration(doc, ns),
                Tests = results,
                ProjectPath = string.Empty // Filled by caller
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse TRX file: {Path}", filePath);
            throw;
        }
    }
    
    private async Task<XDocument> LoadTrxWithRetryAsync(
        string filePath, 
        CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        const int delayMs = 100;
        
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await using var stream = new FileStream(
                    filePath, 
                    FileMode.Open, 
                    FileAccess.Read, 
                    FileShare.Read);
                return await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
            }
            catch (IOException) when (i < maxRetries - 1)
            {
                await Task.Delay(delayMs, cancellationToken);
            }
        }
        
        throw new IOException($"Could not access TRX file after {maxRetries} attempts");
    }
    
    private TestResult ParseTestResult(XElement element)
    {
        var ns = element.Name.Namespace;
        var outcome = element.Attribute("outcome")?.Value ?? "Unknown";
        
        var errorInfo = element.Element(ns + "Output")?.Element(ns + "ErrorInfo");
        
        return new TestResult
        {
            Name = element.Attribute("testName")?.Value ?? "Unknown",
            FullName = ParseFullName(element),
            Status = ParseStatus(outcome),
            Duration = TimeSpan.Parse(element.Attribute("duration")?.Value ?? "00:00:00"),
            ErrorMessage = errorInfo?.Element(ns + "Message")?.Value,
            StackTrace = errorInfo?.Element(ns + "StackTrace")?.Value
        };
    }
    
    private TestStatus ParseStatus(string outcome) => outcome switch
    {
        "Passed" => TestStatus.Passed,
        "Failed" => TestStatus.Failed,
        "NotExecuted" => TestStatus.Skipped,
        _ => TestStatus.Skipped
    };
}
```

### CLI Command

```csharp
// src/AgenticCoder.CLI/Commands/TestCommand.cs
namespace AgenticCoder.CLI.Commands;

[Command("test", Description = "Run tests in project or solution")]
public sealed class TestCommand : ICommand
{
    private readonly ITestRunner _testRunner;
    private readonly IConsole _console;
    
    [Argument(0, Description = "Path to project, solution, or directory")]
    public string? Path { get; set; }
    
    [Option("--filter", "-f", Description = "Filter tests by name pattern")]
    public string? Filter { get; set; }
    
    [Option("--timeout", "-t", Description = "Timeout in seconds")]
    public int? TimeoutSeconds { get; set; }
    
    [Option("--fail-fast", Description = "Stop on first failure")]
    public bool FailFast { get; set; }
    
    [Option("--coverage", Description = "Collect code coverage")]
    public bool Coverage { get; set; }
    
    [Option("--no-build", Description = "Skip build")]
    public bool NoBuild { get; set; }
    
    [Option("--json", Description = "Output as JSON")]
    public bool Json { get; set; }
    
    [Option("--verbose", "-v", Description = "Verbose output")]
    public bool Verbose { get; set; }
    
    public async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        var projectPath = Path ?? Directory.GetCurrentDirectory();
        
        var options = new TestOptions
        {
            Filter = string.IsNullOrEmpty(Filter) ? null : new TestFilter { NamePattern = Filter },
            Timeout = TimeoutSeconds.HasValue 
                ? TimeSpan.FromSeconds(TimeoutSeconds.Value) 
                : TimeSpan.FromMinutes(5),
            FailFast = FailFast,
            CollectCoverage = Coverage,
            NoBuild = NoBuild,
            Verbose = Verbose
        };
        
        var result = await _testRunner.RunTestsAsync(projectPath, options, cancellationToken);
        
        if (Json)
        {
            WriteJsonOutput(result);
        }
        else
        {
            WriteHumanOutput(result);
        }
        
        Environment.ExitCode = result.Success ? 0 : 1;
    }
    
    private void WriteHumanOutput(TestRunResult result)
    {
        _console.WriteLine($"\nTest Run Summary");
        _console.WriteLine(new string('─', 16));
        _console.WriteLine($"Total:   {result.TotalTests}");
        _console.WriteLine($"Passed:  {result.PassedTests}");
        _console.WriteLine($"Failed:  {result.FailedTests}");
        _console.WriteLine($"Skipped: {result.SkippedTests}");
        _console.WriteLine($"Duration: {result.Duration.TotalSeconds:F1}s");
        
        if (result.TimedOut)
        {
            _console.WriteLine("\n⚠ Test run timed out");
        }
        
        if (result.HasFailures)
        {
            _console.WriteLine("\nFailed Tests:");
            foreach (var test in result.Tests.Where(t => t.Status == TestStatus.Failed))
            {
                _console.WriteLine($"  ✗ {test.FullName}");
                if (!string.IsNullOrEmpty(test.ErrorMessage))
                    _console.WriteLine($"    {test.ErrorMessage}");
                if (!string.IsNullOrEmpty(test.StackTrace))
                    _console.WriteLine($"    {test.StackTrace.Split('\n').FirstOrDefault()}");
            }
        }
        else if (result.Success)
        {
            _console.WriteLine("\nAll tests passed!");
        }
    }
}
```

### Error Codes

| Code | Meaning | Resolution |
|------|---------|------------|
| ACODE-TST-001 | Test execution failed | Check test output for details |
| ACODE-TST-002 | TRX parse failed | Verify TRX file format |
| ACODE-TST-003 | Test timeout exceeded | Increase timeout or fix slow tests |
| ACODE-TST-004 | Framework not detected | Specify framework explicitly |
| ACODE-TST-005 | Build failed before tests | Fix compilation errors |
| ACODE-TST-006 | No tests found | Check test discovery and filters |
| ACODE-TST-007 | Invalid filter syntax | Check filter expression format |
| ACODE-TST-008 | Coverage collection failed | Check coverage tooling |

### Implementation Checklist

1. [ ] Create domain models (`TestRunResult`, `TestResult`, `TestOptions`, `TestFilter`)
2. [ ] Create `ITestRunner` interface in Domain layer
3. [ ] Create `ITestResultParser` interface in Domain layer
4. [ ] Create `ITestFilterBuilder` interface in Domain layer
5. [ ] Implement `DotNetTestExecutor` in Infrastructure
6. [ ] Implement `TrxParser` with retry logic
7. [ ] Implement `DotNetFilterBuilder`
8. [ ] Implement `NodeTestExecutor` in Infrastructure
9. [ ] Implement `JestJsonParser`
10. [ ] Implement `MochaJsonParser`
11. [ ] Implement `JestFilterBuilder`
12. [ ] Implement `FrameworkDetector`
13. [ ] Implement `TestRunnerService` in Application layer
14. [ ] Implement `TestCommand` in CLI
15. [ ] Write unit tests for all domain models (100% coverage)
16. [ ] Write unit tests for all parsers (90%+ coverage)
17. [ ] Write unit tests for all executors (85%+ coverage)
18. [ ] Write unit tests for filter builders (95%+ coverage)
19. [ ] Write integration tests for .NET execution
20. [ ] Write integration tests for Node.js execution
21. [ ] Write E2E tests for CLI command
22. [ ] Add XML documentation to all public members
23. [ ] Register services in DI container
24. [ ] Add verbose logging at appropriate levels

### Rollout Plan

1. **Phase 1 - Domain Models:** Create all domain models and interfaces (1 day)
2. **Phase 2 - .NET Executor:** Implement DotNetTestExecutor and TrxParser (1 day)
3. **Phase 3 - .NET Filters:** Implement DotNetFilterBuilder (0.5 day)
4. **Phase 4 - Node.js Executor:** Implement NodeTestExecutor and parsers (1 day)
5. **Phase 5 - Node.js Filters:** Implement Jest/Mocha filter builders (0.5 day)
6. **Phase 6 - Framework Detection:** Implement FrameworkDetector (0.5 day)
7. **Phase 7 - CLI Integration:** Implement TestCommand (0.5 day)
8. **Phase 8 - Testing:** Complete unit, integration, E2E tests (2 days)
9. **Phase 9 - Documentation:** Add XML docs and user manual (0.5 day)

---

**End of Task 019.b Specification**