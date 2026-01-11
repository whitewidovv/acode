# Task 049.a: Conversation Data Model + Storage Provider Abstraction

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 2 – CLI + Orchestration Core  
**Dependencies:** Task 049 (Parent), Task 050 (Workspace DB), Task 011 (Session), Task 026 (Storage)  

---

## Description

Task 049.a defines the canonical data model for conversation history and the storage provider abstraction. This is the foundation for all conversation operations—the schemas that structure data and the interfaces that hide storage details.

### Business Value

**Productivity Impact:** Structured conversation storage saves engineers $8,640/year per developer by preventing data loss (4 hours/month @ $180/hour = $720/month), enabling fast retrieval (15 min/day @ $1.50/day = $32/month), and supporting reliable sync (2 hours/month saved @ $360 = $30/month).

**Cost Savings:** Efficient schema design reduces database costs by 40% compared to document-store approaches. SQLite local caching eliminates 95% of network calls. Optimized indexing reduces query times from 500ms to 5ms.

**Risk Mitigation:** Referential integrity prevents orphaned records. ACID transactions ensure consistency. Soft-delete pattern enables recovery. Version tracking detects conflicts before data loss.

### Architecture Overview

The data model implements a three-layer architecture:

**Layer 1: Domain Entities (C# Classes)**
- Chat: Aggregate root for conversation threads
- Run: Child entity representing request/response cycles
- Message: Leaf entity for individual exchanges
- ToolCall: Value object within messages

**Layer 2: Repository Abstraction (Interfaces)**
- IChatRepository: Chat CRUSD operations
- IRunRepository: Run CRUSD operations  
- IMessageRepository: Message CRUSD operations
- IUnitOfWork: Transaction coordination

**Layer 3: Storage Providers (Implementations)**
- SqliteChatRepository: Local storage (offline-first)
- PostgresChatRepository: Remote storage (sync target)
- Dapper-based data access (lightweight, performant)

```
                   ┌─────────────────┐
                   │  Application    │
                   │  Use Cases      │
                   └────────┬────────┘
                            │
                            ▼
              ┌─────────────────────────┐
              │  Repository Interfaces  │
              │  IChatRepository        │
              │  IRunRepository         │
              │  IMessageRepository     │
              └────────┬────────────────┘
                       │
           ┌───────────┴───────────┐
           ▼                       ▼
    ┌─────────────┐         ┌─────────────┐
    │  SQLite     │         │ PostgreSQL  │
    │  Provider   │         │  Provider   │
    │  (Local)    │         │  (Remote)   │
    └─────────────┘         └─────────────┘
```

### Entity Model Design

**Chat Entity:**
- **ChatId:** ULID (26 chars, sortable, collision-resistant)
- **Title:** String (max 500 chars, user-editable)
- **Tags:** String array (stored as JSON, searchable)
- **WorktreeId:** Nullable ULID (binds chat to worktree)
- **IsDeleted:** Boolean (soft-delete flag)
- **DeletedAt:** Nullable timestamp (tombstone for sync)
- **SyncStatus:** Enum (Pending, Synced, Conflict)
- **Version:** Integer (optimistic concurrency)
- **CreatedAt:** UTC timestamp (immutable)
- **UpdatedAt:** UTC timestamp (tracks last modification)

**Run Entity:**
- **RunId:** ULID (unique per run)
- **ChatId:** Foreign key to Chat (1:N relationship)
- **Status:** Enum (Running, Completed, Failed, Cancelled)
- **StartedAt:** UTC timestamp (request initiated)
- **CompletedAt:** Nullable timestamp (response finished)
- **TokensUsed:** Integer (tracks API costs)
- **SequenceNumber:** Integer (ordering within chat)
- **SyncStatus:** Enum (tracks sync state)

**Message Entity:**
- **MessageId:** ULID (unique per message)
- **RunId:** Foreign key to Run (1:N relationship)
- **Role:** Enum (User, Assistant, System, Tool)
- **Content:** String (UTF-8 text, max 100KB)
- **ToolCalls:** JSON array (tool invocations)
- **CreatedAt:** UTC timestamp (immutable)
- **SequenceNumber:** Integer (ordering within run)
- **SyncStatus:** Enum (tracks sync state)

**ToolCall Value Object:**
- **Id:** String (unique per tool call)
- **Name:** String (tool function name)
- **Arguments:** JSON object (tool parameters)
- **Result:** Optional string (tool execution output)

### Offline-First Design

**Dual-ID Pattern:**
Every entity has a local ULID generated immediately on creation. Remote IDs are assigned by PostgreSQL after sync. A mapping table correlates identities:

```sql
CREATE TABLE id_mapping (
    local_id TEXT PRIMARY KEY,
    remote_id TEXT NOT NULL,
    entity_type TEXT NOT NULL,
    synced_at TEXT NOT NULL
);
```

**Sync Metadata:**
Each entity tracks sync state to enable eventual consistency:

- **Pending:** Created locally, not yet synced
- **Synced:** Successfully pushed to remote
- **Conflict:** Remote version diverged, needs resolution

**Tombstone Pattern:**
Soft-delete uses IsDeleted flag and DeletedAt timestamp. Deleted entities sync to remote, then purge after grace period (30 days). This prevents resurrection conflicts.

### Repository Pattern Implementation

**Interface Design:**
Repositories expose domain operations, not SQL:

```csharp
public interface IChatRepository
{
    Task<ChatId> CreateAsync(Chat chat, CancellationToken ct);
    Task<Chat?> GetByIdAsync(ChatId id, CancellationToken ct);
    Task UpdateAsync(Chat chat, CancellationToken ct);
    Task SoftDeleteAsync(ChatId id, CancellationToken ct);
    Task<PagedResult<Chat>> ListAsync(ChatFilter filter, CancellationToken ct);
}
```

**Provider Abstraction:**
Implementations handle storage details:

- **Connection management:** Pooling, timeout, retry
- **Transaction coordination:** Unit of work pattern
- **Error translation:** SQL exceptions → domain errors
- **Type mapping:** C# types ↔ SQL types
- **Performance optimization:** Batching, caching

### Schema Migration Strategy

**Version-Controlled Migrations:**
Each migration is a timestamped file with up/down scripts:

```
Migrations/
├── 001_InitialSchema.sql
├── 002_AddTags.sql
├── 003_AddSyncStatus.sql
└── MigrationRunner.cs
```

**Auto-Apply on Startup:**
Migrations run automatically when application starts. Version tracking prevents duplicate application:

```sql
CREATE TABLE schema_version (
    version INTEGER PRIMARY KEY,
    applied_at TEXT NOT NULL,
    description TEXT NOT NULL
);
```

**Rollback Support:**
Each migration has a down script for emergency rollback:

```bash
$ acode db migrations rollback
Rolling back 003_AddSyncStatus... done.
```

### Type Mapping

| C# Type | SQLite | PostgreSQL |
|---------|--------|------------|
| DateTimeOffset | TEXT (ISO 8601) | TIMESTAMPTZ |
| string | TEXT | VARCHAR |
| int | INTEGER | INT |
| bool | INTEGER (0/1) | BOOLEAN |
| JSON | TEXT | JSONB |
| Enum | TEXT | VARCHAR |
| ULID | TEXT (26 chars) | VARCHAR(26) |

### Performance Characteristics

| Operation | SQLite (Local) | PostgreSQL (Remote) | Target |
|-----------|----------------|---------------------|--------|
| Insert Chat | 2ms | 15ms | < 10ms |
| Get by ID | 1ms | 8ms | < 5ms |
| List 100 | 12ms | 35ms | < 50ms |
| Update | 3ms | 18ms | < 10ms |
| Soft Delete | 4ms | 20ms | < 15ms |

**Optimization Strategies:**
- **SQLite:** WAL mode (concurrent reads), memory-mapped I/O, prepared statements
- **PostgreSQL:** Connection pooling (10 connections), statement caching, batch inserts
- **Indexing:** ChatId (PK), WorktreeId (FK), CreatedAt (range queries)

### Error Handling

**Domain Error Codes:**

| Code | Meaning | Recovery |
|------|---------|----------|
| CONV-DATA-001 | Chat not found | Retry or create |
| CONV-DATA-002 | Run not found | Retry or create |
| CONV-DATA-003 | Message not found | Retry or create |
| CONV-DATA-004 | Foreign key violation | Fix parent entity |
| CONV-DATA-005 | Migration failed | Rollback and debug |
| CONV-DATA-006 | Connection timeout | Exponential backoff |
| CONV-DATA-007 | Constraint violation | Validate input |

**Retry Policy:**
Transient errors (connection, timeout) trigger exponential backoff:
- Attempt 1: Immediate
- Attempt 2: 1 second delay
- Attempt 3: 2 second delay
- Attempt 4: 4 second delay
- Attempt 5: Fail with error

**Circuit Breaker:**
After 5 consecutive failures, open circuit for 60 seconds. Prevents cascade failures.

### Constraints & Trade-offs

**Design Constraints:**
- **Must:** Support offline-first operation (engineers work without connectivity)
- **Must:** Maintain referential integrity (no orphaned messages)
- **Must:** Enable future sync (local → remote replication)
- **Must:** Support concurrent access (multiple CLI commands)

**Trade-offs:**
- **Dual IDs:** Adds complexity but enables offline operation
- **Soft Delete:** Uses more storage but enables recovery
- **Repository Pattern:** More abstraction but enables testability
- **JSON Storage:** Less structured but more flexible

### Dependencies & Integration

**Upstream Dependencies:**
- Task 050: Workspace database infrastructure (connection management)
- Task 011: Session management (current user context)
- Task 026: Storage configuration (connection strings)

**Downstream Consumers:**
- Task 049b: CRUSD CLI commands (uses repositories)
- Task 049c: Multi-chat concurrency (needs isolation)
- Task 049d: Search indexing (reads message content)
- Task 049e: Retention policies (queries by date)
- Task 049f: Sync engine (reads pending entities)

---

## Use Cases

### Use Case 1: Ethan Creates Local Chat History

**Context:** Ethan, a backend engineer, starts a new coding session. Acode needs to store his conversation history locally for offline access and future sync.

**Goal:** Create conversation entities in SQLite without network dependency.

**Workflow:**
1. Ethan runs `acode chat start "Refactor auth service"`
2. CLI calls `CreateAsync(new Chat("Refactor auth service"))`
3. Repository generates ULID: `01J8AYGF7QPQZN3M4R5W6X7Y8Z`
4. Inserts row into SQLite: `chats` table
5. Sets SyncStatus = Pending (will sync later)
6. Returns ChatId to CLI (< 5ms)
7. Ethan continues working offline

**Persona:** Ethan (backend engineer, 3 years experience, works on laptop with intermittent Wi-Fi)

**Metrics:**
- **Before:** No structured storage, lost history on crash (1 hour/week lost = $180/month)
- **After:** Structured local storage, instant recovery (< 5ms retrieval)
- **ROI:** $2,160/year saved per engineer ($180/month × 12)

**Success Criteria:**
- Chat created in < 5ms
- ULID generated correctly
- Foreign keys set up for future Runs
- Offline operation (no network call)

### Use Case 2: Sarah Queries Chat by Worktree

**Context:** Sarah, a full-stack engineer, switches between worktrees frequently. She needs to retrieve conversation history specific to each worktree.

**Goal:** Query chats bound to specific worktree efficiently.

**Workflow:**
1. Sarah runs `acode chat list --worktree current`
2. CLI gets current WorktreeId: `01J8B1XHFN2PQRS3T4U5V6W7X8`
3. Calls `GetByWorktreeAsync(worktreeId)`
4. Repository queries with index: `WHERE worktree_id = ?`
5. Returns Chat with all Runs and Messages (< 20ms)
6. CLI displays formatted output
7. Sarah sees relevant history

**Persona:** Sarah (full-stack engineer, 5 years experience, manages 4 worktrees for microservices)

**Metrics:**
- **Before:** No worktree binding, manual search through all chats (5 min/day = $15/month)
- **After:** Instant worktree-filtered query (< 20ms)
- **ROI:** $180/year saved per engineer ($15/month × 12)

**Success Criteria:**
- Query uses index (< 20ms)
- Returns only chats for specified worktree
- Includes Runs and Messages (eager loading)
- Pagination works for large result sets

### Use Case 3: Jordan Recovers Soft-Deleted Chat

**Context:** Jordan, a DevOps engineer, accidentally deletes a chat containing important debugging history. Soft-delete pattern enables recovery within grace period.

**Goal:** Restore accidentally deleted chat before permanent purge.

**Workflow:**
1. Jordan runs `acode chat delete abc123` (soft delete)
2. Repository sets IsDeleted = true, DeletedAt = now
3. Chat hidden from normal queries
4. Jordan realizes mistake within 30 days
5. Runs `acode chat restore abc123`
6. Repository sets IsDeleted = false, DeletedAt = null
7. Chat reappears in list
8. No data lost

**Persona:** Jordan (DevOps engineer, 7 years experience, manages production incidents with extensive chat history)

**Metrics:**
- **Before:** Hard delete, permanent data loss (2 hours/quarter recreating context = $360/year)
- **After:** Soft delete with 30-day grace period, zero data loss
- **ROI:** $360/year saved per engineer

**Success Criteria:**
- Soft delete in < 10ms
- Deleted chats excluded from normal queries
- Restore works within grace period
- Cascade to Runs and Messages (tombstones)

---

## Security Considerations

### Threat 1: SQL Injection via Repository Methods

**Threat:** Malicious input in Chat title or Message content could inject SQL commands if not parameterized.

**Impact:** HIGH - Could read, modify, or delete arbitrary database records.

**Likelihood:** MEDIUM - Requires user-controlled input reaching repository without sanitization.

**Mitigation:** Use parameterized queries exclusively. Never concatenate user input into SQL strings.

**Implementation:**

```csharp
namespace AgenticCoder.Infrastructure.Persistence.Conversation;

public sealed class SqlInjectionProtectedRepository : IChatRepository
{
    private readonly IDbConnection _db;

    public async Task<ChatId> CreateAsync(Chat chat, CancellationToken ct)
    {
        // ✅ SECURE: Parameterized query
        const string sql = @"
            INSERT INTO chats (id, title, tags, worktree_id, created_at, updated_at)
            VALUES (@Id, @Title, @Tags, @WorktreeId, @CreatedAt, @UpdatedAt)";

        var parameters = new
        {
            Id = chat.Id.Value,
            Title = chat.Title,  // User input safely parameterized
            Tags = JsonSerializer.Serialize(chat.Tags),
            WorktreeId = chat.WorktreeId?.Value,
            CreatedAt = chat.CreatedAt.ToString("O"),
            UpdatedAt = chat.UpdatedAt.ToString("O")
        };

        await _db.ExecuteAsync(sql, parameters);
        return chat.Id;
    }

    public async Task<PagedResult<Chat>> ListAsync(ChatFilter filter, CancellationToken ct)
    {
        // ✅ SECURE: Filter values parameterized
        const string sql = @"
            SELECT * FROM chats 
            WHERE is_deleted = 0
              AND (worktree_id = @WorktreeId OR @WorktreeId IS NULL)
            ORDER BY updated_at DESC
            LIMIT @PageSize OFFSET @Offset";

        var chats = await _db.QueryAsync<ChatRow>(sql, new
        {
            WorktreeId = filter.WorktreeId?.Value,
            PageSize = filter.PageSize,
            Offset = filter.Page * filter.PageSize
        });

        return new PagedResult<Chat>(chats.Select(ToChatEntity).ToList());
    }
}
```

**Verification:**
- Test with malicious input: `'; DROP TABLE chats; --`
- Confirm parameterization prevents execution
- Use static analysis tools (Roslyn analyzers) to detect string concatenation in SQL

**Additional Mitigations:**
- Input validation before reaching repository
- Whitelist allowed characters in titles (alphanumeric, spaces, punctuation)
- Limit title length to 500 characters

---

### Threat 2: Unauthorized Data Access via Repository Bypass

**Threat:** Application code could bypass repository abstraction and access database directly, circumventing security controls.

**Impact:** HIGH - Could access data from other users' chats or modify sync metadata.

**Likelihood:** LOW - Requires developer mistake or malicious insider.

**Mitigation:** Seal repository implementations. Make IDbConnection internal to infrastructure layer. Enforce architecture tests.

**Implementation:**

```csharp
namespace AgenticCoder.Infrastructure.Persistence.Conversation;

// ✅ SECURE: Sealed class prevents inheritance/override
public sealed class SqliteChatRepository : IChatRepository
{
    private readonly IDbConnection _db;  // Internal connection, not exposed

    // Constructor accepts connection from DI container only
    internal SqliteChatRepository(IDbConnection db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    // All methods go through abstraction
    public async Task<Chat?> GetByIdAsync(ChatId id, CancellationToken ct)
    {
        const string sql = "SELECT * FROM chats WHERE id = @Id AND is_deleted = 0";
        var row = await _db.QueryFirstOrDefaultAsync<ChatRow>(sql, new { Id = id.Value });
        return row == null ? null : ToChatEntity(row);
    }
}

// Architecture test to enforce layering
public class ArchitectureTests
{
    [Fact]
    public void Application_Layer_Should_Not_Reference_Database()
    {
        var applicationAssembly = typeof(IChatRepository).Assembly;
        var types = applicationAssembly.GetTypes();

        // Assert no types in Application layer use IDbConnection directly
        foreach (var type in types)
        {
            var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.DoesNotContain(fields, f => f.FieldType == typeof(IDbConnection));
        }
    }
}
```

**Verification:**
- Run architecture tests in CI/CD pipeline
- Code review checks for IDbConnection usage outside Infrastructure layer
- Use ArchUnit or NetArchTest for automated verification

**Additional Mitigations:**
- Make IDbConnection internal to Infrastructure assembly
- Use strong naming to prevent reflection-based access
- Log all database access for audit trail

---

### Threat 3: Race Condition in Optimistic Concurrency

**Threat:** Two processes update the same Chat simultaneously, causing version conflict. Last write wins, silently losing first update.

**Impact:** MEDIUM - Data loss if updates overwrite each other without detection.

**Likelihood:** MEDIUM - Concurrent CLI commands or sync engine vs. user edit.

**Mitigation:** Implement optimistic concurrency check with version field. Fail update if version changed.

**Implementation:**

```csharp
namespace AgenticCoder.Infrastructure.Persistence.Conversation;

public sealed class OptimisticConcurrencyRepository : IChatRepository
{
    private readonly IDbConnection _db;

    public async Task UpdateAsync(Chat chat, CancellationToken ct)
    {
        // ✅ SECURE: Version check prevents lost updates
        const string sql = @"
            UPDATE chats
            SET title = @Title,
                tags = @Tags,
                updated_at = @UpdatedAt,
                version = version + 1
            WHERE id = @Id 
              AND version = @ExpectedVersion";

        var rowsAffected = await _db.ExecuteAsync(sql, new
        {
            Id = chat.Id.Value,
            Title = chat.Title,
            Tags = JsonSerializer.Serialize(chat.Tags),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O"),
            ExpectedVersion = chat.Version
        });

        if (rowsAffected == 0)
        {
            // Version mismatch - concurrent modification detected
            throw new ConcurrencyException(
                $"Chat {chat.Id} was modified by another process. " +
                "Expected version {chat.Version}, but current version is higher.");
        }

        // Increment version in entity to match database
        chat.IncrementVersion();
    }

    public async Task<Chat?> GetByIdAsync(ChatId id, CancellationToken ct)
    {
        const string sql = "SELECT * FROM chats WHERE id = @Id";
        var row = await _db.QueryFirstOrDefaultAsync<ChatRow>(sql, new { Id = id.Value });
        
        if (row == null) return null;

        // Include version in entity
        return new Chat(
            new ChatId(row.Id),
            row.Title,
            row.Version);  // Current version from database
    }
}

// Domain entity tracks version
public sealed class Chat : AggregateRoot<ChatId>
{
    public int Version { get; private set; }

    internal void IncrementVersion()
    {
        Version++;
    }
}
```

**Verification:**
- Concurrent test: Start two updates simultaneously
- Confirm one succeeds, other throws ConcurrencyException
- Retry mechanism reloads entity and applies change

**Additional Mitigations:**
- Expose version to user: "Chat was modified by another process. Reload and retry."
- Implement automatic retry with exponential backoff (max 3 attempts)
- Log concurrency conflicts for monitoring

---

### Threat 4: Database File Corruption

**Threat:** SQLite database file corrupted due to crash, disk failure, or improper shutdown.

**Impact:** CRITICAL - Complete loss of local conversation history.

**Likelihood:** LOW - SQLite is crash-safe with WAL mode, but disk failures happen.

**Mitigation:** Enable Write-Ahead Logging (WAL), implement backup on startup, detect corruption and recover.

**Implementation:**

```csharp
namespace AgenticCoder.Infrastructure.Persistence.Conversation;

public sealed class CorruptionDetectionRepository
{
    private readonly string _dbPath;
    private readonly ILogger _logger;

    public async Task InitializeAsync()
    {
        // ✅ SECURE: Enable WAL mode for crash safety
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync();
        await conn.ExecuteAsync("PRAGMA journal_mode=WAL");
        await conn.ExecuteAsync("PRAGMA synchronous=NORMAL");

        // Check database integrity on startup
        var integrityCheck = await conn.QueryFirstAsync<string>("PRAGMA integrity_check");

        if (integrityCheck != "ok")
        {
            _logger.LogError("Database corruption detected: {Result}", integrityCheck);

            // Attempt recovery from backup
            await RecoverFromBackupAsync();
        }
        else
        {
            // Create backup if none exists or last backup > 24 hours
            await CreateBackupIfNeededAsync();
        }
    }

    private async Task CreateBackupIfNeededAsync()
    {
        var backupPath = _dbPath + ".backup";
        var lastBackup = File.Exists(backupPath) 
            ? File.GetLastWriteTimeUtc(backupPath) 
            : DateTimeOffset.MinValue;

        if (DateTimeOffset.UtcNow - lastBackup > TimeSpan.FromHours(24))
        {
            _logger.LogInformation("Creating database backup...");
            
            // ✅ SECURE: Use SQLite backup API (atomic operation)
            using var sourceConn = new SqliteConnection($"Data Source={_dbPath}");
            using var destConn = new SqliteConnection($"Data Source={backupPath}");
            await sourceConn.OpenAsync();
            await destConn.OpenAsync();

            sourceConn.BackupDatabase(destConn);
            _logger.LogInformation("Backup created successfully.");
        }
    }

    private async Task RecoverFromBackupAsync()
    {
        var backupPath = _dbPath + ".backup";

        if (!File.Exists(backupPath))
        {
            throw new DatabaseCorruptedException(
                "Database is corrupted and no backup exists. Data loss occurred.");
        }

        _logger.LogWarning("Restoring database from backup...");

        // Move corrupted file to .corrupted
        File.Move(_dbPath, _dbPath + ".corrupted");

        // Copy backup to primary location
        File.Copy(backupPath, _dbPath);

        _logger.LogInformation("Database restored from backup.");
    }
}
```

**Verification:**
- Simulate corruption: Truncate .db file while application running
- Confirm application detects corruption on next startup
- Verify backup restoration recovers data

**Additional Mitigations:**
- Daily automated backups to separate disk/cloud storage
- PRAGMA quick_check before each write transaction
- Monitor for SQLite error codes (SQLITE_CORRUPT = 11)

---

### Threat 5: Sensitive Data in Chat History

**Threat:** Chat content contains sensitive data (API keys, passwords) stored in plaintext in database.

**Impact:** HIGH - Credential leak if database file accessed by unauthorized party.

**Likelihood:** HIGH - Engineers frequently paste credentials during debugging.

**Mitigation:** Detect and redact sensitive patterns before persistence. Warn user if sensitive data detected.

**Implementation:**

```csharp
namespace AgenticCoder.Infrastructure.Persistence.Conversation;

public sealed class SensitiveDataRedactionRepository : IMessageRepository
{
    private readonly IDbConnection _db;
    private readonly ILogger _logger;

    private static readonly Regex[] SensitivePatterns = new[]
    {
        new Regex(@"sk-[a-zA-Z0-9]{48}", RegexOptions.Compiled),  // OpenAI keys
        new Regex(@"ghp_[a-zA-Z0-9]{36}", RegexOptions.Compiled),  // GitHub tokens
        new Regex(@"AKIA[0-9A-Z]{16}", RegexOptions.Compiled),  // AWS access keys
        new Regex(@"password\s*[:=]\s*[^\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"Bearer\s+[a-zA-Z0-9\-._~+/]+", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    };

    public async Task<MessageId> CreateAsync(Message message, CancellationToken ct)
    {
        var content = message.Content;
        var redactedContent = content;
        var foundSensitiveData = false;

        // ✅ SECURE: Scan for sensitive patterns
        foreach (var pattern in SensitivePatterns)
        {
            if (pattern.IsMatch(content))
            {
                foundSensitiveData = true;
                redactedContent = pattern.Replace(redactedContent, "[REDACTED]");
            }
        }

        if (foundSensitiveData)
        {
            _logger.LogWarning(
                "Sensitive data detected in message {MessageId}. Data redacted before storage.",
                message.Id);

            // Optionally: Warn user
            Console.WriteLine(
                "⚠️  Sensitive data detected in message and automatically redacted. " +
                "Avoid pasting credentials in chat.");
        }

        // Store redacted content
        const string sql = @"
            INSERT INTO messages (id, run_id, role, content, created_at, sequence_number)
            VALUES (@Id, @RunId, @Role, @Content, @CreatedAt, @SequenceNumber)";

        await _db.ExecuteAsync(sql, new
        {
            Id = message.Id.Value,
            RunId = message.RunId.Value,
            Role = message.Role.ToString(),
            Content = redactedContent,  // Redacted version
            CreatedAt = message.CreatedAt.ToString("O"),
            SequenceNumber = message.SequenceNumber
        });

        return message.Id;
    }
}
```

**Verification:**
- Test with known sensitive patterns (API keys, passwords)
- Confirm redaction applied before database write
- Verify warning logged and displayed to user

**Additional Mitigations:**
- Encrypt database file at rest (SQLCipher extension)
- Implement retention policy to purge old messages containing credentials
- Require encryption for sync to remote PostgreSQL (TLS 1.3)

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Chat | Conversation thread entity |
| Run | Single request/response cycle |
| Message | Individual exchange |
| ToolCall | Tool invocation within message |
| Repository | Data access abstraction |
| Provider | Storage implementation |
| ULID | Universally Unique Lexicographically Sortable ID |
| Tombstone | Soft-delete marker |
| Migration | Schema version change |
| Sync Metadata | Tracking fields for sync |
| Local ID | Client-generated identifier |
| Remote ID | Server-assigned identifier |
| Cascade | Related entity operations |
| Conflict | Concurrent modification |
| Unit of Work | Transaction scope |

---

## Out of Scope

The following items are explicitly excluded from Task 049.a:

- **CLI commands** - Task 049.b
- **Concurrency** - Task 049.c
- **Search indexing** - Task 049.d
- **Retention policies** - Task 049.e
- **Sync engine** - Task 049.f
- **PostgreSQL repository** - Deferred to Task 049.f (Phase 6 implementation moved for cohesion with sync engine)
- **Query optimization** - Performance tuning
- **Sharding** - Single database
- **Replication** - Not in scope
- **Encryption at rest** - Task 021
- **Custom fields** - Fixed schema

**Note (2026-01-10):** AC-077 through AC-082 (PostgreSQL provider) originally listed in this task have been migrated to Task 049.f as AC-133 through AC-146 for implementation cohesion. Task 049.a focuses on SQLite implementation only.

---

## Assumptions

### Technical Assumptions

- ASM-001: Entity Framework Core provides ORM capabilities
- ASM-002: ULID provides time-ordered unique identifiers
- ASM-003: JSON stores extensible metadata and content
- ASM-004: Database supports efficient queries on chat/message tables
- ASM-005: Soft delete pattern enables recovery

### Data Model Assumptions

- ASM-006: Chat is the root aggregate for conversations
- ASM-007: Message belongs to exactly one Chat
- ASM-008: Message content is immutable after creation
- ASM-009: Role enum (User, Assistant, System) is fixed
- ASM-010: Timestamps are stored as UTC DateTimeOffset

### Dependency Assumptions

- ASM-011: Task 050 workspace database provides infrastructure
- ASM-012: Task 049 main defines overall chat management requirements
- ASM-013: Task 011.b persistence patterns are followed

### Storage Assumptions

- ASM-014: Message content size is bounded (< 100KB typical)
- ASM-015: Attachments stored as references, not inline
- ASM-016: Unit of work pattern manages transactions

---

## Functional Requirements

### Chat Entity

- FR-001: Chat MUST have ChatId (ULID)
- FR-002: Chat MUST have Title (string, max 500)
- FR-003: Chat MUST have CreatedAt (DateTimeOffset)
- FR-004: Chat MUST have UpdatedAt (DateTimeOffset)
- FR-005: Chat MAY have Tags (string array)
- FR-006: Chat MAY have WorktreeId (nullable)
- FR-007: Chat MUST have IsDeleted (bool)
- FR-008: Chat MUST have DeletedAt (nullable)
- FR-009: Chat MUST have SyncStatus (enum)
- FR-010: Chat MUST have Version (int)

### Run Entity

- FR-011: Run MUST have RunId (ULID)
- FR-012: Run MUST have ChatId (foreign key)
- FR-013: Run MUST have Status (enum)
- FR-014: Run MUST have StartedAt (DateTimeOffset)
- FR-015: Run MAY have CompletedAt (nullable)
- FR-016: Run MUST have TokensUsed (int)
- FR-017: Run MUST have SequenceNumber (int)
- FR-018: Run MUST have SyncStatus (enum)

### Message Entity

- FR-019: Message MUST have MessageId (ULID)
- FR-020: Message MUST have RunId (foreign key)
- FR-021: Message MUST have Role (enum)
- FR-022: Message MUST have Content (string)
- FR-023: Message MUST have CreatedAt (DateTimeOffset)
- FR-024: Message MUST have SequenceNumber (int)
- FR-025: Message MAY have ToolCalls (JSON)
- FR-026: Message MUST have SyncStatus (enum)

### ToolCall Structure

- FR-027: ToolCall MUST have Id (string)
- FR-028: ToolCall MUST have Name (string)
- FR-029: ToolCall MUST have Arguments (JSON)
- FR-030: ToolCall MAY have Result (string)
- FR-031: ToolCall MUST have Status (enum)

### Enums

- FR-032: SyncStatus: Pending, Synced, Conflict, Failed
- FR-033: RunStatus: Running, Completed, Failed, Cancelled
- FR-034: MessageRole: User, Assistant, System, Tool

### Repository Interfaces

- FR-035: IChatRepository MUST define CRUD
- FR-036: IRunRepository MUST define CRUD
- FR-037: IMessageRepository MUST define CRUD
- FR-038: All repos MUST support async
- FR-039: All repos MUST support cancellation

### Chat Repository

- FR-040: CreateAsync(Chat) MUST return ChatId
- FR-041: GetByIdAsync(ChatId) MUST return Chat?
- FR-042: UpdateAsync(Chat) MUST work
- FR-043: SoftDeleteAsync(ChatId) MUST work
- FR-044: ListAsync(filter) MUST paginate
- FR-045: GetByWorktreeAsync(WorktreeId) MUST work

### Run Repository

- FR-046: CreateAsync(Run) MUST return RunId
- FR-047: GetByIdAsync(RunId) MUST return Run?
- FR-048: UpdateAsync(Run) MUST work
- FR-049: ListByChatAsync(ChatId) MUST paginate
- FR-050: GetLatestByChatAsync(ChatId) MUST work

### Message Repository

- FR-051: CreateAsync(Message) MUST return MessageId
- FR-052: GetByIdAsync(MessageId) MUST return Message?
- FR-053: ListByRunAsync(RunId) MUST paginate
- FR-054: AppendAsync(RunId, Message) MUST work
- FR-055: BulkCreateAsync(messages) MUST work

### Migrations

- FR-056: Migrations MUST be versioned
- FR-057: Migrations MUST be idempotent
- FR-058: Version table MUST track applied
- FR-059: Rollback MUST be supported

### Provider Registration

- FR-060: Providers MUST be DI registered
- FR-061: Configuration MUST select provider
- FR-062: Connection strings MUST be configurable
- FR-063: Pool size MUST be configurable

---

## Non-Functional Requirements

### Performance

- NFR-001: Insert < 10ms
- NFR-002: Get by ID < 5ms
- NFR-003: List 100 < 50ms
- NFR-004: Connection pool reuse

### Reliability

- NFR-005: ACID transactions
- NFR-006: Crash-safe
- NFR-007: No silent data loss

### Security

- NFR-008: Parameterized queries
- NFR-009: No SQL injection
- NFR-010: Connection string secrets

### Compatibility

- NFR-011: SQLite 3.35+
- NFR-012: PostgreSQL 14+
- NFR-013: .NET 8+

### Maintainability

- NFR-014: Clear schema docs
- NFR-015: Migration history
- NFR-016: Test coverage > 90%

---

## User Manual Documentation

### Overview

The conversation data model provides structured storage for all chat history. Understanding the model helps with debugging, data export, and custom tooling.

### Entity Hierarchy

```
Chat
├── ChatId: ULID
├── Title: string
├── Tags: string[]
├── WorktreeId: ULID?
├── IsDeleted: bool
├── CreatedAt: DateTimeOffset
├── UpdatedAt: DateTimeOffset
└── Runs[]
    ├── RunId: ULID
    ├── Status: Running|Completed|Failed|Cancelled
    ├── StartedAt: DateTimeOffset
    ├── CompletedAt: DateTimeOffset?
    ├── TokensUsed: int
    └── Messages[]
        ├── MessageId: ULID
        ├── Role: User|Assistant|System|Tool
        ├── Content: string
        ├── ToolCalls: ToolCall[]?
        └── CreatedAt: DateTimeOffset
```

### Database Location

**Local (SQLite):**
```
.agent/
└── data/
    └── conversations.db
```

**Remote (PostgreSQL):**
Configured via connection string in config.

### Schema (SQLite)

```sql
-- Chats table
CREATE TABLE chats (
    id TEXT PRIMARY KEY,
    title TEXT NOT NULL,
    tags TEXT,  -- JSON array
    worktree_id TEXT,
    is_deleted INTEGER DEFAULT 0,
    deleted_at TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    sync_status TEXT DEFAULT 'pending',
    version INTEGER DEFAULT 1
);

-- Runs table
CREATE TABLE runs (
    id TEXT PRIMARY KEY,
    chat_id TEXT NOT NULL REFERENCES chats(id),
    status TEXT NOT NULL,
    started_at TEXT NOT NULL,
    completed_at TEXT,
    tokens_used INTEGER DEFAULT 0,
    sequence_number INTEGER NOT NULL,
    sync_status TEXT DEFAULT 'pending',
    UNIQUE(chat_id, sequence_number)
);

-- Messages table
CREATE TABLE messages (
    id TEXT PRIMARY KEY,
    run_id TEXT NOT NULL REFERENCES runs(id),
    role TEXT NOT NULL,
    content TEXT NOT NULL,
    tool_calls TEXT,  -- JSON
    created_at TEXT NOT NULL,
    sequence_number INTEGER NOT NULL,
    sync_status TEXT DEFAULT 'pending',
    UNIQUE(run_id, sequence_number)
);

-- Indexes
CREATE INDEX idx_chats_worktree ON chats(worktree_id);
CREATE INDEX idx_runs_chat ON runs(chat_id);
CREATE INDEX idx_messages_run ON messages(run_id);
```

### Configuration

```yaml
# .agent/config.yml
storage:
  conversation:
    # Local SQLite settings
    local:
      path: .agent/data/conversations.db
      wal_mode: true
      busy_timeout_ms: 5000
      
    # Remote PostgreSQL settings
    remote:
      enabled: true
      connection_string: ${ACODE_PG_CONNECTION}
      pool_size: 10
      command_timeout_seconds: 30
```

### Direct Database Access

For debugging or export:

```bash
# Open SQLite database
$ sqlite3 .agent/data/conversations.db

# List recent chats
sqlite> SELECT id, title, created_at FROM chats 
        WHERE is_deleted = 0 
        ORDER BY updated_at DESC 
        LIMIT 10;

# Export to JSON
sqlite> .mode json
sqlite> .output chats.json
sqlite> SELECT * FROM chats WHERE is_deleted = 0;
```

### Migration Management

```bash
# Check migration status
$ acode db migrations status

Applied Migrations:
  ✓ 001_initial_schema (2024-01-01)
  ✓ 002_add_tags (2024-01-15)
  ✓ 003_add_sync_status (2024-02-01)

Pending Migrations:
  ○ 004_add_token_tracking

# Apply pending migrations
$ acode db migrations apply
Applying 004_add_token_tracking... done.

# Rollback last migration
$ acode db migrations rollback
Rolling back 004_add_token_tracking... done.
```

### Troubleshooting

#### Database Locked

**Problem:** SQLite database is locked

**Solution:**
1. Check for other processes: `lsof .agent/data/conversations.db`
2. Increase busy timeout in config
3. Ensure WAL mode is enabled

#### Connection Pool Exhausted

**Problem:** PostgreSQL pool exhausted

**Solution:**
1. Increase pool_size in config
2. Check for connection leaks
3. Add connection timeout

#### Migration Failed

**Problem:** Migration fails midway

**Solution:**
1. Check migration logs
2. Fix schema manually if needed
3. Mark migration as applied: `acode db migrations mark 004`

---

## Acceptance Criteria

### Chat Entity

- [ ] AC-001: ChatId is ULID (26 characters, lexicographically sortable)
- [ ] AC-002: Title stored (max 500 characters, required)
- [ ] AC-003: CreatedAt timestamp is UTC and immutable
- [ ] AC-004: UpdatedAt timestamp updates on any modification
- [ ] AC-005: Tags stored as JSON array
- [ ] AC-006: Tags can be added without duplicates
- [ ] AC-007: Tags can be removed
- [ ] AC-008: WorktreeId is nullable foreign key
- [ ] AC-009: IsDeleted defaults to false
- [ ] AC-010: Soft delete sets IsDeleted=true and DeletedAt
- [ ] AC-011: Restore clears IsDeleted and DeletedAt
- [ ] AC-012: Version starts at 1 and increments on update
- [ ] AC-013: SyncStatus defaults to Pending
- [ ] AC-014: SyncStatus can transition to Synced, Conflict, Failed

### Run Entity

- [ ] AC-015: RunId is ULID (26 characters)
- [ ] AC-016: ChatId is required foreign key
- [ ] AC-017: Status defaults to Running
- [ ] AC-018: Status can transition: Running → Completed/Failed/Cancelled
- [ ] AC-019: StartedAt is set on creation (UTC)
- [ ] AC-020: CompletedAt is nullable, set on completion
- [ ] AC-021: TokensIn tracks input tokens (default 0)
- [ ] AC-022: TokensOut tracks output tokens (default 0)
- [ ] AC-023: SequenceNumber auto-increments per Chat
- [ ] AC-024: ErrorMessage is set on failure
- [ ] AC-025: ModelId stores inference provider name
- [ ] AC-026: SyncStatus tracks sync state

### Message Entity

- [ ] AC-027: MessageId is ULID (26 characters)
- [ ] AC-028: RunId is required foreign key
- [ ] AC-029: Role is enum (User, Assistant, System, Tool)
- [ ] AC-030: Content stored as text (max 100KB)
- [ ] AC-031: Content is immutable after creation
- [ ] AC-032: ToolCalls stored as JSON array
- [ ] AC-033: ToolCalls can be added to assistant messages
- [ ] AC-034: CreatedAt is UTC and immutable
- [ ] AC-035: SequenceNumber auto-increments per Run
- [ ] AC-036: SyncStatus tracks sync state

### ToolCall Structure

- [ ] AC-037: ToolCall.Id is unique string
- [ ] AC-038: ToolCall.Name is function name
- [ ] AC-039: ToolCall.Arguments is JSON object
- [ ] AC-040: ToolCall.Result is optional string
- [ ] AC-041: ToolCall.Status tracks execution state

### Repository Interfaces

- [ ] AC-042: IChatRepository defines all CRUD operations
- [ ] AC-043: IRunRepository defines all CRUD operations
- [ ] AC-044: IMessageRepository defines all CRUD operations
- [ ] AC-045: All methods accept CancellationToken
- [ ] AC-046: All methods are async (return Task)
- [ ] AC-047: Create methods return entity ID
- [ ] AC-048: Get methods return nullable entity
- [ ] AC-049: List methods return PagedResult

### Chat Repository Operations

- [ ] AC-050: CreateAsync creates new Chat
- [ ] AC-051: GetByIdAsync retrieves Chat by ID
- [ ] AC-052: GetByIdAsync returns null for non-existent
- [ ] AC-053: UpdateAsync persists changes
- [ ] AC-054: UpdateAsync throws ConcurrencyException on version conflict
- [ ] AC-055: SoftDeleteAsync sets IsDeleted flag
- [ ] AC-056: ListAsync filters by IsDeleted=false by default
- [ ] AC-057: ListAsync supports pagination (page, pageSize)
- [ ] AC-058: ListAsync supports WorktreeId filter
- [ ] AC-059: ListAsync supports date range filter
- [ ] AC-060: GetByWorktreeAsync returns chats for worktree

### Run Repository Operations

- [ ] AC-061: CreateAsync creates Run with ChatId
- [ ] AC-062: GetByIdAsync retrieves Run
- [ ] AC-063: UpdateAsync persists status changes
- [ ] AC-064: ListByChatAsync returns Runs for Chat
- [ ] AC-065: GetLatestByChatAsync returns most recent Run

### Message Repository Operations

- [ ] AC-066: CreateAsync creates Message with RunId
- [ ] AC-067: GetByIdAsync retrieves Message
- [ ] AC-068: ListByRunAsync returns Messages for Run
- [ ] AC-069: AppendAsync adds Message to Run
- [ ] AC-070: BulkCreateAsync inserts multiple Messages efficiently

### SQLite Provider

- [ ] AC-071: SQLite CRUD operations work correctly
- [ ] AC-072: WAL mode enabled for concurrent reads
- [ ] AC-073: Busy timeout configured (5 seconds default)
- [ ] AC-074: Transactions support commit and rollback
- [ ] AC-075: Connection pooling works
- [ ] AC-076: Prepared statements cached

### PostgreSQL Provider

**DEFERRED TO TASK 049.F** (2026-01-10): PostgreSQL repository implementation has been moved to Task 049.f for cohesion with sync engine.

- [ ] ~~AC-077: PostgreSQL CRUD operations work correctly~~ → Moved to Task 049.f as AC-133
- [ ] ~~AC-078: Connection pooling works (10 connections default)~~ → Moved to Task 049.f as AC-134
- [ ] ~~AC-079: Command timeout configured (30 seconds default)~~ → Moved to Task 049.f as AC-135
- [ ] ~~AC-080: Transactions support commit and rollback~~ → Moved to Task 049.f as AC-136
- [ ] ~~AC-081: Statement caching works~~ → Moved to Task 049.f as AC-137
- [ ] ~~AC-082: TLS encryption required for connections~~ → Moved to Task 049.f as AC-138

### Migrations

- [ ] AC-083: Migrations auto-apply on application start
- [ ] AC-084: Migration version tracked in schema_version table
- [ ] AC-085: Each migration has up and down scripts
- [ ] AC-086: Migrations are idempotent
- [ ] AC-087: Rollback reverts last migration
- [ ] AC-088: Migration status command shows applied/pending

### Error Handling

- [ ] AC-089: EntityNotFoundException thrown for missing entities
- [ ] AC-090: ConcurrencyException thrown on version conflict
- [ ] AC-091: ValidationException thrown for invalid data
- [ ] AC-092: ConnectionException thrown for database errors
- [ ] AC-093: Error codes follow CONV-DATA-xxx pattern

### Performance

- [ ] AC-094: Insert Chat completes in < 10ms
- [ ] AC-095: Get by ID completes in < 5ms
- [ ] AC-096: List 100 items completes in < 50ms
- [ ] AC-097: Update completes in < 10ms
- [ ] AC-098: Connection pool reused between operations

---

## Best Practices

### Entity Design

- **BP-001: Immutable core properties** - ID, creation timestamp, and core content should never change
- **BP-002: Use ULIDs for ordering** - ULIDs provide time-ordering benefits over random UUIDs
- **BP-003: Nullable metadata** - Allow null metadata to avoid empty JSON objects
- **BP-004: Consistent timestamp handling** - Always use UTC DateTimeOffset

### Storage Patterns

- **BP-005: Repository pattern** - Abstract storage behind interfaces for testability
- **BP-006: Unit of work** - Group related operations in transactions
- **BP-007: Soft delete first** - Use soft delete before permanent deletion
- **BP-008: Eager loading awareness** - Be intentional about navigation property loading

### Data Integrity

- **BP-009: Validate on write** - Check data constraints before persisting
- **BP-010: Foreign key integrity** - Maintain referential integrity in database
- **BP-011: Index frequently queried fields** - ChatId, CreatedAt, Role
- **BP-012: Handle orphaned records** - Clean up messages when chats are deleted

### Performance

- **BP-013: Batch inserts** - Use bulk operations for multiple messages instead of individual inserts
- **BP-014: Connection pooling** - Reuse database connections to avoid connection overhead
- **BP-015: Prepared statements** - Use parameterized queries for repeated operations
- **BP-016: Limit query results** - Always paginate large result sets to prevent memory exhaustion

### Error Handling

- **BP-017: Retry transient failures** - Implement exponential backoff for network/timeout errors
- **BP-018: Circuit breaker pattern** - Open circuit after repeated failures to prevent cascade
- **BP-019: Meaningful error codes** - Use domain-specific error codes (CONV-DATA-xxx) for debugging
- **BP-020: Log database operations** - Structured logging for all CRUD operations with entity IDs

---

## Troubleshooting

### Entity Validation Errors

**Symptom:** Save operation fails with validation error.

**Cause:** Entity properties don't meet constraints.

**Solution:**
1. Check required properties are set
2. Verify string lengths are within limits
3. Ensure foreign keys reference valid records

### Navigation Property Null

**Symptom:** Accessing Chat.Messages returns null.

**Cause:** Lazy loading not enabled or collection not included in query.

**Solution:**
1. Use Include() in query
2. Enable lazy loading if appropriate
3. Check if collection was properly initialized

### Concurrency Conflicts

**Symptom:** Save fails with concurrency exception.

**Cause:** Another process modified the same record.

**Solution:**
1. Reload entity from database
2. Merge changes appropriately
3. Retry the operation

### Database File Locked (SQLite)

**Symptom:** "database is locked" error when writing to SQLite.

**Cause:** Another process holds write lock, or WAL checkpoint in progress.

**Solution:**
1. Check for other processes accessing the database: `lsof .agent/data/conversations.db`
2. Increase busy timeout in connection string: `Busy Timeout=30000`
3. Ensure WAL mode is enabled: `PRAGMA journal_mode=WAL`
4. Check for long-running transactions that hold locks
5. Verify only one CLI instance is running write operations

### Connection Pool Exhausted (PostgreSQL)

**Symptom:** "Timeout expired. The timeout period elapsed prior to obtaining a connection from the pool."

**Cause:** All connections in pool are in use and not being released.

**Solution:**
1. Check for connection leaks (missing `using` statements or `Dispose()` calls)
2. Increase pool size in connection string: `Maximum Pool Size=20`
3. Reduce command timeout to release connections faster
4. Profile application to find long-running queries
5. Implement connection monitoring to track active connections

### ULID Generation Collision

**Symptom:** Duplicate key violation when creating new entities rapidly.

**Cause:** ULID generator producing duplicate IDs under high concurrency.

**Solution:**
1. Verify ULID library is thread-safe or use lock
2. Add retry logic for unique constraint violations
3. Ensure monotonic timestamp source for ULID generation
4. Consider using GUID as fallback for high-throughput scenarios

### Migration Version Mismatch

**Symptom:** Application fails to start with "Migration version mismatch" error.

**Cause:** Database schema version doesn't match expected application version.

**Solution:**
1. Check current database version: `acode db migrations status`
2. Apply pending migrations: `acode db migrations apply`
3. If schema was manually modified, repair version table
4. Backup database before applying migrations
5. Rollback to previous version if migration fails: `acode db migrations rollback`

### Foreign Key Constraint Violation

**Symptom:** Insert fails with "FOREIGN KEY constraint failed" error.

**Cause:** Parent entity doesn't exist or was deleted.

**Solution:**
1. Verify parent entity exists before creating child
2. Check cascade delete rules on relationships
3. Ensure operations execute in correct order (parent before child)
4. Use transactions to maintain consistency
5. Handle soft-deleted parents appropriately

---

## Testing Requirements

### Unit Tests - ChatTests.cs (Complete Implementation)

```csharp
// Tests/Unit/Conversation/Model/ChatTests.cs
using Xunit;
using FluentAssertions;
using AgenticCoder.Domain.Conversation;

namespace AgenticCoder.Tests.Unit.Conversation.Model;

public sealed class ChatTests
{
    [Fact]
    public void Should_Create_With_Valid_Id()
    {
        // Arrange
        var title = "Feature: Add Authentication";
        var worktreeId = WorktreeId.From("worktree-01HKABC");

        // Act
        var chat = Chat.Create(title, worktreeId);

        // Assert
        chat.Should().NotBeNull();
        chat.Id.Should().NotBe(ChatId.Empty);
        chat.Title.Should().Be(title);
        chat.WorktreeBinding.Should().Be(worktreeId);
        chat.IsDeleted.Should().BeFalse();
        chat.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        chat.UpdatedAt.Should().Be(chat.CreatedAt);
        chat.Version.Should().Be(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Invalid_Title(string? invalidTitle)
    {
        // Arrange
        var worktreeId = WorktreeId.From("worktree-01HKABC");

        // Act
        var act = () => Chat.Create(invalidTitle!, worktreeId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*title*")
            .And.ParamName.Should().Be("title");
    }

    [Fact]
    public void Should_Validate_Title_Length()
    {
        // Arrange
        var longTitle = new string('a', 501);  // 501 characters exceeds 500 limit
        var worktreeId = WorktreeId.From("worktree-01HKABC");

        // Act
        var act = () => Chat.Create(longTitle, worktreeId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*500 characters*");
    }

    [Fact]
    public void Should_Track_Version_On_Update()
    {
        // Arrange
        var chat = Chat.Create("Original Title", WorktreeId.From("worktree-01HKABC"));
        var originalVersion = chat.Version;

        // Act
        chat.UpdateTitle("Updated Title");

        // Assert
        chat.Version.Should().Be(originalVersion + 1);
        chat.Title.Should().Be("Updated Title");
        chat.UpdatedAt.Should().BeAfter(chat.CreatedAt);
    }

    [Fact]
    public void Should_Soft_Delete_Chat()
    {
        // Arrange
        var chat = Chat.Create("Test Chat", WorktreeId.From("worktree-01HKABC"));

        // Act
        chat.Delete();

        // Assert
        chat.IsDeleted.Should().BeTrue();
        chat.DeletedAt.Should().NotBeNull();
        chat.DeletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Should_Restore_Soft_Deleted_Chat()
    {
        // Arrange
        var chat = Chat.Create("Test Chat", WorktreeId.From("worktree-01HKABC"));
        chat.Delete();

        // Act
        chat.Restore();

        // Assert
        chat.IsDeleted.Should().BeFalse();
        chat.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void Should_Add_Tags()
    {
        // Arrange
        var chat = Chat.Create("Test Chat", WorktreeId.From("worktree-01HKABC"));

        // Act
        chat.AddTag("bug-fix");
        chat.AddTag("priority-high");

        // Assert
        chat.Tags.Should().HaveCount(2);
        chat.Tags.Should().Contain(new[] { "bug-fix", "priority-high" });
    }

    [Fact]
    public void Should_Prevent_Duplicate_Tags()
    {
        // Arrange
        var chat = Chat.Create("Test Chat", WorktreeId.From("worktree-01HKABC"));
        chat.AddTag("bug-fix");

        // Act
        chat.AddTag("bug-fix");

        // Assert
        chat.Tags.Should().HaveCount(1, "duplicate tags should not be added");
    }
}
```

### Unit Tests - RunTests.cs (Complete Implementation)

```csharp
// Tests/Unit/Conversation/Model/RunTests.cs
using Xunit;
using FluentAssertions;
using AgenticCoder.Domain.Conversation;

namespace AgenticCoder.Tests.Unit.Conversation.Model;

public sealed class RunTests
{
    [Fact]
    public void Should_Require_ChatId()
    {
        // Arrange & Act
        var act = () => Run.Create(ChatId.Empty, "azure-gpt4");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*chatId*")
            .And.ParamName.Should().Be("chatId");
    }

    [Fact]
    public void Should_Track_Status()
    {
        // Arrange
        var chatId = ChatId.NewId();

        // Act
        var run = Run.Create(chatId, "azure-gpt4");

        // Assert
        run.Status.Should().Be(RunStatus.InProgress);
        run.StartedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        run.CompletedAt.Should().BeNull();
        run.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Should_Complete_Run_Successfully()
    {
        // Arrange
        var run = Run.Create(ChatId.NewId(), "azure-gpt4");

        // Act
        run.Complete(tokensIn: 150, tokensOut: 230);

        // Assert
        run.Status.Should().Be(RunStatus.Completed);
        run.CompletedAt.Should().NotBeNull();
        run.CompletedAt.Should().BeAfter(run.StartedAt);
        run.TokensIn.Should().Be(150);
        run.TokensOut.Should().Be(230);
    }

    [Fact]
    public void Should_Fail_Run_With_Error()
    {
        // Arrange
        var run = Run.Create(ChatId.NewId(), "azure-gpt4");

        // Act
        run.Fail("API timeout after 30s");

        // Assert
        run.Status.Should().Be(RunStatus.Failed);
        run.ErrorMessage.Should().Be("API timeout after 30s");
        run.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Should_Calculate_Duration()
    {
        // Arrange
        var run = Run.Create(ChatId.NewId(), "azure-gpt4");
        Thread.Sleep(100);  // Simulate processing time

        // Act
        run.Complete(tokensIn: 100, tokensOut: 200);

        // Assert
        var duration = run.CompletedAt!.Value - run.StartedAt;
        duration.Should().BeGreaterThan(TimeSpan.FromMilliseconds(90));
    }
}
```

### Unit Tests - MessageTests.cs (Complete Implementation)

```csharp
// Tests/Unit/Conversation/Model/MessageTests.cs
using Xunit;
using FluentAssertions;
using AgenticCoder.Domain.Conversation;
using System.Text.Json;

namespace AgenticCoder.Tests.Unit.Conversation.Model;

public sealed class MessageTests
{
    [Fact]
    public void Should_Require_RunId()
    {
        // Arrange & Act
        var act = () => Message.Create(
            runId: RunId.Empty,
            role: "user",
            content: "Hello");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*runId*")
            .And.ParamName.Should().Be("runId");
    }

    [Theory]
    [InlineData("user")]
    [InlineData("assistant")]
    [InlineData("system")]
    [InlineData("tool")]
    public void Should_Accept_Valid_Roles(string role)
    {
        // Arrange
        var runId = RunId.NewId();

        // Act
        var message = Message.Create(runId, role, "Content");

        // Assert
        message.Role.Should().Be(role);
    }

    [Fact]
    public void Should_Serialize_ToolCalls()
    {
        // Arrange
        var runId = RunId.NewId();
        var message = Message.Create(runId, "assistant", "I'll search for that.");

        var toolCalls = new[]
        {
            new ToolCall("call_001", "grep_search", """{"query": "user authentication", "isRegexp": false}"""),
            new ToolCall("call_002", "read_file", """{"filePath": "src/auth.ts", "startLine": 1, "endLine": 50}""")
        };

        // Act
        message.AddToolCalls(toolCalls);
        var serialized = JsonSerializer.Serialize(message.ToolCalls);
        var deserialized = JsonSerializer.Deserialize<ToolCall[]>(serialized);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.Should().HaveCount(2);
        deserialized![0].Id.Should().Be("call_001");
        deserialized[0].Function.Should().Be("grep_search");
        deserialized[1].Id.Should().Be("call_002");
        deserialized[1].Function.Should().Be("read_file");
    }

    [Fact]
    public void Should_Store_Large_Content()
    {
        // Arrange
        var runId = RunId.NewId();
        var largeContent = new string('a', 50_000);  // 50KB content

        // Act
        var message = Message.Create(runId, "user", largeContent);

        // Assert
        message.Content.Should().HaveLength(50_000);
        message.Content.Should().Be(largeContent);
    }
}
```

### Integration Tests - SqliteRepositoryTests.cs (Complete Implementation)

```csharp
// Tests/Integration/Conversation/Storage/SqliteRepositoryTests.cs
using Xunit;
using FluentAssertions;
using AgenticCoder.Infrastructure.Conversation;
using AgenticCoder.Domain.Conversation;
using Microsoft.EntityFrameworkCore;

namespace AgenticCoder.Tests.Integration.Conversation.Storage;

public sealed class SqliteRepositoryTests : IAsyncLifetime
{
    private ConversationDbContext _dbContext = null!;
    private ChatRepository _repository = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ConversationDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _dbContext = new ConversationDbContext(options);
        await _dbContext.Database.OpenConnectionAsync();
        await _dbContext.Database.EnsureCreatedAsync();

        _repository = new ChatRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.CloseConnectionAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task Should_Create_Chat()
    {
        // Arrange
        var chat = Chat.Create("Test Chat", WorktreeId.From("worktree-01HKABC"));

        // Act
        var chatId = await _repository.CreateAsync(chat, CancellationToken.None);

        // Assert
        chatId.Should().NotBe(ChatId.Empty);

        var retrieved = await _repository.GetByIdAsync(chatId, CancellationToken.None);
        retrieved.Should().NotBeNull();
        retrieved!.Title.Should().Be("Test Chat");
    }

    [Fact]
    public async Task Should_Query_By_Worktree()
    {
        // Arrange
        var worktreeId = WorktreeId.From("worktree-01HKABC");
        var chat1 = Chat.Create("Chat 1", worktreeId);
        var chat2 = Chat.Create("Chat 2", worktreeId);
        var chat3 = Chat.Create("Chat 3", WorktreeId.From("worktree-other"));

        await _repository.CreateAsync(chat1, CancellationToken.None);
        await _repository.CreateAsync(chat2, CancellationToken.None);
        await _repository.CreateAsync(chat3, CancellationToken.None);

        // Act
        var filter = new ChatFilter { WorktreeId = worktreeId };
        var results = await _repository.ListAsync(filter, CancellationToken.None);

        // Assert
        results.Items.Should().HaveCount(2);
        results.Items.Should().AllSatisfy(c => c.WorktreeBinding.Should().Be(worktreeId));
    }

    [Fact]
    public async Task Should_Handle_Concurrent_Access()
    {
        // Arrange
        var chat = Chat.Create("Concurrent Test", WorktreeId.From("worktree-01HKABC"));
        var chatId = await _repository.CreateAsync(chat, CancellationToken.None);

        // Act - Simulate two concurrent updates
        var task1 = Task.Run(async () =>
        {
            var c = await _repository.GetByIdAsync(chatId, CancellationToken.None);
            c!.UpdateTitle("Update 1");
            await _repository.UpdateAsync(c, CancellationToken.None);
        });

        var task2 = Task.Run(async () =>
        {
            var c = await _repository.GetByIdAsync(chatId, CancellationToken.None);
            c!.UpdateTitle("Update 2");
            await _repository.UpdateAsync(c, CancellationToken.None);
        });

        // Assert
        var act = async () => await Task.WhenAll(task1, task2);
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>(
            "concurrent updates should trigger optimistic concurrency check");
    }
}
```

### E2E Tests - DataModelE2ETests.cs (Complete Implementation)

```csharp
// Tests/E2E/Conversation/DataModelE2ETests.cs
using Xunit;
using FluentAssertions;
using AgenticCoder.Infrastructure.Conversation;
using AgenticCoder.Domain.Conversation;

namespace AgenticCoder.Tests.E2E.Conversation;

public sealed class DataModelE2ETests
{
    [Fact]
    public async Task Should_Create_Full_Hierarchy()
    {
        // Arrange
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        var repository = new ChatRepository(dbPath);

        try
        {
            // Act - Create full hierarchy
            var chat = Chat.Create("Feature: User Management", WorktreeId.From("worktree-01HKABC"));
            var chatId = await repository.CreateAsync(chat, CancellationToken.None);

            var run = Run.Create(chatId, "azure-gpt4");
            var runId = await repository.CreateRunAsync(run, CancellationToken.None);

            var message1 = Message.Create(runId, "user", "How do I implement user authentication?");
            await repository.CreateMessageAsync(message1, CancellationToken.None);

            var message2 = Message.Create(runId, "assistant", "I'll help with that.");
            message2.AddToolCalls(new[]
            {
                new ToolCall("call_001", "grep_search", """{"query": "authentication"}""")
            });
            await repository.CreateMessageAsync(message2, CancellationToken.None);

            // Assert
            var retrievedChat = await repository.GetByIdAsync(chatId, includeRuns: true, CancellationToken.None);
            retrievedChat.Should().NotBeNull();
            retrievedChat!.Runs.Should().HaveCount(1);
            retrievedChat.Runs[0].Messages.Should().HaveCount(2);
            retrievedChat.Runs[0].Messages[1].ToolCalls.Should().HaveCount(1);
        }
        finally
        {
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }

    [Fact]
    public async Task Should_Migrate_Schema()
    {
        // Arrange
        var dbPath = Path.Combine(Path.GetTempPath(), $"migration_test_{Guid.NewGuid()}.db");

        try
        {
            // Act - Create with initial schema
            var migrator = new SchemaMigrator(dbPath);
            await migrator.MigrateAsync(CancellationToken.None);

            // Verify schema version
            var version = await migrator.GetCurrentVersionAsync(CancellationToken.None);

            // Assert
            version.Should().BeGreaterThan(0, "schema should be migrated to latest version");
        }
        finally
        {
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }
}
```

### Performance Benchmarks

```csharp
// Tests/Performance/ConversationBenchmarks.cs
using BenchmarkDotNet.Attributes;
using AgenticCoder.Infrastructure.Conversation;
using AgenticCoder.Domain.Conversation;

[MemoryDiagnoser]
public class ConversationBenchmarks
{
    private ChatRepository _repository = null!;
    private ChatId _chatId;

    [GlobalSetup]
    public async Task Setup()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), "benchmark.db");
        _repository = new ChatRepository(dbPath);

        var chat = Chat.Create("Benchmark Chat", WorktreeId.From("worktree-01HKABC"));
        _chatId = await _repository.CreateAsync(chat, CancellationToken.None);
    }

    [Benchmark]
    public async Task Insert_Chat()
    {
        var chat = Chat.Create("New Chat", WorktreeId.From("worktree-01HKABC"));
        await _repository.CreateAsync(chat, CancellationToken.None);
    }

    [Benchmark]
    public async Task Insert_Message()
    {
        var run = Run.Create(_chatId, "azure-gpt4");
        var runId = await _repository.CreateRunAsync(run, CancellationToken.None);

        var message = Message.Create(runId, "user", "Test message");
        await _repository.CreateMessageAsync(message, CancellationToken.None);
    }

    [Benchmark]
    public async Task Get_By_Id()
    {
        await _repository.GetByIdAsync(_chatId, includeRuns: false, CancellationToken.None);
    }

    [Benchmark]
    public async Task List_100_Chats()
    {
        var filter = new ChatFilter { PageSize = 100 };
        await _repository.ListAsync(filter, CancellationToken.None);
    }
}
```

**Performance Targets:**
- Insert Chat: 5ms target, 10ms maximum
- Insert Message: 3ms target, 10ms maximum
- Get by ID: 2ms target, 5ms maximum
- List 100: 25ms target, 50ms maximum

---

## User Verification Steps

### Scenario 1: Create Chat with Repository

**Objective:** Verify Chat entity creation works correctly through repository abstraction.

**Prerequisites:**
- Application running with SQLite database configured
- Access to SQLite CLI or database viewer

**Steps:**

1. **Create a new chat using the repository:**
   ```csharp
   var chat = Chat.Create("Feature: User Authentication", WorktreeId.From("worktree-01HKABC"));
   var chatId = await chatRepository.CreateAsync(chat, CancellationToken.None);
   Console.WriteLine($"Created chat: {chatId}");
   ```

2. **Query the database directly to verify:**
   ```bash
   sqlite3 .agent/data/conversations.db "SELECT id, title, worktree_id, created_at, is_deleted, version FROM chats WHERE id = '<chatId>'"
   ```

3. **Expected results:**
   - Row exists in chats table
   - ID is 26-character ULID
   - Title matches "Feature: User Authentication"
   - WorktreeId matches "worktree-01HKABC"
   - CreatedAt is recent UTC timestamp
   - IsDeleted is 0 (false)
   - Version is 1

**Verification checklist:**
- [ ] ChatId is valid ULID format
- [ ] Title stored correctly
- [ ] Timestamps are UTC
- [ ] Default values set correctly

---

### Scenario 2: Create Full Conversation Hierarchy

**Objective:** Verify Chat → Run → Message hierarchy with foreign keys.

**Prerequisites:**
- Chat created from Scenario 1

**Steps:**

1. **Create a Run for the chat:**
   ```csharp
   var run = Run.Create(chatId, "azure-gpt4");
   var runId = await runRepository.CreateAsync(run, CancellationToken.None);
   Console.WriteLine($"Created run: {runId}");
   ```

2. **Create Messages for the run:**
   ```csharp
   var userMessage = Message.Create(runId, "user", "How do I implement JWT authentication?");
   await messageRepository.CreateAsync(userMessage, CancellationToken.None);

   var assistantMessage = Message.Create(runId, "assistant", "I'll help you implement JWT auth...");
   assistantMessage.AddToolCalls(new[] {
       new ToolCall("call_001", "read_file", """{"filePath": "src/auth.cs"}""")
   });
   await messageRepository.CreateAsync(assistantMessage, CancellationToken.None);
   ```

3. **Query database to verify hierarchy:**
   ```bash
   sqlite3 .agent/data/conversations.db "
     SELECT c.title, r.status, m.role, m.content
     FROM chats c
     JOIN runs r ON r.chat_id = c.id
     JOIN messages m ON m.run_id = r.id
     WHERE c.id = '<chatId>'
     ORDER BY m.sequence_number"
   ```

4. **Expected results:**
   - Run has correct ChatId foreign key
   - Messages have correct RunId foreign key
   - SequenceNumbers are ordered (1, 2)
   - ToolCalls JSON stored correctly

**Verification checklist:**
- [ ] Foreign keys resolve correctly
- [ ] Sequence numbers auto-increment
- [ ] ToolCalls serialized as JSON
- [ ] All roles (user, assistant) work

---

### Scenario 3: Query Chats by Worktree

**Objective:** Verify worktree filtering returns only relevant chats.

**Prerequisites:**
- Multiple chats exist with different WorktreeIds

**Steps:**

1. **Create chats for different worktrees:**
   ```csharp
   var chat1 = Chat.Create("Backend Auth", WorktreeId.From("worktree-backend"));
   var chat2 = Chat.Create("Frontend Auth", WorktreeId.From("worktree-frontend"));
   var chat3 = Chat.Create("Backend API", WorktreeId.From("worktree-backend"));
   await chatRepository.CreateAsync(chat1, CancellationToken.None);
   await chatRepository.CreateAsync(chat2, CancellationToken.None);
   await chatRepository.CreateAsync(chat3, CancellationToken.None);
   ```

2. **Query by worktree:**
   ```csharp
   var filter = new ChatFilter { WorktreeId = WorktreeId.From("worktree-backend") };
   var results = await chatRepository.ListAsync(filter, CancellationToken.None);
   ```

3. **Expected results:**
   - Returns 2 chats (chat1 and chat3)
   - Does NOT include chat2 (different worktree)
   - Query executes in < 20ms

**Verification checklist:**
- [ ] Filter returns correct subset
- [ ] Index used (check EXPLAIN QUERY PLAN)
- [ ] Pagination works with filter
- [ ] Performance target met

---

### Scenario 4: Soft Delete and Restore

**Objective:** Verify soft delete pattern preserves data for recovery.

**Prerequisites:**
- Chat with runs and messages exists

**Steps:**

1. **Soft delete the chat:**
   ```csharp
   await chatRepository.SoftDeleteAsync(chatId, CancellationToken.None);
   ```

2. **Verify chat is hidden from normal queries:**
   ```csharp
   var normalList = await chatRepository.ListAsync(new ChatFilter(), CancellationToken.None);
   // Should NOT include deleted chat
   
   var deletedChat = await chatRepository.GetByIdAsync(chatId, CancellationToken.None);
   // Should return chat with IsDeleted = true
   ```

3. **Query database to verify tombstone:**
   ```bash
   sqlite3 .agent/data/conversations.db "SELECT is_deleted, deleted_at FROM chats WHERE id = '<chatId>'"
   ```

4. **Restore the chat:**
   ```csharp
   var chat = await chatRepository.GetByIdAsync(chatId, CancellationToken.None);
   chat!.Restore();
   await chatRepository.UpdateAsync(chat, CancellationToken.None);
   ```

5. **Verify restoration:**
   - Chat appears in normal queries
   - IsDeleted is false
   - DeletedAt is null
   - Runs and Messages still accessible

**Verification checklist:**
- [ ] Soft delete sets IsDeleted and DeletedAt
- [ ] Deleted chats excluded from list queries
- [ ] Restore clears deletion fields
- [ ] Child entities remain intact

---

### Scenario 5: Optimistic Concurrency

**Objective:** Verify concurrent updates are detected and handled.

**Prerequisites:**
- Chat exists in database

**Steps:**

1. **Load chat in two "processes":**
   ```csharp
   var chat1 = await chatRepository.GetByIdAsync(chatId, CancellationToken.None);
   var chat2 = await chatRepository.GetByIdAsync(chatId, CancellationToken.None);
   // Both have Version = 1
   ```

2. **Update first instance:**
   ```csharp
   chat1!.UpdateTitle("Updated by Process 1");
   await chatRepository.UpdateAsync(chat1, CancellationToken.None);
   // Version now 2 in database
   ```

3. **Attempt to update second instance:**
   ```csharp
   chat2!.UpdateTitle("Updated by Process 2");
   try {
       await chatRepository.UpdateAsync(chat2, CancellationToken.None);
   } catch (ConcurrencyException ex) {
       Console.WriteLine($"Conflict detected: {ex.Message}");
   }
   ```

4. **Expected results:**
   - First update succeeds, version increments to 2
   - Second update throws ConcurrencyException
   - Database contains "Updated by Process 1"

**Verification checklist:**
- [ ] Version increments on successful update
- [ ] Stale version update throws exception
- [ ] Exception message is helpful
- [ ] Original data preserved on conflict

---

### Scenario 6: Migration Execution

**Objective:** Verify schema migrations apply correctly.

**Prerequisites:**
- Fresh database or known schema version

**Steps:**

1. **Check current migration status:**
   ```bash
   acode db migrations status
   ```

2. **Create new database and apply migrations:**
   ```csharp
   var migrator = new SchemaMigrator(dbPath);
   await migrator.MigrateAsync(CancellationToken.None);
   var version = await migrator.GetCurrentVersionAsync(CancellationToken.None);
   ```

3. **Verify schema:**
   ```bash
   sqlite3 .agent/data/conversations.db ".schema chats"
   sqlite3 .agent/data/conversations.db ".schema runs"
   sqlite3 .agent/data/conversations.db ".schema messages"
   ```

4. **Verify version tracking:**
   ```bash
   sqlite3 .agent/data/conversations.db "SELECT * FROM schema_version ORDER BY version"
   ```

5. **Expected results:**
   - All tables created with correct columns
   - Indexes created for foreign keys and common queries
   - Version table tracks applied migrations

**Verification checklist:**
- [ ] All tables exist
- [ ] Column types correct
- [ ] Indexes created
- [ ] Version tracking works

---

### Scenario 7: Performance Benchmarks

**Objective:** Verify performance targets are met.

**Prerequisites:**
- Database with sample data

**Steps:**

1. **Measure insert time:**
   ```csharp
   var sw = Stopwatch.StartNew();
   var chat = Chat.Create("Perf Test", WorktreeId.From("worktree-perf"));
   await chatRepository.CreateAsync(chat, CancellationToken.None);
   sw.Stop();
   Console.WriteLine($"Insert: {sw.ElapsedMilliseconds}ms");
   ```

2. **Measure get by ID time:**
   ```csharp
   sw.Restart();
   await chatRepository.GetByIdAsync(chatId, CancellationToken.None);
   sw.Stop();
   Console.WriteLine($"Get by ID: {sw.ElapsedMilliseconds}ms");
   ```

3. **Measure list time:**
   ```csharp
   sw.Restart();
   await chatRepository.ListAsync(new ChatFilter { PageSize = 100 }, CancellationToken.None);
   sw.Stop();
   Console.WriteLine($"List 100: {sw.ElapsedMilliseconds}ms");
   ```

4. **Expected results:**
   - Insert: < 10ms
   - Get by ID: < 5ms
   - List 100: < 50ms

**Verification checklist:**
- [ ] Insert meets target
- [ ] Get meets target
- [ ] List meets target
- [ ] WAL mode enabled

---

### Scenario 8: Large Content Handling

**Objective:** Verify system handles large message content correctly.

**Prerequisites:**
- Run exists

**Steps:**

1. **Create message with large content (50KB):**
   ```csharp
   var largeContent = new string('a', 50_000);  // 50KB
   var message = Message.Create(runId, "user", largeContent);
   await messageRepository.CreateAsync(message, CancellationToken.None);
   ```

2. **Retrieve and verify:**
   ```csharp
   var retrieved = await messageRepository.GetByIdAsync(message.Id, CancellationToken.None);
   Console.WriteLine($"Content length: {retrieved!.Content.Length}");
   ```

3. **Expected results:**
   - Full content stored without truncation
   - Retrieval returns complete content
   - No performance degradation

**Verification checklist:**
- [ ] 50KB content stored correctly
- [ ] Content retrieved completely
- [ ] UTF-8 encoding preserved
- [ ] Performance acceptable

---

### Scenario 9: Sensitive Data Redaction

**Objective:** Verify sensitive patterns are detected and redacted.

**Prerequisites:**
- Message repository with redaction enabled

**Steps:**

1. **Create message with sensitive content:**
   ```csharp
   var content = "Use this API key: sk-abc123xyz789 and password: secret123";
   var message = Message.Create(runId, "user", content);
   await messageRepository.CreateAsync(message, CancellationToken.None);
   ```

2. **Retrieve and verify redaction:**
   ```csharp
   var retrieved = await messageRepository.GetByIdAsync(message.Id, CancellationToken.None);
   Console.WriteLine(retrieved!.Content);
   // Should show: "Use this API key: [REDACTED] and password: [REDACTED]"
   ```

3. **Check logs for warning:**
   - Log should contain "Sensitive data detected"

**Verification checklist:**
- [ ] API keys redacted
- [ ] Passwords redacted
- [ ] Warning logged
- [ ] User notified

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Conversation/
│   ├── Chat.cs
│   ├── ChatId.cs
│   ├── Run.cs
│   ├── RunId.cs
│   ├── Message.cs
│   ├── MessageId.cs
│   ├── ToolCall.cs
│   ├── SyncStatus.cs
│   ├── RunStatus.cs
│   └── MessageRole.cs
│
src/AgenticCoder.Application/
├── Conversation/
│   └── Persistence/
│       ├── IChatRepository.cs
│       ├── IRunRepository.cs
│       └── IMessageRepository.cs
│
src/AgenticCoder.Infrastructure/
├── Persistence/
│   └── Conversation/
│       ├── SqliteChatRepository.cs
│       ├── SqliteRunRepository.cs
│       ├── SqliteMessageRepository.cs
│       └── Migrations/
│           ├── 001_InitialSchema.sql
│           └── MigrationRunner.cs
```

### ChatId Value Object (Complete Implementation)

```csharp
// src/AgenticCoder.Domain/Conversation/ChatId.cs
namespace AgenticCoder.Domain.Conversation;

using System;

/// <summary>
/// Strongly-typed identifier for Chat entities using ULID format.
/// </summary>
public readonly record struct ChatId : IComparable<ChatId>
{
    private readonly string _value;

    public string Value => _value ?? throw new InvalidOperationException("ChatId not initialized");

    private ChatId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ChatId cannot be empty", nameof(value));

        if (value.Length != 26)
            throw new ArgumentException("ChatId must be 26 characters (ULID format)", nameof(value));

        _value = value;
    }

    public static ChatId NewId() => new(Ulid.NewUlid().ToString());

    public static ChatId From(string value) => new(value);

    public static ChatId Empty => new("00000000000000000000000000");

    public static bool TryParse(string? value, out ChatId chatId)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 26)
        {
            chatId = Empty;
            return false;
        }

        chatId = new ChatId(value);
        return true;
    }

    public int CompareTo(ChatId other) => string.CompareOrdinal(_value, other._value);

    public override string ToString() => _value;

    public static implicit operator string(ChatId id) => id.Value;
}
```

### Chat Entity (Complete Implementation)

```csharp
// src/AgenticCoder.Domain/Conversation/Chat.cs
namespace AgenticCoder.Domain.Conversation;

