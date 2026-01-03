# Task 010.a: Command Routing + Help Output Standard

**Priority:** P0 – Critical Path  
**Tier:** Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 010 (CLI Command Framework), Task 002 (.agent/config.yml)  

---

## Description

Task 010.a implements the command routing system and standardized help output for the Acode CLI. Command routing is the mechanism that maps user input to executable commands. Help output provides discoverable documentation for all CLI functionality. Together, these systems ensure users can effectively navigate and learn the CLI.

Command routing acts as the traffic controller for the CLI. When a user types `acode run`, the router identifies "run" as the command, locates the RunCommand implementation, and dispatches execution. The router handles command aliases, subcommand hierarchies, and unknown command errors. Fast and accurate routing is essential—users expect immediate response to their input.

The routing system employs a hierarchical structure. Top-level commands (run, chat, config) are registered at the root. Subcommands (config get, config set) are nested under their parent. This hierarchy enables intuitive command organization while keeping individual commands focused. The router traverses this hierarchy to find the target command.

Unknown command handling provides a helpful experience. When users mistype commands, the router doesn't just fail—it suggests similar commands. "Did you mean 'run'?" improves usability significantly. The router uses edit distance algorithms to find close matches, helping users recover from typos quickly.

Help output follows established conventions. The format matches what users expect from modern CLI tools: description, usage pattern, options list, examples, related commands. Consistency with tools like git, docker, and npm reduces learning curve. Users apply their existing knowledge.

Help is generated, not manually written. Commands implement a structured interface that the help generator uses. This ensures consistency across all commands and eliminates the common problem of outdated help documentation. When commands change, help automatically reflects those changes.

The help system is contextual. Global help (`acode --help`) provides an overview and lists all commands. Command help (`acode run --help`) focuses on that specific command. Subcommand help goes deeper. This layered approach lets users drill down to the detail level they need.

Terminal width adaptation ensures help looks good everywhere. On narrow terminals, text wraps appropriately. On wide terminals, content spreads to improve readability. Tables adjust column widths. This responsiveness handles the diversity of terminal environments users operate in.

Color and formatting enhance readability. Command names are highlighted. Required vs optional parameters are distinguished. Examples stand out from prose. However, colors are strictly optional—they're disabled automatically in non-TTY contexts and can be disabled manually with --no-color.

Internationalization is considered but not implemented in MVP. The help system is structured to support future localization. All strings are externalized. Date and number formatting is locale-aware. This preparation avoids costly refactoring later, though English-only is the current scope.

The routing and help systems integrate tightly with configuration. The router respects operating modes—some commands may be unavailable in certain modes. Help reflects current configuration, showing only options that apply. This dynamic behavior keeps the CLI coherent.

Performance is critical for both systems. Routing must complete in under 10ms—users shouldn't perceive any delay between pressing Enter and seeing output. Help generation must complete in under 100ms. These targets require efficient data structures and lazy loading of command implementations.

Testing verifies both systems extensively. Unit tests cover routing logic and help generation. Integration tests verify the full path from user input to command execution. Property-based tests ensure routing handles edge cases. The help system is tested for format consistency across all commands.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Command Router | Component that maps input to commands |
| Command Registry | Store of available commands |
| Command Hierarchy | Tree structure of commands/subcommands |
| Route Resolution | Process of finding target command |
| Help Generator | Creates help text from command metadata |
| Help Template | Format structure for help output |
| Edit Distance | Algorithm for string similarity |
| Fuzzy Matching | Finding similar strings |
| Terminal Width | Number of columns available |
| Word Wrapping | Breaking text at word boundaries |
| ANSI Escape Codes | Control sequences for formatting |
| TTY Detection | Determining if output is terminal |
| Command Alias | Alternative name for a command |
| Subcommand | Nested command under parent |
| Usage Pattern | Syntax description for a command |

---

## Out of Scope

The following items are explicitly excluded from Task 010.a:

- **Actual command implementation** - Routing only
- **JSONL output mode** - Task 010.b
- **Non-interactive behaviors** - Task 010.c
- **Multi-language help** - English only
- **Interactive tutorials** - Simple help only
- **Man page generation** - Post-MVP
- **Shell completion scripts** - Post-MVP
- **Web-based documentation** - Post-MVP
- **Command recording/replay** - Post-MVP
- **Plugin command loading** - Fixed command set

---

## Functional Requirements

### Command Registration

- FR-001: Commands MUST be registered with unique names
- FR-002: Command names MUST be lowercase alphanumeric
- FR-003: Commands MAY have aliases (e.g., "r" for "run")
- FR-004: Aliases MUST be unique across all commands
- FR-005: Commands MUST declare their subcommands
- FR-006: Subcommands MUST be registered under parent
- FR-007: Maximum nesting depth MUST be 2 (cmd sub)
- FR-008: Registration MUST fail on duplicate names

### Route Resolution

- FR-009: Router MUST find command by exact name
- FR-010: Router MUST find command by alias
- FR-011: Router MUST traverse subcommand hierarchy
- FR-012: Router MUST return null for unknown commands
- FR-013: Router MUST be case-insensitive
- FR-014: Router MUST trim whitespace from input
- FR-015: Route resolution MUST complete in < 10ms
- FR-016: Router MUST handle empty input gracefully

### Unknown Command Handling

- FR-017: Unknown commands MUST show error message
- FR-018: Error MUST include the unknown command name
- FR-019: Router MUST suggest similar commands
- FR-020: Suggestions MUST use edit distance algorithm
- FR-021: Maximum 3 suggestions MUST be shown
- FR-022: Suggestions MUST be ranked by similarity
- FR-023: Minimum similarity threshold: 60%
- FR-024: No suggestions if none meet threshold

### Command Metadata

- FR-025: Commands MUST have Name property
- FR-026: Commands MUST have Description property
- FR-027: Commands MUST have Usage property
- FR-028: Commands MAY have Aliases list
- FR-029: Commands MUST have Options list
- FR-030: Commands MAY have Examples list
- FR-031: Commands MAY have RelatedCommands list
- FR-032: Commands MUST have Visible property

### Help Generation

- FR-033: Help MUST be generated from metadata
- FR-034: Help MUST NOT be manually written prose
- FR-035: Help generator MUST use consistent template
- FR-036: Template MUST include all metadata fields
- FR-037: Help MUST adapt to terminal width
- FR-038: Minimum supported width: 40 columns
- FR-039: Maximum content width: 120 columns
- FR-040: Help MUST wrap text at word boundaries

### Help Content Sections

- FR-041: Help MUST include NAME section
- FR-042: Help MUST include DESCRIPTION section
- FR-043: Help MUST include USAGE section
- FR-044: Help MUST include OPTIONS section
- FR-045: Help MAY include EXAMPLES section
- FR-046: Help MAY include SEE ALSO section
- FR-047: Sections MUST appear in consistent order
- FR-048: Empty sections MUST be omitted

### Options Display

- FR-049: Options MUST show short and long forms
- FR-050: Options MUST show value placeholder if required
- FR-051: Options MUST show description
- FR-052: Options MUST show default value if any
- FR-053: Required options MUST be marked [required]
- FR-054: Options MUST be grouped logically
- FR-055: Global options MUST be separated
- FR-056: Options MUST be alphabetically sorted within groups

### Examples Display

- FR-057: Examples MUST include command line
- FR-058: Examples MUST include description
- FR-059: Examples MUST be executable as shown
- FR-060: Examples MUST use realistic values
- FR-061: Minimum 2 examples per command
- FR-062: Examples MUST progress simple to complex

### Global Help

- FR-063: `acode --help` MUST show all commands
- FR-064: Commands MUST be grouped by category
- FR-065: Each command MUST show one-line description
- FR-066: Help MUST show global options
- FR-067: Help MUST show version information
- FR-068: Help MUST show documentation URL

### Command Help

- FR-069: `acode <cmd> --help` MUST show command help
- FR-070: `acode help <cmd>` MUST show same content
- FR-071: Help MUST show full description
- FR-072: Help MUST show all command options
- FR-073: Help MUST show all examples
- FR-074: Help MUST show related commands

