# Task 015.a: Ignore Rules + Gitignore Support

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 015 (Indexing v1)  

---

## Description

Task 015.a implements the ignore rule system for indexing. Ignore rules filter what gets indexed. They exclude build artifacts, dependencies, and binary files.

Gitignore is the primary ignore source. Most projects already have .gitignore files. Respecting these means zero configuration for typical projects. The existing rules just work.

The gitignore format is well-defined. Patterns use glob syntax. Negation is supported. Directory-specific rules are supported. The implementation must match git's behavior.

Additional ignore sources are supported. The .agent/config.yml can add patterns. Global ignores can be configured. These layer on top of gitignore.

Pattern precedence follows git conventions. Later patterns override earlier ones. More specific patterns override general ones. Negation patterns re-include excluded files.

Multiple .gitignore files are supported. The root .gitignore applies everywhere. Subdirectory .gitignore files add local rules. This matches how git handles ignores.

Binary file detection is automatic. Images, executables, and archives are detected. They are excluded by default. No explicit patterns needed for common binary types.

The ignore service is used by multiple systems. Indexing uses it. File enumeration uses it. Context packing uses it. A consistent API serves all consumers.

Performance is critical. Ignore checks happen for every file. Compiled patterns enable fast matching. Caching reduces repeated work.

### Business Value

Effective ignore rule support is fundamental to the quality and usability of the indexing system. Without proper ignore rules, the index would be polluted with build artifacts, dependency folders like node_modules, binary files, and other non-essential content that dramatically degrades search relevance and inflates index size. By implementing comprehensive gitignore support, the agent leverages the project's existing configuration, requiring zero additional setup for most repositories.

The business value extends beyond mere file filtering. Proper ignore handling ensures that context provided to the LLM is focused on actual source code and meaningful project files rather than generated or vendored content. This directly impacts response quality, token efficiency, and the agent's ability to understand project structure. Teams already invested in maintaining .gitignore files see immediate benefit without configuration overhead.

Furthermore, the ignore service serves as a foundational component for multiple downstream systems. Search results, context packing, file enumeration, and agent tool responses all depend on consistent ignore behavior. A single, well-tested ignore implementation prevents fragmented logic and ensures uniform behavior across all agent operations.

### ROI Analysis

**Cost of Poor Ignore Handling:**
| Symptom | Impact | Weekly Cost (10-person team) |
|---------|--------|------------------------------|
| node_modules indexed | Search results polluted with 50K+ dependency files | 5 hours wasted filtering results × $75/hr = $375/week |
| Binary files tokenized | Garbled context confuses LLM responses | 8 failed queries/day × 15 min recovery = $1,500/week |
| Build artifacts in context | Outdated compiled code in suggestions | 3 bugs/week from stale context × $300/bug = $900/week |
| Manual ignore config required | Setup overhead for each project | 2 hours/project × 5 new projects/month = $750/month |

**Investment:** 40 hours implementation @ $100/hr = **$4,000 one-time**

**Annual Savings:**
- Search result quality: $375/week × 52 = **$19,500/year**
- LLM response accuracy: $1,500/week × 52 = **$78,000/year**
- Bug prevention: $900/week × 52 = **$46,800/year**
- Zero-config setup: $750/month × 12 = **$9,000/year**

**Total Annual Savings: $153,300/year**
**ROI: 38× investment in Year 1**

**Time Savings Comparison:**

| Workflow | Before (manual ignore) | After (gitignore integration) | Improvement |
|----------|------------------------|------------------------------|-------------|
| Project setup | 45 min configuring ignores | 0 min (auto-detected) | 100% reduction |
| Search filtering | 2-5 min per search | 0 min (pre-filtered) | 100% reduction |
| Context review | 3 min removing junk files | 0 min (clean context) | 100% reduction |
| Debug ignore issues | 30 min per incident | 2 min with CLI tools | 93% reduction |

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              IGNORE RULE SYSTEM                                   │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                   │
│  ┌─────────────────────────────────────────────────────────────────────────────┐ │
│  │                           RULE SOURCES                                       │ │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐ │ │
│  │  │ .gitignore  │  │ Nested      │  │ config.yml  │  │ Built-in Binary     │ │ │
│  │  │ (root)      │  │ .gitignore  │  │ ignores:    │  │ Detection Rules     │ │ │
│  │  │             │  │ files       │  │ - custom    │  │                     │ │ │
│  │  │ node_modules│  │             │  │ - patterns  │  │ .exe, .dll, .png    │ │ │
│  │  │ dist/       │  │ src/tests/  │  │             │  │ .jpg, .zip, .tar    │ │ │
│  │  │ *.log       │  │ .gitignore  │  │             │  │ Magic: 0x7F ELF     │ │ │
│  │  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └──────────┬──────────┘ │ │
│  │         │                │                │                     │            │ │
│  └─────────┼────────────────┼────────────────┼─────────────────────┼────────────┘ │
│            │                │                │                     │              │
│            ▼                ▼                ▼                     ▼              │
│  ┌─────────────────────────────────────────────────────────────────────────────┐ │
│  │                        GITIGNORE PARSER                                      │ │
│  │  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐                    │ │
│  │  │ Lexer         │  │ Pattern       │  │ Negation      │                    │ │
│  │  │ - Comments #  │─▶│ Compiler      │─▶│ Handler       │                    │ │
│  │  │ - Blanks      │  │ - Glob→Regex  │  │ - ! prefix    │                    │ │
│  │  │ - Escapes     │  │ - ** expand   │  │ - Re-include  │                    │ │
│  │  └───────────────┘  └───────────────┘  └───────────────┘                    │ │
│  └────────────────────────────────────┬────────────────────────────────────────┘ │
│                                       │                                          │
│                                       ▼                                          │
│  ┌─────────────────────────────────────────────────────────────────────────────┐ │
│  │                         PATTERN MATCHER                                      │ │
│  │  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐  ┌──────────────┐  │ │
│  │  │ Compiled      │  │ Precedence    │  │ Directory     │  │ Result       │  │ │
│  │  │ Regex Cache   │  │ Resolver      │  │ Scope Check   │  │ Cache        │  │ │
│  │  │               │  │               │  │               │  │              │  │ │
│  │  │ O(1) lookup   │  │ Last wins     │  │ Path contains │  │ LRU 10K      │  │ │
│  │  └───────────────┘  └───────────────┘  └───────────────┘  └──────────────┘  │ │
│  └────────────────────────────────────┬────────────────────────────────────────┘ │
│                                       │                                          │
│                                       ▼                                          │
│  ┌─────────────────────────────────────────────────────────────────────────────┐ │
│  │                          IGNORE SERVICE API                                  │ │
│  │                                                                               │ │
│  │    IsIgnored(path) ──▶ bool                                                  │ │
│  │    CheckIgnored(path) ──▶ IgnoreCheckResult { IsIgnored, Reason, Rule }      │ │
│  │    GetActiveRules() ──▶ IReadOnlyList<IgnoreRule>                            │ │
│  │    AddPattern(pattern, source) ──▶ void                                      │ │
│  │    ClearCache() ──▶ void                                                     │ │
│  │                                                                               │ │
│  └────────────────────────────────────┬────────────────────────────────────────┘ │
│                                       │                                          │
└───────────────────────────────────────┼──────────────────────────────────────────┘
                                        │
                        ┌───────────────┼───────────────┐
                        ▼               ▼               ▼
               ┌─────────────┐  ┌─────────────┐  ┌─────────────┐
               │ Index       │  │ File        │  │ Context     │
               │ Builder     │  │ Enumerator  │  │ Packer      │
               │             │  │             │  │             │
               │ Skip ignored│  │ Filter      │  │ Exclude     │
               │ files       │  │ traversal   │  │ from LLM    │
               └─────────────┘  └─────────────┘  └─────────────┘
```

### Trade-offs and Alternatives

| Decision | Alternative Considered | Chosen Approach | Rationale |
|----------|----------------------|-----------------|-----------|
| Glob-to-regex compilation | Pure glob interpreter | Compile to .NET Regex | Regex engine highly optimized; glob interpretation would require custom state machine; regex caching amortizes compilation cost |
| Pattern caching strategy | No caching (always recompile) | LRU cache with 10K entries | 10K covers typical project; avoids memory bloat; 99%+ hit rate expected |
| Binary detection method | Magic number only | Extension + magic number | Extension check is O(1); magic number fallback handles edge cases; covers 99%+ of binaries |
| Nested .gitignore handling | Single root file only | Full Git-compatible hierarchy | Matches user expectations from git; subdirectory ignores are common in monorepos |
| Case sensitivity | Always case-insensitive | OS-dependent (Windows insensitive, Linux sensitive) | Matches git behavior; avoids cross-platform surprises |
| Invalid pattern handling | Fail fast on any error | Log warning, skip pattern | Graceful degradation preferred; one bad pattern shouldn't break entire ignore system |

**Trade-off 1: Regex Compilation vs. Glob Interpretation**
- **Pro Regex:** .NET regex engine is heavily optimized, handles complex patterns efficiently, provides timeout support
- **Con Regex:** Compilation overhead for first match, complex glob features (brace expansion) need manual handling
- **Decision:** Compile to regex with caching. Compilation cost is ~5ms per pattern, amortized across thousands of matches. Timeout support critical for security.

**Trade-off 2: Full Git Compatibility vs. Simplified Subset**
- **Pro Full Compatibility:** Users expect gitignore to "just work"; no learning curve; existing .gitignore files work unchanged
- **Con Full Compatibility:** Obscure features (escaped wildcards, character classes with negation) add implementation complexity
- **Decision:** Full compatibility. The gitignore format is well-documented and stable. Implementation complexity is bounded. User confusion from subtle incompatibilities would be worse than implementation effort.

**Trade-off 3: Caching Strategy**
- **Pro Aggressive Caching:** File paths are checked repeatedly during indexing/search; cache eliminates 95%+ of redundant work
- **Con Aggressive Caching:** Memory overhead; cache invalidation complexity when rules change
- **Decision:** LRU cache with 10K entries, cleared on rule changes. Memory overhead is ~2MB worst case. Invalidation triggered by file watcher on .gitignore changes.

### Scope

1. **Gitignore Parser** - Full-fidelity parser for .gitignore files supporting all standard syntax including comments, blank lines, wildcards, double wildcards, character classes, negation, and directory-specific patterns
2. **Pattern Matcher** - High-performance glob pattern matching engine with compiled pattern support for fast evaluation against file paths
3. **Binary Detection** - Automatic binary file detection via file extension lookup and magic number inspection for common binary formats
4. **Ignore Service** - Unified API that aggregates rules from multiple sources (.gitignore files, config, global settings) with proper precedence ordering
5. **CLI Commands** - `acode ignore check` and `acode ignore list` commands for debugging and visibility into ignore behavior

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Index Service | Consumer | Calls IsIgnored() during file enumeration to filter indexed content |
| File Enumerator | Consumer | Uses ignore service to exclude files from directory traversal |
| Context Packer | Consumer | Filters context candidates through ignore rules before inclusion |
| Configuration Service | Provider | Supplies additional ignore patterns from .agent/config.yml |
| Search Tools | Consumer | Ensures search results respect ignore rules for consistency |
| CLI Commands | Consumer | Exposes ignore checking and listing for debugging |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Malformed gitignore pattern | Invalid pattern skipped, file may be incorrectly included | Log warning with line number, continue processing remaining patterns |
| Missing .gitignore file | No project-specific ignores applied | Graceful handling with empty rule set, log at debug level only |
| Encoding errors in gitignore | Parser fails to read patterns | Attempt UTF-8 fallback, then Latin-1, log warning and skip problematic lines |
| Recursive pattern explosion | Pattern matching becomes slow | Set maximum recursion depth, timeout on pattern compilation |
| Binary detection false positive | Text file incorrectly excluded | Provide override mechanism via explicit negation patterns |
| Binary detection false negative | Binary file incorrectly indexed | Accept as low-impact; index size increases but functionality preserved |

### Assumptions

1. The .gitignore format follows the specification documented in the Git manual and behavior matches git version 2.x
2. Most repositories will have existing .gitignore files that cover the majority of files that should be excluded
3. Binary file detection by extension covers 95%+ of binary files encountered in typical repositories
4. Magic number detection is only needed for files without extensions or with misleading extensions
5. Pattern matching performance is critical since every file enumeration triggers ignore checks
6. Users expect gitignore behavior to match git exactly - any deviation will cause confusion
7. The .agent/config.yml ignore patterns supplement rather than replace gitignore rules
8. Case sensitivity of pattern matching follows the operating system conventions

### Security Considerations

#### Threat 1: Path Traversal via Malicious Patterns

**Threat:** Attacker-controlled .gitignore patterns containing `..` sequences could be crafted to reference files outside the repository boundary.

**Risk Level:** High

**Mitigation:** Validate and normalize all paths before pattern matching.

```csharp
using System;
using System.IO;

namespace Acode.Infrastructure.Ignore;

/// <summary>
/// Validates and normalizes paths to prevent traversal attacks.
/// </summary>
public sealed class PathValidator
{
    private readonly string _repositoryRoot;
    private readonly string _normalizedRoot;

    public PathValidator(string repositoryRoot)
    {
        _repositoryRoot = Path.GetFullPath(repositoryRoot);
        _normalizedRoot = _repositoryRoot.TrimEnd(Path.DirectorySeparatorChar) 
            + Path.DirectorySeparatorChar;
    }

    /// <summary>
    /// Validates that a path is within the repository boundary.
    /// </summary>
    /// <param name="relativePath">The relative path to validate.</param>
    /// <returns>True if path is safe, false if traversal detected.</returns>
    public bool IsWithinRepository(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        // Reject explicit traversal attempts
        if (relativePath.Contains(".."))
        {
            return false;
        }

        // Normalize and verify containment
        var fullPath = Path.GetFullPath(
            Path.Combine(_repositoryRoot, relativePath));
        
        return fullPath.StartsWith(_normalizedRoot, 
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Normalizes a pattern, removing dangerous sequences.
    /// </summary>
    public string NormalizePattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return string.Empty;
        }

        // Remove any traversal sequences
        var sanitized = pattern
            .Replace("..", "")
            .Replace("\\", "/");
        
        // Remove leading slashes that could indicate absolute paths
        while (sanitized.StartsWith("//") || sanitized.StartsWith("/.."))
        {
            sanitized = sanitized.Substring(1);
        }

        return sanitized;
    }
}
```

#### Threat 2: Denial of Service via Regex Backtracking

**Threat:** Maliciously crafted glob patterns could be converted to regex patterns with catastrophic backtracking, causing CPU exhaustion.

**Risk Level:** High

**Mitigation:** Use timeout protection and pattern complexity limits.

```csharp
using System;
using System.Text.RegularExpressions;
using System.Threading;

