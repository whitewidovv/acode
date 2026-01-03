# Task 002.c: Define Command Groups (setup/build/test/lint/format/start)

**Priority:** 11 / 49  
**Tier:** Foundation  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 0 — Foundation  
**Dependencies:** Task 002.a (schema defined), Task 002.b (parser implemented)  

---

## Description

### Overview

Task 002.c defines the command groups that enable Acode to interact with any repository's development workflow. Command groups are standardized categories of shell commands that Acode uses to set up, build, test, lint, format, and run projects. By defining these groups in `.agent/config.yml`, developers give Acode the knowledge it needs to work effectively with their codebase.

The six command groups—setup, build, test, lint, format, and start—cover the essential lifecycle operations for any software project. Each group has specific semantics, expected behaviors, and exit code interpretations that Acode uses to understand command success or failure.

### Business Value

Command groups provide:

1. **Universal Project Support** — Works with any language, framework, or build system
2. **Consistent Interface** — Same Acode commands work across all projects
3. **Automation Foundation** — Enables automated workflows and CI integration
4. **Developer Ergonomics** — Reduces cognitive load by standardizing operations
5. **Agent Intelligence** — Acode understands what each command does, not just how to run it
6. **Error Recovery** — Semantic understanding enables intelligent retry and fallback

### Scope Boundaries

**In Scope:**
- Definition of six command groups (setup, build, test, lint, format, start)
- Command specification formats (string, array, object)
- Command execution semantics and exit codes
- Working directory handling
- Environment variable passing
- Timeout and retry configuration
- Output capture and parsing
- Error classification and handling
- Command chaining and dependencies
- Platform-specific command variants

**Out of Scope:**
- Command execution implementation (Epic 2)
- Shell selection implementation
- Output streaming implementation
- Parallel execution implementation
- Command result caching

### Integration Points

| Task | Relationship | Description |
|------|--------------|-------------|
| Task 002 | Parent | Defines config structure |
| Task 002.a | Producer | Command schema |
| Task 002.b | Producer | Command parsing |
| Epic 2 | Consumer | Command execution |
| Epic 3 | Consumer | Agent uses commands |
| Epic 8 | Consumer | CI/CD commands |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Command not defined | Operation blocked | Clear error, suggest default |
| Command fails | Workflow blocked | Retry logic, clear error |
| Timeout exceeded | Hung process | Configurable timeout, kill |
| Wrong working directory | Wrong output | Validate paths exist |
| Platform incompatibility | Command fails | Platform variants |

### Assumptions

1. Shell is available on all target platforms
2. Commands are executed in repository context
3. Exit code 0 means success
4. Commands are idempotent or at least safe to retry
5. Output is UTF-8 encoded

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| **Command Group** | Category of related commands (setup, build, etc.) |
| **Setup** | Commands to initialize development environment |
| **Build** | Commands to compile/bundle the project |
| **Test** | Commands to run test suites |
| **Lint** | Commands to check code quality |
| **Format** | Commands to auto-format code |
| **Start** | Commands to run the application |
| **Command String** | Single command as a string |
| **Command Array** | Multiple commands in sequence |
| **Command Object** | Command with full options |
| **Exit Code** | Numeric status returned by command |
| **Working Directory** | Directory where command executes |
| **Timeout** | Maximum command execution time |
| **Retry** | Automatic re-execution on failure |
| **Platform Variant** | OS-specific command version |
| **Shell** | Command interpreter (bash, cmd, pwsh) |
| **Streaming** | Real-time output capture |
| **Idempotent** | Safe to run multiple times |

---

## Out of Scope

- Shell implementation and selection logic
- Process spawning and management
- Output streaming to terminals
- Parallel command execution
- Command result caching
- Interactive command support
- Sudo/elevation handling
- Container/Docker execution
- Remote command execution
- Command history tracking
- Command undo/rollback
- Custom command groups
- Command aliases
- Pipeline/workflow definition

---

## Functional Requirements

### Command Group Definitions (FR-002c-01 to FR-002c-30)

### Storage configuration (NEW)

Add support in `.agent/config.yml` for a `storage:` block.

Minimum keys:
- `storage.mode`: `local_cache_only` | `offline_first_sync` | `remote_required`
- `storage.local.type`: `sqlite`
- `storage.local.sqlite_path`: path (default: `.acode/workspace.db`)
- `storage.remote.type`: `postgres`
- `storage.remote.postgres.dsn`: secret reference or DSN (MUST support env var indirection)
- `storage.sync.enabled`: bool (default true for offline_first_sync)
- `storage.sync.batch_size`: int
- `storage.sync.retry_policy`: (max_attempts, backoff)
- `storage.sync.conflict_policy`: `lww` | `reject` (default `lww` for metadata; append-only for messages/events)

Notes:
- This MUST integrate with Task 050 (DB foundation) and Task 049.f (sync engine).
- Secret material MUST NOT be stored directly in config; use env vars or secret references.

