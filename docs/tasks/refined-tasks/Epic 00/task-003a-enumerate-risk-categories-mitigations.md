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

#### Unit Test Code Examples

```csharp
// DreadScoreTests.cs
[TestClass]
public class DreadScoreTests
{
    [TestMethod]
    public void Total_Should_Be_Average_Of_Components()
    {
        // Arrange
        var score = new DreadScore(
            Damage: 8,
            Reproducibility: 6,
            Exploitability: 4,
            AffectedUsers: 10,
            Discoverability: 2);

        // Act
        var total = score.Total;

        // Assert
        Assert.AreEqual(6.0, total); // (8+6+4+10+2) / 5 = 6.0
    }

    [TestMethod]
    [DataRow(1, 1, 1, 1, 1, Severity.Low)]      // Total = 1.0
    [DataRow(3, 3, 3, 3, 3, Severity.Low)]      // Total = 3.0
    [DataRow(4, 4, 4, 4, 4, Severity.Medium)]   // Total = 4.0
    [DataRow(6, 6, 6, 6, 6, Severity.Medium)]   // Total = 6.0
    [DataRow(7, 7, 7, 7, 7, Severity.High)]     // Total = 7.0
    [DataRow(10, 10, 10, 10, 10, Severity.High)] // Total = 10.0
    public void Severity_Should_Be_Classified_Correctly(
        int d, int r, int e, int a, int disc, Severity expected)
    {
        // Arrange
        var score = new DreadScore(d, r, e, a, disc);

        // Act & Assert
        Assert.AreEqual(expected, score.Severity);
    }

    [TestMethod]
    [DataRow(0, 5, 5, 5, 5)]  // Damage below 1
    [DataRow(11, 5, 5, 5, 5)] // Damage above 10
    [DataRow(5, -1, 5, 5, 5)] // Reproducibility negative
    public void Should_Reject_Invalid_Score_Values(int d, int r, int e, int a, int disc)
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            new DreadScore(d, r, e, a, disc));
    }
}

// RiskIdValidatorTests.cs
[TestClass]
public class RiskIdValidatorTests
{
    private readonly RiskIdValidator _validator = new();

    [TestMethod]
    [DataRow("RISK-S-001", true)]
    [DataRow("RISK-T-001", true)]
    [DataRow("RISK-R-001", true)]
    [DataRow("RISK-I-001", true)]
    [DataRow("RISK-D-001", true)]
    [DataRow("RISK-E-001", true)]
    [DataRow("RISK-I-100", true)]
    [DataRow("RISK-I-999", true)]
    public void Should_Accept_Valid_Risk_IDs(string riskId, bool expected)
    {
        // Act
        var result = _validator.IsValid(riskId);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("RISK-X-001")]   // Invalid category
    [DataRow("RISK-S-01")]    // Too few digits
    [DataRow("RISK-S-1000")]  // Too many digits
    [DataRow("risk-s-001")]   // Lowercase
    [DataRow("RISK_S_001")]   // Wrong delimiter
    [DataRow("RISK-001")]     // Missing category
    [DataRow("")]             // Empty
    [DataRow(null)]           // Null
    public void Should_Reject_Invalid_Risk_IDs(string riskId)
    {
        // Act
        var result = _validator.IsValid(riskId);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ExtractCategory_Should_Return_Correct_Category()
    {
        // Test cases
        var testCases = new Dictionary<string, RiskCategory>
        {
            ["RISK-S-001"] = RiskCategory.Spoofing,
            ["RISK-T-001"] = RiskCategory.Tampering,
            ["RISK-R-001"] = RiskCategory.Repudiation,
            ["RISK-I-001"] = RiskCategory.InformationDisclosure,
            ["RISK-D-001"] = RiskCategory.DenialOfService,
            ["RISK-E-001"] = RiskCategory.ElevationOfPrivilege
        };

        foreach (var (riskId, expectedCategory) in testCases)
        {
            var category = _validator.ExtractCategory(riskId);
            Assert.AreEqual(expectedCategory, category, $"Failed for {riskId}");
        }
    }
}

// RiskRegisterLoaderTests.cs
[TestClass]
public class RiskRegisterLoaderTests
{
    [TestMethod]
    public void Should_Parse_Valid_Risk_Register_YAML()
    {
        // Arrange
        var yaml = """
            version: "1.0.0"
            last_updated: "2025-01-03"
            risks:
              - id: RISK-I-001
                category: information_disclosure
                title: Source code exfiltration via LLM
                description: Code sent to external LLM
                dread:
                  damage: 9
                  reproducibility: 10
                  exploitability: 3
                  affected_users: 10
                  discoverability: 7
                severity: high
                mitigations:
                  - MIT-001
                owner: security-team
                status: active
            """;
        var loader = new RiskRegisterLoader();

        // Act
        var register = loader.Parse(yaml);

        // Assert
        Assert.AreEqual("1.0.0", register.Version);
        Assert.AreEqual(1, register.Risks.Count);
        
        var risk = register.Risks[0];
        Assert.AreEqual("RISK-I-001", risk.Id);
        Assert.AreEqual(RiskCategory.InformationDisclosure, risk.Category);
        Assert.AreEqual(7.8, risk.Dread.Total, 0.01);
        Assert.AreEqual(Severity.High, risk.Dread.Severity);
    }

    [TestMethod]
    public void Should_Detect_Duplicate_Risk_IDs()
    {
        // Arrange
        var yaml = """
            version: "1.0.0"
            risks:
              - id: RISK-I-001
                category: information_disclosure
                title: Risk 1
              - id: RISK-I-001
                category: information_disclosure
                title: Risk 2 with same ID
            """;
        var loader = new RiskRegisterLoader();

        // Act & Assert
        var ex = Assert.ThrowsException<RiskRegisterValidationException>(
            () => loader.Parse(yaml));
        Assert.IsTrue(ex.Message.Contains("RISK-I-001"));
        Assert.IsTrue(ex.Message.Contains("duplicate"));
    }

    [TestMethod]
    public void Should_Validate_Mitigation_References_Exist()
    {
        // Arrange
        var yaml = """
            version: "1.0.0"
            risks:
              - id: RISK-I-001
                mitigations:
                  - MIT-001
                  - MIT-NONEXISTENT
            mitigations:
              - id: MIT-001
                title: LocalOnly mode
            """;
        var loader = new RiskRegisterLoader();

        // Act & Assert
        var ex = Assert.ThrowsException<RiskRegisterValidationException>(
            () => loader.Parse(yaml));
        Assert.IsTrue(ex.Message.Contains("MIT-NONEXISTENT"));
        Assert.IsTrue(ex.Message.Contains("not found"));
    }
}

// RiskFilterTests.cs
[TestClass]
public class RiskFilterTests
{
    private readonly List<Risk> _testRisks;
    private readonly RiskFilter _filter;

    public RiskFilterTests()
    {
        _testRisks = new List<Risk>
        {
            CreateRisk("RISK-I-001", RiskCategory.InformationDisclosure, 8.0),
            CreateRisk("RISK-I-002", RiskCategory.InformationDisclosure, 5.0),
            CreateRisk("RISK-S-001", RiskCategory.Spoofing, 3.0),
            CreateRisk("RISK-E-001", RiskCategory.ElevationOfPrivilege, 9.0),
        };
        _filter = new RiskFilter();
    }

    [TestMethod]
    public void Should_Filter_By_Category()
    {
        // Act
        var result = _filter.ByCategory(_testRisks, RiskCategory.InformationDisclosure);

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.All(r => r.Category == RiskCategory.InformationDisclosure));
    }

    [TestMethod]
    public void Should_Filter_By_Severity()
    {
        // Act
        var highSeverity = _filter.BySeverity(_testRisks, Severity.High);

        // Assert
        Assert.AreEqual(2, highSeverity.Count);
        Assert.IsTrue(highSeverity.All(r => r.Dread.Severity == Severity.High));
    }

    [TestMethod]
    public void Should_Search_By_Keyword()
    {
        // Arrange
        var risks = new List<Risk>
        {
            CreateRisk("RISK-I-001", title: "Source code exfiltration"),
            CreateRisk("RISK-I-002", title: "Secrets in logs"),
            CreateRisk("RISK-T-001", title: "Config tampering")
        };

        // Act
        var result = _filter.Search(risks, "source code");

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("RISK-I-001", result[0].Id);
    }

    private static Risk CreateRisk(
        string id, 
        RiskCategory category = RiskCategory.InformationDisclosure,
        double score = 5.0,
        string title = "Test Risk")
    {
        var s = (int)score;
        return new Risk(id, category, title, "Description", 
            new DreadScore(s, s, s, s, s), Array.Empty<string>());
    }
}

// MitigationVerifierTests.cs
[TestClass]
public class MitigationVerifierTests
{
    [TestMethod]
    public void Should_Report_Implemented_Mitigations()
    {
        // Arrange
        var mitigations = new List<Mitigation>
        {
            new("MIT-001", "LocalOnly mode", "Blocks external LLM", 
                MitigationStatus.Implemented, "test_default_mode_local"),
            new("MIT-002", "Secret redaction", "Redacts secrets", 
                MitigationStatus.Implemented, "test_secret_patterns")
        };
        var testResults = new Dictionary<string, bool>
        {
            ["test_default_mode_local"] = true,
            ["test_secret_patterns"] = true
        };
        var verifier = new MitigationVerifier(mitigations, testResults);

        // Act
        var report = verifier.Verify();

        // Assert
        Assert.IsTrue(report.AllPassed);
        Assert.AreEqual(2, report.VerifiedCount);
        Assert.AreEqual(0, report.FailedCount);
    }

    [TestMethod]
    public void Should_Report_Failed_Mitigations()
    {
        // Arrange
        var mitigations = new List<Mitigation>
        {
            new("MIT-001", "LocalOnly mode", "Blocks external LLM", 
                MitigationStatus.Implemented, "test_default_mode_local"),
        };
        var testResults = new Dictionary<string, bool>
        {
            ["test_default_mode_local"] = false  // Test failed
        };
        var verifier = new MitigationVerifier(mitigations, testResults);

        // Act
        var report = verifier.Verify();

        // Assert
        Assert.IsFalse(report.AllPassed);
        Assert.AreEqual(0, report.VerifiedCount);
        Assert.AreEqual(1, report.FailedCount);
        Assert.AreEqual("MIT-001", report.FailedMitigations[0].Id);
    }

    [TestMethod]
    public void Should_Report_Missing_Verification_Tests()
    {
        // Arrange
        var mitigations = new List<Mitigation>
        {
            new("MIT-001", "LocalOnly mode", "Blocks external LLM", 
                MitigationStatus.Implemented, "test_that_doesnt_exist"),
        };
        var testResults = new Dictionary<string, bool>(); // No test results
        var verifier = new MitigationVerifier(mitigations, testResults);

        // Act
        var report = verifier.Verify();

        // Assert
        Assert.IsFalse(report.AllPassed);
        Assert.AreEqual(1, report.MissingTestsCount);
    }
}
```

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