namespace Acode.Infrastructure.Ignore;

/// <summary>
/// Safe pattern compiler with backtracking protection.
/// </summary>
public sealed class SafePatternCompiler
{
    private const int MaxPatternLength = 1000;
    private const int MaxWildcardCount = 50;
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Compiles a glob pattern to regex with safety limits.
    /// </summary>
    /// <param name="globPattern">The glob pattern to compile.</param>
    /// <returns>Compiled regex or null if pattern is unsafe.</returns>
    public Regex? CompileGlobToRegex(string globPattern)
    {
        // Length check
        if (globPattern.Length > MaxPatternLength)
        {
            throw new PatternTooLongException(
                $"Pattern exceeds maximum length of {MaxPatternLength} characters");
        }

        // Wildcard count check (prevent exponential matching)
        var wildcardCount = CountWildcards(globPattern);
        if (wildcardCount > MaxWildcardCount)
        {
            throw new PatternTooComplexException(
                $"Pattern has {wildcardCount} wildcards, maximum is {MaxWildcardCount}");
        }

        // Convert to regex with atomic groups to prevent backtracking
        var regexPattern = ConvertGlobToSafeRegex(globPattern);

        try
        {
            return new Regex(
                regexPattern,
                RegexOptions.Compiled | RegexOptions.CultureInvariant,
                MatchTimeout);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidPatternException(
                $"Invalid pattern: {globPattern}", ex);
        }
    }

    /// <summary>
    /// Safely matches a pattern against text with timeout.
    /// </summary>
    public bool SafeMatch(Regex pattern, string text)
    {
        try
        {
            return pattern.IsMatch(text);
        }
        catch (RegexMatchTimeoutException)
        {
            // Log timeout and return safe default (not ignored)
            return false;
        }
    }

    private int CountWildcards(string pattern)
    {
        int count = 0;
        foreach (char c in pattern)
        {
            if (c == '*' || c == '?' || c == '[')
            {
                count++;
            }
        }
        return count;
    }

    private string ConvertGlobToSafeRegex(string glob)
    {
        // Use atomic groups (?>) to prevent backtracking
        var escaped = Regex.Escape(glob);
        
        return "^" + escaped
            .Replace(@"\*\*", "(?>.*)")  // ** matches anything (atomic)
            .Replace(@"\*", "(?>[^/]*)")  // * matches non-slash (atomic)
            .Replace(@"\?", ".")          // ? matches single char
            + "$";
    }
}

public class PatternTooLongException : Exception
{
    public PatternTooLongException(string message) : base(message) { }
}

public class PatternTooComplexException : Exception
{
    public PatternTooComplexException(string message) : base(message) { }
}

public class InvalidPatternException : Exception
{
    public InvalidPatternException(string message, Exception inner) 
        : base(message, inner) { }
}
```

#### Threat 3: Symbolic Link Escape

**Threat:** Symlinks within the repository could point outside the repository, allowing ignore checks to access unauthorized files.

**Risk Level:** Medium

**Mitigation:** Detect and handle symlinks explicitly.

```csharp
using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Ignore;

/// <summary>
/// Handles symbolic link detection and safe resolution.
/// </summary>
public sealed class SymlinkHandler
{
    private readonly string _repositoryRoot;
    private readonly ILogger<SymlinkHandler> _logger;
    private readonly bool _followSymlinks;

    public SymlinkHandler(
        string repositoryRoot,
        ILogger<SymlinkHandler> logger,
        bool followSymlinks = false)
    {
        _repositoryRoot = Path.GetFullPath(repositoryRoot);
        _logger = logger;
        _followSymlinks = followSymlinks;
    }

    /// <summary>
    /// Determines if a path is a symbolic link.
    /// </summary>
    public bool IsSymbolicLink(string path)
    {
        var fileInfo = new FileInfo(path);
        return fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
    }

    /// <summary>
    /// Resolves a path, handling symlinks safely.
    /// </summary>
    /// <param name="path">The path to resolve.</param>
    /// <returns>Resolved path if safe, null if outside repository.</returns>
    public string? SafeResolve(string path)
    {
        if (!IsSymbolicLink(path))
        {
            return path;
        }

        if (!_followSymlinks)
        {
            _logger.LogDebug("Skipping symlink (follow disabled): {Path}", path);
            return null;
        }

        try
        {
            var target = Path.GetFullPath(
                new FileInfo(path).LinkTarget ?? path);

            // Verify target is within repository
            if (!target.StartsWith(_repositoryRoot))
            {
                _logger.LogWarning(
                    "Symlink escapes repository: {Path} -> {Target}",
                    path, target);
                return null;
            }

            // Recursively check for chained symlinks (max depth 10)
            return SafeResolveRecursive(target, 10);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, 
                "Failed to resolve symlink: {Path}", path);
            return null;
        }
    }

    private string? SafeResolveRecursive(string path, int maxDepth)
    {
        if (maxDepth <= 0)
        {
            _logger.LogWarning(
                "Symlink chain too deep: {Path}", path);
            return null;
        }

        if (!IsSymbolicLink(path))
        {
            return path;
        }

        var target = Path.GetFullPath(
            new FileInfo(path).LinkTarget ?? path);

        if (!target.StartsWith(_repositoryRoot))
        {
            return null;
        }

        return SafeResolveRecursive(target, maxDepth - 1);
    }
}
```

#### Threat 4: Sensitive File Exposure via Magic Number Inspection

**Threat:** Reading file headers for binary detection could expose sensitive data in memory or trigger security monitoring.

**Risk Level:** Low

**Mitigation:** Limit header reads and clear buffers after use.

```csharp
using System;
using System.IO;
using System.Security.Cryptography;

namespace Acode.Infrastructure.Ignore;

/// <summary>
/// Safe binary file detection with limited file access.
/// </summary>
public sealed class SafeBinaryDetector : IDisposable
{
    private const int MaxHeaderBytes = 512;
    private readonly byte[] _buffer;
    private bool _disposed;

    // Magic number signatures for common binary formats
    private static readonly (byte[] Signature, string Format)[] MagicNumbers =
    {
        (new byte[] { 0x89, 0x50, 0x4E, 0x47 }, "PNG"),
        (new byte[] { 0xFF, 0xD8, 0xFF }, "JPEG"),
        (new byte[] { 0x47, 0x49, 0x46 }, "GIF"),
        (new byte[] { 0x50, 0x4B, 0x03, 0x04 }, "ZIP"),
        (new byte[] { 0x1F, 0x8B }, "GZIP"),
        (new byte[] { 0x4D, 0x5A }, "PE/EXE"),
        (new byte[] { 0x7F, 0x45, 0x4C, 0x46 }, "ELF"),
        (new byte[] { 0xCF, 0xFA, 0xED, 0xFE }, "Mach-O"),
        (new byte[] { 0x25, 0x50, 0x44, 0x46 }, "PDF"),
    };

    public SafeBinaryDetector()
    {
        _buffer = new byte[MaxHeaderBytes];
    }

    /// <summary>
    /// Detects if a file is binary using magic number inspection.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <returns>True if binary, false if text or unknown.</returns>
    public bool IsBinaryFile(string filePath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: MaxHeaderBytes,
                FileOptions.SequentialScan);

            var bytesRead = stream.Read(_buffer, 0, MaxHeaderBytes);
            
            if (bytesRead == 0)
            {
                return false; // Empty file is text
            }

            // Check magic numbers
            foreach (var (signature, _) in MagicNumbers)
            {
                if (bytesRead >= signature.Length &&
                    StartsWith(_buffer, signature))
                {
                    return true;
                }
            }

            // Check for null bytes (binary indicator)
            for (int i = 0; i < bytesRead; i++)
            {
                if (_buffer[i] == 0x00)
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            // On any error, assume text (safe default)
            return false;
        }
        finally
        {
            // Clear buffer to prevent data leakage
            CryptographicOperations.ZeroMemory(_buffer);
        }
    }

    private static bool StartsWith(byte[] data, byte[] prefix)
    {
        for (int i = 0; i < prefix.Length; i++)
        {
            if (data[i] != prefix[i])
            {
                return false;
            }
        }
        return true;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            CryptographicOperations.ZeroMemory(_buffer);
            _disposed = true;
        }
    }
}
```

#### Threat 5: Configuration Injection via Pattern Strings

**Threat:** Pattern strings loaded from .agent/config.yml could contain escape sequences or special characters that cause unexpected behavior.

**Risk Level:** Medium

**Mitigation:** Validate and sanitize configuration patterns.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Ignore;

/// <summary>
/// Validates patterns from configuration to prevent injection.
/// </summary>
public sealed class PatternValidator
{
    private readonly ILogger<PatternValidator> _logger;
    
    // Characters allowed in patterns
    private static readonly Regex SafePatternChars = new(
        @"^[\w\-./\*\?\[\]!]+$",
        RegexOptions.Compiled);
    
    // Dangerous sequences to reject
    private static readonly string[] DangerousSequences =
    {
        "..",           // Path traversal
        "//",           // Double slash could mean root
        "$(", "${",     // Shell expansion
        "`",            // Command substitution
        "\0",           // Null byte
        "\r", "\n",     // Line injection
    };

    public PatternValidator(ILogger<PatternValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates a pattern from configuration.
    /// </summary>
    /// <param name="pattern">The pattern to validate.</param>
    /// <returns>Validated pattern or null if invalid.</returns>
    public string? ValidatePattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return null;
        }

        // Check for dangerous sequences
        foreach (var seq in DangerousSequences)
        {
            if (pattern.Contains(seq))
            {
                _logger.LogWarning(
                    "Pattern contains dangerous sequence '{Seq}': {Pattern}",
                    seq, pattern);
                return null;
            }
        }

        // Verify only safe characters
        if (!SafePatternChars.IsMatch(pattern))
        {
            _logger.LogWarning(
                "Pattern contains invalid characters: {Pattern}", pattern);
            return null;
        }

        // Length limit
        if (pattern.Length > 500)
        {
            _logger.LogWarning(
                "Pattern exceeds maximum length: {Pattern}", pattern);
            return null;
        }

        return pattern;
    }

    /// <summary>
    /// Validates a list of patterns, returning only valid ones.
    /// </summary>
    public IReadOnlyList<string> ValidatePatterns(IEnumerable<string> patterns)
    {
        return patterns
            .Select(ValidatePattern)
            .Where(p => p != null)
            .ToList()!;
    }
}
```

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Gitignore | A file named `.gitignore` that specifies patterns for files and directories that should be excluded from version control and, in this context, from indexing and search operations |
| Ignore Rule | A pattern specification that defines which files or directories should be excluded from processing, matching files based on glob syntax, path location, or file type |
| Glob Pattern | A wildcard pattern syntax using special characters like `*` (match any characters), `?` (match single character), `**` (match any path segments), and `[abc]` (character classes) |
| Negation Pattern | A pattern prefixed with `!` that re-includes previously excluded files, allowing exceptions to broader exclusion rules |
| Pattern Precedence | The ordering rules that determine which pattern takes effect when multiple patterns could match a file; later patterns override earlier ones, and more specific paths override general patterns |
| Binary File | A file containing non-textual data such as images, executables, archives, or compiled code that cannot be meaningfully tokenized for text search |
| Magic Number | A sequence of bytes at the beginning of a file that identifies its format (e.g., `0x7F 0x45 0x4C 0x46` for ELF executables, `0x89 0x50 0x4E 0x47` for PNG images) |
| Anchored Pattern | A pattern that begins with `/` indicating it only matches relative to the location of the .gitignore file, not in subdirectories |
| Directory Pattern | A pattern ending with `/` that explicitly matches only directories, not files with the same name |
| Compiled Pattern | An ignore pattern that has been converted from glob syntax to an optimized regex representation for faster matching during repeated evaluations |
| Pattern Cascade | The hierarchical application of ignore rules from multiple sources, where nested .gitignore files add rules that apply only to their directory subtree |
| Global Ignore | System-wide ignore patterns configured in `~/.config/git/ignore` or via `core.excludesFile` that apply to all repositories |
| Local Ignore | Ignore patterns specified in a repository's .gitignore file or subdirectory .gitignore files that apply only within that repository |
| Rule Inheritance | The behavior where ignore rules from parent directories continue to apply to subdirectories unless explicitly negated |
| Pattern Cache | An in-memory LRU cache storing the results of ignore checks to avoid repeated pattern matching for the same file paths |

---

## Use Cases

### Use Case 1: DevBot Indexes a Node.js Monorepo

**Persona:** DevBot, an AI coding assistant, is processing a large enterprise monorepo for the first time. The repository contains 15 microservices, each with its own node_modules, dist, and coverage directories.

**Before (Manual Ignore Configuration):**
DevBot begins indexing without ignore rule support. The indexing process runs for 47 minutes as it processes 2.3 million files. The resulting index is 4.7 GB and contains 89% dependency code from node_modules. When DevBot searches for "UserService", it returns 847 results, with 812 being false positives from lodash, express, and other dependencies. The developer wastes 15 minutes scrolling through irrelevant results. Context sent to the LLM includes minified bundle code, causing confused responses about implementation patterns.

**After (Gitignore Integration):**
DevBot detects the root `.gitignore` which contains:
```gitignore
node_modules/
dist/
coverage/
*.log
.env*
```

Additionally, each microservice has its own `.gitignore` with service-specific exclusions. DevBot's ignore service compiles all 183 patterns from 16 .gitignore files into optimized regex matchers. Indexing completes in 4 minutes, processing only 42,000 actual source files. The index is 89 MB. Searching for "UserService" returns 35 relevant results, all in the team's actual code. LLM context contains only meaningful source, producing accurate implementation suggestions.

**Quantified Improvement:**
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Index time | 47 min | 4 min | 91% faster |
| Index size | 4.7 GB | 89 MB | 98% smaller |
| Search results | 847 | 35 | 96% noise reduction |
| Time to find relevant code | 15 min | 30 sec | 97% faster |
| LLM response accuracy | 23% | 94% | 4× improvement |

---

### Use Case 2: Security Analyst Audits Ignore Behavior

**Persona:** Jordan, a security-conscious DevOps engineer, needs to verify that sensitive files are properly excluded from the agent's index before deploying to production.

**Before (No Debug Visibility):**
Jordan has no way to verify what files the agent will index. They manually search for `.env` files in the index and find several exposed API keys. They try adding patterns to .gitignore but can't tell if they're working correctly. A security incident occurs when the agent includes database credentials in LLM context.

**After (CLI Debug Tools):**
Jordan uses the ignore debugging commands:

```bash
$ acode ignore check .env.production
Ignored: YES
  Rule: *.env* (source: .gitignore, line 12)
  Type: glob pattern match

