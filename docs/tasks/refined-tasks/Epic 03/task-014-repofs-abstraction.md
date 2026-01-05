# Task 014: RepoFS Abstraction

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 002 (Config Contract), Task 003 (DI Container), Task 011 (Run Session)  

---

## Description

### Business Value

RepoFS is the foundational file system abstraction that enables Agentic Coding Bot to interact with repository files safely and consistently. This abstraction is critical because:

1. **Platform Independence:** Developers work on Windows, macOS, and Linux. Docker containers add another dimension. Without RepoFS, every file operation would need platform-specific handling scattered throughout the codebase.

2. **Security Boundary:** The agent must NEVER access files outside the repository. A single path traversal vulnerability could expose sensitive system files or credentials. RepoFS provides the security boundary that protects user systems.

3. **Transactional Integrity:** When the agent modifies files, partial failures can corrupt code. RepoFS transactions ensure changes are atomic—either all succeed or all are rolled back.

4. **Testability:** By abstracting file system operations behind an interface, unit tests can use in-memory implementations. This enables fast, reliable testing without touching the actual file system.

5. **Future Extensibility:** The abstraction allows adding new file system types (cloud storage, network shares) without modifying consuming code.

### Scope

This task defines the complete file system abstraction layer:

1. **IRepoFS Interface:** The primary contract for file system operations. Defines reading, writing, deletion, enumeration, metadata, transactions, and patching. All file system implementations MUST implement this interface.

2. **Path Handling:** Normalization and validation of file paths. Handles platform differences (slashes, case sensitivity). Prevents path traversal attacks.

3. **Local File System Implementation:** The primary implementation for native file system access. Optimized for common development scenarios.

4. **Docker File System Implementation:** Enables file operations within Docker containers via mounted volumes or Docker API.

5. **Transaction Support:** Groups multiple file operations into atomic units. Supports commit and rollback.

6. **Patch Application:** Applies unified diff patches to files. Critical for the agent's primary modification mechanism.

7. **Factory Pattern:** Creates appropriate file system instances based on configuration.

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Task 002 (Config) | Configuration | RepoFS settings in `.agent/config.yml` under `repo` section |
| Task 003 (DI) | Dependency Injection | IRepoFS registered as scoped service |
| Task 011 (Session) | Transaction Context | Sessions wrap file operations in transactions |
| Task 015 (Indexing) | Content Access | Indexer reads files via RepoFS for indexing |
| Task 016 (Context) | Context Building | Context packer reads files via RepoFS |
| Task 025 (File Tool) | Tool Operations | File read/write tools use RepoFS |
| Task 050 (Git Sync) | Change Detection | Git operations observe RepoFS changes |
| Task 003.c (Audit) | Audit Logging | All file operations are audited |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| File not found | Read operation fails | Clear error message, file existence check in tooling |
| Permission denied | Cannot read/write | Detect at startup, report permissions issue |
| Disk full | Write fails mid-operation | Transaction rollback, disk space check |
| Path traversal attempt | Security violation | Strict validation, request rejection, audit log |
| Encoding detection fails | Garbled content | Default to UTF-8, warn user |
| Docker container unavailable | Cannot access files | Health check, clear error, retry guidance |
| Transaction timeout | Operation blocked | Configurable timeout, deadlock detection |
| Concurrent modification | Race condition | File locking for writes, optimistic concurrency for reads |
| Long path (Windows) | Operation fails | Detect and warn, suggest path shortening |
| Symbolic link escape | Security violation | Resolve symlinks, validate final path |

### Assumptions

1. The repository is stored on a local or Docker-mounted file system
2. Files are predominantly text (UTF-8) with occasional binary files
3. The agent has read access to all files in the repository
4. Write access may be restricted to certain directories
5. File operations complete in reasonable time (no network latency)
6. The file system supports atomic rename operations (for transactions)
7. File paths are valid for the target platform
8. File sizes are reasonable for in-memory processing (< 10MB typical)
9. Concurrent agents are not modifying the same repository
10. Git ignore patterns are respected for enumeration
11. Docker containers, when used, are already running and accessible
12. File operations complete without network latency (local or mounted)
13. The agent process has sufficient memory for file buffering (10MB+ available)
14. File system supports POSIX-like semantics (or Windows equivalent)
15. Temp directories are available with write permissions
16. Clock synchronization is sufficient for timestamp accuracy
17. File system events (if enabled) are delivered reliably
18. The repository does not contain circular symbolic links
19. Binary file detection heuristics are sufficient for common formats
20. Transaction backup storage has sufficient space (2x modified files)

### ROI and Business Value Metrics

**Quantified Value Proposition:**

| Benefit Area | Without RepoFS | With RepoFS | Annual Savings (10 developers) |
|--------------|----------------|-------------|-------------------------------|
| Platform bugs | 15 hrs/dev fixing path issues | 0 hrs | $16,200 |
| Security incidents | 2 traversal vulnerabilities/year | 0 | $50,000 (breach cost avoided) |
| Test setup time | 30 min/test for file system mocking | 2 min | $14,400 |
| Transaction rollback coding | 8 hrs/dev manual rollback | 0 hrs | $8,640 |
| Docker integration debugging | 20 hrs/dev Docker file issues | 2 hrs | $19,440 |
| **Total Annual ROI** | | | **$108,680** |

**Calculation Methodology:**
- Developer rate: $108/hr (fully loaded cost)
- 10 developers working on agent-related features
- Security breach cost based on industry average for SMB ($50k minimum)
- Test setup savings: 28 min × 5 tests/day × 250 days × 10 developers

---

## Use Cases

### Use Case 1: Multi-Platform Development Team

**Persona:** Sarah, Senior Developer at a 50-person SaaS company
**Context:** Team uses Windows, macOS, and Linux workstations with a shared Git repository

**Before RepoFS:**
Sarah's team constantly dealt with path-related bugs. A colleague's code used `Path.Combine("src", "file.cs")` which worked on Windows but failed on Linux CI. Another developer hard-coded `C:\repo\file.txt` in tests. The codebase was littered with `#if WINDOWS` directives. Every PR required manual testing on all platforms.

**After RepoFS:**
Sarah's team uses RepoFS abstraction exclusively. All file operations go through `IRepoFS`:
```csharp
// Works identically on Windows, macOS, Linux, and Docker
var content = await repoFS.ReadFileAsync("src/services/UserService.cs");
await repoFS.WriteFileAsync("src/generated/models.cs", generatedCode);
```

**Measurable Improvement:**
- Path-related bugs: 12 bugs/quarter → 0 bugs/quarter
- Cross-platform test failures: 8% failure rate → 0.1% failure rate
- Developer time on platform issues: 15 hrs/month → 0 hrs/month
- CI pipeline reliability: 87% → 99.2% first-pass success

---

### Use Case 2: Secure Agent Operations in Financial Services

**Persona:** Marcus, DevSecOps Lead at a banking institution
**Context:** Strict security requirements, audit trail mandatory, zero tolerance for file access violations

**Before RepoFS:**
Marcus's security team discovered the coding agent could access files outside the repository through carefully crafted paths like `../../etc/passwd`. The agent's logs didn't capture file access patterns. There was no way to prove the agent hadn't accessed sensitive files during an incident investigation.

**After RepoFS:**
Marcus implements RepoFS with security hardening:
```yaml
# .agent/config.yml
repo:
  root: /workspace/project
  read_only: false
  security:
    audit_all_operations: true
    reject_symlinks_outside_root: true
    protected_paths:
      - ".env"
      - "secrets/"
      - "credentials/"
```

Every file access is validated and logged:
```
2024-01-15T10:23:45Z INFO [RepoFS] READ src/models/Account.cs user=agent session=abc123
2024-01-15T10:23:46Z WARN [RepoFS] REJECTED path_traversal path=../../etc/passwd user=agent session=abc123
```

**Measurable Improvement:**
- Path traversal vulnerabilities: 3 discovered in audit → 0 possible
- Security incident response time: 4 hours investigation → 15 minutes (complete audit trail)
- Compliance audit findings: 5 file access concerns → 0 concerns
- Security team approval time: 3 months → 2 weeks (clear security model)

---

### Use Case 3: Docker-Based Isolated Development

**Persona:** Jordan, Platform Engineer implementing containerized agent execution
**Context:** Agent must run in Docker container with mounted repository for isolation

**Before RepoFS:**
Jordan's Docker implementation was fragile. File permission issues plagued mounted volumes. The agent couldn't reliably detect whether it was running in Docker or locally. Different teams used different mount configurations, causing inconsistent behavior. When Docker volumes had issues, debugging was a nightmare.

**After RepoFS:**
Jordan deploys RepoFS with Docker-aware configuration:
```yaml
# .agent/config.yml
repo:
  fs_type: auto  # Auto-detects Docker environment
  docker:
    container: agent-sandbox
    mount_path: /workspace
    timeout_seconds: 30
    retry_on_failure: true
```

RepoFS automatically adapts:
```csharp
// Factory auto-detects environment
var repoFS = await repoFSFactory.CreateAsync(config);
// Returns DockerFileSystem when in container, LocalFileSystem otherwise

// Same code works in both environments
await repoFS.WriteFileAsync("output/report.txt", report);
```

**Measurable Improvement:**
- Docker file operation failures: 15% of runs → 0.5% of runs
- Environment detection bugs: 8 bugs/quarter → 0 bugs/quarter
- Developer onboarding (Docker setup): 4 hours → 30 minutes
- CI/CD Docker pipeline failures: 20% → 2% (with proper retry handling)

---

### Security Considerations

RepoFS is a critical security boundary. All file operations MUST implement these security controls.

---

### Threat 1: Path Traversal Attack via Encoded Sequences

**Risk Level:** Critical
**CVSS Score:** 9.1 (Critical)
**Attack Vector:** Input manipulation

**Description:**
An attacker crafts a file path containing encoded traversal sequences (`%2e%2e%2f`, URL-encoded `../`) or Unicode normalization attacks to escape the repository boundary and access system files like `/etc/passwd`, `~/.ssh/id_rsa`, or Windows credential stores.

**Attack Scenario:**
1. Agent receives instruction: "Read file config%2e%2e%2f%2e%2e%2fetc%2fpasswd"
2. Naive decoder converts to "config../../etc/passwd"
3. Path validation happens BEFORE decoding
4. System file exposed to attacker

**Complete Mitigation Implementation:**

