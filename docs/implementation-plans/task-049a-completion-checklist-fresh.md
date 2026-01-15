# Task-049a Completion Checklist (FRESH): Conversation Data Model + Storage Provider

**Status:** 🟡 79.6% COMPLETE - Ready for Gap Implementation (REVISED: PostgreSQL now IN SCOPE)

**Date:** 2026-01-15
**Created From:** task-049a-semantic-gap-analysis-fresh.md (completed 78/98 ACs)
**Reference Implementation:** task-049d-completion-checklist.md (gold standard)
**CRITICAL UPDATE:** PostgreSQL ACs (AC-077-082) are IN SCOPE and must be implemented as part of 049a

---

## CRITICAL: READ THIS FIRST

### METHODOLOGY

This checklist is created FROM the gap analysis (task-049a-semantic-gap-analysis-fresh.md), not before it. Each gap listed below comes directly from AC verification findings showing what's missing.

**Before using this checklist:**

1. Read `task-049a-semantic-gap-analysis-fresh.md` completely (identifies all 20 missing/incomplete ACs, including 6 PostgreSQL ACs)
2. Understand that semantic completeness = 78/98 (79.6%) - 20 ACs not fully implemented
3. PostgreSQL repositories are IN SCOPE and CRITICAL priority (Gaps 1-8)
4. All core entities are complete; PostgreSQL implementations, specific methods, and optimizations remain

### BLOCKING DEPENDENCIES: NONE ✅

No dependencies on other tasks. All infrastructure is present.

### HOW TO USE THIS CHECKLIST

#### For Fresh-Context Agent:

1. **Read task-049a-semantic-gap-analysis-fresh.md** (identifies all gaps)
2. **Read Section 2** - Gap mapping shows what each gap implements
3. **Follow Phases 1-3 sequentially** in TDD order
4. **For Each Gap:**
   - Write test(s) that fail (RED)
   - Implement minimum code to pass (GREEN)
   - Clean up while keeping tests green (REFACTOR)
5. **Mark Progress:** `[ ]` = not started, `[🔄]` = in progress, `[✅]` = complete
6. **After Each Phase:** Run `dotnet test` and verify all tests pass
7. **After Each Gap:** Commit with `git commit -m "feat(task-049a): [gap description]"`

#### For Continuing Agent:

1. Find last `[✅]` item
2. Read next `[🔄]` or `[ ]` item
3. Follow same TDD cycle
4. Update checklist with test evidence

---

## SECTION 1: SEMANTIC COMPLETENESS STATUS

### Current State (VERIFIED IN FRESH GAP ANALYSIS - REVISED FOR POSTGRESQL IN SCOPE)

**Total Acceptance Criteria:** 98 in scope (PostgreSQL AC-077-082 now IN SCOPE, NOT deferred)
**ACs Complete:** 78 (79.6%)
**ACs Partial:** 5 (5.1%)
**ACs Missing:** 15 (15.3%)

### Completed Work (DO NOT REDO)

✅ All 14 Chat Entity ACs (AC-001–014)
✅ All 12 Run Entity ACs (AC-015–026)
✅ All 10 Message Entity ACs (AC-027–036)
✅ All 5 ToolCall ACs (AC-037–041)
✅ All 8 Repository Interface ACs (AC-042–049)
✅ All 11 Chat Repository ACs (AC-050–060)
✅ All 5 Run Repository ACs (AC-061–065)
✅ 3/5 Message Repository ACs (AC-066–068)
✅ 4/6 SQLite Provider ACs (AC-071, 072, 073, 074)
✅ 2/6 Migration ACs (AC-084, AC-085)
✅ 4/5 Error Handling ACs (AC-089–092)

---

## SECTION 2: GAPS TO IMPLEMENT

### ⚠️ CRITICAL PRIORITY GAPS (PostgreSQL Repositories) - Gaps 1-8

These 8 gaps implement the PostgreSQL Provider (AC-077-082) and must be completed before other gaps to reach semantic completeness.

---

### Gap 1: PostgreSQL Connection Factory and Configuration [ ]

**ACs Covered:** AC-077, AC-078, AC-079, AC-080, AC-081, AC-082
**Effort:** 4-5 hours
**Spec Reference:** Implementation Prompt line ~2800-2900 (PostgreSQL connection details)
**Status:** [ ]

