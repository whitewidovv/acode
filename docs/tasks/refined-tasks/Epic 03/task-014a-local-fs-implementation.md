# Task 014.a: Local FS Implementation

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 014 (RepoFS Abstraction)  

---

## Description

### Business Value

The Local File System implementation is the primary file access mechanism for Agentic Coding Bot, serving as the default provider that most developers will use. When developers run acode directly on their machine, this implementation enables seamless file operations on local repositories without additional configuration or infrastructure.

This implementation is critical because it wraps .NET's System.IO with production-grade enhancements: atomic write operations prevent file corruption during agent modifications, streaming support handles large files without memory exhaustion, and comprehensive encoding detection ensures correct handling of diverse codebases. Without these safeguards, agent operations could corrupt files, crash on large repositories, or produce garbled content.

The Local FS implementation also establishes the security boundary that prevents the agent from accessing files outside the designated repository root. Path traversal prevention, null byte rejection, and invalid character filtering protect the user's system from accidental or malicious access attempts.

### Scope

This task delivers the complete local file system implementation:

1. **LocalFileSystem Class:** The primary IRepoFS implementation for native file access. Wraps System.IO with async operations, path validation, and security enforcement.

2. **Atomic Write Support:** Implements write-to-temp-then-rename pattern ensuring file modifications are never partially applied. Temp files use same directory for atomic rename guarantee.

3. **Encoding Detection:** Automatically detects file encoding via BOM detection and heuristics. Supports UTF-8, UTF-16, and explicit encoding override for legacy files.

4. **Large File Streaming:** Reads large files in chunks to prevent memory pressure. Configurable threshold determines streaming vs. full-read behavior.

5. **Lazy Enumeration:** Directory enumeration yields files as discovered, enabling early processing and constant memory usage regardless of directory size.

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Task 014 (RepoFS) | Interface Implementation | Implements IRepoFS interface contract |
| Task 014.c (Patching) | File Access | Patch applicator uses LocalFS for reading and writing files |
| Task 003 (DI) | Dependency Injection | Registered as default IRepoFS implementation |
| Task 002 (Config) | Configuration | Local FS settings from `repo.local` config section |
| Task 011 (Session) | Session Context | Session determines repository root path |
| Task 015 (Indexing) | Content Reading | Indexer reads file content for indexing |
| Task 025 (File Tool) | Tool Backend | File read/write tools use LocalFS |
| Task 003.c (Audit) | Audit Logging | All operations logged with path and duration |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| File not found | Read returns error | Clear FileNotFoundException with path context |
| Permission denied | Read/write blocked | Detect at startup, report with suggested remediation |
| Disk full during write | Atomic write fails | Temp file cleaned up, original unchanged |
| Lock conflict | Operation blocked | Configurable timeout with retry, deadlock detection |
| Path traversal attempt | Security violation | Strict validation, rejection, audit logging |
| Invalid encoding | Garbled content | Default to UTF-8, binary detection, explicit override option |
| Long path (Windows) | Operation fails | Detect MAX_PATH limits, report with shortening suggestion |
| Temp file cleanup fails | Orphaned temp files | Background cleanup on startup |

### Assumptions

1. The repository is stored on a local file system (not network share or cloud storage)
2. The file system supports atomic rename operations (required for atomic writes)
3. The agent process has read access to all files in the repository
4. Write access is explicitly granted via configuration when needed
5. Files are predominantly text (UTF-8) with occasional binary files
6. File sizes are reasonable for processing (< 10MB typical, streaming for larger)
7. The operating system provides reliable file locking primitives
8. Temp directory is on the same volume as repository (for atomic rename)

### Security Considerations

#### Threat 1: Lock Exhaustion Denial of Service

**Risk Description:** An attacker or buggy code could acquire file locks and never release them, preventing other operations from completing and potentially crashing the agent.

**Attack Scenario:** A malicious prompt instructs the agent to open many files for writing. The agent acquires exclusive locks but encounters an exception before releasing them. Subsequent operations timeout, and the system becomes unusable.

**Complete Mitigation Code:**

```csharp
using System.Collections.Concurrent;

namespace AgenticCoder.Infrastructure.FileSystem.Local;

/// <summary>
/// Manages file locks with automatic timeout and cleanup to prevent lock exhaustion.
/// </summary>
public sealed class SafeFileLockManager : IDisposable
{
    private readonly ConcurrentDictionary<string, LockEntry> _activeLocks = new();
    private readonly ILogger<SafeFileLockManager> _logger;
    private readonly TimeSpan _defaultTimeout;
    private readonly int _maxConcurrentLocks;
    private readonly Timer _cleanupTimer;
    private bool _disposed;

    public SafeFileLockManager(
        ILogger<SafeFileLockManager> logger,
        TimeSpan? defaultTimeout = null,
        int maxConcurrentLocks = 100)
    {
        _logger = logger;
        _defaultTimeout = defaultTimeout ?? TimeSpan.FromSeconds(30);
        _maxConcurrentLocks = maxConcurrentLocks;

        // Background cleanup of abandoned locks every 30 seconds
        _cleanupTimer = new Timer(
            CleanupExpiredLocks,
            null,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30));
    }

    public async Task<IAsyncDisposable> AcquireReadLockAsync(
        string path,
        CancellationToken ct = default)
    {
        ValidateLockCount();
        var normalizedPath = NormalizePath(path);

        var entry = _activeLocks.GetOrAdd(normalizedPath, _ => new LockEntry());

        try
        {
            // Read locks allow multiple readers
            await entry.Semaphore.WaitAsync(ct);
            entry.ReaderCount++;
            entry.LastAccess = DateTimeOffset.UtcNow;

            _logger.LogDebug("Acquired read lock: {Path} (readers: {Count})",
                path, entry.ReaderCount);

            return new LockHandle(() => ReleaseReadLock(normalizedPath, entry));
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Read lock acquisition cancelled: {Path}", path);
            throw;
        }
    }

    public async Task<IAsyncDisposable> AcquireWriteLockAsync(
        string path,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        ValidateLockCount();
        var normalizedPath = NormalizePath(path);
        var effectiveTimeout = timeout ?? _defaultTimeout;

        var entry = _activeLocks.GetOrAdd(normalizedPath, _ => new LockEntry());

        using var timeoutCts = new CancellationTokenSource(effectiveTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            // Write lock requires exclusive access - wait for all readers
            await entry.WriterSemaphore.WaitAsync(linkedCts.Token);
            entry.IsWriteLocked = true;
            entry.LastAccess = DateTimeOffset.UtcNow;

            _logger.LogDebug("Acquired write lock: {Path}", path);

            return new LockHandle(() => ReleaseWriteLock(normalizedPath, entry));
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Write lock timeout after {Timeout}ms: {Path}",
                effectiveTimeout.TotalMilliseconds, path);

            throw new LockTimeoutException(
                $"Failed to acquire write lock on '{path}' within {effectiveTimeout.TotalMilliseconds}ms. " +
                "Another process may be holding the lock.",
                "ACODE-LFS-004");
        }
    }

    private void ValidateLockCount()
    {
        if (_activeLocks.Count >= _maxConcurrentLocks)
        {
            _logger.LogError("Lock exhaustion: {Count} active locks", _activeLocks.Count);
            throw new InvalidOperationException(
                $"Maximum concurrent locks ({_maxConcurrentLocks}) exceeded. " +
                "Possible lock leak detected.");
        }
    }

    private void ReleaseReadLock(string path, LockEntry entry)
    {
        entry.ReaderCount--;
        entry.Semaphore.Release();
        _logger.LogDebug("Released read lock: {Path}", path);

        TryRemoveUnusedEntry(path, entry);
    }

    private void ReleaseWriteLock(string path, LockEntry entry)
    {
        entry.IsWriteLocked = false;
        entry.WriterSemaphore.Release();
        _logger.LogDebug("Released write lock: {Path}", path);

        TryRemoveUnusedEntry(path, entry);
    }

    private void TryRemoveUnusedEntry(string path, LockEntry entry)
    {
        if (entry.ReaderCount == 0 && !entry.IsWriteLocked)
        {
            _activeLocks.TryRemove(path, out _);
        }
    }

    private void CleanupExpiredLocks(object? state)
    {
        var expiredThreshold = DateTimeOffset.UtcNow.AddMinutes(-5);
        var expiredPaths = _activeLocks
            .Where(kvp => kvp.Value.LastAccess < expiredThreshold)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var path in expiredPaths)
        {
            if (_activeLocks.TryRemove(path, out var entry))
            {
                _logger.LogWarning("Cleaned up abandoned lock: {Path}", path);
                entry.Dispose();
            }
        }
    }

    private static string NormalizePath(string path) =>
        path.Replace('\\', '/').ToLowerInvariant();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cleanupTimer.Dispose();
        foreach (var entry in _activeLocks.Values)
        {
            entry.Dispose();
        }
        _activeLocks.Clear();
    }

    private sealed class LockEntry : IDisposable
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public SemaphoreSlim WriterSemaphore { get; } = new(1, 1);
        public int ReaderCount { get; set; }
        public bool IsWriteLocked { get; set; }
        public DateTimeOffset LastAccess { get; set; } = DateTimeOffset.UtcNow;

        public void Dispose()
        {
            Semaphore.Dispose();
            WriterSemaphore.Dispose();
        }
    }

    private sealed class LockHandle : IAsyncDisposable
    {
        private readonly Action _releaseAction;
        private bool _disposed;

        public LockHandle(Action releaseAction) => _releaseAction = releaseAction;

        public ValueTask DisposeAsync()
        {
            if (_disposed) return ValueTask.CompletedTask;
            _disposed = true;
            _releaseAction();
            return ValueTask.CompletedTask;
        }
    }
}

public sealed class LockTimeoutException : Exception
{
    public string ErrorCode { get; }

    public LockTimeoutException(string message, string errorCode)
        : base(message) => ErrorCode = errorCode;
}
```

