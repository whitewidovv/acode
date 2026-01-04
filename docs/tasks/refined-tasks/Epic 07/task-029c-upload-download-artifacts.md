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

| Component | Integration Type | Description |
|-----------|-----------------|-------------|
| Task 029 IComputeTarget | Parent | TransferArtifactsAsync is part of target interface |
| Task 029.a Workspace | Prerequisite | Artifacts may be transferred as part of preparation |
| Task 027 Workers | Consumer | Workers retrieve artifacts after task completion |
| Task 030 SSH Target | Override | Uses SCP/SFTP for remote transfers |
| Task 031 EC2 Target | Override | Uses S3 or SSH for cloud transfers |
| ITransferService | Interface | Main contract for transfer logic |
| IProgressReporter | Callback | User-provided progress reporting |

### Failure Modes

| Failure Type | Detection | Recovery | User Impact |
|--------------|-----------|----------|-------------|
| Transfer timeout | Timer expiration | Retry with resume | Delayed completion |
| Checksum mismatch | Hash comparison | Re-transfer entire file | Automatic retry |
| Disk full (remote) | IOException | Error with space needed | Must free space |
| Disk full (local) | IOException | Cleanup partial, error | Must free space |
| Permission denied | Access exception | Clear error with path | Fix permissions |
| File not found | FileNotFound | Clear error with path | Fix source path |
| Network interruption | Connection error | Retry with resume | Automatic recovery |
| Path too long | PathTooLong | Truncate or error | Shorten path |

---

## Assumptions

1. **Target Ready**: Compute target is provisioned and accessible for file operations
2. **Sufficient Space**: Disk space checked before transfer (not mid-transfer)
3. **Network Available**: For remote targets, network connectivity exists (respecting mode)
4. **Hash Support**: SHA256 hashing available on all platforms
5. **Streaming Support**: Target supports chunked/streaming file writes
6. **Path Compatibility**: Path separators handled (Unix ↔ Windows)
7. **File Handle Limits**: System file handle limits sufficient for parallel transfers
8. **Time Synchronization**: Clocks synchronized for timestamp preservation

---

## Security Considerations

1. **Path Validation**: All paths validated to prevent directory traversal attacks
2. **Content Scanning**: Optional malware scanning hook for downloaded artifacts
3. **Secure Transfer**: Remote transfers use encrypted channels (SSH/TLS)
4. **Permission Preservation**: Permissions not blindly copied (could escalate privileges)
5. **Sensitive Content**: Artifacts marked sensitive excluded from logs
6. **Checksum Verification**: All transfers verified to prevent corruption/tampering
7. **Temporary File Security**: Temp files created with restricted permissions
8. **Audit Trail**: All transfers logged with source, destination, size, checksum

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

### Upload Operations (FR-029C-01 to FR-029C-30)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-029C-01 | `UploadAsync(TransferRequest, CancellationToken)` MUST be defined | Must Have |
| FR-029C-02 | TransferRequest MUST include local source path | Must Have |
| FR-029C-03 | TransferRequest MUST include remote destination path | Must Have |
| FR-029C-04 | Single file upload MUST be supported | Must Have |
| FR-029C-05 | Directory upload MUST be supported | Must Have |
| FR-029C-06 | Directory upload MUST be recursive by default | Must Have |
| FR-029C-07 | Glob patterns MUST be supported for file selection | Should Have |
| FR-029C-08 | Hidden files MUST be configurable (default: exclude) | Should Have |
| FR-029C-09 | Symlinks MUST follow by default | Should Have |
| FR-029C-10 | Symlink behavior MUST be configurable (follow/copy/skip) | Should Have |
| FR-029C-11 | Transfer MUST use streaming (not buffering entire file) | Must Have |
| FR-029C-12 | Chunk size MUST be configurable | Should Have |
| FR-029C-13 | Default chunk size: 64KB | Should Have |
| FR-029C-14 | Progress MUST be reported during transfer | Must Have |
| FR-029C-15 | Progress MUST include bytes transferred | Must Have |
| FR-029C-16 | Progress MUST include percent complete | Must Have |
| FR-029C-17 | Progress MUST include current transfer rate (bytes/sec) | Should Have |
| FR-029C-18 | Progress MUST include estimated time remaining | Should Have |
| FR-029C-19 | SHA256 checksum MUST be computed during transfer | Must Have |
| FR-029C-20 | Checksum MUST be verified after transfer | Must Have |
| FR-029C-21 | Checksum mismatch MUST trigger retry | Must Have |
| FR-029C-22 | Maximum retries MUST be configurable (default: 3) | Should Have |
| FR-029C-23 | Timeout MUST be enforced per transfer | Must Have |
| FR-029C-24 | Default timeout MUST be file-size based (1min + 1min/100MB) | Should Have |
| FR-029C-25 | Overwrite behavior MUST be configurable | Should Have |
| FR-029C-26 | Default overwrite: replace existing files | Should Have |
| FR-029C-27 | Skip existing option MUST be available | Should Have |
| FR-029C-28 | Error on existing option MUST be available | Should Have |
| FR-029C-29 | Preserve timestamps MUST be optional (default: true) | Should Have |
| FR-029C-30 | Preserve permissions MUST be optional (default: false) | Should Have |

