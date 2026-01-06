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

**Return on Investment (ROI) Analysis:**

| Metric | Before (Manual Search) | After (Indexed Search) | Savings |
|--------|------------------------|------------------------|---------|
| Time to find code reference | 2-5 minutes per search | < 1 second per search | 99% reduction |
| Developer searches per day | 30-50 searches | 30-50 searches | Same volume |
| Daily time savings | 0 | 75-125 minutes/day | $62.50-$104/day at $50/hr |
| Monthly time savings per developer | 0 | 25-42 hours/month | $1,250-$2,100/month |
| Annual time savings per developer | 0 | 300-500 hours/year | $15,000-$25,000/year |
| 10-developer team annual savings | 0 | 3,000-5,000 hours/year | **$150,000-$250,000/year** |

**Additional Business Value:**
- **Context Quality Improvement:** Indexed search enables TF-IDF ranking, ensuring the most relevant code is included in LLM context, improving code generation accuracy by 40-60%
- **Token Budget Optimization:** Fast search enables intelligent file selection, reducing wasted tokens on irrelevant files by 60-80%, translating to $2,000-$5,000/year in LLM API costs for active users
- **Developer Experience:** Sub-second search maintains flow state; studies show context switching costs 23 minutes to recover from each interruption
- **Compliance Readiness:** Audit trail of searches enables security reviews and incident investigation

**Total Quantified Annual Value: $152,000-$255,000 for a 10-developer team**

The indexing system provides:

1. **Fast Code Discovery:** Sub-second search across any repository size. The agent can quickly find relevant code, enabling intelligent context selection that fits within token limits.

2. **Smart Exclusion:** Ignore rules prevent indexing of build artifacts, dependencies, and generated files. This focuses the agent on actual source code, improving search relevance and reducing noise.

3. **Incremental Updates:** Only changed files are re-indexed, making updates fast and efficient. The agent always works with current code without waiting for full re-indexing.

4. **Offline Capability:** All indexing is local. Works without network access. Complies with air-gapped mode requirements from Task 001.

5. **Foundation for Intelligence:** This index is the data source for Task 016 (Context Packing) and Task 017 (Symbol Indexing). Quality context selection depends on quality search.

### Technical Architecture

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              INDEXING SUBSYSTEM                                 │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                           CLI LAYER                                      │   │
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐        │   │
│  │  │ index build │ │ index update│ │ index status│ │   search    │        │   │
│  │  └──────┬──────┘ └──────┬──────┘ └──────┬──────┘ └──────┬──────┘        │   │
│  └─────────┼───────────────┼───────────────┼───────────────┼────────────────┘   │
│            │               │               │               │                    │
│  ┌─────────▼───────────────▼───────────────▼───────────────▼────────────────┐   │
│  │                      APPLICATION LAYER                                    │   │
│  │  ┌───────────────────────────────────────────────────────────────────┐   │   │
│  │  │                      IIndexService                                 │   │   │
│  │  │  BuildAsync() │ UpdateAsync() │ SearchAsync() │ GetStatsAsync()   │   │   │
│  │  └───────────────────────────────────────────────────────────────────┘   │   │
│  └──────────────────────────────────────────────────────────────────────────┘   │
│                                      │                                          │
│  ┌───────────────────────────────────▼──────────────────────────────────────┐   │
│  │                       DOMAIN LAYER                                        │   │
│  │  ┌────────────────┐ ┌────────────────┐ ┌────────────────┐                │   │
│  │  │  SearchQuery   │ │  SearchResult  │ │   IndexStats   │                │   │
│  │  │  - Terms       │ │  - FilePath    │ │  - FileCount   │                │   │
│  │  │  - Filters     │ │  - LineNumber  │ │  - IndexSize   │                │   │
│  │  │  - Options     │ │  - Snippet     │ │  - LastUpdated │                │   │
│  │  │  - Pagination  │ │  - Score       │ │  - TokenCount  │                │   │
│  │  └────────────────┘ └────────────────┘ └────────────────┘                │   │
│  └──────────────────────────────────────────────────────────────────────────┘   │
│                                      │                                          │
│  ┌───────────────────────────────────▼──────────────────────────────────────┐   │
│  │                     INFRASTRUCTURE LAYER                                  │   │
│  │                                                                           │   │
│  │  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐       │   │
│  │  │  IndexBuilder   │    │  SearchEngine   │    │ IgnoreRuleParser│       │   │
│  │  │  - ScanFiles()  │    │  - Query()      │    │  - Parse()      │       │   │
│  │  │  - Tokenize()   │    │  - Rank()       │    │  - Match()      │       │   │
│  │  │  - StoreIndex() │    │  - Paginate()   │    │  - Merge()      │       │   │
│  │  └─────────────────┘    └─────────────────┘    └─────────────────┘       │   │
│  │                                                                           │   │
│  │  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐       │   │
│  │  │IncrementalUpdater│   │  Tokenizer      │    │ IndexPersistence│       │   │
│  │  │  - DetectChanges│    │  - Split()      │    │  - Save()       │       │   │
│  │  │  - ApplyDelta() │    │  - Normalize()  │    │  - Load()       │       │   │
│  │  │  - TrackMtime() │    │  - Stem()       │    │  - Validate()   │       │   │
│  │  └─────────────────┘    └─────────────────┘    └─────────────────┘       │   │
│  │                                                                           │   │
│  └──────────────────────────────────────────────────────────────────────────┘   │
│                                      │                                          │
│  ┌───────────────────────────────────▼──────────────────────────────────────┐   │
│  │                       STORAGE LAYER                                       │   │
│  │  ┌─────────────────────────────────────────────────────────────────────┐ │   │
│  │  │                    .agent/index.db (SQLite)                         │ │   │
│  │  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌────────────┐  │ │   │
│  │  │  │   files      │ │    terms     │ │  postings    │ │  metadata  │  │ │   │
│  │  │  │  - path      │ │  - term      │ │  - term_id   │ │  - version │  │ │   │
│  │  │  │  - size      │ │  - doc_freq  │ │  - file_id   │ │  - checksum│  │ │   │
│  │  │  │  - mtime     │ │              │ │  - positions │ │  - built_at│  │ │   │
│  │  │  │  - hash      │ │              │ │  - tf        │ │            │  │ │   │
│  │  │  └──────────────┘ └──────────────┘ └──────────────┘ └────────────┘  │ │   │
│  │  └─────────────────────────────────────────────────────────────────────┘ │   │
│  └──────────────────────────────────────────────────────────────────────────┘   │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Index Data Flow

```
                         BUILD FLOW
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   RepoFS    │────▶│IgnoreFilter │────▶│  Tokenizer  │────▶│ IndexWriter │
│ ListFiles() │     │  ShouldIndex│     │ Tokenize()  │     │  Store()    │
└─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘
      │                   │                   │                   │
      ▼                   ▼                   ▼                   ▼
 [All Files]        [Source Only]       [Term Stream]      [Inverted Index]
   5,000 files        1,200 files       ~500K tokens        2.3 MB SQLite

                         SEARCH FLOW
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│ QueryParser │────▶│ IndexReader │────▶│   Ranker    │────▶│  Formatter  │
│  Parse()    │     │  Lookup()   │     │  Score()    │     │  Format()   │
└─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘
      │                   │                   │                   │
      ▼                   ▼                   ▼                   ▼
 [SearchQuery]       [Postings]        [Ranked Docs]      [SearchResults]
  "user create"     [doc1,doc2,...]   [(doc1,0.95),...]   JSON/Console
```

### Inverted Index Structure

```
TERM: "userservice"
┌──────────────────────────────────────────────────────────────────────────┐
│ Document Frequency: 15                                                    │
├──────────────────────────────────────────────────────────────────────────┤
│ POSTINGS LIST                                                            │
│ ┌────────────────────────────────────────────────────────────────────┐  │
│ │ File: src/Services/UserService.cs                                  │  │
│ │   Positions: [1, 25, 45, 78]  TF: 4  Line Numbers: [1, 25, 45, 78] │  │
│ ├────────────────────────────────────────────────────────────────────┤  │
│ │ File: src/Controllers/UserController.cs                            │  │
│ │   Positions: [12, 20]         TF: 2  Line Numbers: [12, 20]        │  │
│ ├────────────────────────────────────────────────────────────────────┤  │
│ │ File: tests/UserServiceTests.cs                                    │  │
│ │   Positions: [8, 15, 30]      TF: 3  Line Numbers: [8, 15, 30]     │  │
│ └────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────┘
```

### Architectural Decisions

| Decision | Choice | Rationale | Trade-offs |
|----------|--------|-----------|------------|
| Storage Format | SQLite | ACID transactions, corruption recovery, built-in FTS5 support, zero-configuration | Slightly larger than custom binary format (+15-20%), but robust and queryable |
| Tokenization | Custom Code-Aware | Must handle camelCase, snake_case, and code identifiers; stemming optional | More complex than standard NLP tokenizers, but essential for code search quality |
| Ignore Parsing | Git-Compatible | Developers expect gitignore patterns to work identically | Complex glob parsing required, but essential for user expectations |
| Incremental Updates | Mtime + Size | Fast detection without content hashing for most cases | May miss same-size modifications (rare), hash fallback available |
| Ranking Algorithm | BM25 | Industry standard for text search, proven effective | More complex than simple TF-IDF, but significantly better relevance |
| Concurrency Model | Reader-Writer Lock | Multiple concurrent searches, single writer for updates | Updates block during heavy search load, acceptable for local use |

### Trade-offs and Alternatives Considered