---

#### Threat 2: Temp File Race Condition Attack

**Risk Description:** Between creating a temp file and renaming it to the target, an attacker could replace the temp file with malicious content or create a symlink to an external location.

**Attack Scenario:** Attacker monitors the temp directory. When they see a `.tmp` file created, they quickly delete it and replace it with a symlink pointing to `/etc/passwd`. The rename operation then overwrites a system file.

**Complete Mitigation Code:**

```csharp
using System.Security.Cryptography;

namespace AgenticCoder.Infrastructure.FileSystem.Local;

/// <summary>
/// Secure atomic file writer that prevents temp file race conditions.
/// </summary>
public sealed class SecureAtomicFileWriter
{
    private readonly ILogger<SecureAtomicFileWriter> _logger;
    private readonly string _repoRoot;

    public SecureAtomicFileWriter(string repoRoot, ILogger<SecureAtomicFileWriter> logger)
    {
        _repoRoot = Path.GetFullPath(repoRoot);
        _logger = logger;
    }

    public async Task WriteAsync(
        string targetPath,
        string content,
        CancellationToken ct = default)
    {
        var fullTargetPath = Path.GetFullPath(Path.Combine(_repoRoot, targetPath));

        // Validate target is within repo root
        if (!fullTargetPath.StartsWith(_repoRoot, StringComparison.OrdinalIgnoreCase))
            throw new PathTraversalException("Target path escapes repository root");

        // Generate cryptographically random temp file name
        var tempFileName = $".tmp.{GenerateSecureId()}.{Path.GetFileName(targetPath)}";
        var tempDirectory = Path.GetDirectoryName(fullTargetPath);

        if (string.IsNullOrEmpty(tempDirectory))
            throw new InvalidOperationException("Invalid target path");

        // Ensure temp directory is within repo
        if (!tempDirectory.StartsWith(_repoRoot, StringComparison.OrdinalIgnoreCase))
            throw new PathTraversalException("Temp directory escapes repository root");

        var tempPath = Path.Combine(tempDirectory, tempFileName);

        // Verify temp path doesn't already exist (should be impossible with random ID)
        if (File.Exists(tempPath) || Directory.Exists(tempPath))
        {
            _logger.LogError("Temp file collision detected - possible attack: {Path}", tempPath);
            throw new SecurityException("Temp file path collision - operation aborted");
        }

        try
        {
            // Create parent directories if needed
            Directory.CreateDirectory(tempDirectory);

            // Write to temp file with exclusive access
            await using (var stream = new FileStream(
                tempPath,
                FileMode.CreateNew,          // Fail if exists
                FileAccess.Write,
                FileShare.None,              // Exclusive access
                bufferSize: 4096,
                FileOptions.WriteThrough))   // Bypass OS cache for durability
            {
                await using var writer = new StreamWriter(stream, Encoding.UTF8);
                await writer.WriteAsync(content.AsMemory(), ct);
                await writer.FlushAsync(ct);
                await stream.FlushAsync(ct);
            }

            // Verify the file we wrote is still ours (content hash check)
            var writtenHash = await ComputeFileHashAsync(tempPath, ct);
            var expectedHash = ComputeContentHash(content);

            if (!writtenHash.SequenceEqual(expectedHash))
            {
                _logger.LogError(
                    "Temp file content mismatch - possible tampering: {Path}",
                    tempPath);
                throw new SecurityException("Temp file was modified - possible race condition attack");
            }

            // Verify temp file is not a symlink
            var tempFileInfo = new FileInfo(tempPath);
            if (tempFileInfo.LinkTarget != null)
            {
                _logger.LogError("Temp file is symlink - attack detected: {Path}", tempPath);
                throw new SecurityException("Temp file is a symlink - attack detected");
            }

            // Atomic rename
            File.Move(tempPath, fullTargetPath, overwrite: true);

            _logger.LogDebug("Atomic write complete: {Path}", targetPath);
        }
        catch (Exception ex) when (ex is not SecurityException)
        {
            // Clean up temp file on failure
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to clean up temp file: {Path}", tempPath);
            }

            throw;
        }
    }

    private static string GenerateSecureId()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }

    private static async Task<byte[]> ComputeFileHashAsync(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        return await SHA256.HashDataAsync(stream, ct);
    }

    private static byte[] ComputeContentHash(string content)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(content));
    }
}
```