### Download Operations (FR-029C-31 to FR-029C-55)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-029C-31 | `DownloadAsync(TransferRequest, CancellationToken)` MUST be defined | Must Have |
| FR-029C-32 | Download MUST accept remote source path | Must Have |
| FR-029C-33 | Download MUST accept local destination path | Must Have |
| FR-029C-34 | Single file download MUST be supported | Must Have |
| FR-029C-35 | Directory download MUST be supported | Must Have |
| FR-029C-36 | Glob patterns MUST work for remote file selection | Should Have |
| FR-029C-37 | Streaming MUST be used (no full buffering) | Must Have |
| FR-029C-38 | Progress MUST be reported | Must Have |
| FR-029C-39 | Checksum MUST be verified after download | Must Have |
| FR-029C-40 | Checksum mismatch MUST retry transfer | Must Have |
| FR-029C-41 | Timeout MUST be enforced | Must Have |
| FR-029C-42 | Local directory MUST be created if not exists | Must Have |
| FR-029C-43 | Parent directories MUST be created as needed | Must Have |
| FR-029C-44 | Existing files MAY be overwritten (configurable) | Should Have |
| FR-029C-45 | Resume MUST be supported for partial downloads | Should Have |
| FR-029C-46 | Resume MUST use existing partial file | Should Have |
| FR-029C-47 | Resume MUST verify range checksums | Should Have |
| FR-029C-48 | Non-resumable transfers MUST fall back to full download | Must Have |
| FR-029C-49 | Temporary file MUST be used during download | Must Have |
| FR-029C-50 | Atomic rename on completion MUST be used | Must Have |
| FR-029C-51 | Failed download MUST cleanup temp files | Must Have |
| FR-029C-52 | TransferResult MUST include all downloaded paths | Must Have |
| FR-029C-53 | TransferResult MUST include file sizes | Must Have |
| FR-029C-54 | TransferResult MUST include checksums | Must Have |
| FR-029C-55 | TransferResult MUST include transfer duration | Must Have |

### Batch Operations (FR-029C-56 to FR-029C-70)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-029C-56 | Batch upload MUST accept list of file pairs | Must Have |
| FR-029C-57 | Batch MUST support mixed files and directories | Should Have |
| FR-029C-58 | Parallel transfer MUST be optional (default: true) | Should Have |
| FR-029C-59 | Parallelism MUST be configurable (default: 4) | Should Have |
| FR-029C-60 | Per-file progress MUST be available | Should Have |
| FR-029C-61 | Total progress MUST aggregate all transfers | Must Have |
| FR-029C-62 | Partial failure MUST be handled gracefully | Must Have |
| FR-029C-63 | Successful transfers MUST complete even if some fail | Should Have |
| FR-029C-64 | Batch result MUST report success and failures separately | Must Have |
| FR-029C-65 | Batch download MUST work similarly | Must Have |
| FR-029C-66 | Same parallelism rules apply to downloads | Should Have |
| FR-029C-67 | Manifest file MUST be generatable | Should Have |
| FR-029C-68 | Manifest MUST list all transfers with status | Should Have |
| FR-029C-69 | Manifest MUST include checksums | Should Have |
| FR-029C-70 | Manifest MUST be JSON format | Should Have |

---

## Non-Functional Requirements

### Performance (NFR-029C-01 to NFR-029C-10)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-029C-01 | 1GB file transfer time | <5 minutes on LAN | Should Have |
| NFR-029C-02 | Transfer throughput minimum | 50MB/s on local, 10MB/s on SSH | Should Have |
| NFR-029C-03 | Memory usage per transfer | <10MB (regardless of file size) | Must Have |
| NFR-029C-04 | Parallel transfers supported | 100 concurrent | Should Have |
| NFR-029C-05 | Progress update frequency | Every 1 second | Should Have |
| NFR-029C-06 | Small file overhead | <10ms per file | Should Have |
| NFR-029C-07 | Batch of 1000 small files | <60 seconds | Should Have |
| NFR-029C-08 | Checksum computation overhead | <5% of transfer time | Should Have |
| NFR-029C-09 | Resume overhead | <1 second | Should Have |
| NFR-029C-10 | Directory enumeration (10K files) | <5 seconds | Should Have |

### Reliability (NFR-029C-11 to NFR-029C-20)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-029C-11 | Data corruption rate | 0% (checksum verified) | Must Have |
| NFR-029C-12 | Network interruption recovery | Automatic resume | Should Have |
| NFR-029C-13 | Atomic file placement | No partial files visible | Must Have |
| NFR-029C-14 | Temp file cleanup on failure | 100% | Must Have |
| NFR-029C-15 | Retry success rate (transient failures) | 95%+ | Should Have |
| NFR-029C-16 | Concurrent access safety | No corruption | Must Have |
| NFR-029C-17 | Path handling cross-platform | Windows ↔ Unix | Must Have |
| NFR-029C-18 | Special character handling | All valid filenames | Should Have |
| NFR-029C-19 | Long path support | 4096 chars on Unix, 260+ on Windows | Should Have |
| NFR-029C-20 | Cancellation cleanup | All temp files removed | Must Have |

### Observability (NFR-029C-21 to NFR-029C-30)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-029C-21 | Transfer start log | Info level with source/dest | Must Have |
| NFR-029C-22 | Transfer complete log | Info level with size/duration | Must Have |
| NFR-029C-23 | Transfer failure log | Error level with reason | Must Have |
| NFR-029C-24 | Retry log | Warning level | Should Have |
| NFR-029C-25 | Progress log (large files) | Debug level every 10% | Should Have |
| NFR-029C-26 | Checksum log | Debug level | Should Have |
| NFR-029C-27 | Structured logging format | JSON-compatible | Should Have |
| NFR-029C-28 | TargetId in all logs | Correlation | Must Have |
| NFR-029C-29 | Metric: transfer_bytes_total | Counter | Should Have |
| NFR-029C-30 | Metric: transfer_duration_seconds | Histogram | Should Have |

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

