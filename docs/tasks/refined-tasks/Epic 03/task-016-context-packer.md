# Task 016: Context Packer

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 015 (Indexing), Task 014 (RepoFS), Task 004 (Model Provider)  

---

## Description

### Business Value

The Context Packer is the intelligence behind what code the agent "sees." LLMs have limited context windows—typically 100K-200K tokens. A typical enterprise codebase contains millions of lines of code. The Context Packer solves this fundamental mismatch.

Without intelligent context selection, the agent would either:
- Include too little context and miss critical information
- Include too much and exceed token limits
- Include irrelevant code and confuse the model

The Context Packer provides:

1. **Optimal Information Density:** Maximizes useful information per token. The agent works with the most relevant code, not random selections.

2. **Token Budget Management:** Guarantees context fits within model limits. Reserves space for system prompts and responses. Never exceeds limits.

3. **Multi-Source Aggregation:** Combines search results, open files, tool outputs, and references into coherent context. Deduplicates to avoid wasting tokens.

4. **Quality Rankings:** Prioritizes code by relevance to the current task. Higher-ranked content is included first when budget is limited.

5. **Consistent Formatting:** Produces well-structured context that LLMs can easily parse. Clear file boundaries, line numbers, and language hints.

### Scope

This task implements the complete context packing pipeline:

1. **Source Collector:** Gathers candidate content from multiple sources (search, files, tools). Normalizes into common format.

2. **Chunker:** Breaks large files into meaningful pieces (functions, classes, sections). Preserves semantic boundaries.

3. **Ranker:** Scores and orders chunks by relevance. Combines multiple signals (task relevance, recency, source priority).

4. **Budget Manager:** Tracks token allocations. Enforces limits. Reserves space for system and response.

5. **Deduplicator:** Detects and removes duplicate or overlapping content. Keeps highest-ranked version.

6. **Selector:** Chooses top-ranked chunks that fit within budget. Balances across sources.

7. **Formatter:** Produces final context string with file headers, line numbers, and separators.

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Task 015 (Index) | Data Source | Search results provide candidates |
| Task 014 (RepoFS) | File Access | Reads file content for chunking |
| Task 004 (Model) | Token Counting | Uses model tokenizer for accurate counts |
| Task 017 (Symbols) | Data Source | Symbol definitions provide candidates |
| Task 012 (Agent Loop) | Consumer | Agent stages request context packing |
| Task 008 (Prompts) | Consumer | Prompt templates include packed context |
| Task 003.c (Audit) | Audit Logging | Context selections are audited |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Token count inaccurate | Context too large or small | Use exact model tokenizer |
| Chunking breaks semantics | Confusing context | Prefer structural boundaries |
| Ranking misses relevance | Poor context quality | Configurable weights, tuning |
| Dedup misses overlap | Wasted tokens | Content hashing, range comparison |
| Budget exceeded | Model error | Hard limit enforcement |
| No candidates | Empty context | Warn user, suggest search |
| Source timeout | Missing content | Timeout handling, partial results |
| Memory exhaustion | Crash on large repos | Streaming processing |

### Assumptions

1. Model tokenizer is available for accurate token counting
2. Source content is UTF-8 text
3. File structure is determinable (for structural chunking)
4. Total budget is known before packing
5. Relevance scores are comparable across sources
6. Token counting is fast (<50ms for typical content)
7. Typical context is 10-100 chunks
8. Typical chunk is 100-2000 tokens
9. Deduplication is based on content equality
10. Formatting overhead is predictable

### Security Considerations

The Context Packer handles repository content and must ensure:

1. **Content Filtering:** Sensitive files (secrets, credentials) SHOULD be filtered from context.

2. **Path Sanitization:** File paths in formatted output MUST be relative, not absolute.

3. **No Data Leakage:** Context packing MUST NOT log full content. Debug logs SHOULD show only file paths and sizes.

4. **Audit Trail:** Context selections SHOULD be logged for debugging without exposing content.

