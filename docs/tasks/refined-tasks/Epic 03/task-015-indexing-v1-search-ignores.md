# Task 015: Indexing v1 (Search + Ignores)

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 014 (RepoFS), Task 002 (Config Contract), Task 003 (DI Container)  

---

## Description

### Business Value

Repository indexing is fundamental to the agent's ability to understand and navigate code. Without indexing, every search would require scanning all files sequentially—an O(n) operation that becomes impractical for repositories with thousands of files.

The indexing system provides:

1. **Fast Code Discovery:** Sub-second search across any repository size. The agent can quickly find relevant code, enabling intelligent context selection that fits within token limits.

2. **Smart Exclusion:** Ignore rules prevent indexing of build artifacts, dependencies, and generated files. This focuses the agent on actual source code, improving search relevance and reducing noise.

3. **Incremental Updates:** Only changed files are re-indexed, making updates fast and efficient. The agent always works with current code without waiting for full re-indexing.

4. **Offline Capability:** All indexing is local. Works without network access. Complies with air-gapped mode requirements from Task 001.

5. **Foundation for Intelligence:** This index is the data source for Task 016 (Context Packing) and Task 017 (Symbol Indexing). Quality context selection depends on quality search.

### Scope

This task implements version 1 of the indexing system:

1. **Index Builder:** Scans repository files, tokenizes content, and builds searchable inverted index. Respects ignore rules.

2. **Search Engine:** Queries the index with word, phrase, and pattern searches. Returns ranked results with file paths, line numbers, and snippets.

3. **Ignore Rule Parser:** Parses .gitignore, .agentignore, and configuration-based patterns. Combines rules for unified exclusion.

4. **Incremental Updater:** Detects file changes and updates only affected index entries. Efficient delta updates.

5. **Persistence Layer:** Stores index to disk for fast startup. Handles corruption recovery.

6. **CLI Commands:** Build, update, rebuild, status, and search commands for manual interaction.

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Task 014 (RepoFS) | File Access | All file reads go through RepoFS |
| Task 002 (Config) | Configuration | Index settings in `.agent/config.yml` under `index` section |
| Task 003 (DI) | Dependency Injection | IIndexService registered as singleton |
| Task 016 (Context) | Data Source | Context packer uses search to select files |
| Task 017 (Symbols) | Foundation | Symbol index builds on file index |
| Task 025 (Search Tool) | Tool Implementation | Search tool queries the index |
| Task 003.c (Audit) | Audit Logging | Index operations are audited |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Index file corrupted | Search unavailable | Corruption detection, auto-rebuild |
| Disk full during indexing | Partial index | Transaction-based writes, cleanup |
| File access denied | File not indexed | Log warning, continue with others |
| Memory exhausted | Indexing crashes | Streaming processing, memory limits |
| Invalid ignore pattern | Build fails | Validation, skip invalid with warning |
| Encoding detection fails | Garbled tokens | Default UTF-8, mark as binary if binary content |
| Concurrent update conflict | Inconsistent index | File locking, retry |
| Search timeout | Query hangs | Query timeout, partial results |

### Assumptions

1. Repository contains primarily text files (source code, docs)
2. Binary files can be detected and skipped
3. File content fits in memory for tokenization (< 10MB typical)
4. Index file can be stored in `.agent` directory
5. File system supports atomic rename (for safe persistence)
6. UTF-8 is the predominant encoding
7. .gitignore patterns follow Git specification
8. Repository size is < 1 million files
9. Single agent instance accesses the index
10. Index can be rebuilt from source if corrupted

### Security Considerations

The indexing system handles repository content and must ensure:

1. **Content Protection:** Index files MUST NOT be more accessible than source files. Index permissions SHOULD match repository permissions.

2. **Path Validation:** All file paths MUST be validated through RepoFS. No direct file system access.

3. **Ignore Sensitive Files:** Patterns like `.env`, `secrets.yml` SHOULD be ignored by default.

4. **Query Sanitization:** Search queries MUST be sanitized to prevent injection or DoS attacks.

