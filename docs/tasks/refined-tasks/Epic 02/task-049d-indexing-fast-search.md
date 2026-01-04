# Task 049.d: Indexing + Fast Search Over Chats/Runs/Messages

**Priority:** P1 – High Priority  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 2 – CLI + Orchestration Core  
**Dependencies:** Task 049.a (Data Model), Task 049.b (CLI Commands), Task 030 (Search)  

---

## Description

Task 049.d implements indexing and fast search across conversation history. Users need to find past discussions quickly: "What did we decide about authentication?" Full-text search with ranking makes this possible.

Search is essential for productivity. Developers have hundreds of conversations across dozens of projects. Finding relevant context shouldn't require scrolling through history. Type a query, get ranked results instantly.

The indexing system builds searchable structures from conversation content. Message text is tokenized, stemmed, and indexed. Chat titles and tags are indexed. Run metadata is indexed. The index updates incrementally as content is added.

Full-text search uses SQLite FTS5 for local storage. FTS5 provides efficient term matching, phrase search, and ranking. PostgreSQL uses its native full-text search for remote. Both backends provide consistent search semantics.

Search queries support common patterns. Simple term search finds messages containing words. Phrase search with quotes finds exact sequences. Boolean operators (AND, OR, NOT) combine conditions. Field-specific search targets titles, content, or tags.

Ranking orders results by relevance. Term frequency and document length factor into scores. Recent messages rank higher for recency bias. Results include snippets with highlighted matches.

Search scope can be constrained. Search within a specific chat. Search within a date range. Search by message role (user, assistant). Search by run status. Combining constraints narrows results.

Incremental indexing keeps the index current. New messages are indexed immediately. Updates reindex affected content. Deletions remove from index. Background reindexing handles schema changes.

Index maintenance ensures performance. Periodic optimization merges index segments. Statistics update for accurate ranking. Corruption detection triggers rebuild.

The search API exposes query capabilities. ISearchService defines the interface. SearchQuery encapsulates parameters. SearchResult contains matches with context. Pagination handles large result sets.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| FTS5 | SQLite Full-Text Search 5 |
| Indexing | Building searchable structure |
| Tokenization | Splitting text into terms |
| Stemming | Reducing words to roots |
| Ranking | Ordering by relevance |
| Snippet | Highlighted match excerpt |
| Phrase Search | Exact sequence match |
| Boolean Search | AND/OR/NOT operators |
| Incremental | Adding without rebuild |
| Reindex | Rebuild index |
| Segment | Index partition |
| Optimize | Merge segments |
| Scope | Search constraint |
| Recency Bias | Prefer recent results |
| Stop Words | Ignored common words |

---

## Out of Scope

The following items are explicitly excluded from Task 049.d:

- **Data model** - Task 049.a
- **CLI commands** - Task 049.b
- **Concurrency** - Task 049.c
- **Retention** - Task 049.e
- **Sync** - Task 049.f
- **Semantic search** - Term-based only
- **ML ranking** - Rule-based only
- **Cross-workspace** - Single workspace
- **Real-time updates** - Near real-time
- **Fuzzy matching** - Exact terms only

---

## Assumptions

### Technical Assumptions

- ASM-001: SQLite FTS5 or similar provides full-text indexing
- ASM-002: Index updates are near real-time (< 1s delay)
- ASM-003: Search queries use standard syntax (term AND/OR)
- ASM-004: Ranking uses relevance scoring
- ASM-005: Index size is proportional to content size

### Behavioral Assumptions

- ASM-006: Users search for specific terms or phrases
- ASM-007: Search results show relevant context snippets
- ASM-008: Filters narrow results (date, chat, sender)
- ASM-009: Search is fast (< 100ms for typical queries)
- ASM-010: Empty results provide suggestions

### Dependency Assumptions

- ASM-011: Task 049.a data model provides indexable content
- ASM-012: Task 050 database supports FTS extensions
- ASM-013: Task 049.b provides search CLI command

### Indexing Assumptions

- ASM-014: Messages are indexed on creation
- ASM-015: Index rebuilds are rare but supported
- ASM-016: Stop words are configurable

---

## Functional Requirements

### Index Structure

- FR-001: Index MUST cover messages
- FR-002: Index MUST cover chat titles
- FR-003: Index MUST cover tags
- FR-004: Index MUST support full-text

### SQLite FTS5

