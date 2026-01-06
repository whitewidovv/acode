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

## Use Cases

### Use Case 1: Elena the Backend Developer — Validating API Changes with Rapid Feedback

**Persona:** Elena is a backend developer working on a .NET microservices project using xUnit for testing. She's implementing a new authentication endpoint and needs to verify that her changes don't break existing functionality while ensuring the new endpoint works correctly.

**Before (Manual Test Execution):**
- Elena modifies `/src/AuthService/Controllers/AuthController.cs` to add OAuth2 support
- She opens terminal, navigates to `/tests/AuthService.Tests`
- Runs `dotnet test --filter "Category=Auth"` manually
- Waits 12 seconds for test execution
- Parses visual output: "Test Run Successful. Total: 47, Passed: 46, Failed: 1"
- Searches scrollback to find which test failed: `ValidateTokenExpiry_ExpiredToken_ReturnsFalse`
- Opens TRX file manually to see failure details: "Expected: false, Actual: true"
- Fixes the bug, runs tests again manually
- Repeats this 5 times during development
- **Total time spent on test cycle**: 8 minutes (5 iterations × 1.6 minutes)
- **Cognitive load**: High (context switching between code and terminal, manual parsing)
- **Error-prone**: Must remember exact filter syntax, easy to miss failures in scrollback

**After (Unified Test Runner Wrapper):**
- Elena modifies the same file using Acode
- Agent detects code change, automatically triggers: `acode test --filter "Category==Auth" --project AuthService.Tests`
- Test runner:
  1. Detects xUnit framework from project references
  2. Invokes `dotnet test --filter "Category=Auth" --logger trx --no-build`
  3. Parses TRX output in realtime
  4. Returns structured `TestRunResult` to agent within 1.2 seconds
- Agent receives:
  ```json
  {
    "totalTests": 47,
    "passedTests": 46,
    "failedTests": 1,
    "duration": 1.2,
    "results": [
      {
        "testName": "ValidateTokenExpiry_ExpiredToken_ReturnsFalse",
        "outcome": "Failed",
        "errorMessage": "Expected: false, Actual: true",
        "stackTrace": "at AuthService.Tests.TokenValidatorTests.ValidateTokenExpiry_ExpiredToken_ReturnsFalse() in /tests/AuthService.Tests/TokenValidatorTests.cs:line 42"
      }
    ]
  }
  ```
- Agent analyzes failure, identifies bug in `/src/AuthService/TokenValidator.cs:67`, proposes fix
- Auto-runs tests again after fix, confirms all 47 pass
- **Total time spent**: 2.4 seconds (2 iterations × 1.2 seconds)
- **Cognitive load**: Zero (fully automated)
- **Accuracy**: 100% (structured parsing, no manual interpretation)

**Improvement Metrics:**
- **Time reduction**: 8 minutes → 2.4 seconds = **99.5% faster**
- **Developer productivity**: Elena saves ~45 minutes/day on test cycles (average 6 API changes/day)
- **ROI**: $112.50/day × 20 working days/month = **$2,250/month per developer**
- **Quality improvement**: Zero missed test failures (previously ~3% of failures missed in scrollback)

---

### Use Case 2: Marcus the AI Agent — Test-Driven Refactoring in Polyglot Codebase

**Persona:** Marcus is Acode's autonomous coding agent tasked with refactoring a monorepo containing both a .NET backend (xUnit tests) and a React frontend (Vitest tests). The refactoring involves renaming a shared data model used across both projects.

**Before (Without Unified Test Wrapper):**
- Marcus receives task: "Rename `UserProfile` to `UserAccount` across codebase"
- Attempts to run .NET tests: must know to use `dotnet test`, parse TRX format
- Attempts to run Node.js tests: must know to use `npm test`, parse Jest JSON reporter
- Framework detection fails 40% of the time (hardcoded paths, wrong assumptions)
- Test result parsing errors occur in 15% of runs (TRX schema variations, Jest version differences)
- Agent cannot determine if refactoring broke tests without external CI pipeline
- **Success rate**: 60% (40% failure rate due to framework detection/parsing errors)
- **Time to complete**: 45 minutes (including retries and manual human intervention)

**After (Unified Test Runner Wrapper):**
- Marcus receives the same task
- Step 1: Invokes `acode detect` to find all projects (Task 019a)
  - Discovers: `/backend/UserService.csproj` (xUnit), `/frontend/package.json` (Vitest)
- Step 2: Performs refactoring across 47 files
- Step 3: Validates backend changes:
  ```bash
  acode test --project UserService.csproj --timeout 60
  ```
  - Test runner auto-detects xUnit, invokes `dotnet test`, parses TRX
  - Returns: `{ "totalTests": 238, "passedTests": 235, "failedTests": 3, "duration": 8.4 }`
- Step 4: Analyzes 3 failed tests, identifies missed rename in `UserService/Mappers/ProfileMapper.cs`
- Step 5: Fixes mapper, re-runs tests, confirms all 238 pass
- Step 6: Validates frontend changes:
  ```bash
  acode test --project frontend --filter "UserAccount"
  ```
  - Test runner auto-detects Vitest, invokes `npm test -- --reporter=json`, parses JSON output
  - Returns: `{ "totalTests": 64, "passedTests": 64, "failedTests": 0, "duration": 3.2 }`
- Step 7: Commits refactoring with confidence (all 302 tests passing)
- **Success rate**: 98% (2% failure rate due to legitimate test failures, not tooling issues)
- **Time to complete**: 4.5 minutes (fully autonomous, no human intervention)
- **Confidence**: 100% (structured test results provide proof of correctness)

**Improvement Metrics:**
- **Reliability**: 60% → 98% success rate = **63% improvement**
- **Speed**: 45 minutes → 4.5 minutes = **90% time reduction**
- **Autonomy**: 0% → 100% (eliminated need for human intervention to run/interpret tests)
- **Framework coverage**: 2 frameworks (xUnit, Vitest) tested with single unified API
- **Agent capability unlock**: Enables autonomous refactoring tasks previously requiring human oversight

---

### Use Case 3: Jordan the DevOps Engineer — CI/CD Pipeline Integration with Unified Test Output

