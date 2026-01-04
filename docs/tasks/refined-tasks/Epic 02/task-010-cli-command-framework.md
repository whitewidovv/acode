# Task 010: CLI Command Framework

**Priority:** P0 – Critical Path  
**Tier:** Core Infrastructure  
**Complexity:** 21 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 002 (.agent/config.yml), Task 001 (Operating Modes)  

---

## Description

Task 010 implements the CLI Command Framework, the primary interface through which users interact with Acode. The CLI provides structured commands for starting agent runs, managing sessions, configuring behavior, and querying status. A well-designed CLI is essential for usability, automation, and integration with development workflows.

### Purpose and Scope

The CLI serves as the single entry point for all Acode functionality. Whether a developer wants to start an agent run, check status, configure models, or manage conversations, they do so through the CLI. This unified interface simplifies learning and ensures consistent behavior across all operations. The CLI is designed for both interactive use (developers at their terminal) and automated use (CI/CD pipelines, scripts, tooling integrations).

### Command Architecture

The CLI follows established conventions for command-line tools. Commands use a consistent pattern: `acode <command> [subcommand] [options] [arguments]`. Global options apply to all commands. Command-specific options customize behavior. Arguments provide required inputs. This structure enables both interactive use and scriptable automation.

The command router is the central dispatch mechanism. When a user invokes `acode run "task"`, the router parses the command name, locates the registered handler, validates arguments, and delegates execution. The router also handles unknown commands gracefully, suggesting similar commands when a typo is detected.

Each command implements a standard interface that provides:
- Command name and aliases
- Description for help text
- Option definitions with types and defaults
- Argument specifications
- Execute method that performs the actual work
- Help generation for the command

### Help System Design

Help documentation is comprehensive and discoverable. Every command has built-in help accessible via `--help` or `acode help <command>`. Help includes descriptions, option documentation, examples, and related commands. Users can learn the CLI without external documentation.

The help system is generated from command metadata, ensuring documentation stays synchronized with implementation. When new options are added, help automatically updates. This eliminates documentation drift that plagues manually-maintained help.

### Output Formatting Strategy

Output formatting adapts to context. Interactive use gets human-readable output with colors and formatting. Non-interactive use (scripts, CI/CD) gets structured output (JSONL) for parsing. The `--json` flag switches to machine-readable mode. This flexibility enables the CLI to serve both humans and automation.

The output system uses a formatter abstraction. Commands emit semantic output (tables, messages, progress) and the formatter renders appropriately. Console formatter adds colors, borders, and spacing. JSONL formatter emits structured records. This separation keeps commands format-agnostic.

### Configuration Hierarchy

Configuration follows a precedence hierarchy. Command-line arguments override environment variables, which override configuration file values, which override defaults. This allows users to set baseline configuration in files while overriding specific values at runtime.

The configuration loader reads from multiple sources and merges them according to precedence. This happens once at startup, and the resolved configuration is available to all commands. Configuration errors are detected early and reported with actionable messages.

### Error Handling Philosophy

Error handling provides actionable feedback. When commands fail, error messages explain what went wrong and suggest remediation. Exit codes distinguish between different failure types—user error, system error, configuration error. Scripts can respond appropriately to different failure modes.

Every error includes:
- A unique error code for programmatic handling
- A human-readable message explaining the problem
- Suggestions for how to fix the issue
- Context about what operation was attempted

### Subsystem Integration

The CLI integrates with all Acode subsystems. Model management commands interact with the provider registry (Epic 01). Run commands trigger the agent orchestrator (Tasks 011-012). Configuration commands read and write `.agent/config.yml`. This integration makes the CLI the unified control surface.

Integration follows dependency injection patterns. Commands receive their dependencies rather than creating them. This enables testing with mock implementations and ensures clean separation of concerns.

### Extensibility Design

The framework is extensible for future commands. New commands can be added by implementing the command interface and registering with the router. The framework handles parsing, validation, help generation, and output formatting. This extensibility supports growth without framework changes.

The command registration is declarative. Commands self-describe their options, arguments, and help text. The framework uses this metadata for parsing, validation, and help generation. Adding a command requires only implementing the interface—no framework modifications.

### Performance Optimization

Performance is a key concern. CLI startup must be fast—users expect immediate response from command-line tools. Lazy loading and minimal initialization keep startup under 500ms. Long-running commands provide progress feedback to maintain responsiveness perception.

Startup optimization strategies:
- Defer configuration loading until needed
- Lazy-load command handlers
- Minimize assembly loading
- Cache parsed configuration
- Use async initialization where possible

