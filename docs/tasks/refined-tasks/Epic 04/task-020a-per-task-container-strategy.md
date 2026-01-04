# Task 020.a: Per-Task Container Strategy

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 020 (Docker Sandbox)  

---

## Description

### Overview

Task 020.a defines the per-task container strategy for the Docker sandboxing infrastructure. Each agent task MUST execute in a fresh, isolated container. Container reuse between tasks is strictly prohibited to ensure complete isolation and prevent state leakage between operations.

Fresh containers prevent one task from affecting another. File modifications made in one task MUST NOT persist to the next. Environment variable changes MUST NOT carry over. Package installations MUST NOT accumulate. Each task starts from a known, clean baseline.

### Business Value

1. **Reproducibility** - Same task produces same results regardless of previous operations
2. **Isolation** - Malicious or buggy code cannot persist beyond single task
3. **Debuggability** - Each task's behavior is independent and predictable
4. **Security** - No privilege escalation or credential leakage between tasks
5. **Resource Control** - Each container has defined resource limits
6. **Auditability** - Container naming enables clear tracking of what ran when

### Scope

This task delivers:

1. `IContainerLifecycleManager` interface for managing container lifecycle
2. `ContainerLifecycleManager` implementation with create/start/stop/remove
3. Container naming convention with session and task IDs
4. Image selection strategy based on task requirements
5. Parallel task container isolation
6. Automatic cleanup on task completion
7. Orphan container cleanup on agent startup
8. Resource limit configuration per container
9. Container health monitoring during execution

### Integration Points

| Component | Integration |
|-----------|-------------|
| Task 018 (CommandExecutor) | Executes commands inside containers |
| Task 020b (Cache Volumes) | Mounts shared cache volumes |
| Task 020c (Policy Enforcement) | Applies security policies |
| Task 021 (Artifact Management) | Extracts artifacts from containers |
| Docker Engine | Container runtime via Docker API |

### Failure Modes

| Failure | Behavior |
|---------|----------|
| Docker not running | Abort with `ACODE-CTN-010` error |
| Image not found | Pull image or abort with `ACODE-CTN-011` |
| Container create fails | Abort task with `ACODE-CTN-001` |
| Container start fails | Remove container, abort task |
| Cleanup fails | Log warning, continue, mark for retry |
| Orphan detected on startup | Remove orphan with warning log |
| Resource limit exceeded | Container killed, task fails |
| Docker API timeout | Retry with exponential backoff |

### Assumptions

1. Docker or compatible runtime is installed and accessible
2. User has permissions to create/manage containers
3. Required images are available (locally or from registry)
4. Session ID and task ID are provided by orchestration layer
5. Container networking is available

### Security Considerations

1. **No privileged containers** - Containers run without elevated privileges
2. **Read-only root filesystem** - Where possible, prevent file system modifications
3. **No host namespace sharing** - Network, PID, IPC namespaces isolated
4. **Dropped capabilities** - Only minimal capabilities enabled
5. **User namespacing** - Run as non-root user inside container

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Container | Isolated execution environment managed by Docker |
| Session ID | Unique identifier for agent session (UUID) |
| Task ID | Unique identifier for specific task (UUID or sequential) |
| Image | Container image containing runtime and tools |
| Orphan Container | Container left running from crashed session |
| Resource Limit | CPU, memory, disk constraints on container |
| Lifecycle | Create → Start → Run → Stop → Remove |

---

## Out of Scope

- **Volume mounting strategy** - See Task 020.b
- **Security policy enforcement** - See Task 020.c
- **Network isolation policies** - See Task 020.c
- **Image building/customization** - Future task
- **Container registry authentication** - Future task
- **Kubernetes orchestration** - Out of scope entirely

---

## Functional Requirements

### Container Lifecycle (FR-020A-01 to FR-020A-20)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-020A-01 | Define `IContainerLifecycleManager` interface | Must Have |
| FR-020A-02 | `CreateContainerAsync` MUST create new container | Must Have |
| FR-020A-03 | `StartContainerAsync` MUST start created container | Must Have |
| FR-020A-04 | `StopContainerAsync` MUST stop running container | Must Have |
| FR-020A-05 | `RemoveContainerAsync` MUST delete stopped container | Must Have |
| FR-020A-06 | `GetContainerStatusAsync` MUST return current status | Must Have |
| FR-020A-07 | Each task MUST get a fresh container | Must Have |
| FR-020A-08 | Container reuse between tasks MUST NOT occur | Must Have |
| FR-020A-09 | Container creation MUST be atomic (succeed or fail) | Must Have |
| FR-020A-10 | Failed creation MUST NOT leave orphan containers | Must Have |
| FR-020A-11 | Container MUST be removable even if not properly stopped | Should Have |
| FR-020A-12 | Force removal MUST be supported | Should Have |
| FR-020A-13 | Lifecycle events MUST be logged | Should Have |
| FR-020A-14 | Container ID MUST be returned after creation | Must Have |
| FR-020A-15 | Container name MUST be returned after creation | Must Have |
| FR-020A-16 | Multiple containers MUST be manageable concurrently | Must Have |
| FR-020A-17 | Lifecycle operations MUST support cancellation | Must Have |
| FR-020A-18 | Create MUST accept configuration options | Must Have |
| FR-020A-19 | Container labels MUST be settable | Should Have |
| FR-020A-20 | Container logs MUST be retrievable | Should Have |

