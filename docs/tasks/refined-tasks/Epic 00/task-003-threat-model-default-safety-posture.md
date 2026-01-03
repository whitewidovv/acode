# Task 003: Threat Model & Default Safety Posture

**Priority:** 12 / 49  
**Tier:** Foundation  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 0 — Foundation  
**Dependencies:** Task 001 (operating modes), Task 002 (config contract)  

---

## Description

### Overview

Task 003 defines the threat model and default safety posture for Acode. A threat model systematically identifies potential security risks, attack vectors, and threat actors that could compromise the system. The default safety posture defines how Acode behaves "out of the box" to minimize risk without user configuration—secure by default, not secure by opt-in.

This task is foundational because security cannot be retrofitted. Every feature, every code path, and every configuration option must be evaluated against this threat model. Acode operates with elevated access to developer machines and source code—the most sensitive assets in a software organization. A security breach in Acode could compromise intellectual property, inject malicious code, or exfiltrate secrets.

### Business Value

A comprehensive threat model provides:

1. **Enterprise Acceptability** — Security teams can evaluate and approve Acode based on documented threat analysis
2. **Regulatory Compliance** — Supports SOC2, ISO 27001, and GDPR requirements for threat assessment
3. **Secure by Default** — Users are protected without needing security expertise
4. **Trust Foundation** — Transparent security posture builds user and organizational trust
5. **Development Guidance** — Developers know what threats to consider when implementing features
6. **Incident Prevention** — Proactive threat identification prevents security incidents
7. **Audit Readiness** — Documentation supports security audits and certifications

### Scope Boundaries

**In Scope:**
- Threat actor identification and classification
- Attack vector enumeration
- Risk assessment methodology
- Default safety posture definition
- Defense-in-depth strategy
- Security invariants (things that must always be true)
- Fail-safe behaviors (what happens on error)
- Trust boundaries (what is trusted vs. untrusted)
- Data classification (what data exists, sensitivity levels)
- Security control mapping (which controls mitigate which threats)

**Out of Scope:**
- Detailed risk enumeration (Task 003.a)
- Denylist and path protection (Task 003.b)
- Audit logging requirements (Task 003.c)
- Implementation of security controls
- Penetration testing execution
- Security tool selection

### Integration Points

| Task | Relationship | Description |
|------|--------------|-------------|
| Task 003.a | Subtask | Risk enumeration details |
| Task 003.b | Subtask | Path protection specifics |
| Task 003.c | Subtask | Audit logging requirements |
| Task 001 | Producer | Mode constraints are security controls |
| Task 002 | Producer | Config validation is security control |
| All Epics | Consumer | All features reference threat model |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Threat missed | Vulnerability exploited | Regular threat model review |
| Control bypassed | Security breach | Defense-in-depth |
| False positive | Usability degraded | Tunable controls |
| Performance impact | User abandonment | Efficient controls |
| Documentation gap | Implementation error | Review process |

### Assumptions

