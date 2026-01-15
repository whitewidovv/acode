# Task-049b Completion Checklist: CRUSD APIs + CLI Commands

**Status:** 🟥 5.2% COMPLETE - READY FOR IMPLEMENTATION (CQRS Pattern Missing)

**Date:** 2026-01-15
**Created From:** task-049b-semantic-gap-analysis.md (43 gaps identified)
**Reference Implementation:** task-049d-completion-checklist.md (gold standard)
**Spec Reference:** docs/tasks/refined-tasks/Epic 02/task-049b-crusd-apis-cli-commands.md (2914 lines)

---

## CRITICAL: READ THIS FIRST

### METHODOLOGY

This checklist is created FROM the gap analysis (task-049b-semantic-gap-analysis.md), not before it. Each gap listed below comes directly from AC verification findings showing what's MISSING.

**Before using this checklist:**

1. Read `task-049b-semantic-gap-analysis.md` completely (identifies all 43 missing/incomplete components)
2. Understand that semantic completeness = 6/115 (5.2%) - CLI works but violates CQRS architecture
3. The entire Application Layer CQRS pattern is missing
4. Critical blocker: Must implement CQRS infrastructure BEFORE handlers

### BLOCKING DEPENDENCIES: NONE ✅

All required domain entities from task-049a (Chat, Run, Message, IDs) are available.
No dependency on other tasks - can implement independently.

### HOW TO USE THIS CHECKLIST

#### For Fresh-Context Agent:

1. **Read task-049b-semantic-gap-analysis.md** (identifies all 43 gaps)
2. **Read Section 2** - Gap mapping shows what each gap implements
3. **Follow Phases 1-8 sequentially** in TDD order
4. **For Each Gap:**
   - Write test(s) that fail (RED)
   - Implement minimum code to pass (GREEN)
   - Clean up while keeping tests green (REFACTOR)
5. **Mark Progress:** `[ ]` = not started, `[🔄]` = in progress, `[✅]` = complete
6. **After Each Gap:** Run `dotnet test` and verify tests pass
7. **After Each Gap:** Commit with `git commit -m "feat(task-049b): [gap description]"`

#### For Continuing Agent:

1. Find last `[✅]` item
2. Read next `[🔄]` or `[ ]` item
3. Follow same TDD cycle
4. Update checklist with test evidence

---

## SECTION 1: SEMANTIC COMPLETENESS STATUS

### Current State (VERIFIED IN GAP ANALYSIS)

**Total Acceptance Criteria:** 115 in scope
**ACs Complete:** 6 (partial CLI functionality only)
**ACs Partial:** 0
**ACs Missing:** 109 (95%)

### Completed Work (DO NOT REDO)

✅ CLI command execution (ChatCommand.cs, 895 lines)
- All 13 subcommand methods implemented
- Tests passing (40 tests in ChatCommandTests.cs)
- Integration tests present (ChatCommandIntegrationTests.cs)

### Missing/Incomplete Work (CRITICAL - MUST IMPLEMENT)

**CQRS Infrastructure (4 gaps) - PHASE 1**
❌ ICommand interface
❌ IQuery interface
❌ ICommandHandler interface
❌ IQueryHandler interface

**Command Records (6 gaps) - PHASE 2**
❌ CreateChatCommand record
❌ OpenChatCommand record
❌ RenameChatCommand record
❌ DeleteChatCommand record
❌ RestoreChatCommand record
❌ PurgeChatCommand record

**Query Records (2 gaps) - PHASE 3**
❌ ListChatsQuery record + ChatSummary DTO
❌ ShowChatQuery record + ChatDetails DTO

**Command Handlers (6 gaps) - PHASE 4**
❌ CreateChatHandler (63 lines)
❌ OpenChatHandler (37 lines)
❌ RenameChatHandler (33 lines)
❌ DeleteChatHandler (31 lines)
❌ RestoreChatHandler (36 lines)
❌ PurgeChatHandler (52 lines)

**Query Handlers (2 gaps) - PHASE 5**
❌ ListChatsHandler (34 lines)
❌ ShowChatHandler (38 lines)

