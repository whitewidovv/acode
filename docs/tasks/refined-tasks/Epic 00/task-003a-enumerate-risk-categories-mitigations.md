# Task 003.a: Enumerate Risk Categories + Mitigations

**Priority:** 13 / 49  
**Tier:** Foundation  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 0 — Foundation  
**Dependencies:** Task 003 (threat model defined)  

---

## Description

### Overview

Task 003.a provides a comprehensive enumeration of risk categories and their corresponding mitigations for Acode. While Task 003 defines the threat model framework, this task details every specific risk that has been identified and maps each to concrete mitigation strategies. This creates a traceable security control matrix that developers can reference during implementation and auditors can verify during security assessments.

Risk categories are organized using the STRIDE framework (Spoofing, Tampering, Repudiation, Information Disclosure, Denial of Service, Elevation of Privilege) and scored using DREAD methodology (Damage, Reproducibility, Exploitability, Affected Users, Discoverability). Each risk has at least one mitigation, and high-severity risks have multiple mitigations following defense-in-depth principles.

### Business Value

Comprehensive risk enumeration provides:

1. **Implementation Guidance** — Developers know exactly what security controls to implement
2. **Audit Support** — Auditors can verify each risk has appropriate mitigations
3. **Compliance Evidence** — Documentation supports SOC2, ISO 27001 requirements
4. **Prioritization** — DREAD scoring helps prioritize security work
5. **Traceability** — Clear mapping from risk to control to test
6. **Risk Acceptance** — Documented residual risks for business decisions
7. **Training Material** — Educates team on security considerations

### Scope Boundaries

**In Scope:**
- Complete risk enumeration across all STRIDE categories
- DREAD scoring for each risk
- Mitigation strategy for each risk
- Control mapping (which code/config mitigates which risk)
- Residual risk documentation
- Risk owner assignment
- Mitigation verification approach

**Out of Scope:**
- Mitigation implementation (separate tasks)
- Penetration testing execution
- Third-party security assessments
- Risk acceptance decisions (business decision)
- Insurance and liability

### Integration Points

| Task | Relationship | Description |
|------|--------------|-------------|
| Task 003 | Parent | Provides threat model framework |
| Task 003.b | Sibling | Protected paths mitigate path risks |
| Task 003.c | Sibling | Audit logs mitigate repudiation |
| All Epics | Consumer | Implementation references risks |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Risk missed | Unmitigated vulnerability | Regular review cycle |
| Mitigation incomplete | Partial protection | Defense in depth |
| Scoring incorrect | Wrong prioritization | Peer review |
| Documentation stale | False security confidence | Version control |

### Assumptions

1. STRIDE is appropriate framework for risk categorization
2. DREAD provides useful prioritization metric
3. Risks are relatively stable (not changing daily)
4. Mitigations can be mapped to specific code/config
5. Some residual risk is acceptable

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| **STRIDE** | Spoofing, Tampering, Repudiation, Information Disclosure, DoS, Elevation of Privilege |
| **DREAD** | Damage, Reproducibility, Exploitability, Affected Users, Discoverability |
| **Risk Category** | Group of related security risks |
| **Mitigation** | Control that reduces risk likelihood or impact |
| **Residual Risk** | Risk remaining after mitigations applied |
| **Control** | Security mechanism (technical or procedural) |
| **Risk Score** | Numeric assessment of risk severity |
| **Risk Owner** | Person accountable for risk management |
| **Defense in Depth** | Multiple layers of security controls |
| **Control Mapping** | Linking risks to mitigating controls |
| **Compensating Control** | Alternative when primary control unavailable |
| **Risk Register** | Document tracking all identified risks |
| **Likelihood** | Probability of risk exploitation |
| **Impact** | Damage if risk is exploited |
| **Severity** | Combined likelihood and impact score |

---

## Out of Scope

- Implementation of mitigation controls
- Penetration testing and vulnerability scanning
- Third-party security assessments
- Bug bounty program design
- Security tool selection
- Security training program design
- Incident response procedures
- Business continuity planning
- Disaster recovery planning
- Insurance procurement
- Legal review of risks
- Regulatory compliance mapping (detailed)
- Vendor risk management
- Physical security risks

---

## Functional Requirements

### Risk Categorization Framework (FR-003a-01 to FR-003a-15)

| ID | Requirement |
|----|-------------|
| FR-003a-01 | Risks MUST be categorized using STRIDE framework |
| FR-003a-02 | Each risk MUST belong to exactly one STRIDE category |
| FR-003a-03 | Spoofing risks MUST address identity and authentication |
| FR-003a-04 | Tampering risks MUST address data integrity |
| FR-003a-05 | Repudiation risks MUST address non-deniability |
| FR-003a-06 | Information Disclosure risks MUST address confidentiality |
| FR-003a-07 | Denial of Service risks MUST address availability |
| FR-003a-08 | Elevation of Privilege risks MUST address authorization |
| FR-003a-09 | Each category MUST have at least 5 risks identified |
| FR-003a-10 | Risks MUST have unique identifiers (RISK-S-001, RISK-T-001, etc.) |
| FR-003a-11 | Risk identifiers MUST be stable across versions |
| FR-003a-12 | New risks MUST receive new identifiers (no reuse) |
| FR-003a-13 | Deprecated risks MUST be marked, not deleted |
| FR-003a-14 | Category MUST be derivable from risk ID prefix |
| FR-003a-15 | Risk register MUST be version-controlled |

