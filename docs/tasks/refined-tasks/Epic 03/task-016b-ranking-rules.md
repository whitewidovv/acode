# Task 016.b: Ranking Rules

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 016 (Context Packer), Task 016.a (Chunking)  

---

## Description

### Business Value

Ranking is the intelligence that determines which code chunks matter most for the agent's current task. With potentially thousands of chunks across a codebase, the agent cannot include everything in its context window. Task 016.b delivers the prioritization logic that ensures the most relevant, useful chunks appear first—maximizing the value of every token spent on context.

Effective ranking directly impacts agent response quality. When the agent is asked to modify a function, the ranking system must surface that function's code, its callers, its tests, and related interfaces—in priority order. Poor ranking buries critical context below less relevant code, forcing the LLM to work with incomplete information and producing lower-quality responses.

The multi-factor ranking approach recognizes that "relevance" is complex. A chunk might be relevant because it matched a search query, because the user has the file open, because it was recently modified, or because it's in a critical path of the codebase. By combining these factors with configurable weights, the ranking system adapts to different workflows and use cases.

### Scope

This task defines the complete ranking subsystem for the Context Packer:

1. **Relevance Scoring:** Calculate how well each chunk matches the current query or task based on search scores and keyword overlap.

2. **Source Priority Scoring:** Prioritize chunks based on their origin—tool results rank higher than search results, which rank higher than indirect references.

3. **Recency Scoring:** Factor in how recently files were modified or accessed, with configurable time decay.

4. **Position Scoring:** Boost chunks based on their position in files (headers, class declarations) and relationship to other selected chunks.

5. **Combined Ranking Engine:** Weighted combination of all factors producing a final score for deterministic ordering.

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Task 016 (Context Packer) | Parent System | Ranker is invoked by Context Packer to prioritize chunks |
| Task 016.a (Chunking) | Upstream | Receives chunks with metadata for ranking |
| Task 016.c (Budgeting) | Downstream | Ranked chunks passed to budget selector |
| Task 002 (Config) | Configuration | Ranking weights and settings from `.agent/config.yml` |
| Task 015 (Indexing) | Metadata Source | File modification times and access patterns from index |
| Task 011 (Session) | Context Source | Current task context for relevance scoring |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Missing relevance signal | Cannot score by relevance | Default to neutral score (0.5), rely on other factors |
| Invalid weight configuration | Weights don't sum to 1.0 | Normalize weights automatically, log warning |
| Missing file metadata | Cannot score recency | Default to neutral score, warn in debug output |
| Pattern matching error | Boost/penalty not applied | Validate patterns at config load, skip invalid patterns |
| Tie-breaking inconsistency | Non-deterministic order | Strict tie-breaking rules: score → source → path → line |
| Score overflow | Numeric instability | Clamp scores to 0-1 range after all adjustments |
| Empty chunk set | No results to rank | Return empty result immediately, no error |
| Circular boost/penalty | Unexpected score behavior | Apply boosts and penalties in single pass only |

### Assumptions

1. All chunks have associated metadata (source, path, line numbers)
2. Search scores, when present, are normalized to 0-1 range
3. File modification times are available from the file system or index
4. Boost and penalty patterns use standard glob syntax
5. Weight configuration is validated at startup
6. The ranking algorithm is deterministic for identical inputs
7. Performance is acceptable for up to 10,000 chunks
8. Debug output is available for ranking troubleshooting

### Security Considerations

1. **Path Pattern Safety:** Boost and penalty patterns must be validated to prevent regex denial-of-service attacks.

2. **Information Leakage:** Ranking debug output must not expose sensitive file paths outside the repository.

3. **Configuration Tampering:** Ranking weights should be validated to prevent malicious configurations that could prioritize dangerous code.

4. **Deterministic Output:** Ranking must be deterministic to enable auditing and reproducibility.

5. **No External Calls:** Ranking must be purely computational with no external service dependencies.

### Return on Investment (ROI)

**Problem Cost (Without Intelligent Ranking):**

When chunks are ranked poorly or randomly, the agent wastes context tokens on irrelevant code and misses critical context. This manifests as:

- **Incorrect Code Changes:** Agent modifies the wrong function or file because the right one wasn't in context → 45 min debugging/reverting per incident
- **Incomplete Context:** Agent implements feature without seeing test files, breaking existing tests → 30 min to fix + run CI again
- **Repeated Queries:** User must manually guide agent to correct files because relevance ranking failed → 15 min per iteration, 3-5 iterations typical
- **Context Window Waste:** 40% of context filled with low-value chunks (generated files, distant dependencies, irrelevant code)

**Cost Calculation (10-person dev team, 250 work days/year):**

| Issue | Frequency | Time Cost | Annual Cost |
|-------|-----------|-----------|-------------|
| Incorrect code changes (debugging) | 1.5/week/dev | 45 min × 1.5 × 10 devs × 50 weeks | **562.5 hours** |
| Incomplete context (broken tests) | 2/week/dev | 30 min × 2 × 10 devs × 50 weeks | **500 hours** |
| Repeated queries (manual guidance) | 8/week/dev | 15 min × 3 iterations × 8 × 10 devs × 50 weeks | **3,000 hours** |
| Context waste (re-running with better prompt) | 5/week/dev | 10 min × 5 × 10 devs × 50 weeks | **416.7 hours** |
| **Total Annual Cost** | | | **4,479.2 hours** |

At $150/hour blended dev rate: **$671,880/year**

**Benefit (With Multi-Factor Ranking):**

Intelligent ranking surfaces the right code first:

- **Relevance scoring:** Ensures chunks matching the query appear first (90% reduction in wrong-file incidents)
- **Source priority:** Tool results and open files prioritized over distant references (85% reduction in incomplete context)
- **Recency scoring:** Recently modified code surfaces higher (relevant for "what changed" queries, reduces manual guidance by 75%)
- **Position scoring:** Class headers and imports appear before implementation details (better context structure, 60% fewer re-runs)

**Reduction in Problem Costs:**

| Issue | Reduction | Hours Saved | Cost Saved |
|-------|-----------|-------------|------------|
| Incorrect code changes | 90% | 506.3 hours | $75,945 |
| Incomplete context | 85% | 425 hours | $63,750 |
| Repeated queries | 75% | 2,250 hours | $337,500 |
| Context waste | 60% | 250 hours | $37,500 |
| **Total Annual Savings** | | **3,431.3 hours** | **$514,695** |

**Implementation Cost:**

- Development: 32 hours (ranking engine, scoring factors, configuration)
- Testing: 16 hours (unit tests, integration tests, performance tests)
- Documentation: 4 hours
- **Total:** 52 hours @ $150/hour = **$7,800**

**ROI Calculation:**

- Annual Savings: $514,695
- Implementation Cost: $7,800
- **Net Annual Benefit:** $506,895
- **ROI:** 6,499%
- **Payback Period:** 5.5 days

### Impact Metrics: Before vs. After

| Metric | Before (Random/Simple Ranking) | After (Multi-Factor Ranking) | Improvement |
|--------|-------------------------------|------------------------------|-------------|
| Correct file in top 5 results | 45% | 94% | **2.1x better** |
| Critical context within budget | 52% | 91% | **1.75x better** |
| Context re-runs needed | 3.2 per task | 0.5 per task | **6.4x fewer** |
| Avg time to find right code | 12.5 min | 1.8 min | **6.9x faster** |
| Context window utilization | 60% relevant | 89% relevant | **48% improvement** |
| Agent response accuracy | 67% | 91% | **36% improvement** |
| Manual file selection needed | 65% of queries | 12% of queries | **5.4x reduction** |

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                         RANKING PIPELINE                            │
└─────────────────────────────────────────────────────────────────────┘

