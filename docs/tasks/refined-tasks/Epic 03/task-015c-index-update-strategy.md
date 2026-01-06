# Task 015.c: Index Update Strategy

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 015 (Indexing v1)  

---

## Description

Task 015.c defines the index update strategy. The index must stay current as files change. Updates must be efficient. Full rebuilds are too slow for routine use.

Incremental updates process only changed files. Detect what changed. Update only those entries. This is much faster than rebuilding.

Change detection uses file timestamps. Compare current mtime to indexed mtime. Different means changed. This is simple and efficient.

Multiple update triggers are supported. Manual updates via CLI. Updates on startup. Updates before search. Updates after agent writes. Each has its use case.

Update batching groups changes together. Many small files changed? Update them together. Batching reduces overhead.

Conflict handling addresses concurrent access. The agent might be writing while index updates. Updates must handle this gracefully.

Staleness tolerance is configurable. How old can the index be before mandatory update? Some use cases need fresher data than others.

Update progress is reported. Long updates show progress. Users know the system is working. Cancellation is supported.

Failure handling ensures consistency. If update fails partway, index is still valid. Either all changes apply or none. Atomic updates prevent corruption.

### Business Value

An index is only valuable if it reflects the current state of the codebase. As developers and the agent itself make changes to files, the index becomes stale. Without an efficient update strategy, users would be forced to choose between slow full rebuilds after every change or working with outdated search results that miss recent modifications. The index update strategy solves this by enabling fast, incremental updates that keep the index fresh without the cost of full rebuilds.

The business value is particularly pronounced in agent-assisted workflows. When the agent modifies files, those changes should immediately be searchable for subsequent operations. Without post-write update triggers, the agent could search for code it just wrote and not find it, leading to confusion and incorrect behavior. Automatic update triggers ensure the agent always works with current information.

Furthermore, the update strategy directly impacts user experience. Progress reporting and cancellation support ensure that users understand what the system is doing during long operations. Atomic updates guarantee that an interrupted update doesn't corrupt the index. These reliability features build trust and enable confident use of the indexing system in production workflows.

### ROI Analysis

**Cost of Implementation:**
- Development: 120 hours × $150/hr = $18,000
- Testing and validation: 40 hours × $150/hr = $6,000
- Documentation and training: 15 hours × $100/hr = $1,500
- **Total Investment: $25,500**

**Annual Savings:**

| Category | Without Update Strategy | With Update Strategy | Annual Savings |
|----------|------------------------|---------------------|----------------|
| Full rebuild wait time | 45 sec × 50 rebuilds/day × 250 days × $1.25/min | 2 sec × 200 updates/day × 250 days × $1.25/min | $104,375 |
| Stale search result debugging | 30 min × 2 incidents/day × 250 days × $75/hr | Eliminated | $18,750 |
| Agent workflow interruptions | 15 min × 5 incidents/day × 250 days × $75/hr | Eliminated | $23,437 |
| Index corruption recovery | 2 hr × 20 incidents/year × $200/hr | 0.5 hr × 2 incidents/year × $200/hr | $7,800 |
| **Total Annual Savings** | | | **$154,362** |

**ROI Calculation:**
- First year ROI: ($154,362 - $25,500) / $25,500 = **505%**
- Payback period: $25,500 / ($154,362 / 12) = **1.98 months**
- 3-year NPV (8% discount): **$372,486**

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        Index Update Strategy Architecture                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────────┐ │
│  │                          UPDATE TRIGGERS                                 │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │ │
│  │  │   Startup    │  │  Pre-Search  │  │  Post-Write  │  │   Manual     │ │ │
│  │  │   Trigger    │  │   Trigger    │  │   Trigger    │  │   CLI cmd    │ │ │
│  │  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘ │ │
│  │         │                 │                 │                 │         │ │
│  └─────────┼─────────────────┼─────────────────┼─────────────────┼─────────┘ │
│            └────────────┬────┴─────────────────┴─────┬───────────┘           │
│                         ▼                            ▼                       │
│  ┌─────────────────────────────────────────────────────────────────────────┐ │
│  │                     UPDATE TRIGGER MANAGER                               │ │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │ │
│  │  │  Debounce   │  │   Queue     │  │  Serialize  │  │  Staleness  │     │ │
│  │  │   500ms     │  │  Updates    │  │  Execution  │  │   Check     │     │ │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘     │ │
│  └──────────────────────────────┬──────────────────────────────────────────┘ │
│                                 ▼                                            │
│  ┌─────────────────────────────────────────────────────────────────────────┐ │
│  │                       CHANGE DETECTION                                   │ │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐          │ │
│  │  │  File System    │  │  mtime Compare  │  │  Change Set     │          │ │
│  │  │  Scanner        │──▶│  vs Indexed    │──▶│  Builder        │          │ │
│  │  │  (batch 500)    │  │  Timestamps     │  │  (new/mod/del)  │          │ │
│  │  └─────────────────┘  └─────────────────┘  └────────┬────────┘          │ │
│  └─────────────────────────────────────────────────────┼────────────────────┘ │
│                                                        ▼                     │
│  ┌─────────────────────────────────────────────────────────────────────────┐ │
│  │                     INCREMENTAL UPDATER                                  │ │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐          │ │
│  │  │  Batch Files    │  │  Read & Parse   │  │  Index Entry    │          │ │
│  │  │  (100 at time)  │──▶│  Changed Files  │──▶│  Update/Add/Del │          │ │
│  │  └─────────────────┘  └─────────────────┘  └────────┬────────┘          │ │
│  └─────────────────────────────────────────────────────┼────────────────────┘ │
│                                                        ▼                     │
│  ┌─────────────────────────────────────────────────────────────────────────┐ │
│  │                     ATOMIC WRITE LAYER                                   │ │
│  │  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐  ┌───────────┐ │ │
│  │  │ Lock Manager  │  │ Temp File     │  │ Checkpoint    │  │ Atomic    │ │ │
│  │  │ (exclusive)   │──▶│ Writer        │──▶│ Progress      │──▶│ Rename    │ │ │
│  │  └───────────────┘  └───────────────┘  └───────────────┘  └─────┬─────┘ │ │
│  └─────────────────────────────────────────────────────────────────┼───────┘ │
│                                                                    ▼         │
│  ┌─────────────────────────────────────────────────────────────────────────┐ │
│  │                       SQLite FTS5 INDEX                                  │ │
│  │  ┌──────────────────────────────────────────────────────────────────┐   │ │
│  │  │  code_index (id, path, content, mtime, hash, ...)                │   │ │
│  │  └──────────────────────────────────────────────────────────────────┘   │ │
│  │  ┌──────────────────────────────────────────────────────────────────┐   │ │
│  │  │  index_metadata (last_update, staleness_age, file_count, ...)    │   │ │
│  │  └──────────────────────────────────────────────────────────────────┘   │ │
│  └─────────────────────────────────────────────────────────────────────────┘ │
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────────┐ │
│  │                        SAFETY SYSTEMS                                    │ │
│  │  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐  ┌───────────┐ │ │
│  │  │ Path          │  │ Resource      │  │ TOCTOU        │  │ Rollback  │ │ │
│  │  │ Validator     │  │ Limiter       │  │ Protection    │  │ Manager   │ │ │
│  │  └───────────────┘  └───────────────┘  └───────────────┘  └───────────┘ │ │
│  └─────────────────────────────────────────────────────────────────────────┘ │
│                                                                               │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Trade-offs and Design Decisions

| Decision | Options Considered | Choice Made | Rationale |
|----------|-------------------|-------------|-----------|
| **Change detection method** | 1) mtime comparison 2) Content hashing 3) File system watchers 4) Git diff | mtime comparison with hash verification | mtime is fast and reliable; hash confirms changes; watchers add complexity; Git not always available |
| **Update granularity** | 1) Single file 2) Batch 3) Entire workspace | Batch (100 files) | Balances memory usage with performance; single file too slow; workspace defeats incremental purpose |
| **Atomicity approach** | 1) SQLite transactions 2) Temp file + rename 3) Copy-on-write 4) Journal-based | Temp file + atomic rename | Works across file systems; survives crashes; SQLite handles its own transactions |
| **Staleness tracking** | 1) Last update timestamp 2) File count changes 3) Git commit hash 4) Directory fingerprint | Timestamp + file count | Simple and reliable; Git not always available; fingerprint expensive to compute |
| **Lock strategy** | 1) File-based lock 2) Database lock 3) Process mutex 4) Advisory lock | File-based with timeout and stale detection | Portable across OS; survives crashes; easy to diagnose; timeout prevents deadlock |
| **Progress reporting** | 1) Percentage only 2) File counts 3) ETA 4) Current file | Percentage + count + ETA | Users need multiple signals; ETA helps expectation setting; current file aids debugging |

**Accepted Trade-offs:**
- **mtime resolution:** Some file systems have 1-second mtime resolution, potentially missing rapid file changes within same second. Accept this as edge case; hash verification catches if critical.
- **Lock contention:** File-based locks block concurrent access. Accept serial execution for correctness; debouncing reduces contention.
- **Memory during batch:** Holding 100 files in memory requires ~50MB peak. Accept this as reasonable for modern systems; configurable for constrained environments.

### Scope

1. **Change Detection** - File modification time (mtime) based detection of new, modified, and deleted files since last index update
2. **Incremental Updater** - Efficient update mechanism that processes only changed files, preserving unchanged index entries
3. **Update Triggers** - Configurable automatic triggers for startup, pre-search, post-write, and staleness-based updates
4. **Batching System** - Grouping of file operations for efficient processing with configurable batch sizes and checkpoints
5. **Atomicity Guarantees** - Transaction-style updates with rollback on failure to prevent index corruption

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Index Service | Primary | Receives update commands and coordinates with storage layer |
| File System | Provider | Scanned for file changes via mtime comparison |
| Agent Write Service | Trigger | Triggers post-write updates when agent modifies files |
| Search Service | Trigger | Triggers pre-search updates when staleness threshold exceeded |
| CLI Commands | Interface | Exposes manual update and rebuild commands |
| Configuration Service | Provider | Supplies update trigger settings and threshold values |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| File deleted during scan | Stale data or missing entry | Handle file-not-found gracefully, skip missing files |
| Disk full during update | Update cannot complete | Rollback transaction, report clear error with space needed |
| File locked by another process | Cannot read file for indexing | Retry with backoff, eventually skip with warning |
| Concurrent update attempts | Potential corruption or deadlock | Queue concurrent updates, serialize execution |
| Power loss during update | Partially written index | Atomic write with temp file and rename; recovery on startup |
| Timezone change | mtime comparisons incorrect | Use UTC timestamps internally, revalidate on timezone detection |

### Assumptions

1. File modification time (mtime) is reliable for detecting changes on all supported operating systems (Windows, macOS, Linux)
2. The file system maintains accurate and consistent mtime values across file operations with at least 1-second resolution
3. Most update operations will be incremental with less than 5% of files changed between updates
4. Full rebuilds are acceptable for initial index creation or corruption recovery, but should be rare in normal operation
5. Batching provides significant performance improvement (3-5×) over individual file processing due to reduced syscall overhead
6. Staleness thresholds are configurable to meet different workflow requirements, ranging from seconds (real-time) to hours (batch processing)
7. Users prefer background updates that don't block their primary workflow, with progress visible but non-intrusive
8. Atomic update guarantees are achievable using file system rename operations on all supported platforms
9. The workspace file system supports exclusive file locking for concurrent access control
10. File system operations (stat, read, write) are the primary performance bottleneck, not CPU processing
11. Memory is sufficient to hold batch metadata (500 files × ~200 bytes = ~100KB) plus file content buffer (~50MB peak)
12. The index database (SQLite) can handle concurrent read and exclusive write operations without corruption
13. Users will not manually modify the index database or lock files; these are managed exclusively by the system
14. Network file systems (NFS, SMB) may have unreliable mtime and locking; local file systems are the primary target
15. The operating system's process ID is reliable for stale lock detection within a single machine
16. Timezone changes are detectable via system APIs and can be handled by converting all timestamps to UTC internally
17. File content can be fully read into memory for indexing; streaming is not required for individual file processing
18. Users accept that very rapid changes (multiple saves within 1 second) may not be individually detected due to mtime resolution
19. The .agent directory is writable and can store temporary files, lock files, and checkpoint data
20. CancellationToken is the standard mechanism for cooperative cancellation in all async operations

### Security Considerations

1. **File Access Verification** - Update process must verify file access permissions before reading, avoiding permission-denied errors
2. **Temporary File Security** - Temporary files used for atomic updates must be created with restricted permissions
3. **Lock File Integrity** - Lock files for concurrent access control must be protected against tampering
4. **Path Validation** - Changed file paths must be validated to prevent directory traversal attacks
5. **Resource Limits** - Update operations must respect resource limits to prevent denial of service during large updates

#### Threat 1: Race Condition on File Read (TOCTOU)

**Attack Vector:** Attacker replaces file between mtime check and content read, injecting malicious content.

**Mitigation:**

