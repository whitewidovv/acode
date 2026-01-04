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