5. **Token Limits:** Hard enforcement prevents potential memory attacks via oversized content.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Context Packer | Assembles LLM prompts |
| Context Window | LLM token limit |
| Chunk | Content piece |
| Ranking | Priority ordering |
| Token Budget | Allowed tokens |
| Deduplication | Remove duplicates |
| Relevance | Task applicability |
| Source | Content origin |
| Candidate | Potential inclusion |
| Selection | Chosen content |
| Overflow | Exceeds budget |
| Truncation | Cut to fit |
| Density | Info per token |
| Format | Output structure |
| Prompt | LLM input |

---

## Out of Scope

The following items are explicitly excluded from Task 016:

- **Semantic ranking** - Embedding-based (v2)
- **Dynamic budgeting** - Fixed budgets v1
- **Multi-model support** - Single model v1
- **Streaming assembly** - Batch assembly
- **Caching** - Fresh assembly each time
- **Compression** - No summarization v1

---

## Functional Requirements

### Context Packer Interface (FR-016-01 to FR-016-15)

| ID | Requirement |
|----|-------------|
| FR-016-01 | System MUST define IContextPacker interface |
| FR-016-02 | PackAsync MUST accept list of ContextSource |
| FR-016-03 | PackAsync MUST accept ContextBudget parameter |
| FR-016-04 | PackAsync MUST accept CancellationToken |
| FR-016-05 | PackAsync MUST return PackedContext |
| FR-016-06 | PackedContext MUST include formatted content string |
| FR-016-07 | PackedContext MUST include total token count |
| FR-016-08 | PackedContext MUST include list of included chunks |
| FR-016-09 | PackedContext MUST include list of excluded chunks |
| FR-016-10 | IContextPacker MUST have PreviewAsync method |
| FR-016-11 | PreviewAsync MUST return what would be included |
| FR-016-12 | PreviewAsync MUST NOT format content |
| FR-016-13 | IContextPacker MUST have GetStatsAsync method |
| FR-016-14 | GetStatsAsync MUST return packing statistics |
| FR-016-15 | All methods MUST support cancellation |

### Source Collection (FR-016-16 to FR-016-30)

| ID | Requirement |
|----|-------------|
| FR-016-16 | System MUST define ContextSource record |
| FR-016-17 | ContextSource MUST have SourceType enum |
| FR-016-18 | SourceType MUST include SearchResult |
| FR-016-19 | SourceType MUST include OpenFile |
| FR-016-20 | SourceType MUST include ToolResult |
| FR-016-21 | SourceType MUST include Reference |
| FR-016-22 | ContextSource MUST have Content property |
| FR-016-23 | ContextSource MUST have FilePath property |
| FR-016-24 | ContextSource MUST have LineRange property |
| FR-016-25 | ContextSource MUST have RelevanceScore property |
| FR-016-26 | ContextSource MUST have Timestamp property |
| FR-016-27 | Collector MUST gather from multiple sources |
| FR-016-28 | Collector MUST normalize to common format |
| FR-016-29 | Collector MUST handle async sources |
| FR-016-30 | Collector MUST respect source timeouts |

### Chunking (FR-016-31 to FR-016-50)

| ID | Requirement |
|----|-------------|
| FR-016-31 | System MUST define IChunker interface |
| FR-016-32 | ChunkAsync MUST accept file content |
| FR-016-33 | ChunkAsync MUST return list of Chunk |
| FR-016-34 | Chunk MUST have Content property |
| FR-016-35 | Chunk MUST have StartLine property |
| FR-016-36 | Chunk MUST have EndLine property |
| FR-016-37 | Chunk MUST have TokenCount property |
| FR-016-38 | Chunker MUST support structural chunking |
| FR-016-39 | Structural chunking MUST identify functions |
| FR-016-40 | Structural chunking MUST identify classes |
| FR-016-41 | Structural chunking MUST identify sections |
| FR-016-42 | Chunker MUST support line-based chunking |
| FR-016-43 | Chunker MUST support token-based chunking |
| FR-016-44 | Chunk size MUST respect MaxChunkTokens |
| FR-016-45 | Chunk size MUST meet MinChunkTokens |
| FR-016-46 | Oversized structures MUST be split |
| FR-016-47 | Split MUST preserve line boundaries |
| FR-016-48 | Chunk MUST NOT start mid-line |
| FR-016-49 | Chunker MUST support language-specific rules |
| FR-016-50 | Unknown languages MUST use line-based chunking |

