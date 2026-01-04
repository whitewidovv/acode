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

| Component | Integration Type | Description |
|-----------|-----------------|-------------|
| Task 030 SSH Target | Parent | Provides SshComputeTarget using these connections |
| Task 030.b Command Execution | Consumer | Uses connections for command execution |
| Task 030.c File Transfer | Consumer | Uses SFTP connections for file transfer |
| ISshConnectionPool | Interface | Main contract for connection pooling |
| ISshClient | Dependency | Underlying SSH client wrapper |
| IConnectionHealthChecker | Component | Monitors connection health |

### Failure Modes

| Failure Type | Detection | Recovery | User Impact |
|--------------|-----------|----------|-------------|
| Connection lost | Read/write exception | Auto-reconnect (3 attempts) | Brief pause |
| All connections failed | Pool empty | Error with diagnostic info | Operation fails |
| Pool exhausted | No available connections | Queue with timeout | Delayed execution |
| Auth expired | Auth exception | Re-authenticate | Brief reconnect |
| Network timeout | Timeout exception | Retry with backoff | Delayed execution |
| Host unreachable | Socket exception | Fail with clear message | Check network |

---

## Assumptions

1. **Stable Network**: Network is generally stable (brief interruptions acceptable)
2. **SSH Server Responsive**: Remote SSH server responds to keep-alives
3. **Thread Pool Available**: .NET thread pool available for async operations
4. **Reasonable Concurrency**: Max 100 concurrent commands per pool
5. **Memory Available**: Sufficient memory for connection buffers (~5MB/connection)
6. **Credentials Valid**: Credentials remain valid for session duration
7. **Host Reachable**: Host remains reachable (same IP/DNS)
8. **Pool per Target**: Each SSH target has its own connection pool

---

## Security Considerations

1. **Connection Reuse Security**: Reused connections don't leak data between commands
2. **Credential Caching**: Credentials cached securely for reconnection
3. **No Plaintext Logging**: Connection strings never contain passwords in logs
4. **Health Check Safety**: Health checks don't expose sensitive info
5. **Idle Timeout Security**: Idle connections closed to limit exposure
6. **Pool Isolation**: Pools isolated between targets (no cross-target reuse)
7. **Reconnection Auth**: Reconnection uses same secure auth as initial
8. **Audit Trail**: All connection events logged for security audit

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

### Connection Pool Management

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-030A-01 | `SshConnectionPool` class MUST be implemented in `AgenticCodingBot.Infrastructure.Compute.Ssh` namespace | P0 |
| FR-030A-02 | Pool MUST implement `ISshConnectionPool` interface with `AcquireAsync`, `ReleaseAsync`, `GetStatusAsync` methods | P0 |
| FR-030A-03 | Pool MUST be configurable via `SshPoolConfiguration` record | P0 |
| FR-030A-04 | Minimum connection count MUST be configurable with validation (≥0, ≤max) | P0 |
| FR-030A-05 | Maximum connection count MUST be configurable with validation (≥1, ≤100) | P0 |
| FR-030A-06 | Default minimum connections MUST be 1 | P1 |
| FR-030A-07 | Default maximum connections MUST be 4 | P1 |
| FR-030A-08 | Connections MUST be created on-demand (lazy initialization) | P0 |
| FR-030A-09 | Pool MUST pre-warm to minimum connections on first acquire | P1 |
| FR-030A-10 | Pool MUST implement `IAsyncDisposable` for proper cleanup | P0 |
| FR-030A-11 | Dispose MUST close all connections with configurable grace period | P0 |
| FR-030A-12 | Dispose MUST cancel pending waiters with `OperationCanceledException` | P0 |
| FR-030A-13 | `AcquireAsync` MUST return `IPooledSshConnection` wrapper | P0 |
| FR-030A-14 | Acquire MUST wait with timeout if pool exhausted | P0 |
| FR-030A-15 | Acquire timeout MUST be configurable (default 30s) | P0 |
| FR-030A-16 | Acquire MUST throw `SshPoolExhaustedException` on timeout | P0 |
| FR-030A-17 | `ReleaseAsync` MUST return connection to pool for reuse | P0 |
| FR-030A-18 | Released connection MUST be health-checked before reuse | P1 |
| FR-030A-19 | Failed connection on release MUST be disposed, not reused | P0 |
| FR-030A-20 | Pool MUST create replacement connection when one fails | P0 |

### Pool Statistics and Monitoring

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-030A-21 | `GetStatusAsync` MUST return `SshPoolStatus` with comprehensive stats | P0 |
| FR-030A-22 | Status MUST include active connection count | P0 |
| FR-030A-23 | Status MUST include idle connection count | P0 |
| FR-030A-24 | Status MUST include total connection count | P0 |
| FR-030A-25 | Status MUST include waiting request count | P1 |
| FR-030A-26 | Status MUST include total acquires since start | P2 |
| FR-030A-27 | Status MUST include total releases since start | P2 |
| FR-030A-28 | Status MUST include connection failure count | P1 |
| FR-030A-29 | Status MUST include reconnection attempt count | P1 |
| FR-030A-30 | Pool MUST expose `IObservable<SshPoolEvent>` for event streaming | P2 |