### Container Naming (FR-020A-21 to FR-020A-32)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-020A-21 | Container name MUST follow pattern: `acode-{session}-{task}` | Must Have |
| FR-020A-22 | Session ID MUST be included in name | Must Have |
| FR-020A-23 | Task ID MUST be included in name | Must Have |
| FR-020A-24 | Names MUST be DNS-compatible (lowercase, alphanumeric, hyphens) | Must Have |
| FR-020A-25 | Names MUST be unique within Docker host | Must Have |
| FR-020A-26 | Name generation MUST be deterministic | Must Have |
| FR-020A-27 | Names MUST be parseable back to session and task | Should Have |
| FR-020A-28 | Name prefix `acode-` MUST be configurable | Could Have |
| FR-020A-29 | Name MUST NOT exceed 63 characters | Must Have |
| FR-020A-30 | Invalid characters MUST be replaced with hyphen | Should Have |
| FR-020A-31 | Container MUST have label with full session/task info | Should Have |
| FR-020A-32 | Name collision MUST be detected and rejected | Must Have |

### Image Selection (FR-020A-33 to FR-020A-48)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-020A-33 | Image MUST match task language requirements | Must Have |
| FR-020A-34 | .NET tasks MUST use appropriate .NET SDK image | Must Have |
| FR-020A-35 | Node.js tasks MUST use appropriate Node image | Must Have |
| FR-020A-36 | Multi-language tasks MUST use combined image | Should Have |
| FR-020A-37 | Default images MUST be configurable | Should Have |
| FR-020A-38 | Image version/tag MUST be specifiable | Should Have |
| FR-020A-39 | Image MUST be pulled if not present locally | Must Have |
| FR-020A-40 | Pull progress MUST be reported | Should Have |
| FR-020A-41 | Pull MUST support timeout | Must Have |
| FR-020A-42 | Pull failure MUST abort container creation | Must Have |
| FR-020A-43 | Image presence MUST be checkable | Should Have |
| FR-020A-44 | Custom image MUST be specifiable per task | Should Have |
| FR-020A-45 | Repo contract MAY override default images | Should Have |
| FR-020A-46 | Image digest/SHA MUST be loggable | Should Have |
| FR-020A-47 | Untrusted images MUST be rejectable via policy | Should Have |
| FR-020A-48 | Image list MUST be retrievable | Should Have |

### Parallel Task Isolation (FR-020A-49 to FR-020A-58)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-020A-49 | Parallel tasks MUST have separate containers | Must Have |
| FR-020A-50 | No container sharing between parallel tasks | Must Have |
| FR-020A-51 | Containers MUST NOT share writable volumes | Must Have |
| FR-020A-52 | Read-only cache volumes MAY be shared | Should Have |
| FR-020A-53 | Parallel container creation MUST be thread-safe | Must Have |
| FR-020A-54 | Concurrent lifecycle operations MUST be safe | Must Have |
| FR-020A-55 | Resource contention MUST be handled gracefully | Should Have |
| FR-020A-56 | Max concurrent containers MUST be configurable | Should Have |
| FR-020A-57 | Container limit reached MUST queue or reject | Should Have |
| FR-020A-58 | Parallel cleanup MUST be coordinated | Should Have |

### Cleanup (FR-020A-59 to FR-020A-78)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-020A-59 | Container cleanup MUST occur on task completion | Must Have |
| FR-020A-60 | Cleanup MUST occur regardless of task success/failure | Must Have |
| FR-020A-61 | Container MUST be stopped before removal | Must Have |
| FR-020A-62 | Force removal MUST be used if stop times out | Should Have |
| FR-020A-63 | Agent exit MUST trigger cleanup of all session containers | Must Have |
| FR-020A-64 | Agent startup MUST detect orphaned containers | Must Have |
| FR-020A-65 | Orphan detection MUST use label/name pattern | Must Have |
| FR-020A-66 | Orphan cleanup MUST be automatic | Must Have |
| FR-020A-67 | Orphan cleanup MUST log what was removed | Must Have |
| FR-020A-68 | Cleanup failure MUST NOT block task completion | Should Have |
| FR-020A-69 | Cleanup failure MUST be logged with details | Must Have |
| FR-020A-70 | Retry logic MUST be applied to failed cleanup | Should Have |
| FR-020A-71 | Cleanup MUST respect graceful shutdown period | Should Have |
| FR-020A-72 | Cleanup MUST use configurable timeout | Should Have |
| FR-020A-73 | Container logs MUST be captured before removal | Should Have |
| FR-020A-74 | Artifact extraction MUST occur before removal | Must Have |
| FR-020A-75 | Cleanup MUST support dry-run mode | Could Have |
| FR-020A-76 | Cleanup MUST track containers cleaned | Should Have |
| FR-020A-77 | Background cleanup worker MUST be supported | Should Have |
| FR-020A-78 | Cleanup MUST handle containers in any state | Must Have |

### Resource Limits (FR-020A-79 to FR-020A-92)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-020A-79 | CPU limit MUST be configurable | Should Have |
| FR-020A-80 | Memory limit MUST be configurable | Must Have |
| FR-020A-81 | Disk I/O limit SHOULD be configurable | Could Have |
| FR-020A-82 | Network bandwidth limit SHOULD be configurable | Could Have |
| FR-020A-83 | PIDs limit MUST be set to prevent fork bombs | Should Have |
| FR-020A-84 | Default limits MUST be defined | Must Have |
| FR-020A-85 | Limits MUST be enforced by Docker | Must Have |
| FR-020A-86 | Resource limit exceeded MUST kill container | Should Have |
| FR-020A-87 | OOM-killed containers MUST be detectable | Should Have |
| FR-020A-88 | Resource usage MUST be monitorable | Could Have |
| FR-020A-89 | Limits MUST be overridable per task | Should Have |
| FR-020A-90 | Repo contract MAY specify resource limits | Should Have |
| FR-020A-91 | Limits MUST be validated before container creation | Should Have |
| FR-020A-92 | Resource usage MUST be loggable | Should Have |

---

## Non-Functional Requirements

