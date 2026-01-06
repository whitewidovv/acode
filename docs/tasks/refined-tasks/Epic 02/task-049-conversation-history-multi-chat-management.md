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
- ASM-018: SQLite FTS5 extension available for full-text search
- ASM-019: Network availability can be detected reliably
- ASM-020: Timestamps are monotonically increasing (no clock skew)

---

## Security Considerations

### Threat 1: SQL Injection in Search Queries

**Risk:** User-provided search terms could contain malicious SQL that escapes FTS5 query syntax and executes arbitrary commands, potentially reading/modifying chat data, extracting secrets, or corrupting the database.

**Attack Scenario:** Attacker provides search term: `" OR 1=1; DROP TABLE chats; --` hoping to break out of the FTS MATCH clause.

**Mitigation:**

```csharp
namespace AgenticCoder.Infrastructure.Conversation;

public sealed class SafeSearchQueryBuilder
{
    private static readonly HashSet<char> ForbiddenChars = new()
    {
        ';', '\'', '"', '\\', '\0', '\n', '\r'
    };

    public static string BuildSafeQuery(string userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput))
            throw new ArgumentException("Search term cannot be empty", nameof(userInput));

        // Step 1: Validate no forbidden characters
        if (userInput.Any(c => ForbiddenChars.Contains(c)))
            throw new SecurityException($"Search term contains forbidden characters: {userInput}");

        // Step 2: Escape FTS5 special characters
        var escaped = userInput
            .Replace("*", "\\*")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("[", "\\[")
            .Replace("]", "\\]")
            .Replace("{", "\\{")
            .Replace("}", "\\}")
            .Replace("^", "\\^")
            .Replace("$", "\\$");

        // Step 3: Use parameterized queries ONLY
        return escaped;
    }
}

public sealed class ChatSearchService
{
    private readonly IDbConnection _db;

    public async Task<List<Message>> SearchAsync(string searchTerm, ChatId? chatFilter = null)
    {
        var safeTerm = SafeSearchQueryBuilder.BuildSafeQuery(searchTerm);

        // ALWAYS use parameterized queries
        var sql = @"
            SELECT m.* FROM messages m
            JOIN message_fts fts ON m.id = fts.message_id
            WHERE fts.content MATCH @searchTerm";

        if (chatFilter is not null)
            sql += " AND m.chat_id = @chatId";

        sql += " ORDER BY m.created_at DESC LIMIT 50";

        var parameters = new { searchTerm = safeTerm, chatId = chatFilter?.Value };
        return (await _db.QueryAsync<Message>(sql, parameters)).AsList();
    }
}
```

### Threat 2: Unauthorized Chat Access Across Workspaces

**Risk:** If multiple workspaces share the same PostgreSQL instance, one workspace could read/modify chats from another workspace, leaking sensitive project information or sabotaging team conversations.

**Attack Scenario:** Attacker configures their workspace to use victim's PostgreSQL connection string, gains full read/write access to all victim chats.

**Mitigation:**

```csharp
namespace AgenticCoder.Domain.Conversation;

public sealed record WorkspaceId(Guid Value)
{
    public static WorkspaceId FromEnvironment()
    {
        // Workspace ID derived from git repo root + machine ID
        var repoRoot = GitHelper.GetRepositoryRoot();
        var machineId = Environment.MachineName;
        var combined = $"{repoRoot}:{machineId}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return new WorkspaceId(new Guid(hash.Take(16).ToArray()));
    }
}

public sealed class WorkspaceScopedChatRepository : IChatRepository
{
    private readonly IDbConnection _db;
    private readonly WorkspaceId _workspaceId;

    public WorkspaceScopedChatRepository(IDbConnection db, WorkspaceId workspaceId)
    {
        _db = db;
        _workspaceId = workspaceId;
    }

    public async Task<Chat?> GetByIdAsync(ChatId id)
    {
        // ALWAYS filter by workspace_id
        var sql = @"
            SELECT * FROM chats
            WHERE id = @id AND workspace_id = @workspaceId AND is_deleted = 0";

        return await _db.QuerySingleOrDefaultAsync<Chat>(sql, new
        {
            id = id.Value,
            workspaceId = _workspaceId.Value
        });
    }

    public async Task<ChatId> CreateAsync(Chat chat)
    {
        // ALWAYS insert with workspace_id
        chat.WorkspaceId = _workspaceId; // Enforce workspace scope

        var sql = @"
            INSERT INTO chats (id, workspace_id, title, created_at, updated_at)
            VALUES (@Id, @WorkspaceId, @Title, @CreatedAt, @UpdatedAt)";

        await _db.ExecuteAsync(sql, chat);
        return chat.Id;
    }

    // All other methods similarly scoped
}
```

### Threat 3: Secret Leakage in Remote Sync

**Risk:** API keys, passwords, tokens embedded in chat messages sync to PostgreSQL in plain text, where they could be exposed via database backups, logs, or unauthorized access.

**Attack Scenario:** Developer pastes AWS access key in chat. Key syncs to PostgreSQL. Database backup is stolen. Attacker uses key to access AWS resources.

**Mitigation:**

```csharp
namespace AgenticCoder.Application.Conversation;

public sealed class SecretRedactor
{
    private static readonly Regex[] SecretPatterns =
    {
        new Regex(@"(sk|pk)_live_[a-zA-Z0-9]{24,}", RegexOptions.Compiled), // Stripe keys
        new Regex(@"AKIA[0-9A-Z]{16}", RegexOptions.Compiled), // AWS access keys
        new Regex(@"ghp_[a-zA-Z0-9]{36}", RegexOptions.Compiled), // GitHub tokens
        new Regex(@"xox[baprs]-[a-zA-Z0-9-]{10,72}", RegexOptions.Compiled), // Slack tokens
        new Regex(@"Bearer [a-zA-Z0-9_\-\.]+", RegexOptions.Compiled), // Bearer tokens
        new Regex(@"password[\"']?\s*[:=]\s*[\"']([^\"']{8,})[\"']", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new Regex(@"api[_-]?key[\"']?\s*[:=]\s*[\"']([^\"']{16,})[\"']", RegexOptions.Compiled | RegexOptions.IgnoreCase)
    };

    public string Redact(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        var redacted = content;

        foreach (var pattern in SecretPatterns)
        {
            redacted = pattern.Replace(redacted, match =>
            {
                var secretType = DetermineSecretType(match.Value);
                return $"[REDACTED:{secretType}]";
            });
        }

        return redacted;
    }

    private static string DetermineSecretType(string secret)
    {
        if (secret.StartsWith("sk_")) return "API_KEY";
        if (secret.StartsWith("AKIA")) return "AWS_KEY";
        if (secret.StartsWith("ghp_")) return "GITHUB_TOKEN";
        if (secret.StartsWith("xox")) return "SLACK_TOKEN";
        if (secret.Contains("Bearer")) return "BEARER_TOKEN";
        if (secret.Contains("password", StringComparison.OrdinalIgnoreCase)) return "PASSWORD";
        return "SECRET";
    }
}

public sealed class SyncService
{
    private readonly SecretRedactor _redactor;

    public async Task SyncMessageAsync(Message message)
    {
        // Redact before sending to remote
        var redactedContent = _redactor.Redact(message.Content);
        var syncMessage = message with { Content = redactedContent };

        await _remote.InsertMessageAsync(syncMessage);
    }
}
```

### Threat 4: Chat Data Tampering via Direct Database Access

**Risk:** Attacker gains filesystem access to `.agent/chats.db` and modifies chat history—injecting fake messages, altering decisions, planting backdoors in code snippets.