### Keep-Alive Management

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-030A-31 | Keep-alive MUST be enabled by default on all pooled connections | P0 |
| FR-030A-32 | Keep-alive interval MUST be configurable (default 15 seconds) | P0 |
| FR-030A-33 | Keep-alive MUST use SSH protocol-level `SSH_MSG_IGNORE` packets | P1 |
| FR-030A-34 | Keep-alive MUST track missed responses per connection | P0 |
| FR-030A-35 | Maximum missed keep-alives MUST be configurable (default 3) | P0 |
| FR-030A-36 | Connection MUST be marked dead after max missed threshold | P0 |
| FR-030A-37 | Dead connections MUST trigger automatic reconnection | P0 |
| FR-030A-38 | Reconnection MUST preserve workspace path state | P0 |
| FR-030A-39 | Reconnection MUST preserve environment variable state | P1 |
| FR-030A-40 | Background keep-alive timer MUST use `PeriodicTimer` | P1 |
| FR-030A-41 | Timer MUST stop automatically on pool dispose | P0 |
| FR-030A-42 | Timer callback MUST be non-blocking (fire-and-forget) | P0 |
| FR-030A-43 | Keep-alive failures MUST be logged at Warning level | P1 |
| FR-030A-44 | Metrics MUST track keep-alive success/failure counts | P1 |
| FR-030A-45 | Reconnect MUST use exponential backoff (100ms → 30s) | P0 |

### Health Check System

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-030A-46 | Health check MUST be available via `CheckHealthAsync()` method | P0 |
| FR-030A-47 | Periodic health check MUST run on configurable interval (default 60s) | P1 |
| FR-030A-48 | Health check MUST execute `echo ok` test command | P0 |
| FR-030A-49 | Health check MUST have configurable timeout (default 5s) | P0 |
| FR-030A-50 | Failed health check MUST mark connection as unhealthy | P0 |
| FR-030A-51 | Unhealthy connection MUST trigger reconnection attempt | P0 |
| FR-030A-52 | Multiple consecutive failures (3+) MUST trigger escalation event | P1 |
| FR-030A-53 | Escalation MUST emit `ConnectionEscalated` event for alerting | P1 |
| FR-030A-54 | Health status MUST be queryable via `GetHealthAsync()` method | P0 |
| FR-030A-55 | Status MUST return enum: `Healthy`, `Unhealthy`, `Degraded`, `Unknown` | P0 |
| FR-030A-56 | Status MUST include last successful check timestamp | P1 |
| FR-030A-57 | Status MUST include consecutive failure count | P1 |
| FR-030A-58 | Forced health check MUST be triggerable via `ForceHealthCheckAsync()` | P2 |
| FR-030A-59 | Health check MUST NOT block pool acquire operations | P0 |
| FR-030A-60 | Health check MUST use dedicated connection slot (not from pool) | P1 |

### Connection Lifecycle Events

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-030A-61 | Pool MUST emit `ConnectionCreated` event on new connection | P1 |
| FR-030A-62 | Pool MUST emit `ConnectionAcquired` event on acquire | P1 |
| FR-030A-63 | Pool MUST emit `ConnectionReleased` event on release | P1 |
| FR-030A-64 | Pool MUST emit `ConnectionFailed` event on connection error | P0 |
| FR-030A-65 | Pool MUST emit `ConnectionReconnected` event on successful reconnect | P1 |
| FR-030A-66 | Pool MUST emit `PoolExhausted` event when max reached | P1 |
| FR-030A-67 | Pool MUST emit `PoolDrained` event on graceful shutdown | P1 |
| FR-030A-68 | All events MUST include correlation ID for tracing | P0 |
| FR-030A-69 | Events MUST be non-blocking (fire-and-forget with queue) | P0 |
| FR-030A-70 | Event handlers MUST be exception-isolated | P0 |

