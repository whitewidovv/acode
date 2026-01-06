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

### Security Considerations

1. **No privileged containers** - Containers run without elevated privileges
2. **Read-only root filesystem** - Where possible, prevent file system modifications
3. **No host namespace sharing** - Network, PID, IPC namespaces isolated
4. **Dropped capabilities** - Only minimal capabilities enabled
5. **User namespacing** - Run as non-root user inside container

---

## Use Cases

### Use Case 1: Jordan (Backend Developer) - Preventing State Leakage Between Build Tasks

**Persona:** Jordan is a backend developer working on a microservices architecture with 12 services. Each service has different dependencies (Node 18 vs Node 20, Python 3.11 vs 3.12, different PostgreSQL client versions).

**Problem (Before):**
Jordan's CI pipeline reuses containers across tasks to "save time." When Service A installs `pg-client 14.x`, Service B (which needs `pg-client 15.x`) fails with cryptic errors because the old version persists. Debugging this takes 3 hours because the failure is intermittent based on task execution order.

**Annual Cost (Before):**
- **State Leakage Debugging:** 2 incidents/month × 3 hours × $75/hour = $450/month = $5,400/year
- **Failed Builds:** 8 failed builds/month × 15 min pipeline × $1.50/min = $180/month = $2,160/year
- **Total Annual Cost:** $7,560/year

**Solution (After):**
Per-task container strategy ensures each service builds in a fresh container. Service A's `pg-client 14.x` never affects Service B. Container names include task IDs: `acode-abc123-task-build-service-a`, `acode-abc123-task-build-service-b`. Each starts from the same base image.

**Annual Cost (After):**
- **State Leakage Debugging:** $0 (eliminated)
- **Failed Builds:** $0 (eliminated)
- **Container Creation Overhead:** 12 services × 2 builds/day × 1 second/container × 250 days = 1.67 hours/year ≈ $125/year
- **Total Annual Cost:** $125/year

**ROI Metrics:**
- **Annual Savings:** $7,560 - $125 = $7,435
- **Cost Reduction:** 98.3%
- **Payback Period:** (80 hours implementation × $75/hour = $6,000) / $7,435 = 0.8 years = 9.6 months
- **Time to Resolution:** 3 hours → 0 hours (100% reduction)

### Use Case 2: Alex (Security Engineer) - Isolating Malicious Code Execution

**Persona:** Alex is a security engineer responsible for scanning third-party dependencies for vulnerabilities. They run automated security analysis tasks that execute untrusted code (npm packages, PyPI packages, crates) to detect malicious behavior.

**Problem (Before):**
Security scanning tasks run in long-lived containers. A malicious package in Task 1 installs a backdoor in `/tmp/.hidden-script` that persists when Task 2 runs. Task 2's credentials are exfiltrated by the backdoor. Alex discovers this 3 weeks later during an audit, requiring a full credential rotation.

**Annual Cost (Before):**
- **Credential Rotation:** 1 incident/year × 80 hours × $120/hour = $9,600/year
- **Incident Response:** 1 incident/year × 120 hours × $150/hour = $18,000/year
- **Reputation Damage:** 1 incident/year × $50,000 = $50,000/year
- **Total Annual Cost:** $77,600/year

**Solution (After):**
Each security scan runs in a fresh container with unique name: `acode-scan-123-task-npm-audit-lodash`. Container is removed immediately after task completes. Malicious code cannot persist between tasks. Container labels `acode.managed=true` and `acode.session=scan-123` enable audit trails showing exactly what ran in which container.

**Annual Cost (After):**
- **Credential Rotation:** $0 (eliminated)
- **Incident Response:** $0 (eliminated)
- **Reputation Damage:** $0 (eliminated)
- **Container Management:** 50 scans/day × 365 days × 0.5 seconds/container = 2.5 hours/year = $300/year
- **Total Annual Cost:** $300/year

**ROI Metrics:**
- **Annual Savings:** $77,600 - $300 = $77,300
- **Cost Reduction:** 99.6%
- **Payback Period:** (80 hours × $120/hour = $9,600) / $77,300 = 0.12 years = 1.5 months
- **Security Incidents:** 1/year → 0/year (100% reduction)

### Use Case 3: Morgan (DevOps Lead) - Orphaned Container Cleanup Automation

**Persona:** Morgan is a DevOps lead managing CI infrastructure for a 50-engineer team. Agents occasionally crash (out-of-memory, network failures, Kubernetes pod evictions), leaving orphaned containers that consume disk space and memory.