### DREAD Scoring (FR-003a-16 to FR-003a-30)

| ID | Requirement |
|----|-------------|
| FR-003a-16 | Each risk MUST have DREAD score |
| FR-003a-17 | Damage MUST be scored 1-10 |
| FR-003a-18 | Reproducibility MUST be scored 1-10 |
| FR-003a-19 | Exploitability MUST be scored 1-10 |
| FR-003a-20 | Affected Users MUST be scored 1-10 |
| FR-003a-21 | Discoverability MUST be scored 1-10 |
| FR-003a-22 | Total DREAD score MUST be average of components |
| FR-003a-23 | Score 1-3 MUST be classified as Low severity |
| FR-003a-24 | Score 4-6 MUST be classified as Medium severity |
| FR-003a-25 | Score 7-10 MUST be classified as High severity |
| FR-003a-26 | Scoring rationale MUST be documented |
| FR-003a-27 | Scores MUST be reviewed when context changes |
| FR-003a-28 | Score changes MUST be logged with reason |
| FR-003a-29 | Scoring MUST be consistent across similar risks |
| FR-003a-30 | Scoring MUST be reviewed by security team |

### Spoofing Risks (FR-003a-31 to FR-003a-45)

| ID | Requirement |
|----|-------------|
| FR-003a-31 | RISK-S-001: Malicious config file injection MUST be documented |
| FR-003a-32 | RISK-S-002: Fake LLM provider endpoint MUST be documented |
| FR-003a-33 | RISK-S-003: Spoofed environment variables MUST be documented |
| FR-003a-34 | RISK-S-004: Impersonated repository MUST be documented |
| FR-003a-35 | RISK-S-005: Fake Acode binary MUST be documented |
| FR-003a-36 | RISK-S-006: Man-in-the-middle on localhost MUST be documented |
| FR-003a-37 | Each spoofing risk MUST have DREAD score |
| FR-003a-38 | Each spoofing risk MUST have at least one mitigation |
| FR-003a-39 | Config file validation MUST mitigate RISK-S-001 |
| FR-003a-40 | Endpoint validation MUST mitigate RISK-S-002 |
| FR-003a-41 | Environment sanitization MUST mitigate RISK-S-003 |
| FR-003a-42 | Repository verification MUST mitigate RISK-S-004 |
| FR-003a-43 | Binary signing SHOULD mitigate RISK-S-005 |
| FR-003a-44 | TLS for localhost SHOULD mitigate RISK-S-006 |
| FR-003a-45 | Spoofing mitigations MUST be testable |

### Tampering Risks (FR-003a-46 to FR-003a-60)

| ID | Requirement |
|----|-------------|
| FR-003a-46 | RISK-T-001: Config file modification MUST be documented |
| FR-003a-47 | RISK-T-002: LLM response manipulation MUST be documented |
| FR-003a-48 | RISK-T-003: Command injection via config MUST be documented |
| FR-003a-49 | RISK-T-004: Malicious code in repository MUST be documented |
| FR-003a-50 | RISK-T-005: Dependency tampering MUST be documented |
| FR-003a-51 | RISK-T-006: Log file modification MUST be documented |
| FR-003a-52 | RISK-T-007: Output file corruption MUST be documented |
| FR-003a-53 | Each tampering risk MUST have DREAD score |
| FR-003a-54 | Each tampering risk MUST have at least one mitigation |
| FR-003a-55 | Config validation MUST mitigate RISK-T-001 |
| FR-003a-56 | Output sanitization MUST mitigate RISK-T-002 |
| FR-003a-57 | Input escaping MUST mitigate RISK-T-003 |
| FR-003a-58 | Static analysis SHOULD mitigate RISK-T-004 |
| FR-003a-59 | Dependency verification MUST mitigate RISK-T-005 |
| FR-003a-60 | Append-only logs MUST mitigate RISK-T-006 |

### Repudiation Risks (FR-003a-61 to FR-003a-70)

| ID | Requirement |
|----|-------------|
| FR-003a-61 | RISK-R-001: Unlogged file modifications MUST be documented |
| FR-003a-62 | RISK-R-002: Unlogged command execution MUST be documented |
| FR-003a-63 | RISK-R-003: Unlogged mode changes MUST be documented |
| FR-003a-64 | RISK-R-004: Unlogged external API calls MUST be documented |
| FR-003a-65 | RISK-R-005: Log deletion MUST be documented |
| FR-003a-66 | Each repudiation risk MUST have DREAD score |
| FR-003a-67 | Each repudiation risk MUST have at least one mitigation |
| FR-003a-68 | Comprehensive logging MUST mitigate RISK-R-001 through R-004 |
| FR-003a-69 | Tamper-evident logs MUST mitigate RISK-R-005 |
| FR-003a-70 | Audit logging requirements MUST reference Task 003.c |