```csharp
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace AgenticCoder.Infrastructure.FileSystem;

/// <summary>
/// Validates file paths for security, rejecting all traversal attempts including
/// encoded sequences, Unicode tricks, and null byte injection.
/// </summary>
public sealed class SecurePathValidator
{
    private readonly string _rootPath;
    private readonly ILogger<SecurePathValidator> _logger;

    /// <summary>
    /// Patterns that indicate traversal or injection attempts.
    /// </summary>
    private static readonly Regex[] DangerousPatterns = new[]
    {
        // Basic traversal
        new Regex(@"\.\./", RegexOptions.Compiled),
        new Regex(@"\.\.\\", RegexOptions.Compiled),
        new Regex(@"^\.\.?$", RegexOptions.Compiled),

        // URL-encoded traversal
        new Regex(@"%2e%2e[/%5c]", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"%252e%252e", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"\.%2e[/%5c]", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"%2e\.[/%5c]", RegexOptions.Compiled | RegexOptions.IgnoreCase),

        // Unicode normalization attacks
        new Regex(@"[\u002e][\u002e][\u002f]", RegexOptions.Compiled), // Regular dots/slash
        new Regex(@"[\uff0e][\uff0e][\uff0f]", RegexOptions.Compiled), // Fullwidth ．．／
        new Regex(@"[\u2024][\u2024]", RegexOptions.Compiled),         // One dot leader
        new Regex(@"[\u2025]", RegexOptions.Compiled),                 // Two dot leader
        new Regex(@"[\u2026]", RegexOptions.Compiled),                 // Horizontal ellipsis

        // Windows-specific
        new Regex(@"^[a-zA-Z]:", RegexOptions.Compiled),               // Drive letter
        new Regex(@"^\\\\", RegexOptions.Compiled),                    // UNC path

        // Null byte injection
        new Regex(@"%00", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"\x00", RegexOptions.Compiled),
    };

    public SecurePathValidator(string rootPath, ILogger<SecurePathValidator> logger)
    {
        _rootPath = Path.GetFullPath(rootPath);
        _logger = logger;
    }

    /// <summary>
    /// Validates and normalizes a path, ensuring it cannot escape the root.
    /// </summary>
    public PathValidationResult Validate(string inputPath)
    {
        if (string.IsNullOrEmpty(inputPath))
        {
            return PathValidationResult.Invalid("Path cannot be null or empty");
        }

        // Step 1: Check for dangerous patterns BEFORE any normalization
        foreach (var pattern in DangerousPatterns)
        {
            if (pattern.IsMatch(inputPath))
            {
                _logger.LogWarning(
                    "SECURITY: Path traversal pattern detected in '{Path}'",
                    SanitizeForLogging(inputPath));
                return PathValidationResult.TraversalAttempt(
                    "Path contains dangerous sequences");
            }
        }

        // Step 2: Check for null bytes
        if (inputPath.Contains('\0'))
        {
            _logger.LogWarning(
                "SECURITY: Null byte injection detected in path");
            return PathValidationResult.Invalid("Path contains null bytes");
        }

        // Step 3: Normalize the path
        var normalized = NormalizePath(inputPath);

        // Step 4: Resolve to absolute path
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, normalized));

        // Step 5: Verify the resolved path is within root (defense in depth)
        if (!IsWithinRoot(fullPath))
        {
            _logger.LogWarning(
                "SECURITY: Path '{Normalized}' escapes root after resolution",
                SanitizeForLogging(normalized));
            return PathValidationResult.TraversalAttempt(
                "Path escapes repository boundary");
        }

        // Step 6: Additional check - re-validate normalized path
        if (normalized.Contains(".."))
        {
            _logger.LogWarning(
                "SECURITY: Path still contains '..' after normalization");
            return PathValidationResult.TraversalAttempt(
                "Path contains parent directory reference");
        }

        return PathValidationResult.Valid(normalized, fullPath);
    }

    private string NormalizePath(string path)
    {
        // Convert backslashes to forward slashes
        var normalized = path.Replace('\\', '/');

        // Remove leading ./ or /
        if (normalized.StartsWith("./"))
            normalized = normalized.Substring(2);
        if (normalized.StartsWith("/"))
            normalized = normalized.Substring(1);

        // Collapse multiple slashes
        while (normalized.Contains("//"))
            normalized = normalized.Replace("//", "/");

        // Remove trailing slash
        normalized = normalized.TrimEnd('/');

        return normalized;
    }

    private bool IsWithinRoot(string fullPath)
    {
        // Ensure path starts with root (case-insensitive on Windows)
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return fullPath.StartsWith(_rootPath, comparison) &&
               (fullPath.Length == _rootPath.Length ||
                fullPath[_rootPath.Length] == Path.DirectorySeparatorChar);
    }

    private static string SanitizeForLogging(string path)
    {
        // Don't log full paths - only safe representation
        if (path.Length > 50)
            return path.Substring(0, 47) + "...";
        return path.Replace("\0", "\\0");
    }
}

public sealed record PathValidationResult
{
    public bool IsValid { get; }
    public bool IsTraversalAttempt { get; }
    public string? NormalizedPath { get; }
    public string? FullPath { get; }
    public string? ErrorMessage { get; }

    private PathValidationResult(bool valid, bool traversal, string? normalized,
        string? full, string? error)
    {
        IsValid = valid;
        IsTraversalAttempt = traversal;
        NormalizedPath = normalized;
        FullPath = full;
        ErrorMessage = error;
    }

    public static PathValidationResult Valid(string normalized, string full) =>
        new(true, false, normalized, full, null);

    public static PathValidationResult Invalid(string error) =>
        new(false, false, null, null, error);

    public static PathValidationResult TraversalAttempt(string error) =>
        new(false, true, null, null, error);
}
```

---

### Threat 2: Symlink Escape to External Files

**Risk Level:** High
**CVSS Score:** 7.5 (High)
**Attack Vector:** File system manipulation

**Description:**
An attacker creates a symbolic link within the repository that points to a file outside the repository boundary. When RepoFS follows the symlink, it inadvertently reads or writes to external files, bypassing the security boundary.

**Attack Scenario:**
1. Attacker commits symlink: `repo/config/secrets -> /etc/passwd`
2. Agent is instructed: "Read config/secrets"
3. Symlink is followed, system file exposed
4. Similar attack for writes could corrupt system files

**Complete Mitigation Implementation:**

```csharp
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace AgenticCoder.Infrastructure.FileSystem;

/// <summary>
/// Resolves symbolic links safely, ensuring the final target is within
/// the repository boundary. Rejects links pointing outside.
/// </summary>
public sealed class SafeSymlinkResolver
{
    private readonly string _rootPath;
    private readonly ILogger<SafeSymlinkResolver> _logger;
    private const int MaxSymlinkDepth = 40; // Linux default

    public SafeSymlinkResolver(string rootPath, ILogger<SafeSymlinkResolver> logger)
    {
        _rootPath = Path.GetFullPath(rootPath);
        _logger = logger;
    }

    /// <summary>
    /// Resolves all symbolic links in a path and validates the final target.
    /// </summary>
    public SymlinkResolutionResult ResolvePath(string fullPath)
    {
        var depth = 0;
        var currentPath = fullPath;
        var visitedLinks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (depth < MaxSymlinkDepth)
        {
            // Check if current path is a symlink
            if (!IsSymbolicLink(currentPath))
            {
                // Not a symlink - validate final path
                if (!IsWithinRoot(currentPath))
                {
                    _logger.LogWarning(
                        "SECURITY: Path '{Path}' escapes root after symlink resolution",
                        SanitizeForLog(currentPath));
                    return SymlinkResolutionResult.EscapesRoot(
                        "Resolved path is outside repository boundary");
                }
                return SymlinkResolutionResult.Resolved(currentPath);
            }

            // Detect circular symlinks
            if (!visitedLinks.Add(currentPath))
            {
                _logger.LogWarning(
                    "SECURITY: Circular symlink detected at '{Path}'",
                    SanitizeForLog(currentPath));
                return SymlinkResolutionResult.CircularLink(
                    "Circular symbolic link detected");
            }

            // Read symlink target
            var target = ReadSymlinkTarget(currentPath);
            if (target == null)
            {
                return SymlinkResolutionResult.Error("Cannot read symlink target");
            }

            // Resolve relative symlinks against their parent directory
            if (!Path.IsPathRooted(target))
            {
                var parentDir = Path.GetDirectoryName(currentPath) ?? _rootPath;
                target = Path.GetFullPath(Path.Combine(parentDir, target));
            }
            else
            {
                target = Path.GetFullPath(target);
            }

            // Immediately check if symlink points outside root
            if (!IsWithinRoot(target))
            {
                _logger.LogWarning(
                    "SECURITY: Symlink '{Link}' points outside repository to '{Target}'",
                    SanitizeForLog(currentPath),
                    SanitizeForLog(target));
                return SymlinkResolutionResult.EscapesRoot(
                    "Symbolic link points outside repository boundary");
            }

            currentPath = target;
            depth++;
        }

        _logger.LogWarning(
            "SECURITY: Symlink resolution exceeded maximum depth at '{Path}'",
            SanitizeForLog(fullPath));
        return SymlinkResolutionResult.MaxDepthExceeded(
            $"Symbolic link depth exceeds maximum of {MaxSymlinkDepth}");
    }

    private bool IsSymbolicLink(string path)
    {
        try
        {
            var fileInfo = new FileInfo(path);
            return fileInfo.LinkTarget != null;
        }
        catch
        {
            return false;
        }
    }

    private string? ReadSymlinkTarget(string path)
    {
        try
        {
            var fileInfo = new FileInfo(path);
            return fileInfo.LinkTarget;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to read symlink target for '{Path}'", path);
            return null;
        }
    }

    private bool IsWithinRoot(string fullPath)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return fullPath.StartsWith(_rootPath, comparison) &&
               (fullPath.Length == _rootPath.Length ||
                fullPath[_rootPath.Length] == Path.DirectorySeparatorChar);
    }

    private static string SanitizeForLog(string path) =>
        path.Length > 50 ? path.Substring(0, 47) + "..." : path;
}

public sealed record SymlinkResolutionResult
{
    public bool IsSuccess { get; }
    public string? ResolvedPath { get; }
    public SymlinkFailureReason? FailureReason { get; }
    public string? ErrorMessage { get; }

    private SymlinkResolutionResult(bool success, string? resolved,
        SymlinkFailureReason? reason, string? error)
    {
        IsSuccess = success;
        ResolvedPath = resolved;
        FailureReason = reason;
        ErrorMessage = error;
    }

    public static SymlinkResolutionResult Resolved(string path) =>
        new(true, path, null, null);

    public static SymlinkResolutionResult EscapesRoot(string error) =>
        new(false, null, SymlinkFailureReason.EscapesRoot, error);

    public static SymlinkResolutionResult CircularLink(string error) =>
        new(false, null, SymlinkFailureReason.CircularLink, error);

    public static SymlinkResolutionResult MaxDepthExceeded(string error) =>
        new(false, null, SymlinkFailureReason.MaxDepthExceeded, error);

    public static SymlinkResolutionResult Error(string error) =>
        new(false, null, SymlinkFailureReason.ReadError, error);
}

public enum SymlinkFailureReason
{
    EscapesRoot,
    CircularLink,
    MaxDepthExceeded,
    ReadError
}
```

---

### Threat 3: Transaction Log Manipulation for Corrupt Rollback

**Risk Level:** High
**CVSS Score:** 7.8 (High)
**Attack Vector:** File system race condition

**Description:**
An attacker manipulates transaction backup files during the commit or rollback process. By modifying backup files or creating race conditions, the attacker could cause rollback to restore malicious content instead of the original files.

**Attack Scenario:**
1. Transaction begins, original file backed up to `.tx/backup/file.cs.bak`
2. Attacker overwrites backup: `file.cs.bak` with malicious content
3. Transaction fails (or is forced to fail)
4. Rollback restores malicious backup, not original

**Complete Mitigation Implementation:**