```csharp
namespace Acode.Infrastructure.Index.Update;

/// <summary>
/// Validates file state consistency between detection and read.
/// Prevents TOCTOU (Time of Check to Time of Use) attacks.
/// </summary>
public sealed class SafeFileReader
{
    private readonly ILogger<SafeFileReader> _logger;
    private const int MaxRetries = 3;

    public SafeFileReader(ILogger<SafeFileReader> logger)
    {
        _logger = logger;
    }

    public async Task<FileReadResult> ReadWithConsistencyCheckAsync(
        string filePath,
        FileInfo detectedInfo,
        CancellationToken ct)
    {
        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                // Capture state before read
                var preReadInfo = new FileInfo(filePath);
                if (!preReadInfo.Exists)
                {
                    return FileReadResult.FileDeleted(filePath);
                }

                // Verify mtime matches detection
                if (preReadInfo.LastWriteTimeUtc != detectedInfo.LastWriteTimeUtc)
                {
                    _logger.LogWarning(
                        "File changed between detection and read: {Path}",
                        filePath);
                    
                    // Re-detect and retry
                    await Task.Delay(100 * (attempt + 1), ct);
                    continue;
                }

                // Read content
                var content = await File.ReadAllTextAsync(filePath, ct);

                // Verify state after read
                var postReadInfo = new FileInfo(filePath);
                if (postReadInfo.LastWriteTimeUtc != preReadInfo.LastWriteTimeUtc)
                {
                    _logger.LogWarning(
                        "File changed during read: {Path}",
                        filePath);
                    continue;
                }

                return FileReadResult.Success(filePath, content, postReadInfo.LastWriteTimeUtc);
            }
            catch (IOException ex) when (ex.HResult == -2147024864) // File in use
            {
                _logger.LogDebug(
                    "File locked, retrying: {Path}",
                    filePath);
                await Task.Delay(100 * (attempt + 1), ct);
            }
        }

        return FileReadResult.InconsistentState(filePath);
    }
}

public record FileReadResult(
    string FilePath,
    bool IsSuccess,
    string? Content,
    DateTime? LastModified,
    string? ErrorReason)
{
    public static FileReadResult Success(string path, string content, DateTime modified) =>
        new(path, true, content, modified, null);

    public static FileReadResult FileDeleted(string path) =>
        new(path, false, null, null, "File deleted");

    public static FileReadResult InconsistentState(string path) =>
        new(path, false, null, null, "File state inconsistent");
}
```

#### Threat 2: Temporary File Hijacking

**Attack Vector:** Attacker intercepts temporary files used for atomic writes, replacing content before rename.

**Mitigation:**

```csharp
namespace Acode.Infrastructure.Index.Update;

/// <summary>
/// Manages secure temporary files for atomic updates.
/// Creates temp files with restricted permissions in secure location.
/// </summary>
public sealed class SecureTempFileManager : IDisposable
{
    private readonly string _secureDirectory;
    private readonly List<string> _tempFiles = new();
    private readonly object _lock = new();
    private bool _disposed;

    public SecureTempFileManager(string workspacePath)
    {
        // Create temp directory inside workspace with restricted access
        _secureDirectory = Path.Combine(
            workspacePath,
            ".agent",
            "temp",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_secureDirectory);
        SetRestrictedPermissions(_secureDirectory);
    }

    public string CreateTempFile(string extension = ".tmp")
    {
        ThrowIfDisposed();

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var tempPath = Path.Combine(_secureDirectory, fileName);

        // Create file with exclusive access
        using var fs = new FileStream(
            tempPath,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None);

        lock (_lock)
        {
            _tempFiles.Add(tempPath);
        }

        return tempPath;
    }

    public async Task AtomicMoveAsync(
        string tempPath,
        string targetPath,
        CancellationToken ct)
    {
        ThrowIfDisposed();

        // Verify temp file is ours
        lock (_lock)
        {
            if (!_tempFiles.Contains(tempPath))
            {
                throw new SecurityException(
                    $"Attempted to move unmanaged temp file: {tempPath}");
            }
        }

        // Verify temp file hasn't been tampered with
        var tempInfo = new FileInfo(tempPath);
        if (!tempInfo.Exists)
        {
            throw new SecurityException(
                $"Temp file missing or deleted: {tempPath}");
        }

        // Ensure parent directory exists
        var targetDir = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        // Atomic rename (same filesystem required)
        File.Move(tempPath, targetPath, overwrite: true);

        lock (_lock)
        {
            _tempFiles.Remove(tempPath);
        }
    }

    private void SetRestrictedPermissions(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            // Windows: Set DACL to owner-only
            var info = new DirectoryInfo(path);
            var security = info.GetAccessControl();
            security.SetAccessRuleProtection(true, false);
            
            var identity = WindowsIdentity.GetCurrent();
            security.AddAccessRule(new FileSystemAccessRule(
                identity.User!,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow));
            
            info.SetAccessControl(security);
        }
        else
        {
            // Unix: chmod 700
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Clean up all temp files
        lock (_lock)
        {
            foreach (var file in _tempFiles)
            {
                try { File.Delete(file); } catch { }
            }
            _tempFiles.Clear();
        }

        // Remove temp directory
        try { Directory.Delete(_secureDirectory, recursive: true); } catch { }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SecureTempFileManager));
        }
    }
}
```

#### Threat 3: Lock File Denial of Service

**Attack Vector:** Attacker creates or holds lock file indefinitely, preventing legitimate updates.

**Mitigation:**

```csharp
namespace Acode.Infrastructure.Index.Update;

/// <summary>
/// Manages index lock with expiration to prevent indefinite blocking.
/// Uses process ID tracking to detect stale locks.
/// </summary>
public sealed class IndexLockManager : IAsyncDisposable
{
    private readonly string _lockPath;
    private readonly TimeSpan _lockTimeout;
    private readonly ILogger<IndexLockManager> _logger;
    private FileStream? _lockStream;
    private bool _disposed;

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan StaleLockAge = TimeSpan.FromMinutes(10);

    public IndexLockManager(
        string workspacePath,
        ILogger<IndexLockManager> logger,
        TimeSpan? lockTimeout = null)
    {
        _lockPath = Path.Combine(workspacePath, ".agent", "index.lock");
        _lockTimeout = lockTimeout ?? DefaultTimeout;
        _logger = logger;
    }

    public async Task<LockAcquisitionResult> AcquireAsync(CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + _lockTimeout;

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            // Check for stale lock
            if (File.Exists(_lockPath))
            {
                if (await TryCleanStaleLockAsync())
                {
                    _logger.LogInformation("Cleaned stale lock file");
                }
            }

            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(_lockPath)!);

                // Try to create lock file with exclusive access
                _lockStream = new FileStream(
                    _lockPath,
                    FileMode.CreateNew,
                    FileAccess.ReadWrite,
                    FileShare.None);

                // Write process info for stale detection
                await WriteLockInfoAsync(_lockStream);

                _logger.LogDebug("Acquired index lock: {Path}", _lockPath);
                return LockAcquisitionResult.Acquired();
            }
            catch (IOException)
            {
                // Lock held by another process, wait and retry
                await Task.Delay(500, ct);
            }
        }

        return LockAcquisitionResult.Timeout(_lockTimeout);
    }

    private async Task<bool> TryCleanStaleLockAsync()
    {
        try
        {
            var lockInfo = new FileInfo(_lockPath);
            if (!lockInfo.Exists) return false;

            // Check age
            if (DateTime.UtcNow - lockInfo.LastWriteTimeUtc > StaleLockAge)
            {
                // Read lock info to verify process is dead
                var content = await File.ReadAllTextAsync(_lockPath);
                var parts = content.Split('|');
                if (parts.Length >= 2 && int.TryParse(parts[0], out var pid))
                {
                    try
                    {
                        var process = Process.GetProcessById(pid);
                        // Process exists, check if it's the same one
                        if (process.StartTime.ToString("O") != parts[1])
                        {
                            // Different process with recycled PID
                            File.Delete(_lockPath);
                            return true;
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Process doesn't exist, safe to delete
                        File.Delete(_lockPath);
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check stale lock");
        }

        return false;
    }

    private async Task WriteLockInfoAsync(FileStream stream)
    {
        var process = Process.GetCurrentProcess();
        var info = $"{process.Id}|{process.StartTime:O}|{Environment.MachineName}";
        var bytes = Encoding.UTF8.GetBytes(info);
        await stream.WriteAsync(bytes);
        await stream.FlushAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_lockStream != null)
        {
            await _lockStream.DisposeAsync();
            try { File.Delete(_lockPath); } catch { }
        }
    }
}

public record LockAcquisitionResult(bool IsAcquired, TimeSpan? TimeoutDuration)
{
    public static LockAcquisitionResult Acquired() => new(true, null);
    public static LockAcquisitionResult Timeout(TimeSpan duration) => new(false, duration);
}
```

#### Threat 4: Path Traversal in Changed Files

**Attack Vector:** Malicious symlink or file path tricks system into indexing files outside workspace.

**Mitigation:**

```csharp
namespace Acode.Infrastructure.Index.Update;

/// <summary>
/// Validates that changed file paths are within the workspace boundary.
/// Detects and blocks path traversal attempts via symlinks or relative paths.
/// </summary>
public sealed class UpdatePathValidator
{
    private readonly string _workspaceRoot;
    private readonly bool _followSymlinks;
    private readonly ILogger<UpdatePathValidator> _logger;

    public UpdatePathValidator(
        string workspaceRoot,
        bool followSymlinks,
        ILogger<UpdatePathValidator> logger)
    {
        _workspaceRoot = Path.GetFullPath(workspaceRoot);
        _followSymlinks = followSymlinks;
        _logger = logger;
    }

    public PathValidationResult ValidateChangedPath(string filePath)
    {
        try
        {
            // Resolve to absolute path
            var absolutePath = Path.GetFullPath(filePath);

            // Check within workspace
            if (!absolutePath.StartsWith(_workspaceRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Path outside workspace rejected: {Path}",
                    filePath);
                return PathValidationResult.OutsideWorkspace;
            }

            // Check for symlink escape
            if (!_followSymlinks)
            {
                var info = new FileInfo(absolutePath);
                if (info.LinkTarget != null)
                {
                    var linkTarget = Path.GetFullPath(info.LinkTarget);
                    if (!linkTarget.StartsWith(_workspaceRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning(
                            "Symlink escaping workspace rejected: {Path} -> {Target}",
                            filePath, linkTarget);
                        return PathValidationResult.SymlinkEscape;
                    }
                }
            }

            // Check for suspicious patterns
            if (ContainsSuspiciousPatterns(absolutePath))
            {
                _logger.LogWarning(
                    "Suspicious path pattern rejected: {Path}",
                    filePath);
                return PathValidationResult.SuspiciousPattern;
            }

            return PathValidationResult.Valid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Path validation failed: {Path}", filePath);
            return PathValidationResult.ValidationError;
        }
    }

    private bool ContainsSuspiciousPatterns(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        
        foreach (var segment in segments)
        {
            // Check for null bytes
            if (segment.Contains('\0'))
                return true;

            // Check for excessive dots (beyond ..)
            if (segment.Count(c => c == '.') > 2)
                return true;

            // Check for control characters
            if (segment.Any(c => char.IsControl(c)))
                return true;
        }

        return false;
    }
}

public enum PathValidationResult
{
    Valid,
    OutsideWorkspace,
    SymlinkEscape,
    SuspiciousPattern,
    ValidationError
}
```

#### Threat 5: Resource Exhaustion During Update

**Attack Vector:** Attacker triggers update on repository with millions of tiny files, exhausting memory/CPU.

**Mitigation:**