### Graceful Shutdown

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-030A-71 | Pool MUST support `DrainAsync()` for graceful shutdown | P0 |
| FR-030A-72 | Drain MUST stop accepting new acquire requests | P0 |
| FR-030A-73 | Drain MUST wait for active connections to release | P0 |
| FR-030A-74 | Drain MUST have configurable timeout (default 30s) | P0 |
| FR-030A-75 | Drain timeout MUST force-close remaining connections | P0 |
| FR-030A-76 | Pool state MUST transition: Ready → Draining → Drained | P1 |
| FR-030A-77 | Acquire during drain MUST throw `SshPoolDrainingException` | P0 |
| FR-030A-78 | Drain progress MUST be observable (remaining count) | P2 |
| FR-030A-79 | Drain MUST log progress at Info level | P1 |
| FR-030A-80 | Double-drain MUST be idempotent (no error) | P1 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-030A-01 | Pool acquire latency from idle connection | <10ms p99 | P0 |
| NFR-030A-02 | Pool acquire latency when creating new connection | <2s p99 | P0 |
| NFR-030A-03 | Connection reconnection time | <5s | P0 |
| NFR-030A-04 | Keep-alive packet size | <1KB | P1 |
| NFR-030A-05 | Health check execution time | <100ms | P0 |
| NFR-030A-06 | Pool status query time | <1ms | P1 |
| NFR-030A-07 | Concurrent acquire throughput | 100 req/s | P1 |
| NFR-030A-08 | Memory per pooled connection | <50KB | P2 |
| NFR-030A-09 | Background timer CPU usage | <0.1% | P2 |
| NFR-030A-10 | Drain completion time with active connections | <configurable timeout | P0 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-030A-11 | Connection leak prevention | Zero leaks in 24h test | P0 |
| NFR-030A-12 | Reconnection success rate after network blip | >99% | P0 |
| NFR-030A-13 | Keep-alive false positive rate | <0.1% | P1 |
| NFR-030A-14 | Pool availability under partial failures | Degraded but operational | P0 |
| NFR-030A-15 | Graceful degradation when all connections fail | Emit event, no crash | P0 |
| NFR-030A-16 | Thread safety under concurrent access | Zero race conditions | P0 |
| NFR-030A-17 | Exception isolation in event handlers | 100% isolated | P0 |
| NFR-030A-18 | State consistency after reconnection | Identical to pre-failure | P0 |
| NFR-030A-19 | Double-dispose safety | Idempotent, no error | P0 |
| NFR-030A-20 | Waiter queue FIFO ordering | Guaranteed fair ordering | P1 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-030A-21 | Structured logging with correlation IDs | All operations | P0 |
| NFR-030A-22 | Metrics for pool state (active, idle, waiters) | Real-time via `IMetrics` | P0 |
| NFR-030A-23 | Metrics for connection lifecycle events | Per-event counters | P1 |
| NFR-030A-24 | Health status exposure via interface | Queryable at any time | P0 |
| NFR-030A-25 | Event streaming for external consumers | Via `IObservable` | P2 |
| NFR-030A-26 | Log level configurability | Per-category | P1 |
| NFR-030A-27 | Distributed tracing context propagation | `Activity` support | P1 |
| NFR-030A-28 | Connection duration histograms | Buckets: 1s, 10s, 60s, 5m | P2 |
| NFR-030A-29 | Alerting threshold for pool exhaustion | Configurable | P1 |
| NFR-030A-30 | Diagnostic dump capability | Full pool state on demand | P2 |

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

### Pool Creation and Configuration
- [ ] AC-001: `SshConnectionPool` class exists in correct namespace
- [ ] AC-002: Pool implements `ISshConnectionPool` interface
- [ ] AC-003: Pool implements `IAsyncDisposable` for cleanup
- [ ] AC-004: `SshPoolConfiguration` record validates min/max constraints
- [ ] AC-005: Default min connections is 1
- [ ] AC-006: Default max connections is 4
- [ ] AC-007: Invalid configuration throws `ArgumentException` with details
- [ ] AC-008: Pool can be created with DI container registration

### Connection Acquire/Release
- [ ] AC-009: `AcquireAsync` returns `IPooledSshConnection` when available
- [ ] AC-010: `AcquireAsync` waits when pool exhausted
- [ ] AC-011: `AcquireAsync` throws `SshPoolExhaustedException` on timeout
- [ ] AC-012: Acquire timeout is configurable (default 30s)
- [ ] AC-013: `ReleaseAsync` returns connection to pool
- [ ] AC-014: Released connection is reusable for next acquire
- [ ] AC-015: Failed connection on release is disposed, not reused
- [ ] AC-016: Pool creates replacement when connection fails
- [ ] AC-017: Concurrent acquires are handled fairly (FIFO)
- [ ] AC-018: 100 concurrent acquires complete without deadlock

### Connection Lifecycle Events
- [ ] AC-019: `ConnectionCreated` event fires on new connection
- [ ] AC-020: `ConnectionAcquired` event fires on every acquire
- [ ] AC-021: `ConnectionReleased` event fires on every release
- [ ] AC-022: `ConnectionFailed` event fires on connection error
- [ ] AC-023: `ConnectionReconnected` event fires on successful reconnect
- [ ] AC-024: `PoolExhausted` event fires when max reached
- [ ] AC-025: All events include correlation ID
- [ ] AC-026: Event handler exceptions don't crash pool

### Keep-Alive Management
- [ ] AC-027: Keep-alive is enabled by default
- [ ] AC-028: Keep-alive interval is configurable (default 15s)
- [ ] AC-029: Keep-alive uses SSH protocol packets
- [ ] AC-030: Missed keep-alives are tracked per connection
- [ ] AC-031: Connection marked dead after 3 missed (configurable)
- [ ] AC-032: Dead connection triggers automatic reconnection
- [ ] AC-033: Keep-alive timer stops on pool dispose
- [ ] AC-034: Keep-alive failures logged at Warning level
- [ ] AC-035: Keep-alive metrics track success/failure counts

### Reconnection Behavior
- [ ] AC-036: Dead connections reconnect automatically
- [ ] AC-037: Reconnection preserves workspace path
- [ ] AC-038: Reconnection preserves environment variables
- [ ] AC-039: Reconnection uses exponential backoff (100ms → 30s)
- [ ] AC-040: Max reconnection attempts is configurable
- [ ] AC-041: Failed reconnection emits escalation event
- [ ] AC-042: Reconnection success rate >99% after network blip