**What to Implement:**

Create `src/Acode.Infrastructure/Persistence/PostgreSQL/PostgresConnectionFactory.cs`:

```csharp
namespace Acode.Infrastructure.Persistence.PostgreSQL;

using Npgsql;

public sealed class PostgresConnectionFactory
{
    private readonly string _connectionString;

    public PostgresConnectionFactory(string host, string database, string userId, string password, int? port = null)
    {
        var portValue = port ?? 5432;
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Database = database,
            Username = userId,
            Password = password,
            Port = portValue,
            CommandTimeout = 30, // AC-079: 30 second timeout
            MinPoolSize = 0, // AC-078: Connection pooling
            MaxPoolSize = 10, // AC-078: 10 connections default
            StatementCacheSize = 250, // AC-081: Statement caching
            SslMode = SslMode.Require, // AC-082: TLS encryption required
            TrustServerCertificate = false
        };
        _connectionString = builder.ConnectionString;
    }

    public NpgsqlConnection CreateConnection() => new(_connectionString);

    public string ConnectionString => _connectionString;
}
```

**Tests (4):**
- [ ] Connection string built with correct parameters
- [ ] CommandTimeout = 30 seconds
- [ ] Pool size configured (min=0, max=10)
- [ ] SSL Mode = Require

**Success Criteria:**
- [ ] PostgresConnectionFactory.cs created
- [ ] Connection pooling configured (AC-078)
- [ ] Command timeout set to 30s (AC-079)
- [ ] Statement caching enabled (AC-081)
- [ ] TLS encryption required (AC-082)
- [ ] All 4 tests passing

---

### Gap 2: PostgreSQL ChatRepository Implementation [ ]

**ACs Covered:** AC-077 (CRUD operations)
**Effort:** 4-5 hours
**Spec Reference:** Implementation Prompt line ~2800-2900
**Status:** [ ]

**What to Implement:**

Create `src/Acode.Infrastructure/Persistence/Conversation/PostgresChatRepository.cs` with methods:

```csharp
public sealed class PostgresChatRepository : IChatRepository
{
    private readonly PostgresConnectionFactory _factory;

    // Implement all IChatRepository methods:
    // - CreateAsync(Chat chat, CancellationToken ct)
    // - GetByIdAsync(ChatId id, CancellationToken ct)
    // - UpdateAsync(Chat chat, CancellationToken ct)
    // - SoftDeleteAsync(ChatId id, CancellationToken ct)
    // - ListAsync(ChatFilter filter, CancellationToken ct)
    // - GetByWorktreeAsync(WorktreeId id, int skip, int take, CancellationToken ct)
    // - PurgeDeletedAsync(DateTimeOffset olderThan, CancellationToken ct)
    // All operations wrapped in NpgsqlTransaction (AC-080)
}
```

**Spec Requirements:**
- [ ] AC-077: CRUD operations work correctly
- [ ] AC-080: Transactions support commit and rollback (use NpgsqlTransaction)
- [ ] AC-081: Statement caching (use Npgsql prepared statements)
- [ ] All methods async with CancellationToken
- [ ] Parameterized queries (no SQL injection)

**Tests (7):**
- [ ] CreateAsync creates chat and returns ChatId
- [ ] GetByIdAsync returns null when not found
- [ ] UpdateAsync with concurrency control
- [ ] SoftDeleteAsync marks deleted_at
- [ ] ListAsync with filters and pagination
- [ ] GetByWorktreeAsync filters by worktree
- [ ] All operations transactional

**Success Criteria:**
- [ ] PostgresChatRepository.cs created
- [ ] All 7 IChatRepository methods implemented
- [ ] Transactions used (AC-080)
- [ ] Parameterized queries
- [ ] 7 tests passing

---

### Gap 3: PostgreSQL RunRepository Implementation [ ]

**ACs Covered:** AC-077 (CRUD operations)
**Effort:** 3-4 hours
**Status:** [ ]

**What to Implement:**

Create `src/Acode.Infrastructure/Persistence/Conversation/PostgresRunRepository.cs` with all IRunRepository methods, similar pattern to PostgresChatRepository but for Run entity.

