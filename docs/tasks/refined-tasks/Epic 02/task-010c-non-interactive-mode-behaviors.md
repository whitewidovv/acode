# Task 010.c: Non-Interactive Mode Behaviors

**Priority:** P0 – Critical Path  
**Tier:** Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 010 (CLI Framework), Task 010.a (Command Routing), Task 010.b (JSONL Mode)  

---

## Description

Task 010.c implements non-interactive mode behaviors for the Acode CLI, enabling fully automated operation without human input. Non-interactive mode is essential for CI/CD pipelines, scheduled automation, remote execution, and any context where interactive prompts are not possible or desirable.

Non-interactive mode fundamentally changes how the CLI handles decisions that would normally require user input. Interactive mode prompts users for approval, confirmation, or choices. Non-interactive mode must either have pre-configured answers to these prompts or fail gracefully when user input is required. This distinction is critical for automation reliability.

The CLI auto-detects non-interactive contexts. When stdin is not a TTY, the CLI assumes non-interactive operation. When stdout is not a TTY, output formatting changes. Environment variables can also signal non-interactive mode explicitly. This detection ensures appropriate behavior without requiring users to always specify flags.

Approval handling in non-interactive mode follows configurable policies. The `--yes` flag auto-approves all prompts. Alternatively, `--approval-policy` can specify granular rules: approve low-risk actions, reject high-risk, or fail requiring explicit pre-authorization. This flexibility balances automation needs with safety requirements.

Timeout handling prevents automation from hanging indefinitely. Every operation that might wait for input has a configurable timeout. When timeouts expire, the operation fails with a clear error code. Timeouts are set via flags, environment variables, or configuration file with appropriate precedence.

Exit codes in non-interactive mode provide rich status information. Different failure modes have different codes: user input required (code 10), timeout expired (code 11), approval denied (code 12). Automation can respond appropriately to different failure types—retrying on transient issues, alerting on policy violations.

Progress output adapts to non-interactive contexts. Spinner animations are disabled. Progress bars use simple percentage output. Status updates are less frequent to reduce log noise. The goal is clean, parseable output suitable for log files and monitoring systems.

Logging becomes the primary observability mechanism in non-interactive mode. Without the ability to display transient UI elements, all status information goes to logs. Log levels are respected, with INFO providing adequate visibility for normal operation and DEBUG available for troubleshooting.

Integration with CI/CD systems requires consideration of specific environments. GitHub Actions, GitLab CI, Azure DevOps, and Jenkins all have detection mechanisms. The CLI recognizes these environments and applies appropriate defaults. Environment-specific behavior differences are documented.

Error handling prioritizes clear failure diagnosis. When automation fails, operators need to understand why quickly. Error messages include all context needed for diagnosis. Stack traces are included at DEBUG level. Suggestions for remediation are included where applicable.

Signal handling in non-interactive mode differs from interactive. SIGINT still triggers graceful shutdown, but without confirmation prompts. SIGTERM has a shorter grace period. SIGPIPE is handled to prevent crashes when piped output is terminated. These behaviors align with Unix automation conventions.

Configuration for non-interactive mode can be persisted. Rather than specifying flags on every invocation, operators can configure defaults appropriate for their automation environment. Environment variables provide another persistence mechanism suitable for containerized deployments.

Testing non-interactive mode requires simulating non-TTY contexts. Test infrastructure mocks stdin/stdout as pipes rather than terminals. CI/CD environment detection is tested by setting appropriate environment variables. Coverage must include all decision points that differ between interactive and non-interactive modes.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Interactive Mode | User provides input via TTY |
| Non-Interactive Mode | No user input available |
| TTY | Terminal/teletype device |
| stdin | Standard input stream |
| stdout | Standard output stream |
| Batch Mode | Synonym for non-interactive |
| CI/CD | Continuous Integration/Deployment |
| Approval Policy | Rules for auto-approving actions |
| Timeout | Maximum wait duration |
| Exit Code | Numeric completion status |
| SIGINT | Interrupt signal (Ctrl+C) |
| SIGTERM | Termination signal |
| SIGPIPE | Broken pipe signal |
| Environment Detection | Identifying CI/CD platforms |
| Auto-Approve | Automatic approval without prompt |

