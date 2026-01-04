# Task 017.b: TS/JS Symbol Extraction

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 017 (Symbol Index v2)  

---

## Description

### Business Value

TypeScript and JavaScript symbol extraction enables the agent to understand web and Node.js codebases, which represent a significant portion of modern software development. Without TS/JS parsing capabilities, the agent cannot navigate React components, understand Express routes, or comprehend the structure of frontend applications. This task implements production-grade TypeScript/JavaScript parsing using the TypeScript Compiler API.

The TypeScript Compiler API provides complete AST access comparable to Roslyn for C#. It handles both TypeScript's rich type system and JavaScript's dynamic nature. Since TypeScript is a superset of JavaScript, a single extractor implementation can process both languages, with TypeScript files providing richer type information. This unified approach reduces complexity while maximizing language coverage.

The extracted symbols integrate with the Symbol Index (Task 017) to power cross-language code intelligence. When a user asks the agent to modify a React component or an API endpoint, the symbol extractor identifies functions, classes, exports, and their associated JSDoc documentation. The extractor runs in a Node.js subprocess, communicating with the .NET agent via a JSON message protocol, ensuring isolation and proper language runtime support.

### Scope

This task delivers the following components:

1. **NodeBridge** - .NET component that spawns and manages the Node.js extraction process with JSON stdin/stdout protocol
2. **TypeScript Extractor (Node.js)** - Node.js application using TypeScript Compiler API to parse and extract symbols
3. **JSDocExtractor** - Parser for JSDoc comments (@param, @returns, @description, @example, @deprecated)
4. **MessageProtocol** - Defines request/response message formats for cross-process communication
5. **TypeScriptSymbolExtractor** - ISymbolExtractor implementation that orchestrates the Node.js bridge for .ts/.js/.tsx/.jsx files

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Symbol Index (Task 017) | Consumer | Receives extracted symbols for indexing and querying |
| Dependency Mapper (Task 017.c) | Consumer | Uses extracted symbols to build import/export relationships |
| Context Packer | Consumer | Retrieves symbol metadata when packing code context for prompts |
| Configuration System | Configuration | Reads extraction settings (include JS, include JSX, extract JSDoc) |
| File System Abstraction | Dependency | Reads TypeScript/JavaScript source files |
| Node.js Runtime | External Dependency | Required runtime for TypeScript Compiler API execution |
| Logging Infrastructure | Dependency | Reports extraction progress, errors, and bridge status |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Node.js not installed | Extractor unavailable | Clear error message with installation instructions, graceful feature degradation |
| Node.js process crash | Extraction interruption | Auto-restart bridge with exponential backoff, report partial results |
| Invalid TypeScript syntax | Partial AST available | Extract from valid portions, log diagnostics |
| Large file timeout | Extraction hangs | Configurable timeout (default 30s), return partial results |
| Missing tsconfig.json | Reduced type resolution | Fall back to default compiler options |
| Memory exhaustion in Node | Process crash | Memory limits on Node process, file size restrictions |

### Assumptions

1. Node.js 18.x or higher is installed and available in PATH
2. TypeScript Compiler API is bundled with the extraction tool (no external npm install required)
3. Source files use UTF-8 encoding
4. JSDoc follows standard format (@param, @returns, etc.)
5. ES modules (import/export) and CommonJS (require/module.exports) are both supported
6. React JSX/TSX syntax is handled via appropriate compiler options
7. The Node.js bridge process has a maximum lifespan and is recycled periodically
8. tsconfig.json is used when present but not required for basic extraction

### Security Considerations

1. **Process Isolation** - Node.js runs in a separate process with no .NET memory access
2. **No Code Execution** - TypeScript parsing MUST NOT execute source code or eval() statements
3. **Path Validation** - All file paths MUST be validated before passing to Node.js subprocess
4. **Input Sanitization** - JSON messages MUST be validated to prevent injection attacks
5. **Resource Limits** - Node.js process MUST have memory and CPU limits to prevent DoS

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| TypeScript | Typed superset of JavaScript |
| TSC | TypeScript Compiler |
| AST | Abstract Syntax Tree |
| SourceFile | TypeScript's file representation |
| Node | AST node type |
| Symbol | TypeScript's Symbol type |
| Declaration | Symbol definition location |
| JSDoc | JavaScript documentation comments |
| ES Module | ECMAScript module system |
| CommonJS | Node.js module system |
| Export | Module's public interface |
| Import | Module dependency |
| Decorator | Metadata annotation |
| JSX | JavaScript XML syntax |
| TSX | TypeScript with JSX |