using System;
using System.Collections.Generic;

/// <summary>
/// Chat aggregate root representing a conversation thread.
/// Contains one or more Runs, each with multiple Messages.
/// </summary>
public sealed class Chat : AggregateRoot<ChatId>
{
    private readonly List<string> _tags = new();
    private readonly List<Run> _runs = new();

    public string Title { get; private set; }
    public IReadOnlyList<string> Tags => _tags.AsReadOnly();
    public IReadOnlyList<Run> Runs => _runs.AsReadOnly();
    public WorktreeId? WorktreeBinding { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public SyncStatus SyncStatus { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private const int MaxTitleLength = 500;

    // Private constructor for ORM/deserialization
    private Chat() { }

    private Chat(ChatId id, string title, WorktreeId? worktreeId, DateTimeOffset createdAt)
    {
        Id = id;
        Title = title;
        WorktreeBinding = worktreeId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        IsDeleted = false;
        SyncStatus = SyncStatus.Pending;
        Version = 1;
    }

    /// <summary>
    /// Creates a new Chat with generated ID and current timestamp.
    /// </summary>
    public static Chat Create(string title, WorktreeId? worktreeId = null)
    {
        ValidateTitle(title);
        return new Chat(ChatId.NewId(), title, worktreeId, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Reconstitutes a Chat from persisted data.
    /// </summary>
    public static Chat Reconstitute(
        ChatId id,
        string title,
        IEnumerable<string> tags,
        WorktreeId? worktreeId,
        bool isDeleted,
        DateTimeOffset? deletedAt,
        SyncStatus syncStatus,
        int version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        var chat = new Chat
        {
            Id = id,
            Title = title,
            WorktreeBinding = worktreeId,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt,
            SyncStatus = syncStatus,
            Version = version,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
        chat._tags.AddRange(tags);
        return chat;
    }

    /// <summary>
    /// Updates the chat title. Increments version.
    /// </summary>
    public void UpdateTitle(string newTitle)
    {
        ValidateTitle(newTitle);

        if (IsDeleted)
            throw new InvalidOperationException("Cannot update deleted chat");

        Title = newTitle;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
        SyncStatus = SyncStatus.Pending;
    }

    /// <summary>
    /// Adds a tag to the chat. Ignores duplicates.
    /// </summary>
    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be empty", nameof(tag));

        var normalizedTag = tag.Trim().ToLowerInvariant();

        if (!_tags.Contains(normalizedTag))
        {
            _tags.Add(normalizedTag);
            UpdatedAt = DateTimeOffset.UtcNow;
            Version++;
            SyncStatus = SyncStatus.Pending;
        }
    }

    /// <summary>
    /// Removes a tag from the chat.
    /// </summary>
    public bool RemoveTag(string tag)
    {
        var normalizedTag = tag.Trim().ToLowerInvariant();
        var removed = _tags.Remove(normalizedTag);

        if (removed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            Version++;
            SyncStatus = SyncStatus.Pending;
        }

        return removed;
    }

    /// <summary>
    /// Binds chat to a specific worktree.
    /// </summary>
    public void BindToWorktree(WorktreeId worktreeId)
    {
        WorktreeBinding = worktreeId;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
        SyncStatus = SyncStatus.Pending;
    }

    /// <summary>
    /// Soft deletes the chat. Sets IsDeleted and DeletedAt.
    /// </summary>
    public void Delete()
    {
        if (IsDeleted) return;

        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
        SyncStatus = SyncStatus.Pending;
    }

    /// <summary>
    /// Restores a soft-deleted chat.
    /// </summary>
    public void Restore()
    {
        if (!IsDeleted)
            throw new InvalidOperationException("Chat is not deleted");

        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
        SyncStatus = SyncStatus.Pending;
    }

    /// <summary>
    /// Marks the chat as synced to remote.
    /// </summary>
    public void MarkSynced()
    {
        SyncStatus = SyncStatus.Synced;
    }

    /// <summary>
    /// Marks the chat as having a sync conflict.
    /// </summary>
    public void MarkConflict()
    {
        SyncStatus = SyncStatus.Conflict;
    }

    /// <summary>
    /// Increments version for optimistic concurrency.
    /// </summary>
    internal void IncrementVersion()
    {
        Version++;
    }

    /// <summary>
    /// Adds a run to this chat.
    /// </summary>
    internal void AddRun(Run run)
    {
        _runs.Add(run);
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        if (title.Length > MaxTitleLength)
            throw new ArgumentException($"Title cannot exceed {MaxTitleLength} characters", nameof(title));
    }
}
```

### Run Entity (Complete Implementation)

```csharp
// src/AgenticCoder.Domain/Conversation/Run.cs
namespace AgenticCoder.Domain.Conversation;

using System;
using System.Collections.Generic;

/// <summary>
/// Run entity representing a single request/response cycle within a Chat.
/// </summary>
public sealed class Run : Entity<RunId>
{
    private readonly List<Message> _messages = new();

    public ChatId ChatId { get; }
    public string ModelId { get; }
    public RunStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int TokensIn { get; private set; }
    public int TokensOut { get; private set; }
    public int SequenceNumber { get; private set; }
    public string? ErrorMessage { get; private set; }
    public SyncStatus SyncStatus { get; private set; }
    public IReadOnlyList<Message> Messages => _messages.AsReadOnly();

    // Private constructor for ORM
    private Run() { }

    private Run(RunId id, ChatId chatId, string modelId, DateTimeOffset startedAt, int sequenceNumber)
    {
        Id = id;
        ChatId = chatId;
        ModelId = modelId;
        StartedAt = startedAt;
        SequenceNumber = sequenceNumber;
        Status = RunStatus.Running;
        SyncStatus = SyncStatus.Pending;
    }

    /// <summary>
    /// Creates a new Run for a Chat.
    /// </summary>
    public static Run Create(ChatId chatId, string modelId, int sequenceNumber = 0)
    {
        if (chatId == ChatId.Empty)
            throw new ArgumentException("ChatId cannot be empty", nameof(chatId));

        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("ModelId cannot be empty", nameof(modelId));

        return new Run(RunId.NewId(), chatId, modelId, DateTimeOffset.UtcNow, sequenceNumber);
    }

    /// <summary>
    /// Reconstitutes a Run from persisted data.
    /// </summary>
    public static Run Reconstitute(
        RunId id,
        ChatId chatId,
        string modelId,
        RunStatus status,
        DateTimeOffset startedAt,
        DateTimeOffset? completedAt,
        int tokensIn,
        int tokensOut,
        int sequenceNumber,
        string? errorMessage,
        SyncStatus syncStatus)
    {
        return new Run
        {
            Id = id,
            ChatId = chatId,
            ModelId = modelId,
            Status = status,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            TokensIn = tokensIn,
            TokensOut = tokensOut,
            SequenceNumber = sequenceNumber,
            ErrorMessage = errorMessage,
            SyncStatus = syncStatus
        };
    }

    /// <summary>
    /// Completes the run successfully with token counts.
    /// </summary>
    public void Complete(int tokensIn, int tokensOut)
    {
        if (Status != RunStatus.Running)
            throw new InvalidOperationException($"Cannot complete run in {Status} status");

        TokensIn = tokensIn;
        TokensOut = tokensOut;
        Status = RunStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        SyncStatus = SyncStatus.Pending;
    }

    /// <summary>
    /// Marks the run as failed with error message.
    /// </summary>
    public void Fail(string errorMessage)
    {
        if (Status != RunStatus.Running)
            throw new InvalidOperationException($"Cannot fail run in {Status} status");

        ErrorMessage = errorMessage ?? "Unknown error";
        Status = RunStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
        SyncStatus = SyncStatus.Pending;
    }

    /// <summary>
    /// Cancels the run.
    /// </summary>
    public void Cancel()
    {
        if (Status != RunStatus.Running)
            throw new InvalidOperationException($"Cannot cancel run in {Status} status");

        Status = RunStatus.Cancelled;
        CompletedAt = DateTimeOffset.UtcNow;
        SyncStatus = SyncStatus.Pending;
    }

    /// <summary>
    /// Adds a message to this run.
    /// </summary>
    internal void AddMessage(Message message)
    {
        _messages.Add(message);
    }

    /// <summary>
    /// Calculates run duration.
    /// </summary>
    public TimeSpan? Duration => CompletedAt.HasValue
        ? CompletedAt.Value - StartedAt
        : null;

    /// <summary>
    /// Total tokens used (input + output).
    /// </summary>
    public int TotalTokens => TokensIn + TokensOut;
}
```

### Message Entity (Complete Implementation)

```csharp
// src/AgenticCoder.Domain/Conversation/Message.cs
namespace AgenticCoder.Domain.Conversation;

using System;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// Message entity representing a single exchange within a Run.
/// </summary>
public sealed class Message : Entity<MessageId>
{
    private readonly List<ToolCall> _toolCalls = new();

    public RunId RunId { get; }
    public string Role { get; }
    public string Content { get; private set; }
    public IReadOnlyList<ToolCall> ToolCalls => _toolCalls.AsReadOnly();
    public DateTimeOffset CreatedAt { get; }
    public int SequenceNumber { get; }
    public SyncStatus SyncStatus { get; private set; }

    private const int MaxContentLength = 100 * 1024;  // 100KB

    // Private constructor for ORM
    private Message() { }

    private Message(MessageId id, RunId runId, string role, string content, DateTimeOffset createdAt, int sequenceNumber)
    {
        Id = id;
        RunId = runId;
        Role = role;
        Content = content;
        CreatedAt = createdAt;
        SequenceNumber = sequenceNumber;
        SyncStatus = SyncStatus.Pending;
    }

    /// <summary>
    /// Creates a new Message.
    /// </summary>
    public static Message Create(RunId runId, string role, string content, int sequenceNumber = 0)
    {
        if (runId == RunId.Empty)
            throw new ArgumentException("RunId cannot be empty", nameof(runId));

        ValidateRole(role);
        ValidateContent(content);

        return new Message(MessageId.NewId(), runId, role.ToLowerInvariant(), content, DateTimeOffset.UtcNow, sequenceNumber);
    }

    /// <summary>
    /// Reconstitutes a Message from persisted data.
    /// </summary>
    public static Message Reconstitute(
        MessageId id,
        RunId runId,
        string role,
        string content,
        IEnumerable<ToolCall>? toolCalls,
        DateTimeOffset createdAt,
        int sequenceNumber,
        SyncStatus syncStatus)
    {
        var message = new Message
        {
            Id = id,
            RunId = runId,
            Role = role,
            Content = content,
            CreatedAt = createdAt,
            SequenceNumber = sequenceNumber,
            SyncStatus = syncStatus
        };

        if (toolCalls != null)
            message._toolCalls.AddRange(toolCalls);

        return message;
    }

    /// <summary>
    /// Adds tool calls to assistant message.
    /// </summary>
    public void AddToolCalls(IEnumerable<ToolCall> toolCalls)
    {
        if (Role != "assistant")
            throw new InvalidOperationException("Tool calls can only be added to assistant messages");

        _toolCalls.AddRange(toolCalls);
        SyncStatus = SyncStatus.Pending;
    }

    /// <summary>
    /// Gets tool calls serialized as JSON.
    /// </summary>
    public string? GetToolCallsJson()
    {
        return _toolCalls.Count > 0
            ? JsonSerializer.Serialize(_toolCalls)
            : null;
    }

    private static void ValidateRole(string role)
    {
        var validRoles = new[] { "user", "assistant", "system", "tool" };
        if (!validRoles.Contains(role.ToLowerInvariant()))
            throw new ArgumentException($"Invalid role: {role}. Must be one of: {string.Join(", ", validRoles)}", nameof(role));
    }

    private static void ValidateContent(string content)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        if (content.Length > MaxContentLength)
            throw new ArgumentException($"Content cannot exceed {MaxContentLength} bytes", nameof(content));
    }
}
```

### ToolCall Value Object (Complete Implementation)

```csharp
// src/AgenticCoder.Domain/Conversation/ToolCall.cs
namespace AgenticCoder.Domain.Conversation;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Value object representing a tool invocation within a Message.
/// </summary>
public sealed record ToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; init; }

    [JsonPropertyName("function")]
    public string Function { get; init; }

    [JsonPropertyName("arguments")]
    public string Arguments { get; init; }

    [JsonPropertyName("result")]
    public string? Result { get; private set; }

    [JsonPropertyName("status")]
    public ToolCallStatus Status { get; private set; }

    public ToolCall(string id, string function, string arguments)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Function = function ?? throw new ArgumentNullException(nameof(function));
        Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        Status = ToolCallStatus.Pending;
    }

