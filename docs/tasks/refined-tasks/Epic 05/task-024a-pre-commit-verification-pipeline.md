# Task 024.a: pre-commit verification pipeline

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 5 – Git Integration Layer  
**Dependencies:** Task 024 (Safe Workflow), Task 019 (Language Runners)  

---

## Description

Task 024.a implements the pre-commit verification pipeline. Before a commit is created, configurable verification steps MUST execute. Failed steps MUST block the commit.

The pipeline MUST support multiple step types: build, test, lint, and custom commands. Each step MUST be independently configurable. Step order MUST be deterministic.

Fail-fast mode MUST stop on first failure. Parallel execution MAY be supported for independent steps. Step results MUST be collected and returned.

Output capture MUST enable debugging. Both stdout and stderr MUST be captured. Large output MUST be truncated with tail preserved.

### Business Value

Pre-commit verification catches issues before they enter version control. Automated checks ensure code quality. Failed commits are prevented rather than fixed later.

### Scope Boundaries

This task covers the verification pipeline execution. Message validation is in 024.b. Push gating is in 024.c.

### Integration Points

- Task 024: Workflow orchestration
- Task 019: Language runners for build/test
- Task 018: Command execution
- Task 002: Configuration

### Failure Modes

- Step command not found → Clear error
- Step timeout → Abort and report
- Step crashes → Capture output, report failure

---

## Functional Requirements

### FR-001 to FR-030: Pipeline Execution

- FR-001: `IPreCommitPipeline` interface MUST be defined
- FR-002: `RunAsync` MUST execute all configured steps
- FR-003: Steps MUST execute in configured order
- FR-004: Each step MUST have a name
- FR-005: Each step MUST have a command
- FR-006: Step command MUST support arguments
- FR-007: Step MUST capture stdout
- FR-008: Step MUST capture stderr
- FR-009: Step MUST capture exit code
- FR-010: Exit code 0 MUST indicate success
- FR-011: Non-zero exit MUST indicate failure
- FR-012: `failFast` MUST stop on first failure
- FR-013: Non-failFast MUST run all steps
- FR-014: Step timeout MUST be configurable
- FR-015: Default step timeout MUST be 60 seconds
- FR-016: Timed out step MUST be marked failed
- FR-017: Step working directory MUST be repo root
- FR-018: Custom working directory MAY be specified
- FR-019: Environment variables MUST be passable
- FR-020: Step results MUST include duration
- FR-021: Step results MUST include output
- FR-022: Output MUST be truncated if too long
- FR-023: Truncation MUST preserve tail
- FR-024: Default max output MUST be 10KB
- FR-025: All steps MUST be cancellable
- FR-026: Cancellation MUST abort current step
- FR-027: Pipeline result MUST aggregate step results
- FR-028: Pipeline MUST report overall success/failure
- FR-029: Pipeline MUST emit step events
- FR-030: Pipeline MUST be logged

### FR-031 to FR-045: Built-in Steps

- FR-031: `build` step MUST run build command
- FR-032: Build command MUST detect project type
- FR-033: .NET projects MUST use `dotnet build`
- FR-034: Node projects MUST use `npm run build`
- FR-035: Custom build MUST override detection
- FR-036: `test` step MUST run test command
- FR-037: .NET tests MUST use `dotnet test`
- FR-038: Node tests MUST use `npm test`
- FR-039: `lint` step MUST run linter
- FR-040: .NET lint MUST use `dotnet format --verify-no-changes`
- FR-041: Node lint MUST use `npm run lint`
- FR-042: `custom` step MUST run arbitrary command
- FR-043: Step dependencies MAY be specified
- FR-044: Dependent steps MUST wait for prerequisites
- FR-045: Circular dependencies MUST be rejected

---

## Non-Functional Requirements

- NFR-001: Pipeline start MUST be <100ms
- NFR-002: Step overhead MUST be <500ms
- NFR-003: Parallel steps MUST share resources safely
- NFR-004: Memory MUST NOT exceed 100MB for pipeline
- NFR-005: Output buffering MUST NOT exceed 50MB
- NFR-006: Step processes MUST be terminated on timeout
- NFR-007: Zombie processes MUST be prevented
- NFR-008: Secrets MUST be redacted in output
- NFR-009: File paths MUST be normalized
- NFR-010: Cross-platform commands MUST work

---

## User Manual Documentation

### Configuration

