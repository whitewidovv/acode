# Task 049b Gap Analysis and Fixes

## INSTRUCTIONS FOR RESUMPTION AFTER CONTEXT COMPACTION

**Current Status**: Phase 4 in progress - ChatCommand router complete, implementing subcommand methods.

**What to do next**:
1. Read this entire file to understand completed work and remaining tasks
2. Implement remaining CLI subcommand methods in `src/Acode.Cli/Commands/ChatCommand.cs`
3. Each subcommand is a private method (NewAsync, ListAsync, OpenAsync, etc.) - replace TODO stubs with implementation
4. Follow existing pattern from ConfigCommand.cs in same directory
5. Use injected dependencies: `_chatRepository`, `_runRepository`, `_messageRepository`, `_sessionManager`
6. Follow TDD: write tests after each subcommand implementation
7. Commit after each logical unit (1-2 subcommands implemented)
8. Reference AC-001 through AC-115 in task spec for requirements

**Key Architecture Notes**:
- **NO MediatR/CQRS**: Implementation Prompt suggested it, but existing codebase uses custom ICommand pattern
- CLI commands call repositories directly (no separate Command/Handler classes)
- `ICommand` interface: `Task<ExitCode> ExecuteAsync(CommandContext context)`
- CommandContext provides: Args, Output, Formatter, CancellationToken, Configuration
- Repository interfaces already exist and are implemented in SQLite

**File Locations**:
- Task spec: `docs/tasks/refined-tasks/Epic 02/task-049b-crusd-apis-cli-commands.md`
- CLI command: `src/Acode.Cli/Commands/ChatCommand.cs` (router complete, subcommands TODO)
- Repositories: `src/Acode.Application/Conversation/Persistence/I*Repository.cs`
- Domain entities: `src/Acode.Domain/Conversation/*.cs`
- Tests: `tests/Acode.Cli.Tests/Commands/ChatCommandTests.cs` (create this)

**Current Branch**: `feature/task-050-workspace-database-foundation`

**Commits So Far**:
- 33f2156: ChatSortField enum + ChatFilter extensions
- 3570909: ISessionManager + InMemorySessionManager
- 06b3c48: IChatRepository.DeleteAsync for hard delete
- 948a515: ChatCommand router with TODO stubs

**Status**: 7/9 subcommands implemented (New, Open, Rename, Delete, Restore, Purge, Status).
Remaining: ListAsync, ShowAsync. Build errors in StatusAsync to fix (Count property vs method).

**Next**: Fix build errors, implement ListAsync and ShowAsync, commit, then write tests.

## Context

Task 049b implements CRUSD APIs + CLI Commands for conversation management. After reading the Implementation Prompt section and analyzing existing code, I've identified gaps between the specification and current implementation.

## Architectural Note

**CRITICAL**: The Implementation Prompt suggests Spectre.Console + MediatR (CQRS pattern), but the existing codebase uses a custom CLI framework with ICommand interface. I'm adapting the implementation to match the EXISTING architecture while satisfying all acceptance criteria.

**Existing Architecture:**
- CLI: Custom `ICommand` interface with `Task<ExitCode> ExecuteAsync(CommandContext context)`
- No MediatR/CQRS (no separate Command/Query/Handler classes)
- CLI commands will call repositories directly or through thin application services
- Repository interfaces already exist: IChatRepository, IRunRepository, IMessageRepository

## Gaps Found

### Gap #1: PagedResult<T> Type
**Status**: ❌ NOT STARTED

**Details**:
- IChatRepository.ListAsync returns `PagedResult<Chat>`
- PagedResult<T> type not found in codebase
- Need to check if it exists or create it

### Gap #2: ChatFilter Extensions
**Status**: ❌ NOT STARTED

**Details**:
- Existing ChatFilter has: WorktreeId, CreatedAfter, CreatedBefore, IncludeDeleted, Page, PageSize
- Missing: TitleContains (string?), SortBy (ChatSortField), SortDescending (bool)
- AC-019 requires title substring filtering
- AC-016, AC-017, AC-018 require sorting by created/updated/title

### Gap #3: ChatSortField Enum
**Status**: ❌ NOT STARTED

**Details**:
- Not found in codebase
- Required for ChatFilter.SortBy property
- Values: CreatedAt, UpdatedAt, Title

### Gap #4: Chat Domain Methods
**Status**: ❌ NOT STARTED

**Details**:
- Need to verify Chat entity has: SoftDelete(), Restore(), Rename(string newTitle)
- AC-049 requires Rename
- AC-059 requires SoftDelete
- AC-071 requires Restore

### Gap #5: ISessionManager Interface
**Status**: ❌ NOT STARTED

