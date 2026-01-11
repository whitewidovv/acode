# Task 002c - Gap Analysis and Implementation Checklist

## Instructions for Fresh Agent

This checklist contains ONLY what's MISSING from task-002c implementation.
DO NOT recreate files marked as "Already Complete" below.
Work through gaps sequentially following TDD: write tests first (RED), then implementation (GREEN), then refactor (CLEAN).

## Task Spec Location

`docs/tasks/refined-tasks/Epic 00/task-002c-define-command-groups.md`

## What EXISTS (Already Complete)

✅ `src/Acode.Domain/Commands/CommandGroup.cs` - Enum with all 6 command groups (Setup, Build, Test, Lint, Format, Start)
✅ `src/Acode.Domain/Commands/CommandSpec.cs` - Immutable record for command specification with all properties
✅ `src/Acode.Domain/Commands/CommandResult.cs` - Immutable record for execution results
✅ `src/Acode.Domain/Commands/ExitCodes.cs` - Static class with exit code constants and GetDescription method
✅ `tests/Acode.Domain.Tests/Commands/CommandGroupTests.cs` - 4 tests for CommandGroup enum
✅ `tests/Acode.Domain.Tests/Commands/CommandSpecTests.cs` - 10 tests for CommandSpec
✅ `tests/Acode.Domain.Tests/Commands/CommandResultTests.cs` - 8 tests for CommandResult
✅ `tests/Acode.Domain.Tests/Commands/ExitCodesTests.cs` - 14 tests for ExitCodes

## GAPS IDENTIFIED (What's Missing)

### Gap #1: CommandLogFields Static Class
**Status**: [✅ COMPLETE]
**File to Create**: `src/Acode.Domain/Commands/CommandLogFields.cs`
**Why Needed**: Spec lines 1106-1121 define logging field constants for structured logging
**Required Constants**:
- CommandGroup = "command_group"
- Command = "command"
- WorkingDirectory = "working_directory"
- ExitCode = "exit_code"
- DurationMs = "duration_ms"
- Attempt = "attempt"
- TimedOut = "timed_out"
- Platform = "platform"
- EnvVarCount = "env_var_count"

**Implementation Pattern**:
```csharp
public static class CommandLogFields
{
    public const string CommandGroup = "command_group";
    public const string Command = "command";
    // ... etc per spec lines 1109-1120
}
```

**Test File**: `tests/Acode.Domain.Tests/Commands/CommandLogFieldsTests.cs`
**Required Tests**:
1. Verify all 9 constants are defined
2. Verify constant values match specification
3. Verify constants are public and accessible

**Success Criteria**: All constants defined, tests pass, used in logging
**Evidence**: Commit 8d2898a - 10 tests passing, all 9 constants implemented

---

### Gap #2: Application Layer - ICommandParser Interface
**Status**: [✅ COMPLETE]
**File to Create**: `src/Acode.Application/Commands/ICommandParser.cs`
**Why Needed**: Spec line 1029 defines interface for parsing commands from YAML config
**Testing Requirements Reference**: UT-002c-01 through UT-002c-09 require command parsing

**Required Methods**:
```csharp
public interface ICommandParser
{
    // Parse string format: "npm install"
    CommandSpec ParseString(string command);

    // Parse array format: ["npm install", "npm run build"]
    IReadOnlyList<CommandSpec> ParseArray(object[] commands);

    // Parse object format: { run: "npm test", timeout: 60 }
    CommandSpec ParseObject(Dictionary<string, object> commandObject);

    // Parse mixed format (returns list of specs)
    IReadOnlyList<CommandSpec> Parse(object commandValue);
}
```

**Test File**: `tests/Acode.Application.Tests/Commands/CommandParserTests.cs`
**Required Tests** (per Testing Requirements lines 826-851):
- UT-002c-01: Parse string command → Returns CommandSpec
- UT-002c-02: Parse array command → Returns CommandSpec[]
- UT-002c-03: Parse object command → Returns CommandSpec with options
- UT-002c-04: Parse mixed array → Returns mixed CommandSpec[]
- UT-002c-05: Reject empty string → Returns validation error
- UT-002c-06: Accept empty array → Returns empty CommandSpec[]
- UT-002c-07: Reject whitespace-only → Returns validation error
- UT-002c-08: Trim command string → Whitespace removed
- UT-002c-09: Preserve multi-line → Lines preserved

