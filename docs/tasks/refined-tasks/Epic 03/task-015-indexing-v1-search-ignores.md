# Task 015: Indexing v1 (Search + Ignores)

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 014 (RepoFS)  

---

## Description

Task 015 implements the first version of repository indexing. Indexing makes files searchable. The agent uses search to find relevant code. Good search enables good context selection.

The index supports full-text search. Search for words, phrases, or patterns. Find files containing specific content. Rank results by relevance.

Ignore rules filter what gets indexed. Build artifacts are excluded. Dependencies are excluded. Binary files are excluded. This keeps the index focused on source code.

Gitignore integration respects existing conventions. If the project has a .gitignore, those rules apply. Additional rules can be configured. No duplicate configuration needed.

The index is persistent. It survives restarts. It loads quickly on startup. It updates incrementally when files change.

Search is fast. Sub-second for typical queries. Even large codebases search quickly. The index is optimized for code patterns.

File metadata is also indexed. File paths are searchable. File types are filterable. Modified dates are tracked.

The index integrates with the tool system. Search tools query the index. Results feed the context packer. This is the foundation for intelligent context selection.

All indexing is local. No external APIs. Works offline. Works in air-gapped environments. This aligns with Task 001 constraints.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Index | Searchable data structure |
| Full-Text Search | Search by content |
| Ignore Rules | Exclusion patterns |
| Gitignore | Git ignore file |
| Incremental | Update changed only |
| Persistent | Survives restart |
| Ranking | Order by relevance |
| Tokenization | Break into words |
| Stemming | Word root extraction |
| Inverted Index | Term to document map |
| Query | Search request |
| Result | Search match |
| Relevance | Match quality score |
| Filter | Limit results |
| Pagination | Result batching |

---

## Out of Scope

The following items are explicitly excluded from Task 015:

- **Semantic search** - Embedding-based (v2)
- **Symbol indexing** - Task 017
- **Real-time watching** - Manual refresh
- **Distributed index** - Single machine
- **Full regex** - Simple patterns only
- **Fuzzy matching** - Exact matching v1
- **Index sharding** - Single index file
- **Compression** - Raw storage

---

## Functional Requirements

### Index Creation

- FR-001: CreateIndexAsync MUST work
- FR-002: Index all text files
- FR-003: Skip ignored files
- FR-004: Store in persistent file
- FR-005: Track file metadata

### Search

- FR-006: SearchAsync MUST work
- FR-007: Word search MUST work
- FR-008: Phrase search MUST work
- FR-009: Pattern search MUST work
- FR-010: Case-insensitive default

### Results

- FR-011: Return file paths
- FR-012: Return line numbers
- FR-013: Return snippets
- FR-014: Return relevance score
- FR-015: Pagination support

### Ignore Rules

- FR-016: .gitignore MUST work
- FR-017: Custom ignores MUST work
- FR-018: Pattern matching MUST work
- FR-019: Negation MUST work
- FR-020: Directory ignores MUST work

### Incremental Updates

- FR-021: Detect file changes
- FR-022: Update changed files
- FR-023: Remove deleted files
- FR-024: Add new files
- FR-025: Efficient updates

### Filtering

- FR-026: Filter by file type
- FR-027: Filter by directory
- FR-028: Filter by size
- FR-029: Filter by date
- FR-030: Combine filters

### Index Management

- FR-031: RebuildIndexAsync MUST work
- FR-032: ClearIndexAsync MUST work
- FR-033: GetIndexStatsAsync MUST work
- FR-034: Index persistence
- FR-035: Index loading

### Query Parsing

- FR-036: Simple word queries
- FR-037: Quoted phrases
- FR-038: Wildcard patterns
- FR-039: AND/OR operators
- FR-040: Exclusion (-)

---

## Non-Functional Requirements

### Performance

- NFR-001: Index 10K files in < 30s
- NFR-002: Search < 100ms
- NFR-003: Incremental update < 5s
- NFR-004: Index load < 500ms

### Reliability

- NFR-005: Corruption recovery
- NFR-006: Atomic updates
- NFR-007: Graceful degradation

### Storage

- NFR-008: Index < 10% of source size
- NFR-009: Efficient storage
- NFR-010: Compaction

---

## User Manual Documentation

### Overview

The indexing system makes your codebase searchable. The agent uses search to find relevant files for context.

### Building the Index

```bash
$ acode index build

Building index...
  Scanning files...
  Found: 1,234 files
  Ignored: 456 files (gitignore)
  Indexing: 1,234 files
    [====================] 100%

Index built:
  Files: 1,234
  Size: 2.3 MB
  Time: 8.5s
```

### Searching

```bash
$ acode search "UserService"

Found 15 results:

1. [src/Services/UserService.cs] (score: 0.95)
   Line 1: public class UserService : IUserService
   Line 25: public async Task<User> GetUserAsync(int id)
   Line 45: public async Task<User> CreateUserAsync(CreateUserRequest request)

2. [src/Controllers/UserController.cs] (score: 0.87)
   Line 12: private readonly IUserService _userService;
   Line 20: _userService = userService;

3. [tests/UserServiceTests.cs] (score: 0.82)
   Line 8: public class UserServiceTests
```

### Search Syntax

```bash
# Simple word
acode search "controller"

# Phrase (exact)
acode search '"user service"'

# Wildcard
acode search "User*"

# AND (default)
acode search "user create"

# OR
acode search "user OR customer"

# Exclude
acode search "user -test"

# Filter by type
acode search "controller" --ext cs

# Filter by directory
acode search "api" --dir src/Controllers
```

