# Task 001.c: Write Constraints Doc + Enforcement Checklist

**Priority:** 7 / 49  
**Tier:** Foundation  
**Complexity:** 3 (Fibonacci points)  
**Phase:** Phase 0 — Foundation  
**Dependencies:** Task 001 (parent), Task 001.a (mode matrix), Task 001.b (validation rules)  

---

## Description

### Overview

Task 001.c creates the authoritative constraints documentation and the enforcement checklist that ensures all constraints are properly implemented. This documentation serves multiple audiences: developers implementing features, security auditors verifying compliance, and users understanding the system's guarantees.

The constraints document is not just informational—it is a specification that every feature must reference and comply with. The enforcement checklist is an operational tool used during code review and security audits to verify that constraints are properly implemented.

### Business Value

Formal constraint documentation delivers:

1. **Implementation Guidance** — Developers know what they must enforce
2. **Security Assurance** — Auditors can verify compliance
3. **User Trust** — Users understand the system's guarantees
4. **Compliance Support** — Documentation supports regulatory reviews
5. **Consistency** — Single source of truth prevents drift

### Scope Boundaries

**In Scope:**
- Constraints reference document (CONSTRAINTS.md)
- Enforcement checklist for code review
- Enforcement checklist for security audit
- Constraint violation severity levels
- Constraint testing requirements
- Constraint monitoring requirements
- Documentation in code (XML docs, comments)
- Architecture Decision Records (ADRs) for constraints

**Out of Scope:**
- Implementation of constraint enforcement (Tasks 001.a, 001.b)
- Mode implementation (Task 001)
- Configuration schema (Task 002)
- Threat modeling (Task 003)
- CI/CD enforcement (Epic 8)

### Integration Points

| Task | Relationship | Description |
|------|--------------|-------------|
| Task 001 | Parent | Provides constraint definitions |
| Task 001.a | Sibling | Provides mode matrix |
| Task 001.b | Sibling | Provides validation rules |
| Task 003 | Consumer | References constraints in threat model |
| Epic 8 | Consumer | CI/CD uses enforcement checklist |
| All Tasks | Consumer | All must reference constraints doc |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Doc outdated | Implementation drift | Version with code |
| Doc incomplete | Gaps in enforcement | Comprehensive review |
| Doc ambiguous | Inconsistent interpretation | Clear, precise language |
| Checklist skipped | Unverified compliance | Mandatory PR step |

### Assumptions

1. Documentation will be maintained with code
2. Code reviewers will use the checklist
3. Security audits will occur periodically
4. Constraints are stable (infrequent changes)
5. ADR format is understood by team

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| **CONSTRAINTS.md** | The authoritative constraints document |
| **Enforcement Checklist** | Verification steps for constraint compliance |
| **ADR** | Architecture Decision Record |
| **Constraint** | Invariant that must never be violated |
| **Hard Constraint** | Absolute requirement, no exceptions |
| **Soft Constraint** | Preference, can be overridden with justification |
| **Violation Severity** | Criticality level of a constraint breach |
| **Code Review Checklist** | Items verified during PR review |
| **Security Audit Checklist** | Items verified during security review |
| **Defense in Depth** | Multiple layers of enforcement |
| **Single Source of Truth** | One authoritative definition |

---

## Out of Scope

- Implementation of constraint checking code
- Implementation of mode validation
- Automated enforcement in CI/CD
- Penetration testing
- Security audit execution
- Compliance certification
- Legal review of constraints
- Privacy policy updates
- Terms of service updates
- User agreements

---

## Functional Requirements

### CONSTRAINTS.md Document (FR-001c-01 to FR-001c-30)

