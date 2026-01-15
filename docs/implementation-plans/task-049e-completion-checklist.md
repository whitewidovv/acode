# Task-049e Completion Checklist: Retention, Export, Privacy + Redaction

**Status:** ❌ 0% COMPLETE - READY FOR IMPLEMENTATION

**Date:** 2026-01-15
**Created By:** Claude Code
**Methodology:** Gap analysis to checklist per CLAUDE.md Section 3.2
**Gap Analysis Source:** task-049e-semantic-gap-analysis.md (all 115 ACs documented)

---

## CRITICAL: READ THIS FIRST

### STRUCTURE

This checklist is built FROM the gap analysis (task-049e-semantic-gap-analysis.md) while fresh in context.

All 115 ACs are organized into 4 feature domains with clear implementation phases:
- **Retention:** 20 ACs (policy engine, enforcement, notifications) - 10 hours
- **Export:** 25 ACs (formats, filtering, compression) - 12 hours
- **Privacy:** 15 ACs (4-level model, configuration, transitions) - 8 hours
- **Redaction:** 55+ ACs (patterns, custom rules, behavior) - 10 hours

### NO BLOCKING DEPENDENCIES ✅

Can implement all 4 domains independently and in any order. Recommended order: Retention → Privacy → Export → Redaction (allows privacy constraints to inform export behavior).

### HOW TO USE THIS CHECKLIST

#### For Fresh-Context Agent:

1. **Read task-049e-semantic-gap-analysis.md completely** (all 115 ACs listed with status)
2. **Read Section 2 below** (AC mapping to implementation components)
3. **Follow Phases 1-4 sequentially** in TDD order
4. **For Each Gap:**
   - Write test(s) that fail (RED)
   - Implement minimum code to pass (GREEN)
   - Refactor while keeping tests green
5. **Mark Progress:** `[ ]` = not started, `[🔄]` = in progress, `[✅]` = complete
6. **After Each Phase:** Run `dotnet test` and verify all tests pass
7. **Commit after each logical unit:** `git commit -m "feat(task-049e): ..."`

#### For Continuing Agent:

1. Find last `[✅]` item
2. Read next `[🔄]` or `[ ]` item
3. Follow same TDD cycle
4. Update checklist with evidence

---

## SECTION 1: WHAT EXISTS

**Nothing for task-049e yet.** No retention, export, privacy, or redaction systems exist in codebase.

**Can reference:**
- Chat/Run/Message domain entities (from 049a)
- SQLite/PostgreSQL repositories (from 049a, 049f)
- CLI command structure (from 049b)

---

## SECTION 2: IMPLEMENTATION FILES NEEDED (18 production files)

### Application Layer - Domain Models & Interfaces (6 files)

```
src/Acode.Application/Retention/
├── IRetentionPolicy.cs                    [GAP 1]
├── RetentionPolicyEngine.cs               [GAP 2]
└── RetentionConfig.cs                     [Domain model]

src/Acode.Application/Export/
├── IExportService.cs                      [GAP 3]
├── ExportFormatter.cs                     [GAP 4]
└── ExportOptions.cs                       [Domain model]

src/Acode.Application/Privacy/
├── IPrivacyService.cs                     [GAP 5]
└── PrivacyLevel.cs                        [Enum: LOCAL_ONLY, REDACTED, METADATA_ONLY, FULL]

src/Acode.Application/Redaction/
├── IRedactionEngine.cs                    [GAP 6]
└── RedactionPattern.cs                    [Domain model for custom patterns]
```

### Infrastructure Layer - Implementations (9 files)

```
src/Acode.Infrastructure/Retention/
├── RetentionBackgroundWorker.cs           [GAP 7] - Scheduled enforcement
├── RetentionEnforcer.cs                   [GAP 8] - Soft/hard delete logic
└── RetentionStatus.cs                     [Status model]

src/Acode.Infrastructure/Export/
├── ExporterFactory.cs                     [GAP 9] - Format factory
├── JsonExporter.cs                        [GAP 10] - JSON format implementation
├── MarkdownExporter.cs                    [GAP 11] - Markdown format implementation
└── TextExporter.cs                        [GAP 12] - Plain text format implementation

src/Acode.Infrastructure/Redaction/
├── PatternLibrary.cs                      [GAP 13] - Built-in patterns (Stripe, GitHub, AWS, JWT, passwords, private keys)
└── RedactionEngine.cs                     [GAP 14] - Matching + replacement logic
```

