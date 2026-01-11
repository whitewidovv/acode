# Task 015.b: Search Tool Integration

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 015 (Indexing v1), Task 010 (CLI Framework)  

---

## Description

Task 015.b integrates the search index with the tool system. The agent uses tools to interact with the codebase. Search is one of the most important tools.

The search tool enables the agent to find relevant code. When the agent needs to understand something, it searches. Search results inform context selection.

The tool follows the standard tool interface. It has defined inputs and outputs. It logs its usage. It handles errors gracefully.

Multiple search modes are supported. Text search finds content. File search finds paths. Grep search finds patterns. Each mode has its use cases.

Results are formatted for the agent. Snippets show relevant context. Line numbers enable navigation. Scores indicate relevance.

Rate limiting prevents runaway searches. Too many searches slow everything down. Limits are configurable. The agent is informed when limits apply.

The search tool integrates with context budgeting. Results count toward token limits. The tool can return fewer results when budget is tight.

Error handling is comprehensive. Index not ready. Search too broad. No results. Each case has clear handling and messaging.

### Business Value

Search is the primary mechanism by which the AI agent discovers and understands code in the repository. Without effective search tools, the agent would be limited to files explicitly provided by the user, dramatically reducing its ability to understand context, find related implementations, and make informed changes across a codebase. The search tool integration transforms a passive index into an active capability that the agent can leverage autonomously.

The business value is multiplied by the tool interface approach. By exposing search as a standard tool with well-defined inputs and outputs, the agent can reason about when and how to search. It can formulate queries based on its current understanding, interpret results, and refine searches iteratively. This creates a feedback loop where the agent becomes progressively better at finding relevant code as it explores the repository.

Rate limiting and error handling protect both the user and the system from pathological behavior. An agent that searches too aggressively can slow down the entire interaction, while poor error messages leave the agent unable to recover. By providing clear limits and actionable error information, the search tools enable robust, predictable agent behavior even in edge cases.

### ROI Analysis

**Cost Without Search Tools:**
| Scenario | Manual Approach | Time Cost | Weekly Frequency | Weekly Cost (@ $100/hr) |
|----------|-----------------|-----------|------------------|-------------------------|
| Find implementation | Manually browse files | 15 min | 50× | $1,250/week |
| Locate usages | Grep + review | 8 min | 30× | $400/week |
| Find related code | Ask user for context | 10 min + wait | 25× | $417/week |
| Debug reference | Manual file hunting | 12 min | 20× | $400/week |

**Cost With Search Tools:**
| Scenario | Tool Approach | Time Cost | Weekly Frequency | Weekly Cost |
|----------|---------------|-----------|------------------|-------------|
| Find implementation | `search_text` | 2 sec | 50× | $2.78/week |
| Locate usages | `grep` tool | 3 sec | 30× | $2.50/week |
| Find related code | `search_files` | 1 sec | 25× | $0.69/week |
| Debug reference | Combined search | 5 sec | 20× | $2.78/week |

**Investment:** 32 hours implementation @ $100/hr = **$3,200 one-time**

**Annual Savings:**
- Implementation discovery: ($1,250 - $2.78) × 52 = **$64,856/year**
- Usage location: ($400 - $2.50) × 52 = **$20,670/year**
- Context gathering: ($417 - $0.69) × 52 = **$21,648/year**
- Debug reference: ($400 - $2.78) × 52 = **$20,655/year**

**Total Annual Savings: $127,829/year**
**ROI: 40× investment in Year 1**

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                            SEARCH TOOL INTEGRATION                                   │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                       │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐ │
│  │                              AGENT ORCHESTRATOR                                  │ │
│  │                                                                                   │ │
│  │   Agent Loop ──▶ "I need to find UserService implementation"                     │ │
│  │                  ──▶ Decides to use search_text tool                             │ │
│  │                  ──▶ Formulates query: { "query": "UserService", "file_type": ".cs" } │
│  └───────────────────────────────────────┬─────────────────────────────────────────┘ │
│                                          │                                           │
│                                          ▼                                           │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐ │
│  │                               TOOL REGISTRY                                      │ │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐ │ │
│  │  │ search_text │  │search_files │  │    grep     │  │   ... other tools ...   │ │ │
│  │  │   Tool      │  │   Tool      │  │   Tool      │  │                         │ │ │
│  │  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └─────────────────────────┘ │ │
│  └─────────┼────────────────┼────────────────┼─────────────────────────────────────┘ │
│            │                │                │                                       │
│            ▼                ▼                ▼                                       │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐ │
│  │                            SEARCH TOOL BASE                                      │ │
│  │  ┌───────────────────┐  ┌───────────────────┐  ┌──────────────────────────────┐ │ │
│  │  │  Rate Limiter     │  │  Query Validator  │  │  Result Formatter            │ │ │
│  │  │  - 30/min default │  │  - Sanitize input │  │  - JSON serialization        │ │ │
│  │  │  - Per-tool limits│  │  - Regex compile  │  │  - Snippet extraction        │ │ │
│  │  │  - Retry-after    │  │  - Length limits  │  │  - Score normalization       │ │ │
│  │  └───────────────────┘  └───────────────────┘  └──────────────────────────────┘ │ │
│  └────────────────────────────────────┬────────────────────────────────────────────┘ │
│                                       │                                              │
│                                       ▼                                              │
│  ┌─────────────────────────────────────────────────────────────────────────────────┐ │
│  │                              INDEX SERVICE                                       │ │
│  │                                                                                   │ │
│  │    SearchAsync(query) ──▶ SearchResultList { Results, TotalCount, Duration }     │ │
│  │                                                                                   │ │
│  │    ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐           │ │
│  │    │ FTS5 Search │  │ BM25 Rank   │  │ Snippet Gen │  │ Filter      │           │ │
│  │    │ Engine      │  │ Algorithm   │  │ Engine      │  │ Matcher     │           │ │
│  │    └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘           │ │
│  └────────────────────────────────────┬────────────────────────────────────────────┘ │
│                                       │                                              │
└───────────────────────────────────────┼──────────────────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                                 JSON RESULT                                          │
│                                                                                       │
│  {                                                                                   │
│    "totalCount": 15,                                                                 │
│    "results": [                                                                      │
│      {                                                                               │
│        "path": "src/Services/UserService.cs",                                        │
│        "line": 42,                                                                   │
│        "score": 0.95,                                                                │
│        "snippet": "public class UserService : IUserService { ... }"                 │
│      },                                                                              │
│      ...                                                                             │
│    ]                                                                                 │
│  }                                                                                   │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Trade-offs and Alternatives

| Decision | Alternative | Chosen Approach | Rationale |
|----------|-------------|-----------------|-----------|
| JSON result format | Plain text output | Structured JSON | JSON enables programmatic parsing by agent; easier to extract specific fields; consistent with other tools |
| Rate limiting strategy | No limits (trust agent) | 30/min default, configurable | Prevents runaway searches that degrade UX; protects shared resources; agent can be informed and adjust |
| Three separate tools | Single unified search | Distinct search_text, search_files, grep | Clear semantics for agent; different use cases; easier to optimize each independently |
| Synchronous execution | Async with callbacks | Sync with timeout | Agent loop expects sync results; simpler mental model; timeout prevents hangs |
| Results in tool response | Results stored, fetch separately | Inline results | Reduces round trips; agent has immediate context; no state management complexity |
| Default result limit | No limit (return all) | 20 results default | Prevents token explosion; forces relevance ranking; agent can request more if needed |

**Trade-off 1: Granular Tools vs. Unified Search**
- **Pro Granular:** Each tool has clear, focused purpose (content vs. path vs. pattern); agent can choose optimal tool for task; easier to extend with specialized behavior
- **Con Granular:** More tools to learn and choose from; potential confusion about when to use each
- **Decision:** Three separate tools. The distinct purposes (find content, find files, find patterns) map to different agent reasoning patterns. A unified tool would need complex mode switching.

**Trade-off 2: Rate Limiting Approach**
- **Pro Aggressive Limits:** Prevents token explosion; keeps interactions responsive; protects shared infrastructure
- **Con Aggressive Limits:** May block legitimate use cases; adds complexity to agent reasoning
- **Decision:** 30/min default with configurable override. This balances protection with usability. The agent receives clear feedback when limited and can adjust strategy.

**Trade-off 3: Snippet Length in Results**
- **Pro Long Snippets:** More context for agent understanding; fewer follow-up requests
- **Con Long Snippets:** Token overhead; may exceed context windows; diminishing returns after ~5 lines
- **Decision:** Default 2 lines context (configurable). This provides enough context for relevance assessment while remaining token-efficient.

### Scope

1. **Text Search Tool** - Content-based search across indexed files with relevance scoring, snippet extraction, and configurable result limits
2. **File Search Tool** - Path-based search with glob pattern support for finding files by name or location
3. **Grep Tool** - Pattern matching search with regex support for finding specific text patterns across the codebase
4. **Rate Limiting** - Per-session and configurable limits on search frequency with informative feedback to the agent
5. **Result Formatting** - Structured JSON output with snippets, line numbers, context, and relevance scores optimized for LLM consumption

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Tool Registry | Registration | Search tools register with the central tool registry for agent discovery |
| Index Service | Dependency | All search tools query the index service for fast search execution |
| Logging Service | Integration | Tool invocations and results are logged for debugging and analytics |
| Metrics Service | Integration | Search latency, result counts, and rate limit hits are recorded as metrics |
| Context Budget | Consumer | Search tools respect token budgets and adjust result counts accordingly |
| Agent Orchestrator | Consumer | Agent orchestrator invokes search tools during planning and execution |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Index not ready | Search cannot execute | Return clear error with retry guidance; suggest index build |
| Search query too broad | Excessive results, slow response | Apply result caps, suggest query refinement in response |
| No results found | Agent lacks needed information | Provide helpful message with alternative search suggestions |
| Rate limit exceeded | Agent blocked from searching | Return retry-after time, allow agent to adjust strategy |
| Search timeout | Long-running search abandoned | Return partial results if available, timeout notification |
| Invalid regex pattern | Grep tool fails | Validate regex before search, return syntax error with position |

### Assumptions

1. The index service is available and has been built before search tools are invoked
2. The agent understands the tool interface and can formulate valid search queries
3. Rate limits are sufficient for typical agent workflows without causing excessive blocking
4. JSON result format is optimal for LLM consumption and token efficiency
5. Relevance scoring from the index service accurately reflects result quality
6. Context lines around matches provide sufficient information for the agent to understand results
7. The tool registry follows a standard pattern that search tools can integrate with
8. Search performance depends on index quality and size; results are approximate not exhaustive

### Security Considerations

#### Threat 1: Query Injection via Regex Patterns

**Threat:** Malicious or malformed regex patterns in grep tool queries could cause ReDoS (Regular Expression Denial of Service) attacks.

**Risk Level:** High

**Mitigation:** Validate and compile patterns with timeout protection.

```csharp
using System;
using System.Text.RegularExpressions;

namespace Acode.Infrastructure.Tools.Search;

/// <summary>
/// Validates and safely compiles regex patterns from user input.
/// </summary>
public sealed class SafeRegexCompiler
{
    private const int MaxPatternLength = 500;
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan CompileTimeout = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Safely compiles a regex pattern with protection against ReDoS.
    /// </summary>
    /// <param name="pattern">The regex pattern to compile.</param>
    /// <param name="options">Regex options.</param>
    /// <returns>Compiled regex or null if unsafe.</returns>
    /// <exception cref="InvalidPatternException">Pattern is invalid or too complex.</exception>
    public Regex SafeCompile(string pattern, RegexOptions options = RegexOptions.None)
    {
        // Length check
        if (string.IsNullOrEmpty(pattern))
        {
            throw new InvalidPatternException("Pattern cannot be empty");
        }

        if (pattern.Length > MaxPatternLength)
        {
            throw new InvalidPatternException(
                $"Pattern exceeds maximum length of {MaxPatternLength} characters");
        }

        // Detect potentially dangerous patterns
        if (IsPotentiallyDangerous(pattern))
        {
            throw new InvalidPatternException(
                "Pattern contains potentially dangerous constructs that could cause slow matching");
        }

        try
        {
            // Compile with timeout protection
            var regex = new Regex(
                pattern,
                options | RegexOptions.Compiled,
                MatchTimeout);

            // Test the pattern with a sample to catch obvious issues
            _ = regex.IsMatch("test sample string for validation");

            return regex;
        }
        catch (ArgumentException ex)
        {
            throw new InvalidPatternException(
                $"Invalid regex pattern: {ex.Message}", ex);
        }
        catch (RegexMatchTimeoutException)
        {
            throw new InvalidPatternException(
                "Pattern matching timed out during validation - pattern may be too complex");
        }
    }

    /// <summary>
    /// Detects patterns that could cause exponential backtracking.
    /// </summary>
    private bool IsPotentiallyDangerous(string pattern)
    {
        // Nested quantifiers: (a+)+ or (a*)* 
        if (Regex.IsMatch(pattern, @"\([^)]*[+*][^)]*\)[+*]"))
        {
            return true;
        }

        // Overlapping alternations with quantifiers
        if (Regex.IsMatch(pattern, @"\([^)]*\|[^)]*\)[+*]"))
        {
            return true;
        }

        // Multiple adjacent quantifiers
        if (Regex.IsMatch(pattern, @"[+*?]{2,}"))
        {
            return true;
        }

        return false;
    }
}

public class InvalidPatternException : Exception
{
    public InvalidPatternException(string message) : base(message) { }
    public InvalidPatternException(string message, Exception inner) : base(message, inner) { }
}
```