```csharp
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace AgenticCoder.Infrastructure.FileSystem;

/// <summary>
/// Manages transaction backups with integrity verification to prevent
/// backup manipulation attacks.
/// </summary>
public sealed class SecureTransactionBackup : IAsyncDisposable
{
    private readonly string _transactionId;
    private readonly string _backupDir;
    private readonly ILogger<SecureTransactionBackup> _logger;
    private readonly Dictionary<string, BackupRecord> _backups = new();
    private bool _isDisposed;

    public SecureTransactionBackup(
        string transactionId,
        string rootPath,
        ILogger<SecureTransactionBackup> logger)
    {
        _transactionId = transactionId;
        _backupDir = Path.Combine(rootPath, ".agent", "tx", transactionId);
        _logger = logger;

        // Create backup directory with restricted permissions
        Directory.CreateDirectory(_backupDir);

        // On Unix, set directory permissions to owner-only
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(_backupDir,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    /// <summary>
    /// Creates a verified backup of a file before modification.
    /// </summary>
    public async Task<BackupResult> CreateBackupAsync(
        string originalPath,
        CancellationToken ct = default)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SecureTransactionBackup));

        var relativePath = Path.GetRelativePath(_backupDir, originalPath);
        var backupPath = Path.Combine(_backupDir, Guid.NewGuid().ToString("N") + ".bak");

        try
        {
            // Read original content
            byte[] originalContent;
            if (File.Exists(originalPath))
            {
                originalContent = await File.ReadAllBytesAsync(originalPath, ct);
            }
            else
            {
                // File doesn't exist yet - backup is "no file"
                originalContent = Array.Empty<byte>();
            }

            // Compute integrity hash
            var hash = ComputeHash(originalContent);

            // Write backup with restricted permissions
            await File.WriteAllBytesAsync(backupPath, originalContent, ct);

            // On Unix, restrict backup file permissions
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(backupPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }

            // Verify backup was written correctly (defense in depth)
            var verifyContent = await File.ReadAllBytesAsync(backupPath, ct);
            var verifyHash = ComputeHash(verifyContent);

            if (hash != verifyHash)
            {
                _logger.LogError(
                    "SECURITY: Backup verification failed for '{Path}' - possible manipulation",
                    relativePath);
                File.Delete(backupPath);
                return BackupResult.IntegrityFailure("Backup verification failed");
            }

            // Record the backup
            _backups[originalPath] = new BackupRecord(
                originalPath,
                backupPath,
                hash,
                originalContent.Length == 0);

            _logger.LogDebug(
                "Created verified backup for '{Path}' with hash {Hash}",
                relativePath, hash.Substring(0, 8));

            return BackupResult.Success(backupPath, hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup for '{Path}'", relativePath);
            return BackupResult.Error($"Backup failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Restores all backups with integrity verification.
    /// </summary>
    public async Task<RollbackResult> RollbackAsync(CancellationToken ct = default)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SecureTransactionBackup));

        var errors = new List<string>();
        var restored = 0;

        foreach (var (originalPath, record) in _backups)
        {
            try
            {
                // Verify backup integrity before restoring
                if (!File.Exists(record.BackupPath))
                {
                    _logger.LogError(
                        "SECURITY: Backup file missing for '{Path}' - possible deletion attack",
                        originalPath);
                    errors.Add($"Backup missing for {originalPath}");
                    continue;
                }

                var backupContent = await File.ReadAllBytesAsync(record.BackupPath, ct);
                var backupHash = ComputeHash(backupContent);

                if (backupHash != record.OriginalHash)
                {
                    _logger.LogError(
                        "SECURITY: Backup integrity violation for '{Path}' - hash mismatch. " +
                        "Expected {Expected}, got {Actual}. Possible manipulation!",
                        originalPath,
                        record.OriginalHash.Substring(0, 8),
                        backupHash.Substring(0, 8));
                    errors.Add($"Backup corrupted for {originalPath}");
                    continue;
                }

                // Restore the original
                if (record.WasNew)
                {
                    // File didn't exist before - delete it
                    if (File.Exists(originalPath))
                    {
                        File.Delete(originalPath);
                    }
                }
                else
                {
                    // Restore original content
                    await File.WriteAllBytesAsync(originalPath, backupContent, ct);
                }

                restored++;
                _logger.LogDebug("Restored '{Path}' from verified backup", originalPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore '{Path}'", originalPath);
                errors.Add($"Restore failed for {originalPath}: {ex.Message}");
            }
        }

        if (errors.Any())
        {
            return RollbackResult.PartialFailure(restored, errors);
        }

        return RollbackResult.Success(restored);
    }

    /// <summary>
    /// Commits the transaction by cleaning up backups.
    /// </summary>
    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SecureTransactionBackup));

        // Delete all backup files
        foreach (var record in _backups.Values)
        {
            try
            {
                if (File.Exists(record.BackupPath))
                {
                    File.Delete(record.BackupPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up backup '{Path}'", record.BackupPath);
            }
        }

        // Remove backup directory
        try
        {
            if (Directory.Exists(_backupDir))
            {
                Directory.Delete(_backupDir, recursive: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up backup directory");
        }

        _backups.Clear();
    }

    private static string ComputeHash(byte[] content)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(content);
        return Convert.ToHexString(hashBytes);
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        // Auto-rollback on dispose if not committed
        if (_backups.Any())
        {
            await RollbackAsync();
        }
    }

    private sealed record BackupRecord(
        string OriginalPath,
        string BackupPath,
        string OriginalHash,
        bool WasNew);
}

public sealed record BackupResult(bool IsSuccess, string? BackupPath, string? Hash, string? Error)
{
    public static BackupResult Success(string path, string hash) => new(true, path, hash, null);
    public static BackupResult IntegrityFailure(string error) => new(false, null, null, error);
    public static BackupResult Error(string error) => new(false, null, null, error);
}

public sealed record RollbackResult(bool IsSuccess, int RestoredCount, IReadOnlyList<string> Errors)
{
    public static RollbackResult Success(int count) => new(true, count, Array.Empty<string>());
    public static RollbackResult PartialFailure(int count, IEnumerable<string> errors) =>
        new(false, count, errors.ToList());
}
```

---

### Threat 4: Error Message Information Disclosure

**Risk Level:** Medium
**CVSS Score:** 5.3 (Medium)
**Attack Vector:** Information leakage

**Description:**
Detailed error messages expose sensitive system information like absolute paths, usernames, or internal directory structures. An attacker probes the system with invalid paths to map the file system structure through error messages.

**Attack Scenario:**
1. Attacker requests: "Read /very/long/path/that/doesnt/exist"
2. System returns: "File not found: /home/ubuntu/agent-workspace/repo/very/long/path..."
3. Attacker learns: system username is "ubuntu", workspace location, deployment structure
4. Information aids further attacks (path construction, privilege escalation)

**Complete Mitigation Implementation:**

```csharp
using Microsoft.Extensions.Logging;

namespace AgenticCoder.Infrastructure.FileSystem;

/// <summary>
/// Sanitizes error messages to prevent information disclosure while
/// maintaining usefulness for legitimate debugging.
/// </summary>
public sealed class SecureErrorMessageBuilder
{
    private readonly string _rootPath;
    private readonly bool _debugMode;
    private readonly ILogger<SecureErrorMessageBuilder> _logger;

    public SecureErrorMessageBuilder(
        string rootPath,
        bool debugMode,
        ILogger<SecureErrorMessageBuilder> logger)
    {
        _rootPath = Path.GetFullPath(rootPath);
        _debugMode = debugMode;
        _logger = logger;
    }

    /// <summary>
    /// Creates a safe error message for file not found.
    /// </summary>
    public FileSystemException FileNotFound(string requestedPath, string? fullPath = null)
    {
        var safePath = SanitizePath(requestedPath);

        // Log full details internally
        _logger.LogDebug(
            "File not found: requested='{Requested}' resolved='{Resolved}'",
            requestedPath, fullPath ?? "not resolved");

        // Return sanitized message to user
        return new FileSystemException(
            "ACODE-FS-001",
            $"File not found: {safePath}",
            FileSystemErrorType.NotFound);
    }

    /// <summary>
    /// Creates a safe error message for permission denied.
    /// </summary>
    public FileSystemException PermissionDenied(
        string requestedPath,
        FileSystemOperation operation)
    {
        var safePath = SanitizePath(requestedPath);

        _logger.LogWarning(
            "Permission denied for {Operation} on '{Path}'",
            operation, requestedPath);

        return new FileSystemException(
            "ACODE-FS-002",
            $"Permission denied: Cannot {operation.ToString().ToLower()} '{safePath}'",
            FileSystemErrorType.PermissionDenied);
    }

    /// <summary>
    /// Creates a safe error message for path traversal attempt.
    /// Does NOT include the attempted path (security).
    /// </summary>
    public FileSystemException PathTraversalAttempt(string attemptedPath)
    {
        // Log full details for security audit
        _logger.LogWarning(
            "SECURITY: Path traversal attempt detected: '{Path}'",
            attemptedPath);

        // Return vague message - don't help attacker
        return new FileSystemException(
            "ACODE-FS-003",
            "Invalid path: Path must be within the repository",
            FileSystemErrorType.PathTraversal);
    }

    /// <summary>
    /// Creates a safe error message for general I/O errors.
    /// </summary>
    public FileSystemException IoError(
        string requestedPath,
        Exception innerException)
    {
        var safePath = SanitizePath(requestedPath);

        // Log full details internally
        _logger.LogError(innerException,
            "I/O error accessing '{Path}'", requestedPath);

        // Sanitize the error message
        var safeMessage = SanitizeExceptionMessage(innerException.Message);

        return new FileSystemException(
            "ACODE-FS-010",
            $"Error accessing '{safePath}': {safeMessage}",
            FileSystemErrorType.IoError,
            innerException);
    }

    /// <summary>
    /// Sanitizes a path for user-facing output.
    /// </summary>
    private string SanitizePath(string path)
    {
        // In debug mode, show relative path from root
        if (_debugMode)
        {
            try
            {
                var fullPath = Path.GetFullPath(Path.Combine(_rootPath, path));
                if (fullPath.StartsWith(_rootPath))
                {
                    return Path.GetRelativePath(_rootPath, fullPath);
                }
            }
            catch
            {
                // Fall through to safe handling
            }
        }

        // Production: Only show the filename or relative path as-given
        // Never expose full system paths
        if (path.Contains(_rootPath))
        {
            // Strip system path prefix
            path = path.Replace(_rootPath, "");
        }

        // Remove any absolute path indicators
        if (Path.IsPathRooted(path))
        {
            return Path.GetFileName(path);
        }

        // Truncate very long paths
        if (path.Length > 100)
        {
            return "..." + path.Substring(path.Length - 97);
        }

        return path;
    }

    /// <summary>
    /// Sanitizes exception messages to remove sensitive info.
    /// </summary>
    private string SanitizeExceptionMessage(string message)
    {
        // Remove absolute paths
        var sanitized = message;

        // Replace common path patterns with generic placeholders
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"(/[a-zA-Z0-9_\-\.]+)+",
            "[path]");

        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"[A-Z]:\\[a-zA-Z0-9_\-\.\\]+",
            "[path]");

        // Remove usernames from paths
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"/home/[a-zA-Z0-9_\-]+/",
            "/home/[user]/");

        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"C:\\Users\\[a-zA-Z0-9_\-]+\\",
            "C:\\Users\\[user]\\");

        return sanitized;
    }
}

public class FileSystemException : Exception
{
    public string ErrorCode { get; }
    public FileSystemErrorType ErrorType { get; }

    public FileSystemException(
        string errorCode,
        string message,
        FileSystemErrorType errorType,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        ErrorType = errorType;
    }
}

public enum FileSystemErrorType
{
    NotFound,
    PermissionDenied,
    PathTraversal,
    IoError,
    TransactionFailed,
    PatchFailed
}

public enum FileSystemOperation
{
    Read,
    Write,
    Delete,
    List,
    CreateDirectory
}
```

---

### Threat 5: Audit Log Bypass via Concurrent Operations

**Risk Level:** Medium
**CVSS Score:** 5.5 (Medium)
**Attack Vector:** Race condition

**Description:**
An attacker performs file operations during brief windows when the audit logger is unavailable (e.g., during log rotation, high load, or after triggering an exception in the logger). Operations complete without audit trail, enabling undetected malicious activity.

**Attack Scenario:**
1. Attacker floods audit logger with requests, causing backpressure
2. Logger drops events or fails silently
3. Attacker performs sensitive file operations
4. No audit trail exists for forensic investigation

**Complete Mitigation Implementation:**

