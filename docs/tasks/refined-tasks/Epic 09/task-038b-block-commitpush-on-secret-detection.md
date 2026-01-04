# Task 038.b: Block Commit/Push on Secret Detection

**Priority:** P0 – Critical  
**Tier:** L – Feature Layer  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 9 – Safety & Compliance  
**Dependencies:** Task 038, Task 016-018, Task 050  

---

## Description

Task 038.b implements commit and push blocking when secrets are detected in staged changes. This is a critical safety gate that prevents credentials from entering version control history.

When the agent attempts to commit or push changes, all staged content is scanned. If any secret is detected, the operation is blocked with a clear error message identifying the problematic content. The user must resolve the issue before proceeding.

This is a non-bypassable safety mechanism. Unlike pre-commit hooks (which can be skipped), this is enforced at the agent level before the git command is even executed.

### DB-Aware Integration

**Update (DB-aware redaction):** Redaction/secret scanning MUST apply to DB-derived snapshots included in exports, and MUST occur before remote sync writes.

**Update (Workspace DB Foundation):** This task MUST use the Workspace DB abstraction introduced in **Task 050**.

### Business Value

Commit/push blocking provides:
- Prevents secrets in git history
- Catches mistakes before they're permanent
- Clear guidance on resolution
- Non-bypassable enforcement
- Audit trail of blocked attempts

### Scope Boundaries

This task covers git operation blocking. Core engine is 038. Tool output redaction is 038.a. Pattern config is 038.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Git Operations | Task 016-018 | Pre-operation | Block before |
| Redaction Engine | Task 038 | Scan staged | Detection |
| File Tool | Task 021 | Staged content | Get files |
| Workspace DB | Task 050 | Operation log | Audit |
| Policy Engine | Task 037 | Config | Sensitivity |
| CLI | Task 000 | Error display | User feedback |
| Audit | Task 039 | Block events | Trail |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Scan timeout | Watchdog | Block commit | Must wait |
| Memory overflow | Monitor | Stream scan | Slower |
| Pattern miss | Corpus test | Update pattern | Potential leak |
| Git state error | Git check | Retry | Error message |
| File access error | IOException | Block | Must fix |
| Encoding issue | Decode error | Block | Must check |
| Conflict state | Merge check | Resolve first | Cannot commit |
| Large diff | Size check | Stream scan | Slower |

### Assumptions

1. **Agent controls git ops**: All commit/push via agent
2. **Staged content accessible**: Can read staged files
3. **Performance acceptable**: <5s for typical commit
4. **Error messages clear**: User knows what to fix
5. **Non-bypassable**: No --force or skip option
6. **Audit all blocks**: Full trail
7. **Recovery guidance**: Tell user how to fix
8. **Workspace DB tracking**: Log all attempts

### Security Considerations

1. **Non-bypassable**: Cannot skip scan
2. **Fail closed**: Error = block
3. **No partial commit**: All or nothing
4. **Clear error**: Identify secret location
5. **No secret in error**: Location, not value
6. **Audit all blocks**: Trail of attempts
7. **Resolution guidance**: Safe removal steps
8. **History check**: Consider amend case
9. **Push block**: Even if local commit slipped
10. **Remote sync block**: For workspace DB

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Pre-Commit Scan | Check before git commit |
| Pre-Push Scan | Check before git push |
| Staged Content | Files in git staging area |
| Block | Prevent operation |
| Non-Bypassable | Cannot skip or override |
| Secret Location | File + line (not value) |
| Resolution Guidance | How to fix |
| History Check | Previous commits |

---

## Out of Scope

- Third-party pre-commit hooks
- Scanning entire git history
- Automatic secret removal
- Git hook installation
- Branch protection rules
- GitHub/GitLab integrations

---

## Functional Requirements

### FR-001 to FR-015: Pre-Commit Blocking

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-038B-01 | All commits MUST be scanned | P0 |
| FR-038B-02 | Staged files MUST be scanned | P0 |
| FR-038B-03 | Secret detected MUST block commit | P0 |
| FR-038B-04 | Block MUST be non-bypassable | P0 |
| FR-038B-05 | Block reason MUST be shown | P0 |
| FR-038B-06 | Secret location MUST be shown | P0 |
| FR-038B-07 | Secret value MUST NOT be shown | P0 |
| FR-038B-08 | File path MUST be shown | P0 |
| FR-038B-09 | Line number MUST be shown | P1 |
| FR-038B-10 | Secret type MUST be shown | P0 |
| FR-038B-11 | Resolution guidance MUST be shown | P0 |
| FR-038B-12 | Multiple secrets MUST all be listed | P0 |
| FR-038B-13 | Exit code MUST be non-zero | P0 |
| FR-038B-14 | Exit code MUST be documented | P0 |
| FR-038B-15 | JSON error format MUST be available | P1 |