### CLI Layer - Commands (3 files)

```
src/Acode.Cli/Commands/
├── RetentionCommand.cs                    [GAP 15] - acode retention [policy|enforce|status]
├── ExportCommand.cs                       [GAP 16] - acode export <chat-id> [--format|--output|--compress|--encrypt|--redact]
├── PrivacyCommand.cs                      [GAP 17] - acode chat privacy <id> <level>
└── RedactionCommand.cs                    [GAP 18] - acode redaction [patterns|test]
```

**Total: 18 production files**

---

## SECTION 3: ACCEPTANCE CRITERIA BY FEATURE DOMAIN

### PHASE 1: RETENTION ENGINE (10 hours, 20 ACs)

#### Gap 1: IRetentionPolicy Interface [ ]

**File:** `src/Acode.Application/Retention/IRetentionPolicy.cs`
**ACs Covered:** AC-001-007
**Status:** [ ] PENDING
**Effort:** 0.5 hours

**What to Implement:**

```csharp
namespace Acode.Application.Retention;

public interface IRetentionPolicy
{
    /// <summary>
    /// Get retention period for chat (in days)
    /// </summary>
    Task<int> GetRetentionDaysAsync(ChatId chatId, CancellationToken ct);

    /// <summary>
    /// Check if chat should be retained (active chats exempt by default)
    /// </summary>
    Task<bool> ShouldRetainAsync(ChatId chatId, CancellationToken ct);

    /// <summary>
    /// Get grace period before hard deletion (in days)
    /// </summary>
    int GetGracePeriodDays { get; }

    /// <summary>
    /// Validate retention period (min 7 days, max unlimited)
    /// </summary>
    bool ValidateRetentionDays(int days);
}

public sealed record RetentionConfig(
    int DefaultRetentionDays = 365,
    int MinimumRetentionDays = 7,
    int GracePeriodDays = 7,
    bool RetainActiveChats = true,
    TimeSpan BackgroundJobInterval = default);
```

**Spec Reference:** Lines 1520-1537 (Retention requirements)

**Tests (3):**
- [ ] DEFAULT retention = 365 days (AC-001)
- [ ] MINIMUM retention = 7 days enforced (AC-003)
- [ ] Active chats exempt by default (AC-005)

**Success Criteria:**
- [ ] Interface compiles
- [ ] RetentionConfig immutable
- [ ] 3 tests passing

---

#### Gap 2: RetentionPolicyEngine [ ]

**File:** `src/Acode.Application/Retention/RetentionPolicyEngine.cs`
**ACs Covered:** AC-001-015
**Status:** [ ] PENDING
**Effort:** 2 hours

**What to Implement:**