---

## Out of Scope

The following items are explicitly excluded from Task 010.c:

- **Remote control interfaces** - Local execution only
- **Queue-based task submission** - Direct invocation only
- **Notification integrations** - Exit codes only
- **Webhook callbacks** - Post-MVP
- **Retry orchestration** - Caller responsibility
- **Distributed execution** - Single process only
- **Container orchestration** - Direct execution
- **Credential injection** - Environment variables only
- **Secret management services** - Plain config only
- **Multi-tenant isolation** - Single user context

---

## Functional Requirements

### Mode Detection

- FR-001: MUST detect non-interactive when stdin is not TTY
- FR-002: MUST detect non-interactive when stdout is not TTY
- FR-003: --non-interactive MUST force non-interactive mode
- FR-004: ACODE_NON_INTERACTIVE=1 MUST force mode
- FR-005: CI=true MUST trigger non-interactive mode
- FR-006: Mode MUST be determined at startup
- FR-007: Mode MUST NOT change during execution
- FR-008: Mode MUST be logged at startup

### CI/CD Environment Detection

- FR-009: GitHub Actions MUST be detected (GITHUB_ACTIONS)
- FR-010: GitLab CI MUST be detected (GITLAB_CI)
- FR-011: Azure DevOps MUST be detected (TF_BUILD)
- FR-012: Jenkins MUST be detected (JENKINS_URL)
- FR-013: CircleCI MUST be detected (CIRCLECI)
- FR-014: Travis CI MUST be detected (TRAVIS)
- FR-015: Bitbucket MUST be detected (BITBUCKET_BUILD_NUMBER)
- FR-016: Detected environment MUST be logged

### Approval Handling

- FR-017: --yes MUST auto-approve all prompts
- FR-018: --no-approve MUST reject all prompts
- FR-019: --approval-policy MUST accept policy name
- FR-020: Policy "none" MUST reject all approvals
- FR-021: Policy "low-risk" MUST approve low-risk only
- FR-022: Policy "all" MUST approve all actions
- FR-023: Approval decisions MUST be logged
- FR-024: Missing approval in non-interactive MUST fail

### Timeout Configuration

- FR-025: --timeout MUST set global timeout
- FR-026: ACODE_TIMEOUT MUST set via environment
- FR-027: Default timeout MUST be 3600 seconds (1 hour)
- FR-028: Timeout 0 MUST mean no timeout
- FR-029: Timeout expiry MUST exit with code 11
- FR-030: Remaining time MUST be logged periodically
- FR-031: Individual operations MAY have sub-timeouts
- FR-032: Timeout MUST trigger graceful shutdown

### Input Handling

- FR-033: No prompts MUST be shown in non-interactive
- FR-034: Required input without default MUST fail
- FR-035: Failure MUST indicate what input was required
- FR-036: Exit code MUST be 10 for input required
- FR-037: Preconfig via args/env MUST satisfy input needs
- FR-038: Prompt text MUST be logged for diagnostics

### Output Formatting

- FR-039: Spinners MUST be disabled
- FR-040: Progress bars MUST use simple format
- FR-041: Colors MUST be disabled
- FR-042: Tables MUST use simple text format
- FR-043: Status updates MUST be less frequent
- FR-044: Output MUST be line-oriented

### Progress Reporting

- FR-045: Progress MUST go to stderr
- FR-046: Progress MUST include timestamp
- FR-047: Progress MUST be machine-parseable
- FR-048: Progress frequency MUST be configurable
- FR-049: Default progress interval: 10 seconds
- FR-050: --quiet MUST suppress progress

### Exit Codes

