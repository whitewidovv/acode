# Task 050.b: DB Access Layer + Connection Management

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 2 – Persistence + Reliability Core  
**Dependencies:** Task 050 (Database Foundation)  

---

## Description

### Business Value and ROI

Task 050.b implements the database access layer and connection management for SQLite and PostgreSQL. This foundational infrastructure component directly impacts application reliability, performance, and developer productivity across all features that interact with persistent storage.

**Quantified ROI Analysis:**

| Investment Area | Annual Value | Calculation Basis |
|----------------|--------------|-------------------|
| **Connection Reliability** | $72,000 | Proper pooling prevents 6 outages/year × 8 hours MTTR × $1,500/hour |
| **Developer Productivity** | $48,000 | Abstraction saves 20 min/day × 4 developers × $60/hr × 200 days |
| **Reduced Debugging Time** | $36,000 | Consistent error handling saves 3 hours/incident × 50 incidents × $80/hr |
| **Memory Leak Prevention** | $30,000 | Proper disposal prevents 4 memory issues/year × 25 hours resolution × $300/hr |
| **Performance Optimization** | $24,000 | Pooled connections save 50ms/request × 100K requests/day × compute costs |
| **Total Annual ROI** | **$210,000** | |

The access layer serves as the single point of contact between application code and database connectivity. Without a well-designed access layer, each feature reimplements connection handling, leading to inconsistent behavior, resource leaks, and maintenance burden. With a unified access layer, all database operations follow the same patterns for connection lifecycle, error handling, and transaction management.

### Technical Architecture

The database access layer follows Clean Architecture principles, providing clear separation between abstractions (Application layer) and implementations (Infrastructure layer):

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         APPLICATION LAYER                                    │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                        Interfaces                                     │   │
│  │  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐   │   │
│  │  │ IConnectionFactory│  │   IUnitOfWork    │  │IUnitOfWorkFactory│   │   │
│  │  │                  │  │                  │  │                  │   │   │
│  │  │ +CreateAsync()   │  │ +Connection      │  │ +CreateAsync()   │   │   │
│  │  │ +DatabaseType    │  │ +Transaction     │  │                  │   │   │
│  │  │                  │  │ +CommitAsync()   │  │                  │   │   │
│  │  │                  │  │ +RollbackAsync() │  │                  │   │   │
│  │  └──────────────────┘  └──────────────────┘  └──────────────────┘   │   │
│  └─────────────────────────────────────────────────────────────────────┘    │
├─────────────────────────────────────────────────────────────────────────────┤
│                        INFRASTRUCTURE LAYER                                  │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                     Implementations                                   │   │
│  │  ┌──────────────────────────────────────────────────────────────┐   │   │
│  │  │                    Connection Factories                        │   │   │
│  │  │  ┌─────────────────────┐    ┌─────────────────────────┐      │   │   │
│  │  │  │SqliteConnectionFactory│   │PostgresConnectionFactory│      │   │   │
│  │  │  │                     │    │                         │      │   │   │
│  │  │  │ - Configure WAL     │    │ - Configure pooling     │      │   │   │
│  │  │  │ - Set busy timeout  │    │ - Set SSL mode          │      │   │   │
│  │  │  │ - Enable pragmas    │    │ - Set command timeout   │      │   │   │
│  │  │  └─────────────────────┘    └─────────────────────────┘      │   │   │
│  │  └──────────────────────────────────────────────────────────────┘   │   │
│  │                                                                       │   │
│  │  ┌──────────────────────────────────────────────────────────────┐   │   │
│  │  │                    Transaction Management                      │   │   │
│  │  │  ┌─────────────────────┐    ┌─────────────────────────┐      │   │   │
│  │  │  │     UnitOfWork      │    │   UnitOfWorkFactory     │      │   │   │
│  │  │  │                     │    │                         │      │   │   │
│  │  │  │ - Wraps connection  │    │ - Creates UoW instances │      │   │   │
│  │  │  │ - Manages transaction│    │ - Resolves factory     │      │   │   │
│  │  │  │ - Tracks commit     │    │ - Configures isolation  │      │   │   │
│  │  │  └─────────────────────┘    └─────────────────────────┘      │   │   │
│  │  └──────────────────────────────────────────────────────────────┘   │   │
│  │                                                                       │   │
│  │  ┌──────────────────────────────────────────────────────────────┐   │   │
│  │  │                    Resilience & Logging                        │   │   │
│  │  │  ┌─────────────────────┐    ┌─────────────────────────┐      │   │   │
│  │  │  │DatabaseRetryPolicy  │    │   ConnectionLogger      │      │   │   │
│  │  │  │                     │    │                         │      │   │   │
│  │  │  │ - Exponential backoff│   │ - Open/close events     │      │   │   │
│  │  │  │ - Transient detection│   │ - Error logging         │      │   │   │
│  │  │  │ - Max attempts config│   │ - Secret filtering      │      │   │   │
│  │  │  └─────────────────────┘    └─────────────────────────┘      │   │   │
│  │  └──────────────────────────────────────────────────────────────┘   │   │
│  └─────────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Connection Lifecycle Flow:**