### Performance (NFR-020A-01 to NFR-020A-10)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-020A-01 | Container creation time | <3 seconds (cached image) | Must Have |
| NFR-020A-02 | Container start time | <1 second | Must Have |
| NFR-020A-03 | Container stop time | <10 seconds | Must Have |
| NFR-020A-04 | Container removal time | <2 seconds | Must Have |
| NFR-020A-05 | Image pull time | Depends on network | N/A |
| NFR-020A-06 | Orphan scan time | <5 seconds | Should Have |
| NFR-020A-07 | Max concurrent containers | 10+ | Should Have |
| NFR-020A-08 | Docker API call latency | <100ms | Should Have |
| NFR-020A-09 | Memory overhead per container | Platform default | N/A |
| NFR-020A-10 | Name generation time | <1ms | Must Have |

### Reliability (NFR-020A-11 to NFR-020A-20)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-020A-11 | Container creation success rate | >99.9% | Must Have |
| NFR-020A-12 | Cleanup success rate | >99.9% | Must Have |
| NFR-020A-13 | Orphan detection accuracy | 100% | Must Have |
| NFR-020A-14 | Docker API retry | 3 retries with backoff | Should Have |
| NFR-020A-15 | Docker connection recovery | Automatic | Should Have |
| NFR-020A-16 | Graceful shutdown | Clean all containers | Must Have |
| NFR-020A-17 | Crash recovery | Orphan cleanup on restart | Must Have |
| NFR-020A-18 | Container state consistency | Always accurate | Must Have |
| NFR-020A-19 | Concurrent operation safety | Thread-safe | Must Have |
| NFR-020A-20 | Resource leak prevention | Zero leaks | Must Have |

### Security (NFR-020A-21 to NFR-020A-28)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-020A-21 | No privileged containers | Enforced | Must Have |
| NFR-020A-22 | Minimal capabilities | Drop ALL, add minimal | Should Have |
| NFR-020A-23 | Non-root user | Run as UID 1000 | Should Have |
| NFR-020A-24 | No host mounts (except allowed) | Enforced | Must Have |
| NFR-020A-25 | Network isolation | Default bridge | Should Have |
| NFR-020A-26 | Credential protection | No secrets in containers | Must Have |
| NFR-020A-27 | Container escape prevention | Seccomp profile | Should Have |
| NFR-020A-28 | Image verification | Optional SHA check | Could Have |

### Maintainability (NFR-020A-29 to NFR-020A-36)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-020A-29 | Test coverage | >90% | Must Have |
| NFR-020A-30 | Docker mock support | Full mock layer | Must Have |
| NFR-020A-31 | Configuration externalization | All settings in config | Should Have |
| NFR-020A-32 | XML documentation | 100% public members | Must Have |
| NFR-020A-33 | Interface segregation | Single responsibility | Should Have |
| NFR-020A-34 | Dependency injection | All components | Must Have |
| NFR-020A-35 | Code complexity | <10 per method | Should Have |
| NFR-020A-36 | Error message clarity | Actionable messages | Must Have |

### Observability (NFR-020A-37 to NFR-020A-44)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-020A-37 | Log container creation | Info level | Must Have |
| NFR-020A-38 | Log container start/stop | Info level | Must Have |
| NFR-020A-39 | Log container removal | Debug level | Should Have |
| NFR-020A-40 | Log orphan cleanup | Warning level | Must Have |
| NFR-020A-41 | Log resource limits | Debug level | Should Have |
| NFR-020A-42 | Log Docker errors | Error level | Must Have |
| NFR-020A-43 | Structured container metrics | Available | Should Have |
| NFR-020A-44 | Correlation ID in logs | UUID per operation | Should Have |

---

## User Manual Documentation

### Overview

The Agentic Coder Bot uses Docker containers to isolate task execution. Each task runs in a fresh container that is automatically created and destroyed, ensuring no state persists between tasks.

### Container Configuration

```yaml
# .agent/config.yml
docker:
  # Enable/disable sandboxing
  enabled: true
  
  # Default images by language
  images:
    dotnet: mcr.microsoft.com/dotnet/sdk:8.0
    node: node:20-alpine
    python: python:3.12-slim
    
  # Resource limits
  resources:
    memory: 4g
    cpus: 2.0
    pids_limit: 1000
    
  # Cleanup settings
  cleanup:
    on_task_complete: true
    on_agent_exit: true
    orphan_on_startup: true
    timeout_seconds: 30
```

### Container Naming

Containers are named using the pattern: `acode-{session}-{task}`

Example: `acode-a1b2c3d4-build-001`

This naming enables:
- Tracking which session created the container
- Identifying orphaned containers from crashed sessions
- Correlating logs with specific tasks

### CLI Commands

```bash
# List active containers
acode docker list

# Inspect a container
acode docker inspect acode-a1b2c3d4-build-001

# Force cleanup of orphaned containers
acode docker cleanup

# Show container logs
acode docker logs acode-a1b2c3d4-build-001
```

### Troubleshooting

#### Container Creation Fails

**Problem:** `ACODE-CTN-001: Container creation failed`

**Possible Causes:**
1. Docker not running
2. Insufficient disk space
3. Image not available
4. Resource limits too restrictive

**Solutions:**
```bash
# Check Docker status
docker info

# Check disk space
docker system df

# Pull required image
docker pull mcr.microsoft.com/dotnet/sdk:8.0
```

#### Orphaned Containers

**Problem:** Containers remain after agent crash

**Solution:** Containers are automatically cleaned on next startup:
```
[WARN] Found 3 orphaned containers from session a1b2c3d4
[INFO] Removing: acode-a1b2c3d4-build-001
[INFO] Removing: acode-a1b2c3d4-test-002
[INFO] Removing: acode-a1b2c3d4-run-003
[INFO] Orphan cleanup complete
```

---

## Acceptance Criteria

### Container Lifecycle (AC-020A-01 to AC-020A-16)

