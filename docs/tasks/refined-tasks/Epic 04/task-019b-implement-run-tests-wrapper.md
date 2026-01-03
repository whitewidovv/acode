# Task 019.b: Implement run_tests Wrapper

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 019 (Language Runners), Task 019.a (Layout Detection), Task 018 (Command Runner)  

---

## Description

Task 019.b implements a unified test execution wrapper. The agent MUST run tests to verify code changes. This wrapper provides a consistent interface across test frameworks.

Test execution is central to agent verification. The agent writes code. It MUST run tests. Test results guide corrections. Without reliable test execution, the agent cannot verify its work.

The wrapper abstracts test framework differences. .NET uses xUnit, NUnit, or MSTest. Node.js uses Jest, Mocha, or Vitest. The agent MUST NOT need to know framework specifics.

Test result parsing MUST be standardized. Each framework outputs differently. The wrapper MUST parse output into a common format. Pass, fail, skip counts. Failure messages and locations.

Selective test execution MUST be supported. Running all tests is slow. The agent MUST run specific tests. Filter by name, class, namespace. Filter by file path.

Test output MUST be captured completely. Stack traces are essential for debugging. Error messages guide fixes. The wrapper MUST NOT truncate critical information.

Timeout handling MUST prevent hung tests. Tests can hang indefinitely. The wrapper MUST enforce timeouts. Hung tests MUST be killed.

The wrapper integrates with Task 018 command execution. Commands are constructed and passed to the executor. Output is captured via Task 018.a.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Test Wrapper | Unified test execution interface |
| Test Framework | xUnit, NUnit, Jest, etc. |
| Test Result | Pass/fail/skip outcome |
| Test Case | Individual test method |
| Test Suite | Collection of tests |
| Filter | Test selection criteria |
| TRX | .NET test result format |
| JUnit XML | Standard test result format |
| Stack Trace | Error location chain |
| Assertion | Test verification statement |

---

## Out of Scope

- **Test code generation** - Separate concern
- **Coverage collection** - Future version
- **Parallel test distribution** - Single-process only
- **Test debugging** - No debugger attachment
- **Watch mode** - Single execution only
- **Visual test results** - Data only

---

## Functional Requirements

### Wrapper Interface

- FR-001: Define ITestRunner interface with RunTestsAsync method
- FR-002: Accept TestRunOptions parameter
- FR-003: Return TestRunResult with structured data
- FR-004: Support CancellationToken for abort

### Test Run Options

- FR-005: Filter by test name pattern (MUST support wildcards)
- FR-006: Filter by namespace/class
- FR-007: Filter by file path
- FR-008: Filter by test category/trait
- FR-009: Timeout per test (MUST default to 30 seconds)
- FR-010: Total run timeout (MUST default to 300 seconds)
- FR-011: Verbosity level (minimal, normal, detailed)

### Test Run Result

- FR-012: Total test count (MUST be accurate)
- FR-013: Passed count
- FR-014: Failed count
- FR-015: Skipped count
- FR-016: Duration in milliseconds
- FR-017: List of individual test results
- FR-018: Overall success flag

### Individual Test Result

- FR-019: Test name (fully qualified)
- FR-020: Test outcome (Passed, Failed, Skipped)
- FR-021: Duration in milliseconds
- FR-022: Failure message (if failed)
- FR-023: Stack trace (if failed)
- FR-024: Source file path (if available)
- FR-025: Source line number (if available)

### .NET Test Execution

- FR-026: Execute via `dotnet test`
- FR-027: Use `--filter` for test selection
- FR-028: Use `--logger trx` for structured output
- FR-029: Parse TRX file for results
- FR-030: Support xUnit, NUnit, MSTest frameworks
- FR-031: Handle build failures gracefully

### Node.js Test Execution

- FR-032: Detect test script in package.json
- FR-033: Support Jest with `--json` output
- FR-034: Support Mocha with JSON reporter
- FR-035: Support Vitest with JSON reporter
- FR-036: Parse JSON output for results
- FR-037: Handle npm script failures

### Error Handling

- FR-038: Map framework errors to standard format
- FR-039: Capture partial results on timeout
- FR-040: Report build failures distinctly from test failures
- FR-041: Handle missing test projects gracefully

---

## Non-Functional Requirements

### Performance

- NFR-001: Wrapper overhead MUST be < 100ms
- NFR-002: Result parsing MUST complete < 50ms
- NFR-003: Large result sets (1000+ tests) MUST parse < 200ms

### Reliability

- NFR-004: MUST NOT crash on malformed test output
- NFR-005: MUST return partial results on timeout
- NFR-006: MUST handle process termination gracefully

### Accuracy

- NFR-007: Test counts MUST match framework output exactly
- NFR-008: Failure messages MUST NOT be truncated
- NFR-009: Stack traces MUST include all frames

---

## User Manual Documentation

### Configuration

```yaml
# .agent/config.yml
testing:
  default_timeout_seconds: 300
  per_test_timeout_seconds: 30
  verbosity: normal
  
  dotnet:
    configuration: Debug
    no_build: false
    
  node:
    prefer_script: test
```

### CLI Commands

```bash
# Run all tests
acode test

# Run filtered tests
acode test --filter "UserService*"

# Run specific test file
acode test --file tests/UserServiceTests.cs

# Run with timeout
acode test --timeout 60
```

### Test Result Output

```
Test Run Summary
────────────────
Total:   47
Passed:  45
Failed:   1
Skipped:  1
Duration: 3.2s

Failed Tests:
  ✗ UserServiceTests.GetById_WhenNotFound_ReturnsNull
    Expected: null
    Actual: threw NotFoundException
    at UserServiceTests.cs:45
```

---

## Acceptance Criteria

- [ ] AC-001: ITestRunner interface MUST be defined
- [ ] AC-002: .NET tests MUST execute via dotnet test
- [ ] AC-003: Node.js tests MUST execute via npm test
- [ ] AC-004: Test filtering by name MUST work
- [ ] AC-005: TRX parsing MUST extract all results
- [ ] AC-006: Jest JSON parsing MUST extract all results
- [ ] AC-007: Failure messages MUST include stack traces
- [ ] AC-008: Timeout MUST kill hung tests
- [ ] AC-009: Partial results MUST be returned on timeout
- [ ] AC-010: CLI `acode test` command MUST work

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Testing/
├── TestRunnerTests.cs
│   ├── Should_Parse_Trx_Results()
│   ├── Should_Parse_Jest_Json()
│   └── Should_Handle_Timeout()
```

### Integration Tests

```
Tests/Integration/Testing/
├── DotNetTestRunnerIntegrationTests.cs
├── NodeTestRunnerIntegrationTests.cs
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Parse 100 results | 10ms | 20ms |
| Wrapper overhead | 50ms | 100ms |

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/Testing/
├── ITestRunner.cs
├── TestRunResult.cs
├── TestResult.cs
└── TestOutcome.cs

src/AgenticCoder.Infrastructure/Testing/
├── DotNetTestRunner.cs
├── NodeTestRunner.cs
├── TrxParser.cs
└── JestJsonParser.cs
```

### ITestRunner Interface

```csharp
namespace AgenticCoder.Domain.Testing;

public interface ITestRunner
{
    Task<TestRunResult> RunTestsAsync(
        string projectPath,
        TestRunOptions? options = null,
        CancellationToken ct = default);
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-TEST-001 | Test execution failed |
| ACODE-TEST-002 | Build failed before tests |
| ACODE-TEST-003 | Timeout exceeded |
| ACODE-TEST-004 | No tests found |

---

**End of Task 019.b Specification**