### FR-016 to FR-030: Pre-Push Blocking

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-038B-16 | All pushes MUST be scanned | P0 |
| FR-038B-17 | Commits being pushed MUST be scanned | P0 |
| FR-038B-18 | Secret detected MUST block push | P0 |
| FR-038B-19 | Block MUST be non-bypassable | P0 |
| FR-038B-20 | Commit ID with secret MUST be shown | P0 |
| FR-038B-21 | Resolution guidance MUST be shown | P0 |
| FR-038B-22 | Amend option MUST be suggested | P1 |
| FR-038B-23 | Rebase option MUST be suggested | P1 |
| FR-038B-24 | Force push danger MUST be warned | P0 |
| FR-038B-25 | Remote name MUST be shown | P1 |
| FR-038B-26 | Branch name MUST be shown | P1 |
| FR-038B-27 | Exit code MUST be non-zero | P0 |
| FR-038B-28 | Multiple commits MUST all be scanned | P0 |
| FR-038B-29 | New commits only MUST be scanned | P1 |
| FR-038B-30 | Existing remote MUST be skipped | P1 |

### FR-031 to FR-045: Resolution Guidance

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-038B-31 | Steps to fix MUST be shown | P0 |
| FR-038B-32 | Unstage command MUST be shown | P1 |
| FR-038B-33 | Remove secret suggestion MUST be shown | P0 |
| FR-038B-34 | Environment variable alternative MUST be suggested | P1 |
| FR-038B-35 | Gitignore suggestion MUST be shown | P1 |
| FR-038B-36 | .env.example pattern MUST be suggested | P1 |
| FR-038B-37 | Secret rotation warning MUST be shown | P0 |
| FR-038B-38 | If already committed, amend MUST be suggested | P0 |
| FR-038B-39 | History rewrite warning MUST be shown | P0 |
| FR-038B-40 | Link to docs MUST be provided | P2 |
| FR-038B-41 | Interactive resolution MUST be offered | P2 |
| FR-038B-42 | Auto-fix suggestion MUST be offered | P2 |
| FR-038B-43 | Verify after fix MUST be explained | P1 |
| FR-038B-44 | Re-scan command MUST be shown | P1 |
| FR-038B-45 | False positive report MUST be possible | P2 |

### FR-046 to FR-060: Audit Integration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-038B-46 | Block event MUST be logged | P0 |
| FR-038B-47 | Log MUST include timestamp | P0 |
| FR-038B-48 | Log MUST include operation type | P0 |
| FR-038B-49 | Log MUST include file paths | P0 |
| FR-038B-50 | Log MUST include secret types | P0 |
| FR-038B-51 | Log MUST include locations | P0 |
| FR-038B-52 | Log MUST NOT include secret values | P0 |
| FR-038B-53 | Log MUST include user action | P1 |
| FR-038B-54 | Structured log format | P0 |
| FR-038B-55 | Events: commit blocked | P1 |
| FR-038B-56 | Events: push blocked | P1 |
| FR-038B-57 | Events: resolution attempt | P2 |
| FR-038B-58 | Metrics: blocks per day | P2 |
| FR-038B-59 | Workspace DB record MUST be created | P0 |
| FR-038B-60 | Block history MUST be queryable | P2 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-038B-01 | Small commit scan | <1s | P1 |
| NFR-038B-02 | Medium commit scan | <3s | P1 |
| NFR-038B-03 | Large commit scan | <10s | P2 |
| NFR-038B-04 | Push scan (1 commit) | <2s | P1 |
| NFR-038B-05 | Push scan (10 commits) | <20s | P2 |
| NFR-038B-06 | Memory per scan | <100MB | P1 |
| NFR-038B-07 | Streaming for large | Yes | P1 |
| NFR-038B-08 | Error display | <100ms | P0 |
| NFR-038B-09 | Guidance render | <100ms | P1 |
| NFR-038B-10 | Audit write | <100ms | P1 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-038B-11 | All commits scanned | 100% | P0 |
| NFR-038B-12 | All pushes scanned | 100% | P0 |
| NFR-038B-13 | Non-bypassable | 100% | P0 |
| NFR-038B-14 | Detection rate | >99.9% | P0 |
| NFR-038B-15 | Fail closed | 100% | P0 |
| NFR-038B-16 | Graceful on error | Always | P0 |
| NFR-038B-17 | Thread safety | No races | P0 |
| NFR-038B-18 | Cross-platform | All OS | P0 |
| NFR-038B-19 | Encoding support | UTF-8 | P0 |
| NFR-038B-20 | Git version compat | 2.20+ | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-038B-21 | Block logged | Info level | P0 |
| NFR-038B-22 | Scan start logged | Debug level | P1 |
| NFR-038B-23 | Secret found logged | Warning level | P0 |
| NFR-038B-24 | Error logged | Error level | P0 |
| NFR-038B-25 | Metrics exported | Prometheus | P2 |
| NFR-038B-26 | Events published | EventBus | P1 |
| NFR-038B-27 | Structured logging | JSON | P0 |
| NFR-038B-28 | Correlation ID | Required | P0 |
| NFR-038B-29 | DB event stored | Required | P0 |
| NFR-038B-30 | Dashboard data | Exported | P2 |

