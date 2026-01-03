# Task 017.a: C# Symbol Extraction

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 017 (Symbol Index v2)  

---

## Description

Task 017.a implements C# symbol extraction using Roslyn. Roslyn is the .NET compiler platform. It provides complete syntactic and semantic analysis of C# code.

C# is the primary language for this project. Most agent code is C#. C# symbol extraction is critical for code intelligence. Without it, the agent cannot understand C# codebases.

Roslyn provides two analysis layers. Syntax analysis parses code structure without compilation. Semantic analysis provides type information and bindings. Both are needed for complete extraction.

Syntax analysis is fast. It parses a single file independently. No project context needed. Useful for basic symbol identification.

Semantic analysis is richer. It resolves types, finds overloads, identifies inheritance. Requires compilation context. More expensive but more accurate.

The extractor supports both modes. Quick mode uses syntax only. Full mode uses semantic analysis. Configuration controls the mode.

Symbol extraction handles all C# constructs. Classes, interfaces, structs, enums. Methods, properties, fields, events. Constructors, finalizers, operators. Delegates, lambdas (named only).

The extractor captures symbol metadata. Name and fully qualified name. Location (file, line, column). Visibility (public, private, protected, internal). Signature for methods. Documentation comments.

XML documentation is extracted. Summary, param, returns, example sections. Documentation aids context packing. The agent can explain what code does.

Roslyn handles C# versions automatically. Syntax varies between versions. The extractor adapts to the code being parsed. No version configuration needed.

Error handling is robust. Malformed code is common during editing. The extractor extracts what it can. Parse errors are logged but don't block extraction.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Roslyn | .NET Compiler Platform for C# analysis |
| Syntax Tree | Parsed code structure |
| Semantic Model | Type and binding information |
| Compilation | Complete project context |
| Symbol | Roslyn's ISymbol type |
| Declaration | Symbol definition node |
| Reference | Symbol usage node |
| XML Docs | C# documentation comments |
| Trivia | Whitespace and comments |
| Span | Text position range |
| SyntaxWalker | Visitor pattern for AST |
| SyntaxNode | AST node type |
| SyntaxToken | Leaf AST node |
| Analyzer | Code inspection component |
| Workspace | Roslyn project container |

---

## Out of Scope

The following items are explicitly excluded from Task 017.a:

- **TypeScript/JavaScript** - See Task 017.b
- **Python/Go/Rust** - Future versions
- **Cross-file analysis** - Semantic model per file
- **Refactoring** - Read-only extraction
- **Code generation** - Extraction only
- **Real-time updates** - Batch extraction
- **IDE integration** - CLI tool only

---

## Functional Requirements

### Roslyn Integration

- FR-001: Create Roslyn workspace
- FR-002: Parse single file
- FR-003: Parse project
- FR-004: Get syntax tree
- FR-005: Get semantic model
- FR-006: Handle parse errors

### Symbol Discovery

- FR-007: Extract namespace declarations
- FR-008: Extract class declarations
- FR-009: Extract interface declarations
- FR-010: Extract struct declarations
- FR-011: Extract enum declarations
- FR-012: Extract record declarations
- FR-013: Extract method declarations
- FR-014: Extract property declarations
- FR-015: Extract field declarations
- FR-016: Extract event declarations
- FR-017: Extract constructor declarations
- FR-018: Extract delegate declarations
- FR-019: Extract indexer declarations

### Symbol Metadata

- FR-020: Extract symbol name
- FR-021: Extract fully qualified name
- FR-022: Extract symbol kind
- FR-023: Extract visibility modifier
- FR-024: Extract static modifier
- FR-025: Extract abstract modifier
- FR-026: Extract sealed modifier
- FR-027: Extract virtual modifier
- FR-028: Extract override modifier
- FR-029: Extract async modifier
- FR-030: Extract generic parameters

### Location Extraction

- FR-031: Extract file path
- FR-032: Extract start line
- FR-033: Extract start column
- FR-034: Extract end line
- FR-035: Extract end column
- FR-036: Extract span offset
- FR-037: Extract span length

### Signature Extraction

- FR-038: Extract method parameters
- FR-039: Extract parameter types
- FR-040: Extract parameter names
- FR-041: Extract return type
- FR-042: Extract property type
- FR-043: Extract field type
- FR-044: Format signature string

### Documentation Extraction

