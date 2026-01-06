# Task 049.d: Indexing + Fast Search Over Chats/Runs/Messages

**Priority:** P1 – High Priority  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 2 – CLI + Orchestration Core  
**Dependencies:** Task 049.a (Data Model), Task 049.b (CLI Commands), Task 030 (Search)  

---

## Description

**Business Value & ROI**

Fast, accurate search over conversation history saves developers $14,280/year per engineer by eliminating manual scrolling and re-asking questions. A 10-engineer team saves $142,800 annually.

**Time Savings Breakdown:**
- Finding past decisions: 30 min/day → 2 min/day (93% reduction)
  - Manual scrolling through chat history: 15 min/day saved
  - Re-asking already-answered questions: 10 min/day saved
  - Context reconstruction from partial memory: 5 min/day saved
- Debugging with historical context: 45 min/week → 10 min/week (78% reduction)
  - Searching for error patterns: 20 min/week saved
  - Finding previous solutions: 15 min/week saved

**Cost Calculation:**
- 28 min/day × 220 days/year = 102.7 hours/year saved
- 35 min/week × 52 weeks/year = 30.3 hours/year saved
- **Total: 133 hours/year per engineer @ $108/hour = $14,280/year**
- **10-engineer team: $142,800/year**

**Technical Architecture**

The indexing and search system provides sub-second full-text search across conversation history using SQLite FTS5 (local) or PostgreSQL full-text search (remote). The architecture consists of three layers: indexing, querying, and ranking.

**Three-Layer Architecture:**

```
User Query: "authentication JWT validation"
     │
     ▼
┌─────────────────────────────────────────┐
│  Query Parser                           │
│  ├─ Tokenize: [auth, jwt, valid]      │
│  ├─ Expand: auth → authentication     │
│  ├─ Parse operators: AND (implicit)    │
│  └─ Build FTS query                    │
└─────────────────────────────────────────┘
     │
     ▼
┌─────────────────────────────────────────┐
│  Search Engine (SQLite FTS5)           │
│  ├─ Match terms in indexed content     │
│  ├─ Apply filters (chat, date, role)   │
│  ├─ Return candidate results           │
│  └─ Include match offsets for snippets │
└─────────────────────────────────────────┘
     │
     ▼
┌─────────────────────────────────────────┐
│  Ranking & Snippet Generation          │
│  ├─ Calculate BM25 scores              │
│  ├─ Apply recency boost (7-day decay)  │
│  ├─ Sort by final score                │
│  ├─ Generate context snippets          │
│  └─ Highlight matching terms           │
└─────────────────────────────────────────┘
     │
     ▼
   Results with snippets, ranked by relevance
```

**SQLite FTS5 Implementation:**

FTS5 (Full-Text Search version 5) is SQLite's advanced full-text indexing extension providing:
- **Porter stemming:** "authentication" matches "authenticate", "authenticating"
- **Tokenization:** Splits on whitespace, punctuation, handles camelCase
- **BM25 ranking:** Standard information retrieval algorithm
- **Match offsets:** Character positions for snippet highlighting
- **Phrase queries:** Exact sequence matching with quotes
- **Boolean operators:** AND/OR/NOT with precedence

**FTS5 Virtual Table Schema:**

```sql
CREATE VIRTUAL TABLE conversation_search USING fts5(
    message_id UNINDEXED,
    chat_id UNINDEXED,
    run_id UNINDEXED,
    created_at UNINDEXED,
    role UNINDEXED,
    content,
    chat_title,
    tags,
    tokenize='porter unicode61',
    content='messages',  -- External content table
    content_rowid='rowid'
);
```

**Key Design Decisions:**
- **External content:** FTS5 table references `messages` table, avoiding duplication
- **UNINDEXED columns:** Metadata stored for filtering but not searched
- **Porter tokenizer:** English stemming for better recall
- **unicode61:** Handles international characters correctly

**Incremental Indexing Flow:**

```
Message Created
     │
     ▼
[Message Repository SaveAsync()]
     │
     ├─ Write to messages table
     ├─ Publish MessageCreated event
     │
     ▼
[SearchIndexer EventHandler]
     │
     ├─ Extract searchable fields:
     │   ├─ Message content
     │   ├─ Chat title
     │   └─ Tags
     │
     ├─ INSERT INTO conversation_search
     │     (message_id, chat_id, content, ...)
     │     VALUES (?, ?, ?, ...)
     │
     └─ Log index update (< 10ms)
```

**Search Query Processing:**

```csharp
// User query: "JWT validation" --chat auth-chat --since 2025-12-01

1. Parse query text:
   - Terms: [JWT, validation]
   - Chat filter: auth-chat
   - Date filter: >= 2025-12-01

2. Build FTS5 query:
   SELECT 
     message_id,
     snippet(conversation_search, 2, '<mark>', '</mark>', '...', 32) AS snippet,
     bm25(conversation_search) AS score
   FROM conversation_search
   WHERE conversation_search MATCH 'JWT AND validation'
     AND chat_id = 'auth-chat'
     AND created_at >= '2025-12-01'
   ORDER BY score DESC
   LIMIT 20;

3. Apply recency boost:
   - Messages < 7 days old: score × 1.5
   - Messages 7-30 days old: score × 1.0
   - Messages > 30 days old: score × 0.8

4. Format results:
   [
     {
       "messageId": "...",
       "snippet": "...implementing <mark>JWT</mark> <mark>validation</mark> for...",
       "score": 12.34,
       "createdAt": "2025-12-15T10:30:00Z"
     },
     ...
   ]
```

**Ranking Algorithm (BM25 with Recency Boost):**

BM25 (Best Match 25) is the industry-standard term-based ranking function:

```
score = Σ IDF(term) × (TF(term) × (k1 + 1)) / (TF(term) + k1 × (1 - b + b × (|D| / avgDL)))

Where:
- IDF(term) = Inverse Document Frequency (rarity of term)
- TF(term) = Term Frequency (occurrences in document)
- |D| = Document length
- avgDL = Average document length across corpus
- k1 = 1.2 (term saturation parameter)
- b = 0.75 (length normalization parameter)
```

**Recency Boost Applied:**

```
final_score = bm25_score × recency_multiplier

recency_multiplier = {
  1.5  if age < 7 days
  1.0  if 7 days ≤ age ≤ 30 days
  0.8  if age > 30 days
}
```

This ensures recent conversations surface higher while still respecting relevance.

**Snippet Generation:**

Snippets provide context around matches:

1. **Find match positions:** FTS5 returns character offsets for each matching term
2. **Extract context window:** 150 characters before/after match (configurable)
3. **Highlight terms:** Wrap matching text in `<mark>` tags
4. **Truncate intelligently:** Break at word boundaries, add ellipsis
5. **Multiple snippets:** If multiple matches, show best snippet (highest term density)

**Example:**
```
Original: "We need to implement JWT token validation in the API gateway. The tokens should expire after 15 minutes and support refresh tokens for long-lived sessions."

Snippet: "...implement <mark>JWT</mark> token <mark>validation</mark> in the API gateway. The tokens..."
```

**Performance Characteristics:**

| Operation | SQLite FTS5 | PostgreSQL FTS | Notes |
|-----------|-------------|----------------|-------|
| Index 1 message | 5ms | 8ms | INSERT + trigger |
| Search 10k messages | 250ms | 300ms | Single term query |
| Search 100k messages | 800ms | 1.2s | Complex Boolean query |
| Index rebuild (10k) | 30s | 45s | Bulk reindex |
| Snippet generation | 25ms | 30ms | Per result |

**Integration Points:**

1. **Task-049a (Data Model):**
   - Index `Message.Content` field
   - Index `Chat.Title` field
   - Index `Chat.Tags` array

2. **Task-049b (CLI Commands):**
   - `acode search <query>` executes search
   - `acode search --rebuild` triggers reindex
   - Output formatting (table vs JSON)

3. **Task-030 (Search Infrastructure):**
   - Reuses ISearchService interface
   - Implements conversation-specific searcher

4. **Task-050 (Workspace Database):**
   - FTS5 extension enabled in SQLite
   - GIN index created in PostgreSQL

**Constraints and Limitations:**

1. **Term-based only:** No semantic/embedding search ("What did we discuss about auth?" requires keywords)
2. **English-centric:** Porter stemmer optimized for English (other languages need different stemmers)
3. **No fuzzy matching:** Typos don't match ("athentication" won't find "authentication")
4. **Index size:** ~30% overhead (10 MB messages → 13 MB with index)
5. **Rebuild time:** Full reindex takes O(n) time, blocking writes during rebuild
6. **Stop words:** Common words ("the", "a", "is") ignored, can't search for them
7. **SQLite limits:** FTS5 not efficient for >1 million documents (use PostgreSQL)

**Trade-offs and Alternatives:**

1. **SQLite FTS5 vs. External Search (Elasticsearch):**
   - **Chosen:** SQLite FTS5
   - **Alternative:** Elasticsearch, Meilisearch, Typesense
   - **Reason:** Zero-dependency local search, instant queries, no separate service

2. **Incremental vs. Batch Indexing:**
   - **Chosen:** Incremental (index each message on creation)
   - **Alternative:** Batch indexing (index every N messages or every M minutes)
   - **Reason:** Users expect search to work immediately, <10ms indexing overhead acceptable

3. **BM25 vs. TF-IDF:**
   - **Chosen:** BM25
   - **Alternative:** TF-IDF (older ranking algorithm)
   - **Reason:** BM25 handles document length better, industry standard for search

4. **Stemming vs. No Stemming:**
   - **Chosen:** Porter stemming enabled
   - **Alternative:** Exact term matching only
   - **Reason:** Users expect "authentication" to match "authenticate", "authenticating"

**Observability:**

- **Metrics:** Query latency (p50/p95/p99), index size, queries per second, cache hit rate
- **Logs:** Slow queries (>500ms), index updates, rebuild operations
- **Error Codes:** ACODE-SRCH-001 through ACODE-SRCH-005 for diagnosable failures

---

## Use Cases

### Use Case 1: DevBot - Finding Past Architectural Decisions

**Persona:** DevBot is a senior engineer working on microservices architecture. He often needs to recall decisions made 2-3 months ago during design discussions.

**Before (Manual Scrolling):**

DevBot remembers discussing "authentication" with the AI but can't recall the specific recommendation. He opens chat history and scrolls backward, reading every message.

```bash
acode chat list
# Shows 47 chats
# Which chat had the auth discussion? Was it "API Design" or "Security Review"?

acode chat open <auth-chat-maybe?>
# Scrolls through 180 messages
# "Was it JWT or OAuth2? I remember something about tokens..."
# Takes 15 minutes to find the relevant exchange
```

**Time spent:** 15 min per search × 8 searches/week = 120 min/week = **104 hours/year**

**After (Fast Search):**

```bash
acode search "authentication JWT decision"
# Returns in 150ms:
# [2] Auth Implementation (2025-11-10)
#     ...we decided on <mark>JWT</mark> tokens for <mark>authentication</mark>...
#     Score: 14.2
#
# [1] Security Review (2025-11-15)
#     ...confirmed <mark>JWT</mark> approach for API <mark>authentication</mark>...
#     Score: 12.8

acode search "JWT" --chat auth-chat --since 2025-11-01
# Narrows to 3 specific messages in auth chat
# Finds exact recommendation in 30 seconds
```

**Time spent:** 30 seconds per search

**Savings:** 14.5 min per search × 8 searches/week = 116 min/week = 100 hours/year @ $108/hour = **$10,800/year per engineer**

**Business Impact:** DevBot makes better decisions faster, avoids contradicting past choices, ships features 10% faster.

---

### Use Case 2: Jordan - Debugging with Historical Error Patterns

**Persona:** Jordan is a platform engineer responding to production incidents. She needs to quickly check if similar errors occurred before and how they were resolved.

**Before (Re-asking Questions):**

Production alert: "Database connection pool exhausted." Jordan doesn't remember if this happened before.

```bash
# Opens new chat
acode run "Have we seen database connection pool exhaustion before?"
# LLM responds: "I don't have context about previous incidents."

# Jordan manually searches Slack, Jira, documentation
# Takes 20 minutes to find that this happened 3 months ago
# Takes another 10 minutes to find the solution (increase pool size + add timeout)
```

**Time spent:** 30 min per incident × 4 incidents/month = 120 min/month = **24 hours/year**

**After (Search Historical Conversations):**

```bash
acode search "database connection pool exhausted"
# Returns in 200ms:
# [1] Incident 2025-09-12 (chat: incident-sep)
#     ...fixed by increasing pool size from 20 to 50...
#     ...also added 30-second connection timeout...
#     Score: 15.7

acode chat open <incident-sep-id>
acode run "Show me the solution for connection pool issue"
# LLM has full context from September incident
# Provides exact fix immediately
```

