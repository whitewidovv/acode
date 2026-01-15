# Task-049e Semantic Gap Analysis: Retention, Export, Privacy + Redaction

**Status:** ❌ 0% COMPLETE - SEMANTIC COMPLETENESS: 0/115 ACs (0%)

**Date:** 2026-01-15
**Analyzed By:** Claude Code
**Methodology:** Semantic completeness verification per CLAUDE.md Section 3.2

---

## EXECUTIVE SUMMARY

Task-049e (Retention, Export, Privacy + Redaction) is **0% semantically complete**. All 115 Acceptance Criteria are missing:

- **Total ACs:** 115
- **ACs Present:** 0
- **ACs Missing:** 115
- **Semantic Completeness:** (0 / 115) × 100 = **0%**
- **Implementation Gaps:** 4 major feature domains
- **Estimated Effort:** 36-40 hours

**Feature Breakdown:**
- **Retention:** 20 ACs (policy engine, enforcement, notifications) - 10 hours
- **Export:** 25 ACs (JSON/Markdown/Text formats, filtering, compression) - 12 hours
- **Privacy:** 15 ACs (4-level privacy model, per-chat configuration) - 8 hours
- **Redaction:** 55 ACs (builtin patterns, custom patterns, behavior) - 10 hours

**Blocking Dependencies:** NONE - can implement independently

---

## SECTION 1: ACCEPTANCE CRITERIA BY FEATURE DOMAIN

### RETENTION (AC-001 through AC-020) - 20 ACs - 0/20 COMPLETE (0%)

**Policy Configuration (AC-001-007):**
- ❌ AC-001: Default retention = 365 days for archived chats
- ❌ AC-002: Configurable via CLI + config file
- ❌ AC-003: Minimum 7 days enforced
- ❌ AC-004: Maximum = "never" (unlimited)
- ❌ AC-005: Active chats exempt by default
- ❌ AC-006: Per-chat retention override
- ❌ AC-007: Changes take effect immediately

**Enforcement (AC-008-015):**
- ❌ AC-008: Background job (default: daily 3 AM)
- ❌ AC-009: Expiry detection (archived_at comparison)
- ❌ AC-010: 7-day grace period before deletion
- ❌ AC-011: Soft-delete with deleted_at timestamp
- ❌ AC-012: Hard-delete after grace period
- ❌ AC-013: Cascade deletion (chats → runs → messages → index)
- ❌ AC-014: Batch processing (100 chats/cycle)
- ❌ AC-015: Manual trigger: `acode retention enforce --now`

**Warnings (AC-016-020):**
- ❌ AC-016: Expiry warning in list (< 7 days)
- ❌ AC-017: Warning includes date + message count
- ❌ AC-018: Suppression flag: `--no-expiry-warning`
- ❌ AC-019: Status command: `acode retention status`
- ❌ AC-020: Email/webhook notifications

**Subtotal Retention: 0/20 ACs**

---

### EXPORT (AC-021 through AC-045) - 25 ACs - 0/25 COMPLETE (0%)

**Formats (AC-021-027):**
- ❌ AC-021: JSON export with valid schema
- ❌ AC-022: JSON includes all fields (id, role, content, timestamps, tool_calls)
- ❌ AC-023: Markdown readable format
- ❌ AC-024: Markdown with syntax highlighting markers
- ❌ AC-025: Plain text minimal formatting
- ❌ AC-026: Format option: `--format json|markdown|text`
- ❌ AC-027: Metadata header (timestamp, version)

**Content Selection (AC-028-034):**
- ❌ AC-028: Single chat: `acode export <chat-id>`
- ❌ AC-029: All chats: `acode export --all`
- ❌ AC-030: Date range: `--since` and `--until` (ISO 8601)
- ❌ AC-031: Relative dates: `--since 7d`
- ❌ AC-032: Tag filter: `--tag <tagname>`
- ❌ AC-033: Multiple filters (AND logic)
- ❌ AC-034: Preview: `acode export --preview`

**Output Options (AC-035-040):**
- ❌ AC-035: File output: `--output /path/to/file`
- ❌ AC-036: Stdout output (piped when no output file)
- ❌ AC-037: Progress display (>5 seconds)
- ❌ AC-038: Compression: `--compress` (creates .gz)
- ❌ AC-039: Encryption: `--encrypt`
- ❌ AC-040: Overwrite protection (prompt)

**Redaction Integration (AC-041-045):**
- ❌ AC-041: `--redact` flag applies patterns
- ❌ AC-042: Redaction statistics (count, patterns)
- ❌ AC-043: Redaction preview (no modification)
- ❌ AC-044: Unredacted warning
- ❌ AC-045: In-memory application only

**Subtotal Export: 0/25 ACs**

---

### PRIVACY (AC-046 through AC-060) - 15 ACs - 0/15 COMPLETE (0%)

**Privacy Levels (AC-046-050):**
- ❌ AC-046: LOCAL_ONLY prevents remote sync
- ❌ AC-047: REDACTED syncs with secrets removed
- ❌ AC-048: METADATA_ONLY syncs titles/tags/timestamps only
- ❌ AC-049: FULL syncs all (with warning)
- ❌ AC-050: Default = LOCAL_ONLY

**Per-Chat Configuration (AC-051-055):**
- ❌ AC-051: Set per-chat: `acode chat privacy <id> <level>`
- ❌ AC-052: Visible in: `acode chat show <id>`
- ❌ AC-053: Filter by level: `acode chat list --privacy local_only`
- ❌ AC-054: Bulk update: `acode chat privacy --all <level>`
- ❌ AC-055: Inheritance from project default

