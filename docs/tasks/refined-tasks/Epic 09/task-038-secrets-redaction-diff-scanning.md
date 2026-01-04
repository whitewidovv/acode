# Task 038: Secrets Redaction + Diff Scanning

**Priority:** P0 – Critical  
**Tier:** X – Cross-Cutting  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 9 – Safety & Compliance  
**Dependencies:** Task 037, Task 050  

---

## Description

Task 038 implements the secrets redaction and diff scanning engine. This system detects and redacts secrets from all outputs before they can be persisted, exported, or sent to external systems. It's a critical security layer that prevents credential leakage.

The redaction engine scans multiple content types: tool outputs, file diffs, terminal output, log messages, and database snapshots. Detection uses configurable patterns (regex, entropy analysis, known formats) to identify API keys, passwords, tokens, and other sensitive data.

Redaction is mandatory and non-bypassable. When secrets are detected, they are replaced with redaction markers (`[REDACTED:API_KEY]`) that indicate what was removed without exposing the actual value. All redaction events are logged for audit.

### DB-Aware Redaction

**Update (DB-aware redaction):** Redaction/secret scanning MUST apply to DB-derived snapshots included in exports, and MUST occur before remote sync writes.

**Update (Workspace DB Foundation):** This task MUST use the Workspace DB abstraction introduced in **Task 050**.
- SQLite is the REQUIRED local workspace cache (fast, offline, crash-safe)
- When configured, Postgres is the canonical source-of-truth; local changes MUST sync via outbox/idempotency (Task 049.f / Task 050)
- No direct dependency on a concrete DB engine outside Infrastructure; Application MUST depend on storage interfaces only

### Business Value

Secrets redaction provides:
- Prevention of credential leakage
- Compliance with security requirements
- Safe export and sharing
- Audit trail of all redactions
- Protection against accidental exposure

### Scope Boundaries

This task covers the core redaction engine. Tool output redaction is 038.a. Commit/push blocking is 038.b. Pattern configuration is 038.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Tool Output | Task 020 | Pre-model | Before LLM sees |
| Git Operations | Task 016-018 | Pre-commit | Block on detection |
| Audit Export | Task 039 | Pre-export | Verify clean |
| Workspace DB | Task 050 | All queries | Scan results |
| Remote Sync | Task 049.f | Pre-sync | Block secrets |
| Policy Engine | Task 037 | Config | Pattern rules |
| Logging | Task 007 | All logs | Auto-redact |
| CLI Output | Task 000 | All output | Display safe |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Pattern miss (false negative) | Corpus test | Update pattern | Potential leak |
| Over-redaction (false positive) | User report | Tune pattern | Content lost |
| Performance degradation | Timer | Log, continue | Slower output |
| DB scan timeout | Watchdog | Partial scan | Warning |
| Sync blocked | Detection | Retry after fix | Cannot sync |
| Memory pressure | Monitor | Stream mode | Slower |
| Pattern regex error | Compile check | Skip pattern | Warning logged |
| Circular redaction | Depth check | Limit depth | Error |

### Assumptions

1. **All outputs scanned**: No exception
2. **Patterns are configurable**: Via 038.c
3. **Redaction is non-bypassable**: No override
4. **Performance acceptable**: <100ms for typical content
5. **Streaming supported**: For large content
6. **Database integration**: Via Task 050 interfaces
7. **Audit required**: All redactions logged
8. **Marker format consistent**: `[REDACTED:TYPE]`

### Security Considerations

1. **Non-bypassable**: Cannot disable redaction
2. **Defense in depth**: Multiple detection methods
3. **No raw secrets in logs**: Redact before log
4. **No raw secrets in DB**: Scan on write
5. **No raw secrets in sync**: Block remote sync
6. **Audit all redactions**: Full trail
7. **Secure pattern storage**: Patterns in config
8. **Memory safety**: Clear sensitive data after scan
9. **Test corpus**: Validate pattern effectiveness
10. **Regular updates**: Pattern corpus maintenance

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Redaction | Replacing secret with marker |
| Redaction Marker | `[REDACTED:TYPE]` replacement |
| Secret Pattern | Regex or heuristic for detection |
| Entropy Analysis | High-entropy string detection |
| Known Format | API key format (AWS, GitHub, etc.) |
| False Negative | Missed secret (dangerous) |
| False Positive | Non-secret redacted (annoying) |
| Corpus Test | Test suite for pattern validation |
| Pre-model Scan | Scan before LLM sees content |
| Pre-sync Scan | Scan before remote write |