| ID | Requirement |
|----|-------------|
| FR-002c-01 | System MUST support exactly six command groups |
| FR-002c-02 | Command groups MUST be: setup, build, test, lint, format, start |
| FR-002c-03 | All command groups MUST be optional in config |
| FR-002c-04 | Missing command group MUST NOT cause startup failure |
| FR-002c-05 | Missing command group MUST cause clear error when invoked |
| FR-002c-06 | Each command group MUST have defined semantics |
| FR-002c-07 | setup MUST initialize development environment |
| FR-002c-08 | setup MUST include dependency installation |
| FR-002c-09 | setup MUST be safe to run multiple times |
| FR-002c-10 | build MUST compile or bundle the project |
| FR-002c-11 | build MUST produce deployment artifacts |
| FR-002c-12 | build MUST support incremental builds where possible |
| FR-002c-13 | test MUST run the test suite |
| FR-002c-14 | test MUST return non-zero on any test failure |
| FR-002c-15 | test MUST support test filtering (passed via args) |
| FR-002c-16 | lint MUST check code quality and style |
| FR-002c-17 | lint MUST return non-zero on any violation |
| FR-002c-18 | lint MUST NOT modify files |
| FR-002c-19 | format MUST auto-format source code |
| FR-002c-20 | format MUST modify files in place |
| FR-002c-21 | format MUST be idempotent |
| FR-002c-22 | start MUST run the application |
| FR-002c-23 | start MUST support long-running processes |
| FR-002c-24 | start MUST be terminable via signal |
| FR-002c-25 | All groups MUST log execution start |
| FR-002c-26 | All groups MUST log execution end |
| FR-002c-27 | All groups MUST log exit code |
| FR-002c-28 | All groups MUST capture stdout |
| FR-002c-29 | All groups MUST capture stderr |
| FR-002c-30 | All groups MUST respect timeout configuration |

### Command Specification Formats (FR-002c-31 to FR-002c-50)

| ID | Requirement |
|----|-------------|
| FR-002c-31 | Commands MUST support string format |
| FR-002c-32 | String format MUST be single shell command |
| FR-002c-33 | Commands MUST support array format |
| FR-002c-34 | Array format MUST execute commands in sequence |
| FR-002c-35 | Array format MUST stop on first failure |
| FR-002c-36 | Commands MUST support object format |
| FR-002c-37 | Object format MUST have "run" property for command |
| FR-002c-38 | Object format MUST have optional "cwd" property |
| FR-002c-39 | Object format MUST have optional "env" property |
| FR-002c-40 | Object format MUST have optional "timeout" property |
| FR-002c-41 | Object format MUST have optional "retry" property |
| FR-002c-42 | Object format MUST have optional "continue_on_error" property |
| FR-002c-43 | Object format MUST have optional "platforms" property |
| FR-002c-44 | Mixed formats MUST be supported in arrays |
| FR-002c-45 | Empty string MUST be rejected |
| FR-002c-46 | Empty array MUST be allowed (no-op) |
| FR-002c-47 | Whitespace-only string MUST be rejected |
| FR-002c-48 | Commands MUST be trimmed before execution |
| FR-002c-49 | Multi-line strings MUST be supported |
| FR-002c-50 | Command parsing MUST preserve arguments |

### Working Directory Handling (FR-002c-51 to FR-002c-65)

| ID | Requirement |
|----|-------------|
| FR-002c-51 | Default working directory MUST be repository root |
| FR-002c-52 | Working directory MUST be configurable per command |
| FR-002c-53 | Working directory MUST be relative to repository root |
| FR-002c-54 | Absolute paths MUST be rejected |
| FR-002c-55 | Path traversal (../) MUST be rejected |
| FR-002c-56 | Non-existent directory MUST cause clear error |
| FR-002c-57 | Working directory MUST be logged |
| FR-002c-58 | Working directory MUST support environment variables |
| FR-002c-59 | Working directory MUST be normalized (/ on all platforms) |
| FR-002c-60 | Working directory MUST be validated before execution |
| FR-002c-61 | Symlink directories MUST be followed |
| FR-002c-62 | Circular symlinks MUST be detected |
| FR-002c-63 | Working directory MUST be created if missing (for output dirs) |
| FR-002c-64 | Working directory creation MUST be explicit (option) |
| FR-002c-65 | Working directory validation MUST be fast (<10ms) |

### Environment Variables (FR-002c-66 to FR-002c-80)

| ID | Requirement |
|----|-------------|
| FR-002c-66 | Commands MUST inherit process environment |
| FR-002c-67 | Commands MUST support additional env vars |
| FR-002c-68 | Additional env vars MUST override inherited |
| FR-002c-69 | Env var names MUST be validated |
| FR-002c-70 | Env var values MUST be strings |
| FR-002c-71 | Env vars MUST support interpolation from config |
| FR-002c-72 | Sensitive env vars MUST be redacted in logs |
| FR-002c-73 | ACODE_* env vars MUST be set by Acode |
| FR-002c-74 | ACODE_MODE MUST contain current mode |
| FR-002c-75 | ACODE_ROOT MUST contain repository root |
| FR-002c-76 | ACODE_COMMAND MUST contain command group name |
| FR-002c-77 | PATH MUST include repository tool paths |
| FR-002c-78 | Env vars MUST NOT be modified after command starts |
| FR-002c-79 | Env var count MUST be logged |
| FR-002c-80 | Empty env var value MUST be allowed |

