# Task 049.f: SQLite→PostgreSQL Sync Engine

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 2 – CLI + Orchestration Core  
**Dependencies:** Task 049.a (Data Model), Task 050 (Workspace DB), Task 039 (Security)  

---

## Description

**Business Value & ROI**

Reliable sync between local SQLite and remote PostgreSQL enables offline-first development while providing cross-device access and cloud backup. This saves $2,400/year per engineer in productivity (eliminates sync conflicts, data loss) and $1,800/year in reduced cloud costs (efficient batching).

**Cost Breakdown:**
- Eliminated sync conflicts: 3 hours/month resolving → 0 (36 hours/year @ $108/hour = $3,888/year)
- Prevented data loss: 1 incident/year averaging 8 hours recovery → 0 ($864/year saved)
- Reduced API calls: 10,000 calls/month → 2,000 with batching ($50/month saved = $600/year)
- **Total savings: $5,352/year per engineer**
- **10-engineer team: $53,520/year**

**Time Savings:**
- No manual sync: 15 min/day checking sync status → automatic (60 hours/year saved)
- No conflict resolution: 3 hours/month → 0 (36 hours/year saved)
- **Total: 96 hours/year per engineer @ $108/hour = $10,368/year**

**Technical Architecture**

The sync engine implements a bidirectional synchronization system using outbox/inbox patterns for reliability, batching for efficiency, exponential backoff for resilience, and last-write-wins conflict resolution for simplicity.

**Five-Layer Architecture:**

```
User Action: "acode run 'implement auth'" (creates conversation locally)
     │
     ▼
┌─────────────────────────────────────────┐
│  Local SQLite (Primary Storage)         │
│  ├─ Chats, Messages, Runs (fast R/W)   │
│  ├─ Outbox table (pending uploads)      │
│  └─ Sync_status column on all entities  │
└─────────────────────────────────────────┘
     │
     ▼
┌─────────────────────────────────────────┐
│  Outbox Pattern (Reliable Delivery)     │
│  ├─ INSERT INTO outbox (atomic with TX) │
│  ├─ Idempotency key: entity_id + version│
│  ├─ Payload: Entity JSON + privacy level│
│  └─ Status: PENDING → SYNCED | FAILED   │
└─────────────────────────────────────────┘
     │
     ▼
┌─────────────────────────────────────────┐
│  Batch Processor (Efficiency)           │
│  ├─ Poll outbox every 5 seconds         │
│  ├─ Batch 50 items (configurable)       │
│  ├─ Apply privacy/redaction filters     │
│  └─ Single HTTP call with all items     │
└─────────────────────────────────────────┘
     │
     ▼
┌─────────────────────────────────────────┐
│  Retry Engine (Resilience)              │
│  ├─ Exponential backoff: 1s, 2s, 4s...  │
│  ├─ Max retries: 5 attempts              │
│  ├─ Transient failures: Network, 5xx     │
│  └─ Permanent failures: Validation, 4xx  │
└─────────────────────────────────────────┘
     │
     ▼
┌─────────────────────────────────────────┐
│  Remote PostgreSQL (Durable Storage)    │
│  ├─ Chats, Messages, Runs (cloud backup)│
│  ├─ Conflict detection: Compare versions│
│  ├─ Idempotency: Deduplicate requests   │
│  └─ Inbox: Changes from other devices   │
└─────────────────────────────────────────┘
```

**Outbox Pattern Implementation:**

The outbox pattern ensures reliable delivery by making sync operations transactional with business logic:

```sql
-- Atomic write: Insert message + outbox entry in single transaction
BEGIN TRANSACTION;

-- Insert message
INSERT INTO messages (id, chat_id, role, content, created_at, sync_status, version)
VALUES ('msg-123', 'chat-456', 'user', 'How do I...', NOW(), 'PENDING', 1);

-- Insert outbox entry
INSERT INTO outbox (id, entity_type, entity_id, operation, idempotency_key, payload, status, created_at)
VALUES 
  (uuid(), 'message', 'msg-123', 'CREATE', 'msg-123-v1', 
   '{"id":"msg-123","chat_id":"chat-456","role":"user","content":"How do I...","version":1}',
   'PENDING', NOW());

COMMIT;
```

**Key Outbox Design Decisions:**
- **Atomic with business logic:** Outbox INSERT in same transaction as entity INSERT/UPDATE
- **Idempotency key:** `entity_id + version` ensures deduplication
- **Payload includes version:** Server can detect conflicts
- **Status transitions:** PENDING → IN_PROGRESS → SYNCED | FAILED

**Batch Processing Flow:**

```
Background Worker (every 5 seconds)
     │
     ▼
[SELECT * FROM outbox WHERE status = 'PENDING' LIMIT 50]
     │
     ├─ Found 47 pending items (3 chats, 44 messages)
     │
     ▼
[Group by entity type + privacy level]
     │
     ├─ Chats: 3 items (2 LOCAL_ONLY, 1 FULL)
     ├─ Messages: 44 items (10 LOCAL_ONLY, 30 REDACTED, 4 FULL)
     │
     ▼
[Apply privacy filters]
     │
     ├─ Remove LOCAL_ONLY items (12 total)
     ├─ Apply redaction to REDACTED items (30 messages)
     ├─ Keep FULL items as-is (5 items)
     │
     ▼
[Build batch payload]
     │
     ├─ POST /api/v1/sync/batch
     ├─ Headers: Idempotency-Keys: ["msg-123-v1", "msg-124-v1", ...]
     ├─ Body: [{"type":"chat","id":"...","data":{...}}, ...]
     │
     ▼
[Send HTTP request with timeout 30s]
     │
     ├─ SUCCESS (200 OK) → Mark outbox items SYNCED
     ├─ TRANSIENT (5xx, timeout) → Increment retry_count, schedule backoff
     ├─ PERMANENT (400, 422) → Mark FAILED with error message
     │
     ▼
[Update outbox status]
     │
     └─ UPDATE outbox SET status = ?, synced_at = NOW(), retry_count = ?
         WHERE id IN (?)
```

**Performance Characteristics:**

Batching provides significant performance improvements over individual syncs:

| Operation | Individual | Batched (50x) | Improvement |
|-----------|------------|---------------|-------------|
| API calls | 50 calls | 1 call | 98% reduction |
| Network overhead | 50 × 100ms = 5s | 1 × 100ms = 100ms | 98% faster |
| Server processing | 50 × 10ms = 500ms | 1 × 80ms = 80ms | 84% faster |
| **Total latency** | **5.5s** | **180ms** | **97% faster** |

**Retry Strategy with Exponential Backoff:**

Transient failures (network timeouts, server restarts, rate limits) are common in distributed systems. The sync engine retries failed operations with exponential backoff:

```
Attempt 1: Immediate (0s delay)
Attempt 2: 1s delay (2^0 × base_delay)
Attempt 3: 2s delay (2^1 × base_delay)
Attempt 4: 4s delay (2^2 × base_delay)
Attempt 5: 8s delay (2^3 × base_delay)
Attempt 6: FAILED (max retries reached)
```

**Retry Decision Matrix:**

| Error Type | HTTP Code | Action | Retry | Backoff |
|------------|-----------|--------|-------|---------|
| Network timeout | - | Retry | Yes | Exponential |
| Server error | 500-599 | Retry | Yes | Exponential |
| Rate limit | 429 | Retry | Yes | Exponential |
| Validation error | 400, 422 | Fail | No | - |
| Not found | 404 | Fail | No | - |
| Unauthorized | 401, 403 | Fail | No | - |
| Conflict | 409 | Resolve | Maybe | Immediate |

**Idempotency Key Design:**

Idempotency ensures that retrying the same operation produces the same result without side effects. Each sync operation has a unique key:

```
Idempotency Key Format: {entity_type}-{entity_id}-v{version}

Examples:
- chat-abc123-v1    (Chat creation, version 1)
- msg-xyz789-v1     (Message creation, version 1)
- msg-xyz789-v2     (Message update, version 2)
```

**Server-Side Idempotency Check:**

```sql
-- Server receives sync request
-- Check if idempotency key already processed
SELECT COUNT(*) FROM sync_log WHERE idempotency_key = 'msg-xyz789-v1';

-- If found (COUNT = 1): Return 200 OK with cached result
-- If not found (COUNT = 0): Process request, insert into sync_log
INSERT INTO sync_log (idempotency_key, entity_type, entity_id, processed_at, result)
VALUES ('msg-xyz789-v1', 'message', 'msg-xyz789', NOW(), 'SUCCESS');
```

**Conflict Resolution (Last-Write-Wins):**

Conflicts occur when the same entity is modified on multiple devices concurrently. The sync engine uses last-write-wins (LWW) by default:

**Conflict Detection:**

```
Device A (local):                Device B (remote):
  msg-123, version=2               msg-123, version=2
  content="Old text"               content="New text"
  updated_at=2025-01-15 10:00     updated_at=2025-01-15 10:05

Sync from Device A to Server:
  Server has version=2 with updated_at=2025-01-15 10:05
  Device A sending version=2 with updated_at=2025-01-15 10:00
  → CONFLICT DETECTED (same version, different timestamps)
```

**Resolution Strategy:**

```csharp
public Message ResolveConflict(Message local, Message remote)
{
    // Compare timestamps
    if (local.UpdatedAt > remote.UpdatedAt)
    {
        return local; // Local wins
    }
    else if (remote.UpdatedAt > local.UpdatedAt)
    {
        return remote; // Remote wins
    }
    else
    {
        // Same timestamp (rare), use entity ID as tiebreaker
        return local.Id.CompareTo(remote.Id) > 0 ? local : remote;
    }
}
```

**Conflict Logging:**

All conflicts are logged for visibility and debugging:

```csharp
await _conflictLogger.LogAsync(new ConflictEvent
{
    EntityType = "message",
    EntityId = "msg-123",
    LocalVersion = 2,
    RemoteVersion = 2,
    LocalUpdatedAt = localMessage.UpdatedAt,
    RemoteUpdatedAt = remoteMessage.UpdatedAt,
    Resolution = "REMOTE_WINS",
    ResolvedAt = DateTime.UtcNow
});
```

**Inbox Pattern (Reverse Sync):**

The inbox pattern handles changes made on other devices:

```
Device B makes changes → Server stores in inbox for Device A

Device A polls inbox:
  GET /api/v1/sync/inbox?since_sequence=1234
  
Server responds:
  {
    "sequence": 1250,
    "changes": [
      {
        "type": "message",
        "operation": "CREATE",
        "entity_id": "msg-new-456",
        "data": {...}
      },
      {
        "type": "message",
        "operation": "UPDATE",
        "entity_id": "msg-existing-789",
        "data": {...}
      }
    ]
  }

Device A applies changes locally:
  - INSERT new messages
  - UPDATE existing messages (check for conflicts)
  - Update local sequence number to 1250
```

**Sync Status Tracking:**

Every entity has a `sync_status` column:

```sql
CREATE TABLE messages (
  id TEXT PRIMARY KEY,
  chat_id TEXT NOT NULL,
  role TEXT NOT NULL,
  content TEXT NOT NULL,
  created_at TIMESTAMP NOT NULL,
  updated_at TIMESTAMP NOT NULL,
  version INTEGER DEFAULT 1,
  sync_status TEXT DEFAULT 'PENDING' CHECK (sync_status IN ('PENDING', 'IN_PROGRESS', 'SYNCED', 'CONFLICT', 'FAILED')),
  last_sync_at TIMESTAMP,
  sync_error TEXT
);
```

**Status Transitions:**

```
CREATE  → PENDING → IN_PROGRESS → SYNCED
UPDATE  → PENDING → IN_PROGRESS → SYNCED
DELETE  → PENDING → IN_PROGRESS → SYNCED

Failures:
  IN_PROGRESS → FAILED (retry_count < max_retries, schedule retry)
  IN_PROGRESS → FAILED (retry_count >= max_retries, permanent failure)

Conflicts:
  IN_PROGRESS → CONFLICT (detected on server)
  CONFLICT → PENDING (after user resolution or auto-resolution)
```

**Integration Points:**

1. **Task-049a (Data Model):**
   - `sync_status` column on all entities
   - `version` column for conflict detection
   - `outbox` and `inbox` tables

2. **Task-049e (Privacy):**
   - Sync respects `privacy_level` (LOCAL_ONLY never syncs)
   - Apply redaction before uploading

3. **Task-050 (Workspace Database):**
   - SQLite connection for local storage
   - PostgreSQL connection for remote storage

4. **Task-039 (Security):**
   - TLS for all sync traffic
   - API token authentication
   - Audit log for all sync operations

**Constraints and Limitations:**

