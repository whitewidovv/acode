# Task 018.b: Working Dir/Env Enforcement

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 4 – Execution Layer  
**Dependencies:** Task 018 (Structured Command Runner), Task 050 (Workspace Database)  

---

## Description

### Overview

Task 018.b implements working directory and environment variable enforcement for Agentic Coding Bot's command execution system. Every command executes within a specific directory context and environment configuration, and this subtask ensures both are correctly established, validated, and secured before any command runs.

Correct working directory is essential—relative paths, file I/O, and many tools depend on the current directory being set correctly. Environment variables configure runtime behavior, control tool paths, and pass configuration to commands. Both must be handled with security in mind to prevent injection attacks and credential exposure.

### Business Value

Proper working directory and environment enforcement provides critical value:

1. **Command Reliability** — Commands execute in the expected directory with correct environment, preventing path-related failures
2. **Security Protection** — Validation prevents path traversal attacks and environment variable injection
3. **Credential Safety** — Sensitive variables (API keys, tokens) are passed to processes but redacted from logs
4. **Cross-Platform Support** — Path normalization ensures consistent behavior across Windows, Linux, and macOS
5. **Debugging Support** — Audit logs capture execution context for troubleshooting while protecting secrets

### Scope

This subtask delivers:

1. **Working Directory Resolution** — Resolve and validate the execution directory from absolute, relative, or default paths
2. **Path Validation** — Prevent path traversal, validate existence, enforce boundaries
3. **Environment Modes** — Support inherit, replace, and merge modes for environment variable handling
4. **Variable Validation** — Validate variable names and values to prevent injection
5. **Sensitive Detection** — Identify sensitive variables by pattern and redact in logs
6. **PATH Management** — Special handling for PATH variable with prepend/append support
7. **Audit Recording** — Log execution context with sensitive values redacted

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Task 018 Command Runner | Parent | Receives resolved context before execution |
| Task 018.a Output Capture | Sibling | Context set before capture begins |
| Task 018.c Artifact Logging | Sibling | Context included in artifact metadata |
| Task 002 Agent Config | Upstream | Reads environment configuration |
| Task 014 RepoFS | Upstream | Provides repo root for default working directory |
| Audit System | Downstream | Records redacted context |

### Failure Modes

| Failure | Detection | Impact | Recovery |
|---------|-----------|--------|----------|
| Directory does not exist | DirectoryNotFoundException | Command cannot start | Return clear error with path |
| Path traversal attempt | Validation rejection | Potential security bypass | Block and log attempt |
| Invalid variable name | Validation rejection | Variable not set | Return error with valid patterns |
| Environment too large | Size limit exceeded | Process start fails | Trim or reject with message |
| Sensitive pattern miss | Credential in logs | Security exposure | Improve patterns, rotate credential |

### Assumptions

1. Commands execute as child processes with configurable environment
2. .NET ProcessStartInfo supports setting working directory and environment
3. Parent process environment is accessible via Environment.GetEnvironmentVariables()
4. Case sensitivity of environment variables follows platform conventions
5. Maximum environment block size is platform-limited (~32KB on Windows)

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Working Directory | Command execution path |
| CWD | Current working directory |
| Environment Variable | Named configuration value |
| PATH | Executable search path |
| Inherit | Pass parent environment |
| Merge | Combine environments |
| Replace | Use only specified vars |
| Redaction | Hide sensitive values |
| Normalization | Path standardization |
| Injection | Malicious input attack |
| Sensitive | Security-critical value |
| Variable Name | Environment key |
| Variable Value | Environment data |

---

## Out of Scope

The following items are explicitly excluded from Task 018.b:

- **Output capture** - See Task 018.a
- **Artifact logging** - See Task 018.c
- **Shell profile loading** - No shell profiles
- **User switching** - Same user only
- **Container environment** - See Task 020
- **Dynamic environment** - Static at command start
- **File-based env loading** - Explicit variables only

---

## Functional Requirements

### Working Directory (FR-018B-01 through FR-018B-15)

| ID | Requirement |
|----|-------------|
| FR-018B-01 | System MUST set process working directory before execution |
| FR-018B-02 | System MUST validate working directory exists |
| FR-018B-03 | System MUST validate path is a directory (not a file) |
| FR-018B-04 | System MUST normalize path separators for platform |
| FR-018B-05 | System MUST resolve relative paths from repo root |
| FR-018B-06 | System MUST handle UNC paths on Windows |
| FR-018B-07 | System MUST resolve symlinks to actual path |
| FR-018B-08 | System MUST default to repo root if not specified |
| FR-018B-09 | System MUST support ${repo_root} variable in paths |
| FR-018B-10 | System MUST support ${workspace} variable in paths |
| FR-018B-11 | System MUST handle paths with spaces |
| FR-018B-12 | System MUST handle Unicode characters in paths |
| FR-018B-13 | System MUST report resolved path in result |
| FR-018B-14 | System MUST reject empty path |
| FR-018B-15 | System MUST handle drive-relative paths on Windows |

### Path Validation (FR-018B-16 through FR-018B-28)

| ID | Requirement |
|----|-------------|
| FR-018B-16 | System MUST reject path traversal attempts (../) |
| FR-018B-17 | System MUST reject null bytes in path |
| FR-018B-18 | System MUST validate path length (< 260 on Windows default) |
| FR-018B-19 | System MUST validate path contains no invalid characters |
| FR-018B-20 | System MUST handle leading/trailing whitespace |
| FR-018B-21 | System MUST reject paths outside repo by default |
| FR-018B-22 | System MUST support allow_external option to permit outside paths |
| FR-018B-23 | System MUST reject device paths (CON, NUL, etc. on Windows) |
| FR-018B-24 | System MUST validate read permission on directory |
| FR-018B-25 | System MUST log path validation failures |
| FR-018B-26 | System MUST provide clear error for validation failure |
| FR-018B-27 | System MUST reject double-encoded path traversal |
| FR-018B-28 | System MUST normalize before validation (no bypass) |

### Environment Mode (FR-018B-29 through FR-018B-40)

| ID | Requirement |
|----|-------------|
| FR-018B-29 | System MUST support inherit mode (start with parent env) |
| FR-018B-30 | System MUST support replace mode (only specified vars) |
| FR-018B-31 | System MUST support merge mode (parent + specified, specified wins) |
| FR-018B-32 | System MUST default to inherit mode |
| FR-018B-33 | System MUST apply default variables from config |
| FR-018B-34 | System MUST allow per-command mode override |
| FR-018B-35 | System MUST handle null value as variable removal in merge |
| FR-018B-36 | System MUST preserve unspecified parent variables in merge |
| FR-018B-37 | System MUST handle empty string value (different from removal) |
| FR-018B-38 | System MUST support variable expansion in values |
| FR-018B-39 | System MUST handle circular variable expansion |
| FR-018B-40 | System MUST limit expansion depth to prevent infinite loops |

