# Task 021.c: Export Bundle Format

**Priority:** P2 – Medium  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 021 (Artifact Collection), Task 050 (CLI), Task 039 (Sessions), Task 038 (Outbox), Task 049.e (Provenance)  

---

## Description

Task 021.c defines the export bundle format for sharing and archiving execution runs. Bundles MUST be self-contained archives that include all metadata and artifacts needed for replay or audit. This enables debugging across machines and sharing run data with team members.

The export bundle MUST include a DB snapshot containing run, session, task, step, and tool-call metadata. All sensitive data MUST be redacted before export. The redaction process MUST follow the patterns defined in Task 021.b for environment variables.

The bundle MUST include outbox/sync status summary. This captures pending, acknowledged, and failed message counts. This metadata enables understanding the synchronization state at the time of export.

Stable pointers to included artifacts MUST be present. Artifacts MUST be referenced by content hash rather than filesystem paths. This ensures bundles remain valid when moved between systems.

Provenance fields MUST be included for audit and replay support. These fields MUST include repository SHA, worktree ID, and timestamps. This enables correlating exported runs with specific code versions.

The bundle format MUST be a ZIP archive with a defined internal structure. The format MUST be versioned to support future evolution. Older readers MUST be able to detect incompatible versions and fail gracefully.

Import functionality MUST validate bundle integrity before extracting. Corrupted or tampered bundles MUST be rejected. Signature verification MUST be optional but supported.

The CLI MUST provide `acode runs export` and `acode runs import` commands. Export MUST support selecting specific runs or date ranges. Import MUST support merging with existing data or replacing.

### Business Value

Export bundles enable portable debugging. When a run fails in one environment, the bundle can be shared for analysis elsewhere. This accelerates troubleshooting across distributed teams. Bundles also serve as audit trails for compliance requirements.

### Scope Boundaries

This task covers the bundle format definition and CLI commands for export/import. It does NOT cover the artifact storage (Task 021), directory standards (Task 021.a), or viewing commands (Task 021.b).

### Integration Points

- Task 021: RunRecord and artifact data sources
- Task 021.a: Artifact directory structure for collection
- Task 021.b: Redaction patterns for sensitive data
- Task 039: Session metadata for inclusion
- Task 038: Outbox status summary
- Task 049.e: Provenance fields

### Failure Modes

- Export with missing artifacts → Include manifest noting missing files
- Import with version mismatch → Reject with clear error message
- Import with hash mismatch → Reject as corrupted
- Export to read-only path → Return exit code 1 with error
- Import conflicts with existing runs → Configurable merge strategy

### Assumptions

- ZIP format is universally supported
- SHA-256 provides sufficient integrity verification
- Bundles will typically be <100MB
- Import frequency is low (occasional, not continuous)

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Bundle | A self-contained ZIP archive containing run data and artifacts |
| Export | Process of creating a bundle from local run data |
| Import | Process of loading a bundle into local storage |
| Provenance | Metadata linking runs to source code versions |
| Redaction | Removal or masking of sensitive data before export |
| Manifest | JSON file listing bundle contents and metadata |
| Content Hash | SHA-256 hash of artifact file contents |
| Worktree ID | Identifier for the git worktree where run executed |
| Outbox | Queue of messages pending synchronization |
| Replay | Re-executing commands from historical run data |
| Integrity Check | Verification that bundle contents match expected hashes |
| Version Tag | Semantic version identifying bundle format compatibility |

---

## Out of Scope

- Artifact storage implementation (Task 021)
- Directory structure standards (Task 021.a)
- Run viewing commands (Task 021.b)
- Real-time synchronization
- Cloud storage integration
- Bundle encryption (beyond optional signing)
- Incremental/delta exports
- Automatic periodic exports
- Bundle compression algorithm selection (uses ZIP default)

---

## Functional Requirements

### FR-001 to FR-020: Bundle Format Structure