---

#### Threat 3: Memory Exhaustion via Large File Attack

**Risk Description:** An attacker could craft a request to read an extremely large file, causing the agent to allocate gigabytes of memory and crash with OutOfMemoryException.

**Attack Scenario:** Repository contains a 10GB log file. Attacker prompts: "Read the contents of debug.log and summarize it." The agent attempts to load the entire file into memory, crashes, and loses all session state.

**Complete Mitigation Code:**

```csharp
namespace AgenticCoder.Infrastructure.FileSystem.Local;

/// <summary>
/// Large file reader with streaming support to prevent memory exhaustion.
/// </summary>
public sealed class SafeLargeFileReader
{
    private readonly ILogger<SafeLargeFileReader> _logger;
    private readonly long _streamingThreshold;
    private readonly long _maxFileSize;
    private readonly int _bufferSize;

    public SafeLargeFileReader(
        ILogger<SafeLargeFileReader> logger,
        long streamingThreshold = 1 * 1024 * 1024,   // 1MB
        long maxFileSize = 100 * 1024 * 1024,        // 100MB
        int bufferSize = 64 * 1024)                   // 64KB
    {
        _logger = logger;
        _streamingThreshold = streamingThreshold;
        _maxFileSize = maxFileSize;
        _bufferSize = bufferSize;
    }

    public async Task<FileReadResult> ReadFileAsync(
        string path,
        CancellationToken ct = default)
    {
        var fileInfo = new FileInfo(path);

        if (!fileInfo.Exists)
            throw new FileNotFoundException($"File not found: {path}", path);

        var fileSize = fileInfo.Length;

        // Hard limit check
        if (fileSize > _maxFileSize)
        {
            _logger.LogWarning(
                "File exceeds maximum size: {Path} ({Size} > {Max})",
                path, fileSize, _maxFileSize);

            return FileReadResult.TooLarge(
                path,
                fileSize,
                _maxFileSize,
                $"File size ({FormatSize(fileSize)}) exceeds maximum allowed " +
                $"({FormatSize(_maxFileSize)}). Use streaming read for large files.");
        }

        // Choose read strategy based on size
        if (fileSize > _streamingThreshold)
        {
            _logger.LogDebug(
                "Using streaming read for large file: {Path} ({Size})",
                path, FormatSize(fileSize));

            return await ReadWithStreamingAsync(path, fileSize, ct);
        }

        // Small file - read entirely into memory
        var content = await File.ReadAllTextAsync(path, ct);
        return FileReadResult.Success(path, content, fileSize);
    }

    private async Task<FileReadResult> ReadWithStreamingAsync(
        string path,
        long fileSize,
        CancellationToken ct)
    {
        var chunks = new List<string>();
        var totalBytesRead = 0L;
        var encoding = await DetectEncodingAsync(path, ct);

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            _bufferSize,
            FileOptions.SequentialScan | FileOptions.Asynchronous);

        using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true);

        var buffer = new char[_bufferSize];
        int charsRead;

        while ((charsRead = await reader.ReadAsync(buffer, ct)) > 0)
        {
            chunks.Add(new string(buffer, 0, charsRead));
            totalBytesRead += charsRead;

            // Progress logging for very large files
            if (totalBytesRead % (10 * 1024 * 1024) == 0) // Every 10MB
            {
                _logger.LogDebug(
                    "Reading large file: {Path} - {Progress}%",
                    path, (totalBytesRead * 100) / fileSize);
            }

            ct.ThrowIfCancellationRequested();
        }

        var content = string.Concat(chunks);
        return FileReadResult.Success(path, content, fileSize, wasStreamed: true);
    }

    private static async Task<Encoding> DetectEncodingAsync(string path, CancellationToken ct)
    {
        var buffer = new byte[4];
        await using var stream = File.OpenRead(path);
        var bytesRead = await stream.ReadAsync(buffer, ct);

        if (bytesRead >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
            return Encoding.UTF8;
        if (bytesRead >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE)
            return Encoding.Unicode;
        if (bytesRead >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF)
            return Encoding.BigEndianUnicode;

        return Encoding.UTF8;
    }

    private static string FormatSize(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
            _ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
        };
    }
}

public sealed record FileReadResult
{
    public required bool IsSuccess { get; init; }
    public string? Content { get; init; }
    public required string Path { get; init; }
    public required long FileSize { get; init; }
    public bool WasStreamed { get; init; }
    public string? ErrorMessage { get; init; }
    public long? MaxAllowedSize { get; init; }

    public static FileReadResult Success(
        string path,
        string content,
        long fileSize,
        bool wasStreamed = false) => new()
    {
        IsSuccess = true,
        Path = path,
        Content = content,
        FileSize = fileSize,
        WasStreamed = wasStreamed
    };

    public static FileReadResult TooLarge(
        string path,
        long fileSize,
        long maxSize,
        string message) => new()
    {
        IsSuccess = false,
        Path = path,
        FileSize = fileSize,
        MaxAllowedSize = maxSize,
        ErrorMessage = message
    };
}
```

---

#### Threat 4: Encoding Detection Exploitation

**Risk Description:** Malicious files with crafted byte sequences could cause incorrect encoding detection, leading to garbled content or code injection when the content is processed downstream.

**Attack Scenario:** Attacker creates a file that appears to be UTF-8 but contains invalid sequences. When the agent reads it and passes to an LLM, the garbled output confuses the model into executing unintended actions.

**Complete Mitigation Code:**