### Exit Code Handling (FR-002c-81 to FR-002c-95)

| ID | Requirement |
|----|-------------|
| FR-002c-81 | Exit code 0 MUST indicate success |
| FR-002c-82 | Exit code non-zero MUST indicate failure |
| FR-002c-83 | Exit code MUST be logged |
| FR-002c-84 | Exit code MUST be returned to caller |
| FR-002c-85 | Exit code MUST be preserved (not normalized) |
| FR-002c-86 | Signal termination MUST return 128 + signal |
| FR-002c-87 | Timeout MUST return exit code 124 |
| FR-002c-88 | Exit code MUST be included in error message |
| FR-002c-89 | Common exit codes MUST have descriptive messages |
| FR-002c-90 | Exit code 1 MUST be "General error" |
| FR-002c-91 | Exit code 2 MUST be "Misuse of command" |
| FR-002c-92 | Exit code 126 MUST be "Command not executable" |
| FR-002c-93 | Exit code 127 MUST be "Command not found" |
| FR-002c-94 | Exit code 130 MUST be "Interrupted (Ctrl+C)" |
| FR-002c-95 | Exit code mapping MUST be extensible |

### Timeout and Retry (FR-002c-96 to FR-002c-110)

| ID | Requirement |
|----|-------------|
| FR-002c-96 | All commands MUST have timeout |
| FR-002c-97 | Default timeout MUST be 300 seconds (5 minutes) |
| FR-002c-98 | Timeout MUST be configurable per command group |
| FR-002c-99 | Timeout MUST be configurable per command |
| FR-002c-100 | Timeout of 0 MUST mean no timeout |
| FR-002c-101 | Timeout exceeded MUST kill process tree |
| FR-002c-102 | Timeout MUST log warning before killing |
| FR-002c-103 | Retry count MUST be configurable |
| FR-002c-104 | Default retry count MUST be 0 (no retry) |
| FR-002c-105 | Retry MUST use exponential backoff |
| FR-002c-106 | Retry base delay MUST be 1 second |
| FR-002c-107 | Retry max delay MUST be 30 seconds |
| FR-002c-108 | Retry attempts MUST be logged |
| FR-002c-109 | Final retry failure MUST return last exit code |
| FR-002c-110 | Retry MUST respect total timeout budget |

### Platform Variants (FR-002c-111 to FR-002c-120)

| ID | Requirement |
|----|-------------|
| FR-002c-111 | Commands MUST support platform-specific variants |
| FR-002c-112 | Platform identifiers MUST be: windows, linux, macos |
| FR-002c-113 | Current platform MUST be detected automatically |
| FR-002c-114 | Platform variant MUST override default command |
| FR-002c-115 | Missing platform variant MUST use default |
| FR-002c-116 | Platform MUST be logged |
| FR-002c-117 | Platform detection MUST be deterministic |
| FR-002c-118 | Cross-platform commands SHOULD use default only |
| FR-002c-119 | Platform-specific shells MUST be used |
| FR-002c-120 | Windows MUST use cmd.exe or pwsh.exe |

---

## Non-Functional Requirements

### Security (NFR-002c-01 to NFR-002c-15)

| ID | Requirement |
|----|-------------|
| NFR-002c-01 | Commands MUST NOT execute in elevated/root context |
| NFR-002c-02 | Commands MUST NOT access paths outside repository |
| NFR-002c-03 | Shell injection MUST be prevented |
| NFR-002c-04 | Environment variables MUST be sanitized |
| NFR-002c-05 | Sensitive env vars MUST be redacted in logs |
| NFR-002c-06 | Command output MUST be scanned for secrets |
| NFR-002c-07 | Secrets in output MUST be redacted before storage |
| NFR-002c-08 | Commands MUST NOT modify Acode installation |
| NFR-002c-09 | Commands MUST NOT access network in Airgapped mode |
| NFR-002c-10 | Process isolation MUST be maintained |
| NFR-002c-11 | Child processes MUST be tracked |
| NFR-002c-12 | Orphan processes MUST be killed on timeout |
| NFR-002c-13 | Temp files MUST be cleaned up |
| NFR-002c-14 | File permissions MUST NOT be elevated |
| NFR-002c-15 | Commands MUST run as current user |

### Performance (NFR-002c-16 to NFR-002c-28)

| ID | Requirement |
|----|-------------|
| NFR-002c-16 | Command startup overhead MUST be under 100ms |
| NFR-002c-17 | Output capture MUST NOT add significant latency |
| NFR-002c-18 | Output buffer MUST be bounded (10MB default) |
| NFR-002c-19 | Large output MUST be truncated with warning |
| NFR-002c-20 | Memory usage MUST be proportional to output size |
| NFR-002c-21 | Process tree kill MUST complete in under 5 seconds |
| NFR-002c-22 | Command parsing MUST be under 10ms |
| NFR-002c-23 | Platform detection MUST be under 1ms |
| NFR-002c-24 | Working directory validation MUST be under 10ms |
| NFR-002c-25 | Environment setup MUST be under 50ms |
| NFR-002c-26 | Multiple commands MUST NOT leak resources |
| NFR-002c-27 | Output streaming MUST have under 100ms latency |
| NFR-002c-28 | Parallel command execution MUST scale linearly |