- FR-001: Bundle MUST be a ZIP archive with `.acode-bundle` extension
- FR-002: Bundle MUST contain `manifest.json` at root level
- FR-003: Manifest MUST include bundle format version
- FR-004: Manifest MUST include creation timestamp (ISO 8601)
- FR-005: Manifest MUST include creator tool version
- FR-006: Manifest MUST include content hash of all included files
- FR-007: Manifest MUST include run count
- FR-008: Manifest MUST include total artifact size
- FR-009: Bundle MUST contain `runs/` directory for run metadata
- FR-010: Bundle MUST contain `artifacts/` directory for artifact files
- FR-011: Bundle MUST contain `provenance.json` for source tracking
- FR-012: Provenance MUST include repository remote URL (if available)
- FR-013: Provenance MUST include commit SHA at export time
- FR-014: Provenance MUST include worktree ID
- FR-015: Provenance MUST include export timestamp
- FR-016: Bundle MUST contain `outbox-summary.json` for sync status
- FR-017: Outbox summary MUST include pending message count
- FR-018: Outbox summary MUST include acknowledged message count
- FR-019: Outbox summary MUST include failed message count
- FR-020: Bundle format version MUST follow semantic versioning

### FR-021 to FR-040: Export Command

- FR-021: `acode runs export` MUST create a bundle file
- FR-022: `--output PATH` MUST specify output file path
- FR-023: Default output MUST be `acode-export-{timestamp}.acode-bundle`
- FR-024: `--run ID` MUST export specific run by ID
- FR-025: `--run ID` MUST be repeatable for multiple runs
- FR-026: `--since DATE` MUST export runs after specified date
- FR-027: `--until DATE` MUST export runs before specified date
- FR-028: `--last N` MUST export N most recent runs
- FR-029: `--all` MUST export all runs (with confirmation prompt)
- FR-030: `--include-artifacts` MUST include artifact files (default: true)
- FR-031: `--no-artifacts` MUST exclude artifact files
- FR-032: `--redact` MUST apply redaction patterns (default: true)
- FR-033: `--no-redact` MUST skip redaction (with warning)
- FR-034: Export MUST validate all referenced artifacts exist
- FR-035: Missing artifacts MUST be noted in manifest
- FR-036: Export MUST compute content hashes for all files
- FR-037: Export MUST show progress for large bundles
- FR-038: Export MUST be atomic (complete or no file created)
- FR-039: Export MUST verify bundle integrity before finalizing
- FR-040: Export MUST return exit code 0 on success

### FR-041 to FR-060: Import Command

- FR-041: `acode runs import {file}` MUST import a bundle
- FR-042: Import MUST validate bundle format version
- FR-043: Import MUST reject incompatible format versions
- FR-044: Import MUST verify manifest content hashes
- FR-045: Import MUST reject bundles with hash mismatches
- FR-046: `--dry-run` MUST preview import without changes
- FR-047: `--merge` MUST add to existing data (default)
- FR-048: `--replace` MUST replace existing runs with same ID
- FR-049: `--skip-existing` MUST skip runs that already exist
- FR-050: Import MUST detect run ID collisions
- FR-051: Import MUST prompt for collision resolution
- FR-052: `--force` MUST skip confirmation prompts
- FR-053: Import MUST preserve original timestamps
- FR-054: Import MUST update provenance with import metadata
- FR-055: Import MUST show progress for large bundles
- FR-056: Import MUST be atomic (complete or rollback)
- FR-057: Failed import MUST NOT leave partial data
- FR-058: Import MUST log imported run count
- FR-059: Import MUST return exit code 0 on success
- FR-060: Import MUST return exit code 1 on validation failure

### FR-061 to FR-080: Redaction and Security

- FR-061: Redaction MUST apply to environment variables
- FR-062: Redaction MUST mask PASSWORD patterns
- FR-063: Redaction MUST mask SECRET patterns
- FR-064: Redaction MUST mask KEY patterns
- FR-065: Redaction MUST mask TOKEN patterns
- FR-066: Redaction MUST mask API_KEY patterns
- FR-067: Custom redaction patterns MUST be configurable
- FR-068: Redaction MUST replace values with `[REDACTED]`
- FR-069: Redaction MUST NOT modify artifact file contents
- FR-070: Redaction MUST apply to command arguments
- FR-071: File paths MUST NOT be redacted
- FR-072: Timestamps MUST NOT be redacted
- FR-073: Exit codes MUST NOT be redacted
- FR-074: `--sign` MUST add digital signature to bundle
- FR-075: Signature MUST use configured signing key
- FR-076: `--verify` MUST validate signature on import
- FR-077: Invalid signature MUST block import
- FR-078: `--no-verify` MUST skip signature verification
- FR-079: Signature key path MUST be configurable
- FR-080: Unsigned bundles MUST import with warning

---

## Non-Functional Requirements

### NFR-001 to NFR-010: Performance

