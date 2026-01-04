# Task 023.b: persist worktree ↔ task mapping

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 3 (Fibonacci points)  
**Phase:** Phase 5 – Git Integration Layer  
**Dependencies:** Task 023 (Worktree-per-Task), Task 011 (Workspace DB)  

---

## Description

Task 023.b implements persistence for the worktree-to-task mapping. When a worktree is created for a task, the association MUST be stored in the Workspace DB. This enables recovery and lookup.

The mapping MUST survive process restarts. After a crash or restart, the agent MUST be able to resume work in the correct worktree for each task.

Lookup by task ID MUST be efficient. The mapping MUST use indexed queries. Orphan detection MUST identify worktrees without active tasks.

Mapping updates MUST be atomic with worktree operations. If worktree creation fails, no mapping MUST exist. If removal succeeds, the mapping MUST be deleted.

### Business Value

Persistent mapping enables reliable task isolation. The agent can recover context after restarts. Orphaned worktrees can be detected and cleaned.

### Scope Boundaries

This task covers database persistence only. Worktree operations are in 023.a. Cleanup policies are in 023.c.

### Integration Points

- Task 023: Worktree management
- Task 011: Workspace DB storage
- Task 039: Task/session context

### Failure Modes

- DB write fails → Rollback worktree creation
- Mapping out of sync → Reconciliation on startup
- Task not found → Return null, not error

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Mapping | Association between worktree path and task ID |
| Orphan | Worktree with no associated active task |
| Reconciliation | Syncing DB state with filesystem state |

---

## Out of Scope

- Worktree creation/removal (Task 023.a)
- Cleanup policy execution (Task 023.c)
- Cross-repo mappings

---

## Assumptions

### Technical Assumptions

1. **SQLite database available** - Workspace database from Task 050
2. **Schema applied** - Migration creates worktree_mappings table
3. **Unique constraints** - One mapping per task, one per path
4. **Transaction support** - Atomic operations for consistency
5. **Index support** - Database indexes for efficient queries

### Data Assumptions

6. **Task IDs stable** - Task IDs don't change during lifecycle
7. **Paths absolute** - Worktree paths stored as absolute paths
8. **Timestamps UTC** - All times stored as UTC
9. **Reasonable scale** - <1000 mappings per workspace typical
10. **No external sync** - Single-process database access

### Integration Assumptions

11. **Worktree service calls mapping** - Task 023.a uses this for persistence
12. **Cleanup queries mappings** - Task 023.c queries for cleanup candidates
13. **Reconciliation possible** - Can sync DB with filesystem state
14. **Error propagation** - DB errors surface to callers

---

## Functional Requirements

### FR-001 to FR-020: Core Operations

- FR-001: `CreateMappingAsync` MUST store worktree-task association
- FR-002: `GetMappingByTaskAsync` MUST return worktree for task
- FR-003: `GetMappingByPathAsync` MUST return task for worktree
- FR-004: `DeleteMappingAsync` MUST remove association
- FR-005: `ListMappingsAsync` MUST return all mappings
- FR-006: Mapping MUST include worktree path
- FR-007: Mapping MUST include task ID
- FR-008: Mapping MUST include creation timestamp
- FR-009: Mapping MUST include last access timestamp
- FR-010: Mapping MUST include branch name
- FR-011: Mapping MUST include commit SHA at creation
- FR-012: Duplicate task mapping MUST be rejected
- FR-013: Duplicate path mapping MUST be rejected
- FR-014: Null task ID MUST be allowed (manual worktrees)
- FR-015: Lookup by task MUST use index
- FR-016: Lookup by path MUST use index
- FR-017: All operations MUST be async
- FR-018: All operations MUST support cancellation
- FR-019: Operations MUST be transactional
- FR-020: Failed operations MUST NOT leave partial state

### FR-021 to FR-035: Synchronization

- FR-021: Startup MUST reconcile DB with filesystem
- FR-022: Missing worktrees MUST be flagged
- FR-023: Missing mappings MUST be created for found worktrees
- FR-024: Reconciliation MUST be logged
- FR-025: `ReconcileAsync` MUST be callable manually
- FR-026: Reconciliation MUST NOT delete mappings automatically
- FR-027: Orphan query MUST return unmapped worktrees
- FR-028: Stale query MUST return mappings for missing worktrees
- FR-029: Access timestamp MUST update on worktree use
- FR-030: Access update MUST be debounced
- FR-031: Bulk operations MUST be efficient
- FR-032: Large mapping counts MUST be handled
- FR-033: Mapping schema MUST be versioned
- FR-034: Schema migration MUST be automatic
- FR-035: Backup MUST be possible

---

## Non-Functional Requirements