#### Integration Test Code Examples

```csharp
// RiskRegisterIntegrationTests.cs
[TestClass]
[TestCategory("Integration")]
public class RiskRegisterIntegrationTests
{
    private IRiskRegister _riskRegister;
    private IServiceProvider _services;

    [TestInitialize]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRiskRegistry();
        _services = services.BuildServiceProvider();
        _riskRegister = _services.GetRequiredService<IRiskRegister>();
    }

    [TestMethod]
    public async Task Should_Load_Complete_Risk_Register_From_File()
    {
        // Act
        var risks = await _riskRegister.GetAllRisksAsync();

        // Assert
        Assert.IsTrue(risks.Count >= 40, $"Expected 40+ risks, found {risks.Count}");
        
        // Verify all STRIDE categories are covered
        var categories = risks.Select(r => r.Category).Distinct().ToList();
        Assert.AreEqual(6, categories.Count, "All STRIDE categories must be represented");
        Assert.IsTrue(categories.Contains(RiskCategory.Spoofing));
        Assert.IsTrue(categories.Contains(RiskCategory.Tampering));
        Assert.IsTrue(categories.Contains(RiskCategory.Repudiation));
        Assert.IsTrue(categories.Contains(RiskCategory.InformationDisclosure));
        Assert.IsTrue(categories.Contains(RiskCategory.DenialOfService));
        Assert.IsTrue(categories.Contains(RiskCategory.ElevationOfPrivilege));
    }

    [TestMethod]
    public async Task Should_Have_Minimum_Risks_Per_Category()
    {
        // Arrange
        var expectedMinimums = new Dictionary<RiskCategory, int>
        {
            [RiskCategory.Spoofing] = 6,
            [RiskCategory.Tampering] = 7,
            [RiskCategory.Repudiation] = 5,
            [RiskCategory.InformationDisclosure] = 10,
            [RiskCategory.DenialOfService] = 7,
            [RiskCategory.ElevationOfPrivilege] = 7
        };

        // Act
        var risks = await _riskRegister.GetAllRisksAsync();
        var byCategory = risks.GroupBy(r => r.Category).ToDictionary(g => g.Key, g => g.Count());

        // Assert
        foreach (var (category, minimum) in expectedMinimums)
        {
            Assert.IsTrue(
                byCategory.GetValueOrDefault(category, 0) >= minimum,
                $"{category} should have at least {minimum} risks, found {byCategory.GetValueOrDefault(category, 0)}");
        }
    }

    [TestMethod]
    public async Task Should_Verify_All_Mitigation_Code_Paths_Exist()
    {
        // Arrange
        var mitigations = await _riskRegister.GetAllMitigationsAsync();
        var codePathValidator = _services.GetRequiredService<ICodePathValidator>();

        // Act & Assert
        foreach (var mitigation in mitigations.Where(m => m.Status == MitigationStatus.Implemented))
        {
            var exists = await codePathValidator.ValidateAsync(mitigation.Implementation);
            Assert.IsTrue(exists, 
                $"Mitigation {mitigation.Id} references non-existent code path: {mitigation.Implementation}");
        }
    }

    [TestMethod]
    public async Task Should_Verify_All_Mitigation_Tests_Exist()
    {
        // Arrange
        var mitigations = await _riskRegister.GetAllMitigationsAsync();
        var testDiscovery = _services.GetRequiredService<ITestDiscoveryService>();

        // Act & Assert
        foreach (var mitigation in mitigations.Where(m => m.Status == MitigationStatus.Implemented))
        {
            if (string.IsNullOrEmpty(mitigation.VerificationTest))
            {
                Assert.Fail($"Mitigation {mitigation.Id} has no verification test defined");
            }

            var testExists = await testDiscovery.TestExistsAsync(mitigation.VerificationTest);
            Assert.IsTrue(testExists,
                $"Mitigation {mitigation.Id} references non-existent test: {mitigation.VerificationTest}");
        }
    }

    [TestMethod]
    public async Task Should_Cross_Reference_Risks_And_Mitigations()
    {
        // Act
        var risks = await _riskRegister.GetAllRisksAsync();
        var mitigations = await _riskRegister.GetAllMitigationsAsync();
        var mitigationIds = mitigations.Select(m => m.Id).ToHashSet();

        // Assert
        foreach (var risk in risks)
        {
            // Every risk must have at least one mitigation
            Assert.IsTrue(risk.MitigationIds.Count > 0,
                $"Risk {risk.Id} has no mitigations defined");

            // All mitigation references must be valid
            foreach (var mitId in risk.MitigationIds)
            {
                Assert.IsTrue(mitigationIds.Contains(mitId),
                    $"Risk {risk.Id} references non-existent mitigation: {mitId}");
            }
        }

        // High-severity risks should have multiple mitigations (defense in depth)
        var highSeverityRisks = risks.Where(r => r.Dread.Severity == Severity.High);
        foreach (var risk in highSeverityRisks)
        {
            Assert.IsTrue(risk.MitigationIds.Count >= 2,
                $"High-severity risk {risk.Id} should have multiple mitigations (defense in depth)");
        }
    }

    [TestMethod]
    public async Task All_High_Severity_Mitigation_Tests_Should_Pass()
    {
        // Arrange
        var risks = await _riskRegister.GetAllRisksAsync();
        var highSeverityRiskIds = risks
            .Where(r => r.Dread.Severity == Severity.High)
            .SelectMany(r => r.MitigationIds)
            .Distinct()
            .ToList();

        var mitigations = await _riskRegister.GetAllMitigationsAsync();
        var testRunner = _services.GetRequiredService<ITestRunner>();

        // Act & Assert
        foreach (var mitId in highSeverityRiskIds)
        {
            var mitigation = mitigations.First(m => m.Id == mitId);
            if (mitigation.Status != MitigationStatus.Implemented)
            {
                continue; // Skip non-implemented mitigations
            }

            var result = await testRunner.RunTestAsync(mitigation.VerificationTest);
            Assert.IsTrue(result.Passed,
                $"Mitigation test for {mitigation.Id} failed: {result.ErrorMessage}");
        }
    }
}

// SecurityCommandsIntegrationTests.cs
[TestClass]
[TestCategory("Integration")]
public class SecurityCommandsIntegrationTests
{
    private ICommandRunner _commandRunner;

    [TestInitialize]
    public void Setup()
    {
        _commandRunner = new TestCommandRunner();
    }

    [TestMethod]
    public async Task SecurityRisks_Should_Display_All_Risks()
    {
        // Act
        var result = await _commandRunner.RunAsync("acode", "security", "risks");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        Assert.IsTrue(result.StandardOutput.Contains("RISK-I-001"));
        Assert.IsTrue(result.StandardOutput.Contains("RISK-S-001"));
        Assert.IsTrue(result.StandardOutput.Contains("RISK-E-001"));
    }

    [TestMethod]
    public async Task SecurityRisks_Filter_By_Category_Should_Work()
    {
        // Act
        var result = await _commandRunner.RunAsync(
            "acode", "security", "risks", "--category", "information-disclosure");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        Assert.IsTrue(result.StandardOutput.Contains("RISK-I-001"));
        Assert.IsFalse(result.StandardOutput.Contains("RISK-S-001"));
        Assert.IsFalse(result.StandardOutput.Contains("RISK-E-001"));
    }

    [TestMethod]
    public async Task SecurityRisks_Filter_By_Severity_Should_Work()
    {
        // Act
        var result = await _commandRunner.RunAsync(
            "acode", "security", "risks", "--severity", "high");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        // All displayed risks should be high severity
        var lines = result.StandardOutput.Split('\n')
            .Where(l => l.Contains("RISK-"));
        foreach (var line in lines)
        {
            Assert.IsTrue(line.Contains("High") || line.Contains("high"));
        }
    }

    [TestMethod]
    public async Task SecurityRisks_Export_JSON_Should_Produce_Valid_JSON()
    {
        // Act
        var result = await _commandRunner.RunAsync(
            "acode", "security", "risks", "--export", "json");

        // Assert
        Assert.AreEqual(0, result.ExitCode);
        
        // Should be valid JSON
        var json = JsonDocument.Parse(result.StandardOutput);
        Assert.IsNotNull(json);
        
        // Should have risks array
        Assert.IsTrue(json.RootElement.TryGetProperty("risks", out var risks));
        Assert.IsTrue(risks.GetArrayLength() >= 40);
    }
}
```

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

