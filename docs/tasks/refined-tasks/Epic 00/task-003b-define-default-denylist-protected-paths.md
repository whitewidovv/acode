# Task 003.b: Define Default Denylist + Protected Paths

**Priority:** 14 / 49  
**Tier:** Foundation  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 0 — Foundation  
**Dependencies:** Task 003 (threat model), Task 003.a (risk enumeration)  

---

## Description

### Overview

Task 003.b defines the default denylist and protected paths that Acode MUST NOT read, modify, or expose. These paths contain sensitive system files, credentials, and private data that pose significant security risks if accessed by an AI coding assistant. The denylist operates as a security control mitigating path traversal, information disclosure, and privilege escalation risks identified in Task 003.a.

Protected paths are enforced at the deepest level of file access—any attempt to read or write protected paths is blocked before the operation can execute. This is a fail-closed design: if there is any doubt about path safety, access is denied.

### Business Value

Default denylists provide:

1. **Automatic Protection** — Users are protected without any configuration
2. **Credential Safety** — SSH keys, API tokens, and passwords protected by default
3. **System Integrity** — System files cannot be modified
4. **Enterprise Confidence** — Security teams can verify protected paths
5. **Compliance Support** — Demonstrates data protection controls
6. **Trust Building** — Users can verify what is protected

### Scope Boundaries

**In Scope:**
- Default denylist of protected paths
- Path matching rules (glob patterns, directories)
- Cross-platform path definitions
- User-extensible protected paths
- Protected path verification commands
- Path access logging
- Denylist bypass prevention

**Out of Scope:**
- Path validation implementation details (Task 002.b)
- Allowlist for permitted paths
- Runtime path access control
- File system monitoring
- Access control lists (ACLs)

### Integration Points

| Task | Relationship | Description |
|------|--------------|-------------|
| Task 003 | Parent | Security posture |
| Task 003.a | Sibling | Mitigates path risks |
| Task 002.b | Consumer | Validation uses denylist |
| All file ops | Consumer | All access checked |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Denylist incomplete | Sensitive file exposed | Comprehensive list |
| Pattern too broad | Legitimate files blocked | Test patterns |
| Pattern too narrow | Bypass possible | Defense in depth |
| Platform mismatch | Path not matched | Cross-platform testing |

### Assumptions

1. Standard OS paths are known and documented
2. Common credential file locations are known
3. Glob patterns are sufficient for matching
4. Paths can be normalized for comparison
5. Denylist can be extended but not reduced

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| **Denylist** | List of paths that MUST NOT be accessed |
| **Protected Path** | Path on the denylist |
| **Glob Pattern** | Wildcard pattern for path matching |
| **Absolute Path** | Full path from filesystem root |
| **Relative Path** | Path relative to working directory |
| **Normalized Path** | Path with resolved symlinks and canonicalization |
| **Path Traversal** | Escaping intended directory via ../ |
| **Symlink** | Symbolic link to another file/directory |
| **Hardlink** | Additional directory entry for same file |
| **Home Directory** | User's home folder (~) |
| **Repository Root** | Top-level directory of the repository |
| **System Directory** | OS-level protected directory |
| **Credential File** | File containing secrets or keys |
| **Fail-Closed** | Deny access on any error or doubt |

---

## Out of Scope

- Allowlist definitions (inverse of denylist)
- File content inspection for secrets
- Real-time file system monitoring
- Access control list (ACL) management
- File permission modification
- Encryption of protected files
- Backup and recovery of protected files
- Remote file system protection
- Container/VM file isolation
- Memory protection for loaded secrets
- Secure deletion of files
- Time-based access controls

---

## Functional Requirements

### Default Denylist Definition (FR-003b-01 to FR-003b-25)

| ID | Requirement |
|----|-------------|
| FR-003b-01 | System MUST have default denylist of protected paths |
| FR-003b-02 | Denylist MUST be enforced without user configuration |
| FR-003b-03 | Denylist MUST be defined in code (not user-editable) |
| FR-003b-04 | Denylist MUST include all paths from this specification |
| FR-003b-05 | Denylist MUST be viewable via CLI command |
| FR-003b-06 | Denylist MUST NOT be reducible by user |
| FR-003b-07 | Denylist MUST be extensible by user (add only) |
| FR-003b-08 | Extended denylist MUST be in config file |
| FR-003b-09 | Denylist MUST be platform-aware |
| FR-003b-10 | Windows paths MUST use Windows conventions |
| FR-003b-11 | Unix paths MUST use Unix conventions |
| FR-003b-12 | Denylist MUST support glob patterns |
| FR-003b-13 | Denylist MUST support directory prefixes |
| FR-003b-14 | Denylist MUST support exact matches |
| FR-003b-15 | Denylist entries MUST have documented reason |
| FR-003b-16 | Denylist MUST be versioned with codebase |
| FR-003b-17 | Denylist changes MUST be in CHANGELOG |
| FR-003b-18 | Denylist MUST be testable |
| FR-003b-19 | Denylist MUST be auditable |
| FR-003b-20 | Denylist MUST be documented in SECURITY.md |
| FR-003b-21 | Denylist MUST cover cross-platform equivalents |
| FR-003b-22 | Denylist MUST be checked before any file read |
| FR-003b-23 | Denylist MUST be checked before any file write |
| FR-003b-24 | Denylist violations MUST be logged |
| FR-003b-25 | Denylist violations MUST return clear error |

### SSH and Credential Paths (FR-003b-26 to FR-003b-40)

| ID | Requirement |
|----|-------------|
| FR-003b-26 | ~/.ssh/ MUST be protected |
| FR-003b-27 | ~/.ssh/id_* MUST be protected |
| FR-003b-28 | ~/.ssh/known_hosts MUST be protected |
| FR-003b-29 | ~/.ssh/authorized_keys MUST be protected |
| FR-003b-30 | ~/.ssh/config MUST be protected |
| FR-003b-31 | ~/.gnupg/ MUST be protected |
| FR-003b-32 | ~/.gpg/ MUST be protected |
| FR-003b-33 | ~/.aws/ MUST be protected |
| FR-003b-34 | ~/.aws/credentials MUST be protected |
| FR-003b-35 | ~/.azure/ MUST be protected |
| FR-003b-36 | ~/.gcloud/ MUST be protected |
| FR-003b-37 | ~/.config/gcloud/ MUST be protected |
| FR-003b-38 | ~/.kube/ MUST be protected |
| FR-003b-39 | ~/.docker/config.json MUST be protected |
| FR-003b-40 | %USERPROFILE%\.ssh\ MUST be protected (Windows) |

### Token and Key Files (FR-003b-41 to FR-003b-55)