5. **Memory Limits:** Indexing MUST have memory bounds to prevent resource exhaustion attacks via large files.

6. **Audit Trail:** Index build and search operations SHOULD be logged for troubleshooting without exposing content.

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

### Index Service Interface (FR-015-01 to FR-015-15)

| ID | Requirement |
|----|-------------|
| FR-015-01 | System MUST define IIndexService interface |
| FR-015-02 | IIndexService MUST have BuildAsync method |
| FR-015-03 | BuildAsync MUST scan all repository files |
| FR-015-04 | BuildAsync MUST respect ignore rules |
| FR-015-05 | BuildAsync MUST report progress |
| FR-015-06 | IIndexService MUST have UpdateAsync method |
| FR-015-07 | UpdateAsync MUST only process changed files |
| FR-015-08 | IIndexService MUST have RebuildAsync method |
| FR-015-09 | RebuildAsync MUST clear and rebuild from scratch |
| FR-015-10 | IIndexService MUST have SearchAsync method |
| FR-015-11 | SearchAsync MUST accept SearchQuery parameter |
| FR-015-12 | SearchAsync MUST return IReadOnlyList<SearchResult> |
| FR-015-13 | IIndexService MUST have GetStatsAsync method |
| FR-015-14 | GetStatsAsync MUST return IndexStats |
| FR-015-15 | All methods MUST support CancellationToken |

### Index Building (FR-015-16 to FR-015-35)

| ID | Requirement |
|----|-------------|
| FR-015-16 | Builder MUST enumerate files via RepoFS |
| FR-015-17 | Builder MUST detect and skip binary files |
| FR-015-18 | Builder MUST detect file encoding |
| FR-015-19 | Builder MUST read file content |
| FR-015-20 | Builder MUST tokenize content |
| FR-015-21 | Tokenization MUST handle code identifiers |
| FR-015-22 | Tokenization MUST split CamelCase |
| FR-015-23 | Tokenization MUST split snake_case |
| FR-015-24 | Tokenization MUST normalize case |
| FR-015-25 | Builder MUST track line numbers |
| FR-015-26 | Builder MUST store file metadata |
| FR-015-27 | Metadata MUST include file path |
| FR-015-28 | Metadata MUST include file size |
| FR-015-29 | Metadata MUST include last modified time |
| FR-015-30 | Builder MUST create inverted index |
| FR-015-31 | Inverted index MUST map terms to documents |
| FR-015-32 | Index MUST store term positions |
| FR-015-33 | Index MUST support term frequency |
| FR-015-34 | Build MUST be atomic (complete or rollback) |
| FR-015-35 | Build errors MUST NOT corrupt existing index |

### Search Operations (FR-015-36 to FR-015-60)

| ID | Requirement |
|----|-------------|
| FR-015-36 | Search MUST support single word queries |
| FR-015-37 | Search MUST support multiple word queries |
| FR-015-38 | Multiple words MUST default to AND |
| FR-015-39 | Search MUST support OR operator |
| FR-015-40 | Search MUST support exact phrase (quoted) |
| FR-015-41 | Search MUST support exclusion (-term) |
| FR-015-42 | Search MUST support wildcard suffix (*) |
| FR-015-43 | Search MUST support wildcard prefix (*) |
| FR-015-44 | Search MUST be case-insensitive by default |
| FR-015-45 | Search MUST support case-sensitive option |
| FR-015-46 | Search MUST return matched file paths |
| FR-015-47 | Search MUST return matched line numbers |
| FR-015-48 | Search MUST return context snippets |
| FR-015-49 | Snippets MUST include surrounding lines |
| FR-015-50 | Search MUST return relevance score |
| FR-015-51 | Results MUST be ranked by relevance |
| FR-015-52 | Ranking MUST consider term frequency |
| FR-015-53 | Ranking MUST consider term position |
| FR-015-54 | Search MUST support pagination |
| FR-015-55 | Pagination MUST accept skip and take |
| FR-015-56 | Search MUST return total count |
| FR-015-57 | Search MUST handle empty query |
| FR-015-58 | Empty query MUST return empty results |
| FR-015-59 | Search MUST timeout after configurable period |
| FR-015-60 | Timeout MUST return partial results with warning |