$ acode ignore check src/config/secrets.ts
Ignored: NO
  No matching rules found

$ acode ignore list --source gitignore
Active Ignore Rules (47 total):
  .gitignore:3    node_modules/
  .gitignore:4    dist/
  .gitignore:12   *.env*
  .gitignore:13   *.pem
  .gitignore:14   *.key
  ...
```

Jordan identifies that `secrets.ts` is not ignored and adds a negation pattern. They re-run the check to confirm. The production deployment proceeds with confidence.

**Quantified Improvement:**
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Time to audit ignore rules | 2+ hours | 10 min | 92% faster |
| Confidence in security | Low (guessing) | High (verified) | Qualitative |
| Security incidents | 1 per quarter | 0 | 100% prevention |
| Pattern debugging time | 30 min per issue | 2 min per issue | 93% faster |

---

### Use Case 3: Alex Configures Project-Specific Ignores

**Persona:** Alex, a team lead, manages a mixed-language project with custom build artifacts that aren't covered by standard .gitignore patterns.

**Before (Standard Gitignore Only):**
Alex's project generates `.agenix` temporary files from a custom code generator. These files pollute search results and confuse the LLM. Adding them to .gitignore would affect git status, which the team doesn't want. Alex has no way to tell the agent to ignore these files without modifying version-controlled files.

**After (Config.yml Supplementary Ignores):**
Alex creates `.agent/config.yml` with additional ignore patterns:

```yaml
ignore:
  patterns:
    - "*.agenix"
    - "generated/**"
    - ".cache/"
  include:  # Negation - re-include specific files
    - "!generated/schema.ts"  # This one is important
```

The ignore service loads these patterns as supplementary rules, applied after .gitignore. The custom generator files are excluded while the important schema file remains indexed. Git status is unaffected since .agent/ is already in .gitignore.

**Quantified Improvement:**
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Custom file exclusion | Impossible | 5 min setup | ∞ improvement |
| Search noise from generator | 200+ results | 0 | 100% elimination |
| Team workflow disruption | Required git changes | None | Zero impact |
| Time to configure | N/A (not possible) | 5 minutes | Enabled new capability |

---

### Use Case 4: Handling Deeply Nested Monorepo Structure

**Persona:** Robin, a developer working on a microservices platform, encounters complex ignore scenarios with nested .gitignore files that override parent rules.

**Before (Flat Ignore Processing):**
Robin's monorepo has a root .gitignore excluding `*.log`. However, the `services/logging/` directory has a local .gitignore that re-includes `*.log` files for its test fixtures. A flat ignore processor doesn't respect the nested override, incorrectly excluding the test fixtures and causing CI failures.

**After (Full Git-Compatible Hierarchy):**
The ignore service processes .gitignore files hierarchically:

1. Root `.gitignore`: `*.log` (exclude all .log files)
2. `services/logging/.gitignore`: `!*.log` (re-include in this directory)
3. `services/logging/tests/.gitignore`: `!fixtures/**` (re-include test fixtures)

When checking `services/logging/tests/fixtures/access.log`:
- Root rule says: IGNORE (matches *.log)
- Parent override says: INCLUDE (negation in services/logging/)
- Test fixtures are correctly indexed

CI passes. Robin's test fixture logs are searchable. Log files elsewhere remain excluded.

**Quantified Improvement:**
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Nested override support | Broken | Working | Critical fix |
| CI failures from wrong ignores | 3/week | 0 | 100% elimination |
| Developer confusion | High | None | Matches git behavior |
| Time debugging ignore issues | 1 hr/incident | 0 | 100% prevention |

---

## Out of Scope

The following items are explicitly excluded from Task 015.a:

1. **.gitattributes Processing** - The `.gitattributes` file controls file-level Git settings (line endings, diff drivers, merge strategies) but does not define ignore patterns. This task focuses solely on .gitignore and related ignore mechanisms.

2. **Git LFS Integration** - Large File Storage (LFS) uses pointer files and external storage. LFS-tracked files will be indexed like regular files; no special LFS handling or binary fetching is implemented.

3. **Sparse Checkout Support** - Git's sparse checkout feature selectively populates the working directory. This task operates on the full file system view and does not integrate with sparse checkout patterns.

4. **Submodule .gitignore Processing** - While the ignore service will respect the parent repository's .gitignore, it will not recurse into git submodules to discover and apply their internal .gitignore files. Submodules are treated as opaque directories.

5. **Custom Binary Detection Plugins** - Binary file detection uses a fixed set of file extensions and magic number signatures. Custom detection rules or user-defined binary file types beyond the built-in list are not supported.

6. **Regex Pattern Syntax** - Ignore patterns use glob syntax exclusively. Direct regular expression patterns are not supported, maintaining compatibility with standard .gitignore format.

7. **Remote Ignore File Fetching** - Ignore files must be present in the local file system. The system will not fetch ignore patterns from remote repositories, URLs, or external configuration services.

8. **Cross-Repository Ignore Sharing** - Each repository is processed independently. There is no mechanism to share or inherit ignore patterns across multiple repositories in a workspace.

9. **Ignore Pattern Conflict Resolution UI** - When patterns conflict (e.g., include and exclude same file), the system applies standard precedence rules silently. No interactive conflict resolution or warning UI is provided.

10. **Performance Profiling Dashboard** - While performance is optimized, there is no built-in dashboard or real-time metrics for monitoring ignore service performance. Logging provides basic timing information.

---

## Functional Requirements

### Gitignore Parsing

| ID | Requirement |
|----|-------------|
| FR-015a-01 | The system MUST parse .gitignore files from the repository root and any subdirectories |
| FR-015a-02 | The parser MUST correctly handle comment lines beginning with # character |
| FR-015a-03 | The parser MUST skip blank lines without error |
| FR-015a-04 | The parser MUST handle trailing spaces in patterns correctly per git specification |
| FR-015a-05 | The parser MUST support escape characters for special characters in patterns |

### Pattern Syntax

| ID | Requirement |
|----|-------------|
| FR-015a-06 | Simple literal patterns MUST match files with exact names |
| FR-015a-07 | Single wildcard (*) MUST match any sequence of characters within a path component |
| FR-015a-08 | Double wildcard (**) MUST match zero or more directories in the path |
| FR-015a-09 | Question mark (?) MUST match exactly one character |
| FR-015a-10 | Character class ([abc], [a-z]) MUST match any single character in the class |
| FR-015a-11 | Negation patterns (!) MUST re-include previously excluded files |
| FR-015a-12 | Trailing slash (/) MUST indicate the pattern matches directories only |

### Pattern Matching

| ID | Requirement |
|----|-------------|
| FR-015a-13 | The matcher MUST support matching against file names only |
| FR-015a-14 | The matcher MUST support matching against full relative paths |
| FR-015a-15 | The matcher MUST correctly identify directory matches when pattern ends with slash |
| FR-015a-16 | Case sensitivity MUST follow the operating system conventions (case-insensitive on Windows) |
| FR-015a-17 | Path separators MUST be normalized to forward slashes internally |

### Multiple Gitignores

| ID | Requirement |
|----|-------------|
| FR-015a-18 | The root .gitignore MUST apply to all files in the repository |
| FR-015a-19 | Nested .gitignore files MUST apply only to their directory subtree |
| FR-015a-20 | Pattern precedence MUST follow git rules: later patterns override earlier ones |
| FR-015a-21 | Child directory patterns MUST inherit parent directory patterns |

### Additional Sources

| ID | Requirement |
|----|-------------|
| FR-015a-22 | The system MUST support additional ignore patterns from .agent/config.yml |
| FR-015a-23 | The system MUST support a global ignore file configured by the user |
| FR-015a-24 | The system MUST support command-line specified ignore patterns |
| FR-015a-25 | Source precedence MUST be: gitignore < global < config < command-line |

### Binary Detection

| ID | Requirement |
|----|-------------|
| FR-015a-26 | The system MUST detect binary files by file extension |
| FR-015a-27 | The system MUST detect binary files by magic number signature in file header |
| FR-015a-28 | Detected binary files MUST be automatically added to the ignore list |
| FR-015a-29 | Binary detection MUST be overridable via explicit negation patterns |

### API

| ID | Requirement |
|----|-------------|
| FR-015a-30 | IsIgnored(path) MUST return true if the path matches any ignore pattern |
| FR-015a-31 | GetIgnores() MUST return all currently active ignore patterns with their sources |
| FR-015a-32 | AddIgnore(pattern, source) MUST add a new pattern to the ignore list at runtime |
| FR-015a-33 | Refresh() MUST reload all ignore patterns from disk sources |

### Performance

| ID | Requirement |
|----|-------------|
| FR-015a-34 | Patterns MUST be compiled to optimized form on first load |
| FR-015a-35 | IsIgnored results MUST be cached for repeated calls with the same path |
| FR-015a-36 | Ignore sources MUST be loaded lazily when first accessed |
| FR-015a-37 | The API MUST support batch checking of multiple paths in a single call |

### CLI Commands

| ID | Requirement |
|----|-------------|
| FR-015a-38 | The system MUST provide an `acode ignore check <path>` command to test if a path is ignored |
| FR-015a-39 | The ignore check command MUST display the matching rule and source file when a path is ignored |
| FR-015a-40 | The system MUST provide an `acode ignore list` command to display all active ignore patterns |
| FR-015a-41 | The ignore list command MUST support filtering by source (gitignore, config, global) |
| FR-015a-42 | The ignore list command MUST display patterns in precedence order with source file and line number |
| FR-015a-43 | The CLI commands MUST support JSON output format for scripting integration |

### Error Handling

| ID | Requirement |
|----|-------------|
| FR-015a-44 | The parser MUST continue processing remaining patterns when encountering an invalid pattern |
| FR-015a-45 | Invalid patterns MUST be logged with the source file path and line number |
| FR-015a-46 | File read errors MUST be caught and logged without crashing the ignore service |
| FR-015a-47 | Pattern compilation timeouts MUST be handled gracefully with pattern skip and warning |
| FR-015a-48 | The system MUST validate pattern syntax before compilation to prevent regex injection |

### Cache Management

| ID | Requirement |
|----|-------------|
| FR-015a-49 | The cache MUST be invalidated when any .gitignore file changes |
| FR-015a-50 | The cache MUST use an LRU eviction policy to limit memory usage |
| FR-015a-51 | The cache size MUST be configurable via .agent/config.yml |
| FR-015a-52 | The system MUST provide a ClearCache() method for explicit cache invalidation |
| FR-015a-53 | Cache statistics (hit rate, size, evictions) MUST be available for monitoring |

### File System Integration

| ID | Requirement |
|----|-------------|
| FR-015a-54 | The system MUST detect .gitignore file changes via file system watcher when available |
| FR-015a-55 | The system MUST support manual refresh when file watchers are not available |
| FR-015a-56 | Symlinked .gitignore files MUST be resolved and processed correctly |
| FR-015a-57 | The system MUST handle concurrent access to the ignore service thread-safely |
| FR-015a-58 | File path comparisons MUST use normalized paths to handle platform differences |

### Configuration

| ID | Requirement |
|----|-------------|
| FR-015a-59 | The config.yml ignore section MUST support a patterns array for additional exclusions |
| FR-015a-60 | The config.yml ignore section MUST support an include array for negation patterns |
| FR-015a-61 | The system MUST support disabling binary detection via configuration |
| FR-015a-62 | The system MUST support extending the binary file extension list via configuration |
| FR-015a-63 | Configuration changes MUST trigger cache invalidation and pattern recompilation |

---

## Non-Functional Requirements

### Performance

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015a-01 | Performance | Single IsIgnored() check MUST complete in less than 1ms average |
| NFR-015a-02 | Performance | Batch checking of 1,000 paths MUST complete in less than 50ms |
| NFR-015a-03 | Performance | Loading and compiling all ignore patterns MUST complete in less than 50ms |

### Reliability

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015a-04 | Reliability | Invalid patterns MUST be skipped with warning rather than failing the entire parse |
| NFR-015a-05 | Reliability | Missing .gitignore files MUST be handled gracefully without errors |
| NFR-015a-06 | Reliability | Non-UTF-8 encoded gitignore files MUST be handled with fallback encoding |

### Compatibility

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015a-07 | Compatibility | Pattern matching behavior MUST match git's behavior for all documented patterns |
| NFR-015a-08 | Compatibility | Path handling MUST work correctly on Windows, macOS, and Linux |
| NFR-015a-09 | Compatibility | Case sensitivity MUST respect operating system file system conventions |

### Maintainability

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015a-10 | Maintainability | The ignore service MUST have a clear interface for extension with new pattern sources |
| NFR-015a-11 | Maintainability | Pattern matching logic MUST be unit testable in isolation from file system |
| NFR-015a-12 | Maintainability | All gitignore parsing edge cases MUST be covered by unit tests |

### Observability

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015a-13 | Observability | Pattern compilation failures MUST be logged with file and line number |
| NFR-015a-14 | Observability | Cache hit/miss ratios MUST be available for performance monitoring |
| NFR-015a-15 | Observability | The ignore check command MUST show which pattern caused a file to be ignored |

---

## User Manual Documentation

### Overview

The ignore system filters files from indexing. It uses gitignore patterns and additional configuration.

### Gitignore Patterns

Standard gitignore syntax is supported:

```gitignore
# Comments start with #

# Ignore all .log files
*.log

# Ignore build directory
build/

# Ignore node_modules anywhere
**/node_modules

# But not this specific file
!important.log

# Ignore files in root only
/config.local.json

# Ignore .env in any directory
**/.env
```

### Configuration

```yaml
# .agent/config.yml
index:
  ignore:
    # Additional patterns
    patterns:
      - "*.generated.cs"
      - ".idea/**"
      - ".vs/**"
      
    # Use gitignore (default: true)
    use_gitignore: true
    
    # Global ignore file
    global_file: ~/.config/acode/ignore
    
    # Auto-ignore binaries (default: true)
    auto_ignore_binaries: true
```

### Binary File Detection

These extensions are auto-ignored:
- Images: .png, .jpg, .gif, .ico, .svg
- Archives: .zip, .tar, .gz, .7z
- Binaries: .exe, .dll, .so, .dylib
- Media: .mp3, .mp4, .wav
- Office: .pdf, .doc, .xls

### Checking Ignores

```bash
$ acode ignore check src/Program.cs

src/Program.cs: NOT ignored

$ acode ignore check node_modules/lodash/package.json

node_modules/lodash/package.json: IGNORED
  Matched: **/node_modules

$ acode ignore list

Ignore Rules
────────────────────
Source: .gitignore
  node_modules/
  *.log
  build/
  