**CLI Refactoring (1 gap) - PHASE 6**
⚠️ ChatCommand.cs refactor to use CQRS mediator pattern

**Handler Tests (8 gaps) - PHASE 7**
❌ 50+ unit tests for all handlers

**Missing Features (5 gaps) - PHASE 8**
❌ Confirmation prompts for delete
❌ Double confirmation for purge
❌ Input sanitization (character validation)
❌ Rate limiting for purge
❌ Workspace authorization verification

---

## SECTION 2: IMPLEMENTATION PHASES

### PHASE 1: CQRS INFRASTRUCTURE (4 gaps, 4 hours)

These 4 interfaces are REQUIRED before implementing any handlers. Start here.

---

#### Gap 1: ICommand Interface [ ]

**ACs Covered:** AC-042, AC-107 (frameworks/patterns)
**Effort:** 1 hour
**Spec Reference:** Lines 2278, 2387, 2429, 2467, 2503, 2545
**Status:** [ ]

**What to Implement:**

Create `src/Acode.Application/Chat/Commands/ICommand.cs`:

```csharp
namespace Acode.Application.Chat.Commands;

/// <summary>
/// Marker interface for command objects in CQRS pattern.
/// Commands represent actions that change state.
/// </summary>
public interface ICommand<TResult>
{
}
```

**Tests (1):**
- [ ] Interface compiles with generic TResult type
- [ ] Command records can implement ICommand<T>

**Success Criteria:**
- [ ] File exists at correct path
- [ ] Generic interface with TResult type parameter
- [ ] 1 test passing

---

#### Gap 2: IQuery Interface [ ]

**ACs Covered:** AC-042, AC-107 (frameworks/patterns)
**Effort:** 1 hour
**Spec Reference:** Lines 2341, 2602
**Status:** [ ]

**What to Implement:**

Create `src/Acode.Application/Chat/Queries/IQuery.cs`:

```csharp
namespace Acode.Application.Chat.Queries;

/// <summary>
/// Marker interface for query objects in CQRS pattern.
/// Queries represent actions that read state without changing it.
/// </summary>
public interface IQuery<TResult>
{
}
```

**Tests (1):**
- [ ] Interface compiles with generic TResult type

**Success Criteria:**
- [ ] File exists at correct path
- [ ] Generic interface with TResult type parameter
- [ ] 1 test passing

---

#### Gap 3: ICommandHandler Interface [ ]

**ACs Covered:** AC-042, AC-107, AC-109 (handler registration)
**Effort:** 1 hour
**Spec Reference:** Lines 2281, 2390, 2432, 2470, 2506, 2548
**Status:** [ ]

**What to Implement:**

Create `src/Acode.Application/Chat/Commands/ICommandHandler.cs`:

```csharp
namespace Acode.Application.Chat.Commands;

/// <summary>
/// Handler for command objects. Implements business logic for a specific command.
/// </summary>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    /// <summary>
    /// Handles the command and returns the result.
    /// </summary>
    Task<Result<TResult, Error>> Handle(TCommand command, CancellationToken ct);
}
```

**Tests (1):**
- [ ] Interface compiles with command and result generics
- [ ] Handler can implement ICommandHandler<T, TResult>

**Success Criteria:**
- [ ] File exists at correct path
- [ ] Generic interface with TCommand constraint
- [ ] Handle method with Result return type
- [ ] 1 test passing

---

#### Gap 4: IQueryHandler Interface [ ]

**ACs Covered:** AC-042, AC-107, AC-109
**Effort:** 1 hour
**Spec Reference:** Lines 2351, 2616
**Status:** [ ]

**What to Implement:**

Create `src/Acode.Application/Chat/Queries/IQueryHandler.cs`:

```csharp
namespace Acode.Application.Chat.Queries;

/// <summary>
/// Handler for query objects. Implements read-only logic for a specific query.
/// </summary>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    /// <summary>
    /// Handles the query and returns the result.
    /// </summary>
    Task<Result<TResult, Error>> Handle(TQuery query, CancellationToken ct);
}
```