### Ignore Rules (FR-015-61 to FR-015-80)

| ID | Requirement |
|----|-------------|
| FR-015-61 | System MUST parse .gitignore files |
| FR-015-62 | Parser MUST handle comment lines |
| FR-015-63 | Parser MUST handle blank lines |
| FR-015-64 | Parser MUST handle exact file patterns |
| FR-015-65 | Parser MUST handle glob patterns (*) |
| FR-015-66 | Parser MUST handle double glob (**) |
| FR-015-67 | Parser MUST handle directory patterns (/) |
| FR-015-68 | Parser MUST handle negation patterns (!) |
| FR-015-69 | Parser MUST handle escaped characters |
| FR-015-70 | System MUST parse .agentignore files |
| FR-015-71 | .agentignore MUST take precedence over .gitignore |
| FR-015-72 | System MUST support config-based ignores |
| FR-015-73 | Config ignores MUST take highest precedence |
| FR-015-74 | System MUST handle nested ignore files |
| FR-015-75 | Nested rules MUST apply to subdirectories |
| FR-015-76 | Rules MUST be applied in order |
| FR-015-77 | Later rules MUST override earlier |
| FR-015-78 | Invalid patterns MUST be skipped with warning |
| FR-015-79 | System MUST cache parsed rules |
| FR-015-80 | Cache MUST invalidate on file change |

### Incremental Updates (FR-015-81 to FR-015-95)

| ID | Requirement |
|----|-------------|
| FR-015-81 | Update MUST detect modified files |
| FR-015-82 | Detection MUST use file modification time |
| FR-015-83 | Detection MUST use file size |
| FR-015-84 | Detection MAY use file hash |
| FR-015-85 | Update MUST detect new files |
| FR-015-86 | Update MUST detect deleted files |
| FR-015-87 | Update MUST detect renamed files |
| FR-015-88 | Modified files MUST be re-indexed |
| FR-015-89 | New files MUST be added to index |
| FR-015-90 | Deleted files MUST be removed from index |
| FR-015-91 | Renamed files MUST update path |
| FR-015-92 | Update MUST preserve unaffected entries |
| FR-015-93 | Update MUST be transactional |
| FR-015-94 | Failed update MUST NOT corrupt index |
| FR-015-95 | Update MUST track last update timestamp |

### Search Filtering (FR-015-96 to FR-015-110)

| ID | Requirement |
|----|-------------|
| FR-015-96 | Search MUST support file extension filter |
| FR-015-97 | Extension filter MUST accept multiple values |
| FR-015-98 | Search MUST support directory filter |
| FR-015-99 | Directory filter MUST support recursive |
| FR-015-100 | Search MUST support file size filter |
| FR-015-101 | Size filter MUST support min and max |
| FR-015-102 | Search MUST support date filter |
| FR-015-103 | Date filter MUST support since/before |
| FR-015-104 | Search MUST support combining filters |
| FR-015-105 | Filters MUST apply before search |
| FR-015-106 | Filters MUST improve search performance |
| FR-015-107 | Empty filter MUST search all files |
| FR-015-108 | Invalid filter MUST return error |
| FR-015-109 | Filter MUST support exclude patterns |
| FR-015-110 | Exclude MUST remove files from results |

### Persistence (FR-015-111 to FR-015-125)

| ID | Requirement |
|----|-------------|
| FR-015-111 | Index MUST persist to disk |
| FR-015-112 | Persist location MUST be configurable |
| FR-015-113 | Default location MUST be .agent/index.db |
| FR-015-114 | Persistence MUST be atomic |
| FR-015-115 | Atomic write MUST use temp file + rename |
| FR-015-116 | Index MUST load on startup |
| FR-015-117 | Load MUST validate index integrity |
| FR-015-118 | Invalid index MUST trigger rebuild |
| FR-015-119 | Index MUST include version number |
| FR-015-120 | Version mismatch MUST trigger rebuild |
| FR-015-121 | Index MUST include checksum |
| FR-015-122 | Checksum mismatch MUST trigger rebuild |
| FR-015-123 | Index format MUST be documented |
| FR-015-124 | Index MUST support compaction |
| FR-015-125 | Compaction MUST reclaim deleted space |