### Ranking (FR-016-51 to FR-016-70)

| ID | Requirement |
|----|-------------|
| FR-016-51 | System MUST define IRanker interface |
| FR-016-52 | RankAsync MUST accept list of candidates |
| FR-016-53 | RankAsync MUST accept task context |
| FR-016-54 | RankAsync MUST return ordered list |
| FR-016-55 | Ranking MUST consider relevance score |
| FR-016-56 | Ranking MUST consider recency |
| FR-016-57 | Ranking MUST consider source priority |
| FR-016-58 | Ranking weights MUST be configurable |
| FR-016-59 | Default relevance weight MUST be 0.5 |
| FR-016-60 | Default recency weight MUST be 0.3 |
| FR-016-61 | Default source weight MUST be 0.2 |
| FR-016-62 | Source priorities MUST be configurable |
| FR-016-63 | Default ToolResult priority MUST be 100 |
| FR-016-64 | Default OpenFile priority MUST be 80 |
| FR-016-65 | Default SearchResult priority MUST be 60 |
| FR-016-66 | Default Reference priority MUST be 40 |
| FR-016-67 | Combined score MUST be normalized [0,1] |
| FR-016-68 | Equal scores MUST have stable ordering |
| FR-016-69 | Ranking MUST be deterministic |
| FR-016-70 | Ranking MUST log scoring factors |

### Token Budgeting (FR-016-71 to FR-016-90)

| ID | Requirement |
|----|-------------|
| FR-016-71 | System MUST define ITokenCounter interface |
| FR-016-72 | CountTokens MUST accept string content |
| FR-016-73 | CountTokens MUST return accurate count |
| FR-016-74 | Token counter MUST use model-specific tokenizer |
| FR-016-75 | Token counter MUST cache counts |
| FR-016-76 | System MUST define ContextBudget record |
| FR-016-77 | ContextBudget MUST have TotalTokens property |
| FR-016-78 | ContextBudget MUST have ResponseReserve property |
| FR-016-79 | ContextBudget MUST have SystemReserve property |
| FR-016-80 | Available budget MUST be Total - Response - System |
| FR-016-81 | System MUST define IBudgetManager interface |
| FR-016-82 | BudgetManager MUST track allocations |
| FR-016-83 | BudgetManager MUST report remaining budget |
| FR-016-84 | BudgetManager MUST prevent overflow |
| FR-016-85 | Allocation MUST fail if exceeds remaining |
| FR-016-86 | BudgetManager MUST support category budgets |
| FR-016-87 | Category budgets MUST be optional |
| FR-016-88 | BudgetManager MUST support allocation rollback |
| FR-016-89 | Final allocation MUST NOT exceed total |
| FR-016-90 | Overflow MUST raise ContextOverflowException |

### Deduplication (FR-016-91 to FR-016-105)

| ID | Requirement |
|----|-------------|
| FR-016-91 | System MUST define IDeduplicator interface |
| FR-016-92 | Deduplicate MUST accept list of chunks |
| FR-016-93 | Deduplicate MUST return unique chunks |
| FR-016-94 | Exact duplicate MUST be detected |
| FR-016-95 | Exact detection MUST use content hash |
| FR-016-96 | Overlapping chunks MUST be detected |
| FR-016-97 | Overlap detection MUST use file+line range |
| FR-016-98 | Duplicate MUST keep highest-ranked version |
| FR-016-99 | Overlapping chunks MAY be merged |
| FR-016-100 | Merge MUST combine into single chunk |
| FR-016-101 | Merge MUST use highest rank of components |
| FR-016-102 | Deduplication MUST preserve rank order |
| FR-016-103 | Deduplication MUST log removals |
| FR-016-104 | Different sources same content MUST dedupe |
| FR-016-105 | Deduplication MUST be efficient (O(n log n)) |