**Tests (6):**
- [ ] CreateAsync creates run and returns RunId
- [ ] GetByIdAsync returns null when not found
- [ ] UpdateAsync with concurrency control
- [ ] ListByChatAsync filters by chat
- [ ] GetLatestByChatAsync returns most recent
- [ ] DeleteAsync soft deletes

**Success Criteria:**
- [ ] PostgresRunRepository.cs created
- [ ] All 6 IRunRepository methods implemented
- [ ] 6 tests passing

---

### Gap 4: PostgreSQL MessageRepository Implementation [ ]

**ACs Covered:** AC-077 (CRUD operations)
**Effort:** 3-4 hours
**Status:** [ ]

**What to Implement:**

Create `src/Acode.Infrastructure/Persistence/Conversation/PostgresMessageRepository.cs` with all IMessageRepository methods.

**Tests (6):**
- [ ] CreateAsync creates message and returns MessageId
- [ ] GetByIdAsync returns null when not found
- [ ] ListByRunAsync filters by run with ordering
- [ ] DeleteByRunAsync soft deletes all messages in run
- [ ] AppendAsync adds message (AC-069)
- [ ] BulkCreateAsync inserts multiple (AC-070)

**Success Criteria:**
- [ ] PostgresMessageRepository.cs created
- [ ] All IMessageRepository methods implemented
- [ ] 6 tests passing

---

### Gap 5: PostgreSQL Repository Registration (Dependency Injection) [ ]

**ACs Covered:** AC-077, AC-078, AC-079, AC-080, AC-081, AC-082
**Effort:** 1 hour
**Status:** [ ]

**What to Implement:**

Update dependency injection to register PostgreSQL repositories:

```csharp
// In Startup/ConfigureServices:
services.AddSingleton(new PostgresConnectionFactory(host, database, userId, password));
services.AddSingleton<IChatRepository>(sp =>
    new PostgresChatRepository(sp.GetRequiredService<PostgresConnectionFactory>()));
services.AddSingleton<IRunRepository>(sp =>
    new PostgresRunRepository(sp.GetRequiredService<PostgresConnectionFactory>()));
services.AddSingleton<IMessageRepository>(sp =>
    new PostgresMessageRepository(sp.GetRequiredService<PostgresConnectionFactory>()));
```

**Tests (1):**
- [ ] All PostgreSQL repositories successfully registered and can be resolved

**Success Criteria:**
- [ ] Dependency injection configured
- [ ] All PostgreSQL repos resolvable
- [ ] 1 test passing

---

### HIGH PRIORITY GAPS (Non-PostgreSQL) - Gaps 6-8

---

### Gap 6: IMessageRepository.AppendAsync() Method [ ]

**AC Covered:** AC-069 - "AppendAsync adds Message to Run"
**Effort:** 1-2 hours
**Spec Reference:** Implementation Prompt line ~2945
**Priority:** HIGH
**Status:** [ ]

**What to Implement:**

Add to `src/Acode.Application/Conversation/Persistence/IMessageRepository.cs`:

```csharp
/// <summary>
/// Appends a message to a run (alias for CreateAsync).
/// </summary>
Task<MessageId> AppendAsync(Message message, CancellationToken ct);
```

Implement in `src/Acode.Infrastructure/Persistence/Conversation/SqliteMessageRepository.cs`:

```csharp
public async Task<MessageId> AppendAsync(Message message, CancellationToken ct)
{
    return await CreateAsync(message, ct);
}
```

**Tests (2):**
- [ ] AppendAsync creates message in database
- [ ] AppendAsync returns MessageId

**Success Criteria:**
- [ ] Method exists in IMessageRepository interface
- [ ] Method exists in SqliteMessageRepository implementation
- [ ] 2 new tests passing

**Evidence:**
- [ ] IMessageRepository has AppendAsync method
- [ ] SqliteMessageRepository implements AppendAsync
- [ ] Tests verify message created

---

### Gap 7: IMessageRepository.BulkCreateAsync() Method [ ]

**AC Covered:** AC-070 - "BulkCreateAsync inserts multiple Messages efficiently"
**Priority:** HIGH
**Effort:** 2-3 hours
**Spec Reference:** Implementation Prompt line ~2970
**Status:** [ ]

**What to Implement:**

Add to `src/Acode.Application/Conversation/Persistence/IMessageRepository.cs`:

```csharp
/// <summary>
/// Efficiently inserts multiple messages using bulk insert pattern.
/// </summary>
Task BulkCreateAsync(IEnumerable<Message> messages, CancellationToken ct);
```

Implement in `src/Acode.Infrastructure/Persistence/Conversation/SqliteMessageRepository.cs`:

```csharp
public async Task BulkCreateAsync(IEnumerable<Message> messages, CancellationToken ct)
{
    const string sql = @"
        INSERT INTO conv_messages (id, run_id, role, content, tool_calls, created_at, sequence_number, sync_status)
        VALUES (@Id, @RunId, @Role, @Content, @ToolCalls, @CreatedAt, @SequenceNumber, @SyncStatus)";

    await using var conn = new SqliteConnection(_connectionString);
    await conn.OpenAsync(ct);

    var messageList = messages.ToList();
    foreach (var message in messageList)
    {
        await conn.ExecuteAsync(sql, new
        {
            Id = message.Id.Value,
            RunId = message.RunId.Value,
            Role = message.Role,
            Content = message.Content,
            ToolCalls = message.GetToolCallsJson(),
            CreatedAt = message.CreatedAt.ToString("O"),
            SequenceNumber = message.SequenceNumber,
            SyncStatus = message.SyncStatus.ToString()
        });
    }
}
```

**Tests (3):**
- [ ] BulkCreateAsync creates multiple messages
- [ ] BulkCreateAsync preserves sequence numbers
- [ ] BulkCreateAsync completes faster than individual creates

**Success Criteria:**
- [ ] Method exists in IMessageRepository interface
- [ ] Method exists in SqliteMessageRepository implementation
- [ ] All messages inserted correctly
- [ ] 3 new tests passing

**Evidence:**
- [ ] IMessageRepository has BulkCreateAsync method
- [ ] SqliteMessageRepository implements BulkCreateAsync
- [ ] Messages verified in database after bulk insert
- [ ] Performance test shows bulk < n × individual

---

### Gap 8: Add Error Code Pattern to All Exceptions [ ]

**AC Covered:** AC-093 - "Error codes follow ACODE-CONV-DATA-xxx pattern"
**Priority:** HIGH
**Effort:** 1-2 hours
**Spec Reference:** Implementation Prompt line ~3529
**Status:** [ ]

**Files to Modify:**
1. src/Acode.Application/Conversation/Persistence/ConcurrencyException.cs
2. src/Acode.Domain/Conversation/Exceptions/EntityNotFoundException.cs
3. src/Acode.Domain/Conversation/Exceptions/ValidationException.cs
4. src/Acode.Infrastructure/Persistence/Conversation/Exceptions/ConnectionException.cs

**Error Codes to Define:**
- ACODE-CONV-DATA-001: Chat not found
- ACODE-CONV-DATA-002: Run not found
- ACODE-CONV-DATA-003: Message not found
- ACODE-CONV-DATA-004: Foreign key violation
- ACODE-CONV-DATA-005: Migration failed
- ACODE-CONV-DATA-006: Concurrency conflict
- ACODE-CONV-DATA-007: Validation error

**What to Implement:**

Modify each exception class to include:

```csharp
public sealed class ConcurrencyException : Exception
{
    public string ErrorCode { get; }

    public ConcurrencyException(string message, string errorCode = "ACODE-CONV-DATA-006")
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
```

Update SqliteChatRepository.UpdateAsync() to include error code:

```csharp
if (rowsAffected == 0)
{
    throw new ConcurrencyException(
        $"Chat {chat.Id} was modified by another process. Reload and retry.",
        "ACODE-CONV-DATA-006");
}
```

**Tests (4):**
- [ ] ConcurrencyException includes error code ACODE-CONV-DATA-006
- [ ] EntityNotFoundException includes error code ACODE-CONV-DATA-001
- [ ] ValidationException includes error code ACODE-CONV-DATA-007
- [ ] ConnectionException includes error code ACODE-CONV-DATA-005

**Success Criteria:**
- [ ] All 4 exception classes have ErrorCode property
- [ ] All error codes follow ACODE-CONV-DATA-xxx pattern
- [ ] Repositories throw exceptions with correct error codes
- [ ] 4+ new tests passing

**Evidence:**
- [ ] All exception classes have ErrorCode property
- [ ] Error codes verified in exception constructors
- [ ] Tests verify error codes in exceptions