```csharp
namespace AgenticCoder.Infrastructure.FileSystem.Local;

/// <summary>
/// Robust encoding detector with fallback handling for malformed content.
/// </summary>
public sealed class SafeEncodingDetector
{
    private readonly ILogger<SafeEncodingDetector> _logger;

    public SafeEncodingDetector(ILogger<SafeEncodingDetector> logger)
    {
        _logger = logger;
    }

    public async Task<EncodingDetectionResult> DetectAsync(
        string path,
        CancellationToken ct = default)
    {
        var buffer = new byte[8192]; // Read first 8KB for detection
        int bytesRead;

        await using (var stream = File.OpenRead(path))
        {
            bytesRead = await stream.ReadAsync(buffer, ct);
        }

        if (bytesRead == 0)
            return EncodingDetectionResult.Empty(Encoding.UTF8);

        // Check BOM first
        var bomResult = DetectByBOM(buffer.AsSpan(0, bytesRead));
        if (bomResult.HasValue)
        {
            return EncodingDetectionResult.FromBOM(bomResult.Value.Encoding, bomResult.Value.BOMLength);
        }

        // Check for binary content (null bytes, high concentration of control chars)
        var binaryScore = ComputeBinaryScore(buffer.AsSpan(0, bytesRead));
        if (binaryScore > 0.3) // More than 30% binary-like
        {
            _logger.LogDebug("File appears to be binary: {Path} (score: {Score:P})", path, binaryScore);
            return EncodingDetectionResult.Binary(binaryScore);
        }

        // Try UTF-8 validation
        var utf8Result = ValidateUtf8(buffer.AsSpan(0, bytesRead));
        if (utf8Result.IsValid)
        {
            return EncodingDetectionResult.FromHeuristic(Encoding.UTF8, utf8Result.Confidence);
        }

        // UTF-8 validation failed - check if it's mostly valid with some issues
        if (utf8Result.Confidence > 0.9)
        {
            _logger.LogWarning(
                "File has UTF-8 encoding errors: {Path} (confidence: {Confidence:P}, errors: {Errors})",
                path, utf8Result.Confidence, utf8Result.InvalidSequences);

            // Return UTF-8 with replacement decoder
            return EncodingDetectionResult.FromHeuristicWithErrors(
                Encoding.UTF8,
                utf8Result.Confidence,
                utf8Result.InvalidSequences);
        }

        // Fall back to system default encoding
        _logger.LogWarning(
            "Could not determine encoding for: {Path} - using system default",
            path);

        return EncodingDetectionResult.Fallback(Encoding.Default);
    }

    private static (Encoding Encoding, int BOMLength)? DetectByBOM(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length >= 3 &&
            buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
            return (Encoding.UTF8, 3);

        if (buffer.Length >= 4 &&
            buffer[0] == 0x00 && buffer[1] == 0x00 &&
            buffer[2] == 0xFE && buffer[3] == 0xFF)
            return (new UTF32Encoding(bigEndian: true, byteOrderMark: true), 4);

        if (buffer.Length >= 4 &&
            buffer[0] == 0xFF && buffer[1] == 0xFE &&
            buffer[2] == 0x00 && buffer[3] == 0x00)
            return (Encoding.UTF32, 4);

        if (buffer.Length >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF)
            return (Encoding.BigEndianUnicode, 2);

        if (buffer.Length >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE)
            return (Encoding.Unicode, 2);

        return null;
    }

    private static double ComputeBinaryScore(ReadOnlySpan<byte> buffer)
    {
        var binaryCount = 0;
        foreach (var b in buffer)
        {
            // Null bytes or most control characters indicate binary
            if (b == 0 || (b < 32 && b != 9 && b != 10 && b != 13))
                binaryCount++;
        }

        return (double)binaryCount / buffer.Length;
    }

    private static Utf8ValidationResult ValidateUtf8(ReadOnlySpan<byte> buffer)
    {
        var invalidCount = 0;
        var i = 0;

        while (i < buffer.Length)
        {
            var b = buffer[i];

            int expectedContinuationBytes;
            if ((b & 0x80) == 0)       // ASCII
                expectedContinuationBytes = 0;
            else if ((b & 0xE0) == 0xC0) // 2-byte sequence
                expectedContinuationBytes = 1;
            else if ((b & 0xF0) == 0xE0) // 3-byte sequence
                expectedContinuationBytes = 2;
            else if ((b & 0xF8) == 0xF0) // 4-byte sequence
                expectedContinuationBytes = 3;
            else
            {
                // Invalid start byte
                invalidCount++;
                i++;
                continue;
            }

            // Check continuation bytes
            var valid = true;
            for (var j = 1; j <= expectedContinuationBytes && i + j < buffer.Length; j++)
            {
                if ((buffer[i + j] & 0xC0) != 0x80)
                {
                    valid = false;
                    break;
                }
            }

            if (!valid)
                invalidCount++;

            i += 1 + expectedContinuationBytes;
        }

        var confidence = 1.0 - ((double)invalidCount / (buffer.Length / 3.0));
        return new Utf8ValidationResult
        {
            IsValid = invalidCount == 0,
            Confidence = Math.Max(0, confidence),
            InvalidSequences = invalidCount
        };
    }
}

public sealed record EncodingDetectionResult
{
    public required Encoding Encoding { get; init; }
    public required EncodingSource Source { get; init; }
    public double Confidence { get; init; } = 1.0;
    public bool IsBinary { get; init; }
    public int BOMLength { get; init; }
    public int InvalidSequences { get; init; }

    public static EncodingDetectionResult Empty(Encoding encoding) => new()
    {
        Encoding = encoding,
        Source = EncodingSource.Default,
        Confidence = 1.0
    };

    public static EncodingDetectionResult FromBOM(Encoding encoding, int bomLength) => new()
    {
        Encoding = encoding,
        Source = EncodingSource.BOM,
        Confidence = 1.0,
        BOMLength = bomLength
    };

    public static EncodingDetectionResult FromHeuristic(Encoding encoding, double confidence) => new()
    {
        Encoding = encoding,
        Source = EncodingSource.Heuristic,
        Confidence = confidence
    };

    public static EncodingDetectionResult FromHeuristicWithErrors(
        Encoding encoding,
        double confidence,
        int invalidCount) => new()
    {
        Encoding = encoding,
        Source = EncodingSource.HeuristicWithErrors,
        Confidence = confidence,
        InvalidSequences = invalidCount
    };

    public static EncodingDetectionResult Binary(double binaryScore) => new()
    {
        Encoding = Encoding.Default,
        Source = EncodingSource.Binary,
        IsBinary = true,
        Confidence = binaryScore
    };

    public static EncodingDetectionResult Fallback(Encoding encoding) => new()
    {
        Encoding = encoding,
        Source = EncodingSource.Fallback,
        Confidence = 0.5
    };
}

public enum EncodingSource
{
    Default,
    BOM,
    Heuristic,
    HeuristicWithErrors,
    Binary,
    Fallback
}

public readonly struct Utf8ValidationResult
{
    public bool IsValid { get; init; }
    public double Confidence { get; init; }
    public int InvalidSequences { get; init; }
}
```

---

#### Threat 5: Time-of-Check to Time-of-Use (TOCTOU) File Access Race

**Risk Description:** Between checking a file's properties (size, permissions, existence) and actually reading it, the file could be modified by another process, leading to unexpected behavior.

**Attack Scenario:** Agent checks that a file exists and is under the size limit. Between the check and the read, an attacker replaces the file with a 50GB file. The agent attempts to read it and runs out of memory.

**Complete Mitigation Code:**