---

## Non-Functional Requirements

### Performance (NFR-015-01 to NFR-015-20)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015-01 | Performance | Index 1,000 files MUST complete in < 5s |
| NFR-015-02 | Performance | Index 10,000 files MUST complete in < 30s |
| NFR-015-03 | Performance | Index 100,000 files MUST complete in < 5 min |
| NFR-015-04 | Performance | Simple word search MUST return in < 50ms |
| NFR-015-05 | Performance | Complex query search MUST return in < 100ms |
| NFR-015-06 | Performance | Phrase search MUST return in < 150ms |
| NFR-015-07 | Performance | Wildcard search MUST return in < 200ms |
| NFR-015-08 | Performance | Incremental update for 10 files MUST complete in < 2s |
| NFR-015-09 | Performance | Incremental update for 100 files MUST complete in < 5s |
| NFR-015-10 | Performance | Index load from disk MUST complete in < 500ms |
| NFR-015-11 | Performance | Index load for 100MB index MUST complete in < 2s |
| NFR-015-12 | Performance | Ignore pattern matching MUST be O(1) per file |
| NFR-015-13 | Performance | Memory usage during indexing MUST be < 500MB |
| NFR-015-14 | Performance | Memory usage for loaded index MUST be < 100MB |
| NFR-015-15 | Performance | Search MUST use streaming for large result sets |
| NFR-015-16 | Performance | Tokenization MUST process 1MB/s minimum |
| NFR-015-17 | Performance | Index writes MUST use buffered I/O |
| NFR-015-18 | Performance | Search MUST NOT block index updates |
| NFR-015-19 | Performance | Updates MUST use reader-writer locks |
| NFR-015-20 | Performance | Compaction MUST complete in < index build time |

### Reliability (NFR-015-21 to NFR-015-35)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015-21 | Reliability | Index corruption MUST be detected on load |
| NFR-015-22 | Reliability | Corruption MUST trigger automatic rebuild |
| NFR-015-23 | Reliability | Build interruption MUST NOT corrupt index |
| NFR-015-24 | Reliability | Update interruption MUST NOT corrupt index |
| NFR-015-25 | Reliability | Out of disk space MUST be handled gracefully |
| NFR-015-26 | Reliability | Large file (>10MB) MUST NOT crash indexer |
| NFR-015-27 | Reliability | Binary file MUST be skipped without error |
| NFR-015-28 | Reliability | Encoding errors MUST be handled gracefully |
| NFR-015-29 | Reliability | File access errors MUST be logged and skipped |
| NFR-015-30 | Reliability | Concurrent searches MUST be safe |
| NFR-015-31 | Reliability | Search during update MUST return consistent results |
| NFR-015-32 | Reliability | Index version mismatch MUST trigger rebuild |
| NFR-015-33 | Reliability | Stale index MUST be detected |
| NFR-015-34 | Reliability | Stale detection MUST check root directory mtime |
| NFR-015-35 | Reliability | Recovery from crash MUST be automatic |

### Security (NFR-015-36 to NFR-015-45)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015-36 | Security | Index file MUST have same permissions as repo |
| NFR-015-37 | Security | Index MUST NOT expose content outside search |
| NFR-015-38 | Security | Search queries MUST be sanitized |
| NFR-015-39 | Security | Regex patterns MUST have complexity limits |
| NFR-015-40 | Security | Default ignores MUST exclude secrets |
| NFR-015-41 | Security | .env files MUST be ignored by default |
| NFR-015-42 | Security | *secret* patterns MUST be ignored by default |
| NFR-015-43 | Security | Index operations MUST be audited |
| NFR-015-44 | Security | Audit MUST NOT log search content |
| NFR-015-45 | Security | File paths in logs MUST be relative |