### Upload Operations (AC-029C-01 to AC-029C-20)

- [ ] AC-029C-01: `IArtifactTransfer` interface defined in Application layer
- [ ] AC-029C-02: `UploadAsync` method accepts TransferRequest and CancellationToken
- [ ] AC-029C-03: TransferRequest includes source path, destination path, options
- [ ] AC-029C-04: Single file upload works correctly
- [ ] AC-029C-05: Directory upload works recursively
- [ ] AC-029C-06: Glob patterns filter files correctly
- [ ] AC-029C-07: Hidden files excluded by default
- [ ] AC-029C-08: Symlinks followed by default
- [ ] AC-029C-09: Transfer uses streaming (bounded memory)
- [ ] AC-029C-10: Chunk size configurable
- [ ] AC-029C-11: Progress reported every 1 second
- [ ] AC-029C-12: Progress includes bytes, percent, rate, ETA
- [ ] AC-029C-13: SHA256 checksum computed during transfer
- [ ] AC-029C-14: Checksum verified after transfer completes
- [ ] AC-029C-15: Checksum mismatch triggers automatic retry
- [ ] AC-029C-16: Maximum retries respected (default 3)
- [ ] AC-029C-17: Timeout enforced per transfer
- [ ] AC-029C-18: Overwrite mode works (default: replace)
- [ ] AC-029C-19: Timestamps preserved when configured
- [ ] AC-029C-20: Upload returns TransferResult with details

### Download Operations (AC-029C-21 to AC-029C-40)

- [ ] AC-029C-21: `DownloadAsync` method accepts TransferRequest and CancellationToken
- [ ] AC-029C-22: Single file download works correctly
- [ ] AC-029C-23: Directory download works recursively
- [ ] AC-029C-24: Remote glob patterns work
- [ ] AC-029C-25: Streaming used (bounded memory)
- [ ] AC-029C-26: Progress reported during download
- [ ] AC-029C-27: Checksum verified after download
- [ ] AC-029C-28: Mismatch triggers retry
- [ ] AC-029C-29: Timeout enforced
- [ ] AC-029C-30: Local directory created if not exists
- [ ] AC-029C-31: Parent directories created as needed
- [ ] AC-029C-32: Overwrite configurable
- [ ] AC-029C-33: Resume supported for interrupted downloads
- [ ] AC-029C-34: Resume uses partial file and range requests
- [ ] AC-029C-35: Non-resumable falls back to full download
- [ ] AC-029C-36: Temporary file used during download
- [ ] AC-029C-37: Atomic rename on completion
- [ ] AC-029C-38: Failed download cleans up temp files
- [ ] AC-029C-39: Result includes all paths, sizes, checksums
- [ ] AC-029C-40: Result includes total duration

### Batch Operations (AC-029C-41 to AC-029C-50)

- [ ] AC-029C-41: Batch upload accepts list of file pairs
- [ ] AC-029C-42: Batch processes in parallel (configurable)
- [ ] AC-029C-43: Default parallelism is 4
- [ ] AC-029C-44: Per-file progress available
- [ ] AC-029C-45: Total progress aggregates all transfers
- [ ] AC-029C-46: Partial failure doesn't stop other transfers
- [ ] AC-029C-47: Result separates successes and failures
- [ ] AC-029C-48: Batch download works similarly
- [ ] AC-029C-49: Manifest file generatable as JSON
- [ ] AC-029C-50: Manifest includes all transfer details

### Reliability and Cleanup (AC-029C-51 to AC-029C-60)

- [ ] AC-029C-51: No data corruption (verified by checksum)
- [ ] AC-029C-52: Network interruption handled with retry
- [ ] AC-029C-53: Temp files cleaned up on any failure
- [ ] AC-029C-54: Cancellation cleans up resources
- [ ] AC-029C-55: File handles properly closed
- [ ] AC-029C-56: Memory stays bounded for large files
- [ ] AC-029C-57: Cross-platform path handling works
- [ ] AC-029C-58: Special characters in filenames work
- [ ] AC-029C-59: Long paths handled correctly
- [ ] AC-029C-60: Concurrent transfers don't interfere

---

## User Verification Scenarios

### Scenario 1: Upload Build Artifacts to Remote Target

**Persona:** Developer collecting build outputs

**Steps:**
1. Build project locally generating bin/Release folder
2. Execute upload: `target.UploadAsync("./bin/Release/", "/workspace/artifacts/")`
3. Observe progress: "Uploading 45 files (256MB)..."
4. Observe progress updates every second
5. Observe completion: "Upload complete in 12s, checksum verified"
6. Verify files exist on remote target

**Verification:**
- [ ] All files transferred
- [ ] Progress updates accurate
- [ ] Checksums verified
- [ ] No orphan temp files

### Scenario 2: Download Test Results After Remote Execution

**Persona:** Developer retrieving test outputs

**Steps:**
1. Execute tests on remote target
2. Execute download: `target.DownloadAsync("/workspace/TestResults/", "./results/")`
3. Observe progress during download
4. Verify local files match remote
5. Check checksums in result

