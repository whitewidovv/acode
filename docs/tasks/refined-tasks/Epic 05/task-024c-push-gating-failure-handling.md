# Task 024.c: push gating + failure handling

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 5 – Git Integration Layer  
**Dependencies:** Task 024 (Safe Workflow), Task 022.c (push), Task 001 (Modes)  

---

## Description

Task 024.c implements push gating and failure handling. Before pushing, configurable gates MUST be evaluated. Failed gates MUST block the push. Push failures MUST be handled with retries.

Push gates MUST verify local state before network operations. This includes ensuring pre-commit verification passed, branch is up-to-date, and no conflicting changes exist.

Failure handling MUST support automatic retries. Network errors MUST trigger retry with exponential backoff. Authentication failures MUST NOT retry. Rejection (non-fast-forward) MUST suggest remediation.

Operating mode compliance MUST be enforced. Push MUST be blocked in local-only and airgapped modes. Mode violations MUST produce clear errors.

### Business Value

Push gating prevents broken code from reaching remotes. Automatic retries handle transient network issues. Clear failure messages enable quick remediation.

### Scope Boundaries

This task covers push gating and error handling. Pre-commit verification is in 024.a. Message validation is in 024.b.

### Integration Points

- Task 024: Workflow orchestration
- Task 022.c: Push operations
- Task 001: Operating mode validation
- Task 002: Configuration

### Failure Modes

- Gate fails → Block push, show failures
- Network timeout → Retry with backoff
- Auth failure → Report, no retry
- Non-fast-forward → Suggest pull/rebase
- Mode violation → Clear error

---

## Assumptions

### Technical Assumptions

1. **Git remote access** - Can query remote refs
2. **Operating mode known** - Task 001 provides mode context
3. **Network available** - For remote checks (unless airgapped)
4. **Push operations ready** - Task 022.c provides push
5. **Pre-commit results available** - Can check if verification passed

### Gate Assumptions

6. **Gates are independent** - Each gate evaluates separately
7. **All gates must pass** - By default, all required
8. **Gates can be disabled** - Individual gates configurable
9. **Order is deterministic** - Same gates run same order
10. **Fast gates first** - Local checks before network checks

### Failure Handling Assumptions

11. **Retry on transient** - Network failures get retry
12. **No retry on auth** - Authentication failures are final
13. **Clear failure reasons** - User knows why push blocked
14. **Suggested actions** - Help user resolve issues

---

## Functional Requirements

### FR-001 to FR-025: Gate Evaluation

- FR-001: `IPushGate` interface MUST be defined
- FR-002: `EvaluateAsync` MUST check all gates
- FR-003: Result MUST indicate pass/fail
- FR-004: Result MUST include failed gates
- FR-005: `preCommitPassed` gate MUST verify verification ran
- FR-006: `branchUpToDate` gate MUST check remote tracking
- FR-007: `noConflicts` gate MUST check for conflicts
- FR-008: `modeAllowed` gate MUST check operating mode
- FR-009: Gates MUST be configurable
- FR-010: `requireAllChecks` MUST require all gates pass
- FR-011: Gates MUST be independently disableable
- FR-012: Gate order MUST be deterministic
- FR-013: Fast gates MUST run first
- FR-014: Gate timeout MUST be configurable
- FR-015: Default gate timeout MUST be 30 seconds
- FR-016: Gate results MUST be logged
- FR-017: Gate results MUST include duration
- FR-018: Failed gate MUST block push
- FR-019: All gates MUST run unless fail-fast
- FR-020: Gate events MUST be emitted
- FR-021: Custom gates MUST be registrable
- FR-022: Custom gate MUST implement interface
- FR-023: Gate dependencies MAY be specified
- FR-024: Dependent gates MUST wait
- FR-025: Gate caching MAY optimize repeated checks

### FR-026 to FR-045: Failure Handling

- FR-026: Network errors MUST trigger retry
- FR-027: Retry count MUST be configurable
- FR-028: Default retry count MUST be 3
- FR-029: Retry delay MUST use exponential backoff
- FR-030: Initial delay MUST be 1 second
- FR-031: Max delay MUST be 30 seconds
- FR-032: Total timeout MUST be respected
- FR-033: Auth failure MUST NOT retry
- FR-034: Auth failure MUST suggest credential setup
- FR-035: Non-fast-forward MUST NOT retry
- FR-036: Non-fast-forward MUST suggest pull
- FR-037: Permission denied MUST NOT retry
- FR-038: Permission denied MUST explain
- FR-039: Unknown errors MUST retry
- FR-040: Final failure MUST include all attempts
- FR-041: Failure MUST be logged
- FR-042: Failure metrics MUST be tracked
- FR-043: Recovery suggestions MUST be provided
- FR-044: Manual retry MUST be possible
- FR-045: Retry state MUST be clearable

---

## Non-Functional Requirements

- NFR-001: Gate evaluation MUST complete in <30s
- NFR-002: Mode check MUST be <10ms
- NFR-003: Remote check MUST respect timeout
- NFR-004: Retry MUST NOT block indefinitely
- NFR-005: Backoff MUST cap at max delay
- NFR-006: Concurrent pushes MUST be serialized
- NFR-007: Partial push MUST be detectable
- NFR-008: Credentials MUST NOT be logged
- NFR-009: Remote URLs MUST be redacted
- NFR-010: Error messages MUST NOT leak secrets

