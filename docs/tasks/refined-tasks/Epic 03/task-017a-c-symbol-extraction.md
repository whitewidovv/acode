# Task 017.a: C# Symbol Extraction

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 017 (Symbol Index v2)  

---

## Description

### Business Value

C# symbol extraction is the foundational capability that enables the agent to understand .NET codebases. Without the ability to parse and extract symbols from C# source files, the agent cannot navigate class hierarchies, find method signatures, or understand the structure of the code it is asked to modify. This task implements production-grade C# parsing using Roslyn, the official .NET compiler platform.

The extracted symbols feed directly into the Symbol Index (Task 017), which powers code intelligence features across the entire agent. When a user asks the agent to modify a method, the symbol extractor identifies that method's location, signature, visibility, and documentation. When the agent needs to understand class relationships, the extractor provides inheritance and interface implementation data. The quality of symbol extraction directly impacts the agent's ability to make correct, contextually-aware code changes.

Roslyn provides two analysis layers that this task leverages. Syntax analysis offers fast, standalone parsing without project context—ideal for quick symbol identification during interactive sessions. Semantic analysis provides richer type resolution, overload identification, and inheritance information at the cost of requiring compilation context. The extractor supports both modes, allowing the system to trade accuracy for speed based on operational requirements.

### Scope

This task delivers the following components:

1. **RoslynParser** - Wrapper around Roslyn APIs for syntax tree and semantic model access with proper workspace management
2. **SymbolVisitor** - CSharpSyntaxWalker implementation that visits all declaration nodes and extracts symbol information
3. **XmlDocExtractor** - Parser for C# XML documentation comments (summary, param, returns, remarks, example sections)
4. **SignatureFormatter** - Generates human-readable method and property signatures from Roslyn symbols
5. **CSharpSymbolExtractor** - ISymbolExtractor implementation that orchestrates parsing and extraction for .cs files

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Symbol Index (Task 017) | Consumer | Receives extracted symbols for indexing and querying |
| Dependency Mapper (Task 017.c) | Consumer | Uses extracted symbols to build dependency relationships |
| Context Packer | Consumer | Retrieves symbol metadata when packing code context for prompts |
| Configuration System | Configuration | Reads extraction settings (semantic mode, include private, exclude patterns) |
| File System Abstraction | Dependency | Reads C# source files for parsing |
| Logging Infrastructure | Dependency | Reports extraction progress, errors, and metrics |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Roslyn parse failure | File symbols not extracted | Graceful degradation—log error, continue with other files, return partial results |
| Semantic model unavailable | Reduced type resolution accuracy | Fall back to syntax-only mode automatically |
| Out of memory on large files | Process crash or hang | File size limits (500KB default), streaming for very large files |
| Invalid C# syntax | Partial AST available | Extract from valid portions of the syntax tree |
| Missing project references | Unresolved type names | Use syntax analysis for names, mark types as unresolved |
| Concurrent file access | Read failures | Retry with exponential backoff, skip if unavailable |

### Assumptions

1. Roslyn is available as a NuGet dependency (Microsoft.CodeAnalysis.CSharp)
2. Source files use UTF-8 encoding (with BOM detection for legacy files)
3. C# language version is auto-detected from the source file syntax
4. Semantic analysis requires a compilable project context (csproj or solution)
5. Private members are extracted by default but can be filtered via configuration
6. Generated files (*.Designer.cs, obj/, bin/) are excluded by default
7. XML documentation follows standard C# documentation comment format
8. Symbol IDs are generated as deterministic hashes for consistent indexing

### Security Considerations

1. **No Code Execution** - Roslyn parsing MUST NOT compile or execute any code from source files
2. **Path Validation** - All file paths MUST be validated to prevent directory traversal attacks
3. **Content Limits** - Maximum file size and symbol count limits prevent DoS via crafted files
4. **Sandboxed Parsing** - Roslyn workspace is read-only; no modifications to source files
5. **Memory Bounds** - Semantic model caching is bounded to prevent memory exhaustion

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

### Roslyn Integration (FR-017a-01 to FR-017a-06)

