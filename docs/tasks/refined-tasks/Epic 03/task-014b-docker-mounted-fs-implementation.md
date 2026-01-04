# Task 014.b: Docker-Mounted FS Implementation

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 014 (RepoFS Abstraction), Task 014.a (Local FS)  

---

## Description

### Business Value

The Docker-Mounted File System implementation extends Agentic Coding Bot's reach to containerized development environments, enabling the agent to modify files within Docker containers. As container-based development becomes standard practice, this capability is essential for supporting modern development workflows.

Many development teams run their applications in containers for consistency between development and production environments. Without Docker FS support, developers would need to manually copy files between the host and container, breaking the seamless agent experience. This implementation enables the agent to work directly with containerized codebases, maintaining the same user experience as local development.

The Docker FS implementation addresses the unique challenges of container file access: higher latency than local I/O, potential permission differences, path translation between host and container, and the transient nature of container environments. Caching, intelligent error handling, and clear diagnostics ensure reliable operation despite these challenges.

### Scope

This task delivers the complete Docker-mounted file system implementation:

1. **DockerFileSystem Class:** IRepoFS implementation that executes file operations via `docker exec` commands. Provides the same interface as LocalFS while handling Docker-specific concerns.

2. **Docker Command Executor:** Secure execution of `docker exec` commands with proper shell escaping, timeout handling, and exit code interpretation.

3. **Mount Path Translation:** Bidirectional mapping between host paths and container paths. Supports multiple mount configurations for complex container setups.

4. **Operation Caching:** Reduces Docker exec overhead by caching directory listings and existence checks. Automatic invalidation on write operations with configurable TTL.

5. **Container Health Detection:** Verifies container availability and mount accessibility before operations, providing clear diagnostics when containers are unavailable.

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Task 014 (RepoFS) | Interface Implementation | Implements IRepoFS interface contract |
| Task 014.a (Local FS) | Behavior Sharing | Shares encoding detection and atomic write patterns |
| Task 014.c (Patching) | File Access | Patch applicator uses DockerFS for container-hosted repositories |
| Task 003 (DI) | Dependency Injection | Registered as alternative IRepoFS when Docker mode configured |
| Task 002 (Config) | Configuration | Docker settings from `repo.docker` config section |
| Task 011 (Session) | Session Context | Session determines container and mount configuration |
| Task 003.c (Audit) | Audit Logging | All container operations logged with container ID |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Container not found | All operations fail | Verify container at startup, clear error message |
| Container not running | All operations fail | Health check before operations, suggest `docker start` |
| Mount not accessible | Path operations fail | Validate mount configuration, report mapping issues |
| Permission denied in container | Read/write blocked | Report user context, suggest container permissions |
| Command timeout | Operation blocked | Configurable timeout, suggest container health check |
| Shell injection attempt | Security violation | Strict argument escaping, reject suspicious input |
| High latency | Slow operations | Aggressive caching, batch operations where possible |
| Container restart mid-operation | Transient failure | Retry with exponential backoff, cache invalidation |

### Assumptions

1. Docker daemon is running and accessible on the local machine
2. The target container is running and has the repository mounted
3. The container has `cat`, `find`, `stat`, `rm`, and `mkdir` commands available
4. Docker exec operations complete within configurable timeout
5. Mount paths are correctly configured in the agent configuration
6. The agent process has permission to execute Docker commands
7. File paths inside the container use forward slashes (Linux containers)
8. Container environment remains stable during operation sequences

### Security Considerations

1. **Shell Injection Prevention:** All arguments passed to `docker exec` MUST be properly escaped. Command construction MUST use safe builder patterns.

2. **Container Boundary Enforcement:** Operations MUST be limited to configured mount paths. Path traversal within container MUST be prevented.

3. **Container Name Validation:** Container names/IDs MUST be validated against expected patterns to prevent injection.

4. **Credential Protection:** Docker commands MUST NOT expose sensitive information in process arguments or logs.

5. **Minimum Privilege:** Operations SHOULD use non-root container users where possible. Write access SHOULD be explicit.

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

### Docker Detection (FR-014b-01 to FR-014b-04)

| ID | Requirement |
|----|-------------|
| FR-014b-01 | System MUST detect if Docker daemon is available and accessible |
| FR-014b-02 | System MUST verify specified container exists |
| FR-014b-03 | System MUST verify container is in running state |
| FR-014b-04 | System MUST verify configured mount paths are accessible in container |

### File Reading (FR-014b-05 to FR-014b-09)

| ID | Requirement |
|----|-------------|
| FR-014b-05 | ReadFileAsync MUST execute via `docker exec cat` command |
| FR-014b-06 | Text file reading MUST use `cat` command with proper encoding |
| FR-014b-07 | Binary file reading MUST use base64 encoding for transport |
| FR-014b-08 | ReadFileAsync MUST throw FileNotFoundException for missing files |
| FR-014b-09 | ReadFileAsync MUST throw AccessDeniedException for permission failures |

### File Writing (FR-014b-10 to FR-014b-14)

