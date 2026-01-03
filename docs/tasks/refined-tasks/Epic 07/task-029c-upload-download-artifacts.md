# Task 029.c: Upload/Download Artifacts

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 7 – Cloud Integration  
**Dependencies:** Task 029 (Interface), Task 029.a (Prepare)  

---

## Description

Task 029.c implements artifact upload and download for compute targets. Files MUST be transferable between local and remote targets. Progress MUST be reported.

Upload sends files to the target before or during execution. Download retrieves files from the target after execution. Both operations MUST handle large files efficiently.

Transfer MUST support streaming. Large files MUST NOT exhaust memory. Checksums MUST verify integrity. Partial transfers MUST be resumable.

### Business Value

Artifact transfer enables:
- Input data staging
- Output retrieval
- Build artifact collection
- Log file retrieval

### Scope Boundaries

This task covers file transfer. Execution is in 029.b. Workspace preparation is in 029.a.

### Integration Points

- Task 029: Part of target interface
- Task 029.a: Artifacts may be part of workspace
- Task 027: Workers retrieve artifacts

### Failure Modes

- Transfer timeout → Retry
- Checksum mismatch → Re-transfer
- Disk full → Error with cleanup
- Permission denied → Clear error

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Artifact | File to transfer |
| Upload | Local to remote |
| Download | Remote to local |
| Checksum | Integrity verification |
| Resume | Continue partial transfer |
| Streaming | Transfer without full buffering |

---

## Out of Scope

- S3/blob storage integration
- Artifact caching service
- Artifact versioning
- Artifact encryption
- Artifact compression (at this layer)

---

## Functional Requirements

### FR-001 to FR-030: Upload

- FR-001: `UploadAsync` MUST be defined
- FR-002: Upload MUST accept local path
- FR-003: Upload MUST accept remote path
- FR-004: Single file MUST work
- FR-005: Directory MUST work
- FR-006: Directory MUST be recursive
- FR-007: Glob patterns MUST work
- FR-008: Hidden files MUST be optional
- FR-009: Default: exclude hidden
- FR-010: Symlinks MUST follow by default
- FR-011: Symlink handling MUST be configurable
- FR-012: Transfer MUST be streaming
- FR-013: Chunk size MUST be configurable
- FR-014: Default chunk: 64KB
- FR-015: Progress MUST be reported
- FR-016: Progress: bytes transferred
- FR-017: Progress: percent complete
- FR-018: Progress: transfer rate
- FR-019: Checksum MUST be computed
- FR-020: Checksum: SHA256
- FR-021: Checksum MUST be verified
- FR-022: Mismatch MUST retry
- FR-023: Max retries MUST be configurable
- FR-024: Default retries: 3
- FR-025: Timeout MUST be enforced
- FR-026: Default timeout: file-size based
- FR-027: Overwrite MUST be configurable
- FR-028: Default: overwrite
- FR-029: Preserve timestamps MUST be optional
- FR-030: Preserve permissions MUST be optional

### FR-031 to FR-055: Download

- FR-031: `DownloadAsync` MUST be defined
- FR-032: Download MUST accept remote path
- FR-033: Download MUST accept local path
- FR-034: Single file MUST work
- FR-035: Directory MUST work
- FR-036: Glob patterns MUST work
- FR-037: Streaming MUST be used
- FR-038: Progress MUST be reported
- FR-039: Checksum MUST be verified
- FR-040: Mismatch MUST retry
- FR-041: Timeout MUST be enforced
- FR-042: Local directory MUST be created
- FR-043: Parent directories MUST be created
- FR-044: Existing files MAY be overwritten
- FR-045: Overwrite MUST be configurable
- FR-046: Resume MUST be supported
- FR-047: Resume uses partial file
- FR-048: Resume verifies ranges
- FR-049: Non-resumable falls back
- FR-050: Temp file MUST be used
- FR-051: Atomic move on complete
- FR-052: Cleanup on failure
- FR-053: Download result MUST include paths
- FR-054: Result MUST include sizes
- FR-055: Result MUST include checksums