```csharp
using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace AgenticCoder.Infrastructure.FileSystem;

/// <summary>
/// Reliable audit logger that guarantees audit events are recorded
/// even under high load or failure conditions.
/// </summary>
public sealed class ReliableAuditLogger : IAsyncDisposable
{
    private readonly ILogger<ReliableAuditLogger> _logger;
    private readonly string _auditLogPath;
    private readonly Channel<AuditEvent> _eventChannel;
    private readonly Task _writerTask;
    private readonly ConcurrentQueue<AuditEvent> _failedEvents = new();
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private bool _isDisposed;

    // Fail-closed: If we can't audit, we can't proceed
    private bool _failClosed = true;

    public ReliableAuditLogger(
        string auditLogPath,
        ILogger<ReliableAuditLogger> logger,
        bool failClosed = true)
    {
        _auditLogPath = auditLogPath;
        _logger = logger;
        _failClosed = failClosed;

        // Bounded channel with backpressure
        _eventChannel = Channel.CreateBounded<AuditEvent>(new BoundedChannelOptions(10000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

        // Start background writer
        _writerTask = Task.Run(WriteEventsAsync);

        // Ensure audit directory exists
        var auditDir = Path.GetDirectoryName(auditLogPath);
        if (!string.IsNullOrEmpty(auditDir))
        {
            Directory.CreateDirectory(auditDir);
        }
    }

    /// <summary>
    /// Records a file operation to the audit log.
    /// In fail-closed mode, throws if audit cannot be recorded.
    /// </summary>
    public async Task AuditAsync(
        FileSystemOperation operation,
        string path,
        string? sessionId,
        bool success,
        string? errorMessage = null,
        CancellationToken ct = default)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(ReliableAuditLogger));

        var auditEvent = new AuditEvent
        {
            Timestamp = DateTimeOffset.UtcNow,
            Operation = operation,
            Path = SanitizePath(path),
            SessionId = sessionId ?? "unknown",
            Success = success,
            ErrorMessage = errorMessage,
            EventId = Guid.NewGuid()
        };

        try
        {
            // Attempt to queue the event
            if (!_eventChannel.Writer.TryWrite(auditEvent))
            {
                // Channel is full - apply backpressure
                _logger.LogWarning(
                    "Audit channel full, applying backpressure for event {EventId}",
                    auditEvent.EventId);

                // Wait for space (with timeout)
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(5));

                if (!await _eventChannel.Writer.WaitToWriteAsync(cts.Token))
                {
                    throw new AuditException("Audit channel closed");
                }

                if (!_eventChannel.Writer.TryWrite(auditEvent))
                {
                    throw new AuditException("Failed to write audit event after waiting");
                }
            }

            _logger.LogDebug(
                "Queued audit event {EventId} for {Operation} on {Path}",
                auditEvent.EventId, operation, auditEvent.Path);
        }
        catch (Exception ex) when (ex is not AuditException)
        {
            _logger.LogError(ex,
                "SECURITY: Failed to queue audit event for {Operation} on {Path}",
                operation, path);

            // Store failed event for retry
            _failedEvents.Enqueue(auditEvent);

            if (_failClosed)
            {
                throw new AuditException(
                    $"Cannot proceed without audit: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Synchronously ensures an audit event is written before continuing.
    /// Used for critical operations that must not proceed without audit.
    /// </summary>
    public async Task AuditSyncAsync(
        FileSystemOperation operation,
        string path,
        string? sessionId,
        bool success,
        CancellationToken ct = default)
    {
        var auditEvent = new AuditEvent
        {
            Timestamp = DateTimeOffset.UtcNow,
            Operation = operation,
            Path = SanitizePath(path),
            SessionId = sessionId ?? "unknown",
            Success = success,
            EventId = Guid.NewGuid()
        };

        await _writeLock.WaitAsync(ct);
        try
        {
            await WriteEventToFileAsync(auditEvent);
            _logger.LogDebug(
                "Synchronously wrote audit event {EventId}",
                auditEvent.EventId);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task WriteEventsAsync()
    {
        try
        {
            await foreach (var auditEvent in _eventChannel.Reader.ReadAllAsync())
            {
                try
                {
                    await WriteEventToFileAsync(auditEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to write audit event {EventId}, queueing for retry",
                        auditEvent.EventId);
                    _failedEvents.Enqueue(auditEvent);
                }

                // Periodically retry failed events
                await RetryFailedEventsAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit writer task terminated unexpectedly");
        }
    }

    private async Task WriteEventToFileAsync(AuditEvent auditEvent)
    {
        var line = FormatAuditLine(auditEvent);

        await _writeLock.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(_auditLogPath, line + Environment.NewLine);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task RetryFailedEventsAsync()
    {
        var retryCount = Math.Min(_failedEvents.Count, 10);
        for (var i = 0; i < retryCount; i++)
        {
            if (_failedEvents.TryDequeue(out var failedEvent))
            {
                try
                {
                    await WriteEventToFileAsync(failedEvent);
                    _logger.LogInformation(
                        "Successfully retried audit event {EventId}",
                        failedEvent.EventId);
                }
                catch
                {
                    // Re-queue for later retry
                    _failedEvents.Enqueue(failedEvent);
                    break; // Don't keep trying if writes are failing
                }
            }
        }
    }

    private static string FormatAuditLine(AuditEvent e)
    {
        var status = e.Success ? "SUCCESS" : "FAILED";
        var error = e.ErrorMessage != null ? $" error=\"{e.ErrorMessage}\"" : "";
        return $"{e.Timestamp:O} [{e.EventId}] {status} {e.Operation} " +
               $"path=\"{e.Path}\" session=\"{e.SessionId}\"{error}";
    }

    private static string SanitizePath(string path)
    {
        // Never log absolute system paths
        if (Path.IsPathRooted(path))
        {
            return Path.GetFileName(path);
        }
        return path.Length > 200 ? path.Substring(0, 197) + "..." : path;
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _eventChannel.Writer.Complete();
        await _writerTask;

        // Final retry of failed events
        while (_failedEvents.TryDequeue(out var failedEvent))
        {
            try
            {
                await WriteEventToFileAsync(failedEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "SECURITY: Lost audit event {EventId} on shutdown",
                    failedEvent.EventId);
            }
        }

        _writeLock.Dispose();
    }
}

public sealed record AuditEvent
{
    public DateTimeOffset Timestamp { get; init; }
    public Guid EventId { get; init; }
    public FileSystemOperation Operation { get; init; }
    public required string Path { get; init; }
    public required string SessionId { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

public class AuditException : Exception
{
    public AuditException(string message) : base(message) { }
    public AuditException(string message, Exception inner) : base(message, inner) { }
}
```

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| RepoFS | Repository File System abstraction |
| Local FS | Native file system |
| Docker FS | Docker-mounted file system |
| Patch | Targeted file modification |
| Atomic | All-or-nothing operation |
| Transaction | Grouped operations |
| Rollback | Undo transaction |
| Path Normalization | Consistent path format |
| Traversal Attack | Escaping repo boundary |
| Root Path | Repository root directory |
| Relative Path | Path from root |
| Absolute Path | Full system path |
| File Handle | Open file reference |
| Enumeration | Directory listing |
| Watch | File change monitoring |

---

## Out of Scope

The following items are explicitly excluded from Task 014:

- **Remote file systems** - No network shares
- **Cloud storage** - No S3, Azure Blob
- **Git operations** - Epic 05
- **Symbolic links** - Not supported v1
- **Hard links** - Not supported v1
- **Extended attributes** - Not supported
- **ACLs** - Not supported
- **Compression** - Raw files only
- **Encryption** - Raw files only

---

## Functional Requirements

### IRepoFS Interface (FR-014-01 to FR-014-20)

| ID | Requirement |
|----|-------------|
| FR-014-01 | System MUST define IRepoFS interface |
| FR-014-02 | IRepoFS MUST have RootPath property returning repository root |
| FR-014-03 | IRepoFS MUST be disposable for resource cleanup |
| FR-014-04 | All operations MUST accept CancellationToken |
| FR-014-05 | All operations MUST validate paths before execution |
| FR-014-06 | All write operations MUST be auditable |
| FR-014-07 | Interface MUST support reading files |
| FR-014-08 | Interface MUST support writing files |
| FR-014-09 | Interface MUST support deleting files |
| FR-014-10 | Interface MUST support directory enumeration |
| FR-014-11 | Interface MUST support file existence checking |
| FR-014-12 | Interface MUST support metadata retrieval |
| FR-014-13 | Interface MUST support transactions |
| FR-014-14 | Interface MUST support patch application |
| FR-014-15 | IRepoFS MUST have GetCapabilities method |
| FR-014-16 | Capabilities MUST report read-only mode |
| FR-014-17 | Capabilities MUST report transaction support |
| FR-014-18 | Capabilities MUST report watch support |
| FR-014-19 | IRepoFS MAY support file watching |
| FR-014-20 | Watch events MUST include path and change type |

### Path Handling (FR-014-21 to FR-014-40)

| ID | Requirement |
|----|-------------|
| FR-014-21 | System MUST define IPathNormalizer interface |
| FR-014-22 | Normalize MUST convert backslashes to forward slashes |
| FR-014-23 | Normalize MUST collapse multiple slashes |
| FR-014-24 | Normalize MUST handle ./ (current directory) |
| FR-014-25 | Normalize MUST resolve ../ (parent directory) safely |
| FR-014-26 | Normalize MUST remove trailing slashes |
| FR-014-27 | Normalize MUST handle empty path as root |
| FR-014-28 | System MUST define IPathValidator interface |
| FR-014-29 | Validate MUST reject null paths |
| FR-014-30 | Validate MUST reject empty paths |
| FR-014-31 | Validate MUST reject absolute paths |
| FR-014-32 | Validate MUST reject UNC paths (\\\\server\\share) |
| FR-014-33 | Validate MUST reject paths escaping root via ../ |
| FR-014-34 | Validate MUST reject encoded traversal (%2e%2e) |
| FR-014-35 | Validate MUST reject null bytes in paths |
| FR-014-36 | Validate MUST reject invalid characters |
| FR-014-37 | Validate MUST return normalized path on success |
| FR-014-38 | Validation failure MUST throw PathValidationException |
| FR-014-39 | Exception MUST include sanitized path info |
| FR-014-40 | Exception MUST NOT expose system paths |

### Reading Operations (FR-014-41 to FR-014-55)

| ID | Requirement |
|----|-------------|
| FR-014-41 | ReadFileAsync MUST return file content as string |
| FR-014-42 | ReadFileAsync MUST auto-detect encoding |
| FR-014-43 | ReadFileAsync MUST handle UTF-8 with and without BOM |
| FR-014-44 | ReadFileAsync MUST handle UTF-16 LE and BE |
| FR-014-45 | ReadFileAsync MUST default to UTF-8 if detection fails |
| FR-014-46 | ReadFileAsync MUST throw FileNotFoundException if missing |
| FR-014-47 | ReadFileAsync MUST support cancellation |
| FR-014-48 | ReadLinesAsync MUST return IReadOnlyList<string> |
| FR-014-49 | ReadLinesAsync MUST handle LF, CR, and CRLF |
| FR-014-50 | ReadLinesAsync MUST handle empty files |
| FR-014-51 | ReadLinesAsync MUST handle files without trailing newline |
| FR-014-52 | ReadBytesAsync MUST return raw byte array |
| FR-014-53 | ReadBytesAsync MUST support large files (> 10MB) |
| FR-014-54 | All read operations MUST NOT modify files |
| FR-014-55 | All read operations MUST be thread-safe |

### Writing Operations (FR-014-56 to FR-014-70)