### Health Check System
- [ ] AC-043: `CheckHealthAsync` method exists and works
- [ ] AC-044: Periodic health check runs on configured interval (default 60s)
- [ ] AC-045: Health check executes `echo ok` test command
- [ ] AC-046: Health check timeout is 5s (configurable)
- [ ] AC-047: Failed health check marks connection unhealthy
- [ ] AC-048: Unhealthy connection triggers reconnection
- [ ] AC-049: 3+ consecutive failures trigger escalation
- [ ] AC-050: `GetHealthAsync` returns current health status
- [ ] AC-051: Health status includes last check timestamp
- [ ] AC-052: Health status includes failure count
- [ ] AC-053: `ForceHealthCheckAsync` triggers immediate check
- [ ] AC-054: Health check doesn't block pool acquire

### Pool Statistics
- [ ] AC-055: `GetStatusAsync` returns comprehensive stats
- [ ] AC-056: Stats include active connection count
- [ ] AC-057: Stats include idle connection count
- [ ] AC-058: Stats include total connection count
- [ ] AC-059: Stats include waiting request count
- [ ] AC-060: Stats include total acquires since start
- [ ] AC-061: Stats query completes in <1ms

### Graceful Shutdown
- [ ] AC-062: `DrainAsync` stops new acquire requests
- [ ] AC-063: Drain waits for active connections to release
- [ ] AC-064: Drain timeout is configurable (default 30s)
- [ ] AC-065: Drain timeout force-closes remaining connections
- [ ] AC-066: Pool state transitions: Ready → Draining → Drained
- [ ] AC-067: Acquire during drain throws `SshPoolDrainingException`
- [ ] AC-068: Drain progress is logged at Info level
- [ ] AC-069: Double-drain is idempotent (no error)

### Thread Safety and Reliability
- [ ] AC-070: Pool is thread-safe under concurrent access
- [ ] AC-071: Zero race conditions in 1000-iteration stress test
- [ ] AC-072: Zero connection leaks in 24h stability test
- [ ] AC-073: Event handlers are exception-isolated
- [ ] AC-074: Double-dispose is safe and idempotent
- [ ] AC-075: Pool degrades gracefully when all connections fail

---

## User Verification Scenarios

### Scenario 1: Developer Tests Connection Pool Under Load
**Persona:** Platform Engineer validating pool behavior  
**Preconditions:** SSH target configured, pool created with max 4 connections  
**Steps:**
1. Start 10 concurrent command executions
2. Observe pool exhaustion behavior
3. Verify commands queue and complete in order
4. Check pool statistics during load

**Verification Checklist:**
- [ ] First 4 commands start immediately
- [ ] Remaining 6 commands wait in queue
- [ ] `PoolExhausted` event fires when max reached
- [ ] All 10 commands complete successfully
- [ ] Pool stats show 4 active, 6 waiting during peak
- [ ] No connection leaks after completion

### Scenario 2: Operations Team Monitors Pool Health
**Persona:** SRE monitoring production deployment  
**Preconditions:** Pool running with health checks enabled  
**Steps:**
1. Query pool health status
2. Simulate network blip (disconnect/reconnect)
3. Observe keep-alive failure detection
4. Verify automatic reconnection

**Verification Checklist:**
- [ ] `GetHealthAsync` returns `Healthy` initially
- [ ] Keep-alive failures logged at Warning level
- [ ] Connection marked dead after 3 missed keep-alives
- [ ] Reconnection attempt logged at Info level
- [ ] `ConnectionReconnected` event fires on success
- [ ] Health returns to `Healthy` after reconnect

### Scenario 3: Developer Gracefully Shuts Down Pool
**Persona:** Developer stopping application  
**Preconditions:** Pool with 2 active connections executing commands  
**Steps:**
1. Call `DrainAsync` during active operations
2. Observe drain behavior
3. Verify new acquire requests rejected
4. Wait for drain completion

**Verification Checklist:**
- [ ] Pool state transitions to `Draining`
- [ ] New acquires throw `SshPoolDrainingException`
- [ ] Active connections allowed to complete
- [ ] Drain progress logged (remaining count)
- [ ] Pool state transitions to `Drained` when complete
- [ ] All connections closed after drain

### Scenario 4: Handling Cascading Connection Failures
**Persona:** SRE responding to SSH server crash  
**Preconditions:** Pool with 4 connections, SSH server crashes  
**Steps:**
1. Observe all connections fail simultaneously
2. Check escalation events
3. Verify degraded state
4. Wait for server recovery

**Verification Checklist:**
- [ ] `ConnectionFailed` events fire for all 4 connections
- [ ] `ConnectionEscalated` event fires (multiple failures)
- [ ] Pool enters `Degraded` state
- [ ] Reconnection attempts use backoff
- [ ] Pool recovers when server restarts
- [ ] All connections re-establish

### Scenario 5: Long-Running Session with Keep-Alive
**Persona:** Developer running 2-hour build job  
**Preconditions:** Pool connection acquired, keep-alive enabled  
**Steps:**
1. Acquire connection from pool
2. Run long-running command (2 hours)
3. Verify keep-alive maintains connection
4. Release connection after completion