**Verification:**
- [ ] All files downloaded
- [ ] Checksums match
- [ ] Timestamps preserved (if configured)

### Scenario 3: Resume Interrupted Large File Transfer

**Persona:** Developer on unstable network

**Steps:**
1. Start upload of 1GB file
2. Simulate network interruption at 50%
3. Observe retry with resume
4. Transfer continues from 50%
5. Complete successfully

**Verification:**
- [ ] Resume starts from partial point
- [ ] Total time less than full re-transfer
- [ ] Final checksum correct

### Scenario 4: Batch Transfer with Partial Failure

**Persona:** Developer uploading multiple artifacts

**Steps:**
1. Upload 10 files, one has permission error
2. Observe: 9 succeed, 1 fails
3. Result shows which failed and why
4. Successful files are intact

**Verification:**
- [ ] Partial failure reported correctly
- [ ] Successful transfers complete
- [ ] Error message is actionable

### Scenario 5: Large File Memory Stability

**Persona:** Developer transferring 10GB file

**Steps:**
1. Upload 10GB file
2. Monitor memory usage during transfer
3. Memory stays under 50MB
4. Transfer completes successfully

**Verification:**
- [ ] Memory bounded
- [ ] No out-of-memory errors
- [ ] Streaming working correctly

### Scenario 6: Cancellation During Transfer

**Persona:** Developer cancelling wrong transfer

**Steps:**
1. Start large upload
2. Cancel at 25%
3. Observe cleanup: "Cancelling... cleaning up temp files"
4. No partial files on remote
5. No temp files locally

**Verification:**
- [ ] Cancellation responsive
- [ ] All temp files cleaned
- [ ] No orphan data

---

## Testing Requirements

### Unit Tests (UT-029C-01 to UT-029C-20)

- [ ] UT-029C-01: SHA256 checksum calculated correctly
- [ ] UT-029C-02: Progress calculation accurate
- [ ] UT-029C-03: Transfer rate calculation correct
- [ ] UT-029C-04: ETA estimation reasonable
- [ ] UT-029C-05: Retry logic respects max retries
- [ ] UT-029C-06: Exponential backoff timing correct
- [ ] UT-029C-07: Path normalization Unix → Windows
- [ ] UT-029C-08: Path normalization Windows → Unix
- [ ] UT-029C-09: Glob pattern matching works
- [ ] UT-029C-10: Hidden file detection works
- [ ] UT-029C-11: Symlink detection works
- [ ] UT-029C-12: TransferResult serializes to JSON
- [ ] UT-029C-13: Manifest generation works
- [ ] UT-029C-14: Temp file naming unique
- [ ] UT-029C-15: Atomic rename works
- [ ] UT-029C-16: Cleanup removes all temp files
- [ ] UT-029C-17: Parallel transfer coordination
- [ ] UT-029C-18: Partial failure aggregation
- [ ] UT-029C-19: Cancellation propagation
- [ ] UT-029C-20: Events contain correct data

### Integration Tests (IT-029C-01 to IT-029C-15)

- [ ] IT-029C-01: Upload single file end-to-end
- [ ] IT-029C-02: Upload directory end-to-end
- [ ] IT-029C-03: Download single file end-to-end
- [ ] IT-029C-04: Download directory end-to-end
- [ ] IT-029C-05: Large file (1GB) transfer
- [ ] IT-029C-06: Batch upload with parallelism
- [ ] IT-029C-07: Batch download with parallelism
- [ ] IT-029C-08: Resume after network interruption
- [ ] IT-029C-09: Checksum mismatch retry
- [ ] IT-029C-10: Timeout handling
- [ ] IT-029C-11: Cancellation cleanup
- [ ] IT-029C-12: Cross-platform path handling
- [ ] IT-029C-13: Special characters in filenames
- [ ] IT-029C-14: Memory stable during large transfers
- [ ] IT-029C-15: 100 concurrent transfers

---

## Implementation Prompt

You are implementing artifact upload/download for compute targets. This handles file transfers with streaming, checksums, and resume support. Follow Clean Architecture and TDD.

### Part 1: File Structure and Domain Models

#### File Structure

```
src/Acode.Domain/
├── Compute/
│   └── Artifacts/
│       ├── TransferDirection.cs
│       ├── TransferProgress.cs
│       ├── TransferResult.cs
│       ├── TransferOptions.cs
│       ├── TransferredFile.cs
│       ├── TransferError.cs
│       ├── SymlinkHandling.cs
│       └── Events/
│           ├── TransferStartedEvent.cs
│           ├── TransferProgressEvent.cs
│           ├── TransferCompletedEvent.cs
│           └── TransferFailedEvent.cs

src/Acode.Application/
├── Compute/
│   └── Artifacts/
│       ├── IArtifactTransfer.cs
│       ├── IChecksumCalculator.cs
│       ├── IStreamingTransfer.cs
│       └── IBatchTransfer.cs

src/Acode.Infrastructure/
├── Compute/
│   └── Artifacts/
│       ├── ArtifactTransferService.cs
│       ├── ChecksumCalculator.cs
│       ├── StreamingTransfer.cs
│       ├── BatchTransfer.cs
│       ├── Local/
│       │   ├── LocalUploader.cs
│       │   └── LocalDownloader.cs
│       ├── Remote/
│       │   ├── ScpTransfer.cs
│       │   └── RsyncTransfer.cs
│       └── Resume/
│           ├── ResumeManager.cs
│           └── PartialFileTracker.cs

tests/Acode.Infrastructure.Tests/
├── Compute/
│   └── Artifacts/
│       ├── ArtifactTransferServiceTests.cs
│       ├── ChecksumCalculatorTests.cs
│       ├── StreamingTransferTests.cs
│       ├── BatchTransferTests.cs
│       └── Resume/
│           └── ResumeManagerTests.cs
```

