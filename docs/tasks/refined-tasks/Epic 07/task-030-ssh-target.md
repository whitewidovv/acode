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

| Component | Integration Type | Description |
|-----------|-----------------|-------------|
| Task 029 IComputeTarget | Implements | SshComputeTarget implements IComputeTarget |
| Task 031 EC2 Target | Composition | EC2 uses SSH internally for connection |
| Task 027 Workers | Consumer | Workers dispatch tasks to SSH targets |
| ISshConnectionPool | Component | Manages pooled SSH connections |
| ISshClient | Abstraction | Wrapper around SSH.NET library |
| SSH.NET | External Library | Underlying SSH implementation |
| Known Hosts File | Configuration | Host key verification storage |

### Mode Compliance

| Mode | SSH Behavior | Rationale |
|------|--------------|-----------|
| local-only | BLOCKED | No external connections allowed |
| airgapped | BLOCKED | No network access |
| burst | ALLOWED | Remote compute enabled |

MUST validate mode before connecting.

### Failure Modes

| Failure Type | Detection | Recovery | User Impact |
|--------------|-----------|----------|-------------|
| Connection refused | Socket exception | Retry or fail with clear message | Check host/port/firewall |
| Authentication failed | SSH exception | Report which auth method failed | Check credentials |
| Host key mismatch | Verification failure | Require explicit user action | Update known_hosts |
| Connection timeout | Timer expiration | Retry with backoff | Check network/firewall |
| Connection dropped | Read/write failure | Automatic reconnection | Brief interruption |
| Bastion unreachable | Connection failure | Fail with bastion-specific message | Check bastion config |
| Channel exhausted | SSH exception | Wait and retry | Temporary delay |
| Key file not found | File exception | Clear error with path | Fix key path |

---

## Assumptions

1. **SSH Server Available**: Remote host runs SSH daemon on configured port
2. **Network Reachability**: Host is reachable from client (or via bastion)
3. **Valid Credentials**: User has valid authentication credentials configured
4. **Shell Available**: Remote host has POSIX-compatible shell (bash/sh/zsh)
5. **Disk Access**: User has write access to workspace directory on remote
6. **Known Hosts**: Host key is in known_hosts or strict checking disabled
7. **Mode Burst**: Agent is running in burst mode when SSH is used
8. **Library Support**: SSH.NET or equivalent library available on all platforms

---

## Security Considerations

1. **Host Key Verification**: Always verify host keys to prevent MITM attacks
2. **Credential Storage**: Private keys stored with restricted permissions (0600)
3. **Passphrase Protection**: Key passphrases never logged or stored in plaintext
4. **Connection Encryption**: All traffic encrypted via SSH (AES-256 or better)
5. **No Password Logging**: Password authentication credentials never in logs
6. **Agent Forwarding**: SSH agent forwarding disabled by default
7. **Bastion Security**: Bastion connections use separate credentials
8. **Audit Trail**: All SSH connections logged with user, host, timestamp

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

### SSH Target Core (FR-030-01 to FR-030-20)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-030-01 | `SshComputeTarget` MUST implement `IComputeTarget` interface | Must Have |
| FR-030-02 | SSH connection info MUST be configurable via `SshTargetConfig` | Must Have |
| FR-030-03 | Host (hostname or IP) MUST be required | Must Have |
| FR-030-04 | Port MUST default to 22 if not specified | Must Have |
| FR-030-05 | Username MUST be required | Must Have |
| FR-030-06 | Authentication MUST support multiple methods | Must Have |
| FR-030-07 | Password authentication MUST be supported | Should Have |
| FR-030-08 | Private key authentication MUST be supported | Must Have |
| FR-030-09 | SSH agent authentication MUST be supported | Should Have |
| FR-030-10 | Key file path MUST be configurable | Must Have |
| FR-030-11 | Key passphrase MUST be supported (via secret) | Must Have |
| FR-030-12 | Host key verification MUST exist | Must Have |
| FR-030-13 | Known hosts file MUST be checked by default | Must Have |
| FR-030-14 | Strict host key checking MUST be configurable | Should Have |
| FR-030-15 | Connection timeout MUST be configurable (default: 30s) | Must Have |
| FR-030-16 | SSH keep-alive MUST be enabled by default | Should Have |
| FR-030-17 | Keep-alive interval MUST be configurable (default: 15s) | Should Have |
| FR-030-18 | Automatic reconnection on connection loss MUST be supported | Should Have |
| FR-030-19 | Maximum reconnection attempts MUST be configurable | Should Have |
| FR-030-20 | Mode MUST be validated before connection (burst only) | Must Have |

