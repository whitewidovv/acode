# Task 024: Safe Commit/Push Workflow

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 5 – Git Integration Layer  
**Dependencies:** Task 022.c (add/commit/push), Task 019 (Language Runners)  

---

## Description

Task 024 implements safe commit and push workflows. Commits MUST pass verification before creation. Pushes MUST be gated by quality checks. This prevents broken code from being committed or shared.

The pre-commit verification pipeline MUST run configurable checks. Build verification, test execution, and linting MUST be supported. Failed verification MUST block the commit with clear feedback.

Commit message rules MUST be enforced. Conventional commit format MAY be required. Maximum length, required prefixes, and issue references MAY be configured.

Push gating MUST evaluate additional criteria before push. All local checks MUST pass. Remote push MUST only proceed if gating succeeds. Push failures MUST be handled gracefully with retry support.

The workflow MUST be configurable per-repository. Some repos may require strict checks, others may be lenient. Configuration MUST come from Task 002's `.agent/config.yml`.

### Business Value

Safe workflows prevent broken code from polluting the repository. Automated verification catches issues before they become problems. Consistent commit messages enable automated changelog generation.

### Scope Boundaries

This task defines the workflow orchestration. Subtasks cover specific components: 024.a (verification pipeline), 024.b (message rules), 024.c (push gating).

### Integration Points

- Task 022.c: Underlying commit/push operations
- Task 019: Build and test runners
- Task 002: Configuration contract
- Task 001: Operating mode compliance

### Failure Modes

- Verification fails → Block commit, show failures
- Message validation fails → Block commit, show rules
- Push gate fails → Block push, show failures
- Network error on push → Retry with backoff

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Pre-commit | Verification before commit creation |
| Pipeline | Sequence of verification steps |
| Gate | Condition that must pass before proceeding |
| Conventional Commit | Standard commit message format |
| Retry | Automatic re-attempt after failure |
| Backoff | Increasing delay between retries |

---

## Out of Scope

- External CI/CD integration
- Code review requirements
- Branch protection rules
- Multi-repo workflows
- Commit signing

---

## Functional Requirements

### FR-001 to FR-020: Workflow Orchestration

- FR-001: `SafeCommitAsync` MUST orchestrate verification + commit
- FR-002: Verification MUST run before commit
- FR-003: Failed verification MUST block commit
- FR-004: Verification results MUST be returned
- FR-005: `SafePushAsync` MUST orchestrate gating + push
- FR-006: Gate evaluation MUST run before push
- FR-007: Failed gate MUST block push
- FR-008: Gate results MUST be returned
- FR-009: `--skip-verification` MUST bypass checks
- FR-010: Skip MUST require explicit confirmation
- FR-011: Skip MUST be logged as warning
- FR-012: Workflow MUST be cancellable
- FR-013: Partial failures MUST be recoverable
- FR-014: Workflow status MUST be queryable
- FR-015: Workflow MUST emit events
- FR-016: Events MUST include step completion
- FR-017: Events MUST include step failure
- FR-018: Timeout MUST be configurable
- FR-019: Default timeout MUST be 5 minutes
- FR-020: Timeout MUST abort gracefully

### FR-021 to FR-035: Configuration

- FR-021: Workflow config MUST come from Task 002
- FR-022: `workflow.preCommit.enabled` MUST control verification
- FR-023: Default `enabled` MUST be true
- FR-024: `workflow.preCommit.steps` MUST define checks
- FR-025: Default steps MUST include build and test
- FR-026: `workflow.preCommit.failFast` MUST control behavior
- FR-027: Default `failFast` MUST be true
- FR-028: `workflow.pushGate.enabled` MUST control gating
- FR-029: Default gate enabled MUST be true
- FR-030: `workflow.pushGate.checks` MUST define criteria
- FR-031: Invalid config MUST produce clear error
- FR-032: Missing config MUST use defaults
- FR-033: Config MUST be reloadable
- FR-034: Config changes MUST apply to next workflow
- FR-035: Config MUST be logged at workflow start

---

## Non-Functional Requirements

