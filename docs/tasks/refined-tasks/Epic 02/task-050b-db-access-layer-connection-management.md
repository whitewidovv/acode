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
// Using unit of work pattern for atomic operations
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

### Quick Reference Card

```
┌─────────────────────────────────────────────────────────────────────────┐
│                  CONNECTION MANAGEMENT QUICK REFERENCE                   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  INTERFACES:                         FACTORIES:                          │
│  ───────────                         ──────────                          │
│  IConnectionFactory                  SqliteConnectionFactory             │
│  IUnitOfWork                         PostgresConnectionFactory           │
│  IUnitOfWorkFactory                  UnitOfWorkFactory                   │
│                                                                          │
│  CONNECTION PATTERNS:                                                    │
│  ────────────────────                                                    │
│  await using var conn = await factory.CreateAsync(ct);  // Auto-dispose │
│  await using var uow = await uowFactory.CreateAsync(ct); // Transaction │
│                                                                          │
│  CONFIGURATION KEYS:                 ENVIRONMENT VARS:                   │
│  ───────────────────                 ─────────────────                   │
│  database.local.path                 ACODE_PG_CONNECTION                 │
│  database.local.wal_mode             ACODE_PG_HOST                       │
│  database.local.busy_timeout_ms      ACODE_PG_PORT                       │
│  database.remote.connection_string   ACODE_PG_DATABASE                   │
│  database.remote.pool.max_size       ACODE_PG_USER                       │
│  database.retry.max_attempts         ACODE_PG_PASSWORD                   │
│                                                                          │
│  COMMON COMMANDS:                                                        │
│  ────────────────                                                        │
│  acode db connections               Show connection status               │
│  acode db pool                      Show pool statistics                 │
│  acode db test                      Test connectivity                    │
│  acode db config --validate         Validate configuration               │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Programmatic Connection Access

#### Direct Connection Factory Usage

```csharp
// Inject factory via DI
public class ChatQueryService
{
    private readonly IConnectionFactory _connectionFactory;
    
    public ChatQueryService(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }
    
    public async Task<IReadOnlyList<Chat>> GetRecentChatsAsync(
        int limit, 
        CancellationToken ct)
    {
        // Connection is automatically disposed when exiting using block
        await using var connection = await _connectionFactory.CreateAsync(ct);
        
        return (await connection.QueryAsync<Chat>(
            "SELECT * FROM conv_chats ORDER BY created_at DESC LIMIT @Limit",
            new { Limit = limit })).ToList();
    }
}
```

#### Unit of Work for Transactions

```csharp
// Inject factory via DI
public class ChatService
{
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly IChatRepository _chatRepo;
    private readonly IMessageRepository _messageRepo;
    
    public async Task CreateChatWithInitialMessageAsync(
        Chat chat,
        Message message,
        CancellationToken ct)
    {
        // UnitOfWork manages transaction lifecycle
        await using var uow = await _uowFactory.CreateAsync(ct);
        
        try
        {
            // Both operations share the same transaction
            await _chatRepo.CreateAsync(chat, uow, ct);
            await _messageRepo.CreateAsync(message, uow, ct);
            
            // Commit makes both changes permanent
            await uow.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            // Rollback undoes both changes
            await uow.RollbackAsync(ct);
            throw new ChatCreationException("Failed to create chat", ex);
        }
    }
}
```

### Pool Monitoring

```bash
# View current pool statistics
$ acode db pool

PostgreSQL Connection Pool
────────────────────────────────────────────────────────────
Configuration:
  Min Pool Size: 2
  Max Pool Size: 10
  Connection Lifetime: 300s
  Command Timeout: 30s

Current State:
  Active Connections: 3
  Idle Connections: 2
  Total Connections: 5
  Waiting Requests: 0

Performance (last 5 minutes):
  Acquisitions: 1,247
  Avg Acquire Time: 4.2ms
  Max Acquire Time: 89ms
  Timeouts: 0
  Errors: 0

Connection Ages:
  Oldest: 4m 32s
  Newest: 12s
  Average: 2m 15s
```

### SQLite Configuration Details

```bash
# View SQLite PRAGMA settings
$ acode db config --show-pragmas

SQLite Configuration
────────────────────────────────────────────────────────────
Path: .agent/data/workspace.db
Size: 12.4 MB

PRAGMA Settings:
  journal_mode: wal
  busy_timeout: 5000
  foreign_keys: on
  synchronous: normal
  cache_size: -2000 (2MB)
  temp_store: memory

WAL Status:
  Checkpoint Mode: PASSIVE
  WAL File Size: 1.2 MB
  Frames: 847
  Last Checkpoint: 2m ago
```

### Connection Testing

```bash
# Test all configured connections
$ acode db test

Testing database connections...

SQLite (Local):
  ✓ File exists: .agent/data/workspace.db
  ✓ Can open connection
  ✓ WAL mode enabled
  ✓ Foreign keys enforced
  ✓ Can execute query (3ms)
  Status: HEALTHY

PostgreSQL (Remote):
  ✓ Host reachable: localhost:5432
  ✓ SSL negotiation successful
  ✓ Authentication successful
  ✓ Can execute query (12ms)
  ✓ Pool initialized (2 connections)
  Status: HEALTHY

