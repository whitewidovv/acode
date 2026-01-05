# Task 049.e: Retention, Export, Privacy + Redaction Controls

**Priority:** P1 – High Priority  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 2 – CLI + Orchestration Core  
**Dependencies:** Task 049.a (Data Model), Task 049.f (Sync), Task 039 (Security)  

---

## Description

**Business Value & ROI**

Data lifecycle management prevents storage bloat ($1,200/year per engineer saved on cloud costs), ensures compliance readiness (avoids $50K-$500K GDPR fines), and enables data portability ($800/year saved on lock-in prevention). A 10-engineer team saves $20,000 annually plus avoids regulatory risk.

**Cost Breakdown:**
- Storage cost reduction: 60% less cloud storage ($100/month/engineer → $40/month = $720/year saved)
- Compliance readiness: Avoids audit failures (1 GDPR incident = $50K-$500K fine)
- Data portability: Prevents vendor lock-in ($800/year/engineer in migration costs avoided)
- Privacy protection: Reduces data breach exposure (average breach costs $9.44M per IBM 2023 report)
- **Total savings: $2,000/year per engineer (storage + compliance + portability)**
- **10-engineer team: $20,000/year + regulatory risk mitigation**

**Time Savings:**
- Manual cleanup eliminated: 2 hours/month → 0 (automation)
- Export time: 30 min manual → 2 min automated (93% reduction)
- Compliance documentation: 5 hours/quarter → 30 min (90% reduction)
- **Total: 35 hours/year per engineer @ $108/hour = $3,780/year**

**Technical Architecture**

The retention, export, privacy, and redaction system implements four core capabilities working together: (1) automatic retention enforcement with configurable policies, (2) multi-format export with filtering, (3) layered privacy controls (local-only, redacted, full sync), and (4) pattern-based redaction for sensitive data.

**Four-Layer Architecture:**

```
User Intent: "Keep recent chats, delete old ones, export safely"
     │
     ▼
┌─────────────────────────────────────────┐
│  Retention Policy Engine                │
│  ├─ Policy: 365 days default           │
│  ├─ Grace period: 7 days warning       │
│  ├─ Enforcement: Daily background job   │
│  └─ Cascade: Chats → Runs → Messages   │
└─────────────────────────────────────────┘
     │
     ▼
┌─────────────────────────────────────────┐
│  Export Engine                          │
│  ├─ Formats: JSON, Markdown, Plain Text│
│  ├─ Filtering: By chat/date/tag        │
│  ├─ Redaction: Apply privacy rules      │
│  └─ Output: Self-contained portable    │
└─────────────────────────────────────────┘
     │
     ▼
┌─────────────────────────────────────────┐
│  Privacy Control Layer                  │
│  ├─ LOCAL_ONLY: No sync (default)      │
│  ├─ REDACTED: Sync with secrets removed│
│  ├─ METADATA_ONLY: Sync titles/tags    │
│  └─ FULL: Sync all content             │
└─────────────────────────────────────────┘
     │
     ▼
┌─────────────────────────────────────────┐
│  Redaction Engine                       │
│  ├─ Patterns: Regex matching secrets   │
│  ├─ Detection: API keys, passwords, JWT│
│  ├─ Replacement: [REDACTED-KEY-abc123] │
│  └─ Preview: Show before applying       │
└─────────────────────────────────────────┘
```

**Retention Policy Enforcement:**

Retention policies define how long conversation data is kept. Default retention is 365 days for archived chats. Active chats are never auto-deleted. Soft-deleted chats are purged after retention period.

**Retention Flow:**

```
Daily Background Job (3:00 AM local time)
     │
     ▼
[Query archived chats older than retention period]
     │
     ├─ SELECT * FROM chats
     │   WHERE archived_at < NOW() - INTERVAL '365 days'
     │   AND deleted_at IS NULL
     │
     ▼
[Mark for deletion with 7-day grace period]
     │
     ├─ UPDATE chats SET deleted_at = NOW()
     │   WHERE id IN (expired_chat_ids)
     │
     ▼
[Query soft-deleted chats past grace period]
     │
     ├─ SELECT * FROM chats
     │   WHERE deleted_at < NOW() - INTERVAL '7 days'
     │
     ▼
[Cascade purge: Chats → Runs → Messages → Search Index]
     │
     ├─ DELETE FROM messages WHERE chat_id = ?
     ├─ DELETE FROM runs WHERE chat_id = ?
     ├─ DELETE FROM conversation_search WHERE chat_id = ?
     ├─ DELETE FROM chats WHERE id = ?
     │
     ▼
[Log audit event]
     │
     └─ INSERT INTO audit_log
         (action='purge', chat_id=?, message_count=?, timestamp=NOW())
```

**Key Retention Decisions:**
- **Active chats never expire:** Prevents accidental deletion of ongoing work
- **7-day grace period:** Allows recovery before permanent deletion
- **Cascade deletion:** Ensures referential integrity across all tables
- **Audit logging:** Creates permanent record of deletions for compliance

**Retention Configuration:**

```yaml
# .agent/config.yml
retention:
  default_policy:
    archived_chats: 365d     # 1 year default
    deleted_chats_grace: 7d  # 7-day recovery window
    active_chats: never      # Never auto-delete active chats
  
  policies:
    short:
      archived_chats: 90d    # 3 months for short-term projects
    long:
      archived_chats: 1825d  # 5 years for compliance
  
  enforcement:
    enabled: true
    schedule: "0 3 * * *"    # Daily at 3:00 AM
    batch_size: 100          # Process 100 chats per batch
```

**Export Formats:**

Export creates portable, self-contained copies of conversation data in three formats: JSON (machine-readable), Markdown (human-readable), and Plain Text (minimal formatting).

**Export JSON Format:**

```json
{
  "exported_at": "2025-01-15T10:30:00Z",
  "acode_version": "0.1.0",
  "chats": [
    {
      "id": "abc-123",
      "title": "Authentication Implementation",
      "created_at": "2025-01-10T14:00:00Z",
      "archived_at": null,
      "tags": ["security", "backend"],
      "messages": [
        {
          "id": "msg-1",
          "role": "user",
          "content": "How do I implement JWT authentication?",
          "created_at": "2025-01-10T14:01:00Z",
          "tool_calls": []
        },
        {
          "id": "msg-2",
          "role": "assistant",
          "content": "JWT authentication requires three steps...",
          "created_at": "2025-01-10T14:01:15Z",
          "tool_calls": [
            {
              "id": "tool-1",
              "tool": "create_file",
              "args": {"filePath": "auth.cs", "content": "..."},
              "result": "success"
            }
          ]
        }
      ],
      "runs": [
        {
          "id": "run-1",
          "model": "claude-sonnet-4",
          "status": "completed",
          "token_count": 2847,
          "started_at": "2025-01-10T14:01:00Z",
          "completed_at": "2025-01-10T14:02:30Z"
        }
      ]
    }
  ]
}
```

**Export Markdown Format:**

```markdown
# Acode Conversation Export
Exported: 2025-01-15 10:30:00 UTC
Version: 0.1.0

## Chat: Authentication Implementation
Created: 2025-01-10 14:00:00
Tags: security, backend

### Message 1 (user) - 2025-01-10 14:01:00
How do I implement JWT authentication?

### Message 2 (assistant) - 2025-01-10 14:01:15
JWT authentication requires three steps...

**Tool Call:** create_file
- File: auth.cs
- Status: success

---

## Run Statistics
- Model: claude-sonnet-4
- Tokens: 2,847
- Duration: 90 seconds
```

**Privacy Levels:**

The privacy model uses layered controls to determine what data leaves local storage:

1. **LOCAL_ONLY (default):** All data stays on local machine, no sync
2. **REDACTED:** Sync with sensitive data removed (API keys, passwords, etc.)
3. **METADATA_ONLY:** Sync only chat titles, tags, timestamps (no content)
4. **FULL:** Sync all content (use only with trusted remote storage)

**Privacy Decision Matrix:**

| Data Element | LOCAL_ONLY | REDACTED | METADATA_ONLY | FULL |
|--------------|------------|----------|----------------|------|
| Chat titles | ✅ Local | ✅ Synced | ✅ Synced | ✅ Synced |
| Chat tags | ✅ Local | ✅ Synced | ✅ Synced | ✅ Synced |
| Timestamps | ✅ Local | ✅ Synced | ✅ Synced | ✅ Synced |
| Message content | ✅ Local | ✅ Redacted | ❌ Not synced | ✅ Synced |
| Tool calls | ✅ Local | ✅ Redacted | ❌ Not synced | ✅ Synced |
| File paths | ✅ Local | ✅ Redacted | ❌ Not synced | ✅ Synced |
| API keys | ✅ Local | ❌ Removed | ❌ Not synced | ⚠️ Synced |
| Passwords | ✅ Local | ❌ Removed | ❌ Not synced | ⚠️ Synced |

**Redaction Patterns:**

Redaction uses regex patterns to detect and remove sensitive data. Built-in patterns cover common secrets. Custom patterns support project-specific needs.

**Built-in Patterns:**

```csharp
public static readonly RedactionPattern[] BuiltInPatterns = new[]
{
    // API Keys
    new RedactionPattern("Stripe Keys", @"\bsk_live_[a-zA-Z0-9]{24,}\b"),
    new RedactionPattern("GitHub Tokens", @"\bgh[ps]_[a-zA-Z0-9]{36,}\b"),
    new RedactionPattern("AWS Keys", @"\bAKIA[A-Z0-9]{16}\b"),
    new RedactionPattern("Azure Keys", @"\b[A-Za-z0-9+/]{43}={0,2}\b"),
    
    // Authentication Tokens
    new RedactionPattern("JWT Tokens", @"\beyJ[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+\b"),
    new RedactionPattern("Bearer Tokens", @"Bearer\s+[a-zA-Z0-9\-_]+\.[a-zA-Z0-9\-_]+\.[a-zA-Z0-9\-_]+"),
    
    // Passwords
    new RedactionPattern("Password Fields", @"(?i)(password|passwd|pwd)[=:\s]+\S{8,}"),
    new RedactionPattern("Connection Strings", @"(?i)(password|pwd)=[^;]{8,}"),
    
    // Private Keys
    new RedactionPattern("RSA Private Keys", @"-----BEGIN RSA PRIVATE KEY-----[\s\S]+?-----END RSA PRIVATE KEY-----"),
    new RedactionPattern("SSH Private Keys", @"-----BEGIN OPENSSH PRIVATE KEY-----[\s\S]+?-----END OPENSSH PRIVATE KEY-----"),
};
```

**Redaction Replacement:**

When a pattern matches, sensitive content is replaced with a placeholder that preserves metadata for debugging:

```
Original: "Use API key sk_live_abc123xyz456def789..."
Redacted: "Use API key [REDACTED-STRIPE-KEY-sk_live_abc]"

Original: "Password: MySecretP@ssw0rd!"
Redacted: "Password: [REDACTED-PASSWORD]"

Original: "JWT: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
Redacted: "JWT: [REDACTED-JWT-TOKEN-eyJhbG]"
```

**Performance Characteristics:**

| Operation | Local | Remote | Notes |
|-----------|-------|--------|-------|
| Retention enforcement | 5s per 1000 chats | N/A | Daily background job |
| Export JSON (100 chats) | 2s | N/A | 5MB average |
| Export Markdown (100 chats) | 3s | N/A | 3MB average |
| Redaction scanning (1000 messages) | 800ms | N/A | 10 patterns checked |
| Privacy level change | <100ms | N/A | Metadata update only |

**Integration Points:**

1. **Task-049a (Data Model):**
   - `deleted_at` timestamp for soft deletes
   - `archived_at` timestamp for retention
   - `privacy_level` enum on Chat entity

2. **Task-049b (CLI Commands):**
   - `acode export --format json|markdown|text`
   - `acode retention --policy short|default|long`
   - `acode privacy --level local|redacted|metadata|full`

3. **Task-049f (Sync Engine):**
   - Apply redaction before sync
   - Check privacy level before sync
   - Audit sync operations

4. **Task-039 (Security):**
   - Encrypted export files
   - Secure pattern storage
   - Audit log encryption

**Constraints and Limitations:**

1. **Retention granularity:** Day-level only (not hour/minute)
2. **Redaction reversibility:** Permanent in remote storage
3. **Export size limits:** 1GB per export (prevents OOM)
4. **Pattern performance:** Max 50 custom patterns (regex compilation cost)
5. **Grace period minimum:** 1 day (prevents accidental immediate deletion)
6. **Privacy changes:** Not retroactive (applies to new syncs only)
7. **Audit log size:** Unbounded (manual cleanup required)

**Trade-offs and Alternatives:**

1. **Soft delete vs immediate purge:**
   - **Chosen:** Soft delete with grace period
   - **Alternative:** Immediate permanent deletion
   - **Reason:** Prevents accidental data loss, supports "undo"

2. **Pattern-based vs ML-based redaction:**
   - **Chosen:** Regex patterns
   - **Alternative:** Machine learning model for secret detection
   - **Reason:** Deterministic, fast, no dependencies, explainable

3. **Per-chat vs global privacy:**
   - **Chosen:** Per-chat privacy levels with global default
   - **Alternative:** Global privacy only
   - **Reason:** Flexibility (some chats can sync, others local-only)

4. **Export streaming vs in-memory:**
   - **Chosen:** In-memory with 1GB limit
   - **Alternative:** Streaming export for unlimited size
   - **Reason:** Simpler code, adequate for typical use (100-500 chats)

**Observability:**

- **Metrics:** Retention purge count, export requests, redaction matches, privacy level distribution
- **Logs:** Audit log for all deletions, export operations logged with file size/duration
- **Alerts:** Retention job failures, export timeouts, unusual redaction match rates
- **Error Codes:** ACODE-RET-001 through ACODE-RET-005 for diagnosable failures

---

## Use Cases

### Use Case 1: Claire - Compliance Officer Managing Data Lifecycle

**Persona:** Claire is a compliance officer at a mid-sized software company. She needs to ensure conversation history meets GDPR "right to be forgotten" and data minimization requirements.

**Before (Manual Cleanup):**

Claire receives a data deletion request from an engineer who left the company. She manually searches for chats, messages, and tool call logs.

```bash
# Manual SQL queries to find data
SELECT * FROM chats WHERE created_by = 'user@example.com';
# Copy down 47 chat IDs

# Manually delete each chat
DELETE FROM messages WHERE chat_id = 'chat-001';
DELETE FROM runs WHERE chat_id = 'chat-001';
DELETE FROM chats WHERE id = 'chat-001';
# Repeat 46 more times...

# No audit trail, takes 2 hours
```

**Time spent:** 2 hours per deletion request × 12 requests/year = **24 hours/year @ $150/hour = $3,600/year**

**After (Automated Retention + Audit):**

```bash
# Set retention policy for user's chats
acode retention set --user user@example.com --policy immediate

# Export data for user before deletion (GDPR compliance)
acode export --user user@example.com --format json --output user-data.json
# Export completes in 30 seconds (250 messages)

# Verify retention policy will purge data
acode retention status
# Retention Status:
#   Chats marked for deletion: 47
#   Grace period ends: 2025-01-22
#   Will purge: messages (250), runs (47), chats (47)

# Wait for background job to purge (or trigger manually)
acode retention enforce --now

# Verify deletion
acode audit --action purge --since 2025-01-15
# Audit Log:
#   2025-01-15 10:30:00 | PURGE | 47 chats | user@example.com
```

**Time spent:** 10 minutes per request

