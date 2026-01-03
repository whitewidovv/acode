# Task 014.c: Atomic Patch Application Behavior

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 014 (RepoFS), Task 014.a (Local FS), Task 014.b (Docker FS)  

---

## Description

### Business Value

Atomic patch application is the mechanism by which Agentic Coding Bot safely modifies source files. When the agent generates code changes, those changes are expressed as unified diff patches and applied through this subsystem. Atomicity guarantees that file modifications are never partially applied—critical for maintaining codebase integrity.

The agent's primary value comes from its ability to make code changes. Without robust patch application, those changes could corrupt files: a crash mid-modification could leave files in an inconsistent state, breaking builds and requiring manual recovery. This implementation ensures that every change either fully succeeds or leaves files completely unchanged.

Beyond safety, this subsystem enables powerful development workflows: dry-run previews let developers review changes before applying them, rollback capabilities provide an undo mechanism for recent changes, and transactional multi-file patches ensure related changes across files stay synchronized. These capabilities transform the agent from a code suggestion tool into a reliable code modification system.

### Scope

This task delivers the complete atomic patch application subsystem:

1. **Unified Diff Parser:** Parses standard unified diff format (as produced by `git diff`). Extracts file paths, hunks, context lines, additions, and removals into structured patch objects.

2. **Patch Validator:** Validates patches before application. Verifies context lines match current file content, detects conflicts, and reports validation errors with clear diagnostics.

3. **Atomic Patch Applicator:** Applies patches with all-or-nothing semantics. Creates backups before modification, applies hunks in correct order, and rolls back on any failure.

4. **Dry Run Preview:** Executes full validation and shows what would change without modifying files. Enables review before committing to changes.

5. **Rollback Support:** Maintains backups of modified files for configurable retention period. Enables undoing recent patch applications.

6. **Fuzz Matching:** Handles minor line number drift when file has changed slightly since patch generation. Configurable fuzz factor controls matching tolerance.

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Task 014 (RepoFS) | Interface Extension | Adds ApplyPatchAsync, PreviewPatchAsync, RollbackPatchAsync to IRepoFS |
| Task 014.a (Local FS) | File Operations | Uses LocalFS for reading, writing, and backup storage |
| Task 014.b (Docker FS) | File Operations | Uses DockerFS when applying patches to containerized repos |
| Task 025 (File Tool) | Tool Integration | apply_patch tool uses this subsystem |
| Task 011 (Session) | Transaction Context | Session manages patch transaction boundaries |
| Task 016 (Context) | Change Tracking | Context packer tracks pending patch changes |
| Task 003.c (Audit) | Audit Logging | All patch applications logged with before/after content |
| LLM Output | Patch Source | Agent-generated patches in unified diff format |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Context mismatch | Patch cannot apply | Report conflicting lines, suggest regeneration |
| File modified since patch | Conflict detected | Clear conflict report, suggest re-analysis |
| Malformed patch | Parse failure | Validate format, report syntax errors |
| Disk full during apply | Partial state possible | Backup first, rollback on failure |
| Multi-file partial failure | Inconsistent state | Transaction rollback restores all files |
| Backup creation fails | No rollback possible | Abort before making changes |
| Encoding mismatch | Garbled content | Detect encoding, apply with matching encoding |
| Binary file in patch | Unsupported operation | Detect and reject, report clear error |

### Assumptions

1. Patches are in standard unified diff format (as produced by git diff)
2. Patches are generated against the current file content (no stale patches)
3. Files are text files with supported encodings (no binary file patches)
4. Line endings are consistent within files (LF or CRLF, not mixed)
5. Sufficient disk space exists for backup files
6. File system supports atomic rename (required for atomic writes)
7. Patches are applied sequentially (no concurrent patch application)
8. Rollback window is reasonable (minutes to hours, not days)

### Security Considerations

1. **Patch Content Validation:** Patch file paths MUST be validated against repository boundary. Patches targeting files outside repository MUST be rejected.

2. **Backup Protection:** Backup files MUST be stored securely with appropriate permissions. Backup location MUST be within repository boundary.

3. **Path Injection Prevention:** File paths extracted from patches MUST be sanitized for path traversal attempts and null byte injection.

4. **Resource Limits:** Patch size and hunk count MUST be limited to prevent resource exhaustion attacks.