- FR-045: Extract XML summary
- FR-046: Extract XML param
- FR-047: Extract XML returns
- FR-048: Extract XML remarks
- FR-049: Extract XML example
- FR-050: Strip XML tags for plain text

### Containment

- FR-051: Track parent symbol
- FR-052: Track containing namespace
- FR-053: Track containing type
- FR-054: Build containment hierarchy

### Extractor Interface

- FR-055: Implement ISymbolExtractor
- FR-056: Register for .cs extension
- FR-057: Return extracted symbols
- FR-058: Report extraction errors
- FR-059: Support cancellation
- FR-060: Support progress reporting

---

## Non-Functional Requirements

### Performance

- NFR-001: Parse < 50ms per file
- NFR-002: Extract < 100ms per file
- NFR-003: Handle files up to 500KB
- NFR-004: Memory < 100MB for 1000 files

### Reliability

- NFR-005: Handle malformed code
- NFR-006: Partial results on error
- NFR-007: No crashes on bad input
- NFR-008: Graceful degradation

### Accuracy

- NFR-009: All declared symbols found
- NFR-010: Correct locations
- NFR-011: Correct visibility
- NFR-012: Valid signatures

---

## User Manual Documentation

### Overview

C# symbol extraction uses Roslyn to parse and analyze C# source files. It extracts classes, methods, properties, and other symbols for the symbol index.

### Configuration

```yaml
# .agent/config.yml
symbol_index:
  csharp:
    # Enable C# extraction
    enabled: true
    
    # Use semantic model (slower but richer)
    use_semantic_model: false
    
    # Extract XML documentation
    extract_docs: true
    
    # Include private members
    include_private: true
    
    # Include generated files
    include_generated: false
    
    # File patterns to exclude
    exclude_patterns:
      - "**/obj/**"
      - "**/bin/**"
      - "**.Designer.cs"
```

### Extraction Modes

**Syntax-only mode (default):**
- Faster, no project context needed
- Basic symbol information
- Types shown as written

**Semantic mode:**
- Slower, requires compilable project
- Full type resolution
- Inheritance information

### Symbol Output

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "name": "GetUserById",
  "fullyQualifiedName": "MyApp.Services.UserService.GetUserById",
  "kind": "Method",
  "visibility": "Public",
  "location": {
    "filePath": "src/Services/UserService.cs",
    "startLine": 45,
    "startColumn": 5,
    "endLine": 52,
    "endColumn": 6
  },
  "signature": "User GetUserById(int id)",
  "modifiers": ["public", "async"],
  "containingSymbol": "UserService",
  "documentation": {
    "summary": "Retrieves a user by their unique identifier.",
    "params": [
      { "name": "id", "description": "The user's unique ID" }
    ],
    "returns": "The user if found, null otherwise"
  }
}
```

### Supported Constructs

| Construct | Extracted | Notes |
|-----------|-----------|-------|
| Namespace | ✅ | Name only |
| Class | ✅ | Full metadata |
| Interface | ✅ | Full metadata |
| Struct | ✅ | Full metadata |
| Record | ✅ | Full metadata |
| Enum | ✅ | With members |
| Method | ✅ | With signature |
| Property | ✅ | With type |
| Field | ✅ | With type |
| Constructor | ✅ | With parameters |
| Event | ✅ | With delegate type |
| Delegate | ✅ | With signature |
| Indexer | ✅ | With parameters |
| Operator | ✅ | With signature |
| Lambda | ❌ | Anonymous |
| Local function | ❌ | Future |

### CLI Commands

```bash
# Extract symbols from a C# file
acode symbols extract src/Services/UserService.cs

# Extract with semantic analysis
acode symbols extract src/ --semantic

