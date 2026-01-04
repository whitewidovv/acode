# Task 030: SSH Target

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 7 – Cloud Integration  
**Dependencies:** Task 029 (ComputeTarget Interface)  

---

## Description

Task 030 implements the SSH compute target. Remote Linux machines MUST be accessible via SSH. Commands MUST execute remotely. Files MUST transfer via SFTP.

SSH target enables burst computing on existing infrastructure. Users may have SSH access to powerful servers. These MUST be usable as compute targets.

This task provides the core SSH integration. Subtasks cover connection management, command execution, and file transfer.

### Business Value

SSH targets enable:
- Use existing hardware
- University/HPC access
- No cloud costs
- Private infrastructure

### Scope Boundaries

This task covers SSH target implementation. EC2 is in Task 031. Core interface is in Task 029.

### Integration Points

- Task 029: Implements IComputeTarget
- Task 031: EC2 uses SSH internally
- Task 027: Workers use SSH targets

### Mode Compliance

| Mode | SSH Behavior |
|------|--------------|
| local-only | BLOCKED |
| airgapped | BLOCKED |
| burst | ALLOWED |

MUST validate mode before connecting.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| SSH | Secure Shell protocol |
| SFTP | SSH File Transfer Protocol |
| SCP | Secure Copy Protocol |
| PTY | Pseudo-terminal |
| Bastion | Jump host |
| Agent | SSH key agent |

---

## Out of Scope

- SSH tunnel management
- X11 forwarding
- SOCKS proxy
- SSH certificate auth
- Custom SSH implementations

---

## Functional Requirements

### FR-001 to FR-020: SSH Target

- FR-001: `SshComputeTarget` MUST implement interface
- FR-002: Connection info MUST be configurable
- FR-003: Host MUST be required
- FR-004: Port MUST default to 22
- FR-005: Username MUST be required
- FR-006: Authentication MUST be flexible
- FR-007: Password auth MUST work
- FR-008: Key auth MUST work
- FR-009: Agent auth MUST work
- FR-010: Key file path MUST be configurable
- FR-011: Key passphrase MUST be supported
- FR-012: Host key verification MUST exist
- FR-013: Known hosts MUST be checked
- FR-014: Strict host checking MUST be optional
- FR-015: Connection timeout MUST be configurable
- FR-016: Default timeout: 30 seconds
- FR-017: Keep-alive MUST be enabled
- FR-018: Keep-alive interval: 15 seconds
- FR-019: Reconnection MUST be automatic
- FR-020: Max reconnects MUST be configurable

### FR-021 to FR-040: Connection Management

- FR-021: Connection pool MUST exist
- FR-022: Pool size MUST be configurable
- FR-023: Default pool: 4 connections
- FR-024: Connection reuse MUST work
- FR-025: Idle connections MUST timeout
- FR-026: Idle timeout: 5 minutes
- FR-027: Connection health check MUST exist
- FR-028: Health check: every 30 seconds
- FR-029: Failed connections MUST be replaced
- FR-030: Bastion/jump host MUST be supported
- FR-031: ProxyCommand equivalent MUST work
- FR-032: Multi-hop MUST work
- FR-033: Connection limits MUST be enforced
- FR-034: Concurrent command limit MUST exist
- FR-035: Default concurrent: 10
- FR-036: Command queuing MUST work
- FR-037: Priority queuing MUST be optional
- FR-038: Connection state MUST be tracked
- FR-039: Metrics MUST be emitted
- FR-040: Diagnostics MUST be available

### FR-041 to FR-055: Workspace Management

- FR-041: Remote workspace path MUST be configurable
- FR-042: Default: /tmp/acode-{session-id}
- FR-043: Workspace MUST be created on prepare
- FR-044: Permissions MUST be set correctly
- FR-045: Cleanup on teardown MUST work
- FR-046: Multiple workspaces MUST be isolated
- FR-047: Workspace quota MUST be checked
- FR-048: Disk space MUST be verified
- FR-049: Environment MUST be configurable
- FR-050: PATH MUST be settable
- FR-051: Working directory MUST be settable
- FR-052: Shell MUST be detectable
- FR-053: Common shells MUST be supported
- FR-054: Shell: bash, sh, zsh
- FR-055: Shell-specific escaping MUST work