**Details**:
- Referenced in Implementation Prompt for tracking active chat
- AC-029, AC-030, AC-031 require opening/setting active chat
- AC-095-102 require status command showing active chat
- Need to define ISessionManager interface

### Gap #6: CLI ChatCommand Router
**Status**: ❌ NOT STARTED

**Details**:
- Need ChatCommand implementing ICommand
- Routes to subcommands: new, list, open, show, rename, delete, restore, purge, status
- AC-001-102 cover all subcommands

### Gap #7: CLI Subcommands (9 commands)
**Status**: ❌ NOT STARTED

**Details**:
- NewChatCommand (AC-001 to AC-012)
- ListChatsCommand (AC-013 to AC-028)
- OpenChatCommand (AC-029 to AC-036)
- ShowChatCommand (AC-037 to AC-048)
- RenameChatCommand (AC-049 to AC-058)
- DeleteChatCommand (AC-059 to AC-070)
- RestoreChatCommand (AC-071 to AC-078)
- PurgeChatCommand (AC-079 to AC-094)
- StatusChatCommand (AC-095 to AC-102)

### Gap #8: IChatRepository Missing Methods
**Status**: ❌ NOT STARTED

**Details**:
- Has: CreateAsync, GetByIdAsync (with includeRuns flag), UpdateAsync, SoftDeleteAsync, ListAsync, GetByWorktreeAsync, PurgeDeletedAsync
- Implementation Prompt shows GetByIdAsync(ChatId id, bool includeDeleted, CancellationToken ct)
- Current signature: GetByIdAsync(ChatId id, bool includeRuns = false, CancellationToken ct = default)
- Need to check if includeDeleted parameter is needed or if we query deleted chats differently

### Gap #9: Hard Delete Method
**Status**: ❌ NOT STARTED

**Details**:
- IChatRepository has SoftDeleteAsync and PurgeDeletedAsync(DateTimeOffset before)
- AC-079-094 requires purging a specific chat by ID
- Need DeleteAsync(ChatId id) for hard delete (used by PurgeChat command)
- Or use existing PurgeDeletedAsync pattern differently

## Implementation Plan

**Phase 1: Support Types** ✅ Not started
1. Check/Create PagedResult<T>
2. Create ChatSortField enum
3. Extend ChatFilter with TitleContains, SortBy, SortDescending

**Phase 2: Domain Layer** ✅ Not started
1. Verify/Add Chat.SoftDelete() method
2. Verify/Add Chat.Restore() method
3. Verify/Add Chat.Rename(string newTitle) method

**Phase 3: Application Layer** ✅ Not started
1. Create ISessionManager interface
2. Create in-memory SessionManager implementation (for now)

**Phase 4: Repository Extensions** ✅ Not started
1. Add DeleteAsync(ChatId id) to IChatRepository for hard delete
2. Update SqliteChatRepository implementation
3. Add includeDeleted parameter handling if needed

**Phase 5: CLI Commands** ✅ Not started
1. Create ChatCommand router
2. Create NewChatCommand
3. Create ListChatsCommand
4. Create OpenChatCommand
5. Create ShowChatCommand
6. Create RenameChatCommand
7. Create DeleteChatCommand
8. Create RestoreChatCommand
9. Create PurgeChatCommand
10. Create StatusChatCommand

**Phase 6: Testing** ✅ Not started
1. Unit tests for each CLI command
2. Integration tests for full CRUSD lifecycle
3. Edge case testing (invalid IDs, confirmations, etc.)

## Progress Tracking

- [x] Phase 1: Support Types (ChatSortField, ChatFilter extensions)
- [x] Phase 2: Session Management (ISessionManager, InMemorySessionManager)
- [x] Phase 3: Repository Extensions (IChatRepository.DeleteAsync)
- [ ] Phase 4: CLI Commands (ChatCommand router + 9 subcommands)
- [ ] Phase 5: Testing

## Completed Work

### Phase 1: Support Types ✅
- Created ChatSortField enum (CreatedAt, UpdatedAt, Title)
- Extended ChatFilter with TitleContains, SortBy, SortDescending
- Defaults: SortBy=UpdatedAt, SortDescending=true
- Commit: 33f2156

### Phase 2: Session Management ✅
- Created ISessionManager interface (SetActiveChatAsync, GetActiveChatAsync, ClearActiveChatAsync)
- Implemented InMemorySessionManager in Infrastructure layer
- In-memory storage (not persisted across CLI invocations)
- Commit: 3570909

### Phase 3: Repository Extensions ✅
- Added IChatRepository.DeleteAsync(ChatId id) for hard delete
- Implemented in SqliteChatRepository with CASCADE DELETE
- Foreign key constraints automatically delete runs and messages
- Commit: 06b3c48