**Success Criteria**: Interface defined, all 9 parsing tests pass
**Evidence**: Commit 3b7204a - Interface defined with Parse/ParseString/ParseArray/ParseObject methods

---

### Gap #3: Application Layer - CommandParser Implementation
**Status**: [✅ COMPLETE]
**File to Create**: `src/Acode.Application/Commands/CommandParser.cs`
**Why Needed**: Implements ICommandParser to convert YAML config to CommandSpec objects
**Dependencies**: Gap #2 must be complete (interface defined)

**Implementation Requirements**:
- Support string format (FR-002c-31, FR-002c-32)
- Support array format (FR-002c-33, FR-002c-34, FR-002c-35)
- Support object format (FR-002c-36 through FR-002c-43)
- Support mixed formats (FR-002c-44)
- Reject empty/whitespace-only strings (FR-002c-45, FR-002c-47)
- Allow empty arrays (FR-002c-46)
- Trim commands (FR-002c-48)
- Support multi-line strings (FR-002c-49)
- Preserve arguments (FR-002c-50)

**Test File**: Same as Gap #2 - `tests/Acode.Application.Tests/Commands/CommandParserTests.cs`

**Success Criteria**: All parsing tests pass, handles all three formats correctly
**Evidence**: Commit 3b7204a - 17/17 tests passing, all formats supported (string, array, object, mixed)

---

### Gap #4: Application Layer - ICommandValidator Interface
**Status**: [ ]
**File to Create**: `src/Acode.Application/Commands/ICommandValidator.cs`
**Why Needed**: Validates CommandSpec objects per FR-002c-51 through FR-002c-95

**Required Methods**:
```csharp
public interface ICommandValidator
{
    // Validate entire CommandSpec
    ValidationResult Validate(CommandSpec spec, string repositoryRoot);

    // Validate working directory (FR-002c-51 through FR-002c-65)
    ValidationResult ValidateWorkingDirectory(string cwd, string repositoryRoot);

    // Validate timeout value (FR-002c-96 through FR-002c-110)
    ValidationResult ValidateTimeout(int timeoutSeconds);

    // Validate retry count
    ValidationResult ValidateRetry(int retryCount);

    // Validate environment variables (FR-002c-66 through FR-002c-80)
    ValidationResult ValidateEnvironment(IReadOnlyDictionary<string, string> env);
}

public record ValidationResult(bool IsValid, string? ErrorMessage);
```

**Test File**: `tests/Acode.Application.Tests/Commands/CommandValidatorTests.cs`
**Required Tests** (per Testing Requirements lines 826-851):
- UT-002c-10: Validate working directory → Path validated
- UT-002c-11: Reject absolute path → Returns error
- UT-002c-12: Reject path traversal → Returns error
- UT-002c-13: Parse environment variables → Env vars extracted
- UT-002c-14: Validate timeout value → Positive integer required
- UT-002c-15: Validate retry value → Non-negative integer required

**Success Criteria**: Interface defined, validation tests pass
**Evidence**: [To be filled when complete]

---

### Gap #5: Application Layer - CommandValidator Implementation
**Status**: [ ]
**File to Create**: `src/Acode.Application/Commands/CommandValidator.cs`
**Why Needed**: Implements validation logic for all CommandSpec properties
**Dependencies**: Gap #4 must be complete (interface defined)

**Validation Rules to Implement**:

**Working Directory (FR-002c-51 through FR-002c-65)**:
- Default to repository root
- Must be relative to repository root
- Reject absolute paths
- Reject path traversal (../)
- Validate directory exists or can be created
- Normalize paths (forward slashes)
- Detect circular symlinks
- Validation must complete in <10ms

