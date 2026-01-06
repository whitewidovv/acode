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

## Security Considerations

### Threat 1: ZIP Path Traversal During Import

**Risk:** Malicious bundle contains entries with paths like `../../../etc/passwd` or `..\..\..\Windows\System32\config`. Importing could overwrite critical system files or write files outside workspace directory.

**Attack Scenario:**
1. Attacker crafts malicious bundle: `malicious.acode-bundle`
2. ZIP contains entry: `runs/../../../../../../tmp/malicious.sh`
3. User imports: `acode runs import malicious.acode-bundle`
4. Extraction writes file to `/tmp/malicious.sh` (outside workspace)
5. If `/tmp/malicious.sh` executed later (cron, boot script), attacker gains code execution

**Mitigation (C# - SafeZipExtractor):**

```csharp
namespace Acode.Infrastructure.Export;

public sealed class SafeZipExtractor
{
    private readonly string _extractionRoot;
    private static readonly char[] ForbiddenPathChars = Path.GetInvalidPathChars()
        .Concat(new[] { '\0', '\r', '\n' }).ToArray();

    public SafeZipExtractor(string extractionRoot)
    {
        _extractionRoot = Path.GetFullPath(extractionRoot);
    }

    public async Task<ExtractResult> ExtractAsync(string bundlePath, CancellationToken ct = default)
    {
        if (!File.Exists(bundlePath))
            throw new FileNotFoundException($"Bundle not found: {bundlePath}");

        using var archive = ZipFile.OpenRead(bundlePath);
        var extractedFiles = new List<string>();

        foreach (var entry in archive.Entries)
        {
            // Step 1: Validate entry name
            var (isValid, error) = ValidateEntryPath(entry.FullName);
            if (!isValid)
                throw new SecurityException($"Path traversal detected in entry '{entry.FullName}': {error}");

            // Step 2: Compute absolute target path
            var targetPath = Path.Combine(_extractionRoot, entry.FullName);
            var normalizedPath = Path.GetFullPath(targetPath);

            // Step 3: CRITICAL - Verify normalized path is still within extraction root
            if (!normalizedPath.StartsWith(_extractionRoot, StringComparison.OrdinalIgnoreCase))
                throw new SecurityException($"Entry '{entry.FullName}' resolves outside extraction directory: {normalizedPath}");

            // Step 4: Create parent directory safely
            var parentDir = Path.GetDirectoryName(normalizedPath);
            if (!string.IsNullOrEmpty(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }

            // Step 5: Extract file
            if (entry.FullName.EndsWith('/'))
            {
                // Directory entry - already created above
                continue;
            }

            entry.ExtractToFile(normalizedPath, overwrite: true);
            extractedFiles.Add(normalizedPath);

            ct.ThrowIfCancellationRequested();
        }

        return new ExtractResult { FilesExtracted = extractedFiles, Success = true };
    }

    private (bool IsValid, string Error) ValidateEntryPath(string entryPath)
    {
        // Rule 1: Not null or empty
        if (string.IsNullOrWhiteSpace(entryPath))
            return (false, "Entry path is empty");

        // Rule 2: No forbidden characters
        if (entryPath.IndexOfAny(ForbiddenPathChars) >= 0)
            return (false, "Entry path contains forbidden characters");

        // Rule 3: No absolute paths (Windows drive letters or Unix root)
        if (Path.IsPathRooted(entryPath))
            return (false, "Entry path is absolute (rooted)");

        // Rule 4: No path traversal sequences
        if (entryPath.Contains(".."))
            return (false, "Entry path contains '..' traversal sequence");

        // Rule 5: No backslashes (normalize to forward slashes)
        if (entryPath.Contains('\\'))
            return (false, "Entry path contains backslashes (use forward slashes)");

        // Rule 6: Length bounds
        if (entryPath.Length > 500)
            return (false, "Entry path exceeds maximum length (500 chars)");

        return (true, null);
    }
}

public record ExtractResult
{
    public required List<string> FilesExtracted { get; init; }
    public required bool Success { get; init; }
}
```

**Prevention:**
- Always use `Path.GetFullPath()` and verify result is within extraction root
- Reject any paths containing `..` sequences
- Never trust ZIP entry paths directly - validate every path
- Use whitelisting (allow only specific patterns) vs blacklisting

---

### Threat 2: ZIP Bomb (Decompression Bomb)

**Risk:** Malicious bundle is small when compressed (e.g., 1MB) but expands to consume all disk space when extracted (e.g., 10TB). This causes denial of service by filling disk.

**Attack Scenario:**
1. Attacker creates `bomb.zip` with 1MB compressed size, 10TB uncompressed (nested ZIPs or highly compressible data like zeros)
2. User imports: `acode runs import bomb.acode-bundle`
3. Extraction begins, disk fills: `/dev/sda1 100% full`
4. System becomes unusable, databases crash, services fail
5. Incident response team must restore from backups

**Mitigation (C# - ZipBombDetector):**

```csharp
namespace Acode.Infrastructure.Export;

public sealed class ZipBombDetector
{
    private const long MaxUncompressedSize = 10L * 1024 * 1024 * 1024; // 10GB
    private const int MaxCompressionRatio = 100; // 100:1 ratio
    private const int MaxNestedZips = 2; // Prevent recursive zip bombs

    public (bool IsSafe, string Reason) AnalyzeBundle(string bundlePath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(bundlePath);

            long totalCompressedSize = 0;
            long totalUncompressedSize = 0;
            var zipEntryCount = 0;

            foreach (var entry in archive.Entries)
            {
                // Accumulate sizes
                totalCompressedSize += entry.CompressedLength;
                totalUncompressedSize += entry.Length;

                // Check 1: Total uncompressed size limit
                if (totalUncompressedSize > MaxUncompressedSize)
                    return (false, $"Total uncompressed size ({totalUncompressedSize:N0} bytes) exceeds limit ({MaxUncompressedSize:N0} bytes)");

                // Check 2: Per-file compression ratio
                if (entry.CompressedLength > 0)
                {
                    var ratio = (double)entry.Length / entry.CompressedLength;
                    if (ratio > MaxCompressionRatio)
                        return (false, $"File '{entry.FullName}' has suspicious compression ratio {ratio:F1}:1 (limit: {MaxCompressionRatio}:1)");
                }

                // Check 3: Nested ZIP detection
                if (entry.FullName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                    entry.FullName.EndsWith(".acode-bundle", StringComparison.OrdinalIgnoreCase))
                {
                    zipEntryCount++;
                    if (zipEntryCount > MaxNestedZips)
                        return (false, $"Bundle contains {zipEntryCount} nested ZIP files (limit: {MaxNestedZips})");
                }

                // Check 4: Unusually high entry count (quine zip)
                if (archive.Entries.Count > 100_000)
                    return (false, $"Bundle contains {archive.Entries.Count:N0} entries (limit: 100,000)");
            }

            // Check 5: Overall compression ratio
            if (totalCompressedSize > 0)
            {
                var overallRatio = (double)totalUncompressedSize / totalCompressedSize;
                if (overallRatio > MaxCompressionRatio)
                    return (false, $"Overall compression ratio {overallRatio:F1}:1 exceeds limit ({MaxCompressionRatio}:1)");
            }

            return (true, "Bundle passed safety checks");
        }
        catch (Exception ex)
        {
            return (false, $"Error analyzing bundle: {ex.Message}");
        }
    }
}

// Usage in BundleImporter
public class BundleImporter
{
    private readonly ZipBombDetector _detector = new();
    private readonly SafeZipExtractor _extractor;

    public async Task<ImportResult> ImportAsync(string bundlePath)
    {
        // CRITICAL: Check for zip bomb BEFORE extraction
        var (isSafe, reason) = _detector.AnalyzeBundle(bundlePath);
        if (!isSafe)
            throw new SecurityException($"Bundle rejected: {reason}");

        // Safe to extract
        return await _extractor.ExtractAsync(bundlePath);
    }
}
```

**Prevention:**
- Check total uncompressed size before extracting
- Monitor compression ratios (>100:1 is suspicious)
- Limit number of entries in archive
- Detect nested ZIP files (quine zips)
- Set maximum extraction size limits

---

### Threat 3: Manifest Hash Mismatch (Tampered Artifacts)

**Risk:** Attacker modifies bundle contents after signing but before import. Manifest contains original hashes, but actual files are tampered. User imports malicious artifacts thinking they're verified.

**Attack Scenario:**
1. Legitimate bundle exported: `production-run.acode-bundle` (manifest includes SHA-256 hashes)
2. Attacker intercepts bundle in transit (man-in-the-middle, compromised storage)
3. Attacker modifies `stdout.txt` to inject malicious commands
4. User imports bundle without verification: `acode runs import production-run.acode-bundle`
5. Tampered artifact imported, user reviews "production" output that was actually attacker-modified

**Mitigation (C# - BundleIntegrityVerifier):**

```csharp
namespace Acode.Infrastructure.Export;

public sealed class BundleIntegrityVerifier
{
    private readonly IHashCalculator _hashCalculator;

    public BundleIntegrityVerifier(IHashCalculator hashCalculator)
    {
        _hashCalculator = hashCalculator;
    }

    public async Task<VerificationResult> VerifyAsync(string extractedBundleRoot, BundleManifest manifest, CancellationToken ct = default)
    {
        var errors = new List<IntegrityError>();
        var verifiedCount = 0;

        foreach (var (relativePath, expectedHash) in manifest.Files)
        {
            var fullPath = Path.Combine(extractedBundleRoot, relativePath);

            // Check 1: File exists
            if (!File.Exists(fullPath))
            {
                errors.Add(new IntegrityError
                {
                    FilePath = relativePath,
                    ErrorType = IntegrityErrorType.FileMissing,
                    Message = "File listed in manifest but not found in bundle"
                });
                continue;
            }

            // Check 2: File size matches
            var actualSize = new FileInfo(fullPath).Length;
            if (actualSize != expectedHash.SizeBytes)
            {
                errors.Add(new IntegrityError
                {
                    FilePath = relativePath,
                    ErrorType = IntegrityErrorType.SizeMismatch,
                    Message = $"Size mismatch: expected {expectedHash.SizeBytes:N0} bytes, actual {actualSize:N0} bytes"
                });
                continue; // Don't hash if size wrong - saves time
            }

            // Check 3: SHA-256 hash matches
            var actualHash = await _hashCalculator.ComputeSha256Async(fullPath, ct);
            if (!string.Equals(actualHash, expectedHash.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(new IntegrityError
                {
                    FilePath = relativePath,
                    ErrorType = IntegrityErrorType.HashMismatch,
                    Message = $"Hash mismatch: expected {expectedHash.Sha256}, actual {actualHash}",
                    ExpectedHash = expectedHash.Sha256,
                    ActualHash = actualHash
                });
                continue;
            }

            verifiedCount++;
            ct.ThrowIfCancellationRequested();
        }

        return new VerificationResult
        {
            Success = errors.Count == 0,
            VerifiedCount = verifiedCount,
            Errors = errors,
            TotalFiles = manifest.Files.Count
        };
    }
}

public enum IntegrityErrorType
{
    FileMissing,
    SizeMismatch,
    HashMismatch
}

public record IntegrityError
{
    public required string FilePath { get; init; }
    public required IntegrityErrorType ErrorType { get; init; }
    public required string Message { get; init; }
    public string? ExpectedHash { get; init; }
    public string? ActualHash { get; init; }
}

public record VerificationResult
{
    public required bool Success { get; init; }
    public required int VerifiedCount { get; init; }
    public required int TotalFiles { get; init; }
    public required List<IntegrityError> Errors { get; init; }

    public string GetSummary()
    {
        if (Success)
            return $"All {TotalFiles} files verified successfully";

        return $"Verification failed: {VerifiedCount}/{TotalFiles} files OK, {Errors.Count} errors:\n" +
               string.Join("\n", Errors.Select(e => $"  - {e.FilePath}: {e.Message}"));
    }
}

// HashCalculator implementation
public interface IHashCalculator
{
    Task<string> ComputeSha256Async(string filePath, CancellationToken ct = default);
}

public class HashCalculator : IHashCalculator
{
    public async Task<string> ComputeSha256Async(string filePath, CancellationToken ct = default)
    {
        using var stream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();

        var hashBytes = await sha256.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
```

**Prevention:**
- ALWAYS verify hashes before importing artifacts
- Fail import if ANY hash mismatch detected
- Use cryptographic signatures (detached signature file)
- Display verification results to user prominently

---

### Threat 4: Redaction Bypass via Metadata Channels

**Risk:** Sensitive data (API keys, passwords) leaked through alternative channels even though redaction is applied to environment variables. Attacker gains access to secrets from exported bundle.

**Attack Scenario:**
1. Developer exports run with `--redact` flag (secrets in environment masked)
2. But: command line arguments contain `--api-key sk-abc123` (not redacted)
3. Or: stdout contains echoed environment: `export API_KEY=sk-abc123` (not redacted)
4. Or: provenance includes branch name: `feature/add-api-key-sk-abc123-to-config` (secret in branch name)
5. User shares "redacted" bundle, attacker extracts secrets from these alternative channels

**Mitigation (C# - ComprehensiveRedactor):**

```csharp
namespace Acode.Infrastructure.Export;

public sealed class ComprehensiveRedactor
{
    private static readonly Regex[] SecretPatterns = new[]
    {
        new Regex(@"(?i)(api[_-]?key|password|secret|token|credential)[:=]\s*([^\s]{8,})", RegexOptions.Compiled),
        new Regex(@"sk-[a-zA-Z0-9]{48,}", RegexOptions.Compiled), // OpenAI keys
        new Regex(@"ghp_[a-zA-Z0-9]{36,}", RegexOptions.Compiled), // GitHub tokens
        new Regex(@"xox[baprs]-[a-zA-Z0-9-]{10,}", RegexOptions.Compiled), // Slack tokens
        new Regex(@"AIza[0-9A-Za-z\\-_]{35}", RegexOptions.Compiled), // Google API keys
        new Regex(@"-----BEGIN\s+(RSA\s+)?PRIVATE\s+KEY-----", RegexOptions.Compiled), // Private keys
        new Regex(@"postgres://[^:]+:([^@]+)@", RegexOptions.Compiled), // DB passwords in URLs
        new Regex(@"https://[^:]+:([^@]+)@", RegexOptions.Compiled) // Auth in URLs
    };

    public ExportedRun RedactRun(RunDetails run)
    {
        return new ExportedRun
        {
            Id = run.Id,
            TaskName = run.TaskName,
            StartTime = run.StartTime,
            EndTime = run.EndTime,
            ExitCode = run.ExitCode,
            Status = run.Status,

            // Redact command line
            Command = RedactText(run.Command),

            // Redact environment variables
            Environment = RedactEnvironment(run.Environment),

            // Redact working directory (may contain secrets in path)
            WorkingDirectory = RedactPath(run.WorkingDirectory),

            // Operating mode safe to export
            OperatingMode = run.OperatingMode
        };
    }

    public Provenance RedactProvenance(Provenance provenance)
    {
        return provenance with
        {
            // Redact remote URL (may contain embedded credentials)
            RemoteUrl = RedactUrl(provenance.RemoteUrl),

            // Redact branch name (may contain secrets)
            Branch = RedactText(provenance.Branch),

            // Commit SHA safe to export
            // Worktree ID safe to export
            // Machine name potentially sensitive - redact
            MachineName = RedactHostname(provenance.MachineName)
        };
    }

    public async Task<string> RedactArtifactAsync(string filePath, CancellationToken ct = default)
    {
        var content = await File.ReadAllTextAsync(filePath, ct);
        var redacted = RedactText(content);

        var redactedPath = filePath + ".redacted";
        await File.WriteAllTextAsync(redactedPath, redacted, ct);
        return redactedPath;
    }

    private string RedactText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var redacted = text;
        foreach (var pattern in SecretPatterns)
        {
            redacted = pattern.Replace(redacted, match =>
            {
                // For patterns with capture groups (e.g., key=value), keep key, redact value
                if (match.Groups.Count > 2)
                {
                    var prefix = match.Groups[1].Value;
                    return $"{prefix}=***REDACTED***";
                }

                // For full matches, redact entirely
                return "***REDACTED***";
            });
        }
        return redacted;
    }

    private Dictionary<string, string> RedactEnvironment(IReadOnlyDictionary<string, string> env)
    {
        var redacted = new Dictionary<string, string>();
        foreach (var (key, value) in env)
        {
            // Redact by key name
            if (Regex.IsMatch(key, @"(?i)(key|password|secret|token|credential)"))
            {
                redacted[key] = "***REDACTED***";
                continue;
            }

            // Redact by value pattern
            redacted[key] = RedactText(value);
        }
        return redacted;
    }

    private string RedactPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        // Check if path contains secrets (e.g., /home/user/.secrets/api-keys)
        if (Regex.IsMatch(path, @"(?i)(secret|credential|key)", RegexOptions.IgnoreCase))
            return "***REDACTED-PATH***";

        return path;
    }

    private string? RedactUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return url;

        // Redact embedded credentials in URLs
        if (url.Contains("@"))
        {
            var uri = new Uri(url);
            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                // Replace user:pass@ with ***REDACTED***@
                return url.Replace(uri.UserInfo + "@", "***REDACTED***@");
            }
        }

        return url;
    }

    private string? RedactHostname(string? hostname)
    {
        // Redact hostname for privacy (may reveal internal infrastructure)
        if (string.IsNullOrEmpty(hostname))
            return hostname;

        // Replace with generic hostname
        return $"host-{hostname.GetHashCode():X8}";
    }
}
```

**Prevention:**
- Redact ALL text fields, not just environment variables
- Use pattern matching to detect secrets in command lines, stdout, stderr
- Redact URLs containing embedded credentials
- Redact branch names and commit messages (may contain secrets)
- Provide dry-run preview: `acode runs export --redact --dry-run` shows what will be redacted

---

### Threat 5: JSON Deserialization Attack via Malicious Manifest

**Risk:** Malicious manifest.json exploits JSON deserializer vulnerabilities (type confusion, property injection). Attacker achieves remote code execution during import.

**Attack Scenario:**
1. Attacker crafts malicious bundle with `manifest.json` containing exploit payload
2. Payload exploits deserialization vulnerability (e.g., type confusion if polymorphic deserialization used)
3. User imports: `acode runs import malicious.acode-bundle`
4. Manifest deserialized, exploit triggers arbitrary code execution
5. Attacker gains control of user's machine

**Mitigation (C# - SafeManifestDeserializer):**

```csharp
namespace Acode.Infrastructure.Export;

public sealed class SafeManifestDeserializer
{
    private static readonly JsonSerializerOptions SafeOptions = new()
    {
        // CRITICAL: Disable type discrimination to prevent type confusion attacks
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),

        // Limit recursion depth to prevent stack overflow
        MaxDepth = 32,

        // Don't allow trailing commas or comments (strict JSON only)
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,

        // Property names must match exactly (case-sensitive)
        PropertyNameCaseInsensitive = false,

        // Unknown properties cause errors (fail-secure)
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public BundleManifest DeserializeManifest(string manifestJson)
    {
        BundleManifest? manifest;

        try
        {
            manifest = JsonSerializer.Deserialize<BundleManifest>(manifestJson, SafeOptions);
        }
        catch (JsonException ex)
        {
            throw new SecurityException($"Manifest JSON is invalid or malicious: {ex.Message}", ex);
        }

        // Post-deserialization validation
        if (manifest == null)
            throw new SecurityException("Manifest deserialized to null");

        ValidateManifest(manifest);

        return manifest;
    }

    private void ValidateManifest(BundleManifest manifest)
    {
        // Validation 1: Version is valid semver
        if (string.IsNullOrWhiteSpace(manifest.Version) || !IsValidSemver(manifest.Version))
            throw new SecurityException($"Invalid manifest version: '{manifest.Version}'");

        // Validation 2: Created timestamp is reasonable
        var now = DateTimeOffset.UtcNow;
        if (manifest.CreatedAt > now.AddHours(24))
            throw new SecurityException($"Manifest creation time is in the future: {manifest.CreatedAt}");

        if (manifest.CreatedAt < now.AddYears(-10))
            throw new SecurityException($"Manifest creation time is suspiciously old: {manifest.CreatedAt}");

        // Validation 3: Run count is reasonable
        if (manifest.RunCount < 0)
            throw new SecurityException($"Invalid run count: {manifest.RunCount}");

        if (manifest.RunCount > 100_000)
            throw new SecurityException($"Run count exceeds maximum (100,000): {manifest.RunCount}");

        // Validation 4: File count matches run count expectation
        if (manifest.Files.Count > manifest.RunCount * 100)
            throw new SecurityException($"File count ({manifest.Files.Count}) is excessive for {manifest.RunCount} runs");

        // Validation 5: All file hashes are valid SHA-256 (64 hex chars)
        foreach (var (path, hash) in manifest.Files)
        {
            if (string.IsNullOrWhiteSpace(hash.Sha256) || hash.Sha256.Length != 64 || !IsHexString(hash.Sha256))
                throw new SecurityException($"Invalid SHA-256 hash for file '{path}': '{hash.Sha256}'");

            if (hash.SizeBytes < 0)
                throw new SecurityException($"Invalid file size for '{path}': {hash.SizeBytes}");
        }

        // Validation 6: Total artifact bytes is reasonable
        if (manifest.TotalArtifactBytes < 0)
            throw new SecurityException($"Invalid total artifact bytes: {manifest.TotalArtifactBytes}");

        if (manifest.TotalArtifactBytes > 100L * 1024 * 1024 * 1024) // 100GB
            throw new SecurityException($"Total artifact bytes exceeds maximum (100GB): {manifest.TotalArtifactBytes:N0}");
    }

    private bool IsValidSemver(string version)
    {
        // Simple semver validation: X.Y.Z where X, Y, Z are non-negative integers
        var parts = version.Split('.');
        if (parts.Length != 3)
            return false;

        foreach (var part in parts)
        {
            if (!int.TryParse(part, out var num) || num < 0)
                return false;
        }

        return true;
    }

    private bool IsHexString(string value)
    {
        return value.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
    }
}
```

**Prevention:**
- Use strongly-typed deserialization (no polymorphism)
- Disable type discrimination features in JSON deserializer
- Validate ALL fields after deserialization
- Set recursion depth limits
- Reject manifests with unknown properties (fail-secure)
- Validate version strings, timestamps, counts are within reasonable bounds

---

## Best Practices

### Bundle Design

1. **Keep bundles focused and minimal** - Export only what's needed for the specific use case. For debugging: export single failing run. For compliance: export filtered date range. Don't export entire workspace history unless archiving.

2. **Include provenance by default** - Always include git commit SHA, branch, and worktree ID. This enables correlating runs with specific code versions, critical for reproducing issues. Provenance data is small (~200 bytes) but invaluable for debugging.

3. **Use semantic versioning for bundle format** - When extending bundle format (adding fields to manifest), increment version appropriately: MAJOR for breaking changes (old importers can't read), MINOR for backward-compatible additions, PATCH for fixes.

4. **Compress artifacts intelligently** - Use ZIP's default DEFLATE compression (level 6). Don't compress already-compressed formats (PNG, JPG, gzip files) - wastes CPU. Detect and store these uncompressed.

### Export Operations

5. **Default to redaction, opt-in to expose** - ALWAYS redact secrets by default. Require explicit `--no-redact` flag to export unredacted data, with confirmation prompt. Better to over-redact than leak secrets.

6. **Show redaction preview before export** - Provide `--dry-run` mode that shows what will be redacted without creating bundle. Allows user to review before committing to export. Example output: `API_KEY: sk-abc123 → ***REDACTED***`.

7. **Sign bundles for compliance use cases** - For regulatory compliance or audit trails, sign bundles with detached signature file: `bundle.acode-bundle` + `bundle.acode-bundle.sig`. Use ed25519 signatures (fast, small, secure).

8. **Atomic export with temp files** - Export to temp file first: `.acode-bundle.tmp`, then atomic rename when complete. Prevents partial bundles if export interrupted. Clean up temp files on startup.

9. **Progress indication for large exports** - Show progress bar for exports >100MB or >100 runs: `Exporting 245 runs... [=======>   ] 68% (1.2GB/1.8GB)`. Users need feedback for long operations.

10. **Validate before exporting** - Check disk space available before starting export. Estimate bundle size (sum of artifact sizes × 0.6 for compression) and verify `df` shows 2x that available. Fail early with clear message if insufficient space.

### Import Operations

11. **Verify integrity before importing** - ALWAYS check manifest hashes match actual file contents before importing artifacts. Display verification results: `Verified 145/145 files, 0 errors`. Reject bundles with ANY hash mismatches.

12. **Detect and handle import conflicts** - When importing run IDs that already exist, prompt for strategy: `merge` (keep newer), `replace` (overwrite), `skip` (ignore), `rename` (import as run-001-imported). Default to `skip` (safest).

13. **Preview import without applying** - Support `--dry-run` mode showing what would be imported: `Would import 12 runs, 245MB artifacts, 3 conflicts detected`. Let users review before committing.

14. **Quarantine suspicious bundles** - If ZIP bomb detected, path traversal found, or signature verification fails, move bundle to `.acode/quarantine/` directory instead of deleting. Allows forensic analysis. Auto-purge quarantine after 30 days.

15. **Log all import operations** - Write import audit log: `~/.acode/import-audit.log` with timestamp, bundle path, runs imported, conflicts, verification result. Critical for security audits.

### Security

16. **Never execute code from bundles** - Bundles contain data only, never executable scripts. If bundle includes `.sh` or `.ps1` files, import as data artifacts only. Never auto-execute on import.

17. **Sandbox extraction process** - Extract bundles to isolated temp directory first: `/tmp/acode-import-{uuid}/`, verify contents, then move to final location. If verification fails, delete temp directory entirely.

18. **Rate limit imports** - Prevent DOS via rapid imports by limiting to 10 imports/minute. Track in-memory or temp file. Return error: `Rate limit exceeded, retry in 6 seconds`.

19. **Validate all user-controlled paths** - Bundle filenames, extraction paths, manifest paths - validate ALL against path traversal, forbidden characters, length limits. Use whitelist validation, not blacklist.

### Performance

20. **Stream large artifacts** - When exporting/importing >100MB artifacts, use streaming I/O instead of loading into memory. Process in 4MB chunks. Keeps memory usage constant regardless of bundle size.

21. **Parallelize hash computation** - When verifying manifest hashes, process files in parallel (max 4 threads). Utilize multi-core CPUs. Verification of 1000 files drops from 45s → 12s.

22. **Index bundles for fast lookup** - After importing, build index: `~/.acode/bundle-index.json` mapping run IDs to bundle sources. Enables `acode runs find-bundle {run-id}` for provenance tracking.

23. **Compress metadata sparingly** - Manifest, provenance, outbox-summary are small (<10KB each). Store uncompressed for fast access during verification. Only compress artifacts directory.

---

## Troubleshooting

### Issue 1: Export Fails with "Disk Space Full" Despite Available Space

**Symptoms:**
- `acode runs export` fails with error: "Insufficient disk space"
- `df -h` shows 50GB available on `/home` partition
- Export destination is `/home/user/exports/bundle.acode-bundle`
- Error occurs immediately without writing any data

**Causes:**
1. Temp directory (`/tmp`) is on different partition with less space
2. Export uses `/tmp` for staging before moving to final destination
3. `/tmp` partition only has 500MB free, bundle needs 2GB
4. Disk space check only validates destination path, not temp path
5. User quota exceeded on `/tmp` filesystem
6. Reserved blocks (5% for root) reduce available space for non-root users
7. Another process filled `/tmp` during export attempt

**Solutions:**

```bash
# Solution 1: Check both destination and temp partition space
df -h /tmp
df -h /home/user/exports/
# If /tmp is full, need to either clean it or change temp location

# Clean /tmp:
sudo find /tmp -type f -atime +7 -delete
# Deletes files older than 7 days

# Or set custom temp directory:
export TMPDIR=/home/user/tmp-exports
mkdir -p $TMPDIR
acode runs export --run {run-id} --output bundle.acode-bundle

# Solution 2: Verify disk space checks both locations
acode runs export --run {run-id} --check-space
# Should report space on BOTH temp and destination

# Solution 3: Check user disk quota
quota -vs
# Shows quota limits and current usage

# If quota exceeded, request increase or clean up files:
find ~/Downloads -type f -size +100M -exec rm {} \;

# Solution 4: Check for reserved blocks (root-only space)
sudo tune2fs -l /dev/sda1 | grep -i "block count\|reserved"
# Example output:
# Block count: 26214400
# Reserved block count: 1310720 (5%)

# If non-root user, effective available space is 5% less
# Solution: Export as root (not recommended) or free more space

# Solution 5: Use different temp directory on same partition as destination
acode config set export.temp-directory "/home/user/.acode-tmp"
# Ensures temp and final location on same partition (atomic move possible)

# Solution 6: Export directly without temp staging (riskier)
acode runs export --run {run-id} --no-atomic --output bundle.acode-bundle
# Warning: Partial bundle left if interrupted

# Solution 7: Estimate bundle size before exporting
acode runs export --run {run-id} --estimate-size
# Output: "Estimated bundle size: 1.8GB (artifacts: 1.6GB, metadata: 200MB)"
# Verify sufficient space before proceeding

# Solution 8: Split large exports into multiple smaller bundles
acode runs export --from 2024-01-01 --to 2024-01-15 --output part1.acode-bundle
acode runs export --from 2024-01-16 --to 2024-01-31 --output part2.acode-bundle
# Smaller bundles easier to manage with limited space
```

---

### Issue 2: Import Fails with "Manifest Hash Mismatch" for Multiple Files

**Symptoms:**
- `acode runs import bundle.acode-bundle` fails during verification
- Error: "Hash mismatch for 15 files"
- Example: `stdout.txt: expected abc123..., actual def456...`
- Bundle was transferred from Windows to Linux via USB drive
- Bundle exports successfully on Windows, fails import on Linux

**Causes:**
1. Line ending conversion during transfer (CRLF ↔ LF)
2. Git auto-converted text files when bundle was committed to repo
3. USB drive formatted as FAT32 caused file corruption
4. Windows antivirus modified files during scan
5. ZIP extraction used wrong encoding (UTF-8 vs Windows-1252)
6. Symbolic links resolved differently on Linux vs Windows
7. File timestamps modified, triggering re-hashing with different algorithm

**Solutions:**

```bash
# Solution 1: Extract and inspect affected files
unzip -l bundle.acode-bundle
# Lists all files, check for unexpected sizes

unzip bundle.acode-bundle -d /tmp/bundle-inspect
cd /tmp/bundle-inspect

# Check line endings in text files:
file artifacts/run-001/stdout.txt
# Output: "ASCII text, with CRLF line terminators" (Windows)
# or "ASCII text" (Unix)

# Solution 2: Recompute hashes to identify mismatches
cd /tmp/bundle-inspect
sha256sum artifacts/run-001/stdout.txt
# Compare with manifest.json:
jq '.files["artifacts/run-001/stdout.txt"].sha256' manifest.json

# Solution 3: If line ending issue, normalize and re-bundle
find artifacts -name "*.txt" -exec dos2unix {} \;
# Converts all .txt files to LF

# Re-create bundle with corrected files:
zip -r bundle-fixed.acode-bundle manifest.json provenance.json artifacts/

# Import fixed bundle:
acode runs import bundle-fixed.acode-bundle

# Solution 4: Skip hash verification (DANGEROUS - use only if trusted source)
acode runs import bundle.acode-bundle --skip-verify
# Warning: Imports without integrity checks

# Solution 5: Transfer bundle using hash-preserving method
# BAD: git add bundle.acode-bundle (git may modify)
# BAD: Copy via Windows share with auto-conversion enabled

# GOOD: Use rsync with checksum verification:
rsync -avz --checksum bundle.acode-bundle user@linux-host:/path/
# Verifies transfer integrity

# Or use SCP:
scp -C bundle.acode-bundle user@linux-host:/path/
# -C compresses during transfer

# Solution 6: Verify bundle immediately after creation
acode runs verify bundle.acode-bundle
# Run on source machine before transfer

# If verification fails on source, bundle corrupted during export:
acode runs export --run {run-id} --output bundle-retry.acode-bundle --verify

# Solution 7: Check for antivirus interference
# Windows Defender may scan and modify files
# Temporarily disable real-time protection during export:
# Settings > Update & Security > Windows Security > Virus & threat protection > Manage settings > Real-time protection (Off)

# Better: Add .acode directory to exclusions permanently
Add-MpPreference -ExclusionPath "C:\Users\{user}\.acode"

# Solution 8: Use bundle repair tool
acode runs repair-bundle bundle.acode-bundle --output bundle-repaired.acode-bundle
# Recalculates all hashes, updates manifest, re-creates ZIP
# Warning: Can't detect malicious modifications, only fixes mismatches
```

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