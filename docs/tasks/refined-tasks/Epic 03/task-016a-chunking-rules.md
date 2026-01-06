# Task 016.a: Chunking Rules

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 016 (Context Packer)  

---

## Description

### Business Value

Chunking is the foundation of effective context assembly for Large Language Models. When the agent needs to understand code, it cannot simply include entire files—many files exceed token limits, and including irrelevant sections wastes valuable context space. Task 016.a delivers the chunking intelligence that transforms raw files into meaningful, appropriately-sized pieces that LLMs can process effectively.

The quality of chunks directly impacts agent performance. Poorly chunked code splits functions in half, separates method signatures from their bodies, or breaks up related logic. These fragmented chunks confuse the LLM and degrade response quality. Well-designed chunking respects code structure, preserves semantic units, and maintains enough context for the LLM to understand each piece independently.

Language-specific chunking provides significant advantages over naive approaches. By parsing C# with Roslyn and TypeScript with the compiler API, the chunker understands actual code structure rather than just counting lines. This structural awareness enables intelligent decisions—keeping a small method together rather than splitting it, or separating unrelated classes into distinct chunks. The result is higher-quality context that improves agent accuracy.

### Return on Investment (ROI)

**Development Cost:**
- Developer time: 80 hours (2 weeks) × $100/hour = $8,000
- Infrastructure: Roslyn SDK, TypeScript compiler API (free)
- Testing infrastructure: 20 hours × $100/hour = $2,000
- **Total development cost: $10,000**

**Annual Benefits (10-developer team, 250 working days/year):**

**1. LLM Response Quality Improvement:**
- Current (naive line-based chunking): 65% accuracy on code questions
- After (structural chunking): 88% accuracy on code questions
- Quality improvement: 35.4% relative improvement
- Fewer follow-up questions needed: 2.5 avg → 1.2 avg (52% reduction)
- Time saved per developer: (2.5 - 1.2) × 3 min/question × 50 questions/day = 195 min/day
- 195 min/day × 250 days = 48,750 min/year = 812.5 hours/year per developer
- 812.5 hours × 10 developers × $100/hour = **$812,500/year**

**2. Context Window Utilization:**
- Current waste (poor chunks): ~35% of context window contains partial/broken code
- After structural chunking: ~5% waste (only at file boundaries)
- Effective context improvement: 30% more usable context
- Reduced API calls due to better context: 15% fewer follow-ups
- API cost per developer: $120/month × 12 months = $1,440/year
- Savings: $1,440 × 0.15 × 10 developers = **$2,160/year**

**3. Debugging Time Reduction:**
- Broken chunks cause misunderstandings: 8% of agent responses contain errors
- Structural chunks reduce errors: 2% error rate (75% reduction)
- Time spent debugging agent mistakes: 30 min/day per developer
- Time saved: 30 min × 0.75 = 22.5 min/day per developer
- 22.5 min × 250 days × 10 developers = 56,250 min = 937.5 hours
- 937.5 hours × $100/hour = **$93,750/year**

**Total Annual Benefits:**
- LLM quality improvement: $812,500
- API cost savings: $2,160
- Debugging reduction: $93,750
- **Total: $908,410/year**

**ROI Calculation:**
- Investment: $10,000 (one-time)
- Annual return: $908,410
- **Payback period: 4 days**
- **Annual ROI: 9,084%**
- **5-year NPV (10% discount): $3,444,142**

### Before/After Metrics

| Metric | Before (Line-Based) | After (Structural) | Improvement |
|--------|--------------------|--------------------|-------------|
| **Chunk Quality** |
| Broken code structures | 42% of chunks | 3% of chunks | **93% reduction** |
| Self-contained chunks | 58% | 97% | **67% improvement** |
| LLM comprehension accuracy | 65% | 88% | **35% improvement** |
| **Performance** |
| Chunking time (100KB file) | 180ms | 150ms | **17% faster** |
| Memory overhead | 3.2x file size | 2.1x file size | **34% reduction** |
| Parse success rate | N/A (no parsing) | 94.5% | **New capability** |
| **Context Utilization** |
| Wasted context (broken chunks) | 35% | 5% | **86% reduction** |
| Average chunk token count | 1,850 (uneven) | 1,200 (balanced) | **35% more efficient** |
| Chunks per file | 12 (many tiny) | 6 (semantic units) | **50% reduction** |
| **Developer Experience** |
| Time to understand chunk | 45 sec | 12 sec | **73% faster** |
| Follow-up questions needed | 2.5 avg | 1.2 avg | **52% reduction** |
| Agent error rate | 8% | 2% | **75% reduction** |

### Scope

This task defines the complete chunking subsystem for the Context Packer:

1. **Structural Chunking Engine:** The core system that respects code boundaries (classes, methods, functions) when dividing files into chunks.

2. **Language-Specific Parsers:** Dedicated parsers for C#, TypeScript, and JavaScript that understand each language's structure and produce optimal chunks.

3. **Line-Based Fallback:** A universal fallback chunker for unsupported file types that chunks by line count with configurable overlap.

4. **Token Estimation:** Accurate token counting to ensure chunks fit within LLM token limits.

5. **Chunk Metadata System:** Tracking of source file, line ranges, token estimates, chunk type, and structural hierarchy for each chunk.

### Technical Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        Chunking Subsystem Architecture                       │
└─────────────────────────────────────────────────────────────────────────────┘

Input                           Processing Pipeline                      Output
──────                         ───────────────────                      ───────

File Content                   ┌──────────────────────┐
  (string)                     │ File Type Detection  │
     │                         │  ▪ Extension check   │
     │                         │  ▪ Content sniffing  │
     │                         │  ▪ Binary detection  │
     │                         └──────────────────────┘
     │                                   │
     ├───────────────────────────────────┼─────────────────────────┐
     │                                   │                         │
     ▼                                   ▼                         ▼
┌─────────────┐             ┌──────────────────────┐   ┌─────────────────────┐
│   .cs File  │             │    .ts/.js File      │   │   Other Files       │
└─────────────┘             └──────────────────────┘   └─────────────────────┘
     │                                   │                         │
     ▼                                   ▼                         ▼
┌─────────────────────┐     ┌──────────────────────────┐ ┌─────────────────────┐
│  C# Parser          │     │  TypeScript Parser       │ │  Line-Based Parser  │
│  (Roslyn)           │     │  (TS Compiler API)       │ │  (Regex/Split)      │
│                     │     │                          │ │                     │
│  Extracts:          │     │  Extracts:               │ │  Extracts:          │
│  • Namespaces       │     │  • Modules               │ │  • Lines 1-50       │
│  • Classes          │     │  • Exports               │ │  • Lines 46-95      │
│  • Interfaces       │     │  • Classes               │ │  • Lines 91-140     │
│  • Methods          │     │  • Functions             │ │  • (with overlap)   │
│  • Properties       │     │  • Arrow functions       │ │                     │
│  • Nested types     │     │  • Interfaces            │ │                     │
└─────────────────────┘     └──────────────────────────┘ └─────────────────────┘
          │                              │                         │
          │                              │                         │
          └──────────────────┬───────────┴─────────────────────────┘
                             │
                             ▼
                ┌──────────────────────────────┐
                │  Chunk Boundary Detection    │
                │  ▪ Find semantic units       │
                │  ▪ Calculate token estimates │
                │  ▪ Check size constraints    │
                │  ▪ Determine split points    │
                └──────────────────────────────┘
                             │
                             ▼
                ┌──────────────────────────────┐
                │  Chunk Size Validation       │
                │  ▪ min_tokens check          │
                │  ▪ max_tokens check          │
                │  ▪ Split large units         │
                │  ▪ Combine small units       │
                └──────────────────────────────┘
                             │
                             ▼
                ┌──────────────────────────────┐
                │  Metadata Attachment         │
                │  ▪ Source file path          │
                │  ▪ Line start/end            │
                │  ▪ Token estimate            │
                │  ▪ Chunk type                │
                │  ▪ Hierarchy                 │
                └──────────────────────────────┘
                             │
                             ▼
                    List<ContentChunk>
                    [
                      {
                        Content: "public class User...",
                        LineStart: 10,
                        LineEnd: 45,
                        TokenEstimate: 450,
                        Type: "class",
                        Hierarchy: ["MyApp", "User"]
                      },
                      ...
                    ]

Error Handling Flow:
───────────────────
Parse Error → Log Warning → Fallback to Line-Based → Continue
Token Overflow → Split at mid-point → Re-validate → Continue
Memory Limit → Stream file in chunks → Progressive processing → Continue
Binary File → Skip with warning → Return empty list → Continue
```

### Architectural Decisions

**Decision 1: Roslyn vs. Custom C# Parser**

- **Chosen:** Roslyn (Microsoft.CodeAnalysis)
- **Alternatives Considered:** Custom regex-based parser, CSharpSyntaxTree
- **Rationale:** Roslyn is the official C# compiler API with complete syntax understanding, error recovery, and support for all C# versions including latest features (records, pattern matching, etc.). Custom parsers would be fragile and require constant maintenance.
- **Trade-offs:**
  - ✅ **Benefit:** Perfect syntax understanding, handles edge cases, 99.8% parse success rate
  - ✅ **Benefit:** Automatic support for new C# features (C# 12, 13, etc.)
  - ✅ **Benefit:** Built-in error recovery for partially malformed code
  - ❌ **Cost:** Heavy dependency (~15MB NuGet package)
  - ❌ **Cost:** ~80ms parse time for medium files (acceptable for 200ms budget)
  - **Mitigation:** Cache parse trees for frequently accessed files, lazy-load Roslyn assemblies
- **Impact:** 15MB disk space, 80ms parse time, but 99.8% accuracy vs. 75% for regex

**Decision 2: TypeScript Compiler API vs. Babel Parser**

- **Chosen:** TypeScript Compiler API (tsc programmatic API)
- **Alternatives Considered:** Babel parser (@babel/parser), Acorn, Esprima
- **Rationale:** TypeScript compiler is the authoritative source for TypeScript syntax, and also handles JavaScript perfectly. Babel is designed for transpilation, not syntax analysis.
- **Trade-offs:**
  - ✅ **Benefit:** Handles both TypeScript and JavaScript with single parser
  - ✅ **Benefit:** Understands TypeScript-specific syntax (interfaces, type aliases, decorators)
  - ✅ **Benefit:** 97% parse success rate on real-world files
  - ❌ **Cost:** Requires Node.js runtime for .NET interop (if using .NET)
  - ❌ **Cost:** 120ms parse time for medium TypeScript files
  - **Mitigation:** Use embedded V8 runtime (Microsoft.ClearScript.V8) to avoid Node.js dependency, or call tsc via subprocess
- **Impact:** Additional runtime dependency, but complete TypeScript/JavaScript support

**Decision 3: Structural Chunking vs. Fixed-Size Chunking as Primary Strategy**

- **Chosen:** Structural chunking with line-based fallback
- **Alternatives Considered:** Always line-based (simpler), always token-based, hybrid approach
- **Rationale:** Structural chunks preserve semantic meaning, which is critical for LLM comprehension. A method split mid-body confuses the model and degrades accuracy by ~23%. Line-based is fast but produces inferior results.
- **Trade-offs:**
  - ✅ **Benefit:** 35% improvement in LLM comprehension accuracy (65% → 88%)
  - ✅ **Benefit:** 93% reduction in broken code structures (42% → 3%)
  - ✅ **Benefit:** Self-contained chunks that can be understood independently
  - ❌ **Cost:** 17% slower chunking (180ms → 150ms average with parsing overhead)
  - ❌ **Cost:** Requires language-specific parsers (maintenance burden)
  - ❌ **Cost:** Parse failures require fallback logic
  - **Mitigation:** Automatic fallback to line-based on parse errors, caching for repeated files, progressive parser support (start with C#, add others incrementally)
- **Impact:** Higher complexity but dramatically better results for supported languages

**Decision 4: Exact Token Counting vs. Approximation**

- **Chosen:** Exact token counting using model-specific tokenizer (tiktoken for GPT models, Claude tokenizer for Claude)
- **Alternatives Considered:** Character count × 0.25 estimate, whitespace tokenization, GPT-2 tokenizer
- **Rationale:** Token budget violations cause Context Packer failures. Approximations have 10-15% error rate, leading to budget overruns or wasted space. Exact counting ensures chunks fit within limits.
- **Trade-offs:**
  - ✅ **Benefit:** Zero budget overruns (100% accuracy)
  - ✅ **Benefit:** Optimal context utilization (no 15% safety margin needed)
  - ✅ **Benefit:** Correct handling of special tokens (code blocks, unicode)
  - ❌ **Cost:** 20-30ms tokenization time per chunk
  - ❌ **Cost:** Must maintain multiple tokenizers (GPT-4, Claude, etc.)
  - ❌ **Cost:** Tokenizer dependencies (~5MB per model)
  - **Mitigation:** LRU cache for tokenized content (95% hit rate on repeated chunks), lazy tokenizer loading
- **Impact:** Slight performance cost but eliminates budget failures and increases efficiency by 15%

**Decision 5: Split Large Methods vs. Keep Intact**

- **Chosen:** Split methods/functions exceeding max_tokens, preserve boundary comments
- **Alternatives Considered:** Always keep methods intact (fail if too large), increase token limits, skip large methods
- **Rationale:** Some methods legitimately exceed token limits (e.g., 500-line switch statements, large data initializers). Skipping them leaves gaps in context. Keeping them intact violates budget constraints. Splitting is the pragmatic solution.
- **Trade-offs:**
  - ✅ **Benefit:** No context gaps (100% file coverage)
  - ✅ **Benefit:** Respects token budget constraints
  - ✅ **Benefit:** Adds boundary markers so LLM knows chunk is partial
  - ❌ **Cost:** Split methods are harder for LLM to understand (~15% accuracy penalty)
  - ❌ **Cost:** Requires intelligent split point selection (end of logical blocks, not mid-expression)
  - ❌ **Cost:** Additional metadata to track "split" vs. "complete" chunks
  - **Mitigation:** Include method signature in each split chunk, add comments like `// [Chunk 1 of 3]`, split at logical boundaries (end of if-blocks, loops)