| ID | Requirement |
|----|-------------|
| FR-003b-41 | ~/.netrc MUST be protected |
| FR-003b-42 | ~/.npmrc MUST be protected (contains tokens) |
| FR-003b-43 | ~/.pypirc MUST be protected |
| FR-003b-44 | ~/.nuget/NuGet.Config MUST be protected |
| FR-003b-45 | ~/.gem/credentials MUST be protected |
| FR-003b-46 | ~/.cargo/credentials MUST be protected |
| FR-003b-47 | ~/.composer/auth.json MUST be protected |
| FR-003b-48 | ~/.m2/settings.xml MUST be protected |
| FR-003b-49 | ~/.gradle/gradle.properties MUST be protected |
| FR-003b-50 | ~/.config/gh/hosts.yml MUST be protected |
| FR-003b-51 | ~/.gitconfig MUST be protected (may contain credentials) |
| FR-003b-52 | ~/.git-credentials MUST be protected |
| FR-003b-53 | **/token MUST be protected (common token file name) |
| FR-003b-54 | **/*.pem MUST be protected |
| FR-003b-55 | **/*.key MUST be protected |

### System Directories (FR-003b-56 to FR-003b-70)

| ID | Requirement |
|----|-------------|
| FR-003b-56 | /etc/ MUST be protected (Unix) |
| FR-003b-57 | /etc/passwd MUST be protected |
| FR-003b-58 | /etc/shadow MUST be protected |
| FR-003b-59 | /etc/sudoers MUST be protected |
| FR-003b-60 | /etc/ssh/ MUST be protected |
| FR-003b-61 | /var/log/ MUST be protected |
| FR-003b-62 | /root/ MUST be protected |
| FR-003b-63 | C:\Windows\ MUST be protected |
| FR-003b-64 | C:\Windows\System32\ MUST be protected |
| FR-003b-65 | C:\Windows\SysWOW64\ MUST be protected |
| FR-003b-66 | C:\ProgramData\ MUST be protected |
| FR-003b-67 | C:\Users\*\AppData\ MUST be protected |
| FR-003b-68 | /System/ MUST be protected (macOS) |
| FR-003b-69 | /Library/ MUST be protected (macOS) |
| FR-003b-70 | ~/Library/ MUST be protected (macOS) |

### Repository-Relative Protected Paths (FR-003b-71 to FR-003b-85)

| ID | Requirement |
|----|-------------|
| FR-003b-71 | .env MUST be protected |
| FR-003b-72 | .env.* MUST be protected (all variants) |
| FR-003b-73 | .env.local MUST be protected |
| FR-003b-74 | .env.production MUST be protected |
| FR-003b-75 | **/.env MUST be protected (nested) |
| FR-003b-76 | secrets/ MUST be protected |
| FR-003b-77 | **/secrets/ MUST be protected |
| FR-003b-78 | private/ MUST be protected |
| FR-003b-79 | **/private/ MUST be protected |
| FR-003b-80 | *.secrets MUST be protected |
| FR-003b-81 | *.secret MUST be protected |
| FR-003b-82 | *secret*.json MUST be protected |
| FR-003b-83 | *credential*.json MUST be protected |
| FR-003b-84 | appsettings.*.json MUST be inspected (may contain secrets) |
| FR-003b-85 | **/node_modules/ MAY be excluded (not protected, just large) |

### Path Matching Rules (FR-003b-86 to FR-003b-100)

| ID | Requirement |
|----|-------------|
| FR-003b-86 | Paths MUST be normalized before matching |
| FR-003b-87 | Symlinks MUST be resolved before matching |
| FR-003b-88 | Case sensitivity MUST match platform |
| FR-003b-89 | Windows paths MUST be case-insensitive |
| FR-003b-90 | Unix paths MUST be case-sensitive |
| FR-003b-91 | Trailing slashes MUST be normalized |
| FR-003b-92 | Forward slashes MUST work on all platforms |
| FR-003b-93 | .. components MUST be resolved |
| FR-003b-94 | . components MUST be removed |
| FR-003b-95 | Glob * MUST match any characters except separator |
| FR-003b-96 | Glob ** MUST match any path including separators |
| FR-003b-97 | Glob ? MUST match single character |
| FR-003b-98 | Glob [abc] MUST match character class |
| FR-003b-99 | Matching MUST be O(n) or better |
| FR-003b-100 | Matching MUST complete in < 1ms per path |

### Access Control Behavior (FR-003b-101 to FR-003b-115)

| ID | Requirement |
|----|-------------|
| FR-003b-101 | Protected path read MUST return ProtectedPathError |
| FR-003b-102 | Protected path write MUST return ProtectedPathError |
| FR-003b-103 | Protected path delete MUST return ProtectedPathError |
| FR-003b-104 | Protected path list MUST return ProtectedPathError |
| FR-003b-105 | Error message MUST identify blocked path |
| FR-003b-106 | Error message MUST NOT reveal full path contents |
| FR-003b-107 | Error MUST include error code ACODE-SEC-003 |
| FR-003b-108 | Error MUST be logged with structured data |
| FR-003b-109 | Repeated violations MUST be rate-limited in logs |
| FR-003b-110 | Violation count MUST be tracked |
| FR-003b-111 | Violations MUST NOT crash the application |
| FR-003b-112 | Violations MUST fail the current operation only |
| FR-003b-113 | Partial access MUST NOT be allowed |
| FR-003b-114 | Directory traversal into protected MUST be blocked |
| FR-003b-115 | Symlink to protected path MUST be blocked |

---

## Non-Functional Requirements

### Performance (NFR-003b-01 to NFR-003b-10)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-003b-01 | Performance | Path check MUST complete in < 1ms |
| NFR-003b-02 | Performance | Denylist lookup MUST be O(1) or O(log n) |
| NFR-003b-03 | Performance | Glob matching MUST not cause backtracking |
| NFR-003b-04 | Performance | Path normalization MUST be cached |
| NFR-003b-05 | Performance | Symlink resolution MUST be cached |
| NFR-003b-06 | Performance | Cache MUST have configurable TTL |
| NFR-003b-07 | Performance | Default cache TTL MUST be 5 seconds |
| NFR-003b-08 | Performance | Cache invalidation MUST be explicit |
| NFR-003b-09 | Performance | Memory for denylist MUST be < 1MB |
| NFR-003b-10 | Performance | No allocation per path check |

### Security (NFR-003b-11 to NFR-003b-25)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-003b-11 | Security | Denylist MUST be immutable at runtime |
| NFR-003b-12 | Security | No bypass mechanism MUST exist |
| NFR-003b-13 | Security | Symlink attacks MUST be prevented |
| NFR-003b-14 | Security | TOCTOU attacks MUST be mitigated |
| NFR-003b-15 | Security | Race conditions MUST be handled |
| NFR-003b-16 | Security | Path comparison MUST use constant-time |
| NFR-003b-17 | Security | Error messages MUST NOT leak path info |
| NFR-003b-18 | Security | Logs MUST NOT contain protected content |
| NFR-003b-19 | Security | Denylist changes MUST be auditable |
| NFR-003b-20 | Security | All code MUST have security review |
| NFR-003b-21 | Security | Fail-closed MUST be default |
| NFR-003b-22 | Security | Unknown paths MUST be blocked |
| NFR-003b-23 | Security | Configuration errors MUST block access |
| NFR-003b-24 | Security | Security tests MUST have 100% coverage |
| NFR-003b-25 | Security | Penetration tests MUST verify protection |