**1. Storage: SQLite vs Custom Binary Format vs Lucene.NET**
- **Chosen:** SQLite with FTS5
- **Rationale:** Built-in full-text search, ACID transactions, corruption recovery, cross-platform, zero-configuration. FTS5 provides efficient inverted index implementation.
- **Rejected Alternatives:**
  - Custom binary format: Faster reads but requires custom corruption recovery, no query language, high maintenance burden
  - Lucene.NET: Powerful but heavyweight (10MB+ runtime), complex configuration, overkill for local single-user scenario
- **Trade-off:** Accept 15-20% larger index size in exchange for reliability and maintainability

**2. Tokenization: Code-Aware vs Standard NLP vs Regex-Based**
- **Chosen:** Custom code-aware tokenizer
- **Rationale:** Code has unique patterns (camelCase, snake_case, namespaces) that standard NLP tokenizers miss. "getUserById" must tokenize to ["get", "user", "by", "id"].
- **Rejected Alternatives:**
  - Standard NLP (NLTK-style): Poor handling of code identifiers, treats "getUserById" as single token
  - Regex-based: Fragile, hard to maintain, inconsistent across edge cases
- **Trade-off:** Higher implementation complexity but 60% better recall for code-specific searches

**3. Ignore Parsing: Full Git Compatibility vs Simplified Subset**
- **Chosen:** Full Git compatibility
- **Rationale:** Developers expect .gitignore patterns to work identically. Partial compatibility causes confusion and support burden.
- **Rejected Alternatives:**
  - Simplified subset: Easier to implement but "why doesn't my pattern work?" becomes #1 support issue
- **Trade-off:** Complex glob parsing implementation but zero user confusion

**4. Ranking: BM25 vs TF-IDF vs Simple Term Frequency**
- **Chosen:** BM25 (Okapi BM25)
- **Rationale:** Industry standard with proven effectiveness. Handles document length normalization better than TF-IDF.
- **Rejected Alternatives:**
  - Simple TF: Poor quality, no length normalization, short files always win
  - TF-IDF: Good but BM25 is strictly better for similar implementation cost
- **Trade-off:** Slightly more complex scoring function but measurably better relevance

**5. Incremental Detection: Mtime vs Content Hash vs File System Watcher**
- **Chosen:** Mtime + Size with optional hash fallback
- **Rationale:** Fast detection for 99% of cases. Hash fallback for edge cases. FS watcher requires persistent process.
- **Rejected Alternatives:**
  - Content hash only: Too slow for large repositories (must read every file)
  - File system watcher: Requires persistent daemon, complex OS-specific code, misses changes when agent not running
- **Trade-off:** May miss rare same-size same-mtime changes, but 100x faster detection

### Performance Characteristics

| Operation | Small Repo (1K files) | Medium Repo (10K files) | Large Repo (100K files) |
|-----------|----------------------|------------------------|------------------------|
| Full Build | < 3 seconds | < 20 seconds | < 5 minutes |
| Incremental Update (10 files) | < 500 ms | < 1 second | < 2 seconds |
| Simple Search | < 20 ms | < 50 ms | < 100 ms |
| Complex Search (phrase + filters) | < 50 ms | < 100 ms | < 200 ms |
| Index Load from Disk | < 100 ms | < 300 ms | < 2 seconds |
| Memory Usage (Loaded Index) | < 20 MB | < 50 MB | < 100 MB |

### Error Codes and Recovery

| Code | Error | Automatic Recovery | Manual Recovery |
|------|-------|-------------------|-----------------|
| ACODE-IDX-001 | Index build failed | Retry with exponential backoff | `acode index rebuild` |
| ACODE-IDX-002 | Search query failed | Return empty results with warning | Check query syntax |
| ACODE-IDX-003 | Incremental update failed | Mark index stale, prompt rebuild | `acode index rebuild` |
| ACODE-IDX-004 | Index file corrupted | Delete and rebuild automatically | Delete `.agent/index.db` |
| ACODE-IDX-005 | Ignore pattern parse error | Skip invalid pattern, log warning | Fix pattern in config |
| ACODE-IDX-006 | File access denied | Skip file, continue indexing | Check file permissions |
| ACODE-IDX-007 | Disk full during indexing | Abort cleanly, preserve old index | Free disk space |
| ACODE-IDX-008 | Query timeout exceeded | Return partial results | Simplify query |
| ACODE-IDX-009 | Memory limit exceeded | Stream processing, skip large files | Increase memory limit or exclude large files |
| ACODE-IDX-010 | Version mismatch | Automatic rebuild | None required |

### Observability and Metrics

```
Metrics Emitted:
  acode_index_build_duration_seconds{status="success|failure"}
  acode_index_files_indexed_total
  acode_index_files_skipped_total{reason="binary|ignored|error"}
  acode_index_size_bytes
  acode_index_search_duration_seconds
  acode_index_search_results_total
  acode_index_update_files_changed_total
  acode_index_cache_hit_ratio

Structured Log Events:
  - IndexBuildStarted: {files_discovered, ignore_patterns_loaded}
  - IndexBuildCompleted: {files_indexed, duration_ms, index_size_bytes}
  - IndexBuildFailed: {error_code, error_message, files_processed}
  - SearchExecuted: {query_hash, results_count, duration_ms}
  - IncrementalUpdateCompleted: {files_added, files_modified, files_deleted, duration_ms}
```

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
11. SQLite library is available and up-to-date
12. File system timestamps have at least second-precision
13. Index file is stored on local disk (not network share)
14. User has write access to .agent directory
15. Maximum search query length is 1,000 characters
16. Default timeout values are appropriate for target hardware
17. Memory limits can be configured based on system resources
18. Concurrent search operations are rare (< 10 simultaneous)
19. Index compaction can be scheduled during low-activity periods
20. Ignore patterns are validated before use

### Security Considerations

The indexing system handles repository content and presents several security risks that must be mitigated:

#### Threat 1: Query Injection via Malicious Search Input

**Threat Description:** An attacker could craft a malicious search query containing SQL injection payloads, regex bombs, or specially crafted patterns designed to cause denial of service or extract data from the index database.

**Attack Vector:** User input flows directly to index query without sanitization. FTS5 MATCH syntax could be exploited. Regex patterns with catastrophic backtracking could hang the search.

**Impact:** High - Could cause service denial, expose internal index structure, or bypass intended search restrictions.

**Mitigation - Complete C# Implementation:**

```csharp
using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Index;

/// <summary>
/// Sanitizes search queries to prevent injection attacks and DoS via regex bombs.
/// </summary>
public sealed class SearchQuerySanitizer
{
    private readonly ILogger<SearchQuerySanitizer> _logger;
    
    // Maximum query length to prevent memory exhaustion
    private const int MaxQueryLength = 1000;
    
    // Maximum number of terms to prevent combinatorial explosion
    private const int MaxTermCount = 50;
    
    // Maximum wildcard count to prevent expensive queries
    private const int MaxWildcards = 5;
    
    // Dangerous patterns that could cause SQL injection in FTS5
    private static readonly string[] DangerousPatterns = new[]
    {
        "MATCH", "NEAR", "AND", "OR", "NOT", "--", ";", "/*", "*/",
        "UNION", "SELECT", "INSERT", "UPDATE", "DELETE", "DROP",
        "CREATE", "ALTER", "EXEC", "EXECUTE", "xp_", "sp_"
    };
    
    // Regex patterns that could cause catastrophic backtracking
    private static readonly Regex RegexBombPattern = new(
        @"(\*{2,})|(\+{2,})|(\?{3,})|((.+)+)|((a+)+b)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));
    
    public SearchQuerySanitizer(ILogger<SearchQuerySanitizer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public SanitizationResult Sanitize(string rawQuery)
    {
        if (string.IsNullOrWhiteSpace(rawQuery))
        {
            return SanitizationResult.Empty();
        }
        
        // Length check
        if (rawQuery.Length > MaxQueryLength)
        {
            _logger.LogWarning(
                "Query truncated from {OriginalLength} to {MaxLength} characters",
                rawQuery.Length, MaxQueryLength);
            rawQuery = rawQuery[..MaxQueryLength];
        }
        
        // Remove null bytes and control characters
        var sanitized = RemoveControlCharacters(rawQuery);
        
        // Check for SQL injection patterns
        foreach (var pattern in DangerousPatterns)
        {
            if (sanitized.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Potentially dangerous pattern removed from query: {Pattern}",
                    pattern);
                sanitized = Regex.Replace(
                    sanitized, 
                    Regex.Escape(pattern), 
                    "", 
                    RegexOptions.IgnoreCase);
            }
        }
        
        // Check for regex bombs
        try
        {
            if (RegexBombPattern.IsMatch(sanitized))
            {
                _logger.LogWarning("Potential regex bomb detected and neutralized");
                sanitized = NeutralizeRegexBomb(sanitized);
            }
        }
        catch (RegexMatchTimeoutException)
        {
            _logger.LogWarning("Regex bomb check timed out, applying aggressive sanitization");
            sanitized = AggressiveSanitize(sanitized);
        }
        
        // Count and limit wildcards
        var wildcardCount = sanitized.Count(c => c == '*' || c == '?');
        if (wildcardCount > MaxWildcards)
        {
            _logger.LogWarning(
                "Query contains {Count} wildcards, limiting to {Max}",
                wildcardCount, MaxWildcards);
            sanitized = LimitWildcards(sanitized, MaxWildcards);
        }
        
        // Count and limit terms
        var terms = sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (terms.Length > MaxTermCount)
        {
            _logger.LogWarning(
                "Query contains {Count} terms, limiting to {Max}",
                terms.Length, MaxTermCount);
            sanitized = string.Join(" ", terms.Take(MaxTermCount));
        }
        
        return new SanitizationResult(sanitized, rawQuery != sanitized);
    }
    
    private static string RemoveControlCharacters(string input)
    {
        return new string(input.Where(c => !char.IsControl(c) || c == ' ').ToArray());
    }
    
    private static string NeutralizeRegexBomb(string input)
    {
        // Replace repeated quantifiers with single instances
        return Regex.Replace(input, @"([*+?])\1+", "$1");
    }
    
    private static string AggressiveSanitize(string input)
    {
        // Keep only alphanumeric, spaces, quotes, and single wildcards
        return new string(input.Where(c => 
            char.IsLetterOrDigit(c) || c == ' ' || c == '"' || c == '*').ToArray());
    }
    
    private static string LimitWildcards(string input, int max)
    {
        var count = 0;
        return new string(input.Where(c =>
        {
            if (c == '*' || c == '?')
            {
                count++;
                return count <= max;
            }
            return true;
        }).ToArray());
    }
}

public readonly record struct SanitizationResult(string SanitizedQuery, bool WasModified)
{
    public static SanitizationResult Empty() => new(string.Empty, false);
    public bool IsEmpty => string.IsNullOrWhiteSpace(SanitizedQuery);
}
```