**Time spent:** 2 min per incident

**Savings:** 28 min per incident × 4 incidents/month = 112 min/month = 22.4 hours/year @ $108/hour = **$2,420/year per engineer**

**Business Impact:** Incidents resolved 93% faster (30 min → 2 min), MTTR reduced from 2 hours to 15 minutes, fewer escalations.

---

### Use Case 3: Alex - Code Review with Consistent Standards

**Persona:** Alex is a senior engineer reviewing pull requests. She wants to ensure code reviews apply consistent standards based on team conventions discussed with AI.

**Before (Inconsistent Standards):**

Alex reviews a PR with error handling. "Should we use exceptions or Result<T>?" She can't remember what the team decided.

```bash
# Opens old chats, tries to find discussion
acode chat list
# 68 chats, many about architecture

# Gives up searching, makes a guess
# Comments on PR: "Use exceptions for this case"

# Another engineer comments: "Actually we agreed on Result<T> pattern 2 months ago"
# Alex has to revise review comments, developer has to rework code
```

**Time wasted:** 10 min per inconsistent review × 12 reviews/month = 120 min/month = **24 hours/year**

**After (Search for Team Conventions):**

```bash
acode search "error handling exceptions Result" --since 2025-10-01
# Returns in 180ms:
# [1] Architecture Decisions (2025-10-15)
#     ...agreed on Result<T> pattern for domain layer...
#     ...exceptions only for infrastructure failures...
#     Score: 13.4

# Alex reads the context, applies correct standard
# Review comment: "Per our Oct 15 decision, use Result<T> here"
# Developer implements correctly the first time
```

**Time saved:** 120 min/month = 24 hours/year @ $108/hour = **$2,592/year per engineer**  
**Team impact:** 5 senior engineers × $2,592 = **$12,960/year team savings**

**Business Impact:** Consistent code quality, fewer rework cycles, faster PR turnaround (2 days → 1 day average).

---

## Security Considerations

### Threat 1: Search Query Injection (Boolean Operator Abuse)

**Risk:** Attacker crafts malicious FTS5 query with nested Boolean operators causing exponential query complexity, leading to denial of service (CPU exhaustion).

**Attack Scenario:**
```bash
# Attacker discovers search endpoint
acode search "(term1 OR term2) AND (term3 OR term4) AND (term5 OR term6) AND ...(repeat 20 times)"

# FTS5 evaluates all combinations: 2^20 = 1,048,576 term evaluations
# Query takes 30+ seconds, blocks database, exhausts CPU
# Concurrent attacks bring system down
```

**Mitigation (SafeQueryParser - 55 lines):**

```csharp
// src/AgenticCoder.Infrastructure/Search/SafeQueryParser.cs
namespace AgenticCoder.Infrastructure.Search;

public sealed class SafeQueryParser
{
    private readonly ILogger<SafeQueryParser> _logger;
    private const int MaxBooleanOperators = 5;
    private const int MaxQueryLength = 200;
    private const int MaxTerms = 10;

    public SafeQueryParser(ILogger<SafeQueryParser> logger)
    {
        _logger = logger;
    }

    public Result<string, Error> Parse(string userQuery)
    {
        // Validate query length
        if (userQuery.Length > MaxQueryLength)
        {
            _logger.LogWarning("Query too long: {Length} chars", userQuery.Length);
            return Result.Failure<string, Error>(
                new ValidationError($"Query must be ≤{MaxQueryLength} characters"));
        }

        // Count Boolean operators
        var booleanCount = Regex.Matches(userQuery, @"\b(AND|OR|NOT)\b", RegexOptions.IgnoreCase).Count;
        if (booleanCount > MaxBooleanOperators)
        {
            _logger.LogWarning("Too many Boolean operators: {Count}", booleanCount);
            return Result.Failure<string, Error>(
                new ValidationError($"Maximum {MaxBooleanOperators} Boolean operators allowed"));
        }

        // Count terms
        var terms = Regex.Matches(userQuery, @"\b\w+\b").Count;
        if (terms > MaxTerms)
        {
            _logger.LogWarning("Too many terms: {Count}", terms);
            return Result.Failure<string, Error>(
                new ValidationError($"Maximum {MaxTerms} search terms allowed"));
        }

        // Sanitize dangerous FTS5 syntax
        var sanitized = userQuery
            .Replace("^", "")  // Remove column filter operator
            .Replace("*", "")  // Remove prefix wildcard
            .Replace(":", ""); // Remove field specifier

        // Escape special characters
        sanitized = Regex.Replace(sanitized, @"[^\w\s\""()\-]", "");

        _logger.LogDebug("Query parsed: {Original} → {Sanitized}", userQuery, sanitized);

        return Result.Success<string, Error>(sanitized);
    }
}
```

---

### Threat 2: Result Set Exhaustion (Memory DoS)

**Risk:** Attacker requests search results without pagination limit, causing server to load millions of results into memory, exhausting RAM and crashing process.

**Attack Scenario:**
```bash
# Attacker crafts broad query matching everything
acode search "" --limit 999999999

# Server attempts to load 200,000 messages into memory
# Each message ~2 KB with snippet
# Total: 400 MB loaded, triggers OOM
```

**Mitigation (BoundedSearchResults - 45 lines):**

```csharp
// src/AgenticCoder.Application/Search/BoundedSearchResults.cs
namespace AgenticCoder.Application.Search;

public sealed class BoundedSearchResults
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;
    private const int MaxTotalResults = 1000;

    public static SearchQuery ApplyLimits(SearchQuery query)
    {
        // Enforce maximum page size
        var pageSize = query.PageSize;
        if (pageSize > MaxPageSize)
        {
            pageSize = MaxPageSize;
        }
        else if (pageSize <= 0)
        {
            pageSize = DefaultPageSize;
        }

        return query with { PageSize = pageSize };
    }

    public static async Task<SearchResults> ExecuteWithLimitAsync(
        ISearchService searchService,
        SearchQuery query,
        CancellationToken ct)
    {
        // Apply page size limit
        var boundedQuery = ApplyLimits(query);

        // Execute search with timeout
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        var results = await searchService.SearchAsync(boundedQuery, cts.Token);

        // Cap total results returned (even across pages)
        if (results.TotalCount > MaxTotalResults)
        {
            results = results with
            {
                TotalCount = MaxTotalResults,
                Message = $"Results limited to {MaxTotalResults}. Refine your query for more specific results."
            };
        }

        return results;
    }
}
```

---

### Threat 3: Sensitive Data Leakage in Snippets

**Risk:** Search snippets expose sensitive data (API keys, passwords, tokens) that appear in conversation history, even if chat is deleted or user loses access.

**Attack Scenario:**
```bash
# Developer accidentally pastes API key in chat
acode run "Here's the error with key: sk_live_abc123xyz..."

# Developer deletes the chat (soft delete)
# Attacker searches for "sk_live"
acode search "sk_live"
# Returns snippet: "...error with key: <mark>sk_live</mark>_abc123xyz..."
# API key exposed!
```

**Mitigation (RedactedSnippetGenerator - 60 lines):**

```csharp
// src/AgenticCoder.Infrastructure/Search/RedactedSnippetGenerator.cs
namespace AgenticCoder.Infrastructure.Search;

public sealed class RedactedSnippetGenerator
{
    private static readonly Regex[] SensitivePatterns = new[]
    {
        new Regex(@"\bsk_live_[a-zA-Z0-9]{24,}", RegexOptions.Compiled), // Stripe keys
        new Regex(@"\bgh[ps]_[a-zA-Z0-9]{36,}", RegexOptions.Compiled), // GitHub tokens
        new Regex(@"\bxox[baprs]-[a-zA-Z0-9-]{10,}", RegexOptions.Compiled), // Slack tokens
        new Regex(@"\bAKIA[A-Z0-9]{16}", RegexOptions.Compiled), // AWS keys
        new Regex(@"\beyJ[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+", RegexOptions.Compiled), // JWT
        new Regex(@"(?i)password[=:\s]+[^\s]{8,}", RegexOptions.Compiled), // Passwords
    };

    private readonly ILogger<RedactedSnippetGenerator> _logger;

    public RedactedSnippetGenerator(ILogger<RedactedSnippetGenerator> logger)
    {
        _logger = logger;
    }

    public string GenerateSnippet(string content, IEnumerable<int> matchOffsets)
    {
        // Generate base snippet around matches
        var snippet = ExtractContextWindow(content, matchOffsets);

        // Redact sensitive patterns
        foreach (var pattern in SensitivePatterns)
        {
            var matches = pattern.Matches(snippet);
            foreach (Match match in matches)
            {
                var redacted = match.Value.Substring(0, Math.Min(6, match.Value.Length)) + "***[REDACTED]";
                snippet = snippet.Replace(match.Value, redacted);

                _logger.LogWarning(
                    "Redacted sensitive data in snippet. Pattern: {Pattern}, Prefix: {Prefix}",
                    pattern.ToString().Substring(0, 20),
                    match.Value.Substring(0, Math.Min(6, match.Value.Length)));
            }
        }

        return snippet;
    }

    private string ExtractContextWindow(string content, IEnumerable<int> offsets)
    {
        // Implementation: Extract 150 chars around first match offset
        var firstOffset = offsets.FirstOrDefault();
        var start = Math.Max(0, firstOffset - 75);
        var length = Math.Min(150, content.Length - start);

        var window = content.Substring(start, length);
        return start > 0 ? "..." + window : window;
    }
}
```

---

### Threat 4: Index Poisoning (Malicious Content Injection)

**Risk:** Attacker injects messages with carefully crafted content designed to manipulate search rankings, pushing legitimate results down and promoting spam/phishing.

**Attack Scenario:**
```bash
# Attacker gains access to create messages
# Injects 1000 messages with keyword stuffing
for i in {1..1000}; do
  acode run "authentication authentication authentication [legitimate query terms repeated 50x]"
done

# User searches for "authentication"
acode search "authentication"
# Attacker's spam messages rank highest (high term frequency)
# Legitimate architectural discussions buried on page 10
```

**Mitigation (AntispamIndexFilter - 50 lines):**

```csharp
// src/AgenticCoder.Infrastructure/Search/AntispamIndexFilter.cs
namespace AgenticCoder.Infrastructure.Search;

public sealed class AntispamIndexFilter
{
    private const int MaxTermRepetition = 3;
    private const double MaxTermDensity = 0.05; // 5% of document

    private readonly ILogger<AntispamIndexFilter> _logger;

    public AntispamIndexFilter(ILogger<AntispamIndexFilter> logger)
    {
        _logger = logger;
    }

    public string FilterContent(string content)
    {
        // Tokenize content
        var tokens = content.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var termCounts = new Dictionary<string, int>();

        foreach (var token in tokens)
        {
            var normalized = token.ToLowerInvariant();
            termCounts[normalized] = termCounts.GetValueOrDefault(normalized) + 1;
        }

        // Detect keyword stuffing
        var filtered = new List<string>();
        var termOccurrences = new Dictionary<string, int>();

        foreach (var token in tokens)
        {
            var normalized = token.ToLowerInvariant();
            var currentCount = termOccurrences.GetValueOrDefault(normalized);
            var totalCount = termCounts[normalized];

            // Check term density
            var density = (double)totalCount / tokens.Length;
            if (density > MaxTermDensity && currentCount >= MaxTermRepetition)
            {
                // Skip this occurrence (spam detected)
                _logger.LogWarning(
                    "Keyword stuffing detected: {Term} appears {Count} times ({Density:P1} density)",
                    normalized, totalCount, density);
                continue;
            }

            filtered.Add(token);
            termOccurrences[normalized] = currentCount + 1;
        }

        return string.Join(" ", filtered);
    }
}
```

---

### Threat 5: Search Timing Attack (Information Disclosure)

**Risk:** Attacker uses search query timing to infer existence of sensitive terms in conversation history, even without access to chat content.

**Attack Scenario:**
```bash
# Attacker wants to know if team discussed "layoffs"
# Sends search query and measures response time
time acode search "layoffs"  # Returns in 50ms with 0 results
time acode search "authentication"  # Returns in 250ms with 47 results

# Attacker infers:
# - "layoffs" doesn't exist (fast query, empty result)
# - "authentication" exists heavily (slower query, many results)
# Information leaked via timing side channel
```

**Mitigation (ConstantTimeSearch - 45 lines):**

