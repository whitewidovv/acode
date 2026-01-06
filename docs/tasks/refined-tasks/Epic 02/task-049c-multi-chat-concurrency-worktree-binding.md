# Task 049.c: Multi-Chat Concurrency Model + Run/Worktree Binding

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 2 – CLI + Orchestration Core  
**Dependencies:** Task 049.b (CLI Commands), Task 023 (Events), Task 027 (Git Integration)  

---

## Description

**Business Value & ROI**

Multi-chat concurrency with worktree binding saves developers $18,420/year per engineer by eliminating context switching overhead and preventing concurrent execution conflicts. A 10-engineer team saves $184,200 annually.

**Time Savings Breakdown:**
- Context switching time: 45 min/day → 5 min/day (89% reduction)
  - Manual chat selection eliminated: 20 min/day saved
  - Wrong context errors prevented: 15 min/day saved
  - Binding management automated: 10 min/day saved
- Lock conflict resolution: 30 min/week → 2 min/week (93% reduction)
  - Concurrent execution detection: 15 min/week saved
  - State corruption recovery: 10 min/week saved
  - Lock cleanup automated: 5 min/week saved

**Cost Calculation:**
- 40 min/day × 220 days/year = 146.7 hours/year saved
- 28 min/week × 52 weeks/year = 24.3 hours/year saved
- **Total: 171 hours/year per engineer @ $108/hour = $18,420/year**
- **10-engineer team: $184,200/year**

**Technical Architecture**

The multi-chat concurrency system implements three core mechanisms: worktree binding, context resolution, and lock management. These work together to provide automatic context switching, prevent race conditions, and maintain conversation isolation.

**Worktree Binding Model:**

```
Workspace (SQLite database)
├── Worktree-1 (feature/auth)
│   └── Bound Chat-A ("Auth Implementation")
│       ├── Run-1 (completed)
│       ├── Run-2 (completed)
│       └── Run-3 (in progress - LOCKED)
├── Worktree-2 (feature/api)
│   └── Bound Chat-B ("API Design")
│       └── Run-4 (completed)
└── Worktree-3 (main)
    └── Unbound (uses global chat)
```

**Binding Semantics:**
- **One-to-One:** Each worktree has at most one bound chat
- **Optional:** Worktrees can operate with unbound (global) chats
- **Persistent:** Bindings survive process restarts, machine reboots
- **Cascading:** Deleting worktree unbinds chat, purging chat unbinds worktree
- **Automatic:** New chats created within worktrees auto-bind (unless --no-bind)

**Context Resolution Flow:**

```
User runs: cd ~/project/feature/auth && acode run "Continue auth"

1. Detect Worktree
   ├── Check current directory for .git file
   ├── Parse .git file to find worktree path
   └── Extract worktree ID from Git config

2. Resolve Binding
   ├── Query workspace database for worktree binding
   ├── If bound → Load bound chat as active
   └── If unbound → Use global/manual chat selection

3. Set Session Context
   ├── Update session state with active chat
   ├── Update prompt/status display
   └── Log context switch event

4. Validate Lock
   ├── Check if worktree is locked
   ├── If locked → Return "BUSY" error with lock details
   └── If unlocked → Proceed to acquire lock
```

**Lock Management Implementation:**

Locks are **file-based** using the `.agent/locks/` directory within each worktree:

```
feature/auth/.agent/locks/
└── worktree-01HKABCDEFGHJ.lock
    {
      "lockedBy": "process-12345",
      "lockedAt": "2024-01-15T10:30:00Z",
      "hostname": "dev-machine",
      "terminal": "/dev/ttys001"
    }
```

**Lock Acquisition Algorithm:**

1. **Check Existing Lock:**
   - If lock file exists and not stale → Return BUSY error
   - If lock file exists and stale (>5 min) → Delete and proceed
   - If no lock file → Proceed

2. **Create Lock File:**
   - Write JSON with process ID, timestamp, hostname, terminal
   - Use atomic file write (write to temp, rename)
   - Set file permissions to 600 (owner read/write only)

3. **Verify Lock:**
   - Re-read lock file to confirm our process ID
   - If mismatch → Another process won race, return BUSY
   - If match → Lock acquired successfully

4. **Hold Lock During Run:**
   - Lock remains until run completes or process dies
   - Lock file prevents other sessions from running

5. **Release Lock:**
   - Delete lock file atomically
   - Log lock release event
   - Notify any waiting sessions (future enhancement)

**Stale Lock Detection:**

Locks older than 5 minutes are considered stale (process likely crashed). The system automatically detects and removes stale locks. Stale threshold is configurable via `workspace.lock_timeout_seconds` (default: 300).