---

## Non-Functional Requirements

- NFR-001: Connection in <5 seconds
- NFR-002: Command latency <100ms overhead
- NFR-003: 100 concurrent sessions
- NFR-004: Reconnect in <2 seconds
- NFR-005: No connection leaks
- NFR-006: Secure by default
- NFR-007: Structured logging
- NFR-008: Metrics on connections
- NFR-009: Cross-platform client
- NFR-010: Handle network interruption

---

## User Manual Documentation

### Configuration

```yaml
sshTarget:
  host: build-server.example.com
  port: 22
  username: builder
  keyFile: ~/.ssh/id_ed25519
  keyPassphrase: ${SSH_KEY_PASSPHRASE}
  strictHostKeyChecking: true
  connectionPoolSize: 4
  keepAliveInterval: 15
  bastion:
    host: bastion.example.com
    port: 22
    username: jump
    keyFile: ~/.ssh/bastion_key
```

### CLI Usage

```bash
# Add SSH target
acode target add ssh \
  --host build-server.example.com \
  --user builder \
  --key ~/.ssh/id_ed25519

# Test connection
acode target test ssh://builder@build-server.example.com

# List targets
acode target list
```

### Troubleshooting

| Issue | Resolution |
|-------|------------|
| Connection refused | Check host/port |
| Auth failed | Check credentials |
| Host key mismatch | Update known_hosts |
| Timeout | Check network/firewall |

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: SSH connection works
- [ ] AC-002: Key auth works
- [ ] AC-003: Password auth works
- [ ] AC-004: Agent auth works
- [ ] AC-005: Bastion works
- [ ] AC-006: Connection pool works
- [ ] AC-007: Reconnection works
- [ ] AC-008: Mode compliance enforced
- [ ] AC-009: Workspace created
- [ ] AC-010: Cleanup works

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Config parsing
- [ ] UT-002: Auth method selection
- [ ] UT-003: Mode validation
- [ ] UT-004: Connection pooling logic

### Integration Tests

- [ ] IT-001: Real SSH connection
- [ ] IT-002: Command execution
- [ ] IT-003: File transfer
- [ ] IT-004: Bastion hop

---

## Implementation Prompt

You are implementing the SSH compute target for the Acode project. This enables remote command execution on Linux servers via SSH. Follow Clean Architecture and TDD.

### Part 1: File Structure and Domain Models

#### File Structure

```
src/Acode.Domain/
├── Compute/
│   └── Ssh/
│       ├── SshAuthMethod.cs
│       ├── SshConnectionState.cs
│       ├── SshHostKey.cs
│       └── Events/
│           ├── SshConnectedEvent.cs
│           ├── SshDisconnectedEvent.cs
│           └── SshReconnectingEvent.cs

src/Acode.Application/
├── Compute/
│   └── Ssh/
│       ├── ISshConnectionPool.cs
│       ├── ISshClient.cs
│       ├── ISshSessionFactory.cs
│       └── SshConfiguration.cs

src/Acode.Infrastructure/
├── Compute/
│   └── Ssh/
│       ├── SshComputeTarget.cs
│       ├── SshConnectionPool.cs
│       ├── SshClientWrapper.cs
│       ├── SshSessionFactory.cs
│       ├── Authentication/
│       │   ├── SshKeyAuthenticator.cs
│       │   ├── SshPasswordAuthenticator.cs
│       │   └── SshAgentAuthenticator.cs
│       ├── HostKey/
│       │   ├── KnownHostsManager.cs
│       │   └── HostKeyVerifier.cs
│       └── Bastion/
│           └── BastionTunnel.cs

tests/Acode.Infrastructure.Tests/
├── Compute/
│   └── Ssh/
│       ├── SshComputeTargetTests.cs
│       ├── SshConnectionPoolTests.cs
│       ├── SshClientWrapperTests.cs
│       └── Authentication/
│           └── SshAuthenticatorTests.cs

tests/Acode.Integration.Tests/
├── Compute/
│   └── Ssh/
│       ├── RealSshConnectionTests.cs
│       └── BastionTunnelTests.cs
```

#### Domain Models