#### E2E Test Scenarios

```gherkin
Feature: Risk Register CLI

  Background:
    Given Acode is installed and configured
    And the risk register is populated with 40+ risks

  @Critical
  Scenario: View All Security Risks
    When I run "acode security risks"
    Then the exit code MUST be 0
    And the output MUST contain at least 40 risk entries
    And each risk MUST display ID, title, and severity
    And risks MUST be grouped by STRIDE category
    
  Scenario: Filter Risks by Information Disclosure Category
    When I run "acode security risks --category information-disclosure"
    Then the exit code MUST be 0
    And all displayed risks MUST have ID starting with "RISK-I-"
    And there MUST be at least 10 risks displayed
    And no risks from other categories MUST appear

  Scenario: Filter Risks by High Severity
    When I run "acode security risks --severity high"
    Then the exit code MUST be 0
    And all displayed risks MUST have severity "High"
    And RISK-I-001 MUST be included (Source code exfiltration)
    And RISK-E-002 MUST be included (Prompt injection)

  @Critical
  Scenario: View Individual Risk Details
    When I run "acode security risk RISK-I-001"
    Then the exit code MUST be 0
    And the output MUST contain:
      | Field          | Content                     |
      | ID             | RISK-I-001                  |
      | Category       | Information Disclosure      |
      | Title          | Source code exfiltration    |
      | DREAD Damage   | 9                           |
      | Severity       | High                        |
    And the output MUST list all mitigations
    And the output MUST show residual risk

  Scenario: View All Mitigations
    When I run "acode security mitigations"
    Then the exit code MUST be 0
    And the output MUST list all mitigations with IDs
    And each mitigation MUST show status (Implemented/Pending)
    And implemented mitigations MUST show verification test

  @Critical
  Scenario: Verify Mitigations Execute Tests
    When I run "acode security verify-mitigations"
    Then the exit code MUST be 0 if all tests pass
    And the output MUST show test execution progress
    And the output MUST summarize passed/failed counts
    And any failures MUST show the mitigation ID and test name

  Scenario: Export Risk Register to JSON
    When I run "acode security risks --export json"
    Then the exit code MUST be 0
    And the output MUST be valid JSON
    And the JSON MUST contain a "risks" array
    And the JSON MUST contain a "mitigations" array
    And each risk MUST have all required fields

  @Critical
  Scenario: LocalOnly Mode Mitigates RISK-I-001
    Given I am in LocalOnly operating mode
    And I have a request that would send code to external LLM
    When the request is processed
    Then the external LLM call MUST be blocked
    And an error MUST be logged with risk reference RISK-I-001
    And the mitigation MIT-001 MUST be credited in the log

  @Critical
  Scenario: Secret Redaction Mitigates RISK-I-002
    Given I have a file containing "api_key=sk-secret123"
    When the file is processed and logged
    Then the log entry MUST show "api_key=[REDACTED]"
    And the actual secret value MUST NOT appear in any log
    And the mitigation MIT-002 MUST be active

  @Critical
  Scenario: Path Validation Mitigates RISK-E-003
    Given I have a config with path "../../../etc/passwd"
    When the config is validated
    Then validation MUST fail with path traversal error
    And the error MUST reference RISK-E-003
    And no file access MUST occur outside allowed paths
```