### Reliability (NFR-003b-26 to NFR-003b-35)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-003b-26 | Reliability | Path check MUST NOT throw exceptions |
| NFR-003b-27 | Reliability | Invalid paths MUST be handled |
| NFR-003b-28 | Reliability | Null paths MUST return blocked |
| NFR-003b-29 | Reliability | Empty paths MUST return blocked |
| NFR-003b-30 | Reliability | Very long paths MUST be handled |
| NFR-003b-31 | Reliability | Unicode paths MUST be normalized |
| NFR-003b-32 | Reliability | Special characters MUST be escaped |
| NFR-003b-33 | Reliability | Circular symlinks MUST be detected |
| NFR-003b-34 | Reliability | Max symlink depth MUST be 40 |
| NFR-003b-35 | Reliability | All paths MUST be validated |

### Maintainability (NFR-003b-36 to NFR-003b-45)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-003b-36 | Maintainability | Denylist MUST be in single location |
| NFR-003b-37 | Maintainability | Each entry MUST be documented |
| NFR-003b-38 | Maintainability | Entries MUST have reason code |
| NFR-003b-39 | Maintainability | Entries MUST reference risk IDs |
| NFR-003b-40 | Maintainability | Tests MUST cover each entry |
| NFR-003b-41 | Maintainability | Changes MUST update documentation |
| NFR-003b-42 | Maintainability | Version history MUST be tracked |
| NFR-003b-43 | Maintainability | Code MUST follow style guide |
| NFR-003b-44 | Maintainability | Comments MUST explain security rationale |
| NFR-003b-45 | Maintainability | Interface MUST be stable |

### Compatibility (NFR-003b-46 to NFR-003b-55)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-003b-46 | Compatibility | Windows MUST be supported |
| NFR-003b-47 | Compatibility | Linux MUST be supported |
| NFR-003b-48 | Compatibility | macOS MUST be supported |
| NFR-003b-49 | Compatibility | WSL MUST be supported |
| NFR-003b-50 | Compatibility | Docker containers MUST be supported |
| NFR-003b-51 | Compatibility | Network paths MUST be blocked |
| NFR-003b-52 | Compatibility | UNC paths MUST be handled (Windows) |
| NFR-003b-53 | Compatibility | Extended paths MUST be handled (\\?\) |
| NFR-003b-54 | Compatibility | Device paths MUST be blocked |
| NFR-003b-55 | Compatibility | Named pipes MUST be blocked |

---

## User Manual Documentation

### Overview

The Agentic Coding Bot implements a comprehensive denylist system that prevents access to sensitive files and directories. This protection is active by default and cannot be disabled, ensuring that credentials, private keys, and system files are never exposed to AI-generated code execution.

### Security Model

The denylist operates on a **fail-closed** principle:
- Any path that matches a denylist pattern is blocked
- Any path that cannot be validated is blocked
- Symbolic links pointing to protected paths are blocked
- Directory traversal into protected paths is blocked

### Default Protected Categories

#### 1. SSH and GPG Keys

These paths contain cryptographic keys that could compromise system security:

| Pattern | Platform | Reason |
|---------|----------|--------|
| `~/.ssh/` | Unix/macOS | SSH private keys, known hosts |
| `~/.gnupg/` | Unix/macOS | GPG keyrings and trust database |
| `~/.gpg/` | Unix/macOS | Alternate GPG directory |
| `%USERPROFILE%\.ssh\` | Windows | SSH keys on Windows |
| `%USERPROFILE%\.gnupg\` | Windows | GPG on Windows |

#### 2. Cloud Provider Credentials

These directories contain cloud platform authentication tokens:

| Pattern | Service | Contains |
|---------|---------|----------|
| `~/.aws/` | AWS | Access keys, session tokens |
| `~/.azure/` | Azure | Service principal credentials |
| `~/.gcloud/` | GCP | OAuth tokens |
| `~/.config/gcloud/` | GCP | Application default credentials |
| `~/.kube/` | Kubernetes | Cluster configs and tokens |

#### 3. Package Manager Credentials

These files may contain registry authentication tokens:

| Pattern | Package Manager | Risk |
|---------|-----------------|------|
| `~/.npmrc` | npm | Registry auth tokens |
| `~/.pypirc` | pip | PyPI credentials |
| `~/.nuget/NuGet.Config` | NuGet | Feed credentials |
| `~/.gem/credentials` | RubyGems | API keys |
| `~/.cargo/credentials` | Cargo | Registry tokens |
| `~/.m2/settings.xml` | Maven | Repository credentials |

#### 4. Git Credentials

| Pattern | Contains |
|---------|----------|
| `~/.gitconfig` | May contain credential helpers |
| `~/.git-credentials` | Stored plaintext credentials |
| `~/.netrc` | Legacy credential storage |

#### 5. System Directories

**Unix/Linux:**
| Pattern | Protected Resource |
|---------|-------------------|
| `/etc/passwd` | User accounts |
| `/etc/shadow` | Password hashes |
| `/etc/sudoers` | Sudo privileges |
| `/etc/ssh/` | System SSH keys |
| `/root/` | Root home directory |
| `/var/log/` | System logs |

**Windows:**
| Pattern | Protected Resource |
|---------|-------------------|
| `C:\Windows\` | System files |
| `C:\Windows\System32\` | Core binaries |
| `C:\ProgramData\` | Application data |
| `C:\Users\*\AppData\` | User application data |

**macOS:**
| Pattern | Protected Resource |
|---------|-------------------|
| `/System/` | macOS system files |
| `/Library/` | System libraries |
| `~/Library/` | User libraries and prefs |

#### 6. Environment Files

| Pattern | Typically Contains |
|---------|-------------------|
| `.env` | Environment variables |
| `.env.*` | Environment-specific config |
| `secrets/` | Secret files directory |
| `private/` | Private files directory |
| `*.pem` | Certificate files |
| `*.key` | Private key files |

### Viewing the Denylist

```bash
# Show all default protected paths
agentic-coder security show-denylist

# Show denylist for specific platform
agentic-coder security show-denylist --platform linux
agentic-coder security show-denylist --platform windows
agentic-coder security show-denylist --platform macos

# Show with pattern details
agentic-coder security show-denylist --verbose

# Export as JSON
agentic-coder security show-denylist --format json
```

**Example Output:**

```
Default Protected Paths (cannot be modified)
============================================

SSH & GPG Keys:
  ~/.ssh/              Block all SSH-related files
  ~/.ssh/id_*          SSH private keys
  ~/.gnupg/            GPG keyring directory
  
Cloud Credentials:
  ~/.aws/              AWS credentials and config
  ~/.azure/            Azure CLI credentials
  ~/.gcloud/           Google Cloud credentials
  ~/.kube/             Kubernetes configs
  
Package Credentials:
  ~/.npmrc             npm registry tokens
  ~/.pypirc            PyPI credentials
  ...

User Extended Paths (from .agent/config.yml):
  company-secrets/     Added by user
```

### Extending the Denylist

Users can add additional paths via `.agent/config.yml`:

```yaml
security:
  additional_protected_paths:
    - pattern: "company-secrets/"
      reason: "Internal documentation"
    - pattern: "*.license"
      reason: "License keys"
    - pattern: ".internal/"
      reason: "Internal tools"