| ID | Requirement |
|----|-------------|
| FR-014-56 | WriteFileAsync MUST write string content |
| FR-014-57 | WriteFileAsync MUST use UTF-8 without BOM |
| FR-014-58 | WriteFileAsync MUST create file if not exists |
| FR-014-59 | WriteFileAsync MUST overwrite existing content |
| FR-014-60 | WriteFileAsync MUST create parent directories |
| FR-014-61 | WriteFileAsync MUST support cancellation |
| FR-014-62 | WriteLinesAsync MUST write lines with configurable newlines |
| FR-014-63 | WriteLinesAsync MUST default to platform line endings |
| FR-014-64 | WriteBytesAsync MUST write raw bytes |
| FR-014-65 | All writes MUST be atomic (temp file + rename) |
| FR-014-66 | Atomic write failure MUST NOT corrupt original |
| FR-014-67 | Write operations MUST acquire file lock |
| FR-014-68 | Lock acquisition MUST timeout (configurable) |
| FR-014-69 | Write operations MUST fire change events |
| FR-014-70 | Write operations MUST be audited |

### Deletion Operations (FR-014-71 to FR-014-80)

| ID | Requirement |
|----|-------------|
| FR-014-71 | DeleteFileAsync MUST remove specified file |
| FR-014-72 | DeleteFileAsync MUST NOT error if file missing |
| FR-014-73 | DeleteFileAsync MUST return bool indicating deletion |
| FR-014-74 | DeleteDirectoryAsync MUST remove directory |
| FR-014-75 | DeleteDirectoryAsync MUST support recursive flag |
| FR-014-76 | Non-recursive MUST fail on non-empty directory |
| FR-014-77 | Recursive MUST remove all contents |
| FR-014-78 | Deletion MUST NOT follow symlinks |
| FR-014-79 | Deletion MUST fire change events |
| FR-014-80 | Deletion MUST be audited |

### Enumeration Operations (FR-014-81 to FR-014-95)

| ID | Requirement |
|----|-------------|
| FR-014-81 | EnumerateFilesAsync MUST return IAsyncEnumerable |
| FR-014-82 | EnumerateFilesAsync MUST yield FileEntry records |
| FR-014-83 | FileEntry MUST include relative path |
| FR-014-84 | FileEntry MUST include file name |
| FR-014-85 | FileEntry MAY include size and modified time |
| FR-014-86 | EnumerateFilesAsync MUST support recursive flag |
| FR-014-87 | EnumerateFilesAsync MUST support glob pattern filter |
| FR-014-88 | EnumerateFilesAsync MUST respect .gitignore patterns |
| FR-014-89 | EnumerateFilesAsync MUST respect .agentignore patterns |
| FR-014-90 | EnumerateDirectoriesAsync MUST return directory entries |
| FR-014-91 | Enumeration MUST skip hidden files by default |
| FR-014-92 | Enumeration MUST have option to include hidden |
| FR-014-93 | Enumeration MUST support cancellation |
| FR-014-94 | Enumeration MUST handle inaccessible directories |
| FR-014-95 | Inaccessible directories MUST be skipped with warning |

### Metadata Operations (FR-014-96 to FR-014-105)

| ID | Requirement |
|----|-------------|
| FR-014-96 | ExistsAsync MUST return bool |
| FR-014-97 | ExistsAsync MUST check both files and directories |
| FR-014-98 | ExistsAsync MUST distinguish file from directory |
| FR-014-99 | GetMetadataAsync MUST return FileMetadata |
| FR-014-100 | FileMetadata MUST include Size in bytes |
| FR-014-101 | FileMetadata MUST include LastModified timestamp |
| FR-014-102 | FileMetadata MUST include CreatedAt timestamp |
| FR-014-103 | FileMetadata MUST include IsReadOnly flag |
| FR-014-104 | FileMetadata MUST include IsDirectory flag |
| FR-014-105 | GetMetadataAsync MUST throw if path not found |

### Transaction Support (FR-014-106 to FR-014-120)

| ID | Requirement |
|----|-------------|
| FR-014-106 | BeginTransactionAsync MUST return IRepoFSTransaction |
| FR-014-107 | IRepoFSTransaction MUST implement IAsyncDisposable |
| FR-014-108 | Transaction MUST buffer all write operations |
| FR-014-109 | CommitAsync MUST apply all buffered writes atomically |
| FR-014-110 | CommitAsync MUST use two-phase commit |
| FR-014-111 | RollbackAsync MUST discard all buffered writes |
| FR-014-112 | Dispose without commit MUST auto-rollback |
| FR-014-113 | Transaction MUST track affected files |
| FR-014-114 | Transaction MUST prevent concurrent transactions |
| FR-014-115 | Transaction MUST support timeout |
| FR-014-116 | Timeout MUST trigger auto-rollback |
| FR-014-117 | Nested transactions MUST throw NotSupportedException |
| FR-014-118 | Transaction MUST create backup of modified files |
| FR-014-119 | Rollback MUST restore backups |
| FR-014-120 | Backups MUST be cleaned after commit |

### Patch Application (FR-014-121 to FR-014-140)

| ID | Requirement |
|----|-------------|
| FR-014-121 | ApplyPatchAsync MUST accept unified diff format |
| FR-014-122 | ApplyPatchAsync MUST return PatchResult |
| FR-014-123 | PatchResult MUST include Success flag |
| FR-014-124 | PatchResult MUST include AffectedFiles list |
| FR-014-125 | PatchResult MUST include Error on failure |
| FR-014-126 | Patch MUST support adding lines |
| FR-014-127 | Patch MUST support removing lines |
| FR-014-128 | Patch MUST support modifying lines |
| FR-014-129 | Patch MUST support context matching |
| FR-014-130 | Patch MUST support multiple hunks |
| FR-014-131 | Patch MUST support multiple files |
| FR-014-132 | Patch MUST support new file creation |
| FR-014-133 | Patch MUST support file deletion |
| FR-014-134 | PreviewPatchAsync MUST show changes without applying |
| FR-014-135 | Preview MUST return line-by-line diff |
| FR-014-136 | ValidatePatchAsync MUST check patch applicability |
| FR-014-137 | Validation MUST check context match |
| FR-014-138 | Validation MUST check file existence |
| FR-014-139 | Patch application MUST be transactional |
| FR-014-140 | Partial patch failure MUST rollback entire patch |

### Factory and Configuration (FR-014-141 to FR-014-155)

| ID | Requirement |
|----|-------------|
| FR-014-141 | System MUST define IRepoFSFactory interface |
| FR-014-142 | CreateAsync MUST return configured IRepoFS |
| FR-014-143 | Factory MUST read config from RepoConfig section |
| FR-014-144 | Factory MUST support "local" fs_type |
| FR-014-145 | Factory MUST support "docker" fs_type |
| FR-014-146 | Factory MUST auto-detect type if not specified |
| FR-014-147 | Auto-detect MUST check for Docker environment |
| FR-014-148 | Local type MUST create LocalFileSystem |
| FR-014-149 | Docker type MUST create DockerFileSystem |
| FR-014-150 | Factory MUST validate root path exists |
| FR-014-151 | Factory MUST validate root is directory |
| FR-014-152 | Factory MUST set read-only mode if configured |
| FR-014-153 | Factory MUST configure ignore patterns |
| FR-014-154 | Factory MUST register for DI as scoped |
| FR-014-155 | Factory MUST log configuration on creation |

---

## Non-Functional Requirements

### Performance (NFR-014-01 to NFR-014-20)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014-01 | Performance | Path normalization MUST complete in < 1ms |
| NFR-014-02 | Performance | Path validation MUST complete in < 1ms |
| NFR-014-03 | Performance | Small file read (< 1KB) MUST complete in < 10ms |
| NFR-014-04 | Performance | Medium file read (1KB-100KB) MUST complete in < 50ms |
| NFR-014-05 | Performance | Large file read (100KB-1MB) MUST complete in < 100ms |
| NFR-014-06 | Performance | Very large file read (> 1MB) MUST complete in < 100ms/MB |
| NFR-014-07 | Performance | File write throughput MUST be > 10MB/s |
| NFR-014-08 | Performance | Directory enumeration MUST handle 1000 files in < 50ms |
| NFR-014-09 | Performance | Directory enumeration MUST handle 10,000 files in < 500ms |
| NFR-014-10 | Performance | Metadata query MUST complete in < 5ms |
| NFR-014-11 | Performance | Existence check MUST complete in < 2ms |
| NFR-014-12 | Performance | Transaction begin MUST complete in < 10ms |
| NFR-014-13 | Performance | Transaction commit MUST complete in < 100ms + write time |
| NFR-014-14 | Performance | Transaction rollback MUST complete in < 50ms |
| NFR-014-15 | Performance | Patch validation MUST complete in < 50ms |
| NFR-014-16 | Performance | Patch application MUST complete in < 100ms typical |
| NFR-014-17 | Performance | Memory allocation per read MUST be < 2x file size |
| NFR-014-18 | Performance | Enumeration MUST use streaming (no full materialization) |
| NFR-014-19 | Performance | Connection pooling MUST be used for Docker FS |
| NFR-014-20 | Performance | File handle caching SHOULD be used for hot paths |

### Security (NFR-014-21 to NFR-014-35)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014-21 | Security | All paths MUST be validated before use |
| NFR-014-22 | Security | Path traversal attempts MUST be rejected |
| NFR-014-23 | Security | Symbolic links MUST be resolved and validated |
| NFR-014-24 | Security | Symlinks escaping root MUST be rejected |
| NFR-014-25 | Security | Encoded path attacks MUST be detected |
| NFR-014-26 | Security | Null bytes in paths MUST be rejected |
| NFR-014-27 | Security | Write operations MUST be audited |
| NFR-014-28 | Security | Delete operations MUST be audited |
| NFR-014-29 | Security | Audit logs MUST NOT contain file content |
| NFR-014-30 | Security | Error messages MUST NOT expose system paths |
| NFR-014-31 | Security | Temporary files MUST be in repo temp directory |
| NFR-014-32 | Security | Temp files MUST have restricted permissions |
| NFR-014-33 | Security | Backup files MUST be cleaned after transaction |
| NFR-014-34 | Security | Docker credentials MUST NOT be logged |
| NFR-014-35 | Security | Read-only mode MUST prevent all writes |

### Reliability (NFR-014-36 to NFR-014-50)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014-36 | Reliability | Atomic writes MUST NOT corrupt on crash |
| NFR-014-37 | Reliability | Transaction commit MUST be crash-safe |
| NFR-014-38 | Reliability | Transaction rollback MUST always succeed |
| NFR-014-39 | Reliability | Partial patch MUST NOT leave files corrupted |
| NFR-014-40 | Reliability | File locks MUST be released on disposal |
| NFR-014-41 | Reliability | File locks MUST timeout after configured period |
| NFR-014-42 | Reliability | Stale locks MUST be detected and cleaned |
| NFR-014-43 | Reliability | Encoding detection MUST handle edge cases |
| NFR-014-44 | Reliability | Binary file detection MUST be accurate |
| NFR-014-45 | Reliability | Large file operations MUST not exhaust memory |
| NFR-014-46 | Reliability | Disk full MUST be detected and handled |
| NFR-014-47 | Reliability | Permission denied MUST be reported clearly |
| NFR-014-48 | Reliability | Network errors (Docker) MUST retry with backoff |
| NFR-014-49 | Reliability | Docker container restart MUST be handled |
| NFR-014-50 | Reliability | Concurrent access MUST be handled safely |