#### Domain Models

```csharp
// src/Acode.Domain/Compute/Artifacts/TransferDirection.cs
namespace Acode.Domain.Compute.Artifacts;

public enum TransferDirection { Upload, Download }

// src/Acode.Domain/Compute/Artifacts/TransferProgress.cs
namespace Acode.Domain.Compute.Artifacts;

public sealed record TransferProgress
{
    public required long BytesTransferred { get; init; }
    public required long TotalBytes { get; init; }
    public double Percent => TotalBytes > 0 ? BytesTransferred * 100.0 / TotalBytes : 0;
    public double BytesPerSecond { get; init; }
    public TimeSpan? EstimatedTimeRemaining { get; init; }
    public string? CurrentFile { get; init; }
    public int FilesTransferred { get; init; }
    public int TotalFiles { get; init; }
}

// src/Acode.Domain/Compute/Artifacts/TransferResult.cs
namespace Acode.Domain.Compute.Artifacts;

public sealed record TransferResult
{
    public required bool Success { get; init; }
    public required long BytesTransferred { get; init; }
    public required int FileCount { get; init; }
    public required TimeSpan Duration { get; init; }
    public string? Checksum { get; init; }
    public IReadOnlyList<TransferredFile> Files { get; init; } = [];
    public IReadOnlyList<TransferError> Errors { get; init; } = [];
    
    public static TransferResult Failed(string error) => new()
    {
        Success = false,
        BytesTransferred = 0,
        FileCount = 0,
        Duration = TimeSpan.Zero,
        Errors = [new TransferError("", error)]
    };
}

// src/Acode.Domain/Compute/Artifacts/TransferredFile.cs
namespace Acode.Domain.Compute.Artifacts;

public sealed record TransferredFile(
    string LocalPath,
    string RemotePath,
    long Size,
    string Checksum,
    DateTimeOffset TransferredAt);

// src/Acode.Domain/Compute/Artifacts/TransferError.cs
namespace Acode.Domain.Compute.Artifacts;

public sealed record TransferError(string Path, string ErrorMessage, bool Retryable = false);

// src/Acode.Domain/Compute/Artifacts/SymlinkHandling.cs
namespace Acode.Domain.Compute.Artifacts;

public enum SymlinkHandling { Follow, Skip, CopyAsLink }

// src/Acode.Domain/Compute/Artifacts/TransferOptions.cs
namespace Acode.Domain.Compute.Artifacts;

public sealed record UploadOptions
{
    public bool Recursive { get; init; } = true;
    public bool OverwriteExisting { get; init; } = true;
    public bool PreserveTimestamps { get; init; } = true;
    public bool PreservePermissions { get; init; } = true;
    public bool IncludeHidden { get; init; } = false;
    public SymlinkHandling Symlinks { get; init; } = SymlinkHandling.Follow;
    public IReadOnlyList<string>? ExcludePatterns { get; init; }
    public int ChunkSizeBytes { get; init; } = 64 * 1024;
}

public sealed record DownloadOptions
{
    public bool Recursive { get; init; } = true;
    public bool OverwriteExisting { get; init; } = true;
    public bool Resume { get; init; } = true;
    public bool VerifyChecksum { get; init; } = true;
    public int MaxRetries { get; init; } = 3;
}
```

**End of Task 029.c Specification - Part 1/4**

### Part 2: Application Interfaces

```csharp
// src/Acode.Application/Compute/Artifacts/IArtifactTransfer.cs
namespace Acode.Application.Compute.Artifacts;

public interface IArtifactTransfer
{
    Task<TransferResult> UploadAsync(
        IComputeTarget target,
        string localPath,
        string remotePath,
        UploadOptions? options = null,
        IProgress<TransferProgress>? progress = null,
        CancellationToken ct = default);
    
    Task<TransferResult> DownloadAsync(
        IComputeTarget target,
        string remotePath,
        string localPath,
        DownloadOptions? options = null,
        IProgress<TransferProgress>? progress = null,
        CancellationToken ct = default);
    
    Task<TransferResult> UploadBatchAsync(
        IComputeTarget target,
        IReadOnlyList<(string Local, string Remote)> files,
        UploadOptions? options = null,
        IProgress<TransferProgress>? progress = null,
        CancellationToken ct = default);
    
    Task<TransferResult> DownloadBatchAsync(
        IComputeTarget target,
        IReadOnlyList<(string Remote, string Local)> files,
        DownloadOptions? options = null,
        IProgress<TransferProgress>? progress = null,
        CancellationToken ct = default);
}

// src/Acode.Application/Compute/Artifacts/IChecksumCalculator.cs
namespace Acode.Application.Compute.Artifacts;

public interface IChecksumCalculator
{
    Task<string> CalculateAsync(string filePath, CancellationToken ct = default);
    Task<string> CalculateAsync(Stream stream, CancellationToken ct = default);
    bool Verify(string filePath, string expectedChecksum);
}

// src/Acode.Application/Compute/Artifacts/IStreamingTransfer.cs
namespace Acode.Application.Compute.Artifacts;

public interface IStreamingTransfer
{
    Task TransferAsync(
        Stream source,
        Stream destination,
        int chunkSize,
        IProgress<long>? bytesProgress = null,
        CancellationToken ct = default);
}

// src/Acode.Application/Compute/Artifacts/IBatchTransfer.cs
namespace Acode.Application.Compute.Artifacts;

public interface IBatchTransfer
{
    int MaxParallelism { get; set; }
    
    Task<BatchTransferResult> TransferAsync(
        IReadOnlyList<TransferItem> items,
        IProgress<BatchProgress>? progress = null,
        CancellationToken ct = default);
}

public sealed record TransferItem(
    string Source,
    string Destination,
    TransferDirection Direction);

public sealed record BatchProgress(
    int CompletedFiles,
    int TotalFiles,
    long TotalBytesTransferred,
    string? CurrentFile);

public sealed record BatchTransferResult(
    bool AllSucceeded,
    int SuccessCount,
    int FailureCount,
    IReadOnlyList<TransferredFile> Succeeded,
    IReadOnlyList<TransferError> Failed);
```

