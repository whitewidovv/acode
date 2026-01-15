# Task-010c Completion Checklist: Non-Interactive Mode Behaviors

**Status:** 40% COMPLETE - 2 CRITICAL IMPLEMENTATION GAPS IDENTIFIED

**Date:** 2026-01-15
**Created By:** Claude Code
**Purpose:** Implementation checklist for completing 010c from 40% to 100% production readiness

---

## INSTRUCTIONS FOR IMPLEMENTATION

This checklist contains **2 major implementation gaps** that must be completed to reach production readiness:

1. **Integration Layer (Program.Main orchestration)** - 40-50 hours
2. **E2E and Integration Tests** - 15-20 hours

These gaps represent the ONLY work remaining. All components exist and are tested—they just need to be wired together into the actual CLI flow.

**Total Estimated Effort:** 55-70 hours

**Work Order:** Complete GAP 1 first (integration), then GAP 2 (tests). These are sequential because tests verify the integration works end-to-end.

**TDD Approach:** Add integration tests (RED), implement orchestration (GREEN), then verify E2E (REFACTOR).

---

## WHAT EXISTS (DO NOT RECREATE)

These components are **100% complete and fully tested**. Do NOT reimplement them. They just need to be used:

### ✅ All Component Implementation (74/76 ACs Complete)
- ModeDetector (188 lines) - Detects non-TTY, CI environments
- CIEnvironmentDetector (156 lines) - All 7 CI platforms
- ApprovalManager (137 lines) - Approval policies and decisions
- TimeoutManager (95 lines) - Timeout enforcement with CancellationToken
- SignalHandler (195 lines) - Signal handling and graceful shutdown
- NonInteractiveOptions (record) - All flag/env parsing
- NonInteractiveProgressReporter (179 lines) - Stderr progress output
- PreflightChecker (108 lines) - Pre-execution validation
- ExitCode (enum) - All special exit codes defined
- ConsoleFormatter (130+ lines) - Conditional color/formatting

### ✅ Comprehensive Test Coverage (75 tests, 100% passing)
- ModeDetectorTests.cs (11 tests)
- CIEnvironmentDetectorTests.cs (6 tests)
- ApprovalPolicyTests.cs (3 tests)
- ApprovalManagerTests.cs (3 tests)
- TimeoutManagerTests.cs (5 tests)
- SignalHandlerTests.cs (6 tests)
- PreflightCheckerTests.cs (5 tests)
- PreflightResultTests.cs (2 tests)
- CLI integration tests (28 tests)

**Test Status:** All 502 CLI tests passing ✅

---

## GAP 1: Integration Layer - Program.Main Orchestration

**Status:** 🔄 PENDING IMPLEMENTATION

**Impact:** ALL non-interactive mode features are non-functional until this gap is closed

### What Needs to Be Done

The CLI entry point (Program.cs Main method) currently only handles:
- JSONL mode setup (task-010b)
- Command routing (task-010a)
- Basic exception handling

**Missing:** Complete orchestration of non-interactive mode components

### Implementation Phase 1a: Argument/Environment Parsing

**Work Item 1a-1: Parse NonInteractiveOptions from Arguments**

**File:** `src/Acode.CLI/Program.cs`

**Code Pattern to Implement:**
```csharp
// In Main() method, after services are created:

var nonInteractiveOptions = ParseNonInteractiveOptions(args, serviceProvider);

// Where ParseNonInteractiveOptions:
static NonInteractiveOptions ParseNonInteractiveOptions(string[] args, IServiceProvider serviceProvider)
{
    var options = new NonInteractiveOptions();

    // Parse flags from args
    if (args.Contains("--yes")) options.Yes = true;
    if (args.Contains("--no-approve")) options.NoApprove = true;
    if (args.Contains("--non-interactive")) options.Enabled = true;
    if (args.Contains("--skip-preflight")) options.SkipPreflight = true;
    if (args.Contains("--quiet")) options.Suppress = true;

    // Parse --timeout value
    var timeoutArg = args.FirstOrDefault(a => a.StartsWith("--timeout="));
    if (timeoutArg != null && int.TryParse(timeoutArg.Split('=')[1], out var timeout))
        options.TimeoutSeconds = timeout;

    // Parse --approval-policy value
    var policyArg = args.FirstOrDefault(a => a.StartsWith("--approval-policy="));
    if (policyArg != null)
        options.ApprovalPolicy = policyArg.Split('=')[1];

    // Check environment variables
    if (Environment.GetEnvironmentVariable("ACODE_NON_INTERACTIVE") == "1")
        options.Enabled = true;
    if (int.TryParse(Environment.GetEnvironmentVariable("ACODE_TIMEOUT"), out var envTimeout))
        options.TimeoutSeconds = envTimeout;

    return options;
}
```