#### Threat 2: Path Disclosure Outside Repository

**Threat:** Search results could potentially expose files outside the repository root if paths are not properly validated.

**Risk Level:** High

**Mitigation:** Ensure all result paths are within repository boundaries.

```csharp
using System;
using System.IO;
using System.Linq;
using Acode.Domain.Index;

namespace Acode.Infrastructure.Tools.Search;

/// <summary>
/// Validates and sanitizes search result paths.
/// </summary>
public sealed class SearchResultPathValidator
{
    private readonly string _repositoryRoot;
    private readonly string _normalizedRoot;

    public SearchResultPathValidator(string repositoryRoot)
    {
        _repositoryRoot = Path.GetFullPath(repositoryRoot);
        _normalizedRoot = _repositoryRoot.TrimEnd(Path.DirectorySeparatorChar) 
            + Path.DirectorySeparatorChar;
    }

    /// <summary>
    /// Validates that a result path is within the repository.
    /// </summary>
    public bool IsValidResultPath(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath))
        {
            return false;
        }

        var normalizedPath = Path.GetFullPath(absolutePath);
        return normalizedPath.StartsWith(_normalizedRoot, 
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Converts an absolute path to a safe relative path.
    /// </summary>
    public string ToSafeRelativePath(string absolutePath)
    {
        if (!IsValidResultPath(absolutePath))
        {
            throw new SecurityException(
                "Attempted to expose path outside repository");
        }

        var relative = Path.GetRelativePath(_repositoryRoot, absolutePath);
        
        // Ensure no traversal in the relative path
        if (relative.Contains(".."))
        {
            throw new SecurityException(
                "Path contains traversal sequences");
        }

        // Normalize to forward slashes for consistent output
        return relative.Replace(Path.DirectorySeparatorChar, '/');
    }

    /// <summary>
    /// Filters search results to only include valid paths.
    /// </summary>
    public IReadOnlyList<SearchResult> FilterResults(
        IReadOnlyList<SearchResult> results)
    {
        return results
            .Where(r => IsValidResultPath(
                Path.Combine(_repositoryRoot, r.FilePath)))
            .ToList();
    }
}
```

#### Threat 3: Resource Exhaustion via Excessive Searches

**Threat:** An agent or malicious caller could perform excessive searches, exhausting system resources and degrading performance.

**Risk Level:** Medium

**Mitigation:** Implement sliding window rate limiting with configurable thresholds.

```csharp
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Acode.Infrastructure.Tools.Search;

/// <summary>
/// Rate limiter using sliding window algorithm.
/// </summary>
public sealed class SearchRateLimiter
{
    private readonly ConcurrentDictionary<string, SlidingWindow> _windows = new();
    private readonly SearchRateLimitOptions _options;

    public SearchRateLimiter(IOptions<SearchRateLimitOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Checks if a search is allowed and consumes one token.
    /// </summary>
    /// <param name="toolName">The tool being rate limited.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Rate limit result.</returns>
    public async Task<RateLimitResult> TryAcquireAsync(
        string toolName,
        CancellationToken cancellationToken = default)
    {
        var window = _windows.GetOrAdd(
            toolName,
            _ => new SlidingWindow(
                _options.MaxRequestsPerMinute,
                TimeSpan.FromMinutes(1)));

        if (window.TryAcquire())
        {
            return RateLimitResult.Allowed(
                window.Remaining,
                window.ResetTime);
        }

        var retryAfter = window.ResetTime - DateTime.UtcNow;
        
        return RateLimitResult.Denied(
            retryAfter,
            $"Rate limit exceeded ({_options.MaxRequestsPerMinute}/min)");
    }

    /// <summary>
    /// Resets the rate limiter (for testing).
    /// </summary>
    public void Reset()
    {
        _windows.Clear();
    }
}

public sealed class SlidingWindow
{
    private readonly int _maxRequests;
    private readonly TimeSpan _windowSize;
    private readonly object _lock = new();
    private readonly Queue<DateTime> _timestamps = new();

    public int Remaining => Math.Max(0, _maxRequests - _timestamps.Count);
    public DateTime ResetTime { get; private set; }

    public SlidingWindow(int maxRequests, TimeSpan windowSize)
    {
        _maxRequests = maxRequests;
        _windowSize = windowSize;
        ResetTime = DateTime.UtcNow.Add(windowSize);
    }

    public bool TryAcquire()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var windowStart = now.Subtract(_windowSize);

            // Remove expired timestamps
            while (_timestamps.Count > 0 && _timestamps.Peek() < windowStart)
            {
                _timestamps.Dequeue();
            }

            if (_timestamps.Count >= _maxRequests)
            {
                ResetTime = _timestamps.Peek().Add(_windowSize);
                return false;
            }

            _timestamps.Enqueue(now);
            ResetTime = now.Add(_windowSize);
            return true;
        }
    }
}

public sealed record RateLimitResult
{
    public bool IsAllowed { get; init; }
    public int Remaining { get; init; }
    public DateTime ResetTime { get; init; }
    public TimeSpan? RetryAfter { get; init; }
    public string? Message { get; init; }

    public static RateLimitResult Allowed(int remaining, DateTime resetTime) =>
        new() { IsAllowed = true, Remaining = remaining, ResetTime = resetTime };

    public static RateLimitResult Denied(TimeSpan retryAfter, string message) =>
        new() { IsAllowed = false, RetryAfter = retryAfter, Message = message };
}

public sealed class SearchRateLimitOptions
{
    public int MaxRequestsPerMinute { get; set; } = 30;
    public bool EnableRateLimiting { get; set; } = true;
}
```

#### Threat 4: Sensitive Content Exposure in Results

**Threat:** Search results may include sensitive content (API keys, passwords, secrets) that should not be exposed.

**Risk Level:** Medium

**Mitigation:** Scan and redact potential secrets from search snippets.

```csharp
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Acode.Domain.Index;

namespace Acode.Infrastructure.Tools.Search;

/// <summary>
/// Redacts potential secrets from search result snippets.
/// </summary>
public sealed class SearchResultRedactor
{
    private static readonly List<(Regex Pattern, string Name)> SecretPatterns = new()
    {
        (new Regex(@"(?i)(api[_-]?key|apikey)\s*[:=]\s*['""]?[\w-]{20,}['""]?", RegexOptions.Compiled), "API Key"),
        (new Regex(@"(?i)(password|passwd|pwd)\s*[:=]\s*['""][^'""]{8,}['""]", RegexOptions.Compiled), "Password"),
        (new Regex(@"(?i)(secret|token)\s*[:=]\s*['""]?[\w-]{20,}['""]?", RegexOptions.Compiled), "Secret"),
        (new Regex(@"-----BEGIN (RSA |EC |DSA |OPENSSH )?PRIVATE KEY-----", RegexOptions.Compiled), "Private Key"),
        (new Regex(@"(?i)bearer\s+[\w-_.~+/]+=*", RegexOptions.Compiled), "Bearer Token"),
        (new Regex(@"(?i)(aws_access_key_id|aws_secret_access_key)\s*[:=]", RegexOptions.Compiled), "AWS Key"),
        (new Regex(@"ghp_[a-zA-Z0-9]{36}", RegexOptions.Compiled), "GitHub Token"),
        (new Regex(@"sk-[a-zA-Z0-9]{48}", RegexOptions.Compiled), "OpenAI Key"),
    };

    private const string RedactionPlaceholder = "[REDACTED]";

    /// <summary>
    /// Redacts potential secrets from a snippet.
    /// </summary>
    public string RedactSnippet(string snippet)
    {
        if (string.IsNullOrEmpty(snippet))
        {
            return snippet;
        }

        var result = snippet;
        foreach (var (pattern, _) in SecretPatterns)
        {
            result = pattern.Replace(result, match =>
            {
                // Keep the key name but redact the value
                var parts = match.Value.Split(new[] { ':', '=' }, 2);
                if (parts.Length == 2)
                {
                    return $"{parts[0]}={RedactionPlaceholder}";
                }
                return RedactionPlaceholder;
            });
        }

        return result;
    }

    /// <summary>
    /// Checks if a snippet contains potential secrets.
    /// </summary>
    public bool ContainsSecrets(string snippet)
    {
        if (string.IsNullOrEmpty(snippet))
        {
            return false;
        }

        foreach (var (pattern, _) in SecretPatterns)
        {
            if (pattern.IsMatch(snippet))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Redacts all snippets in search results.
    /// </summary>
    public SearchResultList RedactResults(SearchResultList results)
    {
        var redacted = results.Results.Select(r =>
        {
            var redactedSnippets = r.Snippets.Select(s => 
                s with { Text = RedactSnippet(s.Text) }).ToList();
            return r with { Snippets = redactedSnippets };
        }).ToList();

        return new SearchResultList(redacted, results.TotalCount);
    }
}
```

#### Threat 5: Log Injection via Search Queries

**Threat:** Malicious search queries could inject log entries that confuse log analysis or hide attacks.

**Risk Level:** Low

**Mitigation:** Sanitize search queries before logging.

```csharp
using System;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Tools.Search;

/// <summary>
/// Sanitizes search queries for safe logging.
/// </summary>
public sealed class SearchQuerySanitizer
{
    private const int MaxLoggedQueryLength = 200;

    /// <summary>
    /// Sanitizes a query for safe logging.
    /// </summary>
    public string SanitizeForLogging(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return "[empty]";
        }

        var sanitized = new StringBuilder(Math.Min(query.Length, MaxLoggedQueryLength + 10));

        foreach (char c in query)
        {
            if (sanitized.Length >= MaxLoggedQueryLength)
            {
                sanitized.Append("...");
                break;
            }

            // Replace control characters and newlines
            if (char.IsControl(c) || c == '\n' || c == '\r')
            {
                sanitized.Append(' ');
            }
            // Escape characters that could interfere with log parsing
            else if (c == '|' || c == '=' || c == '{' || c == '}')
            {
                sanitized.Append('_');
            }
            else
            {
                sanitized.Append(c);
            }
        }

        return sanitized.ToString();
    }

    /// <summary>
    /// Logs a search invocation with sanitized query.
    /// </summary>
    public void LogSearchInvocation(
        ILogger logger,
        string toolName,
        string query,
        int resultCount,
        TimeSpan duration)
    {
        logger.LogInformation(
            "Search tool invoked: Tool={Tool}, Query={Query}, Results={ResultCount}, Duration={Duration}ms",
            toolName,
            SanitizeForLogging(query),
            resultCount,
            duration.TotalMilliseconds);
    }
}
```

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Search Tool | A callable tool registered in the agent's tool registry that performs search operations against the code index and returns structured results for agent consumption |
| Tool Interface | The standardized API pattern that all agent tools implement, including schema definition, input validation, execution, and result formatting |
| Text Search | Content-based search that finds files containing specific terms or phrases, using the inverted index and BM25 ranking for relevance scoring |
| File Search | Path-based search that finds files by name pattern matching using glob syntax, without examining file contents |
| Grep Search | Pattern-based search that finds lines matching literal strings or regular expressions across indexed files |
| Snippet | A code excerpt extracted from search results showing the matching content with surrounding context lines for understanding |
| Rate Limiting | A mechanism that restricts the frequency of search operations per session to prevent resource exhaustion and ensure responsive interactions |
| Token Budget | The maximum number of tokens available for context in the current LLM interaction, which search tools respect when formatting results |
| Result Cap | The maximum number of results returned by a search operation, enforced to prevent overwhelming the agent with too many matches |
| Relevance Score | A normalized value (0-1) indicating how well a search result matches the query, computed using BM25 algorithm |
| Query Validation | The process of sanitizing and validating search input before execution, including regex compilation and length checks |
| Context Lines | The number of lines before and after a match included in snippets to provide surrounding context |
| Tool Registry | The central registry where all agent tools are registered and discovered by the agent orchestrator |
| Retry-After | The duration the agent should wait before retrying when a rate limit is exceeded |
| Search Timeout | The maximum execution time allowed for a search operation before it is cancelled |

