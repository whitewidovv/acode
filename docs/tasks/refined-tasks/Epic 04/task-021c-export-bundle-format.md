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