5. **Audit Trail:** All patch applications MUST be logged with sufficient detail for forensic analysis if needed.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Patch | File modification description |
| Unified Diff | Standard patch format |
| Hunk | Single change block |
| Context Lines | Surrounding lines for matching |
| Atomic | All or nothing |
| Rollback | Undo changes |
| Dry Run | Preview without change |
| Conflict | Context mismatch |
| Fuzz | Line number tolerance |
| Transaction | Grouped operations |
| Backup | Pre-change copy |
| Apply | Execute patch |
| Reject | Failed hunk |
| Offset | Line number adjustment |
| Merge | Combine changes |

---

## Out of Scope

The following items are explicitly excluded from Task 014.c:

- **Three-way merge** - Simple patches only
- **Git integration** - File-level only
- **Conflict resolution** - Report only
- **Semantic merge** - Text-based only
- **Binary patches** - Text files only
- **Rename detection** - Explicit renames only
- **Permission changes** - Content only
- **Interactive editing** - Automatic only

---

## Functional Requirements

### Patch Parsing (FR-014c-01 to FR-014c-05)

| ID | Requirement |
|----|-------------|
| FR-014c-01 | System MUST parse standard unified diff format |
| FR-014c-02 | Parser MUST support multi-file patches with multiple file entries |
| FR-014c-03 | Parser MUST extract hunks with line number ranges |
| FR-014c-04 | Parser MUST extract context lines for matching |
| FR-014c-05 | Parser MUST distinguish between additions, removals, and unchanged context |

### Patch Validation (FR-014c-06 to FR-014c-10)

| ID | Requirement |
|----|-------------|
| FR-014c-06 | Validator MUST verify context lines match current file content |
| FR-014c-07 | Validator MUST verify line numbers are within file bounds |
| FR-014c-08 | Validator MUST verify target files exist (for modification patches) |
| FR-014c-09 | Validator MUST verify encoding compatibility with target files |
| FR-014c-10 | Validation errors MUST include clear, actionable error messages |

### Patch Application (FR-014c-11 to FR-014c-15)

| ID | Requirement |
|----|-------------|
| FR-014c-11 | ApplyPatchAsync MUST apply validated patches to files |
| FR-014c-12 | Application MUST correctly add new lines at specified positions |
| FR-014c-13 | Application MUST correctly remove specified lines |
| FR-014c-14 | Application MUST correctly handle line modifications (remove + add) |
| FR-014c-15 | Application MUST handle patches with multiple hunks in single file |

### Atomicity (FR-014c-16 to FR-014c-20)

| ID | Requirement |
|----|-------------|
| FR-014c-16 | Patch application MUST be all-or-nothing (no partial application) |
| FR-014c-17 | Any failure during application MUST trigger full rollback |
| FR-014c-18 | Rollback MUST restore all files to pre-patch state |
| FR-014c-19 | Multi-file patches MUST apply as single transaction |
| FR-014c-20 | Backups MUST be created before any file modification |

### Dry Run (FR-014c-21 to FR-014c-24)

| ID | Requirement |
|----|-------------|
| FR-014c-21 | PreviewPatchAsync MUST show what changes would be made |
| FR-014c-22 | Preview MUST show added and removed lines per file |
| FR-014c-23 | Preview MUST report potential conflicts without modifying files |
| FR-014c-24 | PreviewPatchAsync MUST NOT modify any files |

### Rollback (FR-014c-25 to FR-014c-28)

| ID | Requirement |
|----|-------------|
| FR-014c-25 | RollbackPatchAsync MUST restore files to pre-patch state |
| FR-014c-26 | Rollback MUST restore original content from backup |
| FR-014c-27 | Rollback window (retention period) MUST be configurable |
| FR-014c-28 | Rollback MUST cleanup backup files after successful restore |

### Conflict Detection (FR-014c-29 to FR-014c-32)

| ID | Requirement |
|----|-------------|
| FR-014c-29 | System MUST detect context line mismatches |
| FR-014c-30 | System MUST detect when expected lines are at different positions |
| FR-014c-31 | System MUST detect when file was modified since patch generation |
| FR-014c-32 | Conflicts MUST be reported with specific line numbers and content |

### Fuzz Matching (FR-014c-33 to FR-014c-36)

| ID | Requirement |
|----|-------------|
| FR-014c-33 | Fuzz factor MUST be configurable (number of lines to search) |
| FR-014c-34 | Default fuzz factor MUST be 3 lines |
| FR-014c-35 | System MUST track line offset when fuzz matching succeeds |
| FR-014c-36 | Applied offset MUST be reported in patch result |

### Multi-File Patches (FR-014c-37 to FR-014c-40)

