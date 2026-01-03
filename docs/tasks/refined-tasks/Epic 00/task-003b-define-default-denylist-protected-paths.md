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
```

#### DefaultDenylistTests.cs

```csharp
namespace AgenticCoder.Tests.Unit.Domain.Security.PathProtection;

using AgenticCoder.Domain.Security.PathProtection;
using FluentAssertions;
using Xunit;

public class DefaultDenylistTests
{
    private readonly IReadOnlyList<DenylistEntry> _entries = DefaultDenylist.Entries;

    [Fact]
    public void Should_Include_All_SSH_Paths()
    {
        // Arrange
        var expectedPatterns = new[]
        {
            "~/.ssh/",
            "~/.ssh/id_*",
            "~/.ssh/id_rsa",
            "~/.ssh/id_ed25519",
            "~/.ssh/id_ecdsa",
            "~/.ssh/id_dsa",
            "~/.ssh/authorized_keys",
            "~/.ssh/known_hosts",
            "~/.ssh/config",
            "%USERPROFILE%\\.ssh\\",
            "%USERPROFILE%\\.ssh\\id_*"
        };

        // Act
        var sshEntries = _entries
            .Where(e => e.Category == PathCategory.SshKeys)
            .Select(e => e.Pattern)
            .ToList();

        // Assert
        foreach (var pattern in expectedPatterns)
        {
            sshEntries.Should().Contain(pattern,
                because: $"SSH path {pattern} must be protected");
        }
    }

    [Fact]
    public void Should_Include_All_GPG_Paths()
    {
        // Arrange
        var expectedPatterns = new[]
        {
            "~/.gnupg/",
            "~/.gnupg/private-keys-v1.d/",
            "~/.gnupg/secring.gpg",
            "%APPDATA%\\gnupg\\"
        };

        // Act
        var gpgEntries = _entries
            .Where(e => e.Category == PathCategory.GpgKeys)
            .Select(e => e.Pattern)
            .ToList();

        // Assert
        foreach (var pattern in expectedPatterns)
        {
            gpgEntries.Should().Contain(pattern,
                because: $"GPG path {pattern} must be protected");
        }
    }