### Reliability (NFR-002c-29 to NFR-002c-40)

| ID | Requirement |
|----|-------------|
| NFR-002c-29 | Command failure MUST NOT crash Acode |
| NFR-002c-30 | Process crash MUST be detected |
| NFR-002c-31 | Process hang MUST be detected via timeout |
| NFR-002c-32 | Output corruption MUST be handled |
| NFR-002c-33 | Encoding errors MUST be handled |
| NFR-002c-34 | Disk full MUST be reported clearly |
| NFR-002c-35 | Permission denied MUST be reported clearly |
| NFR-002c-36 | Command not found MUST be reported clearly |
| NFR-002c-37 | Interrupted commands MUST clean up |
| NFR-002c-38 | Concurrent commands MUST be isolated |
| NFR-002c-39 | State MUST be consistent after failure |
| NFR-002c-40 | Recovery from failure MUST be automatic |

---

## User Manual Documentation

### Command Groups Overview

Acode uses six standard command groups to interact with your project:

| Group | Purpose | Typical Commands |
|-------|---------|------------------|
| **setup** | Initialize development environment | `npm install`, `dotnet restore` |
| **build** | Compile or bundle project | `npm run build`, `dotnet build` |
| **test** | Run test suite | `npm test`, `dotnet test` |
| **lint** | Check code quality | `npm run lint`, `dotnet format --verify-no-changes` |
| **format** | Auto-format code | `npm run format`, `dotnet format` |
| **start** | Run the application | `npm start`, `dotnet run` |

### Configuration Syntax

#### Simple String Format

```yaml
commands:
  setup: npm install
  build: npm run build
  test: npm test
  lint: npm run lint
  format: npm run format
  start: npm start
```

#### Array Format (Multiple Commands)

```yaml
commands:
  setup:
    - npm install
    - npm run postinstall
  build:
    - npm run clean
    - npm run build
```

#### Object Format (Full Options)

```yaml
commands:
  build:
    run: npm run build
    cwd: src/frontend
    env:
      NODE_ENV: production
    timeout: 600
    retry: 2
```

#### Mixed Format

```yaml
commands:
  setup:
    - npm install                          # string
    - run: npm run generate                # object
      timeout: 120
    - npm run postinstall                  # string
```

### Command Object Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `run` | string | (required) | The shell command to execute |
| `cwd` | string | `.` | Working directory (relative to repo root) |
| `env` | object | `{}` | Additional environment variables |
| `timeout` | integer | 300 | Timeout in seconds (0 = no timeout) |
| `retry` | integer | 0 | Number of retry attempts |
| `continue_on_error` | boolean | false | Continue sequence on failure |
| `platforms` | object | null | Platform-specific variants |

### Platform-Specific Commands

```yaml
commands:
  build:
    run: make build
    platforms:
      windows: msbuild /p:Configuration=Release
      linux: make build
      macos: make build
```

### Environment Variables

#### Acode-Provided Variables

| Variable | Description |
|----------|-------------|
| `ACODE_MODE` | Current operating mode (local-only, burst, airgapped) |
| `ACODE_ROOT` | Absolute path to repository root |
| `ACODE_COMMAND` | Current command group (setup, build, etc.) |
| `ACODE_ATTEMPT` | Current retry attempt (1-based) |

#### Custom Variables

```yaml
commands:
  build:
    run: npm run build
    env:
      NODE_ENV: production
      BUILD_NUMBER: ${BUILD_NUMBER:-0}
```

### Timeouts and Retries

#### Timeout Configuration

```yaml
commands:
  build:
    run: npm run build
    timeout: 600  # 10 minutes

  test:
    run: npm test
    timeout: 0  # No timeout (use for long test suites)
```

#### Retry Configuration

```yaml
commands:
  setup:
    run: npm install
    retry: 3  # Retry up to 3 times on failure
```

Retry uses exponential backoff: 1s, 2s, 4s, 8s, ... (max 30s).

### Exit Codes

| Code | Meaning | Acode Behavior |
|------|---------|----------------|
| 0 | Success | Continue, report success |
| 1 | General error | Stop, report failure |
| 2 | Misuse | Stop, check command syntax |
| 124 | Timeout | Process killed, report timeout |
| 126 | Not executable | Stop, check file permissions |
| 127 | Not found | Stop, check command exists |
| 130 | Interrupted | User cancelled (Ctrl+C) |

### CLI Usage

```bash
# Run a command group
acode run setup
acode run build
acode run test
acode run lint
acode run format
acode run start

# Run with options
acode run build --timeout 600
acode run test --retry 2
acode run start --background

# Run multiple groups
acode run setup build test

# Check command definition
acode config show commands.build
```

### Best Practices

1. **Keep commands idempotent** — Running `setup` twice should be safe
2. **Use explicit commands** — Avoid complex shell scripting in config
3. **Set appropriate timeouts** — Don't let hung processes block workflows
4. **Use retry sparingly** — Only for transient failures (network, etc.)
5. **Test on all platforms** — Use platform variants for compatibility
6. **Log build artifacts** — Ensure build outputs are in known locations

### Troubleshooting

#### "Command not found"