### Accessibility and Compatibility

Accessibility considerations ensure the CLI works in diverse environments. Colors are optional (disabled in non-TTY contexts). Unicode handling is robust. Screen reader compatibility is considered for help text. These considerations broaden usability.

Cross-platform compatibility is essential:
- Windows PowerShell and cmd.exe
- Linux bash, zsh, and other shells
- macOS Terminal and iTerm
- CI/CD environments (GitHub Actions, Azure DevOps, etc.)

### Logging and Observability

Logging provides observability into CLI operations. Commands log their invocation, key decisions, and results. Log output goes to stderr to not interfere with stdout output. Log verbosity is controllable. This visibility aids debugging and audit.

Log levels:
- ERROR: Operation failed
- WARN: Potential issues, degraded operation
- INFO: Key operations and milestones
- DEBUG: Detailed execution trace (with -v)

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| CLI | Command-Line Interface |
| Command | Top-level action (e.g., run, chat, config) |
| Subcommand | Nested action under a command |
| Global Option | Option that applies to all commands |
| Command Option | Option specific to one command |
| Argument | Positional input value |
| Exit Code | Numeric status returned on completion |
| TTY | Terminal/teletype device (interactive) |
| JSONL | JSON Lines format for streaming |
| Stdout | Standard output stream |
| Stderr | Standard error stream |
| Configuration Precedence | Order of config source priority |
| Command Router | Component that dispatches commands |
| Output Formatter | Component that formats output |
| Progress Indicator | Visual feedback for long operations |
| Help Generator | Produces help documentation |

---

## Assumptions

### Technical Assumptions

- ASM-001: .NET 8+ is the target runtime with C# as the primary implementation language
- ASM-002: System.CommandLine or similar parsing library will be used for argument parsing
- ASM-003: Console/Terminal supports ANSI escape sequences for colors on modern systems
- ASM-004: UTF-8 encoding is available and correctly configured on the host system
- ASM-005: File system operations use standard .NET APIs with cross-platform path handling
- ASM-006: JSON serialization uses System.Text.Json for JSONL output formatting
- ASM-007: Logging infrastructure from Microsoft.Extensions.Logging is available

### Environmental Assumptions

- ASM-008: Users have a working terminal/console environment (bash, zsh, PowerShell, cmd)
- ASM-009: The `.agent/config.yml` file location is determinable from current working directory
- ASM-010: Environment variables are accessible via standard OS mechanisms
- ASM-011: Standard input/output/error streams are available and functional
- ASM-012: The host system has sufficient permissions to read configuration files
- ASM-013: Network access is NOT required for CLI startup (local-only mode support)

### Dependency Assumptions

- ASM-014: Task 002 (.agent/config.yml) schema and parser are complete and available
- ASM-015: Task 001 (Operating Modes) provides mode detection and constraint enforcement
- ASM-016: Model provider registry (Epic 01) is available for model-related commands
- ASM-017: Agent orchestrator (Tasks 011-012) is available for run/resume commands
- ASM-018: Database layer (Task 050) is available for db commands

### User Assumptions

- ASM-019: Users have basic familiarity with command-line interfaces
- ASM-020: Users understand the concept of configuration files and environment variables
- ASM-021: Users can read English help text and error messages
- ASM-022: Users have access to documentation or help commands for learning

---

## Out of Scope

The following items are explicitly excluded from Task 010:

- **GUI or web interface** - CLI only
- **Remote CLI execution** - Local only
- **Shell completion generation** - Post-MVP
- **Alias definitions** - Post-MVP
- **Command history** - Shell-provided
- **Interactive wizards** - Simple prompts only
- **Plugin commands** - Fixed command set
- **Command chaining** - Use shell pipes
- **Daemon mode** - Single invocation
- **Multi-language CLI** - English only

---

## Functional Requirements

### Command Structure

- FR-001: CLI MUST use format: acode [global-opts] <cmd> [opts] [args]
- FR-002: Commands MUST be lowercase alphanumeric
- FR-003: Subcommands MUST be separated by space
- FR-004: Options MUST use -- prefix for long form
- FR-005: Options MUST use - prefix for short form
- FR-006: Boolean options MUST support --no- prefix
- FR-007: Options with values MUST use = or space
- FR-008: Unknown options MUST error with suggestion

### Global Options

- FR-009: --help MUST show global help
- FR-010: --version MUST show version info
- FR-011: --config <path> MUST override config file
- FR-012: --verbose / -v MUST increase verbosity
- FR-013: --quiet / -q MUST decrease verbosity
- FR-014: --json MUST enable JSONL output
- FR-015: --yes / -y MUST skip approval prompts
- FR-016: --dry-run MUST preview without executing
- FR-017: --no-color MUST disable colored output
- FR-018: --log-level MUST set minimum log level