```csharp
namespace Acode.Application.Retention;

public sealed class RetentionPolicyEngine : IRetentionPolicy
{
    private readonly IChatRepository _chatRepo;
    private readonly IRetentionStore _store;
    private readonly RetentionConfig _config;

    public int GetGracePeriodDays => _config.GracePeriodDays;

    public RetentionPolicyEngine(
        IChatRepository chatRepo,
        IRetentionStore store,
        RetentionConfig config)
    {
        _chatRepo = chatRepo ?? throw new ArgumentNullException(nameof(chatRepo));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Get retention days for specific chat (supports per-chat override - AC-006)
    /// </summary>
    public async Task<int> GetRetentionDaysAsync(ChatId chatId, CancellationToken ct)
    {
        // Check for per-chat override first
        var chatOverride = await _store.GetChatOverrideAsync(chatId, ct);
        if (chatOverride.HasValue)
            return chatOverride.Value;

        // Fall back to default
        return _config.DefaultRetentionDays;
    }

    /// <summary>
    /// Check if chat should be retained (AC-005: active chats exempt)
    /// </summary>
    public async Task<bool> ShouldRetainAsync(ChatId chatId, CancellationToken ct)
    {
        var chat = await _chatRepo.GetAsync(chatId, ct);
        if (chat == null)
            return false;

        // Active (non-archived) chats exempt from retention by default
        if (!chat.ArchivedAt.HasValue && _config.RetainActiveChats)
            return true;

        return false;
    }

    /// <summary>
    /// Validate retention period (AC-003: min 7 days, AC-004: max unlimited)
    /// </summary>
    public bool ValidateRetentionDays(int days)
    {
        if (days == -1)
            return true; // "never" (unlimited)

        return days >= _config.MinimumRetentionDays;
    }

    /// <summary>
    /// Identify expired chats for enforcement (AC-009: timestamp comparison)
    /// </summary>
    public async Task<IReadOnlyList<ChatId>> GetExpiredChatsAsync(CancellationToken ct)
    {
        var allChats = await _chatRepo.ListAsync(new ChatFilter(), ct);
        var expired = new List<ChatId>();

        foreach (var chat in allChats)
        {
            if (chat.IsDeleted)
                continue; // Already deleted

            // Skip active chats
            if (!chat.ArchivedAt.HasValue && _config.RetainActiveChats)
                continue;

            var archiveDate = chat.ArchivedAt ?? chat.CreatedAt;
            var retentionDays = await GetRetentionDaysAsync(chat.Id, ct);
            var expiryDate = archiveDate.AddDays(retentionDays);

            if (DateTimeOffset.UtcNow >= expiryDate)
            {
                expired.Add(chat.Id);
            }
        }

        return expired.AsReadOnly();
    }

    /// <summary>
    /// Get chats approaching expiry (AC-016: within 7 days)
    /// </summary>
    public async Task<IReadOnlyList<(ChatId, DateTimeOffset)>> GetExpiringChatsAsync(CancellationToken ct)
    {
        var allChats = await _chatRepo.ListAsync(new ChatFilter(), ct);
        var expiring = new List<(ChatId, DateTimeOffset)>();
        var warningThreshold = DateTimeOffset.UtcNow.AddDays(7);

        foreach (var chat in allChats)
        {
            if (chat.IsDeleted)
                continue;

            if (!chat.ArchivedAt.HasValue && _config.RetainActiveChats)
                continue;

            var archiveDate = chat.ArchivedAt ?? chat.CreatedAt;
            var retentionDays = await GetRetentionDaysAsync(chat.Id, ct);
            var expiryDate = archiveDate.AddDays(retentionDays);

            if (expiryDate <= warningThreshold && DateTimeOffset.UtcNow < expiryDate)
            {
                expiring.Add((chat.Id, expiryDate));
            }
        }

        return expiring.AsReadOnly();
    }
}

public interface IRetentionStore
{
    Task<int?> GetChatOverrideAsync(ChatId chatId, CancellationToken ct);
    Task SetChatOverrideAsync(ChatId chatId, int retentionDays, CancellationToken ct);
}
```

**Spec Reference:** Lines 1538-1610 (Enforcement requirements)

**Tests (5):**
- [ ] Expired chats identified (AC-009)
- [ ] Grace period respected (AC-010)
- [ ] Active chats skipped (AC-005)
- [ ] Per-chat override used (AC-006)
- [ ] Changes take effect immediately (AC-007)

**Success Criteria:**
- [ ] GetExpiredChatsAsync returns correct chats
- [ ] GetExpiringChatsAsync returns within 7 days
- [ ] Per-chat overrides work
- [ ] 5 tests passing

---

#### Gap 3: RetentionEnforcer [ ]

**File:** `src/Acode.Infrastructure/Retention/RetentionEnforcer.cs`
**ACs Covered:** AC-008-015, AC-019
**Status:** [ ] PENDING
**Effort:** 3 hours

**What to Implement:**