- NFR-001: Export of 100 runs MUST complete in <30 seconds
- NFR-002: Export of 1GB artifacts MUST complete in <2 minutes
- NFR-003: Import of 100 runs MUST complete in <30 seconds
- NFR-004: Hash computation MUST use streaming (not load full file)
- NFR-005: ZIP compression MUST use system-default level
- NFR-006: Memory usage MUST NOT exceed 100MB for normal bundles
- NFR-007: Large artifact streaming MUST use <10MB buffer
- NFR-008: Progress updates MUST appear at least every 2 seconds
- NFR-009: Manifest generation MUST complete in <1 second
- NFR-010: Integrity verification MUST use parallel hashing

### NFR-011 to NFR-020: Reliability

- NFR-011: Export MUST be atomic (temp file then rename)
- NFR-012: Import MUST use database transaction
- NFR-013: Interrupted export MUST NOT leave partial file
- NFR-014: Interrupted import MUST rollback cleanly
- NFR-015: Corrupted ZIP MUST be detected before extraction
- NFR-016: Missing manifest MUST produce clear error
- NFR-017: Invalid JSON MUST produce clear error with location
- NFR-018: Disk full during export MUST produce clear error
- NFR-019: File permission errors MUST produce clear error
- NFR-020: Network path failures MUST timeout appropriately

### NFR-021 to NFR-030: Security

- NFR-021: Redaction MUST occur before writing to bundle
- NFR-022: Redacted data MUST NOT be recoverable from bundle
- NFR-023: Path traversal in ZIP MUST be blocked on import
- NFR-024: Symlinks in bundle MUST NOT be followed
- NFR-025: ZIP bomb detection MUST prevent decompression attacks
- NFR-026: Maximum decompressed size MUST be configurable
- NFR-027: Default max decompressed size MUST be 10GB
- NFR-028: Signature verification MUST use SHA-256
- NFR-029: Signing keys MUST NOT be included in bundle
- NFR-030: Import MUST NOT execute any content from bundle

---

## User Manual Documentation

### Quick Start

```bash
# Export last 10 runs
acode runs export --last 10

# Export specific runs
acode runs export --run abc123 --run def456

# Import a bundle
acode runs import team-debug-2026-01-03.acode-bundle
```

### Bundle Format Structure

The `.acode-bundle` file is a ZIP archive with this structure:

```
bundle.acode-bundle/
├── manifest.json           # Bundle metadata and file hashes
├── provenance.json         # Git repo and code version info
├── outbox-summary.json     # Sync status at export time
├── runs/
│   ├── run-001.json        # Run metadata (redacted)
│   ├── run-002.json
│   └── ...
└── artifacts/
    ├── run-001/
    │   ├── stdout.txt
    │   ├── stderr.txt
    │   └── test-results.json
    └── run-002/
        └── ...
```

### Export Command Reference

```bash
acode runs export [options]

Options:
  --output, -o PATH     Output file path
  --run ID              Export specific run (repeatable)
  --since DATE          Export runs after this date
  --until DATE          Export runs before this date
  --last N              Export N most recent runs
  --all                 Export all runs (requires confirmation)
  --no-artifacts        Exclude artifact files
  --no-redact           Skip redaction (WARNING: exposes secrets)
  --sign                Sign bundle with configured key
  --format FORMAT       Output format: bundle (default)
```

**Examples:**

```bash
# Export runs from the last week
acode runs export --since 7d --output weekly-runs.acode-bundle

# Export without artifacts (metadata only)
acode runs export --last 50 --no-artifacts

# Export and sign for verification
acode runs export --last 10 --sign
```

### Import Command Reference

```bash
acode runs import <file> [options]

Options:
  --dry-run             Preview import without changes
  --merge               Add to existing data (default)
  --replace             Replace existing runs with same ID
  --skip-existing       Skip runs that already exist
  --force               Skip confirmation prompts
  --verify              Verify signature (if signed)
  --no-verify           Skip signature verification
```

**Examples:**

```bash
# Preview what would be imported
acode runs import bundle.acode-bundle --dry-run

# Import and replace conflicts
acode runs import bundle.acode-bundle --replace

# Import with signature verification
acode runs import bundle.acode-bundle --verify
```

### Configuration

Configuration in `.agent/config.yml`:

```yaml
export:
  defaultRedaction: true
  redactPatterns:
    - PASSWORD
    - SECRET
    - KEY
    - TOKEN
    - API_KEY
  maxBundleSize: 10GB
  signatureKeyPath: ~/.acode/signing-key.pem

import:
  requireSignature: false
  maxDecompressedSize: 10GB
  defaultMergeStrategy: merge  # merge, replace, skip
```

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Export/import failed |
| 2 | Invalid arguments |
| 3 | Validation failed (hash mismatch, bad signature) |
| 4 | Version incompatibility |

### Best Practices

1. **Always use redaction** - Never export with `--no-redact` unless absolutely necessary
2. **Sign bundles** - Use `--sign` for bundles shared externally
3. **Verify before import** - Use `--dry-run` to preview imports
4. **Keep bundles small** - Export specific runs rather than `--all`
5. **Store bundles securely** - Bundles may contain sensitive metadata

### Troubleshooting

**Q: "Incompatible bundle version"**

The bundle was created with a newer version of acode. Update your acode installation:
```bash
acode update
```

**Q: "Hash mismatch detected"**

The bundle is corrupted or was modified. Request a fresh export from the source.

**Q: "Signature verification failed"**

The signature is invalid. Ensure you have the correct public key configured.

**Q: Export is very slow**

Large artifacts take time. Use `--no-artifacts` for metadata-only export.

---

## Acceptance Criteria / Definition of Done

### Functionality

- [ ] AC-001: `acode runs export` creates valid ZIP archive
- [ ] AC-002: Bundle has `.acode-bundle` extension
- [ ] AC-003: Bundle contains `manifest.json` at root
- [ ] AC-004: Manifest includes format version
- [ ] AC-005: Manifest includes creation timestamp
- [ ] AC-006: Manifest includes content hashes for all files
- [ ] AC-007: Bundle contains `runs/` directory with run JSON files
- [ ] AC-008: Bundle contains `artifacts/` directory structure
- [ ] AC-009: Bundle contains `provenance.json`
- [ ] AC-010: Provenance includes commit SHA
- [ ] AC-011: Bundle contains `outbox-summary.json`
- [ ] AC-012: `--run ID` exports specific run
- [ ] AC-013: `--since DATE` filters by date
- [ ] AC-014: `--last N` exports N recent runs
- [ ] AC-015: `--no-artifacts` excludes artifact files
- [ ] AC-016: `acode runs import` loads bundle data
- [ ] AC-017: Import validates format version
- [ ] AC-018: Import verifies content hashes
- [ ] AC-019: Import rejects corrupted bundles
- [ ] AC-020: `--dry-run` previews without changes
- [ ] AC-021: `--merge` adds to existing data
- [ ] AC-022: `--replace` overwrites existing runs
- [ ] AC-023: `--skip-existing` skips conflicts

### Safety/Policy

- [ ] AC-024: Redaction masks PASSWORD patterns by default
- [ ] AC-025: Redaction masks SECRET patterns by default
- [ ] AC-026: Redaction masks KEY patterns by default
- [ ] AC-027: Redaction masks TOKEN patterns by default
- [ ] AC-028: `--no-redact` requires explicit flag
- [ ] AC-029: `--no-redact` displays warning
- [ ] AC-030: Path traversal in ZIP is blocked
- [ ] AC-031: Symlinks in bundle are not followed
- [ ] AC-032: ZIP bomb detection prevents attacks
- [ ] AC-033: Signature verification works with `--verify`
- [ ] AC-034: Invalid signature blocks import

### CLI/UX

- [ ] AC-035: Commands provide `--help` documentation
- [ ] AC-036: Invalid options produce clear error messages
- [ ] AC-037: Progress is shown for large operations
- [ ] AC-038: Exit codes follow documented conventions
- [ ] AC-039: Confirmation prompt for `--all` export
- [ ] AC-040: Confirmation prompt for collision resolution

### Logging/Audit

- [ ] AC-041: Export logs bundle creation details
- [ ] AC-042: Import logs run count imported
- [ ] AC-043: Validation failures are logged with details
- [ ] AC-044: Redaction patterns applied are logged

### Performance

- [ ] AC-045: Export 100 runs completes in <30 seconds
- [ ] AC-046: Import 100 runs completes in <30 seconds
- [ ] AC-047: Memory stays under 100MB for normal bundles
- [ ] AC-048: 1GB artifact export completes in <2 minutes

### Tests

