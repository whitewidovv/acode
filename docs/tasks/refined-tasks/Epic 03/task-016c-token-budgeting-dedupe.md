# Task 016.c: Token Budgeting + Dedupe

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 016 (Context Packer), Task 016.a (Chunking), Task 016.b (Ranking)  

---

## Description

### Business Value

Token budgeting is the gatekeeper that ensures the agent's context never exceeds LLM limits while maximizing the value of every token spent. Without proper budgeting, requests fail with context overflow errors—a catastrophic user experience. Task 016.c delivers the precise token management and deduplication logic that makes context assembly reliable and efficient.

Every token matters in a fixed context window. Wasting tokens on duplicate content—the same function included from multiple search results—directly reduces the unique information available to the LLM. Deduplication reclaims these wasted tokens, often recovering 10-30% of context capacity in typical codebases where related searches frequently overlap.

Accurate token counting is non-negotiable. Approximate counting leads to either wasted capacity (conservative estimates) or runtime failures (optimistic estimates). By using the actual tokenizer for the target model, this system provides exact counts that enable precise budget management. Combined with category-based allocation, teams can tune how context space is divided between tool results, open files, and search results.

### Scope

This task defines the complete token budgeting and deduplication subsystem:

1. **Token Counter:** Accurate token counting using model-specific tokenizers (tiktoken for GPT models, claude tokenizer for Anthropic).

2. **Budget Manager:** Tracks total budget, reserves space for system prompts and responses, and enforces limits.

3. **Category Allocator:** Divides available budget across content categories (tool results, open files, search results, references) based on configurable ratios.

4. **Exact Deduplication:** Detects and removes identical chunks that appear from multiple sources.

5. **Overlap Deduplication:** Detects and merges chunks with significant line overlap to eliminate redundancy.

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Task 016 (Context Packer) | Parent System | Budget manager is invoked by Context Packer for selection |
| Task 016.a (Chunking) | Upstream | Receives chunks with token estimates |
| Task 016.b (Ranking) | Upstream | Receives ranked chunks for budget-constrained selection |
| Task 002 (Config) | Configuration | Budget settings from `.agent/config.yml` |
| Task 004 (Model Provider) | Model Info | Target model context window size |
| Task 011 (Session) | System Prompt | System prompt token count for reservation |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Tokenizer initialization fails | Cannot count tokens | Fallback to approximate counting (4 chars = 1 token) |
| Token count exceeds budget | Selection fails | Truncate lowest-ranked chunks until budget met |
| Category allocation exceeds 100% | Invalid configuration | Normalize allocations, log warning |
| Deduplication false positive | Unique content removed | Conservative overlap threshold (default 80%) |
| Merge creates oversized chunk | Merged chunk exceeds limit | Re-split after merge if necessary |
| Cache invalidation missed | Stale token counts | Timestamp-based cache invalidation |
| Unicode counting error | Incorrect token estimate | Proper UTF-8 handling in tokenizer |
| Empty result after budgeting | No context for LLM | Minimum content guarantee, warning to user |

### Assumptions

1. The target model's tokenizer is available (tiktoken for OpenAI, etc.)
2. Token counting is deterministic for the same content
3. System prompt and expected response size are known at selection time
4. Category allocations are configured and sum to 100%
5. Chunks have accurate line number metadata for overlap detection
6. Token count caching is beneficial due to repeated content
7. Deduplication is preferred over including duplicates
8. Budget enforcement is strict—no context overflow allowed

### Security Considerations

1. **No Arbitrary Code in Tokenizer:** Tokenizer must be a trusted library, not executing arbitrary content.

2. **Memory Limits on Counting:** Token counting must handle large content without memory exhaustion.

3. **Cache Integrity:** Token count cache must not be manipulable to cause budget overflow.

4. **Audit Trail:** Budget decisions (what was included/excluded) must be logged for debugging.