### Core Commands

- FR-019: `run` MUST start an agent run
- FR-020: `resume` MUST resume an interrupted run
- FR-021: `chat` MUST manage conversations
- FR-022: `models` MUST manage model configuration
- FR-023: `prompts` MUST manage prompt packs
- FR-024: `config` MUST manage configuration
- FR-025: `status` MUST show current state
- FR-026: `db` MUST manage database operations
- FR-027: `help` MUST show help for commands

### Help System

- FR-028: Every command MUST have --help
- FR-029: Help MUST show command description
- FR-030: Help MUST list all options
- FR-031: Help MUST show usage examples
- FR-032: Help MUST show related commands
- FR-033: `acode help` MUST list all commands
- FR-034: `acode help <cmd>` MUST show command help
- FR-035: Help MUST be formatted for terminal width

### Exit Codes

- FR-036: 0 MUST indicate success
- FR-037: 1 MUST indicate general error
- FR-038: 2 MUST indicate invalid arguments
- FR-039: 3 MUST indicate configuration error
- FR-040: 4 MUST indicate runtime error
- FR-041: 5 MUST indicate user cancellation
- FR-042: 130 MUST indicate SIGINT (Ctrl+C)
- FR-043: Exit code MUST be documented

### Configuration Precedence

- FR-044: CLI args MUST have highest precedence
- FR-045: Environment vars MUST override config file
- FR-046: Config file MUST override defaults
- FR-047: Defaults MUST be documented
- FR-048: --config MUST override default config path
- FR-049: ACODE_CONFIG_FILE MUST specify config path
- FR-050: Missing config file MUST use defaults

### Error Handling

- FR-051: Errors MUST be written to stderr
- FR-052: Error messages MUST be actionable
- FR-053: Errors MUST include error code
- FR-054: Errors MUST suggest remediation
- FR-055: Stack traces MUST require --verbose
- FR-056: Errors MUST not expose sensitive data

### Input Validation

- FR-057: Arguments MUST be validated before execution
- FR-058: Invalid arguments MUST fail fast
- FR-059: Missing required arguments MUST error
- FR-060: Type mismatches MUST error with expected type
- FR-061: Path arguments MUST be validated

### Output Formatting

- FR-062: Default output MUST be human-readable
- FR-063: --json MUST output valid JSONL
- FR-064: Colors MUST be disabled in non-TTY
- FR-065: Tables MUST fit terminal width
- FR-066: Long output MUST be paginated (if TTY)
- FR-067: Progress MUST be shown for long operations

### Logging

- FR-068: Logs MUST go to stderr
- FR-069: Log format MUST include timestamp
- FR-070: Log format MUST include level
- FR-071: Log format MUST include message
- FR-072: --verbose MUST show DEBUG logs
- FR-073: --quiet MUST suppress INFO logs
- FR-074: Default MUST show WARN and ERROR

---

## Non-Functional Requirements

### Performance

- NFR-001: CLI startup MUST complete in < 500ms
- NFR-002: Help output MUST complete in < 100ms
- NFR-003: Argument parsing MUST complete in < 50ms
- NFR-004: Memory usage MUST be < 100MB baseline

### Reliability

- NFR-005: Invalid input MUST NOT crash
- NFR-006: Interrupted commands MUST cleanup
- NFR-007: SIGINT MUST be handled gracefully
- NFR-008: SIGTERM MUST be handled gracefully

### Security

- NFR-009: Secrets MUST NOT appear in logs
- NFR-010: File paths MUST be sanitized
- NFR-011: Commands MUST NOT execute arbitrary code
- NFR-012: Config files MUST NOT be world-readable

### Compatibility

- NFR-013: Windows MUST be supported
- NFR-014: Linux MUST be supported
- NFR-015: macOS MUST be supported
- NFR-016: Paths MUST handle spaces
- NFR-017: Unicode MUST be handled correctly

### Observability

- NFR-018: All commands MUST be logged
- NFR-019: Exit codes MUST be logged
- NFR-020: Duration MUST be logged
- NFR-021: Errors MUST be logged with context

---

## Security Considerations

### Input Validation Security

- SEC-001: All command-line arguments MUST be validated before use
- SEC-002: Path arguments MUST be canonicalized to prevent traversal attacks
- SEC-003: Arguments MUST NOT be passed directly to shell execution
- SEC-004: Special characters MUST be escaped or rejected in file paths
- SEC-005: Maximum argument length MUST be enforced to prevent buffer issues