```csharp
namespace Acode.Infrastructure.Index.Update;

/// <summary>
/// Enforces resource limits during index update operations.
/// Prevents denial of service from extreme file counts or sizes.
/// </summary>
public sealed class UpdateResourceLimiter
{
    private readonly UpdateLimitOptions _limits;
    private readonly ILogger<UpdateResourceLimiter> _logger;

    private int _filesScanned;
    private int _filesUpdated;
    private long _totalBytes;
    private readonly Stopwatch _elapsed = new();

    public UpdateResourceLimiter(
        UpdateLimitOptions limits,
        ILogger<UpdateResourceLimiter> logger)
    {
        _limits = limits;
        _logger = logger;
    }

    public void StartUpdate()
    {
        _filesScanned = 0;
        _filesUpdated = 0;
        _totalBytes = 0;
        _elapsed.Restart();
    }

    public void RecordFileScan()
    {
        var count = Interlocked.Increment(ref _filesScanned);
        
        if (count > _limits.MaxFilesToScan)
        {
            throw new UpdateLimitExceededException(
                $"Scan limit exceeded: {count} > {_limits.MaxFilesToScan} files");
        }
    }

    public void RecordFileUpdate(long fileSize)
    {
        var count = Interlocked.Increment(ref _filesUpdated);
        var bytes = Interlocked.Add(ref _totalBytes, fileSize);

        if (count > _limits.MaxFilesToUpdate)
        {
            throw new UpdateLimitExceededException(
                $"Update limit exceeded: {count} > {_limits.MaxFilesToUpdate} files");
        }

        if (bytes > _limits.MaxBytesToProcess)
        {
            throw new UpdateLimitExceededException(
                $"Size limit exceeded: {bytes} > {_limits.MaxBytesToProcess} bytes");
        }

        if (_elapsed.Elapsed > _limits.MaxUpdateDuration)
        {
            throw new UpdateLimitExceededException(
                $"Time limit exceeded: {_elapsed.Elapsed} > {_limits.MaxUpdateDuration}");
        }
    }

    public void CheckMemoryPressure()
    {
        var gcInfo = GC.GetGCMemoryInfo();
        var usedPercentage = (double)gcInfo.MemoryLoadBytes / gcInfo.TotalAvailableMemoryBytes;

        if (usedPercentage > _limits.MaxMemoryUsagePercent)
        {
            _logger.LogWarning(
                "Memory pressure detected: {Used:P0}. Forcing GC and pausing.",
                usedPercentage);
            
            GC.Collect(2, GCCollectionMode.Aggressive, true);
            Thread.Sleep(100);

            // Recheck
            gcInfo = GC.GetGCMemoryInfo();
            usedPercentage = (double)gcInfo.MemoryLoadBytes / gcInfo.TotalAvailableMemoryBytes;

            if (usedPercentage > _limits.MaxMemoryUsagePercent)
            {
                throw new UpdateLimitExceededException(
                    $"Memory limit exceeded: {usedPercentage:P0} > {_limits.MaxMemoryUsagePercent:P0}");
            }
        }
    }

    public UpdateStatistics GetStatistics() => new(
        _filesScanned,
        _filesUpdated,
        _totalBytes,
        _elapsed.Elapsed);
}

public sealed class UpdateLimitOptions
{
    public int MaxFilesToScan { get; set; } = 100_000;
    public int MaxFilesToUpdate { get; set; } = 10_000;
    public long MaxBytesToProcess { get; set; } = 500 * 1024 * 1024; // 500 MB
    public TimeSpan MaxUpdateDuration { get; set; } = TimeSpan.FromMinutes(10);
    public double MaxMemoryUsagePercent { get; set; } = 0.80;
}

public record UpdateStatistics(int FilesScanned, int FilesUpdated, long TotalBytes, TimeSpan Duration);

public class UpdateLimitExceededException : Exception
{
    public UpdateLimitExceededException(string message) : base(message) { }
}

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Incremental Update | An index update operation that processes only files detected as changed (new, modified, or deleted) since the last update, preserving unchanged index entries to minimize processing time and I/O |
| Full Rebuild | A complete reconstruction of the entire index from scratch, scanning all files in the workspace regardless of whether they have changed; used for initial index creation or corruption recovery |
| Change Detection | The process of comparing current file system state to indexed state to identify files that are new, modified, or deleted since the last index update |
| mtime (Modification Time) | A file system metadata field recording when a file's content was last modified; used as the primary signal for detecting file changes |
| Staleness | A measure of how outdated the index is relative to the current file system state, typically expressed as time since last successful update |
| Staleness Threshold | A configurable duration after which the index is considered stale and requires update before serving search requests |
| Update Trigger | An event or condition that initiates an index update operation, including manual CLI commands, application startup, pre-search checks, and post-write hooks |
| Batching | The technique of grouping multiple file operations (reads, index writes) into fixed-size batches to optimize I/O performance and memory usage |
| Concurrent Access | Multiple processes or threads attempting to access the index simultaneously; requires locking to prevent data corruption |
| Atomic Update | An update operation where all changes are applied together or none are applied, ensuring the index is never left in a partially updated inconsistent state |
| Checkpoint | A saved point of progress during a long-running update operation, enabling resumption from the checkpoint if the operation is interrupted |
| Delta | The set of changes (additions, modifications, deletions) between two index states, representing what needs to be applied in an incremental update |
| File System Scan | The process of enumerating and inspecting files in the workspace to gather current state information for change detection |
| Update Queue | A queue of pending update requests that are processed serially to prevent concurrent modification conflicts |
| Background Update | An update operation that runs asynchronously without blocking the user's primary workflow, typically at lower priority |
| Foreground Update | An update operation that blocks user interaction until complete, typically used for manual CLI commands where the user expects immediate results |
| Debouncing | The technique of coalescing multiple rapid trigger events (e.g., many file saves) into a single delayed update to avoid redundant processing |
| Lock File | A file used to coordinate exclusive access to the index, preventing concurrent updates that could cause corruption |
| Rollback | The process of reverting to a previous consistent index state when an update operation fails, ensuring index integrity |
| TOCTOU | Time-of-Check-to-Time-of-Use; a race condition where file state changes between when it's checked and when it's read, potentially introducing inconsistencies |

---

## Use Cases

### Use Case 1: Agent Continues Work After File Modifications

**Persona:** DevBot, an AI coding assistant modifying multiple files during a refactoring task

**Preconditions:**
- Agent has completed a refactoring that modified 15 files across 4 directories
- Index contains the previous version of all modified files
- Post-write trigger is enabled in configuration

**Scenario:**
DevBot completes writing the refactored code across multiple files. The post-write trigger detects that files have been modified. Within 2 seconds, an incremental update processes only the 15 changed files while leaving the other 5,000 indexed files untouched. DevBot immediately searches for all usages of the newly renamed method and finds them in the just-modified files.

**Before Update Strategy:**
- DevBot modifies 15 files, then searches for the new method name
- Search returns 0 results because index still has old content
- DevBot is confused, assumes refactoring failed, wastes 10 minutes investigating
- Manual rebuild takes 45 seconds, blocking all agent work
- Total delay: 10+ minutes of wasted effort

**After Update Strategy:**
- DevBot modifies 15 files, post-write trigger fires
- Incremental update processes 15 files in 1.5 seconds
- DevBot searches and immediately finds all new method usages
- Workflow continues without interruption
- Total delay: 1.5 seconds (transparent)

**Quantified Impact:**
- Updates per day: 150 (agent is very active)
- Time saved per update: 45 seconds (rebuild) vs 1.5 seconds (incremental)
- Daily time saved: 150 × 43.5 seconds = 108 minutes
- Annual developer time recovered: 108 min × 250 days = 450 hours
- Value at $75/hr: $33,750/year

---

### Use Case 2: Developer Resumes Work After Lunch Break

**Persona:** Alex, a developer returning to their project after a 90-minute break

**Preconditions:**
- Alex closed their IDE 90 minutes ago
- Team members committed 3 pull requests with 47 changed files
- Alex pulled latest changes, workspace has 47 modified files
- On-startup trigger is enabled with 3600-second staleness threshold

**Scenario:**
Alex opens the project. The on-startup trigger detects that the index was last updated 90 minutes ago (stale). Change detection scans the workspace and finds 47 files differ from indexed state. Incremental update processes these 47 files in 3 seconds. Alex searches for a function mentioned in the PR description and finds it immediately in the newly-indexed code.

**Before Update Strategy:**
- Alex pulls changes and opens project
- Searches for function mentioned in PR, gets 0 results
- Realizes index is stale, runs full rebuild (45 seconds)
- Finally finds the function after 2-minute delay
- Repeat this scenario 2× daily (morning and after lunch)

**After Update Strategy:**
- Alex pulls changes and opens project
- On-startup trigger runs incremental update (3 seconds, non-blocking)
- Searches and finds function immediately
- Zero workflow interruption

**Quantified Impact:**
- Stale index events per developer per day: 2
- Time lost per event: 2 minutes (searching + rebuilding)
- Team size: 8 developers
- Daily team time lost: 8 × 2 × 2 = 32 minutes
- Annual team time saved: 32 min × 250 days = 133 hours
- Value at $75/hr: $9,975/year

---

### Use Case 3: Pre-Search Update Prevents Empty Results

**Persona:** Jordan, a developer who disabled automatic updates for performance but needs fresh search results

**Preconditions:**
- Jordan configured on_startup and after_write triggers as false
- Staleness threshold is set to 7200 seconds (2 hours)
- before_search trigger is enabled
- Index is 3 hours stale with 89 changed files

**Scenario:**
Jordan initiates a search for "PaymentProcessor". The before_search trigger detects the index is beyond the staleness threshold. Incremental update runs, processing 89 changed files in 6 seconds. The search then executes, returning 12 results including 3 from recently-modified files. Jordan finds exactly what they need.

**Before Update Strategy:**
- Jordan searches for "PaymentProcessor"
- Gets 9 results (missing 3 from recent changes)
- Doesn't find the implementation they need
- Manually searches with grep, finds it in a recently-changed file
- Wonders why index missed it, runs manual rebuild
- Total time: 8 minutes of confusion and recovery

**After Update Strategy:**
- Jordan searches for "PaymentProcessor"
- Pre-search trigger updates index (6 seconds)
- Gets 12 results including all recent changes
- Finds implementation immediately
- Total time: 6 seconds (transparent)

**Quantified Impact:**
- Stale searches avoided per day: 5 (across team)
- Time lost per stale search: 8 minutes (confusion + recovery)
- Daily team time saved: 5 × 8 = 40 minutes
- Annual team time saved: 40 min × 250 days = 166 hours
- Value at $75/hr: $12,450/year

---

### Use Case 4: Large Repository Update with Progress Feedback

**Persona:** Robin, a developer working on a monorepo with 50,000 files

**Preconditions:**
- Robin's monorepo has 50,000 source files
- Last index update was 24 hours ago (weekend)
- 2,847 files were modified by CI/CD and team commits
- Robin runs manual `acode index update`

**Scenario:**
Robin runs the update command. Progress reporting shows "Scanning: 50,000 files" with a progress bar. Change detection completes in 8 seconds, reporting "Found 2,847 changed files". Update begins with "Updating: [=====>    ] 25% (712/2847) ETA: 18s". After 24 seconds, update completes with summary: "Updated 2,847 files in 24.3s (118 files/sec)". Robin can now search with confidence.

**Before Update Strategy:**
- Robin runs rebuild on 50,000-file repo
- No progress feedback, just spinning cursor
- After 5 minutes, Robin wonders if it's stuck
- Ctrl+C cancels, corrupting the index
- Must rebuild from scratch (another 5 minutes)
- Total time: 10+ minutes with uncertainty

**After Update Strategy:**
- Robin runs incremental update
- Progress shows exactly what's happening
- Only changed files are processed (2,847 vs 50,000)
- Clear ETA and completion feedback
- Total time: 24 seconds with confidence

**Quantified Impact:**
- Large updates per week: 2 (Monday + mid-week)
- Time saved per update: 5 minutes (full rebuild) vs 24 seconds (incremental)
- Weekly time saved: 9.2 minutes
- Annual time saved: 9.2 min × 52 weeks = 8 hours
- Value at $75/hr: $600/year

---

## Out of Scope

The following items are explicitly excluded from Task 015.c:

- **File system watching/inotify** - V1 uses polling-based change detection only; real-time file system event watching (inotify, FSEvents, ReadDirectoryChangesW) is deferred to future versions
- **Real-time continuous updates** - V1 uses discrete batch updates triggered at specific points; streaming/continuous synchronization is out of scope
- **Distributed/multi-machine updates** - V1 supports single-machine index only; cluster-aware index synchronization across multiple machines is excluded
- **Parallel file scanning** - V1 uses sequential single-threaded scanning; multi-threaded parallel directory traversal is deferred for simplicity
- **Content hash-based detection** - V1 uses mtime comparison only for change detection; SHA-256 content hashing for change verification is excluded (hash used only for integrity, not detection)
- **Streaming update protocol** - V1 batches all updates in memory; streaming protocols for very large change sets are excluded
- **Partial file updates** - V1 reindexes entire files when changed; sub-file granularity (e.g., only changed functions) is excluded
- **Cross-platform lock compatibility** - V1 lock files are not guaranteed to work across network file systems (NFS, SMB); local file system only
- **Index merge operations** - V1 does not support merging indexes from multiple sources; single linear update stream only
- **Pre-emptive update scheduling** - V1 does not predict when updates will be needed; reactive triggers only, no proactive scheduling

---

## Functional Requirements

### Change Detection

| ID | Requirement |
|----|-------------|
| FR-015c-01 | The system MUST detect modified files by comparing file mtime to indexed mtime |
| FR-015c-02 | The system MUST detect new files not present in the current index |
| FR-015c-03 | The system MUST detect deleted files no longer present in the file system |
| FR-015c-04 | The system MUST detect files with changed content (modified mtime) |
| FR-015c-05 | The system MUST handle timezone changes without false positive detections |

### Update Triggers

| ID | Requirement |
|----|-------------|
| FR-015c-06 | The system MUST support manual update invocation via CLI command |
| FR-015c-07 | The system MUST support optional automatic update on application startup |
| FR-015c-08 | The system MUST support optional automatic update before search if stale |
| FR-015c-09 | The system MUST support optional automatic update after agent writes files |
| FR-015c-10 | All update triggers MUST be configurable via .agent/config.yml |

### Incremental Update

| ID | Requirement |
|----|-------------|
| FR-015c-11 | The system MUST update only files detected as changed |
| FR-015c-12 | The system MUST skip unchanged files without re-processing |
| FR-015c-13 | The system MUST remove index entries for deleted files |
| FR-015c-14 | The system MUST add index entries for new files |
| FR-015c-15 | The system MUST preserve index entries for unchanged files |

### Full Rebuild

| ID | Requirement |
|----|-------------|
| FR-015c-16 | RebuildAsync() MUST perform a complete index rebuild from scratch |
| FR-015c-17 | Rebuild MUST clear all existing index entries before reindexing |
| FR-015c-18 | Rebuild MUST index all files matching the inclusion criteria |
| FR-015c-19 | Rebuild MUST report progress during the operation |

### Batching

| ID | Requirement |
|----|-------------|
| FR-015c-20 | The system MUST batch file system scans for efficient enumeration |
| FR-015c-21 | The system MUST batch index updates for efficient persistence |
| FR-015c-22 | Batch sizes MUST be configurable via settings |
| FR-015c-23 | The system MUST manage memory during large batches to prevent exhaustion |

### Staleness

| ID | Requirement |
|----|-------------|
| FR-015c-24 | The system MUST track the timestamp of the last successful index update |
| FR-015c-25 | The staleness threshold MUST be configurable in seconds |
| FR-015c-26 | The system MUST force update when staleness threshold is exceeded |
| FR-015c-27 | The system MUST report current staleness status via status command |

### Progress

| ID | Requirement |
|----|-------------|
| FR-015c-28 | The system MUST report file scan progress during detection phase |
| FR-015c-29 | The system MUST report index update progress during update phase |
| FR-015c-30 | The system MUST calculate and display estimated time to completion |
| FR-015c-31 | The system MUST support cancellation of in-progress updates |

### Atomicity

| ID | Requirement |
|----|-------------|
| FR-015c-32 | Index updates MUST be atomic - all changes apply or none |
| FR-015c-33 | The system MUST rollback to previous state on update failure |
| FR-015c-34 | The system MUST NOT leave the index in a partially updated state |
| FR-015c-35 | The system MUST support periodic checkpoints for long-running updates |

### Concurrency

| ID | Requirement |
|----|-------------|
| FR-015c-36 | The system MUST handle files changing during the scan operation |
| FR-015c-37 | The system MUST lock the index during write operations |
| FR-015c-38 | The system MUST allow read operations during index updates |
| FR-015c-39 | The system MUST queue concurrent update requests for serialized execution |

### Error Handling

| ID | Requirement |
|----|-------------|
| FR-015c-40 | The system MUST skip files with permission errors and continue update with warning |
| FR-015c-41 | The system MUST abort update on disk full errors with clear error message |
| FR-015c-42 | The system MUST retry transient errors (file locked) with exponential backoff |
| FR-015c-43 | The system MUST log all errors with structured error codes and context |
| FR-015c-44 | The system MUST rollback cleanly on any unrecoverable error |

### Configuration

| ID | Requirement |
|----|-------------|
| FR-015c-45 | The system MUST support configuring on_startup trigger (true/false) |
| FR-015c-46 | The system MUST support configuring before_search trigger (true/false) |
| FR-015c-47 | The system MUST support configuring after_write trigger (true/false) |
| FR-015c-48 | The system MUST support configuring stale_after_seconds threshold |
| FR-015c-49 | The system MUST support configuring scan_batch_size for file enumeration |
| FR-015c-50 | The system MUST support configuring index_batch_size for write operations |
| FR-015c-51 | The system MUST support configuring lock_timeout_seconds for concurrency |

### Logging and Metrics

| ID | Requirement |
|----|-------------|
| FR-015c-52 | The system MUST log update start event at Info level |
| FR-015c-53 | The system MUST log update completion event with duration at Info level |
| FR-015c-54 | The system MUST log individual file operations at Debug level |
| FR-015c-55 | The system MUST record files_scanned metric per update |
| FR-015c-56 | The system MUST record files_updated metric per update |
| FR-015c-57 | The system MUST record update_duration_ms metric per update |

### CLI Integration

| ID | Requirement |
|----|-------------|
| FR-015c-58 | The system MUST provide `acode index update` command for manual incremental update |
| FR-015c-59 | The system MUST provide `acode index rebuild` command for full rebuild |
| FR-015c-60 | The system MUST provide `acode index status` command for staleness check |
| FR-015c-61 | The system MUST support `--force` flag to bypass staleness check |
| FR-015c-62 | The system MUST support `--quiet` flag to suppress progress output |
| FR-015c-63 | The system MUST return exit code 0 on success, 1 on partial failure, 2 on complete failure |

---

## Non-Functional Requirements

### Performance

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015c-01 | Performance | Scanning 10,000 files for changes MUST complete in less than 5 seconds |
| NFR-015c-02 | Performance | Updating 100 changed files MUST complete in less than 2 seconds |
| NFR-015c-03 | Performance | Startup staleness check MUST complete in less than 500ms |

### Reliability

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015c-04 | Reliability | Index updates MUST be atomic with rollback on failure |
| NFR-015c-05 | Reliability | The system MUST recover from interrupted updates without corruption |
| NFR-015c-06 | Reliability | The system MUST degrade gracefully when update capacity is exceeded |

### Usability

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015c-07 | Usability | Progress display MUST clearly show current phase and completion percentage |
| NFR-015c-08 | Usability | Default update settings MUST work for common project sizes |
| NFR-015c-09 | Usability | Update activity MUST be logged at appropriate verbosity levels |

### Maintainability

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015c-10 | Maintainability | Change detection logic MUST be extensible for future hash-based detection |
| NFR-015c-11 | Maintainability | Update triggers MUST be pluggable for adding new trigger types |
| NFR-015c-12 | Maintainability | Batching parameters MUST be tunable without code changes |

### Observability

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015c-13 | Observability | Update duration and file counts MUST be recorded as metrics |
| NFR-015c-14 | Observability | Staleness age MUST be queryable for monitoring |
| NFR-015c-15 | Observability | Update failures MUST be logged with detailed error context |

---

## User Manual Documentation

### Overview

The index update strategy keeps the index current. Updates can be manual, automatic, or triggered.

### Manual Update

```bash
$ acode index update

