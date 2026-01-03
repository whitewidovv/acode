# Task 007.a: JSON Schema Definitions for All Core Tools

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 007 (Tool Schema Registry), Task 003 (Security Layer), Task 001, Task 002  

---

## Description

Task 007.a defines the complete JSON Schema specifications for all core tools in the Acode system. These schemas serve as the authoritative contracts for tool parameters, enabling strict validation, model prompt construction, and structured output enforcement. Every tool argument passed by the model MUST conform to its defined schema before execution.

Core tools are the built-in capabilities that enable Acode to interact with the development environment. These include file system operations (read, write, list, search), code execution (run commands, scripts), code analysis (semantic search, symbol lookup), version control (git operations), and user interaction (ask questions, confirm actions). Each tool requires precise parameter definitions to function correctly and safely.

The schemas follow JSON Schema Draft 2020-12, the same version supported by the Tool Schema Registry (Task 007) and vLLM's structured output enforcement (Task 006.b). This alignment ensures schemas work consistently across all integration points. The schemas define types, constraints, required fields, and documentation that models use to generate valid arguments.

Each schema includes detailed descriptions for every parameter. These descriptions are included in model prompts and help the model understand how to use each tool correctly. Good descriptions reduce tool call errors and improve model behavior. Descriptions MUST be clear, concise, and include examples where helpful.

Security considerations inform schema design. File paths are constrained to prevent directory traversal. Command execution tools have restricted options. Sensitive operations require explicit confirmation flags. The Security Layer (Task 003) enforces these constraints at runtime, but schemas provide the first line of defense by rejecting clearly invalid input.

The core tools are organized into categories: File Operations, Code Execution, Code Analysis, Version Control, and User Interaction. Each category groups related tools with consistent naming conventions and parameter patterns. This organization helps models learn tool usage patterns and enables category-based filtering in the registry.

Versioning enables schema evolution. Each schema has a version number following semver conventions. When schemas change, the version increments. Breaking changes (removing fields, changing types) require major version bumps. Additive changes (new optional fields) require minor bumps. The registry tracks versions and can support multiple versions for backwards compatibility.

The schemas are defined in code as part of the CoreToolsProvider (from Task 007). This provider registers all core tool schemas during application startup. Schemas are compiled and cached for fast validation. The code-based approach enables IDE support, compile-time checking, and version control integration.

Testing validates that schemas are well-formed, that example arguments pass validation, and that invalid arguments are correctly rejected. Each tool has test cases for valid usage, missing required fields, wrong types, and constraint violations. Schema testing runs as part of the standard test suite.

Documentation is generated from schemas. The CLI's `acode tools show <name>` command displays schema details in human-readable format. Schema descriptions appear in generated documentation. This ensures documentation stays synchronized with actual tool behavior.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Core Tool | Built-in Acode capability |
| Tool Schema | JSON Schema defining tool parameters |
| Parameter | Input field for a tool |
| Required Field | Parameter that must be provided |
| Optional Field | Parameter that may be omitted |
| Default Value | Value used when optional field omitted |
| Type Constraint | Restriction on field data type |
| Enum Constraint | Restriction to specific allowed values |
| Pattern Constraint | Regex pattern field must match |
| Min/Max Constraint | Numeric value bounds |
| Length Constraint | String length bounds |
| Nested Object | Object-type field with sub-fields |
| Array Items | Schema for array elements |
| Description | Human-readable field explanation |
| File Operations | Tools for file system access |
| Code Execution | Tools for running commands |
| Code Analysis | Tools for code understanding |
| Version Control | Tools for git operations |
| User Interaction | Tools for user communication |
| CoreToolsProvider | Class registering core schemas |

---

## Out of Scope

The following items are explicitly excluded from Task 007.a:

- **Tool implementation** - Only schemas, not logic
- **Custom tool schemas** - Task 007 covers config-based
- **Schema validation logic** - Task 007 registry handles
- **Structured output integration** - Task 006.b
- **Security enforcement** - Task 003
- **Tool execution engine** - Separate orchestration
- **Result schemas** - Tool output formats
- **Streaming tool support** - Future enhancement
- **Tool composition** - Chaining tools together
- **Async tool support** - Long-running tools