- [ ] AC-049: Unit tests achieve 90% coverage
- [ ] AC-050: Integration tests cover round-trip export/import
- [ ] AC-051: E2E tests verify CLI behavior

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Test manifest JSON generation
- [ ] UT-002: Test content hash computation
- [ ] UT-003: Test redaction pattern matching
- [ ] UT-004: Test redaction replacement
- [ ] UT-005: Test date filter parsing
- [ ] UT-006: Test run ID selection
- [ ] UT-007: Test provenance field extraction
- [ ] UT-008: Test outbox summary generation
- [ ] UT-009: Test format version comparison
- [ ] UT-010: Test hash verification logic
- [ ] UT-011: Test path traversal detection
- [ ] UT-012: Test symlink detection
- [ ] UT-013: Test ZIP bomb detection
- [ ] UT-014: Test signature generation
- [ ] UT-015: Test signature verification

### Integration Tests

- [ ] IT-001: Test export creates valid ZIP
- [ ] IT-002: Test import reads exported bundle
- [ ] IT-003: Test round-trip preserves data
- [ ] IT-004: Test redaction in exported data
- [ ] IT-005: Test artifact inclusion
- [ ] IT-006: Test artifact exclusion with `--no-artifacts`
- [ ] IT-007: Test merge strategy
- [ ] IT-008: Test replace strategy
- [ ] IT-009: Test skip-existing strategy
- [ ] IT-010: Test dry-run mode

### End-to-End Tests

- [ ] E2E-001: Export runs, import on fresh DB, verify data
- [ ] E2E-002: Export with redaction, verify secrets masked
- [ ] E2E-003: Export signed, import with verify
- [ ] E2E-004: Import corrupted bundle, verify rejection
- [ ] E2E-005: Import incompatible version, verify error
- [ ] E2E-006: Export/import large bundle with artifacts
- [ ] E2E-007: Test conflict resolution prompts
- [ ] E2E-008: Test progress display for large exports

### Performance/Benchmarks

- [ ] PB-001: Export 100 runs in <30 seconds
- [ ] PB-002: Import 100 runs in <30 seconds
- [ ] PB-003: Export 1GB artifacts in <2 minutes
- [ ] PB-004: Memory usage under 100MB for 500MB bundle
- [ ] PB-005: Hash computation at >100MB/s

### Regression

- [ ] RG-001: Verify Task 021 RunRecord compatibility
- [ ] RG-002: Verify Task 021.a artifact paths
- [ ] RG-003: Verify Task 021.b redaction patterns
- [ ] RG-004: Verify Task 039 session data inclusion
- [ ] RG-005: Verify Task 038 outbox summary

---

## User Verification Steps

1. **Verify export creates bundle:**
   ```bash
   acode runs export --last 5
   ```
   Verify: File with `.acode-bundle` extension created

2. **Verify bundle is valid ZIP:**
   ```bash
   unzip -l acode-export-*.acode-bundle
   ```
   Verify: Lists manifest.json, runs/, artifacts/

3. **Verify manifest contains hashes:**
   ```bash
   unzip -p acode-export-*.acode-bundle manifest.json | jq '.files'
   ```
   Verify: Each file has sha256 hash

4. **Verify redaction applied:**
   ```bash
   unzip -p acode-export-*.acode-bundle runs/*.json | grep -i password
   ```
   Verify: Values show `[REDACTED]`

5. **Verify import dry-run:**
   ```bash
   acode runs import bundle.acode-bundle --dry-run
   ```
   Verify: Shows what would be imported without changes

6. **Verify import adds runs:**
   ```bash
   acode runs import bundle.acode-bundle
   acode runs list
   ```
   Verify: Imported runs appear in list

7. **Verify hash validation:**
   ```bash
   # Corrupt the bundle
   acode runs import corrupted.acode-bundle
   ```
   Verify: Error "Hash mismatch detected"

8. **Verify version check:**
   ```bash
   # Bundle with future version
   acode runs import future-version.acode-bundle
   ```
   Verify: Error "Incompatible bundle version"

9. **Verify no-artifacts option:**
   ```bash
   acode runs export --last 5 --no-artifacts
   unzip -l *.acode-bundle
   ```
   Verify: No files in artifacts/ directory

10. **Verify provenance data:**
    ```bash
    unzip -p bundle.acode-bundle provenance.json | jq
    ```
    Verify: Contains commitSha, worktreeId, timestamp

11. **Verify outbox summary:**
    ```bash
    unzip -p bundle.acode-bundle outbox-summary.json | jq
    ```
    Verify: Contains pending, acknowledged, failed counts