```
┌────────────────────────────────────────────────────────────────────────────┐
│                     CONNECTION LIFECYCLE                                    │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   REQUEST                                                                   │
│      │                                                                      │
│      ▼                                                                      │
│   ┌──────────────┐    ┌──────────────┐    ┌──────────────┐               │
│   │   Factory    │───►│   Acquire    │───►│   Configure  │               │
│   │  CreateAsync │    │  Connection  │    │   Pragmas    │               │
│   └──────────────┘    └──────────────┘    └──────────────┘               │
│                              │                    │                        │
│                              │                    ▼                        │
│                       ┌──────┴──────┐    ┌──────────────┐               │
│                       │             │    │    Return    │               │
│                       │  SQLite     │    │  Connection  │               │
│                       │  (new)      │    └──────────────┘               │
│                       │             │           │                        │
│                       │  PostgreSQL │           ▼                        │
│                       │  (from pool)│    ┌──────────────┐               │
│                       └─────────────┘    │   Execute    │               │
│                                          │   Commands   │               │
│   RESPONSE                               └──────────────┘               │
│      ▲                                          │                        │
│      │                                          ▼                        │
│   ┌──────────────┐    ┌──────────────┐    ┌──────────────┐               │
│   │   Complete   │◄───│   Dispose    │◄───│    Close     │               │
│   │   Request    │    │   or Return  │    │  Connection  │               │
│   └──────────────┘    │   to Pool    │    └──────────────┘               │
│                       └──────────────┘                                    │
│                              │                                            │
│                       ┌──────┴──────┐                                    │
│                       │             │                                    │
│                       │  SQLite     │                                    │
│                       │  (close)    │                                    │
│                       │             │                                    │
│                       │  PostgreSQL │                                    │
│                       │  (to pool)  │                                    │
│                       └─────────────┘                                    │
│                                                                             │
└────────────────────────────────────────────────────────────────────────────┘
```

**SQLite-Specific Configuration:**

SQLite connection handling has unique requirements that the access layer addresses:

1. **WAL Mode (Write-Ahead Logging):** Enables concurrent readers while writing. Without WAL, SQLite uses rollback journal mode which blocks all readers during writes.

2. **Busy Timeout:** When another process holds the write lock, SQLite waits for the configured timeout before returning SQLITE_BUSY. Default: 5000ms.

3. **Single Writer:** SQLite supports only one writer at a time. The access layer doesn't pool SQLite connections because a single database file can only have one active write connection.

4. **PRAGMA Configuration:**
   - `journal_mode = WAL` - Enable write-ahead logging
   - `busy_timeout = 5000` - Wait 5 seconds for locks
   - `foreign_keys = ON` - Enforce foreign key constraints
   - `synchronous = NORMAL` - Balance durability and performance

**PostgreSQL-Specific Configuration:**

PostgreSQL connection handling leverages Npgsql's built-in pooling:

1. **Connection Pooling:** Npgsql maintains a pool of open connections. Acquiring a pooled connection is ~5ms vs ~50ms for a new connection.

2. **Pool Configuration:**
   - `Minimum Pool Size = 2` - Keep at least 2 connections warm
   - `Maximum Pool Size = 10` - Cap total connections
   - `Connection Lifetime = 300` - Recycle connections after 5 minutes

3. **SSL/TLS:** Production deployments require encrypted connections. Modes: Disable, Prefer, Require, VerifyFull.

4. **Command Timeout:** Default 30 seconds. Prevents runaway queries from holding connections indefinitely.

### Transaction Management

The Unit of Work pattern coordinates multiple repository operations within a single transaction:

```
┌────────────────────────────────────────────────────────────────────────────┐
│                     UNIT OF WORK PATTERN                                    │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   Service Method                                                            │
│   ┌────────────────────────────────────────────────────────────────────┐   │
│   │  async Task CreateChatWithMessages(chat, messages)                  │   │
│   │  {                                                                  │   │
│   │      await using var uow = await _uowFactory.CreateAsync(ct);       │   │
│   │                                                                     │   │
│   │      try                                                            │   │
│   │      {                                                              │   │
│   │          // All operations share same transaction                   │   │
│   │          await _chatRepo.CreateAsync(chat, uow, ct);               │   │
│   │          foreach (var msg in messages)                              │   │
│   │              await _messageRepo.CreateAsync(msg, uow, ct);         │   │
│   │                                                                     │   │
│   │          await uow.CommitAsync(ct);  // All or nothing             │   │
│   │      }                                                              │   │
│   │      catch                                                          │   │
│   │      {                                                              │   │
│   │          await uow.RollbackAsync(ct);  // Undo all                 │   │
│   │          throw;                                                     │   │
│   │      }                                                              │   │
│   │  }                                                                  │   │
│   └────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│   Timeline:                                                                 │
│   ──────────────────────────────────────────────────────────────────────   │
│   │ Create UoW │ Insert Chat │ Insert Msg 1 │ Insert Msg 2 │  Commit  │   │
│   └────────────┴─────────────┴──────────────┴──────────────┴──────────┘   │
│   │◄─────────────────── Transaction Scope ──────────────────────────►│   │
│                                                                             │
└────────────────────────────────────────────────────────────────────────────┘
```