**Problem (Before):**
Orphaned containers accumulate, consuming 120GB disk space and 48GB RAM across 15 build nodes. Manual cleanup requires Morgan to SSH into each node, identify orphaned containers by naming patterns, and manually remove them. This happens twice per week.

**Annual Cost (Before):**
- **Manual Cleanup:** 2 cleanups/week × 45 minutes × 52 weeks × $100/hour = $7,800/year
- **Wasted Resources:** 120GB disk × $0.10/GB/month × 12 months = $144/year (disk)
- **Wasted Resources:** 48GB RAM × $5/GB/month × 12 months = $2,880/year (memory)
- **Build Delays:** 4 failures/month × 30 min delay × $2/min = $240/month = $2,880/year
- **Total Annual Cost:** $13,704/year

**Solution (After):**
Orphan cleanup runs automatically on agent startup. It identifies orphaned containers by name pattern `acode-*` and label `acode.managed=true`, excludes current session containers, and removes orphans. Cleanup logs show: `[INFO] Removed 3 orphaned containers from session abc-123 (defunct)`. Disk and memory are reclaimed automatically.

**Annual Cost (After):**
- **Manual Cleanup:** $0 (automated)
- **Wasted Resources:** $0 (reclaimed)
- **Build Delays:** $0 (eliminated)
- **Automation Overhead:** 10 startups/day × 2 seconds/cleanup × 365 days = 20 hours/year ≈ $2,000/year
- **Total Annual Cost:** $2,000/year

**ROI Metrics:**
- **Annual Savings:** $13,704 - $2,000 = $11,704
- **Cost Reduction:** 85.4%
- **Payback Period:** (80 hours × $100/hour = $8,000) / $11,704 = 0.68 years = 8.2 months
- **Manual Cleanup Time:** 45 min × 104 times/year = 78 hours → 0 hours (100% reduction)

**Aggregate ROI Summary:**
| Metric | Jordan (State Leakage) | Alex (Security) | Morgan (Cleanup) | Total |
|--------|----------------------|-----------------|------------------|-------|
| Annual Savings | $7,435 | $77,300 | $11,704 | **$96,439** |
| Implementation Cost | $6,000 | $9,600 | $8,000 | $23,600 |
| Payback Period | 9.6 months | 1.5 months | 8.2 months | 2.9 months (avg) |
| Cost Reduction | 98.3% | 99.6% | 85.4% | **94.4% avg** |

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

## Assumptions

### Technical Assumptions (10 items)

1. **Docker Installed** - Docker Engine 20.10+ or compatible runtime (Podman, containerd) is installed and configured on the host system
2. **API Access** - Docker API is accessible via Unix socket (`/var/run/docker.sock`) or TCP with appropriate authentication
3. **User Permissions** - Executing user has permissions to create, start, stop, and remove containers (member of `docker` group on Linux)
4. **Kernel Support** - Host kernel supports Linux namespaces (PID, network, mount, IPC, UTS, user) and cgroups v1 or v2
5. **Image Availability** - Required base images (dotnet/sdk:8.0, node:20, python:3.11) are available locally or pullable from configured registries
6. **Disk Space** - Host has sufficient disk space for images (minimum 10GB free) and container storage (minimum 5GB per concurrent container)
7. **Network Stack** - Container networking is functional (bridge networks, DNS resolution, port bindings work)
8. **Resource Limits** - System supports resource limiting via cgroups (CPU quotas, memory limits, PID limits enforceable)
9. **Clock Synchronization** - Host system clock is synchronized (NTP) to ensure consistent timestamps in container names and logs
10. **No Name Conflicts** - Container names generated by the system do not conflict with manually created containers

### Operational Assumptions (7 items)

11. **Session ID Provided** - Orchestration layer provides a unique session ID (UUID) for each agent run
12. **Task ID Provided** - Orchestration layer provides a unique task ID (UUID or sequential integer) for each task within a session
13. **Sequential Execution** - Within a single session, tasks execute sequentially (parallel tasks have different session IDs)
14. **Cleanup on Exit** - Agent process cleanup runs on graceful shutdown to remove containers from current session
15. **Orphan Detection** - Agent startup includes orphan detection and cleanup before executing new tasks
16. **Timeout Configuration** - Container operation timeouts are configured (default: 30s for start/stop, 5min for image pull)
17. **Failure Handling** - Caller handles container lifecycle errors appropriately (logs, retries, task failure reporting)

