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

### Return on Investment (ROI) Analysis

**Problem Quantification:**
- **Path-related failures:** Average 6 hours/week debugging "file not found" errors = 312 hours/year
- **Environment misconfiguration:** Average 4 hours/week troubleshooting missing/wrong variables = 208 hours/year  
- **Security incident costs:** Path traversal or env injection incident = $150,000+ remediation
- **Cross-platform issues:** Average 3 hours/week on Windows vs Linux differences = 156 hours/year

**Solution Investment:**
- Development effort: ~80 hours
- Testing and hardening: ~40 hours
- Total investment: 120 hours × $75/hour = **$9,000**

**Annual Savings:**
- Eliminated path debugging: 312 hours × $75 = $23,400
- Eliminated env debugging: 208 hours × $75 = $15,600
- Cross-platform resolution: 156 hours × $75 = $11,700
- Security incident prevention (10% probability × $150K): $15,000

**Total Annual Savings: $65,700**
**ROI: 630% first year** ($65,700 - $9,000 = $56,700 net savings)

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    WORKING DIRECTORY & ENV ENFORCEMENT                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│    ┌─────────────────────┐                                                  │
│    │   Command Request   │                                                  │
│    │  - WorkingDir: str  │                                                  │
│    │  - EnvMode: enum    │                                                  │
│    │  - EnvVars: dict    │                                                  │
│    └──────────┬──────────┘                                                  │
│               │                                                             │
│               ▼                                                             │
│    ┌─────────────────────────────────────────────────────────┐             │
│    │            WorkingDirectoryResolver                      │             │
│    │  ┌───────────────┐ ┌───────────────┐ ┌───────────────┐ │             │
│    │  │   Normalize   │→│    Expand     │→│   Validate    │ │             │
│    │  │   (slashes)   │ │  (${REPO})    │ │  (security)   │ │             │
│    │  └───────────────┘ └───────────────┘ └───────────────┘ │             │
│    └──────────────────────────┬──────────────────────────────┘             │
│               │               │                                             │
│               │   ┌───────────┴───────────┐                                │
│               │   │    PathValidator       │                                │
│               │   │  - No .. traversal    │                                │
│               │   │  - Within boundary    │                                │
│               │   │  - No null bytes      │                                │
│               │   │  - No device names    │                                │
│               │   └───────────────────────┘                                │
│               │                                                             │
│               ▼                                                             │
│    ┌─────────────────────────────────────────────────────────┐             │
│    │              EnvironmentBuilder                          │             │
│    │                                                          │             │
│    │   Mode Selection:                                        │             │
│    │   ┌─────────┐  ┌─────────┐  ┌─────────┐               │             │
│    │   │ INHERIT │  │ REPLACE │  │  MERGE  │               │             │
│    │   │ Parent  │  │ Clean   │  │ Parent  │               │             │
│    │   │ + Over- │  │ Slate   │  │ + New   │               │             │
│    │   │ rides   │  │ Only    │  │ (wins)  │               │             │
│    │   └─────────┘  └─────────┘  └─────────┘               │             │
│    │                                                          │             │
│    │   ┌─────────────────────────────────────────────────┐  │             │
│    │   │         Variable Validation                      │  │             │
│    │   │  - Name: ^[a-zA-Z_][a-zA-Z0-9_]*$               │  │             │
│    │   │  - Value: max 32KB, no null bytes               │  │             │
│    │   │  - Total: max ~32KB block (Windows limit)       │  │             │
│    │   └─────────────────────────────────────────────────┘  │             │
│    └──────────────────────────┬──────────────────────────────┘             │
│                               │                                             │
│               ┌───────────────┴───────────────┐                            │
│               ▼                               ▼                            │
│    ┌─────────────────────┐         ┌─────────────────────┐                │
│    │  SensitiveRedactor  │         │    PathManager      │                │
│    │                     │         │                     │                │
│    │ Patterns:           │         │ Operations:         │                │
│    │ - *_KEY             │         │ - Prepend to PATH   │                │
│    │ - *_SECRET          │         │ - Append to PATH    │                │
│    │ - *_TOKEN           │         │ - Dedup entries     │                │
│    │ - *_PASSWORD        │         │ - Platform sep      │                │
│    │ - *_CREDENTIAL      │         │                     │                │
│    └──────────┬──────────┘         └──────────┬──────────┘                │
│               │                               │                            │
│               ▼                               ▼                            │
│    ┌─────────────────────────────────────────────────────────┐             │
│    │              Execution Context                           │             │
│    │  ┌───────────────────────────────────────────────────┐ │             │
│    │  │ WorkingDirectory: /home/user/project              │ │             │
│    │  │ Environment: {                                     │ │             │
│    │  │   "PATH": "/tool/bin:/usr/bin:/bin",              │ │             │
│    │  │   "NODE_ENV": "development",                       │ │             │
│    │  │   "API_KEY": "sk-xxx..." (redacted in logs)       │ │             │
│    │  │ }                                                   │ │             │
│    │  └───────────────────────────────────────────────────┘ │             │
│    └──────────────────────────┬──────────────────────────────┘             │
│                               │                                             │
│                               ▼                                             │
│                    ┌─────────────────────┐                                 │
│                    │   Command Executor  │                                 │
│                    │   (Task 018 parent) │                                 │
│                    └─────────────────────┘                                 │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Environment Modes Explained