---

## Functional Requirements

### File Operations Category

#### read_file Tool

- FR-001: MUST define read_file tool schema
- FR-002: MUST require path parameter (string)
- FR-003: MUST support optional start_line (integer, >= 1)
- FR-004: MUST support optional end_line (integer, >= start_line)
- FR-005: MUST support optional encoding (enum: utf-8, ascii, utf-16)
- FR-006: path MUST have maxLength 4096
- FR-007: MUST include descriptions for all parameters

#### write_file Tool

- FR-008: MUST define write_file tool schema
- FR-009: MUST require path parameter (string)
- FR-010: MUST require content parameter (string)
- FR-011: MUST support optional create_directories (boolean, default false)
- FR-012: MUST support optional overwrite (boolean, default true)
- FR-013: MUST support optional encoding (enum)
- FR-014: path MUST have maxLength 4096
- FR-015: content MUST have maxLength 1048576 (1MB)

#### list_directory Tool

- FR-016: MUST define list_directory tool schema
- FR-017: MUST require path parameter (string)
- FR-018: MUST support optional recursive (boolean, default false)
- FR-019: MUST support optional pattern (string glob pattern)
- FR-020: MUST support optional max_depth (integer, >= 1)
- FR-021: MUST support optional include_hidden (boolean, default false)

#### search_files Tool

- FR-022: MUST define search_files tool schema
- FR-023: MUST require query parameter (string)
- FR-024: MUST support optional path (string, default cwd)
- FR-025: MUST support optional pattern (string glob)
- FR-026: MUST support optional case_sensitive (boolean, default false)
- FR-027: MUST support optional regex (boolean, default false)
- FR-028: MUST support optional max_results (integer, default 100)

#### delete_file Tool

- FR-029: MUST define delete_file tool schema
- FR-030: MUST require path parameter (string)
- FR-031: MUST support optional confirm (boolean, default false)
- FR-032: confirm MUST be documented as safety check

#### move_file Tool

- FR-033: MUST define move_file tool schema
- FR-034: MUST require source parameter (string)
- FR-035: MUST require destination parameter (string)
- FR-036: MUST support optional overwrite (boolean, default false)

### Code Execution Category

#### execute_command Tool

- FR-037: MUST define execute_command tool schema
- FR-038: MUST require command parameter (string)
- FR-039: MUST support optional working_directory (string)
- FR-040: MUST support optional timeout_seconds (integer, default 300)
- FR-041: MUST support optional env (object, string keys/values)
- FR-042: timeout_seconds MUST have minimum 1, maximum 3600
- FR-043: command MUST have maxLength 8192

#### execute_script Tool

- FR-044: MUST define execute_script tool schema
- FR-045: MUST require script parameter (string)
- FR-046: MUST require language parameter (enum: powershell, bash, python)
- FR-047: MUST support optional working_directory (string)
- FR-048: MUST support optional timeout_seconds (integer)
- FR-049: script MUST have maxLength 65536

### Code Analysis Category

#### semantic_search Tool

- FR-050: MUST define semantic_search tool schema
- FR-051: MUST require query parameter (string)
- FR-052: MUST support optional scope (enum: workspace, directory, file)
- FR-053: MUST support optional path (string for directory/file scope)
- FR-054: MUST support optional max_results (integer, default 20)
- FR-055: query MUST have minLength 3

#### find_symbol Tool

- FR-056: MUST define find_symbol tool schema
- FR-057: MUST require symbol_name parameter (string)
- FR-058: MUST support optional symbol_type (enum: class, method, function, variable)
- FR-059: MUST support optional path (string, scope)
- FR-060: MUST support optional include_references (boolean, default false)

#### get_definition Tool

- FR-061: MUST define get_definition tool schema
- FR-062: MUST require file_path parameter (string)
- FR-063: MUST require line parameter (integer, >= 1)
- FR-064: MUST require column parameter (integer, >= 1)

### Version Control Category

#### git_status Tool

