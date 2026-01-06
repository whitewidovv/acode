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

### Return on Investment (ROI)

C# symbol extraction delivers significant value as the foundation for .NET code intelligence:

#### Quantified Benefits

| Benefit Category | Annual Value | Calculation Basis |
|------------------|--------------|-------------------|
| **Precise C# Code Navigation** | **$90,000/year** | 20 min/day saved per .NET developer × 6 developers × $75/hour × 250 days |
| **Accurate Method Signatures** | **$45,000/year** | 50% reduction in incorrect method calls; 2 bugs/week avoided × $3,000/bug fix × 52 weeks / 6 devs |
| **Documentation Retrieval** | **$30,000/year** | XML doc extraction saves looking up docs; 15 min/day × 6 devs × $75/hour × 250 days |
| **Inheritance Understanding** | **$25,000/year** | Base class/interface navigation; 10 min/day saved per developer |
| **Reduced Context Pollution** | **$20,000/year** | Precise symbol extraction vs full-file inclusion; 15% token savings |
| **Total Annual Value** | **$210,000/year** | For a 6-developer .NET team |

#### Break-Even Analysis

| Metric | Value |
|--------|-------|
| Implementation Cost | 1 developer × 2 weeks × $75/hour × 40 hours = $6,000 |
| Roslyn NuGet Package | Free (MIT license) |
| Break-Even Point | < 2 weeks of production use |
| 3-Year ROI | ($210,000 × 3) - $6,000 = $624,000 (10,400% return) |

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                         C# SYMBOL EXTRACTION ARCHITECTURE                       │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                    CSharpSymbolExtractor (ISymbolExtractor)              │   │
│  │                                                                          │   │
│  │  ┌──────────────────┐                                                   │   │
│  │  │ ExtractAsync()   │─────┐                                             │   │
│  │  │ - filePath       │     │                                             │   │
│  │  │ - options        │     │                                             │   │
│  │  │ - cancellation   │     │                                             │   │
│  │  └──────────────────┘     │                                             │   │
│  └───────────────────────────┼─────────────────────────────────────────────┘   │
│                              │                                                  │
│                              ▼                                                  │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                         RoslynParser                                     │   │
│  │                                                                          │   │
│  │  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────────┐   │   │
│  │  │ AdhocWorkspace   │  │ SyntaxTree       │  │ SemanticModel        │   │   │
│  │  │ (File isolation) │  │ (Parsed AST)     │  │ (Type resolution)    │   │   │
│  │  └────────┬─────────┘  └────────┬─────────┘  └──────────┬───────────┘   │   │
│  │           │                     │                       │               │   │
│  │           │    ParseFile()      │   GetSemanticModel()  │               │   │
│  │           └─────────────────────┼───────────────────────┘               │   │
│  │                                 │                                       │   │
│  └─────────────────────────────────┼───────────────────────────────────────┘   │
│                                    │                                            │
│                                    ▼                                            │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                    SymbolVisitor (CSharpSyntaxWalker)                    │   │
│  │                                                                          │   │
│  │  ┌─────────────────────────────────────────────────────────────────┐    │   │
│  │  │                    Visitor Methods                               │    │   │
│  │  │  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────┐   │    │   │
│  │  │  │VisitClassDecl   │  │VisitMethodDecl  │  │VisitPropDecl │   │    │   │
│  │  │  └──────────────────┘  └──────────────────┘  └──────────────┘   │    │   │
│  │  │  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────┐   │    │   │
│  │  │  │VisitInterfaceDecl│  │VisitFieldDecl  │  │VisitEnumDecl │   │    │   │
│  │  │  └──────────────────┘  └──────────────────┘  └──────────────┘   │    │   │
│  │  │  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────┐   │    │   │
│  │  │  │VisitStructDecl  │  │VisitRecordDecl  │  │VisitEventDecl│   │    │   │
│  │  │  └──────────────────┘  └──────────────────┘  └──────────────┘   │    │   │
│  │  └─────────────────────────────────────────────────────────────────┘    │   │
│  │                                 │                                        │   │
│  │                                 ▼                                        │   │
│  │  ┌─────────────────────────────────────────────────────────────────┐    │   │
│  │  │                    Symbol Building                               │    │   │
│  │  │  ┌──────────────┐  ┌──────────────┐  ┌────────────────────────┐ │    │   │
│  │  │  │ Name         │  │ Location     │  │ ContainingSymbolId     │ │    │   │
│  │  │  │ FQN          │  │ StartLine    │  │ (Parent tracking)      │ │    │   │
│  │  │  │ Kind         │  │ EndLine      │  └────────────────────────┘ │    │   │
│  │  │  │ Visibility   │  │ StartColumn  │                             │    │   │
│  │  │  │ Modifiers    │  │ EndColumn    │                             │    │   │
│  │  │  └──────────────┘  └──────────────┘                             │    │   │
│  │  └─────────────────────────────────────────────────────────────────┘    │   │
│  └──────────────────────────────────────────────────────────────────────────┘   │
│                                    │                                            │
│          ┌─────────────────────────┼─────────────────────────┐                  │
│          │                         │                         │                  │
│          ▼                         ▼                         ▼                  │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────────────────┐  │
│  │ XmlDocExtractor  │  │ SignatureFormatter│  │ ModifierExtractor           │  │
│  │                  │  │                  │  │                              │  │
│  │ <summary>        │  │ FormatMethod()   │  │ public/private/protected     │  │
│  │ <param>          │  │ FormatProperty() │  │ static/abstract/sealed       │  │
│  │ <returns>        │  │ FormatIndexer()  │  │ async/virtual/override       │  │
│  │ <remarks>        │  │ FormatDelegate() │  │ readonly/const/volatile      │  │
│  │ <example>        │  │                  │  │                              │  │
│  └──────────────────┘  └──────────────────┘  └──────────────────────────────┘  │
│          │                         │                         │                  │
│          └─────────────────────────┼─────────────────────────┘                  │
│                                    │                                            │
│                                    ▼                                            │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                    ExtractionResult                                      │   │
│  │  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────────┐   │   │
│  │  │ Success: bool    │  │ Symbols: List    │  │ Errors: List         │   │   │
│  │  │ FilePath: string │  │ ISymbol          │  │ ParseError           │   │   │
│  │  │ DurationMs: long │  │                  │  │                      │   │   │
│  │  └──────────────────┘  └──────────────────┘  └──────────────────────┘   │   │
│  └──────────────────────────────────────────────────────────────────────────┘   │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Data Flow

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                      C# EXTRACTION DATA FLOW                                    │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  INPUT                                                                          │
│  ═════                                                                          │
│                                                                                 │
│  ┌──────────────────────────────────────────────────────────────────────────┐  │
│  │ public class UserService : IUserService                                  │  │
│  │ {                                                                         │  │
│  │     /// <summary>Gets a user by ID.</summary>                            │  │
│  │     /// <param name="id">The user's unique identifier.</param>           │  │
│  │     /// <returns>The user if found, null otherwise.</returns>            │  │
│  │     public async Task<User?> GetUserByIdAsync(int id)                    │  │
│  │     {                                                                     │  │
│  │         return await _repository.FindAsync(id);                          │  │
│  │     }                                                                     │  │
│  │ }                                                                         │  │
│  └──────────────────────────────────────────────────────────────────────────┘  │
│                                    │                                            │
│                                    ▼                                            │
│  ROSLYN PARSING                                                                 │
│  ══════════════                                                                 │
│                                                                                 │
│  ┌──────────────────────────────────────────────────────────────────────────┐  │
│  │ SyntaxTree                                                                │  │
│  │  └─ CompilationUnitSyntax                                                │  │
│  │      └─ ClassDeclarationSyntax (UserService)                             │  │
│  │          ├─ BaseList: IUserService                                       │  │
│  │          └─ MethodDeclarationSyntax (GetUserByIdAsync)                   │  │
│  │              ├─ ReturnType: Task<User?>                                  │  │
│  │              ├─ Modifiers: [public, async]                               │  │
│  │              ├─ Parameters: (int id)                                     │  │
│  │              └─ LeadingTrivia: XML documentation                         │  │
│  └──────────────────────────────────────────────────────────────────────────┘  │
│                                    │                                            │
│                                    ▼                                            │
│  SYMBOL VISITOR EXTRACTION                                                      │
│  ═════════════════════════                                                      │
│                                    │                                            │
│                                    ▼                                            │
│  OUTPUT (ISymbol List)                                                          │
│  ═════════════════════                                                          │
│                                                                                 │
│  ┌──────────────────────────────────────────────────────────────────────────┐  │
│  │ Symbol 1: UserService                                                    │  │
│  │   Kind: Class                                                            │  │
│  │   FQN: MyApp.Services.UserService                                        │  │
│  │   Location: UserService.cs:1-12                                          │  │
│  │   Visibility: public                                                     │  │
│  │   BaseTypes: [IUserService]                                              │  │
│  ├──────────────────────────────────────────────────────────────────────────┤  │
│  │ Symbol 2: GetUserByIdAsync                                               │  │
│  │   Kind: Method                                                           │  │
│  │   FQN: MyApp.Services.UserService.GetUserByIdAsync                       │  │
│  │   Location: UserService.cs:6-10                                          │  │
│  │   Visibility: public                                                     │  │
│  │   Modifiers: [async]                                                     │  │
│  │   Signature: public async Task<User?> GetUserByIdAsync(int id)           │  │
│  │   ContainingSymbol: UserService                                          │  │
│  │   Documentation:                                                          │  │
│  │     Summary: "Gets a user by ID."                                        │  │
│  │     Params: [{ name: "id", desc: "The user's unique identifier." }]      │  │
│  │     Returns: "The user if found, null otherwise."                        │  │
│  └──────────────────────────────────────────────────────────────────────────┘  │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Trade-Offs Analysis

#### Trade-Off 1: Syntax-Only vs Semantic Analysis

| Approach | Pros | Cons |
|----------|------|------|
| **Syntax-Only (Default)** | Fast (10x), no project context needed, works with incomplete code | No type resolution, unresolved generic types, no inheritance info |
| **Semantic Analysis** | Full type resolution, inheritance trees, overload identification | Slow, requires compilable project, memory intensive |

**Decision:** Default to syntax-only mode for speed. Semantic mode available via configuration when accuracy is critical.

**Quantified Impact:**
- Syntax-only: 30ms/file, works on any .cs file
- Semantic: 300ms/file, requires .csproj context
- Accuracy: Syntax captures 85% of needed info, semantic adds 15% more detail

#### Trade-Off 2: Complete AST Walk vs Targeted Declaration Visitors

| Approach | Pros | Cons |
|----------|------|------|
| **CSharpSyntaxWalker (Chosen)** | Visits all nodes, extensible, handles nested declarations | Visits unused nodes, slightly more memory |
| **CSharpSyntaxVisitor (Targeted)** | Only visits requested types, minimal overhead | Manual descent for nested types, easy to miss declarations |

**Decision:** Use CSharpSyntaxWalker for complete coverage. Override only declaration visitor methods to minimize overhead.

**Quantified Impact:**
- Walker: 100% declaration coverage, 5% overhead for non-declaration nodes
- Targeted: 95% coverage (risk of missing edge cases), marginally faster

#### Trade-Off 3: Eager vs Lazy XML Documentation Parsing

| Approach | Pros | Cons |
|----------|------|------|
| **Eager Parsing (Chosen)** | All docs available immediately, one parse pass | Memory for unused docs, parsing overhead |
| **Lazy Parsing** | Parse on demand, minimal initial memory | Multiple file reads, cache complexity |

**Decision:** Eager parsing during extraction. Documentation is small relative to symbol data and often needed for context.

**Quantified Impact:**
- Eager: 10% extraction time overhead, all docs in single pass
- Lazy: Repeated file I/O for doc lookups, 100ms+ per lazy fetch

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

### Performance Profile

| Metric | Target | Description |
|--------|--------|-------------|
| Single File Parse Time | < 50ms | Time to parse a typical 500-line C# file into syntax tree |
| Single File Extract Time | < 100ms | Time to extract all symbols including documentation |
| Memory per Symbol | < 2KB | Average heap allocation per extracted symbol |
| Concurrent Files | 8 parallel | Maximum parallel file processing capability |
| Cache Hit Ratio | > 80% | Semantic model cache effectiveness |
| Large File Threshold | 500KB | Files above this size use streaming extraction |

### ROI Analysis

**Development Savings:**
- **Manual Symbol Mapping:** ~2 hours/developer/week eliminated
- **Context Window Efficiency:** 40% improvement in relevant code inclusion
- **Onboarding Acceleration:** New developers ramp 25% faster with symbol search

**Quality Improvements:**
- **AI Response Accuracy:** 35% improvement in code understanding queries
- **Refactoring Safety:** Symbol relationships enable impact analysis
- **Documentation Coverage:** XML doc extraction improves inline help

**Total Estimated Value:** $15,000-25,000 annually per 10-developer team

---

## Use Cases

### Use Case 1: Developer Extracts Symbols from a Service Class

**Persona:** Marcus Chen, Senior .NET Developer, working on a microservices codebase.

**Context:** Marcus needs to understand all the methods and properties in `OrderProcessingService` before adding a new feature.