---

## User Manual Documentation

### Configuration

```yaml
workflow:
  pushGate:
    enabled: true
    requireAllChecks: true
    checks:
      - name: preCommitPassed
        enabled: true
      - name: branchUpToDate
        enabled: true
      - name: modeAllowed
        enabled: true
    
  pushRetry:
    maxAttempts: 3
    initialDelayMs: 1000
    maxDelayMs: 30000
    timeoutSeconds: 120
```

### Gate Types

| Gate | Description |
|------|-------------|
| preCommitPassed | Pre-commit verification completed successfully |
| branchUpToDate | Local branch not behind remote |
| noConflicts | No merge conflicts detected |
| modeAllowed | Operating mode permits push |

### Error Recovery

**Non-fast-forward rejection:**
```bash
$ acode push
Error: Push rejected (non-fast-forward)

Remote has commits not in your local branch.

Suggested fix:
  acode git pull --rebase
  acode push
```

**Authentication failure:**
```bash
$ acode push
Error: Authentication failed

Suggested fix:
  Configure git credentials:
    git config credential.helper store
    git push  # Enter credentials
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Gates evaluated before push
- [ ] AC-002: Failed gate blocks push
- [ ] AC-003: Mode gate enforced
- [ ] AC-004: Network retry works
- [ ] AC-005: Backoff increases delay
- [ ] AC-006: Auth failure no retry
- [ ] AC-007: Non-FF no retry
- [ ] AC-008: Recovery suggestions shown
- [ ] AC-009: Configuration respected
- [ ] AC-010: Credentials not logged

---

## Best Practices

### Gate Design

1. **Fast gates first** - Local checks before network
2. **Independent evaluation** - Gates don't depend on each other
3. **Clear pass/fail** - No ambiguous states
4. **Meaningful names** - Gate names explain what they check

### Failure Handling

5. **Classify errors** - Transient vs permanent failures
6. **Retry transient** - Network issues deserve retry
7. **No retry auth** - Authentication failures are final
8. **Suggest recovery** - Tell user how to fix

### Security

9. **Never log credentials** - Redact all auth info
10. **Respect mode restrictions** - Enforce operating mode rules
11. **Audit all pushes** - Log push attempts and results
12. **Rate limit retries** - Prevent abuse of retry mechanism

---

## Troubleshooting

### Issue: Push always retrying

**Symptoms:** Push keeps retrying even after many attempts

**Causes:**
- Error incorrectly classified as transient
- Max retries not configured
- Retry logic bug

**Solutions:**
1. Check error classification logic
2. Set maxRetries configuration
3. Review retry decision logging

### Issue: Non-fast-forward not detected

**Symptoms:** Push fails on remote but gate didn't catch it

**Causes:**
- Race condition with remote changes
- Gate check stale
- Branch tracking not set up

**Solutions:**
1. Fetch before gate evaluation
2. Re-evaluate gate just before push
3. Set up proper branch tracking

### Issue: Mode gate false positive

**Symptoms:** Push blocked in valid mode

**Causes:**
- Mode detection incorrect
- Gate config wrong
- Network check failing in local-only

**Solutions:**
1. Verify current operating mode
2. Check mode gate configuration
3. Ensure network gates skip in local-only

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Test gate evaluation
- [ ] UT-002: Test retry logic
- [ ] UT-003: Test backoff calculation
- [ ] UT-004: Test error classification

### Integration Tests

- [ ] IT-001: Full push workflow
- [ ] IT-002: Gate failure handling
- [ ] IT-003: Network retry
- [ ] IT-004: Mode enforcement

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Core/
│   └── Domain/
│       └── Workflow/
│           ├── GateResult.cs             # Gate evaluation result
│           ├── GateCheck.cs              # Individual check result
│           ├── PushFailure.cs            # Push failure details
│           └── RetryPolicy.cs            # Retry configuration
│
├── Acode.Application/
│   └── Services/
│       └── Workflow/
│           ├── IPushGate.cs              # Gate interface
│           ├── PushGateService.cs        # Gate evaluation
│           ├── IPushRetryPolicy.cs       # Retry policy interface
│           ├── PushRetryPolicy.cs        # Exponential backoff
│           ├── SafePushService.cs        # Orchestration
│           └── Gates/
│               ├── PreCommitPassedGate.cs
│               ├── BranchUpToDateGate.cs
│               ├── ModeAllowedGate.cs
│               └── NoConflictsGate.cs
│
└── Acode.Cli/
    └── Commands/
        └── Workflow/
            └── PushCommand.cs

tests/
├── Acode.Application.Tests/
│   └── Services/
│       └── Workflow/
│           ├── PushGateServiceTests.cs
│           ├── PushRetryPolicyTests.cs
│           └── Gates/
│               ├── PreCommitPassedGateTests.cs
│               ├── BranchUpToDateGateTests.cs
│               └── ModeAllowedGateTests.cs
│
└── Acode.Integration.Tests/
    └── Workflow/
        └── SafePushIntegrationTests.cs
```

### Domain Models

