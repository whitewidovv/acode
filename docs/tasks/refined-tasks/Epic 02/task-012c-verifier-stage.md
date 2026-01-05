# Task 012.c: Verifier Stage

**Priority:** P0 – Critical Path  
**Tier:** Core Infrastructure  
**Complexity:** 21 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 012 (Multi-Stage Loop), Task 012.b (Executor), Task 050 (Workspace DB)  

---

## Description

Task 012.c implements the Verifier stage—the quality gate that validates execution results before work proceeds to review. While the Executor does the work, the Verifier checks that the work is correct. This catch-early approach prevents wasted effort in later stages and catches regressions immediately.

The Verifier operates on step and task outputs, comparing actual results against the acceptance criteria defined during planning. Each step has verification criteria—what must be true for the step to be considered successful. The Verifier evaluates these criteria programmatically where possible and via LLM reasoning where necessary.

Programmatic verification is preferred. When a step writes a file, verify the file exists and matches expected patterns. When a step runs tests, verify tests pass. When a step creates a class, verify the class compiles. These checks are fast, reliable, and deterministic.

LLM-based verification handles fuzzy criteria. "Code should be well-structured" or "implementation should match the user's intent" require reasoning. The Verifier presents the step output and criteria to the LLM, which provides a structured pass/fail judgment with explanation.

Verification results are structured and actionable. A pass means the step succeeded—proceed. A fail includes specific reasons: what was wrong, what was expected, what was found. These reasons flow back to the Executor for retry, enabling targeted fixes rather than blind re-execution.

Test execution is a key verification strategy. When the plan includes tests (unit tests, integration tests), the Verifier runs them and interprets results. Test failures are verification failures—the step didn't achieve its goal. Test output is captured and included in the verification result.

Compilation checking catches syntax errors early. Before verifying behavior, the Verifier can check that code compiles/parses. A file that doesn't compile clearly fails verification. Language-specific tooling (dotnet build, tsc, etc.) provides this capability.

Static analysis can augment verification. Linters catch style issues. Type checkers catch type errors. Security scanners catch vulnerabilities. The Verifier can run configured analysis tools and interpret their output as pass/fail criteria.

The Verifier feeds the cycle logic in Task 012. When verification fails, the Orchestrator can cycle back to the Executor. The Verifier provides the failure information that guides retry. This feedback loop is essential for agentic self-correction.

Performance matters—verification shouldn't dominate execution time. Simple checks are fast. Test runs have configurable timeouts. LLM verification is minimized when programmatic checks suffice. The Verifier parallelizes independent checks where possible.

The Verifier respects Task 001 constraints. Any LLM-based verification uses local models. Test execution respects sandbox constraints. Static analysis runs locally. No external verification services.

Observability tracks verification decisions. Every check is logged with input, criteria, result, and reasoning (for LLM checks). Verification metrics (pass rate, common failures) inform iteration.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Verifier Stage | Stage that validates execution results |
| Verification Criteria | Conditions for step success |
| Programmatic Check | Automated, deterministic validation |
| LLM Verification | Reasoning-based validation |
| Pass | Verification succeeded |
| Fail | Verification found issues |
| Test Execution | Running tests as verification |
| Compilation Check | Verifying code compiles |
| Static Analysis | Automated code quality checking |
| Feedback | Failure info for retry |
| Cycle | Returning to Executor |
| Verification Result | Structured check outcome |
| Assertion | Specific pass/fail condition |
| Coverage | Extent of verification |
| Regression | Previously working now broken |

---

## Out of Scope

The following items are explicitly excluded from Task 012.c:

- **Holistic quality review** - Task 012.d
- **Test generation** - Executor responsibility
- **Code execution** - Executor responsibility
- **Security auditing** - Future enhancement
- **Performance testing** - Future enhancement
- **External verification services** - Task 001 constraint
- **Continuous verification** - Single-shot per step
- **Historical comparison** - Current step only
- **Custom verification plugins** - Future enhancement
- **Multi-language support** - Core languages only

---

## Assumptions

### Technical Assumptions

- ASM-001: Verification checks are defined per step type
- ASM-002: Assertions have pass/fail outcomes
- ASM-003: Test execution results are parseable
- ASM-004: Lint and build tools are available locally
- ASM-005: Verification scope is limited to current step changes

### Behavioral Assumptions

- ASM-006: Verification runs after each step execution
- ASM-007: Failed verification triggers re-planning or retry
- ASM-008: Verification results inform reviewer stage
- ASM-009: Partial passes (warnings) allow continuation
- ASM-010: Critical failures halt the loop

### Dependency Assumptions

- ASM-011: Task 012 orchestrator provides IStage contract
- ASM-012: Task 012.b executor provides step results to verify
- ASM-013: Build/test tooling exists in workspace
- ASM-014: Session state tracks verification outcomes

### Quality Assumptions

- ASM-015: Verification covers correctness, not style
- ASM-016: Test coverage is informative, not enforced
- ASM-017: Verification is fast (< 30s typical)

---

## Functional Requirements

### Stage Lifecycle

- FR-001: Verifier MUST implement IStage interface
- FR-002: OnEnter MUST load step results
- FR-003: Execute MUST verify all results
- FR-004: OnExit MUST report verification status
- FR-005: Lifecycle events MUST be logged

### Verification Flow

