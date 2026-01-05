# Task 049.b: CRUSD APIs + CLI Commands

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 2 – CLI + Orchestration Core  
**Dependencies:** Task 049.a (Data Model), Task 010 (CLI), Task 011 (Session)  

---

## Description

Task 049.b implements the CRUSD APIs and CLI commands for conversation management. CRUSD extends CRUD with Soft-Delete—archiving conversations without permanent loss. Users interact through intuitive commands: create, list, open, rename, delete, restore, and purge. This is the primary interface for conversation lifecycle management, enabling users to organize, navigate, and maintain their AI conversation history through clean, discoverable CLI commands.

### Business Value

Well-designed CLI commands directly impact developer productivity. Clear commands reduce cognitive load. Consistent patterns enable muscle memory. Predictable output formats enable automation. Together, these translate to measurable time savings and workflow efficiency.

**Direct Business Impact:**

1. **Command Discoverability:** Intuitive command names reduce learning time. New users can guess commands without documentation. `acode chat new`, `acode chat list`, `acode chat delete` follow natural language patterns. Reduced onboarding time: 2 hours → 15 minutes per developer. **Savings: $185 per developer onboarded**.

2. **Workflow Automation:** Consistent JSON output enables scripting. Developers can build automation around chat commands. CI/CD pipelines create chats, run tasks, export results. Each automated workflow saves 30 minutes manual work. 10 workflows/month = **$500/month per developer**.

3. **Error Recovery:** Soft-delete prevents accidental data loss. Deleted chats are recoverable for 30 days. Users restore instead of recreating conversations. Average recovery: 2 chats/month × 30 minutes each = **$100/month per developer**.

4. **Context Switching Speed:** Fast chat switching maintains flow state. `acode chat open <id>` takes <50ms. Developers switch between 5-10 chats daily. Slow switching (2-3 seconds) breaks concentration. Fast switching preserves focus. **Value: $125/month per developer in flow state preservation**.

5. **Bulk Operations via Scripting:** Scriptable commands enable batch operations. Developers export all chats, filter by criteria, bulk delete old conversations. Manual bulk operations take 1-2 hours. Scripts run in minutes. **Savings: $150/month per developer**.

**Annual ROI for a 10-Developer Team:**
- Onboarding time savings: 10 devs × $185 = $1,850 (one-time)
- Workflow automation: 10 devs × $500/month × 12 = $60,000
- Error recovery: 10 devs × $100/month × 12 = $12,000
- Context switching: 10 devs × $125/month × 12 = $15,000
- Bulk operations: 10 devs × $150/month × 12 = $18,000
- **Total Annual Value: $106,850**

### Technical Architecture

Task 049.b implements a clean separation between application logic and CLI presentation. Application services handle business logic. CLI commands provide user interface. This separation enables future interfaces (web, IDE plugins) without duplicating logic.

**CQRS Pattern:**

Commands modify state (create, rename, delete). Queries read state (list, show, status). Clear separation improves testability—commands test state changes, queries test read logic. Handlers are single-purpose and composable.

```
┌──────────────────────────────────────────────────────────────────┐
│                    CLI COMMAND ARCHITECTURE                       │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │                        CLI LAYER                            │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │ │
│  │  │ ChatCommand  │  │ chat new     │  │ chat list    │    │ │
│  │  │  (Router)    │──│ chat open    │  │ chat show    │    │ │
│  │  │              │  │ chat rename  │  │ chat delete  │    │ │
│  │  └──────────────┘  │ chat restore │  │ chat purge   │    │ │
│  │         │          └──────────────┘  └──────────────┘    │ │
│  └─────────┼──────────────────────────────────────────────────┘ │
│            │                                                     │
│            ▼                                                     │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │                   APPLICATION LAYER                         │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │ │
│  │  │ Commands     │  │ Queries      │  │ Handlers     │    │ │
│  │  │              │  │              │  │              │    │ │
│  │  │ CreateChat   │  │ ListChats    │  │ CreateChat   │    │ │
│  │  │ RenameChat   │  │ ShowChat     │  │  Handler     │    │ │
│  │  │ DeleteChat   │  │ GetStatus    │  │ ListChats    │    │ │
│  │  │ RestoreChat  │  │              │  │  Handler     │    │ │
│  │  │ PurgeChat    │  │              │  │ DeleteChat   │    │ │
│  │  │ OpenChat     │  │              │  │  Handler     │    │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘    │ │
│  │         │                   │                 │            │ │
│  └─────────┼───────────────────┼─────────────────┼──────────────┘ │
│            │                   │                 │                │
│            ▼                   ▼                 ▼                │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │                     DOMAIN LAYER                            │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │ │
│  │  │ Chat Entity  │  │ IChatRepo    │  │ ISessionMgr  │    │ │
│  │  │ - Create()   │  │ - CreateAsync│  │ - SetActive  │    │ │
│  │  │ - Rename()   │  │ - UpdateAsync│  │ - GetActive  │    │ │
│  │  │ - Delete()   │  │ - DeleteAsync│  │              │    │ │
│  │  │ - Restore()  │  │ - GetByIdAsync│  │             │    │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘    │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

**Command Lifecycle:**

1. **Parse:** CLI framework parses arguments → CreateChatCommand
2. **Validate:** Handler validates Title length, format
3. **Execute:** Handler calls domain Chat.Create() → new Chat entity
4. **Persist:** Handler calls repository CreateAsync()
5. **Update Session:** Handler sets chat as active in session state
6. **Respond:** Handler returns ChatId → CLI formats output

**Soft-Delete Implementation:**

Soft-delete sets `IsDeleted = true` and `DeletedAt = DateTimeOffset.UtcNow`. Deleted chats are filtered from normal queries via `WHERE is_deleted = 0`. Restore clears both flags. Purge executes hard delete via `DELETE FROM chats WHERE id = @Id`. This three-tier deletion model balances safety (restore window) with cleanup (purge old data).

**Active Chat Tracking:**

Session state tracks one active chat per worktree. Opening a chat updates session: `session.SetActiveChatAsync(chatId)`. New runs and messages append to active chat automatically. This eliminates need to specify chat ID for every command.

**Validation Layers:**

1. **CLI Layer:** Basic format validation (non-empty title, valid ULID format)
2. **Application Layer:** Business rule validation (title length, chat exists)
3. **Domain Layer:** Invariant enforcement (title not null, version consistency)
4. **Repository Layer:** Constraint validation (unique ID, foreign keys)

Multiple validation layers catch errors early, provide specific messages, prevent invalid state.

**Error Handling Strategy:**

Commands return Result<T, Error> instead of throwing exceptions for expected failures. Chat not found? Return `Result.Failure(ChatNotFoundError)`. Invalid title? Return `Result.Failure(ValidationError)`. Exceptions reserve for unexpected failures (database unavailable). This makes error handling explicit and testable.

**Output Formatting:**

Human-readable mode shows tables, colors, summaries. JSON mode emits structured data for scripts. Commands detect TTY vs pipe and adjust output accordingly. Example:

```
# Human-readable
acode chat list
┌─────────────┬────────────────────┬───────────────────────┬─────────┐
│ ID          │ Title              │ Updated               │ Status  │
├─────────────┼────────────────────┼───────────────────────┼─────────┤
│ 01HQABC...  │ Feature API Design │ 2025-01-15 14:32:11   │ Active  │
│ 01HQXYZ...  │ Bug Investigation  │ 2025-01-14 09:15:43   │ Active  │
└─────────────┴────────────────────┴───────────────────────┴─────────┘