```

**Note:** Users can only ADD to the denylist, never remove from it.

### Checking Path Protection Status

```bash
# Check if a specific path is protected
agentic-coder security check-path ~/.ssh/id_rsa
# Output: ✗ BLOCKED - SSH private key (default denylist)

agentic-coder security check-path ./src/main.cs
# Output: ✓ ALLOWED - Not in denylist

agentic-coder security check-path ../../../etc/passwd
# Output: ✗ BLOCKED - System password file (default denylist)
```

### Error Messages

When accessing a protected path, the system returns clear errors:

```
Error: Access denied to protected path
Code: ACODE-SEC-003
Path: [REDACTED - matches protected pattern]
Reason: Path matches default denylist pattern for SSH credentials
Action: This path cannot be accessed for security reasons.
        If you need to work with this file, use external tools
        and manually verify the operation.
```

### Logging

All denylist violations are logged:

```json
{
  "timestamp": "2024-01-15T10:30:00Z",
  "event": "protected_path_access_blocked",
  "code": "ACODE-SEC-003",
  "severity": "warning",
  "pattern_matched": "~/.ssh/*",
  "operation": "read",
  "risk_id": "RISK-E-003",
  "mitigation": "MIT-006"
}
```

---

## Acceptance Criteria

### Default Denylist Implementation

- [ ] AC-001: Default denylist exists in source code
- [ ] AC-002: Denylist is loaded at startup
- [ ] AC-003: Denylist cannot be modified at runtime
- [ ] AC-004: Denylist is not user-editable
- [ ] AC-005: Denylist is applied to all file operations
- [ ] AC-006: Denylist includes all specified SSH paths
- [ ] AC-007: Denylist includes all specified GPG paths
- [ ] AC-008: Denylist includes all specified cloud credential paths
- [ ] AC-009: Denylist includes all specified package manager paths
- [ ] AC-010: Denylist includes all specified system paths
- [ ] AC-011: Denylist includes all specified environment files
- [ ] AC-012: Each entry has documented reason
- [ ] AC-013: Each entry references risk ID
- [ ] AC-014: Denylist is documented in SECURITY.md
- [ ] AC-015: Denylist version is tracked

### SSH and GPG Paths

- [ ] AC-016: ~/.ssh/ is blocked
- [ ] AC-017: ~/.ssh/id_rsa is blocked
- [ ] AC-018: ~/.ssh/id_ed25519 is blocked
- [ ] AC-019: ~/.ssh/id_ecdsa is blocked
- [ ] AC-020: ~/.ssh/known_hosts is blocked
- [ ] AC-021: ~/.ssh/authorized_keys is blocked
- [ ] AC-022: ~/.ssh/config is blocked
- [ ] AC-023: ~/.gnupg/ is blocked
- [ ] AC-024: ~/.gpg/ is blocked
- [ ] AC-025: %USERPROFILE%\.ssh\ is blocked (Windows)
- [ ] AC-026: C:\Users\*\.ssh\ is blocked (Windows)

### Cloud Credential Paths

- [ ] AC-027: ~/.aws/ is blocked
- [ ] AC-028: ~/.aws/credentials is blocked
- [ ] AC-029: ~/.aws/config is blocked
- [ ] AC-030: ~/.azure/ is blocked
- [ ] AC-031: ~/.gcloud/ is blocked
- [ ] AC-032: ~/.config/gcloud/ is blocked
- [ ] AC-033: ~/.kube/ is blocked
- [ ] AC-034: ~/.kube/config is blocked
- [ ] AC-035: ~/.docker/config.json is blocked
- [ ] AC-036: Cloud paths on Windows are blocked

### Package Manager Credentials

- [ ] AC-037: ~/.npmrc is blocked
- [ ] AC-038: ~/.pypirc is blocked
- [ ] AC-039: ~/.nuget/NuGet.Config is blocked
- [ ] AC-040: ~/.gem/credentials is blocked
- [ ] AC-041: ~/.cargo/credentials is blocked
- [ ] AC-042: ~/.composer/auth.json is blocked
- [ ] AC-043: ~/.m2/settings.xml is blocked
- [ ] AC-044: ~/.gradle/gradle.properties is blocked
- [ ] AC-045: ~/.config/gh/hosts.yml is blocked

### Git Credentials

- [ ] AC-046: ~/.gitconfig is blocked
- [ ] AC-047: ~/.git-credentials is blocked
- [ ] AC-048: ~/.netrc is blocked
- [ ] AC-049: .git/config with credentials is blocked
- [ ] AC-050: Credential helper output is not logged

### System Directories (Unix)

- [ ] AC-051: /etc/passwd is blocked
- [ ] AC-052: /etc/shadow is blocked
- [ ] AC-053: /etc/sudoers is blocked
- [ ] AC-054: /etc/sudoers.d/ is blocked
- [ ] AC-055: /etc/ssh/ is blocked
- [ ] AC-056: /etc/ssl/private/ is blocked
- [ ] AC-057: /root/ is blocked
- [ ] AC-058: /var/log/ is blocked

### System Directories (Windows)

- [ ] AC-059: C:\Windows\ is blocked
- [ ] AC-060: C:\Windows\System32\ is blocked
- [ ] AC-061: C:\Windows\SysWOW64\ is blocked
- [ ] AC-062: C:\ProgramData\ is blocked
- [ ] AC-063: C:\Users\*\AppData\ is blocked
- [ ] AC-064: Windows Registry paths are blocked

### System Directories (macOS)

- [ ] AC-065: /System/ is blocked
- [ ] AC-066: /Library/ is blocked
- [ ] AC-067: ~/Library/ is blocked
- [ ] AC-068: ~/Library/Keychains/ is blocked
- [ ] AC-069: /private/var/ is blocked

### Environment Files

- [ ] AC-070: .env is blocked
- [ ] AC-071: .env.local is blocked
- [ ] AC-072: .env.development is blocked
- [ ] AC-073: .env.production is blocked
- [ ] AC-074: .env.* pattern matches all variants
- [ ] AC-075: **/.env matches nested files
- [ ] AC-076: secrets/ directory is blocked
- [ ] AC-077: **/secrets/ matches nested directories
- [ ] AC-078: private/ directory is blocked
- [ ] AC-079: **/private/ matches nested directories
- [ ] AC-080: *.pem files are blocked
- [ ] AC-081: *.key files are blocked
- [ ] AC-082: *.p12 files are blocked
- [ ] AC-083: *.pfx files are blocked
- [ ] AC-084: *.jks files are blocked

### Glob Pattern Matching

- [ ] AC-085: Single * matches any chars except separator
- [ ] AC-086: Double ** matches any path segments
- [ ] AC-087: ? matches single character
- [ ] AC-088: [abc] matches character class
- [ ] AC-089: [a-z] matches character range
- [ ] AC-090: Glob patterns are case-aware per platform
- [ ] AC-091: Negation patterns are not supported
- [ ] AC-092: Brace expansion is not supported
- [ ] AC-093: Pattern matching does not backtrack

### Path Normalization

- [ ] AC-094: Paths are normalized before matching
- [ ] AC-095: Trailing slashes are removed
- [ ] AC-096: Multiple slashes are collapsed
- [ ] AC-097: . components are removed
- [ ] AC-098: .. components are resolved
- [ ] AC-099: ~ is expanded to home directory
- [ ] AC-100: %USERPROFILE% is expanded (Windows)
- [ ] AC-101: $HOME is expanded (Unix)
- [ ] AC-102: Forward slashes work on Windows
- [ ] AC-103: Backslashes work on Windows

### Symlink Handling

- [ ] AC-104: Symlinks are resolved before matching
- [ ] AC-105: Symlink to protected path is blocked
- [ ] AC-106: Chain of symlinks is resolved
- [ ] AC-107: Circular symlinks are detected
- [ ] AC-108: Max symlink depth of 40 is enforced
- [ ] AC-109: Symlink resolution errors block access
- [ ] AC-110: Hardlinks are handled (if detectable)

### Access Control Behavior

- [ ] AC-111: Protected path read returns error
- [ ] AC-112: Protected path write returns error
- [ ] AC-113: Protected path delete returns error
- [ ] AC-114: Protected path list returns error
- [ ] AC-115: Error includes code ACODE-SEC-003
- [ ] AC-116: Error message is human-readable
- [ ] AC-117: Error does not reveal file contents
- [ ] AC-118: Error does not reveal full path
- [ ] AC-119: Partial access is never allowed
- [ ] AC-120: Directory traversal is blocked

### User Extension

- [ ] AC-121: Users can add to denylist
- [ ] AC-122: Users cannot remove from denylist
- [ ] AC-123: Extension is in config file
- [ ] AC-124: Extension format is validated
- [ ] AC-125: Invalid extensions are rejected
- [ ] AC-126: Extensions require reason field
- [ ] AC-127: Extensions are applied after defaults
- [ ] AC-128: Extensions are logged
- [ ] AC-129: Extension count is limited (max 1000)
- [ ] AC-130: Extension patterns are validated

### CLI Commands

- [ ] AC-131: `security show-denylist` shows all paths
- [ ] AC-132: `--platform` flag filters by OS
- [ ] AC-133: `--verbose` shows pattern details
- [ ] AC-134: `--format json` outputs JSON
- [ ] AC-135: `security check-path` checks single path
- [ ] AC-136: Check returns BLOCKED or ALLOWED
- [ ] AC-137: Check shows reason for block
- [ ] AC-138: Check shows pattern matched
- [ ] AC-139: Multiple paths can be checked
- [ ] AC-140: Exit code reflects result

### Logging

- [ ] AC-141: Violations are logged to console
- [ ] AC-142: Violations are logged to file
- [ ] AC-143: Log includes timestamp
- [ ] AC-144: Log includes operation type
- [ ] AC-145: Log includes pattern matched
- [ ] AC-146: Log includes risk ID reference
- [ ] AC-147: Log does NOT include file contents
- [ ] AC-148: Log does NOT include full path
- [ ] AC-149: Repeated violations are rate-limited
- [ ] AC-150: Log level is WARNING

### Performance

- [ ] AC-151: Path check completes in < 1ms
- [ ] AC-152: No memory allocation per check
- [ ] AC-153: Denylist lookup is O(log n) or better
- [ ] AC-154: Pattern matching is O(n) per pattern
- [ ] AC-155: Total memory < 1MB
- [ ] AC-156: Cache reduces redundant checks
- [ ] AC-157: Cache TTL is configurable
- [ ] AC-158: Cache can be cleared
- [ ] AC-159: Performance is benchmarked
- [ ] AC-160: No regex catastrophic backtracking

### Security Guarantees

- [ ] AC-161: No bypass mechanism exists
- [ ] AC-162: Fail-closed is default behavior
- [ ] AC-163: Configuration errors block access
- [ ] AC-164: Null paths are blocked
- [ ] AC-165: Empty paths are blocked
- [ ] AC-166: Unknown paths are blocked
- [ ] AC-167: TOCTOU is mitigated
- [ ] AC-168: Race conditions are handled
- [ ] AC-169: Path comparison is constant-time
- [ ] AC-170: Code has security review

### Platform Compatibility

- [ ] AC-171: Windows is supported
- [ ] AC-172: Linux is supported
- [ ] AC-173: macOS is supported
- [ ] AC-174: WSL is supported
- [ ] AC-175: Docker is supported
- [ ] AC-176: Network paths are blocked
- [ ] AC-177: UNC paths are handled
- [ ] AC-178: Extended paths are handled (\\?\)
- [ ] AC-179: Device paths are blocked (\\.\)
- [ ] AC-180: Named pipes are blocked

### Documentation

- [ ] AC-181: SECURITY.md lists all paths
- [ ] AC-182: Each path has documented reason
- [ ] AC-183: Risk IDs are referenced
- [ ] AC-184: Extension process is documented
- [ ] AC-185: CLI commands are documented
- [ ] AC-186: Error codes are documented
- [ ] AC-187: Security model is explained
- [ ] AC-188: Examples are provided
- [ ] AC-189: Changelog tracks changes
- [ ] AC-190: Version history is maintained

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Domain/Security/PathProtection/
├── DefaultDenylistTests.cs
│   ├── Should_Include_All_SSH_Paths()
│   ├── Should_Include_All_GPG_Paths()
│   ├── Should_Include_All_AWS_Paths()
│   ├── Should_Include_All_Azure_Paths()
│   ├── Should_Include_All_GCloud_Paths()
│   ├── Should_Include_All_Kube_Paths()
│   ├── Should_Include_All_PackageManager_Paths()
│   ├── Should_Include_All_Git_Paths()
│   ├── Should_Include_All_System_Unix_Paths()
│   ├── Should_Include_All_System_Windows_Paths()
│   ├── Should_Include_All_System_MacOS_Paths()
│   ├── Should_Include_All_EnvFile_Patterns()
│   ├── Should_Include_All_SecretFile_Patterns()
│   ├── Should_Be_Immutable()
│   ├── Should_Have_Reason_For_Each_Entry()
│   └── Should_Have_RiskId_For_Each_Entry()
│
├── PathMatcherTests.cs
│   ├── Should_Match_Exact_Path()
│   ├── Should_Match_Directory_Prefix()
│   ├── Should_Match_Single_Glob()
│   ├── Should_Match_Double_Glob()
│   ├── Should_Match_Question_Mark()
│   ├── Should_Match_Character_Class()
│   ├── Should_Match_Character_Range()
│   ├── Should_Be_Case_Insensitive_On_Windows()
│   ├── Should_Be_Case_Sensitive_On_Unix()
│   ├── Should_Handle_Trailing_Slash()
│   ├── Should_Handle_Multiple_Slashes()
│   ├── Should_Not_Backtrack()
│   └── Should_Complete_In_Under_1ms()
│
├── PathNormalizerTests.cs
│   ├── Should_Expand_Tilde()
│   ├── Should_Expand_UserProfile()
│   ├── Should_Expand_Home()
│   ├── Should_Resolve_DotDot()
│   ├── Should_Remove_Dot()
│   ├── Should_Collapse_Slashes()
│   ├── Should_Remove_Trailing_Slash()
│   ├── Should_Convert_Backslash_On_Windows()
│   ├── Should_Handle_Very_Long_Paths()
│   ├── Should_Handle_Unicode()
│   └── Should_Handle_Special_Characters()
│
├── SymlinkResolverTests.cs
│   ├── Should_Resolve_Single_Symlink()
│   ├── Should_Resolve_Chain_Of_Symlinks()
│   ├── Should_Detect_Circular_Symlink()
│   ├── Should_Enforce_Max_Depth_40()
│   ├── Should_Block_On_Resolution_Error()
│   └── Should_Cache_Resolution()
│
├── ProtectedPathValidatorTests.cs
│   ├── Should_Block_SSH_Directory()
│   ├── Should_Block_SSH_PrivateKey()
│   ├── Should_Block_AWS_Credentials()
│   ├── Should_Block_Env_File()
│   ├── Should_Block_Traversal_To_Protected()
│   ├── Should_Block_Symlink_To_Protected()
│   ├── Should_Allow_Normal_Source_File()
│   ├── Should_Return_ProtectedPathError()
│   ├── Should_Include_Error_Code()
│   ├── Should_Not_Reveal_Contents()
│   └── Should_Log_Violation()
│
└── UserExtensionTests.cs
    ├── Should_Load_From_Config()
    ├── Should_Validate_Pattern()
    ├── Should_Require_Reason()
    ├── Should_Apply_After_Defaults()
    ├── Should_Not_Remove_Defaults()
    ├── Should_Limit_To_1000_Entries()
    └── Should_Reject_Invalid_Patterns()
```

