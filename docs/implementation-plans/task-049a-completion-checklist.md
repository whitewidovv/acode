# Task 049a - Conversation Data Model + Storage Provider - 100% Completion Checklist

## 🎯 OVERVIEW

**Task**: Task 049a - Conversation Data Model + Storage Provider Abstraction
**Priority**: P0 Critical
**Complexity**: 8 Fibonacci points
**Status**: NOT STARTED

**Mission**: Implement canonical data model for conversation history (Chat, Run, Message entities) with repository abstraction and SQLite storage provider for offline-first operation.

**Scope Summary**:
- Domain entities: Chat (aggregate root), Run, Message, ToolCall (value objects)
- Repository interfaces: IChatRepository, IRunRepository, IMessageRepository
- SQLite implementations with Dapper for local storage
- Database migrations with version control
- Comprehensive error handling and security (SQL injection, concurrency, sensitive data)
- 98 Acceptance Criteria to satisfy
- 200+ lines of testing code provided in spec

**Key Business Value**:
- Enables persistent conversation history (saves $17,520/year per developer)
- Offline-first storage for reliable operation
- Foundation for multi-chat, search, sync features

---

## INSTRUCTIONS FOR FRESH CONTEXT AGENT

**Your Mission**: Complete task-049a to 100% specification compliance - all 98 acceptance criteria must be semantically complete with tests.

**How to Use This File**:
1. Read the entire file first to understand scope
2. Work through Priorities 1-7 sequentially
3. For each item:
   - Mark as [🔄] when starting work
   - Implement following TDD (RED-GREEN-REFACTOR)
   - Run tests to verify
   - Mark as [✅] when complete with evidence
4. Update this file after EACH completed item
5. Commit after each logical unit (not batching)
6. When stuck, read the spec: docs/tasks/refined-tasks/Epic 02/task-049a-conversation-data-model-storage-provider.md

**Status Legend**:
- `[ ]` = TODO (not started)
- `[🔄]` = IN PROGRESS (actively working on this)
- `[✅]` = COMPLETE (implemented + tested + verified)

**Critical Rules**:
- NO deferrals - implement everything
- NO placeholders - full implementations only
- NO "TODO" comments in production code
- TESTS FIRST - write tests before implementation (TDD mandatory)
- VERIFY SEMANTICALLY - tests must actually validate the AC, not just pass

**Spec References**:
- Full spec: docs/tasks/refined-tasks/Epic 02/task-049a-conversation-data-model-storage-provider.md (3,566 lines)
- Functional Requirements: Lines 854-1174 (30+ requirements)
- Acceptance Criteria: Lines 1175-1453 (98 criteria)
- Testing Requirements: Lines 1456-2415 (960 lines with complete test code)
- Implementation Prompt: Lines 2417-3566 (1,150 lines with complete entity/repository code)
- Parent Task 049: docs/tasks/refined-tasks/Epic 02/task-049-conversation-history-multi-chat-management.md

**Audit Reference**: Create docs/audits/task-049a-audit-report.md when complete

---

## PRIORITY 1: DOMAIN VALUE OBJECTS (CRITICAL FOUNDATION)

### P1.1: ChatId Value Object (AC-001)

**Status**: [ ]

**Requirements**:
- FR-001: Strongly-typed identifier for Chat entities using ULID format
- AC-001: ChatId is ULID (26 characters, lexicographically sortable)

**Location**: src/Acode.Domain/Conversation/ChatId.cs (CREATE NEW)

**Implementation Details** (from spec lines 2453-2504):
```csharp
public readonly record struct ChatId : IComparable<ChatId>
{
    private readonly string _value;
    public string Value => _value ?? throw new InvalidOperationException("ChatId not initialized");

    private ChatId(string value) { /* validate 26 chars */ }
    public static ChatId NewId() => new(Ulid.NewUlid().ToString());
    public static ChatId From(string value) => new(value);
    public static ChatId Empty => new("00000000000000000000000000");
    public static bool TryParse(string? value, out ChatId chatId) { /* ... */ }
    public int CompareTo(ChatId other) => string.CompareOrdinal(_value, other._value);
    public static implicit operator string(ChatId id) => id.Value;
}
```

**Tests to Write**:
- tests/Acode.Domain.Tests/Conversation/ChatIdTests.cs (CREATE NEW)
  - NewId_GeneratesValid26CharULID
  - From_ValidString_CreatesInstance
  - From_InvalidLength_ThrowsArgumentException
  - Empty_Returns26Zeros
  - TryParse_ValidString_ReturnsTrue
  - TryParse_InvalidString_ReturnsFalse
  - CompareTo_SortsLexicographically
  - ImplicitConversion_ToStringWorks

**How to Test**:
```bash
dotnet test --filter "ChatIdTests" --verbosity normal
# Expect: 8 tests pass
```

**Success Criteria**:
- [ ] ChatId.cs created with all methods
- [ ] ULID generates 26-character strings
- [ ] Empty returns "00000000000000000000000000"
- [ ] TryParse validates length
- [ ] All 8 ChatIdTests passing
- [ ] AC-001 satisfied

**Evidence**:
```
# Fill this in when complete
```

---

### P1.2: RunId Value Object (AC-015)

**Status**: [ ]

**Requirements**:
- FR-002: Strongly-typed identifier for Run entities using ULID format
- AC-015: RunId is ULID (26 characters)

**Location**: src/Acode.Domain/Conversation/RunId.cs (CREATE NEW)

**Implementation Details**:
- Same structure as ChatId
- Separate type for type safety

**Tests to Write**:
- tests/Acode.Domain.Tests/Conversation/RunIdTests.cs (CREATE NEW)
  - NewId_GeneratesValid26CharULID
  - From_ValidString_CreatesInstance
  - From_InvalidLength_ThrowsArgumentException
  - Empty_Returns26Zeros
  - TryParse_ValidString_ReturnsTrue
  - TryParse_InvalidString_ReturnsFalse

**How to Test**:
```bash
dotnet test --filter "RunIdTests" --verbosity normal
# Expect: 6 tests pass
```

**Success Criteria**:
- [ ] RunId.cs created
- [ ] All RunIdTests passing
- [ ] AC-015 satisfied

**Evidence**:
```
# Fill this in when complete
```

---

### P1.3: MessageId Value Object (AC-027)

**Status**: [ ]

**Requirements**:
- FR-003: Strongly-typed identifier for Message entities using ULID format
- AC-027: MessageId is ULID (26 characters)

**Location**: src/Acode.Domain/Conversation/MessageId.cs (CREATE NEW)

**Implementation Details**:
- Same structure as ChatId and RunId
- Separate type for type safety

**Tests to Write**:
- tests/Acode.Domain.Tests/Conversation/MessageIdTests.cs (CREATE NEW)
  - NewId_GeneratesValid26CharULID
  - From_ValidString_CreatesInstance
  - From_InvalidLength_ThrowsArgumentException
  - Empty_Returns26Zeros

**How to Test**:
```bash
dotnet test --filter "MessageIdTests" --verbosity normal
# Expect: 4 tests pass
```

**Success Criteria**:
- [ ] MessageId.cs created
- [ ] All MessageIdTests passing
- [ ] AC-027 satisfied