**End of Task 029.c Specification - Part 2/4**

### Part 3: Infrastructure Implementation

```csharp
// src/Acode.Infrastructure/Compute/Artifacts/ArtifactTransferService.cs
namespace Acode.Infrastructure.Compute.Artifacts;

public sealed class ArtifactTransferService : IArtifactTransfer
{
    private readonly IChecksumCalculator _checksum;
    private readonly IStreamingTransfer _streaming;
    private readonly IBatchTransfer _batch;
    private readonly IFileSystem _fileSystem;
    private readonly IEventPublisher _events;
    private readonly ILogger<ArtifactTransferService> _logger;
    
    public ArtifactTransferService(
        IChecksumCalculator checksum,
        IStreamingTransfer streaming,
        IBatchTransfer batch,
        IFileSystem fileSystem,
        IEventPublisher events,
        ILogger<ArtifactTransferService> logger)
    {
        _checksum = checksum;
        _streaming = streaming;
        _batch = batch;
        _fileSystem = fileSystem;
        _events = events;
        _logger = logger;
    }
    
    public async Task<TransferResult> UploadAsync(
        IComputeTarget target,
        string localPath,
        string remotePath,
        UploadOptions? options = null,
        IProgress<TransferProgress>? progress = null,
        CancellationToken ct = default)
    {
        options ??= new UploadOptions();
        var stopwatch = Stopwatch.StartNew();
        
        await _events.PublishAsync(new TransferStartedEvent(
            target.Id, TransferDirection.Upload, localPath, DateTimeOffset.UtcNow));
        
        try
        {
            var files = CollectFiles(localPath, options);
            var totalBytes = files.Sum(f => f.Size);
            long transferred = 0;
            var results = new List<TransferredFile>();
            
            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();
                
                var remoteFile = Path.Combine(remotePath, file.RelativePath);
                
                progress?.Report(new TransferProgress
                {
                    BytesTransferred = transferred,
                    TotalBytes = totalBytes,
                    CurrentFile = file.RelativePath,
                    FilesTransferred = results.Count,
                    TotalFiles = files.Count
                });
                
                var checksum = await TransferFileAsync(
                    target, file.FullPath, remoteFile, options, ct);
                
                transferred += file.Size;
                results.Add(new TransferredFile(
                    file.FullPath, remoteFile, file.Size, checksum, DateTimeOffset.UtcNow));
            }
            
            stopwatch.Stop();
            
            return new TransferResult
            {
                Success = true,
                BytesTransferred = transferred,
                FileCount = results.Count,
                Duration = stopwatch.Elapsed,
                Files = results
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed for {Path}", localPath);
            return TransferResult.Failed(ex.Message);
        }
    }
    
    public async Task<TransferResult> DownloadAsync(
        IComputeTarget target,
        string remotePath,
        string localPath,
        DownloadOptions? options = null,
        IProgress<TransferProgress>? progress = null,
        CancellationToken ct = default)
    {
        options ??= new DownloadOptions();
        var stopwatch = Stopwatch.StartNew();
        
        await _events.PublishAsync(new TransferStartedEvent(
            target.Id, TransferDirection.Download, remotePath, DateTimeOffset.UtcNow));
        
        try
        {
            _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
            
            // Use temp file for atomic write
            var tempPath = localPath + ".tmp";
            
            // Handle resume
            long existingBytes = 0;
            if (options.Resume && _fileSystem.File.Exists(tempPath))
            {
                existingBytes = new FileInfo(tempPath).Length;
            }
            
            await using var destStream = new FileStream(
                tempPath,
                existingBytes > 0 ? FileMode.Append : FileMode.Create,
                FileAccess.Write);
            
            // Transfer implementation depends on target type
            var result = await target.DownloadAsync(
                new ArtifactTransferConfig
                {
                    LocalPath = tempPath,
                    RemotePath = remotePath,
                    Recursive = options.Recursive
                },
                ct);
            
            // Verify checksum if requested
            if (options.VerifyChecksum && result.Success)
            {
                var actualChecksum = await _checksum.CalculateAsync(tempPath, ct);
                // Checksum verification would compare with expected
            }
            
            // Atomic move
            if (_fileSystem.File.Exists(localPath))
                _fileSystem.File.Delete(localPath);
            _fileSystem.File.Move(tempPath, localPath);
            
            stopwatch.Stop();
            
            return new TransferResult
            {
                Success = true,
                BytesTransferred = result.BytesTransferred,
                FileCount = 1,
                Duration = stopwatch.Elapsed,
                Checksum = await _checksum.CalculateAsync(localPath, ct)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download failed for {Path}", remotePath);
            return TransferResult.Failed(ex.Message);
        }
    }
    
    private List<FileToTransfer> CollectFiles(string path, UploadOptions options)
    {
        var files = new List<FileToTransfer>();
        
        if (_fileSystem.File.Exists(path))
        {
            var info = new FileInfo(path);
            files.Add(new FileToTransfer(path, info.Name, info.Length));
        }
        else if (_fileSystem.Directory.Exists(path))
        {
            var searchOption = options.Recursive 
                ? SearchOption.AllDirectories 
                : SearchOption.TopDirectoryOnly;
            
            foreach (var file in _fileSystem.Directory.EnumerateFiles(path, "*", searchOption))
            {
                if (!options.IncludeHidden && Path.GetFileName(file).StartsWith('.'))
                    continue;
                
                if (ShouldExclude(file, path, options.ExcludePatterns))
                    continue;
                
                var info = new FileInfo(file);
                var relative = Path.GetRelativePath(path, file);
                files.Add(new FileToTransfer(file, relative, info.Length));
            }
        }
        
        return files;
    }
    
    private record FileToTransfer(string FullPath, string RelativePath, long Size);
}

// src/Acode.Infrastructure/Compute/Artifacts/ChecksumCalculator.cs
namespace Acode.Infrastructure.Compute.Artifacts;

public sealed class ChecksumCalculator : IChecksumCalculator
{
    public async Task<string> CalculateAsync(string filePath, CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(filePath);
        return await CalculateAsync(stream, ct);
    }
    
    public async Task<string> CalculateAsync(Stream stream, CancellationToken ct = default)
    {
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
    
    public bool Verify(string filePath, string expectedChecksum)
    {
        var actual = CalculateAsync(filePath).GetAwaiter().GetResult();
        return string.Equals(actual, expectedChecksum, StringComparison.OrdinalIgnoreCase);
    }
}

// src/Acode.Infrastructure/Compute/Artifacts/StreamingTransfer.cs
namespace Acode.Infrastructure.Compute.Artifacts;

public sealed class StreamingTransfer : IStreamingTransfer
{
    public async Task TransferAsync(
        Stream source,
        Stream destination,
        int chunkSize,
        IProgress<long>? bytesProgress = null,
        CancellationToken ct = default)
    {
        var buffer = new byte[chunkSize];
        long totalTransferred = 0;
        int bytesRead;
        
        while ((bytesRead = await source.ReadAsync(buffer, ct)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            totalTransferred += bytesRead;
            bytesProgress?.Report(totalTransferred);
        }
        
        await destination.FlushAsync(ct);
    }
}
```