**Verification Checklist:**
- [ ] Keep-alive packets sent every 15s
- [ ] Connection remains healthy for 2 hours
- [ ] No timeout or disconnect during job
- [ ] Keep-alive metrics show success count
- [ ] Connection reusable after release
- [ ] No memory growth over session

### Scenario 6: Concurrent Pool Access from Multiple Threads
**Persona:** Developer stress-testing thread safety  
**Preconditions:** Pool configured, multi-threaded test harness  
**Steps:**
1. Launch 50 threads acquiring/releasing rapidly
2. Run for 60 seconds
3. Check for race conditions
4. Verify final pool state

**Verification Checklist:**
- [ ] No deadlocks during test
- [ ] No race condition exceptions
- [ ] Acquire/release counts match
- [ ] Pool stats are consistent
- [ ] No connection leaks
- [ ] All threads complete successfully

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-030A-01 | Pool creates min connections on first acquire | FR-030A-08, FR-030A-09 |
| UT-030A-02 | Pool respects max connection limit | FR-030A-05 |
| UT-030A-03 | Acquire returns connection when available | FR-030A-13 |
| UT-030A-04 | Acquire waits when pool exhausted | FR-030A-14 |
| UT-030A-05 | Acquire throws on timeout | FR-030A-16 |
| UT-030A-06 | Release returns connection for reuse | FR-030A-17 |
| UT-030A-07 | Failed connection not reused | FR-030A-19 |
| UT-030A-08 | Replacement created for failed connection | FR-030A-20 |
| UT-030A-09 | Keep-alive timer starts on pool creation | FR-030A-40 |
| UT-030A-10 | Keep-alive timer stops on dispose | FR-030A-41 |
| UT-030A-11 | Missed keep-alive increments counter | FR-030A-34 |
| UT-030A-12 | Connection marked dead after max missed | FR-030A-36 |
| UT-030A-13 | Health check runs test command | FR-030A-48 |
| UT-030A-14 | Health check respects timeout | FR-030A-49 |
| UT-030A-15 | Failed health marks unhealthy | FR-030A-50 |
| UT-030A-16 | GetStatus returns correct counts | FR-030A-21-24 |
| UT-030A-17 | Drain stops new acquires | FR-030A-72 |
| UT-030A-18 | Drain waits for active connections | FR-030A-73 |
| UT-030A-19 | Drain timeout forces close | FR-030A-75 |
| UT-030A-20 | Events fire with correlation ID | FR-030A-68 |
| UT-030A-21 | Configuration validates min ≤ max | FR-030A-04 |
| UT-030A-22 | Configuration validates max ≥ 1 | FR-030A-05 |
| UT-030A-23 | Double-dispose is safe | NFR-030A-19 |
| UT-030A-24 | Event handler exception isolated | FR-030A-70 |
| UT-030A-25 | Reconnect backoff increases correctly | FR-030A-45 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-030A-01 | Real SSH pool acquires and releases | E2E pool operation |
| IT-030A-02 | Pool recovers from SSH server restart | FR-030A-37 |
| IT-030A-03 | Keep-alive maintains long connection | FR-030A-31-33 |
| IT-030A-04 | Health check detects dead connection | FR-030A-50 |
| IT-030A-05 | Reconnection preserves workspace path | FR-030A-38 |
| IT-030A-06 | Concurrent acquires are fair (FIFO) | NFR-030A-20 |
| IT-030A-07 | Pool handles 100 concurrent acquires | NFR-030A-07 |
| IT-030A-08 | No leaks in 1000-iteration stress test | NFR-030A-11 |
| IT-030A-09 | Drain completes with active connections | FR-030A-73 |
| IT-030A-10 | Metrics report correct values | NFR-030A-22 |
| IT-030A-11 | Events stream to observer | FR-030A-30 |
| IT-030A-12 | Pool degrades gracefully under failure | NFR-030A-15 |
| IT-030A-13 | Distributed tracing propagates | NFR-030A-27 |
| IT-030A-14 | Health status reflects actual state | FR-030A-54 |
| IT-030A-15 | Escalation fires after 3 failures | FR-030A-52 |
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

### Part 2: Application Interfaces and Pool Implementation