### Secret Protection

- SEC-006: API keys and tokens MUST NOT appear in command-line arguments (use env vars or config)
- SEC-007: Secrets MUST NOT be logged at any verbosity level
- SEC-008: Process command lines are visible to other users; secrets MUST NOT be exposed there
- SEC-009: Error messages MUST NOT include secret values even when describing errors
- SEC-010: JSONL output MUST redact any accidentally-included secret patterns

### File System Security

- SEC-011: Configuration files SHOULD have restricted permissions (600 or 640)
- SEC-012: CLI MUST warn if config file is world-readable and contains secrets
- SEC-013: Temporary files MUST be created with secure permissions
- SEC-014: Output files MUST NOT overwrite existing files without confirmation
- SEC-015: Symbolic links MUST be handled carefully to prevent symlink attacks

### Execution Security

- SEC-016: CLI MUST NOT execute arbitrary code from user input
- SEC-017: Plugin/extension loading MUST be disabled in this version
- SEC-018: Commands MUST operate with least-privilege principles
- SEC-019: Dangerous operations MUST require explicit confirmation (unless --yes)
- SEC-020: Audit log MUST record all command invocations with timestamps

### Network Security

- SEC-021: CLI startup MUST NOT require network access (local-only mode)
- SEC-022: Any network operations MUST respect operating mode constraints
- SEC-023: TLS MUST be used for any network communication
- SEC-024: Certificate validation MUST NOT be disabled in production

---

## User Manual Documentation

### Overview

Acode provides a command-line interface for agentic coding assistance. This guide covers basic usage, commands, configuration, and troubleshooting.

### Quick Start

```bash
# Show help
$ acode --help

# Check version
$ acode --version

# Start an agent run
$ acode run "Add input validation to the login form"

# Resume interrupted run
$ acode resume

# Show status
$ acode status
```

### Command Structure

```
acode [global-options] <command> [options] [arguments]
```

**Global Options:**

| Option | Short | Description |
|--------|-------|-------------|
| `--help` | `-h` | Show help |
| `--version` | | Show version |
| `--config <path>` | `-c` | Use config file |
| `--verbose` | `-v` | Verbose output |
| `--quiet` | `-q` | Minimal output |
| `--json` | | JSONL output |
| `--yes` | `-y` | Skip prompts |
| `--dry-run` | | Preview only |
| `--no-color` | | Disable colors |
| `--log-level` | | Set log level |

### Core Commands

#### run

Start an agent run with a task description:

```bash
# Basic run
$ acode run "Fix the bug in UserService"

# With options
$ acode run --model llama3.2:70b "Implement feature X"

# Dry run (preview)
$ acode run --dry-run "Add tests"
```

#### resume

Resume an interrupted run:

```bash
# Resume last run
$ acode resume

# Resume specific run
$ acode resume --run-id abc123
```

#### status

Show current status:

```bash
$ acode status
Current Run: abc123
Status: EXECUTING
Step: 3/5 - Adding validation
Model: llama3.2:7b
```

#### chat

Manage conversations:

```bash
# List chats
$ acode chat list

# Create new chat
$ acode chat new "Feature work"

# Open existing chat
$ acode chat open abc123
```

#### models

Manage model configuration:

```bash
# List available models
$ acode models list

# Show routing configuration
$ acode models routing

# Test model availability
$ acode models test
```

#### config

Manage configuration:

```bash
# Show current config
$ acode config show

# Set value
$ acode config set models.default llama3.2:7b

# Get value
$ acode config get models.default
```

### Configuration

#### Config File

Default location: `.agent/config.yml`

```yaml
# .agent/config.yml
operating_mode: local-only

models:
  default: llama3.2:7b
  routing:
    strategy: role-based

prompts:
  pack_id: acode-standard

approvals:
  require_for:
    - delete_file
    - execute_command
```

#### Environment Variables

All config values can be overridden with environment variables:

```bash
export ACODE_MODELS_DEFAULT=llama3.2:70b
export ACODE_OPERATING_MODE=local-only
```

Pattern: `ACODE_` + uppercase path with underscores.

#### Precedence

1. Command-line arguments (highest)
2. Environment variables
3. Config file
4. Defaults (lowest)

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | General error |
| 2 | Invalid arguments |
| 3 | Configuration error |
| 4 | Runtime error |
| 5 | User cancellation |
| 130 | Interrupted (Ctrl+C) |

### Output Modes