5. **Deterministic Selection:** Budget selection must be deterministic for reproducibility and auditing.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Token | LLM text unit |
| Budget | Allowed tokens |
| Tokenizer | Token counter |
| Window | Context limit |
| Reserve | Set aside tokens |
| Allocation | Budget division |
| Category | Content type |
| Deduplication | Remove duplicates |
| Exact Match | Identical content |
| Overlap | Shared content |
| Merge | Combine chunks |
| Cache | Store for reuse |
| Estimate | Approximate count |
| Precise | Exact count |
| Overflow | Exceeds budget |

---

## Out of Scope

The following items are explicitly excluded from Task 016.c:

- **Dynamic budgets** - Fixed allocations
- **Multi-model support** - Single model v1
- **Compression** - No summarization
- **Streaming counting** - Batch counting
- **Semantic dedup** - Exact/overlap only

---

## Functional Requirements

### Token Counting (FR-016c-01 to FR-016c-05)

| ID | Requirement |
|----|-------------|
| FR-016c-01 | System MUST count tokens accurately using model-specific tokenizer |
| FR-016c-02 | System MUST support tiktoken for OpenAI models |
| FR-016c-03 | System MUST support model-specific tokenizers for other providers |
| FR-016c-04 | System MUST cache token counts for repeated content |
| FR-016c-05 | System MUST handle unicode content correctly |

### Budget Management (FR-016c-06 to FR-016c-10)

| ID | Requirement |
|----|-------------|
| FR-016c-06 | System MUST track total context window budget |
| FR-016c-07 | System MUST reserve tokens for system prompt |
| FR-016c-08 | System MUST reserve tokens for expected response |
| FR-016c-09 | System MUST calculate available budget after reserves |
| FR-016c-10 | Reserve amounts MUST be configurable |

### Category Allocation (FR-016c-11 to FR-016c-15)

| ID | Requirement |
|----|-------------|
| FR-016c-11 | System MUST allocate available budget by content category |
| FR-016c-12 | Category allocation ratios MUST be configurable |
| FR-016c-13 | System MUST track token usage per category |
| FR-016c-14 | System MUST redistribute unused category allocation |
| FR-016c-15 | System MUST report allocation breakdown |

### Budget Enforcement (FR-016c-16 to FR-016c-20)

| ID | Requirement |
|----|-------------|
| FR-016c-16 | System MUST enforce total context budget limit |
| FR-016c-17 | System MUST enforce per-category budget limits |
| FR-016c-18 | System MUST handle budget overflow gracefully |
| FR-016c-19 | System MUST truncate content to fit budget when necessary |
| FR-016c-20 | System MUST report when content is truncated due to budget |

### Exact Deduplication (FR-016c-21 to FR-016c-24)

| ID | Requirement |
|----|-------------|
| FR-016c-21 | System MUST detect identical chunks by content hash |
| FR-016c-22 | System MUST keep highest-ranked instance of duplicate chunks |
| FR-016c-23 | System MUST remove lower-ranked duplicate chunks |
| FR-016c-24 | System MUST track and report deduplication savings |

### Overlap Deduplication (FR-016c-25 to FR-016c-28)

| ID | Requirement |
|----|-------------|
| FR-016c-25 | System MUST detect chunks with overlapping line ranges |
| FR-016c-26 | System MUST calculate overlap percentage between chunks |
| FR-016c-27 | System MUST merge or deduplicate chunks exceeding overlap threshold |
| FR-016c-28 | Overlap threshold MUST be configurable (default: 80%) |

### Selection (FR-016c-29 to FR-016c-32)

| ID | Requirement |
|----|-------------|
| FR-016c-29 | System MUST select chunks to fill available budget |
| FR-016c-30 | System MUST prioritize selection by ranking score |
| FR-016c-31 | System MUST respect category limits during selection |
| FR-016c-32 | System MUST optimize budget utilization |

### Reporting (FR-016c-33 to FR-016c-36)

| ID | Requirement |
|----|-------------|
| FR-016c-33 | System MUST report total token usage |
| FR-016c-34 | System MUST report deduplication savings in tokens |
| FR-016c-35 | System MUST report category-wise token breakdown |
| FR-016c-36 | System MUST report overflow warnings when budget exceeded |

