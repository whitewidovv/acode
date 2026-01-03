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

### Classes

```csharp
public class SshComputeTarget : IComputeTarget
{
    private readonly SshConnectionPool _pool;
    private readonly SshConfiguration _config;
}

public record SshConfiguration(
    string Host,
    int Port = 22,
    string Username = null,
    SshAuthMethod Auth = null,
    string WorkspacePath = null,
    SshBastionConfig Bastion = null,
    bool StrictHostKeyChecking = true,
    int ConnectionPoolSize = 4,
    TimeSpan KeepAliveInterval = default);

public abstract record SshAuthMethod;
public record SshKeyAuth(string KeyFile, string Passphrase = null) : SshAuthMethod;
public record SshPasswordAuth(string Password) : SshAuthMethod;
public record SshAgentAuth() : SshAuthMethod;

public record SshBastionConfig(
    string Host,
    int Port,
    string Username,
    SshAuthMethod Auth);
```

### Factory

```csharp
public class SshComputeTargetFactory : IComputeTargetFactory<SshConfiguration>
{
    public string TargetType => "ssh";
    
    public Task<IComputeTarget> CreateAsync(
        SshConfiguration config,
        CancellationToken ct);
}
```

---

**End of Task 030 Specification**