| ID | Requirement |
|----|-------------|
| FR-014c-37 | Parser MUST correctly separate files in multi-file patches |
| FR-014c-38 | Multi-file patches MUST apply as single atomic transaction |
| FR-014c-39 | Failure in any file MUST rollback all files in patch |
| FR-014c-40 | Result MUST report status for each file in patch |

### Result Reporting (FR-014c-41 to FR-014c-45)

| ID | Requirement |
|----|-------------|
| FR-014c-41 | PatchResult MUST indicate success or failure |
| FR-014c-42 | PatchResult MUST include count of applied hunks |
| FR-014c-43 | PatchResult MUST include details of any rejected hunks |
| FR-014c-44 | PatchResult MUST include offset information if fuzz applied |
| FR-014c-45 | PatchResult MUST include conflict details if validation failed |

---

## Non-Functional Requirements

### Performance (NFR-014c-01 to NFR-014c-03)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014c-01 | Performance | Simple single-hunk patch MUST apply in < 10ms |
| NFR-014c-02 | Performance | Complex multi-hunk patch MUST apply in < 100ms |
| NFR-014c-03 | Performance | Multi-file patches MUST apply at < 50ms per file |

### Reliability (NFR-014c-04 to NFR-014c-06)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014c-04 | Reliability | Partial patch application MUST never occur |
| NFR-014c-05 | Reliability | Rollback MUST always succeed when backups exist |
| NFR-014c-06 | Reliability | File corruption from patch operations MUST be impossible |

### Safety (NFR-014c-07 to NFR-014c-09)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014c-07 | Safety | Backup MUST be created before any file modification |
| NFR-014c-08 | Safety | Patch MUST be fully validated before any modification |
| NFR-014c-09 | Safety | Error messages MUST provide clear guidance for resolution |

### Maintainability (NFR-014c-10 to NFR-014c-12)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-014c-10 | Maintainability | Dry run output MUST clearly show intended changes |
| NFR-014c-11 | Maintainability | Conflict messages MUST identify specific mismatched content |
| NFR-014c-12 | Maintainability | All patch operations MUST be logged with sufficient detail |

---

## User Manual Documentation

### Overview

The patch system applies changes to files atomically. Patches are unified diffs generated by the agent or external tools.

### Unified Diff Format

```diff
--- a/src/Program.cs
+++ b/src/Program.cs
@@ -10,6 +10,7 @@
 using System;
 using System.Collections.Generic;
+using System.Linq;
 
 namespace MyApp
 {
```

Key parts:
- `---` / `+++`: Old and new file paths
- `@@`: Hunk header (line numbers)
- `-`: Removed line
- `+`: Added line
- ` `: Context line (unchanged)

### Applying Patches

```csharp
var patch = @"
--- a/src/Program.cs
+++ b/src/Program.cs
@@ -10,3 +10,4 @@
 using System;
+using System.Linq;
";

var result = await repoFS.ApplyPatchAsync(patch);

if (result.Success)
{
    Console.WriteLine($"Applied {result.HunksApplied} hunks");
}
else
{
    Console.WriteLine($"Failed: {result.Error}");
    foreach (var conflict in result.Conflicts)
    {
        Console.WriteLine($"  {conflict.File}: {conflict.Reason}");
    }
}
```

### Dry Run

Preview changes before applying:

```csharp
var preview = await repoFS.PreviewPatchAsync(patch);

foreach (var file in preview.Files)
{
    Console.WriteLine($"Would modify: {file.Path}");
    foreach (var hunk in file.Hunks)
    {
        Console.WriteLine($"  Lines {hunk.StartLine}-{hunk.EndLine}");
    }
}

if (preview.HasConflicts)
{
    Console.WriteLine("⚠ Conflicts detected!");
}
```

### Rollback

Undo the last patch:

```csharp
var rollback = await repoFS.RollbackLastPatchAsync();

if (rollback.Success)
{
    Console.WriteLine("Rolled back successfully");
}
```

### Configuration

```yaml
# .agent/config.yml
patching:
  # Fuzz factor (lines)
  fuzz: 3
  
  # Keep backups for rollback
  backup:
    enabled: true
    retention_minutes: 60
    
  # Dry run by default
  dry_run_default: false
```

### CLI Integration

```bash
$ acode run "Add error handling to the API controller"

[Tool: apply_patch]
  File: src/Controllers/ApiController.cs
  Hunks: 3
  
  Preview:
    + try {
    +     var result = await _service.ProcessAsync(request);
    + } catch (Exception ex) {
    +     _logger.LogError(ex, "Processing failed");
    +     return StatusCode(500);
    + }
  
  Apply changes? [y/N]
```

### Troubleshooting

#### Context Mismatch