### Maintainability (NFR-014-51 to NFR-014-60)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014-51 | Maintainability | Interface MUST be well-documented |
| NFR-014-52 | Maintainability | All public methods MUST have XML docs |
| NFR-014-53 | Maintainability | Error codes MUST be documented |
| NFR-014-54 | Maintainability | Configuration options MUST be documented |
| NFR-014-55 | Maintainability | Code coverage MUST be > 80% |
| NFR-014-56 | Maintainability | Cyclomatic complexity MUST be < 10 per method |
| NFR-014-57 | Maintainability | Each class MUST have single responsibility |
| NFR-014-58 | Maintainability | Dependencies MUST be injected |
| NFR-014-59 | Maintainability | No static state (testability) |
| NFR-014-60 | Maintainability | Platform-specific code MUST be isolated |

### Observability (NFR-014-61 to NFR-014-70)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014-61 | Observability | All operations MUST log at Debug level |
| NFR-014-62 | Observability | Errors MUST log at Error level with context |
| NFR-014-63 | Observability | Performance metrics MUST be collected |
| NFR-014-64 | Observability | Metrics MUST include operation latency |
| NFR-014-65 | Observability | Metrics MUST include bytes read/written |
| NFR-014-66 | Observability | Metrics MUST include operation count |
| NFR-014-67 | Observability | Metrics MUST include error count |
| NFR-014-68 | Observability | Transaction metrics MUST include commit/rollback ratio |
| NFR-014-69 | Observability | Structured logging MUST be used |
| NFR-014-70 | Observability | Log correlation IDs MUST be propagated |

---

## User Manual Documentation

### Overview

RepoFS (Repository File System) is the abstraction layer that provides safe, consistent file system access for the Agentic Coding Bot. It ensures the agent can read and modify files within a repository while preventing access to files outside the repository boundary.

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Agent Operations                        │
│         (read_file, write_file, list_directory)             │
└─────────────────────────┬───────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│                      IRepoFS Interface                       │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐  │
│  │ Read Operations │  │Write Operations │  │ Transactions│  │
│  └─────────────────┘  └─────────────────┘  └─────────────┘  │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐  │
│  │   Enumeration   │  │    Metadata     │  │   Patching  │  │
│  └─────────────────┘  └─────────────────┘  └─────────────┘  │
└─────────────────────────┬───────────────────────────────────┘
                          │
          ┌───────────────┼───────────────┐
          ▼               ▼               ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│  Path Validator │ │ Path Normalizer │ │  Audit Logger   │
└─────────────────┘ └─────────────────┘ └─────────────────┘
          │               │               │
          └───────────────┼───────────────┘
                          │
          ┌───────────────┴───────────────┐
          ▼                               ▼
┌─────────────────────────┐   ┌─────────────────────────┐
│   LocalFileSystem       │   │   DockerFileSystem      │
│   - Native file I/O     │   │   - Docker API calls    │
│   - Direct disk access  │   │   - Volume mounts       │
│   - File locking        │   │   - Container exec      │
└─────────────────────────┘   └─────────────────────────┘
```

### Configuration

RepoFS is configured in the `.agent/config.yml` file under the `repo` section:

```yaml
# .agent/config.yml
repo:
  # File system type: local, docker, auto (default: auto)
  fs_type: local
  
  # Repository root path (default: current directory)
  root: .
  
  # Read-only mode (default: false)
  read_only: false
  
  # Transaction timeout in seconds (default: 300)
  transaction_timeout_seconds: 300
  
  # File lock timeout in seconds (default: 30)
  lock_timeout_seconds: 30
  
  # Enable file watching (default: false)
  watch_enabled: false
  
  # Ignore patterns (in addition to .gitignore)
  ignore_patterns:
    - "*.log"
    - "node_modules/"
    - ".git/"
  
  # Docker-specific settings (when fs_type: docker)
  docker:
    # Container name or ID
    container: my-container
    
    # Path inside container
    mount_path: /workspace
    
    # Connection timeout in seconds
    timeout_seconds: 10
```

### API Reference

#### Reading Files

```csharp
// Read entire file as string
string content = await repoFS.ReadFileAsync("src/Program.cs", cancellationToken);

// Read file as lines
IReadOnlyList<string> lines = await repoFS.ReadLinesAsync("README.md", cancellationToken);

// Read file as bytes (for binary files)
byte[] bytes = await repoFS.ReadBytesAsync("assets/logo.png", cancellationToken);
```

#### Writing Files

```csharp
// Write string content (creates parent directories if needed)
await repoFS.WriteFileAsync("output/report.txt", content, cancellationToken);

// Write lines with platform-appropriate line endings
await repoFS.WriteLinesAsync("data/items.csv", lines, cancellationToken);

// Write binary content
await repoFS.WriteBytesAsync("output/image.png", bytes, cancellationToken);
```

#### Checking Existence and Metadata

```csharp
// Check if file or directory exists
bool exists = await repoFS.ExistsAsync("src/config.json", cancellationToken);

// Get detailed metadata
FileMetadata meta = await repoFS.GetMetadataAsync("src/Program.cs", cancellationToken);
Console.WriteLine($"Size: {meta.Size} bytes");
Console.WriteLine($"Modified: {meta.LastModified}");
Console.WriteLine($"Read-only: {meta.IsReadOnly}");
```

#### Enumerating Files and Directories

```csharp
// List all files in a directory
await foreach (FileEntry file in repoFS.EnumerateFilesAsync("src", recursive: true))
{
    Console.WriteLine($"{file.Path} ({file.Size} bytes)");
}

// List directories
await foreach (DirectoryEntry dir in repoFS.EnumerateDirectoriesAsync(".", recursive: false))
{
    Console.WriteLine($"Directory: {dir.Path}");
}

// With filtering
await foreach (FileEntry file in repoFS.EnumerateFilesAsync("src", pattern: "*.cs"))
{
    Console.WriteLine($"C# file: {file.Path}");
}
```

#### Deleting Files and Directories

```csharp
// Delete a file (no error if missing)
bool deleted = await repoFS.DeleteFileAsync("temp/cache.json", cancellationToken);

// Delete empty directory
await repoFS.DeleteDirectoryAsync("temp/empty", recursive: false, cancellationToken);

// Delete directory and all contents
await repoFS.DeleteDirectoryAsync("temp/build", recursive: true, cancellationToken);
```

### Transactions

Transactions group multiple file operations into an atomic unit. Either all operations succeed, or all are rolled back.

```csharp
// Using block ensures automatic rollback on exception
await using var transaction = await repoFS.BeginTransactionAsync(cancellationToken);

try
{
    // All writes are buffered
    await repoFS.WriteFileAsync("src/file1.cs", content1);
    await repoFS.WriteFileAsync("src/file2.cs", content2);
    await repoFS.DeleteFileAsync("src/obsolete.cs");
    
    // Commit applies all changes atomically
    await transaction.CommitAsync(cancellationToken);
    Console.WriteLine("All changes applied successfully");
}
catch (Exception ex)
{
    // Rollback is automatic on exception, but can be explicit
    await transaction.RollbackAsync(cancellationToken);
    Console.WriteLine($"Changes rolled back: {ex.Message}");
    throw;
}
```

### Patch Application

The agent primarily modifies files through patches (unified diff format), which enables precise, minimal changes.

```csharp
// Define a patch in unified diff format
var patch = @"
--- a/src/Calculator.cs
+++ b/src/Calculator.cs
@@ -10,6 +10,7 @@ public class Calculator
     public int Add(int a, int b)
     {
+        ArgumentOutOfRangeException.ThrowIfNegative(a);
         return a + b;
     }
 }
";

// Preview patch without applying
PatchPreview preview = await repoFS.PreviewPatchAsync(patch, cancellationToken);
foreach (var change in preview.Changes)
{
    Console.WriteLine($"{change.ChangeType}: {change.Path}");
    Console.WriteLine($"  Lines affected: {change.LinesAdded} added, {change.LinesRemoved} removed");
}

// Validate patch can be applied
ValidationResult validation = await repoFS.ValidatePatchAsync(patch, cancellationToken);
if (!validation.IsValid)
{
    Console.WriteLine($"Patch cannot be applied: {validation.Error}");
    return;
}

// Apply patch (automatically transactional)
PatchResult result = await repoFS.ApplyPatchAsync(patch, cancellationToken);
if (result.Success)
{
    Console.WriteLine($"Applied patch to {result.AffectedFiles.Count} files");
}
else
{
    Console.WriteLine($"Patch failed: {result.Error}");
}
```

### Path Handling

RepoFS normalizes all paths to a consistent format and validates them for security.

```csharp
// All these paths are normalized to "src/utils/helpers.cs"
await repoFS.ReadFileAsync("src/utils/helpers.cs");       // Already normalized
await repoFS.ReadFileAsync("src\\utils\\helpers.cs");     // Windows backslashes
await repoFS.ReadFileAsync("./src/utils/helpers.cs");     // Current directory prefix
await repoFS.ReadFileAsync("src//utils//helpers.cs");     // Double slashes

// These paths are REJECTED (security violations)
await repoFS.ReadFileAsync("../secret.txt");              // Throws PathTraversalException
await repoFS.ReadFileAsync("/etc/passwd");                // Throws PathValidationException
await repoFS.ReadFileAsync("C:\\Windows\\system32");      // Throws PathValidationException
```

### Error Handling

RepoFS uses specific exception types for different error conditions:

```csharp
try
{
    var content = await repoFS.ReadFileAsync(path);
}
catch (FileNotFoundException ex)
{
    // File does not exist
    Console.WriteLine($"File not found: {ex.Path}");
}
catch (PathTraversalException ex)
{
    // Attempted to access file outside repository
    Console.WriteLine($"Security violation: Path traversal attempt");
}
catch (PathValidationException ex)
{
    // Invalid path format
    Console.WriteLine($"Invalid path: {ex.Message}");
}
catch (AccessDeniedException ex)
{
    // Permission denied
    Console.WriteLine($"Access denied: {ex.Path}");
}
catch (TransactionException ex)
{
    // Transaction-related error
    Console.WriteLine($"Transaction error: {ex.Message}");
}
catch (PatchException ex)
{
    // Patch application failed
    Console.WriteLine($"Patch failed: {ex.Message}");
    Console.WriteLine($"Conflict at: {ex.ConflictPath}");
}
```

### CLI Integration

RepoFS is used internally by the agent's file tools. When the agent reads or writes files, it uses RepoFS:

```bash
# Reading a file via agent
$ acode run "Show me the contents of src/Program.cs"

[Tool: read_file]
  Path: src/Program.cs
  Result: (file content displayed)

# Writing a file via agent
$ acode run "Add a comment to the top of Program.cs"

[Tool: apply_patch]
  Path: src/Program.cs
  Result: Patch applied successfully

# Listing directory contents
$ acode run "What files are in the src directory?"

[Tool: list_directory]
  Path: src
  Recursive: false
  Result: 
    - Program.cs (1.2KB)
    - Config.cs (0.8KB)
    - Utils/ (directory)
```

### Troubleshooting

#### File Not Found

**Problem:** `FileNotFoundException` when file should exist

**Diagnosis:**
```bash
$ acode debug fs check-path src/Program.cs
Path: src/Program.cs
Normalized: src/Program.cs
Absolute: /home/user/project/src/Program.cs
Exists: false
Suggestion: Check case sensitivity on Linux systems
```

**Solutions:**
1. Check path is relative to repository root
2. Check case sensitivity (Linux is case-sensitive)
3. Verify file isn't in `.gitignore` or `.agentignore`
4. Check file permissions

#### Path Traversal Blocked

**Problem:** `PathTraversalException` when accessing file

**Cause:** Attempted to access file outside repository root

**Solutions:**
This is intentional security behavior. Files outside the repository cannot be accessed. If you need files from a parent directory:
1. Expand the repository root in configuration
2. Use symbolic links (with caution)
3. Copy required files into the repository

#### Permission Denied

**Problem:** `AccessDeniedException` on read or write

**Diagnosis:**
```bash
$ acode debug fs check-permissions src/readonly.cs
Path: src/readonly.cs
Readable: true
Writable: false
Owner: root
Suggestion: File is owned by different user
```

**Solutions:**
1. Check file ownership and permissions
2. For Docker: check mount options
3. Run agent with appropriate permissions

#### Transaction Timeout

**Problem:** Transaction times out during commit

**Diagnosis:**
```bash
$ acode debug fs list-locks
Active locks:
  - src/large-file.cs (held for 45s)
  - src/another.cs (held for 32s)