```
INHERIT Mode (default):
┌─────────────────────────────────────────────────────────┐
│  Parent Process Environment                              │
│  ┌─────────────────────────────────────────────────────┐│
│  │ PATH=/usr/bin:/bin                                   ││
│  │ HOME=/home/user                                      ││
│  │ LANG=en_US.UTF-8                                    ││
│  │ NODE_ENV=development                                 ││
│  └─────────────────────────────────────────────────────┘│
│                         │                                │
│                         ▼ Apply overrides                │
│  ┌─────────────────────────────────────────────────────┐│
│  │ Specified: { NODE_ENV: "production" }               ││
│  └─────────────────────────────────────────────────────┘│
│                         │                                │
│                         ▼ Result                         │
│  ┌─────────────────────────────────────────────────────┐│
│  │ PATH=/usr/bin:/bin        (from parent)             ││
│  │ HOME=/home/user           (from parent)             ││
│  │ LANG=en_US.UTF-8          (from parent)             ││
│  │ NODE_ENV=production       (OVERRIDDEN)              ││
│  └─────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────┘

REPLACE Mode:
┌─────────────────────────────────────────────────────────┐
│  Parent Environment IGNORED                              │
│                                                          │
│  ┌─────────────────────────────────────────────────────┐│
│  │ Specified: {                                         ││
│  │   PATH: "/custom/bin:/usr/bin",                     ││
│  │   NODE_ENV: "production"                            ││
│  │ }                                                    ││
│  └─────────────────────────────────────────────────────┘│
│                         │                                │
│                         ▼ Result                         │
│  ┌─────────────────────────────────────────────────────┐│
│  │ PATH=/custom/bin:/usr/bin  (only these)             ││
│  │ NODE_ENV=production                                  ││
│  │ (HOME, LANG, etc. NOT present)                      ││
│  └─────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────┘

MERGE Mode:
┌─────────────────────────────────────────────────────────┐
│  Parent: { PATH: "/usr/bin", HOME: "/home/user" }       │
│  Specified: { PATH: "/custom/bin", NEW_VAR: "value" }   │
│                                                          │
│  Merge Strategy: Specified wins on conflict             │
│                         │                                │
│                         ▼ Result                         │
│  ┌─────────────────────────────────────────────────────┐│
│  │ PATH=/custom/bin           (SPECIFIED wins)         ││
│  │ HOME=/home/user            (from parent)            ││
│  │ NEW_VAR=value              (new from specified)     ││
│  └─────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────┘
```

### Architectural Trade-Offs

| Decision | Trade-off | Rationale |
|----------|-----------|-----------|
| Validate paths on resolution | Adds latency vs catching errors early | Early validation prevents wasted process spawns |
| Inherit mode as default | Security (leaks parent env) vs convenience | Most commands need parent PATH/HOME; users expect inheritance |
| Redact by pattern matching | False positives vs missed secrets | Patterns tuned for common conventions; custom patterns supported |
| Platform-specific PATH handling | Code complexity vs correctness | PATH separator and case sensitivity differ fundamentally |
| Maximum env block size | Limits very large configs vs preventing process start failure | Windows has 32KB limit; enforcing prevents cryptic failures |

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

---

## Use Cases

### Scenario 1: DevBot Runs Build in Subdirectory

**Persona:** DevBot (autonomous agent)

**Context:** DevBot clones a monorepo and needs to build a specific package located in packages/api/. The build command must run in that subdirectory for relative imports to work correctly.

**Workflow:**

**Before (Without Working Directory Enforcement):**
1. DevBot executes `npm run build` from repo root
2. Build fails with "Cannot find module '../shared/utils'"
3. DevBot doesn't understand the error
4. Retry loops don't help
5. 15 minutes wasted before human intervenes

**After (With Task 018.b):**
1. DevBot sends command with `workingDir: "${REPO}/packages/api"`
2. WorkingDirectoryResolver:
   - Expands `${REPO}` to `/home/agent/work/monorepo`
   - Validates `/home/agent/work/monorepo/packages/api` exists
   - Confirms it's within allowed boundaries
3. Command executes in correct directory
4. Build succeeds on first attempt
5. **Time saved: 15 minutes per occurrence, ~20 occurrences/week = 5 hours/week**

**Technical Details:**
```csharp
var context = await _contextResolver.ResolveAsync(new ContextRequest
{
    WorkingDir = "${REPO}/packages/api",
    EnvMode = EnvironmentMode.Inherit,
    Environment = new Dictionary<string, string>
    {
        ["NODE_ENV"] = "production"
    }
});

// context.WorkingDirectory = "/home/agent/work/monorepo/packages/api"
// context.Environment includes parent PATH, HOME, plus NODE_ENV=production
```

---

### Scenario 2: Sarah Ensures API Keys Don't Leak to Logs

**Persona:** Sarah, Security Engineer

**Context:** Production deployment requires AWS credentials passed to deployment scripts. Sarah needs assurance that credentials appear in process environment but never in audit logs or error messages.

**Workflow:**

**Before (Without Sensitive Redaction):**
1. Deployment script needs AWS_SECRET_ACCESS_KEY
2. Command fails, full environment dumped to logs
3. AWS_SECRET_ACCESS_KEY visible in CloudWatch
4. Security incident declared
5. Key rotated, incident response engaged
6. **Cost: $25,000+ in incident response**

**After (With Task 018.b):**
1. Deployment command includes sensitive variables
2. SensitiveRedactor detects `*_SECRET_*` pattern
3. Process receives actual key value
4. Audit log records: `AWS_SECRET_ACCESS_KEY=[REDACTED:40 chars]`
5. Log review shows masked value
6. **Security maintained, zero incidents**

**Technical Details:**
```csharp
// Sensitive detection patterns (built-in)
var patterns = new[]
{
    @".*_KEY$",
    @".*_SECRET.*",
    @".*_TOKEN$",
    @".*_PASSWORD$",
    @".*_CREDENTIAL.*",
    @"^API_KEY$"
};

// In audit log:
var redacted = _redactor.RedactForAudit(context.Environment);
// AWS_ACCESS_KEY_ID = "AKIA..." (not sensitive by default)
// AWS_SECRET_ACCESS_KEY = "[REDACTED:40 chars]"
```

---

### Scenario 3: Marcus Debugs PATH Resolution Issue

**Persona:** Marcus, DevOps Engineer

**Context:** A build tool installed in a custom location isn't being found. Marcus needs to prepend a directory to PATH and verify the final PATH value.

**Workflow:**

**Before (Without PATH Management):**
1. Marcus manually constructs PATH string
2. Forgets correct separator (: vs ;) on Windows
3. Tool still not found
4. Spends 30 minutes debugging
5. Realizes typo in path

**After (With Task 018.b):**
1. Marcus uses PathManager to prepend:
   ```yaml
   environment:
     PATH_PREPEND: /custom/tools/bin
   ```
2. PathManager:
   - Detects platform separator automatically
   - Validates prepended path exists
   - Deduplicates if already present
   - Constructs final PATH correctly
3. Tool found immediately
4. Audit shows: `PATH=/custom/tools/bin:/usr/local/bin:/usr/bin`
5. **Time saved: 25 minutes**

**Technical Details:**
```csharp
var pathManager = new PathManager();
var newPath = pathManager.Prepend(
    currentPath: "/usr/local/bin:/usr/bin:/bin",
    prependPath: "/custom/tools/bin"
);
// Result: "/custom/tools/bin:/usr/local/bin:/usr/bin:/bin"

// On Windows:
var winPath = pathManager.Prepend(
    currentPath: @"C:\Windows\system32;C:\Windows",
    prependPath: @"C:\Tools\bin"
);
// Result: @"C:\Tools\bin;C:\Windows\system32;C:\Windows"
```

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