**Attack Scenario:** Attacker modifies message: \"Use bcrypt for passwords\" → \"Use MD5 for passwords\", causing security vulnerabilities in implemented code.

**Mitigation:**

```csharp
namespace AgenticCoder.Domain.Conversation;

public sealed class MessageIntegrityGuard
{
    public static string ComputeChecksum(Message message)
    {
        // Compute SHA-256 hash of immutable message fields
        var data = $"{message.Id}|{message.RunId}|{message.Role}|{message.Content}|{message.CreatedAt:O}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(bytes);
    }

    public static bool VerifyIntegrity(Message message)
    {
        var expectedChecksum = ComputeChecksum(message);
        return message.Checksum == expectedChecksum;
    }
}

public sealed class Message
{
    public MessageId Id { get; init; }
    public RunId RunId { get; init; }
    public string Role { get; init; }
    public string Content { get; private set; } // Private setter prevents tampering
    public DateTimeOffset CreatedAt { get; init; }
    public string Checksum { get; private set; } // SHA-256 integrity hash

    public static Message Create(RunId runId, string role, string content)
    {
        var message = new Message
        {
            Id = MessageId.New(),
            RunId = runId,
            Role = role,
            Content = content,
            CreatedAt = DateTimeOffset.UtcNow
        };

        message.Checksum = MessageIntegrityGuard.ComputeChecksum(message);
        return message;
    }

    // Prevent modification after creation
    public void ValidateIntegrity()
    {
        if (!MessageIntegrityGuard.VerifyIntegrity(this))
            throw new SecurityException($"Message {Id} failed integrity check - possible tampering detected");
    }
}

public sealed class ChatRepository
{
    public async Task<Message> GetMessageAsync(MessageId id)
    {
        var message = await _db.QuerySingleAsync<Message>("SELECT * FROM messages WHERE id = @id", new { id });

        // ALWAYS verify integrity on read
        message.ValidateIntegrity();

        return message;
    }
}
```

### Threat 5: Denial of Service via Large Message Payloads

**Risk:** Attacker sends extremely large messages (multi-MB code files, binary data) that exhaust memory, slow queries, and degrade database performance for all operations.

**Attack Scenario:** Attacker repeatedly appends 10MB messages to chat, causing SQLite file to grow to gigabytes, search queries to timeout, and system to become unusable.

**Mitigation:**

```csharp
namespace AgenticCoder.Application.Conversation;

public sealed class MessageSizeValidator
{
    private const int MaxContentSizeBytes = 100 * 1024; // 100 KB
    private const int MaxToolCallSizeBytes = 50 * 1024; // 50 KB
    private const int MaxMessagesPerRun = 1000;

    public static void ValidateMessageSize(string content, string? toolCalls = null)
    {
        var contentSize = Encoding.UTF8.GetByteCount(content);

        if (contentSize > MaxContentSizeBytes)
            throw new ValidationException(
                $"Message content exceeds maximum size: {contentSize} bytes > {MaxContentSizeBytes} bytes. " +
                $"Consider truncating or using file attachments.");

        if (toolCalls is not null)
        {
            var toolCallSize = Encoding.UTF8.GetByteCount(toolCalls);
            if (toolCallSize > MaxToolCallSizeBytes)
                throw new ValidationException($"Tool calls exceed maximum size: {toolCallSize} bytes > {MaxToolCallSizeBytes} bytes");
        }
    }
}

public sealed class RunGuard
{
    private readonly IChatRepository _repo;

    public async Task<Message> AppendMessageAsync(RunId runId, string role, string content)
    {
        // Validate size before persisting
        MessageSizeValidator.ValidateMessageSize(content);

        // Prevent run from growing unbounded
        var messageCount = await _repo.GetMessageCountAsync(runId);
        if (messageCount >= MessageSizeValidator.MaxMessagesPerRun)
            throw new InvalidOperationException(
                $"Run {runId} has reached maximum message count ({MessageSizeValidator.MaxMessagesPerRun}). " +
                "Create a new run to continue.");

        var message = Message.Create(runId, role, content);
        await _repo.InsertMessageAsync(message);
        return message;
    }
}

// Database schema enforcement
public sealed class ChatDatabaseSchema
{
    public static void CreateTables(IDbConnection db)
    {
        db.Execute(@"
            CREATE TABLE IF NOT EXISTS messages (
                id TEXT PRIMARY KEY,
                run_id TEXT NOT NULL,
                role TEXT NOT NULL CHECK(role IN ('user', 'assistant', 'system', 'tool')),
                content TEXT NOT NULL CHECK(length(content) <= 102400), -- 100 KB limit
                tool_calls TEXT CHECK(tool_calls IS NULL OR length(tool_calls) <= 51200), -- 50 KB limit
                created_at TEXT NOT NULL,
                checksum TEXT NOT NULL
            )");
    }
}
```

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

### Export/Import

- FR-047: Export chat to JSON MUST work
- FR-048: Export chat to Markdown MUST work
- FR-049: Export MUST include all messages
- FR-050: Export MUST include metadata
- FR-051: Export MUST redact secrets
- FR-052: Export single chat MUST work
- FR-053: Export all chats MUST work
- FR-054: Export MUST be resumable if interrupted
- FR-055: Import from JSON MUST work
- FR-056: Import MUST validate schema
- FR-057: Import MUST handle duplicates

### Pagination

- FR-058: Message pagination MUST work
- FR-059: Page size MUST be configurable
- FR-060: Page size MUST default to 50
- FR-061: Offset-based pagination MUST work
- FR-062: Cursor-based pagination SHOULD work
- FR-063: Chat list pagination MUST work

### Sorting

- FR-064: Sort chats by updated_at MUST work
- FR-065: Sort chats by created_at MUST work
- FR-066: Sort chats by title MUST work
- FR-067: Sort chats by message_count MUST work
- FR-068: Sort direction (asc/desc) MUST work
- FR-069: Sort messages by timestamp MUST work

### Filtering

- FR-070: Filter chats by is_deleted MUST work
- FR-071: Filter chats by tags MUST work
- FR-072: Filter chats by date range MUST work
- FR-073: Filter chats by worktree MUST work
- FR-074: Filter messages by role MUST work
- FR-075: Filter messages by date range MUST work
- FR-076: Combined filters MUST use AND logic

### Tagging

- FR-077: Add tags to chat MUST work
- FR-078: Remove tags from chat MUST work
- FR-079: Replace all tags MUST work
- FR-080: List all unique tags MUST work
- FR-081: Tag autocomplete SHOULD work
- FR-082: Tags MUST be case-insensitive
- FR-083: Tags MUST be trimmed

### Metadata Management

- FR-084: Chat metadata MUST include message_count
- FR-085: Chat metadata MUST include last_message_at
- FR-086: Run metadata MUST include duration
- FR-087: Run metadata MUST include token_count
- FR-088: Run metadata MUST include model_used
- FR-089: Metadata MUST update atomically

### Audit Trail

- FR-090: All chat operations MUST be logged
- FR-091: Logs MUST include user_id (if available)
- FR-092: Logs MUST include timestamp
- FR-093: Logs MUST include operation type
- FR-094: Logs MUST be queryable
- FR-095: Purge operations MUST require reason

### Conflict Resolution

- FR-096: Sync conflicts MUST be detected
- FR-097: LWW strategy MUST be applied
- FR-098: Conflict metadata MUST be logged
- FR-099: Manual conflict resolution SHOULD be supported

### Batch Operations

- FR-100: Batch delete chats MUST work
- FR-101: Batch restore chats MUST work
- FR-102: Batch export chats MUST work
- FR-103: Batch tag chats MUST work
- FR-104: Batch operations MUST be atomic

### Performance Requirements

