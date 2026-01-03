# Task 049.a: Conversation Data Model + Storage Provider Abstraction

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 2 – CLI + Orchestration Core  
**Dependencies:** Task 049 (Parent), Task 050 (Workspace DB), Task 011 (Session), Task 026 (Storage)  

---

## Description

Task 049.a defines the canonical data model for conversation history and the storage provider abstraction. This is the foundation for all conversation operations—the schemas that structure data and the interfaces that hide storage details.

The data model defines three core entities. Chat represents a conversation thread—a container for related interactions. Run represents a single user request and agent response cycle. Message represents individual exchanges: user prompts, agent text, tool calls, and tool results.

Schema design prioritizes offline-first operation. Every entity has local and remote IDs. Sync metadata tracks what needs synchronization. Tombstones enable soft-delete with eventual sync. Version fields support conflict detection.

The storage provider abstraction follows the repository pattern. IChatRepository, IRunRepository, and IMessageRepository define operations independent of storage technology. SQLite implements local storage. PostgreSQL implements remote storage. The abstraction enables seamless switching.

Entity relationships are hierarchical. A Chat contains many Runs. A Run contains many Messages. A Message may contain many ToolCalls. Cascading operations respect these relationships—deleting a Chat removes its Runs and Messages.

Identifiers use ULID format—lexicographically sortable, time-based, collision-resistant. Local IDs are generated immediately. Remote IDs are assigned on sync. The mapping table correlates local and remote identities.

Metadata supports rich functionality. Chats have titles, tags, and worktree bindings. Runs track status, duration, and token usage. Messages store role, content, timestamps, and optional tool invocations.

Schema migrations are version-controlled. Each migration has an up and down script. Migrations run automatically on startup. Version tracking prevents duplicate application. Rollback is supported for failed deployments.

The provider abstraction supports pluggable storage. The interface is storage-agnostic. Implementations handle connection pooling, transaction management, and error translation. Mock implementations enable testing without databases.

Type mapping between C# and SQL is explicit. DateTimeOffset maps to TEXT (ISO 8601) in SQLite, TIMESTAMPTZ in PostgreSQL. JSON content maps to TEXT in SQLite, JSONB in PostgreSQL. Enums map to TEXT with validation.

Error handling translates storage exceptions to domain errors. Connection failures, constraint violations, and timeout errors have specific error codes. Retry policies are configurable. Circuit breakers prevent cascade failures.

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
- **Query optimization** - Performance tuning
- **Sharding** - Single database
- **Replication** - Not in scope
- **Encryption at rest** - Task 021
- **Custom fields** - Fixed schema

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

- [ ] AC-001: ChatId is ULID
- [ ] AC-002: Title stored
- [ ] AC-003: Timestamps work
- [ ] AC-004: Tags stored as JSON
- [ ] AC-005: Soft delete works
- [ ] AC-006: Version increments

### Run Entity

- [ ] AC-007: RunId is ULID
- [ ] AC-008: ChatId foreign key
- [ ] AC-009: Status enum works
- [ ] AC-010: Timestamps work
- [ ] AC-011: Tokens tracked

### Message Entity

- [ ] AC-012: MessageId is ULID
- [ ] AC-013: RunId foreign key
- [ ] AC-014: Role enum works
- [ ] AC-015: Content stored
- [ ] AC-016: ToolCalls as JSON

### Repository

- [ ] AC-017: Create works
- [ ] AC-018: Get by ID works
- [ ] AC-019: Update works
- [ ] AC-020: Delete works
- [ ] AC-021: List with pagination
- [ ] AC-022: Async/cancellation

### SQLite Provider

- [ ] AC-023: CRUD works
- [ ] AC-024: WAL mode
- [ ] AC-025: Transactions

### PostgreSQL Provider

- [ ] AC-026: CRUD works
- [ ] AC-027: Connection pool
- [ ] AC-028: Transactions

### Migrations