---

## Non-Functional Requirements

### Performance (NFR-016c-01 to NFR-016c-03)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016c-01 | Performance | Token counting MUST complete in less than 10ms per 1000 tokens |
| NFR-016c-02 | Performance | Deduplication check MUST complete in less than 20ms for 100 chunks |
| NFR-016c-03 | Performance | Budget selection MUST complete in less than 50ms |

### Accuracy (NFR-016c-04 to NFR-016c-06)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016c-04 | Accuracy | Token counts MUST be within 0.5% of actual model tokenization |
| NFR-016c-05 | Accuracy | Selected content MUST NOT exceed total budget |
| NFR-016c-06 | Accuracy | Deduplication MUST correctly identify duplicate content |

### Reliability (NFR-016c-07 to NFR-016c-09)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016c-07 | Reliability | System MUST handle edge cases (empty input, single chunk) |
| NFR-016c-08 | Reliability | System MUST fall back gracefully when tokenizer unavailable |
| NFR-016c-09 | Reliability | No content data MUST be lost during budgeting operations |

---

## User Manual Documentation

### Overview

Token budgeting ensures context fits the LLM. Deduplication removes redundant content to maximize unique information.

### Configuration

```yaml
# .agent/config.yml
context:
  budget:
    # Total context window
    total_tokens: 100000
    
    # Reserved tokens
    system_prompt_reserve: 2000
    response_reserve: 8000
    
    # Category allocations (% of available)
    categories:
      tool_results: 40
      open_files: 30
      search_results: 20
      references: 10
      
  dedup:
    # Enable deduplication
    enabled: true
    
    # Overlap threshold (0-1, 0.8 = 80% overlap)
    overlap_threshold: 0.8
    
    # Merge overlapping chunks
    merge_overlapping: true
    
  tokenizer:
    # Tokenizer to use
    type: tiktoken
    model: gpt-4
```

### Budget Breakdown

```
Total Context Window: 100,000 tokens
├── System Prompt Reserve: 2,000 tokens
├── Response Reserve: 8,000 tokens
└── Available for Context: 90,000 tokens
    ├── Tool Results (40%): 36,000 tokens
    ├── Open Files (30%): 27,000 tokens
    ├── Search Results (20%): 18,000 tokens
    └── References (10%): 9,000 tokens
```

### Deduplication Example

Before dedup:
```
Chunk A (tool): UserService.cs:1-50 (1000 tokens)
Chunk B (search): UserService.cs:25-75 (1000 tokens)  ← Overlaps with A
Chunk C (open): UserService.cs:1-50 (1000 tokens)    ← Duplicate of A
Total: 3000 tokens
```

After dedup:
```
Chunk A (tool): UserService.cs:1-75 (1500 tokens)  ← Merged A+B
Chunk C removed as duplicate
Savings: 1500 tokens
```

### Token Usage Report

```bash
$ acode context report

Token Budget Report
────────────────────
Total Budget: 100,000
Available: 90,000

Category Usage:
  Tool Results:    12,500 / 36,000 (35%)
  Open Files:      18,200 / 27,000 (67%)
  Search Results:  15,800 / 18,000 (88%)
  References:       5,200 /  9,000 (58%)

Total Used: 51,700 / 90,000 (57%)

Deduplication:
  Duplicates removed: 3
  Overlaps merged: 2
  Tokens saved: 4,200
```

### Troubleshooting

#### Budget Overflow

**Problem:** Content exceeds budget

**Solutions:**
1. Increase total budget (if model allows)
2. Reduce reserves
3. Add more aggressive chunking
4. Lower category allocations

#### Inaccurate Counts

**Problem:** Token counts seem wrong

**Solutions:**
1. Verify tokenizer matches model
2. Clear token count cache
3. Check for unicode issues

#### Missing Content

**Problem:** Important content deduped

**Solutions:**
1. Check if merged correctly
2. Lower overlap threshold
3. Disable dedup temporarily

---

## Acceptance Criteria