---

#### Threat 2: Sensitive Data Exposure via Index Content

**Threat Description:** The index stores tokenized content from all indexed files. If secrets, API keys, or passwords appear in source files, they will be tokenized and searchable, potentially exposing sensitive data to unauthorized searches.

**Attack Vector:** Attacker searches for common secret patterns ("api_key", "password", "secret") and extracts sensitive values from snippets. Even partial matches can reveal credential structure.

**Impact:** Critical - Could expose production secrets, API keys, database passwords, or private keys.

**Mitigation - Complete C# Implementation:**

```csharp
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Index;

/// <summary>
/// Filters sensitive content from index entries to prevent secret exposure.
/// Applies default and configurable patterns before content is tokenized.
/// </summary>
public sealed class SensitiveContentFilter
{
    private readonly ILogger<SensitiveContentFilter> _logger;
    private readonly List<CompiledPattern> _patterns;
    
    // Default patterns for common secret formats
    private static readonly (string Name, string Pattern)[] DefaultPatterns = new[]
    {
        // API Keys
        ("AWS Access Key", @"AKIA[0-9A-Z]{16}"),
        ("AWS Secret Key", @"(?i)aws_secret_access_key\s*[=:]\s*[A-Za-z0-9/+=]{40}"),
        ("GitHub Token", @"ghp_[a-zA-Z0-9]{36}"),
        ("GitHub OAuth", @"gho_[a-zA-Z0-9]{36}"),
        ("GitLab Token", @"glpat-[a-zA-Z0-9\-]{20}"),
        ("Slack Token", @"xox[baprs]-[0-9]{10,13}-[0-9]{10,13}-[a-zA-Z0-9]{24}"),
        ("Slack Webhook", @"https://hooks\.slack\.com/services/T[a-zA-Z0-9_]+/B[a-zA-Z0-9_]+/[a-zA-Z0-9_]+"),
        ("Stripe Key", @"sk_live_[0-9a-zA-Z]{24}"),
        ("Stripe Test Key", @"sk_test_[0-9a-zA-Z]{24}"),
        ("SendGrid Key", @"SG\.[a-zA-Z0-9]{22}\.[a-zA-Z0-9]{43}"),
        ("Twilio Key", @"SK[0-9a-fA-F]{32}"),
        ("OpenAI Key", @"sk-[a-zA-Z0-9]{48}"),
        ("Azure Key", @"(?i)azure[_\-]?(?:storage|subscription)?[_\-]?key\s*[=:]\s*[A-Za-z0-9+/=]{44,}"),
        
        // Generic patterns
        ("Password Assignment", @"(?i)password\s*[=:]\s*[""'][^""']{8,}[""']"),
        ("Secret Assignment", @"(?i)secret\s*[=:]\s*[""'][^""']{8,}[""']"),
        ("API Key Assignment", @"(?i)api[_\-]?key\s*[=:]\s*[""'][^""']{16,}[""']"),
        ("Connection String", @"(?i)(?:connection[_\-]?string|connstr)\s*[=:]\s*[""'][^""']+[""']"),
        ("Private Key Block", @"-----BEGIN (?:RSA |DSA |EC |OPENSSH )?PRIVATE KEY-----"),
        ("JWT Token", @"eyJ[A-Za-z0-9_-]+\.eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+"),
        ("Bearer Token", @"(?i)bearer\s+[a-zA-Z0-9_\-\.=]+"),
        
        // Database URLs
        ("Database URL", @"(?i)(?:mysql|postgres|mongodb|redis|amqp)://[^:]+:[^@]+@[^\s]+"),
    };
    
    public SensitiveContentFilter(ILogger<SensitiveContentFilter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _patterns = new List<CompiledPattern>();
        
        foreach (var (name, pattern) in DefaultPatterns)
        {
            try
            {
                var compiled = new Regex(
                    pattern, 
                    RegexOptions.Compiled, 
                    TimeSpan.FromMilliseconds(100));
                _patterns.Add(new CompiledPattern(name, compiled));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compile pattern {Name}: {Pattern}", name, pattern);
            }
        }
    }
    
    /// <summary>
    /// Filters sensitive content from a file before indexing.
    /// Returns the filtered content with secrets replaced by [REDACTED].
    /// </summary>
    public FilterResult FilterContent(string filePath, string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return new FilterResult(content, 0);
        }
        
        var redactionCount = 0;
        var filtered = content;
        
        foreach (var pattern in _patterns)
        {
            try
            {
                var matches = pattern.Regex.Matches(filtered);
                if (matches.Count > 0)
                {
                    redactionCount += matches.Count;
                    _logger.LogInformation(
                        "Redacted {Count} {PatternName} matches in {FilePath}",
                        matches.Count, pattern.Name, SanitizePath(filePath));
                    
                    filtered = pattern.Regex.Replace(filtered, "[REDACTED]");
                }
            }
            catch (RegexMatchTimeoutException)
            {
                _logger.LogWarning(
                    "Pattern {PatternName} timed out on {FilePath}, skipping",
                    pattern.Name, SanitizePath(filePath));
            }
        }
        
        return new FilterResult(filtered, redactionCount);
    }
    
    /// <summary>
    /// Checks if content should be completely excluded from indexing.
    /// Returns true for files likely to contain only sensitive data.
    /// </summary>
    public bool ShouldExcludeEntirely(string filePath)
    {
        var fileName = Path.GetFileName(filePath).ToLowerInvariant();
        
        // Files that should never be indexed
        var sensitiveFiles = new[]
        {
            ".env", ".env.local", ".env.production", ".env.development",
            "secrets.yaml", "secrets.yml", "secrets.json",
            ".npmrc", ".pypirc", ".netrc", ".pgpass",
            "id_rsa", "id_dsa", "id_ecdsa", "id_ed25519",
            "credentials.json", "service-account.json",
            ".htpasswd", "shadow", "passwd"
        };
        
        return sensitiveFiles.Contains(fileName) || 
               fileName.EndsWith(".pem") || 
               fileName.EndsWith(".key") ||
               fileName.EndsWith(".pfx") ||
               fileName.EndsWith(".p12");
    }
    
    private static string SanitizePath(string path)
    {
        // Remove user-specific path components for logging
        var relativePart = path;
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (path.StartsWith(userProfile, StringComparison.OrdinalIgnoreCase))
        {
            relativePart = "~" + path[userProfile.Length..];
        }
        return relativePart;
    }
    
    private sealed record CompiledPattern(string Name, Regex Regex);
}

public readonly record struct FilterResult(string FilteredContent, int RedactionCount)
{
    public bool WasModified => RedactionCount > 0;
}
```

---

#### Threat 3: Resource Exhaustion via Large File Indexing

**Threat Description:** An attacker could create or introduce extremely large files designed to exhaust memory or disk space during indexing, causing denial of service. This includes files with pathological content that expands during tokenization.

**Attack Vector:** Repository contains a 500MB generated file, or a file with millions of repeated tokens. Indexer attempts to load entire file into memory, causing OutOfMemoryException or disk exhaustion.

**Impact:** High - System becomes unresponsive, other operations fail, potential data loss.

**Mitigation - Complete C# Implementation:**

