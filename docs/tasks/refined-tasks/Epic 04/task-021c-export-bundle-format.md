# Task 021.c: Export Bundle Format

**Priority:** P2 – Medium  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 021 (Artifact Collection), Task 050 (CLI), Task 039 (Sessions), Task 038 (Outbox), Task 049.e (Provenance)  

---

## Description

### Overview

Task 021.c defines the export bundle format for sharing and archiving execution runs. Bundles MUST be self-contained ZIP archives that include all metadata and artifacts needed for replay, audit, or cross-machine debugging. This enables teams to share run data, support compliance requirements, and accelerate troubleshooting across distributed environments.

### Business Value

1. **Portable Debugging**: Share run data across machines for collaborative troubleshooting
2. **Audit Trail Compliance**: Bundles serve as immutable records for compliance requirements
3. **Knowledge Transfer**: New team members can review historical runs for learning
4. **Incident Response**: Quick export of relevant runs during outage investigation
5. **Backup and Archive**: Long-term storage of execution history
6. **Reproducibility Support**: Provenance data enables correlating runs with code versions

### Bundle Contents

| Component | File | Purpose |
|-----------|------|---------|
| Manifest | `manifest.json` | Bundle metadata, file hashes, version |
| Provenance | `provenance.json` | Git repo, commit SHA, worktree ID |
| Outbox Summary | `outbox-summary.json` | Sync status at export time |
| Run Metadata | `runs/*.json` | Redacted run records |
| Artifacts | `artifacts/{run-id}/*` | Stdout, stderr, logs, results |

### Scope

This task covers:
1. **Bundle Format Definition**: ZIP structure, manifest schema, versioning
2. **Export Command**: Selection, filtering, redaction, signing
3. **Import Command**: Validation, merge strategies, integrity checks
4. **Redaction**: Sensitive data masking before export
5. **Integrity**: Content hashing, signature verification
6. **Security**: Path traversal prevention, ZIP bomb detection

### Integration Points

| Component | Integration Type | Data Flow |
|-----------|------------------|-----------|
| Task 021 | Run Records | RunRepository → Exporter |
| Task 021.a | Artifact Paths | PathResolver → Exporter |
| Task 021.b | Redaction Patterns | RedactionService |
| Task 039 | Session Data | SessionRepository → Exporter |
| Task 038 | Outbox Status | OutboxService → Exporter |
| Task 049.e | Provenance | ProvenanceService → Exporter |

### Failure Modes

| Failure Mode | Detection | Recovery |
|--------------|-----------|----------|
| Missing artifacts | File not found | Include in manifest as missing |
| Version mismatch | Version comparison | Reject with clear error |
| Hash mismatch | SHA-256 verification | Reject as corrupted |
| Export disk full | Write exception | Clean temp file, error message |
| Import conflicts | Run ID exists | Prompt for merge strategy |
| Path traversal | Path validation | Block import, log violation |
| ZIP bomb | Size ratio check | Abort extraction |

### Assumptions

- ZIP format is universally supported
- SHA-256 provides sufficient integrity verification
- Bundles will typically be <100MB (large bundles up to 10GB)
- Import frequency is low (occasional, not continuous)
- Signing keys are managed externally

---

## Use Cases

### Use Case 1: DevOps Sharing Production Failure Bundle with Remote Team (Alex, Site Reliability Engineer)

**Persona:** Alex is an SRE responding to a production outage at 2 AM. The application crashed with cryptic errors. Alex's offshore team in India needs to debug but can't access production systems due to compliance restrictions.

**Before (Manual Log Export - 45 minutes):**
1. SSH into production server (5 min - VPN, multi-factor auth, jump host)
2. Locate relevant log files manually: `find /var/log/app/ -name "*2024-01-15*"` (10 min - scattered across multiple directories)
3. Tar logs: `tar czf debug-logs.tar.gz /var/log/app/*.log` (5 min)
4. Download from production to laptop: `scp debug-logs.tar.gz local:/tmp/` (10 min - slow network, 500MB file)
5. Upload to file sharing service (Dropbox, Google Drive) manually (10 min - waiting for upload)
6. Share link with offshore team via Slack (2 min)
7. **Problem:** Environment variables missing, command details lost, git commit SHA not captured
8. Team has to ask "what command did you run?" "what was the config?" - back and forth delays (30+ min)
9. **Total time:** 45 minutes + 30 minutes clarification = **75 minutes to share incomplete data**

**After (Acode Export Bundle - 2 minutes):**
1. Identify failed run: `acode runs list --status failed --limit 5` (5 sec)
2. Export run to bundle: `acode runs export --run run-prod-failure-20240115 --output incident-12345.zip` (20 sec - creates self-contained bundle)
3. Upload bundle: `acode runs export --run run-prod-failure-20240115 --upload s3://incident-bucket/incident-12345.zip` (30 sec - streaming upload)
4. Share S3 link with team (5 sec)
5. Offshore team imports: `acode runs import incident-12345.zip` (15 sec)
6. Bundle includes: stdout/stderr, environment (redacted), command, git commit SHA, config files, provenance
7. Team has complete context: `acode runs show run-prod-failure-20240115` shows all details
8. **Total time:** 2 minutes for complete, self-contained data package