**Tests (1):**
- [ ] Interface compiles with query and result generics

**Success Criteria:**
- [ ] File exists at correct path
- [ ] Generic interface with TQuery constraint
- [ ] Handle method with Result return type
- [ ] 1 test passing

---

### PHASE 2: COMMAND RECORDS (6 gaps, 3 hours)

With CQRS interfaces in place, create command record definitions.

---

#### Gap 5: CreateChatCommand Record [ ]

**ACs Covered:** AC-001-012 (Create Command ACs)
**Effort:** 0.5 hours
**Spec Reference:** Lines 2275-2278
**Status:** [ ]

**What to Implement:**

Create `src/Acode.Application/Chat/Commands/CreateChatCommand.cs`:

```csharp
namespace Acode.Application.Chat.Commands;

public sealed record CreateChatCommand(
    string? Title,
    WorktreeId? WorktreeId,
    bool AutoTitle) : ICommand<ChatId>;
```

**Tests (1):**
- [ ] Record compiles and implements ICommand<ChatId>

**Success Criteria:**
- [ ] File exists with correct record definition
- [ ] Implements ICommand<ChatId>
- [ ] 1 test passing

---

#### Gap 6: OpenChatCommand Record [ ]

**ACs Covered:** AC-029-036 (Open Command ACs)
**Effort:** 0.5 hours
**Spec Reference:** Lines 2387
**Status:** [ ]

**What to Implement:**

Create `src/Acode.Application/Chat/Commands/OpenChatCommand.cs`:

```csharp
namespace Acode.Application.Chat.Commands;

public sealed record OpenChatCommand(ChatId ChatId) : ICommand<Unit>;
```

**Tests (1):**
- [ ] Record compiles and implements ICommand<Unit>

**Success Criteria:**
- [ ] File exists with correct record definition
- [ ] 1 test passing

---

#### Gap 7: RenameChatCommand Record [ ]

**ACs Covered:** AC-049-058 (Rename Command ACs)
**Effort:** 0.5 hours
**Spec Reference:** Lines 2429
**Status:** [ ]

**What to Implement:**

```csharp
namespace Acode.Application.Chat.Commands;

public sealed record RenameChatCommand(ChatId ChatId, string NewTitle) : ICommand<Unit>;
```

**Tests (1):**
- [ ] Record compiles

**Success Criteria:**
- [ ] 1 test passing

---

#### Gap 8: DeleteChatCommand Record [ ]

**ACs Covered:** AC-059-070 (Delete Command ACs)
**Effort:** 0.5 hours
**Spec Reference:** Lines 2467
**Status:** [ ]

**What to Implement:**

```csharp
namespace Acode.Application.Chat.Commands;

public sealed record DeleteChatCommand(ChatId ChatId) : ICommand<Unit>;
```

**Tests (1):**
- [ ] Record compiles

**Success Criteria:**
- [ ] 1 test passing

---

#### Gap 9: RestoreChatCommand Record [ ]

**ACs Covered:** AC-071-078 (Restore Command ACs)
**Effort:** 0.5 hours
**Spec Reference:** Lines 2503
**Status:** [ ]

**What to Implement:**

```csharp
namespace Acode.Application.Chat.Commands;

public sealed record RestoreChatCommand(ChatId ChatId) : ICommand<Unit>;
```

**Tests (1):**
- [ ] Record compiles

**Success Criteria:**
- [ ] 1 test passing

---

#### Gap 10: PurgeChatCommand Record [ ]

**ACs Covered:** AC-079-094 (Purge Command ACs)
**Effort:** 0.5 hours
**Spec Reference:** Lines 2545
**Status:** [ ]

**What to Implement:**

```csharp
namespace Acode.Application.Chat.Commands;

public sealed record PurgeChatCommand(ChatId ChatId) : ICommand<Unit>;
```

**Tests (1):**
- [ ] Record compiles

**Success Criteria:**
- [ ] 1 test passing

---

### PHASE 3: QUERY RECORDS (2 gaps, 2 hours)

Create query record definitions with response DTOs.

---