Soft-delete and hard-delete logic with cascade handling (AC-013):

```csharp
namespace Acode.Infrastructure.Retention;

public sealed class RetentionEnforcer
{
    private readonly IChatRepository _chatRepo;
    private readonly IRunRepository _runRepo;
    private readonly IMessageRepository _messageRepo;
    private readonly ISearchIndexService _searchIndex;
    private readonly IRetentionPolicy _policy;
    private readonly ILogger<RetentionEnforcer> _logger;

    public async Task EnforceAsync(CancellationToken ct)
    {
        // Find expired chats (AC-009)
        var expiredChats = await _policy.GetExpiredChatsAsync(ct);

        if (expiredChats.Count == 0)
            return;

        _logger.LogInformation("Enforcing retention on {Count} chats", expiredChats.Count);

        // Process in batches (AC-014: up to 100 per cycle)
        var batches = expiredChats.Batch(100);
        foreach (var batch in batches)
        {
            await ProcessBatchAsync(batch, ct);
        }
    }

    private async Task ProcessBatchAsync(IEnumerable<ChatId> chatIds, CancellationToken ct)
    {
        foreach (var chatId in chatIds)
        {
            try
            {
                var chat = await _chatRepo.GetAsync(chatId, ct);
                if (chat == null)
                    continue;

                // Check if already soft-deleted
                if (chat.DeletedAt.HasValue)
                {
                    // Check if grace period expired (hard delete)
                    var gracePeriodExpired = chat.DeletedAt.Value.AddDays(_policy.GetGracePeriodDays);
                    if (DateTimeOffset.UtcNow >= gracePeriodExpired)
                    {
                        await HardDeleteAsync(chatId, ct); // AC-012
                    }
                }
                else
                {
                    // Soft delete (AC-011: mark with deleted_at)
                    await SoftDeleteAsync(chatId, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enforcing retention on chat {ChatId}", chatId);
            }
        }
    }

    private async Task SoftDeleteAsync(ChatId chatId, CancellationToken ct)
    {
        var chat = await _chatRepo.GetAsync(chatId, ct);
        if (chat == null)
            return;

        chat.MarkDeleted();
        await _chatRepo.SaveAsync(chat, ct);

        _logger.LogInformation("Soft-deleted chat {ChatId} (AC-011)", chatId);
    }

    private async Task HardDeleteAsync(ChatId chatId, CancellationToken ct)
    {
        // Cascade deletion (AC-013): chats → runs → messages → search index
        var runs = await _runRepo.ListByChatsAsync(new[] { chatId }, ct);
        foreach (var run in runs)
        {
            var messages = await _messageRepo.ListByRunAsync(run.Id, ct);
            foreach (var message in messages)
            {
                await _messageRepo.DeleteAsync(message.Id, ct);
            }
            await _runRepo.DeleteAsync(run.Id, ct);
        }

        // Remove from search index
        await _searchIndex.RemoveChatAsync(chatId, ct);

        // Delete chat
        await _chatRepo.DeleteAsync(chatId, ct);

        _logger.LogInformation("Hard-deleted chat {ChatId} and cascade (AC-012)", chatId);
    }
}
```

**Spec Reference:** Lines 1589-1610 (Cascade deletion)

**Tests (5):**
- [ ] Soft-delete marks deleted_at (AC-011)
- [ ] Hard-delete removes data (AC-012)
- [ ] Cascade to runs/messages/index (AC-013)
- [ ] Batch processing (AC-014)
- [ ] Grace period respected (AC-010)

---

#### Gap 4: RetentionBackgroundWorker [ ]

**File:** `src/Acode.Infrastructure/Retention/RetentionBackgroundWorker.cs`
**ACs Covered:** AC-008, AC-015
**Status:** [ ] PENDING
**Effort:** 2 hours

**What to Implement:**

Background job scheduler (default: daily at 3 AM, AC-008):