## Assumptions

### Technical Assumptions

1. **Process Environment Support:** .NET ProcessStartInfo.Environment provides read/write access to child process environment variables.

2. **Working Directory Setting:** ProcessStartInfo.WorkingDirectory is respected by the child process on all platforms.

3. **Environment Inheritance:** By default, child processes inherit parent process environment unless explicitly cleared.

4. **Platform Path Separators:** PATH separator is `;` on Windows and `:` on Unix-like systems.

5. **Environment Case Sensitivity:** Windows environment variables are case-insensitive; Unix are case-sensitive.

6. **Maximum Block Size:** Windows has a ~32KB limit on total environment block size; Unix limits are typically much higher.

7. **Directory Existence:** Working directory must exist before process start; Process.Start throws if it doesn't.

### Operational Assumptions

8. **Repository Root Available:** The repository root path is known and accessible at command execution time.

9. **Relative Paths Common:** Most commands use relative paths from working directory, not absolute paths.

10. **PATH Modifications Needed:** Build tools often require PATH prepending for custom tool locations.

11. **Sensitive Variables Present:** Production workloads will include API keys, tokens, and credentials in environment.

12. **Audit Required:** All command executions must be auditable, but secrets must be redacted.

13. **Cross-Platform Commands:** Same command definitions may execute on Windows, Linux, and macOS.

### Integration Assumptions

14. **Configuration Loaded:** Environment configuration from Task 002 is available before command execution.

15. **Audit System Ready:** Audit service (Task 003.c) is available to persist context records.

16. **RepoFS Provides Root:** Task 014 RepoFS provides reliable repository root path.

17. **No Concurrent Modification:** Environment configuration doesn't change during command execution.

18. **Reasonable Variable Counts:** Typical commands require fewer than 100 environment variables.

---

## Security Threats and Mitigations

### Threat 1: Path Traversal Attack

**Risk:** HIGH - Malicious path could escape sandbox and access sensitive files.

**Attack Scenario:**
```bash
# Attacker provides working directory:
workingDir: "../../../etc"
# Or with encoding:
workingDir: "..%2F..%2F..%2Fetc"
```

**Complete Mitigation Code:**

```csharp
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Acode.Infrastructure.Execution;

/// <summary>
/// Validates paths to prevent traversal attacks.
/// </summary>
public sealed class PathValidator
{
    private readonly string _boundaryPath;
    private readonly bool _allowExternal;
    
    public PathValidator(string boundaryPath, bool allowExternal = false)
    {
        _boundaryPath = Path.GetFullPath(boundaryPath);
        _allowExternal = allowExternal;
    }
    
    /// <summary>
    /// Validates a path for security issues.
    /// </summary>
    public PathValidationResult Validate(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return PathValidationResult.Fail("Path cannot be empty");
        
        // Check for null bytes (can truncate paths in some contexts)
        if (path.Contains('\0'))
            return PathValidationResult.Fail("Path contains null byte");
        
        // Check for encoded traversal sequences
        var decodedPath = Uri.UnescapeDataString(path);
        if (ContainsTraversal(decodedPath))
            return PathValidationResult.Fail("Path contains traversal sequence");
        
        // Normalize the path
        string normalizedPath;
        try
        {
            normalizedPath = Path.GetFullPath(path);
        }
        catch (Exception ex)
        {
            return PathValidationResult.Fail($"Invalid path format: {ex.Message}");
        }
        
        // Check for Windows device names
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var fileName = Path.GetFileName(normalizedPath);
            if (IsWindowsDeviceName(fileName))
                return PathValidationResult.Fail("Path resolves to Windows device name");
        }
        
        // Check boundary unless external paths allowed
        if (!_allowExternal)
        {
            if (!normalizedPath.StartsWith(_boundaryPath, GetPathComparison()))
                return PathValidationResult.Fail(
                    $"Path '{normalizedPath}' is outside boundary '{_boundaryPath}'");
        }
        
        return PathValidationResult.Success(normalizedPath);
    }
    
    private static bool ContainsTraversal(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return Array.Exists(segments, s => s == "..");
    }
    
    private static bool IsWindowsDeviceName(string name)
    {
        var baseName = Path.GetFileNameWithoutExtension(name).ToUpperInvariant();
        return baseName is "CON" or "PRN" or "AUX" or "NUL" or
            "COM1" or "COM2" or "COM3" or "COM4" or "COM5" or
            "COM6" or "COM7" or "COM8" or "COM9" or
            "LPT1" or "LPT2" or "LPT3" or "LPT4" or "LPT5" or
            "LPT6" or "LPT7" or "LPT8" or "LPT9";
    }
    
    private static StringComparison GetPathComparison()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
    }
}

public record PathValidationResult(bool IsValid, string? NormalizedPath, string? Error)
{
    public static PathValidationResult Success(string path) => new(true, path, null);
    public static PathValidationResult Fail(string error) => new(false, null, error);
}
```

---

### Threat 2: Environment Variable Injection

**Risk:** HIGH - Malicious variable names/values could execute code or access secrets.

**Attack Scenario:**
```bash
# Attacker provides malicious variable name:
env: { "LD_PRELOAD": "/tmp/evil.so" }
# Or shell injection in value:
env: { "PATH": "$(cat /etc/passwd > /tmp/leak)" }
```

**Complete Mitigation Code:**