- FR-006: Each step result MUST be verified
- FR-007: Verification MUST use step criteria
- FR-008: Criteria MUST be loaded from plan
- FR-009: Results MUST be structured
- FR-010: Results MUST include pass/fail

### Programmatic Checks

- FR-011: File existence MUST be checkable
- FR-012: File content MUST be matchable
- FR-013: Pattern matching MUST work
- FR-014: Compilation status MUST be checkable
- FR-015: Test results MUST be checkable
- FR-016: Exit codes MUST be checkable

### File Verification

- FR-017: Verify file exists
- FR-018: Verify file has content
- FR-019: Verify content matches pattern
- FR-020: Verify file size in range
- FR-021: Verify file type correct

### Compilation Verification

- FR-022: Run language-appropriate build
- FR-023: Capture compilation output
- FR-024: Parse errors/warnings
- FR-025: Fail on compilation errors
- FR-026: Warn on compilation warnings

### Test Verification

- FR-027: Detect test files
- FR-028: Run test commands
- FR-029: Capture test output
- FR-030: Parse test results
- FR-031: Report pass/fail counts
- FR-032: Timeout after configured limit

### Static Analysis

- FR-033: Run configured linters
- FR-034: Capture linter output
- FR-035: Parse issues
- FR-036: Configure severity thresholds
- FR-037: Fail on threshold exceeded

### LLM Verification

- FR-038: Use LLM for fuzzy criteria
- FR-039: Present context and criteria
- FR-040: Request structured judgment
- FR-041: Parse LLM response
- FR-042: Include reasoning in result

### Verification Result

- FR-043: Result MUST have overall status
- FR-044: Result MUST list checked criteria
- FR-045: Result MUST include details per check
- FR-046: Failures MUST include reason
- FR-047: Failures MUST include suggestion

### Feedback Generation

- FR-048: Failed checks MUST generate feedback
- FR-049: Feedback MUST be actionable
- FR-050: Feedback MUST reference specific issues
- FR-051: Feedback MUST flow to Executor
- FR-052: Feedback MUST be logged

### Cycle Decision

- FR-053: Pass MUST advance to Reviewer
- FR-054: Fail MUST trigger cycle check
- FR-055: Cycle count MUST be checked
- FR-056: Within limit MUST cycle to Executor
- FR-057: At limit MUST escalate

### Timeout Handling

- FR-058: Verification MUST have timeout
- FR-059: Default timeout: 3 minutes
- FR-060: Configurable per check type
- FR-061: Timeout MUST fail gracefully
- FR-062: Timeout reason MUST be logged

### Parallelization

- FR-063: Independent checks MAY run in parallel
- FR-064: Order-dependent checks MUST be sequential
- FR-065: Parallel results MUST be aggregated
- FR-066: First failure MAY short-circuit

### Persistence

- FR-067: Verification results MUST be persisted
- FR-068: Check details MUST be persisted
- FR-069: Results MUST be queryable
- FR-070: History MUST be retained

### Configuration

- FR-071: Enabled checks MUST be configurable
- FR-072: Thresholds MUST be configurable
- FR-073: Timeouts MUST be configurable
- FR-074: Tool paths MUST be configurable

---

## Non-Functional Requirements

### Performance

- NFR-001: Programmatic checks < 100ms each
- NFR-002: Test runs < configured timeout
- NFR-003: LLM verification < 30s
- NFR-004: Parallelization where possible

### Reliability

- NFR-005: Deterministic for same input
- NFR-006: Crash-safe state
- NFR-007: Timeout recovery

### Accuracy

- NFR-008: No false positives (blocking good work)
- NFR-009: Minimal false negatives
- NFR-010: Clear pass/fail criteria

### Security

- NFR-011: Sandbox for test execution
- NFR-012: No arbitrary code execution
- NFR-013: Path validation

### Observability

- NFR-014: All checks logged
- NFR-015: Timing tracked
- NFR-016: Failure rates tracked

---

## User Manual Documentation

### Overview

The Verifier stage validates that execution results meet acceptance criteria. It catches issues early, before human review, enabling automated retry and self-correction.

### How Verification Works

After the Executor completes steps:

1. **Load Results** - Get step outputs and criteria
2. **Run Checks** - Execute programmatic validations
3. **Test if Needed** - Run tests when applicable
4. **LLM Review** - Reason about fuzzy criteria
5. **Report** - Pass to Reviewer or cycle back

### Verification Types

| Type | Speed | Accuracy | Use Case |
|------|-------|----------|----------|
| File Check | Fast | High | File exists/matches |
| Compilation | Medium | High | Code compiles |
| Test Run | Slow | Very High | Behavior correct |
| Static Analysis | Medium | High | Code quality |
| LLM Verification | Slow | Variable | Fuzzy criteria |

### Verification Output

```
$ acode run "Add email validation"

[EXECUTOR] Steps complete

[VERIFIER] Verifying results...
  ✓ File exists: src/validators/EmailValidator.ts
  ✓ File content matches pattern
  ✓ TypeScript compiles
  ✓ Tests pass (5/5)
  ✓ No linter errors

[VERIFIER] All checks passed ✓

[REVIEWER] Reviewing...
```

### Verification Failure