**Timeout (FR-002c-96 through FR-002c-110)**:
- Must be non-negative integer
- Zero means no timeout
- Default is 300 seconds

**Retry (FR-002c-103 through FR-002c-110)**:
- Must be non-negative integer
- Default is 0
- Max is 10

**Environment Variables (FR-002c-66 through FR-002c-80)**:
- Names must be valid env var names
- Values must be strings
- Empty values allowed

**Test File**: Same as Gap #4
**Success Criteria**: All validation rules implemented, tests pass
**Evidence**: [To be filled when complete]

---

### Gap #6: Application Layer - RetryPolicy Class
**Status**: [ ]
**File to Create**: `src/Acode.Application/Commands/RetryPolicy.cs`
**Why Needed**: Spec lines 1032, FR-002c-103 through FR-002c-110 define retry behavior

**Required Functionality**:
- Calculate exponential backoff delay
- Base delay: 1 second
- Max delay: 30 seconds
- Formula: min(1 * 2^attempt, 30)

**Implementation Pattern**:
```csharp
public sealed class RetryPolicy
{
    public static TimeSpan CalculateDelay(int attemptNumber)
    {
        // Exponential backoff: 1s, 2s, 4s, 8s, 16s, 30s (capped)
        var seconds = Math.Min(Math.Pow(2, attemptNumber - 1), 30);
        return TimeSpan.FromSeconds(seconds);
    }

    public static bool ShouldRetry(int exitCode, int attemptCount, int maxRetries)
    {
        return exitCode != 0 && attemptCount < maxRetries + 1;
    }
}
```

**Test File**: `tests/Acode.Application.Tests/Commands/RetryPolicyTests.cs`
**Required Tests** (from UT-002c-20):
- Calculate backoff delay for attempt 1 → 1 second
- Calculate backoff delay for attempt 2 → 2 seconds
- Calculate backoff delay for attempt 3 → 4 seconds
- Calculate backoff delay for attempt 4 → 8 seconds
- Calculate backoff delay for attempt 5 → 16 seconds
- Calculate backoff delay for attempt 6 → 30 seconds (capped)
- Should not retry on exit code 0
- Should retry on non-zero exit code within retry limit

**Success Criteria**: Exponential backoff calculated correctly, tests pass
**Evidence**: [To be filled when complete]

---

### Gap #7: Application Layer - TimeoutPolicy Class
**Status**: [ ]
**File to Create**: `src/Acode.Application/Commands/TimeoutPolicy.cs`
**Why Needed**: Spec lines 1032, FR-002c-96 through FR-002c-102 define timeout behavior

**Required Functionality**:
- Default timeout: 300 seconds
- Timeout of 0 means no timeout
- Return exit code 124 on timeout

**Implementation Pattern**:
```csharp
public sealed class TimeoutPolicy
{
    public const int DefaultTimeoutSeconds = 300;
    public const int NoTimeout = 0;

    public static TimeSpan GetTimeout(int timeoutSeconds)
    {
        if (timeoutSeconds == NoTimeout)
            return Timeout.InfiniteTimeSpan;

        return TimeSpan.FromSeconds(timeoutSeconds);
    }

    public static bool IsTimeout(int timeoutSeconds)
    {
        return timeoutSeconds > 0;
    }
}
```

**Test File**: `tests/Acode.Application.Tests/Commands/TimeoutPolicyTests.cs`
**Required Tests**:
- Default timeout is 300 seconds
- Zero timeout returns InfiniteTimeSpan
- Positive timeout returns correct TimeSpan
- IsTimeout returns true for positive values
- IsTimeout returns false for zero

**Success Criteria**: Timeout policy implemented, tests pass
**Evidence**: [To be filled when complete]

---

### Gap #8: Application Layer - Platform Detection
**Status**: [ ]
**File to Create**: `src/Acode.Application/Commands/PlatformDetector.cs`
**Why Needed**: FR-002c-111 through FR-002c-120 require platform-specific command variants

**Required Functionality**:
- Detect current platform: windows, linux, macos
- Select platform variant or fall back to default

