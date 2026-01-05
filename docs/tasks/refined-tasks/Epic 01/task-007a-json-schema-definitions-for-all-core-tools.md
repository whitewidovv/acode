# Task 007.a: JSON Schema Definitions for All Core Tools

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 007 (Tool Schema Registry), Task 003 (Security Layer), Task 001, Task 002  

---

## Description

### Overview and Business Value

Task 007.a defines the complete JSON Schema specifications for all 18 core tools in the Acode system. These schemas serve as the authoritative contracts for tool parameters, enabling strict validation, model prompt construction, and structured output enforcement. Every tool argument passed by the model MUST conform to its defined schema before execution. Without these precise schema definitions, the system cannot safely execute any tool operations - making this task foundational to all Acode functionality.

The business value of well-defined tool schemas is substantial and measurable. Poor schema definitions lead to three categories of failures: (1) Model confusion where the LLM generates invalid arguments because parameter requirements are unclear (estimated 15-25% of tool calls fail without proper schemas), (2) Security vulnerabilities where missing constraints allow path traversal, command injection, or resource exhaustion attacks, and (3) Developer friction where implementers must reverse-engineer parameter requirements from code rather than documentation. By investing 40-60 hours in comprehensive schema definitions, Acode prevents an estimated 200+ hours of debugging, security remediation, and documentation work over the product lifecycle.

Return on investment calculations demonstrate clear value: With 18 core tools averaging 5 parameters each (90 total parameter definitions), each hour spent on schema quality saves approximately 3.3 hours of downstream work. Organizations using AI coding assistants report that 34% of failed tool calls stem from schema-related issues (incorrect types, missing required fields, constraint violations). Proper schemas reduce this failure rate to under 5%, translating to significantly improved model accuracy and user trust. For a system processing 10,000 tool calls daily, this represents 2,900 fewer errors per day - a substantial improvement in reliability.

### Technical Architecture

Core tools are the built-in capabilities that enable Acode to interact with the development environment. These 18 tools are organized into 5 categories:

**File Operations (6 tools):** read_file, write_file, list_directory, search_files, delete_file, move_file. These tools provide all file system access capabilities. The read_file tool is the most frequently called (approximately 45% of all tool calls), followed by write_file (22%) and list_directory (15%). File operation schemas must enforce path length limits (4096 characters maximum), content size limits (1MB for write operations), and encoding specifications (UTF-8 default, with ASCII and UTF-16 support).

**Code Execution (2 tools):** execute_command, execute_script. These tools enable running shell commands and scripts in PowerShell, Bash, or Python. Execution tools are security-critical and require strict timeout enforcement (1-3600 seconds), working directory validation, and environment variable sanitization. The execute_command schema limits command length to 8192 characters to prevent buffer overflows and denial-of-service via excessively long commands.

**Code Analysis (3 tools):** semantic_search, find_symbol, get_definition. These tools leverage the indexing subsystem (Task 017) to enable intelligent code navigation. The semantic_search tool requires a minimum query length of 3 characters to prevent overly broad searches. find_symbol supports filtering by symbol type (class, method, function, variable). get_definition requires precise file/line/column coordinates for go-to-definition functionality.

**Version Control (4 tools):** git_status, git_diff, git_log, git_commit. These tools provide git integration for the agent. git_log enforces count bounds (1-100 entries) to prevent memory exhaustion. git_commit requires a message (1-500 characters) and supports both selective file staging and the --all flag pattern.

**User Interaction (2 tools):** ask_user, confirm_action. These tools enable bidirectional communication with the user. ask_user supports optional multiple-choice options. confirm_action includes a destructive flag for dangerous operations and requires a minimum 10-character action description to ensure meaningful confirmations.

### JSON Schema Standard Compliance

The schemas follow JSON Schema Draft 2020-12, the same version supported by the Tool Schema Registry (Task 007) and vLLM's structured output enforcement (Task 006.b). This alignment ensures schemas work consistently across all integration points: (1) The Tool Schema Registry validates incoming arguments against compiled schemas, (2) Model prompts include schema definitions for function calling, and (3) vLLM's structured output mode uses schemas to constrain model generation.

JSON Schema Draft 2020-12 provides the following features used extensively in core tool schemas:

| Feature | Usage | Example |
|---------|-------|---------|
| `type` | Data type enforcement | `"type": "string"` |
| `required` | Mandatory field specification | `"required": ["path"]` |
| `enum` | Value restriction to fixed set | `"enum": ["utf-8", "ascii"]` |
| `minimum`/`maximum` | Numeric bounds | `"minimum": 1, "maximum": 3600` |
| `minLength`/`maxLength` | String length bounds | `"maxLength": 4096` |
| `pattern` | Regex validation | `"pattern": "^[a-z_]+$"` |
| `default` | Default value specification | `"default": "utf-8"` |
| `additionalProperties` | Object key/value constraints | `"additionalProperties": {"type": "string"}` |
| `description` | Human-readable documentation | Included for every field |

### Schema Description Quality Standards

Each schema includes detailed descriptions for every parameter. These descriptions are included in model prompts and help the model understand how to use each tool correctly. Good descriptions reduce tool call errors and improve model behavior. Research on LLM function calling shows that description quality directly correlates with call accuracy: models calling functions with 50+ word descriptions achieve 23% higher accuracy than those with 10-word descriptions.

Description quality standards for all core tool schemas:

1. **Clarity**: Descriptions MUST be unambiguous. "Path to the file" is better than "Path" alone.
2. **Examples**: Include inline examples for complex parameters. Example: "Path to the file to read (e.g., 'src/main.cs' or '/absolute/path/file.txt')"
3. **Constraints**: Document constraints in the description even when enforced by schema. Example: "Maximum 4096 characters"
4. **Defaults**: Explicitly state default values and their meaning. Example: "Text encoding for reading the file (default: utf-8)"
5. **Relationships**: Document parameter interdependencies. Example: "end_line must be >= start_line when both are provided"
6. **Length**: Descriptions MUST be 30-200 characters. Too short loses context; too long bloats prompts.

### Security Architecture Integration

Security considerations inform schema design at every level. File paths are constrained to prevent directory traversal. Command execution tools have restricted options. Sensitive operations require explicit confirmation flags. The Security Layer (Task 003) enforces these constraints at runtime, but schemas provide the first line of defense by rejecting clearly invalid input before execution.

The defense-in-depth security model operates in three layers:

**Layer 1 - Schema Validation (This Task):** Rejects malformed input immediately. A path containing null bytes, a timeout of -1, or a command exceeding 8192 characters never reaches execution. Schema validation is fast (< 1ms) and deterministic.

**Layer 2 - Registry Enforcement (Task 007):** The Tool Schema Registry performs additional validation including rate limiting, tool enable/disable checks, and argument audit logging.

**Layer 3 - Execution Security (Task 003):** The Security Layer performs runtime checks including path canonicalization (resolving "../" sequences), command blocklist matching, and sandboxing for execution tools.

Specific security constraints built into core tool schemas:

| Tool | Security Constraint | Rationale |
|------|---------------------|-----------|
| read_file | path maxLength 4096 | Prevent buffer overflow |
| write_file | content maxLength 1MB | Prevent disk exhaustion |
| delete_file | confirm parameter | Require explicit confirmation |
| execute_command | timeout 1-3600s | Prevent runaway processes |
| execute_command | command maxLength 8192 | Prevent buffer overflow |
| execute_script | script maxLength 64KB | Limit script size |
| git_commit | message maxLength 500 | Prevent log spam |
| confirm_action | description minLength 10 | Require meaningful description |

### Schema Organization and Categorization

The core tools are organized into categories: File Operations, Code Execution, Code Analysis, Version Control, and User Interaction. Each category groups related tools with consistent naming conventions and parameter patterns. This organization helps models learn tool usage patterns and enables category-based filtering in the registry.

Category-based organization provides three benefits:

1. **Model Learning**: When models see consistent patterns within a category, they generalize better. All file operation tools use "path" for file paths, never "file", "filepath", or "filename". This consistency reduces parameter name confusion.

2. **Registry Filtering**: The CLI command `acode tools list --category file_operations` returns only file-related tools. This enables focused tool discovery.

3. **Security Policies**: Category-based policies can enable/disable entire tool groups. Example: "Disable all code execution tools in review mode."

Naming conventions enforced across all categories:

| Convention | Example | Applied To |
|------------|---------|------------|
| snake_case tool names | read_file, git_status | All 18 tools |
| snake_case parameter names | start_line, working_directory | All parameters |
| "path" for file paths | path, source, destination | File tools |
| "query" for search terms | query | Search tools |
| "timeout_seconds" for timeouts | timeout_seconds | Execution tools |
| Boolean defaults explicit | "default": false | All booleans |

### Versioning Strategy

Versioning enables schema evolution. Each schema has a version number following semver conventions. When schemas change, the version increments. Breaking changes (removing fields, changing types) require major version bumps. Additive changes (new optional fields) require minor bumps. The registry tracks versions and can support multiple versions for backwards compatibility.

Version format: MAJOR.MINOR.PATCH (e.g., 1.0.0, 1.1.0, 2.0.0)

**MAJOR version** (1.0.0 → 2.0.0): Breaking changes that require model retraining or prompt updates.
- Removing a required field
- Changing a field's type (string → integer)
- Renaming a field
- Changing enum values

**MINOR version** (1.0.0 → 1.1.0): Backwards-compatible additions.
- Adding a new optional field
- Adding a new enum value
- Relaxing a constraint (maxLength 100 → maxLength 200)

**PATCH version** (1.0.0 → 1.0.1): Non-functional changes.
- Description text improvements
- Documentation fixes
- Comment updates

All core tools start at version 1.0.0. The registry maintains a version history and can serve older schema versions for backwards compatibility during migration periods.

### Code-Based Schema Definition

The schemas are defined in code as part of the CoreToolsProvider (from Task 007). This provider registers all core tool schemas during application startup. Schemas are compiled and cached for fast validation. The code-based approach enables IDE support, compile-time checking, and version control integration.

Code-based schemas provide advantages over file-based JSON:

1. **Compile-time checking**: C# compiler catches typos and type errors
2. **IDE support**: IntelliSense, refactoring, and navigation work normally
3. **Testability**: Unit tests can directly reference schema classes
4. **Reusability**: Common patterns extracted to helper methods
5. **Version control**: Standard diff/merge workflows apply

The CoreToolsProvider implements ISchemaProvider and registers all 18 core tool schemas in a single RegisterSchemas method. Each tool schema is defined in its own static class within a category-based folder structure.

### Testing and Validation

Testing validates that schemas are well-formed, that example arguments pass validation, and that invalid arguments are correctly rejected. Each tool has test cases for valid usage, missing required fields, wrong types, and constraint violations. Schema testing runs as part of the standard test suite.

Each core tool requires the following test coverage:

1. **Valid Arguments Test**: Typical usage passes validation
2. **Missing Required Field Test**: Each required field missing → specific error
3. **Wrong Type Test**: String where integer expected → type error
4. **Constraint Violation Test**: Value outside bounds → constraint error
5. **Extra Field Test**: Unknown fields handled (ignored or rejected per config)
6. **Example Arguments Test**: All schema examples validate successfully

Test execution target: All 18 core tool schemas × 6 test categories = 108 minimum test cases. Actual count may exceed this due to parameter combinations.

### Documentation Generation

Documentation is generated from schemas. The CLI's `acode tools show <name>` command displays schema details in human-readable format. Schema descriptions appear in generated documentation. This ensures documentation stays synchronized with actual tool behavior.

Generated documentation includes:

1. **Tool Summary**: Name, version, category, and description
2. **Parameter Table**: Name, type, required/optional, description
3. **Constraints**: All validation rules per parameter
4. **Examples**: Valid invocation examples with sample arguments
5. **Version History**: Changes between schema versions

The `acode tools show` command outputs both human-readable and JSON formats:

```bash
$ acode tools show read_file

read_file v1.0.0 (File Operations)
──────────────────────────────────
Read the contents of a file from the file system

PARAMETERS:
  path         string  [required]  Path to the file to read
  start_line   integer [optional]  Line number to start reading (1-indexed)
  end_line     integer [optional]  Line number to stop reading (inclusive)
  encoding     string  [optional]  Text encoding (default: utf-8)

CONSTRAINTS:
  path:       maxLength 4096
  start_line: minimum 1
  end_line:   minimum 1
  encoding:   enum [utf-8, ascii, utf-16]

EXAMPLE:
  {"path": "src/main.cs", "start_line": 1, "end_line": 100}
```

### Integration Points

This task integrates with multiple other Acode components:

| Component | Integration | Reference |
|-----------|-------------|-----------|
| Tool Schema Registry | Schemas registered via CoreToolsProvider | Task 007 |
| Structured Outputs | Schemas used for constrained generation | Task 006.b |
| Security Layer | Schemas enforce first-level constraints | Task 003 |
| CLI | `acode tools` commands display schemas | Task 010 |
| Model Prompts | Schema descriptions included in prompts | Task 008 |
| Audit Trail | Schema versions recorded with tool calls | Task 039 |

### Constraints and Limitations

The following constraints apply to this task:

1. **No tool implementation**: This task defines schemas only. Tool execution logic is handled by separate tasks (Task 014 for file operations, Task 018 for command execution, etc.)

2. **No result schemas**: Tool output formats are not defined in this task. Output schemas may be added in future enhancements.

3. **No streaming support**: Current schemas are for request/response tools only. Streaming tool support is a future enhancement.

4. **No composition**: Tools cannot be chained or composed within schemas. Tool composition is handled at the orchestration layer.

5. **18 core tools only**: This task covers the 18 built-in core tools. Custom tool schemas are handled by the registry's configuration-based approach (Task 007).

6. **English descriptions only**: Schema descriptions are in English. Localization is not supported in v1.

---

## Use Cases

### Use Case 1: DevBot (AI Coding Agent) Generates Valid Tool Calls

**Scenario:** DevBot is an AI coding assistant working on a bug fix. It needs to read a source file, modify it, and run tests. Each operation requires generating valid tool call arguments that conform to the defined schemas.