# JSON mode
acode chat list --json
{"chats":[{"id":"01HQABC...","title":"Feature API Design","updatedAt":"2025-01-15T14:32:11Z","status":"Active"}]}
```

**Pagination Implementation:**

List commands support `--limit` and `--offset` flags. Default limit: 50 chats. Maximum limit: 1000. Offset enables page navigation. Repository queries include `LIMIT @Limit OFFSET @Offset` clauses. Response includes `hasMore` flag indicating additional pages exist.

**Confirmation Prompts:**

Destructive operations (delete, purge) prompt for confirmation in interactive mode. Non-interactive mode (CI/CD) requires `--force` flag. Double confirmation for purge (type chat title to confirm). Prompts are skippable via `--yes` global flag when appropriate risk level configured.

**Logging and Audit:**

All commands emit structured logs. CreateChat logs: chatId, title, timestamp, worktreeId, userId. DeleteChat logs: chatId, deletedAt, deletedBy. Purge logs: chatId, purgedAt, purgedBy, confirmation. Logs enable audit trails, debugging, and compliance.

**Performance Targets:**

| Operation | Target | Rationale |
|-----------|--------|-----------|
| Create | <50ms | Instant feedback, synchronous operation |
| List 50 | <100ms | Pagination, indexed queries, acceptable UI latency |
| Open | <25ms | Critical path, blocks user workflow |
| Rename | <50ms | Synchronous operation, indexed update |
| Delete | <50ms | Soft-delete is fast (single UPDATE) |
| Restore | <50ms | Soft-restore is fast (single UPDATE) |
| Purge | <500ms | Cascading DELETE, acceptable for rare operation |

**Integration Points:**

- **Task 049.a:** Uses Chat entity, IChatRepository
- **Task 010:** Uses CLI framework, command routing, output formatting
- **Task 011:** Uses session state for active chat tracking
- **Task 049.c:** Future worktree binding uses OpenChat to switch context
- **Task 049.d:** Future search command reuses list filtering patterns

**Constraints and Limitations:**

1. **Single-User Context:** Commands operate in current workspace only. No cross-workspace chat operations.
2. **Synchronous Operations:** Commands block until complete. No background tasks or async progress.
3. **Linear Restore:** Restore recovers last deleted version. No point-in-time restore.
4. **No Undo:** Purge is irreversible. Deleted+Purged chats cannot be recovered.
5. **Title-Only Auto-Generation:** Auto-title generates from timestamp. No LLM-powered summary (future enhancement).
6. **Single Chat Per Command:** Batch operations not supported. Script multiple commands for bulk actions.
7. **No Merge:** Duplicate titles allowed. No automatic chat merging or deduplication.

**Trade-Offs and Alternatives:**

**Soft-Delete vs Hard-Delete:**
- **Chosen:** Soft-delete by default, purge for permanent deletion
- **Rationale:** Safety—accidental deletes recoverable. Compliance—audit trail preserved.
- **Alternative:** Hard-delete with backup/restore. Rejected: More complex, slower recovery.

**Confirmation Prompts vs No Prompts:**
- **Chosen:** Prompt for delete/purge, --force to skip
- **Rationale:** Prevents accidents, aligns with user expectations (rm -rf pattern)
- **Alternative:** Always prompt, no force flag. Rejected: Blocks automation.

**CQRS vs Anemic Services:**
- **Chosen:** CQRS with commands and queries
- **Rationale:** Clear separation, testable, aligns with DDD
- **Alternative:** Single "ChatService" with all methods. Rejected: Harder to test, grows unwieldy.

**Auto-Title vs Manual-Only:**
- **Chosen:** Optional auto-title with --auto-title flag
- **Rationale:** Convenience for quick chats, user choice preserved
- **Alternative:** Always auto-title. Rejected: Users want control over naming.

---

## Use Cases

### Use Case 1: DevBot's Multi-Feature Workflow Management

**Persona:** DevBot is a senior full-stack developer working on three features simultaneously: API design, UI prototyping, and database migration. Each feature has its own git branch and requires separate AI conversation context. DevBot uses chat commands to organize conversations by feature.

**Before:** Without conversation management, DevBot's chat history is one long unorganized thread. Scrolling to find API design context takes 2-3 minutes. Switching between features loses context—DevBot must re-explain requirements to AI. Total context switching overhead: 20 minutes/day × 20 days = **400 minutes/month = $667/month wasted** (at $100/hour).

**After:** DevBot creates three chats with `acode chat new "API Design - Feature XYZ"`, `acode chat new "UI Prototype - Component Library"`, `acode chat new "DB Migration - Schema v2"`. Each chat binds to its worktree via `--worktree` flag. DevBot switches context with `acode chat open <id>` (<25ms). AI maintains separate context per chat. No re-explaining. **Time saved: 20 minutes/day → 2 minutes/day (90% reduction). Annual savings: $6,000 per developer**.

**Concrete Metrics:**
- Context discovery time: 2-3 minutes → <1 second (99% reduction)
- Feature switching time: 5 minutes (context loss + re-explain) → 25ms (instant)
- Context switches per day: 8 → 8 (same frequency, 99% faster)
- Time wasted on context management: 20 min/day → 2 min/day
- Monthly savings: 400 minutes → 40 minutes (360 minutes saved = $600/month)
- Annual value per developer: **$7,200**

### Use Case 2: Jordan's Investigation Chat Lifecycle

**Persona:** Jordan is a DevOps engineer investigating a production incident. Jordan creates a troubleshooting chat, gathers diagnostic information, identifies root cause, implements fix. After deployment, Jordan archives the chat for future reference. Two months later, similar incident occurs—Jordan restores archived chat to review previous diagnosis.

**Before:** Without soft-delete, Jordan either keeps all troubleshooting chats cluttering the list (cognitive overload from 50+ old chats), or hard-deletes them and loses valuable incident history. When similar incident occurs, Jordan starts from scratch, repeating 2-3 hours of diagnostic work. **Incident response time: 4 hours. Cost of repeated work: $400**.

**After:** Jordan creates chat with `acode chat new "Incident 2025-01-15 - API Timeout"`. After resolution, Jordan soft-deletes with `acode chat delete <id>` (archived, not destroyed). Active list stays clean (only current incidents). Two months later, similar incident occurs. Jordan searches old chat titles with `acode chat list --archived | grep Timeout`, finds relevant chat, restores with `acode chat restore <id>` (<50ms). Jordan reviews previous diagnosis, skips 2 hours of duplicate investigation, applies same fix. **Incident response time: 2 hours. Savings: $200 per recurring incident**.

**Concrete Metrics:**
- Active chat list size: 50+ chats → 5-10 current chats (80% reduction)
- Archived chat retention: 0 (deleted forever) → 100% (soft-deleted, recoverable)
- Recurring incident investigation time: 4 hours → 2 hours (50% reduction)
- Incidents reusing archived context: 4/year average
- Annual savings per engineer: 4 incidents × $200 = **$800/year**
- Team of 5 DevOps engineers: **$4,000/year saved**

### Use Case 3: Alex's Automated Chat Lifecycle Management

**Persona:** Alex is a platform engineer maintaining CI/CD pipelines. Alex scripts automated quality checks using Acode. Each pipeline run creates a chat, executes tests, exports results, then purges the chat to prevent clutter. Alex uses JSON output mode for parsing.

**Before:** Without scriptable chat commands, Alex manually creates chats for each test run, copies results to tickets, manually deletes old chats. Manual workflow: 10 minutes per pipeline run × 20 runs/week = **200 minutes/week = $333/week wasted** (at $100/hour). Old chats accumulate (100+ per month), causing list performance degradation.

**After:** Alex scripts the workflow:

```bash
#!/bin/bash
CHAT_ID=$(acode chat new "CI Run $(date +%Y%m%d-%H%M%S)" --json | jq -r '.chatId')
acode run --chat $CHAT_ID "Run test suite and analyze failures"
acode chat show $CHAT_ID --json > results_$CHAT_ID.json
acode chat purge $CHAT_ID --force  # Clean up after export
```

Pipeline runs fully automated. No manual intervention. Chats auto-created, used, exported, purged. List stays clean (0 old chats). **Time per pipeline run: 10 minutes → 30 seconds (95% reduction). Weekly savings: 200 minutes → 10 minutes (190 minutes saved = $317/week)**.

**Concrete Metrics:**
- Manual effort per pipeline run: 10 minutes → 30 seconds (95% reduction)
- Pipeline runs automated: 0/20 → 20/20 (100%)
- Old chats accumulating: 100+/month → 0 (purged after use)
- Weekly time savings: 190 minutes = **$317/week**
- Annual savings per engineer: $317/week × 52 weeks = **$16,484/year**

---

## Security Considerations

### Threat 1: Command Injection via Chat Title

**Risk:** Attackers may inject shell commands into chat titles. If titles are passed unsanitized to shell scripts or logged without escaping, arbitrary code execution is possible.

**Attack Scenario:** Attacker creates chat with title `"; rm -rf / #"`. Script logs `echo "Chat created: $TITLE"` executes rm command. System files deleted.

**Mitigation:**

```csharp
namespace AgenticCoder.Application.Chat.Validation;

/// <summary>
/// Validates and sanitizes chat titles to prevent injection attacks.
/// </summary>
public sealed class ChatTitleValidator
{
    private static readonly Regex InvalidChars = new Regex(
        @"[^\w\s\-_.,!?()\[\]{}@#$%^&*+=<>:;'\""/\\|`~]",
        RegexOptions.Compiled);

    private static readonly string[] DangerousPatterns = new[]
    {
        ";", "&&", "||", "|", ">", "<", "`", "$(",  // Shell metacharacters
        "\r", "\n", "\0",  // Control characters
        "../", "..\\",  // Path traversal
        "<script", "</script>",  // HTML injection
    };

    public static Result<string, ValidationError> Validate(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure<string, ValidationError>(
                new ValidationError("Title cannot be empty"));

        if (title.Length > 500)
            return Result.Failure<string, ValidationError>(
                new ValidationError("Title cannot exceed 500 characters"));

        // Check for dangerous patterns
        foreach (var pattern in DangerousPatterns)
        {
            if (title.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<string, ValidationError>(
                    new ValidationError($"Title contains invalid pattern: {pattern}"));
            }
        }

        // Remove any remaining invalid characters
        var sanitized = InvalidChars.Replace(title, "");

        // Ensure not empty after sanitization
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return Result.Failure<string, ValidationError>(
                new ValidationError("Title contains only invalid characters"));
        }

        return Result.Success<string, ValidationError>(sanitized.Trim());
    }
}
```

### Threat 2: Unauthorized Chat Access via ID Enumeration

**Risk:** Attackers enumerate chat IDs to access conversations from other workspaces. Without workspace scoping, `acode chat show <guessed-id>` may expose other users' chats.

**Attack Scenario:** Attacker obtains one valid chat ID from a shared log. Attacker iterates through IDs: `01HQABC001`, `01HQABC002`, etc. Without authorization checks, attacker views all chats in database.

**Mitigation:**

```csharp
namespace AgenticCoder.Application.Chat.Handlers;

/// <summary>
/// Show chat handler with workspace authorization.
/// </summary>
public sealed class ShowChatHandler : IQueryHandler<ShowChatQuery, ChatDetails>
{
    private readonly IChatRepository _repository;
    private readonly IWorkspaceContext _workspaceContext;
    private readonly ILogger<ShowChatHandler> _logger;

    public async Task<Result<ChatDetails, Error>> Handle(
        ShowChatQuery query,
        CancellationToken ct)
    {
        var chat = await _repository.GetByIdAsync(query.ChatId, ct);

        if (chat is null)
        {
            _logger.LogWarning(
                "Chat {ChatId} not found for workspace {WorkspaceId}",
                query.ChatId, _workspaceContext.WorkspaceId);

            return Result.Failure<ChatDetails, Error>(
                new ChatNotFoundError(query.ChatId));
        }

        // CRITICAL: Verify workspace ownership
        if (chat.WorkspaceId != _workspaceContext.WorkspaceId)
        {
            _logger.LogWarning(
                "Unauthorized access attempt: Chat {ChatId} belongs to workspace {OwnerWorkspace}, " +
                "but requested from workspace {RequestingWorkspace}",
                query.ChatId, chat.WorkspaceId, _workspaceContext.WorkspaceId);

            // Return "not found" instead of "unauthorized" to prevent information disclosure
            return Result.Failure<ChatDetails, Error>(
                new ChatNotFoundError(query.ChatId));
        }

        return Result.Success<ChatDetails, Error>(new ChatDetails
        {
            Id = chat.Id,
            Title = chat.Title,
            CreatedAt = chat.CreatedAt,
            UpdatedAt = chat.UpdatedAt,
            Tags = chat.Tags,
            IsDeleted = chat.IsDeleted
        });
    }
}
```

### Threat 3: Purge Operation Without Sufficient Confirmation

**Risk:** Accidental purge deletes valuable conversation history permanently. Without strong confirmation, users may purge chats by mistake.

**Attack Scenario:** User types `acode chat purge <id> --force` intending to delete a test chat. User mistyped ID, purges important production chat instead. Conversation lost forever.

**Mitigation:**

```csharp
namespace AgenticCoder.CLI.Commands;

/// <summary>
/// Purge command with double confirmation requirement.
/// </summary>
public sealed class PurgeChatCommand : ICommand
{
    private readonly IPurgeChatHandler _handler;
    private readonly IConsole _console;
    private readonly ITerminal _terminal;