Checking for changes...
  Scanned: 1,234 files
  Changed: 15 files
  New: 3 files
  Deleted: 2 files

Updating index...
  [====================] 100%

Index updated in 1.2s
```

### Automatic Updates

```yaml
# .agent/config.yml
index:
  update:
    # Update on startup
    on_startup: true
    
    # Update before search if stale
    before_search: true
    
    # Staleness threshold (seconds)
    stale_after_seconds: 300  # 5 minutes
    
    # Update after agent writes
    after_write: true
```

### Staleness

The index tracks when it was last updated:

```bash
$ acode index status

Index Status
────────────────────
Files indexed: 1,234
Last updated: 5 minutes ago
Status: ⚠ Stale (threshold: 5 min)
Pending changes: ~15 files
```

### Background Updates

Long updates run in the background:

```bash
$ acode run "Add error handling"

Index updating in background...

[Agent working...]
```

### Force Rebuild

When incremental update fails:

```bash
$ acode index rebuild

Rebuilding index from scratch...
  [====================] 100%
  
Rebuild complete: 1,234 files in 8.5s
```

### Configuration

```yaml
# .agent/config.yml
index:
  update:
    # Batch size for scanning
    scan_batch_size: 500
    
    # Batch size for indexing
    index_batch_size: 100
    
    # Maximum update time before checkpoint
    checkpoint_interval_seconds: 30
    
    # Retry failed updates
    retry_count: 3