```csharp
// RiskMitigationE2ETests.cs
[TestClass]
[TestCategory("E2E")]
public class RiskMitigationE2ETests
{
    [TestMethod]
    public async Task RISK_I_001_LocalOnly_Should_Block_External_LLM()
    {
        // Arrange
        var config = new AgentConfig
        {
            Mode = OperatingMode.LocalOnly
        };
        var logCapture = new LogCapture();
        var agent = new AcodeAgent(config, logCapture);

        // Act
        var exception = await Assert.ThrowsExceptionAsync<ExternalLlmBlockedException>(
            () => agent.SendToExternalLlmAsync("code content"));

        // Assert
        Assert.AreEqual("ACODE-SEC-001", exception.ErrorCode);
        Assert.IsTrue(exception.Message.Contains("RISK-I-001"));
        Assert.IsTrue(logCapture.Entries.Any(e => 
            e.Contains("Blocked by MIT-001") && e.Contains("LocalOnly mode")));
    }

    [TestMethod]
    public async Task RISK_I_002_Secrets_Should_Be_Redacted_In_Logs()
    {
        // Arrange
        var secrets = new[]
        {
            "api_key=sk-test123456",
            "password=SuperSecret!",
            "token=ghp_abc123def456",
            "AWS_SECRET_ACCESS_KEY=wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
        };
        var logCapture = new LogCapture();
        var processor = new FileProcessor(logCapture);

        // Act
        foreach (var secret in secrets)
        {
            await processor.ProcessContentAsync(secret);
        }

        // Assert
        foreach (var logEntry in logCapture.Entries)
        {
            Assert.IsFalse(logEntry.Contains("sk-test123456"), "API key not redacted");
            Assert.IsFalse(logEntry.Contains("SuperSecret!"), "Password not redacted");
            Assert.IsFalse(logEntry.Contains("ghp_abc123def456"), "Token not redacted");
            Assert.IsFalse(logEntry.Contains("wJalrXUtnFEMI"), "AWS key not redacted");
            
            // Should contain redaction markers
            Assert.IsTrue(logEntry.Contains("[REDACTED]") || !logEntry.Contains("="));
        }
    }

    [TestMethod]
    public async Task RISK_E_003_Path_Traversal_Should_Be_Blocked()
    {
        // Arrange
        var maliciousPaths = new[]
        {
            "../../../etc/passwd",
            "..\\..\\..\\Windows\\System32\\config\\SAM",
            "/etc/shadow",
            "C:\\Windows\\System32\\config\\SAM",
            "~/../../root/.ssh/id_rsa"
        };
        var pathValidator = new PathValidator(allowedRoot: "/home/user/project");

        // Act & Assert
        foreach (var path in maliciousPaths)
        {
            var result = pathValidator.Validate(path);
            
            Assert.IsFalse(result.IsValid, $"Path should be rejected: {path}");
            Assert.IsTrue(result.ErrorCode == "ACODE-SEC-003" || 
                          result.ErrorCode == "PATH_TRAVERSAL");
            Assert.IsTrue(result.Reason.Contains("traversal") || 
                          result.Reason.Contains("outside allowed"));
        }
    }

    [TestMethod]
    public async Task RISK_D_001_Timeout_Should_Prevent_Infinite_Loop()
    {
        // Arrange
        var config = new AgentConfig
        {
            Timeouts = new TimeoutConfig
            {
                LlmResponseTimeoutSeconds = 5
            }
        };
        var mockLlm = new Mock<ILlmProvider>();
        mockLlm.Setup(m => m.StreamAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .Returns(InfiniteStream()); // Never-ending stream

        var agent = new AcodeAgent(config, mockLlm.Object);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var exception = await Assert.ThrowsExceptionAsync<TimeoutException>(
            () => agent.CompleteAsync("test prompt"));
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(stopwatch.Elapsed.TotalSeconds < 10, 
            "Should timeout within configured limit");
        Assert.IsTrue(exception.Message.Contains("RISK-D-001"));
    }
}
```

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
2. **Verify:** All 40+ risks displayed in formatted table
3. **Verify:** Categories shown with correct color coding (S=yellow, T=orange, R=blue, I=red, D=purple, E=magenta)
4. **Verify:** Severity shown with indicators (🔴 High, 🟡 Medium, 🟢 Low)
5. **Verify:** Output is paginated if more than 25 risks
6. **Verify:** Footer shows total count and category breakdown

