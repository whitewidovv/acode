# Task 017.c: Dependency Mapping + Retrieval APIs

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 017 (Symbol Index), Task 017.a (C# Extraction), Task 017.b (TS/JS Extraction)  

---

## Description

Task 017.c implements dependency mapping and retrieval APIs. Dependency mapping tracks relationships between symbols. Retrieval APIs enable querying these relationships.

Code understanding requires relationship knowledge. When the agent modifies `UserService`, it needs to know what depends on `UserService`. Dependency mapping provides this.

Relationships are directional. `UserController` uses `UserService`. `UserService` is used by `UserController`. Both directions are valuable. Forward: "what does this use?" Reverse: "what uses this?"

Dependency types vary. Method calls are dependencies. Type references are dependencies. Inheritance creates dependencies. Interface implementations create dependencies.

Static analysis discovers dependencies. Task 017.a and 017.b extract symbols. This task links them. Cross-language dependencies are out of scope.

The dependency graph is persistent. It survives restarts. Incremental updates maintain it. Changes to files update affected relationships.

Retrieval APIs serve the context packer. When including a class, the packer can include callers. When the agent edits a method, it can see usages.

APIs support multiple query patterns. Get all dependencies of a symbol. Get all dependents of a symbol. Get the full dependency chain. Filter by relationship type.

Performance is critical. The graph can have millions of edges. Queries must be fast. Indexing and caching are essential.

The graph supports cycle detection. Circular dependencies exist in real code. The API handles them gracefully. No infinite loops in traversal.

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

### Dependency Model

- FR-001: Define IDependency interface
- FR-002: Define DependencyKind enum
- FR-003: Support calls relationship
- FR-004: Support uses relationship
- FR-005: Support inherits relationship
- FR-006: Support implements relationship
- FR-007: Support references relationship
- FR-008: Store source symbol ID
- FR-009: Store target symbol ID
- FR-010: Store relationship kind
- FR-011: Store source location

### Dependency Store

- FR-012: Define IDependencyStore interface
- FR-013: Add dependencies
- FR-014: Remove dependencies
- FR-015: Remove by source symbol
- FR-016: Remove by target symbol
- FR-017: Query by source
- FR-018: Query by target
- FR-019: Query by kind
- FR-020: Batch operations
- FR-021: Persist to database
- FR-022: Load from database

### Dependency Extraction

- FR-023: Extract method calls
- FR-024: Extract property accesses
- FR-025: Extract field accesses
- FR-026: Extract type references
- FR-027: Extract base class
- FR-028: Extract interfaces
- FR-029: Extract constructor calls
- FR-030: Link symbols by name

### Graph Operations

- FR-031: Get direct dependencies
- FR-032: Get direct dependents
- FR-033: Get transitive dependencies
- FR-034: Get transitive dependents
- FR-035: Detect cycles
- FR-036: Get shortest path
- FR-037: Limit depth
- FR-038: Filter by kind

### Retrieval APIs

- FR-039: GetDependencies(symbolId)
- FR-040: GetDependents(symbolId)
- FR-041: GetCallGraph(symbolId, depth)
- FR-042: GetTypeHierarchy(symbolId)
- FR-043: GetImplementors(interfaceId)
- FR-044: GetUsages(symbolId)
- FR-045: FindPath(fromId, toId)

### Update Management

- FR-046: Update on file change
- FR-047: Remove stale edges
- FR-048: Add new edges
- FR-049: Incremental update
- FR-050: Full rebuild

### Index Management

- FR-051: Index source-target pairs
- FR-052: Index by kind
- FR-053: Index by file
- FR-054: Optimize for traversal

---

## Non-Functional Requirements

### Performance

- NFR-001: Query direct deps < 10ms
- NFR-002: Query dependents < 10ms
- NFR-003: Transitive query < 100ms
- NFR-004: Batch insert < 0.1ms/edge
- NFR-005: Handle 1M edges

### Reliability

- NFR-006: Handle cycles
- NFR-007: Handle missing symbols
- NFR-008: Consistent state
- NFR-009: Crash recovery

### Accuracy

- NFR-010: No duplicate edges
- NFR-011: Correct direction
- NFR-012: Correct kinds
- NFR-013: Stale edge cleanup

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
├── DependencyTests.cs
│   ├── Should_Store_Metadata()
│   └── Should_Support_All_Kinds()
│
├── DependencyStoreTests.cs
│   ├── Should_Add_Dependency()
│   ├── Should_Remove_Dependency()
│   ├── Should_Query_By_Source()
│   └── Should_Query_By_Target()
│
├── DependencyExtractorTests.cs
│   ├── Should_Extract_Calls()
│   ├── Should_Extract_Uses()
│   └── Should_Extract_Inherits()
│
└── DependencyGraphTests.cs
    ├── Should_Get_Direct()
    ├── Should_Get_Transitive()
    └── Should_Handle_Cycles()
```

### Integration Tests

```
Tests/Integration/Symbols/Dependencies/
├── DependencyStoreIntegrationTests.cs
│   └── Should_Persist_And_Load()
│
└── DependencyGraphIntegrationTests.cs
    └── Should_Build_Real_Graph()
```

### E2E Tests

```
Tests/E2E/Symbols/Dependencies/
├── DependencyE2ETests.cs
│   ├── Should_Query_Via_CLI()
│   └── Should_Show_Call_Graph()
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