```bash
# Check if command exists
which npm  # Linux/macOS
where npm  # Windows

# Check PATH in config
acode config show env
```

#### "Permission denied"

```bash
# Check file permissions
ls -la ./scripts/build.sh

# Make executable
chmod +x ./scripts/build.sh
```

#### "Timeout exceeded"

```yaml
# Increase timeout
commands:
  build:
    run: npm run build
    timeout: 1200  # 20 minutes
```

#### "Command failed with exit code X"

```bash
# Run command manually to see full output
npm run build

# Check Acode logs for captured output
acode logs show --last
```

### FAQ

**Q: Can I define custom command groups?**
A: Not currently. The six standard groups cover most workflows. Custom commands can be run via shell.

**Q: How do I run commands in parallel?**
A: Parallel execution is not yet supported in config. Use shell parallelization (`command1 & command2`).

**Q: Can I use shell features like pipes?**
A: Yes, commands are executed via shell, so pipes, redirects, and other features work.

**Q: How do I pass arguments to commands?**
A: Use `acode run test -- --filter "MyTest"` to pass additional arguments.

**Q: What shell is used?**
A: On Unix, `/bin/sh`. On Windows, `cmd.exe` (or `pwsh.exe` if configured).

---

## Acceptance Criteria / Definition of Done

### Command Group Definitions (30 items)

- [ ] Six command groups defined: setup, build, test, lint, format, start
- [ ] All command groups are optional in config
- [ ] Missing group returns clear error when invoked
- [ ] setup group installs dependencies
- [ ] setup group is idempotent
- [ ] build group compiles/bundles project
- [ ] build group produces artifacts
- [ ] test group runs test suite
- [ ] test group returns non-zero on failure
- [ ] lint group checks code quality
- [ ] lint group does not modify files
- [ ] format group auto-formats code
- [ ] format group is idempotent
- [ ] start group runs application
- [ ] start group supports long-running processes
- [ ] All groups log execution start
- [ ] All groups log execution end
- [ ] All groups log exit code
- [ ] All groups capture stdout
- [ ] All groups capture stderr
- [ ] All groups respect timeout
- [ ] Groups have clear semantics documented
- [ ] Group names are case-insensitive
- [ ] Group aliases not supported (explicit)
- [ ] Groups are independent (no implicit dependencies)
- [ ] Group execution is synchronous by default
- [ ] Group execution order is deterministic
- [ ] Group re-execution is allowed
- [ ] Group status is queryable
- [ ] Group history is logged

### Command Specification Formats (25 items)

- [ ] String format supported
- [ ] String format executes single command
- [ ] Array format supported
- [ ] Array format executes commands in sequence
- [ ] Array format stops on first failure
- [ ] Object format supported
- [ ] Object format requires "run" property
- [ ] Object format supports "cwd" property
- [ ] Object format supports "env" property
- [ ] Object format supports "timeout" property
- [ ] Object format supports "retry" property
- [ ] Object format supports "continue_on_error" property
- [ ] Object format supports "platforms" property
- [ ] Mixed formats supported in arrays
- [ ] Empty string rejected
- [ ] Empty array allowed (no-op)
- [ ] Whitespace-only string rejected
- [ ] Commands trimmed before execution
- [ ] Multi-line strings supported
- [ ] Arguments preserved in parsing
- [ ] Quotes handled correctly
- [ ] Escape sequences handled
- [ ] Command validation at load time
- [ ] Invalid format rejected with clear error
- [ ] Format examples documented

### Working Directory Handling (20 items)

- [ ] Default is repository root
- [ ] Configurable per command
- [ ] Relative to repository root
- [ ] Absolute paths rejected
- [ ] Path traversal (../) rejected
- [ ] Non-existent directory causes error
- [ ] Working directory logged
- [ ] Environment variables supported in path
- [ ] Paths normalized (forward slashes)
- [ ] Validated before execution
- [ ] Symlinks followed
- [ ] Circular symlinks detected
- [ ] Directory creation optional
- [ ] Creation requires explicit flag
- [ ] Validation under 10ms
- [ ] Path too long handled
- [ ] Unicode paths supported
- [ ] Spaces in path supported
- [ ] Special characters handled
- [ ] Path casing handled per platform

### Environment Variables (20 items)

- [ ] Process environment inherited
- [ ] Additional env vars supported
- [ ] Additional vars override inherited
- [ ] Env var names validated
- [ ] Env var values are strings
- [ ] Interpolation from config works
- [ ] Sensitive vars redacted in logs
- [ ] ACODE_MODE set
- [ ] ACODE_ROOT set
- [ ] ACODE_COMMAND set
- [ ] PATH includes tool paths
- [ ] Env vars immutable during execution
- [ ] Env var count logged
- [ ] Empty value allowed
- [ ] Null value rejected
- [ ] Env vars documented
- [ ] Common vars documented
- [ ] Custom vars documented
- [ ] Env var conflicts handled
- [ ] Env var encoding is UTF-8

### Exit Code Handling (20 items)