| ID | Requirement |
|----|-------------|
| FR-017a-01 | System MUST create Roslyn AdhocWorkspace for file parsing |
| FR-017a-02 | System MUST parse single C# files independently |
| FR-017a-03 | System MUST support parsing files within project context |
| FR-017a-04 | System MUST expose syntax tree for each parsed file |
| FR-017a-05 | System MUST provide semantic model when project context is available |
| FR-017a-06 | System MUST handle parse errors gracefully and continue extraction |

### Symbol Discovery (FR-017a-07 to FR-017a-19)

| ID | Requirement |
|----|-------------|
| FR-017a-07 | System MUST extract namespace declarations |
| FR-017a-08 | System MUST extract class declarations (including nested classes) |
| FR-017a-09 | System MUST extract interface declarations |
| FR-017a-10 | System MUST extract struct declarations |
| FR-017a-11 | System MUST extract enum declarations with members |
| FR-017a-12 | System MUST extract record declarations |
| FR-017a-13 | System MUST extract method declarations with signatures |
| FR-017a-14 | System MUST extract property declarations with types |
| FR-017a-15 | System MUST extract field declarations with types |
| FR-017a-16 | System MUST extract event declarations |
| FR-017a-17 | System MUST extract constructor declarations |
| FR-017a-18 | System MUST extract delegate declarations |
| FR-017a-19 | System MUST extract indexer declarations |

### Symbol Metadata (FR-017a-20 to FR-017a-30)

| ID | Requirement |
|----|-------------|
| FR-017a-20 | Extractor MUST capture symbol name |
| FR-017a-21 | Extractor MUST capture fully qualified name |
| FR-017a-22 | Extractor MUST capture symbol kind (class, method, property, etc.) |
| FR-017a-23 | Extractor MUST capture visibility modifier (public, private, protected, internal) |
| FR-017a-24 | Extractor MUST capture static modifier |
| FR-017a-25 | Extractor MUST capture abstract modifier |
| FR-017a-26 | Extractor MUST capture sealed modifier |
| FR-017a-27 | Extractor MUST capture virtual modifier |
| FR-017a-28 | Extractor MUST capture override modifier |
| FR-017a-29 | Extractor MUST capture async modifier |
| FR-017a-30 | Extractor MUST capture generic type parameters and constraints |

### Location Extraction (FR-017a-31 to FR-017a-37)

| ID | Requirement |
|----|-------------|
| FR-017a-31 | System MUST extract absolute file path |
| FR-017a-32 | System MUST extract 1-based start line number |
| FR-017a-33 | System MUST extract 1-based start column number |
| FR-017a-34 | System MUST extract 1-based end line number |
| FR-017a-35 | System MUST extract 1-based end column number |
| FR-017a-36 | System MUST extract character span offset |
| FR-017a-37 | System MUST extract character span length |

### Signature Extraction (FR-017a-38 to FR-017a-44)

| ID | Requirement |
|----|-------------|
| FR-017a-38 | Extractor MUST capture method parameters |
| FR-017a-39 | Extractor MUST capture parameter types (with nullability) |
| FR-017a-40 | Extractor MUST capture parameter names |
| FR-017a-41 | Extractor MUST capture method return type |
| FR-017a-42 | Extractor MUST capture property type |
| FR-017a-43 | Extractor MUST capture field type |
| FR-017a-44 | Extractor MUST format human-readable signature strings |

### Documentation Extraction (FR-017a-45 to FR-017a-50)

| ID | Requirement |
|----|-------------|
| FR-017a-45 | Extractor MUST extract XML summary documentation |
| FR-017a-46 | Extractor MUST extract XML param documentation |
| FR-017a-47 | Extractor MUST extract XML returns documentation |
| FR-017a-48 | Extractor MUST extract XML remarks documentation |
| FR-017a-49 | Extractor MUST extract XML example documentation |
| FR-017a-50 | Extractor MUST provide plain text option (XML tags stripped) |

### Containment Hierarchy (FR-017a-51 to FR-017a-54)

| ID | Requirement |
|----|-------------|
| FR-017a-51 | Extractor MUST track parent symbol for nested declarations |
| FR-017a-52 | Extractor MUST track containing namespace |
| FR-017a-53 | Extractor MUST track containing type for members |
| FR-017a-54 | Extractor MUST build complete containment hierarchy tree |