```csharp
using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Index;

/// <summary>
/// Guards against resource exhaustion during indexing by enforcing
/// file size limits, memory bounds, and disk space checks.
/// </summary>
public sealed class IndexResourceGuard
{
    private readonly ILogger<IndexResourceGuard> _logger;
    private readonly IndexResourceOptions _options;
    
    // Track memory pressure across concurrent operations
    private static long _currentMemoryUsage;
    
    public IndexResourceGuard(
        ILogger<IndexResourceGuard> logger,
        IndexResourceOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }
    
    /// <summary>
    /// Checks if a file should be indexed based on size limits.
    /// </summary>
    public ResourceCheckResult CheckFile(string filePath, long fileSize)
    {
        // Check maximum file size
        if (fileSize > _options.MaxFileSizeBytes)
        {
            _logger.LogInformation(
                "Skipping {FilePath}: size {Size} exceeds limit {Limit}",
                SanitizePath(filePath),
                FormatSize(fileSize),
                FormatSize(_options.MaxFileSizeBytes));
            
            return ResourceCheckResult.Rejected(
                $"File size {FormatSize(fileSize)} exceeds limit {FormatSize(_options.MaxFileSizeBytes)}");
        }
        
        // Check current memory pressure
        var currentMemory = Interlocked.Read(ref _currentMemoryUsage);
        if (currentMemory + fileSize > _options.MaxTotalMemoryBytes)
        {
            _logger.LogWarning(
                "Memory pressure: current {Current}, file {File}, limit {Limit}",
                FormatSize(currentMemory),
                FormatSize(fileSize),
                FormatSize(_options.MaxTotalMemoryBytes));
            
            // Trigger garbage collection and recheck
            GC.Collect(2, GCCollectionMode.Aggressive, true);
            currentMemory = Interlocked.Read(ref _currentMemoryUsage);
            
            if (currentMemory + fileSize > _options.MaxTotalMemoryBytes)
            {
                return ResourceCheckResult.Rejected(
                    "Memory pressure too high, try again later");
            }
        }
        
        return ResourceCheckResult.Accepted();
    }
    
    /// <summary>
    /// Checks if sufficient disk space is available for index operations.
    /// </summary>
    public ResourceCheckResult CheckDiskSpace(string indexPath, long requiredBytes)
    {
        try
        {
            var directory = Path.GetDirectoryName(indexPath) ?? ".";
            var driveInfo = new DriveInfo(Path.GetPathRoot(directory)!);
            
            // Require 2x the estimated size plus minimum buffer
            var requiredWithBuffer = requiredBytes * 2 + _options.MinFreeDiskSpaceBytes;
            
            if (driveInfo.AvailableFreeSpace < requiredWithBuffer)
            {
                _logger.LogError(
                    "Insufficient disk space: available {Available}, required {Required}",
                    FormatSize(driveInfo.AvailableFreeSpace),
                    FormatSize(requiredWithBuffer));
                
                return ResourceCheckResult.Rejected(
                    $"Insufficient disk space: {FormatSize(driveInfo.AvailableFreeSpace)} available, " +
                    $"{FormatSize(requiredWithBuffer)} required");
            }
            
            return ResourceCheckResult.Accepted();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check disk space, proceeding with caution");
            return ResourceCheckResult.Accepted(); // Fail open, but with warning
        }
    }
    
    /// <summary>
    /// Acquires a memory reservation for file processing.
    /// Returns an IDisposable that releases the reservation when disposed.
    /// </summary>
    public IDisposable AcquireMemoryReservation(long bytes)
    {
        Interlocked.Add(ref _currentMemoryUsage, bytes);
        _logger.LogDebug(
            "Reserved {Bytes} bytes, total now {Total}",
            FormatSize(bytes),
            FormatSize(Interlocked.Read(ref _currentMemoryUsage)));
        
        return new MemoryReservation(bytes);
    }
    
    /// <summary>
    /// Streams file content with chunked reading to avoid loading entire file.
    /// </summary>
    public async IAsyncEnumerable<string> StreamFileChunksAsync(
        string filePath,
        int chunkSizeBytes = 65536,
        [System.Runtime.CompilerServices.EnumeratorCancellation] 
        CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            chunkSizeBytes,
            FileOptions.SequentialScan | FileOptions.Asynchronous);
        
        using var reader = new StreamReader(stream);
        var buffer = new char[chunkSizeBytes];
        int charsRead;
        
        while ((charsRead = await reader.ReadAsync(buffer, cancellationToken)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new string(buffer, 0, charsRead);
        }
    }
    
    private static string FormatSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        var order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < suffixes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        
        return $"{size:0.##} {suffixes[order]}";
    }
    
    private static string SanitizePath(string path) => 
        Path.GetFileName(path); // Log only filename, not full path
    
    private sealed class MemoryReservation : IDisposable
    {
        private readonly long _bytes;
        private bool _disposed;
        
        public MemoryReservation(long bytes) => _bytes = bytes;
        
        public void Dispose()
        {
            if (_disposed) return;
            Interlocked.Add(ref _currentMemoryUsage, -_bytes);
            _disposed = true;
        }
    }
}

public readonly record struct ResourceCheckResult(bool IsAccepted, string? RejectionReason)
{
    public static ResourceCheckResult Accepted() => new(true, null);
    public static ResourceCheckResult Rejected(string reason) => new(false, reason);
}

public sealed class IndexResourceOptions
{
    /// <summary>Maximum size of a single file to index (default: 10 MB).</summary>
    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024;
    
    /// <summary>Maximum total memory for concurrent indexing operations (default: 500 MB).</summary>
    public long MaxTotalMemoryBytes { get; init; } = 500 * 1024 * 1024;
    
    /// <summary>Minimum free disk space required before indexing (default: 100 MB).</summary>
    public long MinFreeDiskSpaceBytes { get; init; } = 100 * 1024 * 1024;
}
```

---

#### Threat 4: Index File Tampering and Corruption Injection

**Threat Description:** An attacker with file system access could modify the index file to inject malicious content, corrupt search results, or cause parsing vulnerabilities when the index is loaded.

**Attack Vector:** Attacker modifies .agent/index.db directly, injecting malformed data that exploits SQLite parsing, corrupts search ranking, or injects code into search result snippets.

**Impact:** High - Could cause code execution, incorrect search results leading to wrong code changes, or system crashes.

**Mitigation - Complete C# Implementation:**

```csharp
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Index;

/// <summary>
/// Verifies index file integrity using cryptographic checksums
/// and validates structure before loading.
/// </summary>
public sealed class IndexIntegrityVerifier
{
    private readonly ILogger<IndexIntegrityVerifier> _logger;
    private const string ChecksumFileName = "index.checksum";
    private const int ExpectedSQLiteMagic = 0x53514C69; // "SQLi" in little-endian
    
    public IndexIntegrityVerifier(ILogger<IndexIntegrityVerifier> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// Verifies the integrity of an index file before loading.
    /// </summary>
    public IntegrityResult VerifyIndex(string indexPath)
    {
        if (!File.Exists(indexPath))
        {
            return IntegrityResult.NotFound("Index file does not exist");
        }
        
        try
        {
            // 1. Verify SQLite file format magic bytes
            if (!VerifySQLiteMagic(indexPath))
            {
                _logger.LogError("Index file failed SQLite magic byte verification");
                return IntegrityResult.Corrupted("Invalid file format - not a valid SQLite database");
            }
            
            // 2. Verify stored checksum matches current file
            var checksumPath = Path.Combine(
                Path.GetDirectoryName(indexPath)!, 
                ChecksumFileName);
            
            if (File.Exists(checksumPath))
            {
                var storedChecksum = File.ReadAllText(checksumPath).Trim();
                var currentChecksum = ComputeChecksum(indexPath);
                
                if (!ConstantTimeEquals(storedChecksum, currentChecksum))
                {
                    _logger.LogError(
                        "Index checksum mismatch: stored={Stored}, computed={Computed}",
                        storedChecksum[..16] + "...",
                        currentChecksum[..16] + "...");
                    
                    return IntegrityResult.Corrupted(
                        "Checksum mismatch - index file may have been tampered with");
                }
            }
            else
            {
                _logger.LogWarning("No checksum file found, creating one");
                UpdateChecksum(indexPath);
            }
            
            // 3. Verify SQLite integrity using PRAGMA integrity_check
            if (!VerifySQLiteIntegrity(indexPath))
            {
                return IntegrityResult.Corrupted("SQLite integrity check failed");
            }
            
            return IntegrityResult.Valid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify index integrity");
            return IntegrityResult.Corrupted($"Verification failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Updates the checksum file after a successful index write.
    /// </summary>
    public void UpdateChecksum(string indexPath)
    {
        var checksumPath = Path.Combine(
            Path.GetDirectoryName(indexPath)!, 
            ChecksumFileName);
        
        var checksum = ComputeChecksum(indexPath);
        File.WriteAllText(checksumPath, checksum);
        
        // Set restrictive permissions on checksum file
        SetRestrictivePermissions(checksumPath);
        
        _logger.LogDebug("Updated index checksum: {Checksum}", checksum[..16] + "...");
    }
    
    private bool VerifySQLiteMagic(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length < 100) return false; // SQLite header is 100 bytes
        
        Span<byte> header = stackalloc byte[16];
        stream.ReadExactly(header);
        
        // SQLite magic: "SQLite format 3\0"
        return header.SequenceEqual("SQLite format 3\0"u8);
    }
    
    private bool VerifySQLiteIntegrity(string filePath)
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection(
            $"Data Source={filePath};Mode=ReadOnly");
        connection.Open();
        
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check(100)"; // Check first 100 pages
        command.CommandTimeout = 30;
        
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var result = reader.GetString(0);
            if (result != "ok")
            {
                _logger.LogError("SQLite integrity check failed: {Result}", result);
                return false;
            }
        }
        
        return true;
    }
    
    private static string ComputeChecksum(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(stream);
        return Convert.ToHexString(hash);
    }
    
    /// <summary>
    /// Constant-time comparison to prevent timing attacks on checksum verification.
    /// </summary>
    private static bool ConstantTimeEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        
        var result = 0;
        for (var i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }
        return result == 0;
    }
    
    private static void SetRestrictivePermissions(string filePath)
    {
        if (OperatingSystem.IsWindows())
        {
            // On Windows, inherit from parent directory (already restricted)
        }
        else
        {
            // On Unix, set 600 (owner read/write only)
            File.SetUnixFileMode(filePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }
}

public readonly record struct IntegrityResult(
    bool IsValid, 
    bool NotFound, 
    string? ErrorMessage)
{
    public static IntegrityResult Valid() => new(true, false, null);
    public static IntegrityResult NotFound(string message) => new(false, true, message);
    public static IntegrityResult Corrupted(string message) => new(false, false, message);
}
```

---

#### Threat 5: Path Traversal via Malicious Ignore Patterns

**Threat Description:** An attacker could craft malicious ignore patterns that cause path traversal, allowing access to files outside the repository, or patterns that bypass intended ignore rules for sensitive files.

**Attack Vector:** Attacker adds patterns like `!../../../etc/passwd` to .agentignore, or crafts patterns that use symlinks to escape repository bounds and index sensitive system files.