- [ ] Exit code 0 is success
- [ ] Non-zero is failure
- [ ] Exit code logged
- [ ] Exit code returned to caller
- [ ] Exit code preserved (not normalized)
- [ ] Signal termination returns 128 + signal
- [ ] Timeout returns 124
- [ ] Exit code in error message
- [ ] Common codes have descriptions
- [ ] Exit code 1 is "General error"
- [ ] Exit code 2 is "Misuse"
- [ ] Exit code 126 is "Not executable"
- [ ] Exit code 127 is "Not found"
- [ ] Exit code 130 is "Interrupted"
- [ ] Exit code mapping extensible
- [ ] Exit codes documented
- [ ] Exit code 0 skips retry
- [ ] All codes handled
- [ ] Negative codes handled (Windows)
- [ ] Exit code history available

### Timeout and Retry (20 items)

- [ ] All commands have timeout
- [ ] Default timeout is 300 seconds
- [ ] Timeout configurable per group
- [ ] Timeout configurable per command
- [ ] Timeout 0 means no timeout
- [ ] Timeout kills process tree
- [ ] Timeout logs warning first
- [ ] Retry count configurable
- [ ] Default retry is 0
- [ ] Retry uses exponential backoff
- [ ] Base delay is 1 second
- [ ] Max delay is 30 seconds
- [ ] Retry attempts logged
- [ ] Final failure returns last exit code
- [ ] Retry respects timeout budget
- [ ] Retry only on failure (not success)
- [ ] Retry logged with attempt number
- [ ] Retry backoff documented
- [ ] Max retry count is 10
- [ ] Retry can be disabled

### Platform Variants (15 items)

- [ ] Platform variants supported
- [ ] Platform identifiers: windows, linux, macos
- [ ] Platform auto-detected
- [ ] Variant overrides default
- [ ] Missing variant uses default
- [ ] Platform logged
- [ ] Detection deterministic
- [ ] Cross-platform uses default
- [ ] Windows uses cmd.exe
- [ ] Unix uses /bin/sh
- [ ] Shell configurable
- [ ] Platform documented
- [ ] Platform examples provided
- [ ] Invalid platform rejected
- [ ] Platform case-insensitive

### Security (20 items)

- [ ] No elevated/root execution
- [ ] No access outside repository
- [ ] Shell injection prevented
- [ ] Environment sanitized
- [ ] Sensitive vars redacted
- [ ] Output scanned for secrets
- [ ] Secrets redacted in storage
- [ ] No Acode modification
- [ ] No network in Airgapped mode
- [ ] Process isolation maintained
- [ ] Child processes tracked
- [ ] Orphans killed on timeout
- [ ] Temp files cleaned up
- [ ] Permissions not elevated
- [ ] Runs as current user
- [ ] Security audit documented
- [ ] Attack vectors documented
- [ ] Mitigations documented
- [ ] Security tests exist
- [ ] Penetration test passed

### Performance (15 items)

- [ ] Startup overhead under 100ms
- [ ] Output capture low latency
- [ ] Output buffer bounded (10MB)
- [ ] Large output truncated
- [ ] Memory proportional to output
- [ ] Process kill under 5 seconds
- [ ] Command parsing under 10ms
- [ ] Platform detection under 1ms
- [ ] Directory validation under 10ms
- [ ] Environment setup under 50ms
- [ ] No resource leaks
- [ ] Streaming under 100ms latency
- [ ] Parallel scales linearly
- [ ] Benchmarks documented
- [ ] Performance tests exist

---
## Testing Requirements

### Unit Tests

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| UT-002c-01 | Parse string command | Returns CommandSpec |
| UT-002c-02 | Parse array command | Returns CommandSpec[] |
| UT-002c-03 | Parse object command | Returns CommandSpec with options |
| UT-002c-04 | Parse mixed array | Returns mixed CommandSpec[] |
| UT-002c-05 | Reject empty string | Returns validation error |
| UT-002c-06 | Accept empty array | Returns empty CommandSpec[] |
| UT-002c-07 | Reject whitespace-only | Returns validation error |
| UT-002c-08 | Trim command string | Whitespace removed |
| UT-002c-09 | Preserve multi-line | Lines preserved |
| UT-002c-10 | Validate working directory | Path validated |
| UT-002c-11 | Reject absolute path | Returns error |
| UT-002c-12 | Reject path traversal | Returns error |
| UT-002c-13 | Parse environment variables | Env vars extracted |
| UT-002c-14 | Validate timeout value | Positive integer required |
| UT-002c-15 | Validate retry value | Non-negative integer required |
| UT-002c-16 | Detect platform variants | Correct platform selected |
| UT-002c-17 | Fall back to default | No variant uses default |
| UT-002c-18 | Exit code mapping | Codes mapped to messages |
| UT-002c-19 | Timeout to exit code 124 | Correct exit code |
| UT-002c-20 | Calculate backoff delay | Exponential backoff correct |
| UT-002c-21 | Command equality | Equal specs are equal |
| UT-002c-22 | Command serialization | Serializes to JSON |
| UT-002c-23 | All groups parseable | All six groups parse |
| UT-002c-24 | Missing group handled | Returns null/error appropriately |
| UT-002c-25 | Command validation | Invalid commands rejected |