---

## Out of Scope

The following items are explicitly excluded from Task 017.b:

- **C#/Roslyn** - See Task 017.a
- **Python/Go/Rust** - Future versions
- **Type checking** - Parse only
- **Transpilation** - Read-only
- **Bundling** - Extraction only
- **Node.js runtime** - Parse only
- **IDE features** - CLI only

---

## Functional Requirements

### TypeScript Integration (FR-017b-01 to FR-017b-06)

| ID | Requirement |
|----|-------------|
| FR-017b-01 | System MUST initialize TypeScript compiler program |
| FR-017b-02 | System MUST parse TypeScript (.ts) files |
| FR-017b-03 | System MUST parse JavaScript (.js) files |
| FR-017b-04 | System MUST parse JSX (.jsx) and TSX (.tsx) files |
| FR-017b-05 | System MUST expose source file AST for each parsed file |
| FR-017b-06 | System MUST handle parse errors gracefully and report diagnostics |

### Symbol Discovery (FR-017b-07 to FR-017b-19)

| ID | Requirement |
|----|-------------|
| FR-017b-07 | System MUST extract class declarations |
| FR-017b-08 | System MUST extract interface declarations (TypeScript only) |
| FR-017b-09 | System MUST extract type alias declarations (TypeScript only) |
| FR-017b-10 | System MUST extract enum declarations (TypeScript only) |
| FR-017b-11 | System MUST extract function declarations |
| FR-017b-12 | System MUST extract named arrow functions |
| FR-017b-13 | System MUST extract method declarations |
| FR-017b-14 | System MUST extract property declarations |
| FR-017b-15 | System MUST extract variable/const declarations |
| FR-017b-16 | System MUST extract let declarations |
| FR-017b-17 | System MUST extract module declarations |
| FR-017b-18 | System MUST extract namespace declarations |
| FR-017b-19 | System MUST extract export declarations |

### Symbol Metadata (FR-017b-20 to FR-017b-29)

| ID | Requirement |
|----|-------------|
| FR-017b-20 | Extractor MUST capture symbol name |
| FR-017b-21 | Extractor MUST capture fully qualified name (module path + name) |
| FR-017b-22 | Extractor MUST capture symbol kind (class, function, variable, etc.) |
| FR-017b-23 | Extractor MUST capture export status (exported, default, none) |
| FR-017b-24 | Extractor MUST capture default export status |
| FR-017b-25 | Extractor MUST capture const modifier |
| FR-017b-26 | Extractor MUST capture readonly modifier |
| FR-017b-27 | Extractor MUST capture abstract modifier |
| FR-017b-28 | Extractor MUST capture async modifier |
| FR-017b-29 | Extractor MUST capture generic type parameters |

### Location Extraction (FR-017b-30 to FR-017b-35)

| ID | Requirement |
|----|-------------|
| FR-017b-30 | System MUST extract file path relative to project root |
| FR-017b-31 | System MUST extract 1-based start line number |
| FR-017b-32 | System MUST extract 1-based start column number |
| FR-017b-33 | System MUST extract 1-based end line number |
| FR-017b-34 | System MUST extract 1-based end column number |
| FR-017b-35 | System MUST handle multi-line spans correctly |

### Signature Extraction (FR-017b-36 to FR-017b-41)

| ID | Requirement |
|----|-------------|
| FR-017b-36 | Extractor MUST capture function parameters |
| FR-017b-37 | Extractor MUST capture parameter types (when available) |
| FR-017b-38 | Extractor MUST capture parameter default values |
| FR-017b-39 | Extractor MUST capture function return type |
| FR-017b-40 | Extractor MUST capture property type annotations |
| FR-017b-41 | Extractor MUST format human-readable signature strings |

### Documentation Extraction (FR-017b-42 to FR-017b-47)

| ID | Requirement |
|----|-------------|
| FR-017b-42 | Extractor MUST extract JSDoc comments attached to declarations |
| FR-017b-43 | Extractor MUST extract @param tags with name and description |
| FR-017b-44 | Extractor MUST extract @returns/@return tag |
| FR-017b-45 | Extractor MUST extract @description tag content |
| FR-017b-46 | Extractor MUST extract @example tag content |
| FR-017b-47 | Extractor MUST extract @deprecated tag presence and reason |