- [ ] AC-029: Auto-apply on start
- [ ] AC-030: Version tracked
- [ ] AC-031: Rollback works

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Conversation/Model/
├── ChatTests.cs
│   ├── Should_Create_With_Valid_Id()
│   ├── Should_Validate_Title_Length()
│   └── Should_Track_Version()
│
├── RunTests.cs
│   ├── Should_Require_ChatId()
│   └── Should_Track_Status()
│
└── MessageTests.cs
    ├── Should_Require_RunId()
    └── Should_Serialize_ToolCalls()
```

### Integration Tests

```
Tests/Integration/Conversation/Storage/
├── SqliteRepositoryTests.cs
│   ├── Should_Create_Chat()
│   ├── Should_Query_By_Worktree()
│   └── Should_Handle_Concurrent_Access()
│
└── PostgresRepositoryTests.cs
    ├── Should_Create_Chat()
    └── Should_Pool_Connections()
```

### E2E Tests

```
Tests/E2E/Conversation/
├── DataModelE2ETests.cs
│   ├── Should_Create_Full_Hierarchy()
│   └── Should_Migrate_Schema()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Insert Chat | 5ms | 10ms |
| Insert Message | 3ms | 10ms |
| Get by ID | 2ms | 5ms |
| List 100 | 25ms | 50ms |

---

## User Verification Steps

### Scenario 1: Create Chat

1. Use repository to create chat
2. Query database directly
3. Verify: Row exists with correct data

### Scenario 2: Create Run

1. Create chat, then run
2. Query runs table
3. Verify: Foreign key set

### Scenario 3: Create Message

1. Create chat, run, message
2. Query messages table
3. Verify: Hierarchy intact

### Scenario 4: Soft Delete

1. Create chat
2. Soft delete chat
3. Verify: IsDeleted true, DeletedAt set

### Scenario 5: Migration

1. Add new migration
2. Run migrations
3. Verify: Schema updated

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
│       ├── PostgresChatRepository.cs
│       ├── PostgresRunRepository.cs
│       ├── PostgresMessageRepository.cs
│       └── Migrations/
│           ├── 001_InitialSchema.cs
│           └── MigrationRunner.cs
```

### Chat Entity

```csharp
namespace AgenticCoder.Domain.Conversation;

public sealed class Chat : AggregateRoot<ChatId>
{
    public string Title { get; private set; }
    public IReadOnlyList<string> Tags { get; }
    public WorktreeId? WorktreeId { get; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public SyncStatus SyncStatus { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }
    
    public static Chat Create(string title);
    public void Rename(string newTitle);
    public void SoftDelete();
    public void Restore();
    public void MarkSynced();
}
```

### IChatRepository Interface

```csharp
namespace AgenticCoder.Application.Conversation.Persistence;

public interface IChatRepository
{
    Task<ChatId> CreateAsync(Chat chat, CancellationToken ct);
    Task<Chat?> GetByIdAsync(ChatId id, CancellationToken ct);
    Task UpdateAsync(Chat chat, CancellationToken ct);
    Task SoftDeleteAsync(ChatId id, CancellationToken ct);
    Task<PagedResult<Chat>> ListAsync(ChatFilter filter, CancellationToken ct);
    Task<Chat?> GetByWorktreeAsync(WorktreeId worktreeId, CancellationToken ct);
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-CONV-DATA-001 | Chat not found |
| ACODE-CONV-DATA-002 | Run not found |
| ACODE-CONV-DATA-003 | Message not found |
| ACODE-CONV-DATA-004 | Foreign key violation |
| ACODE-CONV-DATA-005 | Migration failed |

### Implementation Checklist

1. [ ] Create domain entities
2. [ ] Create value objects (IDs)
3. [ ] Create enums
4. [ ] Create repository interfaces
5. [ ] Implement SQLite repositories
6. [ ] Implement PostgreSQL repositories
7. [ ] Create migration framework
8. [ ] Write initial migration
9. [ ] Add DI registration
10. [ ] Write unit tests
11. [ ] Write integration tests

### Rollout Plan

1. **Phase 1:** Domain entities
2. **Phase 2:** Repository interfaces
3. **Phase 3:** SQLite implementation
4. **Phase 4:** Migrations
5. **Phase 5:** PostgreSQL implementation
6. **Phase 6:** DI registration

---

**End of Task 049.a Specification**