---

## Out of Scope

- Secret rotation
- Secret management (vault integration)
- Secret injection
- Real-time secret monitoring
- Third-party secret scanning services
- Encrypted secret storage

---

## Functional Requirements

### FR-001 to FR-020: Core Redaction

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-038-01 | All outputs MUST be scanned | P0 |
| FR-038-02 | Secrets MUST be replaced with markers | P0 |
| FR-038-03 | Marker format MUST be `[REDACTED:TYPE]` | P0 |
| FR-038-04 | Original secret MUST NOT appear in output | P0 |
| FR-038-05 | Redaction MUST be non-bypassable | P0 |
| FR-038-06 | Redaction MUST be atomic | P0 |
| FR-038-07 | Multiple secrets MUST all be redacted | P0 |
| FR-038-08 | Nested secrets MUST be handled | P0 |
| FR-038-09 | Redaction MUST preserve structure | P1 |
| FR-038-10 | JSON structure MUST be valid after redaction | P0 |
| FR-038-11 | YAML structure MUST be valid after redaction | P0 |
| FR-038-12 | XML structure MUST be valid after redaction | P1 |
| FR-038-13 | Streaming mode MUST be supported | P1 |
| FR-038-14 | Partial scan MUST work for large content | P1 |
| FR-038-15 | Memory limit MUST be enforced | P1 |
| FR-038-16 | Timeout MUST be configurable | P1 |
| FR-038-17 | Timeout MUST log warning | P0 |
| FR-038-18 | Context around secret MUST be preserved | P1 |
| FR-038-19 | Line numbers MUST be tracked | P1 |
| FR-038-20 | File path MUST be tracked if applicable | P0 |

### FR-021 to FR-040: Detection Patterns

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-038-21 | Regex patterns MUST be supported | P0 |
| FR-038-22 | Entropy analysis MUST be supported | P1 |
| FR-038-23 | Known formats MUST be detected | P0 |
| FR-038-24 | AWS keys MUST be detected | P0 |
| FR-038-25 | GitHub tokens MUST be detected | P0 |
| FR-038-26 | Azure secrets MUST be detected | P0 |
| FR-038-27 | Generic passwords MUST be detected | P0 |
| FR-038-28 | Private keys MUST be detected | P0 |
| FR-038-29 | JWT tokens MUST be detected | P0 |
| FR-038-30 | Connection strings MUST be detected | P0 |
| FR-038-31 | Custom patterns MUST be configurable | P0 |
| FR-038-32 | Pattern priority MUST be configurable | P1 |
| FR-038-33 | Pattern categories MUST be defined | P0 |
| FR-038-34 | Pattern enabled/disabled MUST work | P1 |
| FR-038-35 | Pattern corpus MUST validate patterns | P0 |
| FR-038-36 | Pattern updates MUST hot reload | P2 |
| FR-038-37 | Pattern errors MUST skip pattern | P0 |
| FR-038-38 | Pattern performance MUST be tested | P1 |
| FR-038-39 | Pattern false positive rate MUST be tracked | P2 |
| FR-038-40 | Pattern false negative MUST be critical | P0 |