#### Gap 11: ListChatsQuery Record + ChatSummary DTO [ ]

**ACs Covered:** AC-013-028 (List Command ACs)
**Effort:** 1 hour
**Spec Reference:** Lines 2336-2348
**Status:** [ ]

**What to Implement:**

Create `src/Acode.Application/Chat/Queries/ListChatsQuery.cs`:

```csharp
namespace Acode.Application.Chat.Queries;

public sealed record ListChatsQuery(
    bool IncludeDeleted,
    string? Filter,
    ChatSortField SortBy,
    int Page,
    int PageSize) : IQuery<PagedResult<ChatSummary>>;

public sealed record ChatSummary(
    ChatId Id,
    string Title,
    DateTimeOffset UpdatedAt,
    bool IsDeleted,
    int MessageCount);

public enum ChatSortField
{
    Title,
    CreatedAt,
    UpdatedAt
}
```

**Tests (2):**
- [ ] Query record compiles
- [ ] ChatSummary DTO compiles

**Success Criteria:**
- [ ] File exists with query and DTOs
- [ ] 2 tests passing

---

#### Gap 12: ShowChatQuery Record + ChatDetails DTO [ ]

**ACs Covered:** AC-037-048 (Show Command ACs)
**Effort:** 1 hour
**Spec Reference:** Lines 2602-2613
**Status:** [ ]

**What to Implement:**

Create `src/Acode.Application/Chat/Queries/ShowChatQuery.cs`:

```csharp
namespace Acode.Application.Chat.Queries;

public sealed record ShowChatQuery(ChatId ChatId) : IQuery<ChatDetails>;

public sealed record ChatDetails(
    ChatId Id,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<string> Tags,
    WorktreeId? WorktreeId,
    bool IsDeleted,
    int RunCount,
    int MessageCount);
```

**Tests (1):**
- [ ] Query record and DTO compile

**Success Criteria:**
- [ ] 1 test passing

---

### PHASE 4: COMMAND HANDLERS (6 gaps, 12 hours)

Implement handlers with full business logic per spec (lines 2281-2599).

---

#### Gap 13: CreateChatHandler [ ]

**ACs Covered:** AC-001-012
**Effort:** 3 hours
**Spec Reference:** Lines 2281-2333
**Status:** [ ]

**What to Implement:**

Create `src/Acode.Application/Chat/Handlers/CreateChatHandler.cs` with:
- Determine title (auto-generate if needed)
- Validate title (not null/whitespace)
- Create domain Chat entity
- Bind to worktree if provided
- Persist via repository
- Set as active in session
- Log information
- Return ChatId in Result

**Tests (5):**
- [ ] Creates chat with explicit title
- [ ] Creates chat with auto-generated title
- [ ] Binds chat to worktree when requested
- [ ] Rejects empty title when not auto-generating
- [ ] Sets created chat as active

**Success Criteria:**
- [ ] All 5 tests passing
- [ ] Handler implements ICommandHandler<CreateChatCommand, ChatId>
- [ ] Business logic per spec lines 2301-2332

---

#### Gap 14: OpenChatHandler [ ]

**ACs Covered:** AC-029-036
**Effort:** 2 hours
**Spec Reference:** Lines 2390-2426
**Status:** [ ]

**What to Implement:**

Implementation logic from spec:
- Validate chat exists
- Set as active in session
- Log information
- Return Unit in Result

**Tests (3):**
- [ ] Opens existing chat
- [ ] Returns error when chat not found
- [ ] Sets chat as active session

**Success Criteria:**
- [ ] 3 tests passing

---

#### Gap 15: RenameChatHandler [ ]

**ACs Covered:** AC-049-058
**Effort:** 2 hours
**Spec Reference:** Lines 2432-2464
**Status:** [ ]

**What to Implement:**

- Load chat (from repository)
- Validate title
- Call domain Chat.Rename()
- Persist via update
- Log rename event
- Return Unit

**Tests (3):**
- [ ] Renames existing chat
- [ ] Validates new title
- [ ] Updates UpdatedAt timestamp