```csharp
// Acode.Core/Domain/Workflow/GateResult.cs
namespace Acode.Core.Domain.Workflow;

/// <summary>
/// Result of evaluating all push gates.
/// </summary>
public sealed record GateResult
{
    /// <summary>
    /// Whether all gates passed.
    /// </summary>
    public bool Passed { get; init; }
    
    /// <summary>
    /// Results from individual gate checks.
    /// </summary>
    public required IReadOnlyList<GateCheck> Checks { get; init; }
    
    /// <summary>
    /// Total evaluation duration.
    /// </summary>
    public TimeSpan Duration { get; init; }
    
    /// <summary>
    /// Gates that failed.
    /// </summary>
    public IReadOnlyList<GateCheck> FailedGates => 
        Checks.Where(c => !c.Passed).ToList();
    
    /// <summary>
    /// Summary message.
    /// </summary>
    public string Summary => Passed
        ? $"All {Checks.Count} gates passed"
        : $"{FailedGates.Count} of {Checks.Count} gates failed";
    
    public static GateResult AllPassed(IEnumerable<GateCheck> checks, TimeSpan duration) => new()
    {
        Passed = true,
        Checks = checks.ToList(),
        Duration = duration
    };
    
    public static GateResult Failed(IEnumerable<GateCheck> checks, TimeSpan duration) => new()
    {
        Passed = false,
        Checks = checks.ToList(),
        Duration = duration
    };
}

// Acode.Core/Domain/Workflow/GateCheck.cs
namespace Acode.Core.Domain.Workflow;

/// <summary>
/// Result of a single gate check.
/// </summary>
public sealed record GateCheck
{
    /// <summary>Name of the gate.</summary>
    public required string Name { get; init; }
    
    /// <summary>Whether the gate passed.</summary>
    public required bool Passed { get; init; }
    
    /// <summary>Error message if failed.</summary>
    public string? Error { get; init; }
    
    /// <summary>Details about the failure.</summary>
    public string? Details { get; init; }
    
    /// <summary>Evaluation duration.</summary>
    public TimeSpan Duration { get; init; }
    
    /// <summary>Whether the gate was skipped.</summary>
    public bool Skipped { get; init; }
    
    /// <summary>Recovery suggestion if failed.</summary>
    public string? RecoverySuggestion { get; init; }
    
    public static GateCheck Pass(string name, TimeSpan duration) => new()
    {
        Name = name,
        Passed = true,
        Duration = duration
    };
    
    public static GateCheck Fail(
        string name, 
        string error, 
        TimeSpan duration,
        string? details = null,
        string? suggestion = null) => new()
    {
        Name = name,
        Passed = false,
        Error = error,
        Details = details,
        Duration = duration,
        RecoverySuggestion = suggestion
    };
    
    public static GateCheck Skip(string name, string reason) => new()
    {
        Name = name,
        Passed = true,
        Skipped = true,
        Details = reason,
        Duration = TimeSpan.Zero
    };
}

// Acode.Core/Domain/Workflow/PushFailure.cs
namespace Acode.Core.Domain.Workflow;

/// <summary>
/// Details about a push failure.
/// </summary>
public sealed record PushFailure
{
    /// <summary>Type of failure.</summary>
    public required PushFailureType Type { get; init; }
    
    /// <summary>Error message.</summary>
    public required string Message { get; init; }
    
    /// <summary>Whether this failure can be retried.</summary>
    public bool CanRetry { get; init; }
    
    /// <summary>Suggestion for recovery.</summary>
    public string? RecoverySuggestion { get; init; }
    
    /// <summary>Number of retry attempts made.</summary>
    public int RetryAttempts { get; init; }
    
    /// <summary>Original exception if any.</summary>
    public Exception? Exception { get; init; }
    
    /// <summary>Remote URL (redacted).</summary>
    public string? RedactedRemoteUrl { get; init; }
}

/// <summary>
/// Type of push failure.
/// </summary>
public enum PushFailureType
{
    /// <summary>Network error (timeout, connection refused, etc.).</summary>
    Network,
    
    /// <summary>Authentication failure.</summary>
    Authentication,
    
    /// <summary>Non-fast-forward (remote has new commits).</summary>
    NonFastForward,
    
    /// <summary>Permission denied.</summary>
    PermissionDenied,
    
    /// <summary>Remote rejected push (hook failure, etc.).</summary>
    Rejected,
    
    /// <summary>Operating mode violation.</summary>
    ModeViolation,
    
    /// <summary>Gate check failed.</summary>
    GateFailed,
    
    /// <summary>Unknown error.</summary>
    Unknown
}

// Acode.Core/Domain/Workflow/RetryPolicy.cs
namespace Acode.Core.Domain.Workflow;

/// <summary>
/// Configuration for push retry behavior.
/// </summary>
public sealed record PushRetryConfig
{
    /// <summary>Maximum number of retry attempts.</summary>
    public int MaxAttempts { get; init; } = 3;
    
    /// <summary>Initial delay in milliseconds.</summary>
    public int InitialDelayMs { get; init; } = 1000;
    
    /// <summary>Maximum delay in milliseconds.</summary>
    public int MaxDelayMs { get; init; } = 30000;
    
    /// <summary>Multiplier for exponential backoff.</summary>
    public double BackoffMultiplier { get; init; } = 2.0;
    
    /// <summary>Total timeout in seconds.</summary>
    public int TimeoutSeconds { get; init; } = 120;
    
    /// <summary>Add jitter to delays.</summary>
    public bool UseJitter { get; init; } = true;
    
    public static PushRetryConfig Default => new();
}
```