```
[VERIFIER] Verifying results...
  ✓ File exists: src/validators/EmailValidator.ts
  ✓ TypeScript compiles
  ✗ Tests fail (3/5)
      - testValidEmail: PASS
      - testInvalidEmail: FAIL
        Expected: false, Got: true
      - testEmptyEmail: FAIL
        Error: undefined is not a function
      - testNullEmail: PASS  
      - testMalformedEmail: FAIL
        Expected: false, Got: true

[VERIFIER] 3 checks failed

[ORCHESTRATOR] Cycling to Executor for retry
  Feedback: "Fix email validation for invalid formats"
```

### Configuration

```yaml
# .agent/config.yml
verifier:
  # Enable/disable check types
  checks:
    file_exists: true
    file_content: true
    compilation: true
    tests: true
    static_analysis: true
    llm_review: true
    
  # Timeouts
  test_timeout_seconds: 120
  compilation_timeout_seconds: 60
  llm_timeout_seconds: 30
  
  # Thresholds
  max_linter_warnings: 10
  require_test_pass: true
  
  # Tool configuration
  tools:
    typescript:
      command: "npx tsc --noEmit"
    tests:
      command: "npm test"
    linter:
      command: "npm run lint"
```

### Check Types

#### File Verification

```yaml
criteria:
  - type: file_exists
    path: src/validators/EmailValidator.ts
    
  - type: file_content
    path: src/validators/EmailValidator.ts
    contains:
      - "export class EmailValidator"
      - "isValid(email: string)"
```

#### Compilation

```yaml
criteria:
  - type: compilation
    language: typescript
    files:
      - src/validators/*.ts
```

#### Test Execution

```yaml
criteria:
  - type: tests
    command: npm test
    timeout: 120
    required: all_pass
```

#### Static Analysis

```yaml
criteria:
  - type: linter
    command: npm run lint
    max_errors: 0
    max_warnings: 10
```

#### LLM Verification

```yaml
criteria:
  - type: llm_review
    prompt: "Does this implementation properly validate email formats according to RFC 5322?"
```

### CLI Commands

```bash
# View verification status
$ acode verify status

Session: abc123
Last Verification:
  Time: 2 minutes ago
  Result: PASSED
  Checks: 5/5 passed

# Re-run verification
$ acode verify run

[VERIFIER] Running verification...
  ✓ All checks passed

# View verification details
$ acode verify details

Check Details:
  1. file_exists: PASS (12ms)
     Path: src/validators/EmailValidator.ts
     
  2. compilation: PASS (2.3s)
     Command: npx tsc --noEmit
     Output: Success
     
  3. tests: PASS (8.1s)
     Command: npm test
     Results: 5/5 passed
```

### Troubleshooting

#### Verification Takes Too Long

**Problem:** Tests or compilation times out

**Solutions:**
1. Increase timeout: `verifier.test_timeout_seconds: 180`
2. Run subset of tests
3. Optimize test suite

#### False Failures

**Problem:** Verification fails incorrectly

**Solutions:**
1. Review criteria for accuracy
2. Adjust thresholds
3. Exclude flaky tests

#### LLM Verification Unreliable

**Problem:** LLM gives inconsistent judgments

**Solutions:**
1. Prefer programmatic checks
2. Make criteria more specific
3. Increase LLM context

#### Missing Check Types

**Problem:** Need custom verification

**Solutions:**
1. Use command-based checks
2. Create test that validates condition
3. Request feature enhancement

---

## Acceptance Criteria

### Stage Lifecycle

- [ ] AC-001: IStage implemented
- [ ] AC-002: Results loaded
- [ ] AC-003: Verification runs
- [ ] AC-004: Status reported
- [ ] AC-005: Events logged

### Programmatic Checks

- [ ] AC-006: File exists works
- [ ] AC-007: File content works
- [ ] AC-008: Pattern matching works
- [ ] AC-009: Exit codes work

### Compilation

- [ ] AC-010: Build runs
- [ ] AC-011: Output captured
- [ ] AC-012: Errors parsed
- [ ] AC-013: Failure on errors

### Tests

- [ ] AC-014: Tests detected
- [ ] AC-015: Tests run
- [ ] AC-016: Output captured
- [ ] AC-017: Results parsed
- [ ] AC-018: Timeout works

### Static Analysis

- [ ] AC-019: Linters run
- [ ] AC-020: Output captured
- [ ] AC-021: Issues parsed
- [ ] AC-022: Thresholds work

### LLM Verification

- [ ] AC-023: Context presented
- [ ] AC-024: Judgment received
- [ ] AC-025: Response parsed
- [ ] AC-026: Reasoning included

### Results

- [ ] AC-027: Status included
- [ ] AC-028: Details included
- [ ] AC-029: Reasons on failure
- [ ] AC-030: Suggestions on failure

### Feedback

- [ ] AC-031: Generated on failure
- [ ] AC-032: Actionable
- [ ] AC-033: Flows to Executor

### Cycle

- [ ] AC-034: Pass advances
- [ ] AC-035: Fail triggers check
- [ ] AC-036: Cycle works
- [ ] AC-037: Limit enforced

### Performance

- [ ] AC-038: Programmatic < 100ms
- [ ] AC-039: Timeout enforced
- [ ] AC-040: Parallel works

### Persistence

- [ ] AC-041: Results saved
- [ ] AC-042: Details saved
- [ ] AC-043: Queryable

---

## Testing Requirements

### Unit Tests