```

### Troubleshooting

#### Issue 1: Index Never Updates Automatically

**Symptoms:**
- Search results don't reflect recent file changes
- `acode index status` shows index is stale
- No update activity in logs during normal operation
- Manual `acode index update` works, but automatic triggers don't fire

**Causes:**
1. All automatic triggers are disabled in configuration
2. Staleness threshold is set too high (e.g., 86400 seconds = 1 day)
3. Update trigger service failed to start
4. Configuration file syntax error prevents loading trigger settings
5. Application running in limited mode without trigger support

**Solutions:**
1. Check configuration enables at least one trigger:
   ```yaml
   index:
     update:
       on_startup: true
       before_search: true
       after_write: true
       stale_after_seconds: 3600
   ```
2. Lower staleness threshold to detect stale index sooner
3. Check application logs for trigger service initialization errors
4. Validate configuration YAML syntax with `acode config validate`
5. Restart the application to reinitialize trigger services

---

#### Issue 2: Incremental Updates Too Slow

**Symptoms:**
- Simple incremental updates take 30+ seconds when only a few files changed
- Progress bar shows high file count even for small changes
- CPU or disk activity remains high throughout update
- Log shows "Scanning: 50,000 files" when only 10 changed

**Causes:**
1. Change detection is scanning all files instead of using cached metadata
2. Ignore patterns not properly configured, causing unnecessary file scanning
3. Batch size too small, causing excessive I/O overhead
4. Index database fragmented or corrupted, slowing queries
5. File system metadata cache cold (first run after reboot)

**Solutions:**
1. Ensure index metadata table has last-scanned timestamps
2. Add comprehensive ignore patterns:
   ```yaml
   ignore:
     patterns:
       - "node_modules/**"
       - "bin/**"
       - "obj/**"
       - ".git/**"
       - "**/*.min.js"
   ```
3. Increase batch sizes: `scan_batch_size: 1000`, `index_batch_size: 200`
4. Run `acode index rebuild` to defragment database
5. Run a warm-up scan before timing-critical operations

---

#### Issue 3: Concurrent Update Lock Errors

**Symptoms:**
- Error message: "Failed to acquire index lock"
- Update fails with "Lock timeout after 300 seconds"
- Multiple processes report trying to update simultaneously
- Lock file exists but no update seems to be running

**Causes:**
1. Previous update crashed without releasing lock (stale lock)
2. Multiple instances of acode running concurrently
3. Lock file on network drive with unreliable locking
4. Lock timeout configured too short for large updates
5. System clock jumped causing lock expiration miscalculation

**Solutions:**
1. Check for stale lock: `acode index status` reports lock holder
2. Delete stale lock file manually: `.agent/index.lock`
3. Ensure only one acode instance runs per workspace
4. Use local file system for workspace (not NFS/SMB)
5. Increase lock timeout:
   ```yaml
   index:
     update:
       lock_timeout_seconds: 600
   ```

---

#### Issue 4: Update Corrupts Index or Loses Data

**Symptoms:**
- Search returns fewer results after update
- `acode index status` shows fewer files than expected
- Error: "Index integrity check failed"
- Previously indexed files no longer appear in search

**Causes:**
1. Update interrupted during write phase before atomic commit
2. Disk full during temporary file creation
3. Power loss or system crash during rename operation
4. Concurrent modification of index database by external tool
5. Checkpoint file corrupted, causing incomplete recovery

**Solutions:**
1. Run `acode index rebuild` to recreate index from scratch
2. Check disk space: `acode index status --verbose` shows space requirements
3. Ensure UPS or stable power for critical operations
4. Never modify `.agent/index.db` or related files manually
5. Delete checkpoint files and restart: `rm .agent/index.checkpoint*`

---

#### Issue 5: Update Fails with Permission Errors

**Symptoms:**
- Error: "Access denied" or "Permission denied" for specific files
- Update completes but skips many files with warnings
- Log shows "Skipped N files due to permission errors"
- Indexed file count much lower than actual file count

**Causes:**
1. Files owned by different user without read permission
2. SELinux or AppArmor blocking access to certain directories
3. Network share permissions not propagated correctly
4. Anti-virus software quarantining files during scan
5. Container filesystem without required mount permissions

**Solutions:**
1. Check file permissions: `ls -la` on reported files
2. Run with appropriate user context or adjust file ownership
3. Configure security policies to allow acode access
4. Add anti-virus exceptions for `.agent` directory and workspace
5. Mount workspace with correct permissions in container:
   ```bash
   docker run -v /workspace:/workspace:rw ...
   ```

---

#### Issue 6: Staleness Detection Reports Incorrect Status

**Symptoms:**
- `acode index status` shows "Fresh" but search misses recent changes
- Shows "Stale" immediately after successful update
- Staleness age shows negative or impossibly large values
- Before-search trigger never fires despite configuration

**Causes:**
1. System clock changed (NTP sync, timezone change, manual adjustment)
2. Metadata table corrupted with invalid timestamp
3. Update completed but metadata write failed
4. Virtual machine clock drift from host
5. File system timestamps in different timezone than system

**Solutions:**
1. Verify system time is accurate: `date` should match real time
2. Check metadata: `acode index status --debug` shows raw timestamps
3. Run `acode index rebuild` to reset all metadata
4. Enable NTP synchronization on system and VM host
5. Force UTC timestamps in configuration:
   ```yaml
   index:
     timezone: UTC
   ```

---

## Acceptance Criteria

### Change Detection

- [ ] AC-001: System detects files modified since last index update by comparing file mtime to indexed mtime
- [ ] AC-002: System detects new files not present in the current index and marks them for addition
- [ ] AC-003: System detects deleted files no longer present in the file system and marks them for removal
- [ ] AC-004: System detects renamed files as delete + add operations
- [ ] AC-005: System handles timezone changes without producing false positive change detections
- [ ] AC-006: System skips files matching ignore patterns during change detection
- [ ] AC-007: System reports count of new, modified, and deleted files after scan
- [ ] AC-008: System completes change detection for 10K files in under 5 seconds

### Update Triggers

- [ ] AC-009: CLI command `acode index update` triggers manual incremental update
- [ ] AC-010: System triggers automatic update on startup when `on_startup: true` configured
- [ ] AC-011: System triggers automatic update before search when `before_search: true` and index is stale
- [ ] AC-012: System triggers automatic update after agent file writes when `after_write: true` configured
- [ ] AC-013: All update triggers are configurable in `.agent/config.yml` under `index.update` section
- [ ] AC-014: System debounces rapid triggers, coalescing updates within 500ms window
- [ ] AC-015: System skips trigger if update is already in progress

### Incremental Update

- [ ] AC-016: IncrementalUpdateAsync() processes only files detected as changed
- [ ] AC-017: System skips unchanged files without re-reading or re-indexing content
- [ ] AC-018: System removes index entries for files detected as deleted
- [ ] AC-019: System adds index entries for files detected as new
- [ ] AC-020: System preserves all index entries for files detected as unchanged
- [ ] AC-021: System re-tokenizes and re-indexes content for modified files
- [ ] AC-022: Incremental update of 100 changed files completes in under 2 seconds
- [ ] AC-023: System handles files that change during the update gracefully with retry

### Full Rebuild

- [ ] AC-024: CLI command `acode index rebuild` performs complete index rebuild from scratch
- [ ] AC-025: Rebuild clears all existing index entries before reindexing
- [ ] AC-026: Rebuild indexes all files matching inclusion criteria and not matching exclusions
- [ ] AC-027: Rebuild reports progress with percentage complete and file counts
- [ ] AC-028: Rebuild supports cancellation via Ctrl+C with clean rollback
- [ ] AC-029: Rebuild creates new index atomically, old index available until completion

### Batching

- [ ] AC-030: System batches file system scans with configurable batch size (default 500 files)
- [ ] AC-031: System batches index writes with configurable batch size (default 100 files)
- [ ] AC-032: Batch sizes are configurable via `scan_batch_size` and `index_batch_size` settings
- [ ] AC-033: System manages memory during large batches, triggering GC when threshold exceeded
- [ ] AC-034: System processes batches sequentially to prevent memory exhaustion
- [ ] AC-035: System checkpoints progress after each batch for resumability

### Staleness Tracking

- [ ] AC-036: System tracks timestamp of last successful index update in metadata
- [ ] AC-037: Staleness threshold is configurable in seconds via `stale_after_seconds` setting
- [ ] AC-038: System forces update when staleness threshold is exceeded and trigger is enabled
- [ ] AC-039: CLI command `acode index status` reports current staleness age and status
- [ ] AC-040: Status command shows "Fresh" when within threshold, "Stale" when exceeded
- [ ] AC-041: Status command shows estimated pending change count

### Progress Reporting

- [ ] AC-042: System reports scan progress during change detection: "Scanned: X files"
- [ ] AC-043: System reports update progress during indexing: "[=====>    ] 50%"
- [ ] AC-044: System displays estimated time to completion for operations over 5 seconds
- [ ] AC-045: System reports final summary: files scanned, changed, updated, duration
- [ ] AC-046: Progress updates at most once per 100ms to avoid console spam

### Cancellation

- [ ] AC-047: Update operations support cancellation via CancellationToken
- [ ] AC-048: CLI operations respond to Ctrl+C within 500ms
- [ ] AC-049: Cancelled updates roll back to previous consistent state
- [ ] AC-050: Cancelled updates leave index in valid, searchable state
- [ ] AC-051: Cancellation reason is logged for debugging

### Atomicity

- [ ] AC-052: Index updates are atomic - all changes apply or none apply
- [ ] AC-053: System rolls back to previous state on any update failure
- [ ] AC-054: Index is never left in a partially updated state after failure
- [ ] AC-055: Atomic updates use temporary file + rename pattern
- [ ] AC-056: Interrupted updates (crash, power loss) recover to last checkpoint on restart
- [ ] AC-057: System validates index integrity after recovery

### Concurrency

- [ ] AC-058: System handles files changing during scan with retry and re-detection
- [ ] AC-059: System acquires exclusive lock during index write operations
- [ ] AC-060: Read operations (search) continue during index updates with read-write separation
- [ ] AC-061: Concurrent update requests are queued and executed serially
- [ ] AC-062: Lock acquisition times out after configurable duration (default 5 minutes)
- [ ] AC-063: Stale locks (from crashed processes) are detected and cleaned

### Error Handling

- [ ] AC-064: File access errors (permission denied) skip file with warning, continue update
- [ ] AC-065: Disk full errors abort update, report space needed, rollback cleanly
- [ ] AC-066: Parse errors (malformed files) skip file with warning, continue update
- [ ] AC-067: All errors include structured error code and actionable message
- [ ] AC-068: Transient errors (file locked) retry with exponential backoff

### Configuration

- [ ] AC-069: All update settings are configurable via `.agent/config.yml`
- [ ] AC-070: Configuration includes: on_startup, before_search, after_write, stale_after_seconds
- [ ] AC-071: Configuration includes: scan_batch_size, index_batch_size, checkpoint_interval_seconds
- [ ] AC-072: Configuration includes: retry_count, lock_timeout_seconds
- [ ] AC-073: Invalid configuration values produce clear error messages with valid ranges
- [ ] AC-074: Missing configuration uses sensible defaults

### Logging and Metrics

- [ ] AC-075: Update start/complete events logged at Info level
- [ ] AC-076: File-level operations logged at Debug level
- [ ] AC-077: Errors and warnings logged with full context for troubleshooting
- [ ] AC-078: Update duration metric recorded for performance monitoring
- [ ] AC-079: Files scanned/updated/failed metrics recorded
- [ ] AC-080: Staleness age metric available for monitoring systems

### CLI Integration

- [ ] AC-081: `acode index update` - Incremental update with progress
- [ ] AC-082: `acode index rebuild` - Full rebuild with progress
- [ ] AC-083: `acode index status` - Show staleness and pending changes
- [ ] AC-084: `acode index update --force` - Force update even if fresh
- [ ] AC-085: All commands support `--quiet` flag to suppress progress output
- [ ] AC-086: Exit codes: 0 = success, 1 = partial failure, 2 = complete failure

---

## Best Practices

### Change Detection

1. **Use file timestamps** - Compare mtime for quick change detection
2. **Hash for verification** - Confirm changes with content hash if needed
3. **Batch changes** - Coalesce rapid file changes before re-indexing
4. **Debounce updates** - Wait for activity to settle before index update

### Update Efficiency

5. **Incremental updates only** - Never re-index unchanged files
6. **Prioritize active files** - Index recently modified files first
7. **Background processing** - Update index in low-priority background thread
8. **Checkpoint progress** - Resume interrupted updates from checkpoint

### User Experience

9. **Show progress honestly** - Accurate file counts and ETA
10. **Allow cancellation** - User can stop long-running updates
11. **Report completion** - Notify when index is up-to-date
12. **Explain skipped files** - Log why files were not indexed (ignored, binary, etc.)

---

## Testing Requirements

### Unit Tests

#### ChangeDetectorTests.cs

```csharp
using Acode.Infrastructure.Index.Update;
using Xunit;

namespace Acode.Infrastructure.Tests.Index.Update;

public sealed class ChangeDetectorTests
{
    private readonly Mock<IFileSystem> _fileSystem = new();
    private readonly Mock<IIndexMetadataStore> _metadataStore = new();
    private readonly ChangeDetector _sut;

    public ChangeDetectorTests()
    {
        _sut = new ChangeDetector(_fileSystem.Object, _metadataStore.Object);
    }

    [Fact]
    public async Task DetectChangesAsync_WithModifiedFile_ReturnsModified()
    {
        // Arrange
        var lastUpdate = DateTime.UtcNow.AddHours(-1);
        var fileModified = DateTime.UtcNow;
        
        _metadataStore.Setup(m => m.GetLastUpdateTime()).Returns(lastUpdate);
        _metadataStore.Setup(m => m.GetIndexedFileMtime("test.cs")).Returns(lastUpdate);
        _fileSystem.Setup(f => f.GetLastWriteTimeUtc("test.cs")).Returns(fileModified);
        _fileSystem.Setup(f => f.EnumerateFiles(It.IsAny<string>())).Returns(new[] { "test.cs" });

        // Act
        var result = await _sut.DetectChangesAsync("/workspace", CancellationToken.None);

        // Assert
        Assert.Single(result.ModifiedFiles);
        Assert.Contains("test.cs", result.ModifiedFiles);
    }

    [Fact]
    public async Task DetectChangesAsync_WithNewFile_ReturnsNew()
    {
        // Arrange
        _metadataStore.Setup(m => m.GetIndexedFileMtime("new.cs")).Returns((DateTime?)null);
        _fileSystem.Setup(f => f.EnumerateFiles(It.IsAny<string>())).Returns(new[] { "new.cs" });
        _fileSystem.Setup(f => f.GetLastWriteTimeUtc("new.cs")).Returns(DateTime.UtcNow);

        // Act
        var result = await _sut.DetectChangesAsync("/workspace", CancellationToken.None);

        // Assert
        Assert.Single(result.NewFiles);
        Assert.Contains("new.cs", result.NewFiles);
    }

    [Fact]
    public async Task DetectChangesAsync_WithDeletedFile_ReturnsDeleted()
    {
        // Arrange
        _metadataStore.Setup(m => m.GetAllIndexedPaths()).Returns(new[] { "deleted.cs" });
        _fileSystem.Setup(f => f.EnumerateFiles(It.IsAny<string>())).Returns(Array.Empty<string>());
        _fileSystem.Setup(f => f.Exists("deleted.cs")).Returns(false);

        // Act
        var result = await _sut.DetectChangesAsync("/workspace", CancellationToken.None);

        // Assert
        Assert.Single(result.DeletedFiles);
        Assert.Contains("deleted.cs", result.DeletedFiles);
    }

    [Fact]
    public async Task DetectChangesAsync_WithUnchangedFile_DoesNotInclude()
    {
        // Arrange
        var mtime = DateTime.UtcNow.AddHours(-1);
        _metadataStore.Setup(m => m.GetIndexedFileMtime("unchanged.cs")).Returns(mtime);
        _fileSystem.Setup(f => f.GetLastWriteTimeUtc("unchanged.cs")).Returns(mtime);
        _fileSystem.Setup(f => f.EnumerateFiles(It.IsAny<string>())).Returns(new[] { "unchanged.cs" });

        // Act
        var result = await _sut.DetectChangesAsync("/workspace", CancellationToken.None);

        // Assert
        Assert.Empty(result.ModifiedFiles);
        Assert.Empty(result.NewFiles);
        Assert.Empty(result.DeletedFiles);
    }

    [Fact]
    public async Task DetectChangesAsync_WithIgnoredFile_SkipsFile()
    {
        // Arrange
        _fileSystem.Setup(f => f.EnumerateFiles(It.IsAny<string>())).Returns(new[] { "node_modules/pkg/index.js" });

        // Act
        var result = await _sut.DetectChangesAsync("/workspace", CancellationToken.None);

        // Assert
        Assert.Empty(result.NewFiles);
    }

    [Fact]
    public async Task DetectChangesAsync_ReportsProgress()
    {
        // Arrange
        var files = Enumerable.Range(0, 100).Select(i => $"file{i}.cs").ToArray();
        _fileSystem.Setup(f => f.EnumerateFiles(It.IsAny<string>())).Returns(files);
        var progressValues = new List<int>();

        // Act
        await _sut.DetectChangesAsync(
            "/workspace",
            CancellationToken.None,
            new Progress<int>(p => progressValues.Add(p)));

        // Assert
        Assert.True(progressValues.Count > 0);
        Assert.Contains(100, progressValues);
    }
}
```

#### IncrementalUpdaterTests.cs

```csharp
using Acode.Infrastructure.Index.Update;
using Xunit;

namespace Acode.Infrastructure.Tests.Index.Update;

public sealed class IncrementalUpdaterTests
{
    private readonly Mock<IIndexWriter> _indexWriter = new();
    private readonly Mock<IFileReader> _fileReader = new();
    private readonly Mock<ITokenizer> _tokenizer = new();
    private readonly IncrementalUpdater _sut;

    public IncrementalUpdaterTests()
    {
        _sut = new IncrementalUpdater(
            _indexWriter.Object,
            _fileReader.Object,
            _tokenizer.Object);
    }