```csharp
namespace Acode.Infrastructure.Retention;

public sealed class RetentionBackgroundWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RetentionConfig _config;
    private readonly ILogger<RetentionBackgroundWorker> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Retention background worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nextRun = CalculateNextRunTime();
                var delay = nextRun - DateTimeOffset.UtcNow;

                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, stoppingToken);
                }

                // Run enforcement (AC-008: configured schedule, default 3 AM daily)
                using var scope = _serviceProvider.CreateScope();
                var enforcer = scope.ServiceProvider.GetRequiredService<RetentionEnforcer>();
                await enforcer.EnforceAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in retention enforcement cycle");
            }
        }

        _logger.LogInformation("Retention background worker stopped");
    }

    private DateTimeOffset CalculateNextRunTime()
    {
        // Default: 3 AM daily (AC-008)
        var today = DateTimeOffset.UtcNow.Date;
        var scheduledTime = today.AddHours(3);

        if (DateTimeOffset.UtcNow >= scheduledTime)
        {
            return scheduledTime.AddDays(1);
        }

        return scheduledTime;
    }
}
```

**Spec Reference:** Lines 1597-1610 (Background job requirement)

**Tests (2):**
- [ ] Worker runs on schedule (AC-008)
- [ ] Manual trigger works (AC-015)

---

#### Gap 5: CLI RetentionCommand [ ]

**File:** `src/Acode.Cli/Commands/RetentionCommand.cs`
**ACs Covered:** AC-015, AC-019
**Status:** [ ] PENDING
**Effort:** 2 hours

**What to Implement:**

Commands:
- `acode retention enforce --now` (AC-015: manual trigger)
- `acode retention status` (AC-019: expiry summary)
- `acode retention policy set --days 180` (AC-002: configurable)

**Tests (3):**
- [ ] Manual enforcement executes
- [ ] Status shows expiring chats
- [ ] Policy can be configured

---

### PHASE 2: PRIVACY CONTROLS (8 hours, 15 ACs)

#### Gap 6: PrivacyLevel Enum & IPrivacyService [ ]

**File:** `src/Acode.Application/Privacy/IPrivacyService.cs`
**ACs Covered:** AC-046-060
**Status:** [ ] PENDING
**Effort:** 2 hours

**What to Implement:**

4-level privacy model (AC-046-050):

```csharp
namespace Acode.Application.Privacy;

public enum PrivacyLevel
{
    LocalOnly = 0,      // AC-046: No remote sync
    Redacted = 1,       // AC-047: Sync with secrets removed
    MetadataOnly = 2,   // AC-048: Titles/tags/timestamps only
    Full = 3            // AC-049: All content (with warning)
}

public interface IPrivacyService
{
    /// <summary>
    /// Get privacy level for chat (AC-052)
    /// </summary>
    Task<PrivacyLevel> GetLevelAsync(ChatId chatId, CancellationToken ct);

    /// <summary>
    /// Set privacy level for chat (AC-051)
    /// </summary>
    Task SetLevelAsync(ChatId chatId, PrivacyLevel level, CancellationToken ct);

    /// <summary>
    /// Transition is allowed? (AC-056-058: enforce transitions)
    /// </summary>
    bool CanTransition(PrivacyLevel from, PrivacyLevel to, bool hasConfirmation = false);

    /// <summary>
    /// Apply privacy filtering to chat content before sync
    /// </summary>
    Task<Chat> ApplyPrivacyFilterAsync(Chat chat, PrivacyLevel level, CancellationToken ct);
}
```

**Spec Reference:** Lines 1728-1799 (Privacy requirements)

**Tests (4):**
- [ ] Default = LOCAL_ONLY (AC-050)
- [ ] Transitions enforced (AC-056-058)
- [ ] Per-chat level settable (AC-051)
- [ ] Filtering applied correctly

---

#### Gap 7: PrivacyService Implementation [ ]

**File:** `src/Acode.Infrastructure/Privacy/PrivacyService.cs`
**ACs Covered:** AC-046-060
**Status:** [ ] PENDING
**Effort:** 6 hours

**What to Implement:**