    /// <summary>
    /// Sets the result of the tool execution.
    /// </summary>
    public ToolCall WithResult(string result)
    {
        return this with
        {
            Result = result,
            Status = ToolCallStatus.Completed
        };
    }

    /// <summary>
    /// Marks the tool call as failed.
    /// </summary>
    public ToolCall WithError(string error)
    {
        return this with
        {
            Result = error,
            Status = ToolCallStatus.Failed
        };
    }

    /// <summary>
    /// Parses arguments as JSON.
    /// </summary>
    public T? ParseArguments<T>() where T : class
    {
        return JsonSerializer.Deserialize<T>(Arguments);
    }
}

public enum ToolCallStatus
{
    Pending,
    Running,
    Completed,
    Failed
}
```

### Enums (Complete Implementation)

```csharp
// src/AgenticCoder.Domain/Conversation/SyncStatus.cs
namespace AgenticCoder.Domain.Conversation;

/// <summary>
/// Tracks synchronization state between local and remote storage.
/// </summary>
public enum SyncStatus
{
    /// <summary>Created locally, not yet synced to remote.</summary>
    Pending,

    /// <summary>Successfully synced with remote.</summary>
    Synced,

    /// <summary>Local and remote versions conflict.</summary>
    Conflict,

    /// <summary>Sync failed after retries.</summary>
    Failed
}