---

## Use Cases

### Use Case 1: Agent Discovers Implementation Details

**Persona:** DevBot, an AI coding assistant, has been asked to modify the authentication flow in a large enterprise application. The user mentioned "authentication" but didn't specify which files to modify.

**Before (No Search Tools):**
DevBot has no way to discover authentication-related code autonomously. It asks the user: "Which files contain the authentication implementation?" The user spends 5 minutes manually finding files: `AuthService.cs`, `LoginController.cs`, `JwtHandler.cs`, and provides them. DevBot reads all three but misses `AuthMiddleware.cs` which also needs changes. The user discovers the bug in testing and has to provide more context.

**After (With Search Tools):**
DevBot uses the search_text tool:
```json
{"query": "authentication login", "file_type": ".cs", "max_results": 20}
```

Results return instantly:
```json
{
  "totalCount": 23,
  "results": [
    {"path": "src/Services/AuthService.cs", "line": 15, "score": 0.95, "snippet": "public class AuthService : IAuthService"},
    {"path": "src/Controllers/LoginController.cs", "line": 8, "score": 0.89, "snippet": "public async Task<IActionResult> Login(...)"},
    {"path": "src/Middleware/AuthMiddleware.cs", "line": 22, "score": 0.87, "snippet": "if (!await ValidateToken(...)"},
    {"path": "src/Security/JwtHandler.cs", "line": 42, "score": 0.82, "snippet": "public string GenerateToken(User user)"}
  ]
}
```

DevBot discovers all four relevant files, including the middleware the user forgot. The change is complete on first attempt.

**Quantified Improvement:**
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Time to find files | 5+ min (user) | 2 sec (agent) | 150× faster |
| Files missed | 1 (bug) | 0 | 100% discovery |
| User interaction required | 3 rounds | 0 rounds | Autonomous |
| Task completion | Incomplete | Complete | First attempt |

---

### Use Case 2: Agent Locates All Usages for Refactoring

**Persona:** Jordan, a senior developer, asks the agent to rename the `getUserById` method to `findUserById` across the entire codebase.

**Before (No Search Tools):**
The agent can only modify files Jordan explicitly provides. Jordan guesses which files use `getUserById` and provides 5 files. The agent makes changes, but the build fails because 3 other files also called this method. Jordan has to manually grep, find the files, and restart.

**After (With Search Tools):**
The agent uses grep tool to find all usages:
```json
{"pattern": "getUserById", "regex": false, "max_results": 100}
```

Results:
```json
{
  "totalCount": 12,
  "results": [
    {"path": "src/Services/UserService.cs", "line": 45, "snippet": "return getUserById(id);"},
    {"path": "src/Controllers/UserController.cs", "line": 23, "snippet": "var user = _service.getUserById(request.UserId);"},
    {"path": "src/Api/UserApi.cs", "line": 67, "snippet": "getUserById(userId)"},
    ... (9 more)
  ]
}
```

The agent systematically updates all 12 occurrences. Build passes on first try.

**Quantified Improvement:**
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Files found | 5/12 (42%) | 12/12 (100%) | Complete coverage |
| Build attempts | 3+ | 1 | 67% fewer iterations |
| Total time | 25 min | 3 min | 88% faster |
| Missed usages | 7 | 0 | Zero regressions |

---

### Use Case 3: Agent Finds Configuration Files

**Persona:** Alex, an ops engineer, asks the agent to update the database connection string across all environments.

**Before (No Search Tools):**
Alex has to tell the agent where config files are located. In a complex deployment with multiple environments and frameworks, configuration could be in `appsettings.json`, `web.config`, `docker-compose.yml`, `.env` files, or Kubernetes manifests. Alex forgets about the staging environment config.

**After (With Search Tools):**
The agent uses file search to find config files:
```json
{"pattern": "**/appsettings*.json"}
```
Then:
```json
{"pattern": "**/*.config"}
```
And finally grep for the connection string:
```json
{"pattern": "ConnectionString|DATABASE_URL", "regex": true}
```

Results reveal configuration in 7 different files across 4 deployment environments. The agent updates all of them.

**Quantified Improvement:**
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Environments updated | 3/4 (missing staging) | 4/4 | Complete |
| Config files found | User-provided subset | All 7 | Comprehensive |
| Deployment failures | 1 (staging) | 0 | Zero missed configs |
| Time to locate configs | 8 min manual | 10 sec | 48× faster |

---

### Use Case 4: Agent Handles Rate Limits Gracefully

**Persona:** Robin, a developer, asks the agent to "understand this codebase and summarize the main components."

**Before (No Rate Limiting):**
The agent fires off 150 search queries in rapid succession trying to understand everything at once. The system becomes unresponsive. Searches queue up. Robin's interaction hangs for 2 minutes. Eventually, the agent gets partial results in random order and produces a confused summary.

**After (With Rate Limiting):**
The agent's first 30 searches execute normally, building understanding. On the 31st search:
```json
{
  "error": "rate_limit_exceeded",
  "code": "ACODE-SRC-003",
  "message": "Search rate limit exceeded (30/min). Please wait before retrying.",
  "retry_after_seconds": 25,
  "searches_remaining": 0
}
```

The agent adjusts strategy: it processes the 30 results it has, synthesizes an initial summary, then waits 25 seconds. After the window resets, it continues with targeted follow-up searches. Robin sees continuous progress instead of a hang.

**Quantified Improvement:**
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| System responsiveness | Degraded (2 min hang) | Maintained | UX preserved |
| Results quality | Random/partial | Organized/complete | Coherent output |
| Agent strategy | Blind flooding | Adaptive pacing | Intelligent behavior |
| User experience | Frustrating | Smooth progress | Satisfaction |

---

## Out of Scope

The following items are explicitly excluded from Task 015.b:

1. **Semantic Search** - Vector-based similarity search using embeddings is deferred to v2. Current implementation uses keyword/BM25 ranking only.

2. **Symbol Search** - Code-aware symbol navigation (find definition, find references) is handled by Task 017 which integrates with language servers.

3. **Cross-Repository Search** - Search is scoped to the currently indexed repository only. Multi-repo search across workspaces is not supported.

4. **Real-Time Streaming Results** - Results are returned as a complete batch. Streaming partial results as they're found is not implemented.

5. **Search History Persistence** - Previous search queries are not persisted between sessions. Each session starts fresh with no history.

6. **Search Query Suggestions** - No autocomplete or "did you mean" suggestions are provided. The agent formulates complete queries.

7. **Natural Language Query Parsing** - The search tools expect structured queries. Natural language parsing ("find where we handle user login") is not supported.

8. **Fuzzy Matching by Default** - Searches use exact matching. Typo tolerance or edit-distance matching is not included in this scope.

9. **Result Bookmarking** - There is no mechanism to save or bookmark search results for later reference within the agent session.

10. **Search Analytics Dashboard** - While metrics are collected, no dashboard or UI for viewing search analytics is included.

---

## Functional Requirements

### Text Search Tool

| ID | Requirement |
|----|-------------|
| FR-015b-01 | The system MUST expose a search_text tool in the tool registry |
| FR-015b-02 | The tool MUST require a query parameter specifying the search terms |
| FR-015b-03 | The tool MUST return a list of matching files with their paths |
| FR-015b-04 | The tool MUST return line numbers for each match location |
| FR-015b-05 | The tool MUST return code snippets showing the matching content with context |
| FR-015b-06 | The tool MUST return relevance scores for result ranking |

### File Search Tool

| ID | Requirement |
|----|-------------|
| FR-015b-07 | The system MUST expose a search_files tool in the tool registry |
| FR-015b-08 | The tool MUST require a pattern parameter for matching file paths |
| FR-015b-09 | The tool MUST return all matching file paths |
| FR-015b-10 | The tool MUST support glob wildcards (* and **) in patterns |
| FR-015b-11 | The tool MUST support optional directory filtering to scope the search |

### Grep Tool

| ID | Requirement |
|----|-------------|
| FR-015b-12 | The system MUST expose a grep tool in the tool registry |
| FR-015b-13 | The tool MUST require a pattern parameter for content matching |
| FR-015b-14 | The tool MUST return all lines matching the pattern across all files |
| FR-015b-15 | The tool MUST support regular expression patterns when regex flag is set |
| FR-015b-16 | The tool MUST support case sensitivity options for pattern matching |

### Tool Parameters

| ID | Requirement |
|----|-------------|
| FR-015b-17 | All search tools MUST support a max_results parameter to limit output |
| FR-015b-18 | All search tools MUST support an include_path filter for scoping searches |
| FR-015b-19 | All search tools MUST support an exclude_path filter for excluding paths |
| FR-015b-20 | Text and grep tools MUST support a file_type filter for extension filtering |
| FR-015b-21 | Text and grep tools MUST support a context_lines parameter for snippet size |

### Tool Results

| ID | Requirement |
|----|-------------|
| FR-015b-22 | All tool results MUST be returned in structured JSON format |
| FR-015b-23 | Each result MUST include the full relative file path |
| FR-015b-24 | Content results MUST include the line number of the match |
| FR-015b-25 | Content results MUST include a snippet of the matching content with context |
| FR-015b-26 | Text search results MUST include a relevance score between 0 and 1 |
| FR-015b-27 | All results MUST include a total count of matches found |

### Rate Limiting

| ID | Requirement |
|----|-------------|
| FR-015b-28 | The system MUST enforce per-session limits on search invocations |
| FR-015b-29 | Rate limits MUST be configurable via .agent/config.yml |
| FR-015b-30 | The system MUST notify the agent when rate limits are approached or exceeded |
| FR-015b-31 | Rate limit responses MUST include a suggested backoff duration |

### Error Handling

| ID | Requirement |
|----|-------------|
| FR-015b-32 | The system MUST return a clear error when the index is not ready |
| FR-015b-33 | The system MUST return a clear error for malformed or invalid queries |
| FR-015b-34 | The system MUST return a meaningful message when no results are found |
| FR-015b-35 | The system MUST handle search timeouts gracefully with partial results if available |

### Integration

| ID | Requirement |
|----|-------------|
| FR-015b-36 | All search tools MUST register with the central tool registry on startup |
| FR-015b-37 | All search tool invocations MUST be logged with query, timing, and result count |
| FR-015b-38 | All search tools MUST emit metrics for latency, result count, and error rate |
| FR-015b-39 | Search tools MUST integrate with context budget and reduce results when budget is tight |

### Tool Discovery

| ID | Requirement |
|----|-------------|
| FR-015b-40 | Each search tool MUST provide a JSON schema describing its parameters |
| FR-015b-41 | Each search tool MUST provide a description suitable for agent reasoning |
| FR-015b-42 | Tool schemas MUST indicate which parameters are required vs optional |
| FR-015b-43 | Tool schemas MUST specify parameter types and valid value ranges |
| FR-015b-44 | The tool registry MUST support querying available search tools by capability |

### Advanced Search Features

| ID | Requirement |
|----|-------------|
| FR-015b-45 | Text search MUST support OR operator to match any of multiple terms |
| FR-015b-46 | Text search MUST support phrase queries using quoted strings |
| FR-015b-47 | Text search MUST support exclusion of terms using minus prefix |
| FR-015b-48 | Grep tool MUST support whole-word matching option |
| FR-015b-49 | Grep tool MUST support inverted match (lines NOT matching pattern) |
| FR-015b-50 | File search MUST support multiple patterns in a single query |

### Security

| ID | Requirement |
|----|-------------|
| FR-015b-51 | Search queries MUST be sanitized before execution to prevent injection |
| FR-015b-52 | Regex patterns MUST be compiled with timeout to prevent ReDoS |
| FR-015b-53 | Result snippets MUST redact sensitive content matched by redaction patterns |
| FR-015b-54 | Search queries MUST be logged with sensitive values masked |
| FR-015b-55 | The system MUST reject queries exceeding maximum length limits |

---

## Non-Functional Requirements

### Performance

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015b-01 | Performance | Text search execution MUST complete in less than 100ms for typical queries |
| NFR-015b-02 | Performance | Result formatting and serialization MUST complete in less than 50ms |
| NFR-015b-03 | Performance | Total tool execution time MUST be less than 200ms end-to-end |