### FR-041 to FR-060: Content Types

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-038-41 | Tool output MUST be scanned | P0 |
| FR-038-42 | File diffs MUST be scanned | P0 |
| FR-038-43 | Terminal output MUST be scanned | P0 |
| FR-038-44 | Log messages MUST be scanned | P0 |
| FR-038-45 | Database snapshots MUST be scanned | P0 |
| FR-038-46 | Export bundles MUST be scanned | P0 |
| FR-038-47 | Remote sync data MUST be scanned | P0 |
| FR-038-48 | Clipboard content MUST be scanned | P2 |
| FR-038-49 | Error messages MUST be scanned | P0 |
| FR-038-50 | Stack traces MUST be scanned | P0 |
| FR-038-51 | Config files MUST be scanned | P0 |
| FR-038-52 | Environment displays MUST be scanned | P0 |
| FR-038-53 | JSON responses MUST be scanned | P0 |
| FR-038-54 | YAML content MUST be scanned | P0 |
| FR-038-55 | XML content MUST be scanned | P1 |
| FR-038-56 | Base64 encoded MUST be decoded and scanned | P1 |
| FR-038-57 | URL encoded MUST be decoded and scanned | P1 |
| FR-038-58 | Multiline secrets MUST be detected | P1 |
| FR-038-59 | Partial secrets MUST be detected | P2 |
| FR-038-60 | Binary files MUST be skipped | P0 |

### FR-061 to FR-075: Audit Integration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-038-61 | All redactions MUST be logged | P0 |
| FR-038-62 | Log MUST include content type | P0 |
| FR-038-63 | Log MUST include secret type | P0 |
| FR-038-64 | Log MUST include location | P0 |
| FR-038-65 | Log MUST NOT include secret value | P0 |
| FR-038-66 | Log MUST include timestamp | P0 |
| FR-038-67 | Log MUST include scan duration | P1 |
| FR-038-68 | Log MUST include pattern matched | P1 |
| FR-038-69 | Structured log format | P0 |
| FR-038-70 | Metrics: redactions per type | P2 |
| FR-038-71 | Metrics: scan time | P2 |
| FR-038-72 | Events: secret detected | P1 |
| FR-038-73 | Events: redaction applied | P1 |
| FR-038-74 | Audit export MUST include redaction log | P1 |
| FR-038-75 | Retention MUST match audit policy | P1 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-038-01 | Small content scan | <50ms | P1 |
| NFR-038-02 | Large content scan | <500ms | P1 |
| NFR-038-03 | Pattern match time | <10ms each | P1 |
| NFR-038-04 | Streaming throughput | 10MB/s | P2 |
| NFR-038-05 | Memory per scan | <50MB | P1 |
| NFR-038-06 | Concurrent scans | 10+ | P2 |
| NFR-038-07 | DB snapshot scan | <5s | P1 |
| NFR-038-08 | Pattern compilation | <100ms | P2 |
| NFR-038-09 | Hot reload | <50ms | P2 |
| NFR-038-10 | Entropy analysis | <20ms | P1 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-038-11 | Non-bypassable | 100% | P0 |
| NFR-038-12 | Known patterns detected | >99.9% | P0 |
| NFR-038-13 | False positive rate | <5% | P1 |
| NFR-038-14 | Graceful timeout | Always | P1 |
| NFR-038-15 | Memory bounded | Always | P1 |
| NFR-038-16 | Thread safety | No races | P0 |
| NFR-038-17 | Cross-platform | All OS | P0 |
| NFR-038-18 | Encoding support | UTF-8, ASCII | P0 |
| NFR-038-19 | Structure preservation | Always | P0 |
| NFR-038-20 | Deterministic | Same input = same output | P0 |

### Security Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-038-21 | No secret in memory after scan | Cleared | P0 |
| NFR-038-22 | No secret in logs | Never | P0 |
| NFR-038-23 | No secret in DB unscanned | Never | P0 |
| NFR-038-24 | No secret in sync | Never | P0 |
| NFR-038-25 | Audit completeness | 100% | P0 |
| NFR-038-26 | Pattern storage secure | In config | P0 |
| NFR-038-27 | No disable option | Never | P0 |
| NFR-038-28 | Defense in depth | Multiple layers | P0 |
| NFR-038-29 | Corpus test coverage | >95% | P0 |
| NFR-038-30 | Regular pattern updates | Documented | P1 |

---

## Acceptance Criteria / Definition of Done