### Variable Validation (FR-018B-41 through FR-018B-52)

| ID | Requirement |
|----|-------------|
| FR-018B-41 | System MUST validate variable names match [A-Za-z_][A-Za-z0-9_]* |
| FR-018B-42 | System MUST reject names starting with digit |
| FR-018B-43 | System MUST reject names with special characters |
| FR-018B-44 | System MUST handle case sensitivity per platform |
| FR-018B-45 | System MUST limit variable name length |
| FR-018B-46 | System MUST limit variable value length |
| FR-018B-47 | System MUST reject shell injection patterns in values |
| FR-018B-48 | System MUST reject newlines in variable names |
| FR-018B-49 | System MUST handle newlines in variable values |
| FR-018B-50 | System MUST log validation failures |
| FR-018B-51 | System MUST provide clear error for invalid variable |
| FR-018B-52 | System MUST calculate total environment size |

### Sensitive Variable Handling (FR-018B-53 through FR-018B-65)

| ID | Requirement |
|----|-------------|
| FR-018B-53 | System MUST define configurable sensitive name patterns |
| FR-018B-54 | System MUST detect sensitive variables by pattern match |
| FR-018B-55 | System MUST default patterns for common sensitive names |
| FR-018B-56 | System MUST redact sensitive values in all logs |
| FR-018B-57 | System MUST redact sensitive values in error messages |
| FR-018B-58 | System MUST pass actual values to child process |
| FR-018B-59 | System MUST redact in audit records |
| FR-018B-60 | System MUST support exact match patterns |
| FR-018B-61 | System MUST support wildcard patterns (*_KEY, *_SECRET) |
| FR-018B-62 | System MUST support regex patterns |
| FR-018B-63 | System MUST mask with consistent placeholder (<REDACTED>) |
| FR-018B-64 | System MUST detect sensitive in both name and value |
| FR-018B-65 | System MUST log that redaction occurred (not value) |

### PATH Variable Handling (FR-018B-66 through FR-018B-75)

| ID | Requirement |
|----|-------------|
| FR-018B-66 | System MUST inherit PATH by default |
| FR-018B-67 | System MUST support prepending paths to PATH |
| FR-018B-68 | System MUST support appending paths to PATH |
| FR-018B-69 | System MUST use platform-correct separator (; or :) |
| FR-018B-70 | System MUST remove duplicate paths |
| FR-018B-71 | System MUST normalize path separators in PATH entries |
| FR-018B-72 | System MUST validate PATH entries exist (optional) |
| FR-018B-73 | System MUST preserve PATH order |
| FR-018B-74 | System MUST handle empty PATH entries |
| FR-018B-75 | System MUST handle PATH not in parent environment |

### Audit Recording (FR-018B-76 through FR-018B-85)

| ID | Requirement |
|----|-------------|
| FR-018B-76 | System MUST record resolved working directory |
| FR-018B-77 | System MUST record environment mode used |
| FR-018B-78 | System MUST record environment variable names |
| FR-018B-79 | System MUST record non-sensitive values |
| FR-018B-80 | System MUST redact sensitive values in record |
| FR-018B-81 | System MUST record sensitive variable count |
| FR-018B-82 | System MUST record PATH modifications |
| FR-018B-83 | System MUST include correlation IDs |
| FR-018B-84 | System MUST persist to workspace database |
| FR-018B-85 | System MUST emit structured log event |

---

## Non-Functional Requirements

### Performance (NFR-018B-01 through NFR-018B-10)

| ID | Requirement |
|----|-------------|
| NFR-018B-01 | Path validation MUST complete in under 1ms |
| NFR-018B-02 | Path normalization MUST complete in under 0.5ms |
| NFR-018B-03 | Environment merge MUST complete in under 5ms for 100 vars |
| NFR-018B-04 | Sensitive pattern matching MUST complete in under 1ms |
| NFR-018B-05 | No unnecessary memory allocation during merge |
| NFR-018B-06 | Environment building MUST be lazy (only when needed) |
| NFR-018B-07 | Pattern compilation MUST be cached |
| NFR-018B-08 | Directory existence check MUST use caching where safe |
| NFR-018B-09 | Total context resolution MUST complete in under 10ms |
| NFR-018B-10 | No file I/O during validation except existence check |

### Security (NFR-018B-11 through NFR-018B-22)

| ID | Requirement |
|----|-------------|
| NFR-018B-11 | Path traversal attacks MUST be impossible |
| NFR-018B-12 | Sensitive values MUST NOT appear in any log |
| NFR-018B-13 | Sensitive values MUST NOT appear in error messages |
| NFR-018B-14 | Sensitive values MUST NOT appear in telemetry |
| NFR-018B-15 | Command injection via env vars MUST be blocked |
| NFR-018B-16 | Path validation MUST occur before any file access |
| NFR-018B-17 | Redaction MUST use constant placeholder (no partial reveal) |
| NFR-018B-18 | Sensitive detection MUST be case-insensitive |
| NFR-018B-19 | All security failures MUST be logged (without secret) |
| NFR-018B-20 | Security log events MUST include correlation ID |
| NFR-018B-21 | Double-encoding bypass MUST be prevented |
| NFR-018B-22 | Null byte injection MUST be prevented |

### Reliability (NFR-018B-23 through NFR-018B-30)

| ID | Requirement |
|----|-------------|
| NFR-018B-23 | Validation errors MUST provide actionable messages |
| NFR-018B-24 | Missing directory MUST be clearly reported |
| NFR-018B-25 | Invalid variable MUST identify which variable |
| NFR-018B-26 | Environment build MUST be deterministic |
| NFR-018B-27 | Cross-platform behavior MUST be consistent (where possible) |
| NFR-018B-28 | Partial failure MUST not leave invalid state |
| NFR-018B-29 | Config parse failure MUST use safe defaults |
| NFR-018B-30 | All code paths MUST be unit tested |

### Maintainability (NFR-018B-31 through NFR-018B-38)

| ID | Requirement |
|----|-------------|
| NFR-018B-31 | All classes MUST have interfaces for mocking |
| NFR-018B-32 | Configuration MUST be injectable |
| NFR-018B-33 | Logging MUST use ILogger abstraction |
| NFR-018B-34 | Unit test coverage MUST exceed 95% |
| NFR-018B-35 | Sensitive patterns MUST be configurable without code change |
| NFR-018B-36 | Platform-specific code MUST be isolated |
| NFR-018B-37 | All public methods MUST have XML documentation |
| NFR-018B-38 | Security decisions MUST have inline comments explaining rationale |

---

## User Manual Documentation

### Overview

Working directory and environment enforcement ensures commands execute in the correct context. Security protections prevent injection attacks.

### Configuration