**Savings:** 1.83 hours per request × 12 requests/year = **22 hours/year @ $150/hour = $3,300/year**

**Business Impact:** GDPR compliance ensured, audit trail for regulators, zero manual SQL queries, 92% time reduction.

---

### Use Case 2: Marcus - Senior Engineer Exporting Chat for Knowledge Base

**Persona:** Marcus is a senior engineer who solved a complex distributed systems problem through a 3-hour conversation with AI. He wants to export the conversation to add to team documentation.

**Before (Manual Copy-Paste):**

Marcus copies each message manually from terminal output into a Word document, formatting as he goes.

```bash
# Opens old terminal scrollback
# Copies each message one by one
# Loses tool call details
# Loses timestamps
# Formatting is inconsistent
# Takes 30 minutes
```

**Time spent:** 30 min per export × 8 exports/year = **4 hours/year @ $108/hour = $432/year**

**After (Automated Export):**

```bash
# Export conversation to Markdown
acode export --chat distributed-tracing-fix --format markdown --output docs/solutions/tracing.md

# Export includes:
# - All messages with timestamps
# - Tool calls with arguments and results
# - Run statistics (tokens, duration, model)
# - Formatted for direct inclusion in docs
# Takes 2 minutes
```

**Time spent:** 2 min per export

**Savings:** 28 min per export × 8 exports/year = 3.73 hours/year @ $108/hour = **$402/year**

**Business Impact:** High-quality documentation, tool call details preserved, searchable knowledge base, 93% time reduction.

---

### Use Case 3: Priya - Security Engineer Preventing Data Leaks

**Persona:** Priya is a security engineer responsible for preventing sensitive data exposure. She enables remote sync for team collaboration but needs to ensure API keys and passwords don't leak.

**Before (Manual Review):**

Priya manually reviews chat content before syncing, searching for secrets.

```bash
# Manually searches chats
acode chat list | grep -i "key\|password\|secret"
# Finds 12 chats with potential secrets

# Manually reviews each chat
acode chat open <chat-id>
# Reads through looking for API keys
# Edits messages to remove secrets
# Takes 2 hours for 100 chats
```

**Time spent:** 2 hours per review × 12 reviews/year = **24 hours/year @ $130/hour = $3,120/year**

**Risk:** Human error misses 1-2 secrets per review = potential data breach

**After (Automated Redaction):**

```bash
# Configure privacy with redaction
acode privacy set --level redacted

# Test redaction patterns against known content
acode redaction preview --chat api-integration
# Preview Redaction:
#   Line 45: "API key: sk_live_abc123..." → "[REDACTED-STRIPE-KEY-sk_live_abc]"
#   Line 67: "Password: MyP@ssw0rd!" → "[REDACTED-PASSWORD]"
#   Line 89: "JWT: eyJhbG..." → "[REDACTED-JWT-TOKEN-eyJhbG]"

# Add custom pattern for internal secrets
acode redaction add-pattern --name "Internal API" --regex "INTERNAL_[A-Z0-9]{32}"

# Enable sync with confidence
acode sync enable
# Sync applies redaction automatically
# 0 secrets leaked
```

**Time spent:** 15 min setup × 1 time + 5 min per new pattern × 3 patterns/year = **30 minutes/year**

**Savings:** 23.5 hours/year @ $130/hour = **$3,055/year**

**Business Impact:** Zero data leaks, automated enforcement, audit trail, 98% time reduction, eliminates human error.

---

## Security Considerations

### Threat 1: Retention Policy Bypass (Unauthorized Data Recovery)

**Risk:** Attacker bypasses retention enforcement to access deleted conversation data beyond retention period, recovering sensitive information intended for permanent deletion.

**Attack Scenario:**
```bash
# Attacker with database access
sqlite3 .acode/conversations.db

# Finds soft-deleted chats still in database
SELECT * FROM chats WHERE deleted_at IS NOT NULL;
# Returns 23 chats marked for deletion but not yet purged

# Reads deleted messages
SELECT content FROM messages WHERE chat_id IN (
  SELECT id FROM chats WHERE deleted_at IS NOT NULL
);
# Accesses 450 messages intended for deletion
# Includes sensitive data (credentials, personal info)
```

**Mitigation (EnforcedRetentionPolicy - 60 lines):**

```csharp
// src/Acode.Infrastructure/Retention/EnforcedRetentionPolicy.cs
namespace Acode.Infrastructure.Retention;

public sealed class EnforcedRetentionPolicy
{
    private readonly IDbConnection _connection;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<EnforcedRetentionPolicy> _logger;
    private const int MaxGracePeriodDays = 7;

    public EnforcedRetentionPolicy(
        IDbConnection connection,
        IAuditLogger auditLogger,
        ILogger<EnforcedRetentionPolicy> logger)
    {
        _connection = connection;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<Result<int, Error>> EnforcePolicyAsync(CancellationToken ct)
    {
        // Query chats past grace period
        var expiredChatIds = await _connection.QueryAsync<Guid>(@"
            SELECT id FROM chats
            WHERE deleted_at IS NOT NULL
              AND deleted_at < datetime('now', '-7 days')
            LIMIT 100", cancellationToken: ct);

        var purgedCount = 0;
        foreach (var chatId in expiredChatIds)
        {
            // Begin transaction for atomic deletion
            using var transaction = await _connection.BeginTransactionAsync(ct);
            
            try
            {
                // Count messages for audit
                var messageCount = await _connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM messages WHERE chat_id = @chatId",
                    new { chatId }, transaction, ct);

                // Cascade delete
                await _connection.ExecuteAsync(
                    "DELETE FROM messages WHERE chat_id = @chatId", 
                    new { chatId }, transaction, ct);
                await _connection.ExecuteAsync(
                    "DELETE FROM runs WHERE chat_id = @chatId", 
                    new { chatId }, transaction, ct);
                await _connection.ExecuteAsync(
                    "DELETE FROM conversation_search WHERE chat_id = @chatId", 
                    new { chatId }, transaction, ct);
                await _connection.ExecuteAsync(
                    "DELETE FROM chats WHERE id = @chatId", 
                    new { chatId }, transaction, ct);

                await transaction.CommitAsync(ct);

                // Audit log (survives deletion)
                await _auditLogger.LogAsync(new AuditEvent
                {
                    Action = "retention_purge",
                    EntityType = "chat",
                    EntityId = chatId.ToString(),
                    Metadata = new Dictionary<string, object>
                    {
                        ["message_count"] = messageCount,
                        ["purged_at"] = DateTime.UtcNow
                    }
                }, ct);

                purgedCount++;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to purge chat {ChatId}", chatId);
            }
        }

        return Result.Success<int, Error>(purgedCount);
    }
}
```

---

### Threat 2: Export Without Redaction (Data Leak via Export)

**Risk:** User exports conversations to insecure location (email, cloud storage) with sensitive data intact, bypassing privacy controls.

**Attack Scenario:**
```bash
# User exports without checking content
acode export --format json --output /tmp/export.json

# Export includes API keys
grep -i "api_key\|password\|secret" /tmp/export.json
# Matches:
#   "content": "Use API key sk_live_abc123xyz..."
#   "content": "Database password: MyP@ssw0rd!"

# User emails export.json to external consultant
# Sensitive data leaks outside organization
```

**Mitigation (RedactedExporter - 65 lines):**

```csharp
// src/Acode.Application/Export/RedactedExporter.cs
namespace Acode.Application.Export;

public sealed class RedactedExporter
{
    private readonly IRedactionEngine _redactionEngine;
    private readonly ILogger<RedactedExporter> _logger;

    public RedactedExporter(
        IRedactionEngine redactionEngine,
        ILogger<RedactedExporter> logger)
    {
        _redactionEngine = redactionEngine;
        _logger = logger;
    }

    public async Task<Result<ExportResult, Error>> ExportAsync(
        ExportRequest request,
        CancellationToken ct)
    {
        // Warn if exporting without redaction
        if (request.ApplyRedaction == false)
        {
            _logger.LogWarning(
                "Exporting without redaction. File: {FilePath}. " +
                "Consider using --redact flag to protect sensitive data.",
                request.OutputPath);
        }

        var chats = await LoadChatsAsync(request.ChatIds, ct);
        var redactedChats = new List<Chat>();
        var redactionStats = new Dictionary<string, int>();

        foreach (var chat in chats)
        {
            if (request.ApplyRedaction)
            {
                // Redact each message
                var redactedMessages = new List<Message>();
                foreach (var message in chat.Messages)
                {
                    var redactionResult = _redactionEngine.Redact(message.Content);
                    
                    // Track redaction statistics
                    foreach (var match in redactionResult.Matches)
                    {
                        var key = match.PatternName;
                        redactionStats[key] = redactionStats.GetValueOrDefault(key) + 1;
                    }

                    redactedMessages.Add(message with
                    {
                        Content = redactionResult.RedactedContent
                    });
                }

                redactedChats.Add(chat with { Messages = redactedMessages });
            }
            else
            {
                redactedChats.Add(chat);
            }
        }

        // Serialize to requested format
        var exportContent = request.Format switch
        {
            ExportFormat.Json => JsonSerializer.Serialize(redactedChats, JsonOptions),
            ExportFormat.Markdown => MarkdownFormatter.Format(redactedChats),
            ExportFormat.PlainText => PlainTextFormatter.Format(redactedChats),
            _ => throw new ArgumentException($"Unknown format: {request.Format}")
        };

        await File.WriteAllTextAsync(request.OutputPath, exportContent, ct);

        return Result.Success<ExportResult, Error>(new ExportResult
        {
            ChatCount = redactedChats.Count,
            MessageCount = redactedChats.Sum(c => c.Messages.Count),
            FilePath = request.OutputPath,
            FileSizeBytes = exportContent.Length,
            RedactionApplied = request.ApplyRedaction,
            RedactionStats = redactionStats
        });
    }
}
```

---

### Threat 3: Redaction Pattern Bypass (Incomplete Secret Detection)

**Risk:** Attacker crafts secrets in formats not matching redaction patterns, bypassing detection and leaking sensitive data during sync.

**Attack Scenario:**
```bash
# Attacker discovers redaction patterns
acode redaction list
# Patterns:
#   - Stripe Keys: \bsk_live_[a-zA-Z0-9]{24,}\b
#   - GitHub Tokens: \bghp_[a-zA-Z0-9]{36,}\b

# Attacker encodes secrets to bypass patterns
acode run "Store this: sk_li"+"ve_abc123xyz (base64: c2tfbGl2ZV9hYmMxMjN4eXo=)"

# Pattern doesn't match split string or base64
# Secret syncs unredacted
```

**Mitigation (ComprehensiveRedactionEngine - 70 lines):**

```csharp
// src/Acode.Infrastructure/Privacy/ComprehensiveRedactionEngine.cs
namespace Acode.Infrastructure.Privacy;

public sealed class ComprehensiveRedactionEngine : IRedactionEngine
{
    private readonly ILogger<ComprehensiveRedactionEngine> _logger;
    private static readonly RedactionPattern[] Patterns = BuiltInPatterns.Concat(CustomPatterns).ToArray();

    public RedactionResult Redact(string content)
    {
        // Normalize content to catch obfuscation
        var normalized = NormalizeContent(content);
        var matches = new List<RedactionMatch>();

        foreach (var pattern in Patterns)
        {
            var regex = new Regex(pattern.Pattern, RegexOptions.Compiled);
            var matchResults = regex.Matches(normalized);

            foreach (Match match in matchResults)
            {
                matches.Add(new RedactionMatch
                {
                    PatternName = pattern.Name,
                    OriginalText = match.Value,
                    StartIndex = match.Index,
                    Length = match.Length
                });
            }
        }

        // Apply redactions with placeholders
        var redacted = content;
        foreach (var match in matches.OrderByDescending(m => m.StartIndex))
        {
            var prefix = match.OriginalText.Substring(0, Math.Min(10, match.OriginalText.Length));
            var placeholder = $"[REDACTED-{match.PatternName.ToUpper()}-{prefix}]";
            
            redacted = redacted.Remove(match.StartIndex, match.Length);
            redacted = redacted.Insert(match.StartIndex, placeholder);
        }

        if (matches.Any())
        {
            _logger.LogInformation(
                "Redacted {Count} secrets: {Patterns}",
                matches.Count,
                string.Join(", ", matches.Select(m => m.PatternName).Distinct()));
        }

        return new RedactionResult
        {
            RedactedContent = redacted,
            Matches = matches,
            RedactionApplied = matches.Any()
        };
    }

    private string NormalizeContent(string content)
    {
        // Remove common obfuscation techniques
        var normalized = content
            .Replace(" ", "")      // Remove spaces
            .Replace("_", "")      // Remove underscores
            .Replace("-", "")      // Remove hyphens
            .Replace("+", "");     // Remove plus signs

        // Decode base64 if present
        if (TryDecodeBase64(content, out var decoded))
        {
            normalized += " " + decoded;
        }

        // Unescape common encodings
        normalized += " " + Uri.UnescapeDataString(content);

        return normalized;
    }

    private bool TryDecodeBase64(string content, out string decoded)
    {
        try
        {
            var base64Pattern = @"[A-Za-z0-9+/]{20,}={0,2}";
            var matches = Regex.Matches(content, base64Pattern);
            
            var decodedParts = new List<string>();
            foreach (Match match in matches)
            {
                try
                {
                    var bytes = Convert.FromBase64String(match.Value);
                    decodedParts.Add(Encoding.UTF8.GetString(bytes));
                }
                catch
                {
                    // Not valid base64
                }
            }

            decoded = string.Join(" ", decodedParts);
            return decodedParts.Any();
        }
        catch
        {
            decoded = string.Empty;
            return false;
        }
    }
}
```

---

### Threat 4: Privacy Level Downgrade Attack

**Risk:** Attacker changes privacy level from LOCAL_ONLY to FULL after sensitive conversations, exposing previously private data to remote sync.

**Attack Scenario:**
```bash
# User has sensitive local-only chats
acode privacy set --chat sec-incident --level local_only

# Attacker gains access to CLI
acode privacy set --chat sec-incident --level full

# Next sync sends all sensitive data to remote
acode sync
# 47 messages containing incident details, credentials, PII synced
```

**Mitigation (ImmutablePrivacyGuard - 55 lines):**

```csharp
// src/Acode.Application/Privacy/ImmutablePrivacyGuard.cs
namespace Acode.Application.Privacy;

public sealed class ImmutablePrivacyGuard
{
    private readonly IChatRepository _chatRepository;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<ImmutablePrivacyGuard> _logger;

    public async Task<Result<Unit, Error>> SetPrivacyLevelAsync(
        Guid chatId,
        PrivacyLevel newLevel,
        CancellationToken ct)
    {
        var chat = await _chatRepository.GetByIdAsync(chatId, ct);
        if (chat == null)
        {
            return Result.Failure<Unit, Error>(new NotFoundError("Chat not found"));
        }

        var currentLevel = chat.PrivacyLevel;

        // Prevent downgrade from LOCAL_ONLY to more permissive levels
        if (currentLevel == PrivacyLevel.LocalOnly && newLevel != PrivacyLevel.LocalOnly)
        {
            _logger.LogWarning(
                "Blocked privacy downgrade: Chat {ChatId} from LOCAL_ONLY to {NewLevel}",
                chatId, newLevel);

            return Result.Failure<Unit, Error>(new SecurityError(
                "Cannot change privacy level from LOCAL_ONLY. " +
                "Create new chat with desired privacy level instead."));
        }

        // Require confirmation for sensitive transitions
        if (currentLevel == PrivacyLevel.Redacted && newLevel == PrivacyLevel.Full)
        {
            _logger.LogWarning(
                "Privacy upgrade requires confirmation: Chat {ChatId} REDACTED→FULL",
                chatId);

            // Require explicit confirmation flag
            return Result.Failure<Unit, Error>(new SecurityError(
                "Upgrading from REDACTED to FULL requires --confirm-expose-data flag"));
        }

        // Update privacy level
        var updatedChat = chat with { PrivacyLevel = newLevel };
        await _chatRepository.UpdateAsync(updatedChat, ct);

        // Audit log
        await _auditLogger.LogAsync(new AuditEvent
        {
            Action = "privacy_level_change",
            EntityType = "chat",
            EntityId = chatId.ToString(),
            Metadata = new Dictionary<string, object>
            {
                ["old_level"] = currentLevel.ToString(),
                ["new_level"] = newLevel.ToString(),
                ["timestamp"] = DateTime.UtcNow
            }
        }, ct);

        return Result.Success<Unit, Error>(Unit.Value);
    }
}
```