### Extractor Interface (FR-017a-55 to FR-017a-60)

| ID | Requirement |
|----|-------------|
| FR-017a-55 | CSharpSymbolExtractor MUST implement ISymbolExtractor interface |
| FR-017a-56 | Extractor MUST register for .cs file extension |
| FR-017a-57 | ExtractAsync MUST return ExtractionResult with all discovered symbols |
| FR-017a-58 | Extractor MUST report extraction errors in result object |
| FR-017a-59 | ExtractAsync MUST support CancellationToken |
| FR-017a-60 | Extractor MUST support progress reporting via IProgress<T> |

---

## Non-Functional Requirements

### Performance (NFR-017a-01 to NFR-017a-06)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017a-01 | Performance | Syntax parsing MUST complete in < 50ms per file (median) |
| NFR-017a-02 | Performance | Symbol extraction MUST complete in < 100ms per file (median) |
| NFR-017a-03 | Performance | System MUST handle files up to 500KB without degradation |
| NFR-017a-04 | Performance | Memory usage MUST remain < 100MB when processing 1000 files |
| NFR-017a-05 | Performance | Semantic model creation MUST be cached per compilation |
| NFR-017a-06 | Performance | Parallel file processing MUST be supported for batch extraction |

### Reliability (NFR-017a-07 to NFR-017a-12)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017a-07 | Reliability | System MUST handle malformed C# code without crashing |
| NFR-017a-08 | Reliability | Partial results MUST be returned when errors occur |
| NFR-017a-09 | Reliability | Invalid input MUST NOT cause unhandled exceptions |
| NFR-017a-10 | Reliability | Cancellation MUST be honored within 100ms |
| NFR-017a-11 | Reliability | File system errors MUST be logged and skipped gracefully |
| NFR-017a-12 | Reliability | Roslyn exceptions MUST be caught and wrapped in domain exceptions |

### Security (NFR-017a-13 to NFR-017a-16)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017a-13 | Security | Source file content MUST NOT be logged at INFO level |
| NFR-017a-14 | Security | File paths MUST be validated against directory traversal |
| NFR-017a-15 | Security | No code execution MUST occur during parsing |
| NFR-017a-16 | Security | Exception messages MUST NOT contain file content |

### Maintainability (NFR-017a-17 to NFR-017a-20)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017a-17 | Maintainability | All public APIs MUST have XML documentation |
| NFR-017a-18 | Maintainability | Unit test coverage MUST exceed 80% |
| NFR-017a-19 | Maintainability | Roslyn version updates MUST NOT require interface changes |
| NFR-017a-20 | Maintainability | Symbol visitor MUST be extensible for new C# syntax |

### Observability (NFR-017a-21 to NFR-017a-24)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017a-21 | Observability | Extraction duration MUST be logged per file |
| NFR-017a-22 | Observability | Symbol count MUST be logged per file |
| NFR-017a-23 | Observability | Parse errors MUST be logged with file path and position |
| NFR-017a-24 | Observability | Memory pressure warnings MUST be logged at threshold |

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

## Best Practices

### Roslyn Integration

1. **Use semantic model** - Not just syntax for accurate type resolution
2. **Handle partial builds** - Extract symbols even from incomplete code
3. **Cache compilations** - Reuse Roslyn workspace across files
4. **Dispose properly** - Release Roslyn resources to prevent memory leaks

### Symbol Extraction

5. **Extract all symbol types** - Classes, interfaces, methods, properties, fields, enums
6. **Capture modifiers** - public, private, static, async, etc.
7. **Include documentation** - XML doc comments for symbol descriptions
8. **Track generic parameters** - Generic types and constraints

### Performance