```

**Solutions:**
1. Increase `transaction_timeout_seconds` in config
2. Break large transactions into smaller ones
3. Check for external processes locking files

#### Docker Connection Failed

**Problem:** Cannot connect to Docker container

**Diagnosis:**
```bash
$ acode debug fs docker-status
Container: my-container
Status: running
Mount: /workspace accessible
Docker socket: /var/run/docker.sock connected
```

**Solutions:**
1. Verify container is running: `docker ps`
2. Check container name in configuration
3. Verify mount path exists in container
4. Check Docker socket permissions

---

## Acceptance Criteria

### Interface Design (AC-001 to AC-012)

- [ ] AC-001: IRepoFS interface defined with all required methods
- [ ] AC-002: All public methods have XML documentation
- [ ] AC-003: All methods are async (return Task/ValueTask)
- [ ] AC-004: All methods accept CancellationToken parameter
- [ ] AC-005: RootPath property returns repository root as string
- [ ] AC-006: GetCapabilities() returns RepoFSCapabilities record
- [ ] AC-007: Capabilities include IsReadOnly, SupportsTransactions, SupportsWatch flags
- [ ] AC-008: IRepoFS implements IAsyncDisposable for cleanup
- [ ] AC-009: IRepoFSFactory interface defined for creating instances
- [ ] AC-010: IRepoFSTransaction interface defined for transaction support
- [ ] AC-011: All result types use record structs for immutability
- [ ] AC-012: Error codes are documented and consistent (ACODE-FS-XXX format)

### Reading Operations (AC-013 to AC-028)

- [ ] AC-013: ReadFileAsync returns file content as string
- [ ] AC-014: ReadFileAsync auto-detects UTF-8 encoding
- [ ] AC-015: ReadFileAsync handles UTF-8 with and without BOM
- [ ] AC-016: ReadFileAsync handles UTF-16 LE encoding
- [ ] AC-017: ReadFileAsync handles UTF-16 BE encoding
- [ ] AC-018: ReadFileAsync defaults to UTF-8 if detection fails
- [ ] AC-019: ReadFileAsync throws FileNotFoundException for missing files
- [ ] AC-020: ReadFileAsync supports cancellation via CancellationToken
- [ ] AC-021: ReadLinesAsync returns IReadOnlyList<string>
- [ ] AC-022: ReadLinesAsync handles LF line endings
- [ ] AC-023: ReadLinesAsync handles CRLF line endings
- [ ] AC-024: ReadLinesAsync handles CR line endings
- [ ] AC-025: ReadLinesAsync handles empty files (returns empty list)
- [ ] AC-026: ReadLinesAsync handles files without trailing newline
- [ ] AC-027: ReadBytesAsync returns raw byte array
- [ ] AC-028: ReadBytesAsync handles large files (> 10MB) without OOM

### Writing Operations (AC-029 to AC-044)

- [ ] AC-029: WriteFileAsync writes string content successfully
- [ ] AC-030: WriteFileAsync uses UTF-8 without BOM by default
- [ ] AC-031: WriteFileAsync creates file if not exists
- [ ] AC-032: WriteFileAsync overwrites existing file content
- [ ] AC-033: WriteFileAsync creates parent directories automatically
- [ ] AC-034: WriteFileAsync supports cancellation via CancellationToken
- [ ] AC-035: WriteLinesAsync writes lines with configured line endings
- [ ] AC-036: WriteLinesAsync defaults to platform-specific line endings
- [ ] AC-037: WriteLinesAsync can be configured for LF, CRLF, or CR
- [ ] AC-038: WriteBytesAsync writes raw bytes successfully
- [ ] AC-039: All writes are atomic (temp file + rename pattern)
- [ ] AC-040: Atomic write failure does not corrupt original file
- [ ] AC-041: Write operations acquire file locks before modification
- [ ] AC-042: Lock acquisition timeout is configurable (default 30s)
- [ ] AC-043: Write operations fire change events (if watching enabled)
- [ ] AC-044: All write operations are logged to audit system

### Deletion Operations (AC-045 to AC-054)

- [ ] AC-045: DeleteFileAsync removes the specified file
- [ ] AC-046: DeleteFileAsync returns true if file was deleted
- [ ] AC-047: DeleteFileAsync returns false if file did not exist
- [ ] AC-048: DeleteFileAsync does not throw for missing files
- [ ] AC-049: DeleteDirectoryAsync removes empty directories
- [ ] AC-050: DeleteDirectoryAsync with recursive=true removes all contents
- [ ] AC-051: DeleteDirectoryAsync with recursive=false fails on non-empty
- [ ] AC-052: Deletion does not follow symbolic links
- [ ] AC-053: Deletion operations fire change events
- [ ] AC-054: All deletion operations are logged to audit system

### Enumeration Operations (AC-055 to AC-070)

- [ ] AC-055: EnumerateFilesAsync returns IAsyncEnumerable<FileEntry>
- [ ] AC-056: FileEntry includes RelativePath property
- [ ] AC-057: FileEntry includes FileName property
- [ ] AC-058: FileEntry optionally includes Size property
- [ ] AC-059: FileEntry optionally includes LastModified property
- [ ] AC-060: EnumerateFilesAsync supports recursive=false (single directory)
- [ ] AC-061: EnumerateFilesAsync supports recursive=true (all descendants)
- [ ] AC-062: EnumerateFilesAsync supports glob pattern filtering
- [ ] AC-063: EnumerateFilesAsync respects .gitignore patterns
- [ ] AC-064: EnumerateFilesAsync respects .agentignore patterns
- [ ] AC-065: EnumerateDirectoriesAsync returns directory entries
- [ ] AC-066: Enumeration skips hidden files by default
- [ ] AC-067: Enumeration includes hidden files when requested
- [ ] AC-068: Enumeration supports cancellation via CancellationToken
- [ ] AC-069: Enumeration handles inaccessible directories gracefully
- [ ] AC-070: Inaccessible directories are skipped with warning log

### Metadata Operations (AC-071 to AC-080)

- [ ] AC-071: ExistsAsync returns true for existing files
- [ ] AC-072: ExistsAsync returns true for existing directories
- [ ] AC-073: ExistsAsync returns false for non-existent paths
- [ ] AC-074: ExistsAsync distinguishes files from directories
- [ ] AC-075: GetMetadataAsync returns FileMetadata record
- [ ] AC-076: FileMetadata includes Size in bytes
- [ ] AC-077: FileMetadata includes LastModified timestamp (UTC)
- [ ] AC-078: FileMetadata includes CreatedAt timestamp (UTC)
- [ ] AC-079: FileMetadata includes IsReadOnly flag
- [ ] AC-080: FileMetadata includes IsDirectory flag

### Path Validation (AC-081 to AC-095)

- [ ] AC-081: Path normalizer converts backslashes to forward slashes
- [ ] AC-082: Path normalizer collapses multiple slashes
- [ ] AC-083: Path normalizer handles ./ (current directory) prefix
- [ ] AC-084: Path normalizer resolves ../ (parent directory) safely
- [ ] AC-085: Path normalizer removes trailing slashes
- [ ] AC-086: Path normalizer handles empty path as root
- [ ] AC-087: Path validator rejects null paths
- [ ] AC-088: Path validator rejects empty paths
- [ ] AC-089: Path validator rejects absolute paths (Unix and Windows)
- [ ] AC-090: Path validator rejects UNC paths (\\\\server\\share)
- [ ] AC-091: Path validator rejects paths escaping root via ../
- [ ] AC-092: Path validator rejects URL-encoded traversal (%2e%2e)
- [ ] AC-093: Path validator rejects null bytes in paths
- [ ] AC-094: Path validator rejects invalid characters
- [ ] AC-095: PathValidationException includes error code and safe message

### Transaction Support (AC-096 to AC-110)

- [ ] AC-096: BeginTransactionAsync returns IRepoFSTransaction
- [ ] AC-097: IRepoFSTransaction implements IAsyncDisposable
- [ ] AC-098: Transaction buffers all write operations
- [ ] AC-099: CommitAsync applies all buffered writes atomically
- [ ] AC-100: CommitAsync uses two-phase commit for safety
- [ ] AC-101: RollbackAsync discards all buffered writes
- [ ] AC-102: Dispose without commit triggers auto-rollback
- [ ] AC-103: Transaction tracks all affected files
- [ ] AC-104: Concurrent transactions are prevented (throws)
- [ ] AC-105: Transaction timeout is configurable (default 300s)
- [ ] AC-106: Timeout triggers auto-rollback
- [ ] AC-107: Nested transactions throw NotSupportedException
- [ ] AC-108: Transaction creates backup of modified files
- [ ] AC-109: Backup integrity is verified with SHA-256 hash
- [ ] AC-110: Backups are cleaned after successful commit

### Patch Application (AC-111 to AC-125)

- [ ] AC-111: ApplyPatchAsync accepts unified diff format
- [ ] AC-112: ApplyPatchAsync returns PatchResult with Success flag
- [ ] AC-113: PatchResult includes AffectedFiles list
- [ ] AC-114: PatchResult includes Error message on failure
- [ ] AC-115: Patch supports adding lines (+)
- [ ] AC-116: Patch supports removing lines (-)
- [ ] AC-117: Patch supports modifying lines (- then +)
- [ ] AC-118: Patch supports context matching
- [ ] AC-119: Patch supports multiple hunks per file
- [ ] AC-120: Patch supports multiple files in single patch
- [ ] AC-121: Patch supports new file creation (diff /dev/null)
- [ ] AC-122: Patch supports file deletion (diff to /dev/null)
- [ ] AC-123: PreviewPatchAsync shows changes without applying
- [ ] AC-124: ValidatePatchAsync checks patch applicability
- [ ] AC-125: Patch application is wrapped in transaction (atomic)

### Security (AC-126 to AC-140)

- [ ] AC-126: All paths validated before any file operation
- [ ] AC-127: Path traversal attempts rejected with ACODE-FS-003
- [ ] AC-128: URL-encoded traversal attempts detected and rejected
- [ ] AC-129: Unicode normalization attacks detected and rejected
- [ ] AC-130: Symbolic links resolved before path validation
- [ ] AC-131: Symlinks pointing outside root are rejected
- [ ] AC-132: Circular symlinks detected (max depth 40)
- [ ] AC-133: All write operations audited with timestamp and path
- [ ] AC-134: All delete operations audited
- [ ] AC-135: Audit logs do not contain file content
- [ ] AC-136: Error messages do not expose system paths
- [ ] AC-137: Temporary files created in repo .agent/temp directory
- [ ] AC-138: Temp files have restricted permissions (600 on Unix)
- [ ] AC-139: Read-only mode prevents all write/delete operations
- [ ] AC-140: Protected paths (.env, .agent/secrets) require confirmation

### Factory and Configuration (AC-141 to AC-150)

- [ ] AC-141: IRepoFSFactory.CreateAsync creates configured instance
- [ ] AC-142: Factory reads config from repo section in .agent/config.yml
- [ ] AC-143: Factory supports fs_type: local
- [ ] AC-144: Factory supports fs_type: docker
- [ ] AC-145: Factory auto-detects type when fs_type: auto
- [ ] AC-146: Auto-detect checks for Docker environment variables
- [ ] AC-147: Factory validates root path exists and is directory
- [ ] AC-148: Factory sets read-only mode from config
- [ ] AC-149: Factory configures ignore patterns from config
- [ ] AC-150: Factory logs configuration on instance creation

---

## Best Practices

### Interface Design

1. **Keep interfaces minimal** - IRepoFS exposes only essential operations; avoid bloated interfaces
2. **Use async everywhere** - All I/O operations return Task/ValueTask for scalability
3. **Accept cancellation tokens** - Every operation accepts CancellationToken for cooperative cancellation
4. **Return structured results** - Use Result<T> pattern over exceptions for expected failures

### Path Handling

5. **Normalize all paths** - Convert to forward slashes and relative paths at API boundary
6. **Validate path escapes** - Reject paths containing .. that escape workspace root
7. **Handle case sensitivity** - Abstract platform differences in path comparison
8. **Preserve original paths** - Store normalized and original paths for user-facing output

### Implementation Strategy

9. **Composition over inheritance** - Use decorator pattern for caching, logging, tracing
10. **Test via interface** - Write tests against IRepoFS; swap implementations freely
11. **Fail fast on invalid input** - Validate paths before I/O; throw ArgumentException early
12. **Log operations contextually** - Include path, operation, timing in structured logs

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/RepoFS/
├── PathNormalizerTests.cs
│   ├── Should_Normalize_Forward_Slashes()
│   ├── Should_Normalize_Back_Slashes()
│   ├── Should_Normalize_Mixed_Slashes()
│   ├── Should_Handle_Relative_Paths()
│   ├── Should_Handle_Current_Directory_Dot()
│   ├── Should_Collapse_Double_Slashes()
│   ├── Should_Preserve_Leading_Slash()
│   └── Should_Trim_Trailing_Slashes()
│
├── PathValidatorTests.cs
│   ├── Should_Accept_Valid_Relative_Path()
│   ├── Should_Accept_Subdirectory_Path()
│   ├── Should_Accept_Deep_Nested_Path()
│   ├── Should_Reject_Parent_Traversal()
│   ├── Should_Reject_Hidden_Parent_Traversal()
│   ├── Should_Reject_Encoded_Traversal()
│   ├── Should_Reject_Absolute_Path()
│   ├── Should_Reject_UNC_Path()
│   ├── Should_Reject_Null_Path()
│   └── Should_Reject_Empty_Path()
│
├── LocalFileSystemTests.cs
│   ├── ReadFileAsync_Should_Return_Content()
│   ├── ReadFileAsync_Should_Handle_UTF8_BOM()
│   ├── ReadFileAsync_Should_Handle_UTF16()
│   ├── ReadFileAsync_Should_Throw_FileNotFound()
│   ├── ReadFileAsync_Should_Support_Cancellation()
│   ├── ReadLinesAsync_Should_Return_Lines()
│   ├── ReadLinesAsync_Should_Handle_Empty_File()
│   ├── ReadLinesAsync_Should_Handle_No_Trailing_Newline()
│   ├── ReadBytesAsync_Should_Return_Binary()
│   ├── WriteFileAsync_Should_Create_New_File()
│   ├── WriteFileAsync_Should_Overwrite_Existing()
│   ├── WriteFileAsync_Should_Create_Parent_Directories()
│   ├── WriteFileAsync_Should_Use_UTF8_No_BOM()
│   ├── WriteLinesAsync_Should_Write_With_Newlines()
│   ├── WriteBytesAsync_Should_Write_Binary()
│   ├── DeleteFileAsync_Should_Remove_File()
│   ├── DeleteFileAsync_Should_Ignore_Missing()
│   ├── DeleteDirectoryAsync_Should_Remove_Empty()
│   ├── DeleteDirectoryAsync_Should_Remove_Recursive()
│   ├── ExistsAsync_Should_Return_True_For_File()
│   ├── ExistsAsync_Should_Return_True_For_Directory()
│   ├── ExistsAsync_Should_Return_False_For_Missing()
│   ├── GetMetadataAsync_Should_Return_Size()
│   ├── GetMetadataAsync_Should_Return_LastModified()
│   ├── GetMetadataAsync_Should_Return_CreatedDate()
│   └── GetMetadataAsync_Should_Throw_FileNotFound()
│
├── EnumerationTests.cs
│   ├── EnumerateFilesAsync_Should_List_Files()
│   ├── EnumerateFilesAsync_Should_Skip_Directories()
│   ├── EnumerateFilesAsync_Should_Support_Recursive()
│   ├── EnumerateFilesAsync_Should_Apply_Filter()
│   ├── EnumerateFilesAsync_Should_Respect_Ignores()
│   ├── EnumerateFilesAsync_Should_Handle_Empty_Directory()
│   ├── EnumerateDirectoriesAsync_Should_List_Directories()
│   ├── EnumerateDirectoriesAsync_Should_Skip_Files()
│   ├── EnumerateDirectoriesAsync_Should_Support_Recursive()
│   └── EnumerateDirectoriesAsync_Should_Handle_Hidden()
│
├── TransactionTests.cs
│   ├── BeginTransaction_Should_Create_Transaction()
│   ├── Commit_Should_Finalize_Writes()
│   ├── Commit_Should_Be_Atomic()
│   ├── Rollback_Should_Undo_Writes()
│   ├── Rollback_Should_Restore_Original()
│   ├── AutoRollback_On_Exception()
│   ├── AutoRollback_On_Dispose_Without_Commit()
│   ├── Transaction_Should_Handle_Multiple_Files()
│   ├── Transaction_Should_Handle_Create_And_Delete()
│   └── Nested_Transaction_Should_Throw()
│
└── PatchApplicatorTests.cs
    ├── ApplyPatch_Should_Add_Lines()
    ├── ApplyPatch_Should_Remove_Lines()
    ├── ApplyPatch_Should_Modify_Lines()
    ├── ApplyPatch_Should_Handle_Context()
    ├── ApplyPatch_Should_Handle_Multiple_Hunks()
    ├── ApplyPatch_Should_Handle_Multiple_Files()
    ├── ApplyPatch_Should_Create_New_File()
    ├── ApplyPatch_Should_Delete_File()
    ├── ApplyPatch_Should_Fail_On_Mismatch()
    ├── ApplyPatch_Should_Fail_On_Missing_File()
    ├── PreviewPatch_Should_Show_Changes()
    ├── PreviewPatch_Should_Not_Modify()
    ├── ValidatePatch_Should_Accept_Valid()
    └── ValidatePatch_Should_Reject_Malformed()
```