### Formatting

- FR-075: Colors MUST be disabled in non-TTY
- FR-076: --no-color MUST disable colors
- FR-077: NO_COLOR env var MUST disable colors
- FR-078: FORCE_COLOR MUST enable colors in non-TTY
- FR-079: Colors MUST use standard ANSI codes
- FR-080: Colors MUST be readable on light/dark terminals
- FR-081: Bold MUST highlight command names
- FR-082: Underline MUST indicate arguments

### Error Messages

- FR-083: Errors MUST be written to stderr
- FR-084: Errors MUST include error code
- FR-085: Errors MUST be actionable
- FR-086: Errors MUST NOT suggest unavailable commands

---

## Non-Functional Requirements

### Performance

- NFR-001: Route resolution MUST complete in < 10ms
- NFR-002: Help generation MUST complete in < 100ms
- NFR-003: Memory for routing MUST be < 1MB
- NFR-004: Command registration MUST be O(1)
- NFR-005: Fuzzy matching MUST complete in < 50ms

### Reliability

- NFR-006: Invalid input MUST NOT crash router
- NFR-007: Missing metadata MUST NOT crash help
- NFR-008: Terminal resize MUST NOT corrupt output

### Accessibility

- NFR-009: Help MUST be screen reader compatible
- NFR-010: Colors MUST NOT be sole indicator
- NFR-011: Output MUST work without Unicode
- NFR-012: ASCII fallbacks MUST be available

### Maintainability

- NFR-013: Adding commands MUST NOT change router
- NFR-014: Help template MUST be centralized
- NFR-015: Formatting logic MUST be encapsulated

### Security

- NFR-016: Command names MUST be validated
- NFR-017: User input MUST be sanitized in logs

---

## User Manual Documentation

### Overview

The Acode CLI uses a command-based structure with consistent help documentation. This guide covers how to navigate commands and get help.

### Getting Help

#### Global Help

View all available commands:

```bash
$ acode --help
$ acode -h

Acode - Agentic Coding Assistant
Version: 1.0.0

USAGE:
  acode [global-options] <command> [options] [arguments]

COMMANDS:
  run       Start an agent run
  resume    Resume interrupted run
  chat      Manage conversations
  models    Manage model configuration
  prompts   Manage prompt packs
  config    Manage configuration
  status    Show current status
  db        Database operations
  help      Show help for commands

GLOBAL OPTIONS:
  -h, --help           Show help
  -v, --verbose        Verbose output
  -q, --quiet          Minimal output
  --version            Show version
  --config <path>      Use config file
  --json               JSONL output
  --no-color           Disable colors

For more information: https://docs.acode.dev/cli
```

#### Command Help

Get help for a specific command:

```bash
$ acode run --help
$ acode run -h
$ acode help run

NAME:
  acode run - Start an agent run

DESCRIPTION:
  Starts a new agent run with the specified task. The agent
  analyzes the task, plans actions, and executes them with
  approval gates as configured.

USAGE:
  acode run [options] <task>

ARGUMENTS:
  task    Task description (required)

OPTIONS:
  -m, --model <id>      Override default model
  -n, --max-steps <n>   Maximum steps (default: 50)
  -y, --yes             Skip approval prompts
  --dry-run             Preview without executing
  --continue            Continue previous session

EXAMPLES:
  # Simple task
  $ acode run "Fix the login bug"

  # With model override
  $ acode run --model llama3.2:70b "Refactor UserService"

  # Preview mode
  $ acode run --dry-run "Add validation"

SEE ALSO:
  resume, status, chat
```

### Command Structure

Commands follow a consistent pattern:

```
acode [global-options] <command> [subcommand] [options] [arguments]
```

**Components:**

| Component | Description | Example |
|-----------|-------------|---------|
| Global options | Apply to all commands | `--verbose` |
| Command | Primary action | `run` |
| Subcommand | Nested action | `config get` |
| Options | Modify behavior | `--model llama3.2` |
| Arguments | Input values | `"Fix the bug"` |

### Command Categories

#### Session Management

