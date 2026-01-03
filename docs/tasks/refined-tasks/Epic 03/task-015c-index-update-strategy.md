# Task 015.c: Index Update Strategy

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 015 (Indexing v1)  

---

## Description

Task 015.c defines the index update strategy. The index must stay current as files change. Updates must be efficient. Full rebuilds are too slow for routine use.

Incremental updates process only changed files. Detect what changed. Update only those entries. This is much faster than rebuilding.

Change detection uses file timestamps. Compare current mtime to indexed mtime. Different means changed. This is simple and efficient.

Multiple update triggers are supported. Manual updates via CLI. Updates on startup. Updates before search. Updates after agent writes. Each has its use case.

Update batching groups changes together. Many small files changed? Update them together. Batching reduces overhead.

Conflict handling addresses concurrent access. The agent might be writing while index updates. Updates must handle this gracefully.

Staleness tolerance is configurable. How old can the index be before mandatory update? Some use cases need fresher data than others.

Update progress is reported. Long updates show progress. Users know the system is working. Cancellation is supported.

Failure handling ensures consistency. If update fails partway, index is still valid. Either all changes apply or none. Atomic updates prevent corruption.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Incremental | Update changed only |
| Full Rebuild | Rebuild entire index |
| Change Detection | Find what changed |
| mtime | Modification time |
| Staleness | How outdated |
| Trigger | Update cause |
| Batching | Group updates |
| Concurrent | Simultaneous access |
| Atomic | All or nothing |
| Checkpoint | Progress save point |
| Delta | Changes since last |
| Scan | Check all files |
| Queue | Pending updates |
| Background | Non-blocking update |
| Foreground | Blocking update |

---

## Out of Scope

The following items are explicitly excluded from Task 015.c:

- **File system watching** - Polling only v1
- **Real-time updates** - Batch updates
- **Distributed updates** - Single machine
- **Parallel scanning** - Sequential v1
- **Hash-based detection** - mtime only v1
- **Streaming updates** - Batch processing

---

## Functional Requirements

### Change Detection

- FR-001: Detect by mtime MUST work
- FR-002: Detect new files MUST work
- FR-003: Detect deleted files MUST work
- FR-004: Detect modified files MUST work
- FR-005: Handle timezone changes

### Update Triggers

- FR-006: Manual update MUST work
- FR-007: Startup update optional
- FR-008: Pre-search update optional
- FR-009: Post-write update optional
- FR-010: Configurable triggers

### Incremental Update

- FR-011: Update changed only
- FR-012: Skip unchanged files
- FR-013: Remove deleted entries
- FR-014: Add new entries
- FR-015: Preserve unchanged entries

### Full Rebuild

- FR-016: RebuildAsync MUST work
- FR-017: Clear existing index
- FR-018: Index all files
- FR-019: Progress reporting

### Batching

- FR-020: Batch file scans
- FR-021: Batch index updates
- FR-022: Configurable batch size
- FR-023: Memory management

### Staleness

- FR-024: Track index age
- FR-025: Staleness threshold configurable
- FR-026: Force update if stale
- FR-027: Report staleness

### Progress

- FR-028: Report scan progress
- FR-029: Report update progress
- FR-030: ETA estimation
- FR-031: Cancellation support

### Atomicity

- FR-032: Atomic updates
- FR-033: Rollback on failure
- FR-034: No partial updates
- FR-035: Checkpoint support

### Concurrency

- FR-036: Handle file changes during scan
- FR-037: Lock index during write
- FR-038: Read during update OK
- FR-039: Queue concurrent updates

---

## Non-Functional Requirements

### Performance

- NFR-001: Scan 10K files < 5s
- NFR-002: Update 100 files < 2s
- NFR-003: Startup check < 500ms

### Reliability

- NFR-004: Atomic updates
- NFR-005: Corruption recovery
- NFR-006: Graceful degradation

### Usability

- NFR-007: Clear progress display
- NFR-008: Reasonable defaults
- NFR-009: Good logging

---

## User Manual Documentation

### Overview

The index update strategy keeps the index current. Updates can be manual, automatic, or triggered.

### Manual Update