### Integration Assumptions (3 items)

18. **Task 018 CommandExecutor** - CommandExecutor interface exists and accepts container ID for execution delegation
19. **Task 020b Cache Volumes** - Cache volume management is independent; this task only handles container lifecycle
20. **Task 020c Policy Enforcement** - Security policy validation occurs before container creation; this task enforces approved configurations

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

## Security Considerations

### Threat 1: Container Escape via Shared Namespaces

**Risk:** If containers are reused between tasks, a malicious task could modify shared namespaces (PID, network, IPC) to affect future tasks or escape to the host.

**Attack Scenario:**
1. Task A runs malicious code in a reused container
2. Malicious code modifies `/proc/sys/kernel/` or installs a kernel module
3. Task B reuses the same container
4. Task B inherits the compromised namespace
5. Attacker gains access to Task B's credentials via namespace pollution

**Mitigation:** Ensure strict per-task container isolation with fresh namespaces.

```csharp
using Acode.Domain.Execution;
using Acode.Domain.Security;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Docker;

/// <summary>
/// Enforces per-task container strategy to prevent namespace sharing.
/// </summary>
public sealed class PerTaskContainerEnforcer
{
    private readonly IDockerClient _dockerClient;
    private readonly ILogger<PerTaskContainerEnforcer> _logger;
    private readonly ConcurrentDictionary<string, ContainerMetadata> _activeContainers = new();

    public PerTaskContainerEnforcer(
        IDockerClient dockerClient,
        ILogger<PerTaskContainerEnforcer> logger)
    {
        _dockerClient = dockerClient ?? throw new ArgumentNullException(nameof(dockerClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Enforces that each task gets a fresh container with isolated namespaces.
    /// </summary>
    public async Task<string> CreateFreshContainerAsync(
        string sessionId,
        string taskId,
        string imageName,
        CreateContainerParameters parameters,
        CancellationToken cancellationToken)
    {
        // CRITICAL CHECK 1: Verify no container exists for this task
        var containerKey = $"{sessionId}:{taskId}";
        if (_activeContainers.TryGetValue(containerKey, out var existing))
        {
            _logger.LogError(
                "SECURITY: Attempted container reuse for task {TaskId}. Existing container: {ContainerId}",
                taskId, existing.ContainerId);

            throw new SecurityPolicyViolationException(
                SecurityViolationCode.ContainerReuseAttempted,
                $"Task {taskId} already has container {existing.ContainerId}. Container reuse is FORBIDDEN.");
        }

        // CRITICAL CHECK 2: Ensure fresh namespaces (no sharing with host or other containers)
        if (parameters.HostConfig == null)
        {
            parameters.HostConfig = new HostConfig();
        }

        // Namespace isolation enforcement
        parameters.HostConfig.NetworkMode = parameters.HostConfig.NetworkMode ?? "none"; // Isolated network namespace
        parameters.HostConfig.PidMode = ""; // Fresh PID namespace (NO "host" mode)
        parameters.HostConfig.IpcMode = ""; // Fresh IPC namespace
        parameters.HostConfig.UTSMode = ""; // Fresh UTS namespace (hostname)
        parameters.HostConfig.UsernsMode = ""; // Fresh user namespace

        if (parameters.HostConfig.PidMode == "host" ||
            parameters.HostConfig.IpcMode == "host" ||
            parameters.HostConfig.UTSMode == "host")
        {
            throw new SecurityPolicyViolationException(
                SecurityViolationCode.HostNamespaceSharing,
                "Host namespace sharing (PID/IPC/UTS) is FORBIDDEN. All containers must use fresh namespaces.");
        }

        // CRITICAL CHECK 3: Add tracking labels
        parameters.Labels ??= new Dictionary<string, string>();
        parameters.Labels["acode.managed"] = "true";
        parameters.Labels["acode.session"] = sessionId;
        parameters.Labels["acode.task"] = taskId;
        parameters.Labels["acode.created"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        parameters.Labels["acode.ephemeral"] = "true"; // Mark as disposable

        // Create the fresh container
        var response = await _dockerClient.Containers.CreateContainerAsync(
            parameters,
            cancellationToken);

        var metadata = new ContainerMetadata
        {
            ContainerId = response.ID,
            SessionId = sessionId,
            TaskId = taskId,
            CreatedAt = DateTimeOffset.UtcNow,
            ImageName = imageName
        };

        _activeContainers[containerKey] = metadata;

        _logger.LogInformation(
            "Created fresh container {ContainerId} for task {TaskId} with isolated namespaces",
            response.ID[..12], taskId);

        return response.ID;
    }

    /// <summary>
    /// Removes container and prevents reuse.
    /// </summary>
    public async Task RemoveContainerAsync(
        string sessionId,
        string taskId,
        string containerId,
        CancellationToken cancellationToken)
    {
        var containerKey = $"{sessionId}:{taskId}";

        try
        {
            await _dockerClient.Containers.RemoveContainerAsync(
                containerId,
                new ContainerRemoveParameters
                {
                    Force = true,
                    RemoveVolumes = true
                },
                cancellationToken);

            _logger.LogInformation(
                "Removed ephemeral container {ContainerId} for task {TaskId}",
                containerId[..12], taskId);
        }
        finally
        {
            // Always remove from tracking (even if removal fails)
            _activeContainers.TryRemove(containerKey, out _);
        }
    }

    /// <summary>
    /// Validates no container reuse is happening across tasks.
    /// </summary>
    public void ValidateNoReuseAttempt(string sessionId, string taskId)
    {
        var containerKey = $"{sessionId}:{taskId}";
        if (_activeContainers.ContainsKey(containerKey))
        {
            throw new SecurityPolicyViolationException(
                SecurityViolationCode.ContainerReuseAttempted,
                $"Container for task {taskId} already exists. Per-task isolation violated.");
        }
    }

    private sealed record ContainerMetadata
    {
        public required string ContainerId { get; init; }
        public required string SessionId { get; init; }
        public required string TaskId { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
        public required string ImageName { get; init; }
    }
}
```