| Command | Description |
|---------|-------------|
| `run` | Start agent run |
| `resume` | Resume interrupted run |
| `status` | Show current status |

#### Configuration

| Command | Description |
|---------|-------------|
| `config` | Manage configuration |
| `models` | Manage models |
| `prompts` | Manage prompt packs |

#### Conversations

| Command | Description |
|---------|-------------|
| `chat` | Manage conversations |

#### Data

| Command | Description |
|---------|-------------|
| `db` | Database operations |

### Option Formats

Options support multiple formats:

```bash
# Long form with equals
$ acode run --model=llama3.2:7b "task"

# Long form with space
$ acode run --model llama3.2:7b "task"

# Short form with space
$ acode run -m llama3.2:7b "task"

# Short form combined
$ acode run -vy "task"  # --verbose --yes
```

### Boolean Options

Boolean options can be negated:

```bash
# Enable (default may vary)
$ acode run --approve "task"

# Disable explicitly
$ acode run --no-approve "task"
```

### Subcommands

Some commands have subcommands:

```bash
# List subcommands
$ acode config --help

SUBCOMMANDS:
  get     Get configuration value
  set     Set configuration value
  list    List all configuration
  reset   Reset to defaults

# Use subcommand
$ acode config get models.default

# Subcommand help
$ acode config get --help
```

### Unknown Commands

When you mistype a command:

```bash
$ acode rnu "task"

Error [ACODE-CLI-001]: Unknown command 'rnu'

Did you mean:
  run     Start an agent run

Run 'acode --help' for available commands.
```

### Color and Formatting

#### Color Control

```bash
# Force disable colors
$ acode --no-color run --help

# Via environment
$ export NO_COLOR=1
$ acode run --help

# Force colors in non-TTY
$ export FORCE_COLOR=1
$ acode run --help | less -R
```

#### Terminal Width

Help adapts to terminal width:

```bash
# Narrow terminal (80 columns)
OPTIONS:
  -m, --model <id>
      Override the default model for this
      run. See 'acode models list'.

# Wide terminal (120+ columns)
OPTIONS:
  -m, --model <id>    Override the default model for this run. See 'acode models list'.
```

### Best Practices

1. **Start with help**: `acode --help` for overview
2. **Drill down**: `acode <cmd> --help` for details
3. **Check examples**: Examples show real usage
4. **Use related commands**: SEE ALSO links related features

### Troubleshooting

#### Help Not Displaying

**Problem:** Help output is empty or garbled.

**Solutions:**
1. Check terminal encoding: `echo $LANG`
2. Try `--no-color` flag
3. Redirect to file: `acode --help > help.txt`

#### Unknown Command When Valid

**Problem:** Known command shows as unknown.

**Solutions:**
1. Check for typos
2. Check operating mode restrictions
3. Update Acode to latest version

---

## Acceptance Criteria

### Command Registration

- [ ] AC-001: Commands registered with unique names
- [ ] AC-002: Command names lowercase alphanumeric
- [ ] AC-003: Aliases work correctly
- [ ] AC-004: Alias uniqueness enforced
- [ ] AC-005: Subcommands registered under parent
- [ ] AC-006: Max nesting depth of 2 enforced
- [ ] AC-007: Duplicate registration fails

### Route Resolution

- [ ] AC-008: Exact name matching works
- [ ] AC-009: Alias matching works
- [ ] AC-010: Subcommand hierarchy traversed
- [ ] AC-011: Unknown returns null
- [ ] AC-012: Case-insensitive matching
- [ ] AC-013: Whitespace trimmed
- [ ] AC-014: Routing < 10ms
- [ ] AC-015: Empty input handled

### Unknown Commands

- [ ] AC-016: Error message shown
- [ ] AC-017: Unknown name included
- [ ] AC-018: Similar commands suggested
- [ ] AC-019: Max 3 suggestions shown
- [ ] AC-020: Ranked by similarity
- [ ] AC-021: 60% threshold enforced

### Command Metadata

