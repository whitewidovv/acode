# Task 010: CLI Command Framework

**Priority:** P0 – Critical Path  
**Tier:** Core Infrastructure  
**Complexity:** 21 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 002 (.agent/config.yml), Task 001 (Operating Modes)  

---

## Description

Task 010 implements the CLI Command Framework, the primary interface through which users interact with Acode. The CLI provides structured commands for starting agent runs, managing sessions, configuring behavior, and querying status. A well-designed CLI is essential for usability, automation, and integration with development workflows.

The CLI follows established conventions for command-line tools. Commands use a consistent pattern: `acode <command> [subcommand] [options] [arguments]`. Global options apply to all commands. Command-specific options customize behavior. Arguments provide required inputs. This structure enables both interactive use and scriptable automation.

Help documentation is comprehensive and discoverable. Every command has built-in help accessible via `--help` or `acode help <command>`. Help includes descriptions, option documentation, examples, and related commands. Users can learn the CLI without external documentation.

Output formatting adapts to context. Interactive use gets human-readable output with colors and formatting. Non-interactive use (scripts, CI/CD) gets structured output (JSONL) for parsing. The `--json` flag switches to machine-readable mode. This flexibility enables the CLI to serve both humans and automation.

Configuration follows a precedence hierarchy. Command-line arguments override environment variables, which override configuration file values, which override defaults. This allows users to set baseline configuration in files while overriding specific values at runtime.

Error handling provides actionable feedback. When commands fail, error messages explain what went wrong and suggest remediation. Exit codes distinguish between different failure types—user error, system error, configuration error. Scripts can respond appropriately to different failure modes.

The CLI integrates with all Acode subsystems. Model management commands interact with the provider registry (Epic 01). Run commands trigger the agent orchestrator (Tasks 011-012). Configuration commands read and write `.agent/config.yml`. This integration makes the CLI the unified control surface.

The framework is extensible for future commands. New commands can be added by implementing the command interface and registering with the router. The framework handles parsing, validation, help generation, and output formatting. This extensibility supports growth without framework changes.

Performance is a key concern. CLI startup must be fast—users expect immediate response from command-line tools. Lazy loading and minimal initialization keep startup under 500ms. Long-running commands provide progress feedback to maintain responsiveness perception.

Accessibility considerations ensure the CLI works in diverse environments. Colors are optional (disabled in non-TTY contexts). Unicode handling is robust. Screen reader compatibility is considered for help text. These considerations broaden usability.

Logging provides observability into CLI operations. Commands log their invocation, key decisions, and results. Log output goes to stderr to not interfere with stdout output. Log verbosity is controllable. This visibility aids debugging and audit.

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

**Solution:** Add Acode to your PATH or use full path.

#### Invalid Configuration

```
Error [ACODE-CFG-001]: Invalid configuration
  Path: models.default
  Value: invalid-model
  Expected: Valid model ID
```

**Solution:** Check configuration file or environment variables.

#### Model Unavailable

```
Error [ACODE-MDL-001]: Model unavailable
  Model: llama3.2:70b
  Suggestion: Start model with 'ollama run llama3.2:70b'
```

**Solution:** Ensure model is running and accessible.

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

## Testing Requirements

### Unit Tests

```
Tests/Unit/CLI/
├── CommandRouterTests.cs
│   ├── Should_Route_Known_Command()
│   ├── Should_Error_On_Unknown_Command()
│   └── Should_Suggest_Similar_Command()
│
├── ArgumentParserTests.cs
│   ├── Should_Parse_Long_Options()
│   ├── Should_Parse_Short_Options()
│   ├── Should_Parse_Arguments()
│   └── Should_Handle_Mixed_Input()
│
├── OutputFormatterTests.cs
│   ├── Should_Format_Table()
│   ├── Should_Format_JSONL()
│   └── Should_Handle_Wide_Content()
│
└── ConfigPrecedenceTests.cs
    ├── Should_CLI_Override_Env()
    ├── Should_Env_Override_File()
    └── Should_Use_Defaults()
```

### Integration Tests

```
Tests/Integration/CLI/
├── CommandExecutionTests.cs
│   ├── Should_Execute_Help()
│   ├── Should_Execute_Version()
│   └── Should_Execute_Status()
```

### E2E Tests

```
Tests/E2E/CLI/
├── CLIEndToEndTests.cs
│   ├── Should_Run_Full_Workflow()
│   ├── Should_Handle_Interruption()
│   └── Should_Resume_Session()
```

### Performance Tests

- PERF-001: Startup < 500ms
- PERF-002: Help output < 100ms
- PERF-003: Argument parsing < 50ms

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