**Metrics:**
- Time saved: 73 minutes per incident (97% improvement)
- Completeness: 100% context (vs 40% with manual logs)
- Compliance: Automatic redaction of secrets before export
- Collaboration: Offshore team productive immediately (vs 30+ min delays)
- Annual ROI (assuming 2 production incidents per month): 73 min × 2 × 12 = **1,752 minutes/year = 29.2 hours saved**
- Cost savings at $150/hour (SRE rate): **$4,380/year**

---

### Use Case 2: Compliance Team Archiving Q4 Deployment Records (Morgan, Compliance Auditor)

**Persona:** Morgan is a compliance auditor who must archive all Q4 production deployments for SOC 2 audit. Records must be stored for 7 years with proof of integrity. Manual archival is error-prone and incomplete.

**Before (Manual Archive Creation - 12 hours for quarterly archive):**
1. Query database for all Q4 deployments: `SELECT * FROM deployments WHERE date >= '2023-10-01' AND date <= '2023-12-31'` (10 min)
2. Export to CSV manually (5 min)
3. For each deployment (48 deployments in Q4):
   a. SSH to deployment server (2 min each)
   b. Locate deployment logs: `cd /var/log/deployments/deploy-xyz/` (1 min)
   c. Copy logs to archive directory (1 min)
   d. Record git commit SHA manually from Jenkins (2 min)
   e. Document environment variables in Excel (3 min)
   f. Save screenshots of deployment UI (2 min)
   g. **Subtotal per deployment:** 11 minutes
4. 48 deployments × 11 min = **528 minutes = 8.8 hours**
5. Create final archive: `tar czf q4-2023-deployments.tar.gz archive/` (10 min)
6. Upload to compliance S3 bucket manually (15 min)
7. Generate SHA-256 hash manually: `sha256sum q4-2023-deployments.tar.gz > checksum.txt` (2 min)
8. Document in compliance spreadsheet (30 min - manual data entry)
9. **Total time:** 12 hours, **high risk of missing data or errors**

**After (Acode Export Bundle - 30 minutes for quarterly archive):**
1. Export all Q4 deployments in single command:
   ```bash
   acode runs export --task deploy \
                      --from 2023-10-01 \
                      --to 2023-12-31 \
                      --format compliance \
                      --output q4-2023-deployments.zip \
                      --sign
   ```
   (5 minutes - selects 48 runs, includes all artifacts, generates manifest)
2. Manifest automatically includes:
   - SHA-256 hashes for all artifacts
   - Git commit SHAs (from provenance)
   - Redacted environment variables
   - Execution timestamps, exit codes, durations
   - Signature for integrity verification
3. Upload to compliance S3: `aws s3 cp q4-2023-deployments.zip s3://compliance/archives/` (10 min)
4. Verify bundle: `acode runs verify q4-2023-deployments.zip` (5 sec - checks all hashes)
5. Import to compliance archive workspace for spot-checking: `acode runs import q4-2023-deployments.zip --workspace compliance-archive` (10 min)
6. Generate audit report from bundle metadata: `acode runs report q4-2023-deployments.zip --format xlsx` (2 min - auto-generates Excel)
7. Review and submit (3 min)
8. **Total time:** 30 minutes, **100% data completeness, cryptographic integrity proof**

**Metrics:**
- Time saved: 11.5 hours per quarterly archive (96% improvement)
- Accuracy: 100% complete (vs 85% with manual process - often missing env vars or commit SHAs)
- Auditability: Cryptographic signatures prove integrity (vs trust-based manual process)
- Compliance risk: Eliminated (all required fields automatically captured)
- Annual ROI (4 quarterly archives): 11.5 hours × 4 = **46 hours saved per year**
- Cost savings at $120/hour (auditor rate): **$5,520/year**

---

### Use Case 3: Developer Reproducing Customer-Reported Bug Across Environments (Casey, Support Engineer)

**Persona:** Casey is a support engineer who received a customer bug report. The customer says "feature X doesn't work" but Casey can't reproduce it locally. Customer is using a different OS, different config, and won't share their exact environment due to confidentiality.

