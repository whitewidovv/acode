# Task 050.b: DB Access Layer + Connection Management

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 2 – Persistence + Reliability Core  
**Dependencies:** Task 050 (Database Foundation)  

---

## Description

Task 050.b implements the database access layer and connection management for SQLite and PostgreSQL. This includes connection factories, pooling, transactions, and error handling.

The access layer provides a clean abstraction over raw ADO.NET. Application code interacts with interfaces. Infrastructure provides implementations. This separation enables testing and flexibility.

Connection management handles the lifecycle of database connections. Connections are expensive to create. The layer pools and reuses them. Proper disposal prevents resource leaks.

SQLite connection handling is straightforward but has nuances. SQLite supports one writer at a time. WAL mode improves concurrency. Busy timeouts handle contention. The layer configures these correctly.

PostgreSQL connection handling uses proper pooling. Npgsql provides built-in pooling. The layer configures pool size, timeouts, and lifetimes. Connection health is monitored.

Transaction management scopes multiple operations. Begin, commit, and rollback are explicit. Nested transactions are not supported. Savepoints provide partial rollback.

The unit of work pattern coordinates changes. Multiple repositories share a transaction. Commit applies all changes. Rollback reverts all changes. This ensures consistency.

Error handling translates database exceptions. Connection failures, constraint violations, and timeouts have specific handling. Transient errors trigger retries. Permanent errors fail fast.

Logging captures database operations. Query execution is logged at debug level. Errors are logged at error level. Connection events are logged. Secrets are never logged.

The layer supports both synchronous and asynchronous operations. Async is preferred for I/O-bound work. Sync is available when needed. Cancellation tokens are respected.

Configuration comes from multiple sources. Connection strings in config file. Environment variables for secrets. Programmatic overrides for testing. Precedence is well-defined.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Access Layer | Database abstraction |
| Connection Factory | Creates connections |
| Connection Pool | Reusable connections |
| Transaction | Atomic operation scope |
| Unit of Work | Change coordination |
| Savepoint | Partial transaction |
| Busy Timeout | Wait for SQLite lock |
| WAL Mode | Write-Ahead Logging |
| Npgsql | PostgreSQL driver |
| ADO.NET | .NET database API |
| Command Timeout | Query time limit |
| Connection Lifetime | Max age before recycle |
| Retry Policy | Transient error handling |
| Transient Error | Temporary failure |
| Disposed | Resource released |

---

## Out of Scope

The following items are explicitly excluded from Task 050.b:

- **Schema layout** - Task 050.a
- **Migration runner** - Task 050.c
- **Health checks** - Task 050.d
- **Backup/export** - Task 050.e
- **ORM features** - Raw SQL only
- **Query building** - Manual SQL
- **Stored procedures** - Not used
- **Read replicas** - Not supported
- **Distributed transactions** - Not supported
- **Connection encryption** - SSL config only

---

## Functional Requirements

### Connection Factory

- FR-001: IConnectionFactory MUST exist
- FR-002: Factory MUST create connections
- FR-003: Factory MUST configure connection
- FR-004: Factory MUST be injectable

### SQLite Factory

- FR-005: SqliteConnectionFactory MUST exist
- FR-006: MUST create SQLiteConnection
- FR-007: MUST set database path
- FR-008: MUST enable WAL mode
- FR-009: MUST set busy timeout

### PostgreSQL Factory

- FR-010: PostgresConnectionFactory MUST exist
- FR-011: MUST create NpgsqlConnection
- FR-012: MUST use connection string
- FR-013: MUST configure pooling
- FR-014: MUST set command timeout

### Connection Pool

- FR-015: SQLite MUST NOT pool (single file)
- FR-016: PostgreSQL MUST pool
- FR-017: Pool size MUST be configurable
- FR-018: Min pool size: 2
- FR-019: Max pool size: default 10

### Connection Lifecycle

- FR-020: Connections MUST be disposed
- FR-021: Using pattern MUST be used
- FR-022: Lifetime MUST be configurable
- FR-023: Default lifetime: 5 minutes

### Transaction Support

- FR-024: BeginTransaction MUST work
- FR-025: Commit MUST work
- FR-026: Rollback MUST work
- FR-027: IsolationLevel MUST be configurable
- FR-028: Default: Serializable (SQLite)

### Unit of Work

- FR-029: IUnitOfWork MUST exist
- FR-030: SaveChangesAsync MUST commit
- FR-031: Rollback on dispose MUST work
- FR-032: Transaction MUST be shared

### Command Execution

- FR-033: ExecuteNonQueryAsync MUST work
- FR-034: ExecuteScalarAsync MUST work
- FR-035: ExecuteReaderAsync MUST work
- FR-036: Parameters MUST be supported

### Error Handling

