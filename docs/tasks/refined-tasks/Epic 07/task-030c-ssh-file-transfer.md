# Task 030.c: SSH File Transfer (SFTP)

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 7 – Cloud Integration  
**Dependencies:** Task 030 (SSH Target), Task 030.a (Connection)  

---

## Description

Task 030.c implements file transfer over SSH. SFTP MUST be used. Files MUST transfer reliably. Progress MUST be reported.

This extends Task 029.c (artifacts) for SSH specifics. SFTP provides reliable file operations. SCP is an alternative fallback.

Large files MUST stream. Directories MUST transfer recursively. Permissions MUST be preserved.

### Business Value

SFTP file transfer enables:
- Deploy code to remote
- Retrieve build artifacts
- Sync working directories
- Transfer large datasets

### Scope Boundaries

This task covers SFTP operations. Connection management is in 030.a. Command execution is in 030.b.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Task 029.c ITransferArtifacts | Implements interface | Files in/out | Core abstraction |
| Task 030.a Connection Pool | ISshConnectionPool | SFTP channels from pool | Reuses connections |
| Task 027 Worker Orchestration | IWorker.Upload/Download | Worker triggers transfers | Primary consumer |
| SSH.NET SFTP | SftpClient | SFTP protocol ops | Transport layer |
| Local File System | System.IO | Local read/write | Source/dest |
| Progress Reporting | IProgress<TransferProgress> | Real-time progress | UI feedback |
| Checksum Service | sha256sum (remote) | Verification hashes | Integrity check |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Transfer timeout | Timer expiry | Retry with resume | Delayed completion |
| Remote disk full | SFTP error code | Report error, cleanup temp | Transfer fails |
| Permission denied | SFTP error code | Report with path details | Auth issue |
| Connection lost | Channel disconnect | Resume from last offset | Minimal re-transfer |
| Checksum mismatch | Hash comparison | Delete and retry | Retry required |
| SFTP not available | Subsystem failure | Fall back to SCP | Degraded mode |
| Temp file orphan | Cleanup check | Delete on startup | Disk usage |
| Concurrent access | File lock error | Queue or fail | Serialization |

---

## Assumptions

1. **SSH.NET Library**: Implementation uses SSH.NET (Renci) SFTP subsystem
2. **SFTP Availability**: Most SSH servers have SFTP subsystem enabled
3. **Remote Shell**: sha256sum available for checksum verification
4. **Encoding**: UTF-8 for file paths, binary for content
5. **Temp Space**: Remote has sufficient temp space for atomic writes
6. **Permissions**: User has write permission in target directories
7. **File Sizes**: Support files from bytes to 10GB
8. **Network**: Variable quality network (resume support critical)

---

## Security Considerations

1. **Path Traversal Prevention**: Remote paths MUST be validated against traversal attacks
2. **Symlink Safety**: Symlink following MUST be configurable (off by default)
3. **Permission Preservation**: Don't escalate permissions beyond source
4. **Temp File Permissions**: Temp files MUST have restricted permissions
5. **Credential Isolation**: SFTP credentials from connection, never in file ops
6. **Audit Trail**: All transfers MUST be logged with source/dest
7. **Checksum Logging**: Verification results MUST be logged
8. **Size Limits**: Max transfer size MUST be configurable to prevent abuse

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| SFTP | SSH File Transfer Protocol |
| SCP | Secure Copy (fallback) |
| Resume | Continue partial transfer |
| Chunk | Transfer segment |
| Stat | File metadata |
| Glob | Pattern matching |

---

## Out of Scope

- Direct S3 transfers
- rsync integration
- Delta transfers
- Compression at transport level
- Encryption beyond SSH

---

## Functional Requirements

### SFTP Channel Operations

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-030C-01 | SFTP subsystem MUST be used via SSH.NET `SftpClient` | P0 |
| FR-030C-02 | SFTP channel MUST be acquired from connection pool | P0 |
| FR-030C-03 | Multiple concurrent SFTP channels MUST work | P0 |
| FR-030C-04 | Channel reuse MUST work for sequential operations | P0 |
| FR-030C-05 | `OpenSftpAsync()` MUST return `ISftpChannel` wrapper | P0 |
| FR-030C-06 | Channel MUST implement `IAsyncDisposable` | P0 |
| FR-030C-07 | `StatAsync` MUST return file metadata | P0 |
| FR-030C-08 | Stat MUST return size, modified time, permissions | P0 |
| FR-030C-09 | `ExistsAsync` MUST check file/directory existence | P0 |
| FR-030C-10 | `IsDirectoryAsync` MUST detect directory type | P1 |
| FR-030C-11 | `IsFileAsync` MUST detect regular file type | P1 |
| FR-030C-12 | `ListDirectoryAsync` MUST enumerate entries | P0 |
| FR-030C-13 | Recursive listing MUST work via flag | P1 |
| FR-030C-14 | `CreateDirectoryAsync` MUST create single directory | P0 |
| FR-030C-15 | CreateDirectory MUST support recursive creation | P0 |
| FR-030C-16 | `DeleteAsync` MUST remove file or empty directory | P0 |
| FR-030C-17 | `RenameAsync` MUST move/rename files | P0 |
| FR-030C-18 | `ChmodAsync` MUST set permissions | P1 |
| FR-030C-19 | `ChownAsync` MUST set owner (if permitted) | P2 |
| FR-030C-20 | `ReadLinkAsync` MUST resolve symbolic links | P2 |

### Upload Operations

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-030C-21 | `UploadAsync` MUST transfer files via SFTP | P0 |
| FR-030C-22 | Single file upload MUST work | P0 |
| FR-030C-23 | Directory upload MUST work | P0 |
| FR-030C-24 | Recursive directory upload MUST work | P0 |
| FR-030C-25 | Glob patterns MUST be supported for source selection | P1 |
| FR-030C-26 | Streaming upload MUST avoid loading entire file in memory | P0 |
| FR-030C-27 | Chunk size MUST be configurable (default 64KB) | P1 |
| FR-030C-28 | Progress callback MUST be invocable via `IProgress<T>` | P0 |
| FR-030C-29 | Progress MUST include bytes transferred, percent, rate | P0 |
| FR-030C-30 | Upload timeout MUST be configurable | P0 |
| FR-030C-31 | Resume MUST be supported for interrupted transfers | P0 |
| FR-030C-32 | Resume MUST check existing remote file size | P0 |
| FR-030C-33 | Resume MUST append remainder only | P0 |
| FR-030C-34 | Overwrite behavior MUST be configurable (default: overwrite) | P0 |
| FR-030C-35 | Preserve timestamps MUST be supported | P1 |
| FR-030C-36 | Preserve permissions MUST be supported | P1 |
| FR-030C-37 | Preserve owner MUST be optional (off by default) | P2 |
| FR-030C-38 | Create parent directories MUST be automatic | P0 |
| FR-030C-39 | Temp file MUST be used during transfer | P0 |
| FR-030C-40 | Atomic rename on completion MUST be performed | P0 |
| FR-030C-41 | Temp file cleanup MUST occur on failure | P0 |
| FR-030C-42 | Checksum verification MUST be optional | P1 |
| FR-030C-43 | Post-upload stat MUST verify file size | P1 |