```csharp
// src/Acode.Domain/Compute/Ssh/SshAuthMethod.cs
namespace Acode.Domain.Compute.Ssh;

public abstract record SshAuthMethod;

public sealed record SshKeyAuth(
    string KeyFilePath,
    string? Passphrase = null) : SshAuthMethod;

public sealed record SshPasswordAuth(string Password) : SshAuthMethod;

public sealed record SshAgentAuth : SshAuthMethod;

public sealed record SshCertificateAuth(
    string CertificatePath,
    string PrivateKeyPath) : SshAuthMethod;

// src/Acode.Domain/Compute/Ssh/SshConnectionState.cs
namespace Acode.Domain.Compute.Ssh;

public enum SshConnectionState
{
    Disconnected = 0,
    Connecting = 1,
    Connected = 2,
    Authenticating = 3,
    Authenticated = 4,
    Reconnecting = 5,
    Failed = 6
}

// src/Acode.Domain/Compute/Ssh/SshHostKey.cs
namespace Acode.Domain.Compute.Ssh;

public sealed record SshHostKey(
    string Host,
    int Port,
    string KeyType,
    string Fingerprint,
    byte[] PublicKey);

// src/Acode.Domain/Compute/Ssh/Events/SshConnectedEvent.cs
namespace Acode.Domain.Compute.Ssh.Events;

public sealed record SshConnectedEvent(
    ComputeTargetId TargetId,
    string Host,
    int Port,
    DateTimeOffset Timestamp) : IDomainEvent;

public sealed record SshDisconnectedEvent(
    ComputeTargetId TargetId,
    string Reason,
    DateTimeOffset Timestamp) : IDomainEvent;

public sealed record SshReconnectingEvent(
    ComputeTargetId TargetId,
    int AttemptNumber,
    DateTimeOffset Timestamp) : IDomainEvent;
```

**End of Task 030 Specification - Part 1/4**

### Part 2: Application Interfaces and Configuration

```csharp
// src/Acode.Application/Compute/Ssh/SshConfiguration.cs
namespace Acode.Application.Compute.Ssh;

public sealed record SshConfiguration
{
    public required string Host { get; init; }
    public int Port { get; init; } = 22;
    public required string Username { get; init; }
    public required SshAuthMethod Auth { get; init; }
    public string WorkspacePath { get; init; } = "/tmp/acode-{session}";
    public SshBastionConfig? Bastion { get; init; }
    public bool StrictHostKeyChecking { get; init; } = true;
    public int ConnectionPoolSize { get; init; } = 4;
    public TimeSpan ConnectionTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan KeepAliveInterval { get; init; } = TimeSpan.FromSeconds(15);
    public TimeSpan IdleTimeout { get; init; } = TimeSpan.FromMinutes(5);
    public int MaxReconnectAttempts { get; init; } = 3;
    public int MaxConcurrentCommands { get; init; } = 10;
}

public sealed record SshBastionConfig(
    string Host,
    int Port,
    string Username,
    SshAuthMethod Auth);

// src/Acode.Application/Compute/Ssh/ISshConnectionPool.cs
namespace Acode.Application.Compute.Ssh;

public interface ISshConnectionPool : IAsyncDisposable
{
    Task<ISshSession> AcquireAsync(CancellationToken ct = default);
    void Release(ISshSession session);
    int AvailableConnections { get; }
    int TotalConnections { get; }
    int ActiveConnections { get; }
    SshConnectionState PoolState { get; }
    Task WarmupAsync(int count, CancellationToken ct = default);
}

// src/Acode.Application/Compute/Ssh/ISshClient.cs
namespace Acode.Application.Compute.Ssh;

public interface ISshClient : IAsyncDisposable
{
    Task ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync();
    bool IsConnected { get; }
    SshConnectionState State { get; }
    
    Task<SshCommandResult> ExecuteAsync(
        string command,
        TimeSpan? timeout = null,
        CancellationToken ct = default);
    
    Task UploadAsync(
        string localPath,
        string remotePath,
        IProgress<long>? progress = null,
        CancellationToken ct = default);
    
    Task DownloadAsync(
        string remotePath,
        string localPath,
        IProgress<long>? progress = null,
        CancellationToken ct = default);
    
    Task<bool> FileExistsAsync(string remotePath, CancellationToken ct = default);
    Task CreateDirectoryAsync(string remotePath, CancellationToken ct = default);
    Task DeleteAsync(string remotePath, bool recursive = false, CancellationToken ct = default);
}

public sealed record SshCommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    TimeSpan Duration,
    bool TimedOut);

// src/Acode.Application/Compute/Ssh/ISshSessionFactory.cs
namespace Acode.Application.Compute.Ssh;

public interface ISshSessionFactory
{
    Task<ISshClient> CreateClientAsync(
        SshConfiguration config,
        CancellationToken ct = default);
    
    Task<ISshClient> CreateTunneledClientAsync(
        SshConfiguration config,
        SshBastionConfig bastion,
        CancellationToken ct = default);
}

public interface ISshSession : IAsyncDisposable
{
    string SessionId { get; }
    ISshClient Client { get; }
    DateTimeOffset ConnectedAt { get; }
    DateTimeOffset LastUsedAt { get; }
    bool IsHealthy { get; }
    void MarkUsed();
}
```