### Core Redaction
- [ ] AC-001: All outputs scanned
- [ ] AC-002: Secrets replaced with markers
- [ ] AC-003: Marker format correct
- [ ] AC-004: Original never appears
- [ ] AC-005: Non-bypassable
- [ ] AC-006: Atomic operation
- [ ] AC-007: Multiple secrets handled
- [ ] AC-008: Nested secrets handled
- [ ] AC-009: Structure preserved
- [ ] AC-010: Streaming works

### Detection Patterns
- [ ] AC-011: Regex patterns work
- [ ] AC-012: Entropy analysis works
- [ ] AC-013: Known formats detected
- [ ] AC-014: AWS keys detected
- [ ] AC-015: GitHub tokens detected
- [ ] AC-016: Azure secrets detected
- [ ] AC-017: Passwords detected
- [ ] AC-018: Private keys detected
- [ ] AC-019: JWT tokens detected
- [ ] AC-020: Connection strings detected
- [ ] AC-021: Custom patterns work
- [ ] AC-022: Patterns configurable

### Content Types
- [ ] AC-023: Tool output scanned
- [ ] AC-024: File diffs scanned
- [ ] AC-025: Terminal output scanned
- [ ] AC-026: Logs scanned
- [ ] AC-027: DB snapshots scanned
- [ ] AC-028: Exports scanned
- [ ] AC-029: Sync data scanned
- [ ] AC-030: Errors scanned

### Audit
- [ ] AC-031: Redactions logged
- [ ] AC-032: Content type included
- [ ] AC-033: Secret type included
- [ ] AC-034: Location included
- [ ] AC-035: Value never logged
- [ ] AC-036: Timestamp included
- [ ] AC-037: Structured format
- [ ] AC-038: Events published

### Security
- [ ] AC-039: Memory cleared after
- [ ] AC-040: No secrets in logs
- [ ] AC-041: No secrets in DB unscanned
- [ ] AC-042: No secrets in sync
- [ ] AC-043: Defense in depth
- [ ] AC-044: Corpus tested
- [ ] AC-045: No disable option

---

## User Verification Scenarios

### Scenario 1: Tool Output Redaction
**Persona:** Developer using tool  
**Preconditions:** Tool returns AWS key  
**Steps:**
1. Execute tool that outputs key
2. Output contains secret
3. Redaction applied
4. Model sees `[REDACTED:AWS_KEY]`

**Verification Checklist:**
- [ ] Tool executed
- [ ] Secret detected
- [ ] Marker replaced
- [ ] Logged to audit

### Scenario 2: DB Snapshot Scan
**Persona:** Admin exporting data  
**Preconditions:** DB contains connection strings  
**Steps:**
1. Create snapshot export
2. Scan applied before write
3. Secrets redacted
4. Export clean

**Verification Checklist:**
- [ ] Snapshot created
- [ ] Scan executed
- [ ] Secrets found
- [ ] Export safe

### Scenario 3: Remote Sync Block
**Persona:** Developer syncing  
**Preconditions:** Local data has secrets  
**Steps:**
1. Attempt remote sync
2. Pre-sync scan runs
3. Secrets detected
4. Sync blocked until redacted

**Verification Checklist:**
- [ ] Sync attempted
- [ ] Scan runs
- [ ] Secrets found
- [ ] Block enforced

### Scenario 4: Custom Pattern
**Persona:** Security admin  
**Preconditions:** Internal format exists  
**Steps:**
1. Add custom pattern
2. Reload patterns
3. Test detection
4. Works on internal format

**Verification Checklist:**
- [ ] Pattern added
- [ ] Hot reload works
- [ ] Detection works
- [ ] Logged correctly

### Scenario 5: Entropy Detection
**Persona:** Developer with random string  
**Preconditions:** High-entropy token  
**Steps:**
1. Output contains random token
2. Entropy analysis runs
3. High entropy flagged
4. Token redacted

**Verification Checklist:**
- [ ] String analyzed
- [ ] High entropy detected
- [ ] Redaction applied
- [ ] Type marked