### Download Operations

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-030C-44 | `DownloadAsync` MUST transfer files via SFTP | P0 |
| FR-030C-45 | Single file download MUST work | P0 |
| FR-030C-46 | Directory download MUST work | P0 |
| FR-030C-47 | Recursive directory download MUST work | P0 |
| FR-030C-48 | Glob patterns MUST be supported for source selection | P1 |
| FR-030C-49 | Streaming download MUST avoid memory issues | P0 |
| FR-030C-50 | Progress callback MUST report download progress | P0 |
| FR-030C-51 | Download timeout MUST be configurable | P0 |
| FR-030C-52 | Resume MUST be supported for interrupted downloads | P0 |
| FR-030C-53 | Resume MUST check local file size and seek remote | P0 |
| FR-030C-54 | Overwrite behavior MUST be configurable | P0 |
| FR-030C-55 | Preserve timestamps MUST be supported | P1 |
| FR-030C-56 | Preserve permissions MUST be supported | P1 |
| FR-030C-57 | Create parent directories MUST be automatic | P0 |
| FR-030C-58 | Temp file MUST be used during download | P0 |
| FR-030C-59 | Atomic move on completion MUST be performed | P0 |
| FR-030C-60 | Temp file cleanup MUST occur on failure | P0 |
| FR-030C-61 | Checksum verification MUST use remote sha256sum | P1 |
| FR-030C-62 | Fallback to no-verify if sha256sum unavailable | P1 |
| FR-030C-63 | Batch download MUST be supported | P1 |
| FR-030C-64 | Parallel downloads MUST be supported | P1 |
| FR-030C-65 | Parallelism MUST be configurable (default 4) | P1 |

### SCP Fallback

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-030C-66 | SCP MUST be available as fallback | P1 |
| FR-030C-67 | SCP MUST be used when SFTP subsystem unavailable | P1 |
| FR-030C-68 | SCP MUST use exec channel with scp command | P1 |
| FR-030C-69 | SCP single file transfer MUST work | P1 |
| FR-030C-70 | SCP directory transfer MUST work | P1 |
| FR-030C-71 | SCP recursive transfer MUST work | P1 |
| FR-030C-72 | SCP MUST preserve permissions with -p flag | P2 |
| FR-030C-73 | SCP progress MUST be reported | P2 |
| FR-030C-74 | SCP timeout MUST be enforced | P1 |
| FR-030C-75 | Auto-detection of SFTP availability MUST work | P1 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-030C-01 | Large file transfer throughput (1GB) | <5 minutes on 100Mbps | P0 |
| NFR-030C-02 | Memory usage during transfer | Bounded to chunk size | P0 |
| NFR-030C-03 | Parallel transfer capacity | 10 simultaneous | P0 |
| NFR-030C-04 | Progress update frequency | Every 1 second | P1 |
| NFR-030C-05 | Resume efficiency (bandwidth saved) | Only transfers remainder | P0 |
| NFR-030C-06 | Small file latency (single file <1KB) | <100ms | P1 |
| NFR-030C-07 | Directory listing latency (1000 files) | <2s | P1 |
| NFR-030C-08 | Channel acquisition time | <50ms | P1 |
| NFR-030C-09 | Checksum verification time (1GB) | <30s | P2 |
| NFR-030C-10 | Batch operation throughput | 100 files/second | P2 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-030C-11 | Data corruption prevention | Zero corruption | P0 |
| NFR-030C-12 | Atomic file placement (no partial files) | 100% atomic | P0 |
| NFR-030C-13 | Network interruption recovery | Resume from last offset | P0 |
| NFR-030C-14 | Temp file cleanup on failure | 100% cleanup | P0 |
| NFR-030C-15 | Thread safety for concurrent transfers | Zero race conditions | P0 |
| NFR-030C-16 | Checksum match rate | 100% when verified | P0 |
| NFR-030C-17 | SCP fallback success rate | 95% when SFTP fails | P1 |
| NFR-030C-18 | Permission preservation accuracy | Exact match | P1 |
| NFR-030C-19 | Timestamp preservation accuracy | ±1 second | P2 |
| NFR-030C-20 | Symlink handling safety | Configurable follow/skip | P1 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-030C-21 | Structured logging for all transfers | JSON with correlation ID | P0 |
| NFR-030C-22 | Metrics for transfer count/size | Per-operation counters | P1 |
| NFR-030C-23 | Metrics for transfer duration | Histogram buckets | P1 |
| NFR-030C-24 | Metrics for failure rate | Counter by error type | P1 |
| NFR-030C-25 | Metrics for resume usage | Resume vs full transfers | P2 |
| NFR-030C-26 | Progress observable via IProgress<T> | Real-time updates | P0 |
| NFR-030C-27 | Transfer completion events | Event stream | P1 |
| NFR-030C-28 | Bandwidth utilization metrics | Bytes/second | P2 |
| NFR-030C-29 | Error categorization | Typed exception codes | P1 |
| NFR-030C-30 | Checksum verification logging | Success/failure logged | P1 |

---

## User Manual Documentation

### Example Usage

```csharp
// Open SFTP channel
using var sftp = connection.OpenSftp();

// List remote directory
var files = await sftp.ListDirectoryAsync("/app");

// Upload file
await sftp.UploadAsync(
    "/local/build.zip",
    "/remote/build.zip",
    progress: p => Console.WriteLine($"{p.Percent}%"));

// Download directory
await sftp.DownloadAsync(
    "/remote/output/",
    "/local/output/",
    new DownloadOptions { Recursive = true });

// Check file exists
if (await sftp.ExistsAsync("/remote/file.txt"))
{
    var stat = await sftp.StatAsync("/remote/file.txt");
    Console.WriteLine($"Size: {stat.Size}");
}
```

### Transfer Configuration

```yaml
sftp:
  chunkSizeBytes: 65536
  parallelTransfers: 4
  preserveTimestamps: true
  preservePermissions: true
  useAtomicRename: true
  verifyChecksum: true
```

### Troubleshooting