#### Human-Readable (default)

```bash
$ acode status
┌─────────────────────────────────────────────┐
│ Acode Status                                 │
├─────────────────────────────────────────────┤
│ Current Run: abc123                          │
│ Status: EXECUTING                            │
│ Step: 3/5                                    │
└─────────────────────────────────────────────┘
```

#### JSONL Mode

```bash
$ acode status --json
{"type":"status","run_id":"abc123","status":"EXECUTING","step":3,"total_steps":5}
```

### Logging

Logs go to stderr:

```bash
# Normal (WARN and ERROR)
$ acode run "task"

# Verbose (includes DEBUG)
$ acode run -v "task"

# Quiet (ERROR only)
$ acode run -q "task"

# Specific level
$ acode run --log-level debug "task"
```

### Troubleshooting

#### Command Not Found

```
$ acode: command not found
```

**Cause:** Acode is not installed or not in your PATH.

**Solution:** 
1. Verify installation: Check if Acode is installed in the expected location
2. Add to PATH: Add the Acode installation directory to your system PATH
3. Use full path: Run using the complete path to the executable

#### Invalid Configuration

```
Error [ACODE-CFG-001]: Invalid configuration
  Path: models.default
  Value: invalid-model
  Expected: Valid model ID
```

**Cause:** Configuration file contains invalid values.

**Solution:** 
1. Check config file: Open `.agent/config.yml` and review the specified path
2. Validate model ID: Ensure the model ID matches an available model
3. Check environment: Verify no conflicting environment variables

#### Model Unavailable

```
Error [ACODE-MDL-001]: Model unavailable
  Model: llama3.2:70b
  Suggestion: Start model with 'ollama run llama3.2:70b'
```

**Cause:** The specified model is not running or accessible.

**Solution:** 
1. Start the model: Run `ollama run <model-name>` to start the model
2. Check Ollama: Ensure Ollama service is running
3. Verify model exists: Run `ollama list` to see available models

#### Permission Denied

```
Error [ACODE-FS-001]: Permission denied
  Path: /protected/config.yml
```

**Cause:** Insufficient permissions to read/write the specified file.

**Solution:**
1. Check file permissions: Verify you have read/write access
2. Check directory permissions: Ensure parent directories are accessible
3. Run with appropriate privileges: May need elevated permissions

#### JSONL Parse Error

```
Error [ACODE-CLI-005]: Invalid JSON in input
  Line: 3
  Error: Unexpected token
```

**Cause:** Input provided to CLI is not valid JSON.

**Solution:**
1. Validate JSON: Use a JSON validator to check your input
2. Check encoding: Ensure UTF-8 encoding without BOM
3. Escape special characters: Properly escape quotes and backslashes

#### Slow Startup

**Symptom:** CLI takes more than 500ms to start.

**Cause:** Large configuration, slow disk, or excessive plugins.

**Solution:**
1. Check config file size: Large configs slow parsing
2. Check disk performance: SSD recommended for responsive CLI
3. Disable unnecessary features: Reduce startup initialization

#### Colors Not Displaying

**Symptom:** Output appears without colors or with escape codes visible.

**Cause:** Terminal does not support ANSI colors or is not recognized as TTY.

**Solution:**
1. Use `--no-color`: Explicitly disable colors
2. Check terminal: Ensure terminal supports ANSI escape sequences
3. Check TERM variable: Set `TERM=xterm-256color` or similar

#### Environment Variable Not Working

**Symptom:** Setting `ACODE_*` environment variable has no effect.

**Cause:** Variable not exported, typo in name, or CLI arg overriding.

**Solution:**
1. Export the variable: Use `export ACODE_VAR=value` in bash
2. Check spelling: Variable names are case-sensitive
3. Check precedence: CLI args override environment variables

---

## Acceptance Criteria

### Command Structure

- [ ] AC-001: acode [global] <cmd> [opts] [args] format
- [ ] AC-002: Lowercase alphanumeric commands
- [ ] AC-003: Subcommands separated by space
- [ ] AC-004: -- prefix for long options
- [ ] AC-005: - prefix for short options
- [ ] AC-006: --no- prefix works
- [ ] AC-007: = or space for option values
- [ ] AC-008: Unknown options error with suggestion

### Global Options

- [ ] AC-009: --help shows global help
- [ ] AC-010: --version shows version
- [ ] AC-011: --config overrides path
- [ ] AC-012: --verbose increases verbosity
- [ ] AC-013: --quiet decreases verbosity
- [ ] AC-014: --json enables JSONL
- [ ] AC-015: --yes skips prompts
- [ ] AC-016: --dry-run previews only
- [ ] AC-017: --no-color disables colors
- [ ] AC-018: --log-level sets level

