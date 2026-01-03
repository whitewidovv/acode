# Task 014.b: Docker-Mounted FS Implementation

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 014 (RepoFS Abstraction), Task 014.a (Local FS)  

---

## Description

Task 014.b implements the Docker-mounted file system provider. This enables acode to work with files inside Docker containers. It extends the reach of the agent to containerized environments.

Docker mounts present unique challenges. Files are accessed through Docker's mount mechanism. Latency is higher than local. Permissions can differ. The file system may be Linux even on Windows.

Two modes are supported. First, bind mounts where host files are mounted into containers. Second, volume mounts where files live in Docker volumes. Both work through this implementation.

The implementation uses docker exec for file operations. This avoids complex Docker API integration. It works with any Docker setup. Performance is acceptable for typical operations.

Path translation is critical. The host path differs from the container path. The implementation maps between them. Configuration specifies the mount relationship.

Caching improves performance. Directory listings are cached. File existence is cached. Writes invalidate cache. This reduces Docker exec calls.

Error handling maps Docker errors to domain errors. Container not running. Mount not found. Permission denied inside container. Each has clear handling.

The implementation shares behavior with local FS where possible. Reading uses the same encoding detection. Writing uses similar atomic patterns. This ensures consistency.

Security is enforced at the container boundary. Path traversal within container is prevented. Only mounted paths are accessible. Container isolation is respected.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Docker FS | Docker-mounted file system |
| Bind Mount | Host path in container |
| Volume Mount | Docker volume in container |
| docker exec | Execute in container |
| Container Path | Path inside container |
| Host Path | Path on host machine |
| Mount Point | Where mounted |
| Latency | Operation delay |
| Caching | Store for reuse |
| Invalidation | Cache clearing |
| TTL | Time to live |
| Container ID | Container identifier |
| Docker API | Docker interface |
| Shell Escape | Safe command building |
| Exit Code | Command result |

---

## Out of Scope

The following items are explicitly excluded from Task 014.b:

- **Docker API directly** - Uses docker exec
- **Container management** - No start/stop
- **Image operations** - No build/pull
- **Network operations** - Files only
- **Docker Compose** - Single container
- **Kubernetes** - Docker only
- **Remote Docker** - Local daemon only
- **Docker in Docker** - Not supported

---

## Functional Requirements

### Docker Detection

- FR-001: Detect if Docker available
- FR-002: Verify container exists
- FR-003: Verify container running
- FR-004: Verify mount accessible

### File Reading

- FR-005: ReadFileAsync via docker exec
- FR-006: cat command for reading
- FR-007: Base64 for binary
- FR-008: Handle missing file
- FR-009: Handle permission denied

### File Writing

- FR-010: WriteFileAsync via docker exec
- FR-011: Temp file then rename
- FR-012: mkdir -p for parents
- FR-013: Base64 for binary
- FR-014: Handle write errors

### File Deletion

- FR-015: DeleteFileAsync via rm
- FR-016: DeleteDirectoryAsync via rm -rf
- FR-017: Handle missing gracefully
- FR-018: Handle permission errors

### Directory Enumeration

- FR-019: List via find command
- FR-020: Parse find output
- FR-021: Handle large directories
- FR-022: Filter support
- FR-023: Recursive support

### Metadata

- FR-024: ExistsAsync via test
- FR-025: GetMetadataAsync via stat
- FR-026: Size from stat
- FR-027: Modified time from stat
- FR-028: File type detection

### Path Translation

- FR-029: Host to container mapping
- FR-030: Container to host mapping
- FR-031: Configuration of mappings
- FR-032: Multiple mount support

### Caching

- FR-033: Directory listing cache
- FR-034: Existence cache
- FR-035: Cache invalidation on write
- FR-036: TTL expiration
- FR-037: Cache disable option

### Error Handling

- FR-038: Container not found
- FR-039: Container not running
- FR-040: Mount not found
- FR-041: Permission denied
- FR-042: Command timeout

### Security

- FR-043: Shell escape arguments
- FR-044: Path traversal prevention
- FR-045: Boundary enforcement

---

## Non-Functional Requirements

### Performance

- NFR-001: Read < 100ms + docker latency
- NFR-002: Write < 150ms + docker latency
- NFR-003: Cache hit < 5ms
- NFR-004: List < 200ms for 1000 files

### Reliability

- NFR-005: Handle container restarts
- NFR-006: Retry transient errors
- NFR-007: Timeout prevention

### Security

- NFR-008: Safe command building
- NFR-009: No shell injection
- NFR-010: Boundary enforcement

---

## User Manual Documentation

### Overview

Docker FS allows acode to work with files inside Docker containers. Use this when your project runs in containers and you want the agent to modify files there.

### Configuration

```yaml
# .agent/config.yml
repo:
  fs_type: docker
  
  docker:
    # Container name or ID
    container: my-app-container
    
    # Mount mappings (host -> container)
    mounts:
      - host: /home/user/project
        container: /app
        
    # Cache settings
    cache:
      enabled: true
      ttl_seconds: 60
      
    # Timeout for docker exec
    timeout_seconds: 30
```

### Usage Examples

```csharp
// Create Docker file system
var fs = new DockerFileSystem(new DockerFSOptions
{
    ContainerName = "my-app-container",
    Mounts = new[]
    {
        new MountMapping("/home/user/project", "/app")
    }
});

// Read file from container
var content = await fs.ReadFileAsync("src/main.py");

// Write file to container
await fs.WriteFileAsync("config.json", jsonContent);
```

