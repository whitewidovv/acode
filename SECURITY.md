# Security Policy

## Overview

Acode is designed with security as a foundational principle. This document describes our threat model, security posture, and vulnerability reporting process.

## Threat Model

### Threat Actors

Acode's security design considers the following threat actors:

1. **External Attacker** — Remote actor attempting to compromise through network vectors
2. **Curious User** — Legitimate user attempting to bypass restrictions
3. **Malicious Insider** — Team member with access attempting misuse
4. **Compromised Dependency** — Malicious code in third-party libraries
5. **Compromised LLM** — Malicious responses from LLM provider (Burst mode)
6. **Malicious Repository** — Dangerous content in cloned repositories

### Threat Categories (STRIDE)

| Threat | Description | Primary Mitigation |
|--------|-------------|-------------------|
| **Spoofing** | Impersonation of users or processes | Process isolation, audit logging |
| **Tampering** | Modification of data or code | File integrity checks, protected paths |
| **Repudiation** | Denial of actions taken | Comprehensive audit logging |
| **Information Disclosure** | Exposure of sensitive data | Secret redaction, LocalOnly mode |
| **Denial of Service** | Resource exhaustion attacks | Timeouts, resource limits |
| **Elevation of Privilege** | Gaining unauthorized access | Least privilege, no sudo |

### Risk Assessment (DREAD)

Risks are scored using the DREAD methodology:

- **D**amage: How much damage if the vulnerability is exploited? (1-10)
- **R**eproducibility: How easy is it to reproduce the attack? (1-10)
- **E**xploitability: How much effort is required to exploit? (1-10)
- **A**ffected users: How many users would be affected? (1-10)
- **D**iscoverability: How easy is it to discover the vulnerability? (1-10)

See `docs/tasks/refined-tasks/Epic 00/task-003-threat-model-default-safety-posture.md` for the complete risk register.

## Default Security Posture

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

## Trust Boundaries

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

## Data Classification

| Data Type | Classification | Handling |
|-----------|---------------|----------|
| Source Code | Confidential | Never sent externally (LocalOnly) |
| Secrets | Secret | Never logged, always redacted |
| Config Files | Internal | Logged with redaction |
| LLM Prompts | Confidential | Logged locally, consent for external |
| LLM Responses | Internal | Logged, validated before use |
| Audit Logs | Confidential | Tamper-evident, retained |
| Command Output | Internal | Logged with redaction |

## Protected Paths

Acode protects 45 path patterns across 7 categories:

### SSH Keys (6 patterns)
- `~/.ssh/` and subdirectories
- SSH private key files (`id_rsa`, `id_ed25519`, etc.)
- SSH configuration files

### GPG Keys (2 patterns)
- `~/.gnupg/` — GPG keyring directory
- `~/.gpg/` — Alternate GPG directory

### Cloud Credentials (6 patterns)
- AWS (`~/.aws/`)
- Azure (`~/.azure/`)
- Google Cloud (`~/.gcloud/`, `~/.config/gcloud/`)
- Kubernetes (`~/.kube/`)
- Docker (`~/.docker/config.json`)

### Package Manager Credentials (9 patterns)
- npm (`~/.npmrc`)
- PyPI (`~/.pypirc`)
- NuGet (`~/.nuget/NuGet.Config`)
- RubyGems (`~/.gem/credentials`)
- Cargo (`~/.cargo/credentials`)
- Composer (`~/.composer/auth.json`)
- Maven (`~/.m2/settings.xml`)
- Gradle (`~/.gradle/gradle.properties`)
- GitHub CLI (`~/.config/gh/hosts.yml`)

### Git Credentials (2 patterns)
- `~/.gitconfig` — May contain credential helpers
- `~/.git-credentials` — Plaintext credentials

### Environment Files (5 patterns)
- `.env` and `.env.*` variants
- `secrets/` directories
- `private/` directories

### Secret Files (2 patterns)
- `**/*.pem` — Certificate files
- `**/*.key` — Private key files