### Integration Tests

```
Tests/Integration/Security/PathProtection/
├── DenylistEnforcementTests.cs
│   ├── FileRead_ShouldBlock_ProtectedPath()
│   ├── FileWrite_ShouldBlock_ProtectedPath()
│   ├── FileDelete_ShouldBlock_ProtectedPath()
│   ├── DirectoryList_ShouldBlock_ProtectedPath()
│   ├── Should_AllowNormal_SourceFiles()
│   └── Should_EnforceAcross_AllFileOperations()
│
├── CrossPlatformTests.cs
│   ├── Should_UseWindowsPaths_OnWindows()
│   ├── Should_UseUnixPaths_OnLinux()
│   ├── Should_UseMacOSPaths_OnMacOS()
│   ├── Should_HandleMixedSlashes_OnWindows()
│   └── Should_ExpandHomeDirectory_Correctly()
│
├── ConfigIntegrationTests.cs
│   ├── Should_LoadUserExtensions_FromConfig()
│   ├── Should_MergeExtensions_WithDefaults()
│   ├── Should_RejectInvalid_Extensions()
│   └── Should_ApplyExtensions_ToPathChecks()
│
└── CLIIntegrationTests.cs
    ├── ShowDenylist_ShouldList_AllPaths()
    ├── ShowDenylist_ShouldFilter_ByPlatform()
    ├── CheckPath_ShouldReturn_Blocked()
    ├── CheckPath_ShouldReturn_Allowed()
    └── CheckPath_ShouldShow_Reason()
```