### Reliability

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015b-04 | Reliability | Search tools MUST degrade gracefully when the index is unavailable |
| NFR-015b-05 | Reliability | Search timeouts MUST be enforced to prevent runaway operations |
| NFR-015b-06 | Reliability | The system MUST recover from individual search errors without affecting other operations |

### Usability

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015b-07 | Usability | Error messages MUST be clear and actionable for the AI agent |
| NFR-015b-08 | Usability | Result format MUST be optimized for LLM token efficiency |
| NFR-015b-09 | Usability | Tool documentation MUST be comprehensive for agent understanding |

### Maintainability

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015b-10 | Maintainability | Search tools MUST follow the standard tool interface pattern |
| NFR-015b-11 | Maintainability | Rate limiting logic MUST be reusable across different tool types |
| NFR-015b-12 | Maintainability | Result formatting MUST be centralized for consistent output |

### Observability

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-015b-13 | Observability | Search latency percentiles MUST be available in metrics |
| NFR-015b-14 | Observability | Rate limit violations MUST be logged and counted |
| NFR-015b-15 | Observability | Search queries MUST be traceable through the logging system |

---

## User Manual Documentation

### Overview

The search tools enable the agent to find code in the repository. Three search modes are available.

### Search Text Tool

Searches file contents:

```json
{
  "tool": "search_text",
  "parameters": {
    "query": "UserService",
    "max_results": 10,
    "file_type": ".cs",
    "context_lines": 2
  }
}
```

Result:
```json
{
  "total": 15,
  "results": [
    {
      "path": "src/Services/UserService.cs",
      "line": 10,
      "score": 0.95,
      "snippet": "public class UserService : IUserService",
      "context": {
        "before": ["namespace MyApp.Services", "{"],
        "after": ["{", "    private readonly IUserRepository _repo;"]
      }
    }
  ]
}
```

### Search Files Tool

Searches file paths:

```json
{
  "tool": "search_files",
  "parameters": {
    "pattern": "*Controller*.cs",
    "directory": "src"
  }
}
```

Result:
```json
{
  "total": 5,
  "results": [
    { "path": "src/Controllers/UserController.cs" },
    { "path": "src/Controllers/OrderController.cs" }
  ]
}
```

### Grep Tool

Pattern matching:

```json
{
  "tool": "grep",
  "parameters": {
    "pattern": "TODO:|FIXME:",
    "regex": true,
    "include_path": "src/**"
  }
}
```

Result:
```json
{
  "total": 8,
  "results": [
    {
      "path": "src/Services/OrderService.cs",
      "line": 45,
      "match": "// TODO: Add validation"
    }
  ]
}
```

### Configuration

```yaml
# .agent/config.yml
tools:
  search:
    # Default max results
    default_max_results: 20
    
    # Rate limits
    rate_limit:
      searches_per_minute: 30
      
    # Timeout
    timeout_seconds: 10
    
    # Context lines for snippets
    default_context_lines: 2
```

### Rate Limiting

When rate limited:

```json
{
  "error": "rate_limit_exceeded",
  "message": "Search rate limit reached (30/min)",
  "retry_after_seconds": 45
}
```

### Troubleshooting

#### Issue 1: Search Returns No Results

**Symptoms:**
- `search_text` returns empty results array with `totalCount: 0`
- Agent reports "no matches found" when user expects results
- Known code content is not being discovered

**Causes:**
- Query terms misspelled or using wrong terminology
- File type filter too restrictive (e.g., `.cs` when target is `.ts`)
- Index not built or outdated (files added after last index)
- Search terms not in indexed files (ignored via .gitignore)
- Case sensitivity mismatch on case-sensitive systems

**Solutions:**

```bash
# 1. Verify the index is current
acode index status
# If "Pending updates" > 0, rebuild:
acode index update

# 2. Check if target file is ignored
acode ignore check src/target-file.cs

# 3. Use the CLI to test the same query
acode search "your query here"

# 4. Try broader query without filters
# Instead of: {"query": "authenticateUser", "file_type": ".cs"}
# Try: {"query": "authenticate"}

# 5. Check index contains expected files
acode index stats
# Look for "Files indexed" count
```

---

#### Issue 2: Too Many Irrelevant Results

**Symptoms:**
- Search returns hundreds of results with low relevance scores
- Top results don't match what the agent is looking for
- Agent gets confused by noise in results

**Causes:**
- Query too generic (e.g., "get" matches thousands of getters)
- No filters applied (searching entire codebase)
- Common terms in query (e.g., "data", "value", "result")
- max_results too high, including low-quality matches

**Solutions:**

```bash
# 1. Add file type filter to scope results
{"query": "UserService", "file_type": ".cs"}

# 2. Add path filter to focus on specific area
{"query": "UserService", "include_path": "src/Services"}

# 3. Use more specific query terms
# Instead of: "get user"
# Use: "GetUserById" or "UserRepository.Find"

# 4. Reduce max_results to get only top matches
{"query": "UserService", "max_results": 10}

# 5. Combine filters for precision
{
  "query": "authenticate",
  "file_type": ".cs",
  "include_path": "src/Security",
  "max_results": 15
}
```

---

#### Issue 3: Index Not Ready Error (ACODE-SRC-001)

**Symptoms:**
- Search returns error: "Index not ready. Please build the index first."
- Error code `ACODE-SRC-001` in response
- All search tools fail with same error

**Causes:**
- Index has never been built for this repository
- Index file was deleted or corrupted
- Agent session started before index build completed
- Working in a new or uninitialized workspace

**Solutions:**

```bash
# 1. Build the index manually
acode index build
# Wait for completion message

# 2. Check if index file exists
ls -la .agent/index.db
# If missing, build is required

# 3. Verify index status
acode index status
# Should show "Status: Ready"

# 4. If index seems stuck, rebuild
acode index rebuild

# 5. Check for index errors in logs
acode logs --filter "index" --last 50
```

---

#### Issue 4: Rate Limit Exceeded (ACODE-SRC-003)

**Symptoms:**
- Search returns `rate_limit_exceeded` error
- Agent receives `retry_after_seconds` value
- Subsequent searches also fail until window resets

**Causes:**
- Agent fired too many searches in short period
- Default limit (30/min) exceeded
- Aggressive exploration strategy by agent
- Multiple agent sessions sharing same limits

**Solutions:**

```bash
# 1. Wait for retry_after_seconds duration
# Error response includes: "retry_after_seconds": 25
# Wait at least 25 seconds before retrying

# 2. Increase rate limits in config if needed
cat >> .agent/config.yml << 'EOF'
tools:
  search:
    rate_limit_per_minute: 60
EOF

# 3. Check current rate limit settings
acode config show | grep rate_limit

# 4. Implement batching - combine related searches
# Instead of 5 separate searches:
# Use: {"query": "UserService OR AuthService OR SessionService"}

# 5. For agent development, temporarily disable limits
tools:
  search:
    rate_limit_enabled: false  # Development only!
```

---

#### Issue 5: Search Timeout (ACODE-SRC-004)

**Symptoms:**
- Search fails with timeout error after extended wait
- Partial results may be returned
- Complex regex patterns cause timeouts

**Causes:**
- Very large codebase with slow index queries
- Complex regex pattern with backtracking
- Search across too many files without filters
- System under heavy load
- Index file on slow storage

**Solutions:**

```bash
# 1. Add filters to reduce search scope
{"query": "pattern", "include_path": "src/", "file_type": ".cs"}

# 2. For regex, simplify the pattern
# SLOW: "function.*\(.*\).*\{.*return.*\}"
# FAST: "function.*return"

# 3. Increase timeout if needed (config.yml)
tools:
  search:
    timeout_seconds: 30  # Default is 10

# 4. Check index performance
acode index stats
# If "Index size" very large, consider excluding paths

# 5. Use file search first, then targeted grep
# Step 1: {"pattern": "**/*Service.cs"} -> Get file list
# Step 2: {"pattern": "methodName", "include_path": "specific/path"}
```

---

#### Issue 6: Grep Regex Error (ACODE-SRC-002)

**Symptoms:**
- Grep tool returns "Invalid regex pattern" error
- Error includes position of syntax problem
- Literal search works but regex mode fails

**Causes:**
- Unescaped special characters in regex
- Unbalanced brackets or parentheses
- Invalid escape sequences
- Regex too complex for timeout protection

**Solutions:**

```bash
# 1. Check regex syntax before using
# Common mistakes:
# - "(" should be "\(" if matching literal
# - "[" should be "\[" if matching literal
# - "." matches any char, use "\." for literal dot

# 2. Use regex: false for literal searches
{"pattern": "obj.Method()", "regex": false}

# 3. Escape special characters properly
# To find "array[0]", use: "array\[0\]"

# 4. Test regex in isolation
acode grep "your\\.pattern" --test-only

# 5. Simplify complex patterns
# Instead of: "function\s+\w+\s*\([^)]*\)\s*\{"
# Use: "function \w+"  # Then filter results manually
```

---

## Acceptance Criteria

### Category 1: Text Search Tool Registration

- [ ] AC-001: search_text tool is registered in the tool registry on startup
- [ ] AC-002: Tool appears in tool listing with correct name and description
- [ ] AC-003: Tool schema defines query as required parameter
- [ ] AC-004: Tool schema defines optional max_results, file_type, context_lines parameters
- [ ] AC-005: Tool description is clear for agent understanding of when to use

### Category 2: Text Search Functionality

- [ ] AC-006: Single-word query returns matching files with relevance scores
- [ ] AC-007: Multi-word query uses AND logic by default
- [ ] AC-008: Phrase query (quoted) matches exact phrase
- [ ] AC-009: Wildcard patterns (prefix*, *suffix) work correctly
- [ ] AC-010: Case-insensitive matching is default behavior
- [ ] AC-011: Results are sorted by relevance score (highest first)
- [ ] AC-012: Snippets include matched content with highlighting info
- [ ] AC-013: Context lines before/after match are included

### Category 3: Text Search Filtering

- [ ] AC-014: file_type filter limits to specific extension (e.g., ".cs")
- [ ] AC-015: Multiple file_types can be specified
- [ ] AC-016: include_path filter scopes to directory subtree
- [ ] AC-017: exclude_path filter removes paths from results
- [ ] AC-018: max_results parameter caps output count
- [ ] AC-019: context_lines parameter controls snippet size

### Category 4: File Search Tool

- [ ] AC-020: search_files tool is registered in the tool registry
- [ ] AC-021: Exact filename matches return correct path
- [ ] AC-022: Wildcard pattern * matches any sequence in filename
- [ ] AC-023: Double wildcard ** matches across directory levels
- [ ] AC-024: Extension pattern (*.cs) matches all files with extension
- [ ] AC-025: Directory filter scopes search to subtree
- [ ] AC-026: Results include relative file paths
- [ ] AC-027: Results include file size and modification date
- [ ] AC-028: max_results parameter limits output

### Category 5: Grep Tool

- [ ] AC-029: grep tool is registered in the tool registry
- [ ] AC-030: Literal string pattern matches exactly
- [ ] AC-031: Regex mode enabled via regex=true flag
- [ ] AC-032: Case-insensitive matching via case_sensitive=false
- [ ] AC-033: Results include matching line content
- [ ] AC-034: Results include line number (1-based)
- [ ] AC-035: Results include file path
- [ ] AC-036: Context lines included when requested
- [ ] AC-037: Invalid regex returns clear error with position
- [ ] AC-038: File pattern filter limits files searched

### Category 6: Result Formatting

- [ ] AC-039: All results returned as valid JSON
- [ ] AC-040: JSON includes total count of matches
- [ ] AC-041: JSON includes array of result objects
- [ ] AC-042: Each result has path field (relative)
- [ ] AC-043: Content results have line field (1-based)
- [ ] AC-044: Content results have snippet field
- [ ] AC-045: Text search results have score field (0-1)
- [ ] AC-046: Snippets are truncated to reasonable length
- [ ] AC-047: Results are token-efficient for LLM consumption

### Category 7: Rate Limiting

- [ ] AC-048: Rate limits are enforced per tool type
- [ ] AC-049: Default limit is 30 searches per minute
- [ ] AC-050: Limits are configurable via .agent/config.yml
- [ ] AC-051: Under-limit searches execute normally
- [ ] AC-052: Over-limit searches return rate_limit_exceeded error
- [ ] AC-053: Error response includes retry_after_seconds
- [ ] AC-054: Error response includes human-readable message
- [ ] AC-055: Rate limits reset after window expires
- [ ] AC-056: Concurrent requests are handled correctly

### Category 8: Error Handling

