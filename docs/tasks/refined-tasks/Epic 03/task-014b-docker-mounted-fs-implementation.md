# Task 014.b: Docker-Mounted FS Implementation

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 014 (RepoFS Abstraction), Task 014.a (Local FS)  

---

## Description

### Business Value

The Docker-Mounted File System implementation extends Agentic Coding Bot's reach to containerized development environments, enabling the agent to modify files within Docker containers. As container-based development becomes standard practice, this capability is essential for supporting modern development workflows.

Many development teams run their applications in containers for consistency between development and production environments. Without Docker FS support, developers would need to manually copy files between the host and container, breaking the seamless agent experience. This implementation enables the agent to work directly with containerized codebases, maintaining the same user experience as local development.

The Docker FS implementation addresses the unique challenges of container file access: higher latency than local I/O, potential permission differences, path translation between host and container, and the transient nature of container environments. Caching, intelligent error handling, and clear diagnostics ensure reliable operation despite these challenges.

### Return on Investment (ROI)

**Container Development Productivity Gains:**
- **Eliminated Manual File Sync:** Without DockerFS, developers must manually copy files between host and container using `docker cp` for each modification. For a typical development session with 50 file modifications, this requires 50 manual copy commands at ~15 seconds each = 12.5 minutes per session. With 4 sessions/day × 20 workdays/month = 1,000 minutes/month (16.7 hours) saved per developer.
- **At $100/hour developer rate:** $1,670/month per developer or **$20,040/year per developer**
- **For 10-developer team:** **$200,400 annual productivity savings**

**Development Workflow Continuity:**
- **Container parity:** Enables identical AI-assisted workflow in containerized environments as local development. Without this, container users lose AI assistance for 60-80% of development tasks (those requiring file modification).
- **Developer retention:** Containerized projects (estimated 40% of enterprise codebases) become equally supported, preventing developer frustration and tool abandonment.
- **Time-to-value:** New containerized projects get full AI assistance from day 1, accelerating onboarding by 3-5 days per project.

**Error Reduction:**
- **Eliminated sync errors:** Manual `docker cp` causes version mismatches in 2-5% of operations (wrong direction, stale copy, interrupted transfer). For a project with 10,000 agent file operations, this prevents 200-500 sync-related bugs.
- **At 30 min average debug time per sync bug:** 100-250 hours saved = **$10,000-$25,000 annual debugging savings**

**ROI Calculation:**
- Development cost: 80 hours (2 weeks) × $100/hour = $8,000
- Annual savings: $200,400 (productivity) + $17,500 (avg debugging) = **$217,900**
- **Payback period: 11 days**
- **Annual ROI: 2,724%**

### Scope

This task delivers the complete Docker-mounted file system implementation:

1. **DockerFileSystem Class:** IRepoFS implementation that executes file operations via `docker exec` commands. Provides the same interface as LocalFS while handling Docker-specific concerns.

2. **Docker Command Executor:** Secure execution of `docker exec` commands with proper shell escaping, timeout handling, and exit code interpretation.

3. **Mount Path Translation:** Bidirectional mapping between host paths and container paths. Supports multiple mount configurations for complex container setups.

4. **Operation Caching:** Reduces Docker exec overhead by caching directory listings and existence checks. Automatic invalidation on write operations with configurable TTL.

5. **Container Health Detection:** Verifies container availability and mount accessibility before operations, providing clear diagnostics when containers are unavailable.

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Task 014 (RepoFS) | Interface Implementation | Implements IRepoFS interface contract |
| Task 014.a (Local FS) | Behavior Sharing | Shares encoding detection and atomic write patterns |
| Task 014.c (Patching) | File Access | Patch applicator uses DockerFS for container-hosted repositories |
| Task 003 (DI) | Dependency Injection | Registered as alternative IRepoFS when Docker mode configured |
| Task 002 (Config) | Configuration | Docker settings from `repo.docker` config section |
| Task 011 (Session) | Session Context | Session determines container and mount configuration |
| Task 003.c (Audit) | Audit Logging | All container operations logged with container ID |

### Technical Architecture

DockerFileSystem follows a layered architecture with clear separation between Docker command execution, caching, and the IRepoFS interface:

```
┌──────────────────────────────────────────────────────────────┐
│                   Application Layer                           │
│  (Tools, Indexer, Context Packer using IRepoFS interface)    │
└──────────────────────┬───────────────────────────────────────┘
                       │ IRepoFS Interface
                       ▼
┌──────────────────────────────────────────────────────────────┐
│              DockerFileSystem (Main Class)                    │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │   Path       │  │   Cache      │  │   Health     │       │
│  │  Translator  │  │   Manager    │  │   Checker    │       │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘       │
│         │                  │                  │               │
│         └──────────────────┴──────────────────┘               │
│                            │                                  │
└────────────────────────────┼──────────────────────────────────┘
                             │ Docker Exec Commands
                             ▼
                  ┌──────────────────────┐
                  │  DockerCommandRunner │
                  │  - Shell escaping    │
                  │  - Timeout handling  │
                  │  - Exit code parsing │
                  └──────────┬───────────┘
                             │ docker exec -i <container> <cmd>
                             ▼
                  ┌──────────────────────┐
                  │   Docker Daemon      │
                  │   (Container Runtime)│
                  └──────────┬───────────┘
                             │
                             ▼
                  ┌──────────────────────┐
                  │  Target Container    │
                  │  /app (mounted repo) │
                  └──────────────────────┘
```

**Operation Flow Example (ReadFileAsync):**

1. Application calls `dockerFs.ReadFileAsync("src/Program.cs")`
2. DockerFileSystem.PathTranslator converts host path → container path: `/app/src/Program.cs`
3. DockerFileSystem checks cache for file content (cache miss)
4. DockerFileSystem.HealthChecker verifies container is running
5. DockerCommandRunner executes: `docker exec -i mycontainer cat '/app/src/Program.cs'`
6. Command returns content via stdout (exit code 0)
7. DockerFileSystem caches result with 60-second TTL
8. Content returned to application

### Architectural Decisions & Trade-offs

**Decision 1: Docker Exec vs Volume Mounts for File Access**

We use `docker exec` commands rather than directly reading Docker volume mounts from the host filesystem.

- **Rationale:** While volume mounts are visible on the host (`/var/lib/docker/volumes/...`), reading them directly bypasses container filesystem semantics (overlayfs layers, permissions, user mapping). Docker exec guarantees we see files exactly as the container sees them, with correct permissions and content.
- **Trade-off:** Docker exec adds 10-50ms latency per operation vs direct file I/O. We mitigate this with aggressive caching (60s TTL for directory listings, 30s for file existence).
- **Alternative Rejected:** Direct volume access would be faster but risks permission mismatches, missing overlay changes, and platform-specific volume driver issues.

**Decision 2: Caching with TTL vs No Caching**

We cache directory listings, file existence checks, and file metadata with configurable TTLs (default 30-60s). Write operations invalidate relevant cache entries.

- **Rationale:** Docker exec operations have significant overhead (process spawn, container exec setup, result marshaling). For read-heavy workloads like code indexing, caching reduces latency by 95% (5ms cached vs 100ms uncached).
- **Trade-off:** Cached data can become stale if files are modified outside the agent (e.g., manual edits inside container). We accept this risk because: (1) agent is primary modifier, (2) TTLs limit staleness window, (3) writes invalidate cache.
- **Alternative Rejected:** No caching would guarantee freshness but make indexing 20x slower (2 seconds vs 40 seconds for 1000 files).

**Decision 3: Single-Container Support (No Multi-Container)**

DockerFileSystem operates on one container at a time, configured per session. No automatic detection of related containers (e.g., app + database) or multi-container coordination.

- **Rationale:** Simplifies configuration, security model, and caching logic. 95% of use cases involve a single "workspace" container where code resides. Multi-container orchestration is out of scope for file system abstraction.
- **Trade-off:** Users running microservices must configure separate sessions for each container, or choose one primary container. This is acceptable for MVP.
- **Alternative Rejected:** Auto-discovery of containers (via docker-compose.yml) adds complexity, ambiguity (which container for which file?), and race conditions.

**Decision 4: Shell Escaping with Single Quotes**

We wrap all file path arguments in single quotes and escape embedded single quotes: `'path'\''with'\''quotes'`

- **Rationale:** Single-quote wrapping is the most secure shell escaping method - it disables all special character interpretation except single-quote itself. This prevents injection even with complex paths containing `$`, backticks, semicolons, etc.
- **Trade-off:** Paths with single quotes require escaped sequences (`'\''`), slightly increasing command length. This is a minor cost for guaranteed injection safety.
- **Alternative Rejected:** Backslash escaping is error-prone (must escape `\`, `"`, `$`, backticks, etc.). Double-quote escaping still allows `$` and backtick expansion.

**Decision 5: Timeout Default 30 Seconds (Max 300)**

Commands timeout after 30 seconds by default, configurable up to 5 minutes.

- **Rationale:** Most file operations complete in <1 second. 30 seconds accommodates slow containers, cold starts, and large files (10MB @ 1MB/s = 10s). Longer timeouts risk hanging indefinitely on container issues.
- **Trade-off:** Legitimate long operations (reading 100MB file) may timeout. Users must increase timeout for specific use cases. We chose conservative default to prevent hangs.
- **Alternative Rejected:** No timeout would hang forever on container deadlock. Unlimited timeout requires complex cancellation infrastructure.

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Container not found | All operations fail | Verify container at startup, clear error message |
| Container not running | All operations fail | Health check before operations, suggest `docker start` |
| Mount not accessible | Path operations fail | Validate mount configuration, report mapping issues |
| Permission denied in container | Read/write blocked | Report user context, suggest container permissions |
| Command timeout | Operation blocked | Configurable timeout, suggest container health check |
| Shell injection attempt | Security violation | Strict argument escaping, reject suspicious input |
| High latency | Slow operations | Aggressive caching, batch operations where possible |
| Container restart mid-operation | Transient failure | Retry with exponential backoff, cache invalidation |

### Assumptions

1. Docker daemon is running and accessible on the local machine
2. The target container is running and has the repository mounted
3. The container has `cat`, `find`, `stat`, `rm`, and `mkdir` commands available
4. Docker exec operations complete within configurable timeout
5. Mount paths are correctly configured in the agent configuration
6. The agent process has permission to execute Docker commands
7. File paths inside the container use forward slashes (Linux containers)
8. Container environment remains stable during operation sequences

### Security Considerations

#### Threat 1: Shell Injection via Docker Exec Commands

**Risk Description:** Attackers can inject malicious shell commands through file paths or content that gets passed to `docker exec`. Since Docker exec runs commands inside a container with potentially elevated privileges, successful injection could compromise the container environment, access sensitive data, or pivot to other containers.

**Attack Scenario:**
A malicious file path like `src/file.cs; rm -rf /app/*` could be passed to a read operation. If not properly escaped, the command becomes:
```bash
docker exec mycontainer cat src/file.cs; rm -rf /app/*
```
This would read the file AND delete all files in /app inside the container.

**Complete Mitigation Implementation:**

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AgenticCoder.Infrastructure.FileSystem.Docker;

/// <summary>
/// Provides secure shell argument escaping for Docker exec commands.
/// Uses single-quote wrapping with proper quote escaping to prevent injection.
/// </summary>
public sealed partial class DockerCommandSanitizer
{
    // Characters that have special meaning in bash and MUST be escaped
    private static readonly HashSet<char> DangerousChars = new()
    {
        '\'', '"', '\\', '$', '`', '!', '*', '?', '[', ']',
        '{', '}', '(', ')', '|', '&', ';', '<', '>', '\n',
        '\r', '\t', '\0', ' '
    };

    // Maximum argument length to prevent buffer overflow attacks
    private const int MaxArgumentLength = 4096;

    // Pattern for valid container names (alphanumeric, underscore, dash, dot)
    [GeneratedRegex(@"^[a-zA-Z0-9][a-zA-Z0-9_.\-]*$", RegexOptions.Compiled)]
    private static partial Regex ContainerNamePattern();

    /// <summary>
    /// Escapes a string for safe use as a shell argument in docker exec.
    /// Uses single-quote wrapping: 'argument' with internal quotes escaped.
    /// </summary>
    /// <param name="argument">The raw argument to escape</param>
    /// <returns>Escaped argument safe for shell use</returns>
    /// <exception cref="ArgumentException">If argument contains null bytes or exceeds max length</exception>
    public string EscapeShellArgument(string argument)
    {
        ArgumentNullException.ThrowIfNull(argument);

        if (argument.Length > MaxArgumentLength)
        {
            throw new ArgumentException(
                $"Argument exceeds maximum length of {MaxArgumentLength} characters",
                nameof(argument));
        }

        // Null bytes are always dangerous - reject immediately
        if (argument.Contains('\0'))
        {
            throw new ArgumentException(
                "Argument contains null byte which is not permitted",
                nameof(argument));
        }

        // Empty string becomes ''
        if (string.IsNullOrEmpty(argument))
        {
            return "''";
        }

        // Check if any escaping is needed
        var needsEscaping = false;
        foreach (var c in argument)
        {
            if (DangerousChars.Contains(c))
            {
                needsEscaping = true;
                break;
            }
        }

        if (!needsEscaping)
        {
            return argument;
        }

        // Use single-quote wrapping with quote escaping
        // For single quotes inside: replace ' with '\''
        // This ends the string, adds escaped quote, starts new string
        var escaped = new StringBuilder(argument.Length + 10);
        escaped.Append('\'');

        foreach (var c in argument)
        {
            if (c == '\'')
            {
                // End current string, add escaped single quote, start new string
                escaped.Append("'\\''");
            }
            else
            {
                escaped.Append(c);
            }
        }

        escaped.Append('\'');
        return escaped.ToString();
    }

    /// <summary>
    /// Validates and escapes a container name or ID.
    /// </summary>
    public string ValidateContainerName(string containerName)
    {
        ArgumentNullException.ThrowIfNull(containerName);

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new ArgumentException("Container name cannot be empty", nameof(containerName));
        }

        // Container names must match Docker's naming rules
        if (!ContainerNamePattern().IsMatch(containerName))
        {
            throw new ArgumentException(
                $"Invalid container name format: {containerName}. " +
                "Container names must start with alphanumeric and contain only alphanumeric, underscore, dash, or dot.",
                nameof(containerName));
        }

        // Additional length check (Docker limit is 128)
        if (containerName.Length > 128)
        {
            throw new ArgumentException(
                "Container name exceeds maximum length of 128 characters",
                nameof(containerName));
        }

        return containerName;
    }

    /// <summary>
    /// Builds a complete docker exec command with all arguments safely escaped.
    /// </summary>
    public string BuildDockerExecCommand(
        string containerName,
        string command,
        params string[] arguments)
    {
        var validatedContainer = ValidateContainerName(containerName);

        var sb = new StringBuilder();
        sb.Append("docker exec ");
        sb.Append(validatedContainer);
        sb.Append(' ');
        sb.Append(command);

        foreach (var arg in arguments)
        {
            sb.Append(' ');
            sb.Append(EscapeShellArgument(arg));
        }

        return sb.ToString();
    }
}

// Unit tests for the sanitizer
public sealed class DockerCommandSanitizerTests
{
    private readonly DockerCommandSanitizer _sut = new();

    [Fact]
    public void EscapeShellArgument_Should_Return_Empty_Quotes_For_Empty_String()
    {
        // Arrange
        var input = "";

        // Act
        var result = _sut.EscapeShellArgument(input);

        // Assert
        result.Should().Be("''");
    }

    [Fact]
    public void EscapeShellArgument_Should_Return_Unchanged_For_Safe_String()
    {
        // Arrange
        var input = "simple-file.cs";

        // Act
        var result = _sut.EscapeShellArgument(input);

        // Assert
        result.Should().Be("simple-file.cs");
    }