**Problem:** Patch doesn't match current file

**Causes:**
1. File changed since patch created
2. Wrong file version
3. Line endings differ

**Solutions:**
1. Regenerate patch
2. Increase fuzz factor
3. Normalize line endings

#### Hunk Rejected

**Problem:** Some changes failed

**Causes:**
1. Context doesn't match
2. Lines already removed
3. Conflicting changes

**Solutions:**
1. Review rejected hunks
2. Apply manually
3. Regenerate patch

#### Multi-File Failure

**Problem:** Transaction rolled back

**Causes:**
1. One file failed, all rolled back
2. Check individual file errors

**Solutions:**
1. Fix failing file
2. Apply separately
3. Check all files first

---

## Acceptance Criteria

### Parsing

- [ ] AC-001: Unified diff parsed
- [ ] AC-002: Hunks extracted
- [ ] AC-003: Multi-file parsed

### Validation

- [ ] AC-004: Context validated
- [ ] AC-005: Lines validated
- [ ] AC-006: Errors reported

### Application

- [ ] AC-007: Additions work
- [ ] AC-008: Removals work
- [ ] AC-009: Modifications work
- [ ] AC-010: Multi-hunk works

### Atomicity

- [ ] AC-011: All-or-nothing
- [ ] AC-012: Failure rollback
- [ ] AC-013: Backup created

### Dry Run

- [ ] AC-014: Preview works
- [ ] AC-015: No modifications
- [ ] AC-016: Conflicts shown

### Rollback

- [ ] AC-017: Rollback works
- [ ] AC-018: Original restored
- [ ] AC-019: Cleanup works

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Patching/
├── PatchParserTests.cs
│   ├── Should_Parse_Simple_Diff()
│   ├── Should_Parse_Unified_Diff_Header()
│   ├── Should_Parse_Single_Hunk()
│   ├── Should_Parse_Multi_Hunk()
│   ├── Should_Parse_Multi_File()
│   ├── Should_Parse_New_File()
│   ├── Should_Parse_Deleted_File()
│   ├── Should_Parse_Renamed_File()
│   ├── Should_Extract_Line_Numbers()
│   ├── Should_Extract_Context_Lines()
│   ├── Should_Extract_Added_Lines()
│   ├── Should_Extract_Removed_Lines()
│   ├── Should_Handle_No_Newline_At_EOF()
│   ├── Should_Handle_Empty_Patch()
│   └── Should_Reject_Malformed_Patch()
│
├── PatchValidatorTests.cs
│   ├── Should_Validate_Context_Matches()
│   ├── Should_Detect_Context_Mismatch()
│   ├── Should_Detect_Line_Already_Removed()
│   ├── Should_Detect_Line_Already_Added()
│   ├── Should_Allow_Fuzz_Factor()
│   ├── Should_Respect_Max_Fuzz()
│   ├── Should_Validate_Line_Numbers()
│   ├── Should_Handle_Modified_File()
│   ├── Should_Handle_Missing_File()
│   ├── Should_Report_All_Errors()
│   └── Should_Return_Conflict_Details()
│
├── PatchApplicatorTests.cs
│   ├── Should_Apply_Single_Addition()
│   ├── Should_Apply_Multiple_Additions()
│   ├── Should_Apply_Single_Removal()
│   ├── Should_Apply_Multiple_Removals()
│   ├── Should_Apply_Modification()
│   ├── Should_Apply_Multi_Hunk()
│   ├── Should_Apply_In_Reverse_Order()
│   ├── Should_Apply_With_Fuzz()
│   ├── Should_Create_New_File()
│   ├── Should_Delete_File()
│   ├── Should_Apply_Atomically()
│   ├── Should_Rollback_On_Failure()
│   ├── Should_Create_Backup()
│   ├── Should_Preserve_File_Permissions()
│   └── Should_Preserve_Line_Endings()
│
├── MultiFilePatchTests.cs
│   ├── Should_Apply_All_Files()
│   ├── Should_Rollback_All_On_Failure()
│   ├── Should_Apply_In_Order()
│   ├── Should_Handle_Partial_Failure()
│   └── Should_Report_Per_File_Status()
│
├── DryRunTests.cs
│   ├── Should_Preview_Changes()
│   ├── Should_Not_Modify_Files()
│   ├── Should_Show_Added_Lines()
│   ├── Should_Show_Removed_Lines()
│   ├── Should_Show_Conflicts()
│   └── Should_Validate_All_Hunks()
│
├── PatchRollbackTests.cs
│   ├── Should_Rollback_Single_File()
│   ├── Should_Rollback_Multi_File()
│   ├── Should_Restore_Original_Content()
│   ├── Should_Restore_Deleted_File()
│   ├── Should_Delete_Created_File()
│   ├── Should_Handle_Missing_Backup()
│   ├── Should_Cleanup_Backup_After_Rollback()
│   └── Should_Respect_Retention_Period()
│
└── PatchLineEndingTests.cs
    ├── Should_Handle_LF_Files()
    ├── Should_Handle_CRLF_Files()
    ├── Should_Handle_Mixed_Line_Endings()
    └── Should_Preserve_Original_Line_Endings()