12. **Verify signed export:**
    ```bash
    acode runs export --last 5 --sign
    ```
    Verify: Bundle includes signature file

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Export/
│       ├── BundleManifest.cs
│       ├── BundleVersion.cs
│       ├── Provenance.cs
│       └── OutboxSummary.cs
├── Acode.Application/
│   └── Export/
│       ├── Commands/
│       │   ├── ExportBundleCommand.cs
│       │   └── ImportBundleCommand.cs
│       └── Services/
│           ├── IBundleExporter.cs
│           ├── IBundleImporter.cs
│           └── IRedactionService.cs
├── Acode.Infrastructure/
│   └── Export/
│       ├── ZipBundleExporter.cs
│       ├── ZipBundleImporter.cs
│       ├── RedactionService.cs
│       └── HashCalculator.cs
└── Acode.Cli/
    └── Commands/
        └── Runs/
            ├── RunsExportCommand.cs
            └── RunsImportCommand.cs
```

### Core Interfaces

```csharp
public interface IBundleExporter
{
    Task<ExportResult> ExportAsync(ExportOptions options);
}

public interface IBundleImporter
{
    Task<ImportPreview> PreviewAsync(string bundlePath);
    Task<ImportResult> ImportAsync(string bundlePath, ImportOptions options);
}

public interface IRedactionService
{
    string RedactValue(string key, string value);
    Dictionary<string, string> RedactEnvironment(Dictionary<string, string> env);
    bool IsRedactionPattern(string pattern);
}

public record ExportOptions(
    IReadOnlyList<RunId>? RunIds,
    DateTimeOffset? Since,
    DateTimeOffset? Until,
    int? LastN,
    bool IncludeArtifacts,
    bool ApplyRedaction,
    bool Sign,
    string OutputPath);

public record ImportOptions(
    MergeStrategy Strategy,
    bool VerifySignature,
    bool Force);

public enum MergeStrategy { Merge, Replace, SkipExisting }

public record BundleManifest(
    string Version,
    DateTimeOffset CreatedAt,
    string ToolVersion,
    int RunCount,
    long TotalArtifactBytes,
    IReadOnlyDictionary<string, string> FileHashes);

public record Provenance(
    string? RemoteUrl,
    string? CommitSha,
    string? WorktreeId,
    DateTimeOffset Timestamp);

public record OutboxSummary(
    int PendingCount,
    int AcknowledgedCount,
    int FailedCount);
```

### Manifest Schema

```json
{
  "version": "1.0.0",
  "createdAt": "2026-01-03T12:00:00Z",
  "toolVersion": "0.5.0",
  "runCount": 10,
  "totalArtifactBytes": 1048576,
  "files": {
    "runs/run-001.json": "sha256:abc123...",
    "artifacts/run-001/stdout.txt": "sha256:def456..."
  }
}
```

### Error Codes

| Code | Name | Description |
|------|------|-------------|
| EXPORT_001 | OutputPathExists | Output file already exists |
| EXPORT_002 | NoRunsSelected | No runs match selection criteria |
| EXPORT_003 | ArtifactMissing | Referenced artifact not found |
| EXPORT_004 | SigningFailed | Unable to sign bundle |
| IMPORT_001 | InvalidFormat | Bundle format unrecognized |
| IMPORT_002 | VersionMismatch | Incompatible bundle version |
| IMPORT_003 | HashMismatch | Content hash verification failed |
| IMPORT_004 | SignatureInvalid | Signature verification failed |
| IMPORT_005 | PathTraversal | Malicious path detected |

### Validation Checklist Before Merge

- [ ] Export creates valid ZIP archive
- [ ] Manifest includes all required fields
- [ ] Content hashes match file contents
- [ ] Redaction patterns are applied
- [ ] Path traversal is blocked on import
- [ ] ZIP bomb detection works
- [ ] Signature generation and verification work
- [ ] Round-trip preserves all data
- [ ] Performance benchmarks pass
- [ ] Memory profiling completed

### Rollout Plan

1. Implement BundleManifest and domain models
2. Implement RedactionService
3. Implement ZipBundleExporter
4. Implement ZipBundleImporter
5. Add CLI commands
6. Integration tests for round-trip
7. E2E tests for CLI workflow
8. Performance testing with large bundles
9. Documentation updates
10. Release as part of CLI bundle

---

**End of Task 021.c Specification**