    [Theory]
    [InlineData("file with spaces.cs", "'file with spaces.cs'")]
    [InlineData("file's.cs", "'file'\\''s.cs'")]
    [InlineData("file;rm -rf /", "'file;rm -rf /'")]
    [InlineData("$(whoami)", "'$(whoami)'")]
    [InlineData("`id`", "'`id`'")]
    [InlineData("file|cat /etc/passwd", "'file|cat /etc/passwd'")]
    public void EscapeShellArgument_Should_Escape_Dangerous_Characters(
        string input, string expected)
    {
        // Act
        var result = _sut.EscapeShellArgument(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void EscapeShellArgument_Should_Reject_Null_Bytes()
    {
        // Arrange
        var input = "file\0name.cs";

        // Act
        var act = () => _sut.EscapeShellArgument(input);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*null byte*");
    }

    [Fact]
    public void EscapeShellArgument_Should_Reject_Overly_Long_Arguments()
    {
        // Arrange
        var input = new string('a', 5000);

        // Act
        var act = () => _sut.EscapeShellArgument(input);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*maximum length*");
    }

    [Theory]
    [InlineData("valid-container")]
    [InlineData("container_name")]
    [InlineData("container.name")]
    [InlineData("Container123")]
    public void ValidateContainerName_Should_Accept_Valid_Names(string name)
    {
        // Act
        var result = _sut.ValidateContainerName(name);

        // Assert
        result.Should().Be(name);
    }

    [Theory]
    [InlineData("-invalid")]
    [InlineData(".invalid")]
    [InlineData("_invalid")]
    [InlineData("container;rm")]
    [InlineData("container$(id)")]
    [InlineData("")]
    public void ValidateContainerName_Should_Reject_Invalid_Names(string name)
    {
        // Act
        var act = () => _sut.ValidateContainerName(name);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BuildDockerExecCommand_Should_Create_Safe_Command()
    {
        // Arrange
        var container = "my-container";
        var command = "cat";
        var path = "/app/src/file with spaces.cs";

        // Act
        var result = _sut.BuildDockerExecCommand(container, command, path);

        // Assert
        result.Should().Be("docker exec my-container cat '/app/src/file with spaces.cs'");
    }
}
```

---

#### Threat 2: Container Path Traversal and Mount Escape

**Risk Description:** Attackers can use path traversal sequences (`../`) or symlinks to escape the configured mount boundaries and access files outside the intended repository directory inside the container. This could expose sensitive container files like `/etc/passwd`, environment variables, or other mounted secrets.

**Attack Scenario:**
A request to read `src/../../etc/passwd` could traverse outside the mount point to access system files. Or a symlink inside the repo pointing to `/etc/shadow` could be followed to read sensitive files.

**Complete Mitigation Implementation:**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AgenticCoder.Infrastructure.FileSystem.Docker;

/// <summary>
/// Validates paths to prevent traversal attacks and mount boundary escapes
/// within Docker container file operations.
/// </summary>
public sealed class ContainerPathValidator
{
    private readonly IReadOnlyList<MountMapping> _mounts;
    private readonly DockerCommandExecutor _executor;
    private readonly string _containerName;

    // Dangerous path components that could indicate traversal attempts
    private static readonly string[] DangerousComponents = new[]
    {
        "..", "...", "....", // Traversal sequences
        "~", // Home directory expansion
        "\0" // Null byte injection
    };

    // Paths that should never be accessible regardless of mounts
    private static readonly string[] ForbiddenPaths = new[]
    {
        "/etc/shadow", "/etc/passwd", "/etc/sudoers",
        "/root", "/proc", "/sys", "/dev",
        "/var/run/docker.sock", "/run/docker.sock"
    };

    public ContainerPathValidator(
        IReadOnlyList<MountMapping> mounts,
        DockerCommandExecutor executor,
        string containerName)
    {
        _mounts = mounts ?? throw new ArgumentNullException(nameof(mounts));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _containerName = containerName ?? throw new ArgumentNullException(nameof(containerName));
    }

    /// <summary>
    /// Validates a container path is within allowed mount boundaries.
    /// </summary>
    /// <param name="containerPath">Path inside container to validate</param>
    /// <returns>Validated, normalized container path</returns>
    /// <exception cref="PathTraversalException">If path attempts to escape boundaries</exception>
    /// <exception cref="ForbiddenPathException">If path accesses forbidden locations</exception>
    public string ValidatePath(string containerPath)
    {
        ArgumentNullException.ThrowIfNull(containerPath);

        // Check for dangerous components in raw path
        CheckForDangerousComponents(containerPath);

        // Normalize the path (resolve . and .., convert separators)
        var normalized = NormalizePath(containerPath);

        // Check if normalized path is in forbidden list
        CheckForbiddenPaths(normalized);

        // Verify path is within a configured mount
        var mount = FindContainingMount(normalized);
        if (mount == null)
        {
            throw new PathTraversalException(
                $"Path '{containerPath}' is not within any configured mount boundary. " +
                $"Configured mounts: {string.Join(", ", _mounts.Select(m => m.ContainerPath))}");
        }

        // Verify the normalized path still starts with mount after normalization
        if (!normalized.StartsWith(mount.ContainerPath, StringComparison.Ordinal))
        {
            throw new PathTraversalException(
                $"Path '{containerPath}' normalizes to '{normalized}' which escapes " +
                $"mount boundary '{mount.ContainerPath}'");
        }

        return normalized;
    }

    /// <summary>
    /// Validates path and additionally checks for symlink escapes by resolving
    /// the real path inside the container.
    /// </summary>
    public async Task<string> ValidatePathWithSymlinkCheckAsync(
        string containerPath,
        CancellationToken ct = default)
    {
        // First do basic validation
        var normalized = ValidatePath(containerPath);

        // Now resolve the real path inside the container
        var realPath = await ResolveRealPathAsync(normalized, ct);

        // Validate the resolved path is also within bounds
        var mount = FindContainingMount(realPath);
        if (mount == null || !realPath.StartsWith(mount.ContainerPath, StringComparison.Ordinal))
        {
            throw new SymlinkEscapeException(
                $"Path '{containerPath}' resolves to '{realPath}' via symlink which " +
                $"escapes mount boundary. This may be a symlink attack.");
        }

        return realPath;
    }

    private void CheckForDangerousComponents(string path)
    {
        foreach (var dangerous in DangerousComponents)
        {
            if (path.Contains(dangerous, StringComparison.Ordinal))
            {
                throw new PathTraversalException(
                    $"Path contains dangerous component '{dangerous}': {path}");
            }
        }

        // Check for encoded traversal attempts
        if (path.Contains("%2e", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("%2f", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("%5c", StringComparison.OrdinalIgnoreCase))
        {
            throw new PathTraversalException(
                $"Path contains URL-encoded traversal sequences: {path}");
        }
    }

    private void CheckForbiddenPaths(string normalizedPath)
    {
        foreach (var forbidden in ForbiddenPaths)
        {
            if (normalizedPath.Equals(forbidden, StringComparison.Ordinal) ||
                normalizedPath.StartsWith(forbidden + "/", StringComparison.Ordinal))
            {
                throw new ForbiddenPathException(
                    $"Access to '{forbidden}' is forbidden for security reasons");
            }
        }
    }

    private string NormalizePath(string path)
    {
        // Convert backslashes to forward slashes
        var normalized = path.Replace('\\', '/');

        // Ensure absolute path
        if (!normalized.StartsWith('/'))
        {
            throw new ArgumentException(
                $"Container path must be absolute: {path}",
                nameof(path));
        }

        // Split and resolve path components
        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var resolved = new Stack<string>();

        foreach (var segment in segments)
        {
            if (segment == ".")
            {
                // Current directory - skip
                continue;
            }
            else if (segment == "..")
            {
                // Parent directory - pop if we can
                if (resolved.Count > 0)
                {
                    resolved.Pop();
                }
                // If we can't pop, we're trying to go above root - this is suspicious
                // but we'll catch it in mount boundary check
            }
            else
            {
                resolved.Push(segment);
            }
        }

        return "/" + string.Join("/", resolved.Reverse());
    }

    private MountMapping? FindContainingMount(string containerPath)
    {
        // Find the most specific (longest) mount that contains this path
        return _mounts
            .Where(m => containerPath.StartsWith(m.ContainerPath, StringComparison.Ordinal) ||
                        containerPath == m.ContainerPath.TrimEnd('/'))
            .OrderByDescending(m => m.ContainerPath.Length)
            .FirstOrDefault();
    }

    private async Task<string> ResolveRealPathAsync(string path, CancellationToken ct)
    {
        // Use readlink -f inside container to resolve symlinks
        var command = $"readlink -f {_executor.EscapeArgument(path)}";
        var result = await _executor.ExecAsync(_containerName, command, ct);

        if (result.ExitCode != 0)
        {
            // File doesn't exist - that's okay, return original path
            return path;
        }

        return result.Output.Trim();
    }
}

/// <summary>
/// Thrown when a path traversal attack is detected.
/// </summary>
public class PathTraversalException : SecurityException
{
    public PathTraversalException(string message) : base(message) { }
}

/// <summary>
/// Thrown when access to a forbidden path is attempted.
/// </summary>
public class ForbiddenPathException : SecurityException
{
    public ForbiddenPathException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a symlink would escape the mount boundary.
/// </summary>
public class SymlinkEscapeException : SecurityException
{
    public SymlinkEscapeException(string message) : base(message) { }
}

// Unit tests for path validation
public sealed class ContainerPathValidatorTests
{
    private readonly List<MountMapping> _mounts = new()
    {
        new MountMapping("/home/user/project", "/app")
    };

    private readonly Mock<DockerCommandExecutor> _mockExecutor = new();
    private readonly ContainerPathValidator _sut;

    public ContainerPathValidatorTests()
    {
        _sut = new ContainerPathValidator(_mounts, _mockExecutor.Object, "test-container");
    }

    [Theory]
    [InlineData("/app/src/file.cs")]
    [InlineData("/app/tests/UnitTests.cs")]
    [InlineData("/app")]
    public void ValidatePath_Should_Accept_Paths_Within_Mount(string path)
    {
        // Act
        var result = _sut.ValidatePath(path);

        // Assert
        result.Should().StartWith("/app");
    }

    [Theory]
    [InlineData("/app/../etc/passwd")]
    [InlineData("/app/src/../../etc/passwd")]
    [InlineData("/etc/passwd")]
    [InlineData("/var/run/docker.sock")]
    public void ValidatePath_Should_Reject_Traversal_Attempts(string path)
    {
        // Act
        var act = () => _sut.ValidatePath(path);

        // Assert
        act.Should().Throw<Exception>();
    }

    [Theory]
    [InlineData("/app/src/%2e%2e/etc/passwd")]
    [InlineData("/app/src/%2e%2e%2fetc%2fpasswd")]
    public void ValidatePath_Should_Reject_Encoded_Traversal(string path)
    {
        // Act
        var act = () => _sut.ValidatePath(path);

        // Assert
        act.Should().Throw<PathTraversalException>()
            .WithMessage("*URL-encoded*");
    }

    [Fact]
    public async Task ValidatePathWithSymlinkCheck_Should_Detect_Symlink_Escape()
    {
        // Arrange
        var path = "/app/evil-link";
        _mockExecutor
            .Setup(x => x.ExecAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DockerExecResult { ExitCode = 0, Output = "/etc/shadow" });

        // Act
        var act = () => _sut.ValidatePathWithSymlinkCheckAsync(path);

        // Assert
        await act.Should().ThrowAsync<SymlinkEscapeException>()
            .WithMessage("*symlink*escape*");
    }
}
```

---

#### Threat 3: Container Name/ID Injection

**Risk Description:** If container names or IDs are not validated, an attacker could inject malicious container references or additional Docker command flags. This could redirect operations to a different container under attacker control or inject additional commands.

**Attack Scenario:**
A malicious container name like `mycontainer -v /:/hostroot ubuntu cat /hostroot/etc/shadow #` could be injected to create a new container with full host access instead of connecting to an existing one.

**Complete Mitigation Implementation:**

```csharp
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AgenticCoder.Infrastructure.FileSystem.Docker;

/// <summary>
/// Validates Docker container names and IDs to prevent injection attacks.
/// Ensures only legitimate container references are used in docker exec commands.
/// </summary>
public sealed partial class ContainerNameValidator
{
    // Docker container names: [a-zA-Z0-9][a-zA-Z0-9_.-]{0,127}
    [GeneratedRegex(@"^[a-zA-Z0-9][a-zA-Z0-9_.\-]{0,127}$", RegexOptions.Compiled)]
    private static partial Regex ContainerNameRegex();

    // Docker container IDs: 64 hex characters (short form: 12+ chars)
    [GeneratedRegex(@"^[a-f0-9]{12,64}$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex ContainerIdRegex();

    // Characters that should never appear in container references
    private static readonly char[] ForbiddenChars = new[]
    {
        ' ', '\t', '\n', '\r', // Whitespace
        ';', '|', '&', // Command chaining
        '$', '`', // Variable/command expansion
        '<', '>', // Redirection
        '(', ')', // Subshells
        '{', '}', // Brace expansion
        '[', ']', // Glob patterns
        '#', // Comments
        '\\', // Escape character
        '"', '\'' // Quotes
    };

    private readonly IDockerDaemonClient _dockerClient;

    public ContainerNameValidator(IDockerDaemonClient dockerClient)
    {
        _dockerClient = dockerClient ?? throw new ArgumentNullException(nameof(dockerClient));
    }

    /// <summary>
    /// Validates a container reference (name or ID) for safe use in docker exec.
    /// </summary>
    /// <param name="containerRef">Container name or ID to validate</param>
    /// <returns>Validated container reference</returns>
    /// <exception cref="InvalidContainerReferenceException">If reference is invalid</exception>
    public string ValidateContainerReference(string containerRef)
    {
        ArgumentNullException.ThrowIfNull(containerRef);

        if (string.IsNullOrWhiteSpace(containerRef))
        {
            throw new InvalidContainerReferenceException(
                "Container reference cannot be empty or whitespace");
        }

        // Check for forbidden characters first
        foreach (var forbidden in ForbiddenChars)
        {
            if (containerRef.Contains(forbidden))
            {
                throw new InvalidContainerReferenceException(
                    $"Container reference contains forbidden character: '{forbidden}'");
            }
        }

        // Must match either name or ID pattern
        var isValidName = ContainerNameRegex().IsMatch(containerRef);
        var isValidId = ContainerIdRegex().IsMatch(containerRef);

        if (!isValidName && !isValidId)
        {
            throw new InvalidContainerReferenceException(
                $"Container reference '{containerRef}' does not match valid name or ID format. " +
                "Names must start with alphanumeric and contain only alphanumeric, underscore, dash, or dot. " +
                "IDs must be 12-64 hexadecimal characters.");
        }

        return containerRef;
    }

    /// <summary>
    /// Validates container reference and verifies the container exists and is running.
    /// </summary>
    public async Task<ContainerInfo> ValidateAndVerifyContainerAsync(
        string containerRef,
        CancellationToken ct = default)
    {
        // First validate the format
        var validated = ValidateContainerReference(containerRef);

        // Then verify the container exists
        var containerInfo = await _dockerClient.InspectContainerAsync(validated, ct);

        if (containerInfo == null)
        {
            throw new ContainerNotFoundException(
                $"Container '{validated}' does not exist");
        }

        if (containerInfo.State != ContainerState.Running)
        {
            throw new ContainerNotRunningException(
                $"Container '{validated}' exists but is not running (state: {containerInfo.State}). " +
                "Start the container with: docker start " + validated);
        }

        return containerInfo;
    }

    /// <summary>
    /// Creates a safe docker exec command prefix with validated container.
    /// </summary>
    public string CreateSafeExecPrefix(string containerRef, bool interactive = false)
    {
        var validated = ValidateContainerReference(containerRef);

        var flags = interactive ? "-it" : "-i";

        // Build command with no possibility of injection
        return $"docker exec {flags} {validated}";
    }
}

/// <summary>
/// Information about a validated container.
/// </summary>
public sealed class ContainerInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required ContainerState State { get; init; }
    public required IReadOnlyList<MountInfo> Mounts { get; init; }
}

public enum ContainerState
{
    Created,
    Running,
    Paused,
    Restarting,
    Removing,
    Exited,
    Dead
}

public sealed class MountInfo
{
    public required string HostPath { get; init; }
    public required string ContainerPath { get; init; }
    public required bool ReadOnly { get; init; }
}

public class InvalidContainerReferenceException : ArgumentException
{
    public InvalidContainerReferenceException(string message) : base(message) { }
}

public class ContainerNotFoundException : Exception
{
    public ContainerNotFoundException(string message) : base(message) { }
}

public class ContainerNotRunningException : Exception
{
    public ContainerNotRunningException(string message) : base(message) { }
}

// Unit tests
public sealed class ContainerNameValidatorTests
{
    private readonly Mock<IDockerDaemonClient> _mockClient = new();
    private readonly ContainerNameValidator _sut;

    public ContainerNameValidatorTests()
    {
        _sut = new ContainerNameValidator(_mockClient.Object);
    }

    [Theory]
    [InlineData("my-container")]
    [InlineData("my_container")]
    [InlineData("my.container")]
    [InlineData("MyContainer123")]
    [InlineData("a")] // Minimum valid name
    public void ValidateContainerReference_Should_Accept_Valid_Names(string name)
    {
        // Act
        var result = _sut.ValidateContainerReference(name);

        // Assert
        result.Should().Be(name);
    }

    [Theory]
    [InlineData("abc123def456")] // 12 char short ID
    [InlineData("abc123def456789012345678901234567890123456789012345678901234")] // 60 char ID
    public void ValidateContainerReference_Should_Accept_Valid_IDs(string id)
    {
        // Act
        var result = _sut.ValidateContainerReference(id);

        // Assert
        result.Should().Be(id);
    }

    [Theory]
    [InlineData("container; rm -rf /", "';'")]
    [InlineData("container | cat /etc/passwd", "'|'")]
    [InlineData("container && malicious", "'&'")]
    [InlineData("container$(id)", "'$'")]
    [InlineData("container`whoami`", "'`'")]
    [InlineData("container > /dev/null", "'>'")]
    [InlineData("container -v /:/host", "' '")]
    public void ValidateContainerReference_Should_Reject_Injection_Attempts(
        string input, string expectedForbiddenChar)
    {
        // Act
        var act = () => _sut.ValidateContainerReference(input);

        // Assert
        act.Should().Throw<InvalidContainerReferenceException>()
            .WithMessage($"*forbidden character*{expectedForbiddenChar}*");
    }

    [Theory]
    [InlineData("-invalid")] // Starts with dash
    [InlineData(".invalid")] // Starts with dot
    [InlineData("_invalid")] // Starts with underscore
    [InlineData("")] // Empty
    [InlineData("   ")] // Whitespace only
    public void ValidateContainerReference_Should_Reject_Invalid_Formats(string input)
    {
        // Act
        var act = () => _sut.ValidateContainerReference(input);

        // Assert
        act.Should().Throw<Exception>();
    }

    [Fact]
    public async Task ValidateAndVerify_Should_Throw_When_Container_Not_Running()
    {
        // Arrange
        _mockClient
            .Setup(x => x.InspectContainerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContainerInfo
            {
                Id = "abc123",
                Name = "my-container",
                State = ContainerState.Exited,
                Mounts = Array.Empty<MountInfo>()
            });

        // Act
        var act = () => _sut.ValidateAndVerifyContainerAsync("my-container");

        // Assert
        await act.Should().ThrowAsync<ContainerNotRunningException>()
            .WithMessage("*not running*Exited*");
    }
}
```

---

#### Threat 4: Credential Exposure in Docker Commands

**Risk Description:** Sensitive information like API keys, tokens, or passwords could be inadvertently exposed in Docker command arguments, process lists (`ps aux`), or logs. This creates opportunities for credential theft via shoulder surfing, log aggregation, or process monitoring.

**Attack Scenario:**
If file content containing secrets is passed directly to `echo` commands for writing, the secrets appear in the process list and shell history. An attacker with read access to `/proc` or command history could extract these credentials.

**Complete Mitigation Implementation:**

```csharp
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgenticCoder.Infrastructure.FileSystem.Docker;

/// <summary>
/// Handles credential-safe operations for Docker file system commands.
/// Ensures sensitive content never appears in process arguments or logs.
/// </summary>
public sealed class SafeDockerCredentialHandler
{
    private readonly DockerCommandExecutor _executor;
    private readonly string _containerName;
    private readonly IAuditLogger _auditLogger;

    // Patterns that indicate potentially sensitive content
    private static readonly string[] SensitivePatterns = new[]
    {
        "password", "passwd", "secret", "token", "apikey", "api_key",
        "api-key", "credential", "private_key", "privatekey", "private-key",
        "auth", "bearer", "jwt", "session", "cookie", "access_key",
        "secret_key", "aws_", "azure_", "gcp_", "PRIVATE KEY"
    };

    // Maximum content size to pass via stdin (larger uses temp file)
    private const int MaxStdinContentSize = 65536;