- FR-051: 0 MUST indicate success
- FR-052: 1 MUST indicate general error
- FR-053: 10 MUST indicate input required
- FR-054: 11 MUST indicate timeout
- FR-055: 12 MUST indicate approval denied
- FR-056: 13 MUST indicate pre-flight check failed
- FR-057: Exit code MUST be logged at shutdown

### Signal Handling

- FR-058: SIGINT MUST trigger graceful shutdown
- FR-059: SIGTERM MUST trigger graceful shutdown
- FR-060: SIGPIPE MUST NOT crash
- FR-061: Graceful shutdown MUST complete pending writes
- FR-062: Shutdown MUST have maximum duration (30s)
- FR-063: Force kill after shutdown timeout

### Logging Behavior

- FR-064: All decisions MUST be logged
- FR-065: All failures MUST be logged with context
- FR-066: Log level MUST respect configuration
- FR-067: Default log level: INFO
- FR-068: Logs MUST go to stderr
- FR-069: Logs MUST include timestamp
- FR-070: Logs MUST include severity

### Pre-flight Checks

- FR-071: MUST verify required config before start
- FR-072: MUST verify model availability
- FR-073: MUST verify file permissions
- FR-074: Pre-flight failures MUST exit code 13
- FR-075: Pre-flight MUST list all failures at once
- FR-076: --skip-preflight MUST bypass checks

### Configuration Precedence

- FR-077: CLI args MUST override all
- FR-078: Environment vars MUST override config file
- FR-079: CI detection MUST set sensible defaults
- FR-080: Defaults MUST be documented

---

## Non-Functional Requirements

### Performance

- NFR-001: Mode detection MUST complete in < 10ms
- NFR-002: Pre-flight checks MUST complete in < 5s
- NFR-003: Shutdown MUST complete in < 30s
- NFR-004: Progress updates MUST NOT impact performance

### Reliability

- NFR-005: MUST NOT hang waiting for input
- NFR-006: MUST NOT corrupt output on SIGPIPE
- NFR-007: MUST cleanup on any termination
- NFR-008: Partial failures MUST be recoverable

### Security

- NFR-009: --yes MUST require explicit flag
- NFR-010: Auto-approve MUST NOT reveal secrets
- NFR-011: Timeouts MUST NOT leak timing info
- NFR-012: Log redaction MUST apply

### Compatibility

- NFR-013: Linux/macOS/Windows MUST be supported
- NFR-014: All major CI/CD platforms MUST work
- NFR-015: Shell scripts MUST be able to parse output

### Observability

- NFR-016: All mode decisions MUST be logged
- NFR-017: All timeouts MUST be logged
- NFR-018: All approvals/rejections MUST be logged
- NFR-019: Exit codes MUST be logged

---

## User Manual Documentation

### Overview

Non-interactive mode enables Acode to run in environments without user input, such as CI/CD pipelines, scheduled jobs, and remote automation. This guide covers detection, configuration, and troubleshooting.

### Quick Start

```bash
# Most CI/CD environments auto-detect
$ acode run "Add tests"

# Explicitly force non-interactive
$ acode run --non-interactive "Add tests"

# Auto-approve all actions
$ acode run --yes "Add tests"

# With timeout
$ acode run --timeout 600 "Add tests"
```

### Mode Detection

Acode automatically detects non-interactive mode when:

1. **stdin is not a TTY** (piped or redirected)
2. **stdout is not a TTY** (piped or redirected)
3. **CI environment variable is set**
4. **Platform-specific CI variable detected**

```bash
# These all trigger non-interactive mode
$ echo "task" | acode run
$ acode run "task" | tee log.txt
$ CI=true acode run "task"
```

### CI/CD Platform Detection

| Platform | Detection Variable | Auto-Detected |
|----------|-------------------|---------------|
| GitHub Actions | GITHUB_ACTIONS=true | ✓ |
| GitLab CI | GITLAB_CI=true | ✓ |
| Azure DevOps | TF_BUILD=True | ✓ |
| Jenkins | JENKINS_URL set | ✓ |
| CircleCI | CIRCLECI=true | ✓ |
| Travis CI | TRAVIS=true | ✓ |
| Bitbucket | BITBUCKET_BUILD_NUMBER set | ✓ |