**End of Task 029.c Specification - Part 3/4**

### Part 4: Batch Transfer, Resume Manager, and Rollout

```csharp
// src/Acode.Infrastructure/Compute/Artifacts/BatchTransfer.cs
namespace Acode.Infrastructure.Compute.Artifacts;

public sealed class BatchTransfer : IBatchTransfer
{
    private readonly IArtifactTransfer _singleTransfer;
    private readonly ILogger<BatchTransfer> _logger;
    
    public int MaxParallelism { get; set; } = 4;
    
    public BatchTransfer(IArtifactTransfer singleTransfer, ILogger<BatchTransfer> logger)
    {
        _singleTransfer = singleTransfer;
        _logger = logger;
    }
    
    public async Task<BatchTransferResult> TransferAsync(
        IReadOnlyList<TransferItem> items,
        IProgress<BatchProgress>? progress = null,
        CancellationToken ct = default)
    {
        var succeeded = new ConcurrentBag<TransferredFile>();
        var failed = new ConcurrentBag<TransferError>();
        long totalBytes = 0;
        int completed = 0;
        
        await Parallel.ForEachAsync(
            items,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxParallelism,
                CancellationToken = ct
            },
            async (item, token) =>
            {
                try
                {
                    // Would call appropriate upload/download based on direction
                    // This is a simplified version
                    succeeded.Add(new TransferredFile(
                        item.Source, item.Destination, 0, "", DateTimeOffset.UtcNow));
                    
                    Interlocked.Increment(ref completed);
                    progress?.Report(new BatchProgress(completed, items.Count, totalBytes, item.Source));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Transfer failed for {Source}", item.Source);
                    failed.Add(new TransferError(item.Source, ex.Message));
                }
            });
        
        return new BatchTransferResult(
            failed.IsEmpty,
            succeeded.Count,
            failed.Count,
            succeeded.ToList(),
            failed.ToList());
    }
}

// src/Acode.Infrastructure/Compute/Artifacts/Resume/ResumeManager.cs
namespace Acode.Infrastructure.Compute.Artifacts.Resume;

public sealed class ResumeManager
{
    private readonly string _stateDirectory;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<ResumeManager> _logger;
    
    public ResumeManager(
        IOptions<TransferOptions> options,
        IFileSystem fileSystem,
        ILogger<ResumeManager> logger)
    {
        _stateDirectory = options.Value.ResumeStateDirectory;
        _fileSystem = fileSystem;
        _logger = logger;
    }
    
    public async Task<ResumeState?> GetResumeStateAsync(
        string transferId,
        CancellationToken ct = default)
    {
        var statePath = GetStatePath(transferId);
        
        if (!_fileSystem.File.Exists(statePath))
            return null;
        
        var json = await _fileSystem.File.ReadAllTextAsync(statePath, ct);
        return JsonSerializer.Deserialize<ResumeState>(json);
    }
    
    public async Task SaveResumeStateAsync(
        string transferId,
        ResumeState state,
        CancellationToken ct = default)
    {
        var statePath = GetStatePath(transferId);
        _fileSystem.Directory.CreateDirectory(_stateDirectory);
        
        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        await _fileSystem.File.WriteAllTextAsync(statePath, json, ct);
    }
    
    public void ClearResumeState(string transferId)
    {
        var statePath = GetStatePath(transferId);
        if (_fileSystem.File.Exists(statePath))
            _fileSystem.File.Delete(statePath);
    }
    
    private string GetStatePath(string transferId) =>
        Path.Combine(_stateDirectory, $"{transferId}.resume.json");
}

public sealed record ResumeState
{
    public required string TransferId { get; init; }
    public required string SourcePath { get; init; }
    public required string DestinationPath { get; init; }
    public required long BytesTransferred { get; init; }
    public required long TotalBytes { get; init; }
    public required string PartialChecksum { get; init; }
    public required DateTimeOffset LastUpdated { get; init; }
    public IReadOnlyList<string> CompletedFiles { get; init; } = [];
}

// src/Acode.Infrastructure/Compute/Artifacts/Local/LocalUploader.cs
namespace Acode.Infrastructure.Compute.Artifacts.Local;

public sealed class LocalUploader
{
    private readonly IFileSystem _fileSystem;
    private readonly IChecksumCalculator _checksum;
    
    public LocalUploader(IFileSystem fileSystem, IChecksumCalculator checksum)
    {
        _fileSystem = fileSystem;
        _checksum = checksum;
    }
    
    public async Task<TransferredFile> UploadAsync(
        string localPath,
        string remotePath,
        UploadOptions options,
        CancellationToken ct = default)
    {
        // For local targets, "upload" is just a copy
        _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(remotePath)!);
        _fileSystem.File.Copy(localPath, remotePath, options.OverwriteExisting);
        
        if (options.PreserveTimestamps)
        {
            var info = new FileInfo(localPath);
            File.SetLastWriteTimeUtc(remotePath, info.LastWriteTimeUtc);
        }
        
        var size = new FileInfo(remotePath).Length;
        var checksum = await _checksum.CalculateAsync(remotePath, ct);
        
        return new TransferredFile(localPath, remotePath, size, checksum, DateTimeOffset.UtcNow);
    }
}

// src/Acode.Infrastructure/Compute/Artifacts/Local/LocalDownloader.cs
namespace Acode.Infrastructure.Compute.Artifacts.Local;

public sealed class LocalDownloader
{
    private readonly IFileSystem _fileSystem;
    private readonly IChecksumCalculator _checksum;
    
    public LocalDownloader(IFileSystem fileSystem, IChecksumCalculator checksum)
    {
        _fileSystem = fileSystem;
        _checksum = checksum;
    }
    
    public async Task<TransferredFile> DownloadAsync(
        string remotePath,
        string localPath,
        DownloadOptions options,
        CancellationToken ct = default)
    {
        // For local targets, "download" is just a copy
        _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
        
        var tempPath = localPath + ".tmp";
        _fileSystem.File.Copy(remotePath, tempPath, overwrite: true);
        
        // Atomic move
        if (_fileSystem.File.Exists(localPath))
            _fileSystem.File.Delete(localPath);
        _fileSystem.File.Move(tempPath, localPath);
        
        var size = new FileInfo(localPath).Length;
        var checksum = await _checksum.CalculateAsync(localPath, ct);
        
        return new TransferredFile(remotePath, localPath, size, checksum, DateTimeOffset.UtcNow);
    }
}
```