```yaml
# .agent/config.yml
execution:
  working_directory:
    # Default to repo root
    default: "${repo_root}"
    
    # Allow paths outside repo
    allow_external: false
    
  environment:
    # inherit, replace, merge
    mode: inherit
    
    # Variables to always include
    defaults:
      DOTNET_CLI_TELEMETRY_OPTOUT: "1"
      
    # Sensitive variable patterns
    sensitive_patterns:
      - "*_KEY"
      - "*_SECRET"
      - "*_TOKEN"
      - "*_PASSWORD"
      - "*_CREDENTIAL*"
      
    # PATH modifications
    path:
      prepend: []
      append: []
```

### Environment Modes

| Mode | Description |
|------|-------------|
| inherit | Start with parent environment |
| replace | Only specified variables |
| merge | Parent + specified (specified wins) |

### Sensitive Variable Redaction

```
# Original log entry (NOT stored)
Environment: API_KEY=sk-abc123def456

# Redacted log entry (stored)
Environment: API_KEY=<REDACTED>
```

### Variable Name Validation

| Character | Allowed |
|-----------|---------|
| A-Z | ✅ |
| a-z | ✅ |
| 0-9 | ✅ (not first) |
| _ | ✅ |
| Others | ❌ |

### CLI Examples

```bash
# Execute in specific directory
acode exec "npm install" --cwd ./frontend

# Execute with environment variable
acode exec "node app.js" --env "NODE_ENV=production"

# Execute with multiple variables
acode exec "python script.py" \
  --env "DEBUG=true" \
  --env "LOG_LEVEL=info"

# Execute with environment merge
acode exec "dotnet build" --env-mode merge
```

### Troubleshooting

#### Directory Not Found

**Problem:** Working directory doesn't exist

**Solutions:**
1. Verify path is correct
2. Create directory first
3. Check relative path resolution

#### Path Traversal Rejected

**Problem:** Path contains ../

**Solutions:**
1. Use absolute paths
2. Use paths within repo root
3. Enable allow_external if needed

#### Environment Not Set

**Problem:** Variable not visible to command

**Solutions:**
1. Check environment mode
2. Verify variable name is valid
3. Check for typos

---

## Acceptance Criteria

### Working Directory (AC-018B-01 to AC-018B-15)

- [ ] AC-018B-01: Working directory is set on process before start
- [ ] AC-018B-02: Relative path resolved from repo root
- [ ] AC-018B-03: Absolute path used as-is (after validation)
- [ ] AC-018B-04: Missing directory returns clear error
- [ ] AC-018B-05: File path (not directory) returns clear error
- [ ] AC-018B-06: Path with spaces works correctly
- [ ] AC-018B-07: Path with Unicode characters works correctly
- [ ] AC-018B-08: ${repo_root} variable expanded correctly
- [ ] AC-018B-09: Default to repo root when not specified
- [ ] AC-018B-10: Resolved path included in result
- [ ] AC-018B-11: UNC paths work on Windows
- [ ] AC-018B-12: Long paths (> 260) handled on Windows with long path support
- [ ] AC-018B-13: Symlinks resolved to actual path
- [ ] AC-018B-14: Drive-relative paths work on Windows
- [ ] AC-018B-15: Empty path rejected with error

### Path Validation (AC-018B-16 to AC-018B-25)

- [ ] AC-018B-16: Path traversal (../) rejected
- [ ] AC-018B-17: Encoded path traversal (%2e%2e/) rejected
- [ ] AC-018B-18: Null bytes in path rejected
- [ ] AC-018B-19: Invalid characters rejected
- [ ] AC-018B-20: Path outside repo rejected by default
- [ ] AC-018B-21: Path outside repo allowed with allow_external=true
- [ ] AC-018B-22: Device names (CON, NUL) rejected on Windows
- [ ] AC-018B-23: Path length validated
- [ ] AC-018B-24: Whitespace trimmed from path
- [ ] AC-018B-25: Validation failure logged with details

### Environment Mode (AC-018B-26 to AC-018B-35)

- [ ] AC-018B-26: Inherit mode includes all parent variables
- [ ] AC-018B-27: Inherit mode applies specified overrides
- [ ] AC-018B-28: Replace mode uses only specified variables
- [ ] AC-018B-29: Replace mode does not include parent variables
- [ ] AC-018B-30: Merge mode starts with parent variables
- [ ] AC-018B-31: Merge mode overrides with specified values
- [ ] AC-018B-32: Merge mode removes vars set to null
- [ ] AC-018B-33: Default mode is inherit
- [ ] AC-018B-34: Mode configurable per command via CLI
- [ ] AC-018B-35: Default variables from config applied

### Variable Validation (AC-018B-36 to AC-018B-45)

- [ ] AC-018B-36: Valid name [A-Za-z_][A-Za-z0-9_]* accepted
- [ ] AC-018B-37: Name starting with digit rejected
- [ ] AC-018B-38: Name with special chars rejected
- [ ] AC-018B-39: Empty name rejected
- [ ] AC-018B-40: Very long name rejected (> 1024 chars)
- [ ] AC-018B-41: Very long value rejected (> 32KB)
- [ ] AC-018B-42: Empty value allowed (different from null)
- [ ] AC-018B-43: Value with newlines handled correctly
- [ ] AC-018B-44: Shell injection patterns rejected in value
- [ ] AC-018B-45: Case sensitivity follows platform convention

### Sensitive Handling (AC-018B-46 to AC-018B-58)

- [ ] AC-018B-46: Default patterns detect *_KEY
- [ ] AC-018B-47: Default patterns detect *_SECRET
- [ ] AC-018B-48: Default patterns detect *_TOKEN
- [ ] AC-018B-49: Default patterns detect *_PASSWORD
- [ ] AC-018B-50: Custom patterns from config work
- [ ] AC-018B-51: Sensitive values redacted in logs
- [ ] AC-018B-52: Sensitive values redacted in error messages
- [ ] AC-018B-53: Sensitive values passed to process correctly
- [ ] AC-018B-54: Redaction uses <REDACTED> placeholder
- [ ] AC-018B-55: Pattern matching is case-insensitive
- [ ] AC-018B-56: Wildcard patterns work
- [ ] AC-018B-57: Regex patterns work
- [ ] AC-018B-58: Audit records show redacted values

### PATH Handling (AC-018B-59 to AC-018B-68)

- [ ] AC-018B-59: PATH inherited by default
- [ ] AC-018B-60: Prepend adds to front of PATH
- [ ] AC-018B-61: Append adds to end of PATH
- [ ] AC-018B-62: Correct separator used (; on Windows, : on Unix)
- [ ] AC-018B-63: Duplicate paths removed
- [ ] AC-018B-64: Path order preserved
- [ ] AC-018B-65: PATH entry paths normalized
- [ ] AC-018B-66: Empty PATH entries removed
- [ ] AC-018B-67: Missing parent PATH handled gracefully
- [ ] AC-018B-68: PATH modifications logged