### Connection Management (FR-030-21 to FR-030-40)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-030-21 | Connection pool MUST be implemented | Should Have |
| FR-030-22 | Pool size MUST be configurable (default: 4) | Should Have |
| FR-030-23 | Connections MUST be reused across operations | Should Have |
| FR-030-24 | Idle connections MUST be closed after timeout | Should Have |
| FR-030-25 | Idle timeout MUST be configurable (default: 5 min) | Should Have |
| FR-030-26 | Connection health check MUST run periodically | Should Have |
| FR-030-27 | Health check interval MUST be configurable (default: 30s) | Should Have |
| FR-030-28 | Failed connections MUST be removed from pool | Must Have |
| FR-030-29 | Bastion/jump host MUST be supported | Should Have |
| FR-030-30 | Bastion requires separate auth configuration | Should Have |
| FR-030-31 | ProxyCommand equivalent MUST be supported | Could Have |
| FR-030-32 | Multi-hop SSH (chained bastions) MAY be supported | Could Have |
| FR-030-33 | Connection limits MUST be enforced | Should Have |
| FR-030-34 | Concurrent command limit MUST exist (default: 10) | Should Have |
| FR-030-35 | Commands exceeding limit MUST queue | Should Have |
| FR-030-36 | Priority queuing MAY be supported | Could Have |
| FR-030-37 | Connection state MUST be tracked and queryable | Must Have |
| FR-030-38 | Connection metrics MUST be emitted | Should Have |
| FR-030-39 | Connection diagnostics MUST be available | Should Have |
| FR-030-40 | Graceful connection shutdown MUST be supported | Must Have |

### Workspace Management (FR-030-41 to FR-030-55)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-030-41 | Remote workspace path MUST be configurable | Must Have |
| FR-030-42 | Default workspace: `/tmp/acode-{session-id}` | Should Have |
| FR-030-43 | Workspace MUST be created during PrepareAsync | Must Have |
| FR-030-44 | Workspace permissions MUST be set (0755) | Should Have |
| FR-030-45 | Workspace cleanup on teardown MUST remove all files | Must Have |
| FR-030-46 | Multiple concurrent workspaces MUST be isolated | Must Have |
| FR-030-47 | Disk space quota SHOULD be checked before prepare | Could Have |
| FR-030-48 | Available disk space MUST be verified | Should Have |
| FR-030-49 | Remote environment variables MUST be configurable | Should Have |
| FR-030-50 | PATH on remote MUST be settable | Should Have |
| FR-030-51 | Working directory MUST default to workspace | Must Have |
| FR-030-52 | Remote shell SHOULD be auto-detected | Should Have |
| FR-030-53 | bash, sh, zsh MUST be supported | Must Have |
| FR-030-54 | Shell-specific command escaping MUST work | Must Have |
| FR-030-55 | Remote OS detection SHOULD occur on connect | Should Have |

---

## Non-Functional Requirements

