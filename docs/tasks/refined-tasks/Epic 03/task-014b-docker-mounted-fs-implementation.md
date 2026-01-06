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
| Docker FS | Docker-mounted file system |
| Bind Mount | Host path in container |
| Volume Mount | Docker volume in container |
| docker exec | Execute in container |
| Container Path | Path inside container |
| Host Path | Path on host machine |
| Mount Point | Where mounted |
| Latency | Operation delay |
| Caching | Store for reuse |
| Invalidation | Cache clearing |
| TTL | Time to live |
| Container ID | Container identifier |
| Docker API | Docker interface |
| Shell Escape | Safe command building |
| Exit Code | Command result |

---

## Out of Scope

The following items are explicitly excluded from Task 014.b:

- **Docker API directly** - Uses docker exec
- **Container management** - No start/stop
- **Image operations** - No build/pull
- **Network operations** - Files only
- **Docker Compose** - Single container
- **Kubernetes** - Docker only
- **Remote Docker** - Local daemon only
- **Docker in Docker** - Not supported

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

---

## User Manual Documentation

### Overview

Docker FS allows acode to work with files inside Docker containers. Use this when your project runs in containers and you want the agent to modify files there.

### Configuration

```yaml
# .agent/config.yml
repo:
  fs_type: docker
  
  docker:
    # Container name or ID
    container: my-app-container
    
    # Mount mappings (host -> container)
    mounts:
      - host: /home/user/project
        container: /app
        
    # Cache settings
    cache:
      enabled: true
      ttl_seconds: 60
      
    # Timeout for docker exec
    timeout_seconds: 30
```

### Usage Examples

```csharp
// Create Docker file system
var fs = new DockerFileSystem(new DockerFSOptions
{
    ContainerName = "my-app-container",
    Mounts = new[]
    {
        new MountMapping("/home/user/project", "/app")
    }
});

// Read file from container
var content = await fs.ReadFileAsync("src/main.py");

// Write file to container
await fs.WriteFileAsync("config.json", jsonContent);
```

### Path Translation

When you specify paths, use the container paths:

```csharp
// This reads /app/src/main.py inside the container
var content = await fs.ReadFileAsync("src/main.py");
```

The mount configuration translates:
- Root path: `/app` (container)
- Relative `src/main.py` becomes `/app/src/main.py`

### Caching

Docker operations are slow. Caching helps:

```yaml
docker:
  cache:
    enabled: true      # Enable caching
    ttl_seconds: 60    # Cache for 60 seconds
```

Cache is invalidated on writes. You can also manually clear:

```csharp
fs.ClearCache();
```

### Troubleshooting

#### Container Not Found

**Problem:** Cannot connect to container

**Solutions:**
1. Verify container name: `docker ps`
2. Check container is running
3. Verify name matches config

#### Mount Not Accessible

**Problem:** Cannot access mounted path

**Solutions:**
1. Verify mount configuration
2. Check container has mount
3. Verify path inside container

#### Permission Denied

**Problem:** Cannot read/write in container

**Solutions:**
1. Check file permissions in container
2. Verify user running docker exec
3. Consider running as root (for testing)

#### Timeout

**Problem:** Operations take too long

**Solutions:**
1. Increase timeout_seconds
2. Check container load
3. Verify Docker daemon health

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

1. **Validate mount points** - Verify expected paths exist in container at startup
2. **Handle permission differences** - Container UID/GID may differ from host
3. **Monitor mount health** - Detect when bind mounts become unavailable
4. **Use absolute paths in container** - Avoid relative path confusion

### Performance Considerations

5. **Minimize cross-boundary I/O** - Batch operations to reduce overhead
6. **Cache file metadata** - Reduce stat calls across container boundary
7. **Use appropriate timeout** - Container I/O may be slower than native
8. **Consider async polling** - FileSystemWatcher may not work across mounts

### Security