### Approval Handling

#### Auto-Approve All

```bash
# Approve everything (use with caution)
$ acode run --yes "Refactor module"

# Equivalent
$ acode run -y "Refactor module"
```

#### Approval Policies

```bash
# No automatic approvals (fail if needed)
$ acode run --approval-policy none "task"

# Approve low-risk actions only
$ acode run --approval-policy low-risk "task"

# Approve all actions
$ acode run --approval-policy all "task"
```

#### Risk Levels

| Risk Level | Examples | Default Policy |
|------------|----------|----------------|
| Low | Read files, list directory | Auto-approve |
| Medium | Write files, create files | Prompt/configurable |
| High | Delete files, execute commands | Require explicit |
| Critical | Git operations, external calls | Always require |

### Timeout Configuration

#### Command Line

```bash
# 10 minute timeout
$ acode run --timeout 600 "task"

# No timeout
$ acode run --timeout 0 "task"
```

#### Environment Variable

```bash
# Set timeout via environment
$ export ACODE_TIMEOUT=600
$ acode run "task"
```

#### Configuration File

```yaml
# .agent/config.yml
non_interactive:
  timeout_seconds: 600
  approval_policy: low-risk
```

### Exit Codes

| Code | Meaning | Action |
|------|---------|--------|
| 0 | Success | None |
| 1 | General error | Check logs |
| 10 | Input required | Provide via args/env |
| 11 | Timeout | Increase timeout or optimize |
| 12 | Approval denied | Review policy or pre-authorize |
| 13 | Pre-flight failed | Fix configuration |
| 130 | Interrupted | Check for signals |

### Pre-flight Checks

Before starting, Acode verifies:

1. Configuration is valid
2. Required models are available
3. Working directory is writable
4. Required tools are present

```bash
# Skip pre-flight checks
$ acode run --skip-preflight "task"

# Pre-flight failures exit with code 13
$ acode run "task"
[PREFLIGHT] Model llama3.2:7b not available
[PREFLIGHT] Cannot write to ./output
Exit code: 13
```

### Progress Output

In non-interactive mode, progress uses simple format:

```
[2024-01-15T10:30:00Z] [INFO] Starting: Add validation
[2024-01-15T10:30:05Z] [INFO] Progress: 20% - Analyzing codebase
[2024-01-15T10:30:15Z] [INFO] Progress: 40% - Planning changes
[2024-01-15T10:30:30Z] [INFO] Progress: 60% - Implementing
[2024-01-15T10:30:45Z] [INFO] Progress: 80% - Testing
[2024-01-15T10:31:00Z] [INFO] Complete: Success
```

Configure progress interval:

```bash
# Update every 30 seconds
$ acode run --progress-interval 30 "task"

# Suppress progress
$ acode run --quiet "task"
```

### Signal Handling

| Signal | Behavior |
|--------|----------|
| SIGINT (Ctrl+C) | Graceful shutdown, cleanup |
| SIGTERM | Graceful shutdown, cleanup |
| SIGPIPE | Silent handling, no crash |
| SIGKILL | Immediate termination |

Graceful shutdown:
1. Stop accepting new work
2. Complete current operation (max 30s)
3. Write pending output
4. Exit with appropriate code

### Configuration

#### Full Non-Interactive Config

```yaml
# .agent/config.yml
non_interactive:
  # Force non-interactive mode
  enabled: true
  
  # Timeout in seconds (0 = no timeout)
  timeout_seconds: 3600
  
  # Approval policy: none, low-risk, all
  approval_policy: low-risk
  
  # Progress update interval
  progress_interval_seconds: 10
  
  # Skip pre-flight checks
  skip_preflight: false
  
  # Graceful shutdown timeout
  shutdown_timeout_seconds: 30
```

#### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| ACODE_NON_INTERACTIVE | Force non-interactive | auto |
| ACODE_TIMEOUT | Global timeout seconds | 3600 |
| ACODE_APPROVAL_POLICY | Approval policy | prompt |
| ACODE_PROGRESS_INTERVAL | Progress interval | 10 |
| ACODE_SKIP_PREFLIGHT | Skip pre-flight | false |

### CI/CD Examples

#### GitHub Actions

```yaml
# .github/workflows/acode.yml
name: Acode Run
on: push

jobs:
  run:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Run Acode
        run: |
          acode run --yes --timeout 600 "Add tests for new code"
        env:
          ACODE_APPROVAL_POLICY: low-risk
```

#### GitLab CI

```yaml
# .gitlab-ci.yml
acode:
  script:
    - acode run --yes "Review and improve code"
  timeout: 1h
  variables:
    ACODE_TIMEOUT: "3600"
```

#### Azure DevOps

```yaml
# azure-pipelines.yml
steps:
  - script: |
      acode run --yes --timeout 1800 "$(TASK_DESCRIPTION)"
    env:
      ACODE_NON_INTERACTIVE: "1"
```

### Best Practices

1. **Always use timeouts**: Prevent runaway automation
2. **Use specific approval policies**: Don't --yes everything
3. **Check exit codes**: Handle failures appropriately
4. **Capture logs**: Route stderr to log storage
5. **Pre-authorize known operations**: Reduce approval prompts
6. **Test locally first**: Verify behavior before CI/CD

### Troubleshooting

#### Exit Code 10 (Input Required)

**Problem:** Acode needs input not provided.

**Solution:**
```bash
# Check what input was needed
$ ACODE_LOG_LEVEL=debug acode run "task"

# Provide via argument
$ acode run --model llama3.2:7b "task"

# Provide via environment
$ ACODE_MODEL=llama3.2:7b acode run "task"
```

#### Exit Code 11 (Timeout)

**Problem:** Operation exceeded timeout.

**Solutions:**
1. Increase timeout: `--timeout 7200`
2. Break task into smaller pieces
3. Check for stuck operations in logs

#### Exit Code 12 (Approval Denied)

**Problem:** Action required approval not granted.

**Solutions:**
1. Use `--yes` for auto-approve
2. Use `--approval-policy low-risk`
3. Pre-authorize actions in config

#### Exit Code 13 (Pre-flight Failed)

**Problem:** Pre-flight checks found issues.

**Solutions:**
1. Check logs for specific failures
2. Fix configuration/environment
3. Use `--skip-preflight` (not recommended)

---

## Acceptance Criteria

### Mode Detection

- [ ] AC-001: Detects non-TTY stdin
- [ ] AC-002: Detects non-TTY stdout
- [ ] AC-003: --non-interactive flag works
- [ ] AC-004: ACODE_NON_INTERACTIVE env works
- [ ] AC-005: CI=true triggers mode
- [ ] AC-006: Mode determined at startup
- [ ] AC-007: Mode logged at startup

### CI/CD Detection

- [ ] AC-008: GitHub Actions detected
- [ ] AC-009: GitLab CI detected
- [ ] AC-010: Azure DevOps detected
- [ ] AC-011: Jenkins detected
- [ ] AC-012: CircleCI detected
- [ ] AC-013: Travis CI detected
- [ ] AC-014: Bitbucket detected
- [ ] AC-015: Environment logged

### Approvals

- [ ] AC-016: --yes auto-approves
- [ ] AC-017: --no-approve rejects
- [ ] AC-018: --approval-policy works
- [ ] AC-019: Policy "none" rejects all
- [ ] AC-020: Policy "low-risk" approves low only
- [ ] AC-021: Policy "all" approves all
- [ ] AC-022: Decisions logged
- [ ] AC-023: Missing approval fails

### Timeouts