```csharp
// src/Acode.Application/Compute/Ssh/Connection/PoolConfiguration.cs
namespace Acode.Application.Compute.Ssh.Connection;

public sealed record PoolConfiguration
{
    public int MinConnections { get; init; } = 1;
    public int MaxConnections { get; init; } = 4;
    public TimeSpan AcquireTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan IdleTimeout { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan KeepAliveInterval { get; init; } = TimeSpan.FromSeconds(15);
    public int MaxMissedKeepAlives { get; init; } = 3;
    public TimeSpan HealthCheckInterval { get; init; } = TimeSpan.FromSeconds(60);
    public TimeSpan HealthCheckTimeout { get; init; } = TimeSpan.FromSeconds(5);
    public bool EnableHealthChecks { get; init; } = true;
    public bool EnableKeepAlive { get; init; } = true;
}

// src/Acode.Application/Compute/Ssh/Connection/ISshConnectionPool.cs
namespace Acode.Application.Compute.Ssh.Connection;

public interface ISshConnectionPool : IAsyncDisposable
{
    string PoolId { get; }
    PoolState State { get; }
    
    Task<ISshConnection> AcquireAsync(CancellationToken ct = default);
    void Release(ISshConnection connection);
    
    PoolStatus GetStatus();
    Task<HealthStatus> CheckHealthAsync(CancellationToken ct = default);
    Task DrainAsync(CancellationToken ct = default);
}

public sealed record PoolStatus(
    int ActiveConnections,
    int IdleConnections,
    int TotalConnections,
    int WaitingRequests,
    PoolState State,
    HealthState OverallHealth);

// src/Acode.Application/Compute/Ssh/Connection/ISshConnection.cs
namespace Acode.Application.Compute.Ssh.Connection;

public interface ISshConnection : IAsyncDisposable
{
    string ConnectionId { get; }
    bool IsConnected { get; }
    DateTimeOffset CreatedAt { get; }
    DateTimeOffset LastUsedAt { get; }
    int MissedKeepAlives { get; }
    
    Task<SshCommandResult> ExecuteAsync(
        string command,
        TimeSpan? timeout = null,
        CancellationToken ct = default);
    
    ISftpChannel OpenSftp();
    Task ReconnectAsync(CancellationToken ct = default);
    void MarkUsed();
    void IncrementMissedKeepAlive();
    void ResetKeepAliveCounter();
}

// src/Acode.Application/Compute/Ssh/Connection/IHealthChecker.cs
namespace Acode.Application.Compute.Ssh.Connection;

public interface IHealthChecker
{
    Task<HealthCheckResult> CheckAsync(
        ISshConnection connection,
        CancellationToken ct = default);
    
    Task<PoolHealthResult> CheckPoolAsync(
        ISshConnectionPool pool,
        CancellationToken ct = default);
}

public sealed record HealthCheckResult(
    bool IsHealthy,
    TimeSpan Duration,
    string? ErrorMessage);

public sealed record PoolHealthResult(
    HealthState State,
    int HealthyConnections,
    int UnhealthyConnections,
    IReadOnlyList<HealthCheckResult> ConnectionResults);

// src/Acode.Application/Compute/Ssh/Connection/IKeepAliveManager.cs
namespace Acode.Application.Compute.Ssh.Connection;

public interface IKeepAliveManager : IAsyncDisposable
{
    void Start();
    void Stop();
    bool IsRunning { get; }
    void RegisterConnection(ISshConnection connection);
    void UnregisterConnection(ISshConnection connection);
}

// src/Acode.Infrastructure/Compute/Ssh/Connection/SshConnectionPool.cs
namespace Acode.Infrastructure.Compute.Ssh.Connection;

public sealed class SshConnectionPool : ISshConnectionPool
{
    private readonly PoolConfiguration _config;
    private readonly ISshSessionFactory _sessionFactory;
    private readonly IHealthChecker _healthChecker;
    private readonly IKeepAliveManager _keepAlive;
    private readonly IEventPublisher _events;
    private readonly ILogger<SshConnectionPool> _logger;
    
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentQueue<ISshConnection> _idle = new();
    private readonly ConcurrentDictionary<string, ISshConnection> _active = new();
    private readonly Timer _healthCheckTimer;
    private volatile PoolState _state = PoolState.Initializing;
    private volatile HealthState _healthState = HealthState.Unknown;
    
    public string PoolId { get; } = Ulid.NewUlid().ToString();
    public PoolState State => _state;
    
    public SshConnectionPool(
        PoolConfiguration config,
        ISshSessionFactory sessionFactory,
        IHealthChecker healthChecker,
        IKeepAliveManager keepAlive,
        IEventPublisher events,
        ILogger<SshConnectionPool> logger)
    {
        _config = config;
        _sessionFactory = sessionFactory;
        _healthChecker = healthChecker;
        _keepAlive = keepAlive;
        _events = events;
        _logger = logger;
        
        _semaphore = new SemaphoreSlim(config.MaxConnections, config.MaxConnections);
        
        if (config.EnableHealthChecks)
        {
            _healthCheckTimer = new Timer(
                async _ => await PeriodicHealthCheckAsync(),
                null,
                config.HealthCheckInterval,
                config.HealthCheckInterval);
        }
        
        if (config.EnableKeepAlive)
            _keepAlive.Start();
        
        _state = PoolState.Ready;
    }
    
    public async Task<ISshConnection> AcquireAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_config.AcquireTimeout);
        
        if (!await _semaphore.WaitAsync(cts.Token))
        {
            await _events.PublishAsync(new PoolExhaustedEvent(
                PoolId, GetWaitersCount(), DateTimeOffset.UtcNow));
            throw new TimeoutException("Connection pool exhausted");
        }
        
        try
        {
            // Try idle connection first
            while (_idle.TryDequeue(out var idle))
            {
                if (idle.IsConnected && idle.MissedKeepAlives < _config.MaxMissedKeepAlives)
                {
                    idle.MarkUsed();
                    _active[idle.ConnectionId] = idle;
                    
                    await _events.PublishAsync(new ConnectionAcquiredEvent(
                        idle.ConnectionId, PoolId, stopwatch.Elapsed, DateTimeOffset.UtcNow));
                    
                    return idle;
                }
                else
                {
                    await idle.DisposeAsync();
                }
            }
            
            // Create new connection
            var connection = await CreateConnectionAsync(ct);
            _active[connection.ConnectionId] = connection;
            _keepAlive.RegisterConnection(connection);
            
            await _events.PublishAsync(new ConnectionAcquiredEvent(
                connection.ConnectionId, PoolId, stopwatch.Elapsed, DateTimeOffset.UtcNow));
            
            return connection;
        }
        catch
        {
            _semaphore.Release();
            throw;
        }
    }
    
    public void Release(ISshConnection connection)
    {
        if (!_active.TryRemove(connection.ConnectionId, out _))
            return;
        
        var heldDuration = DateTimeOffset.UtcNow - connection.LastUsedAt;
        
        if (connection.IsConnected && 
            connection.MissedKeepAlives < _config.MaxMissedKeepAlives &&
            _state == PoolState.Ready)
        {
            _idle.Enqueue(connection);
        }
        else
        {
            _keepAlive.UnregisterConnection(connection);
            _ = connection.DisposeAsync();
        }
        
        _semaphore.Release();
        
        _ = _events.PublishAsync(new ConnectionReleasedEvent(
            connection.ConnectionId, PoolId, heldDuration, DateTimeOffset.UtcNow));
    }
    
    public PoolStatus GetStatus() => new(
        _active.Count,
        _idle.Count,
        _active.Count + _idle.Count,
        GetWaitersCount(),
        _state,
        _healthState);
    
    private int GetWaitersCount() =>
        _config.MaxConnections - _semaphore.CurrentCount - _active.Count;
    
    private async Task<ISshConnection> CreateConnectionAsync(CancellationToken ct)
    {
        var client = await _sessionFactory.CreateClientAsync(_config, ct);
        await client.ConnectAsync(ct);
        return new SshConnectionWrapper(client);
    }
}
```