**End of Task 030 Specification - Part 2/4**

### Part 3: Infrastructure Implementation

```csharp
// src/Acode.Infrastructure/Compute/Ssh/SshComputeTarget.cs
namespace Acode.Infrastructure.Compute.Ssh;

public sealed class SshComputeTarget : IComputeTarget
{
    private readonly SshConfiguration _config;
    private readonly ISshConnectionPool _pool;
    private readonly ITargetStateManager _stateManager;
    private readonly IEventPublisher _events;
    private readonly ILogger<SshComputeTarget> _logger;
    
    public ComputeTargetId Id { get; }
    public ComputeTargetType Type => ComputeTargetType.SSH;
    public ComputeTargetState State => _stateManager.GetState(Id);
    public TargetMetadata Metadata { get; }
    
    public event EventHandler<TargetStateChangedEvent>? StateChanged;
    
    public SshComputeTarget(
        ComputeTargetId id,
        SshConfiguration config,
        ISshConnectionPool pool,
        ITargetStateManager stateManager,
        IEventPublisher events,
        ILogger<SshComputeTarget> logger)
    {
        Id = id;
        _config = config;
        _pool = pool;
        _stateManager = stateManager;
        _events = events;
        _logger = logger;
        
        Metadata = new TargetMetadata
        {
            CreatedAt = DateTimeOffset.UtcNow,
            Host = config.Host
        };
        Metadata.Set("port", config.Port);
        Metadata.Set("username", config.Username);
    }
    
    public async Task PrepareWorkspaceAsync(
        WorkspaceConfig config,
        IProgress<PreparationProgress>? progress = null,
        CancellationToken ct = default)
    {
        await TransitionStateAsync(ComputeTargetState.Preparing, ct);
        
        await using var session = await _pool.AcquireAsync(ct);
        
        try
        {
            progress?.Report(new PreparationProgress
            {
                Phase = PreparationPhase.Creating,
                PercentComplete = 10,
                Message = "Creating remote workspace"
            });
            
            // Create workspace directory
            var workspacePath = ResolveWorkspacePath(config.WorktreePath);
            await session.Client.CreateDirectoryAsync(workspacePath, ct);
            
            progress?.Report(new PreparationProgress
            {
                Phase = PreparationPhase.Syncing,
                PercentComplete = 30,
                Message = "Syncing source code via rsync"
            });
            
            // Sync using rsync over SSH
            await SyncWorkspaceAsync(session.Client, config, progress, ct);
            
            progress?.Report(new PreparationProgress
            {
                Phase = PreparationPhase.InstallingDependencies,
                PercentComplete = 70,
                Message = "Installing dependencies"
            });
            
            // Install dependencies
            await InstallDependenciesAsync(session.Client, workspacePath, config, ct);
            
            progress?.Report(new PreparationProgress
            {
                Phase = PreparationPhase.Completed,
                PercentComplete = 100,
                Message = "Workspace ready"
            });
            
            await TransitionStateAsync(ComputeTargetState.Ready, ct);
            Metadata.MarkReady();
            Metadata.Set("WorkspacePath", workspacePath);
        }
        catch
        {
            await TransitionStateAsync(ComputeTargetState.Failed, ct);
            throw;
        }
    }
    
    public async Task<ExecutionResult> ExecuteAsync(
        ExecutionCommand command,
        CancellationToken ct = default)
    {
        if (State != ComputeTargetState.Ready)
            throw new InvalidOperationException($"Target not ready: {State}");
        
        await TransitionStateAsync(ComputeTargetState.Busy, ct);
        
        await using var session = await _pool.AcquireAsync(ct);
        
        try
        {
            var workDir = Metadata.Get<string>("WorkspacePath") ?? _config.WorkspacePath;
            var fullCommand = BuildRemoteCommand(command, workDir);
            
            var startedAt = DateTimeOffset.UtcNow;
            var result = await session.Client.ExecuteAsync(fullCommand, command.Timeout, ct);
            
            await TransitionStateAsync(ComputeTargetState.Ready, ct);
            
            return new ExecutionResult
            {
                ExitCode = result.ExitCode,
                StandardOutput = result.StandardOutput,
                StandardError = result.StandardError,
                Duration = result.Duration,
                TimedOut = result.TimedOut,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow
            };
        }
        catch
        {
            await TransitionStateAsync(ComputeTargetState.Ready, ct);
            throw;
        }
    }
    
    public async Task<TransferResult> UploadAsync(
        ArtifactTransferConfig config,
        CancellationToken ct = default)
    {
        await using var session = await _pool.AcquireAsync(ct);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await session.Client.UploadAsync(config.LocalPath, config.RemotePath, null, ct);
            stopwatch.Stop();
            
            var fileInfo = new FileInfo(config.LocalPath);
            return new TransferResult
            {
                Success = true,
                BytesTransferred = fileInfo.Length,
                Duration = stopwatch.Elapsed,
                FilesTransferred = 1
            };
        }
        catch (Exception ex)
        {
            return new TransferResult
            {
                Success = false,
                BytesTransferred = 0,
                Duration = stopwatch.Elapsed,
                ErrorMessage = ex.Message
            };
        }
    }
    
    public async Task<TransferResult> DownloadAsync(
        ArtifactTransferConfig config,
        CancellationToken ct = default)
    {
        await using var session = await _pool.AcquireAsync(ct);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await session.Client.DownloadAsync(config.RemotePath, config.LocalPath, null, ct);
            stopwatch.Stop();
            
            var fileInfo = new FileInfo(config.LocalPath);
            return new TransferResult
            {
                Success = true,
                BytesTransferred = fileInfo.Length,
                Duration = stopwatch.Elapsed,
                FilesTransferred = 1
            };
        }
        catch (Exception ex)
        {
            return new TransferResult
            {
                Success = false,
                BytesTransferred = 0,
                Duration = stopwatch.Elapsed,
                ErrorMessage = ex.Message
            };
        }
    }
    
    public async Task TeardownAsync(CancellationToken ct = default)
    {
        await TransitionStateAsync(ComputeTargetState.Tearingdown, ct);
        
        try
        {
            await using var session = await _pool.AcquireAsync(ct);
            
            var workspacePath = Metadata.Get<string>("WorkspacePath");
            if (workspacePath != null)
            {
                await session.Client.DeleteAsync(workspacePath, recursive: true, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup remote workspace");
        }
        
        await _pool.DisposeAsync();
        await TransitionStateAsync(ComputeTargetState.Terminated, ct);
        Metadata.MarkTerminated();
    }
    
    public async ValueTask DisposeAsync()
    {
        if (State != ComputeTargetState.Terminated)
            await TeardownAsync();
    }
    
    private string BuildRemoteCommand(ExecutionCommand cmd, string workDir)
    {
        var envVars = cmd.Environment != null
            ? string.Join(" ", cmd.Environment.Select(kv => $"{kv.Key}={EscapeShell(kv.Value)}"))
            : "";
        return $"cd {EscapeShell(workDir)} && {envVars} {cmd.Command}";
    }
    
    private static string EscapeShell(string value) =>
        "'" + value.Replace("'", "'\\''") + "'";
    
    private string ResolveWorkspacePath(string template) =>
        template.Replace("{session}", Id.Value[..8]);
}
```