**Before C# Symbol Extraction:**
1. Opens the file in editor, scrolls through 800 lines
2. Manually notes each method signature
3. Checks base class for inherited methods
4. Looks for related interfaces
5. Total time: 15 minutes

**After C# Symbol Extraction:**
```bash
$ acode symbols extract src/Services/OrderProcessingService.cs

Extracting symbols from OrderProcessingService.cs...
Found 23 symbols:

Classes:
  OrderProcessingService (public) - lines 15-425

Methods:
  ProcessOrderAsync (public async) - lines 45-78
    Signature: Task<OrderResult> ProcessOrderAsync(Order order, CancellationToken ct)
    Summary: Processes an order through validation and fulfillment.
  ValidateOrder (private) - lines 82-110
    Signature: ValidationResult ValidateOrder(Order order)
  CalculateTotal (public) - lines 115-145
    Signature: decimal CalculateTotal(Order order, DiscountPolicy? discount)
  ... (20 more)
```

**Quantified Metrics:**
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Time to list methods | 15 min | 2 sec | 450x faster |
| Methods discovered | ~90% | 100% | Complete coverage |
| Signature accuracy | Manual error-prone | 100% | Eliminated errors |

---

### Use Case 2: AI Agent Finds Method to Modify

**Persona:** The AI coding agent, implementing a user-requested feature change.

**Context:** User requests: "Add retry logic to the PaymentGateway.ChargeAsync method." Agent needs to locate the exact method.

**Before C# Symbol Extraction:**
1. Agent greps for "ChargeAsync" - gets 45 matches (usages, tests, mocks)
2. Opens multiple files to find the definition
3. Includes entire file in context (800 lines)
4. LLM sees unrelated code, makes mistakes
5. 3 iterations needed

**After C# Symbol Extraction:**
```csharp
// Agent uses symbol extraction API
var symbols = await csharpExtractor.ExtractAsync("src/Payment/PaymentGateway.cs");
var method = symbols.FirstOrDefault(s => 
    s.Name == "ChargeAsync" && s.Kind == SymbolKind.Method);

// Result:
// Name: ChargeAsync
// Location: lines 145-198
// Signature: public async Task<ChargeResult> ChargeAsync(PaymentRequest request)
// Documentation: "Charges the payment method with the specified amount."
// ContainingType: PaymentGateway
```
1. Agent gets precise method location
2. Retrieves only lines 145-198 for context
3. LLM generates correct modification
4. Single iteration success

**Quantified Metrics:**
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Context size | 800 lines | 53 lines | 93% reduction |
| Iterations needed | 3 | 1 | 66% reduction |
| Token cost | ~$0.80 | ~$0.08 | 90% savings |

---

### Use Case 3: Documentation Extraction for API Understanding

**Persona:** Sofia Rodriguez, Junior Developer, new to the codebase.

**Context:** Sofia needs to understand what `IUserRepository` methods do before implementing a new repository.

**Before C# Symbol Extraction:**
1. Opens IUserRepository.cs
2. Reads through XML documentation comments manually
3. Copies interface to understand method signatures
4. Checks implementations for behavior
5. Total time: 20 minutes

**After C# Symbol Extraction:**
```bash
$ acode symbols extract src/Data/IUserRepository.cs --include-docs

Interface: IUserRepository (public)
  Summary: Repository for user data access operations.

Methods:
  GetByIdAsync
    Signature: Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
    Summary: Retrieves a user by their unique identifier.
    Params:
      id: The unique identifier of the user.
      ct: Cancellation token for the operation.
    Returns: The user if found, null otherwise.

  GetByEmailAsync
    Signature: Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    Summary: Retrieves a user by their email address.
    Params:
      email: The email address to search for.
      ct: Cancellation token for the operation.
    Returns: The user if found, null otherwise.

  CreateAsync
    Signature: Task<User> CreateAsync(User user, CancellationToken ct)
    Summary: Creates a new user in the repository.
    Throws: DuplicateEmailException if email already exists.
```

**Quantified Metrics:**
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Time to understand API | 20 min | 30 sec | 40x faster |
| Documentation completeness | Partial (what was read) | 100% | Complete |
| Mental model accuracy | Variable | Consistent | Reliable |

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

- **TypeScript/JavaScript Parsing** - Covered by Task 017.b, uses TypeScript Compiler API
- **Python/Go/Rust Parsing** - Future versions will add these language extractors
- **Cross-file Semantic Analysis** - Semantic model per file only; cross-project analysis requires project-level context
- **Code Refactoring** - Read-only extraction; no code modifications or renames
- **Code Generation** - Symbol extraction only; no source code output or scaffolding
- **Real-time/Incremental Updates** - Batch extraction only; file system watchers not included
- **IDE Integration** - CLI tool only; no Visual Studio or VS Code extension
- **Source Control Integration** - No Git/VCS awareness; operates on files directly
- **Build System Integration** - Does not invoke MSBuild or dotnet build
- **Type Flow Analysis** - No data flow or control flow analysis beyond basic symbol hierarchy

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

#### Issue 1: Missing Symbols from Extraction

**Symptoms:**
- Expected classes/methods not appearing in extraction results
- Symbol count lower than expected
- Nested types not found

**Possible Causes:**
1. File excluded by patterns (obj/, bin/, *.Designer.cs)
2. `include_private` setting is false and symbols are private
3. File has different extension (e.g., .csx, .cake)
4. Symbols inside #if preprocessor directives with unmet conditions
5. Partial class definitions split across files

**Solutions:**
1. Check exclude patterns in configuration:
   ```bash
   acode config get symbols.csharp.exclude_patterns
   ```
2. Enable private member extraction:
   ```bash
   acode symbols extract src/ --include-private
   ```
3. Verify file extensions in scope:
   ```bash
   acode symbols extract src/ --extensions .cs,.csx
   ```
4. Check for preprocessor directives in source
5. Extract from all partial class files together

---

#### Issue 2: Slow Extraction Performance

**Symptoms:**
- Extraction takes >10 seconds for a single file
- Memory usage spikes during extraction
- CPU at 100% during parsing

**Possible Causes:**
1. Semantic analysis enabled when not needed
2. Very large files (>10K lines)
3. Complex generic types requiring resolution
4. Many referenced assemblies to resolve
5. No Roslyn workspace caching

**Solutions:**
1. Disable semantic model for faster syntax-only extraction:
   ```bash
   acode symbols extract src/ --no-semantic
   ```
2. Exclude generated and test files:
   ```bash
   acode symbols extract src/ --exclude "**/*.g.cs" --exclude "**/Tests/**"
   ```
3. Increase memory limit for large projects:
   ```bash
   acode symbols extract src/ --max-memory 2048
   ```
4. Enable parallel extraction for multiple files:
   ```bash
   acode symbols extract src/ --parallel
   ```
5. Check extraction stats to identify bottlenecks:
   ```bash
   acode symbols extract src/ --stats
   ```

---

#### Issue 3: Roslyn Parse Errors

**Symptoms:**
- Extraction log shows parse errors
- Error code ACODE-CSE-001 or ACODE-CSE-003
- Partial symbol results returned

**Possible Causes:**
1. Invalid C# syntax in source file
2. C# language version mismatch
3. Missing using directives (semantic mode only)
4. Incomplete code during editing
5. Non-UTF8 file encoding

**Solutions:**
1. Verify code compiles with dotnet build first
2. Check C# language version in log:
   ```bash
   acode symbols extract src/File.cs --verbose
   ```
3. Extraction continues with partial results - check returned symbols
4. For incomplete code, syntax-only mode is more forgiving:
   ```bash
   acode symbols extract src/ --no-semantic
   ```
5. Convert file to UTF-8 encoding

---

#### Issue 4: Missing XML Documentation

**Symptoms:**
- Documentation property is null/empty for symbols
- Only some methods have documentation
- XML tags not parsed correctly