### Gate Interface

```csharp
// Acode.Application/Services/Workflow/IPushGate.cs
namespace Acode.Application.Services.Workflow;

/// <summary>
/// A gate that must pass before push is allowed.
/// </summary>
public interface IPushGate
{
    /// <summary>
    /// Name of this gate.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Order in which to evaluate (lower = earlier).
    /// </summary>
    int Order { get; }
    
    /// <summary>
    /// Evaluates whether the gate passes.
    /// </summary>
    /// <param name="context">Push context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Gate check result.</returns>
    Task<GateCheck> EvaluateAsync(PushContext context, CancellationToken ct = default);
}

/// <summary>
/// Context for push gate evaluation.
/// </summary>
public sealed record PushContext
{
    /// <summary>Working directory.</summary>
    public required string WorkingDirectory { get; init; }
    
    /// <summary>Branch being pushed.</summary>
    public required string Branch { get; init; }
    
    /// <summary>Remote name.</summary>
    public string Remote { get; init; } = "origin";
    
    /// <summary>Whether pre-commit verification passed.</summary>
    public bool PreCommitPassed { get; init; }
    
    /// <summary>Current operating mode.</summary>
    public required OperatingMode Mode { get; init; }
    
    /// <summary>Force push requested.</summary>
    public bool Force { get; init; }
}
```

### Retry Policy Interface

```csharp
// Acode.Application/Services/Workflow/IPushRetryPolicy.cs
namespace Acode.Application.Services.Workflow;

/// <summary>
/// Policy for retrying failed push attempts.
/// </summary>
public interface IPushRetryPolicy
{
    /// <summary>
    /// Determines if the push should be retried.
    /// </summary>
    /// <param name="failure">The failure that occurred.</param>
    /// <param name="attempt">Current attempt number (1-based).</param>
    /// <returns>True if should retry.</returns>
    bool ShouldRetry(PushFailure failure, int attempt);
    
    /// <summary>
    /// Gets the delay before the next retry.
    /// </summary>
    /// <param name="attempt">Current attempt number (1-based).</param>
    /// <returns>Delay to wait.</returns>
    TimeSpan GetDelay(int attempt);
    
    /// <summary>
    /// Gets the maximum number of attempts.
    /// </summary>
    int MaxAttempts { get; }
}
```

### Gate Implementations

