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

- Task 029.c: Implements artifact interface
- Task 030.a: Uses SFTP channel from pool
- Task 027: Workers transfer files

### Failure Modes

- Transfer timeout → Retry
- Disk full → Error
- Permission denied → Error
- Connection lost → Resume

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

### FR-001 to FR-020: SFTP Operations

- FR-001: SFTP subsystem MUST be used
- FR-002: SFTP channel from connection pool
- FR-003: Multiple channels MUST work
- FR-004: Channel reuse MUST work
- FR-005: `OpenSftp()` MUST return channel
- FR-006: Channel MUST implement IDisposable
- FR-007: Stat MUST work
- FR-008: Stat returns file info
- FR-009: Exists MUST work
- FR-010: IsDirectory MUST work
- FR-011: IsFile MUST work
- FR-012: ListDirectory MUST work
- FR-013: Recursive listing MUST work
- FR-014: MakeDirectory MUST work
- FR-015: MakeDirectory MUST be recursive
- FR-016: Delete MUST work
- FR-017: Rename MUST work
- FR-018: Chmod MUST work
- FR-019: Chown MUST work (if permitted)
- FR-020: Readlink MUST work

### FR-021 to FR-045: Upload

- FR-021: `UploadAsync` MUST work via SFTP
- FR-022: Single file MUST work
- FR-023: Directory MUST work
- FR-024: Recursive MUST work
- FR-025: Glob patterns MUST work
- FR-026: Streaming MUST work
- FR-027: Chunk size configurable
- FR-028: Default chunk: 64KB
- FR-029: Progress callback MUST work
- FR-030: Progress: bytes, percent, rate
- FR-031: Timeout MUST apply
- FR-032: Resume MUST work
- FR-033: Resume checks existing size
- FR-034: Resume appends remainder
- FR-035: Overwrite MUST be configurable
- FR-036: Default: overwrite
- FR-037: Preserve timestamps MUST work
- FR-038: Preserve permissions MUST work
- FR-039: Preserve owner MUST be optional
- FR-040: Create parent dirs MUST work
- FR-041: Temp file MUST be used
- FR-042: Atomic rename on complete
- FR-043: Cleanup on failure
- FR-044: Checksum verification MUST work
- FR-045: Post-upload stat MUST verify

### FR-046 to FR-070: Download

- FR-046: `DownloadAsync` MUST work via SFTP
- FR-047: Single file MUST work
- FR-048: Directory MUST work
- FR-049: Recursive MUST work
- FR-050: Glob patterns MUST work
- FR-051: Streaming MUST work
- FR-052: Progress MUST report
- FR-053: Timeout MUST apply
- FR-054: Resume MUST work
- FR-055: Resume checks local size
- FR-056: Resume seeks to offset
- FR-057: Overwrite MUST be configurable
- FR-058: Preserve timestamps MUST work
- FR-059: Preserve permissions MUST work
- FR-060: Create parent dirs MUST work
- FR-061: Temp file MUST be used
- FR-062: Atomic move on complete
- FR-063: Cleanup on failure
- FR-064: Checksum verification MUST work
- FR-065: Remote checksum via command
- FR-066: sha256sum MUST be used
- FR-067: Fallback to no-verify
- FR-068: Batch download MUST work
- FR-069: Parallel downloads MUST work
- FR-070: Parallelism configurable

### FR-071 to FR-080: SCP Fallback

- FR-071: SCP MUST be fallback
- FR-072: SCP when SFTP unavailable
- FR-073: SCP via exec channel
- FR-074: SCP single file works
- FR-075: SCP directory works
- FR-076: SCP recursive works
- FR-077: SCP preserves permissions
- FR-078: SCP progress MUST work
- FR-079: SCP timeout MUST work
- FR-080: Auto-detect SFTP availability

---

## Non-Functional Requirements

- NFR-001: 1GB file in <5 minutes
- NFR-002: Memory bounded during transfer
- NFR-003: 10 parallel transfers
- NFR-004: Progress update every 1s
- NFR-005: Resume saves bandwidth
- NFR-006: No data corruption
- NFR-007: Atomic file placement
- NFR-008: Structured logging
- NFR-009: Metrics on transfers
- NFR-010: Network interruption handled

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

- [ ] AC-001: SFTP channel works
- [ ] AC-002: Upload single file
- [ ] AC-003: Upload directory
- [ ] AC-004: Download single file
- [ ] AC-005: Download directory
- [ ] AC-006: Progress reports
- [ ] AC-007: Resume works
- [ ] AC-008: Permissions preserved
- [ ] AC-009: Checksum verifies
- [ ] AC-010: SCP fallback works

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Path handling
- [ ] UT-002: Progress calculation
- [ ] UT-003: Resume logic
- [ ] UT-004: Checksum verification

### Integration Tests

- [ ] IT-001: Real SFTP transfer
- [ ] IT-002: Large file transfer
- [ ] IT-003: Directory recursion
- [ ] IT-004: Resume after interrupt

---

## Implementation Prompt

### Interface

```csharp
public interface ISftpChannel : IDisposable
{
    // File operations
    Task<SftpFileInfo> StatAsync(string path, CancellationToken ct = default);
    Task<bool> ExistsAsync(string path, CancellationToken ct = default);
    Task<IReadOnlyList<SftpFileInfo>> ListDirectoryAsync(string path, CancellationToken ct = default);
    
    // Directory operations
    Task CreateDirectoryAsync(string path, bool recursive = true, CancellationToken ct = default);
    Task DeleteAsync(string path, bool recursive = false, CancellationToken ct = default);
    Task RenameAsync(string oldPath, string newPath, CancellationToken ct = default);
    
    // Transfer
    Task UploadAsync(string localPath, string remotePath, 
        UploadOptions options = null, IProgress<TransferProgress> progress = null,
        CancellationToken ct = default);
    Task DownloadAsync(string remotePath, string localPath,
        DownloadOptions options = null, IProgress<TransferProgress> progress = null,
        CancellationToken ct = default);
}

public record SftpFileInfo(
    string Name,
    string FullPath,
    long Size,
    bool IsDirectory,
    bool IsSymlink,
    DateTime Modified,
    uint Permissions,
    uint UserId,
    uint GroupId);
```

### Transfer Result

```csharp
public record SftpTransferResult(
    bool Success,
    long BytesTransferred,
    int FilesTransferred,
    TimeSpan Duration,
    IReadOnlyList<TransferredFile> Files,
    IReadOnlyList<TransferError> Errors);
```

---

**End of Task 030.c Specification**