```csharp
namespace AgenticCoder.Application.Tests.Unit.Orchestration.Stages.Verifier;

public class VerifierStageTests
{
    private readonly Mock<ICheckRunner> _mockCheckRunner;
    private readonly Mock<IFeedbackGenerator> _mockFeedbackGenerator;
    private readonly ILogger<VerifierStage> _logger;
    private readonly VerifierStage _verifier;
    
    public VerifierStageTests()
    {
        _mockCheckRunner = new Mock<ICheckRunner>();
        _mockFeedbackGenerator = new Mock<IFeedbackGenerator>();
        _logger = NullLogger<VerifierStage>.Instance;
        _verifier = new VerifierStage(_mockCheckRunner.Object, _mockFeedbackGenerator.Object, _logger);
    }
    
    [Fact]
    public async Task Should_Report_Pass_When_All_Checks_Pass()
    {
        // Arrange
        var stepResults = CreateSuccessfulStepResults(3);
        var options = new VerificationOptions();
        var allPassed = new List<CheckResult>
        {
            new CheckResult("FileExists", CheckStatus.Passed, "File found", null, TimeSpan.FromMilliseconds(20)),
            new CheckResult("Compilation", CheckStatus.Passed, "Compiled successfully", null, TimeSpan.FromSeconds(2)),
            new CheckResult("Tests", CheckStatus.Passed, "All tests passed", null, TimeSpan.FromSeconds(5))
        };
        
        _mockCheckRunner
            .Setup(r => r.RunAllAsync(It.IsAny<CheckContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allPassed);
        
        // Act
        var result = await _verifier.VerifyAsync(stepResults, options, CancellationToken.None);
        
        // Assert
        Assert.Equal(VerificationStatus.Passed, result.Status);
        Assert.Equal(3, result.CheckResults.Count);
        Assert.All(result.CheckResults, c => Assert.Equal(CheckStatus.Passed, c.Status));
        Assert.Null(result.Feedback); // No feedback on success
    }
    
    [Fact]
    public async Task Should_Report_Failure_And_Generate_Feedback()
    {
        // Arrange
        var stepResults = CreateSuccessfulStepResults(1);
        var options = new VerificationOptions();
        var withFailure = new List<CheckResult>
        {
            new CheckResult("FileExists", CheckStatus.Passed, "File found", null, TimeSpan.FromMilliseconds(20)),
            new CheckResult("Compilation", CheckStatus.Failed, "Syntax error on line 15", "Missing semicolon", TimeSpan.FromSeconds(2))
        };
        
        var feedback = new VerificationFeedback(
            Issues: new List<FeedbackIssue> { new("Compilation", "Missing semicolon", "Syntax error on line 15") },
            Suggestions: new List<string> { "Add semicolon at end of line 15" });
        
        _mockCheckRunner
            .Setup(r => r.RunAllAsync(It.IsAny<CheckContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(withFailure);
            
        _mockFeedbackGenerator
            .Setup(f => f.Generate(It.IsAny<IReadOnlyList<CheckResult>>()))
            .Returns(feedback);
        
        // Act
        var result = await _verifier.VerifyAsync(stepResults, options, CancellationToken.None);
        
        // Assert
        Assert.Equal(VerificationStatus.Failed, result.Status);
        Assert.Contains(result.CheckResults, c => c.Status == CheckStatus.Failed);
        Assert.NotNull(result.Feedback);
        Assert.Single(result.Feedback.Issues);
        Assert.Single(result.Feedback.Suggestions);
    }
    
    private static IReadOnlyList<StepResult> CreateSuccessfulStepResults(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => new StepResult(
                Status: StepStatus.Success,
                Output: $"Step {i} output",
                Message: "Success",
                TokensUsed: 100))
            .ToList();
    }
}

public class FileCheckTests
{
    private readonly FileExistsCheck _fileExistsCheck;
    private readonly FileContentCheck _fileContentCheck;
    private readonly string _testWorkspacePath;
    
    public FileCheckTests()
    {
        _testWorkspacePath = Path.Combine(Path.GetTempPath(), "test-workspace-" + Guid.NewGuid());
        Directory.CreateDirectory(_testWorkspacePath);
        _fileExistsCheck = new FileExistsCheck(NullLogger<FileExistsCheck>.Instance);
        _fileContentCheck = new FileContentCheck(NullLogger<FileContentCheck>.Instance);
    }
    
    [Fact]
    public async Task Should_Pass_When_File_Exists()
    {
        // Arrange
        var testFile = Path.Combine(_testWorkspacePath, "test.txt");
        File.WriteAllText(testFile, "test content");
        
        var context = new CheckContext(
            WorkspacePath: _testWorkspacePath,
            FilesToVerify: new[] { "test.txt" },
            ExpectedContent: null,
            AdditionalData: new Dictionary<string, object>());
        
        // Act
        var result = await _fileExistsCheck.RunAsync(context, CancellationToken.None);
        
        // Assert
        Assert.Equal(CheckStatus.Passed, result.Status);
        Assert.Contains("test.txt", result.Details);
    }
    
    [Fact]
    public async Task Should_Fail_When_File_Missing()
    {
        // Arrange
        var context = new CheckContext(
            WorkspacePath: _testWorkspacePath,
            FilesToVerify: new[] { "missing.txt" },
            ExpectedContent: null,
            AdditionalData: new Dictionary<string, object>());
        
        // Act
        var result = await _fileExistsCheck.RunAsync(context, CancellationToken.None);
        
        // Assert
        Assert.Equal(CheckStatus.Failed, result.Status);
        Assert.Contains("missing.txt", result.Reason);
    }
    
    [Fact]
    public async Task Should_Verify_Content_Matches_Pattern()
    {
        // Arrange
        var testFile = Path.Combine(_testWorkspacePath, "class.cs");
        File.WriteAllText(testFile, "public class UserService { }");
        
        var context = new CheckContext(
            WorkspacePath: _testWorkspacePath,
            FilesToVerify: new[] { "class.cs" },
            ExpectedContent: new Dictionary<string, string> { { "class.cs", "public class.*\\{" } },
            AdditionalData: new Dictionary<string, object>());
        
        // Act
        var result = await _fileContentCheck.RunAsync(context, CancellationToken.None);
        
        // Assert
        Assert.Equal(CheckStatus.Passed, result.Status);
    }
}

public class CompilationCheckTests
{
    private readonly Mock<IProcessRunner> _mockProcessRunner;
    private readonly CompilationCheck _compilationCheck;
    
    public CompilationCheckTests()
    {
        _mockProcessRunner = new Mock<IProcessRunner>();
        _compilationCheck = new CompilationCheck(_mockProcessRunner.Object, NullLogger<CompilationCheck>.Instance);
    }
    
    [Fact]
    public async Task Should_Pass_When_Compilation_Succeeds()
    {
        // Arrange
        var context = new CheckContext(
            WorkspacePath: "/workspace",
            FilesToVerify: Array.Empty<string>(),
            ExpectedContent: null,
            AdditionalData: new Dictionary<string, object> { { "BuildCommand", "dotnet build" } });
        
        _mockProcessRunner
            .Setup(p => p.RunAsync("dotnet", "build", "/workspace", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult(ExitCode: 0, Output: "Build succeeded", Error: ""));
        
        // Act
        var result = await _compilationCheck.RunAsync(context, CancellationToken.None);
        
        // Assert
        Assert.Equal(CheckStatus.Passed, result.Status);
        Assert.Contains("succeeded", result.Details, StringComparison.OrdinalIgnoreCase);
    }
    
    [Fact]
    public async Task Should_Fail_And_Parse_Errors_When_Compilation_Fails()
    {
        // Arrange
        var context = new CheckContext(
            WorkspacePath: "/workspace",
            FilesToVerify: Array.Empty<string>(),
            ExpectedContent: null,
            AdditionalData: new Dictionary<string, object> { { "BuildCommand", "dotnet build" } });
        
        var errorOutput = "error CS1002: ; expected [/workspace/file.cs(15)]";
        _mockProcessRunner
            .Setup(p => p.RunAsync("dotnet", "build", "/workspace", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult(ExitCode: 1, Output: "", Error: errorOutput));
        
        // Act
        var result = await _compilationCheck.RunAsync(context, CancellationToken.None);
        
        // Assert
        Assert.Equal(CheckStatus.Failed, result.Status);
        Assert.Contains("CS1002", result.Details);
        Assert.Contains("semicolon", result.Reason, StringComparison.OrdinalIgnoreCase);
    }
}
```