**Persona:** Jordan is a DevOps engineer responsible for maintaining CI/CD pipelines for a polyglot microservices architecture. The team uses Jenkins, GitHub Actions, and Azure Pipelines across different projects. Jordan needs consistent test reporting to aggregate results in a dashboard.

**Before (Fragmented Test Execution):**
- Pipeline for .NET service:
  ```yaml
  - run: dotnet test --logger trx
  - run: parse-trx.sh results.trx  # Custom parsing script
  - run: upload-results.sh --format custom  # Custom format
  ```
- Pipeline for Node.js service:
  ```yaml
  - run: npm test -- --reporter=json > results.json
  - run: parse-jest.js results.json  # Different custom parser
  - run: upload-results.sh --format custom  # Same uploader, different logic
  ```
- **Challenges**:
  1. **Maintenance burden**: 8 different parsing scripts across teams (Python, Bash, Node.js, PowerShell)
  2. **Inconsistent formats**: Dashboard shows .NET results as "Total/Passed/Failed", Node results as "Suites/Tests/Failures"
  3. **Framework lock-in**: Switching from Jest to Vitest requires rewriting parser
  4. **Timeout handling**: No unified timeout enforcement, some pipelines hang for hours
  5. **Coverage aggregation**: Manual correlation of coverage reports from different tools
- **Time to add new service**: 4 hours (write parser, test, integrate with dashboard)
- **Ongoing maintenance**: ~6 hours/month fixing parser bugs and format changes
- **Dashboard accuracy**: 85% (15% of failures not captured due to parsing errors)

**After (Unified Test Wrapper in CI/CD):**
- Pipeline for .NET service:
  ```yaml
  - run: acode test --format json --coverage --timeout 300 > test-results.json
  - run: upload-results.sh test-results.json  # Standard format
  ```
- Pipeline for Node.js service:
  ```yaml
  - run: acode test --format json --coverage --timeout 300 > test-results.json
  - run: upload-results.sh test-results.json  # Identical command!
  ```
- **Benefits**:
  1. **Single uploader**: One `upload-results.sh` script handles all projects
  2. **Consistent format**: All results in unified JSON schema:
     ```json
     {
       "totalTests": 156,
       "passedTests": 154,
       "failedTests": 2,
       "skippedTests": 0,
       "duration": 12.4,
       "coverage": {
         "lineRate": 0.847,
         "branchRate": 0.792
       },
       "results": [...]
     }
     ```
  3. **Framework agnostic**: Switching frameworks requires zero pipeline changes
  4. **Built-in timeout**: `--timeout 300` ensures tests never hang beyond 5 minutes
  5. **Unified coverage**: Coverage data in same schema regardless of tool (coverlet vs nyc)
- **Time to add new service**: 10 minutes (copy existing pipeline, change project name)
- **Ongoing maintenance**: <30 minutes/month (only `acode test` tool maintained centrally)
- **Dashboard accuracy**: 99.5% (structured parsing eliminates interpretation errors)

**Improvement Metrics:**
- **Onboarding time**: 4 hours → 10 minutes = **96% reduction**
- **Maintenance cost**: $600/month → $50/month = **92% cost reduction** (at $100/hour)
- **Dashboard accuracy**: 85% → 99.5% = **14.5 percentage point improvement**
- **Framework flexibility**: Zero-cost framework migration (previously $2,000+ per service)
- **Pipeline consistency**: 100% of services use identical test execution pattern
- **Operational risk**: Eliminated (timeouts prevent runaway pipelines)

**ROI Calculation:**
- Setup time saved: 3.9 hours/service × 12 services/year = 46.8 hours = **$4,680 savings/year**
- Maintenance savings: $550/month × 12 months = **$6,600 savings/year**
- Dashboard accuracy improvement value: Prevents ~2 production incidents/year from missed test failures = **$10,000 savings/year** (estimated $5K/incident)
- **Total ROI**: $21,280/year for a 10-service organization

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

## Assumptions

### Technical Assumptions

1. **SDK Availability:** .NET SDK and Node.js runtime are installed and available on the system PATH
2. **Project Built:** Test projects are already compiled and up-to-date, or `--no-build` flag is not used (for .NET)
3. **Dependencies Installed:** All test framework packages and dependencies are restored via `dotnet restore` or `npm install`
4. **Test Framework Installed:** Test projects reference at least one recognized test framework (xUnit/NUnit/MSTest/Jest/Mocha/Vitest)
5. **TRX Logger Available:** .NET projects have access to the TRX logger (included in .NET SDK by default)
6. **JSON Reporter Available:** Node.js test frameworks support JSON output (Jest has `--json` flag, Vitest supports custom reporters)
7. **File System Access:** Runtime user has read/write permissions to project directories and temp directories for TRX/result files
8. **Standard Output Redirection:** Test process stdout/stderr can be captured and redirected for parsing
9. **Process Spawning:** Operating system allows spawning child processes for test execution
10. **UTF-8 Encoding:** Test output is UTF-8 encoded (or compatible encoding) for reliable parsing

### Operational Assumptions

11. **Single Repository Root:** Each test execution operates within a single repository context (multi-repo scenarios handled via multiple invocations)
12. **Test Projects Identifiable:** Test projects follow naming conventions (e.g., `*.Tests.csproj`, test scripts in `package.json`) for auto-detection
13. **No Concurrent Builds:** Tests run while project is not being actively modified/built by another process (file locking)
14. **Network Access for Restore:** If dependencies not cached, internet access available for package restore (configurable to fail-fast if offline)
15. **Sufficient Disk Space:** Temporary TRX files, coverage reports, and logs can be written without disk-full errors
16. **Environment Variables:** Test execution environment inherits necessary environment variables (e.g., `DOTNET_CLI_HOME`, `NODE_ENV`)
17. **No Interactive Tests:** Tests do not require user input or GUI interaction (headless execution)
18. **Test Timeout Acceptable:** Default timeout of 300 seconds (5 minutes) is sufficient for most test suites (configurable via options)

### Integration Assumptions