    public async Task<int> ExecuteAsync(ChatId chatId, bool force, CancellationToken ct)
    {
        // Step 1: Retrieve chat to confirm it exists and show details
        var chat = await _handler.GetChatByIdAsync(chatId, ct);
        if (chat is null)
        {
            _console.WriteError($"Chat {chatId} not found");
            return 1;
        }

        // Step 2: First confirmation - explain consequences
        if (!force && _terminal.IsInteractive)
        {
            _console.WriteWarning($"⚠️  PERMANENT DELETION WARNING ⚠️");
            _console.WriteLine();
            _console.WriteLine($"You are about to permanently delete chat:");
            _console.WriteLine($"  ID: {chat.Id}");
            _console.WriteLine($"  Title: {chat.Title}");
            _console.WriteLine($"  Created: {chat.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            _console.WriteLine($"  Runs: {chat.RunCount}");
            _console.WriteLine($"  Messages: {chat.MessageCount}");
            _console.WriteLine();
            _console.WriteError("This action CANNOT be undone. All runs and messages will be permanently deleted.");
            _console.WriteLine();

            var confirmProceed = await _console.PromptYesNoAsync(
                "Do you want to proceed with permanent deletion?",
                defaultValue: false);

            if (!confirmProceed)
            {
                _console.WriteLine("Purge cancelled");
                return 0;
            }

            // Step 3: Second confirmation - type chat title
            _console.WriteLine();
            _console.WriteLine($"To confirm, please type the chat title exactly: {chat.Title}");
            var typedTitle = await _console.ReadLineAsync();

            if (typedTitle != chat.Title)
            {
                _console.WriteError("Title does not match. Purge cancelled.");
                return 1;
            }
        }
        else if (!force)
        {
            _console.WriteError("Cannot purge in non-interactive mode without --force flag");
            return 1;
        }

        // Step 4: Execute purge with audit logging
        var result = await _handler.PurgeAsync(chatId, ct);

        if (result.IsSuccess)
        {
            _console.WriteSuccess($"Chat {chatId} permanently deleted");
            return 0;
        }
        else
        {
            _console.WriteError($"Purge failed: {result.Error.Message}");
            return 1;
        }
    }
}
```

### Threat 4: Mass Deletion via Script Without Rate Limiting

**Risk:** Malicious or buggy scripts may purge hundreds of chats rapidly, causing data loss before detection.

**Attack Scenario:** Attacker or buggy script runs `for id in $(acode chat list --json | jq -r '.[].id'); do acode chat purge $id --force; done`. All chats deleted in seconds.

**Mitigation:**

```csharp
namespace AgenticCoder.Application.Chat.Middleware;

/// <summary>
/// Rate limiter for destructive chat operations.
/// </summary>
public sealed class DestructiveOperationRateLimiter
{
    private readonly ConcurrentDictionary<string, SlidingWindow> _windows = new();
    private readonly ILogger<DestructiveOperationRateLimiter> _logger;

    private sealed class SlidingWindow
    {
        private readonly Queue<DateTimeOffset> _timestamps = new();
        private readonly int _maxOperations;
        private readonly TimeSpan _windowSize;

        public SlidingWindow(int maxOperations, TimeSpan windowSize)
        {
            _maxOperations = maxOperations;
            _windowSize = windowSize;
        }

        public bool TryAcquire(DateTimeOffset now)
        {
            lock (_timestamps)
            {
                // Remove timestamps outside window
                while (_timestamps.Count > 0 && now - _timestamps.Peek() > _windowSize)
                {
                    _timestamps.Dequeue();
                }

                if (_timestamps.Count >= _maxOperations)
                {
                    return false; // Rate limit exceeded
                }

                _timestamps.Enqueue(now);
                return true;
            }
        }
    }

    public async Task<Result<Unit, Error>> CheckRateLimitAsync(
        string operation,
        WorkspaceId workspaceId,
        CancellationToken ct)
    {
        var key = $"{workspaceId}:{operation}";

        var window = _windows.GetOrAdd(key, _ => new SlidingWindow(
            maxOperations: 10,  // Max 10 purges
            windowSize: TimeSpan.FromMinutes(5)));  // Per 5 minutes

        var now = DateTimeOffset.UtcNow;

        if (!window.TryAcquire(now))
        {
            _logger.LogWarning(
                "Rate limit exceeded for {Operation} in workspace {WorkspaceId}",
                operation, workspaceId);

            return Result.Failure<Unit, Error>(new RateLimitError(
                $"Too many {operation} operations. Maximum 10 per 5 minutes. " +
                "Please wait before retrying or contact support if legitimate use case."));
        }

        return Result.Success<Unit, Error>(Unit.Value);
    }
}

// Apply in handler
public sealed class PurgeChatHandler : ICommandHandler<PurgeChatCommand, Unit>
{
    private readonly DestructiveOperationRateLimiter _rateLimiter;

    public async Task<Result<Unit, Error>> Handle(
        PurgeChatCommand command,
        CancellationToken ct)
    {
        // Check rate limit BEFORE purging
        var rateLimitResult = await _rateLimiter.CheckRateLimitAsync(
            "purge",
            _workspaceContext.WorkspaceId,
            ct);

        if (rateLimitResult.IsFailure)
        {
            return rateLimitResult;
        }

        // Proceed with purge...
    }
}
```

### Threat 5: Log Injection via Chat Titles in Audit Logs

**Risk:** Malicious titles may inject fake log entries or corrupt log files, hindering incident investigation.

**Attack Scenario:** Attacker creates chat with title containing newlines and log format strings: `"Test\n2025-01-16 10:00:00 [ERROR] System compromised"`. Logs appear to show system compromise when only fake entry.

**Mitigation:**

```csharp
namespace AgenticCoder.Infrastructure.Logging;

/// <summary>
/// Structured logger with injection prevention.
/// </summary>
public sealed class SafeChatAuditLogger
{
    private readonly ILogger _logger;

    public void LogChatCreated(ChatId chatId, string title, WorkspaceId workspaceId)
    {
        // Use structured logging with parameters (not string interpolation)
        _logger.LogInformation(
            "Chat created: ChatId={ChatId}, Title={Title}, WorkspaceId={WorkspaceId}",
            chatId,
            SanitizeForLogging(title),  // Sanitize title
            workspaceId);
    }

    public void LogChatDeleted(ChatId chatId, string title, WorkspaceId workspaceId)
    {
        _logger.LogWarning(
            "Chat soft-deleted: ChatId={ChatId}, Title={Title}, WorkspaceId={WorkspaceId}",
            chatId,
            SanitizeForLogging(title),
            workspaceId);
    }

    public void LogChatPurged(ChatId chatId, string title, WorkspaceId workspaceId, string userId)
    {
        _logger.LogCritical(
            "Chat permanently purged: ChatId={ChatId}, Title={Title}, WorkspaceId={WorkspaceId}, UserId={UserId}",
            chatId,
            SanitizeForLogging(title),
            workspaceId,
            userId);
    }

