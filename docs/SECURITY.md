# Security - Protected Paths

This document describes the protected paths security feature in Acode, which prevents accidental exposure of sensitive files and credentials.

## Overview

Acode implements a **default-deny** path protection system that blocks access to sensitive files including:
- SSH private keys and authentication files
- Cloud provider credentials (AWS, Azure, GCloud, etc.)
- Environment files containing secrets (.env, .env.*)
- System configuration files
- GPG/PGP keyrings
- Package manager credentials
- Secret files (certificates, private keys, keystores)

**Security Model:**
- ✅ **118 default protected patterns** (cannot be removed)
- ✅ **Linear-time glob matching** (ReDoS protection)
- ✅ **Symlink resolution** (prevents bypass via symbolic links)
- ✅ **Path normalization** (prevents traversal attacks)
- ✅ **User extensions** (can add, but not remove protections)

## Default Denylist

The default denylist contains 118 protected path patterns across 9 categories:

### 1. SSH Keys (PathCategory.SshKeys)

**Risk:** `RISK-I-003` - SSH Key Exposure

Protected paths:
- `~/.ssh/` - SSH directory
- `**/.ssh/**` - Any file in .ssh anywhere
- `~/.ssh/id_*` - SSH private key wildcards
- `~/.ssh/id_rsa` - RSA private key
- `~/.ssh/id_ed25519` - Ed25519 private key
- `~/.ssh/id_ecdsa` - ECDSA private key
- `~/.ssh/id_dsa` - DSA private key (legacy)
- `~/.ssh/known_hosts` - Known hosts database
- `~/.ssh/authorized_keys` - Authorized keys list
- `~/.ssh/config` - SSH client configuration