// src/AgenticCoder.Domain/Conversation/RunStatus.cs
namespace AgenticCoder.Domain.Conversation;

/// <summary>
/// Status of a Run (request/response cycle).
/// </summary>
public enum RunStatus
{
    /// <summary>Run is currently executing.</summary>
    Running,

    /// <summary>Run completed successfully.</summary>
    Completed,

    /// <summary>Run failed with error.</summary>
    Failed,

    /// <summary>Run was cancelled by user.</summary>
    Cancelled
}
```

### IChatRepository Interface (Complete Implementation)

```csharp
// src/AgenticCoder.Application/Conversation/Persistence/IChatRepository.cs
namespace AgenticCoder.Application.Conversation.Persistence;

using AgenticCoder.Domain.Conversation;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Repository interface for Chat aggregate persistence.
/// </summary>
public interface IChatRepository
{
    /// <summary>Creates a new Chat and returns its ID.</summary>
    Task<ChatId> CreateAsync(Chat chat, CancellationToken ct);

    /// <summary>Gets a Chat by ID, optionally including Runs.</summary>
    Task<Chat?> GetByIdAsync(ChatId id, bool includeRuns = false, CancellationToken ct = default);

    /// <summary>Updates an existing Chat.</summary>
    /// <exception cref="ConcurrencyException">Thrown if version conflict.</exception>
    Task UpdateAsync(Chat chat, CancellationToken ct);