### Retry Policy for Transient Failures

Database operations can fail due to transient issues (network blips, lock contention, pool exhaustion). The retry policy handles these gracefully:

```
┌────────────────────────────────────────────────────────────────────────────┐
│                     RETRY POLICY WITH EXPONENTIAL BACKOFF                   │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   Attempt 1 ──► Transient Error ──► Wait 100ms ──►                        │
│   Attempt 2 ──► Transient Error ──► Wait 200ms ──►                        │
│   Attempt 3 ──► Transient Error ──► Wait 400ms ──►                        │
│   Attempt 4 ──► Success ──► Return Result                                  │
│                   OR                                                        │
│   Attempt 4 ──► Still Failing ──► Throw AcodeDbException                  │
│                                                                             │
│   Transient Errors (retry):          Permanent Errors (fail immediately):  │
│   • Connection timeout               • Constraint violation                 │
│   • Pool exhausted                   • Invalid SQL syntax                   │
│   • Network unreachable              • Authentication failure               │
│   • SQLite busy                      • Permission denied                    │
│   • Deadlock detected                • Object not found                     │
│                                                                             │
└────────────────────────────────────────────────────────────────────────────┘
```

### Integration Points

Task 050.b integrates with multiple system components:

**Task 050 (Database Foundation):**
- Uses configuration from DatabaseOptions
- Implements interfaces defined in foundation
- Shares error code namespace ACODE-DB-*

**Task 050.a (Schema Layout):**
- Connections are used to execute migrations
- Transaction support enables atomic migration application
- Factory creates connections for schema operations

**Task 050.c (Migration Runner):**
- Uses IConnectionFactory to get database connection
- Uses transaction support for atomic migrations
- Retry policy handles migration failures

**Task 050.d (Health Checks):**
- Health checks use factory to test connectivity
- Pool status exposed through health endpoints
- Connection metrics collected for monitoring

**Application Services:**
- All repositories receive IUnitOfWork for transactions
- Services use IConnectionFactory for read-only queries
- Error handling translates to domain exceptions

### Constraints and Limitations

**Constraint 1: No ORM Features**
This layer provides raw connection and transaction management only. No entity mapping, change tracking, or query generation. Those concerns belong to higher-level abstractions.

**Constraint 2: Single Database at a Time**
While both SQLite and PostgreSQL factories exist, a given operation uses one or the other based on configuration. No cross-database transactions.

**Constraint 3: No Distributed Transactions**
Transactions are limited to a single database connection. Distributed transactions (spanning multiple databases) are not supported.

**Constraint 4: Synchronous Blocking on SQLite**
SQLite write operations are synchronous at the file level. The async API wraps this but doesn't provide true async I/O.

**Constraint 5: Connection String Secrets**
Connection strings may contain passwords. The layer ensures these are never logged but cannot prevent memory inspection.

### Trade-offs and Alternatives

**Trade-off 1: Manual SQL vs ORM**

*Decision:* Use raw SQL with parameterized queries, no ORM.

*Rationale:* Full control over queries, no abstraction leakage, easier debugging of SQL issues. Acode's data model is simple enough that ORM benefits don't outweigh the complexity.

*Alternative Considered:* Dapper or EF Core for object mapping. Rejected because the overhead of learning and maintaining ORM configuration exceeds the benefit for this use case.

**Trade-off 2: Single Connection Factory vs Provider-Specific**

*Decision:* Separate SqliteConnectionFactory and PostgresConnectionFactory.

*Rationale:* Each database has vastly different configuration needs (WAL vs pooling, PRAGMA vs SET commands). A single factory would require complex conditional logic.

*Alternative Considered:* Abstract base factory with provider-specific overrides. Rejected because configuration differs too much to share meaningful base logic.

**Trade-off 3: Explicit Transaction vs Implicit**

*Decision:* Require explicit transaction management via Unit of Work.

*Rationale:* Makes transaction boundaries visible in code. Prevents accidental auto-commit behavior. Aligns with Clean Architecture's explicit dependency principle.

*Alternative Considered:* Automatic transaction wrapping per request. Rejected because it hides important behavior and makes debugging harder.

---

## Use Cases

### Use Case 1: DevBot Maintains Data Consistency During Complex Operations