- NFR-001: Lookup MUST complete in <50ms
- NFR-002: Insert MUST complete in <100ms
- NFR-003: List all MUST complete in <200ms
- NFR-004: Reconciliation MUST complete in <5s for 100 worktrees
- NFR-005: Concurrent access MUST be safe
- NFR-006: DB lock timeout MUST be 5s
- NFR-007: Transaction isolation MUST prevent dirty reads
- NFR-008: Path storage MUST be normalized
- NFR-009: Case sensitivity MUST match filesystem
- NFR-010: Maximum path length MUST be 4096

---

## User Manual Documentation

### Database Schema

```sql
CREATE TABLE worktree_mappings (
    id INTEGER PRIMARY KEY,
    worktree_path TEXT NOT NULL UNIQUE,
    task_id TEXT UNIQUE,
    branch_name TEXT NOT NULL,
    commit_sha TEXT NOT NULL,
    created_at TEXT NOT NULL,
    last_accessed_at TEXT NOT NULL
);

CREATE INDEX idx_worktree_task ON worktree_mappings(task_id);
CREATE INDEX idx_worktree_path ON worktree_mappings(worktree_path);
```

### API Usage

```csharp
// Create mapping when worktree created
await _mappingService.CreateMappingAsync(new WorktreeMapping
{
    WorktreePath = "/path/to/worktree",
    TaskId = "TASK-123",
    BranchName = "feature/task-123",
    CommitSha = "abc123"
});

// Lookup by task
var mapping = await _mappingService.GetMappingByTaskAsync("TASK-123");

// Lookup by path
var mapping = await _mappingService.GetMappingByPathAsync("/path/to/worktree");
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Create mapping works
- [ ] AC-002: Get by task works
- [ ] AC-003: Get by path works
- [ ] AC-004: Delete mapping works
- [ ] AC-005: List all works
- [ ] AC-006: Duplicate rejection works
- [ ] AC-007: Indexes used for queries
- [ ] AC-008: Reconciliation works
- [ ] AC-009: Orphan detection works
- [ ] AC-010: Performance benchmarks met

---

## Best Practices

### Data Model

1. **Index by task ID** - Fast lookup by task
2. **Index by path** - Fast lookup by worktree path
3. **Store creation time** - Track when mapping created
4. **Store last access** - Know when worktree was used

### Consistency

5. **Transactional updates** - Create/delete atomically
6. **Reconcile periodically** - Sync DB with filesystem
7. **Handle orphans** - Mappings for deleted worktrees
8. **Handle duplicates** - Prevent same task/path mapped twice

### Query Optimization

9. **Use prepared statements** - Avoid SQL injection, improve perf
10. **Batch queries** - Load multiple mappings at once
11. **Cache hot paths** - Frequently accessed mappings in memory
12. **Log slow queries** - Identify performance issues

---

## Troubleshooting

### Issue: Mapping not found for task

**Symptoms:** GetMappingByTask returns null for active task

**Causes:**
- Worktree created but mapping not persisted
- Transaction rolled back
- Wrong task ID used

**Solutions:**
1. Check if worktree exists on filesystem
2. Run reconciliation to sync state
3. Verify task ID format matches

### Issue: Duplicate mapping error

**Symptoms:** "UNIQUE constraint failed" on create

**Causes:**
- Task already has a worktree
- Path already mapped to another task

**Solutions:**
1. Query existing mapping for task first
2. Delete old mapping if intentional replacement
3. Use different path for new worktree

### Issue: Orphaned mappings accumulate

**Symptoms:** Mappings in DB for non-existent worktrees

**Causes:**
- Worktrees deleted without updating DB
- Crash during removal operation

**Solutions:**
1. Run reconciliation: `acode worktree reconcile`
2. Implement startup reconciliation
3. Schedule periodic cleanup

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Test mapping creation
- [ ] UT-002: Test lookup by task
- [ ] UT-003: Test lookup by path
- [ ] UT-004: Test duplicate rejection
- [ ] UT-005: Test reconciliation logic

### Integration Tests

- [ ] IT-001: Full CRUD cycle
- [ ] IT-002: Reconciliation with real DB
- [ ] IT-003: Concurrent access
- [ ] IT-004: Large dataset handling

### Performance Tests

- [ ] PB-001: Lookup in <50ms
- [ ] PB-002: Insert in <100ms
- [ ] PB-003: List 1000 in <500ms

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Core/
│   └── Domain/
│       └── Git/
│           ├── WorktreeMapping.cs        # Mapping entity
│           └── WorktreeMappingId.cs      # Strongly-typed ID
│
├── Acode.Application/
│   └── Services/
│       └── Git/
│           ├── IWorktreeMappingRepository.cs  # Repository interface
│           ├── IWorktreeMappingService.cs     # Service interface
│           └── WorktreeMappingService.cs      # Orchestration service
│
├── Acode.Infrastructure/
│   └── Persistence/
│       ├── WorktreeMappingRepository.cs  # SQLite implementation
│       └── Migrations/
│           └── 003_WorktreeMappings.sql
│
└── Acode.Cli/
    └── Commands/
        └── Worktree/
            └── WorktreeMappingCommands.cs

tests/
├── Acode.Application.Tests/
│   └── Services/
│       └── Git/
│           └── WorktreeMappingServiceTests.cs
│
└── Acode.Infrastructure.Tests/
    └── Persistence/
        └── WorktreeMappingRepositoryTests.cs
```