- Per-chat privacy configuration (AC-051-054)
- Level transitions with validation (AC-056-058)
- Audit logging (AC-059)
- Filtering by level (AC-053)
- Bulk updates (AC-054)

---

### PHASE 3: EXPORT SYSTEM (12 hours, 25 ACs)

#### Gap 8-11: Export Implementation [ ]

**Files:**
- `src/Acode.Application/Export/IExportService.cs` (GAP 8)
- `src/Acode.Infrastructure/Export/JsonExporter.cs` (GAP 9)
- `src/Acode.Infrastructure/Export/MarkdownExporter.cs` (GAP 10)
- `src/Acode.Infrastructure/Export/TextExporter.cs` (GAP 11)

**ACs Covered:** AC-021-045
**Status:** [ ] PENDING
**Effort:** 12 hours

**What to Implement:**

- 3 format exporters (JSON, Markdown, Text) with metadata headers (AC-027)
- Content filtering (single, all, date range, tags) (AC-028-033)
- Output options (file, stdout, compression, encryption) (AC-035-039)
- Preview mode (AC-034)
- Progress reporting (AC-037)
- Overwrite protection (AC-040)

**Tests (8+ unit, 6+ integration):**
- Each format produces valid output
- Filters work correctly
- Compression/encryption options work
- Redaction integration works (AC-041-045)

---

### PHASE 4: REDACTION ENGINE (10 hours, 55+ ACs)

#### Gap 12: PatternLibrary [ ]

**File:** `src/Acode.Infrastructure/Redaction/PatternLibrary.cs`
**ACs Covered:** AC-061-067
**Status:** [ ] PENDING
**Effort:** 2 hours

**What to Implement:**

Built-in pattern definitions:

```csharp
namespace Acode.Infrastructure.Redaction;

public sealed class PatternLibrary
{
    public static readonly RedactionPattern StripeApiKey = new(
        Name: "Stripe API Key",
        Pattern: @"sk_live_[a-zA-Z0-9]{24,}",
        Replacement: "[REDACTED-StripeKey-sk_***]"
    ); // AC-061

    public static readonly RedactionPattern GitHubToken = new(
        Name: "GitHub Token",
        Pattern: @"gh[ps]_[a-zA-Z0-9]{36,}",
        Replacement: "[REDACTED-GitHubToken-gh***]"
    ); // AC-062

    public static readonly RedactionPattern AwsAccessKey = new(
        Name: "AWS Access Key",
        Pattern: @"AKIA[A-Z0-9]{16}",
        Replacement: "[REDACTED-AWSKey-AKIA***]"
    ); // AC-063

    public static readonly RedactionPattern JwtToken = new(
        Name: "JWT Token",
        Pattern: @"eyJ[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+",
        Replacement: "[REDACTED-JWT-eyJ***]"
    ); // AC-064

    public static readonly RedactionPattern Password = new(
        Name: "Password Field",
        Pattern: @"(password|passwd|pwd)[=:\s]+\S{8,}",
        Replacement: "[REDACTED-Password-***]"
    ); // AC-065

    public static readonly RedactionPattern PrivateKey = new(
        Name: "Private Key Block",
        Pattern: @"-----BEGIN.*PRIVATE KEY-----[\s\S]*?-----END.*PRIVATE KEY-----",
        Replacement: "[REDACTED-PrivateKey-BEGIN***]"
    ); // AC-066

    public IReadOnlyList<RedactionPattern> GetBuiltInPatterns() => new[]
    {
        StripeApiKey,
        GitHubToken,
        AwsAccessKey,
        JwtToken,
        Password,
        PrivateKey
    }; // AC-067: All enabled by default
}

public sealed record RedactionPattern(
    string Name,
    string Pattern,
    string Replacement);
```

**Tests (3):**
- All 6 patterns compile
- Patterns match expected strings
- Patterns enabled by default (AC-067)

---

#### Gap 13: RedactionEngine [ ]

**File:** `src/Acode.Infrastructure/Redaction/RedactionEngine.cs`
**ACs Covered:** AC-075-080
**Status:** [ ] PENDING
**Effort:** 4 hours

