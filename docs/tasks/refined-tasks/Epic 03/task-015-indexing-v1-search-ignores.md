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

#### IndexBuilderTests.cs

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Index;
using Acode.Infrastructure.Index;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Acode.Tests.Unit.Index;

public sealed class IndexBuilderTests
{
    private readonly Mock<IRepoFileSystem> _repoFsMock;
    private readonly Mock<IIgnoreRuleParser> _ignoreParserMock;
    private readonly Mock<ITokenizer> _tokenizerMock;
    private readonly Mock<ILogger<IndexBuilder>> _loggerMock;
    private readonly IndexBuilder _sut;

    public IndexBuilderTests()
    {
        _repoFsMock = new Mock<IRepoFileSystem>();
        _ignoreParserMock = new Mock<IIgnoreRuleParser>();
        _tokenizerMock = new Mock<ITokenizer>();
        _loggerMock = new Mock<ILogger<IndexBuilder>>();
        
        _sut = new IndexBuilder(
            _repoFsMock.Object,
            _ignoreParserMock.Object,
            _tokenizerMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Should_Index_Text_File()
    {
        // Arrange
        var filePath = "src/Services/UserService.cs";
        var fileContent = "public class UserService { }";
        var tokens = new[] { "public", "class", "user", "service" };
        
        _repoFsMock.Setup(x => x.EnumerateFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(new[] { filePath }));
        _repoFsMock.Setup(x => x.ReadAllTextAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileContent);
        _repoFsMock.Setup(x => x.GetFileInfoAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileMetadata(filePath, 100, DateTime.UtcNow, false));
        _ignoreParserMock.Setup(x => x.ShouldIgnore(filePath)).Returns(false);
        _tokenizerMock.Setup(x => x.Tokenize(fileContent)).Returns(tokens);

        // Act
        var result = await _sut.BuildAsync("/repo", CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.FilesIndexed);
        Assert.Contains(filePath, result.IndexedFiles);
    }

    [Fact]
    public async Task Should_Index_Multiple_Files()
    {
        // Arrange
        var files = new[] 
        { 
            "src/Services/UserService.cs",
            "src/Services/OrderService.cs",
            "src/Controllers/UserController.cs"
        };
        
        foreach (var file in files)
        {
            _repoFsMock.Setup(x => x.ReadAllTextAsync(file, It.IsAny<CancellationToken>()))
                .ReturnsAsync($"// Content of {file}");
            _repoFsMock.Setup(x => x.GetFileInfoAsync(file, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileMetadata(file, 50, DateTime.UtcNow, false));
        }
        
        _repoFsMock.Setup(x => x.EnumerateFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(files));
        _ignoreParserMock.Setup(x => x.ShouldIgnore(It.IsAny<string>())).Returns(false);
        _tokenizerMock.Setup(x => x.Tokenize(It.IsAny<string>())).Returns(new[] { "content" });

        // Act
        var result = await _sut.BuildAsync("/repo", CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.FilesIndexed);
    }

    [Fact]
    public async Task Should_Skip_Binary_Files()
    {
        // Arrange
        var textFile = "src/app.cs";
        var binaryFile = "assets/image.png";
        var files = new[] { textFile, binaryFile };
        
        _repoFsMock.Setup(x => x.EnumerateFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(files));
        _repoFsMock.Setup(x => x.GetFileInfoAsync(textFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileMetadata(textFile, 100, DateTime.UtcNow, false));
        _repoFsMock.Setup(x => x.GetFileInfoAsync(binaryFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileMetadata(binaryFile, 50000, DateTime.UtcNow, true)); // isBinary = true
        _repoFsMock.Setup(x => x.ReadAllTextAsync(textFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync("public class App { }");
        _ignoreParserMock.Setup(x => x.ShouldIgnore(It.IsAny<string>())).Returns(false);
        _tokenizerMock.Setup(x => x.Tokenize(It.IsAny<string>())).Returns(new[] { "public", "class", "app" });

        // Act
        var result = await _sut.BuildAsync("/repo", CancellationToken.None);

        // Assert
        Assert.Equal(1, result.FilesIndexed);
        Assert.Equal(1, result.FilesSkipped);
        Assert.Contains(binaryFile, result.SkippedFiles.Select(s => s.Path));
        Assert.Contains("binary", result.SkippedFiles.First(s => s.Path == binaryFile).Reason.ToLower());
    }

    [Fact]
    public async Task Should_Skip_Ignored_Files()
    {
        // Arrange
        var includedFile = "src/app.cs";
        var ignoredFile = "node_modules/package/index.js";
        var files = new[] { includedFile, ignoredFile };
        
        _repoFsMock.Setup(x => x.EnumerateFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(files));
        _repoFsMock.Setup(x => x.GetFileInfoAsync(includedFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileMetadata(includedFile, 100, DateTime.UtcNow, false));
        _repoFsMock.Setup(x => x.ReadAllTextAsync(includedFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync("public class App { }");
        _ignoreParserMock.Setup(x => x.ShouldIgnore(includedFile)).Returns(false);
        _ignoreParserMock.Setup(x => x.ShouldIgnore(ignoredFile)).Returns(true);
        _tokenizerMock.Setup(x => x.Tokenize(It.IsAny<string>())).Returns(new[] { "public" });

        // Act
        var result = await _sut.BuildAsync("/repo", CancellationToken.None);

        // Assert
        Assert.Equal(1, result.FilesIndexed);
        Assert.DoesNotContain(ignoredFile, result.IndexedFiles);
    }

    [Fact]
    public async Task Should_Track_File_Metadata()
    {
        // Arrange
        var filePath = "src/UserService.cs";
        var fileSize = 2048L;
        var lastModified = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        
        _repoFsMock.Setup(x => x.EnumerateFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(new[] { filePath }));
        _repoFsMock.Setup(x => x.GetFileInfoAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileMetadata(filePath, fileSize, lastModified, false));
        _repoFsMock.Setup(x => x.ReadAllTextAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync("public class UserService { }");
        _ignoreParserMock.Setup(x => x.ShouldIgnore(filePath)).Returns(false);
        _tokenizerMock.Setup(x => x.Tokenize(It.IsAny<string>())).Returns(new[] { "user", "service" });

        // Act
        var result = await _sut.BuildAsync("/repo", CancellationToken.None);
        var metadata = result.FileMetadata[filePath];

        // Assert
        Assert.Equal(filePath, metadata.Path);
        Assert.Equal(fileSize, metadata.Size);
        Assert.Equal(lastModified, metadata.LastModified);
    }

    [Fact]
    public async Task Should_Store_Line_Numbers()
    {
        // Arrange
        var filePath = "src/UserService.cs";
        var fileContent = "line 1\nline 2 with keyword\nline 3\nline 4 with keyword";
        
        _repoFsMock.Setup(x => x.EnumerateFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(new[] { filePath }));
        _repoFsMock.Setup(x => x.GetFileInfoAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileMetadata(filePath, 100, DateTime.UtcNow, false));
        _repoFsMock.Setup(x => x.ReadAllTextAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileContent);
        _ignoreParserMock.Setup(x => x.ShouldIgnore(filePath)).Returns(false);
        _tokenizerMock.Setup(x => x.TokenizeWithPositions(fileContent))
            .Returns(new[]
            {
                new TokenPosition("line", 1, 0),
                new TokenPosition("line", 2, 0),
                new TokenPosition("keyword", 2, 13),
                new TokenPosition("line", 3, 0),
                new TokenPosition("line", 4, 0),
                new TokenPosition("keyword", 4, 13)
            });

        // Act
        var result = await _sut.BuildAsync("/repo", CancellationToken.None);
        var keywordPositions = result.GetTermPositions("keyword", filePath);

        // Assert
        Assert.Equal(2, keywordPositions.Count());
        Assert.Contains(keywordPositions, p => p.Line == 2);
        Assert.Contains(keywordPositions, p => p.Line == 4);
    }

    [Fact]
    public async Task Should_Handle_Empty_File()
    {
        // Arrange
        var filePath = "src/empty.cs";
        
        _repoFsMock.Setup(x => x.EnumerateFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(new[] { filePath }));
        _repoFsMock.Setup(x => x.GetFileInfoAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileMetadata(filePath, 0, DateTime.UtcNow, false));
        _repoFsMock.Setup(x => x.ReadAllTextAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);
        _ignoreParserMock.Setup(x => x.ShouldIgnore(filePath)).Returns(false);
        _tokenizerMock.Setup(x => x.Tokenize(string.Empty)).Returns(Array.Empty<string>());

        // Act
        var result = await _sut.BuildAsync("/repo", CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.FilesIndexed);
        Assert.Contains(filePath, result.IndexedFiles);
    }

    [Fact]
    public async Task Should_Handle_Large_File_With_Size_Limit()
    {
        // Arrange
        var smallFile = "src/small.cs";
        var largeFile = "src/generated.cs";
        var maxSizeKb = 500;
        var options = new IndexBuildOptions { MaxFileSizeKb = maxSizeKb };
        
        _repoFsMock.Setup(x => x.EnumerateFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(new[] { smallFile, largeFile }));
        _repoFsMock.Setup(x => x.GetFileInfoAsync(smallFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileMetadata(smallFile, 1024, DateTime.UtcNow, false)); // 1 KB
        _repoFsMock.Setup(x => x.GetFileInfoAsync(largeFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileMetadata(largeFile, 600 * 1024, DateTime.UtcNow, false)); // 600 KB > limit
        _repoFsMock.Setup(x => x.ReadAllTextAsync(smallFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync("small content");
        _ignoreParserMock.Setup(x => x.ShouldIgnore(It.IsAny<string>())).Returns(false);
        _tokenizerMock.Setup(x => x.Tokenize(It.IsAny<string>())).Returns(new[] { "small" });

        // Act
        var result = await _sut.BuildAsync("/repo", options, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.FilesIndexed);
        Assert.Equal(1, result.FilesSkipped);
        Assert.Contains(largeFile, result.SkippedFiles.Select(s => s.Path));
        Assert.Contains("size", result.SkippedFiles.First(s => s.Path == largeFile).Reason.ToLower());
    }

    [Fact]
    public async Task Should_Handle_Unicode_Content()
    {
        // Arrange
        var filePath = "src/i18n/messages.cs";
        var unicodeContent = "// 日本語コメント\npublic string Message = \"Привет мир\"; // Chinese: 你好世界";
        
        _repoFsMock.Setup(x => x.EnumerateFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(new[] { filePath }));
        _repoFsMock.Setup(x => x.GetFileInfoAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileMetadata(filePath, 200, DateTime.UtcNow, false));
        _repoFsMock.Setup(x => x.ReadAllTextAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(unicodeContent);
        _ignoreParserMock.Setup(x => x.ShouldIgnore(filePath)).Returns(false);
        _tokenizerMock.Setup(x => x.Tokenize(unicodeContent))
            .Returns(new[] { "日本語コメント", "public", "string", "message", "привет", "мир", "你好世界" });

        // Act
        var result = await _sut.BuildAsync("/repo", CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.FilesIndexed);
    }

    [Fact]
    public async Task Should_Persist_Index_To_Disk()
    {
        // Arrange
        var indexPath = Path.Combine(Path.GetTempPath(), $"test_index_{Guid.NewGuid()}.db");
        var filePath = "src/app.cs";
        
        try
        {
            _repoFsMock.Setup(x => x.EnumerateFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(AsyncEnumerable(new[] { filePath }));
            _repoFsMock.Setup(x => x.GetFileInfoAsync(filePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FileMetadata(filePath, 100, DateTime.UtcNow, false));
            _repoFsMock.Setup(x => x.ReadAllTextAsync(filePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync("public class App { }");
            _ignoreParserMock.Setup(x => x.ShouldIgnore(filePath)).Returns(false);
            _tokenizerMock.Setup(x => x.Tokenize(It.IsAny<string>())).Returns(new[] { "public", "class", "app" });

            var options = new IndexBuildOptions { IndexPath = indexPath };

            // Act
            var result = await _sut.BuildAsync("/repo", options, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.True(File.Exists(indexPath), "Index file should exist on disk");
            Assert.True(new FileInfo(indexPath).Length > 0, "Index file should not be empty");
        }
        finally
        {
            if (File.Exists(indexPath)) File.Delete(indexPath);
        }
    }

    [Fact]
    public async Task Should_Handle_Corrupted_Index_File()
    {
        // Arrange
        var indexPath = Path.Combine(Path.GetTempPath(), $"corrupted_index_{Guid.NewGuid()}.db");
        
        try
        {
            // Create a corrupted file (not valid SQLite)
            await File.WriteAllTextAsync(indexPath, "THIS IS NOT A VALID SQLITE DATABASE FILE");
            
            var loader = new IndexLoader(_loggerMock.Object);

            // Act
            var loadResult = await loader.LoadAsync(indexPath, CancellationToken.None);

            // Assert
            Assert.False(loadResult.Success);
            Assert.Equal("ACODE-IDX-004", loadResult.ErrorCode);
            Assert.Contains("corrupt", loadResult.ErrorMessage.ToLower());
        }
        finally
        {
            if (File.Exists(indexPath)) File.Delete(indexPath);
        }
    }

    private static async IAsyncEnumerable<string> AsyncEnumerable(IEnumerable<string> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
        await Task.CompletedTask;
    }
}
```

#### TokenizerTests.cs

```csharp
using System.Linq;
using Acode.Infrastructure.Index;
using Xunit;

namespace Acode.Tests.Unit.Index;

public sealed class TokenizerTests
{
    private readonly CodeTokenizer _sut = new();

    [Fact]
    public void Should_Tokenize_Code_Identifiers()
    {
        // Arrange
        var code = "public class UserService : IUserService";

        // Act
        var tokens = _sut.Tokenize(code).ToList();

        // Assert
        Assert.Contains("public", tokens);
        Assert.Contains("class", tokens);
        Assert.Contains("userservice", tokens);
        Assert.Contains("iuserservice", tokens);
    }

    [Theory]
    [InlineData("getUserById", new[] { "get", "user", "by", "id" })]
    [InlineData("processHTTPRequest", new[] { "process", "http", "request" })]
    [InlineData("XMLParser", new[] { "xml", "parser" })]
    [InlineData("parseJSON", new[] { "parse", "json" })]
    [InlineData("IOStream", new[] { "io", "stream" })]
    public void Should_Tokenize_CamelCase(string identifier, string[] expected)
    {
        // Act
        var tokens = _sut.Tokenize(identifier).ToList();

        // Assert
        foreach (var expectedToken in expected)
        {
            Assert.Contains(expectedToken, tokens);
        }
    }

    [Theory]
    [InlineData("get_user_by_id", new[] { "get", "user", "by", "id" })]
    [InlineData("PROCESS_HTTP_REQUEST", new[] { "process", "http", "request" })]
    [InlineData("xml_parser_v2", new[] { "xml", "parser", "v2" })]
    [InlineData("__private_field", new[] { "private", "field" })]
    public void Should_Tokenize_Snake_Case(string identifier, string[] expected)
    {
        // Act
        var tokens = _sut.Tokenize(identifier).ToList();

        // Assert
        foreach (var expectedToken in expected)
        {
            Assert.Contains(expectedToken, tokens);
        }
    }

    [Theory]
    [InlineData("user123", new[] { "user", "123" })]
    [InlineData("v2Controller", new[] { "v", "2", "controller" })]
    [InlineData("sha256Hash", new[] { "sha", "256", "hash" })]
    [InlineData("100percentComplete", new[] { "100", "percent", "complete" })]
    public void Should_Handle_Numbers(string identifier, string[] expected)
    {
        // Act
        var tokens = _sut.Tokenize(identifier).ToList();

        // Assert
        foreach (var expectedToken in expected)
        {
            Assert.Contains(expectedToken, tokens);
        }
    }

    [Fact]
    public void Should_Handle_Punctuation()
    {
        // Arrange
        var code = "user.getName(); // Get the user's name";

        // Act
        var tokens = _sut.Tokenize(code).ToList();

        // Assert
        Assert.Contains("user", tokens);
        Assert.Contains("get", tokens);
        Assert.Contains("name", tokens);
        Assert.DoesNotContain(".", tokens);
        Assert.DoesNotContain(";", tokens);
        Assert.DoesNotContain("//", tokens);
        Assert.DoesNotContain("'", tokens);
    }

    [Fact]
    public void Should_Normalize_Tokens_To_Lowercase()
    {
        // Arrange
        var code = "PUBLIC CLASS USERSERVICE";

        // Act
        var tokens = _sut.Tokenize(code).ToList();

        // Assert
        Assert.All(tokens, token => Assert.Equal(token, token.ToLowerInvariant()));
        Assert.Contains("public", tokens);
        Assert.Contains("class", tokens);
    }

    [Fact]
    public void Should_Remove_Stop_Words()
    {
        // Arrange
        var code = "the user is a member of the group";

        // Act
        var tokens = _sut.Tokenize(code).ToList();

        // Assert
        Assert.DoesNotContain("the", tokens);
        Assert.DoesNotContain("is", tokens);
        Assert.DoesNotContain("a", tokens);
        Assert.DoesNotContain("of", tokens);
        Assert.Contains("user", tokens);
        Assert.Contains("member", tokens);
        Assert.Contains("group", tokens);
    }

    [Fact]
    public void Should_Track_Token_Positions()
    {
        // Arrange
        var code = "line one\nline two\nline three";

        // Act
        var positions = _sut.TokenizeWithPositions(code).ToList();

        // Assert
        var lineOnePositions = positions.Where(p => p.Line == 1).ToList();
        var lineTwoPositions = positions.Where(p => p.Line == 2).ToList();
        var lineThreePositions = positions.Where(p => p.Line == 3).ToList();
        
        Assert.Single(lineOnePositions.Where(p => p.Token == "one"));
        Assert.Single(lineTwoPositions.Where(p => p.Token == "two"));
        Assert.Single(lineThreePositions.Where(p => p.Token == "three"));
    }
}
```

#### SearchEngineTests.cs

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Index;
using Acode.Infrastructure.Index;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Acode.Tests.Unit.Index;

public sealed class SearchEngineTests
{
    private readonly Mock<IIndexStore> _indexStoreMock;
    private readonly Mock<ILogger<SearchEngine>> _loggerMock;
    private readonly SearchEngine _sut;

    public SearchEngineTests()
    {
        _indexStoreMock = new Mock<IIndexStore>();
        _loggerMock = new Mock<ILogger<SearchEngine>>();
        _sut = new SearchEngine(_indexStoreMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Should_Find_Single_Word()
    {
        // Arrange
        var query = new SearchQuery("UserService");
        var indexedDoc = new IndexedDocument("src/UserService.cs", new Dictionary<string, List<int>>
        {
            ["userservice"] = new List<int> { 1, 5, 10 }
        });
        
        _indexStoreMock.Setup(x => x.SearchTermAsync("userservice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { indexedDoc });
        _indexStoreMock.Setup(x => x.GetDocumentContentAsync("src/UserService.cs", 1, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync("public class UserService : IUserService");

        // Act
        var results = await _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.Equal("src/UserService.cs", results[0].FilePath);
        Assert.Contains(1, results[0].MatchedLines);
    }

    [Fact]
    public async Task Should_Find_Multiple_Words_AND()
    {
        // Arrange
        var query = new SearchQuery("user service"); // Default is AND
        
        _indexStoreMock.Setup(x => x.SearchTermAsync("user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new IndexedDocument("src/UserService.cs", new Dictionary<string, List<int>> { ["user"] = new() { 1 } }),
                new IndexedDocument("src/UserController.cs", new Dictionary<string, List<int>> { ["user"] = new() { 5 } })
            });
        _indexStoreMock.Setup(x => x.SearchTermAsync("service", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new IndexedDocument("src/UserService.cs", new Dictionary<string, List<int>> { ["service"] = new() { 1 } }),
                new IndexedDocument("src/OrderService.cs", new Dictionary<string, List<int>> { ["service"] = new() { 2 } })
            });

        // Act
        var results = await _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.Equal("src/UserService.cs", results[0].FilePath);
    }

    [Fact]
    public async Task Should_Find_Multiple_Words_OR()
    {
        // Arrange
        var query = new SearchQuery("user OR order");
        
        _indexStoreMock.Setup(x => x.SearchTermAsync("user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new IndexedDocument("src/UserService.cs", new Dictionary<string, List<int>> { ["user"] = new() { 1 } }) });
        _indexStoreMock.Setup(x => x.SearchTermAsync("order", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new IndexedDocument("src/OrderService.cs", new Dictionary<string, List<int>> { ["order"] = new() { 1 } }) });

        // Act
        var results = await _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.FilePath == "src/UserService.cs");
        Assert.Contains(results, r => r.FilePath == "src/OrderService.cs");
    }

    [Fact]
    public async Task Should_Find_Exact_Phrase()
    {
        // Arrange
        var query = new SearchQuery("\"public class\"");
        
        _indexStoreMock.Setup(x => x.SearchPhraseAsync(new[] { "public", "class" }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new IndexedDocument("src/App.cs", new Dictionary<string, List<int>> { ["public"] = new() { 1 } }) });

        // Act
        var results = await _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.Equal("src/App.cs", results[0].FilePath);
    }

    [Fact]
    public async Task Should_Find_With_Wildcard_Suffix()
    {
        // Arrange
        var query = new SearchQuery("User*");
        
        _indexStoreMock.Setup(x => x.SearchPrefixAsync("user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new IndexedDocument("src/UserService.cs", new Dictionary<string, List<int>> { ["userservice"] = new() { 1 } }),
                new IndexedDocument("src/UserController.cs", new Dictionary<string, List<int>> { ["usercontroller"] = new() { 2 } })
            });

        // Act
        var results = await _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task Should_Find_With_Wildcard_Prefix()
    {
        // Arrange
        var query = new SearchQuery("*Service");
        
        _indexStoreMock.Setup(x => x.SearchSuffixAsync("service", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new IndexedDocument("src/UserService.cs", new Dictionary<string, List<int>> { ["userservice"] = new() { 1 } }),
                new IndexedDocument("src/OrderService.cs", new Dictionary<string, List<int>> { ["orderservice"] = new() { 2 } })
            });

        // Act
        var results = await _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task Should_Exclude_With_Minus()
    {
        // Arrange
        var query = new SearchQuery("service -test");
        
        _indexStoreMock.Setup(x => x.SearchTermAsync("service", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new IndexedDocument("src/UserService.cs", new Dictionary<string, List<int>> { ["service"] = new() { 1 } }),
                new IndexedDocument("tests/UserServiceTests.cs", new Dictionary<string, List<int>> { ["service"] = new() { 5 }, ["test"] = new() { 1 } })
            });
        _indexStoreMock.Setup(x => x.SearchTermAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new IndexedDocument("tests/UserServiceTests.cs", new Dictionary<string, List<int>> { ["test"] = new() { 1 } })
            });

        // Act
        var results = await _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.Equal("src/UserService.cs", results[0].FilePath);
    }

    [Fact]
    public async Task Should_Handle_Case_Insensitive_By_Default()
    {
        // Arrange
        var query = new SearchQuery("USERSERVICE");
        
        _indexStoreMock.Setup(x => x.SearchTermAsync("userservice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new IndexedDocument("src/UserService.cs", new Dictionary<string, List<int>> { ["userservice"] = new() { 1 } }) });

        // Act
        var results = await _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.Single(results);
    }

    [Fact]
    public async Task Should_Return_Line_Numbers()
    {
        // Arrange
        var query = new SearchQuery("controller");
        var indexedDoc = new IndexedDocument("src/UserController.cs", new Dictionary<string, List<int>>
        {
            ["controller"] = new List<int> { 5, 15, 25 }
        });
        
        _indexStoreMock.Setup(x => x.SearchTermAsync("controller", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { indexedDoc });

        // Act
        var results = await _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.Equal(new[] { 5, 15, 25 }, results[0].MatchedLines);
    }

    [Fact]
    public async Task Should_Return_Snippets_With_Context()
    {
        // Arrange
        var query = new SearchQuery("process");
        var fileContent = new[]
        {
            "// Line 1",
            "// Line 2",
            "public void Process() {",  // Line 3 - match
            "    // implementation",
            "}"
        };
        
        _indexStoreMock.Setup(x => x.SearchTermAsync("process", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new IndexedDocument("src/Handler.cs", new Dictionary<string, List<int>> { ["process"] = new() { 3 } }) });
        _indexStoreMock.Setup(x => x.GetDocumentContentAsync("src/Handler.cs", 1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Join("\n", fileContent));

        // Act
        var results = await _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.NotEmpty(results[0].Snippets);
        Assert.Contains("Process", results[0].Snippets[0].Text);
    }

    [Fact]
    public async Task Should_Return_Relevance_Score_Between_0_And_1()
    {
        // Arrange
        var query = new SearchQuery("service");
        
        _indexStoreMock.Setup(x => x.SearchTermAsync("service", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new IndexedDocument("src/UserService.cs", new Dictionary<string, List<int>> { ["service"] = new() { 1, 5, 10 } }) });
        _indexStoreMock.Setup(x => x.GetTotalDocumentCount()).Returns(100);

        // Act
        var results = await _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.InRange(results[0].Score, 0.0, 1.0);
    }

    [Fact]
    public async Task Should_Rank_By_Relevance_Descending()
    {
        // Arrange
        var query = new SearchQuery("user");
        
        _indexStoreMock.Setup(x => x.SearchTermAsync("user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new IndexedDocument("src/UserService.cs", new Dictionary<string, List<int>> { ["user"] = new() { 1, 5, 10, 15, 20 } }), // 5 occurrences
                new IndexedDocument("src/Controller.cs", new Dictionary<string, List<int>> { ["user"] = new() { 100 } }),              // 1 occurrence
                new IndexedDocument("src/UserManager.cs", new Dictionary<string, List<int>> { ["user"] = new() { 1, 2, 3 } })          // 3 occurrences
            });
        _indexStoreMock.Setup(x => x.GetTotalDocumentCount()).Returns(100);

        // Act
        var results = await _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.True(results[0].Score >= results[1].Score);
        Assert.True(results[1].Score >= results[2].Score);
        Assert.Equal("src/UserService.cs", results[0].FilePath); // Most occurrences = highest score
    }

    [Fact]
    public async Task Should_Support_Pagination()
    {
        // Arrange
        var docs = Enumerable.Range(1, 50)
            .Select(i => new IndexedDocument($"src/File{i}.cs", new Dictionary<string, List<int>> { ["keyword"] = new() { 1 } }))
            .ToArray();
        
        _indexStoreMock.Setup(x => x.SearchTermAsync("keyword", It.IsAny<CancellationToken>()))
            .ReturnsAsync(docs);
        
        var query = new SearchQuery("keyword") { Skip = 10, Take = 5 };

        // Act
        var results = await _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.Equal(5, results.Count);
        Assert.Equal("src/File11.cs", results[0].FilePath); // Skip 10, so start at 11
        Assert.Equal(50, results.TotalCount); // Total should be all matches
    }

    [Fact]
    public async Task Should_Handle_No_Results()
    {
        // Arrange
        var query = new SearchQuery("nonexistentterm12345");
        
        _indexStoreMock.Setup(x => x.SearchTermAsync("nonexistentterm12345", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<IndexedDocument>());

        // Act
        var results = await _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.Empty(results);
        Assert.Equal(0, results.TotalCount);
    }

    [Fact]
    public async Task Should_Handle_Empty_Query()
    {
        // Arrange
        var query = new SearchQuery("");

        // Act
        var results = await _sut.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task Should_Handle_Invalid_Query()
    {
        // Arrange
        var query = new SearchQuery("\"unclosed quote");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SearchQueryException>(
            () => _sut.SearchAsync(query, CancellationToken.None));
        
        Assert.Equal("ACODE-IDX-002", exception.ErrorCode);
        Assert.Contains("unbalanced", exception.Message.ToLower());
    }
}
```

#### SearchQueryParserTests.cs

```csharp
using System.Linq;
using Acode.Infrastructure.Index;
using Xunit;

namespace Acode.Tests.Unit.Index;

public sealed class SearchQueryParserTests
{
    private readonly SearchQueryParser _sut = new();

    [Fact]
    public void Should_Parse_Single_Word()
    {
        // Arrange
        var queryText = "UserService";

        // Act
        var result = _sut.Parse(queryText);

        // Assert
        Assert.Single(result.Terms);
        Assert.Equal("userservice", result.Terms[0].Value);
        Assert.Equal(TermType.Required, result.Terms[0].Type);
    }

    [Fact]
    public void Should_Parse_Multiple_Words_As_AND()
    {
        // Arrange
        var queryText = "user service controller";

        // Act
        var result = _sut.Parse(queryText);

        // Assert
        Assert.Equal(3, result.Terms.Count);
        Assert.All(result.Terms, t => Assert.Equal(TermType.Required, t.Type));
        Assert.Contains(result.Terms, t => t.Value == "user");
        Assert.Contains(result.Terms, t => t.Value == "service");
        Assert.Contains(result.Terms, t => t.Value == "controller");
    }

    [Fact]
    public void Should_Parse_Quoted_Phrase()
    {
        // Arrange
        var queryText = "\"public class UserService\"";

        // Act
        var result = _sut.Parse(queryText);

        // Assert
        Assert.Single(result.Phrases);
        Assert.Equal(new[] { "public", "class", "userservice" }, result.Phrases[0].Words);
    }

    [Theory]
    [InlineData("User*", "user", WildcardType.Suffix)]
    [InlineData("*Service", "service", WildcardType.Prefix)]
    [InlineData("*Controller*", "controller", WildcardType.Both)]
    public void Should_Parse_Wildcard(string queryText, string expectedTerm, WildcardType expectedType)
    {
        // Act
        var result = _sut.Parse(queryText);

        // Assert
        Assert.Single(result.Wildcards);
        Assert.Equal(expectedTerm, result.Wildcards[0].Value);
        Assert.Equal(expectedType, result.Wildcards[0].Type);
    }

    [Fact]
    public void Should_Parse_Explicit_AND_Operator()
    {
        // Arrange
        var queryText = "user AND service";

        // Act
        var result = _sut.Parse(queryText);

        // Assert
        Assert.Equal(2, result.Terms.Count);
        Assert.True(result.IsConjunction);
    }

    [Fact]
    public void Should_Parse_OR_Operator()
    {
        // Arrange
        var queryText = "user OR customer OR client";

        // Act
        var result = _sut.Parse(queryText);

        // Assert
        Assert.Equal(3, result.Terms.Count);
        Assert.False(result.IsConjunction);
        Assert.True(result.IsDisjunction);
    }

    [Fact]
    public void Should_Parse_Exclusion()
    {
        // Arrange
        var queryText = "service -test -mock";

        // Act
        var result = _sut.Parse(queryText);

        // Assert
        Assert.Single(result.Terms.Where(t => t.Type == TermType.Required));
        Assert.Equal(2, result.Terms.Count(t => t.Type == TermType.Excluded));
        Assert.Contains(result.Terms, t => t.Value == "test" && t.Type == TermType.Excluded);
        Assert.Contains(result.Terms, t => t.Value == "mock" && t.Type == TermType.Excluded);
    }

    [Fact]
    public void Should_Parse_Combined_Operators()
    {
        // Arrange
        var queryText = "\"public class\" User* -test";

        // Act
        var result = _sut.Parse(queryText);

        // Assert
        Assert.Single(result.Phrases);
        Assert.Equal(new[] { "public", "class" }, result.Phrases[0].Words);
        
        Assert.Single(result.Wildcards);
        Assert.Equal("user", result.Wildcards[0].Value);
        
        Assert.Single(result.Terms.Where(t => t.Type == TermType.Excluded));
        Assert.Equal("test", result.Terms.First(t => t.Type == TermType.Excluded).Value);
    }

    [Fact]
    public void Should_Handle_Special_Characters()
    {
        // Arrange
        var queryText = "user@example.com path/to/file";

        // Act
        var result = _sut.Parse(queryText);

        // Assert
        Assert.Contains(result.Terms, t => t.Value == "user");
        Assert.Contains(result.Terms, t => t.Value == "example");
        Assert.Contains(result.Terms, t => t.Value == "com");
        Assert.Contains(result.Terms, t => t.Value == "path");
        Assert.Contains(result.Terms, t => t.Value == "file");
    }

    [Fact]
    public void Should_Handle_Unbalanced_Quotes()
    {
        // Arrange
        var queryText = "\"unclosed phrase";

        // Act & Assert
        var exception = Assert.Throws<SearchQueryParseException>(() => _sut.Parse(queryText));
        Assert.Contains("unbalanced", exception.Message.ToLower());
    }

    [Fact]
    public void Should_Handle_Empty_Query()
    {
        // Arrange
        var queryText = "";

        // Act
        var result = _sut.Parse(queryText);

        // Assert
        Assert.Empty(result.Terms);
        Assert.Empty(result.Phrases);
        Assert.Empty(result.Wildcards);
    }

    [Fact]
    public void Should_Handle_Whitespace_Only()
    {
        // Arrange
        var queryText = "   \t\n   ";

        // Act
        var result = _sut.Parse(queryText);

        // Assert
        Assert.Empty(result.Terms);
    }
}
```

#### IgnoreRulesTests.cs

```csharp
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Acode.Infrastructure.Index;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Acode.Tests.Unit.Index;

public sealed class IgnoreRulesTests
{
    private readonly Mock<ILogger<IgnoreRuleParser>> _loggerMock;
    private readonly IgnoreRuleParser _sut;

    public IgnoreRulesTests()
    {
        _loggerMock = new Mock<ILogger<IgnoreRuleParser>>();
        _sut = new IgnoreRuleParser(_loggerMock.Object);
    }

    [Fact]
    public void Should_Parse_Gitignore_File()
    {
        // Arrange
        var content = @"
# Build outputs
bin/
obj/
*.dll
*.exe

# IDE files
.vs/
*.suo
";

        // Act
        var rules = _sut.ParseContent(content, ".gitignore");

        // Assert
        Assert.Equal(6, rules.Count());
        Assert.Contains(rules, r => r.Pattern == "bin/");
        Assert.Contains(rules, r => r.Pattern == "*.dll");
        Assert.Contains(rules, r => r.Pattern == ".vs/");
    }

    [Fact]
    public void Should_Parse_Empty_Gitignore()
    {
        // Arrange
        var content = "";

        // Act
        var rules = _sut.ParseContent(content, ".gitignore");

        // Assert
        Assert.Empty(rules);
    }

    [Fact]
    public void Should_Parse_Comment_Lines()
    {
        // Arrange
        var content = @"
# This is a comment
*.log
# Another comment
*.tmp
";

        // Act
        var rules = _sut.ParseContent(content, ".gitignore");

        // Assert
        Assert.Equal(2, rules.Count());
        Assert.DoesNotContain(rules, r => r.Pattern.Contains("#"));
    }

    [Fact]
    public void Should_Match_Exact_Filename()
    {
        // Arrange
        var rules = _sut.ParseContent("README.md", ".gitignore");
        var matcher = new IgnoreMatcher(rules);

        // Act & Assert
        Assert.True(matcher.IsIgnored("README.md"));
        Assert.True(matcher.IsIgnored("docs/README.md"));
        Assert.False(matcher.IsIgnored("README.txt"));
        Assert.False(matcher.IsIgnored("OTHER.md"));
    }

    [Fact]
    public void Should_Match_Glob_Pattern()
    {
        // Arrange
        var rules = _sut.ParseContent("*.log", ".gitignore");
        var matcher = new IgnoreMatcher(rules);

        // Act & Assert
        Assert.True(matcher.IsIgnored("error.log"));
        Assert.True(matcher.IsIgnored("logs/debug.log"));
        Assert.False(matcher.IsIgnored("logfile.txt"));
        Assert.False(matcher.IsIgnored("log"));
    }

    [Fact]
    public void Should_Match_Directory_Pattern()
    {
        // Arrange
        var rules = _sut.ParseContent("node_modules/", ".gitignore");
        var matcher = new IgnoreMatcher(rules);

        // Act & Assert
        Assert.True(matcher.IsIgnored("node_modules/package/index.js"));
        Assert.True(matcher.IsIgnored("frontend/node_modules/lodash/index.js"));
        Assert.False(matcher.IsIgnored("node_modules_backup.zip"));
    }

    [Fact]
    public void Should_Match_Double_Star()
    {
        // Arrange
        var rules = _sut.ParseContent("**/logs/**/*.log", ".gitignore");
        var matcher = new IgnoreMatcher(rules);

        // Act & Assert
        Assert.True(matcher.IsIgnored("logs/error.log"));
        Assert.True(matcher.IsIgnored("app/logs/2024/error.log"));
        Assert.True(matcher.IsIgnored("src/server/logs/debug/trace.log"));
        Assert.False(matcher.IsIgnored("logs.txt"));
        Assert.False(matcher.IsIgnored("mylogs/file.txt"));
    }

    [Fact]
    public void Should_Handle_Negation_Pattern()
    {
        // Arrange
        var content = @"
*.log
!important.log
";
        var rules = _sut.ParseContent(content, ".gitignore");
        var matcher = new IgnoreMatcher(rules);

        // Act & Assert
        Assert.True(matcher.IsIgnored("error.log"));
        Assert.True(matcher.IsIgnored("debug.log"));
        Assert.False(matcher.IsIgnored("important.log"));
    }

    [Fact]
    public void Should_Handle_Escaped_Characters()
    {
        // Arrange
        var content = @"
\#file.txt
\!important.txt
file\ with\ spaces.txt
";
        var rules = _sut.ParseContent(content, ".gitignore");
        var matcher = new IgnoreMatcher(rules);

        // Act & Assert
        Assert.True(matcher.IsIgnored("#file.txt"));
        Assert.True(matcher.IsIgnored("!important.txt"));
        Assert.True(matcher.IsIgnored("file with spaces.txt"));
    }

    [Fact]
    public void Should_Apply_Order_Priority()
    {
        // Arrange - later rules override earlier ones
        var content = @"
*.txt
!important.txt
important.txt
";
        var rules = _sut.ParseContent(content, ".gitignore");
        var matcher = new IgnoreMatcher(rules);

        // Act & Assert
        Assert.True(matcher.IsIgnored("random.txt"));
        Assert.True(matcher.IsIgnored("important.txt")); // Re-ignored by last rule
    }

    [Fact]
    public void Should_Merge_Multiple_Ignore_Files()
    {
        // Arrange
        var gitignore = "*.log\nbin/";
        var agentignore = "*.tmp\n!important.log";
        
        var gitRules = _sut.ParseContent(gitignore, ".gitignore");
        var agentRules = _sut.ParseContent(agentignore, ".agentignore");
        var matcher = new IgnoreMatcher(gitRules.Concat(agentRules), agentignorePrecedence: true);

        // Act & Assert
        Assert.True(matcher.IsIgnored("error.log"));
        Assert.False(matcher.IsIgnored("important.log")); // .agentignore negation takes precedence
        Assert.True(matcher.IsIgnored("temp.tmp"));
        Assert.True(matcher.IsIgnored("bin/app.exe"));
    }

    [Fact]
    public void Should_Apply_Custom_Ignores_From_Config()
    {
        // Arrange
        var customPatterns = new[] { "*.generated.cs", "obj/**", ".vs/**" };
        var matcher = IgnoreMatcher.FromPatterns(customPatterns);

        // Act & Assert
        Assert.True(matcher.IsIgnored("Model.generated.cs"));
        Assert.True(matcher.IsIgnored("obj/Debug/app.dll"));
        Assert.True(matcher.IsIgnored(".vs/config/settings.json"));
        Assert.False(matcher.IsIgnored("Model.cs"));
    }

    [Fact]
    public void Should_Handle_Trailing_Spaces()
    {
        // Arrange - trailing spaces should be trimmed unless escaped
        var content = "*.log   \nfile.txt \\ ";  // Second line has escaped trailing space
        var rules = _sut.ParseContent(content, ".gitignore");
        var matcher = new IgnoreMatcher(rules);

        // Act & Assert
        Assert.True(matcher.IsIgnored("error.log"));
        Assert.True(matcher.IsIgnored("file.txt ")); // With trailing space
        Assert.False(matcher.IsIgnored("file.txt")); // Without trailing space
    }

    [Fact]
    public void Should_Handle_Rooted_Pattern()
    {
        // Arrange - pattern starting with / is anchored to root
        var rules = _sut.ParseContent("/build/", ".gitignore");
        var matcher = new IgnoreMatcher(rules, rootPath: "/repo");

        // Act & Assert
        Assert.True(matcher.IsIgnored("/repo/build/output.dll"));
        Assert.False(matcher.IsIgnored("/repo/src/build/temp.txt")); // Not at root
    }
}
```

#### IncrementalUpdaterTests.cs

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Index;
using Acode.Infrastructure.Index;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Acode.Tests.Unit.Index;

public sealed class IncrementalUpdaterTests
{
    private readonly Mock<IRepoFileSystem> _repoFsMock;
    private readonly Mock<IIndexStore> _indexStoreMock;
    private readonly Mock<IIndexBuilder> _builderMock;
    private readonly Mock<ILogger<IncrementalUpdater>> _loggerMock;
    private readonly IncrementalUpdater _sut;

    public IncrementalUpdaterTests()
    {
        _repoFsMock = new Mock<IRepoFileSystem>();
        _indexStoreMock = new Mock<IIndexStore>();
        _builderMock = new Mock<IIndexBuilder>();
        _loggerMock = new Mock<ILogger<IncrementalUpdater>>();
        
        _sut = new IncrementalUpdater(
            _repoFsMock.Object,
            _indexStoreMock.Object,
            _builderMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Should_Detect_Modified_File_By_Mtime()
    {
        // Arrange
        var filePath = "src/UserService.cs";
        var oldMtime = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var newMtime = new DateTime(2024, 1, 2, 10, 0, 0, DateTimeKind.Utc);
        
        _indexStoreMock.Setup(x => x.GetFileMetadataAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredFileMetadata(filePath, 1000, oldMtime));
        _repoFsMock.Setup(x => x.GetFileInfoAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileMetadata(filePath, 1000, newMtime, false));
        _repoFsMock.Setup(x => x.EnumerateFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(new[] { filePath }));
        _indexStoreMock.Setup(x => x.GetAllFilePathsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { filePath });

        // Act
        var changes = await _sut.DetectChangesAsync("/repo", CancellationToken.None);

        // Assert
        Assert.Single(changes.Modified);
        Assert.Equal(filePath, changes.Modified[0]);
    }

    [Fact]
    public async Task Should_Detect_New_File()
    {
        // Arrange
        var existingFile = "src/UserService.cs";
        var newFile = "src/OrderService.cs";
        
        _repoFsMock.Setup(x => x.EnumerateFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(new[] { existingFile, newFile }));
        _indexStoreMock.Setup(x => x.GetAllFilePathsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { existingFile }); // Only existing file in index
        _indexStoreMock.Setup(x => x.GetFileMetadataAsync(existingFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredFileMetadata(existingFile, 1000, DateTime.UtcNow));
        _repoFsMock.Setup(x => x.GetFileInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string path, CancellationToken _) => new FileMetadata(path, 1000, DateTime.UtcNow, false));

        // Act
        var changes = await _sut.DetectChangesAsync("/repo", CancellationToken.None);

        // Assert
        Assert.Single(changes.Added);
        Assert.Equal(newFile, changes.Added[0]);
    }

    [Fact]
    public async Task Should_Detect_Deleted_File()
    {
        // Arrange
        var existingFile = "src/UserService.cs";
        var deletedFile = "src/OldService.cs";
        
        _repoFsMock.Setup(x => x.EnumerateFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(new[] { existingFile })); // deletedFile not on disk
        _indexStoreMock.Setup(x => x.GetAllFilePathsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { existingFile, deletedFile }); // Both in index
        _indexStoreMock.Setup(x => x.GetFileMetadataAsync(existingFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredFileMetadata(existingFile, 1000, DateTime.UtcNow));
        _repoFsMock.Setup(x => x.GetFileInfoAsync(existingFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileMetadata(existingFile, 1000, DateTime.UtcNow, false));

        // Act
        var changes = await _sut.DetectChangesAsync("/repo", CancellationToken.None);

        // Assert
        Assert.Single(changes.Deleted);
        Assert.Equal(deletedFile, changes.Deleted[0]);
    }

    [Fact]
    public async Task Should_Detect_Renamed_File()
    {
        // Arrange
        var oldPath = "src/UserService.cs";
        var newPath = "src/Services/UserService.cs";
        var contentHash = "abc123hash";
        
        _repoFsMock.Setup(x => x.EnumerateFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(new[] { newPath }));
        _indexStoreMock.Setup(x => x.GetAllFilePathsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { oldPath });
        _indexStoreMock.Setup(x => x.GetFileMetadataAsync(oldPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredFileMetadata(oldPath, 1000, DateTime.UtcNow, contentHash));
        _repoFsMock.Setup(x => x.GetFileInfoAsync(newPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileMetadata(newPath, 1000, DateTime.UtcNow, false));
        _repoFsMock.Setup(x => x.ComputeHashAsync(newPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentHash); // Same hash = renamed

        // Act
        var changes = await _sut.DetectChangesAsync("/repo", CancellationToken.None);

        // Assert
        Assert.Single(changes.Renamed);
        Assert.Equal(oldPath, changes.Renamed[0].OldPath);
        Assert.Equal(newPath, changes.Renamed[0].NewPath);
    }

    [Fact]
    public async Task Should_Update_Only_Changed_Files()
    {
        // Arrange
        var unchangedFile = "src/Unchanged.cs";
        var modifiedFile = "src/Modified.cs";
        var now = DateTime.UtcNow;
        
        _repoFsMock.Setup(x => x.EnumerateFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(new[] { unchangedFile, modifiedFile }));
        _indexStoreMock.Setup(x => x.GetAllFilePathsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { unchangedFile, modifiedFile });
        _indexStoreMock.Setup(x => x.GetFileMetadataAsync(unchangedFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredFileMetadata(unchangedFile, 100, now)); // Same mtime
        _indexStoreMock.Setup(x => x.GetFileMetadataAsync(modifiedFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredFileMetadata(modifiedFile, 100, now.AddHours(-1))); // Older mtime
        _repoFsMock.Setup(x => x.GetFileInfoAsync(unchangedFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileMetadata(unchangedFile, 100, now, false));
        _repoFsMock.Setup(x => x.GetFileInfoAsync(modifiedFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileMetadata(modifiedFile, 100, now, false)); // Newer mtime

        // Act
        var result = await _sut.UpdateAsync("/repo", CancellationToken.None);

        // Assert
        _builderMock.Verify(x => x.IndexFileAsync(modifiedFile, It.IsAny<CancellationToken>()), Times.Once);
        _builderMock.Verify(x => x.IndexFileAsync(unchangedFile, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_Remove_Deleted_From_Index()
    {
        // Arrange
        var deletedFile = "src/Deleted.cs";
        
        _repoFsMock.Setup(x => x.EnumerateFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(Array.Empty<string>()));
        _indexStoreMock.Setup(x => x.GetAllFilePathsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { deletedFile });

        // Act
        await _sut.UpdateAsync("/repo", CancellationToken.None);

        // Assert
        _indexStoreMock.Verify(x => x.RemoveFileAsync(deletedFile, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Add_New_To_Index()
    {
        // Arrange
        var newFile = "src/NewFile.cs";
        
        _repoFsMock.Setup(x => x.EnumerateFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(new[] { newFile }));
        _repoFsMock.Setup(x => x.GetFileInfoAsync(newFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileMetadata(newFile, 100, DateTime.UtcNow, false));
        _indexStoreMock.Setup(x => x.GetAllFilePathsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        // Act
        await _sut.UpdateAsync("/repo", CancellationToken.None);

        // Assert
        _builderMock.Verify(x => x.IndexFileAsync(newFile, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_Track_Last_Update_Timestamp()
    {
        // Arrange
        var beforeUpdate = DateTime.UtcNow;
        
        _repoFsMock.Setup(x => x.EnumerateFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(Array.Empty<string>()));
        _indexStoreMock.Setup(x => x.GetAllFilePathsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var result = await _sut.UpdateAsync("/repo", CancellationToken.None);
        var afterUpdate = DateTime.UtcNow;

        // Assert
        Assert.InRange(result.LastUpdated, beforeUpdate, afterUpdate);
        _indexStoreMock.Verify(x => x.SetLastUpdatedAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static async IAsyncEnumerable<string> ToAsyncEnumerable(IEnumerable<string> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
        await Task.CompletedTask;
    }
}
```

#### FilterTests.cs

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Acode.Domain.Index;
using Acode.Infrastructure.Index;
using Xunit;

namespace Acode.Tests.Unit.Index;

public sealed class FilterTests
{
    [Fact]
    public void Should_Filter_By_Extension()
    {
        // Arrange
        var files = new[]
        {
            new FileInfo("src/App.cs", 100, DateTime.UtcNow),
            new FileInfo("src/App.ts", 100, DateTime.UtcNow),
            new FileInfo("src/App.js", 100, DateTime.UtcNow),
            new FileInfo("docs/README.md", 100, DateTime.UtcNow)
        };
        var filter = new SearchFilter { Extensions = new[] { ".cs", ".ts" } };

        // Act
        var result = SearchFilterApplier.Apply(files, filter).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, f => Assert.True(f.Path.EndsWith(".cs") || f.Path.EndsWith(".ts")));
    }

    [Fact]
    public void Should_Filter_By_Directory()
    {
        // Arrange
        var files = new[]
        {
            new FileInfo("src/Services/UserService.cs", 100, DateTime.UtcNow),
            new FileInfo("src/Controllers/UserController.cs", 100, DateTime.UtcNow),
            new FileInfo("tests/UserServiceTests.cs", 100, DateTime.UtcNow)
        };
        var filter = new SearchFilter { Directory = "src/Services" };

        // Act
        var result = SearchFilterApplier.Apply(files, filter).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("src/Services/UserService.cs", result[0].Path);
    }

    [Fact]
    public void Should_Filter_By_Directory_Recursively()
    {
        // Arrange
        var files = new[]
        {
            new FileInfo("src/Services/UserService.cs", 100, DateTime.UtcNow),
            new FileInfo("src/Services/Orders/OrderService.cs", 100, DateTime.UtcNow),
            new FileInfo("tests/UserServiceTests.cs", 100, DateTime.UtcNow)
        };
        var filter = new SearchFilter { Directory = "src/Services", Recursive = true };

        // Act
        var result = SearchFilterApplier.Apply(files, filter).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, f => Assert.StartsWith("src/Services", f.Path));
    }

    [Fact]
    public void Should_Filter_By_Size()
    {
        // Arrange
        var files = new[]
        {
            new FileInfo("small.txt", 500, DateTime.UtcNow),           // 500 bytes
            new FileInfo("medium.txt", 5 * 1024, DateTime.UtcNow),     // 5 KB
            new FileInfo("large.txt", 500 * 1024, DateTime.UtcNow)     // 500 KB
        };
        var filter = new SearchFilter { MinSizeBytes = 1024, MaxSizeBytes = 100 * 1024 };

        // Act
        var result = SearchFilterApplier.Apply(files, filter).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("medium.txt", result[0].Path);
    }

    [Fact]
    public void Should_Filter_By_Date()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var files = new[]
        {
            new FileInfo("old.txt", 100, now.AddDays(-30)),
            new FileInfo("recent.txt", 100, now.AddDays(-5)),
            new FileInfo("new.txt", 100, now.AddDays(-1))
        };
        var filter = new SearchFilter { ModifiedSince = now.AddDays(-7) };

        // Act
        var result = SearchFilterApplier.Apply(files, filter).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, f => f.Path == "recent.txt");
        Assert.Contains(result, f => f.Path == "new.txt");
        Assert.DoesNotContain(result, f => f.Path == "old.txt");
    }

    [Fact]
    public void Should_Combine_Filters()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var files = new[]
        {
            new FileInfo("src/UserService.cs", 5 * 1024, now.AddDays(-1)),        // Matches all
            new FileInfo("src/OldService.cs", 5 * 1024, now.AddDays(-30)),        // Too old
            new FileInfo("tests/UserServiceTests.cs", 5 * 1024, now.AddDays(-1)), // Wrong dir
            new FileInfo("src/UserService.ts", 5 * 1024, now.AddDays(-1)),        // Wrong ext
            new FileInfo("src/TinyService.cs", 100, now.AddDays(-1))              // Too small
        };
        var filter = new SearchFilter
        {
            Directory = "src",
            Extensions = new[] { ".cs" },
            MinSizeBytes = 1024,
            ModifiedSince = now.AddDays(-7)
        };

        // Act
        var result = SearchFilterApplier.Apply(files, filter).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("src/UserService.cs", result[0].Path);
    }

    [Fact]
    public void Should_Handle_No_Filters()
    {
        // Arrange
        var files = new[]
        {
            new FileInfo("file1.cs", 100, DateTime.UtcNow),
            new FileInfo("file2.ts", 200, DateTime.UtcNow),
            new FileInfo("file3.md", 300, DateTime.UtcNow)
        };
        var filter = new SearchFilter(); // No filters

        // Act
        var result = SearchFilterApplier.Apply(files, filter).ToList();

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Should_Handle_Exclude_Patterns()
    {
        // Arrange
        var files = new[]
        {
            new FileInfo("src/UserService.cs", 100, DateTime.UtcNow),
            new FileInfo("src/UserService.generated.cs", 100, DateTime.UtcNow),
            new FileInfo("tests/UserServiceTests.cs", 100, DateTime.UtcNow)
        };
        var filter = new SearchFilter { ExcludePatterns = new[] { "*.generated.cs", "tests/**" } };

        // Act
        var result = SearchFilterApplier.Apply(files, filter).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("src/UserService.cs", result[0].Path);
    }
}

public record FileInfo(string Path, long Size, DateTime LastModified);
```

### Integration Tests

#### IndexBuildIntegrationTests.cs

```csharp
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Index;
using Acode.Infrastructure.Index;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Acode.Tests.Integration.Index;

public sealed class IndexBuildIntegrationTests : IDisposable
{
    private readonly string _testRepoPath;
    private readonly ServiceProvider _serviceProvider;
    private readonly IIndexService _indexService;

    public IndexBuildIntegrationTests()
    {
        _testRepoPath = Path.Combine(Path.GetTempPath(), $"test_repo_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRepoPath);
        
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddSingleton<IIndexService, IndexService>();
        services.AddSingleton<IIndexBuilder, IndexBuilder>();
        services.AddSingleton<ISearchEngine, SearchEngine>();
        services.AddSingleton<IIgnoreRuleParser, IgnoreRuleParser>();
        services.AddSingleton<ITokenizer, CodeTokenizer>();
        
        _serviceProvider = services.BuildServiceProvider();
        _indexService = _serviceProvider.GetRequiredService<IIndexService>();
    }

    [Fact]
    public async Task Should_Build_Index_For_Small_Repo()
    {
        // Arrange
        CreateTestFiles(100);
        var options = new IndexBuildOptions { RootPath = _testRepoPath };

        // Act
        var startTime = DateTime.UtcNow;
        var result = await _indexService.BuildAsync(options, CancellationToken.None);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        Assert.True(result.Success);
        Assert.Equal(100, result.FilesIndexed);
        Assert.True(duration.TotalSeconds < 5, $"Build took {duration.TotalSeconds}s, expected < 5s");
    }

    [Fact]
    public async Task Should_Build_Index_For_Large_Repo()
    {
        // Arrange
        CreateTestFiles(1000);
        var options = new IndexBuildOptions { RootPath = _testRepoPath };

        // Act
        var startTime = DateTime.UtcNow;
        var result = await _indexService.BuildAsync(options, CancellationToken.None);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1000, result.FilesIndexed);
        Assert.True(duration.TotalSeconds < 30, $"Build took {duration.TotalSeconds}s, expected < 30s");
    }

    [Fact]
    public async Task Should_Respect_Gitignore()
    {
        // Arrange
        CreateTestFiles(50);
        Directory.CreateDirectory(Path.Combine(_testRepoPath, "node_modules"));
        for (int i = 0; i < 20; i++)
        {
            File.WriteAllText(
                Path.Combine(_testRepoPath, "node_modules", $"package{i}.js"),
                $"// Package {i}");
        }
        File.WriteAllText(Path.Combine(_testRepoPath, ".gitignore"), "node_modules/\n*.log");
        
        var options = new IndexBuildOptions { RootPath = _testRepoPath };

        // Act
        var result = await _indexService.BuildAsync(options, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(50, result.FilesIndexed); // Only the 50 test files, not node_modules
        Assert.DoesNotContain(result.IndexedFiles, f => f.Contains("node_modules"));
    }

    [Fact]
    public async Task Should_Handle_Nested_Gitignores()
    {
        // Arrange
        CreateTestFiles(30);
        Directory.CreateDirectory(Path.Combine(_testRepoPath, "subproject"));
        File.WriteAllText(Path.Combine(_testRepoPath, ".gitignore"), "*.log");
        File.WriteAllText(Path.Combine(_testRepoPath, "subproject", ".gitignore"), "*.tmp\n!important.tmp");
        
        File.WriteAllText(Path.Combine(_testRepoPath, "subproject", "test.tmp"), "ignored");
        File.WriteAllText(Path.Combine(_testRepoPath, "subproject", "important.tmp"), "not ignored");
        
        var options = new IndexBuildOptions { RootPath = _testRepoPath };

        // Act
        var result = await _indexService.BuildAsync(options, CancellationToken.None);

        // Assert
        Assert.DoesNotContain(result.IndexedFiles, f => f.EndsWith("test.tmp"));
        Assert.Contains(result.IndexedFiles, f => f.EndsWith("important.tmp"));
    }

    [Fact]
    public async Task Should_Handle_Symlinks()
    {
        // Arrange - only run on systems that support symlinks
        if (!OperatingSystem.IsWindows() || IsRunningAsAdmin())
        {
            CreateTestFiles(10);
            var targetDir = Path.Combine(_testRepoPath, "target");
            var linkDir = Path.Combine(_testRepoPath, "link");
            Directory.CreateDirectory(targetDir);
            File.WriteAllText(Path.Combine(targetDir, "real.cs"), "public class Real { }");
            
            try
            {
                Directory.CreateSymbolicLink(linkDir, targetDir);
            }
            catch (IOException)
            {
                // Symlinks not supported, skip test
                return;
            }
            
            var options = new IndexBuildOptions { RootPath = _testRepoPath, FollowSymlinks = false };

            // Act
            var result = await _indexService.BuildAsync(options, CancellationToken.None);

            // Assert
            Assert.Contains(result.IndexedFiles, f => f.Contains("target") && f.EndsWith("real.cs"));
            Assert.DoesNotContain(result.IndexedFiles, f => f.Contains("link"));
        }
    }

    private void CreateTestFiles(int count)
    {
        var srcDir = Path.Combine(_testRepoPath, "src");
        Directory.CreateDirectory(srcDir);
        
        for (int i = 0; i < count; i++)
        {
            File.WriteAllText(
                Path.Combine(srcDir, $"File{i}.cs"),
                $"// File {i}\npublic class Class{i} {{\n    public void Method{i}() {{ }}\n}}");
        }
    }

    private static bool IsRunningAsAdmin()
    {
        if (OperatingSystem.IsWindows())
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        return false;
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        if (Directory.Exists(_testRepoPath))
        {
            Directory.Delete(_testRepoPath, recursive: true);
        }
    }
}
```

#### SearchIntegrationTests.cs

```csharp
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Index;
using Acode.Infrastructure.Index;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Acode.Tests.Integration.Index;

public sealed class SearchIntegrationTests : IAsyncLifetime
{
    private readonly string _testRepoPath;
    private readonly ServiceProvider _serviceProvider;
    private readonly IIndexService _indexService;

    public SearchIntegrationTests()
    {
        _testRepoPath = Path.Combine(Path.GetTempPath(), $"search_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRepoPath);
        
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IIndexService, IndexService>();
        services.AddSingleton<IIndexBuilder, IndexBuilder>();
        services.AddSingleton<ISearchEngine, SearchEngine>();
        services.AddSingleton<IIgnoreRuleParser, IgnoreRuleParser>();
        services.AddSingleton<ITokenizer, CodeTokenizer>();
        
        _serviceProvider = services.BuildServiceProvider();
        _indexService = _serviceProvider.GetRequiredService<IIndexService>();
    }

    public async Task InitializeAsync()
    {
        // Create test files
        var srcDir = Path.Combine(_testRepoPath, "src");
        Directory.CreateDirectory(srcDir);
        
        File.WriteAllText(Path.Combine(srcDir, "UserService.cs"), @"
using System;
namespace App.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        
        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }
        
        public async Task<User> GetUserByIdAsync(int userId)
        {
            return await _repository.FindByIdAsync(userId);
        }
        
        public async Task<User> CreateUserAsync(CreateUserRequest request)
        {
            var user = new User(request.Name, request.Email);
            return await _repository.SaveAsync(user);
        }
    }
}");
        
        File.WriteAllText(Path.Combine(srcDir, "OrderService.cs"), @"
using System;
namespace App.Services
{
    public class OrderService
    {
        public async Task<Order> ProcessOrderAsync(OrderRequest request)
        {
            // Process the order
            return new Order();
        }
    }
}");
        
        // Build index
        var options = new IndexBuildOptions { RootPath = _testRepoPath };
        await _indexService.BuildAsync(options, CancellationToken.None);
    }

    [Fact]
    public async Task Should_Search_Real_Codebase()
    {
        // Arrange
        var query = new SearchQuery("UserService");

        // Act
        var results = await _indexService.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.FilePath.EndsWith("UserService.cs"));
    }

    [Fact]
    public async Task Should_Return_Correct_Line_Numbers()
    {
        // Arrange
        var query = new SearchQuery("GetUserByIdAsync");

        // Act
        var results = await _indexService.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.Single(results);
        var result = results[0];
        Assert.Contains(14, result.MatchedLines); // Line 14 in UserService.cs
    }

    [Fact]
    public async Task Should_Handle_Concurrent_Searches()
    {
        // Arrange
        var queries = new[]
        {
            new SearchQuery("UserService"),
            new SearchQuery("OrderService"),
            new SearchQuery("repository"),
            new SearchQuery("async"),
            new SearchQuery("CreateUserAsync")
        };

        // Act
        var tasks = queries.Select(q => _indexService.SearchAsync(q, CancellationToken.None));
        var allResults = await Task.WhenAll(tasks);

        // Assert
        Assert.All(allResults, results => Assert.NotNull(results));
        Assert.Contains(allResults[0], r => r.FilePath.Contains("UserService"));
        Assert.Contains(allResults[1], r => r.FilePath.Contains("OrderService"));
    }

    [Fact]
    public async Task Should_Search_During_Update()
    {
        // Arrange
        var searchQuery = new SearchQuery("UserService");
        
        // Add a new file
        var newFilePath = Path.Combine(_testRepoPath, "src", "NewService.cs");
        File.WriteAllText(newFilePath, "public class NewService { }");

        // Act - search while update is happening
        var updateTask = _indexService.UpdateAsync(CancellationToken.None);
        var searchTask = _indexService.SearchAsync(searchQuery, CancellationToken.None);
        
        await Task.WhenAll(updateTask, searchTask);
        var searchResults = searchTask.Result;

        // Assert
        Assert.NotEmpty(searchResults); // Search should still return results during update
    }

    public async Task DisposeAsync()
    {
        _serviceProvider.Dispose();
        if (Directory.Exists(_testRepoPath))
        {
            await Task.Delay(100); // Allow file handles to be released
            Directory.Delete(_testRepoPath, recursive: true);
        }
    }
}
```

#### PersistenceIntegrationTests.cs

```csharp
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Index;
using Acode.Infrastructure.Index;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Acode.Tests.Integration.Index;

public sealed class PersistenceIntegrationTests : IDisposable
{
    private readonly string _testRepoPath;
    private readonly string _indexPath;

    public PersistenceIntegrationTests()
    {
        _testRepoPath = Path.Combine(Path.GetTempPath(), $"persist_test_{Guid.NewGuid()}");
        _indexPath = Path.Combine(_testRepoPath, ".agent", "index.db");
        Directory.CreateDirectory(_testRepoPath);
    }

    [Fact]
    public async Task Should_Survive_Restart()
    {
        // Arrange - Build index with first service instance
        CreateTestFiles(50);
        
        using (var sp1 = CreateServiceProvider())
        {
            var indexService1 = sp1.GetRequiredService<IIndexService>();
            var options = new IndexBuildOptions { RootPath = _testRepoPath, IndexPath = _indexPath };
            await indexService1.BuildAsync(options, CancellationToken.None);
        }

        // Act - Create new service instance (simulating restart)
        using var sp2 = CreateServiceProvider();
        var indexService2 = sp2.GetRequiredService<IIndexService>();
        await indexService2.LoadAsync(_indexPath, CancellationToken.None);
        
        var query = new SearchQuery("Class25");
        var results = await indexService2.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.FilePath.Contains("File25"));
    }

    [Fact]
    public async Task Should_Recover_From_Corruption()
    {
        // Arrange
        CreateTestFiles(20);
        
        using var sp = CreateServiceProvider();
        var indexService = sp.GetRequiredService<IIndexService>();
        var options = new IndexBuildOptions { RootPath = _testRepoPath, IndexPath = _indexPath };
        await indexService.BuildAsync(options, CancellationToken.None);
        
        // Corrupt the index file
        Directory.CreateDirectory(Path.GetDirectoryName(_indexPath)!);
        await File.WriteAllTextAsync(_indexPath, "CORRUPTED DATA - NOT VALID SQLITE");

        // Act
        var loadResult = await indexService.LoadAsync(_indexPath, CancellationToken.None);

        // Assert
        Assert.False(loadResult.Success);
        Assert.Equal("ACODE-IDX-004", loadResult.ErrorCode);
        
        // Should be able to rebuild
        var rebuildResult = await indexService.RebuildAsync(options, CancellationToken.None);
        Assert.True(rebuildResult.Success);
        Assert.Equal(20, rebuildResult.FilesIndexed);
    }

    [Fact]
    public async Task Should_Handle_Disk_Full_Gracefully()
    {
        // This test simulates disk full by using a very small memory-mapped file
        // In real scenarios, we catch IOException and return appropriate error
        
        // Arrange
        CreateTestFiles(10);
        
        using var sp = CreateServiceProvider();
        var indexService = sp.GetRequiredService<IIndexService>();
        
        // Create a read-only directory to simulate disk full
        var readOnlyIndexPath = Path.Combine(_testRepoPath, "readonly", "index.db");
        Directory.CreateDirectory(Path.GetDirectoryName(readOnlyIndexPath)!);
        
        if (OperatingSystem.IsWindows())
        {
            var dirInfo = new DirectoryInfo(Path.GetDirectoryName(readOnlyIndexPath)!);
            dirInfo.Attributes = FileAttributes.ReadOnly;
        }
        
        var options = new IndexBuildOptions { RootPath = _testRepoPath, IndexPath = readOnlyIndexPath };

        // Act
        var result = await indexService.BuildAsync(options, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("disk", result.ErrorMessage.ToLower() + result.ErrorCode.ToLower());
    }

    private void CreateTestFiles(int count)
    {
        var srcDir = Path.Combine(_testRepoPath, "src");
        Directory.CreateDirectory(srcDir);
        
        for (int i = 0; i < count; i++)
        {
            File.WriteAllText(
                Path.Combine(srcDir, $"File{i}.cs"),
                $"public class Class{i} {{ public void Method{i}() {{ }} }}");
        }
    }

    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IIndexService, IndexService>();
        services.AddSingleton<IIndexBuilder, IndexBuilder>();
        services.AddSingleton<ISearchEngine, SearchEngine>();
        services.AddSingleton<IIgnoreRuleParser, IgnoreRuleParser>();
        services.AddSingleton<ITokenizer, CodeTokenizer>();
        return services.BuildServiceProvider();
    }

    public void Dispose()
    {
        // Reset read-only attribute if set
        if (Directory.Exists(Path.Combine(_testRepoPath, "readonly")))
        {
            var dirInfo = new DirectoryInfo(Path.Combine(_testRepoPath, "readonly"));
            dirInfo.Attributes = FileAttributes.Normal;
        }
        
        if (Directory.Exists(_testRepoPath))
        {
            Directory.Delete(_testRepoPath, recursive: true);
        }
    }
}
```

### E2E Tests

#### IndexE2ETests.cs

```csharp
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Acode.Tests.E2E.Index;

public sealed class IndexE2ETests : IDisposable
{
    private readonly string _testRepoPath;
    private readonly string _acodePath;

    public IndexE2ETests()
    {
        _testRepoPath = Path.Combine(Path.GetTempPath(), $"e2e_index_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRepoPath);
        CreateTestRepository();
        
        // Locate acode CLI
        _acodePath = Path.Combine(AppContext.BaseDirectory, "acode");
        if (OperatingSystem.IsWindows())
        {
            _acodePath += ".exe";
        }
    }

    [Fact]
    public async Task Should_Build_Index_Via_CLI()
    {
        // Act
        var result = await RunAcodeAsync("index", "build");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Building index", result.StdOut);
        Assert.Contains("Index built", result.StdOut);
        Assert.True(File.Exists(Path.Combine(_testRepoPath, ".agent", "index.db")));
    }

    [Fact]
    public async Task Should_Search_Via_CLI()
    {
        // Arrange
        await RunAcodeAsync("index", "build");

        // Act
        var result = await RunAcodeAsync("search", "UserService");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("UserService.cs", result.StdOut);
        Assert.Contains("score:", result.StdOut.ToLower());
    }

    [Fact]
    public async Task Should_Search_With_JSON_Output()
    {
        // Arrange
        await RunAcodeAsync("index", "build");

        // Act
        var result = await RunAcodeAsync("search", "--json", "UserService");

        // Assert
        Assert.Equal(0, result.ExitCode);
        
        var lines = result.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.NotEmpty(lines);
        
        foreach (var line in lines)
        {
            var json = JsonDocument.Parse(line);
            Assert.True(json.RootElement.TryGetProperty("filePath", out _));
            Assert.True(json.RootElement.TryGetProperty("score", out _));
        }
    }

    [Fact]
    public async Task Should_Update_Index_Via_CLI()
    {
        // Arrange
        await RunAcodeAsync("index", "build");
        
        // Add a new file
        File.WriteAllText(
            Path.Combine(_testRepoPath, "src", "NewService.cs"),
            "public class NewService { }");

        // Act
        var result = await RunAcodeAsync("index", "update");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Updating index", result.StdOut);
        Assert.Contains("New: 1", result.StdOut);
    }

    [Fact]
    public async Task Should_Rebuild_Index_Via_CLI()
    {
        // Arrange
        await RunAcodeAsync("index", "build");

        // Act
        var result = await RunAcodeAsync("index", "rebuild");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Rebuilding index", result.StdOut);
        Assert.Contains("Index rebuilt", result.StdOut);
    }

    [Fact]
    public async Task Should_Show_Index_Status_Via_CLI()
    {
        // Arrange
        await RunAcodeAsync("index", "build");

        // Act
        var result = await RunAcodeAsync("index", "status");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Files indexed:", result.StdOut);
        Assert.Contains("Index size:", result.StdOut);
        Assert.Contains("Last updated:", result.StdOut);
    }

    [Fact]
    public async Task Should_Show_Help_For_Commands()
    {
        // Act
        var indexHelp = await RunAcodeAsync("index", "--help");
        var searchHelp = await RunAcodeAsync("search", "--help");

        // Assert
        Assert.Equal(0, indexHelp.ExitCode);
        Assert.Contains("build", indexHelp.StdOut);
        Assert.Contains("update", indexHelp.StdOut);
        Assert.Contains("rebuild", indexHelp.StdOut);
        Assert.Contains("status", indexHelp.StdOut);
        
        Assert.Equal(0, searchHelp.ExitCode);
        Assert.Contains("--ext", searchHelp.StdOut);
        Assert.Contains("--dir", searchHelp.StdOut);
    }

    private void CreateTestRepository()
    {
        var srcDir = Path.Combine(_testRepoPath, "src");
        Directory.CreateDirectory(srcDir);
        
        File.WriteAllText(Path.Combine(srcDir, "UserService.cs"), @"
public class UserService : IUserService
{
    public async Task<User> GetUserAsync(int id)
    {
        return await _repository.FindAsync(id);
    }
}");
        
        File.WriteAllText(Path.Combine(srcDir, "OrderService.cs"), @"
public class OrderService
{
    public async Task<Order> ProcessOrderAsync(OrderRequest request)
    {
        return new Order();
    }
}");
        
        File.WriteAllText(Path.Combine(_testRepoPath, ".gitignore"), "bin/\nobj/\n*.log");
    }

    private async Task<(int ExitCode, string StdOut, string StdErr)> RunAcodeAsync(params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _acodePath,
            WorkingDirectory = _testRepoPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }
        
        using var process = Process.Start(psi)!;
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        return (process.ExitCode, stdout, stderr);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRepoPath))
        {
            Directory.Delete(_testRepoPath, recursive: true);
        }
    }
}
```

### Performance Benchmarks

```csharp
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Index;
using Acode.Infrastructure.Index;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Acode.Tests.Benchmarks.Index;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 5)]
public class IndexBenchmarks
{
    private string _testRepoPath = null!;
    private ServiceProvider _serviceProvider = null!;
    private IIndexService _indexService = null!;

    [Params(1000, 10000)]
    public int FileCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _testRepoPath = Path.Combine(Path.GetTempPath(), $"bench_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRepoPath);
        
        var srcDir = Path.Combine(_testRepoPath, "src");
        Directory.CreateDirectory(srcDir);
        
        for (int i = 0; i < FileCount; i++)
        {
            var content = $@"
namespace App.Generated
{{
    public class GeneratedClass{i}
    {{
        public void Method{i}() {{ }}
        public string Property{i} {{ get; set; }}
    }}
}}";
            File.WriteAllText(Path.Combine(srcDir, $"File{i}.cs"), content);
        }
        
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton<IIndexService, IndexService>();
        services.AddSingleton<IIndexBuilder, IndexBuilder>();
        services.AddSingleton<ISearchEngine, SearchEngine>();
        services.AddSingleton<IIgnoreRuleParser, IgnoreRuleParser>();
        services.AddSingleton<ITokenizer, CodeTokenizer>();
        
        _serviceProvider = services.BuildServiceProvider();
        _indexService = _serviceProvider.GetRequiredService<IIndexService>();
    }

    [Benchmark(Description = "Index Build")]
    public async Task<IndexBuildResult> BuildIndex()
    {
        var options = new IndexBuildOptions { RootPath = _testRepoPath };
        return await _indexService.BuildAsync(options, CancellationToken.None);
    }

    [Benchmark(Description = "Simple Search")]
    public async Task<IReadOnlyList<SearchResult>> SimpleSearch()
    {
        var query = new SearchQuery("GeneratedClass500");
        return await _indexService.SearchAsync(query, CancellationToken.None);
    }

    [Benchmark(Description = "Complex Search")]
    public async Task<IReadOnlyList<SearchResult>> ComplexSearch()
    {
        var query = new SearchQuery("Generated* Method -test --ext cs");
        return await _indexService.SearchAsync(query, CancellationToken.None);
    }

    [Benchmark(Description = "Incremental Update (10 files)")]
    public async Task<UpdateResult> IncrementalUpdate()
    {
        // Modify 10 files
        for (int i = 0; i < 10; i++)
        {
            var path = Path.Combine(_testRepoPath, "src", $"File{i}.cs");
            File.AppendAllText(path, $"\n// Updated at {DateTime.UtcNow}");
        }
        
        return await _indexService.UpdateAsync(CancellationToken.None);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
        if (Directory.Exists(_testRepoPath))
        {
            Directory.Delete(_testRepoPath, recursive: true);
        }
    }
}

// Expected benchmark targets:
// | Benchmark | FileCount | Target | Maximum |
// |-----------|-----------|--------|---------|
// | Index Build | 1,000 | 3s | 5s |
// | Index Build | 10,000 | 20s | 30s |
// | Simple Search | any | 50ms | 100ms |
// | Complex Search | any | 80ms | 150ms |
// | Incremental Update | 10 files | 2s | 5s |
```

---

## User Verification Steps

### Scenario 1: Initial Index Build

**Objective:** Verify that the index builds successfully and reports accurate statistics.

**Prerequisites:**
- Repository with at least 100 source files
- No existing index file

**Steps:**

```bash
# Step 1: Navigate to repository root
cd /path/to/your/repository

# Step 2: Verify no index exists
ls -la .agent/
# Expected: Directory does not exist or no index.db file

# Step 3: Build the index
acode index build

# Expected output:
# Building index...
#   Scanning files...
#   Found: 1,234 files
#   Ignored: 456 files (gitignore)
#   Indexing: 1,234 files
#     [====================] 100%
# 
# Index built:
#   Files: 1,234
#   Size: 2.3 MB
#   Time: 8.5s

# Step 4: Verify index file created
ls -la .agent/index.db
# Expected: File exists with size > 0

# Step 5: Verify checksum file created
cat .agent/index.checksum
# Expected: 64-character hex string (SHA-256)

# Step 6: Check index status
acode index status
# Expected output:
# Index Status
# ────────────────────
# Files indexed: 1,234
# Index size: 2.3 MB
# Last updated: 2024-01-20 14:30:00
# Pending updates: 0 files
```

**Verification Criteria:**
- [ ] Exit code is 0
- [ ] Progress bar shows 100%
- [ ] File count matches visible source files
- [ ] Index file exists at `.agent/index.db`
- [ ] Checksum file exists at `.agent/index.checksum`
- [ ] Build completes in < 30 seconds for 1,000 files

---

### Scenario 2: Basic Search Functionality

**Objective:** Verify that search returns accurate results with correct line numbers and snippets.

**Prerequisites:**
- Index built (Scenario 1 complete)
- Known file with known content (e.g., `UserService.cs` containing `GetUserByIdAsync`)

**Steps:**

```bash
# Step 1: Search for a specific method name
acode search "GetUserByIdAsync"

# Expected output:
# Found 3 results:
# 
# 1. [src/Services/UserService.cs] (score: 0.95)
#    Line 45: public async Task<User> GetUserByIdAsync(int userId)
#    Line 46: {
#    Line 47:     return await _repository.FindByIdAsync(userId);
# 
# 2. [tests/UserServiceTests.cs] (score: 0.72)
#    Line 23: var user = await _sut.GetUserByIdAsync(testUserId);

# Step 2: Verify line numbers are correct
# Open the file and check line 45 contains the method

# Step 3: Search with multiple terms (AND)
acode search "user create async"

# Expected: Only files containing ALL three terms

# Step 4: Search with OR operator
acode search "user OR customer"

# Expected: Files containing either "user" OR "customer"

# Step 5: Search with exclusion
acode search "service -test"

# Expected: Service files but NOT test files

# Step 6: Search with phrase
acode search '"public class"'

# Expected: Only exact phrase matches
```

**Verification Criteria:**
- [ ] Results contain expected files
- [ ] Line numbers are accurate (verified by opening file)
- [ ] Snippets show surrounding context
- [ ] Relevance scores are between 0 and 1
- [ ] Results are sorted by relevance (highest first)
- [ ] Search completes in < 100ms

---

### Scenario 3: Search with Filters

**Objective:** Verify that search filters correctly limit results by extension, directory, and size.

**Prerequisites:**
- Index built with files of different types (.cs, .ts, .js, .md)
- Files in different directories

**Steps:**

```bash
# Step 1: Filter by extension
acode search "controller" --ext cs

# Expected: Only .cs files in results
# Verify: No .ts, .js, or other files appear

# Step 2: Filter by multiple extensions
acode search "service" --ext cs,ts

# Expected: Only .cs and .ts files

# Step 3: Filter by directory
acode search "handler" --dir src/Controllers

# Expected: Only files under src/Controllers/

# Step 4: Filter by directory recursively (default)
acode search "model" --dir src

# Expected: Files in src/ and all subdirectories

# Step 5: Combine filters
acode search "async" --ext cs --dir src/Services

# Expected: Only .cs files under src/Services/

# Step 6: Verify filter efficiency
time acode search "common_term" --ext cs
time acode search "common_term"

# Expected: Filtered search is faster or equal
```

**Verification Criteria:**
- [ ] Extension filter excludes non-matching files
- [ ] Directory filter scopes to correct path
- [ ] Multiple extensions work with comma separation
- [ ] Combined filters apply correctly (AND logic)
- [ ] Filtered search is not slower than unfiltered

---

### Scenario 4: Ignore Rules Verification

**Objective:** Verify that .gitignore and .agentignore patterns are correctly applied.

**Prerequisites:**
- Repository with .gitignore file
- node_modules directory with JavaScript files
- Build output directories (bin/, obj/)

**Steps:**

```bash
# Step 1: Check current gitignore
cat .gitignore
# Expected: Contains node_modules/, bin/, obj/, *.log, etc.

# Step 2: Build index
acode index build

# Step 3: Verify ignored directories are excluded
acode search "package" --dir node_modules

# Expected: No results (node_modules is ignored)

# Step 4: Create .agentignore with custom patterns
echo "*.generated.cs" > .agentignore
echo "!important.generated.cs" >> .agentignore

# Step 5: Rebuild index
acode index rebuild

# Step 6: Verify .agentignore precedence
acode search "generated"

# Expected: 
# - *.generated.cs files are excluded
# - important.generated.cs is included (negation)

# Step 7: Test ignore check command
acode ignore check node_modules/lodash/index.js

# Expected output:
# File: node_modules/lodash/index.js
# Status: IGNORED
# Pattern: node_modules/
# Source: .gitignore:5
```

**Verification Criteria:**
- [ ] .gitignore patterns are respected
- [ ] .agentignore patterns override .gitignore
- [ ] Negation patterns work correctly
- [ ] Nested .gitignore files in subdirectories work
- [ ] `acode ignore check` reports correct status

---

### Scenario 5: Incremental Update Detection

**Objective:** Verify that incremental updates correctly detect and index only changed files.

**Prerequisites:**
- Index built
- Ability to modify files

**Steps:**

```bash
# Step 1: Check current index status
acode index status
# Note the "Last updated" timestamp

# Step 2: Modify an existing file
echo "// Modified" >> src/UserService.cs

# Step 3: Create a new file
echo "public class NewService { }" > src/NewService.cs

# Step 4: Delete a file (move to backup)
mv src/OldService.cs src/OldService.cs.bak

# Step 5: Run incremental update
acode index update

# Expected output:
# Updating index...
#   Changed: 1 files
#   New: 1 files
#   Deleted: 1 files
# 
# Index updated.

# Step 6: Verify new file is searchable
acode search "NewService"
# Expected: src/NewService.cs appears in results

# Step 7: Verify deleted file is not searchable
acode search "OldService"
# Expected: src/OldService.cs does NOT appear

# Step 8: Verify update was incremental (fast)
# Update should complete in < 2 seconds for small changes
```

**Verification Criteria:**
- [ ] Modified files are detected
- [ ] New files are added to index
- [ ] Deleted files are removed from index
- [ ] Update is faster than full rebuild
- [ ] Unchanged files are not re-indexed

---

### Scenario 6: Search Performance Validation

**Objective:** Verify that search meets performance requirements.

**Prerequisites:**
- Large index (10,000+ files recommended)
- Timer utility available

**Steps:**

```bash
# Step 1: Build index for large repository
acode index build
# Note: Should complete in < 30 seconds for 10K files

# Step 2: Time simple search
time acode search "function"
# Expected: < 100ms total (including output)

# Step 3: Time complex search
time acode search '"public async" User* -test --ext cs'
# Expected: < 150ms total

# Step 4: Time wildcard search
time acode search "Get*Async"
# Expected: < 200ms total

# Step 5: Run multiple searches in sequence
for i in {1..10}; do
  time acode search "term$i" > /dev/null
done
# Expected: Each search < 100ms

# Step 6: Test search with pagination
acode search "the" --skip 0 --take 10
acode search "the" --skip 10 --take 10
acode search "the" --skip 20 --take 10
# Expected: Different results on each page, consistent total count
```

**Verification Criteria:**
- [ ] Simple search completes in < 50ms
- [ ] Complex search completes in < 100ms
- [ ] Wildcard search completes in < 200ms
- [ ] Pagination returns consistent results
- [ ] Total count is accurate across pages

---

### Scenario 7: Index Persistence and Recovery

**Objective:** Verify that the index persists across restarts and recovers from corruption.

**Prerequisites:**
- Built index

**Steps:**

```bash
# Step 1: Build index and note stats
acode index build
acode index status
# Note file count

# Step 2: Simulate application restart
# (Close and reopen terminal, or wait)

# Step 3: Verify index loads on startup
acode search "test"
# Expected: Works immediately without rebuild

# Step 4: Verify stats are preserved
acode index status
# Expected: Same file count as before

# Step 5: Corrupt the index intentionally
echo "CORRUPTED" > .agent/index.db

# Step 6: Attempt to use corrupted index
acode search "test"

# Expected output:
# Error: Index file corrupted (ACODE-IDX-004)
# Checksum mismatch detected.
# Run 'acode index rebuild' to fix.

# Step 7: Rebuild after corruption
acode index rebuild
# Expected: Rebuilds successfully

# Step 8: Verify recovery
acode search "test"
# Expected: Works normally
```

**Verification Criteria:**
- [ ] Index persists across restarts
- [ ] Corruption is detected (checksum mismatch)
- [ ] Clear error message with recovery instructions
- [ ] Rebuild recovers from corruption
- [ ] No data loss after recovery

---

### Scenario 8: Concurrent Access Safety

**Objective:** Verify that concurrent searches and updates are handled safely.

**Prerequisites:**
- Built index
- Ability to run parallel commands

**Steps:**

```bash
# Step 1: Run multiple concurrent searches
for i in {1..5}; do
  acode search "term$i" &
done
wait
# Expected: All searches complete without error

# Step 2: Search while update is running
acode index update &
UPDATE_PID=$!
acode search "controller"
wait $UPDATE_PID
# Expected: Search returns results, update completes

# Step 3: Attempt concurrent updates (should be blocked)
acode index update &
acode index update
# Expected: Second update waits or reports lock held

# Step 4: Verify no corruption after concurrent operations
acode index status
# Expected: Stats are consistent
```

**Verification Criteria:**
- [ ] Concurrent searches work correctly
- [ ] Search during update returns consistent results
- [ ] Concurrent updates are serialized (locked)
- [ ] No corruption after concurrent operations

---

### Scenario 9: JSON Output Mode

**Objective:** Verify that JSON output mode produces parseable, structured output.

**Prerequisites:**
- Built index
- jq installed (for JSON parsing)

**Steps:**

```bash
# Step 1: Search with JSON output
acode search --json "UserService"

# Expected output (one JSON object per line):
# {"filePath":"src/Services/UserService.cs","score":0.95,"matchedLines":[1,5,10],...}
# {"filePath":"tests/UserServiceTests.cs","score":0.72,"matchedLines":[23],...}

# Step 2: Parse with jq
acode search --json "controller" | jq -r '.filePath'
# Expected: List of file paths, one per line

# Step 3: Filter by score
acode search --json "service" | jq 'select(.score > 0.8)'
# Expected: Only high-relevance results

# Step 4: Count results
acode search --json "async" | wc -l
# Expected: Number of matching files

# Step 5: Get specific fields
acode search --json "handler" | jq '{path: .filePath, lines: .matchedLines}'
# Expected: Simplified JSON with only requested fields

# Step 6: Index status in JSON
acode index status --json | jq '.'
# Expected: {"filesIndexed":1234,"indexSize":2400000,"lastUpdated":"..."}
```

**Verification Criteria:**
- [ ] Each line is valid JSON
- [ ] JSON contains filePath, score, matchedLines
- [ ] Output is parseable by jq
- [ ] JSON mode works for all commands

---

### Scenario 10: Error Handling and Edge Cases

**Objective:** Verify that errors are handled gracefully with clear messages.

**Prerequisites:**
- Access to repository

**Steps:**

```bash
# Step 1: Search with invalid query
acode search '"unclosed quote'
# Expected: Error with ACODE-IDX-002, clear message about unbalanced quotes
# Exit code: Non-zero

# Step 2: Build index in non-existent directory
acode index build --path /nonexistent/path
# Expected: Error message, exit code non-zero

# Step 3: Search before index exists
rm -rf .agent/
acode search "test"
# Expected: Error message suggesting 'acode index build'
# Error code: ACODE-IDX-001 or similar

# Step 4: Handle permission denied
chmod 000 .agent/index.db
acode search "test"
# Expected: Error about access denied
# Restore: chmod 644 .agent/index.db

# Step 5: Handle very long query
acode search "$(printf 'a%.0s' {1..10000})"
# Expected: Error about query too long, or truncation warning

# Step 6: Search with no matches
acode search "xyznonexistentterm12345"
# Expected: "No results found" (not an error)
# Exit code: 0

# Step 7: Verify help is available
acode index --help
acode search --help
# Expected: Usage information displayed
```

**Verification Criteria:**
- [ ] Invalid queries return clear error messages
- [ ] Error codes are included (ACODE-IDX-XXX)
- [ ] Non-zero exit codes on errors
- [ ] Zero exit code for "no results" (not an error)
- [ ] Help text is available for all commands

---

## Implementation Prompt

### File Structure

```
src/Acode.Domain/
├── Index/
│   ├── IIndexService.cs
│   ├── ISearchEngine.cs
│   ├── IIgnoreRuleParser.cs
│   ├── ITokenizer.cs
│   ├── SearchQuery.cs
│   ├── SearchResult.cs
│   ├── SearchFilter.cs
│   ├── IndexStats.cs
│   ├── IndexBuildResult.cs
│   └── Exceptions/
│       ├── IndexException.cs
│       ├── SearchQueryException.cs
│       └── IndexCorruptedException.cs
│
src/Acode.Infrastructure/
├── Index/
│   ├── IndexService.cs
│   ├── IndexBuilder.cs
│   ├── SearchEngine.cs
│   ├── SearchQueryParser.cs
│   ├── IgnoreRuleParser.cs
│   ├── IgnoreMatcher.cs
│   ├── IncrementalUpdater.cs
│   ├── CodeTokenizer.cs
│   ├── IndexStore.cs
│   ├── IndexIntegrityVerifier.cs
│   └── BM25Ranker.cs
│
src/Acode.Cli/
└── Commands/
    ├── IndexCommand.cs
    ├── SearchCommand.cs
    └── IgnoreCommand.cs
```

---

### Domain Models

#### SearchQuery.cs

```csharp
using System;
using System.Collections.Generic;

namespace Acode.Domain.Index;

/// <summary>
/// Represents a search query with terms, filters, and pagination options.
/// </summary>
public sealed class SearchQuery
{
    /// <summary>
    /// The raw query text entered by the user.
    /// </summary>
    public string Text { get; }
    
    /// <summary>
    /// Parsed required terms (AND logic).
    /// </summary>
    public IReadOnlyList<string> RequiredTerms { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// Parsed optional terms (OR logic).
    /// </summary>
    public IReadOnlyList<string> OptionalTerms { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// Terms to exclude from results.
    /// </summary>
    public IReadOnlyList<string> ExcludedTerms { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// Exact phrases to match (quoted strings).
    /// </summary>
    public IReadOnlyList<string[]> Phrases { get; init; } = Array.Empty<string[]>();
    
    /// <summary>
    /// Wildcard patterns (prefix*, *suffix, *contains*).
    /// </summary>
    public IReadOnlyList<WildcardTerm> Wildcards { get; init; } = Array.Empty<WildcardTerm>();
    
    /// <summary>
    /// Filter to apply to search results.
    /// </summary>
    public SearchFilter? Filter { get; init; }
    
    /// <summary>
    /// Number of results to skip (pagination).
    /// </summary>
    public int Skip { get; init; } = 0;
    
    /// <summary>
    /// Maximum number of results to return.
    /// </summary>
    public int Take { get; init; } = 50;
    
    /// <summary>
    /// Whether search is case-sensitive.
    /// </summary>
    public bool CaseSensitive { get; init; } = false;
    
    public SearchQuery(string text)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }
}

public sealed record WildcardTerm(string Value, WildcardType Type);

public enum WildcardType
{
    Prefix,   // *suffix
    Suffix,   // prefix*
    Both      // *contains*
}
```

#### SearchResult.cs

```csharp
using System;
using System.Collections.Generic;

namespace Acode.Domain.Index;

/// <summary>
/// Represents a search result with matched file, lines, snippets, and relevance score.
/// </summary>
public sealed class SearchResult
{
    /// <summary>
    /// Path to the matched file (relative to repository root).
    /// </summary>
    public string FilePath { get; init; } = string.Empty;
    
    /// <summary>
    /// Line numbers where matches were found (1-based).
    /// </summary>
    public IReadOnlyList<int> MatchedLines { get; init; } = Array.Empty<int>();
    
    /// <summary>
    /// Context snippets showing matched content with surrounding lines.
    /// </summary>
    public IReadOnlyList<Snippet> Snippets { get; init; } = Array.Empty<Snippet>();
    
    /// <summary>
    /// Relevance score (0.0 to 1.0) based on BM25 ranking.
    /// </summary>
    public double Score { get; init; }
    
    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSize { get; init; }
    
    /// <summary>
    /// Last modification time of the file.
    /// </summary>
    public DateTime LastModified { get; init; }
}

/// <summary>
/// A code snippet showing matched content with context.
/// </summary>
public sealed class Snippet
{
    /// <summary>
    /// The text content of the snippet.
    /// </summary>
    public string Text { get; init; } = string.Empty;
    
    /// <summary>
    /// Starting line number of the snippet (1-based).
    /// </summary>
    public int StartLine { get; init; }
    
    /// <summary>
    /// Ending line number of the snippet (1-based).
    /// </summary>
    public int EndLine { get; init; }
    
    /// <summary>
    /// Highlighted ranges within the text (character offsets).
    /// </summary>
    public IReadOnlyList<HighlightRange> Highlights { get; init; } = Array.Empty<HighlightRange>();
}

public sealed record HighlightRange(int Start, int Length);
```

#### SearchFilter.cs

```csharp
using System;
using System.Collections.Generic;

namespace Acode.Domain.Index;

/// <summary>
/// Filter options to narrow search results.
/// </summary>
public sealed class SearchFilter
{
    /// <summary>
    /// File extensions to include (e.g., ".cs", ".ts").
    /// </summary>
    public IReadOnlyList<string>? Extensions { get; init; }
    
    /// <summary>
    /// Directory to search within (relative path).
    /// </summary>
    public string? Directory { get; init; }
    
    /// <summary>
    /// Whether to search recursively in subdirectories.
    /// </summary>
    public bool Recursive { get; init; } = true;
    
    /// <summary>
    /// Minimum file size in bytes.
    /// </summary>
    public long? MinSizeBytes { get; init; }
    
    /// <summary>
    /// Maximum file size in bytes.
    /// </summary>
    public long? MaxSizeBytes { get; init; }
    
    /// <summary>
    /// Only include files modified since this date.
    /// </summary>
    public DateTime? ModifiedSince { get; init; }
    
    /// <summary>
    /// Only include files modified before this date.
    /// </summary>
    public DateTime? ModifiedBefore { get; init; }
    
    /// <summary>
    /// Glob patterns to exclude from results.
    /// </summary>
    public IReadOnlyList<string>? ExcludePatterns { get; init; }
}
```

#### IndexStats.cs

```csharp
using System;

namespace Acode.Domain.Index;

/// <summary>
/// Statistics about the current index state.
/// </summary>
public sealed class IndexStats
{
    /// <summary>
    /// Number of files in the index.
    /// </summary>
    public int FileCount { get; init; }
    
    /// <summary>
    /// Total number of unique terms in the index.
    /// </summary>
    public int TermCount { get; init; }
    
    /// <summary>
    /// Size of the index file in bytes.
    /// </summary>
    public long IndexSizeBytes { get; init; }
    
    /// <summary>
    /// Timestamp of last index update.
    /// </summary>
    public DateTime LastUpdated { get; init; }
    
    /// <summary>
    /// Number of files with pending changes (not yet indexed).
    /// </summary>
    public int PendingChanges { get; init; }
    
    /// <summary>
    /// Index format version.
    /// </summary>
    public int Version { get; init; }
    
    /// <summary>
    /// Path to the index file.
    /// </summary>
    public string IndexPath { get; init; } = string.Empty;
}
```

---

### Domain Interfaces

#### IIndexService.cs

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Acode.Domain.Index;

/// <summary>
/// Primary interface for index operations: build, update, search, and status.
/// </summary>
public interface IIndexService
{
    /// <summary>
    /// Builds the index from scratch, scanning all files in the repository.
    /// </summary>
    /// <param name="options">Build options including root path and configuration.</param>
    /// <param name="progress">Progress reporter for UI updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Build result with statistics.</returns>
    Task<IndexBuildResult> BuildAsync(
        IndexBuildOptions options,
        IProgress<IndexProgress>? progress = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs an incremental update, indexing only changed files.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Update result with change statistics.</returns>
    Task<IndexUpdateResult> UpdateAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rebuilds the index from scratch, clearing existing data.
    /// </summary>
    /// <param name="options">Build options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Build result with statistics.</returns>
    Task<IndexBuildResult> RebuildAsync(
        IndexBuildOptions options,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Searches the index for matching files.
    /// </summary>
    /// <param name="query">Search query with terms and filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of search results ranked by relevance.</returns>
    Task<SearchResultList> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets current index statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Index statistics.</returns>
    Task<IndexStats> GetStatsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Loads an existing index from disk.
    /// </summary>
    /// <param name="indexPath">Path to the index file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Load result indicating success or failure.</returns>
    Task<IndexLoadResult> LoadAsync(
        string indexPath,
        CancellationToken cancellationToken = default);
}

public sealed record IndexBuildOptions
{
    public string RootPath { get; init; } = ".";
    public string IndexPath { get; init; } = ".agent/index.db";
    public int MaxFileSizeKb { get; init; } = 500;
    public bool FollowSymlinks { get; init; } = false;
    public IReadOnlyList<string>? IncludeExtensions { get; init; }
    public IReadOnlyList<string>? AdditionalIgnorePatterns { get; init; }
}

public sealed record IndexProgress(int FilesProcessed, int TotalFiles, string CurrentFile);

public sealed record IndexBuildResult(
    bool Success,
    int FilesIndexed,
    int FilesSkipped,
    TimeSpan Duration,
    IReadOnlyList<string> IndexedFiles,
    IReadOnlyList<SkippedFile> SkippedFiles,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public sealed record IndexUpdateResult(
    bool Success,
    int FilesAdded,
    int FilesModified,
    int FilesDeleted,
    DateTime LastUpdated,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public sealed record IndexLoadResult(
    bool Success,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public sealed record SkippedFile(string Path, string Reason);
```

#### ITokenizer.cs

```csharp
using System.Collections.Generic;

namespace Acode.Domain.Index;

/// <summary>
/// Tokenizes source code content into searchable terms.
/// </summary>
public interface ITokenizer
{
    /// <summary>
    /// Tokenizes content into a list of normalized terms.
    /// </summary>
    /// <param name="content">Source code content.</param>
    /// <returns>List of tokens in lowercase.</returns>
    IReadOnlyList<string> Tokenize(string content);
    
    /// <summary>
    /// Tokenizes content with position tracking for line number mapping.
    /// </summary>
    /// <param name="content">Source code content.</param>
    /// <returns>List of tokens with line and column positions.</returns>
    IReadOnlyList<TokenPosition> TokenizeWithPositions(string content);
}

public sealed record TokenPosition(string Token, int Line, int Column);
```

---

### Infrastructure Implementations

#### IndexService.cs

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Index;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Index;

/// <summary>
/// Primary implementation of the index service.
/// </summary>
public sealed class IndexService : IIndexService
{
    private readonly IIndexBuilder _builder;
    private readonly ISearchEngine _searchEngine;
    private readonly IIncrementalUpdater _updater;
    private readonly IIndexStore _store;
    private readonly IIndexIntegrityVerifier _integrityVerifier;
    private readonly ILogger<IndexService> _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public IndexService(
        IIndexBuilder builder,
        ISearchEngine searchEngine,
        IIncrementalUpdater updater,
        IIndexStore store,
        IIndexIntegrityVerifier integrityVerifier,
        ILogger<IndexService> logger)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _searchEngine = searchEngine ?? throw new ArgumentNullException(nameof(searchEngine));
        _updater = updater ?? throw new ArgumentNullException(nameof(updater));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _integrityVerifier = integrityVerifier ?? throw new ArgumentNullException(nameof(integrityVerifier));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IndexBuildResult> BuildAsync(
        IndexBuildOptions options,
        IProgress<IndexProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Starting index build for {RootPath}", options.RootPath);
            var startTime = DateTime.UtcNow;

            var result = await _builder.BuildAsync(options, progress, cancellationToken);

            if (result.Success)
            {
                await _store.SaveAsync(options.IndexPath, cancellationToken);
                _integrityVerifier.UpdateChecksum(options.IndexPath);
                
                _logger.LogInformation(
                    "Index build completed: {FileCount} files in {Duration}",
                    result.FilesIndexed,
                    DateTime.UtcNow - startTime);
            }

            return result;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<IndexUpdateResult> UpdateAsync(CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Starting incremental update");
            return await _updater.UpdateAsync(cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<IndexBuildResult> RebuildAsync(
        IndexBuildOptions options,
        CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Rebuilding index from scratch");
            await _store.ClearAsync(cancellationToken);
            return await _builder.BuildAsync(options, null, cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<SearchResultList> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken = default)
    {
        // Searches do not require write lock (read-only)
        _logger.LogDebug("Searching for: {Query}", query.Text);
        return await _searchEngine.SearchAsync(query, cancellationToken);
    }

    public async Task<IndexStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        return await _store.GetStatsAsync(cancellationToken);
    }

    public async Task<IndexLoadResult> LoadAsync(
        string indexPath,
        CancellationToken cancellationToken = default)
    {
        var verifyResult = _integrityVerifier.VerifyIndex(indexPath);
        if (!verifyResult.IsValid)
        {
            _logger.LogError("Index verification failed: {Error}", verifyResult.ErrorMessage);
            return new IndexLoadResult(false, "ACODE-IDX-004", verifyResult.ErrorMessage);
        }

        try
        {
            await _store.LoadAsync(indexPath, cancellationToken);
            _logger.LogInformation("Index loaded successfully from {Path}", indexPath);
            return new IndexLoadResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load index from {Path}", indexPath);
            return new IndexLoadResult(false, "ACODE-IDX-004", ex.Message);
        }
    }
}
```

#### CodeTokenizer.cs

```csharp
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Acode.Domain.Index;

namespace Acode.Infrastructure.Index;

/// <summary>
/// Code-aware tokenizer that handles camelCase, snake_case, and code identifiers.
/// </summary>
public sealed class CodeTokenizer : ITokenizer
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "is", "are", "was", "were", "be", "been", "being",
        "have", "has", "had", "do", "does", "did", "will", "would", "could",
        "should", "may", "might", "must", "shall", "can", "need", "dare",
        "and", "or", "but", "if", "then", "else", "when", "at", "by", "for",
        "with", "about", "against", "between", "into", "through", "during",
        "before", "after", "above", "below", "to", "from", "up", "down",
        "in", "out", "on", "off", "over", "under", "again", "further",
        "once", "here", "there", "where", "why", "how", "all", "each",
        "few", "more", "most", "other", "some", "such", "no", "nor", "not",
        "only", "own", "same", "so", "than", "too", "very", "just"
    };

    private static readonly Regex CamelCasePattern = new(
        @"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])",
        RegexOptions.Compiled);

    private static readonly Regex WordPattern = new(
        @"[a-zA-Z]+|[0-9]+",
        RegexOptions.Compiled);

    public IReadOnlyList<string> Tokenize(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Array.Empty<string>();
        }

        var tokens = new List<string>();
        
        // Split on whitespace and punctuation first
        var words = WordPattern.Matches(content);
        
        foreach (Match match in words)
        {
            var word = match.Value;
            
            // Split camelCase and PascalCase
            var subWords = CamelCasePattern.Split(word);
            
            foreach (var subWord in subWords)
            {
                if (string.IsNullOrWhiteSpace(subWord) || subWord.Length < 2)
                {
                    continue;
                }
                
                var normalized = subWord.ToLowerInvariant();
                
                // Skip stop words
                if (StopWords.Contains(normalized))
                {
                    continue;
                }
                
                tokens.Add(normalized);
            }
        }

        return tokens;
    }

    public IReadOnlyList<TokenPosition> TokenizeWithPositions(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Array.Empty<TokenPosition>();
        }

        var positions = new List<TokenPosition>();
        var lines = content.Split('\n');
        
        for (int lineNum = 0; lineNum < lines.Length; lineNum++)
        {
            var line = lines[lineNum];
            var words = WordPattern.Matches(line);
            
            foreach (Match match in words)
            {
                var word = match.Value;
                var column = match.Index;
                var subWords = CamelCasePattern.Split(word);
                
                foreach (var subWord in subWords)
                {
                    if (string.IsNullOrWhiteSpace(subWord) || subWord.Length < 2)
                    {
                        continue;
                    }
                    
                    var normalized = subWord.ToLowerInvariant();
                    
                    if (StopWords.Contains(normalized))
                    {
                        continue;
                    }
                    
                    positions.Add(new TokenPosition(normalized, lineNum + 1, column));
                }
            }
        }

        return positions;
    }
}
```

#### BM25Ranker.cs

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Acode.Domain.Index;

namespace Acode.Infrastructure.Index;

/// <summary>
/// BM25 (Best Matching 25) ranking algorithm implementation.
/// </summary>
public sealed class BM25Ranker
{
    private const double K1 = 1.2;  // Term frequency saturation parameter
    private const double B = 0.75;  // Length normalization parameter

    /// <summary>
    /// Calculates BM25 score for a document against a query.
    /// </summary>
    /// <param name="queryTerms">Parsed query terms.</param>
    /// <param name="document">Document to score.</param>
    /// <param name="documentFrequencies">Document frequency for each term.</param>
    /// <param name="totalDocuments">Total number of documents in index.</param>
    /// <param name="averageDocumentLength">Average document length in tokens.</param>
    /// <returns>BM25 relevance score.</returns>
    public double CalculateScore(
        IReadOnlyList<string> queryTerms,
        IndexedDocument document,
        Dictionary<string, int> documentFrequencies,
        int totalDocuments,
        double averageDocumentLength)
    {
        double score = 0.0;
        var documentLength = document.TokenCount;

        foreach (var term in queryTerms)
        {
            if (!document.TermFrequencies.TryGetValue(term, out var termFrequency))
            {
                continue;
            }

            var df = documentFrequencies.GetValueOrDefault(term, 1);
            
            // IDF (Inverse Document Frequency)
            var idf = Math.Log((totalDocuments - df + 0.5) / (df + 0.5) + 1.0);
            
            // Term frequency component with saturation
            var tfComponent = (termFrequency * (K1 + 1)) /
                             (termFrequency + K1 * (1 - B + B * (documentLength / averageDocumentLength)));
            
            score += idf * tfComponent;
        }

        return score;
    }

    /// <summary>
    /// Ranks a list of documents by BM25 score.
    /// </summary>
    public IReadOnlyList<ScoredDocument> RankDocuments(
        IReadOnlyList<string> queryTerms,
        IReadOnlyList<IndexedDocument> documents,
        IIndexStore indexStore)
    {
        var totalDocs = indexStore.GetTotalDocumentCount();
        var avgLength = indexStore.GetAverageDocumentLength();
        var docFreqs = indexStore.GetDocumentFrequencies(queryTerms);

        var scored = documents
            .Select(doc => new ScoredDocument(
                doc,
                CalculateScore(queryTerms, doc, docFreqs, totalDocs, avgLength)))
            .OrderByDescending(sd => sd.Score)
            .ToList();

        // Normalize scores to 0-1 range
        if (scored.Count > 0)
        {
            var maxScore = scored[0].Score;
            if (maxScore > 0)
            {
                return scored
                    .Select(sd => sd with { Score = sd.Score / maxScore })
                    .ToList();
            }
        }

        return scored;
    }
}

public sealed record ScoredDocument(IndexedDocument Document, double Score);
```

---

### CLI Commands

#### IndexCommand.cs

```csharp
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Index;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace Acode.Cli.Commands;

public sealed class IndexCommand : Command
{
    public IndexCommand() : base("index", "Manage the code search index")
    {
        AddCommand(new BuildCommand());
        AddCommand(new UpdateCommand());
        AddCommand(new RebuildCommand());
        AddCommand(new StatusCommand());
    }

    private sealed class BuildCommand : Command
    {
        public BuildCommand() : base("build", "Build the index from scratch")
        {
            var pathOption = new Option<string>(
                "--path",
                () => ".",
                "Path to the repository root");
            AddOption(pathOption);

            this.SetHandler(async (InvocationContext ctx) =>
            {
                var path = ctx.ParseResult.GetValueForOption(pathOption)!;
                var indexService = ctx.BindingContext.GetRequiredService<IIndexService>();
                var ct = ctx.GetCancellationToken();

                await AnsiConsole.Progress()
                    .StartAsync(async progressCtx =>
                    {
                        var task = progressCtx.AddTask("Building index...");
                        
                        var progress = new Progress<IndexProgress>(p =>
                        {
                            task.Value = (double)p.FilesProcessed / p.TotalFiles * 100;
                            task.Description = $"Indexing: {p.CurrentFile}";
                        });

                        var options = new IndexBuildOptions { RootPath = path };
                        var result = await indexService.BuildAsync(options, progress, ct);

                        if (result.Success)
                        {
                            AnsiConsole.MarkupLine($"[green]Index built successfully![/]");
                            AnsiConsole.MarkupLine($"  Files indexed: [bold]{result.FilesIndexed}[/]");
                            AnsiConsole.MarkupLine($"  Files skipped: [bold]{result.FilesSkipped}[/]");
                            AnsiConsole.MarkupLine($"  Duration: [bold]{result.Duration.TotalSeconds:F1}s[/]");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]Index build failed: {result.ErrorMessage}[/]");
                            ctx.ExitCode = 1;
                        }
                    });
            });
        }
    }

    private sealed class UpdateCommand : Command
    {
        public UpdateCommand() : base("update", "Update the index incrementally")
        {
            this.SetHandler(async (InvocationContext ctx) =>
            {
                var indexService = ctx.BindingContext.GetRequiredService<IIndexService>();
                var ct = ctx.GetCancellationToken();

                AnsiConsole.MarkupLine("[yellow]Updating index...[/]");
                
                var result = await indexService.UpdateAsync(ct);

                if (result.Success)
                {
                    AnsiConsole.MarkupLine($"[green]Index updated![/]");
                    AnsiConsole.MarkupLine($"  Added: [bold]{result.FilesAdded}[/]");
                    AnsiConsole.MarkupLine($"  Modified: [bold]{result.FilesModified}[/]");
                    AnsiConsole.MarkupLine($"  Deleted: [bold]{result.FilesDeleted}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Update failed: {result.ErrorMessage}[/]");
                    ctx.ExitCode = 1;
                }
            });
        }
    }

    private sealed class RebuildCommand : Command
    {
        public RebuildCommand() : base("rebuild", "Rebuild the index from scratch")
        {
            this.SetHandler(async (InvocationContext ctx) =>
            {
                var indexService = ctx.BindingContext.GetRequiredService<IIndexService>();
                var ct = ctx.GetCancellationToken();

                AnsiConsole.MarkupLine("[yellow]Rebuilding index...[/]");
                
                var options = new IndexBuildOptions();
                var result = await indexService.RebuildAsync(options, ct);

                if (result.Success)
                {
                    AnsiConsole.MarkupLine($"[green]Index rebuilt![/]");
                    AnsiConsole.MarkupLine($"  Files: [bold]{result.FilesIndexed}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Rebuild failed: {result.ErrorMessage}[/]");
                    ctx.ExitCode = 1;
                }
            });
        }
    }

    private sealed class StatusCommand : Command
    {
        public StatusCommand() : base("status", "Show index status")
        {
            var jsonOption = new Option<bool>("--json", "Output as JSON");
            AddOption(jsonOption);

            this.SetHandler(async (InvocationContext ctx) =>
            {
                var json = ctx.ParseResult.GetValueForOption(jsonOption);
                var indexService = ctx.BindingContext.GetRequiredService<IIndexService>();
                var ct = ctx.GetCancellationToken();

                var stats = await indexService.GetStatsAsync(ct);

                if (json)
                {
                    var jsonOutput = System.Text.Json.JsonSerializer.Serialize(stats);
                    Console.WriteLine(jsonOutput);
                }
                else
                {
                    AnsiConsole.MarkupLine("[bold]Index Status[/]");
                    AnsiConsole.MarkupLine("────────────────────");
                    AnsiConsole.MarkupLine($"Files indexed: [bold]{stats.FileCount}[/]");
                    AnsiConsole.MarkupLine($"Index size: [bold]{FormatSize(stats.IndexSizeBytes)}[/]");
                    AnsiConsole.MarkupLine($"Last updated: [bold]{stats.LastUpdated:yyyy-MM-dd HH:mm:ss}[/]");
                    AnsiConsole.MarkupLine($"Pending updates: [bold]{stats.PendingChanges}[/] files");
                }
            });
        }

        private static string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.#} {sizes[order]}";
        }
    }
}
```

#### SearchCommand.cs

```csharp
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Acode.Domain.Index;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace Acode.Cli.Commands;

public sealed class SearchCommand : Command
{
    public SearchCommand() : base("search", "Search the codebase")
    {
        var queryArg = new Argument<string>("query", "Search query");
        AddArgument(queryArg);

        var extOption = new Option<string?>("--ext", "Filter by file extension(s), comma-separated");
        var dirOption = new Option<string?>("--dir", "Filter by directory");
        var skipOption = new Option<int>("--skip", () => 0, "Number of results to skip");
        var takeOption = new Option<int>("--take", () => 20, "Number of results to return");
        var jsonOption = new Option<bool>("--json", "Output as JSONL");
        
        AddOption(extOption);
        AddOption(dirOption);
        AddOption(skipOption);
        AddOption(takeOption);
        AddOption(jsonOption);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var queryText = ctx.ParseResult.GetValueForArgument(queryArg);
            var ext = ctx.ParseResult.GetValueForOption(extOption);
            var dir = ctx.ParseResult.GetValueForOption(dirOption);
            var skip = ctx.ParseResult.GetValueForOption(skipOption);
            var take = ctx.ParseResult.GetValueForOption(takeOption);
            var json = ctx.ParseResult.GetValueForOption(jsonOption);
            
            var indexService = ctx.BindingContext.GetRequiredService<IIndexService>();
            var ct = ctx.GetCancellationToken();

            var filter = new SearchFilter
            {
                Extensions = ext?.Split(',').Select(e => e.Trim()).ToArray(),
                Directory = dir
            };

            var query = new SearchQuery(queryText)
            {
                Filter = filter,
                Skip = skip,
                Take = take
            };

            try
            {
                var results = await indexService.SearchAsync(query, ct);

                if (json)
                {
                    foreach (var result in results)
                    {
                        var jsonLine = JsonSerializer.Serialize(new
                        {
                            filePath = result.FilePath,
                            score = Math.Round(result.Score, 3),
                            matchedLines = result.MatchedLines,
                            snippets = result.Snippets.Select(s => s.Text).ToArray()
                        });
                        Console.WriteLine(jsonLine);
                    }
                }
                else
                {
                    if (results.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[yellow]No results found.[/]");
                        return;
                    }

                    AnsiConsole.MarkupLine($"[bold]Found {results.TotalCount} results:[/]\n");

                    var displayCount = 1;
                    foreach (var result in results)
                    {
                        AnsiConsole.MarkupLine(
                            $"[bold]{displayCount}.[/] [[{result.FilePath}]] [dim](score: {result.Score:F2})[/]");
                        
                        foreach (var snippet in result.Snippets.Take(2))
                        {
                            AnsiConsole.MarkupLine($"   [dim]Line {snippet.StartLine}:[/] {EscapeMarkup(snippet.Text.Trim())}");
                        }
                        
                        AnsiConsole.WriteLine();
                        displayCount++;
                    }

                    if (results.TotalCount > results.Count)
                    {
                        AnsiConsole.MarkupLine(
                            $"[dim]Showing {results.Count} of {results.TotalCount} results. Use --skip and --take for pagination.[/]");
                    }
                }
            }
            catch (SearchQueryException ex)
            {
                AnsiConsole.MarkupLine($"[red]Search error ({ex.ErrorCode}): {ex.Message}[/]");
                ctx.ExitCode = 1;
            }
        });
    }

    private static string EscapeMarkup(string text)
    {
        return text.Replace("[", "[[").Replace("]", "]]");
    }
}
```

---

### Error Codes

| Code | Meaning | User Message |
|------|---------|--------------|
| ACODE-IDX-001 | Index build failed | Index build failed. Check file permissions and disk space. |
| ACODE-IDX-002 | Search query parse error | Invalid search query. Check for unbalanced quotes or invalid operators. |
| ACODE-IDX-003 | Incremental update failed | Index update failed. Try rebuilding with `acode index rebuild`. |
| ACODE-IDX-004 | Index file corrupted | Index file is corrupted. Run `acode index rebuild` to fix. |
| ACODE-IDX-005 | Ignore pattern parse error | Invalid ignore pattern. Check .gitignore and .agentignore syntax. |
| ACODE-IDX-006 | Index not found | No index found. Run `acode index build` first. |
| ACODE-IDX-007 | Index version mismatch | Index was created with a different version. Rebuilding... |
| ACODE-IDX-008 | Search timeout | Search timed out. Try a more specific query or add filters. |

---

### Implementation Checklist

1. [ ] Create domain models (SearchQuery, SearchResult, SearchFilter, IndexStats)
2. [ ] Create domain interfaces (IIndexService, ITokenizer, ISearchEngine)
3. [ ] Implement CodeTokenizer with camelCase/snake_case splitting
4. [ ] Implement IgnoreRuleParser with Git specification compliance
5. [ ] Implement IgnoreMatcher with pattern matching and negation
6. [ ] Implement IndexBuilder with file enumeration and tokenization
7. [ ] Implement IndexStore with SQLite FTS5 backend
8. [ ] Implement SearchEngine with BM25 ranking
9. [ ] Implement SearchQueryParser with operators and wildcards
10. [ ] Implement IncrementalUpdater with change detection
11. [ ] Implement IndexIntegrityVerifier with checksum validation
12. [ ] Implement IndexService facade with write locking
13. [ ] Create IndexCommand CLI with build/update/rebuild/status
14. [ ] Create SearchCommand CLI with filters and JSON output
15. [ ] Register services in DI container
16. [ ] Write unit tests for all components
17. [ ] Write integration tests for end-to-end flows
18. [ ] Write performance benchmarks

---

### Validation Checklist

| Requirement | Validation |
|-------------|------------|
| Index 1K files < 5s | Run benchmark with 1,000 test files |
| Search < 100ms | Measure search latency with Stopwatch |
| Incremental update 10 files < 2s | Modify 10 files and measure update time |
| BM25 ranking accurate | Compare rankings with expected relevance |
| Ignore patterns respect .gitignore | Test with node_modules and build outputs |
| Checksum validates integrity | Corrupt index and verify detection |
| Concurrent searches safe | Run parallel search load test |
| JSON output valid | Parse output with System.Text.Json |

---

### Rollout Plan

| Phase | Description | Duration |
|-------|-------------|----------|
| 1 | Domain models and interfaces | 1 day |
| 2 | Tokenizer and ignore rules | 2 days |
| 3 | Index builder with SQLite FTS5 | 3 days |
| 4 | Search engine with BM25 | 2 days |
| 5 | Incremental updater | 2 days |
| 6 | CLI commands | 1 day |
| 7 | Integration and E2E testing | 2 days |
| 8 | Performance optimization | 1 day |

---

**End of Task 015 Specification**