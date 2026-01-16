# Task-012c Completion Checklist: Verifier Stage

**Status:** ✅ READY FOR IMPLEMENTATION (AFTER TASK-012A COMPLETE)

**Semantic Completeness:** 0% (0/43 ACs)

**Expected Completion Time:** 15-22 hours (6 phases)

**Implementation Approach:** Test-Driven Development (TDD) - RED → GREEN → REFACTOR

---

## INSTRUCTIONS FOR IMPLEMENTING AGENT

This document is your implementation roadmap. Each phase contains everything you need to implement that component. Do NOT read the full spec unless you need clarification on a specific gap. Each gap includes:

- **Current State**: ❌ MISSING / ⚠️ INCOMPLETE / ✅ COMPLETE
- **Spec Reference**: Line numbers in the spec file
- **What Exists**: Description of current implementation
- **What's Missing**: Detailed gap description
- **Implementation Details**: Code examples directly from spec (ready to copy/paste)
- **Acceptance Criteria Covered**: Which ACs this gap addresses
- **Test Requirements**: Specific tests you need to write
- **Success Criteria**: How to verify completion
- **Gap Checklist Item**: Checkbox to mark when done

**Implementation Rules:**
1. Work through gaps sequentially (top to bottom)
2. Follow TDD: Write test first (RED), implement (GREEN), refactor (CLEAN)
3. Copy code examples from Implementation Details section
4. Mark each gap [✅] when complete with tests passing
5. Run `dotnet test` after each phase to verify
6. Commit after each complete phase
7. If context runs low, save progress and mark last completed phase

**Critical:** Do NOT skip foundational reading before starting. Read CLAUDE.md Section 3.2 and GAP_ANALYSIS_METHODOLOGY.md completely.

---

# PHASE 1: CHECK INFRASTRUCTURE (2-3 hours)

## Gap 1.1: ICheck Interface

- **Current State:** ❌ MISSING
- **Spec Reference:** Lines 990-1000 (Implementation Prompt)
- **What Exists:** Nothing - check infrastructure doesn't exist
- **What's Missing:** ICheck interface that all verification checks must implement
- **Acceptance Criteria Covered:** AC-006 through AC-022 (all programmatic checks depend on this interface)

### Implementation Details (from spec lines 990-1000)

```csharp
namespace Acode.Application.Orchestration.Stages.Verifier.Checks;

public interface ICheck
{
    string Name { get; }
    CheckType Type { get; }
    Task<CheckResult> RunAsync(CheckContext context, CancellationToken ct);
}

public enum CheckType
{
    Programmatic,
    Compilation,
    TestExecution,
    StaticAnalysis,
    LlmVerification
}

public sealed record CheckResult(
    string CheckName,
    CheckStatus Status,
    string? Details,
    string? Reason,
    TimeSpan Duration);

public sealed record CheckContext(
    string WorkspacePath,
    string[] FilesToVerify,
    Dictionary<string, string>? ExpectedContent,
    Dictionary<string, object> AdditionalData);

public enum CheckStatus
{
    Passed,
    Failed,
    Skipped,
    Timeout
}
```

### Test Requirements

```csharp
// Test file: tests/Acode.Application.Tests/Orchestration/Stages/Verifier/Checks/CheckInterfaceTests.cs
namespace Acode.Application.Tests.Orchestration.Stages.Verifier.Checks;

public class CheckInterfaceTests
{
    [Fact]
    public void ICheck_Should_Have_Required_Properties()
    {
        // Verify ICheck interface exists and has Name, Type properties
        // Verify CheckResult record exists with correct fields
        // Verify CheckStatus enum exists
        // Verify CheckContext record exists
    }
}
```

### Success Criteria

- [ ] ICheck interface created in Checks subdirectory
- [ ] CheckType enum created with 5 values
- [ ] CheckResult record created with all fields (CheckName, Status, Details, Reason, Duration)
- [ ] CheckContext record created with all fields
- [ ] CheckStatus enum created with 4 values
- [ ] No NotImplementedException in any file
- [ ] Compiles without errors/warnings
- [ ] Test file created and passing

### Gap Checklist Item

- [ ] 🔄 **Gap 1.1: ICheck Interface** - Implementation complete with tests passing

---

## Gap 1.2: FileExistsCheck Implementation

- **Current State:** ❌ MISSING
- **Spec Reference:** Lines 1237-1289 (Implementation Prompt)
- **What Exists:** Nothing
- **What's Missing:** FileExistsCheck class that verifies expected files exist in workspace
- **Acceptance Criteria Covered:** AC-006 (File exists works)