### Path Translation

When you specify paths, use the container paths:

```csharp
// This reads /app/src/main.py inside the container
var content = await fs.ReadFileAsync("src/main.py");
```

The mount configuration translates:
- Root path: `/app` (container)
- Relative `src/main.py` becomes `/app/src/main.py`

### Caching

Docker operations are slow. Caching helps:

```yaml
docker:
  cache:
    enabled: true      # Enable caching
    ttl_seconds: 60    # Cache for 60 seconds
```

Cache is invalidated on writes. You can also manually clear:

```csharp
fs.ClearCache();
```

### Troubleshooting

#### Container Not Found

**Problem:** Cannot connect to container

**Solutions:**
1. Verify container name: `docker ps`
2. Check container is running
3. Verify name matches config

#### Mount Not Accessible

**Problem:** Cannot access mounted path

**Solutions:**
1. Verify mount configuration
2. Check container has mount
3. Verify path inside container

#### Permission Denied

**Problem:** Cannot read/write in container

**Solutions:**
1. Check file permissions in container
2. Verify user running docker exec
3. Consider running as root (for testing)

#### Timeout

**Problem:** Operations take too long

**Solutions:**
1. Increase timeout_seconds
2. Check container load
3. Verify Docker daemon health

---

## Acceptance Criteria

### Detection

- [ ] AC-001: Docker detected
- [ ] AC-002: Container verified
- [ ] AC-003: Mount verified

### Reading

- [ ] AC-004: Read file works
- [ ] AC-005: Read binary works
- [ ] AC-006: Missing handled
- [ ] AC-007: Permission handled

### Writing

- [ ] AC-008: Write file works
- [ ] AC-009: Atomic via rename
- [ ] AC-010: Creates parents
- [ ] AC-011: Binary works

### Enumeration

- [ ] AC-012: List files works
- [ ] AC-013: Recursive works
- [ ] AC-014: Filter works

### Caching

- [ ] AC-015: Cache works
- [ ] AC-016: Invalidation works
- [ ] AC-017: TTL works

### Security

- [ ] AC-018: No injection
- [ ] AC-019: Path validated
- [ ] AC-020: Boundary enforced

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/FileSystem/Docker/
├── DockerFSTests.cs
│   ├── Should_Read_File()
│   ├── Should_Write_File()
│   └── Should_Translate_Paths()
│
├── DockerCommandBuilderTests.cs
│   ├── Should_Escape_Arguments()
│   └── Should_Build_Commands()
│
└── DockerCacheTests.cs
    ├── Should_Cache_Listings()
    └── Should_Invalidate_On_Write()
```

### Integration Tests

```
Tests/Integration/FileSystem/Docker/
├── DockerFSIntegrationTests.cs
│   ├── Should_Work_With_Real_Container()
│   └── Should_Handle_Large_Files()
```

### E2E Tests

```
Tests/E2E/FileSystem/Docker/
├── DockerFSE2ETests.cs
│   └── Should_Work_With_Agent()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Cached read | 5ms | 10ms |
| Uncached read | 100ms | 200ms |
| Write | 150ms | 300ms |
| List 1000 | 200ms | 500ms |

---

## User Verification Steps

### Scenario 1: Read File

1. Start container with mounted files
2. Configure acode for Docker FS
3. Read file
4. Verify: Content correct

### Scenario 2: Write File

1. Configure Docker FS
2. Write file
3. Exec into container, check file
4. Verify: Content matches

### Scenario 3: Cache

1. Read file (slow)
2. Read again (fast)
3. Verify: Cache hit

### Scenario 4: Error Handling

1. Stop container
2. Try to read
3. Verify: Clear error message

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Infrastructure/
├── FileSystem/
│   └── Docker/
│       ├── DockerFileSystem.cs
│       ├── DockerFSOptions.cs
│       ├── DockerCommandBuilder.cs
│       ├── DockerCommandExecutor.cs
│       ├── MountMapping.cs
│       └── DockerFSCache.cs
```

### DockerFileSystem Class

```csharp
namespace AgenticCoder.Infrastructure.FileSystem.Docker;

public sealed class DockerFileSystem : IRepoFS
{
    private readonly DockerCommandExecutor _executor;
    private readonly MountTranslator _mounts;
    private readonly DockerFSCache _cache;
    
    public async Task<string> ReadFileAsync(
        string path,
        CancellationToken ct = default)
    {
        var containerPath = _mounts.ToContainer(path);
        
        var command = $"cat {ShellEscape(containerPath)}";
        var result = await _executor.ExecAsync(command, ct);
        
        if (result.ExitCode != 0)
            throw MapError(result);
            
        return result.Output;
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-DFS-001 | Container not found |
| ACODE-DFS-002 | Container not running |
| ACODE-DFS-003 | File not found |
| ACODE-DFS-004 | Permission denied |
| ACODE-DFS-005 | Command timeout |

### Implementation Checklist

1. [ ] Create DockerFileSystem
2. [ ] Implement command executor
3. [ ] Implement mount translation
4. [ ] Implement reading
5. [ ] Implement writing
6. [ ] Implement caching
7. [ ] Add error handling
8. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Command execution
2. **Phase 2:** Read operations
3. **Phase 3:** Write operations
4. **Phase 4:** Caching
5. **Phase 5:** Error handling

---

**End of Task 014.b Specification**