### Integration Tests

```csharp
namespace AgenticCoder.Application.Tests.Integration.Orchestration.Stages.Verifier;

public class VerifierIntegrationTests : IClassFixture<TestServerFixture>
{
    private readonly TestServerFixture _fixture;
    
    public VerifierIntegrationTests(TestServerFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task Should_Verify_Real_Files_In_Workspace()
    {
        // Arrange
        var verifier = _fixture.GetService<IVerifierStage>();
        var workspace = await _fixture.CreateTestWorkspaceAsync();
        await File.WriteAllTextAsync(Path.Combine(workspace.RootPath, "test.txt"), "content");
        
        var stepResults = new List<StepResult>
        {
            new StepResult(StepStatus.Success, "Created test.txt", "Success", 100)
        };
        var options = new VerificationOptions();
        
        // Act
        var result = await verifier.VerifyAsync(stepResults, options, CancellationToken.None);
        
        // Assert
        Assert.Equal(VerificationStatus.Passed, result.Status);
    }
}
```

### E2E Tests

```csharp
namespace AgenticCoder.Application.Tests.E2E.Orchestration.Stages.Verifier;

public class VerifierE2ETests : IClassFixture<E2ETestFixture>
{
    private readonly E2ETestFixture _fixture;
    
    public VerifierE2ETests(E2ETestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task Should_Pass_Good_Code_Through_All_Checks()
    {
        // Arrange
        var verifier = _fixture.GetService<IVerifierStage>();
        var workspace = await _fixture.CreateTestWorkspaceWithValidCodeAsync();
        var stepResults = await _fixture.ExecuteCodeGenerationStepsAsync(workspace);
        var options = new VerificationOptions();
        
        // Act
        var result = await verifier.VerifyAsync(stepResults, options, CancellationToken.None);
        
        // Assert
        Assert.Equal(VerificationStatus.Passed, result.Status);
        Assert.All(result.CheckResults, c => Assert.Equal(CheckStatus.Passed, c.Status));
    }
}
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| File check | 20ms | 100ms |
| Compilation | 5s | 60s |
| Test run | 30s | 120s |
| LLM verification | 10s | 30s |

---

## User Verification Steps

### Scenario 1: All Pass

1. Complete good implementation
2. Run verification
3. Verify: All checks pass
4. Verify: Advances to Reviewer

### Scenario 2: File Missing

1. Remove expected file
2. Run verification
3. Verify: File check fails
4. Verify: Feedback generated

### Scenario 3: Compilation Fails

1. Introduce syntax error
2. Run verification
3. Verify: Compilation fails
4. Verify: Error details shown

### Scenario 4: Test Fails

1. Break test condition
2. Run verification
3. Verify: Test failure detected
4. Verify: Test output shown

### Scenario 5: Cycle Triggered

1. Cause verification failure
2. Observe: Cycle to Executor
3. Verify: Feedback provided
4. Verify: Retry occurs

### Scenario 6: Timeout

1. Create slow test
2. Configure short timeout
3. Verify: Timeout triggers
4. Verify: Handled gracefully

### Scenario 7: LLM Verification

1. Add fuzzy criteria
2. Run verification
3. Verify: LLM consulted
4. Verify: Reasoning shown

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Application/
├── Orchestration/
│   └── Stages/
│       └── Verifier/
│           ├── IVerifierStage.cs
│           ├── VerifierStage.cs
│           ├── CheckRunner.cs
│           ├── FeedbackGenerator.cs
│           └── Checks/
│               ├── ICheck.cs
│               ├── FileExistsCheck.cs
│               ├── FileContentCheck.cs
│               ├── CompilationCheck.cs
│               ├── TestRunCheck.cs
│               ├── LinterCheck.cs
│               └── LlmVerificationCheck.cs
```