**Possible Causes:**
1. No XML documentation comments in source (///...)
2. Documentation in wrong format (// instead of ///)
3. Malformed XML in documentation comments
4. Documentation disabled in extraction options

**Solutions:**
1. Verify source has XML documentation:
   ```csharp
   /// <summary>This format is required</summary>
   public void Method() { }
   ```
2. Check documentation extraction is enabled:
   ```bash
   acode symbols extract src/ --include-docs
   ```
3. For malformed XML, check extraction log for warnings
4. Validate XML syntax in documentation comments
5. Use semantic analysis for inherited documentation

---

#### Issue 5: Memory Exhaustion on Large Projects

**Symptoms:**
- OutOfMemoryException during extraction
- Process killed by OS
- Extraction hangs then fails

**Possible Causes:**
1. Too many files loaded simultaneously
2. Semantic model cache not bounded
3. Large solution with many projects
4. Circular project references
5. Memory leak in Roslyn workspace

**Solutions:**
1. Extract in batches by folder:
   ```bash
   acode symbols extract src/Module1/ --no-semantic
   acode symbols extract src/Module2/ --no-semantic
   ```
2. Use streaming extraction mode:
   ```bash
   acode symbols extract src/ --streaming
   ```
3. Limit concurrent file processing:
   ```bash
   acode symbols extract src/ --max-concurrent 4
   ```
4. Reduce memory per-file:
   ```bash
   acode symbols extract src/ --max-file-size 500KB
   ```
5. Ensure Roslyn workspace is disposed after each batch

---

#### Issue 6: Inconsistent Symbol IDs Across Runs

**Symptoms:**
- Same symbol gets different ID on re-extraction
- Symbol lookups fail after re-indexing
- Incremental updates cause duplicates

**Possible Causes:**
1. ID generation includes non-deterministic data
2. File path changed (moved/renamed)
3. Symbol signature changed
4. Different extraction options used
5. Line numbers included in ID hash

**Solutions:**
1. Symbol IDs use deterministic hash of: file path + symbol FQN + kind
2. After moving files, re-index the entire affected scope
3. Verify extraction options are consistent:
   ```bash
   acode symbols extract src/ --config symbols.yml
   ```
4. Check symbol ID algorithm in configuration
5. Use qualified names for stable identification

---

## Acceptance Criteria

### Roslyn Workspace & Parsing (AC-001 to AC-008)

- [ ] AC-001: `RoslynParser` creates `AdhocWorkspace` for single-file analysis
- [ ] AC-002: `RoslynParser` creates workspace from .csproj when semantic analysis requested
- [ ] AC-003: `RoslynParser.ParseAsync()` returns `SyntaxTree` with diagnostics
- [ ] AC-004: Parse errors are logged but do not throw exceptions
- [ ] AC-005: C# language version auto-detected from source syntax
- [ ] AC-006: Files with BOM and different encodings parsed correctly
- [ ] AC-007: Preprocessor symbols (#if DEBUG) handled correctly
- [ ] AC-008: Partial classes across multiple files merged correctly (semantic mode)

### Symbol Extraction - Types (AC-009 to AC-018)

- [ ] AC-009: `SymbolVisitor.VisitClassDeclaration()` extracts class symbols
- [ ] AC-010: Nested classes extracted with correct `ContainingType`
- [ ] AC-011: Abstract and sealed class modifiers captured
- [ ] AC-012: `SymbolVisitor.VisitInterfaceDeclaration()` extracts interface symbols
- [ ] AC-013: Interface inheritance captured in symbol metadata
- [ ] AC-014: `SymbolVisitor.VisitStructDeclaration()` extracts struct symbols
- [ ] AC-015: `SymbolVisitor.VisitEnumDeclaration()` extracts enum symbols
- [ ] AC-016: Enum members extracted with values
- [ ] AC-017: `SymbolVisitor.VisitRecordDeclaration()` extracts record symbols (C# 9+)
- [ ] AC-018: Generic type parameters captured with constraints

### Symbol Extraction - Members (AC-019 to AC-028)

- [ ] AC-019: `SymbolVisitor.VisitMethodDeclaration()` extracts method symbols
- [ ] AC-020: Method parameters extracted with types and names
- [ ] AC-021: Return type captured in method signature
- [ ] AC-022: `SymbolVisitor.VisitPropertyDeclaration()` extracts property symbols
- [ ] AC-023: Property getter/setter accessors captured
- [ ] AC-024: `SymbolVisitor.VisitFieldDeclaration()` extracts field symbols
- [ ] AC-025: Const and readonly field modifiers captured
- [ ] AC-026: `SymbolVisitor.VisitConstructorDeclaration()` extracts constructors
- [ ] AC-027: `SymbolVisitor.VisitEventDeclaration()` extracts event symbols
- [ ] AC-028: `SymbolVisitor.VisitDelegateDeclaration()` extracts delegate symbols

### Symbol Metadata (AC-029 to AC-038)

- [ ] AC-029: Symbol name extracted correctly for all symbol kinds
- [ ] AC-030: Fully qualified name includes namespace and containing types
- [ ] AC-031: `SymbolLocation` includes file path, start line, end line, start column, end column
- [ ] AC-032: Visibility (public, private, protected, internal) captured correctly
- [ ] AC-033: Static modifier captured for all applicable symbols
- [ ] AC-034: Async modifier captured for methods
- [ ] AC-035: Override and virtual modifiers captured
- [ ] AC-036: `SignatureFormatter.FormatSignature()` produces human-readable signature
- [ ] AC-037: Generic method parameters included in signature
- [ ] AC-038: Symbol ID generated as deterministic hash

### XML Documentation Extraction (AC-039 to AC-046)

- [ ] AC-039: `XmlDocExtractor.ExtractDocumentation()` parses XML doc comments
- [ ] AC-040: `<summary>` tag extracted as symbol description
- [ ] AC-041: `<param>` tags extracted with parameter names and descriptions
- [ ] AC-042: `<returns>` tag extracted as return description
- [ ] AC-043: `<exception>` tags extracted with exception types
- [ ] AC-044: `<remarks>` tag extracted when present
- [ ] AC-045: `<see cref="..."/>` references preserved in documentation
- [ ] AC-046: Malformed XML does not crash extraction; warning logged

### Interface Implementation (AC-047 to AC-052)

- [ ] AC-047: `CSharpSymbolExtractor` implements `ISymbolExtractor` interface
- [ ] AC-048: `Language` property returns "csharp"
- [ ] AC-049: `FileExtensions` property returns `[".cs"]`
- [ ] AC-050: `ExtractAsync()` returns `ExtractionResult` with symbols list
- [ ] AC-051: `ExtractionResult` includes extraction statistics
- [ ] AC-052: Extractor registered in `IExtractorRegistry` during startup

### Error Handling (AC-053 to AC-058)

- [ ] AC-053: File not found returns `ExtractionResult` with error code ACODE-CSE-002
- [ ] AC-054: Invalid C# syntax returns partial results with error code ACODE-CSE-003
- [ ] AC-055: `CancellationToken` cancellation stops extraction gracefully
- [ ] AC-056: Exceptions logged with full context and stack trace
- [ ] AC-057: Timeout after configurable duration (default 30s per file)
- [ ] AC-058: Resource cleanup occurs even on exception (Roslyn workspace disposed)

### Performance (AC-059 to AC-064)

- [ ] AC-059: Single file extraction completes in <100ms for files <1000 lines
- [ ] AC-060: Batch extraction processes 100 files in <10 seconds
- [ ] AC-061: Memory usage bounded to configurable limit (default 500MB)
- [ ] AC-062: Parallel extraction uses configurable thread count
- [ ] AC-063: Roslyn workspace cached and reused for same project
- [ ] AC-064: Generated files (*.g.cs, *.Designer.cs) excluded by default

### Configuration (AC-065 to AC-070)

- [ ] AC-065: `include_private` option controls private member extraction
- [ ] AC-066: `exclude_patterns` option filters files by glob patterns
- [ ] AC-067: `max_file_size` option limits file size processed
- [ ] AC-068: `use_semantic` option enables/disables semantic analysis
- [ ] AC-069: `documentation_mode` option controls XML doc extraction
- [ ] AC-070: Configuration loaded from acode.yml symbols.csharp section

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

## Assumptions

This section documents the technical, operational, and integration assumptions made for Task 017.a. If any assumption proves invalid, the implementation approach may need revision.

### Technical Assumptions

1. **.NET SDK Availability** - The .NET 8 SDK or later is installed on all target deployment environments where C# symbol extraction is performed.

2. **Roslyn Package Stability** - The Microsoft.CodeAnalysis.CSharp NuGet package (version 4.x) maintains backward compatibility for syntax tree parsing and symbol extraction APIs.

3. **UTF-8 Source Files** - All C# source files use UTF-8 encoding. Files with BOM are handled, but other encodings require explicit configuration.

4. **Standard C# Language Versions** - Source files use standard C# language features up to C# 12. Experimental or preview features may not be fully supported.

5. **XML Documentation Format** - XML documentation comments follow the standard .NET format (`<summary>`, `<param>`, `<returns>`, etc.). Non-standard tags are extracted as raw content.

6. **Solution/Project Structure** - For semantic analysis, .csproj files follow standard MSBuild format. SDK-style projects are fully supported; legacy projects have limited support.

7. **Single Assembly Context** - Symbol extraction operates within a single assembly context. Cross-project references require explicit project loading.

### Operational Assumptions

8. **Memory Availability** - The agent has at least 500 MB of available memory for Roslyn workspace operations per concurrent extraction.

9. **File System Access** - The agent has read access to all C# files in the workspace. Files with restricted permissions are skipped with appropriate error messages.

10. **No Concurrent File Modifications** - Source files are not modified during extraction. If a file changes mid-extraction, partial or inconsistent results may occur.

11. **Reasonable File Sizes** - Individual C# files do not exceed 1 MB. Generated files (.g.cs, .Designer.cs) are excluded by default.

12. **Workspace Boundaries** - Extraction operates within a single workspace root. Symbolic links and junction points may cause duplicate symbol extraction.

### Integration Assumptions

13. **Symbol Index Availability** - Task 017 (Symbol Index v2) is complete and provides the `ISymbolIndex` interface for storing extracted symbols.

14. **Logging Infrastructure** - The standard Acode logging infrastructure (`Microsoft.Extensions.Logging`) is available for the `Acode.CSharp.*` namespaces.

15. **Configuration System** - The Acode configuration system provides access to `symbol_index.csharp.*` settings via standard configuration providers.

16. **Error Aggregation** - Extraction errors are reported through the standard `SymbolExtractionError` type, which integrates with the error aggregation system.

17. **Cancellation Token Propagation** - Calling code properly propagates `CancellationToken` for all extraction operations.

18. **Dependency Injection** - Services are registered via standard .NET DI container (`Microsoft.Extensions.DependencyInjection`).

---

## Troubleshooting

This section provides diagnosis and resolution steps for common issues with C# symbol extraction.

---

### Issue 1: Roslyn NuGet Package Not Found

**Symptoms:**
- Build error: `Could not resolve package Microsoft.CodeAnalysis.CSharp`
- Runtime error: `FileNotFoundException: Could not load file or assembly 'Microsoft.CodeAnalysis'`
- Symbol extraction returns empty results

**Possible Causes:**
1. NuGet packages not restored
2. Incorrect package version in csproj
3. Package source not configured
4. Conflicting Roslyn versions from other dependencies

**Solutions:**

1. **Restore NuGet Packages:**
```powershell
dotnet restore
```

2. **Verify Package Reference:**
```xml
<!-- In Acode.Infrastructure.csproj -->
<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" />
<PackageReference Include="Microsoft.CodeAnalysis.CSharp.Workspaces" Version="4.8.0" />
```

3. **Check for Version Conflicts:**
```powershell
dotnet list package --include-transitive | findstr "Microsoft.CodeAnalysis"
```

4. **Force Specific Version:**
```xml
<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" ExcludeAssets="build" />
```

---

### Issue 2: Symbols Missing from Extraction Results

**Symptoms:**
- Expected classes/methods not appearing in symbol list
- Partial extraction with fewer symbols than expected
- Nested types not appearing

**Possible Causes:**
1. File excluded by glob pattern
2. Private members excluded by configuration
3. Syntax errors in source file
4. File encoding not recognized
5. Preprocessor directives hiding code

**Solutions:**

1. **Verify Exclusion Patterns:**
```yaml
# .agent/config.yml
symbol_index:
  csharp:
    exclude_patterns:
      - "**/obj/**"
      - "**/bin/**"
      # Remove patterns that exclude your files
```

2. **Enable Private Member Extraction:**
```yaml
symbol_index:
  csharp:
    include_private: true
```

3. **Check for Syntax Errors:**
```powershell
dotnet build src/YourProject.csproj --verbosity detailed
```

4. **Verify File Encoding:**
```powershell
# Check file encoding
[System.IO.File]::ReadAllBytes("path/to/file.cs")[0..2]
# UTF-8 BOM: 239, 187, 191
```

---

### Issue 3: Extraction is Extremely Slow

**Symptoms:**
- Extraction takes more than 10 seconds per file
- Memory usage grows continuously
- Agent becomes unresponsive during extraction

**Possible Causes:**
1. Semantic analysis enabled for all files (expensive)
2. Very large generated files included
3. Roslyn workspace not being reused
4. No file size limits configured
5. Parallel processing disabled

**Solutions:**

1. **Disable Semantic Analysis for Speed:**
```yaml
symbol_index:
  csharp:
    use_semantic: false  # Syntax-only is much faster
```

2. **Exclude Generated Files:**
```yaml
symbol_index:
  csharp:
    exclude_patterns:
      - "**/*.g.cs"
      - "**/*.Designer.cs"
      - "**/obj/**"
```

3. **Configure File Size Limits:**
```yaml
symbol_index:
  csharp:
    max_file_size_kb: 200  # Skip files larger than 200KB
```

4. **Enable Parallel Processing:**
```yaml
symbol_index:
  csharp:
    parallel_extraction: true
    max_parallelism: 4
```

---

### Issue 4: XML Documentation Not Extracted

**Symptoms:**
- Symbols extracted but `documentation` field is empty
- `<summary>` content not appearing
- Parameter descriptions missing

**Possible Causes:**
1. XML documentation extraction disabled
2. XML doc comments missing or malformed
3. Comments not using triple-slash format
4. Build not generating XML documentation

**Solutions:**

1. **Enable Documentation Extraction:**
```yaml
symbol_index:
  csharp:
    extract_documentation: true
```

2. **Verify XML Doc Format:**
```csharp
// Correct format - must use triple-slash
/// <summary>
/// Calculates the sum of two numbers.
/// </summary>
/// <param name="a">First number</param>
/// <param name="b">Second number</param>
/// <returns>The sum of a and b</returns>
public int Add(int a, int b) => a + b;
```

3. **Check for Malformed XML:**
```powershell
# Enable doc warnings
dotnet build -warnaserror:CS1570,CS1571,CS1572,CS1573
```

---

### Issue 5: Memory Exhaustion During Large Project Extraction

**Symptoms:**
- OutOfMemoryException during extraction
- Agent crashes with exit code indicating memory failure
- Gradual memory increase until failure

**Possible Causes:**
1. Too many files processed without disposal
2. Roslyn workspace accumulating projects
3. No memory limits configured
4. Large solution with many projects

**Solutions:**

1. **Configure Memory Limits:**
```yaml
symbol_index:
  csharp:
    max_memory_mb: 512
    gc_threshold_mb: 400
```

2. **Process in Batches:**
```yaml
symbol_index:
  csharp:
    batch_size: 100  # Process 100 files, then GC
```

3. **Force Workspace Disposal:**
```yaml
symbol_index:
  csharp:
    dispose_workspace_after: 500  # files
```

4. **Monitor Memory Usage:**
```powershell
acode symbols extract src/ --stats --verbose
# Watch memory column in output
```

---

## Security Threats and Mitigations

### Threat 1: Directory Traversal via Malicious File Paths

**Threat ID:** THREAT-017a-001  
**Severity:** HIGH  
**Attack Vector:** Attacker provides crafted file path like `../../../etc/passwd` or `C:\Windows\System32\config\SAM`  
**Impact:** Unauthorized file access outside intended scope, information disclosure

**Mitigation - Complete C# Implementation:**

```csharp
using System;
using System.IO;

namespace AgenticCoder.Infrastructure.Symbols.CSharp.Security;

/// <summary>
/// Validates and sanitizes file paths to prevent directory traversal attacks.
/// </summary>
public static class PathSecurityValidator
{
    /// <summary>
    /// Validates that the given file path is within the allowed base directory.
    /// </summary>
    /// <param name="filePath">The file path to validate.</param>
    /// <param name="allowedBaseDirectory">The root directory that all paths must be within.</param>
    /// <returns>True if path is safe, false otherwise.</returns>
    /// <exception cref="SecurityException">Thrown when path traversal is detected.</exception>
    public static bool ValidatePath(string filePath, string allowedBaseDirectory)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(allowedBaseDirectory);
        
        // Normalize paths to absolute form
        string normalizedFilePath = Path.GetFullPath(filePath);
        string normalizedBasePath = Path.GetFullPath(allowedBaseDirectory);
        
        // Ensure base path ends with separator for accurate comparison
        if (!normalizedBasePath.EndsWith(Path.DirectorySeparatorChar))
        {
            normalizedBasePath += Path.DirectorySeparatorChar;
        }
        
        // Check if file path starts with base path (case-insensitive on Windows)
        bool isWithinBase = normalizedFilePath.StartsWith(
            normalizedBasePath, 
            StringComparison.OrdinalIgnoreCase);
        
        if (!isWithinBase)
        {
            throw new SecurityException(
                $"Path traversal detected: '{filePath}' is outside allowed directory '{allowedBaseDirectory}'");
        }
        
        // Additional checks for suspicious patterns
        string[] dangerousPatterns = { "..", "..\\", "../" };
        foreach (var pattern in dangerousPatterns)
        {
            if (filePath.Contains(pattern))
            {
                // Re-verify after normalization caught any issues
                var segments = normalizedFilePath.Split(Path.DirectorySeparatorChar);
                if (segments.Any(s => s == ".."))
                {
                    throw new SecurityException(
                        $"Suspicious path pattern detected in '{filePath}'");
                }
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Sanitizes a file path by resolving it to absolute form and validating.
    /// </summary>
    public static string SanitizePath(string filePath, string allowedBaseDirectory)
    {
        ValidatePath(filePath, allowedBaseDirectory);
        return Path.GetFullPath(filePath);
    }
}

/// <summary>
/// Custom security exception for path validation failures.
/// </summary>
public class SecurityException : Exception
{
    public SecurityException(string message) : base(message) { }
    public SecurityException(string message, Exception inner) : base(message, inner) { }
}
```

---

### Threat 2: Denial of Service via Large/Malicious Files

**Threat ID:** THREAT-017a-002  
**Severity:** HIGH  
**Attack Vector:** Attacker creates extremely large C# file or deeply nested structures to exhaust memory/CPU  
**Impact:** Service unavailability, resource exhaustion, application crash

**Mitigation - Complete C# Implementation:**

```csharp
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AgenticCoder.Infrastructure.Symbols.CSharp.Security;

/// <summary>
/// Guards against DoS attacks via resource limits during extraction.
/// </summary>
public sealed class ResourceLimitGuard : IDisposable
{
    private readonly long _maxFileSizeBytes;
    private readonly int _maxSymbolCount;
    private readonly int _maxNestingDepth;
    private readonly TimeSpan _timeout;
    private readonly CancellationTokenSource _timeoutCts;
    
    private int _currentSymbolCount;
    private int _currentNestingDepth;
    private bool _disposed;
    
    public ResourceLimitGuard(ResourceLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        
        _maxFileSizeBytes = limits.MaxFileSizeBytes;
        _maxSymbolCount = limits.MaxSymbolCount;
        _maxNestingDepth = limits.MaxNestingDepth;
        _timeout = limits.Timeout;
        
        _timeoutCts = new CancellationTokenSource(_timeout);
    }
    
    /// <summary>
    /// Gets a cancellation token that fires when timeout expires.
    /// </summary>
    public CancellationToken TimeoutToken => _timeoutCts.Token;
    
    /// <summary>
    /// Validates file size before processing.
    /// </summary>
    public void ValidateFileSize(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }
        
        if (fileInfo.Length > _maxFileSizeBytes)
        {
            throw new ResourceLimitExceededException(
                $"File size {fileInfo.Length:N0} bytes exceeds limit of {_maxFileSizeBytes:N0} bytes",
                ResourceLimitType.FileSize);
        }
    }
    
    /// <summary>
    /// Tracks symbol count and throws if limit exceeded.
    /// </summary>
    public void IncrementSymbolCount()
    {
        int count = Interlocked.Increment(ref _currentSymbolCount);
        if (count > _maxSymbolCount)
        {
            throw new ResourceLimitExceededException(
                $"Symbol count {count} exceeds limit of {_maxSymbolCount}",
                ResourceLimitType.SymbolCount);
        }
    }
    
    /// <summary>
    /// Tracks nesting depth for recursive visitors.
    /// </summary>
    public IDisposable EnterNestingLevel()
    {
        int depth = Interlocked.Increment(ref _currentNestingDepth);
        if (depth > _maxNestingDepth)
        {
            Interlocked.Decrement(ref _currentNestingDepth);
            throw new ResourceLimitExceededException(
                $"Nesting depth {depth} exceeds limit of {_maxNestingDepth}",
                ResourceLimitType.NestingDepth);
        }
        
        return new NestingLevelToken(this);
    }
    
    private void ExitNestingLevel()
    {
        Interlocked.Decrement(ref _currentNestingDepth);
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _timeoutCts.Dispose();
            _disposed = true;
        }
    }
    
    private sealed class NestingLevelToken : IDisposable
    {
        private readonly ResourceLimitGuard _guard;
        private bool _disposed;
        
        public NestingLevelToken(ResourceLimitGuard guard) => _guard = guard;
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _guard.ExitNestingLevel();
                _disposed = true;
            }
        }
    }
}

public record ResourceLimits
{
    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024; // 10 MB
    public int MaxSymbolCount { get; init; } = 50_000;
    public int MaxNestingDepth { get; init; } = 100;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
}

public enum ResourceLimitType
{
    FileSize,
    SymbolCount,
    NestingDepth,
    Timeout
}

public class ResourceLimitExceededException : Exception
{
    public ResourceLimitType LimitType { get; }
    
    public ResourceLimitExceededException(string message, ResourceLimitType limitType) 
        : base(message)
    {
        LimitType = limitType;
    }
}
```

---

### Threat 3: Information Disclosure via Error Messages

**Threat ID:** THREAT-017a-003  
**Severity:** MEDIUM  
**Attack Vector:** Detailed error messages reveal internal paths, system info, or code structure  
**Impact:** Information useful for further attacks, privacy violations

**Mitigation - Complete C# Implementation:**

```csharp
using System;
using Microsoft.Extensions.Logging;

namespace AgenticCoder.Infrastructure.Symbols.CSharp.Security;

/// <summary>
/// Sanitizes error messages to prevent information disclosure.
/// </summary>
public class SecureErrorHandler
{
    private readonly ILogger<SecureErrorHandler> _logger;
    private readonly string _workspaceRoot;
    
    public SecureErrorHandler(ILogger<SecureErrorHandler> logger, string workspaceRoot)
    {
        _logger = logger;
        _workspaceRoot = workspaceRoot;
    }
    
    /// <summary>
    /// Creates a safe error result for external consumption.
    /// </summary>
    public ExtractionError CreateSafeError(Exception exception, string filePath)
    {
        // Log full details internally
        _logger.LogError(exception, 
            "Extraction error for file {FilePath}: {Message}", 
            filePath, 
            exception.Message);
        
        // Return sanitized error externally
        string safeMessage = exception switch
        {
            FileNotFoundException => "File not found",
            UnauthorizedAccessException => "Access denied",
            ResourceLimitExceededException rle => $"Resource limit exceeded: {rle.LimitType}",
            SecurityException => "Security validation failed",
            OperationCanceledException => "Operation cancelled",
            _ => "Extraction failed"
        };
        
        string errorCode = exception switch
        {
            FileNotFoundException => "ACODE-CSE-002",
            UnauthorizedAccessException => "ACODE-CSE-005",
            ResourceLimitExceededException => "ACODE-CSE-006",
            SecurityException => "ACODE-CSE-007",
            OperationCanceledException => "ACODE-CSE-008",
            _ => "ACODE-CSE-004"
        };
        
        // Sanitize file path - only show relative path from workspace
        string safeFilePath = SanitizeFilePath(filePath);
        
        return new ExtractionError
        {
            Code = errorCode,
            Message = safeMessage,
            FilePath = safeFilePath,
            // Never include stack trace or inner exception in external response
        };
    }
    
    /// <summary>
    /// Converts absolute path to relative path from workspace root.
    /// </summary>
    private string SanitizeFilePath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return "[unknown]";
        
        try
        {
            string fullPath = Path.GetFullPath(filePath);
            string fullRoot = Path.GetFullPath(_workspaceRoot);
            
            if (fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            {
                return fullPath.Substring(fullRoot.Length).TrimStart(Path.DirectorySeparatorChar);
            }
            
            // Path outside workspace - show only filename
            return Path.GetFileName(filePath);
        }
        catch
        {
            return "[invalid path]";
        }
    }
}

public record ExtractionError
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public required string FilePath { get; init; }
}
```

---

### Threat 4: Code Execution via Roslyn Compilation

**Threat ID:** THREAT-017a-004  
**Severity:** CRITICAL  
**Attack Vector:** Attacker expects Roslyn to compile/execute code, or exploits code evaluation features  
**Impact:** Arbitrary code execution on the server

**Mitigation - Complete C# Implementation:**

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AgenticCoder.Infrastructure.Symbols.CSharp.Security;

/// <summary>
/// Ensures Roslyn is used in parse-only mode without code execution capability.
/// </summary>
public static class RoslynSecurityPolicy
{
    /// <summary>
    /// Creates a secure Roslyn workspace that cannot execute code.
    /// </summary>
    public static CSharpParseOptions GetSecureParseOptions()
    {
        return new CSharpParseOptions(
            languageVersion: LanguageVersion.Latest,
            documentationMode: DocumentationMode.Parse,
            kind: SourceCodeKind.Regular
        );
    }
    
    /// <summary>
    /// Creates compilation options that prevent code generation.
    /// </summary>
    public static CSharpCompilationOptions GetSecureCompilationOptions()
    {
        return new CSharpCompilationOptions(
            outputKind: OutputKind.DynamicallyLinkedLibrary,
            // Disable all optimizations - we don't run code
            optimizationLevel: OptimizationLevel.Debug,
            // No strong naming
            cryptoKeyFile: null,
            // Disable deterministic for security (no reproducible build needed)
            deterministic: false
        )
        // Explicitly disable any features that could enable execution
        .WithAllowUnsafe(false);
    }
    
    /// <summary>
    /// Validates that we never call Emit or similar code-producing methods.
    /// This is a compile-time assertion via code review.
    /// </summary>
    /// <remarks>
    /// SECURITY INVARIANT:
    /// The following Roslyn methods MUST NEVER be called:
    /// - Compilation.Emit()
    /// - Compilation.EmitToArray()
    /// - CSharpScript.Create()
    /// - CSharpScript.RunAsync()
    /// - CSharpScript.EvaluateAsync()
    /// - Any method that produces executable output
    /// 
    /// Code review MUST verify this invariant is maintained.
    /// </remarks>
    public static class SecurityInvariants
    {
        public const string NoEmitPolicy = 
            "NEVER call Compilation.Emit() or any code generation method";
        
        public const string NoScriptingPolicy = 
            "NEVER use CSharpScript or any scripting API";
        
        public const string ParseOnlyPolicy = 
            "ONLY use SyntaxTree and SemanticModel for analysis";
    }
}

/// <summary>
/// Safe wrapper around Roslyn that enforces parse-only usage.
/// </summary>
public class SecureRoslynParser
{
    private readonly CSharpParseOptions _parseOptions;
    
    public SecureRoslynParser()
    {
        _parseOptions = RoslynSecurityPolicy.GetSecureParseOptions();
    }
    
    /// <summary>
    /// Parses source code into a syntax tree. NO CODE IS EXECUTED.
    /// </summary>
    public SyntaxTree ParseSafe(string sourceCode, string filePath)
    {
        // SyntaxTree.ParseText ONLY parses - it does not execute
        return CSharpSyntaxTree.ParseText(
            sourceCode,
            _parseOptions,
            filePath,
            encoding: System.Text.Encoding.UTF8);
    }
    
    // Note: No Emit, no Compile, no Execute methods exist on this class
    // This is intentional - we are a PARSER only
}
```

---

### Threat 5: Resource Exhaustion via Malformed Syntax Trees

**Threat ID:** THREAT-017a-005  
**Severity:** HIGH  
**Attack Vector:** Maliciously crafted C# source files with deeply nested expressions, extremely long lines, or recursive macro-like patterns that cause exponential memory consumption or stack overflow during AST traversal  
**Impact:** Denial of service, agent crash, memory exhaustion affecting other operations

**Mitigation - Complete C# Implementation:**

```csharp
using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Symbols.CSharp.Security;

/// <summary>
/// Enforces resource limits during syntax tree traversal to prevent
/// denial of service via malformed or malicious source files.
/// </summary>
public sealed class ResourceBoundedVisitor : CSharpSyntaxWalker
{
    private readonly ILogger _logger;
    private readonly CancellationToken _cancellationToken;
    private readonly ResourceLimits _limits;
    
    private int _currentDepth;
    private int _nodesVisited;
    private long _startMemory;
    private DateTime _startTime;
    
    public ResourceBoundedVisitor(
        ILogger logger,
        ResourceLimits limits,
        CancellationToken cancellationToken)
        : base(SyntaxWalkerDepth.Node)
    {
        _logger = logger;
        _limits = limits;
        _cancellationToken = cancellationToken;
    }
    
    /// <summary>
    /// Initializes resource tracking before visiting begins.
    /// </summary>
    public void BeginVisit()
    {
        _currentDepth = 0;
        _nodesVisited = 0;
        _startMemory = GC.GetTotalMemory(false);
        _startTime = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Overrides base visit to enforce resource limits at every node.
    /// </summary>
    public override void Visit(SyntaxNode? node)
    {
        if (node == null) return;
        
        // Check cancellation
        _cancellationToken.ThrowIfCancellationRequested();
        
        // Check node count limit
        _nodesVisited++;
        if (_nodesVisited > _limits.MaxNodesPerFile)
        {
            _logger.LogWarning(
                "Node limit exceeded ({Count} > {Max}), aborting traversal",
                _nodesVisited, _limits.MaxNodesPerFile);
            throw new ResourceLimitExceededException(
                "ACODE-CSE-010",
                $"File exceeds maximum node count ({_limits.MaxNodesPerFile})");
        }
        
        // Check depth limit
        _currentDepth++;
        if (_currentDepth > _limits.MaxNestingDepth)
        {
            _logger.LogWarning(
                "Depth limit exceeded ({Depth} > {Max}) at {Location}",
                _currentDepth, _limits.MaxNestingDepth, node.GetLocation());
            throw new ResourceLimitExceededException(
                "ACODE-CSE-011",
                $"File exceeds maximum nesting depth ({_limits.MaxNestingDepth})");
        }
        
        // Check time limit
        var elapsed = DateTime.UtcNow - _startTime;
        if (elapsed > _limits.MaxTraversalTime)
        {
            _logger.LogWarning(
                "Time limit exceeded ({Elapsed} > {Max})",
                elapsed, _limits.MaxTraversalTime);
            throw new ResourceLimitExceededException(
                "ACODE-CSE-012",
                $"File traversal exceeded time limit ({_limits.MaxTraversalTime.TotalSeconds}s)");
        }
        
        // Check memory limit (sample every 1000 nodes to reduce overhead)
        if (_nodesVisited % 1000 == 0)
        {
            var currentMemory = GC.GetTotalMemory(false);
            var memoryUsed = currentMemory - _startMemory;
            if (memoryUsed > _limits.MaxMemoryBytes)
            {
                _logger.LogWarning(
                    "Memory limit exceeded ({Used}MB > {Max}MB)",
                    memoryUsed / (1024 * 1024), _limits.MaxMemoryBytes / (1024 * 1024));
                throw new ResourceLimitExceededException(
                    "ACODE-CSE-013",
                    $"File traversal exceeded memory limit ({_limits.MaxMemoryBytes / (1024 * 1024)}MB)");
            }
        }
        
        try
        {
            base.Visit(node);
        }
        finally
        {
            _currentDepth--;
        }
    }
    
    /// <summary>
    /// Returns traversal statistics for logging and diagnostics.
    /// </summary>
    public TraversalStats GetStats()
    {
        return new TraversalStats
        {
            NodesVisited = _nodesVisited,
            MaxDepthReached = _currentDepth,
            Duration = DateTime.UtcNow - _startTime,
            MemoryUsed = GC.GetTotalMemory(false) - _startMemory
        };
    }
}

/// <summary>
/// Configurable resource limits for syntax tree traversal.
/// </summary>
public record ResourceLimits
{
    public int MaxNodesPerFile { get; init; } = 100_000;
    public int MaxNestingDepth { get; init; } = 100;
    public TimeSpan MaxTraversalTime { get; init; } = TimeSpan.FromSeconds(30);
    public long MaxMemoryBytes { get; init; } = 100 * 1024 * 1024; // 100 MB
    
    public static ResourceLimits Default => new();
    
    public static ResourceLimits Strict => new()
    {
        MaxNodesPerFile = 50_000,
        MaxNestingDepth = 50,
        MaxTraversalTime = TimeSpan.FromSeconds(10),
        MaxMemoryBytes = 50 * 1024 * 1024
    };
}

/// <summary>
/// Statistics from a bounded traversal operation.
/// </summary>
public record TraversalStats
{
    public int NodesVisited { get; init; }
    public int MaxDepthReached { get; init; }
    public TimeSpan Duration { get; init; }
    public long MemoryUsed { get; init; }
}

/// <summary>
/// Thrown when a resource limit is exceeded during traversal.
/// </summary>
public class ResourceLimitExceededException : Exception
{
    public string ErrorCode { get; }
    
    public ResourceLimitExceededException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
```

---

## Testing Requirements

### Unit Tests - Complete Implementation

#### CSharpSymbolExtractorTests.cs

```csharp
using Acode.Application.Interfaces;
using Acode.Domain.Entities;
using Acode.Domain.Enums;
using Acode.Infrastructure.Symbols.CSharp;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Acode.Infrastructure.Tests.Symbols.CSharp;

public class CSharpSymbolExtractorTests
{
    private readonly CSharpSymbolExtractor _sut;
    private readonly CSharpExtractorOptions _options;

    public CSharpSymbolExtractorTests()
    {
        _options = new CSharpExtractorOptions
        {
            IncludePrivateMembers = false,
            ExtractDocumentation = true
        };
        
        _sut = new CSharpSymbolExtractor(
            Options.Create(_options),
            new NullLogger<CSharpSymbolExtractor>());
    }

    [Fact]
    public async Task ExtractAsync_WithClass_ReturnsClassSymbol()
    {
        // Arrange
        var source = @"
namespace TestNamespace;

public class UserService
{
    public string Name { get; set; }
}";
        var filePath = CreateTempFile(source);

        // Act
        var result = await _sut.ExtractAsync(filePath, default);

        // Assert
        result.Should().NotBeNull();
        result.Symbols.Should().HaveCount(2); // class + property
        
        var classSymbol = result.Symbols.First(s => s.Kind == SymbolKind.Class);
        classSymbol.Name.Should().Be("UserService");
        classSymbol.FullyQualifiedName.Should().Be("TestNamespace.UserService");
        classSymbol.IsExported.Should().BeTrue();
    }

    [Fact]
    public async Task ExtractAsync_WithAbstractClass_CapturesAbstractModifier()
    {
        // Arrange
        var source = @"
public abstract class BaseService
{
    public abstract void Execute();
}";
        var filePath = CreateTempFile(source);

        // Act
        var result = await _sut.ExtractAsync(filePath, default);

        // Assert
        var classSymbol = result.Symbols.First(s => s.Kind == SymbolKind.Class);
        classSymbol.Modifiers.Should().Contain(SymbolModifier.Abstract);
    }

    [Fact]
    public async Task ExtractAsync_WithInterface_ReturnsInterfaceSymbol()
    {
        // Arrange
        var source = @"
public interface IUserRepository
{
    Task<User> GetByIdAsync(Guid id);
    Task SaveAsync(User user);
}";
        var filePath = CreateTempFile(source);

        // Act
        var result = await _sut.ExtractAsync(filePath, default);

        // Assert
        result.Symbols.Should().ContainSingle(s => s.Kind == SymbolKind.Interface);
        
        var interfaceSymbol = result.Symbols.First(s => s.Kind == SymbolKind.Interface);
        interfaceSymbol.Name.Should().Be("IUserRepository");
        interfaceSymbol.Children.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExtractAsync_WithMethod_CapturesSignature()
    {
        // Arrange
        var source = @"
public class Calculator
{
    public async Task<decimal> CalculateAsync(int value, decimal rate)
    {
        return value * rate;
    }
}";
        var filePath = CreateTempFile(source);

        // Act
        var result = await _sut.ExtractAsync(filePath, default);

        // Assert
        var methodSymbol = result.Symbols.First(s => s.Kind == SymbolKind.Method);
        methodSymbol.Name.Should().Be("CalculateAsync");
        methodSymbol.Signature.Should().Contain("int value");
        methodSymbol.Signature.Should().Contain("decimal rate");
        methodSymbol.Signature.Should().Contain("Task<decimal>");
        methodSymbol.Modifiers.Should().Contain(SymbolModifier.Async);
    }

    [Fact]
    public async Task ExtractAsync_WithXmlDoc_ExtractsDocumentation()
    {
        // Arrange
        var source = @"
/// <summary>
/// Validates user input data.
/// </summary>
/// <param name=""input"">The input to validate</param>
/// <returns>True if valid, false otherwise</returns>
public bool Validate(string input) => !string.IsNullOrEmpty(input);";
        var filePath = CreateTempFile(source);

        // Act
        var result = await _sut.ExtractAsync(filePath, default);

        // Assert
        var methodSymbol = result.Symbols.First(s => s.Kind == SymbolKind.Method);
        methodSymbol.Documentation.Should().NotBeNull();
        methodSymbol.Documentation!.Summary.Should().Contain("Validates user input");
        methodSymbol.Documentation.Parameters.Should().ContainKey("input");
        methodSymbol.Documentation.Returns.Should().Contain("True if valid");
    }

    [Fact]
    public async Task ExtractAsync_WithNestedClass_CapturesContainment()
    {
        // Arrange
        var source = @"
public class Outer
{
    public class Inner
    {
        public void InnerMethod() { }
    }
}";
        var filePath = CreateTempFile(source);

        // Act
        var result = await _sut.ExtractAsync(filePath, default);

        // Assert
        var outerClass = result.Symbols.First(s => s.Name == "Outer");
        outerClass.Children.Should().ContainSingle(s => s.Name == "Inner");
        
        var innerClass = outerClass.Children.First();
        innerClass.FullyQualifiedName.Should().Be("Outer.Inner");
    }

    [Fact]
    public async Task ExtractAsync_WithSyntaxError_ReturnsPartialResults()
    {
        // Arrange
        var source = @"
public class ValidClass
{
    public void ValidMethod() { }
}

public class InvalidClass
{
    // Missing closing brace";
        var filePath = CreateTempFile(source);

        // Act
        var result = await _sut.ExtractAsync(filePath, default);

        // Assert
        result.IsPartial.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
        result.Symbols.Should().ContainSingle(s => s.Name == "ValidClass");
    }

    [Fact]
    public async Task ExtractAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var source = @"public class Test { }";
        var filePath = CreateTempFile(source);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.ExtractAsync(filePath, cts.Token));
    }

    [Fact]
    public async Task ExtractAsync_WithGenericClass_CapturesTypeParameters()
    {
        // Arrange
        var source = @"
public class Repository<TEntity, TKey> where TEntity : class where TKey : struct
{
    public TEntity GetById(TKey id) => default!;
}";
        var filePath = CreateTempFile(source);

        // Act
        var result = await _sut.ExtractAsync(filePath, default);

        // Assert
        var classSymbol = result.Symbols.First(s => s.Kind == SymbolKind.Class);
        classSymbol.TypeParameters.Should().HaveCount(2);
        classSymbol.TypeParameters.Should().Contain("TEntity");
        classSymbol.TypeParameters.Should().Contain("TKey");
    }

    [Fact]
    public async Task ExtractAsync_WithRecord_ReturnsRecordSymbol()
    {
        // Arrange
        var source = @"
public record Person(string FirstName, string LastName);";
        var filePath = CreateTempFile(source);

        // Act
        var result = await _sut.ExtractAsync(filePath, default);

        // Assert
        var recordSymbol = result.Symbols.First(s => s.Kind == SymbolKind.Record);
        recordSymbol.Name.Should().Be("Person");
    }

    [Fact]
    public async Task ExtractAsync_WithEnum_CapturesMembers()
    {
        // Arrange
        var source = @"
public enum Status
{
    Pending = 0,
    Active = 1,
    Completed = 2
}";
        var filePath = CreateTempFile(source);

        // Act
        var result = await _sut.ExtractAsync(filePath, default);

        // Assert
        var enumSymbol = result.Symbols.First(s => s.Kind == SymbolKind.Enum);
        enumSymbol.Name.Should().Be("Status");
        enumSymbol.Children.Should().HaveCount(3);
    }

    private static string CreateTempFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.cs");
        File.WriteAllText(path, content);
        return path;
    }
}
```

---

#### RoslynParserTests.cs

```csharp
using Acode.Infrastructure.Symbols.CSharp;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Acode.Infrastructure.Tests.Symbols.CSharp;

public class RoslynParserTests
{
    private readonly RoslynParser _sut;

    public RoslynParserTests()
    {
        _sut = new RoslynParser(new NullLogger<RoslynParser>());
    }

    [Fact]
    public async Task ParseAsync_WithValidSource_ReturnsSyntaxTree()
    {
        // Arrange
        var source = "public class Test { }";
        var filePath = CreateTempFile(source);

        // Act
        var result = await _sut.ParseAsync(filePath, default);

        // Assert
        result.Should().NotBeNull();
        result.SyntaxTree.Should().NotBeNull();
        result.SyntaxTree.GetRoot().DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .Should().ContainSingle();
    }

    [Fact]
    public async Task ParseAsync_WithSyntaxErrors_ReturnsDiagnostics()
    {
        // Arrange
        var source = "public class Test { ";  // Missing closing brace
        var filePath = CreateTempFile(source);

        // Act
        var result = await _sut.ParseAsync(filePath, default);

        // Assert
        result.Diagnostics.Should().NotBeEmpty();
        result.Diagnostics.Should().Contain(d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task ParseAsync_WithPreprocessorDirectives_HandlesCorrectly()
    {
        // Arrange
        var source = @"
#if DEBUG
public class DebugOnly { }
#endif

public class AlwaysPresent { }";
        var filePath = CreateTempFile(source);

        // Act
        var result = await _sut.ParseAsync(filePath, default);

        // Assert
        result.SyntaxTree.Should().NotBeNull();
        // Both classes are in syntax tree (preprocessor not evaluated)
        var classes = result.SyntaxTree.GetRoot().DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>();
        classes.Should().HaveCount(2);
    }

    [Fact]
    public async Task ParseAsync_WithNullableAnnotations_HandlesCorrectly()
    {
        // Arrange
        var source = @"
#nullable enable
public class NullableTest
{
    public string? NullableProperty { get; set; }
    public string NonNullableProperty { get; set; } = """";
}";
        var filePath = CreateTempFile(source);

        // Act
        var result = await _sut.ParseAsync(filePath, default);

        // Assert
        result.SyntaxTree.Should().NotBeNull();
        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty();
    }

    private static string CreateTempFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.cs");
        File.WriteAllText(path, content);
        return path;
    }
}
```

---

#### XmlDocExtractorTests.cs

```csharp
using Acode.Infrastructure.Symbols.CSharp;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Acode.Infrastructure.Tests.Symbols.CSharp;

public class XmlDocExtractorTests
{
    private readonly XmlDocExtractor _sut = new();

    [Fact]
    public void Extract_WithSummary_ExtractsSummaryText()
    {
        // Arrange
        var source = @"
/// <summary>
/// This is the summary text.
/// </summary>
public void TestMethod() { }";
        var methodNode = ParseMethod(source);

        // Act
        var result = _sut.Extract(methodNode);

        // Assert
        result.Should().NotBeNull();
        result!.Summary.Should().Be("This is the summary text.");
    }

    [Fact]
    public void Extract_WithParams_ExtractsParameterDescriptions()
    {
        // Arrange
        var source = @"
/// <summary>Does something</summary>
/// <param name=""id"">The unique identifier</param>
/// <param name=""name"">The display name</param>
public void TestMethod(int id, string name) { }";
        var methodNode = ParseMethod(source);

        // Act
        var result = _sut.Extract(methodNode);

        // Assert
        result.Should().NotBeNull();
        result!.Parameters.Should().HaveCount(2);
        result.Parameters["id"].Should().Be("The unique identifier");
        result.Parameters["name"].Should().Be("The display name");
    }

    [Fact]
    public void Extract_WithReturns_ExtractsReturnDescription()
    {
        // Arrange
        var source = @"
/// <summary>Gets a value</summary>
/// <returns>The calculated value</returns>
public int TestMethod() => 42;";
        var methodNode = ParseMethod(source);

        // Act
        var result = _sut.Extract(methodNode);

        // Assert
        result.Should().NotBeNull();
        result!.Returns.Should().Be("The calculated value");
    }

    [Fact]
    public void Extract_WithMalformedXml_ReturnsNull()
    {
        // Arrange
        var source = @"
/// <summary>
/// Unclosed tag <param name=""x"">
public void TestMethod() { }";
        var methodNode = ParseMethod(source);

        // Act
        var result = _sut.Extract(methodNode);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Extract_WithNoDocumentation_ReturnsNull()
    {
        // Arrange
        var source = @"public void TestMethod() { }";
        var methodNode = ParseMethod(source);

        // Act
        var result = _sut.Extract(methodNode);

        // Assert
        result.Should().BeNull();
    }

    private static MethodDeclarationSyntax ParseMethod(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        return tree.GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .First();
    }
}
```

---

### Integration Tests

#### CSharpExtractorIntegrationTests.cs

```csharp
using Acode.Infrastructure.Symbols.CSharp;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Acode.Integration.Tests.Symbols.CSharp;

public class CSharpExtractorIntegrationTests : IDisposable
{
    private readonly CSharpSymbolExtractor _sut;
    private readonly string _testDirectory;

    public CSharpExtractorIntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"csharp-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        _sut = new CSharpSymbolExtractor(
            Options.Create(new CSharpExtractorOptions()),
            new NullLogger<CSharpSymbolExtractor>());
    }

    [Fact]
    public async Task ExtractAsync_WithRealProjectStructure_ExtractsAllSymbols()
    {
        // Arrange
        CreateFile("Models/User.cs", @"
namespace TestProject.Models;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}");

        CreateFile("Services/UserService.cs", @"
using TestProject.Models;

namespace TestProject.Services;

public class UserService
{
    public User GetUser(Guid id) => new User { Id = id };
}");

        var files = Directory.GetFiles(_testDirectory, "*.cs", SearchOption.AllDirectories);

        // Act
        var results = new List<SymbolExtractionResult>();
        foreach (var file in files)
        {
            results.Add(await _sut.ExtractAsync(file, default));
        }

        // Assert
        var allSymbols = results.SelectMany(r => r.Symbols).ToList();
        allSymbols.Should().Contain(s => s.Name == "User");
        allSymbols.Should().Contain(s => s.Name == "UserService");
        allSymbols.Should().Contain(s => s.Name == "GetUser");
    }

    [Fact]
    public async Task ExtractAsync_WithLargeFile_CompletesWithinTimeout()
    {
        // Arrange - create a file with 1000 methods
        var methods = string.Join("\n", 
            Enumerable.Range(1, 1000)
                .Select(i => $"public void Method{i}() {{ }}"));
        var source = $"public class LargeClass {{\n{methods}\n}}";
        var filePath = CreateFile("Large.cs", source);

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await _sut.ExtractAsync(filePath, default);
        sw.Stop();

        // Assert
        result.Symbols.Should().HaveCountGreaterThan(1000);
        sw.ElapsedMilliseconds.Should().BeLessThan(5000); // 5 second max
    }

    private string CreateFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_testDirectory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }
}
```

---

### E2E Tests

#### CSharpSymbolE2ETests.cs

```csharp
using System.Diagnostics;
using FluentAssertions;
using Xunit;

namespace Acode.E2E.Tests.Symbols.CSharp;

public class CSharpSymbolE2ETests : IDisposable
{
    private readonly string _testDirectory;

    public CSharpSymbolE2ETests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"e2e-csharp-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public async Task CLI_ExtractSymbols_OutputsSymbolList()
    {
        // Arrange
        var sourceFile = Path.Combine(_testDirectory, "Test.cs");
        await File.WriteAllTextAsync(sourceFile, @"
namespace E2ETest;

public class TestClass
{
    public void TestMethod() { }
}");

        // Act
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "acode",
            Arguments = $"symbols extract \"{sourceFile}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false
        });
        
        var output = await process!.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Assert
        process.ExitCode.Should().Be(0);
        output.Should().Contain("TestClass");
        output.Should().Contain("TestMethod");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }
}
```

---

### Performance Benchmarks

| Benchmark | Target | Maximum | Actual (P95) |
|-----------|--------|---------|--------------|
| Parse single file (100 lines) | 30ms | 50ms | TBD |
| Parse single file (1000 lines) | 100ms | 200ms | TBD |
| Extract single file (100 symbols) | 60ms | 100ms | TBD |
| Extract 100 files sequentially | 5s | 10s | TBD |
| Extract 100 files parallel | 2s | 5s | TBD |
| Memory per 1000 symbols | 10MB | 50MB | TBD |

---

## User Verification Steps

### Scenario 1: Extract Class with Nested Members

**Setup:**
```csharp
// Create test file: TestService.cs
namespace MyApp.Services;

/// <summary>
/// Handles order processing.
/// </summary>
public class OrderService
{
    private readonly ILogger _logger;
    
    public OrderService(ILogger logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Processes an order.
    /// </summary>
    /// <param name="orderId">The order identifier.</param>
    /// <returns>True if successful.</returns>
    public async Task<bool> ProcessAsync(int orderId)
    {
        return true;
    }
}
```

**Steps:**
1. Run: `acode symbols extract TestService.cs`
2. Observe output listing symbols

**Expected Output:**
```
Extracting symbols from TestService.cs...

Symbols extracted: 4
  Class: OrderService (public)
    Location: lines 6-23
    Documentation: "Handles order processing."
  
  Field: _logger (private readonly)
    Location: line 8
    Type: ILogger
  
  Constructor: OrderService (public)
    Location: lines 10-13
    Signature: OrderService(ILogger logger)
  
  Method: ProcessAsync (public async)
    Location: lines 15-22
    Signature: Task<bool> ProcessAsync(int orderId)
    Documentation: "Processes an order."
```

**Verification Checklist:**
- [ ] Class symbol has correct name and visibility
- [ ] Field symbol shows private readonly modifiers
- [ ] Constructor extracted with parameter
- [ ] Method shows async modifier and return type
- [ ] XML documentation extracted for class and method

---

### Scenario 2: Extract Interface with Methods

**Setup:**
```csharp
// Create test file: IRepository.cs
namespace MyApp.Data;

/// <summary>
/// Generic repository interface.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<T> CreateAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
```

**Steps:**
1. Run: `acode symbols extract IRepository.cs --include-docs`
2. Verify interface and all methods extracted

**Expected Output:**
```
Extracting symbols from IRepository.cs...

Symbols extracted: 6
  Interface: IRepository<T> (public)
    Location: lines 7-14
    Generic: T where T : class
    Documentation: "Generic repository interface."
  
  Method: GetByIdAsync
    Signature: Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
  
  Method: GetAllAsync
    Signature: Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
  
  Method: CreateAsync
    Signature: Task<T> CreateAsync(T entity, CancellationToken ct = default)
  
  Method: UpdateAsync
    Signature: Task UpdateAsync(T entity, CancellationToken ct = default)
  
  Method: DeleteAsync
    Signature: Task DeleteAsync(int id, CancellationToken ct = default)
```

**Verification Checklist:**
- [ ] Interface generic parameter captured with constraint
- [ ] All 5 methods extracted
- [ ] Default parameter values shown in signatures
- [ ] Nullable return type (T?) shown correctly

---

### Scenario 3: Extract Enum with Values

**Setup:**
```csharp
// Create test file: OrderStatus.cs
namespace MyApp.Models;

/// <summary>
/// Status of an order in the fulfillment pipeline.
/// </summary>
public enum OrderStatus
{
    /// <summary>New order, not yet processed.</summary>
    Pending = 0,
    
    /// <summary>Order confirmed by customer.</summary>
    Confirmed = 1,
    
    /// <summary>Order shipped to customer.</summary>
    Shipped = 10,
    
    /// <summary>Order delivered successfully.</summary>
    Delivered = 20,
    
    /// <summary>Order cancelled.</summary>
    Cancelled = -1
}
```

**Steps:**
1. Run: `acode symbols extract OrderStatus.cs --include-docs`
2. Verify enum and all values extracted

**Expected Output:**
```
Extracting symbols from OrderStatus.cs...

Symbols extracted: 6
  Enum: OrderStatus (public)
    Location: lines 6-23
    Documentation: "Status of an order in the fulfillment pipeline."
  
  EnumMember: Pending = 0
    Documentation: "New order, not yet processed."
  
  EnumMember: Confirmed = 1
    Documentation: "Order confirmed by customer."
  
  EnumMember: Shipped = 10
    Documentation: "Order shipped to customer."
  
  EnumMember: Delivered = 20
    Documentation: "Order delivered successfully."
  
  EnumMember: Cancelled = -1
    Documentation: "Order cancelled."
```

**Verification Checklist:**
- [ ] Enum symbol extracted
- [ ] All enum members extracted with explicit values
- [ ] Documentation for each member captured

---

### Scenario 4: Handle Syntax Errors Gracefully

**Setup:**
```csharp
// Create test file: Broken.cs with intentional syntax error
namespace MyApp;

public class ValidClass
{
    public void ValidMethod() { }
}

public class BrokenClass
{
    public void Broken(  // Missing closing paren and body
```

**Steps:**
1. Run: `acode symbols extract Broken.cs`
2. Verify partial extraction succeeds

**Expected Output:**
```
Extracting symbols from Broken.cs...

⚠ Parse warning: Unexpected end of file (line 11)

Symbols extracted: 2 (partial - syntax errors present)
  Class: ValidClass (public)
    Location: lines 3-6
  
  Method: ValidMethod (public)
    Location: line 5
    Signature: void ValidMethod()

Errors:
  ACODE-CSE-003: Syntax error at line 11 - extraction continued with partial results
```

**Verification Checklist:**
- [ ] Valid symbols before error are extracted
- [ ] Error logged with line number
- [ ] Extraction does not crash
- [ ] Partial results flag indicates incomplete extraction

---

### Scenario 5: Semantic Analysis with Type Resolution

**Setup:**
```csharp
// Create test file with project context: UserService.cs
// Requires: csproj with proper references

namespace MyApp.Services;

public class UserService
{
    private readonly ApplicationDbContext _dbContext;
    
    public async Task<User> GetUserAsync(int id)
    {
        return await _dbContext.Users.FindAsync(id);
    }
}
```

**Steps:**
1. Run: `acode symbols extract UserService.cs --semantic --project MyApp.csproj`
2. Verify full type information resolved

**Expected Output:**
```
Extracting symbols from UserService.cs (semantic mode)...

Loading project: MyApp.csproj
Resolving references...

Symbols extracted: 3
  Class: UserService (public)
    Location: lines 5-14
    Namespace: MyApp.Services
  
  Field: _dbContext (private readonly)
    Type: MyApp.Data.ApplicationDbContext (resolved)
  
  Method: GetUserAsync (public async)
    Signature: Task<User> GetUserAsync(int id)
    Return Type: System.Threading.Tasks.Task<MyApp.Models.User> (resolved)
```

**Verification Checklist:**
- [ ] Types fully qualified with namespace
- [ ] References resolved from project context
- [ ] Semantic information richer than syntax-only

---

### Scenario 6: Large File Performance

**Setup:**
- Use a large C# file (>2000 lines, ~100 symbols)
- Example: A controller or service with many methods

**Steps:**
1. Run: `acode symbols extract LargeController.cs --stats`
2. Verify extraction completes within performance targets

**Expected Output:**
```
Extracting symbols from LargeController.cs...

Symbols extracted: 127
  Classes: 3
  Methods: 95
  Properties: 24
  Fields: 5

Statistics:
  File size: 78.5 KB
  Lines: 2,341
  Parse time: 45 ms
  Extract time: 82 ms
  Total time: 127 ms
  Memory used: 12.3 MB

✓ Performance targets met (<100ms for <1000 lines)
```

**Verification Checklist:**
- [ ] All symbols extracted correctly
- [ ] Total time < 500ms for large file
- [ ] Memory usage bounded
- [ ] Statistics accurately reported

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Infrastructure/
├── Symbols/
│   └── CSharp/
│       ├── CSharpSymbolExtractor.cs      # Main extractor implementing ISymbolExtractor
│       ├── RoslynParser.cs               # Roslyn workspace and SyntaxTree creation
│       ├── SymbolVisitor.cs              # CSharpSyntaxWalker for symbol collection
│       ├── XmlDocExtractor.cs            # XML documentation comment parser
│       ├── SignatureFormatter.cs         # Human-readable signature generation
│       └── Security/
│           ├── PathSecurityValidator.cs  # Path traversal prevention
│           └── ResourceLimitGuard.cs     # DoS protection
```

### Complete CSharpSymbolExtractor Implementation

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using AgenticCoder.Application.Interfaces;
using AgenticCoder.Domain.Symbols;
using AgenticCoder.Infrastructure.Symbols.CSharp.Security;

namespace AgenticCoder.Infrastructure.Symbols.CSharp;

/// <summary>
/// Extracts symbols from C# source files using Microsoft Roslyn.
/// Implements ISymbolExtractor for registration in the ExtractorRegistry.
/// </summary>
public class CSharpSymbolExtractor : ISymbolExtractor
{
    private readonly ILogger<CSharpSymbolExtractor> _logger;
    private readonly RoslynParser _parser;
    private readonly XmlDocExtractor _docExtractor;
    private readonly SignatureFormatter _signatureFormatter;
    private readonly SecureErrorHandler _errorHandler;
    private readonly string _workspaceRoot;
    
    public CSharpSymbolExtractor(
        ILogger<CSharpSymbolExtractor> logger,
        RoslynParser parser,
        XmlDocExtractor docExtractor,
        SignatureFormatter signatureFormatter,
        SecureErrorHandler errorHandler,
        string workspaceRoot)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _docExtractor = docExtractor ?? throw new ArgumentNullException(nameof(docExtractor));
        _signatureFormatter = signatureFormatter ?? throw new ArgumentNullException(nameof(signatureFormatter));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _workspaceRoot = workspaceRoot ?? throw new ArgumentNullException(nameof(workspaceRoot));
    }
    
    /// <inheritdoc />
    public string Language => "csharp";
    
    /// <inheritdoc />
    public string[] FileExtensions => new[] { ".cs" };
    
    /// <inheritdoc />
    public async Task<ExtractionResult> ExtractAsync(
        string filePath, 
        ExtractionOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        options ??= ExtractionOptions.Default;
        
        var startTime = DateTime.UtcNow;
        var symbols = new List<ISymbol>();
        var errors = new List<ExtractionError>();
        
        try
        {
            // Security: Validate file path
            PathSecurityValidator.ValidatePath(filePath, _workspaceRoot);
            
            // Security: Apply resource limits
            using var resourceGuard = new ResourceLimitGuard(new ResourceLimits
            {
                MaxFileSizeBytes = options.MaxFileSizeBytes,
                MaxSymbolCount = options.MaxSymbolCount,
                Timeout = options.Timeout
            });
            
            resourceGuard.ValidateFileSize(filePath);
            
            // Create linked cancellation token with timeout
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                ct, resourceGuard.TimeoutToken);
            
            // Parse the file
            _logger.LogDebug("Parsing C# file: {FilePath}", filePath);
            var parseResult = await _parser.ParseAsync(filePath, options, linkedCts.Token);
            
            if (parseResult.HasErrors)
            {
                errors.AddRange(parseResult.Errors.Select(e => new ExtractionError
                {
                    Code = "ACODE-CSE-001",
                    Message = e.Message,
                    FilePath = filePath,
                    Line = e.Line
                }));
            }
            
            // Extract symbols using visitor
            var visitor = new SymbolVisitor(
                parseResult.SyntaxTree,
                parseResult.SemanticModel,
                _docExtractor,
                _signatureFormatter,
                resourceGuard,
                options);
            
            visitor.Visit(parseResult.SyntaxTree.GetRoot());
            
            symbols.AddRange(visitor.ExtractedSymbols);
            
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Extracted {Count} symbols from {FilePath} in {Duration}ms",
                symbols.Count, filePath, duration.TotalMilliseconds);
            
            return new ExtractionResult
            {
                FilePath = filePath,
                Symbols = symbols,
                Errors = errors,
                Statistics = new ExtractionStatistics
                {
                    SymbolCount = symbols.Count,
                    ParseTimeMs = parseResult.ParseTimeMs,
                    ExtractTimeMs = (int)(duration.TotalMilliseconds - parseResult.ParseTimeMs),
                    FileSize = new FileInfo(filePath).Length
                }
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Extraction cancelled for {FilePath}", filePath);
            throw;
        }
        catch (Exception ex)
        {
            var error = _errorHandler.CreateSafeError(ex, filePath);
            errors.Add(error);
            
            return new ExtractionResult
            {
                FilePath = filePath,
                Symbols = symbols,
                Errors = errors,
                IsPartial = true
            };
        }
    }
}
```

### Complete RoslynParser Implementation

```csharp
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace AgenticCoder.Infrastructure.Symbols.CSharp;

/// <summary>
/// Parses C# source files into Roslyn SyntaxTrees and optionally SemanticModels.
/// </summary>
public class RoslynParser
{
    private readonly CSharpParseOptions _parseOptions;
    
    public RoslynParser()
    {
        _parseOptions = new CSharpParseOptions(
            languageVersion: LanguageVersion.Latest,
            documentationMode: DocumentationMode.Parse,
            kind: SourceCodeKind.Regular);
    }
    
    /// <summary>
    /// Parses a C# file and returns the syntax tree and optional semantic model.
    /// </summary>
    public async Task<ParseResult> ParseAsync(
        string filePath,
        ExtractionOptions options,
        CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        
        // Read file content
        string sourceCode = await File.ReadAllTextAsync(filePath, ct);
        var sourceText = SourceText.From(sourceCode, System.Text.Encoding.UTF8);
        
        // Parse to syntax tree (NEVER executes code)
        var syntaxTree = CSharpSyntaxTree.ParseText(
            sourceText,
            _parseOptions,
            filePath);
        
        var parseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
        
        // Collect syntax diagnostics
        var diagnostics = syntaxTree.GetDiagnostics(ct);
        var errors = diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => new ParseError
            {
                Message = d.GetMessage(),
                Line = d.Location.GetLineSpan().StartLinePosition.Line + 1,
                Code = d.Id
            })
            .ToList();
        
        // Create semantic model if requested
        SemanticModel? semanticModel = null;
        if (options.UseSemanticAnalysis)
        {
            semanticModel = await CreateSemanticModelAsync(syntaxTree, options, ct);
        }
        
        return new ParseResult
        {
            SyntaxTree = syntaxTree,
            SemanticModel = semanticModel,
            Errors = errors,
            ParseTimeMs = parseTimeMs
        };
    }
    
    private async Task<SemanticModel?> CreateSemanticModelAsync(
        SyntaxTree syntaxTree,
        ExtractionOptions options,
        CancellationToken ct)
    {
        // For semantic analysis, we need a compilation
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
        };
        
        // Add references from project if available
        if (!string.IsNullOrEmpty(options.ProjectPath))
        {
            // Load project references (simplified - full impl uses MSBuild workspace)
            var additionalRefs = await LoadProjectReferencesAsync(options.ProjectPath, ct);
            references.AddRange(additionalRefs);
        }
        
        var compilation = CSharpCompilation.Create(
            assemblyName: "TempCompilation",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        
        // Get semantic model - NO CODE EXECUTION, just analysis
        return compilation.GetSemanticModel(syntaxTree);
    }
    
    private Task<IEnumerable<MetadataReference>> LoadProjectReferencesAsync(
        string projectPath, CancellationToken ct)
    {
        // Simplified implementation - full version would use MSBuild workspace
        return Task.FromResult(Enumerable.Empty<MetadataReference>());
    }
}

public record ParseResult
{
    public required SyntaxTree SyntaxTree { get; init; }
    public SemanticModel? SemanticModel { get; init; }
    public IReadOnlyList<ParseError> Errors { get; init; } = Array.Empty<ParseError>();
    public int ParseTimeMs { get; init; }
    public bool HasErrors => Errors.Count > 0;
}

public record ParseError
{
    public required string Message { get; init; }
    public int Line { get; init; }
    public string? Code { get; init; }
}
```

### Complete SymbolVisitor Implementation

```csharp
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using AgenticCoder.Domain.Symbols;
using AgenticCoder.Infrastructure.Symbols.CSharp.Security;

namespace AgenticCoder.Infrastructure.Symbols.CSharp;

/// <summary>
/// Walks the C# syntax tree to extract symbol information.
/// </summary>
public class SymbolVisitor : CSharpSyntaxWalker
{
    private readonly SyntaxTree _syntaxTree;
    private readonly SemanticModel? _semanticModel;
    private readonly XmlDocExtractor _docExtractor;
    private readonly SignatureFormatter _signatureFormatter;
    private readonly ResourceLimitGuard _resourceGuard;
    private readonly ExtractionOptions _options;
    private readonly Stack<ISymbol> _containingSymbols = new();
    
    public List<ISymbol> ExtractedSymbols { get; } = new();
    
    public SymbolVisitor(
        SyntaxTree syntaxTree,
        SemanticModel? semanticModel,
        XmlDocExtractor docExtractor,
        SignatureFormatter signatureFormatter,
        ResourceLimitGuard resourceGuard,
        ExtractionOptions options)
        : base(SyntaxWalkerDepth.Node)
    {
        _syntaxTree = syntaxTree;
        _semanticModel = semanticModel;
        _docExtractor = docExtractor;
        _signatureFormatter = signatureFormatter;
        _resourceGuard = resourceGuard;
        _options = options;
    }
    
    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        _resourceGuard.IncrementSymbolCount();
        
        using (_resourceGuard.EnterNestingLevel())
        {
            var symbol = CreateSymbol(node, SymbolKind.Class);
            ExtractedSymbols.Add(symbol);
            
            _containingSymbols.Push(symbol);
            base.VisitClassDeclaration(node);
            _containingSymbols.Pop();
        }
    }
    
    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        _resourceGuard.IncrementSymbolCount();
        
        using (_resourceGuard.EnterNestingLevel())
        {
            var symbol = CreateSymbol(node, SymbolKind.Interface);
            ExtractedSymbols.Add(symbol);
            
            _containingSymbols.Push(symbol);
            base.VisitInterfaceDeclaration(node);
            _containingSymbols.Pop();
        }
    }
    
    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
        _resourceGuard.IncrementSymbolCount();
        
        using (_resourceGuard.EnterNestingLevel())
        {
            var symbol = CreateSymbol(node, SymbolKind.Struct);
            ExtractedSymbols.Add(symbol);
            
            _containingSymbols.Push(symbol);
            base.VisitStructDeclaration(node);
            _containingSymbols.Pop();
        }
    }
    
    public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        _resourceGuard.IncrementSymbolCount();
        
        var symbol = CreateSymbol(node, SymbolKind.Enum);
        ExtractedSymbols.Add(symbol);
        
        _containingSymbols.Push(symbol);
        base.VisitEnumDeclaration(node);
        _containingSymbols.Pop();
    }
    
    public override void VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
    {
        _resourceGuard.IncrementSymbolCount();
        
        var symbol = new Symbol
        {
            Id = GenerateSymbolId(node),
            Name = node.Identifier.Text,
            Kind = SymbolKind.EnumMember,
            Location = GetLocation(node),
            ContainingSymbol = _containingSymbols.TryPeek(out var parent) ? parent : null,
            Documentation = _docExtractor.ExtractDocumentation(node)
        };
        
        // Extract enum value if explicit
        if (node.EqualsValue != null)
        {
            symbol.Metadata["Value"] = node.EqualsValue.Value.ToString();
        }
        
        ExtractedSymbols.Add(symbol);
        base.VisitEnumMemberDeclaration(node);
    }
    
    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (!ShouldInclude(node.Modifiers)) return;
        _resourceGuard.IncrementSymbolCount();
        
        var symbol = new Symbol
        {
            Id = GenerateSymbolId(node),
            Name = node.Identifier.Text,
            Kind = SymbolKind.Method,
            Location = GetLocation(node),
            Visibility = GetVisibility(node.Modifiers),
            Signature = _signatureFormatter.FormatMethodSignature(node, _semanticModel),
            ContainingSymbol = _containingSymbols.TryPeek(out var parent) ? parent : null,
            Documentation = _docExtractor.ExtractDocumentation(node)
        };
        
        // Extract modifiers
        symbol.Modifiers = GetModifiers(node.Modifiers);
        
        // Extract return type
        symbol.ReturnType = _semanticModel != null
            ? _semanticModel.GetTypeInfo(node.ReturnType).Type?.ToDisplayString()
            : node.ReturnType.ToString();
        
        ExtractedSymbols.Add(symbol);
        base.VisitMethodDeclaration(node);
    }
    
    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        if (!ShouldInclude(node.Modifiers)) return;
        _resourceGuard.IncrementSymbolCount();
        
        var symbol = new Symbol
        {
            Id = GenerateSymbolId(node),
            Name = node.Identifier.Text,
            Kind = SymbolKind.Property,
            Location = GetLocation(node),
            Visibility = GetVisibility(node.Modifiers),
            Signature = _signatureFormatter.FormatPropertySignature(node, _semanticModel),
            ContainingSymbol = _containingSymbols.TryPeek(out var parent) ? parent : null,
            Documentation = _docExtractor.ExtractDocumentation(node)
        };
        
        symbol.PropertyType = _semanticModel != null
            ? _semanticModel.GetTypeInfo(node.Type).Type?.ToDisplayString()
            : node.Type.ToString();
        
        ExtractedSymbols.Add(symbol);
        base.VisitPropertyDeclaration(node);
    }
    
    public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        if (!ShouldInclude(node.Modifiers)) return;
        
        foreach (var variable in node.Declaration.Variables)
        {
            _resourceGuard.IncrementSymbolCount();
            
            var symbol = new Symbol
            {
                Id = GenerateSymbolId(variable),
                Name = variable.Identifier.Text,
                Kind = SymbolKind.Field,
                Location = GetLocation(variable),
                Visibility = GetVisibility(node.Modifiers),
                Modifiers = GetModifiers(node.Modifiers),
                ContainingSymbol = _containingSymbols.TryPeek(out var parent) ? parent : null,
                Documentation = _docExtractor.ExtractDocumentation(node)
            };
            
            symbol.FieldType = _semanticModel != null
                ? _semanticModel.GetTypeInfo(node.Declaration.Type).Type?.ToDisplayString()
                : node.Declaration.Type.ToString();
            
            ExtractedSymbols.Add(symbol);
        }
        
        base.VisitFieldDeclaration(node);
    }
    
    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        if (!ShouldInclude(node.Modifiers)) return;
        _resourceGuard.IncrementSymbolCount();
        
        var symbol = new Symbol
        {
            Id = GenerateSymbolId(node),
            Name = node.Identifier.Text,
            Kind = SymbolKind.Constructor,
            Location = GetLocation(node),
            Visibility = GetVisibility(node.Modifiers),
            Signature = _signatureFormatter.FormatConstructorSignature(node),
            ContainingSymbol = _containingSymbols.TryPeek(out var parent) ? parent : null,
            Documentation = _docExtractor.ExtractDocumentation(node)
        };
        
        ExtractedSymbols.Add(symbol);
        base.VisitConstructorDeclaration(node);
    }
    
    public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        _resourceGuard.IncrementSymbolCount();
        
        using (_resourceGuard.EnterNestingLevel())
        {
            var symbol = CreateSymbol(node, SymbolKind.Record);
            ExtractedSymbols.Add(symbol);
            
            _containingSymbols.Push(symbol);
            base.VisitRecordDeclaration(node);
            _containingSymbols.Pop();
        }
    }
    
    private Symbol CreateSymbol(TypeDeclarationSyntax node, SymbolKind kind)
    {
        return new Symbol
        {
            Id = GenerateSymbolId(node),
            Name = node.Identifier.Text,
            Kind = kind,
            Location = GetLocation(node),
            Visibility = GetVisibility(node.Modifiers),
            Modifiers = GetModifiers(node.Modifiers),
            ContainingSymbol = _containingSymbols.TryPeek(out var parent) ? parent : null,
            Documentation = _docExtractor.ExtractDocumentation(node),
            GenericParameters = GetGenericParameters(node.TypeParameterList)
        };
    }
    
    private SymbolLocation GetLocation(SyntaxNode node)
    {
        var span = node.GetLocation().GetLineSpan();
        return new SymbolLocation
        {
            FilePath = _syntaxTree.FilePath,
            StartLine = span.StartLinePosition.Line + 1,
            EndLine = span.EndLinePosition.Line + 1,
            StartColumn = span.StartLinePosition.Character + 1,
            EndColumn = span.EndLinePosition.Character + 1
        };
    }
    
    private Visibility GetVisibility(SyntaxTokenList modifiers)
    {
        if (modifiers.Any(SyntaxKind.PublicKeyword)) return Visibility.Public;
        if (modifiers.Any(SyntaxKind.PrivateKeyword)) return Visibility.Private;
        if (modifiers.Any(SyntaxKind.ProtectedKeyword)) return Visibility.Protected;
        if (modifiers.Any(SyntaxKind.InternalKeyword)) return Visibility.Internal;
        return Visibility.Private; // Default for members
    }
    
    private List<string> GetModifiers(SyntaxTokenList modifiers)
    {
        var result = new List<string>();
        if (modifiers.Any(SyntaxKind.StaticKeyword)) result.Add("static");
        if (modifiers.Any(SyntaxKind.AbstractKeyword)) result.Add("abstract");
        if (modifiers.Any(SyntaxKind.VirtualKeyword)) result.Add("virtual");
        if (modifiers.Any(SyntaxKind.OverrideKeyword)) result.Add("override");
        if (modifiers.Any(SyntaxKind.SealedKeyword)) result.Add("sealed");
        if (modifiers.Any(SyntaxKind.AsyncKeyword)) result.Add("async");
        if (modifiers.Any(SyntaxKind.ReadOnlyKeyword)) result.Add("readonly");
        if (modifiers.Any(SyntaxKind.ConstKeyword)) result.Add("const");
        return result;
    }
    
    private List<string>? GetGenericParameters(TypeParameterListSyntax? typeParams)
    {
        if (typeParams == null) return null;
        return typeParams.Parameters.Select(p => p.Identifier.Text).ToList();
    }
    
    private bool ShouldInclude(SyntaxTokenList modifiers)
    {
        if (_options.IncludePrivate) return true;
        return modifiers.Any(SyntaxKind.PublicKeyword) ||
               modifiers.Any(SyntaxKind.ProtectedKeyword) ||
               modifiers.Any(SyntaxKind.InternalKeyword);
    }
    
    private string GenerateSymbolId(SyntaxNode node)
    {
        // Deterministic ID based on file path + location + kind
        var location = node.GetLocation().GetLineSpan();
        var input = $"{_syntaxTree.FilePath}:{location.StartLinePosition.Line}:{node.Kind()}";
        return Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(input)))[..22];
    }
}
```

### Complete XmlDocExtractor Implementation

```csharp
using System;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using AgenticCoder.Domain.Symbols;

namespace AgenticCoder.Infrastructure.Symbols.CSharp;

/// <summary>
/// Extracts XML documentation comments from C# syntax nodes.
/// </summary>
public class XmlDocExtractor
{
    /// <summary>
    /// Extracts documentation from the given syntax node.
    /// </summary>
    public SymbolDocumentation? ExtractDocumentation(SyntaxNode node)
    {
        var trivia = node.GetLeadingTrivia()
            .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                                  t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
        
        if (trivia == default) return null;
        
        var structure = trivia.GetStructure();
        if (structure is not DocumentationCommentTriviaSyntax docComment)
            return null;
        
        return ParseDocumentation(docComment);
    }
    
    private SymbolDocumentation ParseDocumentation(DocumentationCommentTriviaSyntax docComment)
    {
        var doc = new SymbolDocumentation();
        
        foreach (var node in docComment.Content)
        {
            if (node is XmlElementSyntax element)
            {
                var name = element.StartTag.Name.LocalName.Text.ToLowerInvariant();
                var content = GetElementContent(element);
                
                switch (name)
                {
                    case "summary":
                        doc.Summary = content;
                        break;
                    case "remarks":
                        doc.Remarks = content;
                        break;
                    case "returns":
                        doc.Returns = content;
                        break;
                    case "param":
                        var paramName = GetAttributeValue(element.StartTag, "name");
                        if (!string.IsNullOrEmpty(paramName))
                        {
                            doc.Parameters[paramName] = content;
                        }
                        break;
                    case "exception":
                        var exType = GetAttributeValue(element.StartTag, "cref");
                        if (!string.IsNullOrEmpty(exType))
                        {
                            doc.Exceptions[exType] = content;
                        }
                        break;
                    case "example":
                        doc.Examples.Add(content);
                        break;
                    case "seealso":
                        var seeRef = GetAttributeValue(element.StartTag, "cref");
                        if (!string.IsNullOrEmpty(seeRef))
                        {
                            doc.SeeAlso.Add(seeRef);
                        }
                        break;
                }
            }
        }
        
        return doc;
    }
    
    private string GetElementContent(XmlElementSyntax element)
    {
        var text = string.Join("", element.Content.Select(c => c.ToString()));
        return CleanXmlContent(text);
    }
    
    private string? GetAttributeValue(XmlElementStartTagSyntax startTag, string attrName)
    {
        var attr = startTag.Attributes
            .OfType<XmlNameAttributeSyntax>()
            .FirstOrDefault(a => a.Name.LocalName.Text == attrName);
        
        return attr?.Identifier.Identifier.Text;
    }
    
    private string CleanXmlContent(string content)
    {
        return content
            .Replace("///", "")
            .Replace("\r\n", " ")
            .Replace("\n", " ")
            .Trim();
    }
}
```

### Complete SignatureFormatter Implementation

```csharp
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AgenticCoder.Infrastructure.Symbols.CSharp;

/// <summary>
/// Formats C# symbol signatures for human-readable display.
/// </summary>
public class SignatureFormatter
{
    public string FormatMethodSignature(MethodDeclarationSyntax method, SemanticModel? model)
    {
        var sb = new StringBuilder();
        
        // Return type
        var returnType = model != null
            ? model.GetTypeInfo(method.ReturnType).Type?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
            : method.ReturnType.ToString();
        sb.Append(returnType);
        sb.Append(' ');
        
        // Method name
        sb.Append(method.Identifier.Text);
        
        // Generic parameters
        if (method.TypeParameterList != null)
        {
            sb.Append('<');
            sb.Append(string.Join(", ", method.TypeParameterList.Parameters.Select(p => p.Identifier.Text)));
            sb.Append('>');
        }
        
        // Parameters
        sb.Append('(');
        sb.Append(string.Join(", ", method.ParameterList.Parameters.Select(p => FormatParameter(p, model))));
        sb.Append(')');
        
        return sb.ToString();
    }
    
    public string FormatPropertySignature(PropertyDeclarationSyntax prop, SemanticModel? model)
    {
        var type = model != null
            ? model.GetTypeInfo(prop.Type).Type?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
            : prop.Type.ToString();
        
        return $"{type} {prop.Identifier.Text}";
    }
    
    public string FormatConstructorSignature(ConstructorDeclarationSyntax ctor)
    {
        var sb = new StringBuilder();
        sb.Append(ctor.Identifier.Text);
        sb.Append('(');
        sb.Append(string.Join(", ", ctor.ParameterList.Parameters.Select(p => FormatParameter(p, null))));
        sb.Append(')');
        return sb.ToString();
    }
    
    private string FormatParameter(ParameterSyntax param, SemanticModel? model)
    {
        var type = param.Type != null
            ? (model != null ? model.GetTypeInfo(param.Type).Type?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) : param.Type.ToString())
            : "?";
        
        var name = param.Identifier.Text;
        
        if (param.Default != null)
        {
            return $"{type} {name} = {param.Default.Value}";
        }
        
        return $"{type} {name}";
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-CSE-001 | Parse error - syntax issues in source file |
| ACODE-CSE-002 | File not found |
| ACODE-CSE-003 | Invalid C# - file cannot be parsed |
| ACODE-CSE-004 | Extraction error - general failure |
| ACODE-CSE-005 | Access denied - file permission issue |
| ACODE-CSE-006 | Resource limit exceeded - file too large or too many symbols |
| ACODE-CSE-007 | Security validation failed - path traversal or other security issue |
| ACODE-CSE-008 | Operation cancelled - timeout or user cancellation |

### Dependency Injection Registration

```csharp
// In ServiceCollectionExtensions.cs
public static IServiceCollection AddCSharpSymbolExtraction(
    this IServiceCollection services,
    string workspaceRoot)
{
    services.AddSingleton<RoslynParser>();
    services.AddSingleton<XmlDocExtractor>();
    services.AddSingleton<SignatureFormatter>();
    services.AddSingleton(sp => new SecureErrorHandler(
        sp.GetRequiredService<ILogger<SecureErrorHandler>>(),
        workspaceRoot));
    services.AddSingleton<ISymbolExtractor>(sp => new CSharpSymbolExtractor(
        sp.GetRequiredService<ILogger<CSharpSymbolExtractor>>(),
        sp.GetRequiredService<RoslynParser>(),
        sp.GetRequiredService<XmlDocExtractor>(),
        sp.GetRequiredService<SignatureFormatter>(),
        sp.GetRequiredService<SecureErrorHandler>(),
        workspaceRoot));
    
    return services;
}
```

### Implementation Checklist

1. [x] Create RoslynParser with secure parse options
2. [x] Create SymbolVisitor extending CSharpSyntaxWalker
3. [x] Extract all symbol types (class, interface, struct, enum, record)
4. [x] Extract all member types (method, property, field, constructor, event)
5. [x] Extract modifiers (public, private, static, async, etc.)
6. [x] Extract XML documentation comments
7. [x] Implement ISymbolExtractor interface
8. [x] Add security: path validation, resource limits, error sanitization
9. [x] Register extractor in DI container
10. [ ] Add unit tests (see Testing Requirements)
11. [ ] Add integration tests
12. [ ] Add performance benchmarks

### Rollout Plan

1. **Phase 1 (Week 1):** Basic RoslynParser and syntax-only extraction
2. **Phase 2 (Week 2):** SymbolVisitor for all type and member declarations
3. **Phase 3 (Week 3):** XmlDocExtractor and SignatureFormatter
4. **Phase 4 (Week 4):** Security hardening and resource limits
5. **Phase 5 (Week 5):** Integration with Symbol Index, testing, optimization

---

**End of Task 017.a Specification**