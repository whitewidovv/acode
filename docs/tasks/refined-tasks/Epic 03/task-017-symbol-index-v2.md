# Task 017: Symbol Index v2

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 014 (RepoFS), Task 015 (Text Index), Task 016 (Context Packer)  

---

## Description

Task 017 implements Symbol Index v2. The symbol index extracts and stores semantic information about code: classes, methods, properties, functions, types. This enables semantic code navigation and retrieval.

Text search finds strings. Symbol search finds meaning. When the agent needs `GetUserById`, symbol search returns the method definition, not every file containing "GetUserById".

Symbol Index v2 builds on Task 015's text index. Text index handles full-text search. Symbol index handles semantic search. Both serve the context packer.

The symbol index extracts symbols from source code. Roslyn parses C# code. TypeScript compiler API parses TypeScript/JavaScript. Language-specific extractors handle each language.

Symbols have relationships. Methods belong to classes. Classes implement interfaces. Functions call other functions. The symbol index stores these relationships.

Symbol extraction is language-specific. C# symbols differ from TypeScript symbols. Each language has its own extractor. Extractors implement a common interface.

The symbol index persists to the workspace database. Task 050 provides the schema. Symbol data survives restarts. Incremental updates keep it current.

Symbol retrieval serves the context packer. When the agent needs code, symbol lookup finds relevant symbols. Symbol context is more precise than full-file context.

Task 017 defines the core infrastructure. Task 017.a implements C# extraction. Task 017.b implements TypeScript extraction. Task 017.c adds dependency mapping and retrieval APIs.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Symbol | Code element: class, method, function, property, field, type |
| Symbol Index | Database of extracted symbols |
| Extractor | Language-specific symbol parser |
| Roslyn | .NET compiler platform for C# analysis |
| TypeScript Compiler API | TypeScript language services |
| Semantic Model | Compiler's type/binding information |
| Symbol Reference | Location where symbol is used |
| Symbol Definition | Location where symbol is declared |
| Symbol Kind | Category: class, method, property, etc. |
| Fully Qualified Name | Complete name including namespace/module |
| Signature | Method/function parameter and return types |
| Visibility | Public, private, protected, internal |
| Containment | Parent-child symbol relationship |
| Relationship | Symbol-to-symbol connection |
| Incremental Update | Update only changed files |

---

## Out of Scope

The following items are explicitly excluded from Task 017:

- **C# extraction details** - See Task 017.a
- **TypeScript extraction details** - See Task 017.b
- **Dependency mapping** - See Task 017.c
- **Retrieval APIs** - See Task 017.c
- **Python/Go/Rust support** - Future versions
- **Cross-repo symbols** - Single repo only
- **Semantic analysis** - Structure only
- **IDE integration** - CLI only

---

## Functional Requirements

### Symbol Model

- FR-001: Define ISymbol interface
- FR-002: Define SymbolKind enum
- FR-003: Support class symbols
- FR-004: Support interface symbols
- FR-005: Support method symbols
- FR-006: Support property symbols
- FR-007: Support field symbols
- FR-008: Support function symbols
- FR-009: Support type alias symbols
- FR-010: Support enum symbols
- FR-011: Support namespace/module symbols
- FR-012: Store fully qualified name
- FR-013: Store short name
- FR-014: Store file location
- FR-015: Store line/column range
- FR-016: Store visibility
- FR-017: Store signature for methods
- FR-018: Store containing symbol

### Symbol Store

- FR-019: Define ISymbolStore interface
- FR-020: Add symbols to store
- FR-021: Remove symbols from store
- FR-022: Update symbols in store
- FR-023: Query by name
- FR-024: Query by kind
- FR-025: Query by file
- FR-026: Query by namespace
- FR-027: Query by containing symbol
- FR-028: Batch operations
- FR-029: Persist to database
- FR-030: Load from database

### Symbol Extractor

- FR-031: Define ISymbolExtractor interface
- FR-032: Extractor returns symbols for file
- FR-033: Extractor reports parse errors
- FR-034: Registry of extractors by language
- FR-035: Fallback for unknown languages
- FR-036: Configurable extraction depth
- FR-037: Skip test files option
- FR-038: Skip generated files option

### Index Management

- FR-039: Define ISymbolIndex interface
- FR-040: Full index rebuild
- FR-041: Incremental index update
- FR-042: Index specific files
- FR-043: Remove files from index
- FR-044: Clear entire index
- FR-045: Index status reporting
- FR-046: Index progress callback
- FR-047: Cancellation support
- FR-048: Parallel indexing

