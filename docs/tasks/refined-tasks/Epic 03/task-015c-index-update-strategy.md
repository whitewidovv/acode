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

### Business Value

An index is only valuable if it reflects the current state of the codebase. As developers and the agent itself make changes to files, the index becomes stale. Without an efficient update strategy, users would be forced to choose between slow full rebuilds after every change or working with outdated search results that miss recent modifications. The index update strategy solves this by enabling fast, incremental updates that keep the index fresh without the cost of full rebuilds.

The business value is particularly pronounced in agent-assisted workflows. When the agent modifies files, those changes should immediately be searchable for subsequent operations. Without post-write update triggers, the agent could search for code it just wrote and not find it, leading to confusion and incorrect behavior. Automatic update triggers ensure the agent always works with current information.

Furthermore, the update strategy directly impacts user experience. Progress reporting and cancellation support ensure that users understand what the system is doing during long operations. Atomic updates guarantee that an interrupted update doesn't corrupt the index. These reliability features build trust and enable confident use of the indexing system in production workflows.

### Scope

1. **Change Detection** - File modification time (mtime) based detection of new, modified, and deleted files since last index update
2. **Incremental Updater** - Efficient update mechanism that processes only changed files, preserving unchanged index entries
3. **Update Triggers** - Configurable automatic triggers for startup, pre-search, post-write, and staleness-based updates
4. **Batching System** - Grouping of file operations for efficient processing with configurable batch sizes and checkpoints
5. **Atomicity Guarantees** - Transaction-style updates with rollback on failure to prevent index corruption

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Index Service | Primary | Receives update commands and coordinates with storage layer |
| File System | Provider | Scanned for file changes via mtime comparison |
| Agent Write Service | Trigger | Triggers post-write updates when agent modifies files |
| Search Service | Trigger | Triggers pre-search updates when staleness threshold exceeded |
| CLI Commands | Interface | Exposes manual update and rebuild commands |
| Configuration Service | Provider | Supplies update trigger settings and threshold values |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| File deleted during scan | Stale data or missing entry | Handle file-not-found gracefully, skip missing files |
| Disk full during update | Update cannot complete | Rollback transaction, report clear error with space needed |
| File locked by another process | Cannot read file for indexing | Retry with backoff, eventually skip with warning |
| Concurrent update attempts | Potential corruption or deadlock | Queue concurrent updates, serialize execution |
| Power loss during update | Partially written index | Atomic write with temp file and rename; recovery on startup |
| Timezone change | mtime comparisons incorrect | Use UTC timestamps internally, revalidate on timezone detection |

### Assumptions

1. File modification time (mtime) is reliable for detecting changes on all supported operating systems
2. The file system maintains accurate and consistent mtime values across file operations
3. Most update operations will be incremental with a small percentage of files changed
4. Full rebuilds are acceptable for initial index creation or corruption recovery
5. Batching provides significant performance improvement over individual file processing
6. Staleness thresholds are configurable to meet different workflow requirements
7. Users prefer background updates that don't block their primary workflow
8. Atomic update guarantees are achievable using file system rename operations

### Security Considerations

1. **File Access Verification** - Update process must verify file access permissions before reading, avoiding permission-denied errors
2. **Temporary File Security** - Temporary files used for atomic updates must be created with restricted permissions
3. **Lock File Integrity** - Lock files for concurrent access control must be protected against tampering
4. **Path Validation** - Changed file paths must be validated to prevent directory traversal attacks
5. **Resource Limits** - Update operations must respect resource limits to prevent denial of service during large updates

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

| ID | Requirement |
|----|-------------|
| FR-015c-01 | The system MUST detect modified files by comparing file mtime to indexed mtime |
| FR-015c-02 | The system MUST detect new files not present in the current index |
| FR-015c-03 | The system MUST detect deleted files no longer present in the file system |
| FR-015c-04 | The system MUST detect files with changed content (modified mtime) |
| FR-015c-05 | The system MUST handle timezone changes without false positive detections |

### Update Triggers

| ID | Requirement |
|----|-------------|
| FR-015c-06 | The system MUST support manual update invocation via CLI command |
| FR-015c-07 | The system MUST support optional automatic update on application startup |
| FR-015c-08 | The system MUST support optional automatic update before search if stale |
| FR-015c-09 | The system MUST support optional automatic update after agent writes files |
| FR-015c-10 | All update triggers MUST be configurable via .agent/config.yml |