| ID | Requirement |
|----|-------------|
| FR-001c-01 | CONSTRAINTS.md MUST exist at repository root |
| FR-001c-02 | Document MUST have version number |
| FR-001c-03 | Document MUST have last-updated date |
| FR-001c-04 | Document MUST have change history |
| FR-001c-05 | Document MUST list all hard constraints |
| FR-001c-06 | Document MUST list all soft constraints |
| FR-001c-07 | Each constraint MUST have unique ID |
| FR-001c-08 | Each constraint MUST have description |
| FR-001c-09 | Each constraint MUST have rationale |
| FR-001c-10 | Each constraint MUST have enforcement mechanism |
| FR-001c-11 | Each constraint MUST have test requirements |
| FR-001c-12 | Each constraint MUST have violation severity |
| FR-001c-13 | Document MUST define severity levels |
| FR-001c-14 | Document MUST include mode matrix reference |
| FR-001c-15 | Document MUST include validation rules reference |
| FR-001c-16 | Document MUST have compliance mapping |
| FR-001c-17 | Document MUST have quick reference table |
| FR-001c-18 | Document MUST be in Markdown format |
| FR-001c-19 | Document MUST be spell-checked |
| FR-001c-20 | Document MUST use consistent terminology |
| FR-001c-21 | Document MUST be reviewed by security |
| FR-001c-22 | Document MUST be approved by product |
| FR-001c-23 | Document MUST link to related docs |
| FR-001c-24 | Document MUST be searchable |
| FR-001c-25 | Document MUST render correctly on GitHub |
| FR-001c-26 | Document MUST have table of contents |
| FR-001c-27 | Document MUST have examples |
| FR-001c-28 | Document MUST have FAQ section |
| FR-001c-29 | Document MUST have contact/owner info |
| FR-001c-30 | Document changes MUST require PR review |

### Enforcement Checklist (FR-001c-31 to FR-001c-55)

| ID | Requirement |
|----|-------------|
| FR-001c-31 | Checklist MUST exist for code review |
| FR-001c-32 | Checklist MUST exist for security audit |
| FR-001c-33 | Checklist MUST cover all constraints |
| FR-001c-34 | Checklist items MUST be verifiable |
| FR-001c-35 | Checklist items MUST be actionable |
| FR-001c-36 | Checklist MUST have pass/fail for each item |
| FR-001c-37 | Checklist MUST have N/A option where appropriate |
| FR-001c-38 | Checklist MUST reference constraint IDs |
| FR-001c-39 | Checklist MUST be completable in PR template |
| FR-001c-40 | Checklist MUST have section for mode constraints |
| FR-001c-41 | Checklist MUST have section for data constraints |
| FR-001c-42 | Checklist MUST have section for network constraints |
| FR-001c-43 | Checklist MUST have section for security constraints |
| FR-001c-44 | Checklist MUST be versioned |
| FR-001c-45 | Checklist updates MUST update PR template |
| FR-001c-46 | Checklist completion MUST be verified |
| FR-001c-47 | Incomplete checklist MUST block merge |
| FR-001c-48 | Checklist MUST link to constraint details |
| FR-001c-49 | Checklist MUST be printable |
| FR-001c-50 | Checklist MUST support partial completion |
| FR-001c-51 | Checklist MUST track who completed each item |
| FR-001c-52 | Checklist MUST include test verification |
| FR-001c-53 | Checklist MUST include documentation verification |
| FR-001c-54 | Checklist MUST be machine-parseable |
| FR-001c-55 | Checklist MUST be easy to update |

### Architecture Decision Records (FR-001c-56 to FR-001c-70)

| ID | Requirement |
|----|-------------|
| FR-001c-56 | ADR MUST exist for "No External LLM API" constraint |
| FR-001c-57 | ADR MUST exist for "Three Operating Modes" decision |
| FR-001c-58 | ADR MUST exist for "Airgapped Mode Permanence" decision |
| FR-001c-59 | ADR MUST exist for "Burst Mode Consent" requirement |
| FR-001c-60 | ADR MUST exist for "Secrets Redaction" requirement |
| FR-001c-61 | ADRs MUST follow standard template |
| FR-001c-62 | ADRs MUST include context |
| FR-001c-63 | ADRs MUST include decision |
| FR-001c-64 | ADRs MUST include consequences |
| FR-001c-65 | ADRs MUST include alternatives considered |
| FR-001c-66 | ADRs MUST be numbered sequentially |
| FR-001c-67 | ADRs MUST be in docs/adr directory |
| FR-001c-68 | ADRs MUST be indexed in README |
| FR-001c-69 | ADRs MUST reference constraint IDs |
| FR-001c-70 | ADRs MUST be immutable once accepted |

