# Task 016.c: Token Budgeting + Dedupe

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 016 (Context Packer), Task 016.a (Chunking), Task 016.b (Ranking)  

---

## Description

### Business Value

Token budgeting is the gatekeeper that ensures the agent's context never exceeds LLM limits while maximizing the value of every token spent. Without proper budgeting, requests fail with context overflow errors—a catastrophic user experience that destroys developer trust and forces manual intervention. Task 016.c delivers the precise token management and deduplication logic that makes context assembly reliable, efficient, and predictable.

Every token matters in a fixed context window. Wasting tokens on duplicate content—the same function included from multiple search results—directly reduces the unique information available to the LLM. Deduplication reclaims these wasted tokens, often recovering 10-30% of context capacity in typical codebases where related searches frequently overlap. This recovered capacity translates directly to better LLM responses, as more unique, relevant code context fits within the budget.

Accurate token counting is non-negotiable. Approximate counting leads to either wasted capacity (conservative estimates) or runtime failures (optimistic estimates). By using the actual tokenizer for the target model, this system provides exact counts that enable precise budget management. Combined with category-based allocation, teams can tune how context space is divided between tool results, open files, and search results based on their workflow patterns.

### Return on Investment (ROI)

**Problem Cost (Without Accurate Token Budgeting + Deduplication):**

Without this system, teams face three critical failure modes:

1. **Context Overflow Errors** ($234,000/year for 10 developers):
   - LLM requests fail with "context too long" errors
   - Developer must manually reduce context (identify what to remove)
   - Average recovery time: 5 minutes per occurrence
   - Frequency: 12 occurrences per developer per day (3 per hour during 4-hour coding sessions)
   - Annual time cost: 12 occurrences/day × 5 min × 250 days × 10 devs = 150,000 minutes = 2,500 hours
   - At $150/hour blended rate: **$375,000/year**
   - Frustration and context-switching penalty: -20% productivity during recovery → additional $75,000 lost
   - **Subtotal: $450,000/year** (context overflow)

2. **Duplicate Content Waste** ($108,000/year for 10 developers):
   - 20-30% of context window filled with redundant chunks (same code from different sources)
   - This reduces unique code visible to LLM by 25%
   - LLM generates less accurate responses due to missing context
   - Developer must re-run with better queries or manual context editing
   - Average re-run cost: 3 minutes per occurrence
   - Frequency: 8 re-runs per developer per day
   - Annual time cost: 8 × 3 min × 250 days × 10 devs = 60,000 minutes = 1,000 hours
   - At $150/hour: **$150,000/year**
   - Context quality degradation penalty: additional debugging and rework → $36,000/year
   - **Subtotal: $186,000/year** (duplicate waste)

3. **Inaccurate Token Estimation** ($72,000/year for 10 developers):
   - Approximate counting (4 chars = 1 token) is 10-15% inaccurate
   - Conservative estimates waste 12% of context window capacity
   - Optimistic estimates cause 5% request failure rate
   - Combined impact: 480 hours/year rework per team of 10
   - At $150/hour: **$72,000/year** (estimation errors)

**Total Annual Cost Without This System: $708,000/year** (for 10 developers)

**Benefit (With Accurate Token Budgeting + Deduplication):**

With Task 016.c implemented:

1. **Zero Context Overflow Errors**:
   - Budget manager enforces strict limits with deterministic selection
   - Savings: $450,000/year (100% of overflow cost eliminated)

2. **10-30% Token Reclamation from Deduplication**:
   - Average 22% token savings from exact + overlap deduplication
   - Translates to 22% more unique code context per request
   - Reduces re-run frequency from 8/day to 2/day (75% reduction)
   - Savings: $139,500/year (75% of duplicate waste eliminated)

3. **Exact Token Counting (<0.5% error)**:
   - Model-specific tokenizer (tiktoken) provides precise counts
   - Eliminates wasted capacity and unexpected failures
   - Savings: $72,000/year (100% of estimation errors eliminated)

**Total Annual Savings: $661,500/year** (for 10 developers)

**Implementation Cost:**
- Development: 40 hours × $150/hour = $6,000
- Testing and integration: 20 hours × $150/hour = $3,000
- Documentation and rollout: 10 hours × $100/hour = $1,000
- **Total Implementation Cost: $10,000** (one-time)

**ROI Calculation:**
- Annual Savings: $661,500
- Implementation Cost: $10,000
- **Net Annual Benefit:** $651,500
- **ROI:** 6,515%
- **Payback Period:** 5.5 days

**Per-Developer Impact:**
- Annual savings per developer: $66,150
- Frustration reduction: Eliminates 3,000 context overflow interruptions per developer per year
- Cognitive load reduction: No manual context management decisions (automated and reliable)

### Before/After Metrics

| Metric | Before (No Budgeting) | After (Task 016.c) | Improvement |
|--------|----------------------|-------------------|-------------|
| Context overflow errors per day | 12 per developer | 0 | 100% elimination |
| Duplicate content in context | 20-30% average | 0-3% average | 92% reduction |
| Token estimation error rate | 10-15% | <0.5% | 95% reduction |
| Manual context editing time | 60 min/day per dev | 0 min/day | 100% elimination |
| LLM re-runs due to poor context | 8 per day | 2 per day | 75% reduction |
| Context utilization efficiency | 70% (wasted on duplicates + conservative estimates) | 97% (tight packing) | 39% improvement |
| Average context assembly time | 150ms (with retries) | 50ms (single pass) | 67% faster |

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                  TOKEN BUDGETING + DEDUPLICATION PIPELINE               │
└─────────────────────────────────────────────────────────────────────────┘

INPUT: Ranked Chunks (from Task 016b) + Model Info + Budget Config
│
▼
┌─────────────────────────────────────────────────────────────────────────┐
│  STAGE 1: TOKEN COUNTING                                                │
├─────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────────┐   ┌──────────────────┐   ┌──────────────────┐   │
│  │ TiktokenCounter  │   │  Cache Lookup    │   │  Batch Counting  │   │
│  │ (GPT-4, GPT-3.5) │   │  (by content     │   │  (parallel for   │   │
│  │                  │   │   hash)          │   │   large sets)    │   │
│  └──────────────────┘   └──────────────────┘   └──────────────────┘   │
│  Output: Each chunk annotated with exact token count                    │
└─────────────────────────────────────────────────────────────────────────┘
│
▼
┌─────────────────────────────────────────────────────────────────────────┐
│  STAGE 2: BUDGET CALCULATION                                            │
├─────────────────────────────────────────────────────────────────────────┤
│  Total Context Window: 100,000 tokens                                   │
│  ├─ System Prompt Reserve: 2,000 tokens (configured)                    │
│  ├─ Response Reserve: 8,000 tokens (configured)                         │
│  └─ Available for Content: 90,000 tokens                                │
│      ├─ Tool Results (40%):    36,000 tokens                            │
│      ├─ Open Files (30%):      27,000 tokens                            │
│      ├─ Search Results (20%):  18,000 tokens                            │
│      └─ References (10%):       9,000 tokens                            │
└─────────────────────────────────────────────────────────────────────────┘
│
▼
┌─────────────────────────────────────────────────────────────────────────┐
│  STAGE 3: EXACT DEDUPLICATION                                           │
├─────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────────────────┐      │
│  │ Content Hash (SHA-256) → Detect Identical Chunks             │      │
│  │ Keep: Highest-ranked instance (from Task 016b ranking)       │      │
│  │ Remove: All lower-ranked duplicates                          │      │
│  └──────────────────────────────────────────────────────────────┘      │
│  Example: UserService.cs:1-50 appears from tool result (rank 0.95)      │
│           AND search result (rank 0.72) → Keep tool result only         │
│  Savings: ~15% token reduction (typical)                                │
└─────────────────────────────────────────────────────────────────────────┘
│
▼
┌─────────────────────────────────────────────────────────────────────────┐
│  STAGE 4: OVERLAP DEDUPLICATION + MERGE                                 │
├─────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────────────────┐      │
│  │ Line Range Overlap Detection (same file path)               │      │
│  │ Calculate Overlap %: (overlap_lines / min_chunk_lines) × 100│      │
│  │ If overlap >= 80% (configurable): Merge chunks              │      │
│  │ Merged chunk: min(line_start) to max(line_end)              │      │
│  │ Keep highest rank, recalculate token count                  │      │
│  └──────────────────────────────────────────────────────────────┘      │
│  Example: UserService.cs:1-50 (tool) overlaps UserService.cs:25-75     │
│           (search) → Merge to UserService.cs:1-75                       │
│  Savings: Additional ~8% token reduction (typical)                      │
└─────────────────────────────────────────────────────────────────────────┘
│
▼
┌─────────────────────────────────────────────────────────────────────────┐
│  STAGE 5: BUDGET-CONSTRAINED SELECTION                                  │
├─────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────────────────┐      │
│  │ For each category (tool_results, open_files, etc.):         │      │
│  │   1. Sort chunks by rank (descending)                        │      │
│  │   2. Select chunks until category budget exhausted           │      │
│  │   3. Skip chunks that exceed remaining category budget       │      │
│  │   4. Track: selected_tokens, skipped_tokens, skipped_count   │      │
│  └──────────────────────────────────────────────────────────────┘      │
│  Optimization: Redistribute unused category allocations (if enabled)    │
│               e.g., tool_results used 20K/36K → redistribute 16K        │
└─────────────────────────────────────────────────────────────────────────┘
│
▼
┌─────────────────────────────────────────────────────────────────────────┐
│  STAGE 6: FINAL VALIDATION + REPORTING                                  │
├─────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────────────────┐      │
│  │ Assert: total_selected_tokens <= available_budget            │      │
│  │ Assert: total_selected_tokens + reserves <= context_window   │      │
│  │ Generate BudgetReport:                                       │      │
│  │   - Category breakdown (used/allocated/percentage)           │      │
│  │   - Deduplication stats (exact_removed, overlaps_merged)     │      │
│  │   - Tokens saved (dedup_savings, truncation_savings)         │      │
│  │   - Warnings (if content truncated)                          │      │
│  └──────────────────────────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────────────────────────┘
│
▼
OUTPUT: Selected Chunks (within budget) + BudgetReport (for debugging/audit)
```

### Scope

This task defines the complete token budgeting and deduplication subsystem:

1. **Token Counter:** Accurate token counting using model-specific tokenizers (tiktoken for GPT models, claude tokenizer for Anthropic).

2. **Budget Manager:** Tracks total budget, reserves space for system prompts and responses, and enforces limits.

3. **Category Allocator:** Divides available budget across content categories (tool results, open files, search results, references) based on configurable ratios.

4. **Exact Deduplication:** Detects and removes identical chunks that appear from multiple sources.

5. **Overlap Deduplication:** Detects and merges chunks with significant line overlap to eliminate redundancy.

### Architectural Decisions

**Decision 1: Exact Tokenizer vs Approximation Formula**

**Chosen:** Use model-specific tokenizer (tiktoken for OpenAI, transformers tokenizer for others)

**Trade-offs:**
- ✅ **Accuracy:** <0.5% error vs 10-15% error with 4-chars-per-token approximation
- ✅ **Reliability:** Eliminates unexpected context overflow from estimation errors
- ✅ **Budget Utilization:** Can pack to 99.5% of budget safely vs 87% conservative estimate
- ❌ **Performance:** Tokenization adds ~10ms per 1000 tokens (mitigated by caching)
- ❌ **Dependency:** Requires native library (tiktoken) with platform-specific binaries
- ❌ **Multi-Model:** Each model family needs specific tokenizer support

**Alternatives Rejected:**
1. **Approximate counting (4 chars = 1 token):** Too inaccurate, causes failures or wasted capacity
2. **Character count proxy:** Doesn't account for unicode, multi-byte characters, or subword tokenization
3. **Word count heuristic:** Fails completely for code (symbols, operators treated differently)

**Decision 2: SHA-256 Content Hash vs Fuzzy Matching for Exact Dedup**

**Chosen:** SHA-256 content hash for exact duplicate detection

**Trade-offs:**
- ✅ **Determinism:** Same content always produces same hash (reproducible builds)
- ✅ **Performance:** O(1) hash lookup vs O(n²) pairwise comparison
- ✅ **Precision:** 100% accurate for exact matches (no false positives)
- ❌ **Whitespace Sensitivity:** Single space change = different hash (mitigated by normalization)
- ❌ **Near-Duplicates:** Won't detect chunks differing only by comments or formatting
- ❌ **Memory:** Requires hash table storage (~32 bytes per chunk)

**Alternatives Rejected:**
1. **Fuzzy string matching (Levenshtein):** O(n²) complexity, false positives, no determinism
2. **MinHash/LSH:** Complex implementation, tuning required, still probabilistic
3. **Line-by-line comparison:** Slower than hashing, still whitespace-sensitive

**Decision 3: Category Budget Allocation vs Global Free-for-All**

**Chosen:** Category-based allocation with configurable percentages (tool_results 40%, open_files 30%, etc.)

**Trade-offs:**
- ✅ **Predictability:** Users know how budget is divided, can tune for their workflow
- ✅ **Fairness:** Prevents one category from dominating entire context
- ✅ **Optimization:** Can prioritize high-value content types (tool results > references)
- ❌ **Rigidity:** May waste budget if category underutilized (mitigated by redistribution option)
- ❌ **Configuration:** Requires users to think about allocation strategy
- ❌ **Complexity:** More code paths than simple greedy selection

**Alternatives Rejected:**
1. **Global greedy selection:** First-come-first-served leads to search results dominating, tool results excluded
2. **Fixed quotas per category:** Wastes budget when categories don't hit quota
3. **Dynamic ML-based allocation:** Too complex for v1, requires training data

**Decision 4: Overlap Threshold 80% vs Lower/Higher Values**

**Chosen:** Default overlap threshold of 80% for merge/deduplication

**Trade-offs:**
- ✅ **Conservative:** 80% threshold avoids false positives (merging unrelated chunks)
- ✅ **Effective:** Catches most real overlaps (adjacent/contained chunks)
- ✅ **Configurable:** Users can lower to 60% for aggressive dedup or raise to 90% for conservative
- ❌ **Edge Cases:** 79% overlap not merged (could still waste tokens)
- ❌ **Partial Overlaps:** Small overlaps (20-30%) not addressed by this mechanism
- ❌ **Complexity:** Overlap calculation requires line range arithmetic

**Alternatives Rejected:**
1. **100% overlap (exact boundaries only):** Misses most real overlaps (UserService:1-50 vs 10-60)
2. **50% threshold:** Too aggressive, merges chunks with minimal overlap (false positives)
3. **No overlap detection:** Simple but wastes ~8-12% of budget on redundant content

**Decision 5: Merge Overlapping Chunks vs Keep Best Only**

**Chosen:** Merge overlapping chunks into single expanded chunk (min_start to max_end)

**Trade-offs:**
- ✅ **Completeness:** Merged chunk contains all unique lines from both sources
- ✅ **Token Savings:** Eliminates redundant overlapping section (saves ~8% budget)
- ✅ **Context Quality:** LLM sees complete continuous code block, not fragments
- ❌ **Re-tokenization:** Must recalculate token count for merged chunk
- ❌ **Ranking Ambiguity:** Which rank to use for merged chunk? (mitigated: use max rank)
- ❌ **Size Risk:** Merged chunk might exceed reasonable size (mitigated: split if >2000 tokens)

**Alternatives Rejected:**
1. **Keep highest-ranked only:** Simpler but loses unique content from lower-ranked chunk
2. **Keep both with overlap:** Wastes tokens on duplicate lines
3. **Smart diff-based merge:** Too complex, error-prone, hard to test

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

---

## Use Cases

### Use Case 1: Ricardo - Eliminating Context Overflow Errors

**Persona:** Ricardo, Senior Backend Engineer working on a distributed microservices architecture

**Scenario:** Ricardo is debugging a payment processing failure that spans 5 microservices (PaymentService, OrderService, InventoryService, NotificationService, AuditService). He asks the AI agent to help diagnose the issue.

**Before (Without Task 016.c):**
1. Ricardo queries: "Why is payment processing failing for order #12345?"
2. Agent searches codebase, finds 247 relevant chunks across 5 services
3. Agent attempts to pack all 247 chunks into context using approximate token counting (4 chars = 1 token)
4. Estimated context: 94,000 tokens (within 100,000 budget)
5. **LLM request fails:** "Error: Context length 112,340 tokens exceeds maximum of 100,000"
6. Ricardo sees cryptic error, doesn't know which content to remove
7. Ricardo manually edits query to be more specific: "Why is PaymentService.ProcessPayment failing?"
8. Agent retries with narrower scope, succeeds this time
9. **Total time:** 8 minutes (5 min debugging error + 3 min reformulating query)
10. **Frustration level:** High (trust in AI agent damaged)

**After (With Task 016.c):**
1. Ricardo queries: "Why is payment processing failing for order #12345?"
2. Agent searches codebase, finds 247 relevant chunks across 5 services
3. **Token Counter** uses tiktoken to count exact tokens: 112,340 tokens (not 94,000!)
4. **Budget Manager** calculates: 100,000 total - 2,000 system - 8,000 response = 90,000 available
5. **Category Allocator** divides: tool_results 36K, open_files 27K, search_results 18K, references 9K
6. **Exact Deduplicator** finds 37 duplicate chunks (same functions from different searches), removes 22,800 tokens
7. **Overlap Deduplicator** merges 18 overlapping chunks (e.g., PaymentService.cs:1-50 + PaymentService.cs:25-75), saves 8,500 tokens
8. After dedup: 81,040 tokens remaining
9. **Budget Selector** picks top-ranked chunks within budget: 87,200 tokens selected (fits in 90,000)
10. **LLM request succeeds** with comprehensive context from all 5 services
11. **Total time:** 30 seconds (no errors, no retries)
12. **Frustration level:** None (seamless experience)

**Quantified Impact:**
- **Time savings:** 8 minutes → 30 seconds = **16x faster** (93.8% reduction)
- **Success rate:** 50% first-try success → 100% = **2x improvement**
- **Annual impact:** 12 queries/day × 7.5 min saved × 250 days = 22,500 minutes = **375 hours/year saved per developer** ($56,250/year at $150/hour)

---

### Use Case 2: Maya - Maximizing Context Quality Through Deduplication

**Persona:** Maya, Full-Stack Developer implementing a new feature that touches frontend, backend, and database layers

**Scenario:** Maya is adding a "bulk user import" feature that involves React components, C# API endpoints, and SQL migrations. She asks the AI agent for implementation guidance.

**Before (Without Task 016.c):**
1. Maya queries: "How do I implement bulk user import with validation and rollback?"
2. Agent searches: 8 open files (React components, API controllers), 12 tool results (code definitions), 42 search results (similar import features)
3. Agent packs all 62 chunks using greedy selection (highest rank first, no dedup)
4. **Context composition:**
   - UserService.cs:1-100 (from open file, rank 0.92)
   - UserService.cs:1-100 (from search "UserService", rank 0.88) ← **Exact duplicate**
   - UserService.cs:50-150 (from search "CreateUser", rank 0.85) ← **80% overlap with above**
   - UserValidator.cs:1-80 (from tool result, rank 0.91)
   - UserValidator.cs:1-80 (from search "UserValidator", rank 0.87) ← **Exact duplicate**
   - ... (28% of context is redundant)
5. Budget filled with 90,000 tokens, but **only 64,800 tokens are unique content**
6. LLM response lacks database migration guidance (no room in context for DB schema chunks)
7. Maya runs follow-up query: "What SQL migration is needed?" (costs another 30 seconds)
8. **Total time:** 90 seconds (2 queries needed)
9. **Context efficiency:** 72% (28% wasted on duplicates)

**After (With Task 016.c):**
1. Maya queries: "How do I implement bulk user import with validation and rollback?"
2. Agent searches: same 62 chunks
3. **Exact Deduplicator** detects:
   - UserService.cs:1-100 appears 2x → Keep highest rank (open file 0.92), remove duplicate (saves 2,100 tokens)
   - UserValidator.cs:1-80 appears 2x → Keep highest rank (tool result 0.91), remove duplicate (saves 1,680 tokens)
   - 12 other duplicates removed (saves 8,400 tokens total)
4. **Overlap Deduplicator** merges:
   - UserService.cs:1-100 + UserService.cs:50-150 → Merge to UserService.cs:1-150 (saves 1,050 tokens)
   - 5 other overlaps merged (saves 3,200 tokens total)
5. **Total dedup savings:** 11,600 tokens (12.9% of budget)
6. **Budget Selector** fills reclaimed space with:
   - DatabaseMigrations/AddBulkImportTable.sql (1,200 tokens)
   - BulkImportValidator.cs (2,800 tokens)
   - TransactionRollbackHandler.cs (3,100 tokens)
   - ErrorHandling/ValidationErrorFormatter.cs (2,100 tokens)
   - ... (fills to 89,800 tokens, all unique content)
7. LLM response includes complete guidance: React UI, API endpoints, validation, **AND database migrations**
8. **Single query provides complete answer**
9. **Total time:** 35 seconds (1 query)
10. **Context efficiency:** 99.6% (only 0.4% unavoidable overhead)

**Quantified Impact:**
- **Time savings:** 90 seconds → 35 seconds = **2.6x faster** (61% reduction)
- **Query reduction:** 2 queries → 1 query = **50% fewer LLM calls** (cost savings)
- **Context quality:** 72% unique → 99.6% unique = **38% more unique information per request**
- **Annual impact:** 8 complex queries/day × 55 sec saved × 250 days = 18,333 minutes = **306 hours/year saved per developer** ($45,900/year at $150/hour)

---

### Use Case 3: Dimitri - Category-Based Budget Optimization for Workflow Patterns

**Persona:** Dimitri, DevOps Engineer optimizing CI/CD pipelines and reviewing infrastructure code

**Scenario:** Dimitri's workflow is heavily skewed toward tool results (LSP definitions, type hierarchies) and references (imported modules), with few open files or search results. Default category allocation (40% tool, 30% open, 20% search, 10% references) wastes budget on unused categories.

**Before (Without Task 016.c):**
1. Dimitri queries: "How does the deployment pipeline handle rollback scenarios?"
2. Agent collects content:
   - Tool results (type definitions, function signatures): 78 chunks, 52,000 tokens
   - Open files (currently viewing DeploymentOrchestrator.cs): 1 chunk, 3,200 tokens
   - Search results (keyword "rollback"): 4 chunks, 2,800 tokens
   - References (imported modules): 31 chunks, 18,400 tokens
3. **Default category allocation:**
   - Tool results: 36,000 token budget (but have 52,000 available) → **16,000 tokens left on table**
   - Open files: 27,000 token budget (but only need 3,200) → **23,800 tokens wasted**
   - Search results: 18,000 token budget (but only need 2,800) → **15,200 tokens wasted**
   - References: 9,000 token budget (but need 18,400) → **Truncated, missing 9,400 tokens**
4. **Total waste:** 55,000 tokens unused, 9,400 tokens of references excluded
5. LLM response incomplete (missing key module imports needed for rollback logic)
6. Dimitri runs follow-up: "What modules does DeploymentOrchestrator import?" (another query)
7. **Total time:** 75 seconds (2 queries)

**After (With Task 016.c + Custom Category Allocation):**
1. Dimitri configures custom allocation in `.agent/config.yml`:
   ```yaml
   context:
     budget:
       categories:
         tool_results: 60  # Increased (Dimitri uses LSP heavily)
         open_files: 5     # Decreased (rarely views multiple files)
         search_results: 5 # Decreased (uses structured navigation)
         references: 30    # Increased (imports are critical for his work)
   ```
2. Dimitri queries: "How does the deployment pipeline handle rollback scenarios?"
3. Same content collected (78 tool chunks, 1 open file, 4 search results, 31 references)
4. **Custom category allocation:**
   - Tool results: 54,000 token budget (accommodates all 52,000) → **Fully utilized**
   - Open files: 4,500 token budget (need 3,200) → **Sufficient, 1,300 spare**
   - Search results: 4,500 token budget (need 2,800) → **Sufficient, 1,700 spare**
   - References: 27,000 token budget (need 18,400) → **Fully accommodates, 8,600 spare**
5. **Budget Selector** redistributes unused allocations:
   - 1,300 (open files) + 1,700 (search results) + 8,600 (references) = 11,600 tokens spare
   - Adds 11,600 tokens to tool_results → Now 54,000 + 11,600 = 65,600 available
   - Selects additional 11,000 tokens of tool results (type hierarchies, interface definitions)
6. **Total utilization:** 89,400 / 90,000 = **99.3% budget efficiency**
7. LLM response includes complete rollback logic with all module imports
8. **Single query provides complete answer**
9. **Total time:** 28 seconds (1 query, optimized allocation)

**Quantified Impact:**
- **Time savings:** 75 seconds → 28 seconds = **2.7x faster** (63% reduction)
- **Query reduction:** 2 queries → 1 query = **50% fewer LLM calls**
- **Budget utilization:** 39% → 99.3% = **2.5x more efficient use of context window**
- **Customization value:** Tailored to Dimitri's workflow (LSP + references > search)
- **Annual impact:** 15 infrastructure queries/day × 47 sec saved × 250 days = 29,375 minutes = **490 hours/year saved per DevOps engineer** ($73,500/year at $150/hour)

---

## Assumptions

### Technical Assumptions

1. **Tokenizer Availability:** The target model's tokenizer library (tiktoken for OpenAI, transformers for others) is installed and accessible at runtime. No network calls required for tokenization.

2. **Deterministic Tokenization:** Token counting is deterministic—the same content always produces the same token count when passed through the same tokenizer. No randomness or context-dependent variations.

3. **Unicode Support:** The tokenizer correctly handles all unicode characters, including emojis, multi-byte sequences, and various language scripts without corruption or miscounting.

4. **Content Immutability During Counting:** Chunk content does not change between tokenization and actual LLM submission. No runtime transformations that would alter token counts.

5. **Chunk Metadata Accuracy:** All chunks have accurate `LineStart` and `LineEnd` metadata for overlap detection. File paths are normalized and consistent across chunks from the same file.

6. **Model Context Window Known:** The target LLM's context window size is known and fixed at request time (e.g., 100,000 tokens for GPT-4-turbo). No dynamic context window adjustments.

7. **SHA-256 No Collisions:** SHA-256 content hashing produces unique hashes for unique content with negligible collision probability. Hash-based deduplication is safe.

8. **Single-Threaded Deduplication:** Deduplication runs in a single thread to maintain determinism. Parallel processing would introduce race conditions and non-deterministic results.

9. **Cache Performance:** Token count caching provides net performance benefit (cache lookup + occasional miss is faster than always tokenizing). Assumes 40-60% cache hit rate.

10. **Overlap Detection Simplicity:** Line range overlap detection is sufficient for identifying redundant chunks. No need for diff-based or semantic similarity analysis.

### Operational Assumptions

11. **System Prompt Size Known:** The system prompt token count is known or can be accurately estimated before context assembly. Typically configured once and reused.

12. **Response Reserve Conservative:** The expected response token reserve is set conservatively (e.g., 8,000 tokens) to accommodate verbose LLM outputs. Overflow is acceptable for responses.

13. **Category Allocation Tuned:** Users tune category allocation percentages (tool_results, open_files, search_results, references) based on their workflow patterns. Defaults (40/30/20/10) are reasonable starting points.

14. **Deduplication Enabled by Default:** Deduplication is enabled by default and provides value in most use cases. Users can disable if needed for specific workflows.

15. **Budget Enforcement Strict:** Budget overflow is unacceptable. Better to exclude content than risk LLM request failure. Truncation warnings are logged but not blocking.

### Integration Assumptions

16. **Upstream Ranking Available:** Task 016.b (Ranking) has already assigned relevance scores to chunks. Budget selection relies on these scores for prioritization.

17. **Downstream Context Packer:** Task 016 (Context Packer) will consume budgeted chunks and format them for LLM submission. No additional token adjustments downstream.

18. **Configuration System Integration:** Budget configuration is loaded from `.agent/config.yml` via Task 002 (Configuration). Settings are validated at startup.

19. **Logging Infrastructure Available:** Structured logging (ILogger) is available for audit trail, debug output, and performance metrics. Logs are persisted for analysis.

20. **No Concurrent Budget Managers:** Each agent instance has its own BudgetManager. No shared state or coordination between parallel agents. Budget decisions are per-session.

## Security Considerations

### Threat 1: Memory Exhaustion via Large Content Token Counting

**Risk Description:**
An attacker provides extremely large chunks (e.g., 10MB minified JavaScript files) to exhaust memory during tokenization. The tiktoken library loads entire content into memory for counting, making it vulnerable to memory-based DoS attacks.

**Attack Scenario:**
1. Attacker creates a malicious repository with artificially large files (concatenated logs, minified code)
2. Agent searches codebase, chunks create 50×  5MB chunks (250MB total)
3. Token counter attempts to tokenize all chunks in parallel
4. Memory usage spikes to 2GB+ (tiktoken internal buffers)
5. System OOM kills agent process
6. User loses work, agent becomes unavailable

**Mitigation:**

```csharp
namespace AgenticCoder.Infrastructure.Context.Budget;

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using TiktokenSharp;