### Commands

- [ ] AC-019: run command works
- [ ] AC-020: resume command works
- [ ] AC-021: chat command works
- [ ] AC-022: models command works
- [ ] AC-023: prompts command works
- [ ] AC-024: config command works
- [ ] AC-025: status command works
- [ ] AC-026: db command works
- [ ] AC-027: help command works

### Help

- [ ] AC-028: --help on every command
- [ ] AC-029: Shows description
- [ ] AC-030: Lists all options
- [ ] AC-031: Shows examples
- [ ] AC-032: Shows related commands
- [ ] AC-033: acode help lists commands
- [ ] AC-034: acode help <cmd> works
- [ ] AC-035: Formats for terminal width

### Exit Codes

- [ ] AC-036: 0 on success
- [ ] AC-037: 1 on general error
- [ ] AC-038: 2 on invalid arguments
- [ ] AC-039: 3 on config error
- [ ] AC-040: 4 on runtime error
- [ ] AC-041: 5 on cancellation
- [ ] AC-042: 130 on SIGINT

### Precedence

- [ ] AC-043: CLI args highest
- [ ] AC-044: Env vars override config
- [ ] AC-045: Config file override defaults
- [ ] AC-046: Defaults documented
- [ ] AC-047: --config works
- [ ] AC-048: ACODE_CONFIG_FILE works

### Errors

- [ ] AC-049: Errors to stderr
- [ ] AC-050: Actionable messages
- [ ] AC-051: Include error codes
- [ ] AC-052: Suggest remediation
- [ ] AC-053: Stack traces with -v only

### Output

- [ ] AC-054: Human-readable default
- [ ] AC-055: JSONL with --json
- [ ] AC-056: Colors disabled non-TTY
- [ ] AC-057: Tables fit width
- [ ] AC-058: Progress shown

---

## Best Practices

### Command Design

- **BP-001: Use verb-noun naming** - Commands should follow `acode <verb> [noun]` pattern (e.g., `run`, `config get`, `models list`)
- **BP-002: Keep commands shallow** - Limit subcommand depth to 2 levels maximum (`acode config get`, not `acode config settings get value`)
- **BP-003: Provide sensible defaults** - Commands should work with minimal arguments; require only what's truly necessary
- **BP-004: Support both interactive and scripted use** - Every command should work in both modes without modification

### Option Conventions

- **BP-005: Use standard option names** - Follow GNU/POSIX conventions (`--help`, `--version`, `--verbose`, `--quiet`)
- **BP-006: Provide short aliases for common options** - `-h`, `-v`, `-q`, `-y` for frequently used flags
- **BP-007: Boolean options use --no- prefix** - `--color` and `--no-color`, not `--color=false`
- **BP-008: Options before arguments** - Parse options first, then positional arguments

### Output Design

- **BP-009: Human-readable by default** - Default output should be formatted for human consumption
- **BP-010: Machine-readable on request** - `--json` flag for scripting and automation
- **BP-011: Errors to stderr, results to stdout** - Never mix error messages with program output
- **BP-012: Use exit codes consistently** - Scripts should be able to rely on exit codes for flow control

### Error Handling

- **BP-013: Fail fast on invalid input** - Validate all arguments before executing any operation
- **BP-014: Provide actionable error messages** - Tell users what went wrong and how to fix it
- **BP-015: Include error codes for automation** - Error codes enable programmatic error handling
- **BP-016: Never expose sensitive information** - Secrets, tokens, and passwords must never appear in errors

### Help System

- **BP-017: Every command has help** - `--help` must work on every command and subcommand
- **BP-018: Include examples in help** - Real-world usage examples are more valuable than abstract descriptions
- **BP-019: Show related commands** - Guide users to discover related functionality
- **BP-020: Keep help concise** - Avoid overwhelming users; link to full documentation for details

### Configuration

- **BP-021: Respect XDG conventions** - Use standard config locations where applicable
- **BP-022: Environment variables for CI/CD** - Support configuration via environment for automation
- **BP-023: Document all config options** - Every configuration value should be documented
- **BP-024: Validate config on load** - Catch configuration errors early with clear messages

### Performance

- **BP-025: Lazy load dependencies** - Don't initialize subsystems until they're needed
- **BP-026: Fast startup is essential** - Users expect CLI tools to respond instantly
- **BP-027: Show progress for long operations** - Keep users informed during multi-second operations
- **BP-028: Support cancellation** - Ctrl+C should always work and clean up properly