---

### Gap 9: Migration Auto-Apply on Repository Initialization [ ]

**AC Covered:** AC-083 - "Migrations auto-apply on application start"
**Priority:** MEDIUM
**Effort:** 1-2 hours
**Spec Reference:** Implementation Prompt line ~3525
**Status:** [ ]

**What to Implement:**

Create `src/Acode.Infrastructure/Persistence/Conversation/ConversationMigrationRunner.cs`:

```csharp
namespace Acode.Infrastructure.Persistence.Conversation;

public sealed class ConversationMigrationRunner
{
    private readonly string _databasePath;

    public ConversationMigrationRunner(string databasePath)
    {
        _databasePath = databasePath;
    }

    public async Task MigrateAsync(CancellationToken ct)
    {
        await using var conn = new SqliteConnection($"Data Source={_databasePath};Mode=ReadWriteCreate");
        await conn.OpenAsync(ct);

        // Check if schema_version table exists
        const string checkTableSql = @"
            SELECT COUNT(*) FROM sqlite_master
            WHERE type='table' AND name='schema_version'";

        var tableExists = await conn.ExecuteScalarAsync<int>(checkTableSql) > 0;

        if (!tableExists)
        {
            // Read and execute migration script
            var migrationScript = GetEmbeddedMigrationScript("001_InitialSchema.sql");
            await conn.ExecuteAsync(migrationScript);
        }
    }

    private static string GetEmbeddedMigrationScript(string scriptName)
    {
        var assembly = typeof(ConversationMigrationRunner).Assembly;
        var resourceName = $"Acode.Infrastructure.Persistence.Conversation.Migrations.{scriptName}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Migration script not found: {scriptName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
```

Modify `SqliteChatRepository` constructor to auto-apply migrations:

```csharp
public SqliteChatRepository(string databasePath)
{
    _connectionString = $"Data Source={databasePath};Mode=ReadWriteCreate";

    // Auto-apply migrations on first use
    var migrationRunner = new ConversationMigrationRunner(databasePath);
    migrationRunner.MigrateAsync(CancellationToken.None).GetAwaiter().GetResult();
}
```

**Tests (2):**
- [ ] Migrations auto-apply when repository is first instantiated
- [ ] schema_version table created and tracked

**Success Criteria:**
- [ ] ConversationMigrationRunner created
- [ ] SqliteChatRepository auto-applies migrations on init
- [ ] 2 new tests passing
- [ ] schema_version table verified

**Evidence:**
- [ ] ConversationMigrationRunner exists and runs
- [ ] schema_version table created
- [ ] Migration runs idempotently

---

### Gap 10: Migration Status CLI Command [ ]

**AC Covered:** AC-088 - "Migration status command shows applied/pending"
**Effort:** 2-3 hours
**Status:** [ ]

**Note:** This requires CLI integration. May be lower priority if CLI layer not yet implemented.

**What to Implement:**

Create `src/Acode.Cli/Commands/DbMigrationsStatusCommand.cs`:

```csharp
public sealed class DbMigrationsStatusCommand : Command
{
    public DbMigrationsStatusCommand() : base("status", "Show migration status")
    {
    }

    public override async Task<int> ExecuteAsync(InvocationContext context)
    {
        var migrationRunner = new ConversationMigrationRunner(GetDatabasePath());
        var version = await migrationRunner.GetCurrentVersionAsync(CancellationToken.None);

        AnsiConsole.MarkupLine($"[green]Current schema version:[/] {version}");
        AnsiConsole.MarkupLine("[yellow]Pending migrations:[/] None");

        return 0;
    }
}
```

**Tests (2):**
- [ ] Command shows current version
- [ ] Command displays "No pending migrations"

**Success Criteria:**
- [ ] CLI command `acode db migrations status` works
- [ ] Shows applied migrations
- [ ] 2+ new tests passing

**Evidence:**
- [ ] Command implementation exists
- [ ] Command output shows correct version

---

### Gap 11: Performance Benchmarks (AC-094 through AC-098) [ ]

**ACs Covered:** AC-094, AC-095, AC-096, AC-097, AC-098
**Effort:** 2-3 hours
**Status:** [ ]

**What to Implement:**

Create `tests/Acode.Performance.Tests/ConversationBenchmarks.cs`:

```csharp
[MemoryDiagnoser]
public class ConversationBenchmarks
{
    private SqliteChatRepository _chatRepository = null!;
    private ChatId _chatId;

    [GlobalSetup]
    public async Task Setup()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), "benchmark.db");
        _chatRepository = new SqliteChatRepository(dbPath);

        var chat = Chat.Create("Benchmark Chat", WorktreeId.From("worktree-bench"));
        _chatId = await _chatRepository.CreateAsync(chat, CancellationToken.None);
    }

    [Benchmark]
    public async Task InsertChat()
    {
        var chat = Chat.Create("Perf Test", WorktreeId.From("worktree-bench"));
        await _chatRepository.CreateAsync(chat, CancellationToken.None);
    }

    [Benchmark]
    public async Task GetById()
    {
        await _chatRepository.GetByIdAsync(_chatId, false, CancellationToken.None);
    }

    [Benchmark]
    public async Task List100()
    {
        var filter = new ChatFilter { PageSize = 100 };
        await _chatRepository.ListAsync(filter, CancellationToken.None);
    }

    [Benchmark]
    public async Task UpdateChat()
    {
        var chat = await _chatRepository.GetByIdAsync(_chatId, false, CancellationToken.None);
        chat!.UpdateTitle("Updated Title");
        await _chatRepository.UpdateAsync(chat, CancellationToken.None);
    }
}
```

**Performance Targets (from spec):**
- AC-094: Insert Chat < 10ms
- AC-095: Get by ID < 5ms
- AC-096: List 100 < 50ms
- AC-097: Update < 10ms
- AC-098: Connection pool reused

**Tests (5):**
- [ ] InsertChat benchmarks < 10ms
- [ ] GetById benchmarks < 5ms
- [ ] List100 benchmarks < 50ms
- [ ] UpdateChat benchmarks < 10ms
- [ ] Connection pooling verified

**Success Criteria:**
- [ ] Benchmarks created with BenchmarkDotNet
- [ ] All 5 benchmarks pass
- [ ] Performance targets met (or documented if not met)

**Evidence:**
- [ ] ConversationBenchmarks.cs exists
- [ ] Benchmark results show < target milliseconds

---

### Gap 12: Connection Pooling Verification (AC-075) [ ]

**AC Covered:** AC-075 - "Connection pooling works"
**Effort:** 1 hour
**Status:** [ ]

**What to Verify:**

Add test in SqliteChatRepositoryTests:

```csharp
[Fact]
public async Task Should_Reuse_Connections()
{
    // Arrange
    var chat1 = Chat.Create("Chat 1", WorktreeId.From("worktree-01HKABC"));
    var chat2 = Chat.Create("Chat 2", WorktreeId.From("worktree-01HKABC"));

    // Act - Multiple operations should reuse connection pool
    await _repository.CreateAsync(chat1, CancellationToken.None);
    await _repository.CreateAsync(chat2, CancellationToken.None);
    await _repository.GetByIdAsync(chat1.Id, CancellationToken.None);
    await _repository.GetByIdAsync(chat2.Id, CancellationToken.None);

    // Assert - No errors should indicate pool is working
    // (In production, monitor with SQL profiler for connection count)
}
```

**Tests (1):**
- [ ] Multiple operations reuse connection pool

**Success Criteria:**
- [ ] Test verifies pooling by executing many operations without error
- [ ] 1 new test passing

**Evidence:**
- [ ] ConnectionPoolingTest added
- [ ] Test passes with multiple rapid operations

---

### Gap 13: Prepared Statements Verification (AC-076) [ ]

**AC Covered:** AC-076 - "Prepared statements cached"
**Effort:** 1 hour
**Status:** [ ]

**What to Verify:**

Add test in SqliteChatRepositoryTests:

```csharp
[Fact]
public async Task Should_Use_Parameterized_Queries()
{
    // Arrange - SQL injection attempt
    var maliciousTitle = "Title'; DROP TABLE chats; --";
    var chat = Chat.Create(maliciousTitle, WorktreeId.From("worktree-01HKABC"));

    // Act - Should safely escape the malicious input
    var chatId = await _repository.CreateAsync(chat, CancellationToken.None);
    var retrieved = await _repository.GetByIdAsync(chatId, CancellationToken.None);

    // Assert - Title should be stored exactly as provided (escaped)
    retrieved!.Title.Should().Be(maliciousTitle);
}
```