    private static string SanitizeForLogging(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "[empty]";

        // Remove control characters that could manipulate log output
        var sanitized = Regex.Replace(input, @"[\r\n\t\x00-\x1F\x7F]", "");

        // Truncate to prevent log flooding
        const int MaxLength = 200;
        if (sanitized.Length > MaxLength)
        {
            sanitized = sanitized.Substring(0, MaxLength) + "...[truncated]";
        }

        return sanitized;
    }
}
```

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

- **NFR-001**: Create chat operation MUST complete in < 100ms under normal load (single SQLite write with ULID generation)
- **NFR-002**: List chats operation MUST return first page (50 items) in < 200ms with indexed query
- **NFR-003**: Open chat operation MUST complete in < 50ms (single indexed read by primary key)
- **NFR-004**: All database operations MUST be async to avoid blocking the UI thread
- **NFR-005**: Delete operation (soft-delete) MUST complete in < 50ms (single column update)
- **NFR-006**: Purge operation with cascade MUST complete in < 500ms for chats with up to 100 runs and 1000 messages
- **NFR-007**: Bulk list operations MUST support pagination to prevent memory exhaustion on large datasets
- **NFR-008**: CLI startup time MUST be < 500ms including dependency injection initialization

### Usability

- **NFR-009**: Error messages MUST include actionable guidance (e.g., "Chat not found. Run `acode chat list` to see available chats")
- **NFR-010**: Command output MUST use consistent formatting with Spectre.Console for colored terminal output
- **NFR-011**: All commands MUST support --help flag with usage examples and parameter descriptions
- **NFR-012**: List output MUST display truncated IDs (first 12 chars) for readability with full ID in JSON mode
- **NFR-013**: Commands MUST support --json flag for machine-readable output in CI/CD pipelines
- **NFR-014**: Interactive prompts MUST have reasonable timeouts (30 seconds) with graceful handling

### Reliability

- **NFR-015**: All write operations MUST be atomic - no partial states on failure or interruption
- **NFR-016**: Failed operations MUST NOT leave orphaned data or inconsistent state
- **NFR-017**: Commands MUST be idempotent where possible (repeated delete of same chat returns success)
- **NFR-018**: Application MUST handle database connection failures gracefully with retry logic (3 attempts, exponential backoff)
- **NFR-019**: Concurrent operations on same chat MUST be serialized to prevent race conditions

### Security

- **NFR-020**: Command output MUST NOT expose sensitive information (API keys, tokens, credentials)
- **NFR-021**: Destructive operations (delete, purge) MUST require explicit confirmation unless --force flag provided
- **NFR-022**: Purge operation MUST require double confirmation (type chat ID) to prevent accidental data loss
- **NFR-023**: Chat titles MUST be sanitized to prevent log injection and XSS in output formatting
- **NFR-024**: Workspace isolation MUST prevent access to chats from different workspaces

### Maintainability

- **NFR-025**: All handlers MUST have > 80% unit test coverage for business logic
- **NFR-026**: All public APIs MUST have XML documentation comments
- **NFR-027**: Code MUST follow project-wide naming conventions and pass StyleCop analysis
- **NFR-028**: Commands MUST use dependency injection for all services (no direct instantiation)

### Scalability

- **NFR-029**: System MUST handle 10,000+ chats per workspace without performance degradation
- **NFR-030**: Memory usage for list operation MUST NOT exceed 50MB regardless of total chat count (streaming pagination)

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

### Create Command (AC-001 to AC-012)

- [ ] AC-001: `acode chat new "Title"` creates new chat with specified title
- [ ] AC-002: `acode chat new` without title creates chat with auto-generated timestamp title
- [ ] AC-003: Created chat ID is valid ULID format (26 characters, Crockford base32)
- [ ] AC-004: Chat title accepts 1-500 characters alphanumeric with spaces and common punctuation
- [ ] AC-005: Chat title rejects invalid characters (< > : " / \ | ? * control characters)
- [ ] AC-006: `acode chat new --worktree` binds chat to current git worktree
- [ ] AC-007: `acode chat new --auto-title` enables title generation from first message
- [ ] AC-008: Create returns JSON with chat ID when --json flag provided
- [ ] AC-009: Create displays human-readable success message with chat ID
- [ ] AC-010: Create sets IsDeleted=false by default
- [ ] AC-011: Create sets CreatedAt and UpdatedAt to current UTC timestamp
- [ ] AC-012: Create operation completes in < 100ms under normal load

### List Command (AC-013 to AC-028)

- [ ] AC-013: `acode chat list` displays table of active chats with ID, Title, Updated, Runs columns
- [ ] AC-014: List hides soft-deleted (archived) chats by default
- [ ] AC-015: `acode chat list --archived` includes soft-deleted chats with status indicator
- [ ] AC-016: `acode chat list --sort created` sorts by creation date ascending
- [ ] AC-017: `acode chat list --sort updated` sorts by last update date descending (default)
- [ ] AC-018: `acode chat list --sort title` sorts alphabetically by title
- [ ] AC-019: `acode chat list --filter <text>` filters chats by title substring match (case-insensitive)
- [ ] AC-020: `acode chat list --limit 10` returns exactly 10 results
- [ ] AC-021: `acode chat list --offset 10` skips first 10 results for pagination
- [ ] AC-022: List displays truncated chat IDs (first 12 chars) with "..." suffix for readability
- [ ] AC-023: `acode chat list --json` outputs valid JSON array of chat objects
- [ ] AC-024: List JSON output includes full chat ID, not truncated
- [ ] AC-025: List shows "No chats found" message when result is empty
- [ ] AC-026: List operation completes in < 200ms for first 50 results
- [ ] AC-027: List properly handles pagination across 1000+ chats
- [ ] AC-028: List filters apply cumulatively (--filter AND --archived)

### Open Command (AC-029 to AC-036)

- [ ] AC-029: `acode chat open <id>` sets the specified chat as current session
- [ ] AC-030: Open accepts full ULID chat ID
- [ ] AC-031: Open accepts partial chat ID (prefix match if unique)
- [ ] AC-032: Open returns error ACODE-CHAT-CMD-001 if chat not found
- [ ] AC-033: Open returns error if partial ID matches multiple chats
- [ ] AC-034: Open cannot open a soft-deleted chat (must restore first)
- [ ] AC-035: Open updates session state to reflect current chat
- [ ] AC-036: Open operation completes in < 50ms

### Show Command (AC-037 to AC-048)

- [ ] AC-037: `acode chat show <id>` displays detailed chat information
- [ ] AC-038: Show displays: ID, Title, CreatedAt, UpdatedAt, Tags, WorktreeId, IsDeleted
- [ ] AC-039: Show displays run count (number of runs in chat)
- [ ] AC-040: Show displays message count (total messages across all runs)
- [ ] AC-041: Show displays total token count (sum of tokens across messages)
- [ ] AC-042: Show displays last activity timestamp
- [ ] AC-043: Show returns error ACODE-CHAT-CMD-001 if chat not found
- [ ] AC-044: Show includes soft-deleted chats (displays with archived indicator)
- [ ] AC-045: `acode chat show <id> --json` outputs valid JSON object with all details
- [ ] AC-046: Show JSON includes nested arrays for tags
- [ ] AC-047: Show timestamps are ISO 8601 formatted
- [ ] AC-048: Show handles chats with 0 runs gracefully

### Rename Command (AC-049 to AC-058)

- [ ] AC-049: `acode chat rename <id> "New Title"` updates chat title
- [ ] AC-050: Rename validates new title follows same rules as create (1-500 chars, no invalid chars)
- [ ] AC-051: Rename updates UpdatedAt timestamp to current UTC
- [ ] AC-052: Rename preserves all other chat properties (CreatedAt, Tags, WorktreeId, etc.)
- [ ] AC-053: Rename returns error ACODE-CHAT-CMD-001 if chat not found
- [ ] AC-054: Rename returns error ACODE-CHAT-CMD-002 if new title is invalid
- [ ] AC-055: Rename works on soft-deleted chats
- [ ] AC-056: Rename operation is atomic (no partial update on failure)
- [ ] AC-057: `acode chat rename <id> "Title" --json` outputs updated chat details as JSON
- [ ] AC-058: Rename displays confirmation message with old and new titles

### Delete Command (AC-059 to AC-070)

- [ ] AC-059: `acode chat delete <id>` performs soft-delete (sets IsDeleted=true)
- [ ] AC-060: Delete prompts for confirmation "Are you sure you want to delete chat 'Title'? [y/N]"
- [ ] AC-061: Delete aborts on 'N' or Enter (default no) with "Operation cancelled" message
- [ ] AC-062: Delete proceeds on 'y' or 'Y'
- [ ] AC-063: `acode chat delete <id> --force` bypasses confirmation prompt
- [ ] AC-064: Delete returns error ACODE-CHAT-CMD-001 if chat not found
- [ ] AC-065: Delete returns error ACODE-CHAT-CMD-003 if confirmation declined
- [ ] AC-066: Delete is idempotent (deleting already-deleted chat returns success)
- [ ] AC-067: Delete sets DeletedAt timestamp to current UTC
- [ ] AC-068: Delete preserves all chat data (soft-delete only)
- [ ] AC-069: Delete updates UpdatedAt timestamp
- [ ] AC-070: Delete operation completes in < 50ms

### Restore Command (AC-071 to AC-078)

- [ ] AC-071: `acode chat restore <id>` restores soft-deleted chat (sets IsDeleted=false)
- [ ] AC-072: Restore clears DeletedAt timestamp
- [ ] AC-073: Restore updates UpdatedAt timestamp
- [ ] AC-074: Restore returns error ACODE-CHAT-CMD-001 if chat not found
- [ ] AC-075: Restore is idempotent (restoring active chat returns success)
- [ ] AC-076: Restored chat appears in `acode chat list` output
- [ ] AC-077: Restore preserves all chat data and history
- [ ] AC-078: `acode chat restore <id> --json` outputs restored chat details

### Purge Command (AC-079 to AC-094)

- [ ] AC-079: `acode chat purge <id>` permanently deletes chat and all associated data
- [ ] AC-080: Purge requires double confirmation: "Type the chat ID to confirm permanent deletion: "
- [ ] AC-081: Purge aborts if typed ID doesn't match target chat ID
- [ ] AC-082: `acode chat purge <id> --force` bypasses confirmation (dangerous, for automation only)
- [ ] AC-083: Purge cascade-deletes all runs associated with the chat
- [ ] AC-084: Purge cascade-deletes all messages associated with each run
- [ ] AC-085: Purge cascade-deletes all tool calls associated with each message
- [ ] AC-086: Purge removes chat record from database
- [ ] AC-087: Purge returns error ACODE-CHAT-CMD-001 if chat not found
- [ ] AC-088: Purge returns error ACODE-CHAT-CMD-003 if confirmation doesn't match
- [ ] AC-089: Purge logs CRITICAL level audit entry with chat ID, title, and run count
- [ ] AC-090: Purge operation is atomic (all-or-nothing, no partial cascade)
- [ ] AC-091: Purge completes in < 500ms for chats with up to 100 runs and 1000 messages
- [ ] AC-092: Purge works on both active and soft-deleted chats
- [ ] AC-093: Purge cannot be undone (data is permanently removed)
- [ ] AC-094: Purge rate-limited to 1 per minute per workspace

### Status Command (AC-095 to AC-102)

- [ ] AC-095: `acode chat status` displays current active chat details
- [ ] AC-096: Status shows "No active chat" if no chat is currently open
- [ ] AC-097: Status displays chat ID, title, and worktree binding
- [ ] AC-098: Status shows run count and message count
- [ ] AC-099: Status shows last activity timestamp
- [ ] AC-100: `acode chat status --json` outputs current chat as JSON
- [ ] AC-101: Status returns exit code 0 when chat is active
- [ ] AC-102: Status returns exit code 1 when no chat is active

### Cross-Cutting (AC-103 to AC-115)

- [ ] AC-103: All commands support --help flag with usage examples
- [ ] AC-104: All commands return appropriate exit codes (0 success, 1 error)
- [ ] AC-105: All error messages include error code (ACODE-CHAT-CMD-xxx)
- [ ] AC-106: All error messages include actionable guidance
- [ ] AC-107: All write operations are atomic with proper transaction handling
- [ ] AC-108: All commands log operations at appropriate levels (Info for success, Warning for validation, Error for failures)
- [ ] AC-109: All handlers are registered in DI container
- [ ] AC-110: All commands handle database connection failures gracefully with retry
- [ ] AC-111: All commands sanitize user input to prevent injection attacks
- [ ] AC-112: All timestamps are stored and displayed in UTC with ISO 8601 format
- [ ] AC-113: All commands respect workspace isolation (cannot access other workspace's chats)
- [ ] AC-114: Unit test coverage > 80% for all handlers
- [ ] AC-115: Integration tests pass for full CRUSD lifecycle

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

### Issue 1: Command Not Recognized

**Symptom:** `acode chat <subcommand>` says unknown command or unrecognized option.

**Cause:** Typo in command name, missing command registration in DI, or outdated CLI version.

**Solution:**
1. Check `acode chat --help` for available commands and correct syntax
2. Verify spelling of subcommand (new, list, open, show, rename, delete, restore, purge, status)
3. Ensure CLI is up to date with `acode --version`
4. If command was recently added, rebuild and reinstall CLI

### Issue 2: Chat Creation Fails

**Symptom:** `acode chat new "My Title"` returns error ACODE-CHAT-CMD-002 or ACODE-CHAT-CMD-007.

**Cause:** Database error, invalid title characters, title too long, or disk full.

**Solution:**
1. Check database connectivity: verify `.acode/data/chats.db` exists and is writable
2. Verify chat title doesn't contain invalid characters (special unicode, control chars)
3. Ensure title is between 1-500 characters
4. Check disk space on volume containing workspace
5. Check logs at `.acode/logs/acode.log` for detailed error

### Issue 3: List Returns Empty

**Symptom:** `acode chat list` shows no chats when some should exist.

**Cause:** Filter too restrictive, wrong workspace context, or chats were soft-deleted.

**Solution:**
1. Remove filters and try `acode chat list` without options
2. Verify you're in the correct workspace directory
3. Try `acode chat list --archived` to include soft-deleted chats
4. Check database directly: `sqlite3 .acode/data/chats.db "SELECT * FROM Chats LIMIT 10;"`

### Issue 4: Chat Not Found on Open/Show/Delete

**Symptom:** `acode chat open <id>` returns error ACODE-CHAT-CMD-001 "Chat not found".

**Cause:** Chat ID typo, chat was purged (permanently deleted), or chat belongs to different workspace.

**Solution:**
1. Run `acode chat list` to see available chat IDs with correct format
2. Verify you copied the complete chat ID (ULIDs are 26 characters)
3. If chat was soft-deleted, use `acode chat restore <id>` first
4. Check if you're in the correct workspace (chat may exist in different project)

### Issue 5: Purge Operation Timeout

**Symptom:** `acode chat purge <id>` hangs or times out for chats with many messages.

**Cause:** Large cascade delete (many runs/messages), database locked by another process, or slow disk I/O.

**Solution:**
1. Check for other acode processes: `Get-Process -Name acode` and terminate if stuck
2. For very large chats (1000+ messages), purge may take several seconds - wait for completion
3. If database is locked, close other tools accessing `.acode/data/chats.db`
4. Verify SQLite WAL mode is enabled for better concurrent access

### Issue 6: Confirmation Prompt Not Appearing

**Symptom:** Delete or purge operation doesn't show confirmation prompt, proceeds immediately.

**Cause:** Running in non-interactive mode (CI/CD), stdout redirected, or --force flag in config.

**Solution:**
1. Ensure terminal supports interactive input (not piped)
2. Check if `--force` flag is set in config file or environment
3. For CI/CD, explicitly use `--force` flag: `acode chat delete <id> --force`
4. Verify Spectre.Console can detect interactive terminal

### Issue 7: Rename Operation Fails Validation

**Symptom:** `acode chat rename <id> "New Title"` returns ACODE-CHAT-CMD-002 validation error.

**Cause:** New title fails validation rules (length, characters, reserved words).

**Solution:**
1. Ensure new title is 1-500 characters
2. Avoid special characters: `< > : " / \ | ? *`
3. Avoid leading/trailing whitespace
4. Don't use reserved keywords: "null", "undefined", "system"
5. Check for invisible unicode characters (copy-paste from web can include these)

### Issue 8: Database Locked Error

**Symptom:** Any command returns error "database is locked" or "SQLite busy".

**Cause:** Another process has exclusive lock, orphaned lock file, or crashed process.

**Solution:**
1. Close all other terminals/tools accessing the workspace
2. Check for orphaned acode processes: `Get-Process -Name acode`
3. Delete lock file if present: `.acode/data/chats.db-wal`, `.acode/data/chats.db-shm`
4. Restart terminal and retry
5. If persistent, check for antivirus scanning the database file

---

## Testing Requirements

### Unit Tests - Complete C# Implementations

```csharp
// Tests/Unit/Chat/Commands/CreateChatHandlerTests.cs
using AgenticCoder.Application.Chat.Commands;
using AgenticCoder.Application.Chat.Handlers;
using AgenticCoder.Domain.Conversation;
using Xunit;
using Moq;

namespace AgenticCoder.Tests.Unit.Chat.Commands;

public sealed class CreateChatHandlerTests
{
    private readonly Mock<IChatRepository> _mockRepository;
    private readonly Mock<ISessionManager> _mockSession;
    private readonly CreateChatHandler _handler;

    public CreateChatHandlerTests()
    {
        _mockRepository = new Mock<IChatRepository>();
        _mockSession = new Mock<ISessionManager>();
        _handler = new CreateChatHandler(_mockRepository.Object, _mockSession.Object);
    }

    [Fact]
    public async Task Should_Create_Chat_With_Explicit_Title()
    {
        // Arrange
        var command = new CreateChatCommand(
            Title: "Feature API Design",
            WorktreeId: null,
            AutoTitle: false);

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<Domain.Conversation.Chat>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Conversation.Chat chat, CancellationToken ct) => chat.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(ChatId.Empty, result.Value);

        _mockRepository.Verify(r => r.CreateAsync(
            It.Is<Domain.Conversation.Chat>(c => c.Title == "Feature API Design"),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockSession.Verify(s => s.SetActiveChatAsync(
            It.IsAny<ChatId>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Create_Chat_With_Auto_Generated_Title()
    {
        // Arrange
        var command = new CreateChatCommand(
            Title: null,
            WorktreeId: null,
            AutoTitle: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        _mockRepository.Verify(r => r.CreateAsync(
            It.Is<Domain.Conversation.Chat>(c => c.Title.StartsWith("Chat")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Bind_Chat_To_Worktree()
    {
        // Arrange
        var worktreeId = WorktreeId.Parse("feature-xyz");
        var command = new CreateChatCommand(
            Title: "Feature Work",
            WorktreeId: worktreeId,
            AutoTitle: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        _mockRepository.Verify(r => r.CreateAsync(
            It.Is<Domain.Conversation.Chat>(c => c.WorktreeId == worktreeId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_Reject_Empty_Title_When_Not_Auto_Generating(string invalidTitle)
    {
        // Arrange
        var command = new CreateChatCommand(
            Title: invalidTitle,
            WorktreeId: null,
            AutoTitle: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<ValidationError>(result.Error);
        Assert.Contains("title", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }
}

// Tests/Unit/Chat/Commands/ListChatsHandlerTests.cs
public sealed class ListChatsHandlerTests
{
    private readonly Mock<IChatRepository> _mockRepository;
    private readonly ListChatsHandler _handler;

    public ListChatsHandlerTests()
    {
        _mockRepository = new Mock<IChatRepository>();
        _handler = new ListChatsHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task Should_List_Active_Chats_By_Default()
    {
        // Arrange
        var chats = new List<Domain.Conversation.Chat>
        {
            Domain.Conversation.Chat.Create("Chat 1"),
            Domain.Conversation.Chat.Create("Chat 2")
        };

        _mockRepository
            .Setup(r => r.ListAsync(
                It.Is<ChatFilter>(f => !f.IncludeDeleted),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Domain.Conversation.Chat>(chats, 1, 50));

        var query = new ListChatsQuery(
            IncludeDeleted: false,
            Filter: null,
            SortBy: ChatSortField.UpdatedAt,
            Page: 1,
            PageSize: 50);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);

        _mockRepository.Verify(r => r.ListAsync(
            It.Is<ChatFilter>(f => !f.IncludeDeleted),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Include_Archived_When_Requested()
    {
        // Arrange
        var query = new ListChatsQuery(
            IncludeDeleted: true,
            Filter: null,
            SortBy: ChatSortField.UpdatedAt,
            Page: 1,
            PageSize: 50);

        _mockRepository
            .Setup(r => r.ListAsync(
                It.Is<ChatFilter>(f => f.IncludeDeleted),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Domain.Conversation.Chat>(new List<Domain.Conversation.Chat>(), 1, 50));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.ListAsync(
            It.Is<ChatFilter>(f => f.IncludeDeleted),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Filter_By_Title()
    {
        // Arrange
        var query = new ListChatsQuery(
            IncludeDeleted: false,
            Filter: "API",
            SortBy: ChatSortField.Title,
            Page: 1,
            PageSize: 50);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.ListAsync(
            It.Is<ChatFilter>(f => f.TitleContains == "API"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

// Tests/Unit/Chat/Commands/DeleteChatHandlerTests.cs
public sealed class DeleteChatHandlerTests
{
    private readonly Mock<IChatRepository> _mockRepository;
    private readonly DeleteChatHandler _handler;

    public DeleteChatHandlerTests()
    {
        _mockRepository = new Mock<IChatRepository>();
        _handler = new DeleteChatHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task Should_Soft_Delete_Chat()
    {
        // Arrange
        var chat = Domain.Conversation.Chat.Create("To Delete");
        var chatId = chat.Id;

        _mockRepository
            .Setup(r => r.GetByIdAsync(chatId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chat);

        var command = new DeleteChatCommand(chatId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(chat.IsDeleted);
        Assert.NotNull(chat.DeletedAt);

        _mockRepository.Verify(r => r.UpdateAsync(
            It.Is<Domain.Conversation.Chat>(c => c.IsDeleted),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Return_Error_When_Chat_Not_Found()
    {
        // Arrange
        var chatId = ChatId.New();

        _mockRepository
            .Setup(r => r.GetByIdAsync(chatId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Conversation.Chat?)null);

        var command = new DeleteChatCommand(chatId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<ChatNotFoundError>(result.Error);
    }
}

// Tests/Unit/Chat/Commands/RestoreChatHandlerTests.cs
public sealed class RestoreChatHandlerTests
{
    private readonly Mock<IChatRepository> _mockRepository;
    private readonly RestoreChatHandler _handler;

    public RestoreChatHandlerTests()
    {
        _mockRepository = new Mock<IChatRepository>();
        _handler = new RestoreChatHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task Should_Restore_Deleted_Chat()
    {
        // Arrange
        var chat = Domain.Conversation.Chat.Create("Deleted Chat");
        chat.SoftDelete();
        var chatId = chat.Id;

        _mockRepository
            .Setup(r => r.GetByIdAsync(chatId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chat);

        var command = new RestoreChatCommand(chatId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(chat.IsDeleted);
        Assert.Null(chat.DeletedAt);

        _mockRepository.Verify(r => r.UpdateAsync(
            It.Is<Domain.Conversation.Chat>(c => !c.IsDeleted),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Be_Idempotent_For_Active_Chat()
    {
        // Arrange
        var chat = Domain.Conversation.Chat.Create("Active Chat");
        var chatId = chat.Id;

        _mockRepository
            .Setup(r => r.GetByIdAsync(chatId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chat);

        var command = new RestoreChatCommand(chatId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(chat.IsDeleted);  // Still not deleted

        // Update should not be called since chat is already active
        _mockRepository.Verify(r => r.UpdateAsync(
            It.IsAny<Domain.Conversation.Chat>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}

// Tests/Unit/Chat/Commands/PurgeChatHandlerTests.cs
public sealed class PurgeChatHandlerTests
{
    private readonly Mock<IChatRepository> _mockRepository;
    private readonly Mock<IRunRepository> _mockRunRepository;
    private readonly Mock<IMessageRepository> _mockMessageRepository;
    private readonly PurgeChatHandler _handler;

    public PurgeChatHandlerTests()
    {
        _mockRepository = new Mock<IChatRepository>();
        _mockRunRepository = new Mock<IRunRepository>();
        _mockMessageRepository = new Mock<IMessageRepository>();
        _handler = new PurgeChatHandler(
            _mockRepository.Object,
            _mockRunRepository.Object,
            _mockMessageRepository.Object);
    }

    [Fact]
    public async Task Should_Delete_Chat_With_Cascade()
    {
        // Arrange
        var chatId = ChatId.New();
        var runId1 = RunId.New();
        var runId2 = RunId.New();

        var chat = Domain.Conversation.Chat.Create("To Purge");

        _mockRepository
            .Setup(r => r.GetByIdAsync(chatId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chat);

        _mockRunRepository
            .Setup(r => r.ListByChatAsync(chatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Run>
            {
                Run.Create(chatId, 1),
                Run.Create(chatId, 2)
            });

        var command = new PurgeChatCommand(chatId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify cascade: messages deleted for each run, runs deleted, chat deleted
        _mockMessageRepository.Verify(r => r.DeleteByRunAsync(
            It.IsAny<RunId>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));

        _mockRunRepository.Verify(r => r.DeleteAsync(
            It.IsAny<RunId>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));

        _mockRepository.Verify(r => r.DeleteAsync(
            chatId,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### Integration Tests

```csharp
// Tests/Integration/Chat/ChatCommandsIntegrationTests.cs
public sealed class ChatCommandsIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IChatRepository _chatRepo;
    private readonly ISessionManager _sessionManager;