### Module Analysis (FR-017b-48 to FR-017b-52)

| ID | Requirement |
|----|-------------|
| FR-017b-48 | System MUST track import statements and their sources |
| FR-017b-49 | System MUST track export statements and exported symbols |
| FR-017b-50 | System MUST identify default exports |
| FR-017b-51 | System MUST identify named exports |
| FR-017b-52 | System MUST resolve relative module paths |

### Node.js Bridge (FR-017b-53 to FR-017b-57)

| ID | Requirement |
|----|-------------|
| FR-017b-53 | System MUST spawn Node.js process with bundled extractor script |
| FR-017b-54 | System MUST use JSON message protocol over stdin/stdout |
| FR-017b-55 | System MUST stream large results to avoid memory issues |
| FR-017b-56 | System MUST handle process errors and restart automatically |
| FR-017b-57 | System MUST support graceful shutdown with pending request drain |

### Extractor Interface (FR-017b-58 to FR-017b-64)

| ID | Requirement |
|----|-------------|
| FR-017b-58 | TypeScriptSymbolExtractor MUST implement ISymbolExtractor interface |
| FR-017b-59 | Extractor MUST register for .ts file extension |
| FR-017b-60 | Extractor MUST register for .js file extension |
| FR-017b-61 | Extractor MUST register for .tsx file extension |
| FR-017b-62 | Extractor MUST register for .jsx file extension |
| FR-017b-63 | ExtractAsync MUST return ExtractionResult with all discovered symbols |
| FR-017b-64 | Extractor MUST report extraction errors in result object |

---

## Non-Functional Requirements

### Performance (NFR-017b-01 to NFR-017b-06)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017b-01 | Performance | File parsing MUST complete in < 100ms per file (median) |
| NFR-017b-02 | Performance | Symbol extraction MUST complete in < 150ms per file (median) |
| NFR-017b-03 | Performance | Node.js bridge startup MUST complete in < 500ms |
| NFR-017b-04 | Performance | System MUST handle files up to 500KB without degradation |
| NFR-017b-05 | Performance | JSON message serialization MUST not exceed 10% of extraction time |
| NFR-017b-06 | Performance | Batch extraction of 100 files MUST complete within 15 seconds |

### Reliability (NFR-017b-07 to NFR-017b-12)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017b-07 | Reliability | System MUST handle malformed TypeScript/JavaScript without crashing |
| NFR-017b-08 | Reliability | Partial results MUST be returned when parse errors occur |
| NFR-017b-09 | Reliability | Node.js process isolation MUST prevent .NET host crashes |
| NFR-017b-10 | Reliability | Bridge MUST automatically recover from Node.js process failures |
| NFR-017b-11 | Reliability | Pending requests MUST be retried on bridge restart |
| NFR-017b-12 | Reliability | File encoding errors MUST be logged and skipped gracefully |

### Security (NFR-017b-13 to NFR-017b-16)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017b-13 | Security | Source file content MUST NOT be logged at INFO level |
| NFR-017b-14 | Security | File paths MUST be validated before Node.js process access |
| NFR-017b-15 | Security | No code execution (eval, require of unknown modules) MUST occur |
| NFR-017b-16 | Security | Node.js process MUST run with restricted permissions |

### Maintainability (NFR-017b-17 to NFR-017b-20)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017b-17 | Maintainability | TypeScript extractor MUST be bundled (no npm install at runtime) |
| NFR-017b-18 | Maintainability | Node.js version requirements MUST be documented |
| NFR-017b-19 | Maintainability | All public APIs MUST have XML documentation |
| NFR-017b-20 | Maintainability | Message protocol MUST be versioned for compatibility |

### Observability (NFR-017b-21 to NFR-017b-24)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017b-21 | Observability | Bridge startup/shutdown events MUST be logged |
| NFR-017b-22 | Observability | Extraction duration MUST be logged per file |
| NFR-017b-23 | Observability | Parse errors MUST be logged with file path and position |
| NFR-017b-24 | Observability | Node.js process restarts MUST be logged with reason |

---

## User Manual Documentation

### Overview

TS/JS symbol extraction uses the TypeScript Compiler API to parse TypeScript and JavaScript files. It extracts classes, functions, interfaces, and other symbols.

### Prerequisites

Node.js must be installed and available in PATH. TypeScript is bundled with the agent.

```bash
# Verify Node.js
node --version
# Should output: v18.0.0 or higher
```