```csharp
// src/AgenticCoder.Infrastructure/Search/ConstantTimeSearch.cs
namespace AgenticCoder.Infrastructure.Search;

public sealed class ConstantTimeSearch
{
    private const int MinResponseTimeMs = 100;
    private readonly ISearchService _innerService;
    private readonly ILogger<ConstantTimeSearch> _logger;

    public ConstantTimeSearch(
        ISearchService innerService,
        ILogger<ConstantTimeSearch> logger)
    {
        _innerService = innerService;
        _logger = logger;
    }

    public async Task<SearchResults> SearchAsync(
        SearchQuery query,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        // Execute actual search
        var results = await _innerService.SearchAsync(query, ct);

        stopwatch.Stop();
        var elapsed = stopwatch.ElapsedMilliseconds;

        // Add artificial delay to reach minimum response time
        var delay = MinResponseTimeMs - (int)elapsed;
        if (delay > 0)
        {
            await Task.Delay(delay, ct);
            _logger.LogDebug(
                "Added {Delay}ms delay for constant-time search (actual: {Elapsed}ms)",
                delay, elapsed);
        }

        // Optionally add small random jitter to prevent statistical attacks
        var jitter = Random.Shared.Next(0, 20);
        await Task.Delay(jitter, ct);

        return results;
    }
}
```

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

- **NFR-001**: Index single message MUST complete in < 10ms (FTS5 INSERT with trigger)
- **NFR-002**: Search across 10,000 messages MUST return first page in < 500ms with simple term query
- **NFR-003**: Search across 100,000 messages MUST return first page in < 1,500ms with complex Boolean query
- **NFR-004**: Snippet generation (highlight + truncate) MUST complete in < 50ms per result
- **NFR-005**: Index rebuild of 10,000 messages MUST complete in < 60 seconds
- **NFR-006**: Query parsing and validation MUST complete in < 5ms
- **NFR-007**: Recency boost calculation MUST add < 1ms per result

### Accuracy

- **NFR-008**: No false negatives for exact term searches (100% recall for indexed terms)
- **NFR-009**: Relevant results MUST rank higher than less relevant results (BM25 validation)
- **NFR-010**: Stemmed searches (authenticate, authentication) MUST match all related forms
- **NFR-011**: Phrase queries with quotes MUST only match exact sequences
- **NFR-012**: Results MUST be consistent between SQLite FTS5 and PostgreSQL backends

### Reliability

- **NFR-013**: Index MUST survive application crash without data loss (WAL mode enabled)
- **NFR-014**: Index corruption MUST be auto-detected on startup with integrity check
- **NFR-015**: Corrupted index MUST trigger automatic rebuild from source data
- **NFR-016**: Index operations MUST be atomic (no partial updates)
- **NFR-017**: Failed index updates MUST NOT block write operations to messages table

### Scalability

- **NFR-018**: System MUST handle 100,000+ messages per workspace without degradation
- **NFR-019**: Index size MUST be < 30% of source content size (efficient compression)
- **NFR-020**: Query time MUST be sublinear O(log n) with proper indexing
- **NFR-021**: Memory usage during search MUST NOT exceed 100MB regardless of corpus size

### Security

- **NFR-022**: Query injection attempts MUST be sanitized (no FTS5 operator abuse)
- **NFR-023**: Result set size MUST be bounded to prevent memory exhaustion (max 1000 total)
- **NFR-024**: Sensitive data in snippets MUST be redacted (API keys, passwords, tokens)
- **NFR-025**: Search timing MUST be constant-time to prevent information disclosure

### Usability

- **NFR-026**: Empty results MUST provide helpful suggestions (spelling, broader terms)
- **NFR-027**: Match highlights MUST use visible formatting (Markdown bold or HTML mark)
- **NFR-028**: Search errors MUST include actionable guidance with error codes
- **NFR-029**: Pagination MUST support forward/backward navigation with cursors

### Maintainability

- **NFR-030**: All search operations MUST be logged at DEBUG level for troubleshooting
- **NFR-031**: Slow queries (>500ms) MUST be logged at WARNING level with query text
- **NFR-032**: Index status (size, message count, health) MUST be queryable via CLI

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

### Indexing - Content Capture

- [ ] AC-001: All message content indexed within 1 second of message creation
- [ ] AC-002: Chat titles indexed for title-based search
- [ ] AC-003: Chat tags indexed with prefix support
- [ ] AC-004: User prompts (role: user) indexed separately for targeted search
- [ ] AC-005: Assistant responses (role: assistant) indexed with role metadata
- [ ] AC-006: Tool call names indexed for debugging searches
- [ ] AC-007: Error messages indexed for troubleshooting
- [ ] AC-008: Message metadata (timestamp, chat_id) stored for filtering
- [ ] AC-009: Empty messages not indexed (no false matches)
- [ ] AC-010: Binary content excluded from index (images, files)

### Indexing - Backend Implementation

- [ ] AC-011: SQLite backend uses FTS5 virtual table for full-text search
- [ ] AC-012: FTS5 tokenizer configured for code-friendly tokenization (preserves underscores)
- [ ] AC-013: FTS5 porter stemmer enabled for English stemming
- [ ] AC-014: Postgres backend uses tsvector for full-text search
- [ ] AC-015: Postgres GIN index created on tsvector column
- [ ] AC-016: Backend abstraction allows switching without application changes
- [ ] AC-017: Index creation is idempotent (safe to re-run)
- [ ] AC-018: Index schema version tracked for migrations

### Indexing - Performance

- [ ] AC-019: Single message indexing completes in <10ms
- [ ] AC-020: Batch indexing of 100 messages completes in <1 second
- [ ] AC-021: Full index rebuild of 10,000 messages completes in <60 seconds
- [ ] AC-022: Index operations do not block concurrent read queries
- [ ] AC-023: Index size remains <30% of source content size
- [ ] AC-024: Memory usage during indexing stays under 100MB

### Search - Basic Queries

- [ ] AC-025: Single term search returns all messages containing that term
- [ ] AC-026: Multi-term search returns messages containing any term (OR default)
- [ ] AC-027: Phrase search with quotes returns exact phrase matches only
- [ ] AC-028: Case-insensitive search (searching "JWT" finds "jwt")
- [ ] AC-029: Stemmed search (searching "authenticate" finds "authenticated")
- [ ] AC-030: Search query parsed in <5ms before execution
- [ ] AC-031: Empty query returns error with helpful message

### Search - Boolean Operators

- [ ] AC-032: AND operator narrows results to messages with both terms
- [ ] AC-033: OR operator expands results to messages with either term
- [ ] AC-034: NOT operator excludes messages containing specified term
- [ ] AC-035: Parentheses group operators for complex queries
- [ ] AC-036: Maximum 5 Boolean operators per query enforced
- [ ] AC-037: Invalid Boolean syntax returns ACODE-SRCH-001 error

### Search - Field-Specific Queries

- [ ] AC-038: `role:user` searches only user prompts
- [ ] AC-039: `role:assistant` searches only assistant responses
- [ ] AC-040: `chat:name` searches within specific chat
- [ ] AC-041: `title:term` searches chat titles only
- [ ] AC-042: `tag:name` searches by tag
- [ ] AC-043: Multiple field prefixes can be combined in single query

### Ranking - Relevance

- [ ] AC-044: Results sorted by relevance score (highest first)
- [ ] AC-045: BM25 algorithm used for relevance calculation
- [ ] AC-046: Term frequency influences relevance (more occurrences = higher score)
- [ ] AC-047: Inverse document frequency influences relevance (rare terms weighted higher)
- [ ] AC-048: Title matches weighted 2x over body matches
- [ ] AC-049: Exact phrase matches weighted higher than term matches
- [ ] AC-050: Score calculation completes in <1ms per result

### Ranking - Recency Boost

- [ ] AC-051: Messages from last 24 hours receive 1.5x recency boost
- [ ] AC-052: Messages from last 7 days receive 1.2x recency boost
- [ ] AC-053: Messages older than 30 days receive no recency modification
- [ ] AC-054: Recency boost configurable via settings
- [ ] AC-055: Recency boost can be disabled entirely
- [ ] AC-056: Sort by date available as alternative to relevance sort

### Snippets - Generation

- [ ] AC-057: Each search result includes contextual snippet
- [ ] AC-058: Snippet length defaults to 150 characters
- [ ] AC-059: Snippet length configurable from 50-500 characters
- [ ] AC-060: Snippet centered around first matching term occurrence
- [ ] AC-061: Snippets preserve word boundaries (no mid-word truncation)
- [ ] AC-062: Snippet generation completes in <50ms per result

### Snippets - Highlighting

- [ ] AC-063: Matching terms wrapped in `<mark>` tags
- [ ] AC-064: Multiple matching terms all highlighted in same snippet
- [ ] AC-065: Highlight tags configurable for different output formats
- [ ] AC-066: CLI table output renders highlights as colored text
- [ ] AC-067: JSON output includes raw tags for client rendering
- [ ] AC-068: Sensitive content redacted before highlighting applied

### Filters - Chat Scope

- [ ] AC-069: `--chat <id>` limits search to specific chat
- [ ] AC-070: `--chat <name>` supports chat name lookup
- [ ] AC-071: Multiple `--chat` flags combine with OR logic
- [ ] AC-072: Invalid chat ID returns ACODE-CHAT-001 error
- [ ] AC-073: Archived chats excluded by default
- [ ] AC-074: `--include-archived` flag includes archived chats

### Filters - Date Range

- [ ] AC-075: `--since <date>` limits to messages after date
- [ ] AC-076: `--until <date>` limits to messages before date
- [ ] AC-077: `--since` and `--until` can be combined for date range
- [ ] AC-078: ISO 8601 date format accepted (2025-01-15)
- [ ] AC-079: Relative dates accepted ("7d", "2w", "1m" for days/weeks/months)
- [ ] AC-080: Invalid date format returns ACODE-SRCH-003 error

### Filters - Role

- [ ] AC-081: `--role user` limits to user messages only
- [ ] AC-082: `--role assistant` limits to assistant messages only
- [ ] AC-083: `--role system` limits to system prompts only
- [ ] AC-084: `--role tool` limits to tool calls and results only
- [ ] AC-085: Invalid role value returns ACODE-SRCH-004 error

### Filters - Combined

- [ ] AC-086: All filters can be combined in single query
- [ ] AC-087: Filters applied with AND logic (all must match)
- [ ] AC-088: Filter application order is deterministic
- [ ] AC-089: Empty result set from filters returns 0 results (not error)
- [ ] AC-090: Filter stats shown in verbose mode

### Index Maintenance - Incremental Updates

- [ ] AC-091: New messages indexed automatically on creation
- [ ] AC-092: Updated messages re-indexed on modification
- [ ] AC-093: Deleted messages removed from index
- [ ] AC-094: Deleted chats remove all associated message indexes
- [ ] AC-095: Index update queue processed within 1 second

### Index Maintenance - Optimization

- [ ] AC-096: `acode search index optimize` merges index segments
- [ ] AC-097: Optimization reduces segment count to 1
- [ ] AC-098: Optimization completes in <30 seconds for 50k messages
- [ ] AC-099: Optimization can run while searches continue
- [ ] AC-100: Optimization progress shown in CLI

### Index Maintenance - Rebuild

- [ ] AC-101: `acode search index rebuild` drops and recreates index
- [ ] AC-102: Rebuild reprocesses all messages from source tables
- [ ] AC-103: Rebuild completes in <60 seconds for 10k messages
- [ ] AC-104: Rebuild can be cancelled with Ctrl+C
- [ ] AC-105: Partial rebuild available for specific chats

### Index Maintenance - Status

- [ ] AC-106: `acode search index status` shows index health
- [ ] AC-107: Status includes: IndexedMessageCount, PendingCount, IndexSizeBytes
- [ ] AC-108: Status includes: LastOptimized timestamp, SegmentCount
- [ ] AC-109: Status returns "Healthy" or "Unhealthy" with reason
- [ ] AC-110: Status query completes in <100ms

### CLI - Search Command

- [ ] AC-111: `acode search <query>` executes search with default options
- [ ] AC-112: `acode search --help` shows all available options
- [ ] AC-113: `acode search` with no query shows error with usage help
- [ ] AC-114: Search results displayed in table format by default
- [ ] AC-115: Table shows: Score, Chat, Timestamp, Role, Snippet
- [ ] AC-116: `--json` flag outputs results as JSON array
- [ ] AC-117: `--page <n>` navigates to specific result page
- [ ] AC-118: `--page-size <n>` controls results per page (default 20)
- [ ] AC-119: `--verbose` shows query execution time and filter stats
- [ ] AC-120: Exit code 0 for success, non-zero for errors

### Error Handling

- [ ] AC-121: ACODE-SRCH-001 returned for invalid query syntax
- [ ] AC-122: ACODE-SRCH-002 returned for query timeout (>5 seconds)
- [ ] AC-123: ACODE-SRCH-003 returned for invalid date filter
- [ ] AC-124: ACODE-SRCH-004 returned for invalid role filter
- [ ] AC-125: ACODE-SRCH-005 returned for index corruption detected
- [ ] AC-126: ACODE-SRCH-006 returned for index not initialized
- [ ] AC-127: All errors include actionable remediation guidance