- **Impact:** Handles edge cases at cost of 15% accuracy penalty for very large methods (rare: <2% of methods)

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Task 016 (Context Packer) | Parent System | Chunker is invoked by Context Packer to break files into pieces |
| Task 016.b (Ranking) | Downstream | Chunks are passed to ranking system for prioritization |
| Task 016.c (Budgeting) | Downstream | Token estimates used for budget allocation |
| Task 014 (RepoFS) | File Access | Reads file content via RepoFS abstraction |
| Task 002 (Config) | Configuration | Chunk settings loaded from `.agent/config.yml` |
| Task 015 (Indexing) | Index Storage | Chunks may be cached in index for performance |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Parse error in source code | Cannot use structural chunking | Automatic fallback to line-based chunking |
| File too large for memory | Out of memory exception | Progressive chunking with streaming, memory limits |
| Unsupported file type | No structural parser available | Graceful fallback to line-based chunking |
| Token estimation inaccuracy | Chunks exceed budget | Conservative estimation with safety margin |
| Malformed unicode content | Parsing/encoding errors | Robust encoding detection, UTF-8 fallback |
| Circular includes or dependencies | Infinite loop risk | Detection and termination safeguards |
| Empty or trivial files | Wasted processing | Skip files below minimum size threshold |
| Binary file misidentified as text | Garbled chunks | Binary detection before chunking |

### Assumptions

1. Source files are predominantly text with UTF-8 encoding
2. C# files are syntactically valid or recoverable by Roslyn
3. TypeScript/JavaScript files are parseable by the TypeScript compiler
4. Files are reasonably sized (< 10MB) and fit in memory for parsing
5. The target LLM tokenizer is known for accurate token estimation
6. Chunk configuration values are validated at startup
7. Line-based chunking is an acceptable fallback for any file type
8. Overlap configuration is reasonable (not exceeding chunk size)

### Security Considerations

1. **Input Validation:** All file content must be validated before parsing to prevent parser exploits or resource exhaustion attacks.

2. **Memory Limits:** Chunking must enforce memory limits to prevent denial-of-service via extremely large files.

3. **Path Sanitization:** File paths in chunk metadata must be sanitized to prevent information leakage.

4. **No Code Execution:** Parsing must never execute code from the files being chunked.

5. **Resource Cleanup:** Parser resources must be properly disposed to prevent resource leaks.

---

## Use Cases

### Use Case 1: Bug Investigation in Large Service Class

**Persona:** Liam Chen - Senior Backend Engineer at FinTech startup
**Context:** Investigating null reference exception in PaymentProcessor.cs (850 lines, 12 methods)
**Goal:** Understand payment validation logic without reading entire file

**Before (Line-Based Chunking):**

1. Liam asks agent: "Why is GetPaymentMethod() returning null?"
2. Agent receives context with line-based chunks:
   - Chunk 1: Lines 1-200 (using statements, class header, first 3 methods)
   - Chunk 2: Lines 180-380 (overlap includes end of method 3, all of method 4-6)
   - Chunk 3: Lines 360-560 (overlap includes end of method 6, start of GetPaymentMethod but **method body is split**)
   - Chunk 4: Lines 540-720 (continuation of GetPaymentMethod, but **missing signature**)
3. Agent sees GetPaymentMethod body in Chunk 4 but doesn't see the method signature from Chunk 3
4. Agent responds: "I see the method body but can't determine the full logic without the signature"
5. Liam manually copies GetPaymentMethod (lines 510-625) into new message
6. Agent now has full context, identifies issue: null check missing before dictionary lookup
7. **Total time: 8 minutes** (agent confusion, manual copy-paste, second question)

**After (Structural Chunking):**

1. Liam asks agent: "Why is GetPaymentMethod() returning null?"
2. Agent receives context with structural chunks:
   - Chunk 1: Using statements + namespace + class header (lines 1-15, 80 tokens)
   - Chunk 2: Class fields and constructor (lines 16-45, 420 tokens)
   - Chunk 3: ValidatePayment method **COMPLETE** (lines 46-120, 890 tokens)
   - Chunk 4: ProcessPayment method **COMPLETE** (lines 121-205, 1,150 tokens)
   - Chunk 5: GetPaymentMethod method **COMPLETE** (lines 206-280, 980 tokens) ← **RELEVANT CHUNK**
   - Chunk 6: Remaining helper methods (lines 281-850, 1,200 tokens)
3. Agent sees complete GetPaymentMethod with signature + body + context
4. Agent immediately identifies: "Line 245 performs dictionary lookup without ContainsKey check, causing KeyNotFoundException when payment type is unknown"
5. **Total time: 45 seconds** (single interaction, correct diagnosis)

**Metrics:**
- Time: 8 minutes → 45 seconds (**10.7x faster**)
- Interactions: 2 questions + manual intervention → 1 question (**50% reduction**)
- Context quality: Method split across 2 chunks (broken) → Complete method in 1 chunk (intact)
- LLM accuracy: Incomplete answer → Correct root cause identified
- Developer satisfaction: Frustrated (manual copy-paste) → Delighted (instant answer)

---

### Use Case 2: Feature Implementation Guidance for Junior Developer

**Persona:** Maya Patel - Junior Frontend Developer (6 months experience)
**Context:** Adding new validation to UserRegistrationForm.tsx (450 lines, React component)
**Goal:** Understand existing validation pattern to implement consistent email validation

**Before (Line-Based Chunking):**

1. Maya asks: "How do I add email validation to the registration form?"
2. Agent receives line-based chunks that split React component awkwardly:
   - Chunk 1: Lines 1-100 (imports, **partial** component definition, start of render method)
   - Chunk 2: Lines 80-180 (overlap, middle of JSX, several event handlers **split mid-function**)
   - Chunk 3: Lines 160-260 (more JSX, validation helper **function body missing**)
   - Chunk 4: Lines 240-340 (validation logic continuation, submit handler **signature missing**)
3. Agent sees fragments of validation but can't piece together the full pattern
4. Agent responds: "I see partial validation code but need complete context. Can you share the validateField function?"
5. Maya manually locates validateField (lines 285-320), copies to chat
6. Agent provides guidance based on partial understanding
7. Maya implements validation but uses different pattern than existing code (inconsistent)
8. Code review feedback: "Please match existing validation pattern"
9. Maya refactors to match team conventions
10. **Total time: 2.5 hours** (confusion, manual searching, implementation, refactoring)

**After (Structural Chunking):**

1. Maya asks: "How do I add email validation to the registration form?"
2. Agent receives structural chunks organized by semantic units:
   - Chunk 1: Imports and type definitions (lines 1-25, 180 tokens)
   - Chunk 2: Component state and hooks (lines 26-60, 320 tokens)
   - Chunk 3: validateField helper function **COMPLETE** (lines 61-95, 480 tokens) ← **KEY PATTERN**
   - Chunk 4: Event handlers (onChange, onBlur) **COMPLETE** (lines 96-145, 650 tokens)
   - Chunk 5: onSubmit handler **COMPLETE** (lines 146-200, 720 tokens)
   - Chunk 6: Render method with JSX (lines 201-450, 1,980 tokens)
3. Agent sees complete validateField function showing exact pattern: regex validation → state update → error message
4. Agent responds: "Add email validation following the existing pattern in validateField (lines 61-95). Here's the code following your team's conventions..." and provides implementation matching existing style
5. Maya implements suggested code, works correctly on first try
6. Code review: "Perfect, matches our validation pattern!"
7. **Total time: 20 minutes** (single question, correct implementation, no refactoring needed)

**Metrics:**
- Time: 2.5 hours → 20 minutes (**7.5x faster**)
- Manual code searching: 15 minutes → 0 minutes (**eliminated**)
- Refactoring iterations: 1 full rewrite → 0 (**eliminated**)
- Code consistency: Mismatched pattern → Matches team conventions
- Junior developer confidence: Confused → Confident (clear guidance with working example)

---

### Use Case 3: Architecture Review for Tech Lead

**Persona:** Jordan Kim - Engineering Lead reviewing PR with 15 changed files
**Context:** Junior engineer submitted PR refactoring authentication system across multiple services
**Goal:** Understand architectural changes and verify consistency across services

**Before (Line-Based Chunking):**

1. Jordan asks: "Summarize the authentication changes in this PR across all services"
2. Agent receives 15 files × 8 line-based chunks = 120 chunks total
3. Critical architectural elements split across chunks:
   - AuthService.cs: Interface definition (lines 1-40) in Chunk 1, implementation (lines 120-250) in Chunk 3-4 (split)
   - TokenValidator.ts: Validation logic split across 3 chunks, core algorithm broken mid-function
   - UserController.cs: Endpoints split, can't see full API surface
4. Agent attempts to summarize but has incomplete view of each component
5. Agent response: "The PR modifies authentication in 15 files. I see token validation changes but the full flow is unclear due to fragmented context. Key concern: Line 145 in AuthService appears to have different validation than TokenValidator line 89, but I can't see complete logic."
6. Jordan manually reviews all 15 files in IDE (45 minutes)
7. Jordan finds actual issue: Token expiration handling inconsistent between 3 services (30 min vs 60 min vs 24 hours)
8. Jordan comments on PR: "Please standardize token expiration to 60 minutes across all services"
9. **Total time: 65 minutes** (agent failed to provide useful summary, manual review required)

**After (Structural Chunking):**

1. Jordan asks: "Summarize the authentication changes in this PR across all services"
2. Agent receives 15 files × 3-5 structural chunks = 52 chunks total (fewer, higher quality)
3. Each architectural element is complete:
   - AuthService.cs Chunk 1: IAuthService interface **COMPLETE** (lines 1-40)
   - AuthService.cs Chunk 2: AuthServiceImpl class **COMPLETE** (lines 41-280)
   - TokenValidator.ts Chunk 1: TokenValidator class **COMPLETE** with all validation logic (lines 1-120)
   - UserController.cs Chunk 1: All authentication endpoints **COMPLETE** (lines 15-180)
4. Agent can trace full authentication flow across all services
5. Agent response: "The PR refactors authentication across 4 layers:
   - **Core change:** Migrated from JWT static signing to rotating keys
   - **Services modified:** AuthService (key rotation), TokenValidator (key lookup), UserController (error handling), RefreshTokenService (key caching)
   - **Consistency issue found:** Token expiration varies by service:
     - AuthService: 60 minutes (line 89)
     - RefreshTokenService: 30 minutes (line 134)
     - AdminAuthService: 24 hours (line 267)
   - **Recommendation:** Standardize to 60 minutes across all services or make configurable via appsettings.json"
6. Jordan reviews agent summary, verifies finding in code (3 minutes)
7. Jordan comments on PR with agent's exact finding
8. **Total time: 8 minutes** (agent provided accurate architectural summary with specific issue)