**Evidence**:
```
# Fill this in when complete
```

---

### P1.4: SyncStatus Enum (AC-013, AC-014)

**Status**: [ ]

**Requirements**:
- FR-004: Track synchronization state between local and remote storage
- AC-013: SyncStatus defaults to Pending
- AC-014: SyncStatus can transition to Synced, Conflict, Failed

**Location**: src/Acode.Domain/Conversation/SyncStatus.cs (CREATE NEW)

**Implementation Details** (from spec lines 3088-3107):
```csharp
public enum SyncStatus
{
    Pending,    // Created locally, not yet synced to remote
    Synced,     // Successfully synced with remote
    Conflict,   // Local and remote versions conflict
    Failed      // Sync failed after retries
}
```

**Tests to Write**:
- tests/Acode.Domain.Tests/Conversation/SyncStatusTests.cs (CREATE NEW)
  - Pending_IsDefinedValue
  - Synced_IsDefinedValue
  - Conflict_IsDefinedValue
  - Failed_IsDefinedValue

**How to Test**:
```bash
dotnet test --filter "SyncStatusTests" --verbosity normal
# Expect: 4 tests pass
```

**Success Criteria**:
- [ ] SyncStatus.cs created with 4 values
- [ ] All SyncStatusTests passing
- [ ] AC-013, AC-014 satisfied

**Evidence**:
```
# Fill this in when complete
```

---

### P1.5: RunStatus Enum (AC-017, AC-018)

**Status**: [ ]

**Requirements**:
- FR-005: Track execution state of Run entities
- AC-017: Status defaults to Running
- AC-018: Status can transition: Running → Completed/Failed/Cancelled

**Location**: src/Acode.Domain/Conversation/RunStatus.cs (CREATE NEW)

**Implementation Details** (from spec lines 3109-3128):
```csharp
public enum RunStatus
{
    Running,      // Run is currently executing
    Completed,    // Run completed successfully
    Failed,       // Run failed with error
    Cancelled     // Run was cancelled by user
}
```

**Tests to Write**:
- tests/Acode.Domain.Tests/Conversation/RunStatusTests.cs (CREATE NEW)
  - Running_IsDefinedValue
  - Completed_IsDefinedValue
  - Failed_IsDefinedValue
  - Cancelled_IsDefinedValue

**How to Test**:
```bash
dotnet test --filter "RunStatusTests" --verbosity normal
# Expect: 4 tests pass
```

**Success Criteria**:
- [ ] RunStatus.cs created with 4 values
- [ ] All RunStatusTests passing
- [ ] AC-017, AC-018 satisfied

**Evidence**:
```
# Fill this in when complete
```

---

### P1.6: ToolCall Value Object (AC-037 through AC-041)

**Status**: [ ]

**Requirements**:
- FR-006: Represent tool invocations within assistant messages
- AC-037: ToolCall.Id is unique string
- AC-038: ToolCall.Name is function name
- AC-039: ToolCall.Arguments is JSON object
- AC-040: ToolCall.Result is optional string
- AC-041: ToolCall.Status tracks execution state

**Location**: src/Acode.Domain/Conversation/ToolCall.cs (CREATE NEW)

**Implementation Details** (from spec lines 3006-3083):
```csharp
public sealed record ToolCall
{
    public string Id { get; init; }
    public string Function { get; init; }
    public string Arguments { get; init; }
    public string? Result { get; private set; }
    public ToolCallStatus Status { get; private set; }

    public ToolCall(string id, string function, string arguments) { /* ... */ }
    public ToolCall WithResult(string result) { /* ... */ }
    public ToolCall WithError(string error) { /* ... */ }
    public T? ParseArguments<T>() where T : class { /* ... */ }
}

public enum ToolCallStatus { Pending, Running, Completed, Failed }
```

**Tests to Write**:
- tests/Acode.Domain.Tests/Conversation/ToolCallTests.cs (CREATE NEW)
  - Constructor_SetsProperties
  - Constructor_ThrowsOnNullId
  - Constructor_ThrowsOnNullFunction
  - Constructor_ThrowsOnNullArguments
  - WithResult_SetsResultAndCompletesStatus
  - WithError_SetsResultAndFailsStatus
  - ParseArguments_DeserializesJson
  - Status_DefaultsPending

**How to Test**:
```bash
dotnet test --filter "ToolCallTests" --verbosity normal
# Expect: 8 tests pass
```

**Success Criteria**:
- [ ] ToolCall.cs created with immutable record
- [ ] ToolCallStatus enum created
- [ ] All ToolCallTests passing
- [ ] AC-037, AC-038, AC-039, AC-040, AC-041 satisfied

**Evidence**:
```
# Fill this in when complete
```

---

## PRIORITY 2: DOMAIN ENTITIES (AGGREGATE ROOT AND CHILDREN)

### P2.1: Chat Entity - Core Properties (AC-001 through AC-014)

**Status**: [ ]

**Requirements**:
- FR-007: Chat aggregate root representing conversation thread
- AC-001: ChatId is ULID
- AC-002: Title stored (max 500 characters, required)
- AC-003: CreatedAt timestamp is UTC and immutable
- AC-004: UpdatedAt timestamp updates on any modification
- AC-005: Tags stored as JSON array
- AC-006: Tags can be added without duplicates
- AC-007: Tags can be removed
- AC-008: WorktreeId is nullable foreign key
- AC-009: IsDeleted defaults to false
- AC-010: Soft delete sets IsDeleted=true and DeletedAt
- AC-011: Restore clears IsDeleted and DeletedAt
- AC-012: Version starts at 1 and increments on update
- AC-013: SyncStatus defaults to Pending
- AC-014: SyncStatus can transition to Synced, Conflict, Failed

**Location**: src/Acode.Domain/Conversation/Chat.cs (CREATE NEW)

**Implementation Details** (from spec lines 2507-2726):
```csharp
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

    public static Chat Create(string title, WorktreeId? worktreeId = null) { /* ... */ }
    public static Chat Reconstitute(...) { /* ... */ }
    public void UpdateTitle(string newTitle) { /* ... */ }
    public void AddTag(string tag) { /* ... */ }
    public bool RemoveTag(string tag) { /* ... */ }
    public void BindToWorktree(WorktreeId worktreeId) { /* ... */ }
    public void Delete() { /* ... */ }
    public void Restore() { /* ... */ }
    public void MarkSynced() { /* ... */ }
    public void MarkConflict() { /* ... */ }
    internal void IncrementVersion() { /* ... */ }
    internal void AddRun(Run run) { /* ... */ }
}
```

**Tests to Write** (from spec lines 1458-1598):
- tests/Acode.Domain.Tests/Conversation/ChatTests.cs (CREATE NEW)
  - Should_Create_With_Valid_Id (AC-001, AC-003, AC-009, AC-012, AC-013)
  - Should_Reject_Invalid_Title (AC-002, null/empty/whitespace)
  - Should_Validate_Title_Length (AC-002, 501 chars)
  - Should_Track_Version_On_Update (AC-004, AC-012)
  - Should_Soft_Delete_Chat (AC-010)
  - Should_Restore_Soft_Deleted_Chat (AC-011)
  - Should_Add_Tags (AC-005, AC-006)
  - Should_Prevent_Duplicate_Tags (AC-006)
  - Should_Remove_Tags (AC-007)
  - Should_Bind_To_Worktree (AC-008)
  - Should_Update_Timestamps_On_Modification (AC-004)