### Maintainability (NFR-015-46 to NFR-015-55)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015-46 | Maintainability | Index format MUST be versioned |
| NFR-015-47 | Maintainability | Format version upgrade MUST be automatic |
| NFR-015-48 | Maintainability | All public APIs MUST have XML docs |
| NFR-015-49 | Maintainability | Code coverage MUST be > 80% |
| NFR-015-50 | Maintainability | Cyclomatic complexity MUST be < 10 |
| NFR-015-51 | Maintainability | Single responsibility per class |
| NFR-015-52 | Maintainability | Dependencies MUST be injected |
| NFR-015-53 | Maintainability | Configuration MUST be documented |
| NFR-015-54 | Maintainability | Error codes MUST be documented |
| NFR-015-55 | Maintainability | Platform-specific code MUST be isolated |

### Observability (NFR-015-56 to NFR-015-65)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015-56 | Observability | Build progress MUST be logged |
| NFR-015-57 | Observability | Search operations MUST be logged at Debug |
| NFR-015-58 | Observability | Errors MUST be logged at Error level |
| NFR-015-59 | Observability | Metrics MUST track files indexed |
| NFR-015-60 | Observability | Metrics MUST track search latency |
| NFR-015-61 | Observability | Metrics MUST track search count |
| NFR-015-62 | Observability | Metrics MUST track cache hit rate |
| NFR-015-63 | Observability | Metrics MUST track index size |
| NFR-015-64 | Observability | Structured logging MUST be used |
| NFR-015-65 | Observability | Correlation IDs MUST be propagated |

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

## Best Practices

### Index Design

1. **Incremental by default** - Only re-index changed files; full rebuild is opt-in
2. **Store metadata separately** - Keep file content separate from search index
3. **Version the index format** - Include format version for backward compatibility
4. **Compress aggressively** - Trade CPU for smaller index size on disk

### Search Quality

5. **Normalize before indexing** - Consistent tokenization for query and content
6. **Support partial matches** - Prefix, suffix, and fuzzy matching options
7. **Rank by relevance** - Most relevant results first based on scoring
8. **Limit result set** - Return top N results; pagination for more

### Performance