| Issue | Resolution |
|-------|------------|
| SFTP not available | Check subsystem enabled |
| Permission denied | Check remote permissions |
| Disk full | Free space on remote |
| Timeout | Increase limit or chunk |

---

## Acceptance Criteria / Definition of Done

### SFTP Channel Operations
- [ ] AC-001: SFTP channel opens successfully from pool
- [ ] AC-002: Multiple concurrent channels work
- [ ] AC-003: Channel reuse works for sequential operations
- [ ] AC-004: `StatAsync` returns correct file metadata
- [ ] AC-005: `ExistsAsync` correctly detects existence
- [ ] AC-006: `ListDirectoryAsync` enumerates all entries
- [ ] AC-007: Recursive listing works
- [ ] AC-008: `CreateDirectoryAsync` creates directories
- [ ] AC-009: Recursive directory creation works
- [ ] AC-010: `DeleteAsync` removes files and directories

### Upload Operations
- [ ] AC-011: Single file upload works
- [ ] AC-012: Directory upload works
- [ ] AC-013: Recursive directory upload works
- [ ] AC-014: Glob pattern selection works
- [ ] AC-015: Streaming upload doesn't exhaust memory
- [ ] AC-016: Chunk size is configurable
- [ ] AC-017: Progress callback reports correctly
- [ ] AC-018: Progress includes bytes, percent, rate
- [ ] AC-019: Timeout is enforced
- [ ] AC-020: Resume works for interrupted upload
- [ ] AC-021: Resume appends only remainder
- [ ] AC-022: Overwrite behavior is configurable
- [ ] AC-023: Timestamps are preserved when enabled
- [ ] AC-024: Permissions are preserved when enabled
- [ ] AC-025: Parent directories created automatically
- [ ] AC-026: Temp file used during transfer
- [ ] AC-027: Atomic rename on completion
- [ ] AC-028: Temp file cleaned up on failure
- [ ] AC-029: Checksum verification works

### Download Operations
- [ ] AC-030: Single file download works
- [ ] AC-031: Directory download works
- [ ] AC-032: Recursive directory download works
- [ ] AC-033: Glob pattern selection works
- [ ] AC-034: Streaming download doesn't exhaust memory
- [ ] AC-035: Progress callback reports correctly
- [ ] AC-036: Timeout is enforced
- [ ] AC-037: Resume works for interrupted download
- [ ] AC-038: Resume seeks to correct offset
- [ ] AC-039: Timestamps are preserved
- [ ] AC-040: Permissions are preserved
- [ ] AC-041: Temp file used during download
- [ ] AC-042: Atomic move on completion
- [ ] AC-043: Checksum verification uses sha256sum
- [ ] AC-044: Fallback to no-verify when sha256sum unavailable
- [ ] AC-045: Batch download works
- [ ] AC-046: Parallel downloads work
- [ ] AC-047: Parallelism is configurable

### SCP Fallback
- [ ] AC-048: SCP detected when SFTP unavailable
- [ ] AC-049: SCP single file transfer works
- [ ] AC-050: SCP directory transfer works
- [ ] AC-051: SCP recursive transfer works
- [ ] AC-052: SCP preserves permissions
- [ ] AC-053: SCP progress is reported
- [ ] AC-054: SCP timeout is enforced

### Reliability and Performance
- [ ] AC-055: 1GB file transfers in <5 minutes
- [ ] AC-056: Memory bounded during large transfer
- [ ] AC-057: 10 parallel transfers work
- [ ] AC-058: No data corruption in any transfer
- [ ] AC-059: Atomic file placement (no partial files visible)
- [ ] AC-060: Thread-safe under concurrent operations

---

## User Verification Scenarios

### Scenario 1: Developer Uploads Build Artifacts
**Persona:** Developer deploying code to remote server  
**Preconditions:** SSH target connected, local build completed  
**Steps:**
1. Upload build.zip (500MB) to remote
2. Observe progress reporting
3. Verify checksum on completion
4. Check file permissions

**Verification Checklist:**
- [ ] Progress shows bytes, percent, rate
- [ ] Transfer completes in reasonable time
- [ ] Checksum matches
- [ ] Permissions preserved
- [ ] No temp files left behind

### Scenario 2: CI Downloads Test Results
**Persona:** CI system retrieving artifacts  
**Preconditions:** Tests completed on remote  
**Steps:**
1. Download test-results/ directory recursively
2. Verify all files received
3. Check timestamps preserved
4. Process results locally

**Verification Checklist:**
- [ ] All files downloaded
- [ ] Directory structure preserved
- [ ] Timestamps match remote (±1s)
- [ ] No corruption in files

### Scenario 3: Resume After Network Failure
**Persona:** Developer on unstable connection  
**Preconditions:** Large file (2GB) transfer in progress  
**Steps:**
1. Start upload of large file
2. Simulate network disconnect at 50%
3. Reconnect and resume
4. Verify completed file

**Verification Checklist:**
- [ ] Resume detects existing partial file
- [ ] Only remaining 50% transferred
- [ ] Final file checksum correct
- [ ] No duplicate data transferred

### Scenario 4: Parallel Directory Sync
**Persona:** DevOps syncing multiple directories  
**Preconditions:** 4 parallel transfer capacity  
**Steps:**
1. Download 100 files from remote
2. Verify parallel transfers used
3. Check all files complete
4. Verify no corruption

**Verification Checklist:**
- [ ] Multiple transfers run simultaneously
- [ ] All 100 files downloaded
- [ ] No file corruption
- [ ] Total time < sequential time

### Scenario 5: SCP Fallback on Legacy Server
**Persona:** Developer connecting to old SSH server  
**Preconditions:** Server has SFTP disabled  
**Steps:**
1. Attempt upload via SFTP
2. Observe fallback to SCP
3. Verify transfer completes
4. Check file integrity

**Verification Checklist:**
- [ ] SFTP unavailability detected
- [ ] SCP fallback activated
- [ ] Transfer completes successfully
- [ ] Log shows fallback reason

### Scenario 6: Large Directory with Permissions
**Persona:** Admin migrating application  
**Preconditions:** Source directory with 1000 files  
**Steps:**
1. Upload directory with permissions preserved
2. Verify all files transferred
3. Check permissions match source
4. Verify executable flags preserved