### Domain Models

```csharp
// Acode.Core/Domain/Git/WorktreeMappingId.cs
namespace Acode.Core.Domain.Git;

/// <summary>
/// Strongly-typed identifier for worktree mappings.
/// </summary>
public readonly record struct WorktreeMappingId
{
    public int Value { get; }
    
    public WorktreeMappingId(int value)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value), "ID must be positive");
        Value = value;
    }
    
    public static implicit operator int(WorktreeMappingId id) => id.Value;
    public static explicit operator WorktreeMappingId(int value) => new(value);
    
    public override string ToString() => Value.ToString();
}

// Acode.Core/Domain/Git/WorktreeMapping.cs
namespace Acode.Core.Domain.Git;

/// <summary>
/// Represents the association between a worktree and a task.
/// </summary>
public sealed class WorktreeMapping
{
    public WorktreeMappingId? Id { get; private set; }
    
    /// <summary>
    /// Absolute path to the worktree directory.
    /// </summary>
    public string WorktreePath { get; }
    
    /// <summary>
    /// Associated task ID, or null for manual worktrees.
    /// </summary>
    public string? TaskId { get; }
    
    /// <summary>
    /// Branch name used in the worktree.
    /// </summary>
    public string BranchName { get; }
    
    /// <summary>
    /// Commit SHA at worktree creation.
    /// </summary>
    public string CommitSha { get; }
    
    /// <summary>
    /// When the mapping was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }
    
    /// <summary>
    /// When the worktree was last accessed.
    /// </summary>
    public DateTimeOffset LastAccessedAt { get; private set; }
    
    /// <summary>
    /// Optional notes or description.
    /// </summary>
    public string? Notes { get; set; }
    
    private WorktreeMapping(
        string worktreePath,
        string? taskId,
        string branchName,
        string commitSha,
        DateTimeOffset createdAt,
        DateTimeOffset lastAccessedAt,
        string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(worktreePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(branchName);
        ArgumentException.ThrowIfNullOrWhiteSpace(commitSha);
        
        WorktreePath = NormalizePath(worktreePath);
        TaskId = taskId;
        BranchName = branchName;
        CommitSha = commitSha;
        CreatedAt = createdAt;
        LastAccessedAt = lastAccessedAt;
        Notes = notes;
    }
    
    /// <summary>
    /// Creates a new worktree mapping.
    /// </summary>
    public static WorktreeMapping Create(
        string worktreePath,
        string? taskId,
        string branchName,
        string commitSha,
        string? notes = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new WorktreeMapping(
            worktreePath,
            taskId,
            branchName,
            commitSha,
            now,
            now,
            notes);
    }
    
    /// <summary>
    /// Reconstitutes from database.
    /// </summary>
    internal static WorktreeMapping FromDatabase(
        int id,
        string worktreePath,
        string? taskId,
        string branchName,
        string commitSha,
        DateTimeOffset createdAt,
        DateTimeOffset lastAccessedAt,
        string? notes)
    {
        var mapping = new WorktreeMapping(
            worktreePath, taskId, branchName, commitSha,
            createdAt, lastAccessedAt, notes);
        mapping.Id = new WorktreeMappingId(id);
        return mapping;
    }
    
    /// <summary>
    /// Updates the last accessed timestamp.
    /// </summary>
    public void Touch()
    {
        LastAccessedAt = DateTimeOffset.UtcNow;
    }
    
    /// <summary>
    /// Assigns database ID after insert.
    /// </summary>
    internal void SetId(WorktreeMappingId id)
    {
        if (Id.HasValue)
            throw new InvalidOperationException("ID already set");
        Id = id;
    }
    
    private static string NormalizePath(string path)
    {
        // Normalize path separators and case for comparison
        var normalized = Path.GetFullPath(path);
        return normalized.Replace('\\', '/').TrimEnd('/');
    }
}

/// <summary>
/// Result of reconciliation between DB and filesystem.
/// </summary>
public sealed record ReconciliationResult
{
    /// <summary>Mappings whose worktree directories no longer exist.</summary>
    public required IReadOnlyList<WorktreeMapping> MissingWorktrees { get; init; }
    
    /// <summary>Worktrees found on filesystem without mappings.</summary>
    public required IReadOnlyList<string> UnmappedWorktrees { get; init; }
    
    /// <summary>Mappings with valid worktrees.</summary>
    public required IReadOnlyList<WorktreeMapping> ValidMappings { get; init; }
    
    public bool HasIssues => MissingWorktrees.Count > 0 || UnmappedWorktrees.Count > 0;
}
```