```yaml
workflow:
  preCommit:
    enabled: true
    failFast: true
    steps:
      - name: build
        type: build
        timeoutSeconds: 120
        
      - name: test
        type: test
        timeoutSeconds: 300
        
      - name: lint
        type: lint
        timeoutSeconds: 60
        
      - name: custom-check
        type: custom
        command: ./scripts/check.sh
        timeoutSeconds: 30
```

### Step Types

| Type | .NET Command | Node Command |
|------|--------------|--------------|
| build | `dotnet build` | `npm run build` |
| test | `dotnet test` | `npm test` |
| lint | `dotnet format --verify-no-changes` | `npm run lint` |
| custom | (specified) | (specified) |

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Pipeline executes steps in order
- [ ] AC-002: Step output captured
- [ ] AC-003: Step exit code detected
- [ ] AC-004: Fail-fast stops on failure
- [ ] AC-005: Non-failFast runs all
- [ ] AC-006: Timeout aborts step
- [ ] AC-007: Built-in types work
- [ ] AC-008: Custom commands work
- [ ] AC-009: Results aggregated
- [ ] AC-010: Events emitted

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Test step execution
- [ ] UT-002: Test fail-fast
- [ ] UT-003: Test timeout
- [ ] UT-004: Test output capture

### Integration Tests

- [ ] IT-001: Full pipeline run
- [ ] IT-002: Mixed success/failure
- [ ] IT-003: Timeout handling
- [ ] IT-004: Build type detection

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Core/
│   └── Domain/
│       └── Workflow/
│           ├── PipelineStep.cs           # Step definition
│           ├── StepResult.cs             # Execution result
│           ├── PipelineResult.cs         # Aggregate result
│           └── PipelineException.cs      # Pipeline errors
│
├── Acode.Application/
│   └── Services/
│       └── Workflow/
│           ├── IPreCommitPipeline.cs     # Pipeline interface
│           ├── PreCommitPipeline.cs      # Implementation
│           ├── StepExecutor.cs           # Step execution logic
│           ├── OutputCapture.cs          # Output handling
│           └── BuiltInSteps/
│               ├── BuildStep.cs          # Build detection
│               ├── TestStep.cs           # Test execution
│               └── LintStep.cs           # Linting
│
├── Acode.Infrastructure/
│   └── Process/
│       └── ProcessRunner.cs              # Process execution
│
└── Acode.Cli/
    └── Commands/
        └── Workflow/
            └── VerifyCommand.cs

tests/
├── Acode.Application.Tests/
│   └── Services/
│       └── Workflow/
│           ├── PreCommitPipelineTests.cs
│           ├── StepExecutorTests.cs
│           └── BuiltInStepTests.cs
│
└── Acode.Integration.Tests/
    └── Workflow/
        └── PipelineIntegrationTests.cs
```

### Domain Models

```csharp
// Acode.Core/Domain/Workflow/PipelineStep.cs
namespace Acode.Core.Domain.Workflow;

/// <summary>
/// Defines a step in the pre-commit verification pipeline.
/// </summary>
public sealed record PipelineStep
{
    /// <summary>
    /// Unique name for the step.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Type of step: build, test, lint, or custom.
    /// </summary>
    public required StepType Type { get; init; }
    
    /// <summary>
    /// Command to execute (required for custom, optional for built-in).
    /// </summary>
    public string? Command { get; init; }
    
    /// <summary>
    /// Arguments for the command.
    /// </summary>
    public IReadOnlyList<string>? Arguments { get; init; }
    
    /// <summary>
    /// Maximum execution time in seconds.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 60;
    
    /// <summary>
    /// Working directory (defaults to repo root).
    /// </summary>
    public string? WorkingDirectory { get; init; }
    
    /// <summary>
    /// Environment variables for the step.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Environment { get; init; }
    
    /// <summary>
    /// Whether this step is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;
    
    /// <summary>
    /// Names of steps that must complete first.
    /// </summary>
    public IReadOnlyList<string>? DependsOn { get; init; }
    
    /// <summary>
    /// Whether to continue pipeline if this step fails.
    /// </summary>
    public bool ContinueOnError { get; init; }
}

/// <summary>
/// Type of verification step.
/// </summary>
public enum StepType
{
    /// <summary>Build the project.</summary>
    Build,
    
    /// <summary>Run tests.</summary>
    Test,
    
    /// <summary>Run linter/formatter.</summary>
    Lint,
    
    /// <summary>Custom command.</summary>
    Custom
}

// Acode.Core/Domain/Workflow/StepResult.cs
namespace Acode.Core.Domain.Workflow;

/// <summary>
/// Result of executing a single pipeline step.
/// </summary>
public sealed record StepResult
{
    /// <summary>Name of the step.</summary>
    public required string StepName { get; init; }
    