### Code Documentation (FR-001c-71 to FR-001c-85)

| ID | Requirement |
|----|-------------|
| FR-001c-71 | Constraint-related classes MUST have XML docs |
| FR-001c-72 | XML docs MUST reference constraint IDs |
| FR-001c-73 | Constraint checks MUST have inline comments |
| FR-001c-74 | Comments MUST explain why, not what |
| FR-001c-75 | Defense-in-depth layers MUST be commented |
| FR-001c-76 | Validation methods MUST reference rules |
| FR-001c-77 | Mode enum MUST have comprehensive docs |
| FR-001c-78 | Denylist patterns MUST be documented |
| FR-001c-79 | Allowlist entries MUST be documented |
| FR-001c-80 | Error messages MUST reference constraints |
| FR-001c-81 | Log messages MUST reference constraint IDs |
| FR-001c-82 | Test classes MUST document constraint coverage |
| FR-001c-83 | Test methods MUST reference constraint IDs |
| FR-001c-84 | Documentation MUST be accurate |
| FR-001c-85 | Documentation MUST be complete |

---

## Non-Functional Requirements

### Accuracy (NFR-001c-01 to NFR-001c-08)

| ID | Requirement |
|----|-------------|
| NFR-001c-01 | Documentation MUST match implementation |
| NFR-001c-02 | Checklist MUST match constraints document |
| NFR-001c-03 | ADRs MUST reflect actual decisions |
| NFR-001c-04 | Code docs MUST match behavior |
| NFR-001c-05 | Version numbers MUST be accurate |
| NFR-001c-06 | Dates MUST be accurate |
| NFR-001c-07 | Cross-references MUST be valid |
| NFR-001c-08 | Examples MUST work |

### Maintainability (NFR-001c-09 to NFR-001c-16)

| ID | Requirement |
|----|-------------|
| NFR-001c-09 | Documentation MUST be easy to update |
| NFR-001c-10 | Changes MUST require minimal edits |
| NFR-001c-11 | Single source of truth maintained |
| NFR-001c-12 | No duplication across documents |
| NFR-001c-13 | Templates MUST be reusable |
| NFR-001c-14 | Formatting MUST be consistent |
| NFR-001c-15 | Structure MUST be logical |
| NFR-001c-16 | Updates MUST be trackable |

### Accessibility (NFR-001c-17 to NFR-001c-24)

| ID | Requirement |
|----|-------------|
| NFR-001c-17 | Documentation MUST be readable by non-experts |
| NFR-001c-18 | Jargon MUST be defined in glossary |
| NFR-001c-19 | Structure MUST support scanning |
| NFR-001c-20 | Important points MUST be highlighted |
| NFR-001c-21 | Table of contents MUST exist |
| NFR-001c-22 | Quick reference MUST exist |
| NFR-001c-23 | Examples MUST illustrate concepts |
| NFR-001c-24 | FAQ MUST address common questions |

---

## User Manual Documentation

### CONSTRAINTS.md Structure

The constraints document follows this structure:

```markdown
# Acode Constraints Reference

Version: 1.0.0
Last Updated: 2024-01-15
Owner: Security Team

## Table of Contents
1. Quick Reference
2. Constraint Definitions
3. Severity Levels
4. Enforcement Mechanisms
5. Compliance Mapping
6. FAQ

## Quick Reference

| ID | Constraint | Severity | Mode Applies |
|----|------------|----------|--------------|
| HC-01 | No external LLM API in LocalOnly | Critical | LocalOnly, Airgapped |
| HC-02 | No network in Airgapped | Critical | Airgapped |
| ... | ... | ... | ... |

## Constraint Definitions

### HC-01: No External LLM API in LocalOnly Mode

**Description:** When operating in LocalOnly mode, the system MUST NOT 
make any API calls to external LLM services.

**Rationale:** This is the core privacy guarantee of Acode. Users choose 
LocalOnly mode specifically to ensure their code never leaves their machine.

**Enforcement:**
- Denylist of known LLM API endpoints
- IP validation after DNS resolution
- HTTP client interception

**Tests Required:**
- Unit test for each denylist pattern
- Integration test for HTTP client blocking
- E2E test for user workflow

**Violation Severity:** Critical (immediate abort)

---

### HC-02: No Network Access in Airgapped Mode
...
```

### Enforcement Checklist Format

The checklist is embedded in the PR template:

```markdown
## Constraint Compliance Checklist

### Mode Constraints
- [ ] **HC-01:** Changes do not add external LLM calls in LocalOnly mode
- [ ] **HC-02:** Changes respect Airgapped network restrictions
- [ ] **HC-03:** Burst mode requires consent before external calls

### Data Constraints
- [ ] **HC-04:** No source code transmitted without consent
- [ ] **HC-05:** Secrets are redacted before any transmission
- [ ] **HC-06:** Prompts logged locally in Burst mode

### Network Constraints
- [ ] **HC-07:** All network calls go through NetworkGuard
- [ ] **HC-08:** No direct HTTP client usage
- [ ] **HC-09:** DNS resolution validated

### Documentation
- [ ] Code comments reference constraint IDs where relevant
- [ ] New constraints documented in CONSTRAINTS.md
- [ ] Tests reference constraint IDs

### Test Coverage
- [ ] New constraint logic has unit tests
- [ ] Integration tests cover constraint scenarios
- [ ] No decrease in constraint test coverage
```

### ADR Template

```markdown
# ADR-001: No External LLM API by Default

## Status
Accepted

## Context
Users of Acode expect their source code to remain private by default.
Many enterprise users cannot use tools that transmit code externally.
Compliance requirements (GDPR, SOC2) may prohibit data transmission.

## Decision
Acode will operate in LocalOnly mode by default, which prohibits all
external LLM API calls. External APIs are only accessible in Burst mode
with explicit user consent.

## Consequences

### Positive
- User trust established
- Enterprise adoption enabled
- Compliance simplified
- Competitive differentiation

### Negative
- Limited to local model capabilities by default
- Requires Ollama installation
- May frustrate users wanting cloud AI

## Alternatives Considered

1. **Cloud-first with opt-out:** Rejected - violates privacy-first principle
2. **Hybrid default:** Rejected - ambiguous privacy posture
3. **Per-action consent:** Rejected - too disruptive to workflow

## Related Constraints
- HC-01: No external LLM API in LocalOnly mode
- HC-03: Consent required for Burst mode
```

### Using the Documentation

**For Developers:**
1. Before implementing any feature, read CONSTRAINTS.md
2. Identify which constraints apply to your feature
3. Implement with constraint compliance in mind
4. Reference constraint IDs in code comments
5. Complete the PR checklist

**For Reviewers:**
1. Verify PR checklist is complete
2. Check that constraint-related code is documented
3. Verify tests cover constraint scenarios
4. Ensure no constraint violations introduced

**For Security Auditors:**
1. Use the security audit checklist
2. Verify each constraint is enforced
3. Test constraint bypass attempts
4. Review defense-in-depth layers
5. Document findings against constraint IDs

---

## Acceptance Criteria / Definition of Done

### CONSTRAINTS.md (30 items)