**Metrics:**
- Time: 65 minutes → 8 minutes (**8.1x faster**)
- Manual file review: 15 files × 3 min = 45 min → 0 min (**eliminated**)
- Issues found: 1 (manual) → 1 (automated) + architectural clarity
- Chunks processed: 120 (fragmented) → 52 (semantic) (**57% reduction**)
- Tech lead cognitive load: High (manual cross-referencing) → Low (clear summary)
- Review quality: Surface-level (time pressure) → Deep (architectural understanding)

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| **Chunk** | A contiguous segment of source code extracted from a file as a single unit for context assembly. Chunks are the fundamental units passed to the LLM and must be self-contained enough to be understood independently. Each chunk includes content, line range metadata, token count, and hierarchy information. Ideal chunks respect semantic boundaries (complete methods, classes, or functions) rather than arbitrary line splits. |
| **Structural Chunking** | A parsing-based approach that divides source code along semantic boundaries (classes, methods, functions, modules) rather than arbitrary line counts. Structural chunking requires language-specific parsers (e.g., Roslyn for C#, TypeScript compiler for TS/JS) to understand code structure via AST analysis. This produces higher-quality chunks that preserve complete logical units, improving LLM comprehension by 35% compared to line-based approaches. The trade-off is increased complexity and parse-time overhead (~80-120ms per file). |
| **Line-Based Chunking** | A simple fallback strategy that divides files into fixed line-count segments without regard for code structure. For example, lines 1-50 become Chunk 1, lines 46-95 (with 5-line overlap) become Chunk 2, etc. This approach is fast (<10ms), language-agnostic, and works for any text file (code, config, markdown), but produces lower-quality chunks that may split methods mid-body or separate signatures from implementations. Used as fallback when structural parsing fails or for unsupported file types. |
| **Token-Based Chunking** | An approach that uses token count (not line count or character count) as the primary size constraint for chunks. Since LLMs process input as tokens (not characters), token-based chunking ensures chunks fit within model limits (e.g., 2,000 tokens per chunk for a 100K context window). Requires model-specific tokenizer (tiktoken for GPT, Claude tokenizer) to accurately count tokens. May split mid-line if necessary to respect token budget, though structural chunking prefers semantic boundaries when possible. |
| **Overlap** | The practice of including the last N lines of Chunk i as the first N lines of Chunk i+1 to preserve context at boundaries. For example, with 5-line overlap, Chunk 1 (lines 1-50) and Chunk 2 (lines 46-95) share lines 46-50. Overlap helps LLMs understand transitions between chunks and reduces the impact of arbitrary splits. However, overlap creates duplicate content that must be handled by deduplication (Task 016.c) to avoid wasting context budget. Typical overlap: 5-10 lines or 50-100 tokens. |
| **Boundary** | The edge point where one chunk ends and the next begins. In line-based chunking, boundaries are arbitrary (e.g., line 50, line 100). In structural chunking, boundaries align with semantic points (end of method, end of class). Optimal boundaries maximize chunk self-containment while respecting size constraints. Boundary detection is the core challenge of chunking—poor boundaries split functions mid-body or separate related logic. |
| **Parser** | A language-specific tool that analyzes source code to extract structural information (classes, methods, statements). Parsers convert text into Abstract Syntax Trees (ASTs) that represent code structure. For C#, we use Roslyn (Microsoft.CodeAnalysis); for TypeScript/JavaScript, we use the TypeScript compiler API. Parsers enable structural chunking by identifying semantic boundaries. Parse failures (due to syntax errors, unsupported syntax, or binary files) trigger automatic fallback to line-based chunking. |
| **AST (Abstract Syntax Tree)** | A tree representation of source code structure produced by parsers. Each node represents a code construct (class, method, expression, statement). For example, a C# method becomes a MethodDeclarationSyntax node in Roslyn's AST. Structural chunking traverses the AST to find chunk boundaries—e.g., visiting all MethodDeclarationSyntax nodes to extract complete methods as chunks. ASTs abstract away formatting details (whitespace, comments) and expose semantic structure, enabling intelligent chunking decisions. |
| **Self-Contained** | A quality metric for chunks indicating whether the chunk can be understood in isolation without requiring content from other chunks. A self-contained chunk includes enough context (imports, type definitions, method signature) for an LLM to comprehend its purpose and logic. Structural chunks aim for 95%+ self-containment by keeping methods complete. Line-based chunks have ~60% self-containment due to arbitrary splits. Self-containment directly correlates with LLM accuracy—fragmented chunks confuse the model. |
| **Fallback** | An alternative chunking strategy invoked when the primary strategy fails. For example, if Roslyn fails to parse a C# file due to syntax errors, the system falls back to line-based chunking to ensure content is still included (even if quality is lower). Fallback ensures robustness—no file should cause chunking to fail completely. Fallbacks are logged for debugging but transparent to end users. Fallback usage rate is a quality metric (ideal: <5% of files require fallback). |
| **Max Size** | The upper limit on chunk size, typically defined in tokens (e.g., 2,000 tokens per chunk). Enforced to ensure chunks fit within LLM context windows and ranking/selection budgets. If a semantic unit (e.g., a very long method) exceeds max size, the chunker must split it at logical boundaries (end of if-block, loop) with boundary markers (`// [Chunk 1 of 3]`). Max size violations are hard errors that must be handled—rejecting the file is not acceptable; splitting is required. |
| **Min Size** | The lower limit on chunk size (e.g., 100 tokens per chunk) to avoid creating many tiny, inefficient chunks. Very small chunks (e.g., a single import statement) waste context budget and degrade LLM performance. If a semantic unit is below min size, the chunker may combine it with adjacent units (e.g., combining all imports + namespace into one chunk). Min size is a soft guideline—single-line utility methods may legitimately be <100 tokens. |
| **Metadata** | Structured information attached to each chunk describing its source, location, size, and type. Required metadata fields: source file path, line start, line end, token count, chunk type (structural/line-based), hierarchy (namespace → class → method). Metadata enables ranking (Task 016.b), deduplication (Task 016.c), and formatted output. Metadata must not leak sensitive paths (e.g., absolute paths with usernames); paths should be repository-relative. |
| **Strategy** | The algorithm used to divide a file into chunks. Supported strategies: structural (Roslyn for C#, TS compiler for TypeScript/JavaScript), line-based (universal fallback), token-based (respects token budgets). Strategy selection is automatic based on file extension and content detection. Users can override via configuration (`.agent/config.yml`) to force line-based for all files or adjust chunk_level (class vs. method). Strategy choice impacts chunk quality, parse time, and LLM accuracy. |
| **Hierarchy** | The nested structural path from file root to chunk location, represented as an array of ancestor nodes. For example, a method chunk has hierarchy `["MyApp.Services", "UserService", "GetUserAsync"]` indicating namespace, class, method. Hierarchy enables Context Packer to prioritize chunks (e.g., rank chunks from relevant classes higher). Hierarchy is extracted from AST during structural parsing. Line-based chunks have shallow hierarchy (file name only) since no structure is parsed. |
| **Roslyn** | Microsoft's official .NET compiler platform providing C# and VB.NET parsing, semantic analysis, and code generation APIs. We use Roslyn (NuGet package `Microsoft.CodeAnalysis.CSharp`) to parse C# files into syntax trees for structural chunking. Roslyn handles all C# versions (C# 1.0 through C# 12+), has robust error recovery for malformed code, and is the authoritative C# parser (same parser as Visual Studio). Trade-off: ~15MB dependency and 80ms parse time, but 99.8% accuracy. |
| **TypeScript Compiler API** | The programmatic interface to the TypeScript compiler (tsc) that parses TypeScript and JavaScript into ASTs. Accessed via Node.js or embedded V8 runtime (Microsoft.ClearScript.V8) for .NET interop. Handles all TypeScript syntax (interfaces, type aliases, generics, decorators) and JavaScript (ES5, ES6+, JSX). Used for structural chunking of .ts, .tsx, .js, .jsx files. Parse time: ~120ms for medium files. Alternative considered: Babel, but TS compiler is more authoritative for TypeScript. |
| **Tiktoken** | OpenAI's official tokenizer library for GPT models (GPT-3.5, GPT-4, etc.), implemented in Python and Rust with .NET bindings. Used for exact token counting in chunks to ensure they fit within model limits. Different from character-based estimation (which has 10-15% error rate). Tiktoken handles special tokens, unicode, and model-specific encoding rules. We use tiktoken for GPT models and Claude's tokenizer (via Anthropic SDK) for Claude models. Caching is critical—tokenization is slow (~20-30ms per chunk) but 95% cache hit rate. |
| **Semantic Unit** | A logically cohesive piece of code that represents a complete concept, such as a full method, class, interface, or function. Semantic units are the target boundaries for structural chunking—keeping them intact improves LLM comprehension. Examples: a complete `GetUserById` method (lines 50-75), an entire `IUserRepository` interface (lines 10-30), or a `UserValidator` class (lines 100-200). Splitting a semantic unit (e.g., method signature in Chunk 1, body in Chunk 2) degrades quality by 23%. |
| **Context Budget** | The total number of tokens allocated to the packed context (sum of all selected chunks) before sending to the LLM. Context Packer enforces budget limits (e.g., 77,000 tokens for a 100K model with 8K system prompt and 15K response reserve). Chunks must fit within budget after deduplication. Chunking must produce accurate token estimates to avoid budget overruns. If chunks exceed budget, ranking and selection (Tasks 016.b/c) determine which to include/exclude. Budget violations cause hard failures; prevention requires exact tokenization. |

---

## Out of Scope

The following items are explicitly excluded from Task 016.a:

**1. Semantic Embedding-Based Chunking**
- **Rationale:** Semantic chunking using vector embeddings (e.g., chunk based on conceptual similarity rather than syntactic structure) requires embedding models, similarity computation, and clustering algorithms. This adds significant complexity, latency (~200-500ms per file for embedding generation), and infrastructure dependencies (embedding model hosting). Structural chunking via AST parsing already achieves 88% LLM accuracy, making the marginal benefit of semantic chunking (~3-5% improvement) not worth the 3x latency increase. Deferred to v2 roadmap after baseline system proves value.

**2. Machine Learning-Based Boundary Detection**
- **Rationale:** Using ML models to predict optimal chunk boundaries (e.g., train a model on "good vs. bad chunks" labeled data) would require training data collection, model training/deployment, and inference pipeline. This is engineering overkill for v1 when rule-based AST traversal works well. ML approaches also lack explainability—users can't understand why chunks were created. Rule-based chunking is deterministic, debuggable, and sufficient for 95%+ of use cases. ML-based chunking is a research topic, not a production requirement.

**3. Cross-File Chunks (Multi-File Semantic Units)**
- **Rationale:** Some logical units span multiple files (e.g., interface in FileA.cs + implementation in FileB.cs). Creating chunks that span files would require cross-file dependency analysis, file ordering, and complex metadata. This violates the clean separation of concerns (one chunk = one file segment). If users need cross-file context, the Context Packer (Task 016) selects chunks from multiple files—chunking doesn't need to handle this. Cross-file chunks also complicate deduplication and ranking. Explicit exclusion to keep chunking scope manageable.

**4. Streaming/Progressive Chunking for Very Large Files**
- **Rationale:** Files larger than memory (~10MB+) could theoretically be chunked via streaming (read file in 1MB chunks, parse progressively, emit chunks incrementally). This adds significant complexity to the chunker (stateful parsing, partial AST construction) and is rarely needed (99.5% of source files are <1MB). For very large files (e.g., generated SQL dumps, minified JS bundles), the chunker can simply skip them or fall back to line-based chunking with large chunk sizes. Streaming is a premature optimization—implement only if user reports indicate large files are a real problem.

**5. Dynamic Chunk Sizing Based on Complexity**
- **Rationale:** Adjusting chunk size dynamically based on code complexity (e.g., larger chunks for simple code, smaller for complex code) would require complexity metrics (cyclomatic complexity, nesting depth) and adaptive sizing algorithms. This makes chunking behavior non-deterministic and hard to predict. Users expect consistent chunk sizes (±20% variance). Dynamic sizing also complicates token budget planning—Context Packer can't predict chunk sizes before chunking. Fixed sizing rules (max_tokens, min_tokens, chunk_level) are simpler, predictable, and configurable. Complexity-aware chunking is interesting research but not a v1 requirement.

**6. Language Support Beyond C#, TypeScript, JavaScript**
- **Rationale:** Supporting additional languages (Python, Java, Go, Rust, etc.) requires language-specific parsers, AST traversal logic, and test coverage. Each language adds ~2 weeks development + ongoing maintenance. For v1, we focus on C# (primary Acode language) and TypeScript/JavaScript (web frontend). Python support is high priority for v2 (many ML codebases), but v1 uses line-based fallback for Python files. Adding languages incrementally reduces risk and allows validation of chunking architecture before expansion.

**7. Syntax Highlighting or Code Formatting in Chunks**
- **Rationale:** Adding syntax highlighting (ANSI color codes, HTML) or reformatting code (via Prettier, Roslyn formatter) would make chunks prettier but doesn't improve LLM comprehension (LLMs ignore formatting). It would add processing time (10-20ms per chunk for formatting), increase chunk size (ANSI codes add bytes), and complicate testing (compare formatted vs. unformatted). Chunks are for LLM consumption, not human display. If users want pretty-printed code, that's a UI concern (Task 031 - CLI Output), not a chunking concern.

**8. Incremental Chunking (Cache Previous Chunks)**
- **Rationale:** Caching chunks from previous runs and only re-chunking changed files would reduce latency for repeated queries. However, this requires persistent chunk storage, cache invalidation logic (detect file changes via hashes/timestamps), and cache size management. For v1, chunking is fast enough (<200ms per file) that caching isn't critical. Most agent interactions involve different files (search results vary by query). Incremental chunking is a performance optimization for v2, not a v1 requirement. Focus v1 on correctness, add caching only if profiling shows it's a bottleneck.

**9. Chunk Compression or Binary Encoding**
- **Rationale:** Compressing chunks (gzip, brotli) before storage/transmission would reduce memory usage and network bandwidth but adds CPU overhead (compression/decompression) and complexity (handle compressed metadata, debug compressed content). Since chunks are ephemeral (created on-demand, passed to Context Packer, then discarded), there's no persistent storage to optimize. LLM APIs expect plaintext input, not compressed chunks. Compression is a micro-optimization (<5% memory savings) that distracts from core functionality. Explicit exclusion to prevent premature optimization.

**10. Language Detection via Content Analysis (Beyond Extensions)**
- **Rationale:** Detecting language from file content (e.g., use heuristics to identify a .txt file as actually being C# code) would handle edge cases like extensionless scripts or misnamed files. However, this adds unreliable heuristics (shebang parsing, regex matching) and increases false positives (markdown mistaken for code). File extensions are 99.9% accurate in real repositories. For edge cases, users can configure explicit language mappings in `.agent/config.yml`. Content-based detection is complexity without proportional benefit.

**11. Chunk Validation or Quality Scoring**
- **Rationale:** Assigning quality scores to chunks (e.g., "this chunk is 87% self-contained based on reference analysis") would help identify poor chunks but requires reference resolution, dependency analysis, and ML-based scoring models. This duplicates work done by ranking (Task 016.b) which already scores chunks by relevance. Chunk validation could detect broken chunks (e.g., unmatched braces) but Roslyn/TS compiler already validate syntax during parsing. Quality scoring is interesting research but not actionable—what would the chunker do with a low-quality chunk? Split it differently? That's already handled by fallback. Explicit exclusion.

**12. Multi-Language Files (e.g., HTML with Embedded JS)**
- **Rationale:** Files mixing languages (HTML with `<script>` tags, Markdown with code fences, Razor .cshtml with C#) require multi-phase parsing: extract language regions, parse each with appropriate parser, reassemble. This is complex and error-prone. For v1, treat multi-language files as single-language (parse HTML as HTML, .cshtml as C#, .md as plaintext). If users need granular chunking of embedded code, they should extract it to separate files (best practice anyway). Multi-language chunking is a nice-to-have for v2, not blocking for v1.

**13. Chunk Localization or Internationalization**
- **Rationale:** Supporting non-English identifiers, comments, or error messages in chunks (e.g., Chinese variable names, Japanese comments) is already handled—chunkers work on UTF-8 text and preserve all characters. However, localizing chunk *metadata* (e.g., chunk type labels in French) or chunker error messages is out of scope. Acode targets English-speaking developers for v1. Internationalization is a product-level decision (Epic 13 - Localization, if created), not a chunking feature. UTF-8 support is in scope; translated UI is out of scope.

**14. Chunk Merging or Post-Processing**
- **Rationale:** After initial chunking, merging adjacent small chunks or splitting large chunks based on feedback loops (e.g., LLM reports chunk was confusing) would require iterative refinement and LLM interaction during chunking. This makes chunking non-deterministic and slow (requires LLM API calls). Chunking must be fast (<200ms), offline, and deterministic. Adjustments based on LLM feedback belong in future tasks (adaptive context tuning, Task 034 - Feedback Loops). Chunk merging for small chunks is handled by min_tokens enforcement during initial chunking. Explicit exclusion to keep chunking deterministic.

**15. User-Defined Custom Chunking Rules**
- **Rationale:** Allowing users to define custom chunking rules via regex, AST queries, or scripting (e.g., "chunk all methods with [HttpPost] attribute separately") would provide ultimate flexibility but adds a complex rule engine, security risks (arbitrary code execution), and debugging nightmares (users create bad rules, chunking breaks). Configuration knobs (max_tokens, chunk_level, overlap_lines) are sufficient for 95% of customization needs. Custom rules are power-user features that can be added in v3 after validating demand. For v1, opinionated defaults with limited configuration prevent complexity explosion.

---

## Functional Requirements

### Structural Chunking (FR-016a-01 to FR-016a-05)

| ID | Requirement |
|----|-------------|
| FR-016a-01 | System MUST detect class boundaries in source files |
| FR-016a-02 | System MUST detect method boundaries in source files |
| FR-016a-03 | System MUST detect function boundaries in source files |
| FR-016a-04 | System MUST detect block boundaries (if, for, while) |
| FR-016a-05 | System MUST preserve structural integrity when chunking |

### C# Chunking (FR-016a-06 to FR-016a-10)

| ID | Requirement |
|----|-------------|
| FR-016a-06 | System MUST parse C# files using Roslyn |
| FR-016a-07 | System MUST chunk C# files by namespace when configured |
| FR-016a-08 | System MUST chunk C# files by class when configured |
| FR-016a-09 | System MUST chunk C# files by method when configured |
| FR-016a-10 | System MUST handle nested types in C# files |

### TypeScript/JavaScript Chunking (FR-016a-11 to FR-016a-15)

| ID | Requirement |
|----|-------------|
| FR-016a-11 | System MUST parse TypeScript files using TypeScript compiler API |
| FR-016a-12 | System MUST chunk TypeScript files by module |
| FR-016a-13 | System MUST chunk TypeScript/JavaScript files by class |
| FR-016a-14 | System MUST chunk TypeScript/JavaScript files by function |
| FR-016a-15 | System MUST handle export statements in chunking |

### Line-Based Chunking (FR-016a-16 to FR-016a-20)

| ID | Requirement |
|----|-------------|
| FR-016a-16 | System MUST provide line-based chunking as fallback |
| FR-016a-17 | Line count per chunk MUST be configurable |
| FR-016a-18 | System MUST support overlap between adjacent chunks |
| FR-016a-19 | Overlap line count MUST be configurable |
| FR-016a-20 | System MUST respect line boundaries (no mid-line splits) |

### Token-Based Chunking (FR-016a-21 to FR-016a-24)

| ID | Requirement |
|----|-------------|
| FR-016a-21 | System MUST estimate token count for each chunk |
| FR-016a-22 | System MUST enforce maximum token limit per chunk |
| FR-016a-23 | System MUST enforce minimum token limit per chunk |
| FR-016a-24 | System MUST balance chunk sizes within configured limits |

### Strategy Selection (FR-016a-25 to FR-016a-28)

| ID | Requirement |
|----|-------------|
| FR-016a-25 | System MUST detect file type from extension and content |
| FR-016a-26 | System MUST select appropriate chunking strategy for file type |
| FR-016a-27 | System MUST fall back to line-based when parsing fails |
| FR-016a-28 | Chunking strategy MUST be configurable per file type |

### Chunk Metadata (FR-016a-29 to FR-016a-33)

| ID | Requirement |
|----|-------------|
| FR-016a-29 | Each chunk MUST include source file path |
| FR-016a-30 | Each chunk MUST include start and end line numbers |
| FR-016a-31 | Each chunk MUST include token count estimate |
| FR-016a-32 | Each chunk MUST include chunk type (structural, line-based) |
| FR-016a-33 | Each chunk MUST include hierarchy path (namespace, class, method) |

### Overlap Handling (FR-016a-34 to FR-016a-36)

| ID | Requirement |
|----|-------------|
| FR-016a-34 | Overlap line count MUST be configurable |
| FR-016a-35 | Overlap MUST preserve context at chunk boundaries |
| FR-016a-36 | Overlap MUST be compatible with deduplication (Task 016.c) |

### Large File Handling (FR-016a-37 to FR-016a-39)

| ID | Requirement |
|----|-------------|
| FR-016a-37 | System MUST handle files larger than memory limits |
| FR-016a-38 | System MUST use progressive/streaming chunking for large files |
| FR-016a-39 | System MUST minimize memory usage during chunking |

### Configuration Management (FR-016a-40 to FR-016a-44)

| ID | Requirement |
|----|-------------|
| FR-016a-40 | System MUST load chunking configuration from `.agent/config.yml` |
| FR-016a-41 | System MUST support per-language configuration overrides |
| FR-016a-42 | System MUST validate configuration values at startup |
| FR-016a-43 | Configuration MUST specify default values for all settings |
| FR-016a-44 | System MUST log configuration errors with actionable messages |

### Error Handling (FR-016a-45 to FR-016a-49)

| ID | Requirement |
|----|-------------|
| FR-016a-45 | System MUST log parse errors with file path and error details |
| FR-016a-46 | System MUST continue processing after single-file parse failure |
| FR-016a-47 | System MUST provide diagnostic information for chunking failures |
| FR-016a-48 | System MUST handle encoding errors gracefully (UTF-8, UTF-16, etc.) |
| FR-016a-49 | System MUST report token count estimation failures clearly |

### Performance Optimization (FR-016a-50 to FR-016a-52)

| ID | Requirement |
|----|-------------|
| FR-016a-50 | System MUST cache parse results for unchanged files (SHA-256 hash) |
| FR-016a-51 | System MUST lazily load parsers to reduce startup time |
| FR-016a-52 | System MUST support parallel chunking of multiple files |

---

## Non-Functional Requirements

### Performance (NFR-016a-01 to NFR-016a-03)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016a-01 | Performance | System MUST chunk 10KB file in less than 50ms |
| NFR-016a-02 | Performance | System MUST chunk 100KB file in less than 200ms |
| NFR-016a-03 | Performance | Memory usage MUST NOT exceed 2x file size during chunking |

### Quality (NFR-016a-04 to NFR-016a-06)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016a-04 | Quality | Chunks MUST be semantically meaningful for LLM consumption |
| NFR-016a-05 | Quality | Chunk sizes MUST be consistent within configured tolerances |
| NFR-016a-06 | Quality | Chunk boundaries MUST align with logical code boundaries |

### Reliability (NFR-016a-07 to NFR-016a-09)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016a-07 | Reliability | System MUST handle malformed or syntactically invalid files |
| NFR-016a-08 | Reliability | System MUST gracefully fall back to line-based on parse errors |
| NFR-016a-09 | Reliability | No file content MUST be lost during chunking operations |

### Maintainability (NFR-016a-10 to NFR-016a-12)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016a-10 | Maintainability | Code MUST be organized by language (CSharpChunker, TypeScriptChunker, LineBasedChunker) |
| NFR-016a-11 | Maintainability | Adding new language support MUST NOT require changes to core chunking logic |
| NFR-016a-12 | Maintainability | Configuration schema MUST be backward-compatible across versions |

### Usability (NFR-016a-13 to NFR-016a-15)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016a-13 | Usability | Error messages MUST include file path, line number, and suggested fix |
| NFR-016a-14 | Usability | Chunking MUST complete within 500ms for typical files (<100KB) |
| NFR-016a-15 | Usability | Fallback to line-based MUST be transparent to users (logged but not blocking) |

### Security (NFR-016a-16 to NFR-016a-18)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-016a-16 | Security | Parsers MUST NOT execute code from files being chunked |
| NFR-016a-17 | Security | Memory allocation MUST be bounded to prevent DoS via large files |
| NFR-016a-18 | Security | File paths in metadata MUST be sanitized (repository-relative, no absolute paths) |

---

## User Manual Documentation

### Overview

Chunking rules determine how files are broken into pieces. The right strategy depends on file type and content.

### Configuration

```yaml
# .agent/config.yml
context:
  chunking:
    # Maximum tokens per chunk
    max_tokens: 2000
    
    # Minimum tokens per chunk
    min_tokens: 100
    
    # Prefer structural chunking
    prefer_structural: true
    
    # Line-based fallback settings
    line_based:
      lines_per_chunk: 50
      overlap_lines: 5
      
    # Language-specific settings
    languages:
      csharp:
        chunk_level: method  # class, method, or block
      typescript:
        chunk_level: function
```

### Chunking Strategies

#### Structural Chunking

Best for supported languages. Respects code structure:

```
File: UserService.cs
├── Chunk 1: Using statements + namespace declaration
├── Chunk 2: Class UserService (header + fields)
├── Chunk 3: Constructor
├── Chunk 4: GetUserAsync method
├── Chunk 5: CreateUserAsync method
└── Chunk 6: Remaining methods
```

#### Line-Based Chunking

Fallback for unsupported files:

```
File: config.yaml
├── Chunk 1: Lines 1-50
├── Chunk 2: Lines 46-95 (5 line overlap)
├── Chunk 3: Lines 91-140
└── Chunk 4: Lines 136-180
```

### Chunk Metadata

Each chunk includes:

```json
{
  "source_file": "src/Services/UserService.cs",
  "line_start": 25,
  "line_end": 50,
  "token_estimate": 450,
  "chunk_type": "method",
  "hierarchy": ["namespace:MyApp", "class:UserService", "method:GetUserAsync"]
}
```

### Troubleshooting

#### Chunks Too Large

**Problem:** Chunks exceed token limit

**Solutions:**
1. Reduce max_tokens setting
2. Change chunk_level to smaller units
3. Check for very long methods

#### Chunks Too Small

**Problem:** Many tiny chunks

**Solutions:**
1. Increase min_tokens
2. Change chunk_level to larger units
3. Combine small files

#### Parse Errors

**Problem:** Structural chunking fails

**Solutions:**
1. Falls back to line-based automatically
2. Check file for syntax errors
3. Report issue if valid file fails

---

## Assumptions

The chunking subsystem relies on the following technical, operational, and integration assumptions:

**Technical Assumptions:**

1. **Source files use UTF-8 encoding:** 95%+ of modern codebases use UTF-8. The chunker handles UTF-8, UTF-16 (with BOM detection), and ASCII. Non-UTF-8 encodings (EBCDIC, ISO-8859-1) may result in garbled text but won't crash the chunker.

2. **C# files are syntactically parseable by Roslyn:** Files with severe syntax errors (e.g., unmatched braces, truncated mid-statement) may fail to parse. Roslyn has robust error recovery (handles most partial code), but completely malformed files trigger fallback to line-based chunking.

3. **TypeScript/JavaScript files conform to ECMAScript standards:** The TypeScript compiler handles ES5, ES6+, JSX, and TypeScript syntax. Non-standard syntax (e.g., Facebook's experimental JS extensions) may fail to parse. Fallback to line-based chunking ensures content is still included.

4. **Files fit in memory:** Most source files are <1MB. The chunker loads entire file content into memory for parsing. Files >10MB may cause memory pressure or OOM exceptions. Large file handling (FR-016a-37) provides mitigation via progressive chunking.

5. **Tokenizers are available for target LLM:** The system uses tiktoken for GPT models and Claude tokenizer (via Anthropic SDK) for Claude models. If target model lacks a tokenizer, the chunker falls back to character-count estimation (chars ÷ 4) with 15% safety margin.

6. **AST traversal is fast enough:** Roslyn and TypeScript compiler parse medium files in 80-120ms. Very large files (>5,000 LOC) or deeply nested code may exceed 200ms parse time. Performance budgets assume typical file sizes (<500 LOC average).

7. **Chunk configuration is sensible:** Users configure max_tokens (e.g., 2,000), min_tokens (e.g., 100), overlap_lines (e.g., 5). Nonsensical values (max_tokens < min_tokens, overlap > chunk size) are detected and rejected at startup with clear error messages.

8. **Line endings are normalized:** Files may use LF (Unix), CRLF (Windows), or CR (old Mac). The chunker normalizes all line endings to LF during parsing to ensure consistent line counting and chunking behavior across platforms.

9. **File paths are repository-relative:** Chunk metadata includes file paths. These are repository-relative (e.g., `src/Services/UserService.cs`) not absolute (e.g., `/home/user/repo/src/Services/UserService.cs`) to avoid leaking system details and enable portability.

10. **Binary files are detectable:** The chunker detects binary files via content sniffing (presence of null bytes, high ratio of non-printable characters). Binary files (images, compiled assemblies, archives) are skipped with a warning log entry.

**Operational Assumptions:**

11. **Chunking happens on single machine:** The chunker processes files locally, not in a distributed system. Parallel chunking (FR-016a-52) uses local thread pool, not remote workers. Distributed chunking (e.g., chunk 10,000 files across 100 cloud VMs) is out of scope for v1.

12. **Configuration is provided at startup:** Chunking configuration is loaded once from `.agent/config.yml` at application startup. Dynamic reconfiguration (hot-reload) is not supported—changes require restart.

13. **Chunking is deterministic:** For the same file content and configuration, the chunker produces identical chunks on every run. This enables caching (FR-016a-50) via SHA-256 file hashes. Non-determinism (e.g., random chunk boundaries) would break caching.

14. **Logs are available for debugging:** When chunking fails or falls back to line-based, diagnostic logs (file path, error message, fallback trigger) are written. Assumes logging infrastructure (Task 009 - Logging) is configured and accessible.

15. **Performance is measured in milliseconds:** Chunking performance targets (NFR-016a-01, NFR-016a-02) assume SSD storage. HDD-based systems or network-mounted filesystems may experience 2-5x slower read times, but this doesn't invalidate correctness—only latency SLAs.

**Integration Assumptions:**

16. **Context Packer invokes chunker correctly:** The Context Packer (Task 016) passes valid file content and ChunkOptions. Invalid input (null content, negative token limits) causes ArgumentException. Garbage-in-garbage-out principle applies.

17. **RepoFS provides accurate file content:** The chunker receives file content from RepoFS (Task 014). Assumes RepoFS returns up-to-date, unmodified file content. If RepoFS caches stale content, chunks reflect stale code.

18. **Ranking accepts chunk metadata:** Ranking (Task 016.b) uses chunk metadata (hierarchy, token count, line range) for scoring. Assumes ranking system can handle variable-length hierarchy arrays and missing fields gracefully.

19. **Deduplication handles overlapping chunks:** Deduplication (Task 016.c) receives chunks with overlap and removes duplicates. Assumes deduplication logic understands overlap is intentional (not an error) and handles it via range-based comparison.

20. **Index storage can persist chunks (optional):** If caching is enabled (FR-016a-50), chunk data is stored in Index (Task 015). Assumes index supports chunked data types and provides query APIs for cache lookups. If index is unavailable, chunking proceeds without caching (slower but functional).

---

## Security Considerations

The chunking subsystem faces several security threats that must be mitigated through defensive code:

### Threat 1: Parser Exploit via Malicious Code

**Risk Description:**
Adversary crafts a malicious source file designed to exploit vulnerabilities in Roslyn or TypeScript compiler parsers. Examples: deeply nested expressions causing stack overflow, pathological regex in parser (ReDoS), or triggering parser bugs that execute arbitrary code.

**Attack Scenario:**
1. User asks agent: "Review this pull request"
2. PR contains `exploit.cs` with 10,000 levels of nested lambdas: `x => x => x => ... => x`
3. Chunker invokes Roslyn parser on `exploit.cs`
4. Roslyn recursively descends AST, exhausts stack (StackOverflowException)
5. Application crashes, agent unavailable

**Mitigation Code:**

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Diagnostics;
using System.Linq;

namespace Acode.Infrastructure.Context.Chunking
{
    public sealed class CSharpChunker : IChunker
    {
        private readonly int _maxParseTimeMs;
        private readonly int _maxNestingDepth;
        private readonly ILogger<CSharpChunker> _logger;

        public CSharpChunker(
            ILogger<CSharpChunker> logger,
            int maxParseTimeMs = 5000,
            int maxNestingDepth = 500)
        {
            _logger = logger;
            _maxParseTimeMs = maxParseTimeMs;
            _maxNestingDepth = maxNestingDepth;
        }

        public IReadOnlyList<ContentChunk> Chunk(string content, ChunkOptions options)
        {
            // MITIGATION 1: Timeout for parsing
            using var cts = new CancellationTokenSource(_maxParseTimeMs);

            SyntaxTree tree;
            try
            {
                // Parse with cancellation token
                var parseOptions = CSharpParseOptions.Default
                    .WithDocumentationMode(DocumentationMode.None); // Skip XML docs parsing

                tree = CSharpSyntaxTree.ParseText(
                    content,
                    parseOptions,
                    cancellationToken: cts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "C# parsing timed out after {Timeout}ms. Falling back to line-based chunking.",
                    _maxParseTimeMs);

                return FallbackToLineBasedChunking(content, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "C# parsing failed. Falling back to line-based chunking.");
                return FallbackToLineBasedChunking(content, options);
            }

            // MITIGATION 2: Validate nesting depth before processing
            var root = tree.GetRoot(cts.Token);
            int maxDepth = CalculateMaxDepth(root);

            if (maxDepth > _maxNestingDepth)
            {
                _logger.LogWarning(
                    "C# file has excessive nesting depth ({Depth} > {Max}). Falling back to line-based chunking.",
                    maxDepth,
                    _maxNestingDepth);

                return FallbackToLineBasedChunking(content, options);
            }

            // MITIGATION 3: Process in try-catch to handle unexpected parser bugs
            try
            {
                return ExtractStructuralChunks(root, content, options, cts.Token);
            }
            catch (StackOverflowException)
            {
                _logger.LogError("Stack overflow during C# chunk extraction. File likely has pathological nesting.");
                // Cannot catch StackOverflowException reliably, but log for diagnostics
                throw; // Re-throw, will crash process
            }
            catch (OutOfMemoryException)
            {
                _logger.LogError("Out of memory during C# chunk extraction. File likely too large or has memory leak.");
                throw; // Re-throw, critical error
            }
        }

        private static int CalculateMaxDepth(SyntaxNode node, int currentDepth = 0)
        {
            if (node == null) return currentDepth;

            int maxChildDepth = currentDepth;
            foreach (var child in node.ChildNodes())
            {
                int childDepth = CalculateMaxDepth(child, currentDepth + 1);
                if (childDepth > maxChildDepth)
                    maxChildDepth = childDepth;
            }

            return maxChildDepth;
        }

        private IReadOnlyList<ContentChunk> FallbackToLineBasedChunking(
            string content,
            ChunkOptions options)
        {
            var lineBasedChunker = new LineBasedChunker();
            return lineBasedChunker.Chunk(content, options);
        }
    }
}
```

**Impact:** Prevents DoS via parser exploits. Parse timeout (5 seconds) and nesting depth check (500 levels) ensure pathological files don't crash the system. Fallback to line-based chunking maintains functionality even when structural parsing fails.

---

### Threat 2: Resource Exhaustion via Very Large Files

**Risk Description:**
Adversary includes extremely large files (e.g., 50MB minified JS, 100MB SQL dump) in repository. Chunker attempts to load entire file into memory, exhausts available RAM, causes OOM exception and application crash.

**Attack Scenario:**
1. Repository contains `bundle.min.js` (50MB, single line of minified JS)
2. User runs: `acode analyze security vulnerabilities`
3. Context Packer selects `bundle.min.js` as relevant (keyword match: "security")
4. Chunker loads 50MB into memory for TypeScript parsing
5. Parsing allocates additional 100MB for AST (2x overhead)
6. Application OOM, crashes

**Mitigation Code:**

```csharp
using System;
using System.IO;

namespace Acode.Infrastructure.Context.Chunking
{
    public sealed class ChunkerFactory : IChunkerFactory
    {
        private readonly int _maxFileSizeBytes;
        private readonly ILogger<ChunkerFactory> _logger;

        public ChunkerFactory(
            ILogger<ChunkerFactory> logger,
            int maxFileSizeBytes = 5_000_000) // 5MB default
        {
            _logger = logger;
            _maxFileSizeBytes = maxFileSizeBytes;
        }

        public IChunker CreateChunker(string filePath, string content)
        {
            // MITIGATION 1: Reject files exceeding size limit
            int contentBytes = System.Text.Encoding.UTF8.GetByteCount(content);

            if (contentBytes > _maxFileSizeBytes)
            {
                _logger.LogWarning(
                    "File {FilePath} is {Size}MB, exceeding limit of {Limit}MB. Skipping structural parsing, using line-based chunking with size limits.",
                    filePath,
                    contentBytes / 1_000_000.0,
                    _maxFileSizeBytes / 1_000_000.0);

                return new LineBasedChunker(maxChunkSizeBytes: 100_000); // 100KB chunks
            }

            // MITIGATION 2: Detect minified code (single-line files)
            int lineCount = content.Split('\n').Length;
            double avgLineLength = contentBytes / (double)Math.Max(lineCount, 1);

            if (avgLineLength > 1000) // Minified if avg line >1KB
            {
                _logger.LogWarning(
                    "File {FilePath} appears minified (avg line length: {AvgLength} chars). Using line-based chunking.",
                    filePath,
                    (int)avgLineLength);

                return new LineBasedChunker();
            }

            // Select appropriate chunker by extension
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".cs" => new CSharpChunker(_logger),
                ".ts" or ".tsx" or ".js" or ".jsx" => new TypeScriptChunker(_logger),
                _ => new LineBasedChunker()
            };
        }
    }
}
```

**Impact:** Files exceeding 5MB or minified code (avg line >1KB) bypass structural parsing, preventing memory exhaustion. Line-based chunking with 100KB chunk size ensures even very large files are handled safely.

---

### Threat 3: Path Traversal via Malicious Metadata

**Risk Description:**
Adversary manipulates file paths to include path traversal sequences (`../`, absolute paths) attempting to leak information about system structure or read files outside repository boundaries.

**Attack Scenario:**
1. Malicious PR includes file named: `../../../etc/passwd` (valid in Git)
2. Chunker processes file, stores path in chunk metadata
3. Chunk metadata later displayed in UI or logs: `Processing chunk from ../../../etc/passwd`
4. Information leakage reveals system paths

**Mitigation Code:**

```csharp
using System;
using System.IO;

namespace Acode.Infrastructure.Context.Chunking
{
    public sealed class ChunkMetadataValidator
    {
        private readonly string _repositoryRoot;

        public ChunkMetadataValidator(string repositoryRoot)
        {
            _repositoryRoot = Path.GetFullPath(repositoryRoot);
        }

        public string SanitizeFilePath(string filePath)
        {
            // MITIGATION 1: Normalize to absolute path
            string fullPath = Path.GetFullPath(filePath);

            // MITIGATION 2: Ensure path is within repository root
            if (!fullPath.StartsWith(_repositoryRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityException(
                    $"Path traversal detected: {filePath} resolves outside repository root {_repositoryRoot}");
            }

            // MITIGATION 3: Convert to repository-relative path
            string relativePath = Path.GetRelativePath(_repositoryRoot, fullPath);

            // MITIGATION 4: Normalize path separators to forward slash (Unix-style)
            relativePath = relativePath.Replace('\\', '/');

            return relativePath;
        }

        public ContentChunk CreateChunk(
            string filePath,
            string content,
            int lineStart,
            int lineEnd,
            int tokenEstimate,
            ChunkType type,
            IReadOnlyList<string> hierarchy)
        {
            // Sanitize file path before creating chunk
            string sanitizedPath = SanitizeFilePath(filePath);

            return new ContentChunk(
                Content: content,
                FilePath: sanitizedPath, // SAFE: repository-relative, no traversal
                LineStart: lineStart,
                LineEnd: lineEnd,
                TokenEstimate: tokenEstimate,
                Type: type,
                Hierarchy: hierarchy);
        }
    }
}
```

**Impact:** All file paths are normalized to repository-relative format (e.g., `src/Services/UserService.cs`) and validated to prevent traversal. Absolute paths or paths outside repository trigger SecurityException.

---

### Threat 4: Denial of Service via Pathological Regular Expressions (ReDoS)

**Risk Description:**
If chunker uses regex for parsing (e.g., line-based chunker with pattern matching), adversary crafts input that triggers catastrophic backtracking in regex engine, causing 100% CPU usage and application hang.

**Attack Scenario:**
1. Line-based chunker uses regex to detect code blocks: `^\\s*\\{.*\\}\\s*$`
2. Adversary creates file with 10,000 character line: `{aaaaaaa...aaaaaaa` (no closing brace)
3. Regex engine backtracks exponentially, takes 30+ seconds per line
4. Chunking hangs, agent unavailable

**Mitigation Code:**

```csharp
using System;
using System.Text.RegularExpressions;

namespace Acode.Infrastructure.Context.Chunking
{
    public sealed class LineBasedChunker : IChunker
    {
        // MITIGATION 1: Use compiled regex with timeout
        private static readonly Regex CodeBlockPattern = new Regex(
            @"^\s*\{.*\}\s*$",
            RegexOptions.Compiled | RegexOptions.Singleline,
            matchTimeout: TimeSpan.FromMilliseconds(100)); // 100ms timeout per match

        public IReadOnlyList<ContentChunk> Chunk(string content, ChunkOptions options)
        {
            var chunks = new List<ContentChunk>();
            var lines = content.Split('\n');

            int linesPerChunk = options.LinesPerChunk ?? 50;
            int overlapLines = options.OverlapLines ?? 5;

            for (int i = 0; i < lines.Length; i += (linesPerChunk - overlapLines))
            {
                int start = i;
                int end = Math.Min(i + linesPerChunk, lines.Length);

                string chunkContent = string.Join("\n", lines[start..end]);

                // MITIGATION 2: Avoid regex on untrusted input if possible
                // Instead of regex, use simple string checks
                bool looksLikeCodeBlock = chunkContent.TrimStart().StartsWith('{')
                                       && chunkContent.TrimEnd().EndsWith('}');

                // If regex is necessary, use timeout and catch RegexMatchTimeoutException
                try
                {
                    var match = CodeBlockPattern.Match(chunkContent);
                    if (match.Success)
                    {
                        // Process code block...
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    // Regex timed out - skip pattern matching, treat as plaintext
                    _logger.LogWarning(
                        "Regex match timed out on chunk at lines {Start}-{End}. Skipping pattern detection.",
                        start,
                        end);
                }

                chunks.Add(new ContentChunk(
                    Content: chunkContent,
                    FilePath: "unknown", // Set by caller
                    LineStart: start + 1,
                    LineEnd: end,
                    TokenEstimate: EstimateTokens(chunkContent),
                    Type: ChunkType.LineBased,
                    Hierarchy: Array.Empty<string>()));
            }

            return chunks;
        }
    }
}
```

**Impact:** Regex timeouts (100ms per match) prevent catastrophic backtracking. Simple string checks replace regex where possible. Even with malicious input, chunking completes in bounded time.

---

### Threat 5: Information Leakage via Chunk Caching

**Risk Description:**
If chunks are cached (FR-016a-50) to improve performance, cache keys or cached data might leak information about file contents to unauthorized users (e.g., in multi-tenant environments or shared caching infrastructure).

**Attack Scenario:**
1. User A chunks sensitive file: `InternalSecrets.cs`
2. Chunk is cached with key: `SHA256(content) = abc123...`
3. User B (in different project) chunks file with same content hash
4. Cache returns User A's chunk to User B
5. User B gains access to User A's sensitive code

**Mitigation Code:**

```csharp
using System;
using System.Security.Cryptography;
using System.Text;

namespace Acode.Infrastructure.Context.Chunking
{
    public sealed class ChunkCache : IChunkCache
    {
        private readonly Dictionary<string, List<ContentChunk>> _cache;
        private readonly string _repositoryId;

        public ChunkCache(string repositoryId)
        {
            _cache = new Dictionary<string, List<ContentChunk>>();
            _repositoryId = repositoryId;
        }

        public bool TryGetChunks(
            string filePath,
            string content,
            out List<ContentChunk> chunks)
        {
            // MITIGATION 1: Include repository ID in cache key
            // Prevents cross-repository cache hits
            string cacheKey = ComputeCacheKey(filePath, content, _repositoryId);

            return _cache.TryGetValue(cacheKey, out chunks);
        }

        public void StoreChunks(
            string filePath,
            string content,
            List<ContentChunk> chunks)
        {
            string cacheKey = ComputeCacheKey(filePath, content, _repositoryId);

            // MITIGATION 2: Clone chunks before storing to prevent reference leakage
            var clonedChunks = chunks.Select(c => c with { }).ToList();

            _cache[cacheKey] = clonedChunks;
        }

        private string ComputeCacheKey(string filePath, string content, string repositoryId)
        {
            // MITIGATION 3: Hash combines repository ID + file path + content
            // Ensures cache keys are scoped to specific repository
            using var sha256 = SHA256.Create();

            string keyInput = $"{repositoryId}|{filePath}|{content}";
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyInput));

            return Convert.ToHexString(hashBytes);
        }

        public void Clear()
        {
            // MITIGATION 4: Provide cache invalidation
            _cache.Clear();
        }
    }
}
```

**Impact:** Cache keys include repository ID, preventing cross-repository cache hits. Even if two repositories have files with identical content, they receive separate cache entries. Cache is scoped to single repository instance.

---

## Acceptance Criteria

### Structural Chunking (AC-001 to AC-010)

- [ ] AC-001: C# class definitions are extracted as complete chunks (class keyword through closing brace)
- [ ] AC-002: C# method boundaries are respected (signature + body + attributes + doc comments)
- [ ] AC-003: C# namespace declarations are included in first chunk of file
- [ ] AC-004: C# using statements are included in first chunk (not split across multiple chunks)
- [ ] AC-005: TypeScript function declarations are extracted as complete chunks
- [ ] AC-006: TypeScript class definitions including all members are chunked correctly
- [ ] AC-007: JavaScript arrow functions are recognized and chunked as semantic units
- [ ] AC-008: Nested classes/methods are chunked at configurable depth (class or method level)
- [ ] AC-009: Interface definitions (C#/TypeScript) are extracted as complete chunks
- [ ] AC-010: Enum definitions are extracted as complete chunks

### Language-Specific Parsing (AC-011 to AC-022)

- [ ] AC-011: C# files with `.cs` extension are parsed using Roslyn
- [ ] AC-012: TypeScript files (`.ts`, `.tsx`) are parsed using TypeScript compiler API
- [ ] AC-013: JavaScript files (`.js`, `.jsx`) are parsed using TypeScript compiler API
- [ ] AC-014: C# record types are chunked correctly (C# 9+ syntax)
- [ ] AC-015: C# expression-bodied members are chunked with method definition
- [ ] AC-016: C# partial classes are chunked per file (not merged across files)
- [ ] AC-017: TypeScript interfaces with generics are chunked correctly
- [ ] AC-018: TypeScript decorators are included with class/method chunks
- [ ] AC-019: TypeScript/JavaScript ES6 modules (import/export) are handled correctly
- [ ] AC-020: JavaScript CommonJS (require/module.exports) is chunked correctly
- [ ] AC-021: C# attributes are included with decorated members in same chunk
- [ ] AC-022: TypeScript type aliases are extracted as complete chunks

### Line-Based Chunking (AC-023 to AC-030)

- [ ] AC-023: Files without structural parser fall back to line-based chunking
- [ ] AC-024: Line-based chunks respect configured `lines_per_chunk` value
- [ ] AC-025: Line-based chunks include configured overlap (`overlap_lines`)
- [ ] AC-026: Overlap lines appear at end of chunk N and start of chunk N+1
- [ ] AC-027: Line-based chunking works for any text file (.txt, .md, .yaml, etc.)
- [ ] AC-028: Line-based chunking handles files with varying line lengths
- [ ] AC-029: Line-based chunking handles empty lines correctly (preserves in chunks)
- [ ] AC-030: Line-based chunking does not split mid-line (respects line boundaries)

### Token Counting and Limits (AC-031 to AC-042)

- [ ] AC-031: Each chunk includes accurate token count estimate in metadata
- [ ] AC-032: Token counts use model-specific tokenizer (tiktoken for GPT, Claude tokenizer for Claude)
- [ ] AC-033: Chunks exceeding `max_tokens` are split at logical boundaries
- [ ] AC-034: Chunks below `min_tokens` are combined with adjacent chunks when possible
- [ ] AC-035: Token count estimation occurs before chunks are returned
- [ ] AC-036: Token count cache hits are logged (for performance monitoring)
- [ ] AC-037: Token count for identical content is deterministic (same count every time)
- [ ] AC-038: Token overflow (chunk > max_tokens) triggers split with boundary markers (`// [Chunk 1 of N]`)
- [ ] AC-039: Very large methods exceeding max_tokens are split at logical points (end of if/for/while blocks)
- [ ] AC-040: Token estimation fallback (character count ÷ 4) works when tokenizer unavailable
- [ ] AC-041: Token count includes chunk content only (not metadata overhead)
- [ ] AC-042: Sum of all chunk token counts equals total file token count (±overlap regions)

### Chunk Metadata (AC-043 to AC-052)

- [ ] AC-043: Each chunk includes `FilePath` (repository-relative, sanitized)
- [ ] AC-044: Each chunk includes `LineStart` and `LineEnd` (1-indexed, inclusive)
- [ ] AC-045: Each chunk includes `TokenEstimate` (integer, > 0)
- [ ] AC-046: Each chunk includes `Type` (Structural, LineBased, or TokenBased)
- [ ] AC-047: Each chunk includes `Hierarchy` array (e.g., ["Namespace", "Class", "Method"])
- [ ] AC-048: Hierarchy for C# chunks includes namespace, class, method (when applicable)
- [ ] AC-049: Hierarchy for TypeScript chunks includes module, class, function (when applicable)
- [ ] AC-050: Hierarchy for line-based chunks is empty array (no structure parsed)
- [ ] AC-051: File paths are normalized to forward slashes (Unix-style)
- [ ] AC-052: File paths do not contain `../` or absolute paths (security requirement)

### Strategy Selection and Fallback (AC-053 to AC-061)

- [ ] AC-053: Chunker selects C# parser for `.cs` files
- [ ] AC-054: Chunker selects TypeScript parser for `.ts`, `.tsx`, `.js`, `.jsx` files
- [ ] AC-055: Chunker selects line-based parser for unsupported extensions
- [ ] AC-056: Parse failure triggers automatic fallback to line-based chunking
- [ ] AC-057: Fallback is logged with warning message including file path and error details
- [ ] AC-058: Fallback due to timeout (> 5 seconds) is logged separately from parse errors
- [ ] AC-059: Fallback due to excessive nesting depth (> 500 levels) is logged
- [ ] AC-060: Binary files (detected via null bytes) are skipped with warning log
- [ ] AC-061: Minified files (avg line > 1KB) use line-based chunking (skip structural)

### Configuration Loading (AC-062 to AC-068)

- [ ] AC-062: Chunking configuration is loaded from `.agent/config.yml`
- [ ] AC-063: `max_tokens` configuration is respected (default: 2000)
- [ ] AC-064: `min_tokens` configuration is respected (default: 100)
- [ ] AC-065: `lines_per_chunk` configuration is respected for line-based chunking (default: 50)
- [ ] AC-066: `overlap_lines` configuration is respected (default: 5)
- [ ] AC-067: Invalid configuration (max < min) triggers clear error message at startup
- [ ] AC-068: Missing configuration uses documented default values

### Error Handling (AC-069 to AC-076)

- [ ] AC-069: Parse errors are logged with file path and error message
- [ ] AC-070: Parse errors do not crash the application (graceful fallback)
- [ ] AC-071: Encoding errors (non-UTF-8) are logged and handled via fallback encoding detection
- [ ] AC-072: Null or empty file content returns empty chunk list (not exception)
- [ ] AC-073: Invalid ChunkOptions (null, negative values) throws ArgumentException with clear message
- [ ] AC-074: Out of memory errors during parsing are logged and re-thrown (critical error)
- [ ] AC-075: Stack overflow during AST traversal is logged (cannot recover, but diagnostic)
- [ ] AC-076: Regex timeout (ReDoS) in line-based chunker is handled gracefully

### Performance (AC-077 to AC-082)

- [ ] AC-077: 10KB C# file is chunked in < 50ms (including Roslyn parse)
- [ ] AC-078: 100KB TypeScript file is chunked in < 200ms (including TS compiler parse)
- [ ] AC-079: 1MB line-based file is chunked in < 500ms
- [ ] AC-080: Memory usage during chunking does not exceed 2x file size
- [ ] AC-081: Parallel chunking of 10 files completes faster than sequential (measurable speedup)
- [ ] AC-082: Parse result caching reduces repeat chunking time by >90% (cache hit scenario)

### Integration (AC-083 to AC-088)

- [ ] AC-083: Chunks returned by IChunker.Chunk() are compatible with Context Packer (Task 016)
- [ ] AC-084: Chunk metadata is compatible with Ranking (Task 016.b) requirements
- [ ] AC-085: Overlapping chunks are compatible with Deduplication (Task 016.c) logic
- [ ] AC-086: Chunker integrates with RepoFS (Task 014) for file content retrieval
- [ ] AC-087: Chunker uses ITokenCounter (Task 016) for exact token counting
- [ ] AC-088: Chunk cache integrates with Index (Task 015) when caching is enabled

---

## Best Practices

### Chunking Strategy

1. **Prefer semantic boundaries** - Split at function/class boundaries, not arbitrary lines
2. **Respect language syntax** - Don't break mid-statement or mid-block
3. **Include leading context** - Imports, namespace declarations for understanding
4. **Add trailing context** - Closing braces, related code if space permits

### Size Management

5. **Configurable chunk sizes** - Different contexts need different granularity
6. **Overlap for continuity** - Include few lines overlap between chunks
7. **Handle edge cases** - Very long lines, minified code, binary files
8. **Track source metadata** - Preserve file, line numbers through chunking

### Quality Assurance

9. **Validate chunk boundaries** - Verify syntax is valid after chunking
10. **Test language coverage** - Ensure rules work for all supported languages
11. **Measure chunk quality** - Are chunks useful in context? Test with LLM
12. **Log chunking decisions** - Record why boundaries were chosen

---

## Testing Requirements

### Unit Tests - Complete C# Implementations

```csharp
using Xunit;
using FluentAssertions;
using Acode.Domain.Context;
using Acode.Infrastructure.Context.Chunking;

namespace Acode.Tests.Unit.Context.Chunking
{
    public sealed class CSharpChunkerTests
    {
        private readonly CSharpChunker _sut;

        public CSharpChunkerTests()
        {
            var logger = new NullLogger<CSharpChunker>();
            _sut = new CSharpChunker(logger);
        }

        [Fact]
        public void Should_Parse_Class_As_Single_Chunk()
        {
            // Arrange
            string code = @"
using System;

namespace MyApp
{
    public class UserService
    {
        private readonly IUserRepository _repo;

        public UserService(IUserRepository repo)
        {
            _repo = repo;
        }

        public User GetUser(int id)
        {
            return _repo.FindById(id);
        }
    }
}";

            var options = new ChunkOptions
            {
                MaxTokens = 2000,
                MinTokens = 100,
                ChunkLevel = ChunkLevel.Class
            };

            // Act
            var chunks = _sut.Chunk(code, options);

            // Assert
            chunks.Should().HaveCount(2);  // Chunk 1: usings + namespace, Chunk 2: UserService class
            chunks[1].Type.Should().Be(ChunkType.Structural);
            chunks[1].Content.Should().Contain("public class UserService");
            chunks[1].Content.Should().Contain("public User GetUser");
            chunks[1].Hierarchy.Should().BeEquivalentTo(new[] { "MyApp", "UserService" });
            chunks[1].LineStart.Should().Be(6);
            chunks[1].LineEnd.Should().Be(18);
        }

        [Fact]
        public void Should_Chunk_By_Method_When_Configured()
        {
            // Arrange
            string code = @"
public class Calculator
{
    public int Add(int a, int b)
    {
        return a + b;
    }

    public int Subtract(int a, int b)
    {
        return a - b;
    }

    public int Multiply(int a, int b)
    {
        return a * b;
    }
}";

            var options = new ChunkOptions
            {
                MaxTokens = 500,
                ChunkLevel = ChunkLevel.Method
            };

            // Act
            var chunks = _sut.Chunk(code, options);

            // Assert
            chunks.Should().HaveCount(4);  // Class header + 3 methods
            chunks[1].Content.Should().Contain("public int Add");
            chunks[1].Hierarchy.Should().EndWith("Add");
            chunks[2].Content.Should().Contain("public int Subtract");
            chunks[3].Content.Should().Contain("public int Multiply");
        }

        [Fact]
        public void Should_Handle_Nested_Classes()
        {
            // Arrange
            string code = @"
public class Outer
{
    public class Inner
    {
        public void DoWork() { }
    }

    public void OuterMethod() { }
}";

            var options = new ChunkOptions { ChunkLevel = ChunkLevel.Class };

            // Act
            var chunks = _sut.Chunk(code, options);

            // Assert
            chunks.Should().ContainSingle(c => c.Hierarchy.Contains("Inner"));
            var innerChunk = chunks.Single(c => c.Hierarchy.Contains("Inner"));
            innerChunk.Hierarchy.Should().BeEquivalentTo(new[] { "Outer", "Inner" });
        }

        [Fact]
        public void Should_Fallback_To_Line_Based_On_Parse_Error()
        {
            // Arrange - malformed code (unmatched braces)
            string code = @"
public class Broken
{
    public void Method()
    {
        if (true) {
    }
}";  // Missing closing brace

            var options = new ChunkOptions { MaxTokens = 1000 };

            // Act
            var chunks = _sut.Chunk(code, options);

            // Assert
            chunks.Should().NotBeNull();
            chunks.Should().AllSatisfy(c => c.Type.Should().Be(ChunkType.LineBased));
        }

        [Fact]
        public void Should_Include_Attributes_With_Method()
        {
            // Arrange
            string code = @"
public class Controller
{
    [HttpGet(""/users/{id}"")]
    [Authorize]
    public User GetUser(int id)
    {
        return _service.GetUser(id);
    }
}";

            var options = new ChunkOptions { ChunkLevel = ChunkLevel.Method };

            // Act
            var chunks = _sut.Chunk(code, options);

            // Assert
            var methodChunk = chunks.Single(c => c.Content.Contains("GetUser"));
            methodChunk.Content.Should().Contain("[HttpGet");
            methodChunk.Content.Should().Contain("[Authorize]");
        }

        [Fact]
        public void Should_Split_Large_Method_Exceeding_Max_Tokens()
        {
            // Arrange - very long method (500 lines)
            var largeMethod = new StringBuilder();
            largeMethod.AppendLine("public class LargeClass {");
            largeMethod.AppendLine("public void LargeMethod() {");
            for (int i = 0; i < 500; i++)
            {
                largeMethod.AppendLine($"    var x{i} = Calculate{i}();");
            }
            largeMethod.AppendLine("}");
            largeMethod.AppendLine("}");

            var options = new ChunkOptions
            {
                MaxTokens = 500  // Force split
            };

            // Act
            var chunks = _sut.Chunk(largeMethod.ToString(), options);

            // Assert
            chunks.Should().HaveCountGreaterThan(3);  // Method split into multiple chunks
            chunks.Should().Contain(c => c.Content.Contains("// [Chunk"));  // Boundary markers
        }
    }

    public sealed class LineBasedChunkerTests
    {
        private readonly LineBasedChunker _sut;

        public LineBasedChunkerTests()
        {
            _sut = new LineBasedChunker();
        }

        [Fact]
        public void Should_Chunk_By_Line_Count()
        {
            // Arrange
            var lines = Enumerable.Range(1, 100).Select(i => $"Line {i}");
            string content = string.Join("\n", lines);

            var options = new ChunkOptions
            {
                LinesPerChunk = 25,
                OverlapLines = 0
            };

            // Act
            var chunks = _sut.Chunk(content, options);

            // Assert
            chunks.Should().HaveCount(4);  // 100 lines / 25 lines per chunk
            chunks[0].LineStart.Should().Be(1);
            chunks[0].LineEnd.Should().Be(25);
            chunks[3].LineStart.Should().Be(76);
            chunks[3].LineEnd.Should().Be(100);
        }

        [Fact]
        public void Should_Add_Overlap_Between_Chunks()
        {
            // Arrange
            var lines = Enumerable.Range(1, 60).Select(i => $"Line {i}");
            string content = string.Join("\n", lines);

            var options = new ChunkOptions
            {
                LinesPerChunk = 20,
                OverlapLines = 5
            };

            // Act
            var chunks = _sut.Chunk(content, options);

            // Assert
            chunks.Should().HaveCount(4);  // (60 / (20 - 5)) = 4 chunks with overlap

            // Verify overlap: last 5 lines of chunk 0 = first 5 lines of chunk 1
            var chunk0Lines = chunks[0].Content.Split('\n');
            var chunk1Lines = chunks[1].Content.Split('\n');

            chunk0Lines.TakeLast(5).Should().BeEquivalentTo(chunk1Lines.Take(5));
        }

        [Fact]
        public void Should_Handle_Empty_File()
        {
            // Arrange
            string content = string.Empty;
            var options = new ChunkOptions();

            // Act
            var chunks = _sut.Chunk(content, options);

            // Assert
            chunks.Should().BeEmpty();
        }

        [Fact]
        public void Should_Preserve_Line_Numbers_Accurately()
        {
            // Arrange
            string content = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5";
            var options = new ChunkOptions { LinesPerChunk = 2, OverlapLines = 0 };

            // Act
            var chunks = _sut.Chunk(content, options);

            // Assert
            chunks[0].LineStart.Should().Be(1);
            chunks[0].LineEnd.Should().Be(2);
            chunks[1].LineStart.Should().Be(3);
            chunks[1].LineEnd.Should().Be(4);
            chunks[2].LineStart.Should().Be(5);
            chunks[2].LineEnd.Should().Be(5);
        }
    }

    public sealed class ChunkerFactoryTests
    {
        private readonly ChunkerFactory _sut;

        public ChunkerFactoryTests()
        {
            var logger = new NullLogger<ChunkerFactory>();
            _sut = new ChunkerFactory(logger);
        }

        [Fact]
        public void Should_Select_CSharp_Chunker_For_Cs_Files()
        {
            // Arrange
            string filePath = "/repo/Services/UserService.cs";
            string content = "public class UserService { }";

            // Act
            var chunker = _sut.CreateChunker(filePath, content);

            // Assert
            chunker.Should().BeOfType<CSharpChunker>();
        }

        [Fact]
        public void Should_Select_TypeScript_Chunker_For_TS_Files()
        {
            // Arrange
            string filePath = "/repo/src/UserService.ts";
            string content = "export class UserService { }";

            // Act
            var chunker = _sut.CreateChunker(filePath, content);

            // Assert
            chunker.Should().BeOfType<TypeScriptChunker>();
        }

        [Fact]
        public void Should_Fallback_To_LineBased_For_Unknown_Extension()
        {
            // Arrange
            string filePath = "/repo/README.md";
            string content = "# README";

            // Act
            var chunker = _sut.CreateChunker(filePath, content);

            // Assert
            chunker.Should().BeOfType<LineBasedChunker>();
        }

        [Fact]
        public void Should_Reject_Files_Exceeding_Size_Limit()
        {
            // Arrange
            string filePath = "/repo/huge.cs";
            string content = new string('x', 10_000_000);  // 10MB

            // Act
            var chunker = _sut.CreateChunker(filePath, content);

            // Assert - should still return chunker but with line-based fallback
            chunker.Should().BeOfType<LineBasedChunker>();
        }

        [Fact]
        public void Should_Detect_Minified_Files_And_Use_LineBased()
        {
            // Arrange
            string filePath = "/repo/bundle.min.js";
            string content = new string('x', 50000);  // Single 50KB line

            // Act
            var chunker = _sut.CreateChunker(filePath, content);

            // Assert
            chunker.Should().BeOfType<LineBasedChunker>();
        }
    }
}
```

### Integration Tests

```
Tests/Integration/Context/Chunking/
├── ChunkingIntegrationTests.cs
│   ├── Should_Chunk_Real_CSharp_File()
│   ├── Should_Chunk_Real_TypeScript_File()
│   ├── Should_Chunk_Real_JavaScript_File()
│   ├── Should_Chunk_Large_File()
│   ├── Should_Handle_Parse_Errors_Gracefully()
│   └── Should_Chunk_Mixed_Content()
│
└── TokenEstimatorIntegrationTests.cs
    ├── Should_Match_GPT4_Tokenizer()
    └── Should_Match_Claude_Tokenizer()
```

### E2E Tests

```
Tests/E2E/Context/Chunking/
├── ChunkingE2ETests.cs
│   ├── Should_Chunk_For_Context_Packer()
│   ├── Should_Work_With_Real_Codebase()
│   └── Should_Respect_Config_Settings()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| 10KB file | 25ms | 50ms |
| 100KB file | 100ms | 200ms |
| 1MB file | 500ms | 1000ms |

---

## User Verification Steps

### Scenario 1: Structural

1. Chunk C# file with classes
2. Verify: Class boundaries respected

### Scenario 2: Line-Based

1. Chunk plain text file
2. Verify: Even chunks with overlap

### Scenario 3: Large File

1. Chunk very large file
2. Verify: Handles without OOM

### Scenario 4: Fallback

1. Chunk file with syntax errors
2. Verify: Falls back to line-based

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Context/
│   └── IChunker.cs
│
src/AgenticCoder.Infrastructure/
├── Context/
│   └── Chunking/
│       ├── ChunkerFactory.cs
│       ├── StructuralChunker.cs
│       ├── CSharpChunker.cs
│       ├── TypeScriptChunker.cs
│       ├── LineBasedChunker.cs
│       └── TokenEstimator.cs
```

### Complete C# Implementation Code

```csharp
// ===================================================================
// Domain Layer: Interfaces and Entities
// File: src/Acode.Domain/Context/IChunker.cs
// ===================================================================

namespace Acode.Domain.Context;

/// <summary>
/// Defines the contract for chunking file content into semantic or line-based segments.
/// </summary>
public interface IChunker
{
    /// <summary>
    /// Chunks the provided content into a list of ContentChunk instances.
    /// </summary>
    /// <param name="content">The file content to chunk</param>
    /// <param name="options">Chunking configuration options</param>
    /// <returns>List of chunks, ordered by appearance in source file</returns>
    IReadOnlyList<ContentChunk> Chunk(string content, ChunkOptions options);
}

/// <summary>
/// Represents a chunk of source code with metadata.
/// </summary>
public sealed record ContentChunk(
    string Content,
    string FilePath,
    int LineStart,
    int LineEnd,
    int TokenEstimate,
    ChunkType Type,
    IReadOnlyList<string> Hierarchy);

/// <summary>
/// Configuration options for chunking behavior.
/// </summary>
public sealed record ChunkOptions
{
    public int MaxTokens { get; init; } = 2000;
    public int MinTokens { get; init; } = 100;
    public int LinesPerChunk { get; init; } = 50;
    public int OverlapLines { get; init; } = 5;
    public ChunkLevel ChunkLevel { get; init; } = ChunkLevel.Method;
}

public enum ChunkType
{
    Structural,
    LineBased,
    TokenBased
}

public enum ChunkLevel
{
    Namespace,
    Class,
    Method
}

// ===================================================================
// Infrastructure Layer: Line-Based Chunker Implementation
// File: src/Acode.Infrastructure/Context/Chunking/LineBasedChunker.cs
// ===================================================================

using Acode.Domain.Context;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Context.Chunking;

public sealed class LineBasedChunker : IChunker
{
    private readonly ILogger<LineBasedChunker> _logger;
    private readonly ITokenCounter _tokenCounter;

    public LineBasedChunker(
        ILogger<LineBasedChunker> logger,
        ITokenCounter tokenCounter)
    {
        _logger = logger;
        _tokenCounter = tokenCounter;
    }

    public IReadOnlyList<ContentChunk> Chunk(string content, ChunkOptions options)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Array.Empty<ContentChunk>();
        }

        var lines = content.Split('\n');
        var chunks = new List<ContentChunk>();

        int linesPerChunk = options.LinesPerChunk;
        int overlapLines = options.OverlapLines;

        // Validate configuration
        if (overlapLines >= linesPerChunk)
        {
            _logger.LogWarning(
                "Overlap lines ({Overlap}) >= lines per chunk ({LinesPerChunk}). Setting overlap to 0.",
                overlapLines,
                linesPerChunk);
            overlapLines = 0;
        }

        int currentLine = 0;
        while (currentLine < lines.Length)
        {
            int startLine = currentLine;
            int endLine = Math.Min(currentLine + linesPerChunk, lines.Length);

            // Extract chunk content
            var chunkLines = lines[startLine..endLine];
            string chunkContent = string.Join("\n", chunkLines);

            // Estimate tokens
            int tokenEstimate = _tokenCounter.CountTokens(chunkContent);

            var chunk = new ContentChunk(
                Content: chunkContent,
                FilePath: string.Empty,  // Set by caller
                LineStart: startLine + 1,  // 1-indexed
                LineEnd: endLine,
                TokenEstimate: tokenEstimate,
                Type: ChunkType.LineBased,
                Hierarchy: Array.Empty<string>());

            chunks.Add(chunk);

            // Advance position (accounting for overlap)
            currentLine += (linesPerChunk - overlapLines);

            // Prevent infinite loop if overlap == linesPerChunk
            if (currentLine <= startLine)
            {
                currentLine = startLine + 1;
            }
        }

        _logger.LogInformation(
            "Line-based chunking produced {ChunkCount} chunks from {LineCount} lines",
            chunks.Count,
            lines.Length);

        return chunks;
    }
}

// ===================================================================
// Infrastructure Layer: C# Structural Chunker Implementation
// File: src/Acode.Infrastructure/Context/Chunking/CSharpChunker.cs
// ===================================================================

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Acode.Infrastructure.Context.Chunking;

public sealed class CSharpChunker : IChunker
{
    private readonly ILogger<CSharpChunker> _logger;
    private readonly ITokenCounter _tokenCounter;
    private readonly LineBasedChunker _fallbackChunker;
    private readonly int _maxParseTimeMs;
    private readonly int _maxNestingDepth;

    public CSharpChunker(
        ILogger<CSharpChunker> logger,
        ITokenCounter tokenCounter,
        LineBasedChunker fallbackChunker,
        int maxParseTimeMs = 5000,
        int maxNestingDepth = 500)
    {
        _logger = logger;
        _tokenCounter = tokenCounter;
        _fallbackChunker = fallbackChunker;
        _maxParseTimeMs = maxParseTimeMs;
        _maxNestingDepth = maxNestingDepth;
    }

    public IReadOnlyList<ContentChunk> Chunk(string content, ChunkOptions options)
    {
        try
        {
            using var cts = new CancellationTokenSource(_maxParseTimeMs);

            // Parse C# code with timeout
            var tree = CSharpSyntaxTree.ParseText(
                content,
                CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.None),
                cancellationToken: cts.Token);

            var root = tree.GetRoot(cts.Token);

            // Validate nesting depth
            int maxDepth = CalculateMaxDepth(root);
            if (maxDepth > _maxNestingDepth)
            {
                _logger.LogWarning(
                    "C# file has excessive nesting depth ({Depth} > {Max}). Falling back to line-based.",
                    maxDepth,
                    _maxNestingDepth);

                return _fallbackChunker.Chunk(content, options);
            }

            // Extract chunks based on configured level
            return options.ChunkLevel switch
            {
                ChunkLevel.Namespace => ChunkByNamespace(root, content, options),
                ChunkLevel.Class => ChunkByClass(root, content, options),
                ChunkLevel.Method => ChunkByMethod(root, content, options),
                _ => throw new ArgumentException($"Unsupported chunk level: {options.ChunkLevel}")
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("C# parsing timed out after {Timeout}ms. Falling back to line-based.",
                _maxParseTimeMs);
            return _fallbackChunker.Chunk(content, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "C# parsing failed. Falling back to line-based.");
            return _fallbackChunker.Chunk(content, options);
        }
    }

    private IReadOnlyList<ContentChunk> ChunkByClass(SyntaxNode root, string content, ChunkOptions options)
    {
        var chunks = new List<ContentChunk>();
        var lines = content.Split('\n');

        // First chunk: using statements + namespace declaration
        var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>();
        var namespaceDecl = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();

        if (usings.Any() || namespaceDecl != null)
        {
            int headerEndLine = namespaceDecl?.OpenBraceToken.GetLocation().GetLineSpan().StartLinePosition.Line ?? 0;
            if (headerEndLine > 0)
            {
                var headerContent = string.Join("\n", lines.Take(headerEndLine + 1));
                chunks.Add(CreateChunk(headerContent, 1, headerEndLine + 1, Array.Empty<string>()));
            }
        }

        // Chunk each class
        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
        foreach (var classDecl in classes)
        {
            var lineSpan = classDecl.GetLocation().GetLineSpan();
            int startLine = lineSpan.StartLinePosition.Line + 1;
            int endLine = lineSpan.EndLinePosition.Line + 1;

            var classContent = string.Join("\n", lines[(startLine - 1)..endLine]);

            var hierarchy = BuildHierarchy(classDecl);

            chunks.Add(CreateChunk(classContent, startLine, endLine, hierarchy));
        }

        return chunks;
    }

    private IReadOnlyList<ContentChunk> ChunkByMethod(SyntaxNode root, string content, ChunkOptions options)
    {
        var chunks = new List<ContentChunk>();
        var lines = content.Split('\n');

        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var lineSpan = method.GetLocation().GetLineSpan();
            int startLine = lineSpan.StartLinePosition.Line + 1;
            int endLine = lineSpan.EndLinePosition.Line + 1;

            var methodContent = string.Join("\n", lines[(startLine - 1)..endLine]);

            var hierarchy = BuildHierarchy(method);

            var chunk = CreateChunk(methodContent, startLine, endLine, hierarchy);

            // Check if chunk exceeds max tokens
            if (chunk.TokenEstimate > options.MaxTokens)
            {
                _logger.LogWarning(
                    "Method {Method} exceeds max tokens ({Actual} > {Max}). Splitting.",
                    method.Identifier.Text,
                    chunk.TokenEstimate,
                    options.MaxTokens);

                // Split large method
                var splitChunks = SplitLargeChunk(chunk, options.MaxTokens);
                chunks.AddRange(splitChunks);
            }
            else
            {
                chunks.Add(chunk);
            }
        }

        return chunks;
    }

    private ContentChunk CreateChunk(string content, int startLine, int endLine, IReadOnlyList<string> hierarchy)
    {
        int tokenEstimate = _tokenCounter.CountTokens(content);

        return new ContentChunk(
            Content: content,
            FilePath: string.Empty,  // Set by caller
            LineStart: startLine,
            LineEnd: endLine,
            TokenEstimate: tokenEstimate,
            Type: ChunkType.Structural,
            Hierarchy: hierarchy);
    }

    private IReadOnlyList<string> BuildHierarchy(SyntaxNode node)
    {
        var hierarchy = new List<string>();

        var current = node;
        while (current != null)
        {
            switch (current)
            {
                case NamespaceDeclarationSyntax ns:
                    hierarchy.Insert(0, ns.Name.ToString());
                    break;
                case ClassDeclarationSyntax cls:
                    hierarchy.Insert(0, cls.Identifier.Text);
                    break;
                case MethodDeclarationSyntax method:
                    hierarchy.Insert(0, method.Identifier.Text);
                    break;
            }

            current = current.Parent;
        }

        return hierarchy;
    }

    private static int CalculateMaxDepth(SyntaxNode node, int currentDepth = 0)
    {
        if (node == null) return currentDepth;

        int maxChildDepth = currentDepth;
        foreach (var child in node.ChildNodes())
        {
            int childDepth = CalculateMaxDepth(child, currentDepth + 1);
            maxChildDepth = Math.Max(maxChildDepth, childDepth);
        }

        return maxChildDepth;
    }

    private IReadOnlyList<ContentChunk> SplitLargeChunk(ContentChunk chunk, int maxTokens)
    {
        // Split at logical boundaries (mid-point for now, could be improved)
        var lines = chunk.Content.Split('\n');
        int midPoint = lines.Length / 2;

        var chunk1Content = string.Join("\n", lines.Take(midPoint));
        var chunk2Content = string.Join("\n", lines.Skip(midPoint));

        var chunk1 = chunk with
        {
            Content = chunk1Content + "\n// [Chunk 1 of 2]",
            LineEnd = chunk.LineStart + midPoint - 1,
            TokenEstimate = _tokenCounter.CountTokens(chunk1Content)
        };

        var chunk2 = chunk with
        {
            Content = "// [Chunk 2 of 2]\n" + chunk2Content,
            LineStart = chunk.LineStart + midPoint,
            TokenEstimate = _tokenCounter.CountTokens(chunk2Content)
        };

        return new[] { chunk1, chunk2 };
    }

    private IReadOnlyList<ContentChunk> ChunkByNamespace(SyntaxNode root, string content, ChunkOptions options)
    {
        // Simplified: chunk entire namespace as one unit
        var lines = content.Split('\n');
        return new[] { CreateChunk(content, 1, lines.Length, Array.Empty<string>()) };
    }
}

// ===================================================================
// Infrastructure Layer: Chunker Factory
// File: src/Acode.Infrastructure/Context/Chunking/ChunkerFactory.cs
// ===================================================================

namespace Acode.Infrastructure.Context.Chunking;

public sealed class ChunkerFactory : IChunkerFactory
{
    private readonly ILogger<ChunkerFactory> _logger;
    private readonly ITokenCounter _tokenCounter;
    private readonly int _maxFileSizeBytes;

    public ChunkerFactory(
        ILogger<ChunkerFactory> logger,
        ITokenCounter tokenCounter,
        int maxFileSizeBytes = 5_000_000)
    {
        _logger = logger;
        _tokenCounter = tokenCounter;
        _maxFileSizeBytes = maxFileSizeBytes;
    }

    public IChunker CreateChunker(string filePath, string content)
    {
        // File size check
        int contentBytes = System.Text.Encoding.UTF8.GetByteCount(content);
        if (contentBytes > _maxFileSizeBytes)
        {
            _logger.LogWarning(
                "File {FilePath} ({Size}MB) exceeds size limit ({Limit}MB). Using line-based chunking.",
                filePath,
                contentBytes / 1_000_000.0,
                _maxFileSizeBytes / 1_000_000.0);

            return new LineBasedChunker(_logger, _tokenCounter);
        }

        // Minified file detection
        int lineCount = content.Split('\n').Length;
        double avgLineLength = contentBytes / (double)Math.Max(lineCount, 1);

        if (avgLineLength > 1000)
        {
            _logger.LogWarning(
                "File {FilePath} appears minified (avg line: {AvgLength} chars). Using line-based chunking.",
                filePath,
                (int)avgLineLength);

            return new LineBasedChunker(_logger, _tokenCounter);
        }

        // Extension-based selection
        string extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".cs" => new CSharpChunker(
                _logger,
                _tokenCounter,
                new LineBasedChunker(_logger, _tokenCounter)),

            ".ts" or ".tsx" or ".js" or ".jsx" => new TypeScriptChunker(
                _logger,
                _tokenCounter,
                new LineBasedChunker(_logger, _tokenCounter)),

            _ => new LineBasedChunker(_logger, _tokenCounter)
        };
    }
}

// ===================================================================
// Dependency Injection Setup
// File: src/Acode.Infrastructure/DependencyInjection.cs
// ===================================================================

using Microsoft.Extensions.DependencyInjection;

namespace Acode.Infrastructure;

public static class ChunkingServiceExtensions
{
    public static IServiceCollection AddChunking(this IServiceCollection services)
    {
        services.AddSingleton<ITokenCounter, ExactTokenCounter>();
        services.AddScoped<IChunkerFactory, ChunkerFactory>();
        services.AddScoped<LineBasedChunker>();
        services.AddScoped<CSharpChunker>();

        return services;
    }
}

// ===================================================================
// Usage Example
// ===================================================================

// In application code:
public class ContextPackerService
{
    private readonly IChunkerFactory _chunkerFactory;

    public ContextPackerService(IChunkerFactory chunkerFactory)
    {
        _chunkerFactory = chunkerFactory;
    }

    public async Task<List<ContentChunk>> ProcessFile(string filePath)
    {
        // Read file content
        string content = await File.ReadAllTextAsync(filePath);

        // Create appropriate chunker
        var chunker = _chunkerFactory.CreateChunker(filePath, content);

        // Configure chunking options
        var options = new ChunkOptions
        {
            MaxTokens = 2000,
            MinTokens = 100,
            ChunkLevel = ChunkLevel.Method
        };

        // Chunk the file
        var chunks = chunker.Chunk(content, options);

        // Set file path on each chunk
        return chunks.Select(c => c with { FilePath = filePath }).ToList();
    }
}
```

### Implementation Checklist

- [ ] Domain layer: Define IChunker, ContentChunk, ChunkOptions, enums
- [ ] Infrastructure: Implement LineBasedChunker with overlap logic
- [ ] Infrastructure: Implement CSharpChunker using Roslyn AST parsing
- [ ] Infrastructure: Implement TypeScriptChunker using TS compiler API
- [ ] Infrastructure: Implement ChunkerFactory with file size/extension detection
- [ ] Infrastructure: Add parse timeout and nesting depth validation
- [ ] Infrastructure: Implement chunk splitting for oversized methods
- [ ] Testing: Write unit tests for each chunker (Arrange-Act-Assert pattern)
- [ ] Testing: Write integration tests with real C#/TS files
- [ ] Configuration: Load chunking settings from `.agent/config.yml`
- [ ] Logging: Add diagnostic logging for fallback scenarios
- [ ] Documentation: Update user guide with chunking configuration examples

### Rollout Strategy

**Phase 1: Line-Based Chunking (Week 1)**
- Implement LineBasedChunker with configurable line count and overlap
- Implement ITokenCounter for accurate token estimation
- Add unit tests for line chunking edge cases
- Deploy as universal fallback for all file types

**Phase 2: C# Structural Chunking (Week 2)**
- Integrate Roslyn parser (Microsoft.CodeAnalysis.CSharp NuGet)
- Implement CSharpChunker with class/method level chunking
- Add timeout and nesting depth safeguards
- Test with real C# files from repository

**Phase 3: TypeScript/JavaScript Support (Week 3)**
- Integrate TypeScript compiler API (via embedded V8 or subprocess)
- Implement TypeScriptChunker for .ts/.tsx/.js/.jsx files
- Handle ES6 modules, CommonJS, decorators
- Test with React/Vue/Angular codebases

**Phase 4: Factory and Configuration (Week 4)**
- Implement ChunkerFactory with file size/extension/minification detection
- Load chunking configuration from `.agent/config.yml`
- Add comprehensive error handling and fallback logic
- Integration testing with Context Packer (Task 016)

**Phase 5: Performance Optimization (Week 5)**
- Add parse result caching (SHA-256 file hash keys)
- Implement parallel chunking for multiple files
- Profile and optimize hot paths (tokenization, AST traversal)
- Benchmark against performance targets (NFR-016a-01 to NFR-016a-03)

---

**End of Task 016.a Specification**