- [ ] AC-057: Index not ready returns ACODE-SRC-001 error
- [ ] AC-058: Invalid query returns ACODE-SRC-002 error with details
- [ ] AC-059: Rate limited returns ACODE-SRC-003 error
- [ ] AC-060: Timeout returns ACODE-SRC-004 error
- [ ] AC-061: Empty query returns validation error
- [ ] AC-062: Invalid regex returns error with syntax position
- [ ] AC-063: No results returns success with empty array (not error)
- [ ] AC-064: All errors include actionable guidance

### Category 9: Performance

- [ ] AC-065: Text search completes in < 100ms average
- [ ] AC-066: File search completes in < 50ms average
- [ ] AC-067: Grep search completes in < 150ms average
- [ ] AC-068: Result formatting adds < 50ms overhead
- [ ] AC-069: Timeout enforced at configurable threshold (default 10s)
- [ ] AC-070: Concurrent searches don't block each other

### Category 10: Logging and Metrics

- [ ] AC-071: All tool invocations logged with query (sanitized)
- [ ] AC-072: Logs include tool name, result count, duration
- [ ] AC-073: Metrics recorded for search latency
- [ ] AC-074: Metrics recorded for result count distribution
- [ ] AC-075: Metrics recorded for error rates by type
- [ ] AC-076: Rate limit violations counted in metrics

### Category 11: Integration

- [ ] AC-077: Tools work with agent orchestrator loop
- [ ] AC-078: Tools respect context budget when set
- [ ] AC-079: Results can be passed to other tools
- [ ] AC-080: Tools work with streaming responses
- [ ] AC-081: Tools handle cancellation gracefully
- [ ] AC-082: Tool registration is idempotent

### Category 12: Configuration

- [ ] AC-083: Default max_results configurable (default 20)
- [ ] AC-084: Default context_lines configurable (default 2)
- [ ] AC-085: Rate limits configurable per tool
- [ ] AC-086: Timeout configurable
- [ ] AC-087: Logging level configurable
- [ ] AC-088: Secret redaction can be disabled for debugging

---

## Best Practices

### Tool Design

1. **Provide structured output** - Return JSON with file, line, match context
2. **Support filtering** - Allow file patterns, path exclusions in query
3. **Limit output size** - Cap results to prevent overwhelming context
4. **Include match context** - Return lines before/after match for context

### Agent Integration

5. **Clear tool descriptions** - Help LLM understand when to use search vs grep
6. **Validate inputs early** - Check query parameters before searching
7. **Handle empty results** - Return informative message, not just empty array
8. **Log tool invocations** - Track what searches agent performs for debugging

### Error Handling

9. **Graceful degradation** - If index unavailable, fall back to grep
10. **Timeout protection** - Cancel searches exceeding time limit
11. **Report partial results** - Return what was found if search interrupted
12. **Informative errors** - Include what went wrong and how to fix

---

## Testing Requirements

### Unit Tests

#### SearchTextToolTests.cs

```csharp
using Acode.Application.Tools.Search;
using Acode.Domain.Index;
using Acode.Domain.Tools;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Acode.Application.Tests.Tools.Search;

public class SearchTextToolTests
{
    private readonly IIndexService _indexService = Substitute.For<IIndexService>();
    private readonly ISearchRateLimiter _rateLimiter = Substitute.For<ISearchRateLimiter>();
    private readonly SearchTextTool _tool;

    public SearchTextToolTests()
    {
        _rateLimiter.TryAcquireAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(RateLimitResult.Allowed(29, DateTime.UtcNow.AddMinutes(1)));
        _tool = new SearchTextTool(_indexService, _rateLimiter);
    }

    [Fact]
    public async Task Should_Search_Single_Word()
    {
        // Arrange
        var input = new ToolInput(new Dictionary<string, object>
        {
            ["query"] = "UserService"
        });
        
        _indexService.SearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new SearchResultList(new[]
            {
                new SearchResult
                {
                    FilePath = "src/Services/UserService.cs",
                    Score = 0.95,
                    MatchedLines = new[] { 10 },
                    Snippets = new[] { new Snippet { Text = "public class UserService", StartLine = 10 } }
                }
            }, 1));
        
        // Act
        var result = await _tool.ExecuteAsync(input, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Contain("UserService.cs");
        result.Data.Should().Contain("0.95");
    }

    [Fact]
    public async Task Should_Search_Multiple_Words()
    {
        // Arrange
        var input = new ToolInput(new Dictionary<string, object>
        {
            ["query"] = "User Service validation"
        });
        
        _indexService.SearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new SearchResultList(Array.Empty<SearchResult>(), 0));
        
        // Act
        var result = await _tool.ExecuteAsync(input, CancellationToken.None);
        
        // Assert
        await _indexService.Received(1).SearchAsync(
            Arg.Is<SearchQuery>(q => q.Text == "User Service validation"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Filter_By_File_Type()
    {
        // Arrange
        var input = new ToolInput(new Dictionary<string, object>
        {
            ["query"] = "Controller",
            ["file_type"] = ".cs"
        });
        
        _indexService.SearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new SearchResultList(Array.Empty<SearchResult>(), 0));
        
        // Act
        await _tool.ExecuteAsync(input, CancellationToken.None);
        
        // Assert
        await _indexService.Received(1).SearchAsync(
            Arg.Is<SearchQuery>(q => q.Filter!.Extensions!.Contains(".cs")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Filter_By_Directory()
    {
        // Arrange
        var input = new ToolInput(new Dictionary<string, object>
        {
            ["query"] = "Service",
            ["include_path"] = "src/Services"
        });
        
        _indexService.SearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new SearchResultList(Array.Empty<SearchResult>(), 0));
        
        // Act
        await _tool.ExecuteAsync(input, CancellationToken.None);
        
        // Assert
        await _indexService.Received(1).SearchAsync(
            Arg.Is<SearchQuery>(q => q.Filter!.Directory == "src/Services"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Limit_Max_Results()
    {
        // Arrange
        var input = new ToolInput(new Dictionary<string, object>
        {
            ["query"] = "test",
            ["max_results"] = 5
        });
        
        _indexService.SearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new SearchResultList(Array.Empty<SearchResult>(), 0));
        
        // Act
        await _tool.ExecuteAsync(input, CancellationToken.None);
        
        // Assert
        await _indexService.Received(1).SearchAsync(
            Arg.Is<SearchQuery>(q => q.Take == 5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_Snippets()
    {
        // Arrange
        var input = new ToolInput(new Dictionary<string, object>
        {
            ["query"] = "Logger"
        });
        
        _indexService.SearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new SearchResultList(new[]
            {
                new SearchResult
                {
                    FilePath = "src/Logger.cs",
                    Snippets = new[]
                    {
                        new Snippet
                        {
                            Text = "private readonly ILogger _logger;",
                            StartLine = 15,
                            EndLine = 15
                        }
                    }
                }
            }, 1));
        
        // Act
        var result = await _tool.ExecuteAsync(input, CancellationToken.None);
        
        // Assert
        result.Data.Should().Contain("private readonly ILogger _logger;");
    }

    [Fact]
    public async Task Should_Handle_No_Results()
    {
        // Arrange
        var input = new ToolInput(new Dictionary<string, object>
        {
            ["query"] = "NonExistentClassName12345"
        });
        
        _indexService.SearchAsync(Arg.Any<SearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new SearchResultList(Array.Empty<SearchResult>(), 0));
        
        // Act
        var result = await _tool.ExecuteAsync(input, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Contain("\"total\": 0");
    }

    [Fact]
    public async Task Should_Handle_Empty_Query()
    {
        // Arrange
        var input = new ToolInput(new Dictionary<string, object>
        {
            ["query"] = ""
        });
        
        // Act
        var result = await _tool.ExecuteAsync(input, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("query");
    }

    [Fact]
    public async Task Should_Validate_Input_Parameters()
    {
        // Arrange
        var input = new ToolInput(new Dictionary<string, object>
        {
            ["max_results"] = -5  // Invalid
        });
        
        // Act
        var result = await _tool.ExecuteAsync(input, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
    }
}
```

#### GrepToolTests.cs

```csharp
using Acode.Application.Tools.Search;
using Acode.Domain.Index;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Acode.Application.Tests.Tools.Search;

public class GrepToolTests
{
    private readonly IIndexService _indexService = Substitute.For<IIndexService>();
    private readonly ISearchRateLimiter _rateLimiter = Substitute.For<ISearchRateLimiter>();
    private readonly GrepTool _tool;

    public GrepToolTests()
    {
        _rateLimiter.TryAcquireAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(RateLimitResult.Allowed(29, DateTime.UtcNow.AddMinutes(1)));
        _tool = new GrepTool(_indexService, _rateLimiter);
    }

    [Fact]
    public async Task Should_Match_Literal_String()
    {
        // Arrange
        var input = new ToolInput(new Dictionary<string, object>
        {
            ["pattern"] = "TODO: Fix this"
        });
        
        _indexService.GrepAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new GrepResultList(new[]
            {
                new GrepResult("src/Service.cs", 42, "// TODO: Fix this later")
            }, 1));
        
        // Act
        var result = await _tool.ExecuteAsync(input, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Contain("TODO: Fix this");
    }

    [Fact]
    public async Task Should_Match_Regex_Pattern()
    {
        // Arrange
        var input = new ToolInput(new Dictionary<string, object>
        {
            ["pattern"] = @"TODO:|FIXME:|HACK:",
            ["regex"] = true
        });
        
        _indexService.GrepAsync(Arg.Any<string>(), Arg.Is<bool>(true), Arg.Any<CancellationToken>())
            .Returns(new GrepResultList(new[]
            {
                new GrepResult("src/A.cs", 10, "// TODO: implement"),
                new GrepResult("src/B.cs", 20, "// FIXME: broken")
            }, 2));
        
        // Act
        var result = await _tool.ExecuteAsync(input, CancellationToken.None);
        
        // Assert
        result.Data.Should().Contain("TODO");
        result.Data.Should().Contain("FIXME");
    }

    [Fact]
    public async Task Should_Handle_Case_Insensitive()
    {
        // Arrange
        var input = new ToolInput(new Dictionary<string, object>
        {
            ["pattern"] = "error",
            ["case_sensitive"] = false
        });
        
        // Act
        await _tool.ExecuteAsync(input, CancellationToken.None);
        
        // Assert - should match ERROR, Error, error
        await _indexService.Received(1).GrepAsync(
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Return_Line_Numbers()
    {
        // Arrange
        var input = new ToolInput(new Dictionary<string, object>
        {
            ["pattern"] = "private"
        });
        
        _indexService.GrepAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new GrepResultList(new[]
            {
                new GrepResult("src/Class.cs", 15, "    private readonly IService _service;")
            }, 1));
        
        // Act
        var result = await _tool.ExecuteAsync(input, CancellationToken.None);
        
        // Assert
        result.Data.Should().Contain("\"line\": 15");
    }

    [Fact]
    public async Task Should_Handle_Invalid_Regex()
    {
        // Arrange
        var input = new ToolInput(new Dictionary<string, object>
        {
            ["pattern"] = "[invalid(regex",
            ["regex"] = true
        });
        
        // Act
        var result = await _tool.ExecuteAsync(input, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("invalid");
    }

    [Fact]
    public async Task Should_Filter_By_File_Pattern()
    {
        // Arrange
        var input = new ToolInput(new Dictionary<string, object>
        {
            ["pattern"] = "class",
            ["file_pattern"] = "*.cs"
        });
        
        // Act
        await _tool.ExecuteAsync(input, CancellationToken.None);
        
        // Assert
        await _indexService.Received(1).GrepAsync(
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }
}
```

#### RateLimitingTests.cs