**Success Criteria:**
- [ ] 3 tests passing

---

#### Gap 16: DeleteChatHandler [ ]

**ACs Covered:** AC-059-070
**Effort:** 2 hours
**Spec Reference:** Lines 2470-2500
**Status:** [ ]

**What to Implement:**

- Load chat
- Call domain Chat.SoftDelete()
- Persist update
- Log warning
- Return Unit

**Tests (3):**
- [ ] Soft-deletes chat
- [ ] Sets IsDeleted=true
- [ ] Sets DeletedAt timestamp

**Success Criteria:**
- [ ] 3 tests passing

---

#### Gap 17: RestoreChatHandler [ ]

**ACs Covered:** AC-071-078
**Effort:** 2 hours
**Spec Reference:** Lines 2506-2542
**Status:** [ ]

**What to Implement:**

- Load chat (includeDeleted: true)
- Check if already deleted (idempotent)
- Call domain Chat.Restore()
- Persist update
- Log information
- Return Unit

**Tests (3):**
- [ ] Restores deleted chat
- [ ] Idempotent (restoring active chat succeeds)
- [ ] Clears DeletedAt timestamp

**Success Criteria:**
- [ ] 3 tests passing

---

#### Gap 18: PurgeChatHandler [ ]

**ACs Covered:** AC-079-094
**Effort:** 4 hours (most complex - cascade delete)
**Spec Reference:** Lines 2548-2599
**Status:** [ ]

**What to Implement:**

- Load chat (includeDeleted: true)
- Validate exists, return error if not
- Load all runs for chat
- For each run:
  - Delete all messages in run (via IMessageRepository.DeleteByRunAsync)
  - Delete run (via IRunRepository.DeleteAsync)
- Delete chat (via IChatRepository.DeleteAsync)
- Log CRITICAL level with chat ID, title, run count
- Return Unit
- Must be atomic (all-or-nothing)

**Tests (5):**
- [ ] Purges chat and all associated data
- [ ] Cascade-deletes all runs
- [ ] Cascade-deletes all messages
- [ ] Logs CRITICAL level event
- [ ] Atomic (no partial cascade)

**Success Criteria:**
- [ ] 5 tests passing
- [ ] All cascade operations work

---

### PHASE 5: QUERY HANDLERS (2 gaps, 4 hours)

Implement read-only handlers.

---

#### Gap 19: ListChatsHandler [ ]

**ACs Covered:** AC-013-028
**Effort:** 2 hours
**Spec Reference:** Lines 2351-2384
**Status:** [ ]

**What to Implement:**

- Build ChatFilter from query parameters
- Call repository ListAsync
- Map Chat entities to ChatSummary DTOs
- Return PagedResult<ChatSummary>

**Tests (3):**
- [ ] Lists active chats by default
- [ ] Includes archived when requested
- [ ] Filters by title substring

**Success Criteria:**
- [ ] 3 tests passing

---

#### Gap 20: ShowChatHandler [ ]

**ACs Covered:** AC-037-048
**Effort:** 2 hours
**Spec Reference:** Lines 2616-2653
**Status:** [ ]

**What to Implement:**

- Load chat (includeDeleted: true)
- Load runs for chat
- Build ChatDetails DTO with all details
- Return ChatDetails in Result

**Tests (3):**
- [ ] Shows complete chat details
- [ ] Includes run count and message count
- [ ] Returns error when chat not found

**Success Criteria:**
- [ ] 3 tests passing

---

### PHASE 6: CLI REFACTORING (1 gap, 3 hours)

Refactor ChatCommand to use CQRS mediator pattern.

---

#### Gap 21: Refactor ChatCommand to Use Mediator [ ]

**ACs Covered:** AC-107 (layer boundaries), AC-109 (handler registration)
**Effort:** 3 hours
**Spec Reference:** Lines 48-91 (architecture diagram), 2659-2695 (ChatCommand router)
**Status:** [ ]

**Current Issue:**

ChatCommand.cs (895 lines) implements all logic inline with direct repository access. This violates the architectural spec which requires:
- CLI layer should NOT access repositories directly
- Business logic should be in Application layer handlers
- Commands should dispatch through mediator (IMediator from MediatR)