- FR-005: FTS5 virtual table MUST exist
- FR-006: Content MUST be tokenized
- FR-007: Porter stemmer MUST be used
- FR-008: Stop words MUST be filtered

### PostgreSQL FTS

- FR-009: tsvector column MUST exist
- FR-010: GIN index MUST be created
- FR-011: Triggers MUST update vectors
- FR-012: Dictionary MUST be configurable

### Search Queries

- FR-013: Term search MUST work
- FR-014: Phrase search MUST work
- FR-015: Boolean AND MUST work
- FR-016: Boolean OR MUST work
- FR-017: Boolean NOT MUST work
- FR-018: Field search MUST work

### Ranking

- FR-019: Results MUST be ranked
- FR-020: BM25 algorithm MUST be used
- FR-021: Recency MUST boost score
- FR-022: Ranking MUST be configurable

### Snippets

- FR-023: Snippets MUST be generated
- FR-024: Match terms MUST be highlighted
- FR-025: Snippet length MUST be configurable
- FR-026: Multiple snippets per result

### Scope Filters

- FR-027: Filter by chat MUST work
- FR-028: Filter by date MUST work
- FR-029: Filter by role MUST work
- FR-030: Filter by status MUST work
- FR-031: Combined filters MUST work

### Pagination

- FR-032: Results MUST paginate
- FR-033: Total count MUST be available
- FR-034: Page size MUST be configurable
- FR-035: Cursor MUST support deep paging

### Incremental Indexing

- FR-036: New messages MUST index
- FR-037: Updates MUST reindex
- FR-038: Deletes MUST deindex
- FR-039: Indexing MUST be async

### Index Maintenance

- FR-040: Optimize MUST merge segments
- FR-041: Stats MUST update
- FR-042: Corruption MUST be detected
- FR-043: Rebuild MUST be possible

### CLI Integration

- FR-044: `acode search` MUST work
- FR-045: Results MUST show snippets
- FR-046: `--json` MUST output JSON
- FR-047: `--chat` MUST filter

---

## Non-Functional Requirements

### Performance

- NFR-001: Index message < 10ms
- NFR-002: Search 10k messages < 500ms
- NFR-003: Snippet generation < 50ms

### Accuracy

- NFR-004: No false negatives for exact terms
- NFR-005: Relevant results ranked higher
- NFR-006: Consistent across backends

### Reliability

- NFR-007: Index survives crash
- NFR-008: Corruption auto-detected
- NFR-009: Rebuild available

### Scalability

- NFR-010: Handle 100k+ messages
- NFR-011: Index size < 30% content
- NFR-012: Query time sublinear

---

## User Manual Documentation

### Overview

Search finds relevant conversations across your history. Full-text search with ranking means quick answers to "What did we discuss about...?"

### Quick Start

```bash
# Simple search
$ acode search "authentication"

Results for 'authentication' (47 matches)
────────────────────────────────────
[chat_abc123] Feature: User Auth (5 matches)
  "...designing the authentication flow using JWT..."
  "...authentication middleware needs rate limiting..."

[chat_def456] Security Review (3 matches)
  "...authentication bypass vulnerability in..."

# Search within chat
$ acode search --chat chat_abc123 "JWT"

Results for 'JWT' in 'Feature: User Auth' (12 matches)
```

### Query Syntax

```bash
# Simple terms (OR by default)
$ acode search "login authentication"

# Phrase search
$ acode search '"forgot password"'

# Boolean AND
$ acode search "login AND password"

# Boolean NOT
$ acode search "authentication NOT OAuth"

# Field search
$ acode search "title:security"
$ acode search "tag:feature"
```

### Filtering

```bash
# Filter by date
$ acode search "auth" --since 2024-01-01
$ acode search "auth" --until 2024-06-30

# Filter by role
$ acode search "error" --role assistant

# Filter by run status
$ acode search "failed" --status failed

# Combined
$ acode search "auth" --since 2024-01-01 --chat chat_abc123
```

### Output Formats

```bash
# Table format (default)
$ acode search "auth"
Chat          Title              Matches  Top Snippet
chat_abc123   Feature: Auth      5        "...authentication flow..."
chat_def456   Security Review    3        "...authentication bypass..."

# Detailed format
$ acode search "auth" --detail
[chat_abc123] Feature: User Auth
Score: 8.5 | Messages: 5 matches | Updated: 2h ago
  1. "Let's design the authentication flow using JWT..."
  2. "The authentication middleware needs rate limiting..."

# JSON format
$ acode search "auth" --json
{
  "query": "auth",
  "total": 47,
  "results": [...]
}
```