    public ChatCommandsIntegrationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        ConversationSchema.CreateTables(_connection);
        _chatRepo = new SqliteChatRepository(_connection);
        _sessionManager = new InMemorySessionManager();
    }

    [Fact]
    public async Task Should_Create_And_List_Chats()
    {
        // Arrange
        var handler = new CreateChatHandler(_chatRepo, _sessionManager);
        var listHandler = new ListChatsHandler(_chatRepo);

        // Act - Create 3 chats
        await handler.Handle(new CreateChatCommand("Chat 1", null, false), CancellationToken.None);
        await handler.Handle(new CreateChatCommand("Chat 2", null, false), CancellationToken.None);
        await handler.Handle(new CreateChatCommand("Chat 3", null, false), CancellationToken.None);

        // Act - List chats
        var listResult = await listHandler.Handle(
            new ListChatsQuery(false, null, ChatSortField.UpdatedAt, 1, 50),
            CancellationToken.None);

        // Assert
        Assert.True(listResult.IsSuccess);
        Assert.Equal(3, listResult.Value.Items.Count);
    }

    [Fact]
    public async Task Should_Delete_And_Restore_Chat()
    {
        // Arrange
        var createHandler = new CreateChatHandler(_chatRepo, _sessionManager);
        var deleteHandler = new DeleteChatHandler(_chatRepo);
        var restoreHandler = new RestoreChatHandler(_chatRepo);
        var listHandler = new ListChatsHandler(_chatRepo);

        // Act - Create chat
        var createResult = await createHandler.Handle(
            new CreateChatCommand("Test Chat", null, false),
            CancellationToken.None);
        var chatId = createResult.Value;

        // Act - Delete chat
        await deleteHandler.Handle(new DeleteChatCommand(chatId), CancellationToken.None);

        // Act - List (should not show deleted)
        var listResult1 = await listHandler.Handle(
            new ListChatsQuery(false, null, ChatSortField.UpdatedAt, 1, 50),
            CancellationToken.None);

        // Act - Restore chat
        await restoreHandler.Handle(new RestoreChatCommand(chatId), CancellationToken.None);

        // Act - List again (should show restored)
        var listResult2 = await listHandler.Handle(
            new ListChatsQuery(false, null, ChatSortField.UpdatedAt, 1, 50),
            CancellationToken.None);

        // Assert
        Assert.Empty(listResult1.Value.Items);  // Deleted chat not shown
        Assert.Single(listResult2.Value.Items);  // Restored chat shown
    }

    [Fact]
    public async Task Should_Purge_With_Cascade()
    {
        // Arrange
        var createHandler = new CreateChatHandler(_chatRepo, _sessionManager);
        var runRepo = new SqliteRunRepository(_connection);
        var messageRepo = new SqliteMessageRepository(_connection);
        var purgeHandler = new PurgeChatHandler(_chatRepo, runRepo, messageRepo);

        // Act - Create chat with run and messages
        var createResult = await createHandler.Handle(
            new CreateChatCommand("Chat To Purge", null, false),
            CancellationToken.None);
        var chatId = createResult.Value;

        var run = Run.Create(chatId, 1);
        await runRepo.CreateAsync(run, CancellationToken.None);

        var message = Message.Create(run.Id, MessageRole.User, "Test message", null, 1);
        await messageRepo.CreateAsync(message, CancellationToken.None);

        // Act - Purge
        await purgeHandler.Handle(new PurgeChatCommand(chatId), CancellationToken.None);

        // Assert - Verify all deleted
        var chatResult = await _chatRepo.GetByIdAsync(chatId, true, CancellationToken.None);
        var runResult = await runRepo.GetByIdAsync(run.Id, CancellationToken.None);
        var messageResult = await messageRepo.GetByIdAsync(message.Id, CancellationToken.None);

        Assert.Null(chatResult);
        Assert.Null(runResult);
        Assert.Null(messageResult);
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
```

### E2E Tests

```csharp
// Tests/E2E/Chat/ChatWorkflowE2ETests.cs
public sealed class ChatWorkflowE2ETests
{
    [Fact]
    public async Task Should_Complete_Full_Chat_Lifecycle()
    {
        // Arrange - Full application stack
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        ConversationSchema.CreateTables(connection);

        var chatRepo = new SqliteChatRepository(connection);
        var runRepo = new SqliteRunRepository(connection);
        var messageRepo = new SqliteMessageRepository(connection);
        var sessionManager = new InMemorySessionManager();

        var createHandler = new CreateChatHandler(chatRepo, sessionManager);
        var renameHandler = new RenameChatHandler(chatRepo);
        var deleteHandler = new DeleteChatHandler(chatRepo);
        var restoreHandler = new RestoreChatHandler(chatRepo);
        var purgeHandler = new PurgeChatHandler(chatRepo, runRepo, messageRepo);

        // Act & Assert - Step 1: Create
        var createResult = await createHandler.Handle(
            new CreateChatCommand("Initial Title", null, false),
            CancellationToken.None);
        Assert.True(createResult.IsSuccess);
        var chatId = createResult.Value;

        // Act & Assert - Step 2: Rename
        var renameResult = await renameHandler.Handle(
            new RenameChatCommand(chatId, "Updated Title"),
            CancellationToken.None);
        Assert.True(renameResult.IsSuccess);

        // Act & Assert - Step 3: Soft Delete
        var deleteResult = await deleteHandler.Handle(
            new DeleteChatCommand(chatId),
            CancellationToken.None);
        Assert.True(deleteResult.IsSuccess);

        // Act & Assert - Step 4: Restore
        var restoreResult = await restoreHandler.Handle(
            new RestoreChatCommand(chatId),
            CancellationToken.None);
        Assert.True(restoreResult.IsSuccess);

        // Act & Assert - Step 5: Purge
        var purgeResult = await purgeHandler.Handle(
            new PurgeChatCommand(chatId),
            CancellationToken.None);
        Assert.True(purgeResult.IsSuccess);

        // Verify purged
        var finalCheck = await chatRepo.GetByIdAsync(chatId, true, CancellationToken.None);
        Assert.Null(finalCheck);

        connection.Dispose();
    }
}
```

### Performance Benchmarks

| Benchmark | Target | Maximum | Measurement Tool |
|-----------|--------|---------|------------------|
| Create | 50ms | 100ms | BenchmarkDotNet |
| List 100 | 100ms | 200ms | BenchmarkDotNet |
| Open | 25ms | 50ms | BenchmarkDotNet |
| Rename | 50ms | 100ms | BenchmarkDotNet |
| Delete (soft) | 50ms | 100ms | BenchmarkDotNet |
| Restore | 50ms | 100ms | BenchmarkDotNet |
| Purge (with 10 runs) | 300ms | 500ms | BenchmarkDotNet |

---

## User Verification Steps

### Scenario 1: Create Chat with Title

**Objective:** Verify chat creation with explicit title

**Preconditions:**
- acode CLI installed and configured
- Database initialized (`.acode/data/chats.db` exists)
- Terminal in valid workspace directory

**Steps:**
1. Open terminal in workspace root
2. Run `acode chat new "Test Chat for Verification"`
3. Observe output for success message and chat ID

**Expected Results:**
- [ ] Output shows: `✓ Chat created: chat_[ULID]`
- [ ] Chat ID is 26-character ULID format
- [ ] Title shown as "Test Chat for Verification"
- [ ] Exit code is 0
- [ ] Chat appears in `acode chat list` output

**Verification Commands:**
```bash
acode chat new "Test Chat for Verification"
acode chat list --filter "Test Chat"
```

---

### Scenario 2: Create Chat with Worktree Binding

**Objective:** Verify chat creation bound to current git worktree

**Preconditions:**
- Inside a git repository with worktrees
- Valid workspace context

**Steps:**
1. Navigate to a git worktree directory
2. Run `acode chat new --worktree "Feature Branch Chat"`
3. Run `acode chat show <id>` to verify binding

**Expected Results:**
- [ ] Chat created successfully
- [ ] WorktreeId field populated in show output
- [ ] Worktree binding displayed in status

**Verification Commands:**
```bash
cd feature/my-branch
acode chat new --worktree "Feature Branch Chat"
acode chat show <returned-id>
```

---

### Scenario 3: List Chats with Filtering and Sorting

**Objective:** Verify list command with various filters and sort options

**Preconditions:**
- At least 5 chats exist with varying titles and timestamps

**Steps:**
1. Run `acode chat list` without options
2. Run `acode chat list --sort title`
3. Run `acode chat list --filter "Test"`
4. Run `acode chat list --limit 2 --offset 2`
5. Run `acode chat list --json`

**Expected Results:**
- [ ] Default list shows active chats sorted by updated date
- [ ] `--sort title` shows alphabetical ordering
- [ ] `--filter "Test"` shows only chats with "Test" in title
- [ ] `--limit 2 --offset 2` shows exactly 2 results starting from 3rd
- [ ] `--json` outputs valid JSON array

**Verification Commands:**
```bash
acode chat list
acode chat list --sort title
acode chat list --filter "Test"
acode chat list --limit 2 --offset 2
acode chat list --json | jq '.'
```

---

### Scenario 4: Open and Show Chat Details

**Objective:** Verify opening a chat sets active session and show displays details

**Preconditions:**
- At least one chat exists

**Steps:**
1. Run `acode chat list` and note a chat ID
2. Run `acode chat open <id>`
3. Run `acode chat status`
4. Run `acode chat show <id>`

**Expected Results:**
- [ ] Open command confirms chat is now active
- [ ] Status shows the opened chat's ID and title
- [ ] Show displays full details: ID, Title, Created, Updated, Tags, Status
- [ ] Show displays statistics: run count, message count

**Verification Commands:**
```bash
export CHAT_ID=$(acode chat list --json | jq -r '.[0].id')
acode chat open $CHAT_ID
acode chat status
acode chat show $CHAT_ID
```

---

### Scenario 5: Rename Chat

**Objective:** Verify chat rename updates title while preserving other data

**Preconditions:**
- Chat exists with known ID

**Steps:**
1. Note original chat title with `acode chat show <id>`
2. Run `acode chat rename <id> "Renamed Chat Title"`
3. Verify new title with `acode chat show <id>`

**Expected Results:**
- [ ] Rename command confirms title changed
- [ ] New title displayed: "Renamed Chat Title"
- [ ] CreatedAt timestamp unchanged
- [ ] UpdatedAt timestamp updated to current time
- [ ] Other fields (Tags, WorktreeId) preserved

**Verification Commands:**
```bash
acode chat show <id>
acode chat rename <id> "Renamed Chat Title"
acode chat show <id>
```

---

### Scenario 6: Delete with Confirmation Prompt

**Objective:** Verify delete prompts for confirmation and soft-deletes

**Preconditions:**
- Chat exists that can be deleted

**Steps:**
1. Run `acode chat delete <id>` (no --force)
2. Type 'N' at confirmation prompt
3. Verify chat still exists with `acode chat list`
4. Run `acode chat delete <id>` again
5. Type 'y' at confirmation prompt
6. Verify chat no longer in default list
7. Verify chat appears with `acode chat list --archived`

**Expected Results:**
- [ ] Confirmation prompt displayed: "Are you sure you want to delete chat 'Title'? [y/N]"
- [ ] 'N' response aborts with "Operation cancelled"
- [ ] 'y' response proceeds with soft-delete
- [ ] Chat hidden from default list
- [ ] Chat visible with --archived flag
- [ ] Chat data preserved (can be restored)

**Verification Commands:**
```bash
acode chat delete <id>
# Type 'N'
acode chat list --filter "<title>"
acode chat delete <id>
# Type 'y'
acode chat list --archived
```

---

### Scenario 7: Delete with Force Flag

**Objective:** Verify --force bypasses confirmation

**Preconditions:**
- Chat exists for testing

**Steps:**
1. Run `acode chat delete <id> --force`
2. Verify no prompt appeared
3. Verify chat is archived

**Expected Results:**
- [ ] No confirmation prompt displayed
- [ ] Command completes immediately
- [ ] Chat soft-deleted (appears in --archived list)
- [ ] Exit code is 0

**Verification Commands:**
```bash
acode chat delete <id> --force
echo $LASTEXITCODE
acode chat list --archived
```

---

### Scenario 8: Restore Deleted Chat

**Objective:** Verify restore brings back soft-deleted chat

**Preconditions:**
- Soft-deleted chat exists

**Steps:**
1. Run `acode chat list --archived` to find deleted chat
2. Run `acode chat restore <id>`
3. Verify chat appears in default `acode chat list`

**Expected Results:**
- [ ] Restore command confirms chat restored
- [ ] Chat visible in default list (no --archived needed)
- [ ] IsDeleted set to false
- [ ] All chat data and history intact

**Verification Commands:**
```bash
acode chat list --archived
acode chat restore <id>
acode chat list
acode chat show <id>
```

---

### Scenario 9: Purge with Double Confirmation

**Objective:** Verify purge requires typing chat ID and permanently deletes all data

**Preconditions:**
- Chat exists with at least one run and messages
- This is a destructive test - use test data only

**Steps:**
1. Create test chat and add some activity
2. Run `acode chat purge <id>`
3. At prompt "Type the chat ID to confirm permanent deletion:", type incorrect ID
4. Verify operation aborted
5. Run `acode chat purge <id>` again
6. Type correct chat ID
7. Verify chat completely removed

**Expected Results:**
- [ ] Double confirmation prompt displayed
- [ ] Wrong ID input aborts with error
- [ ] Correct ID input proceeds with deletion
- [ ] Chat not found in any list (including --archived)
- [ ] Associated runs permanently deleted
- [ ] Associated messages permanently deleted
- [ ] CRITICAL log entry created

**Verification Commands:**
```bash
acode chat purge <id>
# Type wrong ID - should fail
acode chat purge <id>
# Type correct ID
acode chat list --archived
acode chat show <id>  # Should return not found
```

---

### Scenario 10: End-to-End CRUSD Lifecycle

**Objective:** Verify complete Create-Read-Update-Soft Delete-Restore-Purge lifecycle

**Preconditions:**
- Clean test environment or isolated test workspace

**Steps:**
1. **Create:** `acode chat new "E2E Test Chat"`
2. **Read (List):** `acode chat list` - verify chat appears
3. **Read (Show):** `acode chat show <id>` - verify details
4. **Update (Rename):** `acode chat rename <id> "E2E Renamed"`
5. **Soft Delete:** `acode chat delete <id> --force`
6. **Verify Hidden:** `acode chat list` - should not appear
7. **Restore:** `acode chat restore <id>`
8. **Verify Restored:** `acode chat list` - should appear
9. **Purge:** `acode chat purge <id> --force`
10. **Verify Gone:** `acode chat show <id>` - should error

**Expected Results:**
- [ ] Each operation succeeds with appropriate output
- [ ] State transitions occur as expected
- [ ] Data integrity maintained through lifecycle
- [ ] Final purge completely removes all traces
- [ ] All operations complete within performance SLAs

**Verification Commands:**
```bash
# Full lifecycle script
CHAT_ID=$(acode chat new "E2E Test Chat" --json | jq -r '.id')
acode chat list
acode chat show $CHAT_ID
acode chat rename $CHAT_ID "E2E Renamed"
acode chat delete $CHAT_ID --force
acode chat list --archived
acode chat restore $CHAT_ID
acode chat list
acode chat purge $CHAT_ID --force
acode chat show $CHAT_ID  # Should fail
```

---

## Implementation Prompt

### Complete Application Commands

```csharp
// src/AgenticCoder.Application/Chat/Commands/CreateChatCommand.cs
namespace AgenticCoder.Application.Chat.Commands;

public sealed record CreateChatCommand(
    string? Title,
    WorktreeId? WorktreeId,
    bool AutoTitle) : ICommand<ChatId>;

// src/AgenticCoder.Application/Chat/Handlers/CreateChatHandler.cs
public sealed class CreateChatHandler : ICommandHandler<CreateChatCommand, ChatId>
{
    private readonly IChatRepository _repository;
    private readonly ISessionManager _session;
    private readonly ILogger<CreateChatHandler> _logger;

    public CreateChatHandler(
        IChatRepository repository,
        ISessionManager session,
        ILogger<CreateChatHandler> logger)
    {
        _repository = repository;
        _session = session;
        _logger = logger;
    }

    public async Task<Result<ChatId, Error>> Handle(
        CreateChatCommand command,
        CancellationToken ct)
    {
        // Determine title
        var title = command.AutoTitle
            ? $"Chat {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}"
            : command.Title;

        // Validate title
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure<ChatId, Error>(
                new ValidationError("Title cannot be empty"));
        }

        // Create domain entity
        var chat = Domain.Conversation.Chat.Create(title);

        if (command.WorktreeId is not null)
        {
            chat.BindToWorktree(command.WorktreeId);
        }

        // Persist
        await _repository.CreateAsync(chat, ct);

        // Set as active
        await _session.SetActiveChatAsync(chat.Id, ct);

        _logger.LogInformation(
            "Chat created: {ChatId}, Title={Title}, WorktreeId={WorktreeId}",
            chat.Id, chat.Title, chat.WorktreeId);

        return Result.Success<ChatId, Error>(chat.Id);
    }
}