**End of Task 030 Specification - Part 3/4**

### Part 4: Connection Pool, Host Key Verification, and Rollout

```csharp
// src/Acode.Infrastructure/Compute/Ssh/SshConnectionPool.cs
namespace Acode.Infrastructure.Compute.Ssh;

public sealed class SshConnectionPool : ISshConnectionPool
{
    private readonly SshConfiguration _config;
    private readonly ISshSessionFactory _sessionFactory;
    private readonly ConcurrentBag<SshSession> _available = new();
    private readonly ConcurrentDictionary<string, SshSession> _inUse = new();
    private readonly SemaphoreSlim _semaphore;
    private readonly Timer _healthCheckTimer;
    private readonly ILogger<SshConnectionPool> _logger;
    private volatile SshConnectionState _poolState = SshConnectionState.Disconnected;
    
    public int AvailableConnections => _available.Count;
    public int TotalConnections => _available.Count + _inUse.Count;
    public int ActiveConnections => _inUse.Count;
    public SshConnectionState PoolState => _poolState;
    
    public SshConnectionPool(
        SshConfiguration config,
        ISshSessionFactory sessionFactory,
        ILogger<SshConnectionPool> logger)
    {
        _config = config;
        _sessionFactory = sessionFactory;
        _logger = logger;
        _semaphore = new SemaphoreSlim(config.ConnectionPoolSize, config.ConnectionPoolSize);
        
        _healthCheckTimer = new Timer(
            HealthCheck,
            null,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30));
    }
    
    public async Task<ISshSession> AcquireAsync(CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        
        try
        {
            // Try to get an available connection
            if (_available.TryTake(out var session) && session.IsHealthy)
            {
                session.MarkUsed();
                _inUse[session.SessionId] = session;
                return session;
            }
            
            // Create new connection
            var client = _config.Bastion != null
                ? await _sessionFactory.CreateTunneledClientAsync(_config, _config.Bastion, ct)
                : await _sessionFactory.CreateClientAsync(_config, ct);
            
            await client.ConnectAsync(ct);
            
            var newSession = new SshSession(Ulid.NewUlid().ToString(), client);
            _inUse[newSession.SessionId] = newSession;
            
            _poolState = SshConnectionState.Connected;
            _logger.LogDebug("Created new SSH session {SessionId}", newSession.SessionId);
            
            return newSession;
        }
        catch
        {
            _semaphore.Release();
            throw;
        }
    }
    
    public void Release(ISshSession session)
    {
        if (session is SshSession sshSession && _inUse.TryRemove(sshSession.SessionId, out _))
        {
            if (sshSession.IsHealthy && DateTimeOffset.UtcNow - sshSession.LastUsedAt < _config.IdleTimeout)
            {
                _available.Add(sshSession);
            }
            else
            {
                _ = sshSession.DisposeAsync();
            }
            
            _semaphore.Release();
        }
    }
    
    public async Task WarmupAsync(int count, CancellationToken ct = default)
    {
        var tasks = Enumerable.Range(0, Math.Min(count, _config.ConnectionPoolSize))
            .Select(async _ =>
            {
                var session = await AcquireAsync(ct);
                Release(session);
            });
        
        await Task.WhenAll(tasks);
        _logger.LogInformation("Warmed up {Count} SSH connections", count);
    }
    
    private void HealthCheck(object? state)
    {
        var toRemove = new List<SshSession>();
        
        while (_available.TryTake(out var session))
        {
            if (!session.IsHealthy || DateTimeOffset.UtcNow - session.LastUsedAt > _config.IdleTimeout)
            {
                toRemove.Add(session);
            }
            else
            {
                _available.Add(session);
                break;
            }
        }
        
        foreach (var session in toRemove)
        {
            _ = session.DisposeAsync();
            _logger.LogDebug("Removed unhealthy session {SessionId}", session.SessionId);
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        await _healthCheckTimer.DisposeAsync();
        
        while (_available.TryTake(out var session))
            await session.DisposeAsync();
        
        foreach (var session in _inUse.Values)
            await session.DisposeAsync();
        
        _inUse.Clear();
        _semaphore.Dispose();
        _poolState = SshConnectionState.Disconnected;
    }
}

// src/Acode.Infrastructure/Compute/Ssh/HostKey/KnownHostsManager.cs
namespace Acode.Infrastructure.Compute.Ssh.HostKey;

public sealed class KnownHostsManager
{
    private readonly string _knownHostsPath;
    private readonly Dictionary<string, SshHostKey> _knownHosts = new();
    private readonly ILogger<KnownHostsManager> _logger;
    
    public KnownHostsManager(IOptions<SshOptions> options, ILogger<KnownHostsManager> logger)
    {
        _knownHostsPath = options.Value.KnownHostsPath 
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh", "known_hosts");
        _logger = logger;
        LoadKnownHosts();
    }
    
    public HostKeyVerificationResult Verify(SshHostKey hostKey)
    {
        var key = $"{hostKey.Host}:{hostKey.Port}";
        
        if (!_knownHosts.TryGetValue(key, out var known))
            return HostKeyVerificationResult.Unknown;
        
        if (known.Fingerprint == hostKey.Fingerprint)
            return HostKeyVerificationResult.Trusted;
        
        _logger.LogWarning(
            "Host key mismatch for {Host}:{Port}. Expected {Expected}, got {Actual}",
            hostKey.Host, hostKey.Port, known.Fingerprint, hostKey.Fingerprint);
        
        return HostKeyVerificationResult.Changed;
    }
    
    public void AddHost(SshHostKey hostKey)
    {
        var key = $"{hostKey.Host}:{hostKey.Port}";
        _knownHosts[key] = hostKey;
        SaveKnownHosts();
    }
    
    private void LoadKnownHosts()
    {
        if (!File.Exists(_knownHostsPath)) return;
        // Parse OpenSSH known_hosts format
    }
    
    private void SaveKnownHosts()
    {
        // Write OpenSSH known_hosts format
    }
}

public enum HostKeyVerificationResult { Trusted, Unknown, Changed }

// src/Acode.Infrastructure/Compute/Ssh/Bastion/BastionTunnel.cs
namespace Acode.Infrastructure.Compute.Ssh.Bastion;

public sealed class BastionTunnel : IAsyncDisposable
{
    private readonly ISshClient _bastionClient;
    private readonly int _localPort;
    private readonly string _targetHost;
    private readonly int _targetPort;
    
    public int LocalPort => _localPort;
    
    public BastionTunnel(
        ISshClient bastionClient,
        int localPort,
        string targetHost,
        int targetPort)
    {
        _bastionClient = bastionClient;
        _localPort = localPort;
        _targetHost = targetHost;
        _targetPort = targetPort;
    }
    
    public async Task StartAsync(CancellationToken ct = default)
    {
        // Set up SSH port forwarding through bastion
        await _bastionClient.ConnectAsync(ct);
        // Forward local port to target through bastion
    }
    
    public async ValueTask DisposeAsync()
    {
        await _bastionClient.DisconnectAsync();
        await _bastionClient.DisposeAsync();
    }
}
```