### Index Management

```bash
# Check index status
$ acode search index status
Index Status: Healthy
────────────────────────────────────
Messages indexed: 12,456
Index size: 4.2 MB
Last updated: 2m ago
Pending: 0

# Rebuild index
$ acode search index rebuild
Rebuilding search index...
Indexing 12,456 messages...
████████████████████ 100%
Index rebuilt in 4.2s

# Optimize index
$ acode search index optimize
Optimizing search index...
Merged 5 segments into 1
Index optimized.
```

### Configuration

```yaml
# .agent/config.yml
search:
  # Ranking settings
  ranking:
    recency_boost: 1.5  # Boost recent results
    title_boost: 2.0    # Boost title matches
    
  # Snippet settings
  snippets:
    max_length: 150
    max_per_result: 3
    highlight_tag: "**"
    
  # Index settings
  index:
    auto_optimize: true
    optimize_threshold: 10  # segments
    
  # Default filters
  defaults:
    include_archived: false
    page_size: 20
```

### Troubleshooting

#### No Results

**Problem:** Search returns nothing

**Solutions:**
1. Check spelling
2. Try simpler query
3. Remove restrictive filters
4. Check index status

#### Slow Search

**Problem:** Search takes too long

**Solutions:**
1. Run optimize: `acode search index optimize`
2. Add filters to narrow scope
3. Check index size vs message count

#### Missing Recent Messages

**Problem:** New messages not searchable

**Solutions:**
1. Check pending count: `acode search index status`
2. Wait for indexing (usually < 1s)
3. Force reindex: `acode search index rebuild`

---

## Acceptance Criteria

### Indexing

- [ ] AC-001: Messages indexed
- [ ] AC-002: Titles indexed
- [ ] AC-003: Tags indexed
- [ ] AC-004: FTS5 used (SQLite)
- [ ] AC-005: tsvector used (Postgres)

### Search

- [ ] AC-006: Term search works
- [ ] AC-007: Phrase search works
- [ ] AC-008: Boolean works
- [ ] AC-009: Field search works

### Ranking

- [ ] AC-010: Results ranked
- [ ] AC-011: Recency boost works
- [ ] AC-012: BM25 used

### Snippets

- [ ] AC-013: Snippets generated
- [ ] AC-014: Matches highlighted
- [ ] AC-015: Length configurable

### Filters

- [ ] AC-016: Chat filter works
- [ ] AC-017: Date filter works
- [ ] AC-018: Role filter works
- [ ] AC-019: Combined works

### Maintenance

- [ ] AC-020: Incremental index
- [ ] AC-021: Optimize works
- [ ] AC-022: Rebuild works

### CLI

- [ ] AC-023: `search` works
- [ ] AC-024: Filters work
- [ ] AC-025: JSON output

---

## Best Practices

### Index Management

- **BP-001: Index incrementally** - Update index on message creation, not full rebuilds
- **BP-002: Background indexing** - Don't block writes while indexing
- **BP-003: Monitor index size** - Track growth and set alerts for abnormal expansion
- **BP-004: Periodic optimization** - Schedule index optimization during low activity

### Search Design

- **BP-005: Simple query syntax** - Support intuitive search without special characters
- **BP-006: Highlight matches** - Show matching terms in result snippets
- **BP-007: Relevance ranking** - Sort by relevance, not just recency
- **BP-008: Faceted filtering** - Enable filtering by chat, date, sender

### Performance

- **BP-009: Query timeout** - Prevent runaway queries from blocking system
- **BP-010: Result limits** - Cap results per page for responsiveness
- **BP-011: Index caching** - Cache frequently accessed index segments
- **BP-012: Async search** - Support cancellation for long queries

---

## Troubleshooting

### Index Corruption

**Symptom:** Search returns errors or incorrect results.

**Cause:** Index became corrupted due to crash or incomplete write.

**Solution:**
1. Rebuild index from source data
2. Check for disk errors
3. Review crash logs

### Search Performance Degraded

**Symptom:** Searches take much longer than expected.

**Cause:** Index needs optimization or query is too broad.

**Solution:**
1. Run index optimization
2. Add more specific terms to query
3. Check database performance

### Results Missing Known Content

**Symptom:** Search doesn't find content you know exists.

**Cause:** Content not indexed yet or stop words removed.