### Configuration

```yaml
# .agent/config.yml
index:
  # Index file location
  path: .agent/index.db
  
  # Additional ignore patterns
  ignore:
    - "*.generated.cs"
    - "obj/**"
    - "bin/**"
    
  # File type filters
  include_extensions:
    - .cs
    - .ts
    - .js
    - .py
    - .md
    
  # Size limits
  max_file_size_kb: 500
```

### Index Management

```bash
# Check index status
$ acode index status

Index Status
────────────────────
Files indexed: 1,234
Index size: 2.3 MB
Last updated: 2024-01-20 14:30:00
Pending updates: 5 files

# Update incrementally
$ acode index update

Updating index...
  Changed: 3 files
  New: 2 files
  Deleted: 0 files

Index updated.

# Rebuild from scratch
$ acode index rebuild

Rebuilding index...
```

### Troubleshooting

#### Search Returns Nothing

**Problem:** No results for known content

**Solutions:**
1. Rebuild index: `acode index rebuild`
2. Check ignore rules
3. Verify file type is indexed

#### Index Build Slow

**Problem:** Indexing takes too long

**Solutions:**
1. Add ignore patterns for large directories
2. Limit file extensions
3. Reduce max_file_size_kb

#### Index Corrupt

**Problem:** Search crashes or errors

**Solutions:**
1. Delete index: `rm .agent/index.db`
2. Rebuild: `acode index rebuild`

---

## Acceptance Criteria

### Index Creation

- [ ] AC-001: Index builds
- [ ] AC-002: Text files indexed
- [ ] AC-003: Ignores respected
- [ ] AC-004: Persists to disk

### Search

- [ ] AC-005: Word search works
- [ ] AC-006: Phrase search works
- [ ] AC-007: Pattern search works
- [ ] AC-008: Results ranked

### Ignore Rules

- [ ] AC-009: Gitignore works
- [ ] AC-010: Custom ignores work
- [ ] AC-011: Patterns match

### Updates

- [ ] AC-012: Incremental works
- [ ] AC-013: Deletes handled
- [ ] AC-014: Adds handled

### CLI

- [ ] AC-015: Search command works
- [ ] AC-016: Index build works
- [ ] AC-017: Index status works

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Index/
├── IndexBuilderTests.cs
│   ├── Should_Index_Text_Files()
│   ├── Should_Skip_Ignored()
│   └── Should_Persist()
│
├── SearchTests.cs
│   ├── Should_Find_Word()
│   ├── Should_Find_Phrase()
│   └── Should_Rank_Results()
│
├── IgnoreRulesTests.cs
│   ├── Should_Parse_Gitignore()
│   ├── Should_Match_Patterns()
│   └── Should_Handle_Negation()
│
└── IncrementalUpdateTests.cs
    ├── Should_Detect_Changes()
    └── Should_Update_Efficiently()
```

### Integration Tests

```
Tests/Integration/Index/
├── IndexIntegrationTests.cs
│   ├── Should_Build_Large_Index()
│   └── Should_Search_Real_Repo()
```

### E2E Tests

```
Tests/E2E/Index/
├── IndexE2ETests.cs
│   └── Should_Work_With_Agent()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Index 1K files | 3s | 5s |
| Index 10K files | 20s | 30s |
| Search | 50ms | 100ms |
| Incremental | 2s | 5s |

---

## User Verification Steps

### Scenario 1: Build Index

1. Run `acode index build`
2. Verify: Index file created
3. Verify: Stats accurate

### Scenario 2: Search

1. Search for known term
2. Verify: Results found
3. Verify: Correct files

### Scenario 3: Ignore

1. Add ignore pattern
2. Rebuild index
3. Verify: Pattern excluded

### Scenario 4: Update

1. Modify file
2. Run update
3. Verify: Changes indexed

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Index/
│   ├── IIndexService.cs
│   ├── SearchResult.cs
│   ├── SearchQuery.cs
│   └── IndexStats.cs
│
src/AgenticCoder.Infrastructure/
├── Index/
│   ├── IndexService.cs
│   ├── IndexBuilder.cs
│   ├── SearchEngine.cs
│   ├── IgnoreRuleParser.cs
│   └── IncrementalUpdater.cs
│
src/AgenticCoder.CLI/
└── Commands/
    ├── IndexCommand.cs
    └── SearchCommand.cs
```

### IIndexService Interface

```csharp
namespace AgenticCoder.Domain.Index;

public interface IIndexService
{
    Task BuildAsync(CancellationToken ct);
    Task UpdateAsync(CancellationToken ct);
    Task RebuildAsync(CancellationToken ct);
    Task<IReadOnlyList<SearchResult>> SearchAsync(SearchQuery query, CancellationToken ct);
    Task<IndexStats> GetStatsAsync(CancellationToken ct);
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-IDX-001 | Build failed |
| ACODE-IDX-002 | Search failed |
| ACODE-IDX-003 | Update failed |
| ACODE-IDX-004 | Corrupt index |
| ACODE-IDX-005 | Parse error |

### Implementation Checklist

1. [ ] Create index service
2. [ ] Implement builder
3. [ ] Implement search
4. [ ] Implement ignores
5. [ ] Implement updates
6. [ ] Add persistence
7. [ ] Add CLI commands
8. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Index building
2. **Phase 2:** Search
3. **Phase 3:** Ignores
4. **Phase 4:** Incremental
5. **Phase 5:** CLI

---

**End of Task 015 Specification**