### Scenario 2: Filter by Category
1. Run `acode security risks --category information-disclosure`
2. **Verify:** Only I category risks shown (RISK-I-*)
3. **Verify:** 10+ risks displayed
4. **Verify:** No risks from other categories appear in output
5. **Verify:** Category header confirms filter applied
6. **Verify:** Count shows "10 of 40+ risks matching filter"

### Scenario 3: Filter by Severity
1. Run `acode security risks --severity high`
2. **Verify:** Only high-severity risks shown (score 7.0+)
3. **Verify:** RISK-I-001 included (Source code exfiltration)
4. **Verify:** RISK-E-002 included (Prompt injection)
5. **Verify:** No medium or low severity risks appear
6. **Verify:** All displayed risks have 🔴 indicator

### Scenario 4: View Risk Details
1. Run `acode security risk RISK-I-001`
2. **Verify:** Full description shown with multi-line formatting
3. **Verify:** DREAD scores shown in table format:
   - Damage: 9/10
   - Reproducibility: 10/10
   - Exploitability: 3/10
   - Affected Users: 10/10
   - Discoverability: 7/10
   - Total: 7.8 (High)
4. **Verify:** All mitigations listed with status indicators
5. **Verify:** Residual risk section shown with acceptance status
6. **Verify:** Owner and last review date shown
7. **Verify:** Related risks shown (if any)

