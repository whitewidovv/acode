# Task 017.b: TS/JS Symbol Extraction

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 017 (Symbol Index v2)  

---

## Description

Task 017.b implements TypeScript and JavaScript symbol extraction. TypeScript is a common language in modern projects. Many frontend and Node.js projects use it. JavaScript remains ubiquitous.

TypeScript Compiler API provides parsing capabilities. Similar to Roslyn for C#, it offers complete AST access. The extractor uses this API for accurate symbol extraction.

TypeScript and JavaScript share syntax. The same extractor handles both. TypeScript adds type annotations. JavaScript symbols lack type information but structure remains similar.

The extractor runs in Node.js. A lightweight Node.js process handles parsing. The .NET agent communicates via stdout/stdin. JSON messages exchange symbol data.

Symbol extraction handles all TypeScript constructs. Classes, interfaces, type aliases. Functions, arrow functions, methods. Variables, constants, parameters. Modules, namespaces, exports.

JSDoc comments are extracted. Many JavaScript projects use JSDoc. TypeScript supports JSDoc for typing. Documentation aids context understanding.

ES modules and CommonJS are supported. Import/export statements are analyzed. Module structure is captured. Dependency information extracted.

The extractor handles JSX/TSX. React components are common. JSX elements have symbol information. Component props are extracted.

Decorators are extracted. Angular and NestJS use decorators. Decorator metadata is captured. Decorator arguments noted.

Error handling is critical. JavaScript projects often have syntax errors. Partial results are returned. The agent continues with what's available.

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

### TypeScript Integration

- FR-001: Initialize TypeScript compiler
- FR-002: Parse TypeScript files
- FR-003: Parse JavaScript files
- FR-004: Handle JSX/TSX
- FR-005: Get source file AST
- FR-006: Handle parse errors

### Symbol Discovery

- FR-007: Extract class declarations
- FR-008: Extract interface declarations
- FR-009: Extract type alias declarations
- FR-010: Extract enum declarations
- FR-011: Extract function declarations
- FR-012: Extract arrow functions
- FR-013: Extract method declarations
- FR-014: Extract property declarations
- FR-015: Extract variable declarations
- FR-016: Extract const declarations
- FR-017: Extract module declarations
- FR-018: Extract namespace declarations
- FR-019: Extract export declarations

### Symbol Metadata

- FR-020: Extract symbol name
- FR-021: Extract fully qualified name
- FR-022: Extract symbol kind
- FR-023: Extract export status
- FR-024: Extract default export status
- FR-025: Extract const modifier
- FR-026: Extract readonly modifier
- FR-027: Extract abstract modifier
- FR-028: Extract async modifier
- FR-029: Extract generic parameters

### Location Extraction

- FR-030: Extract file path
- FR-031: Extract start line
- FR-032: Extract start column
- FR-033: Extract end line
- FR-034: Extract end column
- FR-035: Handle multi-line spans

### Signature Extraction

- FR-036: Extract function parameters
- FR-037: Extract parameter types
- FR-038: Extract parameter defaults
- FR-039: Extract return type
- FR-040: Extract property type
- FR-041: Format signature string

### Documentation Extraction

- FR-042: Extract JSDoc comments
- FR-043: Extract @param tags
- FR-044: Extract @returns tag
- FR-045: Extract @description
- FR-046: Extract @example
- FR-047: Extract @deprecated

### Module Analysis

- FR-048: Track imports
- FR-049: Track exports
- FR-050: Identify default exports
- FR-051: Identify named exports
- FR-052: Resolve module paths

### Node.js Bridge

- FR-053: Spawn Node.js process
- FR-054: JSON message protocol
- FR-055: Stream large results
- FR-056: Handle process errors
- FR-057: Graceful shutdown

### Extractor Interface

- FR-058: Implement ISymbolExtractor
- FR-059: Register for .ts extension
- FR-060: Register for .js extension
- FR-061: Register for .tsx extension
- FR-062: Register for .jsx extension
- FR-063: Return extracted symbols
- FR-064: Report extraction errors

---

## Non-Functional Requirements

### Performance

- NFR-001: Parse < 100ms per file
- NFR-002: Extract < 150ms per file
- NFR-003: Bridge startup < 500ms
- NFR-004: Handle files up to 500KB

### Reliability

- NFR-005: Handle malformed code
- NFR-006: Partial results on error
- NFR-007: Process isolation
- NFR-008: Bridge recovery

### Accuracy

- NFR-009: All declarations found
- NFR-010: Correct locations
- NFR-011: Correct signatures
- NFR-012: Correct exports

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

## Testing Requirements

### Unit Tests

```
Tests/Unit/Symbols/TypeScript/
├── TypeScriptExtractorTests.cs
│   ├── Should_Extract_Class()
│   ├── Should_Extract_Interface()
│   ├── Should_Extract_Function()
│   ├── Should_Extract_Variable()
│   ├── Should_Extract_Export()
│   └── Should_Handle_Malformed()
│
├── JSDocExtractorTests.cs
│   ├── Should_Extract_Description()
│   ├── Should_Extract_Params()
│   └── Should_Extract_Returns()
│
└── NodeBridgeTests.cs
    ├── Should_Start_Process()
    ├── Should_Exchange_Messages()
    └── Should_Handle_Errors()
```

### Integration Tests

```
Tests/Integration/Symbols/TypeScript/
├── TypeScriptExtractorIntegrationTests.cs
│   ├── Should_Extract_Real_TS_File()
│   └── Should_Extract_Real_JS_File()
```

### E2E Tests

```
Tests/E2E/Symbols/TypeScript/
├── TypeScriptSymbolE2ETests.cs
│   └── Should_Index_TypeScript_Project()
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