**How to Test**:
```bash
dotnet test --filter "ChatTests" --verbosity normal
# Expect: 11+ tests pass
```

**Success Criteria**:
- [ ] Chat.cs created with all methods
- [ ] Title validation (required, max 500 chars)
- [ ] Version increments on updates
- [ ] Soft delete/restore works
- [ ] Tags add/remove without duplicates
- [ ] All ChatTests passing
- [ ] AC-001 through AC-014 satisfied

**Evidence**:
```
# Fill this in when complete
```

---

### P2.2: Run Entity - Core Properties (AC-015 through AC-026)

**Status**: [ ]

**Requirements**:
- FR-008: Run entity representing single request/response cycle
- AC-015: RunId is ULID
- AC-016: ChatId is required foreign key
- AC-017: Status defaults to Running
- AC-018: Status transitions: Running → Completed/Failed/Cancelled
- AC-019: StartedAt set on creation (UTC)
- AC-020: CompletedAt is nullable, set on completion
- AC-021: TokensIn tracks input tokens (default 0)
- AC-022: TokensOut tracks output tokens (default 0)
- AC-023: SequenceNumber auto-increments per Chat
- AC-024: ErrorMessage is set on failure
- AC-025: ModelId stores inference provider name
- AC-026: SyncStatus tracks sync state

**Location**: src/Acode.Domain/Conversation/Run.cs (CREATE NEW)

**Implementation Details** (from spec lines 2729-2878):
```csharp
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

    public static Run Create(ChatId chatId, string modelId, int sequenceNumber = 0) { /* ... */ }
    public static Run Reconstitute(...) { /* ... */ }
    public void Complete(int tokensIn, int tokensOut) { /* ... */ }
    public void Fail(string errorMessage) { /* ... */ }
    public void Cancel() { /* ... */ }
    internal void AddMessage(Message message) { /* ... */ }
    public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;
    public int TotalTokens => TokensIn + TokensOut;
}
```

**Tests to Write** (from spec lines 1601-1687):
- tests/Acode.Domain.Tests/Conversation/RunTests.cs (CREATE NEW)
  - Should_Require_ChatId (AC-016)
  - Should_Track_Status (AC-017, AC-019)
  - Should_Complete_Run_Successfully (AC-018, AC-020, AC-021, AC-022)
  - Should_Fail_Run_With_Error (AC-018, AC-024)
  - Should_Cancel_Run (AC-018)
  - Should_Calculate_Duration (AC-020)
  - Should_Track_Token_Usage (AC-021, AC-022)
  - Should_Auto_Increment_Sequence_Number (AC-023)
  - Should_Store_ModelId (AC-025)
  - Should_Track_SyncStatus (AC-026)

**How to Test**:
```bash
dotnet test --filter "RunTests" --verbosity normal
# Expect: 10+ tests pass
```

**Success Criteria**:
- [ ] Run.cs created with all methods
- [ ] ChatId validation (not empty)
- [ ] Status transitions work
- [ ] Token tracking works
- [ ] Duration calculation works
- [ ] All RunTests passing
- [ ] AC-015 through AC-026 satisfied

**Evidence**:
```
# Fill this in when complete
```

---

### P2.3: Message Entity - Core Properties (AC-027 through AC-036)

**Status**: [ ]

**Requirements**:
- FR-009: Message entity representing individual exchange
- AC-027: MessageId is ULID
- AC-028: RunId is required foreign key
- AC-029: Role is enum (User, Assistant, System, Tool)
- AC-030: Content stored as text (max 100KB)
- AC-031: Content is immutable after creation
- AC-032: ToolCalls stored as JSON array
- AC-033: ToolCalls can be added to assistant messages
- AC-034: CreatedAt is UTC and immutable
- AC-035: SequenceNumber auto-increments per Run
- AC-036: SyncStatus tracks sync state

**Location**: src/Acode.Domain/Conversation/Message.cs (CREATE NEW)

**Implementation Details** (from spec lines 2881-3003):
```csharp
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

    public static Message Create(RunId runId, string role, string content, int sequenceNumber = 0) { /* ... */ }
    public static Message Reconstitute(...) { /* ... */ }
    public void AddToolCalls(IEnumerable<ToolCall> toolCalls) { /* ... */ }
    public string? GetToolCallsJson() { /* ... */ }
}
```

**Tests to Write** (from spec lines 1690-1776):
- tests/Acode.Domain.Tests/Conversation/MessageTests.cs (CREATE NEW)
  - Should_Require_RunId (AC-028)
  - Should_Accept_Valid_Roles (AC-029, test all 4 roles)
  - Should_Reject_Invalid_Role (AC-029)
  - Should_Serialize_ToolCalls (AC-032, AC-033)
  - Should_Store_Large_Content (AC-030, test 50KB)
  - Should_Validate_Content_Length (AC-030, test 101KB fails)
  - Should_Make_Content_Immutable (AC-031)
  - Should_Auto_Increment_Sequence_Number (AC-035)
  - Should_Track_SyncStatus (AC-036)

**How to Test**:
```bash
dotnet test --filter "MessageTests" --verbosity normal
# Expect: 9+ tests pass
```

**Success Criteria**:
- [ ] Message.cs created with all methods
- [ ] RunId validation (not empty)
- [ ] Role validation (user/assistant/system/tool)
- [ ] Content validation (max 100KB)
- [ ] ToolCalls serialization works
- [ ] All MessageTests passing
- [ ] AC-027 through AC-036 satisfied

**Evidence**:
```
# Fill this in when complete
```

---

## PRIORITY 3: REPOSITORY INTERFACES (APPLICATION LAYER ABSTRACTIONS)

### P3.1: IChatRepository Interface (AC-042 through AC-060)

**Status**: [ ]

**Requirements**:
- FR-010: Repository abstraction for Chat persistence
- AC-042: IChatRepository defines all CRUD operations
- AC-043: IRunRepository defines all CRUD operations
- AC-044: IMessageRepository defines all CRUD operations
- AC-045: All methods accept CancellationToken
- AC-046: All methods are async (return Task)
- AC-047: Create methods return entity ID
- AC-048: Get methods return nullable entity
- AC-049: List methods return PagedResult
- AC-050 through AC-060: Chat-specific operations

**Location**: src/Acode.Application/Conversation/Persistence/IChatRepository.cs (CREATE NEW)

**Implementation Details** (from spec lines 3131-3186):
```csharp
public interface IChatRepository
{
    Task<ChatId> CreateAsync(Chat chat, CancellationToken ct);
    Task<Chat?> GetByIdAsync(ChatId id, bool includeRuns = false, CancellationToken ct = default);
    Task UpdateAsync(Chat chat, CancellationToken ct);
    Task SoftDeleteAsync(ChatId id, CancellationToken ct);
    Task<PagedResult<Chat>> ListAsync(ChatFilter filter, CancellationToken ct);
    Task<IReadOnlyList<Chat>> GetByWorktreeAsync(WorktreeId worktreeId, CancellationToken ct);
    Task<int> PurgeDeletedAsync(DateTimeOffset before, CancellationToken ct);
}

public record ChatFilter
{
    public WorktreeId? WorktreeId { get; init; }
    public DateTimeOffset? CreatedAfter { get; init; }
    public DateTimeOffset? CreatedBefore { get; init; }
    public bool IncludeDeleted { get; init; } = false;
    public int Page { get; init; } = 0;
    public int PageSize { get; init; } = 50;
}

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages - 1;
    public bool HasPreviousPage => Page > 0;
}
```