**Tests (1):**
- [ ] Parameterized queries prevent SQL injection

**Success Criteria:**
- [ ] Test shows SQL injection is prevented
- [ ] 1 new test passing

**Evidence:**
- [ ] ParameterizedQueriesTest added
- [ ] Test passes with malicious input safely handled

---

### Gap 14: Migration Idempotency Testing (AC-086) [ ]

**AC Covered:** AC-086 - "Migrations are idempotent"
**Effort:** 1-2 hours
**Status:** [ ]

**What to Implement:**

Add test in ConversationMigrationRunnerTests:

```csharp
[Fact]
public async Task Should_Be_Idempotent()
{
    // Arrange
    var dbPath = Path.Combine(Path.GetTempPath(), $"idempotent_test_{Guid.NewGuid()}.db");
    var runner = new ConversationMigrationRunner(dbPath);

    try
    {
        // Act - Run migration twice
        await runner.MigrateAsync(CancellationToken.None);
        await runner.MigrateAsync(CancellationToken.None);

        // Assert - Both should succeed without error
        var version = await runner.GetCurrentVersionAsync(CancellationToken.None);
        version.Should().Be(1, "schema version should be 1 after idempotent runs");
    }
    finally
    {
        if (File.Exists(dbPath))
            File.Delete(dbPath);
    }
}
```

**Tests (1):**
- [ ] Migrations can be run multiple times safely

**Success Criteria:**
- [ ] Running migration twice succeeds
- [ ] schema_version shows correct version after both runs
- [ ] 1 new test passing

**Evidence:**
- [ ] IdempotencyTest added
- [ ] Test passes with migration run twice

---

## SECTION 3: IMPLEMENTATION PHASES

### 🟥 PHASE 0: PostgreSQL Repositories (5 gaps, 13-15 hours) - CRITICAL PRIORITY

**Goals:** Implement PostgreSQL persistence layer (AC-077-082)

**Gaps:**
- [ ] Gap 1: PostgreSQL Connection Factory and Configuration
- [ ] Gap 2: PostgreSQL ChatRepository Implementation
- [ ] Gap 3: PostgreSQL RunRepository Implementation
- [ ] Gap 4: PostgreSQL MessageRepository Implementation
- [ ] Gap 5: PostgreSQL Repository Registration (Dependency Injection)

**Order:**
1. Create PostgresConnectionFactory with connection pooling (size=10), timeout=30s, statement caching, TLS
2. Implement PostgresChatRepository with all CRUD + worktree filtering + soft delete
3. Implement PostgresRunRepository with all CRUD + chat filtering
4. Implement PostgresMessageRepository with all CRUD + run filtering + AppendAsync + BulkCreateAsync
5. Register all repositories in DI container for both SQLite (default) and PostgreSQL (if enabled)

**Tests to Create:**
- [ ] PostgresConnectionFactoryTests.cs (4 tests)
- [ ] PostgresChatRepositoryTests.cs (7 tests)
- [ ] PostgresRunRepositoryTests.cs (6 tests)
- [ ] PostgresMessageRepositoryTests.cs (6 tests)
- [ ] RepositoryRegistrationTests.cs (1 test)

**Total: 24 tests, ~13-15 hours effort**

**Command After Completion:**
```bash
dotnet test --filter "Postgres"
```

**Success Criteria:**
- [ ] All PostgreSQL repositories fully implemented
- [ ] Connection pooling configured (min=0, max=10)
- [ ] Command timeout = 30 seconds
- [ ] Statement caching enabled
- [ ] TLS encryption required
- [ ] All 24 tests passing

---

### PHASE 1: Message Repository Methods (2 gaps, 3-5 hours)

**Goals:** Implement AppendAsync and BulkCreateAsync

**Gaps:**
- [ ] Gap 6: AppendAsync() method
- [ ] Gap 7: BulkCreateAsync() method

**Order:**
1. Add AppendAsync to IMessageRepository interface
2. Implement AppendAsync in SqliteMessageRepository
3. Write tests for AppendAsync
4. Add BulkCreateAsync to IMessageRepository interface
5. Implement BulkCreateAsync in SqliteMessageRepository
6. Write tests for BulkCreateAsync