```csharp
namespace AgenticCoder.Infrastructure.FileSystem.Local;

/// <summary>
/// Secure file reader that prevents TOCTOU race conditions.
/// </summary>
public sealed class ToctouSafeFileReader
{
    private readonly ILogger<ToctouSafeFileReader> _logger;
    private readonly long _maxFileSize;

    public ToctouSafeFileReader(
        ILogger<ToctouSafeFileReader> logger,
        long maxFileSize = 100 * 1024 * 1024)
    {
        _logger = logger;
        _maxFileSize = maxFileSize;
    }

    /// <summary>
    /// Reads a file with TOCTOU protection by checking size AFTER opening.
    /// </summary>
    public async Task<string> ReadFileSecurelyAsync(
        string path,
        CancellationToken ct = default)
    {
        // Open file FIRST, then check properties on the open handle
        // This prevents the file from being replaced between check and read
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,          // Allow other readers but not writers
            bufferSize: 4096,
            FileOptions.Asynchronous);

        // Now check size on the OPEN file handle - this is TOCTOU safe
        var fileSize = stream.Length;

        if (fileSize > _maxFileSize)
        {
            _logger.LogWarning(
                "File size check failed (post-open): {Path} ({Size} > {Max})",
                path, fileSize, _maxFileSize);

            throw new FileTooLargeException(
                $"File '{path}' exceeds maximum size ({fileSize} > {_maxFileSize})",
                path,
                fileSize,
                _maxFileSize);
        }

        // Read with bounded memory allocation
        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);

        // Use a bounded buffer instead of ReadToEnd which could allocate unbounded
        var content = await ReadWithSizeLimitAsync(reader, fileSize, ct);

        return content;
    }

    private async Task<string> ReadWithSizeLimitAsync(
        StreamReader reader,
        long expectedSize,
        CancellationToken ct)
    {
        // Pre-allocate StringBuilder with expected size
        var builder = new StringBuilder((int)Math.Min(expectedSize, int.MaxValue));
        var buffer = new char[8192];
        var totalRead = 0L;

        int charsRead;
        while ((charsRead = await reader.ReadAsync(buffer, ct)) > 0)
        {
            totalRead += charsRead;

            // Safety check - file grew while reading
            if (totalRead > _maxFileSize)
            {
                _logger.LogError(
                    "File grew during read - possible attack: expected {Expected}, got {Actual}+",
                    expectedSize, totalRead);

                throw new SecurityException(
                    $"File size changed during read - possible TOCTOU attack. " +
                    $"Expected {expectedSize}, read {totalRead}+ bytes");
            }

            builder.Append(buffer, 0, charsRead);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Securely checks if a file exists and is readable, returning its handle.
    /// </summary>
    public FileSecurityCheck TryOpenSecurely(string path)
    {
        try
        {
            var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            // Check for symlinks
            var fileInfo = new FileInfo(path);
            if (fileInfo.LinkTarget != null)
            {
                stream.Dispose();
                return FileSecurityCheck.SymlinkDetected(path, fileInfo.LinkTarget);
            }

            return FileSecurityCheck.Success(stream);
        }
        catch (FileNotFoundException)
        {
            return FileSecurityCheck.NotFound(path);
        }
        catch (UnauthorizedAccessException ex)
        {
            return FileSecurityCheck.AccessDenied(path, ex.Message);
        }
        catch (IOException ex)
        {
            return FileSecurityCheck.IoError(path, ex.Message);
        }
    }
}

public sealed class FileTooLargeException : Exception
{
    public string Path { get; }
    public long ActualSize { get; }
    public long MaxSize { get; }

    public FileTooLargeException(string message, string path, long actualSize, long maxSize)
        : base(message)
    {
        Path = path;
        ActualSize = actualSize;
        MaxSize = maxSize;
    }
}

public sealed record FileSecurityCheck
{
    public required bool IsSuccess { get; init; }
    public FileStream? Stream { get; init; }
    public string? Path { get; init; }
    public string? ErrorMessage { get; init; }
    public string? SymlinkTarget { get; init; }
    public FileSecurityCheckError? Error { get; init; }

    public static FileSecurityCheck Success(FileStream stream) => new()
    {
        IsSuccess = true,
        Stream = stream
    };

    public static FileSecurityCheck NotFound(string path) => new()
    {
        IsSuccess = false,
        Path = path,
        Error = FileSecurityCheckError.NotFound,
        ErrorMessage = $"File not found: {path}"
    };

    public static FileSecurityCheck AccessDenied(string path, string details) => new()
    {
        IsSuccess = false,
        Path = path,
        Error = FileSecurityCheckError.AccessDenied,
        ErrorMessage = $"Access denied: {path} - {details}"
    };

    public static FileSecurityCheck SymlinkDetected(string path, string target) => new()
    {
        IsSuccess = false,
        Path = path,
        Error = FileSecurityCheckError.SymlinkDetected,
        SymlinkTarget = target,
        ErrorMessage = $"Symlink detected: {path} -> {target}"
    };

    public static FileSecurityCheck IoError(string path, string details) => new()
    {
        IsSuccess = false,
        Path = path,
        Error = FileSecurityCheckError.IoError,
        ErrorMessage = $"I/O error: {path} - {details}"
    };
}

public enum FileSecurityCheckError
{
    NotFound,
    AccessDenied,
    SymlinkDetected,
    IoError
}
```

---

## Use Cases

### Use Case 1: Developer Reading Source Code for Context

**Persona:** Emma, Senior Backend Developer at a fintech startup

**Context:** Emma is using Acode to understand a complex payment processing module before making changes. The codebase contains 500+ C# files with various encodings inherited from legacy systems.

**Before Local FS Implementation:**
Emma would need to manually open each file in her IDE, sometimes encountering garbled text when the encoding wasn't detected correctly. When asking Acode to read files, she'd occasionally get corrupted content from improperly handled encoding, leading to misleading suggestions from the AI assistant.

**After Local FS Implementation:**
```bash
$ acode run "Show me the PaymentProcessor class and explain its workflow"

[Tool: read_file]
  Path: src/Services/PaymentProcessor.cs
  Encoding: UTF-8 (auto-detected)
  Size: 15,432 bytes
  Result: (correctly decoded content displayed)

The PaymentProcessor class implements a saga pattern for distributed transactions...
```

**Metrics:**
- File read latency: 3ms for 15KB file (was 50ms with manual IDE opening)
- Encoding accuracy: 100% (BOM detection + UTF-8 default)
- Developer time saved: 2 hours/day avoiding encoding issues
- Files processed per session: 200+ without memory pressure

---

### Use Case 2: Automated Code Modification with Safety Guarantees

**Persona:** Marcus, DevOps Engineer responsible for platform upgrades

**Context:** Marcus is using Acode to migrate 150 configuration files from YAML v1.0 to v2.0 format. He needs absolute certainty that if any file modification fails, the original file remains intact.

**Before Local FS Implementation:**
Marcus would write custom migration scripts with manual backup/restore logic. Previous migration attempts had left some files in corrupted states when the process was interrupted, requiring time-consuming recovery from source control.

**After Local FS Implementation:**
```bash
$ acode run "Migrate all config files from YAML v1.0 to v2.0 format"

[Processing 150 files...]
[Tool: write_file]
  Path: config/services/payment.yml
  Atomic: true
  Temp file: config/services/.payment.yml.tmp.a8f3b2c1
  Step 1: Write to temp file ✓
  Step 2: Flush to disk ✓
  Step 3: Rename to target ✓
  Result: SUCCESS

...

[Summary]
  Files processed: 150
  Successful: 148
  Failed: 2 (permission denied)
  Corrupted: 0 (atomic writes guarantee)
```

**Metrics:**
- Zero file corruptions during 1,000+ migration runs
- Atomic write overhead: < 5ms per file
- Recovery time from failure: 0 (original preserved automatically)
- Migration throughput: 150 files in 45 seconds