```csharp
using System;
using System.Text.RegularExpressions;

namespace Acode.Infrastructure.Execution;

/// <summary>
/// Validates environment variable names and values.
/// </summary>
public sealed class VariableValidator
{
    // Valid name: starts with letter/underscore, then alphanumeric/underscore
    private static readonly Regex ValidNamePattern = new(
        @"^[a-zA-Z_][a-zA-Z0-9_]*$",
        RegexOptions.Compiled);
    
    // Dangerous patterns that could enable injection
    private static readonly string[] DangerousNames = new[]
    {
        "LD_PRELOAD",
        "LD_LIBRARY_PATH",
        "DYLD_INSERT_LIBRARIES",
        "DYLD_LIBRARY_PATH",
        "PYTHONPATH",
        "NODE_OPTIONS",
        "BASH_ENV",
        "ENV",
        "PROMPT_COMMAND"
    };
    
    // Shell metacharacters that could enable injection in values
    private static readonly Regex ShellInjectionPattern = new(
        @"\$\(|`|\||;|&&|\|\||>|<",
        RegexOptions.Compiled);
    
    private readonly int _maxNameLength;
    private readonly int _maxValueLength;
    private readonly bool _allowDangerousNames;
    
    public VariableValidator(
        int maxNameLength = 256,
        int maxValueLength = 32768,
        bool allowDangerousNames = false)
    {
        _maxNameLength = maxNameLength;
        _maxValueLength = maxValueLength;
        _allowDangerousNames = allowDangerousNames;
    }
    
    /// <summary>
    /// Validates a variable name and value pair.
    /// </summary>
    public VariableValidationResult Validate(string name, string? value)
    {
        // Validate name
        if (string.IsNullOrEmpty(name))
            return VariableValidationResult.Fail("Variable name cannot be empty");
        
        if (name.Length > _maxNameLength)
            return VariableValidationResult.Fail(
                $"Variable name exceeds maximum length of {_maxNameLength}");
        
        if (!ValidNamePattern.IsMatch(name))
            return VariableValidationResult.Fail(
                $"Variable name '{name}' contains invalid characters");
        
        // Check for dangerous names
        if (!_allowDangerousNames && Array.Exists(DangerousNames, 
            d => d.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            return VariableValidationResult.Fail(
                $"Variable name '{name}' is potentially dangerous and blocked");
        }
        
        // Validate value
        if (value != null)
        {
            if (value.Length > _maxValueLength)
                return VariableValidationResult.Fail(
                    $"Variable value exceeds maximum length of {_maxValueLength}");
            
            if (value.Contains('\0'))
                return VariableValidationResult.Fail(
                    "Variable value contains null byte");
            
            // Check for shell injection patterns
            if (ShellInjectionPattern.IsMatch(value))
            {
                // Don't block, just flag for logging
                return VariableValidationResult.Warn(
                    "Variable value contains shell metacharacters");
            }
        }
        
        return VariableValidationResult.Success();
    }
}

public record VariableValidationResult(bool IsValid, bool HasWarning, string? Message)
{
    public static VariableValidationResult Success() => new(true, false, null);
    public static VariableValidationResult Warn(string msg) => new(true, true, msg);
    public static VariableValidationResult Fail(string msg) => new(false, false, msg);
}
```

---

### Threat 3: Credential Leakage in Logs

**Risk:** MEDIUM - Sensitive values could be exposed in audit logs or error messages.

**Attack Scenario:**
```
# Normal operation logs environment:
INFO: Command executed with environment:
  AWS_ACCESS_KEY_ID=AKIAIOSFODNN7EXAMPLE
  AWS_SECRET_ACCESS_KEY=wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY
# Now secrets visible in CloudWatch/Splunk/etc.
```

**Complete Mitigation Code:**

```csharp
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Acode.Infrastructure.Execution;

/// <summary>
/// Redacts sensitive environment variables for safe logging.
/// </summary>
public sealed class SensitiveRedactor
{
    private readonly List<Regex> _sensitivePatterns;
    