**What to Refactor:**

1. Change ChatCommand.NewAsync() to:
   ```csharp
   var command = new CreateChatCommand(title, worktreeId, autoTitle);
   var result = await _mediator.Send(command, ct);
   ```
   Instead of direct repository calls

2. Same pattern for all subcommand methods
   - Use commands for write operations
   - Use queries for read operations
   - Dispatch through _mediator.Send()

3. Dependency injection change:
   - FROM: Direct IChatRepository, IRunRepository, etc.
   - TO: IMediator + register handlers in DI

**Tests (5):**
- [ ] ChatCommand dispatches CreateChatCommand through mediator
- [ ] ChatCommand dispatches ListChatsQuery through mediator
- [ ] ChatCommand dispatches DeleteChatCommand through mediator
- [ ] Error handling works with mediator pattern
- [ ] Output formatting unchanged (CLI behavior identical)

**Success Criteria:**
- [ ] All ChatCommand tests still pass (40+ tests)
- [ ] No direct repository access from CLI layer
- [ ] Uses _mediator.Send(command/query)
- [ ] DI container registers all handlers

---

### PHASE 7: HANDLER UNIT TESTS (8 gaps, 8 hours)

Comprehensive unit test coverage for all handlers (50+ tests total).

---

#### Gap 22: CreateChatHandlerTests [ ]

**Effort:** 1 hour
**Expected Tests:** 5+

**Tests to Write:**
- [ ] Should_Create_Chat_With_Explicit_Title()
- [ ] Should_Create_Chat_With_Auto_Generated_Title()
- [ ] Should_Bind_Chat_To_Worktree()
- [ ] Should_Reject_Empty_Title_When_Not_Auto_Generating()
- [ ] Should_Set_Created_Chat_As_Active()

**Success Criteria:**
- [ ] All 5+ tests passing

---

#### Gap 23: ListChatsHandlerTests [ ]

**Effort:** 0.5 hours
**Expected Tests:** 3+

**Tests to Write:**
- [ ] Should_List_Active_Chats_By_Default()
- [ ] Should_Include_Archived_When_Requested()
- [ ] Should_Filter_By_Title()

**Success Criteria:**
- [ ] All 3+ tests passing

---

#### Gap 24: OpenChatHandlerTests [ ]

**Effort:** 0.5 hours
**Expected Tests:** 3+

---

#### Gap 25: ShowChatHandlerTests [ ]

**Effort:** 0.5 hours
**Expected Tests:** 3+

---

#### Gap 26: RenameChatHandlerTests [ ]

**Effort:** 1 hour
**Expected Tests:** 5+

---

#### Gap 27: DeleteChatHandlerTests [ ]

**Effort:** 1 hour
**Expected Tests:** 5+

---

#### Gap 28: RestoreChatHandlerTests [ ]

**Effort:** 1 hour
**Expected Tests:** 5+

---

#### Gap 29: PurgeChatHandlerTests [ ]

**Effort:** 2 hours
**Expected Tests:** 8+

---

### PHASE 8: MISSING FEATURES (5 gaps, 6 hours)

Implement remaining acceptance criteria features.

---

#### Gap 30: Delete Confirmation Prompt [ ]

**ACs Covered:** AC-060 (confirmation prompt)
**Effort:** 1 hour
**Spec Reference:** Lines 2149-2151
**Status:** [ ]

**Requirement:** "Delete prompts for confirmation: Are you sure you want to delete chat 'Title'? [y/N]"

**Implementation:**
- Use Spectre.Console for interactive prompt
- Default to 'N' (no confirmation = no delete)
- Accept 'y' or 'Y' to proceed
- Show "Operation cancelled" on 'N'

**Tests (2):**
- [ ] Prompts user for confirmation
- [ ] Aborts on default (Enter key)

**Success Criteria:**
- [ ] 2 tests passing
- [ ] AC-060, AC-061, AC-062 verified

---

#### Gap 31: Purge Double Confirmation [ ]