### Counting

- [ ] AC-001: Accurate counting
- [ ] AC-002: Caching works
- [ ] AC-003: Unicode handled

### Budgeting

- [ ] AC-004: Reserves applied
- [ ] AC-005: Categories allocated
- [ ] AC-006: Limits enforced

### Deduplication

- [ ] AC-007: Exact dedup works
- [ ] AC-008: Overlap dedup works
- [ ] AC-009: Merge works

### Selection

- [ ] AC-010: Fills to budget
- [ ] AC-011: Respects categories
- [ ] AC-012: Prioritizes rank

### Reporting

- [ ] AC-013: Usage reported
- [ ] AC-014: Savings reported
- [ ] AC-015: Breakdown shown

---

## Best Practices

### Budget Allocation

1. **Prioritize by value** - Allocate more tokens to high-relevance content
2. **Reserve for essentials** - System prompt, conversation history get guaranteed share
3. **Dynamic reallocation** - Adjust budget based on actual content availability
4. **Overflow handling** - Clear strategy when content exceeds budget

### Deduplication

5. **Content hash comparison** - Detect exact duplicates efficiently
6. **Fuzzy matching** - Handle near-duplicates (whitespace, comments differ)
7. **Keep best version** - When deduping, retain the more complete version
8. **Track dedupe savings** - Report how many tokens saved by deduplication

### Reporting

9. **Show allocation** - Display how budget was spent across categories
10. **Highlight cuts** - Indicate what was omitted due to budget constraints
11. **Suggest increases** - Recommend larger context if consistently truncating
12. **Log for analysis** - Record budget decisions for optimization

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Context/Budget/
├── TokenCounterTests.cs
│   ├── Should_Count_Empty_String()
│   ├── Should_Count_Single_Word()
│   ├── Should_Count_Sentence()
│   ├── Should_Count_Paragraph()
│   ├── Should_Count_Code()
│   ├── Should_Handle_Whitespace()
│   ├── Should_Handle_Unicode()
│   ├── Should_Handle_Emojis()
│   ├── Should_Handle_Long_Identifiers()
│   ├── Should_Cache_Repeated_Counts()
│   ├── Should_Count_Batch()
│   └── Should_Match_Model_Tokenizer()
│
├── BudgetManagerTests.cs
│   ├── Should_Calculate_Total_Available()
│   ├── Should_Apply_System_Reserve()
│   ├── Should_Apply_Response_Reserve()
│   ├── Should_Allocate_Categories()
│   ├── Should_Track_Consumption()
│   ├── Should_Enforce_Total_Limit()
│   ├── Should_Enforce_Category_Limit()
│   ├── Should_Report_Remaining()
│   ├── Should_Handle_Zero_Budget()
│   └── Should_Load_Config()
│
├── CategoryAllocatorTests.cs
│   ├── Should_Allocate_By_Percentage()
│   ├── Should_Handle_Fixed_Allocation()
│   ├── Should_Sum_To_Available()
│   ├── Should_Handle_Single_Category()
│   ├── Should_Handle_Empty_Category()
│   └── Should_Redistribute_Unused()
│
├── ExactDedupTests.cs
│   ├── Should_Detect_Exact_Duplicate()
│   ├── Should_Keep_First_Occurrence()
│   ├── Should_Keep_Highest_Ranked()
│   ├── Should_Handle_Different_Sources()
│   ├── Should_Handle_No_Duplicates()
│   ├── Should_Handle_All_Duplicates()
│   └── Should_Report_Removed_Count()
│
├── OverlapDedupTests.cs
│   ├── Should_Detect_Overlap_By_Lines()
│   ├── Should_Calculate_Overlap_Percentage()
│   ├── Should_Merge_Overlapping_Chunks()
│   ├── Should_Handle_Adjacent_Chunks()
│   ├── Should_Handle_Contained_Chunk()
│   ├── Should_Respect_Threshold()
│   ├── Should_Handle_Different_Files()
│   ├── Should_Handle_No_Overlap()
│   └── Should_Report_Merged_Count()
│
├── SelectorTests.cs
│   ├── Should_Select_By_Rank_Order()
│   ├── Should_Fill_To_Budget()
│   ├── Should_Not_Exceed_Budget()
│   ├── Should_Respect_Category_Limits()
│   ├── Should_Skip_Chunk_If_Too_Large()
│   ├── Should_Balance_Across_Categories()
│   ├── Should_Handle_Empty_Candidates()
│   ├── Should_Handle_Single_Candidate()
│   └── Should_Return_Selected_With_Reasons()
│
└── BudgetReportTests.cs
    ├── Should_Report_Total_Used()
    ├── Should_Report_Category_Breakdown()
    ├── Should_Report_Duplicates_Removed()
    ├── Should_Report_Overlaps_Merged()
    ├── Should_Report_Tokens_Saved()
    └── Should_Format_Percentages()
