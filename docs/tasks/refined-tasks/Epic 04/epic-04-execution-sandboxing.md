# EPIC 4 — Execution & Sandboxing

**Priority:** P0 – Critical  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Epic 02 (CLI Foundation), Epic 03 (Repo Intelligence)  

---

## Epic Overview

Epic 4 implements execution and sandboxing capabilities. The agent must execute code safely. Commands must run in controlled environments. Output must be captured and analyzed.

Execution is how the agent verifies work. The agent writes code. It runs tests. It checks compilation. Execution provides feedback.

Safety is paramount. The agent runs arbitrary commands. These commands must be contained. Resources must be limited. Damage must be prevented.

Command execution is structured. Every command has inputs: working directory, environment, timeout. Every command has outputs: stdout, stderr, exit code. Consistent handling enables reliable operation.

Language runners specialize execution. .NET projects have `dotnet build` and `dotnet test`. Node.js projects have `npm install` and `npm test`. Runners know the conventions.

Docker sandboxing provides isolation. Untrusted code runs in containers. File system access is controlled. Network access is policy-based. Resource limits prevent runaway processes.

Artifact collection preserves results. Build outputs, test results, logs are captured. Runs can be inspected after completion. Results can be exported for analysis.

This epic builds on Task 001's operating modes. Local-only mode runs directly. Docker mode runs in containers. Burst mode has separate policies.

---

## Outcomes

1. Structured command execution with consistent output capture
2. Timeout enforcement preventing hung processes
3. Working directory isolation per command
4. Environment variable management and redaction
5. Artifact logging with intelligent truncation
6. .NET project detection and execution
7. Node.js project detection and execution
8. Test runner abstraction for multiple frameworks
9. Repo contract command integration
10. Docker container lifecycle management
11. Per-task container isolation
12. Cache volume support for package managers
13. Policy enforcement within sandboxes
14. Artifact directory standardization
15. Run history and log inspection
16. Diff viewing for run comparisons
17. Export bundle format for sharing

---

## Non-Goals

1. **Real-time streaming** - Batch output capture only
2. **Remote execution** - Local/Docker only
3. **GPU support** - CPU-only execution
4. **Custom runtimes** - .NET and Node.js only
5. **Container orchestration** - Single container per task
6. **Persistent containers** - Fresh containers per run
7. **Network proxying** - Direct or blocked
8. **Multi-architecture** - Host architecture only
9. **Resource monitoring** - Limits only, no metrics
10. **Distributed builds** - Single machine only

---

## Architecture & Integration Points

### Domain Layer

```
AgenticCoder.Domain/Execution/
├── ICommand.cs              # Command abstraction
├── CommandResult.cs         # Execution result
├── IRunner.cs               # Language runner interface
├── ISandbox.cs              # Sandbox abstraction
├── RunRecord.cs             # Historical run record
└── Artifact.cs              # Output artifact
```

### Application Layer

```
AgenticCoder.Application/Execution/
├── ICommandExecutor.cs      # Execute commands
├── ILanguageRunnerFactory.cs # Create runners
├── ISandboxManager.cs       # Manage sandboxes
├── IArtifactCollector.cs    # Collect artifacts
└── IRunHistoryService.cs    # Query run history
```

### Infrastructure Layer

```
AgenticCoder.Infrastructure/Execution/
├── CommandExecutor.cs       # Process execution
├── DotNetRunner.cs          # .NET execution
├── NodeRunner.cs            # Node.js execution
├── DockerSandbox.cs         # Docker integration
├── ArtifactStore.cs         # Artifact storage
└── RunHistoryRepository.cs  # Run persistence
```

### Key Interfaces

```csharp
public interface ICommandExecutor
{
    Task<CommandResult> ExecuteAsync(
        Command command,
        ExecutionOptions options,
        CancellationToken ct = default);
}

public interface ISandbox
{
    Task<SandboxResult> RunAsync(
        Command command,
        SandboxPolicy policy,
        CancellationToken ct = default);
    
    Task CleanupAsync(CancellationToken ct = default);
}

public interface ILanguageRunner
{
    string Language { get; }
    Task<bool> DetectAsync(string projectPath, CancellationToken ct);
    Task<CommandResult> BuildAsync(string projectPath, CancellationToken ct);
    Task<CommandResult> TestAsync(string projectPath, CancellationToken ct);
}
```

### Integration Points

| Component | Integrates With | Purpose |
|-----------|-----------------|---------|
| CommandExecutor | RepoFS (Task 014) | Working directory |
| LanguageRunner | Config (Task 002) | Repo commands |
| DockerSandbox | OperatingModes (Task 001) | Mode enforcement |
| ArtifactStore | Database (Task 050) | Persistence |
| RunHistory | CLI (Task 010) | User inspection |