| ID | Requirement |
|----|-------------|
| FR-014b-10 | WriteFileAsync MUST execute via `docker exec` commands |
| FR-014b-11 | Writes MUST use temp-file-then-rename pattern for atomicity |
| FR-014b-12 | WriteFileAsync MUST create parent directories via `mkdir -p` |
| FR-014b-13 | Binary file writing MUST use base64 encoding for transport |
| FR-014b-14 | Write failures MUST be reported with clear error messages |

### File Deletion (FR-014b-15 to FR-014b-18)

| ID | Requirement |
|----|-------------|
| FR-014b-15 | DeleteFileAsync MUST execute via `docker exec rm` command |
| FR-014b-16 | DeleteDirectoryAsync MUST execute via `docker exec rm -rf` command |
| FR-014b-17 | DeleteFileAsync MUST NOT throw error for non-existent files |
| FR-014b-18 | Delete operations MUST handle permission errors gracefully |

### Directory Enumeration (FR-014b-19 to FR-014b-23)

| ID | Requirement |
|----|-------------|
| FR-014b-19 | EnumerateFilesAsync MUST use `find` command for listing |
| FR-014b-20 | System MUST parse `find` command output correctly |
| FR-014b-21 | Enumeration MUST handle large directories efficiently |
| FR-014b-22 | EnumerateFilesAsync MUST support glob pattern filtering |
| FR-014b-23 | EnumerateFilesAsync MUST support recursive option |

### Metadata (FR-014b-24 to FR-014b-28)

| ID | Requirement |
|----|-------------|
| FR-014b-24 | ExistsAsync MUST use `test` command for checking |
| FR-014b-25 | GetMetadataAsync MUST use `stat` command for details |
| FR-014b-26 | Metadata MUST include file size parsed from stat output |
| FR-014b-27 | Metadata MUST include modified timestamp parsed from stat output |
| FR-014b-28 | Metadata MUST correctly identify file vs directory type |

### Path Translation (FR-014b-29 to FR-014b-32)

| ID | Requirement |
|----|-------------|
| FR-014b-29 | System MUST translate host paths to container paths |
| FR-014b-30 | System MUST translate container paths to host paths |
| FR-014b-31 | Mount mappings MUST be configurable via configuration |
| FR-014b-32 | System MUST support multiple mount point mappings |

### Caching (FR-014b-33 to FR-014b-37)

| ID | Requirement |
|----|-------------|
| FR-014b-33 | Directory listings MUST be cached to reduce Docker exec calls |
| FR-014b-34 | File existence checks MUST be cached |
| FR-014b-35 | Cache MUST be invalidated on write or delete operations |
| FR-014b-36 | Cache MUST support TTL-based expiration |
| FR-014b-37 | Cache MUST support manual disable option |

### Error Handling (FR-014b-38 to FR-014b-42)

| ID | Requirement |
|----|-------------|
| FR-014b-38 | ContainerNotFoundException MUST be thrown when container not found |
| FR-014b-39 | ContainerNotRunningException MUST be thrown when container stopped |
| FR-014b-40 | MountNotFoundException MUST be thrown for invalid mount paths |
| FR-014b-41 | AccessDeniedException MUST be thrown for permission failures |
| FR-014b-42 | TimeoutException MUST be thrown when command exceeds timeout |

### Security (FR-014b-43 to FR-014b-45)

| ID | Requirement |
|----|-------------|
| FR-014b-43 | All command arguments MUST be properly shell-escaped |
| FR-014b-44 | Path traversal within container MUST be prevented |
| FR-014b-45 | Operations MUST be restricted to configured mount boundaries |

---

## Non-Functional Requirements

### Performance (NFR-014b-01 to NFR-014b-04)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014b-01 | Performance | File read MUST complete in < 100ms plus Docker latency |
| NFR-014b-02 | Performance | File write MUST complete in < 150ms plus Docker latency |
| NFR-014b-03 | Performance | Cache hit operations MUST complete in < 5ms |
| NFR-014b-04 | Performance | Directory listing of 1000 files MUST complete in < 200ms |

### Reliability (NFR-014b-05 to NFR-014b-07)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014b-05 | Reliability | System MUST handle container restarts gracefully |
| NFR-014b-06 | Reliability | Transient Docker errors MUST trigger automatic retry |
| NFR-014b-07 | Reliability | Operations MUST timeout to prevent indefinite blocking |

### Security (NFR-014b-08 to NFR-014b-10)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014b-08 | Security | Command building MUST use safe escaping patterns |
| NFR-014b-09 | Security | Shell injection attacks MUST be prevented |
| NFR-014b-10 | Security | Mount boundary MUST be enforced on all operations |

### Observability (NFR-014b-11 to NFR-014b-13)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014b-11 | Observability | All Docker exec commands MUST be logged |
| NFR-014b-12 | Observability | Cache hit/miss ratios MUST be trackable |
| NFR-014b-13 | Observability | Command latency MUST be measurable for diagnostics |

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

## Best Practices

### Container Integration

1. **Validate mount points** - Verify expected paths exist in container at startup
2. **Handle permission differences** - Container UID/GID may differ from host
3. **Monitor mount health** - Detect when bind mounts become unavailable
4. **Use absolute paths in container** - Avoid relative path confusion