- [ ] AC-024: --timeout flag works
- [ ] AC-025: ACODE_TIMEOUT env works
- [ ] AC-026: Default is 3600s
- [ ] AC-027: Timeout 0 = no timeout
- [ ] AC-028: Expiry exits code 11
- [ ] AC-029: Remaining time logged
- [ ] AC-030: Graceful shutdown on timeout

### Input Handling

- [ ] AC-031: No prompts in non-interactive
- [ ] AC-032: Missing required fails
- [ ] AC-033: Failure indicates what needed
- [ ] AC-034: Exit code 10 used
- [ ] AC-035: Args/env satisfy needs
- [ ] AC-036: Prompt text logged

### Output

- [ ] AC-037: Spinners disabled
- [ ] AC-038: Progress bars simple
- [ ] AC-039: Colors disabled
- [ ] AC-040: Tables simple format
- [ ] AC-041: Updates less frequent
- [ ] AC-042: Line-oriented output

### Progress

- [ ] AC-043: Progress to stderr
- [ ] AC-044: Timestamps included
- [ ] AC-045: Machine-parseable
- [ ] AC-046: Frequency configurable
- [ ] AC-047: Default 10 seconds
- [ ] AC-048: --quiet suppresses

### Exit Codes

- [ ] AC-049: 0 = success
- [ ] AC-050: 1 = error
- [ ] AC-051: 10 = input required
- [ ] AC-052: 11 = timeout
- [ ] AC-053: 12 = approval denied
- [ ] AC-054: 13 = pre-flight failed
- [ ] AC-055: Exit logged

### Signals

- [ ] AC-056: SIGINT graceful
- [ ] AC-057: SIGTERM graceful
- [ ] AC-058: SIGPIPE handled
- [ ] AC-059: Pending writes completed
- [ ] AC-060: 30s shutdown max
- [ ] AC-061: Force kill after timeout

### Logging

- [ ] AC-062: Decisions logged
- [ ] AC-063: Failures logged with context
- [ ] AC-064: Level respected
- [ ] AC-065: Default INFO
- [ ] AC-066: Stderr used
- [ ] AC-067: Timestamps included

### Pre-flight

- [ ] AC-068: Config verified
- [ ] AC-069: Models checked
- [ ] AC-070: Permissions checked
- [ ] AC-071: Code 13 on failure
- [ ] AC-072: All failures listed
- [ ] AC-073: --skip-preflight works

### Performance

- [ ] AC-074: Detection < 10ms
- [ ] AC-075: Pre-flight < 5s
- [ ] AC-076: Shutdown < 30s

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/CLI/NonInteractive/
├── ModeDetectorTests.cs
│   ├── Should_Detect_NonTTY_Stdin()
│   ├── Should_Detect_NonTTY_Stdout()
│   ├── Should_Detect_CI_Variable()
│   └── Should_Honor_Flag()
│
├── CIEnvironmentTests.cs
│   ├── Should_Detect_GitHub_Actions()
│   ├── Should_Detect_GitLab_CI()
│   ├── Should_Detect_Azure_DevOps()
│   └── Should_Detect_Jenkins()
│
├── ApprovalPolicyTests.cs
│   ├── Should_Apply_None_Policy()
│   ├── Should_Apply_LowRisk_Policy()
│   ├── Should_Apply_All_Policy()
│   └── Should_Honor_Yes_Flag()
│
├── TimeoutTests.cs
│   ├── Should_Timeout_After_Duration()
│   ├── Should_Exit_Code_11()
│   └── Should_Gracefully_Shutdown()
│
└── SignalHandlerTests.cs
    ├── Should_Handle_SIGINT()
    ├── Should_Handle_SIGTERM()
    └── Should_Handle_SIGPIPE()
```

### Integration Tests

```
Tests/Integration/CLI/NonInteractive/
├── NonInteractiveRunTests.cs
│   ├── Should_Run_Without_Prompts()
│   ├── Should_Fail_On_Missing_Input()
│   └── Should_Auto_Approve_With_Yes()
│
├── TimeoutIntegrationTests.cs
│   ├── Should_Timeout_Long_Operation()
│   └── Should_Complete_Before_Timeout()
│
└── PreflightTests.cs
    ├── Should_Fail_On_Missing_Config()
    └── Should_Fail_On_Missing_Model()