### System Files (13 patterns)
- Unix: `/etc/`, `/root/`, critical system files
- Windows: `C:\Windows\`, `C:\System32\`, `C:\ProgramData\`
- macOS: `/System/`, `/Library/`, `~/Library/`

To view the complete denylist:
```bash
dotnet run --project src/Acode.Cli
```

To check if a specific path is protected, use the SecurityCommand.

## Security Invariants

These security properties MUST always hold:

1. **No External LLM in LocalOnly Mode** — External LLM API calls are impossible in LocalOnly mode
2. **Protected Paths Are Denied** — Denylist patterns cannot be bypassed
3. **Secrets Are Never Logged** — Secret data never appears in audit logs
4. **All Operations Are Logged** — Every file access, command execution, and LLM interaction is audited
5. **No Privilege Escalation** — Acode runs as regular user, never requests sudo
6. **Config Is Validated** — Invalid configurations are rejected at startup
7. **Fail-Safe Behavior** — Security failures block operations, never grant access

## Vulnerability Disclosure

### Reporting Security Issues

**DO NOT** open public GitHub issues for security vulnerabilities.

Instead, please report security issues via:

1. **GitHub Security Advisory** (preferred): https://github.com/whitewidovv/acode/security/advisories/new
2. **Email**: security@acode.dev (if configured)

### What to Include

Please include:

- Description of the vulnerability
- Steps to reproduce
- Affected versions
- Proposed fix (if you have one)
- Your contact information (for follow-up)

### Response Timeline

- **Initial Response**: Within 48 hours
- **Triage**: Within 1 week
- **Fix Timeline**: Depends on severity
  - Critical: 7 days
  - High: 14 days
  - Medium: 30 days
  - Low: 90 days

### Security Updates

Security fixes are released as:
- **Patch releases** (e.g., 1.0.1) for production versions
- **Minor releases** (e.g., 1.1.0) if breaking changes are required
- **Security advisories** published via GitHub Security Advisories

### CVE Process

For vulnerabilities meeting CVE criteria, we will:
1. Request a CVE ID from MITRE
2. Coordinate disclosure with CVE publication
3. Credit the reporter (if desired)

## Security Best Practices

### For Users

1. **Stay in LocalOnly mode** — Only use Burst mode when necessary
2. **Review prompts before Burst** — Know what data is being sent
3. **Keep protected paths updated** — Add custom sensitive paths to `.agent/config.yml`
4. **Enable audit logging** — Review logs regularly
5. **Update regularly** — Install security patches promptly
6. **Run as regular user** — Never run Acode with elevated privileges
7. **Verify configuration** — Use `dotnet run --project src/Acode.Cli -- config validate`

### For Contributors

1. **Never bypass security controls** — Security invariants cannot be disabled
2. **Add tests for security features** — All security code requires tests
3. **Document threat mitigations** — Link code to threat model entries
4. **Use least privilege** — Request minimum necessary permissions
5. **Fail securely** — Default to deny on errors
6. **Log security events** — Audit all security-relevant actions
7. **Review dependencies** — Check for known vulnerabilities

## Security Architecture

Acode follows Clean Architecture with strict layer boundaries:

```
┌─────────────────────────────────────────┐
│            CLI Layer                    │  ← User commands
│         (Acode.Cli)                     │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│       Infrastructure Layer              │  ← External integrations
│     (Acode.Infrastructure)              │     (File system, Process, Audit)
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│       Application Layer                 │  ← Use cases, interfaces
│      (Acode.Application)                │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│         Domain Layer                    │  ← Business logic, rules
│        (Acode.Domain)                   │     (No dependencies)
└─────────────────────────────────────────┘
```

Security controls are implemented at appropriate layers:
- **Domain**: Threat model types, risk scoring, security policy rules
- **Application**: Security interfaces (IProtectedPathValidator, IAuditLogger, ISecretRedactor)
- **Infrastructure**: Concrete implementations (RegexSecretRedactor, JsonAuditLogger)
- **CLI**: Security commands, user-facing security operations

## Audit Logging

All security-relevant events are logged to `.agent/audit.jsonl` (JSON Lines format):

- File access attempts (allowed and denied)
- Command executions
- Configuration changes
- Security policy violations
- LLM interactions (prompts and responses)
- Operating mode changes

Audit logs include:
- Timestamp (ISO 8601)
- Event ID (unique)
- Session ID (links related events)
- Correlation ID (for distributed tracing)
- Event type
- Severity (Info, Warning, Error, Critical)
- Source (component generating event)
- Operating mode
- Event-specific data

## Secret Detection

Acode uses regex-based patterns to detect and redact secrets:

1. **Password patterns**: `password=`, `pwd:`, etc.
2. **API key patterns**: `api_key=`, `apikey:`, etc. (minimum 10 characters)
3. **Token patterns**: JWT tokens, Bearer tokens, 32+ character tokens
4. **Generic secrets**: `secret=`, `credential:`, etc.
5. **Private keys**: PEM-format private keys

Detected secrets are replaced with `[REDACTED]` in logs and output.

## Compliance

Acode's security design supports:

- **SOC2 Type II**: Audit logging, access controls, security monitoring
- **ISO 27001**: Risk assessment, security controls, incident management
- **GDPR**: Data minimization, purpose limitation, security by design
- **NIST Cybersecurity Framework**: Identify, Protect, Detect, Respond, Recover

## Supported Versions

| Version | Supported |
|---------|-----------|
| 0.1.0-alpha | ✅ Yes (current) |

## Security Roadmap

Future security enhancements (see Epic 9 in task list):

- [ ] Policy engine with custom rules
- [ ] Secrets detection in git history
- [ ] Runtime security monitoring
- [ ] Anomaly detection
- [ ] Security metrics dashboard
- [ ] Integration with SIEM systems
- [ ] Code signing and verification
- [ ] Supply chain security (SBOM)

## Credits

Security design by the Acode team with input from:
- OWASP Application Security Verification Standard (ASVS)
- STRIDE threat modeling methodology
- DREAD risk assessment framework
- Microsoft Security Development Lifecycle (SDL)

## Attribution

We believe in recognizing security researchers. With your permission, we will:

- Credit you in release notes
- Add you to our security acknowledgments page
- Thank you publicly (if you wish)

## License

This security policy is part of the Acode project. See LICENSE file for details.

---

**Last Updated**: 2026-01-03
**Version**: 1.0.0
**Contact**: Project maintainers via GitHub

**Thank you for helping keep Acode and its users safe!**