### Repository Interface

```csharp
// Acode.Application/Services/Git/IWorktreeMappingRepository.cs
namespace Acode.Application.Services.Git;

/// <summary>
/// Repository for worktree-task mappings.
/// </summary>
public interface IWorktreeMappingRepository
{
    /// <summary>
    /// Creates a new mapping.
    /// </summary>
    /// <exception cref="DuplicateMappingException">Mapping already exists.</exception>
    Task CreateAsync(WorktreeMapping mapping, CancellationToken ct = default);
    
    /// <summary>
    /// Gets mapping by task ID.
    /// </summary>
    Task<WorktreeMapping?> GetByTaskAsync(string taskId, CancellationToken ct = default);
    
    /// <summary>
    /// Gets mapping by worktree path.
    /// </summary>
    Task<WorktreeMapping?> GetByPathAsync(string path, CancellationToken ct = default);
    
    /// <summary>
    /// Deletes mapping by worktree path.
    /// </summary>
    Task DeleteByPathAsync(string path, CancellationToken ct = default);
    
    /// <summary>
    /// Deletes mapping by task ID.
    /// </summary>
    Task DeleteByTaskAsync(string taskId, CancellationToken ct = default);
    
    /// <summary>
    /// Lists all mappings.
    /// </summary>
    Task<IReadOnlyList<WorktreeMapping>> ListAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Lists mappings with missing worktree directories.
    /// </summary>
    Task<IReadOnlyList<WorktreeMapping>> GetStaleMappingsAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Updates last accessed timestamp.
    /// </summary>
    Task TouchAsync(string path, CancellationToken ct = default);
    
    /// <summary>
    /// Updates mapping notes.
    /// </summary>
    Task UpdateNotesAsync(string path, string? notes, CancellationToken ct = default);
    
    /// <summary>
    /// Deletes all mappings where worktree directory is missing.
    /// </summary>
    Task<int> PurgeStaleMappingsAsync(CancellationToken ct = default);
}

/// <summary>
/// Exception thrown when creating a duplicate mapping.
/// </summary>
public sealed class DuplicateMappingException : Exception
{
    public string? TaskId { get; }
    public string? WorktreePath { get; }
    
    public DuplicateMappingException(string message, string? taskId = null, string? path = null)
        : base(message)
    {
        TaskId = taskId;
        WorktreePath = path;
    }
}
```

### Service Interface

```csharp
// Acode.Application/Services/Git/IWorktreeMappingService.cs
namespace Acode.Application.Services.Git;

/// <summary>
/// Service for managing worktree-task mappings with reconciliation.
/// </summary>
public interface IWorktreeMappingService
{
    /// <summary>
    /// Creates mapping when a worktree is created for a task.
    /// </summary>
    Task CreateMappingAsync(
        string worktreePath,
        string? taskId,
        string branchName,
        string commitSha,
        CancellationToken ct = default);
    
    /// <summary>
    /// Removes mapping when a worktree is removed.
    /// </summary>
    Task RemoveMappingAsync(string worktreePath, CancellationToken ct = default);
    
    /// <summary>
    /// Gets the worktree path for a task.
    /// </summary>
    Task<string?> GetWorktreeForTaskAsync(string taskId, CancellationToken ct = default);
    
    /// <summary>
    /// Gets the task ID for a worktree.
    /// </summary>
    Task<string?> GetTaskForWorktreeAsync(string worktreePath, CancellationToken ct = default);
    
    /// <summary>
    /// Records access to a worktree.
    /// </summary>
    Task RecordAccessAsync(string worktreePath, CancellationToken ct = default);
    
    /// <summary>
    /// Reconciles database state with filesystem and Git state.
    /// </summary>
    Task<ReconciliationResult> ReconcileAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Gets orphaned mappings (worktree exists but task completed/missing).
    /// </summary>
    Task<IReadOnlyList<WorktreeMapping>> GetOrphanedMappingsAsync(
        IEnumerable<string> activeTaskIds,
        CancellationToken ct = default);
}
```

### Repository Implementation

