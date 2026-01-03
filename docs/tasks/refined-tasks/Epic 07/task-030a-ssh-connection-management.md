# Task 030.a: SSH Connection Management

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 7 – Cloud Integration  
**Dependencies:** Task 030 (SSH Target)  

---

## Description

Task 030.a implements SSH connection management. Connections MUST be pooled. Reconnection MUST be automatic. Health checks MUST run.

Connection pooling prevents repeated handshakes. Keep-alive prevents timeouts. Automatic reconnection handles network issues.

Connections MUST be thread-safe. Multiple commands MUST share connections. Idle connections MUST be cleaned up.

### Business Value

Connection management provides:
- Faster command execution
- Better reliability
- Resource efficiency
- Network resilience

### Scope Boundaries

This task covers connection lifecycle. Command execution is in 030.b. File transfer is in 030.c.

### Integration Points

- Task 030: Part of SSH target
- Task 030.b: Uses connections
- Task 030.c: Uses SFTP connections

### Failure Modes

- Connection lost → Auto-reconnect
- All connections failed → Error
- Pool exhausted → Queue or error
- Auth expired → Re-authenticate

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Pool | Reusable connection set |
| Keep-alive | Heartbeat packets |
| Health check | Connection test |
| Reconnect | Restore failed connection |
| Idle | Unused connection |
| Drain | Stop accepting new requests |

---

## Out of Scope

- Connection multiplexing (SSH ControlMaster)
- Dynamic port forwarding
- Connection sharing across processes
- SSH agent forwarding
- Connection encryption options

---

## Functional Requirements

### FR-001 to FR-020: Connection Pool

- FR-001: `SshConnectionPool` MUST exist
- FR-002: Pool MUST be configurable
- FR-003: Min connections MUST be settable
- FR-004: Max connections MUST be settable
- FR-005: Default min: 1
- FR-006: Default max: 4
- FR-007: Connections created on demand
- FR-008: Pool MUST implement IDisposable
- FR-009: Dispose MUST close all connections
- FR-010: Acquire MUST return connection
- FR-011: Acquire MUST wait if exhausted
- FR-012: Acquire timeout MUST be configurable
- FR-013: Default acquire timeout: 30s
- FR-014: Release MUST return connection
- FR-015: Released connection MUST be reusable
- FR-016: Failed connection MUST be removed
- FR-017: New connection MUST be created
- FR-018: Pool stats MUST be available
- FR-019: Stats: active, idle, total
- FR-020: Stats: waiter count

### FR-021 to FR-040: Keep-Alive

- FR-021: Keep-alive MUST be enabled
- FR-022: Interval MUST be configurable
- FR-023: Default interval: 15 seconds
- FR-024: Keep-alive uses SSH keepalive
- FR-025: Missed keep-alives MUST detect
- FR-026: Max missed MUST be configurable
- FR-027: Default max missed: 3
- FR-028: Connection marked dead after max
- FR-029: Dead connections MUST reconnect
- FR-030: Reconnect MUST preserve state
- FR-031: Workspace path MUST persist
- FR-032: Environment MUST persist
- FR-033: Background timer MUST run
- FR-034: Timer MUST stop on dispose
- FR-035: Timer MUST not block
- FR-036: Keep-alive MUST log failures
- FR-037: Metrics MUST track keep-alives
- FR-038: Metrics MUST track failures
- FR-039: Metrics MUST track reconnects
- FR-040: Reconnect backoff MUST exist

### FR-041 to FR-060: Health Checks

- FR-041: Health check MUST be available
- FR-042: Check MUST be periodic
- FR-043: Check interval MUST be configurable
- FR-044: Default check: every 60 seconds
- FR-045: Check MUST run test command
- FR-046: Test command: `echo ok`
- FR-047: Timeout MUST apply
- FR-048: Check timeout: 5 seconds
- FR-049: Failed check MUST mark unhealthy
- FR-050: Unhealthy MUST reconnect
- FR-051: Multiple failures MUST escalate
- FR-052: Escalation MUST alert
- FR-053: Health status MUST be queryable
- FR-054: Status: healthy, unhealthy, unknown
- FR-055: Status MUST include last check
- FR-056: Status MUST include failures count
- FR-057: Forced health check MUST work
- FR-058: Check MUST not block pool
- FR-059: Check MUST use separate connection
- FR-060: Logging MUST include health status

---

## Non-Functional Requirements

- NFR-001: Pool acquire in <10ms
- NFR-002: Reconnect in <5 seconds
- NFR-003: Keep-alive <1KB network
- NFR-004: Health check <100ms
- NFR-005: 100 concurrent acquires
- NFR-006: No connection leaks
- NFR-007: Thread-safe
- NFR-008: Structured logging
- NFR-009: Metrics on pool state
- NFR-010: Graceful degradation

---

## User Manual Documentation

### Configuration

```yaml
ssh:
  connectionPool:
    minConnections: 1
    maxConnections: 4
    acquireTimeoutSeconds: 30
  keepAlive:
    intervalSeconds: 15
    maxMissed: 3
  healthCheck:
    intervalSeconds: 60
    timeoutSeconds: 5
```