---

## Acceptance Criteria / Definition of Done

### Pre-Commit
- [ ] AC-001: All commits scanned
- [ ] AC-002: Staged files scanned
- [ ] AC-003: Secret blocks commit
- [ ] AC-004: Non-bypassable
- [ ] AC-005: Reason shown
- [ ] AC-006: Location shown
- [ ] AC-007: Value never shown
- [ ] AC-008: Type shown

### Pre-Push
- [ ] AC-009: All pushes scanned
- [ ] AC-010: Commits scanned
- [ ] AC-011: Secret blocks push
- [ ] AC-012: Non-bypassable
- [ ] AC-013: Commit ID shown
- [ ] AC-014: Remote shown
- [ ] AC-015: Branch shown
- [ ] AC-016: Multiple commits

### Resolution
- [ ] AC-017: Steps shown
- [ ] AC-018: Commands shown
- [ ] AC-019: Alternatives suggested
- [ ] AC-020: Rotation warned
- [ ] AC-021: Amend suggested
- [ ] AC-022: Verify explained
- [ ] AC-023: Re-scan shown
- [ ] AC-024: False positive path

### Audit
- [ ] AC-025: Block logged
- [ ] AC-026: Timestamp included
- [ ] AC-027: Operation included
- [ ] AC-028: Paths included
- [ ] AC-029: Types included
- [ ] AC-030: Value never
- [ ] AC-031: DB record
- [ ] AC-032: Queryable

---

## User Verification Scenarios

### Scenario 1: Commit with API Key
**Persona:** Developer committing config  
**Preconditions:** API key in staged file  
**Steps:**
1. Stage file with key
2. Attempt commit
3. Block shown
4. Location identified

**Verification Checklist:**
- [ ] Commit blocked
- [ ] Key identified
- [ ] Location shown
- [ ] Value hidden

### Scenario 2: Push with Token
**Persona:** Developer pushing branch  
**Preconditions:** Token in previous commit  
**Steps:**
1. Commit slipped through
2. Attempt push
3. Block at push
4. Commit identified

**Verification Checklist:**
- [ ] Push blocked
- [ ] Commit named
- [ ] Resolution shown
- [ ] Amend suggested

### Scenario 3: Multiple Secrets
**Persona:** Developer with many issues  
**Preconditions:** 3 secrets in staged  
**Steps:**
1. Stage files
2. Attempt commit
3. All 3 listed
4. Locations for each

**Verification Checklist:**
- [ ] All found
- [ ] All listed
- [ ] Locations each
- [ ] Types shown

### Scenario 4: Resolution Flow
**Persona:** Developer fixing issue  
**Preconditions:** Secret detected  
**Steps:**
1. See guidance
2. Remove secret
3. Stage again
4. Commit succeeds

**Verification Checklist:**
- [ ] Guidance clear
- [ ] Fix applied
- [ ] Re-scan clean
- [ ] Commit works