### End-to-End Tests

```
Tests/E2E/Security/
├── ProtectedPathScenarios.cs
│   ├── Scenario_Agent_Attempts_SSH_Key_Read()
│   ├── Scenario_Agent_Attempts_AWS_Creds_Read()
│   ├── Scenario_Agent_Attempts_Env_File_Read()
│   ├── Scenario_Agent_Attempts_DirectoryTraversal()
│   ├── Scenario_Agent_Attempts_Symlink_Attack()
│   ├── Scenario_NormalDevelopment_Workflow()
│   └── Scenario_UserExtends_Denylist()
```

### Performance Tests

```
Tests/Performance/Security/
├── PathMatchingBenchmarks.cs
│   ├── Benchmark_SinglePathCheck()
│   ├── Benchmark_1000PathChecks()
│   ├── Benchmark_GlobPatternMatching()
│   ├── Benchmark_PathNormalization()
│   └── Benchmark_DenylistLookup()
│
└── MemoryTests.cs
    ├── Should_UseUnder1MB_ForDenylist()
    ├── Should_NotAllocate_PerPathCheck()
    └── Should_CacheEfficiently()
```

### Regression Tests

```
Tests/Regression/Security/
├── PathBypassTests.cs
│   ├── Should_Block_NullPath()
│   ├── Should_Block_EmptyPath()
│   ├── Should_Block_WhitespacePath()
│   ├── Should_Block_EncodedPath()
│   ├── Should_Block_UnicodeNormalization()
│   ├── Should_Block_CaseVariation()
│   └── Should_Block_AlternateDataStream()
```

### Security Tests

```
Tests/Security/PathProtection/
├── PenetrationTests.cs
│   ├── Should_Resist_DirectoryTraversal()
│   ├── Should_Resist_SymlinkAttack()
│   ├── Should_Resist_TOCTOU()
│   ├── Should_Resist_RaceCondition()
│   ├── Should_Resist_EncodingBypass()
│   └── Should_Resist_NullByteInjection()
```

---

## User Verification Steps

### Scenario 1: View Default Denylist

**Objective:** Verify the denylist display command works correctly