### Audit (AC-018B-69 to AC-018B-75)

- [ ] AC-018B-69: Working directory recorded in audit
- [ ] AC-018B-70: Environment mode recorded
- [ ] AC-018B-71: Variable names recorded
- [ ] AC-018B-72: Non-sensitive values recorded
- [ ] AC-018B-73: Sensitive values redacted in record
- [ ] AC-018B-74: Correlation IDs included
- [ ] AC-018B-75: Audit persisted to database

---

## Testing Requirements

### Unit Tests

#### WorkingDirectoryResolverTests
- Resolve_RelativePath_ResolvesFromRepoRoot
- Resolve_AbsolutePath_UsesAsIs
- Resolve_NullPath_ReturnsRepoRoot
- Resolve_EmptyPath_ThrowsArgumentException
- Resolve_PathWithSpaces_WorksCorrectly
- Resolve_PathWithUnicode_WorksCorrectly
- Resolve_RepoRootVariable_ExpandsCorrectly
- Resolve_WorkspaceVariable_ExpandsCorrectly
- Resolve_NonExistentPath_ThrowsDirectoryNotFoundException
- Resolve_FilePath_ThrowsInvalidOperationException
- Resolve_Symlink_ResolvesToActualPath
- Resolve_UncPath_WorksOnWindows
- Resolve_DriveRelativePath_WorksOnWindows

#### PathValidatorTests
- Validate_ValidPath_ReturnsTrue
- Validate_PathTraversal_ThrowsSecurityException
- Validate_EncodedTraversal_ThrowsSecurityException
- Validate_NullByte_ThrowsSecurityException
- Validate_InvalidCharacters_ThrowsArgumentException
- Validate_PathOutsideRepo_ThrowsWhenNotAllowed
- Validate_PathOutsideRepo_AllowsWhenConfigured
- Validate_DeviceName_ThrowsOnWindows
- Validate_LongPath_HandlesCorrectly
- Validate_WhitespaceOnly_ThrowsArgumentException
- Validate_LeadingWhitespace_Trims
- Validate_TrailingWhitespace_Trims

#### EnvironmentBuilderTests
- Build_InheritMode_IncludesParentVariables
- Build_InheritMode_AppliesOverrides
- Build_ReplaceMode_OnlySpecifiedVariables
- Build_ReplaceMode_ExcludesParent
- Build_MergeMode_CombinesParentAndSpecified
- Build_MergeMode_SpecifiedWins
- Build_MergeMode_NullRemovesVariable
- Build_EmptyValue_DifferentFromNull
- Build_AppliesDefaults_FromConfig
- Build_VariableExpansion_Works
- Build_CircularExpansion_LimitsDepth
- Build_NullSpecified_ReturnsParentOnly

#### VariableValidatorTests
- Validate_ValidName_ReturnsTrue
- Validate_NameStartsWithDigit_ReturnsFalse
- Validate_NameWithSpecialChars_ReturnsFalse
- Validate_EmptyName_ReturnsFalse
- Validate_VeryLongName_ReturnsFalse
- Validate_VeryLongValue_ReturnsFalse
- Validate_EmptyValue_ReturnsTrue
- Validate_ValueWithNewlines_ReturnsTrue
- Validate_ShellInjection_ReturnsFalse
- Validate_CaseSensitivity_FollowsPlatform

#### SensitiveRedactorTests
- IsSensitive_MatchesKeyPattern_ReturnsTrue
- IsSensitive_MatchesSecretPattern_ReturnsTrue
- IsSensitive_MatchesTokenPattern_ReturnsTrue
- IsSensitive_MatchesPasswordPattern_ReturnsTrue
- IsSensitive_NoMatch_ReturnsFalse
- IsSensitive_CaseInsensitive_Matches
- IsSensitive_CustomPattern_Works
- IsSensitive_RegexPattern_Works
- Redact_SensitiveValue_ReturnsPlaceholder
- Redact_NonSensitiveValue_ReturnsOriginal
- Redact_EmptyValue_ReturnsEmpty
- RedactEnvironment_MixedSensitivity_CorrectlyRedacts

#### PathManagerTests
- PrependPath_AddsToFront
- AppendPath_AddsToEnd
- RemoveDuplicates_KeepsFirst
- UsesCorrectSeparator_Windows
- UsesCorrectSeparator_Unix
- NormalizesPathSeparators_InEntries
- RemovesEmptyEntries
- HandlesMissingParentPath

### Integration Tests

#### EnvironmentIntegrationTests
- Execute_WithWorkingDirectory_RunsInCorrectDir
- Execute_WithEnvironmentVariable_VariableVisible
- Execute_InheritMode_ParentVariablesVisible
- Execute_MergeMode_BothVariablesVisible
- Execute_ReplaceMode_OnlySpecifiedVisible
- Execute_SensitiveVariable_NotInLogs
- Execute_PathModification_AffectsLookup

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Path validation | 0.5ms | 1ms |
| Path normalization | 0.2ms | 0.5ms |
| Environment merge (100 vars) | 2ms | 5ms |
| Sensitive pattern match (50 patterns) | 0.5ms | 1ms |
| Full context resolution | 5ms | 10ms |

### Coverage Requirements

| Component | Minimum Coverage |
|-----------|-----------------|
| WorkingDirectoryResolver | 95% |
| PathValidator | 98% |
| EnvironmentBuilder | 95% |
| VariableValidator | 98% |
| SensitiveRedactor | 95% |
| PathManager | 90% |

---

## User Verification Steps

### Scenario 1: Working Directory Resolution
```powershell
# Step 1: Create test directory structure
New-Item -ItemType Directory -Path "test-project\src\nested" -Force

# Step 2: Run command with working directory
agentic-coding exec "pwd" --working-dir "test-project\src\nested"

# Expected Output:
# Working directory: C:\Users\...\repo\test-project\src\nested
# [Command output shows the nested directory path]

# Verification: Path resolution works for relative directories
```

### Scenario 2: Path Traversal Prevention
```powershell
# Step 1: Attempt to use path traversal to escape repo
agentic-coding exec "dir" --working-dir "..\..\..\..\Windows\System32"

# Expected Output:
# Error: SEC-018B-01: Path traversal detected in working directory
# Details: Path attempts to navigate outside repository root

# Verification: Security violation is caught and blocked, command does not execute
```

### Scenario 3: Environment Variable Inheritance (Default)
```powershell
# Step 1: Set parent environment variable
$env:PARENT_VAR = "parent_value"

# Step 2: Run with inherit mode (default)
agentic-coding exec "echo %PARENT_VAR%"

# Expected Output:
# parent_value

# Step 3: Verify in logs
agentic-coding run show-logs --last --show-env

# Expected: Logs show PARENT_VAR with value (or redacted if sensitive)

# Verification: Parent variables are inherited correctly
```