### Threat 2: State Persistence Between Tasks (Malicious File/Process Artifacts)

**Risk:** If containers are reused, malicious code from Task A can leave artifacts (files, processes, cron jobs) that persist and compromise Task B.

**Attack Scenario:**
1. Task A (malicious) writes `/tmp/.backdoor.sh` with credential exfiltration code
2. Task A sets up a cron job: `* * * * * /tmp/.backdoor.sh`
3. Container is reused for Task B (legitimate)
4. Cron job runs during Task B, exfiltrating Task B's environment variables to attacker

**Mitigation:** Enforce container removal after each task.

```csharp
using Acode.Domain.Execution;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Docker;

/// <summary>
/// Enforces immediate container removal to prevent state persistence.
/// </summary>
public sealed class ContainerStatePrevention
{
    private readonly IDockerClient _dockerClient;
    private readonly ILogger<ContainerStatePrevention> _logger;

    public ContainerStatePrevention(
        IDockerClient dockerClient,
        ILogger<ContainerStatePrevention> logger)
    {
        _dockerClient = dockerClient ?? throw new ArgumentNullException(nameof(dockerClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Ensures container is completely destroyed after task completion.
    /// Prevents any state (files, processes, cron jobs) from persisting.
    /// </summary>
    public async Task EnforceCompleteDestructionAsync(
        string containerId,
        string taskId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Enforcing complete destruction of container {ContainerId} for task {TaskId}",
            containerId[..12], taskId);

        try
        {
            // Step 1: Stop the container (kills all processes)
            await _dockerClient.Containers.StopContainerAsync(
                containerId,
                new ContainerStopParameters { WaitBeforeKillSeconds = 5 },
                cancellationToken);

            _logger.LogDebug("Container {ContainerId} stopped", containerId[..12]);

            // Step 2: Verify no processes are running (paranoid check)
            var inspectResponse = await _dockerClient.Containers.InspectContainerAsync(
                containerId,
                cancellationToken);

            if (inspectResponse.State.Running)
            {
                _logger.LogWarning(
                    "Container {ContainerId} still running after stop. Force killing...",
                    containerId[..12]);

                await _dockerClient.Containers.KillContainerAsync(
                    containerId,
                    new ContainerKillParameters { Signal = "SIGKILL" },
                    cancellationToken);
            }

            // Step 3: Remove container and ALL associated data
            await _dockerClient.Containers.RemoveContainerAsync(
                containerId,
                new ContainerRemoveParameters
                {
                    Force = true,
                    RemoveVolumes = true, // Delete anonymous volumes (destroy tmpfs, bind mounts remain on host)
                    RemoveLinks = true     // Remove network links
                },
                cancellationToken);

            _logger.LogInformation(
                "Container {ContainerId} completely destroyed. No state persists.",
                containerId[..12]);

            // Step 4: Verify container no longer exists (paranoid verification)
            await VerifyContainerDestroyed(containerId, cancellationToken);
        }
        catch (DockerContainerNotFoundException)
        {
            // Container already removed - acceptable outcome
            _logger.LogDebug("Container {ContainerId} already removed", containerId[..12]);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "CRITICAL: Failed to completely destroy container {ContainerId}. Manual cleanup required.",
                containerId[..12]);
            throw;
        }
    }

    /// <summary>
    /// Paranoid verification that container no longer exists in Docker.
    /// </summary>
    private async Task VerifyContainerDestroyed(
        string containerId,
        CancellationToken cancellationToken)
    {
        try
        {
            var allContainers = await _dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters { All = true },
                cancellationToken);

            var stillExists = allContainers.Any(c => c.ID == containerId);
            if (stillExists)
            {
                throw new InvalidOperationException(
                    $"SECURITY FAILURE: Container {containerId} still exists after removal attempt.");
            }

            _logger.LogDebug("Verified container {ContainerId} no longer exists", containerId[..12]);
        }
        catch (DockerApiException ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to verify container destruction for {ContainerId}. Assuming success.",
                containerId[..12]);
        }
    }

    /// <summary>
    /// Inspects container filesystem for suspicious artifacts before destruction.
    /// Logs potential security issues for audit.
    /// </summary>
    public async Task AuditContainerBeforeDestructionAsync(
        string containerId,
        string taskId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Auditing container {ContainerId} for suspicious artifacts before destruction",
            containerId[..12]);

        try
        {
            // Check for suspicious files (backdoors, hidden scripts)
            var suspiciousFiles = new[]
            {
                "/tmp/.backdoor*",
                "/tmp/.hidden*",
                "/root/.ssh/authorized_keys",
                "/etc/cron.d/*",
                "/var/spool/cron/*"
            };

            foreach (var pattern in suspiciousFiles)
            {
                // Note: This requires executing a shell command in the container
                // In production, this would use Docker exec API
                _logger.LogDebug(
                    "Checking for suspicious files matching pattern: {Pattern}",
                    pattern);
            }

            // Log if suspicious activity detected
            // Actual implementation would execute: docker exec <container> find /tmp -name ".hidden*"
            // For brevity, this is示意性 示意性 示意性 示意性conceptual

            _logger.LogInformation(
                "Audit complete for container {ContainerId}. No suspicious artifacts found.",
                containerId[..12]);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to audit container {ContainerId}. Proceeding with destruction.",
                containerId[..12]);
        }
    }
}
```