1. Open terminal in repository root
2. Run: `agentic-coder security show-denylist`
3. Verify output shows all SSH paths (~/.ssh/*, etc.)
4. Verify output shows all cloud credential paths
5. Verify output shows all package manager paths
6. Verify output shows all system paths
7. Verify output shows all environment file patterns
8. Verify each entry has a reason description

**Expected Result:**
- Complete list of protected paths displayed
- Paths grouped by category
- Each path has explanation

### Scenario 2: Platform-Specific Paths

**Objective:** Verify platform filtering works

1. Run: `agentic-coder security show-denylist --platform windows`
2. Verify Windows paths are shown (C:\Windows\, etc.)
3. Verify Unix-only paths are NOT shown
4. Run: `agentic-coder security show-denylist --platform linux`
5. Verify Unix paths are shown (/etc/, /root/, etc.)
6. Verify Windows-only paths are NOT shown

**Expected Result:**
- Platform flag correctly filters paths
- Only relevant paths for platform shown

### Scenario 3: Check Blocked Path

**Objective:** Verify path checking identifies protected paths

1. Run: `agentic-coder security check-path ~/.ssh/id_rsa`
2. Verify output shows "BLOCKED"
3. Verify output shows reason (SSH private key)
4. Verify output shows matched pattern (~/.ssh/id_*)
5. Run: `agentic-coder security check-path ~/.aws/credentials`
6. Verify output shows "BLOCKED"
7. Verify output shows reason (AWS credentials)

**Expected Result:**
- Protected paths correctly identified
- Clear reason provided
- Pattern match shown

### Scenario 4: Check Allowed Path

**Objective:** Verify normal paths are allowed

1. Run: `agentic-coder security check-path ./src/Program.cs`
2. Verify output shows "ALLOWED"
3. Run: `agentic-coder security check-path ./README.md`
4. Verify output shows "ALLOWED"

**Expected Result:**
- Normal development files are allowed
- No false positives

### Scenario 5: Directory Traversal Block

**Objective:** Verify directory traversal is blocked

1. Run: `agentic-coder security check-path ./src/../../../etc/passwd`
2. Verify output shows "BLOCKED"
3. Verify path is normalized before check
4. Verify attack is detected

**Expected Result:**
- Traversal attempts blocked
- Path normalized correctly
- Security violation logged

### Scenario 6: Symlink Attack Block

**Objective:** Verify symlinks to protected paths are blocked

1. Create symlink: `ln -s ~/.ssh/id_rsa test_link`
2. Run: `agentic-coder security check-path ./test_link`
3. Verify output shows "BLOCKED"
4. Verify symlink was resolved
5. Clean up: `rm test_link`

**Expected Result:**
- Symlink attacks detected
- Real target path checked
- Protection enforced

### Scenario 7: Environment File Protection

**Objective:** Verify .env files are protected

1. Run: `agentic-coder security check-path .env`
2. Verify output shows "BLOCKED"
3. Run: `agentic-coder security check-path .env.local`
4. Verify output shows "BLOCKED"
5. Run: `agentic-coder security check-path ./config/.env.production`
6. Verify output shows "BLOCKED" (nested pattern)

**Expected Result:**
- All .env variants blocked
- Nested .env files blocked
- Pattern matching works correctly

### Scenario 8: User Extension

**Objective:** Verify users can extend the denylist

1. Add to `.agent/config.yml`:
   ```yaml
   security:
     additional_protected_paths:
       - pattern: "company-secrets/"
         reason: "Internal documentation"
   ```
2. Run: `agentic-coder security show-denylist`
3. Verify "company-secrets/" appears in list
4. Run: `agentic-coder security check-path ./company-secrets/internal.doc`
5. Verify output shows "BLOCKED"
6. Verify reason shows "Internal documentation"

**Expected Result:**
- User extensions loaded
- Extensions applied to checks
- Custom reason displayed

### Scenario 9: Cannot Remove Default Paths

**Objective:** Verify default paths cannot be removed

1. Add to `.agent/config.yml`:
   ```yaml
   security:
     remove_protected_paths:
       - "~/.ssh/"
   ```
2. Run: `agentic-coder security show-denylist`
3. Verify ~/.ssh/ is STILL in the list
4. Verify warning or error about invalid config

**Expected Result:**
- Default paths cannot be removed
- Clear error/warning message
- Security not compromised

### Scenario 10: Violation Logging

**Objective:** Verify violations are logged

1. Enable verbose logging
2. Attempt operation that accesses ~/.ssh/id_rsa
3. Check log output
4. Verify log contains:
   - Timestamp
   - Event type (protected_path_access_blocked)
   - Error code (ACODE-SEC-003)
   - Pattern matched
   - Risk ID reference
5. Verify log does NOT contain:
   - File contents
   - Full path (redacted)

**Expected Result:**
- Violations properly logged
- Logs contain required fields
- No sensitive data in logs

### Scenario 11: Performance Verification

**Objective:** Verify path checking is fast

1. Run benchmark: `agentic-coder benchmark path-check`
2. Verify single path check < 1ms
3. Verify 1000 path checks complete reasonably
4. Verify no memory growth over repeated checks

**Expected Result:**
- Path checking is performant
- No memory leaks
- Acceptable for production use

### Scenario 12: Error Message Quality

**Objective:** Verify error messages are helpful

1. Attempt to read ~/.ssh/id_rsa via the agent
2. Verify error message includes:
   - Error code ACODE-SEC-003
   - Clear description of why blocked
   - Suggestion for alternative
3. Verify error message does NOT include:
   - File contents
   - Exact file path (redacted)
   - Stack traces (unless debug mode)

**Expected Result:**
- Error messages are user-friendly
- Security information not leaked
- Actionable guidance provided

---

## Implementation Prompt

### File Structure

```
src/
├── AgenticCoder.Domain/
│   ├── Security/
│   │   ├── PathProtection/
│   │   │   ├── DefaultDenylist.cs
│   │   │   ├── DenylistEntry.cs
│   │   │   ├── IPathMatcher.cs
│   │   │   ├── GlobMatcher.cs
│   │   │   ├── IPathNormalizer.cs
│   │   │   ├── PathNormalizer.cs
│   │   │   ├── ISymlinkResolver.cs
│   │   │   ├── SymlinkResolver.cs
│   │   │   ├── IProtectedPathValidator.cs
│   │   │   ├── ProtectedPathValidator.cs
│   │   │   └── ProtectedPathError.cs
│   │   └── Risks/
│   │       └── PathProtectionRisks.cs
│   └── ValueObjects/
│       └── NormalizedPath.cs
│
├── AgenticCoder.Application/
│   ├── Security/
│   │   ├── Commands/
│   │   │   ├── CheckPathCommand.cs
│   │   │   └── CheckPathHandler.cs
│   │   └── Queries/
│   │       ├── GetDenylistQuery.cs
│   │       └── GetDenylistHandler.cs
│
├── AgenticCoder.Infrastructure/
│   ├── Security/
│   │   ├── PathProtection/
│   │   │   ├── FileSystemPathNormalizer.cs
│   │   │   ├── FileSystemSymlinkResolver.cs
│   │   │   └── PlatformPathDetector.cs
│   └── Configuration/
│       └── UserDenylistExtensionLoader.cs
│
└── AgenticCoder.CLI/
    └── Commands/
        └── Security/
            ├── ShowDenylistCommand.cs
            └── CheckPathCommand.cs
```

### Core Interfaces

```csharp
namespace AgenticCoder.Domain.Security.PathProtection;

/// <summary>
/// Entry in the denylist defining a protected path pattern.
/// </summary>
public sealed record DenylistEntry
{
    public required string Pattern { get; init; }
    public required string Reason { get; init; }
    public required string RiskId { get; init; }
    public required PathCategory Category { get; init; }
    public required Platform[] Platforms { get; init; }
    public bool IsDefault { get; init; } = true;
}

public enum PathCategory
{
    SshKeys,
    GpgKeys,
    CloudCredentials,
    PackageManagerCredentials,
    GitCredentials,
    SystemFiles,
    EnvironmentFiles,
    SecretFiles,
    UserDefined
}

public enum Platform
{
    Windows,
    Linux,
    MacOS,
    All
}

/// <summary>
/// Validates whether a path is protected and should be blocked.
/// </summary>
public interface IProtectedPathValidator
{
    /// <summary>
    /// Checks if the given path is protected by the denylist.
    /// </summary>
    /// <param name="path">The path to check (may be relative or absolute).</param>
    /// <returns>Result indicating if path is protected and why.</returns>
    PathValidationResult Validate(string path);
    
    /// <summary>
    /// Checks if the given path is protected, with operation context.
    /// </summary>
    PathValidationResult Validate(string path, FileOperation operation);
}

public enum FileOperation
{
    Read,
    Write,
    Delete,
    List
}

public sealed record PathValidationResult
{
    public bool IsProtected { get; init; }
    public string? MatchedPattern { get; init; }
    public string? Reason { get; init; }
    public string? RiskId { get; init; }
    public PathCategory? Category { get; init; }
    public ProtectedPathError? Error { get; init; }
    
    public static PathValidationResult Allowed() => 
        new() { IsProtected = false };
    
    public static PathValidationResult Blocked(DenylistEntry entry) =>
        new()
        {
            IsProtected = true,
            MatchedPattern = entry.Pattern,
            Reason = entry.Reason,
            RiskId = entry.RiskId,
            Category = entry.Category,
            Error = new ProtectedPathError(entry)
        };
}
```

### Default Denylist Definition

```csharp
namespace AgenticCoder.Domain.Security.PathProtection;

/// <summary>
/// Immutable default denylist that cannot be modified.
/// SECURITY CRITICAL: Changes to this class require security review.
/// </summary>
public static class DefaultDenylist
{
    /// <summary>
    /// Gets all default protected path entries.
    /// This collection is immutable and cannot be reduced.
    /// </summary>
    public static IReadOnlyList<DenylistEntry> Entries { get; } = CreateEntries();
    
    private static IReadOnlyList<DenylistEntry> CreateEntries()
    {
        return new[]
        {
            // SSH Keys
            new DenylistEntry
            {
                Pattern = "~/.ssh/",
                Reason = "SSH directory containing private keys",
                RiskId = "RISK-E-003",
                Category = PathCategory.SshKeys,
                Platforms = [Platform.Linux, Platform.MacOS]
            },
            new DenylistEntry
            {
                Pattern = "~/.ssh/id_*",
                Reason = "SSH private key files",
                RiskId = "RISK-E-003",
                Category = PathCategory.SshKeys,
                Platforms = [Platform.Linux, Platform.MacOS]
            },
            // ... (all entries defined)
            
            // Cloud Credentials
            new DenylistEntry
            {
                Pattern = "~/.aws/",
                Reason = "AWS credentials and configuration",
                RiskId = "RISK-I-003",
                Category = PathCategory.CloudCredentials,
                Platforms = [Platform.All]
            },
            
            // Environment Files
            new DenylistEntry
            {
                Pattern = ".env",
                Reason = "Environment file may contain secrets",
                RiskId = "RISK-I-002",
                Category = PathCategory.EnvironmentFiles,
                Platforms = [Platform.All]
            },
            new DenylistEntry
            {
                Pattern = ".env.*",
                Reason = "Environment file variants",
                RiskId = "RISK-I-002",
                Category = PathCategory.EnvironmentFiles,
                Platforms = [Platform.All]
            },
            new DenylistEntry
            {
                Pattern = "**/.env",
                Reason = "Nested environment files",
                RiskId = "RISK-I-002",
                Category = PathCategory.EnvironmentFiles,
                Platforms = [Platform.All]
            },
            
            // System Paths (Windows)
            new DenylistEntry
            {
                Pattern = @"C:\Windows\",
                Reason = "Windows system directory",
                RiskId = "RISK-E-004",
                Category = PathCategory.SystemFiles,
                Platforms = [Platform.Windows]
            },
            
            // System Paths (Unix)
            new DenylistEntry
            {
                Pattern = "/etc/",
                Reason = "Unix system configuration directory",
                RiskId = "RISK-E-004",
                Category = PathCategory.SystemFiles,
                Platforms = [Platform.Linux, Platform.MacOS]
            },
            
        }.ToImmutableList();
    }
}
```

### Path Matcher Implementation

```csharp
namespace AgenticCoder.Domain.Security.PathProtection;