**What to Implement:**

Pattern matching and replacement with:
- Deterministic output (AC-078)
- Recursive nested redaction (AC-079)
- Placeholder with partial info (AC-076)
- Match logging (AC-080)

```csharp
public sealed class RedactionEngine : IRedactionEngine
{
    private readonly List<RedactionPattern> _patterns;
    private readonly ILogger<RedactionEngine> _logger;

    public string Redact(string content)
    {
        var redacted = content;
        var matchStats = new Dictionary<string, int>();

        foreach (var pattern in _patterns)
        {
            var regex = new Regex(pattern.Pattern);
            var matches = regex.Matches(content);

            if (matches.Count > 0)
            {
                matchStats[pattern.Name] = matches.Count;

                // Replace all matches (AC-077: multiple matches)
                redacted = regex.Replace(redacted, m =>
                {
                    // AC-076: Placeholder preserves first 10 chars for debugging
                    var prefix = m.Value.Length > 10 ? m.Value.Substring(0, 10) : m.Value;
                    return $"[REDACTED-{pattern.Name}-{prefix}***]";
                });
            }
        }

        // AC-080: Log match statistics
        if (matchStats.Count > 0)
        {
            _logger.LogInformation("Redaction applied: {Patterns}",
                string.Join(", ", matchStats.Select(x => $"{x.Key}({x.Value})")));
        }

        return redacted; // AC-078: Deterministic
    }

    public async Task<string> RedactAsync(string content, CancellationToken ct)
    {
        return await Task.Run(() => Redact(content), ct);
    }
}
```

**Tests (6):**
- All patterns matched and replaced
- Deterministic output (same input = same output)
- Nested content redacted (AC-079)
- Partial info preserved (AC-076)
- Multiple matches all redacted (AC-077)
- Statistics logged (AC-080)

---

#### Gap 14: Custom Pattern Management [ ]

**File:** `src/Acode.Application/Redaction/RedactionPatternService.cs`
**ACs Covered:** AC-068-074
**Status:** [ ] PENDING
**Effort:** 4 hours

**What to Implement:**

- Custom patterns in config (AC-068)
- Pattern validation (AC-070)
- Pattern limit (AC-071: max 50)
- Pattern test command (AC-072)
- Pattern list command (AC-073)
- Pattern removal (AC-074)

Commands:
- `acode redaction patterns list`
- `acode redaction patterns remove <name>`
- `acode redaction test --pattern <regex> --text <sample>`

**Tests (6):**
- Patterns validated before save
- Max 50 patterns enforced
- Pattern testing works
- List and remove work

---

#### Gap 15: ExportCommand [ ]

**File:** `src/Acode.Cli/Commands/ExportCommand.cs`
**ACs Covered:** AC-021-045, AC-103
**Status:** [ ] PENDING
**Effort:** 3 hours

**What to Implement:**

CLI command: `acode export <chat-id>` with options:
- `--format json|markdown|text` (AC-026)
- `--output /path/to/file` (AC-035)
- `--all` (AC-029)
- `--since 2025-01-01` or `--since 7d` (AC-030-031)
- `--until 2025-12-31` (AC-030)
- `--tag <tagname>` (AC-032)
- `--redact` (AC-041)
- `--compress` (AC-038)
- `--encrypt` (AC-039)
- `--preview` (AC-034)

**Tests (4):**
- [ ] Single chat export works (AC-028)
- [ ] All chats export works (AC-029)
- [ ] Filters applied correctly (AC-030-034)
- [ ] Redaction integration works (AC-041-045)

---

#### Gap 16: PrivacyCommand [ ]

**File:** `src/Acode.Cli/Commands/PrivacyCommand.cs`
**ACs Covered:** AC-051-055, AC-104-105
**Status:** [ ] PENDING
**Effort:** 2 hours

**What to Implement:**

CLI commands:
- `acode chat privacy <id> <level>` (AC-051, AC-104) - Set per-chat level
- `acode chat privacy --all <level>` (AC-054) - Bulk update
- `acode privacy status` (AC-105) - Show privacy status