### Information Disclosure Risks (FR-003a-71 to FR-003a-90)

| ID | Requirement |
|----|-------------|
| FR-003a-71 | RISK-I-001: Source code exfiltration via LLM MUST be documented |
| FR-003a-72 | RISK-I-002: Secrets in logs MUST be documented |
| FR-003a-73 | RISK-I-003: Secrets in prompts MUST be documented |
| FR-003a-74 | RISK-I-004: Verbose error messages MUST be documented |
| FR-003a-75 | RISK-I-005: Config file exposure MUST be documented |
| FR-003a-76 | RISK-I-006: Temp file secrets MUST be documented |
| FR-003a-77 | RISK-I-007: Memory dump secrets MUST be documented |
| FR-003a-78 | RISK-I-008: Path disclosure MUST be documented |
| FR-003a-79 | RISK-I-009: Version information disclosure MUST be documented |
| FR-003a-80 | RISK-I-010: LLM training data leakage MUST be documented |
| FR-003a-81 | Each information disclosure risk MUST have DREAD score |
| FR-003a-82 | Each information disclosure risk MUST have at least one mitigation |
| FR-003a-83 | LocalOnly mode MUST mitigate RISK-I-001 |
| FR-003a-84 | Secret redaction MUST mitigate RISK-I-002, I-003 |
| FR-003a-85 | Safe error handling MUST mitigate RISK-I-004 |
| FR-003a-86 | File permissions MUST mitigate RISK-I-005 |
| FR-003a-87 | Secure temp files MUST mitigate RISK-I-006 |
| FR-003a-88 | Secure memory handling SHOULD mitigate RISK-I-007 |
| FR-003a-89 | Path normalization MUST mitigate RISK-I-008 |
| FR-003a-90 | Information disclosure MUST be highest priority category |

### Denial of Service Risks (FR-003a-91 to FR-003a-105)

| ID | Requirement |
|----|-------------|
| FR-003a-91 | RISK-D-001: Infinite loop in LLM response MUST be documented |
| FR-003a-92 | RISK-D-002: Resource exhaustion via large files MUST be documented |
| FR-003a-93 | RISK-D-003: Memory exhaustion via prompts MUST be documented |
| FR-003a-94 | RISK-D-004: Disk exhaustion via logs MUST be documented |
| FR-003a-95 | RISK-D-005: CPU exhaustion via regex MUST be documented |
| FR-003a-96 | RISK-D-006: Process fork bomb MUST be documented |
| FR-003a-97 | RISK-D-007: Network flooding MUST be documented |
| FR-003a-98 | Each DoS risk MUST have DREAD score |
| FR-003a-99 | Each DoS risk MUST have at least one mitigation |
| FR-003a-100 | Timeouts MUST mitigate RISK-D-001 |
| FR-003a-101 | File size limits MUST mitigate RISK-D-002 |
| FR-003a-102 | Memory limits MUST mitigate RISK-D-003 |
| FR-003a-103 | Log rotation MUST mitigate RISK-D-004 |
| FR-003a-104 | Regex timeouts MUST mitigate RISK-D-005 |
| FR-003a-105 | Process limits MUST mitigate RISK-D-006 |

### Elevation of Privilege Risks (FR-003a-106 to FR-003a-120)

| ID | Requirement |
|----|-------------|
| FR-003a-106 | RISK-E-001: Config-driven code execution MUST be documented |
| FR-003a-107 | RISK-E-002: Prompt injection to command execution MUST be documented |
| FR-003a-108 | RISK-E-003: Path traversal to system files MUST be documented |
| FR-003a-109 | RISK-E-004: Symlink following to protected areas MUST be documented |
| FR-003a-110 | RISK-E-005: YAML deserialization attacks MUST be documented |
| FR-003a-111 | RISK-E-006: Mode bypass MUST be documented |
| FR-003a-112 | RISK-E-007: Dependency confusion attacks MUST be documented |
| FR-003a-113 | Each EoP risk MUST have DREAD score |
| FR-003a-114 | Each EoP risk MUST have at least one mitigation |
| FR-003a-115 | Command whitelist MUST mitigate RISK-E-001 |
| FR-003a-116 | Output sanitization MUST mitigate RISK-E-002 |
| FR-003a-117 | Path validation MUST mitigate RISK-E-003, E-004 |
| FR-003a-118 | Safe YAML parsing MUST mitigate RISK-E-005 |
| FR-003a-119 | Mode enforcement MUST mitigate RISK-E-006 |
| FR-003a-120 | EoP risks MUST be treated as high severity |

---

## Non-Functional Requirements

### Documentation Quality (NFR-003a-01 to NFR-003a-15)