```

### E2E Tests

```
Tests/E2E/CLI/NonInteractive/
├── CICDSimulationTests.cs
│   ├── Should_Run_In_GitHub_Actions_Env()
│   ├── Should_Run_In_GitLab_CI_Env()
│   └── Should_Handle_Pipeline_Cancel()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Mode detection | 5ms | 10ms |
| Pre-flight checks | 2s | 5s |
| Graceful shutdown | 15s | 30s |
| Signal handling | 10ms | 50ms |

### Regression Tests

- Mode detection after TTY library update
- Signal handling after platform changes
- Exit codes after error handling changes

---

## User Verification Steps

### Scenario 1: Auto-Detect Pipe

1. Run `echo "task" | acode run`
2. Verify: Non-interactive mode detected
3. Verify: No prompts shown

### Scenario 2: Force Non-Interactive

1. Run `acode run --non-interactive "task"`
2. Verify: Mode forced
3. Verify: Logged at startup

### Scenario 3: CI Environment

1. Set CI=true
2. Run `acode run "task"`
3. Verify: Non-interactive mode
4. Verify: CI detected and logged

### Scenario 4: Auto-Approve

1. Run `acode run --yes "task"`
2. Verify: No approval prompts
3. Verify: All actions approved

### Scenario 5: Approval Policy

1. Run `acode run --approval-policy low-risk "task"`
2. Verify: Low-risk approved
3. Verify: High-risk fails

### Scenario 6: Timeout Trigger

1. Run `acode run --timeout 5 "long task"`
2. Verify: Timeout after 5s
3. Verify: Exit code 11

### Scenario 7: Input Required

1. Run with missing required input
2. Verify: Exit code 10
3. Verify: Error indicates what needed

### Scenario 8: Pre-flight Failure

1. Configure invalid model
2. Run `acode run "task"`
3. Verify: Exit code 13
4. Verify: Failure message clear

### Scenario 9: Progress Output

1. Run `acode run "task"`
2. Verify: Progress to stderr
3. Verify: Timestamps present

### Scenario 10: Quiet Mode

1. Run `acode run --quiet "task"`
2. Verify: No progress output
3. Verify: Only final result

### Scenario 11: SIGINT Handling

1. Start `acode run "task"`
2. Send SIGINT (Ctrl+C)
3. Verify: Graceful shutdown
4. Verify: Exit code 130

### Scenario 12: GitHub Actions

1. Set GITHUB_ACTIONS=true
2. Run `acode run "task"`
3. Verify: GitHub detected
4. Verify: Appropriate defaults

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.CLI/
├── NonInteractive/
│   ├── IModeDetector.cs
│   ├── ModeDetector.cs
│   ├── CIEnvironmentDetector.cs
│   ├── IApprovalPolicy.cs
│   ├── ApprovalPolicyFactory.cs
│   ├── TimeoutManager.cs
│   ├── SignalHandler.cs
│   └── PreflightChecker.cs
│
├── Progress/
│   ├── IProgressReporter.cs
│   ├── NonInteractiveProgressReporter.cs
│   └── ProgressInterval.cs
│
└── Configuration/
    └── NonInteractiveOptions.cs
```

### IModeDetector Interface

```csharp
namespace AgenticCoder.CLI.NonInteractive;

public interface IModeDetector
{
    bool IsInteractive { get; }
    bool IsTTY { get; }
    CIEnvironment? DetectedCIEnvironment { get; }
    void Initialize();
}

public enum CIEnvironment
{
    GitHubActions,
    GitLabCI,
    AzureDevOps,
    Jenkins,
    CircleCI,
    TravisCI,
    Bitbucket,
    Generic
}
```

### IApprovalPolicy Interface

```csharp
namespace AgenticCoder.CLI.NonInteractive;