### Incremental Update

| ID | Requirement |
|----|-------------|
| FR-015c-11 | The system MUST update only files detected as changed |
| FR-015c-12 | The system MUST skip unchanged files without re-processing |
| FR-015c-13 | The system MUST remove index entries for deleted files |
| FR-015c-14 | The system MUST add index entries for new files |
| FR-015c-15 | The system MUST preserve index entries for unchanged files |

### Full Rebuild

| ID | Requirement |
|----|-------------|
| FR-015c-16 | RebuildAsync() MUST perform a complete index rebuild from scratch |
| FR-015c-17 | Rebuild MUST clear all existing index entries before reindexing |
| FR-015c-18 | Rebuild MUST index all files matching the inclusion criteria |
| FR-015c-19 | Rebuild MUST report progress during the operation |

### Batching

| ID | Requirement |
|----|-------------|
| FR-015c-20 | The system MUST batch file system scans for efficient enumeration |
| FR-015c-21 | The system MUST batch index updates for efficient persistence |
| FR-015c-22 | Batch sizes MUST be configurable via settings |
| FR-015c-23 | The system MUST manage memory during large batches to prevent exhaustion |

### Staleness

| ID | Requirement |
|----|-------------|
| FR-015c-24 | The system MUST track the timestamp of the last successful index update |
| FR-015c-25 | The staleness threshold MUST be configurable in seconds |
| FR-015c-26 | The system MUST force update when staleness threshold is exceeded |
| FR-015c-27 | The system MUST report current staleness status via status command |

### Progress

| ID | Requirement |
|----|-------------|
| FR-015c-28 | The system MUST report file scan progress during detection phase |
| FR-015c-29 | The system MUST report index update progress during update phase |
| FR-015c-30 | The system MUST calculate and display estimated time to completion |
| FR-015c-31 | The system MUST support cancellation of in-progress updates |

### Atomicity

| ID | Requirement |
|----|-------------|
| FR-015c-32 | Index updates MUST be atomic - all changes apply or none |
| FR-015c-33 | The system MUST rollback to previous state on update failure |
| FR-015c-34 | The system MUST NOT leave the index in a partially updated state |
| FR-015c-35 | The system MUST support periodic checkpoints for long-running updates |

### Concurrency

| ID | Requirement |
|----|-------------|
| FR-015c-36 | The system MUST handle files changing during the scan operation |
| FR-015c-37 | The system MUST lock the index during write operations |
| FR-015c-38 | The system MUST allow read operations during index updates |
| FR-015c-39 | The system MUST queue concurrent update requests for serialized execution |

---

## Non-Functional Requirements

### Performance

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015c-01 | Performance | Scanning 10,000 files for changes MUST complete in less than 5 seconds |
| NFR-015c-02 | Performance | Updating 100 changed files MUST complete in less than 2 seconds |
| NFR-015c-03 | Performance | Startup staleness check MUST complete in less than 500ms |

### Reliability

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015c-04 | Reliability | Index updates MUST be atomic with rollback on failure |
| NFR-015c-05 | Reliability | The system MUST recover from interrupted updates without corruption |
| NFR-015c-06 | Reliability | The system MUST degrade gracefully when update capacity is exceeded |

### Usability

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015c-07 | Usability | Progress display MUST clearly show current phase and completion percentage |
| NFR-015c-08 | Usability | Default update settings MUST work for common project sizes |
| NFR-015c-09 | Usability | Update activity MUST be logged at appropriate verbosity levels |

### Maintainability

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015c-10 | Maintainability | Change detection logic MUST be extensible for future hash-based detection |
| NFR-015c-11 | Maintainability | Update triggers MUST be pluggable for adding new trigger types |
| NFR-015c-12 | Maintainability | Batching parameters MUST be tunable without code changes |

### Observability

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015c-13 | Observability | Update duration and file counts MUST be recorded as metrics |
| NFR-015c-14 | Observability | Staleness age MUST be queryable for monitoring |
| NFR-015c-15 | Observability | Update failures MUST be logged with detailed error context |

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