- FR-065: MUST define git_status tool schema
- FR-066: MUST support optional path (string, default cwd)
- FR-067: All parameters optional (simple status check)

#### git_diff Tool

- FR-068: MUST define git_diff tool schema
- FR-069: MUST support optional path (string, specific file)
- FR-070: MUST support optional staged (boolean, default false)
- FR-071: MUST support optional commit (string, ref to diff against)

#### git_log Tool

- FR-072: MUST define git_log tool schema
- FR-073: MUST support optional count (integer, default 10)
- FR-074: MUST support optional path (string, filter by file)
- FR-075: MUST support optional author (string, filter by author)
- FR-076: count MUST have minimum 1, maximum 100

#### git_commit Tool

- FR-077: MUST define git_commit tool schema
- FR-078: MUST require message parameter (string)
- FR-079: MUST support optional files (array of strings)
- FR-080: MUST support optional all (boolean, default false)
- FR-081: message MUST have minLength 1, maxLength 500

### User Interaction Category

#### ask_user Tool

- FR-082: MUST define ask_user tool schema
- FR-083: MUST require question parameter (string)
- FR-084: MUST support optional options (array of strings)
- FR-085: MUST support optional default_option (string)
- FR-086: question MUST have minLength 1, maxLength 500

#### confirm_action Tool

- FR-087: MUST define confirm_action tool schema
- FR-088: MUST require action_description parameter (string)
- FR-089: MUST support optional destructive (boolean, default false)
- FR-090: action_description MUST have minLength 10

---

## Non-Functional Requirements

### Consistency

- NFR-001: All schemas MUST follow same structural patterns
- NFR-002: All path parameters MUST have maxLength 4096
- NFR-003: All timeout parameters MUST have min 1, max 3600
- NFR-004: All schemas MUST have descriptions for every field
- NFR-005: Boolean defaults MUST be explicitly documented

### Documentation

- NFR-006: Every tool MUST have top-level description
- NFR-007: Every parameter MUST have description
- NFR-008: Descriptions MUST be < 500 characters
- NFR-009: Descriptions MUST include examples where helpful
- NFR-010: Descriptions MUST NOT use jargon

### Security

- NFR-011: Paths MUST NOT allow patterns like "../"
- NFR-012: Commands MUST have reasonable length limits
- NFR-013: Sensitive tools MUST have confirm parameters
- NFR-014: Defaults MUST be safe (not destructive)

### Performance

- NFR-015: All schemas MUST compile in < 10ms each
- NFR-016: All schemas MUST validate in < 1ms
- NFR-017: Total schema definitions < 500KB JSON

### Maintainability

- NFR-018: Schemas MUST be version-controlled
- NFR-019: Schemas MUST have unit tests
- NFR-020: Breaking changes MUST bump major version
- NFR-021: All schemas MUST have example arguments

---

## User Manual Documentation

### Overview

Core tools are the built-in capabilities Acode uses to interact with your development environment. Each tool has a defined schema specifying what parameters it accepts.

### Tool Categories

#### File Operations

| Tool | Description |
|------|-------------|
| read_file | Read contents of a file |
| write_file | Write content to a file |
| list_directory | List directory contents |
| search_files | Search for text in files |
| delete_file | Delete a file |
| move_file | Move or rename a file |

#### Code Execution

| Tool | Description |
|------|-------------|
| execute_command | Run a shell command |
| execute_script | Run a script (PowerShell/Bash/Python) |

#### Code Analysis

| Tool | Description |
|------|-------------|
| semantic_search | Search code semantically |
| find_symbol | Find symbol definition |
| get_definition | Get definition at location |

#### Version Control

| Tool | Description |
|------|-------------|
| git_status | Show git status |
| git_diff | Show git diff |
| git_log | Show git log |
| git_commit | Create git commit |

#### User Interaction

| Tool | Description |
|------|-------------|
| ask_user | Ask user a question |
| confirm_action | Request user confirmation |

### Schema Details

#### read_file