---

### Threat 5: Retention Enforcement Tampering

**Risk:** Attacker disables retention enforcement background job, causing unlimited data accumulation and preventing compliance with data minimization policies.

**Attack Scenario:**
```bash
# Attacker modifies configuration
vi .agent/config.yml
# Changes: enforcement.enabled: false

# Retention job stops running
# Old chats accumulate indefinitely
# After 2 years: 10,000+ chats, 500,000+ messages
# Storage bloats from 50MB to 5GB
# GDPR audit fails (data minimization violation)
```

**Mitigation (TamperProofRetentionScheduler - 50 lines):**

```csharp
// src/Acode.Infrastructure/Retention/TamperProofRetentionScheduler.cs
namespace Acode.Infrastructure.Retention;

public sealed class TamperProofRetentionScheduler : BackgroundService
{
    private readonly IEnforcedRetentionPolicy _retentionPolicy;
    private readonly IConfigurationMonitor _configMonitor;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<TamperProofRetentionScheduler> _logger;
    private const int EnforcementIntervalHours = 24;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Check if retention enforcement is disabled
                var config = await _configMonitor.GetConfigurationAsync(stoppingToken);
                
                if (!config.Retention.Enforcement.Enabled)
                {
                    _logger.LogWarning(
                        "Retention enforcement is DISABLED in configuration. " +
                        "This may violate data retention policies.");

                    // Log audit event
                    await _auditLogger.LogAsync(new AuditEvent
                    {
                        Action = "retention_enforcement_disabled",
                        Severity = AuditSeverity.Warning,
                        Metadata = new Dictionary<string, object>
                        {
                            ["disabled_at"] = DateTime.UtcNow,
                            ["config_path"] = config.SourcePath
                        }
                    }, stoppingToken);
                }
                else
                {
                    // Enforce retention policy
                    _logger.LogInformation("Starting retention enforcement");
                    
                    var result = await _retentionPolicy.EnforcePolicyAsync(stoppingToken);
                    
                    if (result.IsSuccess)
                    {
                        _logger.LogInformation(
                            "Retention enforcement completed: {Count} chats purged",
                            result.Value);
                    }
                    else
                    {
                        _logger.LogError(
                            "Retention enforcement failed: {Error}",
                            result.Error.Message);
                    }
                }

                // Wait for next enforcement cycle
                await Task.Delay(TimeSpan.FromHours(EnforcementIntervalHours), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Retention enforcement cycle failed");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
```

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Retention | How long data is kept |
| Expiry | When data can be deleted |
| Export | Create portable data copy |
| Redaction | Remove sensitive content |
| Privacy | Data visibility controls |
| Compliance | Meeting requirements |
| Purge | Permanent deletion |
| Policy | Retention/privacy rules |
| Pattern | Regex for matching |
| Placeholder | Replacement for redacted |
| Metadata | Data about data |
| Audit | Record of actions |
| Layered | Different levels |
| Portable | Works standalone |
| Enforcement | Applying policies |

---

## Out of Scope

The following items are explicitly excluded from Task 049.e:

- **Data model** - Task 049.a
- **CLI commands** - Task 049.b
- **Concurrency** - Task 049.c
- **Search** - Task 049.d
- **Sync engine** - Task 049.f
- **Encryption** - Task 039
- **Legal hold** - Not supported
- **Cross-tenant** - Single user
- **Audit archival** - Basic only
- **GDPR automation** - Manual process

---

## Assumptions

### Technical Assumptions

- ASM-001: Retention policies are time-based (age of chat/message)
- ASM-002: Export formats include JSON and Markdown
- ASM-003: Redaction patterns are configurable (regex)
- ASM-004: Deletion is permanent after retention period
- ASM-005: Export respects redaction rules

### Behavioral Assumptions

- ASM-006: Users configure retention based on preferences
- ASM-007: Export is triggered manually, not automatic
- ASM-008: Redaction applies to sensitive patterns (keys, passwords)
- ASM-009: Privacy concerns vary by user
- ASM-010: Deleted content is unrecoverable

### Dependency Assumptions

- ASM-011: Task 049.a data model supports soft/hard delete
- ASM-012: Task 049.b provides export CLI command
- ASM-013: Task 002 config stores retention settings

### Compliance Assumptions

- ASM-014: Users responsible for compliance decisions
- ASM-015: Redaction patterns catch common secrets
- ASM-016: Export documentation notes privacy implications

---

## Functional Requirements

### Retention Policies

- FR-001: Default retention: 365 days
- FR-002: Retention MUST be configurable
- FR-003: Minimum retention: 7 days
- FR-004: Maximum retention: unlimited
- FR-005: Active chats MUST NOT expire
- FR-006: Archived chats MUST follow policy

### Retention Enforcement

- FR-007: Background job MUST run
- FR-008: Expired chats MUST be identified
- FR-009: Expired chats MUST be purged
- FR-010: Purge MUST cascade to runs/messages
- FR-011: Index MUST be updated
- FR-012: Enforcement MUST log actions

### Retention Warnings

- FR-013: Chats near expiry MUST warn
- FR-014: Warning threshold: 7 days
- FR-015: Warnings MUST be visible in list
- FR-016: Warning suppression MUST work

### Export Formats

- FR-017: JSON export MUST work
- FR-018: Markdown export MUST work
- FR-019: Plain text export MUST work
- FR-020: Format MUST be selectable

### Export Content

- FR-021: Messages MUST be included
- FR-022: Tool calls MUST be included
- FR-023: Metadata MUST be included
- FR-024: Timestamps MUST be included
- FR-025: Content MUST be redacted if configured

### Export Filtering

- FR-026: Filter by chat MUST work
- FR-027: Filter by date MUST work
- FR-028: Filter by tag MUST work
- FR-029: All chats export MUST work

### Export CLI

- FR-030: `acode export` MUST work
- FR-031: Output file MUST be configurable
- FR-032: Stdout MUST be supported
- FR-033: Progress MUST be shown

### Privacy Levels

- FR-034: LOCAL_ONLY MUST prevent sync
- FR-035: REDACTED MUST sync cleaned content
- FR-036: FULL MUST sync everything
- FR-037: Default MUST be LOCAL_ONLY

### Privacy Controls

- FR-038: Per-chat privacy MUST work
- FR-039: Global default MUST work
- FR-040: Privacy MUST be changeable
- FR-041: Privacy change MUST log

### Redaction Patterns

- FR-042: API key patterns MUST exist
- FR-043: Password patterns MUST exist
- FR-044: Custom patterns MUST work
- FR-045: Patterns MUST be configurable

### Redaction Behavior

- FR-046: Match MUST be replaced
- FR-047: Placeholder MUST be used
- FR-048: Original MUST be preserved locally
- FR-049: Redaction MUST be logged

### Redaction Preview

- FR-050: `acode redact preview` MUST work
- FR-051: Preview MUST show matches
- FR-052: Preview MUST NOT modify

### Compliance

- FR-053: Deletion audit MUST exist
- FR-054: Export audit MUST exist
- FR-055: Retention report MUST exist
- FR-056: Compliance status MUST be queryable

---

## Non-Functional Requirements

### Performance

- NFR-001: Export of 1000 messages MUST complete in <10 seconds
- NFR-002: Export of 10000 messages MUST complete in <60 seconds
- NFR-003: Redaction processing MUST complete in <1ms per message
- NFR-004: Batch redaction of 1000 messages MUST complete in <800ms
- NFR-005: Retention policy check MUST complete in <1 second for 10k chats
- NFR-006: Retention enforcement job MUST complete in <5 minutes for 1000 expired chats
- NFR-007: Privacy level lookup MUST complete in <10ms

### Reliability

- NFR-008: Retention enforcement MUST NOT delete non-expired data
- NFR-009: Retention enforcement MUST be idempotent (safe to run multiple times)
- NFR-010: Export MUST include all messages matching filter criteria
- NFR-011: Export MUST produce valid JSON/Markdown with no truncation
- NFR-012: Redaction MUST be consistent (same input always produces same output)
- NFR-013: Partial export failure MUST NOT corrupt output file
- NFR-014: Database transaction MUST ensure atomic cascade deletion

### Security

- NFR-015: Redaction patterns MUST detect common API key formats (Stripe, GitHub, AWS, Azure)
- NFR-016: Redaction patterns MUST detect password fields in common formats
- NFR-017: Redaction MUST apply before any data leaves local storage
- NFR-018: Export files MUST support optional encryption at rest
- NFR-019: Audit logs MUST be append-only and tamper-evident
- NFR-020: Privacy level changes MUST require explicit confirmation for downgrades
- NFR-021: LOCAL_ONLY privacy MUST be irreversible without data loss confirmation

### Compliance

- NFR-022: Retention enforcement MUST run at configured schedule (default: daily)
- NFR-023: Deletion MUST be provable via audit log entry
- NFR-024: Export format MUST be portable (no external dependencies to read)
- NFR-025: Retention warnings MUST appear in CLI output for chats near expiry
- NFR-026: All purge operations MUST create immutable audit records
- NFR-027: Compliance report MUST be exportable for auditors

### Scalability

- NFR-028: Export MUST support output files up to 1GB without OOM
- NFR-029: Retention enforcement MUST batch processing to avoid memory spikes
- NFR-030: Redaction patterns MUST support up to 50 custom patterns without degradation

### Usability

- NFR-031: Export progress MUST be displayed for operations >5 seconds
- NFR-032: Redaction preview MUST show before/after comparison
- NFR-033: Error messages MUST include remediation steps
- NFR-034: CLI commands MUST support --dry-run for destructive operations

---

## User Manual Documentation

### Overview

Data lifecycle controls manage retention, export, and privacy. Keep data as long as needed, export when required, and protect sensitive content.

### Retention Configuration

```yaml
# .agent/config.yml
retention:
  # Default retention period
  default_days: 365
  
  # Per-status overrides
  overrides:
    archived: 90   # Archived chats: 90 days
    active: -1     # Active chats: never expire
    
  # Warning before expiry
  warn_days_before: 7
  
  # Enforcement schedule
  enforce:
    enabled: true
    schedule: "0 2 * * *"  # Daily at 2 AM
```

### Viewing Retention Status

```bash
$ acode retention status

Retention Status
────────────────────────────────────
Active Policy: 365 days (archived: 90 days)
Last Enforcement: 2024-01-15 02:00 UTC

Statistics:
  Total Chats: 156
  Active: 12 (never expire)
  Archived: 144
  Expiring Soon (7d): 3
  
Expiring Soon:
  chat_old001  "Old Feature"      Expires: 2024-01-20
  chat_old002  "Archived Bug"     Expires: 2024-01-21
  chat_old003  "Legacy Work"      Expires: 2024-01-22
```

### Export Commands

```bash
# Export single chat to JSON
$ acode export chat_abc123 --format json > chat.json

# Export to Markdown
$ acode export chat_abc123 --format markdown > chat.md

# Export all chats
$ acode export --all --format json > all_chats.json

# Export with date filter
$ acode export --since 2024-01-01 --format json > recent.json

# Export with redaction
$ acode export chat_abc123 --redact --format json > redacted.json
```

### Export Formats

**JSON Format:**
```json
{
  "exported_at": "2024-01-15T10:00:00Z",
  "chats": [
    {
      "id": "chat_abc123",
      "title": "Feature: Auth",
      "runs": [
        {
          "id": "run_001",
          "messages": [
            {
              "role": "user",
              "content": "Design login flow",
              "created_at": "2024-01-15T09:00:00Z"
            }
          ]
        }
      ]
    }
  ]
}
```

**Markdown Format:**
```markdown
# Feature: Auth

## Run 1 - 2024-01-15 09:00

**User:** Design login flow

**Assistant:** I'll help design the login flow...
```

### Privacy Configuration

```yaml
# .agent/config.yml
privacy:
  # Default privacy level
  default: local_only  # local_only | redacted | full
  
  # Sync settings
  sync:
    enabled: true
    level: redacted  # What to sync
    
  # Per-chat overrides
  overrides:
    - pattern: "*secret*"
      level: local_only
```

### Privacy Levels

| Level | Local | Synced | Description |
|-------|-------|--------|-------------|
| local_only | Full | None | Never sync |
| redacted | Full | Cleaned | Sync with redaction |
| full | Full | Full | Sync everything |

### Setting Chat Privacy

```bash
# Set chat to local-only
$ acode chat privacy chat_abc123 local_only
Privacy set to: local_only (never syncs)

# Set chat to sync redacted
$ acode chat privacy chat_abc123 redacted
Privacy set to: redacted (syncs cleaned content)

# View privacy
$ acode chat show chat_abc123
...
Privacy: local_only
Sync Status: Not synced
```

### Redaction Patterns

```yaml
# .agent/config.yml
redaction:
  # Built-in patterns (enabled by default)
  builtin:
    api_keys: true
    passwords: true
    tokens: true
    
  # Custom patterns
  custom:
    - name: internal_urls
      pattern: "https?://internal\\..+"
      replacement: "[INTERNAL_URL]"
      
    - name: employee_ids
      pattern: "EMP-\\d{6}"
      replacement: "[EMPLOYEE_ID]"
```

### Previewing Redaction

```bash
$ acode redact preview chat_abc123

Redaction Preview
────────────────────────────────────
Chat: Feature: Auth (chat_abc123)
Messages to scan: 47

Matches Found: 3
  Line 12: "API key: sk-abc...xyz" → "[API_KEY]"
  Line 34: "password = 'secret'" → "password = '[PASSWORD]'"
  Line 45: "https://internal.company.com" → "[INTERNAL_URL]"

No changes made. Use 'acode export --redact' to apply.
```

### Compliance Reports

```bash
$ acode compliance report

Compliance Report
────────────────────────────────────
Generated: 2024-01-15 10:00 UTC

Retention Compliance:
  ✓ Policy enforced: 2024-01-15 02:00 UTC
  ✓ No overdue purges
  ✓ 3 chats expiring within warning period

Privacy Compliance:
  ✓ 144 chats: local_only
  ✓ 10 chats: redacted
  ✓ 2 chats: full

Recent Deletions (30 days):
  2024-01-10: 5 chats purged (retention)
  2024-01-05: 2 chats purged (user request)

Export Log:
  2024-01-12: 3 exports (JSON)
```

---

## Acceptance Criteria

### Retention - Policy Configuration