```csharp
using Acode.Infrastructure.Tools.Search;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Acode.Infrastructure.Tests.Tools.Search;

public class RateLimitingTests
{
    [Fact]
    public async Task Should_Allow_Under_Limit()
    {
        // Arrange
        var options = Options.Create(new SearchRateLimitOptions
        {
            MaxRequestsPerMinute = 10
        });
        var limiter = new SearchRateLimiter(options);
        
        // Act
        var result = await limiter.TryAcquireAsync("search_text");
        
        // Assert
        result.IsAllowed.Should().BeTrue();
        result.Remaining.Should().Be(9);
    }

    [Fact]
    public async Task Should_Block_Over_Limit()
    {
        // Arrange
        var options = Options.Create(new SearchRateLimitOptions
        {
            MaxRequestsPerMinute = 3
        });
        var limiter = new SearchRateLimiter(options);
        
        // Act - exhaust limit
        await limiter.TryAcquireAsync("search_text");
        await limiter.TryAcquireAsync("search_text");
        await limiter.TryAcquireAsync("search_text");
        var result = await limiter.TryAcquireAsync("search_text");
        
        // Assert
        result.IsAllowed.Should().BeFalse();
        result.RetryAfter.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Return_Retry_After()
    {
        // Arrange
        var options = Options.Create(new SearchRateLimitOptions
        {
            MaxRequestsPerMinute = 1
        });
        var limiter = new SearchRateLimiter(options);
        
        // Act
        await limiter.TryAcquireAsync("search_text");
        var result = await limiter.TryAcquireAsync("search_text");
        
        // Assert
        result.IsAllowed.Should().BeFalse();
        result.RetryAfter.Should().BeGreaterThan(TimeSpan.Zero);
        result.RetryAfter.Should().BeLessThanOrEqualTo(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Should_Track_Per_Tool()
    {
        // Arrange
        var options = Options.Create(new SearchRateLimitOptions
        {
            MaxRequestsPerMinute = 2
        });
        var limiter = new SearchRateLimiter(options);
        
        // Act
        await limiter.TryAcquireAsync("search_text");
        await limiter.TryAcquireAsync("search_text");
        var textResult = await limiter.TryAcquireAsync("search_text");
        var grepResult = await limiter.TryAcquireAsync("grep");
        
        // Assert
        textResult.IsAllowed.Should().BeFalse();
        grepResult.IsAllowed.Should().BeTrue(); // Different tool
    }

    [Fact]
    public async Task Should_Handle_Concurrent_Requests()
    {
        // Arrange
        var options = Options.Create(new SearchRateLimitOptions
        {
            MaxRequestsPerMinute = 100
        });
        var limiter = new SearchRateLimiter(options);
        
        // Act
        var tasks = Enumerable.Range(0, 50)
            .Select(_ => limiter.TryAcquireAsync("search_text"))
            .ToArray();
        var results = await Task.WhenAll(tasks);
        
        // Assert
        results.Count(r => r.IsAllowed).Should().Be(50);
    }
}
```

#### SearchResultFormatterTests.cs

```csharp
using Acode.Domain.Index;
using Acode.Infrastructure.Tools.Search;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Acode.Infrastructure.Tests.Tools.Search;

public class SearchResultFormatterTests
{
    private readonly SearchResultFormatter _formatter = new();

    [Fact]
    public void Should_Format_Single_Result()
    {
        // Arrange
        var results = new SearchResultList(new[]
        {
            new SearchResult
            {
                FilePath = "src/Service.cs",
                Score = 0.85,
                MatchedLines = new[] { 10 },
                Snippets = new[] { new Snippet { Text = "public class Service", StartLine = 10 } }
            }
        }, 1);
        
        // Act
        var json = _formatter.Format(results);
        
        // Assert
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("total").GetInt32().Should().Be(1);
        doc.RootElement.GetProperty("results").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public void Should_Include_Snippet()
    {
        // Arrange
        var results = new SearchResultList(new[]
        {
            new SearchResult
            {
                FilePath = "test.cs",
                Snippets = new[] { new Snippet { Text = "var x = 42;", StartLine = 5 } }
            }
        }, 1);
        
        // Act
        var json = _formatter.Format(results);
        
        // Assert
        json.Should().Contain("var x = 42;");
    }

    [Fact]
    public void Should_Truncate_Long_Snippets()
    {
        // Arrange
        var longSnippet = new string('x', 1000);
        var results = new SearchResultList(new[]
        {
            new SearchResult
            {
                FilePath = "test.cs",
                Snippets = new[] { new Snippet { Text = longSnippet, StartLine = 1 } }
            }
        }, 1);
        
        // Act
        var json = _formatter.Format(results);
        
        // Assert
        json.Length.Should().BeLessThan(longSnippet.Length + 200);
    }

    [Fact]
    public void Should_Handle_Empty_Results()
    {
        // Arrange
        var results = new SearchResultList(Array.Empty<SearchResult>(), 0);
        
        // Act
        var json = _formatter.Format(results);
        
        // Assert
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("total").GetInt32().Should().Be(0);
        doc.RootElement.GetProperty("results").GetArrayLength().Should().Be(0);
    }
}
```

### Integration Tests

#### SearchToolIntegrationTests.cs

```csharp
using System.IO;
using Acode.Application.Tools.Search;
using Acode.Infrastructure.Index;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Acode.Integration.Tests.Tools.Search;

public class SearchToolIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ServiceProvider _services;

    public SearchToolIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        
        // Create test files
        File.WriteAllText(Path.Combine(_tempDir, "Service.cs"), 
            "public class UserService { }");
        File.WriteAllText(Path.Combine(_tempDir, "Controller.cs"),
            "public class UserController { private UserService _svc; }");
        
        // Build services with real index
        _services = new ServiceCollection()
            .AddIndexing()
            .AddSearchTools()
            .BuildServiceProvider();
        
        // Build index
        var indexService = _services.GetRequiredService<IIndexService>();
        indexService.BuildAsync(new IndexBuildOptions { RootPath = _tempDir }).Wait();
    }

    [Fact]
    public async Task Should_Work_With_Real_Index()
    {
        // Arrange
        var tool = _services.GetRequiredService<SearchTextTool>();
        var input = new ToolInput(new Dictionary<string, object>
        {
            ["query"] = "UserService"
        });
        
        // Act
        var result = await tool.ExecuteAsync(input, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Contain("Service.cs");
    }

    [Fact]
    public async Task Should_Return_Correct_Results()
    {
        // Arrange
        var tool = _services.GetRequiredService<SearchTextTool>();
        var input = new ToolInput(new Dictionary<string, object>
        {
            ["query"] = "UserController"
        });
        
        // Act
        var result = await tool.ExecuteAsync(input, CancellationToken.None);
        
        // Assert
        result.Data.Should().Contain("Controller.cs");
        result.Data.Should().NotContain("Service.cs"); // Less relevant
    }

    [Fact]
    public async Task Should_Handle_Concurrent_Searches()
    {
        // Arrange
        var tool = _services.GetRequiredService<SearchTextTool>();
        var tasks = Enumerable.Range(0, 10)
            .Select(i => tool.ExecuteAsync(
                new ToolInput(new Dictionary<string, object> { ["query"] = "class" }),
                CancellationToken.None));
        
        // Act
        var results = await Task.WhenAll(tasks);
        
        // Assert
        results.All(r => r.IsSuccess).Should().BeTrue();
    }

    public void Dispose()
    {
        _services.Dispose();
        try { Directory.Delete(_tempDir, true); } catch { }
    }
}
```

### E2E Tests

#### SearchToolE2ETests.cs

```csharp
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Acode.E2E.Tests.Tools.Search;

public class SearchToolE2ETests : IDisposable
{
    private readonly string _tempDir;

    public SearchToolE2ETests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        
        // Create test files
        File.WriteAllText(Path.Combine(_tempDir, "App.cs"),
            "public class Application { public void Run() { } }");
        
        // Build index
        RunAcode("index build");
    }

    [Fact]
    public void Should_Execute_Search_Text_Tool()
    {
        // Act
        var output = RunAcode("tool invoke search_text --query Application");
        
        // Assert
        output.Should().Contain("App.cs");
        
        var json = JsonDocument.Parse(output);
        json.RootElement.GetProperty("total").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public void Should_Execute_Grep_Tool()
    {
        // Act
        var output = RunAcode("tool invoke grep --pattern \"public void\"");
        
        // Assert
        output.Should().Contain("Run");
    }

    [Fact]
    public void Should_Execute_Search_Files_Tool()
    {
        // Act
        var output = RunAcode("tool invoke search_files --pattern \"*.cs\"");
        
        // Assert
        output.Should().Contain("App.cs");
    }

    private string RunAcode(string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "acode",
            Arguments = args,
            WorkingDirectory = _tempDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        
        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        
        return output;
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }
}
```

### Performance Benchmarks

```csharp
using BenchmarkDotNet.Attributes;
using Acode.Application.Tools.Search;
using Acode.Domain.Tools;

namespace Acode.Benchmarks.Tools.Search;

[MemoryDiagnoser]
public class SearchToolBenchmarks
{
    private SearchTextTool _textTool = null!;
    private SearchFilesTool _filesTool = null!;
    private GrepTool _grepTool = null!;
    private ToolInput _simpleInput = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup with real index on test repository
        _simpleInput = new ToolInput(new Dictionary<string, object>
        {
            ["query"] = "class"
        });
    }

    [Benchmark]
    public async Task<ToolResult> TextSearch()
    {
        return await _textTool.ExecuteAsync(_simpleInput, CancellationToken.None);
    }

    [Benchmark]
    public async Task<ToolResult> FileSearch()
    {
        var input = new ToolInput(new Dictionary<string, object>
        {
            ["pattern"] = "*.cs"
        });
        return await _filesTool.ExecuteAsync(input, CancellationToken.None);
    }

    [Benchmark]
    public async Task<ToolResult> GrepSearch()
    {
        var input = new ToolInput(new Dictionary<string, object>
        {
            ["pattern"] = "TODO"
        });
        return await _grepTool.ExecuteAsync(input, CancellationToken.None);
    }
}
```

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| TextSearch | 50ms | 100ms |
| FileSearch | 25ms | 50ms |
| GrepSearch | 75ms | 150ms |

---

## User Verification Steps

### Scenario 1: Text Search Basic Functionality

**Objective:** Verify the search_text tool returns relevant results.

```bash
# Setup: Create test repository
mkdir -p /tmp/search-test/src && cd /tmp/search-test

# Create source files
cat > src/UserService.cs << 'EOF'
namespace MyApp.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        
        public async Task<User> GetUserAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }
    }
}
EOF

cat > src/OrderService.cs << 'EOF'
namespace MyApp.Services
{
    public class OrderService : IOrderService
    {
        private readonly UserService _userService;
        
        public async Task<Order> CreateOrderAsync(int userId)
        {
            var user = await _userService.GetUserAsync(userId);
            return new Order { UserId = user.Id };
        }
    }
}
EOF

# Build index
acode index build

# Test 1: Basic query
acode tool invoke search_text --query "UserService"
# Expected: Results from both files (UserService.cs higher score)

# Test 2: Verify snippets
acode tool invoke search_text --query "GetUserAsync" --context_lines 2
# Expected: Snippet shows the method with 2 lines before/after

# Test 3: Verify line numbers
acode tool invoke search_text --query "IUserRepository"
# Expected: Result shows line 5

# Cleanup
cd / && rm -rf /tmp/search-test
```

### Scenario 2: Text Search with Filters

**Objective:** Verify filtering by file type and directory.

```bash
# Setup
mkdir -p /tmp/filter-test/src/api /tmp/filter-test/src/web && cd /tmp/filter-test

cat > src/api/Handler.cs << 'EOF'
public class ApiHandler { }
EOF

cat > src/web/Handler.ts << 'EOF'
export class WebHandler { }
EOF

cat > src/api/Service.cs << 'EOF'
public class ApiService { }
EOF

acode index build

# Test 1: Filter by extension
acode tool invoke search_text --query "Handler" --file_type ".cs"
# Expected: Only Handler.cs, not Handler.ts

# Test 2: Filter by directory
acode tool invoke search_text --query "Handler" --include_path "src/web"
# Expected: Only WebHandler from src/web

# Test 3: Combine filters
acode tool invoke search_text --query "class" --file_type ".cs" --include_path "src/api"
# Expected: Only results from src/api/*.cs

# Test 4: Limit results
acode tool invoke search_text --query "class" --max_results 1
# Expected: Only 1 result returned

# Cleanup
cd / && rm -rf /tmp/filter-test
```

### Scenario 3: File Search Tool

**Objective:** Verify file path searching with glob patterns.

```bash
# Setup
mkdir -p /tmp/file-search/src/controllers /tmp/file-search/src/services
cd /tmp/file-search

touch src/controllers/UserController.cs
touch src/controllers/OrderController.cs
touch src/services/UserService.cs
touch src/services/OrderService.cs
touch README.md

acode index build

# Test 1: Exact filename
acode tool invoke search_files --pattern "README.md"
# Expected: README.md

# Test 2: Wildcard pattern
acode tool invoke search_files --pattern "*Controller*"
# Expected: UserController.cs, OrderController.cs

# Test 3: Extension pattern
acode tool invoke search_files --pattern "*.cs"
# Expected: All 4 .cs files

# Test 4: Double wildcard
acode tool invoke search_files --pattern "**/User*.cs"
# Expected: UserController.cs, UserService.cs

# Test 5: Directory filter
acode tool invoke search_files --pattern "*.cs" --directory "src/services"
# Expected: Only UserService.cs, OrderService.cs

# Cleanup
cd / && rm -rf /tmp/file-search
```