```csharp
// Acode.Infrastructure/Persistence/WorktreeMappingRepository.cs
namespace Acode.Infrastructure.Persistence;

/// <summary>
/// SQLite implementation of worktree mapping repository.
/// </summary>
public sealed class WorktreeMappingRepository : IWorktreeMappingRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<WorktreeMappingRepository> _logger;
    private readonly SemaphoreSlim _accessUpdateLock = new(1, 1);
    private readonly Dictionary<string, DateTimeOffset> _pendingAccessUpdates = new();
    private readonly TimeSpan _accessUpdateDebounce = TimeSpan.FromSeconds(30);
    
    public WorktreeMappingRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<WorktreeMappingRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }
    
    public async Task CreateAsync(WorktreeMapping mapping, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        
        const string sql = """
            INSERT INTO worktree_mappings 
                (worktree_path, task_id, branch_name, commit_sha, 
                 created_at, last_accessed_at, notes)
            VALUES 
                (@Path, @TaskId, @Branch, @Commit, 
                 @CreatedAt, @LastAccessed, @Notes)
            RETURNING id
            """;
        
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        
        try
        {
            var id = await connection.ExecuteScalarAsync<int>(sql, new
            {
                Path = mapping.WorktreePath,
                TaskId = mapping.TaskId,
                Branch = mapping.BranchName,
                Commit = mapping.CommitSha,
                CreatedAt = mapping.CreatedAt.ToString("O"),
                LastAccessed = mapping.LastAccessedAt.ToString("O"),
                Notes = mapping.Notes
            });
            
            mapping.SetId(new WorktreeMappingId(id));
            
            _logger.LogInformation(
                "Created worktree mapping: {Path} -> {TaskId}",
                mapping.WorktreePath, mapping.TaskId ?? "(no task)");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // UNIQUE constraint
        {
            if (ex.Message.Contains("task_id"))
            {
                throw new DuplicateMappingException(
                    $"Mapping for task '{mapping.TaskId}' already exists",
                    taskId: mapping.TaskId);
            }
            if (ex.Message.Contains("worktree_path"))
            {
                throw new DuplicateMappingException(
                    $"Mapping for path '{mapping.WorktreePath}' already exists",
                    path: mapping.WorktreePath);
            }
            throw;
        }
    }
    
    public async Task<WorktreeMapping?> GetByTaskAsync(string taskId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        
        const string sql = """
            SELECT id, worktree_path, task_id, branch_name, commit_sha,
                   created_at, last_accessed_at, notes
            FROM worktree_mappings
            WHERE task_id = @TaskId
            """;
        
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<MappingRow>(sql, new { TaskId = taskId });
        
        return row?.ToMapping();
    }
    
    public async Task<WorktreeMapping?> GetByPathAsync(string path, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        
        var normalizedPath = NormalizePath(path);
        
        const string sql = """
            SELECT id, worktree_path, task_id, branch_name, commit_sha,
                   created_at, last_accessed_at, notes
            FROM worktree_mappings
            WHERE worktree_path = @Path
            """;
        
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<MappingRow>(sql, new { Path = normalizedPath });
        
        return row?.ToMapping();
    }
    
    public async Task DeleteByPathAsync(string path, CancellationToken ct = default)
    {
        var normalizedPath = NormalizePath(path);
        
        const string sql = "DELETE FROM worktree_mappings WHERE worktree_path = @Path";
        
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        var affected = await connection.ExecuteAsync(sql, new { Path = normalizedPath });
        
        if (affected > 0)
        {
            _logger.LogInformation("Deleted worktree mapping for path: {Path}", path);
        }
    }
    
    public async Task DeleteByTaskAsync(string taskId, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM worktree_mappings WHERE task_id = @TaskId";
        
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        var affected = await connection.ExecuteAsync(sql, new { TaskId = taskId });
        
        if (affected > 0)
        {
            _logger.LogInformation("Deleted worktree mapping for task: {TaskId}", taskId);
        }
    }
    
    public async Task<IReadOnlyList<WorktreeMapping>> ListAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, worktree_path, task_id, branch_name, commit_sha,
                   created_at, last_accessed_at, notes
            FROM worktree_mappings
            ORDER BY created_at DESC
            """;
        
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        var rows = await connection.QueryAsync<MappingRow>(sql);
        
        return rows.Select(r => r.ToMapping()).ToList();
    }
    
    public async Task<IReadOnlyList<WorktreeMapping>> GetStaleMappingsAsync(CancellationToken ct = default)
    {
        // Get all mappings, then filter to those with missing directories
        var allMappings = await ListAsync(ct);
        
        return allMappings
            .Where(m => !Directory.Exists(m.WorktreePath))
            .ToList();
    }
    
    public async Task TouchAsync(string path, CancellationToken ct = default)
    {
        var normalizedPath = NormalizePath(path);
        var now = DateTimeOffset.UtcNow;
        
        // Debounce access updates to reduce DB writes
        await _accessUpdateLock.WaitAsync(ct);
        try
        {
            if (_pendingAccessUpdates.TryGetValue(normalizedPath, out var lastUpdate))
            {
                if (now - lastUpdate < _accessUpdateDebounce)
                {
                    return; // Skip - too soon since last update
                }
            }
            
            _pendingAccessUpdates[normalizedPath] = now;
        }
        finally
        {
            _accessUpdateLock.Release();
        }
        
        const string sql = """
            UPDATE worktree_mappings 
            SET last_accessed_at = @Now 
            WHERE worktree_path = @Path
            """;
        
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        await connection.ExecuteAsync(sql, new { Path = normalizedPath, Now = now.ToString("O") });
    }
    
    public async Task UpdateNotesAsync(string path, string? notes, CancellationToken ct = default)
    {
        var normalizedPath = NormalizePath(path);
        
        const string sql = """
            UPDATE worktree_mappings 
            SET notes = @Notes 
            WHERE worktree_path = @Path
            """;
        
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        await connection.ExecuteAsync(sql, new { Path = normalizedPath, Notes = notes });
    }
    
    public async Task<int> PurgeStaleMappingsAsync(CancellationToken ct = default)
    {
        var staleMappings = await GetStaleMappingsAsync(ct);
        
        if (staleMappings.Count == 0)
            return 0;
        
        var paths = staleMappings.Select(m => m.WorktreePath).ToList();
        
        const string sql = "DELETE FROM worktree_mappings WHERE worktree_path = @Path";
        
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        
        var deleted = 0;
        foreach (var path in paths)
        {
            deleted += await connection.ExecuteAsync(sql, new { Path = path });
        }
        
        _logger.LogInformation("Purged {Count} stale worktree mappings", deleted);
        return deleted;
    }
    
    private static string NormalizePath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return fullPath.Replace('\\', '/').TrimEnd('/');
    }
    
    // Internal record for Dapper mapping
    private sealed record MappingRow(
        int Id,
        string Worktree_Path,
        string? Task_Id,
        string Branch_Name,
        string Commit_Sha,
        string Created_At,
        string Last_Accessed_At,
        string? Notes)
    {
        public WorktreeMapping ToMapping()
        {
            return WorktreeMapping.FromDatabase(
                Id,
                Worktree_Path,
                Task_Id,
                Branch_Name,
                Commit_Sha,
                DateTimeOffset.Parse(Created_At),
                DateTimeOffset.Parse(Last_Accessed_At),
                Notes);
        }
    }
}
```