INPUT: Chunk Set (from Task 016a) + Query Context
│
│  ┌────────────────────────────────────────────┐
│  │  ContentChunk[]                            │
│  │  - FilePath, Content, LineStart, LineEnd   │
│  │  - TokenEstimate, Type, Hierarchy          │
│  │  - Source (ToolResult/OpenFile/Search/Ref) │
│  └────────────────────────────────────────────┘
│
▼
┌─────────────────────────────────────────────────────────────────────┐
│                      FACTOR SCORING (Parallel)                      │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐    │
│  │ RelevanceScorer │  │  SourceScorer   │  │  RecencyScorer  │    │
│  ├─────────────────┤  ├─────────────────┤  ├─────────────────┤    │
│  │ • Query match   │  │ • Tool: 100     │  │ • File mtime    │    │
│  │ • Keyword TF-IDF│  │ • Open: 80      │  │ • Time decay    │    │
│  │ • Term overlap  │  │ • Search: 60    │  │ • Half-life: 24h│    │
│  │ → 0.0 - 1.0     │  │ • Ref: 40       │  │ → 0.0 - 1.0     │    │
│  └─────────────────┘  │ → 0.0 - 1.0     │  └─────────────────┘    │
│                       └─────────────────┘                          │
│                                                                     │
│  ┌─────────────────┐  ┌─────────────────┐                          │
│  │ PositionScorer  │  │  BoostPenalty   │                          │
│  ├─────────────────┤  ├─────────────────┤                          │
│  │ • Top-of-file   │  │ • Path patterns │                          │
│  │ • Class headers │  │ • File types    │                          │
│  │ • Proximity     │  │ • Session rules │                          │
│  │ → 0.0 - 1.0     │  │ • Multipliers   │                          │
│  └─────────────────┘  └─────────────────┘                          │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
│
│  Each chunk now has 4 factor scores: [relevance, source, recency, position]
│
▼
┌─────────────────────────────────────────────────────────────────────┐
│                        WEIGHTED COMBINATION                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│   FinalScore = (relevance × W_rel) + (source × W_src) +            │
│                (recency × W_rec) + (position × W_pos)              │
│                                                                     │
│   Default Weights (configurable in .agent/config.yml):             │
│   • W_rel = 0.50  (relevance is most important)                    │
│   • W_src = 0.25  (source priority matters)                        │
│   • W_rec = 0.15  (recency helps)                                  │
│   • W_pos = 0.10  (position is least critical)                     │
│                                                                     │
│   Apply boosts/penalties (multiplicative):                         │
│   • FinalScore *= boost_factor  (e.g., 1.2 for /src/core/*)        │
│   • FinalScore *= penalty_factor (e.g., 0.5 for **/tests/**)       │
│                                                                     │
│   Clamp to [0.0, 1.0] range                                         │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
│
│  Each chunk now has: FinalScore (0.0 - 1.0)
│
▼
┌─────────────────────────────────────────────────────────────────────┐
│                      SORTING & TIE-BREAKING                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│   Primary:   Sort by FinalScore DESC                                │
│   Secondary: If scores equal, sort by SourcePriority DESC          │
│   Tertiary:  If still equal, sort by FilePath ASC (alphabetical)   │
│   Final:     If still equal, sort by LineStart ASC                 │
│                                                                     │
│   → Deterministic ordering for identical inputs                     │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
│
│  Chunks now in priority order
│
▼
┌─────────────────────────────────────────────────────────────────────┐
│                     THRESHOLD FILTERING (Optional)                  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│   If MinScore configured (e.g., 0.25):                              │
│   • Remove chunks where FinalScore < MinScore                       │
│   • Prevents very low-quality chunks from wasting tokens            │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
│
▼
OUTPUT: RankedChunk[] (sorted, filtered, ready for budgeting)
│
│  Passed to Task 016c (Token Budgeting & Deduplication)
│
▼
┌─────────────────────────────────────────────────────────────────────┐
│             CONTEXT PACKER (Task 016) Final Assembly                │
└─────────────────────────────────────────────────────────────────────┘
```

**Data Flow Example:**

```
Chunk A: UserService.cs, lines 45-78, "GetUserById method"
  - Relevance: 0.92 (exact query match for "GetUserById")
  - Source: 1.0 (from tool result - definition lookup)
  - Recency: 0.65 (modified 8 hours ago, decay applied)
  - Position: 0.70 (mid-file, part of public API)
  → FinalScore = (0.92×0.5) + (1.0×0.25) + (0.65×0.15) + (0.70×0.10)
  → FinalScore = 0.46 + 0.25 + 0.0975 + 0.07 = 0.8775

Chunk B: UserService.cs, lines 1-20, "using statements + class header"
  - Relevance: 0.45 (indirect match - same file, different content)
  - Source: 0.60 (from search result)
  - Recency: 0.65 (same file modification time)
  - Position: 0.95 (top-of-file, class declaration)
  → FinalScore = (0.45×0.5) + (0.60×0.25) + (0.65×0.15) + (0.95×0.10)
  → FinalScore = 0.225 + 0.15 + 0.0975 + 0.095 = 0.5675

Chunk C: UserServiceTests.cs, lines 120-145, "GetUserById test"
  - Relevance: 0.88 (strong query match - test for target method)
  - Source: 0.40 (from reference lookup - test → implementation)
  - Recency: 0.30 (modified 3 days ago)
  - Position: 0.60 (mid-file test method)
  - Penalty: 0.7 (test file penalty configured at 0.7x)
  → FinalScore = [(0.88×0.5) + (0.40×0.25) + (0.30×0.15) + (0.60×0.10)] × 0.7
  → FinalScore = [0.44 + 0.10 + 0.045 + 0.06] × 0.7 = 0.645 × 0.7 = 0.4515

Final Ranking: A (0.8775) → B (0.5675) → C (0.4515)
```

### Architectural Decisions

#### Decision 1: Multi-Factor Weighted Scoring vs. Single-Factor Ranking

**Context:** How should we determine chunk priority when multiple signals are available (relevance, source, recency, position)?

**Options Considered:**

1. **Single-Factor (Relevance Only):** Rank purely by query match score
2. **Sequential Filtering:** Apply filters in order (source → relevance → recency)
3. **Multi-Factor Weighted Combination:** Compute weighted sum of all factors

**Decision:** Multi-Factor Weighted Combination

**Rationale:**

Multi-factor weighted scoring provides the best balance of flexibility and accuracy:

- **Captures Nuance:** A chunk that's moderately relevant but from a tool result (high source priority) should often rank higher than a highly relevant chunk from a distant reference. Weighted combination captures this.
- **Configurable:** Different workflows benefit from different weights (debugging favors recency, feature development favors relevance). Configuration allows tuning.
- **Smooth Degradation:** If one factor is missing (e.g., no recency data), other factors compensate. Sequential filtering fails hard when early filters remove everything.
- **Interpretable:** Final score is transparent sum of weighted factors, easier to debug than ML black box.

**Trade-offs:**

- ✅ **Pro:** Flexible, configurable, handles missing data gracefully
- ✅ **Pro:** More accurate than single-factor for complex scenarios
- ✅ **Pro:** Weights can be tuned per-user or per-workflow
- ❌ **Con:** More complex than single-factor ranking
- ❌ **Con:** Requires weight tuning (though defaults work for 90% of cases)
- ❌ **Con:** Slightly slower than simple relevance sort (50ms vs 5ms for 1000 chunks)

**Alternatives Rejected:**

- **Single-Factor:** Too simplistic, ignores valuable signals (source, recency)
- **Sequential Filtering:** Brittle, order-dependent, can filter out all results
- **ML-Based Ranking:** Overkill, requires training data, not deterministic

#### Decision 2: Configurable Weights vs. Fixed Weights

**Context:** Should factor weights be configurable or hardcoded?

**Options Considered:**

1. **Fixed Weights:** Hardcode default weights (0.5, 0.25, 0.15, 0.10)
2. **Configurable Weights:** Allow users to override weights in `.agent/config.yml`
3. **Adaptive Weights:** Automatically adjust weights based on query type

**Decision:** Configurable Weights with Sensible Defaults

**Rationale:**

Configurable weights provide flexibility for different workflows while maintaining simplicity:

- **Debugging Workflow:** Users debugging production issues may want higher recency weight (0.30) to surface recently changed code
- **Feature Development:** Users implementing new features may want higher relevance weight (0.60) and lower recency (0.05)
- **Code Review:** Users reviewing PRs may want higher source weight (0.40) for open files and tool results
- **Default Works for Most:** 80% of users never change weights, defaults are tuned for general-purpose coding

**Trade-offs:**

- ✅ **Pro:** Flexible for different workflows and user preferences
- ✅ **Pro:** Power users can optimize for their specific use cases
- ✅ **Pro:** Easy to experiment and find optimal weights
- ❌ **Con:** Configuration complexity (though defaults minimize this)
- ❌ **Con:** Invalid configurations possible (weights don't sum to 1.0 - mitigated by auto-normalization)
- ❌ **Con:** More documentation needed to explain weights

**Alternatives Rejected:**

- **Fixed Weights:** Too rigid, doesn't accommodate different workflows
- **Adaptive Weights:** Too complex, requires query classification, not transparent

#### Decision 3: Time-Based Decay for Recency vs. Binary Recent/Old

**Context:** How should file modification time affect ranking?

**Options Considered:**

1. **Binary Recent/Old:** Files modified within 24 hours get score 1.0, older files get 0.0
2. **Linear Decay:** Score decreases linearly from 1.0 (now) to 0.0 (365 days ago)
3. **Exponential Decay:** Score decays exponentially with configurable half-life

**Decision:** Exponential Decay with Configurable Half-Life (Default: 24 hours)

**Rationale:**

Exponential decay reflects human intuition about recency:

- **Natural Relevance Curve:** A file modified 1 hour ago feels "very recent", 12 hours ago "somewhat recent", 3 days ago "old". Exponential decay models this.
- **Smooth Gradient:** Unlike binary, provides smooth score gradient (avoids cliff at 24-hour boundary)
- **Configurable:** Half-life can be tuned (8 hours for fast-moving projects, 7 days for stable codebases)
- **Mathematical Simplicity:** `score = 0.5 ^ (hours_since_modified / half_life_hours)` is simple and efficient

**Trade-offs:**

- ✅ **Pro:** Intuitive, matches human perception of recency
- ✅ **Pro:** Smooth gradient, no artificial boundaries
- ✅ **Pro:** Configurable half-life allows tuning
- ❌ **Con:** More complex than binary (though implementation is simple)
- ❌ **Con:** Requires file modification time metadata (gracefully degrades if missing)

**Alternatives Rejected:**

- **Binary Recent/Old:** Too coarse, creates artificial cliff at boundary
- **Linear Decay:** Doesn't match human intuition (file from 6 months ago shouldn't score 0.5)

#### Decision 4: Path-Based Boosts/Penalties vs. No Path Adjustments

**Context:** Should certain paths or file types receive score adjustments?

**Options Considered:**

1. **No Path Adjustments:** Pure factor-based scoring, no special treatment
2. **Hardcoded Boosts/Penalties:** Hardcode rules like "penalize **/tests/** by 0.5x"
3. **Configurable Boosts/Penalties:** Allow glob patterns with multipliers in config

**Decision:** Configurable Boosts/Penalties via Glob Patterns

**Rationale:**

Path-based adjustments reflect real-world code organization and priorities:

- **Test Files:** Often match queries strongly (test name mirrors implementation) but are lower priority for implementation tasks → penalty makes sense
- **Core Infrastructure:** Files in `/src/core/` or `/src/domain/` are often more important than utilities → boost makes sense
- **Generated Code:** Auto-generated files should typically rank lower → penalty makes sense
- **Project-Specific:** Different projects have different conventions, configuration allows customization

**Example Configuration:**

```yaml
ranking:
  boosts:
    - pattern: "src/core/**/*.cs"
      factor: 1.3
    - pattern: "src/domain/**/*.cs"
      factor: 1.2
  penalties:
    - pattern: "**/tests/**"
      factor: 0.7
    - pattern: "**/*.g.cs"
      factor: 0.3
    - pattern: "obj/**"
      factor: 0.1
```

**Trade-offs:**

- ✅ **Pro:** Handles real-world code organization patterns
- ✅ **Pro:** Configurable per-project
- ✅ **Pro:** Prevents test files from dominating results when inappropriate
- ❌ **Con:** Configuration complexity
- ❌ **Con:** Glob pattern validation needed (ReDoS mitigation)
- ❌ **Con:** Can be misused (over-boosting can bury relevant results)

**Alternatives Rejected:**

- **No Path Adjustments:** Ignores valuable organizational signals
- **Hardcoded Boosts/Penalties:** Too rigid, doesn't accommodate different project structures

#### Decision 5: Deterministic Tie-Breaking vs. Undefined Order

**Context:** When two chunks have identical final scores, how should they be ordered?

**Options Considered:**

1. **Undefined Order:** Accept non-deterministic ordering for ties
2. **Insertion Order:** Maintain original chunk order for ties
3. **Deterministic Tie-Breaking:** Use cascading rules (score → source → path → line)

**Decision:** Deterministic Tie-Breaking (score → source → path → line)

**Rationale:**

Deterministic ordering is critical for reproducibility and debugging:

- **Auditability:** For security audit logs, results must be reproducible given identical inputs
- **Testing:** Unit tests can assert exact order, catching ranking regressions
- **Debugging:** When ranking seems wrong, deterministic order allows step-through debugging
- **User Experience:** Consistent results for identical queries feels more "correct" to users

**Tie-Breaking Rules:**

1. **Primary:** Sort by FinalScore (descending)
2. **Secondary:** If scores equal, sort by SourcePriority (descending)
3. **Tertiary:** If still equal, sort by FilePath (ascending, alphabetical)
4. **Final:** If still equal, sort by LineStart (ascending)

**Trade-offs:**

- ✅ **Pro:** Fully deterministic, reproducible results
- ✅ **Pro:** Enables regression testing
- ✅ **Pro:** Supports audit requirements
- ✅ **Pro:** Better UX (consistent results)
- ❌ **Con:** Slightly more complex sorting logic
- ❌ **Con:** Negligible performance impact (10-15ms vs 8-10ms for 1000 chunks)

**Alternatives Rejected:**

- **Undefined Order:** Non-deterministic, breaks auditability
- **Insertion Order:** Still somewhat arbitrary, doesn't prioritize by meaningful signal

---

## Use Cases

### Use Case 1: Elena Rodriguez – Debugging Production Payment Failure

**Persona:** Elena Rodriguez, Senior Backend Developer at FinTech startup (8 years experience)

**Context:** Production alert at 2:47 AM – payment processing failing for 15% of transactions. Elena needs to identify root cause quickly before revenue impact escalates.

**Query to Agent:** "Why are payments failing in ProductCheckoutHandler? Show me the payment flow."

**Before (Random/Simple Ranking):**

1. Elena asks the agent about payment failures
2. Agent receives 847 chunks from search across the codebase
3. **Poor ranking surfaces irrelevant chunks first:**
   - Chunk 1: `PaymentHandlerTests.cs` (test file, outdated tests from 6 months ago)
   - Chunk 2: `LegacyPaymentGateway.cs` (deprecated code, not used in current flow)
   - Chunk 3: `PaymentDTO.cs` (data model, no business logic)
   - Chunk 4: `README.md` mentioning payment system
   - Chunk 5: `PaymentController.cs` (HTTP layer, not the failure point)
4. Critical chunks buried at position 47-52:
   - `ProductCheckoutHandler.cs` (actual failure location - position 47)
   - `PaymentValidator.cs` (validation logic changed yesterday - position 51)
   - Recent commit adding new validation rule - position 89
5. Elena spends 8 minutes reading agent responses with wrong context
6. Realizes agent is looking at old code, manually specifies file: "Look at ProductCheckoutHandler.cs lines 120-150"
7. Agent now has correct file but missing `PaymentValidator.cs` dependency
8. Another 6 minutes, Elena manually adds: "Also check PaymentValidator.cs"
9. Agent finally sees the bug: new validation rule rejecting valid card types
10. **Total time to identify bug: 18 minutes**
11. **Manual interventions: 3 (specify file twice, add dependency)**
12. **Context window waste: 65% filled with irrelevant chunks**

**After (Multi-Factor Ranking):**

1. Elena asks the agent about payment failures
2. Agent receives 847 chunks from search across the codebase
3. **Intelligent ranking surfaces critical chunks first:**
   - **Chunk 1:** `ProductCheckoutHandler.cs` lines 120-180 (score: 0.91)
     - Relevance: 0.95 (exact match for "ProductCheckoutHandler")
     - Source: 1.0 (from tool result - definition lookup)
     - Recency: 0.82 (modified 14 hours ago - yesterday's deploy)
     - Position: 0.75 (contains method "ProcessPayment")
   - **Chunk 2:** `PaymentValidator.cs` lines 45-89 (score: 0.87)
     - Relevance: 0.88 (called by ProductCheckoutHandler, matches "payment" + "fail")
     - Source: 0.80 (from reference - called by chunk 1)
     - Recency: 0.94 (modified 16 hours ago - same deploy)
     - Position: 0.70 (validation logic)
   - **Chunk 3:** Git commit diff (score: 0.79)
     - Shows validation rule change in PaymentValidator
     - Recency: 0.95 (committed yesterday)
   - **Chunk 4:** `PaymentValidator.cs` lines 1-20 (score: 0.68)
     - Position: 0.95 (class header, using statements - context)
   - **Chunk 5:** `IPaymentGateway.cs` interface (score: 0.64)
     - Referenced by ProductCheckoutHandler
4. Agent immediately sees the bug in first response: new validation rule in PaymentValidator rejecting card types "AMEX_CORPORATE" (added yesterday)
5. Agent shows exact line: `if (!AllowedCardTypes.Contains(cardType))` - missing AMEX_CORPORATE in allowed list
6. **Total time to identify bug: 1.2 minutes**
7. **Manual interventions: 0**
8. **Context window utilization: 91% relevant chunks**

**Impact:**

- **Time savings:** 18 min → 1.2 min = **15x faster**
- **Accuracy:** Bug identified in first response (vs. 3 iterations)
- **Revenue protection:** 16.8 min faster × $12,000/min revenue loss = **$201,600 saved** (for this single incident)
- **Developer experience:** No frustration, immediate answer, trust in agent increases

**Key Ranking Factors:**

- **Recency** scored recently modified files higher (deployment from yesterday)
- **Source priority** ensured tool results (definition of ProductCheckoutHandler) ranked above distant references
- **Relevance** matched query terms precisely
- **Position** surfaced class headers and method definitions before implementation details

---

### Use Case 2: Marcus Kim – Implementing User Profile Photo Upload

**Persona:** Marcus Kim, Junior Full-Stack Developer (1 year experience)

**Context:** Marcus is tasked with adding profile photo upload to the user settings page. He needs to understand existing upload patterns and security requirements.

**Query to Agent:** "How do I implement file upload securely? Show me examples in the codebase."

**Before (Random/Simple Ranking):**

1. Marcus asks about file upload implementation
2. Agent receives 623 chunks mentioning "upload", "file", "storage"
3. **Poor ranking shows scattered, inconsistent examples:**
   - Chunk 1: `UploadController.cs` from legacy admin panel (deprecated pattern, no validation)
   - Chunk 2: `FileUploadTests.cs` (test file, but testing old implementation)
   - Chunk 3: Blog post markdown mentioning file uploads
   - Chunk 4: `package.json` with file upload dependency (not helpful for backend)
   - Chunk 5: `ImageUploadService.cs` from different feature (correct pattern, but buried)
4. Agent suggests pattern from deprecated UploadController (no file type validation, no size limits)
5. Marcus implements following agent's guidance
6. **Security issue:** No MIME type validation, accepts any file
7. Code review flags 3 security issues: missing validation, no size limit, path traversal risk
8. Marcus spends 45 minutes fixing security issues
9. Re-submits for review
10. **Total time: 2.5 hours (implementation + fixes)**
11. **Rework cycles: 2**
12. **Security issues introduced: 3**

**After (Multi-Factor Ranking):**

1. Marcus asks about file upload implementation
2. Agent receives 623 chunks mentioning "upload", "file", "storage"
3. **Intelligent ranking surfaces best examples first:**
   - **Chunk 1:** `DocumentUploadService.cs` lines 34-120 (score: 0.89)
     - Relevance: 0.92 (exact match, "upload" + "secure" + "file")
     - Source: 1.0 (tool result - recent search for upload patterns)
     - Recency: 0.78 (modified 2 weeks ago - current pattern)
     - Position: 0.80 (complete upload method with validation)
     - **Boost:** 1.2x for `/src/core/**` path (core infrastructure)
   - **Chunk 2:** `FileValidationService.cs` lines 18-67 (score: 0.84)
     - Called by DocumentUploadService
     - Shows MIME type validation, size limits, allowed extensions
     - Recency: 0.81 (same commit as DocumentUploadService)
   - **Chunk 3:** `StorageOptions.cs` configuration (score: 0.76)
     - Security configuration: max file size, allowed types
   - **Chunk 4:** `DocumentUploadServiceTests.cs` security tests (score: 0.71)
     - Shows security test cases: malicious MIME, oversized files, path traversal attempts
     - **Penalty:** 0.7x for test files (lower priority but still relevant)
   - **Chunk 5:** `IStorageProvider.cs` interface (score: 0.68)
     - Abstraction for cloud storage
4. Agent responds with complete secure pattern:
   - MIME type validation from FileValidationService
   - File size limits from StorageOptions
   - Path sanitization
   - Virus scanning hook (from DocumentUploadService)
5. Marcus implements following the secure pattern
6. Code review: **No security issues**, approved immediately
7. **Total time: 35 minutes (implementation only)**
8. **Rework cycles: 0**
9. **Security issues: 0**

**Impact:**

- **Time savings:** 2.5 hours → 35 min = **4.3x faster**
- **Rework elimination:** 2 cycles → 0 = 100% reduction
- **Security:** 3 vulnerabilities prevented
- **Learning:** Marcus now understands secure upload patterns (better educational outcome)
- **Code quality:** First submission is production-ready

**Key Ranking Factors:**

- **Source priority** favored recent tool results over old search results
- **Recency** ensured current patterns (2 weeks old) ranked above deprecated code (2 years old)
- **Path boost** prioritized `/src/core/` infrastructure code over scattered examples
- **Test penalty** kept test files lower (but still in context for learning)

---

### Use Case 3: Sarah Thompson – Reviewing Pull Request for API Rate Limiting

**Persona:** Sarah Thompson, Tech Lead (12 years experience)

**Context:** Sarah is reviewing a 347-line PR adding rate limiting to API endpoints. She needs to verify the implementation follows existing patterns and doesn't introduce edge cases.

**Query to Agent:** "Review this rate limiting implementation. Does it match our existing middleware patterns?"

**Before (Random/Simple Ranking):**

1. Sarah opens PR, asks agent to review rate limiting code
2. Agent receives 1,247 chunks (PR files + codebase context)
3. **Poor ranking focuses on wrong aspects:**
   - Chunk 1: `RateLimitMiddleware.cs` from PR (correct, but missing context)
   - Chunk 2: Random controller using old rate limiting attribute (deprecated)
   - Chunk 3: NuGet package documentation for rate limiting library
   - Chunk 4: Test file for different middleware
   - Chunk 5: `Startup.cs` from 6 months ago (outdated DI pattern)
   - **Missing:** Current authentication middleware pattern (should inform review)
   - **Missing:** Existing `CacheService.cs` used for distributed rate limit state
   - **Missing:** Recent security audit recommendation about rate limit bypasses
4. Agent responds: "Implementation looks good, follows standard middleware pattern"
5. Sarah notices agent missed distributed cache integration (PR uses in-memory, wrong for multi-instance deployment)
6. Sarah manually adds: "Check how AuthenticationMiddleware uses distributed cache"
7. Agent now sees the issue
8. Sarah also notices missing integration with existing `CacheService.cs`
9. **Total review time: 22 minutes**
10. **Issues found by agent: 0 (Sarah found 2 manually)**
11. **Manual prompts needed: 2**

**After (Multi-Factor Ranking):**

1. Sarah opens PR, asks agent to review rate limiting code
2. Agent receives 1,247 chunks (PR files + codebase context)
3. **Intelligent ranking provides comprehensive context:**
   - **Chunk 1:** `RateLimitMiddleware.cs` from PR (score: 0.94)
     - Relevance: 0.98 (exact file being reviewed)
     - Source: 1.0 (open file in editor)
     - Recency: 1.0 (current PR)
     - Position: 0.85
   - **Chunk 2:** `AuthenticationMiddleware.cs` existing pattern (score: 0.88)
     - Relevance: 0.85 (similar middleware type, pattern reference)
     - Source: 0.60 (search result for "middleware pattern")
     - Recency: 0.76 (modified 1 month ago - current pattern)
     - Position: 0.80 (complete middleware implementation)
     - **Boost:** 1.2x for `/src/core/middleware/**` path
   - **Chunk 3:** `CacheService.cs` distributed cache (score: 0.82)
     - Relevance: 0.78 ("cache" mentioned in PR, critical dependency)
     - Source: 0.80 (reference from AuthenticationMiddleware)
     - Recency: 0.72 (modified 6 weeks ago)
     - Shows distributed Redis cache pattern
   - **Chunk 4:** Security audit doc - rate limit bypass (score: 0.76)
     - Relevance: 0.88 ("rate limit" exact match)
     - Recency: 0.92 (audit from 2 weeks ago)
     - **Boost:** 1.3x for security documentation
   - **Chunk 5:** `RateLimitMiddlewareTests.cs` from PR (score: 0.70)
     - Shows test coverage
     - **Penalty:** 0.7x for test files
   - **Chunk 6:** `Startup.cs` DI registration pattern (score: 0.66)
     - Current DI pattern (recent file)
4. Agent responds with comprehensive review:
   - ✅ Middleware pattern matches AuthenticationMiddleware structure
   - ❌ **Issue 1:** Uses in-memory cache instead of distributed CacheService (multi-instance deployment will have inconsistent limits)
   - ❌ **Issue 2:** Missing bypass prevention from security audit (IP spoofing via X-Forwarded-For header not validated)
   - ⚠️ **Suggestion:** Add distributed tracing correlation (pattern from AuthenticationMiddleware)
5. Sarah confirms agent's findings, adds review comments to PR
6. **Total review time: 4 minutes**
7. **Issues found by agent: 2 (both critical)**
8. **Manual prompts needed: 0**

**Impact:**

- **Time savings:** 22 min → 4 min = **5.5x faster**
- **Issue detection:** 0 automated → 2 automated = **critical bugs caught**
- **Review quality:** More thorough (agent saw security audit doc that Sarah would have missed)
- **Consistency:** Ensures PR matches existing patterns (AuthenticationMiddleware, CacheService)
- **Developer experience:** PR author gets detailed, constructive feedback faster

**Key Ranking Factors:**

- **Source priority** heavily weighted open files (PR being reviewed)
- **Path boost** prioritized core middleware patterns over scattered examples
- **Recency** surfaced recent security audit (2 weeks old) over outdated docs
- **Relevance** matched "rate limit" + "middleware" + "cache" across multiple files
- **Position** surfaced complete implementations over fragments

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| **Ranking** | The process of assigning priority order to code chunks based on multiple scoring factors to determine which chunks should appear first in the agent's context window. Ranking is deterministic (same inputs produce same order) and configurable (weights and boosts can be adjusted per-project). The ranking system is the core intelligence of the Context Packer, ensuring the most relevant code appears within the token budget. Higher-ranked chunks are more likely to be included when budget constraints apply. |
| **Relevance** | A scoring factor measuring how well a chunk matches the current query or task based on keyword overlap, search scores, and term frequency. Relevance is the most heavily weighted factor (default 50%) because matching the user's intent is critical. Relevance scoring uses techniques like TF-IDF to measure keyword importance and query term overlap to assess match quality. A chunk with high relevance directly answers the user's question. |
| **Recency** | A scoring factor based on how recently a file was modified or accessed, with exponential time decay to reflect diminishing importance over time. Recency is particularly valuable for debugging (recent changes often cause bugs) and "what changed" queries. The decay function uses a configurable half-life (default 24 hours) where a file's recency score halves every N hours. Files modified in the last hour score near 1.0, files from last week score near 0.1. |
| **Source Priority** | A scoring factor based on the origin of a chunk, prioritizing chunks from high-confidence sources like tool results (score 100) over lower-confidence sources like distant references (score 40). Source priority recognizes that a chunk from a "Go to Definition" tool result is more likely relevant than a chunk from a full-text search. Open files in the user's editor receive high source priority (80) because they represent current focus. This factor prevents low-quality search results from dominating context. |
| **Weight** | A configurable multiplier (0.0 to 1.0) applied to each scoring factor when computing the combined score, allowing customization of which factors matter most for different workflows. The four weights (relevance, source, recency, position) must sum to 1.0 and are validated at configuration load time. Default weights are: relevance 0.50, source 0.25, recency 0.15, position 0.10. Users can adjust weights for specific use cases (e.g., increase recency weight for debugging, increase relevance for feature work). |
| **Combined Score** | The final numeric score (0.0 to 1.0) for a chunk, computed as the weighted sum of all factor scores plus any boosts/penalties applied. The combined score determines a chunk's priority in the ranked list. The formula is: `(relevance × W_rel) + (source × W_src) + (recency × W_rec) + (position × W_pos)`, then multiplied by boost/penalty factors, then clamped to [0.0, 1.0]. Chunks are sorted by combined score in descending order (highest score first). |
| **Normalization** | The process of scaling raw scores to a consistent 0.0 to 1.0 range to enable fair comparison and combination across different scoring factors. Normalization is critical because raw relevance scores might range 0-100 while raw source priorities range 0-100 but in different scales. After normalization, all factor scores are in [0.0, 1.0] where 0.0 means "lowest possible" and 1.0 means "highest possible". This allows weighted combination to work correctly. |
| **Tie-Breaking** | The cascading set of rules used to deterministically order chunks with identical combined scores, ensuring reproducible results for auditing and testing. The tie-breaking sequence is: (1) sort by combined score descending, (2) if equal, sort by source priority descending, (3) if still equal, sort by file path ascending (alphabetical), (4) if still equal, sort by line number ascending. This guarantees that two chunks with score 0.75 always appear in the same order regardless of execution environment. |
| **Diversity** | An optional ranking consideration (not implemented in Task 016.b) that would ensure variety in results by penalizing multiple chunks from the same file or package. Diversity is out of scope for the initial ranking implementation because it adds complexity and can conflict with relevance (sometimes the best answer is multiple chunks from the same file). Future versions may add diversity as a configurable factor. |
| **Boost** | A multiplicative factor (e.g., 1.2x or 1.5x) applied to a chunk's combined score based on path patterns or file types, increasing its priority relative to other chunks. Boosts are configured via glob patterns in `.agent/config.yml` (e.g., `src/core/**/*.cs` gets 1.3x boost). Boosts are useful for prioritizing critical infrastructure code or domain layer code that's architecturally important. Multiple boosts can apply to a single chunk (they multiply together). |
| **Penalty** | A multiplicative factor less than 1.0 (e.g., 0.7x or 0.3x) applied to a chunk's combined score based on path patterns, decreasing its priority. Penalties are essential for de-prioritizing test files (which often match queries strongly but aren't the implementation), generated code (*.g.cs), and build artifacts (obj/**, bin/**). Penalties prevent noise from dominating search results. Like boosts, multiple penalties multiply together. |
| **Factor** | An individual scoring component (relevance, source, recency, position) that contributes to the combined score. Each factor is computed independently and normalized to [0.0, 1.0], then combined using configurable weights. Factors are designed to be orthogonal (measuring different aspects of chunk quality) so they provide complementary signals. Adding new factors in the future (e.g., code complexity, test coverage) would require extending the scoring pipeline. |
| **Aggregate** | The act of combining multiple factor scores into a single value, typically through weighted summation. In the ranking system, aggregation happens in the "Weighted Combination" stage where factor scores are multiplied by their weights and summed. Aggregation must preserve the relative importance of factors (via weights) while producing a single sortable score. The aggregation formula is linear and deterministic. |
| **Decay** | The mathematical function (exponential) that reduces a score over time, used in recency scoring to reflect diminishing relevance as files age. The decay function is: `score = 0.5 ^ (hours_since_modified / half_life_hours)`. With a 24-hour half-life, a file modified 24 hours ago scores 0.5, a file modified 48 hours ago scores 0.25, and a file modified 1 hour ago scores 0.97. Decay is configurable via the half-life parameter. |
| **Threshold** | A minimum combined score (e.g., 0.25) below which chunks are excluded from results entirely, preventing very low-quality chunks from wasting context tokens. The threshold is optional and configurable in `.agent/config.yml`. When enabled, chunks with `combined_score < threshold` are filtered out after ranking but before budgeting. This is useful when search returns hundreds of weak matches – the threshold removes noise. |
| **TF-IDF** | Term Frequency-Inverse Document Frequency, a numerical statistic used in relevance scoring to measure the importance of a keyword in a chunk relative to the entire codebase. TF (term frequency) measures how often a term appears in a chunk. IDF (inverse document frequency) measures how rare the term is across all chunks (rare terms are more discriminative). TF-IDF score = TF × IDF. This prevents common terms like "class" or "return" from dominating relevance scores while highlighting distinctive terms. |
| **Half-Life** | The time duration (in hours) after which a recency score decays to 50% of its original value, used to configure the exponential decay function for time-based scoring. A short half-life (8 hours) makes recency matter more for fast-moving projects where code changes rapidly. A long half-life (7 days) makes recency matter less for stable codebases where modification time is less indicative of relevance. The default half-life is 24 hours, meaning a file modified yesterday scores approximately 0.5 on recency. |
| **Deterministic Ordering** | The property that identical inputs (same chunks, same query, same configuration) always produce identical output order, critical for auditing, debugging, and regression testing. Deterministic ordering is achieved through: (1) deterministic scoring (no randomness, no time-of-day dependencies), (2) complete tie-breaking rules (all edge cases handled), (3) stable sort algorithm. Non-deterministic ranking would make debugging impossible (results change on each run) and break audit requirements. |
| **Score Clamping** | The operation of constraining a numeric score to a valid range (typically [0.0, 1.0]) by setting values below the minimum to the minimum and values above the maximum to the maximum. Clamping prevents numeric overflow or underflow when boosts and penalties are applied. For example, a chunk with combined score 0.92 and boost 1.3x would compute to 1.196, which is clamped to 1.0. Similarly, a score of -0.05 (theoretically impossible but possible with bugs) clamps to 0.0. Clamping ensures all scores are valid for sorting. |
| **Context Window** | The limited token budget available to the LLM for processing user queries and codebase context, typically 128,000 to 200,000 tokens for modern models. The context window is a hard constraint – if ranked chunks exceed the available budget, lower-ranked chunks are truncated. Effective ranking maximizes the value of every token in the context window by ensuring the most relevant chunks appear first (and thus are most likely to fit within budget). Poor ranking wastes context window space on irrelevant code. |

---

## Out of Scope

The following items are explicitly excluded from Task 016.b:

1. **Machine Learning-Based Ranking:** This task implements rule-based ranking with configurable weights. ML-based ranking (neural networks, gradient boosting, learning-to-rank models) is out of scope because it requires training data, labeled examples, and model infrastructure. ML would add significant complexity for marginal benefit given that rule-based ranking with proper tuning achieves 90%+ accuracy. Future tasks may explore ML ranking as an optional enhancement.

2. **Per-User Personalization:** All users share the same ranking configuration (weights, boosts, penalties). Per-user personalization (learning individual preferences, tracking which chunks users click, adapting to usage patterns) is out of scope because it requires user tracking, privacy considerations, and personalized state storage. Personalization conflicts with the deterministic audit requirement. Configuration allows team-level customization but not individual adaptation.

3. **Learning from User Feedback:** The ranking system does not adapt based on which chunks users find helpful or ignore. Feedback-based learning (implicit signals like dwell time, explicit signals like thumbs up/down) is out of scope because it introduces non-determinism and requires telemetry infrastructure. Static, configuration-driven ranking is simpler, more predictable, and auditable.

4. **Semantic Similarity Scoring:** This task uses keyword-based relevance (TF-IDF, term overlap). Semantic similarity via embeddings (vector search, cosine similarity of chunk embeddings) is out of scope and handled by Task 016.c (if needed). Embeddings require embedding models, vector databases, and significant storage overhead. Keyword-based scoring is sufficient for most queries and much faster (sub-millisecond vs 10-50ms per chunk).

5. **Natural Language Query Understanding:** The ranking system treats queries as keyword bags, not natural language. Advanced NLU (intent classification, entity extraction, query expansion, synonym handling) is out of scope. Queries like "Why is login failing?" are treated as keywords ["why", "login", "failing"] without understanding causality. NLU would require LLM calls or NLP pipelines, adding latency and complexity.

6. **Cross-File Chunk Relationships:** Ranking treats each chunk independently. Recognizing that chunks are related (e.g., class definition + all its methods should rank together, test + implementation should both be included) is out of scope. Relationship-aware ranking would require dependency graph analysis and chunking coordination. Task 016.b focuses on individual chunk scoring; relationships are a future enhancement.

7. **Dynamic Weight Adjustment:** Weights are static per configuration. Dynamically adjusting weights based on query type (e.g., use higher recency weight for "bug" queries, higher relevance for "implement" queries) is out of scope because it requires query classification and adds non-determinism. Users can manually create multiple configuration profiles for different workflows, but automatic adjustment is not included.

8. **Diversity Constraints:** Ensuring result diversity (e.g., "include chunks from at least 3 different files", "don't return more than 5 chunks from the same file") is out of scope. Diversity can conflict with relevance (sometimes the best answer is 10 chunks from the same file). Enforcing diversity requires post-processing and may exclude highly relevant chunks. Future versions may add optional diversity constraints.

9. **Real-Time Re-Ranking:** Once chunks are ranked, the order is fixed. Real-time re-ranking based on user actions (e.g., re-rank remaining chunks after user selects one, boost chunks related to selected chunk) is out of scope. Re-ranking would require stateful interaction tracking and multiple ranking passes. The initial ranking is deterministic and complete.

10. **Explanation Generation:** The ranking system does not generate human-readable explanations of why a chunk ranked highly (e.g., "This chunk ranked #1 because: relevance 0.95, source priority 1.0, recency 0.82"). Explanation generation is out of scope but could be added as a debug feature. The scoring is transparent (all factors visible) but not automatically narrated.

11. **A/B Testing Infrastructure:** The ranking system does not support A/B testing of different ranking strategies (e.g., 50% of users get weight set A, 50% get weight set B, measure which performs better). A/B testing requires experiment tracking, randomization, and metrics collection. Teams can manually test different configurations but not in a controlled experiment framework.

12. **Context-Aware Position Scoring:** Position scoring is simplistic (top-of-file gets boost). Context-aware position scoring (e.g., boost chunks that are "entry points" to execution flow, boost chunks that are frequently called, boost chunks that are architecturally central) is out of scope because it requires static analysis, call graph, and architectural heuristics. Basic position scoring is sufficient for most cases.

13. **Historical Query Analytics:** The ranking system does not track which queries were issued, which chunks were ranked highly, or how often chunks are selected. Historical analytics (e.g., "chunk X has been in top 10 for 80% of queries this week") could inform ranking but requires telemetry and analytics infrastructure. The system is stateless per-query.

14. **Multi-Objective Optimization:** The ranking system optimizes a single objective (combined score). Multi-objective optimization (e.g., maximize relevance while minimizing token cost, balance precision and recall) is out of scope because it requires Pareto optimization or scalarization strategies. The current weighted sum approach is a simple scalarization but does not expose multiple objectives explicitly.

15. **External Ranking Services:** The ranking computation is entirely local. Integration with external ranking services (e.g., GitHub Copilot ranking API, cloud-based learning-to-rank services) is out of scope and violates the local-first principle. All ranking logic runs in-process with no external dependencies.

---

## Functional Requirements

### Relevance Scoring (FR-016b-01 to FR-016b-04)

| ID | Requirement |
|----|-------------|
| FR-016b-01 | System MUST score chunks by search relevance when available |
| FR-016b-02 | System MUST score chunks by keyword match against current query |
| FR-016b-03 | System MUST score chunks by query term overlap |
| FR-016b-04 | Relevance scores MUST be normalized to 0-1 range |

### Source Scoring (FR-016b-05 to FR-016b-10)

| ID | Requirement |
|----|-------------|
| FR-016b-05 | System MUST assign priority score based on chunk source |
| FR-016b-06 | Tool results MUST receive high source priority (default: 100) |
| FR-016b-07 | Open files MUST receive medium-high source priority (default: 80) |
| FR-016b-08 | Search results MUST receive medium source priority (default: 60) |
| FR-016b-09 | References MUST receive low source priority (default: 40) |
| FR-016b-10 | Source priorities MUST be configurable |

### Recency Scoring (FR-016b-11 to FR-016b-14)

| ID | Requirement |
|----|-------------|
| FR-016b-11 | System MUST score chunks by file modification time |
| FR-016b-12 | System MUST score chunks by file access time when available |
| FR-016b-13 | System MUST apply time decay function to recency scores |
| FR-016b-14 | Decay rate MUST be configurable (hours until score halves) |

### Position Scoring (FR-016b-15 to FR-016b-17)

| ID | Requirement |
|----|-------------|
| FR-016b-15 | System MUST score chunks by position within file |
| FR-016b-16 | Top-of-file content (imports, class headers) MUST receive position boost |
| FR-016b-17 | Code related to other selected chunks MUST receive boost |

### Combined Scoring (FR-016b-18 to FR-016b-21)

| ID | Requirement |
|----|-------------|
| FR-016b-18 | System MUST apply configurable weights to each scoring factor |
| FR-016b-19 | System MUST sum weighted scores to produce final score |
| FR-016b-20 | Final scores MUST be normalized to 0-1 range |
| FR-016b-21 | All weights MUST be configurable in configuration file |

### Tie-Breaking (FR-016b-22 to FR-016b-25)

| ID | Requirement |
|----|-------------|
| FR-016b-22 | Primary sort MUST be by final combined score (descending) |
| FR-016b-23 | Secondary sort MUST be by source priority (descending) |
| FR-016b-24 | Tertiary sort MUST be by file path (alphabetical) |
| FR-016b-25 | Ranking MUST be deterministic for identical inputs |

### Filtering (FR-016b-26 to FR-016b-28)

| ID | Requirement |
|----|-------------|
| FR-016b-26 | System MUST support minimum score threshold |
| FR-016b-27 | Chunks below threshold MUST be excluded from results |
| FR-016b-28 | Minimum score threshold MUST be configurable |

### Boosting (FR-016b-29 to FR-016b-31)

| ID | Requirement |
|----|-------------|
| FR-016b-29 | System MUST support score boost for specific path patterns |
| FR-016b-30 | System MUST support score boost for specific file types |
| FR-016b-31 | System MUST support temporary session-level boosts |

### Penalties (FR-016b-32 to FR-016b-34)

| ID | Requirement |
|----|-------------|
| FR-016b-32 | System MUST apply score penalty for test file paths |
| FR-016b-33 | System MUST apply score penalty for generated file paths |
| FR-016b-34 | Penalty patterns and factors MUST be configurable |

### Configuration Management (FR-016b-35 to FR-016b-41)

| ID | Requirement |
|----|-------------|
| FR-016b-35 | System MUST load ranking configuration from `.agent/config.yml` |
| FR-016b-36 | System MUST validate weight sum equals 1.0 (tolerance: 0.01) |
| FR-016b-37 | System MUST normalize weights automatically if sum != 1.0 |
| FR-016b-38 | System MUST validate boost/penalty glob patterns at startup |
| FR-016b-39 | System MUST reject invalid glob patterns with descriptive errors |
| FR-016b-40 | System MUST support environment-specific overrides (dev, staging, prod) |
| FR-016b-41 | System MUST provide default configuration if user config missing |

### Validation & Error Handling (FR-016b-42 to FR-016b-46)

| ID | Requirement |
|----|-------------|
| FR-016b-42 | System MUST handle missing chunk metadata gracefully (default to neutral scores) |
| FR-016b-43 | System MUST handle missing file modification time (use current time or neutral score) |
| FR-016b-44 | System MUST clamp all scores to [0.0, 1.0] range after boost/penalty application |
| FR-016b-45 | System MUST handle empty chunk list (return empty result, no exception) |
| FR-016b-46 | System MUST log warnings for invalid configuration but continue with defaults |

### Debug & Observability (FR-016b-47 to FR-016b-50)

| ID | Requirement |
|----|-------------|
| FR-016b-47 | System MUST provide debug output showing all factor scores per chunk when enabled |
| FR-016b-48 | System MUST log ranking summary (total chunks, avg score, top 10 scores) at INFO level |
| FR-016b-49 | System MUST support dry-run mode (show ranking without applying) |
| FR-016b-50 | System MUST expose ranking metrics (execution time, chunk count, factor distribution) |

### Performance Optimization (FR-016b-51 to FR-016b-54)

| ID | Requirement |
|----|-------------|
| FR-016b-51 | System MUST compute factor scores in parallel when chunk count > 100 |
| FR-016b-52 | System MUST cache compiled glob patterns for boost/penalty matching |
| FR-016b-53 | System MUST use in-place sorting to minimize memory allocations |
| FR-016b-54 | System MUST short-circuit scoring for chunks below threshold early |

---

## Non-Functional Requirements

### Performance (NFR-016b-01 to NFR-016b-03)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016b-01 | Performance | System MUST rank 1000 chunks in less than 50ms |
| NFR-016b-02 | Performance | Individual score computation MUST complete in less than 1ms |
| NFR-016b-03 | Performance | Sorting MUST complete in less than 10ms for 1000 chunks |

### Quality (NFR-016b-04 to NFR-016b-06)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016b-04 | Quality | Ranking MUST be consistent across identical inputs |
| NFR-016b-05 | Quality | Ranking results MUST be intuitive for developers |
| NFR-016b-06 | Quality | Ranking behavior MUST be configurable for different use cases |

### Reliability (NFR-016b-07 to NFR-016b-09)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016b-07 | Reliability | System MUST handle missing scoring factors gracefully |
| NFR-016b-08 | Reliability | System MUST use sensible default values for missing data |
| NFR-016b-09 | Reliability | System MUST NOT crash on invalid input data |

### Maintainability (NFR-016b-10 to NFR-016b-12)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016b-10 | Maintainability | Ranking logic MUST be separated into independent, testable scorers |
| NFR-016b-11 | Maintainability | Adding new scoring factors MUST NOT require changes to existing factors |
| NFR-016b-12 | Maintainability | Configuration schema MUST be documented with examples and validation rules |

### Usability (NFR-016b-13 to NFR-016b-15)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016b-13 | Usability | Configuration errors MUST provide actionable error messages |
| NFR-016b-14 | Usability | Debug output MUST be human-readable and include chunk paths |
| NFR-016b-15 | Usability | Default configuration MUST work well for 80% of use cases without tuning |

### Security (NFR-016b-16 to NFR-016b-18)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016b-16 | Security | Boost/penalty glob patterns MUST be validated to prevent ReDoS attacks |
| NFR-016b-17 | Security | Ranking MUST complete within timeout (5 seconds) to prevent DoS |
| NFR-016b-18 | Security | File path handling MUST prevent path traversal attempts |

---

## User Manual Documentation

### Overview

Ranking rules determine which chunks appear first in context. Better ranking means better context quality.

### Configuration

```yaml
# .agent/config.yml
context:
  ranking:
    # Factor weights (must sum to 1.0)
    weights:
      relevance: 0.50
      source: 0.25
      recency: 0.15
      position: 0.10
      
    # Source priorities (0-100)
    source_priority:
      tool_result: 100
      open_file: 80
      search_result: 60
      reference: 40
      
    # Time decay for recency (hours)
    recency_decay_hours: 24
    
    # Minimum score threshold (0-1)
    min_score: 0.1
    
    # Path boosts
    boosts:
      - pattern: "src/core/**"
        factor: 1.2
      - pattern: "*Service*.cs"
        factor: 1.1
        
    # Path penalties
    penalties:
      - pattern: "**/tests/**"
        factor: 0.8
      - pattern: "*.generated.cs"
        factor: 0.5
```

### Ranking Factors

#### Relevance (default: 50%)

How well the chunk matches the current task:
- Search score if from search
- Keyword overlap with query
- Direct reference from other code

#### Source Priority (default: 25%)

Where the chunk came from:
- Tool results: 100 (directly requested)
- Open files: 80 (user is viewing)
- Search results: 60 (query matched)
- References: 40 (indirectly related)

#### Recency (default: 15%)

How recently the file was touched:
- Modified today: 1.0
- Modified yesterday: 0.9
- Modified last week: 0.7
- Older: decays further

#### Position (default: 10%)

Where in the file:
- Top (imports, class header): slight boost
- Related to other chunks: boost

### Debugging Ranking

```bash
$ acode context debug-ranking

Ranking Debug
────────────────────
Top 10 chunks:

1. UserService.cs:25-50 (score: 0.92)
   Relevance: 0.95 × 0.50 = 0.475
   Source: 1.00 × 0.25 = 0.250
   Recency: 0.90 × 0.15 = 0.135
   Position: 0.60 × 0.10 = 0.060
   
2. IUserService.cs:1-20 (score: 0.85)
   Relevance: 0.90 × 0.50 = 0.450
   Source: 0.80 × 0.25 = 0.200
   ...
```

### Troubleshooting

#### Wrong Chunks Selected

**Problem:** Irrelevant code ranked higher

**Solutions:**
1. Increase relevance weight
2. Decrease source weight
3. Add penalties for irrelevant paths

#### Stale Code Selected

**Problem:** Old code ranked over new

**Solutions:**
1. Increase recency weight
2. Decrease recency decay
3. Manually update index

#### Test Code Included

**Problem:** Test files in context

**Solutions:**
1. Add test path penalty
2. Set penalty factor low (0.3)
3. Increase min_score threshold

---

## Assumptions

### Technical Assumptions

1. **Chunk Metadata Availability:** All chunks passed to the ranker have metadata fields (FilePath, LineStart, LineEnd, Source) populated. If metadata is missing, the ranker defaults to neutral scores (0.5) for affected factors.

2. **Search Score Normalization:** When chunks originate from search results, the search scores are pre-normalized to [0.0, 1.0] range by the search subsystem. The ranker does not re-normalize search scores.

3. **File System Access:** The ranker can access file modification times via the file system or index. If file system access fails, recency scoring defaults to neutral (0.5) without throwing exceptions.

4. **Glob Pattern Syntax:** Boost and penalty patterns use standard glob syntax (`**` for recursive, `*` for wildcard). Advanced regex patterns are not supported.

5. **Configuration Validation:** The configuration loading system validates ranking configuration at startup. Invalid configurations are rejected before the ranker is instantiated.

6. **Single-Threaded Scoring:** While factor scoring can be parallelized, the final sorting is single-threaded. This is acceptable because sorting 10,000 chunks takes <20ms.

7. **No External Dependencies:** The ranker does not call external services, LLMs, or APIs. All computation is local and deterministic.

8. **UTF-8 File Paths:** File paths are UTF-8 encoded. Non-UTF-8 paths may cause glob pattern matching failures (gracefully degraded to no boost/penalty).

9. **Clock Availability:** Recency scoring requires access to current time (DateTime.UtcNow). Clock skew or incorrect system time may affect recency scores but won't cause failures.

10. **Memory Limits:** The ranker assumes sufficient memory to hold all chunks in memory simultaneously. For 10,000 chunks × 2KB avg = 20MB, this is reasonable for modern systems.

### Operational Assumptions

11. **Configuration Stability:** Ranking configuration (weights, boosts, penalties) changes infrequently (daily or weekly, not per-query). The system reloads configuration on startup but not mid-execution.

12. **Representative Defaults:** Default weights (relevance 50%, source 25%, recency 15%, position 10%) work for 80% of use cases. Power users tune weights for specific workflows.

13. **Auditability Requirement:** Ranking must be auditable (deterministic, reproducible). This precludes randomization, A/B testing, and personalization in the initial implementation.

14. **Performance Targets:** Ranking 1,000 chunks in <50ms is the performance target. Most queries involve 100-500 chunks, well within this limit.

15. **Chunk Volume:** Typical queries produce 100-1,000 chunks. Edge cases may produce 10,000+ chunks (large codebases, broad queries), which still meet performance targets but approach limits.

### Integration Assumptions

16. **Task 016.a Integration:** The chunker (Task 016.a) produces chunks with required metadata. The ranker does not validate chunk content, only metadata.

17. **Task 016.c Integration:** The budgeter (Task 016.c) receives ranked chunks in order and selects the top N that fit within token budget. The ranker does not perform budgeting.

18. **Task 002 Configuration:** Ranking configuration is stored in `.agent/config.yml` under the `context.ranking` key. The configuration loader (Task 002) handles parsing and validation.

19. **Task 015 Index:** File modification times and access patterns are provided by the indexing subsystem (Task 015). If the index is unavailable or stale, recency scoring degrades gracefully.

20. **Task 011 Session Context:** The current query or task context is provided by the session management subsystem (Task 011). The ranker uses this context for relevance scoring but does not modify it.

---

## Security Considerations

### Threat 1: ReDoS via Malicious Glob Patterns

**Risk:** High

**Description:** If a malicious user can modify `.agent/config.yml`, they could insert glob patterns designed to cause exponential backtracking (ReDoS - Regular Expression Denial of Service). Example: `**/*/*/*/*/*/*/*/*/*/*/*` could cause pattern matching to hang when applied to deep directory structures.

**Attack Scenario:**
1. Attacker gains write access to `.agent/config.yml` (e.g., via compromised developer machine)
2. Attacker adds boost pattern: `pattern: "**/*{a,b,c,d,e,f}*{a,b,c,d,e,f}*{a,b,c,d,e,f}*"`
3. Ranker compiles pattern and attempts to match against 10,000 chunks
4. Pattern matching hangs or consumes excessive CPU (minutes instead of milliseconds)
5. Agent becomes unresponsive, denying service to user

**Mitigation (Complete C# Implementation):**

```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using DotNet.Globbing;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.ContextPacker
{
    /// <summary>
    /// Validates and compiles glob patterns with ReDoS protection.
    /// </summary>
    public sealed class SafeGlobPatternCompiler
    {
        private readonly ILogger<SafeGlobPatternCompiler> _logger;
        private readonly int _maxPatternLength;
        private readonly int _maxCompilationTimeMs;
        private const int DefaultMaxPatternLength = 200;
        private const int DefaultMaxCompilationTimeMs = 100;

        public SafeGlobPatternCompiler(
            ILogger<SafeGlobPatternCompiler> logger,
            int maxPatternLength = DefaultMaxPatternLength,
            int maxCompilationTimeMs = DefaultMaxCompilationTimeMs)
        {
            _logger = logger;
            _maxPatternLength = maxPatternLength;
            _maxCompilationTimeMs = maxCompilationTimeMs;
        }

        /// <summary>
        /// Compiles a glob pattern with safety checks.
        /// </summary>
        /// <returns>Compiled glob or null if validation failed</returns>
        public Glob? CompilePattern(string pattern, string contextName)
        {
            // MITIGATION 1: Length limit
            if (pattern.Length > _maxPatternLength)
            {
                _logger.LogWarning(
                    "Glob pattern exceeds max length ({Length} > {Max}) in {Context}: {Pattern}",
                    pattern.Length, _maxPatternLength, contextName, pattern);
                return null;
            }

            // MITIGATION 2: Suspicious pattern detection
            if (IsSuspiciousPattern(pattern))
            {
                _logger.LogWarning(
                    "Glob pattern rejected as potentially malicious in {Context}: {Pattern}",
                    contextName, pattern);
                return null;
            }

            // MITIGATION 3: Compilation timeout
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var options = new GlobOptions
                {
                    Evaluation =
                    {
                        CaseInsensitive = false
                    }
                };

                var glob = Glob.Parse(pattern, options);
                stopwatch.Stop();

                if (stopwatch.ElapsedMilliseconds > _maxCompilationTimeMs)
                {
                    _logger.LogWarning(
                        "Glob compilation took too long ({Elapsed}ms > {Max}ms) in {Context}: {Pattern}",
                        stopwatch.ElapsedMilliseconds, _maxCompilationTimeMs, contextName, pattern);
                    return null;
                }

                _logger.LogDebug(
                    "Compiled glob pattern in {Elapsed}ms for {Context}: {Pattern}",
                    stopwatch.ElapsedMilliseconds, contextName, pattern);

                return glob;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Failed to compile glob pattern in {Context}: {Pattern}",
                    contextName, pattern);
                return null;
            }
        }

        /// <summary>
        /// Detects potentially malicious patterns.
        /// </summary>
        private bool IsSuspiciousPattern(string pattern)
        {
            // Excessive alternation groups: {a,b,c,d,e,f}{a,b,c,d,e,f}
            if (Regex.Matches(pattern, @"\{[^}]{10,}\}").Count > 2)
            {
                return true;
            }

            // Excessive wildcards: *{10,}
            if (pattern.Contains("**********"))
            {
                return true;
            }

            // Nested brackets: [[[[
            if (pattern.Contains("[[[["))
            {
                return true;
            }

            // Too many alternation options
            var alternationMatches = Regex.Matches(pattern, @"\{([^}]+)\}");
            foreach (Match match in alternationMatches)
            {
                var options = match.Groups[1].Value.Split(',');
                if (options.Length > 20)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Applies path-based boosts and penalties with ReDoS protection.
    /// </summary>
    public sealed class PathAdjustmentService
    {
        private readonly SafeGlobPatternCompiler _globCompiler;
        private readonly ILogger<PathAdjustmentService> _logger;
        private readonly Dictionary<string, Glob> _compiledBoostPatterns;
        private readonly Dictionary<string, Glob> _compiledPenaltyPatterns;

        public PathAdjustmentService(
            SafeGlobPatternCompiler globCompiler,
            ILogger<PathAdjustmentService> logger)
        {
            _globCompiler = globCompiler;
            _logger = logger;
            _compiledBoostPatterns = new Dictionary<string, Glob>();
            _compiledPenaltyPatterns = new Dictionary<string, Glob>();
        }

        public void LoadConfiguration(RankingConfiguration config)
        {
            _compiledBoostPatterns.Clear();
            _compiledPenaltyPatterns.Clear();

            // Compile boost patterns
            foreach (var boost in config.Boosts)
            {
                var glob = _globCompiler.CompilePattern(boost.Pattern, "boost");
                if (glob != null)
                {
                    _compiledBoostPatterns[boost.Pattern] = glob;
                }
            }

            // Compile penalty patterns
            foreach (var penalty in config.Penalties)
            {
                var glob = _globCompiler.CompilePattern(penalty.Pattern, "penalty");
                if (glob != null)
                {
                    _compiledPenaltyPatterns[penalty.Pattern] = glob;
                }
            }

            _logger.LogInformation(
                "Loaded {BoostCount} boost patterns and {PenaltyCount} penalty patterns",
                _compiledBoostPatterns.Count, _compiledPenaltyPatterns.Count);
        }

        public double ApplyAdjustments(string filePath, double baseScore, RankingConfiguration config)
        {
            double adjustedScore = baseScore;

            // Apply boosts
            foreach (var boost in config.Boosts)
            {
                if (_compiledBoostPatterns.TryGetValue(boost.Pattern, out var glob))
                {
                    if (glob.IsMatch(filePath))
                    {
                        adjustedScore *= boost.Factor;
                        _logger.LogDebug(
                            "Applied boost {Factor}x to {Path} (pattern: {Pattern})",
                            boost.Factor, filePath, boost.Pattern);
                    }
                }
            }

            // Apply penalties
            foreach (var penalty in config.Penalties)
            {
                if (_compiledPenaltyPatterns.TryGetValue(penalty.Pattern, out var glob))
                {
                    if (glob.IsMatch(filePath))
                    {
                        adjustedScore *= penalty.Factor;
                        _logger.LogDebug(
                            "Applied penalty {Factor}x to {Path} (pattern: {Pattern})",
                            penalty.Factor, filePath, penalty.Pattern);
                    }
                }
            }

            // Clamp to valid range
            return Math.Clamp(adjustedScore, 0.0, 1.0);
        }
    }

    public record BoostConfig(string Pattern, double Factor);
    public record PenaltyConfig(string Pattern, double Factor);

    public record RankingConfiguration(
        List<BoostConfig> Boosts,
        List<PenaltyConfig> Penalties);
}
```

**Test Coverage:**
- Pattern length limit enforcement (201 chars → rejected)
- Suspicious pattern detection (excessive alternation → rejected)
- Compilation timeout (complex pattern → rejected within 100ms)
- Valid pattern acceptance (src/**/*.cs → compiled successfully)
- Malicious pattern rejection (**/*{a,b,c,d,e,f}*{a,b,c,d,e,f}* → rejected)

---

### Threat 2: Information Leakage via Debug Output

**Risk:** Medium

**Description:** When debug mode is enabled, the ranking system outputs detailed scoring information including file paths and scores. If debug output is logged to files or displayed in shared environments, sensitive file paths (e.g., `/src/secrets/ApiKeys.cs`, `/internal/salary-data/`) could be exposed to unauthorized users.

**Attack Scenario:**
1. Developer enables debug logging: `acode context debug-ranking`
2. Debug output includes: "UserSalaryCalculator.cs:45-89 (score: 0.88)"
3. Debug log is written to shared file or monitoring dashboard
4. Attacker with access to logs sees sensitive file paths
5. Attacker learns about salary calculation logic and targets those files

**Mitigation (Complete C# Implementation):**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.ContextPacker
{
    /// <summary>
    /// Provides sanitized debug output for ranking with path redaction.
    /// </summary>
    public sealed class RankingDebugService
    {
        private readonly ILogger<RankingDebugService> _logger;
        private readonly string _repositoryRoot;
        private readonly HashSet<string> _sensitivePathPatterns;

        public RankingDebugService(
            ILogger<RankingDebugService> logger,
            string repositoryRoot)
        {
            _logger = logger;
            _repositoryRoot = repositoryRoot;
            _sensitivePathPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "secret", "password", "token", "key", "credential",
                "salary", "compensation", "internal", "private"
            };
        }

        /// <summary>
        /// Sanitizes file path for debug output.
        /// </summary>
        public string SanitizePath(string fullPath)
        {
            // Remove repository root to show relative path
            var relativePath = Path.GetRelativePath(_repositoryRoot, fullPath);

            // Check for sensitive keywords
            if (_sensitivePathPatterns.Any(pattern =>
                relativePath.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
            {
                // Redact directory names containing sensitive keywords
                var parts = relativePath.Split(Path.DirectorySeparatorChar);
                for (int i = 0; i < parts.Length - 1; i++) // Keep filename
                {
                    if (_sensitivePathPatterns.Any(pattern =>
                        parts[i].Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                    {
                        parts[i] = "[REDACTED]";
                    }
                }
                return string.Join(Path.DirectorySeparatorChar, parts);
            }

            return relativePath;
        }

        /// <summary>
        /// Outputs ranking debug information with path sanitization.
        /// </summary>
        public void OutputRankingDebug(IReadOnlyList<RankedChunk> rankedChunks, int topN = 10)
        {
            _logger.LogInformation("Ranking Debug (top {TopN} of {Total})", topN, rankedChunks.Count);
            _logger.LogInformation("────────────────────────────────────────");

            var topChunks = rankedChunks.Take(topN);
            int rank = 1;

            foreach (var chunk in topChunks)
            {
                var sanitizedPath = SanitizePath(chunk.FilePath);
                _logger.LogInformation(
                    "{Rank}. {Path}:{LineStart}-{LineEnd} (score: {Score:F2})",
                    rank, sanitizedPath, chunk.LineStart, chunk.LineEnd, chunk.CombinedScore);

                _logger.LogInformation(
                    "   Relevance: {Rel:F2} × {RelWeight:F2} = {RelContrib:F3}",
                    chunk.RelevanceScore, chunk.Weights.Relevance,
                    chunk.RelevanceScore * chunk.Weights.Relevance);

                _logger.LogInformation(
                    "   Source:    {Src:F2} × {SrcWeight:F2} = {SrcContrib:F3}",
                    chunk.SourceScore, chunk.Weights.Source,
                    chunk.SourceScore * chunk.Weights.Source);

                _logger.LogInformation(
                    "   Recency:   {Rec:F2} × {RecWeight:F2} = {RecContrib:F3}",
                    chunk.RecencyScore, chunk.Weights.Recency,
                    chunk.RecencyScore * chunk.Weights.Recency);

                _logger.LogInformation(
                    "   Position:  {Pos:F2} × {PosWeight:F2} = {PosContrib:F3}",
                    chunk.PositionScore, chunk.Weights.Position,
                    chunk.PositionScore * chunk.Weights.Position);

                _logger.LogInformation("");
                rank++;
            }

            // Summary statistics (no paths revealed)
            var avgScore = rankedChunks.Average(c => c.CombinedScore);
            var medianScore = rankedChunks[rankedChunks.Count / 2].CombinedScore;
            _logger.LogInformation("Average score: {AvgScore:F3}", avgScore);
            _logger.LogInformation("Median score: {MedianScore:F3}", medianScore);
        }
    }

    public record RankedChunk(
        string FilePath,
        int LineStart,
        int LineEnd,
        double CombinedScore,
        double RelevanceScore,
        double SourceScore,
        double RecencyScore,
        double PositionScore,
        RankingWeights Weights);

    public record RankingWeights(
        double Relevance,
        double Source,
        double Recency,
        double Position);
}
```

**Test Coverage:**
- Path sanitization (sensitive keywords → [REDACTED])
- Relative path display (absolute → relative from repo root)
- Debug output format (readable, no full paths)
- Summary statistics (no individual paths in aggregates)

---

### Threat 3: Configuration Tampering to Prioritize Malicious Code

**Risk:** Medium

**Description:** If an attacker gains write access to `.agent/config.yml`, they could manipulate ranking weights or add boosts to prioritize malicious code files. For example, boosting `/malware/*.cs` files to score 100x higher would ensure the agent always includes malware code in context, potentially causing the agent to suggest or execute malicious operations.

**Attack Scenario:**
1. Attacker compromises developer's machine or CI/CD pipeline
2. Attacker modifies `.agent/config.yml`:
   ```yaml
   ranking:
     boosts:
       - pattern: "malware/**"
         factor: 100.0
     weights:
       relevance: 0.01  # Reduce relevance importance
       source: 0.99     # Maximize source (attacker controls)
   ```
3. Agent now prioritizes malware code in every query
4. User asks: "How do I authenticate users?"
5. Agent responds with malicious authentication code from `/malware/backdoor-auth.cs`
6. User implements backdoored authentication

**Mitigation (Complete C# Implementation):**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.ContextPacker
{
    /// <summary>
    /// Validates ranking configuration to prevent malicious tampering.
    /// </summary>
    public sealed class RankingConfigurationValidator
    {
        private readonly ILogger<RankingConfigurationValidator> _logger;
        private const double MaxBoostFactor = 3.0;
        private const double MinPenaltyFactor = 0.1;
        private const double MinRelevanceWeight = 0.25; // Relevance must be significant
        private const double WeightTolerancesum = 0.01;

        public RankingConfigurationValidator(ILogger<RankingConfigurationValidator> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Validates and sanitizes ranking configuration.
        /// </summary>
        public RankingConfiguration Validate(RankingConfiguration config)
        {
            var errors = new List<string>();

            // MITIGATION 1: Weight sum validation
            var weightSum = config.Weights.Relevance + config.Weights.Source +
                           config.Weights.Recency + config.Weights.Position;

            if (Math.Abs(weightSum - 1.0) > WeightTolerancesum)
            {
                _logger.LogWarning(
                    "Weight sum is {Sum} (expected 1.0). Auto-normalizing.",
                    weightSum);

                // Auto-normalize weights
                config = config with
                {
                    Weights = config.Weights with
                    {
                        Relevance = config.Weights.Relevance / weightSum,
                        Source = config.Weights.Source / weightSum,
                        Recency = config.Weights.Recency / weightSum,
                        Position = config.Weights.Position / weightSum
                    }
                };
            }

            // MITIGATION 2: Relevance weight minimum
            if (config.Weights.Relevance < MinRelevanceWeight)
            {
                errors.Add(
                    $"Relevance weight ({config.Weights.Relevance:F2}) is below minimum " +
                    $"({MinRelevanceWeight}). This could allow non-relevant code to dominate.");

                config = config with
                {
                    Weights = config.Weights with
                    {
                        Relevance = MinRelevanceWeight
                    }
                };
            }

            // MITIGATION 3: Boost factor limits
            var excessiveBoosts = config.Boosts.Where(b => b.Factor > MaxBoostFactor).ToList();
            if (excessiveBoosts.Any())
            {
                foreach (var boost in excessiveBoosts)
                {
                    errors.Add(
                        $"Boost factor {boost.Factor}x for pattern '{boost.Pattern}' exceeds " +
                        $"maximum ({MaxBoostFactor}x). Clamping to maximum.");
                }

                config = config with
                {
                    Boosts = config.Boosts.Select(b =>
                        b.Factor > MaxBoostFactor ? b with { Factor = MaxBoostFactor } : b).ToList()
                };
            }

            // MITIGATION 4: Penalty factor limits
            var excessivePenalties = config.Penalties.Where(p => p.Factor < MinPenaltyFactor).ToList();
            if (excessivePenalties.Any())
            {
                foreach (var penalty in excessivePenalties)
                {
                    errors.Add(
                        $"Penalty factor {penalty.Factor}x for pattern '{penalty.Pattern}' is below " +
                        $"minimum ({MinPenaltyFactor}x). Clamping to minimum.");
                }

                config = config with
                {
                    Penalties = config.Penalties.Select(p =>
                        p.Factor < MinPenaltyFactor ? p with { Factor = MinPenaltyFactor } : p).ToList()
                };
            }

            // MITIGATION 5: Suspicious pattern detection
            var suspiciousBoosts = DetectSuspiciousBoosts(config.Boosts);
            if (suspiciousBoosts.Any())
            {
                foreach (var boost in suspiciousBoosts)
                {
                    errors.Add(
                        $"Boost pattern '{boost.Pattern}' appears suspicious (contains keywords: " +
                        $"malware, backdoor, exploit). Removing.");
                }

                config = config with
                {
                    Boosts = config.Boosts.Except(suspiciousBoosts).ToList()
                };
            }

            if (errors.Any())
            {
                _logger.LogWarning(
                    "Ranking configuration had {Count} validation errors. Sanitized configuration loaded.",
                    errors.Count);

                foreach (var error in errors)
                {
                    _logger.LogWarning("  - {Error}", error);
                }
            }

            return config;
        }

        private List<BoostConfig> DetectSuspiciousBoosts(List<BoostConfig> boosts)
        {
            var suspiciousKeywords = new[] { "malware", "backdoor", "exploit", "hack", "trojan" };

            return boosts.Where(b =>
                suspiciousKeywords.Any(keyword =>
                    b.Pattern.Contains(keyword, StringComparison.OrdinalIgnoreCase))).ToList();
        }
    }

    public record RankingConfiguration(
        RankingWeights Weights,
        List<BoostConfig> Boosts,
        List<PenaltyConfig> Penalties);
}
```

**Test Coverage:**
- Weight sum validation (0.7 → normalized to 1.0)
- Relevance minimum enforcement (0.05 → clamped to 0.25)
- Boost factor limits (100.0x → clamped to 3.0x)
- Penalty factor limits (0.01x → clamped to 0.1x)
- Suspicious pattern detection (malware/** boost → removed)

---

### Threat 4: Denial of Service via Excessive Chunk Count

**Risk:** Low

**Description:** If the chunking subsystem produces an abnormally large number of chunks (e.g., 1 million chunks from a single large file or malicious chunking configuration), the ranking system could consume excessive CPU and memory sorting and scoring them, causing the agent to hang or crash.

**Attack Scenario:**
1. Attacker modifies chunking configuration to produce 1-line chunks
2. Large file (100,000 lines) produces 100,000 chunks
3. Across 100 files, this produces 10 million chunks
4. Ranker attempts to score and sort 10 million chunks
5. Memory exhausted (10M chunks × 500 bytes/chunk = 5GB)
6. Agent crashes or system becomes unresponsive

**Mitigation (Complete C# Implementation):**

```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.ContextPacker
{
    /// <summary>
    /// Ranks chunks with resource limits to prevent DoS.
    /// </summary>
    public sealed class SafeRankingService
    {
        private readonly ILogger<SafeRankingService> _logger;
        private readonly int _maxChunkCount;
        private readonly int _maxRankingTimeMs;
        private const int DefaultMaxChunkCount = 50000;
        private const int DefaultMaxRankingTimeMs = 5000;

        public SafeRankingService(
            ILogger<SafeRankingService> logger,
            int maxChunkCount = DefaultMaxChunkCount,
            int maxRankingTimeMs = DefaultMaxRankingTimeMs)
        {
            _logger = logger;
            _maxChunkCount = maxChunkCount;
            _maxRankingTimeMs = maxRankingTimeMs;
        }

        /// <summary>
        /// Ranks chunks with resource limits.
        /// </summary>
        public IReadOnlyList<RankedChunk> RankChunks(
            IReadOnlyList<ContentChunk> chunks,
            RankingConfiguration config)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // MITIGATION 1: Chunk count limit
                if (chunks.Count > _maxChunkCount)
                {
                    _logger.LogWarning(
                        "Chunk count ({Count}) exceeds maximum ({Max}). Truncating to top {Max} by source priority.",
                        chunks.Count, _maxChunkCount);

                    // Pre-filter by source priority before full ranking
                    chunks = chunks
                        .OrderByDescending(c => GetSourcePriority(c.Source, config))
                        .Take(_maxChunkCount)
                        .ToList();
                }

                // MITIGATION 2: Early termination check
                var scoredChunks = new List<RankedChunk>(chunks.Count);

                for (int i = 0; i < chunks.Count; i++)
                {
                    // Check timeout every 1000 chunks
                    if (i % 1000 == 0 && stopwatch.ElapsedMilliseconds > _maxRankingTimeMs)
                    {
                        _logger.LogWarning(
                            "Ranking timeout after {Elapsed}ms and {Count} chunks. Returning partial results.",
                            stopwatch.ElapsedMilliseconds, i);

                        // Return what we have so far, sorted
                        return scoredChunks.OrderByDescending(c => c.CombinedScore).ToList();
                    }

                    var ranked = ScoreChunk(chunks[i], config);
                    scoredChunks.Add(ranked);
                }

                // MITIGATION 3: In-place sort to minimize allocations
                scoredChunks.Sort((a, b) =>
                {
                    var scoreCompare = b.CombinedScore.CompareTo(a.CombinedScore);
                    if (scoreCompare != 0) return scoreCompare;

                    var sourceCompare = b.SourceScore.CompareTo(a.SourceScore);
                    if (sourceCompare != 0) return sourceCompare;

                    var pathCompare = string.Compare(a.FilePath, b.FilePath, StringComparison.Ordinal);
                    if (pathCompare != 0) return pathCompare;

                    return a.LineStart.CompareTo(b.LineStart);
                });

                stopwatch.Stop();
                _logger.LogInformation(
                    "Ranked {Count} chunks in {Elapsed}ms (avg: {AvgMs:F2}ms/chunk)",
                    chunks.Count, stopwatch.ElapsedMilliseconds,
                    (double)stopwatch.ElapsedMilliseconds / chunks.Count);

                return scoredChunks;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Ranking failed after {Elapsed}ms. Returning chunks in source priority order.",
                    stopwatch.ElapsedMilliseconds);

                // Fallback: return chunks ordered by source priority only
                return chunks
                    .Select(c => new RankedChunk(
                        c.FilePath, c.LineStart, c.LineEnd,
                        GetSourcePriority(c.Source, config) / 100.0,
                        0.5, GetSourcePriority(c.Source, config) / 100.0, 0.5, 0.5,
                        config.Weights))
                    .OrderByDescending(c => c.SourceScore)
                    .ToList();
            }
        }

        private RankedChunk ScoreChunk(ContentChunk chunk, RankingConfiguration config)
        {
            // Simplified scoring (full implementation would use actual scorers)
            double relevance = 0.5;  // Placeholder
            double source = GetSourcePriority(chunk.Source, config) / 100.0;
            double recency = 0.5;    // Placeholder
            double position = 0.5;   // Placeholder

            double combined = (relevance * config.Weights.Relevance) +
                            (source * config.Weights.Source) +
                            (recency * config.Weights.Recency) +
                            (position * config.Weights.Position);

            combined = Math.Clamp(combined, 0.0, 1.0);

            return new RankedChunk(
                chunk.FilePath, chunk.LineStart, chunk.LineEnd, combined,
                relevance, source, recency, position, config.Weights);
        }

        private double GetSourcePriority(ChunkSource source, RankingConfiguration config)
        {
            return source switch
            {
                ChunkSource.ToolResult => 100,
                ChunkSource.OpenFile => 80,
                ChunkSource.SearchResult => 60,
                ChunkSource.Reference => 40,
                _ => 50
            };
        }
    }

    public enum ChunkSource
    {
        ToolResult,
        OpenFile,
        SearchResult,
        Reference
    }

    public record ContentChunk(
        string FilePath,
        int LineStart,
        int LineEnd,
        ChunkSource Source);
}
```

**Test Coverage:**
- Chunk count limit (100,000 chunks → truncated to 50,000)
- Timeout enforcement (ranking > 5s → partial results returned)
- In-place sorting (no excessive allocations)
- Fallback on failure (exception → source priority order)

---

### Threat 5: Time-of-Check Time-of-Use (TOCTOU) on File Modification Times

**Risk:** Low

**Description:** The ranking system reads file modification times to compute recency scores. If an attacker can manipulate file modification times between when the ranker checks them (time-of-check) and when the chunks are actually used (time-of-use), they could influence ranking to prioritize attacker-controlled files.

**Attack Scenario:**
1. Agent queries "Show me authentication code"
2. Ranker reads modification time of `AuthService.cs` (modified 1 year ago, low recency score)
3. Attacker uses `touch` command to update modification time of `MaliciousAuth.cs` to current time
4. Ranker reads modification time of `MaliciousAuth.cs` (modified 1 second ago, high recency score)
5. `MaliciousAuth.cs` ranks higher and is included in context
6. Agent suggests using malicious authentication code

**Mitigation (Complete C# Implementation):**

```csharp
using System;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.ContextPacker
{
    /// <summary>
    /// Provides file metadata with caching to prevent TOCTOU attacks.
    /// </summary>
    public sealed class FileMetadataCache
    {
        private readonly ILogger<FileMetadataCache> _logger;
        private readonly ConcurrentDictionary<string, CachedMetadata> _cache;
        private readonly TimeSpan _cacheLifetime;

        public FileMetadataCache(
            ILogger<FileMetadataCache> logger,
            TimeSpan? cacheLifetime = null)
        {
            _logger = logger;
            _cache = new ConcurrentDictionary<string, CachedMetadata>();
            _cacheLifetime = cacheLifetime ?? TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Gets file modification time with TOCTOU protection via caching.
        /// </summary>
        public DateTime GetModificationTime(string filePath)
        {
            var now = DateTime.UtcNow;

            // MITIGATION: Use cached value if still valid
            if (_cache.TryGetValue(filePath, out var cached))
            {
                if (now - cached.CachedAt < _cacheLifetime)
                {
                    _logger.LogDebug(
                        "Using cached modification time for {Path} (age: {Age}s)",
                        filePath, (now - cached.CachedAt).TotalSeconds);
                    return cached.ModificationTime;
                }
            }

            // Read from file system
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    _logger.LogWarning("File not found: {Path}. Using current time.", filePath);
                    return now;
                }

                var mtime = fileInfo.LastWriteTimeUtc;

                // MITIGATION: Reject future timestamps (indicates clock skew or tampering)
                if (mtime > now.AddMinutes(5))
                {
                    _logger.LogWarning(
                        "File {Path} has future modification time ({Mtime} > {Now}). Using current time.",
                        filePath, mtime, now);
                    mtime = now;
                }

                // Cache the result
                _cache[filePath] = new CachedMetadata(mtime, now);

                _logger.LogDebug(
                    "Cached modification time for {Path}: {Mtime}",
                    filePath, mtime);

                return mtime;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read modification time for {Path}. Using current time.", filePath);
                return now;
            }
        }

        /// <summary>
        /// Clears the cache (called at start of each ranking operation).
        /// </summary>
        public void ClearCache()
        {
            var count = _cache.Count;
            _cache.Clear();
            _logger.LogDebug("Cleared file metadata cache ({Count} entries)", count);
        }

        private record CachedMetadata(DateTime ModificationTime, DateTime CachedAt);
    }

    /// <summary>
    /// Computes recency scores using cached file metadata.
    /// </summary>
    public sealed class RecencyScorer
    {
        private readonly FileMetadataCache _metadataCache;
        private readonly ILogger<RecencyScorer> _logger;
        private readonly double _halfLifeHours;

        public RecencyScorer(
            FileMetadataCache metadataCache,
            ILogger<RecencyScorer> logger,
            double halfLifeHours = 24.0)
        {
            _metadataCache = metadataCache;
            _logger = logger;
            _halfLifeHours = halfLifeHours;
        }

        /// <summary>
        /// Computes recency score with TOCTOU protection.
        /// </summary>
        public double ComputeRecencyScore(string filePath)
        {
            var now = DateTime.UtcNow;
            var mtime = _metadataCache.GetModificationTime(filePath);

            var hoursSinceModified = (now - mtime).TotalHours;

            // Exponential decay: score = 0.5 ^ (hours / half_life)
            var score = Math.Pow(0.5, hoursSinceModified / _halfLifeHours);

            // Clamp to [0.0, 1.0]
            score = Math.Clamp(score, 0.0, 1.0);

            _logger.LogDebug(
                "Recency score for {Path}: {Score:F3} (modified {Hours:F1}h ago)",
                filePath, score, hoursSinceModified);

            return score;
        }
    }
}
```

**Test Coverage:**
- Cache hit (file metadata cached → no file system access)
- Cache expiry (30s old → re-read from file system)
- Future timestamp rejection (mtime > now → clamped to now)
- File not found (missing file → current time used)
- TOCTOU prevention (modification time read once per ranking operation)

---

## Acceptance Criteria

### Relevance Scoring (AC-001 to AC-012)

- [ ] AC-001: System scores chunks by search relevance when search scores are available
- [ ] AC-002: System scores chunks by keyword match against current query terms
- [ ] AC-003: System calculates query term overlap percentage (matching terms / total query terms)
- [ ] AC-004: System uses TF-IDF scoring for keyword importance weighting
- [ ] AC-005: Relevance scores are normalized to 0.0-1.0 range before combination
- [ ] AC-006: Chunks with zero relevance signal default to neutral score (0.5)
- [ ] AC-007: Relevance scoring handles empty queries without errors
- [ ] AC-008: Relevance scoring handles special characters in queries correctly
- [ ] AC-009: Relevance scoring is case-insensitive for keyword matching
- [ ] AC-010: Relevance scoring prioritizes exact phrase matches over individual term matches
- [ ] AC-011: Relevance factor contributes exactly (score × weight) to combined score
- [ ] AC-012: Relevance scoring completes in <1ms per chunk

### Source Priority Scoring (AC-013 to AC-021)

- [ ] AC-013: Tool results receive default source priority of 100
- [ ] AC-014: Open files receive default source priority of 80
- [ ] AC-015: Search results receive default source priority of 60
- [ ] AC-016: References receive default source priority of 40
- [ ] AC-017: Unknown sources default to neutral priority (50)
- [ ] AC-018: Source priorities are configurable in `.agent/config.yml`
- [ ] AC-019: Source scores are normalized to 0.0-1.0 range (priority / 100)
- [ ] AC-020: Source priority configuration is validated at startup
- [ ] AC-021: Invalid source priorities log warnings and use defaults

### Recency Scoring (AC-022 to AC-030)

- [ ] AC-022: System reads file modification time from file system or index
- [ ] AC-023: Missing modification times default to current time (neutral score 1.0)
- [ ] AC-024: System applies exponential decay function: score = 0.5 ^ (hours / half_life)
- [ ] AC-025: Default half-life is 24 hours (configurable)
- [ ] AC-026: Files modified in last hour score near 1.0
- [ ] AC-027: Files modified 24 hours ago score approximately 0.5
- [ ] AC-028: Files modified 1 week ago score <0.1
- [ ] AC-029: Recency scores are clamped to [0.0, 1.0] range
- [ ] AC-030: File modification times are cached to prevent TOCTOU attacks

### Position Scoring (AC-031 to AC-037)

- [ ] AC-031: Top-of-file chunks (first 20% of file) receive position boost
- [ ] AC-032: Class/interface/namespace declarations receive position boost
- [ ] AC-033: Import/using statements receive moderate position score
- [ ] AC-034: Mid-file chunks receive neutral position score (0.5)
- [ ] AC-035: Chunks related to other selected chunks receive proximity boost
- [ ] AC-036: Position scores are normalized to [0.0, 1.0] range
- [ ] AC-037: Position scoring handles files with <10 lines without errors

### Combined Scoring (AC-038 to AC-046)

- [ ] AC-038: System loads factor weights from `.agent/config.yml`
- [ ] AC-039: Default weights are: relevance 0.50, source 0.25, recency 0.15, position 0.10
- [ ] AC-040: System validates weight sum equals 1.0 (tolerance ±0.01)
- [ ] AC-041: Weights not summing to 1.0 are auto-normalized with warning logged
- [ ] AC-042: Combined score = (relevance × W_rel) + (source × W_src) + (recency × W_rec) + (position × W_pos)
- [ ] AC-043: Combined score is calculated for every chunk
- [ ] AC-044: Combined scores are clamped to [0.0, 1.0] after boost/penalty application
- [ ] AC-045: Score calculation is deterministic (same inputs → same output)
- [ ] AC-046: Combined scoring completes in <50ms for 1000 chunks

### Tie-Breaking (AC-047 to AC-053)

- [ ] AC-047: Primary sort is by combined score in descending order
- [ ] AC-048: Secondary sort is by source priority in descending order
- [ ] AC-049: Tertiary sort is by file path in ascending alphabetical order
- [ ] AC-050: Final sort is by line start number in ascending order
- [ ] AC-051: Tie-breaking produces deterministic order for identical inputs
- [ ] AC-052: Tie-breaking handles equal scores consistently across runs
- [ ] AC-053: Sorting completes in <10ms for 1000 chunks

### Threshold Filtering (AC-054 to AC-060)

- [ ] AC-054: System supports minimum score threshold configuration
- [ ] AC-055: Default minimum score threshold is 0.0 (no filtering)
- [ ] AC-056: Chunks with combined score < threshold are excluded from results
- [ ] AC-057: Threshold filtering happens after ranking, before budgeting
- [ ] AC-058: Threshold value is validated to be in [0.0, 1.0] range
- [ ] AC-059: Invalid threshold values log warnings and use default (0.0)
- [ ] AC-060: Empty result set (all filtered) is handled gracefully without errors

### Boosting & Penalties (AC-061 to AC-072)

- [ ] AC-061: System loads boost patterns from `.agent/config.yml`
- [ ] AC-062: System loads penalty patterns from `.agent/config.yml`
- [ ] AC-063: Boost patterns use standard glob syntax (**/**/*, etc.)
- [ ] AC-064: Penalty patterns use standard glob syntax
- [ ] AC-065: Boost factors are multiplicative (score × boost_factor)
- [ ] AC-066: Penalty factors are multiplicative (score × penalty_factor)
- [ ] AC-067: Multiple boosts applying to same chunk multiply together
- [ ] AC-068: Multiple penalties applying to same chunk multiply together
- [ ] AC-069: Glob patterns are validated at configuration load time
- [ ] AC-070: Invalid glob patterns are rejected with descriptive errors
- [ ] AC-071: Glob pattern compilation times out after 100ms (ReDoS protection)
- [ ] AC-072: Boost/penalty application completes in <5ms for 1000 chunks

### Configuration Management (AC-073 to AC-083)

- [ ] AC-073: System loads ranking configuration from `.agent/config.yml`
- [ ] AC-074: Missing configuration file uses sensible defaults
- [ ] AC-075: Configuration validation happens at startup before ranking begins
- [ ] AC-076: Invalid configuration logs detailed errors with actionable messages
- [ ] AC-077: Weight sum validation succeeds for sum = 1.0 ± 0.01
- [ ] AC-078: Boost factors are clamped to maximum 3.0x
- [ ] AC-079: Penalty factors are clamped to minimum 0.1x
- [ ] AC-080: Relevance weight is enforced to be ≥0.25 (prevent manipulation)
- [ ] AC-081: Suspicious boost patterns (malware, backdoor, etc.) are rejected
- [ ] AC-082: Configuration errors do not crash the system
- [ ] AC-083: Default configuration works well for 80% of use cases

### Error Handling (AC-084 to AC-092)

- [ ] AC-084: Missing chunk metadata defaults to neutral scores without errors
- [ ] AC-085: Missing file modification time uses current time or neutral score
- [ ] AC-086: Empty chunk list returns empty result without throwing exceptions
- [ ] AC-087: File system errors (permission denied, file not found) are logged and handled gracefully
- [ ] AC-088: Invalid boost/penalty patterns are skipped with warnings
- [ ] AC-089: Numeric overflow/underflow is prevented by score clamping
- [ ] AC-090: Null or empty file paths are handled without crashes
- [ ] AC-091: UTF-8 decoding errors in file paths fall back to best-effort matching
- [ ] AC-092: All errors log detailed context (file path, chunk index, error message)

### Performance (AC-093 to AC-099)

- [ ] AC-093: Ranking 100 chunks completes in <10ms
- [ ] AC-094: Ranking 1000 chunks completes in <50ms
- [ ] AC-095: Ranking 10,000 chunks completes in <500ms
- [ ] AC-096: Individual chunk scoring completes in <0.1ms
- [ ] AC-097: Glob pattern matching completes in <0.01ms per pattern per chunk
- [ ] AC-098: Sorting uses in-place algorithm to minimize memory allocations
- [ ] AC-099: Parallel scoring is enabled for chunk counts >100

### Security (AC-100 to AC-108)

- [ ] AC-100: Glob patterns are validated for length (<200 chars)
- [ ] AC-101: Suspicious glob patterns (excessive alternation, wildcards) are rejected
- [ ] AC-102: Glob compilation timeout (100ms) prevents ReDoS attacks
- [ ] AC-103: Debug output sanitizes sensitive file paths (secret, password, salary keywords → [REDACTED])
- [ ] AC-104: Boost factors cannot exceed 3.0x (prevent malicious prioritization)
- [ ] AC-105: Penalty factors cannot go below 0.1x (prevent complete exclusion)
- [ ] AC-106: Relevance weight minimum (0.25) prevents relevance suppression attacks
- [ ] AC-107: Chunk count limit (50,000) prevents DoS via excessive chunks
- [ ] AC-108: Ranking timeout (5 seconds) prevents DoS via slow operations

---

## Best Practices

### Relevance Scoring

1. **Multiple signals** - Combine text similarity, recency, structure signals
2. **Weight by confidence** - Strong signals count more than weak
3. **Normalize scores** - Put all signals on comparable scale before combining
4. **Explain rankings** - Log why each result scored as it did

### Signal Types

5. **Lexical similarity** - Text matching, keyword frequency
6. **Structural proximity** - Same file, same class, same namespace
7. **Edit recency** - Recently modified files may be more relevant
8. **Usage patterns** - Files edited together tend to be related

### Tuning

9. **Configurable weights** - Allow adjustment of signal weights
10. **Test with real queries** - Validate ranking quality with example searches
11. **Collect feedback** - Was selected context useful? Learn from outcomes
12. **A/B test changes** - Compare ranking quality before/after changes

---

## Testing Requirements

### Unit Tests - RelevanceScorerTests.cs

```csharp
using Xunit;
using FluentAssertions;
using Acode.Infrastructure.ContextPacker;
using Acode.Domain.ContextPacker;
using Microsoft.Extensions.Logging.Abstractions;

namespace Acode.Infrastructure.Tests.ContextPacker
{
    public class RelevanceScorerTests
    {
        private readonly RelevanceScorer _sut;

        public RelevanceScorerTests()
        {
            _sut = new RelevanceScorer(NullLogger<RelevanceScorer>.Instance);
        }

        [Fact]
        public void Should_Score_High_Relevance_For_Exact_Keyword_Match()
        {
            // Arrange
            var chunk = new ContentChunk(
                Content: "public class UserService { public User GetUserById(int id) { return repository.Find(id); } }",
                FilePath: "src/UserService.cs",
                LineStart: 10,
                LineEnd: 15,
                TokenEstimate: 20,
                Type: ChunkType.Structural,
                Hierarchy: new[] { "UserService" },
                Source: ChunkSource.SearchResult,
                SearchScore: null);

            var query = "GetUserById";

            // Act
            var score = _sut.ComputeRelevanceScore(chunk, query);

            // Assert
            score.Should().BeGreaterThan(0.8, "exact keyword match should score high");
            score.Should().BeLessOrEqualTo(1.0, "score should be normalized");
        }

        [Fact]
        public void Should_Score_Low_Relevance_For_No_Keyword_Match()
        {
            // Arrange
            var chunk = new ContentChunk(
                Content: "public class ProductService { public Product GetProduct(int id) { } }",
                FilePath: "src/ProductService.cs",
                LineStart: 5,
                LineEnd: 10,
                TokenEstimate: 15,
                Type: ChunkType.Structural,
                Hierarchy: new[] { "ProductService" },
                Source: ChunkSource.SearchResult,
                SearchScore: null);

            var query = "UserAuthentication";

            // Act
            var score = _sut.ComputeRelevanceScore(chunk, query);

            // Assert
            score.Should().BeLessThan(0.3, "no keyword match should score low");
            score.Should().BeGreaterOrEqualTo(0.0, "score should be non-negative");
        }

        [Fact]
        public void Should_Use_Search_Score_When_Available()
        {
            // Arrange
            var chunk = new ContentChunk(
                Content: "authentication logic",
                FilePath: "src/Auth.cs",
                LineStart: 1,
                LineEnd: 5,
                TokenEstimate: 10,
                Type: ChunkType.Structural,
                Hierarchy: new[] { "Auth" },
                Source: ChunkSource.SearchResult,
                SearchScore: 0.95);

            var query = "authentication";

            // Act
            var score = _sut.ComputeRelevanceScore(chunk, query);

            // Assert
            score.Should().Be(0.95, "should use pre-computed search score");
        }

        [Fact]
        public void Should_Score_Multiple_Keywords_Higher_Than_Single()
        {
            // Arrange
            var chunkWithThreeMatches = new ContentChunk(
                Content: "user authentication service validates user credentials",
                FilePath: "src/Auth.cs",
                LineStart: 1,
                LineEnd: 5,
                TokenEstimate: 10,
                Type: ChunkType.Structural,
                Hierarchy: new[] { "Auth" },
                Source: ChunkSource.SearchResult,
                SearchScore: null);

            var chunkWithOneMatch = new ContentChunk(
                Content: "user profile display logic",
                FilePath: "src/Profile.cs",
                LineStart: 1,
                LineEnd: 5,
                TokenEstimate: 10,
                Type: ChunkType.Structural,
                Hierarchy: new[] { "Profile" },
                Source: ChunkSource.SearchResult,
                SearchScore: null);

            var query = "user authentication credentials";

            // Act
            var scoreThreeMatches = _sut.ComputeRelevanceScore(chunkWithThreeMatches, query);
            var scoreOneMatch = _sut.ComputeRelevanceScore(chunkWithOneMatch, query);

            // Assert
            scoreThreeMatches.Should().BeGreaterThan(scoreOneMatch,
                "chunk with 3 keyword matches should score higher than chunk with 1 match");
        }

        [Fact]
        public void Should_Handle_Empty_Query_Without_Error()
        {
            // Arrange
            var chunk = new ContentChunk(
                Content: "some code",
                FilePath: "src/Test.cs",
                LineStart: 1,
                LineEnd: 5,
                TokenEstimate: 5,
                Type: ChunkType.Structural,
                Hierarchy: new[] { "Test" },
                Source: ChunkSource.SearchResult,
                SearchScore: null);

            var query = "";

            // Act
            var score = _sut.ComputeRelevanceScore(chunk, query);

            // Assert
            score.Should().Be(0.5, "empty query should default to neutral score");
        }

        [Fact]
        public void Should_Normalize_Score_To_Zero_One_Range()
        {
            // Arrange
            var chunk = new ContentChunk(
                Content: "test test test test test",
                FilePath: "src/Test.cs",
                LineStart: 1,
                LineEnd: 5,
                TokenEstimate: 5,
                Type: ChunkType.Structural,
                Hierarchy: new[] { "Test" },
                Source: ChunkSource.SearchResult,
                SearchScore: null);

            var query = "test";

            // Act
            var score = _sut.ComputeRelevanceScore(chunk, query);

            // Assert
            score.Should().BeInRange(0.0, 1.0, "score must be normalized to [0.0, 1.0]");
        }

        [Fact]
        public void Should_Be_Case_Insensitive_For_Keyword_Matching()
        {
            // Arrange
            var chunk = new ContentChunk(
                Content: "UserService Authentication GetUser",
                FilePath: "src/UserService.cs",
                LineStart: 1,
                LineEnd: 5,
                TokenEstimate: 5,
                Type: ChunkType.Structural,
                Hierarchy: new[] { "UserService" },
                Source: ChunkSource.SearchResult,
                SearchScore: null);

            var queryLowercase = "userservice authentication getuser";
            var queryMixedCase = "UserService Authentication GetUser";

            // Act
            var scoreLowercase = _sut.ComputeRelevanceScore(chunk, queryLowercase);
            var scoreMixedCase = _sut.ComputeRelevanceScore(chunk, queryMixedCase);

            // Assert
            scoreLowercase.Should().Be(scoreMixedCase,
                "keyword matching should be case-insensitive");
        }
    }
}
```

### Unit Tests - SourceScorerTests.cs

```csharp
using Xunit;
using FluentAssertions;
using Acode.Infrastructure.ContextPacker;
using Acode.Domain.ContextPacker;
using Microsoft.Extensions.Logging.Abstractions;

namespace Acode.Infrastructure.Tests.ContextPacker
{
    public class SourceScorerTests
    {
        private readonly SourceScorer _sut;
        private readonly RankingConfiguration _defaultConfig;

        public SourceScorerTests()
        {
            _defaultConfig = new RankingConfiguration(
                Weights: new RankingWeights(0.5, 0.25, 0.15, 0.1),
                SourcePriorities: new SourcePriorities(
                    ToolResult: 100,
                    OpenFile: 80,
                    SearchResult: 60,
                    Reference: 40),
                Boosts: new List<BoostConfig>(),
                Penalties: new List<PenaltyConfig>());

            _sut = new SourceScorer(NullLogger<SourceScorer>.Instance);
        }

        [Fact]
        public void Should_Score_Tool_Result_At_Maximum_Priority()
        {
            // Arrange
            var chunk = new ContentChunk(
                Content: "code",
                FilePath: "src/Test.cs",
                LineStart: 1,
                LineEnd: 5,
                TokenEstimate: 5,
                Type: ChunkType.Structural,
                Hierarchy: new[] { "Test" },
                Source: ChunkSource.ToolResult,
                SearchScore: null);

            // Act
            var score = _sut.ComputeSourceScore(chunk, _defaultConfig);

            // Assert
            score.Should().Be(1.0, "tool result should normalize to 1.0 (100/100)");
        }

        [Fact]
        public void Should_Score_Open_File_At_High_Priority()
        {
            // Arrange
            var chunk = new ContentChunk(
                Content: "code",
                FilePath: "src/Test.cs",
                LineStart: 1,
                LineEnd: 5,
                TokenEstimate: 5,
                Type: ChunkType.Structural,
                Hierarchy: new[] { "Test" },
                Source: ChunkSource.OpenFile,
                SearchScore: null);

            // Act
            var score = _sut.ComputeSourceScore(chunk, _defaultConfig);

            // Assert
            score.Should().Be(0.8, "open file should normalize to 0.8 (80/100)");
        }

        [Fact]
        public void Should_Score_Search_Result_At_Medium_Priority()
        {
            // Arrange
            var chunk = new ContentChunk(
                Content: "code",
                FilePath: "src/Test.cs",
                LineStart: 1,
                LineEnd: 5,
                TokenEstimate: 5,
                Type: ChunkType.Structural,
                Hierarchy: new[] { "Test" },
                Source: ChunkSource.SearchResult,
                SearchScore: null);

            // Act
            var score = _sut.ComputeSourceScore(chunk, _defaultConfig);

            // Assert
            score.Should().Be(0.6, "search result should normalize to 0.6 (60/100)");
        }

        [Fact]
        public void Should_Score_Reference_At_Low_Priority()
        {
            // Arrange
            var chunk = new ContentChunk(
                Content: "code",
                FilePath: "src/Test.cs",
                LineStart: 1,
                LineEnd: 5,
                TokenEstimate: 5,
                Type: ChunkType.Structural,
                Hierarchy: new[] { "Test" },
                Source: ChunkSource.Reference,
                SearchScore: null);

            // Act
            var score = _sut.ComputeSourceScore(chunk, _defaultConfig);

            // Assert
            score.Should().Be(0.4, "reference should normalize to 0.4 (40/100)");
        }

        [Fact]
        public void Should_Load_Custom_Source_Priorities_From_Config()
        {
            // Arrange
            var customConfig = new RankingConfiguration(
                Weights: new RankingWeights(0.5, 0.25, 0.15, 0.1),
                SourcePriorities: new SourcePriorities(
                    ToolResult: 90,
                    OpenFile: 70,
                    SearchResult: 50,
                    Reference: 30),
                Boosts: new List<BoostConfig>(),
                Penalties: new List<PenaltyConfig>());

            var chunk = new ContentChunk(
                Content: "code",
                FilePath: "src/Test.cs",
                LineStart: 1,
                LineEnd: 5,
                TokenEstimate: 5,
                Type: ChunkType.Structural,
                Hierarchy: new[] { "Test" },
                Source: ChunkSource.OpenFile,
                SearchScore: null);

            // Act
            var score = _sut.ComputeSourceScore(chunk, customConfig);

            // Assert
            score.Should().Be(0.7, "should use custom priority 70 → normalized 0.7");
        }

        [Fact]
        public void Should_Normalize_All_Scores_To_Zero_One_Range()
        {
            // Arrange
            var sources = new[] {
                ChunkSource.ToolResult,
                ChunkSource.OpenFile,
                ChunkSource.SearchResult,
                ChunkSource.Reference
            };

            // Act & Assert
            foreach (var source in sources)
            {
                var chunk = new ContentChunk(
                    Content: "code",
                    FilePath: "src/Test.cs",
                    LineStart: 1,
                    LineEnd: 5,
                    TokenEstimate: 5,
                    Type: ChunkType.Structural,
                    Hierarchy: new[] { "Test" },
                    Source: source,
                    SearchScore: null);

                var score = _sut.ComputeSourceScore(chunk, _defaultConfig);

                score.Should().BeInRange(0.0, 1.0,
                    $"{source} score must be in [0.0, 1.0] range");
            }
        }
    }
}
```

### Unit Tests - CombinedRankerTests.cs

```csharp
using Xunit;
using FluentAssertions;
using Acode.Infrastructure.ContextPacker;
using Acode.Domain.ContextPacker;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;

namespace Acode.Infrastructure.Tests.ContextPacker
{
    public class CombinedRankerTests
    {
        private readonly CombinedRanker _sut;
        private readonly RankingConfiguration _defaultConfig;

        public CombinedRankerTests()
        {
            _defaultConfig = new RankingConfiguration(
                Weights: new RankingWeights(
                    Relevance: 0.50,
                    Source: 0.25,
                    Recency: 0.15,
                    Position: 0.10),
                SourcePriorities: new SourcePriorities(100, 80, 60, 40),
                Boosts: new List<BoostConfig>(),
                Penalties: new List<PenaltyConfig>());

            _sut = new CombinedRanker(NullLogger<CombinedRanker>.Instance);
        }

        [Fact]
        public void Should_Apply_Default_Weights_Correctly()
        {
            // Arrange
            var factorScores = new FactorScores(
                Relevance: 0.8,
                Source: 0.6,
                Recency: 0.4,
                Position: 0.5);

            // Act
            var combinedScore = _sut.ComputeCombinedScore(factorScores, _defaultConfig);

            // Assert
            var expectedScore = (0.8 * 0.50) + (0.6 * 0.25) + (0.4 * 0.15) + (0.5 * 0.10);
            combinedScore.Should().BeApproximately(expectedScore, 0.001,
                "combined score should be weighted sum of factor scores");
        }

        [Fact]
        public void Should_Apply_Custom_Weights()
        {
            // Arrange
            var customConfig = _defaultConfig with
            {
                Weights = new RankingWeights(
                    Relevance: 0.70,  // Increased relevance
                    Source: 0.10,
                    Recency: 0.10,
                    Position: 0.10)
            };

            var factorScores = new FactorScores(
                Relevance: 1.0,
                Source: 0.5,
                Recency: 0.5,
                Position: 0.5);

            // Act
            var combinedScore = _sut.ComputeCombinedScore(factorScores, customConfig);

            // Assert
            var expectedScore = (1.0 * 0.70) + (0.5 * 0.10) + (0.5 * 0.10) + (0.5 * 0.10);
            combinedScore.Should().BeApproximately(expectedScore, 0.001);
            combinedScore.Should().BeGreaterThan(0.80, "high relevance weight should dominate");
        }

        [Fact]
        public void Should_Normalize_Final_Score_To_Zero_One()
        {
            // Arrange
            var factorScores = new FactorScores(
                Relevance: 1.0,
                Source: 1.0,
                Recency: 1.0,
                Position: 1.0);

            // Act
            var combinedScore = _sut.ComputeCombinedScore(factorScores, _defaultConfig);

            // Assert
            combinedScore.Should().Be(1.0, "max factor scores with normalized weights should equal 1.0");
            combinedScore.Should().BeLessOrEqualTo(1.0, "score cannot exceed 1.0");
        }

        [Fact]
        public void Should_Sort_Chunks_By_Score_Descending()
        {
            // Arrange
            var chunks = new List<RankedChunk>
            {
                new RankedChunk("file1.cs", 1, 10, 0.5, new FactorScores(0.5, 0.5, 0.5, 0.5), _defaultConfig.Weights),
                new RankedChunk("file2.cs", 1, 10, 0.9, new FactorScores(0.9, 0.9, 0.9, 0.9), _defaultConfig.Weights),
                new RankedChunk("file3.cs", 1, 10, 0.3, new FactorScores(0.3, 0.3, 0.3, 0.3), _defaultConfig.Weights),
                new RankedChunk("file4.cs", 1, 10, 0.7, new FactorScores(0.7, 0.7, 0.7, 0.7), _defaultConfig.Weights)
            };

            // Act
            var ranked = _sut.SortByRank(chunks);

            // Assert
            ranked[0].CombinedScore.Should().Be(0.9);
            ranked[1].CombinedScore.Should().Be(0.7);
            ranked[2].CombinedScore.Should().Be(0.5);
            ranked[3].CombinedScore.Should().Be(0.3);
        }

        [Fact]
        public void Should_Handle_Tie_Deterministically_By_Secondary_Criteria()
        {
            // Arrange
            var chunks = new List<RankedChunk>
            {
                new RankedChunk("zebra.cs", 1, 10, 0.8, new FactorScores(0.8, 0.6, 0.8, 0.8), _defaultConfig.Weights),
                new RankedChunk("alpha.cs", 1, 10, 0.8, new FactorScores(0.8, 0.6, 0.8, 0.8), _defaultConfig.Weights)
            };

            // Act
            var ranked = _sut.SortByRank(chunks);

            // Assert
            ranked[0].FilePath.Should().Be("alpha.cs", "tie-breaking should sort alphabetically by file path");
            ranked[1].FilePath.Should().Be("zebra.cs");
        }

        [Fact]
        public void Should_Apply_Min_Score_Threshold()
        {
            // Arrange
            var configWithThreshold = _defaultConfig with
            {
                MinScoreThreshold = 0.5
            };

            var chunks = new List<RankedChunk>
            {
                new RankedChunk("file1.cs", 1, 10, 0.8, new FactorScores(0.8, 0.8, 0.8, 0.8), _defaultConfig.Weights),
                new RankedChunk("file2.cs", 1, 10, 0.3, new FactorScores(0.3, 0.3, 0.3, 0.3), _defaultConfig.Weights),
                new RankedChunk("file3.cs", 1, 10, 0.6, new FactorScores(0.6, 0.6, 0.6, 0.6), _defaultConfig.Weights)
            };

            // Act
            var filtered = _sut.ApplyThreshold(chunks, configWithThreshold);

            // Assert
            filtered.Should().HaveCount(2, "chunks with score < 0.5 should be filtered out");
            filtered[0].CombinedScore.Should().Be(0.8);
            filtered[1].CombinedScore.Should().Be(0.6);
        }

        [Fact]
        public void Should_Handle_Zero_Weight_Factor()
        {
            // Arrange
            var configZeroRecency = _defaultConfig with
            {
                Weights = new RankingWeights(
                    Relevance: 0.60,
                    Source: 0.30,
                    Recency: 0.0,   // Zero weight
                    Position: 0.10)
            };

            var factorScores = new FactorScores(
                Relevance: 0.8,
                Source: 0.6,
                Recency: 1.0,  // Should have no effect
                Position: 0.5);

            // Act
            var combinedScore = _sut.ComputeCombinedScore(factorScores, configZeroRecency);

            // Assert
            var expectedScore = (0.8 * 0.60) + (0.6 * 0.30) + (0.0 * 0.0) + (0.5 * 0.10);
            combinedScore.Should().BeApproximately(expectedScore, 0.001);
        }
    }
}
```

### Integration Tests - RankingIntegrationTests.cs

```csharp
using Xunit;
using FluentAssertions;
using Acode.Infrastructure.ContextPacker;
using Acode.Domain.ContextPacker;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace Acode.Integration.Tests.ContextPacker
{
    public class RankingIntegrationTests
    {
        [Fact]
        public void Should_Rank_Real_Chunks_End_To_End()
        {
            // Arrange
            var config = new RankingConfiguration(
                Weights: new RankingWeights(0.50, 0.25, 0.15, 0.10),
                SourcePriorities: new SourcePriorities(100, 80, 60, 40),
                Boosts: new List<BoostConfig>
                {
                    new BoostConfig("src/core/**", 1.2)
                },
                Penalties: new List<PenaltyConfig>
                {
                    new PenaltyConfig("**/tests/**", 0.7)
                });

            var chunks = new List<ContentChunk>
            {
                new ContentChunk(
                    "public class UserService { }",
                    "src/core/UserService.cs",
                    1, 10, 20,
                    ChunkType.Structural,
                    new[] { "UserService" },
                    ChunkSource.ToolResult,
                    null),

                new ContentChunk(
                    "public class UserServiceTests { }",
                    "tests/UserServiceTests.cs",
                    1, 10, 20,
                    ChunkType.Structural,
                    new[] { "UserServiceTests" },
                    ChunkSource.SearchResult,
                    0.8),

                new ContentChunk(
                    "public interface IUserService { }",
                    "src/IUserService.cs",
                    1, 5, 10,
                    ChunkType.Structural,
                    new[] { "IUserService" },
                    ChunkSource.OpenFile,
                    null)
            };

            var ranker = new FullRankingPipeline(
                NullLogger<FullRankingPipeline>.Instance,
                config);

            // Act
            var ranked = ranker.RankChunks(chunks, "UserService");

            // Assert
            ranked.Should().HaveCount(3);

            // First should be UserService.cs (tool result + core boost)
            ranked[0].FilePath.Should().Be("src/core/UserService.cs");
            ranked[0].CombinedScore.Should().BeGreaterThan(0.9);

            // Test file should rank lower due to penalty
            var testFile = ranked.Single(c => c.FilePath.Contains("tests"));
            testFile.CombinedScore.Should().BeLessThan(ranked[0].CombinedScore);
        }

        [Fact]
        public void Should_Rank_Large_Chunk_Set_Within_Performance_Target()
        {
            // Arrange
            var config = new RankingConfiguration(
                Weights: new RankingWeights(0.50, 0.25, 0.15, 0.10),
                SourcePriorities: new SourcePriorities(100, 80, 60, 40),
                Boosts: new List<BoostConfig>(),
                Penalties: new List<PenaltyConfig>());

            var chunks = Enumerable.Range(0, 1000).Select(i =>
                new ContentChunk(
                    $"code chunk {i}",
                    $"src/File{i}.cs",
                    1, 10, 20,
                    ChunkType.Structural,
                    new[] { $"Class{i}" },
                    ChunkSource.SearchResult,
                    (double)i / 1000.0)).ToList();

            var ranker = new FullRankingPipeline(
                NullLogger<FullRankingPipeline>.Instance,
                config);

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var ranked = ranker.RankChunks(chunks, "query");
            stopwatch.Stop();

            // Assert
            ranked.Should().HaveCount(1000);
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(50,
                "ranking 1000 chunks should complete within 50ms");

            // Verify descending order
            for (int i = 0; i < ranked.Count - 1; i++)
            {
                ranked[i].CombinedScore.Should().BeGreaterOrEqualTo(ranked[i + 1].CombinedScore,
                    "chunks should be sorted in descending score order");
            }
        }

        [Fact]
        public void Should_Produce_Stable_Deterministic_Ranking()
        {
            // Arrange
            var config = new RankingConfiguration(
                Weights: new RankingWeights(0.50, 0.25, 0.15, 0.10),
                SourcePriorities: new SourcePriorities(100, 80, 60, 40),
                Boosts: new List<BoostConfig>(),
                Penalties: new List<PenaltyConfig>());

            var chunks = new List<ContentChunk>
            {
                new ContentChunk("code A", "FileA.cs", 1, 10, 20, ChunkType.Structural,
                    new[] { "A" }, ChunkSource.SearchResult, 0.7),
                new ContentChunk("code B", "FileB.cs", 1, 10, 20, ChunkType.Structural,
                    new[] { "B" }, ChunkSource.SearchResult, 0.8),
                new ContentChunk("code C", "FileC.cs", 1, 10, 20, ChunkType.Structural,
                    new[] { "C" }, ChunkSource.SearchResult, 0.6)
            };

            var ranker = new FullRankingPipeline(
                NullLogger<FullRankingPipeline>.Instance,
                config);

            // Act
            var ranked1 = ranker.RankChunks(chunks, "query");
            var ranked2 = ranker.RankChunks(chunks, "query");

            // Assert
            ranked1.Should().HaveCount(ranked2.Count);

            for (int i = 0; i < ranked1.Count; i++)
            {
                ranked1[i].FilePath.Should().Be(ranked2[i].FilePath,
                    $"ranking should be deterministic - position {i} should match");
                ranked1[i].CombinedScore.Should().Be(ranked2[i].CombinedScore,
                    $"scores should be deterministic - position {i} should match");
            }
        }
    }
}
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Rank 100 chunks | 5ms | 10ms |
| Rank 1000 chunks | 25ms | 50ms |
| Score computation | 0.5ms | 1ms |

---

## User Verification Steps

### Scenario 1: Verify Relevance Scoring with Exact Keyword Match

**Objective:** Confirm that chunks containing exact query keywords rank higher than unrelated chunks

**Prerequisites:**
- Acode CLI installed and configured
- Test repository cloned at `/test-repo`
- Configuration file at `/test-repo/.agent/config.yml` with default ranking weights

**Steps:**

1. Navigate to test repository:
   ```bash
   cd /test-repo
   ```

2. Query for "UserAuthentication" functionality:
   ```bash
   acode context debug-ranking "UserAuthentication"
   ```

3. Observe the debug output showing top 10 ranked chunks

**Expected Results:**

```
Ranking Debug (top 10 of 247)
────────────────────────────────────────

1. src/Auth/UserAuthentication.cs:12-45 (score: 0.92)
   Relevance: 0.98 × 0.50 = 0.490
   Source:    0.80 × 0.25 = 0.200
   Recency:   0.85 × 0.15 = 0.128
   Position:  0.70 × 0.10 = 0.070

2. src/Auth/IUserAuthentication.cs:1-20 (score: 0.88)
   Relevance: 0.95 × 0.50 = 0.475
   ...

10. src/Services/NotificationService.cs:50-80 (score: 0.42)
   Relevance: 0.15 × 0.50 = 0.075
   ...
```

**Verification:** Chunks with "UserAuthentication" in filename or content should appear in top 3 results. Irrelevant chunks (NotificationService) should rank significantly lower.

---

### Scenario 2: Verify Source Priority (Tool Result > Open File > Search Result)

**Objective:** Confirm that chunks from tool results rank higher than chunks from search results

**Prerequisites:**
- Same as Scenario 1
- File `src/UserService.cs` currently open in editor

**Steps:**

1. Simulate ranking with mixed sources:
   ```bash
   acode context rank --sources "tool:src/UserService.cs,open:src/ProductService.cs,search:src/OrderService.cs" --query "service"
   ```

2. View ranking output:
   ```bash
   acode context show-ranked
   ```

**Expected Results:**

```
Ranked Chunks (3 total):

1. src/UserService.cs (source: tool_result, score: 0.93)
2. src/ProductService.cs (source: open_file, score: 0.81)
3. src/OrderService.cs (source: search_result, score: 0.68)
```

**Verification:** Tool result chunk ranks first, open file second, search result third, regardless of content relevance (assuming similar relevance scores).

---

### Scenario 3: Verify Recency Scoring with Time Decay

**Objective:** Confirm that recently modified files score higher than old files

**Prerequisites:**
- Same as Scenario 1
- Files: `recent.cs` (modified 2 hours ago), `old.cs` (modified 30 days ago)

**Steps:**

1. Check file modification times:
   ```bash
   ls -lt src/recent.cs src/old.cs
   ```

2. Rank both files:
   ```bash
   acode context rank --files "src/recent.cs,src/old.cs" --query "test"
   ```

3. Enable debug output to see recency scores:
   ```bash
   acode context debug-ranking "test"
   ```

**Expected Results:**

```
1. src/recent.cs:1-10 (score: 0.85)
   Relevance: 0.70 × 0.50 = 0.350
   Source:    0.60 × 0.25 = 0.150
   Recency:   0.97 × 0.15 = 0.146  ← High recency (2 hours ago)
   Position:  0.70 × 0.10 = 0.070

2. src/old.cs:1-10 (score: 0.61)
   Relevance: 0.70 × 0.50 = 0.350
   Source:    0.60 × 0.25 = 0.150
   Recency:   0.08 × 0.15 = 0.012  ← Low recency (30 days ago)
   Position:  0.70 × 0.10 = 0.070
```

**Verification:** Recent file scores 0.85, old file scores 0.61. Recency factor is 0.97 for recent vs 0.08 for old.

---

### Scenario 4: Verify Path Boost Configuration

**Objective:** Confirm that configured path boosts increase chunk scores

**Prerequisites:**
- Same as Scenario 1
- Files: `src/core/CoreService.cs`, `src/util/UtilService.cs`

**Steps:**

1. Edit `.agent/config.yml` to add core boost:
   ```yaml
   context:
     ranking:
       boosts:
         - pattern: "src/core/**"
           factor: 1.3
   ```

2. Reload configuration:
   ```bash
   acode config reload
   ```

3. Rank files with similar content:
   ```bash
   acode context rank --files "src/core/CoreService.cs,src/util/UtilService.cs" --query "service"
   ```

**Expected Results:**

```
1. src/core/CoreService.cs (score: 0.91)  ← Boosted by 1.3x
2. src/util/UtilService.cs (score: 0.70)  ← No boost
```

**Verification:** Core file scores ~30% higher than util file due to 1.3x boost factor.

---

### Scenario 5: Verify Test File Penalty

**Objective:** Confirm that test files receive penalty and rank lower than implementation files

**Prerequisites:**
- Same as Scenario 1
- Files: `src/UserService.cs`, `tests/UserServiceTests.cs`

**Steps:**

1. Configure test file penalty in `.agent/config.yml`:
   ```yaml
   context:
     ranking:
       penalties:
         - pattern: "**/tests/**"
           factor: 0.7
   ```

2. Reload configuration:
   ```bash
   acode config reload
   ```

3. Search for "UserService":
   ```bash
   acode context rank --query "UserService"
   ```

**Expected Results:**

```
1. src/UserService.cs (score: 0.88)     ← No penalty
2. tests/UserServiceTests.cs (score: 0.62)  ← Penalized by 0.7x
```

**Verification:** Test file scores ~30% lower than implementation file due to 0.7x penalty.

---

### Scenario 6: Verify Custom Weight Configuration

**Objective:** Confirm that custom ranking weights change chunk ordering

**Prerequisites:**
- Same as Scenario 1

**Steps:**

1. Set default weights and rank:
   ```yaml
   weights:
     relevance: 0.50
     source: 0.25
     recency: 0.15
     position: 0.10
   ```
   ```bash
   acode context rank --query "payment"
   ```
   Note the top result.

2. Change to recency-focused weights:
   ```yaml
   weights:
     relevance: 0.30
     source: 0.10
     recency: 0.50  # Increased
     position: 0.10
   ```
   ```bash
   acode config reload
   acode context rank --query "payment"
   ```

**Expected Results:**

With default weights: `src/Payment/PaymentService.cs` ranks first (high relevance)

With recency weights: `src/Payment/RecentPaymentFix.cs` ranks first (modified yesterday)

**Verification:** Changing weights changes ranking order. Recency-focused config prioritizes recently modified files.

---

### Scenario 7: Verify Deterministic Tie-Breaking

**Objective:** Confirm that identical scores produce consistent ordering across multiple runs

**Prerequisites:**
- Same as Scenario 1
- Three files with identical content and modification times

**Steps:**

1. Create identical test files:
   ```bash
   echo "public class TestA { }" > src/TestA.cs
   echo "public class TestB { }" > src/TestB.cs
   echo "public class TestC { }" > src/TestC.cs
   ```

2. Rank files multiple times:
   ```bash
   acode context rank --query "Test" > run1.txt
   acode context rank --query "Test" > run2.txt
   acode context rank --query "Test" > run3.txt
   ```

3. Compare outputs:
   ```bash
   diff run1.txt run2.txt && diff run2.txt run3.txt
   ```

**Expected Results:**

```
No differences found (files are identical)

Ranking order (all runs):
1. src/TestA.cs (score: 0.75)  ← Alphabetically first
2. src/TestB.cs (score: 0.75)
3. src/TestC.cs (score: 0.75)  ← Alphabetically last
```

**Verification:** Identical scores are tie-broken alphabetically by file path. Order is identical across all runs.

---

### Scenario 8: Verify Minimum Score Threshold Filtering

**Objective:** Confirm that chunks below threshold are excluded from results

**Prerequisites:**
- Same as Scenario 1

**Steps:**

1. Configure minimum score threshold:
   ```yaml
   context:
     ranking:
       min_score: 0.40
   ```

2. Reload and rank with broad query:
   ```bash
   acode config reload
   acode context rank --query "code" --show-all
   ```

3. Count total results:
   ```bash
   acode context rank --query "code" | wc -l
   ```

**Expected Results:**

Without threshold (min_score: 0.0): 347 chunks returned

With threshold (min_score: 0.40): 89 chunks returned

All returned chunks have score ≥ 0.40:
```
1. src/CodeGenerator.cs (score: 0.92)
2. src/CodeAnalyzer.cs (score: 0.85)
...
89. src/Utils.cs (score: 0.40)
```

**Verification:** Low-scoring chunks (score < 0.40) are filtered out. Result count drops from 347 to 89.

---

### Scenario 9: Verify Performance Target (1000 chunks < 50ms)

**Objective:** Confirm that ranking 1000 chunks completes within 50ms performance target

**Prerequisites:**
- Same as Scenario 1
- Large repository with 5000+ files

**Steps:**

1. Generate 1000 test chunks:
   ```bash
   acode test generate-chunks --count 1000 --output chunks.json
   ```

2. Benchmark ranking performance:
   ```bash
   acode context benchmark --chunks chunks.json --iterations 10
   ```

**Expected Results:**

```
Ranking Benchmark Results:
──────────────────────────
Chunks: 1000
Iterations: 10

Run 1: 42ms
Run 2: 38ms
Run 3: 41ms
Run 4: 39ms
Run 5: 40ms
Run 6: 43ms
Run 7: 37ms
Run 8: 41ms
Run 9: 39ms
Run 10: 42ms

Average: 40.2ms
Min: 37ms
Max: 43ms
P95: 42.6ms

✓ PASS: All runs completed within 50ms target
```

**Verification:** Average ranking time is ~40ms, well within the 50ms target. P95 is 42.6ms.

---

### Scenario 10: Verify Debug Output Sanitizes Sensitive Paths

**Objective:** Confirm that debug output redacts sensitive file paths to prevent information leakage

**Prerequisites:**
- Same as Scenario 1
- Sensitive files: `src/secrets/ApiKeys.cs`, `internal/salary/SalaryCalculator.cs`

**Steps:**

1. Enable debug logging:
   ```bash
   export ACODE_LOG_LEVEL=DEBUG
   ```

2. Rank including sensitive files:
   ```bash
   acode context debug-ranking "authentication" 2>&1 | tee debug.log
   ```

3. Check for sensitive paths in debug output:
   ```bash
   grep -i "secret\|salary\|password" debug.log
   ```

**Expected Results:**

Debug output shows sanitized paths:
```
Ranking Debug (top 10 of 89)
────────────────────────────

3. src/[REDACTED]/ApiKeys.cs:1-10 (score: 0.78)
   ...

7. [REDACTED]/[REDACTED]/SalaryCalculator.cs:50-80 (score: 0.65)
   ...
```

**Verification:** Sensitive directory names ("secrets", "salary") are replaced with `[REDACTED]`. Full paths are never exposed in debug logs.

---

## Implementation Prompt

**Objective:** Implement a multi-factor weighted ranking system that combines relevance, source priority, recency, and position to score and rank content chunks for context window packing.

**Key Requirements:**
- Configurable weights for each factor (default: relevance 50%, source 25%, recency 15%, position 10%)
- Deterministic tie-breaking (score → source priority → path alphabetical → line number)
- Boost/penalty system via glob patterns
- Minimum score threshold filtering
- Performance targets: 1000 chunks ranked in <50ms

### File Structure

```
src/AgenticCoder.Domain/
├── Context/
│   ├── IRanker.cs
│   ├── IRelevanceScorer.cs
│   ├── ISourceScorer.cs
│   ├── IRecencyScorer.cs
│   ├── IPositionScorer.cs
│   ├── RankedChunk.cs
│   └── RankingModels.cs
│
src/AgenticCoder.Infrastructure/
├── Context/
│   └── Ranking/
│       ├── ChunkRanker.cs
│       ├── RelevanceScorer.cs
│       ├── SourceScorer.cs
│       ├── RecencyScorer.cs
│       ├── PositionScorer.cs
│       ├── BoostPenaltyApplicator.cs
│       └── RankingConfiguration.cs
│
src/AgenticCoder.Infrastructure/
└── DependencyInjection/
    └── RankingServiceExtensions.cs
```

---

### Domain Layer - Interfaces and Models

**File: `src/AgenticCoder.Domain/Context/IRanker.cs`**

```csharp
namespace AgenticCoder.Domain.Context;

/// <summary>
/// Multi-factor ranking service that combines relevance, source priority, recency, and position
/// to produce a scored and ordered list of content chunks for context window packing.
/// </summary>
public interface IRanker
{
    /// <summary>
    /// Ranks a collection of chunks using multi-factor weighted scoring.
    /// </summary>
    /// <param name="chunks">The chunks to rank.</param>
    /// <param name="context">The ranking context (query, current file, etc.).</param>
    /// <param name="options">Ranking configuration options.</param>
    /// <returns>Ranked chunks ordered by descending score, with deterministic tie-breaking.</returns>
    IReadOnlyList<RankedChunk> Rank(
        IReadOnlyList<ContentChunk> chunks,
        RankingContext context,
        RankingOptions options);
}

/// <summary>
/// Represents a ranked chunk with its computed score and factor breakdown.
/// </summary>
public sealed record RankedChunk(
    ContentChunk Chunk,
    double Score,
    RankingFactors Factors);

/// <summary>
/// Context information for ranking (query, current file, etc.).
/// </summary>
public sealed record RankingContext(
    string Query,
    string? CurrentFilePath = null,
    DateTime? CurrentTime = null);

/// <summary>
/// Configuration options for ranking behavior.
/// </summary>
public sealed record RankingOptions(
    RankingWeights Weights,
    double MinimumScore = 0.0,
    bool EnableBoostPenalty = true,
    bool EnableDebugOutput = false);

/// <summary>
/// Weights for each ranking factor (must sum to 1.0).
/// </summary>
public sealed record RankingWeights(
    double Relevance = 0.50,
    double Source = 0.25,
    double Recency = 0.15,
    double Position = 0.10)
{
    public void Validate()
    {
        if (Math.Abs(Relevance + Source + Recency + Position - 1.0) > 0.001)
            throw new ArgumentException("Weights must sum to 1.0");
        if (Relevance < 0 || Source < 0 || Recency < 0 || Position < 0)
            throw new ArgumentException("Weights cannot be negative");
    }
}

/// <summary>
/// Breakdown of individual factor scores for a chunk.
/// </summary>
public sealed record RankingFactors(
    double Relevance,
    double Source,
    double Recency,
    double Position,
    double BoostPenalty = 1.0);
```

**File: `src/AgenticCoder.Domain/Context/IRelevanceScorer.cs`**

```csharp
namespace AgenticCoder.Domain.Context;

/// <summary>
/// Scores chunks based on keyword relevance to the query.
/// Uses TF-IDF-inspired keyword matching and optional search engine scores.
/// </summary>
public interface IRelevanceScorer
{
    /// <summary>
    /// Computes relevance score (0.0 to 1.0) based on query keyword matches.
    /// </summary>
    double ComputeRelevanceScore(ContentChunk chunk, string query);
}
```

**File: `src/AgenticCoder.Domain/Context/ISourceScorer.cs`**

```csharp
namespace AgenticCoder.Domain.Context;

/// <summary>
/// Scores chunks based on their source type (tool result > open file > search result > reference).
/// </summary>
public interface ISourceScorer
{
    /// <summary>
    /// Computes source priority score (0.0 to 1.0) based on chunk source type.
    /// </summary>
    double ComputeSourceScore(ContentChunk chunk);
}
```

**File: `src/AgenticCoder.Domain/Context/IRecencyScorer.cs`**

```csharp
namespace AgenticCoder.Domain.Context;

/// <summary>
/// Scores chunks based on file modification time with exponential decay.
/// </summary>
public interface IRecencyScorer
{
    /// <summary>
    /// Computes recency score (0.0 to 1.0) based on file modification time.
    /// Uses exponential decay: score = 0.5^(hours_old / half_life).
    /// </summary>
    double ComputeRecencyScore(ContentChunk chunk, DateTime currentTime);
}
```

**File: `src/AgenticCoder.Domain/Context/IPositionScorer.cs`**

```csharp
namespace AgenticCoder.Domain.Context;

/// <summary>
/// Scores chunks based on their position in the file (earlier = higher score).
/// </summary>
public interface IPositionScorer
{
    /// <summary>
    /// Computes position score (0.0 to 1.0) based on line number in file.
    /// Earlier chunks (closer to top) score higher.
    /// </summary>
    double ComputePositionScore(ContentChunk chunk);
}
```

---

### Infrastructure Layer - Implementations

**File: `src/AgenticCoder.Infrastructure/Context/Ranking/RelevanceScorer.cs`**

```csharp
namespace AgenticCoder.Infrastructure.Context.Ranking;

using AgenticCoder.Domain.Context;
using Microsoft.Extensions.Logging;

public sealed class RelevanceScorer : IRelevanceScorer
{
    private readonly ILogger<RelevanceScorer> _logger;

    public RelevanceScorer(ILogger<RelevanceScorer> logger)
    {
        _logger = logger;
    }

    public double ComputeRelevanceScore(ContentChunk chunk, string query)
    {
        // If chunk has pre-computed search score, use it
        if (chunk.SearchScore.HasValue)
        {
            return NormalizeScore(chunk.SearchScore.Value);
        }

        // Otherwise, compute keyword-based relevance
        var keywords = ExtractKeywords(query);
        if (keywords.Count == 0)
        {
            return 0.0;
        }

        var matchCount = 0;
        var totalKeywords = keywords.Count;

        foreach (var keyword in keywords)
        {
            if (ContainsKeyword(chunk, keyword))
            {
                matchCount++;
            }
        }

        var score = (double)matchCount / totalKeywords;
        return NormalizeScore(score);
    }

    private static List<string> ExtractKeywords(string query)
    {
        return query
            .Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2)
            .Select(w => w.ToLowerInvariant())
            .Distinct()
            .ToList();
    }

    private static bool ContainsKeyword(ContentChunk chunk, string keyword)
    {
        return chunk.Content.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
               chunk.FilePath.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private static double NormalizeScore(double score)
    {
        return Math.Clamp(score, 0.0, 1.0);
    }
}
```

**File: `src/AgenticCoder.Infrastructure/Context/Ranking/SourceScorer.cs`**

```csharp
namespace AgenticCoder.Infrastructure.Context.Ranking;

using AgenticCoder.Domain.Context;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class SourceScorer : ISourceScorer
{
    private readonly ILogger<SourceScorer> _logger;
    private readonly RankingConfiguration _config;

    private static readonly Dictionary<ChunkSource, int> DefaultPriorities = new()
    {
        { ChunkSource.ToolResult, 100 },
        { ChunkSource.OpenFile, 80 },
        { ChunkSource.SearchResult, 60 },
        { ChunkSource.Reference, 40 }
    };

    public SourceScorer(
        ILogger<SourceScorer> logger,
        IOptions<RankingConfiguration> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    public double ComputeSourceScore(ContentChunk chunk)
    {
        var priorities = _config.SourcePriorities ?? DefaultPriorities;

        if (!priorities.TryGetValue(chunk.Source, out var priority))
        {
            _logger.LogWarning("Unknown chunk source {Source}, defaulting to 0", chunk.Source);
            return 0.0;
        }

        var maxPriority = priorities.Values.Max();
        var score = (double)priority / maxPriority;

        return NormalizeScore(score);
    }

    private static double NormalizeScore(double score)
    {
        return Math.Clamp(score, 0.0, 1.0);
    }
}
```

**File: `src/AgenticCoder.Infrastructure/Context/Ranking/RecencyScorer.cs`**

```csharp
namespace AgenticCoder.Infrastructure.Context.Ranking;

using AgenticCoder.Domain.Context;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class RecencyScorer : IRecencyScorer
{
    private readonly ILogger<RecencyScorer> _logger;
    private readonly RankingConfiguration _config;

    public RecencyScorer(
        ILogger<RecencyScorer> logger,
        IOptions<RankingConfiguration> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    public double ComputeRecencyScore(ContentChunk chunk, DateTime currentTime)
    {
        var lastModified = chunk.LastModified ?? currentTime.AddDays(-30);

        // Reject future timestamps (clock skew or tampering)
        if (lastModified > currentTime)
        {
            _logger.LogWarning("Chunk {FilePath} has future timestamp, clamping to current time",
                chunk.FilePath);
            lastModified = currentTime;
        }

        var ageHours = (currentTime - lastModified).TotalHours;
        var halfLife = _config.RecencyHalfLifeHours ?? 24.0;

        // Exponential decay: score = 0.5^(age / half_life)
        var score = Math.Pow(0.5, ageHours / halfLife);

        return NormalizeScore(score);
    }

    private static double NormalizeScore(double score)
    {
        return Math.Clamp(score, 0.0, 1.0);
    }
}
```

**File: `src/AgenticCoder.Infrastructure/Context/Ranking/PositionScorer.cs`**

```csharp
namespace AgenticCoder.Infrastructure.Context.Ranking;

using AgenticCoder.Domain.Context;
using Microsoft.Extensions.Logging;

public sealed class PositionScorer : IPositionScorer
{
    private readonly ILogger<PositionScorer> _logger;

    public PositionScorer(ILogger<PositionScorer> logger)
    {
        _logger = logger;
    }

    public double ComputePositionScore(ContentChunk chunk)
    {
        // Earlier chunks (closer to top of file) score higher
        // Use inverse logarithmic decay based on line number
        var lineNumber = chunk.LineStart;

        if (lineNumber <= 0)
        {
            return 1.0; // Top of file
        }

        // Score decays logarithmically: score = 1 / (1 + log10(lineNumber))
        var score = 1.0 / (1.0 + Math.Log10(lineNumber));

        return NormalizeScore(score);
    }

    private static double NormalizeScore(double score)
    {
        return Math.Clamp(score, 0.0, 1.0);
    }
}
```

**File: `src/AgenticCoder.Infrastructure/Context/Ranking/BoostPenaltyApplicator.cs`**

```csharp
namespace AgenticCoder.Infrastructure.Context.Ranking;

using AgenticCoder.Domain.Context;
using DotNet.Globbing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class BoostPenaltyApplicator
{
    private readonly ILogger<BoostPenaltyApplicator> _logger;
    private readonly RankingConfiguration _config;
    private readonly List<(Glob Pattern, double Factor)> _boostRules;
    private readonly List<(Glob Pattern, double Factor)> _penaltyRules;

    public BoostPenaltyApplicator(
        ILogger<BoostPenaltyApplicator> logger,
        IOptions<RankingConfiguration> config)
    {
        _logger = logger;
        _config = config.Value;

        _boostRules = CompileRules(_config.PathBoosts ?? new());
        _penaltyRules = CompileRules(_config.PathPenalties ?? new());
    }

    public double ApplyBoostPenalty(ContentChunk chunk, double baseScore)
    {
        var factor = 1.0;

        // Apply boosts (multiplicative)
        foreach (var (pattern, boostFactor) in _boostRules)
        {
            if (pattern.IsMatch(chunk.FilePath))
            {
                factor *= boostFactor;
                _logger.LogDebug("Boost applied: {Pattern} → {Factor}x for {Path}",
                    pattern, boostFactor, chunk.FilePath);
            }
        }

        // Apply penalties (multiplicative)
        foreach (var (pattern, penaltyFactor) in _penaltyRules)
        {
            if (pattern.IsMatch(chunk.FilePath))
            {
                factor *= penaltyFactor;
                _logger.LogDebug("Penalty applied: {Pattern} → {Factor}x for {Path}",
                    pattern, penaltyFactor, chunk.FilePath);
            }
        }

        var finalScore = baseScore * factor;
        return Math.Clamp(finalScore, 0.0, 1.0);
    }

    private List<(Glob Pattern, double Factor)> CompileRules(
        Dictionary<string, double> rules)
    {
        var compiled = new List<(Glob, double)>();

        foreach (var (pattern, factor) in rules)
        {
            try
            {
                var glob = Glob.Parse(pattern);
                compiled.Add((glob, factor));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compile glob pattern: {Pattern}", pattern);
            }
        }

        return compiled;
    }
}
```

**File: `src/AgenticCoder.Infrastructure/Context/Ranking/ChunkRanker.cs`**

```csharp
namespace AgenticCoder.Infrastructure.Context.Ranking;

using AgenticCoder.Domain.Context;
using Microsoft.Extensions.Logging;

public sealed class ChunkRanker : IRanker
{
    private readonly ILogger<ChunkRanker> _logger;
    private readonly IRelevanceScorer _relevanceScorer;
    private readonly ISourceScorer _sourceScorer;
    private readonly IRecencyScorer _recencyScorer;
    private readonly IPositionScorer _positionScorer;
    private readonly BoostPenaltyApplicator _boostPenalty;

    public ChunkRanker(
        ILogger<ChunkRanker> logger,
        IRelevanceScorer relevanceScorer,
        ISourceScorer sourceScorer,
        IRecencyScorer recencyScorer,
        IPositionScorer positionScorer,
        BoostPenaltyApplicator boostPenalty)
    {
        _logger = logger;
        _relevanceScorer = relevanceScorer;
        _sourceScorer = sourceScorer;
        _recencyScorer = recencyScorer;
        _positionScorer = positionScorer;
        _boostPenalty = boostPenalty;
    }

    public IReadOnlyList<RankedChunk> Rank(
        IReadOnlyList<ContentChunk> chunks,
        RankingContext context,
        RankingOptions options)
    {
        options.Weights.Validate();

        var currentTime = context.CurrentTime ?? DateTime.UtcNow;
        var rankedChunks = new List<RankedChunk>(chunks.Count);

        foreach (var chunk in chunks)
        {
            var factors = ComputeFactors(chunk, context, currentTime);
            var combinedScore = ComputeCombinedScore(factors, options.Weights);

            // Apply boost/penalty if enabled
            var finalScore = options.EnableBoostPenalty
                ? _boostPenalty.ApplyBoostPenalty(chunk, combinedScore)
                : combinedScore;

            rankedChunks.Add(new RankedChunk(chunk, finalScore, factors));
        }

        // Sort by score descending with deterministic tie-breaking
        rankedChunks.Sort((a, b) =>
        {
            // Primary: score descending
            var scoreComparison = b.Score.CompareTo(a.Score);
            if (scoreComparison != 0) return scoreComparison;

            // Secondary: source priority descending
            var sourceComparison = b.Factors.Source.CompareTo(a.Factors.Source);
            if (sourceComparison != 0) return sourceComparison;

            // Tertiary: path alphabetical ascending
            var pathComparison = string.Compare(a.Chunk.FilePath, b.Chunk.FilePath,
                StringComparison.Ordinal);
            if (pathComparison != 0) return pathComparison;

            // Quaternary: line number ascending
            return a.Chunk.LineStart.CompareTo(b.Chunk.LineStart);
        });

        // Apply minimum score threshold
        var filtered = rankedChunks
            .Where(rc => rc.Score >= options.MinimumScore)
            .ToList();

        _logger.LogInformation("Ranked {Total} chunks, {Filtered} passed threshold {MinScore}",
            chunks.Count, filtered.Count, options.MinimumScore);

        return filtered;
    }

    private RankingFactors ComputeFactors(
        ContentChunk chunk,
        RankingContext context,
        DateTime currentTime)
    {
        var relevance = _relevanceScorer.ComputeRelevanceScore(chunk, context.Query);
        var source = _sourceScorer.ComputeSourceScore(chunk);
        var recency = _recencyScorer.ComputeRecencyScore(chunk, currentTime);
        var position = _positionScorer.ComputePositionScore(chunk);

        return new RankingFactors(relevance, source, recency, position);
    }

    private static double ComputeCombinedScore(
        RankingFactors factors,
        RankingWeights weights)
    {
        var score =
            (factors.Relevance * weights.Relevance) +
            (factors.Source * weights.Source) +
            (factors.Recency * weights.Recency) +
            (factors.Position * weights.Position);

        return Math.Clamp(score, 0.0, 1.0);
    }
}
```

**File: `src/AgenticCoder.Infrastructure/Context/Ranking/RankingConfiguration.cs`**

```csharp
namespace AgenticCoder.Infrastructure.Context.Ranking;

using AgenticCoder.Domain.Context;

public sealed class RankingConfiguration
{
    public Dictionary<ChunkSource, int>? SourcePriorities { get; set; }
    public double? RecencyHalfLifeHours { get; set; }
    public Dictionary<string, double>? PathBoosts { get; set; }
    public Dictionary<string, double>? PathPenalties { get; set; }
}
```

---

### Dependency Injection Setup

**File: `src/AgenticCoder.Infrastructure/DependencyInjection/RankingServiceExtensions.cs`**

```csharp
namespace AgenticCoder.Infrastructure.DependencyInjection;

using AgenticCoder.Domain.Context;
using AgenticCoder.Infrastructure.Context.Ranking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class RankingServiceExtensions
{
    public static IServiceCollection AddRankingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration
        services.Configure<RankingConfiguration>(
            configuration.GetSection("Ranking"));

        // Scorers
        services.AddSingleton<IRelevanceScorer, RelevanceScorer>();
        services.AddSingleton<ISourceScorer, SourceScorer>();
        services.AddSingleton<IRecencyScorer, RecencyScorer>();
        services.AddSingleton<IPositionScorer, PositionScorer>();

        // Boost/Penalty
        services.AddSingleton<BoostPenaltyApplicator>();

        // Ranker
        services.AddSingleton<IRanker, ChunkRanker>();

        return services;
    }
}
```

---

### Error Codes

| Code | Meaning | Resolution |
|------|---------|------------|
| ACODE-RNK-001 | Scoring operation failed due to internal error | Check logs for exception details, verify chunk data integrity |
| ACODE-RNK-002 | Invalid ranking weights configuration (not summing to 1.0) | Verify weights in config: relevance + source + recency + position = 1.0 |
| ACODE-RNK-003 | Glob pattern compilation failed for boost/penalty rule | Fix malformed glob pattern, check for ReDoS-susceptible patterns |
| ACODE-RNK-004 | Chunk count exceeds safety limit (50,000) | Reduce result set before ranking, apply stricter search filters |
| ACODE-RNK-005 | Ranking timeout exceeded (5 seconds) | Reduce chunk count, optimize scoring implementations |
| ACODE-RNK-006 | Boost/penalty factor out of allowed range | Boosts: 1.0-3.0, Penalties: 0.1-1.0 |
| ACODE-RNK-007 | Relevance weight below minimum threshold (0.25) | Increase relevance weight to at least 0.25 to ensure quality |

---

### Implementation Checklist

**Domain Layer:**
- [ ] Create `IRanker` interface with `Rank()` method
- [ ] Create `IRelevanceScorer` interface with `ComputeRelevanceScore()` method
- [ ] Create `ISourceScorer` interface with `ComputeSourceScore()` method
- [ ] Create `IRecencyScorer` interface with `ComputeRecencyScore()` method
- [ ] Create `IPositionScorer` interface with `ComputePositionScore()` method
- [ ] Create `RankedChunk` record with Chunk, Score, Factors
- [ ] Create `RankingContext` record with Query, CurrentFilePath, CurrentTime
- [ ] Create `RankingOptions` record with Weights, MinimumScore, EnableBoostPenalty
- [ ] Create `RankingWeights` record with Relevance, Source, Recency, Position, Validate()
- [ ] Create `RankingFactors` record with Relevance, Source, Recency, Position, BoostPenalty

**Infrastructure Layer - Scorers:**
- [ ] Implement `RelevanceScorer` with keyword extraction, matching, normalization
- [ ] Implement `SourceScorer` with priority lookup, normalization
- [ ] Implement `RecencyScorer` with exponential decay, future timestamp rejection
- [ ] Implement `PositionScorer` with logarithmic decay based on line number
- [ ] Implement `BoostPenaltyApplicator` with glob pattern matching, factor application

**Infrastructure Layer - Ranker:**
- [ ] Implement `ChunkRanker` with factor computation for all chunks
- [ ] Implement combined score calculation with weighted sum
- [ ] Implement sorting with deterministic tie-breaking (score → source → path → line)
- [ ] Implement minimum score threshold filtering
- [ ] Add logging for ranking summary (total, filtered, threshold)

**Configuration:**
- [ ] Create `RankingConfiguration` class with SourcePriorities, RecencyHalfLifeHours, PathBoosts, PathPenalties
- [ ] Add configuration validation (weight sum = 1.0, boost/penalty ranges)
- [ ] Add appsettings.json section with default values

**Dependency Injection:**
- [ ] Create `RankingServiceExtensions` with `AddRankingServices()` method
- [ ] Register all scorers as singletons
- [ ] Register `BoostPenaltyApplicator` as singleton
- [ ] Register `IRanker` → `ChunkRanker` as singleton
- [ ] Bind `RankingConfiguration` from appsettings

**Testing:**
- [ ] Write unit tests for `RelevanceScorer` (7 tests)
- [ ] Write unit tests for `SourceScorer` (6 tests)
- [ ] Write unit tests for `RecencyScorer` (5 tests)
- [ ] Write unit tests for `PositionScorer` (4 tests)
- [ ] Write unit tests for `BoostPenaltyApplicator` (6 tests)
- [ ] Write unit tests for `ChunkRanker` (7 tests)
- [ ] Write integration tests for end-to-end ranking (3 tests)
- [ ] Write performance tests for 100, 1000, 10000 chunks

**Documentation:**
- [ ] Add XML comments to all public interfaces and classes
- [ ] Document configuration options in appsettings.json
- [ ] Add README with usage examples
- [ ] Document error codes and resolutions

---

### Rollout Plan

**Phase 1: Individual Scorers (Week 1)**
- Implement `RelevanceScorer`, `SourceScorer`, `RecencyScorer`, `PositionScorer`
- Write unit tests for each scorer (100% coverage)
- Verify normalization (all scores 0.0-1.0)
- **Verification:** Run `dotnet test --filter "Category=Scorer"` → All pass

**Phase 2: Combined Ranker (Week 2)**
- Implement `ChunkRanker` with factor aggregation
- Implement weighted sum calculation
- Implement deterministic sorting
- Write unit tests for combined scoring and tie-breaking
- **Verification:** Run integration test with 100 real chunks → Correct order

**Phase 3: Boost/Penalty System (Week 3)**
- Implement `BoostPenaltyApplicator` with glob pattern matching
- Add configuration for PathBoosts and PathPenalties
- Test boost/penalty factor application
- **Verification:** Configure boost for `src/Core/**` → Verify 1.5x multiplier applied

**Phase 4: Configuration & Validation (Week 4)**
- Implement `RankingConfiguration` with validation
- Add weight sum validation (must equal 1.0)
- Add boost/penalty range validation (1.0-3.0, 0.1-1.0)
- Add relevance minimum enforcement (>= 0.25)
- **Verification:** Set invalid weights (sum = 0.8) → Throws ArgumentException

**Phase 5: Performance Optimization (Week 5)**
- Add chunk count limit (50,000)
- Add ranking timeout (5 seconds)
- Optimize sorting (in-place, avoid allocations)
- Add performance benchmarks
- **Verification:** Rank 1000 chunks → <50ms (P95)

**Phase 6: Debug Tooling (Week 6)**
- Add debug output mode with factor breakdown
- Add path sanitization for sensitive directories
- Add ranking summary logging
- **Verification:** Enable debug mode → Output shows factor scores for each chunk

**Phase 7: Integration & E2E Testing (Week 7)**
- Integrate with Task 016a chunking system
- Test end-to-end: chunking → ranking → context packing
- Verify query-specific ranking (relevance factor dominates)
- Verify deterministic output (same input → same order)
- **Verification:** Run full pipeline → Context window has most relevant chunks at top

---

**End of Task 016.b Specification**