19. **Task-019a Complete:** Layout detection (Task 019a) is implemented and returns valid `DetectionResult` with test project metadata
20. **Task-018 Complete:** Command runner (Task 018) is implemented and can execute processes with timeout/cancellation support
21. **Task-002a Configuration:** Configuration system can provide test runner settings (default timeout, output format preferences)
22. **Task-018c Artifacts:** Artifact storage system available to persist test results and coverage reports if requested

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

## Security

### Threat 1: Command Injection via Test Filter Arguments

**Risk Description:** Malicious test filter expressions could inject shell commands if filter arguments are not properly sanitized before being passed to `dotnet test` or `npm test` processes.

**Attack Scenario:**
```csharp
// Attacker provides malicious filter
var maliciousFilter = "UnitTests; rm -rf /; #";

// Vulnerable code:
var command = $"dotnet test --filter \"{maliciousFilter}\"";  // DANGEROUS!
// Executes: dotnet test --filter "UnitTests; rm -rf /; #"
```

**Mitigation (Complete C# Code):**

```csharp
using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Testing
{
    public sealed class FilterSanitizer
    {
        private readonly ILogger<FilterSanitizer> _logger;

        // Allow only alphanumeric, dots, underscores, equals, ampersands, tilde, pipes
        private static readonly Regex AllowedFilterPattern = new(
            @"^[a-zA-Z0-9._=&~|() ]+$",
            RegexOptions.Compiled);

        // Detect shell metacharacters
        private static readonly Regex ShellMetacharPattern = new(
            @"[;&|`$<>{}[\]\\]",
            RegexOptions.Compiled);

        public FilterSanitizer(ILogger<FilterSanitizer> logger)
        {
            _logger = logger;
        }

        public ValidationResult ValidateFilter(string filter, FilterType filterType)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return ValidationResult.Success();  // Empty filter is valid (runs all tests)
            }

            // Check for shell metacharacters (strict)
            if (ShellMetacharPattern.IsMatch(filter))
            {
                _logger.LogWarning(
                    "Filter contains shell metacharacters: {Filter}",
                    filter);
                return ValidationResult.Fail(
                    $"Filter '{filter}' contains invalid characters (;, &, |, `, $, <, >, etc.)");
            }

            // Validate against allowed pattern
            if (!AllowedFilterPattern.IsMatch(filter))
            {
                _logger.LogWarning(
                    "Filter does not match allowed pattern: {Filter}",
                    filter);
                return ValidationResult.Fail(
                    $"Filter '{filter}' contains disallowed characters. " +
                    "Allowed: alphanumeric, dots, underscores, equals, ampersands, tilde, pipes, parentheses");
            }

            // Framework-specific validation
            var frameworkValidation = filterType switch
            {
                FilterType.DotNet => ValidateDotNetFilter(filter),
                FilterType.Jest => ValidateJestFilter(filter),
                FilterType.Mocha => ValidateMochaFilter(filter),
                _ => ValidationResult.Success()
            };

            return frameworkValidation;
        }

        private ValidationResult ValidateDotNetFilter(string filter)
        {
            // .NET filter syntax: "FullyQualifiedName~Namespace.Class" or "Category=Unit&Priority=1"
            // Validate specific keywords
            var allowedKeywords = new[] { "FullyQualifiedName", "Name", "Category", "Priority", "TestCategory" };
            var hasValidKeyword = false;

            foreach (var keyword in allowedKeywords)
            {
                if (filter.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    hasValidKeyword = true;
                    break;
                }
            }

            if (!hasValidKeyword && filter.Length > 0)
            {
                _logger.LogWarning(
                    "DotNet filter does not contain recognized keywords: {Filter}",
                    filter);
            }

            return ValidationResult.Success();
        }

        private ValidationResult ValidateJestFilter(string filter)
        {
            // Jest uses regex patterns, ensure they're safe
            if (filter.Contains("(?", StringComparison.Ordinal))
            {
                return ValidationResult.Fail("Jest filter cannot contain regex look-ahead/behind");
            }

            return ValidationResult.Success();
        }

        private ValidationResult ValidateMochaFilter(string filter)
        {
            // Mocha --grep uses regex
            if (filter.Length > 500)
            {
                return ValidationResult.Fail($"Mocha filter too long: {filter.Length} chars (max 500)");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Escapes filter for safe process argument passing
        /// </summary>
        public string EscapeForProcessArgument(string filter, FilterType filterType)
        {
            // Always validate first
            var validation = ValidateFilter(filter, filterType);
            if (!validation.IsValid)
            {
                throw new ArgumentException(validation.ErrorMessage, nameof(filter));
            }

            // Escape double quotes and backslashes for command-line safety
            var escaped = filter
                .Replace("\\", "\\\\")  // Escape backslashes
                .Replace("\"", "\\\""); // Escape double quotes

            return escaped;
        }
    }

    public enum FilterType
    {
        DotNet,
        Jest,
        Mocha,
        Vitest
    }

    public readonly record struct ValidationResult(bool IsValid, string ErrorMessage)
    {
        public static ValidationResult Success() => new(true, string.Empty);
        public static ValidationResult Fail(string message) => new(false, message);
        public static ValidationResult Warn(string message) => new(true, message);  // Valid but with warning
    }
}
```

---

### Threat 2: Path Traversal in Test Result File Output

**Risk Description:** If test result file paths (TRX, coverage reports) are constructed from user input without validation, attackers could write results outside the intended directory, potentially overwriting system files.

**Attack Scenario:**
```csharp
// Attacker provides malicious output path
var maliciousPath = "../../../etc/passwd";