### File Tracking

- FR-049: Track indexed file hashes
- FR-050: Detect file changes
- FR-051: Detect file deletions
- FR-052: Detect new files
- FR-053: Handle file renames

### Query Interface

- FR-054: Search symbols by name
- FR-055: Fuzzy name matching
- FR-056: Prefix matching
- FR-057: Filter by kind
- FR-058: Filter by visibility
- FR-059: Filter by file pattern
- FR-060: Pagination support
- FR-061: Order by relevance
- FR-062: Order by name
- FR-063: Order by file

### Symbol Resolution

- FR-064: Resolve symbol by ID
- FR-065: Get symbol source code
- FR-066: Get symbol documentation
- FR-067: Get containing context
- FR-068: Navigate to definition

---

## Non-Functional Requirements

### Performance

- NFR-001: Index 1000 files < 30s
- NFR-002: Incremental update < 100ms/file
- NFR-003: Query < 50ms
- NFR-004: Batch insert < 1ms/symbol
- NFR-005: Parallel indexing with workers

### Scalability

- NFR-006: Handle 100K files
- NFR-007: Handle 1M symbols
- NFR-008: Memory-efficient streaming
- NFR-009: Pagination for large results

### Reliability

- NFR-010: Survive parse errors
- NFR-011: Partial results on failure
- NFR-012: Consistent index state
- NFR-013: Crash recovery

### Accuracy

- NFR-014: No duplicate symbols
- NFR-015: Correct locations
- NFR-016: Valid relationships

---

## User Manual Documentation

### Overview

Symbol Index v2 provides semantic code search. Find classes, methods, and functions by name and type.

### Configuration

```yaml
# .agent/config.yml
symbol_index:
  # Enable symbol indexing
  enabled: true
  
  # Languages to index
  languages:
    - csharp
    - typescript
    - javascript
    
  # Indexing options
  options:
    # Include test files
    include_tests: false
    
    # Include generated files
    include_generated: false
    
    # Max file size to index (KB)
    max_file_size_kb: 500
    
    # Parallel workers
    workers: 4
    
  # Auto-update strategy
  update:
    # on_save, on_commit, manual
    trigger: on_save
    
    # Debounce delay (ms)
    debounce_ms: 500
```

### CLI Commands

```bash
# Build full symbol index
acode symbols build

# Build with progress
acode symbols build --progress

# Rebuild specific files
acode symbols build --files "src/**/*.cs"

# Incremental update
acode symbols update

# Search symbols
acode symbols search "UserService"

# Search by kind
acode symbols search "GetUser" --kind method

# Show index status
acode symbols status

# Clear index
acode symbols clear
```

### Search Examples

```bash
# Find all classes named "User*"
$ acode symbols search "User*" --kind class

Symbols matching "User*":
  1. UserService (class) - src/Services/UserService.cs:15
  2. UserRepository (class) - src/Data/UserRepository.cs:8
  3. UserController (class) - src/Controllers/UserController.cs:12
  4. UserDto (class) - src/Models/UserDto.cs:5

# Find methods containing "Get"
$ acode symbols search "*Get*" --kind method

Symbols matching "*Get*":
  1. GetById (method) - UserService.cs:45
  2. GetAll (method) - UserService.cs:60
  3. GetConnection (method) - DbContext.cs:25
```

### Index Status

```bash
$ acode symbols status

Symbol Index Status
───────────────────
Files indexed: 1,234
Symbols stored: 45,678
Last full build: 2024-01-15 10:30:00
Last update: 2024-01-15 14:45:00

By Language:
  C#: 28,450 symbols (650 files)
  TypeScript: 15,200 symbols (480 files)
  JavaScript: 2,028 symbols (104 files)

By Kind:
  Classes: 1,250
  Interfaces: 340
  Methods: 28,500
  Properties: 12,100
  Functions: 3,488
```

### Troubleshooting

#### Missing Symbols

**Problem:** Some symbols not appearing

**Solutions:**
1. Check file is in indexed languages
2. Check file size under limit
3. Check file not in ignore patterns
4. Rebuild index: `acode symbols build`

#### Parse Errors

**Problem:** Extractor reports errors

**Solutions:**
1. Ensure code compiles
2. Check language version settings
3. Update language extractors
4. Skip problematic files

#### Slow Indexing

**Problem:** Full build takes too long