- [ ] File exists at repository root
- [ ] Has version number
- [ ] Has last-updated date
- [ ] Has change history section
- [ ] Has table of contents
- [ ] Has quick reference table
- [ ] Lists all hard constraints
- [ ] Lists all soft constraints
- [ ] Each constraint has unique ID
- [ ] Each constraint has description
- [ ] Each constraint has rationale
- [ ] Each constraint has enforcement mechanism
- [ ] Each constraint has test requirements
- [ ] Each constraint has violation severity
- [ ] Severity levels defined
- [ ] Mode matrix referenced
- [ ] Validation rules referenced
- [ ] Compliance mapping included
- [ ] FAQ section present
- [ ] Contact/owner info present
- [ ] Spell-checked
- [ ] Consistent terminology
- [ ] Links valid
- [ ] Renders on GitHub
- [ ] Examples work
- [ ] Reviewed by security
- [ ] Approved by product
- [ ] In source control
- [ ] PR required for changes
- [ ] Matches implementation

### Enforcement Checklist (25 items)

- [ ] Code review checklist exists
- [ ] Security audit checklist exists
- [ ] All constraints covered
- [ ] Items are verifiable
- [ ] Items are actionable
- [ ] Pass/fail for each item
- [ ] N/A option available
- [ ] References constraint IDs
- [ ] Embedded in PR template
- [ ] Mode constraints section
- [ ] Data constraints section
- [ ] Network constraints section
- [ ] Security constraints section
- [ ] Versioned
- [ ] Matches constraints doc
- [ ] Completion verified
- [ ] Links to details
- [ ] Printable
- [ ] Supports partial completion
- [ ] Machine-parseable
- [ ] Easy to update
- [ ] Tested in real PR
- [ ] Reviewed by team
- [ ] Documented usage
- [ ] Integrated with CI (future)

### ADRs (20 items)

- [ ] ADR-001 No External LLM API exists
- [ ] ADR-002 Three Operating Modes exists
- [ ] ADR-003 Airgapped Permanence exists
- [ ] ADR-004 Burst Mode Consent exists
- [ ] ADR-005 Secrets Redaction exists
- [ ] Standard template used
- [ ] Context included
- [ ] Decision clear
- [ ] Consequences listed
- [ ] Alternatives documented
- [ ] Numbered sequentially
- [ ] In docs/adr directory
- [ ] Indexed in README
- [ ] Reference constraint IDs
- [ ] Immutable once accepted
- [ ] Reviewed by team
- [ ] Approved by stakeholders
- [ ] Consistent format
- [ ] Spell-checked
- [ ] Links valid

### Code Documentation (20 items)

- [ ] Constraint classes have XML docs
- [ ] XML docs reference IDs
- [ ] Inline comments explain why
- [ ] Defense layers commented
- [ ] Validation methods referenced
- [ ] Mode enum documented
- [ ] Denylist documented
- [ ] Allowlist documented
- [ ] Error messages reference constraints
- [ ] Log messages reference IDs
- [ ] Test classes document coverage
- [ ] Test methods reference IDs
- [ ] Documentation accurate
- [ ] Documentation complete
- [ ] Consistent style
- [ ] Reviewed in PRs
- [ ] Updated with code changes
- [ ] Searchable
- [ ] Examples included
- [ ] Links valid

### Integration (15 items)

- [ ] CONSTRAINTS.md linked from README
- [ ] Checklist in PR template
- [ ] ADRs indexed
- [ ] All docs consistent
- [ ] No contradictions
- [ ] Single source of truth
- [ ] Update process documented
- [ ] Ownership clear
- [ ] Review process clear
- [ ] Version tracking works
- [ ] Change history maintained
- [ ] Cross-references valid
- [ ] Navigation easy
- [ ] Findable via search
- [ ] Tested with real users

---

## Testing Requirements

### Unit Tests

| ID | Test | Expected |
|----|------|----------|
| UT-001c-01 | CONSTRAINTS.md exists | File found |
| UT-001c-02 | CONSTRAINTS.md has version | Version present |
| UT-001c-03 | All constraint IDs unique | No duplicates |
| UT-001c-04 | All constraint IDs follow pattern | HC-XX format |
| UT-001c-05 | Checklist covers all constraints | Complete coverage |
| UT-001c-06 | PR template contains checklist | Checklist present |
| UT-001c-07 | All ADRs follow template | Structure valid |
| UT-001c-08 | ADR index is complete | All ADRs listed |
| UT-001c-09 | Links in docs are valid | All resolve |
| UT-001c-10 | Code references valid constraint IDs | IDs exist |