**Verification Checklist:**
- [ ] All 1000 files uploaded
- [ ] Permissions match source exactly
- [ ] Executable bits preserved
- [ ] Timestamps preserved

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-030C-01 | Path validation rejects traversal | Security |
| UT-030C-02 | Progress calculation is accurate | FR-030C-29 |
| UT-030C-03 | Resume offset calculation | FR-030C-32 |
| UT-030C-04 | Checksum comparison logic | FR-030C-42 |
| UT-030C-05 | Glob pattern matching | FR-030C-25 |
| UT-030C-06 | Chunk size configuration | FR-030C-27 |
| UT-030C-07 | Temp file naming | FR-030C-39 |
| UT-030C-08 | Atomic rename logic | FR-030C-40 |
| UT-030C-09 | Permission preservation mapping | FR-030C-36 |
| UT-030C-10 | Timestamp preservation mapping | FR-030C-35 |
| UT-030C-11 | SCP command building | FR-030C-68 |
| UT-030C-12 | SFTP availability detection | FR-030C-75 |
| UT-030C-13 | Parallel transfer semaphore | FR-030C-65 |
| UT-030C-14 | Error code mapping | NFR-030C-29 |
| UT-030C-15 | Symlink handling configuration | NFR-030C-20 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-030C-01 | Real SFTP single file upload | FR-030C-22 |
| IT-030C-02 | Real SFTP single file download | FR-030C-45 |
| IT-030C-03 | Large file transfer (1GB) | NFR-030C-01 |
| IT-030C-04 | Directory recursive upload | FR-030C-24 |
| IT-030C-05 | Directory recursive download | FR-030C-47 |
| IT-030C-06 | Resume after interrupt (upload) | FR-030C-31 |
| IT-030C-07 | Resume after interrupt (download) | FR-030C-52 |
| IT-030C-08 | Parallel transfers (4 concurrent) | NFR-030C-03 |
| IT-030C-09 | Checksum verification E2E | FR-030C-61 |
| IT-030C-10 | Permission preservation E2E | NFR-030C-18 |
| IT-030C-11 | SCP fallback when SFTP disabled | FR-030C-67 |
| IT-030C-12 | Atomic rename visibility | NFR-030C-12 |
| IT-030C-13 | Memory bounded during large transfer | NFR-030C-02 |
| IT-030C-14 | 100 file batch operation | NFR-030C-10 |
| IT-030C-15 | Progress reporting accuracy | FR-030C-29 |

---

## Implementation Prompt

### Part 1: File Structure and Domain Models

**Target Directory Structure:**
```
src/
├── Acode.Domain/
│   └── Compute/
│       └── Ssh/
│           └── FileTransfer/
│               ├── SftpFileInfo.cs
│               ├── TransferProgress.cs
│               ├── TransferResult.cs
│               ├── TransferError.cs
│               └── Events/
│                   ├── TransferStartedEvent.cs
│                   ├── TransferProgressEvent.cs
│                   ├── TransferCompletedEvent.cs
│                   └── TransferResumedEvent.cs
├── Acode.Application/
│   └── Compute/
│       └── Ssh/
│           └── FileTransfer/
│               ├── ISftpChannel.cs
│               ├── UploadOptions.cs
│               ├── DownloadOptions.cs
│               ├── ITransferResumeManager.cs
│               └── IChecksumVerifier.cs
└── Acode.Infrastructure/
    └── Compute/
        └── Ssh/
            └── FileTransfer/
                ├── SftpChannelWrapper.cs
                ├── SftpUploader.cs
                ├── SftpDownloader.cs
                ├── TransferResumeManager.cs
                ├── ChecksumVerifier.cs
                ├── ScpFallback.cs
                └── DirectoryTransferHandler.cs
```

**Domain Models:**

```csharp
// src/Acode.Domain/Compute/Ssh/FileTransfer/SftpFileInfo.cs
namespace Acode.Domain.Compute.Ssh.FileTransfer;

public sealed record SftpFileInfo
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public long Size { get; init; }
    public bool IsDirectory { get; init; }
    public bool IsSymlink { get; init; }
    public DateTimeOffset Modified { get; init; }
    public DateTimeOffset? Accessed { get; init; }
    public uint Permissions { get; init; }
    public uint UserId { get; init; }
    public uint GroupId { get; init; }
    public string? LinkTarget { get; init; }
}

// src/Acode.Domain/Compute/Ssh/FileTransfer/TransferProgress.cs
namespace Acode.Domain.Compute.Ssh.FileTransfer;

public sealed record TransferProgress
{
    public required string FilePath { get; init; }
    public long BytesTransferred { get; init; }
    public long TotalBytes { get; init; }
    public double Percent => TotalBytes > 0 ? (double)BytesTransferred / TotalBytes * 100 : 0;
    public double BytesPerSecond { get; init; }
    public TimeSpan Elapsed { get; init; }
    public TimeSpan? EstimatedRemaining => BytesPerSecond > 0 
        ? TimeSpan.FromSeconds((TotalBytes - BytesTransferred) / BytesPerSecond) 
        : null;
    public int CurrentFile { get; init; }
    public int TotalFiles { get; init; }
}

// src/Acode.Domain/Compute/Ssh/FileTransfer/TransferResult.cs
namespace Acode.Domain.Compute.Ssh.FileTransfer;

public sealed record TransferResult
{
    public bool Success { get; init; }
    public long BytesTransferred { get; init; }
    public int FilesTransferred { get; init; }
    public TimeSpan Duration { get; init; }
    public IReadOnlyList<TransferredFile> Files { get; init; } = [];
    public IReadOnlyList<TransferError> Errors { get; init; } = [];
    public bool WasResumed { get; init; }
    public bool ChecksumVerified { get; init; }
}

public sealed record TransferredFile(
    string LocalPath,
    string RemotePath,
    long Size,
    string? Checksum);

// src/Acode.Domain/Compute/Ssh/FileTransfer/TransferError.cs
namespace Acode.Domain.Compute.Ssh.FileTransfer;

public sealed record TransferError(
    string Path,
    string ErrorMessage,
    TransferErrorType Type,
    Exception? Exception = null);

public enum TransferErrorType
{
    PermissionDenied,
    FileNotFound,
    DiskFull,
    Timeout,
    ConnectionLost,
    ChecksumMismatch,
    Unknown
}

// src/Acode.Domain/Compute/Ssh/FileTransfer/Events/TransferStartedEvent.cs
namespace Acode.Domain.Compute.Ssh.FileTransfer.Events;

public sealed record TransferStartedEvent(
    string TransferId,
    string ConnectionId,
    TransferDirection Direction,
    string SourcePath,
    string DestinationPath,
    long TotalBytes,
    int TotalFiles,
    DateTimeOffset StartedAt);

public enum TransferDirection { Upload, Download }

// src/Acode.Domain/Compute/Ssh/FileTransfer/Events/TransferCompletedEvent.cs
namespace Acode.Domain.Compute.Ssh.FileTransfer.Events;

public sealed record TransferCompletedEvent(
    string TransferId,
    bool Success,
    long BytesTransferred,
    int FilesTransferred,
    TimeSpan Duration,
    DateTimeOffset CompletedAt);

// src/Acode.Domain/Compute/Ssh/FileTransfer/Events/TransferResumedEvent.cs
namespace Acode.Domain.Compute.Ssh.FileTransfer.Events;

public sealed record TransferResumedEvent(
    string TransferId,
    string Path,
    long ResumedFromByte,
    long TotalBytes,
    DateTimeOffset ResumedAt);
```