    /// <summary>Whether the step passed.</summary>
    public required bool Success { get; init; }
    
    /// <summary>Exit code from the command.</summary>
    public int ExitCode { get; init; }
    
    /// <summary>Execution duration.</summary>
    public TimeSpan Duration { get; init; }
    
    /// <summary>Captured stdout.</summary>
    public string? StandardOutput { get; init; }
    
    /// <summary>Captured stderr.</summary>
    public string? StandardError { get; init; }
    
    /// <summary>Whether the step was skipped.</summary>
    public bool Skipped { get; init; }
    
    /// <summary>Reason for skipping.</summary>
    public string? SkipReason { get; init; }
    
    /// <summary>Whether the step timed out.</summary>
    public bool TimedOut { get; init; }
    
    /// <summary>Error message if failed.</summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>Command that was executed.</summary>
    public string? ExecutedCommand { get; init; }
    
    public static StepResult Passed(string name, TimeSpan duration, string? output = null) => new()
    {
        StepName = name,
        Success = true,
        ExitCode = 0,
        Duration = duration,
        StandardOutput = output
    };
    
    public static StepResult Failed(string name, int exitCode, TimeSpan duration, 
        string? output = null, string? error = null) => new()
    {
        StepName = name,
        Success = false,
        ExitCode = exitCode,
        Duration = duration,
        StandardOutput = output,
        StandardError = error,
        ErrorMessage = $"Step exited with code {exitCode}"
    };
    
    public static StepResult TimedOutResult(string name, TimeSpan duration, 
        string? output = null) => new()
    {
        StepName = name,
        Success = false,
        ExitCode = -1,
        Duration = duration,
        StandardOutput = output,
        TimedOut = true,
        ErrorMessage = "Step timed out"
    };
    
    public static StepResult SkippedResult(string name, string reason) => new()
    {
        StepName = name,
        Success = true, // Skipped is not a failure
        Skipped = true,
        SkipReason = reason,
        Duration = TimeSpan.Zero
    };
}

// Acode.Core/Domain/Workflow/PipelineResult.cs
namespace Acode.Core.Domain.Workflow;

/// <summary>
/// Aggregate result of the entire pipeline.
/// </summary>
public sealed record PipelineResult
{
    /// <summary>Whether all required steps passed.</summary>
    public required bool Success { get; init; }
    
    /// <summary>Total pipeline duration.</summary>
    public TimeSpan Duration { get; init; }
    
    /// <summary>Results for each step.</summary>
    public required IReadOnlyList<StepResult> Steps { get; init; }
    
    /// <summary>Number of steps that passed.</summary>
    public int PassedCount => Steps.Count(s => s.Success && !s.Skipped);
    
    /// <summary>Number of steps that failed.</summary>
    public int FailedCount => Steps.Count(s => !s.Success);
    
    /// <summary>Number of steps that were skipped.</summary>
    public int SkippedCount => Steps.Count(s => s.Skipped);
    
    /// <summary>First failure if any.</summary>
    public StepResult? FirstFailure => Steps.FirstOrDefault(s => !s.Success);
    
    /// <summary>Summary message.</summary>
    public string Summary => Success
        ? $"All {PassedCount} steps passed"
        : $"{FailedCount} of {Steps.Count} steps failed";
    
    public static PipelineResult Empty => new()
    {
        Success = true,
        Duration = TimeSpan.Zero,
        Steps = []
    };
}

// Acode.Core/Domain/Workflow/PipelineException.cs
namespace Acode.Core.Domain.Workflow;

/// <summary>
/// Exception during pipeline execution.
/// </summary>
public class PipelineException : Exception
{
    public PipelineException(string message) : base(message) { }
    public PipelineException(string message, Exception inner) : base(message, inner) { }
}

public sealed class CircularDependencyException : PipelineException
{
    public IReadOnlyList<string> Cycle { get; }
    
    public CircularDependencyException(IEnumerable<string> cycle)
        : base($"Circular dependency detected: {string.Join(" -> ", cycle)}")
    {
        Cycle = cycle.ToList();
    }
}

public sealed class DependencyNotFoundException : PipelineException
{
    public string StepName { get; }
    public string DependencyName { get; }
    
    public DependencyNotFoundException(string step, string dependency)
        : base($"Step '{step}' depends on unknown step '{dependency}'")
    {
        StepName = step;
        DependencyName = dependency;
    }
}
```

### Pipeline Options

```csharp
// Acode.Application/Services/Workflow/PipelineOptions.cs
namespace Acode.Application.Services.Workflow;