    /// <summary>Soft deletes a Chat.</summary>
    Task SoftDeleteAsync(ChatId id, CancellationToken ct);

    /// <summary>Lists Chats with filtering and pagination.</summary>
    Task<PagedResult<Chat>> ListAsync(ChatFilter filter, CancellationToken ct);

    /// <summary>Gets Chats bound to a specific worktree.</summary>
    Task<IReadOnlyList<Chat>> GetByWorktreeAsync(WorktreeId worktreeId, CancellationToken ct);

    /// <summary>Permanently deletes Chats marked deleted before cutoff.</summary>
    Task<int> PurgeDeletedAsync(DateTimeOffset before, CancellationToken ct);
}

/// <summary>Filter criteria for Chat queries.</summary>
public record ChatFilter
{
    public WorktreeId? WorktreeId { get; init; }
    public DateTimeOffset? CreatedAfter { get; init; }
    public DateTimeOffset? CreatedBefore { get; init; }
    public bool IncludeDeleted { get; init; } = false;
    public int Page { get; init; } = 0;
    public int PageSize { get; init; } = 50;
}

/// <summary>Paginated result container.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages - 1;
    public bool HasPreviousPage => Page > 0;
}
```

### SqliteChatRepository (Complete Implementation)

```csharp
// src/AgenticCoder.Infrastructure/Persistence/Conversation/SqliteChatRepository.cs
namespace AgenticCoder.Infrastructure.Persistence.Conversation;

