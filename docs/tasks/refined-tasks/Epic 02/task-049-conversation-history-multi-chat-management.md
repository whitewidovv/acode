# Task 049: Conversation History & Multi-Chat Management

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 2 – CLI + Orchestration Core  
**Dependencies:** Task 010 (CLI), Task 011 (Session), Task 021 (Security), Task 023 (Events), Task 026 (Storage)  

---

## Description

### Business Value

Conversation history is the memory layer of Acode—enabling continuity, context preservation, and knowledge sharing across sessions, machines, and team members. Without persistent conversation storage, developers lose valuable context every time they close the CLI, switch machines, or work on multiple features concurrently. Conversations History & Multi-Chat Management transforms Acode from a stateless tool into an intelligent assistant that remembers, learns, and adapts.

**Direct Business Impact:**
- **Developer Productivity:** Eliminates 40-45 minutes per day spent re-explaining context and searching previous discussions ($17,520/year per developer at $100/hour rate)
- **Incident Resolution:** Reduces investigation time by 50% through complete audit trails of troubleshooting attempts ($13,500 per major incident)
- **Team Knowledge Sharing:** Cuts onboarding time by 60% and repetitive support questions by 80% ($44,760/year for a 10-person team)
- **Multi-Tasking Efficiency:** Enables seamless context switching between concurrent features, reducing cognitive load and errors
- **Audit & Compliance:** Provides complete, immutable records of all AI-assisted development decisions for regulatory and internal review

**Annual ROI for a 10-Developer Team:**
- Productivity gains: 10 developers × $17,520 = $175,200
- Incident cost reduction: 8 incidents/year × $13,500 savings = $108,000
- Onboarding efficiency: 10 hires/year × $2,400 savings = $24,000
- Support time savings: 10 developers × $8,000 = $80,000
- **Total Annual Value: $387,200**

### Technical Architecture

Task 049 implements a three-layer architecture for conversation storage: Domain (entities), Application (use cases), Infrastructure (persistence). The design prioritizes offline-first operation, eventual consistency, and multi-device synchronization.

**Core Entities:**