| ID | Requirement |
|----|-------------|
| NFR-003a-01 | Risk register MUST be complete and current |
| NFR-003a-02 | Risk descriptions MUST be clear and unambiguous |
| NFR-003a-03 | Mitigations MUST be specific and actionable |
| NFR-003a-04 | DREAD scores MUST have documented rationale |
| NFR-003a-05 | Control mappings MUST reference specific code/config |
| NFR-003a-06 | Risk register MUST be in machine-readable format |
| NFR-003a-07 | Risk register MUST be in human-readable format |
| NFR-003a-08 | Documentation MUST be reviewed quarterly |
| NFR-003a-09 | Changes MUST be tracked in version control |
| NFR-003a-10 | Risk register MUST be accessible to developers |
| NFR-003a-11 | Risk register MUST be accessible to auditors |
| NFR-003a-12 | Sensitive details MAY be in private documentation |
| NFR-003a-13 | Documentation MUST include last review date |
| NFR-003a-14 | Documentation MUST include risk owner |
| NFR-003a-15 | Documentation MUST include mitigation status |

### Mitigation Verification (NFR-003a-16 to NFR-003a-28)

| ID | Requirement |
|----|-------------|
| NFR-003a-16 | Each mitigation MUST have verification method |
| NFR-003a-17 | High-severity mitigations MUST have automated tests |
| NFR-003a-18 | Mitigation tests MUST run in CI |
| NFR-003a-19 | Failed mitigation tests MUST block release |
| NFR-003a-20 | Mitigation effectiveness MUST be measurable |
| NFR-003a-21 | Defense-in-depth MUST be verified for high risks |
| NFR-003a-22 | Compensating controls MUST be documented |
| NFR-003a-23 | Residual risk MUST be documented |
| NFR-003a-24 | Risk acceptance MUST be documented |
| NFR-003a-25 | Mitigation gaps MUST be tracked |
| NFR-003a-26 | New mitigations MUST be tested before release |
| NFR-003a-27 | Mitigation removal MUST be reviewed |
| NFR-003a-28 | Mitigation effectiveness MUST be reported |

### Risk Management Process (NFR-003a-29 to NFR-003a-40)

| ID | Requirement |
|----|-------------|
| NFR-003a-29 | New risks MUST be added within 5 business days |
| NFR-003a-30 | Risk scoring MUST be completed within review cycle |
| NFR-003a-31 | High-severity risks MUST have mitigation plan within 2 weeks |
| NFR-003a-32 | Critical risks MUST have immediate response |
| NFR-003a-33 | Risk register MUST be reviewed on each release |
| NFR-003a-34 | Risk trends MUST be tracked |
| NFR-003a-35 | Risk metrics MUST be reported quarterly |
| NFR-003a-36 | Risk owners MUST be notified of changes |
| NFR-003a-37 | Risk register MUST support search and filter |
| NFR-003a-38 | Risk register MUST support export |
| NFR-003a-39 | Risk history MUST be preserved |
| NFR-003a-40 | Risk dependencies MUST be documented |

---

## User Manual Documentation

### Risk Register Overview

The Acode risk register documents all identified security risks, their severity, and mitigations. This document provides guidance on understanding and using the risk register.

### STRIDE Categories

| Category | Description | Key Concern |
|----------|-------------|-------------|
| **S**poofing | Impersonating something or someone | Authentication |
| **T**ampering | Modifying data or code | Integrity |
| **R**epudiation | Denying actions were taken | Non-repudiation |
| **I**nformation Disclosure | Exposing data to unauthorized parties | Confidentiality |
| **D**enial of Service | Preventing legitimate use | Availability |
| **E**levation of Privilege | Gaining unauthorized capabilities | Authorization |

### DREAD Scoring

| Factor | Score 1-3 | Score 4-6 | Score 7-10 |
|--------|-----------|-----------|------------|
| **D**amage | Minor impact | Significant impact | Critical impact |
| **R**eproducibility | Hard to reproduce | Sometimes reproducible | Always reproducible |
| **E**xploitability | Requires expertise | Some skill needed | Easy to exploit |
| **A**ffected Users | Few users | Some users | All users |
| **D**iscoverability | Hard to discover | May be discovered | Easily discovered |

### Risk Register Format

Each risk entry contains:

```yaml
risk_id: RISK-I-001
category: Information Disclosure
title: Source code exfiltration via LLM
description: |
  In Burst mode, source code sent as context to external LLM could be
  stored, logged, or used for training by the provider.
dread:
  damage: 9         # IP theft is severe
  reproducibility: 10  # Always happens in Burst mode
  exploitability: 3   # Requires Burst mode consent
  affected_users: 10   # All users in Burst mode
  discoverability: 7   # Well-known risk
  total: 7.8
severity: High
mitigations:
  - id: MIT-001
    control: LocalOnly mode default
    description: External LLM disabled by default
    verification: Unit test confirms LocalOnly default
  - id: MIT-002
    control: Burst mode consent
    description: User must explicitly consent to data sharing
    verification: E2E test confirms consent prompt
  - id: MIT-003
    control: Context size limits
    description: Limit amount of code in prompts
    verification: Integration test confirms limits
residual_risk: |
  In Burst mode with consent, code is sent externally. Residual risk
  accepted as user-initiated with informed consent.
owner: Security Team
last_review: 2025-01-03
status: Active
```