```json
{
    "name": "read_file",
    "description": "Read the contents of a file from the file system",
    "version": "1.0.0",
    "parameters": {
        "type": "object",
        "properties": {
            "path": {
                "type": "string",
                "description": "Path to the file to read (absolute or relative to workspace)",
                "maxLength": 4096
            },
            "start_line": {
                "type": "integer",
                "description": "Line number to start reading from (1-indexed)",
                "minimum": 1
            },
            "end_line": {
                "type": "integer",
                "description": "Line number to stop reading at (inclusive)",
                "minimum": 1
            },
            "encoding": {
                "type": "string",
                "description": "Text encoding for reading the file",
                "enum": ["utf-8", "ascii", "utf-16"],
                "default": "utf-8"
            }
        },
        "required": ["path"]
    }
}
```

#### write_file

```json
{
    "name": "write_file",
    "description": "Write content to a file, creating it if it doesn't exist",
    "version": "1.0.0",
    "parameters": {
        "type": "object",
        "properties": {
            "path": {
                "type": "string",
                "description": "Path where the file will be written",
                "maxLength": 4096
            },
            "content": {
                "type": "string",
                "description": "Content to write to the file",
                "maxLength": 1048576
            },
            "create_directories": {
                "type": "boolean",
                "description": "Create parent directories if they don't exist",
                "default": false
            },
            "overwrite": {
                "type": "boolean",
                "description": "Overwrite file if it already exists",
                "default": true
            }
        },
        "required": ["path", "content"]
    }
}
```

#### execute_command

```json
{
    "name": "execute_command",
    "description": "Execute a shell command and return its output",
    "version": "1.0.0",
    "parameters": {
        "type": "object",
        "properties": {
            "command": {
                "type": "string",
                "description": "The command to execute",
                "maxLength": 8192
            },
            "working_directory": {
                "type": "string",
                "description": "Directory to run the command in",
                "maxLength": 4096
            },
            "timeout_seconds": {
                "type": "integer",
                "description": "Maximum time to wait for command completion",
                "minimum": 1,
                "maximum": 3600,
                "default": 300
            },
            "env": {
                "type": "object",
                "description": "Environment variables to set",
                "additionalProperties": { "type": "string" }
            }
        },
        "required": ["command"]
    }
}
```

### CLI Commands

```bash
# List all core tools
$ acode tools list --category core

# Show specific tool schema
$ acode tools show read_file

# Validate tool arguments
$ echo '{"path": "README.md"}' | acode tools validate read_file
```

---

## Acceptance Criteria

### File Operations

- [ ] AC-001: read_file schema defined
- [ ] AC-002: read_file.path required
- [ ] AC-003: read_file.start_line optional integer
- [ ] AC-004: read_file.end_line optional integer
- [ ] AC-005: read_file.encoding enum
- [ ] AC-006: write_file schema defined
- [ ] AC-007: write_file.path required
- [ ] AC-008: write_file.content required
- [ ] AC-009: write_file.content maxLength 1MB
- [ ] AC-010: list_directory schema defined
- [ ] AC-011: list_directory.path required
- [ ] AC-012: list_directory.recursive optional
- [ ] AC-013: search_files schema defined
- [ ] AC-014: search_files.query required
- [ ] AC-015: search_files.max_results optional
- [ ] AC-016: delete_file schema defined
- [ ] AC-017: delete_file.confirm optional
- [ ] AC-018: move_file schema defined
- [ ] AC-019: move_file source/destination required

### Code Execution

- [ ] AC-020: execute_command schema defined
- [ ] AC-021: execute_command.command required
- [ ] AC-022: execute_command.timeout bounded
- [ ] AC-023: execute_script schema defined
- [ ] AC-024: execute_script.language enum
- [ ] AC-025: execute_script.script maxLength

### Code Analysis

- [ ] AC-026: semantic_search schema defined
- [ ] AC-027: semantic_search.query required
- [ ] AC-028: semantic_search.query minLength
- [ ] AC-029: find_symbol schema defined
- [ ] AC-030: find_symbol.symbol_name required
- [ ] AC-031: get_definition schema defined
- [ ] AC-032: get_definition line/column required

### Version Control