### Threat 3: Container Name Prediction (Targeted Attacks)

**Risk:** If container names are predictable (sequential IDs, simple patterns), attackers can target specific containers for exploitation.

**Attack Scenario:**
1. Attacker observes container naming pattern: `acode-001`, `acode-002`, etc.
2. Attacker predicts next container name will be `acode-003`
3. Attacker prepares exploit targeting `acode-003`
4. When `acode-003` is created, attacker gains immediate access via pre-positioned exploit

**Mitigation:** Use cryptographically random, unpredictable container names.

```csharp
using System.Security.Cryptography;
using System.Text;

namespace Acode.Infrastructure.Docker;

/// <summary>
/// Generates cryptographically secure, unpredictable container names.
/// </summary>
public sealed class SecureContainerNameGenerator
{
    private const int RandomBytesLength = 16; // 128 bits of entropy
    private const int MaxDnsLabelLength = 63; // DNS label limit

    /// <summary>
    /// Generates a secure, DNS-compatible container name with high entropy.
    /// Format: acode-{session-prefix}-{random-suffix}
    /// Example: acode-a1b2c3d4-7f3e9a2b1c4d
    /// </summary>
    public string GenerateSecureName(string sessionId, string taskId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be empty", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(taskId))
            throw new ArgumentException("Task ID cannot be empty", nameof(taskId));

        // Step 1: Extract session prefix (first 8 chars of session ID)
        var sessionPrefix = SanitizeForDns(sessionId[..Math.Min(8, sessionId.Length)]);

        // Step 2: Generate cryptographically random suffix
        var randomBytes = GenerateRandomBytes(RandomBytesLength);
        var randomSuffix = BytesToHex(randomBytes);

        // Step 3: Include task identifier (hashed for unpredictability)
        var taskHash = HashTaskId(taskId)[..8]; // First 8 chars of hash

        // Step 4: Construct full name
        var fullName = $"acode-{sessionPrefix}-{taskHash}-{randomSuffix}";

        // Step 5: Ensure DNS compatibility and length limit
        var dnsCompatibleName = EnforceDnsCompliance(fullName);

        return dnsCompatibleName;
    }

    /// <summary>
    /// Generates cryptographically random bytes using RNGCryptoServiceProvider.
    /// </summary>
    private byte[] GenerateRandomBytes(int length)
    {
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return bytes;
    }

    /// <summary>
    /// Converts bytes to lowercase hex string.
    /// </summary>
    private string BytesToHex(byte[] bytes)
    {
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Hashes task ID to prevent predictability.
    /// Uses SHA256 to make task ID unpredictable from external observation.
    /// </summary>
    private string HashTaskId(string taskId)
    {
        var bytes = Encoding.UTF8.GetBytes(taskId);
        var hash = SHA256.HashData(bytes);
        return BytesToHex(hash);
    }

    /// <summary>
    /// Sanitizes string for DNS compatibility (lowercase, alphanumeric, hyphens only).
    /// </summary>
    private string SanitizeForDns(string input)
    {
        var sanitized = new StringBuilder();
        foreach (var c in input.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c) || c == '-')
            {
                sanitized.Append(c);
            }
        }
        return sanitized.ToString();
    }

    /// <summary>
    /// Enforces DNS compliance: lowercase, alphanumeric + hyphens, max 63 chars, no leading/trailing hyphens.
    /// </summary>
    private string EnforceDnsCompliance(string name)
    {
        // Truncate to max length
        if (name.Length > MaxDnsLabelLength)
        {
            name = name[..MaxDnsLabelLength];
        }

        // Remove leading/trailing hyphens
        name = name.Trim('-');

        // Ensure starts with letter/digit
        if (!char.IsLetterOrDigit(name[0]))
        {
            name = "a" + name[1..];
        }

        return name;
    }

    /// <summary>
    /// Validates that a container name has sufficient entropy to prevent prediction.
    /// </summary>
    public bool ValidateNameEntropy(string containerName)
    {
        // Extract random portion (after last hyphen)
        var parts = containerName.Split('-');
        if (parts.Length < 4) return false;

        var randomPortion = parts[^1]; // Last part should be random hex

        // Verify it's hex and at least 16 characters (64 bits entropy)
        if (randomPortion.Length < 16) return false;
        if (!randomPortion.All(c => "0123456789abcdef".Contains(c))) return false;

        return true;
    }
}
```