---

## Operational Considerations

### Operating Mode Compliance

- **Local-Only Mode:** Direct process execution, no containers
- **Docker Mode:** All commands in containers
- **Burst Mode:** Cached containers for speed
- **Air-Gapped Mode:** No network in sandbox

### Security

- Commands sanitized before execution
- Environment variables redacted in logs
- Sensitive files excluded from artifacts
- Container runs as non-root user
- Resource limits prevent DoS

### Audit Trail

- Every command logged with inputs/outputs
- Run history persists across sessions
- Artifacts retained per policy
- Export includes full context

### Resource Management

- Timeout kills hung processes
- Memory limits prevent OOM
- Disk limits prevent fill
- Concurrent execution limits

---

## Acceptance Criteria / Definition of Done

### Command Execution

- [ ] AC-001: Commands execute successfully
- [ ] AC-002: Stdout captured correctly
- [ ] AC-003: Stderr captured correctly
- [ ] AC-004: Exit codes returned
- [ ] AC-005: Timeouts enforced
- [ ] AC-006: Working directory set
- [ ] AC-007: Environment variables set
- [ ] AC-008: Secrets redacted in logs

### Language Runners

- [ ] AC-009: .NET projects detected
- [ ] AC-010: .NET build works
- [ ] AC-011: .NET test works
- [ ] AC-012: Node.js projects detected
- [ ] AC-013: npm install works
- [ ] AC-014: npm test works
- [ ] AC-015: Repo contract commands work

### Docker Sandbox

- [ ] AC-016: Container starts
- [ ] AC-017: Container stops
- [ ] AC-018: Files mounted
- [ ] AC-019: Output captured
- [ ] AC-020: Limits enforced
- [ ] AC-021: Network policy works
- [ ] AC-022: Cache volumes work

### Artifacts

- [ ] AC-023: Artifacts collected
- [ ] AC-024: Artifacts stored
- [ ] AC-025: Artifacts retrieved
- [ ] AC-026: Truncation works
- [ ] AC-027: Standards followed

### Run History

- [ ] AC-028: Runs recorded
- [ ] AC-029: Runs queryable
- [ ] AC-030: Logs viewable
- [ ] AC-031: Diffs work
- [ ] AC-032: Export works

---

## Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Command injection | Critical | Medium | Strict sanitization |
| Runaway process | High | Medium | Timeout + memory limits |
| Container escape | Critical | Low | Rootless containers |
| Disk fill | High | Medium | Disk quotas |
| Secret exposure | Critical | Medium | Redaction patterns |
| Docker unavailable | Medium | Low | Graceful fallback |
| Slow container start | Medium | High | Image caching |
| Network abuse | High | Low | Deny by default |
| Memory exhaustion | High | Medium | Memory limits |
| Orphaned containers | Medium | Medium | Cleanup on exit |
| Large artifacts | Medium | High | Size limits + truncation |
| Race conditions | Medium | Low | Proper synchronization |

---

## Milestone Plan

### Milestone 1: Command Execution Foundation
**Tasks:** 018, 018.a, 018.b, 018.c  
**Deliverables:**
- ICommandExecutor implementation
- Stdout/stderr/exit code capture
- Timeout and working directory support
- Artifact logging with truncation

### Milestone 2: Language Runners
**Tasks:** 019, 019.a, 019.b, 019.c  
**Deliverables:**
- .NET runner with build/test
- Node.js runner with npm commands
- Project layout detection
- Repo contract integration

### Milestone 3: Docker Sandboxing
**Tasks:** 020, 020.a, 020.b, 020.c  
**Deliverables:**
- Docker container lifecycle
- Per-task isolation
- Cache volumes for packages
- Policy enforcement

### Milestone 4: Artifact Management
**Tasks:** 021, 021.a, 021.b, 021.c  
**Deliverables:**
- Artifact directory structure
- Run inspection CLI
- Diff commands
- Export bundle format

---

## Definition of Epic Complete

- [ ] All Task 018.x specifications implemented
- [ ] All Task 019.x specifications implemented
- [ ] All Task 020.x specifications implemented
- [ ] All Task 021.x specifications implemented
- [ ] Command execution works reliably
- [ ] .NET and Node.js runners functional
- [ ] Docker sandboxing operational
- [ ] Artifacts collected and inspectable
- [ ] Run history queryable
- [ ] Export format documented
- [ ] All unit tests passing
- [ ] All integration tests passing
- [ ] All E2E tests passing
- [ ] Performance benchmarks met
- [ ] Security review completed
- [ ] Documentation complete
- [ ] CLI commands documented
- [ ] Configuration options documented
- [ ] Error messages clear
- [ ] Logging comprehensive

---

**END OF EPIC 4**