All connections healthy.
```

---

## Troubleshooting

### Issue 1: Connection Pool Exhausted

**Symptoms:**
- Error: "Timeout expired. The timeout period elapsed prior to obtaining a connection"
- Error: "The connection pool has been exhausted"
- Application hangs waiting for database operations
- Increasing latency over time

**Causes:**
- Connections not being disposed properly (missing `using` or `Dispose()`)
- Long-running transactions holding connections
- Pool size too small for concurrent workload
- Queries timing out but holding connections

**Solutions:**

1. **Check for connection leaks:**
   ```bash
   # Enable connection tracking
   acode db config --set diagnostics.track_connections=true
   
   # View open connections and their origins
   acode db connections --trace
   ```

2. **Increase pool size:**
   ```yaml
   # agent-config.yml
   database:
     remote:
       pool:
         max_size: 20  # Increase from default 10
   ```

3. **Add connection leak detection:**
   ```csharp
   // In development, detect undisposed connections
   builder.ConnectionString += ";Include Error Detail=true";
   ```

4. **Verify using patterns in code:**
   ```csharp
   // WRONG - connection may leak
   var conn = await factory.CreateAsync(ct);
   var result = await conn.QueryAsync(...);
   // Missing dispose!
   
   // RIGHT - connection always disposed
   await using var conn = await factory.CreateAsync(ct);
   var result = await conn.QueryAsync(...);
   ```

---

### Issue 2: SQLite Database Locked

**Symptoms:**
- Error: "database is locked"
- Error: "SQLITE_BUSY"
- Operations fail after waiting
- Concurrent writes fail

**Causes:**
- Another process has write lock
- WAL mode not enabled (rollback journal blocks readers)
- Transaction held too long
- Busy timeout too short
- External tool (DB browser) holding lock

**Solutions:**

1. **Verify WAL mode is enabled:**
   ```bash
   acode db config --show-pragmas | grep journal_mode
   # Should show: journal_mode: wal
   ```

2. **Increase busy timeout:**
   ```yaml
   database:
     local:
       busy_timeout_ms: 10000  # 10 seconds
   ```

3. **Close external tools:**
   - DB Browser for SQLite
   - VS Code SQLite extension
   - Other processes accessing the file

4. **Check for stuck transactions:**
   ```bash
   # List active transactions
   acode db transactions --active
   ```

5. **Force WAL checkpoint:**
   ```bash
   acode db checkpoint --force
   ```

---

### Issue 3: PostgreSQL SSL Connection Failures

**Symptoms:**
- Error: "SSL connection is required"
- Error: "certificate verify failed"
- Error: "The remote certificate was rejected"
- Connection refused in production

**Causes:**
- Server requires SSL but client doesn't enable it
- Self-signed certificate not trusted
- Certificate chain incomplete
- SSL mode mismatch between client and server

**Solutions:**

1. **Set appropriate SSL mode:**
   ```yaml
   database:
     remote:
       ssl_mode: require  # or verify-ca, verify-full
   ```

2. **For development with self-signed certs:**
   ```yaml
   database:
     remote:
       trust_server_certificate: true  # NOT for production!
   ```

3. **Install CA certificate:**
   ```bash
   # Add CA cert to trusted store
   cp /path/to/ca-certificate.crt /usr/local/share/ca-certificates/
   update-ca-certificates
   ```

4. **Verify server certificate:**
   ```bash
   openssl s_client -connect hostname:5432 -starttls postgres
   ```

---

### Issue 4: Connection Timeout on Startup

**Symptoms:**
- Application fails to start
- Error: "A network-related error occurred"
- Error: "Connection refused"
- Long startup delay before failure

**Causes:**
- Database server not running
- Wrong host/port configuration
- Firewall blocking connection
- DNS resolution failure
- Network routing issue

**Solutions:**

1. **Verify database server is running:**
   ```bash
   # PostgreSQL
   pg_isready -h localhost -p 5432
   
   # Check if port is listening
   netstat -an | grep 5432
   ```

2. **Test basic connectivity:**
   ```bash
   # Test TCP connection
   telnet localhost 5432
   
   # Or with nc
   nc -zv localhost 5432
   ```

3. **Check configuration:**
   ```bash
   acode db config --validate
   ```

4. **Verify environment variables:**
   ```bash
   echo $ACODE_PG_HOST
   echo $ACODE_PG_PORT
   ```

5. **Check firewall rules:**
   ```bash
   # Linux
   sudo iptables -L -n | grep 5432
   
   # Windows
   netsh advfirewall firewall show rule name=all | findstr 5432
   ```

---

### Issue 5: Transaction Deadlock

**Symptoms:**
- Error: "deadlock detected"
- Operations hang indefinitely
- Multiple operations fail simultaneously
- PostgreSQL kills one of the transactions

**Causes:**
- Two transactions acquiring locks in opposite order
- Long-held locks on frequently accessed rows
- Complex queries with multiple table locks
- Nested operations without proper lock ordering

**Solutions:**

1. **Identify the deadlock participants:**
   ```sql
   -- PostgreSQL: Check for locks
   SELECT * FROM pg_stat_activity 
   WHERE wait_event_type = 'Lock';
   ```

2. **Implement consistent lock ordering:**
   ```csharp
   // Always acquire locks in consistent order (e.g., by ID)
   var sortedIds = ids.OrderBy(id => id).ToList();
   foreach (var id in sortedIds)
   {
       await LockRowAsync(id, ct);
   }
   ```

3. **Reduce transaction scope:**
   ```csharp
   // Break large transactions into smaller ones
   foreach (var batch in items.Chunk(100))
   {
       await using var uow = await _uowFactory.CreateAsync(ct);
       await ProcessBatchAsync(batch, uow, ct);
       await uow.CommitAsync(ct);
   }
   ```

4. **Add retry for deadlock:**
   ```csharp
   // Retry policy handles deadlock as transient
   await _retryPolicy.ExecuteAsync(async ct =>
   {
       await using var uow = await _uowFactory.CreateAsync(ct);
       await DoWorkAsync(uow, ct);
       await uow.CommitAsync(ct);
   }, cancellationToken);
   ```

---

### Issue 6: Memory Growth from Unclosed Connections

**Symptoms:**
- Memory usage grows over time
- GC pressure increases
- Eventually OutOfMemoryException
- Application needs periodic restarts

**Causes:**
- Connections created without proper disposal
- Exception paths skipping disposal
- Async void methods losing reference to connection
- Fire-and-forget calls with connections

**Solutions:**

1. **Enable connection tracking diagnostics:**
   ```yaml
   database:
     diagnostics:
       track_allocations: true
       allocation_stack_trace: true  # Development only
   ```

2. **Run memory profiler:**
   ```bash
   dotnet-trace collect --process-id <PID> --providers Microsoft.Data.SqlClient
   ```

3. **Review async patterns:**
   ```csharp
   // WRONG - fire and forget loses connection reference
   _ = ProcessAsync(data);  // Connection may leak
   
   // RIGHT - properly await
   await ProcessAsync(data);
   ```

4. **Wrap in try-finally:**
   ```csharp
   IDbConnection? conn = null;
   try
   {
       conn = await factory.CreateAsync(ct);
       await DoWorkAsync(conn, ct);
   }
   finally
   {
       conn?.Dispose();
   }
   ```

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

### IConnectionFactory Interface (AC-001 to AC-008)

- [ ] AC-001: `IConnectionFactory` interface exists in `Acode.Application.Interfaces` namespace with `CreateAsync(CancellationToken)` method returning `Task<IDbConnection>`
- [ ] AC-002: `IConnectionFactory` interface includes `DatabaseType` property returning enum value (SQLite, PostgreSQL)
- [ ] AC-003: `IConnectionFactory.CreateAsync` throws `OperationCanceledException` when cancellation token is cancelled before connection completes
- [ ] AC-004: `IConnectionFactory.CreateAsync` throws `DatabaseException` with error code `ACODE-DB-ACC-001` when connection cannot be established
- [ ] AC-005: Returned `IDbConnection` is in `Open` state when `CreateAsync` completes successfully
- [ ] AC-006: `IConnectionFactory` implementations are registered in DI container as singletons
- [ ] AC-007: `IConnectionFactory` can be resolved from DI using `IConnectionFactory` interface type
- [ ] AC-008: Factory selection is based on `database.provider` configuration value (sqlite or postgresql)

### SQLite Connection Factory (AC-009 to AC-025)

- [ ] AC-009: `SqliteConnectionFactory` implements `IConnectionFactory` interface
- [ ] AC-010: SQLite connection uses path from `database.local.path` configuration
- [ ] AC-011: SQLite connection creates parent directories if they don't exist
- [ ] AC-012: SQLite database file is created on first connection if it doesn't exist
- [ ] AC-013: WAL mode is enabled via `PRAGMA journal_mode=WAL` when `database.local.wal_mode` is true
- [ ] AC-014: WAL mode is disabled (rollback journal) when `database.local.wal_mode` is false
- [ ] AC-015: `PRAGMA busy_timeout` is set to value from `database.local.busy_timeout_ms` (default 5000)
- [ ] AC-016: `PRAGMA foreign_keys=ON` is always executed after connection opens
- [ ] AC-017: `PRAGMA synchronous=NORMAL` is set for performance with WAL mode
- [ ] AC-018: SQLite connection string includes `Mode=ReadWrite` for normal operations
- [ ] AC-019: SQLite throws `DatabaseException` with code `ACODE-DB-ACC-001` if database file path is invalid
- [ ] AC-020: SQLite throws `DatabaseException` with code `ACODE-DB-ACC-001` if database file is corrupted
- [ ] AC-021: SQLite logs connection open events with path, duration, and WAL status at Debug level
- [ ] AC-022: SQLite logs connection close events with total connection time at Debug level
- [ ] AC-023: SQLite connection returns `DatabaseType.Sqlite` from `DatabaseType` property
- [ ] AC-024: SQLite connection supports concurrent readers when WAL mode is enabled
- [ ] AC-025: SQLite connection properly handles `SQLITE_BUSY` by waiting up to busy_timeout before failing

### PostgreSQL Connection Factory (AC-026 to AC-050)

- [ ] AC-026: `PostgresConnectionFactory` implements `IConnectionFactory` interface
- [ ] AC-027: PostgreSQL uses connection string from `database.remote.connection_string` configuration
- [ ] AC-028: PostgreSQL falls back to environment variable `ACODE_PG_CONNECTION` if config not set
- [ ] AC-029: PostgreSQL supports component-based configuration: `host`, `port`, `database`, `username`, `password`
- [ ] AC-030: PostgreSQL component values can come from environment variables: `ACODE_PG_HOST`, `ACODE_PG_PORT`, `ACODE_PG_DATABASE`, `ACODE_PG_USER`, `ACODE_PG_PASSWORD`
- [ ] AC-031: PostgreSQL connection string never logged or exposed in exceptions (password masking)
- [ ] AC-032: PostgreSQL pool minimum size configured via `database.remote.pool.min_size` (default 2)
- [ ] AC-033: PostgreSQL pool maximum size configured via `database.remote.pool.max_size` (default 10)
- [ ] AC-034: PostgreSQL connection lifetime configured via `database.remote.pool.connection_lifetime_seconds` (default 300)
- [ ] AC-035: PostgreSQL command timeout configured via `database.remote.command_timeout_seconds` (default 30)
- [ ] AC-036: PostgreSQL SSL mode configured via `database.remote.ssl_mode` (disable, prefer, require, verify-ca, verify-full)
- [ ] AC-037: PostgreSQL connection uses SSL by default in production environment
- [ ] AC-038: PostgreSQL logs pool acquisition time exceeding 100ms at Warning level
- [ ] AC-039: PostgreSQL logs connection errors with masked connection string at Error level
- [ ] AC-040: PostgreSQL throws `DatabaseException` with code `ACODE-DB-ACC-001` if host unreachable
- [ ] AC-041: PostgreSQL throws `DatabaseException` with code `ACODE-DB-ACC-001` if authentication fails
- [ ] AC-042: PostgreSQL throws `DatabaseException` with code `ACODE-DB-ACC-002` if pool exhausted within timeout
- [ ] AC-043: PostgreSQL connection returns `DatabaseType.Postgres` from `DatabaseType` property
- [ ] AC-044: PostgreSQL pool prewarming creates minimum connections on first factory use
- [ ] AC-045: PostgreSQL connections are validated before returning from pool (connection lifetime check)
- [ ] AC-046: PostgreSQL broken connections are detected and removed from pool automatically
- [ ] AC-047: PostgreSQL connection includes application name for server-side identification
- [ ] AC-048: PostgreSQL supports trust server certificate option for development scenarios
- [ ] AC-049: PostgreSQL connection acquisition completes within 5 seconds or throws timeout
- [ ] AC-050: PostgreSQL pool statistics are available via `IConnectionPoolMetrics` interface

### IUnitOfWork Interface (AC-051 to AC-060)

- [ ] AC-051: `IUnitOfWork` interface exists in `Acode.Application.Interfaces` namespace
- [ ] AC-052: `IUnitOfWork` implements `IAsyncDisposable` for proper async cleanup
- [ ] AC-053: `IUnitOfWork.Connection` property provides access to underlying `IDbConnection`
- [ ] AC-054: `IUnitOfWork.Transaction` property provides access to active `IDbTransaction`
- [ ] AC-055: `IUnitOfWork.CommitAsync(CancellationToken)` commits all pending changes atomically
- [ ] AC-056: `IUnitOfWork.RollbackAsync(CancellationToken)` reverts all pending changes
- [ ] AC-057: `IUnitOfWork` automatically rolls back uncommitted changes on `DisposeAsync`
- [ ] AC-058: `IUnitOfWork.CommitAsync` throws `InvalidOperationException` if already committed or rolled back
- [ ] AC-059: `IUnitOfWork.RollbackAsync` throws `InvalidOperationException` if already committed or rolled back
- [ ] AC-060: `IUnitOfWork` logs transaction duration and outcome (commit/rollback) at Debug level

### IUnitOfWorkFactory Interface (AC-061 to AC-068)

- [ ] AC-061: `IUnitOfWorkFactory` interface exists with `CreateAsync(CancellationToken)` method
- [ ] AC-062: `IUnitOfWorkFactory.CreateAsync` returns configured `IUnitOfWork` with active transaction
- [ ] AC-063: `UnitOfWorkFactory` implementation uses injected `IConnectionFactory` to create connections
- [ ] AC-064: `UnitOfWorkFactory` creates transaction with `ReadCommitted` isolation level by default
- [ ] AC-065: `UnitOfWorkFactory` supports configurable isolation level via method overload
- [ ] AC-066: `UnitOfWorkFactory` logs unit of work creation with correlation ID at Debug level
- [ ] AC-067: `UnitOfWorkFactory` is registered in DI container as scoped lifetime
- [ ] AC-068: Multiple `IUnitOfWork` instances can be created and used independently

### Transaction Management (AC-069 to AC-080)

- [ ] AC-069: Transaction begins automatically when `UnitOfWork` is created
- [ ] AC-070: All database operations within `UnitOfWork` share the same transaction
- [ ] AC-071: Commit persists all changes to database permanently
- [ ] AC-072: Rollback reverts all changes within the transaction
- [ ] AC-073: Nested transactions are not supported; throws `NotSupportedException` if attempted
- [ ] AC-074: Transaction timeout is configured via `database.transaction_timeout_seconds` (default 30)
- [ ] AC-075: Transaction logs start time, operations count, and duration on completion
- [ ] AC-076: Concurrent transactions on different connections do not interfere with each other
- [ ] AC-077: Transaction correctly handles `IsolationLevel.Serializable` for critical operations
- [ ] AC-078: Transaction correctly handles `IsolationLevel.RepeatableRead` for read-modify-write patterns
- [ ] AC-079: Transaction correctly handles `IsolationLevel.ReadCommitted` for standard operations
- [ ] AC-080: Transaction automatically times out if held longer than configured maximum

### Retry Policy (AC-081 to AC-095)

- [ ] AC-081: `IDatabaseRetryPolicy` interface exists with `ExecuteAsync<T>` method
- [ ] AC-082: Retry policy uses exponential backoff with jitter for retries
- [ ] AC-083: Maximum retry attempts configured via `database.retry.max_attempts` (default 3)
- [ ] AC-084: Base delay configured via `database.retry.base_delay_ms` (default 100)
- [ ] AC-085: Maximum delay configured via `database.retry.max_delay_ms` (default 5000)
- [ ] AC-086: Jitter factor is 0.1-0.3 of base delay to prevent thundering herd
- [ ] AC-087: Transient errors are retried: connection timeout, pool exhausted, network error
- [ ] AC-088: SQLite `SQLITE_BUSY` is classified as transient and retried
- [ ] AC-089: PostgreSQL deadlock detected is classified as transient and retried
- [ ] AC-090: Permanent errors fail immediately without retry: constraint violation, syntax error
- [ ] AC-091: Authentication failures fail immediately without retry
- [ ] AC-092: Retry policy logs each retry attempt with attempt number, error, and delay at Warning level
- [ ] AC-093: Retry policy logs final failure with all attempts summarized at Error level
- [ ] AC-094: Retry policy respects cancellation token between retry attempts
- [ ] AC-095: Retry policy can be disabled via `database.retry.enabled=false` configuration

### Error Handling (AC-096 to AC-110)

- [ ] AC-096: All database errors are wrapped in `DatabaseException` with structured error codes
- [ ] AC-097: Error code `ACODE-DB-ACC-001` indicates connection failure
- [ ] AC-098: Error code `ACODE-DB-ACC-002` indicates pool exhausted
- [ ] AC-099: Error code `ACODE-DB-ACC-003` indicates transaction failure
- [ ] AC-100: Error code `ACODE-DB-ACC-004` indicates command timeout
- [ ] AC-101: Error code `ACODE-DB-ACC-005` indicates constraint violation (unique, foreign key)
- [ ] AC-102: Error code `ACODE-DB-ACC-006` indicates SQL syntax error
- [ ] AC-103: Error code `ACODE-DB-ACC-007` indicates permission denied
- [ ] AC-104: Error code `ACODE-DB-ACC-008` indicates database does not exist
- [ ] AC-105: `DatabaseException` includes `IsTransient` property for retry decision
- [ ] AC-106: `DatabaseException` includes `InnerException` with original provider error
- [ ] AC-107: `DatabaseException` includes `CorrelationId` for tracing across logs
- [ ] AC-108: Provider-specific error codes are mapped to generic error codes
- [ ] AC-109: SQLite error codes (SQLITE_BUSY, SQLITE_LOCKED, etc.) are properly categorized
- [ ] AC-110: PostgreSQL error codes (23xxx, 40xxx, 53xxx) are properly categorized

### Configuration Validation (AC-111 to AC-120)

- [ ] AC-111: Configuration is validated at startup before first connection attempt
- [ ] AC-112: Missing `database.provider` throws `ConfigurationException` with clear message
- [ ] AC-113: Invalid `database.provider` value throws `ConfigurationException` listing valid options
- [ ] AC-114: SQLite: Missing `database.local.path` throws `ConfigurationException`
- [ ] AC-115: PostgreSQL: Missing connection string or components throws `ConfigurationException`
- [ ] AC-116: Pool size validation: `max_size >= min_size` or throws `ConfigurationException`
- [ ] AC-117: Timeout validation: all timeout values > 0 or throws `ConfigurationException`
- [ ] AC-118: Configuration validation logs all validated settings at Information level on startup
- [ ] AC-119: Configuration validation masks passwords in log output
- [ ] AC-120: Configuration can be reloaded at runtime via configuration change notification

### Logging and Diagnostics (AC-121 to AC-130)

- [ ] AC-121: All log entries include `database_type`, `operation`, and `duration_ms` fields
- [ ] AC-122: Connection open logged at Debug level with database type and path/host
- [ ] AC-123: Connection close logged at Debug level with total time held
- [ ] AC-124: Transaction start logged at Debug level with isolation level
- [ ] AC-125: Transaction commit logged at Debug level with duration and operations count
- [ ] AC-126: Transaction rollback logged at Information level with reason
- [ ] AC-127: Pool exhaustion logged at Warning level with pool statistics
- [ ] AC-128: Connection errors logged at Error level with masked connection details
- [ ] AC-129: Slow operations (>1s) logged at Warning level with operation details
- [ ] AC-130: Retry attempts logged at Warning level with attempt number and next delay

### Performance Metrics (AC-131 to AC-140)

- [ ] AC-131: `IConnectionPoolMetrics` interface provides pool statistics
- [ ] AC-132: Metric: `db_connection_pool_size` (current pool size)
- [ ] AC-133: Metric: `db_connection_pool_active` (connections in use)
- [ ] AC-134: Metric: `db_connection_pool_idle` (connections available)
- [ ] AC-135: Metric: `db_connection_acquire_duration_ms` (histogram of acquisition times)
- [ ] AC-136: Metric: `db_command_duration_ms` (histogram of command execution times)
- [ ] AC-137: Metric: `db_transaction_duration_ms` (histogram of transaction durations)
- [ ] AC-138: Metric: `db_errors_total` (counter by error code)
- [ ] AC-139: Metric: `db_retries_total` (counter of retry attempts)
- [ ] AC-140: Metrics are exposed via OpenTelemetry if configured

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

#### SqliteConnectionFactoryTests.cs

```csharp
// Tests/Acode.Infrastructure.Tests/Database/SqliteConnectionFactoryTests.cs
namespace Acode.Infrastructure.Tests.Database;