```csharp
// Acode.Application/Services/Workflow/Gates/PreCommitPassedGate.cs
namespace Acode.Application.Services.Workflow.Gates;

/// <summary>
/// Gate that verifies pre-commit verification passed.
/// </summary>
public sealed class PreCommitPassedGate : IPushGate
{
    public string Name => "preCommitPassed";
    public int Order => 10;
    
    public Task<GateCheck> EvaluateAsync(PushContext context, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        if (context.PreCommitPassed)
        {
            return Task.FromResult(GateCheck.Pass(Name, stopwatch.Elapsed));
        }
        
        return Task.FromResult(GateCheck.Fail(
            Name,
            "Pre-commit verification has not passed",
            stopwatch.Elapsed,
            "Run 'acode verify' before pushing",
            "acode verify && acode push"));
    }
}

// Acode.Application/Services/Workflow/Gates/ModeAllowedGate.cs
namespace Acode.Application.Services.Workflow.Gates;

/// <summary>
/// Gate that verifies operating mode allows push.
/// </summary>
public sealed class ModeAllowedGate : IPushGate
{
    public string Name => "modeAllowed";
    public int Order => 1; // Check first - fast
    
    public Task<GateCheck> EvaluateAsync(PushContext context, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        switch (context.Mode)
        {
            case OperatingMode.LocalOnly:
                return Task.FromResult(GateCheck.Fail(
                    Name,
                    "Push not allowed in local-only mode",
                    stopwatch.Elapsed,
                    "Operating mode: local-only",
                    "Switch to burst mode: acode config set mode burst"));
                
            case OperatingMode.Airgapped:
                return Task.FromResult(GateCheck.Fail(
                    Name,
                    "Push not allowed in airgapped mode",
                    stopwatch.Elapsed,
                    "Operating mode: airgapped",
                    "Use export bundle instead: acode export"));
                
            case OperatingMode.Burst:
            case OperatingMode.Connected:
                return Task.FromResult(GateCheck.Pass(Name, stopwatch.Elapsed));
                
            default:
                return Task.FromResult(GateCheck.Pass(Name, stopwatch.Elapsed));
        }
    }
}

// Acode.Application/Services/Workflow/Gates/BranchUpToDateGate.cs
namespace Acode.Application.Services.Workflow.Gates;

/// <summary>
/// Gate that verifies local branch is not behind remote.
/// </summary>
public sealed class BranchUpToDateGate : IPushGate
{
    private readonly IGitService _git;
    
    public BranchUpToDateGate(IGitService git)
    {
        _git = git;
    }
    
    public string Name => "branchUpToDate";
    public int Order => 20;
    
    public async Task<GateCheck> EvaluateAsync(PushContext context, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        if (context.Force)
        {
            return GateCheck.Skip(Name, "Skipped due to force push");
        }
        
        try
        {
            // Fetch to get latest remote state
            await _git.ExecuteAsync(
                ["fetch", context.Remote, "--quiet"],
                ct,
                workingDirectory: context.WorkingDirectory);
            
            // Check if we're behind
            var behindOutput = await _git.ExecuteAsync(
                ["rev-list", "--count", $"HEAD..{context.Remote}/{context.Branch}"],
                ct,
                workingDirectory: context.WorkingDirectory);
            
            if (int.TryParse(behindOutput.Trim(), out var behindCount) && behindCount > 0)
            {
                return GateCheck.Fail(
                    Name,
                    $"Local branch is {behindCount} commits behind remote",
                    stopwatch.Elapsed,
                    $"Remote has {behindCount} commit(s) not in your local branch",
                    "acode git pull --rebase && acode push");
            }
            
            return GateCheck.Pass(Name, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            // If we can't check, pass the gate (push will fail if there's an issue)
            return GateCheck.Pass(Name, stopwatch.Elapsed) with
            {
                Details = $"Could not verify: {ex.Message}"
            };
        }
    }
}

// Acode.Application/Services/Workflow/Gates/NoConflictsGate.cs
namespace Acode.Application.Services.Workflow.Gates;

/// <summary>
/// Gate that verifies no merge conflicts exist.
/// </summary>
public sealed class NoConflictsGate : IPushGate
{
    private readonly IGitService _git;
    
    public NoConflictsGate(IGitService git)
    {
        _git = git;
    }
    
    public string Name => "noConflicts";
    public int Order => 15;
    
    public async Task<GateCheck> EvaluateAsync(PushContext context, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var status = await _git.StatusAsync(context.WorkingDirectory, ct);
            
            var conflicts = status.Entries
                .Where(e => e.Status == FileChangeStatus.Conflicted)
                .ToList();
            
            if (conflicts.Count > 0)
            {
                var fileList = string.Join(", ", conflicts.Take(3).Select(c => c.Path));
                if (conflicts.Count > 3)
                    fileList += $" and {conflicts.Count - 3} more";
                
                return GateCheck.Fail(
                    Name,
                    $"Repository has {conflicts.Count} unresolved conflict(s)",
                    stopwatch.Elapsed,
                    $"Conflicted files: {fileList}",
                    "Resolve conflicts, then: acode git add . && acode commit && acode push");
            }
            
            return GateCheck.Pass(Name, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            return GateCheck.Fail(
                Name,
                "Could not check for conflicts",
                stopwatch.Elapsed,
                ex.Message);
        }
    }
}
```

### Retry Policy Implementation

```csharp
// Acode.Application/Services/Workflow/PushRetryPolicy.cs
namespace Acode.Application.Services.Workflow;

/// <summary>
/// Implements exponential backoff retry policy for push operations.
/// </summary>
public sealed class PushRetryPolicy : IPushRetryPolicy
{
    private readonly PushRetryConfig _config;
    private readonly Random _random = new();
    
    public PushRetryPolicy(IOptions<PushRetryConfig> config)
    {
        _config = config.Value;
    }
    
    public int MaxAttempts => _config.MaxAttempts;
    
    public bool ShouldRetry(PushFailure failure, int attempt)
    {
        if (attempt >= _config.MaxAttempts)
            return false;
        
        // Certain failure types should never retry
        return failure.Type switch
        {
            PushFailureType.Network => true,        // Retry network errors
            PushFailureType.Unknown => true,        // Retry unknown (might be transient)
            
            PushFailureType.Authentication => false, // Never retry auth failures
            PushFailureType.NonFastForward => false, // Needs manual intervention
            PushFailureType.PermissionDenied => false, // Won't change
            PushFailureType.ModeViolation => false,   // Configuration issue
            PushFailureType.GateFailed => false,      // Need to fix issue
            PushFailureType.Rejected => false,        // Remote rejected
            
            _ => false
        };
    }
    
    public TimeSpan GetDelay(int attempt)
    {
        // Exponential backoff: delay = initial * multiplier^(attempt-1)
        var baseDelay = _config.InitialDelayMs * 
            Math.Pow(_config.BackoffMultiplier, attempt - 1);
        
        // Cap at max delay
        var delay = Math.Min(baseDelay, _config.MaxDelayMs);
        
        // Add jitter (±25%)
        if (_config.UseJitter)
        {
            var jitter = delay * 0.25;
            delay += (_random.NextDouble() * 2 - 1) * jitter;
        }
        
        return TimeSpan.FromMilliseconds(Math.Max(0, delay));
    }
}
```

### Gate Service