### Integration Tests

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| IT-002c-01 | Load config with all command groups | All groups accessible |
| IT-002c-02 | Execute simple command | Exit code 0 |
| IT-002c-03 | Execute failing command | Non-zero exit code |
| IT-002c-04 | Execute command with timeout | Timeout kills process |
| IT-002c-05 | Execute command with retry | Retry attempts logged |
| IT-002c-06 | Execute command with env vars | Vars available in process |
| IT-002c-07 | Execute command with cwd | Correct working directory |
| IT-002c-08 | Execute command on Windows | cmd.exe used |
| IT-002c-09 | Execute command on Linux | /bin/sh used |
| IT-002c-10 | Execute array of commands | Sequence executed |
| IT-002c-11 | Array stops on failure | Remaining commands skipped |
| IT-002c-12 | Continue on error | Sequence continues |
| IT-002c-13 | Platform variant selected | Correct variant used |
| IT-002c-14 | Output captured | stdout/stderr captured |
| IT-002c-15 | Large output handled | Truncated with warning |

### End-to-End Tests

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| E2E-002c-01 | acode run setup | Runs setup commands |
| E2E-002c-02 | acode run build | Runs build commands |
| E2E-002c-03 | acode run test | Runs test commands |
| E2E-002c-04 | acode run lint | Runs lint commands |
| E2E-002c-05 | acode run format | Runs format commands |
| E2E-002c-06 | acode run start | Runs start command |
| E2E-002c-07 | acode run (missing group) | Clear error message |
| E2E-002c-08 | acode run setup build test | Runs sequence |
| E2E-002c-09 | acode run test --timeout 60 | Timeout overridden |
| E2E-002c-10 | acode run test -- --filter X | Arguments passed |
| E2E-002c-11 | acode run start --background | Process backgrounded |
| E2E-002c-12 | acode config show commands | Shows command config |

### Performance / Benchmarks

| ID | Benchmark | Target | Measurement Method |
|----|-----------|--------|-------------------|
| PERF-002c-01 | Command startup overhead | < 100ms | Stopwatch |
| PERF-002c-02 | Command parsing | < 10ms | Stopwatch, 1000 iterations |
| PERF-002c-03 | Platform detection | < 1ms | Stopwatch, 10000 iterations |
| PERF-002c-04 | Directory validation | < 10ms | Stopwatch, 1000 iterations |
| PERF-002c-05 | Environment setup | < 50ms | Stopwatch, 100 iterations |
| PERF-002c-06 | Process kill (tree) | < 5s | Stopwatch |
| PERF-002c-07 | Output streaming latency | < 100ms | End-to-end measurement |
| PERF-002c-08 | Memory per command | < 10MB | Memory profiler |

### Regression / Impacted Areas

| Area | Impact | Regression Test |
|------|--------|-----------------|
| Config loading | Command parsing | Commands parse correctly |
| CLI | Run command | All groups executable |
| Mode enforcement | Network in commands | Airgapped blocks network |
| Logging | Command logging | All events logged |
| Error handling | Command failures | Errors reported correctly |
| Process management | Timeouts | Processes killed |
| Platform support | Platform variants | Variants selected correctly |

---

## User Verification Steps

### Scenario 1: Simple String Command
1. Create config with `commands: { build: "echo hello" }`
2. Run `acode run build`
3. **Verify:** Output shows "hello"
4. **Verify:** Exit code is 0

### Scenario 2: Array of Commands
1. Create config with `commands: { setup: ["echo first", "echo second"] }`
2. Run `acode run setup`
3. **Verify:** Output shows "first" then "second"
4. **Verify:** Both commands executed

### Scenario 3: Array Stops on Failure
1. Create config with `commands: { test: ["exit 1", "echo never"] }`
2. Run `acode run test`
3. **Verify:** Exit code is 1
4. **Verify:** "never" not in output

### Scenario 4: Continue on Error
1. Create config with array including `{ run: "exit 1", continue_on_error: true }`
2. Run command group
3. **Verify:** Subsequent commands still run

### Scenario 5: Custom Working Directory
1. Create config with `commands: { build: { run: "pwd", cwd: "src" } }`
2. Run `acode run build`
3. **Verify:** Output shows `*/src` path

### Scenario 6: Environment Variables
1. Create config with `commands: { test: { run: "echo $MY_VAR", env: { MY_VAR: "hello" } } }`
2. Run `acode run test`
3. **Verify:** Output shows "hello"

### Scenario 7: Acode Environment Variables
1. Create any command
2. Run with `echo $ACODE_MODE`
3. **Verify:** Mode is displayed (local-only, etc.)

### Scenario 8: Timeout
1. Create config with `commands: { test: { run: "sleep 60", timeout: 2 } }`
2. Run `acode run test`
3. **Verify:** Command killed after ~2 seconds
4. **Verify:** Exit code is 124

### Scenario 9: Retry on Failure
1. Create config with `commands: { setup: { run: "exit 1", retry: 2 } }`
2. Run `acode run setup`
3. **Verify:** Output shows retry attempts
4. **Verify:** Total of 3 attempts (1 + 2 retries)

### Scenario 10: Platform Variant
1. Create config with platform-specific commands
2. Run on current platform
3. **Verify:** Correct variant executed

### Scenario 11: Missing Command Group
1. Remove "build" from commands section
2. Run `acode run build`
3. **Verify:** Clear error message about missing group