- [ ] AC-022: Name property required
- [ ] AC-023: Description property required
- [ ] AC-024: Usage property required
- [ ] AC-025: Aliases optional
- [ ] AC-026: Options list required
- [ ] AC-027: Examples optional
- [ ] AC-028: RelatedCommands optional

### Help Generation

- [ ] AC-029: Generated from metadata
- [ ] AC-030: Consistent template used
- [ ] AC-031: Terminal width adapted
- [ ] AC-032: 40-column minimum works
- [ ] AC-033: 120-column maximum works
- [ ] AC-034: Word wrapping correct

### Help Content

- [ ] AC-035: NAME section present
- [ ] AC-036: DESCRIPTION section present
- [ ] AC-037: USAGE section present
- [ ] AC-038: OPTIONS section present
- [ ] AC-039: EXAMPLES section when available
- [ ] AC-040: SEE ALSO section when available
- [ ] AC-041: Section order consistent
- [ ] AC-042: Empty sections omitted

### Options Display

- [ ] AC-043: Short and long forms shown
- [ ] AC-044: Value placeholders shown
- [ ] AC-045: Descriptions shown
- [ ] AC-046: Default values shown
- [ ] AC-047: Required marked
- [ ] AC-048: Logical grouping
- [ ] AC-049: Alphabetical within groups

### Examples

- [ ] AC-050: Command line included
- [ ] AC-051: Description included
- [ ] AC-052: Executable as shown
- [ ] AC-053: Realistic values
- [ ] AC-054: 2+ examples per command

### Global Help

- [ ] AC-055: All commands listed
- [ ] AC-056: Grouped by category
- [ ] AC-057: One-line descriptions
- [ ] AC-058: Global options shown
- [ ] AC-059: Version shown
- [ ] AC-060: Docs URL shown

### Command Help

- [ ] AC-061: --help works
- [ ] AC-062: help <cmd> works
- [ ] AC-063: Full description shown
- [ ] AC-064: All options shown
- [ ] AC-065: All examples shown
- [ ] AC-066: Related commands shown

### Formatting

- [ ] AC-067: Colors disabled non-TTY
- [ ] AC-068: --no-color works
- [ ] AC-069: NO_COLOR env works
- [ ] AC-070: FORCE_COLOR works
- [ ] AC-071: Standard ANSI codes
- [ ] AC-072: Light/dark compatible
- [ ] AC-073: Bold for commands
- [ ] AC-074: Underline for arguments

### Errors

- [ ] AC-075: Errors to stderr
- [ ] AC-076: Error codes included
- [ ] AC-077: Actionable messages
- [ ] AC-078: Only available commands suggested

### Performance

- [ ] AC-079: Route < 10ms
- [ ] AC-080: Help < 100ms
- [ ] AC-081: Routing < 1MB memory
- [ ] AC-082: Fuzzy matching < 50ms

### Accessibility

- [ ] AC-083: Screen reader compatible
- [ ] AC-084: Colors not sole indicator
- [ ] AC-085: ASCII fallbacks work

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/CLI/Routing/
├── CommandRouterTests.cs
│   ├── Should_Find_Command_By_Name()
│   ├── Should_Find_Command_By_Alias()
│   ├── Should_Return_Null_For_Unknown()
│   ├── Should_Be_Case_Insensitive()
│   ├── Should_Trim_Whitespace()
│   ├── Should_Traverse_Subcommands()
│   └── Should_Complete_Under_10ms()
│
├── CommandRegistryTests.cs
│   ├── Should_Register_Command()
│   ├── Should_Reject_Duplicate_Name()
│   ├── Should_Reject_Duplicate_Alias()
│   └── Should_Enforce_Nesting_Limit()
│
├── FuzzyMatcherTests.cs
│   ├── Should_Find_Similar_Commands()
│   ├── Should_Rank_By_Similarity()
│   ├── Should_Limit_To_3_Suggestions()
│   └── Should_Enforce_Threshold()
│
└── HelpGeneratorTests.cs
    ├── Should_Generate_Name_Section()
    ├── Should_Generate_Description_Section()
    ├── Should_Generate_Usage_Section()
    ├── Should_Generate_Options_Section()
    ├── Should_Omit_Empty_Sections()
    ├── Should_Adapt_To_Terminal_Width()
    └── Should_Complete_Under_100ms()