```csharp
// Acode.Application/Services/Workflow/PushGateService.cs
namespace Acode.Application.Services.Workflow;

/// <summary>
/// Service for evaluating push gates.
/// </summary>
public sealed class PushGateService
{
    private readonly IEnumerable<IPushGate> _gates;
    private readonly IOptions<PushGateConfig> _config;
    private readonly ILogger<PushGateService> _logger;
    
    public PushGateService(
        IEnumerable<IPushGate> gates,
        IOptions<PushGateConfig> config,
        ILogger<PushGateService> logger)
    {
        _gates = gates.OrderBy(g => g.Order);
        _config = config;
        _logger = logger;
    }
    
    public async Task<GateResult> EvaluateAsync(
        PushContext context,
        bool failFast = true,
        CancellationToken ct = default)
    {
        var config = _config.Value;
        var stopwatch = Stopwatch.StartNew();
        var checks = new List<GateCheck>();
        
        _logger.LogInformation("Evaluating push gates for branch {Branch}", context.Branch);
        
        foreach (var gate in _gates)
        {
            ct.ThrowIfCancellationRequested();
            
            // Check if gate is enabled
            if (!IsGateEnabled(gate.Name, config))
            {
                checks.Add(GateCheck.Skip(gate.Name, "Disabled in configuration"));
                continue;
            }
            
            try
            {
                using var timeout = new CancellationTokenSource(
                    TimeSpan.FromSeconds(config.GateTimeoutSeconds));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);
                
                var check = await gate.EvaluateAsync(context, linked.Token);
                checks.Add(check);
                
                _logger.LogDebug(
                    "Gate '{Gate}': {Result} in {Duration}ms",
                    gate.Name,
                    check.Passed ? "passed" : "failed",
                    check.Duration.TotalMilliseconds);
                
                if (!check.Passed && failFast)
                {
                    _logger.LogWarning("Gate '{Gate}' failed: {Error}", gate.Name, check.Error);
                    break;
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                checks.Add(GateCheck.Fail(
                    gate.Name,
                    "Gate evaluation timed out",
                    TimeSpan.FromSeconds(config.GateTimeoutSeconds)));
                    
                if (failFast) break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gate '{Gate}' threw exception", gate.Name);
                
                checks.Add(GateCheck.Fail(
                    gate.Name,
                    $"Gate error: {ex.Message}",
                    TimeSpan.Zero));
                    
                if (failFast) break;
            }
        }
        
        stopwatch.Stop();
        
        var allPassed = checks.All(c => c.Passed);
        
        var result = allPassed
            ? GateResult.AllPassed(checks, stopwatch.Elapsed)
            : GateResult.Failed(checks, stopwatch.Elapsed);
        
        _logger.LogInformation(
            "Gate evaluation complete: {Summary} in {Duration}ms",
            result.Summary, stopwatch.ElapsedMilliseconds);
        
        return result;
    }
    
    private bool IsGateEnabled(string gateName, PushGateConfig config)
    {
        if (!config.Enabled)
            return false;
        
        var gateConfig = config.Checks?.FirstOrDefault(
            c => c.Name.Equals(gateName, StringComparison.OrdinalIgnoreCase));
        
        return gateConfig?.Enabled ?? true;
    }
}

/// <summary>
/// Configuration for push gates.
/// </summary>
public sealed record PushGateConfig
{
    public bool Enabled { get; init; } = true;
    public bool RequireAllChecks { get; init; } = true;
    public int GateTimeoutSeconds { get; init; } = 30;
    public IReadOnlyList<GateCheckConfig>? Checks { get; init; }
}

public sealed record GateCheckConfig
{
    public required string Name { get; init; }
    public bool Enabled { get; init; } = true;
}
```

### Safe Push Service