### Performance Considerations

5. **Minimize cross-boundary I/O** - Batch operations to reduce overhead
6. **Cache file metadata** - Reduce stat calls across container boundary
7. **Use appropriate timeout** - Container I/O may be slower than native
8. **Consider async polling** - FileSystemWatcher may not work across mounts

### Security

9. **Enforce read-only when possible** - Mount volumes as ro: when writes not needed
10. **Validate all paths** - Double-check paths don't escape mount point
11. **No shell injection** - Never build shell commands from file paths
12. **Principle of least privilege** - Request only necessary container capabilities

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/FileSystem/Docker/
├── DockerFSReadTests.cs
│   ├── Should_Read_Text_File()
│   ├── Should_Read_Binary_File()
│   ├── Should_Read_Large_File()
│   ├── Should_Handle_Missing_File()
│   ├── Should_Handle_Permission_Denied()
│   ├── Should_Handle_Container_Not_Running()
│   └── Should_Support_Cancellation()
│
├── DockerFSWriteTests.cs
│   ├── Should_Write_Text_File()
│   ├── Should_Write_Binary_File()
│   ├── Should_Write_Atomically()
│   ├── Should_Create_Parent_Directories()
│   ├── Should_Overwrite_Existing()
│   ├── Should_Handle_Write_Errors()
│   └── Should_Invalidate_Cache_On_Write()
│
├── DockerFSDeleteTests.cs
│   ├── Should_Delete_File()
│   ├── Should_Delete_Directory()
│   ├── Should_Delete_Recursive()
│   ├── Should_Handle_Missing_Gracefully()
│   └── Should_Handle_Permission_Denied()
│
├── DockerFSEnumerationTests.cs
│   ├── Should_List_Files()
│   ├── Should_List_Recursively()
│   ├── Should_Apply_Filter()
│   ├── Should_Handle_Large_Directory()
│   └── Should_Parse_Find_Output()
│
├── DockerFSMetadataTests.cs
│   ├── Should_Check_Exists()
│   ├── Should_Get_File_Size()
│   ├── Should_Get_Modified_Time()
│   ├── Should_Detect_File_Type()
│   └── Should_Parse_Stat_Output()
│
├── DockerCommandBuilderTests.cs
│   ├── Should_Escape_Single_Quotes()
│   ├── Should_Escape_Double_Quotes()
│   ├── Should_Escape_Spaces()
│   ├── Should_Escape_Special_Characters()
│   ├── Should_Escape_Newlines()
│   ├── Should_Build_Cat_Command()
│   ├── Should_Build_Find_Command()
│   ├── Should_Build_Stat_Command()
│   ├── Should_Build_Mkdir_Command()
│   └── Should_Build_Rm_Command()
│
├── DockerCommandExecutorTests.cs
│   ├── Should_Execute_Simple_Command()
│   ├── Should_Handle_Exit_Code_Zero()
│   ├── Should_Handle_Exit_Code_NonZero()
│   ├── Should_Handle_Timeout()
│   ├── Should_Handle_Large_Output()
│   └── Should_Support_Cancellation()
│
├── MountMappingTests.cs
│   ├── Should_Translate_Host_To_Container()
│   ├── Should_Translate_Container_To_Host()
│   ├── Should_Handle_Multiple_Mounts()
│   ├── Should_Find_Best_Mount_Match()
│   └── Should_Handle_Unmapped_Path()
│
├── DockerCacheTests.cs
│   ├── Should_Cache_Directory_Listing()
│   ├── Should_Cache_Existence_Check()
│   ├── Should_Return_Cached_Value()
│   ├── Should_Invalidate_On_Write()
│   ├── Should_Invalidate_On_Delete()
│   ├── Should_Expire_After_TTL()
│   ├── Should_Clear_All_Cache()
│   └── Should_Disable_Cache()
│
└── DockerSecurityTests.cs
    ├── Should_Prevent_Shell_Injection()
    ├── Should_Block_Path_Traversal()
    ├── Should_Enforce_Mount_Boundary()
    └── Should_Reject_Invalid_Container_Name()
```

### Integration Tests

```
Tests/Integration/FileSystem/Docker/
├── DockerFSIntegrationTests.cs
│   ├── Should_Work_With_Real_Container()
│   ├── Should_Handle_Large_Files()
│   ├── Should_Handle_Many_Small_Files()
│   ├── Should_Handle_Concurrent_Operations()
│   ├── Should_Survive_Container_Restart()
│   └── Should_Handle_Slow_Container()
│
└── DockerMountIntegrationTests.cs
    ├── Should_Work_With_Bind_Mount()
    ├── Should_Work_With_Volume_Mount()
    └── Should_Handle_Multiple_Mounts()
```

### E2E Tests

```
Tests/E2E/FileSystem/Docker/
├── DockerFSE2ETests.cs
│   ├── Should_Read_File_Via_Agent_Tool()
│   ├── Should_Write_File_Via_Agent_Tool()
│   ├── Should_List_Files_Via_Agent_Tool()
│   └── Should_Work_With_Containerized_Project()
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