// src/AgenticCoder.Application/Chat/Queries/ListChatsQuery.cs
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

// src/AgenticCoder.Application/Chat/Handlers/ListChatsHandler.cs
public sealed class ListChatsHandler : IQueryHandler<ListChatsQuery, PagedResult<ChatSummary>>
{
    private readonly IChatRepository _repository;

    public ListChatsHandler(IChatRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResult<ChatSummary>, Error>> Handle(
        ListChatsQuery query,
        CancellationToken ct)
    {
        var filter = new ChatFilter(
            Page: query.Page,
            PageSize: query.PageSize,
            IncludeDeleted: query.IncludeDeleted,
            TitleContains: query.Filter,
            SortBy: query.SortBy,
            SortDescending: true);

        var chats = await _repository.ListAsync(filter, ct);

        var summaries = chats.Items.Select(c => new ChatSummary(
            c.Id,
            c.Title,
            c.UpdatedAt,
            c.IsDeleted,
            c.MessageCount)).ToList();

        return Result.Success<PagedResult<ChatSummary>, Error>(
            new PagedResult<ChatSummary>(summaries, query.Page, query.PageSize));
    }
}

// src/AgenticCoder.Application/Chat/Commands/OpenChatCommand.cs
public sealed record OpenChatCommand(ChatId ChatId) : ICommand<Unit>;

// src/AgenticCoder.Application/Chat/Handlers/OpenChatHandler.cs
public sealed class OpenChatHandler : ICommandHandler<OpenChatCommand, Unit>
{
    private readonly IChatRepository _repository;
    private readonly ISessionManager _session;
    private readonly ILogger<OpenChatHandler> _logger;