**Level Transitions (AC-056-060):**
- ❌ AC-056: LOCAL_ONLY → others blocked
- ❌ AC-057: REDACTED → FULL requires confirmation
- ❌ AC-058: Any → LOCAL_ONLY always allowed
- ❌ AC-059: Change logged to audit
- ❌ AC-060: Downgrade warning

**Subtotal Privacy: 0/15 ACs**

---

### REDACTION (AC-061 through AC-115+) - 55+ ACs - 0/55 COMPLETE (0%)

**Built-in Patterns (AC-061-067):**
- ❌ AC-061: Stripe API keys: `sk_live_[a-zA-Z0-9]{24,}`
- ❌ AC-062: GitHub tokens: `gh[ps]_[a-zA-Z0-9]{36,}`
- ❌ AC-063: AWS keys: `AKIA[A-Z0-9]{16}`
- ❌ AC-064: JWT tokens: `eyJ[a-zA-Z0-9_-]+...`
- ❌ AC-065: Passwords: `(password|passwd|pwd)[=:\s]+\S{8,}`
- ❌ AC-066: Private keys: `-----BEGIN.*PRIVATE KEY-----`
- ❌ AC-067: All enabled by default

**Custom Patterns (AC-068-074):**
- ❌ AC-068: Configurable in config file
- ❌ AC-069: Requires: name, regex, replacement
- ❌ AC-070: Validation before saving
- ❌ AC-071: Max 50 patterns
- ❌ AC-072: Test pattern: `acode redaction test --pattern <regex> --text <sample>`
- ❌ AC-073: List patterns: `acode redaction patterns list`
- ❌ AC-074: Remove pattern: `acode redaction patterns remove <name>`

**Behavior (AC-075-080+):**
- ❌ AC-075: Replacement: `[REDACTED-<PATTERN>-<prefix>]`
- ❌ AC-076: Preserves first 10 chars for debugging
- ❌ AC-077: Multiple matches all redacted
- ❌ AC-078: Deterministic (same input = same output)
- ❌ AC-079: Recursive to nested content
- ❌ AC-080: Logging shows match count + pattern names

**Subtotal Redaction: 0/55+ ACs**

---

## SECTION 2: PRODUCTION FILES NEEDED (Estimated 15-18 files)

**Application Layer (Domain Models & Interfaces):**
- [ ] `src/Acode.Application/Retention/IRetentionPolicy.cs` - Policy interface
- [ ] `src/Acode.Application/Retention/RetentionPolicyEngine.cs` - Policy enforcement
- [ ] `src/Acode.Application/Export/IExportService.cs` - Export abstraction
- [ ] `src/Acode.Application/Export/ExportFormatter.cs` - Format handling
- [ ] `src/Acode.Application/Privacy/IPrivacyService.cs` - Privacy controls
- [ ] `src/Acode.Application/Redaction/IRedactionEngine.cs` - Pattern matching + replacement

**Infrastructure Layer:**
- [ ] `src/Acode.Infrastructure/Retention/RetentionBackgroundWorker.cs` - Scheduled enforcement
- [ ] `src/Acode.Infrastructure/Export/ExporterFactory.cs` - Format factories
- [ ] `src/Acode.Infrastructure/Export/JsonExporter.cs` - JSON format
- [ ] `src/Acode.Infrastructure/Export/MarkdownExporter.cs` - Markdown format
- [ ] `src/Acode.Infrastructure/Export/TextExporter.cs` - Plain text format
- [ ] `src/Acode.Infrastructure/Redaction/PatternLibrary.cs` - Built-in patterns
- [ ] `src/Acode.Infrastructure/Redaction/RedactionEngine.cs` - Matching + replacement

**CLI Layer:**
- [ ] `src/Acode.Cli/Commands/RetentionCommand.cs` - `acode retention` commands
- [ ] `src/Acode.Cli/Commands/ExportCommand.cs` - `acode export` commands
- [ ] `src/Acode.Cli/Commands/PrivacyCommand.cs` - `acode chat privacy` commands
- [ ] `src/Acode.Cli/Commands/RedactionCommand.cs` - `acode redaction` commands

**Total: ~18 production files**

---

## SECTION 3: TEST FILES NEEDED (~25+ tests)

**Unit Tests:**
- RetentionPolicyTests (5 tests)
- ExportFormatterTests (8 tests)
- PrivacyLevelTests (4 tests)
- RedactionEngineTests (10 tests)

**Integration Tests:**
- ExportIntegrationTests (6 tests)
- RetentionEnforcementTests (4 tests)
- PrivacyIntegrationTests (3 tests)
- RedactionIntegrationTests (5 tests)

**E2E Tests:**
- ExportE2ETests (3 tests)
- RetentionE2ETests (2 tests)

**Total: ~50+ test methods**

---

## SECTION 4: EFFORT BREAKDOWN

| Feature | ACs | Files | Tests | Hours |
|---------|-----|-------|-------|-------|
| Retention | 20 | 3 | 8 | 10 |
| Export | 25 | 6 | 18 | 12 |
| Privacy | 15 | 2 | 6 | 8 |
| Redaction | 55 | 6 | 18 | 10 |
| **TOTAL** | **115** | **18** | **50+** | **40** |

---

## SEMANTIC COMPLETENESS

```
Task-049e Semantic Completeness = (ACs fully implemented / Total ACs) × 100

ACs Fully Implemented: 0
  - Retention: 0/20
  - Export: 0/25
  - Privacy: 0/15
  - Redaction: 0/55

Total ACs: 115

Semantic Completeness: (0 / 115) × 100 = 0%
```

---

**Status:** ❌ NOT STARTED - Full implementation needed across 4 feature domains

**Blocking Dependencies:** NONE - ready to implement immediately

**Recommendation:** Create completion checklist in 4 phases (Retention → Privacy → Export → Redaction) to manage scope.

---