### Pool States

| State | Description |
|-------|-------------|
| Initializing | Creating first connection |
| Ready | Connections available |
| Exhausted | All connections in use |
| Degraded | Some connections failed |
| Failed | All connections failed |

### Diagnostics

```csharp
var status = pool.GetStatus();
Console.WriteLine($"Active: {status.ActiveConnections}");
Console.WriteLine($"Idle: {status.IdleConnections}");
Console.WriteLine($"Waiters: {status.WaitingRequests}");
Console.WriteLine($"Health: {status.OverallHealth}");
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Pool creates connections
- [ ] AC-002: Pool limits enforced
- [ ] AC-003: Acquire/release works
- [ ] AC-004: Keep-alive works
- [ ] AC-005: Reconnection works
- [ ] AC-006: Health check works
- [ ] AC-007: Dead connection replaced
- [ ] AC-008: Stats available
- [ ] AC-009: Thread-safe verified
- [ ] AC-010: No leaks in tests

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Pool acquire/release
- [ ] UT-002: Pool exhaustion
- [ ] UT-003: Keep-alive logic
- [ ] UT-004: Health check logic

### Integration Tests

- [ ] IT-001: Real SSH pool
- [ ] IT-002: Connection recovery
- [ ] IT-003: Health monitoring
- [ ] IT-004: Concurrent usage

---

## Implementation Prompt

You are implementing SSH connection management for compute targets. This handles connection pooling, keep-alive, and health checks. Follow Clean Architecture and TDD.

### Part 1: File Structure and Domain Models

#### File Structure

```
src/Acode.Domain/
├── Compute/
│   └── Ssh/
│       └── Connection/
│           ├── PoolState.cs
│           ├── HealthState.cs
│           ├── ConnectionMetrics.cs
│           └── Events/
│               ├── ConnectionAcquiredEvent.cs
│               ├── ConnectionReleasedEvent.cs
│               ├── ConnectionFailedEvent.cs
│               └── PoolExhaustedEvent.cs

src/Acode.Application/
├── Compute/
│   └── Ssh/
│       └── Connection/
│           ├── ISshConnectionPool.cs
│           ├── ISshConnection.cs
│           ├── IHealthChecker.cs
│           ├── IKeepAliveManager.cs
│           └── PoolConfiguration.cs

src/Acode.Infrastructure/
├── Compute/
│   └── Ssh/
│       └── Connection/
│           ├── SshConnectionPool.cs
│           ├── SshConnectionWrapper.cs
│           ├── HealthChecker.cs
│           ├── KeepAliveManager.cs
│           └── PoolMetricsCollector.cs

tests/Acode.Infrastructure.Tests/
├── Compute/
│   └── Ssh/
│       └── Connection/
│           ├── SshConnectionPoolTests.cs
│           ├── HealthCheckerTests.cs
│           └── KeepAliveManagerTests.cs
```

#### Domain Models

```csharp
// src/Acode.Domain/Compute/Ssh/Connection/PoolState.cs
namespace Acode.Domain.Compute.Ssh.Connection;

public enum PoolState
{
    Initializing = 0,
    Ready = 1,
    Exhausted = 2,
    Degraded = 3,
    Failed = 4,
    Draining = 5,
    Disposed = 6
}

// src/Acode.Domain/Compute/Ssh/Connection/HealthState.cs
namespace Acode.Domain.Compute.Ssh.Connection;

public enum HealthState { Healthy, Unhealthy, Unknown, Degraded, Checking }

public sealed record HealthStatus(
    HealthState State,
    DateTimeOffset LastCheck,
    int ConsecutiveFailures,
    string? LastError,
    TimeSpan LastCheckDuration);

// src/Acode.Domain/Compute/Ssh/Connection/ConnectionMetrics.cs
namespace Acode.Domain.Compute.Ssh.Connection;

public sealed record ConnectionMetrics
{
    public int TotalCreated { get; init; }
    public int TotalFailed { get; init; }
    public int TotalReconnects { get; init; }
    public int KeepAlivesSent { get; init; }
    public int KeepAlivesFailed { get; init; }
    public int HealthChecksPassed { get; init; }
    public int HealthChecksFailed { get; init; }
    public TimeSpan AverageAcquireTime { get; init; }
    public TimeSpan AverageCommandTime { get; init; }
}

// src/Acode.Domain/Compute/Ssh/Connection/Events/ConnectionAcquiredEvent.cs
namespace Acode.Domain.Compute.Ssh.Connection.Events;

public sealed record ConnectionAcquiredEvent(
    string ConnectionId,
    string PoolId,
    TimeSpan WaitTime,
    DateTimeOffset Timestamp) : IDomainEvent;

public sealed record ConnectionReleasedEvent(
    string ConnectionId,
    string PoolId,
    TimeSpan HeldDuration,
    DateTimeOffset Timestamp) : IDomainEvent;

public sealed record PoolExhaustedEvent(
    string PoolId,
    int WaitersCount,
    DateTimeOffset Timestamp) : IDomainEvent;
```

**End of Task 030.a Specification - Part 1/3**