**Verification Command:**
```bash
dotnet run -- --yes --timeout=300 --approval-policy=low-risk test-command
# Should parse flags correctly
```

**Effort:** 2 hours
- 1 hour: Implement parsing
- 1 hour: Add unit tests for parsing logic

---

**Work Item 1a-2: Create NonInteractiveOptions Record (if doesn't exist)**

**File:** `src/Acode.CLI/NonInteractiveOptions.cs` or modify if exists

**Code Pattern:**
```csharp
public record NonInteractiveOptions(
    bool Enabled = false,
    bool Yes = false,
    bool NoApprove = false,
    string? ApprovalPolicy = null,
    int TimeoutSeconds = 3600,
    bool SkipPreflight = false,
    bool Suppress = false
);
```

**Verify:** Check if this already exists in codebase

**Effort:** 0.5 hours (if creating), 0 hours (if already exists)

---

### Implementation Phase 1b: Component Initialization

**Work Item 1b-1: Initialize ModeDetector**

**File:** `src/Acode.CLI/Program.cs` - Main() method

**Code Pattern:**
```csharp
// After NonInteractiveOptions parsed
var modeDetector = serviceProvider.GetRequiredService<IModeDetector>();
modeDetector.Initialize(nonInteractiveOptions);

// ModeDetector logs which mode was selected
// Sets internal state for use by other components
```

**Where ModeDetector is Used:**
- Returns IsNonInteractive boolean
- IsNonInteractive affects which ConsoleFormatter, ProgressReporter to use
- Affects how commands are executed (no interactive prompts)

**Verification Test:**
```bash
# With --non-interactive flag
dotnet run -- --non-interactive help
# Should detect non-interactive mode

# Piped input (should auto-detect)
echo "help" | dotnet run
# Should auto-detect non-TTY
```

**Effort:** 1 hour
- 0.5 hours: Add ModeDetector initialization call
- 0.5 hours: Add verification tests

---

**Work Item 1b-2: Register SignalHandler**

**File:** `src/Acode.CLI/Program.cs` - Main() method

**Code Pattern:**
```csharp
// Early in Main(), register signal handlers
var signalHandler = serviceProvider.GetRequiredService<ISignalHandler>();
signalHandler.Register();

// Later, ensure cleanup on exit:
try
{
    // ... command execution ...
}
finally
{
    signalHandler.Dispose();
}
```

**What This Does:**
- Hooks into Console.CancelKeyPress (Ctrl+C/SIGINT)
- Hooks into AppDomain.ProcessExit (SIGTERM)
- Handles SIGPIPE (broken pipe)
- Enables graceful shutdown within 30 seconds

**Verification:**
```bash
# Start a long-running command
dotnet run -- long-task

# Press Ctrl+C
# Should print "Shutting down gracefully..."
# Should complete within 30 seconds
```

**Effort:** 1.5 hours
- 0.5 hours: Add register/dispose calls
- 1 hour: Add graceful shutdown verification

---

### Implementation Phase 1c: Pre-Execution Checks

**Work Item 1c-1: Run PreflightChecker**

**File:** `src/Acode.CLI/Program.cs` - Main() method, before command routing

**Code Pattern:**
```csharp
// After ModeDetector initialized
if (modeDetector.IsNonInteractive && !nonInteractiveOptions.SkipPreflight)
{
    var preflight = serviceProvider.GetRequiredService<IPreflightChecker>();
    var result = await preflight.RunAllChecksAsync();

    if (!result.IsSuccessful)
    {
        // Log failures
        foreach (var failure in result.Failures)
            _logger.LogError("Pre-flight check failed: {failure}", failure);

        // Return special exit code
        return (int)ExitCode.PreflightFailed; // 13
    }
}
```

**What This Checks:**
- Config files exist and are valid
- Required models are available
- File access permissions OK
- Other pre-execution validations

**Verification:**
```bash
# With missing config
dotnet run -- --non-interactive task-that-needs-config
# Should return exit code 13
```

**Effort:** 2 hours
- 1 hour: Add PreflightChecker call and error handling
- 1 hour: Add failure tests and exit code verification

---

### Implementation Phase 1d: Timeout and Approval Wiring

**Work Item 1d-1: Create TimeoutManager and Wrap Command Execution**

**File:** `src/Acode.CLI/Program.cs` - Main() method

**Code Pattern:**
```csharp
// Create timeout manager
var timeoutManager = new TimeoutManager(
    TimeSpan.FromSeconds(nonInteractiveOptions.TimeoutSeconds)
);

// Wrap command execution with timeout
int result;
try
{
    var router = serviceProvider.GetRequiredService<ICommandRouter>();
    var cts = new CancellationTokenSource();

    // Start timeout countdown
    if (nonInteractiveOptions.TimeoutSeconds > 0)
    {
        cts.CancelAfter(TimeSpan.FromSeconds(nonInteractiveOptions.TimeoutSeconds));
    }

    // Store CancellationToken in context for commands
    // (see next work item)
    result = await router.RouteAsync(args);
}
catch (OperationCanceledException)
{
    _logger.LogError("Command timed out after {seconds} seconds",
        nonInteractiveOptions.TimeoutSeconds);
    return (int)ExitCode.Timeout; // 11
}
```

**Verification:**
```bash
# With 2-second timeout on longer task
dotnet run -- --non-interactive --timeout=2 long-task
# Should return exit code 11 after ~2 seconds
```

**Effort:** 2 hours
- 1 hour: Add timeout wrapping
- 1 hour: Add timeout test scenarios

---

**Work Item 1d-2: Inject ApprovalManager into Commands**

**File:** `src/Acode.CLI/Program.cs` - Main() method and CommandContext modification

**Code Pattern:**
```csharp
// Create approval manager
var approvalManager = new ApprovalManager(
    nonInteractiveOptions,
    serviceProvider.GetRequiredService<ILogger>()
);

// Store in CommandContext or DI (implementation depends on architecture)
// Option A: Add to CommandContext
commandContext.ApprovalManager = approvalManager;

// Option B: Register in DI as scoped
// services.AddScoped<IApprovalManager>(_ => approvalManager);

// Then in commands, they can request approval:
var result = await approvalManager.RequestApprovalAsync(
    actionType: "execute_command",
    riskLevel: "high"
);

if (!result)
{
    return (int)ExitCode.InputRequired; // 10 (needs approval but can't get it)
}
```

**Verification:**
```bash
# With auto-approve
dotnet run -- --yes command-needing-approval
# Should execute without prompting

# Without approval policy (should fail)
dotnet run -- --non-interactive command-needing-approval
# Should return exit code 10 (input required)
```

**Effort:** 2.5 hours
- 1.5 hours: Implement approval injection
- 1 hour: Add approval flow tests

---

### Implementation Phase 1e: Output and Progress Setup

**Work Item 1e-1: Select Appropriate Output Formatter and Progress Reporter**

**File:** `src/Acode.CLI/Program.cs` - Main() method, before command execution

**Code Pattern:**
```csharp
// Select progress reporter based on mode
IProgressReporter progressReporter = modeDetector.IsNonInteractive
    ? new NonInteractiveProgressReporter(
        Console.Error,
        TimeSpan.FromSeconds(10),
        nonInteractiveOptions.Suppress
    )
    : new InteractiveProgressReporter(Console.Out);

// Register in DI for commands to use
// services.AddScoped<IProgressReporter>(_ => progressReporter);

// Or pass through CommandContext
commandContext.ProgressReporter = progressReporter;
```

**What NonInteractiveProgressReporter Does:**
- Outputs to stderr (not stdout)
- Uses ISO 8601 timestamps
- Machine-parseable format
- 10-second default interval
- Can be suppressed with --quiet flag

**Verification:**
```bash
# In non-interactive mode
echo "test" | dotnet run -- task
# Progress should go to stderr (not stdout)
# No spinners, no colors

# Capture and verify format
echo "test" | dotnet run -- task 2>&1 | grep "^\[.*\]"
# Should show ISO 8601 timestamps
```

**Effort:** 1.5 hours
- 1 hour: Implement formatter/reporter selection
- 0.5 hours: Add output format verification tests

---

### Implementation Phase 1f: Exit Code Handling

**Work Item 1f-1: Ensure Special Exit Codes Are Returned**

**File:** `src/Acode.CLI/Program.cs` - Main() method exception handlers

**Code Pattern:**
```csharp
// In Main() return logic, map exceptions to exit codes:

try
{
    // ... execution ...
    return (int)ExitCode.Success; // 0
}
catch (ApprovalRequiredException)
{
    _logger.LogError("Approval required but could not be obtained");
    return (int)ExitCode.InputRequired; // 10
}
catch (OperationCanceledException)
{
    _logger.LogError("Command timed out");
    return (int)ExitCode.Timeout; // 11
}
catch (ApprovalDeniedException)
{
    _logger.LogError("Approval was denied");
    return (int)ExitCode.ApprovalDenied; // 12
}
catch (PreflightCheckException)
{
    _logger.LogError("Pre-flight checks failed");
    return (int)ExitCode.PreflightFailed; // 13
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error");
    return (int)ExitCode.GeneralError; // 1
}
```

**Verify Exit Codes:**
```bash
# Test each exit code
dotnet run -- --timeout=1 sleep-5
echo $?  # Should be 11

dotnet run -- --non-interactive missing-config-task
echo $?  # Should be 13
```

**Effort:** 1 hour
- 0.5 hours: Add exception mapping
- 0.5 hours: Add exit code verification tests

---

## Phase 1 Summary: Integration Layer

**Total Phase 1 Effort:** 40-50 hours
- 1a: Argument parsing (2 hours)
- 1b: Component initialization (2.5 hours)
- 1c: Pre-execution checks (2 hours)
- 1d: Timeout and approval (4.5 hours)
- 1e: Output and progress (1.5 hours)
- 1f: Exit code handling (1 hour)
- Integration/testing: 10-15 hours
- Refactoring: 5 hours

**Mark Phase 1 Complete When:**
- [x] Program.Main orchestrates all components
- [x] ModeDetector initialized and mode detected
- [x] SignalHandler registered for graceful shutdown
- [x] PreflightChecker runs before commands
- [x] TimeoutManager enforces timeout with CancellationToken
- [x] ApprovalManager available to commands
- [x] ProgressReporter selected based on mode
- [x] Special exit codes (10, 11, 12, 13) returned correctly
- [x] Build clean: 0 errors, 0 warnings
- [x] All 502 CLI tests pass

---

## GAP 2: E2E and Integration Test Coverage

**Status:** 🔄 PENDING IMPLEMENTATION

**Current Test Coverage:** 75 unit tests for components ✅
**Missing:** 30-40 E2E/integration tests that verify components work together

### What Needs to Be Tested

#### Test Group 1: Mode Detection E2E (4 tests)

**File:** `tests/Acode.CLI.Integration.Tests/NonInteractiveModeDetectionE2ETests.cs`

**Test 1: Detect non-TTY stdin**
```csharp
[Fact]
public async Task Given_PipedInput_When_CommandExecutes_Then_ModeDetectionTriggersNonInteractive()
{
    // Simulate: echo "test" | acode run
    // Use ProcessStartInfo with RedirectStandardInput = true
    // Verify: ModeDetector.IsNonInteractive == true
}
```

**Test 2: Detect non-TTY stdout**
```csharp
[Fact]
public async Task Given_RedirectedOutput_When_CommandExecutes_Then_ModeDetectionTriggersNonInteractive()
{
    // Simulate: acode run > output.txt
    // Verify: ModeDetector.IsNonInteractive == true
}
```

**Test 3: Detect --non-interactive flag**
```csharp
[Fact]
public async Task Given_NonInteractiveFlag_When_CommandExecutes_Then_ModeDetectionTriggersNonInteractive()
{
    // Run: dotnet run -- --non-interactive command
    // Verify: ModeDetector.IsNonInteractive == true
}
```

**Test 4: Detect CI environment (CI=true)**
```csharp
[Fact]
public async Task Given_CIEnvironmentVariable_When_CommandExecutes_Then_ModeDetectionTriggersNonInteractive()
{
    // Set CI=true env var
    // Run: dotnet run -- command
    // Verify: ModeDetector.IsNonInteractive == true
    // Verify: CIEnvironmentDetector identifies CI platform
}
```

**Effort:** 4 hours
- 2 hours: Implement test fixtures for process simulation
- 2 hours: Implement 4 test methods

---

#### Test Group 2: Flag Parsing E2E (3 tests)

**File:** `tests/Acode.CLI.Integration.Tests/NonInteractiveFlagParsingE2ETests.cs`

**Test 1: Parse --yes flag**
```csharp
[Fact]
public async Task Given_YesFlag_When_CommandExecutes_Then_ApprovalsAutomatic()
{
    var result = await RunCommand("--yes --non-interactive approve-test");
    // Command should approve automatically, exit 0
    Assert.Equal(0, result.ExitCode);
}
```

**Test 2: Parse --timeout flag**
```csharp
[Fact]
public async Task Given_TimeoutFlag_When_CommandExecutes_Then_TimeoutEnforced()
{
    var result = await RunCommand("--timeout=1 --non-interactive sleep-5");
    // Should timeout after ~1 second, exit 11
    Assert.Equal(11, result.ExitCode);
}
```

**Test 3: Parse --approval-policy flag**
```csharp
[Fact]
public async Task Given_ApprovalPolicyFlag_When_CommandExecutes_Then_PolicyApplied()
{
    var result = await RunCommand("--approval-policy=low-risk --non-interactive risky-task");
    // Should reject high-risk actions, exit 10
    Assert.Equal(10, result.ExitCode);
}
```

**Effort:** 3 hours
- 1 hour: Create test runner helper
- 2 hours: Implement 3 test methods

---

#### Test Group 3: Approval Flow E2E (5 tests)

**File:** `tests/Acode.CLI.Integration.Tests/ApprovalFlowE2ETests.cs`

**Test 1: Auto-approve with --yes**
```csharp
[Fact]
public async Task Given_YesFlag_When_ApprovalNeeded_Then_AutoApproved()
{
    var result = await RunCommand("--yes --non-interactive action-needing-approval");
    Assert.Equal(0, result.ExitCode);
    Assert.Contains("approved", result.Output.ToLower());
}
```

**Test 2: Reject with --no-approve**
```csharp
[Fact]
public async Task Given_NoApproveFlag_When_ApprovalNeeded_Then_Rejected()
{
    var result = await RunCommand("--no-approve --non-interactive action-needing-approval");
    Assert.Equal(12, result.ExitCode); // ApprovalDenied
}
```

**Test 3: Input required when no policy**
```csharp
[Fact]
public async Task Given_NoApprovalPolicy_When_ApprovalNeeded_Then_InputRequired()
{
    var result = await RunCommand("--non-interactive action-needing-approval");
    Assert.Equal(10, result.ExitCode); // InputRequired
}
```

**Test 4: Low-risk approval policy**
```csharp
[Fact]
public async Task Given_LowRiskPolicy_When_HighRiskAction_Then_Rejected()
{
    var result = await RunCommand("--approval-policy=low-risk --non-interactive high-risk-action");
    Assert.Equal(12, result.ExitCode); // ApprovalDenied
}
```

**Test 5: All approval policy**
```csharp
[Fact]
public async Task Given_AllPolicy_When_AnyRiskAction_Then_Approved()
{
    var result = await RunCommand("--approval-policy=all --non-interactive any-risk-action");
    Assert.Equal(0, result.ExitCode);
}
```

**Effort:** 5 hours
- 1 hour: Create action runner helpers
- 4 hours: Implement 5 test methods

---

#### Test Group 4: Timeout Enforcement E2E (4 tests)

**File:** `tests/Acode.CLI.Integration.Tests/TimeoutE2ETests.cs`

**Test 1: Timeout with default value**
```csharp
[Fact]
public async Task Given_NoTimeoutFlag_When_CommandStalls_Then_DefaultTimeout()
{
    var result = await RunCommand("--non-interactive stall-indefinitely");
    // Should timeout after 3600 seconds (1 hour)
    // This is the default, so for testing use --timeout=1
    Assert.Equal(11, result.ExitCode); // Timeout
}
```

**Test 2: Timeout with custom value**
```csharp
[Fact]
public async Task Given_TimeoutFlag_When_Exceeded_Then_ExitCode11()
{
    var result = await RunCommand("--timeout=1 --non-interactive sleep-5");
    Assert.Equal(11, result.ExitCode);
    Assert.InRange(result.Duration.TotalSeconds, 0.5, 2);
}
```

**Test 3: Timeout zero = infinite**
```csharp
[Fact]
public async Task Given_TimeoutZero_When_CommandExecutes_Then_NoTimeout()
{
    var result = await RunCommand("--timeout=0 --non-interactive quick-task");
    Assert.Equal(0, result.ExitCode);
}
```

**Test 4: Graceful shutdown on timeout**
```csharp
[Fact]
public async Task Given_Timeout_When_Exceeded_Then_GracefulShutdown()
{
    var result = await RunCommand("--timeout=1 --non-interactive task-with-cleanup");
    // Task should have time to clean up (30s grace period)
    Assert.Equal(11, result.ExitCode);
    Assert.Contains("cleanup complete", result.Output.ToLower());
}
```

**Effort:** 4 hours
- 1 hour: Create timing helpers
- 3 hours: Implement 4 test methods

---

#### Test Group 5: Pre-flight Checks E2E (3 tests)

**File:** `tests/Acode.CLI.Integration.Tests/PreflightCheckE2ETests.cs`

**Test 1: Pre-flight failure exit code**
```csharp
[Fact]
public async Task Given_PreflightCheckFails_When_CommandExecutes_Then_ExitCode13()
{
    // Task without required config file
    var result = await RunCommand("--non-interactive task-needing-config");
    Assert.Equal(13, result.ExitCode); // PreflightFailed
    Assert.Contains("config", result.ErrorOutput.ToLower());
}
```

**Test 2: Skip preflight with flag**
```csharp
[Fact]
public async Task Given_SkipPreflightFlag_When_CommandExecutes_Then_ChecksSkipped()
{
    // Even with missing config
    var result = await RunCommand("--skip-preflight --non-interactive task-needing-config");
    // Should attempt execution (might fail differently)
    Assert.NotEqual(13, result.ExitCode); // NOT PreflightFailed
}
```

**Test 3: All pre-flight failures logged**
```csharp
[Fact]
public async Task Given_MultipleChecksFail_When_PreflightRuns_Then_AllFailuresLogged()
{
    var result = await RunCommand("--non-interactive multi-failure-task");
    Assert.Equal(13, result.ExitCode);
    Assert.Contains("config", result.ErrorOutput.ToLower());
    Assert.Contains("permission", result.ErrorOutput.ToLower());
    Assert.Contains("model", result.ErrorOutput.ToLower());
}
```

**Effort:** 3 hours
- 1 hour: Create pre-flight failure scenarios
- 2 hours: Implement 3 test methods

---

#### Test Group 6: Signal Handling E2E (4 tests)

**File:** `tests/Acode.CLI.Integration.Tests/SignalHandlingE2ETests.cs`

**Test 1: SIGINT (Ctrl+C) graceful**
```csharp
[Fact]
public async Task Given_CtrlCPressed_When_CommandRunning_Then_GracefulShutdown()
{
    var process = RunCommandProcess("--non-interactive long-task");
    await Task.Delay(500); // Let it start

    // Send SIGINT (Ctrl+C)
    process.StandardInput.WriteLine();
    process.StandardInput.Close();

    var exited = process.WaitForExit(35000); // 30s grace + 5s margin
    Assert.True(exited, "Process should exit within grace period");
}
```

**Test 2: SIGTERM graceful**
```csharp
[Fact]
public async Task Given_TermSignal_When_CommandRunning_Then_GracefulShutdown()
{
    // Simulate: kill -TERM <pid>
    // Send SIGTERM and verify graceful shutdown
}
```

**Test 3: Timeout before grace period**
```csharp
[Fact]
public async Task Given_TimeoutDuringShutdown_When_GracePeriodExpires_Then_ForceKill()
{
    // Send SIGTERM, then don't respond to cleanup
    // Grace period (30s) should expire and force kill
}
```

**Test 4: Pending writes completed**
```csharp
[Fact]
public async Task Given_SignalDuringOutput_When_Shutdown_Then_BuffersFlushed()
{
    var result = await RunCommand("--non-interactive task-with-output");
    // Send SIGINT partway through
    // Verify all output was written (not lost)
    Assert.NotEmpty(result.Output);
}
```

**Effort:** 4 hours
- 2 hours: Create process signal helpers
- 2 hours: Implement 4 test methods

---

#### Test Group 7: Output and Progress Format E2E (2 tests)

**File:** `tests/Acode.CLI.Integration.Tests/OutputFormatE2ETests.cs`

**Test 1: Non-interactive progress format**
```csharp
[Fact]
public async Task Given_NonInteractiveMode_When_ProgressOutput_Then_ISO8601Format()
{
    var result = await RunCommand("--non-interactive long-task");
    // Parse stderr for progress lines
    var progressLines = result.ErrorOutput.Split('\n').Where(l => l.Contains(']'));

    foreach (var line in progressLines)
    {
        // [2026-01-15T12:34:56Z] [INFO] Progress: 50%
        Assert.Matches(@"^\[\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z\]", line);
    }
}
```

**Test 2: Quiet flag suppresses progress**
```csharp
[Fact]
public async Task Given_QuietFlag_When_ProgressOutput_Then_Suppressed()
{
    var result = await RunCommand("--quiet --non-interactive long-task");
    // Should have no progress output
    Assert.DoesNotContain("[INFO]", result.ErrorOutput);
}
```

**Effort:** 2 hours
- 1 hour: Create output parsing helpers
- 1 hour: Implement 2 test methods

---

## Phase 2 Summary: E2E and Integration Tests

**Total Phase 2 Effort:** 15-20 hours
- Mode detection tests (4 tests, 4 hours)
- Flag parsing tests (3 tests, 3 hours)
- Approval flow tests (5 tests, 5 hours)
- Timeout tests (4 tests, 4 hours)
- Pre-flight tests (3 tests, 3 hours)
- Signal handling tests (4 tests, 4 hours)
- Output format tests (2 tests, 2 hours)

**Total New Tests:** ~30 E2E/integration test methods

**Mark Phase 2 Complete When:**
- [x] All 30+ E2E tests written
- [x] All E2E tests passing
- [x] Integration layer tested end-to-end
- [x] All 502+ CLI tests passing (including new ones)
- [x] Build clean: 0 errors, 0 warnings
- [x] 76/76 ACs verified working end-to-end (100% compliance)

---

## FINAL AC VERIFICATION TABLE

When all phases complete, verify these ACs are now 76/76 complete:

| Category | Status | ACs | Notes |
|----------|--------|-----|-------|
| Mode Detection | ✅ 100% | 7/7 | E2E tested |
| CI/CD Detection | ✅ 100% | 8/8 | E2E tested |
| Approval Flow | ✅ 100% | 8/8 | E2E tested |
| Timeouts | ✅ 100% | 7/7 | E2E tested |
| Input Handling | ✅ 100% | 6/6 | E2E tested |
| Output Format | ✅ 100% | 6/6 | E2E tested |
| Progress Output | ✅ 100% | 6/6 | E2E tested |
| Exit Codes | ✅ 100% | 7/7 | All returned correctly |
| Signal Handling | ✅ 100% | 6/6 | E2E tested |
| Logging | ✅ 100% | 6/6 | Integrated |
| Pre-flight Checks | ✅ 100% | 6/6 | E2E tested |
| Performance | ✅ 100% | 3/3 | Targets met |
| **TOTAL** | **✅ 100%** | **76/76** | **PRODUCTION READY** |

---

## SUCCESS CRITERIA

Task-010c is COMPLETE when:

- [x] Integration layer fully implemented (Phase 1)
- [x] All 30+ E2E tests written and passing (Phase 2)
- [x] Build clean: 0 errors, 0 warnings
- [x] All 530+ CLI tests passing (502 original + 30 new)
- [x] AC compliance: 76/76 complete (100%)
- [x] All non-interactive features work end-to-end
- [x] Signal handling verified graceful
- [x] Exit codes returned correctly
- [x] Progress output format correct
- [x] Approval flow works
- [x] Timeout enforcement works
- [x] Pre-flight checks work
- [x] Commit created with descriptive message
- [x] Ready for PR review

---

## COMMITS EXPECTED

Create commits in this order (one per logical unit):

**Phase 1: Integration Layer**
1. `feat(task-010c): parse non-interactive options from args and environment`
2. `feat(task-010c): initialize ModeDetector in Program.Main`
3. `feat(task-010c): register SignalHandler at program startup`
4. `feat(task-010c): run PreflightChecker before command execution`
5. `feat(task-010c): enforce timeout around command execution`
6. `feat(task-010c): inject ApprovalManager into commands`
7. `feat(task-010c): select appropriate output formatter and progress reporter`
8. `feat(task-010c): return special exit codes (10/11/12/13)`

**Phase 2: E2E Tests**
9. `test(task-010c): add mode detection E2E tests`
10. `test(task-010c): add flag parsing E2E tests`
11. `test(task-010c): add approval flow E2E tests`
12. `test(task-010c): add timeout enforcement E2E tests`
13. `test(task-010c): add pre-flight check E2E tests`
14. `test(task-010c): add signal handling E2E tests`
15. `test(task-010c): add output format E2E tests`

**Documentation**
16. `docs(task-010c): update completion checklist with verification evidence`

---

## GIT WORKFLOW

**Branch:** `feature/task-010-validator-system`

After completing both phases:

```bash
# Verify no uncommitted changes
git status

# Push all commits
git push origin feature/task-010-validator-system

# Create PR after this file is complete
# (will be created when all task-010 suite is done)
```

---

## IF BLOCKED

If you encounter issues:

1. **Components not in DI:** Verify all components are registered in DI container in Program.cs
2. **Test process creation fails:** Use ProcessStartInfo with RedirectStandard* properties
3. **Timeout not enforced:** Verify CancellationToken is threaded through to actual command execution
4. **Exit code not returned:** Check Main() return type is `Task<int>` and all paths return proper exit codes
5. **E2E test timing:** Add wait/delay helpers for async operations

Contact user if unable to resolve in 15 minutes.

---

**Checklist Created:** 2026-01-15
**Total Estimated Time:** 55-70 hours (Phase 1: 40-50 hrs + Phase 2: 15-20 hrs)
**Status:** Ready for implementation