    public SafeDockerCredentialHandler(
        DockerCommandExecutor executor,
        string containerName,
        IAuditLogger auditLogger)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _containerName = containerName ?? throw new ArgumentNullException(nameof(containerName));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
    }

    /// <summary>
    /// Writes content to a file inside the container without exposing
    /// the content in process arguments.
    /// </summary>
    public async Task WriteSecurelyAsync(
        string containerPath,
        string content,
        CancellationToken ct = default)
    {
        var containsSensitive = DetectSensitiveContent(content);

        if (containsSensitive)
        {
            _auditLogger.LogSensitiveOperation(
                "DockerFS.WriteSecure",
                $"Writing potentially sensitive content to {containerPath}",
                _containerName);
        }

        // Never put content in command line arguments
        // Use stdin piping or base64 encoding through a temporary mechanism

        if (content.Length <= MaxStdinContentSize)
        {
            // Small content: use stdin piping (content not in ps output)
            await WriteViaStdinAsync(containerPath, content, ct);
        }
        else
        {
            // Large content: use temp file on host, copy, then delete
            await WriteViaTempFileAsync(containerPath, content, ct);
        }
    }

    /// <summary>
    /// Reads a file that may contain credentials, with secure logging.
    /// </summary>
    public async Task<string> ReadSecurelyAsync(
        string containerPath,
        CancellationToken ct = default)
    {
        // Read via docker exec - content goes through stdout, not visible in ps
        var content = await _executor.ExecWithOutputAsync(
            _containerName,
            $"cat {_executor.EscapeArgument(containerPath)}",
            ct);

        var containsSensitive = DetectSensitiveContent(content);

        if (containsSensitive)
        {
            _auditLogger.LogSensitiveOperation(
                "DockerFS.ReadSecure",
                $"Read potentially sensitive content from {containerPath}",
                _containerName);
        }

        return content;
    }

    /// <summary>
    /// Creates a redacted version of content for safe logging.
    /// </summary>
    public static string RedactForLogging(string content, int maxLength = 100)
    {
        if (string.IsNullOrEmpty(content))
        {
            return "[empty]";
        }

        // Check if content appears sensitive
        if (SensitivePatterns.Any(p =>
            content.Contains(p, StringComparison.OrdinalIgnoreCase)))
        {
            return "[REDACTED - potentially sensitive content]";
        }

        if (content.Length <= maxLength)
        {
            return content;
        }

        return content[..maxLength] + $"... [truncated, {content.Length} total chars]";
    }

    private async Task WriteViaStdinAsync(
        string containerPath,
        string content,
        CancellationToken ct)
    {
        // Use tee to write stdin to file - content flows through stdin, not args
        // First ensure parent directory exists
        var parentDir = Path.GetDirectoryName(containerPath)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(parentDir))
        {
            await _executor.ExecAsync(
                _containerName,
                $"mkdir -p {_executor.EscapeArgument(parentDir)}",
                ct);
        }

        // Write via stdin piping
        // The content is passed to docker exec's stdin, not as an argument
        await _executor.ExecWithStdinAsync(
            _containerName,
            $"tee {_executor.EscapeArgument(containerPath)} > /dev/null",
            content,
            ct);
    }

    private async Task WriteViaTempFileAsync(
        string containerPath,
        string content,
        CancellationToken ct)
    {
        // For large files, use base64 to avoid shell interpretation issues
        var base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
        var tempPath = $"/tmp/.acode-{GenerateSecureId()}.tmp";

        try
        {
            // Create parent directory
            var parentDir = Path.GetDirectoryName(containerPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parentDir))
            {
                await _executor.ExecAsync(
                    _containerName,
                    $"mkdir -p {_executor.EscapeArgument(parentDir)}",
                    ct);
            }

            // Write base64 content to temp file via stdin
            await _executor.ExecWithStdinAsync(
                _containerName,
                $"tee {_executor.EscapeArgument(tempPath)} > /dev/null",
                base64Content,
                ct);

            // Decode and move atomically
            await _executor.ExecAsync(
                _containerName,
                $"base64 -d {_executor.EscapeArgument(tempPath)} > {_executor.EscapeArgument(containerPath + ".new")} && " +
                $"mv {_executor.EscapeArgument(containerPath + ".new")} {_executor.EscapeArgument(containerPath)}",
                ct);
        }
        finally
        {
            // Clean up temp file
            await _executor.ExecAsync(
                _containerName,
                $"rm -f {_executor.EscapeArgument(tempPath)} {_executor.EscapeArgument(containerPath + ".new")}",
                ct);
        }
    }

    private static bool DetectSensitiveContent(string content)
    {
        return SensitivePatterns.Any(pattern =>
            content.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static string GenerateSecureId()
    {
        var bytes = RandomNumberGenerator.GetBytes(8);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

// Unit tests
public sealed class SafeDockerCredentialHandlerTests
{
    [Theory]
    [InlineData("password=secret123", true)]
    [InlineData("api_key: abc123xyz", true)]
    [InlineData("Authorization: Bearer eyJ...", true)]
    [InlineData("-----BEGIN PRIVATE KEY-----", true)]
    [InlineData("AWS_SECRET_ACCESS_KEY=xxx", true)]
    [InlineData("normal content here", false)]
    [InlineData("console.log('hello')", false)]
    public void DetectSensitiveContent_Should_Identify_Patterns(
        string content, bool expectedSensitive)
    {
        // This tests the internal detection logic
        var result = SensitivePatterns.Any(p =>
            content.Contains(p, StringComparison.OrdinalIgnoreCase));

        result.Should().Be(expectedSensitive);
    }

    [Theory]
    [InlineData("password=secret", "[REDACTED - potentially sensitive content]")]
    [InlineData("hello world", "hello world")]
    [InlineData("", "[empty]")]
    public void RedactForLogging_Should_Hide_Sensitive_Content(
        string input, string expected)
    {
        // Act
        var result = SafeDockerCredentialHandler.RedactForLogging(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void RedactForLogging_Should_Truncate_Long_NonSensitive_Content()
    {
        // Arrange
        var longContent = new string('a', 500);

        // Act
        var result = SafeDockerCredentialHandler.RedactForLogging(longContent);

        // Assert
        result.Should().StartWith(new string('a', 100));
        result.Should().Contain("truncated");
        result.Should().Contain("500 total chars");
    }
}
```

---

#### Threat 5: Mount Point Escape via Relative Paths

**Risk Description:** Even with validated absolute paths, an attacker could exploit relative path handling or path canonicalization differences between the host and container to escape mount boundaries. Race conditions during path validation and file access could also be exploited (TOCTOU attacks).

**Attack Scenario:**
An attacker creates a symlink during the time between path validation and file access. Or exploits differences in how the host OS and container OS handle Unicode normalization or case sensitivity to access files outside the mount.

**Complete Mitigation Implementation:**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgenticCoder.Infrastructure.FileSystem.Docker;

/// <summary>
/// Enforces mount boundaries with TOCTOU-safe file operations.
/// All file operations verify path containment atomically with the operation itself.
/// </summary>
public sealed class MountBoundaryEnforcer
{
    private readonly IReadOnlyList<MountMapping> _mounts;
    private readonly DockerCommandExecutor _executor;
    private readonly string _containerName;
    private readonly SemaphoreSlim _operationLock = new(1, 1);

    public MountBoundaryEnforcer(
        IReadOnlyList<MountMapping> mounts,
        DockerCommandExecutor executor,
        string containerName)
    {
        _mounts = mounts ?? throw new ArgumentNullException(nameof(mounts));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _containerName = containerName ?? throw new ArgumentNullException(nameof(containerName));

        if (_mounts.Count == 0)
        {
            throw new ArgumentException("At least one mount mapping is required", nameof(mounts));
        }
    }

    /// <summary>
    /// Performs a read operation with atomic boundary verification.
    /// The path is resolved and verified in the same shell command as the read,
    /// preventing TOCTOU attacks.
    /// </summary>
    public async Task<string> ReadWithBoundaryCheckAsync(
        string requestedPath,
        CancellationToken ct = default)
    {
        var mount = FindBestMount(requestedPath);
        if (mount == null)
        {
            throw new MountBoundaryViolationException(
                $"Path '{requestedPath}' is not within any configured mount");
        }

        // Atomic operation: resolve real path, verify it's within mount, then read
        // All in one shell command to prevent TOCTOU
        var script = BuildAtomicReadScript(requestedPath, mount.ContainerPath);

        var result = await _executor.ExecAsync(_containerName, script, ct);

        if (result.ExitCode == 99)
        {
            throw new MountBoundaryViolationException(
                $"Path '{requestedPath}' resolved outside mount boundary '{mount.ContainerPath}'");
        }

        if (result.ExitCode != 0)
        {
            throw new IOException($"Failed to read file: {result.Error}");
        }

        return result.Output;
    }

    /// <summary>
    /// Performs a write operation with atomic boundary verification.
    /// </summary>
    public async Task WriteWithBoundaryCheckAsync(
        string requestedPath,
        string content,
        CancellationToken ct = default)
    {
        var mount = FindBestMount(requestedPath);
        if (mount == null)
        {
            throw new MountBoundaryViolationException(
                $"Path '{requestedPath}' is not within any configured mount");
        }

        if (mount.ReadOnly)
        {
            throw new ReadOnlyMountException(
                $"Mount '{mount.ContainerPath}' is read-only. Cannot write to '{requestedPath}'");
        }

        // Build atomic write script
        var script = BuildAtomicWriteScript(requestedPath, mount.ContainerPath);

        // Use stdin for content to avoid argument exposure
        var result = await _executor.ExecWithStdinAsync(
            _containerName,
            script,
            content,
            ct);

        if (result.ExitCode == 99)
        {
            throw new MountBoundaryViolationException(
                $"Path '{requestedPath}' resolved outside mount boundary '{mount.ContainerPath}'");
        }

        if (result.ExitCode != 0)
        {
            throw new IOException($"Failed to write file: {result.Error}");
        }
    }

    /// <summary>
    /// Performs a delete operation with atomic boundary verification.
    /// </summary>
    public async Task DeleteWithBoundaryCheckAsync(
        string requestedPath,
        bool recursive,
        CancellationToken ct = default)
    {
        var mount = FindBestMount(requestedPath);
        if (mount == null)
        {
            throw new MountBoundaryViolationException(
                $"Path '{requestedPath}' is not within any configured mount");
        }

        if (mount.ReadOnly)
        {
            throw new ReadOnlyMountException(
                $"Mount '{mount.ContainerPath}' is read-only. Cannot delete '{requestedPath}'");
        }

        // Prevent deleting the mount point itself
        var normalizedPath = NormalizePath(requestedPath);
        if (normalizedPath == mount.ContainerPath ||
            normalizedPath == mount.ContainerPath.TrimEnd('/'))
        {
            throw new MountBoundaryViolationException(
                $"Cannot delete mount point root: {mount.ContainerPath}");
        }

        var script = BuildAtomicDeleteScript(requestedPath, mount.ContainerPath, recursive);

        var result = await _executor.ExecAsync(_containerName, script, ct);

        if (result.ExitCode == 99)
        {
            throw new MountBoundaryViolationException(
                $"Path '{requestedPath}' resolved outside mount boundary '{mount.ContainerPath}'");
        }

        // Exit code 0 or file not found (already deleted) are both acceptable
        if (result.ExitCode != 0 && !result.Error.Contains("No such file"))
        {
            throw new IOException($"Failed to delete: {result.Error}");
        }
    }

    private MountMapping? FindBestMount(string path)
    {
        var normalized = NormalizePath(path);

        return _mounts
            .Where(m => normalized.StartsWith(m.ContainerPath, StringComparison.Ordinal) ||
                       normalized == m.ContainerPath.TrimEnd('/'))
            .OrderByDescending(m => m.ContainerPath.Length)
            .FirstOrDefault();
    }

    private static string NormalizePath(string path)
    {
        var normalized = path.Replace('\\', '/');
        if (!normalized.StartsWith('/'))
        {
            throw new ArgumentException($"Path must be absolute: {path}");
        }
        return normalized;
    }

    /// <summary>
    /// Builds a shell script that atomically:
    /// 1. Resolves the real path (follows symlinks)
    /// 2. Verifies it starts with the mount point
    /// 3. Reads the file content
    /// All in one command to prevent TOCTOU.
    /// </summary>
    private string BuildAtomicReadScript(string path, string mountPoint)
    {
        var escapedPath = _executor.EscapeArgument(path);
        var escapedMount = _executor.EscapeArgument(mountPoint);

        return $@"
REAL_PATH=$(readlink -f {escapedPath} 2>/dev/null || echo {escapedPath})
case ""$REAL_PATH"" in
  {escapedMount}|{escapedMount}/*)
    cat {escapedPath}
    ;;
  *)
    echo ""Path escapes mount boundary: $REAL_PATH"" >&2
    exit 99
    ;;
esac
".Trim();
    }

    /// <summary>
    /// Builds a shell script for atomic write with boundary verification.
    /// </summary>
    private string BuildAtomicWriteScript(string path, string mountPoint)
    {
        var escapedPath = _executor.EscapeArgument(path);
        var escapedMount = _executor.EscapeArgument(mountPoint);
        var escapedDir = _executor.EscapeArgument(
            Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "/");

        return $@"
# Verify parent directory is within mount
PARENT_REAL=$(readlink -f {escapedDir} 2>/dev/null || mkdir -p {escapedDir} && readlink -f {escapedDir})
case ""$PARENT_REAL"" in
  {escapedMount}|{escapedMount}/*)
    # Create temp file in same directory for atomic rename
    TEMP_FILE=""{escapedPath}.tmp.$$""
    cat > ""$TEMP_FILE"" && mv ""$TEMP_FILE"" {escapedPath}
    ;;
  *)
    echo ""Path escapes mount boundary: $PARENT_REAL"" >&2
    exit 99
    ;;
esac
".Trim();
    }

    /// <summary>
    /// Builds a shell script for atomic delete with boundary verification.
    /// </summary>
    private string BuildAtomicDeleteScript(string path, string mountPoint, bool recursive)
    {
        var escapedPath = _executor.EscapeArgument(path);
        var escapedMount = _executor.EscapeArgument(mountPoint);
        var rmFlags = recursive ? "-rf" : "-f";

        return $@"
REAL_PATH=$(readlink -f {escapedPath} 2>/dev/null || echo {escapedPath})
case ""$REAL_PATH"" in
  {escapedMount})
    echo ""Cannot delete mount point root"" >&2
    exit 99
    ;;
  {escapedMount}/*)
    rm {rmFlags} {escapedPath}
    ;;
  *)
    echo ""Path escapes mount boundary: $REAL_PATH"" >&2
    exit 99
    ;;
esac
".Trim();
    }
}

public class MountBoundaryViolationException : SecurityException
{
    public MountBoundaryViolationException(string message) : base(message) { }
}

public class ReadOnlyMountException : InvalidOperationException
{
    public ReadOnlyMountException(string message) : base(message) { }
}

public sealed class MountMapping
{
    public required string HostPath { get; init; }
    public required string ContainerPath { get; init; }
    public bool ReadOnly { get; init; }
}

// Unit tests
public sealed class MountBoundaryEnforcerTests
{
    private readonly List<MountMapping> _mounts = new()
    {
        new MountMapping { HostPath = "/home/user/project", ContainerPath = "/app", ReadOnly = false },
        new MountMapping { HostPath = "/home/user/data", ContainerPath = "/data", ReadOnly = true }
    };

    private readonly Mock<DockerCommandExecutor> _mockExecutor = new();
    private readonly MountBoundaryEnforcer _sut;

    public MountBoundaryEnforcerTests()
    {
        _mockExecutor.Setup(x => x.EscapeArgument(It.IsAny<string>()))
            .Returns<string>(s => $"'{s}'");

        _sut = new MountBoundaryEnforcer(_mounts, _mockExecutor.Object, "test-container");
    }

    [Fact]
    public async Task ReadWithBoundaryCheck_Should_Succeed_For_Valid_Path()
    {
        // Arrange
        _mockExecutor
            .Setup(x => x.ExecAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DockerExecResult { ExitCode = 0, Output = "file content" });

        // Act
        var result = await _sut.ReadWithBoundaryCheckAsync("/app/src/file.cs");

        // Assert
        result.Should().Be("file content");
    }

    [Fact]
    public async Task ReadWithBoundaryCheck_Should_Throw_For_Boundary_Escape()
    {
        // Arrange
        _mockExecutor
            .Setup(x => x.ExecAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DockerExecResult { ExitCode = 99, Error = "escapes mount" });

        // Act
        var act = () => _sut.ReadWithBoundaryCheckAsync("/app/../etc/passwd");

        // Assert
        await act.Should().ThrowAsync<MountBoundaryViolationException>();
    }

    [Fact]
    public async Task WriteWithBoundaryCheck_Should_Throw_For_ReadOnly_Mount()
    {
        // Act
        var act = () => _sut.WriteWithBoundaryCheckAsync("/data/file.txt", "content");

        // Assert
        await act.Should().ThrowAsync<ReadOnlyMountException>()
            .WithMessage("*read-only*");
    }

    [Fact]
    public async Task DeleteWithBoundaryCheck_Should_Prevent_Mount_Root_Deletion()
    {
        // Act
        var act = () => _sut.DeleteWithBoundaryCheckAsync("/app", recursive: true);

        // Assert
        await act.Should().ThrowAsync<MountBoundaryViolationException>()
            .WithMessage("*mount point root*");
    }

    [Fact]
    public void Constructor_Should_Require_At_Least_One_Mount()
    {
        // Act
        var act = () => new MountBoundaryEnforcer(
            new List<MountMapping>(),
            _mockExecutor.Object,
            "test");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*At least one mount*");
    }
}
```

---

## Use Cases

### Use Case 1: Development Team Working with Containerized Microservices

**Persona:** Priya, Lead Developer at a microservices startup

**Context:** Priya's team runs 12 microservices in Docker containers during development. Each service has its own container with mounted source code. She needs Acode to help refactor common utility code across multiple services without constantly copying files between host and containers.

**Before Docker FS Implementation:**
Priya would have to manually exec into each container to read files, copy changes back to the host, and hope her IDE's file watchers picked up the changes. Debugging cross-service issues meant juggling multiple terminal windows with docker exec sessions. When Acode needed to understand code context, it couldn't see the files as they appeared to the running applications.

**After Docker FS Implementation:**
```bash
$ acode run "Update the error handling pattern in all microservices to use the new ErrorResult type"

[Configuration detected]
  Mode: Docker FS
  Container: user-service
  Mount: /app

[Tool: enumerate_files]
  Path: src/**/*.cs
  Container: user-service
  Results: 45 files

[Tool: read_file]
  Path: src/Controllers/UserController.cs
  Container: user-service
  Content: (retrieved via docker exec cat)

[Tool: write_file]
  Path: src/Controllers/UserController.cs
  Container: user-service
  Method: docker exec (temp + mv for atomicity)
  Result: SUCCESS

[Switching container: order-service]
...

[Summary]
  Services updated: 12
  Files modified: 67
  Pattern applied: ErrorResult<T> return type
  Containers touched: 12
```

**Metrics:**
- Context switching eliminated: 12 container execs → 1 unified session
- Developer time saved: 4 hours/day on cross-service changes
- File sync errors: 0 (direct container modification)
- Cache hit rate: 85% for repeated reads

---

### Use Case 2: CI/CD Integration Testing in Ephemeral Containers

**Persona:** Alex, DevOps Engineer at a fintech company

**Context:** Alex runs integration tests in ephemeral Docker containers that spin up, run tests, and tear down. When tests fail, developers need Acode to investigate the test environment, read log files, and suggest fixes - all before the container is destroyed.

**Before Docker FS Implementation:**
When integration tests failed, Alex would have to quickly docker exec into the container, manually extract log files, and hope to capture the state before the CI system killed the container. Often, critical diagnostic information was lost. Acode couldn't help because it couldn't access the container's file system.

**After Docker FS Implementation:**
```bash
# In CI failure handler
$ acode run "Analyze the test failure in container ci-runner-a8b3f and suggest fixes"

[Connecting to container]
  Container: ci-runner-a8b3f
  Status: Running (will terminate in 60s)
  Mount: /workspace

[Tool: read_file]
  Path: /var/log/test-results.xml
  Container: ci-runner-a8b3f
  Content: (test failure details)

[Tool: enumerate_files]
  Path: /workspace/test-output/**/*.log
  Results: 23 log files

[Analysis]
  Root cause: Database connection string using localhost
  Fix: Update connection string to use service name 'postgres'

[Tool: read_file]
  Path: /workspace/src/appsettings.Test.json
  Content: {"ConnectionStrings": {"Default": "Host=localhost..."}}

[Suggested Fix]
  Change 'localhost' to 'postgres' in appsettings.Test.json

[Tool: write_file]
  Path: /workspace/src/appsettings.Test.json
  Container: ci-runner-a8b3f
  Result: SUCCESS
```

**Metrics:**
- Diagnostic data capture: 100% (vs 30% manual)
- Time to root cause: 2 minutes (vs 30 minutes)
- Ephemeral container support: Full access until termination
- CI feedback loop: Reduced from 45min to 15min

---

### Use Case 3: Security Isolation for Untrusted Code Analysis

**Persona:** Morgan, Security Researcher analyzing open-source dependencies

**Context:** Morgan needs to analyze potentially malicious code in downloaded packages. Running analysis inside a Docker container provides isolation from the host system. Acode needs to read the untrusted code for analysis without exposing the host file system to risk.

**Before Docker FS Implementation:**
Morgan would have to copy suspicious files to the host for Acode to analyze, potentially exposing the system to malicious payloads. Alternatively, analysis had to be done manually inside the container, losing the benefits of AI-assisted review.

**After Docker FS Implementation:**
```bash
$ acode run "Analyze the npm package 'suspicious-logger' for security vulnerabilities"

[Configuration]
  Mode: Docker FS (isolation mode)
  Container: security-sandbox
  Mount: /analysis (read-only)
  Boundary: Strictly enforced

[Tool: enumerate_files]
  Path: /analysis/suspicious-logger/**/*
  Container: security-sandbox
  Mode: Read-only
  Results: 15 files

[Tool: read_file]
  Path: /analysis/suspicious-logger/lib/index.js
  Container: security-sandbox
  Content: (analyzed in isolation)

[Security Analysis]
  ⚠️ CRITICAL: Found process.env access on line 47
  ⚠️ HIGH: Obfuscated eval() call on line 123
  ⚠️ MEDIUM: Network request to unknown domain on line 89

[Host Impact]
  Files accessed: Container only
  Host exposure: None (boundary enforced)
  Sandbox integrity: Maintained
```

**Metrics:**
- Host file system exposure: 0 files
- Isolation guarantee: Container boundary enforced
- Analysis capability: Full read access to suspicious code
- Risk reduction: Complete host isolation during analysis

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| **Docker FS (Docker File System)** | The Docker-mounted file system implementation that provides access to files inside running Docker containers. Unlike LocalFS which accesses the host file system directly, DockerFS executes file operations via `docker exec` commands, ensuring the agent sees files exactly as the container sees them (including overlayfs layers, permissions, and user mappings). This abstraction enables AI-assisted development workflows in containerized environments without requiring manual file copying between host and container. |
| **Bind Mount** | A Docker mounting mechanism where a specific host directory path is mapped into a container at a specified mount point. For example, `-v /home/user/project:/app` makes the host directory `/home/user/project` visible inside the container at `/app`. Changes to files in a bind mount are immediately visible to both the host and container. Bind mounts are preferred for development because they allow editing files on the host while the container sees the changes in real-time. |
| **Volume Mount** | A Docker-managed storage mechanism where Docker creates and manages a named volume independent of the host file system structure. Unlike bind mounts which expose a specific host path, volume mounts are stored in Docker's internal storage location (e.g., `/var/lib/docker/volumes/` on Linux). Volume mounts provide better performance and portability across platforms but are harder to inspect from the host. DockerFS treats volume mounts the same as bind mounts since it accesses files via `docker exec` inside the container. |
| **docker exec** | A Docker command that executes a shell command inside a running container's environment. Syntax: `docker exec [options] <container> <command>`. DockerFS uses `docker exec` to run commands like `cat`, `find`, `stat`, and `mv` inside containers to read, list, and write files. This guarantees file operations see the exact same file content, permissions, and paths as the containerized application, avoiding inconsistencies from direct host file system access. |
| **Container Path** | The absolute file path as it appears inside a Docker container's file system namespace. For example, if a repository is mounted at `/app` inside the container, the container path for a file might be `/app/src/Program.cs`. Container paths always use forward slashes (Linux convention) regardless of the host OS, since containers run Linux environments. DockerFS translates between host paths (used by the application) and container paths (used in docker exec commands). |
| **Host Path** | The absolute file path as it appears on the host machine's file system. For example, `/home/user/projects/myapp/src/Program.cs` on Linux or `C:\Users\User\Projects\MyApp\src\Program.cs` on Windows. Host paths use the host OS's path conventions (backslashes on Windows, forward slashes on Linux). The application layer uses host paths, while DockerFS internally translates them to container paths for docker exec operations. |
| **Mount Point** | The directory path inside a container where a bind mount or volume mount is attached. For example, if the host directory `/home/user/project` is mounted with `-v /home/user/project:/app`, then `/app` is the mount point. Mount points define the boundary between the host-managed files and the container's file system. DockerFS uses mount configuration to translate between host paths and container paths (e.g., host path `/home/user/project/src/file.cs` → container path `/app/src/file.cs` given mount point `/app`). |
| **Latency** | The time delay between initiating a file operation and receiving the result. Docker exec operations have higher latency than direct local file system access due to process spawning overhead, container execution setup, and result marshaling. Typical latency: local FS ~0.1-1ms, docker exec ~10-50ms. DockerFS mitigates latency with aggressive caching (60s TTL for directory listings), reducing perceived latency by 95% for read-heavy workloads like code indexing. |
| **Caching** | The practice of storing the results of expensive operations (like docker exec commands) in memory for reuse on subsequent requests. DockerFS caches directory listings, file existence checks, and file metadata with configurable time-to-live (TTL) values. For example, after listing files in `/app/src`, subsequent requests for the same path return cached results for 60 seconds instead of re-executing `docker exec find`. Caching reduces latency from 50ms to 5ms for cached operations, critical for performance when indexing large codebases. |
| **Cache Invalidation** | The process of removing or marking stale entries in the cache when underlying data changes. DockerFS invalidates cache entries on write operations to ensure subsequent reads reflect the latest state. For example, writing to `/app/src/Program.cs` invalidates: (1) the directory listing cache for `/app/src`, (2) the file existence cache for `/app/src/Program.cs`, (3) any file metadata cache for that file. Without invalidation, reads after writes could return stale data from the cache. |
| **TTL (Time To Live)** | The duration in seconds that a cached entry remains valid before being considered stale and evicted. DockerFS uses different TTLs for different operation types: 60 seconds for directory listings (rarely change during indexing), 30 seconds for file existence checks, 10 seconds for file metadata. TTL balances performance (longer TTL = more cache hits) against freshness (shorter TTL = less staleness risk). Users can configure TTL values in DockerFSOptions based on their workflow (e.g., longer TTL for read-only analysis, shorter TTL for active development). |
| **Container ID** | A unique identifier assigned to each Docker container, either the full 64-character hexadecimal hash (e.g., `a8b3f7c2d9e1...`) or a human-readable name (e.g., `my-app-container`). DockerFS requires a container ID in its configuration to target docker exec commands. Users can specify either the short ID (first 12 chars), full ID, or name. The container must be in "running" state for file operations to succeed - DockerFS verifies container status before operations and provides clear error messages if the container is stopped or doesn't exist. |
| **Docker Daemon** | The background service (`dockerd`) that manages Docker containers, images, networks, and volumes on the host machine. DockerFS communicates with the Docker daemon by executing `docker` CLI commands, which send requests to the daemon via a Unix socket (`/var/run/docker.sock` on Linux) or named pipe (Windows). The Docker daemon must be running and accessible for DockerFS operations to succeed. If the daemon is unreachable, DockerFS fails fast with a clear error message prompting the user to start Docker. |
| **Docker API** | The REST API exposed by the Docker daemon for programmatic container management. While DockerFS COULD use the Docker API directly (via HTTP requests to `/var/run/docker.sock`), it instead uses the `docker` CLI command for simplicity, better error messages, and compatibility with user's Docker configuration (authentication, context, etc.). Direct API usage is out of scope for Task 014b but could be a future optimization for reduced latency. |
| **Shell Escaping** | The process of transforming potentially dangerous file paths and arguments into safe shell command strings that prevent command injection attacks. DockerFS uses single-quote wrapping with embedded single-quote escaping: the path `file's name.txt` becomes `'file'\''s name.txt'` in the shell command. Single quotes disable all special character interpretation (`$`, backticks, semicolons, etc.) except single-quote itself, which is escaped as `'\''` (close quote, escaped quote, open quote). This ensures malicious paths like `file.txt; rm -rf /` are treated as literal filenames, not command sequences. |
| **Exit Code** | The integer return code (0-255) that shell commands produce upon completion to indicate success or failure. By convention, exit code 0 means success, while non-zero codes indicate errors. DockerFS uses exit codes to detect failures: `docker exec cat file.txt` returns 0 if the file was read successfully, 1 if the file doesn't exist, 126 if permission was denied. Exit codes are more reliable than parsing stderr text, which varies across Docker versions and container operating systems. DockerFS maps specific exit codes to typed exceptions (FileNotFoundException for code 1, UnauthorizedAccessException for code 126). |

---

## Out of Scope

The following items are explicitly excluded from Task 014.b:

- **Direct Docker API Usage**: DockerFS uses the `docker` CLI command rather than making direct HTTP requests to the Docker API socket (`/var/run/docker.sock`). While direct API usage could reduce latency by 5-10ms per operation, it adds complexity (authentication, API versioning, request/response marshaling) and bypasses user's Docker configuration (context, registry auth). Future optimization could add optional direct API mode, but Task 014b uses CLI exclusively.

- **Container Lifecycle Management**: DockerFS does NOT start, stop, restart, pause, or create containers. It assumes the target container is already running and configured correctly. If the container is not running, DockerFS fails with a clear error message prompting the user to start the container manually (`docker start <container>`). Container orchestration is the user's responsibility, not the file system abstraction's.

- **Docker Image Operations**: Building images (`docker build`), pulling images from registries (`docker pull`), pushing images (`docker push`), or managing image layers is out of scope. DockerFS operates on running containers only. Image management is a separate concern handled by Docker tooling or CI/CD pipelines, not the file system abstraction.

- **Container Networking Operations**: Inspecting or modifying container networks, port mappings, DNS configuration, or inter-container communication is out of scope. DockerFS is solely concerned with file operations (read, write, list, delete). Network-related tasks belong to container orchestration tools, not the file system layer.

- **Multi-Container Orchestration (Docker Compose)**: DockerFS operates on a single container at a time, specified in the session configuration. It does NOT automatically detect related containers from `docker-compose.yml`, coordinate operations across multiple containers, or manage container dependencies. Users working with multi-container applications must configure separate sessions for each container or choose a primary container for file operations.

- **Kubernetes Support**: DockerFS targets local Docker containers only and does NOT support Kubernetes pods, deployments, or services. Kubernetes uses a different container runtime interface (CRI) and requires different tooling (`kubectl exec` vs `docker exec`). Supporting Kubernetes would require a separate RepoFS implementation (KubernetesFS) which is out of scope for Task 014b.

- **Remote Docker Daemon**: DockerFS communicates with the Docker daemon on the local machine only (`/var/run/docker.sock` or `npipe:////./pipe/docker_engine`). It does NOT support connecting to remote Docker daemons via TCP (e.g., `tcp://remote-host:2375`), SSH (e.g., `ssh://user@remote-host`), or Docker contexts pointing to remote machines. Remote Docker support would require handling network latency, authentication, and security implications that are out of scope for the MVP.

- **Docker-in-Docker (DinD)**: Running Docker commands inside a Docker container that itself manages containers (Docker-in-Docker pattern) is not supported. This creates complex nesting scenarios (agent → host Docker → DinD container → nested container) with ambiguous mount paths and security implications. Users requiring DinD must run the agent on the host, not inside a container.

- **Windows Container Support**: DockerFS assumes Linux containers with standard Unix commands (`cat`, `find`, `stat`, `rm`, `mv`). Windows containers use different command syntax (PowerShell-based) and file system semantics (backslashes, case-insensitive paths, different permission models). Supporting Windows containers would require a separate command executor and path translation logic, which is out of scope for Task 014b.

- **Volume Driver Abstraction**: DockerFS treats all volume types (local, NFS, cloud storage drivers) uniformly via `docker exec`. It does NOT interact directly with volume drivers or optimize operations based on volume type. This simplifies implementation but means DockerFS cannot leverage volume-specific optimizations (e.g., direct NFS access when agent and container share the same NFS mount).

- **Container Resource Monitoring**: Tracking container CPU usage, memory consumption, disk I/O, or file system quotas is out of scope. DockerFS is concerned only with file operations, not resource telemetry. Users needing resource monitoring should use `docker stats` or container orchestration platforms.

- **Advanced File System Features**: Extended file attributes (xattrs), Access Control Lists (ACLs), file system watches (inotify), hard links, symbolic links outside the mount boundary, and file locks are not supported or are supported with degraded functionality. DockerFS provides basic POSIX file operations (read, write, list, delete, move) sufficient for source code management but does not expose advanced file system capabilities.

- **Transactional File Operations**: Multi-file atomic transactions (e.g., "write these 5 files atomically or rollback all") are not supported. Each file operation is independent. While individual file writes use atomic temp-then-move patterns, there is no cross-file transaction boundary. Applications requiring multi-file consistency must implement their own transaction logic (e.g., write marker files to signal completion).

- **File System Permissions Management**: Changing file ownership (`chown`), permissions (`chmod`), or user/group mappings between host and container is out of scope. DockerFS observes existing permissions and reports permission errors clearly, but does not modify permissions. Users must configure container user mappings (`--user` flag, `USER` directive in Dockerfile) to ensure the container user has appropriate permissions for file operations.

- **Container Snapshot/Checkpoint**: Creating container snapshots, checkpoints (CRIU), or exporting container file systems to tar archives is out of scope. DockerFS provides live file access to running containers only. Backup and disaster recovery are separate concerns handled by Docker tooling or infrastructure-level solutions.

---

## Functional Requirements

### Docker Detection (FR-014b-01 to FR-014b-04)

| ID | Requirement |
|----|-------------|
| FR-014b-01 | System MUST detect if Docker daemon is available and accessible |
| FR-014b-02 | System MUST verify specified container exists |
| FR-014b-03 | System MUST verify container is in running state |
| FR-014b-04 | System MUST verify configured mount paths are accessible in container |

### File Reading (FR-014b-05 to FR-014b-09)

| ID | Requirement |
|----|-------------|
| FR-014b-05 | ReadFileAsync MUST execute via `docker exec cat` command |
| FR-014b-06 | Text file reading MUST use `cat` command with proper encoding |
| FR-014b-07 | Binary file reading MUST use base64 encoding for transport |
| FR-014b-08 | ReadFileAsync MUST throw FileNotFoundException for missing files |
| FR-014b-09 | ReadFileAsync MUST throw AccessDeniedException for permission failures |

### File Writing (FR-014b-10 to FR-014b-14)

| ID | Requirement |
|----|-------------|
| FR-014b-10 | WriteFileAsync MUST execute via `docker exec` commands |
| FR-014b-11 | Writes MUST use temp-file-then-rename pattern for atomicity |
| FR-014b-12 | WriteFileAsync MUST create parent directories via `mkdir -p` |
| FR-014b-13 | Binary file writing MUST use base64 encoding for transport |
| FR-014b-14 | Write failures MUST be reported with clear error messages |

### File Deletion (FR-014b-15 to FR-014b-18)

| ID | Requirement |
|----|-------------|
| FR-014b-15 | DeleteFileAsync MUST execute via `docker exec rm` command |
| FR-014b-16 | DeleteDirectoryAsync MUST execute via `docker exec rm -rf` command |
| FR-014b-17 | DeleteFileAsync MUST NOT throw error for non-existent files |
| FR-014b-18 | Delete operations MUST handle permission errors gracefully |

### Directory Enumeration (FR-014b-19 to FR-014b-23)

| ID | Requirement |
|----|-------------|
| FR-014b-19 | EnumerateFilesAsync MUST use `find` command for listing |
| FR-014b-20 | System MUST parse `find` command output correctly |
| FR-014b-21 | Enumeration MUST handle large directories efficiently |
| FR-014b-22 | EnumerateFilesAsync MUST support glob pattern filtering |
| FR-014b-23 | EnumerateFilesAsync MUST support recursive option |

### Metadata (FR-014b-24 to FR-014b-28)

| ID | Requirement |
|----|-------------|
| FR-014b-24 | ExistsAsync MUST use `test` command for checking |
| FR-014b-25 | GetMetadataAsync MUST use `stat` command for details |
| FR-014b-26 | Metadata MUST include file size parsed from stat output |
| FR-014b-27 | Metadata MUST include modified timestamp parsed from stat output |
| FR-014b-28 | Metadata MUST correctly identify file vs directory type |

### Path Translation (FR-014b-29 to FR-014b-32)

| ID | Requirement |
|----|-------------|
| FR-014b-29 | System MUST translate host paths to container paths |
| FR-014b-30 | System MUST translate container paths to host paths |
| FR-014b-31 | Mount mappings MUST be configurable via configuration |
| FR-014b-32 | System MUST support multiple mount point mappings |

### Caching (FR-014b-33 to FR-014b-37)

| ID | Requirement |
|----|-------------|
| FR-014b-33 | Directory listings MUST be cached to reduce Docker exec calls |
| FR-014b-34 | File existence checks MUST be cached |
| FR-014b-35 | Cache MUST be invalidated on write or delete operations |
| FR-014b-36 | Cache MUST support TTL-based expiration |
| FR-014b-37 | Cache MUST support manual disable option |

### Error Handling (FR-014b-38 to FR-014b-42)

| ID | Requirement |
|----|-------------|
| FR-014b-38 | ContainerNotFoundException MUST be thrown when container not found |
| FR-014b-39 | ContainerNotRunningException MUST be thrown when container stopped |
| FR-014b-40 | MountNotFoundException MUST be thrown for invalid mount paths |
| FR-014b-41 | AccessDeniedException MUST be thrown for permission failures |
| FR-014b-42 | TimeoutException MUST be thrown when command exceeds timeout |

### Security (FR-014b-43 to FR-014b-45)

| ID | Requirement |
|----|-------------|
| FR-014b-43 | All command arguments MUST be properly shell-escaped |
| FR-014b-44 | Path traversal within container MUST be prevented |
| FR-014b-45 | Operations MUST be restricted to configured mount boundaries |

### Container Health and Configuration (FR-014b-46 to FR-014b-50)

| ID | Requirement |
|----|-------------|
| FR-014b-46 | System MUST verify Docker daemon is running and accessible before operations |
| FR-014b-47 | System MUST validate container exists and is in "running" state before file operations |
| FR-014b-48 | System MUST verify mount paths are accessible within container before operations |
| FR-014b-49 | System MUST support configurable exec command timeout (default: 30 seconds, max: 300 seconds) |
| FR-014b-50 | System MUST map container user permissions to host user permissions for permission error diagnostics |

---

## Non-Functional Requirements

### Performance (NFR-014b-01 to NFR-014b-04)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014b-01 | Performance | File read MUST complete in < 100ms plus Docker latency |
| NFR-014b-02 | Performance | File write MUST complete in < 150ms plus Docker latency |
| NFR-014b-03 | Performance | Cache hit operations MUST complete in < 5ms |
| NFR-014b-04 | Performance | Directory listing of 1000 files MUST complete in < 200ms |

### Reliability (NFR-014b-05 to NFR-014b-07)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014b-05 | Reliability | System MUST handle container restarts gracefully |
| NFR-014b-06 | Reliability | Transient Docker errors MUST trigger automatic retry |
| NFR-014b-07 | Reliability | Operations MUST timeout to prevent indefinite blocking |

### Security (NFR-014b-08 to NFR-014b-10)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014b-08 | Security | Command building MUST use safe escaping patterns |
| NFR-014b-09 | Security | Shell injection attacks MUST be prevented |
| NFR-014b-10 | Security | Mount boundary MUST be enforced on all operations |

### Observability (NFR-014b-11 to NFR-014b-13)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014b-11 | Observability | All Docker exec commands MUST be logged |
| NFR-014b-12 | Observability | Cache hit/miss ratios MUST be trackable |
| NFR-014b-13 | Observability | Command latency MUST be measurable for diagnostics |

### Maintainability (NFR-014b-14 to NFR-014b-16)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014b-14 | Maintainability | Code MUST follow Clean Architecture layer boundaries (no direct Infrastructure dependencies in Domain) |
| NFR-014b-15 | Maintainability | All public methods and classes MUST have XML documentation comments |
| NFR-014b-16 | Maintainability | Docker command construction MUST be centralized in DockerCommandBuilder class for testability |

### Compatibility (NFR-014b-17 to NFR-014b-20)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014b-17 | Compatibility | System MUST support Docker Engine 20.10+ and Docker Desktop 4.0+ |
| NFR-014b-18 | Compatibility | System MUST work with Linux containers on Windows (WSL2 backend) |
| NFR-014b-19 | Compatibility | System MUST work with Linux containers on macOS (Docker Desktop VM) |
| NFR-014b-20 | Compatibility | System MUST work with native Linux Docker daemon |

---

## User Manual Documentation

### Overview

Docker File System (DockerFS) enables Acode to work seamlessly with files inside running Docker containers. This is essential for containerized development workflows where your application runs in a container during development. Without DockerFS, you would need to manually copy files between your host and container using `docker cp` after every change, breaking the AI-assisted development flow.

**When to Use DockerFS:**
- Your project runs in a Docker container during development
- You use Docker Compose for local development environments
- You need Acode to see files exactly as the containerized application sees them (with correct permissions, overlayfs layers)
- You want to avoid manual file synchronization between host and container

**Key Benefits:**
- **Seamless Workflow:** Acode modifies files directly inside containers, no manual copying required
- **Guaranteed Parity:** Files are accessed via `docker exec`, ensuring exact match with container's view
- **Performance Optimized:** Aggressive caching reduces Docker exec overhead by 95%
- **Security Enforced:** Mount boundary enforcement prevents operations outside configured paths

### Prerequisites

Before using DockerFS, ensure:

1. **Docker is installed and running:**
   ```bash
   docker --version
   # Should show Docker version 20.10 or higher

   docker ps
   # Should list running containers without error
   ```

2. **Your container is running:**
   ```bash
   docker ps --filter "name=my-app-container"
   # Should show your target container in "Up" status
   ```

3. **Your project is mounted in the container:**
   ```bash
   docker inspect my-app-container --format '{{json .Mounts}}'
   # Should show bind mount from your host project directory to container path
   ```

4. **Required commands exist in container:**
   ```bash
   docker exec my-app-container which cat find stat rm mkdir mv
   # Should show paths for all commands (e.g., /usr/bin/cat)
   ```

### Step-by-Step Setup Guide

#### Step 1: Identify Your Container

First, identify the container where your project is mounted:

```bash
# List all running containers
docker ps

# Example output:
# CONTAINER ID   IMAGE       COMMAND       NAMES
# a8b3f7c2d9e1   node:18     "npm start"   my-app-container
```

Note the container name (e.g., `my-app-container`) or ID (e.g., `a8b3f7c2d9e1`).

#### Step 2: Verify Mount Configuration

Check where your project is mounted inside the container:

```bash
docker inspect my-app-container --format '{{range .Mounts}}{{.Source}} -> {{.Destination}}{{println}}{{end}}'

# Example output:
# /home/user/projects/myapp -> /app
```

This shows your host directory `/home/user/projects/myapp` is mounted at `/app` inside the container.

#### Step 3: Configure Acode

Create or update `.agent/config.yml` in your project root:

```yaml
# .agent/config.yml
repo:
  # Specify Docker as the file system provider
  fs_type: docker

  docker:
    # Container name or ID (from Step 1)
    container: my-app-container

    # Mount mappings: host path -> container path (from Step 2)
    mounts:
      - host: /home/user/projects/myapp
        container: /app
        read_only: false  # Set true to prevent writes

    # Optional: Multiple mounts for complex setups
    # mounts:
    #   - host: /home/user/projects/myapp/src
    #     container: /app/src
    #   - host: /home/user/projects/myapp/data
    #     container: /data
    #     read_only: true  # Read-only mount

    # Cache configuration (recommended for performance)
    cache:
      enabled: true           # Enable result caching
      ttl_seconds: 60         # Cache directory listings for 60s
      file_existence_ttl: 30  # Cache file existence checks for 30s
      metadata_ttl: 10        # Cache file metadata for 10s

    # Command timeout (prevents hanging on container issues)
    timeout_seconds: 30       # Default timeout for docker exec commands
    max_timeout_seconds: 300  # Maximum allowed timeout for large files

    # Retry configuration (handles transient failures)
    retry:
      enabled: true
      max_attempts: 3
      backoff_seconds: 1
```

#### Step 4: Verify Configuration

Test that Acode can connect to your container:

```bash
# Initialize Acode session (reads config.yml)
acode init

# Expected output:
# [DockerFS] Detected Docker daemon: Docker version 24.0.5
# [DockerFS] Container 'my-app-container' found (status: running)
# [DockerFS] Verified mount: /app (accessible)
# [DockerFS] Configuration valid
```

#### Step 5: Run First Operation

Test reading a file from the container:

```bash
acode run "Read the contents of src/index.js and explain its purpose"

# Expected tool sequence:
# [Tool: read_file]
#   Path: src/index.js
#   Container: my-app-container
#   Method: docker exec my-app-container cat '/app/src/index.js'
#   Result: (file contents displayed)
```

### Configuration Reference

#### Basic Configuration (Minimal)

```yaml
repo:
  fs_type: docker
  docker:
    container: my-app-container
    mounts:
      - host: /home/user/project
        container: /app
```

This minimal configuration uses all default values (caching enabled, 30s timeout, 3 retries).

#### Advanced Configuration (Full Options)

```yaml
repo:
  fs_type: docker

  docker:
    # Container Identification
    container: my-app-container  # Can also use container ID: a8b3f7c2d9e1

    # Mount Mappings (supports multiple mounts)
    mounts:
      - host: /home/user/project/src
        container: /app/src
        read_only: false

      - host: /home/user/project/data
        container: /data
        read_only: true  # Prevent writes to this mount

    # Performance Tuning
    cache:
      enabled: true              # Master switch for all caching
      ttl_seconds: 60            # Directory listing cache duration
      file_existence_ttl: 30     # File existence check cache duration
      metadata_ttl: 10           # File metadata (size, mtime) cache duration
      max_entries: 10000         # Maximum cache entries before eviction

    # Timeout Configuration
    timeout_seconds: 30          # Default timeout for most operations
    max_timeout_seconds: 300     # Maximum timeout for large file operations
    read_timeout_multiplier: 1.5 # Extra time allowance for read operations

    # Reliability Settings
    retry:
      enabled: true
      max_attempts: 3            # Retry up to 3 times on transient failures
      backoff_seconds: 1         # Wait 1s, 2s, 4s between retries (exponential)

    # Security Settings
    enforce_mount_boundaries: true  # Prevent access outside configured mounts
    allow_symlinks: false           # Reject symlinks outside mount boundaries
    max_file_size: 104857600        # Reject files > 100MB (prevents DoS)

    # Logging/Observability
    log_commands: true           # Log all docker exec commands
    track_cache_metrics: true    # Track cache hit/miss ratios
    measure_latency: true        # Measure and log command latency
```

### Usage Examples

#### Example 1: Reading Files

```csharp
// Initialize Docker file system
var options = new DockerFSOptions
{
    ContainerName = "my-app-container",
    Mounts = new[]
    {
        new MountMapping("/home/user/project", "/app")
    }
};

var fs = new DockerFileSystem(options);

// Read a text file (uses docker exec cat)
var content = await fs.ReadFileAsync("src/Program.cs");
Console.WriteLine(content);  // C# source code

// Read binary file (uses base64 encoding for transport)
var imageBytes = await fs.ReadBinaryFileAsync("assets/logo.png");
File.WriteAllBytes("local-copy.png", imageBytes);
```

#### Example 2: Writing Files

```csharp
// Write text file (atomic temp-then-rename pattern)
await fs.WriteFileAsync("config.json", jsonContent);

// Write binary file (base64 encoded for transport)
var logoBytes = File.ReadAllBytes("new-logo.png");
await fs.WriteBinaryFileAsync("assets/logo.png", logoBytes);

// Parent directories are created automatically via mkdir -p
await fs.WriteFileAsync("deeply/nested/path/file.txt", "content");
```

#### Example 3: Directory Operations

```csharp
// List all C# files recursively (uses docker exec find)
var files = await fs.EnumerateFilesAsync("src", pattern: "*.cs", recursive: true);
foreach (var file in files)
{
    Console.WriteLine(file);  // src/Controllers/UserController.cs
}

// Check if file exists (cached for 30 seconds by default)
var exists = await fs.ExistsAsync("src/Program.cs");

// Get file metadata (size, modified time, type)
var metadata = await fs.GetMetadataAsync("src/Program.cs");
Console.WriteLine($"Size: {metadata.Size} bytes, Modified: {metadata.LastModified}");
```

#### Example 4: Error Handling

```csharp
try
{
    var content = await fs.ReadFileAsync("missing-file.txt");
}
catch (FileNotFoundException ex)
{
    // File doesn't exist in container
    Console.WriteLine($"File not found: {ex.Message}");
}
catch (UnauthorizedAccessException ex)
{
    // Permission denied in container
    Console.WriteLine($"Permission denied: {ex.Message}");
    Console.WriteLine($"Container user: {ex.Data["ContainerUser"]}");
}
catch (ContainerNotRunningException ex)
{
    // Container stopped mid-operation
    Console.WriteLine($"Container not running: {ex.Message}");
    Console.WriteLine("Restart container with: docker start my-app-container");
}
catch (TimeoutException ex)
{
    // Operation took longer than configured timeout
    Console.WriteLine($"Operation timed out after {ex.Data["TimeoutSeconds"]}s");
    Console.WriteLine("Consider increasing timeout_seconds in config.yml");
}
```

### Path Translation Deep Dive

DockerFS translates between host paths (used by your application) and container paths (used in docker exec commands).

**Example Scenario:**
- Host project: `/home/user/projects/myapp`
- Container mount: `/app`
- Mount configuration: `host: /home/user/projects/myapp, container: /app`

**Path Translation Examples:**

| Application Path (Host) | Container Path | docker exec Command |
|-------------------------|----------------|---------------------|
| `src/Program.cs` | `/app/src/Program.cs` | `docker exec container cat '/app/src/Program.cs'` |
| `/home/user/projects/myapp/src/Program.cs` | `/app/src/Program.cs` | Same as above |
| `../other-project/file.txt` | ❌ Outside mount | Error: MountBoundaryViolationException |

**Key Rules:**
1. Relative paths are resolved relative to the container mount point (`/app`)
2. Absolute host paths are translated to container paths using mount mappings
3. Paths outside the mount boundary are rejected (security enforcement)
4. All container paths use forward slashes (Linux convention), even on Windows host

### Performance Optimization with Caching

DockerFS caches results of expensive docker exec operations to minimize latency.

**Cache Behavior:**

```
┌─────────────────────────────────────────────────────────┐
│                  First Request (Cache Miss)              │
│                                                           │
│  EnumerateFilesAsync("src") → docker exec find          │
│  ├─ Latency: 50ms (process spawn + find execution)      │
│  └─ Result cached with 60s TTL                          │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│            Subsequent Request (Cache Hit)                │
│                                                           │
│  EnumerateFilesAsync("src") → return from cache         │
│  ├─ Latency: 0.5ms (memory lookup)                      │
│  └─ 100x faster than cache miss                         │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│           Write Operation (Cache Invalidation)           │
│                                                           │
│  WriteFileAsync("src/new.cs", content)                  │
│  ├─ Execute: docker exec ... (write file)               │
│  └─ Invalidate cache for:                               │
│      - Directory listing: src/                           │
│      - File existence: src/new.cs                        │
│      - File metadata: src/new.cs                         │
└─────────────────────────────────────────────────────────┘
```

**Cache Configuration Trade-offs:**

| TTL Setting | Performance | Staleness Risk | Best For |
|-------------|-------------|----------------|----------|
| 5 seconds | Good (80% hit rate) | Very Low | Active development (frequent file changes) |
| 30 seconds | Better (90% hit rate) | Low | Balanced development workflow |
| 60 seconds | Best (95% hit rate) | Medium | Read-heavy operations (code indexing, analysis) |
| 300 seconds | Excellent (98% hit rate) | High | Read-only containers, CI/CD analysis |

**Recommendation:** Use 60s TTL (default) for most scenarios. Reduce to 10-30s if you notice stale file listings during active development.

### Troubleshooting Guide

#### Issue 1: "Container Not Found" Error

**Symptoms:**
```
ERROR: ContainerNotFoundException: Container 'my-app-container' not found
  Available containers: postgres-db, redis-cache
```

**Root Causes:**
- Container name in config.yml doesn't match actual container name
- Container was renamed or recreated
- Docker daemon not running

**Solutions:**

1. **List running containers and verify name:**
   ```bash
   docker ps --format "table {{.Names}}\t{{.Status}}"

   # Example output:
   # NAMES               STATUS
   # my-app              Up 2 hours
   # postgres-db         Up 2 hours
   ```
   Update config.yml with the exact name from the "NAMES" column.

2. **Check if container was stopped:**
   ```bash
   docker ps -a --filter "name=my-app-container"

   # If status shows "Exited", restart it:
   docker start my-app-container
   ```

3. **Use container ID instead of name:**
   ```yaml
   docker:
     container: a8b3f7c2d9e1  # Use ID from 'docker ps'
   ```

#### Issue 2: "Mount Path Not Accessible" Error

**Symptoms:**
```
ERROR: MountNotFoundException: Mount path '/app' not accessible in container 'my-app-container'
  Container file system: / (missing /app directory)
```

**Root Causes:**
- Mount path in config.yml doesn't match container's actual mount point
- Container started without the expected bind mount
- Mount path is a typo

**Solutions:**

1. **Inspect container mounts:**
   ```bash
   docker inspect my-app-container --format '{{json .Mounts}}' | jq '.'

   # Example output:
   # [
   #   {
   #     "Source": "/home/user/project",
   #     "Destination": "/workspace",  ← Actual mount is /workspace, not /app!
   #     "Mode": "rw"
   #   }
   # ]
   ```
   Update config.yml `container: /workspace` to match "Destination".

2. **Verify path exists in container:**
   ```bash
   docker exec my-app-container ls -la /app

   # If error: "ls: /app: No such file or directory"
   # Then /app doesn't exist - check mount configuration
   ```

3. **Recreate container with correct mount:**
   ```bash
   docker stop my-app-container
   docker rm my-app-container
   docker run -d --name my-app-container \
     -v /home/user/project:/app \
     my-image:latest
   ```

#### Issue 3: "Permission Denied" in Container

**Symptoms:**
```
ERROR: UnauthorizedAccessException: Permission denied reading '/app/secrets.txt'
  Container user: node (uid=1000)
  File owner: root (uid=0)
  File permissions: -rw------- (600)
```

**Root Causes:**
- File owned by different user than container's default user
- Container runs as non-root user without read permissions
- Write operation on read-only mount

**Solutions:**

1. **Check file permissions in container:**
   ```bash
   docker exec my-app-container ls -la /app/secrets.txt

   # Example output:
   # -rw------- 1 root root 1234 Jan 5 10:00 /app/secrets.txt
   #  ^          ^    ^
   #  |          |    |
   #  |          |    └─ Group: root
   #  |          └────── Owner: root
   #  └───────────────── Permissions: owner-only read/write
   ```

2. **Change file ownership to match container user:**
   ```bash
   # Find container's default user
   docker exec my-app-container whoami  # Example: node

   # Change ownership on host (file is bind-mounted)
   sudo chown $USER:$USER /home/user/project/secrets.txt
   chmod 644 /home/user/project/secrets.txt
   ```

3. **Run container as root (for testing only, not production):**
   ```bash
   docker stop my-app-container
   docker rm my-app-container
   docker run -d --name my-app-container \
     --user root \
     -v /home/user/project:/app \
     my-image:latest
   ```

4. **For read-only mount errors, check mount configuration:**
   ```yaml
   mounts:
     - host: /home/user/project
       container: /app
       read_only: false  ← Ensure this is false for writes
   ```

#### Issue 4: "Operation Timeout" Error

**Symptoms:**
```
ERROR: TimeoutException: docker exec command exceeded 30 second timeout
  Command: docker exec my-app-container cat '/app/large-file.bin'
  File size: 150 MB
  Timeout configured: 30 seconds
```

**Root Causes:**
- Large file operation exceeds default 30s timeout
- Container is under heavy load (CPU/IO throttling)
- Docker daemon is unresponsive

**Solutions:**

1. **Increase timeout for large files:**
   ```yaml
   docker:
     timeout_seconds: 120      # Increase to 2 minutes
     max_timeout_seconds: 600  # Allow up to 10 minutes max
   ```

2. **Check container resource usage:**
   ```bash
   docker stats my-app-container --no-stream

   # Example output showing high CPU:
   # CONTAINER           CPU %   MEM USAGE / LIMIT
   # my-app-container    98%     512MB / 2GB
   ```
   If CPU/memory is maxed out, increase container limits or reduce load.

3. **Check Docker daemon health:**
   ```bash
   docker info  # Should complete in < 1 second

   # If slow, restart Docker daemon:
   sudo systemctl restart docker  # Linux
   # or restart Docker Desktop on Windows/macOS
   ```

#### Issue 5: Cache Showing Stale Data

**Symptoms:**
- Modified files in container don't reflect changes in Acode
- Directory listings missing recently created files
- File existence checks return false for files that exist

**Root Causes:**
- Files modified outside Acode (manual container edits, application writes)
- TTL too long for active development workflow
- Cache not invalidated properly on writes

**Solutions:**

1. **Manual cache clear:**
   ```csharp
   // Clear entire cache
   await fs.ClearCacheAsync();

   // Or disable cache temporarily
   var options = new DockerFSOptions
   {
       // ... other settings
       CacheEnabled = false
   };
   ```

2. **Reduce cache TTL for active development:**
   ```yaml
   docker:
     cache:
       ttl_seconds: 10          # Reduce from 60s to 10s
       file_existence_ttl: 5    # Reduce to 5s
   ```

3. **Disable cache entirely (testing only):**
   ```yaml
   docker:
     cache:
       enabled: false  # No caching, every operation hits docker exec
   ```

### Best Practices

1. **Use Container Names, Not IDs:** Container IDs change on recreation. Use stable names: `container: my-app-container`

2. **Configure Timeouts Based on File Sizes:** For projects with large binary assets (videos, ML models), increase `timeout_seconds` to avoid false timeouts.

3. **Enable Caching for Read-Heavy Workflows:** When indexing large codebases, caching reduces latency by 95%. Use default 60s TTL.

4. **Use Read-Only Mounts for Data Directories:** If Acode should never modify certain directories (e.g., `/data`), mark them `read_only: true`.

5. **Monitor Cache Hit Rates:** Enable `track_cache_metrics: true` and review logs to optimize TTL settings for your workflow.

6. **Test Configuration with `acode init`:** Always run `acode init` after changing config.yml to validate configuration before starting work.

7. **Keep Container Running:** DockerFS requires containers in "running" state. Use `restart: unless-stopped` in docker-compose.yml for development.

8. **Match Container User with Host User:** To avoid permission issues, run containers with `--user $(id -u):$(id -g)` matching your host user ID.

---

## Acceptance Criteria

### Docker Environment Detection (AC-001 to AC-008)

- [ ] AC-001: DockerFileSystem MUST detect Docker daemon availability at startup
- [ ] AC-002: DockerFileSystem MUST verify specified container exists before operations
- [ ] AC-003: DockerFileSystem MUST verify container is in "running" state
- [ ] AC-004: DockerFileSystem MUST verify configured mount paths are accessible in container
- [ ] AC-005: System MUST throw ContainerNotFoundException when container does not exist
- [ ] AC-006: System MUST throw ContainerNotRunningException when container is stopped
- [ ] AC-007: System MUST provide helpful error message suggesting `docker start` for stopped containers
- [ ] AC-008: System MUST validate container name/ID format before attempting connection

### File Reading Operations (AC-009 to AC-020)

- [ ] AC-009: ReadFileAsync MUST read text files via `docker exec cat` command
- [ ] AC-010: ReadFileAsync MUST handle UTF-8 encoded files correctly
- [ ] AC-011: ReadFileAsync MUST handle UTF-16 with BOM detection
- [ ] AC-012: ReadFileAsync MUST support binary file reading via base64 encoding
- [ ] AC-013: ReadFileAsync MUST throw FileNotFoundException for missing files
- [ ] AC-014: ReadFileAsync MUST throw AccessDeniedException for permission failures
- [ ] AC-015: ReadFileAsync MUST support cancellation via CancellationToken
- [ ] AC-016: ReadFileAsync MUST timeout after configurable duration (default 30s)
- [ ] AC-017: ReadFileAsync MUST handle files larger than 10MB with streaming
- [ ] AC-018: ReadFileAsync MUST cache file content when caching is enabled
- [ ] AC-019: ReadFileAsync MUST return cached content on cache hit within TTL
- [ ] AC-020: ReadFileAsync MUST translate paths from relative to container absolute paths

### File Writing Operations (AC-021 to AC-033)

- [ ] AC-021: WriteFileAsync MUST write files via `docker exec` commands
- [ ] AC-022: WriteFileAsync MUST use temp-file-then-rename pattern for atomicity
- [ ] AC-023: WriteFileAsync MUST create parent directories via `mkdir -p` if missing
- [ ] AC-024: WriteFileAsync MUST support binary content via base64 encoding
- [ ] AC-025: WriteFileAsync MUST overwrite existing files when they exist
- [ ] AC-026: WriteFileAsync MUST preserve file permissions when overwriting
- [ ] AC-027: WriteFileAsync MUST throw AccessDeniedException for permission failures
- [ ] AC-028: WriteFileAsync MUST invalidate cache for written path
- [ ] AC-029: WriteFileAsync MUST invalidate cache for parent directory listing
- [ ] AC-030: WriteFileAsync MUST support cancellation via CancellationToken
- [ ] AC-031: WriteFileAsync MUST timeout after configurable duration (default 60s)
- [ ] AC-032: WriteFileAsync MUST NOT expose file content in command line arguments
- [ ] AC-033: WriteFileAsync MUST use stdin piping for content transfer

### File Deletion Operations (AC-034 to AC-041)

- [ ] AC-034: DeleteFileAsync MUST delete files via `docker exec rm` command
- [ ] AC-035: DeleteDirectoryAsync MUST delete directories via `docker exec rm -rf` command
- [ ] AC-036: DeleteFileAsync MUST NOT throw error for non-existent files (idempotent)
- [ ] AC-037: DeleteFileAsync MUST throw AccessDeniedException for permission failures
- [ ] AC-038: DeleteFileAsync MUST invalidate cache for deleted path
- [ ] AC-039: DeleteFileAsync MUST invalidate cache for parent directory listing
- [ ] AC-040: Delete operations MUST support cancellation via CancellationToken
- [ ] AC-041: DeleteFileAsync MUST prevent deletion of mount point root

### Directory Enumeration Operations (AC-042 to AC-051)

- [ ] AC-042: EnumerateFilesAsync MUST list files using `find` command
- [ ] AC-043: EnumerateFilesAsync MUST parse `find` command output correctly
- [ ] AC-044: EnumerateFilesAsync MUST handle directories with 10,000+ files
- [ ] AC-045: EnumerateFilesAsync MUST support glob pattern filtering (e.g., `*.cs`)
- [ ] AC-046: EnumerateFilesAsync MUST support recursive option (default: true)
- [ ] AC-047: EnumerateFilesAsync MUST support max depth limiting
- [ ] AC-048: EnumerateFilesAsync MUST cache directory listings when caching enabled
- [ ] AC-049: EnumerateFilesAsync MUST return FileMetadata with size and modified time
- [ ] AC-050: EnumerateFilesAsync MUST handle permission denied for individual files gracefully
- [ ] AC-051: EnumerateFilesAsync MUST support cancellation via CancellationToken

### File Metadata Operations (AC-052 to AC-058)

- [ ] AC-052: ExistsAsync MUST check file existence using `test -e` command
- [ ] AC-053: GetMetadataAsync MUST retrieve file details using `stat` command
- [ ] AC-054: GetMetadataAsync MUST parse file size from stat output correctly
- [ ] AC-055: GetMetadataAsync MUST parse modified timestamp from stat output correctly
- [ ] AC-056: GetMetadataAsync MUST distinguish between file and directory types
- [ ] AC-057: ExistsAsync MUST cache existence checks when caching enabled
- [ ] AC-058: GetMetadataAsync MUST support cancellation via CancellationToken

### Path Translation (AC-059 to AC-065)

- [ ] AC-059: System MUST translate relative paths to container absolute paths
- [ ] AC-060: System MUST translate host paths to container paths via mount mapping
- [ ] AC-061: System MUST translate container paths to host paths via mount mapping
- [ ] AC-062: Mount mappings MUST be configurable via configuration file
- [ ] AC-063: System MUST support multiple simultaneous mount point mappings
- [ ] AC-064: System MUST select most specific (longest matching) mount for path translation
- [ ] AC-065: System MUST throw MountNotFoundException for paths outside all mounts

### Caching Behavior (AC-066 to AC-075)

- [ ] AC-066: Cache MUST store directory listings to reduce Docker exec calls
- [ ] AC-067: Cache MUST store file existence check results
- [ ] AC-068: Cache MUST be invalidated for path on write operation
- [ ] AC-069: Cache MUST be invalidated for parent directory on write operation
- [ ] AC-070: Cache MUST be invalidated for path on delete operation
- [ ] AC-071: Cache MUST support configurable TTL (default: 60 seconds)
- [ ] AC-072: Cache MUST expire entries automatically after TTL
- [ ] AC-073: Cache MUST support manual clearance via ClearCache() method
- [ ] AC-074: Cache MUST be disableable via configuration option
- [ ] AC-075: Cache MUST track and report hit/miss ratios for diagnostics

### Security Requirements (AC-076 to AC-090)

- [ ] AC-076: All command arguments MUST be shell-escaped using single-quote wrapping
- [ ] AC-077: System MUST reject paths containing null bytes
- [ ] AC-078: System MUST reject command arguments exceeding 4096 characters
- [ ] AC-079: Path traversal sequences (`..`) MUST be blocked or normalized
- [ ] AC-080: URL-encoded traversal sequences MUST be detected and blocked
- [ ] AC-081: System MUST verify resolved path is within mount boundary
- [ ] AC-082: System MUST detect and block symlink-based mount escapes
- [ ] AC-083: Container name/ID MUST be validated against safe character patterns
- [ ] AC-084: System MUST reject container names containing shell metacharacters
- [ ] AC-085: File content MUST NOT appear in process arguments (use stdin)
- [ ] AC-086: Sensitive content MUST be detected and logged appropriately
- [ ] AC-087: Atomic read/write operations MUST verify boundary in same command as operation
- [ ] AC-088: System MUST prevent deletion of mount point root directory
- [ ] AC-089: Read-only mounts MUST reject write and delete operations
- [ ] AC-090: All security violations MUST be logged with audit trail

### Error Handling (AC-091 to AC-098)

- [ ] AC-091: ContainerNotFoundException MUST be thrown when container does not exist
- [ ] AC-092: ContainerNotRunningException MUST be thrown when container is stopped
- [ ] AC-093: MountNotFoundException MUST be thrown for paths outside configured mounts
- [ ] AC-094: AccessDeniedException MUST be thrown for permission failures in container
- [ ] AC-095: TimeoutException MUST be thrown when command exceeds timeout
- [ ] AC-096: PathTraversalException MUST be thrown for traversal attack attempts
- [ ] AC-097: SymlinkEscapeException MUST be thrown for symlink-based mount escapes
- [ ] AC-098: All exceptions MUST include actionable diagnostic information

### Performance Requirements (AC-099 to AC-104)

- [ ] AC-099: Cached file existence check MUST complete in < 5ms
- [ ] AC-100: Uncached file read (1KB) MUST complete in < 100ms + Docker latency
- [ ] AC-101: Uncached file write (1KB) MUST complete in < 150ms + Docker latency
- [ ] AC-102: Directory listing (1000 files) MUST complete in < 200ms + Docker latency
- [ ] AC-103: Cache hit ratio MUST be trackable and reportable
- [ ] AC-104: System MUST support at least 100 concurrent operations per container

### Observability (AC-105 to AC-110)

- [ ] AC-105: All Docker exec commands MUST be logged at Debug level
- [ ] AC-106: Command execution duration MUST be logged for performance tracking
- [ ] AC-107: Cache hit/miss events MUST be logged at Trace level
- [ ] AC-108: Security violations MUST be logged at Warning level
- [ ] AC-109: Container health checks MUST be logged at Info level
- [ ] AC-110: Error conditions MUST be logged with full context at Error level

---

## Best Practices

### Container Integration

1. **Validate Mount Points at Startup**
   - Always verify that configured mount paths exist in the container before starting operations
   - Use `docker exec container test -d /app` during initialization
   - Rationale: Prevents cryptic errors later when file operations fail due to misconfigured mounts
   - Example validation:
     ```csharp
     var mountExists = await _executor.ExecuteAsync(
         containerId,
         new[] { "test", "-d", containerMountPath },
         CancellationToken.None);
     if (mountExists.ExitCode != 0)
         throw new MountNotFoundException($"Mount {containerMountPath} not accessible");
     ```

2. **Handle User ID Mapping Between Host and Container**
   - Container may run as different UID/GID than host user, causing permission mismatches
   - Match container user to host user: `docker run --user $(id -u):$(id -g) ...`
   - Or grant container user permissions: `chown -R 1000:1000 /host/project` (where 1000 is container UID)
   - Rationale: Prevents "permission denied" errors when agent writes files that host user can't modify
   - Check container's UID: `docker exec container id -u` should match host's `id -u`

3. **Implement Health Checks for Container Availability**
   - Docker containers can stop, restart, or become unresponsive mid-session
   - Implement periodic health checks: `docker inspect --format='{{.State.Running}}' container`
   - Retry transient failures (container restart) with exponential backoff
   - Fail fast on permanent failures (container removed) with clear error messages
   - Rationale: Prevents hanging indefinitely when container becomes unavailable

4. **Use Container Paths Consistently in Configuration**
   - All configuration paths should use container's perspective (`/app/src`), not host (`/home/user/project/src`)
   - Avoid relative path confusion by normalizing to absolute container paths early
   - Document which paths are host vs container in configuration comments
   - Rationale: Eliminates ambiguity about which file system namespace a path refers to

### Performance Optimization

5. **Batch Operations to Minimize Docker Exec Overhead**
   - Each `docker exec` has 10-50ms overhead (process spawn, exec setup)
   - Batch multiple operations into single commands: `find . -name '*.cs' | xargs cat` instead of multiple `cat` calls
   - Use streaming for large operations: `tar -c src/ | base64` instead of individual file reads
   - Rationale: Reduces latency by 10-50x for operations on multiple files (e.g., indexing 100 files: 5s vs 5min)

6. **Enable Aggressive Caching for Read-Heavy Workflows**
   - Use default 60s TTL for directory listings during code indexing
   - Increase TTL to 300s for read-only analysis workloads (container files won't change)
   - Decrease TTL to 10s for active development (frequent file modifications)
   - Monitor cache hit rates and adjust TTL based on actual hit/miss patterns
   - Rationale: 95% latency reduction on cache hits (5ms vs 50ms), critical for large codebases

7. **Configure Timeouts Based on Operation Type and File Sizes**
   - Default 30s timeout is appropriate for files < 10MB
   - Increase to 120s for files 10-100MB, 300s for files > 100MB
   - Lower timeout to 10s for existence checks and metadata queries (should complete in <1s)
   - Rationale: Prevents false timeouts on legitimate large file operations while still detecting hung containers quickly

8. **Avoid FileSystemWatcher for Cross-Container File Monitoring**
   - FileSystemWatcher (inotify) events don't propagate reliably across Docker mounts
   - Use polling-based approaches: periodic directory listing + change detection
   - If real-time updates needed, use Docker volume events or custom container-side webhooks
   - Rationale: FileSystemWatcher will miss changes made inside container, causing stale data

### Security Hardening

9. **Use Read-Only Mounts for Data Directories**
   - Mark mounts as `read_only: true` in configuration when agent should never modify them
   - Example: Configuration files, reference data, ML model weights
   - Prevents accidental or malicious modification of critical data
   - Enforce at Docker level: `docker run -v /data:/data:ro` in addition to config setting
   - Rationale: Defense in depth - prevents data corruption even if agent is compromised

10. **Validate All Paths Against Mount Boundaries**
    - Always check that resolved paths stay within configured mount points
    - Reject path traversal attempts (`../../../etc/passwd`) before executing commands
    - Normalize paths and verify: `Path.GetFullPath(containerPath).StartsWith(mountRoot)`
    - Log boundary violations as security events for audit review
    - Rationale: Prevents container escape via path traversal, protects host file system

11. **Never Construct Shell Commands from Untrusted Path Input**
    - Use parameterized command execution, not string concatenation
    - Wrap all path arguments in single quotes: `cat '/app/file.txt'`
    - Escape embedded single quotes: `file's name.txt` → `'file'\''s name.txt'`
    - Reject paths containing null bytes (shell escape attempt indicator)
    - Rationale: Prevents shell injection attacks that could execute arbitrary code in container

12. **Apply Principle of Least Privilege to Container Configuration**
    - Don't run containers as root unless absolutely necessary
    - Drop unnecessary Linux capabilities: `--cap-drop=ALL --cap-add=CHOWN` (if file ownership changes needed)
    - Use seccomp profiles to restrict syscalls container can make
    - Limit container resources: `--memory=512m --cpus=1.0` to prevent resource exhaustion
    - Rationale: Limits blast radius if agent or container is compromised

### Observability and Debugging

13. **Log All Docker Exec Commands at Debug Level**
    - Include full command string, container ID, exit code, and execution duration
    - Helps diagnose permission issues, timeout problems, and container misconfigurations
    - Example: `[DEBUG] docker exec abc123 cat '/app/file.txt' -> exit=0 duration=45ms`
    - Rationale: Essential for troubleshooting "works on host, fails in container" issues

14. **Track and Report Cache Hit Rates**
    - Expose cache hit/miss metrics: `CacheHitRate`, `CacheMissRate`, `CacheSize`
    - Log cache statistics periodically (e.g., every 1000 operations)
    - Use metrics to tune TTL settings for optimal performance
    - Rationale: Low hit rates indicate TTL too short or cache disabled; high miss rates indicate inefficient access patterns

15. **Include Actionable Context in Error Messages**
    - Don't just say "permission denied" - include container user, file owner, permissions
    - Example: `Permission denied reading '/app/secrets.txt': file owned by root (uid=0), container user is node (uid=1000), permissions=600`
    - Suggest remediation: "Run container as root with --user root, or change file ownership with chown"
    - Rationale: Reduces time-to-resolution by providing diagnostic info and solutions upfront

### Configuration Management

16. **Use Container Names, Not Ephemeral IDs**
    - Container IDs change every time container is recreated (e.g., `docker-compose down && up`)
    - Container names are stable across restarts: `container: my-app-container`
    - If using IDs, provide fallback logic to find container by label: `docker ps --filter "label=app=myapp"`
    - Rationale: Prevents configuration breakage every time development environment is recreated

17. **Document Mount Mappings in Configuration File**
    - Add comments explaining which host directory maps to which container path and why
    - Example:
      ```yaml
      mounts:
        # Source code: synced for live reload
        - host: /home/user/project/src
          container: /app/src
        # Data directory: pre-populated dataset (read-only)
        - host: /home/user/datasets
          container: /data
          read_only: true
      ```
    - Rationale: Future developers (or yourself in 6 months) won't remember mount purposes without documentation

18. **Version-Control Docker Configuration Alongside Code**
    - Include `.agent/config.yml` in version control
    - Commit Dockerfile and docker-compose.yml that define container setup
    - Document required Docker version and platform (Docker Desktop vs native Linux)
    - Rationale: Enables other developers to reproduce exact containerized development environment

### Testing and Validation

19. **Test with Production-Like Container Configuration**
    - If production uses read-only root filesystem, test agent with `--read-only` containers
    - If production uses non-root users, test agent with `--user 1000:1000` containers
    - If production uses resource limits, test agent with `--memory` and `--cpus` constraints
    - Rationale: Prevents "works in development, fails in production" issues due to environment differences

20. **Verify Graceful Handling of Container Lifecycle Events**
    - Test agent behavior when container is stopped mid-operation
    - Test agent behavior when container is restarted (cache invalidation, reconnection)
    - Test agent behavior when container is removed and recreated with same name
    - Ensure error messages are actionable (e.g., "Container stopped. Restart with: docker start my-app")
    - Rationale: Containers are ephemeral in nature; agent must handle lifecycle transitions gracefully

---

## Testing Requirements

### Unit Tests - File Reading Operations

```csharp
// Tests/Unit/FileSystem/Docker/DockerFSReadTests.cs
using Xunit;
using FluentAssertions;
using NSubstitute;

public class DockerFSReadTests
{
    private readonly IDockerCommandExecutor _mockExecutor;
    private readonly DockerFileSystem _sut;

    public DockerFSReadTests()
    {
        _mockExecutor = Substitute.For<IDockerCommandExecutor>();
        _sut = new DockerFileSystem(new DockerFSOptions
        {
            ContainerName = "test-container",
            Mounts = new[] { new MountMapping("/host/project", "/app") },
            CacheEnabled = false  // Disable for unit tests
        }, _mockExecutor);
    }

    [Fact]
    public async Task Should_Read_Text_File_Via_Docker_Exec()
    {
        // Arrange
        var expectedContent = "Console.WriteLine(\"Hello\");";
        _mockExecutor
            .ExecuteAsync("test-container", new[] { "cat", "/app/src/Program.cs" }, Arg.Any<CancellationToken>())
            .Returns(new ExecResult { ExitCode = 0, Stdout = expectedContent });

        // Act
        var content = await _sut.ReadFileAsync("src/Program.cs");

        // Assert
        content.Should().Be(expectedContent);
        await _mockExecutor.Received(1).ExecuteAsync(
            "test-container",
            Arg.Is<string[]>(args => args[0] == "cat" && args[1] == "/app/src/Program.cs"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Throw_FileNotFoundException_For_Missing_File()
    {
        // Arrange
        _mockExecutor
            .ExecuteAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(new ExecResult { ExitCode = 1, Stderr = "cat: /app/missing.txt: No such file or directory" });

        // Act
        var act = () => _sut.ReadFileAsync("missing.txt");

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("*missing.txt*");
    }

    [Fact]
    public async Task Should_Throw_UnauthorizedAccessException_For_Permission_Denied()
    {
        // Arrange
        _mockExecutor
            .ExecuteAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(new ExecResult { ExitCode = 126, Stderr = "cat: /app/secrets.txt: Permission denied" });

        // Act
        var act = () => _sut.ReadFileAsync("secrets.txt");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Permission denied*secrets.txt*");
    }

    [Fact]
    public async Task Should_Support_Cancellation_Token()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockExecutor
            .ExecuteAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => throw new OperationCanceledException());

        // Act
        var act = () => _sut.ReadFileAsync("file.txt", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
```

### Unit Tests - File Writing Operations

```csharp
// Tests/Unit/FileSystem/Docker/DockerFSWriteTests.cs
public class DockerFSWriteTests
{
    private readonly IDockerCommandExecutor _mockExecutor;
    private readonly DockerFileSystem _sut;

    public DockerFSWriteTests()
    {
        _mockExecutor = Substitute.For<IDockerCommandExecutor>();
        _sut = new DockerFileSystem(new DockerFSOptions
        {
            ContainerName = "test-container",
            Mounts = new[] { new MountMapping("/host/project", "/app") }
        }, _mockExecutor);
    }

    [Fact]
    public async Task Should_Write_File_Atomically_Via_Temp_File()
    {
        // Arrange
        var content = "new configuration";
        var received Calls = new List<string[]>();

        _mockExecutor
            .ExecuteAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(new ExecResult { ExitCode = 0 })
            .AndDoes(callInfo => receivedCalls.Add(callInfo.ArgAt<string[]>(1)));

        // Act
        await _sut.WriteFileAsync("config.json", content);

        // Assert
        receivedCalls.Should().HaveCountGreaterThanOrEqualTo(3);

        // Should create temp file with content via stdin
        receivedCalls.Should().Contain(args =>
            args.Contains("cat") && args.Contains(">") && args.Any(a => a.StartsWith("/app/.tmp_")));

        // Should move temp file to final location
        receivedCalls.Should().Contain(args =>
            args.Contains("mv") && args.Contains("/app/config.json"));
    }

    [Fact]
    public async Task Should_Create_Parent_Directories_If_Missing()
    {
        // Arrange
        _mockExecutor
            .ExecuteAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(new ExecResult { ExitCode = 0 });

        // Act
        await _sut.WriteFileAsync("deeply/nested/path/file.txt", "content");

        // Assert
        await _mockExecutor.Received().ExecuteAsync(
            "test-container",
            Arg.Is<string[]>(args => args[0] == "mkdir" && args[1] == "-p" && args[2] == "/app/deeply/nested/path"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Invalidate_Cache_After_Write()
    {
        // Arrange
        var cache = Substitute.For<IDockerCache>();
        var sut = new DockerFileSystem(new DockerFSOptions
        {
            ContainerName = "test-container",
            Mounts = new[] { new MountMapping("/host", "/app") },
            CacheEnabled = true
        }, _mockExecutor, cache);

        _mockExecutor
            .ExecuteAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(new ExecResult { ExitCode = 0 });

        // Act
        await sut.WriteFileAsync("src/Program.cs", "new code");

        // Assert
        cache.Received().Invalidate("/app/src/Program.cs");  // File path
        cache.Received().Invalidate("/app/src");             // Parent directory
    }
}
```

### Unit Tests - Security

```csharp
// Tests/Unit/FileSystem/Docker/DockerSecurityTests.cs
public class DockerSecurityTests
{
    private readonly IDockerCommandExecutor _mockExecutor;
    private readonly DockerFileSystem _sut;

    [Fact]
    public async Task Should_Escape_Single_Quotes_In_File_Paths()
    {
        // Arrange
        _mockExecutor = Substitute.For<IDockerCommandExecutor>();
        _sut = new DockerFileSystem(new DockerFSOptions
        {
            ContainerName = "test",
            Mounts = new[] { new MountMapping("/host", "/app") }
        }, _mockExecutor);

        _mockExecutor
            .ExecuteAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(new ExecResult { ExitCode = 0, Stdout = "content" });

        // Act
        await _sut.ReadFileAsync("file's name.txt");

        // Assert
        await _mockExecutor.Received().ExecuteAsync(
            "test",
            Arg.Is<string[]>(args =>
                args.Contains("cat") &&
                args.Any(a => a.Contains("file'\\''s name.txt"))), // Escaped as file'\''s name.txt
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Block_Shell_Injection_Via_Semicolon()
    {
        // Arrange
        _sut = new DockerFileSystem(new DockerFSOptions
        {
            ContainerName = "test",
            Mounts = new[] { new MountMapping("/host", "/app") }
        });

        // Act - Malicious path with shell command injection
        var act = () => _sut.ReadFileAsync("file.txt; rm -rf /app/*");

        // Assert - Should either escape properly or reject
        // If escaped: 'file.txt; rm -rf /app/*' is treated as literal filename
        // If rejected: throws SecurityException for suspicious characters
        await act.Should().NotThrowAsync();  // Must handle safely either way
    }

    [Fact]
    public async Task Should_Block_Path_Traversal_Outside_Mount()
    {
        // Arrange
        _sut = new DockerFileSystem(new DockerFSOptions
        {
            ContainerName = "test",
            Mounts = new[] { new MountMapping("/host/project", "/app") }
        });

        // Act - Attempt to escape mount boundary
        var act = () => _sut.ReadFileAsync("../../etc/passwd");

        // Assert
        await act.Should().ThrowAsync<MountBoundaryViolationException>()
            .WithMessage("*outside mount boundary*");
    }

    [Fact]
    public async Task Should_Enforce_Mount_Boundary_On_Write()
    {
        // Arrange
        _sut = new DockerFileSystem(new DockerFSOptions
        {
            ContainerName = "test",
            Mounts = new[] { new MountMapping("/host/project", "/app") }
        });

        // Act
        var act = () => _sut.WriteFileAsync("/etc/shadow", "malicious content");

        // Assert
        await act.Should().ThrowAsync<MountBoundaryViolationException>();
    }

    [Fact]
    public void Should_Reject_Container_Names_With_Shell_Metacharacters()
    {
        // Act
        var act = () => new DockerFileSystem(new DockerFSOptions
        {
            ContainerName = "test; rm -rf /",  // Shell injection in container name
            Mounts = new[] { new MountMapping("/host", "/app") }
        });

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*invalid container name*shell metacharacters*");
    }
}
```

### Unit Tests - Caching

```csharp
// Tests/Unit/FileSystem/Docker/DockerCacheTests.cs
public class DockerCacheTests
{
    [Fact]
    public async Task Should_Cache_Directory_Listing()
    {
        // Arrange
        var cache = new DockerCache(new CacheOptions { TTL = TimeSpan.FromSeconds(60) });
        var listing = new[] { "/app/file1.txt", "/app/file2.txt" };

        // Act
        cache.Set("dir:/app", listing);
        var cached = cache.TryGet<string[]>("dir:/app", out var result);

        // Assert
        cached.Should().BeTrue();
        result.Should().BeEquivalentTo(listing);
    }

    [Fact]
    public async Task Should_Invalidate_Cache_On_Write()
    {
        // Arrange
        var cache = new DockerCache(new CacheOptions { TTL = TimeSpan.FromSeconds(60) });
        cache.Set("file:/app/src/file.txt", "old content");
        cache.Set("dir:/app/src", new[] { "file.txt" });

        // Act
        cache.Invalidate("/app/src/file.txt");
        cache.Invalidate("/app/src");

        // Assert
        cache.TryGet<string>("file:/app/src/file.txt", out _).Should().BeFalse();
        cache.TryGet<string[]>("dir:/app/src", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Should_Expire_Entries_After_TTL()
    {
        // Arrange
        var cache = new DockerCache(new CacheOptions { TTL = TimeSpan.FromMilliseconds(100) });
        cache.Set("test-key", "value");

        // Act
        await Task.Delay(150);  // Wait for TTL expiration
        var cached = cache.TryGet<string>("test-key", out _);

        // Assert
        cached.Should().BeFalse();
    }

    [Fact]
    public void Should_Report_Cache_Hit_Miss_Metrics()
    {
        // Arrange
        var cache = new DockerCache(new CacheOptions { TTL = TimeSpan.FromSeconds(60) });
        cache.Set("key", "value");

        // Act
        cache.TryGet<string>("key", out _);      // Hit
        cache.TryGet<string>("missing", out _);  // Miss
        cache.TryGet<string>("key", out _);      // Hit

        // Assert
        cache.Metrics.Hits.Should().Be(2);
        cache.Metrics.Misses.Should().Be(1);
        cache.Metrics.HitRate.Should().BeApproximately(0.67, 0.01);
    }
}
```

### Integration Tests

```csharp
// Tests/Integration/FileSystem/Docker/DockerFSIntegrationTests.cs
[Collection("Docker")] // Requires Docker daemon running
public class DockerFSIntegrationTests : IAsyncLifetime
{
    private string _containerId;
    private DockerFileSystem _sut;

    public async Task InitializeAsync()
    {
        // Start test container
        var process = await Process.Start(new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = "run -d --name test-dockerfs-integration alpine:latest sleep 3600",
            RedirectStandardOutput = true
        });

        _containerId = (await process.StandardOutput.ReadToEndAsync()).Trim();

        _sut = new DockerFileSystem(new DockerFSOptions
        {
            ContainerName = "test-dockerfs-integration",
            Mounts = new[] { new MountMapping("/tmp", "/tmp") }
        });
    }

    [Fact]
    public async Task Should_Read_And_Write_File_In_Real_Container()
    {
        // Arrange
        var testContent = "Integration test content " + Guid.NewGuid();
        var filePath = $"/tmp/test-{Guid.NewGuid()}.txt";

        // Act - Write
        await _sut.WriteFileAsync(filePath, testContent);

        // Act - Read
        var content = await _sut.ReadFileAsync(filePath);

        // Assert
        content.Should().Be(testContent);
    }

    [Fact]
    public async Task Should_Handle_Large_File()
    {
        // Arrange
        var largeContent = new string('X', 10 * 1024 * 1024); // 10 MB
        var filePath = $"/tmp/large-{Guid.NewGuid()}.bin";

        // Act
        var sw = Stopwatch.StartNew();
        await _sut.WriteFileAsync(filePath, largeContent);
        var writeTime = sw.Elapsed;

        sw.Restart();
        var content = await _sut.ReadFileAsync(filePath);
        var readTime = sw.Elapsed;

        // Assert
        content.Length.Should().Be(largeContent.Length);
        writeTime.Should().BeLessThan(TimeSpan.FromSeconds(5));  // Large file write
        readTime.Should().BeLessThan(TimeSpan.FromSeconds(3));   // Large file read
    }

    public async Task DisposeAsync()
    {
        // Cleanup container
        await Process.Start("docker", $"rm -f {_containerId}");
    }
}
```

### Performance Benchmarks

| Test Scenario | Target | Maximum | Notes |
|---------------|--------|---------|-------|
| Cached existence check | < 5ms | 10ms | Cache hit, in-memory lookup |
| Uncached read (1KB file) | < 100ms | 200ms | Includes Docker exec overhead |
| Uncached write (1KB file) | < 150ms | 300ms | Includes temp-file-then-rename pattern |
| Directory listing (1000 files) | < 200ms | 500ms | Single `find` command |
| Cache invalidation | < 1ms | 5ms | Remove entries from cache |
| Path translation | < 0.1ms | 1ms | Mount mapping lookup |

---

## User Verification Steps

### Scenario 1: Basic File Read from Container

**Objective:** Verify DockerFileSystem can read files from a running container.

**Prerequisites:**
- Docker daemon running
- Test container created: `docker run -d --name test-container -v $(pwd)/testdata:/app alpine tail -f /dev/null`
- Test file created: `echo "Hello from container" > testdata/hello.txt`

**Steps:**
```bash
# 1. Configure acode for Docker FS mode
$ cat > .agent/config.yml << EOF
repo:
  fs_type: docker
  docker:
    container: test-container
    mounts:
      - host: $(pwd)/testdata
        container: /app
EOF

# 2. Start acode and request file read
$ acode run "Read the file /app/hello.txt and show me its contents"

# Expected output:
[Tool: read_file]
  Path: /app/hello.txt
  Container: test-container
  Content: Hello from container

# 3. Verify via manual docker exec
$ docker exec test-container cat /app/hello.txt
Hello from container
```

**Expected Results:**
- File content matches "Hello from container"
- No errors in acode output
- Debug log shows `docker exec test-container cat /app/hello.txt` command

---

### Scenario 2: Write File to Container with Atomic Rename

**Objective:** Verify files are written atomically using temp-file-then-rename pattern.

**Prerequisites:**
- Docker container running with writable mount

**Steps:**
```bash
# 1. Request file write via acode
$ acode run "Create a new file /app/newfile.txt with the content 'Written by Acode'"

# Expected output:
[Tool: write_file]
  Path: /app/newfile.txt
  Container: test-container
  Method: docker exec (temp + mv for atomicity)
  Result: SUCCESS

# 2. Verify file was created in container
$ docker exec test-container cat /app/newfile.txt
Written by Acode

# 3. Verify no temp files left behind
$ docker exec test-container ls -la /app/ | grep tmp
# Should return nothing - all temp files cleaned up

# 4. Check debug logs for atomic write pattern
$ grep -E "\.tmp\." ~/.acode/logs/debug.log | tail -5
# Should show temp file creation and mv command
```

**Expected Results:**
- File created with correct content
- No `.tmp` files remaining in directory
- Debug log shows temp file creation followed by rename

---

### Scenario 3: Cache Hit Performance Verification

**Objective:** Verify caching reduces Docker exec calls and improves performance.

**Prerequisites:**
- Caching enabled in configuration (default)

**Steps:**
```bash
# 1. Configure cache settings
$ cat >> .agent/config.yml << EOF
  docker:
    cache:
      enabled: true
      ttl_seconds: 60
EOF

# 2. First read (cache miss - slow)
$ time acode run "Read /app/hello.txt"
# Note the execution time (e.g., 150ms)

# 3. Second read (cache hit - fast)
$ time acode run "Read /app/hello.txt"
# Note the execution time (e.g., 5ms)

# 4. Verify cache hit in logs
$ grep "cache hit" ~/.acode/logs/trace.log | tail -1
[TRACE] DockerFSCache: Cache hit for /app/hello.txt

# 5. Wait for TTL expiration and re-read
$ sleep 65
$ time acode run "Read /app/hello.txt"
# Should be slow again (cache expired)

# 6. Verify cache miss in logs
$ grep "cache miss" ~/.acode/logs/trace.log | tail -1
[TRACE] DockerFSCache: Cache miss for /app/hello.txt (expired)
```

**Expected Results:**
- First read: ~100-150ms (Docker exec overhead)
- Second read: < 10ms (cache hit)
- After TTL: ~100-150ms again (cache expired)
- Logs show hit/miss events

---

### Scenario 4: Container Not Running Error Handling

**Objective:** Verify clear error messages when container is stopped.

**Steps:**
```bash
# 1. Stop the test container
$ docker stop test-container

# 2. Attempt file read
$ acode run "Read /app/hello.txt"

# Expected output:
[ERROR] ContainerNotRunningException: Container 'test-container' exists but
is not running (state: Exited). Start the container with: docker start test-container

# 3. Verify error code returned
$ echo $?
1

# 4. Start container and retry
$ docker start test-container
$ acode run "Read /app/hello.txt"
# Should succeed now
```

**Expected Results:**
- Clear error message indicating container is stopped
- Helpful suggestion to start container
- Non-zero exit code
- Operation succeeds after container restart

---

### Scenario 5: Path Traversal Attack Prevention

**Objective:** Verify system blocks path traversal attempts.

**Steps:**
```bash
# 1. Attempt to read outside mount boundary
$ acode run "Read the file /app/../etc/passwd"

# Expected output:
[ERROR] PathTraversalException: Path '/app/../etc/passwd' normalizes to
'/etc/passwd' which escapes mount boundary '/app'

# 2. Attempt URL-encoded traversal
$ acode run "Read the file /app/%2e%2e/etc/passwd"

# Expected output:
[ERROR] PathTraversalException: Path contains URL-encoded traversal sequences

# 3. Attempt via symlink
$ docker exec test-container ln -sf /etc/passwd /app/evil-link
$ acode run "Read the file /app/evil-link"

# Expected output:
[ERROR] SymlinkEscapeException: Path '/app/evil-link' resolves to
'/etc/passwd' via symlink which escapes mount boundary

# 4. Verify security warnings in audit log
$ grep "SECURITY" ~/.acode/logs/audit.log | tail -3
[WARN] Security violation: Path traversal attempt blocked
[WARN] Security violation: URL-encoded traversal blocked
[WARN] Security violation: Symlink escape blocked
```

**Expected Results:**
- All traversal attempts blocked
- Clear error messages explaining why
- Audit log records security violations
- No sensitive files exposed

---

### Scenario 6: Shell Injection Prevention

**Objective:** Verify system escapes shell metacharacters safely.

**Steps:**
```bash
# 1. Create file with special characters in name
$ docker exec test-container touch "/app/file with spaces.txt"
$ docker exec test-container sh -c 'echo "test" > "/app/file with spaces.txt"'

# 2. Read file with spaces (should work)
$ acode run "Read '/app/file with spaces.txt'"
# Should succeed

# 3. Attempt command injection via filename
$ acode run "Read '/app/file.txt; rm -rf /app/*'"

# Expected output:
[ERROR] FileNotFoundException: File '/app/file.txt; rm -rf /app/*' not found

# NOT: Files deleted from /app/

# 4. Verify container files intact
$ docker exec test-container ls /app/
file with spaces.txt
hello.txt
newfile.txt
# All files still present

# 5. Check debug log for proper escaping
$ grep "docker exec" ~/.acode/logs/debug.log | tail -1
docker exec test-container cat '/app/file.txt; rm -rf /app/*'
# Note: entire path treated as literal string
```

**Expected Results:**
- Files with spaces read correctly
- Injection attempts treated as literal filenames
- No files deleted or commands executed
- Debug log shows proper single-quote escaping

---

### Scenario 7: Large File Handling

**Objective:** Verify system handles large files with streaming.

**Steps:**
```bash
# 1. Create a large file (50MB)
$ docker exec test-container dd if=/dev/urandom of=/app/large.bin bs=1M count=50

# 2. Read the large file
$ acode run "Read /app/large.bin and tell me its size"

# Expected output:
[Tool: read_file]
  Path: /app/large.bin
  Container: test-container
  Mode: Binary (base64)
  Size: 52,428,800 bytes
  Transfer: Streamed in chunks

# 3. Verify memory usage stayed bounded
# (Check acode process memory didn't spike to 50MB+)

# 4. Write a large file
$ acode run "Create a 10MB file at /app/large-write.bin with random content"

# 5. Verify via checksum
$ docker exec test-container stat /app/large-write.bin
# Should show ~10MB file
```

**Expected Results:**
- Large file read successfully without timeout
- Memory usage remains bounded
- Large file write completes atomically
- No partial/corrupted files

---

### Scenario 8: Multiple Mount Point Handling

**Objective:** Verify system handles multiple mount configurations.

**Steps:**
```bash
# 1. Create container with multiple mounts
$ docker stop test-container
$ docker rm test-container
$ mkdir -p testdata/src testdata/config
$ echo "source code" > testdata/src/main.cs
$ echo "config data" > testdata/config/app.json
$ docker run -d --name test-container \
    -v $(pwd)/testdata/src:/app/src \
    -v $(pwd)/testdata/config:/app/config:ro \
    alpine tail -f /dev/null

# 2. Configure multiple mounts
$ cat > .agent/config.yml << EOF
repo:
  fs_type: docker
  docker:
    container: test-container
    mounts:
      - host: $(pwd)/testdata/src
        container: /app/src
      - host: $(pwd)/testdata/config
        container: /app/config
        read_only: true
EOF

# 3. Read from first mount
$ acode run "Read /app/src/main.cs"
# Should succeed: "source code"

# 4. Read from second mount
$ acode run "Read /app/config/app.json"
# Should succeed: "config data"

# 5. Write to first mount (writable)
$ acode run "Write 'updated' to /app/src/main.cs"
# Should succeed

# 6. Attempt write to read-only mount
$ acode run "Write 'hacked' to /app/config/app.json"

# Expected output:
[ERROR] ReadOnlyMountException: Mount '/app/config' is read-only.
Cannot write to '/app/config/app.json'
```

**Expected Results:**
- Both mounts accessible for reading
- Writable mount accepts writes
- Read-only mount rejects writes
- Clear error for read-only violations

---

### Scenario 9: Concurrent Operations Stress Test

**Objective:** Verify system handles concurrent operations safely.

**Steps:**
```bash
# 1. Create test script for concurrent operations
$ cat > stress-test.sh << 'EOF'
#!/bin/bash
for i in {1..20}; do
    (acode run "Read /app/hello.txt" &)
    (acode run "Write 'test$i' to /app/concurrent-$i.txt" &)
done
wait
EOF
$ chmod +x stress-test.sh

# 2. Run concurrent operations
$ ./stress-test.sh

# 3. Verify all files created
$ docker exec test-container ls /app/concurrent-*.txt | wc -l
20

# 4. Verify no corruption
$ docker exec test-container cat /app/concurrent-1.txt
test1

# 5. Check for any errors in logs
$ grep -c "ERROR" ~/.acode/logs/error.log
0
```

**Expected Results:**
- All 20 files created successfully
- No file corruption or partial writes
- No race condition errors
- No deadlocks (script completes)

---

### Scenario 10: Container Restart Recovery

**Objective:** Verify system recovers gracefully from container restart.

**Steps:**
```bash
# 1. Start a long operation
$ acode run "List all files in /app recursively" &

# 2. Restart container mid-operation
$ docker restart test-container

# 3. Wait for operation to complete/fail
$ wait

# Expected output (one of):
[WARN] Container restarted during operation, retrying...
[Tool: enumerate_files]
  Path: /app/**/*
  Results: (file list)

# OR if retry fails:
[ERROR] Operation failed after container restart. Please retry.

# 4. Verify cache was invalidated
$ grep "cache invalidated" ~/.acode/logs/debug.log | tail -1
[DEBUG] DockerFSCache: All entries invalidated (container restart detected)

# 5. Subsequent operations should work
$ acode run "Read /app/hello.txt"
# Should succeed
```

**Expected Results:**
- Operation either retries successfully or fails gracefully
- No partial results or corruption
- Cache invalidated on restart detection
- Subsequent operations work normally

---

## Implementation Prompt

Implement the Docker-mounted file system for Task 014.b following Clean Architecture principles. All code must be fully functional with complete error handling, security validation, and logging.

### Step 1: Create DockerFSOptions Configuration Class

**File:** `src/AgenticCoder.Infrastructure/FileSystem/Docker/DockerFSOptions.cs`

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AgenticCoder.Infrastructure.FileSystem.Docker;

/// <summary>
/// Configuration options for Docker file system operations.
/// </summary>
public sealed class DockerFSOptions
{
    /// <summary>
    /// The container name or ID to connect to.
    /// </summary>
    [Required]
    [RegularExpression(@"^[a-zA-Z0-9][a-zA-Z0-9_.\-]{0,127}$|^[a-f0-9]{12,64}$")]
    public required string ContainerName { get; init; }

    /// <summary>
    /// Mount mappings between host and container paths.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required IReadOnlyList<MountMapping> Mounts { get; init; }

    /// <summary>
    /// Timeout for individual docker exec commands.
    /// </summary>
    public TimeSpan CommandTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Timeout for write operations (longer due to temp file pattern).
    /// </summary>
    public TimeSpan WriteTimeout { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Whether caching is enabled.
    /// </summary>
    public bool CacheEnabled { get; init; } = true;

    /// <summary>
    /// Time-to-live for cached entries.
    /// </summary>
    public TimeSpan CacheTtl { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Maximum file size to read in a single operation (larger files use streaming).
    /// </summary>
    public long MaxSingleReadSize { get; init; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// Number of retry attempts for transient failures.
    /// </summary>
    public int RetryCount { get; init; } = 3;

    /// <summary>
    /// Base delay between retries (exponential backoff applied).
    /// </summary>
    public TimeSpan RetryBaseDelay { get; init; } = TimeSpan.FromMilliseconds(100);
}

/// <summary>
/// Represents a mount mapping between host and container paths.
/// </summary>
public sealed class MountMapping
{
    [Required]
    public required string HostPath { get; init; }

    [Required]
    public required string ContainerPath { get; init; }

    public bool ReadOnly { get; init; } = false;
}
```

---

### Step 2: Create DockerCommandExecutor for Safe Command Execution

**File:** `src/AgenticCoder.Infrastructure/FileSystem/Docker/DockerCommandExecutor.cs`

```csharp
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AgenticCoder.Infrastructure.FileSystem.Docker;

/// <summary>
/// Executes Docker commands safely with proper escaping and timeout handling.
/// </summary>
public sealed class DockerCommandExecutor
{
    private readonly string _containerName;
    private readonly TimeSpan _defaultTimeout;
    private readonly ILogger<DockerCommandExecutor> _logger;
    private readonly DockerCommandSanitizer _sanitizer;

    public DockerCommandExecutor(
        string containerName,
        TimeSpan defaultTimeout,
        ILogger<DockerCommandExecutor> logger)
    {
        _containerName = containerName ?? throw new ArgumentNullException(nameof(containerName));
        _defaultTimeout = defaultTimeout;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sanitizer = new DockerCommandSanitizer();

        // Validate container name at construction time
        _sanitizer.ValidateContainerName(containerName);
    }

    /// <summary>
    /// Escapes an argument for safe shell use.
    /// </summary>
    public string EscapeArgument(string argument)
    {
        return _sanitizer.EscapeShellArgument(argument);
    }

    /// <summary>
    /// Executes a command in the container and returns the result.
    /// </summary>
    public async Task<DockerExecResult> ExecAsync(
        string command,
        CancellationToken ct = default)
    {
        return await ExecAsync(command, _defaultTimeout, ct);
    }

    /// <summary>
    /// Executes a command in the container with custom timeout.
    /// </summary>
    public async Task<DockerExecResult> ExecAsync(
        string command,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        var fullCommand = $"docker exec {_containerName} sh -c {EscapeArgument(command)}";

        _logger.LogDebug("Executing: {Command}", fullCommand);
        var stopwatch = Stopwatch.StartNew();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"exec {_containerName} sh -c {EscapeArgument(command)}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cts.Token);

            stopwatch.Stop();
            _logger.LogDebug("Command completed in {Duration}ms with exit code {ExitCode}",
                stopwatch.ElapsedMilliseconds, process.ExitCode);

            return new DockerExecResult
            {
                ExitCode = process.ExitCode,
                Output = outputBuilder.ToString().TrimEnd(),
                Error = errorBuilder.ToString().TrimEnd(),
                Duration = stopwatch.Elapsed
            };
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Command timed out after {Timeout}s", timeout.TotalSeconds);
            throw new TimeoutException(
                $"Docker command timed out after {timeout.TotalSeconds} seconds");
        }
    }

    /// <summary>
    /// Executes a command with stdin input (for secure content transfer).
    /// </summary>
    public async Task<DockerExecResult> ExecWithStdinAsync(
        string command,
        string stdinContent,
        CancellationToken ct = default)
    {
        return await ExecWithStdinAsync(command, stdinContent, _defaultTimeout, ct);
    }

    /// <summary>
    /// Executes a command with stdin input and custom timeout.
    /// </summary>
    public async Task<DockerExecResult> ExecWithStdinAsync(
        string command,
        string stdinContent,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Executing with stdin: docker exec -i {Container} sh -c ...",
            _containerName);
        var stopwatch = Stopwatch.StartNew();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"exec -i {_containerName} sh -c {EscapeArgument(command)}",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Write content to stdin
            await process.StandardInput.WriteAsync(stdinContent);
            process.StandardInput.Close();

            await process.WaitForExitAsync(cts.Token);

            stopwatch.Stop();
            _logger.LogDebug("Stdin command completed in {Duration}ms",
                stopwatch.ElapsedMilliseconds);

            return new DockerExecResult
            {
                ExitCode = process.ExitCode,
                Output = outputBuilder.ToString().TrimEnd(),
                Error = errorBuilder.ToString().TrimEnd(),
                Duration = stopwatch.Elapsed
            };
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Docker stdin command timed out after {timeout.TotalSeconds} seconds");
        }
    }
}

/// <summary>
/// Result of a Docker exec command.
/// </summary>
public sealed class DockerExecResult
{
    public int ExitCode { get; init; }
    public string Output { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }

    public bool IsSuccess => ExitCode == 0;
}
```

---

### Step 3: Create DockerFSCache for Performance Optimization

**File:** `src/AgenticCoder.Infrastructure/FileSystem/Docker/DockerFSCache.cs`

```csharp
using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace AgenticCoder.Infrastructure.FileSystem.Docker;

/// <summary>
/// Cache for Docker FS operations to reduce exec overhead.
/// </summary>
public sealed class DockerFSCache : IDisposable
{
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new();
    private readonly TimeSpan _ttl;
    private readonly bool _enabled;
    private readonly ILogger<DockerFSCache> _logger;
    private readonly Timer _cleanupTimer;

    private long _hits;
    private long _misses;

    public DockerFSCache(TimeSpan ttl, bool enabled, ILogger<DockerFSCache> logger)
    {
        _ttl = ttl;
        _enabled = enabled;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Periodic cleanup of expired entries
        _cleanupTimer = new Timer(
            _ => CleanupExpired(),
            null,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// Gets the cache hit ratio as a percentage.
    /// </summary>
    public double HitRatio
    {
        get
        {
            var total = _hits + _misses;
            return total == 0 ? 0 : (double)_hits / total * 100;
        }
    }

    /// <summary>
    /// Tries to get a cached value.
    /// </summary>
    public bool TryGet<T>(string key, out T? value)
    {
        value = default;

        if (!_enabled)
        {
            Interlocked.Increment(ref _misses);
            return false;
        }

        if (_entries.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt > DateTime.UtcNow)
            {
                value = (T)entry.Value;
                Interlocked.Increment(ref _hits);
                _logger.LogTrace("Cache hit for {Key}", key);
                return true;
            }

            // Expired - remove it
            _entries.TryRemove(key, out _);
            _logger.LogTrace("Cache miss for {Key} (expired)", key);
        }
        else
        {
            _logger.LogTrace("Cache miss for {Key}", key);
        }

        Interlocked.Increment(ref _misses);
        return false;
    }

    /// <summary>
    /// Sets a cached value.
    /// </summary>
    public void Set<T>(string key, T value)
    {
        if (!_enabled) return;

        var entry = new CacheEntry
        {
            Value = value!,
            ExpiresAt = DateTime.UtcNow.Add(_ttl)
        };

        _entries[key] = entry;
        _logger.LogTrace("Cache set for {Key}, expires at {ExpiresAt}", key, entry.ExpiresAt);
    }

    /// <summary>
    /// Invalidates a specific key and optionally its parent directory.
    /// </summary>
    public void Invalidate(string path, bool includeParent = true)
    {
        if (!_enabled) return;

        _entries.TryRemove(path, out _);
        _entries.TryRemove($"exists:{path}", out _);
        _entries.TryRemove($"metadata:{path}", out _);

        _logger.LogDebug("Cache invalidated for {Path}", path);

        if (includeParent)
        {
            var parent = GetParentPath(path);
            if (!string.IsNullOrEmpty(parent))
            {
                _entries.TryRemove($"list:{parent}", out _);
                _logger.LogDebug("Parent cache invalidated for {Parent}", parent);
            }
        }
    }

    /// <summary>
    /// Clears all cached entries.
    /// </summary>
    public void Clear()
    {
        _entries.Clear();
        _logger.LogDebug("Cache cleared");
    }

    private void CleanupExpired()
    {
        var now = DateTime.UtcNow;
        var removed = 0;

        foreach (var kvp in _entries)
        {
            if (kvp.Value.ExpiresAt < now)
            {
                if (_entries.TryRemove(kvp.Key, out _))
                    removed++;
            }
        }

        if (removed > 0)
        {
            _logger.LogDebug("Cache cleanup removed {Count} expired entries", removed);
        }
    }

    private static string? GetParentPath(string path)
    {
        var lastSlash = path.LastIndexOf('/');
        return lastSlash > 0 ? path[..lastSlash] : null;
    }

    public void Dispose()
    {
        _cleanupTimer.Dispose();
    }

    private sealed class CacheEntry
    {
        public required object Value { get; init; }
        public required DateTime ExpiresAt { get; init; }
    }
}
```

---

### Step 4: Create MountTranslator for Path Translation

**File:** `src/AgenticCoder.Infrastructure/FileSystem/Docker/MountTranslator.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace AgenticCoder.Infrastructure.FileSystem.Docker;

/// <summary>
/// Translates paths between host and container using mount mappings.
/// </summary>
public sealed class MountTranslator
{
    private readonly IReadOnlyList<MountMapping> _mounts;

    public MountTranslator(IReadOnlyList<MountMapping> mounts)
    {
        _mounts = mounts ?? throw new ArgumentNullException(nameof(mounts));

        if (_mounts.Count == 0)
        {
            throw new ArgumentException("At least one mount mapping is required", nameof(mounts));
        }
    }

    /// <summary>
    /// Translates a host path to a container path.
    /// </summary>
    public string ToContainerPath(string hostPath)
    {
        var normalized = NormalizePath(hostPath);

        var mount = _mounts
            .Where(m => normalized.StartsWith(NormalizePath(m.HostPath), StringComparison.Ordinal))
            .OrderByDescending(m => m.HostPath.Length)
            .FirstOrDefault();

        if (mount == null)
        {
            throw new MountNotFoundException(
                $"Host path '{hostPath}' is not within any configured mount. " +
                $"Configured host paths: {string.Join(", ", _mounts.Select(m => m.HostPath))}");
        }

        var relativePath = normalized[NormalizePath(mount.HostPath).Length..].TrimStart('/');
        var containerPath = string.IsNullOrEmpty(relativePath)
            ? mount.ContainerPath
            : $"{mount.ContainerPath.TrimEnd('/')}/{relativePath}";

        return containerPath;
    }

    /// <summary>
    /// Translates a container path to a host path.
    /// </summary>
    public string ToHostPath(string containerPath)
    {
        var normalized = NormalizePath(containerPath);

        var mount = _mounts
            .Where(m => normalized.StartsWith(NormalizePath(m.ContainerPath), StringComparison.Ordinal))
            .OrderByDescending(m => m.ContainerPath.Length)
            .FirstOrDefault();

        if (mount == null)
        {
            throw new MountNotFoundException(
                $"Container path '{containerPath}' is not within any configured mount. " +
                $"Configured container paths: {string.Join(", ", _mounts.Select(m => m.ContainerPath))}");
        }

        var relativePath = normalized[NormalizePath(mount.ContainerPath).Length..].TrimStart('/');
        var hostPath = string.IsNullOrEmpty(relativePath)
            ? mount.HostPath
            : $"{mount.HostPath.TrimEnd('/')}/{relativePath}";

        return hostPath;
    }

    /// <summary>
    /// Finds the mount containing a container path.
    /// </summary>
    public MountMapping? FindMount(string containerPath)
    {
        var normalized = NormalizePath(containerPath);

        return _mounts
            .Where(m => normalized.StartsWith(NormalizePath(m.ContainerPath), StringComparison.Ordinal) ||
                       normalized == NormalizePath(m.ContainerPath).TrimEnd('/'))
            .OrderByDescending(m => m.ContainerPath.Length)
            .FirstOrDefault();
    }

    /// <summary>
    /// Checks if a container path is within a read-only mount.
    /// </summary>
    public bool IsReadOnly(string containerPath)
    {
        var mount = FindMount(containerPath);
        return mount?.ReadOnly ?? false;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimEnd('/');
    }
}

public class MountNotFoundException : Exception
{
    public MountNotFoundException(string message) : base(message) { }
}
```

---

### Step 5: Create DockerFileSystem Main Implementation

**File:** `src/AgenticCoder.Infrastructure/FileSystem/Docker/DockerFileSystem.cs`

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AgenticCoder.Domain.FileSystem;
using Microsoft.Extensions.Logging;

namespace AgenticCoder.Infrastructure.FileSystem.Docker;

/// <summary>
/// IRepoFS implementation that executes file operations via Docker exec commands.
/// </summary>
public sealed class DockerFileSystem : IRepoFS, IDisposable
{
    private readonly DockerFSOptions _options;
    private readonly DockerCommandExecutor _executor;
    private readonly MountTranslator _translator;
    private readonly ContainerPathValidator _pathValidator;
    private readonly MountBoundaryEnforcer _boundaryEnforcer;
    private readonly DockerFSCache _cache;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<DockerFileSystem> _logger;

    private bool _disposed;

    public DockerFileSystem(
        DockerFSOptions options,
        ILogger<DockerFileSystem> logger,
        ILogger<DockerCommandExecutor> executorLogger,
        ILogger<DockerFSCache> cacheLogger,
        IAuditLogger auditLogger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));

        _executor = new DockerCommandExecutor(
            options.ContainerName,
            options.CommandTimeout,
            executorLogger);

        _translator = new MountTranslator(options.Mounts);

        _pathValidator = new ContainerPathValidator(
            options.Mounts,
            _executor,
            options.ContainerName);

        _boundaryEnforcer = new MountBoundaryEnforcer(
            options.Mounts,
            _executor,
            options.ContainerName);

        _cache = new DockerFSCache(
            options.CacheTtl,
            options.CacheEnabled,
            cacheLogger);

        _logger.LogInformation(
            "DockerFileSystem initialized for container {Container} with {MountCount} mounts",
            options.ContainerName,
            options.Mounts.Count);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string path, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        var containerPath = ResolvePath(path);

        // Check cache first
        var cacheKey = $"content:{containerPath}";
        if (_cache.TryGet<string>(cacheKey, out var cached))
        {
            return cached!;
        }

        // Use boundary-safe read
        var content = await _boundaryEnforcer.ReadWithBoundaryCheckAsync(containerPath, ct);

        _auditLogger.Log("DockerFS.ReadFile", containerPath, _options.ContainerName);
        _cache.Set(cacheKey, content);

        return content;
    }

    /// <inheritdoc />
    public async Task WriteFileAsync(string path, string content, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        var containerPath = ResolvePath(path);

        // Check read-only
        if (_translator.IsReadOnly(containerPath))
        {
            throw new ReadOnlyMountException(
                $"Cannot write to read-only mount: {containerPath}");
        }

        // Use boundary-safe write
        await _boundaryEnforcer.WriteWithBoundaryCheckAsync(containerPath, content, ct);

        _auditLogger.Log("DockerFS.WriteFile", containerPath, _options.ContainerName);
        _cache.Invalidate(containerPath);
    }

    /// <inheritdoc />
    public async Task DeleteFileAsync(string path, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        var containerPath = ResolvePath(path);

        if (_translator.IsReadOnly(containerPath))
        {
            throw new ReadOnlyMountException(
                $"Cannot delete from read-only mount: {containerPath}");
        }

        await _boundaryEnforcer.DeleteWithBoundaryCheckAsync(containerPath, recursive: false, ct);

        _auditLogger.Log("DockerFS.DeleteFile", containerPath, _options.ContainerName);
        _cache.Invalidate(containerPath);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string path, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        var containerPath = ResolvePath(path);

        var cacheKey = $"exists:{containerPath}";
        if (_cache.TryGet<bool>(cacheKey, out var cached))
        {
            return cached;
        }

        var command = $"test -e {_executor.EscapeArgument(containerPath)} && echo 1 || echo 0";
        var result = await _executor.ExecAsync(command, ct);

        var exists = result.Output.Trim() == "1";
        _cache.Set(cacheKey, exists);

        return exists;
    }

    /// <inheritdoc />
    public async Task<FileMetadata> GetMetadataAsync(string path, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        var containerPath = ResolvePath(path);

        var cacheKey = $"metadata:{containerPath}";
        if (_cache.TryGet<FileMetadata>(cacheKey, out var cached))
        {
            return cached!;
        }

        // Use stat to get file info
        var command = $"stat -c '%s %Y %F' {_executor.EscapeArgument(containerPath)}";
        var result = await _executor.ExecAsync(command, ct);

        if (result.ExitCode != 0)
        {
            throw new FileNotFoundException($"File not found: {containerPath}");
        }

        var parts = result.Output.Trim().Split(' ', 3);
        var size = long.Parse(parts[0]);
        var modifiedUnix = long.Parse(parts[1]);
        var type = parts[2];

        var metadata = new FileMetadata
        {
            Path = containerPath,
            Size = size,
            ModifiedTime = DateTimeOffset.FromUnixTimeSeconds(modifiedUnix),
            IsDirectory = type.Contains("directory", StringComparison.OrdinalIgnoreCase)
        };

        _cache.Set(cacheKey, metadata);
        return metadata;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<FileMetadata> EnumerateFilesAsync(
        string directory,
        string? pattern = null,
        bool recursive = true,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ThrowIfDisposed();
        var containerPath = ResolvePath(directory);

        var cacheKey = $"list:{containerPath}:{pattern}:{recursive}";
        if (_cache.TryGet<IReadOnlyList<FileMetadata>>(cacheKey, out var cached))
        {
            foreach (var item in cached!)
            {
                yield return item;
            }
            yield break;
        }

        var results = new List<FileMetadata>();
        var depthArg = recursive ? "" : "-maxdepth 1";
        var nameArg = string.IsNullOrEmpty(pattern) ? "" : $"-name {_executor.EscapeArgument(pattern)}";

        var command = $"find {_executor.EscapeArgument(containerPath)} {depthArg} {nameArg} -type f -printf '%p\\t%s\\t%T@\\n'";
        var result = await _executor.ExecAsync(command, ct);

        if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
        {
            foreach (var line in result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split('\t');
                if (parts.Length >= 3)
                {
                    var metadata = new FileMetadata
                    {
                        Path = parts[0],
                        Size = long.TryParse(parts[1], out var s) ? s : 0,
                        ModifiedTime = double.TryParse(parts[2], out var t)
                            ? DateTimeOffset.FromUnixTimeSeconds((long)t)
                            : DateTimeOffset.MinValue,
                        IsDirectory = false
                    };
                    results.Add(metadata);
                    yield return metadata;
                }
            }
        }

        _cache.Set(cacheKey, results);
    }

    /// <summary>
    /// Clears the internal cache.
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    public double GetCacheHitRatio() => _cache.HitRatio;

    private string ResolvePath(string path)
    {
        // If already absolute container path, validate and return
        if (path.StartsWith('/'))
        {
            return _pathValidator.ValidatePath(path);
        }

        // Otherwise treat as relative to first mount
        var containerPath = $"{_options.Mounts[0].ContainerPath.TrimEnd('/')}/{path}";
        return _pathValidator.ValidatePath(containerPath);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(DockerFileSystem));
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cache.Dispose();
    }
}
```

---

### Step 6: Create DI Registration Extension

**File:** `src/AgenticCoder.Infrastructure/FileSystem/Docker/ServiceCollectionExtensions.cs`

```csharp
using System;
using AgenticCoder.Domain.FileSystem;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgenticCoder.Infrastructure.FileSystem.Docker;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers DockerFileSystem as the IRepoFS implementation.
    /// </summary>
    public static IServiceCollection AddDockerFileSystem(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection("Repo:Docker")
            .Get<DockerFSOptions>();

        if (options == null)
        {
            throw new InvalidOperationException(
                "Docker file system configuration not found. " +
                "Ensure 'Repo:Docker' section exists in configuration.");
        }

        services.AddSingleton(options);

        services.AddSingleton<IRepoFS>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<DockerFileSystem>>();
            var executorLogger = sp.GetRequiredService<ILogger<DockerCommandExecutor>>();
            var cacheLogger = sp.GetRequiredService<ILogger<DockerFSCache>>();
            var auditLogger = sp.GetRequiredService<IAuditLogger>();

            return new DockerFileSystem(options, logger, executorLogger, cacheLogger, auditLogger);
        });

        return services;
    }
}
```

---

### Error Codes Reference

| Code | Exception Type | Meaning |
|------|---------------|---------|
| ACODE-DFS-001 | ContainerNotFoundException | Container does not exist |
| ACODE-DFS-002 | ContainerNotRunningException | Container exists but is not running |
| ACODE-DFS-003 | FileNotFoundException | File not found in container |
| ACODE-DFS-004 | AccessDeniedException | Permission denied in container |
| ACODE-DFS-005 | TimeoutException | Command exceeded timeout |
| ACODE-DFS-006 | MountNotFoundException | Path outside configured mounts |
| ACODE-DFS-007 | PathTraversalException | Path traversal attack blocked |
| ACODE-DFS-008 | SymlinkEscapeException | Symlink escapes mount boundary |
| ACODE-DFS-009 | ReadOnlyMountException | Write to read-only mount |
| ACODE-DFS-010 | MountBoundaryViolationException | Operation escapes mount boundary |

---

### Implementation Checklist

1. [ ] Create DockerFSOptions with validation attributes
2. [ ] Create DockerCommandSanitizer with shell escaping (from Security section)
3. [ ] Create ContainerNameValidator with format and safety checks
4. [ ] Create ContainerPathValidator with traversal prevention
5. [ ] Create DockerCommandExecutor with timeout and logging
6. [ ] Create DockerFSCache with TTL and metrics
7. [ ] Create MountTranslator with bidirectional translation
8. [ ] Create MountBoundaryEnforcer with TOCTOU-safe operations
9. [ ] Create SafeDockerCredentialHandler for secure content transfer
10. [ ] Create DockerFileSystem implementing IRepoFS
11. [ ] Create ServiceCollectionExtensions for DI registration
12. [ ] Write unit tests for all components (see Testing Requirements)
13. [ ] Write integration tests with real Docker container
14. [ ] Verify all 110 acceptance criteria pass

---

### Rollout Plan

1. **Phase 1: Foundation** - DockerFSOptions, DockerCommandSanitizer, ContainerNameValidator
2. **Phase 2: Security** - ContainerPathValidator, MountBoundaryEnforcer, SafeDockerCredentialHandler
3. **Phase 3: Execution** - DockerCommandExecutor with timeout handling
4. **Phase 4: Translation** - MountTranslator with bidirectional support
5. **Phase 5: Caching** - DockerFSCache with TTL and invalidation
6. **Phase 6: Main Implementation** - DockerFileSystem with all operations
7. **Phase 7: Integration** - DI registration, configuration binding
8. **Phase 8: Testing** - Unit tests, integration tests, manual verification

---

**End of Task 014.b Specification**