**Solutions:**
1. Increase worker count
2. Exclude large generated files
3. Exclude test files if not needed
4. Use incremental updates

---

## Acceptance Criteria

### Model

- [ ] AC-001: ISymbol interface defined
- [ ] AC-002: All symbol kinds supported
- [ ] AC-003: Symbol metadata complete

### Store

- [ ] AC-004: CRUD operations work
- [ ] AC-005: Queries return results
- [ ] AC-006: Persistence works

### Extractor

- [ ] AC-007: Interface defined
- [ ] AC-008: Registry works
- [ ] AC-009: Error handling works

### Index

- [ ] AC-010: Full build works
- [ ] AC-011: Incremental works
- [ ] AC-012: Status reporting works

### CLI

- [ ] AC-013: Build command works
- [ ] AC-014: Search command works
- [ ] AC-015: Status command works

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Symbols/
├── SymbolTests.cs
│   ├── Should_Store_All_Metadata()
│   └── Should_Support_All_Kinds()
│
├── SymbolStoreTests.cs
│   ├── Should_Add_Symbol()
│   ├── Should_Remove_Symbol()
│   ├── Should_Query_By_Name()
│   └── Should_Query_By_Kind()
│
├── SymbolIndexTests.cs
│   ├── Should_Build_Index()
│   └── Should_Update_Incrementally()
│
└── ExtractorRegistryTests.cs
    ├── Should_Register_Extractor()
    └── Should_Get_By_Language()
```

### Integration Tests

```
Tests/Integration/Symbols/
├── SymbolStoreIntegrationTests.cs
│   └── Should_Persist_And_Load()
│
└── SymbolIndexIntegrationTests.cs
    └── Should_Index_Real_Files()
```

### E2E Tests

```
Tests/E2E/Symbols/
├── SymbolE2ETests.cs
│   ├── Should_Build_Via_CLI()
│   └── Should_Search_Via_CLI()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Index 1K files | 20s | 30s |
| Query by name | 30ms | 50ms |
| Incremental update | 50ms | 100ms |

---

## User Verification Steps

### Scenario 1: Build Index

1. Run `acode symbols build`
2. Wait for completion
3. Verify: Symbols indexed

### Scenario 2: Search Symbols

1. Build index
2. Run `acode symbols search "MyClass"`
3. Verify: Class found

### Scenario 3: Incremental Update

1. Build index
2. Modify a file
3. Run `acode symbols update`
4. Verify: Changes reflected

### Scenario 4: Check Status

1. Build index
2. Run `acode symbols status`
3. Verify: Stats accurate

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Symbols/
│   ├── ISymbol.cs
│   ├── Symbol.cs
│   ├── SymbolKind.cs
│   ├── SymbolLocation.cs
│   ├── ISymbolStore.cs
│   ├── ISymbolExtractor.cs
│   └── ISymbolIndex.cs
│
src/AgenticCoder.Infrastructure/
├── Symbols/
│   ├── SymbolStore.cs
│   ├── ExtractorRegistry.cs
│   └── SymbolIndexService.cs
```

### ISymbol Interface

```csharp
namespace AgenticCoder.Domain.Symbols;

public interface ISymbol
{
    Guid Id { get; }
    string Name { get; }
    string FullyQualifiedName { get; }
    SymbolKind Kind { get; }
    SymbolLocation Location { get; }
    string? Signature { get; }
    string Visibility { get; }
    Guid? ContainingSymbolId { get; }
}
```

### SymbolKind Enum

```csharp
public enum SymbolKind
{
    Namespace,
    Class,
    Interface,
    Struct,
    Enum,
    Method,
    Property,
    Field,
    Constructor,
    Function,
    Variable,
    TypeAlias
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-SYM-001 | Parse error |
| ACODE-SYM-002 | Store error |
| ACODE-SYM-003 | Index error |
| ACODE-SYM-004 | Query error |

### Implementation Checklist

1. [ ] Create symbol model
2. [ ] Create symbol store
3. [ ] Create extractor interface
4. [ ] Create extractor registry
5. [ ] Create index service
6. [ ] Add CLI commands
7. [ ] Add database schema
8. [ ] Add unit tests

### Rollout Plan

1. **Phase 1:** Symbol model and store
2. **Phase 2:** Extractor infrastructure
3. **Phase 3:** Index service
4. **Phase 4:** CLI integration
5. **Phase 5:** Persistence

---

**End of Task 017 Specification**