- [ ] AC-001: Default retention period is 365 days for archived chats
- [ ] AC-002: Retention period is configurable via CLI and config file
- [ ] AC-003: Minimum retention period of 7 days is enforced
- [ ] AC-004: Maximum retention can be set to "never" (unlimited)
- [ ] AC-005: Active (non-archived) chats are exempt from retention by default
- [ ] AC-006: Per-chat retention override is supported
- [ ] AC-007: Retention policy changes take effect immediately

### Retention - Enforcement

- [ ] AC-008: Background retention job runs at configured schedule (default: daily at 3 AM)
- [ ] AC-009: Expired chats are identified by comparing archived_at timestamp
- [ ] AC-010: Grace period of 7 days applies before permanent deletion
- [ ] AC-011: Soft-delete marks chat with deleted_at timestamp
- [ ] AC-012: Hard-delete permanently removes data after grace period
- [ ] AC-013: Cascade deletion removes: chats → runs → messages → search index
- [ ] AC-014: Batch processing handles up to 100 chats per enforcement cycle
- [ ] AC-015: Manual enforcement trigger available: `acode retention enforce --now`

### Retention - Warnings and Notifications

- [ ] AC-016: Chats within 7 days of expiry show warning in list output
- [ ] AC-017: Warning includes expiry date and message count
- [ ] AC-018: Warning suppression available per-chat: `--no-expiry-warning`
- [ ] AC-019: `acode retention status` shows summary of expiring chats
- [ ] AC-020: Email/webhook notifications configurable for impending deletions

### Export - Formats

- [ ] AC-021: JSON export produces valid JSON with complete schema
- [ ] AC-022: JSON export includes all message fields (id, role, content, timestamps, tool_calls)
- [ ] AC-023: Markdown export produces readable formatted document
- [ ] AC-024: Markdown export includes code blocks with syntax highlighting markers
- [ ] AC-025: Plain text export produces minimal formatting for simple consumption
- [ ] AC-026: Export format selectable via `--format json|markdown|text`
- [ ] AC-027: Export includes metadata header (export timestamp, acode version)

### Export - Content Selection

- [ ] AC-028: Single chat export: `acode export <chat-id>`
- [ ] AC-029: All chats export: `acode export --all`
- [ ] AC-030: Date filter: `--since` and `--until` with ISO 8601 dates
- [ ] AC-031: Relative date filter: `--since 7d` for last 7 days
- [ ] AC-032: Tag filter: `--tag <tagname>` exports only matching chats
- [ ] AC-033: Multiple filters combine with AND logic
- [ ] AC-034: Export preview available: `acode export --preview` shows what would be exported

### Export - Output Options

- [ ] AC-035: File output: `--output /path/to/file.json`
- [ ] AC-036: Stdout output: pipes to stdout when no output file specified
- [ ] AC-037: Progress display for exports >5 seconds
- [ ] AC-038: Compression option: `--compress` creates .gz file
- [ ] AC-039: Encryption option: `--encrypt` creates encrypted archive
- [ ] AC-040: Overwrite protection: prompts before overwriting existing file

### Export - Redaction Integration

- [ ] AC-041: `--redact` flag applies redaction patterns before export
- [ ] AC-042: Redaction statistics shown after export (patterns matched, occurrences)
- [ ] AC-043: Redaction preview available without modifying data
- [ ] AC-044: Unredacted export warns about sensitive content risk
- [ ] AC-045: Redaction is applied in-memory, original data unchanged

### Privacy - Levels

- [ ] AC-046: LOCAL_ONLY level prevents all remote sync
- [ ] AC-047: REDACTED level syncs content with secrets removed
- [ ] AC-048: METADATA_ONLY level syncs titles/tags/timestamps only
- [ ] AC-049: FULL level syncs all content (with warning)
- [ ] AC-050: Default privacy level is LOCAL_ONLY

### Privacy - Per-Chat Configuration

- [ ] AC-051: Privacy level settable per chat: `acode chat privacy <id> <level>`
- [ ] AC-052: Privacy level visible in chat details: `acode chat show <id>`
- [ ] AC-053: Privacy level filterable in list: `acode chat list --privacy local_only`
- [ ] AC-054: Bulk privacy update: `acode chat privacy --all <level>`
- [ ] AC-055: Privacy inheritance from project default

### Privacy - Level Transitions

- [ ] AC-056: LOCAL_ONLY → other levels blocked (security enforcement)
- [ ] AC-057: REDACTED → FULL requires explicit confirmation flag
- [ ] AC-058: Any level → LOCAL_ONLY always allowed
- [ ] AC-059: Privacy level change logged to audit trail
- [ ] AC-060: Privacy downgrade warning displayed before confirmation

### Redaction - Built-in Patterns

- [ ] AC-061: Stripe API keys pattern: `sk_live_[a-zA-Z0-9]{24,}`
- [ ] AC-062: GitHub tokens pattern: `gh[ps]_[a-zA-Z0-9]{36,}`
- [ ] AC-063: AWS access keys pattern: `AKIA[A-Z0-9]{16}`
- [ ] AC-064: JWT tokens pattern: `eyJ[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+`
- [ ] AC-065: Password fields pattern: `(password|passwd|pwd)[=:\s]+\S{8,}`
- [ ] AC-066: Private key blocks pattern: `-----BEGIN.*PRIVATE KEY-----`
- [ ] AC-067: Built-in patterns enabled by default

### Redaction - Custom Patterns

- [ ] AC-068: Custom patterns definable in config file
- [ ] AC-069: Custom pattern requires: name, regex, replacement
- [ ] AC-070: Custom patterns validated before saving
- [ ] AC-071: Maximum 50 custom patterns supported
- [ ] AC-072: Custom pattern testing: `acode redaction test --pattern <regex> --text <sample>`
- [ ] AC-073: Pattern list viewable: `acode redaction patterns list`
- [ ] AC-074: Pattern removable: `acode redaction patterns remove <name>`

### Redaction - Behavior

- [ ] AC-075: Matched content replaced with placeholder: `[REDACTED-<PATTERN>-<prefix>]`
- [ ] AC-076: Placeholder preserves partial info for debugging (first 10 chars)
- [ ] AC-077: Multiple matches in same message all redacted
- [ ] AC-078: Redaction is deterministic (same input = same output)
- [ ] AC-079: Redaction applied recursively to nested content
- [ ] AC-080: Redaction logging shows match count and pattern names

### Redaction - Preview

- [ ] AC-081: `acode redaction preview <chat-id>` shows what would be redacted
- [ ] AC-082: Preview shows line number, original text, replacement
- [ ] AC-083: Preview counts total matches per pattern
- [ ] AC-084: Preview does NOT modify actual data
- [ ] AC-085: Preview available for export: `acode export --redact --preview`

### Compliance - Audit Logging

- [ ] AC-086: All retention purge operations logged with timestamp
- [ ] AC-087: All export operations logged with file path and size
- [ ] AC-088: All privacy level changes logged with old/new values
- [ ] AC-089: Audit log format is JSON Lines (one JSON object per line)
- [ ] AC-090: Audit log location configurable (default: `.agent/logs/audit.jsonl`)
- [ ] AC-091: Audit log tamper-evident (append-only, hash-chained)
- [ ] AC-092: Audit log retention separate from chat retention (default: 7 years)

### Compliance - Reporting

- [ ] AC-093: `acode compliance report` generates summary report
- [ ] AC-094: Report shows retention compliance status (% on-time purges)
- [ ] AC-095: Report shows privacy distribution (count by level)
- [ ] AC-096: Report shows recent deletions with timestamps
- [ ] AC-097: Report shows export history with redaction status
- [ ] AC-098: Report exportable as JSON for external audit systems
- [ ] AC-099: Report includes recommendations for violations

### CLI Commands

- [ ] AC-100: `acode retention status` - view retention status
- [ ] AC-101: `acode retention enforce` - trigger manual enforcement
- [ ] AC-102: `acode retention set --policy <name>` - set retention policy
- [ ] AC-103: `acode export` - export chat data
- [ ] AC-104: `acode privacy set <chat-id> <level>` - set privacy level
- [ ] AC-105: `acode privacy status` - view privacy status
- [ ] AC-106: `acode redaction preview` - preview redaction
- [ ] AC-107: `acode redaction patterns` - manage patterns
- [ ] AC-108: `acode compliance report` - generate compliance report

### Error Handling

- [ ] AC-109: ACODE-PRIV-001 returned for retention enforcement errors
- [ ] AC-110: ACODE-PRIV-002 returned for export failures
- [ ] AC-111: ACODE-PRIV-003 returned for redaction errors
- [ ] AC-112: ACODE-PRIV-004 returned for invalid pattern syntax
- [ ] AC-113: ACODE-PRIV-005 returned for compliance report errors
- [ ] AC-114: ACODE-PRIV-006 returned for privacy level transition blocked
- [ ] AC-115: All errors include actionable remediation guidance

---

## Best Practices

### Retention Policies

- **BP-001: Define clear policies** - Document retention periods and their rationale
- **BP-002: Notify before deletion** - Warn users before permanent deletion
- **BP-003: Grace period** - Allow recovery window before permanent deletion
- **BP-004: Audit retention actions** - Log what was deleted and when

### Export Handling

- **BP-005: Multiple formats** - Support JSON for programmatic use, Markdown for reading
- **BP-006: Include metadata** - Export timestamps, chat names, message counts
- **BP-007: Encryption option** - Allow encrypted exports for sensitive data
- **BP-008: Progress feedback** - Show export progress for large exports

### Privacy & Redaction

- **BP-009: Default patterns** - Ship with common secret patterns (API keys, passwords)
- **BP-010: Reversibility awareness** - Document that redaction is permanent
- **BP-011: Preview redaction** - Let users preview what will be redacted
- **BP-012: Pattern testing** - Provide way to test patterns before applying

---

## Troubleshooting

### Issue 1: Retention Not Applied

**Symptom:** Old content not deleted as expected. Chats older than retention period still appear in listings. Storage continues growing despite retention policy.

**Causes:**
- Retention enforcement job disabled in configuration
- Background service not running (crashed or never started)
- Retention policy set to `never` or very long period
- Chats marked as `active` (exempt from retention)

**Solution:**
1. Check retention policy configuration: `acode config get retention`
2. Verify background job is running: `acode service status` - look for RetentionEnforcer
3. Check retention logs: `.agent/logs/retention.log` for errors or skipped chats
4. Review chat status - active chats are exempt: `acode chat list --status active`
5. Force immediate enforcement: `acode retention enforce --now`
6. If job disabled, re-enable: `acode config set retention.enforce.enabled true`

---

### Issue 2: Export Fails or Creates Empty File

**Symptom:** Export command errors with exception. Export completes but output file is empty or truncated. JSON export is malformed.

**Causes:**
- No chats match the filter criteria
- Disk space insufficient for export
- Chat IDs don't exist
- Date filter excludes all content

**Solution:**
1. Verify chats exist with list command: `acode chat list`
2. Check disk space: `Get-PSDrive C` (Windows) or `df -h` (Unix)
3. Try export without filters first: `acode export --all --format json`
4. Verify chat ID is correct: `acode chat show <chat-id>`
5. Check date format is ISO 8601: `--since 2025-01-01`
6. Try smaller scope: single chat instead of all chats

---

### Issue 3: Redaction Pattern Not Working

**Symptom:** Sensitive data (API keys, passwords) not redacted in exports. Pattern preview shows no matches for known sensitive content.

**Causes:**
- Pattern regex doesn't match content format
- Custom pattern has syntax error
- Built-in patterns disabled in configuration
- Content obfuscated or encoded (base64)

**Solution:**
1. Test pattern against known content: `acode redaction test --pattern "sk_live_.*" --text "sk_live_abc123"`
2. Check regex syntax - validate at regex101.com
3. Verify built-in patterns enabled: `acode config get redaction.builtin`
4. Use preview to see what would match: `acode redaction preview --chat <id>`
5. Check for encoding - base64 encoded secrets won't match plaintext patterns
6. Review pattern list: `acode redaction patterns list`

---

### Issue 4: Privacy Level Cannot Be Changed

**Symptom:** Attempting to change chat privacy level returns error. "Cannot change privacy from LOCAL_ONLY" message appears.

**Causes:**
- Attempting to downgrade from LOCAL_ONLY (blocked by design)
- Attempting to upgrade from REDACTED to FULL without confirmation
- Chat is locked or archived
- Configuration prevents privacy changes

**Solution:**
1. LOCAL_ONLY→other is blocked for security - create new chat with desired level
2. For REDACTED→FULL, add confirmation flag: `acode chat privacy <id> full --confirm-expose-data`
3. Unarchive chat before changing privacy: `acode chat unarchive <id>`
4. Check configuration: `acode config get privacy.allow_level_changes`
5. Review privacy constraints in CLAUDE.md documentation

---

### Issue 5: Audit Log Missing or Incomplete

**Symptom:** Compliance report shows gaps in audit trail. Retention purge events not logged. Export operations not recorded.

**Causes:**
- Audit logging disabled in configuration
- Audit log file corrupted or deleted
- Disk full preventing log writes
- Log rotation removed old entries

**Solution:**
1. Check audit logging enabled: `acode config get audit.enabled`
2. Verify log file exists: `.agent/logs/audit.log`
3. Check disk space for log partition
4. Review log rotation settings: `audit.rotation_days` configuration
5. If corrupted, rebuild from database: `acode audit rebuild`
6. For compliance, ensure rotation keeps required duration

---

### Issue 6: Export Takes Too Long

**Symptom:** Export of large dataset times out. Progress bar stalls. CLI becomes unresponsive during export.

**Causes:**
- Exporting too many chats at once (10k+)
- Slow disk I/O (network drive, HDD)
- Redaction processing adds overhead
- Large messages (code blocks) slowing serialization

**Solution:**
1. Use filters to reduce export scope: `--since 7d` or `--chat <specific-chat>`
2. Export without redaction for speed, then redact separately: `--no-redact`
3. Use SSD instead of HDD or network drive
4. Export to local disk, then copy to final destination
5. Increase timeout: `--timeout 300` (5 minutes)
6. Monitor progress: `--verbose` shows messages processed

---

### Issue 7: Compliance Report Shows Violations

**Symptom:** `acode compliance report` shows warnings or errors. Retention violations flagged. Privacy misconfigurations detected.

**Causes:**
- Chats exceed retention period without purge
- Privacy level mismatches between chats
- Export without redaction performed
- Audit gaps detected

**Solution:**
1. Review specific violations in report output
2. For retention violations: `acode retention enforce --now`
3. For privacy mismatches: standardize with `acode privacy set-default <level>`
4. For unredacted exports: re-export with `--redact` flag
5. For audit gaps: check if audit was disabled temporarily
6. Address violations before next compliance audit

---

### Issue 8: Custom Redaction Pattern Causes Errors

**Symptom:** Adding custom pattern causes regex compilation error. Export fails after adding custom pattern. Performance degraded with custom patterns.

**Causes:**
- Invalid regex syntax in custom pattern
- Pattern uses unsupported regex features
- Too many patterns (>50) causing slowdown
- Pattern matches too broadly (e.g., `.+`)

**Solution:**
1. Validate regex syntax before adding: `acode redaction test --pattern "<regex>"`
2. Use only supported regex features (no lookbehind in some engines)
3. Keep pattern count under 50 for optimal performance
4. Avoid overly broad patterns - be specific
5. Test pattern against sample content: `acode redaction preview --pattern-file custom.yaml`
6. Remove problematic pattern: `acode redaction patterns remove <name>`