using Acode.Application.Interfaces;
using Acode.Domain.Exceptions;
using Acode.Infrastructure.Persistence.Connections;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

public sealed class SqliteConnectionFactoryTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly SqliteConnectionFactory _sut;
    
    public SqliteConnectionFactoryTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        var options = Options.Create(new DatabaseOptions
        {
            Provider = "sqlite",
            Local = new LocalDatabaseOptions
            {
                Path = _testDbPath,
                WalMode = true,
                BusyTimeoutMs = 5000
            }
        });
        _sut = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);
    }
    
    public void Dispose()
    {
        if (File.Exists(_testDbPath))
            File.Delete(_testDbPath);
    }
    
    [Fact]
    public async Task CreateAsync_ShouldReturnOpenConnection()
    {
        // Act
        await using var connection = await _sut.CreateAsync(CancellationToken.None);
        
        // Assert
        connection.Should().NotBeNull();
        connection.State.Should().Be(ConnectionState.Open);
    }
    
    [Fact]
    public async Task CreateAsync_ShouldCreateDatabaseFileIfNotExists()
    {
        // Arrange
        File.Exists(_testDbPath).Should().BeFalse();
        
        // Act
        await using var connection = await _sut.CreateAsync(CancellationToken.None);
        
        // Assert
        File.Exists(_testDbPath).Should().BeTrue();
    }
    
    [Fact]
    public async Task CreateAsync_ShouldEnableWalMode_WhenConfigured()
    {
        // Act
        await using var connection = await _sut.CreateAsync(CancellationToken.None);
        
        // Assert
        var sqliteConn = connection as SqliteConnection;
        await using var cmd = sqliteConn!.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode;";
        var result = await cmd.ExecuteScalarAsync();
        result.Should().Be("wal");
    }
    
    [Fact]
    public async Task CreateAsync_ShouldSetBusyTimeout()
    {
        // Act
        await using var connection = await _sut.CreateAsync(CancellationToken.None);
        
        // Assert
        var sqliteConn = connection as SqliteConnection;
        await using var cmd = sqliteConn!.CreateCommand();
        cmd.CommandText = "PRAGMA busy_timeout;";
        var result = await cmd.ExecuteScalarAsync();
        Convert.ToInt32(result).Should().Be(5000);
    }
    
    [Fact]
    public async Task CreateAsync_ShouldEnableForeignKeys()
    {
        // Act
        await using var connection = await _sut.CreateAsync(CancellationToken.None);
        
        // Assert
        var sqliteConn = connection as SqliteConnection;
        await using var cmd = sqliteConn!.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys;";
        var result = await cmd.ExecuteScalarAsync();
        Convert.ToInt32(result).Should().Be(1);
    }
    
    [Fact]
    public async Task CreateAsync_ShouldThrowDatabaseException_WhenPathInvalid()
    {
        // Arrange
        var options = Options.Create(new DatabaseOptions
        {
            Provider = "sqlite",
            Local = new LocalDatabaseOptions { Path = "/invalid\0path/db.sqlite" }
        });
        var factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);
        
        // Act
        var act = () => factory.CreateAsync(CancellationToken.None);
        
        // Assert
        await act.Should().ThrowAsync<DatabaseException>()
            .Where(e => e.ErrorCode == "ACODE-DB-ACC-001");
    }
    
    [Fact]
    public async Task CreateAsync_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        // Act
        var act = () => _sut.CreateAsync(cts.Token);
        
        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
    
    [Fact]
    public void DatabaseType_ShouldReturnSqlite()
    {
        // Act & Assert
        _sut.DatabaseType.Should().Be(DatabaseType.Sqlite);
    }
    
    [Fact]
    public async Task CreateAsync_ShouldCreateParentDirectories()
    {
        // Arrange
        var nestedPath = Path.Combine(Path.GetTempPath(), "nested", "dirs", $"test_{Guid.NewGuid()}.db");
        var options = Options.Create(new DatabaseOptions
        {
            Provider = "sqlite",
            Local = new LocalDatabaseOptions { Path = nestedPath, WalMode = true }
        });
        var factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);
        
        try
        {
            // Act
            await using var connection = await factory.CreateAsync(CancellationToken.None);
            
            // Assert
            File.Exists(nestedPath).Should().BeTrue();
        }
        finally
        {
            if (File.Exists(nestedPath))
            {
                File.Delete(nestedPath);
                Directory.Delete(Path.GetDirectoryName(nestedPath)!, true);
            }
        }
    }
}
```

#### PostgresConnectionFactoryTests.cs

```csharp
// Tests/Acode.Infrastructure.Tests/Database/PostgresConnectionFactoryTests.cs
namespace Acode.Infrastructure.Tests.Database;