**Implementation Pattern**:
```csharp
public static class PlatformDetector
{
    public static string GetCurrentPlatform()
    {
        if (OperatingSystem.IsWindows()) return "windows";
        if (OperatingSystem.IsLinux()) return "linux";
        if (OperatingSystem.IsMacOS()) return "macos";
        throw new PlatformNotSupportedException();
    }

    public static string SelectCommand(string defaultCommand, IReadOnlyDictionary<string, string>? platforms)
    {
        if (platforms == null || platforms.Count == 0)
            return defaultCommand;

        var platform = GetCurrentPlatform();
        return platforms.TryGetValue(platform, out var variant) ? variant : defaultCommand;
    }
}
```

**Test File**: `tests/Acode.Application.Tests/Commands/PlatformDetectorTests.cs`
**Required Tests** (from UT-002c-16, UT-002c-17):
- Detect platform variants → Correct platform selected
- Fall back to default → No variant uses default
- Platform detection is deterministic
- Detection completes in <1ms (PERF-002c-03)

**Success Criteria**: Platform detection working, tests pass, performance met
**Evidence**: [To be filled when complete]

---

### Gap #9: Data File - exit-codes.json
**Status**: [ ]
**File to Create**: `data/exit-codes.json`
**Why Needed**: Spec line 1035 defines exit code descriptions data file

**Content**:
```json
{
  "0": "Success",
  "1": "General error",
  "2": "Misuse of command",
  "124": "Command timed out",
  "126": "Command not executable",
  "127": "Command not found",
  "130": "Interrupted (Ctrl+C)",
  "signal": "Killed by signal {signal_number}"
}
```

**Test**: Verify ExitCodes.GetDescription matches data file
**Success Criteria**: File exists, descriptions match ExitCodes.GetDescription output
**Evidence**: [To be filled when complete]

---

### Gap #10: Application Layer Tests - Setup Test Project
**Status**: [✅ COMPLETE]
**Directory to Create**: `tests/Acode.Application.Tests/Commands/`
**Why Needed**: Testing Requirements specify 25 unit tests, many are Application layer tests

**Setup Steps**:
1. Verify `tests/Acode.Application.Tests/` project exists
2. Create `Commands/` subdirectory
3. Add project reference to `Acode.Application`
4. Add xUnit, FluentAssertions, NSubstitute packages

**Success Criteria**: Test project builds, ready for test files
**Evidence**: Commands directory created, test project verified with dependencies (xUnit, FluentAssertions, NSubstitute)

---

### Gap #11: Integration Tests - Command Parsing and Validation
**Status**: [ ]
**Files to Create**:
- `tests/Acode.Integration.Tests/Commands/CommandParsingIntegrationTests.cs`
- `tests/Acode.Integration.Tests/Commands/CommandValidationIntegrationTests.cs`

**Why Needed**: Testing Requirements IT-002c-01 through IT-002c-15 require integration tests

**Required Integration Tests** (from spec lines 854-872):
- IT-002c-01: Load config with all command groups → All groups accessible
- IT-002c-02: Execute simple command → Exit code 0 (NOTE: May be Epic 2 scope)
- IT-002c-03: Execute failing command → Non-zero exit code (NOTE: May be Epic 2 scope)
- IT-002c-04: Execute command with timeout (NOTE: May be Epic 2 scope)
- IT-002c-05: Execute command with retry (NOTE: May be Epic 2 scope)
- IT-002c-06: Execute command with env vars (NOTE: May be Epic 2 scope)
- IT-002c-07: Execute command with cwd (NOTE: May be Epic 2 scope)
- IT-002c-08: Execute command on Windows (NOTE: May be Epic 2 scope)
- IT-002c-09: Execute command on Linux (NOTE: May be Epic 2 scope)
- IT-002c-10: Execute array of commands (NOTE: May be Epic 2 scope)
- IT-002c-11: Array stops on failure (NOTE: May be Epic 2 scope)
- IT-002c-12: Continue on error (NOTE: May be Epic 2 scope)
- IT-002c-13: Platform variant selected → Correct variant used
- IT-002c-14: Output captured (NOTE: May be Epic 2 scope)
- IT-002c-15: Large output handled (NOTE: May be Epic 2 scope)