### Selection (FR-016-106 to FR-016-120)

| ID | Requirement |
|----|-------------|
| FR-016-106 | System MUST define ISelector interface |
| FR-016-107 | Select MUST accept ranked chunk list |
| FR-016-108 | Select MUST accept available budget |
| FR-016-109 | Select MUST return selected chunks |
| FR-016-110 | Selection MUST respect rank order |
| FR-016-111 | Selection MUST fill to budget |
| FR-016-112 | Selection MUST NOT exceed budget |
| FR-016-113 | Chunk larger than remaining MUST be skipped |
| FR-016-114 | Selection MUST try next smaller chunk |
| FR-016-115 | Selection MAY balance across sources |
| FR-016-116 | Balance MUST be configurable |
| FR-016-117 | Selection MUST return excluded chunks |
| FR-016-118 | Excluded MUST include exclusion reason |
| FR-016-119 | Selection MUST support minimum chunks |
| FR-016-120 | Minimum MUST be guaranteed if content exists |

### Formatting (FR-016-121 to FR-016-140)

| ID | Requirement |
|----|-------------|
| FR-016-121 | System MUST define IFormatter interface |
| FR-016-122 | Format MUST accept selected chunks |
| FR-016-123 | Format MUST return formatted string |
| FR-016-124 | Format MUST add file path header |
| FR-016-125 | Header MUST include relative path |
| FR-016-126 | Header MUST include line range |
| FR-016-127 | Format MUST add language hint |
| FR-016-128 | Language hint MUST use code fence |
| FR-016-129 | Format MUST add chunk separator |
| FR-016-130 | Separator MUST be visually distinct |
| FR-016-131 | Multiple chunks same file MUST group |
| FR-016-132 | Grouped chunks MUST show combined range |
| FR-016-133 | Format MUST handle non-contiguous ranges |
| FR-016-134 | Non-contiguous MUST show gap indicator |
| FR-016-135 | Format MUST escape special characters |
| FR-016-136 | Escape MUST not break code fences |
| FR-016-137 | Format MUST be consistent across calls |
| FR-016-138 | Format overhead MUST be included in token count |
| FR-016-139 | Format MUST support custom templates |
| FR-016-140 | Default template MUST be markdown-compatible |

---

## Non-Functional Requirements

### Performance (NFR-016-01 to NFR-016-20)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016-01 | Performance | Full pack operation MUST complete in < 500ms |
| NFR-016-02 | Performance | Token counting MUST complete in < 50ms |
| NFR-016-03 | Performance | Ranking 100 chunks MUST complete in < 50ms |
| NFR-016-04 | Performance | Ranking 1000 chunks MUST complete in < 100ms |
| NFR-016-05 | Performance | Deduplication MUST complete in < 50ms |
| NFR-016-06 | Performance | Selection MUST complete in < 25ms |
| NFR-016-07 | Performance | Formatting MUST complete in < 50ms |
| NFR-016-08 | Performance | Chunking 1MB file MUST complete in < 200ms |
| NFR-016-09 | Performance | Token cache hit rate SHOULD be > 80% |
| NFR-016-10 | Performance | Memory usage MUST be < 100MB during packing |
| NFR-016-11 | Performance | Packing MUST be parallelizable |
| NFR-016-12 | Performance | Chunking MUST use streaming for large files |
| NFR-016-13 | Performance | Deduplication MUST use O(n log n) algorithm |
| NFR-016-14 | Performance | Selection MUST be O(n) after sorting |
| NFR-016-15 | Performance | Formatting MUST use StringBuilder |
| NFR-016-16 | Performance | Token counter MUST cache by content hash |
| NFR-016-17 | Performance | Cache invalidation MUST be LRU-based |
| NFR-016-18 | Performance | Cache size MUST be configurable |
| NFR-016-19 | Performance | Concurrent packing MUST be supported |
| NFR-016-20 | Performance | No blocking during packing |