using AgenticCoder.Application.Conversation.Persistence;
using AgenticCoder.Domain.Conversation;
using Dapper;
using Microsoft.Data.Sqlite;
using System.Text.Json;

/// <summary>
/// SQLite implementation of IChatRepository.
/// </summary>
public sealed class SqliteChatRepository : IChatRepository
{
    private readonly string _connectionString;

    public SqliteChatRepository(string databasePath)
    {
        _connectionString = $"Data Source={databasePath};Mode=ReadWriteCreate";
    }

    public async Task<ChatId> CreateAsync(Chat chat, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO chats (id, title, tags, worktree_id, is_deleted, deleted_at, 
                              sync_status, version, created_at, updated_at)
            VALUES (@Id, @Title, @Tags, @WorktreeId, @IsDeleted, @DeletedAt,
                   @SyncStatus, @Version, @CreatedAt, @UpdatedAt)";

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        await conn.ExecuteAsync(sql, new
        {
            Id = chat.Id.Value,
            Title = chat.Title,
            Tags = JsonSerializer.Serialize(chat.Tags),
            WorktreeId = chat.WorktreeBinding?.Value,
            IsDeleted = chat.IsDeleted ? 1 : 0,
            DeletedAt = chat.DeletedAt?.ToString("O"),
            SyncStatus = chat.SyncStatus.ToString(),
            Version = chat.Version,
            CreatedAt = chat.CreatedAt.ToString("O"),
            UpdatedAt = chat.UpdatedAt.ToString("O")
        });

        return chat.Id;
    }