### Migration Script

```sql
-- Acode.Infrastructure/Persistence/Migrations/003_WorktreeMappings.sql
-- Migration: 003
-- Description: Create worktree_mappings table

CREATE TABLE IF NOT EXISTS worktree_mappings (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    worktree_path TEXT NOT NULL UNIQUE,
    task_id TEXT UNIQUE,
    branch_name TEXT NOT NULL,
    commit_sha TEXT NOT NULL,
    created_at TEXT NOT NULL,
    last_accessed_at TEXT NOT NULL,
    notes TEXT
);

-- Index for task lookups
CREATE INDEX IF NOT EXISTS idx_worktree_mappings_task_id 
ON worktree_mappings(task_id) WHERE task_id IS NOT NULL;

-- Index for path lookups
CREATE INDEX IF NOT EXISTS idx_worktree_mappings_path 
ON worktree_mappings(worktree_path);

-- Index for age-based cleanup queries
CREATE INDEX IF NOT EXISTS idx_worktree_mappings_last_accessed 
ON worktree_mappings(last_accessed_at);
```

### Service Implementation

```csharp
// Acode.Application/Services/Git/WorktreeMappingService.cs
namespace Acode.Application.Services.Git;

/// <summary>
/// Service coordinating worktree mappings with reconciliation.
/// </summary>
public sealed class WorktreeMappingService : IWorktreeMappingService
{
    private readonly IWorktreeMappingRepository _repository;
    private readonly IWorktreeService _worktreeService;
    private readonly ILogger<WorktreeMappingService> _logger;
    
    public WorktreeMappingService(
        IWorktreeMappingRepository repository,
        IWorktreeService worktreeService,
        ILogger<WorktreeMappingService> logger)
    {
        _repository = repository;
        _worktreeService = worktreeService;
        _logger = logger;
    }
    
    public async Task CreateMappingAsync(
        string worktreePath,
        string? taskId,
        string branchName,
        string commitSha,
        CancellationToken ct = default)
    {
        var mapping = WorktreeMapping.Create(
            worktreePath, taskId, branchName, commitSha);
        
        await _repository.CreateAsync(mapping, ct);
    }
    
    public async Task RemoveMappingAsync(string worktreePath, CancellationToken ct = default)
    {
        await _repository.DeleteByPathAsync(worktreePath, ct);
    }
    
    public async Task<string?> GetWorktreeForTaskAsync(string taskId, CancellationToken ct = default)
    {
        var mapping = await _repository.GetByTaskAsync(taskId, ct);
        return mapping?.WorktreePath;
    }
    
    public async Task<string?> GetTaskForWorktreeAsync(string worktreePath, CancellationToken ct = default)
    {
        var mapping = await _repository.GetByPathAsync(worktreePath, ct);
        return mapping?.TaskId;
    }
    
    public async Task RecordAccessAsync(string worktreePath, CancellationToken ct = default)
    {
        await _repository.TouchAsync(worktreePath, ct);
    }
    
    public async Task<ReconciliationResult> ReconcileAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting worktree mapping reconciliation");
        
        // Get all mappings from DB
        var mappings = await _repository.ListAsync(ct);
        
        // Get all worktrees from Git
        var worktrees = await _worktreeService.ListAsync(ct);
        var worktreePaths = worktrees
            .Where(w => !w.IsMain)  // Exclude main worktree
            .Select(w => NormalizePath(w.Path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        
        var mappingPaths = mappings
            .Select(m => NormalizePath(m.WorktreePath))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        
        // Find missing worktrees (mapping exists, worktree doesn't)
        var missingWorktrees = mappings
            .Where(m => !worktreePaths.Contains(NormalizePath(m.WorktreePath)))
            .ToList();
        
        // Find unmapped worktrees (worktree exists, no mapping)
        var unmappedWorktrees = worktreePaths
            .Where(p => !mappingPaths.Contains(p))
            .ToList();
        
        // Valid mappings
        var validMappings = mappings
            .Where(m => worktreePaths.Contains(NormalizePath(m.WorktreePath)))
            .ToList();
        
        var result = new ReconciliationResult
        {
            MissingWorktrees = missingWorktrees,
            UnmappedWorktrees = unmappedWorktrees,
            ValidMappings = validMappings
        };
        
        if (result.HasIssues)
        {
            _logger.LogWarning(
                "Reconciliation found issues: {Missing} missing worktrees, {Unmapped} unmapped worktrees",
                missingWorktrees.Count, unmappedWorktrees.Count);
        }
        else
        {
            _logger.LogInformation(
                "Reconciliation complete: {Valid} valid mappings, no issues",
                validMappings.Count);
        }
        
        return result;
    }
    
    public async Task<IReadOnlyList<WorktreeMapping>> GetOrphanedMappingsAsync(
        IEnumerable<string> activeTaskIds,
        CancellationToken ct = default)
    {
        var activeSet = new HashSet<string>(
            activeTaskIds,
            StringComparer.OrdinalIgnoreCase);
        
        var mappings = await _repository.ListAsync(ct);
        
        // Orphaned = has task ID but task is not active
        return mappings
            .Where(m => m.TaskId != null && !activeSet.Contains(m.TaskId))
            .ToList();
    }
    
    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path).Replace('\\', '/').TrimEnd('/');
    }
}
```