Must enforce transitions (AC-056-060):
- LOCAL_ONLY → others blocked unless --force flag (AC-056)
- REDACTED → FULL requires --confirm-expose-data (AC-057)
- Any → LOCAL_ONLY always allowed (AC-058)

**Tests (3):**
- [ ] Per-chat level settable (AC-051)
- [ ] Bulk update works (AC-054)
- [ ] Transition rules enforced (AC-056-058)

---

#### Gap 17: RedactionCommand [ ]

**File:** `src/Acode.Cli/Commands/RedactionCommand.cs`
**ACs Covered:** AC-061-085, AC-106-107
**Status:** [ ] PENDING
**Effort:** 2 hours

**What to Implement:**

CLI commands:
- `acode redaction preview <chat-id>` (AC-081, AC-106) - Show what would be redacted
- `acode redaction patterns list` (AC-073, AC-107) - Show all patterns (built-in + custom)
- `acode redaction patterns remove <name>` (AC-074) - Remove custom pattern
- `acode redaction test --pattern <regex> --text <sample>` (AC-072) - Test pattern

**Tests (4):**
- [ ] Preview shows matches (AC-081-085)
- [ ] Pattern list works (AC-073)
- [ ] Pattern removal works (AC-074)
- [ ] Pattern testing works (AC-072)

---

## SECTION 4: VERIFICATION CHECKLIST

**After all 4 phases complete, verify all 115 ACs:**

### Retention (AC-001-020): [ ]
- [ ] Policy configuration (AC-001-007)
- [ ] Enforcement (AC-008-015)
- [ ] Warnings (AC-016-020)

### Privacy (AC-046-060): [ ]
- [ ] 4 privacy levels defined (AC-046-050)
- [ ] Per-chat configuration (AC-051-055)
- [ ] Transition enforcement (AC-056-060)

### Export (AC-021-045): [ ]
- [ ] JSON format (AC-021-022)
- [ ] Markdown format (AC-023-024)
- [ ] Plain text format (AC-025-027)
- [ ] Content filters (AC-028-034)
- [ ] Output options (AC-035-040)
- [ ] Redaction integration (AC-041-045)

### Redaction (AC-061-080+): [ ]
- [ ] 6 built-in patterns (AC-061-067)
- [ ] Custom patterns (AC-068-074)
- [ ] Redaction behavior (AC-075-080)

---

## GIT WORKFLOW

**Commit after each gap completes:**

```bash
# Phase 1
git commit -m "test(task-049e): add retention policy tests"
git commit -m "feat(task-049e): implement IRetentionPolicy interface"
git commit -m "feat(task-049e): implement RetentionPolicyEngine"
git commit -m "feat(task-049e): implement RetentionEnforcer"
git commit -m "feat(task-049e): implement RetentionBackgroundWorker"
git commit -m "feat(task-049e): add retention CLI commands"

# Phase 2 (similar pattern)
git commit -m "feat(task-049e): implement privacy levels and service"
git commit -m "feat(task-049e): add privacy CLI commands"

# Phase 3
git commit -m "feat(task-049e): implement JSON/Markdown/Text exporters"
git commit -m "feat(task-049e): add export CLI commands"

# Phase 4
git commit -m "feat(task-049e): implement redaction engine with built-in patterns"
git commit -m "feat(task-049e): implement custom pattern management"

# Final
git commit -m "feat(task-049e): complete retention/export/privacy/redaction

- Retention: 20 ACs (policy engine, enforcement, notifications)
- Export: 25 ACs (JSON/Markdown/Text, filters, compression)
- Privacy: 15 ACs (4-level model, per-chat config, transitions)
- Redaction: 55+ ACs (built-in patterns, custom rules, behavior)

Total: 115 ACs, 18 production files, 50+ tests

🤖 Generated with Claude Code"
```

---

**Next Action:** Begin Phase 1 (Gaps 1-5) with task-049e-completion-checklist in hand and gap-analysis context fresh.