### IVerifierStage Interface

```csharp
namespace AgenticCoder.Application.Orchestration.Stages.Verifier;

public interface IVerifierStage : IStage
{
    Task<VerificationResult> VerifyAsync(
        IReadOnlyList<StepResult> stepResults,
        VerificationOptions options,
        CancellationToken ct);
}

public sealed record VerificationResult(
    VerificationStatus Status,
    IReadOnlyList<CheckResult> CheckResults,
    VerificationFeedback? Feedback);

public enum VerificationStatus
{
    Passed,
    Failed,
    PartiallyPassed,
    Timeout
}
```

### ICheck Interface

```csharp
namespace AgenticCoder.Application.Orchestration.Stages.Verifier.Checks;

public interface ICheck
{
    string Name { get; }
    CheckType Type { get; }
    Task<CheckResult> RunAsync(CheckContext context, CancellationToken ct);
}

public sealed record CheckResult(
    string CheckName,
    CheckStatus Status,
    string? Details,
    string? Reason,
    TimeSpan Duration);

public enum CheckStatus
{
    Passed,
    Failed,
    Skipped,
    Timeout
}
```

### VerifierStage Complete Implementation

```csharp
namespace AgenticCoder.Application.Orchestration.Stages.Verifier;

public sealed class VerifierStage : StageBase, IVerifierStage
{
    private readonly ICheckRunner _checkRunner;
    private readonly IFeedbackGenerator _feedbackGenerator;
    private readonly ILogger<VerifierStage> _logger;
    
    public override StageType Type => StageType.Verifier;
    
    public VerifierStage(
        ICheckRunner checkRunner,
        IFeedbackGenerator feedbackGenerator,
        ILogger<VerifierStage> logger) : base(logger)
    {
        _checkRunner = checkRunner ?? throw new ArgumentNullException(nameof(checkRunner));
        _feedbackGenerator = feedbackGenerator ?? throw new ArgumentNullException(nameof(feedbackGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    protected override async Task<StageResult> ExecuteStageAsync(
        StageContext context,
        CancellationToken ct)
    {
        var stepResults = (IReadOnlyList<StepResult>)context.StageData["step_results"];
        var options = new VerificationOptions();
        
        var verificationResult = await VerifyAsync(stepResults, options, ct);
        
        return new StageResult(
            Status: verificationResult.Status == VerificationStatus.Passed ? StageStatus.Success : StageStatus.Cycle,
            Output: verificationResult,
            NextStage: verificationResult.Status == VerificationStatus.Passed ? StageType.Reviewer : StageType.Executor,
            Message: $"Verification {verificationResult.Status}: {verificationResult.CheckResults.Count} checks",
            Metrics: new StageMetrics(StageType.Verifier, TimeSpan.Zero, 0));
    }
    
    public async Task<VerificationResult> VerifyAsync(
        IReadOnlyList<StepResult> stepResults,
        VerificationOptions options,
        CancellationToken ct)
    {
        _logger.LogInformation("Verifying {StepCount} steps", stepResults.Count);
        
        var checkContext = new CheckContext(
            WorkspacePath: options.WorkspacePath,
            FilesToVerify: ExtractFilesFromStepResults(stepResults),
            ExpectedContent: null,
            AdditionalData: new Dictionary<string, object> { { "StepResults", stepResults } });
        
        var checkResults = await _checkRunner.RunAllAsync(checkContext, ct);
        
        var allPassed = checkResults.All(c => c.Status == CheckStatus.Passed);
        var status = allPassed ? VerificationStatus.Passed : VerificationStatus.Failed;
        
        VerificationFeedback? feedback = null;
        if (!allPassed)
        {
            feedback = _feedbackGenerator.Generate(checkResults);
            _logger.LogWarning("Verification failed: {IssueCount} issues found", feedback.Issues.Count);
        }
        
        return new VerificationResult(status, checkResults, feedback);
    }
    
    private static string[] ExtractFilesFromStepResults(IReadOnlyList<StepResult> stepResults)
    {
        return stepResults
            .Where(r => r.Output != null && r.Output.ToString().Contains("file"))
            .Select(r => ExtractFileName(r.Output.ToString()))
            .Where(f => !string.IsNullOrEmpty(f))
            .ToArray();
    }
    
    private static string ExtractFileName(string output)
    {
        // Simple extraction logic - in real implementation would be more sophisticated
        var match = Regex.Match(output, @"\bfile:\s*([\w/.\-]+)");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}
```