```

### Integration Tests

```
Tests/Integration/Patching/
├── PatchIntegrationTests.cs
│   ├── Should_Apply_Complex_Patch()
│   ├── Should_Apply_Large_Patch()
│   ├── Should_Apply_To_Large_File()
│   ├── Should_Handle_Binary_Detection()
│   ├── Should_Work_With_Real_Git_Diff()
│   └── Should_Handle_Concurrent_Patches()
│
└── PatchAtomicityIntegrationTests.cs
    ├── Should_Survive_Process_Crash()
    ├── Should_Recover_From_Partial_Apply()
    └── Should_Handle_Disk_Full()
```

### E2E Tests

```
Tests/E2E/Patching/
├── PatchE2ETests.cs
│   ├── Should_Apply_Via_Agent_Tool()
│   ├── Should_Preview_Via_Agent_Tool()
│   ├── Should_Rollback_Via_Agent_Tool()
│   ├── Should_Work_With_Confirmation_Flow()
│   └── Should_Handle_User_Rejection()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Simple patch | 5ms | 10ms |
| Multi-hunk | 15ms | 30ms |
| Multi-file | 25ms | 50ms |
| Rollback | 10ms | 25ms |

---

## User Verification Steps

### Scenario 1: Simple Patch

1. Create file with known content
2. Create patch to add line
3. Apply patch
4. Verify: Line added

### Scenario 2: Multi-Hunk

1. Create file
2. Create patch with 3 hunks
3. Apply patch
4. Verify: All hunks applied

### Scenario 3: Rollback

1. Apply patch
2. Rollback
3. Verify: Original restored

### Scenario 4: Dry Run

1. Create patch
2. Preview
3. Verify: No changes made
4. Verify: Preview accurate

### Scenario 5: Conflict

1. Create patch
2. Modify file differently
3. Apply patch
4. Verify: Conflict reported

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Patching/
│   ├── Patch.cs
│   ├── Hunk.cs
│   ├── PatchResult.cs
│   └── PatchConflict.cs
│
src/AgenticCoder.Infrastructure/
├── FileSystem/
│   └── Patching/
│       ├── UnifiedDiffParser.cs
│       ├── PatchValidator.cs
│       ├── PatchApplicator.cs
│       ├── PatchRollback.cs
│       └── FuzzMatcher.cs
```

### PatchApplicator Class

```csharp
namespace AgenticCoder.Infrastructure.FileSystem.Patching;

public sealed class PatchApplicator
{
    public async Task<PatchResult> ApplyAsync(
        IRepoFS fs,
        Patch patch,
        PatchOptions options,
        CancellationToken ct)
    {
        // Validate first
        var validation = await _validator.ValidateAsync(fs, patch, ct);
        if (!validation.IsValid)
            return PatchResult.Failed(validation.Errors);
        
        // Create backups
        var backups = await CreateBackupsAsync(fs, patch.Files, ct);
        
        try
        {
            // Apply each file
            foreach (var file in patch.Files)
            {
                await ApplyFileAsync(fs, file, options, ct);
            }
            
            return PatchResult.Success(patch.Hunks.Count);
        }
        catch
        {
            // Rollback on failure
            await RestoreBackupsAsync(fs, backups, ct);
            throw;
        }
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-PAT-001 | Parse error |
| ACODE-PAT-002 | Validation failed |
| ACODE-PAT-003 | Context mismatch |
| ACODE-PAT-004 | Apply failed |
| ACODE-PAT-005 | Rollback failed |

### Implementation Checklist

1. [ ] Create diff parser
2. [ ] Create validator
3. [ ] Create applicator
4. [ ] Implement atomicity
5. [ ] Implement dry run
6. [ ] Implement rollback
7. [ ] Add fuzz matching
8. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Parser
2. **Phase 2:** Validator
3. **Phase 3:** Simple apply
4. **Phase 4:** Atomicity
5. **Phase 5:** Rollback

---

**End of Task 014.c Specification**