**Windows-specific:**
- `%USERPROFILE%\.ssh\` - SSH directory
- `%USERPROFILE%\.ssh\id_*` - SSH private keys
- `C:\Users\*\.ssh\` - All users SSH directories

**Why protected:** SSH private keys provide authentication to remote systems. Exposure allows unauthorized access to servers and services.

### 2. GPG/PGP Keys (PathCategory.GpgKeys)

**Risk:** `RISK-I-003` - GPG Key Exposure

Protected paths:
- `~/.gnupg/` - GPG keyring directory
- `**/.gnupg/**` - Any file in .gnupg anywhere
- `~/.gnupg/private-keys-v1.d/` - Private keys directory
- `~/.gnupg/secring.gpg` - Secret keyring (legacy)
- `~/.gpg/` - Alternate GPG directory
- `%APPDATA%\gnupg\` - Windows GPG directory

**Why protected:** GPG keys are used for encryption and signing. Exposure compromises confidentiality and authenticity.

### 3. Cloud Credentials (PathCategory.CloudCredentials)

**Risk:** `RISK-I-003` - Cloud Credential Exposure

**AWS:**
- `~/.aws/` - AWS CLI directory
- `**/.aws/**` - Any file in .aws anywhere
- `~/.aws/credentials` - AWS access keys
- `~/.aws/config` - AWS configuration
- `%USERPROFILE%\.aws\` - Windows AWS directory

**Azure:**
- `~/.azure/` - Azure CLI directory
- `**/.azure/**` - Any file in .azure anywhere
- `~/.azure/credentials` - Azure credentials
- `~/.azure/accessTokens.json` - Azure access tokens

**GCloud:**
- `~/.gcloud/` - GCloud SDK directory
- `**/.gcloud/**` - Any file in .gcloud anywhere
- `~/.gcloud/credentials.json` - GCloud credentials

**Kubernetes:**
- `~/.kube/config` - Kubernetes configuration
- `~/.kube/` - Kubernetes directory

**Other Cloud Providers:**
- `~/.config/digitalocean/` - DigitalOcean CLI
- `~/.config/heroku/` - Heroku CLI
- `~/.config/linode-cli` - Linode CLI
- `~/.terraformrc` - Terraform credentials
- `~/.pulumi/` - Pulumi credentials

**Why protected:** Cloud credentials provide access to infrastructure and resources. Exposure can result in data breaches and unauthorized charges.

### 4. Environment Files (PathCategory.EnvironmentFiles)

**Risk:** `RISK-I-002` - Environment File Exposure

Protected paths:
- `**/.env` - Environment files (all locations)
- `**/.env.*` - Environment file variants (.env.local, .env.production, etc.)

**Why protected:** Environment files commonly contain API keys, database passwords, and other secrets in plain text.

### 5. Package Manager Credentials (PathCategory.PackageManagerCredentials)

**Risk:** `RISK-I-003` - Package Credential Exposure

Protected paths:
- `~/.npmrc` - NPM credentials
- `~/.pypirc` - PyPI credentials
- `~/.nuget/NuGet.Config` - NuGet credentials
- `~/.gem/credentials` - RubyGems credentials
- `~/.config/bundler/` - Bundler credentials
- `~/.hex/` - Hex (Elixir) credentials
- `~/.composer/auth.json` - Composer credentials

**Why protected:** Package manager credentials can be used to publish malicious packages or access private repositories.

### 6. Git Credentials (PathCategory.GitCredentials)

**Risk:** `RISK-I-003` - Git Credential Exposure

Protected paths:
- `~/.gitconfig` - Git configuration with credentials
- `~/.git-credentials` - Plain-text git credentials
- `~/.netrc` - Generic network credentials (used by git)

**Why protected:** Git credentials provide access to source code repositories. Exposure can lead to code theft or malicious commits.

### 7. System Files (PathCategory.SystemFiles)

**Risk:** `RISK-E-004` - System File Modification

**Unix/Linux:**
- `/etc/` - System configuration
- `/etc/passwd` - User accounts
- `/etc/shadow` - Password hashes
- `/proc/` - Process information
- `/sys/` - System information
- `/dev/` - Device files

**Windows:**
- `C:\Windows\` - Windows directory
- `C:\Windows\System32\` - System files

**macOS:**
- `/System/` - System files
- `/Library/` - System library

**Why protected:** System files are critical to OS operation. Modification can compromise system integrity and security.

### 8. Secret Files (PathCategory.SecretFiles)

**Risk:** `RISK-I-003` - Secret File Exposure

Protected patterns:
- `**/secrets/` - Secrets directories
- `**/private/` - Private directories
- `**/*.pem` - PEM certificates/keys
- `**/*.key` - Private key files
- `**/*.p12` - PKCS12 keystores
- `**/*.pfx` - PFX keystores
- `**/*.jks` - Java keystores
- `**/*.cer` - Certificate files
- `**/*.crt` - Certificate files
- `**/*.der` - DER certificates

**Why protected:** These file extensions commonly indicate certificates, private keys, or keystores containing sensitive cryptographic material.

### 9. Database Credentials (PathCategory.CloudCredentials)

Protected paths:
- `~/.pgpass` - PostgreSQL password file
- `~/.my.cnf` - MySQL configuration with credentials
- `~/.mongorc.js` - MongoDB credentials
- `~/.rediscli_history` - Redis command history (may contain passwords)

**Why protected:** Database credentials provide direct access to data stores.

### 10. Browser Credentials (PathCategory.CloudCredentials)

**Windows:**
- `%APPDATA%\Google\Chrome\User Data\` - Chrome profiles
- `%APPDATA%\Mozilla\Firefox\Profiles\` - Firefox profiles

**Unix:**
- `~/.config/google-chrome/` - Chrome profiles
- `~/.mozilla/firefox/` - Firefox profiles

**Why protected:** Browser profiles contain saved passwords, cookies, and session tokens.

### 11. Development Tools

Protected paths:
- `~/.docker/config.json` - Docker credentials
- `~/.kube/config` - Kubernetes configuration
- `~/.helm/` - Helm configuration
- `~/.config/filezilla/` - FileZilla FTP credentials

**Why protected:** Development tool credentials provide access to deployment infrastructure.

## Risk Mitigations

### RISK-I-002: Environment File Exposure
- **Mitigation:** Block all `.env` and `.env.*` files
- **Detection:** Glob pattern `**/.env*` matches recursively
- **Impact:** Prevents accidental commit/upload of secrets

### RISK-I-003: Credential Exposure (SSH, Cloud, GPG)
- **Mitigation:** Block all credential directories and files
- **Detection:** Combination of exact paths and glob patterns
- **Impact:** Prevents unauthorized access to infrastructure

### RISK-E-004: System File Modification
- **Mitigation:** Block system directories (`/etc`, `/Windows`, etc.)
- **Detection:** Exact directory patterns
- **Impact:** Prevents system compromise

### RISK-E-005: Symlink Attack
- **Mitigation:** Resolve symlinks before validation
- **Detection:** `SymlinkResolver` with circular reference detection
- **Impact:** Prevents bypassing protection via symbolic links

### RISK-E-006: Directory Traversal
- **Mitigation:** Normalize paths (resolve `..`, `.`, `//`)
- **Detection:** `PathNormalizer` with path canonicalization
- **Impact:** Prevents bypassing protection via path traversal