**End of Task 030.c Specification - Part 1/4**

### Part 2: Application Interfaces

```csharp
// src/Acode.Application/Compute/Ssh/FileTransfer/ISftpChannel.cs
namespace Acode.Application.Compute.Ssh.FileTransfer;

public interface ISftpChannel : IDisposable
{
    bool IsConnected { get; }
    
    // File operations
    Task<SftpFileInfo> StatAsync(string path, CancellationToken ct = default);
    Task<bool> ExistsAsync(string path, CancellationToken ct = default);
    Task<bool> IsDirectoryAsync(string path, CancellationToken ct = default);
    Task<bool> IsFileAsync(string path, CancellationToken ct = default);
    Task<string?> ReadLinkAsync(string path, CancellationToken ct = default);
    
    // Directory operations
    Task<IReadOnlyList<SftpFileInfo>> ListDirectoryAsync(
        string path, 
        bool recursive = false,
        CancellationToken ct = default);
    
    Task CreateDirectoryAsync(
        string path, 
        bool recursive = true, 
        uint permissions = 0755,
        CancellationToken ct = default);
    
    Task DeleteAsync(
        string path, 
        bool recursive = false, 
        CancellationToken ct = default);
    
    Task RenameAsync(
        string oldPath, 
        string newPath, 
        bool overwrite = false,
        CancellationToken ct = default);
    
    // Permission operations
    Task ChmodAsync(string path, uint permissions, CancellationToken ct = default);
    Task ChownAsync(string path, uint userId, uint groupId, CancellationToken ct = default);
    
    // Transfer operations
    Task<TransferResult> UploadAsync(
        string localPath, 
        string remotePath,
        UploadOptions? options = null, 
        IProgress<TransferProgress>? progress = null,
        CancellationToken ct = default);
    
    Task<TransferResult> DownloadAsync(
        string remotePath, 
        string localPath,
        DownloadOptions? options = null, 
        IProgress<TransferProgress>? progress = null,
        CancellationToken ct = default);
    
    // Batch operations
    Task<TransferResult> UploadBatchAsync(
        IEnumerable<(string LocalPath, string RemotePath)> files,
        UploadOptions? options = null,
        IProgress<TransferProgress>? progress = null,
        CancellationToken ct = default);
    
    Task<TransferResult> DownloadBatchAsync(
        IEnumerable<(string RemotePath, string LocalPath)> files,
        DownloadOptions? options = null,
        IProgress<TransferProgress>? progress = null,
        CancellationToken ct = default);
}

// src/Acode.Application/Compute/Ssh/FileTransfer/UploadOptions.cs
namespace Acode.Application.Compute.Ssh.FileTransfer;

public sealed record UploadOptions
{
    public bool Recursive { get; init; } = true;
    public bool Overwrite { get; init; } = true;
    public bool CreateParentDirectories { get; init; } = true;
    public bool PreserveTimestamps { get; init; } = true;
    public bool PreservePermissions { get; init; } = true;
    public bool PreserveOwner { get; init; } = false;
    public bool UseAtomicRename { get; init; } = true;
    public bool VerifyChecksum { get; init; } = true;
    public bool EnableResume { get; init; } = true;
    public int ChunkSizeBytes { get; init; } = 65536;
    public int ParallelTransfers { get; init; } = 1;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromHours(1);
    public string? GlobPattern { get; init; }
    
    public static UploadOptions Default => new();
}

// src/Acode.Application/Compute/Ssh/FileTransfer/DownloadOptions.cs
namespace Acode.Application.Compute.Ssh.FileTransfer;

public sealed record DownloadOptions
{
    public bool Recursive { get; init; } = true;
    public bool Overwrite { get; init; } = true;
    public bool CreateParentDirectories { get; init; } = true;
    public bool PreserveTimestamps { get; init; } = true;
    public bool PreservePermissions { get; init; } = true;
    public bool UseAtomicRename { get; init; } = true;
    public bool VerifyChecksum { get; init; } = true;
    public bool EnableResume { get; init; } = true;
    public int ChunkSizeBytes { get; init; } = 65536;
    public int ParallelTransfers { get; init; } = 4;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromHours(1);
    public string? GlobPattern { get; init; }
    
    public static DownloadOptions Default => new();
}

// src/Acode.Application/Compute/Ssh/FileTransfer/ITransferResumeManager.cs
namespace Acode.Application.Compute.Ssh.FileTransfer;

public interface ITransferResumeManager
{
    Task<ResumeState?> GetResumeStateAsync(
        string sourcePath, 
        string destinationPath,
        CancellationToken ct = default);
    
    Task SaveResumeStateAsync(
        ResumeState state,
        CancellationToken ct = default);
    
    Task ClearResumeStateAsync(
        string sourcePath, 
        string destinationPath,
        CancellationToken ct = default);
}

public sealed record ResumeState(
    string SourcePath,
    string DestinationPath,
    long BytesTransferred,
    long TotalBytes,
    string PartialChecksum,
    DateTimeOffset LastUpdated);

// src/Acode.Application/Compute/Ssh/FileTransfer/IChecksumVerifier.cs
namespace Acode.Application.Compute.Ssh.FileTransfer;

public interface IChecksumVerifier
{
    Task<string> ComputeLocalChecksumAsync(
        string filePath,
        CancellationToken ct = default);
    
    Task<string?> ComputeRemoteChecksumAsync(
        ISshConnection connection,
        string remotePath,
        CancellationToken ct = default);
    
    Task<bool> VerifyAsync(
        string localPath,
        ISshConnection connection,
        string remotePath,
        CancellationToken ct = default);
}
```

**End of Task 030.c Specification - Part 2/4**

### Part 3: Infrastructure Implementation - SFTP Channel and Uploader