### Top Risks by Severity

| ID | Risk | Severity | Primary Mitigation |
|----|------|----------|-------------------|
| RISK-I-001 | Source code exfiltration | High | LocalOnly mode |
| RISK-I-002 | Secrets in logs | High | Secret redaction |
| RISK-E-002 | Prompt injection → commands | High | Output sanitization |
| RISK-E-003 | Path traversal | High | Path validation |
| RISK-T-003 | Command injection | High | Input escaping |

### Viewing the Risk Register

```bash
# View all risks
acode security risks

# View risks by category
acode security risks --category information-disclosure

# View high-severity risks
acode security risks --severity high

# View risk details
acode security risk RISK-I-001

# Export risk register
acode security risks --export json > risks.json
```

### Mitigation Status

```bash
# View mitigation status
acode security mitigations

# View mitigations for a risk
acode security mitigations --risk RISK-I-001

# Verify mitigations (run tests)
acode security verify-mitigations
```

### Adding New Risks

New risks should be added through the security review process:

1. **Identify** — Document the risk in a security issue
2. **Categorize** — Assign STRIDE category
3. **Score** — Complete DREAD scoring with rationale
4. **Mitigate** — Propose mitigation strategies
5. **Review** — Security team review and approval
6. **Document** — Add to risk register
7. **Implement** — Implement mitigations
8. **Verify** — Add tests for mitigations

### Mitigation Mapping

Each mitigation maps to implementation:

| Mitigation | Implementation | Test |
|------------|---------------|------|
| LocalOnly mode default | `ConfigDefaults.Mode` | `test_default_mode_local` |
| Secret redaction | `SecretRedactor.cs` | `test_secret_patterns` |
| Path validation | `PathValidator.cs` | `test_path_traversal_blocked` |
| Config validation | `ConfigValidator.cs` | `test_config_validation` |
| Timeouts | `TimeoutPolicy.cs` | `test_timeout_enforcement` |

### Defense in Depth Example

High-severity risks have multiple mitigations:

**RISK-I-001: Source code exfiltration**

| Layer | Mitigation |
|-------|------------|
| Layer 1 | LocalOnly mode by default |
| Layer 2 | Burst mode requires explicit consent |
| Layer 3 | Context size limits |
| Layer 4 | Sensitive file exclusion |
| Layer 5 | Audit logging of external calls |

### Residual Risk

Some risk remains after mitigations. Documented residual risks:

| Risk | Residual Risk | Acceptance |
|------|--------------|------------|
| RISK-I-001 | Code sent in Burst mode with consent | User-accepted |
| RISK-T-004 | Malicious code in repo exists | Out of scope |
| RISK-D-001 | LLM may be slow | User tolerance |

### FAQ

**Q: Who owns the risk register?**
A: The Security Team owns the risk register, but all developers can view it.

**Q: How often is it updated?**
A: At minimum quarterly, and whenever new risks are identified.

**Q: Can I propose new mitigations?**
A: Yes, open a security issue with your proposal.

**Q: What if I find an undocumented risk?**
A: Report it immediately via security issue. High-severity risks get priority.

**Q: How are DREAD scores decided?**
A: Security team consensus with documented rationale.

---

## Acceptance Criteria / Definition of Done

### Risk Categorization (25 items)

- [ ] STRIDE framework adopted
- [ ] Each risk in exactly one category
- [ ] Spoofing risks complete (6+ risks)
- [ ] Tampering risks complete (7+ risks)
- [ ] Repudiation risks complete (5+ risks)
- [ ] Information Disclosure risks complete (10+ risks)
- [ ] Denial of Service risks complete (7+ risks)
- [ ] Elevation of Privilege risks complete (7+ risks)
- [ ] Unique identifiers assigned
- [ ] ID format correct (RISK-X-NNN)
- [ ] IDs stable across versions
- [ ] No reused IDs
- [ ] Deprecated risks marked
- [ ] Category derivable from ID
- [ ] Risk register versioned
- [ ] Total 40+ risks documented
- [ ] All high-priority risks included
- [ ] Risks reviewed by security team
- [ ] Risks mapped to threat actors
- [ ] Risks mapped to attack vectors
- [ ] Risk dependencies documented
- [ ] Risk relationships documented
- [ ] Categories balanced appropriately
- [ ] No duplicate risks
- [ ] All risks actionable

### DREAD Scoring (25 items)