### Integration Tests

| ID | Test | Expected |
|----|------|----------|
| IT-001c-01 | PR with incomplete checklist | Flagged |
| IT-001c-02 | Constraint code matches docs | Consistent |
| IT-001c-03 | Error messages match constraints | Correct IDs |
| IT-001c-04 | Log messages match constraints | Correct IDs |
| IT-001c-05 | Test coverage matches docs | Complete |

### End-to-End Tests

| ID | Test | Expected |
|----|------|----------|
| E2E-001c-01 | Developer follows CONSTRAINTS.md | Clear guidance |
| E2E-001c-02 | Reviewer uses checklist | Actionable items |
| E2E-001c-03 | Auditor finds all constraints | Complete list |
| E2E-001c-04 | New constraint added | Process works |
| E2E-001c-05 | Documentation updated | Sync maintained |

---

## User Verification Steps

### Verification 1: CONSTRAINTS.md Exists
1. Navigate to repository root
2. **Verify:** CONSTRAINTS.md file exists

### Verification 2: Document Completeness
1. Open CONSTRAINTS.md
2. Check table of contents
3. **Verify:** All sections present

### Verification 3: Quick Reference Usable
1. Open CONSTRAINTS.md
2. Find quick reference table
3. **Verify:** All constraints listed with severity

### Verification 4: Constraint Details Complete
1. Pick any constraint (e.g., HC-01)
2. Read full definition
3. **Verify:** Has ID, description, rationale, enforcement, tests, severity

### Verification 5: PR Template Has Checklist
1. Create new PR (or view template)
2. **Verify:** Constraint checklist appears

### Verification 6: Checklist Actionable
1. Read each checklist item
2. **Verify:** Each is verifiable and actionable

### Verification 7: ADRs Accessible
1. Navigate to docs/adr
2. **Verify:** All ADRs listed and indexed

### Verification 8: ADR Structure Valid
1. Open any ADR
2. **Verify:** Has Context, Decision, Consequences, Alternatives

### Verification 9: Code References Constraints
1. Open constraint-related code file
2. **Verify:** Comments reference constraint IDs

### Verification 10: Documentation Matches Code
1. Read constraint in CONSTRAINTS.md
2. Find implementation in code
3. **Verify:** Behavior matches documentation

---

## Implementation Prompt for Claude

### Files to Create

```
CONSTRAINTS.md                      # At repository root
.github/
├── PULL_REQUEST_TEMPLATE.md        # With checklist
│
docs/
├── adr/
│   ├── README.md                   # ADR index
│   ├── adr-001-no-external-llm-default.md
│   ├── adr-002-three-operating-modes.md
│   ├── adr-003-airgapped-permanence.md
│   ├── adr-004-burst-mode-consent.md
│   └── adr-005-secrets-redaction.md
│
├── security-audit-checklist.md     # For auditors
```

### CONSTRAINTS.md Content