```csharp
// src/Acode.Infrastructure/Compute/Ssh/FileTransfer/SftpChannelWrapper.cs
namespace Acode.Infrastructure.Compute.Ssh.FileTransfer;

public sealed class SftpChannelWrapper : ISftpChannel
{
    private readonly ISftpClient _sftp;
    private readonly ISshConnection _connection;
    private readonly IChecksumVerifier _checksumVerifier;
    private readonly ITransferResumeManager _resumeManager;
    private readonly IEventPublisher _events;
    private readonly ILogger<SftpChannelWrapper> _logger;
    
    public bool IsConnected => _sftp.IsConnected;
    
    public SftpChannelWrapper(
        ISftpClient sftp,
        ISshConnection connection,
        IChecksumVerifier checksumVerifier,
        ITransferResumeManager resumeManager,
        IEventPublisher events,
        ILogger<SftpChannelWrapper> logger)
    {
        _sftp = sftp;
        _connection = connection;
        _checksumVerifier = checksumVerifier;
        _resumeManager = resumeManager;
        _events = events;
        _logger = logger;
    }
    
    public async Task<SftpFileInfo> StatAsync(string path, CancellationToken ct = default)
    {
        var attrs = await _sftp.GetAttributesAsync(path, ct);
        return MapToFileInfo(path, attrs);
    }
    
    public async Task<bool> ExistsAsync(string path, CancellationToken ct = default)
    {
        try
        {
            await _sftp.GetAttributesAsync(path, ct);
            return true;
        }
        catch (SftpPathNotFoundException)
        {
            return false;
        }
    }
    
    public async Task<IReadOnlyList<SftpFileInfo>> ListDirectoryAsync(
        string path, 
        bool recursive = false,
        CancellationToken ct = default)
    {
        var results = new List<SftpFileInfo>();
        await ListDirectoryRecursiveAsync(path, recursive, results, ct);
        return results;
    }
    
    private async Task ListDirectoryRecursiveAsync(
        string path,
        bool recursive,
        List<SftpFileInfo> results,
        CancellationToken ct)
    {
        var entries = await _sftp.ListDirectoryAsync(path, ct);
        
        foreach (var entry in entries)
        {
            if (entry.Name is "." or "..") continue;
            
            var info = MapToFileInfo(entry);
            results.Add(info);
            
            if (recursive && info.IsDirectory)
            {
                await ListDirectoryRecursiveAsync(info.FullPath, true, results, ct);
            }
        }
    }
    
    public async Task CreateDirectoryAsync(
        string path, 
        bool recursive = true, 
        uint permissions = 0755,
        CancellationToken ct = default)
    {
        if (recursive)
        {
            var parts = path.Split('/').Where(p => !string.IsNullOrEmpty(p)).ToList();
            var current = path.StartsWith('/') ? "/" : "";
            
            foreach (var part in parts)
            {
                current = Path.Combine(current, part).Replace('\\', '/');
                if (!await ExistsAsync(current, ct))
                {
                    await _sftp.CreateDirectoryAsync(current, ct);
                    await ChmodAsync(current, permissions, ct);
                }
            }
        }
        else
        {
            await _sftp.CreateDirectoryAsync(path, ct);
            await ChmodAsync(path, permissions, ct);
        }
    }
    
    public async Task ChmodAsync(string path, uint permissions, CancellationToken ct = default)
    {
        var attrs = await _sftp.GetAttributesAsync(path, ct);
        attrs.Permissions = permissions;
        await _sftp.SetAttributesAsync(path, attrs, ct);
    }
    
    public async Task<TransferResult> UploadAsync(
        string localPath,
        string remotePath,
        UploadOptions? options = null,
        IProgress<TransferProgress>? progress = null,
        CancellationToken ct = default)
    {
        options ??= UploadOptions.Default;
        var uploader = new SftpUploader(
            _sftp, _connection, _checksumVerifier, _resumeManager, _events, _logger);
        
        return await uploader.UploadAsync(localPath, remotePath, options, progress, ct);
    }
    
    public void Dispose() => _sftp.Dispose();
}

// src/Acode.Infrastructure/Compute/Ssh/FileTransfer/SftpUploader.cs
namespace Acode.Infrastructure.Compute.Ssh.FileTransfer;

public sealed class SftpUploader
{
    private readonly ISftpClient _sftp;
    private readonly ISshConnection _connection;
    private readonly IChecksumVerifier _checksumVerifier;
    private readonly ITransferResumeManager _resumeManager;
    private readonly IEventPublisher _events;
    private readonly ILogger _logger;
    
    public async Task<TransferResult> UploadAsync(
        string localPath,
        string remotePath,
        UploadOptions options,
        IProgress<TransferProgress>? progress,
        CancellationToken ct)
    {
        var transferId = Ulid.NewUlid().ToString();
        var stopwatch = Stopwatch.StartNew();
        var files = new List<TransferredFile>();
        var errors = new List<TransferError>();
        long totalBytes = 0;
        var wasResumed = false;
        
        var localInfo = new FileInfo(localPath);
        if (localInfo.Exists)
        {
            // Single file upload
            var result = await UploadFileAsync(
                transferId, localPath, remotePath, localInfo.Length,
                options, progress, ct);
            
            if (result.Success)
            {
                files.Add(new TransferredFile(localPath, remotePath, localInfo.Length, result.Checksum));
                totalBytes = localInfo.Length;
            }
            else if (result.Error != null)
            {
                errors.Add(result.Error);
            }
            wasResumed = result.WasResumed;
        }
        else if (Directory.Exists(localPath) && options.Recursive)
        {
            // Directory upload
            await UploadDirectoryAsync(
                transferId, localPath, remotePath, options,
                files, errors, progress, ref totalBytes, ct);
        }
        else
        {
            errors.Add(new TransferError(localPath, "Path not found", TransferErrorType.FileNotFound));
        }
        
        stopwatch.Stop();
        
        var success = errors.Count == 0;
        await _events.PublishAsync(new TransferCompletedEvent(
            transferId, success, totalBytes, files.Count, stopwatch.Elapsed, DateTimeOffset.UtcNow));
        
        return new TransferResult
        {
            Success = success,
            BytesTransferred = totalBytes,
            FilesTransferred = files.Count,
            Duration = stopwatch.Elapsed,
            Files = files,
            Errors = errors,
            WasResumed = wasResumed,
            ChecksumVerified = options.VerifyChecksum && success
        };
    }
    
    private async Task<FileUploadResult> UploadFileAsync(
        string transferId,
        string localPath,
        string remotePath,
        long fileSize,
        UploadOptions options,
        IProgress<TransferProgress>? progress,
        CancellationToken ct)
    {
        var wasResumed = false;
        long startOffset = 0;
        
        // Check for resume
        if (options.EnableResume)
        {
            var resumeState = await _resumeManager.GetResumeStateAsync(localPath, remotePath, ct);
            if (resumeState != null && resumeState.BytesTransferred < fileSize)
            {
                startOffset = resumeState.BytesTransferred;
                wasResumed = true;
                await _events.PublishAsync(new TransferResumedEvent(
                    transferId, localPath, startOffset, fileSize, DateTimeOffset.UtcNow));
            }
        }
        
        // Create parent directories
        if (options.CreateParentDirectories)
        {
            var parentDir = Path.GetDirectoryName(remotePath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parentDir))
            {
                await CreateRemoteDirectoryAsync(parentDir, ct);
            }
        }
        
        // Upload to temp file if atomic rename enabled
        var targetPath = options.UseAtomicRename 
            ? $"{remotePath}.tmp.{Ulid.NewUlid()}" 
            : remotePath;
        
        try
        {
            await using var localStream = File.OpenRead(localPath);
            localStream.Seek(startOffset, SeekOrigin.Begin);
            
            await _sftp.UploadAsync(localStream, targetPath, startOffset, options.ChunkSizeBytes,
                bytesWritten =>
                {
                    progress?.Report(new TransferProgress
                    {
                        FilePath = localPath,
                        BytesTransferred = startOffset + bytesWritten,
                        TotalBytes = fileSize
                    });
                }, ct);
            
            // Atomic rename
            if (options.UseAtomicRename)
            {
                if (options.Overwrite && await _sftp.ExistsAsync(remotePath, ct))
                {
                    await _sftp.DeleteAsync(remotePath, ct);
                }
                await _sftp.RenameAsync(targetPath, remotePath, ct);
            }
            
            // Preserve attributes
            if (options.PreserveTimestamps || options.PreservePermissions)
            {
                var localAttrs = File.GetAttributes(localPath);
                var attrs = await _sftp.GetAttributesAsync(remotePath, ct);
                
                if (options.PreserveTimestamps)
                {
                    attrs.LastWriteTime = File.GetLastWriteTimeUtc(localPath);
                }
                
                await _sftp.SetAttributesAsync(remotePath, attrs, ct);
            }
            
            // Verify checksum
            string? checksum = null;
            if (options.VerifyChecksum)
            {
                var localChecksum = await _checksumVerifier.ComputeLocalChecksumAsync(localPath, ct);
                var remoteChecksum = await _checksumVerifier.ComputeRemoteChecksumAsync(_connection, remotePath, ct);
                
                if (remoteChecksum != null && localChecksum != remoteChecksum)
                {
                    return new FileUploadResult(false, null, false, 
                        new TransferError(localPath, "Checksum mismatch", TransferErrorType.ChecksumMismatch));
                }
                checksum = localChecksum;
            }
            
            await _resumeManager.ClearResumeStateAsync(localPath, remotePath, ct);
            return new FileUploadResult(true, checksum, wasResumed, null);
        }
        catch (Exception ex)
        {
            // Cleanup temp file
            if (options.UseAtomicRename)
            {
                try { await _sftp.DeleteAsync(targetPath, ct); } catch { }
            }
            
            return new FileUploadResult(false, null, wasResumed,
                new TransferError(localPath, ex.Message, TransferErrorType.Unknown, ex));
        }
    }
    
    private record FileUploadResult(
        bool Success, string? Checksum, bool WasResumed, TransferError? Error);
}
```