---

## Implementation Checklist

- [ ] Create SshAuthMethod hierarchy (key, password, agent)
- [ ] Define SshConnectionState and SshHostKey records
- [ ] Implement SshConfiguration with all connection options
- [ ] Create ISshConnectionPool interface
- [ ] Implement SshConnectionPool with health checks
- [ ] Build SshComputeTarget implementing IComputeTarget
- [ ] Implement SshClientWrapper using SSH.NET library
- [ ] Create KnownHostsManager for host key verification
- [ ] Build BastionTunnel for jump host support
- [ ] Write unit tests for all components (TDD)
- [ ] Write integration tests with real SSH servers
- [ ] Test connection pooling behavior
- [ ] Test reconnection on failure
- [ ] Test bastion/jump host scenarios
- [ ] Verify mode compliance blocking

---

## Rollout Plan

1. **Phase 1**: Domain models (auth methods, connection state)
2. **Phase 2**: Application interfaces (pool, client, factory)
3. **Phase 3**: SshClientWrapper using SSH.NET
4. **Phase 4**: SshConnectionPool with health checks
5. **Phase 5**: SshComputeTarget implementation
6. **Phase 6**: KnownHostsManager for security
7. **Phase 7**: BastionTunnel for multi-hop
8. **Phase 8**: Integration testing

---

**End of Task 030 Specification**