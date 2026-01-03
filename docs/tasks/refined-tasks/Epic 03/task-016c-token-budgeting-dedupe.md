# Task 016.c: Token Budgeting + Dedupe

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 016 (Context Packer), Task 016.a (Chunking), Task 016.b (Ranking)  

---

## Description

Task 016.c implements token budgeting and deduplication. Token budgeting ensures context fits the LLM window. Deduplication removes redundant content.

Token budgeting is essential. LLMs have fixed context windows. Exceeding the window causes errors. Budget management prevents this.

Accurate token counting is critical. Different tokenizers count differently. Use the appropriate tokenizer for the target model. Approximate counting is insufficient.

Budget allocation is strategic. Reserve space for system prompts. Reserve space for responses. Allocate remaining to context.

Category budgets distribute tokens. Tool results get a share. Open files get a share. Search results get a share. Configurable ratios.

Deduplication saves tokens. The same code might appear from multiple sources. Include it once. Use saved tokens for other content.

Exact deduplication catches identical chunks. Same file, same lines, same content. Easy to detect. Always remove duplicates.

Overlap deduplication handles partial matches. Two chunks might share lines. Merge them or keep the better one.

Token counting is expensive. Cache counts when possible. Reuse cached counts. Invalidate on content change.

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

### Token Counting

- FR-001: Count tokens accurately
- FR-002: Support tiktoken
- FR-003: Support model-specific tokenizers
- FR-004: Cache token counts
- FR-005: Handle unicode correctly

### Budget Management

- FR-006: Track total budget
- FR-007: Reserve for system prompt
- FR-008: Reserve for response
- FR-009: Calculate available budget
- FR-010: Configurable reserves

### Category Allocation

- FR-011: Allocate by category
- FR-012: Configurable ratios
- FR-013: Track category usage
- FR-014: Rebalance if under-used
- FR-015: Report allocation

### Budget Enforcement

- FR-016: Enforce total limit
- FR-017: Enforce category limits
- FR-018: Handle overflow
- FR-019: Truncate if needed
- FR-020: Report overflow

### Exact Deduplication

- FR-021: Detect identical chunks
- FR-022: Keep highest-ranked
- FR-023: Remove duplicates
- FR-024: Track dedup savings

### Overlap Deduplication

- FR-025: Detect overlapping chunks
- FR-026: Calculate overlap amount
- FR-027: Merge or keep best
- FR-028: Configurable threshold

### Selection

- FR-029: Select to fill budget
- FR-030: Prioritize by rank
- FR-031: Respect category limits
- FR-032: Optimize filling

### Reporting

- FR-033: Report token usage
- FR-034: Report dedup savings
- FR-035: Report category breakdown
- FR-036: Report overflow warnings

---

## Non-Functional Requirements

### Performance

- NFR-001: Token count < 10ms/1000 tokens
- NFR-002: Dedup check < 20ms
- NFR-003: Selection < 50ms

### Accuracy

- NFR-004: Count within 0.5%
- NFR-005: No budget overflow
- NFR-006: Correct dedup

### Reliability

- NFR-007: Handle edge cases
- NFR-008: Graceful fallback
- NFR-009: No data loss

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

## Testing Requirements

### Unit Tests

```
Tests/Unit/Context/Budget/
├── TokenCounterTests.cs
│   ├── Should_Count_Accurately()
│   ├── Should_Handle_Unicode()
│   └── Should_Cache_Counts()
│
├── BudgetManagerTests.cs
│   ├── Should_Calculate_Available()
│   ├── Should_Allocate_Categories()
│   └── Should_Enforce_Limits()
│
├── ExactDedupTests.cs
│   ├── Should_Detect_Duplicates()
│   └── Should_Keep_Best()
│
├── OverlapDedupTests.cs
│   ├── Should_Detect_Overlap()
│   ├── Should_Calculate_Amount()
│   └── Should_Merge()
│
└── SelectorTests.cs
    ├── Should_Fill_Budget()
    └── Should_Respect_Categories()
```

### Integration Tests

```
Tests/Integration/Context/Budget/
├── BudgetIntegrationTests.cs
│   └── Should_Manage_Real_Context()
```

### E2E Tests

```
Tests/E2E/Context/Budget/
├── BudgetE2ETests.cs
│   └── Should_Budget_For_Agent()
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