**End of Task 030.c Specification - Part 3/4**

### Part 4: Downloader, SCP Fallback, and Implementation Checklist

```csharp
// src/Acode.Infrastructure/Compute/Ssh/FileTransfer/SftpDownloader.cs
namespace Acode.Infrastructure.Compute.Ssh.FileTransfer;

public sealed class SftpDownloader
{
    private readonly ISftpClient _sftp;
    private readonly ISshConnection _connection;
    private readonly IChecksumVerifier _checksumVerifier;
    private readonly ITransferResumeManager _resumeManager;
    private readonly ILogger _logger;
    
    public async Task<TransferResult> DownloadAsync(
        string remotePath,
        string localPath,
        DownloadOptions options,
        IProgress<TransferProgress>? progress,
        CancellationToken ct)
    {
        var transferId = Ulid.NewUlid().ToString();
        var stopwatch = Stopwatch.StartNew();
        var files = new List<TransferredFile>();
        var errors = new List<TransferError>();
        long totalBytes = 0;
        var wasResumed = false;
        
        var remoteInfo = await _sftp.GetAttributesAsync(remotePath, ct);
        
        if (remoteInfo.IsDirectory && options.Recursive)
        {
            await DownloadDirectoryAsync(
                transferId, remotePath, localPath, options,
                files, errors, progress, ref totalBytes, ct);
        }
        else if (!remoteInfo.IsDirectory)
        {
            var result = await DownloadFileAsync(
                transferId, remotePath, localPath, remoteInfo.Size,
                options, progress, ct);
            
            if (result.Success)
            {
                files.Add(new TransferredFile(localPath, remotePath, remoteInfo.Size, result.Checksum));
                totalBytes = remoteInfo.Size;
            }
            else if (result.Error != null)
            {
                errors.Add(result.Error);
            }
            wasResumed = result.WasResumed;
        }
        
        stopwatch.Stop();
        
        return new TransferResult
        {
            Success = errors.Count == 0,
            BytesTransferred = totalBytes,
            FilesTransferred = files.Count,
            Duration = stopwatch.Elapsed,
            Files = files,
            Errors = errors,
            WasResumed = wasResumed,
            ChecksumVerified = options.VerifyChecksum && errors.Count == 0
        };
    }
    
    private async Task<FileDownloadResult> DownloadFileAsync(
        string transferId,
        string remotePath,
        string localPath,
        long fileSize,
        DownloadOptions options,
        IProgress<TransferProgress>? progress,
        CancellationToken ct)
    {
        var wasResumed = false;
        long startOffset = 0;
        
        // Check for resume
        if (options.EnableResume && File.Exists(localPath))
        {
            var localInfo = new FileInfo(localPath);
            if (localInfo.Length < fileSize)
            {
                startOffset = localInfo.Length;
                wasResumed = true;
            }
        }
        
        // Create parent directories
        if (options.CreateParentDirectories)
        {
            var parentDir = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }
        }
        
        var tempPath = options.UseAtomicRename 
            ? $"{localPath}.tmp.{Ulid.NewUlid()}" 
            : localPath;
        
        try
        {
            await using var localStream = new FileStream(
                tempPath, 
                wasResumed ? FileMode.Append : FileMode.Create,
                FileAccess.Write);
            
            await _sftp.DownloadAsync(remotePath, localStream, startOffset, options.ChunkSizeBytes,
                bytesRead =>
                {
                    progress?.Report(new TransferProgress
                    {
                        FilePath = remotePath,
                        BytesTransferred = startOffset + bytesRead,
                        TotalBytes = fileSize
                    });
                }, ct);
            
            // Close stream before rename
            await localStream.FlushAsync(ct);
            localStream.Close();
            
            // Atomic rename
            if (options.UseAtomicRename)
            {
                if (options.Overwrite && File.Exists(localPath))
                {
                    File.Delete(localPath);
                }
                File.Move(tempPath, localPath);
            }
            
            // Preserve attributes
            if (options.PreserveTimestamps)
            {
                var remoteAttrs = await _sftp.GetAttributesAsync(remotePath, ct);
                File.SetLastWriteTimeUtc(localPath, remoteAttrs.LastWriteTime);
            }
            
            // Verify checksum
            string? checksum = null;
            if (options.VerifyChecksum)
            {
                var remoteChecksum = await _checksumVerifier.ComputeRemoteChecksumAsync(
                    _connection, remotePath, ct);
                var localChecksum = await _checksumVerifier.ComputeLocalChecksumAsync(localPath, ct);
                
                if (remoteChecksum != null && localChecksum != remoteChecksum)
                {
                    File.Delete(localPath);
                    return new FileDownloadResult(false, null, false,
                        new TransferError(remotePath, "Checksum mismatch", TransferErrorType.ChecksumMismatch));
                }
                checksum = localChecksum;
            }
            
            await _resumeManager.ClearResumeStateAsync(remotePath, localPath, ct);
            return new FileDownloadResult(true, checksum, wasResumed, null);
        }
        catch (Exception ex)
        {
            if (options.UseAtomicRename && File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }
            
            return new FileDownloadResult(false, null, wasResumed,
                new TransferError(remotePath, ex.Message, TransferErrorType.Unknown, ex));
        }
    }
    
    private record FileDownloadResult(
        bool Success, string? Checksum, bool WasResumed, TransferError? Error);
}

// src/Acode.Infrastructure/Compute/Ssh/FileTransfer/ChecksumVerifier.cs
namespace Acode.Infrastructure.Compute.Ssh.FileTransfer;

public sealed class ChecksumVerifier : IChecksumVerifier
{
    public async Task<string> ComputeLocalChecksumAsync(
        string filePath,
        CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
    
    public async Task<string?> ComputeRemoteChecksumAsync(
        ISshConnection connection,
        string remotePath,
        CancellationToken ct = default)
    {
        // Try sha256sum first (Linux)
        var result = await connection.ExecuteAsync(
            $"sha256sum {ShellEscaper.Escape(remotePath)} | cut -d' ' -f1",
            timeout: TimeSpan.FromMinutes(5),
            ct: ct);
        
        if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
        {
            return result.Output.Trim().ToLowerInvariant();
        }
        
        // Fallback to shasum (macOS)
        result = await connection.ExecuteAsync(
            $"shasum -a 256 {ShellEscaper.Escape(remotePath)} | cut -d' ' -f1",
            timeout: TimeSpan.FromMinutes(5),
            ct: ct);
        
        if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
        {
            return result.Output.Trim().ToLowerInvariant();
        }
        
        return null; // No checksum tool available
    }
    
    public async Task<bool> VerifyAsync(
        string localPath,
        ISshConnection connection,
        string remotePath,
        CancellationToken ct = default)
    {
        var local = await ComputeLocalChecksumAsync(localPath, ct);
        var remote = await ComputeRemoteChecksumAsync(connection, remotePath, ct);
        return remote != null && local == remote;
    }
}

// src/Acode.Infrastructure/Compute/Ssh/FileTransfer/ScpFallback.cs
namespace Acode.Infrastructure.Compute.Ssh.FileTransfer;

public sealed class ScpFallback
{
    private readonly ISshConnection _connection;
    
    public async Task<bool> IsSftpAvailableAsync(CancellationToken ct = default)
    {
        var result = await _connection.ExecuteAsync(
            "cat /etc/ssh/sshd_config | grep -i 'Subsystem.*sftp'",
            ct: ct);
        return result.ExitCode == 0;
    }
    
    public async Task<TransferResult> UploadViaScpAsync(
        string localPath,
        string remotePath,
        IProgress<TransferProgress>? progress,
        CancellationToken ct)
    {
        // SCP via exec channel - implementation omitted for brevity
        // Uses: scp -r localPath user@host:remotePath
        throw new NotImplementedException("SCP fallback to be implemented");
    }
}
```