### Security

- **BP-029: Sanitize all file paths** - Prevent path traversal and injection attacks
- **BP-030: Use secure defaults** - Security should not require explicit opt-in
- **BP-031: Audit log all commands** - Maintain a record of what was executed and when
- **BP-032: Principle of least privilege** - Request only the permissions actually needed

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/CLI/
├── CommandRouterTests.cs
│   ├── Should_Route_Known_Command()
│   ├── Should_Error_On_Unknown_Command()
│   ├── Should_Suggest_Similar_Command()
│   ├── Should_Handle_Aliases()
│   └── Should_List_All_Commands()
│
├── ArgumentParserTests.cs
│   ├── Should_Parse_Long_Options()
│   ├── Should_Parse_Short_Options()
│   ├── Should_Parse_Arguments()
│   ├── Should_Handle_Mixed_Input()
│   ├── Should_Handle_Equals_Syntax()
│   ├── Should_Handle_Space_Syntax()
│   ├── Should_Handle_No_Prefix()
│   ├── Should_Reject_Unknown_Option()
│   └── Should_Handle_Boolean_Options()
│
├── GlobalOptionsTests.cs
│   ├── Should_Parse_Help_Flag()
│   ├── Should_Parse_Version_Flag()
│   ├── Should_Parse_Verbose_Flag()
│   ├── Should_Parse_Quiet_Flag()
│   ├── Should_Parse_Json_Flag()
│   ├── Should_Parse_Yes_Flag()
│   ├── Should_Parse_DryRun_Flag()
│   ├── Should_Parse_NoColor_Flag()
│   └── Should_Parse_LogLevel_Option()
│
├── OutputFormatterTests.cs
│   ├── Should_Format_Table()
│   ├── Should_Format_JSONL()
│   ├── Should_Handle_Wide_Content()
│   ├── Should_Truncate_Long_Values()
│   ├── Should_Handle_Unicode()
│   ├── Should_Disable_Colors_NonTTY()
│   └── Should_Respect_Terminal_Width()
│
├── ConfigPrecedenceTests.cs
│   ├── Should_CLI_Override_Env()
│   ├── Should_Env_Override_File()
│   ├── Should_Use_Defaults()
│   ├── Should_Merge_Nested_Config()
│   └── Should_Handle_Missing_File()
│
├── ErrorHandlerTests.cs
│   ├── Should_Format_Error_Message()
│   ├── Should_Include_Error_Code()
│   ├── Should_Suggest_Remediation()
│   ├── Should_Write_To_Stderr()
│   └── Should_Redact_Secrets()
│
└── HelpGeneratorTests.cs
    ├── Should_Generate_Command_Help()
    ├── Should_Include_Examples()
    ├── Should_List_Options()
    ├── Should_Show_Related_Commands()
    └── Should_Respect_Terminal_Width()
```

### Integration Tests

```
Tests/Integration/CLI/
├── CommandExecutionTests.cs
│   ├── Should_Execute_Help()
│   ├── Should_Execute_Version()
│   ├── Should_Execute_Status()
│   ├── Should_Execute_Config_Get()
│   ├── Should_Execute_Config_Set()
│   └── Should_Execute_Models_List()
│
├── ConfigurationLoadingTests.cs
│   ├── Should_Load_From_File()
│   ├── Should_Load_From_Environment()
│   ├── Should_Apply_Precedence()
│   └── Should_Validate_Config()
│
├── OutputModeTests.cs
│   ├── Should_Output_Human_Readable()
│   ├── Should_Output_JSONL()
│   ├── Should_Detect_TTY()
│   └── Should_Respect_NoColor()
│
└── SignalHandlingTests.cs
    ├── Should_Handle_SIGINT()
    ├── Should_Handle_SIGTERM()
    └── Should_Cleanup_On_Cancel()
```

### E2E Tests

```
Tests/E2E/CLI/
├── CLIEndToEndTests.cs
│   ├── Should_Run_Full_Workflow()
│   ├── Should_Handle_Interruption()
│   ├── Should_Resume_Session()
│   ├── Should_Respect_DryRun()
│   └── Should_Work_With_Config_Overrides()
│
├── CrossPlatformTests.cs
│   ├── Should_Work_On_Windows()
│   ├── Should_Work_On_Linux()
│   ├── Should_Work_On_MacOS()
│   └── Should_Handle_Path_Separators()
│
└── AutomationTests.cs
    ├── Should_Work_In_CICD()
    ├── Should_Parse_JSONL_Output()
    ├── Should_Return_Correct_Exit_Codes()
    └── Should_Work_NonInteractive()