### Accuracy (NFR-016-21 to NFR-016-35)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016-21 | Accuracy | Token count MUST be within 1% of actual |
| NFR-016-22 | Accuracy | Token count MUST use model-specific tokenizer |
| NFR-016-23 | Accuracy | Packed content MUST NOT exceed budget |
| NFR-016-24 | Accuracy | Budget enforcement MUST have zero tolerance |
| NFR-016-25 | Accuracy | Chunk boundaries MUST be accurate |
| NFR-016-26 | Accuracy | Line numbers MUST be 1-based |
| NFR-016-27 | Accuracy | Line numbers MUST match source file |
| NFR-016-28 | Accuracy | Relevance scores MUST be normalized |
| NFR-016-29 | Accuracy | Ranking MUST be deterministic |
| NFR-016-30 | Accuracy | Same input MUST produce same output |
| NFR-016-31 | Accuracy | Deduplication MUST detect all exact duplicates |
| NFR-016-32 | Accuracy | Overlap detection MUST be precise |
| NFR-016-33 | Accuracy | Format overhead MUST be counted |
| NFR-016-34 | Accuracy | Header overhead MUST be counted |
| NFR-016-35 | Accuracy | Separator overhead MUST be counted |

### Reliability (NFR-016-36 to NFR-016-45)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016-36 | Reliability | Empty input MUST return empty context |
| NFR-016-37 | Reliability | Single chunk MUST work |
| NFR-016-38 | Reliability | All duplicates MUST work |
| NFR-016-39 | Reliability | Zero budget MUST return empty |
| NFR-016-40 | Reliability | Cancellation MUST be immediate |
| NFR-016-41 | Reliability | Source errors MUST be handled |
| NFR-016-42 | Reliability | Partial sources MUST produce partial result |
| NFR-016-43 | Reliability | Corrupt content MUST be skipped |
| NFR-016-44 | Reliability | Binary content MUST be skipped |
| NFR-016-45 | Reliability | Unicode content MUST be handled |

### Maintainability (NFR-016-46 to NFR-016-55)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016-46 | Maintainability | All interfaces MUST have XML docs |
| NFR-016-47 | Maintainability | Code coverage MUST be > 80% |
| NFR-016-48 | Maintainability | Cyclomatic complexity MUST be < 10 |
| NFR-016-49 | Maintainability | Single responsibility per component |
| NFR-016-50 | Maintainability | Dependencies MUST be injected |
| NFR-016-51 | Maintainability | Configuration MUST be documented |
| NFR-016-52 | Maintainability | Ranking formula MUST be documented |
| NFR-016-53 | Maintainability | Format template MUST be documented |
| NFR-016-54 | Maintainability | Error codes MUST be documented |
| NFR-016-55 | Maintainability | Extension points MUST be clear |

### Observability (NFR-016-56 to NFR-016-65)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016-56 | Observability | Packing operations MUST log at Debug |
| NFR-016-57 | Observability | Errors MUST log at Error with context |
| NFR-016-58 | Observability | Metrics MUST track pack latency |
| NFR-016-59 | Observability | Metrics MUST track chunk counts |
| NFR-016-60 | Observability | Metrics MUST track token usage |
| NFR-016-61 | Observability | Metrics MUST track budget utilization |
| NFR-016-62 | Observability | Metrics MUST track dedup effectiveness |
| NFR-016-63 | Observability | Metrics MUST track cache hit rate |
| NFR-016-64 | Observability | Structured logging MUST be used |
| NFR-016-65 | Observability | Correlation IDs MUST be propagated |

---

## User Manual Documentation

### Overview

The Context Packer assembles prompts for the LLM. It selects relevant content, chunks it appropriately, ranks by importance, and fits within token limits.

### How It Works

```
Sources → Chunking → Ranking → Budgeting → Selection → Formatting → Context
```