1. Threat actors include both external attackers and curious users
2. Local machine is partially trusted (user's own machine)
3. Network is untrusted by default
4. Source code is highly sensitive
5. Secrets in repositories are common (even if discouraged)
6. Users may not be security experts
7. Secure defaults are preferable to configuration

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| **Threat Model** | Structured analysis of potential security threats |
| **Threat Actor** | Entity that can exploit vulnerabilities |
| **Attack Vector** | Path or method used to exploit a vulnerability |
| **Risk** | Probability of threat × Impact of exploitation |
| **Security Posture** | Overall security state of a system |
| **Defense in Depth** | Multiple layers of security controls |
| **Fail-Safe** | Default to secure state on failure |
| **Fail-Secure** | Deny access/operation on error |
| **Trust Boundary** | Line between trusted and untrusted components |
| **Security Control** | Mechanism that mitigates a threat |
| **Invariant** | Condition that must always hold |
| **Data Classification** | Categorization of data by sensitivity |
| **Principle of Least Privilege** | Grant minimum necessary access |
| **Exfiltration** | Unauthorized data extraction |
| **Injection** | Inserting malicious content into system |
| **Privilege Escalation** | Gaining unauthorized elevated access |
| **Denial of Service** | Making system unavailable |
| **Supply Chain Attack** | Compromising dependencies |
| **STRIDE** | Threat classification framework |
| **DREAD** | Risk assessment methodology |

---

## Out of Scope

- Detailed implementation of security controls
- Security testing and penetration testing
- Security tool selection and deployment
- Security incident response procedures
- Security awareness training materials
- Third-party security assessments
- Compliance certification processes
- Bug bounty program definition
- Security SLA definitions
- Insurance and liability considerations
- Legal review of security practices
- International security regulation compliance
- Hardware security considerations
- Physical security measures

---

## Functional Requirements

### Threat Actor Identification (FR-003-01 to FR-003-15)

| ID | Requirement |
|----|-------------|
| FR-003-01 | Threat model MUST identify all relevant threat actors |
| FR-003-02 | Threat actors MUST include: Malicious External Attacker |
| FR-003-03 | Threat actors MUST include: Curious/Careless User |
| FR-003-04 | Threat actors MUST include: Malicious Insider |
| FR-003-05 | Threat actors MUST include: Compromised Dependency |
| FR-003-06 | Threat actors MUST include: Compromised LLM Provider |
| FR-003-07 | Threat actors MUST include: Malicious Repository Content |
| FR-003-08 | Each threat actor MUST have capability description |
| FR-003-09 | Each threat actor MUST have motivation description |
| FR-003-10 | Each threat actor MUST have access level classification |
| FR-003-11 | Threat actor likelihood MUST be assessed |
| FR-003-12 | Threat actor impact potential MUST be assessed |
| FR-003-13 | Threat actors MUST be prioritized by risk |
| FR-003-14 | Threat actor list MUST be reviewed quarterly |
| FR-003-15 | New threat actors MUST be added as identified |

### Attack Vector Enumeration (FR-003-16 to FR-003-35)

| ID | Requirement |
|----|-------------|
| FR-003-16 | All attack vectors MUST be documented |
| FR-003-17 | Attack vectors MUST include: Data Exfiltration via LLM |
| FR-003-18 | Attack vectors MUST include: Prompt Injection |
| FR-003-19 | Attack vectors MUST include: Command Injection |
| FR-003-20 | Attack vectors MUST include: Path Traversal |
| FR-003-21 | Attack vectors MUST include: Secrets Exposure |
| FR-003-22 | Attack vectors MUST include: Code Injection |
| FR-003-23 | Attack vectors MUST include: Denial of Service |
| FR-003-24 | Attack vectors MUST include: Privilege Escalation |
| FR-003-25 | Attack vectors MUST include: Supply Chain Compromise |
| FR-003-26 | Attack vectors MUST include: Malicious Config |
| FR-003-27 | Attack vectors MUST include: Log Injection |
| FR-003-28 | Each vector MUST have exploitation description |
| FR-003-29 | Each vector MUST have impact assessment |
| FR-003-30 | Each vector MUST have mitigation reference |
| FR-003-31 | Vectors MUST be classified by STRIDE category |
| FR-003-32 | Vectors MUST be scored using DREAD methodology |
| FR-003-33 | High-risk vectors MUST have multiple mitigations |
| FR-003-34 | Vector mitigations MUST be testable |
| FR-003-35 | Vector documentation MUST be versioned |

### Trust Boundaries (FR-003-36 to FR-003-50)

| ID | Requirement |
|----|-------------|
| FR-003-36 | All trust boundaries MUST be identified |
| FR-003-37 | Boundary: Local Machine vs. Network MUST be defined |
| FR-003-38 | Boundary: Acode Process vs. Spawned Processes MUST be defined |
| FR-003-39 | Boundary: User Code vs. Acode Code MUST be defined |
| FR-003-40 | Boundary: Config Input vs. Internal State MUST be defined |
| FR-003-41 | Boundary: LLM Output vs. Trusted Logic MUST be defined |
| FR-003-42 | Boundary: Repository vs. Acode Installation MUST be defined |
| FR-003-43 | Each boundary MUST have data flow documentation |
| FR-003-44 | Each boundary MUST have validation requirements |
| FR-003-45 | Cross-boundary data MUST be validated |
| FR-003-46 | Cross-boundary data MUST be sanitized |
| FR-003-47 | Cross-boundary data MUST be logged |
| FR-003-48 | Trust boundary violations MUST be detected |
| FR-003-49 | Trust boundary violations MUST be logged as errors |
| FR-003-50 | Trust boundary diagram MUST be maintained |

### Data Classification (FR-003-51 to FR-003-65)

| ID | Requirement |
|----|-------------|
| FR-003-51 | All data types MUST be classified |
| FR-003-52 | Classification levels MUST be: Public, Internal, Confidential, Secret |
| FR-003-53 | Source code MUST be classified as Confidential |
| FR-003-54 | Secrets (API keys, passwords) MUST be classified as Secret |
| FR-003-55 | Configuration MUST be classified as Internal |
| FR-003-56 | Logs MUST be classified as Internal (with redaction) |
| FR-003-57 | LLM prompts MUST be classified as Confidential |
| FR-003-58 | LLM responses MUST be classified as Internal |
| FR-003-59 | Audit logs MUST be classified as Confidential |
| FR-003-60 | Each classification MUST have handling requirements |
| FR-003-61 | Secret data MUST never be logged |
| FR-003-62 | Secret data MUST never leave local machine (in LocalOnly) |
| FR-003-63 | Confidential data MUST be encrypted at rest if stored |
| FR-003-64 | Data retention requirements MUST be defined |
| FR-003-65 | Data disposal requirements MUST be defined |

### Default Safety Posture (FR-003-66 to FR-003-85)

| ID | Requirement |
|----|-------------|
| FR-003-66 | Default mode MUST be LocalOnly (no external LLM) |
| FR-003-67 | External network for LLM MUST require explicit consent |
| FR-003-68 | File access MUST be limited to repository by default |
| FR-003-69 | Protected paths MUST be inaccessible by default |
| FR-003-70 | Command execution MUST be logged by default |
| FR-003-71 | Secrets MUST be redacted in logs by default |
| FR-003-72 | Unknown config fields MUST warn by default |
| FR-003-73 | Large files MUST be rejected by default |
| FR-003-74 | Binary files MUST be skipped by default |
| FR-003-75 | Symlinks outside repo MUST be rejected by default |
| FR-003-76 | Timeouts MUST be enforced by default |
| FR-003-77 | Retry limits MUST be enforced by default |
| FR-003-78 | Resource limits MUST be enforced by default |
| FR-003-79 | Audit logging MUST be enabled by default |
| FR-003-80 | Verbose mode MUST NOT log secrets |
| FR-003-81 | Debug mode MUST NOT be enabled by default |
| FR-003-82 | All defaults MUST favor security over convenience |
| FR-003-83 | Defaults MUST be documented |
| FR-003-84 | Default changes MUST require major version bump |
| FR-003-85 | Defaults MUST be testable |

### Security Invariants (FR-003-86 to FR-003-100)

| ID | Requirement |
|----|-------------|
| FR-003-86 | Invariant: No external LLM in LocalOnly mode |
| FR-003-87 | Invariant: No network in Airgapped mode |
| FR-003-88 | Invariant: Secrets never logged unredacted |
| FR-003-89 | Invariant: Protected paths never modified |
| FR-003-90 | Invariant: User consent before Burst mode |
| FR-003-91 | Invariant: All file access logged |
| FR-003-92 | Invariant: All command execution logged |
| FR-003-93 | Invariant: All mode transitions logged |
| FR-003-94 | Invariant: Exit codes propagated correctly |
| FR-003-95 | Invariant: Config validation before use |
| FR-003-96 | Invariant: Input sanitization before processing |
| FR-003-97 | Invariant: Output redaction before storage |
| FR-003-98 | Invariants MUST be enforced at multiple layers |
| FR-003-99 | Invariant violations MUST halt operation |
| FR-003-100 | Invariant violations MUST be logged as critical |

### Fail-Safe Behaviors (FR-003-101 to FR-003-115)

| ID | Requirement |
|----|-------------|
| FR-003-101 | On config error: Use most restrictive defaults |
| FR-003-102 | On mode uncertainty: Default to LocalOnly |
| FR-003-103 | On network error: Fail closed (no external call) |
| FR-003-104 | On timeout: Kill process, log event |
| FR-003-105 | On memory limit: Abort operation, log event |
| FR-003-106 | On disk full: Stop writing, log event |
| FR-003-107 | On permission denied: Fail operation, log event |
| FR-003-108 | On unknown file type: Skip file, log warning |
| FR-003-109 | On validation failure: Reject input, log event |
| FR-003-110 | On invariant violation: Halt, log critical |
| FR-003-111 | On unhandled exception: Log, exit with error code |
| FR-003-112 | Fail-safe MUST NOT leak sensitive data |
| FR-003-113 | Fail-safe MUST leave system in consistent state |
| FR-003-114 | Fail-safe MUST be testable |
| FR-003-115 | Fail-safe behaviors MUST be documented |

---

## Non-Functional Requirements

### Security Documentation (NFR-003-01 to NFR-003-15)

| ID | Requirement |
|----|-------------|
| NFR-003-01 | Threat model MUST be documented in SECURITY.md |
| NFR-003-02 | Threat model MUST be version-controlled |
| NFR-003-03 | Threat model MUST be reviewed on each major release |
| NFR-003-04 | Threat model MUST be reviewed when new features added |
| NFR-003-05 | Threat model changes MUST be logged in CHANGELOG |
| NFR-003-06 | Security controls MUST be documented |
| NFR-003-07 | Security contact MUST be documented |
| NFR-003-08 | Vulnerability disclosure process MUST be documented |
| NFR-003-09 | Security advisories MUST have template |
| NFR-003-10 | Security documentation MUST be public |
| NFR-003-11 | Implementation security details MAY be private |
| NFR-003-12 | Documentation MUST be readable by non-experts |
| NFR-003-13 | Documentation MUST include examples |
| NFR-003-14 | Documentation MUST be kept current |
| NFR-003-15 | Documentation MUST be reviewed for accuracy |

### Security Testing (NFR-003-16 to NFR-003-30)

| ID | Requirement |
|----|-------------|
| NFR-003-16 | All security controls MUST have tests |
| NFR-003-17 | Security tests MUST run in CI |
| NFR-003-18 | Security tests MUST block merge on failure |
| NFR-003-19 | Security test coverage MUST be > 90% |
| NFR-003-20 | Fuzzing MUST be performed on input handlers |
| NFR-003-21 | Static analysis MUST be performed on each build |
| NFR-003-22 | Dependency scanning MUST be performed weekly |
| NFR-003-23 | Known vulnerabilities MUST block release |
| NFR-003-24 | Security regression tests MUST exist |
| NFR-003-25 | Penetration testing SHOULD be performed annually |
| NFR-003-26 | Security test results MUST be documented |
| NFR-003-27 | Failed security tests MUST be investigated |
| NFR-003-28 | Security test gaps MUST be tracked |
| NFR-003-29 | New threats MUST have corresponding tests |
| NFR-003-30 | Test coverage for security MUST be reported |

### Security Monitoring (NFR-003-31 to NFR-003-40)

| ID | Requirement |
|----|-------------|
| NFR-003-31 | Security events MUST be logged |
| NFR-003-32 | Security logs MUST be structured (JSON) |
| NFR-003-33 | Security logs MUST include timestamp |
| NFR-003-34 | Security logs MUST include event type |
| NFR-003-35 | Security logs MUST include severity |
| NFR-003-36 | Security logs MUST be tamper-evident |
| NFR-003-37 | Security logs MUST be retained per policy |
| NFR-003-38 | Critical security events MUST be alertable |
| NFR-003-39 | Security log format MUST be documented |
| NFR-003-40 | Security logs MUST NOT contain secrets |

### Security Performance (NFR-003-41 to NFR-003-50)

| ID | Requirement |
|----|-------------|
| NFR-003-41 | Security checks MUST NOT add > 10% overhead |
| NFR-003-42 | Path validation MUST complete in < 10ms |
| NFR-003-43 | Secret redaction MUST complete in < 5ms |
| NFR-003-44 | Input validation MUST complete in < 50ms |
| NFR-003-45 | Security logging MUST be async (not blocking) |
| NFR-003-46 | Security controls MUST be cacheable where safe |
| NFR-003-47 | Security initialization MUST complete in < 500ms |
| NFR-003-48 | Security controls MUST NOT leak memory |
| NFR-003-49 | Security controls MUST handle concurrent access |
| NFR-003-50 | Performance impact MUST be documented |

---

## User Manual Documentation

### Security Overview

Acode is designed with security as a foundational principle. This document describes the threat model, default security posture, and how to configure security settings.

### Threat Model Summary

Acode protects against the following threat categories:

| Threat | Description | Primary Mitigation |
|--------|-------------|-------------------|
| Data Exfiltration | Source code sent to external services | LocalOnly mode by default |
| Prompt Injection | Malicious instructions in code | LLM output sanitization |
| Command Injection | Malicious commands executed | Input validation, sandboxing |
| Path Traversal | Access files outside repository | Path validation, denylist |
| Secrets Exposure | API keys, passwords leaked | Secret detection, redaction |
| Privilege Escalation | Gaining unauthorized access | Least privilege, no sudo |

### Threat Actors

Acode's threat model considers these actors:

1. **External Attacker** — Remote actor attempting to compromise through network vectors
2. **Curious User** — Legitimate user attempting to bypass restrictions
3. **Malicious Insider** — Team member with access attempting misuse
4. **Compromised Dependency** — Malicious code in third-party libraries
5. **Compromised LLM** — Malicious responses from LLM provider (Burst mode)
6. **Malicious Repository** — Dangerous content in cloned repositories

### Default Security Posture

Acode ships with secure defaults:

| Setting | Default | Reason |
|---------|---------|--------|
| Operating Mode | LocalOnly | No data leaves machine |
| External LLM | Disabled | Requires explicit consent |
| Protected Paths | Enforced | System files inaccessible |
| Secret Redaction | Enabled | Secrets never logged |
| Audit Logging | Enabled | All operations logged |
| Command Timeouts | 5 minutes | Prevent hung processes |
| File Size Limit | 10 MB | Prevent resource exhaustion |
| Binary Files | Skipped | Prevent processing errors |

### Trust Boundaries

```
┌─────────────────────────────────────────────────────────────┐
│                      LOCAL MACHINE                          │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                  ACODE PROCESS                       │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │   │
│  │  │   Config    │  │    Core     │  │     LLM     │  │   │
│  │  │   Parser    │──│    Logic    │──│   Client    │  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  │   │
│  │         │                │                │          │   │
│  │         │ TRUST BOUNDARY │                │          │   │
│  │  ═══════╪════════════════╪════════════════╪═════════ │   │
│  │         │                │                │          │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │   │
│  │  │ Repository  │  │  Commands   │  │   Network   │  │   │
│  │  │   Files     │  │  (Spawned)  │  │   (Local)   │  │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘  │   │
│  └─────────────────────────────────────────────────────┘   │
│                            │                                │
│         TRUST BOUNDARY     │    (Burst mode only)          │
│  ══════════════════════════╪═══════════════════════════════│
│                            │                                │
└────────────────────────────│────────────────────────────────┘
                             ▼
                    ┌─────────────────┐
                    │  External LLM   │
                    │   (Untrusted)   │
                    └─────────────────┘
```

### Data Classification

| Data Type | Classification | Handling |
|-----------|---------------|----------|
| Source Code | Confidential | Never sent externally (LocalOnly) |
| Secrets | Secret | Never logged, always redacted |
| Config Files | Internal | Logged with redaction |
| LLM Prompts | Confidential | Logged locally, consent for external |
| LLM Responses | Internal | Logged, validated before use |
| Audit Logs | Confidential | Tamper-evident, retained |
| Command Output | Internal | Logged with redaction |

### Security Invariants

These conditions MUST always be true:

1. **No external LLM in LocalOnly mode** — Source code never leaves machine
2. **No network in Airgapped mode** — Complete network isolation
3. **Secrets never logged unredacted** — API keys, passwords protected
4. **Protected paths never modified** — System files safe
5. **Consent before Burst mode** — User acknowledges data sharing

### Fail-Safe Behaviors

When errors occur, Acode fails safely:

| Error Condition | Fail-Safe Behavior |
|-----------------|-------------------|
| Config parse error | Use restrictive defaults |
| Unknown mode | Default to LocalOnly |
| Network error | Fail closed, no external call |
| Timeout exceeded | Kill process, log event |
| Memory limit hit | Abort operation |
| Permission denied | Fail operation, log |
| Invariant violated | Halt immediately |

### CLI Security Commands

```bash
# View current security posture
acode security status

# Validate security configuration
acode security check

# Show threat model summary
acode security threats

# Audit recent operations
acode security audit --last 24h

# Check for secrets in repository
acode security scan-secrets
```

### Configuration Options

```yaml
# .agent/config.yml security settings
security:
  # Secret patterns to detect and redact
  secret_patterns:
    - "(?i)api[_-]?key"
    - "(?i)password"
    - "(?i)secret"
    - "(?i)token"
  
  # Additional protected paths
  protected_paths:
    - ".ssh/"
    - ".gnupg/"
  
  # Maximum file size to process
  max_file_size_mb: 10
  
  # Enable verbose security logging
  verbose_security_log: false
```

### Best Practices

1. **Stay in LocalOnly mode** — Only use Burst mode when necessary
2. **Review prompts before Burst** — Know what data is being sent
3. **Keep protected paths updated** — Add custom sensitive paths
4. **Enable audit logging** — Keep records for security review
5. **Scan for secrets** — Run `acode security scan-secrets` regularly
6. **Update regularly** — Security patches in new versions
7. **Review permissions** — Ensure Acode runs as regular user

### Troubleshooting Security Issues

#### "Operation blocked by security policy"

```bash
# Check what security control triggered
acode security explain-block --last

# Review security configuration
acode config show security
```

#### "Protected path access denied"

The file is in a protected directory. Either:
1. Remove the file from protected paths (if safe)
2. Copy the file to a non-protected location

#### "Secret detected in output"

Acode detected a potential secret and redacted it. Check:
1. The redacted value is actually a secret
2. Remove secrets from code/config where possible
3. Add to `.gitignore` if file should be ignored

### FAQ

**Q: Can I disable security checks?**
A: Core security invariants cannot be disabled. Some checks can be configured.

**Q: Is my code sent to any server?**
A: In LocalOnly mode (default), never. In Burst mode, only what you consent to.

**Q: How do I know if a secret was exposed?**
A: Check audit logs with `acode security audit`. Redaction is logged.

**Q: Can Acode access my SSH keys?**
A: No. `.ssh/` is a protected path and inaccessible by default.

**Q: What happens in Airgapped mode?**
A: All network access is blocked, including localhost. Use offline models.

---

## Acceptance Criteria / Definition of Done

### Threat Actor Documentation (20 items)

- [ ] All threat actors identified
- [ ] External attacker documented
- [ ] Curious user documented
- [ ] Malicious insider documented
- [ ] Compromised dependency documented
- [ ] Compromised LLM documented
- [ ] Malicious repository documented
- [ ] Each actor has capability description
- [ ] Each actor has motivation description
- [ ] Each actor has access level
- [ ] Likelihood assessed for each actor
- [ ] Impact assessed for each actor
- [ ] Actors prioritized by risk
- [ ] Review schedule documented
- [ ] Actor list version-controlled
- [ ] Actor documentation in SECURITY.md
- [ ] Actor documentation reviewed
- [ ] Actor list covers local-first context
- [ ] Actor list covers enterprise context
- [ ] Actor list covers individual developer context

### Attack Vector Documentation (25 items)

- [ ] All attack vectors documented
- [ ] Data exfiltration documented
- [ ] Prompt injection documented
- [ ] Command injection documented
- [ ] Path traversal documented
- [ ] Secrets exposure documented
- [ ] Code injection documented
- [ ] Denial of service documented
- [ ] Privilege escalation documented
- [ ] Supply chain documented
- [ ] Malicious config documented
- [ ] Log injection documented
- [ ] Each vector has exploitation description
- [ ] Each vector has impact assessment
- [ ] Each vector has mitigation reference
- [ ] STRIDE classification complete
- [ ] DREAD scoring complete
- [ ] High-risk vectors have multiple mitigations
- [ ] Mitigations are testable
- [ ] Documentation is versioned
- [ ] Attack trees documented where applicable
- [ ] Kill chain analysis complete
- [ ] Common CVE patterns mapped
- [ ] Detection strategies documented
- [ ] Response strategies documented

### Trust Boundary Documentation (20 items)

- [ ] All trust boundaries identified
- [ ] Local machine vs network documented
- [ ] Acode process vs spawned processes documented
- [ ] User code vs Acode code documented
- [ ] Config input vs internal state documented
- [ ] LLM output vs trusted logic documented
- [ ] Repository vs Acode installation documented
- [ ] Each boundary has data flow documentation
- [ ] Each boundary has validation requirements
- [ ] Cross-boundary validation defined
- [ ] Cross-boundary sanitization defined
- [ ] Cross-boundary logging defined
- [ ] Violation detection defined
- [ ] Violation logging defined
- [ ] Trust boundary diagram exists
- [ ] Diagram is current
- [ ] Diagram is in documentation
- [ ] Diagram reviewed for accuracy
- [ ] Boundary transitions documented
- [ ] Boundary exceptions documented (if any)

### Data Classification (20 items)

- [ ] All data types classified
- [ ] Classification levels defined
- [ ] Source code classified
- [ ] Secrets classified
- [ ] Configuration classified
- [ ] Logs classified
- [ ] LLM prompts classified
- [ ] LLM responses classified
- [ ] Audit logs classified
- [ ] Each classification has handling requirements
- [ ] Secret handling documented
- [ ] Confidential handling documented
- [ ] Internal handling documented
- [ ] Public handling documented
- [ ] Retention requirements defined
- [ ] Disposal requirements defined
- [ ] Classification documented
- [ ] Classification reviewed
- [ ] Classification training provided
- [ ] Classification audit possible

### Default Safety Posture (25 items)

- [ ] Default mode is LocalOnly
- [ ] External LLM requires consent
- [ ] File access limited to repository
- [ ] Protected paths enforced
- [ ] Command execution logged
- [ ] Secrets redacted in logs
- [ ] Unknown config fields warn
- [ ] Large files rejected
- [ ] Binary files skipped
- [ ] External symlinks rejected
- [ ] Timeouts enforced
- [ ] Retry limits enforced
- [ ] Resource limits enforced
- [ ] Audit logging enabled
- [ ] Verbose mode safe
- [ ] Debug mode disabled by default
- [ ] Defaults favor security
- [ ] Defaults documented
- [ ] Default changes require major version
- [ ] Defaults testable
- [ ] Defaults reviewed
- [ ] Defaults match documentation
- [ ] Defaults consistent across platforms
- [ ] Defaults enforceable
- [ ] Defaults auditable

### Security Invariants (20 items)

- [ ] No external LLM in LocalOnly invariant
- [ ] No network in Airgapped invariant
- [ ] Secrets never logged invariant
- [ ] Protected paths invariant
- [ ] User consent invariant
- [ ] File access logged invariant
- [ ] Command execution logged invariant
- [ ] Mode transition logged invariant
- [ ] Exit codes propagated invariant
- [ ] Config validation invariant
- [ ] Input sanitization invariant
- [ ] Output redaction invariant
- [ ] Invariants enforced at multiple layers
- [ ] Violations halt operation
- [ ] Violations logged as critical
- [ ] Invariants tested
- [ ] Invariants documented
- [ ] Invariants reviewed
- [ ] Invariant monitoring exists
- [ ] Invariant enforcement verified

### Fail-Safe Behaviors (20 items)

- [ ] Config error fail-safe defined
- [ ] Mode uncertainty fail-safe defined
- [ ] Network error fail-safe defined
- [ ] Timeout fail-safe defined
- [ ] Memory limit fail-safe defined
- [ ] Disk full fail-safe defined
- [ ] Permission denied fail-safe defined
- [ ] Unknown file type fail-safe defined
- [ ] Validation failure fail-safe defined
- [ ] Invariant violation fail-safe defined
- [ ] Unhandled exception fail-safe defined
- [ ] Fail-safes don't leak data
- [ ] Fail-safes leave consistent state
- [ ] Fail-safes testable
- [ ] Fail-safes documented
- [ ] Fail-safes reviewed
- [ ] Fail-safe tests exist
- [ ] Fail-safe logging correct
- [ ] Fail-safe recovery documented
- [ ] Fail-safe user guidance provided

### Documentation (20 items)

- [ ] SECURITY.md exists
- [ ] Threat model in SECURITY.md
- [ ] Security contact documented
- [ ] Vulnerability disclosure documented
- [ ] Security advisory template exists
- [ ] Documentation is public
- [ ] Documentation readable by non-experts
- [ ] Documentation includes examples
- [ ] Documentation current
- [ ] Documentation reviewed
- [ ] Documentation version-controlled
- [ ] Documentation in release notes
- [ ] CLI help includes security info
- [ ] Error messages are security-aware
- [ ] Security configuration documented
- [ ] Trust boundaries diagrammed
- [ ] Data flow documented
- [ ] Mitigation mapping documented
- [ ] Testing approach documented
- [ ] Update process documented

### Testing and Validation (20 items)

- [ ] All security controls tested
- [ ] Security tests in CI
- [ ] Security tests block merge
- [ ] Security coverage > 90%
- [ ] Fuzzing performed
- [ ] Static analysis performed
- [ ] Dependency scanning performed
- [ ] Known vulnerabilities checked
- [ ] Regression tests exist
- [ ] Test results documented
- [ ] Failed tests investigated
- [ ] Test gaps tracked
- [ ] New threats have tests
- [ ] Coverage reported
- [ ] Penetration test planned
- [ ] Security review completed
- [ ] Code review includes security
- [ ] Security sign-off required
- [ ] Security audit trail exists
- [ ] Security metrics tracked

---
## Testing Requirements

### Unit Tests

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| UT-003-01 | LocalOnly mode blocks external LLM | Request rejected |
| UT-003-02 | Airgapped mode blocks all network | Network call fails |
| UT-003-03 | Secret pattern detected | Secret flagged |
| UT-003-04 | Secret redaction works | Secret replaced with [REDACTED] |
| UT-003-05 | Protected path rejected | Access denied |
| UT-003-06 | Path traversal rejected | Access denied |
| UT-003-07 | Symlink outside repo rejected | Access denied |
| UT-003-08 | Large file rejected | File skipped |
| UT-003-09 | Binary file skipped | File not processed |
| UT-003-10 | Input sanitization removes dangerous chars | Clean output |
| UT-003-11 | STRIDE classification correct | Category assigned |
| UT-003-12 | DREAD score calculated | Score returned |
| UT-003-13 | Trust boundary crossing logged | Log entry created |
| UT-003-14 | Data classification returns correct level | Level returned |
| UT-003-15 | Fail-safe on config error | Restrictive defaults used |
| UT-003-16 | Invariant violation halts | Operation stopped |
| UT-003-17 | Invariant violation logged | Critical log entry |
| UT-003-18 | Mode transition logged | Log entry created |
| UT-003-19 | Command execution logged | Log entry created |
| UT-003-20 | Audit log tamper-evident | Hash included |

### Integration Tests

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| IT-003-01 | Full request in LocalOnly mode | No external network |
| IT-003-02 | Full request in Airgapped mode | No network at all |
| IT-003-03 | Burst mode requires consent | Consent prompt shown |
| IT-003-04 | Secret in file redacted in logs | Redacted in output |
| IT-003-05 | Protected path inaccessible | Error returned |
| IT-003-06 | Multiple trust boundaries crossed | All logged |
| IT-003-07 | Config error triggers fail-safe | Safe defaults used |
| IT-003-08 | Timeout triggers process kill | Process terminated |
| IT-003-09 | Memory limit triggers abort | Operation aborted |
| IT-003-10 | Security status command works | Status displayed |
| IT-003-11 | Security check command works | Check results shown |
| IT-003-12 | Security audit command works | Audit displayed |
| IT-003-13 | All security controls active | Controls verified |
| IT-003-14 | Security logging complete | All events logged |
| IT-003-15 | Security configuration applied | Settings effective |

### End-to-End Tests

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| E2E-003-01 | acode security status | Status displayed |
| E2E-003-02 | acode security check | Check passes |
| E2E-003-03 | acode security threats | Threats listed |
| E2E-003-04 | acode security audit --last 24h | Audit displayed |
| E2E-003-05 | Access protected path | Blocked, error shown |
| E2E-003-06 | Attempt external LLM in LocalOnly | Blocked |
| E2E-003-07 | Attempt network in Airgapped | Blocked |
| E2E-003-08 | Config with secrets | Secrets redacted in logs |
| E2E-003-09 | Invalid config | Fail-safe defaults used |
| E2E-003-10 | Invariant violation scenario | Operation halted |
| E2E-003-11 | Trust boundary diagram accessible | Diagram shown |
| E2E-003-12 | Security documentation accessible | Docs available |

### Performance / Benchmarks

| ID | Benchmark | Target | Measurement Method |
|----|-----------|--------|-------------------|
| PERF-003-01 | Security check overhead | < 10% total | Comparison timing |
| PERF-003-02 | Path validation | < 10ms | Stopwatch, 1000 iterations |
| PERF-003-03 | Secret redaction | < 5ms | Stopwatch, 1000 iterations |
| PERF-003-04 | Input validation | < 50ms | Stopwatch, 100 iterations |
| PERF-003-05 | Security logging latency | < 10ms | Async measurement |
| PERF-003-06 | Security initialization | < 500ms | Startup timing |
| PERF-003-07 | Trust boundary check | < 1ms | Stopwatch, 10000 iterations |
| PERF-003-08 | Data classification lookup | < 1ms | Stopwatch, 10000 iterations |

### Regression / Impacted Areas

| Area | Impact | Regression Test |
|------|--------|-----------------|
| All features | Security checks | Controls still active |
| Mode enforcement | Mode rules | Modes enforced |
| Config loading | Validation | Config validated |
| Logging | Redaction | Secrets redacted |
| Command execution | Sandboxing | Commands isolated |
| File access | Path checking | Paths validated |
| Network | Mode blocking | Network controlled |
| Error handling | Fail-safes | Safe failures |

---

## User Verification Steps

### Scenario 1: View Security Status
1. Run `acode security status`
2. **Verify:** Current mode displayed
3. **Verify:** Protected paths shown
4. **Verify:** Audit logging status shown

### Scenario 2: Security Configuration Check
1. Run `acode security check`
2. **Verify:** All security controls verified
3. **Verify:** Any issues reported

### Scenario 3: LocalOnly Mode Enforcement
1. Set mode to LocalOnly in config
2. Attempt to use external LLM provider
3. **Verify:** Request blocked
4. **Verify:** Error message explains why

### Scenario 4: Airgapped Mode Enforcement
1. Set mode to Airgapped in config
2. Attempt any network operation
3. **Verify:** Operation blocked
4. **Verify:** No network traffic generated

### Scenario 5: Protected Path Access
1. Attempt to access file in `.ssh/`
2. **Verify:** Access denied
3. **Verify:** Error logged

### Scenario 6: Secret Redaction
1. Include a file with `API_KEY=secret123`
2. Run operation that logs file content
3. **Verify:** Log shows `API_KEY=[REDACTED]`

### Scenario 7: Path Traversal Prevention
1. Create config with path `../outside-repo`
2. Run `acode config validate`
3. **Verify:** Validation fails
4. **Verify:** Path traversal error shown

### Scenario 8: Fail-Safe on Config Error
1. Create malformed config file
2. Run any acode command
3. **Verify:** Error shown for config
4. **Verify:** Restrictive defaults used

### Scenario 9: Burst Mode Consent
1. Set mode to allow burst
2. Trigger burst mode usage
3. **Verify:** Consent prompt shown
4. **Verify:** No external call without consent

### Scenario 10: Audit Log Review
1. Perform several operations
2. Run `acode security audit --last 1h`
3. **Verify:** All operations logged
4. **Verify:** Timestamps present
5. **Verify:** Secrets redacted

### Scenario 11: Invariant Violation
1. Simulate invariant violation (test mode)
2. **Verify:** Operation halts immediately
3. **Verify:** Critical error logged
4. **Verify:** System in safe state

### Scenario 12: Trust Boundary Logging
1. Perform operation that crosses trust boundary
2. Check logs
3. **Verify:** Boundary crossing logged
4. **Verify:** Data flow documented

### Scenario 13: Large File Rejection
1. Create file > 10MB
2. Attempt to process
3. **Verify:** File skipped
4. **Verify:** Warning logged

### Scenario 14: Security Documentation
1. Run `acode docs security`
2. **Verify:** SECURITY.md displayed
3. **Verify:** Threat model accessible
4. **Verify:** Contact information present

---

## Implementation Prompt for Claude

### Objective

Define and document the threat model and default safety posture for Acode. This establishes the security foundation for all features.

### Architecture Constraints

- **Security by Default** — Secure configuration requires no user action
- **Defense in Depth** — Multiple layers of security controls
- **Fail-Safe** — Errors result in secure state
- **Transparency** — Security posture is visible and documented

### File Structure

```
docs/
├── SECURITY.md                    # Public security documentation
├── security/
│   ├── threat-model.md           # Detailed threat model
│   ├── trust-boundaries.md       # Trust boundary documentation
│   ├── data-classification.md    # Data handling requirements
│   └── diagrams/
│       ├── trust-boundaries.png  # Trust boundary diagram
│       └── data-flow.png         # Data flow diagram
src/
├── Acode.Domain/
│   └── Security/
│       ├── ThreatActor.cs
│       ├── AttackVector.cs
│       ├── TrustBoundary.cs
│       ├── DataClassification.cs
│       ├── SecurityInvariant.cs
│       └── FailSafeBehavior.cs
├── Acode.Application/
│   └── Security/
│       ├── ISecurityChecker.cs
│       ├── ISecretRedactor.cs
│       ├── IPathValidator.cs
│       ├── IInvariantEnforcer.cs
│       ├── SecurityChecker.cs
│       ├── SecretRedactor.cs
│       ├── PathValidator.cs
│       └── InvariantEnforcer.cs
└── Acode.Cli/
    └── Commands/
        └── SecurityCommands.cs
```

### Interface Contracts

```csharp
// ISecurityChecker.cs
public interface ISecurityChecker
{
    Task<SecurityStatus> GetStatusAsync();
    Task<SecurityCheckResult> CheckAsync();
    bool IsOperationAllowed(string operation, OperatingMode mode);
}

// ISecretRedactor.cs
public interface ISecretRedactor
{
    string Redact(string input);
    bool ContainsSecrets(string input);
    IReadOnlyList<string> GetDefaultPatterns();
}

// IPathValidator.cs
public interface IPathValidator
{
    bool IsPathSafe(string path, string repositoryRoot);
    bool IsProtectedPath(string path);
    PathValidationResult Validate(string path, string repositoryRoot);
}

// IInvariantEnforcer.cs
public interface IInvariantEnforcer
{
    void Enforce(SecurityInvariant invariant, object context);
    void RegisterInvariant(SecurityInvariant invariant);
    IReadOnlyList<SecurityInvariant> GetAllInvariants();
}
```

### Security Event Codes

```csharp
public static class SecurityEventCodes
{
    public const string InvariantViolation = "ACODE-SEC-001";
    public const string TrustBoundaryCrossing = "ACODE-SEC-002";
    public const string ProtectedPathAccess = "ACODE-SEC-003";
    public const string SecretDetected = "ACODE-SEC-004";
    public const string ModeViolation = "ACODE-SEC-005";
    public const string PathTraversalAttempt = "ACODE-SEC-006";
    public const string FailSafeTriggered = "ACODE-SEC-007";
    public const string ConsentRequired = "ACODE-SEC-008";
    public const string ConsentGranted = "ACODE-SEC-009";
    public const string ConsentDenied = "ACODE-SEC-010";
    public const string AuditEvent = "ACODE-SEC-011";
    public const string SecurityCheckFailed = "ACODE-SEC-012";
    public const string SecurityCheckPassed = "ACODE-SEC-013";
}
```

### Logging Schema

```csharp
public static class SecurityLogFields
{
    public const string EventCode = "security_event_code";
    public const string EventType = "security_event_type";
    public const string Severity = "security_severity";
    public const string ThreatActor = "threat_actor";
    public const string AttackVector = "attack_vector";
    public const string TrustBoundary = "trust_boundary";
    public const string DataClassification = "data_classification";
    public const string Invariant = "security_invariant";
    public const string OperatingMode = "operating_mode";
    public const string ActionTaken = "action_taken";
    public const string ResourcePath = "resource_path";
    public const string UserId = "user_id";
}
```

### Validation Checklist Before Merge

- [ ] All 115 functional requirements documented
- [ ] All 50 non-functional requirements documented
- [ ] SECURITY.md complete and accurate
- [ ] Threat model documented
- [ ] All threat actors identified
- [ ] All attack vectors documented
- [ ] All trust boundaries documented
- [ ] Data classification complete
- [ ] Default safety posture defined
- [ ] All invariants defined
- [ ] All fail-safes defined
- [ ] Trust boundary diagram created
- [ ] Data flow diagram created
- [ ] All unit tests passing
- [ ] All integration tests passing
- [ ] All E2E tests passing
- [ ] Security review completed
- [ ] Documentation reviewed

### Rollout Plan

1. **Phase 1: Documentation**
   - Create SECURITY.md
   - Document threat actors
   - Document attack vectors
   - Create diagrams

2. **Phase 2: Domain Models**
   - Implement security domain types
   - Implement invariant definitions
   - Implement fail-safe definitions
   - Add unit tests

3. **Phase 3: Application Services**
   - Implement security checker
   - Implement secret redactor
   - Implement path validator
   - Implement invariant enforcer
   - Add integration tests

4. **Phase 4: CLI Integration**
   - Implement security commands
   - Integrate with existing commands
   - Add E2E tests

5. **Phase 5: Review**
   - Security review
   - Documentation review
   - Penetration test planning
   - Final validation

---

**END OF TASK 003**