**Tests to Write**:
- Interface definition tests (verified via implementation tests in P4)

**Success Criteria**:
- [ ] IChatRepository.cs created with all methods
- [ ] ChatFilter record created
- [ ] PagedResult record created
- [ ] All methods return Task (async)
- [ ] All methods accept CancellationToken
- [ ] AC-042, AC-045, AC-046, AC-047, AC-048, AC-049 satisfied

**Evidence**:
```
# Fill this in when complete
```

---

### P3.2: IRunRepository Interface (AC-061 through AC-065)

**Status**: [ ]

**Requirements**:
- FR-011: Repository abstraction for Run persistence
- AC-061: CreateAsync creates Run with ChatId
- AC-062: GetByIdAsync retrieves Run
- AC-063: UpdateAsync persists status changes
- AC-064: ListByChatAsync returns Runs for Chat
- AC-065: GetLatestByChatAsync returns most recent Run

**Location**: src/Acode.Application/Conversation/Persistence/IRunRepository.cs (CREATE NEW)

**Implementation Details**:
```csharp
public interface IRunRepository
{
    Task<RunId> CreateAsync(Run run, CancellationToken ct);
    Task<Run?> GetByIdAsync(RunId id, CancellationToken ct);
    Task UpdateAsync(Run run, CancellationToken ct);
    Task<IReadOnlyList<Run>> ListByChatAsync(ChatId chatId, CancellationToken ct);
    Task<Run?> GetLatestByChatAsync(ChatId chatId, CancellationToken ct);
}
```

**Tests to Write**:
- Interface definition tests (verified via implementation tests in P4)

**Success Criteria**:
- [ ] IRunRepository.cs created with all methods
- [ ] All methods return Task (async)
- [ ] All methods accept CancellationToken
- [ ] AC-061 through AC-065 method signatures defined

**Evidence**:
```
# Fill this in when complete
```

---

### P3.3: IMessageRepository Interface (AC-066 through AC-070)

**Status**: [ ]

**Requirements**:
- FR-012: Repository abstraction for Message persistence
- AC-066: CreateAsync creates Message with RunId
- AC-067: GetByIdAsync retrieves Message
- AC-068: ListByRunAsync returns Messages for Run
- AC-069: AppendAsync adds Message to Run
- AC-070: BulkCreateAsync inserts multiple Messages efficiently

**Location**: src/Acode.Application/Conversation/Persistence/IMessageRepository.cs (CREATE NEW)

**Implementation Details**:
```csharp
public interface IMessageRepository
{
    Task<MessageId> CreateAsync(Message message, CancellationToken ct);
    Task<Message?> GetByIdAsync(MessageId id, CancellationToken ct);
    Task<IReadOnlyList<Message>> ListByRunAsync(RunId runId, CancellationToken ct);
    Task AppendAsync(Message message, CancellationToken ct);
    Task BulkCreateAsync(IEnumerable<Message> messages, CancellationToken ct);
}
```

**Tests to Write**:
- Interface definition tests (verified via implementation tests in P4)

**Success Criteria**:
- [ ] IMessageRepository.cs created with all methods
- [ ] All methods return Task (async)
- [ ] All methods accept CancellationToken
- [ ] AC-066 through AC-070 method signatures defined

**Evidence**:
```
# Fill this in when complete
```

---

## PRIORITY 4: SQLITE REPOSITORY IMPLEMENTATIONS (INFRASTRUCTURE LAYER)

### P4.1: SqliteChatRepository - Core CRUD (AC-050 through AC-054, AC-071 through AC-076)

**Status**: [ ]

**Requirements**:
- FR-013: SQLite implementation of IChatRepository using Dapper
- AC-050: CreateAsync creates new Chat
- AC-051: GetByIdAsync retrieves Chat by ID
- AC-052: GetByIdAsync returns null for non-existent
- AC-053: UpdateAsync persists changes
- AC-054: UpdateAsync throws ConcurrencyException on version conflict
- AC-071 through AC-076: SQLite-specific features (WAL, transactions, pooling)

**Location**: src/Acode.Infrastructure/Persistence/Conversation/SqliteChatRepository.cs (CREATE NEW)

**Implementation Details** (from spec lines 3189-3447):
```csharp
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
        // Use Dapper with parameterized query
        // Serialize tags as JSON
        // Return chat.Id
    }

    public async Task<Chat?> GetByIdAsync(ChatId id, bool includeRuns = false, CancellationToken ct = default)
    {
        // Query chats table
        // Map row to Chat entity using Reconstitute
        // Optionally eager-load Runs
    }

    public async Task UpdateAsync(Chat chat, CancellationToken ct)
    {
        // WHERE id = @Id AND version = @ExpectedVersion
        // Throw ConcurrencyException if rowsAffected == 0
    }

    // ... other methods from spec
}
```

**Tests to Write** (from spec lines 1779-1881):
- tests/Acode.Infrastructure.Tests/Persistence/Conversation/SqliteChatRepositoryTests.cs (CREATE NEW)
  - Should_Create_Chat (AC-050, AC-094: <10ms)
  - Should_Get_By_Id (AC-051, AC-095: <5ms)
  - Should_Return_Null_For_Nonexistent_Chat (AC-052)
  - Should_Update_Chat (AC-053, AC-097: <10ms)
  - Should_Throw_On_Version_Conflict (AC-054, AC-090)
  - Should_Soft_Delete_Chat (AC-055)
  - Should_List_With_Pagination (AC-057, AC-096: <50ms for 100 items)
  - Should_Filter_By_Worktree (AC-058)
  - Should_Filter_By_Date_Range (AC-059)
  - Should_Query_By_Worktree (AC-060)
  - Should_Handle_Concurrent_Access (AC-072: WAL mode)
  - Should_Use_Transactions (AC-074)

**How to Test**:
```bash
dotnet test --filter "SqliteChatRepositoryTests" --verbosity normal
# Expect: 12+ tests pass
```

**Success Criteria**:
- [ ] SqliteChatRepository.cs created
- [ ] All IChatRepository methods implemented with Dapper
- [ ] Parameterized queries (SQL injection protection)
- [ ] Optimistic concurrency with version check
- [ ] WAL mode enabled in connection
- [ ] All SqliteChatRepositoryTests passing
- [ ] Performance targets met (AC-094 through AC-098)
- [ ] AC-050 through AC-060, AC-071 through AC-076 satisfied

**Evidence**:
```
# Fill this in when complete
```

---

### P4.2: SqliteRunRepository - Core CRUD (AC-061 through AC-065)

**Status**: [ ]