**End of Task 030.a Specification - Part 2/3**

### Part 3: Health Checking, Keep-Alive, and Implementation Checklist

```csharp
// src/Acode.Infrastructure/Compute/Ssh/Connection/HealthChecker.cs
namespace Acode.Infrastructure.Compute.Ssh.Connection;

public sealed class HealthChecker : IHealthChecker
{
    private readonly ILogger<HealthChecker> _logger;
    
    public async Task<HealthCheckResult> CheckAsync(
        ISshConnection connection,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await connection.ExecuteAsync(
                "echo health-check", 
                timeout: TimeSpan.FromSeconds(5),
                ct: ct);
            
            stopwatch.Stop();
            
            var isHealthy = result.ExitCode == 0 && 
                           result.Output.Contains("health-check");
            
            return new HealthCheckResult(isHealthy, stopwatch.Elapsed, null);
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(false, stopwatch.Elapsed, ex.Message);
        }
    }
    
    public async Task<PoolHealthResult> CheckPoolAsync(
        ISshConnectionPool pool,
        CancellationToken ct = default)
    {
        var status = pool.GetStatus();
        var results = new List<HealthCheckResult>();
        var healthyCount = 0;
        
        // Check each active connection
        // In production, sample rather than check all
        foreach (var connection in GetPoolConnections(pool))
        {
            var result = await CheckAsync(connection, ct);
            results.Add(result);
            if (result.IsHealthy) healthyCount++;
        }
        
        var unhealthyCount = results.Count - healthyCount;
        var state = DetermineHealthState(healthyCount, unhealthyCount);
        
        return new PoolHealthResult(state, healthyCount, unhealthyCount, results);
    }
    
    private static HealthState DetermineHealthState(int healthy, int unhealthy)
    {
        if (unhealthy == 0) return HealthState.Healthy;
        if (healthy == 0) return HealthState.Unhealthy;
        return HealthState.Degraded;
    }
}

// src/Acode.Infrastructure/Compute/Ssh/Connection/KeepAliveManager.cs
namespace Acode.Infrastructure.Compute.Ssh.Connection;

public sealed class KeepAliveManager : IKeepAliveManager
{
    private readonly PoolConfiguration _config;
    private readonly ILogger<KeepAliveManager> _logger;
    private readonly ConcurrentDictionary<string, ISshConnection> _connections = new();
    private Timer? _timer;
    private volatile bool _isRunning;
    
    public bool IsRunning => _isRunning;
    
    public KeepAliveManager(
        PoolConfiguration config,
        ILogger<KeepAliveManager> logger)
    {
        _config = config;
        _logger = logger;
    }
    
    public void Start()
    {
        if (_isRunning) return;
        
        _timer = new Timer(
            async _ => await SendKeepAlivesAsync(),
            null,
            _config.KeepAliveInterval,
            _config.KeepAliveInterval);
        
        _isRunning = true;
        _logger.LogDebug("Keep-alive manager started with interval {Interval}", 
            _config.KeepAliveInterval);
    }
    
    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
        _isRunning = false;
        _logger.LogDebug("Keep-alive manager stopped");
    }
    
    public void RegisterConnection(ISshConnection connection)
    {
        _connections[connection.ConnectionId] = connection;
        _logger.LogDebug("Registered connection {ConnectionId} for keep-alive", 
            connection.ConnectionId);
    }
    
    public void UnregisterConnection(ISshConnection connection)
    {
        _connections.TryRemove(connection.ConnectionId, out _);
        _logger.LogDebug("Unregistered connection {ConnectionId} from keep-alive", 
            connection.ConnectionId);
    }
    
    private async Task SendKeepAlivesAsync()
    {
        foreach (var (id, connection) in _connections)
        {
            try
            {
                // Send SSH keep-alive (null request)
                var result = await connection.ExecuteAsync(
                    ":", // No-op command
                    timeout: TimeSpan.FromSeconds(5),
                    ct: CancellationToken.None);
                
                if (result.ExitCode == 0)
                {
                    connection.ResetKeepAliveCounter();
                }
                else
                {
                    connection.IncrementMissedKeepAlive();
                    _logger.LogWarning(
                        "Keep-alive failed for {ConnectionId}, missed count: {Count}",
                        id, connection.MissedKeepAlives);
                }
            }
            catch (Exception ex)
            {
                connection.IncrementMissedKeepAlive();
                _logger.LogWarning(ex,
                    "Keep-alive exception for {ConnectionId}, missed count: {Count}",
                    id, connection.MissedKeepAlives);
            }
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        Stop();
        _connections.Clear();
    }
}

// src/Acode.Infrastructure/Compute/Ssh/Connection/SshConnectionWrapper.cs
namespace Acode.Infrastructure.Compute.Ssh.Connection;

public sealed class SshConnectionWrapper : ISshConnection
{
    private readonly ISshClient _client;
    private int _missedKeepAlives;
    
    public string ConnectionId { get; } = Ulid.NewUlid().ToString();
    public bool IsConnected => _client.IsConnected;
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastUsedAt { get; private set; } = DateTimeOffset.UtcNow;
    public int MissedKeepAlives => _missedKeepAlives;
    
    public SshConnectionWrapper(ISshClient client)
    {
        _client = client;
    }
    
    public async Task<SshCommandResult> ExecuteAsync(
        string command,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        MarkUsed();
        return await _client.ExecuteAsync(command, timeout, ct);
    }
    
    public ISftpChannel OpenSftp() => _client.OpenSftp();
    
    public async Task ReconnectAsync(CancellationToken ct = default)
    {
        await _client.DisconnectAsync();
        await _client.ConnectAsync(ct);
        ResetKeepAliveCounter();
    }
    
    public void MarkUsed() => LastUsedAt = DateTimeOffset.UtcNow;
    
    public void IncrementMissedKeepAlive() => 
        Interlocked.Increment(ref _missedKeepAlives);
    
    public void ResetKeepAliveCounter() => 
        Interlocked.Exchange(ref _missedKeepAlives, 0);
    
    public async ValueTask DisposeAsync()
    {
        await _client.DisconnectAsync();
        _client.Dispose();
    }
}
```