    [Fact]
    public async Task UpdateAsync_WithModifiedFile_UpdatesIndexEntry()
    {
        // Arrange
        var changes = new ChangeSet(
            ModifiedFiles: new[] { "/workspace/modified.cs" },
            NewFiles: Array.Empty<string>(),
            DeletedFiles: Array.Empty<string>());
        
        _fileReader.Setup(f => f.ReadAllTextAsync("/workspace/modified.cs", It.IsAny<CancellationToken>()))
            .ReturnsAsync("new content");
        _tokenizer.Setup(t => t.Tokenize("new content")).Returns(new[] { "new", "content" });

        // Act
        var result = await _sut.UpdateAsync(changes, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.FilesUpdated);
        _indexWriter.Verify(w => w.UpdateDocumentAsync(
            "/workspace/modified.cs",
            It.IsAny<string[]>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNewFile_AddsIndexEntry()
    {
        // Arrange
        var changes = new ChangeSet(
            ModifiedFiles: Array.Empty<string>(),
            NewFiles: new[] { "/workspace/new.cs" },
            DeletedFiles: Array.Empty<string>());
        
        _fileReader.Setup(f => f.ReadAllTextAsync("/workspace/new.cs", It.IsAny<CancellationToken>()))
            .ReturnsAsync("class Foo {}");
        _tokenizer.Setup(t => t.Tokenize(It.IsAny<string>())).Returns(new[] { "class", "foo" });

        // Act
        var result = await _sut.UpdateAsync(changes, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.FilesAdded);
        _indexWriter.Verify(w => w.AddDocumentAsync(
            "/workspace/new.cs",
            It.IsAny<string[]>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithDeletedFile_RemovesIndexEntry()
    {
        // Arrange
        var changes = new ChangeSet(
            ModifiedFiles: Array.Empty<string>(),
            NewFiles: Array.Empty<string>(),
            DeletedFiles: new[] { "/workspace/deleted.cs" });

        // Act
        var result = await _sut.UpdateAsync(changes, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.FilesRemoved);
        _indexWriter.Verify(w => w.RemoveDocumentAsync(
            "/workspace/deleted.cs",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithFileReadError_SkipsFileAndContinues()
    {
        // Arrange
        var changes = new ChangeSet(
            ModifiedFiles: new[] { "/workspace/error.cs", "/workspace/good.cs" },
            NewFiles: Array.Empty<string>(),
            DeletedFiles: Array.Empty<string>());
        
        _fileReader.Setup(f => f.ReadAllTextAsync("/workspace/error.cs", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Access denied"));
        _fileReader.Setup(f => f.ReadAllTextAsync("/workspace/good.cs", It.IsAny<CancellationToken>()))
            .ReturnsAsync("good content");
        _tokenizer.Setup(t => t.Tokenize(It.IsAny<string>())).Returns(new[] { "good" });

        // Act
        var result = await _sut.UpdateAsync(changes, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.FilesUpdated);
        Assert.Equal(1, result.FilesSkipped);
    }

    [Fact]
    public async Task UpdateAsync_WithCancellation_StopsAndRollsBack()
    {
        // Arrange
        var changes = new ChangeSet(
            ModifiedFiles: Enumerable.Range(0, 100).Select(i => $"/workspace/file{i}.cs").ToArray(),
            NewFiles: Array.Empty<string>(),
            DeletedFiles: Array.Empty<string>());
        
        var cts = new CancellationTokenSource();
        var callCount = 0;
        
        _fileReader.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                if (++callCount == 10) cts.Cancel();
                return "content";
            });

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.UpdateAsync(changes, cts.Token));
        
        _indexWriter.Verify(w => w.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_BatchesOperations()
    {
        // Arrange
        var changes = new ChangeSet(
            ModifiedFiles: Enumerable.Range(0, 250).Select(i => $"/workspace/file{i}.cs").ToArray(),
            NewFiles: Array.Empty<string>(),
            DeletedFiles: Array.Empty<string>());
        
        _fileReader.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("content");
        _tokenizer.Setup(t => t.Tokenize(It.IsAny<string>())).Returns(new[] { "content" });

        // Act
        var result = await _sut.UpdateAsync(changes, CancellationToken.None);

        // Assert
        // With batch size of 100, should have 3 commit calls
        _indexWriter.Verify(w => w.CommitBatchAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }
}
```

#### UpdateTriggerManagerTests.cs

```csharp
using Acode.Infrastructure.Index.Update;
using Xunit;

namespace Acode.Infrastructure.Tests.Index.Update;

public sealed class UpdateTriggerManagerTests
{
    private readonly Mock<IIndexUpdater> _updater = new();
    private readonly Mock<IStalenessChecker> _stalenessChecker = new();
    private readonly UpdateTriggerOptions _options = new();
    private readonly UpdateTriggerManager _sut;

    public UpdateTriggerManagerTests()
    {
        _sut = new UpdateTriggerManager(_updater.Object, _stalenessChecker.Object, _options);
    }

    [Fact]
    public async Task OnStartupAsync_WhenEnabled_TriggersUpdate()
    {
        // Arrange
        _options.OnStartup = true;
        _stalenessChecker.Setup(s => s.IsStaleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _sut.OnStartupAsync(CancellationToken.None);

        // Assert
        _updater.Verify(u => u.UpdateAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnStartupAsync_WhenDisabled_DoesNotTrigger()
    {
        // Arrange
        _options.OnStartup = false;

        // Act
        await _sut.OnStartupAsync(CancellationToken.None);

        // Assert
        _updater.Verify(u => u.UpdateAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BeforeSearchAsync_WhenStale_TriggersUpdate()
    {
        // Arrange
        _options.BeforeSearch = true;
        _stalenessChecker.Setup(s => s.IsStaleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _sut.BeforeSearchAsync(CancellationToken.None);

        // Assert
        _updater.Verify(u => u.UpdateAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BeforeSearchAsync_WhenFresh_DoesNotTrigger()
    {
        // Arrange
        _options.BeforeSearch = true;
        _stalenessChecker.Setup(s => s.IsStaleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _sut.BeforeSearchAsync(CancellationToken.None);

        // Assert
        _updater.Verify(u => u.UpdateAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AfterWriteAsync_WhenEnabled_TriggersUpdate()
    {
        // Arrange
        _options.AfterWrite = true;

        // Act
        await _sut.AfterWriteAsync(new[] { "/workspace/file.cs" }, CancellationToken.None);

        // Assert
        _updater.Verify(u => u.UpdateAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AfterWriteAsync_DebouncesManyWrites()
    {
        // Arrange
        _options.AfterWrite = true;
        _options.DebounceMs = 500;

        // Act - trigger many writes rapidly
        var tasks = Enumerable.Range(0, 10)
            .Select(i => _sut.AfterWriteAsync(new[] { $"/workspace/file{i}.cs" }, CancellationToken.None));
        await Task.WhenAll(tasks);
        await Task.Delay(600); // Wait for debounce

        // Assert - should coalesce to single update
        _updater.Verify(u => u.UpdateAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Trigger_WhenUpdateInProgress_Queues()
    {
        // Arrange
        _options.OnStartup = true;
        _stalenessChecker.Setup(s => s.IsStaleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        var tcs = new TaskCompletionSource();
        _updater.Setup(u => u.UpdateAsync(It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        // Act
        var firstUpdate = _sut.OnStartupAsync(CancellationToken.None);
        var secondUpdate = _sut.OnStartupAsync(CancellationToken.None);
        
        tcs.SetResult();
        await Task.WhenAll(firstUpdate, secondUpdate);

        // Assert - second should have been queued, only one update runs
        _updater.Verify(u => u.UpdateAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

#### AtomicUpdateWriterTests.cs

```csharp
using Acode.Infrastructure.Index.Update;
using Xunit;

namespace Acode.Infrastructure.Tests.Index.Update;

public sealed class AtomicUpdateWriterTests
{
    private readonly string _testDir;
    private readonly AtomicUpdateWriter _sut;

    public AtomicUpdateWriterTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
        _sut = new AtomicUpdateWriter(_testDir);
    }

    [Fact]
    public async Task WriteAtomicAsync_WritesToTempThenRenames()
    {
        // Arrange
        var targetPath = Path.Combine(_testDir, "index.db");
        var content = "test content"u8.ToArray();

        // Act
        await _sut.WriteAtomicAsync(targetPath, content, CancellationToken.None);

        // Assert
        Assert.True(File.Exists(targetPath));
        Assert.Equal("test content", await File.ReadAllTextAsync(targetPath));
    }

    [Fact]
    public async Task WriteAtomicAsync_PreservesExistingOnFailure()
    {
        // Arrange
        var targetPath = Path.Combine(_testDir, "index.db");
        await File.WriteAllTextAsync(targetPath, "original");
        
        // Act - simulate failure by passing null content
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.WriteAtomicAsync(targetPath, null!, CancellationToken.None));

        // Assert - original preserved
        Assert.Equal("original", await File.ReadAllTextAsync(targetPath));
    }

    [Fact]
    public async Task WriteAtomicAsync_CleansTempFileOnFailure()
    {
        // Arrange
        var targetPath = Path.Combine(_testDir, "index.db");

        // Act
        try
        {
            await _sut.WriteAtomicAsync(targetPath, null!, CancellationToken.None);
        }
        catch { }

        // Assert - no temp files left behind
        var tempFiles = Directory.GetFiles(_testDir, "*.tmp");
        Assert.Empty(tempFiles);
    }

    [Fact]
    public async Task WriteAtomicAsync_HandlesLargeContent()
    {
        // Arrange
        var targetPath = Path.Combine(_testDir, "large.db");
        var content = new byte[10 * 1024 * 1024]; // 10 MB
        new Random(42).NextBytes(content);

        // Act
        await _sut.WriteAtomicAsync(targetPath, content, CancellationToken.None);

        // Assert
        var written = await File.ReadAllBytesAsync(targetPath);
        Assert.Equal(content, written);
    }
}
```

### Integration Tests

#### UpdateIntegrationTests.cs

```csharp
using Acode.Infrastructure.Index.Update;
using Xunit;

namespace Acode.Integration.Tests.Index.Update;

public sealed class UpdateIntegrationTests : IDisposable
{
    private readonly string _workspaceDir;
    private readonly IndexUpdater _sut;

    public UpdateIntegrationTests()
    {
        _workspaceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_workspaceDir);
        
        var services = new ServiceCollection()
            .AddIndexServices(_workspaceDir)
            .BuildServiceProvider();
        
        _sut = services.GetRequiredService<IndexUpdater>();
    }

    [Fact]
    public async Task UpdateAsync_WithRealFiles_UpdatesIndex()
    {
        // Arrange - create initial index
        await File.WriteAllTextAsync(Path.Combine(_workspaceDir, "file1.cs"), "class A {}");
        await _sut.RebuildAsync(null, CancellationToken.None);

        // Modify file
        await File.WriteAllTextAsync(Path.Combine(_workspaceDir, "file1.cs"), "class A { int x; }");

        // Act
        var result = await _sut.UpdateAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.FilesUpdated);
    }

    [Fact]
    public async Task UpdateAsync_WithConcurrentFileChanges_HandlesGracefully()
    {
        // Arrange
        for (int i = 0; i < 100; i++)
        {
            await File.WriteAllTextAsync(
                Path.Combine(_workspaceDir, $"file{i}.cs"),
                $"class C{i} {{}}");
        }
        await _sut.RebuildAsync(null, CancellationToken.None);

        // Start update while modifying files
        var updateTask = _sut.UpdateAsync(CancellationToken.None);
        
        // Concurrent modifications
        for (int i = 0; i < 10; i++)
        {
            await File.WriteAllTextAsync(
                Path.Combine(_workspaceDir, $"file{i}.cs"),
                $"class C{i} {{ int modified; }}");
        }

        // Act
        var result = await updateTask;

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task RebuildAsync_WithLargeFileSet_ReportsProgress()
    {
        // Arrange
        for (int i = 0; i < 1000; i++)
        {
            await File.WriteAllTextAsync(
                Path.Combine(_workspaceDir, $"file{i}.cs"),
                $"namespace N{i} {{ class C{i} {{ void M() {{}} }} }}");
        }

        var progressValues = new List<int>();

        // Act
        var result = await _sut.RebuildAsync(
            new Progress<UpdateProgress>(p => progressValues.Add(p.PercentComplete)),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1000, result.FilesAdded);
        Assert.Contains(100, progressValues);
    }

    public void Dispose()
    {
        try { Directory.Delete(_workspaceDir, true); } catch { }
    }
}
```

### E2E Tests

#### UpdateE2ETests.cs

```csharp
using Xunit;

namespace Acode.E2E.Tests.Index.Update;

public sealed class UpdateE2ETests : IDisposable
{
    private readonly string _workspaceDir;
    private readonly CliRunner _cli;

    public UpdateE2ETests()
    {
        _workspaceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_workspaceDir);
        _cli = new CliRunner(_workspaceDir);
    }

    [Fact]
    public async Task IndexUpdate_ViaCommand_UpdatesModifiedFiles()
    {
        // Arrange
        await File.WriteAllTextAsync(
            Path.Combine(_workspaceDir, "test.cs"),
            "class Original {}");
        
        await _cli.RunAsync("index", "rebuild");
        
        // Modify file
        await File.WriteAllTextAsync(
            Path.Combine(_workspaceDir, "test.cs"),
            "class Modified {}");

        // Act
        var result = await _cli.RunAsync("index", "update");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("1 file", result.Output);
        
        // Verify searchable
        var searchResult = await _cli.RunAsync("search", "Modified");
        Assert.Contains("test.cs", searchResult.Output);
    }

    [Fact]
    public async Task IndexRebuild_ViaCommand_RebuildsFromScratch()
    {
        // Arrange
        for (int i = 0; i < 50; i++)
        {
            await File.WriteAllTextAsync(
                Path.Combine(_workspaceDir, $"file{i}.cs"),
                $"class C{i} {{}}");
        }

        // Act
        var result = await _cli.RunAsync("index", "rebuild");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("50 files", result.Output);
        Assert.Contains("100%", result.Output);
    }

    [Fact]
    public async Task IndexStatus_ShowsStaleness()
    {
        // Arrange
        await File.WriteAllTextAsync(
            Path.Combine(_workspaceDir, "test.cs"),
            "class A {}");
        await _cli.RunAsync("index", "rebuild");

        // Act
        var result = await _cli.RunAsync("index", "status");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Files indexed:", result.Output);
        Assert.Contains("Last updated:", result.Output);
    }

    public void Dispose()
    {
        try { Directory.Delete(_workspaceDir, true); } catch { }
    }
}
```

### Performance Benchmarks

#### UpdateBenchmarks.cs

```csharp
using BenchmarkDotNet.Attributes;

namespace Acode.Benchmarks.Index.Update;

[MemoryDiagnoser]
public class UpdateBenchmarks
{
    private string _workspaceDir = null!;
    private IndexUpdater _updater = null!;

    [Params(1000, 10000)]
    public int FileCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _workspaceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_workspaceDir);

        for (int i = 0; i < FileCount; i++)
        {
            File.WriteAllText(
                Path.Combine(_workspaceDir, $"file{i}.cs"),
                $"namespace N{i} {{ class C{i} {{ void M() {{}} }} }}");
        }

        var services = new ServiceCollection()
            .AddIndexServices(_workspaceDir)
            .BuildServiceProvider();
        
        _updater = services.GetRequiredService<IndexUpdater>();
        _updater.RebuildAsync(null, CancellationToken.None).Wait();
    }

    [Benchmark]
    public async Task ScanForChanges()
    {
        var detector = new ChangeDetector(/* ... */);
        await detector.DetectChangesAsync(_workspaceDir, CancellationToken.None);
    }

    [Benchmark]
    public async Task IncrementalUpdate_100Files()
    {
        // Modify 100 files
        for (int i = 0; i < 100; i++)
        {
            File.WriteAllText(
                Path.Combine(_workspaceDir, $"file{i}.cs"),
                $"namespace N{i} {{ class C{i} {{ void Modified() {{}} }} }}");
        }

        await _updater.UpdateAsync(CancellationToken.None);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        try { Directory.Delete(_workspaceDir, true); } catch { }
    }
}
```

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Scan 1K files | 0.5s | 1s |
| Scan 10K files | 3s | 5s |
| Update 100 files | 1s | 2s |
| Staleness check | 50ms | 100ms |
| Lock acquisition | 10ms | 100ms |

---

## User Verification Steps

### Scenario 1: Manual Incremental Update

**Objective:** Verify that manual update detects and indexes modified files.

```bash
# Setup: Create files and build initial index
cd /test/workspace
echo "class Original {}" > test.cs
acode index rebuild

# Modify file
echo "class Modified { int x; }" > test.cs

# Run manual update
acode index update

# Expected output:
# Checking for changes...
#   Scanned: 1 files
#   Changed: 1 files
#   New: 0 files
#   Deleted: 0 files
#
# Updating index...
#   [====================] 100%
#
# Index updated in 0.3s

# Verify: Search finds modified content
acode search "Modified"
# Should return: test.cs:1: class Modified { int x; }
```

### Scenario 2: Startup Automatic Update

**Objective:** Verify that index auto-updates on startup when configured.

```bash
# Setup: Enable startup trigger
cat > .agent/config.yml << 'EOF'
index:
  update:
    on_startup: true
EOF

# Build initial index
acode index rebuild

# Modify file while acode not running
echo "class StartupTest {}" > startup.cs

# Start acode (triggers auto-update)
acode run "describe the codebase"

# Expected: Index updated before agent runs
# Look for log message: "Index updated: 1 new file"

# Verify
acode search "StartupTest"
# Should find startup.cs
```

### Scenario 3: Pre-Search Staleness Update

**Objective:** Verify that stale index triggers update before search.

```bash
# Setup: Enable before_search with short staleness
cat > .agent/config.yml << 'EOF'
index:
  update:
    before_search: true
    stale_after_seconds: 60
EOF

acode index rebuild

# Wait for staleness (or fake with system time)
# Modify a file
echo "class StaleTest {}" > stale.cs

# Run search (should trigger update first)
acode search "StaleTest"

# Expected output shows update before results:
# Index stale (2 minutes old). Updating...
#   Added: 1 file
# 
# Search results:
#   stale.cs:1: class StaleTest {}
```

### Scenario 4: Post-Write Trigger

**Objective:** Verify that agent file writes trigger index update.

```bash
# Setup: Enable after_write trigger
cat > .agent/config.yml << 'EOF'
index:
  update:
    after_write: true
EOF

acode index rebuild

# Have agent create a file
acode run "create a Calculator class in Calculator.cs"

# Agent writes file, then index updates automatically

# Immediately search for the new content
acode search "Calculator"
# Should find Calculator.cs without manual update
```

### Scenario 5: Full Rebuild

**Objective:** Verify full index rebuild with progress reporting.

```bash
# Setup: Create many files
cd /test/workspace
for i in {1..100}; do
  echo "namespace N$i { class C$i {} }" > "file$i.cs"
done

# Run rebuild
acode index rebuild

# Expected output with progress:
# Rebuilding index from scratch...
#   [==========          ] 50% - 50/100 files
#   [====================] 100% - 100/100 files
#
# Rebuild complete: 100 files in 2.1s

# Verify
acode search "C50"
# Should find file50.cs
```

### Scenario 6: Staleness Status Check

**Objective:** Verify status command shows accurate staleness information.

```bash
# Build index
acode index rebuild

# Check status immediately
acode index status

# Expected output:
# Index Status
# ────────────────────
# Files indexed: 100
# Last updated: just now
# Status: ✓ Fresh
# Pending changes: 0

# Modify some files
echo "modified" >> file1.cs
echo "modified" >> file2.cs

# Check status again
acode index status

# Expected output:
# Index Status
# ────────────────────
# Files indexed: 100
# Last updated: 30 seconds ago
# Status: ✓ Fresh
# Pending changes: ~2 files

# Wait for staleness threshold
sleep 300

acode index status
# Expected output:
# Status: ⚠ Stale (threshold: 5 min)
```

### Scenario 7: Update Cancellation

**Objective:** Verify that Ctrl+C cleanly cancels update and preserves index.

```bash
# Create many files for slow update
for i in {1..1000}; do
  echo "namespace N$i { class C$i {} }" > "file$i.cs"
done

# Start rebuild and cancel mid-way
acode index rebuild
# Press Ctrl+C at ~50%

# Expected output:
# Rebuilding index...
#   [==========          ] 50%
# ^C
# Cancelling... rolling back to previous index.
# Index preserved.

# Verify index still works (with old data)
acode search "C1"
# Should work if file1.cs was previously indexed
```

### Scenario 8: Failure Recovery

**Objective:** Verify that interrupted update recovers gracefully.

```bash
# Create index
acode index rebuild

# Simulate crash during update (kill process)
acode index update &
PID=$!
sleep 0.5
kill -9 $PID

# Restart and verify recovery
acode index status

# Expected output:
# Index Status
# ────────────────────
# Status: ✓ Valid (recovered from interrupted update)
# Last checkpoint: 50 files

# Index should be searchable
acode search "class"
```

### Scenario 9: Concurrent Access

**Objective:** Verify that concurrent update attempts are serialized.

```bash
# Terminal 1: Start long update
acode index rebuild

# Terminal 2: Try to start another update
acode index update

# Expected output in Terminal 2:
# Update in progress. Waiting for completion...
# 
# (waits until Terminal 1 completes, then runs)

# Or with timeout:
acode index update --timeout 5

# Expected if timeout exceeded:
# Error: Could not acquire index lock (timeout after 5s)
# Another update is in progress. Try again later.
```

### Scenario 10: Configuration Validation

**Objective:** Verify that invalid configuration produces clear errors.

```bash
# Create invalid config
cat > .agent/config.yml << 'EOF'
index:
  update:
    stale_after_seconds: -100
    scan_batch_size: 0
    on_startup: "maybe"
EOF

# Try to use index
acode index status

# Expected output:
# Configuration Error:
#   - stale_after_seconds: Must be positive (got: -100)
#   - scan_batch_size: Must be at least 1 (got: 0)
#   - on_startup: Must be boolean (got: "maybe")
#
# Using default configuration.
```

---

## Implementation Prompt

### File Structure

```
src/Acode.Domain/
├── Index/
│   ├── IIndexUpdater.cs
│   ├── IChangeDetector.cs
│   ├── IStalenessChecker.cs
│   ├── ChangeSet.cs
│   ├── UpdateResult.cs
│   └── StalenessInfo.cs
│
src/Acode.Application/
├── Index/
│   └── Update/
│       ├── UpdateTriggerManager.cs
│       └── UpdateTriggerOptions.cs
│
src/Acode.Infrastructure/
├── Index/
│   └── Update/
│       ├── ChangeDetector.cs
│       ├── IncrementalUpdater.cs
│       ├── StalenessChecker.cs
│       ├── UpdateBatcher.cs
│       ├── AtomicUpdateWriter.cs
│       ├── IndexLockManager.cs
│       ├── SafeFileReader.cs
│       └── UpdateResourceLimiter.cs
```

---

### Domain Models

#### IIndexUpdater.cs

```csharp
using System.Threading;
using System.Threading.Tasks;

namespace Acode.Domain.Index;

/// <summary>
/// Interface for index update operations.
/// </summary>
public interface IIndexUpdater
{
    /// <summary>
    /// Performs incremental update, processing only changed files.
    /// </summary>
    Task<UpdateResult> UpdateAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs full index rebuild from scratch.
    /// </summary>
    Task<UpdateResult> RebuildAsync(
        IProgress<UpdateProgress>? progress = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks current staleness status of the index.
    /// </summary>
    Task<StalenessInfo> CheckStalenessAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Indicates whether an update is currently needed based on staleness.
    /// </summary>
    bool IsUpdateNeeded { get; }
}
```

#### IChangeDetector.cs

```csharp
namespace Acode.Domain.Index;

/// <summary>
/// Detects file changes since last index update.
/// </summary>
public interface IChangeDetector
{
    /// <summary>
    /// Scans workspace for changed files.
    /// </summary>
    Task<ChangeSet> DetectChangesAsync(
        string workspacePath,
        CancellationToken cancellationToken = default,
        IProgress<int>? progress = null);
}
```

#### ChangeSet.cs

```csharp
namespace Acode.Domain.Index;

/// <summary>
/// Represents files changed since last index update.
/// </summary>
public sealed record ChangeSet(
    IReadOnlyList<string> NewFiles,
    IReadOnlyList<string> ModifiedFiles,
    IReadOnlyList<string> DeletedFiles)
{
    public int TotalChanges => NewFiles.Count + ModifiedFiles.Count + DeletedFiles.Count;
    public bool HasChanges => TotalChanges > 0;

    public static ChangeSet Empty => new(
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>());
}
```

#### UpdateResult.cs

```csharp
namespace Acode.Domain.Index;

/// <summary>
/// Result of an index update operation.
/// </summary>
public sealed record UpdateResult(
    bool IsSuccess,
    int FilesAdded,
    int FilesUpdated,
    int FilesRemoved,
    int FilesSkipped,
    TimeSpan Duration,
    string? Error = null)
{
    public static UpdateResult Success(int added, int updated, int removed, int skipped, TimeSpan duration) =>
        new(true, added, updated, removed, skipped, duration);

    public static UpdateResult Failure(string error) =>
        new(false, 0, 0, 0, 0, TimeSpan.Zero, error);
}
```

#### StalenessInfo.cs

```csharp
namespace Acode.Domain.Index;

/// <summary>
/// Information about index staleness.
/// </summary>
public sealed record StalenessInfo(
    DateTime LastUpdateTime,
    TimeSpan Age,
    bool IsStale,
    int EstimatedPendingChanges)
{
    public string Status => IsStale ? "Stale" : "Fresh";
}
```

---

### Application Layer

#### UpdateTriggerManager.cs

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Index;
using Microsoft.Extensions.Logging;

namespace Acode.Application.Index.Update;

/// <summary>
/// Manages automatic update triggers based on configuration.
/// </summary>
public sealed class UpdateTriggerManager
{
    private readonly IIndexUpdater _updater;
    private readonly IStalenessChecker _stalenessChecker;
    private readonly UpdateTriggerOptions _options;
    private readonly ILogger<UpdateTriggerManager> _logger;
    
    private readonly SemaphoreSlim _updateLock = new(1, 1);
    private DateTime _lastTriggerTime = DateTime.MinValue;

    public UpdateTriggerManager(
        IIndexUpdater updater,
        IStalenessChecker stalenessChecker,
        UpdateTriggerOptions options,
        ILogger<UpdateTriggerManager> logger)
    {
        _updater = updater;
        _stalenessChecker = stalenessChecker;
        _options = options;
        _logger = logger;
    }

    public async Task OnStartupAsync(CancellationToken cancellationToken)
    {
        if (!_options.OnStartup)
        {
            _logger.LogDebug("Startup update trigger disabled");
            return;
        }

        await TriggerUpdateIfNeededAsync("startup", cancellationToken);
    }

    public async Task BeforeSearchAsync(CancellationToken cancellationToken)
    {
        if (!_options.BeforeSearch)
        {
            return;
        }

        var staleness = await _stalenessChecker.CheckAsync(cancellationToken);
        if (staleness.IsStale)
        {
            _logger.LogInformation(
                "Index stale ({Age:g}). Triggering pre-search update.",
                staleness.Age);
            await TriggerUpdateIfNeededAsync("pre-search", cancellationToken);
        }
    }

    public async Task AfterWriteAsync(
        IReadOnlyList<string> modifiedFiles,
        CancellationToken cancellationToken)
    {
        if (!_options.AfterWrite)
        {
            return;
        }

        // Debounce rapid writes
        var now = DateTime.UtcNow;
        if (now - _lastTriggerTime < TimeSpan.FromMilliseconds(_options.DebounceMs))
        {
            _logger.LogDebug(
                "Debouncing after-write trigger ({FileCount} files)",
                modifiedFiles.Count);
            return;
        }

        _lastTriggerTime = now;
        await TriggerUpdateIfNeededAsync("post-write", cancellationToken);
    }

    private async Task TriggerUpdateIfNeededAsync(
        string reason,
        CancellationToken cancellationToken)
    {
        if (!await _updateLock.WaitAsync(0, cancellationToken))
        {
            _logger.LogDebug("Update already in progress, skipping {Reason} trigger", reason);
            return;
        }

        try
        {
            _logger.LogInformation("Triggering index update: {Reason}", reason);
            var result = await _updater.UpdateAsync(cancellationToken);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Update complete: +{Added} ~{Updated} -{Removed} in {Duration:g}",
                    result.FilesAdded,
                    result.FilesUpdated,
                    result.FilesRemoved,
                    result.Duration);
            }
            else
            {
                _logger.LogWarning("Update failed: {Error}", result.Error);
            }
        }
        finally
        {
            _updateLock.Release();
        }
    }
}
```

#### UpdateTriggerOptions.cs

```csharp
namespace Acode.Application.Index.Update;

/// <summary>
/// Configuration options for update triggers.
/// </summary>
public sealed class UpdateTriggerOptions
{
    public bool OnStartup { get; set; } = true;
    public bool BeforeSearch { get; set; } = true;
    public bool AfterWrite { get; set; } = true;
    public int StaleAfterSeconds { get; set; } = 300;
    public int DebounceMs { get; set; } = 500;
}
```

---

### Infrastructure Layer

#### ChangeDetector.cs

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Index;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Index.Update;

/// <summary>
/// Detects file changes by comparing current mtime to indexed mtime.
/// </summary>
public sealed class ChangeDetector : IChangeDetector
{
    private readonly IIndexMetadataStore _metadataStore;
    private readonly IIgnoreService _ignoreService;
    private readonly ILogger<ChangeDetector> _logger;

    public ChangeDetector(
        IIndexMetadataStore metadataStore,
        IIgnoreService ignoreService,
        ILogger<ChangeDetector> logger)
    {
        _metadataStore = metadataStore;
        _ignoreService = ignoreService;
        _logger = logger;
    }

    public async Task<ChangeSet> DetectChangesAsync(
        string workspacePath,
        CancellationToken cancellationToken = default,
        IProgress<int>? progress = null)
    {
        var newFiles = new List<string>();
        var modifiedFiles = new List<string>();
        var deletedFiles = new List<string>();

        // Get all indexed files for deletion detection
        var indexedPaths = await _metadataStore.GetAllIndexedPathsAsync(cancellationToken);
        var indexedSet = new HashSet<string>(indexedPaths, StringComparer.OrdinalIgnoreCase);
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Scan current files
        var files = Directory.EnumerateFiles(
            workspacePath,
            "*",
            SearchOption.AllDirectories);

        int scanned = 0;
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(workspacePath, file);
            
            // Skip ignored files
            if (_ignoreService.IsIgnored(relativePath))
            {
                continue;
            }

            seenPaths.Add(relativePath);
            scanned++;

            if (scanned % 100 == 0)
            {
                progress?.Report(scanned);
            }

            try
            {
                var currentMtime = File.GetLastWriteTimeUtc(file);
                var indexedMtime = await _metadataStore.GetIndexedMtimeAsync(
                    relativePath, cancellationToken);

                if (indexedMtime == null)
                {
                    // New file
                    newFiles.Add(relativePath);
                }
                else if (currentMtime != indexedMtime.Value)
                {
                    // Modified file
                    modifiedFiles.Add(relativePath);
                }
                // Else: unchanged
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking file: {Path}", relativePath);
            }
        }

        // Find deleted files
        foreach (var indexedPath in indexedSet)
        {
            if (!seenPaths.Contains(indexedPath))
            {
                deletedFiles.Add(indexedPath);
            }
        }

        progress?.Report(scanned);

        _logger.LogInformation(
            "Change detection complete: {New} new, {Modified} modified, {Deleted} deleted",
            newFiles.Count, modifiedFiles.Count, deletedFiles.Count);

        return new ChangeSet(newFiles, modifiedFiles, deletedFiles);
    }
}
```

#### IncrementalUpdater.cs

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Index;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Index.Update;

/// <summary>
/// Performs incremental index updates for changed files only.
/// </summary>
public sealed class IncrementalUpdater
{
    private readonly IIndexWriter _indexWriter;
    private readonly IFileReader _fileReader;
    private readonly ITokenizer _tokenizer;
    private readonly UpdateBatcher _batcher;
    private readonly ILogger<IncrementalUpdater> _logger;

    private const int DefaultBatchSize = 100;

    public IncrementalUpdater(
        IIndexWriter indexWriter,
        IFileReader fileReader,
        ITokenizer tokenizer,
        UpdateBatcher batcher,
        ILogger<IncrementalUpdater> logger)
    {
        _indexWriter = indexWriter;
        _fileReader = fileReader;
        _tokenizer = tokenizer;
        _batcher = batcher;
        _logger = logger;
    }

    public async Task<UpdateResult> UpdateAsync(
        ChangeSet changes,
        CancellationToken cancellationToken,
        IProgress<UpdateProgress>? progress = null)
    {
        var startTime = DateTime.UtcNow;
        var added = 0;
        var updated = 0;
        var removed = 0;
        var skipped = 0;

        try
        {
            await _indexWriter.BeginTransactionAsync(cancellationToken);

            // Process deletions
            foreach (var path in changes.DeletedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _indexWriter.RemoveDocumentAsync(path, cancellationToken);
                removed++;
            }

            // Process modifications
            foreach (var path in changes.ModifiedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                try
                {
                    var content = await _fileReader.ReadAllTextAsync(path, cancellationToken);
                    var tokens = _tokenizer.Tokenize(content);
                    await _indexWriter.UpdateDocumentAsync(path, tokens, cancellationToken);
                    updated++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Skipping file: {Path}", path);
                    skipped++;
                }

                await _batcher.CheckBatchAsync(cancellationToken);
            }

            // Process additions
            foreach (var path in changes.NewFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                try
                {
                    var content = await _fileReader.ReadAllTextAsync(path, cancellationToken);
                    var tokens = _tokenizer.Tokenize(content);
                    await _indexWriter.AddDocumentAsync(path, tokens, cancellationToken);
                    added++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Skipping file: {Path}", path);
                    skipped++;
                }

                await _batcher.CheckBatchAsync(cancellationToken);
            }

            await _indexWriter.CommitTransactionAsync(cancellationToken);

            var duration = DateTime.UtcNow - startTime;
            return UpdateResult.Success(added, updated, removed, skipped, duration);
        }
        catch (OperationCanceledException)
        {
            await _indexWriter.RollbackAsync(CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            await _indexWriter.RollbackAsync(CancellationToken.None);
            return UpdateResult.Failure(ex.Message);
        }
    }
}
```

#### StalenessChecker.cs

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Index;

namespace Acode.Infrastructure.Index.Update;

/// <summary>
/// Checks index staleness based on last update time.
/// </summary>
public sealed class StalenessChecker : IStalenessChecker
{
    private readonly IIndexMetadataStore _metadataStore;
    private readonly TimeSpan _staleThreshold;

    public StalenessChecker(
        IIndexMetadataStore metadataStore,
        TimeSpan staleThreshold)
    {
        _metadataStore = metadataStore;
        _staleThreshold = staleThreshold;
    }

    public async Task<StalenessInfo> CheckAsync(CancellationToken cancellationToken)
    {
        var lastUpdate = await _metadataStore.GetLastUpdateTimeAsync(cancellationToken);
        
        if (lastUpdate == null)
        {
            return new StalenessInfo(
                DateTime.MinValue,
                TimeSpan.MaxValue,
                IsStale: true,
                EstimatedPendingChanges: -1);
        }

        var age = DateTime.UtcNow - lastUpdate.Value;
        var isStale = age > _staleThreshold;

        // Estimate pending changes (quick scan)
        var pendingEstimate = await EstimatePendingChangesAsync(cancellationToken);

        return new StalenessInfo(
            lastUpdate.Value,
            age,
            isStale,
            pendingEstimate);
    }

    private async Task<int> EstimatePendingChangesAsync(CancellationToken cancellationToken)
    {
        // Quick sample-based estimation
        // Full detection is too slow for staleness check
        return 0; // Placeholder - implement sampling
    }
}
```

---

### Error Codes

| Code | Meaning | User Message |
|------|---------|--------------|
| ACODE-UPD-001 | Change detection failed | Failed to scan for file changes. Check file permissions. |
| ACODE-UPD-002 | Update failed | Index update failed. Try 'acode index rebuild'. |
| ACODE-UPD-003 | Lock conflict | Another update is in progress. Please wait. |
| ACODE-UPD-004 | Cancelled | Update was cancelled. Index preserved. |
| ACODE-UPD-005 | Disk full | Not enough disk space. Need {N} MB free. |
| ACODE-UPD-006 | Recovery needed | Index corrupted. Run 'acode index rebuild'. |

---

### DI Registration

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace Acode.Infrastructure.Index.Update;

public static class UpdateServiceCollectionExtensions
{
    public static IServiceCollection AddIndexUpdateServices(
        this IServiceCollection services)
    {
        // Domain interfaces
        services.AddScoped<IIndexUpdater, IndexUpdater>();
        services.AddScoped<IChangeDetector, ChangeDetector>();
        services.AddScoped<IStalenessChecker, StalenessChecker>();
        
        // Infrastructure components
        services.AddScoped<IncrementalUpdater>();
        services.AddScoped<UpdateBatcher>();
        services.AddScoped<AtomicUpdateWriter>();
        services.AddSingleton<IndexLockManager>();
        
        // Security components
        services.AddScoped<SafeFileReader>();
        services.AddScoped<UpdatePathValidator>();
        services.AddScoped<UpdateResourceLimiter>();
        services.AddScoped<SecureTempFileManager>();
        
        // Trigger management
        services.AddSingleton<UpdateTriggerManager>();
        
        // Options
        services.AddOptions<UpdateTriggerOptions>()
            .BindConfiguration("Index:Update");
        services.AddOptions<UpdateLimitOptions>()
            .BindConfiguration("Index:Limits");
        
        return services;
    }
}
```

---

### Implementation Checklist

1. [ ] Create domain models (IIndexUpdater, IChangeDetector, ChangeSet, UpdateResult, StalenessInfo)
2. [ ] Implement ChangeDetector with mtime-based detection
3. [ ] Implement IncrementalUpdater with batched processing
4. [ ] Implement StalenessChecker with configurable threshold
5. [ ] Implement UpdateTriggerManager with startup, pre-search, post-write triggers
6. [ ] Implement UpdateBatcher with checkpoints
7. [ ] Implement AtomicUpdateWriter with temp file + rename
8. [ ] Implement IndexLockManager with stale lock detection
9. [ ] Implement SafeFileReader with TOCTOU protection
10. [ ] Implement SecureTempFileManager with restricted permissions
11. [ ] Implement UpdatePathValidator for workspace boundary
12. [ ] Implement UpdateResourceLimiter for DoS protection
13. [ ] Add CLI commands: index update, index rebuild, index status
14. [ ] Add progress reporting for long operations
15. [ ] Add cancellation support throughout
16. [ ] Configure DI registration
17. [ ] Write unit tests for all components
18. [ ] Write integration tests
19. [ ] Write E2E tests

---

### Rollout Plan

| Phase | Description | Duration |
|-------|-------------|----------|
| 1 | Domain models and interfaces | 0.5 day |
| 2 | ChangeDetector implementation | 1 day |
| 3 | IncrementalUpdater implementation | 1 day |
| 4 | UpdateTriggerManager implementation | 1 day |
| 5 | Atomicity and locking | 1 day |
| 6 | Security components | 0.5 day |
| 7 | CLI integration | 0.5 day |
| 8 | Testing and documentation | 1 day |

---

**End of Task 015.c Specification**