**Requirements**:
- FR-014: SQLite implementation of IRunRepository using Dapper
- AC-061: CreateAsync creates Run with ChatId
- AC-062: GetByIdAsync retrieves Run
- AC-063: UpdateAsync persists status changes
- AC-064: ListByChatAsync returns Runs for Chat
- AC-065: GetLatestByChatAsync returns most recent Run

**Location**: src/Acode.Infrastructure/Persistence/Conversation/SqliteRunRepository.cs (CREATE NEW)

**Implementation Details**:
```csharp
public sealed class SqliteRunRepository : IRunRepository
{
    private readonly string _connectionString;

    public async Task<RunId> CreateAsync(Run run, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO runs (id, chat_id, model_id, status, started_at, completed_at,
                             tokens_in, tokens_out, sequence_number, error_message, sync_status)
            VALUES (@Id, @ChatId, @ModelId, @Status, @StartedAt, @CompletedAt,
                   @TokensIn, @TokensOut, @SequenceNumber, @ErrorMessage, @SyncStatus)";
        // Dapper insert
    }

    public async Task<Run?> GetByIdAsync(RunId id, CancellationToken ct)
    {
        const string sql = "SELECT * FROM runs WHERE id = @Id";
        // Map to Run entity
    }

    public async Task<IReadOnlyList<Run>> ListByChatAsync(ChatId chatId, CancellationToken ct)
    {
        const string sql = @"
            SELECT * FROM runs
            WHERE chat_id = @ChatId
            ORDER BY sequence_number";
        // Return ordered list
    }

    public async Task<Run?> GetLatestByChatAsync(ChatId chatId, CancellationToken ct)
    {
        const string sql = @"
            SELECT * FROM runs
            WHERE chat_id = @ChatId
            ORDER BY sequence_number DESC
            LIMIT 1";
        // Return most recent
    }
}
```

**Tests to Write**:
- tests/Acode.Infrastructure.Tests/Persistence/Conversation/SqliteRunRepositoryTests.cs (CREATE NEW)
  - Should_Create_Run (AC-061)
  - Should_Get_By_Id (AC-062)
  - Should_Update_Status (AC-063)
  - Should_List_By_Chat_Ordered_By_Sequence (AC-064)
  - Should_Get_Latest_By_Chat (AC-065)
  - Should_Track_Token_Usage
  - Should_Store_Error_Message
  - Should_Calculate_Duration

**How to Test**:
```bash
dotnet test --filter "SqliteRunRepositoryTests" --verbosity normal
# Expect: 8+ tests pass
```

**Success Criteria**:
- [ ] SqliteRunRepository.cs created
- [ ] All IRunRepository methods implemented
- [ ] Sequence number ordering works
- [ ] Latest run query works
- [ ] All SqliteRunRepositoryTests passing
- [ ] AC-061 through AC-065 satisfied

**Evidence**:
```
# Fill this in when complete
```

---

### P4.3: SqliteMessageRepository - Core CRUD (AC-066 through AC-070)

**Status**: [ ]

**Requirements**:
- FR-015: SQLite implementation of IMessageRepository using Dapper
- AC-066: CreateAsync creates Message with RunId
- AC-067: GetByIdAsync retrieves Message
- AC-068: ListByRunAsync returns Messages for Run
- AC-069: AppendAsync adds Message to Run
- AC-070: BulkCreateAsync inserts multiple Messages efficiently

**Location**: src/Acode.Infrastructure/Persistence/Conversation/SqliteMessageRepository.cs (CREATE NEW)

**Implementation Details**:
```csharp
public sealed class SqliteMessageRepository : IMessageRepository
{
    private readonly string _connectionString;

    public async Task<MessageId> CreateAsync(Message message, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO messages (id, run_id, role, content, tool_calls, created_at, sequence_number, sync_status)
            VALUES (@Id, @RunId, @Role, @Content, @ToolCalls, @CreatedAt, @SequenceNumber, @SyncStatus)";
        // Serialize ToolCalls as JSON
    }

    public async Task<IReadOnlyList<Message>> ListByRunAsync(RunId runId, CancellationToken ct)
    {
        const string sql = @"
            SELECT * FROM messages
            WHERE run_id = @RunId
            ORDER BY sequence_number";
        // Deserialize ToolCalls from JSON
    }

    public async Task BulkCreateAsync(IEnumerable<Message> messages, CancellationToken ct)
    {
        // Use transaction
        // Insert all messages in single batch
        // Much faster than individual inserts
    }
}
```

**Tests to Write**:
- tests/Acode.Infrastructure.Tests/Persistence/Conversation/SqliteMessageRepositoryTests.cs (CREATE NEW)
  - Should_Create_Message (AC-066)
  - Should_Get_By_Id (AC-067)
  - Should_List_By_Run_Ordered_By_Sequence (AC-068)
  - Should_Append_Message (AC-069)
  - Should_Bulk_Create_Messages (AC-070)
  - Should_Serialize_ToolCalls
  - Should_Deserialize_ToolCalls
  - Should_Store_Large_Content (50KB test)

**How to Test**:
```bash
dotnet test --filter "SqliteMessageRepositoryTests" --verbosity normal
# Expect: 8+ tests pass
```

**Success Criteria**:
- [ ] SqliteMessageRepository.cs created
- [ ] All IMessageRepository methods implemented
- [ ] ToolCalls JSON serialization/deserialization works
- [ ] BulkCreate uses transaction for efficiency
- [ ] All SqliteMessageRepositoryTests passing
- [ ] AC-066 through AC-070 satisfied

**Evidence**:
```
# Fill this in when complete
```

---

## PRIORITY 5: DATABASE MIGRATIONS AND SCHEMA

### P5.1: Initial Schema Migration (AC-083 through AC-088)

**Status**: [ ]

**Requirements**:
- FR-016: Schema migrations with version control and auto-apply
- AC-083: Migrations auto-apply on application start
- AC-084: Migration version tracked in schema_version table
- AC-085: Each migration has up and down scripts
- AC-086: Migrations are idempotent
- AC-087: Rollback reverts last migration
- AC-088: Migration status command shows applied/pending

**Location**:
- src/Acode.Infrastructure/Persistence/Conversation/Migrations/001_InitialSchema.sql (CREATE NEW)
- src/Acode.Infrastructure/Persistence/Conversation/Migrations/MigrationRunner.cs (CREATE NEW)

**Implementation Details** (from spec lines 3450-3522):
```sql
-- 001_InitialSchema.sql
CREATE TABLE IF NOT EXISTS schema_version (
    version INTEGER PRIMARY KEY,
    applied_at TEXT NOT NULL,
    description TEXT NOT NULL
);

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

-- Indexes
CREATE INDEX IF NOT EXISTS idx_chats_worktree ON chats(worktree_id) WHERE worktree_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_chats_updated ON chats(updated_at DESC);
CREATE INDEX IF NOT EXISTS idx_chats_deleted ON chats(is_deleted, deleted_at);
CREATE INDEX IF NOT EXISTS idx_runs_chat ON runs(chat_id);
CREATE INDEX IF NOT EXISTS idx_runs_status ON runs(status);
CREATE INDEX IF NOT EXISTS idx_messages_run ON messages(run_id);
CREATE INDEX IF NOT EXISTS idx_messages_role ON messages(role);

-- Enable WAL mode
PRAGMA journal_mode=WAL;
PRAGMA synchronous=NORMAL;
PRAGMA busy_timeout=5000;

-- Record migration
INSERT INTO schema_version (version, applied_at, description)
VALUES (1, datetime('now'), 'Initial schema with chats, runs, messages');
```