- [ ] AC-033: git_status schema defined
- [ ] AC-034: git_diff schema defined
- [ ] AC-035: git_log schema defined
- [ ] AC-036: git_log.count bounded
- [ ] AC-037: git_commit schema defined
- [ ] AC-038: git_commit.message required

### User Interaction

- [ ] AC-039: ask_user schema defined
- [ ] AC-040: ask_user.question required
- [ ] AC-041: confirm_action schema defined
- [ ] AC-042: confirm_action description required

### Constraints

- [ ] AC-043: All paths maxLength 4096
- [ ] AC-044: All timeouts bounded
- [ ] AC-045: All fields have descriptions
- [ ] AC-046: All defaults documented
- [ ] AC-047: All enums defined

### Documentation

- [ ] AC-048: All tools have descriptions
- [ ] AC-049: All parameters have descriptions
- [ ] AC-050: Example arguments exist
- [ ] AC-051: CLI show works for all

### Versioning

- [ ] AC-052: All schemas have versions
- [ ] AC-053: Versions follow semver
- [ ] AC-054: Versions are 1.0.0 initially

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Infrastructure/ToolSchemas/CoreTools/
├── FileOperationsSchemaTests.cs
│   ├── ReadFile_Should_Validate_Path()
│   ├── ReadFile_Should_Reject_Missing_Path()
│   ├── WriteFile_Should_Validate_Content()
│   ├── WriteFile_Should_Enforce_MaxLength()
│   └── ListDirectory_Should_Validate_Recursive()
│
├── CodeExecutionSchemaTests.cs
│   ├── ExecuteCommand_Should_Validate_Command()
│   ├── ExecuteCommand_Should_Enforce_Timeout_Bounds()
│   └── ExecuteScript_Should_Validate_Language()
│
├── CodeAnalysisSchemaTests.cs
│   ├── SemanticSearch_Should_Require_Query()
│   ├── SemanticSearch_Should_Enforce_MinLength()
│   └── FindSymbol_Should_Validate_SymbolName()
│
├── VersionControlSchemaTests.cs
│   ├── GitLog_Should_Enforce_Count_Bounds()
│   └── GitCommit_Should_Require_Message()
│
└── UserInteractionSchemaTests.cs
    ├── AskUser_Should_Require_Question()
    └── ConfirmAction_Should_Require_Description()
```

### Integration Tests

```
Tests/Integration/ToolSchemas/
├── CoreToolsRegistrationTests.cs
│   ├── Should_Register_All_Core_Tools()
│   └── Should_Validate_Example_Arguments()
```

---

## User Verification Steps

### Scenario 1: Read File Schema

1. Run `acode tools show read_file`
2. Verify: path shown as required
3. Verify: start_line, end_line optional
4. Verify: encoding enum shown

### Scenario 2: Write File Validation

1. Validate `{"path": "test.txt", "content": "hello"}`
2. Verify: Validation passes
3. Validate `{"content": "hello"}` (missing path)
4. Verify: Validation fails

### Scenario 3: Execute Command Bounds

1. Validate timeout_seconds: 0
2. Verify: Fails (minimum 1)
3. Validate timeout_seconds: 9999
4. Verify: Fails (maximum 3600)

### Scenario 4: All Tools Registered

1. Run `acode tools list`
2. Verify: All core tools listed
3. Verify: Categories shown

### Scenario 5: Schema Versions

1. Run `acode tools show` for each tool
2. Verify: Version is 1.0.0
3. Verify: Version format correct

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Infrastructure/ToolSchemas/Providers/
├── CoreToolsProvider.cs
├── Schemas/
│   ├── FileOperations/
│   │   ├── ReadFileSchema.cs
│   │   ├── WriteFileSchema.cs
│   │   ├── ListDirectorySchema.cs
│   │   ├── SearchFilesSchema.cs
│   │   ├── DeleteFileSchema.cs
│   │   └── MoveFileSchema.cs
│   ├── CodeExecution/
│   │   ├── ExecuteCommandSchema.cs
│   │   └── ExecuteScriptSchema.cs
│   ├── CodeAnalysis/
│   │   ├── SemanticSearchSchema.cs
│   │   ├── FindSymbolSchema.cs
│   │   └── GetDefinitionSchema.cs
│   ├── VersionControl/
│   │   ├── GitStatusSchema.cs
│   │   ├── GitDiffSchema.cs
│   │   ├── GitLogSchema.cs
│   │   └── GitCommitSchema.cs
│   └── UserInteraction/
│       ├── AskUserSchema.cs
│       └── ConfirmActionSchema.cs
```