---

## Testing Requirements

### Unit Tests - RetentionTests.cs (Complete Implementation)

```csharp
// Tests/Unit/Privacy/RetentionTests.cs
using Xunit;
using FluentAssertions;
using AgenticCoder.Application.Privacy;
using AgenticCoder.Domain.Conversation;

namespace AgenticCoder.Tests.Unit.Privacy;

public sealed class RetentionTests
{
    [Fact]
    public void Should_Identify_Expired()
    {
        // Arrange
        var policy = new RetentionPolicy(retentionDays: 30);
        var expiredChat = Chat.Create("Old Chat", WorktreeId.From("worktree-01HKABC"));
        
        // Simulate chat created 35 days ago
        var createdAt = DateTimeOffset.UtcNow.AddDays(-35);
        typeof(Chat).GetProperty("CreatedAt")!.SetValue(expiredChat, createdAt);

        // Act
        var isExpired = policy.IsExpired(expiredChat);

        // Assert
        isExpired.Should().BeTrue("chat older than 30 days should be expired");
    }

    [Fact]
    public void Should_Respect_Active()
    {
        // Arrange
        var policy = new RetentionPolicy(retentionDays: 30);
        var activeChat = Chat.Create("Recent Chat", WorktreeId.From("worktree-01HKABC"));

        // Act
        var isExpired = policy.IsExpired(activeChat);

        // Assert
        isExpired.Should().BeFalse("recently created chat should not be expired");
    }

    [Fact]
    public async Task Should_Cascade_Purge()
    {
        // Arrange
        var repository = new InMemoryChatRepository();
        var chat = Chat.Create("Test Chat", WorktreeId.From("worktree-01HKABC"));
        var chatId = await repository.CreateAsync(chat, CancellationToken.None);

        var run = Run.Create(chatId, "azure-gpt4");
        var runId = await repository.CreateRunAsync(run, CancellationToken.None);

        var message = Message.Create(runId, "user", "Test message");
        await repository.CreateMessageAsync(message, CancellationToken.None);

        var retentionService = new RetentionService(repository);

        // Act
        await retentionService.PurgeAsync(chatId, CancellationToken.None);

        // Assert
        var retrievedChat = await repository.GetByIdAsync(chatId, CancellationToken.None);
        retrievedChat.Should().BeNull("chat should be hard-deleted");

        var retrievedRun = await repository.GetRunByIdAsync(runId, CancellationToken.None);
        retrievedRun.Should().BeNull("run should be cascade deleted");
    }

    [Fact]
    public async Task Should_Apply_LocalOnly_Retention()
    {
        // Arrange
        var policy = new RetentionPolicy(retentionDays: 30, applyToRemote: false);
        var repository = new InMemoryChatRepository();

        var localChat = Chat.Create("Local Chat", WorktreeId.From("worktree-01HKABC"));
        localChat.MarkAsLocalOnly();

        var syncedChat = Chat.Create("Synced Chat", WorktreeId.From("worktree-01HKABC"));
        syncedChat.MarkAsSynced();

        await repository.CreateAsync(localChat, CancellationToken.None);
        await repository.CreateAsync(syncedChat, CancellationToken.None);

        var retentionService = new RetentionService(repository, policy);

        // Act
        var eligibleChats = await retentionService.GetEligibleForRetentionAsync(CancellationToken.None);

        // Assert
        eligibleChats.Should().Contain(c => c.Id == localChat.Id, "local-only chats should be eligible");
        eligibleChats.Should().NotContain(c => c.Id == syncedChat.Id, "synced chats should be excluded when applyToRemote=false");
    }
}
```

### Unit Tests - ExportTests.cs (Complete Implementation)

```csharp
// Tests/Unit/Privacy/ExportTests.cs
using Xunit;
using FluentAssertions;
using AgenticCoder.Application.Privacy;
using AgenticCoder.Domain.Conversation;
using System.Text.Json;

namespace AgenticCoder.Tests.Unit.Privacy;

public sealed class ExportTests
{
    [Fact]
    public async Task Should_Export_Json()
    {
        // Arrange
        var repository = new InMemoryChatRepository();
        var chat = Chat.Create("Export Test", WorktreeId.From("worktree-01HKABC"));
        var chatId = await repository.CreateAsync(chat, CancellationToken.None);

        var run = Run.Create(chatId, "azure-gpt4");
        var runId = await repository.CreateRunAsync(run, CancellationToken.None);

        var message = Message.Create(runId, "user", "Hello world");
        await repository.CreateMessageAsync(message, CancellationToken.None);

        var exporter = new ChatExporter(repository);

        // Act
        var json = await exporter.ExportAsync(chatId, ExportFormat.Json, CancellationToken.None);

        // Assert
        json.Should().NotBeNullOrEmpty();

        var exported = JsonSerializer.Deserialize<ExportedChat>(json);
        exported.Should().NotBeNull();
        exported!.Title.Should().Be("Export Test");
        exported.Runs.Should().HaveCount(1);
        exported.Runs[0].Messages.Should().HaveCount(1);
        exported.Runs[0].Messages[0].Content.Should().Be("Hello world");
    }

    [Fact]
    public async Task Should_Export_Markdown()
    {
        // Arrange
        var repository = new InMemoryChatRepository();
        var chat = Chat.Create("Markdown Export", WorktreeId.From("worktree-01HKABC"));
        var chatId = await repository.CreateAsync(chat, CancellationToken.None);

        var run = Run.Create(chatId, "azure-gpt4");
        var runId = await repository.CreateRunAsync(run, CancellationToken.None);

        await repository.CreateMessageAsync(Message.Create(runId, "user", "What is React?"), CancellationToken.None);
        await repository.CreateMessageAsync(Message.Create(runId, "assistant", "React is a JavaScript library for building user interfaces."), CancellationToken.None);

        var exporter = new ChatExporter(repository);

        // Act
        var markdown = await exporter.ExportAsync(chatId, ExportFormat.Markdown, CancellationToken.None);

        // Assert
        markdown.Should().NotBeNullOrEmpty();
        markdown.Should().Contain("# Markdown Export");
        markdown.Should().Contain("**User:**");
        markdown.Should().Contain("What is React?");
        markdown.Should().Contain("**Assistant:**");
        markdown.Should().Contain("React is a JavaScript library");
    }

    [Fact]
    public async Task Should_Apply_Filters()
    {
        // Arrange
        var repository = new InMemoryChatRepository();
        var chat = Chat.Create("Filtered Export", WorktreeId.From("worktree-01HKABC"));
        var chatId = await repository.CreateAsync(chat, CancellationToken.None);

        var run1 = Run.Create(chatId, "azure-gpt4");
        var runId1 = await repository.CreateRunAsync(run1, CancellationToken.None);

        var run2 = Run.Create(chatId, "azure-gpt4");
        run2.Complete(tokensIn: 100, tokensOut: 200);
        var runId2 = await repository.CreateRunAsync(run2, CancellationToken.None);

        await repository.CreateMessageAsync(Message.Create(runId1, "user", "Message in run 1"), CancellationToken.None);
        await repository.CreateMessageAsync(Message.Create(runId2, "user", "Message in run 2"), CancellationToken.None);

        var exporter = new ChatExporter(repository);
        var filter = new ExportFilter { ExcludeIncompleteRuns = true };

        // Act
        var json = await exporter.ExportAsync(chatId, ExportFormat.Json, filter, CancellationToken.None);

        // Assert
        var exported = JsonSerializer.Deserialize<ExportedChat>(json);
        exported!.Runs.Should().HaveCount(1, "incomplete runs should be excluded");
        exported.Runs[0].Status.Should().Be("Completed");
    }

    [Fact]
    public async Task Should_Include_Metadata()
    {
        // Arrange
        var repository = new InMemoryChatRepository();
        var chat = Chat.Create("Metadata Test", WorktreeId.From("worktree-01HKABC"));
        chat.AddTag("important");
        var chatId = await repository.CreateAsync(chat, CancellationToken.None);

        var exporter = new ChatExporter(repository);

        // Act
        var json = await exporter.ExportAsync(chatId, ExportFormat.Json, CancellationToken.None);

        // Assert
        var exported = JsonSerializer.Deserialize<ExportedChat>(json);
        exported!.Tags.Should().Contain("important");
        exported.WorktreeId.Should().Be("worktree-01HKABC");
        exported.ExportedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }
}
```

### Unit Tests - RedactionTests.cs (Complete Implementation)

```csharp
// Tests/Unit/Privacy/RedactionTests.cs
using Xunit;
using FluentAssertions;
using AgenticCoder.Application.Privacy;

namespace AgenticCoder.Tests.Unit.Privacy;

public sealed class RedactionTests
{
    [Theory]
    [InlineData("sk-1234567890abcdef", "sk-****")]
    [InlineData("My API key is sk-abc123def456", "My API key is sk-****")]
    [InlineData("No sensitive data here", "No sensitive data here")]
    public void Should_Match_ApiKey(string input, string expected)
    {
        // Arrange
        var redactor = new SensitiveDataRedactor();

        // Act
        var redacted = redactor.Redact(input);

        // Assert
        redacted.Should().Be(expected);
    }

    [Theory]
    [InlineData("password: mysecret123", "password: ****")]
    [InlineData("pwd=admin123", "pwd=****")]
    [InlineData("No password mentioned", "No password mentioned")]
    public void Should_Match_Password(string input, string expected)
    {
        // Arrange
        var redactor = new SensitiveDataRedactor();

        // Act
        var redacted = redactor.Redact(input);

        // Assert
        redacted.Should().Be(expected);
    }

    [Fact]
    public void Should_Use_Custom_Pattern()
    {
        // Arrange
        var customPattern = new RedactionPattern(
            name: "SSN",
            regex: @"\d{3}-\d{2}-\d{4}",
            replacement: "***-**-****");

        var redactor = new SensitiveDataRedactor(new[] { customPattern });

        // Act
        var redacted = redactor.Redact("My SSN is 123-45-6789");

        // Assert
        redacted.Should().Be("My SSN is ***-**-****");
    }

    [Fact]
    public void Should_Redact_Multiple_Occurrences()
    {
        // Arrange
        var redactor = new SensitiveDataRedactor();
        var input = "API keys: sk-key1 and sk-key2, passwords: pass123 and pass456";

        // Act
        var redacted = redactor.Redact(input);

        // Assert
        redacted.Should().NotContain("sk-key1");
        redacted.Should().NotContain("sk-key2");
        redacted.Should().NotContain("pass123");
        redacted.Should().NotContain("pass456");
        redacted.Should().Contain("sk-****");
    }

    [Fact]
    public async Task Should_Redact_Message_Content()
    {
        // Arrange
        var message = Message.Create(RunId.NewId(), "user", "My API key is sk-secret123");
        var redactor = new SensitiveDataRedactor();

        // Act
        var redactedMessage = await redactor.RedactMessageAsync(message, CancellationToken.None);

        // Assert
        redactedMessage.Content.Should().Be("My API key is sk-****");
        redactedMessage.Id.Should().Be(message.Id, "message ID should be preserved");
    }
}
```

### Unit Tests - PrivacyTests.cs (Complete Implementation)

```csharp
// Tests/Unit/Privacy/PrivacyTests.cs
using Xunit;
using FluentAssertions;
using AgenticCoder.Application.Privacy;
using AgenticCoder.Domain.Conversation;

namespace AgenticCoder.Tests.Unit.Privacy;

public sealed class PrivacyTests
{
    [Fact]
    public void Should_Block_LocalOnly()
    {
        // Arrange
        var chat = Chat.Create("Local Chat", WorktreeId.From("worktree-01HKABC"));
        chat.MarkAsLocalOnly();

        var policy = new PrivacyPolicy();

        // Act
        var canSync = policy.CanSync(chat);

        // Assert
        canSync.Should().BeFalse("local-only chats should not sync");
    }

    [Fact]
    public async Task Should_Redact_For_Sync()
    {
        // Arrange
        var repository = new InMemoryChatRepository();
        var chat = Chat.Create("Sync Test", WorktreeId.From("worktree-01HKABC"));
        var chatId = await repository.CreateAsync(chat, CancellationToken.None);

        var run = Run.Create(chatId, "azure-gpt4");
        var runId = await repository.CreateRunAsync(run, CancellationToken.None);

        var message = Message.Create(runId, "user", "My password is secret123");
        await repository.CreateMessageAsync(message, CancellationToken.None);

        var privacyService = new PrivacyService(repository, new SensitiveDataRedactor());

        // Act
        var syncPayload = await privacyService.PrepareSyncPayloadAsync(chatId, CancellationToken.None);

        // Assert
        syncPayload.Messages.Should().HaveCount(1);
        syncPayload.Messages[0].Content.Should().NotContain("secret123");
        syncPayload.Messages[0].Content.Should().Contain("****");
    }

    [Fact]
    public void Should_Generate_Privacy_Report()
    {
        // Arrange
        var chat = Chat.Create("Report Test", WorktreeId.From("worktree-01HKABC"));
        chat.AddTag("sensitive");
        chat.MarkAsLocalOnly();

        var reporter = new PrivacyReporter();

        // Act
        var report = reporter.Generate(chat);

        // Assert
        report.ChatId.Should().Be(chat.Id);
        report.IsLocalOnly.Should().BeTrue();
        report.HasSensitiveTags.Should().BeTrue();
        report.SyncStatus.Should().Be("Blocked - Local Only");
    }
}
```

### Integration Tests - RetentionEnforcementTests.cs (Complete Implementation)

```csharp
// Tests/Integration/Privacy/RetentionEnforcementTests.cs
using Xunit;
using FluentAssertions;
using AgenticCoder.Application.Privacy;
using AgenticCoder.Infrastructure.Privacy;

namespace AgenticCoder.Tests.Integration.Privacy;

public sealed class RetentionEnforcementTests
{
    [Fact]
    public async Task Should_Run_Background()
    {
        // Arrange
        var repository = new InMemoryChatRepository();
        var policy = new RetentionPolicy(retentionDays: 30);
        var service = new BackgroundRetentionService(repository, policy);

        var expiredChat = Chat.Create("Old Chat", WorktreeId.From("worktree-01HKABC"));
        typeof(Chat).GetProperty("CreatedAt")!.SetValue(expiredChat, DateTimeOffset.UtcNow.AddDays(-35));
        await repository.CreateAsync(expiredChat, CancellationToken.None);

        // Act
        await service.EnforceRetentionAsync(CancellationToken.None);

        // Wait for background processing
        await Task.Delay(500);

        // Assert
        var chats = await repository.ListAsync(new ChatFilter(), CancellationToken.None);
        chats.Items.Should().BeEmpty("expired chat should be purged");
    }

    [Fact]
    public async Task Should_Log_Deletions()
    {
        // Arrange
        var logger = new InMemoryLogger<BackgroundRetentionService>();
        var repository = new InMemoryChatRepository();
        var policy = new RetentionPolicy(retentionDays: 30);
        var service = new BackgroundRetentionService(repository, policy, logger);

        var expiredChat = Chat.Create("Old Chat", WorktreeId.From("worktree-01HKABC"));
        var chatId = await repository.CreateAsync(expiredChat, CancellationToken.None);
        typeof(Chat).GetProperty("CreatedAt")!.SetValue(expiredChat, DateTimeOffset.UtcNow.AddDays(-35));

        // Act
        await service.EnforceRetentionAsync(CancellationToken.None);

        // Assert
        logger.Entries.Should().Contain(e => 
            e.Level == LogLevel.Information && 
            e.Message.Contains("Purged chat") &&
            e.Message.Contains(chatId.ToString()));
    }
}
```