### Scenario 4: Environment Variable Replacement Mode
```powershell
# Step 1: Set parent environment variables
$env:PARENT_VAR = "parent_value"
$env:PATH = "C:\Windows\System32"

# Step 2: Run with replace mode
agentic-coding exec "set" --env-mode replace --env "MY_VAR=my_value"

# Expected Output:
# MY_VAR=my_value
# [PARENT_VAR and PATH not present]

# Verification: Replace mode completely replaces parent environment
```

### Scenario 5: Environment Variable Merge Mode
```powershell
# Step 1: Set parent variable
$env:PARENT_VAR = "parent_value"

# Step 2: Run with merge mode and add child variable
agentic-coding exec "echo %PARENT_VAR% %CHILD_VAR%" --env-mode merge --env "CHILD_VAR=child_value"

# Expected Output:
# parent_value child_value

# Step 3: Run with merge mode and override parent
agentic-coding exec "echo %PARENT_VAR%" --env-mode merge --env "PARENT_VAR=overridden"

# Expected Output:
# overridden

# Verification: Merge mode combines parent and specified, with specified winning
```

### Scenario 6: Sensitive Variable Redaction
```powershell
# Step 1: Run with sensitive variable
agentic-coding exec "echo Using API key" --env "MY_API_KEY=sk-super-secret-key-12345"

# Step 2: Check execution logs
agentic-coding run show-logs --last --show-env

# Expected Log Output:
# Environment Variables:
#   MY_API_KEY=<REDACTED>
# [NOT showing sk-super-secret-key-12345]

# Step 3: Verify command received actual value
agentic-coding exec "echo %MY_API_KEY%" --env "MY_API_KEY=sk-super-secret-key-12345"

# Expected Command Output:
# sk-super-secret-key-12345
# [Command gets real value, only logs are redacted]

# Verification: Sensitive values redacted from logs but passed to process
```

### Scenario 7: Variable Name Validation
```powershell
# Step 1: Try invalid variable name (starts with digit)
agentic-coding exec "echo test" --env "123INVALID=value"

# Expected Output:
# Error: VAL-018B-01: Invalid environment variable name '123INVALID'
# Details: Variable names must start with letter or underscore

# Step 2: Try shell injection in value
agentic-coding exec "echo test" --env "VAR=value`; rm -rf /"

# Expected Output:
# Error: SEC-018B-03: Potential shell injection detected in variable value
# Details: Variable 'VAR' contains potentially dangerous characters

# Verification: Variable names and values are validated for security
```

### Scenario 8: PATH Manipulation
```powershell
# Step 1: Create test with custom bin directory
New-Item -ItemType Directory -Path "test-bin" -Force

# Step 2: Test prepend (custom path searched first)
agentic-coding exec "where dotnet" --path-prepend ".\test-bin"

# Expected: Looks in test-bin first

# Step 3: Test append (custom path searched last)
agentic-coding exec "where dotnet" --path-append ".\test-bin"

# Expected: Looks in test-bin after standard PATH

# Step 4: Verify PATH in logs
agentic-coding run show-logs --last --show-env

# Expected: PATH shows test-bin at beginning or end

# Verification: PATH prepend/append works correctly
```

### Scenario 9: Audit Trail Verification
```powershell
# Step 1: Run command with full environment context
agentic-coding exec "echo audit test" `
    --working-dir "src" `
    --env-mode merge `
    --env "APP_ENV=production" `
    --env "DB_PASSWORD=secret123"

# Step 2: Retrieve audit record
agentic-coding run show-logs --last --show-audit

# Expected Audit Output:
# Execution Audit:
#   Correlation ID: abc-123-def
#   Working Directory: C:\...\repo\src
#   Environment Mode: merge
#   Variables Set:
#     APP_ENV=production
#     DB_PASSWORD=<REDACTED>
#   Started: 2024-01-15T10:30:00Z
#   Completed: 2024-01-15T10:30:01Z

# Verification: Complete audit trail maintained with sensitive values redacted
```

### Scenario 10: Config-Based Defaults
```yaml
# Step 1: Create .agent/config.yml with defaults
# .agent/config.yml:
execution:
  working_directory:
    default: "${repo_root}/src"
    allow_external: false
  environment:
    mode: inherit
    defaults:
      DOTNET_CLI_TELEMETRY_OPTOUT: "1"
      NODE_ENV: "development"
    sensitive_patterns:
      - "*_KEY"
      - "*_TOKEN"
```

```powershell
# Step 2: Run command without specifying working dir
agentic-coding exec "pwd"

# Expected Output:
# C:\...\repo\src
# [Uses config default]

# Step 3: Verify default environment variables
agentic-coding exec "echo %DOTNET_CLI_TELEMETRY_OPTOUT%"

# Expected Output:
# 1
# [Config defaults applied]

# Verification: Configuration defaults are applied correctly
```

---

## Implementation Prompt

You are implementing Task-018b: Working Directory & Environment Enforcement for the Agentic Coding Bot. This subtask focuses on secure working directory resolution and comprehensive environment variable management including mode selection, sensitive value redaction, and PATH manipulation.

### File Structure

```
src/
├── AgenticCoder.Domain/
│   └── Execution/
│       └── Environment/
│           ├── IEnvironmentBuilder.cs
│           ├── IPathValidator.cs
│           ├── ISensitiveRedactor.cs
│           ├── IWorkingDirectoryResolver.cs
│           ├── EnvironmentContext.cs
│           ├── EnvironmentMode.cs
│           ├── EnvironmentVariableConfig.cs
│           └── WorkingDirectoryConfig.cs
│
├── AgenticCoder.Application/
│   └── Execution/
│       └── Environment/
│           ├── EnvironmentContextFactory.cs
│           └── IEnvironmentContextFactory.cs
│
├── AgenticCoder.Infrastructure/
│   └── Execution/
│       └── Environment/
│           ├── WorkingDirectoryResolver.cs
│           ├── EnvironmentBuilder.cs
│           ├── SensitiveRedactor.cs
│           ├── PathValidator.cs
│           ├── PathManager.cs
│           ├── VariableValidator.cs
│           └── DependencyInjection/
│               └── EnvironmentServiceExtensions.cs
│
└── tests/
    ├── AgenticCoder.Domain.Tests/
    │   └── Execution/
    │       └── Environment/
    │           └── EnvironmentContextTests.cs
    │
    └── AgenticCoder.Infrastructure.Tests/
        └── Execution/
            └── Environment/
                ├── WorkingDirectoryResolverTests.cs
                ├── EnvironmentBuilderTests.cs
                ├── SensitiveRedactorTests.cs
                ├── PathValidatorTests.cs
                ├── PathManagerTests.cs
                └── VariableValidatorTests.cs
```

### Domain Models

```csharp
namespace AgenticCoder.Domain.Execution.Environment;