### Configuration

```yaml
# .agent/config.yml
symbol_index:
  typescript:
    # Enable TS/JS extraction
    enabled: true
    
    # Include JavaScript files
    include_javascript: true
    
    # Include JSX/TSX files
    include_jsx: true
    
    # Extract JSDoc comments
    extract_jsdoc: true
    
    # File patterns to exclude
    exclude_patterns:
      - "**/node_modules/**"
      - "**/dist/**"
      - "**/*.d.ts"
      - "**/*.min.js"
```

### Symbol Output

```json
{
  "id": "f1e2d3c4-b5a6-7890-1234-567890abcdef",
  "name": "UserService",
  "fullyQualifiedName": "src/services/UserService.UserService",
  "kind": "Class",
  "visibility": "Exported",
  "location": {
    "filePath": "src/services/UserService.ts",
    "startLine": 5,
    "startColumn": 1,
    "endLine": 45,
    "endColumn": 2
  },
  "modifiers": ["export", "class"],
  "documentation": {
    "description": "Handles user-related operations",
    "example": "const service = new UserService();"
  }
}
```

### Supported Constructs

| Construct | TypeScript | JavaScript | Notes |
|-----------|------------|------------|-------|
| Class | ✅ | ✅ | Full metadata |
| Interface | ✅ | ❌ | TS only |
| Type Alias | ✅ | ❌ | TS only |
| Function | ✅ | ✅ | With signature |
| Arrow Function | ✅ | ✅ | Named only |
| Method | ✅ | ✅ | With signature |
| Property | ✅ | ✅ | With type |
| Variable | ✅ | ✅ | Named exports |
| Const | ✅ | ✅ | Named exports |
| Enum | ✅ | ❌ | TS only |
| Module | ✅ | ❌ | TS only |
| Export | ✅ | ✅ | Tracked |
| Decorator | ✅ | ❌ | TS only |

### CLI Commands

```bash
# Extract symbols from TypeScript file
acode symbols extract src/services/UserService.ts

# Extract from JavaScript file
acode symbols extract src/utils/helpers.js

# Extract with stats
acode symbols extract src/ --stats
```

### Troubleshooting

#### Node.js Not Found

**Problem:** Bridge fails to start

**Solutions:**
1. Verify Node.js installed: `node --version`
2. Add Node.js to PATH
3. Set explicit path in config

#### Missing Symbols

**Problem:** Some symbols not extracted

**Solutions:**
1. Check file extensions included
2. Check exclude patterns
3. Verify syntax is valid
4. Check for parse errors

#### Slow Extraction

**Problem:** Extraction takes too long

**Solutions:**
1. Exclude node_modules
2. Exclude dist/build folders
3. Exclude .d.ts files
4. Exclude minified files

#### JSDoc Not Extracted

**Problem:** Documentation missing

**Solutions:**
1. Enable extract_jsdoc
2. Verify JSDoc format is standard
3. Check for syntax errors in comments

---

## Acceptance Criteria

### TypeScript

- [ ] AC-001: Compiler initialized
- [ ] AC-002: TS files parsed
- [ ] AC-003: JS files parsed
- [ ] AC-004: JSX/TSX parsed

### Symbols

- [ ] AC-005: Classes extracted
- [ ] AC-006: Interfaces extracted
- [ ] AC-007: Functions extracted
- [ ] AC-008: Variables extracted
- [ ] AC-009: Exports tracked

### Metadata

- [ ] AC-010: Names correct
- [ ] AC-011: Locations correct
- [ ] AC-012: Signatures correct
- [ ] AC-013: JSDoc extracted

### Bridge

- [ ] AC-014: Process starts
- [ ] AC-015: Messages exchanged
- [ ] AC-016: Errors handled
- [ ] AC-017: Graceful shutdown

### Interface

- [ ] AC-018: Implements ISymbolExtractor
- [ ] AC-019: Registered correctly

---

## Best Practices

### Tree-sitter Integration

1. **Robust parsing** - Tree-sitter handles syntax errors gracefully
2. **Grammar maintenance** - Keep JavaScript/TypeScript grammars updated
3. **Efficient queries** - Use tree-sitter queries for pattern matching
4. **Language detection** - Distinguish JS, TS, JSX, TSX correctly

### JavaScript/TypeScript Specifics