using Acode.Application.Interfaces;
using Acode.Domain.Exceptions;
using Acode.Infrastructure.Persistence.Connections;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

public sealed class PostgresConnectionFactoryTests
{
    private readonly Mock<IOptions<DatabaseOptions>> _optionsMock;
    private readonly PostgresConnectionFactory _sut;
    
    public PostgresConnectionFactoryTests()
    {
        _optionsMock = new Mock<IOptions<DatabaseOptions>>();
        _optionsMock.Setup(o => o.Value).Returns(new DatabaseOptions
        {
            Provider = "postgresql",
            Remote = new RemoteDatabaseOptions
            {
                ConnectionString = "Host=localhost;Database=test;Username=test;Password=test",
                Pool = new PoolOptions { MinSize = 1, MaxSize = 5 },
                CommandTimeoutSeconds = 30
            }
        });
        _sut = new PostgresConnectionFactory(_optionsMock.Object, NullLogger<PostgresConnectionFactory>.Instance);
    }
    
    [Fact]
    public void DatabaseType_ShouldReturnPostgres()
    {
        // Act & Assert
        _sut.DatabaseType.Should().Be(DatabaseType.Postgres);
    }
    
    [Fact]
    public void Constructor_ShouldApplyPoolSettings()
    {
        // Arrange
        var options = Options.Create(new DatabaseOptions
        {
            Provider = "postgresql",
            Remote = new RemoteDatabaseOptions
            {
                ConnectionString = "Host=localhost;Database=test",
                Pool = new PoolOptions { MinSize = 2, MaxSize = 20 }
            }
        });
        
        // Act
        var factory = new PostgresConnectionFactory(options, NullLogger<PostgresConnectionFactory>.Instance);
        
        // Assert - factory should be created without exception
        factory.Should().NotBeNull();
        factory.DatabaseType.Should().Be(DatabaseType.Postgres);
    }
    