9. **Parallel file processing** - Process independent files concurrently
10. **Memory management** - Stream large files, don't load fully into memory
11. **Skip generated code** - Exclude .g.cs, designer files by default
12. **Cache resolved types** - Avoid redundant type resolution

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Symbols/CSharp/
├── CSharpExtractorTests.cs
│   ├── Should_Extract_Class()
│   ├── Should_Extract_Abstract_Class()
│   ├── Should_Extract_Static_Class()
│   ├── Should_Extract_Sealed_Class()
│   ├── Should_Extract_Partial_Class()
│   ├── Should_Extract_Generic_Class()
│   ├── Should_Extract_Interface()
│   ├── Should_Extract_Struct()
│   ├── Should_Extract_Record()
│   ├── Should_Extract_Enum()
│   ├── Should_Extract_Delegate()
│   ├── Should_Extract_Method()
│   ├── Should_Extract_Async_Method()
│   ├── Should_Extract_Static_Method()
│   ├── Should_Extract_Extension_Method()
│   ├── Should_Extract_Property()
│   ├── Should_Extract_Auto_Property()
│   ├── Should_Extract_Expression_Property()
│   ├── Should_Extract_Field()
│   ├── Should_Extract_Const_Field()
│   ├── Should_Extract_Constructor()
│   ├── Should_Extract_Static_Constructor()
│   ├── Should_Extract_Event()
│   ├── Should_Extract_Indexer()
│   ├── Should_Extract_Operator()
│   ├── Should_Extract_Visibility_Public()
│   ├── Should_Extract_Visibility_Private()
│   ├── Should_Extract_Visibility_Protected()
│   ├── Should_Extract_Visibility_Internal()
│   ├── Should_Extract_Method_Signature()
│   ├── Should_Extract_Generic_Constraints()
│   ├── Should_Extract_Inheritance()
│   ├── Should_Extract_Interface_Implementation()
│   ├── Should_Extract_Attributes()
│   ├── Should_Extract_Location()
│   ├── Should_Handle_Malformed_File()
│   └── Should_Handle_Partial_Results()
│
├── RoslynParserTests.cs
│   ├── Should_Parse_Single_File()
│   ├── Should_Parse_Multiple_Files()
│   ├── Should_Get_Syntax_Tree()
│   ├── Should_Handle_Syntax_Errors()
│   ├── Should_Handle_Different_LangVersions()
│   ├── Should_Handle_Preprocessor_Directives()
│   ├── Should_Handle_Nullable_Context()
│   └── Should_Cache_Compilation()
│
├── XmlDocExtractorTests.cs
│   ├── Should_Extract_Summary()
│   ├── Should_Extract_Params()
│   ├── Should_Extract_Returns()
│   ├── Should_Extract_Remarks()
│   ├── Should_Extract_Exception()
│   ├── Should_Extract_Example()
│   ├── Should_Extract_See_Refs()
│   ├── Should_Handle_Missing_Docs()
│   ├── Should_Handle_Malformed_Xml()
│   └── Should_Strip_Tags()
│
├── SignatureFormatterTests.cs
│   ├── Should_Format_Method_Signature()
│   ├── Should_Format_Generic_Method()
│   ├── Should_Format_Overloaded_Method()
│   ├── Should_Format_Property_Signature()
│   └── Should_Format_Indexer_Signature()
│
└── SymbolVisitorTests.cs
    ├── Should_Visit_All_Classes()
    ├── Should_Visit_Nested_Classes()
    ├── Should_Visit_All_Methods()
    ├── Should_Maintain_Containment()
    └── Should_Handle_Deep_Nesting()
```

### Integration Tests

```
Tests/Integration/Symbols/CSharp/
├── CSharpExtractorIntegrationTests.cs
│   ├── Should_Extract_Real_File()
│   ├── Should_Extract_Real_Project()
│   ├── Should_Handle_Large_File()
│   ├── Should_Handle_Many_Files()
│   ├── Should_Handle_Generated_Code()
│   └── Should_Extract_NuGet_Types()
│
└── CSharpSemanticIntegrationTests.cs
    ├── Should_Resolve_Type_References()
    └── Should_Handle_Missing_References()
```

### E2E Tests

```
Tests/E2E/Symbols/CSharp/
├── CSharpSymbolE2ETests.cs
│   ├── Should_Index_CSharp_Project()
│   ├── Should_Search_CSharp_Symbols()
│   └── Should_Extract_Via_CLI()
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