### Scenario 5: Large Commit
**Persona:** Developer with many files  
**Preconditions:** 100 files staged  
**Steps:**
1. Stage many files
2. Attempt commit
3. Streaming scan
4. Complete scan

**Verification Checklist:**
- [ ] Streaming used
- [ ] All scanned
- [ ] No timeout
- [ ] Result correct

### Scenario 6: History Check
**Persona:** Developer amending  
**Preconditions:** Amend with new secret  
**Steps:**
1. Amend commit
2. Add secret
3. Scan catches
4. Block amend

**Verification Checklist:**
- [ ] Amend blocked
- [ ] Secret found
- [ ] Guidance shown
- [ ] Safe amend suggested

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-038B-01 | Commit scan | FR-038B-01 |
| UT-038B-02 | Staged detection | FR-038B-02 |
| UT-038B-03 | Block on secret | FR-038B-03 |
| UT-038B-04 | Non-bypassable | FR-038B-04 |
| UT-038B-05 | Location shown | FR-038B-06 |
| UT-038B-06 | Value hidden | FR-038B-07 |
| UT-038B-07 | Push scan | FR-038B-16 |
| UT-038B-08 | Commit ID shown | FR-038B-20 |
| UT-038B-09 | Guidance rendered | FR-038B-31 |
| UT-038B-10 | Multiple listed | FR-038B-12 |
| UT-038B-11 | Exit code | FR-038B-13 |
| UT-038B-12 | JSON format | FR-038B-15 |
| UT-038B-13 | Audit logged | FR-038B-46 |
| UT-038B-14 | DB record | FR-038B-59 |
| UT-038B-15 | Thread safety | NFR-038B-17 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-038B-01 | Full commit flow | E2E |
| IT-038B-02 | Full push flow | E2E |
| IT-038B-03 | Resolution flow | FR-038B-31 |
| IT-038B-04 | Git integration | Task 016-018 |
| IT-038B-05 | Multiple commits | FR-038B-28 |
| IT-038B-06 | Large commit | NFR-038B-03 |
| IT-038B-07 | Streaming mode | NFR-038B-07 |
| IT-038B-08 | Error handling | NFR-038B-16 |
| IT-038B-09 | Workspace DB | Task 050 |
| IT-038B-10 | Cross-platform | NFR-038B-18 |
| IT-038B-11 | Git versions | NFR-038B-20 |
| IT-038B-12 | Performance | NFR-038B-01 |
| IT-038B-13 | Detection rate | NFR-038B-14 |
| IT-038B-14 | Amend case | Scenario 6 |
| IT-038B-15 | Events | FR-038B-55 |

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Secrets/
│       └── Git/
│           ├── CommitBlockResult.cs
│           ├── PushBlockResult.cs
│           └── SecretLocation.cs
├── Acode.Application/
│   └── Secrets/
│       └── Git/
│           ├── IPreCommitScanner.cs
│           ├── IPrePushScanner.cs
│           └── IResolutionGuidance.cs
├── Acode.Infrastructure/
│   └── Secrets/
│       └── Git/
│           ├── PreCommitScanner.cs
│           ├── PrePushScanner.cs
│           ├── ResolutionGuidance.cs
│           └── GitStagingReader.cs
```

### Exit Codes

```
EXIT_CODE_SUCCESS = 0
EXIT_CODE_SECRET_DETECTED = 10
EXIT_CODE_SCAN_ERROR = 11
EXIT_CODE_GIT_ERROR = 12
EXIT_CODE_TIMEOUT = 13
```

### Error Message Format

```
╔══════════════════════════════════════════════════════════════════╗
║  🛑 COMMIT BLOCKED: Secret Detected                              ║
╠══════════════════════════════════════════════════════════════════╣
║  File: src/config/settings.json                                  ║
║  Line: 42                                                        ║
║  Type: AWS_ACCESS_KEY                                            ║
╠══════════════════════════════════════════════════════════════════╣
║  Resolution Steps:                                               ║
║  1. Remove the secret from the file                              ║
║  2. Use environment variable: AWS_ACCESS_KEY_ID                  ║
║  3. Add to .gitignore if appropriate                             ║
║  4. Run: acode git scan --verify                                 ║
║                                                                  ║
║  ⚠️  If this secret was ever committed, rotate it immediately!   ║
╚══════════════════════════════════════════════════════════════════╝
```

**End of Task 038.b Specification**