/// <summary>
/// Environment variable handling modes
/// </summary>
public enum EnvironmentMode
{
    /// <summary>Start with parent environment, apply overrides</summary>
    Inherit,
    
    /// <summary>Only use explicitly specified variables</summary>
    Replace,
    
    /// <summary>Combine parent and specified, specified wins</summary>
    Merge
}

/// <summary>
/// Working directory configuration
/// </summary>
public sealed record WorkingDirectoryConfig
{
    public string? Default { get; init; }
    public bool AllowExternal { get; init; } = false;
    public int MaxPathLength { get; init; } = 4096;
}

/// <summary>
/// Environment variable configuration
/// </summary>
public sealed record EnvironmentVariableConfig
{
    public EnvironmentMode DefaultMode { get; init; } = EnvironmentMode.Inherit;
    public IReadOnlyDictionary<string, string> Defaults { get; init; } 
        = new Dictionary<string, string>();
    public IReadOnlyList<string> SensitivePatterns { get; init; } 
        = new[] { "*_KEY", "*_SECRET", "*_TOKEN", "*_PASSWORD", "*_CREDENTIAL*" };
    public IReadOnlyList<string> PathPrepend { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> PathAppend { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Complete execution environment context
/// </summary>
public sealed record EnvironmentContext
{
    public required string WorkingDirectory { get; init; }
    public required EnvironmentMode Mode { get; init; }
    public required IReadOnlyDictionary<string, string> Variables { get; init; }
    public required IReadOnlyDictionary<string, string> RedactedVariables { get; init; }
    public DateTimeOffset ResolvedAt { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Variables with sensitive values redacted for logging
    /// </summary>
    public IReadOnlyDictionary<string, string> GetAuditSafeVariables() => RedactedVariables;
}
```

### Core Interfaces

```csharp
namespace AgenticCoder.Domain.Execution.Environment;

public interface IWorkingDirectoryResolver
{
    /// <summary>
    /// Resolves and validates working directory path
    /// </summary>
    /// <param name="requestedPath">User-requested path (relative or absolute)</param>
    /// <param name="repoRoot">Repository root for relative path resolution</param>
    /// <param name="config">Working directory configuration</param>
    /// <returns>Validated absolute path</returns>
    /// <exception cref="DirectoryNotFoundException">If directory doesn't exist</exception>
    /// <exception cref="SecurityException">If path validation fails</exception>
    string Resolve(string? requestedPath, string repoRoot, WorkingDirectoryConfig config);
}

public interface IPathValidator
{
    /// <summary>
    /// Validates a path for security issues
    /// </summary>
    /// <param name="path">Path to validate</param>
    /// <param name="repoRoot">Repository root for boundary checks</param>
    /// <param name="allowExternal">Whether paths outside repo are allowed</param>
    /// <exception cref="SecurityException">If path contains security violations</exception>
    void Validate(string path, string repoRoot, bool allowExternal);
    
    /// <summary>
    /// Checks if path contains traversal sequences
    /// </summary>
    bool ContainsTraversal(string path);
    
    /// <summary>
    /// Checks if path is within repository boundaries
    /// </summary>
    bool IsWithinRepo(string path, string repoRoot);
}

public interface IEnvironmentBuilder
{
    /// <summary>
    /// Builds environment variable dictionary based on mode
    /// </summary>
    /// <param name="mode">Environment handling mode</param>
    /// <param name="specified">Explicitly specified variables</param>
    /// <param name="defaults">Default variables from config</param>
    /// <param name="pathModifications">PATH prepend/append entries</param>
    /// <returns>Complete environment dictionary</returns>
    IReadOnlyDictionary<string, string> Build(
        EnvironmentMode mode,
        IReadOnlyDictionary<string, string>? specified,
        IReadOnlyDictionary<string, string>? defaults,
        (IEnumerable<string> Prepend, IEnumerable<string> Append)? pathModifications);
}

public interface ISensitiveRedactor
{
    /// <summary>
    /// Determines if a variable name matches sensitive patterns
    /// </summary>
    bool IsSensitive(string variableName, IEnumerable<string> patterns);
    
    /// <summary>
    /// Redacts sensitive values from environment dictionary
    /// </summary>
    IReadOnlyDictionary<string, string> RedactSensitiveValues(
        IReadOnlyDictionary<string, string> variables,
        IEnumerable<string> patterns);
    
    /// <summary>
    /// Redact placeholder
    /// </summary>
    string RedactedPlaceholder => "<REDACTED>";
}

public interface IVariableValidator
{
    /// <summary>
    /// Validates environment variable name
    /// </summary>
    /// <exception cref="ArgumentException">If name is invalid</exception>
    void ValidateName(string name);
    
    /// <summary>
    /// Validates environment variable value
    /// </summary>
    /// <exception cref="SecurityException">If value contains injection patterns</exception>
    void ValidateValue(string name, string value);
}
```

### Infrastructure Implementations

```csharp
namespace AgenticCoder.Infrastructure.Execution.Environment;

public sealed class WorkingDirectoryResolver : IWorkingDirectoryResolver
{
    private readonly IPathValidator _pathValidator;
    private readonly ILogger<WorkingDirectoryResolver> _logger;
    
    public WorkingDirectoryResolver(
        IPathValidator pathValidator,
        ILogger<WorkingDirectoryResolver> logger)
    {
        _pathValidator = pathValidator;
        _logger = logger;
    }
    
    public string Resolve(string? requestedPath, string repoRoot, WorkingDirectoryConfig config)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repoRoot, nameof(repoRoot));
        
        // Use default if not specified
        var effectivePath = requestedPath?.Trim();
        if (string.IsNullOrEmpty(effectivePath))
        {
            effectivePath = config.Default ?? repoRoot;
        }
        
        // Expand variables
        effectivePath = ExpandVariables(effectivePath, repoRoot);
        
        // Normalize path separators
        effectivePath = NormalizePath(effectivePath);
        
        // Make absolute from repo root
        var absolutePath = Path.IsPathRooted(effectivePath)
            ? effectivePath
            : Path.GetFullPath(effectivePath, repoRoot);
        
        // Validate security
        _pathValidator.Validate(absolutePath, repoRoot, config.AllowExternal);
        
        // Verify exists
        if (!Directory.Exists(absolutePath))
        {
            throw new DirectoryNotFoundException(
                $"Working directory not found: {absolutePath}");
        }
        
        // Resolve symlinks
        absolutePath = ResolveSymlinks(absolutePath);
        
        _logger.LogDebug("Resolved working directory: {Path}", absolutePath);
        return absolutePath;
    }
    
    private string ExpandVariables(string path, string repoRoot)
    {
        return path
            .Replace("${repo_root}", repoRoot, StringComparison.OrdinalIgnoreCase)
            .Replace("${workspace}", repoRoot, StringComparison.OrdinalIgnoreCase)
            .Replace("%REPO_ROOT%", repoRoot, StringComparison.OrdinalIgnoreCase);
    }
    
    private static string NormalizePath(string path)
    {
        return OperatingSystem.IsWindows()
            ? path.Replace('/', '\\')
            : path.Replace('\\', '/');
    }
    
    private static string ResolveSymlinks(string path)
    {
        var info = new DirectoryInfo(path);
        return info.LinkTarget ?? path;
    }
}

public sealed class PathValidator : IPathValidator
{
    private static readonly string[] TraversalPatterns = 
        { "..", "%2e%2e", "%252e%252e", "..%2f", "..%5c" };
    
    private static readonly char[] InvalidPathChars = 
        Path.GetInvalidPathChars().Concat(new[] { '\0' }).ToArray();
    
    private static readonly string[] WindowsDeviceNames =
        { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", 
          "LPT1", "LPT2", "LPT3", "LPT4" };
    
    public void Validate(string path, string repoRoot, bool allowExternal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));
        
        // Check for traversal
        if (ContainsTraversal(path))
        {
            throw new SecurityException(
                "SEC-018B-01: Path traversal detected in path");
        }
        
        // Check for invalid characters
        if (path.IndexOfAny(InvalidPathChars) >= 0)
        {
            throw new ArgumentException(
                "SEC-018B-02: Path contains invalid characters");
        }
        
        // Check Windows device names
        if (OperatingSystem.IsWindows())
        {
            var fileName = Path.GetFileNameWithoutExtension(path).ToUpperInvariant();
            if (WindowsDeviceNames.Contains(fileName))
            {
                throw new SecurityException(
                    "SEC-018B-04: Path contains reserved device name");
            }
        }
        
        // Check repo boundary
        if (!allowExternal && !IsWithinRepo(path, repoRoot))
        {
            throw new SecurityException(
                "SEC-018B-05: Path is outside repository root");
        }
    }
    