**Without This Feature (No Schema Definitions):**
DevBot receives a user request: "Fix the null reference exception in UserService.cs on line 45". To investigate, DevBot needs to read the file. Without schema definitions, DevBot must guess the correct parameter format. It generates:
```json
{"file": "src/UserService.cs", "lines": "40-50"}
```
This fails because the correct parameter names are "path" (not "file") and separate "start_line"/"end_line" integers (not a "lines" range string). The tool call is rejected with an unhelpful error: "Invalid arguments". DevBot tries again with different guesses, burning through 3-4 attempts (wasting 45 seconds and 2,000+ tokens) before stumbling on the correct format. When it tries to write the fix, it guesses:
```json
{"path": "src/UserService.cs", "text": "fixed code..."}
```
This fails because the parameter is "content", not "text". Another 2 failed attempts. Total time wasted: 2 minutes. Total extra API cost: $0.08 per bug fix. At 50 bug fixes per day across an engineering team, this adds up to $4/day, $1,460/year in wasted inference costs alone—plus developer frustration.

**With This Feature (Complete Schema Definitions):**
DevBot's system prompt includes the read_file schema:
```json
{
    "name": "read_file",
    "parameters": {
        "properties": {
            "path": {"type": "string", "description": "Path to the file to read"},
            "start_line": {"type": "integer", "description": "Line to start reading (1-indexed)"},
            "end_line": {"type": "integer", "description": "Line to stop reading (inclusive)"}
        },
        "required": ["path"]
    }
}
```
DevBot immediately generates the correct call:
```json
{"path": "src/UserService.cs", "start_line": 40, "end_line": 50}
```
The call succeeds on the first attempt. The write operation also succeeds on the first attempt because DevBot knows the parameter is "content". Total time: 15 seconds. No wasted tokens.

**Outcome:**
- **First-Call Success Rate:** Improves from 65% to 97% for tool calls
- **Token Efficiency:** 30-40% reduction in total tokens used per task
- **User Experience:** Eliminates frustrating "retry loops" that erode trust
- **Cost Savings:** $1,460+ annual savings in inference costs for active team

### Use Case 2: Jordan (Security Engineer) Audits Tool Schemas for Vulnerabilities

**Scenario:** Jordan is responsible for security review of the Acode system before enterprise deployment. Jordan needs to verify that tool schemas enforce appropriate constraints to prevent common attack vectors.

**Without This Feature (Ad-Hoc Tool Definitions):**
Jordan asks for tool documentation and receives informal descriptions: "read_file takes a path and optional line numbers". Jordan must manually review implementation code to understand actual constraints. Questions arise: Can paths contain "../"? Is there a maximum path length? What about null bytes? Jordan spends 8 hours reverse-engineering tool behavior from 18 different implementation files. Even then, Jordan can't be certain the documentation matches the implementation. The security review is incomplete, and Jordan flags the system as "high risk" pending further review.

**With This Feature (Complete Schema Definitions):**
Jordan runs `acode tools show read_file --json` and immediately sees:
```json
{
    "parameters": {
        "properties": {
            "path": {
                "type": "string",
                "maxLength": 4096,
                "description": "Path to the file to read"
            }
        }
    }
}
```
Jordan notes: path has maxLength 4096 (buffer overflow protection). Jordan reviews all 18 core tool schemas in 45 minutes. Jordan creates a security checklist:

| Tool | Constraint | Status |
|------|------------|--------|
| read_file | path maxLength 4096 | ✓ |
| write_file | content maxLength 1MB | ✓ |
| delete_file | confirm parameter | ✓ |
| execute_command | timeout 1-3600s | ✓ |
| execute_command | command maxLength 8192 | ✓ |

Jordan confirms that schema-level constraints provide first-line defense. Jordan documents that runtime validation (Task 003) handles additional checks like path traversal detection. The security review completes in 2 hours instead of 8+.

**Outcome:**
- **Review Time:** Reduced from 8+ hours to 2 hours (75% time savings)
- **Confidence:** Complete visibility into all tool constraints
- **Documentation:** Schemas serve as authoritative security documentation
- **Compliance:** Enables SOC2/ISO27001 evidence collection for tool security

### Use Case 3: Alex (Junior Developer) Implements New Tool Schema Following Patterns

**Scenario:** Alex is a junior developer tasked with adding a new "copy_file" tool schema. Alex has 6 months of C# experience and has never worked with JSON Schema before.

**Without This Feature (No Patterns or Examples):**
Alex is told: "Add a copy_file tool schema. It should take source and destination paths." Alex has no examples to follow. Alex spends 4 hours researching JSON Schema syntax, experimenting with different formats, and debugging validation errors. Alex's first attempt:
```csharp
public static ToolDefinition Create() => new()
{
    Name = "copy_file",
    Parameters = "{ source: string, destination: string }"  // Invalid JSON
};
```
This doesn't compile. Alex eventually produces a working schema, but it's inconsistent with other tools: uses "src" instead of "source" (other tools use full words), omits maxLength constraints (other file tools have them), and lacks descriptions. The code review takes 2 hours of back-and-forth corrections.

**With This Feature (Consistent Patterns and Examples):**
Alex looks at move_file schema (same category, similar parameters):
```csharp
public static class MoveFileSchema
{
    public static ToolDefinition Create() => new()
    {
        Name = "move_file",
        Description = "Move or rename a file",
        Version = "1.0.0",
        Category = ToolCategory.FileOperations,
        Parameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "source": {
                    "type": "string",
                    "description": "Path to the source file",
                    "maxLength": 4096
                },
                "destination": {
                    "type": "string",
                    "description": "Path to the destination",
                    "maxLength": 4096
                },
                "overwrite": {
                    "type": "boolean",
                    "description": "Overwrite destination if exists",
                    "default": false
                }
            },
            "required": ["source", "destination"]
        }
        """).RootElement
    };
}
```
Alex copies this pattern, adjusts the name/description, and produces a consistent copy_file schema in 30 minutes. The code review has no corrections—Alex followed the established pattern perfectly.

**Outcome:**
- **Development Time:** Reduced from 4+ hours to 30 minutes (87% time savings)
- **Quality:** First attempt matches team standards
- **Onboarding:** New developers productive immediately
- **Consistency:** All tools follow identical patterns

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

## Assumptions

### Technical Assumptions

1. **JSON Schema Draft 2020-12 Support:** The underlying validation library (System.Text.Json or JsonSchema.Net) fully supports JSON Schema Draft 2020-12 features including `type`, `required`, `enum`, `minimum`, `maximum`, `minLength`, `maxLength`, `pattern`, `default`, and `additionalProperties`. If the library has limitations, schemas will be adjusted to use supported features only.