```csharp
// MigrationRunner.cs
public sealed class MigrationRunner
{
    private readonly string _dbPath;

    public async Task MigrateAsync(CancellationToken ct)
    {
        // Check current version
        // Find pending migrations
        // Apply each migration in order
        // Update schema_version table
    }

    public async Task<int> GetCurrentVersionAsync(CancellationToken ct)
    {
        // Query schema_version table
        // Return highest version number
    }

    public async Task RollbackAsync(CancellationToken ct)
    {
        // Find last migration
        // Execute down script
        // Remove from schema_version table
    }
}
```

**Tests to Write**:
- tests/Acode.Infrastructure.Tests/Persistence/Conversation/MigrationRunnerTests.cs (CREATE NEW)
  - Should_Apply_Initial_Migration (AC-083, AC-084)
  - Should_Track_Migration_Version (AC-084)
  - Should_Be_Idempotent (AC-086, run twice, verify no error)
  - Should_Rollback_Migration (AC-087)
  - Should_Show_Migration_Status (AC-088)
  - Should_Enable_WAL_Mode (AC-072)
  - Should_Set_Busy_Timeout (AC-073)

**How to Test**:
```bash
dotnet test --filter "MigrationRunnerTests" --verbosity normal
# Expect: 7+ tests pass
```

**Success Criteria**:
- [ ] 001_InitialSchema.sql created with all tables
- [ ] All indexes created
- [ ] Foreign keys with CASCADE DELETE
- [ ] WAL mode enabled
- [ ] MigrationRunner.cs created
- [ ] All MigrationRunnerTests passing
- [ ] AC-083 through AC-088 satisfied

**Evidence**:
```
# Fill this in when complete
```

---

## PRIORITY 6: ERROR HANDLING AND SECURITY

### P6.1: Custom Exceptions (AC-089 through AC-093)

**Status**: [ ]

**Requirements**:
- FR-017: Domain-specific exceptions with error codes
- AC-089: EntityNotFoundException thrown for missing entities
- AC-090: ConcurrencyException thrown on version conflict
- AC-091: ValidationException thrown for invalid data
- AC-092: ConnectionException thrown for database errors
- AC-093: Error codes follow CONV-DATA-xxx pattern

**Locations**:
- src/Acode.Domain/Conversation/Exceptions/EntityNotFoundException.cs (CREATE NEW)
- src/Acode.Domain/Conversation/Exceptions/ConcurrencyException.cs (CREATE NEW)
- src/Acode.Domain/Conversation/Exceptions/ValidationException.cs (CREATE NEW)
- src/Acode.Infrastructure/Conversation/Exceptions/ConnectionException.cs (CREATE NEW)

**Implementation Details** (from spec lines 3524-3535):
```csharp
public class EntityNotFoundException : Exception
{
    public string ErrorCode { get; }
    public EntityNotFoundException(string message, string errorCode = "ACODE-CONV-DATA-001")
        : base(message)
    {
        ErrorCode = errorCode;
    }
}

public class ConcurrencyException : Exception
{
    public string ErrorCode { get; }
    public ConcurrencyException(string message, string errorCode = "ACODE-CONV-DATA-006")
        : base(message)
    {
        ErrorCode = errorCode;
    }
}

// Similar for ValidationException (ACODE-CONV-DATA-007)
// Similar for ConnectionException (ACODE-CONV-DATA-006)
```

**Tests to Write**:
- tests/Acode.Domain.Tests/Conversation/Exceptions/ExceptionTests.cs (CREATE NEW)
  - EntityNotFoundException_Has_Correct_ErrorCode (AC-089, AC-093)
  - ConcurrencyException_Has_Correct_ErrorCode (AC-090, AC-093)
  - ValidationException_Has_Correct_ErrorCode (AC-091, AC-093)
  - ConnectionException_Has_Correct_ErrorCode (AC-092, AC-093)

**How to Test**:
```bash
dotnet test --filter "ExceptionTests" --verbosity normal
# Expect: 4 tests pass
```

**Success Criteria**:
- [ ] All 4 exception classes created
- [ ] Error codes follow ACODE-CONV-DATA-xxx pattern
- [ ] All ExceptionTests passing
- [ ] AC-089 through AC-093 satisfied

**Evidence**:
```
# Fill this in when complete
```

---

### P6.2: SQL Injection Protection (Security Threat 1)

**Status**: [ ]

**Requirements**:
- Threat 1 from spec: SQL Injection via Repository Methods
- Mitigation: Use parameterized queries exclusively

**Verification** (from spec lines 363-427):
- All repository methods use parameterized queries (Dapper with @parameters)
- No string concatenation in SQL
- Test with malicious input: `'; DROP TABLE chats; --`

**Tests to Write**:
- tests/Acode.Infrastructure.Tests/Persistence/Conversation/SqlInjectionTests.cs (CREATE NEW)
  - Should_Reject_SQL_Injection_In_Title
  - Should_Reject_SQL_Injection_In_Tag
  - Should_Reject_SQL_Injection_In_Content
  - Should_Parameterize_All_Queries

**How to Test**:
```bash
dotnet test --filter "SqlInjectionTests" --verbosity normal
# Expect: 4 tests pass, no SQL executed
```

**Success Criteria**:
- [ ] All queries use Dapper parameters
- [ ] No string concatenation in SQL
- [ ] Malicious input safely handled
- [ ] All SqlInjectionTests passing
- [ ] Security Threat 1 mitigated

**Evidence**:
```
# Fill this in when complete
```

---

### P6.3: Optimistic Concurrency (Security Threat 3)

**Status**: [ ]

**Requirements**:
- Threat 3 from spec: Race Condition in Optimistic Concurrency
- Mitigation: Version field check in UpdateAsync
- AC-054: UpdateAsync throws ConcurrencyException on version conflict
- AC-012: Version starts at 1 and increments on update

**Implementation Details** (from spec lines 501-590):
```csharp
public async Task UpdateAsync(Chat chat, CancellationToken ct)
{
    const string sql = @"
        UPDATE chats
        SET title = @Title, ..., version = version + 1
        WHERE id = @Id AND version = @ExpectedVersion";

    var rowsAffected = await _db.ExecuteAsync(sql, new { ExpectedVersion = chat.Version });

    if (rowsAffected == 0)
        throw new ConcurrencyException($"Chat {chat.Id} was modified by another process.");
}
```

**Tests to Write**:
- Already covered in SqliteChatRepositoryTests.Should_Throw_On_Version_Conflict
- tests/Acode.Infrastructure.Tests/Persistence/Conversation/ConcurrencyTests.cs (CREATE NEW)
  - Should_Detect_Concurrent_Chat_Updates
  - Should_Detect_Concurrent_Run_Updates
  - Should_Increment_Version_On_Successful_Update
  - Should_Preserve_Data_On_Conflict