### Performance SLAs

- [ ] AC-128: Search of 10,000 messages completes in <500ms (p95)
- [ ] AC-129: Search of 100,000 messages completes in <1.5 seconds (p95)
- [ ] AC-130: Concurrent searches (10) don't degrade individual query time >20%
- [ ] AC-131: Memory usage during search stays under 100MB
- [ ] AC-132: Index operations don't block search queries

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

### Issue 1: Index Corruption

**Symptom:** Search returns errors like "FTS5 index corrupt" or returns incorrect/inconsistent results. Same query returns different results on repeated execution.

**Causes:**
- Application crash during index write operation
- Disk errors or filesystem corruption
- Database file modified externally while in use
- Power failure during transaction

**Solution:**
1. Check index integrity: `acode search index status` - look for "Unhealthy" status
2. Rebuild index from source data: `acode search index rebuild`
3. Check for disk errors: `chkdsk /f` (Windows) or `fsck` (Unix)
4. Review crash logs at `.agent/logs/acode.log` for root cause
5. If recurring, check SQLite WAL mode is enabled: `PRAGMA journal_mode;` should return "wal"

---

### Issue 2: Search Performance Degraded

**Symptom:** Searches that previously returned in <500ms now take 2-5+ seconds. CLI hangs for extended periods during search.

**Causes:**
- Index fragmentation (many segments due to incremental updates)
- Query too broad (single common term matching thousands of results)
- Database connection pool exhaustion
- Disk I/O bottleneck

**Solution:**
1. Run index optimization: `acode search index optimize` - merges segments
2. Add more specific terms to narrow query
3. Use filters to constrain search: `--chat`, `--since`, `--role`
4. Check index status: `acode search index status` - look for segment count
5. If many segments (>10), optimization needed
6. Check database statistics: `sqlite3 .agent/data/workspace.db "ANALYZE;"`

---

### Issue 3: Results Missing Known Content

**Symptom:** Search doesn't find messages you know exist. Recently added messages don't appear in results.