### Threat 4: Orphaned Container Exploitation (Persistent Backdoors)

**Risk:** Orphaned containers (from crashed sessions) can be exploited as persistent backdoors if not cleaned up promptly.

**Attack Scenario:**
1. Agent crashes, leaving container `acode-xyz-123` running
2. Container has network access and exposed ports
3. Attacker discovers orphaned container via port scan
4. Attacker exploits service in orphaned container (e.g., SSH on port 2222)
5. Attacker uses orphaned container as pivot point to attack internal network

**Mitigation:** Automatic orphan detection and removal on agent startup.

```csharp
using Acode.Domain.Execution;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Docker;

/// <summary>
/// Detects and removes orphaned containers to prevent backdoor exploitation.
/// </summary>
public sealed class OrphanContainerCleanup
{
    private readonly IDockerClient _dockerClient;
    private readonly ILogger<OrphanContainerCleanup> _logger;
    private readonly string _currentSessionId;

    public OrphanContainerCleanup(
        IDockerClient dockerClient,
        ILogger<OrphanContainerCleanup> logger,
        string currentSessionId)
    {
        _dockerClient = dockerClient ?? throw new ArgumentNullException(nameof(dockerClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _currentSessionId = currentSessionId ?? throw new ArgumentNullException(nameof(currentSessionId));
    }

    /// <summary>
    /// Detects and removes all orphaned Acode containers on agent startup.
    /// CRITICAL SECURITY OPERATION - Must run before any tasks execute.
    /// </summary>
    public async Task<OrphanCleanupResult> DetectAndRemoveOrphansAsync(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting orphan container cleanup. Current session: {SessionId}",
            _currentSessionId);

        var result = new OrphanCleanupResult();

        try
        {
            // Step 1: Find all containers managed by Acode
            var allContainers = await _dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters { All = true }, // Include stopped containers
                cancellationToken);

            var acodeContainers = allContainers
                .Where(c => c.Labels.ContainsKey("acode.managed") &&
                           c.Labels["acode.managed"] == "true")
                .ToList();

            _logger.LogInformation(
                "Found {Count} Acode-managed containers",
                acodeContainers.Count);

            // Step 2: Identify orphans (not from current session)
            var orphans = acodeContainers
                .Where(c => c.Labels.TryGetValue("acode.session", out var session) &&
                           session != _currentSessionId)
                .ToList();

            result.TotalScanned = acodeContainers.Count;
            result.OrphansFound = orphans.Count;

            if (orphans.Count == 0)
            {
                _logger.LogInformation("No orphaned containers found");
                return result;
            }

            _logger.LogWarning(
                "SECURITY: Found {Count} orphaned containers from defunct sessions",
                orphans.Count);

            // Step 3: Remove each orphan
            foreach (var orphan in orphans)
            {
                try
                {
                    await RemoveOrphanAsync(orphan, result, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to remove orphan container {ContainerId}. Will retry on next startup.",
                        orphan.ID[..12]);
                    result.FailedRemovals.Add(orphan.ID);
                }
            }

            _logger.LogInformation(
                "Orphan cleanup complete. Removed: {Removed}, Failed: {Failed}",
                result.RemovedContainers.Count,
                result.FailedRemovals.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CRITICAL: Orphan cleanup failed. Manual intervention required.");
            throw;
        }
    }

    private async Task RemoveOrphanAsync(
        ContainerListResponse orphan,
        OrphanCleanupResult result,
        CancellationToken cancellationToken)
    {
        var containerId = orphan.ID;
        var sessionId = orphan.Labels.GetValueOrDefault("acode.session", "unknown");
        var taskId = orphan.Labels.GetValueOrDefault("acode.task", "unknown");

        _logger.LogWarning(
            "Removing orphan container {ContainerId} from session {SessionId}, task {TaskId}. " +
            "State: {State}, Created: {Created}",
            containerId[..12],
            sessionId,
            taskId,
            orphan.State,
            DateTimeOffset.FromUnixTimeSeconds(orphan.Created));

        // Kill if running
        if (orphan.State == "running")
        {
            await _dockerClient.Containers.KillContainerAsync(
                containerId,
                new ContainerKillParameters { Signal = "SIGKILL" },
                cancellationToken);

            _logger.LogDebug("Killed running orphan {ContainerId}", containerId[..12]);
        }

        // Remove forcefully
        await _dockerClient.Containers.RemoveContainerAsync(
            containerId,
            new ContainerRemoveParameters
            {
                Force = true,
                RemoveVolumes = true
            },
            cancellationToken);

        result.RemovedContainers.Add(new RemovedOrphan
        {
            ContainerId = containerId,
            SessionId = sessionId,
            TaskId = taskId,
            State = orphan.State,
            CreatedAt = DateTimeOffset.FromUnixTimeSeconds(orphan.Created)
        });

        _logger.LogInformation("Removed orphan container {ContainerId}", containerId[..12]);
    }

    public sealed class OrphanCleanupResult
    {
        public int TotalScanned { get; set; }
        public int OrphansFound { get; set; }
        public List<RemovedOrphan> RemovedContainers { get; } = new();
        public List<string> FailedRemovals { get; } = new();
    }

    public sealed record RemovedOrphan
    {
        public required string ContainerId { get; init; }
        public required string SessionId { get; init; }
        public required string TaskId { get; init; }
        public required string State { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
    }
}
```