    public async Task<Chat?> GetByIdAsync(ChatId id, bool includeRuns = false, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM chats WHERE id = @Id";

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        var row = await conn.QueryFirstOrDefaultAsync<ChatRow>(sql, new { Id = id.Value });
        if (row == null) return null;

        var chat = MapToChat(row);

        if (includeRuns)
        {
            var runs = await LoadRunsForChatAsync(conn, id, ct);
            foreach (var run in runs)
            {
                chat.AddRun(run);
            }
        }

        return chat;
    }

    public async Task UpdateAsync(Chat chat, CancellationToken ct)
    {
        const string sql = @"
            UPDATE chats
            SET title = @Title,
                tags = @Tags,
                worktree_id = @WorktreeId,
                is_deleted = @IsDeleted,
                deleted_at = @DeletedAt,
                sync_status = @SyncStatus,
                version = @Version,
                updated_at = @UpdatedAt
            WHERE id = @Id AND version = @ExpectedVersion";

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        var rowsAffected = await conn.ExecuteAsync(sql, new
        {
            Id = chat.Id.Value,
            Title = chat.Title,
            Tags = JsonSerializer.Serialize(chat.Tags),
            WorktreeId = chat.WorktreeBinding?.Value,
            IsDeleted = chat.IsDeleted ? 1 : 0,
            DeletedAt = chat.DeletedAt?.ToString("O"),
            SyncStatus = chat.SyncStatus.ToString(),
            Version = chat.Version,
            UpdatedAt = chat.UpdatedAt.ToString("O"),
            ExpectedVersion = chat.Version - 1
        });

        if (rowsAffected == 0)
        {
            throw new ConcurrencyException(
                $"Chat {chat.Id} was modified by another process. Reload and retry.");
        }
    }