---

### Use Case 3: Large Repository Indexing with Memory Efficiency

**Persona:** Jordan, Platform Engineer maintaining a monorepo

**Context:** Jordan's team maintains a 50GB monorepo with 200,000+ files. They need Acode to index the codebase for semantic search without causing out-of-memory errors or slowing down other processes.

**Before Local FS Implementation:**
Previous attempts to index the repository would consume 8GB+ of memory as file contents were loaded entirely into memory. The indexing process would often be killed by the OOM killer, leaving partial indices.

**After Local FS Implementation:**
```bash
$ acode index --repository /path/to/monorepo

[Indexing 200,000 files...]
  Current memory: 512MB (streaming enabled)
  Files/second: 2,500

[Progress]
  Indexed: 50,000 / 200,000
  Large files (streamed): 1,247
  Binary files (skipped): 3,891

[Tool: enumerate_files]
  Mode: Lazy enumeration
  Batch size: 1,000 files
  Memory stable: true

[Complete]
  Total files indexed: 196,109
  Memory peak: 650MB
  Duration: 78 seconds
```

**Metrics:**
- Memory usage: 650MB peak (vs 8GB+ previously)
- Large file handling: Streaming threshold 1MB
- Enumeration: Lazy, 1,000 files/batch
- Indexing speed: 2,500 files/second

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Local FS | Native file system |
| System.IO | .NET file system APIs |
| Atomic Write | All-or-nothing write |
| Temp File | Intermediate file for atomic |
| BOM | Byte Order Mark |
| Encoding | Character encoding |
| Streaming | Read in chunks |
| Case Sensitivity | Upper/lower handling |
| File Lock | Exclusive access |
| Lazy Enumeration | On-demand iteration |
| Memory Mapped | OS file mapping |
| ENOENT | File not found |
| EACCES | Permission denied |
| Path Separator | / or \ |
| Normalized Path | Consistent format |

---

## Out of Scope

The following items are explicitly excluded from Task 014.a:

- **Docker file systems** - Task 014.b
- **Network shares** - Not supported
- **Remote files** - Not supported
- **Symbolic links** - Limited support
- **Hard links** - Not supported
- **Extended attributes** - Not supported
- **File system events** - Separate concern
- **Compression** - Not supported

---

## Functional Requirements

### File Reading (FR-014a-01 to FR-014a-07)

| ID | Requirement |
|----|-------------|
| FR-014a-01 | ReadFileAsync MUST return file content as string |
| FR-014a-02 | ReadFileAsync MUST default to UTF-8 encoding when no BOM present |
| FR-014a-03 | ReadFileAsync MUST detect and respect BOM encoding |
| FR-014a-04 | ReadLinesAsync MUST return file content as IAsyncEnumerable of lines |
| FR-014a-05 | ReadBytesAsync MUST return file content as byte array |
| FR-014a-06 | Large files (above threshold) MUST be streamed rather than fully loaded |
| FR-014a-07 | Binary files MUST be detected and handled without encoding conversion |

### File Writing (FR-014a-08 to FR-014a-14)

| ID | Requirement |
|----|-------------|
| FR-014a-08 | WriteFileAsync MUST write string content to specified path |
| FR-014a-09 | WriteFileAsync MUST use atomic write pattern (temp file then rename) |
| FR-014a-10 | Temp file MUST be created in same directory as target file |
| FR-014a-11 | Atomic rename MUST replace original file only on success |
| FR-014a-12 | WriteLinesAsync MUST write IEnumerable of lines with platform line endings |
| FR-014a-13 | WriteBytesAsync MUST write byte array content to specified path |
| FR-014a-14 | WriteFileAsync MUST create parent directories if they do not exist |

### File Deletion (FR-014a-15 to FR-014a-19)

| ID | Requirement |
|----|-------------|
| FR-014a-15 | DeleteFileAsync MUST delete file at specified path |
| FR-014a-16 | DeleteFileAsync MUST NOT throw error if file does not exist |
| FR-014a-17 | DeleteDirectoryAsync MUST delete directory at specified path |
| FR-014a-18 | DeleteDirectoryAsync MUST support recursive deletion |
| FR-014a-19 | DeleteDirectoryAsync MUST handle non-empty directories when recursive enabled |

### Directory Enumeration (FR-014a-20 to FR-014a-25)

| ID | Requirement |
|----|-------------|
| FR-014a-20 | EnumerateFilesAsync MUST return IAsyncEnumerable of file entries |
| FR-014a-21 | Enumeration MUST be lazy (yield files as discovered) |
| FR-014a-22 | EnumerateFilesAsync MUST support recursive option |
| FR-014a-23 | EnumerateFilesAsync MUST support glob pattern filtering |
| FR-014a-24 | EnumerateFilesAsync MUST support hidden file include/exclude option |
| FR-014a-25 | EnumerateDirectoriesAsync MUST return IAsyncEnumerable of directory entries |

### Metadata (FR-014a-26 to FR-014a-31)

| ID | Requirement |
|----|-------------|
| FR-014a-26 | ExistsAsync MUST return true if file or directory exists |
| FR-014a-27 | GetMetadataAsync MUST return file metadata object |
| FR-014a-28 | Metadata MUST include file size in bytes |
| FR-014a-29 | Metadata MUST include last modified timestamp |
| FR-014a-30 | Metadata MUST include created timestamp |
| FR-014a-31 | Metadata MUST include IsDirectory flag |

### Path Handling (FR-014a-32 to FR-014a-36)

| ID | Requirement |
|----|-------------|
| FR-014a-32 | Path normalization MUST convert all paths to consistent format |
| FR-014a-33 | Forward slashes MUST be accepted on all platforms |
| FR-014a-34 | Backslashes MUST be converted to forward slashes |
| FR-014a-35 | Trailing slashes MUST be handled consistently |
| FR-014a-36 | Path combining MUST respect repository root boundary |

### Security (FR-014a-37 to FR-014a-40)

| ID | Requirement |
|----|-------------|
| FR-014a-37 | All operations MUST enforce repository root boundary |
| FR-014a-38 | Path traversal via ../ MUST be prevented |
| FR-014a-39 | Null bytes in paths MUST be rejected |
| FR-014a-40 | Invalid characters in paths MUST be rejected |

### Error Handling (FR-014a-41 to FR-014a-45)

| ID | Requirement |
|----|-------------|
| FR-014a-41 | FileNotFoundException MUST be thrown for missing files |
| FR-014a-42 | AccessDeniedException MUST be thrown for permission failures |
| FR-014a-43 | DirectoryNotFoundException MUST be thrown for missing directories |
| FR-014a-44 | IOException MUST be thrown for disk/IO errors |
| FR-014a-45 | All exceptions MUST include clear, actionable error messages |

### Locking (FR-014a-46 to FR-014a-49)

| ID | Requirement |
|----|-------------|
| FR-014a-46 | Read operations MUST use shared locks allowing concurrent reads |
| FR-014a-47 | Write operations MUST use exclusive locks |
| FR-014a-48 | Lock timeout MUST be configurable |
| FR-014a-49 | Lock conflicts MUST trigger configurable retry behavior |