public sealed class SafeTokenCounter : ITokenCounter
{
    private readonly ILogger<SafeTokenCounter> _logger;
    private readonly int _maxContentSizeBytes;
    private readonly int _maxBatchSizeBytes;
    private readonly Dictionary<string, int> _cache;
    private readonly SemaphoreSlim _semaphore;

    private const int DefaultMaxContentSize = 5_000_000; // 5MB per chunk
    private const int DefaultMaxBatchSize = 50_000_000; // 50MB total batch

    public SafeTokenCounter(
        ILogger<SafeTokenCounter> logger,
        int maxContentSizeBytes = DefaultMaxContentSize,
        int maxBatchSizeBytes = DefaultMaxBatchSize)
    {
        _logger = logger;
        _maxContentSizeBytes = maxContentSizeBytes;
        _maxBatchSizeBytes = maxBatchSizeBytes;
        _cache = new Dictionary<string, int>();
        _semaphore = new SemaphoreSlim(Environment.ProcessorCount); // Limit parallelism
    }

    public int Count(string content)
    {
        // MITIGATION 1: Content size limit
        var sizeBytes = Encoding.UTF8.GetByteCount(content);
        if (sizeBytes > _maxContentSizeBytes)
        {
            _logger.LogWarning(
                "Content size {Size}MB exceeds limit {Limit}MB, truncating",
                sizeBytes / 1_000_000.0,
                _maxContentSizeBytes / 1_000_000.0);

            content = content.Substring(0, _maxContentSizeBytes / 4); // ~4 bytes per char
            sizeBytes = _maxContentSizeBytes;
        }

        // MITIGATION 2: Cache lookup to avoid redundant work
        var hash = ComputeHash(content);
        if (_cache.TryGetValue(hash, out var cachedCount))
        {
            return cachedCount;
        }

        // MITIGATION 3: Rate-limited tokenization (prevent parallel memory spike)
        _semaphore.Wait();
        try
        {
            var encoding = TikToken.EncodingForModel("gpt-4");
            var tokens = encoding.Encode(content);
            var count = tokens.Count;

            _cache[hash] = count;
            return count;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public int Count(IEnumerable<string> contents)
    {
        var contentList = contents.ToList();

        // MITIGATION 4: Batch size limit
        var totalSizeBytes = contentList.Sum(c => Encoding.UTF8.GetByteCount(c));
        if (totalSizeBytes > _maxBatchSizeBytes)
        {
            _logger.LogWarning(
                "Batch size {Size}MB exceeds limit {Limit}MB, processing in chunks",
                totalSizeBytes / 1_000_000.0,
                _maxBatchSizeBytes / 1_000_000.0);
        }

        return contentList.Sum(c => Count(c)); // Process individually with size checks
    }

    private static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
```

---

### Threat 2: Cache Poisoning to Bypass Budget Limits

**Risk Description:**
An attacker manipulates the token count cache to store artificially low counts, causing budget enforcement to fail. If the cache is file-based or shared, tampering could lead to context overflow errors in production.

**Attack Scenario:**
1. Agent uses file-based cache for token counts (e.g., `.acode/token_cache.json`)
2. Attacker modifies cache file:
   ```json
   {
     "abc123hash": 50,   // Real count: 5000 tokens (100x undercount)
     "def456hash": 100   // Real count: 10000 tokens (100x undercount)
   }
   ```
3. Agent reads poisoned cache, trusts counts
4. Budget selector packs 200,000 tokens into 100,000 budget (thinks it's 20,000)
5. LLM request fails with overflow error
6. User frustrated, agent unreliable

**Mitigation:**

```csharp
namespace AgenticCoder.Infrastructure.Context.Budget;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

public sealed class SecureTokenCountCache
{
    private readonly ILogger<SecureTokenCountCache> _logger;
    private readonly string _cacheFilePath;
    private readonly TimeSpan _cacheLifetime;
    private readonly Dictionary<string, CacheEntry> _inMemoryCache;

    public SecureTokenCountCache(
        ILogger<SecureTokenCountCache> logger,
        string cacheFilePath,
        TimeSpan cacheLifetime)
    {
        _logger = logger;
        _cacheFilePath = cacheFilePath;
        _cacheLifetime = cacheLifetime;
        _inMemoryCache = new Dictionary<string, CacheEntry>();
    }

    public int? Get(string contentHash, int actualTokenCount)
    {
        // MITIGATION 1: In-memory cache only (no file-based tampering)
        if (!_inMemoryCache.TryGetValue(contentHash, out var entry))
        {
            return null;
        }

        // MITIGATION 2: Timestamp-based expiration
        if (DateTime.UtcNow - entry.Timestamp > _cacheLifetime)
        {
            _inMemoryCache.Remove(contentHash);
            _logger.LogDebug("Cache entry expired for hash {Hash}", contentHash);
            return null;
        }

        // MITIGATION 3: Sanity check (cached count within 10% of actual)
        var deviation = Math.Abs(entry.TokenCount - actualTokenCount) / (double)actualTokenCount;
        if (deviation > 0.10)
        {
            _logger.LogWarning(
                "Cache entry {Hash} has suspicious deviation {Deviation:P}, rejecting",
                contentHash, deviation);
            _inMemoryCache.Remove(contentHash);
            return null;
        }

        return entry.TokenCount;
    }

    public void Set(string contentHash, int tokenCount)
    {
        // MITIGATION 4: Validate token count is reasonable
        if (tokenCount < 0 || tokenCount > 1_000_000)
        {
            _logger.LogWarning("Rejecting invalid token count {Count} for hash {Hash}",
                tokenCount, contentHash);
            return;
        }

        _inMemoryCache[contentHash] = new CacheEntry
        {
            TokenCount = tokenCount,
            Timestamp = DateTime.UtcNow
        };

        // MITIGATION 5: Cache size limit (prevent memory exhaustion)
        if (_inMemoryCache.Count > 10_000)
        {
            var oldest = _inMemoryCache.OrderBy(kvp => kvp.Value.Timestamp).First();
            _inMemoryCache.Remove(oldest.Key);
        }
    }

    private sealed record CacheEntry
    {
        public int TokenCount { get; init; }
        public DateTime Timestamp { get; init; }
    }
}
```

---

### Threat 3: Budget Overflow via Integer Overflow in Summation

**Risk Description:**
When summing token counts across thousands of chunks, integer overflow could wrap to negative values, bypassing budget enforcement checks. This would allow unlimited context to be sent to the LLM.

**Attack Scenario:**
1. Attacker crafts repository with 50,000 small chunks (each 500 tokens)
2. Budget selector sums token counts: `int totalTokens = chunks.Sum(c => c.TokenCount);`
3. Total = 25,000,000 tokens (exceeds `int.MaxValue` = 2,147,483,647 if repeated)
4. Integer overflow wraps to negative value: `-2,147,418,648`
5. Budget check: `if (totalTokens < budget)` → passes (negative < 100,000)
6. Sends 25M tokens to LLM → request rejected, agent broken

**Mitigation:**

```csharp
namespace AgenticCoder.Infrastructure.Context.Budget;

using Microsoft.Extensions.Logging;

public sealed class OverflowSafeBudgetEnforcer
{
    private readonly ILogger<OverflowSafeBudgetEnforcer> _logger;
    private readonly int _maxTotalBudget;

    private const int AbsoluteMaxBudget = 1_000_000; // 1M tokens hard limit

    public OverflowSafeBudgetEnforcer(
        ILogger<OverflowSafeBudgetEnforcer> logger,
        int maxTotalBudget)
    {
        _logger = logger;
        _maxTotalBudget = Math.Min(maxTotalBudget, AbsoluteMaxBudget);
    }

    public int SafeSum(IEnumerable<int> tokenCounts)
    {
        // MITIGATION 1: Use long for accumulation (no overflow until 9 quintillion)
        long totalLong = 0;

        foreach (var count in tokenCounts)
        {
            // MITIGATION 2: Validate individual counts are non-negative
            if (count < 0)
            {
                _logger.LogWarning("Negative token count {Count} detected, treating as 0", count);
                continue;
            }

            totalLong += count;

            // MITIGATION 3: Early termination if exceeds budget (avoid full summation)
            if (totalLong > _maxTotalBudget)
            {
                _logger.LogWarning(
                    "Token sum {Total} exceeds budget {Budget}, stopping early",
                    totalLong, _maxTotalBudget);
                return _maxTotalBudget + 1; // Force overflow detection
            }
        }

        // MITIGATION 4: Clamp to int range before returning
        if (totalLong > int.MaxValue)
        {
            _logger.LogError(
                "Token sum {Total} exceeds int.MaxValue, clamping to max budget",
                totalLong);
            return _maxTotalBudget + 1; // Force budget enforcement failure
        }

        return (int)totalLong;
    }

    public bool IsWithinBudget(int totalTokens, int availableBudget)
    {
        // MITIGATION 5: Defensive checks for negative or overflow values
        if (totalTokens < 0)
        {
            _logger.LogError("Negative total tokens {Total}, rejecting", totalTokens);
            return false;
        }

        if (availableBudget <= 0)
        {
            _logger.LogError("Invalid available budget {Budget}", availableBudget);
            return false;
        }

        return totalTokens <= availableBudget;
    }
}
```

---

### Threat 4: Determinism Bypass via Race Conditions in Deduplication

**Risk Description:**
Parallel deduplication processing could introduce race conditions where the same chunk is processed by multiple threads, leading to non-deterministic selection. This breaks audit trails and makes debugging impossible.

**Attack Scenario:**
1. Agent processes 1000 chunks with 100 exact duplicates
2. Deduplication runs in parallel (10 threads)
3. Two threads detect duplicate of "UserService.cs:1-50" simultaneously
4. Thread A marks rank 0.95 as "keep", Thread B marks rank 0.92 as "keep"
5. Race condition: sometimes 0.95 wins, sometimes 0.92 wins
6. Same input produces different output on repeated runs
7. User cannot reproduce bugs, audit logs are useless

**Mitigation:**

```csharp
namespace AgenticCoder.Infrastructure.Context.Budget;

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

public sealed class DeterministicExactDeduplicator
{
    private readonly ILogger<DeterministicExactDeduplicator> _logger;

    public IReadOnlyList<RankedChunk> Deduplicate(IReadOnlyList<RankedChunk> chunks)
    {
        // MITIGATION 1: Stable sort by rank + secondary tie-breakers (no parallel sorting)
        var sortedChunks = chunks
            .OrderByDescending(c => c.Score)
            .ThenByDescending(c => c.Chunk.Source) // Tool > Open > Search > Reference
            .ThenBy(c => c.Chunk.FilePath, StringComparer.Ordinal) // Alphabetical
            .ThenBy(c => c.Chunk.LineStart) // Line number
            .ToList(); // Materialize to prevent re-evaluation

        // MITIGATION 2: Single-threaded deduplication (no race conditions)
        var seenHashes = new HashSet<string>(StringComparer.Ordinal);
        var deduplicated = new List<RankedChunk>(chunks.Count);
        var removedCount = 0;

        foreach (var chunk in sortedChunks)
        {
            var hash = ComputeContentHash(chunk.Chunk.Content);

            // MITIGATION 3: Deterministic first-wins strategy (stable sort ensures highest rank first)
            if (seenHashes.Add(hash))
            {
                deduplicated.Add(chunk);
            }
            else
            {
                removedCount++;
                _logger.LogDebug(
                    "Removed duplicate chunk: {FilePath}:{LineStart}-{LineEnd} (hash: {Hash})",
                    chunk.Chunk.FilePath, chunk.Chunk.LineStart, chunk.Chunk.LineEnd, hash[..8]);
            }
        }

        _logger.LogInformation(
            "Exact deduplication: {Total} chunks → {Unique} unique ({Removed} removed)",
            chunks.Count, deduplicated.Count, removedCount);

        // MITIGATION 4: Return in original rank order (preserve determinism for downstream)
        return deduplicated;
    }

    private static string ComputeContentHash(string content)
    {
        // MITIGATION 5: Normalize whitespace before hashing (consistent results)
        var normalized = string.Join(" ", content.Split(
            new[] { ' ', '\t', '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries));

        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
```

---

### Threat 5: Audit Trail Tampering via Log Injection

**Risk Description:**
Budget decisions (what was included/excluded) are logged for debugging. If log messages are not sanitized, an attacker could inject malicious content into logs to hide evidence of budget manipulation or confuse investigators.

**Attack Scenario:**
1. Attacker creates file with malicious name: `UserService.cs\nBUDGET_OVERRIDE:999999\n.cs`
2. Budget selector logs: "Selected chunk: UserService.cs\nBUDGET_OVERRIDE:999999\n.cs:1-50"
3. Log appears as:
   ```
   Selected chunk: UserService.cs
   BUDGET_OVERRIDE:999999
   .cs:1-50
   ```
4. Log parser (or human) misinterprets as budget override command
5. Attacker hides actual budget violations in noise
6. Audit trail is compromised

**Mitigation:**

```csharp
namespace AgenticCoder.Infrastructure.Context.Budget;

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

public sealed class SecureBudgetAuditor
{
    private readonly ILogger<SecureBudgetAuditor> _logger;
    private readonly List<AuditEntry> _auditTrail;

    public SecureBudgetAuditor(ILogger<SecureBudgetAuditor> logger)
    {
        _logger = logger;
        _auditTrail = new List<AuditEntry>();
    }

    public void LogSelection(RankedChunk chunk, string category, int tokensUsed, int categoryBudget)
    {
        // MITIGATION 1: Sanitize file path (remove newlines, control characters)
        var sanitizedPath = SanitizeForLogging(chunk.Chunk.FilePath);

        // MITIGATION 2: Structured logging (not string concatenation)
        _logger.LogInformation(
            "Selected chunk: {FilePath}:{LineStart}-{LineEnd} | " +
            "Category: {Category} | Tokens: {Tokens}/{Budget} | Rank: {Rank:F3}",
            sanitizedPath,
            chunk.Chunk.LineStart,
            chunk.Chunk.LineEnd,
            category,
            tokensUsed,
            categoryBudget,
            chunk.Score);

        // MITIGATION 3: Immutable audit trail (append-only, no modifications)
        _auditTrail.Add(new AuditEntry
        {
            Timestamp = DateTime.UtcNow,
            Action = "SELECT",
            FilePath = sanitizedPath,
            LineRange = $"{chunk.Chunk.LineStart}-{chunk.Chunk.LineEnd}",
            Category = category,
            TokensUsed = tokensUsed,
            CategoryBudget = categoryBudget,
            Rank = chunk.Score
        });
    }

    public void LogExclusion(RankedChunk chunk, string reason)
    {
        var sanitizedPath = SanitizeForLogging(chunk.Chunk.FilePath);
        var sanitizedReason = SanitizeForLogging(reason);

        _logger.LogWarning(
            "Excluded chunk: {FilePath}:{LineStart}-{LineEnd} | Reason: {Reason}",
            sanitizedPath, chunk.Chunk.LineStart, chunk.Chunk.LineEnd, sanitizedReason);

        _auditTrail.Add(new AuditEntry
        {
            Timestamp = DateTime.UtcNow,
            Action = "EXCLUDE",
            FilePath = sanitizedPath,
            LineRange = $"{chunk.Chunk.LineStart}-{chunk.Chunk.LineEnd}",
            Reason = sanitizedReason
        });
    }

    public IReadOnlyList<AuditEntry> GetAuditTrail()
    {
        // MITIGATION 4: Return read-only copy (prevent tampering)
        return _auditTrail.AsReadOnly();
    }

    private static string SanitizeForLogging(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "[EMPTY]";
        }

        // MITIGATION 5: Remove control characters, newlines, and limit length
        var sanitized = Regex.Replace(input, @"[\r\n\t\x00-\x1F\x7F]", "_");

        if (sanitized.Length > 200)
        {
            sanitized = sanitized.Substring(0, 197) + "...";
        }

        return sanitized;
    }

    public sealed record AuditEntry
    {
        public DateTime Timestamp { get; init; }
        public string Action { get; init; } = string.Empty;
        public string FilePath { get; init; } = string.Empty;
        public string LineRange { get; init; } = string.Empty;
        public string? Category { get; init; }
        public int? TokensUsed { get; init; }
        public int? CategoryBudget { get; init; }
        public double? Rank { get; init; }
        public string? Reason { get; init; }
    }
}
```

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Token | The fundamental unit of text processing in Large Language Models (LLMs). A token can be a word, part of a word (subword), or a character, depending on the tokenization algorithm. GPT models use tiktoken tokenizer, which typically produces ~1.3 tokens per word in English code and text. |
| Budget | The total number of tokens allowed for a single LLM request, encompassing system prompts, user context, and expected response. For GPT-4, this is typically 8,000 to 128,000 tokens depending on the model variant. Budget management ensures the total never exceeds this limit. |
| Tokenizer | A library or algorithm that converts text into tokens and counts them accurately. Examples include tiktoken (for OpenAI models), transformers (for Hugging Face models), and claude-tokenizer (for Anthropic models). Model-specific tokenizers are essential for accurate budget management. |
| Context Window | The maximum total tokens an LLM can process in a single request, including system prompt, user input, conversation history, and response. Also called "context limit" or "token limit." Exceeding this causes request rejection. |
| Reserve | Tokens set aside from the total budget for essential components like system prompts (instructions to the LLM) and expected response length. Reserves are subtracted from the context window to calculate available budget for user content. Typical reserves: 2,000-5,000 tokens for system, 4,000-10,000 tokens for response. |
| Allocation | The process of dividing the available budget (after reserves) across content categories. For example, allocating 40% to tool results, 30% to open files, 20% to search results, and 10% to references. Allocations are configurable and can be optimized per user workflow. |
| Category | A classification of content chunks by source type. Standard categories: tool_results (LSP definitions, type info), open_files (currently edited files), search_results (keyword/semantic search), references (imported modules, dependencies). Categories enable prioritized budget allocation. |
| Deduplication | The process of detecting and removing redundant content to maximize unique information within the token budget. Two types: exact deduplication (identical chunks) and overlap deduplication (chunks sharing significant line ranges). Typical savings: 15-25% of budget reclaimed. |
| Exact Match | Two chunks are exact matches if their content is byte-for-byte identical (after normalization). Detected using SHA-256 content hashing for O(1) lookup performance. When found, the highest-ranked instance is kept, others are removed. |
| Overlap | When two chunks from the same file share a significant portion of their line ranges. Calculated as: (overlapping_lines / min_chunk_size) × 100%. If overlap percentage exceeds the configured threshold (default 80%), chunks are merged or deduplicated. |
| Merge | Combining two overlapping chunks into a single larger chunk covering the union of their line ranges. For example, chunk A (lines 1-50) + chunk B (lines 30-70) → merged chunk (lines 1-70). The merged chunk inherits the higher rank and requires re-tokenization. |
| Cache | A temporary storage of token counts indexed by content hash to avoid redundant tokenization. When the same content appears multiple times (common in codebases), the cached count is reused instead of re-tokenizing. Typically provides 40-60% cache hit rate, significantly improving performance. |
| Estimate | An approximate token count using heuristic formulas like "4 characters = 1 token" or "1 word = 1.3 tokens." Estimates are fast but inaccurate (10-15% error), leading to wasted budget (conservative) or overflow errors (optimistic). Task 016.c rejects estimates in favor of precise counting. |
| Precise | An exact token count obtained by passing content through the actual model-specific tokenizer (e.g., tiktoken for GPT-4). Precision error is <0.5%, enabling safe packing to 99%+ of budget without overflow risk. Required for production reliability. |
| Overflow | When the total selected tokens exceed the context window budget. Causes LLM request rejection with errors like "context length 112,340 exceeds maximum 100,000." Task 016.c eliminates overflow through strict budget enforcement and deterministic selection. |
| Redistribution | The process of reallocating unused budget from underutilized categories to categories that can use more tokens. For example, if open_files allocated 27K tokens but only used 5K, the remaining 22K can be redistributed to tool_results or search_results. Maximizes budget utilization. |
| Rank | The relevance score assigned to each chunk by Task 016.b (Ranking). Range: 0.0 to 1.0, where 1.0 is most relevant. Budget selection prioritizes higher-ranked chunks within each category to maximize context quality. Deduplication keeps the highest-ranked instance when duplicates are found. |
| Threshold | The minimum score (or minimum overlap percentage) required for a chunk to be selected (score threshold) or merged (overlap threshold). Score threshold filters out low-relevance chunks; overlap threshold controls aggressiveness of merge operations. |
| Deterministic | A property of the budgeting process ensuring that the same input (chunks, ranks, config) always produces the same output (selected chunks, budget report). Critical for reproducible builds, debugging, and auditing. Achieved through stable sorting, tie-breaking rules, and no randomness. |
| Throughput | The number of chunks processed per second during token counting or deduplication. Performance target: 10,000 tokens counted in <10ms, 100 chunks deduplicated in <20ms. High throughput enables real-time context assembly without perceived latency. |

---

## Out of Scope

The following items are explicitly excluded from Task 016.c:

1. **Dynamic Budget Adjustment Based on Content Quality** - Budget allocations are fixed percentages configured upfront. The system does not dynamically adjust allocations based on content quality scores or LLM feedback. Future enhancement.

2. **Multi-Model Tokenizer Support in v1** - Initial release supports tiktoken (OpenAI models) only. Support for other tokenizers (claude-tokenizer, transformers) is deferred to v2. Single-model focus reduces complexity.

3. **Content Compression or Summarization** - The system does not compress or summarize chunks to fit within budget. Content is selected as-is based on ranking. Summarization would require LLM calls (expensive, slow) and risks information loss.

4. **Streaming Token Counting** - Token counting is performed in batch mode on the full chunk set. Streaming (counting chunks as they arrive) is not supported in v1. Batch mode simplifies caching and enables parallel processing.

5. **Semantic Deduplication (Embeddings)** - Deduplication relies on exact content matching (SHA-256 hash) and line range overlap detection. Semantic similarity using embeddings (e.g., detecting "login" and "authentication" as similar) is out of scope. Too complex and slow for v1.

6. **Per-User or Per-Session Budget Learning** - Budget allocations are static configuration. The system does not learn user-specific or session-specific allocation patterns from historical usage. Future machine learning enhancement.

7. **Automatic Budget Expansion Recommendations** - If content is consistently truncated, the system logs warnings but does not automatically recommend increasing the context window budget. User must manually adjust configuration.

8. **Cross-Model Budget Translation** - When switching models (e.g., GPT-3.5 → GPT-4), token counts may differ. The system does not translate budgets across models. User must reconfigure allocations per model.

9. **Content Reordering for Optimal Packing** - Chunks are selected in rank order. The system does not reorder chunks to optimize budget utilization (e.g., bin packing algorithms). Simple greedy selection is sufficient for v1.

10. **Partial Chunk Inclusion** - Chunks are included in their entirety or excluded completely. The system does not truncate individual chunks to fit remaining budget. Partial chunks would break code context.

11. **Budget Visualization or Interactive Tuning UI** - Budget reports are text-based (CLI output, logs). No graphical visualization or interactive UI for adjusting allocations in real-time. Future enhancement.

12. **Budget Sharing Across Parallel Agent Instances** - Each agent instance has its own independent budget. No coordination or budget sharing between concurrent agent sessions. Would require distributed state management.

13. **Historical Budget Analytics or Trends** - The system does not track budget usage over time or provide trend analysis. Each request is independent. Future observability enhancement.

14. **Content Prioritization Based on User Feedback** - Deduplication and selection do not incorporate user feedback (e.g., "this chunk was helpful"). Ranking is purely based on Task 016.b scores.

15. **Integration with External Budget Management Systems** - The system is self-contained. No integration with external quota management, billing, or usage tracking systems.

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

### Configuration Management (FR-016c-37 to FR-016c-41)

| ID | Requirement |
|----|-------------|
| FR-016c-37 | System MUST load budget configuration from `.agent/config.yml` |
| FR-016c-38 | System MUST validate category allocation percentages sum to 1.0 |
| FR-016c-39 | System MUST validate all budget values are non-negative |
| FR-016c-40 | System MUST apply default values when configuration is missing |
| FR-016c-41 | System MUST reload configuration without restarting agent |

### Validation & Error Handling (FR-016c-42 to FR-016c-46)

| ID | Requirement |
|----|-------------|
| FR-016c-42 | System MUST validate token counts are non-negative before summation |
| FR-016c-43 | System MUST detect and reject integer overflow in token summation |
| FR-016c-44 | System MUST handle tokenization failures gracefully (fallback to estimate) |
| FR-016c-45 | System MUST log errors when deduplication fails (continue without dedup) |
| FR-016c-46 | System MUST prevent division by zero in overlap percentage calculation |

### Debug & Observability (FR-016c-47 to FR-016c-50)

| ID | Requirement |
|----|-------------|
| FR-016c-47 | System MUST provide debug mode showing factor scores for each chunk |
| FR-016c-48 | System MUST log budget decisions (selected/excluded) for audit trail |
| FR-016c-49 | System MUST sanitize file paths in logs to prevent log injection |
| FR-016c-50 | System MUST generate machine-readable budget reports (JSON format) |

### Performance Optimization (FR-016c-51 to FR-016c-54)

| ID | Requirement |
|----|-------------|
| FR-016c-51 | System MUST limit token count cache to 10,000 entries |
| FR-016c-52 | System MUST evict oldest cache entries when limit reached |
| FR-016c-53 | System MUST use in-place sorting to minimize memory allocations |
| FR-016c-54 | System MUST process deduplication in O(n log n) time complexity |

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

### Maintainability (NFR-016c-10 to NFR-016c-12)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016c-10 | Maintainability | Code MUST achieve >= 80% unit test coverage |
| NFR-016c-11 | Maintainability | All public interfaces MUST have XML documentation comments |
| NFR-016c-12 | Maintainability | Configuration changes MUST NOT require code modifications |

### Usability (NFR-016c-13 to NFR-016c-15)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016c-13 | Usability | Error messages MUST provide actionable resolution guidance |
| NFR-016c-14 | Usability | Budget reports MUST be human-readable with clear formatting |
| NFR-016c-15 | Usability | Default configuration MUST work for 90% of use cases without tuning |

### Security (NFR-016c-16 to NFR-016c-18)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016c-16 | Security | Log output MUST NOT contain sensitive file paths or content |
| NFR-016c-17 | Security | Budget decisions MUST be deterministic for audit reproducibility |
| NFR-016c-18 | Security | Memory consumption MUST be bounded (no unbounded caching) |

---

## User Manual Documentation

### Overview

Token budgeting and deduplication work together to ensure your context fits within the LLM's context window while maximizing the amount of unique, relevant information. The system:

1. **Counts tokens accurately** using the model-specific tokenizer (tiktoken for GPT models)
2. **Allocates budget** across categories (tool results, open files, search results, references)
3. **Removes duplicates** using content hashing (SHA-256)
4. **Merges overlapping chunks** from the same file to eliminate redundancy
5. **Selects the best content** up to the budget limit, prioritizing by relevance rank
6. **Reports usage** with detailed breakdowns for optimization

### Quick Start Guide

#### Step 1: Configure Budget Settings

Edit `.agent/config.yml` to define your budget constraints:

```yaml
# .agent/config.yml
context:
  budget:
    # Total context window (must match your LLM model)
    # GPT-4: 8192, GPT-4-32k: 32768, GPT-4-turbo: 128000, Claude-2: 100000
    total_tokens: 100000

    # Reserved tokens for system prompts, instructions, tool schemas
    system_prompt_reserve: 2000

    # Reserved tokens for expected LLM response (conservative estimate)
    response_reserve: 8000

    # Category allocations (% of available budget after reserves)
    # Must sum to 100
    categories:
      tool_results: 40      # CLI output, test results, build logs
      open_files: 30        # Files currently being edited
      search_results: 20    # grep/file search matches
      references: 10        # Documentation, related code

  dedup:
    # Enable exact + overlap deduplication (recommended: true)
    enabled: true

    # Overlap threshold: % of lines that must overlap to merge chunks
    # 0.8 = 80% overlap required (recommended: 0.7-0.9)
    overlap_threshold: 0.8

    # Merge overlapping chunks into single larger chunk (recommended: true)
    merge_overlapping: true

  tokenizer:
    # Tokenizer type (currently only 'tiktoken' supported)
    type: tiktoken

    # Model name for tokenizer (must match your LLM)
    # Options: gpt-4, gpt-3.5-turbo, gpt-4-32k, gpt-4-turbo
    model: gpt-4
```

#### Step 2: Validate Configuration

Run the configuration validator to catch issues early:

```bash
$ acode config validate

✓ Budget configuration valid
✓ Category percentages sum to 100%
✓ Tokenizer 'tiktoken' available
✓ Model 'gpt-4' supported
✓ Total budget (100,000) > reserves (10,000)
✓ Overlap threshold (0.8) in valid range [0.0, 1.0]

Configuration OK. Budget available for content: 90,000 tokens
```

#### Step 3: Run with Budget Enforcement

Budget enforcement is automatic. When you run a command that generates context:

```bash
$ acode agent run "Refactor UserService to use dependency injection"

[Context Assembly]
├── Counting tokens (tiktoken/gpt-4)...
├── Calculating budget allocation...
├── Deduplicating content...
│   ├── Exact dedup: removed 3 duplicates (-2,200 tokens)
│   └── Overlap dedup: merged 2 chunks (-800 tokens)
├── Selecting chunks by rank...
│   ├── Tool results: 15/42 chunks (36,000 tokens, FULL)
│   ├── Open files: 8/12 chunks (27,000 tokens, FULL)
│   ├── Search results: 22/38 chunks (18,000 tokens, FULL)
│   └── References: 4/7 chunks (9,000 tokens, FULL)
└── Total context: 90,000 / 90,000 tokens (100%)

[LLM Request Sent]
```

#### Step 4: Review Budget Report

After execution, review detailed usage:

```bash
$ acode context report --last

Token Budget Report (Run #142, 2025-01-05 14:23:07)
═══════════════════════════════════════════════════

Budget Configuration:
  Total Context Window: 100,000 tokens
  System Prompt Reserve: 2,000 tokens
  Response Reserve: 8,000 tokens
  Available for Content: 90,000 tokens

Category Allocation:
┌──────────────────┬──────────┬──────────┬──────────┬──────────┐
│ Category         │ Allocated│ Used     │ Wasted   │ Fill %   │
├──────────────────┼──────────┼──────────┼──────────┼──────────┤
│ Tool Results     │ 36,000   │ 36,000   │ 0        │ 100%     │
│ Open Files       │ 27,000   │ 27,000   │ 0        │ 100%     │
│ Search Results   │ 18,000   │ 18,000   │ 0        │ 100%     │
│ References       │ 9,000    │ 9,000    │ 0        │ 100%     │
├──────────────────┼──────────┼──────────┼──────────┼──────────┤
│ TOTAL            │ 90,000   │ 90,000   │ 0        │ 100%     │
└──────────────────┴──────────┴──────────┴──────────┴──────────┘

Deduplication Savings:
  Exact Duplicates Removed: 3 chunks (-2,200 tokens)
  Overlapping Chunks Merged: 2 chunks (-800 tokens)
  Total Savings: 3,000 tokens (3.3% of original)

Content Selection:
  Chunks Considered: 99
  Chunks Selected: 49
  Chunks Rejected: 50 (insufficient budget)

Performance:
  Token Counting: 8ms
  Deduplication: 12ms
  Selection: 5ms
  Total: 25ms
```

### Configuration Tuning Guide

#### Scenario 1: Debugging Test Failures (Prioritize Tool Output)

When debugging, you want maximum test output and logs:

```yaml
categories:
  tool_results: 60    # Increased for test output, stack traces
  open_files: 20      # Reduced (you know which files already)
  search_results: 15  # Reduced (less exploration)
  references: 5       # Minimal
```

#### Scenario 2: Exploring Unfamiliar Codebase (Prioritize Search)

When learning a new codebase, prioritize search results:

```yaml
categories:
  tool_results: 15    # Minimal (less execution)
  open_files: 25      # Moderate (some context)
  search_results: 45  # Maximized for exploration
  references: 15      # Increased (more docs)
```

#### Scenario 3: Code Review (Balanced)

When reviewing code, balance all categories:

```yaml
categories:
  tool_results: 25    # Moderate (some test runs)
  open_files: 35      # Increased (reviewing many files)
  search_results: 25  # Moderate (finding related code)
  references: 15      # Moderate (checking docs)
```

#### Scenario 4: Working with Small Models (Aggressive Dedup)

For smaller context windows (e.g., 8K tokens):

```yaml
budget:
  total_tokens: 8192
  system_prompt_reserve: 1000
  response_reserve: 2000

dedup:
  enabled: true
  overlap_threshold: 0.6  # Lower threshold = more aggressive merging
  merge_overlapping: true
```

### Advanced Usage Examples

#### Example 1: Budget Overflow Warning

When content exceeds budget, you see warnings:

```bash
$ acode agent run "Analyze entire codebase for security issues"

[Context Assembly]
⚠ WARNING: Content (182,000 tokens) exceeds available budget (90,000 tokens)
⚠ Strategy: Selecting highest-ranked 49% of chunks

├── Deduplication savings: 8,200 tokens
├── After dedup: 173,800 tokens available
├── Selection by rank...
│   ├── Tool results: 20/85 chunks selected (budget: 36,000)
│   ├── Open files: 12/42 chunks selected (budget: 27,000)
│   ├── Search results: 30/128 chunks selected (budget: 18,000)
│   └── References: 6/18 chunks selected (budget: 9,000)
└── Total: 90,000 / 90,000 tokens (100%)

ℹ 205 chunks omitted due to budget constraints
ℹ Consider: Increase context window or narrow task scope
```

#### Example 2: Cache Performance Report

Enable cache diagnostics to optimize performance:

```bash
$ acode context report --cache-stats

Token Count Cache Statistics
═════════════════════════════

Total Requests: 1,247
Cache Hits: 723 (58%)
Cache Misses: 524 (42%)

Cache Size: 8,450 / 10,000 entries (85% full)
Memory Usage: ~2.1 MB

Top Cached Content (by hit count):
  1. src/Domain/User.cs:1-150 (42 hits)
  2. src/Application/Commands/CreateUser.cs:1-80 (38 hits)
  3. tests/Domain.Tests/UserTests.cs:1-200 (35 hits)

Recommendation: Cache performing well (58% hit rate > 40% target)
```

#### Example 3: Deduplication Detail Report

See exactly what was deduplicated:

```bash
$ acode context report --dedup-detail

Deduplication Report
════════════════════

Exact Duplicates Removed (3):
  1. src/Domain/User.cs:1-50
     - Found in: tool_results (rank 42), references (rank 156)
     - Kept: tool_results copy (higher rank)
     - Saved: 1,200 tokens

  2. tests/Domain.Tests/UserTests.cs:100-150
     - Found in: open_files (rank 15), search_results (rank 88)
     - Kept: open_files copy (higher rank)
     - Saved: 800 tokens

  3. README.md:1-30
     - Found in: references (rank 180), references (rank 181)
     - Kept: First occurrence (rank 180)
     - Saved: 200 tokens

Overlapping Chunks Merged (2):
  1. src/Application/Services/UserService.cs
     - Chunk A: lines 1-75 (1,500 tokens, rank 22, tool_results)
     - Chunk B: lines 50-120 (1,400 tokens, rank 35, search_results)
     - Overlap: lines 50-75 (26/75 = 83% of chunk A)
     - Merged: lines 1-120 (2,100 tokens, rank 22, tool_results)
     - Saved: 800 tokens

  2. src/Infrastructure/Data/UserRepository.cs
     - Chunk A: lines 10-80 (1,600 tokens, rank 48, open_files)
     - Chunk B: lines 60-100 (900 tokens, rank 72, search_results)
     - Overlap: lines 60-80 (21/40 = 84% of chunk B)
     - Merged: lines 10-100 (2,000 tokens, rank 48, open_files)
     - Saved: 500 tokens

Total Savings: 3,500 tokens (3.8% reduction)
```

### Troubleshooting Guide

#### Issue 1: Context Overflow Errors

**Symptoms:**
- Error: "Context length 105,340 tokens exceeds maximum 100,000"
- LLM requests fail before sending
- Budget report shows total > available

**Causes:**
- Token counts were estimates, actual counts higher
- Reserves too small (system prompt grew)
- Deduplication disabled or ineffective

**Solutions:**
1. **Enable accurate counting**: Ensure `tokenizer.type: tiktoken` in config
2. **Increase reserves**: Add 10-20% buffer to system_prompt_reserve
3. **Enable deduplication**: Set `dedup.enabled: true`
4. **Lower overlap threshold**: Try `overlap_threshold: 0.7` for more aggressive merging
5. **Adjust category allocation**: Reduce percentages for less-critical categories

**Example Fix:**
```yaml
# Before (failing)
budget:
  total_tokens: 100000
  system_prompt_reserve: 1500  # Too small
  response_reserve: 6000

dedup:
  enabled: false  # Missing savings

# After (working)
budget:
  total_tokens: 100000
  system_prompt_reserve: 2500  # Added buffer
  response_reserve: 8000

dedup:
  enabled: true
  overlap_threshold: 0.75  # Aggressive
```

#### Issue 2: Token Counts Inaccurate

**Symptoms:**
- Budget reports show "estimated" label
- Counts vary by ±10% between runs
- Different tokenizers give wildly different counts

**Causes:**
- Using fallback approximation (tiktoken not installed)
- Model mismatch (counting for gpt-3.5, using gpt-4)
- Unicode normalization differences

**Solutions:**
1. **Install tiktoken**: `pip install tiktoken` in Acode environment
2. **Match model name**: Ensure `tokenizer.model` matches LLM model exactly
3. **Clear cache**: `acode context clear-cache` to remove bad estimates
4. **Verify encoding**: Check tiktoken encoding with `acode context test-tokenizer`

**Verification:**
```bash
$ acode context test-tokenizer "Hello, world!"

Tokenizer: tiktoken (gpt-4)
Content: "Hello, world!"
Tokens: 4
Encoding: [9906, 11, 1917, 0]

✓ Tokenizer working correctly
```

#### Issue 3: Important Content Missing (Over-Deduplication)

**Symptoms:**
- LLM lacks context it needs (e.g., "I don't see UserService.cs")
- Budget report shows chunks "merged" that shouldn't be
- Different chunks from same file were needed

**Causes:**
- Overlap threshold too low (merging unrelated chunks)
- Exact dedup removed intentional duplicates (different versions)
- Ranking didn't prioritize critical chunks

**Solutions:**
1. **Raise overlap threshold**: Try `overlap_threshold: 0.9` (only merge near-identical)
2. **Disable merge**: Set `merge_overlapping: false` (keep separate chunks)
3. **Temporarily disable dedup**: Set `dedup.enabled: false` to diagnose
4. **Improve ranking**: Ensure Task 016.b ranks critical files higher

**Diagnosis:**
```bash
$ acode context report --dedup-detail

# Look for suspicious merges:
Overlapping Chunks Merged (1):
  1. src/Services/UserService.cs
     - Chunk A: lines 1-50 (constructor)
     - Chunk B: lines 200-250 (unrelated method)
     - Overlap: 82%  ← SUSPICIOUS: These shouldn't overlap!
```

### Frequently Asked Questions (FAQ)

**Q1: What happens if my category percentages don't sum to 100?**

A: Configuration validation will fail with an error:
```
✗ Error: Category percentages sum to 95% (expected 100%)
  - tool_results: 40%
  - open_files: 30%
  - search_results: 20%
  - references: 5%
  Missing: 5%
```
Fix by adjusting percentages to total exactly 100.

---

**Q2: Can I disable deduplication for specific categories?**

A: No, deduplication applies globally. However, you can effectively disable it by setting `dedup.enabled: false`. Per-category dedup control is out of scope for v1 (see "Out of Scope" section).

---

**Q3: How do I know if my budget is too small?**

A: Check the budget report fill percentages. If all categories consistently show 100% fill and "chunks rejected" is high, your budget is too small:
```
⚠ All categories at 100% fill
ℹ 87 chunks rejected (insufficient budget)
→ Recommendation: Increase total_tokens or reduce category allocations
```

---

**Q4: What's the difference between system_prompt_reserve and response_reserve?**

A:
- **system_prompt_reserve**: Tokens reserved for the system prompt, instructions, tool schemas, and conversation history that the LLM receives as input. This is known before sending.
- **response_reserve**: Tokens reserved for the LLM's expected response. Since responses can be long, this is a conservative estimate (e.g., 8K tokens) to prevent the total (input + output) from exceeding the context window.

---

**Q5: Can I use different tokenizers for different models?**

A: Not in v1. The tokenizer is configured globally. If you switch models, update the config:
```yaml
# For GPT-4
tokenizer:
  type: tiktoken
  model: gpt-4

# For GPT-3.5
tokenizer:
  type: tiktoken
  model: gpt-3.5-turbo
```
Multi-model support is out of scope (see "Out of Scope" section).

---

**Q6: How does overlap detection work for chunks from different files?**

A: It doesn't. Overlap deduplication only merges chunks **from the same file path**. Chunks from different files are never merged, even if their content is identical (that's what exact deduplication handles via SHA-256 hashing).

---

**Q7: What if two chunks have the same rank?**

A: The system uses **stable deterministic sorting**: chunks with the same rank are ordered by `(source, path, line_start)`. This ensures reproducible results for audit trails.

Example:
```
Chunk A: rank 50, source=tool_results, path=User.cs, line=1
Chunk B: rank 50, source=open_files, path=User.cs, line=1
→ Chunk A selected first (tool_results < open_files alphabetically)
```

---

**Q8: Can I see the actual token count for each chunk?**

A: Yes, use the detailed report:
```bash
$ acode context report --chunk-detail

Selected Chunks (49):
┌──────┬────────────────────────────────────┬───────┬────────┬──────────┐
│ Rank │ Path                               │ Lines │ Tokens │ Category │
├──────┼────────────────────────────────────┼───────┼────────┼──────────┤
│ 98   │ src/Domain/User.cs                 │ 1-150 │ 3,200  │ tool     │
│ 95   │ src/Application/Commands/Create.cs │ 1-80  │ 1,800  │ open     │
│ 92   │ tests/Domain.Tests/UserTests.cs    │ 1-200 │ 4,500  │ tool     │
...
```

---

**Q9: What happens if tokenization fails (e.g., tiktoken crashes)?**

A: The system falls back to a character-based approximation (1 token ≈ 4 characters) and logs a warning:
```
⚠ WARNING: Tokenization failed, using approximation
⚠ Token counts may be inaccurate (±15% error)
```
Fix by resolving the tokenizer issue (see Issue 2 in Troubleshooting).

---

**Q10: How do I optimize cache performance?**

A: Follow these practices:
1. **Keep cache enabled**: Default 10K entries is good for most workflows
2. **Run similar tasks**: Repeated analysis benefits most from caching
3. **Monitor hit rate**: Aim for >40% (check with `--cache-stats`)
4. **Clear cache periodically**: `acode context clear-cache` if stale data suspected
5. **Don't cache too aggressively**: 10K limit prevents unbounded memory growth

Cache hit rate >60% is excellent, 40-60% is good, <40% suggests tasks are too diverse for caching to help.

---

## Acceptance Criteria

This section provides comprehensive testable acceptance criteria organized by functional area. Each criterion must be verifiable through automated tests or manual verification steps.

### Token Counting (AC-001 to AC-010)

- [ ] AC-001: **Accurate token counting** - tiktoken counts tokens for sample text with <0.5% error compared to OpenAI API
- [ ] AC-002: **Model-specific tokenization** - Different models (gpt-4, gpt-3.5-turbo) produce different token counts for same content
- [ ] AC-003: **Caching works** - Second count of identical content returns cached result in <1ms
- [ ] AC-004: **Unicode handled correctly** - Content with emoji, Chinese, Japanese, Arabic tokenizes without errors
- [ ] AC-005: **Whitespace normalized** - Leading/trailing whitespace doesn't affect token count (trimmed before counting)
- [ ] AC-006: **Empty content handled** - Empty string or null returns 0 tokens without throwing exception
- [ ] AC-007: **Batch counting supported** - Can count tokens for list of 100 strings in <50ms total
- [ ] AC-008: **Long content handled** - Content with 10K+ tokens counted successfully without memory issues
- [ ] AC-009: **Cache eviction works** - When cache exceeds 10K entries, oldest entries evicted automatically
- [ ] AC-010: **Cache hit rate reported** - Cache statistics available showing hits/misses/evictions

### Budget Calculation (AC-011 to AC-020)

- [ ] AC-011: **Total budget configured** - Can set total_tokens in config (e.g., 100,000)
- [ ] AC-012: **System reserve applied** - System prompt reserve subtracted from total budget correctly
- [ ] AC-013: **Response reserve applied** - Response reserve subtracted from total budget correctly
- [ ] AC-014: **Available budget calculated** - Available = Total - SystemReserve - ResponseReserve
- [ ] AC-015: **Category percentages configured** - Can set category allocations (tool_results: 40, etc.)
- [ ] AC-016: **Category percentages validated** - Config validation fails if percentages don't sum to 100
- [ ] AC-017: **Category budgets allocated** - Each category gets percentage of available budget
- [ ] AC-018: **Zero budget rejected** - Error thrown if total budget <= (system_reserve + response_reserve)
- [ ] AC-019: **Negative values rejected** - Config validation fails if any value is negative
- [ ] AC-020: **Budget recalculation** - Can recalculate budget allocation dynamically during runtime

### Exact Deduplication (AC-021 to AC-028)

- [ ] AC-021: **Exact duplicates detected** - Two chunks with identical content detected as duplicates
- [ ] AC-022: **SHA-256 hash used** - Content hashing uses SHA-256 for collision resistance
- [ ] AC-023: **Hash collisions handled** - Two different contents with same hash prefix don't break deduplication
- [ ] AC-024: **Highest rank kept** - When duplicates exist, chunk with highest rank is retained
- [ ] AC-025: **Lower rank removed** - Duplicate chunks with lower rank are excluded from selection
- [ ] AC-026: **Different sources handled** - Duplicates from different sources (tool/search) detected correctly
- [ ] AC-027: **Dedup count reported** - Report shows exact number of duplicates removed
- [ ] AC-028: **Tokens saved reported** - Report shows tokens saved from exact deduplication

### Overlap Deduplication (AC-029 to AC-037)

- [ ] AC-029: **Overlap detected** - Two chunks from same file with overlapping line ranges detected
- [ ] AC-030: **Overlap percentage calculated** - Overlap % = (overlap_lines / min_chunk_lines) × 100
- [ ] AC-031: **Threshold respected** - Only chunks with overlap >= configured threshold are merged
- [ ] AC-032: **Same file required** - Chunks from different files never merged, even if content identical
- [ ] AC-033: **Line ranges merged** - Merged chunk includes full line range from both inputs (min to max)
- [ ] AC-034: **Content merged** - Merged chunk contains complete content from both chunks
- [ ] AC-035: **Highest rank preserved** - Merged chunk retains rank from highest-ranked input
- [ ] AC-036: **Merge count reported** - Report shows number of overlapping chunks merged
- [ ] AC-037: **Merge savings reported** - Report shows tokens saved from overlap merging

### Budget Enforcement (AC-038 to AC-046)

- [ ] AC-038: **Total limit enforced** - Selected chunks total tokens <= available budget
- [ ] AC-039: **Category limits enforced** - Each category's selected tokens <= category budget
- [ ] AC-040: **Selection by rank** - Chunks selected in descending order of rank
- [ ] AC-041: **Deterministic sorting** - Same input produces same output order (rank, source, path, line)
- [ ] AC-042: **Budget overflow prevented** - Error thrown if trying to consume more than budget allows
- [ ] AC-043: **Chunk skipping works** - If chunk exceeds remaining budget, skip and try next chunk
- [ ] AC-044: **Fill maximization** - Selection algorithm fills each category as close to budget as possible
- [ ] AC-045: **Empty categories handled** - Categories with no chunks don't break selection
- [ ] AC-046: **Single chunk oversized** - If single chunk exceeds category budget, it's skipped with warning

### Selection Logic (AC-047 to AC-054)

- [ ] AC-047: **Category isolation** - tool_results budget doesn't affect open_files budget
- [ ] AC-048: **Rank ordering stable** - Chunks with same rank ordered by (source, path, line_start)
- [ ] AC-049: **All categories processed** - Selection runs for all configured categories
- [ ] AC-050: **Rejected chunks tracked** - Chunks not selected due to budget tracked and reported
- [ ] AC-051: **Selected chunks tracked** - Chunks selected for each category tracked and reported
- [ ] AC-052: **Zero-token chunks excluded** - Chunks with 0 tokens automatically excluded
- [ ] AC-053: **Duplicate IDs prevented** - Same chunk ID not selected twice across categories
- [ ] AC-054: **Selection performance** - 1000 chunks selected in <50ms

---

## Best Practices

### Budget Allocation (BP-001 to BP-006)

1. **BP-001: Prioritize high-value content** - Allocate more budget to categories containing the most relevant information for the current task. For debugging, prioritize tool_results (60%). For exploration, prioritize search_results (45%). Review your budget reports to understand which categories provide the most value.

2. **BP-002: Reserve adequately for system prompts** - Always allocate 5-10% of total budget for system prompts and instructions. Underestimating reserves leads to overflow errors. Use `system_prompt_reserve: 2000` as a baseline for simple agents, increase to 5000+ for complex multi-tool agents with extensive schemas.

3. **BP-003: Be conservative with response reserves** - LLM responses can be verbose, especially for code generation tasks. Reserve at least 8-10% of total budget (8,000 tokens for 100K window) to prevent truncated responses. Monitor actual response sizes in reports and adjust accordingly.

4. **BP-004: Adjust allocations based on workflow patterns** - Track category fill percentages over time. If a category consistently shows <50% utilization, reduce its allocation and redistribute to frequently-full categories. Aim for 80-90% utilization across all categories.

5. **BP-005: Handle budget overflow gracefully** - When content exceeds budget, the system selects highest-ranked chunks. Ensure your ranking algorithm (Task 016.b) accurately reflects priority. For critical tasks, consider increasing total budget or narrowing scope rather than relying on automatic truncation.

6. **BP-006: Use category-specific budgets for isolation** - Category budgets prevent any single source from monopolizing context. For example, if tool output produces 50K tokens of logs, it cannot starve open_files from their allocated 27K tokens. This guarantees diverse context even with noisy categories.

### Deduplication (BP-007 to BP-012)

7. **BP-007: Enable deduplication by default** - Exact and overlap deduplication typically save 10-25% of tokens with zero information loss. The performance overhead (<20ms for 100 chunks) is negligible compared to the benefits. Only disable for debugging or troubleshooting.

8. **BP-008: Use SHA-256 for exact duplicate detection** - Content-based hashing is more reliable than filename/location-based deduplication. SHA-256 ensures 100% precision (no false positives) and handles renamed files, moved code, and copy-pasted snippets correctly.

9. **BP-009: Tune overlap threshold for your codebase** - Default 80% overlap works well for most codebases. Increase to 90% if you see unrelated chunks being merged (especially in small files). Decrease to 70% if you have many near-duplicates with minor differences (e.g., templated code, generated files).

10. **BP-010: Prioritize higher-ranked chunk when deduplicating** - When multiple copies of identical content exist with different ranks, always keep the highest-ranked copy. This ensures the most relevant source category (e.g., tool_results rank 42 vs references rank 156) is retained in the final context.

11. **BP-011: Merge overlapping chunks from same file** - Overlapping chunks waste tokens on repeated content. Merging a chunk at lines 1-75 with another at lines 50-120 creates a single chunk at lines 1-120, eliminating the redundant lines 50-75. Enable `merge_overlapping: true` for maximum efficiency.

12. **BP-012: Track and report deduplication savings** - Always log how many duplicates were removed and tokens saved. This data helps you optimize chunking strategies upstream (in Task 016.a) and proves the value of deduplication. Include in budget reports for visibility.

### Token Counting (BP-013 to BP-016)

13. **BP-013: Use model-specific tokenizers** - Generic approximations (1 token ≈ 4 characters) can be off by 10-20%. Use tiktoken with the exact model name (`gpt-4`, `gpt-3.5-turbo`) to ensure <0.5% counting error. Install tiktoken as a required dependency, not optional.

14. **BP-014: Cache token counts aggressively** - Token counting is CPU-intensive. SHA-256-indexed caching reduces repeated work. With a 10K entry limit and 40-60% hit rate, caching cuts token counting time by 50%+ on real workloads. Monitor cache hit rate and adjust size if needed.

15. **BP-015: Handle unicode correctly** - Unicode characters (emojis, non-ASCII) tokenize differently than ASCII. Ensure your tokenizer library supports unicode normalization. Test with sample content containing chinese/japanese/emoji to verify correctness before production use.

16. **BP-016: Batch counting for performance** - When counting tokens for 100+ chunks, use batch APIs (if available) to reduce overhead. Even without batch APIs, cache sharing across chunks amortizes costs. Measure token counting time and optimize if it exceeds 10ms per 100 chunks.

### Selection and Ranking (BP-017 to BP-020)

17. **BP-017: Sort deterministically for reproducibility** - Use stable sorting by (rank DESC, source ASC, path ASC, line_start ASC) to ensure identical runs produce identical results. This is critical for audit trails, debugging, and testing. Non-deterministic selection makes issues unreproducible.

18. **BP-018: Fill each category to its budget** - The selection algorithm should maximize token utilization within each category's limit. Don't waste budget by stopping early. If category has 36K budget and chunks total 38K, select chunks totaling ~35,900 tokens (as close to budget as possible without exceeding).

19. **BP-019: Skip chunks that exceed remaining budget** - If selecting the next chunk would overflow the category budget, skip it and try the next lower-ranked chunk. This greedy algorithm maximizes selected chunks. Better to include 10 smaller chunks than exclude all because the first was too large.

20. **BP-020: Report rejected chunks for visibility** - Log how many chunks were considered but not selected due to budget constraints. This helps users understand when they're losing context and decide whether to increase budget or narrow scope. Include file paths of rejected high-rank chunks for actionable insight.

---

## Troubleshooting

This section provides solutions to common issues encountered when implementing and operating token budgeting and deduplication.

### Issue 1: Token Count Exceeds Budget Despite Deduplication

**Symptoms:**
- Error: `BudgetOverflowException: Total tokens (105,340) exceeds available budget (90,000)`
- Budget report shows `Total Used: 105,340 / 90,000 tokens (117%)`
- Deduplication reports minimal savings (e.g., 2-3% instead of expected 10-15%)
- LLM requests fail with context overflow errors

**Causes:**
1. **Tokenizer mismatch**: Using `gpt-3.5-turbo` tokenizer but actual model is `gpt-4` (different vocabularies)
2. **Insufficient reserves**: System prompt grew from 1,500 to 3,200 tokens but `system_prompt_reserve: 2000` not updated
3. **Deduplication disabled**: Config has `dedup.enabled: false` or dedup service not registered in DI
4. **Estimation instead of precise counting**: tiktoken not installed, falling back to approximation formula
5. **Large chunks not split**: Upstream chunking (Task 016.a) producing chunks larger than category budgets

**Solutions:**

**Solution 1: Verify tokenizer configuration**
```bash
# Check current tokenizer config
$ cat .agent/config.yml | grep -A 3 "tokenizer:"

# Expected output:
tokenizer:
  type: tiktoken
  model: gpt-4  # Must match your actual LLM model

# If mismatch, update config
$ nano .agent/config.yml  # Change model: gpt-4

# Verify tokenization works
$ acode context test-tokenizer "Test content"
# Should show: "Tokenizer: tiktoken (gpt-4)" not "Approximation"
```

**Solution 2: Increase reserves with buffer**
```yaml
# Before (failing):
budget:
  system_prompt_reserve: 2000  # Too tight
  response_reserve: 6000

# After (fixed):
budget:
  system_prompt_reserve: 3000  # +50% buffer
  response_reserve: 8000       # +33% buffer
```

**Solution 3: Enable and verify deduplication**
```csharp
// In Startup.cs or Program.cs, verify DI registration:
services.AddBudgetServices(config);  // Should include dedup services

// Verify dedup is enabled in config:
dedup:
  enabled: true
  overlap_threshold: 0.8
  merge_overlapping: true

// Test dedup manually:
var dedup = serviceProvider.GetRequiredService<IDeduplicator>();
var chunks = LoadTestChunks();  // Load known duplicates
var result = dedup.Deduplicate(chunks);
Assert.True(result.RemovedCount > 0);  // Should find duplicates
```

**Solution 4: Install tiktoken for precise counting**
```bash
# Check if tiktoken is installed
$ pip list | grep tiktoken
# If not found, install:
$ pip install tiktoken==0.5.1

# Verify in code:
try {
    var encoding = TikToken.EncodingForModel("gpt-4");
    Console.WriteLine("tiktoken OK");
} catch {
    Console.WriteLine("ERROR: tiktoken not available");
}
```

**Solution 5: Implement chunk splitting**
```csharp
// In chunking service (Task 016.a), add size limit:
public IEnumerable<Chunk> SplitLargeChunks(
    IEnumerable<Chunk> chunks,
    int maxTokens = 5000)
{
    foreach (var chunk in chunks)
    {
        if (chunk.Tokens <= maxTokens)
        {
            yield return chunk;
            continue;
        }

        // Split large chunk into smaller pieces
        var lines = chunk.Content.Split('\n');
        var splitChunks = SplitIntoChunks(lines, maxTokens);
        foreach (var split in splitChunks)
        {
            yield return split;
        }
    }
}
```

---

### Issue 2: Cache Hit Rate Too Low (Performance Degradation)

**Symptoms:**
- Token counting takes >100ms for 100 chunks (expected <20ms)
- Cache stats show hit rate <20% (expected 40-60%)
- CPU usage spikes during context assembly
- Budget report shows `Cache Performance: Poor (18% hit rate)`

**Causes:**
1. **Content constantly changing**: Each run uses completely different files/chunks
2. **Cache eviction too aggressive**: 10K entry limit reached, evicting frequently-used items
3. **Hash collisions**: Multiple chunks have same hash prefix (unlikely but possible)
4. **Cache not persisted**: Cache cleared between runs or process restarts
5. **Large variation in workflow**: User switches between unrelated tasks frequently

**Solutions:**

**Solution 1: Increase cache size for stable workloads**
```csharp
// In TiktokenCounter constructor:
public TiktokenCounter(ILogger<TiktokenCounter> logger, string modelName = "gpt-4")
{
    _logger = logger;
    _modelName = modelName;
    // Increase cache size from 10K to 25K for stable workloads
    _cache = new Dictionary<string, int>(capacity: 25_000);
    _maxCacheSize = 25_000;
}
```

**Solution 2: Use LRU eviction instead of FIFO**
```csharp
// Replace simple dictionary with LRU cache:
using System.Collections.Concurrent;

public sealed class LruTokenCountCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache;
    private readonly int _maxSize;

    private class CacheEntry
    {
        public int TokenCount { get; set; }
        public DateTime LastAccess { get; set; }
    }

    public int Count(string content, Func<string, int> countFunc)
    {
        var hash = ComputeHash(content);

        if (_cache.TryGetValue(hash, out var entry))
        {
            entry.LastAccess = DateTime.UtcNow;  // Update LRU timestamp
            return entry.TokenCount;
        }

        var count = countFunc(content);
        _cache[hash] = new CacheEntry
        {
            TokenCount = count,
            LastAccess = DateTime.UtcNow
        };

        // Evict oldest if over limit
        if (_cache.Count > _maxSize)
        {
            var oldest = _cache.OrderBy(x => x.Value.LastAccess).First();
            _cache.TryRemove(oldest.Key, out _);
        }

        return count;
    }
}
```

**Solution 3: Monitor and report cache statistics**
```bash
# Add cache diagnostics to budget report
$ acode context report --cache-stats

# Example output identifying the issue:
Cache Statistics:
  Total Requests: 1,247
  Cache Hits: 223 (18%)  ← LOW
  Cache Misses: 1,024 (82%)

  Evictions: 847 (68% of requests)  ← TOO HIGH
  Cache Size: 10,000 / 10,000 (100% full)  ← AT LIMIT

  Recommendation: Increase cache size to reduce evictions
```

**Solution 4: Accept low hit rate for diverse workloads**
```
If your workflow involves constantly switching between unrelated codebases
or tasks, low cache hit rate (20-30%) is expected and acceptable. The cache
still provides value for repeated chunks within a single run.

Do NOT increase cache size indefinitely - it consumes memory. For highly
diverse workloads, focus on fast tokenization (use batch APIs, parallelize)
rather than caching.
```

---

### Issue 3: Chunks from Same File Being Merged Incorrectly

**Symptoms:**
- Deduplication report shows merged chunks with no actual overlap
- Context missing expected file sections (e.g., "I don't see the Initialize method")
- Overlap percentage >80% reported for non-overlapping line ranges
- Budget report shows: `Overlapping Chunks Merged: 12 chunks (-8,500 tokens)` but files are incomplete

**Causes:**
1. **Overlap calculation bug**: Line range intersection logic incorrect (e.g., off-by-one error)
2. **Different files treated as same**: Path normalization issue (e.g., `UserService.cs` vs `./UserService.cs`)
3. **Threshold too low**: `overlap_threshold: 0.5` merges chunks with only 50% overlap
4. **Line numbers incorrect**: Upstream chunking provides wrong `LineStart`/`LineEnd` metadata
5. **Merge logic bug**: Merged chunk doesn't include full line range from both inputs

**Solutions:**

**Solution 1: Validate overlap calculation logic**
```csharp
// Correct overlap detection:
public static bool IsOverlapping(Chunk a, Chunk b, double threshold)
{
    // Must be same file
    if (a.Path != b.Path) return false;

    // Calculate line range intersection
    var overlapStart = Math.Max(a.LineStart, b.LineStart);
    var overlapEnd = Math.Min(a.LineEnd, b.LineEnd);

    // No overlap if ranges don't intersect
    if (overlapStart > overlapEnd) return false;

    var overlapLines = overlapEnd - overlapStart + 1;  // +1 for inclusive
    var minChunkLines = Math.Min(
        a.LineEnd - a.LineStart + 1,
        b.LineEnd - b.LineStart + 1);

    var overlapPercentage = (double)overlapLines / minChunkLines;

    return overlapPercentage >= threshold;
}

// Test with known cases:
[Fact]
public void Should_Detect_Overlap_Correctly()
{
    var a = new Chunk { Path = "User.cs", LineStart = 1, LineEnd = 50 };
    var b = new Chunk { Path = "User.cs", LineStart = 40, LineEnd = 80 };

    // Lines 40-50 overlap = 11 lines
    // Min chunk size = 50 lines (chunk a)
    // Overlap % = 11/50 = 22%
    Assert.False(IsOverlapping(a, b, threshold: 0.8));  // 22% < 80%
}
```

**Solution 2: Normalize file paths before comparison**
```csharp
public static string NormalizePath(string path)
{
    // Convert to absolute path and normalize separators
    var absolute = Path.GetFullPath(path);
    var normalized = absolute.Replace('\\', '/').ToLowerInvariant();
    return normalized;
}

// Use in overlap detection:
if (NormalizePath(a.Path) != NormalizePath(b.Path)) return false;
```

**Solution 3: Raise overlap threshold**
```yaml
# Before (too aggressive):
dedup:
  overlap_threshold: 0.5  # Merges chunks with only 50% overlap

# After (conservative):
dedup:
  overlap_threshold: 0.85  # Requires 85% overlap to merge
```

**Solution 4: Add detailed dedup logging**
```bash
# Enable debug logging to see exactly what's being merged:
$ export ACODE_LOG_LEVEL=Debug
$ acode agent run "..."

# Log output should show:
[DEBUG] Overlap detected:
  Chunk A: User.cs:1-50 (50 lines, 1200 tokens)
  Chunk B: User.cs:40-80 (41 lines, 1000 tokens)
  Overlap: lines 40-50 (11 lines)
  Overlap %: 11/41 = 26.8%
  Threshold: 80%
  Action: NOT merged (below threshold)  ✓ EXPECTED
```

---

### Issue 4: Budget Categories Consistently Under-Utilized

**Symptoms:**
- Budget report shows multiple categories at <50% fill
- Total tokens used: 52,000 / 90,000 (58% waste)
- Category breakdown: `Tool Results: 35% fill`, `Open Files: 42% fill`, `Search: 28% fill`
- No chunks rejected due to budget constraints

**Causes:**
1. **Category allocations don't match actual content volume**: User allocated 40% to tool_results but workflow produces minimal tool output
2. **Workflow changed**: Previously debugged tests (lots of tool output), now exploring code (lots of search results)
3. **Over-conservative budgeting**: Total budget set too high for actual needs
4. **Content volume varies by task**: Some tasks generate 80K tokens, others only 30K

**Solutions:**

**Solution 1: Rebalance category allocations**
```yaml
# Before (mismatched):
categories:
  tool_results: 40     # Allocated 36K, using only 12K (33% waste)
  open_files: 30       # Allocated 27K, using only 11K (59% waste)
  search_results: 20   # Allocated 18K, using 18K (100% fill)  ← bottleneck
  references: 10       # Allocated 9K, using only 2K (78% waste)

# After (rebalanced based on actual usage):
categories:
  tool_results: 20     # Reduced from 40
  open_files: 25       # Reduced from 30
  search_results: 45   # Increased from 20  ← fix bottleneck
  references: 10       # Keep at 10
```

**Solution 2: Monitor and auto-suggest rebalancing**
```csharp
public static void AnalyzeBudgetEfficiency(BudgetReport report)
{
    var suggestions = new List<string>();

    foreach (var (category, allocated) in report.CategoryBudgets)
    {
        var used = report.CategoryUsage[category];
        var fillPercentage = (double)used / allocated;

        if (fillPercentage < 0.5)
        {
            suggestions.Add(
                $"Category '{category}' only using {fillPercentage:P0} of allocation. " +
                $"Consider reducing from {allocated} to {used * 1.2} tokens.");
        }
        else if (fillPercentage >= 0.95)
        {
            suggestions.Add(
                $"Category '{category}' at {fillPercentage:P0} capacity. " +
                $"Consider increasing from {allocated} to {used * 1.3} tokens.");
        }
    }

    if (suggestions.Any())
    {
        Console.WriteLine("\nBudget Optimization Suggestions:");
        suggestions.ForEach(s => Console.WriteLine($"  - {s}"));
    }
}
```

**Solution 3: Create workflow-specific configurations**
```yaml
# .agent/config-debug.yml (for debugging workflows)
categories:
  tool_results: 60    # High for stack traces, test output
  open_files: 20
  search_results: 15
  references: 5

# .agent/config-explore.yml (for exploration workflows)
categories:
  tool_results: 15
  open_files: 25
  search_results: 50  # High for finding relevant code
  references: 10

# .agent/config-review.yml (for code review workflows)
categories:
  tool_results: 20
  open_files: 45      # High for reviewing many files
  search_results: 20
  references: 15
```

```bash
# Use workflow-specific config:
$ acode --config .agent/config-debug.yml agent run "Debug test failure"
$ acode --config .agent/config-explore.yml agent run "Find authentication code"
```

---

### Issue 5: Non-Deterministic Selection Results (Audit Failures)

**Symptoms:**
- Same input produces different output chunks across runs
- Audit logs show: `WARN: Context selection is non-deterministic`
- Testing fails: `Expected chunk order [A, B, C], got [A, C, B]`
- Debugging impossible: Cannot reproduce context from previous run

**Causes:**
1. **Unstable sorting**: Using only rank, which has ties (multiple chunks with rank 50)
2. **Dictionary iteration order**: Relying on hash table iteration (non-deterministic in .NET)
3. **Parallel processing**: Using `Parallel.ForEach` to select chunks (race conditions)
4. **Floating-point rank values**: Rank 42.000001 vs 42.000002 (precision errors)
5. **Non-deterministic deduplication**: Keeping random duplicate when ranks are equal

**Solutions:**

**Solution 1: Implement stable multi-key sorting**
```csharp
// WRONG (non-deterministic):
var selected = chunks.OrderByDescending(c => c.Rank).Take(limit);

// CORRECT (deterministic):
var selected = chunks
    .OrderByDescending(c => c.Rank)           // Primary: rank (highest first)
    .ThenBy(c => c.Source)                    // Secondary: source category (alphabetic)
    .ThenBy(c => c.Path)                      // Tertiary: file path (alphabetic)
    .ThenBy(c => c.LineStart)                 // Quaternary: line number (lowest first)
    .Take(limit)
    .ToList();

// Verify determinism:
[Fact]
public void Selection_Should_Be_Deterministic()
{
    var chunks = LoadTestChunks();

    var result1 = selector.Select(chunks);
    var result2 = selector.Select(chunks);

    Assert.Equal(result1.Select(c => c.Id), result2.Select(c => c.Id));
}
```

**Solution 2: Use deterministic data structures**
```csharp
// WRONG (dictionary iteration is non-deterministic):
foreach (var category in categoryBudgets.Keys)
{
    SelectForCategory(category);
}

// CORRECT (sorted keys for determinism):
foreach (var category in categoryBudgets.Keys.OrderBy(k => k))
{
    SelectForCategory(category);
}
```

**Solution 3: Disable parallelization for selection**
```csharp
// WRONG (parallel = non-deterministic):
Parallel.ForEach(categories, category => {
    SelectChunksForCategory(category);
});

// CORRECT (sequential = deterministic):
foreach (var category in categories.OrderBy(c => c))
{
    SelectChunksForCategory(category);
}
```

**Solution 4: Use integer ranks or fixed precision**
```csharp
// Option A: Use integer ranks (0-100)
public int Rank { get; set; }  // Not double

// Option B: Round floating-point ranks to fixed precision
public double NormalizedRank => Math.Round(Rank, 2);  // 2 decimal places

// Use normalized rank in sorting:
.OrderByDescending(c => c.NormalizedRank)
```

**Solution 5: Deterministic duplicate resolution**
```csharp
public static Chunk ResolveDuplicate(List<Chunk> duplicates)
{
    // WRONG (non-deterministic):
    return duplicates.MaxBy(d => d.Rank);  // Undefined behavior for ties

    // CORRECT (deterministic):
    return duplicates
        .OrderByDescending(d => d.Rank)
        .ThenBy(d => d.Source)
        .ThenBy(d => d.Path)
        .ThenBy(d => d.LineStart)
        .First();
}
```

---

## Testing Requirements

### Complete Unit Test Implementations

Below are complete, runnable C# test implementations using xUnit, FluentAssertions, and NSubstitute. All tests follow Arrange-Act-Assert pattern.

#### TokenCounterTests.cs

```csharp
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using AgenticCoder.Infrastructure.Context.Budget;

namespace AgenticCoder.Infrastructure.Tests.Context.Budget;

public sealed class TokenCounterTests
{
    private readonly TiktokenCounter _sut;

    public TokenCounterTests()
    {
        _sut = new TiktokenCounter(NullLogger<TiktokenCounter>.Instance, "gpt-4");
    }

    [Fact]
    public void Should_Count_Empty_String()
    {
        // Arrange
        var content = "";

        // Act
        var count = _sut.Count(content);

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void Should_Count_Single_Word()
    {
        // Arrange
        var content = "Hello";

        // Act
        var count = _sut.Count(content);

        // Assert
        count.Should().BeGreaterThan(0);
        count.Should().BeLessThan(5);  // "Hello" is typically 1-2 tokens
    }

    [Fact]
    public void Should_Count_Sentence()
    {
        // Arrange
        var content = "The quick brown fox jumps over the lazy dog.";

        // Act
        var count = _sut.Count(content);

        // Assert
        count.Should().BeGreaterThan(5);
        count.Should().BeLessThan(15);  // Sentence ~10 tokens
    }

    [Fact]
    public void Should_Count_Code()
    {
        // Arrange
        var content = @"
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}";

        // Act
        var count = _sut.Count(content);

        // Assert
        count.Should().BeGreaterThan(10);
        count.Should().BeLessThan(40);  // Code ~20-30 tokens
    }

    [Fact]
    public void Should_Handle_Unicode()
    {
        // Arrange
        var content = "你好世界";  // Chinese: "Hello World"

        // Act
        var count = _sut.Count(content);

        // Assert
        count.Should().BeGreaterThan(0);
        count.Should().BeLessThan(20);  // Should tokenize without error
    }

    [Fact]
    public void Should_Handle_Emojis()
    {
        // Arrange
        var content = "Hello 👋 World 🌍";

        // Act
        var count = _sut.Count(content);

        // Assert
        count.Should().BeGreaterThan(0);  // Should handle emojis
    }

    [Fact]
    public void Should_Cache_Repeated_Counts()
    {
        // Arrange
        var content = "This is a test sentence for caching.";

        // Act
        var count1 = _sut.Count(content);
        var count2 = _sut.Count(content);  // Second call should be cached

        // Assert
        count1.Should().Be(count2);
        count1.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Should_Count_Batch()
    {
        // Arrange
        var contents = new[]
        {
            "First sentence.",
            "Second sentence.",
            "Third sentence."
        };

        // Act
        var totalCount = _sut.Count(contents);

        // Assert
        totalCount.Should().BeGreaterThan(0);
        totalCount.Should().Be(
            _sut.Count(contents[0]) +
            _sut.Count(contents[1]) +
            _sut.Count(contents[2]));
    }

    [Fact]
    public void Should_Handle_Null_Content()
    {
        // Arrange
        string content = null;

        // Act
        var count = _sut.Count(content);

        // Assert
        count.Should().Be(0);  // Null treated as empty
    }
}
```

#### BudgetManagerTests.cs

```csharp
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Nulls;
using AgenticCoder.Infrastructure.Context.Budget;
using AgenticCoder.Domain.Context;

namespace AgenticCoder.Infrastructure.Tests.Context.Budget;

public sealed class BudgetManagerTests
{
    [Fact]
    public void Should_Calculate_Total_Available()
    {
        // Arrange
        var config = new BudgetConfiguration
        {
            CategoryPercentages = new Dictionary<string, double>
            {
                ["tool_results"] = 0.40,
                ["open_files"] = 0.30,
                ["search_results"] = 0.20,
                ["references"] = 0.10
            }
        };
        var sut = new BudgetManager(NullLogger<BudgetManager>.Instance, config);

        // Act
        var allocation = sut.CalculateAllocation(
            totalBudget: 100_000,
            systemReserve: 2_000,
            responseReserve: 8_000);

        // Assert
        allocation.AvailableForContent.Should().Be(90_000);  // 100K - 2K - 8K
    }

    [Fact]
    public void Should_Apply_System_Reserve()
    {
        // Arrange
        var config = new BudgetConfiguration
        {
            CategoryPercentages = new Dictionary<string, double>
            {
                ["tool_results"] = 1.0
            }
        };
        var sut = new BudgetManager(NullLogger<BudgetManager>.Instance, config);

        // Act
        var allocation = sut.CalculateAllocation(
            totalBudget: 100_000,
            systemReserve: 5_000,
            responseReserve: 0);

        // Assert
        allocation.SystemReserve.Should().Be(5_000);
        allocation.AvailableForContent.Should().Be(95_000);
    }

    [Fact]
    public void Should_Apply_Response_Reserve()
    {
        // Arrange
        var config = new BudgetConfiguration
        {
            CategoryPercentages = new Dictionary<string, double>
            {
                ["tool_results"] = 1.0
            }
        };
        var sut = new BudgetManager(NullLogger<BudgetManager>.Instance, config);

        // Act
        var allocation = sut.CalculateAllocation(
            totalBudget: 100_000,
            systemReserve: 0,
            responseReserve: 10_000);

        // Assert
        allocation.ResponseReserve.Should().Be(10_000);
        allocation.AvailableForContent.Should().Be(90_000);
    }

    [Fact]
    public void Should_Allocate_Categories()
    {
        // Arrange
        var config = new BudgetConfiguration
        {
            CategoryPercentages = new Dictionary<string, double>
            {
                ["tool_results"] = 0.40,
                ["open_files"] = 0.60
            }
        };
        var sut = new BudgetManager(NullLogger<BudgetManager>.Instance, config);

        // Act
        var allocation = sut.CalculateAllocation(
            totalBudget: 100_000,
            systemReserve: 0,
            responseReserve: 0);

        // Assert
        allocation.CategoryBudgets["tool_results"].Should().Be(40_000);
        allocation.CategoryBudgets["open_files"].Should().Be(60_000);
    }

    [Fact]
    public void Should_Track_Consumption()
    {
        // Arrange
        var config = new BudgetConfiguration
        {
            CategoryPercentages = new Dictionary<string, double>
            {
                ["tool_results"] = 1.0
            }
        };
        var sut = new BudgetManager(NullLogger<BudgetManager>.Instance, config);
        sut.CalculateAllocation(totalBudget: 100_000, systemReserve: 0, responseReserve: 0);

        // Act
        sut.Consume(tokens: 5_000, category: "tool_results");
        var canFit = sut.CanFit(tokens: 95_000, category: "tool_results");

        // Assert
        canFit.Should().BeTrue();  // 5K used, 95K remaining, fits exactly
    }

    [Fact]
    public void Should_Enforce_Category_Limit()
    {
        // Arrange
        var config = new BudgetConfiguration
        {
            CategoryPercentages = new Dictionary<string, double>
            {
                ["tool_results"] = 1.0
            }
        };
        var sut = new BudgetManager(NullLogger<BudgetManager>.Instance, config);
        sut.CalculateAllocation(totalBudget: 100_000, systemReserve: 0, responseReserve: 0);
        sut.Consume(tokens: 95_000, category: "tool_results");

        // Act
        var canFit = sut.CanFit(tokens: 10_000, category: "tool_results");

        // Assert
        canFit.Should().BeFalse();  // 95K used + 10K exceeds 100K budget
    }

    [Fact]
    public void Should_Handle_Zero_Budget()
    {
        // Arrange
        var config = new BudgetConfiguration
        {
            CategoryPercentages = new Dictionary<string, double>
            {
                ["tool_results"] = 1.0
            }
        };
        var sut = new BudgetManager(NullLogger<BudgetManager>.Instance, config);

        // Act
        Action act = () => sut.CalculateAllocation(
            totalBudget: 10_000,
            systemReserve: 5_000,
            responseReserve: 5_000);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No budget available*");
    }
}
```

#### ExactDeduplicatorTests.cs

```csharp
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using AgenticCoder.Infrastructure.Context.Budget;
using AgenticCoder.Domain.Context;

namespace AgenticCoder.Infrastructure.Tests.Context.Budget;

public sealed class ExactDeduplicatorTests
{
    private readonly ExactDeduplicator _sut;

    public ExactDeduplicatorTests()
    {
        _sut = new ExactDeduplicator(NullLogger<ExactDeduplicator>.Instance);
    }

    [Fact]
    public void Should_Detect_Exact_Duplicate()
    {
        // Arrange
        var chunks = new List<RankedChunk>
        {
            new RankedChunk
            {
                Id = "chunk-1",
                Content = "public class User { }",
                Rank = 100,
                Source = "tool_results",
                Path = "User.cs",
                LineStart = 1,
                LineEnd = 1,
                Tokens = 10
            },
            new RankedChunk
            {
                Id = "chunk-2",
                Content = "public class User { }",  // Duplicate content
                Rank = 50,
                Source = "search_results",
                Path = "User.cs",
                LineStart = 1,
                LineEnd = 1,
                Tokens = 10
            }
        };

        // Act
        var result = _sut.Deduplicate(chunks);

        // Assert
        result.UniqueChunks.Should().HaveCount(1);
        result.UniqueChunks[0].Id.Should().Be("chunk-1");  // Higher rank kept
        result.RemovedCount.Should().Be(1);
        result.TokensSaved.Should().Be(10);
    }

    [Fact]
    public void Should_Keep_Highest_Ranked()
    {
        // Arrange
        var chunks = new List<RankedChunk>
        {
            new RankedChunk
            {
                Id = "chunk-low",
                Content = "Test content",
                Rank = 10,
                Tokens = 5
            },
            new RankedChunk
            {
                Id = "chunk-high",
                Content = "Test content",  // Same content
                Rank = 100,
                Tokens = 5
            }
        };

        // Act
        var result = _sut.Deduplicate(chunks);

        // Assert
        result.UniqueChunks.Should().HaveCount(1);
        result.UniqueChunks[0].Id.Should().Be("chunk-high");  // Higher rank
    }

    [Fact]
    public void Should_Handle_No_Duplicates()
    {
        // Arrange
        var chunks = new List<RankedChunk>
        {
            new RankedChunk { Id = "1", Content = "Content A", Rank = 100, Tokens = 5 },
            new RankedChunk { Id = "2", Content = "Content B", Rank = 90, Tokens = 5 },
            new RankedChunk { Id = "3", Content = "Content C", Rank = 80, Tokens = 5 }
        };

        // Act
        var result = _sut.Deduplicate(chunks);

        // Assert
        result.UniqueChunks.Should().HaveCount(3);
        result.RemovedCount.Should().Be(0);
        result.TokensSaved.Should().Be(0);
    }

    [Fact]
    public void Should_Handle_All_Duplicates()
    {
        // Arrange
        var chunks = new List<RankedChunk>
        {
            new RankedChunk { Id = "1", Content = "Same", Rank = 100, Tokens = 10 },
            new RankedChunk { Id = "2", Content = "Same", Rank = 90, Tokens = 10 },
            new RankedChunk { Id = "3", Content = "Same", Rank = 80, Tokens = 10 }
        };

        // Act
        var result = _sut.Deduplicate(chunks);

        // Assert
        result.UniqueChunks.Should().HaveCount(1);
        result.UniqueChunks[0].Id.Should().Be("1");  // Highest rank
        result.RemovedCount.Should().Be(2);
        result.TokensSaved.Should().Be(20);  // 2 duplicates × 10 tokens
    }
}
```

#### BudgetSelectorTests.cs

```csharp
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using AgenticCoder.Infrastructure.Context.Budget;
using AgenticCoder.Domain.Context;

namespace AgenticCoder.Infrastructure.Tests.Context.Budget;

public sealed class BudgetSelectorTests
{
    [Fact]
    public void Should_Select_By_Rank_Order()
    {
        // Arrange
        var chunks = new List<RankedChunk>
        {
            new RankedChunk { Id = "1", Rank = 50, Tokens = 100, Source = "tool_results" },
            new RankedChunk { Id = "2", Rank = 100, Tokens = 100, Source = "tool_results" },
            new RankedChunk { Id = "3", Rank = 75, Tokens = 100, Source = "tool_results" }
        };
        var categoryBudgets = new Dictionary<string, int>
        {
            ["tool_results"] = 250  // Can fit 2.5 chunks
        };
        var sut = new BudgetSelector(NullLogger<BudgetSelector>.Instance);

        // Act
        var result = sut.Select(chunks, categoryBudgets);

        // Assert
        result.SelectedChunks.Should().HaveCount(2);
        result.SelectedChunks[0].Id.Should().Be("2");  // Rank 100 (highest)
        result.SelectedChunks[1].Id.Should().Be("3");  // Rank 75 (second)
    }

    [Fact]
    public void Should_Fill_To_Budget()
    {
        // Arrange
        var chunks = new List<RankedChunk>
        {
            new RankedChunk { Id = "1", Rank = 100, Tokens = 900, Source = "tool_results" },
            new RankedChunk { Id = "2", Rank = 90, Tokens = 100, Source = "tool_results" },
            new RankedChunk { Id = "3", Rank = 80, Tokens = 50, Source = "tool_results" }
        };
        var categoryBudgets = new Dictionary<string, int>
        {
            ["tool_results"] = 1000
        };
        var sut = new BudgetSelector(NullLogger<BudgetSelector>.Instance);

        // Act
        var result = sut.Select(chunks, categoryBudgets);

        // Assert
        result.SelectedChunks.Should().HaveCount(2);  // 900 + 100 = 1000 (full)
        result.TotalTokens.Should().Be(1000);
    }

    [Fact]
    public void Should_Not_Exceed_Budget()
    {
        // Arrange
        var chunks = new List<RankedChunk>
        {
            new RankedChunk { Id = "1", Rank = 100, Tokens = 600, Source = "tool_results" },
            new RankedChunk { Id = "2", Rank = 90, Tokens = 600, Source = "tool_results" }
        };
        var categoryBudgets = new Dictionary<string, int>
        {
            ["tool_results"] = 1000
        };
        var sut = new BudgetSelector(NullLogger<BudgetSelector>.Instance);

        // Act
        var result = sut.Select(chunks, categoryBudgets);

        // Assert
        result.SelectedChunks.Should().HaveCount(1);  // Only first chunk fits
        result.TotalTokens.Should().Be(600);
        result.TotalTokens.Should().BeLessOrEqualTo(1000);
    }

    [Fact]
    public void Should_Skip_Chunk_If_Too_Large()
    {
        // Arrange
        var chunks = new List<RankedChunk>
        {
            new RankedChunk { Id = "1", Rank = 100, Tokens = 1200, Source = "tool_results" },  // Too large
            new RankedChunk { Id = "2", Rank = 90, Tokens = 500, Source = "tool_results" }  // Fits
        };
        var categoryBudgets = new Dictionary<string, int>
        {
            ["tool_results"] = 1000
        };
        var sut = new BudgetSelector(NullLogger<BudgetSelector>.Instance);

        // Act
        var result = sut.Select(chunks, categoryBudgets);

        // Assert
        result.SelectedChunks.Should().HaveCount(1);
        result.SelectedChunks[0].Id.Should().Be("2");  // Skipped oversized chunk
        result.TotalTokens.Should().Be(500);
    }

    [Fact]
    public void Should_Respect_Category_Limits()
    {
        // Arrange
        var chunks = new List<RankedChunk>
        {
            new RankedChunk { Id = "1", Rank = 100, Tokens = 500, Source = "tool_results" },
            new RankedChunk { Id = "2", Rank = 90, Tokens = 500, Source = "open_files" }
        };
        var categoryBudgets = new Dictionary<string, int>
        {
            ["tool_results"] = 600,
            ["open_files"] = 400
        };
        var sut = new BudgetSelector(NullLogger<BudgetSelector>.Instance);

        // Act
        var result = sut.Select(chunks, categoryBudgets);

        // Assert
        result.SelectedChunks.Should().HaveCount(1);  // tool chunk selected
        result.CategoryUsage["tool_results"].Should().Be(500);
        result.CategoryUsage["open_files"].Should().Be(0);  // 500 > 400 budget
    }
}
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

This section provides step-by-step manual verification scenarios to confirm the token budgeting and deduplication system works correctly. Each scenario includes exact commands and expected outputs.

### Scenario 1: Token Counting Accuracy

**Objective:** Verify tiktoken produces accurate token counts matching the model tokenizer.

1. **Create test content file**
   ```bash
   $ cat > test-content.txt <<'EOF'
   The quick brown fox jumps over the lazy dog.
   This is a test of the token counter.
   EOF
   ```

2. **Count tokens using Acode**
   ```bash
   $ acode context count test-content.txt

   Token count: 18
   Model: gpt-4
   Tokenizer: tiktoken
   File: test-content.txt
   ```

3. **Verify count is accurate** (manually check against OpenAI playground or API)
   - Expected: Count should match OpenAI's tokenizer within ±1 token
   - Actual: 18 tokens ✓

### Scenario 2: Budget Configuration and Allocation

**Objective:** Verify budget configuration is loaded and category allocations calculated correctly.

1. **Create budget configuration**
   ```bash
   $ cat > .agent/config.yml <<'EOF'
   context:
     budget:
       total_tokens: 10000
       system_prompt_reserve: 1000
       response_reserve: 2000
       categories:
         tool_results: 50
         open_files: 30
         search_results: 20
   EOF
   ```

2. **Validate configuration**
   ```bash
   $ acode config validate

   ✓ Budget configuration valid
   ✓ Category percentages sum to 100%
   ✓ Total budget (10,000) > reserves (3,000)

   Available for content: 7,000 tokens
   Category allocations:
     - tool_results: 3,500 tokens (50%)
     - open_files: 2,100 tokens (30%)
     - search_results: 1,400 tokens (20%)
   ```

3. **Verify calculations**
   - Total available = 10,000 - 1,000 - 2,000 = 7,000 ✓
   - tool_results = 7,000 × 0.50 = 3,500 ✓
   - open_files = 7,000 × 0.30 = 2,100 ✓
   - search_results = 7,000 × 0.20 = 1,400 ✓

### Scenario 3: Exact Deduplication

**Objective:** Verify exact duplicate chunks are detected and removed.

1. **Create test scenario with duplicate content**
   ```bash
   # Simulate context with same file from different sources
   $ acode agent run "Show me the User class" --dry-run
   ```

2. **View deduplication report**
   ```bash
   $ acode context report --dedup-detail

   Exact Duplicates Removed (2):
     1. src/Domain/User.cs:1-50
        - Found in: tool_results (rank 85), search_results (rank 42)
        - Kept: tool_results copy (higher rank)
        - Saved: 1,200 tokens

     2. src/Application/UserService.cs:10-30
        - Found in: open_files (rank 90), references (rank 15)
        - Kept: open_files copy (higher rank)
        - Saved: 450 tokens

   Total duplicates removed: 2
   Total tokens saved: 1,650
   ```

3. **Verify deduplication correctness**
   - Each duplicate should appear only once in final context ✓
   - Highest-ranked copy should be retained ✓
   - Token savings should be accurate ✓

### Scenario 4: Overlap Deduplication and Merging

**Objective:** Verify overlapping chunks from the same file are detected and merged.

1. **Create scenario with overlapping chunks**
   ```bash
   # Simulate chunks with overlapping line ranges
   $ cat > test-chunks.json <<'EOF'
   [
     {"path": "User.cs", "lines": "1-50", "rank": 100, "source": "tool_results"},
     {"path": "User.cs", "lines": "40-80", "rank": 85, "source": "search_results"}
   ]
   EOF

   $ acode context deduplicate test-chunks.json --threshold 0.8
   ```

2. **View merge report**
   ```bash
   $ acode context report --dedup-detail

   Overlapping Chunks Merged (1):
     1. User.cs
        - Chunk A: lines 1-50 (1,200 tokens, rank 100, tool_results)
        - Chunk B: lines 40-80 (1,000 tokens, rank 85, search_results)
        - Overlap: lines 40-50 (11 lines)
        - Overlap %: 11/40 = 27.5%  < 80% threshold
        - Action: NOT merged (below threshold)

   # Adjust threshold to 0.25
   $ acode context deduplicate test-chunks.json --threshold 0.25

   Overlapping Chunks Merged (1):
     1. User.cs
        - Chunk A: lines 1-50 (1,200 tokens, rank 100, tool_results)
        - Chunk B: lines 40-80 (1,000 tokens, rank 85, search_results)
        - Merged: lines 1-80 (1,600 tokens, rank 100, tool_results)
        - Saved: 600 tokens
   ```

3. **Verify merge logic**
   - Overlap percentage calculated correctly ✓
   - Threshold respected ✓
   - Merged chunk includes full line range (1-80) ✓
   - Tokens saved = 1,200 + 1,000 - 1,600 = 600 ✓

### Scenario 5: Budget-Constrained Selection

**Objective:** Verify selection algorithm respects budget limits and selects highest-ranked chunks.

1. **Set small budget to force selection**
   ```bash
   $ cat > .agent/config.yml <<'EOF'
   context:
     budget:
       total_tokens: 5000
       system_prompt_reserve: 500
       response_reserve: 500
       categories:
         tool_results: 100
   EOF
   ```

2. **Run agent with budget constraints**
   ```bash
   $ acode agent run "Refactor UserService"

   [Context Assembly]
   ⚠ WARNING: Content (8,200 tokens) exceeds available budget (4,000 tokens)
   ⚠ Strategy: Selecting highest-ranked 48.8% of chunks

   ├── Tool results: 15/32 chunks selected (4,000 tokens, FULL)
   └── Total: 4,000 / 4,000 tokens (100%)

   ℹ 17 chunks rejected (insufficient budget)
   ```

3. **Verify selection correctness**
   ```bash
   $ acode context report --selection-detail

   Selected Chunks (15):
     Rank 98: UserService.cs:1-150 (800 tokens)
     Rank 95: UserRepository.cs:1-200 (1,200 tokens)
     Rank 92: IUserService.cs:1-50 (300 tokens)
     ... (highest-ranked chunks first)

   Rejected Chunks (17):
     Rank 45: UserValidator.cs:1-80 (400 tokens) - Budget exhausted
     Rank 42: UserDto.cs:1-50 (200 tokens) - Budget exhausted
     ...
   ```

4. **Verify budget enforcement**
   - Total selected tokens = 4,000 (exactly at budget) ✓
   - Chunks ordered by rank (highest first) ✓
   - Lower-ranked chunks rejected when budget full ✓

### Scenario 6: Category Isolation

**Objective:** Verify category budgets are enforced independently (one category can't starve another).

1. **Configure category budgets**
   ```bash
   $ cat > .agent/config.yml <<'EOF'
   context:
     budget:
       total_tokens: 10000
       system_prompt_reserve: 0
       response_reserve: 0
       categories:
         tool_results: 60  # 6,000 tokens
         open_files: 40    # 4,000 tokens
   EOF
   ```

2. **Simulate scenario with excessive tool output**
   ```bash
   $ acode agent run "Run all tests and show output"

   [Context Assembly]
   ├── Tool results: 42/42 chunks (6,000 tokens, FULL)
   ├── Open files: 8/8 chunks (4,000 tokens, FULL)
   └── Total: 10,000 / 10,000 tokens (100%)
   ```

3. **Verify category isolation**
   ```bash
   $ acode context report

   Category Usage:
     Tool Results:    6,000 / 6,000 (100%) ✓
     Open Files:      4,000 / 4,000 (100%) ✓

   # Even though tool_results wanted 12K tokens,
   # it was capped at 6K and did NOT consume open_files budget
   ```

4. **Confirm isolation**
   - tool_results capped at category budget (6,000) ✓
   - open_files received full allocation (4,000) ✓
   - No budget sharing between categories ✓

### Scenario 7: Cache Performance

**Objective:** Verify token count caching improves performance.

1. **Clear cache and count tokens**
   ```bash
   $ acode context clear-cache
   $ time acode context count large-file.cs

   Token count: 8,542
   Time: 245ms  # First count (no cache)
   ```

2. **Count same file again (cached)**
   ```bash
   $ time acode context count large-file.cs

   Token count: 8,542
   Time: 2ms  # Cached count (122x faster)
   ```

3. **Check cache statistics**
   ```bash
   $ acode context report --cache-stats

   Cache Statistics:
     Total Requests: 2
     Cache Hits: 1 (50%)
     Cache Misses: 1 (50%)
     Cache Size: 1 / 10,000 entries
   ```

4. **Verify caching**
   - Second count uses cache (2ms vs 245ms) ✓
   - Cache hit rate increases with repeated content ✓
   - Token counts match (8,542) ✓

### Scenario 8: Deterministic Selection

**Objective:** Verify identical inputs produce identical outputs (reproducible for audits).

1. **Run selection twice with same input**
   ```bash
   $ acode agent run "Analyze User.cs" --seed 12345 > run1.log
   $ acode agent run "Analyze User.cs" --seed 12345 > run2.log
   ```

2. **Compare outputs**
   ```bash
   $ diff run1.log run2.log

   # No output (files identical) ✓
   ```

3. **Verify chunk order**
   ```bash
   $ grep "Selected chunk" run1.log
   Selected chunk: User.cs:1-50 (rank 100)
   Selected chunk: UserService.cs:1-80 (rank 95)
   Selected chunk: IUser.cs:1-30 (rank 90)

   $ grep "Selected chunk" run2.log
   Selected chunk: User.cs:1-50 (rank 100)
   Selected chunk: UserService.cs:1-80 (rank 95)
   Selected chunk: IUser.cs:1-30 (rank 90)

   # Identical order ✓
   ```

4. **Confirm determinism**
   - Same chunks selected in both runs ✓
   - Same order in both runs ✓
   - Total tokens identical ✓

### Scenario 9: Budget Report Accuracy

**Objective:** Verify budget reports show accurate statistics.

1. **Run agent task**
   ```bash
   $ acode agent run "Implement new feature"
   ```

2. **Generate budget report**
   ```bash
   $ acode context report

   Token Budget Report (Run #47, 2025-01-05 15:42:18)
   ════════════════════════════════════════════════

   Budget Configuration:
     Total Context Window: 100,000 tokens
     System Prompt Reserve: 2,000 tokens
     Response Reserve: 8,000 tokens
     Available for Content: 90,000 tokens

   Category Allocation:
   ┌──────────────────┬──────────┬──────────┬──────────┬──────────┐
   │ Category         │ Allocated│ Used     │ Wasted   │ Fill %   │
   ├──────────────────┼──────────┼──────────┼──────────┼──────────┤
   │ Tool Results     │ 36,000   │ 35,800   │ 200      │ 99%      │
   │ Open Files       │ 27,000   │ 27,000   │ 0        │ 100%     │
   │ Search Results   │ 18,000   │ 12,400   │ 5,600    │ 69%      │
   │ References       │ 9,000    │ 4,200    │ 4,800    │ 47%      │
   ├──────────────────┼──────────┼──────────┼──────────┼──────────┤
   │ TOTAL            │ 90,000   │ 79,400   │ 10,600   │ 88%      │
   └──────────────────┴──────────┴──────────┴──────────┴──────────┘

   Deduplication Savings:
     Exact Duplicates Removed: 5 chunks (-3,800 tokens)
     Overlapping Chunks Merged: 3 chunks (-1,200 tokens)
     Total Savings: 5,000 tokens (6.3% of original)

   Performance:
     Token Counting: 12ms
     Deduplication: 18ms
     Selection: 8ms
     Total: 38ms
   ```

3. **Verify report accuracy**
   - Sum of category usage = 35,800 + 27,000 + 12,400 + 4,200 = 79,400 ✓
   - Wasted budget calculated correctly (36,000 - 35,800 = 200) ✓
   - Deduplication savings add up (3,800 + 1,200 = 5,000) ✓
   - Performance within targets (<50ms total) ✓

### Scenario 10: Configuration Validation

**Objective:** Verify configuration errors are caught early with actionable messages.

1. **Create invalid configuration (percentages don't sum to 100)**
   ```bash
   $ cat > .agent/config.yml <<'EOF'
   context:
     budget:
       total_tokens: 100000
       categories:
         tool_results: 40
         open_files: 30
         search_results: 20
         # Missing: references (should be 10)
   EOF
   ```

2. **Validate configuration**
   ```bash
   $ acode config validate

   ✗ Error: Category percentages sum to 90% (expected 100%)
     - tool_results: 40%
     - open_files: 30%
     - search_results: 20%
     Missing: 10%

   Suggestion: Add missing category or adjust percentages to sum to 100%

   Exit code: 1
   ```

3. **Fix configuration**
   ```bash
   $ cat >> .agent/config.yml <<'EOF'
         references: 10
   EOF

   $ acode config validate

   ✓ Budget configuration valid
   ✓ Category percentages sum to 100%

   Exit code: 0 ✓
   ```

4. **Verify validation**
   - Invalid config rejected ✓
   - Clear error message with exact problem ✓
   - Actionable suggestion provided ✓
   - Fixed config passes validation ✓

---

## Implementation Prompt

**Objective:** Implement a complete token budgeting and deduplication system with model-specific tokenization, category-based allocation, exact/overlap deduplication, and budget-constrained selection.

**Key Requirements:**
- Accurate token counting using tiktoken (<0.5% error)
- Budget enforcement with category allocations (tool_results 40%, open_files 30%, search_results 20%, references 10%)
- Exact deduplication via SHA-256 content hashing
- Overlap deduplication with configurable threshold (default 80%)
- Deterministic selection (reproducible builds, audit trails)
- Performance: 10K tokens counted in <10ms, 100 chunks deduplicated in <20ms

### File Structure

```
src/AgenticCoder.Domain/
├── Context/
│   ├── ITokenCounter.cs
│   ├── IBudgetManager.cs
│   ├── IDeduplicator.cs
│   ├── BudgetModels.cs
│   └── ContentChunk.cs  (from Task 016.a)
│
src/AgenticCoder.Infrastructure/
├── Context/
│   └── Budget/
│       ├── TiktokenCounter.cs
│       ├── BudgetManager.cs
│       ├── CategoryAllocator.cs
│       ├── ExactDeduplicator.cs
│       ├── OverlapDeduplicator.cs
│       ├── BudgetSelector.cs
│       └── BudgetConfiguration.cs
│
src/AgenticCoder.Infrastructure/
└── DependencyInjection/
    └── BudgetServiceExtensions.cs
```

---

### Domain Layer - Interfaces and Models

**File: `src/AgenticCoder.Domain/Context/ITokenCounter.cs`**

```csharp
namespace AgenticCoder.Domain.Context;

/// <summary>
/// Service for counting tokens in text content using model-specific tokenizers.
/// </summary>
public interface ITokenCounter
{
    /// <summary>
    /// Counts tokens in a single piece of content.
    /// </summary>
    /// <param name="content">The text content to tokenize.</param>
    /// <returns>The exact token count for the target model.</returns>
    int Count(string content);

    /// <summary>
    /// Counts total tokens across multiple pieces of content.
    /// </summary>
    /// <param name="contents">The collection of text content.</param>
    /// <returns>The sum of token counts across all content.</returns>
    int Count(IEnumerable<string> contents);
}
```

**File: `src/AgenticCoder.Domain/Context/IBudgetManager.cs`**

```csharp
namespace AgenticCoder.Domain.Context;

/// <summary>
/// Manages token budget allocation and enforcement across content categories.
/// </summary>
public interface IBudgetManager
{
    /// <summary>
    /// Calculates budget allocation for each category based on configuration.
    /// </summary>
    /// <param name="totalBudget">Total context window size in tokens.</param>
    /// <param name="systemReserve">Tokens reserved for system prompt.</param>
    /// <param name="responseReserve">Tokens reserved for LLM response.</param>
    /// <returns>Budget allocation breakdown by category.</returns>
    BudgetAllocation CalculateAllocation(int totalBudget, int systemReserve, int responseReserve);

    /// <summary>
    /// Checks if a given number of tokens can fit within a category's budget.
    /// </summary>
    /// <param name="tokens">Number of tokens to check.</param>
    /// <param name="category">Target category (e.g., "tool_results").</param>
    /// <returns>True if tokens fit within category budget, false otherwise.</returns>
    bool CanFit(int tokens, string category);

    /// <summary>
    /// Consumes tokens from a category's budget.
    /// </summary>
    /// <param name="tokens">Number of tokens to consume.</param>
    /// <param name="category">Category to consume from.</param>
    void Consume(int tokens, string category);

    /// <summary>
    /// Generates a budget report showing usage across categories.
    /// </summary>
    /// <returns>Budget report with usage statistics.</returns>
    BudgetReport GetReport();
}
```

**File: `src/AgenticCoder.Domain/Context/IDeduplicator.cs`**

```csharp
namespace AgenticCoder.Domain.Context;

/// <summary>
/// Service for detecting and removing duplicate or overlapping chunks.
/// </summary>
public interface IDeduplicator
{
    /// <summary>
    /// Removes exact duplicate chunks (same content).
    /// </summary>
    /// <param name="chunks">Input chunks (may contain duplicates).</param>
    /// <returns>Deduplicated chunks (highest-ranked instance kept).</returns>
    IReadOnlyList<RankedChunk> RemoveExactDuplicates(IReadOnlyList<RankedChunk> chunks);

    /// <summary>
    /// Merges chunks with overlapping line ranges (same file).
    /// </summary>
    /// <param name="chunks">Input chunks (may have overlaps).</param>
    /// <param name="overlapThreshold">Minimum overlap percentage to merge (0.0-1.0).</param>
    /// <returns>Chunks with overlaps merged.</returns>
    IReadOnlyList<RankedChunk> MergeOverlapping(IReadOnlyList<RankedChunk> chunks, double overlapThreshold);
}
```

**File: `src/AgenticCoder.Domain/Context/BudgetModels.cs`**

```csharp
namespace AgenticCoder.Domain.Context;

/// <summary>
/// Budget allocation breakdown by category.
/// </summary>
public sealed record BudgetAllocation(
    int TotalBudget,
    int SystemReserve,
    int ResponseReserve,
    int AvailableForContent,
    Dictionary<string, int> CategoryBudgets);

/// <summary>
/// Budget usage report with statistics.
/// </summary>
public sealed record BudgetReport(
    int TotalBudget,
    int TotalUsed,
    Dictionary<string, CategoryUsage> CategoryBreakdown,
    DeduplicationStats DeduplicationStats);

/// <summary>
/// Usage statistics for a single category.
/// </summary>
public sealed record CategoryUsage(
    int Allocated,
    int Used,
    double UtilizationPercentage);

/// <summary>
/// Deduplication savings statistics.
/// </summary>
public sealed record DeduplicationStats(
    int ExactDuplicatesRemoved,
    int OverlapsMerged,
    int TokensSaved);

/// <summary>
/// Configuration for budget allocation.
/// </summary>
public sealed record BudgetConfiguration
{
    public int TotalBudget { get; init; } = 100_000;
    public int SystemReserve { get; init; } = 2_000;
    public int ResponseReserve { get; init; } = 8_000;
    public Dictionary<string, double> CategoryPercentages { get; init; } = new()
    {
        { "tool_results", 0.40 },
        { "open_files", 0.30 },
        { "search_results", 0.20 },
        { "references", 0.10 }
    };
    public double OverlapThreshold { get; init; } = 0.80;
    public bool EnableDeduplication { get; init; } = true;
}
```

---

### Infrastructure Layer - Implementations

**File: `src/AgenticCoder.Infrastructure/Context/Budget/TiktokenCounter.cs`**

```csharp
namespace AgenticCoder.Infrastructure.Context.Budget;

using AgenticCoder.Domain.Context;
using Microsoft.Extensions.Logging;
using TiktokenSharp;
using System.Security.Cryptography;
using System.Text;

public sealed class TiktokenCounter : ITokenCounter
{
    private readonly ILogger<TiktokenCounter> _logger;
    private readonly string _modelName;
    private readonly Dictionary<string, int> _cache;

    public TiktokenCounter(ILogger<TiktokenCounter> logger, string modelName = "gpt-4")
    {
        _logger = logger;
        _modelName = modelName;
        _cache = new Dictionary<string, int>();
    }

    public int Count(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return 0;
        }

        // Check cache first
        var hash = ComputeHash(content);
        if (_cache.TryGetValue(hash, out var cachedCount))
        {
            _logger.LogDebug("Cache hit for content hash {Hash}", hash[..8]);
            return cachedCount;
        }

        // Tokenize with tiktoken
        var encoding = TikToken.EncodingForModel(_modelName);
        var tokens = encoding.Encode(content);
        var count = tokens.Count;

        // Cache result
        _cache[hash] = count;

        // Limit cache size
        if (_cache.Count > 10_000)
        {
            var oldest = _cache.First();
            _cache.Remove(oldest.Key);
        }

        return count;
    }

    public int Count(IEnumerable<string> contents)
    {
        return contents.Sum(c => Count(c));
    }

    private static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
```

**File: `src/AgenticCoder.Infrastructure/Context/Budget/BudgetManager.cs`**

```csharp
namespace AgenticCoder.Infrastructure.Context.Budget;

using AgenticCoder.Domain.Context;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class BudgetManager : IBudgetManager
{
    private readonly ILogger<BudgetManager> _logger;
    private readonly BudgetConfiguration _config;
    private readonly Dictionary<string, int> _consumed;
    private BudgetAllocation? _currentAllocation;

    public BudgetManager(
        ILogger<BudgetManager> logger,
        IOptions<BudgetConfiguration> config)
    {
        _logger = logger;
        _config = config.Value;
        _consumed = new Dictionary<string, int>();
    }

    public BudgetAllocation CalculateAllocation(int totalBudget, int systemReserve, int responseReserve)
    {
        var availableForContent = totalBudget - systemReserve - responseReserve;

        if (availableForContent <= 0)
        {
            throw new InvalidOperationException(
                $"No budget available for content. Total: {totalBudget}, " +
                $"Reserves: {systemReserve + responseReserve}");
        }

        var categoryBudgets = new Dictionary<string, int>();
        foreach (var (category, percentage) in _config.CategoryPercentages)
        {
            var allocated = (int)(availableForContent * percentage);
            categoryBudgets[category] = allocated;
            _consumed[category] = 0; // Initialize consumption tracking
        }

        _currentAllocation = new BudgetAllocation(
            totalBudget,
            systemReserve,
            responseReserve,
            availableForContent,
            categoryBudgets);

        _logger.LogInformation(
            "Budget allocated: {Available} tokens available for content",
            availableForContent);

        return _currentAllocation;
    }

    public bool CanFit(int tokens, string category)
    {
        if (_currentAllocation == null)
        {
            throw new InvalidOperationException("Budget allocation not calculated");
        }

        if (!_currentAllocation.CategoryBudgets.TryGetValue(category, out var allocated))
        {
            _logger.LogWarning("Unknown category {Category}", category);
            return false;
        }

        var used = _consumed.GetValueOrDefault(category, 0);
        var remaining = allocated - used;

        return tokens <= remaining;
    }

    public void Consume(int tokens, string category)
    {
        if (!_consumed.ContainsKey(category))
        {
            _consumed[category] = 0;
        }

        _consumed[category] += tokens;

        _logger.LogDebug(
            "Consumed {Tokens} tokens from {Category}. Total used: {Used}",
            tokens, category, _consumed[category]);
    }

    public BudgetReport GetReport()
    {
        if (_currentAllocation == null)
        {
            throw new InvalidOperationException("Budget allocation not calculated");
        }

        var categoryBreakdown = new Dictionary<string, CategoryUsage>();
        var totalUsed = 0;

        foreach (var (category, allocated) in _currentAllocation.CategoryBudgets)
        {
            var used = _consumed.GetValueOrDefault(category, 0);
            totalUsed += used;

            var utilization = allocated > 0 ? (double)used / allocated : 0.0;

            categoryBreakdown[category] = new CategoryUsage(
                allocated,
                used,
                utilization);
        }

        return new BudgetReport(
            _currentAllocation.TotalBudget,
            totalUsed,
            categoryBreakdown,
            new DeduplicationStats(0, 0, 0)); // Populated by deduplicator
    }
}
```

**File: `src/AgenticCoder.Infrastructure/Context/Budget/ExactDeduplicator.cs`**

```csharp
namespace AgenticCoder.Infrastructure.Context.Budget;

using AgenticCoder.Domain.Context;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

public sealed class ExactDeduplicator : IDeduplicator
{
    private readonly ILogger<ExactDeduplicator> _logger;

    public ExactDeduplicator(ILogger<ExactDeduplicator> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<RankedChunk> RemoveExactDuplicates(IReadOnlyList<RankedChunk> chunks)
    {
        // Stable sort by rank (descending) to ensure highest-ranked duplicates are kept
        var sorted = chunks
            .OrderByDescending(c => c.Score)
            .ThenBy(c => c.Chunk.FilePath)
            .ThenBy(c => c.Chunk.LineStart)
            .ToList();

        var seenHashes = new HashSet<string>();
        var deduplicated = new List<RankedChunk>();
        var removedCount = 0;

        foreach (var chunk in sorted)
        {
            var hash = ComputeContentHash(chunk.Chunk.Content);

            if (seenHashes.Add(hash))
            {
                deduplicated.Add(chunk);
            }
            else
            {
                removedCount++;
                _logger.LogDebug(
                    "Removed duplicate: {FilePath}:{LineStart}-{LineEnd}",
                    chunk.Chunk.FilePath, chunk.Chunk.LineStart, chunk.Chunk.LineEnd);
            }
        }

        _logger.LogInformation(
            "Exact deduplication: {Total} → {Unique} chunks ({Removed} removed)",
            chunks.Count, deduplicated.Count, removedCount);

        return deduplicated;
    }

    public IReadOnlyList<RankedChunk> MergeOverlapping(IReadOnlyList<RankedChunk> chunks, double overlapThreshold)
    {
        // Group by file path
        var byFile = chunks
            .GroupBy(c => c.Chunk.FilePath)
            .ToDictionary(g => g.Key, g => g.ToList());

        var merged = new List<RankedChunk>();

        foreach (var (filePath, fileChunks) in byFile)
        {
            var sortedByLine = fileChunks.OrderBy(c => c.Chunk.LineStart).ToList();
            var processed = new HashSet<int>();

            for (int i = 0; i < sortedByLine.Count; i++)
            {
                if (processed.Contains(i)) continue;

                var current = sortedByLine[i];
                var toMerge = new List<RankedChunk> { current };

                // Find overlapping chunks
                for (int j = i + 1; j < sortedByLine.Count; j++)
                {
                    if (processed.Contains(j)) continue;

                    var candidate = sortedByLine[j];
                    var overlap = CalculateOverlap(current.Chunk, candidate.Chunk);

                    if (overlap >= overlapThreshold)
                    {
                        toMerge.Add(candidate);
                        processed.Add(j);
                    }
                }

                // Merge if multiple chunks found
                if (toMerge.Count > 1)
                {
                    var mergedChunk = MergeChunks(toMerge);
                    merged.Add(mergedChunk);
                    _logger.LogDebug(
                        "Merged {Count} overlapping chunks in {FilePath}",
                        toMerge.Count, filePath);
                }
                else
                {
                    merged.Add(current);
                }

                processed.Add(i);
            }
        }

        return merged;
    }

    private static double CalculateOverlap(ContentChunk a, ContentChunk b)
    {
        if (a.FilePath != b.FilePath) return 0.0;

        var overlapStart = Math.Max(a.LineStart, b.LineStart);
        var overlapEnd = Math.Min(a.LineEnd, b.LineEnd);

        if (overlapStart >= overlapEnd) return 0.0;

        var overlapLines = overlapEnd - overlapStart;
        var minChunkSize = Math.Min(a.LineEnd - a.LineStart, b.LineEnd - b.LineStart);

        return (double)overlapLines / minChunkSize;
    }

    private static RankedChunk MergeChunks(List<RankedChunk> chunks)
    {
        var minLine = chunks.Min(c => c.Chunk.LineStart);
        var maxLine = chunks.Max(c => c.Chunk.LineEnd);
        var maxRank = chunks.Max(c => c.Score);
        var first = chunks.First();

        // Reconstruct merged content (simplified - in production, re-read file lines)
        var mergedContent = string.Join("\n", chunks.Select(c => c.Chunk.Content));

        var mergedContentChunk = first.Chunk with
        {
            LineStart = minLine,
            LineEnd = maxLine,
            Content = mergedContent
        };

        return new RankedChunk(mergedContentChunk, maxRank, first.Factors);
    }

    private static string ComputeContentHash(string content)
    {
        var normalized = string.Join(" ", content.Split(
            new[] { ' ', '\t', '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries));

        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
```

**File: `src/AgenticCoder.Infrastructure/Context/Budget/BudgetSelector.cs`**

```csharp
namespace AgenticCoder.Infrastructure.Context.Budget;

using AgenticCoder.Domain.Context;
using Microsoft.Extensions.Logging;

public sealed class BudgetSelector
{
    private readonly ILogger<BudgetSelector> _logger;
    private readonly IBudgetManager _budgetManager;
    private readonly ITokenCounter _tokenCounter;

    public BudgetSelector(
        ILogger<BudgetSelector> logger,
        IBudgetManager budgetManager,
        ITokenCounter tokenCounter)
    {
        _logger = logger;
        _budgetManager = budgetManager;
        _tokenCounter = tokenCounter;
    }

    public IReadOnlyList<RankedChunk> SelectWithinBudget(IReadOnlyList<RankedChunk> chunks)
    {
        // Group by category
        var byCategory = chunks
            .GroupBy(c => MapSourceToCategory(c.Chunk.Source))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.Score).ToList());

        var selected = new List<RankedChunk>();

        foreach (var (category, categoryChunks) in byCategory)
        {
            foreach (var chunk in categoryChunks)
            {
                var tokenCount = _tokenCounter.Count(chunk.Chunk.Content);

                if (_budgetManager.CanFit(tokenCount, category))
                {
                    selected.Add(chunk);
                    _budgetManager.Consume(tokenCount, category);
                }
                else
                {
                    _logger.LogDebug(
                        "Skipped chunk (budget exceeded): {FilePath}:{LineStart}-{LineEnd}",
                        chunk.Chunk.FilePath, chunk.Chunk.LineStart, chunk.Chunk.LineEnd);
                }
            }
        }

        _logger.LogInformation(
            "Selected {Selected}/{Total} chunks within budget",
            selected.Count, chunks.Count);

        return selected;
    }

    private static string MapSourceToCategory(ChunkSource source)
    {
        return source switch
        {
            ChunkSource.ToolResult => "tool_results",
            ChunkSource.OpenFile => "open_files",
            ChunkSource.SearchResult => "search_results",
            ChunkSource.Reference => "references",
            _ => "search_results"
        };
    }
}
```

---

### Dependency Injection Setup

**File: `src/AgenticCoder.Infrastructure/DependencyInjection/BudgetServiceExtensions.cs`**

```csharp
namespace AgenticCoder.Infrastructure.DependencyInjection;

using AgenticCoder.Domain.Context;
using AgenticCoder.Infrastructure.Context.Budget;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class BudgetServiceExtensions
{
    public static IServiceCollection AddBudgetServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration
        services.Configure<BudgetConfiguration>(
            configuration.GetSection("Budget"));

        // Token counting
        services.AddSingleton<ITokenCounter, TiktokenCounter>();

        // Budget management
        services.AddSingleton<IBudgetManager, BudgetManager>();

        // Deduplication
        services.AddSingleton<IDeduplicator, ExactDeduplicator>();

        // Selection
        services.AddSingleton<BudgetSelector>();

        return services;
    }
}
```

---

### Error Codes

| Code | Meaning | Resolution |
|------|---------|------------|
| ACODE-BUD-001 | Total budget exceeded | Reduce content, increase context window, or adjust allocations |
| ACODE-BUD-002 | Category budget exceeded | Increase category percentage or reduce content in that category |
| ACODE-BUD-003 | Token counting failed | Check tiktoken library installation, verify model name |
| ACODE-BUD-004 | Deduplication failed | Check for corrupted chunk data, verify file paths are valid |
| ACODE-BUD-005 | Invalid budget configuration | Ensure category percentages sum to 1.0, reserves are positive |
| ACODE-BUD-006 | Allocation not calculated | Call CalculateAllocation before attempting selection |

---

### Implementation Checklist

**Domain Layer:**
- [ ] Create `ITokenCounter` interface with Count methods
- [ ] Create `IBudgetManager` interface with allocation, consumption, reporting methods
- [ ] Create `IDeduplicator` interface with exact and overlap deduplication methods
- [ ] Create `BudgetAllocation` record with category budgets
- [ ] Create `BudgetReport` record with usage statistics
- [ ] Create `CategoryUsage` record with allocated/used/utilization
- [ ] Create `DeduplicationStats` record with savings metrics
- [ ] Create `BudgetConfiguration` record with default values

**Infrastructure Layer:**
- [ ] Implement `TiktokenCounter` with tiktoken integration and caching
- [ ] Implement `BudgetManager` with allocation calculation and tracking
- [ ] Implement `ExactDeduplicator` with SHA-256 hashing and stable sorting
- [ ] Implement overlap detection algorithm (line range intersection)
- [ ] Implement chunk merging logic (union of line ranges)
- [ ] Implement `BudgetSelector` with category-based selection
- [ ] Add configuration binding from appsettings.json

**Dependency Injection:**
- [ ] Create `BudgetServiceExtensions` with AddBudgetServices method
- [ ] Register ITokenCounter → TiktokenCounter as singleton
- [ ] Register IBudgetManager → BudgetManager as singleton
- [ ] Register IDeduplicator → ExactDeduplicator as singleton
- [ ] Register BudgetSelector as singleton
- [ ] Bind BudgetConfiguration from configuration

**Testing:**
- [ ] Write unit tests for TiktokenCounter (caching, batch counting)
- [ ] Write unit tests for BudgetManager (allocation, consumption, reporting)
- [ ] Write unit tests for ExactDeduplicator (duplicate detection, stable sort)
- [ ] Write unit tests for overlap detection (edge cases, different thresholds)
- [ ] Write integration tests for end-to-end budgeting pipeline
- [ ] Write performance benchmarks (10K tokens < 10ms, 100 chunks < 20ms)

**Documentation:**
- [ ] Add XML comments to all public interfaces and classes
- [ ] Document configuration options in appsettings.json
- [ ] Add usage examples to README
- [ ] Document error codes and resolutions

---

### Rollout Plan

**Phase 1: Token Counting (Week 1)**
- Implement `TiktokenCounter` with tiktoken library integration
- Add content hashing for cache keys
- Implement cache size limit (10,000 entries)
- Write unit tests for empty string, single word, code blocks, unicode
- **Verification:** Run benchmark → 10,000 tokens counted in <10ms

**Phase 2: Budget Management (Week 2)**
- Implement `BudgetManager` with allocation calculation
- Add category tracking (consumed vs allocated)
- Implement budget enforcement (CanFit, Consume)
- Write unit tests for various allocation scenarios
- **Verification:** Configure 40/30/20/10 split → verify category budgets correct

**Phase 3: Exact Deduplication (Week 3)**
- Implement `ExactDeduplicator` with SHA-256 content hashing
- Add stable sorting (rank → source → path → line)
- Implement deterministic first-wins strategy
- Write unit tests for duplicates across different sources
- **Verification:** Feed 100 chunks with 20 duplicates → 80 unique chunks returned

**Phase 4: Overlap Deduplication (Week 4)**
- Implement overlap detection (line range intersection)
- Add configurable overlap threshold (default 0.80)
- Implement chunk merging (union of line ranges)
- Write unit tests for adjacent, contained, partial overlaps
- **Verification:** Merge UserService.cs:1-50 + UserService.cs:25-75 → UserService.cs:1-75

**Phase 5: Selection (Week 5)**
- Implement `BudgetSelector` with category-based selection
- Add rank-ordered selection within categories
- Integrate with BudgetManager for consumption tracking
- Write integration tests for end-to-end pipeline
- **Verification:** Select from 247 chunks → all selected chunks fit within 90,000 token budget

**Phase 6: Reporting (Week 6)**
- Implement `BudgetReport` generation
- Add category usage statistics (allocated/used/utilization)
- Add deduplication savings tracking
- Integrate reporting into BudgetManager
- **Verification:** Run report → shows accurate category breakdown and dedup savings

**Phase 7: Integration & Performance (Week 7)**
- Integrate with Task 016 (Context Packer)
- Run performance benchmarks (1000 chunks in <50ms)
- Optimize cache hit rate (target 50%+)
- Add debug logging for budget decisions
- **Verification:** End-to-end test → context assembly completes without overflow errors

---

**End of Task 016.c Specification**