5. **Handle module systems** - CommonJS, ESM, AMD imports
6. **Extract JSDoc comments** - Use for type inference in JS
7. **Track exports** - Named, default, and re-exports
8. **Handle dynamic patterns** - Best-effort for computed properties

### Performance

9. **Process pools** - Worker processes for parallel parsing
10. **Skip node_modules** - Ignore by default, opt-in for specific packages
11. **Graceful shutdown** - Clean up worker processes properly
12. **Incremental updates** - Only re-parse changed files

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Symbols/TypeScript/
├── TypeScriptExtractorTests.cs
│   ├── Should_Extract_Class()
│   ├── Should_Extract_Abstract_Class()
│   ├── Should_Extract_Class_With_Generics()
│   ├── Should_Extract_Interface()
│   ├── Should_Extract_Type_Alias()
│   ├── Should_Extract_Enum()
│   ├── Should_Extract_Function()
│   ├── Should_Extract_Arrow_Function()
│   ├── Should_Extract_Async_Function()
│   ├── Should_Extract_Generator_Function()
│   ├── Should_Extract_Class_Method()
│   ├── Should_Extract_Static_Method()
│   ├── Should_Extract_Property()
│   ├── Should_Extract_Readonly_Property()
│   ├── Should_Extract_Constructor()
│   ├── Should_Extract_Getter_Setter()
│   ├── Should_Extract_Named_Export()
│   ├── Should_Extract_Default_Export()
│   ├── Should_Extract_Re_Export()
│   ├── Should_Extract_Variable_Const()
│   ├── Should_Extract_Variable_Let()
│   ├── Should_Extract_Decorator()
│   ├── Should_Extract_Visibility()
│   ├── Should_Extract_Return_Type()
│   ├── Should_Extract_Parameter_Types()
│   ├── Should_Extract_Location()
│   ├── Should_Handle_Malformed_File()
│   └── Should_Handle_Partial_Results()
│
├── JavaScriptExtractorTests.cs
│   ├── Should_Extract_Function()
│   ├── Should_Extract_Arrow_Function()
│   ├── Should_Extract_Class()
│   ├── Should_Extract_Object_Method()
│   ├── Should_Extract_CommonJS_Export()
│   ├── Should_Extract_ES_Module_Export()
│   ├── Should_Handle_No_Types()
│   └── Should_Infer_From_JSDoc()
│
├── JSDocExtractorTests.cs
│   ├── Should_Extract_Description()
│   ├── Should_Extract_Params()
│   ├── Should_Extract_Returns()
│   ├── Should_Extract_Typedef()
│   ├── Should_Extract_Type()
│   ├── Should_Extract_Throws()
│   ├── Should_Extract_Example()
│   ├── Should_Handle_Missing_Docs()
│   ├── Should_Handle_Malformed_JSDoc()
│   └── Should_Handle_Multi_Line()
│
├── NodeBridgeTests.cs
│   ├── Should_Start_Process()
│   ├── Should_Find_Node_Executable()
│   ├── Should_Exchange_JSON_Messages()
│   ├── Should_Handle_Large_Messages()
│   ├── Should_Handle_Process_Crash()
│   ├── Should_Handle_Timeout()
│   ├── Should_Restart_On_Failure()
│   ├── Should_Shutdown_Gracefully()
│   └── Should_Handle_Concurrent_Requests()
│
├── TSConfigParserTests.cs
│   ├── Should_Parse_TsConfig()
│   ├── Should_Resolve_Extends()
│   ├── Should_Handle_Missing_TsConfig()
│   └── Should_Apply_Compiler_Options()
│
└── TypeScriptParserTests.cs
    ├── Should_Parse_TS_File()
    ├── Should_Parse_JS_File()
    ├── Should_Parse_TSX_File()
    ├── Should_Parse_JSX_File()
    ├── Should_Handle_Syntax_Errors()
    └── Should_Handle_Different_Targets()
```

### Integration Tests

```
Tests/Integration/Symbols/TypeScript/
├── TypeScriptExtractorIntegrationTests.cs
│   ├── Should_Extract_Real_TS_File()
│   ├── Should_Extract_Real_JS_File()
│   ├── Should_Extract_Real_TSX_File()
│   ├── Should_Extract_React_Components()
│   ├── Should_Handle_Large_File()
│   ├── Should_Handle_Many_Files()
│   ├── Should_Handle_Node_Modules()
│   └── Should_Handle_Monorepo()
│
└── NodeBridgeIntegrationTests.cs
    ├── Should_Start_Bridge_Process()
    ├── Should_Handle_Long_Running()
    └── Should_Survive_Process_Restart()