```

### Integration Tests

```
Tests/Integration/Context/Budget/
├── BudgetIntegrationTests.cs
│   ├── Should_Manage_Real_Context()
│   ├── Should_Handle_Large_Chunk_Set()
│   ├── Should_Work_With_Real_Tokenizer()
│   └── Should_Apply_Config_Settings()
│
└── DedupIntegrationTests.cs
    ├── Should_Dedup_Real_Search_Results()
    └── Should_Merge_Real_Overlapping_Chunks()
```

### E2E Tests

```
Tests/E2E/Context/Budget/
├── BudgetE2ETests.cs
│   ├── Should_Budget_For_Agent_Context()
│   ├── Should_Show_Budget_Report_Via_CLI()
│   └── Should_Respect_Config_Changes()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Count 10K tokens | 5ms | 10ms |
| Dedup 100 chunks | 10ms | 20ms |
| Selection | 25ms | 50ms |

---

## User Verification Steps

### Scenario 1: Budget Enforcement

1. Set small budget
2. Try to pack too much
3. Verify: Trimmed to budget

### Scenario 2: Deduplication

1. Include same file twice
2. Pack context
3. Verify: Included once

### Scenario 3: Category Allocation

1. Set category limits
2. Pack context
3. Verify: Categories respected

### Scenario 4: Reporting

1. Pack context
2. Run report
3. Verify: Accurate stats

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Context/
│   ├── ITokenCounter.cs
│   ├── IBudgetManager.cs
│   └── IDeduplicator.cs
│
src/AgenticCoder.Infrastructure/
├── Context/
│   └── Budget/
│       ├── TiktokenCounter.cs
│       ├── BudgetManager.cs
│       ├── CategoryAllocator.cs
│       ├── ExactDeduplicator.cs
│       ├── OverlapDeduplicator.cs
│       └── BudgetSelector.cs
```

### ITokenCounter Interface

```csharp
namespace AgenticCoder.Domain.Context;

public interface ITokenCounter
{
    int Count(string content);
    int Count(IEnumerable<string> contents);
}
```

### IBudgetManager Interface

```csharp
namespace AgenticCoder.Domain.Context;

public interface IBudgetManager
{
    BudgetAllocation CalculateAllocation(BudgetOptions options);
    bool CanFit(int tokens, string category);
    void Consume(int tokens, string category);
    BudgetReport GetReport();
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-BUD-001 | Budget exceeded |
| ACODE-BUD-002 | Category exceeded |
| ACODE-BUD-003 | Count failed |
| ACODE-BUD-004 | Dedup failed |

### Implementation Checklist

1. [ ] Create token counter
2. [ ] Implement tiktoken integration
3. [ ] Create budget manager
4. [ ] Implement category allocation
5. [ ] Implement exact dedup
6. [ ] Implement overlap dedup
7. [ ] Create selector
8. [ ] Add reporting

### Rollout Plan

1. **Phase 1:** Token counting
2. **Phase 2:** Budget management
3. **Phase 3:** Exact dedup
4. **Phase 4:** Overlap dedup
5. **Phase 5:** Selection and reporting

---

**End of Task 016.c Specification**