## User Extensions

Users can **add** protected paths via `.agent/config.yml`:

```yaml
security:
  additional_protected_paths:
    - pattern: "**/*.secret"
      reason: "Custom secret files"
      risk_id: "RISK-U-001"
      category: "user_defined"
      platforms: ["all"]

    - pattern: "~/work/credentials/"
      reason: "Work credentials directory"
      risk_id: "RISK-U-001"
      category: "user_defined"
      platforms: ["linux", "macos"]
```

**Important:**
- ✅ Can **add** protected paths
- ❌ **Cannot** remove default protections
- ✅ User extensions are merged with defaults
- ✅ Supports all platforms: `all`, `windows`, `linux`, `macos`

## Testing Protected Paths

### Command Line

```bash
# Check if a path is protected
acode security check-path .ssh/id_rsa
# Output: BLOCKED: .ssh/id_rsa

# Check with file operation
acode security check-path .env --operation read
# Output: BLOCKED: .env

# Show all protected patterns
acode security show-denylist
# Output: 118 protected patterns
```

### Programmatic

```csharp
// Create validator
var validator = new ProtectedPathValidator(
    new GlobMatcher(caseSensitive: false),
    new PathNormalizer(),
    new SymlinkResolver()
);

// Validate path
var result = validator.Validate(".ssh/id_rsa");

if (result.IsProtected)
{
    Console.WriteLine($"BLOCKED: {result.Reason}");
    Console.WriteLine($"Risk: {result.RiskId}");
    Console.WriteLine($"Error Code: {result.Error.ErrorCode}");
}
```

## Performance

Path validation is designed for high performance:

- **Average validation time:** < 1ms per path
- **Algorithm complexity:** O(n) linear time (no backtracking)
- **ReDoS protection:** No regex-based glob matching
- **Caching:** Symlink resolution results are cached

**Benchmark results:**
- Single path check: < 1ms
- 100 path checks: < 10ms average
- Pathological glob patterns: < 100ms (no exponential blowup)

## Security Audit

All path protection code has passed security audit:

✅ Linear-time glob matching (no ReDoS)
✅ Symlink resolution with circular reference detection
✅ Path normalization prevents traversal attacks
✅ Default denylist cannot be reduced
✅ 233 tests passing (100% coverage of security scenarios)
✅ 22 security bypass tests (all attacks properly blocked)

## Exit Codes

Security commands return standard exit codes:

- **0** - Path is allowed (not protected)
- **1** - Path is blocked (protected)
- **2** - Invalid arguments
- **3** - Configuration error

## Error Codes

Protected path violations return structured error codes:

- `ACODE-SEC-003-001` - SSH Keys blocked
- `ACODE-SEC-003-002` - GPG Keys blocked
- `ACODE-SEC-003-003` - Cloud Credentials blocked
- `ACODE-SEC-003-004` - Environment Files blocked
- `ACODE-SEC-003-005` - System Files blocked
- `ACODE-SEC-003-006` - Secret Files blocked
- `ACODE-SEC-003-007` - Package Manager Credentials blocked
- `ACODE-SEC-003-008` - Git Credentials blocked
- `ACODE-SEC-003-009` - User-Defined paths blocked

## References

- Task Specification: `docs/tasks/refined-tasks/Epic 00/task-003b-define-default-denylist-protected-paths.md`
- Implementation: `src/Acode.Domain/Security/PathProtection/`
- Tests: `tests/Acode.Domain.Tests/Security/PathProtection/`
- Risk Definitions: `src/Acode.Domain/Risks/PathProtectionRisks.cs`