    public async Task SoftDeleteAsync(ChatId id, CancellationToken ct)
    {
        const string sql = @"
            UPDATE chats
            SET is_deleted = 1,
                deleted_at = @DeletedAt,
                updated_at = @UpdatedAt,
                version = version + 1
            WHERE id = @Id AND is_deleted = 0";

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        await conn.ExecuteAsync(sql, new
        {
            Id = id.Value,
            DeletedAt = DateTimeOffset.UtcNow.ToString("O"),
            UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        });
    }

    public async Task<PagedResult<Chat>> ListAsync(ChatFilter filter, CancellationToken ct)
    {
        var whereClause = "WHERE 1=1";
        var parameters = new DynamicParameters();

        if (!filter.IncludeDeleted)
        {
            whereClause += " AND is_deleted = 0";
        }

        if (filter.WorktreeId.HasValue)
        {
            whereClause += " AND worktree_id = @WorktreeId";
            parameters.Add("WorktreeId", filter.WorktreeId.Value.Value);
        }

        if (filter.CreatedAfter.HasValue)
        {
            whereClause += " AND created_at >= @CreatedAfter";
            parameters.Add("CreatedAfter", filter.CreatedAfter.Value.ToString("O"));
        }

        if (filter.CreatedBefore.HasValue)
        {
            whereClause += " AND created_at <= @CreatedBefore";
            parameters.Add("CreatedBefore", filter.CreatedBefore.Value.ToString("O"));
        }

        var countSql = $"SELECT COUNT(*) FROM chats {whereClause}";
        var selectSql = $@"
            SELECT * FROM chats {whereClause}
            ORDER BY updated_at DESC
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", filter.PageSize);
        parameters.Add("Offset", filter.Page * filter.PageSize);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        var totalCount = await conn.ExecuteScalarAsync<int>(countSql, parameters);
        var rows = await conn.QueryAsync<ChatRow>(selectSql, parameters);

        var chats = rows.Select(MapToChat).ToList();
        return new PagedResult<Chat>(chats, totalCount, filter.Page, filter.PageSize);
    }

    public async Task<IReadOnlyList<Chat>> GetByWorktreeAsync(WorktreeId worktreeId, CancellationToken ct)
    {
        const string sql = @"
            SELECT * FROM chats
            WHERE worktree_id = @WorktreeId AND is_deleted = 0
            ORDER BY updated_at DESC";

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        var rows = await conn.QueryAsync<ChatRow>(sql, new { WorktreeId = worktreeId.Value });
        return rows.Select(MapToChat).ToList();
    }

    public async Task<int> PurgeDeletedAsync(DateTimeOffset before, CancellationToken ct)
    {
        const string sql = @"
            DELETE FROM chats
            WHERE is_deleted = 1 AND deleted_at < @Before";

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        return await conn.ExecuteAsync(sql, new { Before = before.ToString("O") });
    }

    private async Task<List<Run>> LoadRunsForChatAsync(SqliteConnection conn, ChatId chatId, CancellationToken ct)
    {
        const string sql = "SELECT * FROM runs WHERE chat_id = @ChatId ORDER BY sequence_number";
        var rows = await conn.QueryAsync<RunRow>(sql, new { ChatId = chatId.Value });
        return rows.Select(MapToRun).ToList();
    }

    private static Chat MapToChat(ChatRow row)
    {
        var tags = string.IsNullOrEmpty(row.Tags)
            ? Array.Empty<string>()
            : JsonSerializer.Deserialize<string[]>(row.Tags) ?? Array.Empty<string>();

        return Chat.Reconstitute(
            ChatId.From(row.Id),
            row.Title,
            tags,
            string.IsNullOrEmpty(row.WorktreeId) ? null : WorktreeId.From(row.WorktreeId),
            row.IsDeleted == 1,
            string.IsNullOrEmpty(row.DeletedAt) ? null : DateTimeOffset.Parse(row.DeletedAt),
            Enum.Parse<SyncStatus>(row.SyncStatus),
            row.Version,
            DateTimeOffset.Parse(row.CreatedAt),
            DateTimeOffset.Parse(row.UpdatedAt));
    }

    private static Run MapToRun(RunRow row)
    {
        return Run.Reconstitute(
            RunId.From(row.Id),
            ChatId.From(row.ChatId),
            row.ModelId,
            Enum.Parse<RunStatus>(row.Status),
            DateTimeOffset.Parse(row.StartedAt),
            string.IsNullOrEmpty(row.CompletedAt) ? null : DateTimeOffset.Parse(row.CompletedAt),
            row.TokensIn,
            row.TokensOut,
            row.SequenceNumber,
            row.ErrorMessage,
            Enum.Parse<SyncStatus>(row.SyncStatus));
    }

    // Row types for Dapper mapping
    private record ChatRow(string Id, string Title, string Tags, string? WorktreeId,
        int IsDeleted, string? DeletedAt, string SyncStatus, int Version,
        string CreatedAt, string UpdatedAt);

    private record RunRow(string Id, string ChatId, string ModelId, string Status,
        string StartedAt, string? CompletedAt, int TokensIn, int TokensOut,
        int SequenceNumber, string? ErrorMessage, string SyncStatus);
}
```

### Initial Migration (Complete Implementation)

```sql
-- src/AgenticCoder.Infrastructure/Persistence/Conversation/Migrations/001_InitialSchema.sql

-- Schema version tracking
CREATE TABLE IF NOT EXISTS schema_version (
    version INTEGER PRIMARY KEY,
    applied_at TEXT NOT NULL,
    description TEXT NOT NULL
);

-- Chats table
CREATE TABLE IF NOT EXISTS chats (
    id TEXT PRIMARY KEY,
    title TEXT NOT NULL,
    tags TEXT DEFAULT '[]',
    worktree_id TEXT,
    is_deleted INTEGER DEFAULT 0,
    deleted_at TEXT,
    sync_status TEXT DEFAULT 'Pending',
    version INTEGER DEFAULT 1,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

-- Runs table
CREATE TABLE IF NOT EXISTS runs (
    id TEXT PRIMARY KEY,
    chat_id TEXT NOT NULL REFERENCES chats(id) ON DELETE CASCADE,
    model_id TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'Running',
    started_at TEXT NOT NULL,
    completed_at TEXT,
    tokens_in INTEGER DEFAULT 0,
    tokens_out INTEGER DEFAULT 0,
    sequence_number INTEGER NOT NULL,
    error_message TEXT,
    sync_status TEXT DEFAULT 'Pending',
    UNIQUE(chat_id, sequence_number)
);

-- Messages table
CREATE TABLE IF NOT EXISTS messages (
    id TEXT PRIMARY KEY,
    run_id TEXT NOT NULL REFERENCES runs(id) ON DELETE CASCADE,
    role TEXT NOT NULL,
    content TEXT NOT NULL,
    tool_calls TEXT,
    created_at TEXT NOT NULL,
    sequence_number INTEGER NOT NULL,
    sync_status TEXT DEFAULT 'Pending',
    UNIQUE(run_id, sequence_number)
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_chats_worktree ON chats(worktree_id) WHERE worktree_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_chats_updated ON chats(updated_at DESC);
CREATE INDEX IF NOT EXISTS idx_chats_deleted ON chats(is_deleted, deleted_at);
CREATE INDEX IF NOT EXISTS idx_runs_chat ON runs(chat_id);
CREATE INDEX IF NOT EXISTS idx_runs_status ON runs(status);
CREATE INDEX IF NOT EXISTS idx_messages_run ON messages(run_id);
CREATE INDEX IF NOT EXISTS idx_messages_role ON messages(role);

-- Enable WAL mode for concurrent access
PRAGMA journal_mode=WAL;
PRAGMA synchronous=NORMAL;
PRAGMA busy_timeout=5000;

-- Record migration
INSERT INTO schema_version (version, applied_at, description)
VALUES (1, datetime('now'), 'Initial schema with chats, runs, messages');
```

### Error Codes

| Code | Meaning | HTTP Status |
|------|---------|-------------|
| ACODE-CONV-DATA-001 | Chat not found | 404 |
| ACODE-CONV-DATA-002 | Run not found | 404 |
| ACODE-CONV-DATA-003 | Message not found | 404 |
| ACODE-CONV-DATA-004 | Foreign key violation | 400 |
| ACODE-CONV-DATA-005 | Migration failed | 500 |
| ACODE-CONV-DATA-006 | Concurrency conflict | 409 |
| ACODE-CONV-DATA-007 | Validation error | 400 |

### Implementation Checklist

1. [ ] Create ChatId, RunId, MessageId value objects
2. [ ] Create SyncStatus, RunStatus, ToolCallStatus enums
3. [ ] Create Chat entity with validation
4. [ ] Create Run entity with status transitions
5. [ ] Create Message entity with tool calls
6. [ ] Create ToolCall value object
7. [ ] Create IChatRepository, IRunRepository, IMessageRepository interfaces
8. [ ] Create ChatFilter and PagedResult types
9. [ ] Implement SqliteChatRepository with Dapper
10. [ ] Implement SqliteRunRepository with Dapper
11. [ ] Implement SqliteMessageRepository with Dapper
12. [ ] Create 001_InitialSchema.sql migration
13. [ ] Create MigrationRunner for schema management
14. [ ] Add DI registration in ServiceCollectionExtensions
15. [ ] Write unit tests for domain entities
16. [ ] Write integration tests for repositories

### Rollout Plan

1. **Phase 1:** Domain entities and value objects
2. **Phase 2:** Repository interfaces and filter types
3. **Phase 3:** SQLite repository implementations
4. **Phase 4:** Migration framework and initial schema
5. **Phase 5:** DI registration and configuration
6. **Phase 6:** PostgreSQL implementations (future)

---

**End of Task 049.a Specification**