// Vulnerable code:
var outputPath = Path.Combine(_tempDir, maliciousPath + ".trx");  // DANGEROUS!
// Results in: /tmp/../../../etc/passwd.trx => /etc/passwd.trx
File.WriteAllText(outputPath, trxContent);  // Overwrites /etc/passwd.trx!
```

**Mitigation (Complete C# Code):**

```csharp
using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Testing
{
    public sealed class SafeResultFileWriter
    {
        private readonly string _allowedRootPath;
        private readonly ILogger<SafeResultFileWriter> _logger;

        public SafeResultFileWriter(string allowedRootPath, ILogger<SafeResultFileWriter> logger)
        {
            _allowedRootPath = Path.GetFullPath(allowedRootPath);
            _logger = logger;
        }

        public ValidationResult ValidateOutputPath(string requestedPath)
        {
            // Resolve to absolute path
            var absolutePath = Path.GetFullPath(Path.Combine(_allowedRootPath, requestedPath));

            // Ensure resolved path is within allowed root
            if (!absolutePath.StartsWith(_allowedRootPath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Path traversal detected: {Requested} resolves to {Absolute} outside {Root}",
                    requestedPath, absolutePath, _allowedRootPath);

                return ValidationResult.Fail(
                    $"Output path '{requestedPath}' resolves outside allowed directory");
            }

            // Check for excessive parent directory traversals (suspicious)
            var traversalCount = requestedPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
                .Count(segment => segment == "..");

            if (traversalCount > 2)
            {
                _logger.LogWarning(
                    "Excessive path traversal detected: {Count} levels in {Path}",
                    traversalCount, requestedPath);

                return ValidationResult.Warn(
                    $"Output path '{requestedPath}' contains {traversalCount} parent traversals (suspicious)");
            }

            // Validate file extension (only allow known result formats)
            var allowedExtensions = new[] { ".trx", ".xml", ".json", ".coverage", ".cobertura.xml" };
            var extension = Path.GetExtension(absolutePath).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                _logger.LogWarning(
                    "Invalid result file extension: {Extension} for {Path}",
                    extension, requestedPath);

                return ValidationResult.Fail(
                    $"Output file extension '{extension}' not allowed. " +
                    $"Allowed: {string.Join(", ", allowedExtensions)}");
            }

            return ValidationResult.Success();
        }

        public async Task<string> WriteTestResultAsync(
            string requestedPath,
            string content,
            CancellationToken ct)
        {
            // Validate path
            var validation = ValidateOutputPath(requestedPath);
            if (!validation.IsValid)
            {
                throw new SecurityException($"Invalid output path: {validation.ErrorMessage}");
            }

            // Resolve safe absolute path
            var safePath = Path.GetFullPath(Path.Combine(_allowedRootPath, requestedPath));

            // Ensure directory exists
            var directory = Path.GetDirectoryName(safePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Created output directory: {Directory}", directory);
            }

            // Write atomically (temp file + rename)
            var tempPath = safePath + ".tmp";
            try
            {
                await File.WriteAllTextAsync(tempPath, content, ct);

                // Atomic rename (overwrites if exists)
                File.Move(tempPath, safePath, overwrite: true);

                _logger.LogInformation("Wrote test result to: {Path}", safePath);
                return safePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write test result to: {Path}", safePath);

                // Clean up temp file
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                throw;
            }
        }
    }
}
```

---

### Threat 3: Resource Exhaustion via Infinite Test Loops

**Risk Description:** Malicious or buggy tests could enter infinite loops, consuming CPU indefinitely and blocking the agent. Without timeout enforcement, test runs could hang for hours.

**Attack Scenario:**
```csharp
[Fact]
public void MaliciousTest_InfiniteLoop()
{
    while (true)  // Infinite loop
    {
        Thread.Sleep(1000);
    }
}
```

**Mitigation (Complete C# Code):**

```csharp
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Testing
{
    public sealed class TimeoutEnforcedTestRunner
    {
        private readonly ILogger<TimeoutEnforcedTestRunner> _logger;
        private readonly TimeSpan _defaultTimeout;

        public TimeoutEnforcedTestRunner(
            ILogger<TimeoutEnforcedTestRunner> logger,
            TimeSpan defaultTimeout)
        {
            _logger = logger;
            _defaultTimeout = defaultTimeout;
        }

        public async Task<TestRunResult> RunWithTimeoutAsync(
            string testCommand,
            string arguments,
            string workingDirectory,
            TimeSpan? customTimeout,
            CancellationToken ct)
        {
            var effectiveTimeout = customTimeout ?? _defaultTimeout;
            var sw = Stopwatch.StartNew();

            _logger.LogInformation(
                "Starting test run with timeout {Timeout}s: {Command} {Args}",
                effectiveTimeout.TotalSeconds, testCommand, arguments);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(effectiveTimeout);

            Process? process = null;
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = testCommand,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process = Process.Start(psi);
                if (process == null)
                {
                    throw new InvalidOperationException($"Failed to start process: {testCommand}");
                }

                // Wait for exit with timeout enforcement
                await process.WaitForExitAsync(cts.Token);

                sw.Stop();

                var stdout = await process.StandardOutput.ReadToEndAsync(CancellationToken.None);
                var stderr = await process.StandardError.ReadToEndAsync(CancellationToken.None);

                _logger.LogInformation(
                    "Test run completed in {Duration}ms with exit code {ExitCode}",
                    sw.ElapsedMilliseconds, process.ExitCode);

                return new TestRunResult
                {
                    ExitCode = process.ExitCode,
                    StandardOutput = stdout,
                    StandardError = stderr,
                    Duration = sw.Elapsed,
                    TimedOut = false
                };
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
            {
                sw.Stop();

                _logger.LogWarning(
                    "Test run timed out after {Timeout}s (limit: {Limit}s)",
                    sw.Elapsed.TotalSeconds, effectiveTimeout.TotalSeconds);

                // Kill process tree
                if (process != null && !process.HasExited)
                {
                    _logger.LogWarning("Killing test process {PID}", process.Id);

                    try
                    {
                        // Kill process tree (parent + children)
                        KillProcessTree(process.Id);
                        process.Kill(entireProcessTree: true);
                        process.WaitForExit(5000);  // Give it 5 seconds to die
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to kill test process {PID}", process.Id);
                    }
                }

                // Return partial results (timeout)
                return new TestRunResult
                {
                    ExitCode = -1,
                    StandardOutput = string.Empty,
                    StandardError = $"Test execution timed out after {effectiveTimeout.TotalSeconds} seconds",
                    Duration = sw.Elapsed,
                    TimedOut = true
                };
            }
            finally
            {
                process?.Dispose();
            }
        }

        private void KillProcessTree(int processId)
        {
            // Platform-specific process tree killing
            if (OperatingSystem.IsWindows())
            {
                // Windows: use taskkill /T
                var killProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = "taskkill",
                    Arguments = $"/PID {processId} /T /F",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                killProcess?.WaitForExit(3000);
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                // Linux/macOS: kill process group
                var killProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = "pkill",
                    Arguments = $"-P {processId}",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                killProcess?.WaitForExit(3000);
            }
        }
    }

    public sealed class TestRunResult
    {
        public int ExitCode { get; init; }
        public string StandardOutput { get; init; } = string.Empty;
        public string StandardError { get; init; } = string.Empty;
        public TimeSpan Duration { get; init; }
        public bool TimedOut { get; init; }
    }
}
```

---

### Threat 4: Output Injection in Test Result Messages

**Risk Description:** Malicious test code could inject ANSI escape codes or control characters into test output (error messages, stack traces) to manipulate terminal display, potentially hiding failures or injecting fake success messages.

**Attack Scenario:**
```csharp
[Fact]
public void MaliciousTest_OutputInjection()
{
    // Inject ANSI codes to clear screen and show fake success
    var maliciousMessage = "\x1b[2J\x1b[H✓ All tests passed (0 failed, 1000 passed)";
    throw new Exception(maliciousMessage);  // Appears as success!
}
```

**Mitigation (Complete C# Code):**

```csharp
using System;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Testing
{
    public sealed class OutputSanitizer
    {
        private readonly ILogger<OutputSanitizer> _logger;

        // ANSI escape code pattern (CSI sequences)
        private static readonly Regex AnsiEscapePattern = new(
            @"\x1b\[[0-9;]*[a-zA-Z]",
            RegexOptions.Compiled);

        // Control characters (except newline, tab, carriage return)
        private static readonly Regex ControlCharPattern = new(
            @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]",
            RegexOptions.Compiled);

        public OutputSanitizer(ILogger<OutputSanitizer> logger)
        {
            _logger = logger;
        }

        public string SanitizeTestOutput(string rawOutput, int maxLength = 100_000)
        {
            if (string.IsNullOrEmpty(rawOutput))
            {
                return rawOutput;
            }

            // Truncate if too long (prevent memory exhaustion)
            if (rawOutput.Length > maxLength)
            {
                _logger.LogWarning(
                    "Test output truncated from {Original} to {Max} characters",
                    rawOutput.Length, maxLength);

                rawOutput = rawOutput.Substring(0, maxLength) + "\n[... output truncated ...]";
            }

            // Remove ANSI escape codes
            var withoutAnsi = AnsiEscapePattern.Replace(rawOutput, string.Empty);

            // Remove control characters (keep \n, \r, \t)
            var sanitized = ControlCharPattern.Replace(withoutAnsi, string.Empty);

            // Check if sanitization occurred
            if (sanitized.Length != rawOutput.Length)
            {
                var removedChars = rawOutput.Length - sanitized.Length;
                _logger.LogInformation(
                    "Sanitized test output: removed {Count} ANSI/control characters",
                    removedChars);
            }

            return sanitized;
        }

        public string SanitizeTestName(string testName, int maxLength = 500)
        {
            if (string.IsNullOrWhiteSpace(testName))
            {
                return "[unnamed test]";
            }

            // Remove control characters and ANSI codes
            var sanitized = ControlCharPattern.Replace(testName, string.Empty);
            sanitized = AnsiEscapePattern.Replace(sanitized, string.Empty);

            // Truncate if too long
            if (sanitized.Length > maxLength)
            {
                sanitized = sanitized.Substring(0, maxLength) + "...";
            }

            // Ensure non-empty
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                _logger.LogWarning("Test name sanitized to empty string, original: {Original}", testName);
                return "[invalid test name]";
            }

            return sanitized;
        }

        public string SanitizeErrorMessage(string errorMessage, int maxLength = 5000)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                return errorMessage;
            }

            // Sanitize control chars and ANSI codes
            var sanitized = SanitizeTestOutput(errorMessage, maxLength);

            // Escape XML special characters (for TRX compatibility)
            sanitized = EscapeXml(sanitized);

            return sanitized;
        }

        private static string EscapeXml(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return input
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        public TestResult SanitizeTestResult(TestResult rawResult)
        {
            return new TestResult
            {
                TestName = SanitizeTestName(rawResult.TestName),
                FullyQualifiedName = SanitizeTestName(rawResult.FullyQualifiedName),
                Outcome = rawResult.Outcome,  // Enum, no sanitization needed
                Duration = rawResult.Duration,
                ErrorMessage = rawResult.ErrorMessage != null
                    ? SanitizeErrorMessage(rawResult.ErrorMessage)
                    : null,
                StackTrace = rawResult.StackTrace != null
                    ? SanitizeTestOutput(rawResult.StackTrace, maxLength: 10_000)
                    : null
            };
        }
    }

    public sealed class TestResult
    {
        public string TestName { get; init; } = string.Empty;
        public string FullyQualifiedName { get; init; } = string.Empty;
        public TestOutcome Outcome { get; init; }
        public TimeSpan Duration { get; init; }
        public string? ErrorMessage { get; init; }
        public string? StackTrace { get; init; }
    }

    public enum TestOutcome
    {
        Passed,
        Failed,
        Skipped,
        NotExecuted
    }
}
```

---

### Threat 5: Denial of Service via Massive Test Output

**Risk Description:** Malicious tests could generate gigabytes of output (console writes in tight loops), causing memory exhaustion and process crashes.

**Attack Scenario:**
```csharp
[Fact]
public void MaliciousTest_OutputFlood()
{
    for (int i = 0; i < 100_000_000; i++)
    {
        Console.WriteLine(new string('A', 10_000));  // 1GB+ output
    }
}
```

**Mitigation (Complete C# Code):**

```csharp
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Testing
{
    public sealed class BoundedOutputCapture
    {
        private readonly ILogger<BoundedOutputCapture> _logger;
        private readonly int _maxOutputBytes;

        public BoundedOutputCapture(
            ILogger<BoundedOutputCapture> logger,
            int maxOutputBytes = 10_000_000)  // 10MB default
        {
            _logger = logger;
            _maxOutputBytes = maxOutputBytes;
        }

        public async Task<CapturedOutput> CaptureOutputAsync(
            StreamReader outputStream,
            string streamName,
            CancellationToken ct)
        {
            var buffer = new StringBuilder();
            var totalBytes = 0;
            var truncated = false;

            try
            {
                char[] chunk = new char[4096];
                int charsRead;

                while ((charsRead = await outputStream.ReadAsync(chunk, 0, chunk.Length, ct)) > 0)
                {
                    var chunkBytes = Encoding.UTF8.GetByteCount(chunk, 0, charsRead);
                    totalBytes += chunkBytes;

                    if (totalBytes > _maxOutputBytes)
                    {
                        truncated = true;
                        _logger.LogWarning(
                            "{Stream} output truncated at {Size} bytes (limit: {Limit} bytes)",
                            streamName, totalBytes, _maxOutputBytes);

                        buffer.Append($"\n\n[{streamName} TRUNCATED - exceeded {_maxOutputBytes} bytes limit]");
                        break;
                    }

                    buffer.Append(chunk, 0, charsRead);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("{Stream} capture cancelled", streamName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing {Stream} output", streamName);
            }

            return new CapturedOutput
            {
                Content = buffer.ToString(),
                BytesRead = totalBytes,
                Truncated = truncated
            };
        }

        public async Task<(CapturedOutput stdout, CapturedOutput stderr)> CaptureAllAsync(
            StreamReader stdoutStream,
            StreamReader stderrStream,
            CancellationToken ct)
        {
            // Capture stdout and stderr concurrently
            var stdoutTask = CaptureOutputAsync(stdoutStream, "stdout", ct);
            var stderrTask = CaptureOutputAsync(stderrStream, "stderr", ct);

            await Task.WhenAll(stdoutTask, stderrTask);

            return (await stdoutTask, await stderrTask);
        }
    }

    public sealed class CapturedOutput
    {
        public string Content { get; init; } = string.Empty;
        public int BytesRead { get; init; }
        public bool Truncated { get; init; }
    }
}
```

---

## Troubleshooting

### Issue 1: No Tests Discovered in Test Project

**Symptoms:**
- `acode test` returns "0 tests found"
- Output shows "Test run complete: 0 passed, 0 failed"
- Project clearly contains test classes/methods
- Manual `dotnet test` or `npm test` finds and runs tests successfully

**Causes:**
- Test project not built (missing compiled assemblies for .NET)
- Test framework package not installed or not restored
- Test class/method attributes missing or incorrect (e.g., missing `[Fact]` attribute)
- Test discovery filters excluding all tests
- Test project metadata indicates `IsTestProject=false` incorrectly
- For Node.js: `test` script missing from `package.json`

**Solutions:**

**Solution 1: Verify Project is Built**
```bash
# For .NET projects
dotnet build /path/to/MyApp.Tests.csproj --configuration Release

# Verify DLL exists
ls -la /path/to/MyApp.Tests/bin/Release/net8.0/MyApp.Tests.dll

# Try test run without --no-build flag
acode test --project MyApp.Tests --no-build false
```

**Solution 2: Check Test Framework Packages**
```bash
# For .NET projects - verify test framework is installed
grep -A5 "<ItemGroup>" MyApp.Tests.csproj | grep -E "xunit|NUnit|MSTest"

# Expected output (xUnit example):
# <PackageReference Include="xunit" Version="2.6.2" />
# <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
# <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />

# If missing, add packages
dotnet add package xunit --version 2.6.2
dotnet add package xunit.runner.visualstudio --version 2.5.4
dotnet add package Microsoft.NET.Test.Sdk --version 17.8.0
dotnet restore
```

**Solution 3: Verify Test Attributes**
```csharp
// WRONG - test will NOT be discovered:
public class UserServiceTests
{
    public void GetUser_ValidId_ReturnsUser()  // Missing [Fact] attribute!
    {
        // Test code
    }
}

// CORRECT - test WILL be discovered:
using Xunit;

public class UserServiceTests
{
    [Fact]  // Required attribute for xUnit
    public void GetUser_ValidId_ReturnsUser()
    {
        // Test code
    }
}

// For NUnit:
using NUnit.Framework;

[TestFixture]
public class UserServiceTests
{
    [Test]  // NUnit uses [Test] instead of [Fact]
    public void GetUser_ValidId_ReturnsUser()
    {
        // Test code
    }
}
```

**Solution 4: Run with Verbose Logging**
```bash
# Enable detailed discovery output
acode test --project MyApp.Tests --verbose --log-level Debug

# Output will show:
# [DEBUG] Discovering tests in /repo/MyApp.Tests.csproj
# [DEBUG] Test framework detected: xUnit 2.6.2
# [DEBUG] Running: dotnet test MyApp.Tests.csproj --list-tests
# [DEBUG] Discovered tests:
#   - MyApp.Tests.UserServiceTests.GetUser_ValidId_ReturnsUser
#   - MyApp.Tests.UserServiceTests.GetUser_InvalidId_ThrowsException
# [INFO] Test discovery complete: 2 tests found
```

**Solution 5: Check Node.js Test Script**
```bash
# For Node.js projects - verify test script exists
jq '.scripts.test' package.json

# If null or missing, add test script:
npm pkg set scripts.test="jest"

# Or manually edit package.json:
{
  "scripts": {
    "test": "jest",  // Add this line
    "build": "tsc"
  }
}
```

---

### Issue 2: TRX Parse Failure (Results Not Structured)

**Symptoms:**
- Test run completes but results show raw TRX XML instead of structured JSON
- Error: "XmlException: The 'Results' start tag does not match"
- Results object has `totalTests: 0` despite tests running
- Log shows "TRX parser warning: invalid format"

**Causes:**
- TRX file corrupted or incomplete (test process crashed mid-write)
- TRX format version mismatch (older/newer than expected schema)
- Encoding issues (UTF-16 BOM causing parse failure)
- Concurrent writes to same TRX file (race condition)
- Disk full during TRX write (truncated file)

**Solutions:**

**Solution 1: Verify TRX File Integrity**
```bash
# Locate TRX file
TRX_FILE=$(find ~/.acode/test-results -name "*.trx" | head -1)

# Check file size (should be >1KB)
ls -lh "$TRX_FILE"

# Validate XML structure
xmllint --noout "$TRX_FILE"
# If valid: no output
# If invalid: shows line/column of error

# Check for truncation (last line should be </TestRun>)
tail -1 "$TRX_FILE"
```

**Solution 2: Use JSON Reporter for Node.js**
```bash
# For Jest tests - use JSON reporter instead of default
acode test --project frontend --output-format json

# Or configure Jest to always output JSON:
cat >> jest.config.js << 'EOF'
module.exports = {
  reporters: [
    'default',
    ['jest-json-reporter', {
      outputPath: './test-results.json'
    }]
  ]
};
EOF

npm install --save-dev jest-json-reporter
```

**Solution 3: Handle Encoding Issues**
```csharp
// In TrxParser.cs - auto-detect encoding
public TestRunResult ParseTrx(string trxFilePath)
{
    // Read raw bytes to detect BOM
    var bytes = File.ReadAllBytes(trxFilePath);
    var encoding = DetectEncoding(bytes);

    var xmlContent = encoding.GetString(bytes);

    // Remove BOM if present
    if (xmlContent.StartsWith("\uFEFF"))
    {
        xmlContent = xmlContent.Substring(1);
    }

    using var stringReader = new StringReader(xmlContent);
    using var xmlReader = XmlReader.Create(stringReader, _xmlSettings);
    var doc = XDocument.Load(xmlReader);

    // Parse TestRun element
    return ParseTestRunElement(doc.Root);
}
```

**Solution 4: Enable Fallback to Raw Output**
```bash
# Configure test runner to return raw output on parse failure
acode config set test.fallback-to-raw-output true

# Now if TRX parse fails, you still get results:
acode test --project MyApp.Tests
# Output (on parse failure):
# {
#   "totalTests": -1,
#   "rawOutput": "Test Run Successful.\nTotal tests: 47\nPassed: 46\nFailed: 1",
#   "parseError": "TRX parse failed: invalid XML at line 42"
# }
```

**Solution 5: Clear Stale TRX Files**
```bash
# Remove old TRX files that may conflict
rm -rf ~/.acode/test-results/*.trx

# Run test with fresh output
acode test --project MyApp.Tests --force-refresh
```

---

### Issue 3: Test Run Exceeds Timeout

**Symptoms:**
- Test execution stops after exactly 300 seconds (5 minutes)
- Output shows "Test execution timed out after 300 seconds"
- Some tests pass, but run is incomplete
- Process killed before all tests complete

**Causes:**
- Tests legitimately slow (e.g., integration tests with database operations)
- Infinite loop in test code
- Test waiting for external resource that never responds (HTTP timeout, database deadlock)
- Insufficient timeout for large test suite (1000+ tests)
- Tests running sequentially instead of parallel (slower than expected)

**Solutions:**

**Solution 1: Increase Timeout**
```bash
# Set timeout to 10 minutes (600 seconds)
acode test --project MyApp.Tests --timeout 600

# For very large test suites, increase further
acode test --project MyApp.Tests --timeout 1800  # 30 minutes

# Set default timeout in config
acode config set test.default-timeout-seconds 600
```

**Solution 2: Run Tests in Parallel**
```bash
# For .NET tests - enable parallel execution
acode test --project MyApp.Tests -- --parallel

# Or configure in .csproj:
<PropertyGroup>
  <ParallelizeTestCollections>true</ParallelizeTestCollections>
  <MaxParallelThreads>4</MaxParallelThreads>
</PropertyGroup>

# For Jest - parallel is default, but can configure:
npm test -- --maxWorkers=4
```

**Solution 3: Identify Slow Tests**
```bash
# Run with verbose timing to find culprits
acode test --project MyApp.Tests --verbose

# Output shows per-test durations:
# [PASS] GetUser_ValidId_ReturnsUser (12ms)
# [PASS] GetUser_InvalidId_ThrowsException (8ms)
# [FAIL] GetUser_WithDatabase_IntegrationTest (45,230ms)  # SLOW!
# [PASS] DeleteUser_ValidId_DeletesUser (15ms)

# Run only slow test to diagnose:
acode test --project MyApp.Tests --filter "GetUser_WithDatabase_IntegrationTest" --timeout 120
```

**Solution 4: Break Up Large Test Suites**
```bash
# Instead of running all 1000 tests at once, split by category
acode test --project MyApp.Tests --filter "Category=Unit" --timeout 300
acode test --project MyApp.Tests --filter "Category=Integration" --timeout 600
acode test --project MyApp.Tests --filter "Category=E2E" --timeout 1800

# Or run per-class:
acode test --project MyApp.Tests --filter "FullyQualifiedName~UserServiceTests" --timeout 60
acode test --project MyApp.Tests --filter "FullyQualifiedName~OrderServiceTests" --timeout 60
```

**Solution 5: Debug Infinite Loop**
```bash
# Run single test with debugger to identify infinite loop
dotnet test MyApp.Tests.csproj --filter "FullyQualifiedName~SuspectedHangingTest" --logger "console;verbosity=detailed"

# If test hangs, check for:
# - while(true) with no exit condition
# - await Task.Delay() with CancellationToken.None (never cancels)
# - HTTP client calls without timeout configured
```

---

### Issue 4: Test Framework Not Detected

**Symptoms:**
- Error: "No test framework detected for project /path/to/MyApp.Tests"
- `acode detect` shows project but `isTestProject: false`
- Manual `dotnet test` works but `acode test` fails
- Tests run in IDE but not via CLI

**Causes:**
- Test framework package missing from project references
- Package reference uses unexpected naming (e.g., `xunit.core` instead of `xunit`)
- Test project is .NET Framework (not .NET Core/5+)
- Custom test framework not in Acode's detection list
- `IsTestProject` MSBuild property explicitly set to `false`

**Solutions:**

**Solution 1: Verify Test Framework Package**
```bash
# Check for standard test framework packages
dotnet list package | grep -E "xunit|NUnit|MSTest"

# Expected output (xUnit):
# > xunit                   2.6.2
# > xunit.runner.visualstudio  2.5.4
# > Microsoft.NET.Test.Sdk  17.8.0

# If missing Microsoft.NET.Test.Sdk, add it:
dotnet add package Microsoft.NET.Test.Sdk --version 17.8.0
```

**Solution 2: Check IsTestProject Property**
```xml
<!-- In MyApp.Tests.csproj -->
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <IsPackable>false</IsPackable>
  <!-- Ensure IsTestProject is true or omitted (defaults to true for test projects) -->
  <IsTestProject>true</IsTestProject>
</PropertyGroup>
```

**Solution 3: Explicitly Specify Framework**
```bash
# Override auto-detection by specifying framework explicitly
acode test --project MyApp.Tests --framework xunit

# Supported frameworks: xunit, nunit, mstest, jest, mocha, vitest
```

**Solution 4: Check Framework Detection Logic**
```csharp
// Acode's detection heuristics (for reference):

// xUnit detection:
bool IsXUnit(ProjectMetadata project) =>
    project.PackageReferences.ContainsKey("xunit") ||
    project.PackageReferences.ContainsKey("xunit.core");

// NUnit detection:
bool IsNUnit(ProjectMetadata project) =>
    project.PackageReferences.ContainsKey("NUnit") ||
    project.PackageReferences.ContainsKey("nunit.framework");

// MSTest detection:
bool IsMSTest(ProjectMetadata project) =>
    project.PackageReferences.ContainsKey("MSTest.TestFramework") ||
    project.PackageReferences.ContainsKey("Microsoft.VisualStudio.TestPlatform.TestFramework");
```

**Solution 5: Use Repo Contract Hint**
```yaml
# In .acode/config.yml - provide explicit test framework hint
test:
  framework-overrides:
    "tests/MyApp.Tests": xunit
    "tests/MyApp.IntegrationTests": xunit
    "frontend/src/__tests__": jest
```

---

### Issue 5: Filter Syntax Error (No Tests Match Filter)

**Symptoms:**
- `acode test --filter "..."` returns "0 tests found"
- Filter syntax appears correct but no matches
- Removing filter runs all tests successfully
- Error: "Filter expression is invalid"

**Causes:**
- Filter syntax varies between frameworks (.NET uses `~` for contains, Jest uses regex)
- Typo in test name or namespace
- Case sensitivity mismatch
- Incorrect property name (e.g., `TestCategory` vs `Category`)
- Special characters not escaped (e.g., parentheses in test names)

**Solutions:**

**Solution 1: Verify Filter Syntax for Framework**
```bash
# .NET filter syntax (uses property expressions)
acode test --project MyApp.Tests --filter "FullyQualifiedName~UserService"      # Contains
acode test --project MyApp.Tests --filter "Category=Unit"                       # Exact match
acode test --project MyApp.Tests --filter "Name~GetUser"                        # Test name contains
acode test --project MyApp.Tests --filter "Category=Unit&Priority=1"           # AND condition
acode test --project MyApp.Tests --filter "Category=Unit|Category=Integration" # OR condition

# Jest filter syntax (uses regex)
acode test --project frontend --filter "UserService"       # Regex match (no quotes)
acode test --project frontend --filter "^UserService"      # Starts with
acode test --project frontend --filter "GetUser.*ValidId"  # Regex pattern

# Mocha filter syntax (uses --grep)
acode test --project backend-tests --filter "UserService"  # Substring match
acode test --project backend-tests --filter "/^UserService/"  # Regex (wrapped in /)
```

**Solution 2: List All Tests to Find Correct Names**
```bash
# For .NET - list all test names
dotnet test MyApp.Tests.csproj --list-tests

# Output:
# MyApp.Tests.UserServiceTests.GetUser_ValidId_ReturnsUser
# MyApp.Tests.UserServiceTests.GetUser_InvalidId_ThrowsException
# MyApp.Tests.OrderServiceTests.CreateOrder_ValidInput_CreatesOrder

# Now use exact name in filter:
acode test --project MyApp.Tests --filter "FullyQualifiedName~MyApp.Tests.UserServiceTests"
```

**Solution 3: Escape Special Characters**
```bash
# If test name contains parentheses, escape them:
# Test name: "GetUser(ValidId)_ReturnsUser"

# WRONG (will fail):
acode test --filter "GetUser(ValidId)"

# CORRECT (escaped):
acode test --filter "GetUser\\(ValidId\\)"

# Or use FullyQualifiedName with ~ (contains):
acode test --filter "FullyQualifiedName~GetUser"
```

**Solution 4: Check Case Sensitivity**
```bash
# .NET filters are case-sensitive!

# WRONG (won't match "UserServiceTests"):
acode test --filter "FullyQualifiedName~userservicetests"

# CORRECT:
acode test --filter "FullyQualifiedName~UserServiceTests"

# For case-insensitive matching, use wildcards:
acode test --filter "Name~User"  # Matches "UserTests", "userTests", "USER_TESTS"
```

**Solution 5: Validate Filter Before Test Run**
```bash
# Use dry-run mode to validate filter (list tests that would run)
acode test --project MyApp.Tests --filter "Category=Unit" --dry-run

# Output shows matched tests:
# [DRY-RUN] Would execute 47 tests:
#   1. MyApp.Tests.UserServiceTests.GetUser_ValidId_ReturnsUser
#   2. MyApp.Tests.UserServiceTests.GetUser_InvalidId_ThrowsException
#   ... (45 more)

# If output shows "Would execute 0 tests", filter is wrong
```

---

## Best Practices

### Test Execution

1. **Run all tests by default** - Filter only when explicitly requested
2. **Parallel when safe** - Enable parallel test execution for speed
3. **Isolate test runs** - Prevent test pollution between runs
4. **Timeout per test** - Prevent hung tests from blocking everything

### Result Parsing

5. **Use structured formats** - TRX for .NET, JSON for Jest
6. **Handle all outcomes** - Pass, fail, skip, inconclusive
7. **Include timing** - Track duration per test for optimization
8. **Preserve stack traces** - Full error details for debugging

### Coverage Collection

9. **Optional by default** - Coverage has performance cost
10. **Standard formats** - Cobertura, Istanbul for tooling compatibility
11. **Report summary** - Show line and branch coverage percentages
12. **Store coverage files** - Keep for trend analysis

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