    public SensitiveRedactor(IEnumerable<string>? additionalPatterns = null)
    {
        _sensitivePatterns = new List<Regex>
        {
            // Standard secret patterns
            new Regex(@".*_KEY$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@".*_SECRET.*", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@".*_TOKEN$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@".*_PASSWORD$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@".*_CREDENTIAL.*", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"^API_KEY$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"^AUTH.*", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@".*_AUTH$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"^PRIVATE_.*", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@".*_PRIVATE$", RegexOptions.IgnoreCase | RegexOptions.Compiled)
        };
        
        if (additionalPatterns != null)
        {
            foreach (var pattern in additionalPatterns)
            {
                _sensitivePatterns.Add(new Regex(pattern, 
                    RegexOptions.IgnoreCase | RegexOptions.Compiled));
            }
        }
    }
    
    /// <summary>
    /// Checks if a variable name matches sensitive patterns.
    /// </summary>
    public bool IsSensitive(string variableName)
    {
        if (string.IsNullOrEmpty(variableName))
            return false;
        
        foreach (var pattern in _sensitivePatterns)
        {
            if (pattern.IsMatch(variableName))
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Redacts a value for logging, showing only length hint.
    /// </summary>
    public string RedactValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "[EMPTY]";
        
        return $"[REDACTED:{value.Length} chars]";
    }
    
    /// <summary>
    /// Redacts an entire environment dictionary for safe logging.
    /// </summary>
    public Dictionary<string, string> RedactEnvironment(
        IDictionary<string, string?> environment)
    {
        var redacted = new Dictionary<string, string>();
        
        foreach (var kvp in environment)
        {
            if (IsSensitive(kvp.Key))
            {
                redacted[kvp.Key] = RedactValue(kvp.Value);
            }
            else
            {
                redacted[kvp.Key] = kvp.Value ?? "[NULL]";
            }
        }
        
        return redacted;
    }
}
```

---

### Threat 4: Environment Block Overflow

**Risk:** LOW - Oversized environment could cause process start failure or truncation.

**Attack Scenario:**
```csharp
// Create massive environment
for (int i = 0; i < 10000; i++)
{
    env[$"VAR_{i}"] = new string('X', 10000); // 100MB total
}
// Process.Start fails with cryptic error
```

**Complete Mitigation Code:**

```csharp
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Acode.Infrastructure.Execution;

/// <summary>
/// Validates total environment block size.
/// </summary>
public sealed class EnvironmentSizeValidator
{
    // Windows limit is approximately 32KB for environment block
    // Unix limits are typically much higher (128KB+)
    private readonly int _maxBlockSize;
    
    public EnvironmentSizeValidator(int? maxBlockSize = null)
    {
        _maxBlockSize = maxBlockSize ?? (
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                ? 32 * 1024  // 32KB on Windows
                : 128 * 1024 // 128KB on Unix
        );
    }
    
    /// <summary>
    /// Calculates environment block size and validates against limit.
    /// </summary>
    public EnvironmentSizeResult Validate(IDictionary<string, string?> environment)
    {
        int totalSize = 0;
        var variableSizes = new Dictionary<string, int>();
        
        foreach (var kvp in environment)
        {
            // Format: NAME=VALUE\0
            int entrySize = Encoding.Unicode.GetByteCount(
                $"{kvp.Key}={kvp.Value ?? ""}\0");
            
            variableSizes[kvp.Key] = entrySize;
            totalSize += entrySize;
        }
        
        // Add final null terminator
        totalSize += 2;
        
        if (totalSize > _maxBlockSize)
        {
            // Find largest variables to suggest trimming
            var sorted = new List<KeyValuePair<string, int>>(variableSizes);
            sorted.Sort((a, b) => b.Value.CompareTo(a.Value));
            
            var largestVars = new List<string>();
            for (int i = 0; i < Math.Min(5, sorted.Count); i++)
            {
                largestVars.Add($"{sorted[i].Key} ({sorted[i].Value} bytes)");
            }
            
            return EnvironmentSizeResult.Fail(
                totalSize,
                _maxBlockSize,
                $"Environment block size ({totalSize} bytes) exceeds limit ({_maxBlockSize} bytes). " +
                $"Largest variables: {string.Join(", ", largestVars)}");
        }
        
        return EnvironmentSizeResult.Success(totalSize, _maxBlockSize);
    }
}

public record EnvironmentSizeResult(
    bool IsValid, 
    int ActualSize, 
    int MaxSize, 
    string? Error)
{
    public static EnvironmentSizeResult Success(int actual, int max) => 
        new(true, actual, max, null);
    public static EnvironmentSizeResult Fail(int actual, int max, string error) => 
        new(false, actual, max, error);
}
```

---

### Threat 5: Symlink Escape

**Risk:** MEDIUM - Working directory could follow symlink outside boundaries.

**Attack Scenario:**
```bash
# Attacker creates symlink inside repo
ln -s /etc repo/innocent-looking-dir
# Request uses that path
workingDir: "repo/innocent-looking-dir"
# Resolves to /etc, bypassing boundary check
```

**Complete Mitigation Code:**

```csharp
using System;
using System.IO;

namespace Acode.Infrastructure.Execution;

/// <summary>
/// Resolves symlinks to verify final path is within boundaries.
/// </summary>
public sealed class SymlinkResolver
{
    /// <summary>
    /// Resolves a path following all symlinks to get the real path.
    /// </summary>
    public string ResolveFully(string path)
    {
        var fullPath = Path.GetFullPath(path);
        
        // On Unix, use realpath equivalent
        // On Windows, use final path resolution
        var current = fullPath;
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        while (true)
        {
            if (!visited.Add(current))
                throw new InvalidOperationException(
                    $"Circular symlink detected: {current}");
            
            if (visited.Count > 40)
                throw new InvalidOperationException(
                    "Too many symlink levels (max 40)");
            
            var linkTarget = GetSymlinkTarget(current);
            if (linkTarget == null)
                break; // Not a symlink, done
            
            // Resolve relative to parent directory
            if (!Path.IsPathRooted(linkTarget))
            {
                var parent = Path.GetDirectoryName(current);
                linkTarget = Path.GetFullPath(Path.Combine(parent ?? "", linkTarget));
            }
            
            current = linkTarget;
        }
        
        return current;
    }
    
    private string? GetSymlinkTarget(string path)
    {
        try
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.LinkTarget != null)
                return fileInfo.LinkTarget;
            
            var dirInfo = new DirectoryInfo(path);
            if (dirInfo.LinkTarget != null)
                return dirInfo.LinkTarget;
            
            return null;
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Validates that resolved path is within boundary.
    /// </summary>
    public SymlinkValidationResult ValidateWithinBoundary(
        string path, 
        string boundary)
    {
        var resolvedPath = ResolveFully(path);
        var resolvedBoundary = ResolveFully(boundary);
        
        if (!resolvedPath.StartsWith(resolvedBoundary, StringComparison.OrdinalIgnoreCase))
        {
            return SymlinkValidationResult.Fail(
                $"Path '{path}' resolves to '{resolvedPath}' which is outside boundary '{resolvedBoundary}'");
        }
        
        return SymlinkValidationResult.Success(resolvedPath);
    }
}

public record SymlinkValidationResult(bool IsValid, string? ResolvedPath, string? Error)
{
    public static SymlinkValidationResult Success(string path) => new(true, path, null);
    public static SymlinkValidationResult Fail(string error) => new(false, null, error);
}
```

---

## Troubleshooting

### Issue 1: Directory Not Found Error

**Symptoms:**
- Error: "DirectoryNotFoundException: Could not find directory..."
- Command fails immediately before execution
- Path appears correct visually
- Works when run manually

**Causes:**
- Relative path resolved from wrong base directory
- Variable expansion (`${REPO}`) failed
- Path exists but is a file, not directory
- Symlink target doesn't exist
- Typo in path (Windows path used on Linux or vice versa)

**Solutions:**
1. Check path resolution base:
   ```yaml
   # Explicit absolute path
   working_dir: /home/user/project/subdir
   
   # Or use ${REPO} variable
   working_dir: ${REPO}/subdir
   ```

2. Verify path exists:
   ```bash
   # Check if path exists and is directory
   test -d "/path/to/dir" && echo "Exists" || echo "Missing"
   ```

3. Check variable expansion:
   ```bash
   # Debug: see what ${REPO} expands to
   acode config show | grep repo_root
   ```

---

### Issue 2: Path Traversal Blocked

**Symptoms:**
- Error: "SecurityException: Path contains traversal sequence"
- Using `..` in path
- Legitimate need to access parent directory
- Encoded paths also rejected

**Causes:**
- Path contains `../` sequences
- URL-encoded `%2F..%2F` patterns detected
- Path resolves outside repository boundary
- Security boundary too restrictive for use case

**Solutions:**
1. Use absolute paths instead of relative:
   ```yaml
   # Instead of: working_dir: ../sibling-repo
   working_dir: /home/user/sibling-repo
   ```

2. Enable external paths if authorized:
   ```yaml
   execution:
     allow_external_paths: true
     allowed_paths:
       - /home/user/other-project
   ```

3. Move resources inside repository:
   ```bash
   # Copy needed files into repo
   cp -r /external/resource ./local-copy
   ```

---

### Issue 3: Environment Variable Not Visible

**Symptoms:**
- Command doesn't see expected variable
- `echo $VAR` returns empty
- Variable set but command uses wrong value
- Works locally but not in automation

**Causes:**
- Wrong environment mode (REPLACE instead of INHERIT)
- Variable name invalid (rejected by validator)
- Variable marked as sensitive but value is null
- Case sensitivity difference (Linux vs Windows)
- Variable blocked as dangerous name

**Solutions:**
1. Check environment mode:
   ```yaml
   # INHERIT mode includes parent environment
   execution:
     env_mode: inherit
     environment:
       CUSTOM_VAR: "value"
   ```

2. Verify variable name is valid:
   ```bash
   # Valid: starts with letter/underscore, alphanumeric
   VALID_NAME=value     # OK
   _ALSO_VALID=value    # OK
   123_INVALID=value    # REJECTED
   ```

3. Check if blocked as dangerous:
   ```yaml
   # If you really need LD_PRELOAD (not recommended):
   execution:
     allow_dangerous_env_vars: true
   ```

---

### Issue 4: PATH Not Updated

**Symptoms:**
- Custom tools not found
- `command not found` errors
- PATH changes don't take effect
- Works with absolute path but not command name

**Causes:**
- PATH not inherited (REPLACE mode)
- Prepend/append order wrong
- Wrong separator for platform
- Duplicate entries causing confusion
- PATH variable case issue on Windows

**Solutions:**
1. Use PATH_PREPEND for tool directories:
   ```yaml
   environment:
     PATH_PREPEND: /custom/tools/bin
   ```

2. Verify platform separator:
   ```csharp
   // Automatic handling
   var separator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
       ? ";" 
       : ":";
   ```

3. Check final PATH value:
   ```bash
   # Debug: print PATH in command
   acode exec "echo $PATH"  # Linux
   acode exec "echo %PATH%"  # Windows
   ```

---

### Issue 5: Sensitive Values in Logs

**Symptoms:**
- Credentials visible in audit logs
- Secret not redacted
- Partial redaction (some secrets hidden, some visible)
- Custom secret pattern not matched

**Causes:**
- Variable name doesn't match built-in patterns
- Custom pattern not configured
- Pattern regex error
- Logging occurring before redaction applied

**Solutions:**
1. Use standard naming convention:
   ```yaml
   # These patterns are auto-detected:
   environment:
     AWS_SECRET_ACCESS_KEY: "..."  # Matches *_SECRET_*
     GITHUB_TOKEN: "..."           # Matches *_TOKEN
     DB_PASSWORD: "..."            # Matches *_PASSWORD
   ```

2. Add custom patterns:
   ```yaml
   security:
     sensitive_patterns:
       - "^MY_COMPANY_.*"
       - ".*_CRED$"
   ```

3. Verify pattern works:
   ```bash
   # Test pattern matching
   acode config test-sensitive-pattern "MY_VAR_NAME"
   ```

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

## Best Practices

### Working Directory

1. **Validate before execution** - Ensure directory exists and is accessible
2. **Use absolute paths** - Resolve relative paths before spawning process
3. **Preserve original for logging** - Show user-specified path in logs, resolved path in debug
4. **Handle symbolic links** - Resolve symlinks consistently

### Environment Variables

5. **Inherit selectively** - Start with clean env, add specific variables
6. **Redact secrets in logs** - Never log values of secret environment variables
7. **Validate required variables** - Fail fast if required env vars missing
8. **Document special variables** - Explain agent-injected variables in docs

### Security

9. **Sanitize PATH** - Don't inherit potentially malicious PATH entries
10. **Block dangerous variables** - Prevent LD_PRELOAD and similar on Linux
11. **Audit env changes** - Log what environment was set for each execution
12. **Enforce sandbox boundaries** - Env vars respect current operating mode

---

## Testing Requirements

### Complete Test Implementations

#### PathValidatorTests.cs

```csharp
using System;
using System.IO;
using System.Runtime.InteropServices;
using Acode.Infrastructure.Execution;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Execution;

/// <summary>
/// Tests for path validation and security.
/// </summary>
public class PathValidatorTests
{
    private readonly string _testBoundary;
    private readonly PathValidator _validator;
    
    public PathValidatorTests()
    {
        _testBoundary = Path.GetTempPath();
        _validator = new PathValidator(_testBoundary);
    }
    
    [Fact]
    public void Validate_ValidPath_ReturnsSuccess()
    {
        // Arrange
        var path = Path.Combine(_testBoundary, "valid", "path");
        Directory.CreateDirectory(path);
        
        try
        {
            // Act
            var result = _validator.Validate(path);
            
            // Assert
            result.IsValid.Should().BeTrue();
            result.NormalizedPath.Should().NotBeNullOrEmpty();
        }
        finally
        {
            Directory.Delete(path, true);
        }
    }
    
    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\windows\\system32")]
    [InlineData("subdir/../../../etc")]
    public void Validate_PathTraversal_ReturnsFail(string path)
    {
        // Act
        var result = _validator.Validate(path);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("traversal");
    }
    
    [Theory]
    [InlineData("..%2F..%2Fetc")]
    [InlineData("%2e%2e%2f%2e%2e%2fetc")]
    public void Validate_EncodedTraversal_ReturnsFail(string path)
    {
        // Act
        var result = _validator.Validate(path);
        
        // Assert
        result.IsValid.Should().BeFalse();
    }
    
    [Fact]
    public void Validate_NullByte_ReturnsFail()
    {
        // Arrange
        var path = "valid\x00/../etc";
        
        // Act
        var result = _validator.Validate(path);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("null byte");
    }
    
    [SkippableFact]
    public void Validate_DeviceName_ReturnsFailOnWindows()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        
        // Arrange
        var path = "CON";
        
        // Act
        var result = _validator.Validate(path);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("device name");
    }
    
    [Fact]
    public void Validate_PathOutsideBoundary_ReturnsFail()
    {
        // Arrange
        var outsidePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"C:\Windows\System32"
            : "/etc";
        
        // Act
        var result = _validator.Validate(outsidePath);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("outside boundary");
    }
    
    [Fact]
    public void Validate_PathOutsideBoundary_SucceedsWhenAllowed()
    {
        // Arrange
        var permissiveValidator = new PathValidator(_testBoundary, allowExternal: true);
        var outsidePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"C:\Windows"
            : "/tmp";
        
        // Act
        var result = permissiveValidator.Validate(outsidePath);
        
        // Assert
        result.IsValid.Should().BeTrue();
    }
}
```

#### VariableValidatorTests.cs

```csharp
using System;
using Acode.Infrastructure.Execution;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Execution;

/// <summary>
/// Tests for environment variable validation.
/// </summary>
public class VariableValidatorTests
{
    private readonly VariableValidator _validator;
    