- NFR-001: Workflow start MUST be <100ms
- NFR-002: Verification step overhead MUST be <1s
- NFR-003: Total workflow MUST complete within timeout
- NFR-004: Memory MUST NOT exceed 100MB for workflow
- NFR-005: Concurrent workflows MUST be serialized
- NFR-006: Failed steps MUST NOT corrupt state
- NFR-007: Recovery MUST be possible from any point
- NFR-008: Audit log MUST record all workflow runs
- NFR-009: Metrics MUST track success/failure rates
- NFR-010: Secrets in output MUST be redacted

---

## User Manual Documentation

### Configuration

```yaml
workflow:
  preCommit:
    enabled: true
    failFast: true
    timeoutSeconds: 300
    steps:
      - name: build
        command: dotnet build
      - name: test
        command: dotnet test
      - name: lint
        command: dotnet format --verify-no-changes
        
  commitMessage:
    pattern: "^(feat|fix|docs|chore|refactor|test)\\(.*\\): .+"
    maxLength: 72
    requireIssueReference: false
    
  pushGate:
    enabled: true
    requireAllChecks: true
    checks:
      - preCommit
      - branchUpToDate
```

### Usage

```bash
# Safe commit with verification
acode commit "feat(api): add new endpoint"

# Skip verification (not recommended)
acode commit "fix: urgent hotfix" --skip-verification

# Safe push with gating
acode push

# Skip gate (not recommended)
acode push --skip-gate
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Pre-commit verification runs
- [ ] AC-002: Failed verification blocks commit
- [ ] AC-003: Message validation runs
- [ ] AC-004: Invalid message blocks commit
- [ ] AC-005: Push gate evaluates
- [ ] AC-006: Failed gate blocks push
- [ ] AC-007: Skip flags work
- [ ] AC-008: Configuration respected
- [ ] AC-009: Timeout enforced
- [ ] AC-010: Events emitted

---

## Testing Requirements

### Unit Tests

| Test ID | Method | Scenario |
|---------|--------|----------|
| UT-001 | `WorkflowStateMachine_TransitionsCorrectly` | State machine logic |
| UT-002 | `ExecuteSteps_InOrder_ReturnsResults` | Step execution order |
| UT-003 | `FailFast_OnFirstFailure_StopsExecution` | Fail-fast behavior |
| UT-004 | `Timeout_AfterLimit_CancelsExecution` | Timeout handling |
| UT-005 | `ValidateMessage_WithPattern_ReturnsResult` | Message validation |
| UT-006 | `ParseConventionalCommit_ExtractsType` | Conventional commit parsing |
| UT-007 | `EvaluateGate_AllChecksPass_AllowsPush` | Gate evaluation success |
| UT-008 | `EvaluateGate_CheckFails_BlocksPush` | Gate evaluation failure |
| UT-009 | `SkipVerification_Bypasses_AllSteps` | Skip flag behavior |
| UT-010 | `Events_Emitted_ForEachStep` | Event emission |

### Integration Tests

| Test ID | Scenario |
|---------|----------|
| IT-001 | Full commit workflow with build and test |
| IT-002 | Full push workflow with all gates |
| IT-003 | Verification failure blocks commit |
| IT-004 | Gate failure blocks push |
| IT-005 | Configuration changes take effect |
| IT-006 | Concurrent workflow serialization |
| IT-007 | Recovery after partial failure |
| IT-008 | Custom verification steps work |

### End-to-End Tests

| Test ID | Scenario |
|---------|----------|
| E2E-001 | `acode commit "feat: add feature"` runs verification |
| E2E-002 | `acode push` runs gate checks |
| E2E-003 | `--skip-verification` bypasses checks |
| E2E-004 | `--skip-gate` bypasses push gate |
| E2E-005 | Invalid commit message rejected |
| E2E-006 | Build failure blocks commit |

### Performance/Benchmarks

| Benchmark | Target | Threshold |
|-----------|--------|-----------|
| Workflow start overhead | <100ms | <200ms |
| Step wrapper overhead | <50ms | <100ms |
| Message validation | <10ms | <50ms |
| Gate evaluation (no external) | <100ms | <200ms |

---

## User Verification Steps

1. **Verify pre-commit verification runs:**
   ```bash
   echo "broken" > file.cs
   git add file.cs
   acode commit "fix: broken code"
   ```
   Verify: Build fails, commit blocked

2. **Verify message validation:**
   ```bash
   echo "valid" > file.cs
   git add file.cs
   acode commit "invalid message"
   ```
   Verify: Message rejected with pattern hint

3. **Verify conventional commit accepted:**
   ```bash
   acode commit "feat(api): add endpoint"
   ```
   Verify: Commit succeeds

4. **Verify skip-verification works:**
   ```bash
   acode commit "hotfix" --skip-verification
   ```
   Verify: Warning shown, commit proceeds

5. **Verify push gate runs:**
   ```bash
   acode push
   ```
   Verify: Pre-commit checks run before push

6. **Verify push blocked in local-only mode:**
   ```bash
   acode config set mode local-only
   acode push
   ```
   Verify: Push blocked with clear message

7. **Verify configuration changes:**
   ```yaml
   workflow:
     preCommit:
       enabled: false
   ```
   ```bash
   acode commit "test"
   ```
   Verify: Verification skipped

8. **Verify timeout enforcement:**
   ```yaml
   workflow:
     preCommit:
       timeoutSeconds: 5
       steps:
         - name: slow
           command: sleep 60
   ```
   ```bash
   acode commit "test"
   ```
   Verify: Timeout after 5 seconds

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Workflow/
│       ├── WorkflowResult.cs
│       ├── StepResult.cs
│       ├── WorkflowState.cs
│       ├── VerificationStep.cs
│       └── GateCheck.cs
├── Acode.Application/
│   └── Workflow/
│       ├── ISafeWorkflowService.cs
│       ├── IPreCommitPipeline.cs
│       ├── ICommitMessageValidator.cs
│       ├── IPushGate.cs
│       ├── WorkflowOptions.cs
│       └── Events/
│           ├── WorkflowStartedEvent.cs
│           ├── StepCompletedEvent.cs
│           ├── StepFailedEvent.cs
│           └── WorkflowCompletedEvent.cs
├── Acode.Infrastructure/
│   └── Workflow/
│       ├── SafeWorkflowService.cs
│       ├── PreCommitPipeline.cs
│       ├── CommitMessageValidator.cs
│       ├── PushGate.cs
│       ├── StepExecutor.cs
│       └── WorkflowConfiguration.cs
└── Acode.Cli/
    └── Commands/
        ├── CommitCommand.cs
        └── PushCommand.cs
```