**Persona:** DevBot, an AI developer assistant performing multi-step operations on behalf of a user.

**Context:** A user asks DevBot to "create a new project with initial configuration and first chat thread." This requires inserting records into multiple tables atomically.

**Before (Without Unit of Work Pattern):**
```csharp
// Each operation commits independently
await _projectRepo.CreateAsync(project);  // COMMITTED
await _configRepo.CreateAsync(config);    // COMMITTED
await _chatRepo.CreateAsync(chat);        // FAILS - database full!

// Result: Project and config exist, but chat doesn't
// Data is in inconsistent state
// User sees partial results, confusing experience
// Manual cleanup required
```

**After (With Task 050.b Unit of Work):**
```csharp
await using var uow = await _uowFactory.CreateAsync(ct);

try
{
    await _projectRepo.CreateAsync(project, uow, ct);  // Pending
    await _configRepo.CreateAsync(config, uow, ct);    // Pending
    await _chatRepo.CreateAsync(chat, uow, ct);        // Fails!
    
    await uow.CommitAsync(ct);  // Never reached
}
catch
{
    await uow.RollbackAsync(ct);  // All changes reverted
    throw;
}

// Result: Nothing committed, database unchanged
// Clean error reported to user
// No manual cleanup needed
```

**Metrics:**
- Data consistency incidents: 5/month → 0/month (100% reduction)
- Manual cleanup time: 2 hours/incident × $80/hr = $160 → $0
- User confusion tickets: 8/month → 0/month
- Annual savings: $19,200 (cleanup) + $9,600 (support) = **$28,800/year**

---

### Use Case 2: Jordan Handles Network Instability During Remote Sync

**Persona:** Jordan, a backend developer working on sync features between local SQLite and remote PostgreSQL.

**Context:** Users on unreliable networks (coffee shops, trains) experience intermittent connection drops during sync operations. Without retry logic, every network blip causes sync failures.

**Before (Without Retry Policy):**
```csharp
// First attempt, network blip
try
{
    await _pgConnection.ExecuteAsync(insertSql, data);
}
catch (NpgsqlException ex) when (ex.Message.Contains("connection"))
{
    // Immediate failure, user sees error
    throw new SyncException("Sync failed: network error");
}

// User experience:
// - Sync fails 15% of the time on unstable networks
// - Users manually retry, frustrating experience
// - Support tickets pile up
```

**After (With Task 050.b Retry Policy):**
```csharp
// Retry policy handles transient failures
var result = await _retryPolicy.ExecuteAsync(async ct =>
{
    await using var conn = await _factory.CreateAsync(ct);
    return await conn.ExecuteAsync(insertSql, data);
}, cancellationToken);

// Internal behavior:
// Attempt 1: NetworkException → Wait 100ms
// Attempt 2: NetworkException → Wait 200ms  
// Attempt 3: Success → Return result

// User experience:
// - Sync succeeds after brief delay
// - User unaware of transient issues
// - No manual intervention needed
```

**Metrics:**
- Sync failure rate on unstable networks: 15% → 1.5% (90% reduction)
- Manual retry attempts by users: 50/day → 5/day
- Support tickets for sync issues: 20/month → 2/month
- Developer time investigating: 10 hours/month → 1 hour/month
- Annual savings: (9 hrs × $80 × 12) + (18 tickets × $50 × 12) = **$19,440/year**

---

### Use Case 3: Alex Prevents Memory Leaks in Long-Running Server

**Persona:** Alex, a DevOps engineer monitoring Acode deployments in production.

**Context:** Without proper connection disposal, long-running Acode instances accumulate undisposed connections, leading to memory growth and eventual out-of-memory crashes.

**Before (Without Proper Connection Management):**
```csharp
// Forgotten disposal - connection leaks
public async Task<Chat> GetChatAsync(string id)
{
    var conn = await _factory.CreateAsync(ct);  // Acquired
    var result = await conn.QueryAsync<Chat>(sql, new { id });
    return result.FirstOrDefault();
    // Connection never disposed! Leak.
}

// Production behavior over 7 days:
// Day 1: 50 connections, 100MB memory
// Day 3: 500 connections, 400MB memory
// Day 5: 2000 connections, 1.2GB memory
// Day 7: OOM crash, service restart
```

**After (With Task 050.b Connection Factory + Using Pattern):**
```csharp
// Factory enforces proper lifecycle
public async Task<Chat> GetChatAsync(string id, CancellationToken ct)
{
    await using var conn = await _factory.CreateAsync(ct);  // Acquired
    var result = await conn.QueryAsync<Chat>(sql, new { id });
    return result.FirstOrDefault();
    // Connection disposed automatically on exit
}

// Production behavior:
// Stable at 10-50 connections (pool ceiling)
// Memory stable at 150MB
// No crashes, no manual restarts
```