### Scenario 5: View Mitigations
1. Run `acode security mitigations`
2. **Verify:** All mitigations listed with unique IDs (MIT-001, MIT-002, ...)
3. **Verify:** Status shown: ✅ Implemented, 🔄 In Progress, ⏳ Pending
4. **Verify:** Each mitigation shows verification test name
5. **Verify:** Count of risks mitigated by each control shown
6. **Verify:** Can filter by status: `--status implemented`

### Scenario 6: Verify Mitigations
1. Run `acode security verify-mitigations`
2. **Verify:** Progress bar shows test execution (e.g., "Running tests... 15/23")
3. **Verify:** Each test result displayed as it completes (✓ or ✗)
4. **Verify:** Summary shows: "22 passed, 1 failed, 0 skipped"
5. **Verify:** Failed mitigations show test name and error message
6. **Verify:** Exit code is 0 if all pass, 1 if any fail
7. **Verify:** Total execution time displayed

### Scenario 7: Export Risk Register
1. Run `acode security risks --export json > risks.json`
2. **Verify:** File created with valid JSON content
3. **Verify:** JSON has `version`, `last_updated`, `risks`, `mitigations` keys
4. **Verify:** Each risk has all required fields (id, category, title, dread, mitigations)
5. **Verify:** DREAD scores are numeric, not strings
6. **Verify:** Can re-import: `acode security risks --import risks.json --validate`

### Scenario 8: RISK-I-001 Mitigation Effectiveness
1. Ensure LocalOnly mode: `acode config get mode` shows "local-only"
2. Create a request that would send code externally
3. Attempt to process the request
4. **Verify:** Request is blocked with error message
5. **Verify:** Error references RISK-I-001 and MIT-001
6. **Verify:** Log shows: `[BLOCKED] External LLM request denied by LocalOnly mode`
7. **Verify:** No network traffic sent (verify with packet capture if needed)

### Scenario 9: RISK-I-002 Mitigation Effectiveness
1. Create a file containing: `api_key=sk-secret123456789`
2. Process the file with logging enabled: `acode process --verbose file.txt`
3. Check the log output
4. **Verify:** Log shows `api_key=[REDACTED]` not the actual key
5. **Verify:** The actual value `sk-secret123456789` does NOT appear anywhere in logs
6. **Verify:** Processing continues normally after redaction

### Scenario 10: RISK-E-003 Mitigation Effectiveness
1. Create config with path traversal: `target_path: "../../../etc/passwd"`
2. Run validation: `acode config validate`
3. **Verify:** Validation fails with path traversal error
4. **Verify:** Error message: "Path traversal detected: '../../../etc/passwd'"
5. **Verify:** Error code is ACODE-SEC-003
6. **Verify:** No file access occurred (no read attempt on /etc/passwd)

### Scenario 11: DREAD Score Calculation Verification
1. View risk with known manual calculation: `acode security risk RISK-I-001`
2. DREAD scores: D=9, R=10, E=3, A=10, D=7
3. Calculate manually: (9+10+3+10+7) / 5 = 7.8
4. **Verify:** Displayed total matches 7.8
5. **Verify:** Severity correctly shows "High" (7.0+ threshold)

### Scenario 12: Risk Documentation Currency
1. Access risk documentation in repo: `docs/security/risk-register.md`
2. **Verify:** Last review date is within past quarter
3. **Verify:** All required fields present for each risk
4. **Verify:** Documentation matches CLI output exactly
5. **Verify:** Version number matches current release

### Scenario 13: New Risk Addition Workflow
1. Open security issue for new risk
2. Assign STRIDE category and DREAD scores
3. Run `acode security risks --pending` 
4. **Verify:** New risk appears in pending list
5. After approval, run `acode security risks`
6. **Verify:** New risk appears in main list with new ID

### Scenario 14: Mitigation Coverage Report
1. Run `acode security coverage`
2. **Verify:** Report shows percentage of risks with mitigations (target: 100%)
3. **Verify:** Report shows percentage of mitigations with tests (target: 100%)
4. **Verify:** Report shows high-severity risks with single mitigation (should be 0)
5. **Verify:** Defense-in-depth metric shown (avg mitigations per high-severity risk)

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
    │   ├── MIT-003-path-validation.md
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
│       ├── MitigationStatus.cs
│       ├── Severity.cs
│       └── RiskId.cs
├── Acode.Application/
│   └── Security/
│       ├── IRiskRegister.cs
│       ├── RiskRegister.cs
│       ├── IRiskFilter.cs
│       ├── RiskFilter.cs
│       ├── IMitigationVerifier.cs
│       ├── MitigationVerifier.cs
│       └── RiskRegisterLoader.cs
├── Acode.Infrastructure/
│   └── Security/
│       ├── YamlRiskRegisterRepository.cs
│       └── RiskRegisterExporter.cs
└── Acode.Cli/
    └── Commands/
        ├── SecurityCommands.cs
        ├── RisksCommand.cs
        ├── RiskDetailCommand.cs
        ├── MitigationsCommand.cs
        └── VerifyMitigationsCommand.cs
```

### Interface Contracts

```csharp
// IRiskRegister.cs - Core risk register interface
namespace Acode.Application.Security;