**NOTE**: Many integration tests involve EXECUTION, which may be Epic 2 scope per spec line 48.
For task 002c, focus on:
- IT-002c-01: Loading and parsing config
- IT-002c-13: Platform variant selection

**Success Criteria**: Integration tests for parsing/validation pass
**Evidence**: [To be filled when complete]

---

### Gap #12: Additional Unit Tests - Fill Coverage Gaps
**Status**: [ ]
**Files to Update**: Various test files
**Why Needed**: Testing Requirements specify 25 unit tests (UT-002c-01 to UT-002c-25), currently have ~36 tests but may not cover all spec requirements

**Missing Unit Tests to Add**:
- UT-002c-21: Command equality → Equal specs are equal
- UT-002c-22: Command serialization → Serializes to JSON
- UT-002c-23: All groups parseable → All six groups parse
- UT-002c-24: Missing group handled → Returns null/error appropriately
- UT-002c-25: Command validation → Invalid commands rejected

**Test Files to Update**:
- `tests/Acode.Domain.Tests/Commands/CommandSpecTests.cs` - add equality and serialization tests
- `tests/Acode.Application.Tests/Commands/CommandParserTests.cs` - add group parsing tests
- `tests/Acode.Application.Tests/Commands/CommandValidatorTests.cs` - add validation tests

**Success Criteria**: All 25 unit tests from spec implemented and passing
**Evidence**: [To be filled when complete]

---

### Gap #13: Documentation - Update User Manual
**Status**: [ ]
**File to Update**: Update README or create user guide based on spec lines 363-603
**Why Needed**: Users need documentation on command group configuration

**Required Documentation Sections** (from spec User Manual Documentation):
1. Command Groups Overview (lines 366-376)
2. Configuration Syntax (lines 378-427)
3. Command Object Properties (lines 429-438)
4. Platform-Specific Commands (lines 440-450)
5. Environment Variables (lines 452-472)
6. Timeouts and Retries (lines 474-498)
7. Exit Codes (lines 500-510)
8. CLI Usage examples (lines 512-533)
9. Best Practices (lines 535-542)
10. Troubleshooting (lines 544-602)

**Success Criteria**: Documentation complete, examples accurate, troubleshooting comprehensive
**Evidence**: [To be filled when complete]

---

## Implementation Order

Follow TDD strictly - write tests first for each gap:

1. **Gap #1** - CommandLogFields (simple, sets up logging constants)
2. **Gap #10** - Setup Application.Tests project (prerequisite for remaining gaps)
3. **Gap #2 + #3** - ICommandParser interface and implementation (with tests first)
4. **Gap #4 + #5** - ICommandValidator interface and implementation (with tests first)
5. **Gap #6** - RetryPolicy (with tests first)
6. **Gap #7** - TimeoutPolicy (with tests first)
7. **Gap #8** - PlatformDetector (with tests first)
8. **Gap #9** - exit-codes.json data file
9. **Gap #11** - Integration tests for parsing/validation
10. **Gap #12** - Fill remaining unit test coverage gaps
11. **Gap #13** - Documentation updates

## Notes

- Many integration and E2E tests involve command EXECUTION, which is Epic 2 scope (spec line 48: "Command execution implementation (Epic 2)")
- For task 002c, focus on PARSING, VALIDATION, and SPECIFICATION definition
- Execution-related tests (IT-002c-02 through IT-002c-12) will be implemented when Epic 2 tasks are assigned
- Performance benchmarks (PERF-002c-01 through PERF-002c-08) may also be Epic 2 scope, except parsing/validation performance

## Completion Criteria

Task 002c is complete when:
- [ ] All 13 gaps filled
- [ ] All tests passing (`dotnet test`)
- [ ] Build succeeds with no warnings
- [ ] Documentation complete
- [ ] Security audit passed per `docs/AUDIT-GUIDELINES.md`
- [ ] PR created and ready for review