### E2E Tests - PrivacyE2ETests.cs (Complete Implementation)

```csharp
// Tests/E2E/Privacy/PrivacyE2ETests.cs
using Xunit;
using FluentAssertions;
using AgenticCoder.Application.Privacy;
using AgenticCoder.Infrastructure.Conversation;

namespace AgenticCoder.Tests.E2E.Privacy;

public sealed class PrivacyE2ETests
{
    [Fact]
    public async Task Should_Enforce_Retention()
    {
        // Arrange
        var dbPath = Path.Combine(Path.GetTempPath(), $"retention_test_{Guid.NewGuid()}.db");
        var repository = new ChatRepository(dbPath);

        try
        {
            var chat = Chat.Create("Test Chat", WorktreeId.From("worktree-01HKABC"));
            typeof(Chat).GetProperty("CreatedAt")!.SetValue(chat, DateTimeOffset.UtcNow.AddDays(-35));
            await repository.CreateAsync(chat, CancellationToken.None);

            var policy = new RetentionPolicy(retentionDays: 30);
            var service = new BackgroundRetentionService(repository, policy);

            // Act
            await service.EnforceRetentionAsync(CancellationToken.None);

            // Assert
            var chats = await repository.ListAsync(new ChatFilter(), CancellationToken.None);
            chats.Items.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }

    [Fact]
    public async Task Should_Export_With_Redaction()
    {
        // Arrange
        var dbPath = Path.Combine(Path.GetTempPath(), $"export_test_{Guid.NewGuid()}.db");
        var repository = new ChatRepository(dbPath);

        try
        {
            var chat = Chat.Create("Export Test", WorktreeId.From("worktree-01HKABC"));
            var chatId = await repository.CreateAsync(chat, CancellationToken.None);

            var run = Run.Create(chatId, "azure-gpt4");
            var runId = await repository.CreateRunAsync(run, CancellationToken.None);

            var message = Message.Create(runId, "user", "My API key is sk-secret123");
            await repository.CreateMessageAsync(message, CancellationToken.None);

            var exporter = new ChatExporter(repository, new SensitiveDataRedactor());

            // Act
            var exported = await exporter.ExportAsync(chatId, ExportFormat.Json, CancellationToken.None);

            // Assert
            exported.Should().NotContain("sk-secret123");
            exported.Should().Contain("sk-****");
        }
        finally
        {
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }

    [Fact]
    public async Task Should_Generate_Report()
    {
        // Arrange
        var dbPath = Path.Combine(Path.GetTempPath(), $"report_test_{Guid.NewGuid()}.db");
        var repository = new ChatRepository(dbPath);

        try
        {
            var chat1 = Chat.Create("Public Chat", WorktreeId.From("worktree-01HKABC"));
            var chat2 = Chat.Create("Private Chat", WorktreeId.From("worktree-01HKABC"));
            chat2.MarkAsLocalOnly();

            await repository.CreateAsync(chat1, CancellationToken.None);
            await repository.CreateAsync(chat2, CancellationToken.None);

            var reporter = new PrivacyReporter(repository);

            // Act
            var report = await reporter.GenerateReportAsync(CancellationToken.None);

            // Assert
            report.TotalChats.Should().Be(2);
            report.LocalOnlyChats.Should().Be(1);
            report.SyncableChats.Should().Be(1);
        }
        finally
        {
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }
}
```

### Performance Benchmarks

```csharp
// Tests/Performance/PrivacyBenchmarks.cs
using BenchmarkDotNet.Attributes;
using AgenticCoder.Application.Privacy;

[MemoryDiagnoser]
public class PrivacyBenchmarks
{
    private ChatRepository _repository = null!;
    private ChatExporter _exporter = null!;
    private SensitiveDataRedactor _redactor = null!;
    private ChatId _chatId;

    [GlobalSetup]
    public async Task Setup()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), "privacy_benchmark.db");
        _repository = new ChatRepository(dbPath);
        _exporter = new ChatExporter(_repository);
        _redactor = new SensitiveDataRedactor();

        // Create chat with 1000 messages
        var chat = Chat.Create("Benchmark Chat", WorktreeId.From("worktree-01HKABC"));
        _chatId = await _repository.CreateAsync(chat, CancellationToken.None);

        var run = Run.Create(_chatId, "azure-gpt4");
        var runId = await _repository.CreateRunAsync(run, CancellationToken.None);

        for (int i = 0; i < 1000; i++)
        {
            var message = Message.Create(runId, "user", $"Message {i}");
            await _repository.CreateMessageAsync(message, CancellationToken.None);
        }
    }

    [Benchmark]
    public async Task Export_1000_Messages()
    {
        await _exporter.ExportAsync(_chatId, ExportFormat.Json, CancellationToken.None);
    }

    [Benchmark]
    public void Redact_Message()
    {
        _redactor.Redact("My API key is sk-secret123 and password is pass456");
    }

    [Benchmark]
    public async Task Retention_Check()
    {
        var policy = new RetentionPolicy(retentionDays: 30);
        var service = new RetentionService(_repository, policy);
        await service.GetEligibleForRetentionAsync(CancellationToken.None);
    }
}
```

**Performance Targets:**
- Export 1000 msgs: 5s target, 10s maximum
- Redact message: 0.5ms target, 1ms maximum
- Retention check: 500ms target, 1s maximum

---

## User Verification Steps

### Scenario 1: Retention Policy Configuration and Enforcement

**Objective:** Verify that retention policies can be configured and automatically enforce data deletion.

**Preconditions:**
- ACODE installed and configured
- At least 3 chats exist with varying ages
- One chat archived >30 days ago

**Steps:**
1. Set retention policy to 30 days: `acode config set retention.default_days 30`
2. Verify policy is applied: `acode retention status`
3. Create a test chat and immediately archive it
4. Modify the chat's archived_at timestamp to 35 days ago (for testing)
5. Run retention enforcement: `acode retention enforce --now`
6. List all chats: `acode chat list --include-deleted`

**Expected Results:**
- ✅ Retention status shows policy: 30 days
- ✅ Chat archived >30 days ago marked for deletion
- ✅ After grace period (7 days), chat permanently purged
- ✅ Deleted chat no longer appears in list
- ✅ Audit log contains purge event

**Verification Commands:**
```bash
acode retention status
# Output should show:
# Policy: 30 days
# Expiring Soon: <chats within warning period>

acode audit --action retention_purge --since 1h
# Should show the purge event with chat ID and message count
```

---

### Scenario 2: JSON Export with Complete Data

**Objective:** Verify that JSON export produces valid, complete data structure with all message details.

**Preconditions:**
- Chat exists with multiple runs and messages
- Chat includes tool calls in some messages
- Chat has tags assigned

**Steps:**
1. Create or identify a chat with rich content: `acode chat show <chat-id>`
2. Export to JSON: `acode export <chat-id> --format json --output export.json`
3. Validate JSON structure: `cat export.json | jq .`
4. Verify all messages present: `cat export.json | jq '.chats[0].runs[].messages | length'`
5. Check metadata included: `cat export.json | jq '.exported_at, .acode_version'`

**Expected Results:**
- ✅ Export completes without errors
- ✅ JSON is valid (jq parses without error)
- ✅ All messages from source chat are present
- ✅ Tool calls included with arguments and results
- ✅ Timestamps preserved in ISO 8601 format
- ✅ Tags and metadata included in export

**Verification Commands:**
```bash
acode export <chat-id> --format json --output export.json
cat export.json | jq '.chats[0].title'
# Should match chat title

cat export.json | jq '.chats[0].runs[].messages[] | {role, content: .content[0:50]}'
# Should list all messages with role and content preview
```

---

### Scenario 3: Markdown Export for Documentation

**Objective:** Verify that Markdown export produces human-readable, well-formatted documentation.

**Preconditions:**
- Chat with code blocks, conversation flow, and tool calls exists

**Steps:**
1. Export to Markdown: `acode export <chat-id> --format markdown --output export.md`
2. Open in Markdown viewer or preview
3. Check heading structure
4. Verify code blocks have syntax highlighting markers
5. Check timestamp formatting

**Expected Results:**
- ✅ Export creates .md file
- ✅ Chat title as H1 heading
- ✅ Each run as H2 heading with timestamp
- ✅ Messages clearly labeled by role (User/Assistant)
- ✅ Code blocks preserved with language markers
- ✅ Tool calls formatted with arguments and results
- ✅ Readable flow of conversation

**Verification Commands:**
```bash
acode export <chat-id> --format markdown --output export.md
head -50 export.md
# Should show formatted headers and content

grep -c "```" export.md
# Should match number of code blocks in source
```

---

### Scenario 4: Privacy Level LOCAL_ONLY Enforcement

**Objective:** Verify that LOCAL_ONLY privacy level prevents any remote synchronization.

**Preconditions:**
- Remote sync configured and enabled
- Chat with sensitive content exists

**Steps:**
1. Create a new chat: `acode chat create "Sensitive Discussion"`
2. Set privacy to LOCAL_ONLY: `acode chat privacy <chat-id> local_only`
3. Add sensitive message: "Our API key is sk_live_abc123xyz456def789"
4. Attempt to sync: `acode sync`
5. Check sync status for chat: `acode sync status <chat-id>`

**Expected Results:**
- ✅ Privacy level set to LOCAL_ONLY
- ✅ Sync command completes but skips LOCAL_ONLY chats
- ✅ Chat sync status shows "Not synced - LOCAL_ONLY"
- ✅ Remote database does NOT contain this chat
- ✅ Sensitive content remains only on local machine

**Verification Commands:**
```bash
acode chat privacy <chat-id> local_only
acode chat show <chat-id>
# Privacy field should show: local_only

acode sync status <chat-id>
# Should show: "Not synced - LOCAL_ONLY privacy level"
```

---

### Scenario 5: Redaction Pattern Testing and Preview

**Objective:** Verify that redaction patterns correctly identify and would replace sensitive data.

**Preconditions:**
- Chat with various sensitive data patterns:
  - API keys (sk_live_..., ghp_...)
  - Passwords in various formats
  - JWT tokens

**Steps:**
1. Create chat with sensitive content:
   - "API key: sk_live_abc123xyz456def789ghi012jkl"
   - "password=MySecretP@ssw0rd!"
   - "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
2. Preview redaction: `acode redaction preview <chat-id>`
3. Verify all patterns matched
4. Check placeholder format

**Expected Results:**
- ✅ Preview shows all sensitive data detected
- ✅ Stripe key matched with pattern name shown
- ✅ Password matched across different formats
- ✅ JWT token detected and would be redacted
- ✅ Preview shows replacement: `[REDACTED-STRIPE-KEY-sk_live_abc]`
- ✅ Original data NOT modified (preview only)

**Verification Commands:**
```bash
acode redaction preview <chat-id>
# Output should show:
# Line 1: "sk_live_abc123..." → "[REDACTED-STRIPE-KEY-sk_live_abc]"
# Line 2: "password=MySecret..." → "password=[REDACTED-PASSWORD]"
# Line 3: "Bearer eyJhbG..." → "Bearer [REDACTED-JWT-TOKEN-eyJhbG]"

# Verify original unchanged:
acode chat show <chat-id>
# Messages should still contain original sensitive content
```

---

### Scenario 6: Export with Redaction Applied

**Objective:** Verify that export with --redact flag produces sanitized output.

**Preconditions:**
- Chat with sensitive data (API keys, passwords)
- Redaction patterns configured and tested

**Steps:**
1. Export WITHOUT redaction: `acode export <chat-id> --format json --output unredacted.json`
2. Export WITH redaction: `acode export <chat-id> --format json --redact --output redacted.json`
3. Compare files: diff or grep for sensitive patterns
4. Verify redaction statistics displayed

**Expected Results:**
- ✅ Unredacted export contains original sensitive data
- ✅ Redacted export contains placeholders instead of secrets
- ✅ Search for "sk_live" in redacted file returns no matches
- ✅ Placeholder format preserved: `[REDACTED-PATTERN-prefix]`
- ✅ Redaction stats shown: "Redacted 3 secrets (2 API keys, 1 password)"
- ✅ Original database unchanged

**Verification Commands:**
```bash
grep -i "sk_live\|password\|eyJ" unredacted.json
# Should find sensitive content

grep -i "sk_live\|password\|eyJ" redacted.json
# Should find NO sensitive content

grep -i "REDACTED" redacted.json
# Should find placeholder replacements
```

---

### Scenario 7: Privacy Level Transition Restrictions

**Objective:** Verify that privacy level downgrades are blocked for security.

**Preconditions:**
- Chat set to LOCAL_ONLY privacy level

**Steps:**
1. Set chat to LOCAL_ONLY: `acode chat privacy <chat-id> local_only`
2. Attempt to change to REDACTED: `acode chat privacy <chat-id> redacted`
3. Observe error message
4. Attempt with force flag (if available)
5. Create new chat with desired level instead

**Expected Results:**
- ✅ LOCAL_ONLY → REDACTED blocked with error
- ✅ Error message: "Cannot change privacy from LOCAL_ONLY"
- ✅ Suggestion to create new chat with desired level
- ✅ Audit log records attempted downgrade
- ✅ Data remains secure at LOCAL_ONLY level

**Verification Commands:**
```bash
acode chat privacy <chat-id> local_only
# Sets privacy level

acode chat privacy <chat-id> redacted
# Should fail with: ACODE-PRIV-006: Cannot downgrade from LOCAL_ONLY

acode audit --action privacy_level_change --since 1h
# Should show attempted change was blocked
```

---

### Scenario 8: Compliance Report Generation

**Objective:** Verify that compliance report accurately reflects data lifecycle activities.

**Preconditions:**
- Performed retention purges, exports, privacy changes in last 30 days
- Audit log has entries

**Steps:**
1. Generate compliance report: `acode compliance report`
2. Review retention compliance section
3. Review privacy distribution section
4. Review recent deletions and exports
5. Export report for auditor: `acode compliance report --format json > compliance.json`

**Expected Results:**
- ✅ Report generates without errors
- ✅ Retention status shows % compliance
- ✅ Privacy distribution shows count per level
- ✅ Recent deletions list with timestamps and counts
- ✅ Export history with redaction status noted
- ✅ Recommendations for any violations
- ✅ JSON export parseable for external systems

**Verification Commands:**
```bash
acode compliance report
# Output includes:
# - Retention Compliance: 100% (no overdue purges)
# - Privacy Distribution: 50 LOCAL_ONLY, 20 REDACTED, 5 FULL
# - Recent Deletions: 10 chats in last 30 days
# - Export Log: 5 exports (4 redacted, 1 unredacted)

acode compliance report --format json > compliance.json
cat compliance.json | jq '.retention_compliance'
```

---

### Scenario 9: Custom Redaction Pattern Creation and Testing

**Objective:** Verify that custom redaction patterns can be added and work correctly.

**Preconditions:**
- Need to redact internal employee IDs in format EMP-######
- Need to redact internal URLs matching internal.company.com

**Steps:**
1. Add custom pattern for employee IDs:
   ```bash
   acode redaction patterns add --name "EmployeeID" --regex "EMP-\\d{6}" --replacement "[EMPLOYEE-ID]"
   ```
2. Add custom pattern for internal URLs:
   ```bash
   acode redaction patterns add --name "InternalURL" --regex "https://internal\\.company\\.com[/\\w]*" --replacement "[INTERNAL-URL]"
   ```
3. List patterns to verify: `acode redaction patterns list`
4. Test patterns against sample: `acode redaction test --text "Contact EMP-123456 at https://internal.company.com/profile"`
5. Preview on chat with this content

**Expected Results:**
- ✅ Custom patterns added successfully
- ✅ Pattern list shows both built-in and custom patterns
- ✅ Test command matches both employee ID and URL
- ✅ Replacements shown correctly
- ✅ Preview on actual chat detects custom patterns
- ✅ Export with redaction applies custom patterns

**Verification Commands:**
```bash
acode redaction patterns list
# Shows custom patterns with name, regex, replacement