```

### Integration Tests

```
Tests/Integration/CLI/Routing/
├── CommandDispatchTests.cs
│   ├── Should_Route_And_Execute_Run()
│   ├── Should_Route_And_Execute_Help()
│   └── Should_Handle_Unknown_Gracefully()
│
└── HelpIntegrationTests.cs
    ├── Should_Show_Global_Help()
    ├── Should_Show_Command_Help()
    └── Should_Respect_Color_Settings()
```

### E2E Tests

```
Tests/E2E/CLI/
├── HelpE2ETests.cs
│   ├── Should_Display_Help_For_All_Commands()
│   ├── Should_Handle_Terminal_Resize()
│   └── Should_Work_In_Pipe()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Route resolution | 5ms | 10ms |
| Help generation | 50ms | 100ms |
| Fuzzy matching | 25ms | 50ms |
| Full startup | 250ms | 500ms |

### Regression Tests

- Command routing after new command added
- Help format consistency after metadata change
- Color output after terminal library update

---

## User Verification Steps

### Scenario 1: Global Help

1. Run `acode --help`
2. Verify: All commands listed
3. Verify: Categories shown
4. Verify: Global options listed

### Scenario 2: Command Help

1. Run `acode run --help`
2. Verify: Full description shown
3. Verify: All options documented
4. Verify: Examples provided

### Scenario 3: Help Subcommand

1. Run `acode help run`
2. Verify: Same as --help output
3. Verify: Exit code 0

### Scenario 4: Unknown Command

1. Run `acode rnu`
2. Verify: Error message
3. Verify: "run" suggested
4. Verify: Exit code 2

### Scenario 5: Command Alias

1. Run `acode r "task"` (if r is alias for run)
2. Verify: Runs as expected
3. Verify: Help shows alias

### Scenario 6: Subcommand Help

1. Run `acode config get --help`
2. Verify: Subcommand options shown
3. Verify: Parent reference included

### Scenario 7: Color Disabled

1. Run `acode --no-color --help`
2. Verify: No ANSI escape codes
3. Verify: Readable output

### Scenario 8: Piped Output

1. Run `acode --help | cat`
2. Verify: Colors disabled automatically
3. Verify: Clean text output

### Scenario 9: Narrow Terminal

1. Set terminal to 50 columns
2. Run `acode run --help`
3. Verify: Text wraps correctly
4. Verify: Options aligned

### Scenario 10: Wide Terminal

1. Set terminal to 150 columns
2. Run `acode run --help`
3. Verify: Content not too wide
4. Verify: Readable layout

### Scenario 11: NO_COLOR Environment

1. Set `NO_COLOR=1`
2. Run `acode --help`
3. Verify: No colors
4. Unset and verify colors return

### Scenario 12: FORCE_COLOR Environment

1. Run `FORCE_COLOR=1 acode --help | cat`
2. Verify: Colors present in pipe

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.CLI/
├── Routing/
│   ├── ICommandRouter.cs
│   ├── CommandRouter.cs
│   ├── CommandRegistry.cs
│   ├── RouteResult.cs
│   └── FuzzyMatcher.cs
│
├── Help/
│   ├── IHelpGenerator.cs
│   ├── HelpGenerator.cs
│   ├── HelpTemplate.cs
│   ├── HelpSection.cs
│   └── TerminalFormatter.cs
│
├── Commands/
│   ├── ICommand.cs
│   ├── CommandMetadata.cs
│   ├── CommandOption.cs
│   ├── CommandExample.cs
│   └── CommandGroup.cs
│
└── Output/
    ├── ITerminal.cs
    ├── Terminal.cs
    └── ColorSettings.cs
```

### ICommandRouter Interface

```csharp
namespace AgenticCoder.CLI.Routing;

public interface ICommandRouter
{
    RouteResult Route(string[] args);
    IEnumerable<ICommand> GetAllCommands();
    IEnumerable<string> GetSuggestions(string unknown);
}