### CLI Commands

```csharp
// Acode.Cli/Commands/Worktree/WorktreeMappingCommands.cs
namespace Acode.Cli.Commands.Worktree;

[Command("worktree mappings", Description = "Show worktree-task mappings")]
public sealed class WorktreeMappingsCommand : ICommand
{
    [CommandOption("json", Description = "Output as JSON")]
    public bool Json { get; init; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var repository = GetMappingRepository(); // DI
        var mappings = await repository.ListAsync();
        
        if (Json)
        {
            var json = JsonSerializer.Serialize(mappings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            console.Output.WriteLine(json);
            return;
        }
        
        if (mappings.Count == 0)
        {
            console.Output.WriteLine("No worktree mappings found.");
            return;
        }
        
        foreach (var mapping in mappings)
        {
            console.Output.WriteLine($"Path:     {mapping.WorktreePath}");
            console.Output.WriteLine($"  Task:   {mapping.TaskId ?? "(none)"}");
            console.Output.WriteLine($"  Branch: {mapping.BranchName}");
            console.Output.WriteLine($"  Commit: {mapping.CommitSha[..8]}");
            console.Output.WriteLine($"  Age:    {FormatAge(mapping.CreatedAt)}");
            console.Output.WriteLine();
        }
        
        console.Output.WriteLine($"Total: {mappings.Count} mappings");
    }
    
    private static string FormatAge(DateTimeOffset created)
    {
        var age = DateTimeOffset.UtcNow - created;
        if (age.TotalDays >= 1) return $"{(int)age.TotalDays}d ago";
        if (age.TotalHours >= 1) return $"{(int)age.TotalHours}h ago";
        return $"{(int)age.TotalMinutes}m ago";
    }
}

[Command("worktree reconcile", Description = "Reconcile worktree mappings with filesystem")]
public sealed class WorktreeReconcileCommand : ICommand
{
    [CommandOption("purge", Description = "Purge stale mappings")]
    public bool Purge { get; init; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var service = GetMappingService();
        
        var result = await service.ReconcileAsync();
        
        if (!result.HasIssues)
        {
            console.Output.WriteLine($"✓ All {result.ValidMappings.Count} mappings are valid.");
            return;
        }
        
        if (result.MissingWorktrees.Count > 0)
        {
            console.Output.WriteLine($"Missing worktrees ({result.MissingWorktrees.Count}):");
            foreach (var mapping in result.MissingWorktrees)
            {
                console.Output.WriteLine($"  - {mapping.WorktreePath}");
                console.Output.WriteLine($"    Task: {mapping.TaskId ?? "(none)"}");
            }
            console.Output.WriteLine();
        }
        
        if (result.UnmappedWorktrees.Count > 0)
        {
            console.Output.WriteLine($"Unmapped worktrees ({result.UnmappedWorktrees.Count}):");
            foreach (var path in result.UnmappedWorktrees)
            {
                console.Output.WriteLine($"  - {path}");
            }
            console.Output.WriteLine();
        }
        
        if (Purge && result.MissingWorktrees.Count > 0)
        {
            var repository = GetMappingRepository();
            var purged = await repository.PurgeStaleMappingsAsync();
            console.Output.WriteLine($"Purged {purged} stale mappings.");
        }
        else if (result.MissingWorktrees.Count > 0)
        {
            console.Output.WriteLine("Use --purge to remove stale mappings.");
        }
    }
}

[Command("worktree show-task", Description = "Show worktree for a task")]
public sealed class WorktreeShowTaskCommand : ICommand
{
    [CommandParameter(0, Description = "Task ID")]
    public required string TaskId { get; init; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var service = GetMappingService();
        var path = await service.GetWorktreeForTaskAsync(TaskId);
        
        if (path == null)
        {
            console.Output.WriteLine($"No worktree found for task: {TaskId}");
            Environment.ExitCode = 1;
            return;
        }
        
        console.Output.WriteLine(path);
    }
}
```