    public bool ContainsTraversal(string path)
    {
        var normalized = path.ToLowerInvariant();
        return TraversalPatterns.Any(p => 
            normalized.Contains(p, StringComparison.Ordinal));
    }
    
    public bool IsWithinRepo(string path, string repoRoot)
    {
        var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
        var normalizedRoot = Path.GetFullPath(repoRoot).TrimEnd(Path.DirectorySeparatorChar);
        
        return normalizedPath.StartsWith(
            normalizedRoot + Path.DirectorySeparatorChar, 
            StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class EnvironmentBuilder : IEnvironmentBuilder
{
    private readonly IVariableValidator _validator;
    private readonly ILogger<EnvironmentBuilder> _logger;
    
    public IReadOnlyDictionary<string, string> Build(
        EnvironmentMode mode,
        IReadOnlyDictionary<string, string>? specified,
        IReadOnlyDictionary<string, string>? defaults,
        (IEnumerable<string> Prepend, IEnumerable<string> Append)? pathModifications)
    {
        // Validate all specified variables
        if (specified != null)
        {
            foreach (var (name, value) in specified)
            {
                _validator.ValidateName(name);
                _validator.ValidateValue(name, value);
            }
        }
        
        var result = mode switch
        {
            EnvironmentMode.Inherit => BuildInheritMode(specified, defaults),
            EnvironmentMode.Replace => BuildReplaceMode(specified, defaults),
            EnvironmentMode.Merge => BuildMergeMode(specified, defaults),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
        
        // Apply PATH modifications
        if (pathModifications != null)
        {
            result = ApplyPathModifications(result, pathModifications.Value);
        }
        
        return result.AsReadOnly();
    }
    
    private Dictionary<string, string> BuildInheritMode(
        IReadOnlyDictionary<string, string>? specified,
        IReadOnlyDictionary<string, string>? defaults)
    {
        // Start with parent environment
        var result = new Dictionary<string, string>(
            Environment.GetEnvironmentVariables()
                .Cast<DictionaryEntry>()
                .ToDictionary(e => (string)e.Key, e => (string)e.Value!),
            StringComparer.OrdinalIgnoreCase);
        
        // Apply defaults
        if (defaults != null)
        {
            foreach (var (key, value) in defaults)
            {
                result[key] = value;
            }
        }
        
        // Apply specified overrides
        if (specified != null)
        {
            foreach (var (key, value) in specified)
            {
                result[key] = value;
            }
        }
        
        return result;
    }
    
    private Dictionary<string, string> BuildReplaceMode(
        IReadOnlyDictionary<string, string>? specified,
        IReadOnlyDictionary<string, string>? defaults)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        // Only apply defaults and specified
        if (defaults != null)
        {
            foreach (var (key, value) in defaults)
            {
                result[key] = value;
            }
        }
        
        if (specified != null)
        {
            foreach (var (key, value) in specified)
            {
                result[key] = value;
            }
        }
        
        return result;
    }
    
    private Dictionary<string, string> BuildMergeMode(
        IReadOnlyDictionary<string, string>? specified,
        IReadOnlyDictionary<string, string>? defaults)
    {
        // Same as inherit but handle null values for removal
        var result = BuildInheritMode(null, defaults);
        
        if (specified != null)
        {
            foreach (var (key, value) in specified)
            {
                if (value == null)
                {
                    result.Remove(key);
                }
                else
                {
                    result[key] = value;
                }
            }
        }
        
        return result;
    }
    
    private Dictionary<string, string> ApplyPathModifications(
        Dictionary<string, string> env,
        (IEnumerable<string> Prepend, IEnumerable<string> Append) mods)
    {
        var pathKey = OperatingSystem.IsWindows() ? "PATH" : "PATH";
        var separator = OperatingSystem.IsWindows() ? ';' : ':';
        
        var currentPath = env.TryGetValue(pathKey, out var p) ? p : "";
        var pathParts = currentPath.Split(separator, StringSplitOptions.RemoveEmptyEntries).ToList();
        
        // Prepend
        foreach (var entry in mods.Prepend.Reverse())
        {
            pathParts.Insert(0, entry);
        }
        
        // Append
        pathParts.AddRange(mods.Append);
        
        // Remove duplicates keeping first occurrence
        pathParts = pathParts.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        
        env[pathKey] = string.Join(separator, pathParts);
        return env;
    }
}

public sealed class SensitiveRedactor : ISensitiveRedactor
{
    public string RedactedPlaceholder => "<REDACTED>";
    
    public bool IsSensitive(string variableName, IEnumerable<string> patterns)
    {
        return patterns.Any(pattern => MatchesPattern(variableName, pattern));
    }
    
    public IReadOnlyDictionary<string, string> RedactSensitiveValues(
        IReadOnlyDictionary<string, string> variables,
        IEnumerable<string> patterns)
    {
        var patternList = patterns.ToList();
        return variables.ToDictionary(
            kvp => kvp.Key,
            kvp => IsSensitive(kvp.Key, patternList) ? RedactedPlaceholder : kvp.Value);
    }
    
    private static bool MatchesPattern(string name, string pattern)
    {
        // Simple wildcard matching
        if (pattern.StartsWith("*"))
        {
            var suffix = pattern[1..];
            return name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }
        
        if (pattern.EndsWith("*"))
        {
            var prefix = pattern[..^1];
            return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }
        
        if (pattern.Contains("*"))
        {
            // Convert to regex
            var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            return Regex.IsMatch(name, regex, RegexOptions.IgnoreCase);
        }
        
        return name.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class VariableValidator : IVariableValidator
{
    private static readonly Regex ValidNameRegex = new(
        @"^[A-Za-z_][A-Za-z0-9_]*$", 
        RegexOptions.Compiled);
    
    private static readonly string[] InjectionPatterns =
        { ";", "&&", "||", "|", "`", "$(", "${", "\n", "\r" };
    
    private const int MaxNameLength = 1024;
    private const int MaxValueLength = 32768; // 32KB
    
    public void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("VAL-018B-01: Variable name cannot be empty");
        }
        
        if (name.Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"VAL-018B-02: Variable name exceeds {MaxNameLength} characters");
        }
        
        if (!ValidNameRegex.IsMatch(name))
        {
            throw new ArgumentException(
                $"VAL-018B-03: Invalid variable name '{name}'. " +
                "Must match [A-Za-z_][A-Za-z0-9_]*");
        }
    }
    
    public void ValidateValue(string name, string value)
    {
        if (value == null) return; // null is valid (for removal in merge mode)
        
        if (value.Length > MaxValueLength)
        {
            throw new ArgumentException(
                $"VAL-018B-04: Variable '{name}' value exceeds {MaxValueLength} characters");
        }
        
        // Check for shell injection patterns
        foreach (var pattern in InjectionPatterns)
        {
            if (value.Contains(pattern))
            {
                throw new SecurityException(
                    $"SEC-018B-03: Potential shell injection detected in variable '{name}'");
            }
        }
    }
}
```

### Dependency Injection Registration

```csharp
namespace AgenticCoder.Infrastructure.Execution.Environment.DependencyInjection;

public static class EnvironmentServiceExtensions
{
    public static IServiceCollection AddEnvironmentServices(
        this IServiceCollection services)
    {
        services.AddSingleton<IPathValidator, PathValidator>();
        services.AddSingleton<IVariableValidator, VariableValidator>();
        services.AddSingleton<ISensitiveRedactor, SensitiveRedactor>();
        services.AddSingleton<IWorkingDirectoryResolver, WorkingDirectoryResolver>();
        services.AddSingleton<IEnvironmentBuilder, EnvironmentBuilder>();
        services.AddSingleton<IEnvironmentContextFactory, EnvironmentContextFactory>();
        
        return services;
    }
}
```

### Error Codes

| Code | Category | Description |
|------|----------|-------------|
| SEC-018B-01 | Security | Path traversal detected |
| SEC-018B-02 | Security | Invalid characters in path |
| SEC-018B-03 | Security | Shell injection detected |
| SEC-018B-04 | Security | Reserved device name in path |
| SEC-018B-05 | Security | Path outside repository |
| VAL-018B-01 | Validation | Empty variable name |
| VAL-018B-02 | Validation | Variable name too long |
| VAL-018B-03 | Validation | Invalid variable name format |
| VAL-018B-04 | Validation | Variable value too long |
| DIR-018B-01 | Directory | Working directory not found |
| DIR-018B-02 | Directory | Path is file, not directory |
| ENV-018B-01 | Environment | Invalid environment mode |
| ENV-018B-02 | Environment | Variable expansion failed |

### Implementation Checklist

1. [ ] Create domain models (EnvironmentMode, WorkingDirectoryConfig, EnvironmentVariableConfig, EnvironmentContext)
2. [ ] Create interfaces (IWorkingDirectoryResolver, IPathValidator, IEnvironmentBuilder, ISensitiveRedactor, IVariableValidator)
3. [ ] Implement PathValidator with traversal detection, device name detection, repo boundary checks
4. [ ] Implement VariableValidator with name format validation and injection detection
5. [ ] Implement WorkingDirectoryResolver with variable expansion, symlink resolution
6. [ ] Implement EnvironmentBuilder with inherit/replace/merge modes
7. [ ] Implement SensitiveRedactor with wildcard pattern matching
8. [ ] Implement PathManager for PATH variable manipulation
9. [ ] Create EnvironmentContextFactory to orchestrate all components
10. [ ] Add DI registration extensions
11. [ ] Write unit tests for WorkingDirectoryResolver (13 tests)
12. [ ] Write unit tests for PathValidator (12 tests)
13. [ ] Write unit tests for EnvironmentBuilder (12 tests)
14. [ ] Write unit tests for VariableValidator (10 tests)
15. [ ] Write unit tests for SensitiveRedactor (12 tests)
16. [ ] Write unit tests for PathManager (8 tests)
17. [ ] Write integration tests (7 tests)
18. [ ] Add CLI options for --working-dir, --env, --env-mode, --path-prepend, --path-append
19. [ ] Integrate with command runner from Task-018a
20. [ ] Add audit logging for environment context
21. [ ] Update documentation

### Rollout Plan

| Phase | Components | Validation |
|-------|------------|------------|
| 1 | PathValidator, VariableValidator | Security tests pass |
| 2 | WorkingDirectoryResolver | Directory resolution works |
| 3 | EnvironmentBuilder (inherit mode) | Basic environment works |
| 4 | EnvironmentBuilder (replace/merge) | All modes work |
| 5 | SensitiveRedactor | Secrets not in logs |
| 6 | PATH manipulation | Custom paths work |
| 7 | Full integration | E2E scenarios pass |

### Dependencies

- **Task-018a**: Provides CommandRunner that uses EnvironmentContext
- **Task-002a**: Provides config schema for environment settings
- **.NET 8.0**: Process and environment APIs

### Integration Points

```csharp
// Example integration with CommandRunner from Task-018a
public class CommandRunner
{
    private readonly IEnvironmentContextFactory _contextFactory;
    
    public async Task<CommandResult> RunAsync(CommandRequest request)
    {
        // Build environment context
        var context = _contextFactory.Create(
            request.WorkingDirectory,
            request.EnvironmentMode,
            request.EnvironmentVariables);
        
        // Use in process start
        var startInfo = new ProcessStartInfo
        {
            WorkingDirectory = context.WorkingDirectory,
            // ... other settings
        };
        
        // Apply environment
        startInfo.Environment.Clear();
        foreach (var (key, value) in context.Variables)
        {
            startInfo.Environment[key] = value;
        }
        
        // Log with redacted values
        _logger.LogInformation(
            "Executing with env: {@Environment}",
            context.GetAuditSafeVariables());
        
        // ... run process
    }
}
```

---

**End of Task 018.b Specification**