**Metrics:**
- Production crashes from memory leaks: 2/month → 0/month
- MTTR per crash: 30 minutes × 2 = 60 min/month → 0
- User impact per crash: ~100 users affected
- Infrastructure cost of oversized instances: $500/month → $200/month
- Annual savings: (2 crashes × $1,500 impact × 12) + ($300 infra × 12) = **$39,600/year**

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

## Assumptions

### Technical Assumptions

1. **ADO.NET Providers** - Microsoft.Data.Sqlite and Npgsql packages provide database connectivity
2. **Connection Factory** - IConnectionFactory creates and configures database connections
3. **Connection Lifetime** - Connections are short-lived; opened for query, closed immediately
4. **No Connection Pooling SQLite** - SQLite uses single connection; no pooling needed
5. **PostgreSQL Pooling** - Npgsql built-in pooling handles connection reuse automatically
6. **Thread Safety** - Connection factory is thread-safe; individual connections are not
7. **Async Support** - All data access methods are async with proper cancellation token support

### Configuration Assumptions

8. **Connection Strings** - Database connection info read from agent-config.yml storage section
9. **SSL Configuration** - PostgreSQL SSL mode configurable: disable, prefer, require, verify-full
10. **Timeout Settings** - Connection and command timeouts configurable with sensible defaults
11. **Retry Logic** - Transient failure retries with exponential backoff for network issues
12. **Health Endpoints** - Connection health exposed via Task 050.d health check framework

### Operational Assumptions

13. **Lazy Initialization** - Database connections created on first use, not at startup
14. **Graceful Shutdown** - Open connections properly disposed on application shutdown
15. **Transaction Scope** - Unit of work pattern manages transaction boundaries
16. **Exception Mapping** - Database-specific exceptions wrapped in domain exceptions
17. **Logging Integration** - Connection events logged for diagnostics (open, close, errors)
18. **No Raw SQL Logging** - Query text not logged by default to prevent credential exposure

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

- NFR-001: Pooled connection acquisition MUST complete in < 10ms (p95)
- NFR-002: New connection creation MUST complete in < 100ms (p95)
- NFR-003: Transaction begin overhead MUST be < 1ms
- NFR-004: Transaction commit overhead MUST be < 5ms for small transactions (< 100 operations)
- NFR-005: Connection disposal MUST complete in < 1ms
- NFR-006: Retry policy total wait MUST NOT exceed configured max delay (default 5 seconds)
- NFR-007: Pool warmup (min connections) SHOULD complete during application startup

### Reliability

- NFR-008: Zero connection leaks under normal operation (verified by tests holding 1000+ cycles)
- NFR-009: All connections MUST be properly disposed even when exceptions occur
- NFR-010: Graceful timeout handling for connection acquisition (configurable, default 30s)
- NFR-011: Graceful timeout handling for command execution (configurable, default 30s)
- NFR-012: Failed connection attempts MUST NOT corrupt connection pool state
- NFR-013: Transaction rollback MUST succeed even after partial failure
- NFR-014: Application shutdown MUST wait for active transactions to complete (max 10s)

### Security

- NFR-015: Connection strings MUST NOT appear in logs (passwords must be redacted)
- NFR-016: All queries MUST use parameterized statements (no string concatenation)
- NFR-017: Connection strings MUST be protected from memory dumps where possible
- NFR-018: PostgreSQL connections MUST use SSL in production (SslMode >= Require)
- NFR-019: Failed authentication attempts MUST be logged for security monitoring
- NFR-020: Database credentials MUST support rotation without application restart

### Scalability

- NFR-021: Connection pool MUST handle burst load up to 10x normal without failure
- NFR-022: Concurrent access from multiple threads MUST be safe (thread-safe factories)
- NFR-023: Pool size MUST be configurable without code changes (via configuration)
- NFR-024: Connection lifetime MUST prevent stale connection accumulation
- NFR-025: Factory instantiation MUST be singleton-scoped to share pool state

### Maintainability

- NFR-026: All database exceptions MUST be wrapped in domain-specific exceptions with error codes
- NFR-027: Exception messages MUST include correlation IDs for log tracing
- NFR-028: Configuration validation MUST fail fast at startup (not on first use)
- NFR-029: Factory implementations MUST be swappable via dependency injection
- NFR-030: Unit of work pattern MUST support repository composition

### Observability

- NFR-031: Connection open/close events MUST be logged at Debug level
- NFR-032: Connection errors MUST be logged at Error level with full context
- NFR-033: Pool statistics (active, available, total) MUST be exposed for monitoring
- NFR-034: Slow query warnings MUST be logged when execution exceeds threshold (default 1s)
- NFR-035: Retry attempts MUST be logged at Warning level with attempt number

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

## Security Considerations

### Threat 1: Connection String Credential Exposure

**Risk:** Connection strings containing database passwords may be exposed through logs, error messages, stack traces, or memory dumps.