```markdown
# Acode Constraints Reference

**Version:** 1.0.0  
**Last Updated:** 2024-01-15  
**Owner:** Acode Security Team  
**Status:** Approved

---

## Table of Contents

1. [Quick Reference](#quick-reference)
2. [Severity Levels](#severity-levels)
3. [Hard Constraints](#hard-constraints)
4. [Soft Constraints](#soft-constraints)
5. [Enforcement Mechanisms](#enforcement-mechanisms)
6. [Compliance Mapping](#compliance-mapping)
7. [FAQ](#faq)
8. [Change History](#change-history)

---

## Quick Reference

| ID | Constraint | Severity | Modes |
|----|------------|----------|-------|
| HC-01 | No external LLM API in LocalOnly | Critical | LocalOnly, Airgapped |
| HC-02 | No network access in Airgapped | Critical | Airgapped |
| HC-03 | Consent required for Burst mode | Critical | Burst |
| HC-04 | Secrets redacted before transmission | Critical | Burst |
| HC-05 | All mode changes logged | High | All |
| HC-06 | Violations logged and aborted | High | All |
| HC-07 | Fail-safe to LocalOnly on error | High | All |

---

## Severity Levels

| Level | Description | Response |
|-------|-------------|----------|
| **Critical** | Core privacy/security guarantee | Immediate abort, logged as error |
| **High** | Important operational requirement | Operation blocked, logged as warning |
| **Medium** | Best practice enforcement | Warning issued, operation continues |
| **Low** | Guidance/recommendation | Logged for awareness |

---

## Hard Constraints

### HC-01: No External LLM API in LocalOnly Mode

**ID:** HC-01  
**Severity:** Critical  
**Applies To:** LocalOnly mode, Airgapped mode

**Description:**  
When operating in LocalOnly or Airgapped mode, the system MUST NOT make 
any API calls to external LLM services including but not limited to OpenAI, 
Anthropic, Azure OpenAI, Google AI, AWS Bedrock, Cohere, Hugging Face 
Inference API, Together.ai, and Replicate.

**Rationale:**  
This constraint is the foundation of Acode's privacy guarantee. Users 
choose LocalOnly mode specifically to ensure their source code, prompts, 
and development context never leave their local machine. Violating this 
constraint would fundamentally breach user trust.

**Enforcement Mechanisms:**
1. Denylist of known LLM API endpoints (hostname patterns)
2. IP validation after DNS resolution
3. HTTP client wrapper that validates all requests
4. Defense-in-depth: multiple validation checkpoints

**Test Requirements:**
- Unit tests for each denylist pattern
- Integration tests for HTTP client blocking
- E2E test verifying user workflow in LocalOnly mode
- Negative tests attempting bypass

**Violation Response:**
- Request immediately blocked (no data sent)
- Error logged with constraint ID (HC-01)
- User-facing error with remediation guidance
- Operation aborted

**Related:** Task 001.b, ADR-001

---

### HC-02: No Network Access in Airgapped Mode

**ID:** HC-02  
**Severity:** Critical  
**Applies To:** Airgapped mode

**Description:**  
When operating in Airgapped mode, the system MUST NOT make ANY network 
connections, including localhost connections to Ollama. All required 
resources must be pre-loaded.

...
```

### PR Template Checklist

```markdown
## Constraint Compliance

> Complete all applicable items. Mark N/A if not applicable.

### Mode Constraints
- [ ] **HC-01:** No external LLM calls added in LocalOnly mode
- [ ] **HC-02:** Airgapped network restrictions respected
- [ ] **HC-03:** Burst mode consent implemented if applicable

### Data Constraints  
- [ ] **HC-04:** Secrets redacted before any external transmission
- [ ] **HC-05:** No bulk code transmission implemented
- [ ] **HC-06:** Logging implemented without sensitive data

### Network Constraints
- [ ] All network calls use NetworkGuard
- [ ] No direct HttpClient instantiation
- [ ] Redirect handling validates destinations

### Documentation
- [ ] Constraint IDs referenced in relevant code comments
- [ ] New/modified constraints updated in CONSTRAINTS.md
- [ ] Tests reference constraint IDs in names/comments

### Testing
- [ ] Unit tests cover constraint logic
- [ ] Integration tests verify constraint enforcement
- [ ] Constraint test coverage maintained or increased
```

### Validation Checklist Before Merge

- [ ] CONSTRAINTS.md complete and accurate
- [ ] All constraints have unique IDs
- [ ] All constraints have required sections
- [ ] PR template contains checklist
- [ ] All ADRs created
- [ ] ADR index complete
- [ ] Security audit checklist exists
- [ ] Code documentation standards defined
- [ ] Cross-references valid
- [ ] Reviewed by security team
- [ ] Approved by product owner

---

**END OF TASK 001.c**