```bash
$ acode index update

Checking for changes...
  Scanned: 1,234 files
  Changed: 15 files
  New: 3 files
  Deleted: 2 files

Updating index...
  [====================] 100%

Index updated in 1.2s
```

### Automatic Updates

```yaml
# .agent/config.yml
index:
  update:
    # Update on startup
    on_startup: true
    
    # Update before search if stale
    before_search: true
    
    # Staleness threshold (seconds)
    stale_after_seconds: 300  # 5 minutes
    
    # Update after agent writes
    after_write: true
```

### Staleness

The index tracks when it was last updated:

```bash
$ acode index status

Index Status
────────────────────
Files indexed: 1,234
Last updated: 5 minutes ago
Status: ⚠ Stale (threshold: 5 min)
Pending changes: ~15 files
```

### Background Updates

Long updates run in the background:

```bash
$ acode run "Add error handling"

Index updating in background...

[Agent working...]
```

### Force Rebuild

When incremental update fails:

```bash
$ acode index rebuild

Rebuilding index from scratch...
  [====================] 100%
  
Rebuild complete: 1,234 files in 8.5s
```

### Configuration

```yaml
# .agent/config.yml
index:
  update:
    # Batch size for scanning
    scan_batch_size: 500
    
    # Batch size for indexing
    index_batch_size: 100
    
    # Maximum update time before checkpoint
    checkpoint_interval_seconds: 30
    
    # Retry failed updates
    retry_count: 3
```

### Troubleshooting

#### Index Never Updates

**Problem:** Changes not reflected in search

**Solutions:**
1. Run `acode index update` manually
2. Check update triggers are enabled
3. Check file modification times

#### Updates Too Slow

**Problem:** Incremental update takes too long

**Solutions:**
1. Add more ignore patterns
2. Reduce index file size
3. Consider rebuild if very stale

#### Concurrent Update Errors

**Problem:** Update fails with lock error

**Solutions:**
1. Wait for current update
2. Don't run multiple instances
3. Check for stuck processes

---

## Acceptance Criteria

### Detection

- [ ] AC-001: mtime detection works
- [ ] AC-002: New files detected
- [ ] AC-003: Deleted files detected
- [ ] AC-004: Modified detected

### Triggers

- [ ] AC-005: Manual update works
- [ ] AC-006: Startup trigger works
- [ ] AC-007: Pre-search works
- [ ] AC-008: Post-write works

### Incremental

- [ ] AC-009: Only changed updated
- [ ] AC-010: Unchanged preserved
- [ ] AC-011: Deleted removed

### Atomicity

- [ ] AC-012: Atomic updates
- [ ] AC-013: Rollback on failure
- [ ] AC-014: No corruption

### Progress

- [ ] AC-015: Progress shown
- [ ] AC-016: Cancellable
- [ ] AC-017: ETA shown

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Index/Update/
├── ChangeDetectorTests.cs
│   ├── Should_Detect_Modified_By_Mtime()
│   ├── Should_Detect_Modified_By_Size()
│   ├── Should_Detect_New_File()
│   ├── Should_Detect_Deleted_File()
│   ├── Should_Detect_Renamed_File()
│   ├── Should_Handle_Many_Changes()
│   ├── Should_Handle_No_Changes()
│   ├── Should_Handle_Missing_Baseline()
│   ├── Should_Ignore_Excluded_Files()
│   └── Should_Track_Scan_Progress()
│
├── IncrementalUpdaterTests.cs
│   ├── Should_Update_Modified_File()
│   ├── Should_Add_New_File()
│   ├── Should_Remove_Deleted_File()
│   ├── Should_Skip_Unchanged_File()
│   ├── Should_Batch_Updates()
│   ├── Should_Handle_Large_Batch()
│   ├── Should_Report_Update_Progress()
│   ├── Should_Support_Cancellation()
│   ├── Should_Handle_Parse_Errors()
│   └── Should_Continue_After_Single_Failure()
│
├── UpdateTriggerTests.cs
│   ├── Should_Trigger_On_Startup()
│   ├── Should_Trigger_On_Stale()
│   ├── Should_Trigger_Before_Search()
│   ├── Should_Trigger_After_Write()
│   ├── Should_Respect_Stale_Threshold()
│   ├── Should_Load_Trigger_Config()
│   ├── Should_Debounce_Rapid_Triggers()
│   └── Should_Skip_If_Already_Running()
│
├── StalenessCheckerTests.cs
│   ├── Should_Report_Last_Update_Time()
│   ├── Should_Report_Pending_Count()
│   ├── Should_Determine_Stale_Status()
│   ├── Should_Respect_Threshold_Config()
│   └── Should_Handle_Never_Updated()
│
├── UpdateBatcherTests.cs
│   ├── Should_Batch_By_Count()
│   ├── Should_Checkpoint_By_Time()
│   ├── Should_Flush_On_Complete()
│   └── Should_Handle_Empty_Batch()
│
├── AtomicUpdateTests.cs
│   ├── Should_Write_Atomically()
│   ├── Should_Rollback_On_Failure()
│   ├── Should_Preserve_Index_On_Error()
│   ├── Should_Handle_Disk_Full()
│   ├── Should_Handle_Concurrent_Update()
│   └── Should_Lock_During_Update()
│
└── UpdateProgressTests.cs
    ├── Should_Report_Scan_Progress()
    ├── Should_Report_Index_Progress()
    ├── Should_Calculate_ETA()
    └── Should_Report_File_Count()