Source: .agent/config.yml
  *.generated.cs
  
Source: auto-binary
  (37 extensions)
```

### Troubleshooting

#### Issue 1: File Not Ignored When It Should Be

**Symptoms:**
- File appears in search results despite having a matching pattern in .gitignore
- `acode ignore check <path>` returns "NOT ignored"
- Index includes files from node_modules or build directories

**Causes:**
- Pattern syntax error (e.g., missing trailing slash for directories)
- Pattern order issue - a later negation pattern is re-including the file
- Pattern is in a child .gitignore but checking from parent directory
- Case sensitivity mismatch on case-sensitive file systems
- Pattern is not properly anchored (matches subdirectory only)

**Solutions:**

```bash
# 1. Verify the pattern matches using the check command
acode ignore check node_modules/lodash/index.js --verbose

# 2. List all patterns to see the full rule set
acode ignore list --json | jq '.patterns[] | select(.pattern | contains("node_modules"))'

# 3. Check if there's a negation pattern overriding
acode ignore list | grep "!"

# 4. Add directory trailing slash for directory patterns
# WRONG: node_modules
# RIGHT: node_modules/

# 5. For rooted patterns, add leading slash
/build/    # Only matches build at repository root
build/     # Matches build anywhere in tree
```

---

#### Issue 2: File Unexpectedly Ignored

**Symptoms:**
- Important source files missing from search results
- `acode ignore check <path>` returns "IGNORED" for files that should be included
- Specific files not appearing in LLM context

**Causes:**
- Broad wildcard pattern matching unintended files (e.g., `*.local` ignoring `.local` files)
- Parent .gitignore has a pattern affecting child directories
- Binary detection false positive (file extension in binary list)
- Pattern from global ignore or config.yml overriding

**Solutions:**

```bash
# 1. Check which pattern is causing the exclusion
acode ignore check src/config.local.ts --verbose
# Output shows: Matched by pattern "*.local" from .gitignore:15

# 2. Add a negation pattern to re-include specific file
echo "!src/config.local.ts" >> .gitignore

# 3. Or use more specific pattern in original ignore
# Replace: *.local
# With: *.local.json

# 4. Check if binary detection is the cause
acode ignore check important.data --verbose
# If source shows "AutoBinary", the file extension is in binary list

# 5. Disable binary detection in config.yml
echo "ignore:" >> .agent/config.yml
echo "  detectBinary: false" >> .agent/config.yml
```

---

#### Issue 3: Binary File Not Being Auto-Detected

**Symptoms:**
- Binary files (images, executables) appearing in search results
- Index size unexpectedly large
- Garbled content in search snippets

**Causes:**
- File extension not in the built-in binary extension list
- File has no extension and magic number not recognized
- Binary detection disabled in configuration
- File is small and doesn't have recognizable magic number

**Solutions:**

```bash
# 1. Check current binary extension list
acode config show | grep binaryExtensions

# 2. Add custom extensions to config.yml
cat >> .agent/config.yml << 'EOF'
ignore:
  binaryExtensions:
    - ".blend"   # Blender files
    - ".psd"     # Photoshop files
    - ".sketch"  # Sketch files
EOF

# 3. Add explicit ignore patterns for custom binary types
echo "*.blend" >> .gitignore
echo "*.psd" >> .gitignore

# 4. Verify binary detection is enabled
acode config show | grep detectBinary
# Should show: true

# 5. Force re-index after config changes
acode index rebuild
```

---

#### Issue 4: Slow Pattern Matching Performance

**Symptoms:**
- Indexing takes much longer than expected
- `acode ignore check` commands are slow (> 100ms)
- High CPU usage during file enumeration
- Log warnings about pattern timeouts

**Causes:**
- Patterns with excessive wildcards causing regex explosion
- Very large number of patterns (> 500)
- Cache disabled or not working correctly
- Deeply nested directory structure with many .gitignore files

**Solutions:**

```bash
# 1. Check pattern count and complexity
acode ignore list --json | jq '.patterns | length'
# If > 500, consider consolidating patterns

# 2. Look for problematic patterns with excessive wildcards
acode ignore list | grep -E '\*\*.*\*\*.*\*\*'
# Patterns with multiple ** segments are expensive

# 3. Check cache statistics
acode config show | grep -A5 ignoreCache
# cacheHitRate should be > 90%

# 4. Increase cache size if needed
cat >> .agent/config.yml << 'EOF'
ignore:
  cacheSize: 50000  # Default is 10000
EOF

# 5. Simplify complex patterns
# SLOW: **/src/**/test/**/fixtures/**/*.json
# FAST: src/**/fixtures/*.json
```

---

#### Issue 5: Nested Gitignore Not Applying Correctly

**Symptoms:**
- Patterns in subdirectory .gitignore not taking effect
- Negation in child .gitignore not overriding parent pattern
- Confusion about which .gitignore file is being used

**Causes:**
- Path relative to wrong directory (patterns are relative to gitignore location)
- Missing leading slash making pattern match wrong files
- Parent pattern not properly scoped
- Gitignore file not discovered (hidden, symlink, or permission issue)

**Solutions:**

```bash
# 1. List all discovered gitignore files
acode ignore list --json | jq '.patterns | group_by(.sourceFile) | map({file: .[0].sourceFile, count: length})'

# 2. Check if specific gitignore was loaded
acode ignore list | grep "services/api/.gitignore"

# 3. Verify gitignore file permissions
ls -la services/api/.gitignore

# 4. Understand pattern scoping
# In /services/api/.gitignore:
# "temp/"     -> Matches /services/api/temp/ and /services/api/src/temp/
# "/temp/"    -> Only matches /services/api/temp/ (anchored to gitignore location)

# 5. Use verbose check to see full resolution chain
acode ignore check services/api/src/temp/cache.json --verbose
# Shows all patterns evaluated and which file they came from
```

---

#### Issue 6: Configuration Ignore Patterns Not Working

**Symptoms:**
- Patterns added to .agent/config.yml not being applied
- `acode ignore list` doesn't show config-based patterns
- Changes to config.yml not taking effect

**Causes:**
- YAML syntax error in config.yml
- Wrong configuration key path
- Config file not in expected location
- Cache not invalidated after config change

**Solutions:**

```bash
# 1. Validate config.yml syntax
acode config validate
# Or use yamllint: yamllint .agent/config.yml

# 2. Check correct config structure
cat .agent/config.yml
# Should look like:
# ignore:
#   patterns:
#     - "*.tmp"
#     - "cache/"

# 3. Verify config file location
ls -la .agent/config.yml

# 4. Clear cache to force reload
acode cache clear

# 5. Check if config source appears in list
acode ignore list --json | jq '.patterns[] | select(.source == "Config")'

# 6. Ensure patterns array uses correct format (list, not inline)
# WRONG: ignore: { patterns: ["*.tmp"] }
# RIGHT:
# ignore:
#   patterns:
#     - "*.tmp"
```

---

## Acceptance Criteria

### Category 1: Gitignore Parsing

- [ ] AC-001: System parses .gitignore files from repository root correctly
- [ ] AC-002: Parser handles comment lines starting with # without error
- [ ] AC-003: Parser handles blank lines without error
- [ ] AC-004: Parser handles trailing whitespace in patterns correctly
- [ ] AC-005: Parser handles escaped special characters (\ before #, !)
- [ ] AC-006: Parser handles UTF-8 encoded gitignore files
- [ ] AC-007: Parser handles Windows CRLF line endings
- [ ] AC-008: Parser handles Unix LF line endings
- [ ] AC-009: Parser handles files with BOM (byte order mark)
- [ ] AC-010: Parser returns line numbers for each pattern for diagnostics

### Category 2: Glob Pattern Syntax

- [ ] AC-011: Literal patterns match exact file names (e.g., "README.md")
- [ ] AC-012: Single wildcard * matches any sequence except path separator
- [ ] AC-013: Double wildcard ** matches zero or more directories
- [ ] AC-014: Question mark ? matches exactly one character
- [ ] AC-015: Character class [abc] matches any single character in set
- [ ] AC-016: Character range [a-z] matches any character in range
- [ ] AC-017: Negated class [!abc] matches any character not in set
- [ ] AC-018: Trailing slash / indicates directory-only match
- [ ] AC-019: Leading slash / anchors pattern to repository root
- [ ] AC-020: Pattern "*.log" matches "error.log" and "logs/error.log"

### Category 3: Negation Patterns

- [ ] AC-021: Negation pattern !important.log re-includes excluded file
- [ ] AC-022: Negation only applies to files matched by previous patterns
- [ ] AC-023: Multiple sequential negations work correctly
- [ ] AC-024: Negation of directory pattern works (!build/)
- [ ] AC-025: Negation order matters (last match wins)
- [ ] AC-026: Negation pattern with wildcard works (!*.keep)

### Category 4: Multiple Gitignore Files

- [ ] AC-027: Root .gitignore applies to entire repository
- [ ] AC-028: Nested .gitignore applies only to its subtree
- [ ] AC-029: Child patterns override parent patterns
- [ ] AC-030: Deep nesting (3+ levels) works correctly
- [ ] AC-031: Missing .gitignore in directory handled gracefully
- [ ] AC-032: Gitignore file discovery is efficient (not re-reading every check)

### Category 5: Additional Sources

- [ ] AC-033: Patterns from .agent/config.yml are loaded and applied
- [ ] AC-034: Global ignore file (~/.config/acode/ignore) is supported
- [ ] AC-035: Command-line --ignore patterns are applied
- [ ] AC-036: Source precedence is: gitignore < global < config < CLI
- [ ] AC-037: Config patterns supplement (not replace) gitignore
- [ ] AC-038: Invalid config patterns are logged and skipped

### Category 6: Binary File Detection

- [ ] AC-039: Image extensions (.png, .jpg, .gif, .ico, .webp) auto-ignored
- [ ] AC-040: Archive extensions (.zip, .tar, .gz, .7z, .rar) auto-ignored
- [ ] AC-041: Binary extensions (.exe, .dll, .so, .dylib) auto-ignored
- [ ] AC-042: Media extensions (.mp3, .mp4, .wav, .avi) auto-ignored
- [ ] AC-043: Office extensions (.pdf, .doc, .xls, .ppt) auto-ignored
- [ ] AC-044: Magic number detection identifies PNG files correctly
- [ ] AC-045: Magic number detection identifies ELF binaries correctly
- [ ] AC-046: Magic number detection identifies PE/EXE correctly
- [ ] AC-047: Binary auto-detection can be disabled via config
- [ ] AC-048: Binary files can be force-included via negation pattern

### Category 7: Ignore Service API

- [ ] AC-049: IsIgnored(path) returns true for ignored files
- [ ] AC-050: IsIgnored(path) returns false for non-ignored files
- [ ] AC-051: IsIgnored handles both relative and absolute paths
- [ ] AC-052: IsIgnored normalizes path separators internally
- [ ] AC-053: GetPatterns() returns all active patterns with sources
- [ ] AC-054: AddPattern() adds runtime pattern that persists during session
- [ ] AC-055: Refresh() reloads all patterns from disk
- [ ] AC-056: BatchIsIgnored() checks multiple paths efficiently

### Category 8: CLI Commands

- [ ] AC-057: `acode ignore check <path>` shows ignored status
- [ ] AC-058: Check command shows which pattern caused exclusion
- [ ] AC-059: Check command shows pattern source (file and line)
- [ ] AC-060: `acode ignore list` shows all active patterns
- [ ] AC-061: List command groups patterns by source
- [ ] AC-062: `acode ignore list --json` outputs valid JSON
- [ ] AC-063: CLI commands exit with code 0 on success
- [ ] AC-064: CLI commands provide helpful error messages

### Category 9: Performance

- [ ] AC-065: Single IsIgnored check completes in < 1ms average
- [ ] AC-066: Batch check of 1,000 paths completes in < 50ms
- [ ] AC-067: Pattern loading/compilation completes in < 50ms
- [ ] AC-068: Results are cached for repeated same-path checks
- [ ] AC-069: Cache invalidates when gitignore files change
- [ ] AC-070: Memory usage for 10,000 cached paths < 10MB

### Category 10: Error Handling

- [ ] AC-071: Invalid pattern syntax logged with file and line number
- [ ] AC-072: Missing gitignore file handled gracefully (empty rules)
- [ ] AC-073: Permission denied on gitignore returns graceful degradation
- [ ] AC-074: Corrupted gitignore (binary content) detected and skipped
- [ ] AC-075: Circular symlinks in .gitignore paths handled
- [ ] AC-076: ACODE-IGN-001 returned for invalid pattern syntax
- [ ] AC-077: ACODE-IGN-002 returned for parse errors
- [ ] AC-078: ACODE-IGN-003 returned for file access errors

### Category 11: Platform Compatibility

- [ ] AC-079: Case-insensitive matching on Windows by default
- [ ] AC-080: Case-sensitive matching on Linux by default
- [ ] AC-081: Path separators normalized (\ to / internally)
- [ ] AC-082: UNC paths on Windows handled correctly
- [ ] AC-083: Paths with spaces handled correctly
- [ ] AC-084: Unicode file names handled correctly

### Category 12: Integration

- [ ] AC-085: Index builder uses ignore service during enumeration
- [ ] AC-086: Search results exclude ignored files
- [ ] AC-087: Context packer respects ignore rules
- [ ] AC-088: File enumerator integrates with ignore service
- [ ] AC-089: Ignore service registered in DI container correctly
- [ ] AC-090: Ignore service is thread-safe for concurrent access

---

## Best Practices

### Gitignore Parsing

1. **Follow git semantics exactly** - Match git's behavior for edge cases
2. **Support nested gitignores** - Respect .gitignore at each directory level
3. **Handle negation patterns** - Support ! prefix to un-ignore files
4. **Cache parsed rules** - Parse .gitignore once per directory

### Rule Evaluation

5. **Evaluate in order** - Later rules override earlier; last match wins
6. **Use anchoring correctly** - Leading / anchors to root; trailing / matches directories only
7. **Support glob patterns** - *, **, ?, and character classes
8. **Handle directory vs file** - Distinguish between dir and file for trailing slash rules

### Performance

9. **Minimize filesystem calls** - Batch path existence checks
10. **Pre-compile patterns** - Convert globs to regex once, reuse
11. **Short-circuit evaluation** - Skip expensive patterns if already matched
12. **Cache decisions** - Remember ignore status for paths already evaluated

---

## Testing Requirements

### Unit Tests

#### GitignoreParserTests.cs

```csharp
using Acode.Infrastructure.Ignore;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Ignore;

public class GitignoreParserTests
{
    private readonly GitignoreParser _parser = new();