/// <summary>
/// Matches paths against glob patterns.
/// SECURITY CRITICAL: Must not use backtracking regex.
/// </summary>
public sealed class GlobMatcher : IPathMatcher
{
    private readonly bool _caseSensitive;
    
    public GlobMatcher(bool caseSensitive)
    {
        _caseSensitive = caseSensitive;
    }
    
    /// <summary>
    /// Matches a normalized path against a glob pattern.
    /// Uses linear-time algorithm to prevent ReDoS.
    /// </summary>
    public bool Matches(string pattern, string path)
    {
        // Implementation uses state machine, not regex
        // to avoid catastrophic backtracking
        return MatchGlob(pattern, path, 0, 0);
    }
    
    private bool MatchGlob(string pattern, string path, int pi, int si)
    {
        // Linear-time glob matching algorithm
        // Handles *, **, ?, [abc], [a-z]
        // Returns match result without backtracking
    }
}
```

### Error Codes

| Code | Description |
|------|-------------|
| ACODE-SEC-003 | Protected path access blocked |
| ACODE-SEC-003-001 | SSH key path blocked |
| ACODE-SEC-003-002 | Cloud credential path blocked |
| ACODE-SEC-003-003 | System path blocked |
| ACODE-SEC-003-004 | Environment file blocked |
| ACODE-SEC-003-005 | Symlink to protected path blocked |
| ACODE-SEC-003-006 | Directory traversal blocked |
| ACODE-SEC-003-007 | User-defined protected path blocked |

### CLI Exit Codes

| Exit Code | Meaning |
|-----------|---------|
| 0 | Path is allowed |
| 1 | Path is blocked (protected) |
| 2 | Invalid arguments |
| 3 | Configuration error |

### Implementation Checklist

1. [ ] Implement `DefaultDenylist` with all required entries
2. [ ] Implement `DenylistEntry` record with all fields
3. [ ] Implement `IPathMatcher` and `GlobMatcher`
4. [ ] Implement `IPathNormalizer` and `PathNormalizer`
5. [ ] Implement `ISymlinkResolver` and `SymlinkResolver`
6. [ ] Implement `IProtectedPathValidator` and `ProtectedPathValidator`
7. [ ] Implement `ProtectedPathError` with proper error codes
8. [ ] Implement user extension loading from config
9. [ ] Implement CLI `security show-denylist` command
10. [ ] Implement CLI `security check-path` command
11. [ ] Add integration with file operation interceptor
12. [ ] Add logging for all violations
13. [ ] Write unit tests for each denylist entry
14. [ ] Write unit tests for path matching
15. [ ] Write unit tests for symlink resolution
16. [ ] Write integration tests for enforcement
17. [ ] Write security tests for bypass attempts
18. [ ] Document all entries in SECURITY.md
19. [ ] Add performance benchmarks
20. [ ] Conduct security review

### Dependencies

- Task 002.a (config schema for user extensions)
- Task 002.b (config parser for loading extensions)
- Task 003 (threat model for risk references)
- Task 003.a (risk IDs for entry references)

### Verification Command

```bash
# Run all denylist tests
dotnet test --filter "FullyQualifiedName~PathProtection"

# Run security tests
dotnet test --filter "Category=Security&FullyQualifiedName~Denylist"

# Run benchmarks
dotnet run --project Tests/Performance -- --filter "*PathMatching*"
```

---

**End of Task 003.b Specification**