    public VariableValidatorTests()
    {
        _validator = new VariableValidator();
    }
    
    [Theory]
    [InlineData("VALID_NAME")]
    [InlineData("_UNDERSCORE_START")]
    [InlineData("lowercase")]
    [InlineData("CamelCase")]
    [InlineData("NAME123")]
    public void Validate_ValidName_ReturnsSuccess(string name)
    {
        // Act
        var result = _validator.Validate(name, "value");
        
        // Assert
        result.IsValid.Should().BeTrue();
    }
    
    [Theory]
    [InlineData("123_STARTS_WITH_DIGIT")]
    [InlineData("HAS-HYPHEN")]
    [InlineData("HAS.DOT")]
    [InlineData("HAS SPACE")]
    [InlineData("HAS=EQUALS")]
    public void Validate_InvalidName_ReturnsFail(string name)
    {
        // Act
        var result = _validator.Validate(name, "value");
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("invalid");
    }
    
    [Fact]
    public void Validate_EmptyName_ReturnsFail()
    {
        // Act
        var result = _validator.Validate("", "value");
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("empty");
    }
    
    [Fact]
    public void Validate_VeryLongName_ReturnsFail()
    {
        // Arrange
        var longName = new string('A', 1000);
        
        // Act
        var result = _validator.Validate(longName, "value");
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("length");
    }
    
