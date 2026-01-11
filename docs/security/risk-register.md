# Risk Register

**Version**: 1.0.0
**Last Updated**: 2026-01-03

This document provides a comprehensive enumeration of security risks and their mitigations for Acode.

## Table of Contents

- [Risks by STRIDE Category](#risks-by-stride-category)
  - [Spoofing](#spoofing)
  - [Tampering](#tampering)
  - [Repudiation](#repudiation)
  - [Information Disclosure](#information-disclosure)
  - [Denial of Service](#denial-of-service)
  - [Elevation of Privilege](#elevation-of-privilege)
- [Mitigations](#mitigations)

## Risks by STRIDE Category

### Spoofing

**Risks in Category**: 6

| ID | Title | Severity | DREAD | Status | Mitigations |
|----|-------|----------|-------|--------|-------------|
| RISK-S-001 | Malicious LLM impersonating local model | Medium | 5.8 | Active | MIT-001, MIT-004, MIT-005 |
| RISK-S-002 | Config file replacement attack | Medium | 6.4 | Active | MIT-006, MIT-007, MIT-008 |
| RISK-S-003 | Dependency confusion attack | Medium | 7.0 | Active | None |
| RISK-S-004 | Process impersonation | Medium | 6.0 | Active | None |
| RISK-S-005 | Git remote impersonation | Medium | 6.4 | Active | MIT-015 |
| RISK-S-006 | User identity spoofing in audit logs | Medium | 6.2 | Active | MIT-020 |

#### Detailed Risk Information

##### RISK-S-001: Malicious LLM impersonating local model

- **Description**: In Burst mode, if an attacker controls DNS or network routing, they could redirect
LLM API requests to a malicious endpoint that impersonates the intended service,
capturing prompts containing source code.

- **Severity**: Medium
- **Status**: Active
- **Owner**: security-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 9/10
- Reproducibility: 3/10
- Exploitability: 5/10
- Affected Users: 8/10
- Discoverability: 4/10
- **Average**: 5.8

**Mitigations**:
- [MIT-001](#mit-mit-001): LocalOnly operating mode default
- [MIT-004](#mit-mit-004): TLS certificate pinning for external LLM connections
- [MIT-005](#mit-mit-005): User consent workflow for Burst mode

**Residual Risk**: In Burst mode with user consent, if TLS is compromised, risk remains.


---

##### RISK-S-002: Config file replacement attack

- **Description**: Attacker with file system access replaces .agent/config.yml with malicious
configuration pointing to attacker-controlled LLM or modifying operating mode.

- **Severity**: Medium
- **Status**: Active
- **Owner**: security-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 8/10
- Reproducibility: 7/10
- Exploitability: 6/10
- Affected Users: 5/10
- Discoverability: 6/10
- **Average**: 6.4

**Mitigations**:
- [MIT-006](#mit-mit-006): Config file integrity checks
- [MIT-007](#mit-mit-007): File permissions validation on config files
- [MIT-008](#mit-mit-008): Audit logging of configuration changes

**Residual Risk**: If attacker has filesystem write access, they may have broader compromise capability.


---

##### RISK-S-003: Dependency confusion attack

- **Description**: Attacker publishes malicious package with same name as internal dependency,
causing package manager to install malicious version.

- **Severity**: Medium
- **Status**: Active
- **Owner**: infrastructure-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 10/10
- Reproducibility: 8/10
- Exploitability: 4/10
- Affected Users: 10/10
- Discoverability: 3/10
- **Average**: 7.0

**Residual Risk**: Supply chain attacks remain difficult to detect without comprehensive SBOM analysis.


---

##### RISK-S-004: Process impersonation

- **Description**: Malicious process impersonates Acode CLI to capture user input or credentials.

- **Severity**: Medium
- **Status**: Active
- **Owner**: security-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 7/10
- Reproducibility: 5/10
- Exploitability: 7/10
- Affected Users: 6/10
- Discoverability: 5/10
- **Average**: 6.0

**Residual Risk**: Users may not verify process identity before providing sensitive input.


---

##### RISK-S-005: Git remote impersonation

- **Description**: Attacker modifies .git/config to point to malicious remote, exfiltrating code during push/pull.

- **Severity**: Medium
- **Status**: Active
- **Owner**: security-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 9/10
- Reproducibility: 6/10
- Exploitability: 6/10
- Affected Users: 7/10
- Discoverability: 4/10
- **Average**: 6.4

**Mitigations**:
- [MIT-015](#mit-mit-015): .git/ protected path enforcement

**Residual Risk**: If .git/ protection is bypassed, repository integrity is compromised.


---

##### RISK-S-006: User identity spoofing in audit logs

- **Description**: If audit logs don't capture true user identity, attacker actions may be attributed to legitimate user.

- **Severity**: Medium
- **Status**: Active
- **Owner**: audit-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 6/10
- Reproducibility: 4/10
- Exploitability: 6/10
- Affected Users: 8/10
- Discoverability: 7/10
- **Average**: 6.2

**Mitigations**:
- [MIT-020](#mit-mit-020): Tamper-evident audit logs

**Residual Risk**: User identity spoofing at OS level (su, sudo) may still attribute actions incorrectly.


---

### Tampering

**Risks in Category**: 7

| ID | Title | Severity | DREAD | Status | Mitigations |
|----|-------|----------|-------|--------|-------------|
| RISK-T-001 | Source code modification by malicious LLM response | Medium | 7.0 | Active | MIT-021 |
| RISK-T-002 | Audit log tampering | High | 7.4 | Active | MIT-020 |
| RISK-T-003 | Configuration tampering to disable security controls | High | 7.6 | Active | MIT-006, MIT-028, MIT-008 |
| RISK-T-004 | Symlink attack to modify protected files | Medium | 7.0 | Active | MIT-029 |
| RISK-T-005 | Time-of-check to time-of-use (TOCTOU) race | Medium | 5.2 | Active | None |
| RISK-T-006 | Environment variable injection | Medium | 6.4 | Active | None |
| RISK-T-007 | Binary tampering via package manager | Medium | 5.8 | Active | None |

#### Detailed Risk Information

##### RISK-T-001: Source code modification by malicious LLM response

- **Description**: LLM response contains malicious code that, when applied by Acode, introduces
backdoors, vulnerabilities, or data exfiltration into the codebase.

- **Severity**: Medium
- **Status**: Active
- **Owner**: security-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 10/10
- Reproducibility: 7/10
- Exploitability: 5/10
- Affected Users: 10/10
- Discoverability: 3/10
- **Average**: 7.0

**Mitigations**:
- [MIT-021](#mit-mit-021): Output sanitization and validation

**Residual Risk**: Subtle malicious code may pass review and static analysis.


---

##### RISK-T-002: Audit log tampering

- **Description**: Attacker with file system access modifies or deletes audit logs to hide malicious activity.

- **Severity**: High
- **Status**: Active
- **Owner**: audit-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 8/10
- Reproducibility: 8/10
- Exploitability: 5/10
- Affected Users: 10/10
- Discoverability: 6/10
- **Average**: 7.4

**Mitigations**:
- [MIT-020](#mit-mit-020): Tamper-evident audit logs

**Residual Risk**: Root/admin access allows log deletion. Remote shipping conflicts with LocalOnly mode.


---

##### RISK-T-003: Configuration tampering to disable security controls

- **Description**: Attacker modifies .agent/config.yml to disable protected paths, secret redaction, or audit logging.

- **Severity**: High
- **Status**: Active
- **Owner**: security-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 9/10
- Reproducibility: 9/10
- Exploitability: 5/10
- Affected Users: 10/10
- Discoverability: 5/10
- **Average**: 7.6

**Mitigations**:
- [MIT-006](#mit-mit-006): Config file integrity checks
- [MIT-028](#mit-mit-028): Security invariants cannot be disabled
- [MIT-008](#mit-mit-008): Audit logging of configuration changes

**Residual Risk**: If config validation is bypassed, security controls may be disabled.


---

##### RISK-T-004: Symlink attack to modify protected files

- **Description**: Attacker creates symlink from allowed path to protected path, bypassing path protection.

- **Severity**: Medium
- **Status**: Active
- **Owner**: security-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 9/10
- Reproducibility: 7/10
- Exploitability: 6/10
- Affected Users: 8/10
- Discoverability: 5/10
- **Average**: 7.0

**Mitigations**:
- [MIT-029](#mit-mit-029): Symlink resolution before path validation

**Residual Risk**: Complex symlink chains may be difficult to detect.


---

##### RISK-T-005: Time-of-check to time-of-use (TOCTOU) race

- **Description**: Path is validated as safe, but replaced with malicious file before use.

- **Severity**: Medium
- **Status**: Active
- **Owner**: infrastructure-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 7/10
- Reproducibility: 3/10
- Exploitability: 8/10
- Affected Users: 6/10
- Discoverability: 2/10
- **Average**: 5.2

**Residual Risk**: TOCTOU is inherently difficult to eliminate completely.


---

##### RISK-T-006: Environment variable injection

- **Description**: Attacker modifies environment variables to influence Acode behavior (PATH, LD_PRELOAD, etc.).

- **Severity**: Medium
- **Status**: Active
- **Owner**: infrastructure-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 8/10
- Reproducibility: 6/10
- Exploitability: 6/10
- Affected Users: 7/10
- Discoverability: 5/10
- **Average**: 6.4

**Residual Risk**: LD_PRELOAD and similar attacks may bypass application-level controls.


---

##### RISK-T-007: Binary tampering via package manager

- **Description**: Attacker compromises package manager to deliver tampered Acode binaries.

- **Severity**: Medium
- **Status**: Active
- **Owner**: release-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 10/10
- Reproducibility: 4/10
- Exploitability: 3/10
- Affected Users: 10/10
- Discoverability: 2/10
- **Average**: 5.8

**Residual Risk**: Supply chain attacks on distribution infrastructure are difficult to prevent.


---

### Repudiation

**Risks in Category**: 5

| ID | Title | Severity | DREAD | Status | Mitigations |
|----|-------|----------|-------|--------|-------------|
| RISK-R-001 | Unlogged file modifications | Medium | 7.0 | Active | MIT-042 |
| RISK-R-002 | Unlogged command execution | High | 7.4 | Active | None |
| RISK-R-003 | Unlogged operating mode changes | High | 7.6 | Active | None |
| RISK-R-004 | Unlogged external API calls | Medium | 7.0 | Active | None |
| RISK-R-005 | Audit log deletion | High | 7.4 | Active | MIT-020 |

#### Detailed Risk Information

##### RISK-R-001: Unlogged file modifications

- **Description**: File modifications occur without audit logging, preventing attribution of changes.

- **Severity**: Medium
- **Status**: Active
- **Owner**: audit-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 6/10
- Reproducibility: 8/10
- Exploitability: 5/10
- Affected Users: 9/10
- Discoverability: 7/10
- **Average**: 7.0

**Mitigations**:
- [MIT-042](#mit-mit-042): Comprehensive file operation logging

**Residual Risk**: File system level changes (outside Acode) won't be logged.


---

##### RISK-R-002: Unlogged command execution

- **Description**: Commands executed by Acode are not logged, preventing accountability for malicious commands.

- **Severity**: High
- **Status**: Active
- **Owner**: audit-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 8/10
- Reproducibility: 8/10
- Exploitability: 5/10
- Affected Users: 9/10
- Discoverability: 7/10
- **Average**: 7.4

**Residual Risk**: Commands run outside Acode control won't be logged.


---

##### RISK-R-003: Unlogged operating mode changes

- **Description**: Operating mode changes (LocalOnly → Burst) are not logged, hiding security policy violations.

- **Severity**: High
- **Status**: Active
- **Owner**: audit-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 7/10
- Reproducibility: 9/10
- Exploitability: 4/10
- Affected Users: 10/10
- Discoverability: 8/10
- **Average**: 7.6

**Residual Risk**: Config file edits may change mode without triggering logging if done outside Acode.


---

##### RISK-R-004: Unlogged external API calls

- **Description**: In Burst mode, external API calls (to LLMs) are not logged, hiding data exfiltration.

- **Severity**: Medium
- **Status**: Active
- **Owner**: audit-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 9/10
- Reproducibility: 7/10
- Exploitability: 5/10
- Affected Users: 8/10
- Discoverability: 6/10
- **Average**: 7.0

**Residual Risk**: Network-level exfiltration may bypass application logging.


---

##### RISK-R-005: Audit log deletion

- **Description**: Attacker deletes audit logs to hide malicious activity after the fact.

- **Severity**: High
- **Status**: Active
- **Owner**: audit-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 8/10
- Reproducibility: 8/10
- Exploitability: 5/10
- Affected Users: 10/10
- Discoverability: 6/10
- **Average**: 7.4

**Mitigations**:
- [MIT-020](#mit-mit-020): Tamper-evident audit logs

**Residual Risk**: Root/admin access can delete all logs.


---

### Information Disclosure

**Risks in Category**: 10

| ID | Title | Severity | DREAD | Status | Mitigations |
|----|-------|----------|-------|--------|-------------|
| RISK-I-001 | Source code exfiltration via external LLM | High | 7.8 | Active | MIT-001, MIT-005 |
| RISK-I-002 | Secrets in audit logs | Medium | 6.8 | Active | MIT-057 |
| RISK-I-003 | Secrets in LLM prompts | Medium | 7.0 | Active | MIT-057, MIT-061 |
| RISK-I-004 | Verbose error messages expose sensitive details | Medium | 7.0 | Active | None |
| RISK-I-005 | Config file exposure | Medium | 6.8 | Active | None |
| RISK-I-006 | Temporary files contain secrets | Medium | 6.2 | Active | None |
| RISK-I-007 | Memory dump contains secrets | Medium | 5.6 | Active | None |
| RISK-I-008 | Path disclosure reveals system topology | High | 7.6 | Active | None |
| RISK-I-009 | Version information disclosure | High | 8.2 | Active | None |
| RISK-I-010 | LLM training data leakage | Medium | 5.6 | Active | MIT-001 |

#### Detailed Risk Information

##### RISK-I-001: Source code exfiltration via external LLM

- **Description**: In Burst mode, source code sent as context to external LLM could be stored,
logged, or used for training, violating intellectual property.

- **Severity**: High
- **Status**: Active
- **Owner**: security-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 9/10
- Reproducibility: 10/10
- Exploitability: 3/10
- Affected Users: 10/10
- Discoverability: 7/10
- **Average**: 7.8

**Mitigations**:
- [MIT-001](#mit-mit-001): LocalOnly operating mode default
- [MIT-005](#mit-mit-005): User consent workflow for Burst mode

**Residual Risk**: In Burst mode with user consent, code is intentionally sent externally.
Users must understand and accept this risk.


---

##### RISK-I-002: Secrets in audit logs

- **Description**: API keys, passwords, or tokens appear in audit logs due to insufficient redaction.

- **Severity**: Medium
- **Status**: Active
- **Owner**: security-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 10/10
- Reproducibility: 6/10
- Exploitability: 4/10
- Affected Users: 9/10
- Discoverability: 5/10
- **Average**: 6.8

**Mitigations**:
- [MIT-057](#mit-mit-057): Secret redaction before logging

**Residual Risk**: Novel secret formats may not match redaction patterns.


---

##### RISK-I-003: Secrets in LLM prompts

- **Description**: Secrets inadvertently included in code sent to LLM as context, exposing credentials.

- **Severity**: Medium
- **Status**: Active
- **Owner**: security-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 10/10
- Reproducibility: 7/10
- Exploitability: 4/10
- Affected Users: 8/10
- Discoverability: 6/10
- **Average**: 7.0

**Mitigations**:
- [MIT-057](#mit-mit-057): Secret redaction before logging
- [MIT-061](#mit-mit-061): Protected path enforcement prevents reading secret files

**Residual Risk**: Secrets hardcoded in source may not be detected by pattern matching.


---

##### RISK-I-004: Verbose error messages expose sensitive details

- **Description**: Error messages contain file paths, usernames, or stack traces revealing system internals.

- **Severity**: Medium
- **Status**: Active
- **Owner**: engineering-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 5/10
- Reproducibility: 9/10
- Exploitability: 6/10
- Affected Users: 7/10
- Discoverability: 8/10
- **Average**: 7.0

**Residual Risk**: Stack traces may still reveal framework internals.


---

##### RISK-I-005: Config file exposure

- **Description**: .agent/config.yml contains sensitive settings or paths that reveal system topology.

- **Severity**: Medium
- **Status**: Active
- **Owner**: security-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 6/10
- Reproducibility: 8/10
- Exploitability: 5/10
- Affected Users: 8/10
- Discoverability: 7/10
- **Average**: 6.8

**Residual Risk**: Config files inherently contain some system information.


---

##### RISK-I-006: Temporary files contain secrets

- **Description**: Temporary files created during operation contain secrets that persist after process exit.

- **Severity**: Medium
- **Status**: Active
- **Owner**: engineering-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 9/10
- Reproducibility: 5/10
- Exploitability: 6/10
- Affected Users: 7/10
- Discoverability: 4/10
- **Average**: 6.2

**Residual Risk**: Process crash may leave temp files undeleted.


---

##### RISK-I-007: Memory dump contains secrets

- **Description**: Core dump or crash dump contains secrets from process memory.

- **Severity**: Medium
- **Status**: Active
- **Owner**: infrastructure-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 9/10
- Reproducibility: 3/10
- Exploitability: 7/10
- Affected Users: 6/10
- Discoverability: 3/10
- **Average**: 5.6

**Residual Risk**: Operating system may override application core dump settings.


---

##### RISK-I-008: Path disclosure reveals system topology

- **Description**: File paths in logs/errors reveal usernames, directory structure, or OS type.

- **Severity**: High
- **Status**: Active
- **Owner**: engineering-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 4/10
- Reproducibility: 10/10
- Exploitability: 7/10
- Affected Users: 8/10
- Discoverability: 9/10
- **Average**: 7.6

**Residual Risk**: Some path disclosure is necessary for debugging.


---

##### RISK-I-009: Version information disclosure

- **Description**: Software version in error messages or headers enables targeted exploitation.

- **Severity**: High
- **Status**: Active
- **Owner**: product-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 5/10
- Reproducibility: 10/10
- Exploitability: 6/10
- Affected Users: 10/10
- Discoverability: 10/10
- **Average**: 8.2

**Residual Risk**: Version information is useful for support and debugging.


---

##### RISK-I-010: LLM training data leakage

- **Description**: External LLM trained on data sent in Burst mode, leaking information to other users.

- **Severity**: Medium
- **Status**: Active
- **Owner**: legal-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 8/10
- Reproducibility: 5/10
- Exploitability: 3/10
- Affected Users: 10/10
- Discoverability: 2/10
- **Average**: 5.6

**Mitigations**:
- [MIT-001](#mit-mit-001): LocalOnly operating mode default

**Residual Risk**: Cannot verify external LLM provider compliance with data usage policies.


---

### Denial of Service

**Risks in Category**: 7

| ID | Title | Severity | DREAD | Status | Mitigations |
|----|-------|----------|-------|--------|-------------|
| RISK-D-001 | Infinite loop in LLM-generated code | Medium | 6.6 | Active | MIT-083 |
| RISK-D-002 | Resource exhaustion via large files | Medium | 7.0 | Active | MIT-086 |
| RISK-D-003 | Memory exhaustion via LLM prompts | Medium | 6.6 | Active | None |
| RISK-D-004 | Disk exhaustion via audit logs | High | 7.2 | Active | MIT-091 |
| RISK-D-005 | CPU exhaustion via regex | Medium | 5.8 | Active | None |
| RISK-D-006 | Process fork bomb | Medium | 6.4 | Active | None |
| RISK-D-007 | Network flooding in Burst mode | Medium | 6.2 | Active | None |

#### Detailed Risk Information

##### RISK-D-001: Infinite loop in LLM-generated code

- **Description**: LLM generates code with infinite loop, causing Acode process to hang.

- **Severity**: Medium
- **Status**: Active
- **Owner**: engineering-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 5/10
- Reproducibility: 6/10
- Exploitability: 7/10
- Affected Users: 7/10
- Discoverability: 8/10
- **Average**: 6.6

**Mitigations**:
- [MIT-083](#mit-mit-083): Command execution timeout

**Residual Risk**: Complex infinite loops difficult to detect statically.


---

##### RISK-D-002: Resource exhaustion via large files

- **Description**: Acode attempts to read/process extremely large file, exhausting memory.

- **Severity**: Medium
- **Status**: Active
- **Owner**: engineering-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 6/10
- Reproducibility: 9/10
- Exploitability: 5/10
- Affected Users: 8/10
- Discoverability: 7/10
- **Average**: 7.0

**Mitigations**:
- [MIT-086](#mit-mit-086): File size limit enforcement

**Residual Risk**: Legitimate large files may need processing.


---

##### RISK-D-003: Memory exhaustion via LLM prompts

- **Description**: Extremely large context sent to LLM exhausts available memory.

- **Severity**: Medium
- **Status**: Active
- **Owner**: engineering-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 6/10
- Reproducibility: 7/10
- Exploitability: 6/10
- Affected Users: 8/10
- Discoverability: 6/10
- **Average**: 6.6

**Residual Risk**: Large codebases may legitimately need large context.


---

##### RISK-D-004: Disk exhaustion via audit logs

- **Description**: Excessive audit logging fills disk, preventing further operation.

- **Severity**: High
- **Status**: Active
- **Owner**: infrastructure-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 7/10
- Reproducibility: 8/10
- Exploitability: 5/10
- Affected Users: 9/10
- Discoverability: 7/10
- **Average**: 7.2

**Mitigations**:
- [MIT-091](#mit-mit-091): Audit log rotation policy

**Residual Risk**: Disk full is always possible with sufficient activity.


---

##### RISK-D-005: CPU exhaustion via regex

- **Description**: Maliciously crafted input causes catastrophic regex backtracking, consuming CPU.

- **Severity**: Medium
- **Status**: Active
- **Owner**: engineering-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 6/10
- Reproducibility: 5/10
- Exploitability: 7/10
- Affected Users: 7/10
- Discoverability: 4/10
- **Average**: 5.8

**Residual Risk**: Novel ReDoS patterns may bypass detection.


---

##### RISK-D-006: Process fork bomb

- **Description**: LLM-generated code or config causes Acode to spawn unbounded processes.

- **Severity**: Medium
- **Status**: Active
- **Owner**: infrastructure-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 8/10
- Reproducibility: 6/10
- Exploitability: 6/10
- Affected Users: 7/10
- Discoverability: 5/10
- **Average**: 6.4

**Residual Risk**: Operating system limits may be too high to prevent DoS.


---

##### RISK-D-007: Network flooding in Burst mode

- **Description**: Excessive API calls to external LLM in Burst mode exhaust rate limits or bandwidth.

- **Severity**: Medium
- **Status**: Active
- **Owner**: engineering-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 5/10
- Reproducibility: 7/10
- Exploitability: 6/10
- Affected Users: 6/10
- Discoverability: 7/10
- **Average**: 6.2

**Residual Risk**: External API rate limits enforced by provider, not under our control.


---

### Elevation of Privilege

**Risks in Category**: 7

| ID | Title | Severity | DREAD | Status | Mitigations |
|----|-------|----------|-------|--------|-------------|
| RISK-E-001 | Config-driven arbitrary code execution | High | 7.6 | Active | MIT-104, MIT-105 |
| RISK-E-002 | Prompt injection to command execution | Medium | 7.0 | Active | None |
| RISK-E-003 | Path traversal to system files | High | 8.4 | Active | MIT-112, MIT-113 |
| RISK-E-004 | Symlink following to protected areas | Medium | 7.0 | Active | MIT-029 |
| RISK-E-005 | YAML deserialization attacks | Medium | 6.6 | Active | MIT-104 |
| RISK-E-006 | Operating mode bypass | Medium | 6.6 | Active | None |
| RISK-E-007 | Dependency confusion leading to code execution | Medium | 6.6 | Active | None |

#### Detailed Risk Information

##### RISK-E-001: Config-driven arbitrary code execution

- **Description**: Malicious .agent/config.yml contains code execution payloads via YAML deserialization or command injection.

- **Severity**: High
- **Status**: Active
- **Owner**: security-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 10/10
- Reproducibility: 8/10
- Exploitability: 5/10
- Affected Users: 10/10
- Discoverability: 5/10
- **Average**: 7.6

**Mitigations**:
- [MIT-104](#mit-mit-104): Safe YAML parsing with no object deserialization
- [MIT-105](#mit-mit-105): JSON schema validation on config

**Residual Risk**: If config parser has vulnerabilities, arbitrary code execution possible.


---

##### RISK-E-002: Prompt injection to command execution

- **Description**: Attacker embeds malicious instructions in source code comments/strings that LLM interprets
as commands, causing Acode to execute unintended actions.

- **Severity**: Medium
- **Status**: Active
- **Owner**: ai-safety-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 9/10
- Reproducibility: 6/10
- Exploitability: 7/10
- Affected Users: 9/10
- Discoverability: 4/10
- **Average**: 7.0

**Residual Risk**: Prompt injection attacks continually evolve; detection is difficult.


---

##### RISK-E-003: Path traversal to system files

- **Description**: Attacker crafts path with ../ sequences to escape repository and access system files.

- **Severity**: High
- **Status**: Active
- **Owner**: security-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 10/10
- Reproducibility: 9/10
- Exploitability: 6/10
- Affected Users: 10/10
- Discoverability: 7/10
- **Average**: 8.4

**Mitigations**:
- [MIT-112](#mit-mit-112): Path normalization and canonicalization
- [MIT-113](#mit-mit-113): Protected path denylist enforcement

**Residual Risk**: Novel path traversal techniques may bypass validation.


---

##### RISK-E-004: Symlink following to protected areas

- **Description**: Symlink from allowed path to system file bypasses path protection via TOCTOU.

- **Severity**: Medium
- **Status**: Active
- **Owner**: security-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 9/10
- Reproducibility: 7/10
- Exploitability: 6/10
- Affected Users: 8/10
- Discoverability: 5/10
- **Average**: 7.0

**Mitigations**:
- [MIT-029](#mit-mit-029): Symlink resolution before path validation

**Residual Risk**: Complex symlink chains may evade detection.


---

##### RISK-E-005: YAML deserialization attacks

- **Description**: Malicious YAML config contains gadget chains leading to arbitrary code execution.

- **Severity**: Medium
- **Status**: Active
- **Owner**: security-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 10/10
- Reproducibility: 6/10
- Exploitability: 4/10
- Affected Users: 10/10
- Discoverability: 3/10
- **Average**: 6.6

**Mitigations**:
- [MIT-104](#mit-mit-104): Safe YAML parsing with no object deserialization

**Residual Risk**: YAML deserialization vulnerabilities are complex and evolving.


---

##### RISK-E-006: Operating mode bypass

- **Description**: Attacker bypasses LocalOnly mode to enable Burst mode without user consent.

- **Severity**: Medium
- **Status**: Active
- **Owner**: security-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 9/10
- Reproducibility: 5/10
- Exploitability: 5/10
- Affected Users: 10/10
- Discoverability: 4/10
- **Average**: 6.6

**Residual Risk**: Implementation bugs may allow mode bypass.


---

##### RISK-E-007: Dependency confusion leading to code execution

- **Description**: Malicious package installed via dependency confusion executes arbitrary code during Acode operation.

- **Severity**: Medium
- **Status**: Active
- **Owner**: infrastructure-team
- **Created**: 2026-01-03
- **Last Review**: 2026-01-03

**DREAD Score**:
- Damage: 10/10
- Reproducibility: 6/10
- Exploitability: 4/10
- Affected Users: 10/10
- Discoverability: 3/10
- **Average**: 6.6

**Residual Risk**: Supply chain attacks remain difficult to prevent completely.


---

## Mitigations

**Total Mitigations**: 21

| ID | Title | Status | Last Verified |
|----|-------|--------|---------------|
| MIT-001 | LocalOnly operating mode default | Implemented | 2026-01-03 |
| MIT-004 | TLS certificate pinning for external LLM connections | Pending | Never |
| MIT-005 | User consent workflow for Burst mode | Pending | Never |
| MIT-006 | Config file integrity checks | Pending | Never |
| MIT-007 | File permissions validation on config files | Pending | Never |
| MIT-008 | Audit logging of configuration changes | Implemented | 2026-01-03 |
| MIT-015 | .git/ protected path enforcement | Implemented | 2026-01-03 |
| MIT-020 | Tamper-evident audit logs | InProgress | 2026-01-03 |
| MIT-021 | Output sanitization and validation | Pending | Never |
| MIT-028 | Security invariants cannot be disabled | Implemented | 2026-01-03 |
| MIT-029 | Symlink resolution before path validation | Pending | Never |
| MIT-042 | Comprehensive file operation logging | Pending | Never |
| MIT-057 | Secret redaction before logging | Implemented | 2026-01-03 |
| MIT-061 | Protected path enforcement prevents reading secret files | Implemented | 2026-01-03 |
| MIT-083 | Command execution timeout | Pending | Never |
| MIT-086 | File size limit enforcement | Pending | Never |
| MIT-091 | Audit log rotation policy | Pending | Never |
| MIT-104 | Safe YAML parsing with no object deserialization | Implemented | 2026-01-03 |
| MIT-105 | JSON schema validation on config | Implemented | 2026-01-03 |
| MIT-112 | Path normalization and canonicalization | Implemented | 2026-01-03 |
| MIT-113 | Protected path denylist enforcement | Implemented | 2026-01-03 |

### Detailed Mitigation Information

#### <a name="mit-mit-001"></a>MIT-001: LocalOnly operating mode default

- **Description**: External LLM API calls disabled by default, requiring explicit user opt-in for Burst mode.
- **Status**: Implemented
- **Implementation**: src/Acode.Domain/OperatingMode/OperatingMode.cs
Default value: LocalOnly

- **Verification Test**: unit_test:OperatingModeTests.Default_Is_LocalOnly
- **Last Verified**: 2026-01-03

#### <a name="mit-mit-004"></a>MIT-004: TLS certificate pinning for external LLM connections

- **Description**: Verify external LLM TLS certificates against known pins to prevent MITM.
- **Status**: Pending
- **Implementation**: Future (Epic 1 - Model Runtime)

#### <a name="mit-mit-005"></a>MIT-005: User consent workflow for Burst mode

- **Description**: Explicit user confirmation required before enabling Burst mode.
- **Status**: Pending
- **Implementation**: Future (Epic 2 - CLI)

#### <a name="mit-mit-006"></a>MIT-006: Config file integrity checks

- **Description**: Validate config file hash/signature before loading.
- **Status**: Pending
- **Implementation**: Future (Task 002 enhancement)

#### <a name="mit-mit-007"></a>MIT-007: File permissions validation on config files

- **Description**: Verify .agent/config.yml is not world-writable.
- **Status**: Pending
- **Implementation**: Future (Task 002 enhancement)

#### <a name="mit-mit-008"></a>MIT-008: Audit logging of configuration changes

- **Description**: Log all modifications to .agent/config.yml with timestamp and user.
- **Status**: Implemented
- **Implementation**: src/Acode.Infrastructure/Audit/JsonAuditLogger.cs
Event: ConfigChange

- **Verification Test**: integration_test:ConfigChangeLogged
- **Last Verified**: 2026-01-03

#### <a name="mit-mit-015"></a>MIT-015: .git/ protected path enforcement

- **Description**: .git/ directory on denylist, preventing read/write access.
- **Status**: Implemented
- **Implementation**: src/Acode.Domain/Security/PathProtection/DefaultDenylist.cs
Pattern: .git/

- **Verification Test**: unit_test:DefaultDenylistTests.Git_Directory_Protected
- **Last Verified**: 2026-01-03

#### <a name="mit-mit-020"></a>MIT-020: Tamper-evident audit logs

- **Description**: Append-only logs with integrity checksums.
- **Status**: InProgress
- **Implementation**: src/Acode.Infrastructure/Audit/JsonAuditLogger.cs
Append-only file mode, future: checksums

- **Verification Test**: integration_test:AuditLogTamperDetection
- **Last Verified**: 2026-01-03

#### <a name="mit-mit-021"></a>MIT-021: Output sanitization and validation

- **Description**: Validate LLM responses before applying changes.
- **Status**: Pending
- **Implementation**: Future (Epic 2 - Agent Orchestration)

#### <a name="mit-mit-028"></a>MIT-028: Security invariants cannot be disabled

- **Description**: Protected paths, audit logging cannot be disabled via config.
- **Status**: Implemented
- **Implementation**: Design principle: Security controls in code, not config.
Config can add paths, not remove from denylist.

- **Verification Test**: design_review:SecurityInvariants
- **Last Verified**: 2026-01-03

#### <a name="mit-mit-029"></a>MIT-029: Symlink resolution before path validation

- **Description**: Resolve all symlinks to canonical paths before checking denylist.
- **Status**: Pending
- **Implementation**: src/Acode.Infrastructure/Security/ProtectedPathValidator.cs
Future enhancement: Path.GetFullPath with symlink resolution

- **Verification Test**: unit_test:PathValidator_Symlink_Resolution

#### <a name="mit-mit-042"></a>MIT-042: Comprehensive file operation logging

- **Description**: Log all file read/write/delete operations with path and result.
- **Status**: Pending
- **Implementation**: Future (Epic 2 - Core + Audit integration)

#### <a name="mit-mit-057"></a>MIT-057: Secret redaction before logging

- **Description**: Detect and redact secrets using regex patterns before writing to audit logs.
- **Status**: Implemented
- **Implementation**: src/Acode.Infrastructure/Security/RegexSecretRedactor.cs
Patterns: password, api_key, token, secret, private_key

- **Verification Test**: unit_test:RegexSecretRedactorTests
- **Last Verified**: 2026-01-03

#### <a name="mit-mit-061"></a>MIT-061: Protected path enforcement prevents reading secret files

- **Description**: Denylist blocks access to credential files (SSH keys, AWS credentials, etc.).
- **Status**: Implemented
- **Implementation**: src/Acode.Domain/Security/PathProtection/DefaultDenylist.cs
45 protected path patterns across 7 categories

- **Verification Test**: unit_test:DefaultDenylistTests
- **Last Verified**: 2026-01-03

#### <a name="mit-mit-083"></a>MIT-083: Command execution timeout

- **Description**: All spawned processes have 5-minute timeout by default.
- **Status**: Pending
- **Implementation**: Future (Epic 4 - Execution & Sandboxing)

#### <a name="mit-mit-086"></a>MIT-086: File size limit enforcement

- **Description**: Reject files larger than 10 MB by default.
- **Status**: Pending
- **Implementation**: Future (Epic 3 - Repo Intelligence)

#### <a name="mit-mit-091"></a>MIT-091: Audit log rotation policy

- **Description**: Rotate audit logs daily or at 100 MB, retain for 90 days.
- **Status**: Pending
- **Implementation**: Future (Task 003c - Audit Baseline)

#### <a name="mit-mit-104"></a>MIT-104: Safe YAML parsing with no object deserialization

- **Description**: Use YAML parser that only deserializes to primitives, no arbitrary types.
- **Status**: Implemented
- **Implementation**: src/Acode.Infrastructure/Configuration/YamlConfigReader.cs
YamlDotNet with SafeMode enabled

- **Verification Test**: unit_test:YamlConfigReaderTests.Deserialization_Attack_Blocked
- **Last Verified**: 2026-01-03

#### <a name="mit-mit-105"></a>MIT-105: JSON schema validation on config

- **Description**: Validate .agent/config.yml against schema before processing.
- **Status**: Implemented
- **Implementation**: src/Acode.Infrastructure/Configuration/JsonSchemaValidator.cs
data/config-schema.json

- **Verification Test**: integration_test:ConfigE2ETests.Schema_Validation
- **Last Verified**: 2026-01-03

#### <a name="mit-mit-112"></a>MIT-112: Path normalization and canonicalization

- **Description**: Convert all paths to absolute canonical form before validation.
- **Status**: Implemented
- **Implementation**: src/Acode.Infrastructure/Security/ProtectedPathValidator.cs
Path normalization with separator replacement

- **Verification Test**: unit_test:ProtectedPathValidatorTests
- **Last Verified**: 2026-01-03

#### <a name="mit-mit-113"></a>MIT-113: Protected path denylist enforcement

- **Description**: Block access to 45 protected path patterns.
- **Status**: Implemented
- **Implementation**: src/Acode.Domain/Security/PathProtection/DefaultDenylist.cs
src/Acode.Infrastructure/Security/ProtectedPathValidator.cs

- **Verification Test**: unit_test:ProtectedPathValidatorTests
- **Last Verified**: 2026-01-03