/// <summary>
/// Options for pipeline execution.
/// </summary>
public sealed record PipelineOptions
{
    /// <summary>
    /// Whether to stop on first failure.
    /// </summary>
    public bool FailFast { get; init; } = true;
    
    /// <summary>
    /// Steps to execute (null = all configured steps).
    /// </summary>
    public IReadOnlyList<string>? StepFilter { get; init; }
    
    /// <summary>
    /// Whether to run in parallel where possible.
    /// </summary>
    public bool Parallel { get; init; }
    
    /// <summary>
    /// Maximum parallel steps.
    /// </summary>
    public int MaxParallelism { get; init; } = 4;
    
    /// <summary>
    /// Working directory override.
    /// </summary>
    public string? WorkingDirectory { get; init; }
    
    /// <summary>
    /// Additional environment variables for all steps.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Environment { get; init; }
    
    /// <summary>
    /// Maximum output size per step in bytes.
    /// </summary>
    public int MaxOutputBytes { get; init; } = 10 * 1024; // 10KB
}
```

### Pipeline Interface

```csharp
// Acode.Application/Services/Workflow/IPreCommitPipeline.cs
namespace Acode.Application.Services.Workflow;

/// <summary>
/// Pre-commit verification pipeline.
/// </summary>
public interface IPreCommitPipeline
{
    /// <summary>
    /// Runs the pre-commit verification pipeline.
    /// </summary>
    /// <param name="workingDir">Repository working directory.</param>
    /// <param name="options">Pipeline options.</param>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Pipeline result.</returns>
    Task<PipelineResult> RunAsync(
        string workingDir,
        PipelineOptions? options = null,
        IProgress<StepProgress>? progress = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets the configured pipeline steps.
    /// </summary>
    IReadOnlyList<PipelineStep> GetSteps();
    
    /// <summary>
    /// Validates the pipeline configuration.
    /// </summary>
    /// <returns>Validation errors, empty if valid.</returns>
    IReadOnlyList<string> ValidateConfiguration();
}

/// <summary>
/// Progress information for pipeline execution.
/// </summary>
public sealed record StepProgress(
    string StepName,
    StepProgressState State,
    int CurrentStep,
    int TotalSteps,
    string? Message = null);

public enum StepProgressState
{
    Starting,
    Running,
    Passed,
    Failed,
    Skipped,
    TimedOut
}
```

### Step Executor

```csharp
// Acode.Application/Services/Workflow/StepExecutor.cs
namespace Acode.Application.Services.Workflow;

/// <summary>
/// Executes individual pipeline steps.
/// </summary>
public sealed class StepExecutor
{
    private readonly IProcessRunner _processRunner;
    private readonly IProjectTypeDetector _projectDetector;
    private readonly ISecretRedactor _redactor;
    private readonly ILogger<StepExecutor> _logger;
    
    public StepExecutor(
        IProcessRunner processRunner,
        IProjectTypeDetector projectDetector,
        ISecretRedactor redactor,
        ILogger<StepExecutor> logger)
    {
        _processRunner = processRunner;
        _projectDetector = projectDetector;
        _redactor = redactor;
        _logger = logger;
    }
    