### CheckRunner Implementation

```csharp
namespace AgenticCoder.Application.Orchestration.Stages.Verifier;

public interface ICheckRunner
{
    Task<IReadOnlyList<CheckResult>> RunAllAsync(CheckContext context, CancellationToken ct);
}

public sealed class CheckRunner : ICheckRunner
{
    private readonly IEnumerable<ICheck> _checks;
    private readonly ILogger<CheckRunner> _logger;
    
    public CheckRunner(IEnumerable<ICheck> checks, ILogger<CheckRunner> logger)
    {
        _checks = checks ?? throw new ArgumentNullException(nameof(checks));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<IReadOnlyList<CheckResult>> RunAllAsync(CheckContext context, CancellationToken ct)
    {
        _logger.LogInformation("Running {CheckCount} verification checks", _checks.Count());
        
        var results = new List<CheckResult>();
        
        // Run checks in parallel for better performance
        var tasks = _checks.Select(check => RunCheckAsync(check, context, ct));
        var checkResults = await Task.WhenAll(tasks);
        
        results.AddRange(checkResults);
        
        var passedCount = results.Count(r => r.Status == CheckStatus.Passed);
        var failedCount = results.Count(r => r.Status == CheckStatus.Failed);
        
        _logger.LogInformation("Checks complete: {Passed} passed, {Failed} failed",
            passedCount, failedCount);
        
        return results.AsReadOnly();
    }
    
    private async Task<CheckResult> RunCheckAsync(ICheck check, CheckContext context, CancellationToken ct)
    {
        _logger.LogDebug("Running check: {CheckName}", check.Name);
        
        try
        {
            return await check.RunAsync(context, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check {CheckName} threw exception", check.Name);
            return new CheckResult(
                CheckName: check.Name,
                Status: CheckStatus.Failed,
                Details: null,
                Reason: $"Check threw exception: {ex.Message}",
                Duration: TimeSpan.Zero);
        }
    }
}
```

### FeedbackGenerator Complete Implementation

```csharp
namespace AgenticCoder.Application.Orchestration.Stages.Verifier;

public interface IFeedbackGenerator
{
    VerificationFeedback Generate(IReadOnlyList<CheckResult> checkResults);
}

public sealed class FeedbackGenerator : IFeedbackGenerator
{
    private readonly ILogger<FeedbackGenerator> _logger;
    
    public FeedbackGenerator(ILogger<FeedbackGenerator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public VerificationFeedback Generate(IReadOnlyList<CheckResult> checkResults)
    {
        _logger.LogInformation("Generating feedback for {FailureCount} failures",
            checkResults.Count(c => c.Status == CheckStatus.Failed));
        
        var issues = checkResults
            .Where(c => c.Status == CheckStatus.Failed)
            .Select(c => new FeedbackIssue(
                CheckName: c.CheckName,
                Reason: c.Reason ?? "Check failed",
                Details: c.Details))
            .ToList();
        
        var suggestions = GenerateSuggestions(issues);
        
        return new VerificationFeedback(issues.AsReadOnly(), suggestions.AsReadOnly());
    }
    
    private List<string> GenerateSuggestions(List<FeedbackIssue> issues)
    {
        var suggestions = new List<string>();
        
        foreach (var issue in issues)
        {
            var suggestion = issue.CheckName switch
            {
                "FileExists" => $"Create the missing file: {issue.Details}",
                "Compilation" => $"Fix compilation error: {issue.Reason}",
                "Tests" => $"Fix failing test: {issue.Details}",
                "LintErrors" => $"Address linter issues: {issue.Reason}",
                _ => $"Resolve issue in {issue.CheckName}: {issue.Reason}"
            };
            
            suggestions.Add(suggestion);
        }
        
        return suggestions;
    }
}

public sealed record VerificationFeedback(
    IReadOnlyList<FeedbackIssue> Issues,
    IReadOnlyList<string> Suggestions);

public sealed record FeedbackIssue(
    string CheckName,
    string Reason,
    string? Details);
```

### FileExistsCheck Implementation

```csharp
namespace AgenticCoder.Application.Orchestration.Stages.Verifier.Checks;

public sealed class FileExistsCheck : ICheck
{
    private readonly ILogger<FileExistsCheck> _logger;
    
    public string Name => "FileExists";
    public CheckType Type => CheckType.Programmatic;
    
    public FileExistsCheck(ILogger<FileExistsCheck> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public Task<CheckResult> RunAsync(CheckContext context, CancellationToken ct)
    {
        var startTime = DateTimeOffset.UtcNow;
        _logger.LogInformation("Checking existence of {FileCount} files", context.FilesToVerify.Length);
        
        var missingFiles = new List<string>();
        
        foreach (var file in context.FilesToVerify)
        {
            var fullPath = Path.Combine(context.WorkspacePath, file);
            if (!File.Exists(fullPath))
            {
                missingFiles.Add(file);
            }
        }
        
        var duration = DateTimeOffset.UtcNow - startTime;
        
        if (missingFiles.Any())
        {
            return Task.FromResult(new CheckResult(
                CheckName: Name,
                Status: CheckStatus.Failed,
                Details: string.Join(", ", missingFiles),
                Reason: $"{missingFiles.Count} file(s) not found",
                Duration: duration));
        }
        
        return Task.FromResult(new CheckResult(
            CheckName: Name,
            Status: CheckStatus.Passed,
            Details: $"All {context.FilesToVerify.Length} files exist",
            Reason: null,
            Duration: duration));
    }
}
```

