# Task 049: Conversation History & Multi-Chat Management

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 2 – CLI + Orchestration Core  
**Dependencies:** Task 010 (CLI), Task 011 (Session), Task 021 (Security), Task 023 (Events), Task 026 (Storage)  

---

## Description

Task 049 implements conversation history and multi-chat management—the system that stores, organizes, and retrieves chat sessions. Users can have multiple concurrent conversations, switch between them, and search across history. The architecture is offline-first with SQLite for local storage and PostgreSQL for remote sync.

Conversation history is fundamental to agentic coding workflows. Developers work on multiple features simultaneously—each deserving its own conversation context. The agent needs to recall what was discussed, what was tried, and what worked. History enables continuity across sessions and machines.

The data model centers on Chats, Runs, and Messages. A Chat is a conversation thread with a title and metadata. A Run is an execution within a chat—a single user request and the agent's response. Messages are the individual exchanges: user prompts, agent responses, tool calls, and tool results.

Offline-first architecture ensures the agent works without network. SQLite serves as the local database—fast, reliable, zero-config. Changes are queued in an outbox for sync. When network is available, the sync engine pushes changes to PostgreSQL and pulls updates from other machines.

CRUSD operations (Create, Read, Update, Soft-Delete) provide full lifecycle management. Create new chats for new topics. Read to load conversation context. Update to rename or tag chats. Soft-delete to archive without losing history. Restore to recover archived chats. Purge for permanent deletion.

Multi-chat concurrency allows multiple open conversations. Each chat can be bound to a worktree for context. Switching chats is instant—context is isolated. The agent maintains state per-chat without confusion.

Search spans all history. Find messages by content, filter by date, filter by chat. Full-text search enables quick retrieval: "What did we decide about the authentication approach?" Search works locally and remotely.

Privacy and retention policies govern data lifecycle. Configure how long to retain chats. Configure what syncs to remote. Redact sensitive content before sync. Export conversations for archival.

Integration with the session system (Task 011) ensures conversation state survives interruptions. Resume a chat after restart, after network loss, after machine switch. The experience is seamless.

The sync engine (Task 049.f) handles the complexity of distributed data. Conflict resolution when the same chat is modified on multiple machines. Batching for efficiency. Retries for reliability. Idempotency for safety.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Chat | Conversation thread |
| Run | Single execution (request/response) |
| Message | Individual exchange unit |
| CRUSD | Create, Read, Update, Soft-Delete |
| Offline-First | Local works without network |
| SQLite | Local embedded database |
| PostgreSQL | Remote database |
| Outbox | Queue for pending syncs |
| Sync | Synchronize local/remote |
| Worktree | Git working directory |
| Soft-Delete | Mark deleted, keep data |
| Purge | Permanent deletion |
| Restore | Recover soft-deleted |
| Retention | How long to keep data |
| Full-Text Search | Content-based search |

---

## Out of Scope

The following items are explicitly excluded from Task 049:

- **Detailed data model** - Task 049.a
- **CLI commands** - Task 049.b
- **Concurrency model** - Task 049.c
- **Search indexing** - Task 049.d
- **Retention/privacy** - Task 049.e
- **Sync engine** - Task 049.f
- **Real-time collaboration** - Not supported
- **Chat sharing** - Single user
- **Chat encryption** - Task 021
- **Branching conversations** - Linear only
- **Chat templates** - Not in scope

---

## Functional Requirements

### Chat Management

- FR-001: Create chat MUST work
- FR-002: Chat MUST have unique ID
- FR-003: Chat MUST have title
- FR-004: Chat MUST have created timestamp
- FR-005: Chat MUST have updated timestamp
- FR-006: Chat MAY have tags
- FR-007: Chat MAY have worktree binding

### Run Management

- FR-008: Run MUST belong to chat
- FR-009: Run MUST have unique ID
- FR-010: Run MUST have status
- FR-011: Run MUST have timestamps
- FR-012: Run MUST track token usage

### Message Management

- FR-013: Message MUST belong to run
- FR-014: Message MUST have role
- FR-015: Message MUST have content
- FR-016: Message MUST have timestamp
- FR-017: Message MAY have tool calls

### CRUSD Operations

- FR-018: Create chat MUST work offline
- FR-019: Read chat MUST work offline
- FR-020: Update chat MUST work offline
- FR-021: Soft-delete MUST work offline
- FR-022: Restore MUST work offline
- FR-023: Purge MUST work offline

### Offline-First

- FR-024: All operations MUST work offline
- FR-025: Local MUST be authoritative offline
- FR-026: Changes MUST queue for sync
- FR-027: Sync MUST happen when online

### Local Storage

- FR-028: SQLite MUST be used locally
- FR-029: Database MUST be in .agent/
- FR-030: Schema MUST support migrations
- FR-031: WAL mode MUST be enabled

### Remote Storage

- FR-032: PostgreSQL MUST be supported
- FR-033: Remote MUST be source of truth online
- FR-034: Sync MUST be bidirectional
- FR-035: Conflicts MUST be resolved