# Show extraction stats
acode symbols extract src/ --stats
```

### Troubleshooting

#### Missing Symbols

**Problem:** Some symbols not extracted

**Solutions:**
1. Check file has .cs extension
2. Check file is valid C# syntax
3. Check include_private setting
4. Verify file not in exclude patterns

#### Slow Extraction

**Problem:** Extraction takes too long

**Solutions:**
1. Disable semantic model
2. Exclude generated files
3. Exclude test files
4. Reduce file size limit

#### Parse Errors

**Problem:** Roslyn reports errors

**Solutions:**
1. Ensure code is valid C#
2. Check C# language version
3. Symbol extraction continues anyway

---

## Acceptance Criteria

### Roslyn

- [ ] AC-001: Workspace created
- [ ] AC-002: Files parsed
- [ ] AC-003: Trees generated

### Symbols

- [ ] AC-004: Classes extracted
- [ ] AC-005: Interfaces extracted
- [ ] AC-006: Methods extracted
- [ ] AC-007: Properties extracted
- [ ] AC-008: Fields extracted

### Metadata

- [ ] AC-009: Names correct
- [ ] AC-010: Locations correct
- [ ] AC-011: Visibility correct
- [ ] AC-012: Signatures correct

### Documentation

- [ ] AC-013: XML docs extracted
- [ ] AC-014: Summary parsed
- [ ] AC-015: Params parsed

### Interface

- [ ] AC-016: Implements ISymbolExtractor
- [ ] AC-017: Registered correctly
- [ ] AC-018: Errors handled

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Symbols/CSharp/
├── CSharpExtractorTests.cs
│   ├── Should_Extract_Class()
│   ├── Should_Extract_Interface()
│   ├── Should_Extract_Method()
│   ├── Should_Extract_Property()
│   ├── Should_Extract_Visibility()
│   ├── Should_Extract_Signature()
│   └── Should_Handle_Malformed()
│
├── RoslynParserTests.cs
│   ├── Should_Parse_File()
│   ├── Should_Get_Syntax_Tree()
│   └── Should_Handle_Errors()
│
└── XmlDocExtractorTests.cs
    ├── Should_Extract_Summary()
    ├── Should_Extract_Params()
    └── Should_Strip_Tags()
```

### Integration Tests

```
Tests/Integration/Symbols/CSharp/
├── CSharpExtractorIntegrationTests.cs
│   ├── Should_Extract_Real_File()
│   └── Should_Extract_Project()
```

### E2E Tests

```
Tests/E2E/Symbols/CSharp/
├── CSharpSymbolE2ETests.cs
│   └── Should_Index_Csharp_Project()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Parse single file | 30ms | 50ms |
| Extract single file | 60ms | 100ms |
| Extract 100 files | 5s | 10s |

---

## User Verification Steps

### Scenario 1: Extract Class

1. Create file with class
2. Run extraction
3. Verify: Class symbol returned

### Scenario 2: Extract Method

1. Create file with method
2. Run extraction
3. Verify: Method symbol with signature

### Scenario 3: XML Docs

1. Create file with XML docs
2. Run extraction
3. Verify: Docs extracted

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
│   └── CSharp/
│       ├── CSharpSymbolExtractor.cs
│       ├── RoslynParser.cs
│       ├── SymbolVisitor.cs
│       ├── XmlDocExtractor.cs
│       └── SignatureFormatter.cs
```

### CSharpSymbolExtractor Class

```csharp
namespace AgenticCoder.Infrastructure.Symbols.CSharp;

public class CSharpSymbolExtractor : ISymbolExtractor
{
    public string Language => "csharp";
    public string[] FileExtensions => new[] { ".cs" };
    
    public async Task<ExtractionResult> ExtractAsync(
        string filePath, 
        ExtractionOptions options,
        CancellationToken ct = default)
    {
        // Implementation
    }
}
```

### SymbolVisitor Class

```csharp
public class SymbolVisitor : CSharpSyntaxWalker
{
    public List<ExtractedSymbol> Symbols { get; }
    
    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        // Extract class symbol
        base.VisitClassDeclaration(node);
    }
    
    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // Extract method symbol
        base.VisitMethodDeclaration(node);
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-CSE-001 | Parse error |
| ACODE-CSE-002 | File not found |
| ACODE-CSE-003 | Invalid C# |
| ACODE-CSE-004 | Extraction error |

### Implementation Checklist

1. [ ] Create RoslynParser
2. [ ] Create SymbolVisitor
3. [ ] Extract all symbol types
4. [ ] Extract metadata
5. [ ] Extract XML docs
6. [ ] Implement ISymbolExtractor
7. [ ] Register extractor
8. [ ] Add unit tests

### Rollout Plan

1. **Phase 1:** Basic parsing
2. **Phase 2:** Symbol visitor
3. **Phase 3:** Metadata extraction
4. **Phase 4:** Documentation extraction
5. **Phase 5:** Integration

---

**End of Task 017.a Specification**