- [ ] AC-020A-01: `IContainerLifecycleManager` interface exists in Domain layer
- [ ] AC-020A-02: `ContainerLifecycleManager` implementation creates containers via Docker API
- [ ] AC-020A-03: Each task starts with a fresh container
- [ ] AC-020A-04: No container is reused between tasks
- [ ] AC-020A-05: Container creation returns container ID and name
- [ ] AC-020A-06: Container start works for created containers
- [ ] AC-020A-07: Container stop works for running containers
- [ ] AC-020A-08: Container removal works for stopped containers
- [ ] AC-020A-09: Force removal works for any state
- [ ] AC-020A-10: Container status is accurately reported
- [ ] AC-020A-11: Lifecycle operations support cancellation
- [ ] AC-020A-12: Failed creation does not leave orphans
- [ ] AC-020A-13: All lifecycle events are logged
- [ ] AC-020A-14: Docker API errors are properly propagated
- [ ] AC-020A-15: Retries occur for transient failures
- [ ] AC-020A-16: Concurrent lifecycle operations are thread-safe

### Container Naming (AC-020A-17 to AC-020A-24)

- [ ] AC-020A-17: Container names follow pattern `acode-{session}-{task}`
- [ ] AC-020A-18: Names are DNS-compatible (lowercase, alphanumeric, hyphens)
- [ ] AC-020A-19: Names are unique within Docker host
- [ ] AC-020A-20: Names do not exceed 63 characters
- [ ] AC-020A-21: Names can be parsed back to session and task IDs
- [ ] AC-020A-22: Container labels include full session/task metadata
- [ ] AC-020A-23: Name collision is detected and rejected
- [ ] AC-020A-24: Invalid characters are replaced with hyphens

### Image Selection (AC-020A-25 to AC-020A-34)

- [ ] AC-020A-25: .NET tasks use configured .NET SDK image
- [ ] AC-020A-26: Node.js tasks use configured Node image
- [ ] AC-020A-27: Image is pulled automatically if not present
- [ ] AC-020A-28: Pull progress is reported
- [ ] AC-020A-29: Pull timeout is enforced
- [ ] AC-020A-30: Pull failure aborts container creation
- [ ] AC-020A-31: Custom image can be specified per task
- [ ] AC-020A-32: Repo contract can override default images
- [ ] AC-020A-33: Image digest is logged
- [ ] AC-020A-34: Missing image gives clear error message

### Parallel Task Isolation (AC-020A-35 to AC-020A-40)

- [ ] AC-020A-35: Parallel tasks each get separate containers
- [ ] AC-020A-36: No writable volumes are shared between parallel containers
- [ ] AC-020A-37: Concurrent container creation is thread-safe
- [ ] AC-020A-38: Max concurrent container limit is respected
- [ ] AC-020A-39: Container limit reached provides clear error
- [ ] AC-020A-40: Parallel cleanup is coordinated

### Cleanup (AC-020A-41 to AC-020A-56)

- [ ] AC-020A-41: Container is removed after task completion
- [ ] AC-020A-42: Cleanup occurs for both successful and failed tasks
- [ ] AC-020A-43: Container is stopped before removal
- [ ] AC-020A-44: Force removal is used if stop times out
- [ ] AC-020A-45: Agent exit triggers cleanup of all session containers
- [ ] AC-020A-46: Agent startup detects orphaned containers
- [ ] AC-020A-47: Orphan detection uses label/name pattern
- [ ] AC-020A-48: Orphans are automatically removed
- [ ] AC-020A-49: Orphan cleanup is logged with container names
- [ ] AC-020A-50: Cleanup failure does not block task completion
- [ ] AC-020A-51: Cleanup failure is logged with details
- [ ] AC-020A-52: Retry logic is applied to failed cleanup
- [ ] AC-020A-53: Graceful shutdown period is respected
- [ ] AC-020A-54: Container logs are captured before removal
- [ ] AC-020A-55: Artifacts are extracted before removal
- [ ] AC-020A-56: Cleanup handles containers in any state

### Resource Limits (AC-020A-57 to AC-020A-64)

- [ ] AC-020A-57: Memory limit is applied to containers
- [ ] AC-020A-58: CPU limit is applied to containers
- [ ] AC-020A-59: PIDs limit is applied to containers
- [ ] AC-020A-60: Default limits are used when not specified
- [ ] AC-020A-61: Limits are validated before container creation
- [ ] AC-020A-62: OOM-killed containers are detectable
- [ ] AC-020A-63: Limits are overridable per task
- [ ] AC-020A-64: Resource usage is logged

---

## Best Practices

### Container Lifecycle

1. **Fresh container per task** - Prevent state leakage between tasks
2. **Reuse base image** - Pull once, use many times
3. **Fast startup** - Minimize container initialization time
4. **Clean exit handling** - Proper cleanup even on failure

### Resource Management

5. **Set memory limits** - Prevent runaway memory consumption
6. **Set CPU limits** - Fair sharing of CPU resources
7. **PIDs limit** - Prevent fork bombs
8. **Disk quotas** - Limit writeable volume sizes

### State Handling

9. **Mount workspaces carefully** - Source code read-only when possible
10. **Output to specific volumes** - Artifacts to designated output volumes
11. **Ephemeral by default** - Container filesystem cleared on exit
12. **Log container metrics** - Track resource usage per task

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Domain/Docker/
├── ContainerConfigTests.cs
│   ├── Name_FollowsPattern()
│   ├── Name_IsDnsCompatible()
│   ├── Name_DoesNotExceed63Chars()
│   ├── Labels_ContainSessionAndTask()
│   ├── ResourceLimits_HaveDefaults()
│   └── Image_MatchesLanguage()
│
└── ContainerNameGeneratorTests.cs
    ├── Generate_IncludesSessionId()
    ├── Generate_IncludesTaskId()
    ├── Generate_IsDnsCompatible()
    ├── Generate_ReplacesInvalidChars()
    ├── Generate_TruncatesToMaxLength()
    ├── Parse_ExtractsSessionId()
    └── Parse_ExtractsTaskId()