1. **Chat** - Aggregate root representing a conversation thread
   - Unique identifier (ULID for time-based sorting and global uniqueness)
   - Title (user-provided or auto-generated from first message)
   - Tags (for categorization: \"bug\", \"feature\", \"refactor\")
   - Worktree binding (optional link to git worktree for context isolation)
   - Soft-delete flag (archive without losing history)
   - Timestamps (created, updated, last message)
   - Message count (cached for performance)

2. **Run** - Single execution cycle within a chat (one user request + agent response)
   - Unique identifier (ULID)
   - Parent chat reference
   - Status (pending, in-progress, completed, failed, cancelled)
   - Token usage tracking (prompt tokens, completion tokens, total cost)
   - Duration (start time, end time, elapsed seconds)
   - Model used (for cost analysis and debugging)
   - Exit code (0 for success, non-zero for errors)

3. **Message** - Individual exchange unit within a run
   - Unique identifier (ULID)
   - Parent run reference
   - Role (user, assistant, system, tool)
   - Content (text, code, structured data)
   - Timestamp (sub-second precision for ordering)
   - Tool calls (for assistant messages invoking tools)
   - Tool results (for tool messages returning outputs)
   - Metadata (token count, latency, model-specific info)

**Relationship Model:**
```
Chat (1) ──▶ (N) Run (1) ──▶ (N) Message
  │                              │
  └─ WorktreeBinding?            └─ ToolCall[]?
  └─ Tags[]                      └─ ToolResult?
```

**Storage Architecture:**

**Local Storage (SQLite):**
- Primary data store for offline operation
- Located in `.agent/chats.db` within workspace
- Write-Ahead Logging (WAL) mode for concurrent read/write
- Full-text search index on message content
- Indexes on chat_id, run_id, timestamps for fast queries
- Pragma settings: `journal_mode=WAL`, `synchronous=NORMAL`, `cache_size=-2000` (2MB)
- Schema versioning with migration support (compatible with Task 050 workspace database)

**Remote Storage (PostgreSQL - Optional):**
- Secondary store for multi-machine sync and team sharing
- Connection configured in `~/.acode/config.yml` (user-level, not workspace-level to avoid secret leakage)
- Same schema as SQLite for portability
- Materialized views for aggregations (chat message counts, token usage summaries)
- Row-level security for multi-user scenarios (future)

**Sync Engine (Task 049.f):**
- Outbox pattern for reliable sync: changes written to `sync_outbox` table locally
- Background worker polls outbox every 30 seconds when online
- Batch size: 50 messages per sync round (tunable via config)
- Conflict resolution: Last-Write-Wins (LWW) based on updated_at timestamp
- Idempotency: Each sync operation has a unique ID to prevent duplicates on retry
- Retry policy: Exponential backoff (1s, 2s, 4s, 8s, 16s max), 5 attempts
- Network detection: Periodic health check to PostgreSQL (5s timeout)

**CRUSD Operations:**

- **Create**: New chat with generated ID, initial metadata. Offline: writes to SQLite, queues sync. Online: writes to both SQLite and PostgreSQL transactionally.
- **Read**: Load chat by ID, load all chats, load chat with message history (paginated). Always reads from local SQLite for speed.
- **Update**: Change title, add/remove tags, update worktree binding. Offline: writes locally, queues sync. Online: writes both.
- **Soft-Delete**: Set `is_deleted=true`, preserve all data. UI hides by default, but data remains queryable with `--include-deleted` flag.
- **Restore**: Set `is_deleted=false`. Only works if chat not purged.
- **Purge**: Permanent deletion, cascades to runs and messages. Requires explicit confirmation (`acode chat purge <id> --confirm`). Not reversible.

**Multi-Chat Concurrency:**

Acode supports multiple open chats for concurrent workflows. Key design decisions:

1. **Active Chat Determination**: The active chat is resolved in order of precedence:
   - Explicit `--chat <id>` flag on command
   - Environment variable `ACODE_ACTIVE_CHAT`
   - Worktree binding (if current directory is in a worktree, use its bound chat)
   - Session state (last active chat in current session)
   - Fallback: Prompt user to select or create a chat

2. **Context Isolation**: Each chat maintains its own conversation context. Switching chats does not leak messages or tool call history. The agent's context window is populated only from the active chat's messages.

3. **Worktree Binding**: Chat can be bound to a git worktree (Task 023) for automatic context association. When the user `cd`s into a worktree, the active chat switches to that worktree's bound chat. This enables spatial context: \"When I'm in the `feature/auth` worktree, I'm working on authentication; when I'm in `bugfix/leak`, I'm debugging memory issues.\"

4. **Fast Switching**: Chat switching is <100ms. Only metadata loads initially; messages load on-demand when displayed or sent to model.

**Search Implementation:**

Full-text search uses SQLite's FTS5 extension (fallback to LIKE queries if FTS5 unavailable):

```sql
CREATE VIRTUAL TABLE message_fts USING fts5(
  message_id UNINDEXED,
  content,
  tokenize='porter unicode61'
);
```

Search query:
```sql
SELECT m.* FROM messages m
JOIN message_fts fts ON m.id = fts.message_id
WHERE fts.content MATCH ? 
ORDER BY m.created_at DESC
LIMIT 50;
```

Filters:
- By chat: `WHERE m.chat_id = ?`
- By date range: `WHERE m.created_at BETWEEN ? AND ?`
- By role: `WHERE m.role = ?`
- Combined: Filters applied with AND logic

Search ranking: Results ordered by recency (most recent first). Future: relevance scoring using FTS5 `rank` function.

### Integration Points

**Task 010 (CLI Framework):**
- Conversation commands: `acode chat <subcommand>` (new, list, open, show, rename, tag, delete, restore, purge, search, export, sync)
- Global `--chat <id>` flag to specify active chat
- Output formatting via Task 010.b (JSONL mode) for programmatic access

**Task 011 (Session State Machine):**
- Active chat persisted in session state
- Chat history included in session checkpoints for resume
- Session recovery restores last active chat

**Task 023 (Worktree Management):**
- Worktree-to-chat binding stored in both chat metadata and worktree state
- Automatic chat activation when entering worktree directory
- Cleanup: When worktree is removed, chat binding is cleared (chat remains but unbound)

**Task 050 (Workspace Database):**
- Conversation history shares database connection pool with other workspace data
- Migrations coordinated with workspace schema versioning
- Backup/export includes conversation data

**Task 021 (Security & Secrets):**
- Secret redaction before sync: Messages scanned for API keys, tokens, passwords before writing to remote PostgreSQL
- Redaction patterns configurable in `security.redaction_patterns`
- Redacted content replaced with `[REDACTED:API_KEY]` placeholder

### Constraints and Limitations

1. **Linear Conversation Model**: Conversations are strictly linear—no branching or parallel conversation threads. If a user wants to explore two different approaches, they must create two separate chats. This simplifies the mental model and implementation but reduces flexibility.

2. **Single-User Context**: Task 049 assumes single-user ownership of chats. No sharing, no collaboration, no permissions. Multi-user support deferred to future (would require Row-Level Security in PostgreSQL, user authentication, and access control lists).

3. **Sync Conflicts Use LWW**: Last-Write-Wins conflict resolution is simple but may lose concurrent edits. Example: User edits chat title on machine A, edits same chat title on machine B while offline. When both sync, only the last sync wins. Acceptable trade-off because chat metadata changes are rare and low-risk. Messages are append-only, so no message conflicts.

4. **No Encryption at Rest**: Chat data stored in plain text in SQLite and PostgreSQL. Encryption deferred to Task 021 (disk encryption at OS level recommended). Future: Optionally encrypt messages using workspace-level key.

5. **Pagination Required for Large Chats**: Chats with >1,000 messages must paginate or performance degrades. CLI commands default to last 50 messages; use `--limit` and `--offset` for pagination. UI/API must implement virtual scrolling or infinite scroll for large chats.

6. **Search Limited to Content**: Search only indexes message `content` field. Tool call parameters and tool result outputs are not indexed (would require structured indexing—deferred to Task 049.d).

7. **No Real-Time Sync**: Sync is eventually consistent with 30-second polling interval. Real-time push notifications via WebSocket/SSE not supported. Acceptable for single-user agentic workflows; problematic for multi-user collaboration (future consideration).

### Trade-Offs and Alternatives Considered

**Trade-Off: SQLite vs. PostgreSQL Only**
- **Decision**: Use SQLite locally, PostgreSQL optionally for remote sync.
- **Alternative Considered**: PostgreSQL-only with local caching.
- **Why SQLite**: Zero-config, single-file, embedded, excellent for offline-first. PostgreSQL requires server setup, network connection, complexity.
- **Trade-Off**: Sync complexity, schema duplication. **Benefit**: Offline robustness, simplicity, portability.

**Trade-Off: Outbox Pattern vs. Direct Sync**
- **Decision**: Use outbox table to queue sync operations.
- **Alternative Considered**: Direct write to PostgreSQL when online, with rollback on failure.
- **Why Outbox**: Decouples sync from main workflow, enables retries, prevents blocking. If PostgreSQL is slow or unavailable, local operation continues unaffected.
- **Trade-Off**: Sync lag (up to 30 seconds). **Benefit**: Reliability, availability, performance.

**Trade-Off: Last-Write-Wins vs. Operational Transformation**
- **Decision**: LWW conflict resolution for simplicity.
- **Alternative Considered**: Operational Transformation (OT) or CRDTs for conflict-free merging.
- **Why LWW**: Conflicts are rare (chats are typically edited on one machine at a time). OT/CRDTs add significant complexity for minimal benefit in single-user scenario.
- **Trade-Off**: Potential data loss on rare concurrent edit. **Benefit**: Simple implementation, predictable behavior.

**Trade-Off: Soft-Delete vs. Hard-Delete**
- **Decision**: Soft-delete by default, purge for permanent deletion.
- **Alternative Considered**: Hard-delete only.
- **Why Soft-Delete**: Accidental deletion is common; recovery without backup is valuable. Supports audit trail and compliance.
- **Trade-Off**: Storage growth, additional complexity. **Benefit**: Data recovery, audit history, user confidence.

**Trade-Off: Auto-Title vs. Manual-Only Titles**
- **Decision**: Support both—auto-generate from first message, allow manual override.
- **Alternative Considered**: Manual titles only (like Slack channels).
- **Why Auto**: Reduces friction—user can start chatting immediately. Manual override for important conversations.
- **Trade-Off**: Auto-generated titles may be generic. **Benefit**: Low cognitive load, fast start.

### Performance Targets

| Operation | Target | Maximum | Justification |
|-----------|--------|---------|---------------|
| Create chat | 25ms | 50ms | Single row insert + index update |
| Append message | 5ms | 10ms | Single row insert, FTS index update deferred |
| Load chat metadata | 10ms | 25ms | Single row read by primary key |
| Load chat with last 50 messages | 50ms | 100ms | Join query with limit, indexed |
| Switch active chat | 20ms | 50ms | Update session state, no message load |
| Search 10,000 messages | 250ms | 500ms | FTS5 query, indexed, paginated |
| Sync 50 messages | 500ms | 1000ms | Batch insert to PostgreSQL over network |

### Observability and Logging

**Metrics to Track:**
- Chat operations: create, read, update, delete, restore, purge counts
- Message operations: append, read counts
- Search operations: query count, query latency, result count
- Sync operations: success, failure, conflict count, bytes synced
- Storage: SQLite file size, PostgreSQL table size, message count per chat

**Log Events:**
- Chat lifecycle: `ChatCreated`, `ChatRenamed`, `ChatDeleted`, `ChatRestored`, `ChatPurged`
- Run lifecycle: `RunStarted`, `RunCompleted`, `RunFailed`
- Sync events: `SyncStarted`, `SyncCompleted`, `SyncFailed`, `SyncConflict`
- Search events: `SearchExecuted`, `SearchFailed`

**Error Codes:**
| Code | Meaning | Action |
|------|---------|--------|
| ACODE-CONV-001 | Chat not found | Verify chat ID, check if deleted |
| ACODE-CONV-002 | Chat already exists | Use unique title or explicit ID |
| ACODE-CONV-003 | Storage error | Check SQLite file permissions, disk space |
| ACODE-CONV-004 | Sync failed | Check network, PostgreSQL connection, retry |
| ACODE-CONV-005 | Search failed | Check FTS5 extension, rebuild index |
| ACODE-CONV-006 | Chat deleted | Use --include-deleted or restore |
| ACODE-CONV-007 | Purge not allowed | Confirm with --confirm flag |

This architecture enables Acode to provide seamless, reliable, offline-first conversation management with optional multi-device sync—forming the foundation for persistent, context-aware agentic coding workflows.

---

## Use Cases

### Use Case 1: DevBot's Multi-Feature Development Workflow

**Persona:** DevBot is a senior software engineer working on three concurrent features: authentication refactor, API versioning, and database migration. Each feature requires distinct context, and DevBot switches between them multiple times per day based on team priorities.

**Before:** DevBot used a single long conversation that mixed contexts from all three features. Finding previous discussions required scrolling through hundreds of messages. "What did we decide about the JWT expiry time?" meant reading through irrelevant database discussions. Context switching was slow and error-prone. DevBot wasted 45 minutes per day searching conversation history and re-explaining context when switching topics.

**After:** DevBot creates three separate chats—one per feature. Each chat has a descriptive title and is bound to its worktree: `feature/auth-refactor` → "Auth: JWT Migration to RS256", `feature/api-v2` → "API: v2 Endpoint Design", `feature/db-migration` → "Database: PostgreSQL Migration". When DevBot switches worktrees with `cd feature/auth-refactor`, the active chat automatically switches to the auth context. The agent remembers: "Last time we discussed rotating keys every 30 days." Search with `acode chat search "JWT expiry"` instantly finds the relevant decision in the auth chat. DevBot saves 40 minutes per day—14.6 hours per month, $1,460/month at $100/hour rate. Over a year: **$17,520 productivity gain**.

**Concrete Metrics:**
- Context switch time: 5 minutes → 10 seconds (97% reduction)
- Time spent searching history: 20 minutes/day → 2 minutes/day (90% reduction)
- Incorrect context retrieved: 3 times/week → 0 (100% elimination)
- Monthly time saved: 14.6 hours per developer
- Annual ROI: $17,520 per developer

### Use Case 2: Jordan's Long-Running Investigation

**Persona:** Jordan is investigating a production memory leak that spans multiple weeks. The investigation involves hypothesis testing, profiling runs, code reviews, and discussions with the team. Jordan needs to track what was tried, what failed, and why.

**Before:** Jordan took notes in a text file, but notes lacked agent context—no tool outputs, no code snippets, no timestamps. "Did we try increasing the GC threshold?" required manual searching through notes and logs. After two weeks, Jordan forgot which approaches had been ruled out. The investigation dragged on for 6 weeks with repeated dead-ends. The memory leak cost $500/month in excess infrastructure costs during investigation.

**After:** Jordan creates a chat titled "Incident: Memory Leak in API Gateway" and binds it to worktree `bugfix/memory-leak`. All profiling commands, heap dumps, and code changes are captured in the chat history. When Jordan returns after a weekend, `acode chat show` displays the full context: "Last Friday we confirmed the leak is in the Redis connection pool. We tested increasing pool size—no effect. Next: Check for connection leaks." Search with `acode chat search "connection pool"` finds all related discussions. Jordan exports the investigation as a postmortem with `acode chat export --format markdown > postmortem.md`. The leak is fixed in 3 weeks instead of 6. **$1,500 infrastructure cost saved + 120 hours of developer time saved = $13,500 total savings**.

**Concrete Metrics:**
- Investigation time: 6 weeks → 3 weeks (50% reduction)
- Repeated attempts at ruled-out solutions: 5 → 0 (100% elimination)
- Time to recall previous context after breaks: 30 minutes → 2 minutes (93% reduction)
- Infrastructure waste during investigation: $3,000 → $1,500 (50% reduction)
- Total incident cost: $21,000 → $7,500 (64% reduction)

### Use Case 3: Alex's Team Knowledge Sharing

**Persona:** Alex is a tech lead mentoring junior developers on the team. When juniors ask "How do I set up the local environment?" or "What's our pattern for error handling?", Alex wants to share previous agent conversations as examples rather than re-explaining.

**Before:** Alex re-typed explanations in Slack or copy-pasted code snippets without full context. Juniors got partial information and often misunderstood. "What's the full command to initialize the database?" led to Alex searching through terminal history or re-running commands. Team members asked the same questions multiple times. Alex spent 5 hours per week answering repetitive questions.

**After:** Alex maintains a chat titled "Setup: Local Development Environment" that captures the complete setup process with all commands, outputs, and troubleshooting. When a junior asks for help, Alex shares: `acode chat export chat_setup123 --format markdown > setup-guide.md` and posts the guide in Slack. Juniors follow step-by-step instructions with actual tool outputs and error handling. Complex questions like "How do we handle API rate limiting?" link to chat `chat_api456` with the full design discussion. Alex saves 4 hours per week—17.3 hours per month, $1,730/month at $100/hour rate. Over a year: **$20,760 time savings**. Team onboarding time drops from 5 days to 2 days—saving $2,400 per hire for 10 hires/year = **$24,000 additional savings**.

**Concrete Metrics:**
- Time answering repetitive questions: 5 hours/week → 1 hour/week (80% reduction)
- Junior developer self-service success rate: 30% → 85% (183% improvement)
- Team onboarding time: 5 days → 2 days (60% reduction)
- Onboarding cost per hire: $4,000 → $1,600 (60% reduction)
- Annual value for 10 hires: $20,760 time savings + $24,000 onboarding savings = **$44,760 total**

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

## Assumptions

### Technical Assumptions

- ASM-001: Conversations are stored locally in workspace database
- ASM-002: Each chat has a unique identifier (ULID for time-ordering)
- ASM-003: Messages are append-only within a chat
- ASM-004: Chat metadata is queryable and filterable
- ASM-005: Storage format supports efficient retrieval

### Behavioral Assumptions

- ASM-006: Users manage multiple concurrent chats for different tasks
- ASM-007: Active chat is determined by current context (worktree, session)
- ASM-008: Chat history persists across CLI sessions
- ASM-009: Chat switching is fast and seamless
- ASM-010: Old chats remain accessible until explicitly deleted

### Dependency Assumptions

- ASM-011: Task 011 session model integrates with chat context
- ASM-012: Task 050 workspace database provides storage
- ASM-013: Task 010 CLI provides chat management commands
- ASM-014: Tasks 049.a-f implement component details

### Design Assumptions

- ASM-015: Chat contains messages, messages contain content
- ASM-016: Linear conversation model (no branching)
- ASM-017: Single-user context for all chats

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

## Best Practices

### Chat Management

- **BP-001: Descriptive chat names** - Use meaningful names that describe the task or context
- **BP-002: One chat per task** - Keep conversations focused on a single objective
- **BP-003: Regular archiving** - Archive completed chats to reduce clutter
- **BP-004: Use worktree binding** - Bind chats to worktrees for context association

### Message Handling

- **BP-005: Append-only design** - Never modify existing messages, only add new ones
- **BP-006: Structured content** - Use consistent message formats for parsing
- **BP-007: Metadata for context** - Store additional context as message metadata
- **BP-008: Limit message size** - Keep messages under 100KB for performance

### Performance

- **BP-009: Paginate large histories** - Don't load entire chat history at once
- **BP-010: Index for search** - Ensure search indexes are maintained
- **BP-011: Lazy loading** - Load message content on demand
- **BP-012: Background sync** - Sync operations shouldn't block user interaction

---

## Troubleshooting

### Chat Not Found

**Symptom:** `acode chat open <id>` returns "Chat not found".

**Cause:** Chat ID is incorrect, or chat was deleted.

**Solution:**
1. List all chats with `acode chat list`
2. Verify the chat ID exists
3. Check if chat was deleted (search in deleted chats if available)

### Slow Chat Loading

**Symptom:** Opening a chat with many messages is slow.

**Cause:** Large chat history loading synchronously.

**Solution:**
1. Paginate message loading
2. Archive old portions of conversation
3. Check database performance

### Search Returns No Results

**Symptom:** Search for known content returns empty.

**Cause:** Search index not updated or query syntax issue.

**Solution:**
1. Verify search index is current
2. Try simpler search terms
3. Check if content was redacted

### Active Chat Mismatch

**Symptom:** Commands operate on wrong chat.

**Cause:** Active chat context not set correctly.

**Solution:**
1. Check current active chat with `acode chat current`
2. Switch to correct chat with `acode chat open`
3. Verify worktree binding if applicable

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