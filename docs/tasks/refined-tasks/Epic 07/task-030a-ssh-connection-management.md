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