```

```
Tests/Unit/Infrastructure/Docker/
├── ContainerLifecycleManagerTests.cs
│   ├── CreateAsync_CallsDockerCreate()
│   ├── CreateAsync_AppliesNameAndLabels()
│   ├── CreateAsync_AppliesResourceLimits()
│   ├── CreateAsync_PullsImageIfMissing()
│   ├── CreateAsync_ReturnsContainerIdAndName()
│   ├── CreateAsync_WhenFails_ThrowsException()
│   ├── CreateAsync_WhenFails_NoOrphansLeft()
│   ├── CreateAsync_SupportsCanellation()
│   ├── StartAsync_CallsDockerStart()
│   ├── StartAsync_WhenFails_ThrowsException()
│   ├── StopAsync_CallsDockerStop()
│   ├── StopAsync_WhenTimeout_ForcesKill()
│   ├── RemoveAsync_CallsDockerRemove()
│   ├── RemoveAsync_SupportsForce()
│   ├── RemoveAsync_WhenFails_Retries()
│   ├── GetStatusAsync_ReturnsCorrectStatus()
│   └── Operations_AreThreadSafe()
│
├── ImageManagerTests.cs
│   ├── SelectImage_ForDotNet_ReturnsDotNetSdk()
│   ├── SelectImage_ForNode_ReturnsNodeImage()
│   ├── SelectImage_UsesConfigOverride()
│   ├── SelectImage_UsesContractOverride()
│   ├── IsPresent_WhenExists_ReturnsTrue()
│   ├── IsPresent_WhenMissing_ReturnsFalse()
│   ├── PullAsync_PullsImage()
│   ├── PullAsync_ReportsProgress()
│   └── PullAsync_WhenFails_ThrowsException()
│
├── OrphanCleanupServiceTests.cs
│   ├── FindOrphans_ByNamePattern()
│   ├── FindOrphans_ByLabel()
│   ├── FindOrphans_ExcludesCurrentSession()
│   ├── Cleanup_RemovesAllOrphans()
│   ├── Cleanup_LogsRemovedContainers()
│   ├── Cleanup_ContinuesOnFailure()
│   └── Cleanup_ReportsCount()
│
└── ResourceLimitValidatorTests.cs
    ├── Validate_ValidLimits_ReturnsSuccess()
    ├── Validate_NegativeMemory_ReturnsError()
    ├── Validate_ZeroCpu_ReturnsError()
    ├── Validate_ExcessiveLimits_ReturnsWarning()
    └── ApplyDefaults_FillsMissingLimits()
```

### Integration Tests

```
Tests/Integration/Infrastructure/Docker/
├── ContainerLifecycleIntegrationTests.cs
│   ├── Should_Create_Container_With_Correct_Name()
│   ├── Should_Start_And_Stop_Container()
│   ├── Should_Remove_Container()
│   ├── Should_Force_Remove_Running_Container()
│   ├── Should_Apply_Resource_Limits()
│   ├── Should_Pull_Missing_Image()
│   ├── Should_Apply_Labels()
│   ├── Should_Execute_Command_In_Container()
│   └── Should_Cleanup_On_Error()
│
├── OrphanCleanupIntegrationTests.cs
│   ├── Should_Detect_Orphaned_Containers()
│   ├── Should_Remove_Orphaned_Containers()
│   ├── Should_Not_Remove_Current_Session_Containers()
│   └── Should_Handle_Mixed_Container_States()
│
└── ParallelContainerTests.cs
    ├── Should_Create_Multiple_Containers_Concurrently()
    ├── Should_Isolate_Parallel_Tasks()
    ├── Should_Respect_Max_Concurrent_Limit()
    └── Should_Cleanup_All_Parallel_Containers()
```

### E2E Tests

```
Tests/E2E/CLI/
└── DockerSandboxE2ETests.cs
    ├── Should_Execute_Task_In_Container()
    ├── Should_Isolate_Consecutive_Tasks()
    ├── Should_Cleanup_After_Task()
    ├── Should_Cleanup_After_Failure()
    ├── Should_List_Active_Containers()
    ├── Should_Show_Container_Logs()
    └── Should_Cleanup_Orphans()
```

### Performance Benchmarks

| Benchmark | Method | Target | Maximum |
|-----------|--------|--------|---------|
| Container creation | `Benchmark_Create` | 2s | 3s |
| Container start | `Benchmark_Start` | 500ms | 1s |
| Container stop | `Benchmark_Stop` | 5s | 10s |
| Container removal | `Benchmark_Remove` | 1s | 2s |
| Name generation | `Benchmark_NameGen` | 0.5ms | 1ms |
| Orphan scan (10 containers) | `Benchmark_OrphanScan` | 2s | 5s |
| Parallel create (5) | `Benchmark_ParallelCreate` | 5s | 10s |

### Coverage Requirements

| Component | Minimum | Target |
|-----------|---------|--------|
| `ContainerLifecycleManager` | 85% | 95% |
| `ImageManager` | 85% | 95% |
| `OrphanCleanupService` | 90% | 98% |
| `ContainerNameGenerator` | 100% | 100% |
| `ResourceLimitValidator` | 95% | 100% |
| Domain models | 100% | 100% |
| **Overall** | **90%** | **95%** |

---

## User Verification Steps

### Scenario 1: Fresh Container Per Task

**Objective:** Verify each task gets a fresh container

**Test Commands:**
```bash
# First task - create a file
acode run "echo 'test' > /tmp/marker.txt && cat /tmp/marker.txt"