9. **Build index in background** - Don't block user operations during indexing
10. **Throttle I/O** - Respect system resources during large index builds
11. **Cancel gracefully** - Stop indexing cleanly when user requests
12. **Cache hot paths** - Keep frequently searched patterns in memory

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Index/
├── IndexBuilderTests.cs
│   ├── Should_Index_Text_File()
│   ├── Should_Index_Multiple_Files()
│   ├── Should_Skip_Binary_Files()
│   ├── Should_Skip_Ignored_Files()
│   ├── Should_Track_File_Metadata()
│   ├── Should_Store_Line_Numbers()
│   ├── Should_Tokenize_Content()
│   ├── Should_Handle_Empty_File()
│   ├── Should_Handle_Large_File()
│   ├── Should_Handle_Unicode_Content()
│   ├── Should_Persist_Index_To_Disk()
│   ├── Should_Load_Index_From_Disk()
│   └── Should_Handle_Corrupted_Index_File()
│
├── SearchEngineTests.cs
│   ├── Should_Find_Single_Word()
│   ├── Should_Find_Multiple_Words_AND()
│   ├── Should_Find_Multiple_Words_OR()
│   ├── Should_Find_Exact_Phrase()
│   ├── Should_Find_With_Wildcard_Suffix()
│   ├── Should_Find_With_Wildcard_Prefix()
│   ├── Should_Exclude_With_Minus()
│   ├── Should_Handle_Case_Insensitive()
│   ├── Should_Handle_Case_Sensitive()
│   ├── Should_Return_Line_Numbers()
│   ├── Should_Return_Snippets()
│   ├── Should_Return_Relevance_Score()
│   ├── Should_Rank_By_Relevance()
│   ├── Should_Support_Pagination()
│   ├── Should_Handle_No_Results()
│   ├── Should_Handle_Empty_Query()
│   └── Should_Handle_Invalid_Query()
│
├── SearchQueryParserTests.cs
│   ├── Should_Parse_Single_Word()
│   ├── Should_Parse_Multiple_Words()
│   ├── Should_Parse_Quoted_Phrase()
│   ├── Should_Parse_Wildcard()
│   ├── Should_Parse_AND_Operator()
│   ├── Should_Parse_OR_Operator()
│   ├── Should_Parse_Exclusion()
│   ├── Should_Parse_Combined_Operators()
│   ├── Should_Handle_Special_Characters()
│   └── Should_Handle_Unbalanced_Quotes()
│
├── IgnoreRulesTests.cs
│   ├── Should_Parse_Gitignore_File()
│   ├── Should_Parse_Empty_Gitignore()
│   ├── Should_Parse_Comment_Lines()
│   ├── Should_Match_Exact_Filename()
│   ├── Should_Match_Glob_Pattern()
│   ├── Should_Match_Directory_Pattern()
│   ├── Should_Match_Double_Star()
│   ├── Should_Handle_Negation_Pattern()
│   ├── Should_Handle_Escaped_Characters()
│   ├── Should_Apply_Order_Priority()
│   ├── Should_Merge_Multiple_Ignore_Files()
│   ├── Should_Apply_Custom_Ignores()
│   └── Should_Handle_Trailing_Spaces()
│
├── IncrementalUpdaterTests.cs
│   ├── Should_Detect_Modified_File()
│   ├── Should_Detect_New_File()
│   ├── Should_Detect_Deleted_File()
│   ├── Should_Detect_Renamed_File()
│   ├── Should_Update_Only_Changed()
│   ├── Should_Remove_Deleted_From_Index()
│   ├── Should_Add_New_To_Index()
│   ├── Should_Handle_Concurrent_Changes()
│   └── Should_Track_Last_Update_Timestamp()
│
├── FilterTests.cs
│   ├── Should_Filter_By_Extension()
│   ├── Should_Filter_By_Directory()
│   ├── Should_Filter_By_Size()
│   ├── Should_Filter_By_Date()
│   ├── Should_Combine_Filters()
│   └── Should_Handle_No_Filters()
│
└── TokenizerTests.cs
    ├── Should_Tokenize_Code_Identifiers()
    ├── Should_Tokenize_CamelCase()
    ├── Should_Tokenize_Snake_Case()
    ├── Should_Handle_Numbers()
    ├── Should_Handle_Punctuation()
    └── Should_Normalize_Tokens()
```

### Integration Tests

```
Tests/Integration/Index/
├── IndexBuildIntegrationTests.cs
│   ├── Should_Build_Index_For_Small_Repo()
│   ├── Should_Build_Index_For_Large_Repo()
│   ├── Should_Respect_Gitignore()
│   ├── Should_Handle_Nested_Gitignores()
│   └── Should_Handle_Symlinks()
│
├── SearchIntegrationTests.cs
│   ├── Should_Search_Real_Codebase()
│   ├── Should_Return_Correct_Line_Numbers()
│   ├── Should_Handle_Concurrent_Searches()
│   └── Should_Search_During_Update()
│
├── IncrementalIntegrationTests.cs
│   ├── Should_Update_After_File_Edit()
│   ├── Should_Update_After_File_Create()
│   ├── Should_Update_After_File_Delete()
│   └── Should_Handle_Many_Simultaneous_Changes()
│
└── PersistenceIntegrationTests.cs
    ├── Should_Survive_Restart()
    ├── Should_Recover_From_Corruption()
    └── Should_Handle_Disk_Full()
```

### E2E Tests

```
Tests/E2E/Index/
├── IndexE2ETests.cs
│   ├── Should_Build_Index_Via_CLI()
│   ├── Should_Search_Via_CLI()
│   ├── Should_Update_Index_Via_CLI()
│   ├── Should_Rebuild_Index_Via_CLI()
│   ├── Should_Show_Index_Status_Via_CLI()
│   ├── Should_Work_With_Agent_Search_Tool()
│   └── Should_Provide_Context_To_Agent()
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