---

## Implementation Checklist

- [ ] Create TransferProgress and TransferResult records
- [ ] Define UploadOptions and DownloadOptions
- [ ] Implement TransferredFile and TransferError records
- [ ] Create domain events for transfer lifecycle
- [ ] Implement IArtifactTransfer interface
- [ ] Build ChecksumCalculator with SHA256
- [ ] Create StreamingTransfer for chunked I/O
- [ ] Implement BatchTransfer with parallelism
- [ ] Build ResumeManager for interrupted transfers
- [ ] Implement LocalUploader and LocalDownloader
- [ ] Write unit tests for all components (TDD)
- [ ] Test large file transfers (>1GB)
- [ ] Test resume functionality
- [ ] Verify checksum calculation
- [ ] Test batch parallel transfers
- [ ] Test error handling and partial failures

---

## Rollout Plan

1. **Phase 1**: Domain models (progress, result, options)
2. **Phase 2**: Application interfaces
3. **Phase 3**: ChecksumCalculator and StreamingTransfer
4. **Phase 4**: Local upload/download implementations
5. **Phase 5**: ResumeManager for interrupted transfers
6. **Phase 6**: BatchTransfer with parallelism
7. **Phase 7**: Integration with compute targets
8. **Phase 8**: Integration testing with large files

---

**End of Task 029.c Specification**