public sealed record RouteResult(
    ICommand? Command,
    string[] RemainingArgs,
    bool IsUnknown,
    string? UnknownName);
```

### CommandRegistry

```csharp
namespace AgenticCoder.CLI.Routing;

public sealed class CommandRegistry
{
    public void Register(ICommand command);
    public void RegisterSubcommand(string parent, ICommand subcommand);
    public ICommand? Find(string name);
    public IReadOnlyList<ICommand> GetAll();
}
```

### ICommand Interface

```csharp
namespace AgenticCoder.CLI.Commands;

public interface ICommand
{
    CommandMetadata Metadata { get; }
    Task<int> ExecuteAsync(CommandContext context);
}

public sealed record CommandMetadata(
    string Name,
    string Description,
    string Usage,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<CommandOption> Options,
    IReadOnlyList<CommandExample> Examples,
    IReadOnlyList<string> RelatedCommands,
    bool IsVisible = true);
```

### IHelpGenerator Interface

```csharp
namespace AgenticCoder.CLI.Help;

public interface IHelpGenerator
{
    string GenerateGlobalHelp();
    string GenerateCommandHelp(ICommand command);
    void Configure(HelpOptions options);
}

public sealed record HelpOptions(
    int TerminalWidth,
    bool UseColors,
    bool UseUnicode);
```

### FuzzyMatcher

```csharp
namespace AgenticCoder.CLI.Routing;

public sealed class FuzzyMatcher
{
    public IReadOnlyList<string> FindSimilar(
        string input,
        IEnumerable<string> candidates,
        int maxResults = 3,
        double threshold = 0.6);
    
    public double CalculateSimilarity(string a, string b);
}
```

### Error Codes

| Code | Constant | Condition |
|------|----------|-----------|
| ACODE-CLI-001 | UnknownCommand | Command not found |
| ACODE-CLI-002 | AmbiguousCommand | Multiple matches |
| ACODE-CLI-003 | InvalidSubcommand | Invalid subcommand |

### Logging Fields

```json
{
  "event": "command_routed",
  "input": "run",
  "resolved": "RunCommand",
  "aliases_checked": ["r"],
  "duration_ms": 2
}
```

### Help Template Format

```
NAME:
  acode {name} - {one_line_description}

DESCRIPTION:
  {full_description}

USAGE:
  acode {usage_pattern}

{if subcommands}
SUBCOMMANDS:
  {subcommand_list}
{endif}

OPTIONS:
  {options_formatted}

{if examples}
EXAMPLES:
  {examples_formatted}
{endif}

{if related}
SEE ALSO:
  {related_commands}
{endif}
```

### Implementation Checklist

1. [ ] Create ICommandRouter interface
2. [ ] Implement CommandRouter
3. [ ] Create CommandRegistry
4. [ ] Implement FuzzyMatcher with edit distance
5. [ ] Create ICommand interface
6. [ ] Create CommandMetadata record
7. [ ] Create IHelpGenerator interface
8. [ ] Implement HelpGenerator
9. [ ] Create HelpTemplate
10. [ ] Create TerminalFormatter
11. [ ] Implement color handling
12. [ ] Add terminal width detection
13. [ ] Write unit tests for routing
14. [ ] Write unit tests for help
15. [ ] Write integration tests
16. [ ] Add performance benchmarks

### Validation Checklist Before Merge

- [ ] All commands have complete metadata
- [ ] All commands have 2+ examples
- [ ] Help generates in < 100ms
- [ ] Routing completes in < 10ms
- [ ] Colors disabled in non-TTY
- [ ] --no-color flag works
- [ ] NO_COLOR env var works
- [ ] Unknown command suggestions work
- [ ] Unit test coverage > 90%
- [ ] No hardcoded strings in output

### Rollout Plan

1. **Phase 1:** Implement routing core
2. **Phase 2:** Implement help generator
3. **Phase 3:** Add color/formatting
4. **Phase 4:** Add fuzzy matching
5. **Phase 5:** Performance optimization
6. **Phase 6:** Full test coverage

---

**End of Task 010.a Specification**