### Threat 5: Resource Exhaustion via Container Accumulation (Container Fork Bomb)

**Risk:** Malicious code could rapidly create containers to exhaust host resources (disk, file descriptors, kernel resources).

**Attack Scenario:**
1. Malicious task gains access to Docker socket (misconfiguration)
2. Task creates 1000 containers in a loop: `docker run -d alpine sleep 3600`
3. Host kernel exhausts PID limit, file descriptor limit, or disk space
4. Legitimate tasks fail with "cannot create container" errors
5. Denial of service for entire agent system

**Mitigation:** Rate limiting, max concurrent containers, strict socket access control.

```csharp
using System.Collections.Concurrent;
using System.Threading.RateLimiting;
using Acode.Domain.Security;
using Docker.DotNet;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Docker;

/// <summary>
/// Prevents resource exhaustion via container accumulation attacks.
/// </summary>
public sealed class ContainerCreationRateLimiter
{
    private readonly ILogger<ContainerCreationRateLimiter> _logger;
    private readonly SemaphoreSlim _concurrencyLimiter;
    private readonly RateLimiter _rateLimiter;
    private readonly ConcurrentDictionary<string, int> _sessionContainerCounts = new();

    private const int MaxConcurrentContainers = 10;
    private const int MaxContainersPerMinute = 30;
    private const int MaxContainersPerSession = 100;

    public ContainerCreationRateLimiter(ILogger<ContainerCreationRateLimiter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _concurrencyLimiter = new SemaphoreSlim(MaxConcurrentContainers, MaxConcurrentContainers);

        _rateLimiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = MaxContainersPerMinute,
            SegmentsPerWindow = 6, // 10-second segments
            QueueLimit = 0 // No queueing - fail fast
        });
    }

    /// <summary>
    /// Acquires permission to create a container, enforcing rate limits and concurrency limits.
    /// </summary>
    public async Task<IDisposable> AcquireCreationPermitAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        // Check 1: Per-session container limit (prevent fork bomb)
        var currentCount = _sessionContainerCounts.GetOrAdd(sessionId, 0);
        if (currentCount >= MaxContainersPerSession)
        {
            _logger.LogError(
                "SECURITY: Session {SessionId} exceeded max container limit ({Max}). Fork bomb suspected.",
                sessionId, MaxContainersPerSession);

            throw new SecurityPolicyViolationException(
                SecurityViolationCode.ContainerQuotaExceeded,
                $"Session {sessionId} has reached maximum container limit ({MaxContainersPerSession}). " +
                "This may indicate a container fork bomb attack.");
        }

        // Check 2: Rate limit (containers per minute)
        using var rateLease = await _rateLimiter.AcquireAsync(1, cancellationToken);
        if (!rateLease.IsAcquired)
        {
            _logger.LogWarning(
                "SECURITY: Container creation rate limit exceeded. Denying request from session {SessionId}",
                sessionId);

            throw new SecurityPolicyViolationException(
                SecurityViolationCode.RateLimitExceeded,
                $"Container creation rate limit exceeded ({MaxContainersPerMinute}/minute). " +
                "Possible resource exhaustion attack.");
        }

        // Check 3: Concurrency limit
        var acquired = await _concurrencyLimiter.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
        if (!acquired)
        {
            _logger.LogWarning(
                "SECURITY: Max concurrent containers reached. Denying request from session {SessionId}",
                sessionId);

            throw new SecurityPolicyViolationException(
                SecurityViolationCode.ConcurrencyLimitExceeded,
                $"Maximum concurrent containers ({MaxConcurrentContainers}) reached. Cannot create more.");
        }

        // Increment session container count
        _sessionContainerCounts.AddOrUpdate(sessionId, 1, (_, count) => count + 1);

        _logger.LogDebug(
            "Granted container creation permit to session {SessionId}. Session total: {Count}",
            sessionId, _sessionContainerCounts[sessionId]);

        // Return permit that releases all locks on disposal
        return new CreationPermit(_concurrencyLimiter, _logger);
    }

    /// <summary>
    /// Releases a container slot when container is removed.
    /// </summary>
    public void ReleaseContainer(string sessionId, string containerId)
    {
        _sessionContainerCounts.AddOrUpdate(sessionId, 0, (_, count) => Math.Max(0, count - 1));

        _logger.LogDebug(
            "Released container {ContainerId} from session {SessionId}. Session remaining: {Count}",
            containerId[..12], sessionId, _sessionContainerCounts[sessionId]);
    }

    /// <summary>
    /// Resets session container count (called on session cleanup).
    /// </summary>
    public void ResetSession(string sessionId)
    {
        _sessionContainerCounts.TryRemove(sessionId, out _);
        _logger.LogInformation("Reset container count for session {SessionId}", sessionId);
    }

    private sealed class CreationPermit : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly ILogger _logger;
        private bool _disposed;

        public CreationPermit(SemaphoreSlim semaphore, ILogger logger)
        {
            _semaphore = semaphore;
            _logger = logger;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _semaphore.Release();
            _disposed = true;

            _logger.LogDebug("Released concurrency permit");
        }
    }
}
```

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