### Domain Models

```csharp
// WorkflowResult.cs
namespace Acode.Domain.Workflow;

public sealed record WorkflowResult
{
    public required bool Success { get; init; }
    public required WorkflowState FinalState { get; init; }
    public required IReadOnlyList<StepResult> Steps { get; init; }
    public GitCommit? Commit { get; init; }
    public string? Error { get; init; }
    public required TimeSpan TotalDuration { get; init; }
    
    public IEnumerable<StepResult> FailedSteps => Steps.Where(s => !s.Success);
}

public enum WorkflowState
{
    NotStarted,
    VerificationRunning,
    VerificationFailed,
    MessageValidating,
    MessageInvalid,
    Committing,
    CommitFailed,
    GateEvaluating,
    GateFailed,
    Pushing,
    PushFailed,
    Completed,
    Cancelled,
    TimedOut
}

// StepResult.cs
public sealed record StepResult
{
    public required string Name { get; init; }
    public required bool Success { get; init; }
    public required TimeSpan Duration { get; init; }
    public string? Output { get; init; }
    public string? Error { get; init; }
    public int? ExitCode { get; init; }
}

// VerificationStep.cs
public sealed record VerificationStep
{
    public required string Name { get; init; }
    public required string Command { get; init; }
    public string? WorkingDirectory { get; init; }
    public TimeSpan? Timeout { get; init; }
    public bool ContinueOnError { get; init; }
}

// GateCheck.cs
public sealed record GateCheck
{
    public required string Name { get; init; }
    public required GateCheckType Type { get; init; }
    public bool Required { get; init; } = true;
}

public enum GateCheckType
{
    PreCommit,      // Run pre-commit pipeline
    BranchUpToDate, // Check branch is up to date with remote
    NoConflicts,    // Check no merge conflicts
    TestsPassing,   // Run tests
    Custom          // Custom command
}
```