**Stale Lock Criteria:**
- Lock file timestamp > 5 minutes old
- Process ID in lock file no longer exists (optional check on Windows/Linux)
- Hostname matches current machine (don't delete remote locks)

**Concurrency Scenarios:**

| Scenario | Behavior |
|----------|----------|
| Terminal-1 runs, Terminal-2 runs same worktree | Terminal-2 gets BUSY error immediately |
| Terminal-1 runs, Terminal-2 runs different worktree | Both succeed (different locks) |
| Terminal-1 crashes mid-run | Lock becomes stale after 5 minutes, Terminal-2 can proceed |
| Terminal-1 runs with `--wait`, Terminal-2 holds lock | Terminal-1 polls lock every 2 seconds until available or timeout |
| User runs `acode unlock` | Force-deletes lock file (emergency use) |

**Run Isolation Guarantees:**

Each run belongs to exactly one chat and records the worktree it originated from. This provides:

- **No Context Bleed:** Run-1 messages never appear in Run-2 conversation
- **Historical Queries:** "Show me all runs for feature/auth worktree"
- **Audit Trail:** "Which chat was active when this run executed?"
- **Cascade Cleanup:** Purging a chat deletes all its runs and messages

**Integration Points:**

1. **Task-049a (Data Model):**
   - `Chat.WorktreeId` stores binding relationship
   - `Run.WorktreeId` records originating worktree
   - Database queries filter by worktree for isolation

2. **Task-049b (CLI Commands):**
   - `acode chat new` auto-binds if in worktree
   - `acode chat bind <id>` explicitly binds chat
   - `acode chat unbind` clears binding
   - `acode status` shows binding and lock state

3. **Task-022 (Git Integration):**
   - Worktree detection via Git worktree list
   - Worktree path normalization
   - Worktree ID generation from path hash

4. **Task-023 (Events):**
   - Publish `ContextSwitched` event on worktree change
   - Publish `LockAcquired` / `LockReleased` events
   - Subscribers can react to concurrency events

5. **Task-013 (Workspace Context):**
   - Session state tracks current worktree
   - Session state tracks active chat
   - Context resolver coordinates with session manager

**Constraints and Limitations:**

1. **Local-Only:** File-based locking doesn't work across network file systems (NFS, SMB)
2. **Single-Machine:** Worktree bindings are per-workspace, not synchronized across machines
3. **No Distributed Locking:** Cannot prevent concurrent runs from different machines on shared storage
4. **Git Dependency:** Worktree detection requires proper Git worktree setup
5. **Lock Granularity:** Locks entire worktree, not per-file or per-directory
6. **No Lock Queue:** `--wait` polls rather than using event-driven notification
7. **Stale Detection Delay:** 5-minute stale threshold means crashed processes block for 5 min

**Trade-offs and Alternatives:**

1. **File Locks vs. Database Locks:**
   - **Chosen:** File locks in `.agent/locks/`
   - **Alternative:** Database row locking with SELECT FOR UPDATE
   - **Reason:** File locks are simpler, don't require database transaction overhead, visible in file system for debugging

2. **Auto-Bind vs. Manual Bind:**
   - **Chosen:** Auto-bind by default with `--no-bind` opt-out
   - **Alternative:** Manual binding required for all chats
   - **Reason:** Auto-bind matches developer expectations ("I'm in a worktree, use that chat"), reduces cognitive load

3. **Lock on Error vs. Lock on Acquire:**
   - **Chosen:** Immediate BUSY error if locked
   - **Alternative:** Always queue and wait
   - **Reason:** Fail-fast prevents surprise delays, explicit `--wait` for queuing behavior

4. **Stale 5-Min vs. Stale 1-Min:**
   - **Chosen:** 5-minute stale threshold
   - **Alternative:** 1-minute threshold for faster recovery
   - **Reason:** Avoid false positives from long-running operations (LLM inference can take 2-3 minutes)

**Performance Targets:**

| Operation | Target Latency | Max Latency | Rationale |
|-----------|----------------|-------------|-----------|
| Context switch | 25ms | 50ms | Must feel instant when changing directories |
| Lock acquire | 5ms | 10ms | Simple file creation, should be nearly instant |
| Lock release | 5ms | 10ms | Simple file deletion, should be nearly instant |
| Binding query | 2ms | 5ms | Database lookup with worktree ID index |
| Stale detection | 100ms | 200ms | Iterates lock directory, checks timestamps |
| Worktree detection | 50ms | 100ms | Git worktree list parsing, should be fast |

**Observability:**

- **Metrics:** Lock acquisition time, stale lock count, context switch latency, binding cache hit rate
- **Logs:** Context switches (INFO), lock acquisitions/releases (DEBUG), stale lock removals (WARN), lock conflicts (WARN)
- **Error Codes:** ACODE-CONC-001 through ACODE-CONC-005 for diagnosable failures

---

## Use Cases

### Use Case 1: DevBot - Multi-Feature Development with Auto-Binding

**Persona:** DevBot is a senior full-stack developer working on an e-commerce platform. He typically has 4-6 feature branches active simultaneously, each in its own Git worktree.

**Before (Manual Context Management):**

DevBot maintains a mental map of which chat belongs to which feature. When switching between features, he must manually run `acode chat list`, find the right chat ID, then `acode chat open <id>`. He frequently opens the wrong chat, leading to context pollution (auth questions mixed with payment implementation).

Typical workflow:
```bash
cd ~/project/feature/auth
# Oh no, which chat was I using for auth?
acode chat list  # Scan through 6 chats
# Ah, it's chat-abc123
acode chat open abc123
acode run "Continue implementing JWT validation"

cd ~/project/feature/payments
# Forgot to switch chat!
acode run "Add Stripe webhook handler"
# Context pollution: LLM thinks we're still doing auth
```

**Time spent:** 5 minutes per context switch × 12 switches/day = 60 min/day wasted

**After (Automatic Worktree Binding):**

DevBot creates a worktree and chat together. The binding is automatic and persistent. Switching directories automatically switches chat context.

```bash
# One-time setup
cd ~/project/feature/auth
acode chat new "Auth Implementation"  # Auto-binds to worktree

cd ~/project/feature/payments
acode chat new "Payment Integration"  # Auto-binds to worktree

# Daily usage - no manual chat selection needed
cd ~/project/feature/auth
acode run "Continue implementing JWT validation"
# Context: Automatically uses "Auth Implementation" chat

cd ~/project/feature/payments
acode run "Add Stripe webhook handler"
# Context: Automatically uses "Payment Integration" chat
# No manual switching! No context pollution!
```

**Time spent:** 0 minutes per context switch (instant)

**Savings:** 60 min/day × 220 days/year = 220 hours/year @ $108/hour = **$23,760/year per engineer**

**Business Impact:** DevBot ships features 15% faster by eliminating context switching friction. Fewer bugs from context pollution. Happier customers.

---

### Use Case 2: Jordan - Incident Response with Lock Safety

**Persona:** Jordan is a platform engineer on-call for production incidents. When alerts fire, she needs to investigate quickly using AI-assisted debugging. Multiple engineers may be investigating simultaneously.

**Before (No Lock Management):**

Multiple engineers SSH into the same debugging worktree. They run AI commands concurrently. Race conditions corrupt conversation state. Messages interleave, context becomes unusable, engineers waste time re-establishing context.

Incident workflow:
```bash
# Engineer-1 (Jordan)
cd ~/debug/incident-2024-01-15
acode run "Analyze this stack trace"
# LLM starts responding...

# Engineer-2 (Alex) - different terminal, same worktree
cd ~/debug/incident-2024-01-15
acode run "Check database connection pool"
# RACE CONDITION! Both commands write to same chat simultaneously
# Message-1: "The stack trace shows..." (Jordan's response)
# Message-2: "The connection pool..." (Alex's response)
# Message-3: Continuation of Message-1 (out of order!)
# Context destroyed. Must restart conversation.
```

**Time wasted per incident:** 15 minutes to untangle state × 3 incidents/week = 45 min/week = **39 hours/year @ $108/hour = $4,212/year per engineer**

**After (Lock Management):**

The first engineer acquires a worktree lock. Other engineers get immediate feedback and can wait or work elsewhere.

```bash
# Engineer-1 (Jordan)
cd ~/debug/incident-2024-01-15
acode run "Analyze this stack trace"
# Lock acquired automatically
# LLM responds safely

# Engineer-2 (Alex) - different terminal, same worktree
cd ~/debug/incident-2024-01-15
acode run "Check database connection pool"
# ⚠️  Worktree is locked by Jordan (PID 12345, terminal /dev/ttys001)
# ⏱️  Locked since 2024-01-15 10:30:00 (2 minutes ago)
# 💡 Wait for lock to be released? (use --wait) or work elsewhere

acode run "Check database connection pool" --wait
# ⏳ Waiting for lock... (timeout: 5 minutes)
# [Jordan's run completes, lock released]
# ✅ Lock acquired, continuing...
```

**Time saved:** No state corruption, no context re-establishment, clear lock status

**Savings:** 39 hours/year per engineer, **$4,212/year per engineer**  
**5-engineer on-call rotation: $21,060/year team savings**

**Business Impact:** Faster incident resolution (MTTR reduced 10%), fewer escalations, better customer SLA compliance.

---

### Use Case 3: Alex - Automated CI/CD with Worktree Isolation

**Persona:** Alex is a DevOps engineer maintaining a CI/CD pipeline that uses AI for automated code review and test generation. The pipeline runs multiple builds concurrently, each in its own worktree.

**Before (No Worktree Isolation):**

The CI pipeline uses a single shared chat for all builds. Concurrent builds interleave their AI conversations. Code review comments for Build-A appear in Build-B's log. Test generation for Build-C uses context from Build-D. Builds fail randomly due to context pollution.

Pipeline behavior:
```bash
# Build-1: feature/auth (worktree-1)
acode run "Review this diff for security issues"
# Build-2: feature/payments (worktree-2) - starts before Build-1 finishes
acode run "Generate integration tests"
# Both writes go to shared chat -> CONTEXT POLLUTION
# Security review mentions Stripe API (wrong build!)
# Tests include JWT validation (wrong build!)
```

**Failure rate:** 15% of builds fail due to context pollution × 100 builds/day = 15 wasted builds/day
**Time wasted:** 15 builds × 10 min/build = 150 min/day = **550 hours/year @ $108/hour = $59,400/year pipeline cost**

**After (Worktree-Bound Chats):**

Each CI build creates its own worktree and bound chat. Concurrent builds are fully isolated. No context pollution. Build reliability improves to 99.5%.

Pipeline script:
```bash
#!/bin/bash
# CI/CD pipeline for feature branch build

# Create isolated worktree for this build
BUILD_ID="build-$(date +%s)"
git worktree add "/tmp/builds/$BUILD_ID" "$BRANCH_NAME"

cd "/tmp/builds/$BUILD_ID"

# Auto-bind creates isolated chat
acode chat new "CI Build $BUILD_ID"

# All AI commands use isolated chat
acode run "Review this diff for security issues"
# No interference from other builds!

acode run "Generate integration tests"
# Uses correct context from this build only!

# Cleanup
acode chat purge --force
git worktree remove "/tmp/builds/$BUILD_ID"
```

**Failure rate:** 0.5% (only actual bugs, not context pollution)

**Savings:** 540 hours/year @ $108/hour = **$58,320/year pipeline reliability improvement**

**Business Impact:** Developers trust CI results, faster deployments, fewer rollbacks, improved team velocity.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Worktree | Git working directory |
| Binding | Chat-to-worktree association |
| Active Chat | Currently selected conversation |
| Concurrency | Multiple simultaneous operations |
| Locking | Exclusive access control |
| Run Isolation | Preventing context bleed |
| Auto-Bind | Automatic association |
| Unbound | No worktree association |
| Context Switch | Changing active chat |
| Queue | Waiting for lock |
| Busy Error | Lock unavailable |
| Hierarchy | Workspace → Worktree → Chat |
| Cascade | Related entity cleanup |
| Race Condition | Concurrent conflict |
| Session | Terminal/process instance |

---

## Security Considerations

### Threat 1: Lock File Manipulation (Unauthorized Access)

**Risk:** Attacker with file system access modifies or deletes lock files to bypass concurrency control, potentially corrupting conversation state or gaining access to locked resources.

**Attack Scenario:**
```bash
# Attacker observes locked worktree
ls .agent/locks/
# worktree-01HKABC.lock exists

# Attacker deletes lock file
rm .agent/locks/worktree-01HKABC.lock

# Attacker acquires lock while legitimate process still running
acode run "Malicious command with corrupted context"
```

**Mitigation (LockFileValidator - 60 lines):**

```csharp
// src/AgenticCoder.Infrastructure/Concurrency/LockFileValidator.cs
namespace AgenticCoder.Infrastructure.Concurrency;

public sealed class LockFileValidator
{
    private readonly ILogger<LockFileValidator> _logger;
    private readonly IProcessChecker _processChecker;

    public LockFileValidator(
        ILogger<LockFileValidator> logger,
        IProcessChecker processChecker)
    {
        _logger = logger;
        _processChecker = processChecker;
    }

    public async Task<ValidationResult> ValidateAsync(
        string lockFilePath,
        CancellationToken ct)
    {
        // Check file permissions (must be 600 - owner read/write only)
        var fileInfo = new FileInfo(lockFilePath);
        if (!fileInfo.Exists)
        {
            return ValidationResult.Missing("Lock file does not exist");
        }

        // On Unix: Check permissions are 600
        if (!OperatingSystem.IsWindows())
        {
            var permissions = File.GetUnixFileMode(lockFilePath);
            var expectedPermissions = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            if ((permissions & ~expectedPermissions) != 0)
            {
                _logger.LogWarning(
                    "Lock file has suspicious permissions: {Permissions}. Expected: 600",
                    permissions);
                return ValidationResult.Invalid("Incorrect permissions");
            }
        }

        // Parse lock file
        var json = await File.ReadAllTextAsync(lockFilePath, ct);
        var lockData = JsonSerializer.Deserialize<LockData>(json);

        if (lockData is null)
        {
            return ValidationResult.Invalid("Malformed lock file");
        }

        // Verify process still running (prevents stale/deleted locks)
        if (!await _processChecker.IsRunningAsync(lockData.ProcessId, ct))
        {
            _logger.LogWarning(
                "Lock held by dead process {ProcessId}. Marking stale.",
                lockData.ProcessId);
            return ValidationResult.Stale("Process no longer running");
        }

        // Verify hostname matches (prevent cross-machine tampering on shared storage)
        var currentHostname = Environment.MachineName;
        if (!string.Equals(lockData.Hostname, currentHostname, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(
                "Lock file hostname mismatch! Expected: {Current}, Got: {Stored}. Possible tampering.",
                currentHostname, lockData.Hostname);
            return ValidationResult.Invalid("Hostname mismatch");
        }

        return ValidationResult.Valid();
    }
}

public sealed record ValidationResult(bool IsValid, string? Reason)
{
    public static ValidationResult Valid() => new(true, null);
    public static ValidationResult Invalid(string reason) => new(false, reason);
    public static ValidationResult Stale(string reason) => new(false, $"STALE: {reason}");
    public static ValidationResult Missing(string reason) => new(false, $"MISSING: {reason}");
}
```

---

### Threat 2: Lock File Race Condition (TOCTOU)

**Risk:** Time-of-check to time-of-use (TOCTOU) race where two processes check for lock, both see "no lock", both create lock file, both believe they own the lock.

**Attack Scenario:**
```bash
# Process-1 and Process-2 start simultaneously in same worktree

# Process-1: Check for lock (none exists)
if not exists(.agent/locks/worktree.lock):
    # Process-2: Also checks (none exists yet)
    if not exists(.agent/locks/worktree.lock):
        # Process-1: Create lock
        write_lock_file(process_id=1234)
        # Process-2: Also creates lock (RACE!)
        write_lock_file(process_id=5678)
        # BOTH PROCESSES THINK THEY OWN THE LOCK
```

**Mitigation (AtomicFileLockService - 70 lines):**

```csharp
// src/AgenticCoder.Infrastructure/Concurrency/AtomicFileLockService.cs
namespace AgenticCoder.Infrastructure.Concurrency;

public sealed class AtomicFileLockService : ILockService
{
    private readonly ILogger<AtomicFileLockService> _logger;

    public AtomicFileLockService(ILogger<AtomicFileLockService> logger)
    {
        _logger = logger;
    }

    public async Task<IAsyncDisposable> AcquireAsync(
        WorktreeId worktreeId,
        TimeSpan? timeout,
        CancellationToken ct)
    {
        var lockFilePath = GetLockFilePath(worktreeId);
        var tempFilePath = lockFilePath + ".tmp";

        var lockData = new LockData(
            ProcessId: Environment.ProcessId,
            LockedAt: DateTimeOffset.UtcNow,
            Hostname: Environment.MachineName,
            Terminal: GetTerminalId());

        var json = JsonSerializer.Serialize(lockData);

        // ATOMIC OPERATION: Write to temp file, then rename
        // File system guarantees rename is atomic (POSIX and NTFS)
        await File.WriteAllTextAsync(tempFilePath, json, ct);

        // Set permissions to 600 before rename (Unix only)
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(tempFilePath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        try
        {
            // Atomic rename: Either succeeds exclusively or fails
            // If another process already created lock file, this throws IOException
            File.Move(tempFilePath, lockFilePath, overwrite: false);
        }
        catch (IOException ex) when (File.Exists(lockFilePath))
        {
            // Lock file already exists - another process won the race
            _logger.LogDebug("Lock acquisition failed: File already exists");

            // Clean up temp file
            File.Delete(tempFilePath);

            // Check if we should wait or error
            if (timeout.HasValue)
            {
                return await WaitForLockAsync(worktreeId, timeout.Value, ct);
            }

            throw new LockBusyException(worktreeId, "Worktree is locked by another process");
        }

        // VERIFY: Re-read lock file to ensure our process ID is recorded
        var verifyJson = await File.ReadAllTextAsync(lockFilePath, ct);
        var verifyData = JsonSerializer.Deserialize<LockData>(verifyJson);

        if (verifyData?.ProcessId != Environment.ProcessId)
        {
            // Another process overwrote our lock (filesystem doesn't support atomic rename?)
            _logger.LogError(
                "Lock verification failed! Expected PID: {Expected}, Got: {Actual}",
                Environment.ProcessId, verifyData?.ProcessId);
            throw new LockCorruptedException(worktreeId, "Lock ownership verification failed");
        }

        _logger.LogInformation("Lock acquired for worktree {WorktreeId}", worktreeId);

        return new FileLock(lockFilePath, _logger);
    }

    private sealed class FileLock : IAsyncDisposable
    {
        private readonly string _lockFilePath;
        private readonly ILogger _logger;
        private bool _disposed;

        public FileLock(string lockFilePath, ILogger logger)
        {
            _lockFilePath = lockFilePath;
            _logger = logger;
        }

        public ValueTask DisposeAsync()
        {
            if (_disposed) return ValueTask.CompletedTask;

            try
            {
                File.Delete(_lockFilePath);
                _logger.LogInformation("Lock released: {LockFile}", _lockFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to release lock: {LockFile}", _lockFilePath);
            }

            _disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
```

---

### Threat 3: Binding Injection (SQL Injection in Binding Queries)

**Risk:** Attacker-controlled worktree path or chat ID contains SQL injection payload, allowing unauthorized binding manipulation or data exfiltration.

**Attack Scenario:**
```bash
# Attacker creates worktree with malicious path
mkdir "/tmp/worktree'; DROP TABLE bindings; --"
cd "/tmp/worktree'; DROP TABLE bindings; --"
acode chat new "Malicious chat"

# Vulnerable code constructs SQL directly from path:
# SELECT * FROM bindings WHERE worktree_path = '/tmp/worktree'; DROP TABLE bindings; --'
# Result: bindings table deleted!
```

**Mitigation (ParameterizedBindingRepository - 55 lines):**

```csharp
// src/AgenticCoder.Infrastructure/Concurrency/ParameterizedBindingRepository.cs
namespace AgenticCoder.Infrastructure.Concurrency;

public sealed class ParameterizedBindingRepository : IBindingRepository
{
    private readonly IDbConnection _connection;
    private readonly ILogger<ParameterizedBindingRepository> _logger;

    public ParameterizedBindingRepository(
        IDbConnection connection,
        ILogger<ParameterizedBindingRepository> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task<WorktreeBinding?> GetByWorktreeAsync(
        WorktreeId worktreeId,
        CancellationToken ct)
    {
        // ALWAYS use parameterized queries - NEVER string concatenation
        const string sql = @"
            SELECT worktree_id, chat_id, created_at
            FROM worktree_bindings
            WHERE worktree_id = @WorktreeId";

        // Parameter binding prevents SQL injection
        var parameters = new { WorktreeId = worktreeId.Value };

        var row = await _connection.QuerySingleOrDefaultAsync<BindingRow>(sql, parameters);

        if (row is null)
        {
            return null;
        }

        return new WorktreeBinding(
            WorktreeId.From(row.WorktreeId),
            ChatId.From(row.ChatId),
            row.CreatedAt);
    }

    public async Task CreateAsync(WorktreeBinding binding, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO worktree_bindings (worktree_id, chat_id, created_at)
            VALUES (@WorktreeId, @ChatId, @CreatedAt)";

        // All values parameterized
        var parameters = new
        {
            WorktreeId = binding.WorktreeId.Value,
            ChatId = binding.ChatId.Value,
            CreatedAt = binding.CreatedAt
        };

        await _connection.ExecuteAsync(sql, parameters);

        _logger.LogInformation(
            "Binding created: Worktree={WorktreeId}, Chat={ChatId}",
            binding.WorktreeId, binding.ChatId);
    }

    public async Task DeleteAsync(WorktreeId worktreeId, CancellationToken ct)
    {
        const string sql = "DELETE FROM worktree_bindings WHERE worktree_id = @WorktreeId";
        var parameters = new { WorktreeId = worktreeId.Value };
        await _connection.ExecuteAsync(sql, parameters);
    }

    private sealed record BindingRow(string WorktreeId, string ChatId, DateTimeOffset CreatedAt);
}
```

---

### Threat 4: Lock Directory Traversal

**Risk:** Attacker crafts malicious worktree ID containing `../` path traversal sequences, writing lock files outside `.agent/locks/` directory, potentially overwriting critical files.

**Attack Scenario:**
```bash
# Attacker provides malicious worktree ID
worktree_id = "../../.ssh/authorized_keys"

# Vulnerable code constructs lock path:
# lock_path = ".agent/locks/" + worktree_id + ".lock"
# Result: ".agent/locks/../../.ssh/authorized_keys.lock"
# Resolves to: ".ssh/authorized_keys.lock" (OUTSIDE LOCKS DIR!)
# Could overwrite SSH authorized_keys!
```

**Mitigation (SafeLockPathResolver - 50 lines):**

```csharp
// src/AgenticCoder.Infrastructure/Concurrency/SafeLockPathResolver.cs
namespace AgenticCoder.Infrastructure.Concurrency;

public sealed class SafeLockPathResolver
{
    private static readonly HashSet<char> InvalidChars = new(Path.GetInvalidFileNameChars())
    {
        '/', '\\', '.', ' '  // Additional dangerous characters
    };

    private readonly string _locksDirectory;
    private readonly ILogger<SafeLockPathResolver> _logger;

    public SafeLockPathResolver(string workspaceRoot, ILogger<SafeLockPathResolver> logger)
    {
        _locksDirectory = Path.Combine(workspaceRoot, ".agent", "locks");
        _logger = logger;

        // Ensure locks directory exists
        Directory.CreateDirectory(_locksDirectory);
    }

    public string GetLockFilePath(WorktreeId worktreeId)
    {
        // Sanitize worktree ID: Remove all invalid/dangerous characters
        var sanitized = SanitizeWorktreeId(worktreeId.Value);

        // Construct path
        var lockFileName = $"{sanitized}.lock";
        var lockFilePath = Path.Combine(_locksDirectory, lockFileName);

        // CRITICAL: Verify resolved path is still within locks directory
        var resolvedPath = Path.GetFullPath(lockFilePath);
        var expectedPrefix = Path.GetFullPath(_locksDirectory) + Path.DirectorySeparatorChar;

        if (!resolvedPath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(
                "Path traversal detected! Worktree={Worktree}, Resolved={Resolved}, Expected prefix={Expected}",
                worktreeId, resolvedPath, expectedPrefix);
            throw new SecurityException($"Invalid worktree ID: Path traversal detected");
        }

        return resolvedPath;
    }

    private static string SanitizeWorktreeId(string worktreeId)
    {
        // Remove all invalid filename characters
        var sanitized = new StringBuilder(worktreeId.Length);

        foreach (var c in worktreeId)
        {
            if (!InvalidChars.Contains(c))
            {
                sanitized.Append(c);
            }
        }

        // Ensure not empty after sanitization
        if (sanitized.Length == 0)
        {
            throw new ArgumentException("Worktree ID becomes empty after sanitization", nameof(worktreeId));
        }

        return sanitized.ToString();
    }
}
```

---

### Threat 5: Binding Cache Poisoning

**Risk:** Attacker exploits caching mechanism to serve stale or malicious binding data, causing commands to operate on wrong chat context, potentially leaking sensitive data across contexts.

**Attack Scenario:**
```bash
# User creates binding in worktree-A to chat-A
cd worktree-A
acode chat new "Public API Docs"

# Binding cached: worktree-A -> chat-A

# Attacker purges chat-A
acode chat purge <chat-A-id> --force

# User switches to worktree-A
cd worktree-A
acode run "Show me the database passwords"
# CACHE HIT: Still thinks chat-A exists!
# Creates new chat-A with same ID (ULID collision unlikely but cache doesn't validate)
# Response contains passwords, but cache points to wrong/deleted chat
# Data leakage!
```

**Mitigation (ValidatedBindingCache - 65 lines):**

```csharp
// src/AgenticCoder.Infrastructure/Concurrency/ValidatedBindingCache.cs
namespace AgenticCoder.Infrastructure.Concurrency;

public sealed class ValidatedBindingCache : IBindingService
{
    private readonly IBindingRepository _repository;
    private readonly IChatRepository _chatRepository;
    private readonly MemoryCache _cache;
    private readonly ILogger<ValidatedBindingCache> _logger;

    public ValidatedBindingCache(
        IBindingRepository repository,
        IChatRepository chatRepository,
        ILogger<ValidatedBindingCache> logger)
    {
        _repository = repository;
        _chatRepository = chatRepository;
        _logger = logger;

        // Cache expires after 30 seconds to prevent stale data
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 100,  // Max 100 bindings cached
            ExpirationScanFrequency = TimeSpan.FromSeconds(10)
        });
    }

    public async Task<ChatId?> GetBoundChatAsync(
        WorktreeId worktreeId,
        CancellationToken ct)
    {
        var cacheKey = $"binding:{worktreeId.Value}";

        // Check cache
        if (_cache.TryGetValue<ChatId>(cacheKey, out var cachedChatId))
        {
            // VALIDATE: Ensure chat still exists before returning cached value
            var chat = await _chatRepository.GetByIdAsync(cachedChatId, includeDeleted: false, ct);

            if (chat is null)
            {
                _logger.LogWarning(
                    "Cached binding references deleted chat. Removing from cache. Worktree={Worktree}, Chat={Chat}",
                    worktreeId, cachedChatId);

                // Invalidate cache entry
                _cache.Remove(cacheKey);

                // Remove binding from database
                await _repository.DeleteAsync(worktreeId, ct);

                return null;
            }

            // Cache valid
            return cachedChatId;
        }

        // Cache miss - query database
        var binding = await _repository.GetByWorktreeAsync(worktreeId, ct);

        if (binding is null)
        {
            return null;
        }

        // VALIDATE: Ensure chat exists before caching
        var boundChat = await _chatRepository.GetByIdAsync(binding.ChatId, includeDeleted: false, ct);

        if (boundChat is null)
        {
            _logger.LogWarning(
                "Binding references deleted chat. Removing binding. Worktree={Worktree}, Chat={Chat}",
                worktreeId, binding.ChatId);

            await _repository.DeleteAsync(worktreeId, ct);
            return null;
        }

        // Cache valid binding with expiration
        _cache.Set(cacheKey, binding.ChatId, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30),
            Size = 1
        });

        return binding.ChatId;
    }

    public async Task InvalidateAsync(WorktreeId worktreeId, CancellationToken ct)
    {
        var cacheKey = $"binding:{worktreeId.Value}";
        _cache.Remove(cacheKey);

        _logger.LogDebug("Binding cache invalidated for worktree {Worktree}", worktreeId);

        await Task.CompletedTask;
    }
}
```

---

## Out of Scope

The following items are explicitly excluded from Task 049.c:

- **Data model** - Task 049.a
- **CLI commands** - Task 049.b
- **Search** - Task 049.d
- **Retention** - Task 049.e
- **Sync** - Task 049.f
- **Distributed locking** - Local only
- **Cross-machine binding** - Single machine
- **Real-time sync** - Async only
- **Worktree creation** - Git operations
- **Chat merging** - Not supported

---

## Assumptions

### Technical Assumptions

- ASM-001: Git worktrees provide isolated working directories
- ASM-002: File-based or database locking prevents concurrent modifications
- ASM-003: Worktree detection is reliable across platforms
- ASM-004: Session binding uses process-level identifiers
- ASM-005: Lock release handles process crashes

### Behavioral Assumptions

- ASM-006: Each worktree can have its own active chat
- ASM-007: Chat binding is optional but useful for context
- ASM-008: Multiple terminals can operate on different chats
- ASM-009: Concurrent reads are safe without locking
- ASM-010: Write locks are short-duration

### Dependency Assumptions

- ASM-011: Task 049.a data model supports worktree references
- ASM-012: Task 022 git operations provide worktree detection
- ASM-013: Task 011 session state tracks active chat

### Safety Assumptions

- ASM-014: Orphaned locks are detected and cleaned
- ASM-015: Lock timeout prevents indefinite blocking
- ASM-016: Concurrent modification errors are clear

---

## Functional Requirements

### Worktree Binding

- FR-001: Chat MAY be bound to worktree
- FR-002: Binding MUST be one-to-one
- FR-003: Worktree MAY have one bound chat
- FR-004: Binding MUST persist
- FR-005: Binding MUST survive restart

### Auto-Binding

- FR-006: `chat new` in worktree MUST auto-bind
- FR-007: `chat new --no-bind` MUST skip
- FR-008: Unbound chat MAY be bound later
- FR-009: `chat bind <id>` MUST work

### Unbinding

- FR-010: `chat unbind` MUST work
- FR-011: Unbind MUST preserve chat
- FR-012: Unbind MUST clear worktree reference
- FR-013: Chat MAY be rebound

### Context Switching

- FR-014: `cd` to worktree MUST switch context
- FR-015: Active chat MUST follow worktree
- FR-016: Switch MUST be instant
- FR-017: Switch MUST log event

### Run Isolation

- FR-018: Run MUST belong to one chat
- FR-019: Run MUST record worktree
- FR-020: Run context MUST NOT bleed
- FR-021: Messages MUST NOT cross runs

### Concurrency Locking

- FR-022: One run per worktree at a time
- FR-023: Lock MUST be acquired before run
- FR-024: Lock MUST be released after run
- FR-025: Lock timeout MUST be configurable

### Lock Behavior

- FR-026: Blocked request MUST queue OR error
- FR-027: Default MUST be error
- FR-028: `--wait` MUST queue
- FR-029: Queue timeout MUST be configurable

### Lock Storage

- FR-030: Locks MUST use file system
- FR-031: Lock files MUST be in .agent/
- FR-032: Stale locks MUST be detected
- FR-033: Stale lock threshold: 5 minutes

### Multi-Session

- FR-034: Multiple terminals MUST work
- FR-035: Each session MUST see consistent state
- FR-036: Lock conflicts MUST be clear

### Binding Queries

- FR-037: Get chat by worktree MUST work
- FR-038: Get worktree by chat MUST work
- FR-039: List bound pairs MUST work
- FR-040: List unbound chats MUST work

### Cascade Handling

- FR-041: Deleted worktree MUST unbind chat
- FR-042: Purged chat MUST unbind worktree
- FR-043: Cascade MUST be atomic

### Status Commands

- FR-044: `acode status` MUST show binding
- FR-045: Status MUST show lock state
- FR-046: Status MUST show queued count

---

## Non-Functional Requirements

### Performance

- **NFR-001**: Context switch (worktree change → chat activation) MUST complete in < 50ms to feel instantaneous
- **NFR-002**: Lock acquisition (check + atomic write + verify) MUST complete in < 10ms under normal conditions
- **NFR-003**: Lock release (file deletion) MUST complete in < 5ms
- **NFR-004**: Binding query (lookup by worktree ID) MUST complete in < 5ms with indexed database
- **NFR-005**: Binding cache MUST achieve > 95% hit rate for active worktrees to minimize database queries
- **NFR-006**: Stale lock detection scan MUST complete in < 200ms for up to 100 lock files
- **NFR-007**: Git worktree detection MUST complete in < 100ms including parsing `.git` file and worktree list

### Reliability

- **NFR-008**: Bindings MUST NOT be lost during crash, power failure, or unexpected termination
- **NFR-009**: Orphaned locks MUST be automatically cleaned within stale threshold (default 5 minutes)
- **NFR-010**: Lock files MUST be atomically created using rename operation (no partial states)
- **NFR-011**: System MUST handle process crashes gracefully without leaving corrupted state
- **NFR-012**: Lock release on `IAsyncDisposable.DisposeAsync()` MUST handle exceptions without throwing

### Consistency

- **NFR-013**: One-to-one binding constraint MUST be enforced at database level (unique constraint)
- **NFR-014**: Race conditions between concurrent binding attempts MUST result in exactly one winner, others receive clear error
- **NFR-015**: Stale locks MUST be cleaned up within 10 seconds of detection by any process
- **NFR-016**: Binding cache MUST be invalidated within 100ms of database change
- **NFR-017**: Lock state MUST be consistent across all terminals in same workspace

### Usability

- **NFR-018**: Lock conflict messages MUST include: process ID, hostname, terminal, lock age, suggestions
- **NFR-019**: Current context (worktree, bound chat, lock state) MUST be visible in `acode status` output
- **NFR-020**: Unbinding MUST be a single command (`acode chat unbind`) with confirmation for safety
- **NFR-021**: `--wait` flag MUST show progress indicator while waiting for lock (spinning, elapsed time)
- **NFR-022**: Worktree detection failures MUST provide clear explanation (not in git repo, worktree not initialized)

### Security

- **NFR-023**: Lock files MUST have restrictive permissions (600 on Unix) to prevent tampering by other users
- **NFR-024**: Lock file paths MUST be validated to prevent path traversal attacks
- **NFR-025**: Binding repository MUST use parameterized queries to prevent SQL injection
- **NFR-026**: Lock ownership MUST be verified after creation to prevent TOCTOU attacks

### Scalability

- **NFR-027**: System MUST support 100+ concurrent worktrees per workspace without performance degradation
- **NFR-028**: Binding table MUST remain performant with 10,000+ historical bindings (indexed queries)
- **NFR-029**: Lock directory MUST handle 50+ concurrent lock files efficiently

### Maintainability

- **NFR-030**: All lock operations MUST be logged at DEBUG level for troubleshooting
- **NFR-031**: Lock conflicts MUST be logged at WARNING level with full context
- **NFR-032**: Unit test coverage for concurrency code MUST exceed 85%

---

## User Manual Documentation

### Overview

Multi-chat concurrency enables working on multiple features simultaneously. Each Git worktree can have its own conversation context, automatically switching as you navigate.

### Quick Start

```bash
# Create worktree with bound chat
$ git worktree add ../feature-auth feature/auth
$ cd ../feature-auth
$ acode chat new "Auth Feature"
Created chat: chat_abc123 (bound to feature/auth)

# Work in worktree
$ acode run "Design login flow"
# Uses auth chat context

# Switch worktree, switch context
$ cd ../feature-payments
$ acode chat new "Payments Feature"
Created chat: chat_def456 (bound to feature/payments)

# Context follows directory
$ cd ../feature-auth
$ acode run "Continue login"
# Back to auth chat context
```

### Binding Commands

```bash
# Bind chat to current worktree
$ acode chat bind chat_abc123
Bound chat_abc123 to: feature/auth

# Unbind chat from worktree
$ acode chat unbind
Unbound chat from: feature/auth

# Create without binding
$ acode chat new --no-bind "General Discussion"
Created chat: chat_xyz789 (unbound)

# View binding status
$ acode chat bindings
Worktree          Bound Chat
feature/auth      chat_abc123 (Auth Feature)
feature/payments  chat_def456 (Payments Feature)
main              (no binding)
```

### Concurrency

```bash
# Terminal 1
$ cd feature/auth
$ acode run "Long operation"
Running...

# Terminal 2 (same worktree)
$ cd feature/auth
$ acode run "Another request"
ERROR: Worktree is locked by another session.
Lock held since: 30s ago
Use --wait to queue, or work in another worktree.

# With --wait
$ acode run --wait "Another request"
Waiting for lock... (timeout: 5m)
Lock acquired!
Running...
```

### Status

```bash
$ acode status

Workspace: /projects/myapp
────────────────────────────────────
Current Worktree: feature/auth
Active Chat: chat_abc123 (Auth Feature)
Binding: Bound

Run Status: Idle
Lock: Available

Other Sessions:
  Terminal 2: Running (feature/payments)
```

### Configuration

```yaml
# .agent/config.yml
concurrency:
  # Lock behavior when busy
  on_busy: error  # or 'wait'
  
  # Wait timeout
  wait_timeout_seconds: 300
  
  # Stale lock detection
  stale_lock_seconds: 300
  
  # Auto-bind new chats
  auto_bind: true
```

### Troubleshooting

#### Stuck Lock

**Problem:** Lock not releasing

**Solution:**
```bash
# Check lock status
$ acode lock status
Lock file: .agent/locks/worktree_feature_auth.lock
Held by: PID 12345
Since: 10m ago
Status: STALE (process not found)

# Force release
$ acode lock release --force
Lock released.
```

#### Wrong Context

**Problem:** Commands using wrong chat

**Solution:**
1. Check current worktree: `pwd`
2. Check binding: `acode chat bindings`
3. Explicitly open: `acode chat open <id>`

#### Orphaned Binding

**Problem:** Binding to deleted worktree

**Solution:**
```bash
$ acode chat bindings --cleanup
Found 1 orphaned binding:
  chat_old123 → deleted_worktree (not found)
  
Clean up? [Y/n] y
Cleaned 1 orphaned binding.
```

---

## Acceptance Criteria

### Binding Commands (AC-001 to AC-020)

- [ ] AC-001: `acode chat bind <chat-id>` creates binding between current worktree and specified chat
- [ ] AC-002: Bind command validates chat exists before creating binding
- [ ] AC-003: Bind command fails with clear error if worktree already has a binding
- [ ] AC-004: Bind command fails with clear error if chat is already bound to different worktree
- [ ] AC-005: `acode chat unbind` removes binding from current worktree
- [ ] AC-006: Unbind prompts for confirmation before removing binding
- [ ] AC-007: `acode chat unbind --force` bypasses confirmation prompt
- [ ] AC-008: Unbind succeeds silently if no binding exists (idempotent)
- [ ] AC-009: `acode chat bindings` lists all worktree-to-chat bindings in workspace
- [ ] AC-010: Bindings list shows worktree path, chat ID, chat title, and creation timestamp
- [ ] AC-011: `acode chat bindings --json` outputs binding list as valid JSON
- [ ] AC-012: `acode chat new "Title"` in worktree auto-binds by default
- [ ] AC-013: `acode chat new --no-bind "Title"` creates chat without binding
- [ ] AC-014: Auto-bind displays message confirming binding creation
- [ ] AC-015: Binding persists after acode process exits
- [ ] AC-016: Binding persists after machine reboot
- [ ] AC-017: Binding stored in workspace SQLite database with foreign key to chats table
- [ ] AC-018: Database has unique constraint on worktree_id column (one-to-one enforcement)
- [ ] AC-019: Binding query by worktree ID returns result in < 5ms
- [ ] AC-020: Binding cache achieves > 95% hit rate for repeated queries

### Context Resolution (AC-021 to AC-035)

- [ ] AC-021: Running any `acode` command in bound worktree automatically uses bound chat
- [ ] AC-022: Context switch occurs on each command execution, not requiring `cd` hook
- [ ] AC-023: Context switch latency < 50ms (measured from command start to chat activation)
- [ ] AC-024: `acode status` displays current worktree ID
- [ ] AC-025: `acode status` displays bound chat ID and title (or "unbound")
- [ ] AC-026: `acode status` displays lock status (locked/available)
- [ ] AC-027: If worktree has no binding, system falls back to global/manual chat selection
- [ ] AC-028: `acode chat open <id>` temporarily overrides bound chat for current session
- [ ] AC-029: Session override resets when terminal closes or explicit `acode chat open` of bound chat
- [ ] AC-030: Git worktree detection works for standard worktree layout
- [ ] AC-031: Git worktree detection works for worktrees with `.git` file (not directory)
- [ ] AC-032: Worktree detection fails gracefully with message if not in git repository
- [ ] AC-033: Worktree detection fails gracefully if git command not available
- [ ] AC-034: Worktree ID generated consistently from worktree path (deterministic hash)
- [ ] AC-035: Same worktree path always resolves to same worktree ID

### Lock Acquisition and Release (AC-036 to AC-055)

- [ ] AC-036: Lock automatically acquired when `acode run` starts
- [ ] AC-037: Lock acquisition creates lock file in `.agent/locks/` directory
- [ ] AC-038: Lock file contains JSON with process ID, timestamp, hostname, terminal
- [ ] AC-039: Lock file has permissions 600 on Unix systems
- [ ] AC-040: Lock acquisition fails immediately if lock file already exists and not stale
- [ ] AC-041: Lock acquisition failure returns error ACODE-CONC-001 with details
- [ ] AC-042: Lock automatically released when `acode run` completes (success or error)
- [ ] AC-043: Lock release deletes lock file from filesystem
- [ ] AC-044: Lock release on dispose handles exceptions without re-throwing
- [ ] AC-045: Lock acquisition latency < 10ms (file create + verify)
- [ ] AC-046: Lock release latency < 5ms (file delete)
- [ ] AC-047: Lock acquisition uses atomic rename (write temp, rename to final)
- [ ] AC-048: Lock ownership verified after creation (re-read file, check process ID)
- [ ] AC-049: `acode run --wait` queues if lock unavailable instead of erroring
- [ ] AC-050: `--wait` polls lock every 2 seconds until available
- [ ] AC-051: `--wait` displays progress indicator (elapsed time, holder info)
- [ ] AC-052: `--wait` has configurable timeout (default 5 minutes)
- [ ] AC-053: `--wait` timeout returns error ACODE-CONC-002 with elapsed time
- [ ] AC-054: `acode unlock --force` deletes lock file regardless of owner
- [ ] AC-055: Force unlock logs WARNING level entry with lock details

### Stale Lock Handling (AC-056 to AC-065)

- [ ] AC-056: Lock is considered stale if timestamp > 5 minutes old
- [ ] AC-057: Stale threshold is configurable via `workspace.lock_timeout_seconds`
- [ ] AC-058: Stale lock detected on next acquisition attempt
- [ ] AC-059: Stale lock automatically deleted before new lock creation
- [ ] AC-060: Stale lock removal logged at WARNING level
- [ ] AC-061: Process ID check (is holder still running) used for stale detection
- [ ] AC-062: Hostname check prevents deleting locks from different machines (NFS safety)
- [ ] AC-063: `acode lock status` displays lock state, age, holder info
- [ ] AC-064: `acode lock status` indicates if lock is stale
- [ ] AC-065: `acode lock cleanup` manually triggers stale lock scan and removal

### Multi-Session Scenarios (AC-066 to AC-080)

- [ ] AC-066: Two terminals in different worktrees can run commands simultaneously
- [ ] AC-067: Each terminal acquires lock on its respective worktree
- [ ] AC-068: Lock files created in separate directories for each worktree
- [ ] AC-069: Two terminals in same worktree: second gets immediate BUSY error
- [ ] AC-070: BUSY error message includes holder process ID, hostname, terminal
- [ ] AC-071: BUSY error message includes lock age
- [ ] AC-072: BUSY error message suggests `--wait` option
- [ ] AC-073: Terminal-1 completes, Terminal-2 with `--wait` acquires lock
- [ ] AC-074: Wait acquisition latency < poll interval (2 seconds) after release
- [ ] AC-075: Lock holder crash leaves lock file (will become stale)
- [ ] AC-076: After crash, other terminal waits 5 minutes then acquires
- [ ] AC-077: Multiple `--wait` requests on same lock: first released gets first acquisition
- [ ] AC-078: No deadlocks possible (single lock per worktree, no multi-lock scenarios)
- [ ] AC-079: Session state is isolated (Terminal-1's chat open doesn't affect Terminal-2)
- [ ] AC-080: Lock contention logged at INFO level with session identifiers

### Cascade Operations (AC-081 to AC-090)

- [ ] AC-081: Deleting worktree (git worktree remove) unbinds associated chat
- [ ] AC-082: Unbind on worktree delete is handled by binding validation (chat remains)
- [ ] AC-083: Purging chat cascade-deletes binding record
- [ ] AC-084: Soft-delete (acode chat delete) does NOT affect binding
- [ ] AC-085: Restored chat retains original binding
- [ ] AC-086: Binding cleanup detects orphaned bindings (worktree doesn't exist)
- [ ] AC-087: `acode chat bindings --cleanup` removes orphaned bindings
- [ ] AC-088: Orphan cleanup prompts for confirmation
- [ ] AC-089: Orphan cleanup logged at INFO level
- [ ] AC-090: Cascade delete is atomic (all or nothing)

### Security (AC-091 to AC-098)

- [ ] AC-091: Lock file path validated to prevent path traversal (no `../`)
- [ ] AC-092: Worktree ID sanitized before use in file paths
- [ ] AC-093: SQL queries use parameterized statements (no string concatenation)
- [ ] AC-094: Lock file permissions prevent other users from reading/modifying
- [ ] AC-095: Binding cache validates chat still exists before returning cached value
- [ ] AC-096: Invalid binding (to deleted chat) is automatically removed
- [ ] AC-097: Hostname mismatch in lock file logged as security warning
- [ ] AC-098: Lock files are not world-readable (contain process metadata)

### Cross-Cutting (AC-099 to AC-108)

- [ ] AC-099: All binding commands support --help flag
- [ ] AC-100: All binding commands return appropriate exit codes
- [ ] AC-101: Error messages include error codes (ACODE-CONC-xxx)
- [ ] AC-102: Lock events logged at DEBUG level by default
- [ ] AC-103: Lock conflicts logged at WARNING level
- [ ] AC-104: Context switches logged at INFO level
- [ ] AC-105: Unit test coverage > 85% for concurrency code
- [ ] AC-106: Integration tests cover multi-session scenarios
- [ ] AC-107: E2E tests cover full bind-run-unbind lifecycle
- [ ] AC-108: Performance benchmarks validate latency targets

---

## Best Practices

### Worktree Binding

- **BP-001: Bind early** - Associate chat with worktree when creating for focused work
- **BP-002: One chat per worktree** - Avoid confusion by keeping 1:1 relationship
- **BP-003: Unbind on completion** - Release bindings when work is done
- **BP-004: Document binding purpose** - Use chat name to indicate worktree relationship

### Concurrency Management

- **BP-005: Short lock duration** - Hold write locks only during actual writes
- **BP-006: Read-heavy design** - Optimize for concurrent reads
- **BP-007: Lock timeout handling** - Fail gracefully if lock unavailable
- **BP-008: Orphan lock cleanup** - Detect and clean stale locks

### Multi-Terminal Usage

- **BP-009: Clear active chat display** - Show which chat is active in prompt or status
- **BP-010: Avoid cross-terminal edits** - One terminal writes to a chat at a time
- **BP-011: Session isolation** - Each terminal session has independent context
- **BP-012: Status refresh** - Update status after context changes

---

## Troubleshooting

### Issue 1: Lock Acquisition Failed (BUSY Error)

**Symptom:** Operation fails with "Cannot acquire lock" or error code ACODE-CONC-001. Terminal displays "Worktree is locked by another process."

**Causes:**
- Another terminal session is actively running a command in the same worktree
- Previous process crashed and left a lock file
- Lock file has incorrect permissions

**Solution:**
1. Check lock status: `acode lock status`
2. If lock is held by active process, wait for it to complete or use `--wait` flag
3. If lock is stale (holder process dead), wait 5 minutes for automatic cleanup
4. For emergencies, force-unlock: `acode unlock --force`
5. Verify no other `acode` processes: `Get-Process -Name acode` (PowerShell) or `pgrep acode` (Unix)

---

### Issue 2: Worktree Not Detected

**Symptom:** Commands say "Not in a worktree" or "Worktree not found" when you are in a valid Git worktree.

**Causes:**
- Git worktree not properly initialized
- `.git` file in worktree directory is missing or corrupted
- Current directory is not within a worktree path
- Git executable not found in PATH

**Solution:**
1. Verify worktree exists: `git worktree list`
2. Check `.git` file exists in worktree root: `cat .git` (should contain `gitdir: /path/to/main/.git/worktrees/...`)
3. Navigate to worktree root directory (not a subdirectory)
4. Re-add worktree if corrupted: `git worktree remove <path>` then `git worktree add <path> <branch>`
5. Verify Git is installed and in PATH: `git --version`

---

### Issue 3: Binding Mismatch (Wrong Chat Active)

**Symptom:** Wrong chat is active in worktree. Commands operate on unexpected conversation context.

**Causes:**
- Binding cache is stale (rare, cache should auto-invalidate)
- Manual `acode chat open <id>` overrode bound chat
- Binding was changed in another terminal
- Chat was deleted but binding remains

**Solution:**
1. Check current binding: `acode chat bindings`
2. Clear and rebind: `acode chat unbind` then `acode chat bind <correct-chat-id>`
3. If bound chat was deleted, binding should auto-clear; if not, unbind manually
4. Restart terminal to clear any session state
5. Verify database integrity: `sqlite3 .agent/data/workspace.db "SELECT * FROM worktree_bindings;"`

---

### Issue 4: Orphaned Lock File (Stale Lock Not Cleaning Up)

**Symptom:** Lock persists beyond 5-minute stale threshold. New lock acquisitions still fail.

**Causes:**
- Stale lock cleanup task not running
- Lock file has incorrect timestamp format
- File system clock skew (rare)
- Lock file permissions prevent deletion

**Solution:**
1. Check lock file age: `acode lock status` - should show "STALE" if > 5 minutes
2. Verify system time is correct
3. Check lock file permissions: should be 600 (owner read/write only)
4. Manually delete lock file: `Remove-Item .agent/locks/*.lock` (PowerShell) or `rm .agent/locks/*.lock` (Unix)
5. If recurring, check for background processes not properly releasing locks

---

### Issue 5: Binding One-to-One Constraint Violation

**Symptom:** Error "Worktree is already bound to chat X" or "Chat is already bound to worktree Y" when trying to create binding.

**Causes:**
- Attempting to bind worktree that already has a bound chat
- Attempting to bind chat that is already bound to different worktree
- Database constraint violation (unique index on worktree_id)

**Solution:**
1. Check existing bindings: `acode chat bindings`
2. Unbind existing binding before creating new one: `acode chat unbind`
3. To rebind same chat to different worktree, unbind from original first
4. For chat bound elsewhere: `acode chat unbind --chat <chat-id>` (unbinds from any worktree)

---

### Issue 6: Context Switch Not Triggering

**Symptom:** Changing directories between worktrees doesn't automatically switch chat context. Still using previous worktree's chat.

**Causes:**
- Shell hook not installed (context detection requires integration)
- Worktree binding doesn't exist
- Running in non-interactive mode (CI/CD)
- Session state cached incorrectly

**Solution:**
1. Context switch is triggered on `acode` commands, not `cd` alone - run any acode command after cd
2. Verify binding exists: `acode chat bindings`
3. Manually switch: `acode chat open <bound-chat-id>`
4. Check `acode status` to see current detected worktree and active chat
5. If using CI/CD, explicitly bind before running: `acode chat bind <id>` in worktree

---

### Issue 7: Lock Wait Timeout

**Symptom:** `acode run --wait` times out with "Timeout waiting for lock after X seconds."

**Causes:**
- Lock holder is running very long operation (>5 min default timeout)
- Lock holder process is hanging/stuck
- Wait timeout too short for operation

**Solution:**
1. Check who holds lock: `acode lock status` shows PID, hostname, terminal
2. Increase wait timeout: `acode run --wait --timeout 600` (10 minutes)
3. If lock holder is stuck, terminate it or force-unlock: `acode unlock --force`
4. Consider if operation can be run in different worktree to avoid waiting

---

### Issue 8: Binding Deleted After Chat Purge

**Symptom:** After purging a chat, the worktree shows as unbound. Subsequent commands use global/manual chat selection.

**Causes:**
- This is expected behavior - cascade delete removes binding when chat is purged
- Binding references deleted chat (validation should have caught this)

**Solution:**
1. This is intentional - purging chat cascades to unbind worktree
2. Create new chat and rebind: `acode chat new "New Chat" && acode chat bind <new-id>`
3. Or use auto-bind: `acode chat new "New Chat"` (auto-binds if in worktree)
4. To preserve binding, use `acode chat delete` (soft-delete) instead of `acode chat purge`

---

## Testing Requirements

### Unit Tests - BindingTests.cs (Complete Implementation)

```csharp
// Tests/Unit/Concurrency/BindingTests.cs
using Xunit;
using FluentAssertions;
using AgenticCoder.Domain.Concurrency;
using AgenticCoder.Domain.Conversation;

namespace AgenticCoder.Tests.Unit.Concurrency;

public sealed class BindingTests
{
    [Fact]
    public async Task Should_Bind_Chat_To_Worktree()
    {
        // Arrange
        var worktreeId = WorktreeId.From("worktree-01HKABC");
        var chatId = ChatId.NewId();
        var repository = new InMemoryBindingRepository();

        // Act
        var binding = WorktreeBinding.Create(worktreeId, chatId);
        await repository.CreateAsync(binding, CancellationToken.None);

        // Assert
        var retrieved = await repository.GetByWorktreeAsync(worktreeId, CancellationToken.None);
        retrieved.Should().NotBeNull();
        retrieved!.WorktreeId.Should().Be(worktreeId);
        retrieved.ChatId.Should().Be(chatId);
        retrieved.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Should_Unbind_Chat()
    {
        // Arrange
        var worktreeId = WorktreeId.From("worktree-01HKABC");
        var chatId = ChatId.NewId();
        var repository = new InMemoryBindingRepository();

        var binding = WorktreeBinding.Create(worktreeId, chatId);
        await repository.CreateAsync(binding, CancellationToken.None);

        // Act
        await repository.DeleteAsync(worktreeId, CancellationToken.None);

        // Assert
        var retrieved = await repository.GetByWorktreeAsync(worktreeId, CancellationToken.None);
        retrieved.Should().BeNull("binding should be deleted");
    }

    [Fact]
    public async Task Should_Enforce_OneToOne_Binding()
    {
        // Arrange
        var worktreeId = WorktreeId.From("worktree-01HKABC");
        var chatId1 = ChatId.NewId();
        var chatId2 = ChatId.NewId();
        var repository = new InMemoryBindingRepository();

        var binding1 = WorktreeBinding.Create(worktreeId, chatId1);
        await repository.CreateAsync(binding1, CancellationToken.None);

        // Act
        var binding2 = WorktreeBinding.Create(worktreeId, chatId2);
        var act = async () => await repository.CreateAsync(binding2, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already bound*");
    }

    [Fact]
    public async Task Should_Persist_Binding()
    {
        // Arrange
        var worktreeId = WorktreeId.From("worktree-01HKABC");
        var chatId = ChatId.NewId();
        var binding = WorktreeBinding.Create(worktreeId, chatId);

        // Act - Simulate restart by creating new repository instance
        var repository1 = new SqliteBindingRepository(":memory:");
        await repository1.CreateAsync(binding, CancellationToken.None);

        var repository2 = new SqliteBindingRepository(":memory:");  // Same connection string
        var retrieved = await repository2.GetByWorktreeAsync(worktreeId, CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull("binding should persist across sessions");
        retrieved!.ChatId.Should().Be(chatId);
    }
}
```

### Unit Tests - LockTests.cs (Complete Implementation)

```csharp
// Tests/Unit/Concurrency/LockTests.cs
using Xunit;
using FluentAssertions;
using AgenticCoder.Infrastructure.Concurrency;

namespace AgenticCoder.Tests.Unit.Concurrency;

public sealed class LockTests
{
    [Fact]
    public async Task Should_Acquire_Lock()
    {
        // Arrange
        var worktreeId = WorktreeId.From("worktree-01HKABC");
        var lockService = new AtomicFileLockService(new NullLogger<AtomicFileLockService>());

        // Act
        await using var lockHandle = await lockService.AcquireAsync(
            worktreeId,
            timeout: null,
            CancellationToken.None);

        // Assert
        lockHandle.Should().NotBeNull();
        var lockFile = Path.Combine(".agent", "locks", $"{worktreeId.Value}.lock");
        File.Exists(lockFile).Should().BeTrue("lock file should exist");

        var lockData = JsonSerializer.Deserialize<LockData>(await File.ReadAllTextAsync(lockFile));
        lockData!.ProcessId.Should().Be(Environment.ProcessId);
    }

    [Fact]
    public async Task Should_Release_Lock_On_Dispose()
    {
        // Arrange
        var worktreeId = WorktreeId.From("worktree-01HKABC");
        var lockService = new AtomicFileLockService(new NullLogger<AtomicFileLockService>());
        var lockFile = Path.Combine(".agent", "locks", $"{worktreeId.Value}.lock");

        // Act
        var lockHandle = await lockService.AcquireAsync(worktreeId, null, CancellationToken.None);
        File.Exists(lockFile).Should().BeTrue("lock acquired");

        await lockHandle.DisposeAsync();

        // Assert
        File.Exists(lockFile).Should().BeFalse("lock should be released after dispose");
    }

    [Fact]
    public async Task Should_Detect_Stale_Lock()
    {
        // Arrange
        var worktreeId = WorktreeId.From("worktree-01HKABC");
        var lockFile = Path.Combine(".agent", "locks", $"{worktreeId.Value}.lock");

        // Create stale lock (timestamp 10 minutes ago, dead process ID)
        var staleLockData = new LockData(
            ProcessId: 99999,  // Unlikely to exist
            LockedAt: DateTimeOffset.UtcNow.AddMinutes(-10),
            Hostname: Environment.MachineName,
            Terminal: "/dev/ttys001");
        await File.WriteAllTextAsync(lockFile, JsonSerializer.Serialize(staleLockData));

        var lockService = new AtomicFileLockService(new NullLogger<AtomicFileLockService>());

        // Act
        var status = await lockService.GetStatusAsync(worktreeId, CancellationToken.None);

        // Assert
        status.IsStale.Should().BeTrue("lock older than 5 minutes should be stale");
        status.Age.Should().BeGreaterThan(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task Should_Block_Concurrent_Acquisition()
    {
        // Arrange
        var worktreeId = WorktreeId.From("worktree-01HKABC");
        var lockService = new AtomicFileLockService(new NullLogger<AtomicFileLockService>());

        // Act
        await using var lock1 = await lockService.AcquireAsync(worktreeId, null, CancellationToken.None);

        var act = async () => await lockService.AcquireAsync(worktreeId, timeout: null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<LockBusyException>()
            .WithMessage("*locked by another process*");
    }

    [Fact]
    public async Task Should_Queue_With_Wait_Timeout()
    {
        // Arrange
        var worktreeId = WorktreeId.From("worktree-01HKABC");
        var lockService = new AtomicFileLockService(new NullLogger<AtomicFileLockService>());

        await using var lock1 = await lockService.AcquireAsync(worktreeId, null, CancellationToken.None);

        // Act
        var startTime = DateTimeOffset.UtcNow;
        var act = async () => await lockService.AcquireAsync(
            worktreeId,
            timeout: TimeSpan.FromSeconds(2),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<TimeoutException>()
            .WithMessage("*timeout waiting for lock*");

        var elapsed = DateTimeOffset.UtcNow - startTime;
        elapsed.Should().BeCloseTo(TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(500));
    }
}
```

### Unit Tests - ContextTests.cs (Complete Implementation)

```csharp
// Tests/Unit/Concurrency/ContextTests.cs
using Xunit;
using FluentAssertions;
using AgenticCoder.Application.Concurrency;
using Moq;

namespace AgenticCoder.Tests.Unit.Concurrency;

public sealed class ContextTests
{
    [Fact]
    public async Task Should_Switch_Context_On_Directory_Change()
    {
        // Arrange
        var worktreeId1 = WorktreeId.From("worktree-auth");
        var worktreeId2 = WorktreeId.From("worktree-payments");
        var chatId1 = ChatId.NewId();
        var chatId2 = ChatId.NewId();

        var bindingService = new InMemoryBindingRepository();
        await bindingService.CreateAsync(WorktreeBinding.Create(worktreeId1, chatId1), CancellationToken.None);
        await bindingService.CreateAsync(WorktreeBinding.Create(worktreeId2, chatId2), CancellationToken.None);

        var contextResolver = new WorktreeContextResolver(
            bindingService,
            new NullLogger<WorktreeContextResolver>());

        // Act - Simulate changing directory
        var resolvedChat1 = await contextResolver.ResolveActiveChatAsync(worktreeId1, CancellationToken.None);
        var resolvedChat2 = await contextResolver.ResolveActiveChatAsync(worktreeId2, CancellationToken.None);

        // Assert
        resolvedChat1.Should().Be(chatId1, "worktree-auth should resolve to chatId1");
        resolvedChat2.Should().Be(chatId2, "worktree-payments should resolve to chatId2");
    }

    [Fact]
    public async Task Should_Isolate_Runs_By_Chat()
    {
        // Arrange
        var chatId1 = ChatId.NewId();
        var chatId2 = ChatId.NewId();

        var run1 = Domain.Conversation.Run.Create(chatId1);
        var run2 = Domain.Conversation.Run.Create(chatId2);

        var runRepository = new InMemoryRunRepository();
        await runRepository.CreateAsync(run1, CancellationToken.None);
        await runRepository.CreateAsync(run2, CancellationToken.None);

        // Act
        var chat1Runs = await runRepository.ListByChatAsync(chatId1, CancellationToken.None);
        var chat2Runs = await runRepository.ListByChatAsync(chatId2, CancellationToken.None);

        // Assert
        chat1Runs.Should().ContainSingle(r => r.Id == run1.Id, "chat1 should only have run1");
        chat2Runs.Should().ContainSingle(r => r.Id == run2.Id, "chat2 should only have run2");

        chat1Runs.Should().NotContain(run2, "run2 should not appear in chat1");
        chat2Runs.Should().NotContain(run1, "run1 should not appear in chat2");
    }

    [Fact]
    public async Task Should_Record_Worktree_In_Run()
    {
        // Arrange
        var chatId = ChatId.NewId();
        var worktreeId = WorktreeId.From("worktree-feature-auth");

        // Act
        var run = Domain.Conversation.Run.Create(chatId, worktreeId);

        // Assert
        run.WorktreeId.Should().Be(worktreeId, "run should record originating worktree");
        run.ChatId.Should().Be(chatId);
    }
}
```

### Integration Tests - MultiSessionTests.cs (Complete Implementation)

```csharp
// Tests/Integration/Concurrency/MultiSessionTests.cs
using Xunit;
using FluentAssertions;
using System.Diagnostics;

namespace AgenticCoder.Tests.Integration.Concurrency;

public sealed class MultiSessionTests
{
    [Fact]
    public async Task Should_Handle_Multiple_Terminals_Different_Worktrees()
    {
        // Arrange
        var worktree1 = WorktreeId.From("worktree-auth");
        var worktree2 = WorktreeId.From("worktree-payments");

        var lockService = new AtomicFileLockService(new NullLogger<AtomicFileLockService>());

        // Act - Simulate two terminal sessions acquiring locks on different worktrees
        await using var lock1 = await lockService.AcquireAsync(worktree1, null, CancellationToken.None);
        await using var lock2 = await lockService.AcquireAsync(worktree2, null, CancellationToken.None);

        // Assert
        lock1.Should().NotBeNull("terminal 1 should acquire lock on worktree-auth");
        lock2.Should().NotBeNull("terminal 2 should acquire lock on worktree-payments");

        // Both locks held simultaneously
        var lock1File = Path.Combine(".agent", "locks", $"{worktree1.Value}.lock");
        var lock2File = Path.Combine(".agent", "locks", $"{worktree2.Value}.lock");

        File.Exists(lock1File).Should().BeTrue();
        File.Exists(lock2File).Should().BeTrue();
    }

    [Fact]
    public async Task Should_Queue_With_Wait_Flag()
    {
        // Arrange
        var worktreeId = WorktreeId.From("worktree-01HKABC");
        var lockService = new AtomicFileLockService(new NullLogger<AtomicFileLockService>());

        // Terminal 1: Acquire lock
        await using var lock1 = await lockService.AcquireAsync(worktreeId, null, CancellationToken.None);

        // Terminal 2: Try to acquire with --wait (5 second timeout)
        var waitTask = lockService.AcquireAsync(worktreeId, TimeSpan.FromSeconds(5), CancellationToken.None);

        // Act - Release lock after 2 seconds
        await Task.Delay(TimeSpan.FromSeconds(2));
        await lock1.DisposeAsync();

        // Terminal 2 should acquire lock within wait period
        await using var lock2 = await waitTask;

        // Assert
        lock2.Should().NotBeNull("terminal 2 should acquire lock after terminal 1 releases");
        var lockFile = Path.Combine(".agent", "locks", $"{worktreeId.Value}.lock");
        File.Exists(lockFile).Should().BeTrue("lock should be held by terminal 2");
    }

    [Fact]
    public async Task Should_Timeout_If_Lock_Not_Released()
    {
        // Arrange
        var worktreeId = WorktreeId.From("worktree-01HKABC");
        var lockService = new AtomicFileLockService(new NullLogger<AtomicFileLockService>());

        await using var lock1 = await lockService.AcquireAsync(worktreeId, null, CancellationToken.None);

        // Act - Try to acquire with short timeout (lock never released)
        var act = async () => await lockService.AcquireAsync(
            worktreeId,
            timeout: TimeSpan.FromSeconds(1),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<TimeoutException>()
            .WithMessage("*timeout waiting for lock*");
    }
}
```

### Integration Tests - BindingPersistenceTests.cs (Complete Implementation)

```csharp
// Tests/Integration/Concurrency/BindingPersistenceTests.cs
using Xunit;
using FluentAssertions;
using Microsoft.Data.Sqlite;

namespace AgenticCoder.Tests.Integration.Concurrency;

public sealed class BindingPersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IBindingRepository _repository;

    public BindingPersistenceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Create schema
        var createTableSql = @"
            CREATE TABLE worktree_bindings (
                worktree_id TEXT PRIMARY KEY,
                chat_id TEXT NOT NULL,
                created_at TEXT NOT NULL
            )";
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = createTableSql;
        cmd.ExecuteNonQuery();

        _repository = new SqliteBindingRepository(_connection);
    }

    [Fact]
    public async Task Should_Survive_Application_Restart()
    {
        // Arrange
        var worktreeId = WorktreeId.From("worktree-feature-auth");
        var chatId = ChatId.NewId();
        var binding = WorktreeBinding.Create(worktreeId, chatId);

        // Act - Save binding
        await _repository.CreateAsync(binding, CancellationToken.None);

        // Simulate restart: Create new repository instance (same connection)
        var repository2 = new SqliteBindingRepository(_connection);
        var retrieved = await repository2.GetByWorktreeAsync(worktreeId, CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull("binding should persist across restarts");
        retrieved!.ChatId.Should().Be(chatId);
        retrieved.WorktreeId.Should().Be(worktreeId);
    }

    [Fact]
    public async Task Should_Cascade_Delete_On_Chat_Purge()
    {
        // Arrange
        var worktreeId = WorktreeId.From("worktree-feature-auth");
        var chatId = ChatId.NewId();
        var binding = WorktreeBinding.Create(worktreeId, chatId);
        await _repository.CreateAsync(binding, CancellationToken.None);

        // Act - Purge chat (cascade delete binding)
        await _repository.DeleteByChatAsync(chatId, CancellationToken.None);

        // Assert
        var retrieved = await _repository.GetByWorktreeAsync(worktreeId, CancellationToken.None);
        retrieved.Should().BeNull("binding should be deleted when chat is purged");
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
```

### E2E Tests - WorktreeWorkflowE2ETests.cs (Complete Implementation)

```csharp
// Tests/E2E/Concurrency/WorktreeWorkflowE2ETests.cs
using Xunit;
using FluentAssertions;

namespace AgenticCoder.Tests.E2E.Concurrency;

public sealed class WorktreeWorkflowE2ETests
{
    [Fact]
    public async Task Should_Auto_Bind_New_Chat_In_Worktree()
    {
        // Arrange - Create worktree
        var worktreeId = WorktreeId.From("worktree-feature-auth");
        var workspacePath = Path.Combine(Path.GetTempPath(), $"workspace-{Guid.NewGuid()}");
        Directory.CreateDirectory(workspacePath);

        var app = new TestApplication(workspacePath);

        // Act - Create chat while in worktree (auto-bind)
        var chatId = await app.ExecuteAsync($"chat new \"Auth Implementation\"", worktreeId);

        // Assert
        var binding = await app.BindingService.GetByWorktreeAsync(worktreeId, CancellationToken.None);
        binding.Should().NotBeNull("chat should be auto-bound to worktree");
        binding!.ChatId.Should().Be(chatId);
    }

    [Fact]
    public async Task Should_Switch_Context_On_Directory_Change()
    {
        // Arrange - Create two worktrees with bound chats
        var worktree1 = WorktreeId.From("worktree-auth");
        var worktree2 = WorktreeId.From("worktree-payments");

        var app = new TestApplication(Path.GetTempPath());

        var chatId1 = await app.ExecuteAsync("chat new \"Auth Chat\"", worktree1);
        var chatId2 = await app.ExecuteAsync("chat new \"Payments Chat\"", worktree2);

        // Act - Switch directories
        var activeChat1 = await app.GetActiveChatAsync(worktree1);
        var activeChat2 = await app.GetActiveChatAsync(worktree2);

        // Assert
        activeChat1.Should().Be(chatId1, "auth worktree should activate auth chat");
        activeChat2.Should().Be(chatId2, "payments worktree should activate payments chat");
    }

    [Fact]
    public async Task Should_Handle_Lock_Conflict_Gracefully()
    {
        // Arrange - Start run in worktree
        var worktreeId = WorktreeId.From("worktree-auth");
        var app = new TestApplication(Path.GetTempPath());

        var chatId = await app.ExecuteAsync("chat new \"Auth Chat\"", worktreeId);

        // Terminal 1: Start long-running command
        var runTask = app.ExecuteAsync("run \"Implement JWT validation\"", worktreeId);

        await Task.Delay(100);  // Ensure lock acquired

        // Act - Terminal 2: Try to run (should fail with lock error)
        var act = async () => await app.ExecuteAsync("run \"Add token refresh\"", worktreeId);

        // Assert
        await act.Should().ThrowAsync<LockBusyException>()
            .WithMessage("*worktree is locked*");

        // Wait for first run to complete
        await runTask;

        // Now second command should succeed
        await app.ExecuteAsync("run \"Add token refresh\"", worktreeId);
    }
}
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Context switch | 25ms | 50ms |
| Lock acquire | 5ms | 10ms |
| Binding query | 2ms | 5ms |

---

## User Verification Steps

### Scenario 1: Auto-Bind on Chat Creation

**Objective:** Verify that creating a new chat while in a worktree automatically binds the chat to that worktree.

**Preconditions:**
- Git repository with at least one worktree
- acode CLI installed and workspace initialized

**Steps:**
1. Create a new Git worktree: `git worktree add ../feature-auth feature/auth`
2. Navigate to worktree: `cd ../feature-auth`
3. Create a new chat: `acode chat new "Auth Implementation"`
4. View bindings: `acode chat bindings`
5. Check status: `acode status`

**Expected Results:**
- [ ] Chat created successfully with ULID
- [ ] Message displayed: "Bound to worktree: feature/auth" (or similar)
- [ ] Bindings list shows: `feature-auth` → `chat_<id>` ("Auth Implementation")
- [ ] Status shows: `Worktree: feature-auth`, `Bound Chat: Auth Implementation`

**Verification Commands:**
```bash
git worktree add ../feature-auth feature/auth
cd ../feature-auth
acode chat new "Auth Implementation"
acode chat bindings
acode status
```

---

### Scenario 2: Manual Bind and Unbind

**Objective:** Verify manual binding and unbinding commands work correctly.

**Preconditions:**
- Worktree exists without binding
- Chat exists that can be bound

**Steps:**
1. Create unbound chat: `acode chat new --no-bind "General Chat"`
2. Verify no binding: `acode chat bindings`
3. Bind chat to worktree: `acode chat bind <chat-id>`
4. Verify binding exists: `acode chat bindings`
5. Unbind: `acode chat unbind`
6. Confirm unbind when prompted
7. Verify binding removed: `acode chat bindings`

**Expected Results:**
- [ ] `--no-bind` creates chat without binding
- [ ] Manual bind creates binding and displays confirmation
- [ ] Unbind prompts for confirmation
- [ ] After unbind, worktree shows "no binding" in list

**Verification Commands:**
```bash
acode chat new --no-bind "General Chat"
acode chat bindings
acode chat bind <chat-id>
acode chat bindings
acode chat unbind
# Type 'y' at prompt
acode chat bindings
```

---

### Scenario 3: Context Switch Between Worktrees

**Objective:** Verify that active chat context changes when navigating between worktrees.

**Preconditions:**
- Two worktrees with different bound chats

**Steps:**
1. Create worktree-1 with bound chat: Auth Feature
2. Create worktree-2 with bound chat: Payments Feature
3. Navigate to worktree-1: `cd ../feature-auth`
4. Check status: `acode status` (should show Auth chat)
5. Navigate to worktree-2: `cd ../feature-payments`
6. Check status: `acode status` (should show Payments chat)
7. Run command: `acode run "Hello"` (should use Payments context)

**Expected Results:**
- [ ] Status in worktree-1 shows "Auth Feature" chat
- [ ] Status in worktree-2 shows "Payments Feature" chat
- [ ] Run command in worktree-2 uses Payments chat context
- [ ] Context switch latency < 50ms (feels instant)

**Verification Commands:**
```bash
# Setup (if not already done)
cd ../feature-auth
acode chat new "Auth Feature"
cd ../feature-payments
acode chat new "Payments Feature"

# Test context switch
cd ../feature-auth
acode status
cd ../feature-payments
acode status
acode run "What is my current chat context?"
```

---

### Scenario 4: Lock Conflict - Busy Error

**Objective:** Verify that concurrent access to same worktree produces immediate BUSY error.

**Preconditions:**
- Bound worktree ready for testing
- Two terminal windows available

**Steps:**
1. **Terminal 1:** Navigate to worktree: `cd feature-auth`
2. **Terminal 1:** Start long-running command: `acode run "Analyze this large codebase..."`
3. **Terminal 2:** Navigate to same worktree: `cd feature-auth`
4. **Terminal 2:** Try to run: `acode run "Quick question"`
5. Observe error message in Terminal 2
6. Wait for Terminal 1 to complete
7. **Terminal 2:** Run command again (should succeed)

**Expected Results:**
- [ ] Terminal 2 immediately gets error (no wait)
- [ ] Error shows: "Worktree is locked by another process"
- [ ] Error includes: process ID, terminal ID, lock age
- [ ] Error suggests: "Use --wait to queue, or work in another worktree"
- [ ] Exit code is non-zero (1)
- [ ] After Terminal 1 completes, Terminal 2 succeeds

**Verification Commands:**
```bash
# Terminal 1
cd feature-auth
acode run "Perform a detailed code review of the entire codebase"

# Terminal 2 (while Terminal 1 running)
cd feature-auth
acode run "What is 2+2?"
# Should fail with BUSY error
```

---

### Scenario 5: Lock Wait Queue with --wait Flag

**Objective:** Verify that `--wait` flag queues request until lock is available.

**Preconditions:**
- Bound worktree, two terminals

**Steps:**
1. **Terminal 1:** Start command: `acode run "Processing..."`
2. **Terminal 2:** Start with wait: `acode run --wait "Queued request"`
3. Observe Terminal 2 displays waiting indicator
4. Wait for Terminal 1 to complete
5. Observe Terminal 2 automatically acquires lock and runs

**Expected Results:**
- [ ] Terminal 2 displays: "Waiting for lock..." with elapsed time
- [ ] Terminal 2 does NOT show error
- [ ] When Terminal 1 finishes, Terminal 2 acquires lock within 2 seconds (poll interval)
- [ ] Terminal 2 command executes successfully

**Verification Commands:**
```bash
# Terminal 1
cd feature-auth
acode run "Generate a detailed test plan for the authentication module"

# Terminal 2 (start while Terminal 1 still running)
cd feature-auth
acode run --wait "What authentication methods should we support?"
# Should show "Waiting for lock..." then execute after Terminal 1 finishes
```

---

### Scenario 6: Stale Lock Detection and Cleanup

**Objective:** Verify that stale locks (from crashed processes) are automatically cleaned.

**Preconditions:**
- Worktree for testing

**Steps:**
1. Manually create stale lock file with old timestamp:
   ```powershell
   $lockData = @{ ProcessId=99999; LockedAt=(Get-Date).AddMinutes(-10).ToString("o"); Hostname=$env:COMPUTERNAME; Terminal="test" }
   $lockData | ConvertTo-Json | Out-File ".agent/locks/worktree-feature-auth.lock"
   ```
2. Check lock status: `acode lock status`
3. Try to run command: `acode run "Test"`
4. Observe stale lock is removed and command executes

**Expected Results:**
- [ ] Lock status shows: "STALE" (age > 5 minutes, process not running)
- [ ] Run command removes stale lock automatically
- [ ] Warning logged: "Removing stale lock..."
- [ ] Command executes successfully

**Verification Commands:**
```bash
# Create stale lock (PowerShell)
$lockData = @{ ProcessId=99999; LockedAt=(Get-Date).AddMinutes(-10).ToString("o"); Hostname=$env:COMPUTERNAME; Terminal="test" } | ConvertTo-Json
Set-Content -Path ".agent/locks/worktree-feature-auth.lock" -Value $lockData

# Verify stale detection
acode lock status
# Should show STALE

# Run command (should clean stale and proceed)
acode run "Hello"
```

---

### Scenario 7: One-to-One Binding Constraint

**Objective:** Verify that each worktree can only have one bound chat (constraint enforced).

**Preconditions:**
- Worktree with existing binding

**Steps:**
1. Create worktree with bound chat
2. Try to bind different chat to same worktree: `acode chat bind <other-chat-id>`
3. Observe error message
4. Unbind first: `acode chat unbind --force`
5. Bind new chat: `acode chat bind <other-chat-id>`
6. Verify new binding: `acode chat bindings`

**Expected Results:**
- [ ] Second bind attempt fails with: "Worktree is already bound to chat X"
- [ ] After unbind, new binding succeeds
- [ ] Bindings list shows new binding only

**Verification Commands:**
```bash
acode chat new "First Chat"  # Auto-binds
CHAT_2=$(acode chat new --no-bind "Second Chat" --json | jq -r '.id')
acode chat bind $CHAT_2
# Should fail - already bound
acode chat unbind --force
acode chat bind $CHAT_2
# Should succeed
acode chat bindings
```

---

### Scenario 8: Force Unlock Emergency Recovery

**Objective:** Verify force-unlock removes lock file regardless of owner.

**Preconditions:**
- Lock held by simulated "stuck" process

**Steps:**
1. Create lock file manually (simulating stuck process)
2. Verify lock exists: `acode lock status`
3. Try normal run: `acode run "Test"` (should fail)
4. Force unlock: `acode unlock --force`
5. Run command: `acode run "Test"` (should succeed)

**Expected Results:**
- [ ] Force unlock removes lock file
- [ ] Warning displayed about force unlock
- [ ] Subsequent commands succeed

**Verification Commands:**
```bash
# Create fake lock (as if another process holds it)
echo '{"ProcessId":99999,"LockedAt":"2024-01-01T00:00:00Z","Hostname":"otherhost","Terminal":"tty1"}' > .agent/locks/worktree-test.lock

acode lock status
acode run "Test"  # Should fail
acode unlock --force
acode run "Test"  # Should succeed
```

---

### Scenario 9: Cascade Delete on Chat Purge

**Objective:** Verify that purging a chat removes its worktree binding.

**Preconditions:**
- Worktree with bound chat

**Steps:**
1. Create bound chat in worktree
2. Note chat ID and verify binding: `acode chat bindings`
3. Purge the chat: `acode chat purge <id> --force`
4. Verify binding removed: `acode chat bindings`
5. Check worktree now unbound: `acode status`

**Expected Results:**
- [ ] Binding removed automatically when chat purged
- [ ] Worktree shows as "unbound" in status
- [ ] No orphaned binding remains in database

**Verification Commands:**
```bash
cd feature-test
CHAT_ID=$(acode chat new "To Be Purged" --json | jq -r '.id')
acode chat bindings  # Should show binding
acode chat purge $CHAT_ID --force
acode chat bindings  # Binding should be gone
acode status  # Should show "Bound Chat: none" or similar
```

---

### Scenario 10: Multi-Worktree Simultaneous Operations

**Objective:** Verify that different worktrees can run commands simultaneously without conflict.

**Preconditions:**
- Two worktrees with different bindings
- Two terminal windows

**Steps:**
1. **Terminal 1:** Navigate to worktree-1, start command
2. **Terminal 2:** Navigate to worktree-2, start command
3. Both should run simultaneously without lock conflict
4. Verify each uses correct chat context

**Expected Results:**
- [ ] Both commands run simultaneously
- [ ] No lock conflicts (different worktrees = different locks)
- [ ] Each command uses its worktree's bound chat
- [ ] Both complete successfully

**Verification Commands:**
```bash
# Terminal 1
cd ../feature-auth
acode run "Analyze authentication module"

# Terminal 2 (start at same time)
cd ../feature-payments
acode run "Review payment integration"

# Both should run without blocking each other
```

---

## Implementation Prompt

### Complete Domain Entities

```csharp
// src/AgenticCoder.Domain/Concurrency/WorktreeBinding.cs
namespace AgenticCoder.Domain.Concurrency;

public sealed class WorktreeBinding
{
    public WorktreeId WorktreeId { get; }
    public ChatId ChatId { get; }
    public DateTimeOffset CreatedAt { get; }

    private WorktreeBinding(
        WorktreeId worktreeId,
        ChatId chatId,
        DateTimeOffset createdAt)
    {
        WorktreeId = worktreeId;
        ChatId = chatId;
        CreatedAt = createdAt;
    }

    public static WorktreeBinding Create(WorktreeId worktreeId, ChatId chatId)
    {
        return new WorktreeBinding(worktreeId, chatId, DateTimeOffset.UtcNow);
    }

    public static WorktreeBinding Reconstitute(
        WorktreeId worktreeId,
        ChatId chatId,
        DateTimeOffset createdAt)
    {
        return new WorktreeBinding(worktreeId, chatId, createdAt);
    }
}

// src/AgenticCoder.Domain/Concurrency/WorktreeLock.cs
public sealed class WorktreeLock
{
    public WorktreeId WorktreeId { get; }
    public int ProcessId { get; }
    public DateTimeOffset LockedAt { get; }
    public string Hostname { get; }
    public string Terminal { get; }

    public WorktreeLock(
        WorktreeId worktreeId,
        int processId,
        DateTimeOffset lockedAt,
        string hostname,
        string terminal)
    {
        WorktreeId = worktreeId;
        ProcessId = processId;
        LockedAt = lockedAt;
        Hostname = hostname;
        Terminal = terminal;
    }

    public TimeSpan Age => DateTimeOffset.UtcNow - LockedAt;

    public bool IsStale(TimeSpan threshold) => Age > threshold;

    public bool IsOwnedByCurrentProcess() => ProcessId == Environment.ProcessId;

    public static WorktreeLock CreateForCurrentProcess(WorktreeId worktreeId)
    {
        return new WorktreeLock(
            worktreeId,
            Environment.ProcessId,
            DateTimeOffset.UtcNow,
            Environment.MachineName,
            GetTerminalId());
    }

    private static string GetTerminalId()
    {
        // Unix: Use TTY, Windows: Use session ID
        if (!OperatingSystem.IsWindows())
        {
            return Environment.GetEnvironmentVariable("TTY") ?? "/dev/ttys000";
        }

        return $"session-{Process.GetCurrentProcess().SessionId}";
    }
}
```

### Application Layer Interfaces

```csharp
// src/AgenticCoder.Application/Concurrency/IBindingService.cs
namespace AgenticCoder.Application.Concurrency;

public interface IBindingService
{
    Task<ChatId?> GetBoundChatAsync(WorktreeId worktreeId, CancellationToken ct);
    Task<WorktreeId?> GetBoundWorktreeAsync(ChatId chatId, CancellationToken ct);
    Task CreateBindingAsync(WorktreeId worktreeId, ChatId chatId, CancellationToken ct);
    Task DeleteBindingAsync(WorktreeId worktreeId, CancellationToken ct);
    Task<IReadOnlyList<WorktreeBinding>> ListAllBindingsAsync(CancellationToken ct);
}

// src/AgenticCoder.Application/Concurrency/ILockService.cs
public interface ILockService
{
    Task<IAsyncDisposable> AcquireAsync(
        WorktreeId worktreeId,
        TimeSpan? timeout,
        CancellationToken ct);

    Task<LockStatus> GetStatusAsync(
        WorktreeId worktreeId,
        CancellationToken ct);

    Task ReleaseStaleLocksAsync(
        TimeSpan threshold,
        CancellationToken ct);

    Task ForceUnlockAsync(
        WorktreeId worktreeId,
        CancellationToken ct);
}

public sealed record LockStatus(
    bool IsLocked,
    bool IsStale,
    TimeSpan Age,
    int? ProcessId,
    string? Hostname,
    string? Terminal);

// src/AgenticCoder.Application/Concurrency/IContextResolver.cs
public interface IContextResolver
{
    Task<ChatId?> ResolveActiveChatAsync(
        WorktreeId currentWorktree,
        CancellationToken ct);

    Task<WorktreeId?> DetectCurrentWorktreeAsync(
        string currentDirectory,
        CancellationToken ct);

    Task NotifyContextSwitchAsync(
        WorktreeId from,
        WorktreeId to,
        CancellationToken ct);
}
```

### Infrastructure Implementations

```csharp
// src/AgenticCoder.Infrastructure/Concurrency/SqliteBindingRepository.cs
namespace AgenticCoder.Infrastructure.Concurrency;

public sealed class SqliteBindingRepository : IBindingRepository
{
    private readonly IDbConnection _connection;
    private readonly ILogger<SqliteBindingRepository> _logger;

    public SqliteBindingRepository(
        IDbConnection connection,
        ILogger<SqliteBindingRepository> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task<WorktreeBinding?> GetByWorktreeAsync(
        WorktreeId worktreeId,
        CancellationToken ct)
    {
        const string sql = @"
            SELECT worktree_id, chat_id, created_at
            FROM worktree_bindings
            WHERE worktree_id = @WorktreeId";

        var row = await _connection.QuerySingleOrDefaultAsync<BindingRow>(
            sql,
            new { WorktreeId = worktreeId.Value });

        if (row is null) return null;

        return WorktreeBinding.Reconstitute(
            WorktreeId.From(row.WorktreeId),
            ChatId.From(row.ChatId),
            row.CreatedAt);
    }

    public async Task<WorktreeBinding?> GetByChatAsync(
        ChatId chatId,
        CancellationToken ct)
    {
        const string sql = @"
            SELECT worktree_id, chat_id, created_at
            FROM worktree_bindings
            WHERE chat_id = @ChatId";

        var row = await _connection.QuerySingleOrDefaultAsync<BindingRow>(
            sql,
            new { ChatId = chatId.Value });

        if (row is null) return null;

        return WorktreeBinding.Reconstitute(
            WorktreeId.From(row.WorktreeId),
            ChatId.From(row.ChatId),
            row.CreatedAt);
    }

    public async Task CreateAsync(WorktreeBinding binding, CancellationToken ct)
    {
        // Check for existing binding (enforce one-to-one)
        var existing = await GetByWorktreeAsync(binding.WorktreeId, ct);
        if (existing is not null)
        {
            throw new InvalidOperationException(
                $"Worktree {binding.WorktreeId} is already bound to chat {existing.ChatId}");
        }

        const string sql = @"
            INSERT INTO worktree_bindings (worktree_id, chat_id, created_at)
            VALUES (@WorktreeId, @ChatId, @CreatedAt)";

        await _connection.ExecuteAsync(sql, new
        {
            WorktreeId = binding.WorktreeId.Value,
            ChatId = binding.ChatId.Value,
            CreatedAt = binding.CreatedAt
        });

        _logger.LogInformation(
            "Binding created: Worktree={WorktreeId}, Chat={ChatId}",
            binding.WorktreeId, binding.ChatId);
    }

    public async Task DeleteAsync(WorktreeId worktreeId, CancellationToken ct)
    {
        const string sql = "DELETE FROM worktree_bindings WHERE worktree_id = @WorktreeId";
        await _connection.ExecuteAsync(sql, new { WorktreeId = worktreeId.Value });

        _logger.LogInformation("Binding deleted for worktree {WorktreeId}", worktreeId);
    }

    public async Task DeleteByChatAsync(ChatId chatId, CancellationToken ct)
    {
        const string sql = "DELETE FROM worktree_bindings WHERE chat_id = @ChatId";
        await _connection.ExecuteAsync(sql, new { ChatId = chatId.Value });

        _logger.LogInformation("Binding deleted for chat {ChatId}", chatId);
    }

    public async Task<IReadOnlyList<WorktreeBinding>> ListAllAsync(CancellationToken ct)
    {
        const string sql = @"
            SELECT worktree_id, chat_id, created_at
            FROM worktree_bindings
            ORDER BY created_at DESC";

        var rows = await _connection.QueryAsync<BindingRow>(sql);

        return rows.Select(r => WorktreeBinding.Reconstitute(
            WorktreeId.From(r.WorktreeId),
            ChatId.From(r.ChatId),
            r.CreatedAt)).ToList();
    }

    private sealed record BindingRow(
        string WorktreeId,
        string ChatId,
        DateTimeOffset CreatedAt);
}

// src/AgenticCoder.Infrastructure/Concurrency/AtomicFileLockService.cs
public sealed class AtomicFileLockService : ILockService
{
    private readonly string _locksDirectory;
    private readonly ILogger<AtomicFileLockService> _logger;
    private readonly SafeLockPathResolver _pathResolver;

    public AtomicFileLockService(
        string workspaceRoot,
        ILogger<AtomicFileLockService> logger)
    {
        _locksDirectory = Path.Combine(workspaceRoot, ".agent", "locks");
        _logger = logger;
        _pathResolver = new SafeLockPathResolver(workspaceRoot, logger);

        Directory.CreateDirectory(_locksDirectory);
    }

    public async Task<IAsyncDisposable> AcquireAsync(
        WorktreeId worktreeId,
        TimeSpan? timeout,
        CancellationToken ct)
    {
        var lockFilePath = _pathResolver.GetLockFilePath(worktreeId);
        var tempFilePath = lockFilePath + ".tmp";

        var lockData = new LockData(
            ProcessId: Environment.ProcessId,
            LockedAt: DateTimeOffset.UtcNow,
            Hostname: Environment.MachineName,
            Terminal: WorktreeLock.GetTerminalId());

        var startTime = DateTimeOffset.UtcNow;

        while (true)
        {
            try
            {
                // Write to temp file
                var json = JsonSerializer.Serialize(lockData);
                await File.WriteAllTextAsync(tempFilePath, json, ct);

                // Set permissions (Unix only)
                if (!OperatingSystem.IsWindows())
                {
                    File.SetUnixFileMode(tempFilePath,
                        UnixFileMode.UserRead | UnixFileMode.UserWrite);
                }

                // Atomic rename
                File.Move(tempFilePath, lockFilePath, overwrite: false);

                // Verify ownership
                var verify = await File.ReadAllTextAsync(lockFilePath, ct);
                var verifyData = JsonSerializer.Deserialize<LockData>(verify);

                if (verifyData?.ProcessId != Environment.ProcessId)
                {
                    throw new LockCorruptedException(worktreeId, "Ownership verification failed");
                }

                _logger.LogInformation("Lock acquired for {Worktree}", worktreeId);

                return new FileLock(lockFilePath, _logger);
            }
            catch (IOException) when (File.Exists(lockFilePath))
            {
                // Lock exists - check if stale
                var status = await GetStatusAsync(worktreeId, ct);

                if (status.IsStale)
                {
                    _logger.LogWarning("Removing stale lock for {Worktree}", worktreeId);
                    File.Delete(lockFilePath);
                    continue;  // Retry acquisition
                }

                // Lock is active - wait or error
                if (timeout.HasValue)
                {
                    var elapsed = DateTimeOffset.UtcNow - startTime;
                    if (elapsed >= timeout.Value)
                    {
                        throw new TimeoutException(
                            $"Timeout waiting for lock on {worktreeId} after {elapsed.TotalSeconds:F1}s");
                    }

                    _logger.LogDebug("Waiting for lock on {Worktree}...", worktreeId);
                    await Task.Delay(TimeSpan.FromSeconds(2), ct);
                    continue;
                }

                throw new LockBusyException(worktreeId, status);
            }
        }
    }

    public async Task<LockStatus> GetStatusAsync(
        WorktreeId worktreeId,
        CancellationToken ct)
    {
        var lockFilePath = _pathResolver.GetLockFilePath(worktreeId);

        if (!File.Exists(lockFilePath))
        {
            return new LockStatus(false, false, TimeSpan.Zero, null, null, null);
        }

        var json = await File.ReadAllTextAsync(lockFilePath, ct);
        var data = JsonSerializer.Deserialize<LockData>(json);

        if (data is null)
        {
            return new LockStatus(false, false, TimeSpan.Zero, null, null, null);
        }

        var age = DateTimeOffset.UtcNow - data.LockedAt;
        var isStale = age > TimeSpan.FromMinutes(5);

        return new LockStatus(true, isStale, age, data.ProcessId, data.Hostname, data.Terminal);
    }

    public async Task ReleaseStaleLocksAsync(TimeSpan threshold, CancellationToken ct)
    {
        var lockFiles = Directory.GetFiles(_locksDirectory, "*.lock");

        foreach (var lockFile in lockFiles)
        {
            var json = await File.ReadAllTextAsync(lockFile, ct);
            var data = JsonSerializer.Deserialize<LockData>(json);

            if (data is null) continue;

            var age = DateTimeOffset.UtcNow - data.LockedAt;

            if (age > threshold)
            {
                _logger.LogWarning(
                    "Removing stale lock: {LockFile}, Age={Age}s, PID={ProcessId}",
                    lockFile, age.TotalSeconds, data.ProcessId);

                File.Delete(lockFile);
            }
        }
    }

    public async Task ForceUnlockAsync(WorktreeId worktreeId, CancellationToken ct)
    {
        var lockFilePath = _pathResolver.GetLockFilePath(worktreeId);

        if (File.Exists(lockFilePath))
        {
            File.Delete(lockFilePath);
            _logger.LogWarning("Force-unlocked worktree {Worktree}", worktreeId);
        }

        await Task.CompletedTask;
    }

    private sealed class FileLock : IAsyncDisposable
    {
        private readonly string _lockFilePath;
        private readonly ILogger _logger;
        private bool _disposed;

        public FileLock(string lockFilePath, ILogger logger)
        {
            _lockFilePath = lockFilePath;
            _logger = logger;
        }

        public ValueTask DisposeAsync()
        {
            if (_disposed) return ValueTask.CompletedTask;

            try
            {
                if (File.Exists(_lockFilePath))
                {
                    File.Delete(_lockFilePath);
                    _logger.LogInformation("Lock released: {LockFile}", _lockFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to release lock: {LockFile}", _lockFilePath);
            }

            _disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}

// src/AgenticCoder.Infrastructure/Concurrency/WorktreeContextResolver.cs
public sealed class WorktreeContextResolver : IContextResolver
{
    private readonly IBindingService _bindingService;
    private readonly IGitWorktreeDetector _worktreeDetector;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<WorktreeContextResolver> _logger;

    public WorktreeContextResolver(
        IBindingService bindingService,
        IGitWorktreeDetector worktreeDetector,
        IEventPublisher eventPublisher,
        ILogger<WorktreeContextResolver> logger)
    {
        _bindingService = bindingService;
        _worktreeDetector = worktreeDetector;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<ChatId?> ResolveActiveChatAsync(
        WorktreeId currentWorktree,
        CancellationToken ct)
    {
        var chatId = await _bindingService.GetBoundChatAsync(currentWorktree, ct);

        if (chatId.HasValue)
        {
            _logger.LogDebug(
                "Resolved active chat: Worktree={Worktree}, Chat={Chat}",
                currentWorktree, chatId.Value);
        }
        else
        {
            _logger.LogDebug(
                "No bound chat for worktree {Worktree}, using global/manual selection",
                currentWorktree);
        }

        return chatId;
    }

    public async Task<WorktreeId?> DetectCurrentWorktreeAsync(
        string currentDirectory,
        CancellationToken ct)
    {
        var worktree = await _worktreeDetector.DetectAsync(currentDirectory, ct);

        if (worktree is not null)
        {
            _logger.LogDebug("Detected worktree: {Worktree} at {Path}", worktree.Id, worktree.Path);
        }

        return worktree?.Id;
    }

    public async Task NotifyContextSwitchAsync(
        WorktreeId from,
        WorktreeId to,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Context switch: {From} → {To}",
            from, to);

        var @event = new ContextSwitchedEvent(from, to, DateTimeOffset.UtcNow);
        await _eventPublisher.PublishAsync(@event, ct);
    }
}
```

### Error Codes

| Code | Meaning | Resolution |
|------|---------|------------|
| ACODE-CONC-001 | Worktree locked by another process | Wait for lock release or use `--wait` flag |
| ACODE-CONC-002 | Wait timeout exceeded | Increase timeout or check for stuck processes |
| ACODE-CONC-003 | Binding already exists | Unbind existing chat before creating new binding |
| ACODE-CONC-004 | Worktree not found | Verify worktree exists with `git worktree list` |
| ACODE-CONC-005 | Chat already bound to different worktree | Unbind from original worktree first |
| ACODE-CONC-006 | Lock file corrupted | Force-unlock with `acode unlock <worktree>` |
| ACODE-CONC-007 | Permission denied on lock file | Check file permissions in `.agent/locks/` |

### Implementation Checklist

1. [ ] Create WorktreeBinding domain entity with Create and Reconstitute
2. [ ] Create WorktreeLock domain entity with stale detection
3. [ ] Implement IBindingService interface
4. [ ] Implement SqliteBindingRepository with parameterized queries
5. [ ] Implement ILockService interface
6. [ ] Implement AtomicFileLockService with file-based locking
7. [ ] Implement SafeLockPathResolver for path traversal prevention
8. [ ] Implement IContextResolver interface
9. [ ] Implement WorktreeContextResolver with binding lookup
10. [ ] Add LockFileValidator for security checks
11. [ ] Add ValidatedBindingCache with staleness detection
12. [ ] Create database migration for worktree_bindings table
13. [ ] Add CLI commands: `acode chat bind`, `acode chat unbind`, `acode unlock`
14. [ ] Add context switching logic to CLI run command
15. [ ] Add lock acquisition/release in run lifecycle
16. [ ] Add stale lock cleanup background task
17. [ ] Write unit tests for binding entity, lock entity (10+ tests)
18. [ ] Write unit tests for lock service (12+ tests)
19. [ ] Write integration tests for multi-session scenarios (5+ tests)
20. [ ] Write E2E tests for worktree workflows (3+ tests)
21. [ ] Add performance benchmarks for context switching
22. [ ] Add logging for lock events and context switches
23. [ ] Add metrics for lock contention rate
24. [ ] Write user documentation for binding workflows
25. [ ] Write troubleshooting guide for lock issues

### Rollout Plan

1. **Phase 1: Domain & Application Layer** (Week 1)
   - Create WorktreeBinding and WorktreeLock entities
   - Define IBindingService, ILockService, IContextResolver interfaces
   - Write unit tests for domain entities

2. **Phase 2: Infrastructure - Binding** (Week 2)
   - Implement SqliteBindingRepository
   - Create database migration for worktree_bindings table
   - Write integration tests for binding persistence
   - Add ValidatedBindingCache with staleness detection

3. **Phase 3: Infrastructure - Locking** (Week 2-3)
   - Implement AtomicFileLockService with file-based locks
   - Implement SafeLockPathResolver for path sanitization
   - Add stale lock detection and cleanup
   - Write integration tests for lock acquisition/release

4. **Phase 4: Context Resolution** (Week 3)
   - Implement WorktreeContextResolver
   - Integrate with Git worktree detection (Task 022)
   - Add context switch event publishing
   - Write E2E tests for automatic context switching

5. **Phase 5: CLI Integration** (Week 4)
   - Add `acode chat bind <id>` command
   - Add `acode chat unbind` command
   - Add `acode unlock <worktree>` command for force-unlock
   - Integrate lock acquisition into `acode run` command
   - Add lock status to `acode status` output

6. **Phase 6: Security & Observability** (Week 4-5)
   - Implement LockFileValidator for permission checks
   - Add process ID validation (check if process still running)
   - Add hostname validation (prevent cross-machine tampering)
   - Add performance metrics (lock contention rate, context switch latency)
   - Write security test scenarios

---

**End of Task 049.c Specification**