### FR-056 to FR-070: Batch Operations

- FR-056: Batch upload MUST work
- FR-057: Batch accepts file list
- FR-058: Parallel transfer MUST be optional
- FR-059: Default: parallel
- FR-060: Parallelism MUST be configurable
- FR-061: Default parallelism: 4
- FR-062: Per-file progress MUST work
- FR-063: Total progress MUST work
- FR-064: Partial failure MUST be handled
- FR-065: Partial success MUST be reported
- FR-066: Batch download MUST work
- FR-067: Same parallel rules apply
- FR-068: Manifest MUST be available
- FR-069: Manifest lists all transfers
- FR-070: Manifest includes success/failure

---

## Non-Functional Requirements

- NFR-001: 1GB file MUST transfer in <5min
- NFR-002: Memory MUST be bounded
- NFR-003: Network interruption MUST retry
- NFR-004: Progress MUST update every 1s
- NFR-005: 100 parallel transfers MUST work
- NFR-006: No data corruption
- NFR-007: Atomic file placement
- NFR-008: Cleanup on failure
- NFR-009: Cross-platform paths
- NFR-010: Structured logging

---

## User Manual Documentation

### Configuration

```yaml
artifacts:
  chunkSizeBytes: 65536
  checksumAlgorithm: sha256
  maxRetries: 3
  parallelTransfers: 4
  preserveTimestamps: true
  preservePermissions: true
```

### Example Usage

```csharp
// Upload file
await target.UploadAsync(
    "/local/input.zip", 
    "/remote/input.zip");

// Upload directory
await target.UploadAsync(
    "/local/data/", 
    "/remote/data/",
    new UploadOptions { Recursive = true });

// Download with progress
await target.DownloadAsync(
    "/remote/output.zip",
    "/local/output.zip",
    progress: p => Console.WriteLine($"{p.Percent}%"));

// Batch download
await target.DownloadAsync(
    new[] { "/remote/a.txt", "/remote/b.txt" },
    "/local/outputs/");
```

### Progress Events

| Field | Description |
|-------|-------------|
| BytesTransferred | Bytes completed |
| TotalBytes | Total to transfer |
| Percent | Completion percentage |
| Rate | Bytes per second |
| ETA | Estimated time remaining |

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Upload single file works
- [ ] AC-002: Upload directory works
- [ ] AC-003: Download single file works
- [ ] AC-004: Download directory works
- [ ] AC-005: Streaming works
- [ ] AC-006: Checksum verifies
- [ ] AC-007: Progress reports
- [ ] AC-008: Resume works
- [ ] AC-009: Batch works
- [ ] AC-010: Errors handled

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Checksum calculation
- [ ] UT-002: Progress reporting
- [ ] UT-003: Retry logic
- [ ] UT-004: Path handling

### Integration Tests

- [ ] IT-001: Large file transfer
- [ ] IT-002: Directory transfer
- [ ] IT-003: Batch parallel
- [ ] IT-004: Resume after interrupt

---

## Implementation Prompt

### Interface

```csharp
public record UploadOptions(
    bool Recursive = true,
    bool OverwriteExisting = true,
    bool PreserveTimestamps = true,
    bool PreservePermissions = true,
    bool IncludeHidden = false,
    SymlinkHandling Symlinks = SymlinkHandling.Follow);

public record DownloadOptions(
    bool Recursive = true,
    bool OverwriteExisting = true,
    bool Resume = true);

public record TransferProgress(
    long BytesTransferred,
    long TotalBytes,
    double Percent,
    double BytesPerSecond,
    TimeSpan? ETA,
    string CurrentFile);

public record TransferResult(
    bool Success,
    long BytesTransferred,
    int FileCount,
    string Checksum,
    IReadOnlyList<TransferredFile> Files,
    IReadOnlyList<TransferError> Errors);

public record TransferredFile(
    string LocalPath,
    string RemotePath,
    long Size,
    string Checksum);

public record TransferError(
    string Path,
    string Error);

public enum SymlinkHandling { Follow, Skip, Copy }
```

---

**End of Task 029.c Specification**