### Implementation Checklist

| # | Requirement | Test | Impl |
|---|-------------|------|------|
| 1 | SFTP channel opens from connection | ⬜ | ⬜ |
| 2 | Stat returns accurate file info | ⬜ | ⬜ |
| 3 | Exists checks path correctly | ⬜ | ⬜ |
| 4 | ListDirectory returns all entries | ⬜ | ⬜ |
| 5 | Recursive listing works | ⬜ | ⬜ |
| 6 | CreateDirectory recursive works | ⬜ | ⬜ |
| 7 | Upload single file works | ⬜ | ⬜ |
| 8 | Upload directory recursive works | ⬜ | ⬜ |
| 9 | Download single file works | ⬜ | ⬜ |
| 10 | Download directory recursive works | ⬜ | ⬜ |
| 11 | Progress reported during transfer | ⬜ | ⬜ |
| 12 | Resume from partial upload | ⬜ | ⬜ |
| 13 | Resume from partial download | ⬜ | ⬜ |
| 14 | Atomic rename on complete | ⬜ | ⬜ |
| 15 | Temp file cleanup on failure | ⬜ | ⬜ |
| 16 | Checksum verification works | ⬜ | ⬜ |
| 17 | Timestamps preserved | ⬜ | ⬜ |
| 18 | Permissions preserved | ⬜ | ⬜ |
| 19 | Parallel transfers work | ⬜ | ⬜ |
| 20 | SCP fallback when SFTP unavailable | ⬜ | ⬜ |

### Rollout Plan

1. **Tests first**: Unit tests for ChecksumVerifier, TransferResumeManager, path handling
2. **Domain models**: Events, SftpFileInfo, TransferProgress, TransferResult
3. **Application interfaces**: ISftpChannel, UploadOptions, DownloadOptions
4. **Infrastructure impl**: SftpChannelWrapper, SftpUploader, SftpDownloader
5. **Resume support**: TransferResumeManager with state persistence
6. **Checksum support**: ChecksumVerifier with sha256sum/shasum detection
7. **SCP fallback**: ScpFallback for SFTP-unavailable hosts
8. **Integration tests**: Real SFTP transfers, large files, resume scenarios
9. **DI registration**: Register channel factory, resume manager as singleton

**End of Task 030.c Specification**