### Performance (NFR-030-01 to NFR-030-10)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-030-01 | Initial connection time | <5 seconds | Must Have |
| NFR-030-02 | Command execution overhead | <100ms latency | Should Have |
| NFR-030-03 | Concurrent sessions | 100 per host | Should Have |
| NFR-030-04 | Reconnection time | <2 seconds | Should Have |
| NFR-030-05 | Connection pool acquisition | <10ms | Should Have |
| NFR-030-06 | Keep-alive overhead | Negligible | Should Have |
| NFR-030-07 | Memory per connection | <5MB | Should Have |
| NFR-030-08 | File transfer throughput | >10MB/s on LAN | Should Have |
| NFR-030-09 | Channel multiplexing | Multiple commands per connection | Should Have |
| NFR-030-10 | Bastion hop latency | <500ms additional | Should Have |

### Reliability (NFR-030-11 to NFR-030-20)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-030-11 | No connection leaks | 0 leaked connections | Must Have |
| NFR-030-12 | Network interruption handling | Auto-reconnect | Should Have |
| NFR-030-13 | Connection health monitoring | Every 30 seconds | Should Have |
| NFR-030-14 | Graceful degradation | Queue on pool exhaustion | Should Have |
| NFR-030-15 | Timeout enforcement | All operations | Must Have |
| NFR-030-16 | Thread safety | Full | Must Have |
| NFR-030-17 | Error isolation | One failure doesn't affect others | Must Have |
| NFR-030-18 | Resource cleanup on exception | 100% | Must Have |
| NFR-030-19 | Cross-platform client | Windows, macOS, Linux | Must Have |
| NFR-030-20 | Handle server reboot | Detect and reconnect | Should Have |

### Security (NFR-030-21 to NFR-030-30)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-030-21 | Encryption algorithm | AES-256-GCM preferred | Must Have |
| NFR-030-22 | Key exchange | Diffie-Hellman Group 16+ | Should Have |
| NFR-030-23 | Host key verification | Always by default | Must Have |
| NFR-030-24 | Credential protection | Never logged | Must Have |
| NFR-030-25 | Agent forwarding | Disabled by default | Must Have |
| NFR-030-26 | TCP forwarding | Disabled by default | Should Have |
| NFR-030-27 | Known hosts management | ~/.ssh/known_hosts | Should Have |
| NFR-030-28 | Permission on key files | Warn if too open | Should Have |
| NFR-030-29 | Connection audit logging | All connections logged | Must Have |
| NFR-030-30 | Secure random generation | Cryptographically secure | Must Have |

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

### SSH Target Implementation (AC-030-01 to AC-030-15)

- [ ] AC-030-01: `SshComputeTarget` class implements `IComputeTarget`
- [ ] AC-030-02: `SshTargetConfig` defines all connection parameters
- [ ] AC-030-03: Config validates host is required
- [ ] AC-030-04: Config validates username is required
- [ ] AC-030-05: Port defaults to 22
- [ ] AC-030-06: Target registers with factory as provider
- [ ] AC-030-07: Mode validation rejects in local-only/airgapped
- [ ] AC-030-08: Mode validation allows in burst mode
- [ ] AC-030-09: All IComputeTarget methods implemented
- [ ] AC-030-10: State machine works correctly for SSH target
- [ ] AC-030-11: Events emitted for connect/disconnect
- [ ] AC-030-12: Metrics exposed for SSH connections
- [ ] AC-030-13: Target ID includes "ssh" prefix
- [ ] AC-030-14: Metadata includes host, username, connection state
- [ ] AC-030-15: Target disposes all SSH resources

### Authentication (AC-030-16 to AC-030-30)

- [ ] AC-030-16: Private key authentication works
- [ ] AC-030-17: Private key with passphrase works
- [ ] AC-030-18: Password authentication works
- [ ] AC-030-19: SSH agent authentication works
- [ ] AC-030-20: Auth method auto-selected based on config
- [ ] AC-030-21: Auth failure throws with method that failed
- [ ] AC-030-22: Auth retry with fallback methods (configurable)
- [ ] AC-030-23: Private key file not found throws with path
- [ ] AC-030-24: Private key wrong format throws with format expected
- [ ] AC-030-25: Host key verification enabled by default
- [ ] AC-030-26: Known hosts file checked (~/.ssh/known_hosts)
- [ ] AC-030-27: Unknown host throws with key fingerprint
- [ ] AC-030-28: Strict checking disabled allows unknown hosts (logs warning)
- [ ] AC-030-29: Host key mismatch throws security exception
- [ ] AC-030-30: Agent auth falls back to key auth if agent unavailable

