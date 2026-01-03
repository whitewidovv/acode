# Task 027.b: Docker Worker Pool

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Task 027 (Worker Pool), Task 027.a (Local), Task 001 (Modes)  

---

## Description

Task 027.b implements Docker container-based workers. Each worker runs in an isolated container. Containers provide stronger isolation than processes.

Docker workers MUST use a standard base image. The image MUST include .NET runtime and git. Custom images MUST be configurable. Images MUST be pulled on demand.

Container lifecycle MUST be managed. Containers MUST start and stop cleanly. Resources MUST be limited. Networking MUST be controlled. Volumes MUST mount work directories.

### Business Value

Docker workers enable:
- Strong isolation
- Reproducible environments
- Resource limits
- Network isolation
- Consistent tooling

### Scope Boundaries

This task covers Docker workers. Local workers are in Task 027.a. Pool management is in Task 027. Log multiplexing is in Task 027.c.

### Integration Points

- Task 027: Pool provides lifecycle
- Task 001: Mode affects availability
- Task 026: Queue for task claim
- Docker: Container runtime

### Failure Modes

- Docker unavailable → Fallback to local
- Image pull failure → Retry with backoff
- Container crash → Restart container
- Resource exhaustion → Kill container

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Container | Isolated runtime environment |
| Image | Container template |
| Registry | Image storage |
| Volume | Mounted directory |
| Network | Container connectivity |
| Dockerfile | Image build recipe |
| Healthcheck | Container liveness probe |

---

## Out of Scope

- Kubernetes deployment
- Docker Swarm
- Remote Docker hosts
- Custom registries auth
- GPU containers
- Windows containers

---

## Functional Requirements

### FR-001 to FR-030: Container Management

- FR-001: Docker client MUST be used
- FR-002: Docker availability MUST be checked
- FR-003: Unavailable MUST fallback gracefully
- FR-004: Container MUST use standard image
- FR-005: Default image MUST be configurable
- FR-006: Default: `mcr.microsoft.com/dotnet/sdk:8.0`
- FR-007: Custom image MUST be supported
- FR-008: Image MUST be pulled if missing
- FR-009: Pull MUST have timeout
- FR-010: Pull failure MUST retry
- FR-011: Max retries MUST be 3
- FR-012: Container MUST have unique name
- FR-013: Name format: `acode-worker-{id}`
- FR-014: Container MUST have labels
- FR-015: Labels MUST identify acode
- FR-016: Container MUST start
- FR-017: Start MUST have timeout
- FR-018: Start failure MUST be logged
- FR-019: Container MUST stop
- FR-020: Stop MUST be graceful
- FR-021: Stop timeout MUST be 10s
- FR-022: Force stop MUST kill
- FR-023: Container MUST be removed after
- FR-024: Removal MUST be optional
- FR-025: `--keep-containers` for debug
- FR-026: Container logs MUST be captured
- FR-027: Logs MUST stream during run
- FR-028: Healthcheck MUST be configured
- FR-029: Unhealthy MUST trigger restart
- FR-030: Restart policy MUST be on-failure

### FR-031 to FR-055: Resource Limits

- FR-031: CPU limit MUST be set
- FR-032: CPU default MUST be 1 core
- FR-033: Memory limit MUST be set
- FR-034: Memory default MUST be 512MB
- FR-035: Disk limit MUST be set
- FR-036: Disk via volume size
- FR-037: Network limit MUST be supported
- FR-038: PID limit MUST be set
- FR-039: PID default MUST be 100
- FR-040: OOM kill MUST be enabled
- FR-041: OOM event MUST be logged
- FR-042: Resource usage MUST be tracked
- FR-043: Stats MUST be polled
- FR-044: Stats interval MUST be 5s
- FR-045: High usage MUST warn
- FR-046: Limits MUST be configurable
- FR-047: Override per-task MAY exist
- FR-048: Task limits MUST not exceed pool
- FR-049: Resource events MUST emit
- FR-050: Metrics MUST be exported
- FR-051: Ulimits MUST be set
- FR-052: No privileged mode
- FR-053: No root user
- FR-054: Read-only root FS MAY be used
- FR-055: Security options MUST be set

### FR-056 to FR-075: Volumes and Network