### CompilationCheck Implementation

```csharp
namespace AgenticCoder.Application.Orchestration.Stages.Verifier.Checks;

public sealed class CompilationCheck : ICheck
{
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<CompilationCheck> _logger;
    
    public string Name => "Compilation";
    public CheckType Type => CheckType.Programmatic;
    
    public CompilationCheck(IProcessRunner processRunner, ILogger<CompilationCheck> logger)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<CheckResult> RunAsync(CheckContext context, CancellationToken ct)
    {
        var startTime = DateTimeOffset.UtcNow;
        _logger.LogInformation("Running compilation check in {WorkspacePath}", context.WorkspacePath);
        
        var buildCommand = context.AdditionalData.GetValueOrDefault("BuildCommand", "dotnet build").ToString();
        var parts = buildCommand.Split(' ', 2);
        var command = parts[0];
        var args = parts.Length > 1 ? parts[1] : string.Empty;
        
        try
        {
            var result = await _processRunner.RunAsync(command, args, context.WorkspacePath, ct);
            var duration = DateTimeOffset.UtcNow - startTime;
            
            if (result.ExitCode == 0)
            {
                return new CheckResult(
                    CheckName: Name,
                    Status: CheckStatus.Passed,
                    Details: "Compilation succeeded",
                    Reason: null,
                    Duration: duration);
            }
            
            // Parse error output
            var errors = ParseCompilationErrors(result.Error + result.Output);
            
            return new CheckResult(
                CheckName: Name,
                Status: CheckStatus.Failed,
                Details: string.Join("; ", errors.Take(3)), // First 3 errors
                Reason: $"{errors.Count} compilation error(s)",
                Duration: duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Compilation check failed");
            return new CheckResult(
                CheckName: Name,
                Status: CheckStatus.Failed,
                Details: null,
                Reason: $"Compilation failed: {ex.Message}",
                Duration: DateTimeOffset.UtcNow - startTime);
        }
    }
    
    private List<string> ParseCompilationErrors(string output)
    {
        // Parse error format: "error CS1002: ; expected [/path/file.cs(15)]"
        var errorRegex = new Regex(@"error\s+([A-Z]{2}\d{4}):\s+(.+?)\s*\[", RegexOptions.IgnoreCase);
        var matches = errorRegex.Matches(output);
        
        return matches.Select(m => $"{m.Groups[1].Value}: {m.Groups[2].Value}").ToList();
    }
}

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(string command, string args, string workingDirectory, CancellationToken ct);
}

public sealed record ProcessResult(int ExitCode, string Output, string Error);
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-VERIF-001 | Verification failed |
| ACODE-VERIF-002 | Check timeout |
| ACODE-VERIF-003 | Compilation failed |
| ACODE-VERIF-004 | Tests failed |
| ACODE-VERIF-005 | Linter threshold exceeded |
| ACODE-VERIF-006 | LLM verification failed |

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Verification passed |
| 1 | General failure |
| 40 | Check failed |
| 41 | Compilation failed |
| 42 | Tests failed |
| 43 | Verification timeout |

### Logging Fields

```json
{
  "event": "verification_check",
  "session_id": "abc123",
  "step_id": "step_1",
  "check_name": "compilation",
  "check_type": "programmatic",
  "status": "passed",
  "duration_ms": 2340,
  "details": "TypeScript compiled successfully"
}
```

### Implementation Checklist

1. [ ] Create IVerifierStage interface
2. [ ] Implement VerifierStage
3. [ ] Create ICheck interface
4. [ ] Implement FileExistsCheck
5. [ ] Implement FileContentCheck
6. [ ] Implement CompilationCheck
7. [ ] Implement TestRunCheck
8. [ ] Implement LinterCheck
9. [ ] Implement LlmVerificationCheck
10. [ ] Create CheckRunner
11. [ ] Implement parallel execution
12. [ ] Create FeedbackGenerator
13. [ ] Implement cycle decision
14. [ ] Add persistence
15. [ ] Write unit tests
16. [ ] Write integration tests
17. [ ] Write E2E tests

### Validation Checklist Before Merge

- [ ] All check types work
- [ ] Compilation checking works
- [ ] Test running works
- [ ] Feedback generated
- [ ] Cycle triggers correctly
- [ ] Timeouts work
- [ ] Results persisted
- [ ] Unit test coverage > 90%

### Rollout Plan

1. **Phase 1:** Check interface
2. **Phase 2:** File checks
3. **Phase 3:** Compilation check
4. **Phase 4:** Test runner
5. **Phase 5:** Static analysis
6. **Phase 6:** LLM verification
7. **Phase 7:** Feedback generation
8. **Phase 8:** Integration

---

**End of Task 012.c Specification**