    public OpenChatHandler(
        IChatRepository repository,
        ISessionManager session,
        ILogger<OpenChatHandler> logger)
    {
        _repository = repository;
        _session = session;
        _logger = logger;
    }

    public async Task<Result<Unit, Error>> Handle(
        OpenChatCommand command,
        CancellationToken ct)
    {
        // Validate chat exists
        var chat = await _repository.GetByIdAsync(command.ChatId, includeDeleted: false, ct);

        if (chat is null)
        {
            return Result.Failure<Unit, Error>(
                new ChatNotFoundError(command.ChatId));
        }

        // Set as active
        await _session.SetActiveChatAsync(chat.Id, ct);

        _logger.LogInformation("Chat opened: {ChatId}, Title={Title}", chat.Id, chat.Title);

        return Result.Success<Unit, Error>(Unit.Value);
    }
}

// src/AgenticCoder.Application/Chat/Commands/RenameChatCommand.cs
public sealed record RenameChatCommand(ChatId ChatId, string NewTitle) : ICommand<Unit>;

// src/AgenticCoder.Application/Chat/Handlers/RenameChatHandler.cs
public sealed class RenameChatHandler : ICommandHandler<RenameChatCommand, Unit>
{
    private readonly IChatRepository _repository;
    private readonly ILogger<RenameChatHandler> _logger;

    public RenameChatHandler(IChatRepository repository, ILogger<RenameChatHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<Unit, Error>> Handle(
        RenameChatCommand command,
        CancellationToken ct)
    {
        var chat = await _repository.GetByIdAsync(command.ChatId, includeDeleted: false, ct);

        if (chat is null)
        {
            return Result.Failure<Unit, Error>(new ChatNotFoundError(command.ChatId));
        }

        chat.Rename(command.NewTitle);

        await _repository.UpdateAsync(chat, ct);

        _logger.LogInformation(
            "Chat renamed: {ChatId}, OldTitle={OldTitle}, NewTitle={NewTitle}",
            chat.Id, chat.Title, command.NewTitle);

        return Result.Success<Unit, Error>(Unit.Value);
    }
}

// src/AgenticCoder.Application/Chat/Commands/DeleteChatCommand.cs
public sealed record DeleteChatCommand(ChatId ChatId) : ICommand<Unit>;

// src/AgenticCoder.Application/Chat/Handlers/DeleteChatHandler.cs
public sealed class DeleteChatHandler : ICommandHandler<DeleteChatCommand, Unit>
{
    private readonly IChatRepository _repository;
    private readonly ILogger<DeleteChatHandler> _logger;

    public DeleteChatHandler(IChatRepository repository, ILogger<DeleteChatHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<Unit, Error>> Handle(
        DeleteChatCommand command,
        CancellationToken ct)
    {
        var chat = await _repository.GetByIdAsync(command.ChatId, includeDeleted: false, ct);

        if (chat is null)
        {
            return Result.Failure<Unit, Error>(new ChatNotFoundError(command.ChatId));
        }

        chat.SoftDelete();

        await _repository.UpdateAsync(chat, ct);

        _logger.LogWarning("Chat soft-deleted: {ChatId}, Title={Title}", chat.Id, chat.Title);

        return Result.Success<Unit, Error>(Unit.Value);
    }
}

// src/AgenticCoder.Application/Chat/Commands/RestoreChatCommand.cs
public sealed record RestoreChatCommand(ChatId ChatId) : ICommand<Unit>;

// src/AgenticCoder.Application/Chat/Handlers/RestoreChatHandler.cs
public sealed class RestoreChatHandler : ICommandHandler<RestoreChatCommand, Unit>
{
    private readonly IChatRepository _repository;
    private readonly ILogger<RestoreChatHandler> _logger;

    public RestoreChatHandler(IChatRepository repository, ILogger<RestoreChatHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<Unit, Error>> Handle(
        RestoreChatCommand command,
        CancellationToken ct)
    {
        var chat = await _repository.GetByIdAsync(command.ChatId, includeDeleted: true, ct);

        if (chat is null)
        {
            return Result.Failure<Unit, Error>(new ChatNotFoundError(command.ChatId));
        }

        if (!chat.IsDeleted)
        {
            // Idempotent - already active
            return Result.Success<Unit, Error>(Unit.Value);
        }

        chat.Restore();

        await _repository.UpdateAsync(chat, ct);

        _logger.LogInformation("Chat restored: {ChatId}, Title={Title}", chat.Id, chat.Title);

        return Result.Success<Unit, Error>(Unit.Value);
    }
}

// src/AgenticCoder.Application/Chat/Commands/PurgeChatCommand.cs
public sealed record PurgeChatCommand(ChatId ChatId) : ICommand<Unit>;

// src/AgenticCoder.Application/Chat/Handlers/PurgeChatHandler.cs
public sealed class PurgeChatHandler : ICommandHandler<PurgeChatCommand, Unit>
{
    private readonly IChatRepository _chatRepository;
    private readonly IRunRepository _runRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ILogger<PurgeChatHandler> _logger;