### Core Interfaces

```csharp
// ISafeWorkflowService.cs
namespace Acode.Application.Workflow;

public interface ISafeWorkflowService
{
    Task<WorkflowResult> SafeCommitAsync(
        string workingDir, 
        string message,
        SafeCommitOptions? options = null, 
        CancellationToken ct = default);
    
    Task<WorkflowResult> SafePushAsync(
        string workingDir,
        SafePushOptions? options = null, 
        CancellationToken ct = default);
}

public sealed record SafeCommitOptions
{
    public bool SkipVerification { get; init; }
    public bool SkipMessageValidation { get; init; }
    public bool AllowEmpty { get; init; }
    public IProgress<WorkflowProgress>? Progress { get; init; }
}

public sealed record SafePushOptions
{
    public bool SkipGate { get; init; }
    public string? Remote { get; init; }
    public string? Branch { get; init; }
    public bool Force { get; init; }
    public IProgress<WorkflowProgress>? Progress { get; init; }
}

public sealed record WorkflowProgress
{
    public required WorkflowState State { get; init; }
    public required string CurrentStep { get; init; }
    public required int CurrentStepIndex { get; init; }
    public required int TotalSteps { get; init; }
}

// IPreCommitPipeline.cs
public interface IPreCommitPipeline
{
    Task<PipelineResult> ExecuteAsync(
        string workingDir,
        PipelineOptions? options = null,
        CancellationToken ct = default);
}

public sealed record PipelineResult
{
    public required bool Success { get; init; }
    public required IReadOnlyList<StepResult> Steps { get; init; }
    public required TimeSpan TotalDuration { get; init; }
}

// ICommitMessageValidator.cs
public interface ICommitMessageValidator
{
    ValidationResult Validate(string message);
}

public sealed record ValidationResult
{
    public required bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    public string? SuggestedMessage { get; init; }
}

// IPushGate.cs
public interface IPushGate
{
    Task<GateResult> EvaluateAsync(
        string workingDir,
        CancellationToken ct = default);
}

public sealed record GateResult
{
    public required bool Passed { get; init; }
    public required IReadOnlyList<GateCheckResult> Checks { get; init; }
}

public sealed record GateCheckResult
{
    public required string Name { get; init; }
    public required bool Passed { get; init; }
    public string? Message { get; init; }
}
```

### Infrastructure Implementation

