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

### Patch Parsing

- FR-001: Unified diff parsing MUST work
- FR-002: Multiple file patches MUST work
- FR-003: Hunk parsing MUST work
- FR-004: Context line parsing MUST work
- FR-005: Add/remove/modify detection

### Patch Validation

- FR-006: Context matching MUST work
- FR-007: Line number validation
- FR-008: File existence check
- FR-009: Encoding compatibility check
- FR-010: Validation error messages

### Patch Application

- FR-011: ApplyPatchAsync MUST work
- FR-012: Line additions MUST work
- FR-013: Line removals MUST work
- FR-014: Line modifications MUST work
- FR-015: Multiple hunks MUST work

### Atomicity

- FR-016: All-or-nothing application
- FR-017: Failure rolls back changes
- FR-018: No partial patches
- FR-019: Multi-file transaction
- FR-020: Backup before apply

### Dry Run

- FR-021: PreviewPatchAsync MUST work
- FR-022: Show what would change
- FR-023: Report potential conflicts
- FR-024: No file modifications

### Rollback

- FR-025: RollbackPatchAsync MUST work
- FR-026: Restore original content
- FR-027: Rollback window configurable
- FR-028: Cleanup after rollback

### Conflict Detection

- FR-029: Context mismatch detection
- FR-030: Line number drift detection
- FR-031: File modification detection
- FR-032: Clear conflict reports

### Fuzz Matching

- FR-033: Fuzz factor configurable
- FR-034: Default fuzz: 3 lines
- FR-035: Offset tracking
- FR-036: Offset reporting

### Multi-File Patches

- FR-037: Parse multi-file patches
- FR-038: Apply transactionally
- FR-039: Rollback all on failure
- FR-040: Report per-file status

### Result Reporting

- FR-041: Success/failure indication
- FR-042: Applied hunk count
- FR-043: Rejected hunk details
- FR-044: Offset information
- FR-045: Conflict details

---

## Non-Functional Requirements

### Performance

- NFR-001: Small patch < 10ms
- NFR-002: Large patch < 100ms
- NFR-003: Multi-file < 50ms/file

### Reliability

- NFR-004: No partial application
- NFR-005: Rollback always works
- NFR-006: Corruption impossible

### Safety

- NFR-007: Backup before change
- NFR-008: Validation before apply
- NFR-009: Clear error messages

### Usability

- NFR-010: Clear dry run output
- NFR-011: Helpful conflict messages
- NFR-012: Good logging

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