**How to Test**:
```bash
dotnet test --filter "ConcurrencyTests" --verbosity normal
# Expect: 4 tests pass
```

**Success Criteria**:
- [ ] Version check in WHERE clause
- [ ] ConcurrencyException thrown on conflict
- [ ] Version increments on success
- [ ] All ConcurrencyTests passing
- [ ] AC-012, AC-054 satisfied
- [ ] Security Threat 3 mitigated

**Evidence**:
```
# Fill this in when complete
```

---

### P6.4: Sensitive Data Redaction (Security Threat 5)

**Status**: [ ]

**Requirements**:
- Threat 5 from spec: Sensitive Data in Chat History
- Mitigation: Detect and redact API keys, passwords before persistence
- Warn user if sensitive data detected

**Implementation Details** (from spec lines 693-780):
```csharp
public sealed class SensitiveDataRedactionRepository : IMessageRepository
{
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

        foreach (var pattern in SensitivePatterns)
        {
            if (pattern.IsMatch(content))
            {
                redactedContent = pattern.Replace(redactedContent, "[REDACTED]");
                _logger.LogWarning("Sensitive data detected and redacted.");
            }
        }

        // Store redacted content
    }
}
```

**Tests to Write**:
- tests/Acode.Infrastructure.Tests/Persistence/Conversation/SensitiveDataTests.cs (CREATE NEW)
  - Should_Redact_OpenAI_Keys
  - Should_Redact_GitHub_Tokens
  - Should_Redact_AWS_Keys
  - Should_Redact_Passwords
  - Should_Redact_Bearer_Tokens
  - Should_Log_Warning_On_Detection
  - Should_Store_Redacted_Content

**How to Test**:
```bash
dotnet test --filter "SensitiveDataTests" --verbosity normal
# Expect: 7 tests pass
```

**Success Criteria**:
- [ ] All 5 sensitive patterns detected
- [ ] Content redacted before storage
- [ ] Warning logged
- [ ] All SensitiveDataTests passing
- [ ] Security Threat 5 mitigated

**Evidence**:
```
# Fill this in when complete
```

---

### P6.5: Database Corruption Detection (Security Threat 4)

**Status**: [ ]

**Requirements**:
- Threat 4 from spec: Database File Corruption
- Mitigation: WAL mode, backup on startup, integrity check

**Implementation Details** (from spec lines 592-691):
```csharp
public sealed class CorruptionDetectionRepository
{
    public async Task InitializeAsync()
    {
        // Enable WAL mode
        await conn.ExecuteAsync("PRAGMA journal_mode=WAL");

        // Check integrity
        var integrityCheck = await conn.QueryFirstAsync<string>("PRAGMA integrity_check");

        if (integrityCheck != "ok")
        {
            _logger.LogError("Database corruption detected: {Result}", integrityCheck);
            await RecoverFromBackupAsync();
        }
        else
        {
            await CreateBackupIfNeededAsync();
        }
    }

    private async Task CreateBackupIfNeededAsync()
    {
        // Create backup every 24 hours
        // Use SQLite backup API (atomic)
    }
}
```

**Tests to Write**:
- tests/Acode.Infrastructure.Tests/Persistence/Conversation/CorruptionDetectionTests.cs (CREATE NEW)
  - Should_Enable_WAL_Mode
  - Should_Check_Integrity_On_Startup
  - Should_Create_Backup_Every_24_Hours
  - Should_Detect_Corruption
  - Should_Recover_From_Backup

**How to Test**:
```bash
dotnet test --filter "CorruptionDetectionTests" --verbosity normal
# Expect: 5 tests pass
```

**Success Criteria**:
- [ ] WAL mode enabled
- [ ] Integrity check on startup
- [ ] Backup created if needed
- [ ] Corruption detection works
- [ ] All CorruptionDetectionTests passing
- [ ] Security Threat 4 mitigated

**Evidence**:
```
# Fill this in when complete
```

---

## PRIORITY 7: INTEGRATION AND E2E TESTS

### P7.1: Integration Tests - SqliteChatRepository (from spec lines 1779-1881)

**Status**: [ ]

**Requirements**:
- Verify repository works with real SQLite database
- Test full CRUD lifecycle
- Test pagination and filtering
- Test concurrent access

**Tests to Write** (from spec):
- tests/Acode.Integration.Tests/Persistence/Conversation/SqliteRepositoryTests.cs (CREATE NEW)
  - Should_Create_Chat (AC-050, AC-094)
  - Should_Query_By_Worktree (AC-058, AC-060)
  - Should_Handle_Concurrent_Access (AC-072, AC-074)
  - Should_List_With_Pagination (AC-057, AC-096)
  - Should_Soft_Delete_And_Restore (AC-055, AC-010, AC-011)

**How to Test**:
```bash
dotnet test --filter "SqliteRepositoryTests" --verbosity normal
# Expect: 5+ tests pass with real database
```

**Success Criteria**:
- [ ] In-memory SQLite database for tests
- [ ] IAsyncLifetime for setup/teardown
- [ ] All integration tests passing
- [ ] AC-050, AC-055, AC-057, AC-058, AC-060, AC-072, AC-074, AC-094, AC-096 verified

**Evidence**:
```
# Fill this in when complete
```

---

### P7.2: E2E Tests - Full Conversation Hierarchy (from spec lines 1884-1961)

**Status**: [ ]

**Requirements**:
- Test creating full Chat → Run → Message hierarchy
- Test schema migration on fresh database
- Verify foreign keys and cascades work

**Tests to Write** (from spec):
- tests/Acode.Integration.Tests/Conversation/DataModelE2ETests.cs (CREATE NEW)
  - Should_Create_Full_Hierarchy
  - Should_Migrate_Schema
  - Should_Cascade_Delete_Runs_And_Messages
  - Should_Handle_Large_Content (50KB message)

**How to Test**:
```bash
dotnet test --filter "DataModelE2ETests" --verbosity normal
# Expect: 4 tests pass
```

**Success Criteria**:
- [ ] Full hierarchy created successfully
- [ ] Schema migration applies correctly
- [ ] Cascade delete works
- [ ] Large content handled (50KB test)
- [ ] All E2E tests passing

**Evidence**:
```
# Fill this in when complete
```

---

### P7.3: Performance Benchmarks (from spec lines 1963-2025, AC-094 through AC-098)

**Status**: [ ]

**Requirements**:
- AC-094: Insert Chat completes in < 10ms
- AC-095: Get by ID completes in < 5ms
- AC-096: List 100 items completes in < 50ms
- AC-097: Update completes in < 10ms
- AC-098: Connection pool reused between operations

**Tests to Write**:
- tests/Acode.Performance.Tests/ConversationBenchmarks.cs (CREATE NEW)
  - Insert_Chat (target 5ms, max 10ms)
  - Insert_Message (target 3ms, max 10ms)
  - Get_By_Id (target 2ms, max 5ms)
  - List_100_Chats (target 25ms, max 50ms)

**Implementation** (from spec):
```csharp
[MemoryDiagnoser]
public class ConversationBenchmarks
{
    private ChatRepository _repository = null!;

    [GlobalSetup]
    public async Task Setup() { /* ... */ }

    [Benchmark]
    public async Task Insert_Chat() { /* target < 10ms */ }

    [Benchmark]
    public async Task Get_By_Id() { /* target < 5ms */ }

    [Benchmark]
    public async Task List_100_Chats() { /* target < 50ms */ }
}
```