### Scenario 4: Grep Tool with Regex

**Objective:** Verify grep pattern matching including regex.

```bash
# Setup
mkdir -p /tmp/grep-test && cd /tmp/grep-test

cat > code.cs << 'EOF'
// TODO: Implement validation
public class Handler
{
    // FIXME: This is broken
    public void Process()
    {
        // HACK: Temporary workaround
    }
}
EOF

acode index build

# Test 1: Literal match
acode tool invoke grep --pattern "TODO"
# Expected: Line 1 with TODO

# Test 2: Regex pattern
acode tool invoke grep --pattern "TODO:|FIXME:|HACK:" --regex true
# Expected: All 3 comment lines

# Test 3: Case insensitive
acode tool invoke grep --pattern "handler" --case_sensitive false
# Expected: Matches "Handler"

# Test 4: With context
acode tool invoke grep --pattern "FIXME" --context_lines 1
# Expected: FIXME line plus 1 line before/after

# Cleanup
cd / && rm -rf /tmp/grep-test
```

### Scenario 5: Rate Limiting

**Objective:** Verify rate limits are enforced.

```bash
# Setup
mkdir -p /tmp/rate-test && cd /tmp/rate-test
echo "test content" > test.txt
acode index build

# Configure low limit for testing
cat > .agent/config.yml << 'EOF'
tools:
  search:
    rate_limit:
      searches_per_minute: 3
EOF

# Test 1: Execute searches up to limit
acode tool invoke search_text --query "test"  # 1st - OK
acode tool invoke search_text --query "test"  # 2nd - OK
acode tool invoke search_text --query "test"  # 3rd - OK

# Test 2: Exceed limit
acode tool invoke search_text --query "test"  # 4th - Should fail
# Expected: Error with rate_limit_exceeded and retry_after_seconds

# Test 3: Verify error format
acode tool invoke search_text --query "test" 2>&1 | jq '.'
# Expected: {"error": "rate_limit_exceeded", "retry_after_seconds": ...}

# Cleanup
cd / && rm -rf /tmp/rate-test
```

### Scenario 6: Error Handling

**Objective:** Verify proper error responses.

```bash
# Setup
mkdir -p /tmp/error-test && cd /tmp/error-test

# Test 1: No index
acode tool invoke search_text --query "test"
# Expected: Error ACODE-SRC-001 "Index not ready"

# Build index for next tests
echo "content" > file.txt
acode index build

# Test 2: Empty query
acode tool invoke search_text --query ""
# Expected: Error ACODE-SRC-002 "Invalid query"

# Test 3: Invalid regex
acode tool invoke grep --pattern "[invalid" --regex true
# Expected: Error with regex syntax position

# Test 4: No results (not an error)
acode tool invoke search_text --query "nonexistentstring12345"
# Expected: Success with empty results array, not error

# Cleanup
cd / && rm -rf /tmp/error-test
```

### Scenario 7: Result Format Verification

**Objective:** Verify JSON output structure.

```bash
# Setup
mkdir -p /tmp/format-test && cd /tmp/format-test
echo "public class MyClass { }" > MyClass.cs
acode index build

# Test 1: Verify JSON structure
acode tool invoke search_text --query "MyClass" | jq '.'
# Expected valid JSON with structure:
# {
#   "total": 1,
#   "results": [
#     {
#       "path": "MyClass.cs",
#       "line": 1,
#       "score": 0.xx,
#       "snippet": "public class MyClass { }"
#     }
#   ]
# }

# Test 2: Verify all required fields
acode tool invoke search_text --query "MyClass" | jq '.results[0] | keys'
# Expected: ["line", "path", "score", "snippet"]

# Test 3: Verify score range
acode tool invoke search_text --query "MyClass" | jq '.results[0].score'
# Expected: Number between 0 and 1

# Cleanup
cd / && rm -rf /tmp/format-test
```

### Scenario 8: Tool Registration Verification

**Objective:** Verify tools are properly registered.

```bash
# Test 1: List all tools
acode tool list
# Expected: search_text, search_files, grep all appear

# Test 2: Get tool schema
acode tool schema search_text
# Expected: JSON schema with query (required), max_results, file_type, etc.

# Test 3: Get tool description
acode tool describe search_text
# Expected: Human-readable description of the tool

# Test 4: Invoke with --help
acode tool invoke search_text --help
# Expected: Usage information with all parameters
```

### Scenario 9: Performance Verification

**Objective:** Verify search meets performance targets.

```bash
# Setup: Create repository with many files
mkdir -p /tmp/perf-test/src && cd /tmp/perf-test

# Create 100 files
for i in $(seq 1 100); do
  echo "public class Class$i { void Method$i() { } }" > "src/Class$i.cs"
done

# Build index
acode index build

# Test 1: Measure search time
time acode tool invoke search_text --query "Method50"
# Expected: < 200ms total

# Test 2: Multiple searches
time for i in $(seq 1 10); do
  acode tool invoke search_text --query "Class" --max_results 5 > /dev/null
done
# Expected: < 2s total (< 200ms each)

# Cleanup
cd / && rm -rf /tmp/perf-test
```

### Scenario 10: Integration with Agent

**Objective:** Verify tools work in agent context.

```bash
# Setup
mkdir -p /tmp/agent-test && cd /tmp/agent-test

cat > service.cs << 'EOF'
public class PaymentService
{
    public async Task<Result> ProcessPayment(Payment payment)
    {
        // TODO: Add validation
        return await _gateway.ChargeAsync(payment);
    }
}
EOF

acode index build

# Test 1: Agent uses search to find code
acode agent "Find the PaymentService class and tell me what it does"
# Expected: Agent invokes search_text, finds PaymentService, describes it

# Test 2: Agent uses grep for patterns
acode agent "Find all TODO comments in the codebase"
# Expected: Agent invokes grep with TODO pattern

# Test 3: Agent uses file search
acode agent "List all C# files in the project"
# Expected: Agent invokes search_files with *.cs pattern

# Cleanup
cd / && rm -rf /tmp/agent-test
```

---

## Implementation Prompt

### File Structure

```
src/Acode.Domain/
├── Tools/
│   ├── ITool.cs
│   ├── IToolRegistry.cs
│   ├── ToolInput.cs
│   ├── ToolResult.cs
│   └── ToolSchema.cs
│
src/Acode.Application/
├── Tools/
│   └── Search/
│       ├── SearchTextTool.cs
│       ├── SearchFilesTool.cs
│       ├── GrepTool.cs
│       └── SearchToolOptions.cs
│
src/Acode.Infrastructure/
├── Tools/
│   └── Search/
│       ├── SearchRateLimiter.cs
│       ├── SearchResultFormatter.cs
│       ├── SearchResultRedactor.cs
│       ├── SearchQuerySanitizer.cs
│       └── SafeRegexCompiler.cs
```

---

### Domain Models

#### ITool.cs

```csharp
using System.Threading;
using System.Threading.Tasks;

namespace Acode.Domain.Tools;

/// <summary>
/// Interface for tools that can be invoked by the agent.
/// </summary>
public interface ITool
{
    /// <summary>
    /// Unique name of the tool (used in tool calls).
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Human-readable description for agent understanding.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// JSON schema defining the tool's parameters.
    /// </summary>
    ToolSchema Schema { get; }
    
    /// <summary>
    /// Executes the tool with the given input.
    /// </summary>
    Task<ToolResult> ExecuteAsync(
        ToolInput input,
        CancellationToken cancellationToken = default);
}
```

#### ToolInput.cs

```csharp
using System;
using System.Collections.Generic;

namespace Acode.Domain.Tools;

/// <summary>
/// Input parameters for a tool invocation.
/// </summary>
public sealed class ToolInput
{
    private readonly Dictionary<string, object> _parameters;

    public ToolInput(Dictionary<string, object> parameters)
    {
        _parameters = parameters ?? new Dictionary<string, object>();
    }

    public T GetRequired<T>(string name)
    {
        if (!_parameters.TryGetValue(name, out var value))
        {
            throw new ArgumentException($"Required parameter '{name}' is missing");
        }

        return ConvertValue<T>(value, name);
    }

    public T GetOptional<T>(string name, T defaultValue = default!)
    {
        if (!_parameters.TryGetValue(name, out var value))
        {
            return defaultValue;
        }

        return ConvertValue<T>(value, name);
    }

    private T ConvertValue<T>(object value, string name)
    {
        try
        {
            if (value is T typed)
            {
                return typed;
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception ex)
        {
            throw new ArgumentException(
                $"Parameter '{name}' could not be converted to {typeof(T).Name}", ex);
        }
    }
}
```

#### ToolResult.cs

```csharp
namespace Acode.Domain.Tools;

/// <summary>
/// Result of a tool execution.
/// </summary>
public sealed class ToolResult
{
    public bool IsSuccess { get; }
    public string Data { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }

    private ToolResult(bool isSuccess, string data, string? error, string? errorCode)
    {
        IsSuccess = isSuccess;
        Data = data;
        Error = error;
        ErrorCode = errorCode;
    }

    public static ToolResult Success(string data) =>
        new(true, data, null, null);

    public static ToolResult Failure(string error, string errorCode) =>
        new(false, string.Empty, error, errorCode);
}
```

---

### Application Layer Tools

#### SearchTextTool.cs

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Index;
using Acode.Domain.Tools;
using Acode.Infrastructure.Tools.Search;
using Microsoft.Extensions.Logging;

namespace Acode.Application.Tools.Search;

/// <summary>
/// Tool for searching file contents with relevance ranking.
/// </summary>
public sealed class SearchTextTool : ITool
{
    private readonly IIndexService _indexService;
    private readonly SearchRateLimiter _rateLimiter;
    private readonly SearchResultFormatter _formatter;
    private readonly SearchResultRedactor _redactor;
    private readonly ILogger<SearchTextTool> _logger;

    public string Name => "search_text";
    
    public string Description => 
        "Search for text content across all indexed files in the repository. " +
        "Returns matching files with relevance scores, line numbers, and code snippets. " +
        "Use this tool when you need to find code that contains specific terms, " +
        "class names, function names, or other text patterns.";

    public ToolSchema Schema => new()
    {
        Type = "object",
        Required = new[] { "query" },
        Properties = new Dictionary<string, SchemaProperty>
        {
            ["query"] = new()
            {
                Type = "string",
                Description = "The search query. Can be a single word, multiple words (AND logic), or a phrase in quotes."
            },
            ["max_results"] = new()
            {
                Type = "integer",
                Description = "Maximum number of results to return (default: 20, max: 100)"
            },
            ["file_type"] = new()
            {
                Type = "string",
                Description = "Filter results by file extension (e.g., '.cs', '.ts')"
            },
            ["include_path"] = new()
            {
                Type = "string",
                Description = "Only search in files under this directory path"
            },
            ["context_lines"] = new()
            {
                Type = "integer",
                Description = "Number of context lines before/after match (default: 2)"
            }
        }
    };

    public SearchTextTool(
        IIndexService indexService,
        SearchRateLimiter rateLimiter,
        SearchResultFormatter formatter,
        SearchResultRedactor redactor,
        ILogger<SearchTextTool> logger)
    {
        _indexService = indexService;
        _rateLimiter = rateLimiter;
        _formatter = formatter;
        _redactor = redactor;
        _logger = logger;
    }