# Second task - file should not exist
acode run "cat /tmp/marker.txt || echo 'File does not exist'"
```

**Expected Output:**
```
[Task 1]
test

[Task 2]
File does not exist
```

**Verification Checklist:**
- [ ] First task creates file successfully
- [ ] Second task cannot find the file
- [ ] State does not persist between tasks

---

### Scenario 2: Container Naming

**Objective:** Verify container names follow pattern

**Test Commands:**
```bash
acode build --verbose
acode docker list
```

**Expected Output:**
```
[DEBUG] Creating container: acode-a1b2c3d4-build-001
...
Active Containers:
  NAME                           STATUS    CREATED
  acode-a1b2c3d4-build-001      running   5s ago
```

**Verification Checklist:**
- [ ] Name follows acode-{session}-{task} pattern
- [ ] Session ID is consistent
- [ ] Task name is included

---

### Scenario 3: Automatic Cleanup

**Objective:** Verify containers are removed after task

**Test Commands:**
```bash
docker ps -a --filter "name=acode-" --format "{{.Names}}"
acode build
docker ps -a --filter "name=acode-" --format "{{.Names}}"
```

**Expected Output:**
```
# Before task
(empty)

# After task
(empty or only running containers)
```

**Verification Checklist:**
- [ ] Container exists during task
- [ ] Container is removed after task
- [ ] No orphans remain

---

### Scenario 4: Orphan Cleanup on Startup

**Objective:** Verify orphans are cleaned on agent start

**Setup:**
```bash
# Simulate crash by creating orphan container
docker run -d --name acode-orphan-test-123 alpine sleep 3600
```

**Test Commands:**
```bash
acode init
docker ps -a --filter "name=acode-orphan" --format "{{.Names}}"
```

**Expected Output:**
```
[WARN] Found 1 orphaned container(s) from previous session
[INFO] Removing: acode-orphan-test-123
[INFO] Orphan cleanup complete
```

**Verification Checklist:**
- [ ] Orphan is detected
- [ ] Orphan is removed
- [ ] Warning is logged

---

### Scenario 5: Resource Limits

**Objective:** Verify resource limits are applied

**Test Commands:**
```bash
acode run --verbose "cat /sys/fs/cgroup/memory.max"
```

**Expected Output:**
```
[DEBUG] Container resource limits: memory=4g, cpus=2.0
4294967296
```

**Verification Checklist:**
- [ ] Memory limit is applied
- [ ] CPU limit is applied
- [ ] Limits are logged

---

### Scenario 6: Parallel Task Isolation

**Objective:** Verify parallel tasks have separate containers

**Test Commands:**
```bash
acode test --parallel
acode docker list
```

**Expected Output:**
```
Running tests in parallel...

Active Containers:
  NAME                           STATUS
  acode-a1b2c3d4-test-unit-001   running
  acode-a1b2c3d4-test-int-002    running
```

**Verification Checklist:**
- [ ] Each parallel task has own container
- [ ] Containers have unique names
- [ ] All are cleaned after completion

---

### Scenario 7: Image Pull on Demand

**Objective:** Verify missing images are pulled

**Setup:**
```bash
docker rmi mcr.microsoft.com/dotnet/sdk:8.0 2>/dev/null
```

**Test Commands:**
```bash
acode build --verbose
```

**Expected Output:**
```
[INFO] Image not found locally: mcr.microsoft.com/dotnet/sdk:8.0
[INFO] Pulling image...
[INFO] Pull progress: 10%... 50%... 100%
[INFO] Image pulled successfully
[INFO] Creating container...
```

**Verification Checklist:**
- [ ] Missing image is detected
- [ ] Image is pulled automatically
- [ ] Progress is reported
- [ ] Build proceeds after pull

---

### Scenario 8: Cleanup After Failure

**Objective:** Verify cleanup occurs even when task fails

**Test Commands:**
```bash
acode run "exit 1"
docker ps -a --filter "name=acode-" --format "{{.Names}}"
```

**Expected Output:**
```
Error: Command failed with exit code 1
(No containers listed)
```

**Verification Checklist:**
- [ ] Task fails as expected
- [ ] Container is still cleaned up
- [ ] No orphans remain

---

## Implementation Prompt

You are implementing the per-task container strategy for the Docker sandboxing infrastructure that ensures each agent task executes in a fresh, isolated container.

### File Structure

```
src/AgenticCoder.Domain/
├── Docker/
│   ├── IContainerLifecycleManager.cs     # Lifecycle interface
│   ├── IImageManager.cs                  # Image management interface
│   ├── IOrphanCleanupService.cs          # Orphan cleanup interface
│   ├── ContainerConfig.cs                # Container configuration
│   ├── ContainerInfo.cs                  # Container info model
│   ├── ContainerStatus.cs                # Status enum
│   ├── ResourceLimits.cs                 # Resource limit model
│   └── ImageInfo.cs                      # Image info model

src/AgenticCoder.Application/
├── Docker/
│   └── ContainerOrchestrator.cs          # Coordinates container lifecycle

src/AgenticCoder.Infrastructure/
├── Docker/
│   ├── ContainerLifecycleManager.cs      # Lifecycle implementation
│   ├── ImageManager.cs                   # Image management
│   ├── OrphanCleanupService.cs           # Orphan cleanup
│   ├── ContainerNameGenerator.cs         # Name generation
│   ├── ResourceLimitValidator.cs         # Limit validation
│   └── DockerClientFactory.cs            # Docker client creation

src/AgenticCoder.CLI/
├── Commands/
│   └── DockerCommand.cs                  # Docker CLI commands

Tests/Unit/Domain/Docker/
├── ContainerConfigTests.cs
└── ContainerNameGeneratorTests.cs

Tests/Unit/Infrastructure/Docker/
├── ContainerLifecycleManagerTests.cs
├── ImageManagerTests.cs
├── OrphanCleanupServiceTests.cs
└── ResourceLimitValidatorTests.cs