- FR-037: SqliteException MUST be caught
- FR-038: NpgsqlException MUST be caught
- FR-039: Transient MUST retry
- FR-040: Permanent MUST throw

### Retry Policy

- FR-041: Max retries: 3
- FR-042: Base delay: 100ms
- FR-043: Exponential backoff MUST apply
- FR-044: Max delay: 5 seconds

### Logging

- FR-045: Connection open MUST log
- FR-046: Connection close MUST log
- FR-047: Errors MUST log
- FR-048: Secrets MUST NOT log

### Configuration

- FR-049: Config file MUST work
- FR-050: Environment variables MUST work
- FR-051: Connection string parsing MUST work
- FR-052: Validation MUST occur

---

## Non-Functional Requirements

### Performance

- NFR-001: Connection acquire < 10ms pooled
- NFR-002: Connection acquire < 100ms new
- NFR-003: Transaction overhead < 1ms

### Reliability

- NFR-004: No connection leaks
- NFR-005: Proper disposal
- NFR-006: Graceful timeout

### Security

- NFR-007: No secrets in logs
- NFR-008: Parameterized queries
- NFR-009: Connection string protection

### Scalability

- NFR-010: Pool handles load
- NFR-011: Concurrent access
- NFR-012: No bottlenecks

---

## User Manual Documentation

### Overview

The database access layer manages connections to SQLite and PostgreSQL. It handles pooling, transactions, and error recovery automatically.

### Configuration

```yaml
# .agent/config.yml
database:
  local:
    path: .agent/data/workspace.db
    wal_mode: true
    busy_timeout_ms: 5000
    
  remote:
    enabled: true
    connection_string: ${ACODE_PG_CONNECTION}
    # Or component form:
    host: localhost
    port: 5432
    database: acode
    username: ${ACODE_PG_USER}
    password: ${ACODE_PG_PASSWORD}
    
    pool:
      min_size: 2
      max_size: 10
      lifetime_seconds: 300
      
    timeouts:
      connect_seconds: 5
      command_seconds: 30
      
  retry:
    max_attempts: 3
    base_delay_ms: 100
    max_delay_ms: 5000
```

### Environment Variables

```bash
# PostgreSQL connection
export ACODE_PG_CONNECTION="Host=localhost;Database=acode;Username=user;Password=secret"

# Or individual components
export ACODE_PG_HOST=localhost
export ACODE_PG_PORT=5432
export ACODE_PG_DATABASE=acode
export ACODE_PG_USER=user
export ACODE_PG_PASSWORD=secret
```

### Connection Status

```bash
$ acode db connections

Connection Status
────────────────────────────────────
SQLite (Local):
  Path: .agent/data/workspace.db
  Status: Connected
  WAL Mode: Enabled
  Busy Timeout: 5000ms
  
PostgreSQL (Remote):
  Host: localhost:5432
  Database: acode
  Status: Connected
  Pool: 2/10 active
  Avg Acquire: 3ms
```

### Transaction Usage

For developers integrating with the access layer:

```csharp
// Using unit of work
await using var uow = await _unitOfWorkFactory.CreateAsync(ct);

try
{
    await _chatRepository.CreateAsync(chat, uow, ct);
    await _messageRepository.CreateAsync(message, uow, ct);
    await uow.CommitAsync(ct);
}
catch
{
    await uow.RollbackAsync(ct);
    throw;
}
```

### Troubleshooting

#### Connection Pool Exhausted

**Problem:** All connections in use

**Solutions:**
1. Increase max_pool_size
2. Check for connection leaks
3. Add connection timeouts

#### SQLite Busy

**Problem:** Database is locked

**Solutions:**
1. Increase busy_timeout_ms
2. Check for long transactions
3. Enable WAL mode

#### Connection Timeout

**Problem:** Cannot connect

**Solutions:**
1. Check network
2. Verify credentials
3. Check firewall

---

## Acceptance Criteria

### Factory

- [ ] AC-001: IConnectionFactory exists
- [ ] AC-002: SQLite factory works
- [ ] AC-003: PostgreSQL factory works

### SQLite

- [ ] AC-004: Connection creates
- [ ] AC-005: WAL enabled
- [ ] AC-006: Busy timeout set

### PostgreSQL

- [ ] AC-007: Connection creates
- [ ] AC-008: Pooling works
- [ ] AC-009: Timeout works

### Transactions

- [ ] AC-010: Begin works
- [ ] AC-011: Commit works
- [ ] AC-012: Rollback works

### Unit of Work

- [ ] AC-013: Creates transaction
- [ ] AC-014: Shares transaction
- [ ] AC-015: Disposes properly

### Error Handling

- [ ] AC-016: Retries transient
- [ ] AC-017: Fails permanent
- [ ] AC-018: Logs errors

### Config