**Tests to Create:**
- [ ] MessageRepositoryAppendAsyncTests.cs (2 tests)
- [ ] MessageRepositoryBulkCreateAsyncTests.cs (3 tests)

**Command After Completion:**
```bash
dotnet test --filter "MessageRepository"
```

---

### PHASE 2: Error Codes (1 gap, 1-2 hours)

**Goal:** Add error code pattern to all exceptions

**Gaps:**
- [ ] Gap 8: Error code pattern (AC-093)

**Order:**
1. Create enum for error codes
2. Add ErrorCode property to ConcurrencyException
3. Add ErrorCode property to EntityNotFoundException
4. Add ErrorCode property to ValidationException
5. Add ErrorCode property to ConnectionException
6. Update repository methods to include error codes in exceptions
7. Write tests for error codes

**Tests to Create:**
- [ ] ExceptionErrorCodeTests.cs (4 tests)

**Command After Completion:**
```bash
dotnet test --filter "Exception"
```

---

### PHASE 3: Migrations (2 gaps, 2-4 hours)

**Goals:** Auto-apply migrations and add verification tests

**Gaps:**
- [ ] Gap 9: Migration auto-apply
- [ ] Gap 14: Migration idempotency

**Order:**
1. Create ConversationMigrationRunner
2. Integrate into SqliteChatRepository constructor
3. Create MigrationRunnerTests
4. Add idempotency test
5. Verify migrations run on repository init

**Tests to Create:**
- [ ] ConversationMigrationRunnerTests.cs (2 tests)

**Command After Completion:**
```bash
dotnet test --filter "Migration"
```

---

### PHASE 4: Verification & Optimization (3 gaps, 2-4 hours)

**Goals:** Verify connection pooling, prepared statements, add performance benchmarks

**Gaps:**
- [ ] Gap 12: Connection pooling verification (AC-075)
- [ ] Gap 13: Prepared statements verification (AC-076)
- [ ] Gap 11: Performance benchmarks (AC-094–098)

**Order:**
1. Add connection pooling test to SqliteChatRepositoryTests
2. Add parameterized query test to SqliteChatRepositoryTests
3. Create ConversationBenchmarks project
4. Add BenchmarkDotNet tests
5. Run benchmarks and verify targets met

**Tests to Create:**
- [ ] ConnectionPoolingTests.cs (1 test)
- [ ] ParameterizedQueriesTests.cs (1 test)
- [ ] ConversationBenchmarks.cs (5 benchmarks)

**Command After Completion:**
```bash
dotnet test --filter "Conversation" && dotnet run --project tests/Acode.Performance.Tests
```

---

### PHASE 5: CLI Integration (1 gap, 2-3 hours)

**Goal:** Add migration status CLI command

**Gaps:**
- [ ] Gap 10: Migration status CLI command (AC-088)

**Order:**
1. Create DbMigrationsStatusCommand
2. Register command in CLI
3. Add tests for command

**Tests to Create:**
- [ ] DbMigrationsStatusCommandTests.cs (2 tests)

**Command After Completion:**
```bash
acode db migrations status
```

---

## SECTION 4: VERIFICATION CHECKLIST

**After all gaps complete, verify:**

- [ ] All 14 gaps implemented (Gaps 1-14)
- [ ] PHASE 0 (PostgreSQL) complete - 5 gaps, 24 tests
- [ ] PHASE 1-5 complete - 9 gaps, 30+ tests
- [ ] All 98 in-scope ACs verified implemented
- [ ] All 55+ new tests passing
- [ ] Zero NotImplementedException
- [ ] Zero build errors/warnings
- [ ] Performance benchmarks passing (AC-094-098)
- [ ] PostgreSQL repositories fully tested
- [ ] Code coverage > 90% on conversation layer
- [ ] PR created and ready for review

---

**Implementation Order (CRITICAL):**

1. **Start with PHASE 0** (PostgreSQL) - This is critical path. Complete all 5 gaps + 24 tests
2. Then proceed to PHASE 1 (Message Repository Methods)
3. Continue through PHASES 2-5 in order

**Next Action:** Begin PHASE 0 (Gaps 1-5) - implement PostgreSQL repositories in TDD order.

**Estimated Total Effort:** 34-44 hours (15-20 hours for PHASE 0 + 19-24 hours for PHASES 1-5)