    public async Task<StepResult> ExecuteAsync(
        PipelineStep step,
        string workingDir,
        IReadOnlyDictionary<string, string>? additionalEnv,
        int maxOutputBytes,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Get command to execute
            var (command, args) = await ResolveCommandAsync(step, workingDir, ct);
            
            if (string.IsNullOrEmpty(command))
            {
                return StepResult.SkippedResult(step.Name, 
                    $"No {step.Type} command found for project type");
            }
            
            var effectiveWorkDir = step.WorkingDirectory ?? workingDir;
            var fullCommand = string.IsNullOrEmpty(args) ? command : $"{command} {args}";
            
            _logger.LogInformation(
                "Executing step '{Step}': {Command}",
                step.Name, fullCommand);
            
            // Merge environment variables
            var environment = MergeEnvironment(step.Environment, additionalEnv);
            
            // Create timeout
            using var timeoutCts = new CancellationTokenSource(
                TimeSpan.FromSeconds(step.TimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
            
            // Execute
            var processResult = await _processRunner.RunAsync(
                command,
                args,
                effectiveWorkDir,
                environment,
                linkedCts.Token);
            
            stopwatch.Stop();
            
            // Truncate output if needed
            var stdout = TruncateOutput(processResult.StandardOutput, maxOutputBytes);
            var stderr = TruncateOutput(processResult.StandardError, maxOutputBytes);
            
            // Redact secrets
            stdout = _redactor.Redact(stdout);
            stderr = _redactor.Redact(stderr);
            
            if (processResult.ExitCode == 0)
            {
                return StepResult.Passed(step.Name, stopwatch.Elapsed, stdout) with
                {
                    ExecutedCommand = fullCommand
                };
            }
            
            return StepResult.Failed(step.Name, processResult.ExitCode, 
                stopwatch.Elapsed, stdout, stderr) with
            {
                ExecutedCommand = fullCommand
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // Re-throw if pipeline was cancelled
        }
        catch (OperationCanceledException)
        {
            // Timeout
            stopwatch.Stop();
            return StepResult.TimedOutResult(step.Name, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Step '{Step}' threw exception", step.Name);
            
            return new StepResult
            {
                StepName = step.Name,
                Success = false,
                ExitCode = -1,
                Duration = stopwatch.Elapsed,
                ErrorMessage = ex.Message
            };
        }
    }
    
    private async Task<(string Command, string Args)> ResolveCommandAsync(
        PipelineStep step,
        string workingDir,
        CancellationToken ct)
    {
        if (step.Type == StepType.Custom)
        {
            if (string.IsNullOrEmpty(step.Command))
                throw new InvalidOperationException($"Custom step '{step.Name}' requires a command");
            
            var args = step.Arguments != null 
                ? string.Join(" ", step.Arguments) 
                : "";
            return (step.Command, args);
        }
        
        // Detect project type for built-in steps
        var projectType = await _projectDetector.DetectAsync(workingDir, ct);
        
        return step.Type switch
        {
            StepType.Build => GetBuildCommand(projectType, step),
            StepType.Test => GetTestCommand(projectType, step),
            StepType.Lint => GetLintCommand(projectType, step),
            _ => throw new InvalidOperationException($"Unknown step type: {step.Type}")
        };
    }
    
    private static (string, string) GetBuildCommand(ProjectType type, PipelineStep step)
    {
        // Allow override
        if (!string.IsNullOrEmpty(step.Command))
            return (step.Command, step.Arguments != null ? string.Join(" ", step.Arguments) : "");
        
        return type switch
        {
            ProjectType.DotNet => ("dotnet", "build --no-restore"),
            ProjectType.Node => ("npm", "run build"),
            ProjectType.Python => ("python", "-m py_compile"),
            _ => ("", "")
        };
    }
    
    private static (string, string) GetTestCommand(ProjectType type, PipelineStep step)
    {
        if (!string.IsNullOrEmpty(step.Command))
            return (step.Command, step.Arguments != null ? string.Join(" ", step.Arguments) : "");
        
        return type switch
        {
            ProjectType.DotNet => ("dotnet", "test --no-build --verbosity normal"),
            ProjectType.Node => ("npm", "test"),
            ProjectType.Python => ("python", "-m pytest"),
            _ => ("", "")
        };
    }
    
    private static (string, string) GetLintCommand(ProjectType type, PipelineStep step)
    {
        if (!string.IsNullOrEmpty(step.Command))
            return (step.Command, step.Arguments != null ? string.Join(" ", step.Arguments) : "");
        
        return type switch
        {
            ProjectType.DotNet => ("dotnet", "format --verify-no-changes"),
            ProjectType.Node => ("npm", "run lint"),
            ProjectType.Python => ("python", "-m flake8"),
            _ => ("", "")
        };
    }
    
    private static Dictionary<string, string> MergeEnvironment(
        IReadOnlyDictionary<string, string>? stepEnv,
        IReadOnlyDictionary<string, string>? additionalEnv)
    {
        var result = new Dictionary<string, string>();
        
        if (additionalEnv != null)
        {
            foreach (var (key, value) in additionalEnv)
                result[key] = value;
        }
        
        if (stepEnv != null)
        {
            foreach (var (key, value) in stepEnv)
                result[key] = value; // Step env overrides
        }
        
        return result;
    }
    
    private static string? TruncateOutput(string? output, int maxBytes)
    {
        if (string.IsNullOrEmpty(output))
            return output;
        
        if (output.Length <= maxBytes)
            return output;
        
        // Keep tail (more useful for errors)
        var truncated = output[^maxBytes..];
        return $"[...output truncated ({output.Length} bytes total)...]\n{truncated}";
    }
}

public enum ProjectType
{
    Unknown,
    DotNet,
    Node,
    Python
}

public interface IProjectTypeDetector
{
    Task<ProjectType> DetectAsync(string path, CancellationToken ct = default);
}
```

### Pipeline Implementation

```csharp
// Acode.Application/Services/Workflow/PreCommitPipeline.cs
namespace Acode.Application.Services.Workflow;

/// <summary>
/// Implements the pre-commit verification pipeline.
/// </summary>
public sealed class PreCommitPipeline : IPreCommitPipeline
{
    private readonly IOptions<PreCommitConfig> _config;
    private readonly StepExecutor _executor;
    private readonly ILogger<PreCommitPipeline> _logger;
    
    public PreCommitPipeline(
        IOptions<PreCommitConfig> config,
        StepExecutor executor,
        ILogger<PreCommitPipeline> logger)
    {
        _config = config;
        _executor = executor;
        _logger = logger;
    }
    
    public IReadOnlyList<PipelineStep> GetSteps() => 
        _config.Value.Steps ?? [];
    
    public IReadOnlyList<string> ValidateConfiguration()
    {
        var errors = new List<string>();
        var steps = GetSteps();
        var stepNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var step in steps)
        {
            if (string.IsNullOrWhiteSpace(step.Name))
            {
                errors.Add("Step name cannot be empty");
                continue;
            }
            
            if (!stepNames.Add(step.Name))
            {
                errors.Add($"Duplicate step name: {step.Name}");
            }
            
            if (step.Type == StepType.Custom && string.IsNullOrEmpty(step.Command))
            {
                errors.Add($"Custom step '{step.Name}' requires a command");
            }
            
            if (step.TimeoutSeconds <= 0)
            {
                errors.Add($"Step '{step.Name}' has invalid timeout");
            }
            
            // Check dependencies exist
            if (step.DependsOn != null)
            {
                foreach (var dep in step.DependsOn)
                {
                    if (!stepNames.Contains(dep) && 
                        !steps.Any(s => s.Name.Equals(dep, StringComparison.OrdinalIgnoreCase)))
                    {
                        errors.Add($"Step '{step.Name}' depends on unknown step '{dep}'");
                    }
                }
            }
        }
        
        // Check for circular dependencies
        try
        {
            TopologicalSort(steps);
        }
        catch (CircularDependencyException ex)
        {
            errors.Add(ex.Message);
        }
        
        return errors;
    }
    
    public async Task<PipelineResult> RunAsync(
        string workingDir,
        PipelineOptions? options = null,
        IProgress<StepProgress>? progress = null,
        CancellationToken ct = default)
    {
        options ??= new PipelineOptions();
        
        var allSteps = GetSteps().Where(s => s.Enabled).ToList();
        
        // Filter steps if specified
        if (options.StepFilter?.Count > 0)
        {
            allSteps = allSteps
                .Where(s => options.StepFilter.Contains(s.Name, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }
        
        if (allSteps.Count == 0)
        {
            _logger.LogInformation("No steps to execute");
            return PipelineResult.Empty;
        }
        
        // Sort by dependencies
        var orderedSteps = TopologicalSort(allSteps);
        
        var effectiveWorkDir = options.WorkingDirectory ?? workingDir;
        var stopwatch = Stopwatch.StartNew();
        var results = new List<StepResult>();
        var failedSteps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        _logger.LogInformation(
            "Running pre-commit pipeline with {Count} steps",
            orderedSteps.Count);
        
        for (var i = 0; i < orderedSteps.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            
            var step = orderedSteps[i];
            
            // Check if dependencies failed
            var failedDeps = step.DependsOn?
                .Where(d => failedSteps.Contains(d))
                .ToList();
            
            if (failedDeps?.Count > 0)
            {
                var result = StepResult.SkippedResult(step.Name,
                    $"Dependency failed: {string.Join(", ", failedDeps)}");
                results.Add(result);
                
                progress?.Report(new StepProgress(
                    step.Name, StepProgressState.Skipped,
                    i + 1, orderedSteps.Count));
                
                continue;
            }
            
            progress?.Report(new StepProgress(
                step.Name, StepProgressState.Running,
                i + 1, orderedSteps.Count,
                $"Executing {step.Name}..."));
            
            var stepResult = await _executor.ExecuteAsync(
                step,
                effectiveWorkDir,
                options.Environment,
                options.MaxOutputBytes,
                ct);
            
            results.Add(stepResult);
            
            var state = stepResult.Success 
                ? (stepResult.Skipped ? StepProgressState.Skipped : StepProgressState.Passed)
                : (stepResult.TimedOut ? StepProgressState.TimedOut : StepProgressState.Failed);
            
            progress?.Report(new StepProgress(
                step.Name, state,
                i + 1, orderedSteps.Count));
            
            if (!stepResult.Success)
            {
                failedSteps.Add(step.Name);
                
                _logger.LogWarning(
                    "Step '{Step}' failed with exit code {ExitCode}",
                    step.Name, stepResult.ExitCode);
                
                if (options.FailFast && !step.ContinueOnError)
                {
                    _logger.LogInformation(
                        "Fail-fast enabled, stopping pipeline after {Step}",
                        step.Name);
                    break;
                }
            }
            else
            {
                _logger.LogInformation(
                    "Step '{Step}' passed in {Duration}ms",
                    step.Name, stepResult.Duration.TotalMilliseconds);
            }
        }
        
        stopwatch.Stop();
        
        var pipelineResult = new PipelineResult
        {
            Success = results.All(r => r.Success),
            Duration = stopwatch.Elapsed,
            Steps = results
        };
        
        _logger.LogInformation(
            "Pipeline complete: {Summary} in {Duration}ms",
            pipelineResult.Summary, stopwatch.ElapsedMilliseconds);
        
        return pipelineResult;
    }
    
    private static List<PipelineStep> TopologicalSort(IReadOnlyList<PipelineStep> steps)
    {
        var sorted = new List<PipelineStep>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stepMap = steps.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);
        
        void Visit(PipelineStep step, List<string> path)
        {
            if (visited.Contains(step.Name))
                return;
            
            if (visiting.Contains(step.Name))
            {
                path.Add(step.Name);
                throw new CircularDependencyException(path);
            }
            
            visiting.Add(step.Name);
            path.Add(step.Name);
            
            if (step.DependsOn != null)
            {
                foreach (var depName in step.DependsOn)
                {
                    if (!stepMap.TryGetValue(depName, out var dep))
                    {
                        throw new DependencyNotFoundException(step.Name, depName);
                    }
                    Visit(dep, new List<string>(path));
                }
            }
            
            visiting.Remove(step.Name);
            visited.Add(step.Name);
            sorted.Add(step);
        }
        
        foreach (var step in steps)
        {
            Visit(step, []);
        }
        
        return sorted;
    }
}

/// <summary>
/// Configuration for pre-commit pipeline.
/// </summary>
public sealed record PreCommitConfig
{
    public bool Enabled { get; init; } = true;
    public bool FailFast { get; init; } = true;
    public IReadOnlyList<PipelineStep>? Steps { get; init; }
}
```

### Process Runner

```csharp
// Acode.Infrastructure/Process/ProcessRunner.cs
namespace Acode.Infrastructure.Process;

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(
        string command,
        string? arguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? environment,
        CancellationToken ct);
}

public sealed record ProcessResult(
    int ExitCode,
    string? StandardOutput,
    string? StandardError);

public sealed class ProcessRunner : IProcessRunner
{
    private readonly ILogger<ProcessRunner> _logger;
    
    public ProcessRunner(ILogger<ProcessRunner> logger)
    {
        _logger = logger;
    }
    
    public async Task<ProcessResult> RunAsync(
        string command,
        string? arguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? environment,
        CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments ?? "",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        
        if (environment != null)
        {
            foreach (var (key, value) in environment)
            {
                startInfo.Environment[key] = value;
            }
        }
        
        using var process = new System.Diagnostics.Process { StartInfo = startInfo };
        
        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();
        
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
                stdoutBuilder.AppendLine(e.Data);
        };
        
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                stderrBuilder.AppendLine(e.Data);
        };
        
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        try
        {
            await process.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to kill process tree");
            }
            throw;
        }
        