- [ ] AC-019: File config works
- [ ] AC-020: Env vars work
- [ ] AC-021: Validation works

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Database/Access/
├── SqliteFactoryTests.cs
│   ├── Should_Create_Connection()
│   ├── Should_Enable_Wal()
│   └── Should_Set_BusyTimeout()
│
├── PostgresFactoryTests.cs
│   ├── Should_Parse_ConnectionString()
│   ├── Should_Configure_Pool()
│   └── Should_Set_Timeout()
│
├── UnitOfWorkTests.cs
│   ├── Should_Share_Transaction()
│   ├── Should_Commit()
│   └── Should_Rollback_On_Dispose()
│
└── RetryPolicyTests.cs
    ├── Should_Retry_Transient()
    └── Should_Fail_Permanent()
```

### Integration Tests

```
Tests/Integration/Database/
├── SqliteConnectionTests.cs
│   ├── Should_Execute_Query()
│   └── Should_Handle_Concurrent()
│
└── PostgresConnectionTests.cs
    ├── Should_Pool_Connections()
    └── Should_Handle_Timeout()
```

### E2E Tests

```
Tests/E2E/Database/
├── ConnectionE2ETests.cs
│   └── Should_Connect_Both_Databases()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Pooled acquire | 5ms | 10ms |
| New connection | 50ms | 100ms |
| Transaction begin | 0.5ms | 1ms |

---

## User Verification Steps

### Scenario 1: SQLite Connection

1. Delete database file
2. Run acode command
3. Verify: Database created

### Scenario 2: PostgreSQL Connection

1. Configure remote
2. Check connections
3. Verify: Pool active

### Scenario 3: Transaction

1. Start operation
2. Force error
3. Verify: Rolled back

### Scenario 4: Pool Exhaustion

1. Set small pool
2. Create many connections
3. Verify: Waits for available

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Application/
├── Persistence/
│   ├── IConnectionFactory.cs
│   ├── IUnitOfWork.cs
│   └── IUnitOfWorkFactory.cs
│
src/AgenticCoder.Infrastructure/
├── Persistence/
│   ├── Connections/
│   │   ├── SqliteConnectionFactory.cs
│   │   └── PostgresConnectionFactory.cs
│   ├── Transactions/
│   │   ├── UnitOfWork.cs
│   │   └── UnitOfWorkFactory.cs
│   └── Retry/
│       └── DatabaseRetryPolicy.cs
```

### IConnectionFactory Interface

```csharp
namespace AgenticCoder.Application.Persistence;

public interface IConnectionFactory
{
    Task<IDbConnection> CreateAsync(CancellationToken ct);
    DatabaseType DatabaseType { get; }
}
```

### IUnitOfWork Interface

```csharp
namespace AgenticCoder.Application.Persistence;

public interface IUnitOfWork : IAsyncDisposable
{
    IDbConnection Connection { get; }
    IDbTransaction Transaction { get; }
    Task CommitAsync(CancellationToken ct);
    Task RollbackAsync(CancellationToken ct);
}
```

### SqliteConnectionFactory

```csharp
namespace AgenticCoder.Infrastructure.Persistence.Connections;

public sealed class SqliteConnectionFactory : IConnectionFactory
{
    private readonly DatabaseOptions _options;
    
    public DatabaseType DatabaseType => DatabaseType.Sqlite;
    
    public async Task<IDbConnection> CreateAsync(CancellationToken ct)
    {
        var path = _options.Local.Path;
        var connectionString = $"Data Source={path}";
        
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(ct);
        
        // Configure SQLite
        await ExecutePragmaAsync(connection, "journal_mode", "WAL", ct);
        await ExecutePragmaAsync(connection, "busy_timeout", 
            _options.Local.BusyTimeoutMs.ToString(), ct);
            
        return connection;
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-DB-ACC-001 | Connection failed |
| ACODE-DB-ACC-002 | Pool exhausted |
| ACODE-DB-ACC-003 | Transaction failed |
| ACODE-DB-ACC-004 | Command timeout |
| ACODE-DB-ACC-005 | Constraint violation |

### Logging Fields

```json
{
  "event": "database_connection_opened",
  "database_type": "sqlite",
  "path": ".agent/data/workspace.db",
  "duration_ms": 12
}
```

### Implementation Checklist

1. [ ] Create IConnectionFactory
2. [ ] Create IUnitOfWork
3. [ ] Implement SQLite factory
4. [ ] Implement PostgreSQL factory
5. [ ] Implement unit of work
6. [ ] Add retry policy
7. [ ] Add logging
8. [ ] Add configuration
9. [ ] Write unit tests
10. [ ] Write integration tests

### Rollout Plan

1. **Phase 1:** Interfaces
2. **Phase 2:** SQLite factory
3. **Phase 3:** PostgreSQL factory
4. **Phase 4:** Unit of work
5. **Phase 5:** Retry policy
6. **Phase 6:** Configuration

---

**End of Task 050.b Specification**