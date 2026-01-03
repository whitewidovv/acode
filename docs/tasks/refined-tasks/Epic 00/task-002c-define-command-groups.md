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