2. **JsonDocument Parsing:** All schema definitions use `JsonDocument.Parse()` with raw string literals (C# 11 feature). The .NET 8 runtime supports these features. Compilation will succeed on the target runtime.

3. **Schema Compilation Performance:** Compiled schemas can validate arguments in under 1ms. The JsonSchema.Net library provides this level of performance. If validation exceeds 1ms, caching and pre-compilation strategies will be employed.

4. **UTF-8 Default Encoding:** All file operation tools default to UTF-8 encoding. This matches .NET's default string handling and is appropriate for 99%+ of source code files. Users working with non-UTF-8 files can specify encoding explicitly.

5. **Path Separator Handling:** Path parameters accept both forward slashes (/) and backslashes (\). The underlying file system abstraction (Task 014) handles normalization. Schemas do not enforce separator conventions.

6. **Case Sensitivity:** Tool names and parameter names are case-sensitive and use snake_case. `read_file` is valid; `Read_File` or `readFile` are not. This matches LLM function calling conventions.

### Operational Assumptions

7. **Tool Registry Availability:** The Tool Schema Registry (Task 007) is available and functioning when CoreToolsProvider registers schemas. Registration failures will throw exceptions at startup, preventing the application from running with incomplete tool definitions.

8. **Single Version Active:** Only one version of each tool schema is active at runtime. The registry does not support concurrent multiple versions of the same tool. Version migration requires schema updates and redeployment.

9. **Example Arguments Valid:** All example arguments provided in test data and documentation are syntactically valid and pass schema validation. Examples are tested as part of the CI pipeline.

10. **Error Messages Actionable:** When validation fails, the error message includes the specific constraint that was violated (e.g., "path exceeds maxLength 4096" or "timeout_seconds below minimum 1"). Generic "validation failed" messages are not acceptable.

11. **Model Prompt Inclusion:** Schema descriptions are included in model system prompts. Description length directly impacts prompt token count. The 200-character limit per description is chosen to balance clarity with token efficiency. Total schema description overhead is approximately 3,000 tokens for all 18 tools.

### Integration Assumptions

12. **Security Layer Runtime Checks:** Schema validation is the first line of defense. The Security Layer (Task 003) performs additional runtime checks including path canonicalization, command blocklist matching, and sandbox enforcement. Schemas do not attempt to prevent all attacks—they provide fast rejection of clearly invalid input.

13. **Tool Implementation Separate:** Tool implementation code is developed in separate tasks. Schema definitions do not depend on implementation completion. Schemas can be finalized and tested before implementations exist.

14. **CLI Display Compatibility:** The CLI's `acode tools show` command can render all schema constructs (objects, arrays, enums, constraints). Complex nested structures may use simplified display formats.

15. **Structured Output Compatibility:** Schemas are compatible with vLLM's structured output enforcement (Task 006.b). All schema features used are supported by the constrained generation system.

### Resource Assumptions

16. **Memory Budget:** All 18 compiled schemas fit within 10MB of memory. Each schema averages 500KB when compiled, with caching optimization. This fits comfortably within Acode's 256MB minimum memory footprint.

17. **Startup Time Impact:** Schema registration and compilation adds less than 500ms to application startup time. This is acceptable given startup is a one-time cost per session.

18. **Test Execution Time:** All schema unit tests (100+ test cases) execute in under 10 seconds total. Fast test execution enables rapid development iteration.

### Maintenance Assumptions

19. **Version Tracking:** Schema versions are tracked in source control alongside code. Version history can be reconstructed from git history. No external version database is required.

20. **Documentation Synchronization:** Schema descriptions and documentation are generated from the same source. If a description is updated in code, documentation automatically reflects the change after rebuild.

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

## Security Considerations

### Threat Model for Core Tool Schemas

The core tool schemas face specific security threats that must be addressed through careful constraint design. This section analyzes threats by tool category and documents mitigations built into the schema definitions.

#### Threat 1: Path Traversal Attacks (File Operations)

**Attack Vector:** An attacker (or a compromised/jailbroken model) attempts to access files outside the workspace using path traversal sequences like "../../../etc/passwd" or absolute paths to sensitive locations.

**Schema Mitigation:**
- All path parameters have `maxLength: 4096` to prevent excessively long paths that might confuse parsers
- Paths are validated as strings (not allowing object injection)
- Description text explicitly warns about workspace-relative paths

**Runtime Mitigation (Task 003):**
- Path canonicalization resolves all "../" sequences before access
- Workspace boundary enforcement restricts access to designated directories
- Protected path blocklist prevents access to `.git/`, `.env`, credentials

**Residual Risk:** Medium. Schema provides first-line defense; runtime validation is essential.

#### Threat 2: Command Injection (Code Execution)

**Attack Vector:** An attacker injects malicious shell commands through the `command` or `script` parameter, potentially executing arbitrary code on the host system.

**Schema Mitigation:**
- `command` has `maxLength: 8192` to prevent buffer-based attacks
- `script` has `maxLength: 65536` to limit script size
- `timeout_seconds` has `maximum: 3600` to prevent indefinite execution
- `timeout_seconds` has `minimum: 1` to prevent immediate timeout bypass

**Runtime Mitigation (Task 003):**
- Command blocklist prevents dangerous commands (rm -rf /, del /s /q, etc.)
- Sandboxing executes commands in isolated environment
- Network access restrictions prevent data exfiltration

**Residual Risk:** High. Execution tools are inherently dangerous. Defense in depth is critical.

#### Threat 3: Denial of Service via Resource Exhaustion

**Attack Vector:** An attacker triggers resource exhaustion by:
- Requesting extremely large file reads/writes
- Running long-duration commands
- Generating excessive search results

**Schema Mitigation:**
| Parameter | Constraint | Protection |
|-----------|------------|------------|
| write_file.content | maxLength 1MB | Disk exhaustion |
| execute_command.timeout_seconds | max 3600 | CPU exhaustion |
| search_files.max_results | default 100 | Memory exhaustion |
| git_log.count | max 100 | Output overflow |
| list_directory.max_depth | limited | Directory explosion |

**Runtime Mitigation:**
- Per-operation timeouts enforced
- Memory limits on tool output capture
- Rate limiting on tool calls

**Residual Risk:** Low. Schema constraints effectively prevent most DoS vectors.

#### Threat 4: Information Disclosure via Error Messages

**Attack Vector:** Schema validation error messages might leak sensitive information about system structure or file paths.

**Schema Mitigation:**
- Error messages include only parameter names and constraint values
- File paths in errors are not expanded or resolved
- No stack traces or internal paths in validation errors

**Example Safe Error:** "Parameter 'path' exceeds maxLength 4096 (actual: 4523)"
**Example Unsafe Error:** "Cannot access C:\\Users\\admin\\secrets\\keys.txt: path exceeds maxLength"

**Residual Risk:** Low. Generic error messages prevent information leakage.

#### Threat 5: Type Confusion Attacks

**Attack Vector:** An attacker provides a value of unexpected type (e.g., object instead of string) to exploit type handling bugs.

**Schema Mitigation:**
- All parameters have explicit `type` declarations
- No `oneOf`, `anyOf`, or `type: [array]` constructs that allow type ambiguity
- Object parameters explicitly define `additionalProperties` policy

**Example Protection:**
```json
"env": {
    "type": "object",
    "additionalProperties": { "type": "string" }
}
```
This prevents non-string values in the environment variable object.

**Residual Risk:** Low. Strict typing prevents type confusion.

### Security Audit Checklist

For security review, verify the following for each core tool schema:

- [ ] All string parameters have `maxLength` constraint
- [ ] All integer parameters have `minimum` and/or `maximum` constraints
- [ ] All file path parameters use consistent maxLength (4096)
- [ ] Destructive operations have `confirm` parameter
- [ ] Boolean parameters default to the safe option (false for destructive)
- [ ] No `additionalProperties: true` (reject unknown parameters)
- [ ] Description text does not reveal sensitive implementation details
- [ ] Example arguments use safe, non-exploitable values

---

## Best Practices

### Schema Design Best Practices

#### 1. Use Consistent Parameter Names Across Tools

When multiple tools share a concept, use identical parameter names:

| Concept | Standard Name | Used In |
|---------|--------------|---------|
| File path | `path` | read_file, write_file, list_directory, delete_file |
| Source file | `source` | move_file |
| Destination | `destination` | move_file |
| Search term | `query` | search_files, semantic_search |
| Timeout | `timeout_seconds` | execute_command, execute_script |
| Confirmation | `confirm` | delete_file |

**Wrong:** Using `file`, `filepath`, `filename`, `path` interchangeably
**Right:** Always use `path` for file paths

#### 2. Always Include maxLength for String Parameters

Every string parameter should have a maxLength constraint:

```json
"path": {
    "type": "string",
    "maxLength": 4096,
    "description": "Path to the file"
}
```

Standard maxLength values:
- File paths: 4096 (maximum path length on most systems)
- Commands: 8192 (generous but bounded)
- File content: 1048576 (1MB)
- User messages: 500 (readable length)

#### 3. Bound All Integer Parameters

Every integer parameter should have minimum and/or maximum constraints:

```json
"timeout_seconds": {
    "type": "integer",
    "minimum": 1,
    "maximum": 3600,
    "default": 300
}
```

Without bounds, an integer could be 0, negative, or astronomically large—all problematic.

#### 4. Document Default Values in Descriptions

When a parameter has a default, state it explicitly in the description:

```json
"encoding": {
    "type": "string",
    "enum": ["utf-8", "ascii", "utf-16"],
    "default": "utf-8",
    "description": "Text encoding for reading the file (default: utf-8)"
}
```

The description should match the default attribute. Models see descriptions but may not see default attributes depending on prompt formatting.

#### 5. Write Descriptions for Model Consumption

Descriptions appear in model prompts. Write them for LLM understanding:

**Good descriptions:**
- "Path to the file to read (e.g., 'src/main.cs' or 'README.md')"
- "Line number to start reading from, 1-indexed (first line is 1)"
- "Create parent directories if they don't exist (default: false)"

**Bad descriptions:**
- "Path" (too terse)
- "The path parameter represents the filesystem location of the target file resource" (too verbose)
- "See documentation" (unhelpful)

#### 6. Use Enums for Fixed Value Sets

When a parameter accepts only specific values, use an enum:

```json
"language": {
    "type": "string",
    "enum": ["powershell", "bash", "python"],
    "description": "Script language (powershell, bash, or python)"
}
```

Enums provide:
- Immediate validation (reject invalid values)
- Clear documentation (all options visible)
- Model guidance (constrained output generation)

#### 7. Default Booleans to the Safe Option

Destructive or sensitive operations should default to false:

```json
"overwrite": {
    "type": "boolean",
    "default": false,
    "description": "Overwrite file if it exists (default: false)"
}
```

The model must explicitly request destructive behavior; it never happens by accident.

#### 8. Group Related Parameters in Descriptions

When parameters have relationships, document them:

```json
"start_line": {
    "type": "integer",
    "minimum": 1,
    "description": "Line number to start reading (1-indexed). Must be <= end_line if both specified."
},
"end_line": {
    "type": "integer", 
    "minimum": 1,
    "description": "Line number to stop reading (inclusive). Must be >= start_line if both specified."
}
```

#### 9. Provide Example Arguments for Testing

Each schema should have valid example arguments for testing:

```csharp
public static class ReadFileSchema
{
    public static object[] ValidExamples => new[]
    {
        new { path = "README.md" },
        new { path = "src/main.cs", start_line = 1, end_line = 100 },
        new { path = "config.json", encoding = "utf-8" }
    };
}
```

#### 10. Version Schemas Semantically

Follow semver for all schema changes:

- Adding optional parameter: minor version bump (1.0.0 → 1.1.0)
- Relaxing constraint: minor version bump
- Tightening constraint: major version bump (breaking)
- Removing parameter: major version bump (breaking)
- Changing type: major version bump (breaking)

#### 11. Test Negative Cases

Test that invalid inputs are rejected:

```csharp
[Fact]
public void ReadFile_Should_Reject_Missing_Path()
{
    var result = schema.Validate(new { });
    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.Contains("'path' is required"));
}
```

#### 12. Keep Schemas Focused

Each tool schema should do one thing well. If a tool accumulates many unrelated parameters, consider splitting into multiple tools.

---

## Troubleshooting

### Issue 1: Schema Validation Always Fails with "Unknown Property" Error

**Symptoms:**
- Tool calls consistently rejected with "unknown property" errors
- Valid-looking arguments fail validation
- Error message mentions property names not in schema

**Causes:**
1. **additionalProperties set to false** and model is sending extra fields
2. **Case mismatch** in property names (camelCase vs snake_case)
3. **Schema not registered** - using outdated cached schema
4. **JSON parsing error** - malformed JSON in arguments

**Solutions:**

1. Check if the schema explicitly rejects unknown properties:
```json
"additionalProperties": false
```
If so, the model must send exactly the defined properties. Update prompts to clarify required parameters.

2. Verify case sensitivity:
```bash
# Wrong (camelCase)
{"startLine": 1, "endLine": 10}

# Right (snake_case)
{"start_line": 1, "end_line": 10}
```

3. Force schema re-registration:
```bash
acode config reload --schemas
```

4. Validate JSON syntax:
```bash
echo '{"path": "test.txt"}' | acode tools validate read_file --verbose
```

### Issue 2: Schema Compilation Fails at Startup

**Symptoms:**
- Application crashes during startup with schema errors
- Error message mentions "Failed to compile schema" or "Invalid JSON Schema"
- CoreToolsProvider registration throws exception

**Causes:**
1. **Malformed JSON** in schema definition (missing comma, unclosed brace)
2. **Invalid JSON Schema construct** (typo in keyword like "maximun" instead of "maximum")
3. **Unsupported JSON Schema feature** (using Draft 2020-12 feature not supported by library)
4. **Raw string literal syntax error** (missing """ markers)

**Solutions:**

1. Validate JSON syntax in schema definitions:
```csharp
// This will throw at compile time if JSON is malformed
var json = JsonDocument.Parse("""
{
    "type": "object",
    "properties": {}
}
""");
```

2. Check for common typos:
```
Wrong: "maximun", "minimun", "reqired", "properites"
Right: "maximum", "minimum", "required", "properties"
```

3. Review the exception stack trace for the specific schema:
```
Schema compilation failed for 'execute_command':
  Error at $.properties.timeout_seconds.maximun: Unknown keyword 'maximun'
```

4. Test schema in isolation:
```csharp
[Fact]
public void Schema_Should_Compile_Successfully()
{
    var schema = ExecuteCommandSchema.Create();
    schema.Should().NotBeNull();
    schema.CompiledSchema.Should().NotBeNull();
}
```

### Issue 3: Model Generates Invalid Arguments Despite Correct Schema

**Symptoms:**
- Schema is correctly defined and compiles
- Model still generates arguments that fail validation
- Same mistakes repeat across multiple interactions

**Causes:**
1. **Schema description too brief** - model doesn't understand usage
2. **Missing examples** - model has no reference for correct format
3. **Conflicting documentation** - other prompts contradict schema
4. **Model confusion** from similar tool names

**Solutions:**

1. Expand descriptions with examples:
```json
"path": {
    "type": "string",
    "description": "Path to the file to read. Examples: 'README.md', 'src/Program.cs', 'docs/api/index.html'. Relative paths are relative to workspace root."
}
```

2. Add format hints for structured data:
```json
"env": {
    "type": "object",
    "description": "Environment variables as key-value pairs. Example: {\"NODE_ENV\": \"test\", \"DEBUG\": \"true\"}"
}
```

3. Review system prompts for conflicting instructions about tool usage

4. Consider renaming tools if names are confusingly similar

### Issue 4: Performance Degradation During Validation

**Symptoms:**
- Tool calls take 100ms+ when they should take <1ms
- Performance degrades with complex arguments
- Memory usage spikes during validation

**Causes:**
1. **Schema not cached** - recompiling on every validation
2. **Regex pattern inefficient** - catastrophic backtracking
3. **Large content validation** - validating 1MB content field
4. **Nested object depth** - deeply nested structures

**Solutions:**

1. Ensure schema caching is enabled:
```csharp
// Bad: Compiles on every call
var schema = JsonSchema.FromText(schemaJson);

// Good: Compile once, reuse
private static readonly JsonSchema CachedSchema = JsonSchema.FromText(schemaJson);
```

2. Profile regex patterns:
```csharp
// Potentially dangerous pattern
"pattern": "^(a+)+$"

// Safe pattern
"pattern": "^[a-z_]+$"
```

3. For large fields, validate length first before content:
```csharp
if (content.Length > 1_048_576)
    return ValidationResult.Failure("Content exceeds 1MB limit");
```

### Issue 5: Schema Version Mismatch Between Components

**Symptoms:**
- Tool calls succeed in one context but fail in another
- "Schema version mismatch" errors
- Features work locally but fail in production

**Causes:**
1. **Different schema versions deployed** across components
2. **Cache invalidation failure** after schema update
3. **Model prompts using old schema descriptions**

**Solutions:**

1. Verify deployed schema versions:
```bash
acode tools show read_file --version
# Should output: 1.0.0
```

2. Clear all caches after schema updates:
```bash
acode config cache --clear --schemas
```

3. Update model prompts when schemas change:
- Descriptions in prompts must match current schema
- Examples must validate against current constraints
- Version numbers in documentation must be current

---

## User Manual Documentation

### Overview

Core tools are the built-in capabilities Acode uses to interact with your development environment. Each tool has a defined schema specifying what parameters it accepts. Understanding these schemas helps you troubleshoot tool call issues and extend Acode with custom tools.

This section provides comprehensive documentation for all 18 core tools, organized by category. For each tool, you'll find the complete parameter specification, usage examples, and common patterns.

### Viewing Tool Schemas

Use the CLI to explore available tools and their schemas:

```bash
# List all tools
$ acode tools list
┌─────────────────────────────────────────────────────────────────────┐
│ ACODE CORE TOOLS                                                    │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│ FILE OPERATIONS                                                      │
│   read_file         Read contents of a file                         │
│   write_file        Write content to a file                         │
│   list_directory    List directory contents                         │
│   search_files      Search for text in files                        │
│   delete_file       Delete a file                                   │
│   move_file         Move or rename a file                           │
│                                                                      │
│ CODE EXECUTION                                                       │
│   execute_command   Run a shell command                             │
│   execute_script    Run a script (PowerShell/Bash/Python)           │
│                                                                      │
│ CODE ANALYSIS                                                        │
│   semantic_search   Search code semantically                        │
│   find_symbol       Find symbol definition                          │
│   get_definition    Get definition at location                      │
│                                                                      │
│ VERSION CONTROL                                                      │
│   git_status        Show git status                                 │
│   git_diff          Show git diff                                   │
│   git_log           Show git log                                    │
│   git_commit        Create git commit                               │
│                                                                      │
│ USER INTERACTION                                                     │
│   ask_user          Ask user a question                             │
│   confirm_action    Request user confirmation                       │
│                                                                      │
│ Total: 18 core tools                                                 │
└─────────────────────────────────────────────────────────────────────┘

# Show specific tool schema
$ acode tools show read_file
┌─────────────────────────────────────────────────────────────────────┐
│ read_file v1.0.0 (File Operations)                                  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│ DESCRIPTION:                                                         │
│   Read the contents of a file from the file system                  │
│                                                                      │
│ PARAMETERS:                                                          │
│   path       string   [required]  Path to the file to read          │
│   start_line integer  [optional]  Line number to start (1-indexed)  │
│   end_line   integer  [optional]  Line number to stop (inclusive)   │
│   encoding   string   [optional]  Text encoding (default: utf-8)    │
│                                                                      │
│ CONSTRAINTS:                                                         │
│   path:       maxLength 4096                                         │
│   start_line: minimum 1                                              │
│   end_line:   minimum 1                                              │
│   encoding:   enum [utf-8, ascii, utf-16]                            │
│                                                                      │
│ EXAMPLES:                                                            │
│   {"path": "README.md"}                                              │
│   {"path": "src/main.cs", "start_line": 1, "end_line": 100}         │
│   {"path": "data.txt", "encoding": "ascii"}                         │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘

# Get schema as JSON
$ acode tools show read_file --json

# List tools by category
$ acode tools list --category file_operations
```

### Tool Categories

#### File Operations (6 Tools)

| Tool | Description | Required Parameters | Optional Parameters |
|------|-------------|---------------------|---------------------|
| read_file | Read file contents | path | start_line, end_line, encoding |
| write_file | Write content to file | path, content | create_directories, overwrite, encoding |
| list_directory | List directory contents | path | recursive, pattern, max_depth, include_hidden |
| search_files | Search text in files | query | path, pattern, case_sensitive, regex, max_results |
| delete_file | Delete a file | path | confirm |
| move_file | Move or rename file | source, destination | overwrite |

#### Code Execution (2 Tools)

| Tool | Description | Required Parameters | Optional Parameters |
|------|-------------|---------------------|---------------------|
| execute_command | Run shell command | command | working_directory, timeout_seconds, env |
| execute_script | Run script file | script, language | working_directory, timeout_seconds |

#### Code Analysis (3 Tools)

| Tool | Description | Required Parameters | Optional Parameters |
|------|-------------|---------------------|---------------------|
| semantic_search | Semantic code search | query | scope, path, max_results |
| find_symbol | Find symbol definition | symbol_name | symbol_type, path, include_references |
| get_definition | Get definition at location | file_path, line, column | (none) |

#### Version Control (4 Tools)

| Tool | Description | Required Parameters | Optional Parameters |
|------|-------------|---------------------|---------------------|
| git_status | Show repository status | (none) | path |
| git_diff | Show file differences | (none) | path, staged, commit |
| git_log | Show commit history | (none) | count, path, author |
| git_commit | Create a commit | message | files, all |

#### User Interaction (2 Tools)

| Tool | Description | Required Parameters | Optional Parameters |
|------|-------------|---------------------|---------------------|
| ask_user | Ask user a question | question | options, default_option |
| confirm_action | Request confirmation | action_description | destructive |

### Complete Schema Reference

#### read_file

**Purpose:** Read the contents of a file from the file system.

**Full Schema:**
```json
{
    "name": "read_file",
    "description": "Read the contents of a file from the file system",
    "version": "1.0.0",
    "category": "file_operations",
    "parameters": {
        "type": "object",
        "properties": {
            "path": {
                "type": "string",
                "description": "Path to the file to read (absolute or relative to workspace). Examples: 'README.md', 'src/Program.cs', '/home/user/file.txt'",
                "maxLength": 4096
            },
            "start_line": {
                "type": "integer",
                "description": "Line number to start reading from (1-indexed). First line is 1. Must be <= end_line if both specified.",
                "minimum": 1
            },
            "end_line": {
                "type": "integer",
                "description": "Line number to stop reading at (inclusive). Must be >= start_line if both specified.",
                "minimum": 1
            },
            "encoding": {
                "type": "string",
                "description": "Text encoding for reading the file (default: utf-8). Use 'ascii' for legacy files, 'utf-16' for Windows Unicode.",
                "enum": ["utf-8", "ascii", "utf-16"],
                "default": "utf-8"
            }
        },
        "required": ["path"],
        "additionalProperties": false
    }
}
```

**Usage Examples:**
```json
// Read entire file
{"path": "README.md"}

// Read specific line range
{"path": "src/main.cs", "start_line": 100, "end_line": 150}

// Read with specific encoding
{"path": "legacy/data.txt", "encoding": "ascii"}

// Read first 50 lines of file
{"path": "large_file.log", "start_line": 1, "end_line": 50}
```

#### write_file

**Purpose:** Write content to a file, creating it if it doesn't exist.

**Full Schema:**
```json
{
    "name": "write_file",
    "description": "Write content to a file, creating it if it doesn't exist",
    "version": "1.0.0",
    "category": "file_operations",
    "parameters": {
        "type": "object",
        "properties": {
            "path": {
                "type": "string",
                "description": "Path where the file will be written. Creates file if it doesn't exist. Examples: 'output.txt', 'src/NewClass.cs'",
                "maxLength": 4096
            },
            "content": {
                "type": "string",
                "description": "Content to write to the file. Maximum 1MB. For large files, write in chunks.",
                "maxLength": 1048576
            },
            "create_directories": {
                "type": "boolean",
                "description": "Create parent directories if they don't exist (default: false). Set to true when writing to new folder structures.",
                "default": false
            },
            "overwrite": {
                "type": "boolean",
                "description": "Overwrite file if it already exists (default: true). Set to false to prevent accidental overwrites.",
                "default": true
            },
            "encoding": {
                "type": "string",
                "description": "Text encoding for writing the file (default: utf-8)",
                "enum": ["utf-8", "ascii", "utf-16"],
                "default": "utf-8"
            }
        },
        "required": ["path", "content"],
        "additionalProperties": false
    }
}
```

**Usage Examples:**
```json
// Write simple file
{"path": "output.txt", "content": "Hello, World!"}

// Write new file with directory creation
{
    "path": "src/services/UserService.cs",
    "content": "public class UserService { }",
    "create_directories": true
}

// Write without overwriting existing
{
    "path": "config.json",
    "content": "{\"key\": \"value\"}",
    "overwrite": false
}
```

#### execute_command

**Purpose:** Execute a shell command and return its output.

**Full Schema:**
```json
{
    "name": "execute_command",
    "description": "Execute a shell command and return its output. Use for running build tools, tests, git commands, etc.",
    "version": "1.0.0",
    "category": "code_execution",
    "parameters": {
        "type": "object",
        "properties": {
            "command": {
                "type": "string",
                "description": "The command to execute. Examples: 'dotnet build', 'npm test', 'git status'. Maximum 8192 characters.",
                "maxLength": 8192
            },
            "working_directory": {
                "type": "string",
                "description": "Directory to run the command in. Defaults to workspace root. Example: 'src/api' to run from subdirectory.",
                "maxLength": 4096
            },
            "timeout_seconds": {
                "type": "integer",
                "description": "Maximum time to wait for command completion (default: 300 seconds / 5 minutes). Range: 1-3600 seconds.",
                "minimum": 1,
                "maximum": 3600,
                "default": 300
            },
            "env": {
                "type": "object",
                "description": "Environment variables to set for the command. Example: {\"NODE_ENV\": \"test\", \"DEBUG\": \"true\"}",
                "additionalProperties": { "type": "string" }
            }
        },
        "required": ["command"],
        "additionalProperties": false
    }
}
```

**Usage Examples:**
```json
// Run build
{"command": "dotnet build"}

// Run tests with timeout
{
    "command": "dotnet test --no-build",
    "timeout_seconds": 600
}

// Run from subdirectory with environment
{
    "command": "npm test",
    "working_directory": "frontend",
    "env": {"NODE_ENV": "test", "CI": "true"}
}
```

### Validating Tool Arguments

Use the CLI to validate arguments before execution:

```bash
# Validate valid arguments
$ echo '{"path": "README.md"}' | acode tools validate read_file
✓ Valid: Arguments conform to read_file schema

# Validate invalid arguments (missing required field)
$ echo '{}' | acode tools validate read_file
✗ Invalid: Missing required property 'path'

# Validate invalid arguments (wrong type)
$ echo '{"path": "test.txt", "start_line": "ten"}' | acode tools validate read_file
✗ Invalid: Property 'start_line' must be integer, got string

# Validate with verbose output
$ echo '{"path": "test.txt", "start_line": 1}' | acode tools validate read_file --verbose
Validating against: read_file v1.0.0
Schema: file_operations category
Checking required fields...
  ✓ path: present
Checking types...
  ✓ path: string
  ✓ start_line: integer
Checking constraints...
  ✓ path length 8 <= 4096
  ✓ start_line 1 >= 1
Result: ✓ Valid
```

### Configuration

Schema behavior can be configured in `acode.yml`:

```yaml
tools:
  schemas:
    # Reject unknown properties (strict mode)
    strict_mode: true
    
    # Cache compiled schemas (recommended)
    cache_compiled: true
    
    # Schema version to use (latest by default)
    # version: "1.0.0"
    
  defaults:
    # Default timeout for execution tools
    timeout_seconds: 300
    
    # Default encoding for file tools
    encoding: "utf-8"
    
    # Default max results for search tools
    max_results: 100
```

### Common Patterns

#### Reading Large Files in Chunks

For files larger than a few hundred lines, read in chunks:

```json
// First chunk
{"path": "large_file.cs", "start_line": 1, "end_line": 100}

// Second chunk
{"path": "large_file.cs", "start_line": 101, "end_line": 200}

// Continue as needed
```

#### Safe File Operations

For destructive operations, use confirmation flags:

```json
// Delete with confirmation
{"path": "old_file.txt", "confirm": true}

// Write without overwrite (fails if exists)
{"path": "new_file.txt", "content": "...", "overwrite": false}
```

#### Running Tests with Appropriate Timeouts

```json
// Unit tests: 5 minute timeout
{"command": "dotnet test --filter Category=Unit", "timeout_seconds": 300}

// Integration tests: 15 minute timeout
{"command": "dotnet test --filter Category=Integration", "timeout_seconds": 900}

// Full test suite: 30 minute timeout
{"command": "dotnet test", "timeout_seconds": 1800}
```

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

#### FileOperationsSchemaTests.cs

```csharp
using Xunit;
using FluentAssertions;
using System.Text.Json;
using Acode.Infrastructure.ToolSchemas.Providers.Schemas.FileOperations;
using Acode.Application.Interfaces;

namespace Acode.Infrastructure.Tests.ToolSchemas.CoreTools;

public class FileOperationsSchemaTests
{
    private readonly ISchemaValidator _validator;

    public FileOperationsSchemaTests()
    {
        _validator = new JsonSchemaValidator();
    }

    #region read_file Tests

    [Fact]
    public void ReadFile_Should_Validate_Valid_Path_Only()
    {
        // Arrange
        var schema = ReadFileSchema.Create();
        var arguments = JsonDocument.Parse("""{"path": "README.md"}""");

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ReadFile_Should_Validate_With_LineRange()
    {
        // Arrange
        var schema = ReadFileSchema.Create();
        var arguments = JsonDocument.Parse("""
        {
            "path": "src/Program.cs",
            "start_line": 1,
            "end_line": 100
        }
        """);

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ReadFile_Should_Validate_With_Encoding()
    {
        // Arrange
        var schema = ReadFileSchema.Create();
        var arguments = JsonDocument.Parse("""
        {
            "path": "legacy.txt",
            "encoding": "ascii"
        }
        """);

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ReadFile_Should_Reject_Missing_Path()
    {
        // Arrange
        var schema = ReadFileSchema.Create();
        var arguments = JsonDocument.Parse("""{}""");

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("path") && e.Contains("required"));
    }

    [Fact]
    public void ReadFile_Should_Reject_Path_Exceeding_MaxLength()
    {
        // Arrange
        var schema = ReadFileSchema.Create();
        var longPath = new string('a', 5000); // Exceeds 4096 maxLength
        var arguments = JsonDocument.Parse($$$"""{"path": "{{{longPath}}}"}""");

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("maxLength") || e.Contains("4096"));
    }

    [Fact]
    public void ReadFile_Should_Reject_StartLine_Below_Minimum()
    {
        // Arrange
        var schema = ReadFileSchema.Create();
        var arguments = JsonDocument.Parse("""
        {
            "path": "test.txt",
            "start_line": 0
        }
        """);

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("start_line") && e.Contains("minimum"));
    }

    [Fact]
    public void ReadFile_Should_Reject_Invalid_Encoding()
    {
        // Arrange
        var schema = ReadFileSchema.Create();
        var arguments = JsonDocument.Parse("""
        {
            "path": "test.txt",
            "encoding": "utf-32"
        }
        """);

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("encoding") && e.Contains("enum"));
    }

    [Fact]
    public void ReadFile_Should_Reject_Wrong_Type_StartLine()
    {
        // Arrange
        var schema = ReadFileSchema.Create();
        var arguments = JsonDocument.Parse("""
        {
            "path": "test.txt",
            "start_line": "ten"
        }
        """);

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("start_line") && e.Contains("integer"));
    }

    #endregion

    #region write_file Tests

    [Fact]
    public void WriteFile_Should_Validate_Required_Fields()
    {
        // Arrange
        var schema = WriteFileSchema.Create();
        var arguments = JsonDocument.Parse("""
        {
            "path": "output.txt",
            "content": "Hello, World!"
        }
        """);

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void WriteFile_Should_Validate_With_Options()
    {
        // Arrange
        var schema = WriteFileSchema.Create();
        var arguments = JsonDocument.Parse("""
        {
            "path": "new/dir/file.txt",
            "content": "content",
            "create_directories": true,
            "overwrite": false
        }
        """);

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void WriteFile_Should_Reject_Missing_Content()
    {
        // Arrange
        var schema = WriteFileSchema.Create();
        var arguments = JsonDocument.Parse("""{"path": "output.txt"}""");

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("content") && e.Contains("required"));
    }

    [Fact]
    public void WriteFile_Should_Enforce_Content_MaxLength()
    {
        // Arrange
        var schema = WriteFileSchema.Create();
        var largeContent = new string('x', 1_100_000); // Exceeds 1MB
        var arguments = JsonDocument.Parse($$$"""
        {
            "path": "large.txt",
            "content": "{{{largeContent}}}"
        }
        """);

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("content") && e.Contains("maxLength"));
    }

    #endregion

    #region list_directory Tests

    [Fact]
    public void ListDirectory_Should_Validate_Path_Only()
    {
        // Arrange
        var schema = ListDirectorySchema.Create();
        var arguments = JsonDocument.Parse("""{"path": "src"}""");

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ListDirectory_Should_Validate_With_Options()
    {
        // Arrange
        var schema = ListDirectorySchema.Create();
        var arguments = JsonDocument.Parse("""
        {
            "path": "src",
            "recursive": true,
            "pattern": "*.cs",
            "max_depth": 3,
            "include_hidden": false
        }
        """);

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ListDirectory_Should_Reject_MaxDepth_Below_Minimum()
    {
        // Arrange
        var schema = ListDirectorySchema.Create();
        var arguments = JsonDocument.Parse("""
        {
            "path": "src",
            "max_depth": 0
        }
        """);

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    #endregion
}
```

#### CodeExecutionSchemaTests.cs

```csharp
using Xunit;
using FluentAssertions;
using System.Text.Json;
using Acode.Infrastructure.ToolSchemas.Providers.Schemas.CodeExecution;

namespace Acode.Infrastructure.Tests.ToolSchemas.CoreTools;

public class CodeExecutionSchemaTests
{
    private readonly ISchemaValidator _validator;

    public CodeExecutionSchemaTests()
    {
        _validator = new JsonSchemaValidator();
    }

    #region execute_command Tests

    [Fact]
    public void ExecuteCommand_Should_Validate_Command_Only()
    {
        // Arrange
        var schema = ExecuteCommandSchema.Create();
        var arguments = JsonDocument.Parse("""{"command": "dotnet build"}""");

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ExecuteCommand_Should_Validate_With_WorkingDirectory()
    {
        // Arrange
        var schema = ExecuteCommandSchema.Create();
        var arguments = JsonDocument.Parse("""
        {
            "command": "npm install",
            "working_directory": "frontend"
        }
        """);

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ExecuteCommand_Should_Validate_With_Environment()
    {
        // Arrange
        var schema = ExecuteCommandSchema.Create();
        var arguments = JsonDocument.Parse("""
        {
            "command": "npm test",
            "env": {
                "NODE_ENV": "test",
                "CI": "true",
                "DEBUG": "app:*"
            }
        }
        """);

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ExecuteCommand_Should_Reject_Timeout_Below_Minimum()
    {
        // Arrange
        var schema = ExecuteCommandSchema.Create();
        var arguments = JsonDocument.Parse("""
        {
            "command": "dotnet test",
            "timeout_seconds": 0
        }
        """);

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("timeout_seconds") && e.Contains("minimum"));
    }

    [Fact]
    public void ExecuteCommand_Should_Reject_Timeout_Above_Maximum()
    {
        // Arrange
        var schema = ExecuteCommandSchema.Create();
        var arguments = JsonDocument.Parse("""
        {
            "command": "long-running-task",
            "timeout_seconds": 10000
        }
        """);

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("timeout_seconds") && e.Contains("maximum"));
    }

    [Fact]
    public void ExecuteCommand_Should_Reject_Command_Exceeding_MaxLength()
    {
        // Arrange
        var schema = ExecuteCommandSchema.Create();
        var longCommand = new string('a', 9000);
        var arguments = JsonDocument.Parse($$$"""{"command": "{{{longCommand}}}"}""");

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("command") && e.Contains("maxLength"));
    }

    [Fact]
    public void ExecuteCommand_Should_Reject_NonString_EnvValues()
    {
        // Arrange
        var schema = ExecuteCommandSchema.Create();
        var arguments = JsonDocument.Parse("""
        {
            "command": "echo test",
            "env": {
                "PORT": 3000
            }
        }
        """);

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("env") || e.Contains("string"));
    }

    #endregion

    #region execute_script Tests

    [Fact]
    public void ExecuteScript_Should_Validate_Required_Fields()
    {
        // Arrange
        var schema = ExecuteScriptSchema.Create();
        var arguments = JsonDocument.Parse("""
        {
            "script": "Write-Host 'Hello'",
            "language": "powershell"
        }
        """);

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("powershell")]
    [InlineData("bash")]
    [InlineData("python")]
    public void ExecuteScript_Should_Accept_Valid_Languages(string language)
    {
        // Arrange
        var schema = ExecuteScriptSchema.Create();
        var arguments = JsonDocument.Parse($$$"""
        {
            "script": "echo hello",
            "language": "{{{language}}}"
        }
        """);

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ExecuteScript_Should_Reject_Invalid_Language()
    {
        // Arrange
        var schema = ExecuteScriptSchema.Create();
        var arguments = JsonDocument.Parse("""
        {
            "script": "console.log('hello')",
            "language": "javascript"
        }
        """);

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("language") && e.Contains("enum"));
    }

    [Fact]
    public void ExecuteScript_Should_Enforce_Script_MaxLength()
    {
        // Arrange
        var schema = ExecuteScriptSchema.Create();
        var longScript = new string('#', 70_000); // Exceeds 65536
        var arguments = JsonDocument.Parse($$$"""
        {
            "script": "{{{longScript}}}",
            "language": "bash"
        }
        """);

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    #endregion
}
```

#### VersionControlSchemaTests.cs

```csharp
using Xunit;
using FluentAssertions;
using System.Text.Json;
using Acode.Infrastructure.ToolSchemas.Providers.Schemas.VersionControl;

namespace Acode.Infrastructure.Tests.ToolSchemas.CoreTools;

public class VersionControlSchemaTests
{
    private readonly ISchemaValidator _validator;

    public VersionControlSchemaTests()
    {
        _validator = new JsonSchemaValidator();
    }

    #region git_log Tests

    [Fact]
    public void GitLog_Should_Accept_Empty_Arguments()
    {
        // Arrange
        var schema = GitLogSchema.Create();
        var arguments = JsonDocument.Parse("""{}""");

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GitLog_Should_Accept_Count_Within_Bounds()
    {
        // Arrange
        var schema = GitLogSchema.Create();
        var arguments = JsonDocument.Parse("""{"count": 50}""");

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GitLog_Should_Reject_Count_Below_Minimum()
    {
        // Arrange
        var schema = GitLogSchema.Create();
        var arguments = JsonDocument.Parse("""{"count": 0}""");

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("count") && e.Contains("minimum"));
    }

    [Fact]
    public void GitLog_Should_Reject_Count_Above_Maximum()
    {
        // Arrange
        var schema = GitLogSchema.Create();
        var arguments = JsonDocument.Parse("""{"count": 500}""");

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("count") && e.Contains("maximum"));
    }

    #endregion

    #region git_commit Tests

    [Fact]
    public void GitCommit_Should_Require_Message()
    {
        // Arrange
        var schema = GitCommitSchema.Create();
        var arguments = JsonDocument.Parse("""{}""");

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("message") && e.Contains("required"));
    }

    [Fact]
    public void GitCommit_Should_Accept_Message_With_Files()
    {
        // Arrange
        var schema = GitCommitSchema.Create();
        var arguments = JsonDocument.Parse("""
        {
            "message": "feat: add new feature",
            "files": ["src/Feature.cs", "tests/FeatureTests.cs"]
        }
        """);

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GitCommit_Should_Reject_Message_Exceeding_MaxLength()
    {
        // Arrange
        var schema = GitCommitSchema.Create();
        var longMessage = new string('a', 600);
        var arguments = JsonDocument.Parse($$$"""{"message": "{{{longMessage}}}"}""");

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("message") && e.Contains("maxLength"));
    }

    [Fact]
    public void GitCommit_Should_Reject_Empty_Message()
    {
        // Arrange
        var schema = GitCommitSchema.Create();
        var arguments = JsonDocument.Parse("""{"message": ""}""");

        // Act
        var result = _validator.Validate(schema, arguments);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("message") && e.Contains("minLength"));
    }

    #endregion
}
```

### Integration Tests

```csharp
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Acode.Infrastructure.ToolSchemas.Providers;
using Acode.Application.Interfaces;

namespace Acode.Infrastructure.Tests.ToolSchemas.Integration;

public class CoreToolsRegistrationTests
{
    [Fact]
    public async Task Should_Register_All_18_Core_Tools()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddToolSchemaRegistry();
        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IToolSchemaRegistry>();

        // Act
        var coreToolsProvider = new CoreToolsProvider();
        coreToolsProvider.RegisterSchemas(registry);

        // Assert
        var tools = registry.GetAllTools();
        tools.Should().HaveCount(18);

        // Verify all expected tools are registered
        var expectedTools = new[]
        {
            "read_file", "write_file", "list_directory", "search_files", "delete_file", "move_file",
            "execute_command", "execute_script",
            "semantic_search", "find_symbol", "get_definition",
            "git_status", "git_diff", "git_log", "git_commit",
            "ask_user", "confirm_action"
        };

        foreach (var toolName in expectedTools)
        {
            registry.TryGetTool(toolName, out var tool).Should().BeTrue($"Tool '{toolName}' should be registered");
            tool.Should().NotBeNull();
            tool.Name.Should().Be(toolName);
            tool.Version.Should().Be("1.0.0");
        }
    }

    [Fact]
    public async Task Should_Validate_All_Example_Arguments()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddToolSchemaRegistry();
        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IToolSchemaRegistry>();
        var validator = provider.GetRequiredService<ISchemaValidator>();

        var coreToolsProvider = new CoreToolsProvider();
        coreToolsProvider.RegisterSchemas(registry);

        // Act & Assert
        foreach (var tool in registry.GetAllTools())
        {
            foreach (var example in tool.Examples)
            {
                var result = validator.Validate(tool.Schema, example);
                result.IsValid.Should().BeTrue(
                    $"Example for '{tool.Name}' should validate: {example}. Errors: {string.Join(", ", result.Errors)}");
            }
        }
    }

    [Fact]
    public async Task Should_Categorize_Tools_Correctly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddToolSchemaRegistry();
        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IToolSchemaRegistry>();

        var coreToolsProvider = new CoreToolsProvider();
        coreToolsProvider.RegisterSchemas(registry);

        // Act
        var fileOpsTools = registry.GetToolsByCategory(ToolCategory.FileOperations);
        var execTools = registry.GetToolsByCategory(ToolCategory.CodeExecution);
        var analysisTools = registry.GetToolsByCategory(ToolCategory.CodeAnalysis);
        var gitTools = registry.GetToolsByCategory(ToolCategory.VersionControl);
        var userTools = registry.GetToolsByCategory(ToolCategory.UserInteraction);

        // Assert
        fileOpsTools.Should().HaveCount(6);
        execTools.Should().HaveCount(2);
        analysisTools.Should().HaveCount(3);
        gitTools.Should().HaveCount(4);
        userTools.Should().HaveCount(2);
    }

    [Fact]
    public async Task Schema_Compilation_Should_Complete_Under_500ms()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var services = new ServiceCollection();
        services.AddToolSchemaRegistry();
        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IToolSchemaRegistry>();

        var coreToolsProvider = new CoreToolsProvider();
        coreToolsProvider.RegisterSchemas(registry);

        // Force compilation of all schemas
        foreach (var tool in registry.GetAllTools())
        {
            _ = tool.CompiledSchema;
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500,
            $"Schema compilation took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
    }
}
```

### Performance Tests

```csharp
using Xunit;
using FluentAssertions;
using System.Diagnostics;
using System.Text.Json;
using Acode.Infrastructure.ToolSchemas.Providers.Schemas.FileOperations;

namespace Acode.Infrastructure.Tests.ToolSchemas.Performance;

public class SchemaValidationPerformanceTests
{
    [Fact]
    public void SingleValidation_Should_Complete_Under_1ms()
    {
        // Arrange
        var schema = ReadFileSchema.Create();
        var arguments = JsonDocument.Parse("""{"path": "test.txt", "start_line": 1, "end_line": 100}""");
        var validator = new JsonSchemaValidator();

        // Warmup
        validator.Validate(schema, arguments);

        // Act
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            validator.Validate(schema, arguments);
        }
        stopwatch.Stop();

        // Assert
        var averageMs = stopwatch.Elapsed.TotalMilliseconds / 100;
        averageMs.Should().BeLessThan(1.0,
            $"Average validation time was {averageMs:F3}ms, expected < 1ms");
    }

    [Fact]
    public void All18Schemas_Validation_Should_Complete_Under_20ms()
    {
        // Arrange
        var allSchemas = GetAllCoreToolSchemas();
        var validator = new JsonSchemaValidator();

        // Warmup
        foreach (var (schema, example) in allSchemas)
        {
            validator.Validate(schema, example);
        }

        // Act
        var stopwatch = Stopwatch.StartNew();
        foreach (var (schema, example) in allSchemas)
        {
            validator.Validate(schema, example);
        }
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(20,
            $"All 18 schema validations took {stopwatch.ElapsedMilliseconds}ms, expected < 20ms");
    }
}
```

---

## User Verification Steps

### Verification Scenario 1: Verify All Core Tools Are Registered

**Purpose:** Confirm that all 18 core tools are properly registered in the schema registry.

**Prerequisites:**
- Acode application built and running
- CLI available in PATH

**Steps:**
1. Open terminal/command prompt
2. Run: `acode tools list`
3. Verify output shows exactly 18 tools organized by category:
   - File Operations: read_file, write_file, list_directory, search_files, delete_file, move_file (6 tools)
   - Code Execution: execute_command, execute_script (2 tools)
   - Code Analysis: semantic_search, find_symbol, get_definition (3 tools)
   - Version Control: git_status, git_diff, git_log, git_commit (4 tools)
   - User Interaction: ask_user, confirm_action (2 tools)
4. Verify total count displayed: "Total: 18 core tools"
5. Run: `acode tools list --count`
6. Verify output shows: "18"

**Expected Result:** All 18 tools listed with correct categorization. No missing or extra tools.

### Verification Scenario 2: Verify read_file Schema Details

**Purpose:** Confirm read_file schema displays all parameters with correct constraints.

**Steps:**
1. Run: `acode tools show read_file`
2. Verify output shows:
   - Tool name: "read_file"
   - Version: "1.0.0"
   - Category: "File Operations"
   - Description contains: "Read the contents of a file"
3. Verify parameters section shows:
   - path: string, [required], maxLength 4096
   - start_line: integer, [optional], minimum 1
   - end_line: integer, [optional], minimum 1
   - encoding: string, [optional], enum [utf-8, ascii, utf-16], default utf-8
4. Verify example section shows valid JSON:
   ```json
   {"path": "README.md"}
   ```
5. Run: `acode tools show read_file --json`
6. Verify JSON output is valid and parseable

**Expected Result:** Schema displays all parameters, constraints, and examples correctly.

### Verification Scenario 3: Validate Valid read_file Arguments

**Purpose:** Confirm schema validation accepts valid arguments.

**Steps:**
1. Run validation with path only:
   ```bash
   echo '{"path": "README.md"}' | acode tools validate read_file
   ```
2. Verify output: "✓ Valid: Arguments conform to read_file schema"
3. Run validation with line range:
   ```bash
   echo '{"path": "src/main.cs", "start_line": 1, "end_line": 100}' | acode tools validate read_file
   ```
4. Verify output: "✓ Valid"
5. Run validation with encoding:
   ```bash
   echo '{"path": "data.txt", "encoding": "ascii"}' | acode tools validate read_file
   ```
6. Verify output: "✓ Valid"
7. Run validation with all optional parameters:
   ```bash
   echo '{"path": "file.txt", "start_line": 10, "end_line": 20, "encoding": "utf-16"}' | acode tools validate read_file
   ```
8. Verify output: "✓ Valid"

**Expected Result:** All valid argument combinations pass validation.

### Verification Scenario 4: Validate Invalid read_file Arguments

**Purpose:** Confirm schema validation correctly rejects invalid arguments.

**Steps:**
1. Test missing required path:
   ```bash
   echo '{}' | acode tools validate read_file
   ```
2. Verify output contains: "✗ Invalid" and "path" and "required"
3. Test wrong type for start_line:
   ```bash
   echo '{"path": "test.txt", "start_line": "ten"}' | acode tools validate read_file
   ```
4. Verify output contains: "✗ Invalid" and "start_line" and "integer"
5. Test invalid encoding value:
   ```bash
   echo '{"path": "test.txt", "encoding": "utf-32"}' | acode tools validate read_file
   ```
6. Verify output contains: "✗ Invalid" and "encoding" and "enum"
7. Test start_line below minimum:
   ```bash
   echo '{"path": "test.txt", "start_line": 0}' | acode tools validate read_file
   ```
8. Verify output contains: "✗ Invalid" and "start_line" and "minimum"

**Expected Result:** All invalid argument combinations are rejected with specific error messages.

### Verification Scenario 5: Verify execute_command Timeout Bounds

**Purpose:** Confirm execute_command enforces timeout_seconds constraints.

**Steps:**
1. Show schema constraints:
   ```bash
   acode tools show execute_command
   ```
2. Verify timeout_seconds shows: minimum 1, maximum 3600, default 300
3. Test valid timeout:
   ```bash
   echo '{"command": "echo test", "timeout_seconds": 60}' | acode tools validate execute_command
   ```
4. Verify: "✓ Valid"
5. Test timeout at minimum boundary:
   ```bash
   echo '{"command": "echo test", "timeout_seconds": 1}' | acode tools validate execute_command
   ```
6. Verify: "✓ Valid"
7. Test timeout at maximum boundary:
   ```bash
   echo '{"command": "echo test", "timeout_seconds": 3600}' | acode tools validate execute_command
   ```
8. Verify: "✓ Valid"
9. Test timeout below minimum:
   ```bash
   echo '{"command": "echo test", "timeout_seconds": 0}' | acode tools validate execute_command
   ```
10. Verify: "✗ Invalid" with "minimum" error
11. Test timeout above maximum:
    ```bash
    echo '{"command": "echo test", "timeout_seconds": 5000}' | acode tools validate execute_command
    ```
12. Verify: "✗ Invalid" with "maximum" error

**Expected Result:** Timeout values at and within bounds pass; values outside bounds are rejected.

### Verification Scenario 6: Verify execute_script Language Enum

**Purpose:** Confirm execute_script only accepts valid language values.

**Steps:**
1. Test PowerShell:
   ```bash
   echo '{"script": "Write-Host hello", "language": "powershell"}' | acode tools validate execute_script
   ```
2. Verify: "✓ Valid"
3. Test Bash:
   ```bash
   echo '{"script": "echo hello", "language": "bash"}' | acode tools validate execute_script
   ```
4. Verify: "✓ Valid"
5. Test Python:
   ```bash
   echo '{"script": "print(hello)", "language": "python"}' | acode tools validate execute_script
   ```
6. Verify: "✓ Valid"
7. Test invalid language:
   ```bash
   echo '{"script": "console.log(hello)", "language": "javascript"}' | acode tools validate execute_script
   ```
8. Verify: "✗ Invalid" with "language" and "enum" error
9. Test another invalid language:
   ```bash
   echo '{"script": "puts hello", "language": "ruby"}' | acode tools validate execute_script
   ```
10. Verify: "✗ Invalid" with enum error

**Expected Result:** Only powershell, bash, and python are accepted as valid languages.

### Verification Scenario 7: Verify git_log Count Bounds

**Purpose:** Confirm git_log enforces count parameter constraints.

**Steps:**
1. Show schema:
   ```bash
   acode tools show git_log
   ```
2. Verify count shows: integer, optional, minimum 1, maximum 100, default 10
3. Test without count (uses default):
   ```bash
   echo '{}' | acode tools validate git_log
   ```
4. Verify: "✓ Valid"
5. Test valid count:
   ```bash
   echo '{"count": 50}' | acode tools validate git_log
   ```
6. Verify: "✓ Valid"
7. Test count at minimum:
   ```bash
   echo '{"count": 1}' | acode tools validate git_log
   ```
8. Verify: "✓ Valid"
9. Test count at maximum:
   ```bash
   echo '{"count": 100}' | acode tools validate git_log
   ```
10. Verify: "✓ Valid"
11. Test count below minimum:
    ```bash
    echo '{"count": 0}' | acode tools validate git_log
    ```
12. Verify: "✗ Invalid"
13. Test count above maximum:
    ```bash
    echo '{"count": 500}' | acode tools validate git_log
    ```
14. Verify: "✗ Invalid"

**Expected Result:** Count values 1-100 pass; 0 and 101+ are rejected.

### Verification Scenario 8: Verify git_commit Message Constraints

**Purpose:** Confirm git_commit enforces message length constraints.

**Steps:**
1. Show schema:
   ```bash
   acode tools show git_commit
   ```
2. Verify message shows: string, required, minLength 1, maxLength 500
3. Test valid message:
   ```bash
   echo '{"message": "feat: add new feature"}' | acode tools validate git_commit
   ```
4. Verify: "✓ Valid"
5. Test message with files:
   ```bash
   echo '{"message": "fix: bug fix", "files": ["src/app.cs"]}' | acode tools validate git_commit
   ```
6. Verify: "✓ Valid"
7. Test empty message (below minLength):
   ```bash
   echo '{"message": ""}' | acode tools validate git_commit
   ```
8. Verify: "✗ Invalid" with minLength error
9. Test missing message:
   ```bash
   echo '{}' | acode tools validate git_commit
   ```
10. Verify: "✗ Invalid" with required error
11. Create a 600-character message and test:
    ```bash
    # Message exceeding 500 characters
    echo '{"message": "aaaa...(600 chars)...aaaa"}' | acode tools validate git_commit
    ```
12. Verify: "✗ Invalid" with maxLength error

**Expected Result:** Messages 1-500 characters pass; empty or >500 characters are rejected.

### Verification Scenario 9: Verify Schema Versions

**Purpose:** Confirm all core tools have version 1.0.0.

**Steps:**
1. For each core tool, run show command and check version:
   ```bash
   for tool in read_file write_file list_directory search_files delete_file move_file execute_command execute_script semantic_search find_symbol get_definition git_status git_diff git_log git_commit ask_user confirm_action; do
     echo "Checking $tool..."
     acode tools show $tool --version
   done
   ```
2. Verify each outputs: "1.0.0"
3. Alternative single command:
   ```bash
   acode tools list --show-versions
   ```
4. Verify all tools show "v1.0.0"

**Expected Result:** All 18 core tools have version 1.0.0.

### Verification Scenario 10: Verify Tool Schema Performance

**Purpose:** Confirm schema validation meets performance requirements.

**Steps:**
1. Run performance test (if available):
   ```bash
   acode tools benchmark
   ```
2. Verify output shows:
   - Average validation time: < 1ms per tool
   - Schema compilation time: < 500ms total
   - Memory usage for schemas: < 10MB
3. Alternative manual timing:
   ```bash
   # Time 100 validations
   time for i in {1..100}; do
     echo '{"path": "test.txt"}' | acode tools validate read_file > /dev/null
   done
   ```
4. Verify total time < 1 second (10ms average per validation)
5. Run memory check:
   ```bash
   acode tools stats --memory
   ```
6. Verify schema memory usage < 10MB

**Expected Result:** Validation < 1ms average, compilation < 500ms, memory < 10MB.

---

## Implementation Prompt for Claude

### Implementation Overview

This task implements the complete JSON Schema definitions for all 18 core tools in the Acode system. Each tool schema defines the parameters, types, constraints, and documentation that enable strict validation and model prompt construction. The implementation follows the Schema Provider pattern established in Task 007.

**What You'll Build:**
- CoreToolsProvider class implementing ISchemaProvider
- 18 individual schema classes organized by category
- Complete JSON Schema definitions with all constraints
- Example arguments for testing and documentation
- Unit tests validating all schemas
- Integration tests verifying registration

**Implementation Order:**
1. Create folder structure
2. Implement CoreToolsProvider shell
3. Implement File Operations schemas (6 tools)
4. Implement Code Execution schemas (2 tools)
5. Implement Code Analysis schemas (3 tools)
6. Implement Version Control schemas (4 tools)
7. Implement User Interaction schemas (2 tools)
8. Write unit tests for each schema
9. Write integration tests for registration
10. Register CoreToolsProvider in DI

### Prerequisites

**Required:**
- .NET 8 SDK installed
- Task 007 (Tool Schema Registry) complete
- Task 003 (Security Layer) for constraint reference

**NuGet Packages:**
- System.Text.Json (included in .NET 8)
- JsonSchema.Net (for schema validation)

### Step 1: Create Folder Structure

Create the following folder structure under `src/Acode.Infrastructure/`:

```
src/Acode.Infrastructure/ToolSchemas/
├── Providers/
│   ├── CoreToolsProvider.cs
│   └── Schemas/
│       ├── FileOperations/
│       │   ├── ReadFileSchema.cs
│       │   ├── WriteFileSchema.cs
│       │   ├── ListDirectorySchema.cs
│       │   ├── SearchFilesSchema.cs
│       │   ├── DeleteFileSchema.cs
│       │   └── MoveFileSchema.cs
│       ├── CodeExecution/
│       │   ├── ExecuteCommandSchema.cs
│       │   └── ExecuteScriptSchema.cs
│       ├── CodeAnalysis/
│       │   ├── SemanticSearchSchema.cs
│       │   ├── FindSymbolSchema.cs
│       │   └── GetDefinitionSchema.cs
│       ├── VersionControl/
│       │   ├── GitStatusSchema.cs
│       │   ├── GitDiffSchema.cs
│       │   ├── GitLogSchema.cs
│       │   └── GitCommitSchema.cs
│       └── UserInteraction/
│           ├── AskUserSchema.cs
│           └── ConfirmActionSchema.cs
```

### Step 2: Implement CoreToolsProvider

Create `src/Acode.Infrastructure/ToolSchemas/Providers/CoreToolsProvider.cs`:

```csharp
using Acode.Application.Interfaces;
using Acode.Domain.ToolSchemas;
using Acode.Infrastructure.ToolSchemas.Providers.Schemas.FileOperations;
using Acode.Infrastructure.ToolSchemas.Providers.Schemas.CodeExecution;
using Acode.Infrastructure.ToolSchemas.Providers.Schemas.CodeAnalysis;
using Acode.Infrastructure.ToolSchemas.Providers.Schemas.VersionControl;
using Acode.Infrastructure.ToolSchemas.Providers.Schemas.UserInteraction;

namespace Acode.Infrastructure.ToolSchemas.Providers;

/// <summary>
/// Provides schema definitions for all 18 core Acode tools.
/// Core tools are built-in capabilities for file operations, code execution,
/// code analysis, version control, and user interaction.
/// </summary>
public sealed class CoreToolsProvider : ISchemaProvider
{
    /// <summary>
    /// Registers all core tool schemas with the tool schema registry.
    /// </summary>
    /// <param name="registry">The registry to register schemas with.</param>
    public void RegisterSchemas(IToolSchemaRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        // File Operations (6 tools)
        registry.RegisterTool(ReadFileSchema.Create());
        registry.RegisterTool(WriteFileSchema.Create());
        registry.RegisterTool(ListDirectorySchema.Create());
        registry.RegisterTool(SearchFilesSchema.Create());
        registry.RegisterTool(DeleteFileSchema.Create());
        registry.RegisterTool(MoveFileSchema.Create());

        // Code Execution (2 tools)
        registry.RegisterTool(ExecuteCommandSchema.Create());
        registry.RegisterTool(ExecuteScriptSchema.Create());

        // Code Analysis (3 tools)
        registry.RegisterTool(SemanticSearchSchema.Create());
        registry.RegisterTool(FindSymbolSchema.Create());
        registry.RegisterTool(GetDefinitionSchema.Create());

        // Version Control (4 tools)
        registry.RegisterTool(GitStatusSchema.Create());
        registry.RegisterTool(GitDiffSchema.Create());
        registry.RegisterTool(GitLogSchema.Create());
        registry.RegisterTool(GitCommitSchema.Create());

        // User Interaction (2 tools)
        registry.RegisterTool(AskUserSchema.Create());
        registry.RegisterTool(ConfirmActionSchema.Create());
    }
}
```

### Step 3: Implement File Operations Schemas

#### ReadFileSchema.cs

```csharp
using System.Text.Json;
using Acode.Domain.ToolSchemas;

namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.FileOperations;

/// <summary>
/// Schema definition for the read_file tool.
/// Reads the contents of a file from the file system.
/// </summary>
public static class ReadFileSchema
{
    public static ToolDefinition Create() => new()
    {
        Name = "read_file",
        Description = "Read the contents of a file from the file system. Use this tool to examine source code, configuration files, documentation, or any text file in the workspace.",
        Version = "1.0.0",
        Category = ToolCategory.FileOperations,
        Parameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "path": {
                    "type": "string",
                    "description": "Path to the file to read. Can be absolute or relative to workspace root. Examples: 'README.md', 'src/Program.cs', '/home/user/file.txt'",
                    "maxLength": 4096
                },
                "start_line": {
                    "type": "integer",
                    "description": "Line number to start reading from (1-indexed). First line of file is 1. Must be <= end_line when both are specified.",
                    "minimum": 1
                },
                "end_line": {
                    "type": "integer",
                    "description": "Line number to stop reading at (inclusive). Must be >= start_line when both are specified.",
                    "minimum": 1
                },
                "encoding": {
                    "type": "string",
                    "description": "Text encoding for reading the file. Use 'utf-8' for most source code (default), 'ascii' for legacy files, 'utf-16' for Windows Unicode files.",
                    "enum": ["utf-8", "ascii", "utf-16"],
                    "default": "utf-8"
                }
            },
            "required": ["path"],
            "additionalProperties": false
        }
        """).RootElement,
        Examples = new[]
        {
            JsonDocument.Parse("""{"path": "README.md"}""").RootElement,
            JsonDocument.Parse("""{"path": "src/Program.cs", "start_line": 1, "end_line": 100}""").RootElement,
            JsonDocument.Parse("""{"path": "data.txt", "encoding": "ascii"}""").RootElement
        }
    };
}
```

#### WriteFileSchema.cs

```csharp
using System.Text.Json;
using Acode.Domain.ToolSchemas;

namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.FileOperations;

/// <summary>
/// Schema definition for the write_file tool.
/// Writes content to a file, creating it if it doesn't exist.
/// </summary>
public static class WriteFileSchema
{
    public static ToolDefinition Create() => new()
    {
        Name = "write_file",
        Description = "Write content to a file, creating it if it doesn't exist. Use this tool to create new files or update existing files with new content.",
        Version = "1.0.0",
        Category = ToolCategory.FileOperations,
        Parameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "path": {
                    "type": "string",
                    "description": "Path where the file will be written. Creates file if it doesn't exist. Examples: 'output.txt', 'src/NewClass.cs'",
                    "maxLength": 4096
                },
                "content": {
                    "type": "string",
                    "description": "Content to write to the file. Maximum 1MB (1,048,576 characters). For larger files, write in multiple chunks.",
                    "maxLength": 1048576
                },
                "create_directories": {
                    "type": "boolean",
                    "description": "Create parent directories if they don't exist (default: false). Set to true when writing to new folder structures.",
                    "default": false
                },
                "overwrite": {
                    "type": "boolean",
                    "description": "Overwrite file if it already exists (default: true). Set to false to prevent accidental overwrites.",
                    "default": true
                },
                "encoding": {
                    "type": "string",
                    "description": "Text encoding for writing the file (default: utf-8).",
                    "enum": ["utf-8", "ascii", "utf-16"],
                    "default": "utf-8"
                }
            },
            "required": ["path", "content"],
            "additionalProperties": false
        }
        """).RootElement,
        Examples = new[]
        {
            JsonDocument.Parse("""{"path": "output.txt", "content": "Hello, World!"}""").RootElement,
            JsonDocument.Parse("""{"path": "src/services/UserService.cs", "content": "public class UserService { }", "create_directories": true}""").RootElement,
            JsonDocument.Parse("""{"path": "config.json", "content": "{\"key\": \"value\"}", "overwrite": false}""").RootElement
        }
    };
}
```

#### ListDirectorySchema.cs

```csharp
using System.Text.Json;
using Acode.Domain.ToolSchemas;

namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.FileOperations;

/// <summary>
/// Schema definition for the list_directory tool.
/// Lists contents of a directory.
/// </summary>
public static class ListDirectorySchema
{
    public static ToolDefinition Create() => new()
    {
        Name = "list_directory",
        Description = "List the contents of a directory, showing files and subdirectories. Use this tool to explore the workspace structure and find files.",
        Version = "1.0.0",
        Category = ToolCategory.FileOperations,
        Parameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "path": {
                    "type": "string",
                    "description": "Path to the directory to list. Examples: '.', 'src', 'tests/unit'",
                    "maxLength": 4096
                },
                "recursive": {
                    "type": "boolean",
                    "description": "List contents recursively including subdirectories (default: false). Use with max_depth to limit depth.",
                    "default": false
                },
                "pattern": {
                    "type": "string",
                    "description": "Glob pattern to filter results. Examples: '*.cs' for C# files, '*.test.js' for test files, 'README*' for readme files.",
                    "maxLength": 256
                },
                "max_depth": {
                    "type": "integer",
                    "description": "Maximum directory depth for recursive listing (default: unlimited). 1 = immediate children only.",
                    "minimum": 1,
                    "maximum": 100
                },
                "include_hidden": {
                    "type": "boolean",
                    "description": "Include hidden files and directories starting with '.' (default: false).",
                    "default": false
                }
            },
            "required": ["path"],
            "additionalProperties": false
        }
        """).RootElement,
        Examples = new[]
        {
            JsonDocument.Parse("""{"path": "src"}""").RootElement,
            JsonDocument.Parse("""{"path": ".", "recursive": true, "pattern": "*.cs"}""").RootElement,
            JsonDocument.Parse("""{"path": "tests", "recursive": true, "max_depth": 2}""").RootElement
        }
    };
}
```

#### SearchFilesSchema.cs

```csharp
using System.Text.Json;
using Acode.Domain.ToolSchemas;

namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.FileOperations;

/// <summary>
/// Schema definition for the search_files tool.
/// Searches for text patterns in files.
/// </summary>
public static class SearchFilesSchema
{
    public static ToolDefinition Create() => new()
    {
        Name = "search_files",
        Description = "Search for text patterns in files within the workspace. Use this tool to find code, configuration values, or any text across multiple files.",
        Version = "1.0.0",
        Category = ToolCategory.FileOperations,
        Parameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "query": {
                    "type": "string",
                    "description": "Text pattern to search for. Examples: 'TODO', 'public class', 'import React'",
                    "minLength": 1,
                    "maxLength": 1000
                },
                "path": {
                    "type": "string",
                    "description": "Directory to search in (default: workspace root). Examples: 'src', 'tests/unit'",
                    "maxLength": 4096
                },
                "pattern": {
                    "type": "string",
                    "description": "Glob pattern to filter files. Examples: '*.cs', '*.ts', '*.json'",
                    "maxLength": 256
                },
                "case_sensitive": {
                    "type": "boolean",
                    "description": "Perform case-sensitive search (default: false).",
                    "default": false
                },
                "regex": {
                    "type": "boolean",
                    "description": "Treat query as regular expression (default: false).",
                    "default": false
                },
                "max_results": {
                    "type": "integer",
                    "description": "Maximum number of results to return (default: 100). Range: 1-1000.",
                    "minimum": 1,
                    "maximum": 1000,
                    "default": 100
                }
            },
            "required": ["query"],
            "additionalProperties": false
        }
        """).RootElement,
        Examples = new[]
        {
            JsonDocument.Parse("""{"query": "TODO"}""").RootElement,
            JsonDocument.Parse("""{"query": "public class", "path": "src", "pattern": "*.cs"}""").RootElement,
            JsonDocument.Parse("""{"query": "^import.*React", "regex": true, "pattern": "*.tsx"}""").RootElement
        }
    };
}
```

#### DeleteFileSchema.cs

```csharp
using System.Text.Json;
using Acode.Domain.ToolSchemas;

namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.FileOperations;

/// <summary>
/// Schema definition for the delete_file tool.
/// Deletes a file from the file system.
/// </summary>
public static class DeleteFileSchema
{
    public static ToolDefinition Create() => new()
    {
        Name = "delete_file",
        Description = "Delete a file from the file system. This is a destructive operation - use with caution and prefer setting confirm to true for safety.",
        Version = "1.0.0",
        Category = ToolCategory.FileOperations,
        Parameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "path": {
                    "type": "string",
                    "description": "Path to the file to delete. Examples: 'temp.txt', 'old/deprecated.cs'",
                    "maxLength": 4096
                },
                "confirm": {
                    "type": "boolean",
                    "description": "Explicit confirmation for deletion (default: false). Set to true to confirm this destructive operation.",
                    "default": false
                }
            },
            "required": ["path"],
            "additionalProperties": false
        }
        """).RootElement,
        Examples = new[]
        {
            JsonDocument.Parse("""{"path": "temp.txt", "confirm": true}""").RootElement,
            JsonDocument.Parse("""{"path": "old/deprecated.cs", "confirm": true}""").RootElement
        }
    };
}
```

#### MoveFileSchema.cs

```csharp
using System.Text.Json;
using Acode.Domain.ToolSchemas;

namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.FileOperations;

/// <summary>
/// Schema definition for the move_file tool.
/// Moves or renames a file.
/// </summary>
public static class MoveFileSchema
{
    public static ToolDefinition Create() => new()
    {
        Name = "move_file",
        Description = "Move or rename a file. Can be used to reorganize files or rename them in place.",
        Version = "1.0.0",
        Category = ToolCategory.FileOperations,
        Parameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "source": {
                    "type": "string",
                    "description": "Path to the source file to move. Examples: 'old_name.cs', 'src/temp/file.txt'",
                    "maxLength": 4096
                },
                "destination": {
                    "type": "string",
                    "description": "Path where the file will be moved to. Examples: 'new_name.cs', 'src/proper/file.txt'",
                    "maxLength": 4096
                },
                "overwrite": {
                    "type": "boolean",
                    "description": "Overwrite destination if it exists (default: false). Set to true to replace existing file.",
                    "default": false
                }
            },
            "required": ["source", "destination"],
            "additionalProperties": false
        }
        """).RootElement,
        Examples = new[]
        {
            JsonDocument.Parse("""{"source": "OldClass.cs", "destination": "NewClass.cs"}""").RootElement,
            JsonDocument.Parse("""{"source": "temp/file.txt", "destination": "final/file.txt", "overwrite": true}""").RootElement
        }
    };
}
```

### Step 4: Implement Code Execution Schemas

#### ExecuteCommandSchema.cs

```csharp
using System.Text.Json;
using Acode.Domain.ToolSchemas;

namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.CodeExecution;

/// <summary>
/// Schema definition for the execute_command tool.
/// Executes a shell command and returns output.
/// </summary>
public static class ExecuteCommandSchema
{
    public static ToolDefinition Create() => new()
    {
        Name = "execute_command",
        Description = "Execute a shell command and return its output. Use for running build tools, tests, git commands, package managers, and other CLI tools.",
        Version = "1.0.0",
        Category = ToolCategory.CodeExecution,
        Parameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "command": {
                    "type": "string",
                    "description": "The shell command to execute. Examples: 'dotnet build', 'npm test', 'git status', 'ls -la'",
                    "maxLength": 8192
                },
                "working_directory": {
                    "type": "string",
                    "description": "Directory to run the command in (default: workspace root). Examples: 'src/api', 'frontend'",
                    "maxLength": 4096
                },
                "timeout_seconds": {
                    "type": "integer",
                    "description": "Maximum time to wait for command completion (default: 300 seconds / 5 minutes). Range: 1-3600 seconds (1 hour max).",
                    "minimum": 1,
                    "maximum": 3600,
                    "default": 300
                },
                "env": {
                    "type": "object",
                    "description": "Environment variables to set for the command. All values must be strings. Example: {\"NODE_ENV\": \"test\", \"DEBUG\": \"true\"}",
                    "additionalProperties": {
                        "type": "string"
                    }
                }
            },
            "required": ["command"],
            "additionalProperties": false
        }
        """).RootElement,
        Examples = new[]
        {
            JsonDocument.Parse("""{"command": "dotnet build"}""").RootElement,
            JsonDocument.Parse("""{"command": "npm test", "working_directory": "frontend", "timeout_seconds": 600}""").RootElement,
            JsonDocument.Parse("""{"command": "node app.js", "env": {"NODE_ENV": "production", "PORT": "3000"}}""").RootElement
        }
    };
}
```

#### ExecuteScriptSchema.cs

```csharp
using System.Text.Json;
using Acode.Domain.ToolSchemas;

namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.CodeExecution;

/// <summary>
/// Schema definition for the execute_script tool.
/// Executes a script in PowerShell, Bash, or Python.
/// </summary>
public static class ExecuteScriptSchema
{
    public static ToolDefinition Create() => new()
    {
        Name = "execute_script",
        Description = "Execute a script in PowerShell, Bash, or Python. Use for multi-line operations, complex logic, or when a single command is insufficient.",
        Version = "1.0.0",
        Category = ToolCategory.CodeExecution,
        Parameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "script": {
                    "type": "string",
                    "description": "The script content to execute. Can be multi-line. Maximum 64KB.",
                    "maxLength": 65536
                },
                "language": {
                    "type": "string",
                    "description": "Script language to use. 'powershell' for Windows, 'bash' for Unix/Linux/macOS, 'python' for cross-platform.",
                    "enum": ["powershell", "bash", "python"]
                },
                "working_directory": {
                    "type": "string",
                    "description": "Directory to run the script in (default: workspace root).",
                    "maxLength": 4096
                },
                "timeout_seconds": {
                    "type": "integer",
                    "description": "Maximum time to wait for script completion (default: 300 seconds). Range: 1-3600.",
                    "minimum": 1,
                    "maximum": 3600,
                    "default": 300
                }
            },
            "required": ["script", "language"],
            "additionalProperties": false
        }
        """).RootElement,
        Examples = new[]
        {
            JsonDocument.Parse("""{"script": "Get-ChildItem -Recurse | Where-Object { $_.Extension -eq '.cs' }", "language": "powershell"}""").RootElement,
            JsonDocument.Parse("""{"script": "find . -name '*.cs' -exec wc -l {} +", "language": "bash"}""").RootElement,
            JsonDocument.Parse("""{"script": "import os\nfor f in os.listdir('.'):\n    print(f)", "language": "python"}""").RootElement
        }
    };
}
```

### Step 5: Implement Code Analysis Schemas

#### SemanticSearchSchema.cs

```csharp
using System.Text.Json;
using Acode.Domain.ToolSchemas;

namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.CodeAnalysis;

/// <summary>
/// Schema definition for the semantic_search tool.
/// Performs semantic search across the codebase.
/// </summary>
public static class SemanticSearchSchema
{
    public static ToolDefinition Create() => new()
    {
        Name = "semantic_search",
        Description = "Search the codebase semantically using natural language. Unlike text search, this understands meaning and can find related code even without exact text matches.",
        Version = "1.0.0",
        Category = ToolCategory.CodeAnalysis,
        Parameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "query": {
                    "type": "string",
                    "description": "Natural language search query. Examples: 'user authentication logic', 'database connection handling', 'error logging'",
                    "minLength": 3,
                    "maxLength": 500
                },
                "scope": {
                    "type": "string",
                    "description": "Search scope: 'workspace' for entire project, 'directory' for specific folder, 'file' for single file.",
                    "enum": ["workspace", "directory", "file"],
                    "default": "workspace"
                },
                "path": {
                    "type": "string",
                    "description": "Path for 'directory' or 'file' scope. Required when scope is not 'workspace'.",
                    "maxLength": 4096
                },
                "max_results": {
                    "type": "integer",
                    "description": "Maximum number of results to return (default: 20). Range: 1-100.",
                    "minimum": 1,
                    "maximum": 100,
                    "default": 20
                }
            },
            "required": ["query"],
            "additionalProperties": false
        }
        """).RootElement,
        Examples = new[]
        {
            JsonDocument.Parse("""{"query": "user authentication and login handling"}""").RootElement,
            JsonDocument.Parse("""{"query": "database connection pooling", "scope": "directory", "path": "src/infrastructure"}""").RootElement,
            JsonDocument.Parse("""{"query": "error handling", "max_results": 50}""").RootElement
        }
    };
}
```

#### FindSymbolSchema.cs

```csharp
using System.Text.Json;
using Acode.Domain.ToolSchemas;

namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.CodeAnalysis;