    [Fact]
    public async Task CreateAsync_ShouldThrowDatabaseException_WhenConnectionFails()
    {
        // Arrange - use invalid host that won't resolve
        var options = Options.Create(new DatabaseOptions
        {
            Provider = "postgresql",
            Remote = new RemoteDatabaseOptions
            {
                ConnectionString = "Host=invalid.host.that.does.not.exist.local;Database=test;Timeout=1"
            }
        });
        var factory = new PostgresConnectionFactory(options, NullLogger<PostgresConnectionFactory>.Instance);
        
        // Act
        var act = () => factory.CreateAsync(CancellationToken.None);
        
        // Assert
        await act.Should().ThrowAsync<DatabaseException>()
            .Where(e => e.ErrorCode == "ACODE-DB-ACC-001");
    }
    
    [Fact]
    public async Task CreateAsync_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        // Act
        var act = () => _sut.CreateAsync(cts.Token);
        
        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
    
    [Fact]
    public void Constructor_ShouldThrow_WhenConnectionStringMissing()
    {
        // Arrange
        var options = Options.Create(new DatabaseOptions
        {
            Provider = "postgresql",
            Remote = new RemoteDatabaseOptions { ConnectionString = null }
        });
        
        // Act
        var act = () => new PostgresConnectionFactory(options, NullLogger<PostgresConnectionFactory>.Instance);
        
        // Assert
        act.Should().Throw<ConfigurationException>();
    }
    
    [Fact]
    public void Constructor_ShouldBuildConnectionString_FromComponents()
    {
        // Arrange
        var options = Options.Create(new DatabaseOptions
        {
            Provider = "postgresql",
            Remote = new RemoteDatabaseOptions
            {
                Host = "localhost",
                Port = 5432,
                Database = "testdb",
                Username = "testuser",
                Password = "testpass"
            }
        });
        
        // Act
        var factory = new PostgresConnectionFactory(options, NullLogger<PostgresConnectionFactory>.Instance);
        
        // Assert
        factory.Should().NotBeNull();
    }
}
```

#### UnitOfWorkTests.cs

```csharp
// Tests/Acode.Infrastructure.Tests/Database/UnitOfWorkTests.cs
namespace Acode.Infrastructure.Tests.Database;

using Acode.Application.Interfaces;
using Acode.Infrastructure.Persistence.Transactions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Data;
using Xunit;

public sealed class UnitOfWorkTests
{
    private readonly Mock<IDbConnection> _connectionMock;
    private readonly Mock<IDbTransaction> _transactionMock;
    
    public UnitOfWorkTests()
    {
        _connectionMock = new Mock<IDbConnection>();
        _transactionMock = new Mock<IDbTransaction>();
        _connectionMock.Setup(c => c.BeginTransaction(It.IsAny<IsolationLevel>()))
            .Returns(_transactionMock.Object);
    }
    
    [Fact]
    public void Constructor_ShouldBeginTransaction()
    {
        // Act
        using var uow = new UnitOfWork(_connectionMock.Object, IsolationLevel.ReadCommitted, 
            NullLogger<UnitOfWork>.Instance);
        
        // Assert
        _connectionMock.Verify(c => c.BeginTransaction(IsolationLevel.ReadCommitted), Times.Once);
    }
    
    [Fact]
    public void Connection_ShouldReturnInjectedConnection()
    {
        // Act
        using var uow = new UnitOfWork(_connectionMock.Object, IsolationLevel.ReadCommitted,
            NullLogger<UnitOfWork>.Instance);
        
        // Assert
        uow.Connection.Should().BeSameAs(_connectionMock.Object);
    }
    
    [Fact]
    public void Transaction_ShouldReturnActiveTransaction()
    {
        // Act
        using var uow = new UnitOfWork(_connectionMock.Object, IsolationLevel.ReadCommitted,
            NullLogger<UnitOfWork>.Instance);
        
        // Assert
        uow.Transaction.Should().BeSameAs(_transactionMock.Object);
    }
    
    [Fact]
    public async Task CommitAsync_ShouldCommitTransaction()
    {
        // Arrange
        await using var uow = new UnitOfWork(_connectionMock.Object, IsolationLevel.ReadCommitted,
            NullLogger<UnitOfWork>.Instance);
        
        // Act
        await uow.CommitAsync(CancellationToken.None);
        
        // Assert
        _transactionMock.Verify(t => t.Commit(), Times.Once);
    }
    
    [Fact]
    public async Task RollbackAsync_ShouldRollbackTransaction()
    {
        // Arrange
        await using var uow = new UnitOfWork(_connectionMock.Object, IsolationLevel.ReadCommitted,
            NullLogger<UnitOfWork>.Instance);
        
        // Act
        await uow.RollbackAsync(CancellationToken.None);
        
        // Assert
        _transactionMock.Verify(t => t.Rollback(), Times.Once);
    }
    
    [Fact]
    public async Task DisposeAsync_ShouldRollback_WhenNotCommitted()
    {
        // Arrange
        var uow = new UnitOfWork(_connectionMock.Object, IsolationLevel.ReadCommitted,
            NullLogger<UnitOfWork>.Instance);
        
        // Act
        await uow.DisposeAsync();
        
        // Assert
        _transactionMock.Verify(t => t.Rollback(), Times.Once);
    }
    
    [Fact]
    public async Task DisposeAsync_ShouldNotRollback_WhenAlreadyCommitted()
    {
        // Arrange
        var uow = new UnitOfWork(_connectionMock.Object, IsolationLevel.ReadCommitted,
            NullLogger<UnitOfWork>.Instance);
        await uow.CommitAsync(CancellationToken.None);
        
        // Act
        await uow.DisposeAsync();
        
        // Assert
        _transactionMock.Verify(t => t.Rollback(), Times.Never);
    }
    
    [Fact]
    public async Task CommitAsync_ShouldThrowInvalidOperationException_WhenAlreadyCommitted()
    {
        // Arrange
        await using var uow = new UnitOfWork(_connectionMock.Object, IsolationLevel.ReadCommitted,
            NullLogger<UnitOfWork>.Instance);
        await uow.CommitAsync(CancellationToken.None);
        
        // Act
        var act = () => uow.CommitAsync(CancellationToken.None);
        
        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already committed*");
    }
    
    [Fact]
    public async Task RollbackAsync_ShouldThrowInvalidOperationException_WhenAlreadyRolledBack()
    {
        // Arrange
        await using var uow = new UnitOfWork(_connectionMock.Object, IsolationLevel.ReadCommitted,
            NullLogger<UnitOfWork>.Instance);
        await uow.RollbackAsync(CancellationToken.None);
        
        // Act
        var act = () => uow.RollbackAsync(CancellationToken.None);
        
        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already rolled back*");
    }
    