```

### E2E Tests

```
Tests/E2E/Symbols/TypeScript/
├── TypeScriptSymbolE2ETests.cs
│   ├── Should_Index_TypeScript_Project()
│   ├── Should_Index_JavaScript_Project()
│   ├── Should_Search_TS_Symbols()
│   └── Should_Extract_Via_CLI()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Parse single file | 60ms | 100ms |
| Extract single file | 100ms | 150ms |
| Bridge startup | 300ms | 500ms |
| Extract 100 files | 10s | 15s |

---

## User Verification Steps

### Scenario 1: Extract TypeScript Class

1. Create .ts file with class
2. Run extraction
3. Verify: Class symbol returned

### Scenario 2: Extract JavaScript Function

1. Create .js file with function
2. Run extraction
3. Verify: Function symbol returned

### Scenario 3: Extract JSDoc

1. Create file with JSDoc comments
2. Run extraction
3. Verify: Documentation extracted

### Scenario 4: Handle Errors

1. Create file with syntax error
2. Run extraction
3. Verify: Partial results, error logged

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Infrastructure/
├── Symbols/
│   └── TypeScript/
│       ├── TypeScriptSymbolExtractor.cs
│       ├── NodeBridge.cs
│       └── MessageProtocol.cs
│
tools/ts-extractor/
├── package.json
├── tsconfig.json
├── src/
│   ├── index.ts
│   ├── extractor.ts
│   ├── visitor.ts
│   └── jsdoc.ts
```

### TypeScriptSymbolExtractor Class

```csharp
namespace AgenticCoder.Infrastructure.Symbols.TypeScript;

public class TypeScriptSymbolExtractor : ISymbolExtractor
{
    public string Language => "typescript";
    public string[] FileExtensions => new[] { ".ts", ".tsx", ".js", ".jsx" };
    
    private readonly NodeBridge _bridge;
    
    public async Task<ExtractionResult> ExtractAsync(
        string filePath,
        ExtractionOptions options,
        CancellationToken ct = default)
    {
        var request = new ExtractionRequest
        {
            FilePath = filePath,
            IncludeJSDoc = options.IncludeDocumentation
        };
        
        var response = await _bridge.SendAsync<ExtractionResponse>(request, ct);
        return MapToResult(response);
    }
}
```

### NodeBridge Class

```csharp
public class NodeBridge : IDisposable
{
    private Process? _process;
    
    public async Task StartAsync(CancellationToken ct = default)
    {
        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "node",
                Arguments = "ts-extractor/dist/index.js",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            }
        };
        _process.Start();
    }
    
    public async Task<T> SendAsync<T>(object request, CancellationToken ct)
    {
        // JSON messaging over stdin/stdout
    }
}
```

### TypeScript Extractor (Node.js)

```typescript
// tools/ts-extractor/src/extractor.ts
import * as ts from 'typescript';

export function extractSymbols(filePath: string): ExtractedSymbol[] {
    const sourceFile = ts.createSourceFile(
        filePath,
        fs.readFileSync(filePath, 'utf8'),
        ts.ScriptTarget.Latest,
        true
    );
    
    const symbols: ExtractedSymbol[] = [];
    visit(sourceFile, symbols);
    return symbols;
}

function visit(node: ts.Node, symbols: ExtractedSymbol[]) {
    if (ts.isClassDeclaration(node)) {
        symbols.push(extractClass(node));
    }
    // ... other node types
    ts.forEachChild(node, child => visit(child, symbols));
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-TSE-001 | Parse error |
| ACODE-TSE-002 | Bridge error |
| ACODE-TSE-003 | Node.js not found |
| ACODE-TSE-004 | Extraction error |

### Implementation Checklist

1. [ ] Create NodeBridge
2. [ ] Create TypeScript extractor (Node.js)
3. [ ] Create message protocol
4. [ ] Implement ISymbolExtractor
5. [ ] Handle all symbol types
6. [ ] Extract JSDoc
7. [ ] Register extractor
8. [ ] Add unit tests

### Rollout Plan

1. **Phase 1:** Node bridge
2. **Phase 2:** Basic extraction
3. **Phase 3:** All symbol types
4. **Phase 4:** JSDoc extraction
5. **Phase 5:** Integration

---

**End of Task 017.b Specification**