```csharp
// SafeWorkflowService.cs
namespace Acode.Infrastructure.Workflow;

public sealed class SafeWorkflowService : ISafeWorkflowService
{
    private readonly IGitService _git;
    private readonly IPreCommitPipeline _pipeline;
    private readonly ICommitMessageValidator _messageValidator;
    private readonly IPushGate _pushGate;
    private readonly IModeResolver _modeResolver;
    private readonly IEventPublisher _events;
    private readonly IOptions<WorkflowConfiguration> _config;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger<SafeWorkflowService> _logger;
    
    public async Task<WorkflowResult> SafeCommitAsync(
        string workingDir, 
        string message,
        SafeCommitOptions? options = null, 
        CancellationToken ct = default)
    {
        options ??= new SafeCommitOptions();
        var stopwatch = Stopwatch.StartNew();
        var steps = new List<StepResult>();
        
        await _lock.WaitAsync(ct);
        try
        {
            await _events.PublishAsync(new WorkflowStartedEvent("commit"), ct);
            
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(_config.Value.PreCommit.TimeoutSeconds));
            
            // Pre-commit verification
            if (!options.SkipVerification && _config.Value.PreCommit.Enabled)
            {
                options.Progress?.Report(new WorkflowProgress
                {
                    State = WorkflowState.VerificationRunning,
                    CurrentStep = "Pre-commit verification",
                    CurrentStepIndex = 1,
                    TotalSteps = 3
                });
                
                var pipelineResult = await _pipeline.ExecuteAsync(workingDir, null, cts.Token);
                steps.AddRange(pipelineResult.Steps);
                
                if (!pipelineResult.Success)
                {
                    _logger.LogWarning("Pre-commit verification failed");
                    return new WorkflowResult
                    {
                        Success = false,
                        FinalState = WorkflowState.VerificationFailed,
                        Steps = steps,
                        Error = "Pre-commit verification failed",
                        TotalDuration = stopwatch.Elapsed
                    };
                }
            }
            else if (options.SkipVerification)
            {
                _logger.LogWarning("Pre-commit verification skipped by user request");
            }
            
            // Message validation
            if (!options.SkipMessageValidation)
            {
                options.Progress?.Report(new WorkflowProgress
                {
                    State = WorkflowState.MessageValidating,
                    CurrentStep = "Message validation",
                    CurrentStepIndex = 2,
                    TotalSteps = 3
                });
                
                var validation = _messageValidator.Validate(message);
                
                if (!validation.IsValid)
                {
                    return new WorkflowResult
                    {
                        Success = false,
                        FinalState = WorkflowState.MessageInvalid,
                        Steps = steps,
                        Error = $"Invalid commit message: {string.Join(", ", validation.Errors)}",
                        TotalDuration = stopwatch.Elapsed
                    };
                }
            }
            
            // Commit
            options.Progress?.Report(new WorkflowProgress
            {
                State = WorkflowState.Committing,
                CurrentStep = "Creating commit",
                CurrentStepIndex = 3,
                TotalSteps = 3
            });
            
            var commit = await _git.CommitAsync(workingDir, message, 
                new CommitOptions { AllowEmpty = options.AllowEmpty }, cts.Token);
            
            _logger.LogInformation("Created commit {Sha}: {Message}", commit.ShortSha, message);
            
            await _events.PublishAsync(new WorkflowCompletedEvent("commit", true), ct);
            
            return new WorkflowResult
            {
                Success = true,
                FinalState = WorkflowState.Completed,
                Steps = steps,
                Commit = commit,
                TotalDuration = stopwatch.Elapsed
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return new WorkflowResult
            {
                Success = false,
                FinalState = WorkflowState.Cancelled,
                Steps = steps,
                Error = "Workflow cancelled",
                TotalDuration = stopwatch.Elapsed
            };
        }
        catch (OperationCanceledException)
        {
            return new WorkflowResult
            {
                Success = false,
                FinalState = WorkflowState.TimedOut,
                Steps = steps,
                Error = $"Workflow timed out after {_config.Value.PreCommit.TimeoutSeconds}s",
                TotalDuration = stopwatch.Elapsed
            };
        }
        finally
        {
            _lock.Release();
        }
    }
    
    public async Task<WorkflowResult> SafePushAsync(
        string workingDir,
        SafePushOptions? options = null, 
        CancellationToken ct = default)
    {
        options ??= new SafePushOptions();
        var stopwatch = Stopwatch.StartNew();
        var steps = new List<StepResult>();
        
        // Mode check
        var mode = await _modeResolver.GetCurrentModeAsync(ct);
        if (mode is OperatingMode.LocalOnly or OperatingMode.Airgapped)
        {
            return new WorkflowResult
            {
                Success = false,
                FinalState = WorkflowState.GateFailed,
                Steps = steps,
                Error = $"Push blocked in {mode} mode",
                TotalDuration = stopwatch.Elapsed
            };
        }
        
        await _lock.WaitAsync(ct);
        try
        {
            // Gate evaluation
            if (!options.SkipGate && _config.Value.PushGate.Enabled)
            {
                options.Progress?.Report(new WorkflowProgress
                {
                    State = WorkflowState.GateEvaluating,
                    CurrentStep = "Push gate evaluation",
                    CurrentStepIndex = 1,
                    TotalSteps = 2
                });
                
                var gateResult = await _pushGate.EvaluateAsync(workingDir, ct);
                
                if (!gateResult.Passed)
                {
                    var failedChecks = gateResult.Checks.Where(c => !c.Passed);
                    return new WorkflowResult
                    {
                        Success = false,
                        FinalState = WorkflowState.GateFailed,
                        Steps = steps,
                        Error = $"Push gate failed: {string.Join(", ", failedChecks.Select(c => c.Name))}",
                        TotalDuration = stopwatch.Elapsed
                    };
                }
            }
            else if (options.SkipGate)
            {
                _logger.LogWarning("Push gate skipped by user request");
            }
            
            // Push
            options.Progress?.Report(new WorkflowProgress
            {
                State = WorkflowState.Pushing,
                CurrentStep = "Pushing to remote",
                CurrentStepIndex = 2,
                TotalSteps = 2
            });
            
            await _git.PushAsync(workingDir, new PushOptions
            {
                Remote = options.Remote,
                Branch = options.Branch,
                Force = options.Force
            }, ct);
            
            _logger.LogInformation("Pushed to {Remote}", options.Remote ?? "origin");
            
            return new WorkflowResult
            {
                Success = true,
                FinalState = WorkflowState.Completed,
                Steps = steps,
                TotalDuration = stopwatch.Elapsed
            };
        }
        finally
        {
            _lock.Release();
        }
    }
}

// CommitMessageValidator.cs
namespace Acode.Infrastructure.Workflow;

public sealed class CommitMessageValidator : ICommitMessageValidator
{
    private readonly IOptions<WorkflowConfiguration> _config;
    
    private static readonly Regex ConventionalCommitPattern = new(
        @"^(?<type>feat|fix|docs|style|refactor|perf|test|build|ci|chore|revert)(\((?<scope>[^)]+)\))?(?<breaking>!)?: (?<description>.+)$",
        RegexOptions.Compiled);
    
    public ValidationResult Validate(string message)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        
        var config = _config.Value.CommitMessage;
        
        // Length check
        var firstLine = message.Split('\n')[0];
        if (firstLine.Length > config.MaxLength)
        {
            errors.Add($"Subject line exceeds {config.MaxLength} characters");
        }
        
        // Pattern check
        if (!string.IsNullOrEmpty(config.Pattern))
        {
            var regex = new Regex(config.Pattern);
            if (!regex.IsMatch(firstLine))
            {
                errors.Add($"Message does not match pattern: {config.Pattern}");
            }
        }
        
        // Conventional commit check (if pattern implies it)
        if (config.Pattern?.Contains("feat|fix") == true)
        {
            var match = ConventionalCommitPattern.Match(firstLine);
            if (!match.Success)
            {
                errors.Add("Use conventional commit format: type(scope): description");
            }
        }
        
        // Issue reference check
        if (config.RequireIssueReference)
        {
            var hasIssue = Regex.IsMatch(message, @"#\d+|[A-Z]+-\d+");
            if (!hasIssue)
            {
                errors.Add("Commit message must reference an issue (#123 or JIRA-123)");
            }
        }
        
        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }
}
```