    [Fact]
    public void Should_Parse_Empty_File()
    {
        // Arrange
        var content = "";
        
        // Act
        var rules = _parser.Parse(content);
        
        // Assert
        rules.Should().BeEmpty();
    }

    [Fact]
    public void Should_Parse_Comment_Lines()
    {
        // Arrange
        var content = """
            # This is a comment
            *.log
            # Another comment
            build/
            """;
        
        // Act
        var rules = _parser.Parse(content);
        
        // Assert
        rules.Should().HaveCount(2);
        rules[0].Pattern.Should().Be("*.log");
        rules[1].Pattern.Should().Be("build/");
    }

    [Fact]
    public void Should_Parse_Blank_Lines()
    {
        // Arrange
        var content = """
            *.log
            
            build/
            
            """;
        
        // Act
        var rules = _parser.Parse(content);
        
        // Assert
        rules.Should().HaveCount(2);
    }

    [Fact]
    public void Should_Parse_Simple_Pattern()
    {
        // Arrange
        var content = "*.log";
        
        // Act
        var rules = _parser.Parse(content);
        
        // Assert
        rules.Should().ContainSingle()
            .Which.Pattern.Should().Be("*.log");
    }

    [Fact]
    public void Should_Parse_Directory_Pattern()
    {
        // Arrange
        var content = "node_modules/";
        
        // Act
        var rules = _parser.Parse(content);
        
        // Assert
        var rule = rules.Should().ContainSingle().Subject;
        rule.Pattern.Should().Be("node_modules/");
        rule.IsDirectoryOnly.Should().BeTrue();
    }

    [Fact]
    public void Should_Parse_Negation_Pattern()
    {
        // Arrange
        var content = "!important.log";
        
        // Act
        var rules = _parser.Parse(content);
        
        // Assert
        var rule = rules.Should().ContainSingle().Subject;
        rule.IsNegation.Should().BeTrue();
        rule.Pattern.Should().Be("important.log");
    }

    [Fact]
    public void Should_Parse_Escaped_Characters()
    {
        // Arrange
        var content = @"\#not-a-comment";
        
        // Act
        var rules = _parser.Parse(content);
        
        // Assert
        rules.Should().ContainSingle()
            .Which.Pattern.Should().Be("#not-a-comment");
    }

    [Fact]
    public void Should_Handle_Trailing_Spaces()
    {
        // Arrange - trailing space with backslash is preserved
        var content = @"file\ with\ space.txt";
        
        // Act
        var rules = _parser.Parse(content);
        
        // Assert
        rules.Should().ContainSingle()
            .Which.Pattern.Should().Contain("with");
    }

    [Fact]
    public void Should_Handle_BOM()
    {
        // Arrange - UTF-8 BOM prefix
        var content = "\uFEFF*.log";
        
        // Act
        var rules = _parser.Parse(content);
        
        // Assert
        rules.Should().ContainSingle()
            .Which.Pattern.Should().Be("*.log");
    }

    [Fact]
    public void Should_Handle_Different_Line_Endings()
    {
        // Arrange - Mixed CRLF and LF
        var content = "*.log\r\nbuild/\ntemp/";
        
        // Act
        var rules = _parser.Parse(content);
        
        // Assert
        rules.Should().HaveCount(3);
    }

    [Fact]
    public void Should_Return_Line_Numbers()
    {
        // Arrange
        var content = """
            # Comment
            *.log
            build/
            """;
        
        // Act
        var rules = _parser.Parse(content);
        
        // Assert
        rules[0].LineNumber.Should().Be(2);
        rules[1].LineNumber.Should().Be(3);
    }

    [Fact]
    public void Should_Parse_Rooted_Pattern()
    {
        // Arrange
        var content = "/config.local.json";
        
        // Act
        var rules = _parser.Parse(content);
        
        // Assert
        var rule = rules.Should().ContainSingle().Subject;
        rule.IsRooted.Should().BeTrue();
        rule.Pattern.Should().Be("config.local.json");
    }
}
```

#### PatternMatcherTests.cs

```csharp
using Acode.Infrastructure.Ignore;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Ignore;

public class PatternMatcherTests
{
    private readonly PatternMatcher _matcher = new();