1. **Sources**: Gather candidates from search, files, tools
2. **Chunking**: Break large files into meaningful pieces
3. **Ranking**: Order by relevance to current task
4. **Budgeting**: Calculate available tokens
5. **Selection**: Pick top-ranked to fill budget
6. **Formatting**: Structure for LLM consumption

### Configuration

```yaml
# .agent/config.yml
context:
  # Token budgets
  budget:
    total: 100000              # Total context window
    response_reserve: 8000     # Reserve for response
    system_reserve: 2000       # Reserve for system prompt
    
  # Source priorities (higher = more important)
  source_priority:
    tool_results: 100
    open_files: 80
    search_results: 60
    references: 40
    
  # Ranking weights
  ranking:
    relevance_weight: 0.5
    recency_weight: 0.3
    source_weight: 0.2
    
  # Chunking
  chunking:
    max_chunk_tokens: 2000
    min_chunk_tokens: 100
    prefer_structural: true
```

### Context Structure

The packed context looks like:

```
## File: src/Services/UserService.cs (lines 1-50)
```csharp
using System;
using Microsoft.Extensions.Logging;

namespace MyApp.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly ILogger<UserService> _logger;
        
        public UserService(IUserRepository repository, ILogger<UserService> logger)
        {
            _repository = repository;
            _logger = logger;
        }
        // ... (lines 16-50)
    }
}
```

## File: src/Interfaces/IUserService.cs (lines 1-20)
```csharp
namespace MyApp.Services
{
    public interface IUserService
    {
        Task<User> GetByIdAsync(int id);
        Task<User> CreateAsync(CreateUserRequest request);
    }
}
```
```

### Debugging Context

```bash
$ acode context show

Context Summary
────────────────────
Total budget: 100,000 tokens
Used: 45,234 tokens (45%)

Sources:
  Tool results: 15,234 tokens (5 items)
  Open files: 20,000 tokens (3 files)
  Search results: 10,000 tokens (10 chunks)

Chunks:
  1. UserService.cs:1-50 (2,000 tokens) [tool]
  2. IUserService.cs:1-20 (500 tokens) [search]
  3. UserController.cs:15-80 (1,500 tokens) [open]
  ...
```

### Troubleshooting

#### Context Too Small

**Problem:** Important code not included

**Solutions:**
1. Increase total budget
2. Adjust source priorities
3. Improve search queries

#### Context Overflow

**Problem:** Budget exceeded

**Solutions:**
1. This shouldn't happen (bug)
2. Check token counting
3. Reduce chunk sizes

#### Poor Relevance

**Problem:** Irrelevant code included

**Solutions:**
1. Adjust ranking weights
2. Improve chunking strategy
3. Better search queries

---

## Acceptance Criteria

### Sources

- [ ] AC-001: Search results collected
- [ ] AC-002: Open files collected
- [ ] AC-003: Tool results collected
- [ ] AC-004: Priority applied

### Chunking

- [ ] AC-005: Files chunked
- [ ] AC-006: Meaningful units
- [ ] AC-007: Size limits respected

### Ranking

- [ ] AC-008: Relevance ranked
- [ ] AC-009: Sources weighted
- [ ] AC-010: Combined scoring

### Budgeting

- [ ] AC-011: Accurate count
- [ ] AC-012: Budget enforced
- [ ] AC-013: Reserves respected

### Deduplication

- [ ] AC-014: Duplicates removed
- [ ] AC-015: Best version kept
- [ ] AC-016: Overlap handled

### Formatting

- [ ] AC-017: Headers added
- [ ] AC-018: Line numbers shown
- [ ] AC-019: Consistent format

---

## Best Practices

### Context Selection

1. **Relevance over recency** - Prioritize content relevant to task over most recent
2. **Include dependencies** - Add imports, type definitions for selected code
3. **Preserve logical units** - Don't split functions or classes mid-definition
4. **Add sufficient context** - Include surrounding code for understanding

### Token Management