- [ ] All risks have DREAD scores
- [ ] Damage scored 1-10
- [ ] Reproducibility scored 1-10
- [ ] Exploitability scored 1-10
- [ ] Affected Users scored 1-10
- [ ] Discoverability scored 1-10
- [ ] Total is average of components
- [ ] Low severity: 1-3
- [ ] Medium severity: 4-6
- [ ] High severity: 7-10
- [ ] Rationale documented for each score
- [ ] Scores reviewed when context changes
- [ ] Score changes logged
- [ ] Scoring consistent
- [ ] Scoring peer-reviewed
- [ ] High-severity risks identified
- [ ] Critical risks flagged
- [ ] Scoring methodology documented
- [ ] Scoring examples provided
- [ ] Scoring training available
- [ ] Scores defensible
- [ ] Scores align with industry standards
- [ ] Scores updated on new information
- [ ] Severity distribution reasonable
- [ ] Score validation complete

### Mitigation Documentation (30 items)

- [ ] Every risk has at least one mitigation
- [ ] High-severity risks have multiple mitigations
- [ ] Mitigations have unique IDs (MIT-NNN)
- [ ] Mitigations reference specific controls
- [ ] Control implementations identified
- [ ] Verification method documented
- [ ] Mitigation effectiveness measurable
- [ ] Defense-in-depth for high risks
- [ ] Compensating controls documented
- [ ] Residual risk documented
- [ ] Risk acceptance documented
- [ ] Mitigation gaps tracked
- [ ] Mitigation dependencies documented
- [ ] Mitigation owners assigned
- [ ] Mitigation timeline documented
- [ ] Mitigation status tracked
- [ ] Implemented mitigations verified
- [ ] Pending mitigations planned
- [ ] Mitigation tests exist
- [ ] Tests pass in CI
- [ ] Failed tests block release
- [ ] Mitigation coverage reported
- [ ] Mitigation effectiveness reviewed
- [ ] Mitigations updated as needed
- [ ] Mitigation retirement documented
- [ ] Cross-mitigation conflicts checked
- [ ] Mitigation cost considered
- [ ] Mitigation usability considered
- [ ] Mitigations are proportionate
- [ ] Mitigations are maintainable

### Spoofing Risks (15 items)

- [ ] RISK-S-001 documented (config injection)
- [ ] RISK-S-002 documented (fake LLM endpoint)
- [ ] RISK-S-003 documented (spoofed env vars)
- [ ] RISK-S-004 documented (impersonated repo)
- [ ] RISK-S-005 documented (fake binary)
- [ ] RISK-S-006 documented (MITM localhost)
- [ ] All spoofing risks have DREAD scores
- [ ] All spoofing risks have mitigations
- [ ] Config validation mitigates S-001
- [ ] Endpoint validation mitigates S-002
- [ ] Environment sanitization mitigates S-003
- [ ] Repository verification mitigates S-004
- [ ] Binary signing considered for S-005
- [ ] TLS considered for S-006
- [ ] Spoofing mitigations tested

### Tampering Risks (15 items)

- [ ] RISK-T-001 documented (config modification)
- [ ] RISK-T-002 documented (LLM response manipulation)
- [ ] RISK-T-003 documented (command injection)
- [ ] RISK-T-004 documented (malicious repo code)
- [ ] RISK-T-005 documented (dependency tampering)
- [ ] RISK-T-006 documented (log modification)
- [ ] RISK-T-007 documented (output corruption)
- [ ] All tampering risks have DREAD scores
- [ ] All tampering risks have mitigations
- [ ] Config validation mitigates T-001
- [ ] Output sanitization mitigates T-002
- [ ] Input escaping mitigates T-003
- [ ] Dependency verification mitigates T-005
- [ ] Append-only logs mitigate T-006
- [ ] Tampering mitigations tested

### Information Disclosure Risks (20 items)

- [ ] RISK-I-001 documented (code exfiltration)
- [ ] RISK-I-002 documented (secrets in logs)
- [ ] RISK-I-003 documented (secrets in prompts)
- [ ] RISK-I-004 documented (verbose errors)
- [ ] RISK-I-005 documented (config exposure)
- [ ] RISK-I-006 documented (temp file secrets)
- [ ] RISK-I-007 documented (memory dump)
- [ ] RISK-I-008 documented (path disclosure)
- [ ] RISK-I-009 documented (version info)
- [ ] RISK-I-010 documented (training data)
- [ ] All info disclosure risks have DREAD scores
- [ ] All info disclosure risks have mitigations
- [ ] LocalOnly mitigates I-001
- [ ] Secret redaction mitigates I-002, I-003
- [ ] Safe error handling mitigates I-004
- [ ] File permissions mitigate I-005
- [ ] Secure temp files mitigate I-006
- [ ] Path normalization mitigates I-008
- [ ] Info disclosure highest priority
- [ ] Info disclosure mitigations tested

### DoS and EoP Risks (20 items)