Tests/Integration/Infrastructure/Docker/
├── ContainerLifecycleIntegrationTests.cs
├── OrphanCleanupIntegrationTests.cs
└── ParallelContainerTests.cs

Tests/E2E/CLI/
└── DockerSandboxE2ETests.cs
```

### Domain Models

```csharp
// src/AgenticCoder.Domain/Docker/IContainerLifecycleManager.cs
namespace AgenticCoder.Domain.Docker;

/// <summary>
/// Manages container lifecycle operations.
/// </summary>
public interface IContainerLifecycleManager
{
    /// <summary>
    /// Creates a new container.
    /// </summary>
    Task<ContainerInfo> CreateContainerAsync(
        ContainerConfig config,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Starts a created container.
    /// </summary>
    Task StartContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops a running container.
    /// </summary>
    Task StopContainerAsync(
        string containerId,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes a container.
    /// </summary>
    Task RemoveContainerAsync(
        string containerId,
        bool force = false,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets container status.
    /// </summary>
    Task<ContainerStatus> GetStatusAsync(
        string containerId,
        CancellationToken cancellationToken = default);
}
```

```csharp
// src/AgenticCoder.Domain/Docker/ContainerConfig.cs
namespace AgenticCoder.Domain.Docker;

/// <summary>
/// Configuration for creating a container.
/// </summary>
public sealed record ContainerConfig
{
    /// <summary>Unique session identifier.</summary>
    public required string SessionId { get; init; }
    
    /// <summary>Unique task identifier.</summary>
    public required string TaskId { get; init; }
    
    /// <summary>Container image to use.</summary>
    public required string Image { get; init; }
    
    /// <summary>Working directory inside container.</summary>
    public string? WorkingDirectory { get; init; }
    
    /// <summary>Environment variables.</summary>
    public IReadOnlyDictionary<string, string>? Environment { get; init; }
    
    /// <summary>Volume mounts.</summary>
    public IReadOnlyList<VolumeMount>? Mounts { get; init; }
    
    /// <summary>Resource limits.</summary>
    public ResourceLimits? Limits { get; init; }
    
    /// <summary>Custom labels.</summary>
    public IReadOnlyDictionary<string, string>? Labels { get; init; }
}
```

```csharp
// src/AgenticCoder.Domain/Docker/ResourceLimits.cs
namespace AgenticCoder.Domain.Docker;

/// <summary>
/// Resource limits for a container.
/// </summary>
public sealed record ResourceLimits
{
    /// <summary>Memory limit (e.g., "4g", "512m").</summary>
    public string? Memory { get; init; }
    
    /// <summary>CPU limit (e.g., 2.0 for 2 CPUs).</summary>
    public double? Cpus { get; init; }
    
    /// <summary>Maximum number of PIDs.</summary>
    public int? PidsLimit { get; init; }
    
    /// <summary>Default limits.</summary>
    public static ResourceLimits Default => new()
    {
        Memory = "4g",
        Cpus = 2.0,
        PidsLimit = 1000
    };
}
```

### Infrastructure Implementation

```csharp
// src/AgenticCoder.Infrastructure/Docker/ContainerLifecycleManager.cs
namespace AgenticCoder.Infrastructure.Docker;

public sealed class ContainerLifecycleManager : IContainerLifecycleManager
{
    private readonly IDockerClient _docker;
    private readonly ContainerNameGenerator _nameGenerator;
    private readonly ResourceLimitValidator _limitValidator;
    private readonly ILogger<ContainerLifecycleManager> _logger;
    
    public ContainerLifecycleManager(
        IDockerClient docker,
        ContainerNameGenerator nameGenerator,
        ResourceLimitValidator limitValidator,
        ILogger<ContainerLifecycleManager> logger)
    {
        _docker = docker;
        _nameGenerator = nameGenerator;
        _limitValidator = limitValidator;
        _logger = logger;
    }
    
    public async Task<ContainerInfo> CreateContainerAsync(
        ContainerConfig config,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        
        var name = _nameGenerator.Generate(config.SessionId, config.TaskId);
        _logger.LogInformation("Creating container: {Name}", name);
        
        var limits = config.Limits ?? ResourceLimits.Default;
        _limitValidator.Validate(limits);
        
        var labels = new Dictionary<string, string>
        {
            ["acode.session"] = config.SessionId,
            ["acode.task"] = config.TaskId,
            ["acode.created"] = DateTimeOffset.UtcNow.ToString("O")
        };
        
        if (config.Labels is not null)
        {
            foreach (var (key, value) in config.Labels)
                labels[key] = value;
        }
        
        try
        {
            var response = await _docker.Containers.CreateContainerAsync(
                new CreateContainerParameters
                {
                    Name = name,
                    Image = config.Image,
                    WorkingDir = config.WorkingDirectory,
                    Env = config.Environment?.Select(kv => $"{kv.Key}={kv.Value}").ToList(),
                    Labels = labels,
                    HostConfig = new HostConfig
                    {
                        Memory = ParseMemory(limits.Memory),
                        NanoCPUs = (long?)((limits.Cpus ?? 2.0) * 1_000_000_000),
                        PidsLimit = limits.PidsLimit,
                        Mounts = config.Mounts?.Select(ToMount).ToList(),
                        SecurityOpt = new[] { "no-new-privileges" },
                        CapDrop = new[] { "ALL" }
                    }
                },
                cancellationToken);
            
            _logger.LogDebug("Container created: {Id}", response.ID);
            
            return new ContainerInfo
            {
                Id = response.ID,
                Name = name,
                Status = ContainerStatus.Created
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create container: {Name}", name);
            throw new ContainerCreationException($"Failed to create container: {name}", ex);
        }
    }
    
    public async Task StopContainerAsync(
        string containerId,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var stopTimeout = timeout ?? TimeSpan.FromSeconds(10);
        _logger.LogDebug("Stopping container: {Id}", containerId);
        
        try
        {
            await _docker.Containers.StopContainerAsync(
                containerId,
                new ContainerStopParameters { WaitBeforeKillSeconds = (uint)stopTimeout.TotalSeconds },
                cancellationToken);
        }
        catch (DockerContainerNotFoundException)
        {
            _logger.LogDebug("Container already removed: {Id}", containerId);
        }
    }
    
    public async Task RemoveContainerAsync(
        string containerId,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Removing container: {Id}, force={Force}", containerId, force);
        
        const int maxRetries = 3;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await _docker.Containers.RemoveContainerAsync(
                    containerId,
                    new ContainerRemoveParameters { Force = force, RemoveVolumes = true },
                    cancellationToken);
                return;
            }
            catch (DockerContainerNotFoundException)
            {
                return; // Already removed
            }
            catch (Exception ex) when (i < maxRetries - 1)
            {
                _logger.LogWarning(ex, "Retry {Attempt} removing container: {Id}", i + 1, containerId);
                await Task.Delay(100 * (i + 1), cancellationToken);
            }
        }
    }
}
```

```csharp
// src/AgenticCoder.Infrastructure/Docker/OrphanCleanupService.cs
namespace AgenticCoder.Infrastructure.Docker;

public sealed class OrphanCleanupService : IOrphanCleanupService
{
    private readonly IDockerClient _docker;
    private readonly string _currentSessionId;
    private readonly ILogger<OrphanCleanupService> _logger;
    
    public async Task<int> CleanupOrphansAsync(CancellationToken cancellationToken = default)
    {
        var orphans = await FindOrphansAsync(cancellationToken);
        
        if (orphans.Count == 0)
        {
            _logger.LogDebug("No orphaned containers found");
            return 0;
        }
        
        _logger.LogWarning("Found {Count} orphaned container(s) from previous session", orphans.Count);
        
        int removed = 0;
        foreach (var container in orphans)
        {
            try
            {
                _logger.LogInformation("Removing: {Name}", container.Name);
                await _docker.Containers.RemoveContainerAsync(
                    container.Id,
                    new ContainerRemoveParameters { Force = true },
                    cancellationToken);
                removed++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove orphan: {Name}", container.Name);
            }
        }
        
        _logger.LogInformation("Orphan cleanup complete: removed {Count}", removed);
        return removed;
    }
    
    private async Task<IReadOnlyList<ContainerInfo>> FindOrphansAsync(CancellationToken ct)
    {
        var containers = await _docker.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["name"] = new Dictionary<string, bool> { ["acode-"] = true }
                }
            },
            ct);
        