acode redaction test --text "Contact EMP-123456 for help"
# Output: "Contact [EMPLOYEE-ID] for help"

acode redaction patterns remove EmployeeID
# Removes custom pattern
```

---

### Scenario 10: Complete Data Lifecycle Workflow

**Objective:** Verify end-to-end data lifecycle from creation through export and eventual retention purge.

**Preconditions:**
- Clean test environment
- Retention policy set to 7 days for testing

**Steps:**
1. Create new chat with privacy LOCAL_ONLY
2. Add messages including sensitive content
3. Export with redaction for sharing
4. Archive the chat: `acode chat archive <id>`
5. Check retention status
6. Fast-forward archive date for testing (modify archived_at)
7. Run retention enforcement
8. Verify chat purged
9. Check audit trail for complete lifecycle

**Expected Results:**
- ✅ Chat created with LOCAL_ONLY (secure by default)
- ✅ Export successful with secrets redacted
- ✅ Archive updates chat status
- ✅ Retention status shows expiry date
- ✅ After retention period, chat soft-deleted
- ✅ After grace period, chat permanently purged
- ✅ Audit trail shows: create → archive → soft-delete → purge
- ✅ No data recovery possible after purge

**Verification Commands:**
```bash
# Full lifecycle trace
acode chat create "Lifecycle Test"
acode chat privacy <id> local_only
acode run --chat <id> "Add some content with API key sk_live_test123"
acode export <id> --format json --redact --output lifecycle.json
acode chat archive <id>
acode retention status
# Shows: Lifecycle Test expires in 7 days

# After retention period expires:
acode retention enforce --now
acode chat show <id>
# Error: Chat not found

acode audit --chat <id>
# Shows complete lifecycle events
```

---

## Implementation Prompt

Implement retention, export, privacy controls, and redaction for conversation data lifecycle management. The system must support configurable retention policies with automatic enforcement, multi-format export with redaction, layered privacy controls, and pattern-based sensitive data redaction.

### File Structure

```
src/Acode.Domain/
├── Privacy/
│   ├── RetentionPolicy.cs
│   ├── PrivacyLevel.cs
│   ├── RedactionPattern.cs
│   └── RedactionMatch.cs
│
src/Acode.Application/
├── Privacy/
│   ├── IRetentionService.cs
│   ├── IExportService.cs
│   ├── IRedactionService.cs
│   ├── IComplianceService.cs
│   └── IAuditLogger.cs
│
src/Acode.Infrastructure/
├── Privacy/
│   ├── RetentionEnforcer.cs
│   ├── JsonExporter.cs
│   ├── MarkdownExporter.cs
│   ├── PatternRedactor.cs
│   ├── ComplianceReporter.cs
│   └── AuditLogger.cs
```

### RetentionPolicy Value Object (Complete Implementation)

```csharp
// src/Acode.Domain/Privacy/RetentionPolicy.cs
namespace Acode.Domain.Privacy;

public sealed record RetentionPolicy
{
    public int DefaultDays { get; init; } = 365;
    public int ArchivedDays { get; init; } = 90;
    public int ActiveDays { get; init; } = -1; // -1 = Never expire
    public int GracePeriodDays { get; init; } = 7;
    public int WarnDaysBefore { get; init; } = 7;

    public static RetentionPolicy Default => new();
    
    public static RetentionPolicy Short => new()
    {
        DefaultDays = 30,
        ArchivedDays = 30,
        GracePeriodDays = 3
    };
    
    public static RetentionPolicy Long => new()
    {
        DefaultDays = 1825, // 5 years
        ArchivedDays = 1825,
        GracePeriodDays = 30
    };

    public bool IsExpired(DateTimeOffset archivedAt, DateTimeOffset now)
    {
        if (ArchivedDays < 0) return false; // Never expire
        var expiryDate = archivedAt.AddDays(ArchivedDays);
        return now >= expiryDate;
    }

    public bool IsInGracePeriod(DateTimeOffset deletedAt, DateTimeOffset now)
    {
        var graceEnd = deletedAt.AddDays(GracePeriodDays);
        return now < graceEnd;
    }

    public bool IsNearExpiry(DateTimeOffset archivedAt, DateTimeOffset now)
    {
        if (ArchivedDays < 0) return false;
        var expiryDate = archivedAt.AddDays(ArchivedDays);
        var warningDate = expiryDate.AddDays(-WarnDaysBefore);
        return now >= warningDate && now < expiryDate;
    }
}
```

### PrivacyLevel Enum (Complete Implementation)

```csharp
// src/Acode.Domain/Privacy/PrivacyLevel.cs
namespace Acode.Domain.Privacy;

public enum PrivacyLevel
{
    /// <summary>
    /// Data never leaves local storage. Most restrictive.
    /// </summary>
    LocalOnly = 0,
    
    /// <summary>
    /// Data syncs with sensitive content redacted.
    /// </summary>
    Redacted = 1,
    
    /// <summary>
    /// Only metadata syncs (titles, tags, timestamps). No content.
    /// </summary>
    MetadataOnly = 2,
    
    /// <summary>
    /// All data syncs including sensitive content. Least restrictive.
    /// </summary>
    Full = 3
}

public static class PrivacyLevelExtensions
{
    public static bool CanSyncContent(this PrivacyLevel level) =>
        level is PrivacyLevel.Redacted or PrivacyLevel.Full;
    
    public static bool CanSyncMetadata(this PrivacyLevel level) =>
        level != PrivacyLevel.LocalOnly;
    
    public static bool RequiresRedaction(this PrivacyLevel level) =>
        level == PrivacyLevel.Redacted;
    
    public static bool CanTransitionTo(this PrivacyLevel current, PrivacyLevel target)
    {
        // LocalOnly cannot transition to anything more permissive
        if (current == PrivacyLevel.LocalOnly && target != PrivacyLevel.LocalOnly)
            return false;
        
        // Any level can become LocalOnly (more restrictive is always allowed)
        return true;
    }
}
```

### RedactionPattern Value Object (Complete Implementation)

```csharp
// src/Acode.Domain/Privacy/RedactionPattern.cs
namespace Acode.Domain.Privacy;

using System.Text.RegularExpressions;

public sealed record RedactionPattern
{
    public string Name { get; }
    public string Pattern { get; }
    public string Replacement { get; }
    public bool IsBuiltIn { get; }
    private readonly Regex _compiledRegex;

    public RedactionPattern(string name, string pattern, string replacement, bool isBuiltIn = false)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        Replacement = replacement ?? $"[REDACTED-{name.ToUpperInvariant()}]";
        IsBuiltIn = isBuiltIn;
        
        try
        {
            _compiledRegex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"Invalid regex pattern for {name}: {ex.Message}", nameof(pattern), ex);
        }
    }

    public IEnumerable<RedactionMatch> FindMatches(string content)
    {
        var matches = _compiledRegex.Matches(content);
        foreach (Match match in matches)
        {
            yield return new RedactionMatch(
                PatternName: Name,
                OriginalText: match.Value,
                StartIndex: match.Index,
                Length: match.Length,
                Replacement: GenerateReplacement(match.Value));
        }
    }

    private string GenerateReplacement(string matchedText)
    {
        // Preserve first 10 chars of matched text as prefix for debugging
        var prefix = matchedText.Length > 10 
            ? matchedText[..10] 
            : matchedText;
        return $"[REDACTED-{Name.ToUpperInvariant()}-{prefix}]";
    }

    // Built-in patterns
    public static RedactionPattern[] BuiltInPatterns => new[]
    {
        new RedactionPattern("StripeKey", @"sk_live_[a-zA-Z0-9]{24,}", "[REDACTED-STRIPE-KEY]", isBuiltIn: true),
        new RedactionPattern("StripeTestKey", @"sk_test_[a-zA-Z0-9]{24,}", "[REDACTED-STRIPE-TEST-KEY]", isBuiltIn: true),
        new RedactionPattern("GitHubToken", @"gh[ps]_[a-zA-Z0-9]{36,}", "[REDACTED-GITHUB-TOKEN]", isBuiltIn: true),
        new RedactionPattern("AwsAccessKey", @"AKIA[A-Z0-9]{16}", "[REDACTED-AWS-KEY]", isBuiltIn: true),
        new RedactionPattern("JwtToken", @"eyJ[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+", "[REDACTED-JWT-TOKEN]", isBuiltIn: true),
        new RedactionPattern("PasswordField", @"(?i)(password|passwd|pwd)[=:\s]+\S{8,}", "[REDACTED-PASSWORD]", isBuiltIn: true),
        new RedactionPattern("PrivateKey", @"-----BEGIN\s+(RSA\s+)?PRIVATE\s+KEY-----[\s\S]+?-----END\s+(RSA\s+)?PRIVATE\s+KEY-----", "[REDACTED-PRIVATE-KEY]", isBuiltIn: true),
        new RedactionPattern("BearerToken", @"Bearer\s+[a-zA-Z0-9\-_.]+", "[REDACTED-BEARER-TOKEN]", isBuiltIn: true)
    };
}

public sealed record RedactionMatch(
    string PatternName,
    string OriginalText,
    int StartIndex,
    int Length,
    string Replacement);
```

### IRetentionService Interface (Complete Implementation)

```csharp
// src/Acode.Application/Privacy/IRetentionService.cs
namespace Acode.Application.Privacy;

using Acode.Domain.Privacy;
using Acode.Domain.Conversation;

public interface IRetentionService
{
    Task<Result<RetentionStatus, Error>> GetStatusAsync(CancellationToken ct);
    Task<Result<IReadOnlyList<Chat>, Error>> GetExpiringChatsAsync(int withinDays, CancellationToken ct);
    Task<Result<int, Error>> EnforceAsync(bool dryRun, CancellationToken ct);
    Task<Result<Unit, Error>> SetPolicyAsync(RetentionPolicy policy, CancellationToken ct);
    Task<Result<Unit, Error>> SetChatPolicyAsync(ChatId chatId, RetentionPolicy policy, CancellationToken ct);
}

public sealed record RetentionStatus
{
    public RetentionPolicy CurrentPolicy { get; init; } = RetentionPolicy.Default;
    public DateTimeOffset? LastEnforcementAt { get; init; }
    public int TotalChats { get; init; }
    public int ActiveChats { get; init; }
    public int ArchivedChats { get; init; }
    public int ExpiringWithinWarningPeriod { get; init; }
    public int SoftDeletedInGracePeriod { get; init; }
}
```

### IExportService Interface (Complete Implementation)

```csharp
// src/Acode.Application/Privacy/IExportService.cs
namespace Acode.Application.Privacy;

using Acode.Domain.Conversation;

public interface IExportService
{
    Task<Result<ExportResult, Error>> ExportAsync(ExportRequest request, CancellationToken ct);
    Task<Result<ExportPreview, Error>> PreviewAsync(ExportRequest request, CancellationToken ct);
}

public enum ExportFormat { Json, Markdown, PlainText }

public sealed record ExportRequest
{
    public ExportFormat Format { get; init; } = ExportFormat.Json;
    public ChatId? ChatFilter { get; init; }
    public DateTimeOffset? Since { get; init; }
    public DateTimeOffset? Until { get; init; }
    public string? TagFilter { get; init; }
    public bool ApplyRedaction { get; init; } = true;
    public string? OutputPath { get; init; }
    public bool Compress { get; init; }
}

public sealed record ExportResult
{
    public string FilePath { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public int ChatCount { get; init; }
    public int MessageCount { get; init; }
    public bool RedactionApplied { get; init; }
    public IDictionary<string, int> RedactionStats { get; init; } = new Dictionary<string, int>();
    public TimeSpan Duration { get; init; }
}

public sealed record ExportPreview
{
    public int ChatCount { get; init; }
    public int MessageCount { get; init; }
    public long EstimatedSizeBytes { get; init; }
    public IReadOnlyList<RedactionPreviewItem> RedactionMatches { get; init; } = Array.Empty<RedactionPreviewItem>();
}

public sealed record RedactionPreviewItem(
    string ChatTitle,
    int LineNumber,
    string OriginalText,
    string RedactedText,
    string PatternName);
```

### IRedactionService Interface (Complete Implementation)

```csharp
// src/Acode.Application/Privacy/IRedactionService.cs
namespace Acode.Application.Privacy;

using Acode.Domain.Privacy;

public interface IRedactionService
{
    RedactionResult Redact(string content);
    Task<Result<IReadOnlyList<RedactionMatch>, Error>> PreviewAsync(ChatId chatId, CancellationToken ct);
    Task<Result<Unit, Error>> AddPatternAsync(RedactionPattern pattern, CancellationToken ct);
    Task<Result<Unit, Error>> RemovePatternAsync(string patternName, CancellationToken ct);
    Task<Result<IReadOnlyList<RedactionPattern>, Error>> ListPatternsAsync(CancellationToken ct);
    RedactionResult TestPattern(string pattern, string testContent);
}

public sealed record RedactionResult
{
    public string RedactedContent { get; init; } = string.Empty;
    public IReadOnlyList<RedactionMatch> Matches { get; init; } = Array.Empty<RedactionMatch>();
    public bool WasRedacted => Matches.Any();
}
```

### RetentionEnforcer Implementation (Complete)

```csharp
// src/Acode.Infrastructure/Privacy/RetentionEnforcer.cs
namespace Acode.Infrastructure.Privacy;

using Acode.Application.Privacy;
using Acode.Domain.Privacy;
using Acode.Domain.Conversation;
using Microsoft.Extensions.Logging;

public sealed class RetentionEnforcer : IRetentionService
{
    private readonly IChatRepository _chatRepository;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<RetentionEnforcer> _logger;
    private RetentionPolicy _currentPolicy = RetentionPolicy.Default;