### Implementation Details (from spec lines 1237-1289)

```csharp
namespace Acode.Application.Orchestration.Stages.Verifier.Checks;

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

### Test Requirements

```csharp
// From spec lines 687-706 (FileCheckTests)
[Fact]
public async Task Should_Pass_When_File_Exists()
{
    // Arrange: Create test file
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
```

### Success Criteria

- [ ] FileExistsCheck class created
- [ ] Implements ICheck interface
- [ ] Name property returns "FileExists"
- [ ] Type property returns CheckType.Programmatic
- [ ] RunAsync checks all files in FilesToVerify
- [ ] Returns Passed status when all files exist
- [ ] Returns Failed status when any file missing
- [ ] Includes file names in Details/Reason
- [ ] Measures execution time in Duration
- [ ] Execution time < 100ms (AC-038: Programmatic < 100ms)
- [ ] Tests pass (both scenarios)
- [ ] No NotImplementedException

### Gap Checklist Item

- [ ] 🔄 **Gap 1.2: FileExistsCheck** - Implementation complete with tests passing

---

## Gap 1.3: FileContentCheck Implementation

- **Current State:** ❌ MISSING
- **Spec Reference:** Lines 729-744 (Testing Requirements - test code)
- **What Exists:** Nothing
- **What's Missing:** FileContentCheck class that verifies file content matches expected patterns
- **Acceptance Criteria Covered:** AC-007 (File content works), AC-008 (Pattern matching works)

### Implementation Details (from spec, reconstructed from test code and description)

```csharp
namespace Acode.Application.Orchestration.Stages.Verifier.Checks;

public sealed class FileContentCheck : ICheck
{
    private readonly ILogger<FileContentCheck> _logger;

    public string Name => "FileContent";
    public CheckType Type => CheckType.Programmatic;

    public FileContentCheck(ILogger<FileContentCheck> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<CheckResult> RunAsync(CheckContext context, CancellationToken ct)
    {
        var startTime = DateTimeOffset.UtcNow;
        _logger.LogInformation("Checking content of files against patterns");

        if (context.ExpectedContent == null || context.ExpectedContent.Count == 0)
        {
            // No content checks configured
            return Task.FromResult(new CheckResult(
                CheckName: Name,
                Status: CheckStatus.Skipped,
                Details: "No content checks configured",
                Reason: null,
                Duration: DateTimeOffset.UtcNow - startTime));
        }

        var mismatches = new List<string>();

        foreach (var (filePath, expectedPattern) in context.ExpectedContent)
        {
            var fullPath = Path.Combine(context.WorkspacePath, filePath);

            if (!File.Exists(fullPath))
            {
                mismatches.Add($"File {filePath} not found");
                continue;
            }

            try
            {
                var content = File.ReadAllText(fullPath);
                var regex = new Regex(expectedPattern);

                if (!regex.IsMatch(content))
                {
                    mismatches.Add($"File {filePath} content doesn't match pattern");
                }
            }
            catch (Exception ex)
            {
                mismatches.Add($"Error checking {filePath}: {ex.Message}");
            }
        }

        var duration = DateTimeOffset.UtcNow - startTime;

        if (mismatches.Any())
        {
            return Task.FromResult(new CheckResult(
                CheckName: Name,
                Status: CheckStatus.Failed,
                Details: string.Join("; ", mismatches),
                Reason: $"{mismatches.Count} content mismatch(es)",
                Duration: duration));
        }

        return Task.FromResult(new CheckResult(
            CheckName: Name,
            Status: CheckStatus.Passed,
            Details: $"All {context.ExpectedContent.Count} files match patterns",
            Reason: null,
            Duration: duration));
    }
}
```

### Test Requirements

```csharp
// From spec lines 726-744
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
```

### Success Criteria

- [ ] FileContentCheck class created
- [ ] Implements ICheck interface
- [ ] Name property returns "FileContent"
- [ ] Type property returns CheckType.Programmatic
- [ ] RunAsync reads file content
- [ ] Uses Regex.IsMatch for pattern matching
- [ ] Returns Passed when all patterns match
- [ ] Returns Failed when any pattern doesn't match
- [ ] Returns Skipped when no content checks configured
- [ ] Handles file not found gracefully
- [ ] Test passes
- [ ] Execution time < 100ms

### Gap Checklist Item

- [ ] 🔄 **Gap 1.3: FileContentCheck** - Implementation complete with tests passing

---

# PHASE 2: COMPILATION & TESTS (3-4 hours)

## Gap 2.1: CompilationCheck Implementation

- **Current State:** ❌ MISSING
- **Spec Reference:** Lines 1293-1374 (Implementation Prompt)
- **What Exists:** Nothing
- **What's Missing:** CompilationCheck class that runs build commands and parses errors
- **Acceptance Criteria Covered:** AC-010 (Build runs), AC-011 (Output captured), AC-012 (Errors parsed), AC-013 (Failure on errors)

### Implementation Details (from spec lines 1293-1374)

```csharp
namespace Acode.Application.Orchestration.Stages.Verifier.Checks;

public sealed class CompilationCheck : ICheck
{
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<CompilationCheck> _logger;

    public string Name => "Compilation";
    public CheckType Type => CheckType.Compilation;

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

### Test Requirements

```csharp
// From spec lines 758-802
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
```

### Success Criteria

- [ ] CompilationCheck class created
- [ ] Implements ICheck interface
- [ ] Name property returns "Compilation"
- [ ] Type property returns CheckType.Compilation
- [ ] RunAsync runs build command via IProcessRunner
- [ ] ParseCompilationErrors extracts error codes and messages
- [ ] Returns Passed when exit code is 0
- [ ] Returns Failed when exit code != 0
- [ ] Includes error details in Details field
- [ ] Parses error format correctly (regex matches CS1002: message format)
- [ ] Test mocks IProcessRunner successfully
- [ ] Both test scenarios pass
- [ ] No NotImplementedException

### Gap Checklist Item

- [ ] 🔄 **Gap 2.1: CompilationCheck** - Implementation complete with tests passing

---

## Gap 2.2: TestRunCheck Implementation

- **Current State:** ❌ MISSING
- **Spec Reference:** Lines 1450-1460 (overview), spec lines 154-161 (functional requirements)
- **What Exists:** Nothing
- **What's Missing:** TestRunCheck class that detects and runs test suites, parses results, and handles timeouts
- **Acceptance Criteria Covered:** AC-014 (Tests detected), AC-015 (Tests run), AC-016 (Output captured), AC-017 (Results parsed), AC-018 (Timeout works)

### Implementation Details (from spec pattern and requirements)

```csharp
namespace Acode.Application.Orchestration.Stages.Verifier.Checks;

public sealed class TestRunCheck : ICheck
{
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<TestRunCheck> _logger;
    private readonly int _timeoutSeconds;

    public string Name => "Tests";
    public CheckType Type => CheckType.TestExecution;

    public TestRunCheck(IProcessRunner processRunner, ILogger<TestRunCheck> logger, int timeoutSeconds = 120)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeoutSeconds = timeoutSeconds;
    }

    public async Task<CheckResult> RunAsync(CheckContext context, CancellationToken ct)
    {
        var startTime = DateTimeOffset.UtcNow;
        _logger.LogInformation("Detecting test files in {WorkspacePath}", context.WorkspacePath);

        // Detect test command (npm test, dotnet test, pytest, etc.)
        var testCommand = DetectTestCommand(context.WorkspacePath);

        if (string.IsNullOrEmpty(testCommand))
        {
            _logger.LogInformation("No test command detected");
            return new CheckResult(
                CheckName: Name,
                Status: CheckStatus.Skipped,
                Details: "No test suite detected",
                Reason: null,
                Duration: DateTimeOffset.UtcNow - startTime);
        }

        _logger.LogInformation("Running tests with command: {TestCommand}", testCommand);

        try
        {
            var parts = testCommand.Split(' ', 2);
            var command = parts[0];
            var args = parts.Length > 1 ? parts[1] : string.Empty;

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

                var result = await _processRunner.RunAsync(command, args, context.WorkspacePath, cts.Token);
                var duration = DateTimeOffset.UtcNow - startTime;

                if (result.ExitCode == 0)
                {
                    var testCount = ParseTestCount(result.Output);
                    return new CheckResult(
                        CheckName: Name,
                        Status: CheckStatus.Passed,
                        Details: $"All tests passed ({testCount} tests)",
                        Reason: null,
                        Duration: duration);
                }

                var failures = ParseTestFailures(result.Output + result.Error);
                return new CheckResult(
                    CheckName: Name,
                    Status: CheckStatus.Failed,
                    Details: string.Join("; ", failures.Take(3)),
                    Reason: $"{failures.Count} test(s) failed",
                    Duration: duration);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Test run timeout exceeded ({TimeoutSeconds}s)", _timeoutSeconds);
            return new CheckResult(
                CheckName: Name,
                Status: CheckStatus.Timeout,
                Details: null,
                Reason: $"Tests timed out after {_timeoutSeconds}s",
                Duration: DateTimeOffset.UtcNow - startTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test run failed");
            return new CheckResult(
                CheckName: Name,
                Status: CheckStatus.Failed,
                Details: null,
                Reason: $"Test execution failed: {ex.Message}",
                Duration: DateTimeOffset.UtcNow - startTime);
        }
    }

    private string DetectTestCommand(string workspacePath)
    {
        // Detect test runner based on files in workspace
        if (File.Exists(Path.Combine(workspacePath, "package.json")))
            return "npm test";

        if (File.Exists(Path.Combine(workspacePath, "*.csproj")))
            return "dotnet test";

        if (File.Exists(Path.Combine(workspacePath, "pytest.ini")) ||
            File.Exists(Path.Combine(workspacePath, "pyproject.toml")))
            return "pytest";

        return null;
    }

    private int ParseTestCount(string output)
    {
        // Simple pattern matching - adjust for actual test output format
        var match = Regex.Match(output, @"(\d+)\s+passed");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    private List<string> ParseTestFailures(string output)
    {
        var failures = new List<string>();
        var lines = output.Split('\n');

        foreach (var line in lines)
        {
            if (line.Contains("FAILED") || line.Contains("Error") || line.Contains("failed"))
            {
                failures.Add(line.Trim());
            }
        }

        return failures;
    }
}
```

### Test Requirements

From spec lines 154-161 (FR-027 through FR-032):

```csharp
// test file: tests/Acode.Application.Tests/Orchestration/Stages/Verifier/Checks/TestRunCheckTests.cs
[Fact]
public async Task Should_Detect_And_Run_Tests()
{
    // Should detect test command from workspace
    // Should run test command
    // Should parse test output
    // Should return passed when tests pass
}

[Fact]
public async Task Should_Fail_When_Tests_Fail()
{
    // Should detect test failures
    // Should return Failed status
    // Should include failure details
}

[Fact]
public async Task Should_Timeout_After_Limit()
{
    // Should timeout when tests take too long
    // Should return Timeout status
    // Should include timeout reason
}
```

### Success Criteria

- [ ] TestRunCheck class created
- [ ] Implements ICheck interface
- [ ] Name property returns "Tests"
- [ ] Type property returns CheckType.TestExecution
- [ ] DetectTestCommand identifies test runner (npm, dotnet, pytest)
- [ ] RunAsync executes test command
- [ ] ParseTestCount extracts number of passing tests
- [ ] ParseTestFailures identifies failed tests
- [ ] Returns Passed when exit code is 0
- [ ] Returns Failed when tests fail
- [ ] Returns Skipped when no test suite detected
- [ ] Returns Timeout when tests exceed timeout
- [ ] Timeout is configurable (default 120 seconds)
- [ ] Tests pass all three scenarios

### Gap Checklist Item

- [ ] 🔄 **Gap 2.2: TestRunCheck** - Implementation complete with tests passing

---

# PHASE 3: STATIC ANALYSIS (1-2 hours)

## Gap 3.1: LinterCheck Implementation

- **Current State:** ❌ MISSING
- **Spec Reference:** Lines 163-170 (Functional Requirements FR-033-037)
- **What Exists:** Nothing
- **What's Missing:** LinterCheck class that runs configured linters and enforces thresholds
- **Acceptance Criteria Covered:** AC-019 (Linters run), AC-020 (Output captured), AC-021 (Issues parsed), AC-022 (Thresholds work)

### Implementation Details (from spec pattern)

```csharp
namespace Acode.Application.Orchestration.Stages.Verifier.Checks;

public sealed class LinterCheck : ICheck
{
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<LinterCheck> _logger;
    private readonly int _maxWarnings;
    private readonly int _maxErrors;

    public string Name => "Linter";
    public CheckType Type => CheckType.StaticAnalysis;

    public LinterCheck(
        IProcessRunner processRunner,
        ILogger<LinterCheck> logger,
        int maxWarnings = 10,
        int maxErrors = 0)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxWarnings = maxWarnings;
        _maxErrors = maxErrors;
    }

    public async Task<CheckResult> RunAsync(CheckContext context, CancellationToken ct)
    {
        var startTime = DateTimeOffset.UtcNow;
        _logger.LogInformation("Running linter check in {WorkspacePath}", context.WorkspacePath);

        var linterCommand = context.AdditionalData.GetValueOrDefault("LinterCommand", "npm run lint").ToString();
        var parts = linterCommand.Split(' ', 2);
        var command = parts[0];
        var args = parts.Length > 1 ? parts[1] : string.Empty;

        try
        {
            var result = await _processRunner.RunAsync(command, args, context.WorkspacePath, ct);
            var duration = DateTimeOffset.UtcNow - startTime;

            var (errorCount, warningCount) = ParseLinterOutput(result.Output + result.Error);

            _logger.LogInformation("Linter found {ErrorCount} errors and {WarningCount} warnings", errorCount, warningCount);

            if (errorCount > _maxErrors || warningCount > _maxWarnings)
            {
                return new CheckResult(
                    CheckName: Name,
                    Status: CheckStatus.Failed,
                    Details: $"{errorCount} errors, {warningCount} warnings",
                    Reason: $"Exceeded threshold (max: {_maxErrors} errors, {_maxWarnings} warnings)",
                    Duration: duration);
            }

            return new CheckResult(
                CheckName: Name,
                Status: CheckStatus.Passed,
                Details: $"{errorCount} errors, {warningCount} warnings (within threshold)",
                Reason: null,
                Duration: duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Linter check failed");
            return new CheckResult(
                CheckName: Name,
                Status: CheckStatus.Failed,
                Details: null,
                Reason: $"Linter execution failed: {ex.Message}",
                Duration: DateTimeOffset.UtcNow - startTime);
        }
    }

    private (int errors, int warnings) ParseLinterOutput(string output)
    {
        var errorCount = 0;
        var warningCount = 0;

        var lines = output.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("error", StringComparison.OrdinalIgnoreCase))
                errorCount++;
            else if (line.Contains("warning", StringComparison.OrdinalIgnoreCase))
                warningCount++;
        }

        return (errorCount, warningCount);
    }
}
```

### Test Requirements

```csharp
[Fact]
public async Task Should_Pass_When_Issues_Below_Threshold()
{
    // Verify linter runs with configured command
    // Verify issues are counted
    // Verify passes when below threshold
}

[Fact]
public async Task Should_Fail_When_Issues_Exceed_Threshold()
{
    // Verify fails when errors exceed max
    // Verify fails when warnings exceed max
}
```

### Success Criteria

- [ ] LinterCheck class created
- [ ] Implements ICheck interface
- [ ] Name property returns "Linter"
- [ ] Type property returns CheckType.StaticAnalysis
- [ ] RunAsync runs configured linter command
- [ ] ParseLinterOutput counts errors and warnings
- [ ] Returns Passed when within thresholds
- [ ] Returns Failed when exceeding thresholds
- [ ] Thresholds are configurable (_maxWarnings, _maxErrors)
- [ ] Tests verify both pass and fail scenarios

### Gap Checklist Item

- [ ] 🔄 **Gap 3.1: LinterCheck** - Implementation complete with tests passing

---

# PHASE 4: VERIFIER STAGE BASE (2-3 hours)

## Gap 4.1: IVerifierStage Interface

- **Current State:** ❌ MISSING
- **Spec Reference:** Lines 963-988 (Implementation Prompt)
- **What Exists:** Nothing
- **What's Missing:** IVerifierStage interface that extends IStage (from task-012a)
- **Acceptance Criteria Covered:** AC-001 (IStage implemented)

### Implementation Details (from spec lines 963-988)

```csharp
namespace Acode.Application.Orchestration.Stages.Verifier;

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

public sealed record VerificationOptions(
    string WorkspacePath = ".",
    bool RunCompilation = true,
    bool RunTests = true,
    bool RunStaticAnalysis = true,
    int TimeoutSeconds = 180);

public sealed record VerificationFeedback(
    IReadOnlyList<FeedbackIssue> Issues,
    IReadOnlyList<string> Suggestions);

public sealed record FeedbackIssue(
    string CheckName,
    string Reason,
    string? Details);

public sealed record StepResult(
    StepStatus Status,
    string Output,
    string Message,
    int TokensUsed);

public enum StepStatus
{
    Success,
    Failed,
    Pending,
    Skipped
}
```

### Test Requirements

```csharp
// From spec lines 599-624
[Fact]
public async Task Should_Report_Pass_When_All_Checks_Pass()
{
    // Verify VerificationResult has Status, CheckResults, Feedback
    // Verify Status is Passed when all checks pass
    // Verify Feedback is null on success
}
```

### Success Criteria

- [ ] IVerifierStage interface created extending IStage
- [ ] VerifyAsync method signature correct
- [ ] VerificationResult record created with Status, CheckResults, Feedback
- [ ] VerificationStatus enum created (Passed, Failed, PartiallyPassed, Timeout)
- [ ] VerificationOptions record created
- [ ] VerificationFeedback record created
- [ ] FeedbackIssue record created
- [ ] StepResult record created (if not already from task-012b)
- [ ] StepStatus enum created (if not already)
- [ ] All types are public/sealed as specified

### Gap Checklist Item

- [ ] 🔄 **Gap 4.1: IVerifierStage Interface** - Implementation complete

---

## Gap 4.2: VerifierStage Implementation

- **Current State:** ❌ MISSING
- **Spec Reference:** Lines 1018-1102 (Implementation Prompt)
- **What Exists:** Nothing
- **What's Missing:** VerifierStage class implementing IVerifierStage and stage lifecycle
- **Acceptance Criteria Covered:** AC-002 (Results loaded), AC-003 (Verification runs), AC-004 (Status reported), AC-005 (Events logged)

### Implementation Details (from spec lines 1018-1102)

```csharp
namespace Acode.Application.Orchestration.Stages.Verifier;

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
        var match = Regex.Match(output, @"\bfile:\s*([\w/.\-]+)");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}

// Base class (assumed to exist from task-012a)
public abstract class StageBase
{
    protected readonly ILogger _logger;

    public abstract StageType Type { get; }

    public StageBase(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<StageResult> OnEnterAsync(StageContext context, CancellationToken ct)
    {
        return await ExecuteStageAsync(context, ct);
    }

    public async Task<StageResult> ExecuteAsync(StageContext context, CancellationToken ct)
    {
        return await ExecuteStageAsync(context, ct);
    }

    protected abstract Task<StageResult> ExecuteStageAsync(StageContext context, CancellationToken ct);
}
```

### Test Requirements

```csharp
// From spec lines 599-624
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
    Assert.Null(result.Feedback);
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
```

### Success Criteria

- [ ] VerifierStage class created extending StageBase
- [ ] Implements IVerifierStage interface
- [ ] Type property returns StageType.Verifier
- [ ] Constructor takes ICheckRunner, IFeedbackGenerator, ILogger
- [ ] ExecuteStageAsync loads step results from context
- [ ] ExecuteStageAsync calls VerifyAsync
- [ ] ExecuteStageAsync returns StageResult with correct NextStage
- [ ] VerifyAsync loads results from context.StageData["step_results"]
- [ ] VerifyAsync creates CheckContext
- [ ] VerifyAsync calls CheckRunner.RunAllAsync
- [ ] VerifyAsync aggregates check results
- [ ] VerifyAsync calls FeedbackGenerator when any check fails
- [ ] ExtractFilesFromStepResults extracts files mentioned in output
- [ ] Both test scenarios pass
- [ ] All logging statements present

### Gap Checklist Item

- [ ] 🔄 **Gap 4.2: VerifierStage** - Implementation complete with tests passing

---

# PHASE 5: FEEDBACK & CYCLING (2-3 hours)

## Gap 5.1: CheckRunner Implementation

- **Current State:** ❌ MISSING
- **Spec Reference:** Lines 1104-1166 (Implementation Prompt)
- **What Exists:** Nothing
- **What's Missing:** CheckRunner service that executes all checks in parallel and aggregates results
- **Acceptance Criteria Covered:** AC-040 (Parallel works), FR-063-066 (Parallelization)

### Implementation Details (from spec lines 1104-1166)

```csharp
namespace Acode.Application.Orchestration.Stages.Verifier;

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

### Test Requirements

(CheckRunner is tested through VerifierStageTests - it's called by VerifyAsync)

```csharp
// Verified in VerifierStageTests through mocking
[Fact]
public async Task CheckRunner_Should_Execute_All_Checks_In_Parallel()
{
    // This is verified through VerifierStageTests
    // where we mock ICheckRunner.RunAllAsync
}
```

### Success Criteria

- [ ] ICheckRunner interface created
- [ ] CheckRunner class created implementing ICheckRunner
- [ ] Constructor takes IEnumerable<ICheck>, ILogger
- [ ] RunAllAsync executes all checks using Task.WhenAll (parallel)
- [ ] RunAllAsync returns IReadOnlyList<CheckResult>
- [ ] RunCheckAsync handles exceptions gracefully
- [ ] Returns CheckResult with Failed status if check throws
- [ ] Logs execution start and completion
- [ ] No NotImplementedException

### Gap Checklist Item

- [ ] 🔄 **Gap 5.1: CheckRunner** - Implementation complete

---

## Gap 5.2: FeedbackGenerator Implementation

- **Current State:** ❌ MISSING
- **Spec Reference:** Lines 1168-1235 (Implementation Prompt)
- **What Exists:** Nothing
- **What's Missing:** FeedbackGenerator service that creates actionable feedback from failed checks
- **Acceptance Criteria Covered:** AC-031 (Generated on failure), AC-032 (Actionable), AC-033 (Flows to Executor)

### Implementation Details (from spec lines 1168-1235)

```csharp
namespace Acode.Application.Orchestration.Stages.Verifier;

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
```

### Test Requirements

```csharp
// Verified in VerifierStageTests through mocking
[Fact]
public void FeedbackGenerator_Should_Create_Actionable_Suggestions()
{
    // This is verified in VerifierStageTests
    // where we check Feedback.Suggestions is not null
}
```

### Success Criteria

- [ ] IFeedbackGenerator interface created
- [ ] FeedbackGenerator class created implementing IFeedbackGenerator
- [ ] Generate method takes IReadOnlyList<CheckResult>
- [ ] Returns VerificationFeedback with Issues and Suggestions
- [ ] Issues extracted from failed checks
- [ ] Suggestions match check type (FileExists, Compilation, Tests, etc.)
- [ ] Suggestions are actionable (not generic)
- [ ] All logging present

### Gap Checklist Item

- [ ] 🔄 **Gap 5.2: FeedbackGenerator** - Implementation complete

---

# PHASE 6: FINAL INTEGRATION & E2E (2-3 hours)

## Gap 6.1: VerifierIntegrationTests

- **Current State:** ❌ MISSING
- **Spec Reference:** Lines 806-840 (Testing Requirements)
- **What Exists:** Nothing
- **What's Missing:** Integration test verifying real files in workspace
- **Acceptance Criteria Covered:** AC-001-043 (verifies entire Verifier Stage works)

### Implementation Details (from spec lines 806-840)

```csharp
namespace Acode.Application.Tests.Integration.Orchestration.Stages.Verifier;

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

### Test Requirements

Verify the integration test runs successfully with real file operations

### Success Criteria

- [ ] Test file created with integration tests
- [ ] Uses TestServerFixture or equivalent
- [ ] Creates real test workspace
- [ ] Creates real files
- [ ] Verifies files are detected
- [ ] Test passes

### Gap Checklist Item

- [ ] 🔄 **Gap 6.1: VerifierIntegrationTests** - Implementation complete with tests passing

---

## Gap 6.2: VerifierE2ETests

- **Current State:** ❌ MISSING
- **Spec Reference:** Lines 842-873 (Testing Requirements)
- **What Exists:** Nothing
- **What's Missing:** End-to-end test verifying complete verification flow with good code
- **Acceptance Criteria Covered:** AC-001-043 (complete E2E verification)

### Implementation Details (from spec lines 842-873)

```csharp
namespace Acode.Application.Tests.E2E.Orchestration.Stages.Verifier;

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

### Test Requirements

Verify E2E test runs successfully with complete workflow

### Success Criteria

- [ ] E2E test file created
- [ ] Uses E2ETestFixture or equivalent
- [ ] Creates workspace with valid code
- [ ] Executes full verification flow
- [ ] Verifies all checks pass
- [ ] Test passes

### Gap Checklist Item

- [ ] 🔄 **Gap 6.2: VerifierE2ETests** - Implementation complete with tests passing

---

## Gap 6.3: Dependency Injection Registration

- **Current State:** ❌ MISSING
- **Spec Reference:** None (implementation requirement)
- **What Exists:** Nothing
- **What's Missing:** Registration of all Verifier components in DI container
- **Acceptance Criteria Covered:** AC-001-043 (all components must be injectable)

### Implementation Details

```csharp
// In IServiceCollection extension
public static IServiceCollection AddVerifierStage(this IServiceCollection services)
{
    // Register interfaces
    services.AddScoped<IVerifierStage, VerifierStage>();
    services.AddScoped<ICheckRunner, CheckRunner>();
    services.AddScoped<IFeedbackGenerator, FeedbackGenerator>();

    // Register checks
    services.AddScoped<FileExistsCheck>();
    services.AddScoped<FileContentCheck>();
    services.AddScoped<CompilationCheck>();
    services.AddScoped<TestRunCheck>();
    services.AddScoped<LinterCheck>();

    // Register as ICheck
    services.AddScoped<ICheck>(sp => sp.GetRequiredService<FileExistsCheck>());
    services.AddScoped<ICheck>(sp => sp.GetRequiredService<FileContentCheck>());
    services.AddScoped<ICheck>(sp => sp.GetRequiredService<CompilationCheck>());
    services.AddScoped<ICheck>(sp => sp.GetRequiredService<TestRunCheck>());
    services.AddScoped<ICheck>(sp => sp.GetRequiredService<LinterCheck>());

    return services;
}
```

### Success Criteria

- [ ] All interfaces registered in DI
- [ ] All implementations registered
- [ ] All checks registered as ICheck
- [ ] Extension method created and called from Startup/Program.cs

### Gap Checklist Item

- [ ] 🔄 **Gap 6.3: Dependency Injection** - Registration complete

---

## Gap 6.4: Final Verification

- **Current State:** ❌ MISSING
- **Spec Reference:** Lines 1413-1432 (Implementation Checklist, Validation Checklist)
- **What Exists:** Nothing
- **What's Missing:** Final verification that all ACs are complete and build passes
- **Acceptance Criteria Covered:** AC-001-043 (ALL ACs)

### Verification Checklist

Run these commands to verify task-012c is complete:

```bash
# 1. Build verification
dotnet build
# Expected: 0 errors, 0 warnings

# 2. Test verification
dotnet test --filter "Verifier" --verbosity normal
# Expected: All tests passing, minimum 9+ test methods passing

# 3. No stubs remaining
grep -r "NotImplementedException" src/Acode.Application/Orchestration/Stages/Verifier/
grep -r "NotImplementedException" tests/Acode.Application.Tests/Orchestration/Stages/Verifier/
# Expected: NO MATCHES

# 4. File count
find src/Acode.Application/Orchestration/Stages/Verifier -name "*.cs" -type f | wc -l
# Expected: 10 files

# 5. Test file count
find tests -path "*Verifier*" -name "*.cs" -type f | wc -l
# Expected: 5 test files
```

### Success Criteria

- [ ] Build passes with 0 errors, 0 warnings
- [ ] All tests passing (9+ tests)
- [ ] No NotImplementedException found
- [ ] All 10 production files exist
- [ ] All 5 test files exist
- [ ] File count matches spec
- [ ] All 43 ACs verified complete
- [ ] Gap analysis updated to 100% completion

### Gap Checklist Item

- [ ] 🔄 **Gap 6.4: Final Verification** - All checks passing

---

# SUMMARY TABLE

| Phase | Description | Hours | AC Coverage | Status |
|-------|-------------|-------|------------|--------|
| Phase 1 | Check Infrastructure (ICheck, FileExistsCheck, FileContentCheck) | 2-3 | AC-006 to AC-008 | Pending |
| Phase 2 | Compilation & Tests (CompilationCheck, TestRunCheck) | 3-4 | AC-010 to AC-018 | Pending |
| Phase 3 | Static Analysis (LinterCheck) | 1-2 | AC-019 to AC-022 | Pending |
| Phase 4 | Verifier Stage Base (IVerifierStage, VerifierStage) | 2-3 | AC-001 to AC-005 | Pending |
| Phase 5 | Feedback & Cycling (CheckRunner, FeedbackGenerator) | 2-3 | AC-031 to AC-037, AC-040 | Pending |
| Phase 6 | Integration & E2E (Tests, DI, Final Verification) | 2-3 | AC-001 to AC-043 | Pending |
| **TOTAL** | **Complete Verifier Stage** | **15-22** | **0/43 ACs** | **Pending** |

---

# ESTIMATED TIMELINE

**Prerequisite:** Task-012a complete (12-16 hours prior)

**Task-012c Implementation:**
- Phase 1: 2-3 hours
- Phase 2: 3-4 hours
- Phase 3: 1-2 hours
- Phase 4: 2-3 hours
- Phase 5: 2-3 hours
- Phase 6: 2-3 hours
- **Total: 15-22 hours**

---

**Status:** ✅ COMPLETION CHECKLIST READY - AWAITING TASK-012A

**Next Agent Instructions:**
1. Read this checklist completely before coding
2. Work through phases sequentially (don't skip)
3. Follow TDD: tests first, then implementation
4. Mark gaps [✅] when complete with tests passing
5. Commit after each phase
6. Run `dotnet test` after each phase
7. At end: Update gap analysis to 100%, create PR

---
