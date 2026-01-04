# Task 049.b: CRUSD APIs + CLI Commands

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 2 – CLI + Orchestration Core  
**Dependencies:** Task 049.a (Data Model), Task 010 (CLI), Task 011 (Session)  

---

## Description

Task 049.b implements the CRUSD APIs and CLI commands for conversation management. CRUSD extends CRUD with Soft-Delete—archiving conversations without permanent loss. Users interact through intuitive commands: create, list, open, rename, delete, restore, and purge.

The API layer exposes application services for conversation management. CreateChatCommand, RenameChatCommand, DeleteChatCommand, and similar handlers implement business logic. These services orchestrate domain operations and repository persistence.

CLI commands provide user-facing access. Each command maps to an application service. Commands use consistent patterns: positional chat ID, flags for options, interactive prompts for confirmations. Output formats support human-readable and JSON modes.

Chat creation supports multiple workflows. Create with explicit title. Create with auto-generated title. Create bound to a worktree. Create from template. Each workflow produces a properly initialized Chat entity.

List commands provide discovery and navigation. List all chats. Filter by status (active, archived). Sort by date, title, or activity. Search by title substring. Pagination handles large histories.

Open command sets the active chat. The active chat receives new runs and messages. Only one chat is active per worktree. Switching is instant—context loads from local storage.

Rename enables title updates. Titles can be changed at any time. Auto-title suggests names based on conversation content. Manual override always wins.

Delete implements soft-delete. Deleted chats are archived, not destroyed. Archived chats are hidden from normal list. Metadata is preserved for restore.

Restore recovers archived chats. Restored chats reappear in active list. All runs and messages are intact. Restore is idempotent—restoring an active chat is a no-op.

Purge provides permanent deletion. Purge removes chat and all associated data. Purge requires explicit confirmation. Purge is irreversible—no recovery possible.

Error handling covers common scenarios. Chat not found. Chat already exists. Invalid title. Permission denied. Each error has a specific exit code and message.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| CRUSD | Create, Read, Update, Soft-Delete |
| Active Chat | Currently selected conversation |
| Archived | Soft-deleted, recoverable |
| Purge | Permanent deletion |
| Restore | Recover from archive |
| Auto-Title | System-generated title |
| Worktree Binding | Chat-to-worktree association |
| Application Service | Business logic handler |
| Command | CLI user invocation |
| Handler | Application layer processor |
| Exit Code | Command result status |
| Interactive | Requires user input |
| Batch | Non-interactive mode |
| Filter | List constraint |
| Pagination | Chunked results |

---

## Out of Scope

The following items are explicitly excluded from Task 049.b:

- **Data model** - Task 049.a
- **Concurrency** - Task 049.c
- **Search indexing** - Task 049.d
- **Retention** - Task 049.e
- **Sync** - Task 049.f
- **Message operations** - Chat-level only
- **Run operations** - Chat-level only
- **Bulk operations** - Single chat per command
- **Import from external** - Native only
- **Chat templates** - Not in scope

---

## Assumptions

### Technical Assumptions

- ASM-001: CLI commands follow established conventions from Task 010
- ASM-002: CRUD operations map to standard CLI verbs (new, list, show, delete)
- ASM-003: Command output supports both human-readable and JSON formats
- ASM-004: Pagination handles large chat/message lists
- ASM-005: Commands are atomic (succeed or fail completely)

### Behavioral Assumptions

- ASM-006: Users interact with chats primarily via CLI
- ASM-007: Chat switching changes active context for subsequent operations
- ASM-008: Delete requires confirmation or --force flag
- ASM-009: List shows summary, show provides full details
- ASM-010: Search integrates with chat commands

### Dependency Assumptions

- ASM-011: Task 049.a data model is available
- ASM-012: Task 010 CLI framework provides command infrastructure
- ASM-013: Task 010.b JSONL mode applies to chat commands

### UX Assumptions

- ASM-014: Commands are discoverable via --help
- ASM-015: Error messages explain what went wrong
- ASM-016: Common operations require minimal typing

---

## Functional Requirements

### Create Command

- FR-001: `acode chat new` MUST create chat
- FR-002: `acode chat new <title>` MUST set title
- FR-003: `acode chat new --worktree` MUST bind
- FR-004: `acode chat new --auto-title` MUST generate
- FR-005: Created chat MUST be active
- FR-006: MUST return chat ID
- FR-007: MUST log creation

### List Command

- FR-008: `acode chat list` MUST show chats
- FR-009: Default MUST hide archived
- FR-010: `--archived` MUST show archived
- FR-011: `--all` MUST show everything
- FR-012: `--sort` MUST support date/title
- FR-013: `--filter` MUST support search
- FR-014: MUST paginate results