### Connection Pool (AC-030-31 to AC-030-45)

- [ ] AC-030-31: Connection pool created on first connect
- [ ] AC-030-32: Pool size configurable (default 4)
- [ ] AC-030-33: Connections reused across operations
- [ ] AC-030-34: Pool acquisition returns healthy connection
- [ ] AC-030-35: Pool releases connection after operation
- [ ] AC-030-36: Idle connections closed after timeout
- [ ] AC-030-37: Health check runs at configured interval
- [ ] AC-030-38: Failed health check removes connection
- [ ] AC-030-39: Pool replenishes after failed connection removed
- [ ] AC-030-40: Pool exhaustion queues requests
- [ ] AC-030-41: Pool size never exceeds configured max
- [ ] AC-030-42: All connections closed on dispose
- [ ] AC-030-43: Metrics: pool size, acquired, waiting
- [ ] AC-030-44: Connection reuse logged at debug level
- [ ] AC-030-45: Pool operations are thread-safe

### Bastion Support (AC-030-46 to AC-030-55)

- [ ] AC-030-46: Bastion host configurable in SshTargetConfig
- [ ] AC-030-47: Bastion has separate auth configuration
- [ ] AC-030-48: Connection tunnels through bastion to target
- [ ] AC-030-49: Bastion failure throws with bastion-specific message
- [ ] AC-030-50: Bastion connection pooled separately
- [ ] AC-030-51: Multi-hop (chained bastions) works
- [ ] AC-030-52: Bastion logged in connection events
- [ ] AC-030-53: Bastion adds latency to metrics
- [ ] AC-030-54: Bastion credentials never logged
- [ ] AC-030-55: Bastion timeout configurable separately

### Workspace and Environment (AC-030-56 to AC-030-65)

- [ ] AC-030-56: Remote workspace created on PrepareAsync
- [ ] AC-030-57: Workspace path configurable (default /tmp/acode-{id})
- [ ] AC-030-58: Workspace permissions set to 0755
- [ ] AC-030-59: Workspace removed on teardown
- [ ] AC-030-60: Multiple workspaces isolated (different paths)
- [ ] AC-030-61: Environment variables set on remote shell
- [ ] AC-030-62: PATH extended with configured values
- [ ] AC-030-63: Working directory set to workspace
- [ ] AC-030-64: Shell auto-detected (bash/sh/zsh)
- [ ] AC-030-65: Shell-specific escaping works correctly

---

## User Verification Scenarios

### Scenario 1: Connect to Remote Server via SSH Key

**Persona:** Developer with SSH access to build server

**Steps:**
1. Configure SSH target in agent config with host, user, key
2. Run `acode target test ssh://builder@build.example.com`
3. Observe: "Connecting to build.example.com:22..."
4. Observe: "Connected successfully (SSH key auth)"
5. Run `acode target add` to add permanently

**Verification:**
- [ ] Connection established
- [ ] Auth method logged correctly
- [ ] Target added to configuration

### Scenario 2: Execute Command on SSH Target

**Persona:** Developer running remote build

**Steps:**
1. Create SSH target and prepare workspace
2. Execute `dotnet build` on remote target
3. Observe streaming output from remote
4. Check exit code and duration

**Verification:**
- [ ] Command executes on remote
- [ ] Output streams correctly
- [ ] Exit code captured
- [ ] Latency overhead <100ms

### Scenario 3: Connection Through Bastion Host