### Integration Tests

```
Tests/Integration/RepoFS/
├── LocalFSIntegrationTests.cs
│   ├── Should_Read_Large_File()
│   ├── Should_Write_Large_File()
│   ├── Should_Handle_Deep_Directory_Tree()
│   ├── Should_Handle_Many_Files()
│   ├── Should_Handle_Unicode_Filenames()
│   ├── Should_Handle_Long_Paths()
│   ├── Should_Handle_Special_Characters()
│   ├── Should_Handle_Concurrent_Reads()
│   ├── Should_Handle_Read_While_Write()
│   └── Should_Survive_Disk_Full()
│
├── TransactionIntegrationTests.cs
│   ├── Should_Rollback_On_Crash()
│   ├── Should_Handle_Concurrent_Transactions()
│   └── Should_Recover_From_Partial_Commit()
│
└── PatchIntegrationTests.cs
    ├── Should_Apply_Real_Git_Diff()
    ├── Should_Handle_Binary_Detection()
    └── Should_Apply_Patch_Atomically()
```

### E2E Tests

```
Tests/E2E/RepoFS/
├── FileToolE2ETests.cs
│   ├── Should_Read_File_Via_Agent_Tool()
│   ├── Should_Write_File_Via_Agent_Tool()
│   ├── Should_List_Directory_Via_Agent_Tool()
│   ├── Should_Apply_Patch_Via_Agent_Tool()
│   └── Should_Handle_Agent_Error_Recovery()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| 1KB file read | 5ms | 10ms |
| 1MB file read | 50ms | 100ms |
| 1000 file enum | 25ms | 50ms |
| Patch apply | 10ms | 25ms |

---

## User Verification Steps

### Scenario 1: Read File

1. Create test file
2. Read via RepoFS
3. Verify: Content matches

### Scenario 2: Write File

1. Write via RepoFS
2. Read back
3. Verify: Content matches

### Scenario 3: Transaction

1. Begin transaction
2. Write file
3. Rollback
4. Verify: File unchanged

### Scenario 4: Apply Patch

1. Create file
2. Apply patch
3. Verify: Changes applied

### Scenario 5: Path Traversal

1. Try reading ../outside.txt
2. Verify: Exception thrown

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── FileSystem/
│   ├── IRepoFS.cs
│   ├── FileMetadata.cs
│   └── PatchResult.cs
│
src/AgenticCoder.Application/
├── FileSystem/
│   └── IRepoFSFactory.cs
│
src/AgenticCoder.Infrastructure/
├── FileSystem/
│   ├── RepoFSFactory.cs
│   ├── PathNormalizer.cs
│   ├── PathValidator.cs
│   ├── Local/
│   │   └── LocalFileSystem.cs
│   ├── Docker/
│   │   └── DockerFileSystem.cs
│   └── Patching/
│       └── UnifiedDiffApplicator.cs
```

### IRepoFS Interface

```csharp
namespace AgenticCoder.Domain.FileSystem;

public interface IRepoFS
{
    // Reading
    Task<string> ReadFileAsync(string path, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ReadLinesAsync(string path, CancellationToken ct = default);
    Task<byte[]> ReadBytesAsync(string path, CancellationToken ct = default);
    
    // Writing
    Task WriteFileAsync(string path, string content, CancellationToken ct = default);
    Task WriteLinesAsync(string path, IEnumerable<string> lines, CancellationToken ct = default);
    Task WriteBytesAsync(string path, byte[] bytes, CancellationToken ct = default);
    
    // Deletion
    Task DeleteFileAsync(string path, CancellationToken ct = default);
    Task DeleteDirectoryAsync(string path, bool recursive, CancellationToken ct = default);
    
    // Enumeration
    IAsyncEnumerable<FileEntry> EnumerateFilesAsync(string path, bool recursive = false);
    IAsyncEnumerable<DirectoryEntry> EnumerateDirectoriesAsync(string path, bool recursive = false);
    
    // Metadata
    Task<bool> ExistsAsync(string path, CancellationToken ct = default);
    Task<FileMetadata> GetMetadataAsync(string path, CancellationToken ct = default);
    
    // Transactions
    Task<IRepoFSTransaction> BeginTransactionAsync(CancellationToken ct = default);
    
    // Patching
    Task<PatchResult> ApplyPatchAsync(string patch, CancellationToken ct = default);
    Task<PatchPreview> PreviewPatchAsync(string patch, CancellationToken ct = default);
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-FS-001 | File not found |
| ACODE-FS-002 | Permission denied |
| ACODE-FS-003 | Path traversal |
| ACODE-FS-004 | Transaction failed |
| ACODE-FS-005 | Patch failed |

### Implementation Checklist

1. [ ] Create IRepoFS interface
2. [ ] Create path normalizer
3. [ ] Create path validator
4. [ ] Implement local FS
5. [ ] Implement transactions
6. [ ] Implement patching
7. [ ] Create factory
8. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Interface and path handling
2. **Phase 2:** Local FS implementation
3. **Phase 3:** Transactions
4. **Phase 4:** Patching
5. **Phase 5:** Factory and DI

---

**End of Task 014 Specification**