### Open Command

- FR-015: `acode chat open <id>` MUST set active
- FR-016: MUST validate chat exists
- FR-017: MUST load chat context
- FR-018: MUST update session
- FR-019: MUST log activation

### Rename Command

- FR-020: `acode chat rename <id> <title>` MUST work
- FR-021: MUST validate title
- FR-022: MUST update timestamp
- FR-023: MUST log rename

### Delete Command (Soft)

- FR-024: `acode chat delete <id>` MUST archive
- FR-025: MUST set IsDeleted true
- FR-026: MUST set DeletedAt
- FR-027: MUST prompt confirmation
- FR-028: `--force` MUST skip prompt
- FR-029: MUST log deletion

### Restore Command

- FR-030: `acode chat restore <id>` MUST recover
- FR-031: MUST clear IsDeleted
- FR-032: MUST clear DeletedAt
- FR-033: MUST be idempotent
- FR-034: MUST log restore

### Purge Command

- FR-035: `acode chat purge <id>` MUST delete permanently
- FR-036: MUST require double confirmation
- FR-037: MUST delete runs
- FR-038: MUST delete messages
- FR-039: `--force` MUST skip prompts
- FR-040: MUST log purge

### Show Command

- FR-041: `acode chat show <id>` MUST display details
- FR-042: MUST show metadata
- FR-043: MUST show statistics
- FR-044: `--json` MUST output JSON

### Status Command

- FR-045: `acode chat status` MUST show active
- FR-046: MUST show worktree binding
- FR-047: MUST show run count

### Application Services

- FR-048: CreateChatHandler MUST exist
- FR-049: ListChatsHandler MUST exist
- FR-050: OpenChatHandler MUST exist
- FR-051: RenameChatHandler MUST exist
- FR-052: DeleteChatHandler MUST exist
- FR-053: RestoreChatHandler MUST exist
- FR-054: PurgeChatHandler MUST exist
- FR-055: ShowChatHandler MUST exist

### Validation

- FR-056: Title MUST NOT be empty
- FR-057: Title MUST NOT exceed 500 chars
- FR-058: Chat ID MUST be valid ULID
- FR-059: Chat MUST exist for operations

### Output Formats

- FR-060: Default MUST be table format
- FR-061: `--json` MUST be JSON
- FR-062: `--quiet` MUST be ID only

---

## Non-Functional Requirements

### Performance

- NFR-001: Create < 100ms
- NFR-002: List < 200ms
- NFR-003: Open < 50ms
- NFR-004: All ops async

### Usability

- NFR-005: Clear error messages
- NFR-006: Helpful suggestions
- NFR-007: Consistent patterns

### Reliability

- NFR-008: Atomic operations
- NFR-009: No partial states
- NFR-010: Safe defaults

### Security

- NFR-011: No secrets in output
- NFR-012: Confirmation for destructive

---

## User Manual Documentation

### Overview

Chat commands manage conversation history. Create new conversations, switch between them, organize with titles, and clean up when done.

### Quick Reference

```bash
acode chat new [title]     # Create chat
acode chat list            # List chats
acode chat open <id>       # Switch to chat
acode chat show <id>       # View details
acode chat rename <id>     # Change title
acode chat delete <id>     # Archive chat
acode chat restore <id>    # Recover chat
acode chat purge <id>      # Permanent delete
acode chat status          # Current chat
```

### Creating Chats

```bash
# Create with title
$ acode chat new "Feature: Authentication"
Created chat: chat_abc123
Title: Feature: Authentication
Status: Active

# Create with auto-title (from first message)
$ acode chat new --auto-title
Created chat: chat_def456
Title: (will be set after first message)

# Create bound to worktree
$ acode chat new --worktree feature/auth "Auth Feature"
Created chat: chat_ghi789
Bound to: feature/auth
```

### Listing Chats

```bash
# List active chats
$ acode chat list
ID          Title                         Updated     Runs
chat_abc123 Feature: Authentication       2m ago      5
chat_def456 Bug Fix: Memory Leak          1h ago      3
chat_ghi789 Refactor: Database            2d ago      12

# Include archived
$ acode chat list --archived
ID          Title                         Status      Updated
chat_xyz000 Old Feature (archived)        Archived    30d ago

# Filter by title
$ acode chat list --filter auth
ID          Title                         Updated
chat_abc123 Feature: Authentication       2m ago

# Sort options
$ acode chat list --sort title   # Alphabetical
$ acode chat list --sort date    # Most recent
$ acode chat list --sort runs    # Most runs
```

### Opening Chats

