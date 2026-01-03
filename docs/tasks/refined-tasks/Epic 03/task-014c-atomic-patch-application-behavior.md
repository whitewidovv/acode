# Task 014.c: Atomic Patch Application Behavior

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 014 (RepoFS), Task 014.a (Local FS), Task 014.b (Docker FS)  

---

## Description

Task 014.c implements atomic patch application for RepoFS. Patches are the primary way the agent modifies files. Atomicity ensures patches either fully apply or don't apply at all.

The agent generates patches as unified diffs. These patches describe changes to make. A patch might add lines, remove lines, or modify lines. Multiple files can be in one patch.

Atomic application is critical for safety. A half-applied patch corrupts files. If the agent crashes mid-patch, files must remain consistent. Atomicity provides this guarantee.

The patch system parses unified diff format. This is the standard format from git diff. It includes context lines for matching. Hunks describe individual changes.

Before applying, patches are validated. Does the context match the current file? Are the line numbers reasonable? Does the file exist? Validation catches problems early.

Dry run mode previews changes. See what would change without actually changing. Review the impact. Catch errors before they affect real files.

Rollback enables recovery. After applying a patch, the original state is preserved. If something goes wrong, rollback restores. The rollback window is configurable.

Multi-file patches apply transactionally. Either all files change or none change. This prevents partial updates that leave the codebase inconsistent.

Conflict detection identifies problems. If the file changed since the patch was generated, there's a conflict. Conflicts are reported clearly. Manual resolution may be needed.

Fuzz matching handles minor line number drift. If the exact line isn't found, nearby lines are checked. Configurable fuzz factor controls how far to search.

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
│   ├── Should_Parse_Multi_Hunk()
│   └── Should_Parse_Multi_File()
│
├── PatchValidatorTests.cs
│   ├── Should_Validate_Context()
│   ├── Should_Detect_Mismatch()
│   └── Should_Allow_Fuzz()
│
├── PatchApplicatorTests.cs
│   ├── Should_Apply_Addition()
│   ├── Should_Apply_Removal()
│   └── Should_Apply_Atomically()
│
└── PatchRollbackTests.cs
    ├── Should_Rollback_Single()
    └── Should_Rollback_Multi()
```

### Integration Tests

```
Tests/Integration/Patching/
├── PatchIntegrationTests.cs
│   ├── Should_Apply_Complex_Patch()
│   └── Should_Handle_Large_Files()
```

### E2E Tests

```
Tests/E2E/Patching/
├── PatchE2ETests.cs
│   └── Should_Apply_Via_Agent()
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