### CLI Commands

```csharp
// CommitCommand.cs
namespace Acode.Cli.Commands;

[Command("commit", Description = "Commit with verification")]
public class CommitCommand
{
    [Argument(0, Description = "Commit message")]
    public string Message { get; set; } = "";
    
    [Option("--skip-verification", Description = "Skip pre-commit checks")]
    public bool SkipVerification { get; set; }
    
    [Option("--skip-message-check", Description = "Skip message validation")]
    public bool SkipMessageCheck { get; set; }
    
    [Option("--allow-empty", Description = "Allow empty commit")]
    public bool AllowEmpty { get; set; }
    
    public async Task<int> ExecuteAsync(
        ISafeWorkflowService workflow,
        IConsole console,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Message))
        {
            console.Error.WriteLine("Commit message required");
            return 1;
        }
        
        if (SkipVerification)
        {
            console.Error.WriteLine("⚠ WARNING: Skipping pre-commit verification");
        }
        
        var progress = new Progress<WorkflowProgress>(p =>
        {
            console.Write($"\r[{p.CurrentStepIndex}/{p.TotalSteps}] {p.CurrentStep}...");
        });
        
        var result = await workflow.SafeCommitAsync(
            Directory.GetCurrentDirectory(),
            Message,
            new SafeCommitOptions
            {
                SkipVerification = SkipVerification,
                SkipMessageValidation = SkipMessageCheck,
                AllowEmpty = AllowEmpty,
                Progress = progress
            },
            ct);
        
        console.WriteLine();
        
        if (result.Success)
        {
            console.WriteLine($"✓ {result.Commit!.ShortSha} {Message}");
            return 0;
        }
        
        console.Error.WriteLine($"✗ {result.Error}");
        
        foreach (var step in result.FailedSteps)
        {
            console.Error.WriteLine($"  - {step.Name}: {step.Error}");
        }
        
        return 1;
    }
}

// PushCommand.cs
[Command("push", Description = "Push with gating")]
public class PushCommand
{
    [Option("--skip-gate", Description = "Skip push gate")]
    public bool SkipGate { get; set; }
    
    [Option("-f|--force", Description = "Force push")]
    public bool Force { get; set; }
    
    [Option("--remote", Description = "Remote name (default: origin)")]
    public string? Remote { get; set; }
    
    [Option("--branch", Description = "Branch to push")]
    public string? Branch { get; set; }
    
    public async Task<int> ExecuteAsync(
        ISafeWorkflowService workflow,
        IConsole console,
        CancellationToken ct)
    {
        if (SkipGate)
        {
            console.Error.WriteLine("⚠ WARNING: Skipping push gate");
        }
        
        var result = await workflow.SafePushAsync(
            Directory.GetCurrentDirectory(),
            new SafePushOptions
            {
                SkipGate = SkipGate,
                Remote = Remote,
                Branch = Branch,
                Force = Force
            },
            ct);
        
        if (result.Success)
        {
            console.WriteLine($"✓ Pushed to {Remote ?? "origin"}");
            return 0;
        }
        
        console.Error.WriteLine($"✗ {result.Error}");
        return 1;
    }
}
```