### Multi-Chat

- FR-036: Multiple chats MUST be supported
- FR-037: Chat switching MUST be instant
- FR-038: Active chat MUST be tracked
- FR-039: Chat context MUST be isolated

### Search

- FR-040: Search MUST work locally
- FR-041: Full-text search MUST work
- FR-042: Filter by chat MUST work
- FR-043: Filter by date MUST work

### Session Integration

- FR-044: Resume MUST restore chat
- FR-045: Checkpoint MUST include chat
- FR-046: Recovery MUST load chat

---

## Non-Functional Requirements

### Performance

- NFR-001: Chat creation < 50ms
- NFR-002: Message append < 10ms
- NFR-003: Chat load < 100ms
- NFR-004: Search < 500ms

### Reliability

- NFR-005: No data loss
- NFR-006: Crash-safe storage
- NFR-007: Eventual consistency

### Security

- NFR-008: No secrets in plain text
- NFR-009: Redaction before sync
- NFR-010: Access control

### Scalability

- NFR-011: 1000+ chats per workspace
- NFR-012: 10000+ messages per chat
- NFR-013: Efficient pagination

### Usability

- NFR-014: Instant chat switching
- NFR-015: Clear status indicators
- NFR-016: Intuitive navigation

---

## User Manual Documentation

### Overview

Conversation history in Acode provides persistent, searchable storage for all your agent interactions. Work offline, sync when online, and never lose context.

### Quick Start

```bash
# Start a new chat
$ acode chat new "Feature: User Authentication"
Created chat: chat_abc123
Title: Feature: User Authentication

# Continue chatting
$ acode run "Let's design the login flow"

# List all chats
$ acode chat list
ID          Title                         Last Active
chat_abc123 Feature: User Authentication  2m ago
chat_def456 Bug Fix: Memory Leak          1h ago
chat_ghi789 Refactor: Database Layer      2d ago

# Switch to a different chat
$ acode chat open chat_def456
Switched to: Bug Fix: Memory Leak
```

### Chat Lifecycle

```bash
# Create
$ acode chat new "My Topic"

# Rename
$ acode chat rename chat_abc123 "Better Title"

# Tag
$ acode chat tag chat_abc123 feature backend

# Archive (soft-delete)
$ acode chat delete chat_abc123
Chat archived. Use 'restore' to recover.

# Restore
$ acode chat restore chat_abc123
Chat restored.

# Permanent delete
$ acode chat purge chat_abc123
Permanently delete chat? [y/N] y
Chat purged.
```

### Multi-Chat Workflows

```bash
# Open multiple chats in parallel worktrees
$ acode chat new --worktree feature/auth "Auth Feature"
$ acode chat new --worktree bugfix/leak "Memory Bug"

# Each worktree has its own active chat
$ cd feature/auth
$ acode run "Continue auth work"
# Uses auth chat context

$ cd ../bugfix/leak  
$ acode run "Check memory allocations"
# Uses memory bug chat context
```

### Search

```bash
# Search all chats
$ acode chat search "authentication"

Results for 'authentication':
  [chat_abc123] Feature: User Authentication
    - "Let's design the authentication flow..."
    - "We should use JWT tokens..."
  
  [chat_xyz789] Security Review
    - "The authentication needs 2FA..."

# Search within chat
$ acode chat search --chat chat_abc123 "JWT"

# Search by date
$ acode chat search --since 2024-01-01 "deployment"
```

### Viewing History

```bash
# Show chat messages
$ acode chat show chat_abc123

Chat: Feature: User Authentication
Created: 2024-01-15 10:00 UTC
Messages: 47

[Run 1] 2024-01-15 10:01
User: Let's design the login flow
Agent: I'll help design the login flow...
  [Tool] file_read: src/auth/login.ts
  [Tool] file_write: src/auth/login.ts (45 lines)

[Run 2] 2024-01-15 10:15
User: Now add password validation
...
```

### Configuration

```yaml
# .agent/config.yml
conversation:
  storage:
    # Local database location
    local_path: .agent/chats.db
    
    # Remote sync
    remote_enabled: true
    remote_url: postgres://...
    
  # Auto-title chats
  auto_title: true
  
  # Default retention
  retention_days: 365
  
  # Sync settings
  sync:
    batch_size: 50
    retry_attempts: 3
```

### Offline Mode

Acode works fully offline:

```bash
# Disconnect network
$ acode run "Make changes"
# Works normally, queues for sync

# Check sync status
$ acode sync status
Pending: 3 messages
Last sync: 2h ago
Status: Waiting for network

# When online
$ acode sync now
Syncing...
✓ 3 messages synced
```

### Export

```bash
# Export chat to JSON
$ acode chat export chat_abc123 > chat.json

# Export to Markdown
$ acode chat export --format markdown chat_abc123 > chat.md

# Export all chats
$ acode chat export --all > all_chats.json
```

---