        return containers
            .Where(c => !c.Names.Any(n => n.Contains(_currentSessionId)))
            .Select(c => new ContainerInfo
            {
                Id = c.ID,
                Name = c.Names.First().TrimStart('/'),
                Status = ParseStatus(c.State)
            })
            .ToList();
    }
}
```

### Error Codes

| Code | Meaning | Resolution |
|------|---------|------------|
| ACODE-CTN-001 | Container creation failed | Check Docker is running, image exists |
| ACODE-CTN-002 | Container start failed | Check container logs |
| ACODE-CTN-003 | Container stop failed | Force remove may be required |
| ACODE-CTN-004 | Container removal failed | Force remove, check mounts |
| ACODE-CTN-005 | Name collision | Ensure unique session/task IDs |
| ACODE-CTN-006 | Image pull failed | Check network, registry access |
| ACODE-CTN-007 | Resource limit invalid | Check memory/CPU values |
| ACODE-CTN-008 | Docker not available | Start Docker daemon |
| ACODE-CTN-009 | Max containers reached | Wait for cleanup or increase limit |
| ACODE-CTN-010 | Orphan cleanup failed | Manual cleanup may be required |

### Implementation Checklist

1. [ ] Create `IContainerLifecycleManager` interface in Domain
2. [ ] Create `IImageManager` interface in Domain
3. [ ] Create `IOrphanCleanupService` interface in Domain
4. [ ] Create domain models (`ContainerConfig`, `ContainerInfo`, `ResourceLimits`)
5. [ ] Implement `ContainerLifecycleManager` with Docker.DotNet
6. [ ] Implement `ContainerNameGenerator` with pattern `acode-{session}-{task}`
7. [ ] Implement `ResourceLimitValidator`
8. [ ] Implement `ImageManager` with pull support
9. [ ] Implement `OrphanCleanupService`
10. [ ] Implement `DockerClientFactory`
11. [ ] Implement `ContainerOrchestrator` in Application layer
12. [ ] Implement `DockerCommand` in CLI
13. [ ] Write unit tests for all components (90%+ coverage)
14. [ ] Write integration tests requiring Docker
15. [ ] Write E2E tests for CLI commands
16. [ ] Add XML documentation to all public members
17. [ ] Register services in DI container
18. [ ] Configure startup orphan cleanup hook

### Rollout Plan

1. **Phase 1 - Domain Models:** Create interfaces and models (0.5 day)
2. **Phase 2 - Name Generator:** Implement naming convention (0.5 day)
3. **Phase 3 - Lifecycle Manager:** Implement create/start/stop/remove (1 day)
4. **Phase 4 - Image Manager:** Implement image selection and pull (0.5 day)
5. **Phase 5 - Orphan Cleanup:** Implement orphan detection and removal (0.5 day)
6. **Phase 6 - CLI Commands:** Implement docker list/inspect/cleanup (0.5 day)
7. **Phase 7 - Testing:** Complete unit, integration, E2E tests (1.5 days)
8. **Phase 8 - Documentation:** Add XML docs and user manual (0.5 day)

---

**End of Task 020.a Specification**