### CoreToolsProvider Implementation

```csharp
namespace AgenticCoder.Infrastructure.ToolSchemas.Providers;

public sealed class CoreToolsProvider : ISchemaProvider
{
    public void RegisterSchemas(IToolSchemaRegistry registry)
    {
        // File Operations
        registry.RegisterTool(ReadFileSchema.Create());
        registry.RegisterTool(WriteFileSchema.Create());
        registry.RegisterTool(ListDirectorySchema.Create());
        registry.RegisterTool(SearchFilesSchema.Create());
        registry.RegisterTool(DeleteFileSchema.Create());
        registry.RegisterTool(MoveFileSchema.Create());
        
        // Code Execution
        registry.RegisterTool(ExecuteCommandSchema.Create());
        registry.RegisterTool(ExecuteScriptSchema.Create());
        
        // Code Analysis
        registry.RegisterTool(SemanticSearchSchema.Create());
        registry.RegisterTool(FindSymbolSchema.Create());
        registry.RegisterTool(GetDefinitionSchema.Create());
        
        // Version Control
        registry.RegisterTool(GitStatusSchema.Create());
        registry.RegisterTool(GitDiffSchema.Create());
        registry.RegisterTool(GitLogSchema.Create());
        registry.RegisterTool(GitCommitSchema.Create());
        
        // User Interaction
        registry.RegisterTool(AskUserSchema.Create());
        registry.RegisterTool(ConfirmActionSchema.Create());
    }
}
```

### Schema Class Pattern

```csharp
namespace AgenticCoder.Infrastructure.ToolSchemas.Providers.Schemas.FileOperations;

public static class ReadFileSchema
{
    public static ToolDefinition Create() => new()
    {
        Name = "read_file",
        Description = "Read the contents of a file from the file system",
        Version = "1.0.0",
        Category = ToolCategory.FileOperations,
        Parameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "path": {
                    "type": "string",
                    "description": "Path to the file to read",
                    "maxLength": 4096
                },
                "start_line": {
                    "type": "integer",
                    "description": "Line number to start reading (1-indexed)",
                    "minimum": 1
                },
                "end_line": {
                    "type": "integer",
                    "description": "Line number to stop reading (inclusive)",
                    "minimum": 1
                },
                "encoding": {
                    "type": "string",
                    "description": "Text encoding",
                    "enum": ["utf-8", "ascii", "utf-16"],
                    "default": "utf-8"
                }
            },
            "required": ["path"]
        }
        """).RootElement
    };
}
```

### Implementation Checklist

1. [ ] Create CoreToolsProvider class
2. [ ] Create ReadFileSchema
3. [ ] Create WriteFileSchema
4. [ ] Create ListDirectorySchema
5. [ ] Create SearchFilesSchema
6. [ ] Create DeleteFileSchema
7. [ ] Create MoveFileSchema
8. [ ] Create ExecuteCommandSchema
9. [ ] Create ExecuteScriptSchema
10. [ ] Create SemanticSearchSchema
11. [ ] Create FindSymbolSchema
12. [ ] Create GetDefinitionSchema
13. [ ] Create GitStatusSchema
14. [ ] Create GitDiffSchema
15. [ ] Create GitLogSchema
16. [ ] Create GitCommitSchema
17. [ ] Create AskUserSchema
18. [ ] Create ConfirmActionSchema
19. [ ] Register CoreToolsProvider in DI
20. [ ] Write unit tests for each schema
21. [ ] Add example arguments for testing
22. [ ] Add XML documentation

### Dependencies

- Task 007 (Tool Schema Registry)
- Task 003 (Security constraints)

### Verification Command

```bash
dotnet test --filter "FullyQualifiedName~CoreTools"
```

---

**End of Task 007.a Specification**