    public PurgeChatHandler(
        IChatRepository chatRepository,
        IRunRepository runRepository,
        IMessageRepository messageRepository,
        ILogger<PurgeChatHandler> logger)
    {
        _chatRepository = chatRepository;
        _runRepository = runRepository;
        _messageRepository = messageRepository;
        _logger = logger;
    }

    public async Task<Result<Unit, Error>> Handle(
        PurgeChatCommand command,
        CancellationToken ct)
    {
        var chat = await _chatRepository.GetByIdAsync(command.ChatId, includeDeleted: true, ct);

        if (chat is null)
        {
            return Result.Failure<Unit, Error>(new ChatNotFoundError(command.ChatId));
        }

        // Cascade delete: messages → runs → chat
        var runs = await _runRepository.ListByChatAsync(command.ChatId, ct);

        foreach (var run in runs)
        {
            // Delete all messages in run
            await _messageRepository.DeleteByRunAsync(run.Id, ct);

            // Delete run
            await _runRepository.DeleteAsync(run.Id, ct);
        }

        // Delete chat
        await _chatRepository.DeleteAsync(command.ChatId, ct);

        _logger.LogCritical(
            "Chat permanently purged: {ChatId}, Title={Title}, RunCount={RunCount}",
            chat.Id, chat.Title, runs.Count);

        return Result.Success<Unit, Error>(Unit.Value);
    }
}

// src/AgenticCoder.Application/Chat/Queries/ShowChatQuery.cs
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

// src/AgenticCoder.Application/Chat/Handlers/ShowChatHandler.cs
public sealed class ShowChatHandler : IQueryHandler<ShowChatQuery, ChatDetails>
{
    private readonly IChatRepository _repository;
    private readonly IRunRepository _runRepository;

    public ShowChatHandler(IChatRepository repository, IRunRepository runRepository)
    {
        _repository = repository;
        _runRepository = runRepository;
    }

    public async Task<Result<ChatDetails, Error>> Handle(
        ShowChatQuery query,
        CancellationToken ct)
    {
        var chat = await _repository.GetByIdAsync(query.ChatId, includeDeleted: true, ct);

        if (chat is null)
        {
            return Result.Failure<ChatDetails, Error>(new ChatNotFoundError(query.ChatId));
        }

        var runs = await _runRepository.ListByChatAsync(query.ChatId, ct);

        var details = new ChatDetails(
            chat.Id,
            chat.Title,
            chat.CreatedAt,
            chat.UpdatedAt,
            chat.Tags,
            chat.WorktreeId,
            chat.IsDeleted,
            runs.Count,
            chat.MessageCount);

        return Result.Success<ChatDetails, Error>(details);
    }
}
```

### CLI Command Implementation

```csharp
// src/AgenticCoder.CLI/Commands/ChatCommand.cs
using Spectre.Console;
using Spectre.Console.Cli;

namespace AgenticCoder.CLI.Commands;

[Description("Manage conversation chats")]
public sealed class ChatCommand : Command<ChatCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<subcommand>")]
        [Description("Subcommand: new|list|open|show|rename|delete|restore|purge|status")]
        public string Subcommand { get; init; } = string.Empty;

        [CommandOption("--json")]
        [Description("Output JSON format")]
        public bool Json { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        return settings.Subcommand switch
        {
            "new" => new CreateChatCommand().Execute(context, new CreateChatCommand.Settings()),
            "list" => new ListChatsCommand().Execute(context, new ListChatsCommand.Settings()),
            "open" => new OpenChatCommand().Execute(context, new OpenChatCommand.Settings()),
            "show" => new ShowChatCommand().Execute(context, new ShowChatCommand.Settings()),
            "rename" => new RenameChatCommand().Execute(context, new RenameChatCommand.Settings()),
            "delete" => new DeleteChatCommand().Execute(context, new DeleteChatCommand.Settings()),
            "restore" => new RestoreChatCommand().Execute(context, new RestoreChatCommand.Settings()),
            "purge" => new PurgeChatCommand().Execute(context, new PurgeChatCommand.Settings()),
            "status" => new StatusChatCommand().Execute(context, new StatusChatCommand.Settings()),
            _ => throw new InvalidOperationException($"Unknown subcommand: {settings.Subcommand}")
        };
    }
}

// src/AgenticCoder.CLI/Commands/CreateChatCommand.cs
public sealed class CreateChatCommand : Command<CreateChatCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[title]")]
        public string? Title { get; init; }

        [CommandOption("--worktree")]
        public bool BindWorktree { get; init; }

        [CommandOption("--auto-title")]
        public bool AutoTitle { get; init; }
    }

    private readonly IMediator _mediator;
    private readonly IAnsiConsole _console;

    public CreateChatCommand(IMediator mediator, IAnsiConsole console)
    {
        _mediator = mediator;
        _console = console;
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        var worktreeId = settings.BindWorktree
            ? WorktreeId.FromCurrentDirectory()
            : null;

        var command = new Application.Chat.Commands.CreateChatCommand(
            settings.Title,
            worktreeId,
            settings.AutoTitle);

        var result = _mediator.Send(command).GetAwaiter().GetResult();

        if (result.IsSuccess)
        {
            _console.MarkupLine($"[green]✓[/] Chat created: {result.Value}");
            return 0;
        }
        else
        {
            _console.MarkupLine($"[red]✗[/] {result.Error.Message}");
            return 1;
        }
    }
}

// src/AgenticCoder.CLI/Commands/ListChatsCommand.cs
public sealed class ListChatsCommand : Command<ListChatsCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--archived")]
        public bool IncludeArchived { get; init; }

        [CommandOption("--filter <FILTER>")]
        public string? Filter { get; init; }

        [CommandOption("--sort <FIELD>")]
        public string SortBy { get; init; } = "updated";

        [CommandOption("--limit <COUNT>")]
        public int Limit { get; init; } = 50;

        [CommandOption("--offset <COUNT>")]
        public int Offset { get; init; } = 0;

        [CommandOption("--json")]
        public bool Json { get; init; }
    }

    private readonly IMediator _mediator;
    private readonly IAnsiConsole _console;

    public ListChatsCommand(IMediator mediator, IAnsiConsole console)
    {
        _mediator = mediator;
        _console = console;
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        var sortField = settings.SortBy.ToLowerInvariant() switch
        {
            "title" => ChatSortField.Title,
            "created" => ChatSortField.CreatedAt,
            "updated" => ChatSortField.UpdatedAt,
            _ => ChatSortField.UpdatedAt
        };

        var page = (settings.Offset / settings.Limit) + 1;

        var query = new ListChatsQuery(
            settings.IncludeArchived,
            settings.Filter,
            sortField,
            page,
            settings.Limit);

        var result = _mediator.Send(query).GetAwaiter().GetResult();

        if (result.IsSuccess)
        {
            if (settings.Json)
            {
                var json = JsonSerializer.Serialize(result.Value.Items);
                _console.WriteLine(json);
            }
            else
            {
                var table = new Table();
                table.AddColumn("ID");
                table.AddColumn("Title");
                table.AddColumn("Updated");
                table.AddColumn("Status");

                foreach (var chat in result.Value.Items)
                {
                    table.AddRow(
                        chat.Id.ToString().Substring(0, 12) + "...",
                        chat.Title,
                        chat.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                        chat.IsDeleted ? "[red]Archived[/]" : "[green]Active[/]");
                }

                _console.Write(table);
            }

            return 0;
        }
        else
        {
            _console.MarkupLine($"[red]✗[/] {result.Error.Message}");
            return 1;
        }
    }
}
```

### Error Codes

| Code | Meaning | Resolution |
|------|---------|------------|
| ACODE-CHAT-CMD-001 | Chat not found | Verify chat ID with `acode chat list` |
| ACODE-CHAT-CMD-002 | Invalid title | Title must be 1-500 characters, no special characters |
| ACODE-CHAT-CMD-003 | Operation cancelled | Confirmation declined, no changes made |
| ACODE-CHAT-CMD-004 | Chat already exists | Use different title or open existing chat |
| ACODE-CHAT-CMD-005 | Purge failed | Check cascading dependencies, verify permissions |
| ACODE-CHAT-CMD-006 | Validation error | Check command syntax with `--help` |
| ACODE-CHAT-CMD-007 | Repository error | Check database connection, file permissions |
| ACODE-CHAT-CMD-008 | Unauthorized access | Chat belongs to different workspace |

### Implementation Checklist

1. [ ] Create all command records (CreateChatCommand, DeleteChatCommand, etc.)
2. [ ] Create all query records (ListChatsQuery, ShowChatQuery, etc.)
3. [ ] Implement all command handlers with business logic
4. [ ] Implement all query handlers with read operations
5. [ ] Add validation logic in handlers (title length, chat exists)
6. [ ] Implement ChatCommand CLI router
7. [ ] Implement individual CLI commands (CreateChatCommand, ListChatsCommand, etc.)
8. [ ] Add confirmation prompts for delete/purge operations
9. [ ] Add JSON output mode support
10. [ ] Add human-readable table output for list command
11. [ ] Add structured logging in all handlers
12. [ ] Register handlers in DI container
13. [ ] Write unit tests for all handlers (20+ tests)
14. [ ] Write integration tests for command workflows
15. [ ] Write E2E tests for full lifecycle
16. [ ] Add rate limiting for purge operations
17. [ ] Add workspace authorization checks
18. [ ] Add input sanitization for chat titles
19. [ ] Add performance benchmarks
20. [ ] Write user documentation for all commands

### Rollout Plan

1. **Phase 1: Application Layer** (Week 1-2)
   - Create command/query records
   - Implement handlers with business logic
   - Add validation and error handling
   - Write unit tests for handlers

2. **Phase 2: CLI Integration** (Week 2)
   - Implement ChatCommand router
   - Implement subcommands (new, list, open, etc.)
   - Add output formatting (tables, JSON)
   - Add --help documentation

3. **Phase 3: Confirmations & Safety** (Week 3)
   - Add confirmation prompts for destructive operations
   - Implement double confirmation for purge
   - Add --force flag support
   - Add rate limiting for purge

4. **Phase 4: Security** (Week 3)
   - Add workspace authorization
   - Add input sanitization
   - Add log injection prevention
   - Security audit

5. **Phase 5: Testing** (Week 4)
   - Write integration tests
   - Write E2E tests
   - Performance benchmarks
   - User acceptance testing

6. **Phase 6: Documentation** (Week 4)
   - User manual for all commands
   - Error code reference
   - Troubleshooting guide
   - CLI reference documentation

---

**End of Task 049.b Specification**