**Before (Manual Environment Recreation - 4 hours + often fails):**
1. Ask customer for environment details via email (30 min - back and forth)
2. Customer provides partial info: OS, app version (missing: env vars, config files, dependency versions)
3. Install matching OS version in VM (45 min - download, setup)
4. Install app version (10 min)
5. Try to reproduce bug with guessed configuration (30 min - doesn't reproduce)
6. Ask customer for more details (30 min email round-trip)
7. Customer shares config file via email (security team flags sensitive data exposure)
8. Sanitize config manually (15 min)
9. Retry reproduction (20 min - still doesn't reproduce because missing env vars)
10. Third round of email asking for env vars (30 min)
11. Customer refuses due to security policy (contains API keys)
12. **Result: Cannot reproduce bug, issue stuck for days**
13. **Total time:** 4 hours, **success rate: 30%**

**After (Acode Export Bundle with Redaction - 15 minutes, 95% success rate):**
1. Ask customer to export run: `acode runs export --run run-feature-x-failure --output customer-bug.zip` (30 sec - customer runs command)
2. Bundle auto-redacts secrets (API keys, passwords) but preserves structure
3. Customer emails bundle (5 min - small ZIP file, no manual sanitization needed)
4. Casey imports: `acode runs import customer-bug.zip` (10 sec)
5. Casey reviews run: `acode runs show run-feature-x-failure` (10 sec)
6. Bundle includes:
   - Exact command executed
   - Redacted environment variables (shows keys but masks values: `API_KEY=***REDACTED***`)
   - Config files (customer can review redaction before sharing)
   - stdout/stderr showing actual error
   - Git commit SHA (customer's app version)
   - OS information from metadata
7. Casey sees error: `stderr.txt` shows "Connection refused on port 5432" - database not running
8. Root cause identified: Customer's PostgreSQL wasn't started
9. Casey replies with fix: "Start PostgreSQL service before running app"
10. **Total time:** 15 minutes, **bug reproduced and fixed**

**Metrics:**
- Time saved: 3 hours 45 minutes per bug investigation (94% improvement)
- Success rate: 95% (vs 30% with manual environment recreation)
- Security: Automatic redaction prevents leaking customer secrets
- Customer satisfaction: Fast resolution (15 min vs days)
- Annual ROI (assuming 3 customer bugs per week): 3.75 hours × 3 × 52 = **585 hours saved per year**
- Cost savings at $100/hour (support engineer rate): **$58,500/year**

---

### Combined ROI Summary for Use Cases

| Use Case | Time Saved | Annual Hours Saved | Annual Cost Savings |
|----------|------------|-------------------|---------------------|
| DevOps sharing production failure bundle | 73 min/incident | 29.2 hours | $4,380 |
| Compliance archiving Q4 deployment records | 11.5 hours/quarter | 46 hours | $5,520 |
| Developer reproducing customer bug | 3.75 hours/bug | 585 hours | $58,500 |
| **TOTAL** | | **660.2 hours/year** | **$68,400/year** |

**Payback Period:** Assuming 60 hours development effort at $100/hour = $6,000 investment, payback in **0.9 months** (32 days).

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

## Assumptions

### Technical Assumptions

1. **ZIP format universality** - All target platforms support ZIP extraction (Windows, Linux, macOS)
2. **SHA-256 sufficiency** - SHA-256 provides adequate integrity verification for bundles
3. **JSON compatibility** - All platforms can parse JSON manifest files
4. **UTF-8 encoding** - All text artifacts use UTF-8 encoding
5. **File system compatibility** - Target systems support long file paths (>260 chars on Windows with long path support enabled)
6. **SQLite portability** - If including database, SQLite files are cross-platform compatible
7. **Git available** - Provenance extraction assumes git command is available
8. **Disk space available** - Export destination has sufficient space (2x bundle size for temp operations)
9. **Memory sufficient** - System has enough RAM to buffer artifacts during compression (recommended 4GB+)
10. **Clock synchronization** - System clocks are reasonably synchronized for timestamp accuracy

### Operational Assumptions

11. **Bundle sizes reasonable** - Most bundles <100MB, largest <10GB (not designed for multi-TB exports)
12. **Infrequent exports** - Export operations are occasional (daily/weekly), not continuous streaming
13. **Single user exports** - Concurrent exports from same workspace are rare
14. **Import trust** - Users import bundles from trusted sources (no automatic signature verification by default)
15. **Storage durability** - Bundle storage (S3, filesystem) provides durability guarantees
16. **Redaction completeness** - Redaction patterns catch most secrets, but manual review recommended for sensitive exports
17. **Network available for upload** - If using `--upload`, network connectivity is stable
18. **Workspace writable** - Import destination workspace has write permissions
19. **No schema changes mid-export** - Database schema doesn't change during export process
20. **Retention policy respected** - Old bundles are managed by external retention policies, not automatic cleanup

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
│       ├── OutboxSummary.cs
│       ├── ExportedRun.cs
│       └── IBundleExporter.cs
├── Acode.Application/
│   └── Export/
│       ├── Commands/
│       │   ├── ExportBundleCommand.cs
│       │   └── ImportBundleCommand.cs
│       └── Handlers/
│           ├── ExportBundleHandler.cs
│           └── ImportBundleHandler.cs
├── Acode.Infrastructure/
│   └── Export/
│       ├── ZipBundleExporter.cs
│       ├── ZipBundleImporter.cs
│       ├── RedactionService.cs
│       ├── HashCalculator.cs
│       ├── BundleValidator.cs
│       └── SignatureService.cs
└── Acode.Cli/
    └── Commands/
        └── Runs/
            ├── RunsExportCommand.cs
            └── RunsImportCommand.cs
```

### Domain Models

```csharp
// BundleManifest.cs
namespace Acode.Domain.Export;

public sealed record BundleManifest
{
    public const string CurrentVersion = "1.0.0";
    public const string FileName = "manifest.json";
    
    public required string Version { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string ToolVersion { get; init; }
    public required int RunCount { get; init; }
    public required long TotalArtifactBytes { get; init; }
    public required IReadOnlyDictionary<string, FileHash> Files { get; init; }
    public IReadOnlyList<string> MissingArtifacts { get; init; } = Array.Empty<string>();
    
    public bool IsCompatible(string readerVersion)
    {
        if (!TryGetMajorVersion(Version, out var bundleMajor))
        {
            // If the bundle manifest has an invalid version, treat it as incompatible.
            return false;
        }

        if (!TryGetMajorVersion(readerVersion, out var readerMajor))
        {
            // If the reader's version is invalid, conservatively treat as incompatible.
            return false;
        }

        return bundleMajor <= readerMajor;
    }

    private static bool TryGetMajorVersion(string? version, out int major)
    {
        major = 0;

        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        var parts = version.Split('.');
        if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
        {
            return false;
        }

        return int.TryParse(parts[0], out major);
    }
}

public sealed record FileHash
{
    public required string Sha256 { get; init; }
    public required long SizeBytes { get; init; }
}

// Provenance.cs
namespace Acode.Domain.Export;

public sealed record Provenance
{
    public const string FileName = "provenance.json";
    
    public string? RemoteUrl { get; init; }
    public string? CommitSha { get; init; }
    public string? Branch { get; init; }
    public string? WorktreeId { get; init; }
    public required DateTimeOffset ExportTimestamp { get; init; }
    public string? ExportedBy { get; init; }
    public string? MachineName { get; init; }
}

// OutboxSummary.cs
namespace Acode.Domain.Export;

public sealed record OutboxSummary
{
    public const string FileName = "outbox-summary.json";
    
    public required int PendingCount { get; init; }
    public required int AcknowledgedCount { get; init; }
    public required int FailedCount { get; init; }
    public DateTimeOffset? OldestPending { get; init; }
    public DateTimeOffset? NewestPending { get; init; }
}

// ExportedRun.cs
namespace Acode.Domain.Export;

public sealed record ExportedRun
{
    public required string RunId { get; init; }
    public required string TaskName { get; init; }
    public required DateTimeOffset StartTime { get; init; }
    public required DateTimeOffset EndTime { get; init; }
    public required long DurationMs { get; init; }
    public required int ExitCode { get; init; }
    public required string Status { get; init; }
    public required string Command { get; init; }
    public required string WorkingDirectory { get; init; }
    public required string OperatingMode { get; init; }
    public string? ContainerId { get; init; }
    public IReadOnlyDictionary<string, string> Environment { get; init; } = 
        new Dictionary<string, string>();
    public IReadOnlyList<string> Artifacts { get; init; } = Array.Empty<string>();
}

// IBundleExporter.cs
namespace Acode.Domain.Export;

public interface IBundleExporter
{
    Task<ExportResult> ExportAsync(
        ExportOptions options,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed record ExportOptions
{
    public IReadOnlyList<string>? RunIds { get; init; }
    public DateTimeOffset? Since { get; init; }
    public DateTimeOffset? Until { get; init; }
    public int? LastN { get; init; }
    public bool ExportAll { get; init; }
    public bool IncludeArtifacts { get; init; } = true;
    public bool ApplyRedaction { get; init; } = true;
    public bool Sign { get; init; }
    public string? OutputPath { get; init; }
}

public sealed record ExportResult
{
    public required bool Success { get; init; }
    public required string OutputPath { get; init; }
    public required int RunCount { get; init; }
    public required long TotalBytes { get; init; }
    public required TimeSpan Duration { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

public sealed record ExportProgress
{
    public required string Phase { get; init; }
    public required int CurrentItem { get; init; }
    public required int TotalItems { get; init; }
    public string? CurrentFile { get; init; }
}

// IBundleImporter.cs
namespace Acode.Domain.Export;

public interface IBundleImporter
{
    Task<ImportPreview> PreviewAsync(
        string bundlePath,
        CancellationToken cancellationToken = default);
    
    Task<ImportResult> ImportAsync(
        string bundlePath,
        ImportOptions options,
        IProgress<ImportProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed record ImportOptions
{
    public MergeStrategy Strategy { get; init; } = MergeStrategy.Merge;
    public bool VerifySignature { get; init; }
    public bool Force { get; init; }
}

public enum MergeStrategy
{
    Merge,      // Add new, skip existing
    Replace,    // Overwrite existing
    SkipExisting // Only add new, skip all conflicts
}

public sealed record ImportPreview
{
    public required BundleManifest Manifest { get; init; }
    public required Provenance Provenance { get; init; }
    public required int NewRuns { get; init; }
    public required int ConflictingRuns { get; init; }
    public required long TotalBytes { get; init; }
    public IReadOnlyList<string> Conflicts { get; init; } = Array.Empty<string>();
}

public sealed record ImportResult
{
    public required bool Success { get; init; }
    public required int ImportedRuns { get; init; }
    public required int SkippedRuns { get; init; }
    public required int ReplacedRuns { get; init; }
    public required TimeSpan Duration { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}
```

### Infrastructure Implementation

```csharp
// ZipBundleExporter.cs
namespace Acode.Infrastructure.Export;

public sealed class ZipBundleExporter : IBundleExporter
{
    private readonly IRunRepository _runRepository;
    private readonly IArtifactReader _artifactReader;
    private readonly IRedactionService _redactionService;
    private readonly IHashCalculator _hashCalculator;
    private readonly IProvenanceService _provenanceService;
    private readonly IOutboxService _outboxService;
    private readonly ISignatureService _signatureService;
    private readonly ILogger<ZipBundleExporter> _logger;
    
    public async Task<ExportResult> ExportAsync(
        ExportOptions options,
        IProgress<ExportProgress>? progress = null,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var tempPath = Path.GetTempFileName();
        
        try
        {
            var runs = await GetRunsToExportAsync(options, ct);
            
            if (runs.Count == 0)
            {
                throw new ExportException("No runs match the selection criteria");
            }
            
            var outputPath = options.OutputPath ?? 
                $"acode-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.acode-bundle";
            
            await using var zipStream = File.Create(tempPath);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);
            
            var fileHashes = new Dictionary<string, FileHash>();
            var missingArtifacts = new List<string>();
            long totalBytes = 0;
            
            // Export runs
            for (var i = 0; i < runs.Count; i++)
            {
                progress?.Report(new ExportProgress
                {
                    Phase = "Exporting runs",
                    CurrentItem = i + 1,
                    TotalItems = runs.Count,
                    CurrentFile = runs[i].RunId
                });
                
                var run = runs[i];
                var exportedRun = await ExportRunAsync(run, options, ct);
                
                var runPath = $"runs/{run.RunId}.json";
                var runJson = JsonSerializer.Serialize(exportedRun, JsonOptions.Pretty);
                var runBytes = Encoding.UTF8.GetBytes(runJson);
                
                var entry = archive.CreateEntry(runPath);
                await using var entryStream = entry.Open();
                await entryStream.WriteAsync(runBytes, ct);
                
                fileHashes[runPath] = new FileHash
                {
                    Sha256 = _hashCalculator.ComputeSha256(runBytes),
                    SizeBytes = runBytes.Length
                };
                totalBytes += runBytes.Length;
                
                // Export artifacts
                if (options.IncludeArtifacts)
                {
                    foreach (var artifact in run.Artifacts)
                    {
                        var artifactPath = $"artifacts/{run.RunId}/{artifact}";
                        
                        if (!await _artifactReader.ExistsAsync(run.RunId, artifact))
                        {
                            missingArtifacts.Add(artifactPath);
                            continue;
                        }
                        
                        var artifactEntry = archive.CreateEntry(artifactPath);
                        await using var artifactStream = artifactEntry.Open();
                        
                        await using var sourceStream = 
                            await _artifactReader.OpenAsync(run.RunId, artifact, ct);
                        
                        var hash = await _hashCalculator.ComputeSha256StreamingAsync(
                            sourceStream, artifactStream, ct);
                        
                        fileHashes[artifactPath] = new FileHash
                        {
                            Sha256 = hash,
                            SizeBytes = sourceStream.Position
                        };
                        totalBytes += sourceStream.Position;
                    }
                }
            }
            
            // Add provenance
            var provenance = await _provenanceService.GetCurrentAsync(ct);
            var provenanceJson = JsonSerializer.Serialize(provenance, JsonOptions.Pretty);
            var provenanceEntry = archive.CreateEntry(Provenance.FileName);
            await using (var ps = provenanceEntry.Open())
            {
                await ps.WriteAsync(Encoding.UTF8.GetBytes(provenanceJson), ct);
            }
            fileHashes[Provenance.FileName] = new FileHash
            {
                Sha256 = _hashCalculator.ComputeSha256(provenanceJson),
                SizeBytes = Encoding.UTF8.GetByteCount(provenanceJson)
            };
            
            // Add outbox summary
            var outboxSummary = await _outboxService.GetSummaryAsync(ct);
            var outboxJson = JsonSerializer.Serialize(outboxSummary, JsonOptions.Pretty);
            var outboxEntry = archive.CreateEntry(OutboxSummary.FileName);
            await using (var os = outboxEntry.Open())
            {
                await os.WriteAsync(Encoding.UTF8.GetBytes(outboxJson), ct);
            }
            fileHashes[OutboxSummary.FileName] = new FileHash
            {
                Sha256 = _hashCalculator.ComputeSha256(outboxJson),
                SizeBytes = Encoding.UTF8.GetByteCount(outboxJson)
            };
            
            // Add manifest (last, after all hashes computed)
            var manifest = new BundleManifest
            {
                Version = BundleManifest.CurrentVersion,
                CreatedAt = DateTimeOffset.UtcNow,
                ToolVersion = typeof(ZipBundleExporter).Assembly.GetName().Version?.ToString() ?? "0.0.0",
                RunCount = runs.Count,
                TotalArtifactBytes = totalBytes,
                Files = fileHashes,
                MissingArtifacts = missingArtifacts
            };
            
            var manifestJson = JsonSerializer.Serialize(manifest, JsonOptions.Pretty);
            var manifestEntry = archive.CreateEntry(BundleManifest.FileName);
            await using (var ms = manifestEntry.Open())
            {
                await ms.WriteAsync(Encoding.UTF8.GetBytes(manifestJson), ct);
            }
            
            // Sign if requested
            if (options.Sign)
            {
                var signature = await _signatureService.SignAsync(tempPath, ct);
                var sigEntry = archive.CreateEntry("signature.sig");
                await using var ss = sigEntry.Open();
                await ss.WriteAsync(signature, ct);
            }
            
            // Close archive and move to final location
            archive.Dispose();
            zipStream.Dispose();
            
            File.Move(tempPath, outputPath, overwrite: true);
            
            _logger.LogInformation(
                "Exported {RunCount} runs to {Path} ({Bytes} bytes)",
                runs.Count, outputPath, totalBytes);
            
            return new ExportResult
            {
                Success = true,
                OutputPath = outputPath,
                RunCount = runs.Count,
                TotalBytes = totalBytes,
                Duration = stopwatch.Elapsed,
                Warnings = missingArtifacts.Select(a => $"Missing artifact: {a}").ToList()
            };
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
            throw;
        }
    }
    
    private async Task<ExportedRun> ExportRunAsync(
        RunDetails run,
        ExportOptions options,
        CancellationToken ct)
    {
        var environment = options.ApplyRedaction
            ? _redactionService.RedactEnvironment(run.Environment)
            : run.Environment;
        
        var command = options.ApplyRedaction
            ? _redactionService.RedactCommand(run.Command)
            : run.Command;
        
        return new ExportedRun
        {
            RunId = run.Id.Value,
            TaskName = run.TaskName,
            StartTime = run.StartTime,
            EndTime = run.EndTime,
            DurationMs = (long)run.Duration.TotalMilliseconds,
            ExitCode = run.ExitCode,
            Status = run.Status.ToString(),
            Command = command,
            WorkingDirectory = run.WorkingDirectory,
            OperatingMode = run.OperatingMode,
            ContainerId = run.ContainerId,
            Environment = environment,
            Artifacts = run.Artifacts.Select(a => a.FileName).ToList()
        };
    }
}

// RedactionService.cs
namespace Acode.Infrastructure.Export;

public sealed class RedactionService : IRedactionService
{
    private readonly IReadOnlyList<string> _patterns;
    private const string RedactedValue = "[REDACTED]";
    
    public RedactionService(IOptions<RedactionConfig> config)
    {
        _patterns = config.Value.Patterns ?? new[]
        {
            "PASSWORD", "SECRET", "KEY", "TOKEN", "API_KEY",
            "APIKEY", "CREDENTIAL", "PRIVATE", "AUTH", "BEARER"
        };
    }
    
    public IReadOnlyDictionary<string, string> RedactEnvironment(
        IReadOnlyDictionary<string, string> environment)
    {
        var redacted = new Dictionary<string, string>(environment.Count);
        
        foreach (var (key, value) in environment)
        {
            redacted[key] = ShouldRedact(key, value) ? RedactedValue : value;
        }
        
        return redacted;
    }
    
    public string RedactCommand(string command)
    {
        foreach (var pattern in _patterns)
        {
            // Redact --password=value, --secret value patterns
            var regex = new Regex(
                $@"(--{pattern}[=\s]+)(\S+)",
                RegexOptions.IgnoreCase);
            command = regex.Replace(command, $"$1{RedactedValue}");
        }
        
        return command;
    }
    
    private bool ShouldRedact(string key, string value)
    {
        return _patterns.Any(p => 
            key.Contains(p, StringComparison.OrdinalIgnoreCase));
    }
}

// BundleValidator.cs
namespace Acode.Infrastructure.Export;

public sealed class BundleValidator : IBundleValidator
{
    private readonly IHashCalculator _hashCalculator;
    private const long MaxDecompressionRatio = 100; // ZIP bomb protection
    
    public async Task<ValidationResult> ValidateAsync(
        string bundlePath,
        CancellationToken ct = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        
        try
        {
            using var archive = ZipFile.OpenRead(bundlePath);
            
            // Check for manifest
            var manifestEntry = archive.GetEntry(BundleManifest.FileName);
            if (manifestEntry is null)
            {
                errors.Add("Bundle missing manifest.json");
                return new ValidationResult(false, errors, warnings);
            }
            
            // Parse manifest
            await using var ms = manifestEntry.Open();
            var manifest = await JsonSerializer.DeserializeAsync<BundleManifest>(ms, ct: ct);
            
            if (manifest is null)
            {
                errors.Add("Invalid manifest.json format");
                return new ValidationResult(false, errors, warnings);
            }
            
            // Version check
            if (!manifest.IsCompatible(BundleManifest.CurrentVersion))
            {
                errors.Add($"Incompatible bundle version: {manifest.Version}");
                return new ValidationResult(false, errors, warnings);
            }
            
            // ZIP bomb check
            var totalDecompressed = archive.Entries.Sum(e => e.Length);
            var compressedSize = new FileInfo(bundlePath).Length;
            
            if (totalDecompressed > compressedSize * MaxDecompressionRatio)
            {
                errors.Add("Potential ZIP bomb detected");
                return new ValidationResult(false, errors, warnings);
            }
            
            // Hash verification
            foreach (var (path, expectedHash) in manifest.Files)
            {
                var entry = archive.GetEntry(path);
                if (entry is null)
                {
                    errors.Add($"Missing file: {path}");
                    continue;
                }
                
                // Path traversal check
                if (path.Contains("..") || Path.IsPathRooted(path))
                {
                    errors.Add($"Invalid path (traversal detected): {path}");
                    continue;
                }
                
                await using var stream = entry.Open();
                var actualHash = await _hashCalculator.ComputeSha256Async(stream, ct);
                
                if (actualHash != expectedHash.Sha256)
                {
                    errors.Add($"Hash mismatch for {path}");
                }
            }
            
            return new ValidationResult(errors.Count == 0, errors, warnings);
        }
        catch (InvalidDataException)
        {
            errors.Add("Invalid or corrupted ZIP archive");
            return new ValidationResult(false, errors, warnings);
        }
    }
}
```

### CLI Commands

```csharp
// RunsExportCommand.cs
namespace Acode.Cli.Commands.Runs;

[Command("runs export", Description = "Export runs to a bundle")]
public class RunsExportCommand
{
    [Option("-o|--output", Description = "Output file path")]
    public string? Output { get; set; }
    
    [Option("--run", Description = "Run ID to export (repeatable)")]
    public string[] RunIds { get; set; } = Array.Empty<string>();
    
    [Option("--since", Description = "Export runs after date")]
    public string? Since { get; set; }
    
    [Option("--last", Description = "Export last N runs")]
    public int? Last { get; set; }
    
    [Option("--no-artifacts", Description = "Exclude artifact files")]
    public bool NoArtifacts { get; set; }
    
    [Option("--no-redact", Description = "Skip redaction (WARNING)")]
    public bool NoRedact { get; set; }
    
    [Option("--sign", Description = "Sign the bundle")]
    public bool Sign { get; set; }
    
    public async Task<int> ExecuteAsync(
        IBundleExporter exporter,
        IConsole console,
        CancellationToken ct)
    {
        if (NoRedact)
        {
            console.Error.WriteLine("WARNING: Exporting without redaction may expose secrets!");
        }
        
        var options = new ExportOptions
        {
            RunIds = RunIds.Length > 0 ? RunIds.ToList() : null,
            Since = ParseDate(Since),
            LastN = Last,
            IncludeArtifacts = !NoArtifacts,
            ApplyRedaction = !NoRedact,
            Sign = Sign,
            OutputPath = Output
        };
        
        var progress = new Progress<ExportProgress>(p =>
        {
            console.Write($"\r{p.Phase}: {p.CurrentItem}/{p.TotalItems}");
        });
        
        var result = await exporter.ExportAsync(options, progress, ct);
        
        console.WriteLine();
        console.WriteLine($"✓ Exported {result.RunCount} runs to {result.OutputPath}");
        console.WriteLine($"  Size: {FormatBytes(result.TotalBytes)}");
        console.WriteLine($"  Duration: {result.Duration.TotalSeconds:F1}s");
        
        foreach (var warning in result.Warnings)
        {
            console.Error.WriteLine($"  ⚠ {warning}");
        }
        
        return 0;
    }
}

// RunsImportCommand.cs
namespace Acode.Cli.Commands.Runs;

[Command("runs import", Description = "Import runs from a bundle")]
public class RunsImportCommand
{
    [Argument(0, Description = "Bundle file path")]
    public string BundlePath { get; set; } = "";
    
    [Option("--dry-run", Description = "Preview without changes")]
    public bool DryRun { get; set; }
    
    [Option("--merge", Description = "Add new, skip existing (default)")]
    public bool Merge { get; set; }
    
    [Option("--replace", Description = "Overwrite existing runs")]
    public bool Replace { get; set; }
    
    [Option("--skip-existing", Description = "Skip all conflicts")]
    public bool SkipExisting { get; set; }
    
    [Option("--verify", Description = "Verify bundle signature")]
    public bool Verify { get; set; }
    
    [Option("--force", Description = "Skip confirmation prompts")]
    public bool Force { get; set; }
    
    public async Task<int> ExecuteAsync(
        IBundleImporter importer,
        IConsole console,
        CancellationToken ct)
    {
        // Preview first
        var preview = await importer.PreviewAsync(BundlePath, ct);
        
        console.WriteLine($"Bundle: {Path.GetFileName(BundlePath)}");
        console.WriteLine($"  Version: {preview.Manifest.Version}");
        console.WriteLine($"  Created: {preview.Manifest.CreatedAt:yyyy-MM-dd HH:mm}");
        console.WriteLine($"  Runs: {preview.Manifest.RunCount}");
        console.WriteLine($"  Size: {FormatBytes(preview.TotalBytes)}");
        console.WriteLine();
        console.WriteLine($"  New runs: {preview.NewRuns}");
        console.WriteLine($"  Conflicts: {preview.ConflictingRuns}");
        
        if (DryRun)
        {
            console.WriteLine();
            console.WriteLine("(Dry run - no changes made)");
            return 0;
        }
        
        if (preview.ConflictingRuns > 0 && !Force)
        {
            console.WriteLine();
            console.WriteLine($"Found {preview.ConflictingRuns} conflicting runs.");
            console.Write("Continue with merge? [y/N]: ");
            var response = Console.ReadLine();
            if (!response?.Equals("y", StringComparison.OrdinalIgnoreCase) ?? true)
            {
                console.WriteLine("Cancelled.");
                return 0;
            }
        }
        
        var strategy = Replace ? MergeStrategy.Replace
            : SkipExisting ? MergeStrategy.SkipExisting
            : MergeStrategy.Merge;
        
        var options = new ImportOptions
        {
            Strategy = strategy,
            VerifySignature = Verify,
            Force = Force
        };
        
        var result = await importer.ImportAsync(BundlePath, options, null, ct);
        
        console.WriteLine();
        console.WriteLine($"✓ Imported {result.ImportedRuns} runs");
        
        if (result.SkippedRuns > 0)
            console.WriteLine($"  Skipped: {result.SkippedRuns}");
        if (result.ReplacedRuns > 0)
            console.WriteLine($"  Replaced: {result.ReplacedRuns}");
        
        console.WriteLine($"  Duration: {result.Duration.TotalSeconds:F1}s");
        
        return result.Success ? 0 : 1;
    }
}
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
    "runs/run-001.json": {
      "sha256": "abc123...",
      "sizeBytes": 1024
    },
    "artifacts/run-001/stdout.txt": {
      "sha256": "def456...",
      "sizeBytes": 45678
    }
  },
  "missingArtifacts": []
}
```

### Error Codes

| Code | Name | Description | Recovery |
|------|------|-------------|----------|
| ACODE-EXP-001 | OutputPathExists | Output file already exists | Use --force or different path |
| ACODE-EXP-002 | NoRunsSelected | No runs match criteria | Adjust filter options |
| ACODE-EXP-003 | ArtifactMissing | Artifact file not found | Warning logged, export continues |
| ACODE-EXP-004 | SigningFailed | Cannot sign bundle | Check signing key config |
| ACODE-EXP-005 | DiskFull | Insufficient disk space | Free disk space |
| ACODE-IMP-001 | InvalidFormat | Bundle format unrecognized | Use valid .acode-bundle |
| ACODE-IMP-002 | VersionMismatch | Incompatible bundle version | Update acode |
| ACODE-IMP-003 | HashMismatch | Content verification failed | Obtain fresh bundle |
| ACODE-IMP-004 | SignatureInvalid | Signature verification failed | Check public key |
| ACODE-IMP-005 | PathTraversal | Malicious path in bundle | Do not import |
| ACODE-IMP-006 | ZipBomb | Decompression attack detected | Do not import |

### Implementation Checklist

- [ ] Create domain models for BundleManifest, Provenance, OutboxSummary
- [ ] Implement IBundleExporter interface
- [ ] Implement ZipBundleExporter with streaming
- [ ] Implement IBundleImporter interface
- [ ] Implement ZipBundleImporter with validation
- [ ] Implement RedactionService with configurable patterns
- [ ] Implement HashCalculator for SHA-256
- [ ] Implement BundleValidator with security checks
- [ ] Implement SignatureService for signing/verification
- [ ] Create RunsExportCommand with all options
- [ ] Create RunsImportCommand with merge strategies
- [ ] Add progress reporting for large bundles
- [ ] Add unit tests for all components
- [ ] Add integration tests for round-trip
- [ ] Add E2E tests for CLI commands
- [ ] Performance test with large bundles
- [ ] Security test for path traversal and ZIP bombs
- [ ] Document configuration options

### Rollout Plan

| Phase | Action | Validation |
|-------|--------|------------|
| 1 | Implement domain models | Unit tests pass |
| 2 | Implement RedactionService | Redaction tests pass |
| 3 | Implement HashCalculator | Hash tests pass |
| 4 | Implement ZipBundleExporter | Export tests pass |
| 5 | Implement BundleValidator | Validation tests pass |
| 6 | Implement ZipBundleImporter | Import tests pass |
| 7 | Implement SignatureService | Signing tests pass |
| 8 | Add CLI commands | E2E tests pass |
| 9 | Performance testing | <30s for 100 runs |
| 10 | Security testing | No vulnerabilities |
| 11 | Documentation and release | User manual complete |

---

**End of Task 021.c Specification**