**How to Test**:
```bash
dotnet run --project tests/Acode.Performance.Tests -c Release
# Review benchmark results
```

**Success Criteria**:
- [ ] BenchmarkDotNet setup
- [ ] All 4 benchmarks run
- [ ] Performance targets met
- [ ] AC-094 through AC-098 satisfied

**Evidence**:
```
# Fill this in when complete
```

---

## PRIORITY 8: FINAL AUDIT AND VERIFICATION

### P8.0: Pre-Audit Gap Analysis (MANDATORY BEFORE P8.1)

**Status**: [ ]

**Purpose**: Ensure zero gaps before audit and PR submission.

**Steps**:
1. Run Gap Analysis (docs/GAP_ANALYSIS_METHODOLOGY.md):
    - Compare implementation against spec (lines 854-1453)
    - List any missing AC, FR, or tests
    
2. Re-verify "Implement Assigned Task" checklist (docs/tasks/implement assigned task prompt.md):
    - All relevant code from Implementation Prompt accounted for -- files exist, code exists, compiles, tests correctly, is semantically complete. 
    - All requirements mapped to code
    - All code covered by tests
    - All tests passing

3. If gaps found:
    - Add new items to relevant Priority section above
    - Implement with TDD (tests first)
    - Return to step 1

4. Proceed to P8.1 only when zero gaps identified

**Success Criteria**:
- [ ] Gap analysis completed
- [ ] Zero gaps remaining
- [ ] All new items (if any) implemented and tested

**Evidence**:
```
# List gaps found and how resolved
```

---

### P8.1: Acceptance Criteria Audit (All 98 AC)

**Status**: [ ]

**Requirements**:
- Verify ALL 98+ acceptance criteria are satisfied
- Document evidence for each AC
- Create audit report

**Process**:
1. Read docs/AUDIT-GUIDELINES.md
2. Go through ALL 98 AC (lines 1175-1453 in spec)
3. For each AC, verify:
   - Implementation exists
   - Tests exist and pass
   - Behavior matches spec
4. Document findings in audit report

**Audit Report Location**: docs/audits/task-049a-audit-report.md (CREATE NEW)

**Template**:
```markdown
# Task 049a Audit Report

## Summary
- Total AC: 98
- Satisfied: X
- Unsatisfied: Y
- Pass Rate: Z%

## Chat Entity (AC-001 through AC-014)
- [✅] AC-001: ChatId is ULID - Evidence: ChatIdTests.cs:12, Chat.cs:45
- [✅] AC-002: Title stored (max 500 chars) - Evidence: ChatTests.cs:34
...

## Issues Found
- None / List issues

## Conclusion
- Ready for PR: Yes/No
```

**Success Criteria**:
- [ ] All 98 AC reviewed
- [ ] Audit report created
- [ ] 100% pass rate
- [ ] No blocking issues

**Evidence**:
```
# Link to audit report when complete
```

---

### P8.2: Build and Test Verification

**Status**: [ ]

**Requirements**:
- Build succeeds with 0 warnings
- All tests pass (0 failures, 0 skips)
- Code coverage > 80%

**Commands**:
```bash
# Clean build
dotnet clean
dotnet build --configuration Release --no-incremental

# Run all tests
dotnet test --configuration Release --logger "console;verbosity=detailed"

# Check coverage (if tooling available)
dotnet test --collect:"XPlat Code Coverage"
```

**Success Criteria**:
- [ ] Build: 0 errors, 0 warnings
- [ ] Tests: 100% pass rate
- [ ] Coverage: >80% (target 90%+)
- [ ] StyleCop violations: 0

**Evidence**:
```
# Paste build and test output when complete
```

---

### P8.3: Create Pull Request

**Status**: [ ]

**Requirements**:
- Feature branch: feature/task-049a-conversation-data-model-storage
- All commits follow Conventional Commits format
- PR description comprehensive

**PR Template**:
```markdown
# Task 049a: Conversation Data Model + Storage Provider

## Summary
Implements canonical data model for conversation history with SQLite storage provider.

## What's Included
- **Domain Entities**: Chat, Run, Message (aggregate root and children)
- **Value Objects**: ChatId, RunId, MessageId, ToolCall
- **Repository Interfaces**: IChatRepository, IRunRepository, IMessageRepository
- **SQLite Implementation**: Full CRUD with Dapper
- **Database Migrations**: 001_InitialSchema.sql with version control
- **Security**: SQL injection protection, optimistic concurrency, sensitive data redaction
- **98 Acceptance Criteria**: 100% satisfied

## Files Changed
- Domain: 15 files (entities, value objects, enums)
- Application: 4 files (repository interfaces, filter types)
- Infrastructure: 8 files (SQLite implementations, migrations)
- Tests: 25 files (unit, integration, E2E, performance)

## Test Results
```
Total tests: 150+
     Passed: 150+
     Failed: 0
   Skipped: 0
```

## Performance Benchmarks
- Insert Chat: 4.2ms (target <10ms) ✅
- Get by ID: 1.8ms (target <5ms) ✅
- List 100: 38ms (target <50ms) ✅

## Audit
- Audit report: docs/audits/task-049a-audit-report.md
- Pass rate: 100%

## Dependencies
- Task 050: Workspace database (provides infrastructure)
- Dapper: Lightweight ORM for data access
- SQLite: Local storage

## Breaking Changes
None - new functionality only

## Migration Notes
- Auto-applies on first run
- Creates .agent/chats.db in workspace
- WAL mode enabled for concurrent access

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
```

**Success Criteria**:
- [ ] Feature branch created
- [ ] All code committed and pushed
- [ ] PR created with comprehensive description
- [ ] All CI checks passing (if applicable)

**Evidence**:
```
# PR URL when created
```

---

## COMPLETION SUMMARY

### Overall Progress
- [ ] Priority 1: Domain Value Objects (6 items)
- [ ] Priority 2: Domain Entities (3 items)
- [ ] Priority 3: Repository Interfaces (3 items)
- [ ] Priority 4: SQLite Implementations (3 items)
- [ ] Priority 5: Database Migrations (1 item)
- [ ] Priority 6: Error Handling & Security (5 items)
- [ ] Priority 7: Integration & E2E Tests (3 items)
- [ ] Priority 8: Final Audit & PR (3 items)

**Total Items**: 27 major items
**Completed**: 0
**In Progress**: 0
**Remaining**: 27

### Next Steps
1. Start with Priority 1: Domain Value Objects
2. Work sequentially through priorities
3. Update this file after each item
4. Commit frequently (after each logical unit)
5. When complete, create audit report and PR

### Notes
- Refer to spec for complete code examples (lines 2417-3566)
- All tests have examples in spec (lines 1456-2415)
- Security threats documented with full mitigation code (lines 359-780)
- Performance targets clearly defined (AC-094 through AC-098)

---

**Last Updated**: 2026-01-10 (Initial creation)
**Status**: NOT STARTED
**Estimated Effort**: 8 Fibonacci points (as per spec)