### Error Codes

| Code | Name | Description | Recovery |
|------|------|-------------|----------|
| WF_001 | VerificationFailed | Pre-commit checks failed | Fix issues, retry |
| WF_002 | MessageInvalid | Commit message validation failed | Fix message format |
| WF_003 | GateFailed | Push gate checks failed | Address failures, retry |
| WF_004 | Timeout | Workflow exceeded timeout | Increase timeout or optimize checks |
| WF_005 | Cancelled | Workflow was cancelled | Retry when ready |
| WF_006 | ModeBlocked | Operation blocked by mode | Switch to burst mode |
| WF_007 | ConfigInvalid | Invalid workflow configuration | Fix configuration |
| WF_008 | StepFailed | Individual step failed | Check step output |

### Implementation Checklist

- [ ] Define domain models (WorkflowResult, StepResult, WorkflowState)
- [ ] Define ISafeWorkflowService interface
- [ ] Define IPreCommitPipeline interface
- [ ] Define ICommitMessageValidator interface
- [ ] Define IPushGate interface
- [ ] Implement SafeWorkflowService with orchestration
- [ ] Implement PreCommitPipeline with step execution
- [ ] Implement CommitMessageValidator with patterns
- [ ] Implement PushGate with check types
- [ ] Add workflow events for observability
- [ ] Implement CLI commands (commit, push)
- [ ] Add timeout and cancellation support
- [ ] Add serialization for concurrent workflows
- [ ] Add unit tests for validators and gates
- [ ] Add integration tests for full workflow
- [ ] Add E2E tests for CLI

### Rollout Plan

| Phase | Action | Validation |
|-------|--------|------------|
| 1 | Implement domain models | Compile check |
| 2 | Define all interfaces | Compile check |
| 3 | Implement CommitMessageValidator | Validation tests pass |
| 4 | Implement PreCommitPipeline | Pipeline tests pass |
| 5 | Implement PushGate | Gate tests pass |
| 6 | Implement SafeWorkflowService | Orchestration tests pass |
| 7 | Add workflow events | Event tests pass |
| 8 | Implement CLI commands | E2E tests pass |
| 9 | Add timeout/cancellation | Timeout tests pass |
| 10 | Documentation | User manual complete |

---

**End of Task 024 Specification**