- FR-105: Lazy loading of messages MUST work
- FR-106: Message content MUST NOT load by default in list
- FR-107: Indexes MUST exist on chat_id, run_id, created_at
- FR-108: FTS index MUST be maintained automatically

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

### Maintainability

- NFR-017: Code coverage >= 80%
- NFR-018: All public APIs MUST have XML docs
- NFR-019: Database schema MUST be versioned
- NFR-020: Migrations MUST be reversible
- NFR-021: Error messages MUST be actionable

### Compatibility

- NFR-022: Windows, macOS, Linux MUST be supported
- NFR-023: SQLite >= 3.35 MUST be supported
- NFR-024: PostgreSQL >= 12 MUST be supported
- NFR-025: Migration path from v1 to v2 schema MUST exist
- NFR-026: Backward-compatible JSON export format

### Observability

- NFR-027: All operations MUST emit structured logs
- NFR-028: Log level MUST be configurable
- NFR-029: Metrics MUST be exportable (Prometheus format)
- NFR-030: Trace IDs MUST propagate across operations
- NFR-031: Database query performance MUST be logged

### Deployment

- NFR-032: Zero-downtime schema migrations
- NFR-033: Rollback MUST preserve data integrity
- NFR-034: Feature flags for gradual rollout
- NFR-035: Canary deployment support

### Error Handling

- NFR-036: Graceful degradation when PostgreSQL unavailable
- NFR-037: Retry logic for transient failures
- NFR-038: Circuit breaker for remote sync
- NFR-039: User-friendly error messages
- NFR-040: Automatic recovery from corrupt indexes

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

### Export/Import

- [ ] AC-031: Export single chat to JSON works
- [ ] AC-032: Export single chat to Markdown works
- [ ] AC-033: Export all chats works
- [ ] AC-034: Exported JSON is valid and parseable
- [ ] AC-035: Export includes all message content
- [ ] AC-036: Export includes metadata (title, tags, timestamps)
- [ ] AC-037: Export redacts secrets automatically
- [ ] AC-038: Import from JSON validates schema
- [ ] AC-039: Import handles duplicate chat IDs gracefully
- [ ] AC-040: Import preserves all metadata

### Pagination

- [ ] AC-041: Chat list pagination works with --limit and --offset
- [ ] AC-042: Message pagination defaults to last 50 messages
- [ ] AC-043: Page size is configurable (10-1000)
- [ ] AC-044: Cursor-based pagination works for large result sets
- [ ] AC-045: Pagination performance < 100ms for 10k messages

### Sorting

- [ ] AC-046: Sort chats by updated_at (default)
- [ ] AC-047: Sort chats by created_at
- [ ] AC-048: Sort chats by title (alphabetical)
- [ ] AC-049: Sort chats by message_count
- [ ] AC-050: Sort direction (asc/desc) works for all fields
- [ ] AC-051: Sort messages by timestamp (chronological order)

### Filtering

- [ ] AC-052: Filter chats by is_deleted flag
- [ ] AC-053: Filter chats by single tag
- [ ] AC-054: Filter chats by multiple tags (AND logic)
- [ ] AC-055: Filter chats by date range (created_at or updated_at)
- [ ] AC-056: Filter chats by worktree binding
- [ ] AC-057: Filter messages by role (user/assistant/system/tool)
- [ ] AC-058: Combined filters use AND logic correctly

### Tagging