**Impact:** High - Could expose system files, configuration, or files from other projects.

**Mitigation - Complete C# Implementation:**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Index;

/// <summary>
/// Validates and sanitizes ignore patterns to prevent path traversal
/// and other malicious pattern exploitation.
/// </summary>
public sealed class IgnorePatternValidator
{
    private readonly ILogger<IgnorePatternValidator> _logger;
    private readonly string _repositoryRoot;
    
    // Patterns that indicate path traversal attempts
    private static readonly Regex PathTraversalPattern = new(
        @"(^|\/)\.\.($|\/)|^\/|[<>:""|?*]|\x00",
        RegexOptions.Compiled);
    
    // Maximum pattern complexity to prevent regex DoS
    private const int MaxPatternLength = 500;
    private const int MaxGlobDepth = 10;
    private const int MaxNegations = 20;
    
    public IgnorePatternValidator(
        ILogger<IgnorePatternValidator> logger,
        string repositoryRoot)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repositoryRoot = Path.GetFullPath(repositoryRoot);
    }
    
    /// <summary>
    /// Validates a single ignore pattern and returns sanitized version if valid.
    /// </summary>
    public PatternValidationResult ValidatePattern(string pattern, string sourceFile)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return PatternValidationResult.Invalid("Empty pattern");
        }
        
        // Check length
        if (pattern.Length > MaxPatternLength)
        {
            _logger.LogWarning(
                "Pattern too long ({Length} chars) in {Source}, skipping",
                pattern.Length, sourceFile);
            return PatternValidationResult.Invalid("Pattern exceeds maximum length");
        }
        
        // Check for path traversal
        if (PathTraversalPattern.IsMatch(pattern))
        {
            _logger.LogWarning(
                "Path traversal attempt detected in pattern '{Pattern}' from {Source}",
                SanitizeForLog(pattern), sourceFile);
            return PatternValidationResult.Invalid("Path traversal not allowed");
        }
        
        // Check glob depth (prevent ** abuse)
        var globDepth = CountGlobDepth(pattern);
        if (globDepth > MaxGlobDepth)
        {
            _logger.LogWarning(
                "Pattern has excessive glob depth ({Depth}) in {Source}",
                globDepth, sourceFile);
            return PatternValidationResult.Invalid("Glob pattern too complex");
        }
        
        // Validate negation patterns carefully
        if (pattern.StartsWith('!'))
        {
            var negatedPath = pattern[1..];
            
            // Don't allow negating critical security patterns
            if (IsCriticalSecurityPattern(negatedPath))
            {
                _logger.LogWarning(
                    "Attempt to negate security pattern '{Pattern}' in {Source}",
                    SanitizeForLog(pattern), sourceFile);
                return PatternValidationResult.Invalid(
                    "Cannot negate security-critical ignore patterns");
            }
        }
        
        return PatternValidationResult.Valid(NormalizePattern(pattern));
    }
    
    /// <summary>
    /// Validates a file path to ensure it's within repository bounds.
    /// </summary>
    public bool IsPathWithinRepository(string filePath)
    {
        try
        {
            var fullPath = Path.GetFullPath(filePath);
            var normalizedRoot = _repositoryRoot.TrimEnd(Path.DirectorySeparatorChar) + 
                                Path.DirectorySeparatorChar;
            
            // Check if path is within repository
            if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Path {Path} is outside repository root {Root}",
                    SanitizeForLog(filePath),
                    SanitizeForLog(_repositoryRoot));
                return false;
            }
            
            // Check for symlink escape
            var realPath = ResolveFinalPath(fullPath);
            if (!realPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Path {Path} resolves outside repository via symlink to {Real}",
                    SanitizeForLog(filePath),
                    SanitizeForLog(realPath));
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate path: {Path}", SanitizeForLog(filePath));
            return false;
        }
    }
    
    /// <summary>
    /// Validates an entire ignore file and returns valid patterns only.
    /// </summary>
    public IReadOnlyList<string> ValidateIgnoreFile(string ignoreFilePath)
    {
        var validPatterns = new List<string>();
        var negationCount = 0;
        
        if (!File.Exists(ignoreFilePath))
        {
            return validPatterns;
        }
        
        if (!IsPathWithinRepository(ignoreFilePath))
        {
            _logger.LogError(
                "Ignore file {Path} is outside repository, ignoring entirely",
                SanitizeForLog(ignoreFilePath));
            return validPatterns;
        }
        
        var lines = File.ReadAllLines(ignoreFilePath);
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            
            // Skip comments and empty lines
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
            {
                continue;
            }
            
            // Track negations
            if (line.StartsWith('!'))
            {
                negationCount++;
                if (negationCount > MaxNegations)
                {
                    _logger.LogWarning(
                        "Too many negation patterns in {File}, skipping remaining",
                        ignoreFilePath);
                    break;
                }
            }
            
            var result = ValidatePattern(line, $"{ignoreFilePath}:{i + 1}");
            if (result.IsValid)
            {
                validPatterns.Add(result.SanitizedPattern!);
            }
        }
        
        return validPatterns;
    }
    
    private static int CountGlobDepth(string pattern)
    {
        var depth = 0;
        var i = 0;
        while ((i = pattern.IndexOf("**", i, StringComparison.Ordinal)) >= 0)
        {
            depth++;
            i += 2;
        }
        return depth;
    }
    
    private static bool IsCriticalSecurityPattern(string pattern)
    {
        var criticalPatterns = new[]
        {
            ".env", "*.env", "*.key", "*.pem", "*.pfx",
            "secrets.*", "*secret*", "*password*", "*credential*",
            "id_rsa", "id_dsa", "id_ecdsa", "id_ed25519",
            ".htpasswd", ".npmrc", ".pypirc"
        };
        
        foreach (var critical in criticalPatterns)
        {
            if (pattern.Contains(critical, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
    
    private static string NormalizePattern(string pattern)
    {
        // Normalize path separators
        return pattern.Replace('\\', '/').Trim();
    }
    
    private static string ResolveFinalPath(string path)
    {
        // Follow symlinks to get the real path
        var info = new FileInfo(path);
        if (info.LinkTarget != null)
        {
            return Path.GetFullPath(info.LinkTarget, Path.GetDirectoryName(path)!);
        }
        return path;
    }
    
    private static string SanitizeForLog(string input)
    {
        // Remove control characters and limit length for safe logging
        var sanitized = new string(input.Where(c => !char.IsControl(c)).ToArray());
        return sanitized.Length > 100 ? sanitized[..100] + "..." : sanitized;
    }
}

public readonly record struct PatternValidationResult(
    bool IsValid, 
    string? SanitizedPattern, 
    string? ErrorMessage)
{
    public static PatternValidationResult Valid(string pattern) => 
        new(true, pattern, null);
    public static PatternValidationResult Invalid(string error) => 
        new(false, null, error);
}
```

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Index | Searchable data structure mapping terms to document locations for fast retrieval |
| Full-Text Search | Searching document content by words, phrases, or patterns rather than metadata only |
| Ignore Rules | Exclusion patterns that specify which files/directories should not be indexed |
| Gitignore | Standard Git ignore file format (.gitignore) specifying files Git should not track |
| Agentignore | Agent-specific ignore file (.agentignore) taking precedence over .gitignore |
| Incremental Update | Updating only changed files rather than rebuilding the entire index |
| Persistent Index | Index data stored on disk that survives application restarts |
| Ranking | Ordering search results by calculated relevance scores (e.g., BM25, TF-IDF) |
| Tokenization | Breaking text content into searchable terms (words, identifiers, symbols) |
| Stemming | Reducing words to their root form (e.g., "running" → "run") for broader matches |
| Inverted Index | Data structure mapping terms to lists of documents containing them |
| Query | A search request containing terms, operators, filters, and pagination options |
| Search Result | A matched document with path, line numbers, snippets, and relevance score |
| Relevance Score | Numerical value (0-1) indicating how well a document matches the query |
| Filter | Constraints applied to limit results by extension, directory, size, or date |
| Pagination | Dividing results into pages using skip/take parameters for efficient retrieval |
| BM25 | Best Matching 25 - probabilistic ranking algorithm used in search engines |
| TF-IDF | Term Frequency-Inverse Document Frequency - statistical relevance measure |
| Postings List | List of documents and positions where a specific term appears |
| Document Frequency | Number of documents containing a specific term |

---

## Use Cases

### Use Case 1: DevBot Searches for Payment Processing Implementation

**Persona:** DevBot, an AI coding agent working on a feature request to modify payment processing logic.

**Scenario:** The user asks DevBot to "update the payment retry logic to use exponential backoff." DevBot needs to find all files related to payment processing to understand the current implementation before making changes.

**Before (Without Indexing):**
```
1. DevBot receives the task
2. DevBot uses file listing to enumerate all 15,000 files in the monorepo
3. DevBot reads file names looking for "payment" - finds 45 candidates
4. DevBot reads each file sequentially to find payment retry logic
5. After 8 minutes of file reading, DevBot has consumed 450,000 tokens
6. DevBot finally locates the relevant code but has exceeded context limits
7. User waits frustrated, token budget exhausted
```

**After (With Indexing):**
```
1. DevBot receives the task
2. DevBot searches: acode search "payment retry" --ext cs
3. In 45ms, returns 12 ranked results with snippets showing line numbers
4. DevBot identifies PaymentRetryService.cs (score: 0.95) and RetryPolicy.cs (score: 0.87)
5. DevBot reads only the 2 relevant files (800 tokens total)
6. DevBot understands current implementation and makes the change
7. Total time: 15 seconds. Token usage: 1,200 tokens
```

**Metrics:**
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Time to find code | 8 minutes | 15 seconds | 97% faster |
| Files read | 45 files | 2 files | 95% reduction |
| Tokens consumed | 450,000 | 1,200 | 99.7% reduction |
| User wait time | 8+ minutes | 15 seconds | 97% faster |
| Task success rate | 60% (context overflow) | 98% | 38% improvement |

**Annual Value:** For a developer team running 50 such searches/day, this saves 6.5 hours/day × 250 days = 1,625 hours/year = **$81,250/year at $50/hr**.

---

### Use Case 2: Jordan Investigates a Production Bug

**Persona:** Jordan, a senior developer investigating a NullReferenceException that appeared in production logs.

**Scenario:** The production monitoring system reports a NullReferenceException in `OrderProcessor.ProcessAsync` with stack trace showing the error originated from a call to `GetCustomerDiscount`. Jordan needs to find all usages of this method and understand the null path.

**Before (Without Indexing):**
```
1. Jordan opens IDE and uses Ctrl+Shift+F to search "GetCustomerDiscount"
2. IDE search takes 45 seconds to scan 15,000 files
3. Returns 89 results including tests, comments, and dead code
4. Jordan manually reviews each result to find actual call sites
5. Switches to agent to ask about null handling, but agent can't search
6. Jordan manually copies relevant code snippets to agent
7. 25 minutes later, Jordan has the context needed for investigation
```

**After (With Indexing):**
```
1. Jordan asks agent: "Find all usages of GetCustomerDiscount and show null handling"
2. Agent searches: acode search "GetCustomerDiscount" --ext cs -test
3. In 60ms, returns 23 production code results ranked by relevance
4. Agent analyzes snippets, identifies 3 call sites without null checks
5. Agent pinpoints the exact location: OrderProcessor.cs line 145
6. Jordan has the root cause in 2 minutes
```

**Metrics:**
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Investigation time | 25 minutes | 2 minutes | 92% faster |
| Search latency | 45 seconds | 60 ms | 99.9% faster |
| Results to review | 89 results | 23 results | 74% reduction |
| Manual code copying | Required | Not needed | Eliminated |
| Mean time to resolution | 25 minutes | 2 minutes | 92% faster |

**Annual Value:** For 3 production incidents/week requiring investigation, this saves 23 minutes × 3 × 52 = 59.8 hours/year = **$2,990/year** per developer. For a team of 10: **$29,900/year**.

---

### Use Case 3: Alex Refactors Deprecated API Usage

**Persona:** Alex, a developer tasked with replacing all usages of a deprecated API before a major version upgrade.

**Scenario:** The team is upgrading from v2 to v3 of a logging library. The deprecated `Logger.Log(string)` method must be replaced with `Logger.LogInformation(string, params object[])` across the entire codebase. Alex needs to find every usage, understand the context, and make appropriate replacements.

**Before (Without Indexing):**
```
1. Alex runs grep across the repository: grep -r "Logger.Log(" --include="*.cs"
2. Grep takes 2 minutes to scan 500MB of source code
3. Returns 234 matches including comments, strings, and test mocks
4. Alex creates a spreadsheet to track each occurrence
5. Alex manually opens each file, reviews context, makes change
6. After 4 hours, Alex has completed 80% of changes
7. Code review reveals 12 missed occurrences in nested directories
8. Total time: 6 hours with incomplete coverage
```

**After (With Indexing):**
```
1. Alex asks agent: "Find all usages of Logger.Log and show surrounding context"
2. Agent searches: acode search '"Logger.Log("' --ext cs
3. In 80ms, returns 234 results with file paths and line numbers
4. Agent filters results: excludes test files, comments, mock implementations
5. Agent generates migration script with 187 actual changes needed
6. Alex reviews the categorized list and approves batch changes
7. Agent applies changes using atomic patch operations
8. Total time: 45 minutes with 100% coverage verified
```

**Metrics:**
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Time to complete | 6 hours | 45 minutes | 87% faster |
| Search latency | 2 minutes | 80 ms | 99.9% faster |
| Missed occurrences | 12 (5%) | 0 | 100% coverage |
| Manual file opens | 234 files | 0 files | Eliminated |
| Rework required | Yes (code review) | No | Eliminated |

**Annual Value:** For 4 similar refactoring tasks/year, this saves 5.25 hours × 4 = 21 hours/year = **$1,050/year** per developer. For a team of 10: **$10,500/year**. Plus: eliminated rework from missed occurrences avoids 2 hours × 4 = 8 hours additional = **$400/year** per developer.

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

#### Issue 1: Search Returns No Results for Known Content

**Error Code:** ACODE-IDX-002

**Symptoms:**
- `acode search "className"` returns "No results found"
- User is certain the term exists in the repository
- Search worked previously but now returns nothing

**Possible Causes:**
1. Index is stale (not updated after file changes)
2. File is excluded by ignore patterns
3. File extension not in `include_extensions` whitelist
4. File exceeds `max_file_size_kb` limit
5. Index was built before the file was created
6. Term is split differently than expected (tokenization issue)

**Diagnostic Steps:**
```bash
# Check index status
acode index status

# Check if file is being indexed
acode index status --verbose | grep "myfile.cs"

# Test ignore patterns
acode ignore check src/MyClass.cs

# Verify tokenization
acode search --debug "className"
```

**Solutions:**

```csharp
// Solution 1: Force index rebuild
// Use when index is stale or corrupted

public async Task DiagnoseSearchIssueAsync(string searchTerm, string expectedFile)
{
    // Check if file exists
    if (!await _repoFs.FileExistsAsync(expectedFile))
    {
        _logger.LogError("File {File} does not exist", expectedFile);
        return;
    }
    
    // Check if file is ignored
    var ignoreResult = await _ignoreRules.ShouldIgnoreAsync(expectedFile);
    if (ignoreResult.IsIgnored)
    {
        _logger.LogWarning(
            "File {File} is ignored by pattern '{Pattern}' in {Source}",
            expectedFile, ignoreResult.MatchedPattern, ignoreResult.Source);
        return;
    }
    
    // Check if file is indexed
    var indexStats = await _indexService.GetFileStatsAsync(expectedFile);
    if (indexStats == null)
    {
        _logger.LogWarning("File {File} is not in index, triggering update", expectedFile);
        await _indexService.UpdateAsync(CancellationToken.None);
        return;
    }
    
    // Check tokenization
    var tokens = _tokenizer.Tokenize(searchTerm);
    _logger.LogInformation("Search term tokenizes to: {Tokens}", string.Join(", ", tokens));
    
    // Suggest rebuild if all else fails
    _logger.LogInformation("Try: acode index rebuild");
}
```

**Prevention:**
- Run `acode index update` after major file changes
- Configure `.agentignore` carefully
- Review `include_extensions` in config

---

#### Issue 2: Index Build Extremely Slow or Hangs

**Error Code:** ACODE-IDX-001

**Symptoms:**
- `acode index build` takes > 10 minutes for small repository
- Progress bar freezes at certain percentage
- High CPU usage but no progress
- Memory usage grows continuously

**Possible Causes:**
1. Very large files being processed (> 10 MB source files)
2. Too many files (missing ignore patterns for node_modules, etc.)
3. Network file system with high latency
4. File system permission issues causing retry loops
5. Circular symlinks
6. Binary files incorrectly detected as text

**Diagnostic Steps:**
```bash
# Check file count
find . -type f | wc -l

# Check for large files
find . -type f -size +10M

# Check for common bloat directories
ls -la node_modules/ .git/objects/ bin/ obj/

# Run with verbose logging
acode index build --verbose 2>&1 | tee index-build.log
```

**Solutions:**

```csharp
// Solution: Add comprehensive ignore patterns
// .agentignore file content for typical projects:

/*
# Dependencies
node_modules/
vendor/
packages/
.nuget/

# Build outputs
bin/
obj/
dist/
build/
out/
target/

# IDE and tools
.idea/
.vs/
.vscode/
*.suo
*.user

# Large generated files
*.min.js
*.bundle.js
*.map

# Logs and caches
*.log
.cache/
.tmp/

# Test artifacts
coverage/
test-results/
*/

// Programmatic optimization
public async Task OptimizeIndexBuildAsync()
{
    var options = new IndexBuildOptions
    {
        MaxFileSizeKb = 500,        // Skip files > 500 KB
        MaxConcurrentFiles = 4,      // Limit parallel processing
        TimeoutPerFileMs = 5000,     // 5 second timeout per file
        SkipSymlinks = true,         // Avoid symlink loops
        StreamLargeFiles = true      // Stream instead of loading entire file
    };
    
    await _indexService.BuildAsync(options, CancellationToken.None);
}
```

**Prevention:**
- Always configure ignore patterns before first build
- Set reasonable `max_file_size_kb` (default: 500 KB)
- Exclude dependency directories (node_modules, vendor)

---

#### Issue 3: Index File Corruption After System Crash

**Error Code:** ACODE-IDX-004

**Symptoms:**
- Error message: "Index file corrupted, checksum mismatch"
- Error message: "SQLite database disk image is malformed"
- Search returns random or incorrect results
- `acode index status` fails with database error

**Possible Causes:**
1. System crash or power failure during index write
2. Disk failure or bad sectors
3. Index file manually modified
4. Concurrent access from multiple processes
5. Incomplete previous build/update

**Diagnostic Steps:**
```bash
# Check index file integrity
sqlite3 .agent/index.db "PRAGMA integrity_check"

# Check checksum
cat .agent/index.checksum
sha256sum .agent/index.db

# Check for lock files
ls -la .agent/*.lock
```

**Solutions:**

```csharp
// Solution: Index recovery service
public sealed class IndexRecoveryService
{
    private readonly ILogger<IndexRecoveryService> _logger;
    private readonly IIndexService _indexService;
    
    public async Task<RecoveryResult> RecoverAsync(string indexPath)
    {
        _logger.LogWarning("Starting index recovery for {Path}", indexPath);
        
        // Step 1: Backup corrupted index for analysis
        var backupPath = indexPath + $".corrupted.{DateTime.UtcNow:yyyyMMddHHmmss}";
        if (File.Exists(indexPath))
        {
            File.Move(indexPath, backupPath);
            _logger.LogInformation("Backed up corrupted index to {Backup}", backupPath);
        }
        
        // Step 2: Remove stale lock files
        var lockPath = indexPath + ".lock";
        if (File.Exists(lockPath))
        {
            var lockAge = DateTime.UtcNow - File.GetLastWriteTimeUtc(lockPath);
            if (lockAge > TimeSpan.FromMinutes(10))
            {
                File.Delete(lockPath);
                _logger.LogWarning("Removed stale lock file (age: {Age})", lockAge);
            }
        }
        
        // Step 3: Rebuild index from scratch
        _logger.LogInformation("Rebuilding index from source files...");
        await _indexService.RebuildAsync(CancellationToken.None);
        
        // Step 4: Verify new index
        var verifyResult = await _indexService.VerifyIntegrityAsync();
        if (!verifyResult.IsValid)
        {
            _logger.LogError("Rebuild failed verification: {Error}", verifyResult.Error);
            return RecoveryResult.Failed(verifyResult.Error);
        }
        
        _logger.LogInformation("Index recovery completed successfully");
        return RecoveryResult.Success();
    }
}
```

**Prevention:**
- Enable atomic writes (temp file + rename)
- Run on local disk, not network storage
- Avoid killing acode process during index operations

---

#### Issue 4: Search Performance Degraded Over Time

**Error Code:** ACODE-IDX-008

**Symptoms:**
- Search that was < 50ms now takes 500ms+
- Performance degrades with more searches
- Memory usage grows over session
- Index file size seems larger than expected

**Possible Causes:**
1. Index fragmentation from many incremental updates
2. Many deleted files leaving orphaned entries
3. Search cache not being cleaned
4. Index compaction never run
5. Too many small incremental updates

**Diagnostic Steps:**
```bash
# Check index size vs file count
acode index status

# Check for fragmentation (SQLite)
sqlite3 .agent/index.db "PRAGMA page_count; PRAGMA freelist_count;"

# Check cache statistics
acode search --stats "test"
```

**Solutions:**

```csharp
// Solution: Index maintenance service
public sealed class IndexMaintenanceService
{
    public async Task PerformMaintenanceAsync(string indexPath)
    {
        _logger.LogInformation("Starting index maintenance...");
        
        // Step 1: Compact index (reclaim deleted space)
        await CompactIndexAsync(indexPath);
        
        // Step 2: Analyze for query optimization
        await OptimizeIndexAsync(indexPath);
        
        // Step 3: Clear search cache
        _searchCache.Clear();
        
        // Step 4: Update statistics
        await UpdateStatisticsAsync(indexPath);
        
        _logger.LogInformation("Maintenance completed, index optimized");
    }
    
    private async Task CompactIndexAsync(string indexPath)
    {
        using var connection = new SqliteConnection($"Data Source={indexPath}");
        await connection.OpenAsync();
        
        using var command = connection.CreateCommand();
        command.CommandText = "VACUUM;";
        await command.ExecuteNonQueryAsync();
        
        _logger.LogInformation("Index compacted, space reclaimed");
    }
    
    private async Task OptimizeIndexAsync(string indexPath)
    {
        using var connection = new SqliteConnection($"Data Source={indexPath}");
        await connection.OpenAsync();
        
        using var command = connection.CreateCommand();
        command.CommandText = "ANALYZE;";
        await command.ExecuteNonQueryAsync();
        
        _logger.LogInformation("Query statistics updated");
    }
}
```

**Prevention:**
- Run `acode index rebuild` periodically (weekly for active repos)
- Enable automatic compaction after N incremental updates
- Monitor index size vs file count ratio

---

#### Issue 5: Ignore Patterns Not Working as Expected

**Error Code:** ACODE-IDX-005

**Symptoms:**
- Files are indexed despite matching ignore pattern
- Negation patterns (!) don't work
- Patterns work in Git but not in acode
- Nested .gitignore not being respected

**Possible Causes:**
1. Pattern syntax incorrect for Git specification
2. Pattern precedence misunderstood
3. .agentignore overriding .gitignore unexpectedly
4. Negation pattern undoing previous match
5. Directory vs file pattern confusion (trailing slash)
6. Pattern not anchored to correct directory

**Diagnostic Steps:**
```bash
# Test specific file against patterns
acode ignore check path/to/file.cs

# List all active ignore patterns
acode ignore list --verbose

# Show pattern matching trace
acode ignore trace path/to/file.cs
```

**Solutions:**

```csharp
// Solution: Ignore pattern debugger
public sealed class IgnorePatternDebugger
{
    public IgnoreTraceResult TraceFile(string filePath, IReadOnlyList<IgnoreSource> sources)
    {
        var trace = new List<TraceEntry>();
        var isIgnored = false;
        
        foreach (var source in sources)
        {
            foreach (var pattern in source.Patterns)
            {
                var matches = pattern.Matches(filePath);
                if (matches)
                {
                    trace.Add(new TraceEntry
                    {
                        Source = source.FilePath,
                        Pattern = pattern.Raw,
                        LineNumber = pattern.LineNumber,
                        IsNegation = pattern.IsNegation,
                        Matches = true,
                        Effect = pattern.IsNegation ? "INCLUDE" : "EXCLUDE"
                    });
                    
                    isIgnored = !pattern.IsNegation;
                }
            }
        }
        
        return new IgnoreTraceResult
        {
            FilePath = filePath,
            FinalResult = isIgnored ? "IGNORED" : "INDEXED",
            Trace = trace
        };
    }
}

// Common pattern fixes:
// WRONG: node_modules    -> Matches file named "node_modules", not directory
// RIGHT: node_modules/   -> Matches directory named "node_modules"
// 
// WRONG: *.log           -> Matches only in root directory
// RIGHT: **/*.log        -> Matches in any directory
//
// WRONG: !important.log  -> Negation ignored if pattern after it re-excludes
// RIGHT: Place negations LAST in file
```

**Prevention:**
- Use `acode ignore check` before building index
- Understand Git ignore specification (man gitignore)
- Place negation patterns at end of file
- Use trailing slash for directories

---

## Acceptance Criteria

### Index Service Interface (AC-001 to AC-010)

| ID | Criterion | Verification Method |
|----|-----------|---------------------|
| AC-001 | IIndexService interface is defined with BuildAsync, UpdateAsync, RebuildAsync, SearchAsync, GetStatsAsync methods | Unit test: interface compilation and method signatures |
| AC-002 | All IIndexService methods accept CancellationToken parameter | Unit test: cancellation token propagation |
| AC-003 | BuildAsync returns IndexBuildResult with file count, duration, and status | Unit test: result object validation |
| AC-004 | SearchAsync returns IReadOnlyList<SearchResult> with file path, line numbers, snippets, and score | Unit test: result properties |
| AC-005 | GetStatsAsync returns IndexStats with file count, index size, last updated timestamp | Unit test: stats accuracy |
| AC-006 | IIndexService is registered as singleton in DI container | Integration test: DI resolution |
| AC-007 | IIndexService implementation uses IRepoFS for all file access | Code review: no direct File.* calls |
| AC-008 | All async methods properly propagate cancellation | Unit test: cancellation during operation |
| AC-009 | BuildAsync reports progress via IProgress<IndexProgress> | Unit test: progress callback invocation |
| AC-010 | All methods log entry/exit with structured logging | Integration test: log verification |

### Index Building (AC-011 to AC-025)

| ID | Criterion | Verification Method |
|----|-----------|---------------------|
| AC-011 | Index builds successfully for a repository with 100 text files in < 2 seconds | Performance test: timing measurement |
| AC-012 | Index builds successfully for a repository with 10,000 text files in < 30 seconds | Performance test: timing measurement |
| AC-013 | Binary files (images, executables, archives) are detected and skipped | Unit test: binary detection |
| AC-014 | Binary detection uses file signature (magic bytes) not just extension | Unit test: renamed binary file |
| AC-015 | Files matching .gitignore patterns are excluded from index | Integration test: gitignore |
| AC-016 | Files matching .agentignore patterns are excluded from index | Integration test: agentignore |
| AC-017 | .agentignore patterns take precedence over .gitignore | Integration test: precedence |
| AC-018 | Default ignore patterns exclude .env, *.key, *.pem, secrets.* | Unit test: default patterns |
| AC-019 | Index persists to .agent/index.db (configurable) | Integration test: file creation |
| AC-020 | Index persistence is atomic (temp file + rename) | Unit test: atomic write |
| AC-021 | Interrupted build does not corrupt existing index | E2E test: interrupt during build |
| AC-022 | Build reports accurate progress percentage | Unit test: progress calculation |
| AC-023 | Large files (> max_file_size_kb) are skipped with warning | Unit test: size limit |
| AC-024 | Files with access errors are skipped with warning and continue | Integration test: permission error |
| AC-025 | Build populates metadata table with version and checksum | Unit test: metadata storage |

### Tokenization (AC-026 to AC-035)

| ID | Criterion | Verification Method |
|----|-----------|---------------------|
| AC-026 | "getUserById" tokenizes to ["get", "user", "by", "id"] | Unit test: camelCase |
| AC-027 | "get_user_by_id" tokenizes to ["get", "user", "by", "id"] | Unit test: snake_case |
| AC-028 | "GetUserByID" tokenizes to ["get", "user", "by", "id"] | Unit test: PascalCase with acronym |
| AC-029 | Tokens are normalized to lowercase | Unit test: case normalization |
| AC-030 | Numbers are tokenized as separate tokens ("user123" → ["user", "123"]) | Unit test: numbers |
| AC-031 | Punctuation is removed during tokenization | Unit test: punctuation handling |
| AC-032 | Line numbers are tracked for each token occurrence | Unit test: position tracking |
| AC-033 | Unicode content (UTF-8) is tokenized correctly | Unit test: unicode handling |
| AC-034 | Files with BOM are handled correctly | Unit test: BOM detection |
| AC-035 | Tokenization processes at minimum 1 MB/second | Performance test: throughput |

### Search Operations (AC-036 to AC-055)

| ID | Criterion | Verification Method |
|----|-----------|---------------------|
| AC-036 | Single word search ("controller") returns matching files | Unit test: basic search |
| AC-037 | Multi-word search ("user service") returns files with both terms (AND) | Unit test: AND operator |
| AC-038 | Quoted phrase search ("\"user service\"") returns exact phrase matches | Unit test: phrase search |
| AC-039 | OR operator ("user OR customer") returns files with either term | Unit test: OR operator |
| AC-040 | Exclusion operator ("user -test") excludes files with term | Unit test: exclusion |
| AC-041 | Wildcard suffix ("User*") matches "UserService", "UserController" | Unit test: suffix wildcard |
| AC-042 | Wildcard prefix ("*Service") matches "UserService", "OrderService" | Unit test: prefix wildcard |
| AC-043 | Search is case-insensitive by default | Unit test: case insensitivity |
| AC-044 | Case-sensitive search option works | Unit test: case sensitivity |
| AC-045 | Search returns matched line numbers for each file | Unit test: line numbers |
| AC-046 | Search returns context snippets with surrounding lines | Unit test: snippet generation |
| AC-047 | Snippets highlight matched terms | Unit test: highlighting |
| AC-048 | Search returns relevance score (0-1) for each result | Unit test: scoring |
| AC-049 | Results are ranked by relevance score (descending) | Unit test: ranking order |
| AC-050 | BM25 ranking algorithm is used | Unit test: scoring formula |
| AC-051 | Pagination accepts skip and take parameters | Unit test: pagination |
| AC-052 | Search returns total count regardless of pagination | Unit test: total count |
| AC-053 | Simple word search completes in < 50ms for 10K file index | Performance test: latency |
| AC-054 | Complex query search completes in < 100ms for 10K file index | Performance test: latency |
| AC-055 | Empty query returns empty results (not error) | Unit test: empty query |

### Search Filtering (AC-056 to AC-065)

| ID | Criterion | Verification Method |
|----|-----------|---------------------|
| AC-056 | Extension filter (--ext cs) limits results to .cs files | Unit test: extension filter |
| AC-057 | Multiple extension filter (--ext cs,ts) accepts multiple | Unit test: multi-extension |
| AC-058 | Directory filter (--dir src) limits to specific directory | Unit test: directory filter |
| AC-059 | Directory filter supports recursive matching | Unit test: recursive |
| AC-060 | Size filter (--min-size 1kb --max-size 100kb) limits by file size | Unit test: size filter |
| AC-061 | Date filter (--since 2024-01-01) limits by modification date | Unit test: date filter |
| AC-062 | Filters can be combined | Unit test: combined filters |
| AC-063 | Filters are applied before search for performance | Performance test: filter optimization |
| AC-064 | Invalid filter returns descriptive error | Unit test: error handling |
| AC-065 | Empty filter matches all files | Unit test: no filter |

### Ignore Rules (AC-066 to AC-080)

| ID | Criterion | Verification Method |
|----|-----------|---------------------|
| AC-066 | .gitignore files are parsed according to Git specification | Integration test: git compliance |
| AC-067 | Comment lines (# comment) are ignored | Unit test: comments |
| AC-068 | Blank lines are ignored | Unit test: blank lines |
| AC-069 | Exact file patterns (README.md) match | Unit test: exact match |
| AC-070 | Glob patterns (*.log) match | Unit test: glob |
| AC-071 | Directory patterns (node_modules/) match directories | Unit test: directory |
| AC-072 | Double glob patterns (**/*.test.js) match recursively | Unit test: double glob |
| AC-073 | Negation patterns (!important.log) exclude from ignore | Unit test: negation |
| AC-074 | Escaped characters (\#file) are handled | Unit test: escaping |
| AC-075 | Nested .gitignore files are discovered | Integration test: nested |
| AC-076 | Nested rules apply only to subdirectories | Unit test: scope |
| AC-077 | Rules are applied in order (later overrides earlier) | Unit test: order |
| AC-078 | Invalid patterns are skipped with warning | Unit test: invalid pattern |
| AC-079 | Parsed rules are cached for performance | Performance test: caching |
| AC-080 | Cache invalidates when ignore file changes | Unit test: invalidation |

### Incremental Updates (AC-081 to AC-092)

| ID | Criterion | Verification Method |
|----|-----------|---------------------|
| AC-081 | Modified files are detected by mtime change | Unit test: mtime detection |
| AC-082 | Modified files are detected by size change | Unit test: size detection |
| AC-083 | New files are detected and added to index | Integration test: new file |
| AC-084 | Deleted files are detected and removed from index | Integration test: delete |
| AC-085 | Renamed files are detected and path updated | Integration test: rename |
| AC-086 | Incremental update for 10 changed files completes in < 2 seconds | Performance test: timing |
| AC-087 | Incremental update for 100 changed files completes in < 5 seconds | Performance test: timing |
| AC-088 | Unchanged files are not re-indexed | Performance test: efficiency |
| AC-089 | Update is transactional (atomic commit) | Unit test: transaction |
| AC-090 | Failed update does not corrupt index | E2E test: failure recovery |
| AC-091 | Update tracks last_updated timestamp | Unit test: timestamp |
| AC-092 | Concurrent updates are prevented via locking | Unit test: concurrency |

### Persistence and Recovery (AC-093 to AC-105)

| ID | Criterion | Verification Method |
|----|-----------|---------------------|
| AC-093 | Index loads from disk on startup in < 500ms for 10K files | Performance test: load time |
| AC-094 | Index validates checksum on load | Unit test: checksum validation |
| AC-095 | Checksum mismatch triggers automatic rebuild | Integration test: corruption |
| AC-096 | Index includes format version number | Unit test: version storage |
| AC-097 | Version mismatch triggers automatic rebuild | Integration test: version change |
| AC-098 | SQLite integrity check runs on load | Unit test: integrity |
| AC-099 | Corrupted index triggers rebuild with user notification | E2E test: corruption |
| AC-100 | Index compaction reclaims space from deleted entries | Unit test: compaction |
| AC-101 | Disk full during write is handled gracefully | E2E test: disk full |
| AC-102 | Index file has same permissions as repository | Security test: permissions |
| AC-103 | Stale index detection compares root mtime | Unit test: staleness |
| AC-104 | Stale index prompts for update | E2E test: stale notification |
| AC-105 | Recovery from crash leaves index in consistent state | E2E test: crash recovery |

### CLI Commands (AC-106 to AC-120)

| ID | Criterion | Verification Method |
|----|-----------|---------------------|
| AC-106 | `acode index build` creates index from scratch | E2E test: CLI |
| AC-107 | `acode index build` shows progress bar with percentage | E2E test: output |
| AC-108 | `acode index update` performs incremental update | E2E test: CLI |
| AC-109 | `acode index rebuild` clears and rebuilds index | E2E test: CLI |
| AC-110 | `acode index status` shows file count, size, last updated | E2E test: output |
| AC-111 | `acode index status` shows pending changes count | E2E test: output |
| AC-112 | `acode search "query"` returns formatted results | E2E test: CLI |
| AC-113 | `acode search --json "query"` returns JSONL output | E2E test: JSON output |
| AC-114 | `acode search --ext cs "query"` applies extension filter | E2E test: filter |
| AC-115 | `acode search --dir src "query"` applies directory filter | E2E test: filter |
| AC-116 | Search results show file path, line numbers, and snippets | E2E test: output format |
| AC-117 | Non-zero exit code on error | E2E test: exit codes |
| AC-118 | --help shows usage for all commands | E2E test: help |
| AC-119 | --verbose enables debug logging | E2E test: logging |
| AC-120 | All commands respect --config for custom config path | E2E test: config |

---

## Best Practices

### Index Design

1. **Incremental by default** - Only re-index changed files; full rebuild is opt-in
2. **Store metadata separately** - Keep file content separate from search index
3. **Version the index format** - Include format version for backward compatibility
4. **Compress aggressively** - Trade CPU for smaller index size on disk

### Search Quality

5. **Normalize before indexing** - Consistent tokenization for query and content
6. **Support partial matches** - Prefix, suffix, and wildcard matching options
7. **Rank by relevance** - Most relevant results first based on BM25 scoring
8. **Limit result set** - Return top N results; pagination for more

### Performance

9. **Build index in background** - Don't block user operations during indexing
10. **Throttle I/O** - Respect system resources during large index builds
11. **Cancel gracefully** - Stop indexing cleanly when user requests
12. **Cache hot paths** - Keep frequently searched patterns in memory

### Security

13. **Validate all paths** - Use RepoFS for all file access, prevent path traversal
14. **Sanitize queries** - Prevent SQL injection and regex DoS attacks
15. **Filter sensitive content** - Redact secrets before tokenization
16. **Restrict permissions** - Index file permissions match repository

### Reliability

17. **Atomic writes** - Use temp file + rename for index persistence
18. **Checksum verification** - Detect corruption on load
19. **Graceful degradation** - Continue on file access errors
20. **Auto-recovery** - Rebuild automatically on corruption

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