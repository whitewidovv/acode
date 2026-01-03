# Task 017.c: Dependency Mapping + Retrieval APIs

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 017 (Symbol Index), Task 017.a (C# Extraction), Task 017.b (TS/JS Extraction)  

---

## Description

### Business Value

Dependency mapping transforms isolated symbol data into an interconnected knowledge graph that reveals how code components relate to each other. Without dependency information, the agent can identify that `UserService` exists but cannot determine what calls it, what it depends on, or what would break if it were modified. This task implements the relationship layer that powers impact analysis, usage discovery, and intelligent context selection.

When a developer asks the agent to refactor a method, the dependency graph immediately identifies all callers that must be reviewed or updated. When the context packer selects code for a prompt, it can include relevant dependencies to provide complete context. When the agent modifies a class, it can warn about breaking changes based on what depends on the public interface. These capabilities are impossible without persistent, queryable dependency relationships.

The dependency graph persists across sessions and updates incrementally as code changes. When a file is modified, only its dependencies are re-extracted—the rest of the graph remains intact. This incremental approach enables near-instant queries even on large codebases, supporting the interactive response times users expect from a coding assistant.

### Scope

This task delivers the following components:

1. **IDependency/Dependency** - Domain model representing a directional relationship between two symbols
2. **DependencyKind** - Enumeration of relationship types (calls, uses, inherits, implements, references)
3. **IDependencyStore/DependencyStore** - Persistence layer for storing and querying dependencies
4. **DependencyExtractor** - Extracts dependencies from parsed symbols (integrates with Task 017.a and 017.b)
5. **IDependencyGraph/DependencyGraph** - Graph operations including transitive queries, cycle detection, and path finding
6. **DependencyQueryService** - High-level retrieval APIs for common query patterns

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| C# Symbol Extractor (Task 017.a) | Producer | Provides symbols from which dependencies are extracted |
| TS/JS Symbol Extractor (Task 017.b) | Producer | Provides symbols from which dependencies are extracted |
| Symbol Index (Task 017) | Dependency | Resolves symbol IDs for dependency endpoints |
| SQLite Database | Persistence | Stores dependency edges with indexes for fast queries |
| Context Packer | Consumer | Queries dependencies to include related code in prompts |
| CLI Commands | Consumer | Exposes dependency queries via `acode deps` commands |
| File Watcher | Trigger | Triggers incremental updates when source files change |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Symbol not found in index | Dependency endpoint unresolved | Log warning, store with unresolved flag, retry on index update |
| Circular dependency in query | Infinite loop potential | Cycle detection with visited set, max depth limits |
| Database corruption | Data loss | Transaction-based writes, backup before rebuild |
| Memory exhaustion on large graph | Query failure or crash | Streaming iterators, pagination, memory bounds |
| Stale dependencies after file delete | Phantom edges remain | File deletion triggers edge cleanup |
| Concurrent write conflicts | Inconsistent state | Write serialization per file, read-write locks |

### Assumptions

1. Symbol IDs from Task 017.a and 017.b are stable and deterministic
2. Dependencies are extracted after symbols are indexed (ordered pipeline)
3. Cross-language dependencies (C# → JS) are out of scope for this task
4. Dependency resolution is best-effort—unresolved references are logged but not blocking
5. The SQLite database has sufficient capacity for millions of edges
6. Transitive query depth is configurable with a sensible default (10 levels)
7. Cycle detection marks cycles rather than attempting to break them
8. Dependency extraction runs in the same process as symbol extraction

### Security Considerations

1. **Query Limits** - All traversal queries MUST have configurable depth and node limits to prevent DoS
2. **Input Validation** - Symbol IDs MUST be validated before database queries
3. **No Code Execution** - Dependency extraction is pure data analysis with no code execution
4. **Path Exposure** - File paths in dependencies MUST be relative to project root
5. **Audit Trail** - Dependency graph modifications SHOULD be logged for debugging

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Dependency | Symbol A uses Symbol B |
| Dependent | Symbol B is used by Symbol A |
| Edge | Graph connection between symbols |
| Node | Symbol in the dependency graph |
| Outgoing Edge | Dependency (uses) relationship |
| Incoming Edge | Dependent (used by) relationship |
| Relationship Type | Kind of dependency |
| Transitive | Indirect dependency chain |
| Cycle | Circular dependency |
| Call Graph | Function call relationships |
| Type Graph | Type reference relationships |
| Inheritance | Parent-child type relationship |
| Implementation | Interface-class relationship |
| Reference | Any usage of a symbol |

---

## Out of Scope

The following items are explicitly excluded from Task 017.c:

- **Cross-language dependencies** - Single language only
- **Runtime dependencies** - Static analysis only
- **Package dependencies** - Source code only
- **Dependency injection** - Manual config only
- **Graph visualization** - Data access only
- **Automatic refactoring** - Query only
- **Impact analysis scoring** - Raw data only

---

## Functional Requirements

### Dependency Model (FR-017c-01 to FR-017c-11)

| ID | Requirement |
|----|-------------|
| FR-017c-01 | System MUST define IDependency interface in Domain layer |
| FR-017c-02 | System MUST define DependencyKind enum with all relationship types |
| FR-017c-03 | DependencyKind MUST include Calls (method/function invocation) |
| FR-017c-04 | DependencyKind MUST include Uses (type reference) |
| FR-017c-05 | DependencyKind MUST include Inherits (class inheritance) |
| FR-017c-06 | DependencyKind MUST include Implements (interface implementation) |
| FR-017c-07 | DependencyKind MUST include References (any symbol usage) |
| FR-017c-08 | Dependency MUST store source symbol ID (caller/user) |
| FR-017c-09 | Dependency MUST store target symbol ID (callee/used) |
| FR-017c-10 | Dependency MUST store relationship kind |
| FR-017c-11 | Dependency MUST store source location (where the reference occurs) |

### Dependency Store (FR-017c-12 to FR-017c-22)

| ID | Requirement |
|----|-------------|
| FR-017c-12 | System MUST define IDependencyStore interface in Domain layer |
| FR-017c-13 | Store MUST support adding single dependencies |
| FR-017c-14 | Store MUST support removing single dependencies |
| FR-017c-15 | Store MUST support removing all dependencies by source symbol |
| FR-017c-16 | Store MUST support removing all dependencies by target symbol |
| FR-017c-17 | Store MUST support querying dependencies by source symbol |
| FR-017c-18 | Store MUST support querying dependencies by target symbol |
| FR-017c-19 | Store MUST support querying dependencies by kind |
| FR-017c-20 | Store MUST support batch add/remove operations for performance |
| FR-017c-21 | Store MUST persist dependencies to SQLite database |
| FR-017c-22 | Store MUST load dependencies from database on startup |

### Dependency Extraction (FR-017c-23 to FR-017c-30)

| ID | Requirement |
|----|-------------|
| FR-017c-23 | Extractor MUST identify method/function calls and create Calls dependencies |
| FR-017c-24 | Extractor MUST identify property accesses and create References dependencies |
| FR-017c-25 | Extractor MUST identify field accesses and create References dependencies |
| FR-017c-26 | Extractor MUST identify type references and create Uses dependencies |
| FR-017c-27 | Extractor MUST identify base class and create Inherits dependencies |
| FR-017c-28 | Extractor MUST identify interface implementations and create Implements dependencies |
| FR-017c-29 | Extractor MUST identify constructor calls and create Calls dependencies |
| FR-017c-30 | Extractor MUST link symbols by resolving names to symbol IDs |

### Graph Operations (FR-017c-31 to FR-017c-38)

| ID | Requirement |
|----|-------------|
| FR-017c-31 | Graph MUST support getting direct dependencies of a symbol |
| FR-017c-32 | Graph MUST support getting direct dependents of a symbol |
| FR-017c-33 | Graph MUST support getting transitive dependencies with depth limit |
| FR-017c-34 | Graph MUST support getting transitive dependents with depth limit |
| FR-017c-35 | Graph MUST detect cycles and mark them without infinite loops |
| FR-017c-36 | Graph MUST support finding shortest path between two symbols |
| FR-017c-37 | Graph MUST enforce configurable maximum depth for traversals |
| FR-017c-38 | Graph MUST support filtering traversal results by dependency kind |

### Retrieval APIs (FR-017c-39 to FR-017c-45)

| ID | Requirement |
|----|-------------|
| FR-017c-39 | API MUST provide GetDependencies(symbolId) returning direct dependencies |
| FR-017c-40 | API MUST provide GetDependents(symbolId) returning direct dependents |
| FR-017c-41 | API MUST provide GetCallGraph(symbolId, depth) returning call tree |
| FR-017c-42 | API MUST provide GetTypeHierarchy(symbolId) returning inheritance chain |
| FR-017c-43 | API MUST provide GetImplementors(interfaceId) returning implementing classes |
| FR-017c-44 | API MUST provide GetUsages(symbolId) returning all reference locations |
| FR-017c-45 | API MUST provide FindPath(fromId, toId) returning dependency path if exists |

### Update Management (FR-017c-46 to FR-017c-50)

| ID | Requirement |
|----|-------------|
| FR-017c-46 | System MUST update dependencies when source file changes |
| FR-017c-47 | System MUST remove stale edges when symbols are deleted |
| FR-017c-48 | System MUST add new edges when symbols are added |
| FR-017c-49 | System MUST support incremental updates (file-level granularity) |
| FR-017c-50 | System MUST support full rebuild of dependency graph |

### Index Management (FR-017c-51 to FR-017c-54)

| ID | Requirement |
|----|-------------|
| FR-017c-51 | Store MUST index source-target pairs for bidirectional queries |
| FR-017c-52 | Store MUST index dependencies by kind for filtered queries |
| FR-017c-53 | Store MUST index dependencies by file for incremental updates |
| FR-017c-54 | Store MUST optimize index structure for graph traversal patterns |

---

## Non-Functional Requirements

### Performance (NFR-017c-01 to NFR-017c-08)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017c-01 | Performance | Direct dependency query MUST complete in < 10ms |
| NFR-017c-02 | Performance | Direct dependent query MUST complete in < 10ms |
| NFR-017c-03 | Performance | Transitive query (depth 5) MUST complete in < 100ms |
| NFR-017c-04 | Performance | Batch insert MUST achieve < 0.1ms per edge |
| NFR-017c-05 | Performance | System MUST handle graphs with 1 million edges |
| NFR-017c-06 | Performance | Index rebuild MUST NOT block read queries |
| NFR-017c-07 | Performance | Incremental update MUST be < 100ms per file |
| NFR-017c-08 | Performance | Path finding MUST use efficient graph algorithms (BFS/Dijkstra) |

### Reliability (NFR-017c-09 to NFR-017c-14)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017c-09 | Reliability | System MUST handle cycles without infinite loops |
| NFR-017c-10 | Reliability | System MUST handle queries for missing symbols gracefully |
| NFR-017c-11 | Reliability | Database state MUST remain consistent after crashes |
| NFR-017c-12 | Reliability | Database transactions MUST be atomic for batch operations |
| NFR-017c-13 | Reliability | Concurrent read/write MUST NOT cause data corruption |
| NFR-017c-14 | Reliability | System MUST recover from database lock timeouts |

### Security (NFR-017c-15 to NFR-017c-18)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017c-15 | Security | Symbol IDs MUST be validated before database queries |
| NFR-017c-16 | Security | Query depth limits MUST be enforced to prevent resource exhaustion |
| NFR-017c-17 | Security | File paths MUST be stored as relative paths only |
| NFR-017c-18 | Security | SQL injection MUST be prevented via parameterized queries |

### Maintainability (NFR-017c-19 to NFR-017c-22)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017c-19 | Maintainability | Domain interfaces MUST be independent of persistence implementation |
| NFR-017c-20 | Maintainability | All public APIs MUST have XML documentation |
| NFR-017c-21 | Maintainability | Database schema MUST be versioned with migrations |
| NFR-017c-22 | Maintainability | Unit test coverage MUST exceed 80% |

### Observability (NFR-017c-23 to NFR-017c-26)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017c-23 | Observability | Query execution time MUST be logged for performance monitoring |
| NFR-017c-24 | Observability | Edge count and graph statistics MUST be available via metrics |
| NFR-017c-25 | Observability | Cycle detection events MUST be logged with involved symbols |
| NFR-017c-26 | Observability | Database maintenance operations MUST be logged |

---

## User Manual Documentation

### Overview

Dependency mapping tracks relationships between code symbols. Retrieval APIs query these relationships to find usages, callers, and related code.

### Configuration

```yaml
# .agent/config.yml
symbol_index:
  dependencies:
    # Enable dependency mapping
    enabled: true
    
    # Relationship types to track
    kinds:
      - calls
      - uses
      - inherits
      - implements
      - references
      
    # Transitive query settings
    transitive:
      # Maximum depth for transitive queries
      max_depth: 10
      
      # Maximum nodes to return
      max_nodes: 1000
```

### Dependency Kinds

| Kind | Description | Example |
|------|-------------|---------|
| Calls | Method/function call | `service.GetUser()` |
| Uses | Type usage | `UserService service` |
| Inherits | Class inheritance | `class Admin : User` |
| Implements | Interface implementation | `class Repo : IRepo` |
| References | Any symbol reference | Property, field access |

### CLI Commands

```bash
# Get dependencies of a symbol
acode deps of UserService

# Get dependents of a symbol
acode deps on UserService

# Get call graph
acode deps calls UserService.GetById --depth 3

# Get type hierarchy
acode deps hierarchy UserService

# Get interface implementors
acode deps implementors IUserRepository

# Find path between symbols
acode deps path UserController UserRepository
```

### Query Examples

```bash
# What does UserService depend on?
$ acode deps of UserService

Dependencies of UserService:
  Uses:
    - IUserRepository (interface)
    - ILogger<UserService> (interface)
  Calls:
    - UserRepository.GetById (method)
    - UserRepository.Save (method)
    - Logger.LogInformation (method)

# What uses UserService?
$ acode deps on UserService

Dependents of UserService:
  Uses:
    - UserController (class)
    - UserFacade (class)
  Calls:
    - UserController.GetUser → UserService.GetById
    - UserController.CreateUser → UserService.Create

# Show call graph
$ acode deps calls UserController.GetUser --depth 2

Call Graph (depth 2):
UserController.GetUser
├── UserService.GetById
│   ├── UserRepository.GetById
│   │   └── DbContext.Users.Find
│   └── Logger.LogDebug
└── Mapper.MapToDto
```

### API Usage

```csharp
// Get direct dependencies
var deps = await dependencyService.GetDependenciesAsync(symbolId);

// Get direct dependents
var dependents = await dependencyService.GetDependentsAsync(symbolId);

// Get transitive dependencies (depth 3)
var transitive = await dependencyService.GetTransitiveDependenciesAsync(
    symbolId, 
    maxDepth: 3);

// Get all usages of a symbol
var usages = await dependencyService.GetUsagesAsync(symbolId);

// Get implementors of an interface
var implementors = await dependencyService.GetImplementorsAsync(interfaceId);

// Find path between symbols
var path = await dependencyService.FindPathAsync(fromId, toId);
```

### Troubleshooting

#### Missing Dependencies

**Problem:** Some dependencies not found

**Solutions:**
1. Ensure symbol extraction completed
2. Rebuild dependency graph
3. Check dependency kinds config
4. Verify symbols are in index

#### Slow Queries

**Problem:** Transitive queries slow

**Solutions:**
1. Reduce max_depth
2. Reduce max_nodes
3. Filter by kind
4. Use direct queries

#### Cycles Detected

**Problem:** Circular dependency warning

**Solutions:**
1. This is a code design issue
2. Queries still work correctly
3. Review and refactor if needed

---

## Acceptance Criteria

### Model

- [ ] AC-001: IDependency defined
- [ ] AC-002: All kinds supported
- [ ] AC-003: Metadata stored

### Store

- [ ] AC-004: CRUD works
- [ ] AC-005: Queries work
- [ ] AC-006: Persistence works

### Extraction

- [ ] AC-007: Calls extracted
- [ ] AC-008: Uses extracted
- [ ] AC-009: Inherits extracted
- [ ] AC-010: Implements extracted

### Graph

- [ ] AC-011: Direct queries work
- [ ] AC-012: Transitive works
- [ ] AC-013: Cycles handled

### APIs

- [ ] AC-014: GetDependencies works
- [ ] AC-015: GetDependents works
- [ ] AC-016: GetUsages works
- [ ] AC-017: FindPath works

### CLI

- [ ] AC-018: deps of works
- [ ] AC-019: deps on works
- [ ] AC-020: deps calls works

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Symbols/Dependencies/
├── DependencyModelTests.cs
│   ├── Should_Store_Source_Id()
│   ├── Should_Store_Target_Id()
│   ├── Should_Store_Kind()
│   ├── Should_Store_Location()
│   ├── Should_Support_Calls_Kind()
│   ├── Should_Support_Uses_Kind()
│   ├── Should_Support_Inherits_Kind()
│   ├── Should_Support_Implements_Kind()
│   ├── Should_Support_Contains_Kind()
│   └── Should_Support_References_Kind()
│
├── DependencyStoreTests.cs
│   ├── Should_Add_Single_Dependency()
│   ├── Should_Add_Batch_Dependencies()
│   ├── Should_Remove_Dependency()
│   ├── Should_Remove_By_Source()
│   ├── Should_Remove_By_Target()
│   ├── Should_Query_By_Source()
│   ├── Should_Query_By_Target()
│   ├── Should_Query_By_Kind()
│   ├── Should_Query_By_File()
│   ├── Should_Handle_Empty_Store()
│   └── Should_Handle_No_Matches()
│
├── DependencyExtractorTests.cs
│   ├── Should_Extract_Method_Calls()
│   ├── Should_Extract_Constructor_Calls()
│   ├── Should_Extract_Property_Access()
│   ├── Should_Extract_Field_Access()
│   ├── Should_Extract_Type_Usage()
│   ├── Should_Extract_Base_Class()
│   ├── Should_Extract_Interface_Implementation()
│   ├── Should_Extract_Generic_Type_Args()
│   ├── Should_Track_Call_Location()
│   ├── Should_Handle_Chained_Calls()
│   ├── Should_Handle_Conditional_Access()
│   └── Should_Handle_Lambda_Calls()
│
├── DependencyGraphTests.cs
│   ├── Should_Get_Direct_Dependencies()
│   ├── Should_Get_Direct_Dependents()
│   ├── Should_Get_Transitive_Depth_1()
│   ├── Should_Get_Transitive_Depth_2()
│   ├── Should_Get_Transitive_Depth_N()
│   ├── Should_Respect_Max_Depth()
│   ├── Should_Respect_Max_Nodes()
│   ├── Should_Handle_Cycles()
│   ├── Should_Detect_Cycle()
│   ├── Should_Not_Infinite_Loop()
│   ├── Should_Filter_By_Kind()
│   └── Should_Return_Graph_Structure()
│
├── UsageFinderTests.cs
│   ├── Should_Find_All_Usages()
│   ├── Should_Return_Usage_Location()
│   ├── Should_Return_Usage_Context()
│   ├── Should_Find_Read_Usages()
│   ├── Should_Find_Write_Usages()
│   └── Should_Find_Call_Usages()
│
├── ImplementorFinderTests.cs
│   ├── Should_Find_Interface_Implementors()
│   ├── Should_Find_Base_Class_Subclasses()
│   ├── Should_Find_Abstract_Implementors()
│   └── Should_Handle_Multiple_Levels()
│
└── PathFinderTests.cs
    ├── Should_Find_Direct_Path()
    ├── Should_Find_Shortest_Path()
    ├── Should_Return_Empty_If_No_Path()
    ├── Should_Handle_Multiple_Paths()
    └── Should_Respect_Max_Path_Length()
```

### Integration Tests

```
Tests/Integration/Symbols/Dependencies/
├── DependencyStoreIntegrationTests.cs
│   ├── Should_Persist_To_Database()
│   ├── Should_Load_From_Database()
│   ├── Should_Handle_Large_Graph()
│   └── Should_Handle_Concurrent_Updates()
│
├── DependencyExtractionIntegrationTests.cs
│   ├── Should_Extract_From_CSharp()
│   ├── Should_Extract_From_TypeScript()
│   ├── Should_Extract_Cross_File()
│   └── Should_Handle_Real_Codebase()
│
└── DependencyGraphIntegrationTests.cs
    ├── Should_Build_Real_Graph()
    ├── Should_Query_Large_Graph()
    └── Should_Handle_Complex_Cycles()
```

### E2E Tests

```
Tests/E2E/Symbols/Dependencies/
├── DependencyE2ETests.cs
│   ├── Should_Query_Deps_Of_Via_CLI()
│   ├── Should_Query_Deps_On_Via_CLI()
│   ├── Should_Show_Call_Graph_Via_CLI()
│   ├── Should_Work_With_Agent_Context()
│   └── Should_Provide_Context_For_Refactoring()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Direct query | 5ms | 10ms |
| Transitive depth 3 | 50ms | 100ms |
| Insert 10K edges | 500ms | 1000ms |

---

## User Verification Steps

### Scenario 1: Get Dependencies

1. Index a project
2. Run `acode deps of MyClass`
3. Verify: Dependencies listed

### Scenario 2: Get Dependents

1. Index a project
2. Run `acode deps on MyClass`
3. Verify: Dependents listed

### Scenario 3: Call Graph

1. Index a project
2. Run `acode deps calls MyMethod`
3. Verify: Call tree shown

### Scenario 4: Find Path

1. Index a project
2. Run `acode deps path A B`
3. Verify: Path shown if exists

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Symbols/
│   ├── IDependency.cs
│   ├── Dependency.cs
│   ├── DependencyKind.cs
│   ├── IDependencyStore.cs
│   └── IDependencyGraph.cs
│
src/AgenticCoder.Infrastructure/
├── Symbols/
│   └── Dependencies/
│       ├── DependencyStore.cs
│       ├── DependencyExtractor.cs
│       ├── DependencyGraph.cs
│       └── DependencyQueryService.cs
```

### IDependency Interface

```csharp
namespace AgenticCoder.Domain.Symbols;

public interface IDependency
{
    Guid Id { get; }
    Guid SourceSymbolId { get; }
    Guid TargetSymbolId { get; }
    DependencyKind Kind { get; }
    SymbolLocation? Location { get; }
}
```

### DependencyKind Enum

```csharp
public enum DependencyKind
{
    Calls,
    Uses,
    Inherits,
    Implements,
    References
}
```

### IDependencyGraph Interface

```csharp
public interface IDependencyGraph
{
    Task<IReadOnlyList<IDependency>> GetDependenciesAsync(
        Guid symbolId, 
        CancellationToken ct = default);
        
    Task<IReadOnlyList<IDependency>> GetDependentsAsync(
        Guid symbolId, 
        CancellationToken ct = default);
        
    Task<IReadOnlyList<IDependency>> GetTransitiveDependenciesAsync(
        Guid symbolId, 
        int maxDepth,
        CancellationToken ct = default);
        
    Task<IReadOnlyList<Guid>?> FindPathAsync(
        Guid fromId, 
        Guid toId,
        CancellationToken ct = default);
        
    Task<bool> HasCycleAsync(
        Guid symbolId,
        CancellationToken ct = default);
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-DEP-001 | Symbol not found |
| ACODE-DEP-002 | Query error |
| ACODE-DEP-003 | Cycle detected |
| ACODE-DEP-004 | Depth exceeded |

### Implementation Checklist

1. [ ] Create dependency model
2. [ ] Create dependency store
3. [ ] Create dependency extractor
4. [ ] Create dependency graph
5. [ ] Implement retrieval APIs
6. [ ] Add cycle detection
7. [ ] Add CLI commands
8. [ ] Add unit tests

### Rollout Plan

1. **Phase 1:** Dependency model and store
2. **Phase 2:** Dependency extraction
3. **Phase 3:** Graph operations
4. **Phase 4:** Retrieval APIs
5. **Phase 5:** CLI integration

---

**End of Task 017.c Specification**