- [ ] AC-059: Add tags to chat works
- [ ] AC-060: Remove tags from chat works
- [ ] AC-061: Replace all tags atomically
- [ ] AC-062: List all unique tags across workspace
- [ ] AC-063: Tags are case-insensitive (\"Bug\" == \"bug\")
- [ ] AC-064: Tags are trimmed of whitespace
- [ ] AC-065: Duplicate tags are prevented

### Metadata Management

- [ ] AC-066: Chat message_count updates on message append
- [ ] AC-067: Chat last_message_at updates on message append
- [ ] AC-068: Run duration is calculated correctly (end - start)
- [ ] AC-069: Run token_count tracks total tokens used
- [ ] AC-070: Run model_used records model identifier
- [ ] AC-071: Metadata updates are atomic (no partial updates)

### Security

- [ ] AC-072: SQL injection attacks are blocked in search queries
- [ ] AC-073: Workspace isolation prevents cross-workspace access
- [ ] AC-074: Secrets are redacted before remote sync
- [ ] AC-075: Message integrity checksums detect tampering
- [ ] AC-076: Message size limits enforce max 100KB content
- [ ] AC-077: Run message limits enforce max 1000 messages per run

### Performance

- [ ] AC-078: Chat creation completes in < 50ms
- [ ] AC-079: Message append completes in < 10ms
- [ ] AC-080: Chat load (with 50 messages) completes in < 100ms
- [ ] AC-081: Search across 10k messages completes in < 500ms
- [ ] AC-082: Chat switching completes in < 50ms
- [ ] AC-083: Sync of 50 messages completes in < 1000ms

### Error Handling

- [ ] AC-084: Chat not found returns clear error message
- [ ] AC-085: Database locked error triggers retry with exponential backoff
- [ ] AC-086: Network unavailable during sync queues operations for later
- [ ] AC-087: Invalid chat ID format returns validation error
- [ ] AC-088: Corrupt database triggers automatic recovery attempt

###Offline & Sync

- [ ] AC-089: All operations work without network connection
- [ ] AC-090: Changes queue in outbox table when offline
- [ ] AC-091: Sync automatically starts when network is available
- [ ] AC-092: Sync conflicts are detected and logged
- [ ] AC-093: Last-Write-Wins resolution applies to conflicts
- [ ] AC-094: Sync batch size is configurable (default 50)
- [ ] AC-095: Sync retries on failure with exponential backoff

### Audit Trail

- [ ] AC-096: All chat operations are logged with timestamps
- [ ] AC-097: Purge operations log reason for deletion
- [ ] AC-098: Sync operations log conflict count and resolution
- [ ] AC-099: Audit logs are queryable via CLI

### Batch Operations

- [ ] AC-100: Batch delete multiple chats atomically
- [ ] AC-101: Batch restore multiple chats atomically
- [ ] AC-102: Batch export multiple chats works
- [ ] AC-103: Batch tag operations apply to all specified chats
- [ ] AC-104: Batch operation failure rolls back all changes

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

### Search Optimization

- **BP-013: Use specific search terms** - Avoid overly broad queries that return thousands of results
- **BP-014: Filter before searching** - Narrow scope with chat/date filters for faster results
- **BP-015: Rebuild FTS index periodically** - Run `PRAGMA optimize` monthly to maintain index performance

### Sync Strategy

- **BP-016: Monitor sync status** - Check `acode sync status` regularly to ensure changes are propagating
- **BP-017: Batch offline work** - Group related changes in one session to reduce sync overhead
- **BP-018: Resolve conflicts promptly** - Review sync logs and address conflicts quickly to prevent data divergence

### Security

- **BP-019: Redact before sharing** - Always export with `--redact` flag when sharing chat transcripts
- **BP-020: Review secrets in history** - Periodically search for accidentally committed secrets using `acode chat search "api[_-]?key"`
- **BP-021: Limit remote sync scope** - Only sync chats that need multi-device access; keep sensitive chats local-only

### Error Recovery

- **BP-022: Regular backups** - Export critical chats weekly: `acode chat export --all > backup.json`
- **BP-023: Test restore process** - Periodically verify backups can be imported successfully
- **BP-024: Monitor disk space** - SQLite database can grow large; ensure adequate free space in `.agent/` directory

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

### Sync Conflicts Not Resolving

**Symptom:** `acode sync status` shows persistent conflicts that won't clear.

**Cause:** Multiple machines modified the same chat while offline, and LWW conflict resolution failed due to identical timestamps or corrupt sync metadata.

**Solution:**
1. Check sync logs: `acode sync logs --tail 100`
2. Identify conflicting chat ID from logs
3. Export both local and remote versions:
   ```bash
   acode chat export <chat_id> > local_version.json
   acode chat export --remote <chat_id> > remote_version.json
   ```
4. Manually merge the versions if both contain important changes
5. Force resolution by choosing a winner:
   ```bash
   # Keep local version
   acode sync resolve <chat_id> --prefer-local
   
   # Or keep remote version
   acode sync resolve <chat_id> --prefer-remote
   ```
6. If resolution fails, purge and recreate:
   ```bash
   acode chat export <chat_id> > backup.json
   acode chat purge <chat_id> --confirm
   acode chat import backup.json
   ```

---

## Testing Requirements

### Unit Tests - Complete C# Implementations

```csharp
// Tests/Unit/Conversation/ChatTests.cs
using Xunit;
using AgenticCoder.Domain.Conversation;

public sealed class ChatTests
{
    [Fact]
    public void Should_Create_Chat_With_Valid_Properties()
    {
        // Arrange
        var title = "Feature: User Authentication";
        var worktreeId = new WorktreeId(Guid.NewGuid());

        // Act
        var chat = Chat.Create(title, worktreeId);

        // Assert
        Assert.NotNull(chat.Id);
        Assert.Equal(title, chat.Title);
        Assert.Equal(worktreeId, chat.WorktreeBinding);
        Assert.False(chat.IsDeleted);
        Assert.Empty(chat.Tags);
        Assert.True(chat.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Equal(chat.CreatedAt, chat.UpdatedAt);
    }

    [Fact]
    public void Should_Update_Title()
    {
        // Arrange
        var chat = Chat.Create("Old Title");
        var newTitle = "New Title";
        var originalUpdatedAt = chat.UpdatedAt;
        Thread.Sleep(10); // Ensure timestamp changes

        // Act
        chat.Rename(newTitle);

        // Assert
        Assert.Equal(newTitle, chat.Title);
        Assert.True(chat.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public void Should_Soft_Delete_Chat()
    {
        // Arrange
        var chat = Chat.Create("Test Chat");

        // Act
        chat.SoftDelete();

        // Assert
        Assert.True(chat.IsDeleted);
    }

    [Fact]
    public void Should_Restore_Soft_Deleted_Chat()
    {
        // Arrange
        var chat = Chat.Create("Test Chat");
        chat.SoftDelete();

        // Act
        chat.Restore();

        // Assert
        Assert.False(chat.IsDeleted);
    }

    [Fact]
    public void Should_Add_Tags()
    {
        // Arrange
        var chat = Chat.Create("Test Chat");

        // Act
        chat.AddTags("feature", "backend", "api");

        // Assert
        Assert.Equal(3, chat.Tags.Count);
        Assert.Contains("feature", chat.Tags);
        Assert.Contains("backend", chat.Tags);
        Assert.Contains("api", chat.Tags);
    }

    [Fact]
    public void Should_Remove_Tags()
    {
        // Arrange
        var chat = Chat.Create("Test Chat");
        chat.AddTags("feature", "backend", "api");

        // Act
        chat.RemoveTag("backend");

        // Assert
        Assert.Equal(2, chat.Tags.Count);
        Assert.DoesNotContain("backend", chat.Tags);
    }
}

// Tests/Unit/Conversation/RunTests.cs
public sealed class RunTests
{
    [Fact]
    public void Should_Create_Run_With_Valid_Properties()
    {
        // Arrange
        var chatId = new ChatId(Ulid.NewUlid());

        // Act
        var run = Run.Create(chatId);

        // Assert
        Assert.NotNull(run.Id);
        Assert.Equal(chatId, run.ChatId);
        Assert.Equal(RunStatus.Pending, run.Status);
        Assert.Equal(0, run.TokensUsed);
        Assert.Null(run.CompletedAt);
    }

    [Fact]
    public void Should_Track_Status_Transitions()
    {
        // Arrange
        var run = Run.Create(new ChatId(Ulid.NewUlid()));

        // Act & Assert - Pending → InProgress
        run.Start();
        Assert.Equal(RunStatus.InProgress, run.Status);
        Assert.NotNull(run.StartedAt);

        // Act & Assert - InProgress → Completed
        run.Complete(500, "gpt-4");
        Assert.Equal(RunStatus.Completed, run.Status);
        Assert.NotNull(run.CompletedAt);
        Assert.Equal(500, run.TokensUsed);
        Assert.Equal("gpt-4", run.ModelUsed);
    }

    [Fact]
    public void Should_Calculate_Duration()
    {
        // Arrange
        var run = Run.Create(new ChatId(Ulid.NewUlid()));
        run.Start();
        Thread.Sleep(100);

        // Act
        run.Complete(100, "gpt-4");
        var duration = run.Duration;

        // Assert
        Assert.NotNull(duration);
        Assert.True(duration.Value.TotalMilliseconds >= 100);
    }
}

// Tests/Unit/Conversation/MessageTests.cs
public sealed class MessageTests
{
    [Fact]
    public void Should_Store_Message_With_Valid_Properties()
    {
        // Arrange
        var runId = new RunId(Ulid.NewUlid());
        var role = "user";
        var content = "Hello, world!";

        // Act
        var message = Message.Create(runId, role, content);

        // Assert
        Assert.NotNull(message.Id);
        Assert.Equal(runId, message.RunId);
        Assert.Equal(role, message.Role);
        Assert.Equal(content, message.Content);
        Assert.True(message.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.NotNull(message.Checksum);
    }

    [Fact]
    public void Should_Store_Tool_Calls()
    {
        // Arrange
        var runId = new RunId(Ulid.NewUlid());
        var toolCalls = "[{\"name\": \"file_read\", \"args\": {\"path\": \"test.cs\"}}]";

        // Act
        var message = Message.Create(runId, "assistant", "I'll read the file", toolCalls);

        // Assert
        Assert.Equal(toolCalls, message.ToolCalls);
    }

    [Fact]
    public void Should_Validate_Integrity_On_Creation()
    {
        // Arrange & Act
        var message = Message.Create(new RunId(Ulid.NewUlid()), "user", "Test content");

        // Assert - Should not throw
        message.ValidateIntegrity();
    }

    [Fact]
    public void Should_Detect_Tampered_Content()
    {
        // Arrange
        var message = Message.Create(new RunId(Ulid.NewUlid()), "user", "Original content");
        var originalChecksum = message.Checksum;

        // Act - Simulate tampering via reflection
        typeof(Message).GetProperty("Content")!.SetValue(message, "Tampered content");

        // Assert
        Assert.Throws<SecurityException>(() => message.ValidateIntegrity());
    }
}

// Tests/Unit/Conversation/SecretRedactorTests.cs
public sealed class SecretRedactorTests
{
    private readonly SecretRedactor _redactor = new();

    [Theory]
    [InlineData("My API key is sk_live_abc123def456ghi789", "[REDACTED:API_KEY]")]
    [InlineData("AWS key AKIAIOSFODNN7EXAMPLE", "[REDACTED:AWS_KEY]")]
    [InlineData("Token: ghp_1234567890abcdefghijklmnopqrstuvwx", "[REDACTED:GITHUB_TOKEN]")]
    public void Should_Redact_Secrets(string input, string expected)
    {
        // Act
        var result = _redactor.Redact(input);

        // Assert
        Assert.Contains(expected, result);
        Assert.DoesNotContain("sk_live", result);
        Assert.DoesNotContain("AKIA", result);
        Assert.DoesNotContain("ghp_", result);
    }

    [Fact]
    public void Should_Preserve_Non_Secret_Content()
    {
        // Arrange
        var input = "This is normal text with no secrets";

        // Act
        var result = _redactor.Redact(input);

        // Assert
        Assert.Equal(input, result);
    }
}
```

### Integration Tests - Complete C# Implementations

```csharp
// Tests/Integration/Conversation/StorageTests.cs
using Xunit;
using Microsoft.Data.Sqlite;
using AgenticCoder.Infrastructure.Persistence;

public sealed class StorageTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ChatRepository _repository;

    public StorageTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        ChatDatabaseSchema.CreateTables(_connection);
        _repository = new ChatRepository(_connection, WorkspaceId.FromEnvironment());
    }

    [Fact]
    public async Task Should_Persist_To_SQLite()
    {
        // Arrange
        var chat = Chat.Create("Test Chat");

        // Act
        await _repository.CreateAsync(chat);
        var retrieved = await _repository.GetByIdAsync(chat.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(chat.Id, retrieved.Id);
        Assert.Equal(chat.Title, retrieved.Title);
        Assert.Equal(chat.IsDeleted, retrieved.IsDeleted);
    }

    [Fact]
    public async Task Should_Handle_Soft_Delete()
    {
        // Arrange
        var chat = Chat.Create("Test Chat");
        await _repository.CreateAsync(chat);

        // Act
        chat.SoftDelete();
        await _repository.UpdateAsync(chat);
        var retrieved = await _repository.GetByIdAsync(chat.Id);

        // Assert
        Assert.Null(retrieved); // Soft-deleted chats not returned by default

        var retrievedWithDeleted = await _repository.GetByIdAsync(chat.Id, includeDeleted: true);
        Assert.NotNull(retrievedWithDeleted);
        Assert.True(retrievedWithDeleted.IsDeleted);
    }

    [Fact]
    public async Task Should_Filter_By_Workspace()
    {
        // Arrange
        var chat1 = Chat.Create("Workspace 1 Chat");
        chat1.WorkspaceId = new WorkspaceId(Guid.NewGuid());
        await _repository.CreateAsync(chat1);

        var chat2 = Chat.Create("Workspace 2 Chat");
        chat2.WorkspaceId = new WorkspaceId(Guid.NewGuid());
        await _repository.CreateAsync(chat2);

        // Act
        var myChats = await _repository.ListAsync();

        // Assert
        Assert.Single(myChats); // Only chat from current workspace
        Assert.Equal(chat1.Id, myChats[0].Id);
    }

    public void Dispose() => _connection.Dispose();
}

// Tests/Integration/Conversation/SearchTests.cs
public sealed class SearchTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ChatSearchService _searchService;

    public SearchTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        ChatDatabaseSchema.CreateTables(_connection);
        _searchService = new ChatSearchService(_connection);
    }

    [Fact]
    public async Task Should_Search_Messages_By_Content()
    {
        // Arrange
        var chatId = new ChatId(Ulid.NewUlid());
        var runId = new RunId(Ulid.NewUlid());
        var message1 = Message.Create(runId, "user", "How do I implement authentication?");
        var message2 = Message.Create(runId, "assistant", "Use JWT tokens for authentication");
        var message3 = Message.Create(runId, "user", "What about authorization?");

        await InsertMessage(message1, chatId);
        await InsertMessage(message2, chatId);
        await InsertMessage(message3, chatId);

        // Act
        var results = await _searchService.SearchAsync("authentication");

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, m => m.Id == message1.Id);
        Assert.Contains(results, m => m.Id == message2.Id);
    }

    [Fact]
    public async Task Should_Filter_Search_By_Chat()
    {
        // Arrange
        var chat1Id = new ChatId(Ulid.NewUlid());
        var chat2Id = new ChatId(Ulid.NewUlid());
        var message1 = Message.Create(new RunId(Ulid.NewUlid()), "user", "authentication in chat 1");
        var message2 = Message.Create(new RunId(Ulid.NewUlid()), "user", "authentication in chat 2");

        await InsertMessage(message1, chat1Id);
        await InsertMessage(message2, chat2Id);

        // Act
        var results = await _searchService.SearchAsync("authentication", chatFilter: chat1Id);

        // Assert
        Assert.Single(results);
        Assert.Equal(message1.Id, results[0].Id);
    }

    private async Task InsertMessage(Message message, ChatId chatId)
    {
        await _connection.ExecuteAsync(
            "INSERT INTO messages (id, chat_id, run_id, role, content, created_at, checksum) VALUES (@Id, @ChatId, @RunId, @Role, @Content, @CreatedAt, @Checksum)",
            new { message.Id, ChatId = chatId, message.RunId, message.Role, message.Content, message.CreatedAt, message.Checksum });
        await _connection.ExecuteAsync(
            "INSERT INTO message_fts (message_id, content) VALUES (@Id, @Content)",
            new { message.Id, message.Content });
    }

    public void Dispose() => _connection.Dispose();
}
```

### E2E Tests - Complete C# Implementations

```csharp
// Tests/E2E/Conversation/MultiChatE2ETests.cs
using Xunit;
using AgenticCoder.Cli;

public sealed class MultiChatE2ETests
{
    [Fact]
    public async Task Should_Create_Multiple_Chats_And_Switch_Between_Them()
    {
        // Arrange
        var cli = new AcodeCli();

        // Act - Create chat 1
        var result1 = await cli.ExecuteAsync("chat new \"Feature: Auth\"");
        var chat1Id = ExtractChatId(result1.Output);

        // Act - Create chat 2
        var result2 = await cli.ExecuteAsync("chat new \"Bug: Memory Leak\"");
        var chat2Id = ExtractChatId(result2.Output);

        // Act - List chats
        var listResult = await cli.ExecuteAsync("chat list");

        // Assert
        Assert.Contains(chat1Id, listResult.Output);
        Assert.Contains(chat2Id, listResult.Output);
        Assert.Contains("Feature: Auth", listResult.Output);
        Assert.Contains("Bug: Memory Leak", listResult.Output);
    }

    [Fact]
    public async Task Should_Maintain_Context_Per_Chat()
    {
        // Arrange
        var cli = new AcodeCli();
        var result1 = await cli.ExecuteAsync("chat new \"Chat 1\"");
        var chat1Id = ExtractChatId(result1.Output);
        var result2 = await cli.ExecuteAsync("chat new \"Chat 2\"");
        var chat2Id = ExtractChatId(result2.Output);

        // Act - Add message to chat 1
        await cli.ExecuteAsync($"--chat {chat1Id} run \"Message for chat 1\"");

        // Act - Add message to chat 2
        await cli.ExecuteAsync($"--chat {chat2Id} run \"Message for chat 2\"");

        // Act - Show chat 1
        var show1 = await cli.ExecuteAsync($"chat show {chat1Id}");

        // Assert - Chat 1 should only have its message
        Assert.Contains("Message for chat 1", show1.Output);
        Assert.DoesNotContain("Message for chat 2", show1.Output);
    }

    private static string ExtractChatId(string output)
    {
        var match = Regex.Match(output, @"chat_[a-z0-9]+");
        return match.Success ? match.Value : throw new Exception("Chat ID not found in output");
    }
}
```

### Performance Benchmarks

| Benchmark | Target | Maximum | Measurement Method |
|-----------|--------|---------|-------------------|
| Chat create | 25ms | 50ms | `BenchmarkDotNet` with 1000 iterations |
| Message append | 5ms | 10ms | `BenchmarkDotNet` with 10000 iterations |
| Chat load (50 msgs) | 50ms | 100ms | `BenchmarkDotNet` with 100 iterations |
| Search 10k messages | 250ms | 500ms | `BenchmarkDotNet` with FTS5 index |

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

### Scenario 7: Export and Import

1. Create a chat with several messages:
   ```bash
   $ acode chat new "Export Test"
   $ acode run "First message"
   $ acode run "Second message"
   ```
2. Export the chat:
   ```bash
   $ acode chat export <chat_id> > exported_chat.json
   ```
3. Verify JSON file exists and is valid:
   ```bash
   $ cat exported_chat.json | jq '.title'
   "Export Test"
   ```
4. Delete the chat:
   ```bash
   $ acode chat purge <chat_id> --confirm
   ```
5. Import the chat back:
   ```bash
   $ acode chat import exported_chat.json
   ```
6. Verify chat and messages are restored:
   ```bash
   $ acode chat show <chat_id>
   # Should display "First message" and "Second message"
   ```

### Scenario 8: Filtering and Sorting

1. Create multiple chats with different properties:
   ```bash
   $ acode chat new "Bug Fix" --tags bug critical
   $ acode chat new "Feature Work" --tags feature
   $ acode chat new "Refactor" --tags refactor
   ```
2. Filter by tag:
   ```bash
   $ acode chat list --tag bug
   # Should show only "Bug Fix"
   ```
3. Sort by title:
   ```bash
   $ acode chat list --sort title
   # Should show: Bug Fix, Feature Work, Refactor (alphabetical)
   ```
4. Combine filter and sort:
   ```bash
   $ acode chat list --tag feature --sort updated_at --desc
   ```

### Scenario 9: Search with Filters

1. Create chat and add multiple messages:
   ```bash
   $ acode chat new "Search Test"
   $ acode run "Implement authentication with JWT"
   $ acode run "Add authorization middleware"
   $ acode run "Configure database connection"
   ```
2. Search across all chats:
   ```bash
   $ acode chat search "authentication"
   # Should find "Implement authentication with JWT"
   ```
3. Search within specific chat:
   ```bash
   $ acode chat search "auth" --chat <chat_id>
   # Should find both authentication and authorization messages
   ```
4. Search with date filter:
   ```bash
   $ acode chat search "database" --since 2024-01-01
   # Should find "Configure database connection"
   ```

### Scenario 10: Performance Validation

1. Create a chat with many messages:
   ```bash
   $ for i in {1..100}; do acode run "Message $i"; done
   ```
2. Time chat loading:
   ```bash
   $ time acode chat show <chat_id>
   # Should complete in < 100ms
   ```
3. Time search across large history:
   ```bash
   $ time acode chat search "Message 50"
   # Should complete in < 500ms
   ```
4. Verify pagination works:
   ```bash
   $ acode chat show <chat_id> --limit 20 --offset 40
   # Should show messages 41-60
   ```

---

## Implementation Prompt

### Complete Domain Entities

```csharp
// src/AgenticCoder.Domain/Conversation/ChatId.cs
namespace AgenticCoder.Domain.Conversation;

public sealed record ChatId(Ulid Value)
{
    public static ChatId New() => new(Ulid.NewUlid());
    public static ChatId Parse(string value) => new(Ulid.Parse(value));
    public override string ToString() => Value.ToString();
}

// src/AgenticCoder.Domain/Conversation/RunId.cs
public sealed record RunId(Ulid Value)
{
    public static RunId New() => new(Ulid.NewUlid());
    public override string ToString() => Value.ToString();
}

// src/AgenticCoder.Domain/Conversation/MessageId.cs
public sealed record MessageId(Ulid Value)
{
    public static MessageId New() => new(Ulid.NewUlid());
    public override string ToString() => Value.ToString();
}

// src/AgenticCoder.Domain/Conversation/WorkspaceId.cs
public sealed record WorkspaceId(Guid Value)
{
    public static WorkspaceId FromEnvironment()
    {
        var repoRoot = GitHelper.GetRepositoryRoot();
        var machineId = Environment.MachineName;
        var combined = $"{repoRoot}:{machineId}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return new WorkspaceId(new Guid(hash.Take(16).ToArray()));
    }
}

// src/AgenticCoder.Domain/Conversation/Chat.cs
public sealed class Chat : AggregateRoot<ChatId>
{
    private readonly List<string> _tags = new();

    public string Title { get; private set; }
    public IReadOnlyList<string> Tags => _tags.AsReadOnly();
    public WorktreeId? WorktreeBinding { get; private set; }
    public WorkspaceId WorkspaceId { get; internal set; }
    public bool IsDeleted { get; private set; }
    public int MessageCount { get; private set; }
    public DateTimeOffset? LastMessageAt { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Chat() { } // EF Core

    public static Chat Create(string title, WorktreeId? worktree = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        return new Chat
        {
            Id = ChatId.New(),
            Title = title.Trim(),
            WorktreeBinding = worktree,
            IsDeleted = false,
            MessageCount = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Rename(string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
            throw new ArgumentException("Title cannot be empty", nameof(newTitle));

        Title = newTitle.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new ChatRenamedEvent(Id, newTitle));
    }

    public void AddTags(params string[] tags)
    {
        foreach (var tag in tags)
        {
            var normalized = tag.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(normalized) && !_tags.Contains(normalized))
            {
                _tags.Add(normalized);
            }
        }
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RemoveTag(string tag)
    {
        var normalized = tag.Trim().ToLowerInvariant();
        _tags.Remove(normalized);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ReplaceTags(params string[] tags)
    {
        _tags.Clear();
        AddTags(tags);
    }

    public void BindToWorktree(WorktreeId worktreeId)
    {
        WorktreeBinding = worktreeId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UnbindFromWorktree()
    {
        WorktreeBinding = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new ChatDeletedEvent(Id));
    }

    public void Restore()
    {
        IsDeleted = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new ChatRestoredEvent(Id));
    }

    public void IncrementMessageCount()
    {
        MessageCount++;
        LastMessageAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

// src/AgenticCoder.Domain/Conversation/Run.cs
public sealed class Run : Entity<RunId>
{
    public ChatId ChatId { get; }
    public RunStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int TokensUsed { get; private set; }
    public string? ModelUsed { get; private set; }
    public int ExitCode { get; private set; }

    public TimeSpan? Duration => CompletedAt.HasValue && StartedAt.HasValue
        ? CompletedAt.Value - StartedAt.Value
        : null;

    private Run() { } // EF Core

    public static Run Create(ChatId chatId)
    {
        return new Run
        {
            Id = RunId.New(),
            ChatId = chatId,
            Status = RunStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            TokensUsed = 0,
            ExitCode = -1
        };
    }

    public void Start()
    {
        if (Status != RunStatus.Pending)
            throw new InvalidOperationException($"Cannot start run in {Status} status");

        Status = RunStatus.InProgress;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void Complete(int tokensUsed, string modelUsed, int exitCode = 0)
    {
        if (Status != RunStatus.InProgress)
            throw new InvalidOperationException($"Cannot complete run in {Status} status");

        Status = RunStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        TokensUsed = tokensUsed;
        ModelUsed = modelUsed;
        ExitCode = exitCode;
    }

    public void Fail(string errorMessage, int exitCode = 1)
    {
        Status = RunStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
        ExitCode = exitCode;
    }

    public void Cancel()
    {
        if (Status == RunStatus.Completed || Status == RunStatus.Failed)
            throw new InvalidOperationException($"Cannot cancel run in {Status} status");

        Status = RunStatus.Cancelled;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}

public enum RunStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}

// src/AgenticCoder.Domain/Conversation/Message.cs
public sealed class Message : Entity<MessageId>
{
    public RunId RunId { get; }
    public string Role { get; } // user, assistant, system, tool
    public string Content { get; private set; }
    public string? ToolCalls { get; }
    public string? ToolResult { get; }
    public DateTimeOffset CreatedAt { get; }
    public string Checksum { get; private set; }

    private Message() { } // EF Core

    public static Message Create(RunId runId, string role, string content, string? toolCalls = null, string? toolResult = null)
    {
        ValidateRole(role);
        ValidateContent(content);
        ValidateSize(content, toolCalls);

        var message = new Message
        {
            Id = MessageId.New(),
            RunId = runId,
            Role = role,
            Content = content,
            ToolCalls = toolCalls,
            ToolResult = toolResult,
            CreatedAt = DateTimeOffset.UtcNow
        };

        message.Checksum = MessageIntegrityGuard.ComputeChecksum(message);
        return message;
    }

    public void ValidateIntegrity()
    {
        if (!MessageIntegrityGuard.VerifyIntegrity(this))
            throw new SecurityException($"Message {Id} failed integrity check - possible tampering detected");
    }

    private static void ValidateRole(string role)
    {
        var validRoles = new[] { "user", "assistant", "system", "tool" };
        if (!validRoles.Contains(role))
            throw new ArgumentException($"Invalid role: {role}. Must be one of: {string.Join(\", \", validRoles)}");
    }

    private static void ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Message content cannot be empty", nameof(content));
    }

    private static void ValidateSize(string content, string? toolCalls)
    {
        const int MaxContentSize = 100 * 1024; // 100 KB
        const int MaxToolCallSize = 50 * 1024; // 50 KB

        var contentSize = Encoding.UTF8.GetByteCount(content);
        if (contentSize > MaxContentSize)
            throw new ValidationException($"Message content exceeds maximum size: {contentSize} bytes > {MaxContentSize} bytes");

        if (toolCalls is not null)
        {
            var toolCallSize = Encoding.UTF8.GetByteCount(toolCalls);
            if (toolCallSize > MaxToolCallSize)
                throw new ValidationException($"Tool calls exceed maximum size: {toolCallSize} bytes > {MaxToolCallSize} bytes");
        }
    }
}

public static class MessageIntegrityGuard
{
    public static string ComputeChecksum(Message message)
    {
        var data = $"{message.Id}|{message.RunId}|{message.Role}|{message.Content}|{message.CreatedAt:O}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(bytes);
    }

    public static bool VerifyIntegrity(Message message)
    {
        var expectedChecksum = ComputeChecksum(message);
        return message.Checksum == expectedChecksum;
    }
}
```

### Repository Interfaces

```csharp
// src/AgenticCoder.Application/Conversation/IChatRepository.cs
namespace AgenticCoder.Application.Conversation;

public interface IChatRepository
{
    Task<Chat?> GetByIdAsync(ChatId id, bool includeDeleted = false, CancellationToken ct = default);
    Task<List<Chat>> ListAsync(ChatFilter? filter = null, ChatSort? sort = null, Pagination? pagination = null, CancellationToken ct = default);
    Task<ChatId> CreateAsync(Chat chat, CancellationToken ct = default);
    Task UpdateAsync(Chat chat, CancellationToken ct = default);
    Task DeleteAsync(ChatId id, CancellationToken ct = default); // Hard delete (purge)
    Task<List<string>> GetAllTagsAsync(CancellationToken ct = default);
    Task<int> GetMessageCountAsync(ChatId id, CancellationToken ct = default);
}

public sealed record ChatFilter(
    bool? IsDeleted = null,
    List<string>? Tags = null,
    DateTimeOffset? CreatedAfter = null,
    DateTimeOffset? CreatedBefore = null,
    DateTimeOffset? UpdatedAfter = null,
    DateTimeOffset? UpdatedBefore = null,
    WorktreeId? WorktreeBinding = null);

public sealed record ChatSort(
    ChatSortField Field = ChatSortField.UpdatedAt,
    SortDirection Direction = SortDirection.Descending);

public enum ChatSortField
{
    CreatedAt,
    UpdatedAt,
    Title,
    MessageCount
}

public enum SortDirection
{
    Ascending,
    Descending
}

public sealed record Pagination(int Limit = 50, int Offset = 0)
{
    public int Limit { get; init; } = Math.Clamp(Limit, 1, 1000);
}

// src/AgenticCoder.Application/Conversation/IRunRepository.cs
public interface IRunRepository
{
    Task<Run?> GetByIdAsync(RunId id, CancellationToken ct = default);
    Task<List<Run>> GetByChatIdAsync(ChatId chatId, CancellationToken ct = default);
    Task<RunId> CreateAsync(Run run, CancellationToken ct = default);
    Task UpdateAsync(Run run, CancellationToken ct = default);
    Task<int> GetMessageCountAsync(RunId id, CancellationToken ct = default);
}

// src/AgenticCoder.Application/Conversation/IMessageRepository.cs
public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(MessageId id, CancellationToken ct = default);
    Task<List<Message>> GetByRunIdAsync(RunId runId, Pagination? pagination = null, CancellationToken ct = default);
    Task<MessageId> CreateAsync(Message message, CancellationToken ct = default);
    Task<List<Message>> SearchAsync(string query, MessageSearchFilter? filter = null, CancellationToken ct = default);
}

public sealed record MessageSearchFilter(
    ChatId? ChatId = null,
    string? Role = null,
    DateTimeOffset? CreatedAfter = null,
    DateTimeOffset? CreatedBefore = null);
```

### SQLite Repository Implementation

```csharp
// src/AgenticCoder.Infrastructure/Persistence/Conversation/SqliteChatRepository.cs
using Dapper;
using Microsoft.Data.Sqlite;

namespace AgenticCoder.Infrastructure.Persistence.Conversation;

public sealed class SqliteChatRepository : IChatRepository
{
    private readonly SqliteConnection _db;
    private readonly WorkspaceId _workspaceId;

    public SqliteChatRepository(SqliteConnection db, WorkspaceId workspaceId)
    {
        _db = db;
        _workspaceId = workspaceId;
    }

    public async Task<Chat?> GetByIdAsync(ChatId id, bool includeDeleted = false, CancellationToken ct = default)
    {
        var sql = @"
            SELECT * FROM chats
            WHERE id = @id AND workspace_id = @workspaceId";

        if (!includeDeleted)
            sql += " AND is_deleted = 0";

        var result = await _db.QuerySingleOrDefaultAsync<ChatDto>(sql, new { id = id.Value.ToString(), workspaceId = _workspaceId.Value });
        return result?.ToDomain();
    }

    public async Task<List<Chat>> ListAsync(ChatFilter? filter = null, ChatSort? sort = null, Pagination? pagination = null, CancellationToken ct = default)
    {
        var sql = "SELECT * FROM chats WHERE workspace_id = @workspaceId";
        var parameters = new DynamicParameters();
        parameters.Add("workspaceId", _workspaceId.Value);

        if (filter is not null)
        {
            if (filter.IsDeleted.HasValue)
            {
                sql += " AND is_deleted = @isDeleted";
                parameters.Add("isDeleted", filter.IsDeleted.Value ? 1 : 0);
            }

            if (filter.Tags is { Count: > 0 })
            {
                sql += " AND (" + string.Join(" AND ", filter.Tags.Select((_, i) => $"tags LIKE @tag{i}")) + ")";
                for (int i = 0; i < filter.Tags.Count; i++)
                    parameters.Add($"tag{i}", $"%{filter.Tags[i]}%");
            }

            if (filter.CreatedAfter.HasValue)
            {
                sql += " AND created_at >= @createdAfter";
                parameters.Add("createdAfter", filter.CreatedAfter.Value);
            }

            if (filter.CreatedBefore.HasValue)
            {
                sql += " AND created_at <= @createdBefore";
                parameters.Add("createdBefore", filter.CreatedBefore.Value);
            }

            if (filter.WorktreeBinding.HasValue)
            {
                sql += " AND worktree_binding = @worktreeBinding";
                parameters.Add("worktreeBinding", filter.WorktreeBinding.Value.Value.ToString());
            }
        }

        if (sort is not null)
        {
            var sortField = sort.Field switch
            {
                ChatSortField.CreatedAt => "created_at",
                ChatSortField.UpdatedAt => "updated_at",
                ChatSortField.Title => "title",
                ChatSortField.MessageCount => "message_count",
                _ => "updated_at"
            };

            var sortDir = sort.Direction == SortDirection.Ascending ? "ASC" : "DESC";
            sql += $" ORDER BY {sortField} {sortDir}";
        }
        else
        {
            sql += " ORDER BY updated_at DESC";
        }

        var page = pagination ?? new Pagination();
        sql += " LIMIT @limit OFFSET @offset";
        parameters.Add("limit", page.Limit);
        parameters.Add("offset", page.Offset);

        var results = await _db.QueryAsync<ChatDto>(sql, parameters);
        return results.Select(dto => dto.ToDomain()).ToList();
    }

    public async Task<ChatId> CreateAsync(Chat chat, CancellationToken ct = default)
    {
        chat.WorkspaceId = _workspaceId; // Enforce workspace scope

        var sql = @"
            INSERT INTO chats (id, workspace_id, title, tags, worktree_binding, is_deleted, message_count, last_message_at, created_at, updated_at)
            VALUES (@Id, @WorkspaceId, @Title, @Tags, @WorktreeBinding, @IsDeleted, @MessageCount, @LastMessageAt, @CreatedAt, @UpdatedAt)";

        await _db.ExecuteAsync(sql, new
        {
            Id = chat.Id.Value.ToString(),
            WorkspaceId = _workspaceId.Value,
            chat.Title,
            Tags = string.Join(",", chat.Tags),
            WorktreeBinding = chat.WorktreeBinding?.Value.ToString(),
            IsDeleted = chat.IsDeleted ? 1 : 0,
            chat.MessageCount,
            chat.LastMessageAt,
            chat.CreatedAt,
            chat.UpdatedAt
        });

        return chat.Id;
    }

    public async Task UpdateAsync(Chat chat, CancellationToken ct = default)
    {
        var sql = @"
            UPDATE chats
            SET title = @Title, tags = @Tags, worktree_binding = @WorktreeBinding,
                is_deleted = @IsDeleted, message_count = @MessageCount,
                last_message_at = @LastMessageAt, updated_at = @UpdatedAt
            WHERE id = @Id AND workspace_id = @WorkspaceId";

        await _db.ExecuteAsync(sql, new
        {
            Id = chat.Id.Value.ToString(),
            WorkspaceId = _workspaceId.Value,
            chat.Title,
            Tags = string.Join(",", chat.Tags),
            WorktreeBinding = chat.WorktreeBinding?.Value.ToString(),
            IsDeleted = chat.IsDeleted ? 1 : 0,
            chat.MessageCount,
            chat.LastMessageAt,
            chat.UpdatedAt
        });
    }

    public async Task DeleteAsync(ChatId id, CancellationToken ct = default)
    {
        await _db.ExecuteAsync("DELETE FROM chats WHERE id = @id AND workspace_id = @workspaceId", 
            new { id = id.Value.ToString(), workspaceId = _workspaceId.Value });
    }

    public async Task<List<string>> GetAllTagsAsync(CancellationToken ct = default)
    {
        var sql = "SELECT DISTINCT tags FROM chats WHERE workspace_id = @workspaceId AND is_deleted = 0";
        var results = await _db.QueryAsync<string>(sql, new { workspaceId = _workspaceId.Value });

        var allTags = new HashSet<string>();
        foreach (var tagString in results.Where(t => !string.IsNullOrEmpty(t)))
        {
            foreach (var tag in tagString.Split(',', StringSplitOptions.RemoveEmptyEntries))
                allTags.Add(tag.Trim());
        }

        return allTags.OrderBy(t => t).ToList();
    }

    public async Task<int> GetMessageCountAsync(ChatId id, CancellationToken ct = default)
    {
        var sql = @"
            SELECT COUNT(*) FROM messages m
            JOIN runs r ON m.run_id = r.id
            WHERE r.chat_id = @chatId";

        return await _db.ExecuteScalarAsync<int>(sql, new { chatId = id.Value.ToString() });
    }
}
```

### Error Codes

| Code | Meaning | Resolution |
|------|---------|------------|
| ACODE-CONV-001 | Chat not found | Verify chat ID exists with `acode chat list` |
| ACODE-CONV-002 | Chat already exists | Use unique title or explicit ID |
| ACODE-CONV-003 | Storage error | Check SQLite file permissions, disk space |
| ACODE-CONV-004 | Sync failed | Check network, PostgreSQL connection, retry |
| ACODE-CONV-005 | Search failed | Check FTS5 extension, rebuild index with `PRAGMA optimize` |
| ACODE-CONV-006 | Chat deleted | Use `--include-deleted` flag or restore with `acode chat restore` |
| ACODE-CONV-007 | Purge not allowed | Confirm with `--confirm` flag |
| ACODE-CONV-008 | Invalid chat ID format | Chat IDs must be valid ULIDs |
| ACODE-CONV-009 | Message size exceeded | Content must be < 100KB, tool calls < 50KB |
| ACODE-CONV-010 | Integrity check failed | Message tampering detected, investigate security breach |

### Implementation Checklist

1. [ ] Create all value objects (ChatId, RunId, MessageId, WorkspaceId)
2. [ ] Create Chat entity with full business logic
3. [ ] Create Run entity with status machine
4. [ ] Create Message entity with integrity validation
5. [ ] Create all repository interfaces
6. [ ] Implement SQLite repositories with workspace scoping
7. [ ] Create database schema with migrations
8. [ ] Implement FTS5 full-text search index
9. [ ] Add secret redaction service
10. [ ] Create CLI commands (new, list, open, show, rename, tag, delete, restore, purge, search, export, sync)
11. [ ] Implement export/import functionality
12. [ ] Add sync engine with outbox pattern
13. [ ] Write comprehensive unit tests (80%+ coverage)
14. [ ] Write integration tests for storage
15. [ ] Write E2E tests for CLI workflows
16. [ ] Performance benchmarks for all operations
17. [ ] Security audit for SQL injection, secret leakage, tampering

### Rollout Plan

1. **Phase 1: Domain & SQLite** (Week 1-2)
   - Implement entities, value objects, repositories
   - SQLite schema and migrations
   - Basic CRUD operations

2. **Phase 2: CLI Commands** (Week 3)
   - Implement chat management commands
   - Message display and formatting
   - Error handling and validation

3. **Phase 3: Search & Filtering** (Week 4)
   - FTS5 index setup
   - Search queries with filters
   - Performance optimization

4. **Phase 4: Export/Import** (Week 5)
   - JSON export format
   - Markdown export format
   - Import with validation

5. **Phase 5: PostgreSQL & Sync** (Week 6-7)
   - PostgreSQL repositories
   - Outbox pattern implementation
   - Conflict resolution (LWW)

6. **Phase 6: Security & Audit** (Week 8)
   - Secret redaction
   - Message integrity checksums
   - Workspace isolation enforcement
   - Security audit

---

**End of Task 049 Specification**