**Attack Scenario:** An attacker gains access to application logs and discovers PostgreSQL connection strings with plaintext passwords: `Host=prod-db;Password=SuperSecret123`. They use these credentials to directly access the production database.

**Mitigation Code:**

```csharp
// Infrastructure/Database/SecureConnectionStringBuilder.cs
namespace Acode.Infrastructure.Database;

public sealed class SecureConnectionStringBuilder
{
    private static readonly Regex PasswordPattern = new(
        @"(password|pwd)\s*=\s*[^;]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    private static readonly Regex UserPattern = new(
        @"(user\s*id|username|uid)\s*=\s*[^;]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    public static string Sanitize(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return connectionString;
        
        var sanitized = PasswordPattern.Replace(connectionString, "$1=***REDACTED***");
        sanitized = UserPattern.Replace(sanitized, "$1=***REDACTED***");
        return sanitized;
    }
    
    public static string BuildFromSecureStore(
        ISecretProvider secretProvider,
        DatabaseOptions options,
        CancellationToken ct)
    {
        // Never log the actual connection string
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = options.Remote.Host,
            Port = options.Remote.Port,
            Database = options.Remote.Database,
            Username = secretProvider.GetSecret("ACODE_PG_USER", ct),
            Password = secretProvider.GetSecret("ACODE_PG_PASSWORD", ct),
            SslMode = options.Remote.SslMode,
            MinPoolSize = options.Remote.Pool.MinSize,
            MaxPoolSize = options.Remote.Pool.MaxSize,
            ConnectionLifetime = options.Remote.Pool.LifetimeSeconds
        };
        
        return builder.ConnectionString;
    }
}

public sealed class SecureConnectionLogger
{
    private readonly ILogger<SecureConnectionLogger> _logger;
    
    public void LogConnectionEvent(string eventType, string connectionString, TimeSpan duration)
    {
        // NEVER log actual connection string
        var sanitized = SecureConnectionStringBuilder.Sanitize(connectionString);
        
        _logger.LogDebug(
            "Database connection {Event}: {ConnectionInfo}, Duration: {Duration}ms",
            eventType,
            sanitized,
            duration.TotalMilliseconds);
    }
}
```

---

### Threat 2: SQL Injection via Unparameterized Queries

**Risk:** User input concatenated directly into SQL strings allows attackers to execute arbitrary database commands.

**Attack Scenario:** A user provides input `'; DROP TABLE conv_chats; --` which, if concatenated into SQL, deletes the entire chat history.

**Mitigation Code:**

```csharp
// Infrastructure/Database/SafeQueryExecutor.cs
namespace Acode.Infrastructure.Database;

public sealed class SafeQueryExecutor
{
    private readonly ILogger<SafeQueryExecutor> _logger;
    
    // DANGEROUS - This pattern is prohibited
    [Obsolete("Use ExecuteParameterizedAsync instead", error: true)]
    public Task<int> ExecuteRawSqlAsync(string sql, CancellationToken ct)
    {
        throw new InvalidOperationException("Raw SQL execution is prohibited. Use parameterized queries.");
    }
    
    // SAFE - Always use parameterized queries
    public async Task<int> ExecuteParameterizedAsync(
        IDbConnection connection,
        string sql,
        object parameters,
        CancellationToken ct)
    {
        // Validate SQL doesn't contain suspicious patterns
        ValidateSqlStructure(sql);
        
        // Dapper automatically parameterizes
        return await connection.ExecuteAsync(
            new CommandDefinition(sql, parameters, cancellationToken: ct));
    }
    
    private static void ValidateSqlStructure(string sql)
    {
        // Check for string concatenation indicators in SQL
        if (sql.Contains("' +") || sql.Contains("+ '") || sql.Contains("\" +"))
        {
            throw new SecurityException(
                "SQL appears to contain string concatenation. Use parameterized queries.");
        }
        
        // Check for multiple statements (possible injection)
        var statementCount = sql.Count(c => c == ';');
        if (statementCount > 1)
        {
            throw new SecurityException(
                "SQL contains multiple statements. This may indicate injection attempt.");
        }
    }
}
```

---

### Threat 3: Connection Pool Denial of Service

**Risk:** An attacker exhausts the connection pool by initiating many long-running operations, preventing legitimate requests from acquiring connections.

**Attack Scenario:** Malicious requests trigger queries that hold connections for extended periods. With a pool max of 10, 10 concurrent malicious requests block all database access for other users.

**Mitigation Code:**

