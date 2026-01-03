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

### Interface

```csharp
public interface IPreCommitPipeline
{
    Task<PipelineResult> RunAsync(string workingDir, 
        PipelineOptions? options = null, CancellationToken ct = default);
}

public record PipelineStep(
    string Name,
    string Type,
    string? Command,
    int TimeoutSeconds,
    string? WorkingDirectory,
    IReadOnlyDictionary<string, string>? Environment);

public record PipelineResult(
    bool Success,
    TimeSpan Duration,
    IReadOnlyList<StepResult> Steps);
```

---

**End of Task 024.a Specification**