    [Theory]
    [InlineData("README.md", "README.md", true)]
    [InlineData("readme.md", "README.md", false)] // case sensitive
    [InlineData("docs/README.md", "README.md", true)]
    public void Should_Match_Exact_Filename(string path, string pattern, bool expected)
    {
        _matcher.IsMatch(path, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("error.log", "*.log", true)]
    [InlineData("logs/error.log", "*.log", true)]
    [InlineData("error.txt", "*.log", false)]
    [InlineData("myfile", "*file", true)]
    public void Should_Match_Single_Wildcard(string path, string pattern, bool expected)
    {
        _matcher.IsMatch(path, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b/c/file.txt", "**/file.txt", true)]
    [InlineData("file.txt", "**/file.txt", true)]
    [InlineData("a/b/c/", "a/**/", true)]
    [InlineData("node_modules/lodash/index.js", "**/node_modules/**", true)]
    public void Should_Match_Double_Wildcard(string path, string pattern, bool expected)
    {
        _matcher.IsMatch(path, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("file1.txt", "file?.txt", true)]
    [InlineData("file12.txt", "file?.txt", false)]
    [InlineData("file.txt", "file?.txt", false)]
    public void Should_Match_Question_Mark(string path, string pattern, bool expected)
    {
        _matcher.IsMatch(path, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("file1.txt", "file[123].txt", true)]
    [InlineData("file4.txt", "file[123].txt", false)]
    [InlineData("filea.txt", "file[a-z].txt", true)]
    [InlineData("fileA.txt", "file[a-z].txt", false)]
    public void Should_Match_Character_Class(string path, string pattern, bool expected)
    {
        _matcher.IsMatch(path, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("filea.txt", "file[!0-9].txt", true)]
    [InlineData("file1.txt", "file[!0-9].txt", false)]
    public void Should_Match_Negated_Class(string path, string pattern, bool expected)
    {
        _matcher.IsMatch(path, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("build/output.dll", "build/", true)]
    [InlineData("build", "build/", false)]  // File named build, not directory
    [InlineData("mybuild/", "build/", false)]
    public void Should_Match_Directory_Only(string path, string pattern, bool expected)
    {
        var isDir = path.EndsWith("/");
        _matcher.IsMatch(path.TrimEnd('/'), pattern, isDir).Should().Be(expected);
    }

    [Theory]
    [InlineData("config.json", "/config.json", true)]
    [InlineData("subdir/config.json", "/config.json", false)]
    public void Should_Match_Rooted_Pattern(string path, string pattern, bool expected)
    {
        _matcher.IsMatch(path, pattern).Should().Be(expected);
    }

    [Fact]
    public void Should_Handle_Deeply_Nested_Paths()
    {
        // Arrange
        var path = "a/b/c/d/e/f/g/h/i/j/file.txt";
        var pattern = "**/file.txt";
        
        // Act & Assert
        _matcher.IsMatch(path, pattern).Should().BeTrue();
    }

    [Theory]
    [InlineData("FILE.TXT", "file.txt", true, false)]  // Windows
    [InlineData("FILE.TXT", "file.txt", false, true)]  // Linux
    public void Should_Handle_Case_Sensitivity(
        string path, string pattern, bool expected, bool caseSensitive)
    {
        _matcher.IsMatch(path, pattern, caseSensitive: caseSensitive)
            .Should().Be(expected);
    }
}
```

#### NegationTests.cs

```csharp
using Acode.Infrastructure.Ignore;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Ignore;

public class NegationTests
{
    [Fact]
    public void Should_Negate_Previous_Pattern()
    {
        // Arrange
        var service = new IgnoreService();
        service.AddPattern("*.log", IgnoreSource.Gitignore);
        service.AddPattern("!important.log", IgnoreSource.Gitignore);
        
        // Act & Assert
        service.IsIgnored("error.log").Should().BeTrue();
        service.IsIgnored("important.log").Should().BeFalse();
    }

    [Fact]
    public void Should_Handle_Multiple_Negations()
    {
        // Arrange
        var service = new IgnoreService();
        service.AddPattern("*.log", IgnoreSource.Gitignore);
        service.AddPattern("!important.log", IgnoreSource.Gitignore);
        service.AddPattern("important.log", IgnoreSource.Gitignore);
        
        // Act - last match wins
        // Assert
        service.IsIgnored("important.log").Should().BeTrue();
    }

    [Fact]
    public void Should_Apply_Order_Correctly()
    {
        // Arrange
        var service = new IgnoreService();
        // Ignore all, then un-ignore specific, then re-ignore subset
        service.AddPattern("logs/**", IgnoreSource.Gitignore);
        service.AddPattern("!logs/keep/**", IgnoreSource.Gitignore);
        service.AddPattern("logs/keep/temp/", IgnoreSource.Gitignore);
        
        // Act & Assert
        service.IsIgnored("logs/error.log").Should().BeTrue();
        service.IsIgnored("logs/keep/important.log").Should().BeFalse();
        service.IsIgnored("logs/keep/temp/").Should().BeTrue();
    }

    [Fact]
    public void Should_Handle_Directory_Negation()
    {
        // Arrange
        var service = new IgnoreService();
        service.AddPattern("build/", IgnoreSource.Gitignore);
        service.AddPattern("!build/keep/", IgnoreSource.Gitignore);
        
        // Act & Assert
        service.IsIgnored("build/", isDirectory: true).Should().BeTrue();
        service.IsIgnored("build/keep/", isDirectory: true).Should().BeFalse();
    }

    [Fact]
    public void Should_Handle_Nested_Negation()
    {
        // Arrange - from different gitignore levels
        var service = new IgnoreService();
        service.AddPattern("*.generated.cs", IgnoreSource.Gitignore);
        service.AddPattern("!Models.generated.cs", IgnoreSource.Config);
        
        // Act & Assert - config takes precedence
        service.IsIgnored("Other.generated.cs").Should().BeTrue();
        service.IsIgnored("Models.generated.cs").Should().BeFalse();
    }
}
```

#### BinaryDetectorTests.cs

```csharp
using Acode.Infrastructure.Ignore;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Ignore;

public class BinaryDetectorTests
{
    private readonly BinaryDetector _detector = new();

    [Theory]
    [InlineData(".png", true)]
    [InlineData(".jpg", true)]
    [InlineData(".jpeg", true)]
    [InlineData(".gif", true)]
    [InlineData(".ico", true)]
    [InlineData(".webp", true)]
    [InlineData(".bmp", true)]
    public void Should_Detect_Image_Extensions(string ext, bool expected)
    {
        _detector.IsBinaryExtension(ext).Should().Be(expected);
    }

    [Theory]
    [InlineData(".zip", true)]
    [InlineData(".tar", true)]
    [InlineData(".gz", true)]
    [InlineData(".7z", true)]
    [InlineData(".rar", true)]
    [InlineData(".bz2", true)]
    public void Should_Detect_Archive_Extensions(string ext, bool expected)
    {
        _detector.IsBinaryExtension(ext).Should().Be(expected);
    }

    [Theory]
    [InlineData(".exe", true)]
    [InlineData(".dll", true)]
    [InlineData(".so", true)]
    [InlineData(".dylib", true)]
    [InlineData(".bin", true)]
    public void Should_Detect_Executable_Extensions(string ext, bool expected)
    {
        _detector.IsBinaryExtension(ext).Should().Be(expected);
    }

    [Theory]
    [InlineData(".mp3", true)]
    [InlineData(".mp4", true)]
    [InlineData(".wav", true)]
    [InlineData(".avi", true)]
    [InlineData(".mov", true)]
    [InlineData(".flac", true)]
    public void Should_Detect_Media_Extensions(string ext, bool expected)
    {
        _detector.IsBinaryExtension(ext).Should().Be(expected);
    }

    [Theory]
    [InlineData(".pdf", true)]
    [InlineData(".doc", true)]
    [InlineData(".docx", true)]
    [InlineData(".xls", true)]
    [InlineData(".xlsx", true)]
    [InlineData(".ppt", true)]
    public void Should_Detect_Office_Extensions(string ext, bool expected)
    {
        _detector.IsBinaryExtension(ext).Should().Be(expected);
    }

    [Fact]
    public void Should_Detect_By_Magic_Number_PNG()
    {
        // Arrange - PNG magic number
        var header = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        
        // Act
        var result = _detector.IsBinaryContent(header);
        
        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Should_Detect_By_Magic_Number_ELF()
    {
        // Arrange - ELF magic number
        var header = new byte[] { 0x7F, 0x45, 0x4C, 0x46 };
        
        // Act
        var result = _detector.IsBinaryContent(header);
        
        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Should_Detect_By_Magic_Number_PE()
    {
        // Arrange - DOS/PE magic number
        var header = new byte[] { 0x4D, 0x5A };
        
        // Act
        var result = _detector.IsBinaryContent(header);
        
        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Should_Handle_Text_File()
    {
        // Arrange
        var content = "This is plain text content"u8.ToArray();
        
        // Act
        var result = _detector.IsBinaryContent(content);
        
        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(".cs", false)]
    [InlineData(".ts", false)]
    [InlineData(".py", false)]
    [InlineData(".md", false)]
    [InlineData(".json", false)]
    [InlineData(".yaml", false)]
    public void Should_Not_Detect_Text_Extensions(string ext, bool expected)
    {
        _detector.IsBinaryExtension(ext).Should().Be(expected);
    }

    [Fact]
    public void Should_Handle_Unknown_Extension()
    {
        // Arrange
        var ext = ".unknownext";
        
        // Act
        var result = _detector.IsBinaryExtension(ext);
        
        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Should_Handle_No_Extension()
    {
        // Arrange
        var ext = "";
        
        // Act
        var result = _detector.IsBinaryExtension(ext);
        
        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Should_Detect_Null_Bytes_As_Binary()
    {
        // Arrange - text with embedded null
        var content = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00, 0x57 };
        
        // Act
        var result = _detector.IsBinaryContent(content);
        
        // Assert
        result.Should().BeTrue();
    }
}
```

#### IgnoreServiceTests.cs

```csharp
using Acode.Domain.Ignore;
using Acode.Infrastructure.Ignore;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Acode.Infrastructure.Tests.Ignore;

public class IgnoreServiceTests
{
    [Fact]
    public void Should_Load_Gitignore()
    {
        // Arrange
        var fs = Substitute.For<IFileSystem>();
        fs.FileExists(".gitignore").Returns(true);
        fs.ReadAllText(".gitignore").Returns("*.log\nbuild/");
        
        var service = new IgnoreService(fs);
        service.Initialize(".");
        
        // Act & Assert
        service.IsIgnored("error.log").Should().BeTrue();
        service.IsIgnored("build/").Should().BeTrue();
    }

    [Fact]
    public void Should_Load_Nested_Gitignores()
    {
        // Arrange
        var fs = Substitute.For<IFileSystem>();
        fs.FileExists(".gitignore").Returns(true);
        fs.ReadAllText(".gitignore").Returns("*.log");
        fs.FileExists("src/.gitignore").Returns(true);
        fs.ReadAllText("src/.gitignore").Returns("*.generated.cs");
        
        var service = new IgnoreService(fs);
        service.Initialize(".");
        
        // Act & Assert
        service.IsIgnored("error.log").Should().BeTrue();
        service.IsIgnored("src/Model.generated.cs").Should().BeTrue();
        service.IsIgnored("Model.generated.cs").Should().BeFalse();
    }

    [Fact]
    public void Should_Load_Config_Ignores()
    {
        // Arrange
        var fs = Substitute.For<IFileSystem>();
        fs.FileExists(".gitignore").Returns(false);
        fs.FileExists(".agent/config.yml").Returns(true);
        fs.ReadAllText(".agent/config.yml").Returns("""
            index:
              ignore:
                patterns:
                  - "*.generated.cs"
            """);
        
        var service = new IgnoreService(fs);
        service.Initialize(".");
        
        // Act & Assert
        service.IsIgnored("Model.generated.cs").Should().BeTrue();
    }

    [Fact]
    public void Should_Combine_All_Sources()
    {
        // Arrange
        var fs = Substitute.For<IFileSystem>();
        fs.FileExists(".gitignore").Returns(true);
        fs.ReadAllText(".gitignore").Returns("*.log");
        fs.FileExists(".agent/config.yml").Returns(true);
        fs.ReadAllText(".agent/config.yml").Returns("""
            index:
              ignore:
                patterns:
                  - "*.tmp"
            """);
        
        var service = new IgnoreService(fs);
        service.Initialize(".");
        
        // Act & Assert
        service.IsIgnored("error.log").Should().BeTrue();
        service.IsIgnored("temp.tmp").Should().BeTrue();
    }

    [Fact]
    public void Should_Apply_Precedence_Order()
    {
        // Arrange
        var fs = Substitute.For<IFileSystem>();
        fs.FileExists(".gitignore").Returns(true);
        fs.ReadAllText(".gitignore").Returns("*.config");
        fs.FileExists(".agent/config.yml").Returns(true);
        fs.ReadAllText(".agent/config.yml").Returns("""
            index:
              ignore:
                patterns:
                  - "!important.config"
            """);
        
        var service = new IgnoreService(fs);
        service.Initialize(".");
        
        // Act - config patterns applied after gitignore
        // Assert
        service.IsIgnored("app.config").Should().BeTrue();
        service.IsIgnored("important.config").Should().BeFalse();
    }

    [Fact]
    public void Should_Cache_Results()
    {
        // Arrange
        var fs = Substitute.For<IFileSystem>();
        fs.FileExists(".gitignore").Returns(true);
        fs.ReadAllText(".gitignore").Returns("*.log");
        
        var service = new IgnoreService(fs);
        service.Initialize(".");
        
        // Act - call twice
        _ = service.IsIgnored("error.log");
        _ = service.IsIgnored("error.log");
        
        // Assert - pattern matching only called once
        service.CacheHits.Should().Be(1);
    }

    [Fact]
    public void Should_Refresh_On_Demand()
    {
        // Arrange
        var fs = Substitute.For<IFileSystem>();
        fs.FileExists(".gitignore").Returns(true);
        fs.ReadAllText(".gitignore").Returns("*.log");
        
        var service = new IgnoreService(fs);
        service.Initialize(".");
        
        // Change the gitignore content
        fs.ReadAllText(".gitignore").Returns("*.tmp");
        
        // Act
        service.Refresh();
        
        // Assert
        service.IsIgnored("error.log").Should().BeFalse();
        service.IsIgnored("temp.tmp").Should().BeTrue();
    }

    [Fact]
    public void Should_Handle_Missing_Gitignore()
    {
        // Arrange
        var fs = Substitute.For<IFileSystem>();
        fs.FileExists(Arg.Any<string>()).Returns(false);
        
        var service = new IgnoreService(fs);
        
        // Act
        var act = () => service.Initialize(".");
        
        // Assert - should not throw
        act.Should().NotThrow();
        service.IsIgnored("anything.txt").Should().BeFalse();
    }

    [Fact]
    public void Should_Handle_Invalid_Pattern()
    {
        // Arrange
        var fs = Substitute.For<IFileSystem>();
        fs.FileExists(".gitignore").Returns(true);
        fs.ReadAllText(".gitignore").Returns("[invalid\n*.log");
        
        var service = new IgnoreService(fs);
        
        // Act
        service.Initialize(".");
        
        // Assert - invalid pattern skipped, valid pattern works
        service.IsIgnored("error.log").Should().BeTrue();
    }
}
```

### Integration Tests

#### IgnoreIntegrationTests.cs

```csharp
using System.IO;
using Acode.Infrastructure.Ignore;
using FluentAssertions;
using Xunit;

namespace Acode.Integration.Tests.Ignore;

public class IgnoreIntegrationTests : IDisposable
{
    private readonly string _tempDir;

    public IgnoreIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void Should_Work_With_Real_Gitignore()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDir, ".gitignore"), """
            *.log
            build/
            node_modules/
            """);
        
        var service = new IgnoreService();
        service.Initialize(_tempDir);
        
        // Act & Assert
        service.IsIgnored("error.log").Should().BeTrue();
        service.IsIgnored("build/", isDirectory: true).Should().BeTrue();
        service.IsIgnored("src/app.ts").Should().BeFalse();
    }

    [Fact]
    public void Should_Work_With_Nested_Directories()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_tempDir, "src"));
        File.WriteAllText(Path.Combine(_tempDir, ".gitignore"), "*.log");
        File.WriteAllText(Path.Combine(_tempDir, "src", ".gitignore"), "*.generated.cs");
        
        var service = new IgnoreService();
        service.Initialize(_tempDir);
        
        // Act & Assert
        service.IsIgnored("error.log").Should().BeTrue();
        service.IsIgnored("src/error.log").Should().BeTrue();
        service.IsIgnored("src/Model.generated.cs").Should().BeTrue();
        service.IsIgnored("Model.generated.cs").Should().BeFalse();
    }

    [Fact]
    public void Should_Handle_Large_Gitignore()
    {
        // Arrange - 1000 patterns
        var patterns = Enumerable.Range(1, 1000)
            .Select(i => $"pattern{i}*.txt")
            .ToArray();
        File.WriteAllLines(Path.Combine(_tempDir, ".gitignore"), patterns);
        
        var service = new IgnoreService();
        
        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        service.Initialize(_tempDir);
        sw.Stop();
        
        // Assert - should load in < 100ms
        sw.ElapsedMilliseconds.Should().BeLessThan(100);
        service.IsIgnored("pattern500test.txt").Should().BeTrue();
    }

    [Fact]
    public void Should_Handle_Many_Ignore_Files()
    {
        // Arrange - 20 nested directories with gitignore files
        var current = _tempDir;
        for (int i = 0; i < 20; i++)
        {
            current = Path.Combine(current, $"dir{i}");
            Directory.CreateDirectory(current);
            File.WriteAllText(Path.Combine(current, ".gitignore"), $"local{i}.txt");
        }
        
        var service = new IgnoreService();
        service.Initialize(_tempDir);
        
        // Act & Assert
        var deepPath = string.Join("/", Enumerable.Range(0, 20).Select(i => $"dir{i}"));
        service.IsIgnored($"{deepPath}/local19.txt").Should().BeTrue();
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }
}
```

### E2E Tests

#### IgnoreE2ETests.cs

```csharp
using System.Diagnostics;
using System.IO;
using FluentAssertions;
using Xunit;

namespace Acode.E2E.Tests.Ignore;

public class IgnoreE2ETests : IDisposable
{
    private readonly string _tempDir;

    public IgnoreE2ETests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        
        // Create test files and gitignore
        File.WriteAllText(Path.Combine(_tempDir, ".gitignore"), """
            *.log
            node_modules/
            """);
        File.WriteAllText(Path.Combine(_tempDir, "app.ts"), "console.log('hello');");
        File.WriteAllText(Path.Combine(_tempDir, "error.log"), "error content");
    }

    [Fact]
    public void Should_Show_Check_Command_Ignored()
    {
        // Arrange
        var psi = new ProcessStartInfo
        {
            FileName = "acode",
            Arguments = "ignore check error.log",
            WorkingDirectory = _tempDir,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        
        // Act
        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        
        // Assert
        output.Should().Contain("IGNORED");
        output.Should().Contain("*.log");
        process.ExitCode.Should().Be(0);
    }

    [Fact]
    public void Should_Show_Check_Command_Not_Ignored()
    {
        // Arrange
        var psi = new ProcessStartInfo
        {
            FileName = "acode",
            Arguments = "ignore check app.ts",
            WorkingDirectory = _tempDir,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        
        // Act
        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        
        // Assert
        output.Should().Contain("NOT ignored");
        process.ExitCode.Should().Be(0);
    }

    [Fact]
    public void Should_Show_List_Command()
    {
        // Arrange
        var psi = new ProcessStartInfo
        {
            FileName = "acode",
            Arguments = "ignore list",
            WorkingDirectory = _tempDir,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        
        // Act
        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        
        // Assert
        output.Should().Contain("*.log");
        output.Should().Contain("node_modules/");
        process.ExitCode.Should().Be(0);
    }

    [Fact]
    public void Should_Show_List_Command_Json()
    {
        // Arrange
        var psi = new ProcessStartInfo
        {
            FileName = "acode",
            Arguments = "ignore list --json",
            WorkingDirectory = _tempDir,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        
        // Act
        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        
        // Assert
        var json = System.Text.Json.JsonDocument.Parse(output);
        json.RootElement.GetProperty("patterns").GetArrayLength().Should().BeGreaterThan(0);
        process.ExitCode.Should().Be(0);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }
}
```

### Performance Benchmarks

```csharp
using BenchmarkDotNet.Attributes;
using Acode.Infrastructure.Ignore;

namespace Acode.Benchmarks.Ignore;

[MemoryDiagnoser]
public class IgnoreBenchmarks
{
    private IgnoreService _service = null!;
    private string[] _paths = null!;

    [GlobalSetup]
    public void Setup()
    {
        _service = new IgnoreService();
        // Add realistic patterns
        for (int i = 0; i < 100; i++)
        {
            _service.AddPattern($"pattern{i}/**", IgnoreSource.Gitignore);
        }
        _service.AddPattern("*.log", IgnoreSource.Gitignore);
        _service.AddPattern("node_modules/", IgnoreSource.Gitignore);
        _service.AddPattern("build/", IgnoreSource.Gitignore);
        
        _paths = Enumerable.Range(0, 1000)
            .Select(i => $"src/components/feature{i}/Component.tsx")
            .ToArray();
    }

    [Benchmark]
    public bool SingleCheck()
    {
        return _service.IsIgnored("src/app.log");
    }

    [Benchmark]
    public int BatchCheck1000()
    {
        var count = 0;
        foreach (var path in _paths)
        {
            if (_service.IsIgnored(path)) count++;
        }
        return count;
    }

    [Benchmark]
    public void PatternCompilation()
    {
        var compiler = new SafePatternCompiler();
        compiler.CompileGlobToRegex("**/node_modules/**/*.js");
    }
}
```

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| SingleCheck | 0.5ms | 1ms |
| BatchCheck1000 | 25ms | 50ms |
| PatternCompilation | 1ms | 5ms |

---

## User Verification Steps

### Scenario 1: Basic Gitignore Patterns

**Objective:** Verify that standard gitignore patterns work correctly.

```bash
# Setup: Create test repository with gitignore
mkdir -p /tmp/ignore-test && cd /tmp/ignore-test
git init

# Create .gitignore with common patterns
cat > .gitignore << 'EOF'
*.log
build/
node_modules/
*.tmp
EOF

# Create test files
touch app.ts error.log debug.log temp.tmp
mkdir -p build node_modules/lodash src

# Test 1: Check ignored files
acode ignore check error.log
# Expected: IGNORED - matched by *.log

acode ignore check debug.log
# Expected: IGNORED - matched by *.log

acode ignore check build/
# Expected: IGNORED - matched by build/

acode ignore check node_modules/lodash/
# Expected: IGNORED - matched by node_modules/

# Test 2: Check non-ignored files
acode ignore check app.ts
# Expected: NOT ignored

acode ignore check src/
# Expected: NOT ignored

# Cleanup
cd / && rm -rf /tmp/ignore-test
```

### Scenario 2: Negation Patterns

**Objective:** Verify that negation patterns re-include files correctly.

```bash
# Setup
mkdir -p /tmp/negation-test && cd /tmp/negation-test
git init

# Create .gitignore with negation
cat > .gitignore << 'EOF'
# Ignore all log files
*.log
# But keep important.log
!important.log
# Ignore all in build except keep folder
build/
!build/keep/
EOF

# Create test files
touch error.log important.log
mkdir -p build/output build/keep

# Test 1: Normal ignored file
acode ignore check error.log
# Expected: IGNORED - matched by *.log

# Test 2: Negated file
acode ignore check important.log
# Expected: NOT ignored - negated by !important.log

# Test 3: Build directory
acode ignore check build/output/
# Expected: IGNORED - matched by build/

# Test 4: Negated directory
acode ignore check build/keep/
# Expected: NOT ignored - negated by !build/keep/

# Cleanup
cd / && rm -rf /tmp/negation-test
```

### Scenario 3: Config-Based Ignore Patterns

**Objective:** Verify that patterns from .agent/config.yml work correctly.

```bash
# Setup
mkdir -p /tmp/config-ignore-test/.agent && cd /tmp/config-ignore-test

# Create config with ignore patterns
cat > .agent/config.yml << 'EOF'
index:
  ignore:
    patterns:
      - "*.generated.cs"
      - ".idea/**"
      - ".vs/**"
      - "*.designer.cs"
EOF

# Create test files
touch Model.generated.cs Model.cs Form.designer.cs Form.cs
mkdir -p .idea .vs src

# Test 1: Config-ignored file
acode ignore check Model.generated.cs
# Expected: IGNORED - matched by *.generated.cs (from config)

# Test 2: Non-ignored file
acode ignore check Model.cs
# Expected: NOT ignored

# Test 3: Config-ignored directory
acode ignore check .idea/
# Expected: IGNORED - matched by .idea/** (from config)

# Test 4: List all patterns
acode ignore list
# Expected: Shows patterns from config with source indication

# Cleanup
cd / && rm -rf /tmp/config-ignore-test
```

### Scenario 4: Binary File Auto-Detection

**Objective:** Verify that binary files are automatically detected and ignored.

```bash
# Setup
mkdir -p /tmp/binary-test && cd /tmp/binary-test

# Create various file types
echo "console.log('hello');" > app.js
echo "# README" > README.md

# Create binary files (hex content)
printf '\x89PNG\r\n\x1a\n' > image.png
printf 'MZ' > program.exe
printf '\x7fELF' > binary.so
printf 'PK\x03\x04' > archive.zip

# Test 1: Check image file
acode ignore check image.png
# Expected: IGNORED - auto-detected as binary (PNG)

# Test 2: Check executable
acode ignore check program.exe
# Expected: IGNORED - auto-detected as binary (PE)

# Test 3: Check ELF binary
acode ignore check binary.so
# Expected: IGNORED - auto-detected as binary (ELF)

# Test 4: Check archive
acode ignore check archive.zip
# Expected: IGNORED - auto-detected as binary (ZIP)

# Test 5: Check text files
acode ignore check app.js
# Expected: NOT ignored

acode ignore check README.md
# Expected: NOT ignored

# Cleanup
cd / && rm -rf /tmp/binary-test
```

### Scenario 5: Nested Gitignore Files

**Objective:** Verify that nested .gitignore files apply only to their subtrees.

```bash
# Setup
mkdir -p /tmp/nested-test/src/components && cd /tmp/nested-test
git init

# Create root gitignore
cat > .gitignore << 'EOF'
*.log
build/
EOF

# Create nested gitignore
cat > src/.gitignore << 'EOF'
*.test.ts
__tests__/
EOF

# Create another nested gitignore
cat > src/components/.gitignore << 'EOF'
*.stories.tsx
EOF

# Create test files
touch error.log
mkdir -p build
touch src/app.ts src/app.test.ts
mkdir -p src/__tests__
touch src/components/Button.tsx src/components/Button.stories.tsx

# Test 1: Root pattern applies everywhere
acode ignore check error.log
# Expected: IGNORED

acode ignore check src/error.log
# Expected: IGNORED (root pattern)

# Test 2: Nested pattern applies to subtree only
acode ignore check src/app.test.ts
# Expected: IGNORED (src/.gitignore)

acode ignore check app.test.ts
# Expected: NOT ignored (pattern doesn't apply at root)

# Test 3: Deep nested pattern
acode ignore check src/components/Button.stories.tsx
# Expected: IGNORED (components/.gitignore)

acode ignore check src/Button.stories.tsx
# Expected: NOT ignored (pattern doesn't apply at src level)

# Cleanup
cd / && rm -rf /tmp/nested-test
```

### Scenario 6: Glob Pattern Matching

**Objective:** Verify that all glob pattern types work correctly.

```bash
# Setup
mkdir -p /tmp/glob-test/a/b/c && cd /tmp/glob-test
git init

# Create .gitignore with various patterns
cat > .gitignore << 'EOF'
# Single wildcard
*.log

# Double wildcard
**/temp/

# Question mark
file?.txt

# Character class
log[0-9].txt

# Negated class
file[!a-z].txt

# Rooted pattern
/config.local.json

# Directory-only pattern
cache/
EOF

# Create test files
touch error.log
touch file1.txt file2.txt filea.txt
touch log1.txt log9.txt loga.txt
touch config.local.json
mkdir -p temp cache a/temp a/b/temp

# Test various patterns
acode ignore check error.log        # IGNORED (*.log)
acode ignore check file1.txt        # IGNORED (file?.txt)
acode ignore check filea.txt        # NOT ignored (doesn't match file?.txt)
acode ignore check log5.txt         # IGNORED (log[0-9].txt)
acode ignore check loga.txt         # NOT ignored
acode ignore check temp/            # IGNORED (**/temp/)
acode ignore check a/b/temp/        # IGNORED (**/temp/)
acode ignore check config.local.json # IGNORED (/config.local.json)
acode ignore check a/config.local.json # NOT ignored (rooted pattern)

# Cleanup
cd / && rm -rf /tmp/glob-test
```

### Scenario 7: Performance Verification

**Objective:** Verify that ignore checking meets performance targets.

```bash
# Setup: Create repository with many files
mkdir -p /tmp/perf-test && cd /tmp/perf-test
git init

# Create gitignore with 100 patterns
for i in $(seq 1 100); do
  echo "pattern$i/**" >> .gitignore
done
echo "*.log" >> .gitignore
echo "node_modules/" >> .gitignore

# Create 1000 test files
mkdir -p src
for i in $(seq 1 1000); do
  touch "src/file$i.ts"
done

# Test 1: Single check performance
time acode ignore check src/file500.ts
# Expected: < 10ms total

# Test 2: Batch check performance (via index build)
time acode index build --dry-run
# Expected: File enumeration < 1s for 1000 files

# Test 3: Pattern loading performance
time acode ignore list > /dev/null
# Expected: < 50ms

# Cleanup
cd / && rm -rf /tmp/perf-test
```

### Scenario 8: Edge Cases and Error Handling

**Objective:** Verify graceful handling of edge cases.

```bash
# Setup
mkdir -p /tmp/edge-test && cd /tmp/edge-test

# Test 1: Missing gitignore
acode ignore list
# Expected: Empty list or message "No ignore patterns found"

# Test 2: Invalid pattern syntax
cat > .gitignore << 'EOF'
[invalid-bracket
*.log
EOF

acode ignore check error.log
# Expected: IGNORED (valid pattern still works)
# Expected: Warning about invalid pattern in output or logs

# Test 3: Very long pattern (should be rejected)
python3 -c "print('a' * 2000)" >> .gitignore
acode ignore list
# Expected: Warning about pattern too long

# Test 4: Circular symlink (if supported)
ln -s . loop 2>/dev/null || echo "Symlinks not supported"
acode ignore check loop/test.txt
# Expected: Graceful handling, no infinite loop

# Test 5: File with path traversal in name
touch "file..test.txt"
acode ignore check "file..test.txt"
# Expected: Works correctly (not a traversal attack)

# Cleanup
cd / && rm -rf /tmp/edge-test
```

### Scenario 9: CLI Command Verification

**Objective:** Verify CLI commands work as documented.

```bash
# Setup
mkdir -p /tmp/cli-test && cd /tmp/cli-test
git init

cat > .gitignore << 'EOF'
*.log
build/
EOF

touch error.log app.ts

# Test 1: Check command with --verbose
acode ignore check error.log --verbose
# Expected: Shows pattern source file and line number

# Test 2: List command with --json
acode ignore list --json | jq '.'
# Expected: Valid JSON with patterns array

# Test 3: Help text
acode ignore --help
# Expected: Shows subcommands (check, list)

acode ignore check --help
# Expected: Shows options (--verbose, etc.)

# Test 4: Exit codes
acode ignore check error.log && echo "Exit 0" || echo "Exit non-zero"
# Expected: Exit 0 (command succeeded)

# Cleanup
cd / && rm -rf /tmp/cli-test
```

### Scenario 10: Integration with Index Build

**Objective:** Verify that ignore rules are respected during index building.

```bash
# Setup
mkdir -p /tmp/integration-test && cd /tmp/integration-test
git init

cat > .gitignore << 'EOF'
*.log
node_modules/
build/
EOF

# Create various files
touch app.ts server.ts error.log debug.log
mkdir -p node_modules/lodash build/output src
touch node_modules/lodash/index.js
touch build/output/app.js
touch src/main.ts

# Build index
acode index build

# Verify ignored files excluded
acode search "error"
# Expected: No results from error.log

acode search "lodash"
# Expected: No results from node_modules/

acode search "main"
# Expected: Results from src/main.ts

# Check index stats
acode index status
# Expected: Should show ~3 files indexed (app.ts, server.ts, src/main.ts)

# Cleanup
cd / && rm -rf /tmp/integration-test
```

---

## Implementation Prompt

### File Structure

```
src/Acode.Domain/
├── Ignore/
│   ├── IIgnoreService.cs
│   ├── IIgnoreRuleParser.cs
│   ├── IBinaryDetector.cs
│   ├── IgnoreRule.cs
│   ├── IgnoreSource.cs
│   ├── IgnoreCheckResult.cs
│   └── Exceptions/
│       ├── InvalidPatternException.cs
│       └── PatternParseException.cs
│
src/Acode.Infrastructure/
├── Ignore/
│   ├── IgnoreService.cs
│   ├── GitignoreParser.cs
│   ├── PatternMatcher.cs
│   ├── PatternCompiler.cs
│   ├── BinaryDetector.cs
│   ├── IgnoreCache.cs
│   └── PathValidator.cs
│
src/Acode.Cli/
└── Commands/
    └── IgnoreCommand.cs
```

---

### Domain Models

#### IgnoreRule.cs

```csharp
namespace Acode.Domain.Ignore;

/// <summary>
/// Represents a single ignore rule with its pattern and metadata.
/// </summary>
public sealed record IgnoreRule
{
    /// <summary>
    /// The pattern string (e.g., "*.log", "build/").
    /// </summary>
    public string Pattern { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether this is a negation pattern (starts with !).
    /// </summary>
    public bool IsNegation { get; init; }
    
    /// <summary>
    /// Whether this pattern only matches directories (ends with /).
    /// </summary>
    public bool IsDirectoryOnly { get; init; }
    
    /// <summary>
    /// Whether this pattern is rooted to the gitignore location (starts with /).
    /// </summary>
    public bool IsRooted { get; init; }
    
    /// <summary>
    /// The source of this rule (gitignore, config, etc.).
    /// </summary>
    public IgnoreSource Source { get; init; }
    
    /// <summary>
    /// Path to the source file (for diagnostics).
    /// </summary>
    public string? SourceFile { get; init; }
    
    /// <summary>
    /// Line number in the source file (1-based).
    /// </summary>
    public int LineNumber { get; init; }
}
```

#### IgnoreSource.cs

```csharp
namespace Acode.Domain.Ignore;

/// <summary>
/// Identifies the source of an ignore rule for precedence ordering.
/// </summary>
public enum IgnoreSource
{
    /// <summary>Patterns from .gitignore files.</summary>
    Gitignore = 0,
    
    /// <summary>Patterns from global ignore file.</summary>
    Global = 1,
    
    /// <summary>Patterns from .agent/config.yml.</summary>
    Config = 2,
    
    /// <summary>Patterns from command-line arguments.</summary>
    CommandLine = 3,
    
    /// <summary>Auto-detected binary files.</summary>
    AutoBinary = 4
}
```

#### IgnoreCheckResult.cs

```csharp
namespace Acode.Domain.Ignore;

/// <summary>
/// Result of checking if a path is ignored.
/// </summary>
public sealed record IgnoreCheckResult
{
    /// <summary>
    /// Whether the path is ignored.
    /// </summary>
    public bool IsIgnored { get; init; }
    
    /// <summary>
    /// The rule that matched (if ignored).
    /// </summary>
    public IgnoreRule? MatchedRule { get; init; }
    
    /// <summary>
    /// The path that was checked.
    /// </summary>
    public string Path { get; init; } = string.Empty;
}
```

---

### Domain Interfaces

#### IIgnoreService.cs

```csharp
using System.Collections.Generic;

namespace Acode.Domain.Ignore;

/// <summary>
/// Service for checking if paths should be ignored based on various rules.
/// </summary>
public interface IIgnoreService
{
    /// <summary>
    /// Initializes the service for a repository root.
    /// </summary>
    /// <param name="repositoryRoot">Path to the repository root.</param>
    void Initialize(string repositoryRoot);
    
    /// <summary>
    /// Checks if a path should be ignored.
    /// </summary>
    /// <param name="relativePath">Path relative to repository root.</param>
    /// <param name="isDirectory">Whether the path is a directory.</param>
    /// <returns>True if the path should be ignored.</returns>
    bool IsIgnored(string relativePath, bool isDirectory = false);
    
    /// <summary>
    /// Checks if a path should be ignored with detailed result.
    /// </summary>
    IgnoreCheckResult CheckIgnored(string relativePath, bool isDirectory = false);
    
    /// <summary>
    /// Checks multiple paths efficiently.
    /// </summary>
    IReadOnlyList<IgnoreCheckResult> BatchCheckIgnored(
        IReadOnlyList<string> paths,
        IReadOnlyList<bool> isDirectory);
    
    /// <summary>
    /// Gets all active ignore patterns.
    /// </summary>
    IReadOnlyList<IgnoreRule> GetPatterns();
    
    /// <summary>
    /// Gets patterns grouped by source.
    /// </summary>
    IReadOnlyDictionary<IgnoreSource, IReadOnlyList<IgnoreRule>> GetPatternsBySource();
    
    /// <summary>
    /// Adds a pattern at runtime.
    /// </summary>
    void AddPattern(string pattern, IgnoreSource source);
    
    /// <summary>
    /// Reloads all patterns from disk.
    /// </summary>
    void Refresh();
    
    /// <summary>
    /// Number of cache hits (for diagnostics).
    /// </summary>
    int CacheHits { get; }
}
```

#### IIgnoreRuleParser.cs

```csharp
using System.Collections.Generic;

namespace Acode.Domain.Ignore;

/// <summary>
/// Parses ignore patterns from various sources.
/// </summary>
public interface IIgnoreRuleParser
{
    /// <summary>
    /// Parses patterns from gitignore content.
    /// </summary>
    IReadOnlyList<IgnoreRule> Parse(string content, string? sourceFile = null);
    
    /// <summary>
    /// Parses patterns from a file.
    /// </summary>
    IReadOnlyList<IgnoreRule> ParseFile(string filePath);
}
```

#### IBinaryDetector.cs

```csharp
namespace Acode.Domain.Ignore;

/// <summary>
/// Detects binary files that should be auto-ignored.
/// </summary>
public interface IBinaryDetector
{
    /// <summary>
    /// Checks if a file extension indicates a binary file.
    /// </summary>
    bool IsBinaryExtension(string extension);
    
    /// <summary>
    /// Checks if file content indicates a binary file.
    /// </summary>
    bool IsBinaryContent(byte[] header);
    
    /// <summary>
    /// Checks if a file is binary using both extension and content.
    /// </summary>
    bool IsBinaryFile(string filePath);
}
```

---

### Infrastructure Implementations

#### IgnoreService.cs

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Acode.Domain.Ignore;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Ignore;

public sealed class IgnoreService : IIgnoreService
{
    private readonly IIgnoreRuleParser _parser;
    private readonly IBinaryDetector _binaryDetector;
    private readonly PatternMatcher _matcher;
    private readonly IgnoreCache _cache;
    private readonly ILogger<IgnoreService> _logger;
    
    private string _repositoryRoot = string.Empty;
    private List<IgnoreRule> _rules = new();
    private readonly object _lock = new();

    public int CacheHits => _cache.Hits;

    public IgnoreService(
        IIgnoreRuleParser parser,
        IBinaryDetector binaryDetector,
        ILogger<IgnoreService> logger)
    {
        _parser = parser;
        _binaryDetector = binaryDetector;
        _matcher = new PatternMatcher();
        _cache = new IgnoreCache();
        _logger = logger;
    }

    public void Initialize(string repositoryRoot)
    {
        _repositoryRoot = Path.GetFullPath(repositoryRoot);
        LoadAllPatterns();
    }

    public bool IsIgnored(string relativePath, bool isDirectory = false)
    {
        return CheckIgnored(relativePath, isDirectory).IsIgnored;
    }

    public IgnoreCheckResult CheckIgnored(string relativePath, bool isDirectory = false)
    {
        // Check cache first
        var cacheKey = $"{relativePath}:{isDirectory}";
        if (_cache.TryGet(cacheKey, out var cached))
        {
            return cached;
        }

        // Check binary auto-detection
        if (!isDirectory)
        {
            var ext = Path.GetExtension(relativePath);
            if (_binaryDetector.IsBinaryExtension(ext))
            {
                var binaryResult = new IgnoreCheckResult
                {
                    IsIgnored = true,
                    Path = relativePath,
                    MatchedRule = new IgnoreRule
                    {
                        Pattern = $"*{ext}",
                        Source = IgnoreSource.AutoBinary
                    }
                };
                _cache.Set(cacheKey, binaryResult);
                return binaryResult;
            }
        }

        // Check patterns (last match wins)
        IgnoreRule? matchedRule = null;
        bool isIgnored = false;

        foreach (var rule in _rules)
        {
            if (rule.IsDirectoryOnly && !isDirectory)
            {
                continue;
            }

            if (_matcher.IsMatch(relativePath, rule))
            {
                if (rule.IsNegation)
                {
                    isIgnored = false;
                    matchedRule = null;
                }
                else
                {
                    isIgnored = true;
                    matchedRule = rule;
                }
            }
        }

        var result = new IgnoreCheckResult
        {
            IsIgnored = isIgnored,
            Path = relativePath,
            MatchedRule = matchedRule
        };

        _cache.Set(cacheKey, result);
        return result;
    }

    public IReadOnlyList<IgnoreCheckResult> BatchCheckIgnored(
        IReadOnlyList<string> paths,
        IReadOnlyList<bool> isDirectory)
    {
        var results = new List<IgnoreCheckResult>(paths.Count);
        for (int i = 0; i < paths.Count; i++)
        {
            results.Add(CheckIgnored(paths[i], isDirectory[i]));
        }
        return results;
    }

    public IReadOnlyList<IgnoreRule> GetPatterns()
    {
        lock (_lock)
        {
            return _rules.ToList();
        }
    }

    public IReadOnlyDictionary<IgnoreSource, IReadOnlyList<IgnoreRule>> GetPatternsBySource()
    {
        lock (_lock)
        {
            return _rules
                .GroupBy(r => r.Source)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<IgnoreRule>)g.ToList());
        }
    }

    public void AddPattern(string pattern, IgnoreSource source)
    {
        var rules = _parser.Parse(pattern);
        lock (_lock)
        {
            foreach (var rule in rules)
            {
                _rules.Add(rule with { Source = source });
            }
        }
        _cache.Clear();
    }

    public void Refresh()
    {
        _cache.Clear();
        LoadAllPatterns();
    }

    private void LoadAllPatterns()
    {
        var rules = new List<IgnoreRule>();

        // Load .gitignore files (recursive)
        LoadGitignoreRecursive(_repositoryRoot, rules);

        // Load global ignore
        var globalPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "acode", "ignore");
        if (File.Exists(globalPath))
        {
            rules.AddRange(_parser.ParseFile(globalPath)
                .Select(r => r with { Source = IgnoreSource.Global }));
        }

        // Load config patterns
        var configPath = Path.Combine(_repositoryRoot, ".agent", "config.yml");
        if (File.Exists(configPath))
        {
            var configPatterns = LoadConfigPatterns(configPath);
            rules.AddRange(configPatterns);
        }

        lock (_lock)
        {
            _rules = rules;
        }

        _logger.LogDebug("Loaded {Count} ignore patterns", rules.Count);
    }

    private void LoadGitignoreRecursive(string directory, List<IgnoreRule> rules)
    {
        var gitignorePath = Path.Combine(directory, ".gitignore");
        if (File.Exists(gitignorePath))
        {
            var parsed = _parser.ParseFile(gitignorePath);
            var relativeTo = Path.GetRelativePath(_repositoryRoot, directory);
            
            foreach (var rule in parsed)
            {
                // Adjust pattern for nested gitignore
                var adjustedPattern = relativeTo == "."
                    ? rule.Pattern
                    : $"{relativeTo}/{rule.Pattern}";
                
                rules.Add(rule with 
                { 
                    Pattern = adjustedPattern,
                    Source = IgnoreSource.Gitignore 
                });
            }
        }

        // Recurse into subdirectories
        foreach (var subdir in Directory.EnumerateDirectories(directory))
        {
            var name = Path.GetFileName(subdir);
            if (name.StartsWith(".") || name == "node_modules")
            {
                continue; // Skip hidden and known heavy directories
            }
            LoadGitignoreRecursive(subdir, rules);
        }
    }

    private IEnumerable<IgnoreRule> LoadConfigPatterns(string configPath)
    {
        // Simplified YAML parsing for ignore patterns
        var content = File.ReadAllText(configPath);
        var lines = content.Split('\n');
        var inIgnoreSection = false;
        var inPatterns = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            if (trimmed.StartsWith("ignore:"))
            {
                inIgnoreSection = true;
                continue;
            }
            
            if (inIgnoreSection && trimmed.StartsWith("patterns:"))
            {
                inPatterns = true;
                continue;
            }
            
            if (inPatterns && trimmed.StartsWith("- "))
            {
                var pattern = trimmed.Substring(2).Trim().Trim('"', '\'');
                var parsed = _parser.Parse(pattern, configPath);
                foreach (var rule in parsed)
                {
                    yield return rule with { Source = IgnoreSource.Config };
                }
            }
            
            if (inPatterns && !trimmed.StartsWith("-") && !string.IsNullOrWhiteSpace(trimmed))
            {
                inPatterns = false;
                inIgnoreSection = false;
            }
        }
    }
}
```

#### GitignoreParser.cs

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Acode.Domain.Ignore;

namespace Acode.Infrastructure.Ignore;

public sealed class GitignoreParser : IIgnoreRuleParser
{
    public IReadOnlyList<IgnoreRule> Parse(string content, string? sourceFile = null)
    {
        var rules = new List<IgnoreRule>();
        var lines = content.Split('\n');
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            var lineNumber = i + 1;
            
            // Handle BOM on first line
            if (lineNumber == 1 && line.StartsWith("\uFEFF"))
            {
                line = line.Substring(1);
            }
            
            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }
            
            // Skip comments (unless escaped)
            if (line.StartsWith("#") && !line.StartsWith("\\#"))
            {
                continue;
            }
            
            // Handle escaped comment
            if (line.StartsWith("\\#"))
            {
                line = line.Substring(1);
            }
            
            // Parse the pattern
            var rule = ParsePattern(line, sourceFile, lineNumber);
            if (rule != null)
            {
                rules.Add(rule);
            }
        }
        
        return rules;
    }

    public IReadOnlyList<IgnoreRule> ParseFile(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath, Encoding.UTF8);
            return Parse(content, filePath);
        }
        catch (Exception)
        {
            // Try fallback encoding
            try
            {
                var content = File.ReadAllText(filePath, Encoding.Latin1);
                return Parse(content, filePath);
            }
            catch
            {
                return Array.Empty<IgnoreRule>();
            }
        }
    }

    private IgnoreRule? ParsePattern(string line, string? sourceFile, int lineNumber)
    {
        var pattern = line;
        var isNegation = false;
        var isRooted = false;
        var isDirectoryOnly = false;
        
        // Handle negation
        if (pattern.StartsWith("!"))
        {
            isNegation = true;
            pattern = pattern.Substring(1);
        }
        
        // Handle trailing slash (directory only)
        if (pattern.EndsWith("/"))
        {
            isDirectoryOnly = true;
            pattern = pattern.TrimEnd('/');
        }
        
        // Handle leading slash (rooted)
        if (pattern.StartsWith("/"))
        {
            isRooted = true;
            pattern = pattern.Substring(1);
        }
        
        // Handle escaped trailing spaces
        pattern = ProcessEscapedSpaces(pattern);
        
        // Trim unescaped trailing spaces
        pattern = pattern.TrimEnd(' ');
        
        if (string.IsNullOrEmpty(pattern))
        {
            return null;
        }
        
        return new IgnoreRule
        {
            Pattern = pattern,
            IsNegation = isNegation,
            IsRooted = isRooted,
            IsDirectoryOnly = isDirectoryOnly,
            Source = IgnoreSource.Gitignore,
            SourceFile = sourceFile,
            LineNumber = lineNumber
        };
    }

    private string ProcessEscapedSpaces(string pattern)
    {
        // Replace "\ " with actual space (git escape for trailing spaces)
        return pattern.Replace("\\ ", " ");
    }
}
```

---

### CLI Command

#### IgnoreCommand.cs

```csharp
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text.Json;
using Acode.Domain.Ignore;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace Acode.Cli.Commands;

public sealed class IgnoreCommand : Command
{
    public IgnoreCommand() : base("ignore", "Manage ignore patterns")
    {
        AddCommand(new CheckCommand());
        AddCommand(new ListCommand());
    }

    private sealed class CheckCommand : Command
    {
        public CheckCommand() : base("check", "Check if a path is ignored")
        {
            var pathArg = new Argument<string>("path", "Path to check");
            AddArgument(pathArg);

            var verboseOption = new Option<bool>("--verbose", "Show detailed match info");
            AddOption(verboseOption);

            this.SetHandler((InvocationContext ctx) =>
            {
                var path = ctx.ParseResult.GetValueForArgument(pathArg);
                var verbose = ctx.ParseResult.GetValueForOption(verboseOption);
                var service = ctx.BindingContext.GetRequiredService<IIgnoreService>();

                var result = service.CheckIgnored(path);

                if (result.IsIgnored)
                {
                    AnsiConsole.MarkupLine($"[red]{path}[/]: [bold]IGNORED[/]");
                    if (result.MatchedRule != null)
                    {
                        AnsiConsole.MarkupLine($"  Matched: [yellow]{result.MatchedRule.Pattern}[/]");
                        if (verbose && result.MatchedRule.SourceFile != null)
                        {
                            AnsiConsole.MarkupLine(
                                $"  Source: {result.MatchedRule.SourceFile}:{result.MatchedRule.LineNumber}");
                        }
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine($"[green]{path}[/]: [bold]NOT ignored[/]");
                }
            });
        }
    }

    private sealed class ListCommand : Command
    {
        public ListCommand() : base("list", "List all ignore patterns")
        {
            var jsonOption = new Option<bool>("--json", "Output as JSON");
            AddOption(jsonOption);

            this.SetHandler((InvocationContext ctx) =>
            {
                var json = ctx.ParseResult.GetValueForOption(jsonOption);
                var service = ctx.BindingContext.GetRequiredService<IIgnoreService>();

                var patternsBySource = service.GetPatternsBySource();

                if (json)
                {
                    var output = new
                    {
                        patterns = patternsBySource.SelectMany(kvp => 
                            kvp.Value.Select(r => new
                            {
                                pattern = r.Pattern,
                                source = kvp.Key.ToString(),
                                isNegation = r.IsNegation,
                                isDirectoryOnly = r.IsDirectoryOnly,
                                sourceFile = r.SourceFile,
                                lineNumber = r.LineNumber
                            }))
                    };
                    Console.WriteLine(JsonSerializer.Serialize(output, 
                        new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    AnsiConsole.MarkupLine("[bold]Ignore Rules[/]");
                    AnsiConsole.MarkupLine("────────────────────");

                    foreach (var (source, rules) in patternsBySource)
                    {
                        AnsiConsole.MarkupLine($"\n[bold]Source: {source}[/]");
                        foreach (var rule in rules)
                        {
                            var prefix = rule.IsNegation ? "!" : "";
                            var suffix = rule.IsDirectoryOnly ? "/" : "";
                            AnsiConsole.MarkupLine($"  {prefix}{rule.Pattern}{suffix}");
                        }
                    }
                }
            });
        }
    }
}
```

---

### Error Codes

| Code | Meaning | User Message |
|------|---------|--------------|
| ACODE-IGN-001 | Invalid pattern syntax | Pattern syntax is invalid. Check for unbalanced brackets or invalid escape sequences. |
| ACODE-IGN-002 | Pattern parse error | Failed to parse gitignore file. Check file encoding and syntax. |
| ACODE-IGN-003 | File not accessible | Cannot access ignore file. Check file permissions. |
| ACODE-IGN-004 | Pattern too complex | Pattern is too complex and may cause performance issues. Simplify the pattern. |
| ACODE-IGN-005 | Path validation failed | Path contains invalid characters or traversal sequences. |

---

### Implementation Checklist

1. [ ] Create domain models (IgnoreRule, IgnoreSource, IgnoreCheckResult)
2. [ ] Create domain interfaces (IIgnoreService, IIgnoreRuleParser, IBinaryDetector)
3. [ ] Implement GitignoreParser with full syntax support
4. [ ] Implement PatternMatcher with glob support
5. [ ] Implement PatternCompiler with safety limits
6. [ ] Implement BinaryDetector with extension and magic number detection
7. [ ] Implement IgnoreCache for performance
8. [ ] Implement IgnoreService combining all sources
9. [ ] Implement PathValidator for security
10. [ ] Create IgnoreCommand CLI with check and list subcommands
11. [ ] Register services in DI container
12. [ ] Write unit tests for all components
13. [ ] Write integration tests
14. [ ] Write E2E tests
15. [ ] Document all error codes and troubleshooting

---

### Rollout Plan

| Phase | Description | Duration |
|-------|-------------|----------|
| 1 | Domain models and interfaces | 0.5 day |
| 2 | GitignoreParser implementation | 1 day |
| 3 | PatternMatcher and PatternCompiler | 1 day |
| 4 | BinaryDetector | 0.5 day |
| 5 | IgnoreService with all sources | 1 day |
| 6 | CLI commands | 0.5 day |
| 7 | Testing and documentation | 1 day |

---

**End of Task 015.a Specification**