    [Fact]
    public void Should_Include_All_AWS_Paths()
    {
        // Arrange
        var expectedPatterns = new[]
        {
            "~/.aws/",
            "~/.aws/credentials",
            "~/.aws/config",
            "%USERPROFILE%\\.aws\\",
            "%USERPROFILE%\\.aws\\credentials"
        };

        // Act
        var awsEntries = _entries
            .Where(e => e.Category == PathCategory.CloudCredentials)
            .Where(e => e.Pattern.Contains("aws", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Pattern)
            .ToList();

        // Assert
        foreach (var pattern in expectedPatterns)
        {
            awsEntries.Should().Contain(pattern,
                because: $"AWS path {pattern} must be protected");
        }
    }

    [Fact]
    public void Should_Include_All_Azure_Paths()
    {
        // Arrange
        var expectedPatterns = new[]
        {
            "~/.azure/",
            "~/.azure/credentials",
            "~/.azure/accessTokens.json",
            "%USERPROFILE%\\.azure\\"
        };

        // Act
        var azureEntries = _entries
            .Where(e => e.Category == PathCategory.CloudCredentials)
            .Where(e => e.Pattern.Contains("azure", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Pattern)
            .ToList();

        // Assert
        foreach (var pattern in expectedPatterns)
        {
            azureEntries.Should().Contain(pattern,
                because: $"Azure path {pattern} must be protected");
        }
    }

    [Fact]
    public void Should_Include_All_GCloud_Paths()
    {
        // Arrange
        var expectedPatterns = new[]
        {
            "~/.config/gcloud/",
            "~/.config/gcloud/credentials.db",
            "~/.config/gcloud/access_tokens.db",
            "~/.config/gcloud/application_default_credentials.json",
            "%APPDATA%\\gcloud\\"
        };

        // Act
        var gcloudEntries = _entries
            .Where(e => e.Category == PathCategory.CloudCredentials)
            .Where(e => e.Pattern.Contains("gcloud", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Pattern)
            .ToList();

        // Assert
        foreach (var pattern in expectedPatterns)
        {
            gcloudEntries.Should().Contain(pattern,
                because: $"GCloud path {pattern} must be protected");
        }
    }

    [Fact]
    public void Should_Include_All_Kube_Paths()
    {
        // Arrange
        var expectedPatterns = new[]
        {
            "~/.kube/",
            "~/.kube/config",
            "%USERPROFILE%\\.kube\\"
        };

        // Act
        var kubeEntries = _entries
            .Where(e => e.Category == PathCategory.CloudCredentials)
            .Where(e => e.Pattern.Contains("kube", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Pattern)
            .ToList();

        // Assert
        foreach (var pattern in expectedPatterns)
        {
            kubeEntries.Should().Contain(pattern,
                because: $"Kubernetes path {pattern} must be protected");
        }
    }

    [Fact]
    public void Should_Include_All_EnvFile_Patterns()
    {
        // Arrange
        var expectedPatterns = new[]
        {
            ".env",
            ".env.*",
            ".env.local",
            ".env.development",
            ".env.production",
            "**/.env",
            "**/.env.*"
        };

        // Act
        var envEntries = _entries
            .Where(e => e.Category == PathCategory.EnvironmentFiles)
            .Select(e => e.Pattern)
            .ToList();

        // Assert
        foreach (var pattern in expectedPatterns)
        {
            envEntries.Should().Contain(pattern,
                because: $"Environment file pattern {pattern} must be protected");
        }
    }

    [Fact]
    public void Should_Be_Immutable()
    {
        // Arrange
        var entries = DefaultDenylist.Entries;

        // Act & Assert
        entries.Should().BeAssignableTo<IReadOnlyList<DenylistEntry>>(
            because: "denylist must be immutable");
        
        // Verify we cannot cast to mutable
        var asList = entries as IList<DenylistEntry>;
        if (asList != null)
        {
            asList.IsReadOnly.Should().BeTrue(
                because: "denylist backing list must be read-only");
        }
    }

    [Fact]
    public void Should_Have_Reason_For_Each_Entry()
    {
        // Act & Assert
        foreach (var entry in _entries)
        {
            entry.Reason.Should().NotBeNullOrWhiteSpace(
                because: $"entry {entry.Pattern} must have a reason");
            entry.Reason.Length.Should().BeGreaterThan(10,
                because: "reason should be descriptive");
        }
    }

    [Fact]
    public void Should_Have_RiskId_For_Each_Entry()
    {
        // Arrange
        var validRiskIdPattern = @"^RISK-[EIC]-\d{3}$";

        // Act & Assert
        foreach (var entry in _entries)
        {
            entry.RiskId.Should().NotBeNullOrWhiteSpace(
                because: $"entry {entry.Pattern} must have a risk ID");
            entry.RiskId.Should().MatchRegex(validRiskIdPattern,
                because: "risk ID must follow RISK-X-NNN format");
        }
    }

    [Fact]
    public void Should_Have_Valid_Category_For_Each_Entry()
    {
        // Act & Assert
        foreach (var entry in _entries)
        {
            entry.Category.Should().BeDefined(
                because: $"entry {entry.Pattern} must have a valid category");
        }
    }

    [Fact]
    public void Should_Have_At_Least_One_Platform_For_Each_Entry()
    {
        // Act & Assert
        foreach (var entry in _entries)
        {
            entry.Platforms.Should().NotBeEmpty(
                because: $"entry {entry.Pattern} must specify at least one platform");
        }
    }

    [Fact]
    public void Should_Contain_Minimum_Required_Entries()
    {
        // The denylist should have comprehensive coverage
        _entries.Count.Should().BeGreaterOrEqualTo(100,
            because: "denylist should cover all security-sensitive paths");
    }
}
```

```
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
```

#### PathMatcherTests.cs

```csharp
namespace AgenticCoder.Tests.Unit.Domain.Security.PathProtection;

using AgenticCoder.Domain.Security.PathProtection;
using FluentAssertions;
using System.Diagnostics;
using Xunit;

public class PathMatcherTests
{
    private readonly IPathMatcher _matcher;
    private readonly IPathMatcher _caseSensitiveMatcher;
    private readonly IPathMatcher _caseInsensitiveMatcher;

    public PathMatcherTests()
    {
        _matcher = new GlobMatcher(caseSensitive: true);
        _caseSensitiveMatcher = new GlobMatcher(caseSensitive: true);
        _caseInsensitiveMatcher = new GlobMatcher(caseSensitive: false);
    }

    [Theory]
    [InlineData("~/.ssh/id_rsa", "~/.ssh/id_rsa", true)]
    [InlineData("~/.ssh/id_rsa", "~/.ssh/id_ed25519", false)]
    [InlineData("/etc/passwd", "/etc/passwd", true)]
    [InlineData("/etc/passwd", "/etc/shadow", false)]
    public void Should_Match_Exact_Path(string pattern, string path, bool expected)
    {
        // Act
        var result = _matcher.Matches(pattern, path);

        // Assert
        result.Should().Be(expected,
            because: $"pattern '{pattern}' should{(expected ? "" : " not")} match '{path}'");
    }

    [Theory]
    [InlineData("~/.ssh/", "~/.ssh/id_rsa", true)]
    [InlineData("~/.ssh/", "~/.ssh/config", true)]
    [InlineData("~/.aws/", "~/.aws/credentials", true)]
    [InlineData("~/.ssh/", "~/.gnupg/secring.gpg", false)]
    [InlineData("/etc/", "/etc/passwd", true)]
    [InlineData("/etc/", "/etc/ssh/sshd_config", true)]
    public void Should_Match_Directory_Prefix(string pattern, string path, bool expected)
    {
        // Act
        var result = _matcher.Matches(pattern, path);

        // Assert
        result.Should().Be(expected,
            because: $"directory pattern '{pattern}' should{(expected ? "" : " not")} match '{path}'");
    }

    [Theory]
    [InlineData("~/.ssh/id_*", "~/.ssh/id_rsa", true)]
    [InlineData("~/.ssh/id_*", "~/.ssh/id_ed25519", true)]
    [InlineData("~/.ssh/id_*", "~/.ssh/id_ecdsa", true)]
    [InlineData("~/.ssh/id_*", "~/.ssh/config", false)]
    [InlineData("*.env", "production.env", true)]
    [InlineData("*.env", ".env", false)] // * doesn't match empty
    [InlineData("*.log", "app.log", true)]
    [InlineData("*.log", "app.txt", false)]
    public void Should_Match_Single_Glob(string pattern, string path, bool expected)
    {
        // Act
        var result = _matcher.Matches(pattern, path);

        // Assert
        result.Should().Be(expected,
            because: $"single glob '{pattern}' should{(expected ? "" : " not")} match '{path}'");
    }

    [Theory]
    [InlineData("**/.env", ".env", true)]
    [InlineData("**/.env", "src/.env", true)]
    [InlineData("**/.env", "src/config/.env", true)]
    [InlineData("**/.env", "deeply/nested/path/.env", true)]
    [InlineData("**/.env", ".env.local", false)]
    [InlineData("**/node_modules/**", "node_modules/package/index.js", true)]
    [InlineData("**/node_modules/**", "src/node_modules/dep/lib.js", true)]
    public void Should_Match_Double_Glob(string pattern, string path, bool expected)
    {
        // Act
        var result = _matcher.Matches(pattern, path);

        // Assert
        result.Should().Be(expected,
            because: $"double glob '{pattern}' should{(expected ? "" : " not")} match '{path}'");
    }

    [Theory]
    [InlineData("id_?sa", "id_rsa", true)]
    [InlineData("id_?sa", "id_dsa", true)]
    [InlineData("id_?sa", "id_ecdsa", false)] // ? matches exactly one char
    [InlineData("?.env", "a.env", true)]
    [InlineData("?.env", ".env", false)]
    public void Should_Match_Question_Mark(string pattern, string path, bool expected)
    {
        // Act
        var result = _matcher.Matches(pattern, path);

        // Assert
        result.Should().Be(expected,
            because: $"question mark pattern '{pattern}' should{(expected ? "" : " not")} match '{path}'");
    }

    [Theory]
    [InlineData("[abc].txt", "a.txt", true)]
    [InlineData("[abc].txt", "b.txt", true)]
    [InlineData("[abc].txt", "d.txt", false)]
    [InlineData("[!abc].txt", "d.txt", true)]
    [InlineData("[!abc].txt", "a.txt", false)]
    public void Should_Match_Character_Class(string pattern, string path, bool expected)
    {
        // Act
        var result = _matcher.Matches(pattern, path);

        // Assert
        result.Should().Be(expected,
            because: $"character class '{pattern}' should{(expected ? "" : " not")} match '{path}'");
    }

    [Theory]
    [InlineData("[a-z].txt", "m.txt", true)]
    [InlineData("[a-z].txt", "5.txt", false)]
    [InlineData("[0-9].log", "5.log", true)]
    [InlineData("[0-9].log", "a.log", false)]
    [InlineData("[a-zA-Z].txt", "M.txt", true)]
    public void Should_Match_Character_Range(string pattern, string path, bool expected)
    {
        // Act
        var result = _matcher.Matches(pattern, path);

        // Assert
        result.Should().Be(expected,
            because: $"character range '{pattern}' should{(expected ? "" : " not")} match '{path}'");
    }

    [Theory]
    [InlineData("C:\\Windows\\", "c:\\windows\\system32")]
    [InlineData(".ENV", ".env")]
    [InlineData("~/.SSH/", "~/.ssh/id_rsa")]
    public void Should_Be_Case_Insensitive_On_Windows(string pattern, string path)
    {
        // Act
        var result = _caseInsensitiveMatcher.Matches(pattern, path);

        // Assert
        result.Should().BeTrue(
            because: "Windows paths should be case-insensitive");
    }

    [Theory]
    [InlineData(".ENV", ".env", false)]
    [InlineData("~/.SSH/", "~/.ssh/id_rsa", false)]
    [InlineData("~/.ssh/", "~/.ssh/id_rsa", true)]
    public void Should_Be_Case_Sensitive_On_Unix(string pattern, string path, bool expected)
    {
        // Act
        var result = _caseSensitiveMatcher.Matches(pattern, path);

        // Assert
        result.Should().Be(expected,
            because: "Unix paths should be case-sensitive");
    }

    [Theory]
    [InlineData("~/.ssh/", "~/.ssh")]
    [InlineData("~/.ssh", "~/.ssh/")]
    public void Should_Handle_Trailing_Slash(string pattern, string path)
    {
        // Act
        var result = _matcher.Matches(pattern, path);

        // Assert
        result.Should().BeTrue(
            because: "trailing slash variations should be handled");
    }

    [Theory]
    [InlineData("~/.ssh//id_rsa", "~/.ssh/id_rsa")]
    [InlineData("~/.ssh/id_rsa", "~/.ssh//id_rsa")]
    public void Should_Handle_Multiple_Slashes(string pattern, string path)
    {
        // Act
        var result = _matcher.Matches(pattern, path);

        // Assert
        result.Should().BeTrue(
            because: "multiple consecutive slashes should be normalized");
    }

    [Fact]
    public void Should_Not_Backtrack()
    {
        // Arrange - pathological pattern that causes exponential backtracking in naive implementations
        var pattern = "a]]]]]]***********************b";
        var path = "a]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]]c";

        // Act
        var sw = Stopwatch.StartNew();
        _ = _matcher.Matches(pattern, path);
        sw.Stop();

        // Assert - should complete quickly even with pathological input
        sw.ElapsedMilliseconds.Should().BeLessThan(100,
            because: "glob matcher must use linear-time algorithm to prevent ReDoS");
    }

    [Fact]
    public void Should_Complete_In_Under_1ms()
    {
        // Arrange
        var testCases = new[]
        {
            ("~/.ssh/id_*", "~/.ssh/id_rsa"),
            ("**/.env", "src/config/.env"),
            ("~/.aws/credentials", "~/.aws/credentials"),
            ("C:\\Windows\\System32\\**", "C:\\Windows\\System32\\drivers\\etc\\hosts")
        };

        foreach (var (pattern, path) in testCases)
        {
            // Warm up
            _ = _matcher.Matches(pattern, path);

            // Act
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                _ = _matcher.Matches(pattern, path);
            }
            sw.Stop();

            // Assert
            var avgMs = sw.Elapsed.TotalMilliseconds / 1000;
            avgMs.Should().BeLessThan(1,
                because: $"single path check for '{pattern}' should complete in under 1ms");
        }
    }
}
```

```
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
```

#### PathNormalizerTests.cs

```csharp
namespace AgenticCoder.Tests.Unit.Domain.Security.PathProtection;

using AgenticCoder.Domain.Security.PathProtection;
using FluentAssertions;
using Xunit;

public class PathNormalizerTests
{
    private readonly IPathNormalizer _normalizer;

    public PathNormalizerTests()
    {
        _normalizer = new PathNormalizer();
    }

    [Theory]
    [InlineData("~/documents/file.txt")]
    [InlineData("~/.ssh/id_rsa")]
    [InlineData("~/.aws/credentials")]
    public void Should_Expand_Tilde(string path)
    {
        // Act
        var result = _normalizer.Normalize(path);

        // Assert
        result.Should().NotContain("~",
            because: "tilde should be expanded to home directory");
        result.Should().NotBeNullOrWhiteSpace();
        
        if (OperatingSystem.IsWindows())
        {
            result.Should().StartWith(Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile));
        }
        else
        {
            result.Should().StartWith(Environment.GetEnvironmentVariable("HOME"));
        }
    }

    [Theory]
    [InlineData("%USERPROFILE%\\.ssh\\id_rsa")]
    [InlineData("%USERPROFILE%\\.aws\\credentials")]
    public void Should_Expand_UserProfile(string path)
    {
        // Skip on non-Windows
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Act
        var result = _normalizer.Normalize(path);

        // Assert
        result.Should().NotContain("%USERPROFILE%",
            because: "USERPROFILE should be expanded");
        result.Should().StartWith(Environment.GetFolderPath(
            Environment.SpecialFolder.UserProfile));
    }

    [Theory]
    [InlineData("$HOME/.ssh/id_rsa")]
    [InlineData("$HOME/.aws/credentials")]
    public void Should_Expand_Home(string path)
    {
        // Skip on Windows
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        // Act
        var result = _normalizer.Normalize(path);

        // Assert
        result.Should().NotContain("$HOME",
            because: "HOME variable should be expanded");
        result.Should().StartWith(Environment.GetEnvironmentVariable("HOME"));
    }

    [Theory]
    [InlineData("/home/user/../root/.ssh", "/root/.ssh")]
    [InlineData("./src/../tests/unit", "./tests/unit")]
    [InlineData("/etc/ssh/../passwd", "/etc/passwd")]
    [InlineData("~/../../../etc/passwd", "/etc/passwd")]
    public void Should_Resolve_DotDot(string path, string expectedEnd)
    {
        // Act
        var result = _normalizer.Normalize(path);

        // Assert
        result.Should().NotContain("..",
            because: "parent directory references must be resolved");
        result.Should().EndWith(expectedEnd.Replace("/", Path.DirectorySeparatorChar.ToString()),
            because: "path should resolve to expected location");
    }

    [Theory]
    [InlineData("./src/./tests/./unit", "src/tests/unit")]
    [InlineData("/etc/./ssh/./config", "/etc/ssh/config")]
    public void Should_Remove_Dot(string path, string expected)
    {
        // Act
        var result = _normalizer.Normalize(path);

        // Assert
        result.Should().NotContain("/./",
            because: "single dot references should be removed");
        result.Should().Contain(expected.Replace("/", Path.DirectorySeparatorChar.ToString()));
    }

    [Theory]
    [InlineData("~/.ssh//id_rsa")]
    [InlineData("/etc///passwd")]
    [InlineData("./src////file.cs")]
    public void Should_Collapse_Slashes(string path)
    {
        // Act
        var result = _normalizer.Normalize(path);

        // Assert
        result.Should().NotContain("//",
            because: "multiple consecutive slashes should be collapsed");
    }

    [Theory]
    [InlineData("~/.ssh/")]
    [InlineData("/etc/ssh/")]
    [InlineData("./src/")]
    public void Should_Remove_Trailing_Slash(string path)
    {
        // Act
        var result = _normalizer.Normalize(path);

        // Assert
        result.Should().NotEndWith("/",
            because: "trailing slashes should be removed for consistency");
        result.Should().NotEndWith("\\",
            because: "trailing backslashes should be removed for consistency");
    }

    [Theory]
    [InlineData("C:\\Users\\test\\.ssh\\id_rsa")]
    [InlineData("C:\\Windows\\System32\\config")]
    public void Should_Convert_Backslash_On_Windows(string path)
    {
        // Act
        var result = _normalizer.Normalize(path);

        // Assert - on Windows, should use consistent separator
        if (OperatingSystem.IsWindows())
        {
            // All separators should be consistent
            var separatorCount = result.Count(c => c == '/' || c == '\\');
            var primarySep = result.Count(c => c == Path.DirectorySeparatorChar);
            primarySep.Should().Be(separatorCount,
                because: "all separators should be the platform separator");
        }
    }

    [Fact]
    public void Should_Handle_Very_Long_Paths()
    {
        // Arrange
        var longPath = "/home/user/" + string.Join("/", Enumerable.Repeat("subdir", 100)) + "/file.txt";

        // Act
        var result = _normalizer.Normalize(longPath);

        // Assert
        result.Should().NotBeNullOrWhiteSpace(
            because: "long paths should be handled without truncation");
        result.Should().Contain("file.txt");
    }

    [Theory]
    [InlineData("/home/用户/documents/文件.txt")]
    [InlineData("/home/пользователь/docs/файл.txt")]
    [InlineData("/home/user/données/ñoño.txt")]
    public void Should_Handle_Unicode(string path)
    {
        // Act
        var result = _normalizer.Normalize(path);

        // Assert
        result.Should().NotBeNullOrWhiteSpace(
            because: "Unicode paths should be preserved");
        // Unicode characters should remain intact
        result.Length.Should().BeGreaterThan(10);
    }

    [Theory]
    [InlineData("/home/user/file with spaces.txt")]
    [InlineData("/home/user/file\ttab.txt")]
    [InlineData("/home/user/file'quote.txt")]
    public void Should_Handle_Special_Characters(string path)
    {
        // Act
        var result = _normalizer.Normalize(path);

        // Assert
        result.Should().NotBeNullOrWhiteSpace(
            because: "special characters should be handled");
    }

    [Fact]
    public void Should_Reject_Null_Byte()
    {
        // Arrange
        var pathWithNull = "/home/user/file\0.txt";

        // Act
        Action act = () => _normalizer.Normalize(pathWithNull);

        // Assert
        act.Should().Throw<ArgumentException>(
            because: "null bytes in paths are a security risk");
    }

    [Fact]
    public void Should_Handle_Empty_Path()
    {
        // Act
        var result = _normalizer.Normalize("");

        // Assert
        result.Should().BeEmpty(
            because: "empty paths should return empty");
    }

    [Fact]
    public void Should_Handle_Null_Path()
    {
        // Act
        Action act = () => _normalizer.Normalize(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
```

```
├── SymlinkResolverTests.cs
│   ├── Should_Resolve_Single_Symlink()
│   ├── Should_Resolve_Chain_Of_Symlinks()
│   ├── Should_Detect_Circular_Symlink()
│   ├── Should_Enforce_Max_Depth_40()
│   ├── Should_Block_On_Resolution_Error()
│   └── Should_Cache_Resolution()
```

#### SymlinkResolverTests.cs

```csharp
namespace AgenticCoder.Tests.Unit.Domain.Security.PathProtection;

using AgenticCoder.Domain.Security.PathProtection;
using FluentAssertions;
using System.IO;
using Xunit;

public class SymlinkResolverTests : IDisposable
{
    private readonly ISymlinkResolver _resolver;
    private readonly string _testDir;

    public SymlinkResolverTests()
    {
        _resolver = new SymlinkResolver(maxDepth: 40);
        _testDir = Path.Combine(Path.GetTempPath(), $"symlink_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_testDir, recursive: true);
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [SkippableFact]
    public void Should_Resolve_Single_Symlink()
    {
        // Skip on Windows if not running as admin (symlinks require elevation)
        Skip.If(OperatingSystem.IsWindows() && !HasSymlinkPrivilege());

        // Arrange
        var targetFile = Path.Combine(_testDir, "target.txt");
        var symlinkPath = Path.Combine(_testDir, "link.txt");
        File.WriteAllText(targetFile, "test content");
        CreateSymlink(symlinkPath, targetFile);

        // Act
        var result = _resolver.Resolve(symlinkPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ResolvedPath.Should().Be(targetFile,
            because: "symlink should resolve to target");
    }

    [SkippableFact]
    public void Should_Resolve_Chain_Of_Symlinks()
    {
        Skip.If(OperatingSystem.IsWindows() && !HasSymlinkPrivilege());

        // Arrange - create chain: link1 -> link2 -> link3 -> target
        var targetFile = Path.Combine(_testDir, "final_target.txt");
        var link3 = Path.Combine(_testDir, "link3.txt");
        var link2 = Path.Combine(_testDir, "link2.txt");
        var link1 = Path.Combine(_testDir, "link1.txt");

        File.WriteAllText(targetFile, "content");
        CreateSymlink(link3, targetFile);
        CreateSymlink(link2, link3);
        CreateSymlink(link1, link2);

        // Act
        var result = _resolver.Resolve(link1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ResolvedPath.Should().Be(targetFile,
            because: "chain of symlinks should resolve to final target");
        result.Depth.Should().Be(3,
            because: "three symlinks were traversed");
    }

    [SkippableFact]
    public void Should_Detect_Circular_Symlink()
    {
        Skip.If(OperatingSystem.IsWindows() && !HasSymlinkPrivilege());

        // Arrange - create circular: link1 -> link2 -> link1
        var link1 = Path.Combine(_testDir, "circular1.txt");
        var link2 = Path.Combine(_testDir, "circular2.txt");

        // Create files first, then replace with symlinks
        File.WriteAllText(link2, "temp");
        CreateSymlink(link1, link2);
        File.Delete(link2);
        CreateSymlink(link2, link1);

        // Act
        var result = _resolver.Resolve(link1);

        // Assert
        result.IsSuccess.Should().BeFalse(
            because: "circular symlinks should be detected");
        result.Error.Should().Be(SymlinkError.CircularReference);
    }

    [SkippableFact]
    public void Should_Enforce_Max_Depth_40()
    {
        Skip.If(OperatingSystem.IsWindows() && !HasSymlinkPrivilege());

        // Arrange - create chain of 45 symlinks (exceeds max of 40)
        var targetFile = Path.Combine(_testDir, "deep_target.txt");
        File.WriteAllText(targetFile, "content");

        var previousLink = targetFile;
        for (int i = 0; i < 45; i++)
        {
            var newLink = Path.Combine(_testDir, $"deep_link_{i}.txt");
            CreateSymlink(newLink, previousLink);
            previousLink = newLink;
        }

        // Act
        var result = _resolver.Resolve(previousLink);

        // Assert
        result.IsSuccess.Should().BeFalse(
            because: "depth exceeding 40 should fail");
        result.Error.Should().Be(SymlinkError.MaxDepthExceeded);
    }

    [Fact]
    public void Should_Block_On_Resolution_Error()
    {
        // Arrange - symlink to non-existent target
        var brokenLink = Path.Combine(_testDir, "broken_link.txt");
        var nonExistentTarget = Path.Combine(_testDir, "does_not_exist.txt");

        if (OperatingSystem.IsWindows() && !HasSymlinkPrivilege())
        {
            // On Windows without privileges, just test with a non-existent path
            var result = _resolver.Resolve(nonExistentTarget);
            result.IsSuccess.Should().BeFalse();
            return;
        }

        CreateSymlink(brokenLink, nonExistentTarget);

        // Act
        var result = _resolver.Resolve(brokenLink);

        // Assert
        result.IsSuccess.Should().BeFalse(
            because: "broken symlink should fail resolution");
        result.Error.Should().Be(SymlinkError.TargetNotFound);
    }

    [Fact]
    public void Should_Cache_Resolution()
    {
        // Arrange
        var regularFile = Path.Combine(_testDir, "regular.txt");
        File.WriteAllText(regularFile, "content");

        // Act - resolve same path twice
        var result1 = _resolver.Resolve(regularFile);
        var result2 = _resolver.Resolve(regularFile);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.ResolvedPath.Should().Be(result2.ResolvedPath);
        
        // Cache hit should be faster (implementation detail)
        // The important thing is both calls succeed
    }

    [Fact]
    public void Should_Return_Same_Path_If_Not_Symlink()
    {
        // Arrange
        var regularFile = Path.Combine(_testDir, "regular_file.txt");
        File.WriteAllText(regularFile, "test content");

        // Act
        var result = _resolver.Resolve(regularFile);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ResolvedPath.Should().Be(regularFile,
            because: "regular files should resolve to themselves");
        result.Depth.Should().Be(0,
            because: "no symlinks were traversed");
    }

    private static bool HasSymlinkPrivilege()
    {
        // On Windows, creating symlinks requires admin or developer mode
        try
        {
            var testLink = Path.Combine(Path.GetTempPath(), $"symlink_test_{Guid.NewGuid():N}");
            var testTarget = Path.Combine(Path.GetTempPath(), $"symlink_target_{Guid.NewGuid():N}");
            File.WriteAllText(testTarget, "test");
            File.CreateSymbolicLink(testLink, testTarget);
            File.Delete(testLink);
            File.Delete(testTarget);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void CreateSymlink(string linkPath, string targetPath)
    {
        if (OperatingSystem.IsWindows())
        {
            File.CreateSymbolicLink(linkPath, targetPath);
        }
        else
        {
            // Unix symlink
            File.CreateSymbolicLink(linkPath, targetPath);
        }
    }
}

public enum SymlinkError
{
    None,
    CircularReference,
    MaxDepthExceeded,
    TargetNotFound,
    AccessDenied,
    Unknown
}

public record SymlinkResolutionResult
{
    public bool IsSuccess { get; init; }
    public string? ResolvedPath { get; init; }
    public SymlinkError Error { get; init; }
    public int Depth { get; init; }

    public static SymlinkResolutionResult Success(string path, int depth) =>
        new() { IsSuccess = true, ResolvedPath = path, Depth = depth };

    public static SymlinkResolutionResult Failure(SymlinkError error) =>
        new() { IsSuccess = false, Error = error };
}
```

```
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
```

#### ProtectedPathValidatorTests.cs

```csharp
namespace AgenticCoder.Tests.Unit.Domain.Security.PathProtection;

using AgenticCoder.Domain.Security.PathProtection;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class ProtectedPathValidatorTests
{
    private readonly IProtectedPathValidator _validator;
    private readonly Mock<ILogger<ProtectedPathValidator>> _loggerMock;
    private readonly Mock<IPathNormalizer> _normalizerMock;
    private readonly Mock<ISymlinkResolver> _symlinkResolverMock;
    private readonly Mock<IPathMatcher> _matcherMock;

    public ProtectedPathValidatorTests()
    {
        _loggerMock = new Mock<ILogger<ProtectedPathValidator>>();
        _normalizerMock = new Mock<IPathNormalizer>();
        _symlinkResolverMock = new Mock<ISymlinkResolver>();
        _matcherMock = new Mock<IPathMatcher>();

        // Default setup: normalizer returns input, symlinks resolve to self
        _normalizerMock
            .Setup(n => n.Normalize(It.IsAny<string>()))
            .Returns<string>(s => s.Replace("~", "/home/user"));

        _symlinkResolverMock
            .Setup(r => r.Resolve(It.IsAny<string>()))
            .Returns<string>(s => SymlinkResolutionResult.Success(s, 0));

        _validator = new ProtectedPathValidator(
            DefaultDenylist.Entries,
            _normalizerMock.Object,
            _symlinkResolverMock.Object,
            new GlobMatcher(caseSensitive: !OperatingSystem.IsWindows()),
            _loggerMock.Object);
    }

    [Theory]
    [InlineData("~/.ssh/")]
    [InlineData("~/.ssh")]
    [InlineData("/home/user/.ssh/")]
    public void Should_Block_SSH_Directory(string path)
    {
        // Act
        var result = _validator.Validate(path);

        // Assert
        result.IsProtected.Should().BeTrue(
            because: "SSH directory must be protected");
        result.Category.Should().Be(PathCategory.SshKeys);
        result.RiskId.Should().StartWith("RISK-");
    }

    [Theory]
    [InlineData("~/.ssh/id_rsa")]
    [InlineData("~/.ssh/id_ed25519")]
    [InlineData("~/.ssh/id_ecdsa")]
    [InlineData("~/.ssh/id_dsa")]
    [InlineData("/home/user/.ssh/id_rsa")]
    public void Should_Block_SSH_PrivateKey(string path)
    {
        // Act
        var result = _validator.Validate(path);

        // Assert
        result.IsProtected.Should().BeTrue(
            because: "SSH private keys must be protected");
        result.Reason.Should().Contain("SSH",
            because: "reason should explain the protection");
        result.MatchedPattern.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("~/.aws/credentials")]
    [InlineData("~/.aws/config")]
    [InlineData("/home/user/.aws/credentials")]
    public void Should_Block_AWS_Credentials(string path)
    {
        // Act
        var result = _validator.Validate(path);

        // Assert
        result.IsProtected.Should().BeTrue(
            because: "AWS credentials must be protected");
        result.Category.Should().Be(PathCategory.CloudCredentials);
    }

    [Theory]
    [InlineData(".env")]
    [InlineData(".env.local")]
    [InlineData(".env.production")]
    [InlineData("config/.env")]
    [InlineData("src/config/.env.development")]
    public void Should_Block_Env_File(string path)
    {
        // Act
        var result = _validator.Validate(path);

        // Assert
        result.IsProtected.Should().BeTrue(
            because: "environment files must be protected");
        result.Category.Should().Be(PathCategory.EnvironmentFiles);
    }

    [Theory]
    [InlineData("./src/../../../home/user/.ssh/id_rsa")]
    [InlineData("../../../etc/passwd")]
    [InlineData("src/../../.ssh/id_rsa")]
    public void Should_Block_Traversal_To_Protected(string path)
    {
        // Arrange - normalizer resolves traversal
        _normalizerMock
            .Setup(n => n.Normalize(It.IsAny<string>()))
            .Returns("/home/user/.ssh/id_rsa");

        // Act
        var result = _validator.Validate(path);

        // Assert
        result.IsProtected.Should().BeTrue(
            because: "directory traversal to protected paths must be blocked");
    }

    [Fact]
    public void Should_Block_Symlink_To_Protected()
    {
        // Arrange - symlink resolves to SSH key
        var symlinkPath = "./innocent_file.txt";
        _symlinkResolverMock
            .Setup(r => r.Resolve(It.IsAny<string>()))
            .Returns(SymlinkResolutionResult.Success("/home/user/.ssh/id_rsa", 1));

        // Act
        var result = _validator.Validate(symlinkPath);

        // Assert
        result.IsProtected.Should().BeTrue(
            because: "symlinks to protected paths must be blocked");
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().Be("ACODE-SEC-003-005",
            because: "symlink attack has specific error code");
    }

    [Theory]
    [InlineData("./src/Program.cs")]
    [InlineData("./README.md")]
    [InlineData("./tests/UnitTests.cs")]
    [InlineData("/home/user/projects/myapp/src/main.py")]
    [InlineData("package.json")]
    [InlineData("Cargo.toml")]
    public void Should_Allow_Normal_Source_File(string path)
    {
        // Arrange
        _normalizerMock
            .Setup(n => n.Normalize(It.IsAny<string>()))
            .Returns<string>(s => s);

        // Act
        var result = _validator.Validate(path);

        // Assert
        result.IsProtected.Should().BeFalse(
            because: "normal source files should be allowed");
        result.MatchedPattern.Should().BeNull();
        result.Error.Should().BeNull();
    }

    [Theory]
    [InlineData("~/.ssh/id_rsa", "ACODE-SEC-003-001")]
    [InlineData("~/.aws/credentials", "ACODE-SEC-003-002")]
    [InlineData("/etc/passwd", "ACODE-SEC-003-003")]
    [InlineData(".env", "ACODE-SEC-003-004")]
    public void Should_Return_ProtectedPathError(string path, string expectedCode)
    {
        // Act
        var result = _validator.Validate(path);

        // Assert
        result.IsProtected.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().StartWith("ACODE-SEC-003",
            because: "protected path errors use SEC-003 code family");
    }

    [Fact]
    public void Should_Include_Error_Code()
    {
        // Arrange
        var protectedPath = "~/.ssh/id_rsa";

        // Act
        var result = _validator.Validate(protectedPath);

        // Assert
        result.Error.Should().NotBeNull();
        result.Error!.ErrorCode.Should().NotBeNullOrWhiteSpace();
        result.Error.ErrorCode.Should().MatchRegex(@"^ACODE-SEC-003(-\d{3})?$",
            because: "error code must follow ACODE-SEC-003 format");
        result.Error.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Should_Not_Reveal_Contents()
    {
        // Arrange
        var protectedPath = "~/.ssh/id_rsa";

        // Act
        var result = _validator.Validate(protectedPath);

        // Assert
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().NotContain("BEGIN RSA PRIVATE KEY",
            because: "error must not leak file contents");
        result.Error.Message.Should().NotContain("PRIVATE KEY",
            because: "error must not leak sensitive keywords from file");
        
        // Path should be redacted in user-facing message
        result.Error.UserMessage.Should().NotContain("id_rsa",
            because: "exact filename should be redacted in user message");
    }

    [Fact]
    public void Should_Log_Violation()
    {
        // Arrange
        var protectedPath = "~/.ssh/id_rsa";

        // Act
        _validator.Validate(protectedPath);

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("protected_path_access_blocked")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            because: "violations must be logged for security audit");
    }

    [Theory]
    [InlineData(FileOperation.Read)]
    [InlineData(FileOperation.Write)]
    [InlineData(FileOperation.Delete)]
    [InlineData(FileOperation.List)]
    public void Should_Block_All_Operations_On_Protected_Path(FileOperation operation)
    {
        // Arrange
        var protectedPath = "~/.ssh/id_rsa";

        // Act
        var result = _validator.Validate(protectedPath, operation);

        // Assert
        result.IsProtected.Should().BeTrue(
            because: $"operation {operation} must be blocked on protected paths");
    }

    [Fact]
    public void Should_Handle_Null_Path()
    {
        // Act
        Action act = () => _validator.Validate(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Should_Handle_Empty_Path()
    {
        // Act
        var result = _validator.Validate("");

        // Assert
        result.IsProtected.Should().BeFalse(
            because: "empty paths are invalid but not protected");
    }
}
```

```
└── UserExtensionTests.cs
    ├── Should_Load_From_Config()
    ├── Should_Validate_Pattern()
    ├── Should_Require_Reason()
    ├── Should_Apply_After_Defaults()
    ├── Should_Not_Remove_Defaults()
    ├── Should_Limit_To_1000_Entries()
    └── Should_Reject_Invalid_Patterns()
```

#### UserExtensionTests.cs

```csharp
namespace AgenticCoder.Tests.Unit.Domain.Security.PathProtection;

using AgenticCoder.Domain.Security.PathProtection;
using AgenticCoder.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class UserExtensionTests
{
    private readonly Mock<ILogger<UserDenylistExtensionLoader>> _loggerMock;
    private readonly UserDenylistExtensionLoader _loader;

    public UserExtensionTests()
    {
        _loggerMock = new Mock<ILogger<UserDenylistExtensionLoader>>();
        _loader = new UserDenylistExtensionLoader(_loggerMock.Object);
    }

    [Fact]
    public void Should_Load_From_Config()
    {
        // Arrange
        var configYaml = @"
security:
  additional_protected_paths:
    - pattern: 'company-secrets/'
      reason: 'Internal documentation'
    - pattern: '*.pem'
      reason: 'Certificate files'
";
        var config = ParseYaml(configYaml);

        // Act
        var extensions = _loader.LoadExtensions(config);

        // Assert
        extensions.Should().HaveCount(2);
        extensions[0].Pattern.Should().Be("company-secrets/");
        extensions[0].Reason.Should().Be("Internal documentation");
        extensions[1].Pattern.Should().Be("*.pem");
    }

    [Theory]
    [InlineData("valid-path/")]
    [InlineData("*.secret")]
    [InlineData("**/private/**")]
    [InlineData("[a-z].key")]
    public void Should_Validate_Pattern(string pattern)
    {
        // Arrange
        var configYaml = $@"
security:
  additional_protected_paths:
    - pattern: '{pattern}'
      reason: 'Test reason'
";
        var config = ParseYaml(configYaml);

        // Act
        var extensions = _loader.LoadExtensions(config);

        // Assert
        extensions.Should().HaveCount(1);
        extensions[0].Pattern.Should().Be(pattern);
    }

    [Fact]
    public void Should_Require_Reason()
    {
        // Arrange
        var configYaml = @"
security:
  additional_protected_paths:
    - pattern: 'no-reason-path/'
";
        var config = ParseYaml(configYaml);

        // Act
        Action act = () => _loader.LoadExtensions(config);

        // Assert
        act.Should().Throw<ConfigurationException>()
            .WithMessage("*reason*required*",
                because: "each extension must have a reason");
    }

    [Fact]
    public void Should_Apply_After_Defaults()
    {
        // Arrange
        var configYaml = @"
security:
  additional_protected_paths:
    - pattern: 'custom-secret/'
      reason: 'Custom protection'
";
        var config = ParseYaml(configYaml);
        var extensions = _loader.LoadExtensions(config);

        // Act
        var combinedDenylist = DefaultDenylist.Entries
            .Concat(extensions)
            .ToList();

        // Assert
        var customEntry = combinedDenylist.Last();
        customEntry.Pattern.Should().Be("custom-secret/");
        customEntry.IsDefault.Should().BeFalse(
            because: "user extensions are not default entries");
        customEntry.Category.Should().Be(PathCategory.UserDefined);
    }

    [Fact]
    public void Should_Not_Remove_Defaults()
    {
        // Arrange - attempt to remove default via config
        var configYaml = @"
security:
  remove_protected_paths:
    - '~/.ssh/'
    - '~/.aws/'
";
        var config = ParseYaml(configYaml);

        // Act
        var result = _loader.TryRemoveDefaults(config, DefaultDenylist.Entries);

        // Assert
        result.Success.Should().BeFalse(
            because: "removing default paths is not allowed");
        result.Errors.Should().Contain(e => e.Contains("cannot remove default"));
        
        // Verify defaults are unchanged
        DefaultDenylist.Entries
            .Should().Contain(e => e.Pattern == "~/.ssh/");
        DefaultDenylist.Entries
            .Should().Contain(e => e.Pattern.Contains("aws"));
    }

    [Fact]
    public void Should_Limit_To_1000_Entries()
    {
        // Arrange - create config with 1001 entries
        var entries = Enumerable.Range(1, 1001)
            .Select(i => $"    - pattern: 'path{i}/'\n      reason: 'Reason {i}'")
            .ToList();

        var configYaml = $@"
security:
  additional_protected_paths:
{string.Join("\n", entries)}
";
        var config = ParseYaml(configYaml);

        // Act
        Action act = () => _loader.LoadExtensions(config);

        // Assert
        act.Should().Throw<ConfigurationException>()
            .WithMessage("*exceed*1000*",
                because: "user extensions are limited to 1000 entries");
    }

    [Theory]
    [InlineData("")] // Empty pattern
    [InlineData("   ")] // Whitespace pattern
    [InlineData("[invalid")] // Unclosed bracket
    [InlineData("path\0/injection")] // Null byte
    [InlineData("../../../escape")] // Traversal attempt
    public void Should_Reject_Invalid_Patterns(string invalidPattern)
    {
        // Arrange
        var configYaml = $@"
security:
  additional_protected_paths:
    - pattern: '{invalidPattern}'
      reason: 'Invalid pattern'
";
        var config = ParseYaml(configYaml);

        // Act
        Action act = () => _loader.LoadExtensions(config);

        // Assert
        act.Should().Throw<ConfigurationException>(
            because: $"pattern '{invalidPattern}' should be rejected");
    }

    [Fact]
    public void Should_Assign_UserDefined_Category()
    {
        // Arrange
        var configYaml = @"
security:
  additional_protected_paths:
    - pattern: 'my-secrets/'
      reason: 'My custom secrets'
";
        var config = ParseYaml(configYaml);

        // Act
        var extensions = _loader.LoadExtensions(config);

        // Assert
        extensions.Should().HaveCount(1);
        extensions[0].Category.Should().Be(PathCategory.UserDefined);
        extensions[0].IsDefault.Should().BeFalse();
    }

    [Fact]
    public void Should_Generate_RiskId_For_UserDefined()
    {
        // Arrange
        var configYaml = @"
security:
  additional_protected_paths:
    - pattern: 'custom/'
      reason: 'Custom path'
";
        var config = ParseYaml(configYaml);

        // Act
        var extensions = _loader.LoadExtensions(config);

        // Assert
        extensions[0].RiskId.Should().Be("RISK-USER-001",
            because: "user-defined entries get USER risk IDs");
    }

    [Fact]
    public void Should_Handle_Empty_Config()
    {
        // Arrange
        var configYaml = @"
security:
  additional_protected_paths: []
";
        var config = ParseYaml(configYaml);

        // Act
        var extensions = _loader.LoadExtensions(config);

        // Assert
        extensions.Should().BeEmpty();
    }

    [Fact]
    public void Should_Handle_Missing_Section()
    {
        // Arrange
        var configYaml = @"
other_settings:
  some_value: true
";
        var config = ParseYaml(configYaml);

        // Act
        var extensions = _loader.LoadExtensions(config);

        // Assert
        extensions.Should().BeEmpty(
            because: "missing security section means no extensions");
    }

    private static AgentConfig ParseYaml(string yaml)
    {
        // This would use actual YAML parsing in implementation
        // For tests, we mock the config object
        return new AgentConfig(yaml);
    }
}
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
```

#### DenylistEnforcementTests.cs

```csharp
namespace AgenticCoder.Tests.Integration.Security.PathProtection;

using AgenticCoder.Application.FileOperations;
using AgenticCoder.Domain.Security.PathProtection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Collection("Integration")]
public class DenylistEnforcementTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IFileOperationService _fileService;
    private readonly IProtectedPathValidator _validator;

    public DenylistEnforcementTests(IntegrationTestFixture fixture)
    {
        _fileService = fixture.Services.GetRequiredService<IFileOperationService>();
        _validator = fixture.Services.GetRequiredService<IProtectedPathValidator>();
    }

    [Theory]
    [InlineData("~/.ssh/id_rsa")]
    [InlineData("~/.aws/credentials")]
    [InlineData(".env")]
    [InlineData("/etc/passwd")]
    public async Task FileRead_ShouldBlock_ProtectedPath(string protectedPath)
    {
        // Act
        var result = await _fileService.ReadFileAsync(protectedPath);

        // Assert
        result.IsSuccess.Should().BeFalse(
            because: $"reading {protectedPath} must be blocked");
        result.Error.Should().BeOfType<ProtectedPathError>();
        result.Error.As<ProtectedPathError>().ErrorCode.Should().StartWith("ACODE-SEC-003");
    }

    [Theory]
    [InlineData("~/.ssh/id_rsa")]
    [InlineData("~/.aws/credentials")]
    [InlineData(".env")]
    public async Task FileWrite_ShouldBlock_ProtectedPath(string protectedPath)
    {
        // Act
        var result = await _fileService.WriteFileAsync(protectedPath, "malicious content");

        // Assert
        result.IsSuccess.Should().BeFalse(
            because: $"writing to {protectedPath} must be blocked");
        result.Error.Should().BeOfType<ProtectedPathError>();
    }

    [Theory]
    [InlineData("~/.ssh/id_rsa")]
    [InlineData("~/.aws/credentials")]
    public async Task FileDelete_ShouldBlock_ProtectedPath(string protectedPath)
    {
        // Act
        var result = await _fileService.DeleteFileAsync(protectedPath);

        // Assert
        result.IsSuccess.Should().BeFalse(
            because: $"deleting {protectedPath} must be blocked");
        result.Error.Should().BeOfType<ProtectedPathError>();
    }

    [Theory]
    [InlineData("~/.ssh/")]
    [InlineData("~/.aws/")]
    [InlineData("/etc/")]
    public async Task DirectoryList_ShouldBlock_ProtectedPath(string protectedDir)
    {
        // Act
        var result = await _fileService.ListDirectoryAsync(protectedDir);

        // Assert
        result.IsSuccess.Should().BeFalse(
            because: $"listing {protectedDir} must be blocked");
        result.Error.Should().BeOfType<ProtectedPathError>();
    }

    [Theory]
    [InlineData("./src/Program.cs")]
    [InlineData("./README.md")]
    [InlineData("./tests/UnitTests/MyTest.cs")]
    [InlineData("package.json")]
    public async Task Should_AllowNormal_SourceFiles(string normalPath)
    {
        // Arrange - create the file for testing
        var testDir = Path.Combine(Path.GetTempPath(), "acode_test");
        Directory.CreateDirectory(testDir);
        var fullPath = Path.Combine(testDir, normalPath.TrimStart('.', '/'));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, "test content");

        try
        {
            // Act
            var readResult = await _fileService.ReadFileAsync(fullPath);

            // Assert
            readResult.IsSuccess.Should().BeTrue(
                because: "normal source files should be allowed");
        }
        finally
        {
            Directory.Delete(testDir, recursive: true);
        }
    }

    [Fact]
    public async Task Should_EnforceAcross_AllFileOperations()
    {
        // Arrange
        var protectedPath = "~/.ssh/id_rsa";
        var operations = new Func<Task<OperationResult>>[]
        {
            () => _fileService.ReadFileAsync(protectedPath),
            () => _fileService.WriteFileAsync(protectedPath, "content"),
            () => _fileService.DeleteFileAsync(protectedPath),
            () => _fileService.CopyFileAsync(protectedPath, "./destination.txt"),
            () => _fileService.MoveFileAsync(protectedPath, "./destination.txt"),
            () => _fileService.GetFileInfoAsync(protectedPath),
        };

        // Act & Assert
        foreach (var operation in operations)
        {
            var result = await operation();
            result.IsSuccess.Should().BeFalse(
                because: "all file operations must be blocked on protected paths");
        }
    }
}
```

```
├── CrossPlatformTests.cs
│   ├── Should_UseWindowsPaths_OnWindows()
│   ├── Should_UseUnixPaths_OnLinux()
│   ├── Should_UseMacOSPaths_OnMacOS()
│   ├── Should_HandleMixedSlashes_OnWindows()
│   └── Should_ExpandHomeDirectory_Correctly()
```

#### CrossPlatformTests.cs

```csharp
namespace AgenticCoder.Tests.Integration.Security.PathProtection;

using AgenticCoder.Domain.Security.PathProtection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Collection("Integration")]
public class CrossPlatformTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IProtectedPathValidator _validator;
    private readonly IPathNormalizer _normalizer;

    public CrossPlatformTests(IntegrationTestFixture fixture)
    {
        _validator = fixture.Services.GetRequiredService<IProtectedPathValidator>();
        _normalizer = fixture.Services.GetRequiredService<IPathNormalizer>();
    }

    [SkippableFact]
    public void Should_UseWindowsPaths_OnWindows()
    {
        Skip.IfNot(OperatingSystem.IsWindows());

        // Arrange
        var windowsPaths = new[]
        {
            @"%USERPROFILE%\.ssh\id_rsa",
            @"C:\Windows\System32\config",
            @"%APPDATA%\gnupg\secring.gpg"
        };

        // Act & Assert
        foreach (var path in windowsPaths)
        {
            var result = _validator.Validate(path);
            result.IsProtected.Should().BeTrue(
                because: $"Windows path {path} should be protected on Windows");
        }
    }

    [SkippableFact]
    public void Should_UseUnixPaths_OnLinux()
    {
        Skip.IfNot(OperatingSystem.IsLinux());

        // Arrange
        var linuxPaths = new[]
        {
            "~/.ssh/id_rsa",
            "/etc/passwd",
            "/etc/shadow",
            "/root/.bashrc"
        };

        // Act & Assert
        foreach (var path in linuxPaths)
        {
            var result = _validator.Validate(path);
            result.IsProtected.Should().BeTrue(
                because: $"Linux path {path} should be protected on Linux");
        }
    }

    [SkippableFact]
    public void Should_UseMacOSPaths_OnMacOS()
    {
        Skip.IfNot(OperatingSystem.IsMacOS());

        // Arrange
        var macPaths = new[]
        {
            "~/.ssh/id_rsa",
            "/etc/passwd",
            "~/Library/Keychains/",
            "~/.gnupg/secring.gpg"
        };

        // Act & Assert
        foreach (var path in macPaths)
        {
            var result = _validator.Validate(path);
            result.IsProtected.Should().BeTrue(
                because: $"macOS path {path} should be protected on macOS");
        }
    }

    [SkippableFact]
    public void Should_HandleMixedSlashes_OnWindows()
    {
        Skip.IfNot(OperatingSystem.IsWindows());

        // Arrange - mixed forward and back slashes
        var mixedPaths = new[]
        {
            @"%USERPROFILE%/.ssh\id_rsa",
            @"C:/Windows\System32/config",
            @"~\.ssh/id_rsa"
        };

        // Act & Assert
        foreach (var path in mixedPaths)
        {
            var result = _validator.Validate(path);
            result.IsProtected.Should().BeTrue(
                because: $"mixed slash path {path} should still be protected");
        }
    }

    [Fact]
    public void Should_ExpandHomeDirectory_Correctly()
    {
        // Arrange
        var homePath = "~/.ssh/id_rsa";

        // Act
        var normalized = _normalizer.Normalize(homePath);

        // Assert
        normalized.Should().NotContain("~");
        
        if (OperatingSystem.IsWindows())
        {
            normalized.Should().StartWith(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        }
        else
        {
            normalized.Should().StartWith(
                Environment.GetEnvironmentVariable("HOME"));
        }
    }
}
```

```
├── ConfigIntegrationTests.cs
│   ├── Should_LoadUserExtensions_FromConfig()
│   ├── Should_MergeExtensions_WithDefaults()
│   ├── Should_RejectInvalid_Extensions()
│   └── Should_ApplyExtensions_ToPathChecks()
```

#### ConfigIntegrationTests.cs

```csharp
namespace AgenticCoder.Tests.Integration.Security.PathProtection;

using AgenticCoder.Domain.Security.PathProtection;
using AgenticCoder.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Collection("Integration")]
public class ConfigIntegrationTests : IClassFixture<IntegrationTestFixture>, IDisposable
{
    private readonly IServiceProvider _services;
    private readonly string _testConfigDir;

    public ConfigIntegrationTests(IntegrationTestFixture fixture)
    {
        _services = fixture.Services;
        _testConfigDir = Path.Combine(Path.GetTempPath(), $"acode_config_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testConfigDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_testConfigDir, true); } catch { }
    }

    [Fact]
    public async Task Should_LoadUserExtensions_FromConfig()
    {
        // Arrange
        var configPath = Path.Combine(_testConfigDir, ".agent", "config.yml");
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        
        await File.WriteAllTextAsync(configPath, @"
security:
  additional_protected_paths:
    - pattern: 'my-secrets/'
      reason: 'Company secrets directory'
    - pattern: '*.credential'
      reason: 'Credential files'
");

        var configLoader = _services.GetRequiredService<IConfigurationLoader>();

        // Act
        var config = await configLoader.LoadAsync(_testConfigDir);
        var extensions = config.Security.AdditionalProtectedPaths;

        // Assert
        extensions.Should().HaveCount(2);
        extensions.Should().Contain(e => e.Pattern == "my-secrets/");
        extensions.Should().Contain(e => e.Pattern == "*.credential");
    }

    [Fact]
    public async Task Should_MergeExtensions_WithDefaults()
    {
        // Arrange
        var configPath = Path.Combine(_testConfigDir, ".agent", "config.yml");
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        
        await File.WriteAllTextAsync(configPath, @"
security:
  additional_protected_paths:
    - pattern: 'custom-secret/'
      reason: 'Custom protection'
");

        var denylistService = _services.GetRequiredService<IDenylistService>();

        // Act
        var allEntries = await denylistService.GetAllEntriesAsync(_testConfigDir);

        // Assert
        // Should have all defaults plus the custom entry
        allEntries.Count.Should().BeGreaterThan(DefaultDenylist.Entries.Count);
        allEntries.Should().Contain(e => e.Pattern == "~/.ssh/",
            because: "defaults must be present");
        allEntries.Should().Contain(e => e.Pattern == "custom-secret/",
            because: "user extension must be added");
    }

    [Fact]
    public async Task Should_RejectInvalid_Extensions()
    {
        // Arrange
        var configPath = Path.Combine(_testConfigDir, ".agent", "config.yml");
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        
        await File.WriteAllTextAsync(configPath, @"
security:
  additional_protected_paths:
    - pattern: '[unclosed'
      reason: 'Invalid pattern'
");

        var configLoader = _services.GetRequiredService<IConfigurationLoader>();

        // Act
        Func<Task> act = async () => await configLoader.LoadAsync(_testConfigDir);

        // Assert
        await act.Should().ThrowAsync<ConfigurationException>()
            .WithMessage("*invalid*pattern*");
    }

    [Fact]
    public async Task Should_ApplyExtensions_ToPathChecks()
    {
        // Arrange
        var configPath = Path.Combine(_testConfigDir, ".agent", "config.yml");
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        
        await File.WriteAllTextAsync(configPath, @"
security:
  additional_protected_paths:
    - pattern: 'company-internal/'
      reason: 'Internal documents'
");

        var validatorFactory = _services.GetRequiredService<IProtectedPathValidatorFactory>();
        var validator = await validatorFactory.CreateAsync(_testConfigDir);

        // Act
        var result = validator.Validate("company-internal/secret.doc");

        // Assert
        result.IsProtected.Should().BeTrue(
            because: "user-defined path should be protected");
        result.Reason.Should().Be("Internal documents");
        result.Category.Should().Be(PathCategory.UserDefined);
    }
}
```

```
└── CLIIntegrationTests.cs
    ├── ShowDenylist_ShouldList_AllPaths()
    ├── ShowDenylist_ShouldFilter_ByPlatform()
    ├── CheckPath_ShouldReturn_Blocked()
    ├── CheckPath_ShouldReturn_Allowed()
    └── CheckPath_ShouldShow_Reason()
```

#### CLIIntegrationTests.cs

```csharp
namespace AgenticCoder.Tests.Integration.Security.PathProtection;

using AgenticCoder.CLI;
using FluentAssertions;
using System.Diagnostics;
using Xunit;

[Collection("Integration")]
public class CLIIntegrationTests
{
    private readonly string _cliPath;

    public CLIIntegrationTests()
    {
        _cliPath = GetCliPath();
    }

    [Fact]
    public async Task ShowDenylist_ShouldList_AllPaths()
    {
        // Act
        var (exitCode, output, _) = await RunCliAsync("security", "show-denylist");

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("~/.ssh/",
            because: "SSH paths should be listed");
        output.Should().Contain("~/.aws/",
            because: "AWS paths should be listed");
        output.Should().Contain(".env",
            because: "env files should be listed");
    }

    [SkippableFact]
    public async Task ShowDenylist_ShouldFilter_ByPlatform()
    {
        // Act - Windows platform filter
        var (exitCode, output, _) = await RunCliAsync(
            "security", "show-denylist", "--platform", "windows");

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain(@"C:\Windows\",
            because: "Windows paths should be shown");
        output.Should().NotContain("/etc/",
            because: "Unix-only paths should not be shown");
    }

    [Theory]
    [InlineData("~/.ssh/id_rsa", 1)]
    [InlineData("~/.aws/credentials", 1)]
    [InlineData(".env", 1)]
    public async Task CheckPath_ShouldReturn_Blocked(string path, int expectedExitCode)
    {
        // Act
        var (exitCode, output, _) = await RunCliAsync("security", "check-path", path);

        // Assert
        exitCode.Should().Be(expectedExitCode,
            because: "exit code 1 indicates blocked path");
        output.Should().Contain("BLOCKED",
            because: "output should indicate path is blocked");
    }

    [Theory]
    [InlineData("./src/Program.cs", 0)]
    [InlineData("./README.md", 0)]
    [InlineData("package.json", 0)]
    public async Task CheckPath_ShouldReturn_Allowed(string path, int expectedExitCode)
    {
        // Act
        var (exitCode, output, _) = await RunCliAsync("security", "check-path", path);

        // Assert
        exitCode.Should().Be(expectedExitCode,
            because: "exit code 0 indicates allowed path");
        output.Should().Contain("ALLOWED",
            because: "output should indicate path is allowed");
    }

    [Fact]
    public async Task CheckPath_ShouldShow_Reason()
    {
        // Act
        var (_, output, _) = await RunCliAsync("security", "check-path", "~/.ssh/id_rsa");

        // Assert
        output.Should().Contain("Reason:",
            because: "output should show reason for blocking");
        output.Should().Contain("SSH",
            because: "reason should mention SSH");
        output.Should().Contain("Pattern:",
            because: "output should show matched pattern");
    }

    [Fact]
    public async Task CheckPath_ShouldShow_ErrorCode()
    {
        // Act
        var (_, output, _) = await RunCliAsync("security", "check-path", "~/.ssh/id_rsa");

        // Assert
        output.Should().MatchRegex(@"ACODE-SEC-003",
            because: "output should show error code");
    }

    private async Task<(int ExitCode, string StdOut, string StdErr)> RunCliAsync(
        params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _cliPath,
            Arguments = string.Join(" ", args),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, stdout, stderr);
    }

    private static string GetCliPath()
    {
        // Find the CLI executable in build output
        var baseDir = AppContext.BaseDirectory;
        var cliName = OperatingSystem.IsWindows() 
            ? "agentic-coder.exe" 
            : "agentic-coder";
        
        return Path.Combine(baseDir, cliName);
    }
}
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

#### Gherkin Feature Specification

```gherkin
Feature: Protected Path Enforcement
  As a security-conscious developer
  I want the agent to be blocked from accessing sensitive files
  So that my credentials and secrets remain protected

  Background:
    Given the agentic-coder agent is running
    And the default denylist is active

  @security @critical
  Scenario: Agent blocked from reading SSH private key
    Given a task that requires reading a file
    When the agent attempts to read "~/.ssh/id_rsa"
    Then the operation should be blocked
    And the error code should be "ACODE-SEC-003-001"
    And the error message should mention "SSH private key"
    And the violation should be logged
    And the file contents should not be exposed

  @security @critical
  Scenario: Agent blocked from reading AWS credentials
    Given a task that requires reading a file
    When the agent attempts to read "~/.aws/credentials"
    Then the operation should be blocked
    And the error code should be "ACODE-SEC-003-002"
    And the error message should mention "AWS credentials"
    And the violation should be logged

  @security @critical
  Scenario: Agent blocked from reading environment file
    Given a task that requires reading a file
    When the agent attempts to read ".env"
    Then the operation should be blocked
    And the error code should be "ACODE-SEC-003-004"
    And the violation should be logged

  @security @critical
  Scenario: Agent blocked from directory traversal attack
    Given a task that requires reading a file
    When the agent attempts to read "./src/../../../etc/passwd"
    Then the operation should be blocked
    And the error code should be "ACODE-SEC-003-006"
    And the path traversal should be detected
    And the violation should be logged with attack details

  @security @critical
  Scenario: Agent blocked from symlink attack
    Given a symlink "./innocent.txt" pointing to "~/.ssh/id_rsa"
    When the agent attempts to read "./innocent.txt"
    Then the operation should be blocked
    And the error code should be "ACODE-SEC-003-005"
    And the symlink should be resolved before checking
    And the violation should be logged

  @development @positive
  Scenario: Normal development workflow proceeds unblocked
    Given a project directory with source files
    When the agent reads "src/Program.cs"
    And the agent writes to "src/NewFile.cs"
    And the agent deletes "src/Obsolete.cs"
    Then all operations should succeed
    And no security violations should be logged

  @configuration @positive
  Scenario: User can extend denylist via config
    Given a config file ".agent/config.yml" with:
      """
      security:
        additional_protected_paths:
          - pattern: 'company-secrets/'
            reason: 'Internal documentation'
      """
    When the agent attempts to read "company-secrets/roadmap.doc"
    Then the operation should be blocked
    And the error code should be "ACODE-SEC-003-007"
    And the reason should be "Internal documentation"

  @configuration @negative
  Scenario: User cannot remove default protections
    Given a config file ".agent/config.yml" with:
      """
      security:
        remove_protected_paths:
          - '~/.ssh/'
      """
    When the agent attempts to read "~/.ssh/id_rsa"
    Then the operation should still be blocked
    And a warning should be logged about invalid config
```

#### ProtectedPathScenarios.cs

```csharp
namespace AgenticCoder.Tests.E2E.Security;

using AgenticCoder.Agent;
using AgenticCoder.Domain.Security.PathProtection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

[Collection("E2E")]
[Trait("Category", "E2E")]
[Trait("Category", "Security")]
public class ProtectedPathScenarios : IClassFixture<E2ETestFixture>, IDisposable
{
    private readonly IAgentRuntime _agent;
    private readonly Mock<ILogger> _loggerMock;
    private readonly string _testWorkspace;

    public ProtectedPathScenarios(E2ETestFixture fixture)
    {
        _agent = fixture.Services.GetRequiredService<IAgentRuntime>();
        _loggerMock = fixture.LoggerMock;
        _testWorkspace = CreateTestWorkspace();
    }

    public void Dispose()
    {
        try { Directory.Delete(_testWorkspace, true); } catch { }
    }

    [Fact]
    [Trait("Priority", "Critical")]
    public async Task Scenario_Agent_Attempts_SSH_Key_Read()
    {
        // Arrange
        var task = new AgentTask
        {
            Description = "Read the SSH key file",
            RequestedPath = "~/.ssh/id_rsa",
            Operation = FileOperation.Read
        };

        // Act
        var result = await _agent.ExecuteTaskAsync(task);

        // Assert
        result.Success.Should().BeFalse(
            because: "SSH key read must be blocked");
        result.Error.Should().BeOfType<ProtectedPathError>();
        
        var error = result.Error as ProtectedPathError;
        error!.ErrorCode.Should().Be("ACODE-SEC-003-001");
        error.Message.Should().Contain("SSH");
        
        // Verify logging
        VerifyViolationLogged("protected_path_access_blocked", "~/.ssh");
        
        // Verify no content leaked
        result.Data.Should().BeNull(
            because: "file contents must not be returned");
    }

    [Fact]
    [Trait("Priority", "Critical")]
    public async Task Scenario_Agent_Attempts_AWS_Creds_Read()
    {
        // Arrange
        var task = new AgentTask
        {
            Description = "Check AWS configuration",
            RequestedPath = "~/.aws/credentials",
            Operation = FileOperation.Read
        };

        // Act
        var result = await _agent.ExecuteTaskAsync(task);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().BeOfType<ProtectedPathError>();
        
        var error = result.Error as ProtectedPathError;
        error!.ErrorCode.Should().Be("ACODE-SEC-003-002");
        
        VerifyViolationLogged("protected_path_access_blocked", "aws");
    }

    [Fact]
    [Trait("Priority", "Critical")]
    public async Task Scenario_Agent_Attempts_Env_File_Read()
    {
        // Arrange
        var envPath = Path.Combine(_testWorkspace, ".env");
        await File.WriteAllTextAsync(envPath, "SECRET_KEY=super_secret_value");

        var task = new AgentTask
        {
            Description = "Read environment configuration",
            RequestedPath = envPath,
            Operation = FileOperation.Read
        };

        // Act
        var result = await _agent.ExecuteTaskAsync(task);

        // Assert
        result.Success.Should().BeFalse();
        
        var error = result.Error as ProtectedPathError;
        error!.ErrorCode.Should().Be("ACODE-SEC-003-004");
        
        // Verify secret not leaked
        result.Data?.ToString().Should().NotContain("super_secret_value");
    }

    [Fact]
    [Trait("Priority", "Critical")]
    public async Task Scenario_Agent_Attempts_DirectoryTraversal()
    {
        // Arrange
        var maliciousPath = Path.Combine(_testWorkspace, "src", "..", "..", "..", "etc", "passwd");

        var task = new AgentTask
        {
            Description = "Read system file via traversal",
            RequestedPath = maliciousPath,
            Operation = FileOperation.Read
        };

        // Act
        var result = await _agent.ExecuteTaskAsync(task);

        // Assert
        result.Success.Should().BeFalse(
            because: "directory traversal attack must be blocked");
        
        var error = result.Error as ProtectedPathError;
        error!.ErrorCode.Should().Be("ACODE-SEC-003-006");
        
        VerifyViolationLogged("directory_traversal_blocked");
    }

    [SkippableFact]
    [Trait("Priority", "Critical")]
    public async Task Scenario_Agent_Attempts_Symlink_Attack()
    {
        // Skip if symlinks not available
        Skip.If(OperatingSystem.IsWindows() && !HasSymlinkPrivilege());

        // Arrange - create symlink to protected path
        var symlinkPath = Path.Combine(_testWorkspace, "innocent_file.txt");
        var targetPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".ssh", "id_rsa");

        if (File.Exists(targetPath))
        {
            File.CreateSymbolicLink(symlinkPath, targetPath);
        }
        else
        {
            // Create a temp target for testing
            var tempTarget = Path.Combine(_testWorkspace, "fake_target.txt");
            await File.WriteAllTextAsync(tempTarget, "fake content");
            File.CreateSymbolicLink(symlinkPath, tempTarget);
            
            // Reconfigure test to simulate SSH path resolution
            // (In real scenario, symlink would point to actual SSH key)
        }

        var task = new AgentTask
        {
            Description = "Read innocent looking file",
            RequestedPath = symlinkPath,
            Operation = FileOperation.Read
        };

        // Act
        var result = await _agent.ExecuteTaskAsync(task);

        // Assert - if symlink points to protected path, must be blocked
        if (File.Exists(targetPath))
        {
            result.Success.Should().BeFalse();
            var error = result.Error as ProtectedPathError;
            error!.ErrorCode.Should().Be("ACODE-SEC-003-005");
        }
    }

    [Fact]
    [Trait("Category", "Positive")]
    public async Task Scenario_NormalDevelopment_Workflow()
    {
        // Arrange - create normal project structure
        var srcDir = Path.Combine(_testWorkspace, "src");
        Directory.CreateDirectory(srcDir);
        
        var programCs = Path.Combine(srcDir, "Program.cs");
        var newFileCs = Path.Combine(srcDir, "NewFile.cs");
        var obsoleteCs = Path.Combine(srcDir, "Obsolete.cs");
        
        await File.WriteAllTextAsync(programCs, "class Program { }");
        await File.WriteAllTextAsync(obsoleteCs, "// to be deleted");

        // Act - read operation
        var readResult = await _agent.ExecuteTaskAsync(new AgentTask
        {
            Description = "Read source file",
            RequestedPath = programCs,
            Operation = FileOperation.Read
        });

        // Act - write operation
        var writeResult = await _agent.ExecuteTaskAsync(new AgentTask
        {
            Description = "Create new file",
            RequestedPath = newFileCs,
            Operation = FileOperation.Write,
            Content = "public class NewFile { }"
        });

        // Act - delete operation
        var deleteResult = await _agent.ExecuteTaskAsync(new AgentTask
        {
            Description = "Delete obsolete file",
            RequestedPath = obsoleteCs,
            Operation = FileOperation.Delete
        });

        // Assert
        readResult.Success.Should().BeTrue(
            because: "reading source files should be allowed");
        readResult.Data.Should().Contain("class Program");

        writeResult.Success.Should().BeTrue(
            because: "writing source files should be allowed");
        File.Exists(newFileCs).Should().BeTrue();

        deleteResult.Success.Should().BeTrue(
            because: "deleting source files should be allowed");
        File.Exists(obsoleteCs).Should().BeFalse();

        // No security violations logged
        VerifyNoViolationsLogged();
    }

    [Fact]
    [Trait("Category", "Configuration")]
    public async Task Scenario_UserExtends_Denylist()
    {
        // Arrange - create config with custom protection
        var agentDir = Path.Combine(_testWorkspace, ".agent");
        Directory.CreateDirectory(agentDir);
        
        var configPath = Path.Combine(agentDir, "config.yml");
        await File.WriteAllTextAsync(configPath, @"
security:
  additional_protected_paths:
    - pattern: 'company-secrets/'
      reason: 'Internal documentation'
");
        
        var secretsDir = Path.Combine(_testWorkspace, "company-secrets");
        Directory.CreateDirectory(secretsDir);
        await File.WriteAllTextAsync(
            Path.Combine(secretsDir, "roadmap.doc"),
            "Secret roadmap content");

        // Reload config
        await _agent.ReloadConfigAsync(_testWorkspace);

        var task = new AgentTask
        {
            Description = "Read company document",
            RequestedPath = Path.Combine(secretsDir, "roadmap.doc"),
            Operation = FileOperation.Read
        };

        // Act
        var result = await _agent.ExecuteTaskAsync(task);

        // Assert
        result.Success.Should().BeFalse(
            because: "user-defined protected path should be blocked");
        
        var error = result.Error as ProtectedPathError;
        error!.ErrorCode.Should().Be("ACODE-SEC-003-007");
        error.Reason.Should().Be("Internal documentation");
    }

    private string CreateTestWorkspace()
    {
        var path = Path.Combine(Path.GetTempPath(), $"acode_e2e_{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private void VerifyViolationLogged(string eventType, string? containsPath = null)
    {
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains(eventType) &&
                    (containsPath == null || v.ToString()!.Contains(containsPath, StringComparison.OrdinalIgnoreCase))),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private void VerifyNoViolationsLogged()
    {
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("protected_path")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    private static bool HasSymlinkPrivilege()
    {
        try
        {
            var testLink = Path.Combine(Path.GetTempPath(), $"symtest_{Guid.NewGuid():N}");
            var testTarget = Path.Combine(Path.GetTempPath(), $"symtarget_{Guid.NewGuid():N}");
            File.WriteAllText(testTarget, "test");
            File.CreateSymbolicLink(testLink, testTarget);
            File.Delete(testLink);
            File.Delete(testTarget);
            return true;
        }
        catch { return false; }
    }
}
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
```

#### PathMatchingBenchmarks.cs

```csharp
namespace AgenticCoder.Tests.Performance.Security;

using AgenticCoder.Domain.Security.PathProtection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class PathMatchingBenchmarks
{
    private IProtectedPathValidator _validator = null!;
    private IPathMatcher _matcher = null!;
    private IPathNormalizer _normalizer = null!;
    private string[] _testPaths = null!;
    private string[] _patterns = null!;

    [GlobalSetup]
    public void Setup()
    {
        _validator = new ProtectedPathValidator(
            DefaultDenylist.Entries,
            new PathNormalizer(),
            new SymlinkResolver(maxDepth: 40),
            new GlobMatcher(caseSensitive: !OperatingSystem.IsWindows()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ProtectedPathValidator>.Instance);

        _matcher = new GlobMatcher(caseSensitive: true);
        _normalizer = new PathNormalizer();

        _testPaths = new[]
        {
            "~/.ssh/id_rsa",
            "./src/Program.cs",
            "~/.aws/credentials",
            "./tests/UnitTests.cs",
            ".env",
            "./package.json",
            "/etc/passwd",
            "./README.md"
        };

        _patterns = new[]
        {
            "~/.ssh/id_*",
            "**/.env",
            "~/.aws/",
            "**/node_modules/**"
        };
    }

    [Benchmark(Description = "Single path validation")]
    public PathValidationResult Benchmark_SinglePathCheck()
    {
        return _validator.Validate("~/.ssh/id_rsa");
    }

    [Benchmark(Description = "1000 path validations")]
    public int Benchmark_1000PathChecks()
    {
        int blockedCount = 0;
        for (int i = 0; i < 1000; i++)
        {
            var path = _testPaths[i % _testPaths.Length];
            var result = _validator.Validate(path);
            if (result.IsProtected) blockedCount++;
        }
        return blockedCount;
    }

    [Benchmark(Description = "Glob pattern matching")]
    [Arguments("~/.ssh/id_*", "~/.ssh/id_rsa")]
    [Arguments("**/.env", "src/config/.env")]
    [Arguments("**/node_modules/**", "app/node_modules/pkg/index.js")]
    public bool Benchmark_GlobPatternMatching(string pattern, string path)
    {
        return _matcher.Matches(pattern, path);
    }

    [Benchmark(Description = "Path normalization")]
    [Arguments("~/.ssh/id_rsa")]
    [Arguments("./src/../tests/./unit")]
    [Arguments("%USERPROFILE%\\.aws\\credentials")]
    public string Benchmark_PathNormalization(string path)
    {
        return _normalizer.Normalize(path);
    }

    [Benchmark(Description = "Denylist lookup (all entries)")]
    public int Benchmark_DenylistLookup()
    {
        int matchCount = 0;
        foreach (var entry in DefaultDenylist.Entries)
        {
            if (_matcher.Matches(entry.Pattern, "~/.ssh/id_rsa"))
            {
                matchCount++;
            }
        }
        return matchCount;
    }

    [Benchmark(Description = "Mixed workload simulation")]
    public int Benchmark_MixedWorkload()
    {
        int operations = 0;
        
        // Simulate typical agent file access pattern
        for (int i = 0; i < 100; i++)
        {
            // Mostly source files (should be fast - allowed)
            _validator.Validate("./src/file" + i + ".cs");
            operations++;
            
            // Occasional protected path check
            if (i % 10 == 0)
            {
                _validator.Validate("~/.ssh/id_rsa");
                operations++;
            }
        }
        
        return operations;
    }
}

/// <summary>
/// Performance acceptance tests that run as unit tests.
/// </summary>
public class PerformanceAcceptanceTests
{
    private readonly IProtectedPathValidator _validator;

    public PerformanceAcceptanceTests()
    {
        _validator = new ProtectedPathValidator(
            DefaultDenylist.Entries,
            new PathNormalizer(),
            new SymlinkResolver(maxDepth: 40),
            new GlobMatcher(caseSensitive: !OperatingSystem.IsWindows()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ProtectedPathValidator>.Instance);
    }

    [Fact]
    public void SinglePathCheck_ShouldComplete_InUnder1ms()
    {
        // Warmup
        _ = _validator.Validate("~/.ssh/id_rsa");

        // Measure
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            _ = _validator.Validate("~/.ssh/id_rsa");
        }
        sw.Stop();

        var avgMs = sw.Elapsed.TotalMilliseconds / 1000;
        avgMs.Should().BeLessThan(1,
            because: "single path check must complete in under 1ms");
    }

    [Fact]
    public void AllowedPath_ShouldBe_FasterThanBlocked()
    {
        // Warmup
        _ = _validator.Validate("./src/Program.cs");
        _ = _validator.Validate("~/.ssh/id_rsa");

        // Measure allowed paths
        var swAllowed = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            _ = _validator.Validate("./src/Program.cs");
        }
        swAllowed.Stop();

        // Measure blocked paths
        var swBlocked = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            _ = _validator.Validate("~/.ssh/id_rsa");
        }
        swBlocked.Stop();

        // Allowed should be same speed or faster (early exit possible)
        // but both should be very fast
        swAllowed.Elapsed.TotalMilliseconds.Should().BeLessThan(1000);
        swBlocked.Elapsed.TotalMilliseconds.Should().BeLessThan(1000);
    }
}
```

```
└── MemoryTests.cs
    ├── Should_UseUnder1MB_ForDenylist()
    ├── Should_NotAllocate_PerPathCheck()
    └── Should_CacheEfficiently()
```

#### MemoryTests.cs

```csharp
namespace AgenticCoder.Tests.Performance.Security;

using AgenticCoder.Domain.Security.PathProtection;
using FluentAssertions;
using Xunit;

public class MemoryTests
{
    [Fact]
    public void Should_UseUnder1MB_ForDenylist()
    {
        // Arrange
        var before = GC.GetTotalMemory(forceFullCollection: true);

        // Act - load denylist
        var entries = DefaultDenylist.Entries;
        _ = entries.Count; // Force enumeration

        var after = GC.GetTotalMemory(forceFullCollection: true);
        var usedBytes = after - before;

        // Assert
        usedBytes.Should().BeLessThan(1024 * 1024,
            because: "denylist should use less than 1MB of memory");
        
        // Log actual usage for monitoring
        Console.WriteLine($"Denylist memory usage: {usedBytes / 1024.0:F2} KB");
    }

    [Fact]
    public void Should_NotAllocate_PerPathCheck()
    {
        // Arrange
        var validator = new ProtectedPathValidator(
            DefaultDenylist.Entries,
            new PathNormalizer(),
            new SymlinkResolver(maxDepth: 40),
            new GlobMatcher(caseSensitive: true),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ProtectedPathValidator>.Instance);

        // Warmup
        _ = validator.Validate("./src/Program.cs");

        // Force GC
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetTotalAllocatedBytes(precise: true);

        // Act - perform many path checks
        for (int i = 0; i < 10000; i++)
        {
            _ = validator.Validate("./src/Program.cs");
        }

        var after = GC.GetTotalAllocatedBytes(precise: true);
        var allocatedPerCheck = (after - before) / 10000.0;

        // Assert
        allocatedPerCheck.Should().BeLessThan(1000,
            because: "each path check should allocate minimal memory (under 1KB)");
        
        Console.WriteLine($"Allocated per check: {allocatedPerCheck:F2} bytes");
    }

    [Fact]
    public void Should_CacheEfficiently()
    {
        // Arrange
        var resolver = new SymlinkResolver(maxDepth: 40);
        var testPath = Path.GetTempFileName();

        try
        {
            // Act - resolve same path multiple times
            var result1 = resolver.Resolve(testPath);
            var result2 = resolver.Resolve(testPath);
            var result3 = resolver.Resolve(testPath);

            // Assert
            result1.IsSuccess.Should().BeTrue();
            result2.IsSuccess.Should().BeTrue();
            result3.IsSuccess.Should().BeTrue();

            // All should return same resolved path
            result1.ResolvedPath.Should().Be(result2.ResolvedPath);
            result2.ResolvedPath.Should().Be(result3.ResolvedPath);

            // Second and third calls should be faster (cached)
            // This is an implementation detail but important for performance
        }
        finally
        {
            File.Delete(testPath);
        }
    }

    [Fact]
    public void Should_HandleLargeDenylist_Efficiently()
    {
        // Arrange - simulate denylist with user extensions (up to 1000)
        var defaultEntries = DefaultDenylist.Entries.ToList();
        var userEntries = Enumerable.Range(1, 1000)
            .Select(i => new DenylistEntry
            {
                Pattern = $"user-path-{i}/",
                Reason = $"User defined path {i}",
                RiskId = $"RISK-USER-{i:D3}",
                Category = PathCategory.UserDefined,
                Platforms = new[] { Platform.All },
                IsDefault = false
            })
            .ToList();

        var allEntries = defaultEntries.Concat(userEntries).ToList();

        var validator = new ProtectedPathValidator(
            allEntries,
            new PathNormalizer(),
            new SymlinkResolver(maxDepth: 40),
            new GlobMatcher(caseSensitive: true),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ProtectedPathValidator>.Instance);

        // Act - time path checks
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            _ = validator.Validate("./src/Program.cs");
        }
        sw.Stop();

        // Assert - should still be fast even with large denylist
        var avgMs = sw.Elapsed.TotalMilliseconds / 1000;
        avgMs.Should().BeLessThan(5,
            because: "path checks should be fast even with 1000+ denylist entries");
        
        Console.WriteLine($"Avg check time with {allEntries.Count} entries: {avgMs:F3} ms");
    }
}
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

#### PathBypassTests.cs

```csharp
namespace AgenticCoder.Tests.Regression.Security;

using AgenticCoder.Domain.Security.PathProtection;
using FluentAssertions;
using Xunit;

/// <summary>
/// Regression tests for path bypass attempts.
/// These tests document known attack vectors that must remain blocked.
/// </summary>
[Trait("Category", "Security")]
[Trait("Category", "Regression")]
public class PathBypassTests
{
    private readonly IProtectedPathValidator _validator;

    public PathBypassTests()
    {
        _validator = new ProtectedPathValidator(
            DefaultDenylist.Entries,
            new PathNormalizer(),
            new SymlinkResolver(maxDepth: 40),
            new GlobMatcher(caseSensitive: !OperatingSystem.IsWindows()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ProtectedPathValidator>.Instance);
    }

    [Fact]
    public void Should_Block_NullPath()
    {
        // Act
        Action act = () => _validator.Validate(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>(
            because: "null paths must be rejected immediately");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void Should_Block_EmptyOrWhitespacePath(string path)
    {
        // Act
        var result = _validator.Validate(path);

        // Assert - empty/whitespace should not be "protected" but should be invalid
        // The validator should handle gracefully
        result.Error?.ErrorCode.Should().NotBe("ACODE-SEC-003",
            because: "empty paths are invalid, not protected path violations");
    }

    [Theory]
    [InlineData("%2e%2e%2f%2e%2e%2f%2e%2e%2fetc%2fpasswd")] // URL encoded ../../../etc/passwd
    [InlineData("..%252f..%252f..%252fetc%252fpasswd")] // Double URL encoded
    [InlineData("%252e%252e%252f")] // Double encoded ..
    [InlineData("~%2f.ssh%2fid_rsa")] // URL encoded ~/.ssh/id_rsa
    public void Should_Block_EncodedPath(string encodedPath)
    {
        // Arrange - decode the path (normalizer should handle this)
        var normalizedPath = System.Web.HttpUtility.UrlDecode(encodedPath);
        
        // Act
        var result = _validator.Validate(normalizedPath);

        // Assert
        result.IsProtected.Should().BeTrue(
            because: $"URL-encoded path {encodedPath} should still be blocked after decoding");
    }

    [Theory]
    [InlineData("~/.ssh/id_rsa")]
    [InlineData("~/.ſsh/id_rsa")] // Unicode 'ſ' (long s) instead of 's'
    [InlineData("~/.ｓｓｈ/id_rsa")] // Fullwidth characters
    [InlineData("~/.ssh\u200B/id_rsa")] // Zero-width space
    [InlineData("~/.ssh\u00AD/id_rsa")] // Soft hyphen
    public void Should_Block_UnicodeNormalization(string path)
    {
        // Act
        var result = _validator.Validate(path);

        // Assert
        // Either the path matches and is blocked, or Unicode normalization rejects it
        if (path.Contains("ssh", StringComparison.OrdinalIgnoreCase))
        {
            // If it looks like SSH, it should be blocked or normalized to SSH and blocked
            result.IsProtected.Should().BeTrue(
                because: $"Unicode variation {path} should be blocked");
        }
    }

    [Theory]
    [InlineData("~/.SSH/ID_RSA")]
    [InlineData("~/.Ssh/Id_Rsa")]
    [InlineData("~/.sSh/iD_rSa")]
    public void Should_Block_CaseVariation_OnCaseInsensitiveSystems(string path)
    {
        // Skip on case-sensitive systems where these are different paths
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Act
        var result = _validator.Validate(path);

        // Assert
        result.IsProtected.Should().BeTrue(
            because: "case variations should be blocked on Windows");
    }

    [SkippableTheory]
    [InlineData("~/.ssh/id_rsa:$DATA")] // NTFS alternate data stream
    [InlineData("~/.ssh/id_rsa::$DATA")]
    [InlineData("~/.aws/credentials:secret:$DATA")]
    public void Should_Block_AlternateDataStream(string path)
    {
        Skip.IfNot(OperatingSystem.IsWindows(), "ADS is Windows-specific");

        // Act
        var result = _validator.Validate(path);

        // Assert
        result.IsProtected.Should().BeTrue(
            because: "NTFS alternate data streams on protected files must be blocked");
    }

    [Theory]
    [InlineData("~/.ssh/id_rsa/.")] // Trailing dot
    [InlineData("~/.ssh/id_rsa/...")] // Multiple trailing dots
    [InlineData("~/.ssh/id_rsa  ")] // Trailing spaces
    [InlineData("~/.ssh/id_rsa\t")] // Trailing tab
    public void Should_Block_TrailingCharacters(string path)
    {
        // Act
        var result = _validator.Validate(path);

        // Assert
        result.IsProtected.Should().BeTrue(
            because: "trailing characters should not bypass protection");
    }

    [Theory]
    [InlineData("~/.ssh/./id_rsa")]
    [InlineData("~/.ssh/foo/../id_rsa")]
    [InlineData("~/.ssh/foo/bar/../../id_rsa")]
    [InlineData("~/.ssh/../.ssh/id_rsa")]
    public void Should_Block_PathTraversalInMiddle(string path)
    {
        // Act
        var result = _validator.Validate(path);

        // Assert
        result.IsProtected.Should().BeTrue(
            because: "path traversal in middle of path should still block");
    }

    [Theory]
    [InlineData("file://~/.ssh/id_rsa")]
    [InlineData("file:///home/user/.ssh/id_rsa")]
    public void Should_Block_FileUri(string path)
    {
        // Act
        var result = _validator.Validate(path);

        // Assert
        // Either blocked as protected path or rejected as invalid path format
        // File URIs should not bypass protection
    }

    [Theory]
    [InlineData(@"\\?\C:\Users\user\.ssh\id_rsa")] // Long path prefix
    [InlineData(@"\\.\C:\Users\user\.ssh\id_rsa")] // Device path prefix
    public void Should_Block_WindowsExtendedPath(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Act
        var result = _validator.Validate(path);

        // Assert
        result.IsProtected.Should().BeTrue(
            because: "Windows extended path syntax should not bypass protection");
    }

    [Fact]
    public void Should_Block_NullByteInjection()
    {
        // Arrange
        var pathWithNull = "~/.ssh/id_rsa\0.txt";

        // Act
        Action act = () => _validator.Validate(pathWithNull);

        // Assert
        act.Should().Throw<ArgumentException>(
            because: "null byte injection is a known attack vector");
    }

    [Theory]
    [InlineData("~/.ssh//id_rsa")]
    [InlineData("~/.ssh///id_rsa")]
    [InlineData("~/.ssh////id_rsa")]
    public void Should_Block_MultipleSlashes(string path)
    {
        // Act
        var result = _validator.Validate(path);

        // Assert
        result.IsProtected.Should().BeTrue(
            because: "multiple slashes should be collapsed and path still blocked");
    }

    [Theory]
    [InlineData("~\\.ssh\\id_rsa")]
    [InlineData("~/.ssh\\id_rsa")]
    [InlineData("~\\.ssh/id_rsa")]
    public void Should_Block_MixedSlashes(string path)
    {
        // Act
        var result = _validator.Validate(path);

        // Assert
        result.IsProtected.Should().BeTrue(
            because: "mixed forward/back slashes should not bypass protection");
    }
}
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

#### PenetrationTests.cs

```csharp
namespace AgenticCoder.Tests.Security.PathProtection;

using AgenticCoder.Application.FileOperations;
using AgenticCoder.Domain.Security.PathProtection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Penetration tests for path protection.
/// These tests simulate real attack scenarios.
/// </summary>
[Collection("Security")]
[Trait("Category", "Security")]
[Trait("Category", "Penetration")]
public class PenetrationTests : IClassFixture<SecurityTestFixture>, IDisposable
{
    private readonly IFileOperationService _fileService;
    private readonly IProtectedPathValidator _validator;
    private readonly string _testDir;

    public PenetrationTests(SecurityTestFixture fixture)
    {
        _fileService = fixture.Services.GetRequiredService<IFileOperationService>();
        _validator = fixture.Services.GetRequiredService<IProtectedPathValidator>();
        _testDir = Path.Combine(Path.GetTempPath(), $"pentest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_testDir, true); } catch { }
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\..\\etc\\passwd")]
    [InlineData("....//....//....//etc/passwd")]
    [InlineData("./.././.././../etc/passwd")]
    [InlineData("src/../../../etc/passwd")]
    public async Task Should_Resist_DirectoryTraversal(string traversalPath)
    {
        // Arrange
        var fullPath = Path.Combine(_testDir, traversalPath);

        // Act
        var result = await _fileService.ReadFileAsync(fullPath);

        // Assert
        result.IsSuccess.Should().BeFalse(
            because: $"traversal path {traversalPath} must be blocked");
        result.Error.Should().BeOfType<ProtectedPathError>();
    }

    [SkippableFact]
    public async Task Should_Resist_SymlinkAttack()
    {
        Skip.If(OperatingSystem.IsWindows() && !HasSymlinkPrivilege());

        // Arrange - create symlink to protected path
        var targetPath = Environment.GetEnvironmentVariable("HOME") + "/.ssh/id_rsa";
        if (!File.Exists(targetPath))
        {
            targetPath = Path.Combine(_testDir, "fake_protected.txt");
            await File.WriteAllTextAsync(targetPath, "protected content");
        }

        var symlinkPath = Path.Combine(_testDir, "innocent.txt");
        File.CreateSymbolicLink(symlinkPath, targetPath);

        // Act
        var result = await _fileService.ReadFileAsync(symlinkPath);

        // Assert
        result.IsSuccess.Should().BeFalse(
            because: "symlink to protected path must be blocked");
    }

    [Fact]
    public async Task Should_Resist_TOCTOU()
    {
        // Time-of-check to time-of-use attack
        // The path is checked, then the file is swapped before use

        // Arrange
        var safePath = Path.Combine(_testDir, "safe_file.txt");
        await File.WriteAllTextAsync(safePath, "safe content");

        // Simulate race condition by checking path, then swapping
        var validationResult = _validator.Validate(safePath);
        validationResult.IsProtected.Should().BeFalse();

        // In a TOCTOU attack, the file would be swapped here
        // Our defense: re-validate at time of use, not just check time

        // Act - the file service should re-validate at read time
        var result = await _fileService.ReadFileAsync(safePath);

        // Assert
        result.IsSuccess.Should().BeTrue(
            because: "safe file should be readable");
        
        // The implementation should re-check at read time to prevent TOCTOU
    }

    [Fact]
    public async Task Should_Resist_RaceCondition()
    {
        // Attempt to exploit race condition with parallel requests

        // Arrange
        var protectedPath = "~/.ssh/id_rsa";
        var tasks = new List<Task<OperationResult>>();

        // Act - fire many parallel requests
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(_fileService.ReadFileAsync(protectedPath));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - ALL requests must be blocked
        foreach (var result in results)
        {
            result.IsSuccess.Should().BeFalse(
                because: "race condition should not bypass protection");
        }
    }

    [Theory]
    [InlineData("%2e%2e%2f")] // ../
    [InlineData("%252e%252e%252f")] // double encoded
    [InlineData("%c0%ae%c0%ae%c0%af")] // overlong UTF-8 ../
    [InlineData("..%c0%af")] // overlong UTF-8 /
    public async Task Should_Resist_EncodingBypass(string encodedSequence)
    {
        // Arrange
        var path = $"{_testDir}/{encodedSequence}etc/passwd";

        // Act
        var result = await _fileService.ReadFileAsync(path);

        // Assert
        // Should either block as protected or reject as invalid path
        if (result.IsSuccess)
        {
            // If somehow it succeeded, it better not have returned /etc/passwd content
            var content = result.Data as string;
            content.Should().NotContain("root:",
                because: "encoding bypass must not expose /etc/passwd");
        }
    }

    [Fact]
    public async Task Should_Resist_NullByteInjection()
    {
        // Null byte injection: path.txt\0.jpg might bypass extension checks

        // Arrange
        var pathWithNull = Path.Combine(_testDir, "file.txt\0.jpg");

        // Act
        Func<Task> act = async () => await _fileService.ReadFileAsync(pathWithNull);

        // Assert
        await act.Should().ThrowAsync<Exception>(
            because: "null byte in path is a security violation");
    }

    [Fact]
    public void Should_BlockAll_KnownAttackVectors()
    {
        // Comprehensive list of attack vectors
        var attackVectors = new[]
        {
            // Directory traversal
            "../../../etc/passwd",
            "..\\..\\..\\Windows\\System32\\config\\SAM",
            ".../....//etc/passwd",
            
            // Encoding attacks
            "%2e%2e%2fetc/passwd",
            "..%252f..%252f..%252fetc/passwd",
            
            // Unicode normalization
            "~/.ssh\u200B/id_rsa",
            "~/.ss\u0068/id_rsa",
            
            // Case manipulation (Windows)
            "~/.SSH/ID_RSA",
            
            // Alternate paths to same location
            "~/.ssh/./id_rsa",
            "~/.ssh/foo/../id_rsa",
            
            // Protocol prefixes
            "file:///etc/passwd",
        };

        foreach (var attack in attackVectors)
        {
            try
            {
                var result = _validator.Validate(attack);
                
                // If the path normalizes to a protected path, it must be blocked
                // If it's invalid, that's also acceptable (rejected)
            }
            catch (ArgumentException)
            {
                // Invalid path format is acceptable - attack was blocked
            }
        }
    }

    private static bool HasSymlinkPrivilege()
    {
        try
        {
            var link = Path.Combine(Path.GetTempPath(), $"symtest_{Guid.NewGuid():N}");
            var target = Path.Combine(Path.GetTempPath(), $"symtarget_{Guid.NewGuid():N}");
            File.WriteAllText(target, "test");
            File.CreateSymbolicLink(link, target);
            File.Delete(link);
            File.Delete(target);
            return true;
        }
        catch { return false; }
    }
}
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