```csharp
// Acode.Application/Services/Workflow/SafePushService.cs
namespace Acode.Application.Services.Workflow;

/// <summary>
/// Orchestrates safe push with gates and retry.
/// </summary>
public sealed class SafePushService
{
    private readonly PushGateService _gates;
    private readonly IGitService _git;
    private readonly IPushRetryPolicy _retryPolicy;
    private readonly ICredentialRedactor _redactor;
    private readonly ILogger<SafePushService> _logger;
    
    public SafePushService(
        PushGateService gates,
        IGitService git,
        IPushRetryPolicy retryPolicy,
        ICredentialRedactor redactor,
        ILogger<SafePushService> logger)
    {
        _gates = gates;
        _git = git;
        _retryPolicy = retryPolicy;
        _redactor = redactor;
        _logger = logger;
    }
    
    public async Task<SafePushResult> PushAsync(
        PushContext context,
        IProgress<PushProgress>? progress = null,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Step 1: Evaluate gates
        progress?.Report(new PushProgress(PushStage.EvaluatingGates, "Checking push gates..."));
        
        var gateResult = await _gates.EvaluateAsync(context, failFast: true, ct);
        
        if (!gateResult.Passed)
        {
            return new SafePushResult
            {
                Success = false,
                GateResult = gateResult,
                Failure = new PushFailure
                {
                    Type = PushFailureType.GateFailed,
                    Message = gateResult.Summary,
                    CanRetry = false,
                    RecoverySuggestion = gateResult.FailedGates
                        .FirstOrDefault()?.RecoverySuggestion
                },
                Duration = stopwatch.Elapsed
            };
        }
        
        // Step 2: Attempt push with retry
        progress?.Report(new PushProgress(PushStage.Pushing, "Pushing to remote..."));
        
        var attempt = 0;
        PushFailure? lastFailure = null;
        
        while (true)
        {
            attempt++;
            ct.ThrowIfCancellationRequested();
            
            try
            {
                var args = new List<string> { "push", context.Remote, context.Branch };
                if (context.Force)
                {
                    args.Add("--force-with-lease");
                }
                
                await _git.ExecuteAsync(args, ct, workingDirectory: context.WorkingDirectory);
                
                _logger.LogInformation(
                    "Push succeeded on attempt {Attempt}",
                    attempt);
                
                return new SafePushResult
                {
                    Success = true,
                    GateResult = gateResult,
                    Attempts = attempt,
                    Duration = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                lastFailure = ClassifyFailure(ex, attempt);
                
                _logger.LogWarning(
                    "Push attempt {Attempt} failed: {Type} - {Message}",
                    attempt, lastFailure.Type, lastFailure.Message);
                
                if (!_retryPolicy.ShouldRetry(lastFailure, attempt))
                {
                    _logger.LogError(
                        "Push failed after {Attempts} attempt(s), not retrying",
                        attempt);
                    break;
                }
                
                var delay = _retryPolicy.GetDelay(attempt);
                
                progress?.Report(new PushProgress(
                    PushStage.Retrying,
                    $"Retry in {delay.TotalSeconds:F1}s (attempt {attempt}/{_retryPolicy.MaxAttempts})..."));
                
                await Task.Delay(delay, ct);
            }
        }
        
        return new SafePushResult
        {
            Success = false,
            GateResult = gateResult,
            Failure = lastFailure,
            Attempts = attempt,
            Duration = stopwatch.Elapsed
        };
    }
    
    private PushFailure ClassifyFailure(Exception ex, int attempt)
    {
        var message = ex.Message;
        var redactedMessage = _redactor.Redact(message);
        
        // Pattern matching for different failure types
        if (message.Contains("Could not resolve host", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Connection refused", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Connection timed out", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Network is unreachable", StringComparison.OrdinalIgnoreCase))
        {
            return new PushFailure
            {
                Type = PushFailureType.Network,
                Message = redactedMessage,
                CanRetry = true,
                RetryAttempts = attempt,
                RecoverySuggestion = "Check network connection and try again",
                Exception = ex
            };
        }
        
        if (message.Contains("Authentication failed", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Invalid credentials", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("could not read Username", StringComparison.OrdinalIgnoreCase))
        {
            return new PushFailure
            {
                Type = PushFailureType.Authentication,
                Message = "Authentication failed",
                CanRetry = false,
                RetryAttempts = attempt,
                RecoverySuggestion = "Configure git credentials:\n  git config credential.helper store\n  git push # Enter credentials",
                Exception = ex
            };
        }
        
        if (message.Contains("[rejected]", StringComparison.OrdinalIgnoreCase) &&
            message.Contains("non-fast-forward", StringComparison.OrdinalIgnoreCase))
        {
            return new PushFailure
            {
                Type = PushFailureType.NonFastForward,
                Message = "Push rejected (non-fast-forward)",
                CanRetry = false,
                RetryAttempts = attempt,
                RecoverySuggestion = "Remote has commits not in your local branch.\n  acode git pull --rebase\n  acode push",
                Exception = ex
            };
        }
        
        if (message.Contains("Permission denied", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("access denied", StringComparison.OrdinalIgnoreCase))
        {
            return new PushFailure
            {
                Type = PushFailureType.PermissionDenied,
                Message = "Permission denied",
                CanRetry = false,
                RetryAttempts = attempt,
                RecoverySuggestion = "Check repository permissions and access rights",
                Exception = ex
            };
        }
        
        if (message.Contains("[rejected]", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("pre-receive hook declined", StringComparison.OrdinalIgnoreCase))
        {
            return new PushFailure
            {
                Type = PushFailureType.Rejected,
                Message = redactedMessage,
                CanRetry = false,
                RetryAttempts = attempt,
                RecoverySuggestion = "Remote rejected the push. Check server-side hooks and policies.",
                Exception = ex
            };
        }
        
        // Unknown error - might be transient
        return new PushFailure
        {
            Type = PushFailureType.Unknown,
            Message = redactedMessage,
            CanRetry = true,
            RetryAttempts = attempt,
            Exception = ex
        };
    }
}

/// <summary>
/// Result of a safe push operation.
/// </summary>
public sealed record SafePushResult
{
    public required bool Success { get; init; }
    public required GateResult GateResult { get; init; }
    public PushFailure? Failure { get; init; }
    public int Attempts { get; init; } = 1;
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Progress information for push operation.
/// </summary>
public sealed record PushProgress(PushStage Stage, string Message);

public enum PushStage
{
    EvaluatingGates,
    Pushing,
    Retrying,
    Complete
}
```

### CLI Command