/// <summary>
/// Schema definition for the find_symbol tool.
/// Finds symbol definitions in the codebase.
/// </summary>
public static class FindSymbolSchema
{
    public static ToolDefinition Create() => new()
    {
        Name = "find_symbol",
        Description = "Find the definition of a symbol (class, method, function, variable) in the codebase. Use to navigate to where something is defined.",
        Version = "1.0.0",
        Category = ToolCategory.CodeAnalysis,
        Parameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "symbol_name": {
                    "type": "string",
                    "description": "Name of the symbol to find. Examples: 'UserService', 'HandleLogin', 'connectionString'",
                    "minLength": 1,
                    "maxLength": 500
                },
                "symbol_type": {
                    "type": "string",
                    "description": "Filter by symbol type to narrow results.",
                    "enum": ["class", "method", "function", "variable", "interface", "enum", "property"]
                },
                "path": {
                    "type": "string",
                    "description": "Limit search to specific directory or file.",
                    "maxLength": 4096
                },
                "include_references": {
                    "type": "boolean",
                    "description": "Also find all references/usages of the symbol (default: false).",
                    "default": false
                }
            },
            "required": ["symbol_name"],
            "additionalProperties": false
        }
        """).RootElement,
        Examples = new[]
        {
            JsonDocument.Parse("""{"symbol_name": "UserService"}""").RootElement,
            JsonDocument.Parse("""{"symbol_name": "HandleLogin", "symbol_type": "method"}""").RootElement,
            JsonDocument.Parse("""{"symbol_name": "DbContext", "include_references": true}""").RootElement
        }
    };
}
```

#### GetDefinitionSchema.cs

```csharp
using System.Text.Json;
using Acode.Domain.ToolSchemas;

namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.CodeAnalysis;

/// <summary>
/// Schema definition for the get_definition tool.
/// Gets the definition at a specific file location.
/// </summary>
public static class GetDefinitionSchema
{
    public static ToolDefinition Create() => new()
    {
        Name = "get_definition",
        Description = "Get the definition of the symbol at a specific location in a file. Like 'Go to Definition' in an IDE - follows the symbol to where it's defined.",
        Version = "1.0.0",
        Category = ToolCategory.CodeAnalysis,
        Parameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "file_path": {
                    "type": "string",
                    "description": "Path to the file containing the symbol reference.",
                    "maxLength": 4096
                },
                "line": {
                    "type": "integer",
                    "description": "Line number in the file (1-indexed). First line is 1.",
                    "minimum": 1
                },
                "column": {
                    "type": "integer",
                    "description": "Column number in the line (1-indexed). First character is 1.",
                    "minimum": 1
                }
            },
            "required": ["file_path", "line", "column"],
            "additionalProperties": false
        }
        """).RootElement,
        Examples = new[]
        {
            JsonDocument.Parse("""{"file_path": "src/Services/UserService.cs", "line": 25, "column": 10}""").RootElement,
            JsonDocument.Parse("""{"file_path": "tests/UserTests.cs", "line": 15, "column": 20}""").RootElement
        }
    };
}
```

### Step 6: Implement Version Control Schemas

#### GitStatusSchema.cs

```csharp
using System.Text.Json;
using Acode.Domain.ToolSchemas;

namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.VersionControl;

/// <summary>
/// Schema definition for the git_status tool.
/// Shows the working tree status.
/// </summary>
public static class GitStatusSchema
{
    public static ToolDefinition Create() => new()
    {
        Name = "git_status",
        Description = "Show the current git repository status including staged, unstaged, and untracked files. Equivalent to 'git status'.",
        Version = "1.0.0",
        Category = ToolCategory.VersionControl,
        Parameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "path": {
                    "type": "string",
                    "description": "Path to the git repository (default: current workspace). Usually not needed.",
                    "maxLength": 4096
                }
            },
            "additionalProperties": false
        }
        """).RootElement,
        Examples = new[]
        {
            JsonDocument.Parse("""{}""").RootElement,
            JsonDocument.Parse("""{"path": "submodule/"}""").RootElement
        }
    };
}
```

#### GitDiffSchema.cs

```csharp
using System.Text.Json;
using Acode.Domain.ToolSchemas;

namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.VersionControl;

/// <summary>
/// Schema definition for the git_diff tool.
/// Shows changes between commits, working tree, etc.
/// </summary>
public static class GitDiffSchema
{
    public static ToolDefinition Create() => new()
    {
        Name = "git_diff",
        Description = "Show changes between commits, the working tree, or staged changes. Equivalent to 'git diff'.",
        Version = "1.0.0",
        Category = ToolCategory.VersionControl,
        Parameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "path": {
                    "type": "string",
                    "description": "Limit diff to specific file or directory.",
                    "maxLength": 4096
                },
                "staged": {
                    "type": "boolean",
                    "description": "Show staged changes only (default: false). Equivalent to 'git diff --staged'.",
                    "default": false
                },
                "commit": {
                    "type": "string",
                    "description": "Compare working tree against this commit/ref. Examples: 'HEAD~1', 'main', 'abc123'",
                    "maxLength": 100
                }
            },
            "additionalProperties": false
        }
        """).RootElement,
        Examples = new[]
        {
            JsonDocument.Parse("""{}""").RootElement,
            JsonDocument.Parse("""{"staged": true}""").RootElement,
            JsonDocument.Parse("""{"path": "src/main.cs"}""").RootElement,
            JsonDocument.Parse("""{"commit": "HEAD~1"}""").RootElement
        }
    };
}
```

#### GitLogSchema.cs

```csharp
using System.Text.Json;
using Acode.Domain.ToolSchemas;

namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.VersionControl;

/// <summary>
/// Schema definition for the git_log tool.
/// Shows commit history.
/// </summary>
public static class GitLogSchema
{
    public static ToolDefinition Create() => new()
    {
        Name = "git_log",
        Description = "Show commit history log. Equivalent to 'git log --oneline'.",
        Version = "1.0.0",
        Category = ToolCategory.VersionControl,
        Parameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "count": {
                    "type": "integer",
                    "description": "Number of commits to show (default: 10). Range: 1-100.",
                    "minimum": 1,
                    "maximum": 100,
                    "default": 10
                },
                "path": {
                    "type": "string",
                    "description": "Show only commits affecting this file or directory.",
                    "maxLength": 4096
                },
                "author": {
                    "type": "string",
                    "description": "Filter commits by author name or email.",
                    "maxLength": 200
                }
            },
            "additionalProperties": false
        }
        """).RootElement,
        Examples = new[]
        {
            JsonDocument.Parse("""{}""").RootElement,
            JsonDocument.Parse("""{"count": 20}""").RootElement,
            JsonDocument.Parse("""{"path": "src/main.cs", "count": 5}""").RootElement,
            JsonDocument.Parse("""{"author": "john@example.com"}""").RootElement
        }
    };
}
```

#### GitCommitSchema.cs

```csharp
using System.Text.Json;
using Acode.Domain.ToolSchemas;

namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.VersionControl;

/// <summary>
/// Schema definition for the git_commit tool.
/// Records changes to the repository.
/// </summary>
public static class GitCommitSchema
{
    public static ToolDefinition Create() => new()
    {
        Name = "git_commit",
        Description = "Create a git commit with the specified message. Stage files first with 'files' parameter or use 'all' to commit all changes.",
        Version = "1.0.0",
        Category = ToolCategory.VersionControl,
        Parameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "message": {
                    "type": "string",
                    "description": "Commit message. Should be descriptive and follow conventional commit format if applicable. Examples: 'feat: add user login', 'fix: resolve null reference'",
                    "minLength": 1,
                    "maxLength": 500
                },
                "files": {
                    "type": "array",
                    "description": "Specific files to stage and commit. If omitted, commits currently staged changes.",
                    "items": {
                        "type": "string",
                        "maxLength": 4096
                    }
                },
                "all": {
                    "type": "boolean",
                    "description": "Stage all modified and deleted files before committing (default: false). Equivalent to 'git commit -a'.",
                    "default": false
                }
            },
            "required": ["message"],
            "additionalProperties": false
        }
        """).RootElement,
        Examples = new[]
        {
            JsonDocument.Parse("""{"message": "feat: add user authentication"}""").RootElement,
            JsonDocument.Parse("""{"message": "fix: resolve null reference in UserService", "files": ["src/UserService.cs", "tests/UserServiceTests.cs"]}""").RootElement,
            JsonDocument.Parse("""{"message": "chore: update dependencies", "all": true}""").RootElement
        }
    };
}
```

### Step 7: Implement User Interaction Schemas

#### AskUserSchema.cs

```csharp
using System.Text.Json;
using Acode.Domain.ToolSchemas;

namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.UserInteraction;

/// <summary>
/// Schema definition for the ask_user tool.
/// Asks the user a question and waits for response.
/// </summary>
public static class AskUserSchema
{
    public static ToolDefinition Create() => new()
    {
        Name = "ask_user",
        Description = "Ask the user a question and wait for their response. Use when you need clarification, preferences, or decisions from the user.",
        Version = "1.0.0",
        Category = ToolCategory.UserInteraction,
        Parameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "question": {
                    "type": "string",
                    "description": "The question to ask the user. Be clear and specific.",
                    "minLength": 1,
                    "maxLength": 500
                },
                "options": {
                    "type": "array",
                    "description": "Optional list of choices for the user. If provided, user must select one.",
                    "items": {
                        "type": "string",
                        "maxLength": 200
                    },
                    "maxItems": 10
                },
                "default_option": {
                    "type": "string",
                    "description": "Default option if user presses Enter without selecting. Must be one of the options.",
                    "maxLength": 200
                }
            },
            "required": ["question"],
            "additionalProperties": false
        }
        """).RootElement,
        Examples = new[]
        {
            JsonDocument.Parse("""{"question": "What database provider should I use for this project?"}""").RootElement,
            JsonDocument.Parse("""{"question": "Which testing framework do you prefer?", "options": ["xUnit", "NUnit", "MSTest"], "default_option": "xUnit"}""").RootElement,
            JsonDocument.Parse("""{"question": "Should I include logging in this service?", "options": ["Yes", "No"], "default_option": "Yes"}""").RootElement
        }
    };
}
```

#### ConfirmActionSchema.cs

```csharp
using System.Text.Json;
using Acode.Domain.ToolSchemas;

namespace Acode.Infrastructure.ToolSchemas.Providers.Schemas.UserInteraction;

/// <summary>
/// Schema definition for the confirm_action tool.
/// Requests user confirmation for an action.
/// </summary>
public static class ConfirmActionSchema
{
    public static ToolDefinition Create() => new()
    {
        Name = "confirm_action",
        Description = "Request explicit user confirmation before performing an action. Use for potentially dangerous or irreversible operations.",
        Version = "1.0.0",
        Category = ToolCategory.UserInteraction,
        Parameters = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "action_description": {
                    "type": "string",
                    "description": "Clear description of what action will be performed. Be specific about consequences.",
                    "minLength": 10,
                    "maxLength": 500
                },
                "destructive": {
                    "type": "boolean",
                    "description": "Mark as destructive operation (default: false). Destructive actions show additional warnings.",
                    "default": false
                }
            },
            "required": ["action_description"],
            "additionalProperties": false
        }
        """).RootElement,
        Examples = new[]
        {
            JsonDocument.Parse("""{"action_description": "Delete all files in the 'temp' directory"}""").RootElement,
            JsonDocument.Parse("""{"action_description": "Drop and recreate the database, losing all data", "destructive": true}""").RootElement,
            JsonDocument.Parse("""{"action_description": "Push changes to the main branch"}""").RootElement
        }
    };
}
```

### Step 8: Register in Dependency Injection

Add registration in `src/Acode.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`:

```csharp
using Acode.Infrastructure.ToolSchemas.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Acode.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Existing registrations...

        // Register CoreToolsProvider
        services.AddSingleton<ISchemaProvider, CoreToolsProvider>();

        return services;
    }
}
```

### Step 9: Implementation Checklist

- [ ] Create folder structure under ToolSchemas/Providers/Schemas/
- [ ] Implement CoreToolsProvider.cs
- [ ] Implement ReadFileSchema.cs
- [ ] Implement WriteFileSchema.cs
- [ ] Implement ListDirectorySchema.cs
- [ ] Implement SearchFilesSchema.cs
- [ ] Implement DeleteFileSchema.cs
- [ ] Implement MoveFileSchema.cs
- [ ] Implement ExecuteCommandSchema.cs
- [ ] Implement ExecuteScriptSchema.cs
- [ ] Implement SemanticSearchSchema.cs
- [ ] Implement FindSymbolSchema.cs
- [ ] Implement GetDefinitionSchema.cs
- [ ] Implement GitStatusSchema.cs
- [ ] Implement GitDiffSchema.cs
- [ ] Implement GitLogSchema.cs
- [ ] Implement GitCommitSchema.cs
- [ ] Implement AskUserSchema.cs
- [ ] Implement ConfirmActionSchema.cs
- [ ] Register CoreToolsProvider in DI
- [ ] Write FileOperationsSchemaTests.cs
- [ ] Write CodeExecutionSchemaTests.cs
- [ ] Write CodeAnalysisSchemaTests.cs
- [ ] Write VersionControlSchemaTests.cs
- [ ] Write UserInteractionSchemaTests.cs
- [ ] Write CoreToolsRegistrationTests.cs
- [ ] Run all tests: `dotnet test --filter "FullyQualifiedName~CoreTools"`
- [ ] Verify CLI displays schemas: `acode tools list`

### Dependencies

- **Task 007:** Tool Schema Registry provides IToolSchemaRegistry and ISchemaProvider interfaces
- **Task 003:** Security Layer informs constraint values (path lengths, timeout limits)
- **Task 006.b:** Structured Outputs will use these schemas for constrained generation

### Verification Command

```bash
# Build and test
dotnet build
dotnet test --filter "FullyQualifiedName~CoreTools"

# Verify registration
acode tools list
acode tools show read_file
acode tools show execute_command
acode tools show git_commit

# Validate examples
echo '{"path": "test.txt"}' | acode tools validate read_file
echo '{"command": "dotnet build"}' | acode tools validate execute_command
echo '{"message": "test commit"}' | acode tools validate git_commit
```

---

**End of Task 007.a Specification**