### Implementation Checklist

- [ ] Create `WorktreeMappingId` value object
- [ ] Create `WorktreeMapping` entity with factory methods
- [ ] Create `ReconciliationResult` record
- [ ] Define `IWorktreeMappingRepository` interface
- [ ] Create `DuplicateMappingException`
- [ ] Define `IWorktreeMappingService` interface
- [ ] Create migration script (003_WorktreeMappings.sql)
- [ ] Implement `WorktreeMappingRepository.CreateAsync` with duplicate handling
- [ ] Implement `WorktreeMappingRepository.GetByTaskAsync` with index
- [ ] Implement `WorktreeMappingRepository.GetByPathAsync` with index
- [ ] Implement `WorktreeMappingRepository.DeleteByPathAsync`
- [ ] Implement `WorktreeMappingRepository.DeleteByTaskAsync`
- [ ] Implement `WorktreeMappingRepository.ListAsync`
- [ ] Implement `WorktreeMappingRepository.GetStaleMappingsAsync`
- [ ] Implement `WorktreeMappingRepository.TouchAsync` with debouncing
- [ ] Implement `WorktreeMappingRepository.PurgeStaleMappingsAsync`
- [ ] Implement `WorktreeMappingService.ReconcileAsync`
- [ ] Implement `WorktreeMappingService.GetOrphanedMappingsAsync`
- [ ] Register repository and service in DI
- [ ] Create `WorktreeMappingsCommand` CLI
- [ ] Create `WorktreeReconcileCommand` CLI
- [ ] Create `WorktreeShowTaskCommand` CLI
- [ ] Write unit tests for mapping entity
- [ ] Write integration tests for repository
- [ ] Write integration tests for reconciliation

### Rollout Plan

1. **Phase 1: Domain Models** (Day 1)
   - Create mapping entity and value objects
   - Unit test entity behavior

2. **Phase 2: Database** (Day 1)
   - Create migration script
   - Test migration applies cleanly

3. **Phase 3: Repository** (Day 2)
   - Implement repository with all operations
   - Add debounced access updates
   - Integration test with real DB

4. **Phase 4: Service** (Day 2)
   - Implement mapping service
   - Add reconciliation logic
   - Test with mock worktree service

5. **Phase 5: CLI** (Day 3)
   - Implement CLI commands
   - Manual testing with scenarios

6. **Phase 6: Integration** (Day 3)
   - Wire up with worktree service
   - End-to-end testing
   - Performance validation

---

**End of Task 023.b Specification**