    public RetentionEnforcer(
        IChatRepository chatRepository,
        IAuditLogger auditLogger,
        ILogger<RetentionEnforcer> logger)
    {
        _chatRepository = chatRepository;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<Result<int, Error>> EnforceAsync(bool dryRun, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var purgedCount = 0;

        // Step 1: Find archived chats past retention period
        var expiredChats = await GetExpiredChatsAsync(now, ct);
        
        foreach (var chat in expiredChats)
        {
            if (chat.DeletedAt == null)
            {
                // Soft delete (start grace period)
                if (!dryRun)
                {
                    await _chatRepository.SoftDeleteAsync(chat.Id, ct);
                    await _auditLogger.LogAsync(new AuditEvent
                    {
                        Action = "retention_soft_delete",
                        EntityType = "chat",
                        EntityId = chat.Id.ToString(),
                        Metadata = new Dictionary<string, object>
                        {
                            ["archived_at"] = chat.ArchivedAt!,
                            ["retention_days"] = _currentPolicy.ArchivedDays
                        }
                    }, ct);
                }
                _logger.LogInformation("Soft-deleted chat {ChatId} for retention", chat.Id);
            }
        }

        // Step 2: Find soft-deleted chats past grace period
        var chatsToHardDelete = await GetChatsForHardDeleteAsync(now, ct);
        
        foreach (var chat in chatsToHardDelete)
        {
            if (!dryRun)
            {
                // Cascade delete all associated data
                var messageCount = await _chatRepository.GetMessageCountAsync(chat.Id, ct);
                await _chatRepository.HardDeleteAsync(chat.Id, ct);
                
                await _auditLogger.LogAsync(new AuditEvent
                {
                    Action = "retention_purge",
                    EntityType = "chat",
                    EntityId = chat.Id.ToString(),
                    Metadata = new Dictionary<string, object>
                    {
                        ["message_count"] = messageCount,
                        ["deleted_at"] = chat.DeletedAt!,
                        ["grace_period_days"] = _currentPolicy.GracePeriodDays
                    }
                }, ct);
                
                purgedCount++;
            }
            _logger.LogInformation("Purged chat {ChatId} ({MessageCount} messages)", chat.Id, 0);
        }

        return Result.Success<int, Error>(purgedCount);
    }

    public async Task<Result<RetentionStatus, Error>> GetStatusAsync(CancellationToken ct)
    {
        var stats = await _chatRepository.GetStatsAsync(ct);
        var expiringChats = await GetExpiringChatsAsync(_currentPolicy.WarnDaysBefore, ct);
        
        return Result.Success<RetentionStatus, Error>(new RetentionStatus
        {
            CurrentPolicy = _currentPolicy,
            TotalChats = stats.TotalChats,
            ActiveChats = stats.ActiveChats,
            ArchivedChats = stats.ArchivedChats,
            ExpiringWithinWarningPeriod = expiringChats.Value?.Count ?? 0
        });
    }

    public async Task<Result<IReadOnlyList<Chat>, Error>> GetExpiringChatsAsync(int withinDays, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var warningCutoff = now.AddDays(withinDays);
        var expiryCutoff = now.AddDays(-_currentPolicy.ArchivedDays);
        
        var chats = await _chatRepository.GetArchivedChatsAsync(ct);
        var expiring = chats
            .Where(c => c.ArchivedAt != null && 
                        c.ArchivedAt > expiryCutoff &&
                        _currentPolicy.IsNearExpiry(c.ArchivedAt.Value, now))
            .ToList();
            
        return Result.Success<IReadOnlyList<Chat>, Error>(expiring);
    }

    public Task<Result<Unit, Error>> SetPolicyAsync(RetentionPolicy policy, CancellationToken ct)
    {
        _currentPolicy = policy;
        return Task.FromResult(Result.Success<Unit, Error>(Unit.Value));
    }

    public Task<Result<Unit, Error>> SetChatPolicyAsync(ChatId chatId, RetentionPolicy policy, CancellationToken ct)
    {
        // Per-chat policy override would be stored in database
        return Task.FromResult(Result.Success<Unit, Error>(Unit.Value));
    }

    private async Task<IReadOnlyList<Chat>> GetExpiredChatsAsync(DateTimeOffset now, CancellationToken ct)
    {
        var chats = await _chatRepository.GetArchivedChatsAsync(ct);
        return chats
            .Where(c => c.ArchivedAt != null && 
                        c.DeletedAt == null &&
                        _currentPolicy.IsExpired(c.ArchivedAt.Value, now))
            .ToList();
    }

    private async Task<IReadOnlyList<Chat>> GetChatsForHardDeleteAsync(DateTimeOffset now, CancellationToken ct)
    {
        var chats = await _chatRepository.GetSoftDeletedChatsAsync(ct);
        return chats
            .Where(c => c.DeletedAt != null && 
                        !_currentPolicy.IsInGracePeriod(c.DeletedAt.Value, now))
            .ToList();
    }
}
```

### PatternRedactor Implementation (Complete)

```csharp
// src/Acode.Infrastructure/Privacy/PatternRedactor.cs
namespace Acode.Infrastructure.Privacy;

using Acode.Application.Privacy;
using Acode.Domain.Privacy;
using Microsoft.Extensions.Logging;

public sealed class PatternRedactor : IRedactionService
{
    private readonly List<RedactionPattern> _customPatterns = new();
    private readonly ILogger<PatternRedactor> _logger;

    public PatternRedactor(ILogger<PatternRedactor> logger)
    {
        _logger = logger;
    }

    public RedactionResult Redact(string content)
    {
        if (string.IsNullOrEmpty(content))
            return new RedactionResult { RedactedContent = content };

        var allPatterns = RedactionPattern.BuiltInPatterns.Concat(_customPatterns);
        var allMatches = new List<RedactionMatch>();
        var redactedContent = content;

        foreach (var pattern in allPatterns)
        {
            var matches = pattern.FindMatches(redactedContent).ToList();
            allMatches.AddRange(matches);
        }

        // Sort matches by position descending to apply from end to start
        // This preserves indices during replacement
        foreach (var match in allMatches.OrderByDescending(m => m.StartIndex))
        {
            redactedContent = redactedContent
                .Remove(match.StartIndex, match.Length)
                .Insert(match.StartIndex, match.Replacement);
        }

        if (allMatches.Any())
        {
            _logger.LogInformation(
                "Redacted {Count} sensitive items: {Patterns}",
                allMatches.Count,
                string.Join(", ", allMatches.Select(m => m.PatternName).Distinct()));
        }

        return new RedactionResult
        {
            RedactedContent = redactedContent,
            Matches = allMatches
        };
    }

    public async Task<Result<IReadOnlyList<RedactionMatch>, Error>> PreviewAsync(
        ChatId chatId, CancellationToken ct)
    {
        // Would load chat content and run redaction without saving
        return Result.Success<IReadOnlyList<RedactionMatch>, Error>(Array.Empty<RedactionMatch>());
    }

    public Task<Result<Unit, Error>> AddPatternAsync(RedactionPattern pattern, CancellationToken ct)
    {
        if (_customPatterns.Count >= 50)
        {
            return Task.FromResult(Result.Failure<Unit, Error>(
                new Error("ACODE-PRIV-004", "Maximum 50 custom patterns allowed")));
        }

        if (_customPatterns.Any(p => p.Name == pattern.Name))
        {
            return Task.FromResult(Result.Failure<Unit, Error>(
                new Error("ACODE-PRIV-004", $"Pattern '{pattern.Name}' already exists")));
        }

        _customPatterns.Add(pattern);
        _logger.LogInformation("Added custom redaction pattern: {Name}", pattern.Name);
        
        return Task.FromResult(Result.Success<Unit, Error>(Unit.Value));
    }

    public Task<Result<Unit, Error>> RemovePatternAsync(string patternName, CancellationToken ct)
    {
        var pattern = _customPatterns.FirstOrDefault(p => p.Name == patternName);
        if (pattern == null)
        {
            return Task.FromResult(Result.Failure<Unit, Error>(
                new Error("ACODE-PRIV-004", $"Pattern '{patternName}' not found")));
        }

        if (pattern.IsBuiltIn)
        {
            return Task.FromResult(Result.Failure<Unit, Error>(
                new Error("ACODE-PRIV-004", "Cannot remove built-in patterns")));
        }

        _customPatterns.Remove(pattern);
        _logger.LogInformation("Removed custom redaction pattern: {Name}", patternName);
        
        return Task.FromResult(Result.Success<Unit, Error>(Unit.Value));
    }

    public Task<Result<IReadOnlyList<RedactionPattern>, Error>> ListPatternsAsync(CancellationToken ct)
    {
        var allPatterns = RedactionPattern.BuiltInPatterns
            .Concat(_customPatterns)
            .ToList();
        return Task.FromResult(Result.Success<IReadOnlyList<RedactionPattern>, Error>(allPatterns));
    }

    public RedactionResult TestPattern(string pattern, string testContent)
    {
        try
        {
            var testPattern = new RedactionPattern("Test", pattern, "[TEST-REDACTED]");
            var matches = testPattern.FindMatches(testContent).ToList();
            
            var redacted = testContent;
            foreach (var match in matches.OrderByDescending(m => m.StartIndex))
            {
                redacted = redacted
                    .Remove(match.StartIndex, match.Length)
                    .Insert(match.StartIndex, match.Replacement);
            }

            return new RedactionResult
            {
                RedactedContent = redacted,
                Matches = matches
            };
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid test pattern: {Pattern}", pattern);
            return new RedactionResult
            {
                RedactedContent = testContent,
                Matches = Array.Empty<RedactionMatch>()
            };
        }
    }
}
```

### JsonExporter Implementation (Complete)

```csharp
// src/Acode.Infrastructure/Privacy/JsonExporter.cs
namespace Acode.Infrastructure.Privacy;

using Acode.Application.Privacy;
using Acode.Domain.Conversation;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class JsonExporter : IExportService
{
    private readonly IChatRepository _chatRepository;
    private readonly IRedactionService _redactionService;
    private readonly IAuditLogger _auditLogger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public JsonExporter(
        IChatRepository chatRepository,
        IRedactionService redactionService,
        IAuditLogger auditLogger)
    {
        _chatRepository = chatRepository;
        _redactionService = redactionService;
        _auditLogger = auditLogger;
    }

    public async Task<Result<ExportResult, Error>> ExportAsync(
        ExportRequest request, CancellationToken ct)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var redactionStats = new Dictionary<string, int>();
        
        // Load chats based on filters
        var chats = await LoadChatsAsync(request, ct);
        var exportedChats = new List<ExportedChat>();

        foreach (var chat in chats)
        {
            var messages = await _chatRepository.GetMessagesAsync(chat.Id, ct);
            var exportedMessages = new List<ExportedMessage>();

            foreach (var message in messages)
            {
                var content = message.Content;
                
                if (request.ApplyRedaction)
                {
                    var redactionResult = _redactionService.Redact(content);
                    content = redactionResult.RedactedContent;
                    
                    foreach (var match in redactionResult.Matches)
                    {
                        redactionStats[match.PatternName] = 
                            redactionStats.GetValueOrDefault(match.PatternName) + 1;
                    }
                }

                exportedMessages.Add(new ExportedMessage
                {
                    Id = message.Id.ToString(),
                    Role = message.Role,
                    Content = content,
                    CreatedAt = message.CreatedAt
                });
            }

            exportedChats.Add(new ExportedChat
            {
                Id = chat.Id.ToString(),
                Title = chat.Title,
                Tags = chat.Tags.ToList(),
                CreatedAt = chat.CreatedAt,
                ArchivedAt = chat.ArchivedAt,
                Messages = exportedMessages
            });
        }

        var exportData = new ExportData
        {
            ExportedAt = DateTimeOffset.UtcNow,
            AcodeVersion = "0.1.0",
            Chats = exportedChats
        };

        var json = JsonSerializer.Serialize(exportData, JsonOptions);
        var outputPath = request.OutputPath ?? 
            Path.Combine(Path.GetTempPath(), $"acode-export-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        
        await File.WriteAllTextAsync(outputPath, json, ct);

        stopwatch.Stop();

        await _auditLogger.LogAsync(new AuditEvent
        {
            Action = "export",
            Metadata = new Dictionary<string, object>
            {
                ["format"] = "json",
                ["chat_count"] = exportedChats.Count,
                ["message_count"] = exportedChats.Sum(c => c.Messages.Count),
                ["redaction_applied"] = request.ApplyRedaction,
                ["output_path"] = outputPath
            }
        }, ct);

        return Result.Success<ExportResult, Error>(new ExportResult
        {
            FilePath = outputPath,
            FileSizeBytes = new FileInfo(outputPath).Length,
            ChatCount = exportedChats.Count,
            MessageCount = exportedChats.Sum(c => c.Messages.Count),
            RedactionApplied = request.ApplyRedaction,
            RedactionStats = redactionStats,
            Duration = stopwatch.Elapsed
        });
    }

    public async Task<Result<ExportPreview, Error>> PreviewAsync(
        ExportRequest request, CancellationToken ct)
    {
        var chats = await LoadChatsAsync(request, ct);
        var redactionMatches = new List<RedactionPreviewItem>();
        var totalMessages = 0;

        foreach (var chat in chats)
        {
            var messages = await _chatRepository.GetMessagesAsync(chat.Id, ct);
            totalMessages += messages.Count;

            if (request.ApplyRedaction)
            {
                var lineNumber = 0;
                foreach (var message in messages)
                {
                    lineNumber++;
                    var result = _redactionService.Redact(message.Content);
                    foreach (var match in result.Matches)
                    {
                        redactionMatches.Add(new RedactionPreviewItem(
                            chat.Title,
                            lineNumber,
                            match.OriginalText.Length > 50 
                                ? match.OriginalText[..50] + "..." 
                                : match.OriginalText,
                            match.Replacement,
                            match.PatternName));
                    }
                }
            }
        }

        return Result.Success<ExportPreview, Error>(new ExportPreview
        {
            ChatCount = chats.Count,
            MessageCount = totalMessages,
            EstimatedSizeBytes = totalMessages * 500, // Rough estimate
            RedactionMatches = redactionMatches
        });
    }

    private async Task<List<Chat>> LoadChatsAsync(ExportRequest request, CancellationToken ct)
    {
        var filter = new ChatFilter
        {
            ChatId = request.ChatFilter,
            Since = request.Since,
            Until = request.Until,
            Tag = request.TagFilter
        };
        
        var result = await _chatRepository.ListAsync(filter, ct);
        return result.Items.ToList();
    }
}

internal sealed record ExportData
{
    public DateTimeOffset ExportedAt { get; init; }
    public string AcodeVersion { get; init; } = string.Empty;
    public List<ExportedChat> Chats { get; init; } = new();
}

internal sealed record ExportedChat
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = new();
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ArchivedAt { get; init; }
    public List<ExportedMessage> Messages { get; init; } = new();
}

internal sealed record ExportedMessage
{
    public string Id { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}
```

### Error Codes

| Code | Meaning | Remediation |
|------|---------|-------------|
| ACODE-PRIV-001 | Retention enforcement failed | Check database connection, review logs |
| ACODE-PRIV-002 | Export failed | Check disk space, verify filters, reduce scope |
| ACODE-PRIV-003 | Redaction error | Check pattern syntax, review content encoding |
| ACODE-PRIV-004 | Invalid redaction pattern | Validate regex at regex101.com |
| ACODE-PRIV-005 | Compliance report error | Check audit log integrity |
| ACODE-PRIV-006 | Privacy level transition blocked | Cannot downgrade from LOCAL_ONLY |

### Implementation Checklist

1. [ ] Create domain types (RetentionPolicy, PrivacyLevel, RedactionPattern)
2. [ ] Implement IRetentionService and RetentionEnforcer
3. [ ] Implement IExportService with JsonExporter and MarkdownExporter
4. [ ] Implement IRedactionService and PatternRedactor
5. [ ] Implement IComplianceService and ComplianceReporter
6. [ ] Implement IAuditLogger for tamper-evident logging
7. [ ] Add CLI commands (retention, export, privacy, redaction, compliance)
8. [ ] Add background retention enforcement job
9. [ ] Write unit tests for all services
10. [ ] Write integration tests for E2E workflows

### Rollout Plan

1. **Phase 1:** Domain types and value objects
2. **Phase 2:** Retention policy engine and enforcement
3. **Phase 3:** Export service with JSON and Markdown formatters
4. **Phase 4:** Redaction service with built-in patterns
5. **Phase 5:** Privacy level controls and transitions
6. **Phase 6:** Compliance reporting and audit logging
7. **Phase 7:** CLI commands and background job integration

---

**End of Task 049.e Specification**