```csharp
// Infrastructure/Database/PoolProtectedConnectionFactory.cs
namespace Acode.Infrastructure.Database;

public sealed class PoolProtectedConnectionFactory : IConnectionFactory
{
    private readonly IConnectionFactory _inner;
    private readonly ILogger<PoolProtectedConnectionFactory> _logger;
    private readonly SemaphoreSlim _acquisitionThrottle;
    private readonly TimeSpan _maxAcquisitionWait;
    private readonly TimeSpan _maxConnectionHold;
    
    public PoolProtectedConnectionFactory(
        IConnectionFactory inner,
        PoolProtectionOptions options,
        ILogger<PoolProtectedConnectionFactory> logger)
    {
        _inner = inner;
        _logger = logger;
        _acquisitionThrottle = new SemaphoreSlim(options.MaxConcurrentAcquisitions);
        _maxAcquisitionWait = TimeSpan.FromSeconds(options.AcquisitionTimeoutSeconds);
        _maxConnectionHold = TimeSpan.FromSeconds(options.MaxHoldTimeSeconds);
    }
    
    public async Task<IDbConnection> CreateAsync(CancellationToken ct)
    {
        // Throttle acquisition to prevent pool exhaustion attacks
        if (!await _acquisitionThrottle.WaitAsync(_maxAcquisitionWait, ct))
        {
            _logger.LogWarning("Connection acquisition throttled - possible DoS attempt");
            throw new DatabaseException("ACODE-DB-ACC-002", "Connection pool busy, please retry");
        }
        
        try
        {
            var connection = await _inner.CreateAsync(ct);
            
            // Wrap connection with timeout enforcement
            return new TimeoutEnforcedConnection(
                connection, 
                _maxConnectionHold,
                () => _acquisitionThrottle.Release());
        }
        catch
        {
            _acquisitionThrottle.Release();
            throw;
        }
    }
    
    public DatabaseType DatabaseType => _inner.DatabaseType;
}

internal sealed class TimeoutEnforcedConnection : IDbConnection
{
    private readonly IDbConnection _inner;
    private readonly CancellationTokenSource _holdTimeout;
    private readonly Action _onDispose;
    
    public TimeoutEnforcedConnection(IDbConnection inner, TimeSpan maxHold, Action onDispose)
    {
        _inner = inner;
        _onDispose = onDispose;
        _holdTimeout = new CancellationTokenSource(maxHold);
        _holdTimeout.Token.Register(() => 
        {
            // Force close if held too long
            try { _inner.Close(); } catch { }
        });
    }
    
    public void Dispose()
    {
        _holdTimeout.Dispose();
        _inner.Dispose();
        _onDispose();
    }
    
    // Delegate all IDbConnection members to _inner...
    public string ConnectionString { get => _inner.ConnectionString; set => _inner.ConnectionString = value; }
    public int ConnectionTimeout => _inner.ConnectionTimeout;
    public string Database => _inner.Database;
    public ConnectionState State => _inner.State;
    public IDbTransaction BeginTransaction() => _inner.BeginTransaction();
    public IDbTransaction BeginTransaction(IsolationLevel il) => _inner.BeginTransaction(il);
    public void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);
    public void Close() => _inner.Close();
    public IDbCommand CreateCommand() => _inner.CreateCommand();
    public void Open() => _inner.Open();
}
```

---

### Threat 4: Transaction Isolation Level Bypass

**Risk:** Incorrect isolation level allows dirty reads, non-repeatable reads, or phantom reads, leading to data inconsistencies or security bypasses.

**Attack Scenario:** A permission check reads user role during transaction. Before commit, another transaction modifies the role. The operation proceeds with outdated permission data.

**Mitigation Code:**

```csharp
// Infrastructure/Database/SecureTransactionFactory.cs
namespace Acode.Infrastructure.Database;

public sealed class SecureTransactionFactory
{
    private readonly ILogger<SecureTransactionFactory> _logger;
    
    // Minimum isolation levels by operation type
    private static readonly Dictionary<OperationType, IsolationLevel> MinimumIsolation = new()
    {
        { OperationType.Read, IsolationLevel.ReadCommitted },
        { OperationType.Write, IsolationLevel.RepeatableRead },
        { OperationType.SecurityCheck, IsolationLevel.Serializable },
        { OperationType.FinancialOperation, IsolationLevel.Serializable }
    };
    
    public IDbTransaction CreateSecureTransaction(
        IDbConnection connection,
        OperationType operationType,
        IsolationLevel? requestedLevel = null)
    {
        var minimumLevel = MinimumIsolation.GetValueOrDefault(operationType, IsolationLevel.ReadCommitted);
        var effectiveLevel = requestedLevel ?? minimumLevel;
        
        // Prevent downgrade attacks
        if (GetIsolationStrength(effectiveLevel) < GetIsolationStrength(minimumLevel))
        {
            _logger.LogWarning(
                "Attempted isolation level downgrade for {Operation}: {Requested} < {Minimum}",
                operationType, effectiveLevel, minimumLevel);
            
            effectiveLevel = minimumLevel;
        }
        
        _logger.LogDebug(
            "Creating transaction with isolation {Level} for {Operation}",
            effectiveLevel, operationType);
        
        return connection.BeginTransaction(effectiveLevel);
    }
    
    private static int GetIsolationStrength(IsolationLevel level) => level switch
    {
        IsolationLevel.ReadUncommitted => 0,
        IsolationLevel.ReadCommitted => 1,
        IsolationLevel.RepeatableRead => 2,
        IsolationLevel.Serializable => 3,
        IsolationLevel.Snapshot => 2,  // Similar to RepeatableRead
        _ => 1
    };
}

public enum OperationType
{
    Read,
    Write,
    SecurityCheck,
    FinancialOperation
}
```