public interface IApprovalPolicy
{
    string Name { get; }
    ApprovalDecision Evaluate(ApprovalRequest request);
}

public enum ApprovalDecision
{
    Approve,
    Reject,
    RequireExplicit
}

public sealed record ApprovalRequest(
    string ActionType,
    RiskLevel RiskLevel,
    Dictionary<string, object> Context);

public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}
```

### TimeoutManager

```csharp
namespace AgenticCoder.CLI.NonInteractive;

public sealed class TimeoutManager : IDisposable
{
    public TimeSpan Timeout { get; }
    public TimeSpan Remaining { get; }
    public bool IsExpired { get; }
    
    public void Start();
    public void Cancel();
    public Task WaitAsync(CancellationToken ct);
}
```

### SignalHandler

```csharp
namespace AgenticCoder.CLI.NonInteractive;

public sealed class SignalHandler
{
    public event EventHandler<SignalEventArgs>? SignalReceived;
    
    public void Register();
    public void Unregister();
    public Task WaitForShutdownAsync(TimeSpan timeout);
}
```

### Exit Codes

| Code | Constant | Condition |
|------|----------|-----------|
| 10 | ExitCode.InputRequired | Input needed not provided |
| 11 | ExitCode.Timeout | Timeout expired |
| 12 | ExitCode.ApprovalDenied | Approval not granted |
| 13 | ExitCode.PreflightFailed | Pre-flight check failed |

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-NI-001 | Non-interactive input required |
| ACODE-NI-002 | Timeout expired |
| ACODE-NI-003 | Approval denied by policy |
| ACODE-NI-004 | Pre-flight check failed |
| ACODE-NI-005 | Signal received during operation |

### Logging Fields

```json
{
  "event": "mode_detected",
  "is_interactive": false,
  "is_tty": false,
  "ci_environment": "github_actions",
  "approval_policy": "low-risk",
  "timeout_seconds": 3600
}
```

### Configuration Defaults

| Setting | Default | CLI Flag | Env Var |
|---------|---------|----------|---------|
| non_interactive | auto | --non-interactive | ACODE_NON_INTERACTIVE |
| timeout_seconds | 3600 | --timeout | ACODE_TIMEOUT |
| approval_policy | prompt | --approval-policy | ACODE_APPROVAL_POLICY |
| progress_interval | 10 | --progress-interval | ACODE_PROGRESS_INTERVAL |
| skip_preflight | false | --skip-preflight | ACODE_SKIP_PREFLIGHT |

### Implementation Checklist

1. [ ] Create IModeDetector interface
2. [ ] Implement ModeDetector with TTY check
3. [ ] Implement CIEnvironmentDetector
4. [ ] Create IApprovalPolicy interface
5. [ ] Implement policy variants (none, low-risk, all)
6. [ ] Create ApprovalPolicyFactory
7. [ ] Implement TimeoutManager
8. [ ] Implement SignalHandler
9. [ ] Implement PreflightChecker
10. [ ] Create NonInteractiveProgressReporter
11. [ ] Add exit code constants
12. [ ] Wire up configuration precedence
13. [ ] Write mode detection tests
14. [ ] Write approval policy tests
15. [ ] Write timeout tests
16. [ ] Write signal handling tests

### Validation Checklist Before Merge

- [ ] All CI environments detected correctly
- [ ] All approval policies work as specified
- [ ] Timeout triggers graceful shutdown
- [ ] All exit codes documented and used
- [ ] Signals handled without crash
- [ ] Pre-flight catches all issues
- [ ] Progress output is clean
- [ ] Unit test coverage > 90%
- [ ] Works on Windows/Linux/macOS

### Rollout Plan

1. **Phase 1:** Mode detection and CI environment
2. **Phase 2:** Approval policies
3. **Phase 3:** Timeout management
4. **Phase 4:** Signal handling
5. **Phase 5:** Pre-flight checks
6. **Phase 6:** Progress reporting
7. **Phase 7:** Full integration testing

---

**End of Task 010.c Specification**