### Encoding (FR-014a-50 to FR-014a-54)

| ID | Requirement |
|----|-------------|
| FR-014a-50 | UTF-8 MUST be the default encoding when no BOM detected |
| FR-014a-51 | UTF-8 with BOM MUST be detected and used |
| FR-014a-52 | UTF-16 (LE/BE) MUST be detected via BOM |
| FR-014a-53 | Binary files MUST be detected to prevent encoding conversion |
| FR-014a-54 | Explicit encoding override MUST be supported for read operations |

---

## Non-Functional Requirements

### Performance (NFR-014a-01 to NFR-014a-04)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014a-01 | Performance | 1KB file read MUST complete in < 5ms |
| NFR-014a-02 | Performance | 1MB file read MUST complete in < 50ms |
| NFR-014a-03 | Performance | Directory enumeration MUST process 1000 files in < 50ms |
| NFR-014a-04 | Performance | File write MUST complete at < 20ms per MB |

### Reliability (NFR-014a-05 to NFR-014a-07)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014a-05 | Reliability | Atomic writes MUST prevent partial file content on failure |
| NFR-014a-06 | Reliability | Temp files MUST be cleaned up on failure |
| NFR-014a-07 | Reliability | File locks MUST be released on error or exception |

### Security (NFR-014a-08 to NFR-014a-10)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014a-08 | Security | All paths MUST be validated before I/O operations |
| NFR-014a-09 | Security | Operations MUST NOT allow privilege escalation |
| NFR-014a-10 | Security | Repository root boundary MUST be enforced on all operations |

### Maintainability (NFR-014a-11 to NFR-014a-13)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014a-11 | Maintainability | Error messages MUST be clear and actionable |
| NFR-014a-12 | Maintainability | All operations MUST be logged with appropriate detail level |
| NFR-014a-13 | Maintainability | Exceptions MUST include context for debugging |

---

## User Manual Documentation

### Overview

The Local FS implementation provides file access for local repositories. It is the default implementation when running acode directly on your machine.

### Configuration

```yaml
# .agent/config.yml
repo:
  fs_type: local  # Default
  root: .         # Repository root
  
  local:
    # Large file threshold (bytes)
    large_file_threshold: 1048576  # 1MB
    
    # Lock timeout (milliseconds)
    lock_timeout_ms: 5000
    
    # Retry on lock conflict
    lock_retry_count: 3
```

### Usage Examples

```csharp
// Create local file system
var fs = new LocalFileSystem(new LocalFSOptions
{
    RootPath = "/path/to/repo",
    LargeFileThreshold = 1024 * 1024  // 1MB
});

// Read a file
var content = await fs.ReadFileAsync("src/Program.cs");

// Write a file
await fs.WriteFileAsync("output.txt", "Hello, World!");

// Enumerate files
await foreach (var file in fs.EnumerateFilesAsync("src", recursive: true))
{
    Console.WriteLine($"{file.Path}: {file.Size} bytes");
}
```

### Encoding Handling

The local FS auto-detects encoding:

1. **BOM present**: Uses BOM encoding
2. **No BOM**: Attempts UTF-8
3. **Invalid UTF-8**: Falls back to system encoding

Force specific encoding:

```csharp
var content = await fs.ReadFileAsync("legacy.txt", Encoding.Latin1);
```

### Atomic Writes

Writes are atomic by default:

1. Content written to `filename.tmp`
2. Temp file flushed to disk
3. Rename replaces original
4. If any step fails, original unchanged

### Troubleshooting

#### File Locked

**Problem:** Cannot write, file in use

**Solutions:**
1. Close other applications using file
2. Increase lock_timeout_ms
3. Check for antivirus interference

#### Permission Denied

**Problem:** Cannot read or write

**Solutions:**
1. Check file permissions
2. Run with appropriate privileges
3. Check if file is read-only

#### Encoding Issues

**Problem:** Garbled text

**Solutions:**
1. Specify explicit encoding
2. Check file's actual encoding
3. Use binary read for non-text

---

## Acceptance Criteria

### Reading

- [ ] AC-001: Read file works
- [ ] AC-002: Read lines works
- [ ] AC-003: Read bytes works
- [ ] AC-004: Large file streaming works
- [ ] AC-005: Encoding detected

### Writing

- [ ] AC-006: Write file works
- [ ] AC-007: Atomic write works
- [ ] AC-008: Creates parent dirs
- [ ] AC-009: Temp file cleaned

### Enumeration

- [ ] AC-010: Files enumerated
- [ ] AC-011: Lazy enumeration works
- [ ] AC-012: Recursive works
- [ ] AC-013: Filtering works

### Security

- [ ] AC-014: Root enforced
- [ ] AC-015: Traversal blocked
- [ ] AC-016: Invalid chars rejected

### Error Handling

- [ ] AC-017: Not found exception
- [ ] AC-018: Access denied exception
- [ ] AC-019: Clear messages

---

## Best Practices

### File I/O

1. **Use FileShare.Read** - Allow other processes to read files while we have them open
2. **Buffer appropriately** - Use 4KB-64KB buffers based on operation type
3. **Dispose streams promptly** - Use `using` statements to release file handles
4. **Handle long paths** - Enable extended-length path support on Windows

### Error Handling

5. **Map to domain exceptions** - Convert IOException to FileSystemException with context
6. **Retry on transient errors** - Anti-virus locks, network hiccups deserve retry
7. **Log before throwing** - Record details before wrapping exceptions
8. **Preserve stack trace** - Use ExceptionDispatchInfo when rethrowing

### Performance