    [Fact]
    public void Validate_VeryLongValue_ReturnsFail()
    {
        // Arrange
        var longValue = new string('X', 100000);
        
        // Act
        var result = _validator.Validate("NAME", longValue);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("length");
    }
    
    [Theory]
    [InlineData("LD_PRELOAD")]
    [InlineData("DYLD_INSERT_LIBRARIES")]
    [InlineData("BASH_ENV")]
    public void Validate_DangerousName_ReturnsFail(string name)
    {
        // Act
        var result = _validator.Validate(name, "value");
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("dangerous");
    }
    
    [Theory]
    [InlineData("$(cat /etc/passwd)")]
    [InlineData("`whoami`")]
    [InlineData("value; rm -rf /")]
    public void Validate_ShellInjection_ReturnsWarning(string value)
    {
        // Act
        var result = _validator.Validate("SAFE_NAME", value);
        
        // Assert - Warning but still valid (up to caller to decide)
        result.IsValid.Should().BeTrue();
        result.HasWarning.Should().BeTrue();
        result.Message.Should().Contain("metacharacters");
    }
    
    [Fact]
    public void Validate_NullValue_ReturnsSuccess()
    {
        // Act - Null value means "unset this variable"
        var result = _validator.Validate("NAME", null);
        
        // Assert
        result.IsValid.Should().BeTrue();
    }
}
```

#### SensitiveRedactorTests.cs

```csharp
using System.Collections.Generic;
using Acode.Infrastructure.Execution;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Execution;

/// <summary>
/// Tests for sensitive value redaction.
/// </summary>
public class SensitiveRedactorTests
{
    private readonly SensitiveRedactor _redactor;
    
    public SensitiveRedactorTests()
    {
        _redactor = new SensitiveRedactor();
    }
    
    [Theory]
    [InlineData("API_KEY")]
    [InlineData("AWS_SECRET_ACCESS_KEY")]
    [InlineData("GITHUB_TOKEN")]
    [InlineData("DB_PASSWORD")]
    [InlineData("AUTH_CREDENTIAL")]
    public void IsSensitive_BuiltInPatterns_ReturnsTrue(string name)
    {
        // Act
        var result = _redactor.IsSensitive(name);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Theory]
    [InlineData("PATH")]
    [InlineData("HOME")]
    [InlineData("NODE_ENV")]
    [InlineData("DEBUG")]
    public void IsSensitive_NonSensitiveNames_ReturnsFalse(string name)
    {
        // Act
        var result = _redactor.IsSensitive(name);
        
        // Assert
        result.Should().BeFalse();
    }
    