public interface IRiskRegister
{
    /// <summary>
    /// Gets all risks in the register.
    /// </summary>
    Task<IReadOnlyList<Risk>> GetAllRisksAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a specific risk by ID.
    /// </summary>
    /// <exception cref="RiskNotFoundException">If risk ID does not exist.</exception>
    Task<Risk> GetRiskAsync(RiskId id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets risks filtered by category.
    /// </summary>
    Task<IReadOnlyList<Risk>> GetRisksByCategoryAsync(
        RiskCategory category, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets risks filtered by severity.
    /// </summary>
    Task<IReadOnlyList<Risk>> GetRisksBySeverityAsync(
        Severity severity, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Searches risks by keyword in title or description.
    /// </summary>
    Task<IReadOnlyList<Risk>> SearchRisksAsync(
        string keyword, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all mitigations in the register.
    /// </summary>
    Task<IReadOnlyList<Mitigation>> GetAllMitigationsAsync(
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets mitigations for a specific risk.
    /// </summary>
    Task<IReadOnlyList<Mitigation>> GetMitigationsForRiskAsync(
        RiskId riskId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the version of the risk register.
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// Gets the last update date.
    /// </summary>
    DateTimeOffset LastUpdated { get; }
}

// IMitigationVerifier.cs - Mitigation verification interface
public interface IMitigationVerifier
{
    /// <summary>
    /// Verifies all mitigations by running their associated tests.
    /// </summary>
    Task<MitigationVerificationReport> VerifyAllAsync(
        IProgress<MitigationVerificationProgress>? progress = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verifies a specific mitigation.
    /// </summary>
    Task<MitigationVerificationResult> VerifyAsync(
        MitigationId id, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verifies all mitigations for a specific risk.
    /// </summary>
    Task<MitigationVerificationReport> VerifyForRiskAsync(
        RiskId riskId, 
        CancellationToken cancellationToken = default);
}

// IRiskRegisterExporter.cs - Export interface
public interface IRiskRegisterExporter
{
    /// <summary>
    /// Exports risk register to specified format.
    /// </summary>
    Task<string> ExportAsync(
        ExportFormat format, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Exports to file.
    /// </summary>
    Task ExportToFileAsync(
        string filePath, 
        ExportFormat format, 
        CancellationToken cancellationToken = default);
}

public enum ExportFormat { Json, Yaml, Markdown, Csv }
```

### Domain Models

```csharp
// Risk.cs
namespace Acode.Domain.Security;

public sealed record Risk
{
    public required RiskId Id { get; init; }
    public required RiskCategory Category { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required DreadScore Dread { get; init; }
    public required IReadOnlyList<MitigationId> MitigationIds { get; init; }
    public string? ResidualRisk { get; init; }
    public required string Owner { get; init; }
    public required RiskStatus Status { get; init; }
    public required DateTimeOffset Created { get; init; }
    public required DateTimeOffset LastReview { get; init; }
    
    public Severity Severity => Dread.Severity;
}

// RiskId.cs - Value object for risk IDs
public readonly struct RiskId : IEquatable<RiskId>
{
    private static readonly Regex Pattern = new(@"^RISK-([STRIDE])-(\d{3})$", RegexOptions.Compiled);
    
    public string Value { get; }
    public RiskCategory Category { get; }
    public int Number { get; }
    
    public RiskId(string value)
    {
        var match = Pattern.Match(value);
        if (!match.Success)
        {
            throw new ArgumentException($"Invalid risk ID format: {value}. Expected RISK-X-NNN", nameof(value));
        }
        
        Value = value;
        Category = ParseCategory(match.Groups[1].Value[0]);
        Number = int.Parse(match.Groups[2].Value);
    }
    
    private static RiskCategory ParseCategory(char c) => c switch
    {
        'S' => RiskCategory.Spoofing,
        'T' => RiskCategory.Tampering,
        'R' => RiskCategory.Repudiation,
        'I' => RiskCategory.InformationDisclosure,
        'D' => RiskCategory.DenialOfService,
        'E' => RiskCategory.ElevationOfPrivilege,
        _ => throw new ArgumentException($"Invalid category: {c}")
    };
    
    public override string ToString() => Value;
    public bool Equals(RiskId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is RiskId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
}

// DreadScore.cs
public sealed record DreadScore
{
    public int Damage { get; }
    public int Reproducibility { get; }
    public int Exploitability { get; }
    public int AffectedUsers { get; }
    public int Discoverability { get; }
    
    public DreadScore(int damage, int reproducibility, int exploitability, int affectedUsers, int discoverability)
    {
        ValidateScore(damage, nameof(damage));
        ValidateScore(reproducibility, nameof(reproducibility));
        ValidateScore(exploitability, nameof(exploitability));
        ValidateScore(affectedUsers, nameof(affectedUsers));
        ValidateScore(discoverability, nameof(discoverability));
        
        Damage = damage;
        Reproducibility = reproducibility;
        Exploitability = exploitability;
        AffectedUsers = affectedUsers;
        Discoverability = discoverability;
    }
    
    public double Total => (Damage + Reproducibility + Exploitability + AffectedUsers + Discoverability) / 5.0;
    
    public Severity Severity => Total switch
    {
        < 4.0 => Severity.Low,
        < 7.0 => Severity.Medium,
        _ => Severity.High
    };
    
    private static void ValidateScore(int score, string name)
    {
        if (score is < 1 or > 10)
        {
            throw new ArgumentOutOfRangeException(name, score, $"{name} must be between 1 and 10");
        }
    }
}

// Mitigation.cs
public sealed record Mitigation
{
    public required MitigationId Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Implementation { get; init; }
    public required string? VerificationTest { get; init; }
    public required MitigationStatus Status { get; init; }
    public required DateTimeOffset LastVerified { get; init; }
}

public enum RiskCategory { Spoofing, Tampering, Repudiation, InformationDisclosure, DenialOfService, ElevationOfPrivilege }
public enum Severity { Low, Medium, High }
public enum RiskStatus { Active, Deprecated, Accepted }
public enum MitigationStatus { Implemented, InProgress, Pending, NotApplicable }
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

### Error Codes

| Code | Constant | Message | Severity |
|------|----------|---------|----------|
| ACODE-SEC-001 | `ExternalLlmBlocked` | External LLM request blocked by LocalOnly mode | Warning |
| ACODE-SEC-002 | `SecretRedacted` | Secret value redacted from output | Info |
| ACODE-SEC-003 | `PathTraversalDetected` | Path traversal attempt detected and blocked | Error |
| ACODE-SEC-004 | `RiskNotFound` | Risk ID not found in register | Error |
| ACODE-SEC-005 | `MitigationNotFound` | Mitigation ID not found in register | Error |
| ACODE-SEC-006 | `RiskRegisterInvalid` | Risk register failed validation | Error |
| ACODE-SEC-007 | `MitigationTestFailed` | Mitigation verification test failed | Error |
| ACODE-SEC-008 | `DuplicateRiskId` | Duplicate risk ID detected | Error |
| ACODE-SEC-009 | `InvalidDreadScore` | DREAD score out of valid range | Error |
| ACODE-SEC-010 | `MissingMitigation` | Risk has no mitigations defined | Warning |

### Logging Schema

All security-related log entries MUST include these structured fields:

```json
{
  "timestamp": "2025-01-03T10:30:00.000Z",
  "level": "Information|Warning|Error",
  "category": "Acode.Security.RiskManagement",
  "eventName": "RiskMitigationApplied|RiskMitigationFailed|SecurityBlocked|SecretRedacted",
  "correlationId": "abc-123-def-456",
  "riskId": "RISK-I-001",
  "mitigationId": "MIT-001",
  "riskCategory": "InformationDisclosure",
  "severity": "High",
  "action": "Blocked|Allowed|Redacted|Logged",
  "details": "Human-readable explanation",
  "operatingMode": "LocalOnly|Burst|Airgapped"
}
```

Log levels for security events:
- **Information**: Mitigation applied successfully, risk documented
- **Warning**: Mitigation triggered (blocked something), residual risk
- **Error**: Mitigation failed, unmitigated risk detected

### CLI Exit Codes

| Code | Name | Description |
|------|------|-------------|
| 0 | Success | Command completed successfully |
| 1 | VerificationFailed | One or more mitigation tests failed |
| 2 | RiskNotFound | Specified risk ID not found |
| 3 | ExportFailed | Failed to export risk register |
| 4 | ValidationFailed | Risk register validation failed |
| 5 | InvalidArguments | Invalid command arguments |

### Validation Checklist Before Merge

- [ ] 40+ risks documented across all STRIDE categories
- [ ] All risks have unique IDs in format RISK-X-NNN
- [ ] All risks have complete DREAD scores (all 5 components)
- [ ] All DREAD scores are within 1-10 range
- [ ] All risks have at least one mitigation
- [ ] High-severity risks have 2+ mitigations (defense in depth)
- [ ] All mitigation references exist in mitigations section
- [ ] All implemented mitigations have verification tests
- [ ] All verification tests pass
- [ ] Risk register YAML is valid and parseable
- [ ] Risk register Markdown is generated and readable
- [ ] CLI commands work for all scenarios
- [ ] Export produces valid JSON/YAML/Markdown
- [ ] Filter by category works for all 6 categories
- [ ] Filter by severity works for all 3 levels
- [ ] Search by keyword returns relevant results
- [ ] RISK-I-001 mitigation effective (LocalOnly blocks external)
- [ ] RISK-I-002 mitigation effective (secrets redacted)
- [ ] RISK-E-003 mitigation effective (path traversal blocked)
- [ ] All unit tests passing
- [ ] All integration tests passing
- [ ] All E2E tests passing
- [ ] Documentation reviewed by security team
- [ ] Risk owners assigned and notified

### Rollout Plan

#### Phase 1: Domain Models (Days 1-2)
1. Implement RiskId value object with validation
2. Implement DreadScore with calculation and severity
3. Implement Risk, Mitigation, and enum types
4. Write unit tests for all domain models
5. Verify DREAD scoring logic

#### Phase 2: Risk Enumeration (Days 3-5)
1. Create risk-register.yaml with all 40+ risks
2. Populate STRIDE categories (minimum per category):
   - Spoofing: 6 risks
   - Tampering: 7 risks
   - Repudiation: 5 risks
   - Information Disclosure: 10 risks
   - Denial of Service: 7 risks
   - Elevation of Privilege: 7 risks
3. Calculate and document DREAD scores
4. Identify top 10 high-severity risks
5. Review with security team

#### Phase 3: Mitigation Mapping (Days 6-8)
1. Define all mitigations (MIT-001 through MIT-050+)
2. Map risks to mitigations (every risk must have 1+)
3. Verify defense-in-depth for high-severity risks
4. Document implementation paths for each mitigation
5. Identify verification tests for each mitigation
6. Document residual risks

#### Phase 4: Application Layer (Days 9-11)
1. Implement IRiskRegister interface
2. Implement YamlRiskRegisterRepository
3. Implement RiskFilter with category/severity/search
4. Implement MitigationVerifier with test execution
5. Implement RiskRegisterExporter for all formats
6. Write integration tests

#### Phase 5: CLI Commands (Days 12-14)
1. Implement `acode security risks` command
2. Implement `acode security risk <id>` command
3. Implement `acode security mitigations` command
4. Implement `acode security verify-mitigations` command
5. Implement `acode security coverage` command
6. Add --export and --filter options
7. Write E2E tests

#### Phase 6: Verification & Documentation (Days 15-16)
1. Run full test suite
2. Manual verification of all scenarios
3. Generate risk-register.md from YAML
4. Create mitigation documentation files
5. Update DREAD methodology document
6. Security team sign-off

---

**END OF TASK 003.a**