5. **Count tokens accurately** - Use proper tokenizer for target model
6. **Leave headroom** - Reserve tokens for response in budget
7. **Prioritize ruthlessly** - Better to include less with more context
8. **Report budget usage** - Show how tokens were allocated

### Output Formatting

9. **Add clear boundaries** - Mark start/end of each code section
10. **Include file paths** - Always show source file and line numbers
11. **Consistent structure** - Same format across all context types
12. **Avoid duplication** - Same content should appear only once

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Context/
├── ChunkerTests.cs
│   ├── Should_Chunk_By_Function_Boundaries()
│   ├── Should_Chunk_By_Class_Boundaries()
│   ├── Should_Chunk_By_Line_Count()
│   ├── Should_Chunk_By_Token_Count()
│   ├── Should_Respect_Max_Chunk_Size()
│   ├── Should_Respect_Min_Chunk_Size()
│   ├── Should_Handle_Empty_File()
│   ├── Should_Handle_Single_Line_File()
│   ├── Should_Handle_Oversized_Function()
│   ├── Should_Preserve_Complete_Functions()
│   ├── Should_Preserve_Complete_Classes()
│   ├── Should_Handle_Nested_Structures()
│   ├── Should_Handle_Anonymous_Functions()
│   └── Should_Handle_Markdown_Sections()
│
├── RankerTests.cs
│   ├── Should_Rank_By_Relevance_Score()
│   ├── Should_Rank_By_Recency()
│   ├── Should_Rank_By_Source_Priority()
│   ├── Should_Combine_Scores_Weighted()
│   ├── Should_Apply_Custom_Weights()
│   ├── Should_Handle_Equal_Scores()
│   ├── Should_Handle_Zero_Relevance()
│   ├── Should_Handle_Missing_Recency()
│   ├── Should_Normalize_Scores()
│   └── Should_Produce_Stable_Ordering()
│
├── TokenCounterTests.cs
│   ├── Should_Count_Tokens_Accurately()
│   ├── Should_Handle_Code_Identifiers()
│   ├── Should_Handle_Unicode()
│   ├── Should_Handle_Whitespace()
│   ├── Should_Handle_Empty_String()
│   ├── Should_Match_Model_Tokenizer()
│   └── Should_Cache_Token_Counts()
│
├── BudgetManagerTests.cs
│   ├── Should_Enforce_Total_Budget()
│   ├── Should_Reserve_Response_Space()
│   ├── Should_Reserve_System_Prompt_Space()
│   ├── Should_Calculate_Available_Budget()
│   ├── Should_Track_Allocated_Tokens()
│   ├── Should_Handle_Zero_Budget()
│   ├── Should_Handle_Negative_Reserve()
│   ├── Should_Allocate_By_Category()
│   └── Should_Report_Remaining_Budget()
│
├── DeduplicatorTests.cs
│   ├── Should_Detect_Exact_Duplicate_Chunks()
│   ├── Should_Detect_Overlapping_Chunks()
│   ├── Should_Keep_Highest_Ranked_Duplicate()
│   ├── Should_Merge_Overlapping_Same_File()
│   ├── Should_Handle_No_Duplicates()
│   ├── Should_Handle_All_Duplicates()
│   ├── Should_Handle_Partial_Overlap()
│   ├── Should_Preserve_Order_After_Dedup()
│   └── Should_Handle_Different_Sources_Same_Content()
│
├── SelectorTests.cs
│   ├── Should_Select_Top_Ranked_Chunks()
│   ├── Should_Fill_To_Budget()
│   ├── Should_Not_Exceed_Budget()
│   ├── Should_Handle_Chunk_Larger_Than_Remaining()
│   ├── Should_Balance_Multiple_Sources()
│   ├── Should_Handle_Empty_Candidates()
│   ├── Should_Handle_Single_Candidate()
│   └── Should_Handle_All_Candidates_Fit()
│
├── FormatterTests.cs
│   ├── Should_Add_File_Path_Header()
│   ├── Should_Add_Line_Number_Range()
│   ├── Should_Add_Language_Hint()
│   ├── Should_Add_Separator_Between_Chunks()
│   ├── Should_Format_Code_Blocks()
│   ├── Should_Handle_Multiple_Chunks_Same_File()
│   ├── Should_Handle_Multiple_Files()
│   ├── Should_Escape_Special_Characters()
│   └── Should_Produce_Consistent_Format()
│
└── ContextPackerTests.cs
    ├── Should_Orchestrate_Full_Pipeline()
    ├── Should_Handle_Empty_Sources()
    ├── Should_Handle_Single_Source()
    ├── Should_Handle_Multiple_Sources()
    ├── Should_Return_Token_Count()
    ├── Should_Return_Included_Chunks()
    ├── Should_Support_Cancellation()
    └── Should_Handle_Configuration_Changes()