    [Theory]
    [InlineData("api_key")]
    [InlineData("Api_Key")]
    [InlineData("API_KEY")]
    public void IsSensitive_CaseInsensitive_Matches(string name)
    {
        // Act
        var result = _redactor.IsSensitive(name);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public void IsSensitive_CustomPattern_Works()
    {
        // Arrange
        var customRedactor = new SensitiveRedactor(new[] { "^MY_COMPANY_.*" });
        
        // Act
        var result = customRedactor.IsSensitive("MY_COMPANY_SECRET");
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public void RedactValue_ReturnsLengthHint()
    {
        // Arrange
        var secretValue = "super-secret-value-12345";
        
        // Act
        var result = _redactor.RedactValue(secretValue);
        
        // Assert
        result.Should().Be("[REDACTED:24 chars]");
        result.Should().NotContain("super");
    }
    
    [Fact]
    public void RedactValue_EmptyValue_ReturnsEmpty()
    {
        // Act
        var result = _redactor.RedactValue("");
        
        // Assert
        result.Should().Be("[EMPTY]");
    }
    
    [Fact]
    public void RedactEnvironment_MixedSensitivity_CorrectlyRedacts()
    {
        // Arrange
        var env = new Dictionary<string, string?>
        {
            ["PATH"] = "/usr/bin:/bin",
            ["API_KEY"] = "sk-abc123xyz",
            ["DEBUG"] = "true",
            ["AWS_SECRET_ACCESS_KEY"] = "wJalrXUtnFEMI/K7MDENG"
        };
        
        // Act
        var result = _redactor.RedactEnvironment(env);
        
        // Assert
        result["PATH"].Should().Be("/usr/bin:/bin"); // Not redacted
        result["DEBUG"].Should().Be("true"); // Not redacted
        result["API_KEY"].Should().Contain("REDACTED"); // Redacted
        result["AWS_SECRET_ACCESS_KEY"].Should().Contain("REDACTED"); // Redacted
    }
}
```

#### EnvironmentBuilderTests.cs

```csharp
using System;
using System.Collections.Generic;
using Acode.Infrastructure.Execution;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Execution;

/// <summary>
/// Tests for environment building and merging.
/// </summary>
public class EnvironmentBuilderTests
{
    [Fact]
    public void Build_InheritMode_IncludesParentVariables()
    {
        // Arrange
        var builder = new EnvironmentBuilder();
        var parent = new Dictionary<string, string>
        {
            ["PATH"] = "/usr/bin",
            ["HOME"] = "/home/user"
        };
        var specified = new Dictionary<string, string?>
        {
            ["CUSTOM"] = "value"
        };
        
        // Act
        var result = builder.Build(EnvironmentMode.Inherit, parent, specified);
        
        // Assert
        result.Should().ContainKey("PATH");
        result.Should().ContainKey("HOME");
        result.Should().ContainKey("CUSTOM");
    }
    
    [Fact]
    public void Build_InheritMode_AppliesOverrides()
    {
        // Arrange
        var builder = new EnvironmentBuilder();
        var parent = new Dictionary<string, string>
        {
            ["NODE_ENV"] = "development"
        };
        var specified = new Dictionary<string, string?>
        {
            ["NODE_ENV"] = "production"
        };
        
        // Act
        var result = builder.Build(EnvironmentMode.Inherit, parent, specified);
        
        // Assert
        result["NODE_ENV"].Should().Be("production");
    }
    
    [Fact]
    public void Build_ReplaceMode_ExcludesParent()
    {
        // Arrange
        var builder = new EnvironmentBuilder();
        var parent = new Dictionary<string, string>
        {
            ["PATH"] = "/usr/bin",
            ["HOME"] = "/home/user"
        };
        var specified = new Dictionary<string, string?>
        {
            ["ONLY_THIS"] = "value"
        };
        
        // Act
        var result = builder.Build(EnvironmentMode.Replace, parent, specified);
        
        // Assert
        result.Should().NotContainKey("PATH");
        result.Should().NotContainKey("HOME");
        result.Should().ContainKey("ONLY_THIS");
    }
    
    [Fact]
    public void Build_MergeMode_SpecifiedWins()
    {
        // Arrange
        var builder = new EnvironmentBuilder();
        var parent = new Dictionary<string, string>
        {
            ["SHARED"] = "parent-value"
        };
        var specified = new Dictionary<string, string?>
        {
            ["SHARED"] = "specified-value"
        };
        
        // Act
        var result = builder.Build(EnvironmentMode.Merge, parent, specified);
        
        // Assert
        result["SHARED"].Should().Be("specified-value");
    }
    
    [Fact]
    public void Build_NullValue_RemovesVariable()
    {
        // Arrange
        var builder = new EnvironmentBuilder();
        var parent = new Dictionary<string, string>
        {
            ["REMOVE_ME"] = "value"
        };
        var specified = new Dictionary<string, string?>
        {
            ["REMOVE_ME"] = null
        };
        
        // Act
        var result = builder.Build(EnvironmentMode.Inherit, parent, specified);
        
        // Assert
        result.Should().NotContainKey("REMOVE_ME");
    }
}
```

#### PathManagerTests.cs

```csharp
using System;
using System.Runtime.InteropServices;
using Acode.Infrastructure.Execution;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Execution;

/// <summary>
/// Tests for PATH variable management.
/// </summary>
public class PathManagerTests
{
    private readonly PathManager _manager;
    
    public PathManagerTests()
    {
        _manager = new PathManager();
    }
    
    [Fact]
    public void Prepend_AddsToFront()
    {
        // Arrange
        var current = "/usr/bin:/bin";
        var prepend = "/custom/bin";
        
        // Act
        var result = _manager.Prepend(current, prepend);
        
        // Assert
        result.Should().StartWith("/custom/bin");
    }
    
    [Fact]
    public void Append_AddsToEnd()
    {
        // Arrange
        var current = "/usr/bin:/bin";
        var append = "/custom/bin";
        
        // Act
        var result = _manager.Append(current, append);
        
        // Assert
        result.Should().EndWith("/custom/bin");
    }
    
    [Fact]
    public void Prepend_RemovesDuplicates()
    {
        // Arrange
        var current = "/custom/bin:/usr/bin:/bin";
        var prepend = "/custom/bin"; // Already exists
        
        // Act
        var result = _manager.Prepend(current, prepend);
        
        // Assert
        var entries = result.Split(':');
        entries.Where(e => e == "/custom/bin").Should().HaveCount(1);
        entries[0].Should().Be("/custom/bin"); // Still at front
    }
    
    [SkippableFact]
    public void UsesCorrectSeparator_Windows()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        
        // Arrange
        var current = @"C:\Windows\system32;C:\Windows";
        var prepend = @"C:\Tools";
        
        // Act
        var result = _manager.Prepend(current, prepend);
        
        // Assert
        result.Should().Be(@"C:\Tools;C:\Windows\system32;C:\Windows");
    }
    
    [SkippableFact]
    public void UsesCorrectSeparator_Unix()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || 
                   RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
        
        // Arrange
        var current = "/usr/bin:/bin";
        var prepend = "/custom/bin";
        
        // Act
        var result = _manager.Prepend(current, prepend);
        
        // Assert
        result.Should().Be("/custom/bin:/usr/bin:/bin");
    }
    
    [Fact]
    public void RemovesEmptyEntries()
    {
        // Arrange
        var current = "/usr/bin::/bin::";
        var prepend = "/custom/bin";
        
        // Act
        var result = _manager.Prepend(current, prepend);
        
        // Assert
        result.Should().NotContain("::");
    }
}
```

### Integration Tests

```csharp
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Acode.Infrastructure.Execution;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Execution;

/// <summary>
/// Integration tests for working directory and environment.
/// </summary>
[Collection("IntegrationTests")]
public class EnvironmentIntegrationTests
{
    [Fact]
    public async Task Execute_WithWorkingDirectory_RunsInCorrectDir()
    {
        // Arrange
        var workDir = Path.GetTempPath();
        var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "cd"
            : "pwd";
        
        // Act
        var psi = new ProcessStartInfo
        {
            FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd" : "sh",
            Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "/c cd" : "-c pwd",
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        
        using var process = Process.Start(psi);
        var output = await process!.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        // Assert
        output.Trim().Should().Contain(Path.GetFileName(workDir.TrimEnd(Path.DirectorySeparatorChar)));
    }
    
    [Fact]
    public async Task Execute_WithEnvironmentVariable_VariableVisible()
    {
        // Arrange
        var varName = "TEST_VAR_" + Guid.NewGuid().ToString("N")[..8];
        var varValue = "test-value-123";
        
        var psi = new ProcessStartInfo
        {
            FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd" : "sh",
            Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                ? $"/c echo %{varName}%" 
                : $"-c 'echo ${varName}'",
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        psi.Environment[varName] = varValue;
        
        // Act
        using var process = Process.Start(psi);
        var output = await process!.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        // Assert
        output.Trim().Should().Be(varValue);
    }
}
```

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