```

### Performance Tests

- PERF-001: Startup MUST complete in < 500ms
- PERF-002: Help output MUST complete in < 100ms
- PERF-003: Argument parsing MUST complete in < 50ms
- PERF-004: Config loading MUST complete in < 100ms
- PERF-005: Command routing MUST complete in < 10ms
- PERF-006: Memory baseline MUST be < 100MB

### Test Coverage Requirements

- Minimum 80% line coverage for CLI module
- 100% coverage for error handling paths
- 100% coverage for security-sensitive paths
- All public APIs must have at least one test

---

## User Verification Steps

### Scenario 1: Help

1. Run `acode --help`
2. Verify: All commands listed
3. Verify: Global options shown

### Scenario 2: Version

1. Run `acode --version`
2. Verify: Version displayed
3. Verify: Exit code 0

### Scenario 3: Command Help

1. Run `acode run --help`
2. Verify: Run options shown
3. Verify: Examples provided

### Scenario 4: Invalid Command

1. Run `acode unknowncommand`
2. Verify: Error message shown
3. Verify: Exit code 2

### Scenario 5: JSONL Mode

1. Run `acode status --json`
2. Verify: Valid JSON output
3. Verify: Parseable by jq

### Scenario 6: Config Override

1. Set env var ACODE_MODELS_DEFAULT=test
2. Run `acode config get models.default`
3. Verify: Shows "test"

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.CLI/
├── Program.cs
├── CommandRouter.cs
├── Commands/
│   ├── ICommand.cs
│   ├── RunCommand.cs
│   ├── ResumeCommand.cs
│   ├── ChatCommand.cs
│   ├── ModelsCommand.cs
│   ├── PromptsCommand.cs
│   ├── ConfigCommand.cs
│   ├── StatusCommand.cs
│   ├── DbCommand.cs
│   └── HelpCommand.cs
├── Options/
│   ├── GlobalOptions.cs
│   └── OptionParser.cs
├── Output/
│   ├── IOutputFormatter.cs
│   ├── ConsoleFormatter.cs
│   └── JsonlFormatter.cs
└── Configuration/
    ├── ConfigurationLoader.cs
    └── PrecedenceResolver.cs
```

### ICommand Interface

```csharp
namespace AgenticCoder.CLI.Commands;

public interface ICommand
{
    string Name { get; }
    string Description { get; }
    IReadOnlyList<string> Aliases { get; }
    Task<int> ExecuteAsync(CommandContext context);
    void PrintHelp(IOutputFormatter formatter);
}
```

### CommandRouter

```csharp
namespace AgenticCoder.CLI;

public sealed class CommandRouter
{
    private readonly Dictionary<string, ICommand> _commands;
    
    public ICommand? Route(string commandName);
    public IEnumerable<ICommand> GetAllCommands();
}
```

### Exit Codes

| Code | Constant | Meaning |
|------|----------|---------|
| 0 | ExitCode.Success | Success |
| 1 | ExitCode.GeneralError | General error |
| 2 | ExitCode.InvalidArguments | Invalid arguments |
| 3 | ExitCode.ConfigurationError | Configuration error |
| 4 | ExitCode.RuntimeError | Runtime error |
| 5 | ExitCode.UserCancellation | User cancellation |
| 130 | ExitCode.Interrupted | SIGINT |

### Error Codes

| Code | Message |
|------|---------|
| ACODE-CLI-001 | Unknown command |
| ACODE-CLI-002 | Invalid option |
| ACODE-CLI-003 | Missing argument |
| ACODE-CLI-004 | Invalid argument type |
| ACODE-CFG-001 | Invalid configuration |
| ACODE-CFG-002 | Missing config file |

### Implementation Checklist

1. [ ] Create Program.cs entry point
2. [ ] Create CommandRouter
3. [ ] Create ICommand interface
4. [ ] Create GlobalOptions class
5. [ ] Create OptionParser
6. [ ] Implement IOutputFormatter
7. [ ] Implement ConsoleFormatter
8. [ ] Implement JsonlFormatter
9. [ ] Implement ConfigurationLoader
10. [ ] Implement PrecedenceResolver
11. [ ] Create all core commands
12. [ ] Add help generation
13. [ ] Add signal handling
14. [ ] Write unit tests
15. [ ] Write integration tests
16. [ ] Add XML documentation

### Verification Command

```bash
dotnet test --filter "FullyQualifiedName~CLI"
```

---

**End of Task 010 Specification**