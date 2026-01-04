# Task 017: Symbol Index v2

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 014 (RepoFS), Task 015 (Text Index), Task 016 (Context Packer), Task 050 (Workspace DB)  

---

## Description

### Business Value

Symbol Index v2 elevates the agent from text-level to semantic-level code understanding. While Task 015's text index finds strings, Symbol Index finds meaning—classes, methods, properties, and their relationships.

This semantic understanding provides:

1. **Precise Code Navigation:** When the agent needs `GetUserById`, symbol search returns the method definition, not every file containing those characters. This precision dramatically improves context quality.

2. **Relationship Awareness:** Symbols have relationships—methods belong to classes, classes implement interfaces, functions call other functions. This enables the agent to understand code structure, not just content.

3. **Language-Aware Indexing:** Each programming language has unique symbol types and conventions. Language-specific extractors (Roslyn for C#, TypeScript Compiler API for TS) provide accurate semantic analysis.

4. **Incremental Efficiency:** Only changed files are re-analyzed. The symbol index stays current with minimal overhead, enabling real-time updates as developers work.

5. **Context Enhancement:** Symbol-based retrieval provides more precise context to the LLM. Instead of "file containing UserService," the agent gets "the CreateUser method in UserService."

### Scope

Task 017 defines the core symbol indexing infrastructure:

1. **Symbol Model:** Defines ISymbol interface and related types (SymbolKind, SymbolLocation, visibility, signatures). Common representation across all languages.

2. **Symbol Store:** Persistent storage for symbols with CRUD operations. Supports queries by name, kind, file, namespace, and containment. Uses workspace database from Task 050.

3. **Extractor Interface:** Defines ISymbolExtractor contract. Language-specific implementations in subtasks (017.a for C#, 017.b for TypeScript).

4. **Extractor Registry:** Maps file extensions to extractors. Fallback for unsupported languages. Enables future language additions.

5. **Index Service:** Orchestrates full and incremental indexing. Tracks file hashes for change detection. Supports parallel processing.

6. **Query Interface:** Rich querying capabilities—exact match, prefix, fuzzy, filtered by kind/visibility/file.

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Task 014 (RepoFS) | File Access | Reads files for extraction |
| Task 015 (Text Index) | Complementary | Text index for full-text, symbol index for semantic |
| Task 016 (Context) | Data Source | Symbol definitions feed context packer |
| Task 050 (Workspace DB) | Persistence | Stores symbols in SQLite database |
| Task 017.a (C# Extractor) | Implementation | Roslyn-based C# extraction |
| Task 017.b (TS Extractor) | Implementation | TypeScript extraction |
| Task 017.c (Dependencies) | Enhancement | Cross-reference and dependency mapping |
| Task 003.c (Audit) | Audit Logging | Index operations are audited |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Parse error in file | File not indexed | Log warning, skip file, continue |
| Extractor crash | Indexing fails | Isolate extraction, timeout, skip file |
| Database corruption | Index unavailable | Corruption detection, rebuild |
| Memory exhaustion | Indexing crashes | Stream processing, file size limits |
| Concurrent updates | Inconsistent state | Locking, transaction isolation |
| Unknown language | No symbols extracted | Warn, fallback to text-only |
| Large file timeout | Slow indexing | Size limits, timeout per file |
| Duplicate symbols | Query ambiguity | Unique ID, include location in disambiguation |

### Assumptions

1. Target languages have parseable syntax (valid source files)
2. Language extractors are available (Roslyn, TS Compiler API)
3. Symbols have unique IDs within the index
4. Symbol locations are stable within a file version
5. Incremental updates are more common than full rebuilds
6. Most queries are by name or kind
7. Containment relationships are tree-structured
8. File hashes reliably detect changes
9. Parallel extraction is safe
10. Database can handle 1M+ symbols

### Security Considerations

Symbol indexing involves parsing and analyzing code:

1. **Parse Safety:** Extractors MUST handle malicious input. Parser crashes MUST NOT affect the host process.

2. **Resource Limits:** Extraction MUST have memory and time limits. Adversarial files MUST NOT cause DoS.

3. **Path Validation:** All file paths MUST go through RepoFS. No direct file system access.

4. **Content Protection:** Indexed content MUST have same access controls as source. Index permissions MUST match repository.

5. **Audit Trail:** Index operations SHOULD be logged for troubleshooting.

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

### Symbol Model (FR-017-01 to FR-017-25)

| ID | Requirement |
|----|-------------|
| FR-017-01 | System MUST define ISymbol interface |
| FR-017-02 | ISymbol MUST have Id property (Guid) |
| FR-017-03 | ISymbol MUST have Name property (short name) |
| FR-017-04 | ISymbol MUST have FullyQualifiedName property |
| FR-017-05 | ISymbol MUST have Kind property (SymbolKind enum) |
| FR-017-06 | ISymbol MUST have Location property (SymbolLocation) |
| FR-017-07 | ISymbol MUST have Signature property (nullable) |
| FR-017-08 | ISymbol MUST have Visibility property |
| FR-017-09 | ISymbol MUST have ContainingSymbolId property (nullable) |
| FR-017-10 | System MUST define SymbolKind enum |
| FR-017-11 | SymbolKind MUST include Namespace |
| FR-017-12 | SymbolKind MUST include Class |
| FR-017-13 | SymbolKind MUST include Interface |
| FR-017-14 | SymbolKind MUST include Struct |
| FR-017-15 | SymbolKind MUST include Enum |
| FR-017-16 | SymbolKind MUST include Method |
| FR-017-17 | SymbolKind MUST include Property |
| FR-017-18 | SymbolKind MUST include Field |
| FR-017-19 | SymbolKind MUST include Constructor |
| FR-017-20 | SymbolKind MUST include Function |
| FR-017-21 | SymbolKind MUST include Variable |
| FR-017-22 | SymbolKind MUST include TypeAlias |
| FR-017-23 | SymbolLocation MUST include FilePath |
| FR-017-24 | SymbolLocation MUST include StartLine/EndLine |
| FR-017-25 | SymbolLocation MUST include StartColumn/EndColumn |

### Symbol Store (FR-017-26 to FR-017-50)

| ID | Requirement |
|----|-------------|
| FR-017-26 | System MUST define ISymbolStore interface |
| FR-017-27 | AddAsync MUST insert single symbol |
| FR-017-28 | AddRangeAsync MUST batch insert symbols |
| FR-017-29 | Batch insert MUST be transactional |
| FR-017-30 | RemoveAsync MUST delete symbol by ID |
| FR-017-31 | RemoveByFileAsync MUST delete all symbols in file |
| FR-017-32 | UpdateAsync MUST modify existing symbol |
| FR-017-33 | GetByIdAsync MUST retrieve symbol by ID |
| FR-017-34 | Store MUST persist to SQLite database |
| FR-017-35 | Store MUST load from database on startup |
| FR-017-36 | Store MUST support concurrent reads |
| FR-017-37 | Store MUST serialize writes |
| FR-017-38 | Store MUST use connection pooling |
| FR-017-39 | Store MUST create indexes for common queries |
| FR-017-40 | Store MUST index Name column |
| FR-017-41 | Store MUST index Kind column |
| FR-017-42 | Store MUST index FilePath column |
| FR-017-43 | Store MUST index ContainingSymbolId column |
| FR-017-44 | Store MUST handle 1M+ symbols |
| FR-017-45 | Store MUST support pagination |
| FR-017-46 | Store MUST report symbol count |
| FR-017-47 | Store MUST report file count |
| FR-017-48 | ClearAsync MUST delete all symbols |
| FR-017-49 | Store MUST be disposable |
| FR-017-50 | Disposal MUST release connections |

### Symbol Extractor (FR-017-51 to FR-017-70)

| ID | Requirement |
|----|-------------|
| FR-017-51 | System MUST define ISymbolExtractor interface |
| FR-017-52 | ExtractAsync MUST accept file path |
| FR-017-53 | ExtractAsync MUST accept file content |
| FR-017-54 | ExtractAsync MUST return list of symbols |
| FR-017-55 | ExtractAsync MUST accept CancellationToken |
| FR-017-56 | Extractor MUST report supported extensions |
| FR-017-57 | Extractor MUST report supported languages |
| FR-017-58 | Extractor MUST handle parse errors gracefully |
| FR-017-59 | Parse errors MUST be logged |
| FR-017-60 | Parse errors MUST return partial results |
| FR-017-61 | Extractor MUST respect extraction depth config |
| FR-017-62 | Depth 0 MUST extract types only |
| FR-017-63 | Depth 1 MUST extract types and members |
| FR-017-64 | Depth 2 MUST extract nested and local |
| FR-017-65 | Extractor MUST respect file size limit |
| FR-017-66 | Oversized files MUST be skipped |
| FR-017-67 | Extractor MUST respect timeout |
| FR-017-68 | Timeout MUST return partial results |
| FR-017-69 | System MUST define IExtractorRegistry |
| FR-017-70 | Registry MUST map extensions to extractors |

### Index Management (FR-017-71 to FR-017-95)

| ID | Requirement |
|----|-------------|
| FR-017-71 | System MUST define ISymbolIndex interface |
| FR-017-72 | BuildAsync MUST perform full index rebuild |
| FR-017-73 | Build MUST clear existing symbols first |
| FR-017-74 | Build MUST enumerate all source files |
| FR-017-75 | Build MUST respect ignore patterns |
| FR-017-76 | Build MUST extract symbols per file |
| FR-017-77 | Build MUST store extracted symbols |
| FR-017-78 | Build MUST track file hashes |
| FR-017-79 | Build MUST report progress |
| FR-017-80 | UpdateAsync MUST perform incremental update |
| FR-017-81 | Update MUST detect modified files |
| FR-017-82 | Update MUST detect new files |
| FR-017-83 | Update MUST detect deleted files |
| FR-017-84 | Modified files MUST be re-extracted |
| FR-017-85 | New files MUST be extracted and added |
| FR-017-86 | Deleted files MUST have symbols removed |
| FR-017-87 | IndexFilesAsync MUST index specific files |
| FR-017-88 | RemoveFilesAsync MUST remove specific files |
| FR-017-89 | ClearAsync MUST clear entire index |
| FR-017-90 | GetStatusAsync MUST return index status |
| FR-017-91 | Status MUST include file count |
| FR-017-92 | Status MUST include symbol count |
| FR-017-93 | Status MUST include last build time |
| FR-017-94 | Status MUST include last update time |
| FR-017-95 | All operations MUST support cancellation |

### Query Interface (FR-017-96 to FR-017-120)

| ID | Requirement |
|----|-------------|
| FR-017-96 | System MUST define ISymbolQuery interface |
| FR-017-97 | SearchAsync MUST accept query string |
| FR-017-98 | Search MUST support exact name match |
| FR-017-99 | Search MUST support prefix match |
| FR-017-100 | Search MUST support fuzzy match |
| FR-017-101 | Fuzzy MUST tolerate typos |
| FR-017-102 | Search MUST support wildcard (*) |
| FR-017-103 | Search MUST filter by SymbolKind |
| FR-017-104 | Search MUST filter by visibility |
| FR-017-105 | Search MUST filter by file pattern |
| FR-017-106 | Search MUST filter by namespace |
| FR-017-107 | Search MUST support multiple filters |
| FR-017-108 | Search MUST return ordered results |
| FR-017-109 | Order MUST support by relevance |
| FR-017-110 | Order MUST support by name |
| FR-017-111 | Order MUST support by file |
| FR-017-112 | Search MUST support pagination |
| FR-017-113 | Pagination MUST accept skip and take |
| FR-017-114 | Search MUST return total count |
| FR-017-115 | ResolveAsync MUST get symbol by ID |
| FR-017-116 | GetSourceAsync MUST return symbol source code |
| FR-017-117 | GetDocumentationAsync MUST return doc comments |
| FR-017-118 | GetContainingAsync MUST return containing context |
| FR-017-119 | GetChildrenAsync MUST return contained symbols |
| FR-017-120 | Query operations MUST be read-only |

---

## Non-Functional Requirements

### Performance (NFR-017-01 to NFR-017-20)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017-01 | Performance | Full index of 1000 files MUST complete in < 30s |
| NFR-017-02 | Performance | Full index of 10,000 files MUST complete in < 5 min |
| NFR-017-03 | Performance | Incremental update per file MUST complete in < 100ms |
| NFR-017-04 | Performance | Incremental update for 100 files MUST complete in < 5s |
| NFR-017-05 | Performance | Name search MUST complete in < 50ms |
| NFR-017-06 | Performance | Fuzzy search MUST complete in < 100ms |
| NFR-017-07 | Performance | Filtered search MUST complete in < 75ms |
| NFR-017-08 | Performance | Symbol resolution by ID MUST complete in < 10ms |
| NFR-017-09 | Performance | Batch insert MUST achieve > 1000 symbols/s |
| NFR-017-10 | Performance | Index load MUST complete in < 2s |
| NFR-017-11 | Performance | Memory usage during indexing MUST be < 1GB |
| NFR-017-12 | Performance | Memory usage for loaded index MUST be < 200MB |
| NFR-017-13 | Performance | Parallel indexing MUST use configurable workers |
| NFR-017-14 | Performance | Default worker count MUST be CPU cores - 1 |
| NFR-017-15 | Performance | Database queries MUST use prepared statements |
| NFR-017-16 | Performance | Database MUST use WAL mode |
| NFR-017-17 | Performance | Database indexes MUST cover common queries |
| NFR-017-18 | Performance | Connection pooling MUST be used |
| NFR-017-19 | Performance | Extraction MUST stream large files |
| NFR-017-20 | Performance | Progress reporting MUST NOT slow indexing |

### Scalability (NFR-017-21 to NFR-017-30)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017-21 | Scalability | Index MUST handle 100,000 files |
| NFR-017-22 | Scalability | Index MUST handle 1,000,000 symbols |
| NFR-017-23 | Scalability | Queries MUST remain < 100ms at 1M symbols |
| NFR-017-24 | Scalability | Pagination MUST work at any offset |
| NFR-017-25 | Scalability | Large result sets MUST be streamed |
| NFR-017-26 | Scalability | Index file size MUST scale linearly |
| NFR-017-27 | Scalability | Memory MUST NOT scale with symbol count |
| NFR-017-28 | Scalability | Concurrent queries MUST be supported |
| NFR-017-29 | Scalability | Concurrent updates MUST be serialized |
| NFR-017-30 | Scalability | Large files (>1MB) MUST NOT block |

### Reliability (NFR-017-31 to NFR-017-45)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017-31 | Reliability | Parse errors MUST NOT stop indexing |
| NFR-017-32 | Reliability | Extractor crash MUST NOT crash host |
| NFR-017-33 | Reliability | Partial results MUST be returned on failure |
| NFR-017-34 | Reliability | Index state MUST remain consistent |
| NFR-017-35 | Reliability | Interrupted build MUST be resumable |
| NFR-017-36 | Reliability | Corruption MUST be detected on load |
| NFR-017-37 | Reliability | Corruption MUST trigger rebuild prompt |
| NFR-017-38 | Reliability | Crash during update MUST NOT corrupt index |
| NFR-017-39 | Reliability | Database transactions MUST be atomic |
| NFR-017-40 | Reliability | Rollback MUST restore previous state |
| NFR-017-41 | Reliability | File locks MUST be handled |
| NFR-017-42 | Reliability | Network errors (if any) MUST retry |
| NFR-017-43 | Reliability | Out of disk space MUST be handled |
| NFR-017-44 | Reliability | Symbols MUST have unique IDs |
| NFR-017-45 | Reliability | Duplicate detection MUST prevent conflicts |

### Accuracy (NFR-017-46 to NFR-017-55)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017-46 | Accuracy | Symbol names MUST match source exactly |
| NFR-017-47 | Accuracy | Symbol locations MUST be precise |
| NFR-017-48 | Accuracy | Line numbers MUST be 1-based |
| NFR-017-49 | Accuracy | Column numbers MUST be 1-based |
| NFR-017-50 | Accuracy | Containment MUST reflect actual structure |
| NFR-017-51 | Accuracy | Visibility MUST match source |
| NFR-017-52 | Accuracy | Signatures MUST be parseable |
| NFR-017-53 | Accuracy | Deleted file symbols MUST be removed |
| NFR-017-54 | Accuracy | Renamed files MUST update correctly |
| NFR-017-55 | Accuracy | No orphaned symbols after update |

### Maintainability (NFR-017-56 to NFR-017-65)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017-56 | Maintainability | Schema MUST be versioned |
| NFR-017-57 | Maintainability | Schema migrations MUST be automatic |
| NFR-017-58 | Maintainability | All interfaces MUST have XML docs |
| NFR-017-59 | Maintainability | Code coverage MUST be > 80% |
| NFR-017-60 | Maintainability | Cyclomatic complexity MUST be < 10 |
| NFR-017-61 | Maintainability | Dependencies MUST be injected |
| NFR-017-62 | Maintainability | Extractors MUST be pluggable |
| NFR-017-63 | Maintainability | Adding language MUST NOT modify core |
| NFR-017-64 | Maintainability | Configuration MUST be documented |
| NFR-017-65 | Maintainability | Error codes MUST be documented |

### Observability (NFR-017-66 to NFR-017-75)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017-66 | Observability | Build progress MUST be logged |
| NFR-017-67 | Observability | Extraction errors MUST be logged |
| NFR-017-68 | Observability | Query operations MUST log at Debug |
| NFR-017-69 | Observability | Metrics MUST track indexed file count |
| NFR-017-70 | Observability | Metrics MUST track symbol count |
| NFR-017-71 | Observability | Metrics MUST track build duration |
| NFR-017-72 | Observability | Metrics MUST track query latency |
| NFR-017-73 | Observability | Metrics MUST track extraction errors |
| NFR-017-74 | Observability | Structured logging MUST be used |
| NFR-017-75 | Observability | Correlation IDs MUST be propagated |

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

## Best Practices

### Index Design

1. **Separate storage from extraction** - Index format independent of language parsers
2. **Support incremental updates** - Only re-extract changed files
3. **Include relationships** - Store call graphs, inheritance, not just symbols
4. **Version the schema** - Allow index format evolution with migration

### Query Optimization

5. **Index by multiple keys** - Name, file, type for fast lookups
6. **Support prefix queries** - Find symbols starting with pattern
7. **Fuzzy matching optional** - Exact match fast, fuzzy if needed
8. **Limit result sets** - Pagination for large result sets

### Scalability

9. **Handle large codebases** - Tested with 100K+ files
10. **Background building** - Don't block user operations
11. **Memory bounded** - Stream processing for large files
12. **Parallel extraction** - Utilize multiple cores for parsing

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Symbols/
├── SymbolTests.cs
│   ├── Should_Store_Name()
│   ├── Should_Store_FullyQualifiedName()
│   ├── Should_Store_Kind()
│   ├── Should_Store_Location()
│   ├── Should_Store_Signature()
│   ├── Should_Store_Visibility()
│   ├── Should_Store_ContainingSymbol()
│   ├── Should_Support_Class_Kind()
│   ├── Should_Support_Interface_Kind()
│   ├── Should_Support_Struct_Kind()
│   ├── Should_Support_Enum_Kind()
│   ├── Should_Support_Method_Kind()
│   ├── Should_Support_Property_Kind()
│   ├── Should_Support_Field_Kind()
│   ├── Should_Support_Function_Kind()
│   ├── Should_Support_TypeAlias_Kind()
│   └── Should_Support_Namespace_Kind()
│
├── SymbolLocationTests.cs
│   ├── Should_Store_FilePath()
│   ├── Should_Store_StartLine()
│   ├── Should_Store_EndLine()
│   ├── Should_Store_StartColumn()
│   ├── Should_Store_EndColumn()
│   └── Should_Calculate_Range()
│
├── SymbolStoreTests.cs
│   ├── Should_Add_Symbol()
│   ├── Should_Add_Multiple_Symbols()
│   ├── Should_Add_Batch_Symbols()
│   ├── Should_Remove_Symbol_By_Id()
│   ├── Should_Remove_Symbols_By_File()
│   ├── Should_Update_Symbol()
│   ├── Should_Query_By_Exact_Name()
│   ├── Should_Query_By_Prefix()
│   ├── Should_Query_By_Fuzzy_Match()
│   ├── Should_Query_By_Kind()
│   ├── Should_Query_By_Visibility()
│   ├── Should_Query_By_File_Pattern()
│   ├── Should_Query_By_Namespace()
│   ├── Should_Query_By_ContainingSymbol()
│   ├── Should_Return_Paginated_Results()
│   ├── Should_Order_By_Name()
│   ├── Should_Order_By_Relevance()
│   ├── Should_Order_By_File()
│   ├── Should_Handle_Empty_Store()
│   └── Should_Handle_No_Matches()
│
├── SymbolIndexTests.cs
│   ├── Should_Build_Full_Index()
│   ├── Should_Track_Indexed_Files()
│   ├── Should_Detect_Changed_Files()
│   ├── Should_Detect_New_Files()
│   ├── Should_Detect_Deleted_Files()
│   ├── Should_Update_Incrementally()
│   ├── Should_Index_Specific_Files()
│   ├── Should_Remove_File_From_Index()
│   ├── Should_Clear_Index()
│   ├── Should_Report_Status()
│   ├── Should_Report_Progress()
│   ├── Should_Support_Cancellation()
│   ├── Should_Index_In_Parallel()
│   ├── Should_Handle_Parse_Errors()
│   └── Should_Provide_Partial_Results()
│
├── ExtractorRegistryTests.cs
│   ├── Should_Register_Extractor()
│   ├── Should_Get_Extractor_By_Extension()
│   ├── Should_Get_Extractor_By_Language()
│   ├── Should_Return_Null_For_Unknown()
│   ├── Should_Support_Multiple_Extensions()
│   ├── Should_Handle_Fallback_Extractor()
│   └── Should_List_Supported_Languages()
│
├── ExtractorConfigTests.cs
│   ├── Should_Respect_Max_File_Size()
│   ├── Should_Skip_Test_Files()
│   ├── Should_Skip_Generated_Files()
│   └── Should_Apply_Extraction_Depth()
│
└── SymbolResolutionTests.cs
    ├── Should_Resolve_Symbol_By_Id()
    ├── Should_Get_Symbol_Source_Code()
    ├── Should_Get_Symbol_Documentation()
    ├── Should_Get_Containing_Context()
    └── Should_Navigate_To_Definition()
```

### Integration Tests

```
Tests/Integration/Symbols/
├── SymbolStoreIntegrationTests.cs
│   ├── Should_Persist_To_Database()
│   ├── Should_Load_From_Database()
│   ├── Should_Handle_Concurrent_Writes()
│   ├── Should_Handle_Large_Symbol_Count()
│   └── Should_Survive_Restart()
│
├── SymbolIndexIntegrationTests.cs
│   ├── Should_Index_CSharp_Files()
│   ├── Should_Index_TypeScript_Files()
│   ├── Should_Index_JavaScript_Files()
│   ├── Should_Index_Mixed_Languages()
│   ├── Should_Handle_Large_Codebase()
│   ├── Should_Handle_Incremental_With_Many_Changes()
│   └── Should_Recover_From_Crash()
│
└── QueryIntegrationTests.cs
    ├── Should_Search_Across_Languages()
    ├── Should_Combine_Filters()
    └── Should_Handle_Complex_Queries()
```

### E2E Tests

```
Tests/E2E/Symbols/
├── SymbolE2ETests.cs
│   ├── Should_Build_Index_Via_CLI()
│   ├── Should_Build_With_Progress_Via_CLI()
│   ├── Should_Update_Index_Via_CLI()
│   ├── Should_Search_Symbols_Via_CLI()
│   ├── Should_Search_By_Kind_Via_CLI()
│   ├── Should_Show_Status_Via_CLI()
│   ├── Should_Clear_Index_Via_CLI()
│   └── Should_Integrate_With_Context_Packer()
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