## Acceptance Criteria

### Chat Management

- [ ] AC-001: Create chat works
- [ ] AC-002: Chat has unique ID
- [ ] AC-003: Chat has title
- [ ] AC-004: Chat has timestamps
- [ ] AC-005: Update title works
- [ ] AC-006: Tags work

### Run Management

- [ ] AC-007: Run belongs to chat
- [ ] AC-008: Run has unique ID
- [ ] AC-009: Run tracks status
- [ ] AC-010: Run tracks tokens

### Message Management

- [ ] AC-011: Message belongs to run
- [ ] AC-012: Message has role
- [ ] AC-013: Message has content
- [ ] AC-014: Tool calls stored

### CRUSD

- [ ] AC-015: Create works offline
- [ ] AC-016: Read works offline
- [ ] AC-017: Update works offline
- [ ] AC-018: Soft-delete works
- [ ] AC-019: Restore works
- [ ] AC-020: Purge works

### Storage

- [ ] AC-021: SQLite works
- [ ] AC-022: PostgreSQL works
- [ ] AC-023: Sync works
- [ ] AC-024: Offline works

### Multi-Chat

- [ ] AC-025: Multiple chats work
- [ ] AC-026: Switching works
- [ ] AC-027: Context isolated

### Search

- [ ] AC-028: Search works
- [ ] AC-029: Filters work
- [ ] AC-030: Full-text works

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Conversation/
├── ChatTests.cs
│   ├── Should_Create_Chat()
│   ├── Should_Update_Title()
│   └── Should_Soft_Delete()
│
├── RunTests.cs
│   ├── Should_Create_Run()
│   └── Should_Track_Status()
│
└── MessageTests.cs
    ├── Should_Store_Message()
    └── Should_Store_Tool_Calls()
```

### Integration Tests

```
Tests/Integration/Conversation/
├── StorageTests.cs
│   ├── Should_Persist_To_SQLite()
│   ├── Should_Sync_To_Postgres()
│   └── Should_Handle_Offline()
│
└── SearchTests.cs
    └── Should_Search_Messages()
```

### E2E Tests

```
Tests/E2E/Conversation/
├── MultiChatE2ETests.cs
│   ├── Should_Create_Multiple_Chats()
│   ├── Should_Switch_Chats()
│   └── Should_Maintain_Context()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Chat create | 25ms | 50ms |
| Message append | 5ms | 10ms |
| Chat load | 50ms | 100ms |
| Search 10k | 250ms | 500ms |

---

## User Verification Steps

### Scenario 1: Create Chat

1. Run `acode chat new "Test"`
2. Verify: Chat created with ID

### Scenario 2: Add Messages

1. Run `acode run "Hello"`
2. Run `acode chat show`
3. Verify: Messages visible

### Scenario 3: Multi-Chat

1. Create two chats
2. Switch between them
3. Verify: Context isolated

### Scenario 4: Search

1. Add messages with keyword
2. Run `acode chat search <keyword>`
3. Verify: Messages found

### Scenario 5: Offline

1. Disconnect network
2. Create chat, add messages
3. Verify: All works locally

### Scenario 6: Sync

1. Make offline changes
2. Reconnect network
3. Run `acode sync now`
4. Verify: Changes synced

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Conversation/
│   ├── Chat.cs
│   ├── Run.cs
│   └── Message.cs
│
src/AgenticCoder.Application/
├── Conversation/
│   ├── IChatRepository.cs
│   ├── IRunRepository.cs
│   └── IMessageRepository.cs
│
src/AgenticCoder.Infrastructure/
├── Persistence/
│   └── Conversation/
│       ├── SqliteChatRepository.cs
│       └── PostgresChatRepository.cs
```

### Chat Aggregate

```csharp
namespace AgenticCoder.Domain.Conversation;

public sealed class Chat : AggregateRoot<ChatId>
{
    public string Title { get; private set; }
    public IReadOnlyList<string> Tags { get; }
    public WorktreeId? WorktreeBinding { get; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }
    
    public static Chat Create(string title, WorktreeId? worktree = null);
    public void Rename(string newTitle);
    public void SoftDelete();
    public void Restore();
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-CONV-001 | Chat not found |
| ACODE-CONV-002 | Chat already exists |
| ACODE-CONV-003 | Storage error |
| ACODE-CONV-004 | Sync failed |
| ACODE-CONV-005 | Search failed |

### Implementation Checklist

1. [ ] Create Chat entity
2. [ ] Create Run entity
3. [ ] Create Message entity
4. [ ] Create repository interfaces
5. [ ] Implement SQLite repositories
6. [ ] Implement PostgreSQL repositories
7. [ ] Create sync engine
8. [ ] Add CLI commands
9. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Domain entities
2. **Phase 2:** SQLite storage
3. **Phase 3:** CLI commands
4. **Phase 4:** Search
5. **Phase 5:** PostgreSQL
6. **Phase 6:** Sync engine

---

**End of Task 049 Specification**