9. **Enforce read-only when possible** - Mount volumes as ro: when writes not needed
10. **Validate all paths** - Double-check paths don't escape mount point
11. **No shell injection** - Never build shell commands from file paths
12. **Principle of least privilege** - Request only necessary container capabilities

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/FileSystem/Docker/
├── DockerFSReadTests.cs
│   ├── Should_Read_Text_File()
│   ├── Should_Read_Binary_File()
│   ├── Should_Read_Large_File()
│   ├── Should_Handle_Missing_File()
│   ├── Should_Handle_Permission_Denied()
│   ├── Should_Handle_Container_Not_Running()
│   └── Should_Support_Cancellation()
│
├── DockerFSWriteTests.cs
│   ├── Should_Write_Text_File()
│   ├── Should_Write_Binary_File()
│   ├── Should_Write_Atomically()
│   ├── Should_Create_Parent_Directories()
│   ├── Should_Overwrite_Existing()
│   ├── Should_Handle_Write_Errors()
│   └── Should_Invalidate_Cache_On_Write()
│
├── DockerFSDeleteTests.cs
│   ├── Should_Delete_File()
│   ├── Should_Delete_Directory()
│   ├── Should_Delete_Recursive()
│   ├── Should_Handle_Missing_Gracefully()
│   └── Should_Handle_Permission_Denied()
│
├── DockerFSEnumerationTests.cs
│   ├── Should_List_Files()
│   ├── Should_List_Recursively()
│   ├── Should_Apply_Filter()
│   ├── Should_Handle_Large_Directory()
│   └── Should_Parse_Find_Output()
│
├── DockerFSMetadataTests.cs
│   ├── Should_Check_Exists()
│   ├── Should_Get_File_Size()
│   ├── Should_Get_Modified_Time()
│   ├── Should_Detect_File_Type()
│   └── Should_Parse_Stat_Output()
│
├── DockerCommandBuilderTests.cs
│   ├── Should_Escape_Single_Quotes()
│   ├── Should_Escape_Double_Quotes()
│   ├── Should_Escape_Spaces()
│   ├── Should_Escape_Special_Characters()
│   ├── Should_Escape_Newlines()
│   ├── Should_Build_Cat_Command()
│   ├── Should_Build_Find_Command()
│   ├── Should_Build_Stat_Command()
│   ├── Should_Build_Mkdir_Command()
│   └── Should_Build_Rm_Command()
│
├── DockerCommandExecutorTests.cs
│   ├── Should_Execute_Simple_Command()
│   ├── Should_Handle_Exit_Code_Zero()
│   ├── Should_Handle_Exit_Code_NonZero()
│   ├── Should_Handle_Timeout()
│   ├── Should_Handle_Large_Output()
│   └── Should_Support_Cancellation()
│
├── MountMappingTests.cs
│   ├── Should_Translate_Host_To_Container()
│   ├── Should_Translate_Container_To_Host()
│   ├── Should_Handle_Multiple_Mounts()
│   ├── Should_Find_Best_Mount_Match()
│   └── Should_Handle_Unmapped_Path()
│
├── DockerCacheTests.cs
│   ├── Should_Cache_Directory_Listing()
│   ├── Should_Cache_Existence_Check()
│   ├── Should_Return_Cached_Value()
│   ├── Should_Invalidate_On_Write()
│   ├── Should_Invalidate_On_Delete()
│   ├── Should_Expire_After_TTL()
│   ├── Should_Clear_All_Cache()
│   └── Should_Disable_Cache()
│
└── DockerSecurityTests.cs
    ├── Should_Prevent_Shell_Injection()
    ├── Should_Block_Path_Traversal()
    ├── Should_Enforce_Mount_Boundary()
    └── Should_Reject_Invalid_Container_Name()
```

### Integration Tests

```
Tests/Integration/FileSystem/Docker/
├── DockerFSIntegrationTests.cs
│   ├── Should_Work_With_Real_Container()
│   ├── Should_Handle_Large_Files()
│   ├── Should_Handle_Many_Small_Files()
│   ├── Should_Handle_Concurrent_Operations()
│   ├── Should_Survive_Container_Restart()
│   └── Should_Handle_Slow_Container()
│
└── DockerMountIntegrationTests.cs
    ├── Should_Work_With_Bind_Mount()
    ├── Should_Work_With_Volume_Mount()
    └── Should_Handle_Multiple_Mounts()
```

### E2E Tests

```
Tests/E2E/FileSystem/Docker/
├── DockerFSE2ETests.cs
│   ├── Should_Read_File_Via_Agent_Tool()
│   ├── Should_Write_File_Via_Agent_Tool()
│   ├── Should_List_Files_Via_Agent_Tool()
│   └── Should_Work_With_Containerized_Project()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Cached read | 5ms | 10ms |
| Uncached read | 100ms | 200ms |
| Write | 150ms | 300ms |
| List 1000 | 200ms | 500ms |

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