---

### Threat 5: Unencrypted PostgreSQL Connection

**Risk:** Database traffic over unencrypted connections exposes queries, results, and credentials to network eavesdroppers.

**Attack Scenario:** An attacker on the same network segment captures PostgreSQL traffic and extracts sensitive conversation data, user information, and authentication tokens.

**Mitigation Code:**

```csharp
// Infrastructure/Database/SecureSslConnectionFactory.cs
namespace Acode.Infrastructure.Database;

public sealed class SecureSslConnectionFactory : IConnectionFactory
{
    private readonly DatabaseOptions _options;
    private readonly ILogger<SecureSslConnectionFactory> _logger;
    
    public async Task<IDbConnection> CreateAsync(CancellationToken ct)
    {
        var builder = new NpgsqlConnectionStringBuilder(_options.Remote.ConnectionString);
        
        // Enforce SSL in production environments
        var environment = _options.Environment;
        
        if (environment == "Production" || environment == "Staging")
        {
            // Production MUST use verified SSL
            if (builder.SslMode < SslMode.VerifyCA)
            {
                _logger.LogWarning(
                    "Upgrading SSL mode from {Current} to VerifyCA for {Environment}",
                    builder.SslMode, environment);
                
                builder.SslMode = SslMode.VerifyCA;
            }
            
            // Disallow trust server certificate in production
            if (builder.TrustServerCertificate)
            {
                throw new SecurityException(
                    "TrustServerCertificate=true is prohibited in production environments");
            }
        }
        else if (environment == "Development")
        {
            // Development: warn but allow less secure modes
            if (builder.SslMode == SslMode.Disable)
            {
                _logger.LogWarning(
                    "SSL disabled for development PostgreSQL connection. " +
                    "This would be blocked in production.");
            }
        }
        
        var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync(ct);
        
        // Verify SSL is actually in use
        if (environment == "Production" && !connection.IsSecure)
        {
            await connection.CloseAsync();
            throw new SecurityException(
                "Failed to establish secure connection to PostgreSQL. " +
                "SSL negotiation failed.");
        }
        
        return connection;
    }
    
    public DatabaseType DatabaseType => DatabaseType.Postgres;
}
```

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

## Best Practices

### Connection Handling

1. **Always use factories** - Never construct connections directly; use IConnectionFactory
2. **Short-lived connections** - Open, execute, close; don't hold connections across requests
3. **Dispose properly** - Use `using` statements to ensure connections are released
4. **Configure pool size** - Set MaxPoolSize based on expected concurrent operations

### Query Execution

5. **Parameterize all queries** - Never concatenate user input into SQL strings
6. **Use async methods** - All data access should use async/await for scalability
7. **Set command timeouts** - Prevent runaway queries from blocking indefinitely
8. **Log slow queries** - Track queries exceeding threshold for optimization

### Error Handling

9. **Wrap database exceptions** - Convert provider-specific exceptions to domain exceptions
10. **Implement retry logic** - Handle transient failures with exponential backoff
11. **Log connection events** - Record opens, closes, and errors for diagnostics
12. **Fail fast on config errors** - Validate connection string at startup, not first use

---

## Troubleshooting

### Issue: Connection pool exhaustion

**Symptoms:** "Timeout waiting for connection" or connection refused

**Causes:**
- Connections not being disposed properly
- Long-running transactions holding connections
- Pool size too small for workload

**Solutions:**
1. Check for missing `using` statements or `Dispose()` calls
2. Add logging to track connection open/close events
3. Increase MaxPoolSize in connection string

### Issue: SSL connection failures (PostgreSQL)

**Symptoms:** "SSL connection required" or certificate errors

**Causes:**
- SSL mode mismatch between config and server
- Certificate not trusted by client
- Self-signed certificate without bypass

**Solutions:**
1. Check sslmode in connection string: require, verify-ca, verify-full
2. Add server certificate to trusted store
3. For development, use `Trust Server Certificate=true` (not for production)

### Issue: Provider not registered

**Symptoms:** "No provider registered for SQLite" or similar

**Causes:**
- Missing NuGet package (Microsoft.Data.Sqlite, Npgsql)
- DI registration not called
- Wrong provider type requested

**Solutions:**
1. Verify NuGet packages installed in project
2. Check AddDatabaseServices() called in DI setup
3. Confirm agent-config.yml database.provider value

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