### Scenario 12: Pass Arguments
1. Create config with `commands: { test: "npm test" }`
2. Run `acode run test -- --watch`
3. **Verify:** `npm test --watch` executed

### Scenario 13: Background Process
1. Create config with `commands: { start: "sleep 100" }`
2. Run `acode run start --background`
3. **Verify:** Command returns immediately
4. **Verify:** Process running in background

### Scenario 14: Command Output Capture
1. Run any command
2. Run `acode logs show --last`
3. **Verify:** stdout and stderr captured

### Scenario 15: Long Output Truncation
1. Create command that outputs >10MB
2. Run command
3. **Verify:** Output truncated with warning

---

## Implementation Prompt for Claude

### Objective

Define the command group specifications for Acode's `.agent/config.yml` file. This defines how commands are specified, parsed, and what semantics each group has.

### Architecture Constraints

- **Clean Architecture:** Domain models in Domain layer
- **Immutability:** Command specifications are immutable
- **Validation:** All commands validated at config load time

### File Structure

```
src/
├── Acode.Domain/
│   └── Commands/
│       ├── CommandGroup.cs           # Enum for groups
│       ├── CommandSpec.cs            # Command specification
│       ├── CommandOptions.cs         # Execution options
│       ├── CommandResult.cs          # Execution result
│       ├── ExitCodeDescriptions.cs   # Exit code mappings
│       └── PlatformVariant.cs        # Platform-specific
├── Acode.Application/
│   └── Commands/
│       ├── ICommandParser.cs
│       ├── ICommandExecutor.cs
│       ├── CommandParser.cs
│       ├── CommandValidator.cs
│       ├── RetryPolicy.cs
│       └── TimeoutPolicy.cs
└── data/
    └── exit-codes.json              # Exit code descriptions
```

### Interface Contracts

```csharp
// CommandGroup.cs
public enum CommandGroup
{
    Setup,
    Build,
    Test,
    Lint,
    Format,
    Start
}

// CommandSpec.cs
public sealed record CommandSpec
{
    public required string Run { get; init; }
    public string WorkingDirectory { get; init; } = ".";
    public IReadOnlyDictionary<string, string> Environment { get; init; } = 
        new Dictionary<string, string>();
    public int TimeoutSeconds { get; init; } = 300;
    public int RetryCount { get; init; } = 0;
    public bool ContinueOnError { get; init; } = false;
    public IReadOnlyDictionary<string, string>? PlatformVariants { get; init; }
}

// CommandResult.cs
public sealed record CommandResult
{
    public required int ExitCode { get; init; }
    public required string Stdout { get; init; }
    public required string Stderr { get; init; }
    public required TimeSpan Duration { get; init; }
    public required bool TimedOut { get; init; }
    public required int AttemptCount { get; init; }
    public bool Success => ExitCode == 0;
}
```

### Exit Code Constants

```csharp
public static class ExitCodes
{
    public const int Success = 0;
    public const int GeneralError = 1;
    public const int Misuse = 2;
    public const int Timeout = 124;
    public const int NotExecutable = 126;
    public const int NotFound = 127;
    public const int Interrupted = 130;
    
    public static string GetDescription(int exitCode) => exitCode switch
    {
        0 => "Success",
        1 => "General error",
        2 => "Misuse of command",
        124 => "Command timed out",
        126 => "Command not executable",
        127 => "Command not found",
        130 => "Interrupted (Ctrl+C)",
        _ when exitCode > 128 => $"Killed by signal {exitCode - 128}",
        _ => $"Failed with exit code {exitCode}"
    };
}
```

### Logging Schema

```csharp
public static class CommandLogFields
{
    public const string CommandGroup = "command_group";
    public const string Command = "command";
    public const string WorkingDirectory = "working_directory";
    public const string ExitCode = "exit_code";
    public const string DurationMs = "duration_ms";
    public const string Attempt = "attempt";
    public const string TimedOut = "timed_out";
    public const string Platform = "platform";
    public const string EnvVarCount = "env_var_count";
}
```

### Validation Checklist Before Merge

- [ ] All 120 functional requirements implemented
- [ ] All 40 non-functional requirements verified
- [ ] All 6 command groups documented
- [ ] All 25 unit tests passing
- [ ] All 15 integration tests passing
- [ ] All 12 E2E tests passing
- [ ] All 8 performance benchmarks met
- [ ] Exit code mapping complete
- [ ] Platform detection working
- [ ] Timeout handling working
- [ ] Retry logic working
- [ ] Environment variable handling working
- [ ] Working directory validation working
- [ ] Security checks in place
- [ ] Documentation complete

### Rollout Plan

1. **Phase 1: Domain Models**
   - Define CommandGroup enum
   - Define CommandSpec record
   - Define CommandResult record
   - Add unit tests

2. **Phase 2: Parsing**
   - Implement command parser
   - Support all formats (string, array, object)
   - Add validation
   - Add integration tests

3. **Phase 3: Execution**
   - Implement command executor
   - Add timeout handling
   - Add retry logic
   - Add E2E tests

4. **Phase 4: Polish**
   - Add platform variants
   - Add environment variable handling
   - Add exit code descriptions
   - Document all features

---

**END OF TASK 002.c**