9. **Avoid File.Exists checks** - Just try operation; handle NotFound exception
10. **Use Memory<byte> over byte[]** - Reduce allocations for large file operations
11. **Consider memory-mapped files** - For very large files that need random access
12. **Batch small operations** - Combine multiple small reads into single I/O when possible

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/FileSystem/Local/
├── LocalFileReadTests.cs
│   ├── Should_Read_Small_File()
│   ├── Should_Read_Empty_File()
│   ├── Should_Read_File_With_Unicode()
│   ├── Should_Read_File_With_BOM()
│   ├── Should_Stream_Large_File()
│   ├── Should_Stream_Very_Large_File()
│   ├── Should_Detect_UTF8_Encoding()
│   ├── Should_Detect_UTF16_Encoding()
│   ├── Should_Detect_Binary_File()
│   ├── Should_Apply_Explicit_Encoding()
│   ├── Should_Handle_Read_Only_File()
│   ├── Should_Handle_Concurrent_Reads()
│   ├── Should_Support_Cancellation()
│   ├── Should_Throw_For_Missing_File()
│   └── Should_Throw_For_Access_Denied()
│
├── LocalFileWriteTests.cs
│   ├── Should_Write_Small_File()
│   ├── Should_Write_Large_File()
│   ├── Should_Write_Empty_File()
│   ├── Should_Write_Unicode_Content()
│   ├── Should_Write_Atomically()
│   ├── Should_Use_Temp_File()
│   ├── Should_Cleanup_Temp_On_Failure()
│   ├── Should_Create_Parent_Directories()
│   ├── Should_Create_Deep_Parent_Directories()
│   ├── Should_Overwrite_Existing_File()
│   ├── Should_Handle_Lock_Conflict()
│   ├── Should_Retry_On_Lock_Conflict()
│   ├── Should_Timeout_On_Locked_File()
│   ├── Should_Preserve_Original_On_Failure()
│   └── Should_Support_Cancellation()
│
├── LocalLineOperationsTests.cs
│   ├── Should_Read_Lines()
│   ├── Should_Read_Lines_Empty_File()
│   ├── Should_Read_Lines_Single_Line()
│   ├── Should_Read_Lines_No_Trailing_Newline()
│   ├── Should_Read_Lines_Mixed_Line_Endings()
│   ├── Should_Write_Lines()
│   ├── Should_Write_Lines_Empty_Array()
│   └── Should_Write_Lines_With_Platform_Newline()
│
├── LocalBinaryOperationsTests.cs
│   ├── Should_Read_Bytes()
│   ├── Should_Read_Bytes_Empty_File()
│   ├── Should_Write_Bytes()
│   └── Should_Write_Bytes_Empty_Array()
│
├── LocalEnumerationTests.cs
│   ├── Should_Enumerate_Files()
│   ├── Should_Enumerate_Empty_Directory()
│   ├── Should_Enumerate_Lazily()
│   ├── Should_Enumerate_Recursively()
│   ├── Should_Enumerate_NonRecursively()
│   ├── Should_Filter_By_Pattern()
│   ├── Should_Filter_By_Extension()
│   ├── Should_Include_Hidden_Files()
│   ├── Should_Exclude_Hidden_Files()
│   ├── Should_Enumerate_Directories()
│   ├── Should_Handle_Deep_Hierarchy()
│   └── Should_Support_Cancellation()
│
├── LocalMetadataTests.cs
│   ├── Should_Check_File_Exists()
│   ├── Should_Check_Directory_Exists()
│   ├── Should_Return_False_For_Missing()
│   ├── Should_Get_File_Size()
│   ├── Should_Get_Last_Modified()
│   ├── Should_Get_Created_Date()
│   ├── Should_Get_IsDirectory_Flag()
│   └── Should_Throw_For_Missing_Metadata()
│
├── LocalDeleteTests.cs
│   ├── Should_Delete_File()
│   ├── Should_Delete_Missing_File_NoError()
│   ├── Should_Delete_Empty_Directory()
│   ├── Should_Delete_Directory_Recursive()
│   ├── Should_Delete_Non_Empty_Directory()
│   └── Should_Throw_For_Locked_File()
│
├── LocalPathHandlingTests.cs
│   ├── Should_Normalize_Forward_Slashes()
│   ├── Should_Normalize_Back_Slashes()
│   ├── Should_Handle_Trailing_Slashes()
│   ├── Should_Combine_With_Root()
│   └── Should_Handle_Case_Sensitivity()
│
└── LocalSecurityTests.cs
    ├── Should_Block_Parent_Traversal()
    ├── Should_Block_Hidden_Traversal()
    ├── Should_Block_Absolute_Path()
    ├── Should_Enforce_Root_Boundary()
    ├── Should_Reject_Null_Bytes()
    └── Should_Reject_Invalid_Characters()
```

### Integration Tests

```
Tests/Integration/FileSystem/Local/
├── LocalFSIntegrationTests.cs
│   ├── Should_Handle_Deep_Hierarchy()
│   ├── Should_Handle_Large_Directory()
│   ├── Should_Handle_Many_Small_Files()
│   ├── Should_Handle_Concurrent_Operations()
│   ├── Should_Handle_Unicode_Filenames()
│   ├── Should_Handle_Long_Paths()
│   └── Should_Handle_Special_Characters()
│
└── LocalAtomicWriteIntegrationTests.cs
    ├── Should_Survive_Process_Crash()
    ├── Should_Handle_Disk_Full()
    └── Should_Handle_Power_Failure()
```

### E2E Tests

```
Tests/E2E/FileSystem/
├── LocalFSE2ETests.cs
│   ├── Should_Read_File_Via_Agent_Tool()
│   ├── Should_Write_File_Via_Agent_Tool()
│   ├── Should_List_Directory_Via_Agent_Tool()
│   └── Should_Handle_Real_Codebase()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| 1KB read | 2ms | 5ms |
| 1MB read | 25ms | 50ms |
| Atomic write 1KB | 5ms | 10ms |
| Enumerate 1000 | 25ms | 50ms |

---

## User Verification Steps

### Scenario 1: Read File

1. Create test file
2. Read via local FS
3. Verify: Content correct

### Scenario 2: Write File

1. Write via local FS
2. Read back
3. Verify: Matches

### Scenario 3: Atomic Write

1. Start write
2. Simulate failure
3. Verify: Original unchanged

### Scenario 4: Enumeration

1. Create directory tree
2. Enumerate
3. Verify: All files found

### Scenario 5: Security

1. Try path traversal
2. Verify: Blocked

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Infrastructure/
├── FileSystem/
│   └── Local/
│       ├── LocalFileSystem.cs
│       ├── LocalFSOptions.cs
│       ├── AtomicFileWriter.cs
│       ├── EncodingDetector.cs
│       └── LargeFileReader.cs
```

### LocalFileSystem Class

```csharp
namespace AgenticCoder.Infrastructure.FileSystem.Local;

public sealed class LocalFileSystem : IRepoFS
{
    private readonly LocalFSOptions _options;
    private readonly PathValidator _validator;
    private readonly EncodingDetector _encoding;
    
    public async Task<string> ReadFileAsync(
        string path,
        CancellationToken ct = default)
    {
        var fullPath = ResolvePath(path);
        _validator.ValidatePath(fullPath, _options.RootPath);
        
        var encoding = await _encoding.DetectAsync(fullPath, ct);
        return await File.ReadAllTextAsync(fullPath, encoding, ct);
    }
    
    public async Task WriteFileAsync(
        string path,
        string content,
        CancellationToken ct = default)
    {
        var fullPath = ResolvePath(path);
        _validator.ValidatePath(fullPath, _options.RootPath);
        
        await _atomicWriter.WriteAsync(fullPath, content, ct);
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-LFS-001 | File not found |
| ACODE-LFS-002 | Permission denied |
| ACODE-LFS-003 | Path traversal |
| ACODE-LFS-004 | Lock timeout |
| ACODE-LFS-005 | Atomic write failed |

### Implementation Checklist

1. [ ] Create LocalFileSystem
2. [ ] Implement reading
3. [ ] Implement atomic writing
4. [ ] Implement enumeration
5. [ ] Add encoding detection
6. [ ] Add path validation
7. [ ] Add error handling
8. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Basic read/write
2. **Phase 2:** Atomic writes
3. **Phase 3:** Enumeration
4. **Phase 4:** Encoding detection
5. **Phase 5:** Error handling

---

**End of Task 014.a Specification**