**ACs Covered:** AC-080 (double confirmation)
**Effort:** 1 hour
**Spec Reference:** Lines 2175-2176
**Status:** [ ]

**Requirement:** "Purge requires double confirmation: Type the chat ID to confirm permanent deletion"

**Implementation:**
- First prompt shows warning about permanent deletion
- Second prompt asks to type chat ID
- Abort if typed ID doesn't match target

**Tests (2):**
- [ ] Requires typed ID confirmation
- [ ] Aborts if ID doesn't match

**Success Criteria:**
- [ ] 2 tests passing
- [ ] AC-080, AC-081 verified

---

#### Gap 32: Input Sanitization (Character Validation) [ ]

**ACs Covered:** AC-111 (input sanitization), AC-004-005 (title validation)
**Effort:** 1 hour
**Spec Reference:** Lines 2078-2079, 1843-1845
**Status:** [ ]

**Requirement:** "Title accepts 1-500 characters alphanumeric with spaces and common punctuation. Rejects invalid characters (< > : \" / \\ | ? * and control characters)"

**Implementation:**
- Regex pattern for valid characters: `^[a-zA-Z0-9\s\-_(),.!?&']+$`
- Check length 1-500
- Return ACODE-CHAT-CMD-002 error for invalid

**Tests (3):**
- [ ] Accepts valid titles
- [ ] Rejects invalid characters
- [ ] Enforces 1-500 character range

**Success Criteria:**
- [ ] 3 tests passing
- [ ] AC-004, AC-005 verified

---

#### Gap 33: Purge Rate Limiting [ ]

**ACs Covered:** AC-094 (rate limiting)
**Effort:** 2 hours
**Spec Reference:** Lines 1189
**Status:** [ ]

**Requirement:** "Purge rate-limited to 1 per minute per workspace"

**Implementation:**
- Track last purge time per workspace
- Throw error if <60 seconds since last purge
- Error code: ACODE-CHAT-CMD-005

**Tests (2):**
- [ ] Allows first purge
- [ ] Blocks second purge within 60 seconds

**Success Criteria:**
- [ ] 2 tests passing
- [ ] AC-094 verified

---

#### Gap 34: Workspace Authorization Verification [ ]

**ACs Covered:** AC-113 (workspace isolation)
**Effort:** 1 hour
**Spec Reference:** Lines 1214
**Status:** [ ]

**Requirement:** "Cannot access other workspace's chats"

**Implementation:**
- Verify chat.WorktreeId matches current workspace context
- Return error if mismatch

**Tests (2):**
- [ ] Allows access to own workspace chats
- [ ] Blocks access to other workspace chats

**Success Criteria:**
- [ ] 2 tests passing
- [ ] AC-113 verified

---

## SECTION 3: VERIFICATION CHECKLIST

**After all gaps complete, verify:**

- [ ] All 34 gaps implemented
- [ ] All 115 ACs verified semantically complete
- [ ] All handler unit tests passing (50+ tests)
- [ ] All CLI tests passing (40+ tests) - refactored but behavior identical
- [ ] Zero NotImplementedException
- [ ] Zero build errors/warnings
- [ ] Performance benchmarks passing (AC-012, AC-026, AC-036)
- [ ] Code coverage > 80%
- [ ] DI registration complete
- [ ] All error codes implemented (ACODE-CHAT-CMD-001 through ACODE-CHAT-CMD-008)
- [ ] PR created and ready for review

---

## GIT WORKFLOW

**Commit after each gap:**

```bash
git commit -m "feat(task-049b): [Gap NN] - [gap description]"
```

**Example:**
```bash
git commit -m "feat(task-049b): Gap 1 - Implement ICommand interface"
git commit -m "feat(task-049b): Gap 5-10 - Implement CreateChatCommand and other command records"
git commit -m "feat(task-049b): Gap 13 - Implement CreateChatHandler with full business logic"
```

---

**Next Action:** Begin Phase 1 (Gaps 1-4) - create CQRS infrastructure interfaces

**Estimated Total Effort:** 45-50 hours (spread across phases)