### Scenario 6: Structure Preservation
**Persona:** Developer with JSON  
**Preconditions:** JSON contains secret  
**Steps:**
1. JSON with embedded key
2. Scan and redact
3. JSON still valid
4. Only secret replaced

**Verification Checklist:**
- [ ] JSON parsed
- [ ] Secret found
- [ ] Structure valid
- [ ] Other content intact

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-038-01 | Basic redaction | FR-038-02 |
| UT-038-02 | Marker format | FR-038-03 |
| UT-038-03 | Multiple secrets | FR-038-07 |
| UT-038-04 | Nested secrets | FR-038-08 |
| UT-038-05 | JSON preservation | FR-038-10 |
| UT-038-06 | YAML preservation | FR-038-11 |
| UT-038-07 | AWS key pattern | FR-038-24 |
| UT-038-08 | GitHub token | FR-038-25 |
| UT-038-09 | Password pattern | FR-038-27 |
| UT-038-10 | Entropy analysis | FR-038-22 |
| UT-038-11 | Custom pattern | FR-038-31 |
| UT-038-12 | Streaming mode | FR-038-13 |
| UT-038-13 | Memory limit | FR-038-15 |
| UT-038-14 | Timeout handling | FR-038-17 |
| UT-038-15 | Audit logging | FR-038-61 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-038-01 | Full scan flow | E2E |
| IT-038-02 | Tool output path | FR-038-41 |
| IT-038-03 | Diff scan path | FR-038-42 |
| IT-038-04 | DB snapshot path | FR-038-45 |
| IT-038-05 | Export path | FR-038-46 |
| IT-038-06 | Sync block path | FR-038-47 |
| IT-038-07 | Non-bypassable | FR-038-05 |
| IT-038-08 | Pattern hot reload | FR-038-36 |
| IT-038-09 | Corpus test suite | FR-038-35 |
| IT-038-10 | Audit export | FR-038-74 |
| IT-038-11 | Performance benchmark | NFR-038-01 |
| IT-038-12 | Memory bounded | NFR-038-05 |
| IT-038-13 | Cross-platform | NFR-038-17 |
| IT-038-14 | Defense in depth | NFR-038-28 |
| IT-038-15 | Workspace DB integration | Task 050 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Secrets/
│       ├── SecretPattern.cs
│       ├── RedactionMarker.cs
│       ├── RedactionResult.cs
│       └── SecretType.cs
├── Acode.Application/
│   └── Secrets/
│       ├── ISecretScanner.cs
│       ├── IRedactionEngine.cs
│       ├── IPatternProvider.cs
│       └── IEntropyAnalyzer.cs
├── Acode.Infrastructure/
│   └── Secrets/
│       ├── SecretScanner.cs
│       ├── RedactionEngine.cs
│       ├── PatternProvider.cs
│       ├── EntropyAnalyzer.cs
│       └── Patterns/
│           ├── BuiltInPatterns.cs
│           ├── AwsPatterns.cs
│           ├── GitHubPatterns.cs
│           └── AzurePatterns.cs
```

### Key Interfaces

```csharp
public interface ISecretScanner
{
    Task<ScanResult> ScanAsync(string content, ContentType type);
    Task<ScanResult> ScanStreamAsync(Stream content, ContentType type);
}

public interface IRedactionEngine
{
    Task<RedactionResult> RedactAsync(ScanResult scanResult);
    string FormatMarker(SecretType type);
}

public interface IPatternProvider
{
    IEnumerable<SecretPattern> GetPatterns();
    void ReloadPatterns();
}
```

### Marker Format

```
[REDACTED:API_KEY]
[REDACTED:AWS_ACCESS_KEY]
[REDACTED:GITHUB_TOKEN]
[REDACTED:PASSWORD]
[REDACTED:PRIVATE_KEY]
[REDACTED:JWT_TOKEN]
[REDACTED:CONNECTION_STRING]
[REDACTED:HIGH_ENTROPY]
[REDACTED:CUSTOM:pattern_name]
```

**End of Task 038 Specification**