```bash
# Switch to chat
$ acode chat open chat_abc123
Switched to: Feature: Authentication

# Chat is now active
$ acode run "Continue with login"
# Runs in chat_abc123 context
```

### Viewing Details

```bash
$ acode chat show chat_abc123

Chat: chat_abc123
────────────────────────────────────
Title: Feature: Authentication
Created: 2024-01-15 10:00 UTC
Updated: 2024-01-15 14:32 UTC

Statistics:
  Runs: 5
  Messages: 47
  Tokens: 12,450

Tags: feature, auth, backend
Worktree: feature/auth

# JSON output
$ acode chat show chat_abc123 --json
{
  "id": "chat_abc123",
  "title": "Feature: Authentication",
  ...
}
```

### Renaming Chats

```bash
$ acode chat rename chat_abc123 "Feature: User Authentication v2"
Renamed: chat_abc123
New title: Feature: User Authentication v2
```

### Archiving (Delete)

```bash
$ acode chat delete chat_abc123
Archive chat 'Feature: Authentication'? [y/N] y
Chat archived.

# Skip confirmation
$ acode chat delete chat_abc123 --force
Chat archived.
```

### Restoring

```bash
$ acode chat restore chat_abc123
Chat restored: Feature: Authentication
```

### Permanent Deletion

```bash
$ acode chat purge chat_abc123
DANGER: This will permanently delete the chat and all its data.
Type the chat ID to confirm: chat_abc123
Chat purged.

# Skip confirmation (CI/scripts only)
$ acode chat purge chat_abc123 --force
Chat purged.
```

### Current Status

```bash
$ acode chat status
Active Chat: chat_abc123
Title: Feature: Authentication
Runs: 5 (latest: 2m ago)
Worktree: feature/auth
```

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | General error |
| 2 | Chat not found |
| 3 | Invalid input |
| 4 | Operation cancelled |

### Configuration

```yaml
# .agent/config.yml
chat:
  # Default sort order
  default_sort: date
  
  # Auto-title behavior
  auto_title:
    enabled: true
    max_length: 50
    
  # Confirmation settings
  confirm:
    delete: true
    purge: true
```

---

## Acceptance Criteria

### Create

- [ ] AC-001: `chat new` works
- [ ] AC-002: Title accepted
- [ ] AC-003: Worktree binding works
- [ ] AC-004: Auto-title works
- [ ] AC-005: Returns ID

### List

- [ ] AC-006: Lists chats
- [ ] AC-007: Hides archived
- [ ] AC-008: --archived works
- [ ] AC-009: Sort works
- [ ] AC-010: Filter works
- [ ] AC-011: Pagination works

### Open

- [ ] AC-012: Opens chat
- [ ] AC-013: Validates exists
- [ ] AC-014: Updates session

### Show

- [ ] AC-015: Shows details
- [ ] AC-016: Shows stats
- [ ] AC-017: JSON output

### Rename

- [ ] AC-018: Renames chat
- [ ] AC-019: Validates title
- [ ] AC-020: Updates timestamp

### Delete

- [ ] AC-021: Archives chat
- [ ] AC-022: Prompts confirm
- [ ] AC-023: --force works

### Restore

- [ ] AC-024: Restores chat
- [ ] AC-025: Idempotent

### Purge

- [ ] AC-026: Permanently deletes
- [ ] AC-027: Deletes runs
- [ ] AC-028: Deletes messages
- [ ] AC-029: Double confirm

### Status

- [ ] AC-030: Shows active
- [ ] AC-031: Shows binding

---

## Best Practices

### Command Design

- **BP-001: Consistent verb usage** - Use new/create, list, show, delete consistently
- **BP-002: Sensible defaults** - Commands should work with minimal arguments
- **BP-003: Confirmation for destructive actions** - Require --force or confirmation for delete
- **BP-004: Clear output** - Success messages should confirm what was done

### Error Handling

- **BP-005: Specific error messages** - "Chat 'abc123' not found" not just "Not found"
- **BP-006: Exit codes** - Use distinct exit codes for different errors
- **BP-007: Suggestions for recovery** - Tell users how to fix the problem
- **BP-008: Log errors with context** - Include enough detail for debugging

### API Design

- **BP-009: Validate early** - Check inputs before starting operations
- **BP-010: Return meaningful results** - Include created/modified entity in response
- **BP-011: Support filtering** - Allow list commands to filter results
- **BP-012: Paginate by default** - Don't return unbounded lists

---

## Troubleshooting

### Command Not Recognized

**Symptom:** `acode chat <subcommand>` says unknown command.

**Cause:** Typo in command name or missing registration.