**Persona:** Developer in enterprise environment

**Steps:**
1. Configure bastion host in config
2. Configure target host (not directly reachable)
3. Connect to target via bastion
4. Observe: "Connecting via bastion.example.com..."
5. Observe: "Tunnel established, connecting to internal.example.com"

**Verification:**
- [ ] Bastion connection works
- [ ] Target reachable via tunnel
- [ ] Both hops logged

### Scenario 4: Connection Pool Under Load

**Persona:** System running parallel tasks

**Steps:**
1. Configure pool size of 4
2. Launch 10 concurrent commands
3. Observe: 4 execute immediately, 6 queue
4. As commands complete, queued ones start
5. All 10 complete successfully

**Verification:**
- [ ] Pool limit enforced
- [ ] Queuing works
- [ ] All commands complete
- [ ] No connection leaks

### Scenario 5: Host Key Verification Failure

**Persona:** Developer connecting to new server

**Steps:**
1. Connect to server not in known_hosts
2. Observe: "Host key verification failed"
3. Error includes host key fingerprint
4. Add to known_hosts manually
5. Reconnect succeeds

**Verification:**
- [ ] Unknown host rejected by default
- [ ] Fingerprint shown for verification
- [ ] After adding, connection works

### Scenario 6: Automatic Reconnection

**Persona:** Developer with unstable network

**Steps:**
1. Establish SSH connection
2. Simulate network interruption (brief)
3. Observe: "Connection lost, reconnecting..."
4. Network recovers
5. Observe: "Reconnected successfully"
6. Pending operation resumes

**Verification:**
- [ ] Disconnection detected quickly
- [ ] Automatic reconnection attempted
- [ ] Connection restored
- [ ] Operation completes

---

## Testing Requirements

### Unit Tests (UT-030-01 to UT-030-20)

- [ ] UT-030-01: SshTargetConfig validates host required
- [ ] UT-030-02: SshTargetConfig validates username required
- [ ] UT-030-03: SshTargetConfig defaults port to 22
- [ ] UT-030-04: Auth method selection based on config
- [ ] UT-030-05: Private key auth configured correctly
- [ ] UT-030-06: Password auth configured correctly
- [ ] UT-030-07: Agent auth configured correctly
- [ ] UT-030-08: Mode validation rejects local-only
- [ ] UT-030-09: Mode validation allows burst
- [ ] UT-030-10: Connection pool respects size limit
- [ ] UT-030-11: Pool returns healthy connections
- [ ] UT-030-12: Pool removes failed connections
- [ ] UT-030-13: Idle timeout closes connections
- [ ] UT-030-14: Health check detects dead connections
- [ ] UT-030-15: Bastion tunnel configuration
- [ ] UT-030-16: Shell escaping for bash
- [ ] UT-030-17: Shell escaping for sh
- [ ] UT-030-18: Workspace path generation
- [ ] UT-030-19: Events emitted correctly
- [ ] UT-030-20: Metrics recorded correctly

### Integration Tests (IT-030-01 to IT-030-15)

- [ ] IT-030-01: Real SSH connection to test server
- [ ] IT-030-02: Key authentication end-to-end
- [ ] IT-030-03: Password authentication (if test server supports)
- [ ] IT-030-04: Command execution over SSH
- [ ] IT-030-05: File transfer over SFTP
- [ ] IT-030-06: Bastion hop connection
- [ ] IT-030-07: Connection pool under concurrent load
- [ ] IT-030-08: Reconnection after disconnect
- [ ] IT-030-09: Workspace creation and cleanup
- [ ] IT-030-10: Host key verification
- [ ] IT-030-11: Full target lifecycle
- [ ] IT-030-12: Multiple concurrent SSH targets
- [ ] IT-030-13: Cross-platform client (run on Windows/macOS/Linux)
- [ ] IT-030-14: No connection leaks after 100 operations
- [ ] IT-030-15: Long-running session stability

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