**Causes:**
- Content not indexed yet (async indexing delay)
- Stop words removed (searching for "the", "a", "is")
- Stemming mismatch (search term doesn't stem to indexed form)
- Content was redacted for security reasons

**Solution:**
1. Wait 1-2 seconds for async index update to complete
2. Check pending index count: `acode search index status` - look for "Pending" > 0
3. Try different search terms - avoid common words (stop words)
4. Try base form of word (authenticate instead of authenticated)
5. Check if content contains sensitive patterns (API keys) which are redacted
6. Force reindex: `acode search index rebuild`

---

### Issue 4: Query Syntax Error

**Symptom:** Search returns error "Invalid query syntax" or ACODE-SRCH-001. Boolean operators seem to be ignored.

**Causes:**
- Unbalanced quotes in phrase query
- Invalid Boolean operator usage (AND at start)
- Too many Boolean operators (>5)
- Special characters not escaped

**Solution:**
1. For phrase search, ensure quotes are balanced: `"JWT token"`
2. Don't start query with AND/OR: use `term1 AND term2` not `AND term1 term2`
3. Simplify complex queries - limit to 5 Boolean operators
4. Escape or remove special characters: `^`, `*`, `:`
5. Use simple space-separated terms - implicit OR by default

---

### Issue 5: Snippet Highlights Missing

**Symptom:** Search returns results but snippets don't show highlighted matching terms. Results show raw text without `<mark>` tags.

**Causes:**
- Highlight tags stripped by output formatter
- JSON output mode doesn't render HTML
- Terminal doesn't support ANSI colors
- Snippet generator not returning offsets

**Solution:**
1. Check output format - table mode should show highlights
2. JSON mode outputs raw HTML tags, use `--json` to see actual markup
3. Verify terminal supports ANSI: most modern terminals do
4. Try different terminal (Windows Terminal, iTerm2, etc.)
5. Check configuration: `search.snippets.highlight_tag` setting

---

### Issue 6: Out of Memory During Large Search

**Symptom:** `acode search` crashes with out-of-memory error or becomes extremely slow when searching broad queries.

**Causes:**
- Query matches too many results (millions of terms)
- Page size set too high
- Result set unbounded
- Snippet generation loading full content

**Solution:**
1. Add filters to narrow results: `--since`, `--chat`, `--role`
2. Use more specific search terms
3. Reduce page size: `--page-size 20` (default)
4. Check max results limit in config: `search.max_total_results`
5. For very large datasets, consider paginating through results

---

### Issue 7: Search Returns Zero Results for Valid Query

**Symptom:** Search returns "0 results" even though messages contain the exact search terms.

**Causes:**
- Index is empty (never populated)
- Message table and index out of sync
- Chat or date filter excluding all results
- Case sensitivity issue (FTS is case-insensitive, but exact match mode isn't)

**Solution:**
1. Check index has content: `acode search index status`
2. If IndexedMessageCount is 0, run: `acode search index rebuild`
3. Remove filters and try simple query first
4. Verify message exists: `acode chat show <id>` - check message count
5. Try lowercase query (FTS is case-insensitive)
6. Check if messages are in archived chats (default excluded)

---

### Issue 8: Index Rebuild Takes Too Long

**Symptom:** `acode search index rebuild` runs for extended periods (>5 minutes for 10k messages). Progress bar stuck.

**Causes:**
- Very large message corpus (100k+ messages)
- Slow disk I/O
- Database connection bottleneck
- Indexing competing with other operations

**Solution:**
1. Index rebuild is O(n) - 10k messages should take ~30-60s
2. Check disk I/O: slow HDD vs SSD makes significant difference
3. Close other applications accessing the database
4. Run during low-activity period
5. Consider partial rebuild: delete and recreate specific chat's index only
6. If consistently slow, check database vacuum: `sqlite3 workspace.db "VACUUM;"`

---

## Testing Requirements

### Unit Tests - IndexerTests.cs

```csharp
// tests/Acode.Infrastructure.Tests/Search/IndexerTests.cs
namespace Acode.Infrastructure.Tests.Search;

public class IndexerTests
{
    [Fact]
    public async Task Should_Tokenize_Content_With_Porter_Stemmer()
    {
        // Arrange
        var logger = Substitute.For<ILogger<SqliteFtsIndexer>>();
        var connection = CreateInMemorySqliteConnection();
        await InitializeFts5Table(connection);
        
        var indexer = new SqliteFtsIndexer(connection, logger);
        var message = new Message
        {
            Id = Guid.NewGuid(),
            Content = "Authentication authenticates the authenticated users",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await indexer.IndexMessageAsync(message, CancellationToken.None);

        // Assert - All stems of "authenticate" should match
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM conversation_search WHERE conversation_search MATCH 'authenticate'";
        var count = (long)cmd.ExecuteScalar()!;
        
        Assert.Equal(1, count); // Porter stemmer normalizes all forms to "authenticate"
    }

    [Fact]
    public async Task Should_Filter_Stop_Words()
    {
        // Arrange
        var logger = Substitute.For<ILogger<SqliteFtsIndexer>>();
        var connection = CreateInMemorySqliteConnection();
        await InitializeFts5Table(connection);
        
        var indexer = new SqliteFtsIndexer(connection, logger);
        var message = new Message
        {
            Id = Guid.NewGuid(),
            Content = "the quick brown fox jumps over the lazy dog",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await indexer.IndexMessageAsync(message, CancellationToken.None);

        // Assert - Stop words "the", "over" not indexed
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM conversation_search WHERE conversation_search MATCH 'the'";
        var count = (long)cmd.ExecuteScalar()!;
        
        Assert.Equal(0, count); // "the" is a stop word, not indexed
        
        // But content words are indexed
        cmd.CommandText = "SELECT COUNT(*) FROM conversation_search WHERE conversation_search MATCH 'quick'";
        count = (long)cmd.ExecuteScalar()!;
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Should_Handle_CamelCase_Tokenization()
    {
        // Arrange
        var logger = Substitute.For<ILogger<SqliteFtsIndexer>>();
        var connection = CreateInMemorySqliteConnection();
        await InitializeFts5Table(connection);
        
        var indexer = new SqliteFtsIndexer(connection, logger);
        var message = new Message
        {
            Id = Guid.NewGuid(),
            Content = "Use GetUserProfile for UserAuthentication",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await indexer.IndexMessageAsync(message, CancellationToken.None);

        // Assert - CamelCase should be searchable as separate terms
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM conversation_search WHERE conversation_search MATCH 'User'";
        var count = (long)cmd.ExecuteScalar()!;
        
        Assert.True(count >= 1); // "User" appears in both compound words
    }

    [Fact]
    public async Task Should_Index_Incrementally_Without_Blocking()
    {
        // Arrange
        var logger = Substitute.For<ILogger<SqliteFtsIndexer>>();
        var connection = CreateInMemorySqliteConnection();
        await InitializeFts5Table(connection);
        
        var indexer = new SqliteFtsIndexer(connection, logger);
        var messages = Enumerable.Range(1, 100).Select(i => new Message
        {
            Id = Guid.NewGuid(),
            Content = $"Message {i} with content",
            CreatedAt = DateTime.UtcNow
        }).ToList();

        // Act - Index 100 messages
        var stopwatch = Stopwatch.StartNew();
        foreach (var message in messages)
        {
            await indexer.IndexMessageAsync(message, CancellationToken.None);
        }
        stopwatch.Stop();

        // Assert - Should complete within reasonable time (< 1 second for 100 messages)
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"Indexing took {stopwatch.ElapsedMilliseconds}ms, should be <1000ms");
        
        // Verify all indexed
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM conversation_search";
        var count = (long)cmd.ExecuteScalar()!;
        Assert.Equal(100, count);
    }

    [Fact]
    public async Task Should_Update_Index_When_Message_Updated()
    {
        // Arrange
        var logger = Substitute.For<ILogger<SqliteFtsIndexer>>();
        var connection = CreateInMemorySqliteConnection();
        await InitializeFts5Table(connection);
        
        var indexer = new SqliteFtsIndexer(connection, logger);
        var messageId = Guid.NewGuid();
        var originalMessage = new Message
        {
            Id = messageId,
            Content = "Original authentication content",
            CreatedAt = DateTime.UtcNow
        };

        // Act - Index original
        await indexer.IndexMessageAsync(originalMessage, CancellationToken.None);
        
        // Update message
        var updatedMessage = originalMessage with { Content = "Updated authorization content" };
        await indexer.UpdateMessageIndexAsync(updatedMessage, CancellationToken.None);

        // Assert - Old term not found
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM conversation_search WHERE conversation_search MATCH 'authentication'";
        var count = (long)cmd.ExecuteScalar()!;
        Assert.Equal(0, count);
        
        // New term found
        cmd.CommandText = "SELECT COUNT(*) FROM conversation_search WHERE conversation_search MATCH 'authorization'";
        count = (long)cmd.ExecuteScalar()!;
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Should_Remove_From_Index_When_Message_Deleted()
    {
        // Arrange
        var logger = Substitute.For<ILogger<SqliteFtsIndexer>>();
        var connection = CreateInMemorySqliteConnection();
        await InitializeFts5Table(connection);
        
        var indexer = new SqliteFtsIndexer(connection, logger);
        var messageId = Guid.NewGuid();
        var message = new Message
        {
            Id = messageId,
            Content = "Content to be deleted",
            CreatedAt = DateTime.UtcNow
        };

        // Act - Index and then delete
        await indexer.IndexMessageAsync(message, CancellationToken.None);
        await indexer.RemoveFromIndexAsync(messageId, CancellationToken.None);

        // Assert
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM conversation_search WHERE message_id = ?";
        cmd.Parameters.AddWithValue("@p0", messageId.ToString());
        var count = (long)cmd.ExecuteScalar()!;
        Assert.Equal(0, count);
    }

    private static SqliteConnection CreateInMemorySqliteConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    private static async Task InitializeFts5Table(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE VIRTUAL TABLE conversation_search USING fts5(
                message_id UNINDEXED,
                chat_id UNINDEXED,
                created_at UNINDEXED,
                role UNINDEXED,
                content,
                chat_title,
                tags,
                tokenize='porter unicode61'
            );";
        await cmd.ExecuteNonQueryAsync();
    }
}
```

### Unit Tests - QueryParserTests.cs

```csharp
// tests/Acode.Application.Tests/Search/QueryParserTests.cs
namespace Acode.Application.Tests.Search;

public class QueryParserTests
{
    [Fact]
    public void Should_Parse_Simple_Terms()
    {
        // Arrange
        var logger = Substitute.For<ILogger<SafeQueryParser>>();
        var parser = new SafeQueryParser(logger);
        var userQuery = "authentication JWT";

        // Act
        var result = parser.Parse(userQuery);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("authentication", result.Value);
        Assert.Contains("JWT", result.Value);
    }

    [Fact]
    public void Should_Parse_Phrase_Queries_With_Quotes()
    {
        // Arrange
        var logger = Substitute.For<ILogger<SafeQueryParser>>();
        var parser = new SafeQueryParser(logger);
        var userQuery = "\"JWT token validation\"";

        // Act
        var result = parser.Parse(userQuery);

        // Assert
        Assert.True(result.IsSuccess);
        // Quotes preserved for FTS5 phrase matching
        Assert.Contains("JWT token validation", result.Value);
    }

    [Fact]
    public void Should_Parse_Boolean_Operators()
    {
        // Arrange
        var logger = Substitute.For<ILogger<SafeQueryParser>>();
        var parser = new SafeQueryParser(logger);
        var userQuery = "authentication AND JWT OR OAuth";

        // Act
        var result = parser.Parse(userQuery);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("AND", result.Value);
        Assert.Contains("OR", result.Value);
    }

    [Fact]
    public void Should_Reject_Query_Exceeding_Max_Length()
    {
        // Arrange
        var logger = Substitute.For<ILogger<SafeQueryParser>>();
        var parser = new SafeQueryParser(logger);
        var userQuery = new string('a', 201); // Exceeds 200 char limit

        // Act
        var result = parser.Parse(userQuery);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<ValidationError>(result.Error);
        Assert.Contains("200 characters", result.Error.Message);
    }

    [Fact]
    public void Should_Reject_Excessive_Boolean_Operators()
    {
        // Arrange
        var logger = Substitute.For<ILogger<SafeQueryParser>>();
        var parser = new SafeQueryParser(logger);
        var userQuery = "a AND b AND c AND d AND e AND f"; // 6 operators, max is 5

        // Act
        var result = parser.Parse(userQuery);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<ValidationError>(result.Error);
        Assert.Contains("5 Boolean operators", result.Error.Message);
    }

    [Fact]
    public void Should_Reject_Excessive_Terms()
    {
        // Arrange
        var logger = Substitute.For<ILogger<SafeQueryParser>>();
        var parser = new SafeQueryParser(logger);
        var userQuery = "term1 term2 term3 term4 term5 term6 term7 term8 term9 term10 term11"; // 11 terms, max 10

        // Act
        var result = parser.Parse(userQuery);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<ValidationError>(result.Error);
        Assert.Contains("10 search terms", result.Error.Message);
    }

    [Fact]
    public void Should_Sanitize_Dangerous_FTS5_Syntax()
    {
        // Arrange
        var logger = Substitute.For<ILogger<SafeQueryParser>>();
        var parser = new SafeQueryParser(logger);
        var userQuery = "term^2 content:field*";

        // Act
        var result = parser.Parse(userQuery);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.DoesNotContain("^", result.Value); // Column filter removed
        Assert.DoesNotContain("*", result.Value); // Wildcard removed
        Assert.DoesNotContain(":", result.Value); // Field specifier removed
    }

    [Theory]
    [InlineData("auth", "auth")]
    [InlineData("auth AND jwt", "auth AND jwt")]
    [InlineData("(auth OR oauth) AND token", "auth OR oauth AND token")]
    public void Should_Preserve_Valid_Query_Syntax(string input, string expectedPattern)
    {
        // Arrange
        var logger = Substitute.For<ILogger<SafeQueryParser>>();
        var parser = new SafeQueryParser(logger);

        // Act
        var result = parser.Parse(input);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains(expectedPattern.Replace("(", "").Replace(")", ""), result.Value);
    }
}
```

### Unit Tests - RankerTests.cs

```csharp
// tests/Acode.Application.Tests/Search/RankerTests.cs
namespace Acode.Application.Tests.Search;

public class RankerTests
{
    [Fact]
    public void Should_Calculate_BM25_Score_Based_On_Term_Frequency()
    {
        // Arrange
        var ranker = new BM25Ranker(k1: 1.2, b: 0.75);
        var documents = new List<SearchDocument>
        {
            new() { Id = "1", Content = "JWT token validation", Length = 3 },
            new() { Id = "2", Content = "JWT JWT JWT", Length = 3 }, // High term frequency
            new() { Id = "3", Content = "authentication method", Length = 2 }
        };
        var query = new[] { "JWT" };

        // Act
        var scores = documents.Select(doc => ranker.Score(doc, query)).ToList();

        // Assert
        // Document 2 should score highest (3 occurrences of JWT)
        Assert.True(scores[1] > scores[0]);
        // Document 3 should score 0 (no match)
        Assert.Equal(0, scores[2]);
    }

    [Fact]
    public void Should_Apply_Recency_Boost_To_Recent_Messages()
    {
        // Arrange
        var ranker = new BM25Ranker(k1: 1.2, b: 0.75);
        var now = DateTime.UtcNow;
        var recentMessage = new SearchResult
        {
            MessageId = Guid.NewGuid(),
            CreatedAt = now.AddDays(-3), // 3 days ago
            BaseScore = 10.0
        };
        var oldMessage = new SearchResult
        {
            MessageId = Guid.NewGuid(),
            CreatedAt = now.AddDays(-90), // 90 days ago
            BaseScore = 10.0
        };

        // Act
        var recentFinalScore = ranker.ApplyRecencyBoost(recentMessage);
        var oldFinalScore = ranker.ApplyRecencyBoost(oldMessage);

        // Assert
        // Recent message gets 1.5x boost (< 7 days)
        Assert.Equal(15.0, recentFinalScore, precision: 1);
        // Old message gets 0.8x penalty (> 30 days)
        Assert.Equal(8.0, oldFinalScore, precision: 1);
    }

    [Fact]
    public void Should_Normalize_Scores_By_Document_Length()
    {
        // Arrange
        var ranker = new BM25Ranker(k1: 1.2, b: 0.75);
        var shortDoc = new SearchDocument { Id = "1", Content = "JWT", Length = 1 };
        var longDoc = new SearchDocument { Id = "2", Content = "JWT " + new string('x', 100), Length = 101 };
        var query = new[] { "JWT" };

        // Act
        var shortScore = ranker.Score(shortDoc, query);
        var longScore = ranker.Score(longDoc, query);

        // Assert
        // Short document should score higher (term appears in smaller context)
        Assert.True(shortScore > longScore);
    }

    [Fact]
    public void Should_Calculate_IDF_For_Rare_Terms()
    {
        // Arrange
        var corpus = new List<string>
        {
            "common common common",
            "common common rare",
            "common common",
            "common rare",
            "common"
        };
        var ranker = new BM25Ranker(corpus);

        // Act
        var commonIDF = ranker.CalculateIDF("common"); // Appears in all 5 docs
        var rareIDF = ranker.CalculateIDF("rare");     // Appears in only 2 docs

        // Assert
        // Rare terms have higher IDF
        Assert.True(rareIDF > commonIDF);
        // Common term should have low IDF (appears everywhere)
        Assert.True(commonIDF < 1.0);
    }

    [Fact]
    public void Should_Rank_Title_Matches_Higher_Than_Content()
    {
        // Arrange
        var ranker = new BM25Ranker(titleBoost: 2.0);
        var titleMatch = new SearchResult
        {
            MessageId = Guid.NewGuid(),
            MatchLocation = MatchLocation.Title,
            BaseScore = 10.0
        };
        var contentMatch = new SearchResult
        {
            MessageId = Guid.NewGuid(),
            MatchLocation = MatchLocation.Content,
            BaseScore = 10.0
        };

        // Act
        var titleScore = ranker.ApplyLocationBoost(titleMatch);
        var contentScore = ranker.ApplyLocationBoost(contentMatch);

        // Assert
        // Title matches boosted 2x
        Assert.Equal(20.0, titleScore);
        Assert.Equal(10.0, contentScore);
    }
}
```

### Unit Tests - SnippetTests.cs

```csharp
// tests/Acode.Application.Tests/Search/SnippetTests.cs
namespace Acode.Application.Tests.Search;

public class SnippetTests
{
    [Fact]
    public void Should_Generate_Snippet_With_Context_Window()
    {
        // Arrange
        var generator = new SnippetGenerator(maxLength: 150);
        var content = "The quick brown fox jumps over the lazy dog. The authentication system uses JWT tokens for validation.";
        var matchOffsets = new[] { 50 }; // Position of "authentication"

        // Act
        var snippet = generator.Generate(content, matchOffsets);

        // Assert
        Assert.Contains("authentication", snippet);
        Assert.Contains("JWT", snippet); // Context after match
        Assert.True(snippet.Length <= 150);
    }

    [Fact]
    public void Should_Highlight_Matching_Terms()
    {
        // Arrange
        var generator = new SnippetGenerator(highlightTag: "<mark>");
        var content = "Use JWT authentication for API security";
        var matchOffsets = new[] { 4, 8 }; // Positions of "JWT" and "authentication"

        // Act
        var snippet = generator.Generate(content, matchOffsets);

        // Assert
        Assert.Contains("<mark>JWT</mark>", snippet);
        Assert.Contains("<mark>authentication</mark>", snippet);
    }

    [Fact]
    public void Should_Truncate_At_Word_Boundaries()
    {
        // Arrange
        var generator = new SnippetGenerator(maxLength: 50);
        var content = "This is a very long sentence that should be truncated intelligently at word boundaries";
        var matchOffsets = new[] { 10 }; // Position of "very"

        // Act
        var snippet = generator.Generate(content, matchOffsets);

        // Assert
        Assert.True(snippet.Length <= 50);
        Assert.DoesNotContain("trunca", snippet); // Should not cut mid-word
        Assert.True(snippet.EndsWith("...") || !content.StartsWith(snippet));
    }

    [Fact]
    public void Should_Generate_Multiple_Snippets_For_Multiple_Matches()
    {
        // Arrange
        var generator = new SnippetGenerator(maxSnippets: 3);
        var content = "JWT at position 0. Authentication at position 50. Security at position 100.";
        var matchOffsets = new[] { 0, 50, 100 };

        // Act
        var snippets = generator.GenerateMultiple(content, matchOffsets);

        // Assert
        Assert.Equal(3, snippets.Count);
        Assert.Contains("JWT", snippets[0]);
        Assert.Contains("Authentication", snippets[1]);
        Assert.Contains("Security", snippets[2]);
    }

    [Fact]
    public void Should_Select_Best_Snippet_By_Term_Density()
    {
        // Arrange
        var generator = new SnippetGenerator();
        var content = "Introduction text. JWT authentication validation security tokens JWT. Conclusion text.";
        var matchOffsets = new[] { 20, 24, 38, 47, 54, 61 }; // High density in middle

        // Act
        var bestSnippet = generator.GenerateBest(content, matchOffsets);

        // Assert
        Assert.Contains("JWT", bestSnippet);
        Assert.Contains("authentication", bestSnippet);
        Assert.Contains("security", bestSnippet);
        Assert.DoesNotContain("Introduction", bestSnippet);
        Assert.DoesNotContain("Conclusion", bestSnippet);
    }

    [Fact]
    public void Should_Add_Ellipsis_For_Truncated_Content()
    {
        // Arrange
        var generator = new SnippetGenerator(maxLength: 50);
        var content = new string('x', 200);
        var matchOffsets = new[] { 100 }; // Match in middle

        // Act
        var snippet = generator.Generate(content, matchOffsets);

        // Assert
        Assert.StartsWith("...", snippet);
        Assert.EndsWith("...", snippet);
    }
}
```

### Integration Tests - SqliteFtsTests.cs

```csharp
// tests/Acode.Integration.Tests/Search/SqliteFtsTests.cs
namespace Acode.Integration.Tests.Search;

public class SqliteFtsTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private SqliteFtsSearchService _searchService = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        
        // Initialize schema
        await CreateMessagesTable();
        await CreateFts5Index();
        
        var logger = Substitute.For<ILogger<SqliteFtsSearchService>>();
        _searchService = new SqliteFtsSearchService(_connection, logger);
    }

    [Fact]
    public async Task Should_Index_And_Search_Messages()
    {
        // Arrange - Insert test messages
        await InsertMessage(Guid.NewGuid(), "JWT authentication implementation");
        await InsertMessage(Guid.NewGuid(), "OAuth2 authorization flow");
        await InsertMessage(Guid.NewGuid(), "JWT token validation");

        // Act
        var query = new SearchQuery
        {
            QueryText = "JWT",
            PageSize = 10
        };
        var results = await _searchService.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, results.TotalCount); // Two messages contain "JWT"
        Assert.All(results.Results, r => Assert.Contains("JWT", r.Snippet));
    }

    [Fact]
    public async Task Should_Handle_Large_Corpus_Efficiently()
    {
        // Arrange - Insert 10,000 messages
        for (int i = 0; i < 10000; i++)
        {
            await InsertMessage(Guid.NewGuid(), $"Message {i} with content about topic {i % 100}");
        }

        // Act
        var stopwatch = Stopwatch.StartNew();
        var query = new SearchQuery { QueryText = "topic 42", PageSize = 20 };
        var results = await _searchService.SearchAsync(query, CancellationToken.None);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 500, $"Search took {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(results.TotalCount >= 100); // ~100 messages mention "topic 42"
    }

    [Fact]
    public async Task Should_Support_Phrase_Queries()
    {
        // Arrange
        await InsertMessage(Guid.NewGuid(), "JWT token validation is important");
        await InsertMessage(Guid.NewGuid(), "JWT and token and validation separately");

        // Act
        var query = new SearchQuery { QueryText = "\"JWT token validation\"", PageSize = 10 };
        var results = await _searchService.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.Equal(1, results.TotalCount); // Only exact phrase matches
    }

    [Fact]
    public async Task Should_Filter_By_Chat_Id()
    {
        // Arrange
        var chatId1 = Guid.NewGuid();
        var chatId2 = Guid.NewGuid();
        await InsertMessage(Guid.NewGuid(), "JWT authentication", chatId1);
        await InsertMessage(Guid.NewGuid(), "JWT validation", chatId2);

        // Act
        var query = new SearchQuery
        {
            QueryText = "JWT",
            ChatId = chatId1,
            PageSize = 10
        };
        var results = await _searchService.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.Equal(1, results.TotalCount);
        Assert.Equal(chatId1, results.Results[0].ChatId);
    }

    [Fact]
    public async Task Should_Filter_By_Date_Range()
    {
        // Arrange
        var now = DateTime.UtcNow;
        await InsertMessage(Guid.NewGuid(), "Recent message", createdAt: now.AddDays(-1));
        await InsertMessage(Guid.NewGuid(), "Old message", createdAt: now.AddDays(-90));

        // Act
        var query = new SearchQuery
        {
            QueryText = "message",
            Since = now.AddDays(-7),
            PageSize = 10
        };
        var results = await _searchService.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.Equal(1, results.TotalCount); // Only recent message
    }

    private async Task CreateMessagesTable()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE messages (
                id TEXT PRIMARY KEY,
                chat_id TEXT NOT NULL,
                content TEXT NOT NULL,
                created_at TEXT NOT NULL
            );";
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task CreateFts5Index()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE VIRTUAL TABLE conversation_search USING fts5(
                message_id UNINDEXED,
                chat_id UNINDEXED,
                created_at UNINDEXED,
                content,
                tokenize='porter unicode61',
                content='messages',
                content_rowid='rowid'
            );";
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task InsertMessage(Guid id, string content, Guid? chatId = null, DateTime? createdAt = null)
    {
        chatId ??= Guid.NewGuid();
        createdAt ??= DateTime.UtcNow;
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO messages (id, chat_id, content, created_at)
            VALUES (?, ?, ?, ?);
            INSERT INTO conversation_search (message_id, chat_id, created_at, content)
            VALUES (?, ?, ?, ?);";
        cmd.Parameters.AddWithValue("@p0", id.ToString());
        cmd.Parameters.AddWithValue("@p1", chatId.ToString());
        cmd.Parameters.AddWithValue("@p2", content);
        cmd.Parameters.AddWithValue("@p3", createdAt.Value.ToString("O"));
        cmd.Parameters.AddWithValue("@p4", id.ToString());
        cmd.Parameters.AddWithValue("@p5", chatId.ToString());
        cmd.Parameters.AddWithValue("@p6", createdAt.Value.ToString("O"));
        cmd.Parameters.AddWithValue("@p7", content);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
```

### End-to-End Tests - SearchE2ETests.cs

```csharp
// tests/Acode.Integration.Tests/E2E/SearchE2ETests.cs
namespace Acode.Integration.Tests.E2E;

public class SearchE2ETests : IAsyncLifetime
{
    private TestApplication _app = null!;
    private IMessageRepository _messageRepo = null!;
    private ISearchService _searchService = null!;

    public async Task InitializeAsync()
    {
        _app = await TestApplication.CreateAsync();
        _messageRepo = _app.Services.GetRequiredService<IMessageRepository>();
        _searchService = _app.Services.GetRequiredService<ISearchService>();
    }

    [Fact]
    public async Task Should_Find_Messages_End_To_End()
    {
        // Arrange - Create conversation through full application stack
        var chatId = Guid.NewGuid();
        await _messageRepo.CreateAsync(new Message
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            Role = MessageRole.User,
            Content = "How do I implement JWT authentication?",
            CreatedAt = DateTime.UtcNow
        });
        await _messageRepo.CreateAsync(new Message
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            Role = MessageRole.Assistant,
            Content = "JWT authentication requires token validation and expiration checks.",
            CreatedAt = DateTime.UtcNow
        });

        // Wait for async indexing
        await Task.Delay(100);

        // Act - Search through application
        var query = new SearchQuery { QueryText = "JWT authentication", PageSize = 10 };
        var results = await _searchService.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, results.TotalCount);
        Assert.Contains(results.Results, r => r.Role == MessageRole.User);
        Assert.Contains(results.Results, r => r.Role == MessageRole.Assistant);
    }

    [Fact]
    public async Task Should_Rank_Results_By_Relevance()
    {
        // Arrange
        var chatId = Guid.NewGuid();
        await _messageRepo.CreateAsync(new Message
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            Content = "JWT JWT JWT authentication", // High term frequency
            CreatedAt = DateTime.UtcNow
        });
        await _messageRepo.CreateAsync(new Message
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            Content = "JWT mentioned once",
            CreatedAt = DateTime.UtcNow
        });

        await Task.Delay(100);

        // Act
        var query = new SearchQuery { QueryText = "JWT", PageSize = 10 };
        var results = await _searchService.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, results.TotalCount);
        // First result should have higher term frequency
        Assert.Contains("JWT JWT JWT", results.Results[0].Snippet);
    }

    [Fact]
    public async Task Should_Filter_By_Chat_And_Date()
    {
        // Arrange
        var targetChatId = Guid.NewGuid();
        var otherChatId = Guid.NewGuid();
        var recentDate = DateTime.UtcNow.AddDays(-1);
        var oldDate = DateTime.UtcNow.AddDays(-90);

        await _messageRepo.CreateAsync(new Message
        {
            Id = Guid.NewGuid(),
            ChatId = targetChatId,
            Content = "Recent authentication discussion",
            CreatedAt = recentDate
        });
        await _messageRepo.CreateAsync(new Message
        {
            Id = Guid.NewGuid(),
            ChatId = otherChatId,
            Content = "Different chat authentication",
            CreatedAt = recentDate
        });
        await _messageRepo.CreateAsync(new Message
        {
            Id = Guid.NewGuid(),
            ChatId = targetChatId,
            Content = "Old authentication discussion",
            CreatedAt = oldDate
        });

        await Task.Delay(100);

        // Act
        var query = new SearchQuery
        {
            QueryText = "authentication",
            ChatId = targetChatId,
            Since = DateTime.UtcNow.AddDays(-7),
            PageSize = 10
        };
        var results = await _searchService.SearchAsync(query, CancellationToken.None);

        // Assert
        Assert.Equal(1, results.TotalCount); // Only recent message in target chat
        Assert.Equal(targetChatId, results.Results[0].ChatId);
    }

    public async Task DisposeAsync()
    {
        await _app.DisposeAsync();
    }
}
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Index message | 5ms | 10ms |
| Search 10k | 250ms | 500ms |
| Snippet gen | 25ms | 50ms |

---

## User Verification Steps

### Scenario 1: Basic Single-Term Search

**Objective:** Verify that simple keyword search finds messages containing the search term.

**Preconditions:**
- ACODE installed and configured
- At least one chat with multiple messages exists
- Search index is populated (automatic on message creation)

**Steps:**
1. Create a new chat: `acode chat create "Search Test Chat"`
2. Start a run and add messages about authentication:
   - "How do I implement JWT authentication?"
   - "Use OAuth2 for secure API access"
   - "Basic auth is not recommended for production"
3. Execute search: `acode search authentication`

**Expected Results:**
- ✅ Search completes in <1 second
- ✅ Message containing "authentication" appears in results
- ✅ Result shows snippet with "authentication" highlighted
- ✅ Relevance score displayed for each result

**Verification Commands:**
```bash
acode search authentication --verbose
# Should show: Query time, Results count, Relevance scores
```

---

### Scenario 2: Phrase Search with Exact Match

**Objective:** Verify that quoted phrase search returns only exact phrase matches.

**Preconditions:**
- Chat with varied messages containing similar but different phrases
- Messages include: "JWT authentication", "authentication token", "token refresh"

**Steps:**
1. Create test messages with various authentication-related phrases
2. Search for exact phrase: `acode search "JWT authentication"`
3. Search for different phrase: `acode search "authentication token"`

**Expected Results:**
- ✅ Search for "JWT authentication" returns ONLY messages with that exact phrase
- ✅ Messages with "authentication" alone NOT returned for phrase search
- ✅ Messages with "JWT" and "authentication" separated by other words NOT returned
- ✅ Phrase is highlighted as a unit in snippets

**Verification Commands:**
```bash
acode search "JWT authentication" --json | jq '.results[].snippet'
# Each snippet should contain "JWT authentication" as consecutive words
```

---

### Scenario 3: Boolean Search with AND/OR/NOT

**Objective:** Verify that Boolean operators correctly combine search terms.

**Preconditions:**
- Chat with messages covering multiple topics:
  - "Implement authentication with JWT tokens"
  - "Use OAuth2 for authorization"
  - "JWT tokens expire after 1 hour"
  - "Session management without JWT"

**Steps:**
1. Search with AND: `acode search "JWT AND tokens"`
2. Search with OR: `acode search "JWT OR OAuth2"`
3. Search with NOT: `acode search "authentication NOT JWT"`
4. Search with complex expression: `acode search "(JWT OR OAuth2) AND security"`

**Expected Results:**
- ✅ AND: Only messages with BOTH "JWT" and "tokens"
- ✅ OR: Messages with either "JWT" or "OAuth2"
- ✅ NOT: Messages with "authentication" but WITHOUT "JWT"
- ✅ Parentheses: Grouped logic evaluated correctly

**Verification Commands:**
```bash
acode search "JWT AND tokens" --verbose
# Result count should be subset of "JWT OR tokens"

acode search "authentication NOT JWT" --verbose
# Should return OAuth2 message but not JWT messages
```

---

### Scenario 4: Chat-Scoped Search with Filters

**Objective:** Verify that `--chat` filter restricts search to specific chat context.

**Preconditions:**
- Two chats created:
  - "Backend Development" with authentication messages
  - "Frontend Work" with React/UI messages

**Steps:**
1. Create two chats with distinct content:
   ```bash
   acode chat create "Backend Development"
   # Add message: "Implement JWT authentication service"
   
   acode chat create "Frontend Work"
   # Add message: "Build login form with authentication"
   ```
2. Search across all: `acode search authentication`
3. Search within specific chat: `acode search authentication --chat "Backend Development"`

**Expected Results:**
- ✅ Global search returns results from BOTH chats
- ✅ Filtered search returns results from Backend Development ONLY
- ✅ Frontend Work messages NOT included in filtered results
- ✅ Filter stats shown in verbose mode

**Verification Commands:**
```bash
acode search authentication --chat "Backend Development" --verbose
# Should show: "Filter: chat=Backend Development"
# Results should only have chatName="Backend Development"
```

---

### Scenario 5: Date Range Search with --since and --until

**Objective:** Verify that date filters correctly constrain search to time range.

**Preconditions:**
- Messages created across different dates (or use --since with relative dates)

**Steps:**
1. Create messages over time (simulate or backdate)
2. Search with since filter: `acode search "API" --since 7d`
3. Search with until filter: `acode search "API" --until 2025-01-01`
4. Search with both: `acode search "API" --since 2025-01-01 --until 2025-01-15`

**Expected Results:**
- ✅ --since 7d returns only messages from last 7 days
- ✅ --until excludes messages after specified date
- ✅ Combined range returns intersection of both filters
- ✅ Relative date formats (7d, 2w, 1m) work correctly

**Verification Commands:**
```bash
acode search "API" --since 7d --verbose
# Should show: "Filter: since=7d ago"
# Timestamps in results should all be within last 7 days
```

---

### Scenario 6: Role-Based Search Filtering

**Objective:** Verify that `--role` filter restricts search to specific message roles.

**Preconditions:**
- Chat with messages from different roles:
  - User prompts about authentication
  - Assistant responses about implementation
  - System prompts (if any)

**Steps:**
1. Search user messages only: `acode search authentication --role user`
2. Search assistant messages only: `acode search authentication --role assistant`
3. Combine with other filters: `acode search authentication --role user --chat "Backend"`

**Expected Results:**
- ✅ --role user returns only messages with role="user"
- ✅ --role assistant returns only messages with role="assistant"
- ✅ Role filter combines correctly with other filters
- ✅ Invalid role value returns helpful error

**Verification Commands:**
```bash
acode search authentication --role user --json | jq '.results[].role'
# Every result should show "user"

acode search authentication --role invalid
# Should return ACODE-SRCH-004 error with valid role values
```

---

### Scenario 7: Relevance Ranking and Recency Boost

**Objective:** Verify that search results are ranked by relevance with configurable recency boost.

**Preconditions:**
- Messages with varying keyword density
- Messages from different time periods

**Steps:**
1. Create messages with different term frequency:
   - "JWT authentication is secure" (1 mention)
   - "JWT JWT JWT everywhere using JWT" (4 mentions)
2. Search: `acode search JWT`
3. Verify ranking shows higher frequency first
4. Create recent message with JWT, verify recency boost affects ranking

**Expected Results:**
- ✅ Messages with more keyword occurrences ranked higher
- ✅ Recent messages (within 24h) receive visible boost
- ✅ Score values shown correlate with relevance factors
- ✅ Exact phrase matches ranked above individual term matches

**Verification Commands:**
```bash
acode search JWT --verbose
# Higher scores should correlate with:
# - More term occurrences
# - More recent timestamps
# - Exact phrase matches
```

---

### Scenario 8: Index Rebuild and Status Verification

**Objective:** Verify that index maintenance commands work correctly and index status is accurate.

**Preconditions:**
- Existing chat data in database
- Index may be out of sync or uninitialized

**Steps:**
1. Check index status: `acode search index status`
2. If unhealthy or empty, rebuild: `acode search index rebuild`
3. Monitor rebuild progress
4. Verify status after rebuild: `acode search index status`
5. Run optimization: `acode search index optimize`

**Expected Results:**
- ✅ Status shows IndexedMessageCount, PendingCount, Health
- ✅ Rebuild processes all messages with progress indicator
- ✅ Rebuild completes in reasonable time (<60s for 10k messages)
- ✅ Status shows "Healthy" after successful rebuild
- ✅ Optimize reduces segment count

**Verification Commands:**
```bash
acode search index status
# Output:
# Status: Healthy
# IndexedMessages: 1234
# PendingMessages: 0
# IndexSize: 2.5 MB
# Segments: 1
# LastOptimized: 2025-01-15T10:30:00Z

acode search index rebuild --verbose
# Shows progress: "Rebuilding index... 500/1234 messages (40%)"
```

---

### Scenario 9: Snippet Generation and Highlighting

**Objective:** Verify that search result snippets are contextual and properly highlighted.

**Preconditions:**
- Messages with search terms in various positions (beginning, middle, end)
- Messages with multiple occurrences of search term

**Steps:**
1. Create message with term in middle of long text
2. Search for term: `acode search "authentication"`
3. Examine snippet in table output
4. Examine snippet in JSON output: `acode search "authentication" --json`

**Expected Results:**
- ✅ Snippet shows ~150 characters of context around match
- ✅ Matching terms wrapped in highlight markers
- ✅ Snippet doesn't truncate mid-word
- ✅ Multiple matches in same message all highlighted
- ✅ JSON output includes raw `<mark>` tags

**Verification Commands:**
```bash
acode search authentication --json | jq '.results[0].snippet'
# Should show something like:
# "...the API endpoint requires <mark>authentication</mark> using..."

acode search authentication
# Table output should render highlighted text in color
```

---

### Scenario 10: Error Handling and Edge Cases

**Objective:** Verify that search handles errors gracefully and provides helpful feedback.

**Preconditions:**
- Access to CLI
- Understanding of expected error codes

**Steps:**
1. Invalid query syntax: `acode search "unterminated phrase`
2. Query timeout simulation: `acode search "*" --timeout 1ms` (very broad query)
3. Invalid filter value: `acode search test --role invalid`
4. Invalid date format: `acode search test --since "not-a-date"`
5. Empty query: `acode search ""`
6. Search with uninitialized index: (delete index file, then search)

**Expected Results:**
- ✅ ACODE-SRCH-001: "Invalid query syntax - unbalanced quotes"
- ✅ ACODE-SRCH-002: "Query timeout exceeded" with suggestion to narrow query
- ✅ ACODE-SRCH-004: "Invalid role 'invalid'. Valid values: user, assistant, system, tool"
- ✅ ACODE-SRCH-003: "Invalid date format. Use ISO 8601 or relative (7d, 2w)"
- ✅ ACODE-SRCH-001: "Query cannot be empty"
- ✅ ACODE-SRCH-006: "Search index not initialized. Run: acode search index rebuild"

**Verification Commands:**
```bash
acode search "unterminated phrase
# Error: ACODE-SRCH-001: Invalid query syntax - unbalanced quotes

acode search test --role invalid
# Error: ACODE-SRCH-004: Invalid role 'invalid'
# Valid values: user, assistant, system, tool
```

---

## Implementation Prompt

Implement full-text search and indexing for conversation history using SQLite FTS5 (local) and PostgreSQL full-text search (remote). The system must provide sub-second search across thousands of messages with relevance ranking and snippet generation.

### File Structure

```
src/Acode.Domain/
├── Search/
│   ├── SearchQuery.cs
│   └── SearchResult.cs
│
src/Acode.Application/
├── Interfaces/
│   ├── ISearchService.cs
│   └── IIndexer.cs
│
src/Acode.Infrastructure/
├── Search/
│   ├── SqliteFtsSearchService.cs
│   ├── BM25Ranker.cs
│   └── SnippetGenerator.cs
│
src/Acode.Cli/
├── Commands/
│   └── SearchCommand.cs
```

### Value Objects - SearchQuery.cs (50 lines)

```csharp
// src/Acode.Domain/Search/SearchQuery.cs
namespace Acode.Domain.Search;

public sealed record SearchQuery
{
    public string QueryText { get; init; } = string.Empty;
    public Guid? ChatId { get; init; }
    public DateTime? Since { get; init; }
    public DateTime? Until { get; init; }
    public MessageRole? RoleFilter { get; init; }
    public int PageSize { get; init; } = 20;
    public int PageNumber { get; init; } = 1;
    public SortOrder SortBy { get; init; } = SortOrder.Relevance;

    public Result<SearchQuery, Error> Validate()
    {
        if (string.IsNullOrWhiteSpace(QueryText))
        {
            return Result.Failure<SearchQuery, Error>(
                new ValidationError("Query text cannot be empty"));
        }

        if (QueryText.Length > 200)
        {
            return Result.Failure<SearchQuery, Error>(
                new ValidationError("Query text must be ≤200 characters"));
        }

        if (PageSize < 1 || PageSize > 100)
        {
            return Result.Failure<SearchQuery, Error>(
                new ValidationError("Page size must be between 1 and 100"));
        }

        if (Since.HasValue && Until.HasValue && Since.Value > Until.Value)
        {
            return Result.Failure<SearchQuery, Error>(
                new ValidationError("Since date must be before Until date"));
        }

        return Result.Success<SearchQuery, Error>(this);
    }

    public enum SortOrder
    {
        Relevance,
        DateDescending,
        DateAscending
    }
}
```

### Value Objects - SearchResult.cs (40 lines)

```csharp
// src/Acode.Domain/Search/SearchResult.cs
namespace Acode.Domain.Search;

public sealed record SearchResult
{
    public required Guid MessageId { get; init; }
    public required Guid ChatId { get; init; }
    public required string ChatTitle { get; init; }
    public required MessageRole Role { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required string Snippet { get; init; }
    public required double Score { get; init; }
    public IReadOnlyList<MatchLocation> Matches { get; init; } = Array.Empty<MatchLocation>();
}

public sealed record MatchLocation
{
    public required string Field { get; init; } // "content", "title", "tags"
    public required int StartOffset { get; init; }
    public required int Length { get; init; }
}

public sealed record SearchResults
{
    public required IReadOnlyList<SearchResult> Results { get; init; }
    public required int TotalCount { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public required double QueryTimeMs { get; init; }
    public string? NextCursor { get; init; }

    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}
```

### Service Interface - ISearchService.cs (25 lines)

```csharp
// src/Acode.Application/Interfaces/ISearchService.cs
namespace Acode.Application.Interfaces;

public interface ISearchService
{
    Task<Result<SearchResults, Error>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken);
        
    Task<Result<Unit, Error>> IndexMessageAsync(
        Message message,
        CancellationToken cancellationToken);
        
    Task<Result<Unit, Error>> UpdateMessageIndexAsync(
        Message message,
        CancellationToken cancellationToken);
        
    Task<Result<Unit, Error>> RemoveFromIndexAsync(
        Guid messageId,
        CancellationToken cancellationToken);
        
    Task<Result<IndexStatus, Error>> GetIndexStatusAsync(
        CancellationToken cancellationToken);
        
    Task<Result<Unit, Error>> RebuildIndexAsync(
        IProgress<int>? progress,
        CancellationToken cancellationToken);
}
```

### SQLite FTS5 Implementation - SqliteFtsSearchService.cs (150 lines)

```csharp
// src/Acode.Infrastructure/Search/SqliteFtsSearchService.cs
namespace Acode.Infrastructure.Search;

public sealed class SqliteFtsSearchService : ISearchService
{
    private readonly SqliteConnection _connection;
    private readonly ILogger<SqliteFtsSearchService> _logger;
    private readonly SafeQueryParser _queryParser;
    private readonly BM25Ranker _ranker;
    private readonly SnippetGenerator _snippetGenerator;

    public SqliteFtsSearchService(
        SqliteConnection connection,
        ILogger<SqliteFtsSearchService> logger,
        SafeQueryParser queryParser,
        BM25Ranker ranker,
        SnippetGenerator snippetGenerator)
    {
        _connection = connection;
        _logger = logger;
        _queryParser = queryParser;
        _ranker = ranker;
        _snippetGenerator = snippetGenerator;
    }

    public async Task<Result<SearchResults, Error>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // Validate query
        var validationResult = query.Validate();
        if (validationResult.IsFailure)
        {
            return Result.Failure<SearchResults, Error>(validationResult.Error);
        }

        // Parse and sanitize query text
        var parsedResult = _queryParser.Parse(query.QueryText);
        if (parsedResult.IsFailure)
        {
            return Result.Failure<SearchResults, Error>(parsedResult.Error);
        }

        try
        {
            using var cmd = _connection.CreateCommand();
            
            // Build FTS5 query with filters
            var sqlBuilder = new StringBuilder();
            sqlBuilder.Append(@"
                SELECT 
                    cs.message_id,
                    m.chat_id,
                    c.title AS chat_title,
                    m.role,
                    m.created_at,
                    snippet(conversation_search, 4, '<mark>', '</mark>', '...', 32) AS snippet,
                    bm25(conversation_search) AS base_score
                FROM conversation_search cs
                INNER JOIN messages m ON cs.message_id = m.id
                INNER JOIN chats c ON m.chat_id = c.id
                WHERE conversation_search MATCH @queryText");

            cmd.Parameters.AddWithValue("@queryText", parsedResult.Value);

            // Apply filters
            if (query.ChatId.HasValue)
            {
                sqlBuilder.Append(" AND m.chat_id = @chatId");
                cmd.Parameters.AddWithValue("@chatId", query.ChatId.Value.ToString());
            }

            if (query.Since.HasValue)
            {
                sqlBuilder.Append(" AND m.created_at >= @since");
                cmd.Parameters.AddWithValue("@since", query.Since.Value.ToString("O"));
            }

            if (query.Until.HasValue)
            {
                sqlBuilder.Append(" AND m.created_at <= @until");
                cmd.Parameters.AddWithValue("@until", query.Until.Value.ToString("O"));
            }

            if (query.RoleFilter.HasValue)
            {
                sqlBuilder.Append(" AND m.role = @role");
                cmd.Parameters.AddWithValue("@role", query.RoleFilter.Value.ToString());
            }

            // Order by relevance (BM25 score)
            sqlBuilder.Append(" ORDER BY base_score DESC");

            // Pagination
            var offset = (query.PageNumber - 1) * query.PageSize;
            sqlBuilder.Append(" LIMIT @pageSize OFFSET @offset");
            cmd.Parameters.AddWithValue("@pageSize", query.PageSize);
            cmd.Parameters.AddWithValue("@offset", offset);

            cmd.CommandText = sqlBuilder.ToString();

            var results = new List<SearchResult>();
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                var createdAt = DateTime.Parse(reader.GetString(4));
                var baseScore = reader.GetDouble(6);
                
                // Apply recency boost
                var finalScore = _ranker.ApplyRecencyBoost(baseScore, createdAt);

                results.Add(new SearchResult
                {
                    MessageId = Guid.Parse(reader.GetString(0)),
                    ChatId = Guid.Parse(reader.GetString(1)),
                    ChatTitle = reader.GetString(2),
                    Role = Enum.Parse<MessageRole>(reader.GetString(3)),
                    CreatedAt = createdAt,
                    Snippet = reader.GetString(5),
                    Score = finalScore
                });
            }

            // Get total count
            var countCmd = _connection.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM conversation_search WHERE conversation_search MATCH @queryText";
            countCmd.Parameters.AddWithValue("@queryText", parsedResult.Value);
            var totalCount = (long)await countCmd.ExecuteScalarAsync(cancellationToken);

            stopwatch.Stop();

            var searchResults = new SearchResults
            {
                Results = results,
                TotalCount = (int)totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                QueryTimeMs = stopwatch.Elapsed.TotalMilliseconds
            };

            _logger.LogInformation(
                "Search completed: {Query} returned {Count} results in {Ms}ms",
                query.QueryText, totalCount, stopwatch.Elapsed.TotalMilliseconds);

            return Result.Success<SearchResults, Error>(searchResults);
        }
        catch (SqliteException ex)
        {
            _logger.LogError(ex, "Search query failed: {Query}", query.QueryText);
            return Result.Failure<SearchResults, Error>(
                new InfrastructureError("Search query failed", ex));
        }
    }

    public async Task<Result<Unit, Error>> IndexMessageAsync(
        Message message,
        CancellationToken cancellationToken)
    {
        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO conversation_search (message_id, chat_id, created_at, role, content, chat_title, tags)
                SELECT 
                    @messageId,
                    @chatId,
                    @createdAt,
                    @role,
                    @content,
                    (SELECT title FROM chats WHERE id = @chatId),
                    (SELECT GROUP_CONCAT(tag, ' ') FROM chat_tags WHERE chat_id = @chatId)";

            cmd.Parameters.AddWithValue("@messageId", message.Id.ToString());
            cmd.Parameters.AddWithValue("@chatId", message.ChatId.ToString());
            cmd.Parameters.AddWithValue("@createdAt", message.CreatedAt.ToString("O"));
            cmd.Parameters.AddWithValue("@role", message.Role.ToString());
            cmd.Parameters.AddWithValue("@content", message.Content);

            await cmd.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogDebug("Indexed message {MessageId}", message.Id);

            return Result.Success<Unit, Error>(Unit.Value);
        }
        catch (SqliteException ex)
        {
            _logger.LogError(ex, "Failed to index message {MessageId}", message.Id);
            return Result.Failure<Unit, Error>(
                new InfrastructureError("Failed to index message", ex));
        }
    }

    public async Task<Result<Unit, Error>> UpdateMessageIndexAsync(
        Message message,
        CancellationToken cancellationToken)
    {
        // FTS5 external content doesn't support UPDATE, need DELETE + INSERT
        await RemoveFromIndexAsync(message.Id, cancellationToken);
        return await IndexMessageAsync(message, cancellationToken);
    }

    public async Task<Result<Unit, Error>> RemoveFromIndexAsync(
        Guid messageId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM conversation_search WHERE message_id = @messageId";
            cmd.Parameters.AddWithValue("@messageId", messageId.ToString());
            await cmd.ExecuteNonQueryAsync(cancellationToken);

            return Result.Success<Unit, Error>(Unit.Value);
        }
        catch (SqliteException ex)
        {
            _logger.LogError(ex, "Failed to remove message {MessageId} from index", messageId);
            return Result.Failure<Unit, Error>(
                new InfrastructureError("Failed to remove from index", ex));
        }
    }

    public async Task<Result<IndexStatus, Error>> GetIndexStatusAsync(
        CancellationToken cancellationToken)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM conversation_search";
        var indexedCount = (long)await cmd.ExecuteScalarAsync(cancellationToken);

        cmd.CommandText = "SELECT COUNT(*) FROM messages";
        var totalCount = (long)await cmd.ExecuteScalarAsync(cancellationToken);

        return Result.Success<IndexStatus, Error>(new IndexStatus
        {
            IndexedMessageCount = (int)indexedCount,
            TotalMessageCount = (int)totalCount,
            IsHealthy = indexedCount == totalCount
        });
    }

    public async Task<Result<Unit, Error>> RebuildIndexAsync(
        IProgress<int>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            // Clear existing index
            using var deleteCmd = _connection.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM conversation_search";
            await deleteCmd.ExecuteNonQueryAsync(cancellationToken);

            // Reindex all messages
            using var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = "SELECT id, chat_id, created_at, role, content FROM messages";
            using var reader = await selectCmd.ExecuteReaderAsync(cancellationToken);

            var indexed = 0;
            while (await reader.ReadAsync(cancellationToken))
            {
                var message = new Message
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    ChatId = Guid.Parse(reader.GetString(1)),
                    CreatedAt = DateTime.Parse(reader.GetString(2)),
                    Role = Enum.Parse<MessageRole>(reader.GetString(3)),
                    Content = reader.GetString(4)
                };

                await IndexMessageAsync(message, cancellationToken);
                indexed++;
                progress?.Report(indexed);
            }

            _logger.LogInformation("Index rebuilt: {Count} messages indexed", indexed);

            return Result.Success<Unit, Error>(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Index rebuild failed");
            return Result.Failure<Unit, Error>(
                new InfrastructureError("Index rebuild failed", ex));
        }
    }
}
```

### BM25 Ranker - BM25Ranker.cs (80 lines)

```csharp
// src/Acode.Infrastructure/Search/BM25Ranker.cs
namespace Acode.Infrastructure.Search;

public sealed class BM25Ranker
{
    private const double K1 = 1.2; // Term saturation parameter
    private const double B = 0.75; // Length normalization parameter
    private const double RecencyBoostRecent = 1.5;  // < 7 days
    private const double RecencyBoostNormal = 1.0;  // 7-30 days
    private const double RecencyBoostOld = 0.8;     // > 30 days
    private const int RecentDays = 7;
    private const int NormalDays = 30;

    public double ApplyRecencyBoost(double baseScore, DateTime messageDate)
    {
        var age = DateTime.UtcNow - messageDate;
        
        if (age.TotalDays < RecentDays)
        {
            return baseScore * RecencyBoostRecent;
        }
        else if (age.TotalDays <= NormalDays)
        {
            return baseScore * RecencyBoostNormal;
        }
        else
        {
            return baseScore * RecencyBoostOld;
        }
    }

    public double CalculateBM25(
        int termFrequency,
        int documentLength,
        int avgDocumentLength,
        int totalDocuments,
        int documentsContainingTerm)
    {
        // IDF (Inverse Document Frequency)
        var idf = Math.Log(
            (totalDocuments - documentsContainingTerm + 0.5) /
            (documentsContainingTerm + 0.5) + 1.0);

        // Normalized term frequency
        var normalizedTF = (termFrequency * (K1 + 1)) /
            (termFrequency + K1 * (1 - B + B * ((double)documentLength / avgDocumentLength)));

        return idf * normalizedTF;
    }

    public double ApplyFieldBoost(double score, string field)
    {
        return field switch
        {
            "title" => score * 2.0,   // Title matches boosted 2x
            "tags" => score * 1.5,    // Tag matches boosted 1.5x
            "content" => score * 1.0, // Content baseline
            _ => score
        };
    }
}
```

### Snippet Generator - SnippetGenerator.cs (70 lines)

```csharp
// src/Acode.Infrastructure/Search/SnippetGenerator.cs
namespace Acode.Infrastructure.Search;

public sealed class SnippetGenerator
{
    private const int DefaultMaxLength = 150;
    private const string HighlightOpen = "<mark>";
    private const string HighlightClose = "</mark>";
    private const string Ellipsis = "...";

    private readonly int _maxLength;

    public SnippetGenerator(int maxLength = DefaultMaxLength)
    {
        _maxLength = maxLength;
    }

    public string Generate(string content, IEnumerable<int> matchOffsets)
    {
        if (!matchOffsets.Any())
        {
            return TruncateAtWordBoundary(content, _maxLength);
        }

        var firstOffset = matchOffsets.First();
        var contextStart = Math.Max(0, firstOffset - 75);
        var contextLength = Math.Min(_maxLength, content.Length - contextStart);

        var snippet = content.Substring(contextStart, contextLength);

        // Add ellipsis if truncated
        if (contextStart > 0)
        {
            snippet = Ellipsis + snippet;
        }

        if (contextStart + contextLength < content.Length)
        {
            snippet = snippet + Ellipsis;
        }

        return snippet;
    }

    public string HighlightTerms(string snippet, IEnumerable<string> terms)
    {
        var highlighted = snippet;
        foreach (var term in terms)
        {
            var pattern = $@"\b({Regex.Escape(term)})\b";
            highlighted = Regex.Replace(
                highlighted,
                pattern,
                $"{HighlightOpen}$1{HighlightClose}",
                RegexOptions.IgnoreCase);
        }

        return highlighted;
    }

    private string TruncateAtWordBoundary(string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }

        var truncated = text.Substring(0, maxLength);
        var lastSpace = truncated.LastIndexOf(' ');

        if (lastSpace > 0)
        {
            truncated = truncated.Substring(0, lastSpace);
        }

        return truncated + Ellipsis;
    }
}
```

### CLI Command - SearchCommand.cs (90 lines)

```csharp
// src/Acode.Cli/Commands/SearchCommand.cs
namespace Acode.Cli.Commands;

[Command("search", Description = "Search conversation history")]
public sealed class SearchCommand : AsyncCommand<SearchCommand.Settings>
{
    private readonly ISearchService _searchService;
    private readonly IAnsiConsole _console;

    public SearchCommand(ISearchService searchService, IAnsiConsole console)
    {
        _searchService = searchService;
        _console = console;
    }

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<query>")]
        [Description("Search query text")]
        public string QueryText { get; init; } = string.Empty;

        [CommandOption("--chat <CHAT_ID>")]
        [Description("Filter by chat ID")]
        public Guid? ChatId { get; init; }

        [CommandOption("--since <DATE>")]
        [Description("Filter messages after date (ISO 8601)")]
        public DateTime? Since { get; init; }

        [CommandOption("--until <DATE>")]
        [Description("Filter messages before date (ISO 8601)")]
        public DateTime? Until { get; init; }

        [CommandOption("--role <ROLE>")]
        [Description("Filter by message role (user, assistant, system)")]
        public MessageRole? RoleFilter { get; init; }

        [CommandOption("--page-size <SIZE>")]
        [Description("Results per page (1-100, default 20)")]
        [DefaultValue(20)]
        public int PageSize { get; init; } = 20;

        [CommandOption("--page <NUMBER>")]
        [Description("Page number (default 1)")]
        [DefaultValue(1)]
        public int PageNumber { get; init; } = 1;

        [CommandOption("--json")]
        [Description("Output as JSON")]
        public bool JsonOutput { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var query = new SearchQuery
        {
            QueryText = settings.QueryText,
            ChatId = settings.ChatId,
            Since = settings.Since,
            Until = settings.Until,
            RoleFilter = settings.RoleFilter,
            PageSize = settings.PageSize,
            PageNumber = settings.PageNumber
        };

        var result = await _searchService.SearchAsync(query, CancellationToken.None);

        if (result.IsFailure)
        {
            _console.MarkupLine($"[red]Error:[/] {result.Error.Message}");
            return 1;
        }

        var searchResults = result.Value;

        if (settings.JsonOutput)
        {
            _console.WriteLine(JsonSerializer.Serialize(searchResults, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
            return 0;
        }

        // Table output
        var table = new Table();
        table.AddColumn("Chat");
        table.AddColumn("Date");
        table.AddColumn("Role");
        table.AddColumn("Snippet");
        table.AddColumn("Score");

        foreach (var item in searchResults.Results)
        {
            table.AddRow(
                item.ChatTitle.EscapeMarkup(),
                item.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                item.Role.ToString(),
                item.Snippet.EscapeMarkup(),
                item.Score.ToString("F2"));
        }

        _console.Write(table);
        _console.MarkupLine(
            $"\nPage {searchResults.PageNumber}/{searchResults.TotalPages} | " +
            $"Total: {searchResults.TotalCount} results | " +
            $"Query time: {searchResults.QueryTimeMs:F0}ms");

        return 0;
    }
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

1. [x] Create SearchQuery value object with validation
2. [x] Create SearchResult/SearchResults value objects
3. [x] Create ISearchService interface
4. [x] Implement SqliteFtsSearchService with FTS5
5. [x] Implement BM25Ranker with recency boost
6. [x] Implement SnippetGenerator with highlighting
7. [x] Add SearchCommand CLI with filters
8. [ ] Implement PostgreSQL FTS backend
9. [ ] Add index management commands (rebuild, optimize, status)
10. [ ] Write comprehensive tests (unit, integration, E2E)

---

**End of Task 049.d Specification**