    public async Task<ToolResult> ExecuteAsync(
        ToolInput input,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Validate input
            var query = input.GetRequired<string>("query");
            if (string.IsNullOrWhiteSpace(query))
            {
                return ToolResult.Failure(
                    "Query cannot be empty",
                    "ACODE-SRC-002");
            }

            var maxResults = Math.Min(input.GetOptional("max_results", 20), 100);
            var fileType = input.GetOptional<string?>("file_type", null);
            var includePath = input.GetOptional<string?>("include_path", null);
            var contextLines = input.GetOptional("context_lines", 2);

            // Check rate limit
            var rateLimitResult = await _rateLimiter.TryAcquireAsync(Name, cancellationToken);
            if (!rateLimitResult.IsAllowed)
            {
                return ToolResult.Failure(
                    $"Rate limit exceeded. Retry after {rateLimitResult.RetryAfter?.TotalSeconds:F0} seconds",
                    "ACODE-SRC-003");
            }

            // Build search query
            var searchQuery = new SearchQuery(query)
            {
                Take = maxResults,
                Filter = new SearchFilter
                {
                    Extensions = fileType != null ? new[] { fileType } : null,
                    Directory = includePath
                }
            };

            // Execute search
            var results = await _indexService.SearchAsync(searchQuery, cancellationToken);

            // Redact sensitive content
            var redactedResults = _redactor.RedactResults(results);

            // Format results
            var json = _formatter.Format(redactedResults, contextLines);

            _logger.LogInformation(
                "Search completed: Query={Query}, Results={Count}, Duration={Duration}ms",
                query, results.TotalCount, (DateTime.UtcNow - startTime).TotalMilliseconds);

            return ToolResult.Success(json);
        }
        catch (IndexNotReadyException)
        {
            return ToolResult.Failure(
                "Index is not ready. Run 'acode index build' first.",
                "ACODE-SRC-001");
        }
        catch (OperationCanceledException)
        {
            return ToolResult.Failure(
                "Search was cancelled",
                "ACODE-SRC-004");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed");
            return ToolResult.Failure(
                $"Search failed: {ex.Message}",
                "ACODE-SRC-002");
        }
    }
}
```

#### GrepTool.cs

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Index;
using Acode.Domain.Tools;
using Acode.Infrastructure.Tools.Search;
using Microsoft.Extensions.Logging;

namespace Acode.Application.Tools.Search;

/// <summary>
/// Tool for pattern matching across file contents.
/// </summary>
public sealed class GrepTool : ITool
{
    private readonly IIndexService _indexService;
    private readonly SearchRateLimiter _rateLimiter;
    private readonly SafeRegexCompiler _regexCompiler;
    private readonly SearchResultFormatter _formatter;
    private readonly ILogger<GrepTool> _logger;

    public string Name => "grep";
    
    public string Description =>
        "Search for a pattern across all indexed files using literal string or regex matching. " +
        "Returns all lines that match the pattern with file paths and line numbers. " +
        "Use this tool when you need to find specific text patterns, " +
        "like TODO comments, error messages, or specific code constructs.";

    public ToolSchema Schema => new()
    {
        Type = "object",
        Required = new[] { "pattern" },
        Properties = new Dictionary<string, SchemaProperty>
        {
            ["pattern"] = new()
            {
                Type = "string",
                Description = "The pattern to search for. Literal string by default, or regex if regex=true."
            },
            ["regex"] = new()
            {
                Type = "boolean",
                Description = "If true, treat pattern as a regular expression (default: false)"
            },
            ["case_sensitive"] = new()
            {
                Type = "boolean",
                Description = "If true, match is case-sensitive (default: true)"
            },
            ["file_pattern"] = new()
            {
                Type = "string",
                Description = "Only search in files matching this glob pattern (e.g., '*.cs')"
            },
            ["context_lines"] = new()
            {
                Type = "integer",
                Description = "Number of context lines before/after match (default: 0)"
            },
            ["max_results"] = new()
            {
                Type = "integer",
                Description = "Maximum number of matches to return (default: 50)"
            }
        }
    };

    public GrepTool(
        IIndexService indexService,
        SearchRateLimiter rateLimiter,
        SafeRegexCompiler regexCompiler,
        SearchResultFormatter formatter,
        ILogger<GrepTool> logger)
    {
        _indexService = indexService;
        _rateLimiter = rateLimiter;
        _regexCompiler = regexCompiler;
        _formatter = formatter;
        _logger = logger;
    }

    public async Task<ToolResult> ExecuteAsync(
        ToolInput input,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pattern = input.GetRequired<string>("pattern");
            var isRegex = input.GetOptional("regex", false);
            var caseSensitive = input.GetOptional("case_sensitive", true);
            var filePattern = input.GetOptional<string?>("file_pattern", null);
            var contextLines = input.GetOptional("context_lines", 0);
            var maxResults = input.GetOptional("max_results", 50);

            // Validate regex if requested
            if (isRegex)
            {
                try
                {
                    _regexCompiler.SafeCompile(pattern);
                }
                catch (InvalidPatternException ex)
                {
                    return ToolResult.Failure(
                        $"Invalid regex pattern: {ex.Message}",
                        "ACODE-SRC-002");
                }
            }

            // Check rate limit
            var rateLimitResult = await _rateLimiter.TryAcquireAsync(Name, cancellationToken);
            if (!rateLimitResult.IsAllowed)
            {
                return ToolResult.Failure(
                    $"Rate limit exceeded. Retry after {rateLimitResult.RetryAfter?.TotalSeconds:F0} seconds",
                    "ACODE-SRC-003");
            }

            // Execute grep
            var results = await _indexService.GrepAsync(
                pattern,
                isRegex,
                caseSensitive,
                filePattern,
                maxResults,
                cancellationToken);

            var json = _formatter.FormatGrepResults(results, contextLines);

            return ToolResult.Success(json);
        }
        catch (IndexNotReadyException)
        {
            return ToolResult.Failure(
                "Index is not ready. Run 'acode index build' first.",
                "ACODE-SRC-001");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Grep failed");
            return ToolResult.Failure(
                $"Grep failed: {ex.Message}",
                "ACODE-SRC-002");
        }
    }
}
```

#### SearchFilesTool.cs

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Index;
using Acode.Domain.Tools;
using Acode.Infrastructure.Tools.Search;
using Microsoft.Extensions.Logging;

namespace Acode.Application.Tools.Search;

/// <summary>
/// Tool for searching file paths by pattern.
/// </summary>
public sealed class SearchFilesTool : ITool
{
    private readonly IIndexService _indexService;
    private readonly SearchRateLimiter _rateLimiter;
    private readonly SearchResultFormatter _formatter;
    private readonly ILogger<SearchFilesTool> _logger;

    public string Name => "search_files";
    
    public string Description =>
        "Search for files by their path or name using glob patterns. " +
        "Returns matching file paths without reading file contents. " +
        "Use this tool when you need to find files by name, extension, or location.";

    public ToolSchema Schema => new()
    {
        Type = "object",
        Required = new[] { "pattern" },
        Properties = new Dictionary<string, SchemaProperty>
        {
            ["pattern"] = new()
            {
                Type = "string",
                Description = "Glob pattern to match file paths (e.g., '*.cs', '*Controller*', 'src/**/*.ts')"
            },
            ["directory"] = new()
            {
                Type = "string",
                Description = "Only search in this directory (relative path)"
            },
            ["max_results"] = new()
            {
                Type = "integer",
                Description = "Maximum number of results (default: 50)"
            }
        }
    };

    public SearchFilesTool(
        IIndexService indexService,
        SearchRateLimiter rateLimiter,
        SearchResultFormatter formatter,
        ILogger<SearchFilesTool> logger)
    {
        _indexService = indexService;
        _rateLimiter = rateLimiter;
        _formatter = formatter;
        _logger = logger;
    }

    public async Task<ToolResult> ExecuteAsync(
        ToolInput input,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pattern = input.GetRequired<string>("pattern");
            var directory = input.GetOptional<string?>("directory", null);
            var maxResults = input.GetOptional("max_results", 50);

            // Check rate limit
            var rateLimitResult = await _rateLimiter.TryAcquireAsync(Name, cancellationToken);
            if (!rateLimitResult.IsAllowed)
            {
                return ToolResult.Failure(
                    $"Rate limit exceeded. Retry after {rateLimitResult.RetryAfter?.TotalSeconds:F0} seconds",
                    "ACODE-SRC-003");
            }

            // Execute file search
            var results = await _indexService.SearchFilesAsync(
                pattern,
                directory,
                maxResults,
                cancellationToken);

            var json = _formatter.FormatFileResults(results);

            return ToolResult.Success(json);
        }
        catch (IndexNotReadyException)
        {
            return ToolResult.Failure(
                "Index is not ready. Run 'acode index build' first.",
                "ACODE-SRC-001");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File search failed");
            return ToolResult.Failure(
                $"File search failed: {ex.Message}",
                "ACODE-SRC-002");
        }
    }
}
```

---

### Infrastructure Components

#### SearchResultFormatter.cs

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Acode.Domain.Index;

namespace Acode.Infrastructure.Tools.Search;

/// <summary>
/// Formats search results as JSON for tool output.
/// </summary>
public sealed class SearchResultFormatter
{
    private const int MaxSnippetLength = 500;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false // Token-efficient
    };

    public string Format(SearchResultList results, int contextLines = 2)
    {
        var output = new
        {
            total = results.TotalCount,
            results = results.Results.Select(r => new
            {
                path = r.FilePath,
                score = Math.Round(r.Score, 3),
                matches = r.MatchedLines.Select((line, i) => new
                {
                    line,
                    snippet = TruncateSnippet(
                        r.Snippets.ElementAtOrDefault(i)?.Text ?? "")
                })
            })
        };

        return JsonSerializer.Serialize(output, JsonOptions);
    }

    public string FormatGrepResults(GrepResultList results, int contextLines = 0)
    {
        var output = new
        {
            total = results.TotalCount,
            results = results.Results.Select(r => new
            {
                path = r.FilePath,
                line = r.LineNumber,
                match = TruncateSnippet(r.LineContent)
            })
        };

        return JsonSerializer.Serialize(output, JsonOptions);
    }

    public string FormatFileResults(FileSearchResultList results)
    {
        var output = new
        {
            total = results.TotalCount,
            results = results.Results.Select(r => new
            {
                path = r.FilePath,
                size = r.SizeBytes,
                modified = r.LastModified.ToString("yyyy-MM-dd HH:mm:ss")
            })
        };

        return JsonSerializer.Serialize(output, JsonOptions);
    }

    private string TruncateSnippet(string snippet)
    {
        if (string.IsNullOrEmpty(snippet))
        {
            return string.Empty;
        }

        if (snippet.Length <= MaxSnippetLength)
        {
            return snippet.Trim();
        }

        return snippet.Substring(0, MaxSnippetLength).Trim() + "...";
    }
}
```

---

### Error Codes

| Code | Meaning | User Message |
|------|---------|--------------|
| ACODE-SRC-001 | Index not ready | Index is not ready. Run 'acode index build' first. |
| ACODE-SRC-002 | Invalid query or pattern | Invalid search query. Check syntax and try again. |
| ACODE-SRC-003 | Rate limit exceeded | Rate limit exceeded. Retry after N seconds. |
| ACODE-SRC-004 | Search timeout | Search timed out. Try a more specific query. |
| ACODE-SRC-005 | Tool not found | Requested search tool is not available. |

---

### DI Registration

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace Acode.Application.Tools.Search;

public static class SearchToolsServiceCollectionExtensions
{
    public static IServiceCollection AddSearchTools(
        this IServiceCollection services)
    {
        // Rate limiter (singleton for state persistence)
        services.AddSingleton<SearchRateLimiter>();
        
        // Formatters and validators
        services.AddSingleton<SearchResultFormatter>();
        services.AddSingleton<SearchResultRedactor>();
        services.AddSingleton<SafeRegexCompiler>();
        services.AddSingleton<SearchQuerySanitizer>();
        
        // Tools
        services.AddScoped<SearchTextTool>();
        services.AddScoped<SearchFilesTool>();
        services.AddScoped<GrepTool>();
        
        // Register with tool registry
        services.AddTransient<ITool, SearchTextTool>();
        services.AddTransient<ITool, SearchFilesTool>();
        services.AddTransient<ITool, GrepTool>();
        
        return services;
    }
}
```

---

### Implementation Checklist

1. [ ] Create domain models (ITool, ToolInput, ToolResult, ToolSchema)
2. [ ] Implement SearchTextTool with query parsing and filtering
3. [ ] Implement SearchFilesTool with glob pattern matching
4. [ ] Implement GrepTool with regex validation
5. [ ] Implement SearchRateLimiter with sliding window
6. [ ] Implement SearchResultFormatter for JSON output
7. [ ] Implement SearchResultRedactor for sensitive content
8. [ ] Implement SafeRegexCompiler with ReDoS protection
9. [ ] Implement SearchQuerySanitizer for logging
10. [ ] Register tools with DI container
11. [ ] Register tools with tool registry
12. [ ] Add configuration options for rate limits
13. [ ] Add metrics collection
14. [ ] Write unit tests for all tools
15. [ ] Write integration tests
16. [ ] Write E2E tests

---

### Rollout Plan

| Phase | Description | Duration |
|-------|-------------|----------|
| 1 | Domain models and interfaces | 0.5 day |
| 2 | SearchTextTool implementation | 1 day |
| 3 | SearchFilesTool implementation | 0.5 day |
| 4 | GrepTool implementation | 1 day |
| 5 | Rate limiting and security | 1 day |
| 6 | Result formatting and logging | 0.5 day |
| 7 | Testing and documentation | 1 day |

---

**End of Task 015.b Specification**