```

### Integration Tests

```
Tests/Integration/Index/Update/
├── UpdateIntegrationTests.cs
│   ├── Should_Update_Real_Index()
│   ├── Should_Handle_Large_Change_Set()
│   ├── Should_Handle_Concurrent_File_Changes()
│   ├── Should_Survive_Interruption()
│   └── Should_Recover_From_Crash()
│
└── UpdateTriggerIntegrationTests.cs
    ├── Should_Auto_Update_On_Startup()
    ├── Should_Auto_Update_Before_Search()
    └── Should_Auto_Update_After_Agent_Write()
```

### E2E Tests

```
Tests/E2E/Index/Update/
├── UpdateE2ETests.cs
│   ├── Should_Update_Via_CLI()
│   ├── Should_Rebuild_Via_CLI()
│   ├── Should_Show_Status_Via_CLI()
│   ├── Should_Detect_Agent_Write_Changes()
│   └── Should_Work_With_Background_Update()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Scan 1K files | 0.5s | 1s |
| Scan 10K files | 3s | 5s |
| Update 100 files | 1s | 2s |

---

## User Verification Steps

### Scenario 1: Manual Update

1. Modify files
2. Run `acode index update`
3. Search for changes
4. Verify: Found

### Scenario 2: Startup Update

1. Modify files
2. Restart acode
3. Search for changes
4. Verify: Found

### Scenario 3: Staleness

1. Wait for staleness
2. Run search
3. Verify: Auto-update triggers

### Scenario 4: Failure Recovery

1. Interrupt update
2. Start new update
3. Verify: Index valid

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Index/
│   └── IIndexUpdater.cs
│
src/AgenticCoder.Infrastructure/
├── Index/
│   └── Update/
│       ├── ChangeDetector.cs
│       ├── IncrementalUpdater.cs
│       ├── UpdateTriggerManager.cs
│       ├── UpdateBatcher.cs
│       └── AtomicUpdateWriter.cs
```

### IIndexUpdater Interface

```csharp
namespace AgenticCoder.Domain.Index;

public interface IIndexUpdater
{
    Task<UpdateResult> UpdateAsync(CancellationToken ct);
    Task<UpdateResult> RebuildAsync(IProgress<UpdateProgress> progress, CancellationToken ct);
    Task<StalenessInfo> CheckStalenessAsync(CancellationToken ct);
    bool IsUpdateNeeded { get; }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-UPD-001 | Detection failed |
| ACODE-UPD-002 | Update failed |
| ACODE-UPD-003 | Lock conflict |
| ACODE-UPD-004 | Cancelled |

### Implementation Checklist

1. [ ] Create change detector
2. [ ] Create incremental updater
3. [ ] Create trigger manager
4. [ ] Implement batching
5. [ ] Implement atomicity
6. [ ] Add progress reporting
7. [ ] Add CLI integration
8. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Change detection
2. **Phase 2:** Incremental update
3. **Phase 3:** Triggers
4. **Phase 4:** Atomicity
5. **Phase 5:** Progress

---

**End of Task 015.c Specification**