        return new ProcessResult(
            process.ExitCode,
            stdoutBuilder.ToString(),
            stderrBuilder.ToString());
    }
}
```

### CLI Command

```csharp
// Acode.Cli/Commands/Workflow/VerifyCommand.cs
namespace Acode.Cli.Commands.Workflow;

[Command("verify", Description = "Run pre-commit verification pipeline")]
public sealed class VerifyCommand : ICommand
{
    [CommandOption("step|s", Description = "Run specific steps only")]
    public string[]? Steps { get; init; }
    
    [CommandOption("fail-fast", Description = "Stop on first failure (default: true)")]
    public bool FailFast { get; init; } = true;
    
    [CommandOption("json", Description = "Output as JSON")]
    public bool Json { get; init; }
    
    [CommandOption("verbose|v", Description = "Show step output")]
    public bool Verbose { get; init; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var pipeline = GetPipeline(); // DI
        var workDir = GetWorkingDirectory();
        
        // Validate configuration first
        var errors = pipeline.ValidateConfiguration();
        if (errors.Count > 0)
        {
            console.Error.WriteLine("Pipeline configuration errors:");
            foreach (var error in errors)
            {
                console.Error.WriteLine($"  - {error}");
            }
            Environment.ExitCode = ExitCodes.ConfigurationError;
            return;
        }
        
        var options = new PipelineOptions
        {
            FailFast = FailFast,
            StepFilter = Steps?.ToList()
        };
        
        var progress = new Progress<StepProgress>(p =>
        {
            if (Json) return;
            
            var icon = p.State switch
            {
                StepProgressState.Running => "⏳",
                StepProgressState.Passed => "✓",
                StepProgressState.Failed => "✗",
                StepProgressState.Skipped => "⊘",
                StepProgressState.TimedOut => "⏱",
                _ => " "
            };
            
            console.Output.WriteLine($"[{p.CurrentStep}/{p.TotalSteps}] {icon} {p.StepName}");
        });
        
        var result = await pipeline.RunAsync(workDir, options, progress);
        
        if (Json)
        {
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            console.Output.WriteLine(json);
        }
        else
        {
            console.Output.WriteLine();
            
            if (Verbose)
            {
                foreach (var step in result.Steps.Where(s => !s.Skipped))
                {
                    console.Output.WriteLine($"--- {step.StepName} ---");
                    if (!string.IsNullOrEmpty(step.StandardOutput))
                    {
                        console.Output.WriteLine(step.StandardOutput);
                    }
                    if (!string.IsNullOrEmpty(step.StandardError))
                    {
                        console.Error.WriteLine(step.StandardError);
                    }
                    console.Output.WriteLine();
                }
            }
            
            console.Output.WriteLine(result.Success 
                ? $"✓ {result.Summary}" 
                : $"✗ {result.Summary}");
            console.Output.WriteLine($"Duration: {result.Duration.TotalSeconds:F1}s");
        }
        
        if (!result.Success)
        {
            Environment.ExitCode = ExitCodes.VerificationFailed;
        }
    }
}
```

### Error Codes

```csharp
// Acode.Cli/ExitCodes.cs (additions)
public static partial class ExitCodes
{
    // Pipeline errors: 70-79
    public const int VerificationFailed = 70;
    public const int StepTimedOut = 71;
    public const int ConfigurationError = 72;
    public const int DependencyError = 73;
}
```

### Implementation Checklist

- [ ] Create `PipelineStep` record with all properties
- [ ] Create `StepType` enum
- [ ] Create `StepResult` with factory methods
- [ ] Create `PipelineResult` with computed properties
- [ ] Create `PipelineException` hierarchy
- [ ] Create `PipelineOptions` record
- [ ] Define `IPreCommitPipeline` interface
- [ ] Create `StepProgress` and `StepProgressState`
- [ ] Implement `IProjectTypeDetector`
- [ ] Implement `StepExecutor.ExecuteAsync` with timeout
- [ ] Implement `StepExecutor` command resolution
- [ ] Implement output truncation with tail preservation
- [ ] Implement secret redaction in output
- [ ] Implement `PreCommitPipeline.RunAsync`
- [ ] Implement `PreCommitPipeline.ValidateConfiguration`
- [ ] Implement topological sort for dependencies
- [ ] Implement circular dependency detection
- [ ] Implement `IProcessRunner`
- [ ] Add process tree termination on timeout
- [ ] Create `VerifyCommand` CLI
- [ ] Add verbose output mode
- [ ] Add JSON output mode
- [ ] Register services in DI
- [ ] Write unit tests for step execution
- [ ] Write unit tests for topological sort
- [ ] Write integration tests with real commands

### Rollout Plan

1. **Phase 1: Domain Models** (Day 1)
   - Create all records and enums
   - Unit test result factory methods

2. **Phase 2: Process Runner** (Day 1)
   - Implement process execution
   - Test timeout and cancellation

3. **Phase 3: Step Executor** (Day 2)
   - Implement command resolution
   - Add output truncation
   - Test built-in step types

4. **Phase 4: Pipeline** (Days 2-3)
   - Implement pipeline execution
   - Add dependency handling
   - Test fail-fast behavior

5. **Phase 5: CLI** (Day 3)
   - Implement verify command
   - Add progress reporting
   - Manual testing

6. **Phase 6: Polish** (Day 4)
   - Secret redaction
   - Error message refinement
   - Documentation

---

**End of Task 024.a Specification**