### Implementation Checklist

| # | Requirement | Test | Impl |
|---|-------------|------|------|
| 1 | Pool tracks min/max connection limits | ⬜ | ⬜ |
| 2 | Acquire timeout throws after configured duration | ⬜ | ⬜ |
| 3 | Idle connections returned before creating new | ⬜ | ⬜ |
| 4 | Unhealthy idle connections disposed on acquire | ⬜ | ⬜ |
| 5 | Release returns connection to idle queue | ⬜ | ⬜ |
| 6 | Release disposes unhealthy connections | ⬜ | ⬜ |
| 7 | Health checks run at configured interval | ⬜ | ⬜ |
| 8 | Keep-alives sent at configured interval | ⬜ | ⬜ |
| 9 | Missed keep-alive counter increments correctly | ⬜ | ⬜ |
| 10 | Connections evicted after max missed keep-alives | ⬜ | ⬜ |
| 11 | Pool state transitions correctly | ⬜ | ⬜ |
| 12 | Drain stops accepting new requests | ⬜ | ⬜ |
| 13 | All connections disposed on DisposeAsync | ⬜ | ⬜ |
| 14 | ConnectionAcquiredEvent published | ⬜ | ⬜ |
| 15 | ConnectionReleasedEvent published | ⬜ | ⬜ |
| 16 | PoolExhaustedEvent published on timeout | ⬜ | ⬜ |
| 17 | PoolStatus reports accurate counts | ⬜ | ⬜ |
| 18 | Thread-safe concurrent access | ⬜ | ⬜ |

### Rollout Plan

1. **Tests first**: Unit tests for pool behavior, health checks, keep-alive logic
2. **Domain models**: Events, states, metrics records
3. **Application interfaces**: ISshConnectionPool, ISshConnection, IHealthChecker, IKeepAliveManager
4. **Infrastructure impl**: SshConnectionPool, HealthChecker, KeepAliveManager, SshConnectionWrapper
5. **Integration tests**: Connection pooling under load, timeout behavior, health transitions
6. **DI registration**: Register pool as singleton per target, health/keep-alive as transient

**End of Task 030.a Specification**