    [Fact]
    public async Task DisposeAsync_ShouldDisposeConnectionAndTransaction()
    {
        // Arrange
        var uow = new UnitOfWork(_connectionMock.Object, IsolationLevel.ReadCommitted,
            NullLogger<UnitOfWork>.Instance);
        await uow.CommitAsync(CancellationToken.None);
        
        // Act
        await uow.DisposeAsync();
        
        // Assert
        _transactionMock.Verify(t => t.Dispose(), Times.Once);
        _connectionMock.Verify(c => c.Dispose(), Times.Once);
    }
}
```

#### DatabaseRetryPolicyTests.cs

```csharp
// Tests/Acode.Infrastructure.Tests/Database/DatabaseRetryPolicyTests.cs
namespace Acode.Infrastructure.Tests.Database;

using Acode.Domain.Exceptions;
using Acode.Infrastructure.Persistence.Retry;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

public sealed class DatabaseRetryPolicyTests
{
    private readonly DatabaseRetryPolicy _sut;
    
    public DatabaseRetryPolicyTests()
    {
        var options = Options.Create(new RetryOptions
        {
            Enabled = true,
            MaxAttempts = 3,
            BaseDelayMs = 10,  // Short for tests
            MaxDelayMs = 100
        });
        _sut = new DatabaseRetryPolicy(options, NullLogger<DatabaseRetryPolicy>.Instance);
    }
    
    [Fact]
    public async Task ExecuteAsync_ShouldReturnResult_WhenNoException()
    {
        // Act
        var result = await _sut.ExecuteAsync(
            async ct => { await Task.Delay(1, ct); return 42; },
            CancellationToken.None);
        
        // Assert
        result.Should().Be(42);
    }
    
    [Fact]
    public async Task ExecuteAsync_ShouldRetry_OnTransientException()
    {
        // Arrange
        var attempts = 0;
        
        // Act
        var result = await _sut.ExecuteAsync(async ct =>
        {
            attempts++;
            if (attempts < 3)
                throw new DatabaseException("ACODE-DB-ACC-001", "Transient", isTransient: true);
            return 42;
        }, CancellationToken.None);
        
        // Assert
        attempts.Should().Be(3);
        result.Should().Be(42);
    }
    
    [Fact]
    public async Task ExecuteAsync_ShouldNotRetry_OnPermanentException()
    {
        // Arrange
        var attempts = 0;
        
        // Act
        var act = async () => await _sut.ExecuteAsync<int>(async ct =>
        {
            attempts++;
            throw new DatabaseException("ACODE-DB-ACC-005", "Constraint violation", isTransient: false);
        }, CancellationToken.None);
        
        // Assert
        await act.Should().ThrowAsync<DatabaseException>();
        attempts.Should().Be(1);
    }
    
    [Fact]
    public async Task ExecuteAsync_ShouldThrow_AfterMaxRetries()
    {
        // Arrange
        var attempts = 0;
        
        // Act
        var act = async () => await _sut.ExecuteAsync<int>(async ct =>
        {
            attempts++;
            throw new DatabaseException("ACODE-DB-ACC-001", "Always fails", isTransient: true);
        }, CancellationToken.None);
        
        // Assert
        await act.Should().ThrowAsync<DatabaseException>();
        attempts.Should().Be(3); // MaxAttempts = 3
    }
    
    [Fact]
    public async Task ExecuteAsync_ShouldRespectCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var attempts = 0;
        
        // Act
        var act = async () => await _sut.ExecuteAsync<int>(async ct =>
        {
            attempts++;
            if (attempts == 2) cts.Cancel();
            throw new DatabaseException("ACODE-DB-ACC-001", "Transient", isTransient: true);
        }, cts.Token);
        
        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
    
    [Fact]
    public async Task ExecuteAsync_ShouldApplyExponentialBackoff()
    {
        // Arrange
        var timestamps = new List<DateTime>();
        
        // Act
        try
        {
            await _sut.ExecuteAsync<int>(async ct =>
            {
                timestamps.Add(DateTime.UtcNow);
                throw new DatabaseException("ACODE-DB-ACC-001", "Transient", isTransient: true);
            }, CancellationToken.None);
        }
        catch { }
        
        // Assert - each delay should be longer than the previous
        timestamps.Should().HaveCount(3);
        var delay1 = (timestamps[1] - timestamps[0]).TotalMilliseconds;
        var delay2 = (timestamps[2] - timestamps[1]).TotalMilliseconds;
        delay2.Should().BeGreaterThanOrEqualTo(delay1);
    }
    
    [Fact]
    public async Task ExecuteAsync_ShouldDisable_WhenConfiguredOff()
    {
        // Arrange
        var options = Options.Create(new RetryOptions { Enabled = false });
        var policy = new DatabaseRetryPolicy(options, NullLogger<DatabaseRetryPolicy>.Instance);
        var attempts = 0;
        
        // Act
        var act = async () => await policy.ExecuteAsync<int>(async ct =>
        {
            attempts++;
            throw new DatabaseException("ACODE-DB-ACC-001", "Transient", isTransient: true);
        }, CancellationToken.None);
        
        // Assert
        await act.Should().ThrowAsync<DatabaseException>();
        attempts.Should().Be(1);
    }
    
    [Theory]
    [InlineData("ACODE-DB-ACC-001", true)]   // Connection failed
    [InlineData("ACODE-DB-ACC-002", true)]   // Pool exhausted
    [InlineData("ACODE-DB-ACC-004", true)]   // Command timeout
    [InlineData("ACODE-DB-ACC-005", false)]  // Constraint violation
    [InlineData("ACODE-DB-ACC-006", false)]  // Syntax error
    [InlineData("ACODE-DB-ACC-007", false)]  // Permission denied
    public async Task ExecuteAsync_ShouldClassifyErrorsCorrectly(string errorCode, bool shouldRetry)
    {
        // Arrange
        var attempts = 0;
        
        // Act
        try
        {
            await _sut.ExecuteAsync<int>(async ct =>
            {
                attempts++;
                throw new DatabaseException(errorCode, "Test error", isTransient: shouldRetry);
            }, CancellationToken.None);
        }
        catch { }
        
        // Assert
        if (shouldRetry)
            attempts.Should().Be(3); // All retries exhausted
        else
            attempts.Should().Be(1); // No retry for permanent errors
    }
}
```

### Integration Tests

#### SqliteConnectionIntegrationTests.cs

```csharp
// Tests/Acode.Integration.Tests/Database/SqliteConnectionIntegrationTests.cs
namespace Acode.Integration.Tests.Database;

using Acode.Infrastructure.Persistence.Connections;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

[Collection("Database")]
public sealed class SqliteConnectionIntegrationTests : IAsyncLifetime
{
    private readonly string _testDbPath;
    private readonly SqliteConnectionFactory _factory;
    
    public SqliteConnectionIntegrationTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"integration_test_{Guid.NewGuid()}.db");
        var options = Options.Create(new DatabaseOptions
        {
            Provider = "sqlite",
            Local = new LocalDatabaseOptions
            {
                Path = _testDbPath,
                WalMode = true,
                BusyTimeoutMs = 5000
            }
        });
        _factory = new SqliteConnectionFactory(options, NullLogger<SqliteConnectionFactory>.Instance);
    }
    
    public Task InitializeAsync() => Task.CompletedTask;
    
    public Task DisposeAsync()
    {
        if (File.Exists(_testDbPath))
            File.Delete(_testDbPath);
        if (File.Exists(_testDbPath + "-wal"))
            File.Delete(_testDbPath + "-wal");
        if (File.Exists(_testDbPath + "-shm"))
            File.Delete(_testDbPath + "-shm");
        return Task.CompletedTask;
    }
    
    [Fact]
    public async Task Should_ExecuteQuery_Successfully()
    {
        // Arrange
        await using var conn = await _factory.CreateAsync(CancellationToken.None);
        
        // Act
        await using var cmd = ((SqliteConnection)conn).CreateCommand();
        cmd.CommandText = "SELECT 1 + 1 AS result;";
        var result = await cmd.ExecuteScalarAsync();
        
        // Assert
        Convert.ToInt32(result).Should().Be(2);
    }
    
    [Fact]
    public async Task Should_CreateTable_AndInsertData()
    {
        // Arrange
        await using var conn = await _factory.CreateAsync(CancellationToken.None);
        var sqliteConn = (SqliteConnection)conn;
        
        // Act - Create table
        await using var createCmd = sqliteConn.CreateCommand();
        createCmd.CommandText = "CREATE TABLE IF NOT EXISTS test (id INTEGER PRIMARY KEY, value TEXT);";
        await createCmd.ExecuteNonQueryAsync();
        
        // Act - Insert data
        await using var insertCmd = sqliteConn.CreateCommand();
        insertCmd.CommandText = "INSERT INTO test (value) VALUES ('hello');";
        await insertCmd.ExecuteNonQueryAsync();
        
        // Act - Read data
        await using var selectCmd = sqliteConn.CreateCommand();
        selectCmd.CommandText = "SELECT value FROM test WHERE id = 1;";
        var result = await selectCmd.ExecuteScalarAsync();
        
        // Assert
        result.Should().Be("hello");
    }
    
    [Fact]
    public async Task Should_HandleConcurrentReaders_WithWalMode()
    {
        // Arrange - Create table with data
        await using var setupConn = await _factory.CreateAsync(CancellationToken.None);
        await using var setupCmd = ((SqliteConnection)setupConn).CreateCommand();
        setupCmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS concurrent_test (id INTEGER PRIMARY KEY, value TEXT);
            INSERT INTO concurrent_test (value) VALUES ('data');";
        await setupCmd.ExecuteNonQueryAsync();
        
        // Act - Open multiple concurrent readers
        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            await using var conn = await _factory.CreateAsync(CancellationToken.None);
            await using var cmd = ((SqliteConnection)conn).CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM concurrent_test;";
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        });
        
        // Assert
        var results = await Task.WhenAll(tasks);
        results.Should().AllSatisfy(r => r.Should().BeGreaterOrEqualTo(1));
    }
    
    [Fact]
    public async Task Should_EnforceForeignKeys()
    {
        // Arrange
        await using var conn = await _factory.CreateAsync(CancellationToken.None);
        var sqliteConn = (SqliteConnection)conn;
        
        await using var createCmd = sqliteConn.CreateCommand();
        createCmd.CommandText = @"
            CREATE TABLE parent (id INTEGER PRIMARY KEY);
            CREATE TABLE child (id INTEGER PRIMARY KEY, parent_id INTEGER REFERENCES parent(id));";
        await createCmd.ExecuteNonQueryAsync();
        
        // Act - Try to insert child without parent
        await using var insertCmd = sqliteConn.CreateCommand();
        insertCmd.CommandText = "INSERT INTO child (parent_id) VALUES (999);";
        
        var act = () => insertCmd.ExecuteNonQueryAsync();
        
        // Assert - Should throw foreign key constraint error
        await act.Should().ThrowAsync<SqliteException>()
            .Where(e => e.SqliteErrorCode == 19); // SQLITE_CONSTRAINT
    }
}
```

### Performance Benchmarks

| Benchmark | Target | Maximum | Measurement Method |
|-----------|--------|---------|-------------------|
| SQLite connection open | 2ms | 10ms | Stopwatch from CreateAsync start to connection.State == Open |
| PostgreSQL pooled acquire | 5ms | 15ms | Stopwatch from CreateAsync call to return |
| PostgreSQL new connection | 50ms | 150ms | First connection with empty pool |
| Transaction begin | 0.5ms | 2ms | Stopwatch around BeginTransaction call |
| Transaction commit | 1ms | 5ms | Stopwatch around CommitAsync call |
| Unit of Work create | 3ms | 10ms | Stopwatch around factory.CreateAsync |
| Retry policy overhead | <1ms | 2ms | Difference between with/without retry wrapper |
| Connection dispose | <1ms | 2ms | Stopwatch around Dispose/DisposeAsync |

### Test Coverage Requirements

| Component | Minimum Coverage | Target Coverage |
|-----------|-----------------|-----------------|
| IConnectionFactory implementations | 90% | 95% |
| IUnitOfWork implementation | 95% | 100% |
| IUnitOfWorkFactory implementation | 90% | 95% |
| DatabaseRetryPolicy | 95% | 100% |
| Configuration validation | 90% | 95% |
| Error handling/mapping | 90% | 95% |
| Overall module | 85% | 90% |

---

## User Verification Steps

### Scenario 1: Fresh SQLite Database Creation

**Objective:** Verify SQLite database is created automatically on first access

**Prerequisites:**
- Acode CLI installed and built
- No existing workspace database file

**Steps:**

1. Navigate to a new empty directory:
   ```bash
   cd /tmp/acode-test-fresh
   ```

2. Ensure no database exists:
   ```bash
   ls -la .agent/data/
   # Expected: Directory does not exist or workspace.db not present
   ```

3. Initialize workspace (triggers database creation):
   ```bash
   acode init
   ```

4. Verify database was created:
   ```bash
   ls -la .agent/data/workspace.db
   # Expected: File exists with non-zero size
   ```

5. Verify WAL mode is enabled:
   ```bash
   sqlite3 .agent/data/workspace.db "PRAGMA journal_mode;"
   # Expected: wal
   ```

6. Verify foreign keys are enabled:
   ```bash
   sqlite3 .agent/data/workspace.db "PRAGMA foreign_keys;"
   # Expected: 1
   ```

**Expected Outcome:**
- ✅ Database file created at `.agent/data/workspace.db`
- ✅ WAL mode enabled (journal_mode = wal)
- ✅ Foreign keys enforced (foreign_keys = 1)
- ✅ No errors in application logs

---

### Scenario 2: PostgreSQL Connection with Pool Verification

**Objective:** Verify PostgreSQL connection pooling works correctly

**Prerequisites:**
- PostgreSQL server running on localhost:5432
- Test database created with proper credentials
- Acode configured for PostgreSQL

**Steps:**

1. Configure PostgreSQL connection:
   ```yaml
   # agent-config.yml
   database:
     provider: postgresql
     remote:
       host: localhost
       port: 5432
       database: acode_test
       username: acode_user
       password: ${ACODE_PG_PASSWORD}
       pool:
         min_size: 2
         max_size: 10
   ```

2. Set environment variable:
   ```bash
   export ACODE_PG_PASSWORD="your_password"
   ```

3. Test connection:
   ```bash
   acode db test
   # Expected: PostgreSQL (Remote): ✓ Status: HEALTHY
   ```

4. View pool statistics:
   ```bash
   acode db pool
   # Expected: Shows pool with min 2 connections
   ```

5. Perform several operations and check pool:
   ```bash
   acode chat list
   acode db pool
   # Expected: Active connections increased during operation
   ```

**Expected Outcome:**
- ✅ Connection test passes with healthy status
- ✅ Pool shows minimum 2 connections initialized
- ✅ Operations complete without pool exhaustion errors
- ✅ Connections are reused (not recreated for each operation)

---

### Scenario 3: Transaction Commit and Rollback

**Objective:** Verify Unit of Work pattern properly commits and rolls back transactions

**Prerequisites:**
- Working SQLite database
- Chat repository functioning

**Steps:**

1. Check initial chat count:
   ```bash
   acode chat list --count
   # Note the count, e.g., "5 chats"
   ```

2. Create a new chat (successful commit):
   ```bash
   acode chat new --name "Test Transaction"
   # Expected: Chat created successfully
   ```

3. Verify chat was committed:
   ```bash
   acode chat list --count
   # Expected: Count increased by 1
   ```

4. Simulate a failure scenario (if test mode available):
   ```bash
   acode --test-mode chat new --name "Fail After Insert" --fail-on-message
   # Expected: Error during message creation
   ```

5. Verify rollback occurred:
   ```bash
   acode chat list --name "Fail After Insert"
   # Expected: Chat not found (rolled back)
   ```

**Expected Outcome:**
- ✅ Successful operations are committed (chat count increases)
- ✅ Failed operations are rolled back (chat not persisted)
- ✅ Transaction logs show commit/rollback events
- ✅ Database remains consistent after failure

---

### Scenario 4: Pool Exhaustion and Recovery

**Objective:** Verify system handles pool exhaustion gracefully

**Prerequisites:**
- PostgreSQL configured with small pool size
- Ability to simulate concurrent operations

**Steps:**

1. Configure minimal pool:
   ```yaml
   # agent-config.yml
   database:
     remote:
       pool:
         max_size: 2
         acquisition_timeout_seconds: 5
   ```

2. Restart application to apply settings

3. Run concurrent operations that exceed pool:
   ```bash
   # Terminal 1
   acode chat export --format json --slow  # Holds connection
   
   # Terminal 2
   acode chat export --format json --slow  # Holds connection
   
   # Terminal 3 (runs while above are executing)
   acode chat list  # Should wait for pool
   ```

4. Observe the third operation:
   ```
   # Expected: Operation waits, then completes when pool frees up
   # Or: Times out after 5 seconds with clear error message
   ```

5. Check logs for pool exhaustion warning:
   ```bash
   grep "pool" .agent/logs/acode.log
   # Expected: Warning about pool wait or exhaustion
   ```

**Expected Outcome:**
- ✅ Third operation waits for available connection
- ✅ Clear warning logged about pool contention
- ✅ Operation completes once connection available
- ✅ Error message is helpful if timeout occurs

---

### Scenario 5: Retry Policy for Transient Failures

**Objective:** Verify transient database errors are retried automatically

**Prerequisites:**
- Test environment that can simulate transient failures
- Logging enabled at Debug level

**Steps:**

1. Enable debug logging:
   ```yaml
   logging:
     level: Debug
   ```

2. Simulate network instability (if test mode available):
   ```bash
   acode --test-mode db simulate-transient --failures 2
   ```

3. Execute a database operation:
   ```bash
   acode chat list
   ```

4. Check logs for retry attempts:
   ```bash
   grep -E "retry|attempt" .agent/logs/acode.log
   # Expected: Shows retry attempts with delays
   ```

5. Verify operation eventually succeeded:
   ```
   # Expected: Chat list displayed correctly after retries
   ```

**Expected Outcome:**
- ✅ Operation succeeds despite initial transient failures
- ✅ Logs show retry attempts with increasing delays
- ✅ User doesn't see intermediate failures
- ✅ Final success is reported normally

---

### Scenario 6: Configuration Validation at Startup

**Objective:** Verify invalid configurations are caught at startup

**Prerequisites:**
- Ability to modify configuration file

**Steps:**

1. Create invalid configuration (missing provider):
   ```yaml
   # agent-config.yml
   database:
     # provider: missing!
     local:
       path: .agent/data/workspace.db
   ```

2. Attempt to start application:
   ```bash
   acode chat list
   # Expected: Startup fails with clear error
   ```

3. Check error message:
   ```
   Configuration error: Missing required field 'database.provider'
   Valid options: sqlite, postgresql
   ```

4. Create invalid pool configuration:
   ```yaml
   database:
     provider: postgresql
     remote:
       pool:
         min_size: 20
         max_size: 5  # Invalid: min > max
   ```

5. Attempt to start:
   ```bash
   acode chat list
   # Expected: Validation error about pool sizes
   ```

**Expected Outcome:**
- ✅ Missing provider caught with clear message
- ✅ Invalid pool sizes caught with explanation
- ✅ Application fails fast before attempting connection
- ✅ Error messages suggest valid values

---

### Scenario 7: SSL/TLS Connection to PostgreSQL

**Objective:** Verify secure connections work correctly

**Prerequisites:**
- PostgreSQL server with SSL enabled
- SSL certificates configured

**Steps:**

1. Configure SSL connection:
   ```yaml
   database:
     provider: postgresql
     remote:
       host: secure.database.example.com
       port: 5432
       database: acode_prod
       ssl_mode: require
   ```

2. Test connection:
   ```bash
   acode db test
   # Expected: Shows SSL negotiation successful
   ```

3. Verify SSL is in use:
   ```bash
   acode db connections --details
   # Expected: Shows "Encrypted: Yes" or similar
   ```

4. Test with stricter SSL mode:
   ```yaml
   ssl_mode: verify-full
   # Requires valid CA certificate
   ```

**Expected Outcome:**
- ✅ Connection established with SSL encryption
- ✅ Connection details show encryption active
- ✅ verify-full mode validates server certificate
- ✅ Connection fails if SSL requirements not met

---

### Scenario 8: Connection Diagnostics and Health Check

**Objective:** Verify diagnostic commands provide useful information

**Prerequisites:**
- Working database connection (SQLite or PostgreSQL)

**Steps:**

1. Run comprehensive connection test:
   ```bash
   acode db test --verbose
   ```

2. Verify output includes:
   ```
   SQLite (Local):
     ✓ File exists: .agent/data/workspace.db
     ✓ Can open connection
     ✓ WAL mode enabled
     ✓ Foreign keys enforced
     ✓ Can execute query (Xms)
     Status: HEALTHY
   ```

3. View connection status:
   ```bash
   acode db connections
   # Expected: Shows current open connections
   ```

4. View configuration:
   ```bash
   acode db config --show
   # Expected: Shows current configuration (passwords masked)
   ```

5. Validate configuration:
   ```bash
   acode db config --validate
   # Expected: All settings validated successfully
   ```

**Expected Outcome:**
- ✅ Health check provides clear pass/fail for each aspect
- ✅ Passwords are masked in configuration display
- ✅ Connection timing information is accurate
- ✅ Diagnostics help identify issues

---

### Scenario 9: Concurrent Transaction Isolation

**Objective:** Verify transactions are properly isolated from each other

**Prerequisites:**
- Working database with chat data
- Two terminal sessions

**Steps:**

1. Terminal 1 - Start a transaction and pause:
   ```bash
   acode --test-mode transaction begin
   acode chat update --id 1 --name "Modified in T1"
   # Don't commit yet
   ```

2. Terminal 2 - Read the same record:
   ```bash
   acode chat get --id 1
   # Expected: Shows ORIGINAL name, not "Modified in T1"
   ```

3. Terminal 1 - Commit the transaction:
   ```bash
   acode --test-mode transaction commit
   ```

4. Terminal 2 - Read again:
   ```bash
   acode chat get --id 1
   # Expected: Now shows "Modified in T1"
   ```

**Expected Outcome:**
- ✅ Uncommitted changes not visible to other transactions
- ✅ Committed changes become visible
- ✅ No dirty reads occur
- ✅ Transaction isolation level is respected

---

### Scenario 10: Error Code Mapping and Diagnostics

**Objective:** Verify database errors are mapped to helpful error codes

**Prerequisites:**
- Working database connection

**Steps:**

1. Trigger a constraint violation:
   ```bash
   acode chat new --id 1  # Assuming ID 1 exists
   # Expected: ACODE-DB-ACC-005 Constraint violation
   ```

2. Trigger a connection error (disconnect database):
   ```bash
   # Stop PostgreSQL or rename SQLite file
   acode chat list
   # Expected: ACODE-DB-ACC-001 Connection failed
   ```

3. Verify error codes are consistent:
   - Connection failed: ACODE-DB-ACC-001
   - Pool exhausted: ACODE-DB-ACC-002
   - Transaction failed: ACODE-DB-ACC-003
   - Command timeout: ACODE-DB-ACC-004
   - Constraint violation: ACODE-DB-ACC-005

4. Verify error details are logged:
   ```bash
   cat .agent/logs/acode.log | grep ACODE-DB-ACC
   # Expected: Shows error code with context
   ```

**Expected Outcome:**
- ✅ Each error type maps to specific error code
- ✅ Error messages are user-friendly
- ✅ Log entries include correlation ID
- ✅ Original exception preserved for debugging

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