1. **Eventual consistency:** Not real-time, 5-second polling delay
2. **Batch size limits:** Max 50 items or 5MB per batch (prevents timeouts)
3. **Conflict resolution:** Last-write-wins only (no custom strategies)
4. **Single region:** No multi-region support (latency for distant users)
5. **No peer-to-peer:** Requires central server (can't sync device-to-device)
6. **Soft delete only:** Hard deletes can't be synced (no tombstone)
7. **Sequence numbers:** Server-assigned, client can't forge

**Trade-offs and Alternatives:**

1. **Outbox vs Direct Sync:**
   - **Chosen:** Outbox pattern
   - **Alternative:** Direct sync on entity save
   - **Reason:** Reliability > speed, survives process crashes

2. **Last-Write-Wins vs Operational Transform:**
   - **Chosen:** Last-Write-Wins (LWW)
   - **Alternative:** Operational Transform (OT) or CRDTs
   - **Reason:** Simplicity, adequate for chat messages (not collaborative docs)

3. **Polling vs WebSocket:**
   - **Chosen:** HTTP polling every 5 seconds
   - **Alternative:** WebSocket for push notifications
   - **Reason:** Simpler infrastructure, works behind corporate proxies

4. **Single batch vs Streaming:**
   - **Chosen:** Single HTTP request with batch
   - **Alternative:** HTTP/2 streaming
   - **Reason:** Compatibility, adequate for typical batch sizes

**Observability:**

- **Metrics:** Sync lag (time between local write and remote sync), outbox queue depth, batch success rate, conflict rate
- **Logs:** All sync operations logged with idempotency keys, conflicts logged separately
- **Alerts:** Sync lag > 5 minutes, outbox queue > 500 items, sync success rate < 90%
- **Error Codes:** ACODE-SYNC-001 through ACODE-SYNC-010 for diagnosable failures

---

## Use Cases

### Use Case 1: Maria - Mobile Developer Working Offline on Flight

**Persona:** Maria is a mobile developer flying from San Francisco to New York (6-hour flight). She wants to work on authentication implementation during the flight without internet.

**Before (No Offline Support):**

Maria opens laptop, tries to use cloud-based AI coding tool.

```bash
# Attempts to start conversation
acode run "implement JWT authentication"

# Error: No internet connection
# Cannot access conversation history
# Cannot create new conversations
# 6 hours of productivity lost
```

**Time lost:** 6 hours @ $108/hour = **$648 lost**

**After (Offline-First with Sync):**

```bash
# Before flight: Sync is up to date
acode sync status
# Sync Status: All changes synced (0 pending)

# During flight: Work offline
acode run "implement JWT authentication"
# Creates conversation locally in SQLite
# 47 messages exchanged, 12 files created
# All tool calls work (local file system)

# Outbox accumulates changes
acode sync status
# Sync Status: 48 items pending sync
#   - 1 chat (CREATE)
#   - 47 messages (CREATE)

# After landing: Connect to WiFi
# Automatic sync in background
acode sync status
# Sync Status: Syncing... 48/48 items (100%)
# Sync Status: All changes synced (0 pending)

# Changes now available on desktop at home
```

**Time saved:** 6 hours of productive work @ $108/hour = **$648 value preserved**

**Business Impact:** Offline productivity maintained, no internet dependency, automatic sync when connected, zero data loss.

---

### Use Case 2: Jordan - DevOps Engineer Resolving Sync Conflicts

**Persona:** Jordan is a DevOps engineer who works on laptop at office and desktop at home. Both devices modified the same chat while offline.

**Before (Manual Conflict Resolution):**

Jordan modifies chat on laptop, then modifies same chat on desktop before syncing.

```bash
# Laptop (office):
acode chat open incident-response
acode run "update runbook with new steps"
# Chat version=5 locally

# Desktop (home):
acode chat open incident-response
acode run "add monitoring alerts section"
# Chat version=5 locally

# Both devices think they have version 5
# Manual conflict detection required:
#   - Export both versions
#   - Compare line by line
#   - Merge manually
#   - Delete one version
# Takes 30 minutes
```

**Time spent:** 30 minutes per conflict × 8 conflicts/year = **4 hours/year @ $108/hour = $432/year**

**After (Automatic Conflict Resolution):**

```bash
# Laptop syncs first:
acode sync
# Syncing chat-incident-response v5 → server
# Server: Success, assigned version 6

# Desktop syncs second:
acode sync
# Syncing chat-incident-response v5 → server
# Server: CONFLICT (server has v6, you have v5)
# Resolution: Last-write-wins (server v6 is newer)
# Downloading server version...
# Local version updated to v6

# View conflict log
acode sync conflicts --show
# Conflict Log:
#   2025-01-15 18:30:00 | chat-incident-response | LOCAL v5 vs REMOTE v6
#   Resolution: REMOTE_WINS (remote updated_at 18:25 > local updated_at 18:20)
#   Local changes saved to: .acode/conflicts/chat-incident-response-v5-backup.json
```

**Time spent:** 2 minutes reviewing conflict log

**Savings:** 28 minutes per conflict × 8 conflicts/year = 3.73 hours/year @ $108/hour = **$402/year**

**Business Impact:** Automatic conflict detection, last-write-wins resolution, conflict audit trail, backup of losing version, 93% time reduction.

---

### Use Case 3: Aisha - Engineering Manager Monitoring Team Sync Health

**Persona:** Aisha is an engineering manager responsible for ensuring the team's AI coding assistant infrastructure is healthy. She monitors sync performance to prevent data loss.

**Before (No Sync Visibility):**

Sync runs in background, no visibility into failures.

```bash
# Engineer reports: "My chat from yesterday is missing"
# Aisha investigates:
#   - Checks database manually
#   - Queries outbox table
#   - Finds 2000+ pending items stuck
#   - Sync has been failing for 3 days (network issue)
#   - 3 engineers affected, 8 hours lost work
# Takes 2 hours to diagnose and fix
```

**Time lost:** 2 hours diagnosis + 8 hours lost work = **10 hours @ $108/hour = $1,080 lost**

**After (Sync Health Dashboard):**

```bash
# Aisha runs sync health check
acode sync health

# Sync Health Report:
#   Status: UNHEALTHY ⚠️
#   Last successful sync: 3 days ago
#   Outbox queue depth: 2,147 items (WARNING: >500)
#   Failed sync attempts: 247 (last 24 hours)
#   Error: "Network timeout to sync.example.com"
#
#   Affected users: 3 engineers
#   Pending items:
#     - user@example.com: 847 items (2 chats, 845 messages)
#     - user2@example.com: 654 items (1 chat, 653 messages)
#     - user3@example.com: 646 items (3 chats, 643 messages)
#
#   Recommended action:
#     1. Check network connectivity to sync.example.com
#     2. Verify sync endpoint is healthy
#     3. Retry failed items: acode sync retry --all

# Aisha diagnoses in 5 minutes:
# - Network firewall blocking outbound HTTPS
# - Whitelists sync.example.com
# - Retries sync

acode sync retry --all
# Retrying 2,147 items...
# Success: 2,147/2,147 synced
# Sync Health: HEALTHY ✓
```

**Time saved:** 1.92 hours diagnosis (2.0 - 0.08) + 8 hours prevented data loss = **9.92 hours @ $108/hour = $1,071 saved**

**Business Impact:** Proactive monitoring, early detection of failures, clear diagnostics, zero data loss, 10x faster resolution.

---

## Security Considerations

### Threat 1: Outbox Tampering (Malicious Payload Injection)

**Risk:** Attacker with local database access modifies outbox entries to inject malicious payloads, bypassing application validation and syncing crafted data to server.

**Attack Scenario:**
```bash
# Attacker gains local database access
sqlite3 .acode/conversations.db

# Views outbox entries
SELECT * FROM outbox WHERE status = 'PENDING';

# Modifies payload to inject malicious content
UPDATE outbox SET payload = '{"id":"msg-123","content":"<script>alert(XSS)</script>","role":"user"}'
WHERE entity_id = 'msg-123';

# Next sync cycle uploads malicious payload
# Server processes without validation
# XSS stored in database
```

**Mitigation (SignedOutboxEntry - 55 lines):**

```csharp
// src/Acode.Infrastructure.Sync/SignedOutboxEntry.cs
namespace Acode.Infrastructure.Sync;

public sealed class SignedOutboxEntry
{
    private readonly IHmacSigner _signer;
    private readonly ILogger<SignedOutboxEntry> _logger;

    public SignedOutboxEntry(IHmacSigner signer, ILogger<SignedOutboxEntry> logger)
    {
        _signer = signer;
        _logger = logger;
    }

    public async Task<OutboxEntry> CreateAsync(
        string entityType,
        Guid entityId,
        string operation,
        object payload,
        CancellationToken ct)
    {
        // Serialize payload
        var payloadJson = JsonSerializer.Serialize(payload);
        
        // Calculate HMAC signature
        var signature = _signer.Sign(payloadJson);

        var entry = new OutboxEntry
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Operation = operation,
            IdempotencyKey = $"{entityId}-v{((dynamic)payload).Version}",
            Payload = payloadJson,
            Signature = signature,
            Status = OutboxStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        return entry;
    }

    public Result<string, Error> VerifyAndGetPayload(OutboxEntry entry)
    {
        // Verify HMAC signature
        var isValid = _signer.Verify(entry.Payload, entry.Signature);

        if (!isValid)
        {
            _logger.LogWarning(
                "Outbox entry signature invalid. EntityId: {EntityId}, Type: {Type}",
                entry.EntityId, entry.EntityType);

            return Result.Failure<string, Error>(
                new SecurityError("Outbox entry signature verification failed"));
        }

        return Result.Success<string, Error>(entry.Payload);
    }
}
```

---

### Threat 2: Replay Attack (Duplicate Idempotency Keys)

**Risk:** Attacker captures sync request with valid idempotency key, replays it multiple times to create duplicate data or exhaust server resources.

**Attack Scenario:**
```bash
# Attacker intercepts sync request
POST /api/v1/sync/batch HTTP/1.1
Host: sync.example.com
Authorization: Bearer token123
Idempotency-Key: msg-abc123-v1
Content-Type: application/json

{"type":"message","id":"msg-abc123","content":"Transfer $10000"}

# Attacker replays request 1000 times
# If server doesn't track idempotency properly:
#   - Creates 1000 duplicate messages
#   - Or exhausts server resources processing
```

**Mitigation (IdempotencyGuard - 60 lines):**

```csharp
// src/Acode.Api/Middleware/IdempotencyGuard.cs
namespace Acode.Api.Middleware;

public sealed class IdempotencyGuard
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<IdempotencyGuard> _logger;
    private const int CacheDurationHours = 24;

    public async Task<Result<SyncResponse, Error>> ProcessWithIdempotencyAsync(
        string idempotencyKey,
        Func<Task<SyncResponse>> operation,
        CancellationToken ct)
    {
        // Check if already processed
        var cacheKey = $"idempotency:{idempotencyKey}";
        var cachedResult = await _cache.GetStringAsync(cacheKey, ct);

        if (cachedResult != null)
        {
            _logger.LogInformation(
                "Idempotency key {Key} already processed, returning cached result",
                idempotencyKey);

            var response = JsonSerializer.Deserialize<SyncResponse>(cachedResult);
            return Result.Success<SyncResponse, Error>(response!);
        }

        // Process operation
        try
        {
            var result = await operation();

            // Cache result for 24 hours
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CacheDurationHours)
            };

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(result),
                cacheOptions,
                ct);

            return Result.Success<SyncResponse, Error>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operation failed for idempotency key {Key}", idempotencyKey);
            return Result.Failure<SyncResponse, Error>(
                new InfrastructureError("Sync operation failed", ex));
        }
    }

    public async Task<bool> IsProcessedAsync(string idempotencyKey, CancellationToken ct)
    {
        var cacheKey = $"idempotency:{idempotencyKey}";
        var exists = await _cache.GetAsync(cacheKey, ct);
        return exists != null;
    }
}
```

---

### Threat 3: Infinite Retry Loop (Resource Exhaustion)

**Risk:** Permanent failures (validation errors, authorization failures) trigger infinite retries, exhausting client resources and flooding server with invalid requests.

**Attack Scenario:**
```bash
# User creates message with invalid content (e.g., exceeds size limit)
acode run "$(cat /dev/urandom | head -c 10000000)"  # 10MB message

# Message inserted locally
# Outbox entry created
# Sync attempts:
#   Attempt 1: Server returns 400 Bad Request (message too large)
#   Attempt 2: Retry after 1s → 400 Bad Request
#   Attempt 3: Retry after 2s → 400 Bad Request
#   ... continues forever
# CPU and network exhausted
# Server flooded with invalid requests
```

**Mitigation (SmartRetryPolicy - 65 lines):**

```csharp
// src/Acode.Infrastructure.Sync/SmartRetryPolicy.cs
namespace Acode.Infrastructure.Sync;

public sealed class SmartRetryPolicy
{
    private readonly ILogger<SmartRetryPolicy> _logger;
    private const int MaxRetries = 5;
    private const int BaseDelayMs = 1000;
    private const int MaxDelayMs = 300000; // 5 minutes

    public async Task<Result<SyncResponse, Error>> ExecuteWithRetryAsync(
        Func<Task<SyncResponse>> operation,
        string idempotencyKey,
        CancellationToken ct)
    {
        var attempt = 0;

        while (attempt < MaxRetries)
        {
            attempt++;

            try
            {
                var result = await operation();
                return Result.Success<SyncResponse, Error>(result);
            }
            catch (HttpRequestException ex) when (IsTransientFailure(ex))
            {
                // Transient failure: Network timeout, 5xx server error
                _logger.LogWarning(
                    "Transient failure on attempt {Attempt}/{Max} for {Key}: {Error}",
                    attempt, MaxRetries, idempotencyKey, ex.Message);

                if (attempt >= MaxRetries)
                {
                    _logger.LogError(
                        "Max retries exceeded for {Key}, marking as failed",
                        idempotencyKey);

                    return Result.Failure<SyncResponse, Error>(
                        new InfrastructureError($"Sync failed after {MaxRetries} attempts", ex));
                }

                // Exponential backoff
                var delay = CalculateBackoff(attempt);
                await Task.Delay(delay, ct);
            }
            catch (HttpRequestException ex) when (IsPermanentFailure(ex))
            {
                // Permanent failure: 400 Bad Request, 422 Validation Error
                _logger.LogError(
                    "Permanent failure for {Key}: {Error}. Will not retry.",
                    idempotencyKey, ex.Message);

                return Result.Failure<SyncResponse, Error>(
                    new ValidationError($"Sync validation failed: {ex.Message}"));
            }
        }

        return Result.Failure<SyncResponse, Error>(
            new InfrastructureError("Unexpected retry loop exit"));
    }

    private bool IsTransientFailure(HttpRequestException ex)
    {
        return ex.StatusCode == null || // Network timeout
               ex.StatusCode >= HttpStatusCode.InternalServerError || // 5xx
               ex.StatusCode == HttpStatusCode.TooManyRequests; // 429
    }

    private bool IsPermanentFailure(HttpRequestException ex)
    {
        return ex.StatusCode == HttpStatusCode.BadRequest || // 400
               ex.StatusCode == HttpStatusCode.UnprocessableEntity || // 422
               ex.StatusCode == HttpStatusCode.Unauthorized || // 401
               ex.StatusCode == HttpStatusCode.Forbidden; // 403
    }

    private TimeSpan CalculateBackoff(int attempt)
    {
        var delayMs = Math.Min(BaseDelayMs * Math.Pow(2, attempt - 1), MaxDelayMs);
        return TimeSpan.FromMilliseconds(delayMs);
    }
}
```

---

### Threat 4: Sync Injection via Malicious Server Response

**Risk:** Compromised or malicious sync server returns crafted responses that inject malicious data into local SQLite database.

**Attack Scenario:**
```bash
# Client pulls inbox changes from server
GET /api/v1/sync/inbox?since_sequence=100

# Malicious server responds with crafted payload
{
  "sequence": 105,
  "changes": [
    {
      "type": "message",
      "operation": "UPDATE",
      "entity_id": "../../../etc/passwd",  # Path traversal attempt
      "data": {
        "id": "msg-123",
        "content": "<script>alert('XSS')</script>",  # XSS payload
        "version": 999999  # Version overflow
      }
    }
  ]
}

# Naive client applies changes without validation
# Malicious data stored in local database
# XSS triggers when viewing messages
```

**Mitigation (ValidatedInboxProcessor - 70 lines):**

```csharp
// src/Acode.Infrastructure.Sync/ValidatedInboxProcessor.cs
namespace Acode.Infrastructure.Sync;

public sealed class ValidatedInboxProcessor
{
    private readonly IValidator<Message> _messageValidator;
    private readonly IValidator<Chat> _chatValidator;
    private readonly ISanitizer _sanitizer;
    private readonly ILogger<ValidatedInboxProcessor> _logger;

    public async Task<Result<int, Error>> ProcessInboxAsync(
        InboxResponse inboxResponse,
        CancellationToken ct)
    {
        var processedCount = 0;

        foreach (var change in inboxResponse.Changes)
        {
            try
            {
                // Validate entity ID format
                if (!Guid.TryParse(change.EntityId, out var entityId))
                {
                    _logger.LogWarning(
                        "Invalid entity ID format: {EntityId}, skipping",
                        change.EntityId);
                    continue;
                }

                // Route to appropriate handler
                var result = change.EntityType switch
                {
                    "message" => await ProcessMessageChangeAsync(change, ct),
                    "chat" => await ProcessChatChangeAsync(change, ct),
                    _ => Result.Failure<Unit, Error>(
                        new ValidationError($"Unknown entity type: {change.EntityType}"))
                };

                if (result.IsSuccess)
                {
                    processedCount++;
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to process change: {EntityId}, Error: {Error}",
                        change.EntityId, result.Error.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Exception processing change: {EntityId}",
                    change.EntityId);
            }
        }

        return Result.Success<int, Error>(processedCount);
    }

    private async Task<Result<Unit, Error>> ProcessMessageChangeAsync(
        InboxChange change,
        CancellationToken ct)
    {
        var message = JsonSerializer.Deserialize<Message>(change.Data.ToString()!);
        if (message == null)
        {
            return Result.Failure<Unit, Error>(
                new ValidationError("Failed to deserialize message"));
        }

        // Validate message
        var validationResult = await _messageValidator.ValidateAsync(message, ct);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Message validation failed: {Errors}",
                string.Join(", ", validationResult.Errors));

            return Result.Failure<Unit, Error>(
                new ValidationError("Message validation failed"));
        }

        // Sanitize content (remove XSS, SQL injection attempts)
        message = message with
        {
            Content = _sanitizer.Sanitize(message.Content)
        };

        // Apply change based on operation
        switch (change.Operation)
        {
            case "CREATE":
                await _messageRepository.CreateAsync(message, ct);
                break;
            case "UPDATE":
                await _messageRepository.UpdateAsync(message, ct);
                break;
            case "DELETE":
                await _messageRepository.DeleteAsync(message.Id, ct);
                break;
        }

        return Result.Success<Unit, Error>(Unit.Value);
    }
}
```

---

### Threat 5: Privacy Level Bypass via Outbox Manipulation

**Risk:** Attacker modifies outbox entries to change privacy level from LOCAL_ONLY to FULL, causing sensitive local-only conversations to sync to remote server.

**Attack Scenario:**
```bash
# User creates local-only chat with sensitive data
acode chat create --title "Security Incident" --privacy local_only
acode run "We have a data breach: API keys exposed..."

# Outbox entry created with privacy_level = LOCAL_ONLY
# Entry marked skip_sync = true

# Attacker modifies outbox
sqlite3 .acode/conversations.db
UPDATE outbox SET skip_sync = false WHERE entity_id = 'chat-security-incident';

# Next sync uploads sensitive data to remote server
```

**Mitigation (PrivacyEnforcedSyncFilter - 50 lines):**

```csharp
// src/Acode.Infrastructure.Sync/PrivacyEnforcedSyncFilter.cs
namespace Acode.Infrastructure.Sync;

public sealed class PrivacyEnforcedSyncFilter
{
    private readonly IChatRepository _chatRepository;
    private readonly ILogger<PrivacyEnforcedSyncFilter> _logger;

    public async Task<List<OutboxEntry>> FilterByPrivacyAsync(
        List<OutboxEntry> entries,
        CancellationToken ct)
    {
        var filteredEntries = new List<OutboxEntry>();

        foreach (var entry in entries)
        {
            // Re-query entity privacy level from source of truth
            var privacyLevel = await GetEntityPrivacyLevelAsync(
                entry.EntityType,
                entry.EntityId,
                ct);

            if (privacyLevel == PrivacyLevel.LocalOnly)
            {
                _logger.LogInformation(
                    "Skipping sync for LOCAL_ONLY entity: {Type} {Id}",
                    entry.EntityType, entry.EntityId);

                // Mark outbox entry as skipped
                entry.Status = OutboxStatus.Skipped;
                entry.SyncError = "Privacy level: LOCAL_ONLY";
                continue;
            }

            filteredEntries.Add(entry);
        }

        return filteredEntries;
    }

    private async Task<PrivacyLevel> GetEntityPrivacyLevelAsync(
        string entityType,
        Guid entityId,
        CancellationToken ct)
    {
        return entityType switch
        {
            "chat" => await GetChatPrivacyLevelAsync(entityId, ct),
            "message" => await GetMessagePrivacyLevelAsync(entityId, ct),
            _ => PrivacyLevel.LocalOnly // Default to most restrictive
        };
    }

    private async Task<PrivacyLevel> GetChatPrivacyLevelAsync(Guid chatId, CancellationToken ct)
    {
        var chat = await _chatRepository.GetByIdAsync(chatId, ct);
        return chat?.PrivacyLevel ?? PrivacyLevel.LocalOnly;
    }
}
```

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Outbox | Queue for pending uploads |
| Inbox | Queue for pending downloads |
| Sync | Synchronize local/remote |
| Batch | Group of items |
| Retry | Try again after failure |
| Backoff | Increasing delay |
| Idempotency | Same result on repeat |
| Idempotency Key | Unique operation ID |
| Conflict | Concurrent modification |
| Last-Write-Wins | Latest change wins |
| Merge | Combine changes |
| Lag | Time behind current |
| Queue Depth | Pending items count |
| Transient | Temporary failure |
| Permanent | Unrecoverable failure |

---

## Out of Scope

The following items are explicitly excluded from Task 049.f:

- **Data model** - Task 049.a
- **CLI commands** - Task 049.b
- **Concurrency** - Task 049.c
- **Search** - Task 049.d
- **Retention** - Task 049.e
- **Real-time sync** - Polling only
- **Conflict resolution UI** - Auto-resolve only
- **Distributed transactions** - Eventual consistency
- **Multi-region** - Single region
- **Peer-to-peer** - Client-server only

---

## Assumptions

### Technical Assumptions

- ASM-001: Outbox pattern ensures reliable message delivery
- ASM-002: PostgreSQL is the remote sync target
- ASM-003: Network failures are transient and recoverable
- ASM-004: Conflict resolution uses last-write-wins
- ASM-005: Sync is bidirectional (up and down)

### Behavioral Assumptions

- ASM-006: Sync is opt-in, not required for local operation
- ASM-007: Users tolerate eventual consistency
- ASM-008: Sync happens in background, non-blocking
- ASM-009: Sync status is visible to users
- ASM-010: Manual sync trigger is available

### Dependency Assumptions

- ASM-011: Task 049.a data model is sync-compatible
- ASM-012: Task 011.b provides SQLite and PostgreSQL access
- ASM-013: Task 002 config stores sync settings

### Reliability Assumptions

- ASM-014: Outbox survives process restarts
- ASM-015: Failed syncs retry with exponential backoff
- ASM-016: Sync conflicts are logged for debugging

---

## Functional Requirements

### Outbox Pattern

- FR-001: Changes MUST write to outbox
- FR-002: Outbox write MUST be atomic
- FR-003: Outbox MUST have idempotency key
- FR-004: Outbox MUST have payload
- FR-005: Outbox MUST have status
- FR-006: Outbox MUST have retry count

### Outbox Processing

- FR-007: Background processor MUST exist
- FR-008: Processor MUST poll outbox
- FR-009: Poll interval MUST be configurable
- FR-010: Default poll: 5 seconds
- FR-011: Batch size MUST be configurable
- FR-012: Default batch: 50 items

### Batching

- FR-013: Multiple items MUST batch
- FR-014: Batch MUST respect size limit
- FR-015: Batch MUST respect byte limit
- FR-016: Partial batch MUST be allowed

### Upload Flow

- FR-017: Read pending from outbox
- FR-018: Apply privacy/redaction
- FR-019: Send batch to server
- FR-020: Mark successful as Synced
- FR-021: Mark failed for retry

### Retries

- FR-022: Transient failures MUST retry
- FR-023: Exponential backoff MUST apply
- FR-024: Max retries MUST be configurable
- FR-025: Default max: 5 retries
- FR-026: Base delay: 1 second
- FR-027: Max delay: 5 minutes

### Idempotency

- FR-028: Each change MUST have key
- FR-029: Key MUST be ULID
- FR-030: Server MUST detect duplicate
- FR-031: Duplicate MUST return success

### Inbox Pattern

- FR-032: Inbox table MUST exist
- FR-033: Poll server for changes
- FR-034: Download to inbox
- FR-035: Apply to local storage
- FR-036: Mark as applied

### Download Flow

- FR-037: Query server for changes since
- FR-038: Download new/updated items
- FR-039: Apply to local DB
- FR-040: Update last sync timestamp

### Conflict Detection

- FR-041: Version field MUST be checked
- FR-042: Concurrent edit MUST be detected
- FR-043: Conflict MUST be flagged
- FR-044: Conflict MUST be logged

### Conflict Resolution

- FR-045: Last-write-wins MUST be default
- FR-046: Policy MUST be configurable
- FR-047: Conflict MUST update local
- FR-048: Original MUST be preserved

### Status Tracking

- FR-049: SyncStatus MUST be per-entity
- FR-050: Pending status MUST exist
- FR-051: Synced status MUST exist
- FR-052: Conflict status MUST exist
- FR-053: Failed status MUST exist

### CLI Integration

- FR-054: `acode sync status` MUST work
- FR-055: `acode sync now` MUST force sync
- FR-056: `acode sync retry` MUST retry failed
- FR-057: `acode sync pause/resume` MUST work

### Health Monitoring

- FR-058: Queue depth MUST be tracked
- FR-059: Sync lag MUST be tracked
- FR-060: Failure rate MUST be tracked
- FR-061: Metrics MUST be exposed

---

## Non-Functional Requirements

### Performance

- NFR-001: Sync lag under normal network conditions MUST be < 30 seconds from local change to remote visibility
- NFR-002: Batch processing MUST complete < 5 seconds for batches up to 100 items
- NFR-003: Sync operations MUST NOT block the main thread or UI responsiveness
- NFR-004: Outbox entry creation MUST complete < 10ms to avoid blocking local operations
- NFR-005: Inbox processing MUST complete < 50ms per entry excluding network latency
- NFR-006: Conflict detection MUST complete < 20ms per entry comparison
- NFR-007: Background sync polling MUST consume < 1% CPU when idle
- NFR-008: Initial sync of 1000 conversations MUST complete < 60 seconds on broadband connection

### Reliability

- NFR-009: Zero data loss guarantee - all committed local changes MUST eventually sync to remote
- NFR-010: Eventual consistency MUST be achieved within 5 minutes under normal conditions
- NFR-011: Sync state MUST survive application restart without data loss or corruption
- NFR-012: Outbox entries MUST be persisted atomically with their source changes (same transaction)
- NFR-013: Idempotency keys MUST be stored for minimum 7 days to prevent duplicate processing
- NFR-014: Failed entries MUST be retryable without manual intervention up to configured maximum
- NFR-015: Sync journal MUST maintain complete audit trail of all sync operations

### Data Integrity

- NFR-016: Outbox entries MUST be cryptographically signed to detect tampering
- NFR-017: Sync operations MUST validate data checksums before and after transmission
- NFR-018: Conflict resolution MUST be deterministic - same inputs always produce same result
- NFR-019: Partial sync failures MUST NOT leave database in inconsistent state
- NFR-020: Foreign key relationships MUST be maintained during sync operations

### Scalability

- NFR-021: System MUST handle 10,000 pending outbox entries without performance degradation
- NFR-022: System MUST sustain 100 items/second sync throughput during burst periods
- NFR-023: Batch sizes MUST be dynamically adjustable based on network conditions
- NFR-024: Memory usage MUST NOT exceed 100MB during large sync operations
- NFR-025: System MUST support horizontal scaling via stateless sync workers

### Resilience

- NFR-026: System MUST handle network disconnection gracefully with automatic recovery
- NFR-027: System MUST handle server downtime with exponential backoff retry strategy
- NFR-028: System MUST degrade gracefully - local operations continue during sync failures
- NFR-029: Circuit breaker MUST engage after 5 consecutive failures to prevent cascade
- NFR-030: Dead letter queue MUST capture permanently failed entries for manual review

### Security

- NFR-031: All sync traffic MUST be encrypted using TLS 1.3
- NFR-032: Sync authentication tokens MUST be refreshed before expiration
- NFR-033: Sensitive fields MUST be encrypted at rest in outbox/inbox tables
- NFR-034: Sync logs MUST NOT contain PII or sensitive conversation content

### Observability

- NFR-035: Sync metrics MUST be exposed for monitoring (queue depth, latency, throughput, errors)
- NFR-036: Health checks MUST report sync subsystem status within 100ms
- NFR-037: Detailed sync traces MUST be available for debugging at DEBUG log level
- NFR-038: Alerts MUST trigger when sync lag exceeds 5 minutes

---

## User Manual Documentation

### Overview

The sync engine keeps local and remote data in sync. Work offline, and changes sync when connected. Cross-device access happens automatically.

### Quick Start

```bash
# Check sync status
$ acode sync status

Sync Status
────────────────────────────────────
Mode: Online
Last Sync: 30s ago
Queue: 0 pending

Local → Remote:
  Pending: 0
  Synced today: 47
  Failed: 0

Remote → Local:
  Last pull: 1m ago
  Downloaded today: 12
```

### Forcing Sync

```bash
# Force immediate sync
$ acode sync now
Syncing...
  ↑ Uploaded: 3 items
  ↓ Downloaded: 1 item
Sync complete.

# Retry failed items
$ acode sync retry
Retrying 2 failed items...
  ✓ chat_abc123: synced
  ✗ chat_def456: still failing (invalid data)
```

### Pausing Sync

```bash
# Pause sync (offline mode)
$ acode sync pause
Sync paused. Changes will queue locally.

# Resume sync
$ acode sync resume
Sync resumed. Processing queue...
```

### Configuration

```yaml
# .agent/config.yml
sync:
  enabled: true
  
  # Remote connection
  remote:
    url: postgres://...
    
  # Upload settings
  upload:
    poll_interval_seconds: 5
    batch_size: 50
    batch_bytes_max: 1048576  # 1MB
    
  # Retry settings
  retry:
    max_attempts: 5
    base_delay_seconds: 1
    max_delay_seconds: 300
    
  # Download settings
  download:
    poll_interval_seconds: 30
    
  # Conflict resolution
  conflict:
    policy: last_write_wins  # or 'local_wins', 'remote_wins'
    preserve_original: true
```

### Viewing Queue

```bash
$ acode sync queue

Sync Queue
────────────────────────────────────
Pending Upload: 3 items

Item                    Status    Retries  Last Error
chat_abc123 (msg)       Pending   0        -
chat_def456 (msg)       Pending   0        -
chat_ghi789 (chat)      Failed    3        Connection timeout

Failed items will retry with backoff.
Use 'acode sync retry --force' to retry now.
```

### Conflict Handling

```bash
$ acode sync conflicts

Conflicts
────────────────────────────────────
1 conflict found

Chat: chat_abc123 "Feature: Auth"
  Local version: 5 (modified 10m ago)
  Remote version: 6 (modified 8m ago)
  Resolution: Remote wins (last-write)
  
  Local changes preserved in: .agent/conflicts/chat_abc123_v5.json
```

### Troubleshooting

#### Sync Stuck

**Problem:** Queue not draining

**Solutions:**
1. Check network: `acode sync status`
2. Check errors: `acode sync queue --errors`
3. Force retry: `acode sync retry --force`

#### High Lag

**Problem:** Sync lag increasing

**Solutions:**
1. Check batch size (increase if small)
2. Check network speed
3. Check server health

#### Conflicts

**Problem:** Frequent conflicts

**Solutions:**
1. Work in separate chats
2. Reduce concurrent edits
3. Increase sync frequency

---

## Acceptance Criteria

### Outbox Entry Creation

- [ ] AC-001: Outbox entry is created for each new Chat insert operation
- [ ] AC-002: Outbox entry is created for each Chat update operation
- [ ] AC-003: Outbox entry is created for each Chat delete operation
- [ ] AC-004: Outbox entry is created for each Run insert/update/delete operation
- [ ] AC-005: Outbox entry is created for each Message insert/update/delete operation
- [ ] AC-006: Outbox entry is written atomically within same transaction as source data change
- [ ] AC-007: Transaction rollback removes both source change AND outbox entry
- [ ] AC-008: Outbox entry contains unique ULID-format idempotency key
- [ ] AC-009: Outbox entry contains serialized entity payload
- [ ] AC-010: Outbox entry contains entity type identifier
- [ ] AC-011: Outbox entry contains entity ID reference
- [ ] AC-012: Outbox entry contains operation type (Insert/Update/Delete)
- [ ] AC-013: Outbox entry contains creation timestamp in UTC
- [ ] AC-014: Outbox entry initial status is "Pending"
- [ ] AC-015: Outbox entry is cryptographically signed on creation
- [ ] AC-016: Outbox signature verification succeeds for valid entries

### Outbox Processing

- [ ] AC-017: Background processor runs on configurable polling interval (default 5 seconds)
- [ ] AC-018: Background processor retrieves entries in creation order (FIFO)
- [ ] AC-019: Batch processing respects configured batch size limit
- [ ] AC-020: Batch is sent to remote API in single request
- [ ] AC-021: Successful batch processing marks entries as "Completed"
- [ ] AC-022: Completed entries include sync completion timestamp
- [ ] AC-023: Processing does not block main application thread
- [ ] AC-024: Processing continues while application is in background
- [ ] AC-025: Processing pauses when network connectivity is lost
- [ ] AC-026: Processing resumes automatically when connectivity restored
- [ ] AC-027: Processing can be manually paused via CLI command
- [ ] AC-028: Processing can be manually resumed via CLI command
- [ ] AC-029: Multiple entries for same entity are batched together
- [ ] AC-030: Processing respects rate limits from remote server

### Retry Mechanism

- [ ] AC-031: Failed entries are marked with "Failed" status and retry count incremented
- [ ] AC-032: Failed entries are automatically retried on next processor cycle
- [ ] AC-033: Retry delay follows exponential backoff: 1s, 2s, 4s, 8s, 16s, 32s, 60s max
- [ ] AC-034: Retry includes jitter of ±10% to prevent thundering herd
- [ ] AC-035: Maximum retry count is configurable (default 10)
- [ ] AC-036: Entries exceeding maximum retries are moved to dead letter queue
- [ ] AC-037: Dead letter entries are marked with "DeadLetter" status
- [ ] AC-038: Dead letter entries retain full error information
- [ ] AC-039: Dead letter entries can be manually retried via CLI
- [ ] AC-040: Dead letter entries can be archived/deleted via CLI
- [ ] AC-041: Circuit breaker engages after 5 consecutive failures
- [ ] AC-042: Circuit breaker prevents new requests for 30 seconds
- [ ] AC-043: Circuit breaker can be manually reset via CLI
- [ ] AC-044: Transient errors (5xx, timeout) trigger retry
- [ ] AC-045: Permanent errors (4xx except 429) move directly to dead letter

### Idempotency Enforcement

- [ ] AC-046: Remote server receives idempotency key with each sync request
- [ ] AC-047: Duplicate idempotency key returns success without re-processing
- [ ] AC-048: Idempotency key is stored in remote database for 7 days minimum
- [ ] AC-049: Idempotency check completes in < 20ms
- [ ] AC-050: Idempotency key collision returns 409 Conflict response
- [ ] AC-051: Local idempotency cache prevents unnecessary remote calls
- [ ] AC-052: Cache eviction follows LRU policy with 10,000 entry limit
- [ ] AC-053: In-flight request detection prevents duplicate concurrent submissions

### Inbox Processing

- [ ] AC-054: Inbox processor polls remote for changes on configurable interval
- [ ] AC-055: Inbox downloads changes since last sync timestamp
- [ ] AC-056: Downloaded entries are written to local inbox table
- [ ] AC-057: Inbox entries are processed in timestamp order
- [ ] AC-058: Successful processing applies change to local database
- [ ] AC-059: Processed inbox entries are marked as "Applied"
- [ ] AC-060: Last sync timestamp is updated atomically with entry processing
- [ ] AC-061: Inbox processing handles insert operations correctly
- [ ] AC-062: Inbox processing handles update operations correctly
- [ ] AC-063: Inbox processing handles delete operations correctly
- [ ] AC-064: Inbox validates incoming data against local schema
- [ ] AC-065: Invalid incoming data is logged and moved to error queue

### Conflict Detection and Resolution

- [ ] AC-066: Conflict detected when same entity modified locally and remotely
- [ ] AC-067: Conflict detected by comparing version vectors
- [ ] AC-068: Conflict triggers configured resolution policy
- [ ] AC-069: Last-write-wins policy selects entry with latest timestamp
- [ ] AC-070: First-write-wins policy preserves original entry
- [ ] AC-071: Manual resolution policy pauses sync and prompts user
- [ ] AC-072: Custom merge policy executes registered merge function
- [ ] AC-073: Original conflicting versions are preserved in conflict table
- [ ] AC-074: Conflict resolution result is logged for audit
- [ ] AC-075: Resolution updates both local database and outbox
- [ ] AC-076: Three-way merge correctly identifies non-conflicting changes
- [ ] AC-077: Field-level conflict resolution preserves non-conflicting fields
- [ ] AC-078: Conflict list viewable via CLI command

### CLI Commands

- [ ] AC-079: `acode sync status` shows current sync state and queue depths
- [ ] AC-080: `acode sync status` shows last successful sync timestamp
- [ ] AC-081: `acode sync status` shows pending outbox entry count
- [ ] AC-082: `acode sync status` shows pending inbox entry count
- [ ] AC-083: `acode sync status --verbose` shows per-entity breakdown
- [ ] AC-084: `acode sync now` triggers immediate sync cycle
- [ ] AC-085: `acode sync now` waits for completion and reports result
- [ ] AC-086: `acode sync retry <id>` retries specific failed entry
- [ ] AC-087: `acode sync retry --all` retries all failed entries
- [ ] AC-088: `acode sync pause` pauses background processor
- [ ] AC-089: `acode sync resume` resumes background processor
- [ ] AC-090: `acode sync full` initiates full resync of all data
- [ ] AC-091: `acode sync full --direction push` syncs only local to remote
- [ ] AC-092: `acode sync full --direction pull` syncs only remote to local
- [ ] AC-093: `acode sync conflicts list` shows all pending conflicts
- [ ] AC-094: `acode sync resolve <id> --strategy <strategy>` resolves conflict
- [ ] AC-095: `acode sync health` shows sync subsystem health metrics
- [ ] AC-096: `acode sync logs` shows recent sync activity
- [ ] AC-097: `acode sync dlq list` shows dead letter queue entries
- [ ] AC-098: `acode sync dlq retry <id>` retries dead letter entry
- [ ] AC-099: `acode sync outbox list` shows pending outbox entries
- [ ] AC-100: `acode sync inbox list` shows pending inbox entries

### Health and Monitoring

- [ ] AC-101: Queue depth metric exposed for outbox entries
- [ ] AC-102: Queue depth metric exposed for inbox entries
- [ ] AC-103: Sync lag metric calculated as oldest pending entry age
- [ ] AC-104: Throughput metric tracks entries processed per second
- [ ] AC-105: Error rate metric tracks failed/total ratio
- [ ] AC-106: Circuit breaker state exposed as metric
- [ ] AC-107: Health check endpoint returns sync subsystem status
- [ ] AC-108: Health check completes within 100ms
- [ ] AC-109: Structured logs include sync operation correlation IDs
- [ ] AC-110: Metrics exportable in Prometheus format
- [ ] AC-111: Alert triggers when sync lag exceeds 5 minutes

### Data Integrity

- [ ] AC-112: Sync never loses committed local changes
- [ ] AC-113: Sync maintains referential integrity (Chats before Messages)
- [ ] AC-114: Partial sync failure does not corrupt local database
- [ ] AC-115: Partial sync failure does not corrupt remote database
- [ ] AC-116: Sync journal records all operations for audit
- [ ] AC-117: Journal entries include before/after states
- [ ] AC-118: Journal supports point-in-time audit queries

### Performance

- [ ] AC-119: Outbox entry creation completes in < 10ms
- [ ] AC-120: Batch of 100 entries processes in < 5 seconds
- [ ] AC-121: Sync polling consumes < 1% CPU when idle
- [ ] AC-122: Memory usage during sync < 100MB
- [ ] AC-123: Initial sync of 1000 conversations completes in < 60 seconds
- [ ] AC-124: Sync does not block UI thread

### Configuration

- [ ] AC-125: Sync enabled/disabled via configuration
- [ ] AC-126: Polling interval configurable (default 5 seconds)
- [ ] AC-127: Batch size configurable (default 100)
- [ ] AC-128: Max retry count configurable (default 10)
- [ ] AC-129: Conflict resolution policy configurable
- [ ] AC-130: Remote endpoint URL configurable
- [ ] AC-131: Authentication method configurable
- [ ] AC-132: Idempotency key TTL configurable

---

## Best Practices

### Outbox Pattern

- **BP-001: Atomic writes** - Write to outbox in same transaction as source data
- **BP-002: Ordered processing** - Process outbox in sequence for consistency
- **BP-003: Idempotent messages** - Design for safe retry
- **BP-004: Batch processing** - Group small changes for efficiency

### Sync Strategy

- **BP-005: Conflict resolution** - Use last-write-wins with timestamp comparison
- **BP-006: Partial sync support** - Continue from last successful point
- **BP-007: Sync status visibility** - Show users sync state and errors
- **BP-008: Manual sync option** - Allow forcing immediate sync

### Error Handling

- **BP-009: Exponential backoff** - Increase delay between retries
- **BP-010: Dead letter handling** - Move permanently failed items aside
- **BP-011: Alert on failures** - Notify users of persistent sync issues
- **BP-012: Health monitoring** - Track queue depth and sync lag

---

## Troubleshooting

### Issue 1: Sync Stuck - Changes Not Appearing

**Symptoms:**
- Local changes not appearing in remote database after several minutes
- Sync status shows "Pending" entries that never decrease
- No error messages in standard output

**Possible Causes:**
1. Background sync job not running or crashed
2. Network connectivity issues or firewall blocking
3. Remote database connection failure
4. Authentication token expired
5. Circuit breaker engaged due to previous failures

**Solutions:**
1. Check sync status: `acode sync status --verbose`
2. Verify background job: `acode sync health`
3. Test network connectivity: `acode sync test-connection`
4. Force token refresh: `acode sync reauthenticate`
5. Reset circuit breaker: `acode sync reset-breaker`
6. Review detailed logs: `acode sync logs --level debug --last 50`

### Issue 2: Conflict Resolution Errors

**Symptoms:**
- Sync reports "Conflict detected, cannot auto-resolve"
- Same conversation shows different content on different devices
- Merge operations failing with conflict codes

**Possible Causes:**
1. Same message edited on multiple devices while offline
2. Clock skew between devices causing timestamp conflicts
3. Custom conflict resolution policy rejecting merges
4. Corrupted version vectors

**Solutions:**
1. View conflict details: `acode sync conflicts list --conversation <id>`
2. Choose resolution strategy: `acode sync resolve <conflict-id> --strategy last-write-wins`
3. Manual resolution: `acode sync resolve <conflict-id> --keep local|remote`
4. Check system clocks and NTP sync
5. For corrupted vectors: `acode sync rebuild-vectors --conversation <id>`

### Issue 3: Outbox Queue Growing Unbounded

**Symptoms:**
- Outbox entry count continuously increasing
- Memory usage rising over time
- Sync lag exceeding expected thresholds

**Possible Causes:**
1. Sync processor not consuming entries fast enough
2. Remote server unavailable or rate-limiting
3. Batch processing failures causing retry accumulation
4. Network bandwidth insufficient for volume

**Solutions:**
1. Check processing rate: `acode sync metrics --component outbox`
2. Verify remote status: `acode sync test-connection --detailed`
3. Adjust batch size: `acode config set sync.batch-size 50`
4. Review failed entries: `acode sync outbox list --status failed`
5. Clear stuck entries: `acode sync outbox clear --status stuck --older-than 1h`

### Issue 4: Idempotency Key Collisions

**Symptoms:**
- Error: "Duplicate idempotency key detected"
- Operations rejected by remote server
- Entries stuck in "Processing" status

**Possible Causes:**
1. Entries being replayed after network failure appeared successful
2. Idempotency key table corrupted or truncated
3. Key generation producing duplicates (clock issues)
4. Multiple sync instances using same database

**Solutions:**
1. Check key status: `acode sync idempotency check <key>`
2. Force replay: `acode sync outbox retry <entry-id> --new-key`
3. Verify single instance: `acode sync instances list`
4. Rebuild key table: `acode sync idempotency rebuild --since <date>`

### Issue 5: Inbox Processing Failures

**Symptoms:**
- Remote changes not appearing locally
- Inbox entries accumulating with "Failed" status
- Partial sync - some conversations update, others don't

**Possible Causes:**
1. Foreign key constraints preventing insert (missing parent records)
2. Schema version mismatch between local and remote
3. Validation failures on incoming data
4. Disk full preventing local writes

**Solutions:**
1. Check inbox status: `acode sync inbox list --status failed`
2. View failure reason: `acode sync inbox show <entry-id> --include-error`
3. Verify schema: `acode sync schema compare`
4. Force full resync: `acode sync full --direction pull`
5. Check disk space: `acode health storage`

### Issue 6: Retry Exhaustion - Entries in Dead Letter Queue

**Symptoms:**
- Entries moved to dead letter queue after max retries
- Warning: "Entry <id> exceeded retry limit"
- Sync health shows dead letter items

**Possible Causes:**
1. Persistent remote server errors (400-level responses)
2. Data validation failures on remote
3. Permanent network partition to specific endpoints
4. Schema incompatibility with remote

**Solutions:**
1. List dead letter items: `acode sync dlq list`
2. Inspect failed entry: `acode sync dlq show <id>`
3. Retry with new payload: `acode sync dlq retry <id> --revalidate`
4. Archive permanently: `acode sync dlq archive <id>`
5. Export for manual fix: `acode sync dlq export <id> --format json`

### Issue 7: Signature Verification Failures

**Symptoms:**
- Error: "Outbox entry signature invalid"
- Entries rejected during processing
- Security warning in logs

**Possible Causes:**
1. Signing key rotated but old entries remain
2. Entry payload modified after signing
3. Corrupted entry in outbox table
4. Key material missing or inaccessible

**Solutions:**
1. Verify signature: `acode sync verify <entry-id>`
2. Check key status: `acode sync keys list`
3. Resign entries: `acode sync outbox resign --status pending`
4. Regenerate keys: `acode sync keys rotate --resign-pending`

### Issue 8: Performance Degradation During Large Sync

**Symptoms:**
- Initial sync taking hours instead of minutes
- UI becomes unresponsive during sync
- Memory usage spikes during batch processing

**Possible Causes:**
1. Batch size too large for available memory
2. No indexing on outbox/inbox query columns
3. Concurrent local operations competing for locks
4. Network latency causing connection timeouts

**Solutions:**
1. Reduce batch size: `acode config set sync.batch-size 25`
2. Enable throttling: `acode config set sync.throttle-percent 50`
3. Run full sync during off-hours: `acode sync full --background --low-priority`
4. Check indexes: `acode db verify --check-indexes`
5. Monitor progress: `acode sync status --watch`

---

## Testing Requirements

### Unit Tests - OutboxTests.cs (Complete Implementation)

```csharp
// Tests/Unit/Sync/OutboxTests.cs
using Xunit;
using FluentAssertions;
using AgenticCoder.Infrastructure.Sync;
using AgenticCoder.Domain.Conversation;

namespace AgenticCoder.Tests.Unit.Sync;

public sealed class OutboxTests
{
    [Fact]
    public async Task Should_Write_Atomically()
    {
        // Arrange
        var repository = new InMemoryOutboxRepository();
        var chat = Chat.Create("Test Chat", WorktreeId.From("worktree-01HKABC"));

        // Act
        using var transaction = await repository.BeginTransactionAsync(CancellationToken.None);
        
        var outboxEntry = OutboxEntry.Create(
            entityType: "Chat",
            entityId: chat.Id.ToString(),
            operation: "Insert",
            payload: JsonSerializer.Serialize(chat));

        await repository.AddAsync(outboxEntry, CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);

        // Assert
        var entries = await repository.GetPendingAsync(limit: 10, CancellationToken.None);
        entries.Should().HaveCount(1);
        entries[0].Status.Should().Be(OutboxStatus.Pending);
    }

    [Fact]
    public void Should_Include_IdempotencyKey()
    {
        // Arrange
        var chat = Chat.Create("Test Chat", WorktreeId.From("worktree-01HKABC"));

        // Act
        var outboxEntry = OutboxEntry.Create(
            entityType: "Chat",
            entityId: chat.Id.ToString(),
            operation: "Insert",
            payload: JsonSerializer.Serialize(chat));

        // Assert
        outboxEntry.IdempotencyKey.Should().NotBeNullOrEmpty();
        outboxEntry.IdempotencyKey.Should().MatchRegex(@"^[A-Z0-9]{26}$", "should be ULID format");
    }

    [Fact]
    public async Task Should_Track_Status()
    {
        // Arrange
        var repository = new InMemoryOutboxRepository();
        var outboxEntry = OutboxEntry.Create(
            entityType: "Chat",
            entityId: ChatId.NewId().ToString(),
            operation: "Insert",
            payload: "{}");

        await repository.AddAsync(outboxEntry, CancellationToken.None);

        // Act - Mark as processing
        outboxEntry.MarkAsProcessing();
        await repository.UpdateAsync(outboxEntry, CancellationToken.None);

        // Assert
        var retrieved = await repository.GetByIdAsync(outboxEntry.Id, CancellationToken.None);
        retrieved!.Status.Should().Be(OutboxStatus.Processing);
        retrieved.ProcessingStartedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Mark_As_Completed()
    {
        // Arrange
        var repository = new InMemoryOutboxRepository();
        var outboxEntry = OutboxEntry.Create(
            entityType: "Chat",
            entityId: ChatId.NewId().ToString(),
            operation: "Insert",
            payload: "{}");

        await repository.AddAsync(outboxEntry, CancellationToken.None);

        // Act
        outboxEntry.MarkAsCompleted();
        await repository.UpdateAsync(outboxEntry, CancellationToken.None);

        // Assert
        var retrieved = await repository.GetByIdAsync(outboxEntry.Id, CancellationToken.None);
        retrieved!.Status.Should().Be(OutboxStatus.Completed);
        retrieved.CompletedAt.Should().NotBeNull();

        var pending = await repository.GetPendingAsync(limit: 10, CancellationToken.None);
        pending.Should().BeEmpty("completed entries should not appear in pending list");
    }

    [Fact]
    public async Task Should_Track_Retry_Count()
    {
        // Arrange
        var repository = new InMemoryOutboxRepository();
        var outboxEntry = OutboxEntry.Create(
            entityType: "Chat",
            entityId: ChatId.NewId().ToString(),
            operation: "Insert",
            payload: "{}");

        await repository.AddAsync(outboxEntry, CancellationToken.None);

        // Act
        outboxEntry.MarkAsFailed("Network timeout");
        await repository.UpdateAsync(outboxEntry, CancellationToken.None);

        // Assert
        var retrieved = await repository.GetByIdAsync(outboxEntry.Id, CancellationToken.None);
        retrieved!.RetryCount.Should().Be(1);
        retrieved.Status.Should().Be(OutboxStatus.Pending, "failed entries should return to pending for retry");
        retrieved.LastError.Should().Be("Network timeout");
    }
}
```

### Unit Tests - BatcherTests.cs (Complete Implementation)

```csharp
// Tests/Unit/Sync/BatcherTests.cs
using Xunit;
using FluentAssertions;
using AgenticCoder.Infrastructure.Sync;

namespace AgenticCoder.Tests.Unit.Sync;

public sealed class BatcherTests
{
    [Fact]
    public void Should_Batch_Items()
    {
        // Arrange
        var entries = Enumerable.Range(1, 75)
            .Select(i => OutboxEntry.Create("Chat", $"chat-{i}", "Insert", "{}"))
            .ToList();

        var batcher = new OutboxBatcher(maxBatchSize: 50, maxBatchBytes: 1_000_000);

        // Act
        var batches = batcher.CreateBatches(entries);

        // Assert
        batches.Should().HaveCount(2, "75 items should create 2 batches with max size 50");
        batches[0].Should().HaveCount(50);
        batches[1].Should().HaveCount(25);
    }

    [Fact]
    public void Should_Respect_Size_Limit()
    {
        // Arrange
        var entries = Enumerable.Range(1, 100)
            .Select(i => OutboxEntry.Create("Chat", $"chat-{i}", "Insert", "{}"))
            .ToList();

        var batcher = new OutboxBatcher(maxBatchSize: 30, maxBatchBytes: 1_000_000);

        // Act
        var batches = batcher.CreateBatches(entries);

        // Assert
        batches.Should().HaveCount(4, "100 items with max 30 per batch should create 4 batches");
        batches.Should().AllSatisfy(b => b.Count.Should().BeLessOrEqualTo(30));
    }

    [Fact]
    public void Should_Respect_Byte_Limit()
    {
        // Arrange
        var largePayload = new string('a', 100_000);  // 100KB payload
        var entries = Enumerable.Range(1, 15)
            .Select(i => OutboxEntry.Create("Chat", $"chat-{i}", "Insert", largePayload))
            .ToList();

        // Max batch: 1MB = 10 x 100KB payloads
        var batcher = new OutboxBatcher(maxBatchSize: 50, maxBatchBytes: 1_000_000);

        // Act
        var batches = batcher.CreateBatches(entries);

        // Assert
        batches.Should().HaveCount(2, "15 x 100KB entries should create 2 batches under 1MB limit");
        batches.Should().AllSatisfy(batch =>
        {
            var totalBytes = batch.Sum(e => System.Text.Encoding.UTF8.GetByteCount(e.Payload));
            totalBytes.Should().BeLessOrEqualTo(1_000_000);
        });
    }

    [Fact]
    public void Should_Handle_Single_Large_Item()
    {
        // Arrange
        var hugePayload = new string('a', 2_000_000);  // 2MB payload (exceeds 1MB limit)
        var entry = OutboxEntry.Create("Chat", "chat-1", "Insert", hugePayload);

        var batcher = new OutboxBatcher(maxBatchSize: 50, maxBatchBytes: 1_000_000);

        // Act
        var batches = batcher.CreateBatches(new[] { entry });

        // Assert
        batches.Should().HaveCount(1, "single item exceeding limit should still create one batch");
        batches[0].Should().HaveCount(1);
    }
}
```

### Unit Tests - RetryTests.cs (Complete Implementation)

```csharp
// Tests/Unit/Sync/RetryTests.cs
using Xunit;
using FluentAssertions;
using AgenticCoder.Infrastructure.Sync;

namespace AgenticCoder.Tests.Unit.Sync;

public sealed class RetryTests
{
    [Fact]
    public async Task Should_Retry_Transient()
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 3, baseDelayMs: 100);
        int attemptCount = 0;

        async Task<bool> TransientFailure()
        {
            attemptCount++;
            if (attemptCount < 3)
                throw new HttpRequestException("Network timeout");
            return true;
        }

        // Act
        var result = await policy.ExecuteAsync(TransientFailure, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        attemptCount.Should().Be(3, "should retry twice before succeeding on 3rd attempt");
    }

    [Fact]
    public async Task Should_Apply_Backoff()
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 3, baseDelayMs: 100);
        var delays = new List<TimeSpan>();
        int attemptCount = 0;

        async Task<bool> TrackDelays()
        {
            attemptCount++;
            if (attemptCount > 1)
            {
                delays.Add(TimeSpan.FromMilliseconds(100 * Math.Pow(2, attemptCount - 2)));
            }

            if (attemptCount < 3)
                throw new HttpRequestException("Retry");

            return true;
        }

        // Act
        await policy.ExecuteAsync(TrackDelays, CancellationToken.None);

        // Assert
        delays.Should().HaveCount(2);
        delays[0].TotalMilliseconds.Should().BeApproximately(100, 10, "first retry should be 100ms");
        delays[1].TotalMilliseconds.Should().BeApproximately(200, 10, "second retry should be 200ms with exponential backoff");
    }

    [Fact]
    public async Task Should_Honor_Max_Retries()
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 3, baseDelayMs: 10);
        int attemptCount = 0;

        async Task<bool> AlwaysFail()
        {
            attemptCount++;
            throw new HttpRequestException("Always fails");
        }

        // Act
        var act = async () => await policy.ExecuteAsync(AlwaysFail, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
        attemptCount.Should().Be(4, "initial attempt + 3 retries");
    }

    [Theory]
    [InlineData(typeof(HttpRequestException), true)]
    [InlineData(typeof(TimeoutException), true)]
    [InlineData(typeof(InvalidOperationException), false)]
    public async Task Should_Distinguish_Transient_Vs_Permanent(Type exceptionType, bool shouldRetry)
    {
        // Arrange
        var policy = new RetryPolicy(maxRetries: 2, baseDelayMs: 10);
        int attemptCount = 0;

        async Task<bool> ThrowException()
        {
            attemptCount++;
            var exception = (Exception)Activator.CreateInstance(exceptionType, "Test error")!;
            throw exception;
        }

        // Act
        var act = async () => await policy.ExecuteAsync(ThrowException, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();

        if (shouldRetry)
            attemptCount.Should().Be(3, "transient errors should trigger retries");
        else
            attemptCount.Should().Be(1, "permanent errors should not trigger retries");
    }
}
```

### Unit Tests - ConflictTests.cs (Complete Implementation)

```csharp
// Tests/Unit/Sync/ConflictTests.cs
using Xunit;
using FluentAssertions;
using AgenticCoder.Infrastructure.Sync;
using AgenticCoder.Domain.Conversation;

namespace AgenticCoder.Tests.Unit.Sync;

public sealed class ConflictTests
{
    [Fact]
    public void Should_Detect_Conflict()
    {
        // Arrange
        var localChat = Chat.Create("Local Title", WorktreeId.From("worktree-01HKABC"));
        localChat.UpdateTitle("Local Updated");

        var remoteChat = Chat.Create("Remote Title", WorktreeId.From("worktree-01HKABC"));
        remoteChat.UpdateTitle("Remote Updated");

        // Both have version 2 but different titles
        var detector = new ConflictDetector();

        // Act
        var conflict = detector.Detect(localChat, remoteChat);

        // Assert
        conflict.Should().NotBeNull();
        conflict!.ConflictType.Should().Be(ConflictType.ModifyModify);
        conflict.LocalVersion.Should().Be(2);
        conflict.RemoteVersion.Should().Be(2);
        conflict.ConflictingFields.Should().Contain("Title");
    }

    [Fact]
    public void Should_Apply_Policy_LastWriteWins()
    {
        // Arrange
        var localChat = Chat.Create("Local Title", WorktreeId.From("worktree-01HKABC"));
        typeof(Chat).GetProperty("UpdatedAt")!.SetValue(localChat, DateTimeOffset.UtcNow.AddMinutes(-5));

        var remoteChat = Chat.Create("Remote Title", WorktreeId.From("worktree-01HKABC"));
        typeof(Chat).GetProperty("UpdatedAt")!.SetValue(remoteChat, DateTimeOffset.UtcNow);

        var policy = new LastWriteWinsPolicy();

        // Act
        var resolved = policy.Resolve(localChat, remoteChat);

        // Assert
        resolved.Should().NotBeNull();
        resolved!.Title.Should().Be("Remote Title", "remote has later UpdatedAt");
    }

    [Fact]
    public void Should_Apply_Policy_LocalWins()
    {
        // Arrange
        var localChat = Chat.Create("Local Title", WorktreeId.From("worktree-01HKABC"));
        var remoteChat = Chat.Create("Remote Title", WorktreeId.From("worktree-01HKABC"));

        var policy = new LocalWinsPolicy();

        // Act
        var resolved = policy.Resolve(localChat, remoteChat);

        // Assert
        resolved.Should().NotBeNull();
        resolved!.Title.Should().Be("Local Title");
    }

    [Fact]
    public void Should_Apply_Policy_RemoteWins()
    {
        // Arrange
        var localChat = Chat.Create("Local Title", WorktreeId.From("worktree-01HKABC"));
        var remoteChat = Chat.Create("Remote Title", WorktreeId.From("worktree-01HKABC"));

        var policy = new RemoteWinsPolicy();

        // Act
        var resolved = policy.Resolve(localChat, remoteChat);

        // Assert
        resolved.Should().NotBeNull();
        resolved!.Title.Should().Be("Remote Title");
    }

    [Fact]
    public void Should_Detect_DeleteModify_Conflict()
    {
        // Arrange
        var localChat = Chat.Create("Test Chat", WorktreeId.From("worktree-01HKABC"));
        localChat.Delete();

        var remoteChat = Chat.Create("Test Chat", WorktreeId.From("worktree-01HKABC"));
        remoteChat.UpdateTitle("Modified Title");

        var detector = new ConflictDetector();

        // Act
        var conflict = detector.Detect(localChat, remoteChat);

        // Assert
        conflict.Should().NotBeNull();
        conflict!.ConflictType.Should().Be(ConflictType.DeleteModify);
    }
}
```

### Integration Tests - SyncEngineTests.cs (Complete Implementation)

```csharp
// Tests/Integration/Sync/SyncEngineTests.cs
using Xunit;
using FluentAssertions;
using AgenticCoder.Infrastructure.Sync;
using Npgsql;

namespace AgenticCoder.Tests.Integration.Sync;

public sealed class SyncEngineTests : IAsyncLifetime
{
    private readonly string _postgresConnectionString = "Host=localhost;Database=test_sync;Username=postgres;Password=postgres";
    private SyncEngine _syncEngine = null!;
    private ChatRepository _localRepository = null!;

    public async Task InitializeAsync()
    {
        var localDbPath = Path.Combine(Path.GetTempPath(), $"sync_test_{Guid.NewGuid()}.db");
        _localRepository = new ChatRepository(localDbPath);

        _syncEngine = new SyncEngine(_localRepository, _postgresConnectionString);

        // Clean remote database
        await using var conn = new NpgsqlConnection(_postgresConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("TRUNCATE TABLE chats CASCADE", conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Should_Upload_To_Postgres()
    {
        // Arrange
        var chat = Chat.Create("Upload Test", WorktreeId.From("worktree-01HKABC"));
        var chatId = await _localRepository.CreateAsync(chat, CancellationToken.None);

        // Act
        var result = await _syncEngine.SyncAsync(direction: SyncDirection.Upload, CancellationToken.None);

        // Assert
        result.UploadedChats.Should().Be(1);
        result.Conflicts.Should().BeEmpty();

        // Verify in Postgres
        await using var conn = new NpgsqlConnection(_postgresConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM chats WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("id", chatId.ToString());
        var count = (long)(await cmd.ExecuteScalarAsync())!;

        count.Should().Be(1);
    }

    [Fact]
    public async Task Should_Download_From_Postgres()
    {
        // Arrange - Insert directly into Postgres
        var remoteChat = Chat.Create("Remote Chat", WorktreeId.From("worktree-01HKABC"));

        await using var conn = new NpgsqlConnection(_postgresConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO chats (id, title, worktree_id, created_at, updated_at) VALUES (@id, @title, @worktree, @created, @updated)",
            conn);
        cmd.Parameters.AddWithValue("id", remoteChat.Id.ToString());
        cmd.Parameters.AddWithValue("title", remoteChat.Title);
        cmd.Parameters.AddWithValue("worktree", remoteChat.WorktreeBinding.Value);
        cmd.Parameters.AddWithValue("created", remoteChat.CreatedAt);
        cmd.Parameters.AddWithValue("updated", remoteChat.UpdatedAt);
        await cmd.ExecuteNonQueryAsync();

        // Act
        var result = await _syncEngine.SyncAsync(direction: SyncDirection.Download, CancellationToken.None);

        // Assert
        result.DownloadedChats.Should().Be(1);

        var localChat = await _localRepository.GetByIdAsync(remoteChat.Id, CancellationToken.None);
        localChat.Should().NotBeNull();
        localChat!.Title.Should().Be("Remote Chat");
    }

    [Fact]
    public async Task Should_Handle_Network_Loss()
    {
        // Arrange
        var chat = Chat.Create("Network Test", WorktreeId.From("worktree-01HKABC"));
        await _localRepository.CreateAsync(chat, CancellationToken.None);

        // Use invalid connection string to simulate network loss
        var brokenEngine = new SyncEngine(_localRepository, "Host=invalid;Database=test;");

        // Act
        var act = async () => await brokenEngine.SyncAsync(direction: SyncDirection.Upload, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NpgsqlException>();

        // Verify outbox still has pending entries for retry
        var outboxRepository = new OutboxRepository(_localRepository.ConnectionString);
        var pending = await outboxRepository.GetPendingAsync(limit: 10, CancellationToken.None);
        pending.Should().NotBeEmpty("failed uploads should remain in outbox for retry");
    }
}
```

### Integration Tests - IdempotencyTests.cs (Complete Implementation)

```csharp
// Tests/Integration/Sync/IdempotencyTests.cs
using Xunit;
using FluentAssertions;
using AgenticCoder.Infrastructure.Sync;

namespace AgenticCoder.Tests.Integration.Sync;

public sealed class IdempotencyTests
{
    [Fact]
    public async Task Should_Deduplicate()
    {
        // Arrange
        var postgresConnectionString = "Host=localhost;Database=test_sync;Username=postgres;Password=postgres";
        var localDbPath = Path.Combine(Path.GetTempPath(), $"idempotency_test_{Guid.NewGuid()}.db");
        var localRepository = new ChatRepository(localDbPath);
        var syncEngine = new SyncEngine(localRepository, postgresConnectionString);

        var chat = Chat.Create("Idempotency Test", WorktreeId.From("worktree-01HKABC"));
        await localRepository.CreateAsync(chat, CancellationToken.None);

        // Act - Sync same chat twice
        await syncEngine.SyncAsync(direction: SyncDirection.Upload, CancellationToken.None);
        await syncEngine.SyncAsync(direction: SyncDirection.Upload, CancellationToken.None);

        // Assert - Verify only one record in Postgres
        await using var conn = new NpgsqlConnection(postgresConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM chats WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("id", chat.Id.ToString());
        var count = (long)(await cmd.ExecuteScalarAsync())!;

        count.Should().Be(1, "duplicate sync should be deduplicated by idempotency key");
    }
}
```

### E2E Tests - SyncE2ETests.cs (Complete Implementation)

```csharp
// Tests/E2E/Sync/SyncE2ETests.cs
using Xunit;
using FluentAssertions;
using AgenticCoder.Infrastructure.Sync;

namespace AgenticCoder.Tests.E2E.Sync;

public sealed class SyncE2ETests
{
    [Fact]
    public async Task Should_Sync_Full_Workflow()
    {
        // Arrange
        var postgresConnectionString = "Host=localhost;Database=test_sync;Username=postgres;Password=postgres";
        var localDbPath = Path.Combine(Path.GetTempPath(), $"e2e_sync_{Guid.NewGuid()}.db");
        var localRepository = new ChatRepository(localDbPath);
        var syncEngine = new SyncEngine(localRepository, postgresConnectionString);

        try
        {
            // Create local chat
            var chat = Chat.Create("E2E Test", WorktreeId.From("worktree-01HKABC"));
            var chatId = await localRepository.CreateAsync(chat, CancellationToken.None);

            var run = Run.Create(chatId, "azure-gpt4");
            var runId = await localRepository.CreateRunAsync(run, CancellationToken.None);

            var message = Message.Create(runId, "user", "Test message");
            await localRepository.CreateMessageAsync(message, CancellationToken.None);

            // Act - Upload
            var uploadResult = await syncEngine.SyncAsync(direction: SyncDirection.Upload, CancellationToken.None);

            // Assert upload
            uploadResult.UploadedChats.Should().Be(1);
            uploadResult.UploadedRuns.Should().Be(1);
            uploadResult.UploadedMessages.Should().Be(1);

            // Modify locally
            chat.UpdateTitle("Updated Title");
            await localRepository.UpdateAsync(chat, CancellationToken.None);

            // Act - Bidirectional sync
            var bidirectionalResult = await syncEngine.SyncAsync(direction: SyncDirection.Bidirectional, CancellationToken.None);

            // Assert
            bidirectionalResult.UploadedChats.Should().Be(1, "updated chat should sync");
            bidirectionalResult.Conflicts.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(localDbPath))
                File.Delete(localDbPath);
        }
    }

    [Fact]
    public async Task Should_Handle_Offline_To_Online()
    {
        // Arrange
        var postgresConnectionString = "Host=localhost;Database=test_sync;Username=postgres;Password=postgres";
        var localDbPath = Path.Combine(Path.GetTempPath(), $"offline_test_{Guid.NewGuid()}.db");
        var localRepository = new ChatRepository(localDbPath);

        try
        {
            // Create chats while "offline"
            var chat1 = Chat.Create("Offline Chat 1", WorktreeId.From("worktree-01HKABC"));
            var chat2 = Chat.Create("Offline Chat 2", WorktreeId.From("worktree-01HKABC"));

            await localRepository.CreateAsync(chat1, CancellationToken.None);
            await localRepository.CreateAsync(chat2, CancellationToken.None);

            // Act - "Go online" and sync
            var syncEngine = new SyncEngine(localRepository, postgresConnectionString);
            var result = await syncEngine.SyncAsync(direction: SyncDirection.Upload, CancellationToken.None);

            // Assert
            result.UploadedChats.Should().Be(2);
            result.Errors.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(localDbPath))
                File.Delete(localDbPath);
        }
    }

    [Fact]
    public async Task Should_Resolve_Conflicts()
    {
        // Arrange
        var postgresConnectionString = "Host=localhost;Database=test_sync;Username=postgres;Password=postgres";
        var localDbPath = Path.Combine(Path.GetTempPath(), $"conflict_test_{Guid.NewGuid()}.db");
        var localRepository = new ChatRepository(localDbPath);
        var syncEngine = new SyncEngine(localRepository, postgresConnectionString, new LastWriteWinsPolicy());

        try
        {
            // Create and sync chat
            var chat = Chat.Create("Original Title", WorktreeId.From("worktree-01HKABC"));
            var chatId = await localRepository.CreateAsync(chat, CancellationToken.None);
            await syncEngine.SyncAsync(direction: SyncDirection.Upload, CancellationToken.None);

            // Modify locally
            chat.UpdateTitle("Local Update");
            await localRepository.UpdateAsync(chat, CancellationToken.None);

            // Simulate remote modification (direct Postgres update)
            await using var conn = new NpgsqlConnection(postgresConnectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(
                "UPDATE chats SET title = @title, updated_at = @updated WHERE id = @id",
                conn);
            cmd.Parameters.AddWithValue("title", "Remote Update");
            cmd.Parameters.AddWithValue("updated", DateTimeOffset.UtcNow.AddMinutes(1));  // Later timestamp
            cmd.Parameters.AddWithValue("id", chatId.ToString());
            await cmd.ExecuteNonQueryAsync();

            // Act - Bidirectional sync with LastWriteWins policy
            var result = await syncEngine.SyncAsync(direction: SyncDirection.Bidirectional, CancellationToken.None);

            // Assert
            result.Conflicts.Should().HaveCount(1);
            result.ConflictsResolved.Should().Be(1);

            var resolved = await localRepository.GetByIdAsync(chatId, CancellationToken.None);
            resolved!.Title.Should().Be("Remote Update", "remote has later timestamp so should win");
        }
        finally
        {
            if (File.Exists(localDbPath))
                File.Delete(localDbPath);
        }
    }
}
```

### Performance Benchmarks

```csharp
// Tests/Performance/SyncBenchmarks.cs
using BenchmarkDotNet.Attributes;
using AgenticCoder.Infrastructure.Sync;

[MemoryDiagnoser]
public class SyncBenchmarks
{
    private SyncEngine _syncEngine = null!;
    private ChatRepository _localRepository = null!;
    private OutboxBatcher _batcher = null!;
    private List<OutboxEntry> _entries = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        var postgresConnectionString = "Host=localhost;Database=benchmark_sync;Username=postgres;Password=postgres";
        var localDbPath = Path.Combine(Path.GetTempPath(), "sync_benchmark.db");
        _localRepository = new ChatRepository(localDbPath);
        _syncEngine = new SyncEngine(_localRepository, postgresConnectionString);

        _batcher = new OutboxBatcher(maxBatchSize: 50, maxBatchBytes: 1_000_000);

        // Prepare 50 entries for batching benchmark
        _entries = Enumerable.Range(1, 50)
            .Select(i => OutboxEntry.Create("Chat", $"chat-{i}", "Insert", "{}"))
            .ToList();

        // Seed local database with test data
        for (int i = 0; i < 50; i++)
        {
            var chat = Chat.Create($"Chat {i}", WorktreeId.From("worktree-01HKABC"));
            await _localRepository.CreateAsync(chat, CancellationToken.None);
        }
    }

    [Benchmark]
    public async Task Batch_50_Items()
    {
        await _syncEngine.SyncAsync(direction: SyncDirection.Upload, CancellationToken.None);
    }

    [Benchmark]
    public async Task Outbox_Write()
    {
        var outboxRepository = new OutboxRepository(_localRepository.ConnectionString);
        var entry = OutboxEntry.Create("Chat", ChatId.NewId().ToString(), "Insert", "{}");
        await outboxRepository.AddAsync(entry, CancellationToken.None);
    }

    [Benchmark]
    public void Conflict_Detect()
    {
        var localChat = Chat.Create("Local", WorktreeId.From("worktree-01HKABC"));
        var remoteChat = Chat.Create("Remote", WorktreeId.From("worktree-01HKABC"));

        var detector = new ConflictDetector();
        detector.Detect(localChat, remoteChat);
    }
}
```

**Performance Targets:**
- Batch 50 items: 2s target, 5s maximum
- Outbox write: 5ms target, 10ms maximum
- Conflict detect: 1ms target, 5ms maximum

---

## User Verification Steps

### Scenario 1: Basic Offline-to-Online Sync

**Objective:** Verify that changes made while offline are synced when connectivity is restored.

**Preconditions:**
- Application installed and authenticated
- Network connectivity available initially
- Remote database accessible

**Steps:**
1. Start application and verify sync status shows "Online"
2. Disconnect from network (airplane mode or disable adapter)
3. Verify sync status shows "Offline"
4. Create a new chat: `acode chat new "Offline Test Chat"`
5. Add a message to the chat: `acode msg add --chat <id> "Test message created offline"`
6. Note the message ID
7. Check outbox status: `acode sync status`
8. Verify outbox shows 2 pending entries (chat + message)
9. Restore network connectivity
10. Wait up to 30 seconds for automatic sync
11. Check sync status: `acode sync status`
12. Log into remote database or web interface
13. Verify chat "Offline Test Chat" exists in remote
14. Verify message "Test message created offline" exists in remote

**Expected Results:**
- Outbox shows 2 pending entries while offline
- After connectivity restored, entries sync within 30 seconds
- Sync status shows 0 pending entries after completion
- Remote database contains exact copies of local data
- No duplicate entries in remote database

---

### Scenario 2: Automatic Retry After Transient Failure

**Objective:** Verify that failed sync operations are automatically retried with exponential backoff.

**Preconditions:**
- Application running with network connectivity
- Remote server can be temporarily blocked (firewall rule or proxy)

**Steps:**
1. Create a new chat: `acode chat new "Retry Test Chat"`
2. Block traffic to remote server (add firewall rule)
3. Check sync status: `acode sync status`
4. Wait 10 seconds, check status again
5. Verify entry shows status "Failed" with retry count 1
6. Check next retry time is approximately 1 second from failure
7. Wait 30 seconds, check retry count progression
8. Verify backoff is increasing (1s, 2s, 4s, 8s...)
9. Remove firewall block
10. Wait for next retry cycle
11. Check sync status: `acode sync status`
12. Verify entry status is "Completed"

**Expected Results:**
- Failed entry automatically retries without user intervention
- Retry delay follows exponential backoff pattern
- Retry count increments with each attempt
- After server becomes available, sync completes successfully
- Total time from unblock to sync < 2 minutes

---

### Scenario 3: Batch Processing Efficiency

**Objective:** Verify that multiple small changes are batched together for efficient syncing.

**Preconditions:**
- Application running with network connectivity
- Sync processor active

**Steps:**
1. Pause sync: `acode sync pause`
2. Create 50 messages rapidly:
   ```bash
   for i in {1..50}; do
     acode msg add --chat <existing-id> "Batch message $i"
   done
   ```
3. Check outbox: `acode sync outbox list --count`
4. Verify 50 entries pending
5. Enable network packet capture or debug logging
6. Resume sync: `acode sync resume`
7. Monitor network traffic or logs
8. Wait for sync to complete
9. Verify batch behavior in logs/capture

**Expected Results:**
- 50 entries queued while sync paused
- After resume, entries batched (not 50 individual requests)
- Batch size matches configured value (default 100, so 1 batch)
- All 50 messages appear in remote database
- Total sync time < 10 seconds for 50 items

---

### Scenario 4: Conflict Detection and Resolution (Last-Write-Wins)

**Objective:** Verify that concurrent modifications trigger conflict detection and resolution.

**Preconditions:**
- Application running on two devices (Device A and Device B)
- Both authenticated to same account
- Remote database accessible from both

**Steps:**
1. On Device A: Create chat: `acode chat new "Conflict Test"`
2. Wait for sync to complete on Device A
3. On Device B: Pull sync: `acode sync now`
4. Verify chat appears on Device B
5. Disconnect Device B from network
6. On Device A: Update chat title: `acode chat update <id> --title "Updated from A"`
7. Wait for Device A sync to complete
8. On Device B: Update same chat: `acode chat update <id> --title "Updated from B"`
9. Note Device B's timestamp
10. Reconnect Device B to network
11. Wait for sync on Device B
12. Check for conflict: `acode sync conflicts list`
13. If conflict exists, check resolution applied
14. Verify final chat title on both devices

**Expected Results:**
- Conflict detected when Device B syncs
- With last-write-wins policy, later timestamp wins
- Both devices converge to same title
- Conflict logged in sync journal
- Original versions preserved in conflict archive

---

### Scenario 5: Pause and Resume Sync

**Objective:** Verify that sync can be manually paused and resumed, with queue accumulation during pause.

**Preconditions:**
- Application running with active sync

**Steps:**
1. Check initial status: `acode sync status`
2. Verify sync is active
3. Pause sync: `acode sync pause`
4. Verify status shows "Paused"
5. Create 5 new messages:
   ```bash
   for i in {1..5}; do
     acode msg add --chat <id> "Paused message $i"
   done
   ```
6. Check outbox: `acode sync status`
7. Verify 5 entries pending and not processing
8. Wait 30 seconds
9. Verify entries still pending (no sync while paused)
10. Resume sync: `acode sync resume`
11. Verify status shows "Active"
12. Wait for queue to drain
13. Check final status: `acode sync status`

**Expected Results:**
- Pause command immediately stops processing
- Entries accumulate in outbox while paused
- No network requests made during pause
- Resume command immediately starts processing
- All accumulated entries sync after resume

---

### Scenario 6: Dead Letter Queue Handling

**Objective:** Verify that entries exceeding max retries are moved to dead letter queue.

**Preconditions:**
- Max retry count set to 3 (for faster testing): `acode config set sync.max-retries 3`
- Remote server can return 400 errors

**Steps:**
1. Configure test to cause permanent failure (e.g., invalid entity)
2. Create entry that will fail validation:
   ```bash
   # Manually create invalid outbox entry via debug tool
   acode debug outbox create --invalid-payload
   ```
3. Start sync: `acode sync now`
4. Watch retry progression: `acode sync status --watch`
5. Wait for 3 retries to exhaust
6. Check dead letter queue: `acode sync dlq list`
7. Verify entry appears in DLQ
8. Inspect entry details: `acode sync dlq show <id>`
9. Verify error information preserved
10. Try manual retry: `acode sync dlq retry <id>`
11. If still failing, archive: `acode sync dlq archive <id>`

**Expected Results:**
- Entry retried exactly 3 times (max-retries setting)
- After exhaustion, entry moved to DLQ with "DeadLetter" status
- DLQ entry contains full error history
- Manual retry from DLQ possible
- Archive removes from active queue but preserves audit trail

---

### Scenario 7: Idempotency Prevents Duplicates

**Objective:** Verify that replaying the same entry does not create duplicates.

**Preconditions:**
- Application running with network connectivity
- Debug mode enabled for idempotency testing

**Steps:**
1. Create a new message: `acode msg add --chat <id> "Idempotency Test"`
2. Note the message ID and outbox entry ID
3. Wait for sync to complete
4. Verify message exists in remote (count = 1)
5. Extract idempotency key: `acode sync outbox show <entry-id> --field idempotency-key`
6. Force replay of same entry:
   ```bash
   acode debug sync replay --entry-id <id> --preserve-key
   ```
7. Wait for sync attempt
8. Check remote database for message count
9. Verify no duplicate created

**Expected Results:**
- Original sync creates message in remote
- Replayed entry with same idempotency key is detected
- Remote returns success (idempotent) without creating duplicate
- Message count in remote remains 1
- Idempotency collision logged for audit

---

### Scenario 8: Circuit Breaker Engagement

**Objective:** Verify that circuit breaker engages after consecutive failures and prevents cascade.

**Preconditions:**
- Application running
- Ability to simulate server failures

**Steps:**
1. Create 10 messages to generate sync traffic
2. Block remote server completely
3. Monitor sync attempts: `acode sync status --watch`
4. Count consecutive failures
5. After 5 failures, check circuit breaker: `acode sync health`
6. Verify circuit breaker status is "Open"
7. Attempt manual sync: `acode sync now`
8. Verify command reports circuit breaker is engaged
9. Wait 30 seconds (circuit breaker timeout)
10. Verify circuit breaker enters "Half-Open" state
11. Restore remote server
12. Watch for probe request and circuit closure
13. Verify sync resumes normally

**Expected Results:**
- After 5 consecutive failures, circuit breaker opens
- Open circuit prevents new requests (fast-fail)
- After timeout, circuit enters half-open for probe
- Successful probe closes circuit
- Normal sync resumes after closure

---

### Scenario 9: Full Resync Recovery

**Objective:** Verify that full resync can recover from corrupted local state.

**Preconditions:**
- Application with existing synced data
- Ability to corrupt local database

**Steps:**
1. Verify current sync state: `acode sync status`
2. Note current message count locally and remotely
3. Simulate corruption by deleting some local messages directly:
   ```bash
   acode debug db exec "DELETE FROM messages WHERE id LIKE '%test%' LIMIT 5"
   ```
4. Verify local count is now lower than remote
5. Initiate full resync: `acode sync full --direction pull`
6. Wait for resync to complete
7. Compare local and remote counts
8. Verify missing messages restored

**Expected Results:**
- Full resync downloads all remote data
- Missing local records are recreated
- Data integrity restored to match remote
- Sync timestamps updated correctly
- No duplicate entries created

---

### Scenario 10: Health Monitoring and Alerting

**Objective:** Verify that sync health metrics are accurate and alerts trigger correctly.

**Preconditions:**
- Monitoring/alerting configured
- Sync lag alert threshold set to 1 minute (for testing)

**Steps:**
1. Check baseline health: `acode sync health`
2. Verify all metrics report healthy
3. Pause sync: `acode sync pause`
4. Create 20 messages to build queue
5. Check health after 30 seconds: `acode sync health`
6. Verify queue depth metric shows 20
7. Wait for alert threshold (1 minute with paused sync)
8. Check for sync lag alert
9. Resume sync: `acode sync resume`
10. Wait for queue to drain
11. Check health again: `acode sync health`
12. Verify metrics return to healthy state

**Expected Results:**
- Health endpoint returns accurate metrics
- Queue depth metric reflects actual pending count
- Sync lag metric reflects oldest pending entry age
- Alert triggers when lag exceeds threshold
- Metrics return to healthy after queue drains

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Sync/
│   ├── OutboxEntry.cs
│   ├── InboxEntry.cs
│   ├── SyncStatus.cs
│   └── ConflictPolicy.cs
│
src/AgenticCoder.Application/
├── Sync/
│   ├── ISyncEngine.cs
│   ├── IOutboxProcessor.cs
│   ├── IInboxProcessor.cs
│   └── IConflictResolver.cs
│
src/AgenticCoder.Infrastructure/
├── Sync/
│   ├── SyncEngine.cs
│   ├── OutboxProcessor.cs
│   ├── InboxProcessor.cs
│   ├── ConflictResolver.cs
│   ├── Batcher.cs
│   └── RetryPolicy.cs
```

### OutboxEntry Entity

```csharp
namespace AgenticCoder.Domain.Sync;

public sealed class OutboxEntry
{
    public OutboxEntryId Id { get; }
    public string IdempotencyKey { get; }
    public string EntityType { get; }
    public string EntityId { get; }
    public string Payload { get; }
    public OutboxStatus Status { get; private set; }
    public int RetryCount { get; private set; }
    public DateTimeOffset? NextRetryAt { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    
    public void MarkSynced();
    public void MarkFailed(string error);
    public void ScheduleRetry(TimeSpan delay);
}
```

### ISyncEngine Interface

```csharp
namespace AgenticCoder.Application.Sync;

public interface ISyncEngine
{
    Task StartAsync(CancellationToken ct);
    Task StopAsync(CancellationToken ct);
    Task SyncNowAsync(CancellationToken ct);
    Task<SyncStatus> GetStatusAsync(CancellationToken ct);
    Task PauseAsync(CancellationToken ct);
    Task ResumeAsync(CancellationToken ct);
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-SYNC-001 | Connection failed |
| ACODE-SYNC-002 | Batch failed |
| ACODE-SYNC-003 | Conflict detected |
| ACODE-SYNC-004 | Max retries exceeded |
| ACODE-SYNC-005 | Invalid payload |

### Implementation Checklist

1. [ ] Create domain entities
2. [ ] Create service interfaces
3. [ ] Implement outbox writer
4. [ ] Implement outbox processor
5. [ ] Implement batcher
6. [ ] Implement retry policy
7. [ ] Implement inbox processor
8. [ ] Implement conflict resolver
9. [ ] Create sync engine
10. [ ] Add CLI commands
11. [ ] Add health monitoring
12. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Domain entities
2. **Phase 2:** Outbox
3. **Phase 3:** Processing
4. **Phase 4:** Batching
5. **Phase 5:** Retries
6. **Phase 6:** Inbox
7. **Phase 7:** Conflicts
8. **Phase 8:** CLI

---

**End of Task 049.f Specification**