```

### Integration Tests

```
Tests/Integration/Context/
├── ContextPackerIntegrationTests.cs
│   ├── Should_Pack_Real_Search_Results()
│   ├── Should_Pack_Open_Files()
│   ├── Should_Pack_Tool_Results()
│   ├── Should_Pack_Mixed_Sources()
│   ├── Should_Handle_Large_Codebase()
│   ├── Should_Handle_Many_Small_Files()
│   └── Should_Handle_Few_Large_Files()
│
└── TokenizerIntegrationTests.cs
    ├── Should_Match_GPT4_Tokenizer()
    ├── Should_Match_Claude_Tokenizer()
    └── Should_Handle_Edge_Cases()
```

### E2E Tests

```
Tests/E2E/Context/
├── ContextE2ETests.cs
│   ├── Should_Provide_Context_To_Planner()
│   ├── Should_Provide_Context_To_Executor()
│   ├── Should_Update_Context_Between_Turns()
│   ├── Should_Show_Context_Debug_Command()
│   └── Should_Respect_Config_Settings()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Pack context | 250ms | 500ms |
| Token count | 25ms | 50ms |
| Ranking | 50ms | 100ms |

---

## User Verification Steps

### Scenario 1: Basic Packing

1. Search for code
2. Pack context
3. Verify: Results included

### Scenario 2: Budget Limit

1. Many sources
2. Pack with small budget
3. Verify: Top-ranked included

### Scenario 3: Deduplication

1. Same file from multiple sources
2. Pack context
3. Verify: Included once

### Scenario 4: Formatting

1. Pack context
2. Examine output
3. Verify: Clear structure

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Context/
│   ├── IContextPacker.cs
│   ├── ContextSource.cs
│   ├── ContextChunk.cs
│   └── PackedContext.cs
│
src/AgenticCoder.Infrastructure/
├── Context/
│   ├── ContextPacker.cs
│   ├── Chunker.cs
│   ├── Ranker.cs
│   ├── BudgetManager.cs
│   ├── Deduplicator.cs
│   └── Formatter.cs
```

### IContextPacker Interface

```csharp
namespace AgenticCoder.Domain.Context;

public interface IContextPacker
{
    Task<PackedContext> PackAsync(
        IReadOnlyList<ContextSource> sources,
        ContextBudget budget,
        CancellationToken ct);
}

public sealed record PackedContext(
    string Content,
    int TokenCount,
    IReadOnlyList<ContextChunk> Chunks);
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-CTX-001 | Packing failed |
| ACODE-CTX-002 | Token count error |
| ACODE-CTX-003 | Budget exceeded |
| ACODE-CTX-004 | No content |

### Implementation Checklist

1. [ ] Create context interfaces
2. [ ] Implement chunker
3. [ ] Implement ranker
4. [ ] Implement budget manager
5. [ ] Implement deduplicator
6. [ ] Implement formatter
7. [ ] Create packer orchestrator
8. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Chunking
2. **Phase 2:** Ranking
3. **Phase 3:** Budgeting
4. **Phase 4:** Deduplication
5. **Phase 5:** Formatting

---

**End of Task 016 Specification**