- FR-056: Worktree MUST be mounted
- FR-057: Mount MUST be read-write
- FR-058: Output dir MUST be mounted
- FR-059: Temp dir MUST be mounted
- FR-060: Config MUST be mounted read-only
- FR-061: Source repo MUST be mounted
- FR-062: Mount paths MUST be configured
- FR-063: Network mode MUST be configurable
- FR-064: Default network MUST be bridge
- FR-065: Host network MUST be optional
- FR-066: No network MAY be used
- FR-067: Port mapping MUST be supported
- FR-068: DNS MUST be configurable
- FR-069: Extra hosts MUST be supported
- FR-070: Environment vars MUST be passed
- FR-071: Secrets MUST NOT be in env
- FR-072: Secrets MUST use files
- FR-073: Secret files MUST be mounted
- FR-074: Secret files MUST be read-only
- FR-075: Cleanup MUST remove volumes

---

## Non-Functional Requirements

- NFR-001: Container start MUST be <10s
- NFR-002: Container stop MUST be <15s
- NFR-003: Image pull MUST be <5min
- NFR-004: Stats poll MUST be <100ms
- NFR-005: Memory overhead MUST be <50MB
- NFR-006: No container leaks
- NFR-007: No volume leaks
- NFR-008: Graceful fallback
- NFR-009: Works with Docker/Podman
- NFR-010: No elevated host privileges

---

## User Manual Documentation

### Configuration

```yaml
workers:
  mode: docker
  count: 4
  
  docker:
    image: "mcr.microsoft.com/dotnet/sdk:8.0"
    pullPolicy: ifNotPresent  # always, never, ifNotPresent
    keepContainers: false
    network: bridge
    
    resources:
      cpus: 1.0
      memoryMb: 512
      pidsLimit: 100
      
    mounts:
      - source: /host/repo
        target: /workspace
        readOnly: false
```

### Docker Requirements

- Docker Engine 20.10+
- Docker CLI in PATH
- User in docker group (Linux)
- Sufficient disk space

### Fallback Behavior

If Docker is unavailable:
1. Warning logged
2. Fallback to local workers
3. Continue with process isolation

### Debugging

```bash
# Start with container preservation
acode worker start --mode docker --keep-containers

# View container logs
docker logs acode-worker-abc123

# Exec into running container
docker exec -it acode-worker-abc123 /bin/bash

# List acode containers
docker ps --filter "label=acode.worker=true"
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Container starts
- [ ] AC-002: Container stops
- [ ] AC-003: Image pulled
- [ ] AC-004: Volumes mounted
- [ ] AC-005: Resource limits work
- [ ] AC-006: Logs captured
- [ ] AC-007: Healthcheck works
- [ ] AC-008: Fallback works
- [ ] AC-009: No leaks
- [ ] AC-010: Security enforced
- [ ] AC-011: Metrics tracked
- [ ] AC-012: Cross-platform works

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Container config building
- [ ] UT-002: Resource limit calculation
- [ ] UT-003: Volume mount config
- [ ] UT-004: Fallback logic

### Integration Tests

- [ ] IT-001: Full container lifecycle
- [ ] IT-002: Image pull
- [ ] IT-003: Resource enforcement
- [ ] IT-004: Cleanup verification

---

## Implementation Prompt

### Interface

```csharp
public interface IDockerWorker
{
    string Id { get; }
    string? ContainerId { get; }
    WorkerStatus Status { get; }
    
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(bool force = false, 
        CancellationToken ct = default);
    Task<TaskResult> ExecuteAsync(QueuedTask task, 
        CancellationToken ct = default);
}

public interface IDockerClient
{
    Task<bool> IsAvailableAsync(
        CancellationToken ct = default);
        
    Task<string> CreateContainerAsync(
        ContainerConfig config,
        CancellationToken ct = default);
        
    Task StartContainerAsync(string containerId,
        CancellationToken ct = default);
        
    Task StopContainerAsync(string containerId,
        TimeSpan timeout,
        CancellationToken ct = default);
        
    Task RemoveContainerAsync(string containerId,
        CancellationToken ct = default);
        
    Task<ContainerStats> GetStatsAsync(string containerId,
        CancellationToken ct = default);
        
    IAsyncEnumerable<string> StreamLogsAsync(
        string containerId,
        CancellationToken ct = default);
}

public record ContainerConfig(
    string Image,
    string Name,
    IReadOnlyList<VolumeMount> Mounts,
    ResourceLimits Resources,
    IReadOnlyDictionary<string, string> Env,
    IReadOnlyDictionary<string, string> Labels,
    NetworkConfig Network);
```

---

**End of Task 027.b Specification**