**Solution:**
1. Check `acode chat --help` for available commands
2. Verify spelling of subcommand

### Chat Creation Fails

**Symptom:** `acode chat new` returns error.

**Cause:** Database error or invalid name.

**Solution:**
1. Check database connectivity
2. Verify chat name doesn't contain invalid characters
3. Check disk space

### List Returns Empty

**Symptom:** `acode chat list` shows no chats when some should exist.

**Cause:** Filter too restrictive or wrong workspace.

**Solution:**
1. Remove filters and try again
2. Verify you're in correct workspace
3. Check if chats were deleted

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Chat/Commands/
├── CreateChatHandlerTests.cs
│   ├── Should_Create_With_Title()
│   ├── Should_Create_With_AutoTitle()
│   └── Should_Bind_Worktree()
│
├── ListChatsHandlerTests.cs
│   ├── Should_List_Active()
│   ├── Should_Include_Archived()
│   └── Should_Filter()
│
├── DeleteChatHandlerTests.cs
│   ├── Should_Soft_Delete()
│   └── Should_Set_DeletedAt()
│
└── PurgeChatHandlerTests.cs
    ├── Should_Delete_Cascade()
    └── Should_Require_Confirm()
```

### Integration Tests

```
Tests/Integration/Chat/
├── ChatCommandsIntegrationTests.cs
│   ├── Should_Create_And_List()
│   ├── Should_Delete_And_Restore()
│   └── Should_Purge_Cascade()
```

### E2E Tests

```
Tests/E2E/Chat/
├── ChatWorkflowE2ETests.cs
│   ├── Should_Complete_Lifecycle()
│   └── Should_Switch_Chats()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Create | 50ms | 100ms |
| List 100 | 100ms | 200ms |
| Open | 25ms | 50ms |

---

## User Verification Steps

### Scenario 1: Create Chat

1. Run `acode chat new "Test Chat"`
2. Verify: Chat created, ID returned

### Scenario 2: List Chats

1. Create multiple chats
2. Run `acode chat list`
3. Verify: All chats shown

### Scenario 3: Open Chat

1. Create chat
2. Run `acode chat open <id>`
3. Verify: Chat is active

### Scenario 4: Rename

1. Create chat
2. Run `acode chat rename <id> "New Name"`
3. Verify: Title changed

### Scenario 5: Delete and Restore

1. Create chat
2. Delete chat
3. Restore chat
4. Verify: Chat active again

### Scenario 6: Purge

1. Create chat with runs
2. Purge chat with --force
3. Verify: Chat and runs gone

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Application/
├── Chat/
│   ├── Commands/
│   │   ├── CreateChatCommand.cs
│   │   ├── ListChatsQuery.cs
│   │   ├── OpenChatCommand.cs
│   │   ├── RenameChatCommand.cs
│   │   ├── DeleteChatCommand.cs
│   │   ├── RestoreChatCommand.cs
│   │   └── PurgeChatCommand.cs
│   └── Handlers/
│       ├── CreateChatHandler.cs
│       └── ...
│
src/AgenticCoder.CLI/
└── Commands/
    └── ChatCommand.cs
```

### CreateChatCommand

```csharp
namespace AgenticCoder.Application.Chat.Commands;

public sealed record CreateChatCommand(
    string? Title,
    WorktreeId? WorktreeId,
    bool AutoTitle) : ICommand<ChatId>;
```

### CreateChatHandler

```csharp
namespace AgenticCoder.Application.Chat.Handlers;

public sealed class CreateChatHandler 
    : ICommandHandler<CreateChatCommand, ChatId>
{
    public async Task<ChatId> Handle(
        CreateChatCommand command,
        CancellationToken ct)
    {
        var chat = Domain.Conversation.Chat.Create(
            command.Title ?? "Untitled",
            command.WorktreeId);
            
        await _repository.CreateAsync(chat, ct);
        await _session.SetActiveChatAsync(chat.Id, ct);
        
        return chat.Id;
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-CHAT-CMD-001 | Chat not found |
| ACODE-CHAT-CMD-002 | Invalid title |
| ACODE-CHAT-CMD-003 | Operation cancelled |
| ACODE-CHAT-CMD-004 | Chat already exists |
| ACODE-CHAT-CMD-005 | Purge failed |

### Implementation Checklist

1. [ ] Create command records
2. [ ] Create handlers
3. [ ] Create CLI commands
4. [ ] Add validation
5. [ ] Add confirmations
6. [ ] Add logging
7. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Commands/handlers
2. **Phase 2:** CLI integration
3. **Phase 3:** Validation
4. **Phase 4:** Confirmations
5. **Phase 5:** Tests

---

**End of Task 049.b Specification**