```csharp
// Acode.Cli/Commands/Workflow/PushCommand.cs
namespace Acode.Cli.Commands.Workflow;

[Command("push", Description = "Push changes with safety gates")]
public sealed class PushCommand : ICommand
{
    [CommandOption("remote|r", Description = "Remote name")]
    public string Remote { get; init; } = "origin";
    
    [CommandOption("branch|b", Description = "Branch name (default: current)")]
    public string? Branch { get; init; }
    
    [CommandOption("force|f", Description = "Force push with lease")]
    public bool Force { get; init; }
    
    [CommandOption("skip-gates", Description = "Skip gate evaluation")]
    public bool SkipGates { get; init; }
    
    [CommandOption("json", Description = "Output as JSON")]
    public bool Json { get; init; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var pushService = GetPushService(); // DI
        var gitService = GetGitService();
        var modeProvider = GetModeProvider();
        
        var workDir = GetWorkingDirectory();
        var currentBranch = Branch ?? await gitService.GetCurrentBranchAsync(workDir);
        var mode = await modeProvider.GetCurrentModeAsync();
        
        var context = new PushContext
        {
            WorkingDirectory = workDir,
            Branch = currentBranch,
            Remote = Remote,
            Mode = mode,
            Force = Force,
            PreCommitPassed = await GetPreCommitStatus()
        };
        
        if (SkipGates)
        {
            console.Output.WriteLine("⚠ Skipping gate evaluation");
        }
        
        var progress = new Progress<PushProgress>(p =>
        {
            if (!Json)
            {
                console.Output.WriteLine($"[{p.Stage}] {p.Message}");
            }
        });
        
        var result = await pushService.PushAsync(context, progress);
        
        if (Json)
        {
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            console.Output.WriteLine(json);
            return;
        }
        
        console.Output.WriteLine();
        
        if (result.Success)
        {
            console.Output.WriteLine($"✓ Push succeeded");
            console.Output.WriteLine($"  Branch: {context.Branch} → {context.Remote}/{context.Branch}");
            console.Output.WriteLine($"  Attempts: {result.Attempts}");
            console.Output.WriteLine($"  Duration: {result.Duration.TotalSeconds:F1}s");
        }
        else
        {
            console.Error.WriteLine($"✗ Push failed: {result.Failure?.Type}");
            console.Error.WriteLine();
            console.Error.WriteLine($"  Error: {result.Failure?.Message}");
            
            if (result.Failure?.RecoverySuggestion != null)
            {
                console.Error.WriteLine();
                console.Error.WriteLine("Suggested fix:");
                foreach (var line in result.Failure.RecoverySuggestion.Split('\n'))
                {
                    console.Error.WriteLine($"  {line}");
                }
            }
            
            // Show failed gates
            if (result.GateResult.FailedGates.Count > 0)
            {
                console.Error.WriteLine();
                console.Error.WriteLine("Failed gates:");
                foreach (var gate in result.GateResult.FailedGates)
                {
                    console.Error.WriteLine($"  ✗ {gate.Name}: {gate.Error}");
                }
            }
            
            Environment.ExitCode = result.Failure?.Type switch
            {
                PushFailureType.GateFailed => ExitCodes.GateFailed,
                PushFailureType.Authentication => ExitCodes.AuthenticationFailed,
                PushFailureType.NonFastForward => ExitCodes.NonFastForward,
                PushFailureType.ModeViolation => ExitCodes.ModeViolation,
                _ => ExitCodes.PushFailed
            };
        }
    }
}
```

### Error Codes

```csharp
// Acode.Cli/ExitCodes.cs (additions)
public static partial class ExitCodes
{
    // Push errors: 80-89
    public const int PushFailed = 80;
    public const int GateFailed = 81;
    public const int AuthenticationFailed = 82;
    public const int NonFastForward = 83;
    public const int ModeViolation = 84;
    public const int RetryExhausted = 85;
}
```

### Implementation Checklist

- [ ] Create `GateResult` and `GateCheck` records
- [ ] Create `PushFailure` and `PushFailureType`
- [ ] Create `PushRetryConfig` record
- [ ] Define `IPushGate` interface
- [ ] Create `PushContext` record
- [ ] Define `IPushRetryPolicy` interface
- [ ] Implement `PreCommitPassedGate`
- [ ] Implement `ModeAllowedGate`
- [ ] Implement `BranchUpToDateGate` with fetch
- [ ] Implement `NoConflictsGate`
- [ ] Implement `PushRetryPolicy` with exponential backoff
- [ ] Add jitter to retry delays
- [ ] Implement `PushGateService.EvaluateAsync`
- [ ] Add gate timeout handling
- [ ] Implement `SafePushService.PushAsync`
- [ ] Implement failure classification
- [ ] Add credential redaction in error messages
- [ ] Create `PushCommand` CLI
- [ ] Add progress reporting
- [ ] Add JSON output support
- [ ] Register all gates in DI
- [ ] Write unit tests for retry policy
- [ ] Write unit tests for each gate
- [ ] Write integration tests for push workflow

### Rollout Plan

1. **Phase 1: Domain Models** (Day 1)
   - Create all records and enums
   - Unit test result types

2. **Phase 2: Gates** (Day 2)
   - Implement all gate types
   - Unit test each gate
   - Test mode enforcement

3. **Phase 3: Retry Policy** (Day 2)
   - Implement exponential backoff
   - Add jitter
   - Test delay calculations

4. **Phase 4: Services** (Day 3)
   - Implement gate service
   - Implement safe push service
   - Add failure classification

5. **Phase 5: CLI** (Day 3)
   - Implement push command
   - Add progress reporting
   - Manual testing

6. **Phase 6: Polish** (Day 4)
   - Improve error messages
   - Add recovery suggestions
   - Credential redaction verification

---

**End of Task 024.c Specification**