- [ ] All DoS risks documented (7+)
- [ ] All EoP risks documented (7+)
- [ ] All DoS risks have DREAD scores
- [ ] All EoP risks have DREAD scores
- [ ] All DoS risks have mitigations
- [ ] All EoP risks have mitigations
- [ ] Timeouts mitigate infinite loops
- [ ] File size limits mitigate large files
- [ ] Memory limits mitigate exhaustion
- [ ] Log rotation mitigates disk fill
- [ ] Regex timeouts mitigate ReDoS
- [ ] Process limits mitigate fork bombs
- [ ] Command whitelist mitigates E-001
- [ ] Output sanitization mitigates E-002
- [ ] Path validation mitigates E-003, E-004
- [ ] Safe YAML mitigates E-005
- [ ] Mode enforcement mitigates E-006
- [ ] EoP treated as high severity
- [ ] DoS mitigations tested
- [ ] EoP mitigations tested

### Documentation and Process (20 items)

- [ ] Risk register is complete
- [ ] Risk register is current
- [ ] Risk register is accessible
- [ ] Machine-readable format exists
- [ ] Human-readable format exists
- [ ] Review process documented
- [ ] Change tracking in place
- [ ] Last review date recorded
- [ ] Risk owners assigned
- [ ] Mitigation status tracked
- [ ] Quarterly review scheduled
- [ ] Release review required
- [ ] Metrics reported
- [ ] Risk trends tracked
- [ ] New risk process documented
- [ ] Deprecation process documented
- [ ] Export functionality works
- [ ] Search/filter works
- [ ] History preserved
- [ ] Training materials available

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| UT-003a-01 | Parse risk register YAML | Valid structure |
| UT-003a-02 | Validate DREAD score range | 1-10 enforced |
| UT-003a-03 | Calculate DREAD total | Average correct |
| UT-003a-04 | Classify severity from score | Correct classification |
| UT-003a-05 | Validate risk ID format | RISK-X-NNN enforced |
| UT-003a-06 | Validate STRIDE category | Valid category required |
| UT-003a-07 | Validate mitigation reference | Mitigation exists |
| UT-003a-08 | Detect duplicate risk IDs | Error on duplicate |
| UT-003a-09 | Validate risk dependencies | Dependencies exist |
| UT-003a-10 | Export to JSON | Valid JSON |
| UT-003a-11 | Export to Markdown | Valid Markdown |
| UT-003a-12 | Filter by category | Correct results |
| UT-003a-13 | Filter by severity | Correct results |
| UT-003a-14 | Search by keyword | Correct results |
| UT-003a-15 | Validate mitigation tests exist | Tests referenced |

### Integration Tests

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| IT-003a-01 | Load complete risk register | All risks loaded |
| IT-003a-02 | Verify mitigation code exists | Code paths valid |
| IT-003a-03 | Verify mitigation tests exist | Tests present |
| IT-003a-04 | Cross-reference risks and controls | Mappings valid |
| IT-003a-05 | CLI displays risks correctly | Formatted output |
| IT-003a-06 | CLI filters work | Correct filtering |
| IT-003a-07 | CLI export works | Valid export |
| IT-003a-08 | Risk register versioning | Version tracked |
| IT-003a-09 | Mitigation status tracking | Status updated |
| IT-003a-10 | All high-severity mitigations tested | Tests pass |

### End-to-End Tests

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| E2E-003a-01 | acode security risks | Risks displayed |
| E2E-003a-02 | acode security risks --category S | Spoofing risks |
| E2E-003a-03 | acode security risks --severity high | High risks |
| E2E-003a-04 | acode security risk RISK-I-001 | Detail displayed |
| E2E-003a-05 | acode security mitigations | Mitigations listed |
| E2E-003a-06 | acode security verify-mitigations | Tests run |
| E2E-003a-07 | acode security risks --export json | JSON output |
| E2E-003a-08 | LocalOnly blocks RISK-I-001 | Mitigation works |
| E2E-003a-09 | Secret redaction blocks RISK-I-002 | Mitigation works |
| E2E-003a-10 | Path validation blocks RISK-E-003 | Mitigation works |

### Performance / Benchmarks

| ID | Benchmark | Target | Measurement |
|----|-----------|--------|-------------|
| PERF-003a-01 | Load risk register | < 100ms | Stopwatch |
| PERF-003a-02 | Filter risks | < 10ms | Stopwatch |
| PERF-003a-03 | Export to JSON | < 50ms | Stopwatch |
| PERF-003a-04 | Search risks | < 20ms | Stopwatch |
| PERF-003a-05 | Verify all mitigations | < 5 minutes | CI timing |

### Regression / Impacted Areas

| Area | Impact | Regression Test |
|------|--------|-----------------|
| Security controls | All mitigations | Controls still effective |
| Risk documentation | Accuracy | Documentation matches code |
| CLI commands | Risk display | Commands work |
| Export | Format | Exports valid |

---

## User Verification Steps

### Scenario 1: View All Risks
1. Run `acode security risks`
2. **Verify:** All 40+ risks displayed
3. **Verify:** Categories shown
4. **Verify:** Severity shown

### Scenario 2: Filter by Category
1. Run `acode security risks --category information-disclosure`
2. **Verify:** Only I category risks shown
3. **Verify:** 10+ risks displayed

### Scenario 3: Filter by Severity
1. Run `acode security risks --severity high`
2. **Verify:** Only high-severity risks shown
3. **Verify:** RISK-I-001 included