**Solution:**
1. Wait for index update
2. Try different search terms
3. Check if content was redacted

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Search/
├── IndexerTests.cs
│   ├── Should_Tokenize_Content()
│   ├── Should_Stem_Terms()
│   └── Should_Filter_StopWords()
│
├── QueryParserTests.cs
│   ├── Should_Parse_Terms()
│   ├── Should_Parse_Phrases()
│   └── Should_Parse_Boolean()
│
├── RankerTests.cs
│   ├── Should_Calculate_BM25()
│   └── Should_Apply_Recency()
│
└── SnippetTests.cs
    ├── Should_Generate_Snippet()
    └── Should_Highlight_Matches()
```

### Integration Tests

```
Tests/Integration/Search/
├── SqliteFtsTests.cs
│   ├── Should_Index_And_Search()
│   └── Should_Handle_Large_Corpus()
│
└── PostgresFtsTests.cs
    ├── Should_Index_And_Search()
    └── Should_Use_GinIndex()
```

### E2E Tests

```
Tests/E2E/Search/
├── SearchE2ETests.cs
│   ├── Should_Find_Messages()
│   ├── Should_Rank_By_Relevance()
│   └── Should_Filter_By_Chat()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Index message | 5ms | 10ms |
| Search 10k | 250ms | 500ms |
| Snippet gen | 25ms | 50ms |

---

## User Verification Steps

### Scenario 1: Simple Search

1. Add messages with keywords
2. Run `acode search <keyword>`
3. Verify: Matching messages found

### Scenario 2: Phrase Search

1. Add message with phrase
2. Search with quotes
3. Verify: Exact phrase matched

### Scenario 3: Boolean Search

1. Add varied messages
2. Search with AND/OR/NOT
3. Verify: Boolean logic works

### Scenario 4: Filtering

1. Add messages to different chats
2. Search with --chat filter
3. Verify: Only that chat searched

### Scenario 5: Ranking

1. Add messages with term
2. Search for term
3. Verify: More relevant first

### Scenario 6: Rebuild Index

1. Add messages
2. Run rebuild
3. Search
4. Verify: All messages found

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Search/
│   ├── SearchQuery.cs
│   └── SearchResult.cs
│
src/AgenticCoder.Application/
├── Search/
│   ├── ISearchService.cs
│   ├── IIndexer.cs
│   └── IRanker.cs
│
src/AgenticCoder.Infrastructure/
├── Search/
│   ├── SqliteFtsSearchService.cs
│   ├── SqliteFtsIndexer.cs
│   ├── PostgresFtsSearchService.cs
│   ├── BM25Ranker.cs
│   └── SnippetGenerator.cs
```

### SearchQuery Value Object

```csharp
namespace AgenticCoder.Domain.Search;

public sealed record SearchQuery
{
    public string QueryText { get; }
    public ChatId? ChatFilter { get; }
    public DateTimeOffset? Since { get; }
    public DateTimeOffset? Until { get; }
    public MessageRole? RoleFilter { get; }
    public int PageSize { get; init; } = 20;
    public string? Cursor { get; init; }
}
```

### ISearchService Interface

```csharp
namespace AgenticCoder.Application.Search;

public interface ISearchService
{
    Task<SearchResults> SearchAsync(
        SearchQuery query,
        CancellationToken ct);
        
    Task IndexMessageAsync(
        Message message,
        CancellationToken ct);
        
    Task RebuildIndexAsync(
        CancellationToken ct);
        
    Task<IndexStatus> GetStatusAsync(
        CancellationToken ct);
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-SRCH-001 | Invalid query syntax |
| ACODE-SRCH-002 | Index corrupted |
| ACODE-SRCH-003 | Index rebuild failed |
| ACODE-SRCH-004 | Search timeout |
| ACODE-SRCH-005 | Too many results |

### Implementation Checklist

1. [ ] Create domain types
2. [ ] Create service interfaces
3. [ ] Implement SQLite FTS5
4. [ ] Implement PostgreSQL FTS
5. [ ] Implement ranker
6. [ ] Implement snippets
7. [ ] Add CLI command
8. [ ] Add index management
9. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Domain types
2. **Phase 2:** SQLite FTS5
3. **Phase 3:** Ranking
4. **Phase 4:** Snippets
5. **Phase 5:** CLI
6. **Phase 6:** PostgreSQL
7. **Phase 7:** Maintenance

---

**End of Task 049.d Specification**