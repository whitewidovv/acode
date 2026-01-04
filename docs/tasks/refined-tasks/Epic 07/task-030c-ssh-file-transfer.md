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