### Scenario 4: View Risk Details
1. Run `acode security risk RISK-I-001`
2. **Verify:** Full description shown
3. **Verify:** DREAD scores shown
4. **Verify:** Mitigations listed

### Scenario 5: View Mitigations
1. Run `acode security mitigations`
2. **Verify:** All mitigations listed
3. **Verify:** Status shown

### Scenario 6: Verify Mitigations
1. Run `acode security verify-mitigations`
2. **Verify:** Tests run
3. **Verify:** Results displayed

### Scenario 7: Export Risk Register
1. Run `acode security risks --export json > risks.json`
2. **Verify:** File created
3. **Verify:** Valid JSON

### Scenario 8: RISK-I-001 Mitigation
1. Ensure LocalOnly mode
2. Attempt external LLM call
3. **Verify:** Call blocked
4. **Verify:** Mitigation effective

### Scenario 9: RISK-I-002 Mitigation
1. Process file with secret pattern
2. Check logs
3. **Verify:** Secret redacted

### Scenario 10: RISK-E-003 Mitigation
1. Attempt path traversal in config
2. Run validation
3. **Verify:** Validation fails
4. **Verify:** Mitigation effective

### Scenario 11: DREAD Score Calculation
1. View risk with known scores
2. Calculate average manually
3. **Verify:** Total matches calculation

### Scenario 12: Risk Documentation
1. Access risk documentation
2. **Verify:** All required fields present
3. **Verify:** Documentation current

---

## Implementation Prompt for Claude

### Objective

Create a comprehensive risk register documenting all identified security risks and their mitigations for Acode.

### File Structure

```
docs/
└── security/
    ├── risk-register.yaml       # Machine-readable risk data
    ├── risk-register.md         # Human-readable documentation
    ├── mitigations/
    │   ├── MIT-001-local-mode.md
    │   ├── MIT-002-secret-redaction.md
    │   └── ...
    └── scoring/
        └── dread-methodology.md
src/
├── Acode.Domain/
│   └── Security/
│       ├── Risk.cs
│       ├── RiskCategory.cs
│       ├── DreadScore.cs
│       ├── Mitigation.cs
│       └── Severity.cs
├── Acode.Application/
│   └── Security/
│       ├── IRiskRegister.cs
│       ├── RiskRegister.cs
│       └── MitigationVerifier.cs
└── Acode.Cli/
    └── Commands/
        └── SecurityCommands.cs
```

### Risk Register Schema (YAML)

```yaml
version: "1.0.0"
last_updated: "2025-01-03"
review_cycle: quarterly

risks:
  - id: RISK-I-001
    category: information_disclosure
    title: Source code exfiltration via LLM
    description: |
      In Burst mode, source code sent as context to external LLM
      could be stored, logged, or used for training.
    dread:
      damage: 9
      reproducibility: 10
      exploitability: 3
      affected_users: 10
      discoverability: 7
    severity: high
    mitigations:
      - MIT-001
      - MIT-002
      - MIT-003
    residual_risk: |
      In Burst mode with consent, code is sent externally.
    owner: security-team
    status: active
    created: "2025-01-03"
    last_review: "2025-01-03"

mitigations:
  - id: MIT-001
    title: LocalOnly mode default
    description: External LLM disabled by default
    implementation: ConfigDefaults.Mode = "local-only"
    verification: unit_test:test_default_mode_local
    status: implemented
```

### Domain Models

```csharp
public enum RiskCategory { Spoofing, Tampering, Repudiation, InformationDisclosure, DenialOfService, ElevationOfPrivilege }
public enum Severity { Low, Medium, High, Critical }

public sealed record DreadScore(int Damage, int Reproducibility, int Exploitability, int AffectedUsers, int Discoverability)
{
    public double Total => (Damage + Reproducibility + Exploitability + AffectedUsers + Discoverability) / 5.0;
    public Severity Severity => Total switch { < 4 => Severity.Low, < 7 => Severity.Medium, _ => Severity.High };
}

public sealed record Risk(string Id, RiskCategory Category, string Title, string Description, DreadScore Dread, IReadOnlyList<string> MitigationIds);
```

### Validation Checklist Before Merge

- [ ] 40+ risks documented
- [ ] All STRIDE categories covered
- [ ] All risks have DREAD scores
- [ ] All risks have mitigations
- [ ] Risk register in YAML format
- [ ] Risk register in Markdown format
- [ ] CLI commands implemented
- [ ] Export functionality works
- [ ] Mitigation tests exist
- [ ] All tests passing
- [ ] Documentation reviewed
- [ ] Security team approved

### Rollout Plan

1. **Phase 1: Framework** — Domain models and schema
2. **Phase 2: Risk Enumeration** — Document all risks
3. **Phase 3: Mitigation Mapping** — Link to controls
4. **Phase 4: CLI** — Display and export commands
5. **Phase 5: Verification** — Mitigation test integration

---

**END OF TASK 003.a**
