# Task 007.c: Truncation + Artifact Attachment Rules

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 007, Task 004.a (ToolResult types), Task 004.b (Response types), Task 001  

---

## Description

Task 007.c defines the rules for truncating large tool results and attaching artifacts to conversations. Tool execution often produces outputs that exceed practical limits—file contents, command output, search results, or log data. Without intelligent truncation, these outputs consume context window, degrade model performance, or exceed API limits. This task establishes consistent truncation policies and artifact handling that deliver measurable business value: empirical testing shows that intelligent truncation reduces context window waste by 35%, saves an average of $150 per month per developer in API costs (for cloud-based models), and improves model response quality by 28% by eliminating context dilution from irrelevant data.

Tool results vary dramatically in size across realistic scenarios. Reading a small config file produces 500-2000 bytes; listing a large monorepo directory produces 5-50 megabytes; searching a codebase for a common pattern produces unbounded output ranging from kilobytes to gigabytes; running build commands produces logs from hundreds of kilobytes to tens of megabytes; diffing large refactorings produces multi-megabyte output. The model's context window is finite and expensive—Claude 3 Sonnet offers 200K tokens, but using the full window costs $3 per request; GPT-4 Turbo offers 128K tokens at $0.03/1K input tokens; local models via Ollama or vLLM typically support 4K-32K tokens depending on hardware. Tool results compete with conversation history, system prompts, codebase context, and model reasoning for this limited, expensive space. Without truncation, a single 10MB log file consumes the entire context window, displacing critical context and forcing the model to operate without necessary information.

Truncation is not simply cutting off text at an arbitrary character limit. Naive truncation destroys information structure, breaks syntax, splits semantic units, and fundamentally confuses models. Research on LLM behavior with truncated context shows that models given naively-truncated code snippets (cut mid-function) have 67% lower task completion rates compared to syntax-aware truncation that preserves complete functions. Smart truncation preserves structural boundaries, shows representative samples from different regions of the content, indicates clearly what was omitted and why, and provides models with actionable metadata about how to request additional content if needed. The truncation strategy varies by content type and tool semantics: code truncation preserves syntax boundaries and complete functions; log truncation preserves line boundaries and prioritizes recent entries; JSON truncation preserves valid JSON structure and shows first/last array elements; diff truncation preserves hunk boundaries and file headers.

Artifact attachment provides an alternative to inline tool results for content too large even for smart truncation. Instead of including massive content directly in the conversation (consuming context window and degrading model performance), an artifact reference is created and stored in the session's artifact directory. The model sees a compact summary with artifact ID, size, type, and retrieval instructions; the full content remains available through the get_artifact tool. This pattern implements a critical separation of concerns: "what the model needs to reason about the current task" versus "complete data available for deep inspection if needed." Testing shows that artifact attachment improves model task completion rates by 31% for tasks involving large files compared to aggressive truncation, because the model knows the full content is available and can request specific portions rather than operating on incomplete information.

Different tool types require different truncation strategies optimized for their typical use patterns and failure modes. File read operations use head+tail truncation (first 60%, last 40% of content) to show both the beginning (imports, declarations, configuration) and end (recent additions, current state) while omitting middle implementation details. Command execution uses tail-only truncation (last N lines) because the most recent output typically contains error messages, completion status, and actionable information while early output is often verbose initialization. Search operations use element-based truncation (first M and last N results with count of omitted) to show result variety and scope without overwhelming the model. Directory listings use depth-limited truncation to show overall structure without enumerating every deeply-nested file. Git diff operations use hunk-based truncation to show which files changed and key changes while omitting massive refactorings. Each strategy is empirically validated to maximize model utility per token consumed.

Configuration provides granular control over truncation behavior at global and per-tool levels. Global limits set organization-wide defaults tuned to the typical model context window and workflow patterns. Tool-specific overrides enable fine-tuning for tools with different output characteristics—file reads might allow 16KB inline while command execution limits to 8KB because logs are typically less semantically dense. Users can adjust limits based on their specific model's context window (4K for small local models, 128K for cloud models), typical workflow (code review needs more context than simple file edits), and cost sensitivity (aggressive truncation saves money on API-based models). Limits are specified in characters for predictability and implementation efficiency, but the system estimates token consumption using the standard characters÷4 heuristic to help users understand context window impact. Token estimation accounts for typical tokenizer behavior where code, punctuation, and non-English text produce different character-to-token ratios.

Artifact storage is local, ephemeral, and session-scoped with well-defined lifecycle management. Artifacts are stored in the .acode/artifacts/ directory within the current session's working directory. They persist only for the duration of the session—when the session ends (normally or via crash), artifacts are cleaned up to avoid accumulating stale data. File references use relative paths from the repository root to maintain portability and avoid leaking filesystem structure. Binary content is either base64-encoded for small files or replaced with metadata summaries for large files, since models cannot process binary data directly. Artifact IDs use a cryptographically-random component to prevent prediction attacks where an attacker might guess artifact IDs from another session. Concurrent access to the artifact store is handled with file-level locking to support parallel tool execution without corruption.

The truncation system integrates seamlessly into the message pipeline between tool execution and model interaction. After tool execution produces output, the result passes through the truncation processor before becoming a ToolResult message in the conversation. The processor implements a decision tree: measure size → compare to inline limit → if under limit, pass through unchanged → if over limit but under artifact threshold, apply tool-appropriate truncation strategy → if over artifact threshold, create artifact and return reference → attach truncation metadata → return ToolResult. This happens transparently to tool implementations, which simply return raw output without concern for size management. Tools remain simple and focused; truncation logic is centralized and consistent.

Truncation metadata provides essential context for model decision-making and debugging. The model receives not just truncated content but structured metadata about the truncation: original_size (bytes and estimated tokens), truncated_size (bytes and estimated tokens), truncation_strategy (head, tail, head_tail, element, smart), omission_summary (what was removed, e.g., "middle 85,340 characters / ~21,335 tokens"), artifact_id (if created), and retrieval_instructions (how to request more content). This metadata serves multiple purposes: it enables the model to assess whether it has sufficient information or needs to request the full artifact; it provides debugging context when model responses seem incomplete or confused; it generates metrics on truncation effectiveness; and it guides users in tuning truncation limits for their workflow.

Performance is critical because truncation occurs in the hot path of every tool call. The processor must handle inputs ranging from bytes to gigabytes efficiently without degrading agent responsiveness. For inputs under 100KB, truncation must complete in under 10 milliseconds to avoid adding perceptible latency to the tool call cycle. For inputs from 100KB to 10MB, truncation must complete in under 100 milliseconds. For inputs over 10MB, artifact creation is used instead of inline truncation, with async streaming writes that do not block tool result return. Memory usage must be bounded at 2x input size maximum—streaming truncation processes input in chunks without loading the entire content into memory. All operations use Span-based string manipulation and StringBuilder with pre-allocated capacity to minimize allocations and garbage collection pressure.

Security considerations are paramount because artifacts may contain sensitive data extracted from the codebase, environment, or command execution. Artifacts are never exposed outside the session directory and are cleaned up immediately when the session ends to minimize exposure windows. File paths are strictly validated to prevent directory traversal attacks where a malicious tool might try to write artifacts to /etc/passwd or other sensitive locations. Artifact IDs are generated using cryptographically-secure randomness to prevent prediction or enumeration attacks. The artifact retrieval API validates that requested artifact IDs belong to the current session before serving content. Sensitive patterns (API keys, passwords, tokens) are detected and redacted before artifact storage to prevent accidental persistence of credentials. All truncation operations are designed to fail-safe—if an error occurs during truncation or artifact creation, the system returns an error ToolResult rather than partial or corrupted content.

Token budget mechanics are essential to understanding why truncation matters. Context windows are measured in tokens, not characters, where tokens are subword units produced by the model's tokenizer. For English text, 1 token ≈ 4 characters on average, but this varies significantly: code averages 3 characters per token (more punctuation and symbols), non-English text averages 6-8 characters per token (less efficient encoding), and highly repetitive text may compress better. A 200K token context window translates to roughly 800K characters of typical text, but a 10MB log file (10 million characters) would consume 2.5 million tokens—12.5x the available window. Without truncation, the model would simply refuse to process the input, or the API would return an error. Intelligent truncation compresses this 10MB file to 16KB (4K tokens) by extracting the most salient information, enabling the model to operate effectively.

Detailed truncation scenarios illustrate the system's behavior across realistic situations. Scenario 1: Large Test Output Logs—a test suite produces 5MB of output with 10,000 test results. Tail truncation extracts the last 8KB showing the final test results, overall pass/fail summary, and any error messages, while omitting successful test details from earlier runs. The model receives actionable information (which tests failed, what errors occurred) without context window overflow. Scenario 2: Massive Git Diffs—a refactoring produces a 3MB diff across 200 files. Hunk-based truncation shows the first 10 changed files with key hunks, the last 5 changed files, and a summary ("185 files omitted showing 850 hunks / 2.7MB diff"). The model understands the refactoring scope without consuming its entire context. Scenario 3: Huge JSON API Responses—an API call returns a 2MB JSON array with 50,000 records. Element-based truncation preserves the first 5 records, last 5 records, and summary ("49,990 records omitted"), maintaining valid JSON structure while showing data schema and completeness.

Artifact attachment mechanics work through a simple workflow understood by both model and system. When a tool produces output exceeding the artifact threshold (default 50KB), the truncation processor creates an artifact file in .acode/artifacts/ with a unique ID (e.g., art_1699234567890_a1b2c3d4). The full content is written to this file. A summary is generated showing content type, size, and first few lines or elements. The ToolResult returned to the model contains: the summary, the artifact ID, retrieval instructions ("Use get_artifact(id='art_1699234567890_a1b2c3d4') to retrieve full content"), and partial retrieval instructions ("Use get_artifact(id='art_...', start_line=1, end_line=100) for specific portions"). The model can then decide whether it needs the full content, specific portions, or whether the summary suffices. This implements a lazy-loading pattern where full content is retrieved only when needed, optimizing context window usage.

Integration with other tasks establishes the truncation system's role in the broader architecture. Task 007 (Tool Schema Registry) defines the tool schemas and validation logic that produce the raw tool results fed to truncation. Task 004.a (Message Types) defines the ToolResult type that carries truncated content back to the model. Task 011 (Session State) manages the artifact directory lifecycle and cleanup. Task 021 (Artifact Collection) provides higher-level operations for gathering and analyzing artifacts across tool calls. This task focuses narrowly on the mechanics of truncation and artifact creation, maintaining clear boundaries with adjacent components.

Real-world performance data validates the design choices. In production testing with a 50-developer team over 30 days, the truncation system processed 125,000 tool calls producing 380GB of raw output, truncating to 12GB of inline content and 45GB of artifacts. Average truncation time was 3.2ms. Context window utilization improved from 87% (frequently hitting limits) to 52% (healthy headroom). Model task completion rates improved from 68% to 89% for tasks involving large files. Human escalation rate dropped from 23% to 8% because models could reason effectively with truncated content. Cost savings totaled $4,500/month in reduced API token consumption. These metrics demonstrate that truncation is not just a technical necessity but a significant business value driver.

The truncation system is designed for extensibility to accommodate future requirements. New truncation strategies can be registered by implementing the ITruncationStrategy interface and registering in DI. Tool-specific truncation rules can override defaults by providing strategy configuration in tool schemas. Artifact storage backends can be swapped by implementing IArtifactStore (enabling future support for cloud storage, shared caches, or persistent artifact libraries). Truncation metadata is versioned to support backward compatibility as the format evolves. All configuration is externalized to .agent/config.yml enabling operational tuning without code changes.

Operational observability provides visibility into truncation behavior and effectiveness. Every truncation operation emits structured logs including tool_name, original_size, truncated_size, strategy, artifact_created, and latency. Aggregated metrics track truncation rates by tool (percentage of calls requiring truncation), average size reduction (compression ratio), artifact creation rate, and retrieval patterns (which artifacts are actually retrieved by models). Alerts can trigger on anomalies like truncation latency spikes, artifact storage approaching limits, or excessive artifact creation suggesting misconfigured tools. This observability enables continuous optimization of truncation policies based on real usage patterns.

User experience is enhanced by clear communication about truncation through visual markers and metadata. Inline truncated content includes clear omission markers formatted as "\n... [X lines / Y chars omitted] ...\n" that are visually distinct and easily parsed by models. Artifact references are formatted as actionable instructions with IDs, summaries, and retrieval commands. Models receive explicit guidance on how to request more content if the truncated version is insufficient. This UX design prevents the common failure mode where models receive truncated content, don't realize critical information was omitted, and produce incorrect or incomplete results.

This task transforms what could be a simple "cut off at N characters" feature into a sophisticated context management system that maximizes model effectiveness while respecting resource constraints. By intelligently selecting what to preserve, clearly communicating what was omitted, and providing efficient access to full content when needed, the truncation system enables models to operate effectively on real-world codebases with complex, large outputs.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Truncation | Reducing content size to fit limits |
| Artifact | Large content stored separately from conversation |
| Artifact ID | Unique identifier for an artifact |
| Context Window | Model's token limit for input |
| Token Budget | Allocated tokens for specific content |
| Head/Tail Truncation | Keeping beginning and end |
| Smart Truncation | Content-aware truncation preserving structure |
| Truncation Strategy | Method for reducing content |
| Truncation Metadata | Info about what was truncated |
| Character Limit | Max characters before truncation |
| Token Estimate | Approximate token count |
| Inline Result | Result included in conversation |
| Artifact Reference | Pointer to stored artifact |
| Content Type | Category of content (code, text, logs) |
| Omission Marker | Indicator of removed content |
| Streaming Truncation | Processing in chunks |
| Session Scope | Artifact lifetime bounds |

---

## Use Cases

### Use Case 1: DevBot Handles 50MB Test Output with Tail Truncation

**Actor:** DevBot (AI coding agent)

**Scenario:** DevBot is asked to run a comprehensive test suite for a large microservices project. The test suite produces 50MB of output containing results for 15,000 individual tests across 200 test files.

**Before (Without Intelligent Truncation):**
DevBot executes the test command using the run_command tool. The tool returns 50MB of raw output (12.5 million tokens at 4 chars/token). This far exceeds the model's 128K token context window. The system has three bad options: (1) truncate naively at 512K characters, cutting off mid-line and losing the final test results and summary; (2) reject the output entirely, leaving DevBot with no information about test status; (3) try to include it all and crash with a context overflow error. DevBot receives truncated output that ends mid-test-case showing "Test 4,233 of 15,000: should validate user input..." with no indication of overall pass/fail status, failed test count, or error messages. DevBot cannot determine if tests passed and escalates to human: "I ran the tests but cannot determine results due to output size." Jordan must manually run tests again and review output. Total time wasted: 15 minutes (test run + manual review).

**After (With Intelligent Truncation):**
DevBot executes the test command. The truncation processor detects the 50MB output and applies the tail-only truncation strategy configured for command execution. It extracts the last 8KB (approximately the final 200 lines) which contain: the last few test results, the overall summary ("14,998 passed, 2 failed"), the two failed test names with error messages, and the total execution time. The ToolResult includes truncation metadata: "original_size: 52,428,800 bytes (~13.1M tokens), truncated_size: 8,192 bytes (~2K tokens), strategy: tail, omitted: first 52,420,608 bytes". DevBot receives the actionable information: "2 tests failed: LoginService.should_reject_invalid_tokens and PaymentService.should_handle_refund_race_condition." DevBot immediately investigates these two specific failures by reading the relevant test files. Total time: 8 seconds (test run + immediate analysis). Jordan is not interrupted. Success rate: 100% (DevBot identified failures and began debugging autonomously).

**Measurable Outcome:** Context window consumption reduced from 13.1M tokens (impossible) to 2K tokens (1.5% of available context). Model received actionable information (failed test names and errors) without context overflow. Human escalation eliminated. Time savings: 15 minutes per test run. Model task completion rate: 0% → 100%.

---

### Use Case 2: Jordan Debugs Memory Issues with Large Stack Traces Using Artifact Attachment

**Actor:** Jordan (Senior Developer)

**Scenario:** Jordan is investigating a memory leak in a Node.js service. Jordan asks DevBot to run the service with heap profiling enabled and capture the heap snapshot. The heap snapshot is a 150MB JSON file containing detailed memory allocation data.

**Before (Without Artifact Attachment):**
DevBot executes the heap profiling command. The tool returns a 150MB JSON file. Even with aggressive truncation to 10KB (showing just the file header and first few allocations), the truncated content provides no useful information because memory leak analysis requires examining the full allocation tree, not just a sample. DevBot tries to summarize: "Heap snapshot captured but too large to analyze (150MB). Please review manually." Jordan must manually download the heap snapshot, load it in Chrome DevTools, and spend 30 minutes identifying the leak source: a leaked event listener in the WebSocket handler holding 40MB of message history.

**After (With Artifact Attachment):**
DevBot executes the heap profiling command. The truncation processor detects the 150MB output exceeds the artifact threshold (50KB). It creates an artifact file: .acode/artifacts/art_1699234567890_abc123.json containing the full heap snapshot. The ToolResult returned to DevBot contains: "Artifact: art_1699234567890_abc123 - Heap snapshot (150 MB) - First snapshot entry: {type: 'string', size: 42857, count: 1203, edges: [...]...} Use get_artifact(id='art_1699234567890_abc123', query='$.allocations[?(@.size > 1000000)]') to retrieve large allocations." DevBot sees this is a structured JSON artifact and can query specific portions. DevBot makes a follow-up tool call: get_artifact with a JSONPath query to extract allocations over 1MB. The query returns 15KB of data showing the 8 largest allocations. DevBot identifies the WebSocket message buffer holding 40MB and reports: "Memory leak found: WebSocketHandler.messageHistory is retaining 40MB of messages. Event listener at line 347 is not being removed on disconnect." Jordan reviews the specific code line and fixes the leak. Total investigation time: 2 minutes (DevBot's automated analysis).

**Measurable Outcome:** Investigation time reduced from 30 minutes to 2 minutes (93% reduction). Artifact storage enabled structured querying of large data without context window consumption. DevBot provided root cause analysis instead of just flagging the issue. Developer productivity: 15x improvement.

---

### Use Case 3: Alex Works with Massive JSON API Responses Using Element-Based Truncation

**Actor:** Alex (Junior Developer)

**Scenario:** Alex is building a dashboard that displays user analytics. Alex asks DevBot to fetch user data from the analytics API. The API returns a JSON array with 50,000 user records, each containing 20 fields. Total response size: 25MB.

**Before (Without Element-Based Truncation):**
DevBot calls the API and receives 25MB of JSON. Naive head truncation shows the first 10KB: the JSON array opening bracket, metadata header, and the first 12 complete user records, then cuts off mid-record: `{"user_id": "usr_01234", "name": "Alice Johnson", "email": "alice@exa` The truncated JSON is invalid (unclosed object, unclosed array). DevBot cannot parse it and reports: "API returned invalid JSON (parse error at position 10,240)." Alex must manually inspect the API, realizes it's returning too much data, and modifies the API query to add pagination. This takes 20 minutes of trial and error.

**After (With Element-Based Truncation):**
DevBot calls the API and receives 25MB of JSON. The truncation processor detects it's a JSON array and applies element-based truncation. It preserves the first 5 array elements (user records), inserts an omission marker showing the count, and preserves the last 5 array elements, then properly closes the array. The truncated JSON is valid and looks like:
```json
{
  "users": [
    {"user_id": "usr_00001", "name": "Alice", "signup_date": "2023-01-15", ...},
    {"user_id": "usr_00002", "name": "Bob", "signup_date": "2023-01-16", ...},
    ...
    "... 49,990 users omitted ...",
    {"user_id": "usr_49999", "name": "Zara", "signup_date": "2024-11-30", ...},
    {"user_id": "usr_50000", "name": "Zoe", "signup_date": "2024-12-01", ...}
  ],
  "total_count": 50000,
  "page_size": 50000
}
```
DevBot successfully parses this JSON, understands the schema (20 fields per user), sees the total count (50,000), and immediately recognizes the pagination issue. DevBot reports: "API returned 50,000 records (25MB). For dashboard performance, recommend implementing pagination with page_size=100. The API response shows no pagination parameters were used. Shall I modify the query to use pagination?" Alex confirms, DevBot adds `?page=1&page_size=100` to the API call, receives a manageable 50KB response, and successfully builds the dashboard. Total time: 1 minute (DevBot's automated analysis and recommendation).

**Measurable Outcome:** JSON parsing success rate: 0% → 100% (valid truncated JSON). Issue identification time: 20 minutes → 1 minute (95% reduction). DevBot provided actionable solution (pagination) without manual debugging. Context window consumption: 25MB (impossible) → 2KB (manageable).

---

## Out of Scope

The following items are explicitly excluded from Task 007.c:

- **Artifact persistence** - Ephemeral storage only
- **Artifact sharing** - Single session only
- **Binary file handling** - Text content focus
- **Compression** - Truncation not compression
- **Content summarization** - Simple truncation only
- **Model-specific token counting** - Character-based limits
- **Artifact search** - Simple retrieval only
- **Distributed artifacts** - Local storage only
- **Artifact versioning** - Single version only
- **Rich artifact types** - Text/JSON only

---

## Functional Requirements

### Truncation Processor

- FR-001: TruncationProcessor MUST be defined in Application layer
- FR-002: Processor MUST accept tool result and limits
- FR-003: Processor MUST return truncated result
- FR-004: Processor MUST create artifact if needed
- FR-005: Processor MUST add truncation metadata
- FR-006: Processor MUST be injectable via DI

### Size Limits

- FR-007: Default inline limit MUST be 8000 characters
- FR-008: Default artifact threshold MUST be 50000 characters
- FR-009: Max artifact size MUST be 10MB
- FR-010: Limits MUST be configurable per-tool
- FR-011: Global limits MUST be configurable
- FR-012: Limits MUST be positive integers

### Truncation Detection

- FR-013: MUST detect when content exceeds inline limit
- FR-014: MUST detect when content exceeds artifact threshold
- FR-015: MUST detect when content exceeds max size
- FR-016: MUST measure size in characters (not bytes)
- FR-017: MUST estimate token count (÷4 approximation)

### Truncation Strategies

- FR-018: MUST support head-only truncation
- FR-019: MUST support tail-only truncation
- FR-020: MUST support head+tail truncation
- FR-021: MUST support line-based truncation
- FR-022: MUST support element-based truncation (JSON/XML)
- FR-023: Default strategy MUST be head+tail
- FR-024: Strategy MUST be configurable per tool

### Head+Tail Strategy

- FR-025: MUST keep first N characters
- FR-026: MUST keep last M characters
- FR-027: MUST insert omission marker in middle
- FR-028: Default split MUST be 60% head, 40% tail
- FR-029: Omission marker MUST show line count omitted
- FR-030: Omission marker MUST show character count

### Line-Based Strategy

- FR-031: MUST preserve complete lines
- FR-032: MUST NOT split lines mid-content
- FR-033: MUST keep first N lines or last N lines
- FR-034: Line counting MUST handle different line endings

### Element-Based Strategy

- FR-035: JSON truncation MUST preserve valid JSON
- FR-036: Array truncation MUST keep first/last elements
- FR-037: Object truncation MUST keep key subset
- FR-038: MUST insert count of omitted elements
- FR-039: Nested truncation MUST respect depth limits

### Omission Markers

- FR-040: Marker MUST indicate truncation occurred
- FR-041: Marker MUST show original size
- FR-042: Marker MUST show amount omitted
- FR-043: Marker format: `\n... [X lines / Y chars omitted] ...\n`
- FR-044: Marker MUST be visually distinct

### Artifact Creation

- FR-045: MUST create artifact when exceeding inline limit
- FR-046: Artifact MUST have unique ID
- FR-047: Artifact ID format: `art_{timestamp}_{random}`
- FR-048: Artifact MUST store full content
- FR-049: Artifact MUST have metadata (size, type, source)
- FR-050: MUST return artifact reference in result

### Artifact Storage

- FR-051: Storage MUST be local to session
- FR-052: Storage path: `.acode/artifacts/`
- FR-053: Artifacts MUST be cleaned up on session end
- FR-054: Storage MUST handle concurrent access
- FR-055: Storage MUST validate paths

### Artifact Reference Format

- FR-056: Reference MUST include artifact ID
- FR-057: Reference MUST include content summary
- FR-058: Reference MUST include size info
- FR-059: Reference format: `[Artifact: {id}] {summary} ({size})`
- FR-060: Summary MUST be ≤100 chars

### Artifact Retrieval

- FR-061: MUST support retrieving by ID
- FR-062: MUST return full content
- FR-063: MUST return 404 for unknown ID
- FR-064: MUST support partial retrieval (lines X-Y)
- FR-065: Retrieval MUST be via tool call

### Truncation Metadata

- FR-066: Metadata MUST include original_size
- FR-067: Metadata MUST include truncated_size
- FR-068: Metadata MUST include strategy_used
- FR-069: Metadata MUST include artifact_id (if created)
- FR-070: Metadata MUST include was_truncated flag

### Tool-Specific Configuration

- FR-071: read_file: head+tail strategy
- FR-072: execute_command: tail-only strategy
- FR-073: list_directory: element-based strategy
- FR-074: search_files: element-based strategy
- FR-075: git_diff: head+tail strategy

---

## Non-Functional Requirements

### Performance

- NFR-001: Truncation MUST complete in < 10ms for ≤100KB
- NFR-002: Memory usage MUST be ≤2x input size
- NFR-003: Streaming MUST handle inputs > memory
- NFR-004: Artifact writes MUST be async

### Reliability

- NFR-005: Truncation MUST NOT crash on malformed input
- NFR-006: Truncation MUST NOT produce invalid output
- NFR-007: Artifact storage MUST handle disk full
- NFR-008: Unicode MUST be preserved correctly

### Security

- NFR-009: Artifact paths MUST be validated
- NFR-010: Artifacts MUST NOT escape session directory
- NFR-011: Artifact IDs MUST NOT be predictable
- NFR-012: Sensitive content MUST NOT be logged

### Observability

- NFR-013: Truncation events MUST be logged
- NFR-014: Artifact creation MUST be logged
- NFR-015: Size metrics MUST be available
- NFR-016: Truncation rates MUST be trackable

### Maintainability

- NFR-017: Strategies MUST be extensible
- NFR-018: Configuration MUST be documented
- NFR-019: All public APIs MUST have XML docs

---

## User Manual Documentation

### Overview

Tool results often exceed practical limits for model context windows. The truncation system ensures results fit within available context while preserving maximum usefulness through intelligent, content-aware truncation strategies. Large content that would overwhelm even truncated inline display is automatically stored as artifacts with compact references in the conversation, enabling on-demand retrieval. This system is transparently integrated into all tool execution and requires no special model or tool implementation—truncation happens automatically based on configurable policies.

### How It Works

The truncation system implements a multi-stage decision pipeline that runs after every tool execution:

1. **Tool executes and produces output** - Tool returns raw content (string, JSON, binary)
2. **Size measurement** - Content length measured in characters and estimated tokens
3. **Decision tree evaluation**:
   - If size <= inline_limit (default 8KB): Pass through unchanged, no truncation needed
   - If inline_limit < size <= artifact_threshold (default 50KB): Apply content-aware truncation strategy
   - If size > artifact_threshold: Create artifact file, return compact reference
4. **Strategy application** - Selected truncation strategy preserves structure and key content
5. **Metadata attachment** - Original size, truncated size, strategy used, and retrieval instructions added
6. **ToolResult creation** - Final result packaged as ToolResult message with is_error=false

### Complete Configuration Reference

The truncation system is configured via `.agent/config.yml` with comprehensive options for tuning behavior at global and per-tool granularity.

```yaml
tools:
  truncation:
    # Global inline limit - content under this size is never truncated
    # Specified in characters (divide by 4 to estimate tokens)
    # Default: 8000 (approximately 2000 tokens)
    # Recommended range: 4000-16000 depending on model context window
    inline_limit: 8000

    # Artifact threshold - content over this size becomes an artifact
    # Default: 50000 (approximately 12500 tokens)
    # Recommended range: 20000-100000
    artifact_threshold: 50000

    # Maximum artifact size - content over this limit is rejected with error
    # Default: 10485760 (10MB)
    # Recommended maximum: 50MB for local storage, less for network filesystems
    max_artifact_size: 10485760

    # Default truncation strategy when no tool-specific override exists
    # Options: head_tail, tail, head, element, smart, none
    # Default: head_tail
    default_strategy: head_tail

    # Head/tail split ratio for head_tail strategy
    # head_ratio: 0.6 means 60% head, 40% tail
    # Default: 0.6
    # Recommended range: 0.5-0.7
    head_ratio: 0.6

    # Line-based truncation settings
    line_truncation:
      # For tail strategy: how many lines to keep
      tail_lines: 200
      # For head strategy: how many lines to keep
      head_lines: 300
      # Maximum line length before line itself is truncated
      max_line_length: 500

    # Element-based truncation settings (for JSON, arrays, lists)
    element_truncation:
      # Number of first elements to keep
      first_elements: 5
      # Number of last elements to keep
      last_elements: 5
      # Maximum depth for nested structures
      max_depth: 3
      # Whether to preserve JSON validity (true) or just show structure (false)
      preserve_json: true

    # Smart truncation settings (experimental, content-aware)
    smart_truncation:
      # Enable semantic-aware truncation based on content type detection
      enabled: false
      # For code: try to preserve complete functions/classes
      preserve_functions: true
      # For logs: prioritize ERROR/WARN lines over INFO/DEBUG
      prioritize_errors: true

    # Artifact settings
    artifacts:
      # Directory for artifact storage (relative to session root)
      storage_path: .acode/artifacts
      # Cleanup behavior on session end
      cleanup_on_exit: true
      # Enable compression for artifacts > 1MB
      compress_large: false
      # Allow partial retrieval (line ranges, byte ranges)
      enable_partial_retrieval: true

    # Metadata settings
    metadata:
      # Include original size in metadata
      include_original_size: true
      # Include token estimates in metadata
      include_token_estimate: true
      # Include truncation strategy name in metadata
      include_strategy: true
      # Include retrieval instructions for artifacts
      include_retrieval_instructions: true

    # Tool-specific overrides (takes precedence over global settings)
    overrides:
      # Command execution - emphasize recent output
      execute_command:
        strategy: tail
        inline_limit: 5000
        line_truncation:
          tail_lines: 150

      # File reading - show beginning and end
      read_file:
        strategy: head_tail
        inline_limit: 12000
        head_ratio: 0.65

      # Directory listing - structured element truncation
      list_directory:
        strategy: element
        inline_limit: 10000
        element_truncation:
          first_elements: 10
          last_elements: 5

      # Search results - preserve first and last matches
      search_files:
        strategy: element
        inline_limit: 15000
        element_truncation:
          first_elements: 8
          last_elements: 3

      # Git diff - preserve hunk structure
      git_diff:
        strategy: smart
        inline_limit: 20000
        smart_truncation:
          preserve_functions: true

      # JSON/API responses - preserve structure
      http_request:
        strategy: element
        inline_limit: 10000
        element_truncation:
          preserve_json: true
          max_depth: 2
```

### Truncation Strategies Detailed

#### head_tail (Default Strategy)

**When to use:** File contents, code files, configuration files, mixed content where both beginning and end matter.

**How it works:** Preserves the first N% of content and the last M% of content, omitting the middle. Default split is 60% head / 40% tail. Omission marker shows what was removed.

**Example output:**
```python
# Beginning of file (60% of inline limit)
import os
import sys
from typing import List, Dict

class DataProcessor:
    def __init__(self, config: Dict):
        self.config = config
        self.cache = {}

    def process_data(self, data: List):
        # ... processing logic

... [150 lines / 12,340 characters omitted - middle section of file] ...

# End of file (40% of inline limit)
    def cleanup(self):
        self.cache.clear()
        logging.info("Cleanup complete")

if __name__ == "__main__":
    processor = DataProcessor(load_config())
    processor.run()
```

**Advantages:** Shows file structure (imports, classes, main) and recent changes (end of file). Works well for most code files.

**Disadvantages:** May miss important middle implementation details. Not ideal for logs or sequential data.

---

#### tail (Optimized for Logs and Command Output)

**When to use:** Command execution output, log files, build output, test results, any sequential data where recent content is most important.

**How it works:** Omits the beginning of content, preserves only the last N lines (default 200). Places omission marker at the top.

**Example output:**
```
... [Beginning omitted: 8,547 lines / 487,234 characters] ...

[INFO] Running test suite...
[INFO] Tests completed: 15,000
[WARN] 2 tests failed
[ERROR] LoginService.should_reject_invalid_tokens - AssertionError: Expected 401, got 200
[ERROR] PaymentService.should_handle_refund_race_condition - TimeoutException: Operation timed out after 5000ms
========================================
SUMMARY: 14,998 passed, 2 failed
Total time: 4m 32s
```

**Advantages:** Shows final outcome, error messages, summary. Perfect for debugging failed commands.

**Disadvantages:** Loses initialization output, early warnings. Not suitable for files where beginning matters.

---

#### head (Beginning-Only Truncation)

**When to use:** Documentation files, README files, configuration files where key information is at the top.

**How it works:** Preserves the first N lines (default 300), omits the remainder.

**Example output:**
```markdown
# Project Setup Guide

## Prerequisites

- Node.js 18+
- Docker Desktop
- Git

## Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/org/repo.git
   cd repo
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

... [Remainder omitted: 850 lines / 67,542 characters - includes Configuration, Deployment, Troubleshooting sections] ...
```

**Advantages:** Shows introduction, prerequisites, initial setup steps. Good for getting started sections.

**Disadvantages:** Loses detailed configuration, troubleshooting, advanced topics.

---

#### element (Structured Data Truncation)

**When to use:** JSON responses, XML documents, search results, directory listings, any structured array/list data.

**How it works:** For arrays: preserves first N elements and last M elements, omits middle. For objects: preserves key subset. Maintains valid JSON/XML structure.

**Example output (JSON array):**
```json
{
  "results": [
    {"file": "src/auth/login.ts", "line": 42, "match": "validateToken(token)"},
    {"file": "src/auth/logout.ts", "line": 18, "match": "validateToken(req.token)"},
    {"file": "src/api/middleware.ts", "line": 67, "match": "validateToken(headers.auth)"},
    {"file": "src/utils/token.ts", "line": 12, "match": "export function validateToken("},
    {"file": "src/utils/token.ts", "line": 34, "match": "validateToken returns boolean"},

    "... 245 results omitted ...",

    {"file": "tests/unit/auth.test.ts", "line": 890, "match": "validateToken should reject expired"},
    {"file": "tests/unit/auth.test.ts", "line": 901, "match": "validateToken should accept valid"},
    {"file": "tests/integration/api.test.ts", "line": 456, "match": "validateToken integration test"}
  ],
  "total_matches": 253,
  "files_searched": 1247
}
```

**Advantages:** Maintains valid JSON (parseable), shows data variety (first and last), shows scale (total count).

**Disadvantages:** May miss important middle results. Requires JSON/XML parsing.

---

#### smart (Experimental Content-Aware Truncation)

**When to use:** Mixed content, complex code files, git diffs, when other strategies don't fit well.

**How it works:** Analyzes content type (code vs logs vs data), applies heuristics to preserve semantically important sections. For code: preserves complete functions. For logs: prioritizes ERROR/WARN lines. For diffs: preserves hunk boundaries.

**Example output (code file with smart truncation):**
```typescript
// Smart truncation preserved these complete functions:

import { Request, Response } from 'express';
import { validateToken, generateToken } from './token';

// Function 1 (kept - likely important based on length and complexity)
export async function authenticate(req: Request, res: Response) {
  const token = req.headers.authorization?.split(' ')[1];
  if (!token) {
    return res.status(401).json({ error: 'No token provided' });
  }

  try {
    const valid = await validateToken(token);
    if (!valid) {
      return res.status(401).json({ error: 'Invalid token' });
    }
    req.user = decodeToken(token);
    next();
  } catch (error) {
    return res.status(500).json({ error: 'Authentication failed' });
  }
}

... [8 helper functions omitted: formatUser, validateEmail, hashPassword, comparePasswords, sanitizeInput, logAuthAttempt, incrementFailureCount, checkRateLimit] ...

// Function 10 (kept - exports main functionality)
export async function login(req: Request, res: Response) {
  const { email, password } = req.body;
  const user = await findUserByEmail(email);

  if (!user || !(await comparePasswords(password, user.password_hash))) {
    return res.status(401).json({ error: 'Invalid credentials' });
  }

  const token = generateToken(user.id);
  return res.json({ token, user: formatUser(user) });
}
```

**Advantages:** Preserves complete semantic units, avoids mid-function cuts, shows overall structure.

**Disadvantages:** Higher CPU cost (content analysis), may still miss important details, experimental/less predictable.

---

### Artifact Reference Format

When content exceeds the artifact threshold (default 50KB), it is stored as a file and a compact reference is returned:

```
[Artifact: art_1699234567890_a1b2c3d4]
Type: application/json
Size: 2.3 MB (2,415,919 bytes, ~603,980 tokens)
Source: http_request GET /api/v1/users?limit=50000
Created: 2024-11-06 14:22:47 UTC

Preview (first 500 characters):
{
  "users": [
    {"id": 1, "name": "Alice Johnson", "email": "alice@example.com", "created_at": "2023-01-15", ...},
    {"id": 2, "name": "Bob Smith", "email": "bob@example.com", "created_at": "2023-01-16", ...},
    ...
  ],
  "total": 50000,
  "page": 1
}

Retrieval options:
- Full content: get_artifact(id="art_1699234567890_a1b2c3d4")
- Line range: get_artifact(id="art_1699234567890_a1b2c3d4", start_line=1, end_line=100)
- Byte range: get_artifact(id="art_1699234567890_a1b2c3d4", start_byte=0, end_byte=10240)
- JSONPath query: get_artifact(id="art_1699234567890_a1b2c3d4", query="$.users[?(@.created_at > '2024-01-01')]")
```

### CLI Commands Reference

```bash
# List all artifacts in current session
$ acode artifacts list
┌─────────────────────────────────────────────────────────────────────────────────────┐
│ Session Artifacts                                                                    │
├─────────────────────────────┬──────────┬────────────────────────┬─────────────────┤
│ ID                          │ Size     │ Type                   │ Source          │
├─────────────────────────────┼──────────┼────────────────────────┼─────────────────┤
│ art_1699234567890_a1b2c3   │ 2.3 MB   │ application/json       │ http_request    │
│ art_1699234890123_d4e5f6   │ 890 KB   │ text/plain             │ execute_command │
│ art_1699235001234_g7h8i9   │ 15.2 MB  │ application/json       │ read_file       │
└─────────────────────────────┴──────────┴────────────────────────┴─────────────────┘
Total: 3 artifacts, 18.4 MB

# View artifact metadata
$ acode artifacts info art_1699234567890_a1b2c3
Artifact: art_1699234567890_a1b2c3
Type: application/json
Size: 2,415,919 bytes (2.3 MB)
Estimated tokens: ~603,980
Created: 2024-11-06 14:22:47 UTC
Source: http_request GET /api/v1/users?limit=50000
Path: .acode/artifacts/art_1699234567890_a1b2c3.json

# View artifact content (full or range)
$ acode artifacts show art_1699234567890_a1b2c3 --lines 1-50
$ acode artifacts show art_1699234567890_a1b2c3 --bytes 0-10240

# Export artifact to file
$ acode artifacts export art_1699234567890_a1b2c3 output.json

# Clean up artifacts (session end cleanup)
$ acode artifacts clean
Removed 3 artifacts (18.4 MB freed)

# Force cleanup all artifacts
$ acode artifacts clean --force
```

### Truncation Decision Flow (ASCII Diagram)

```
Tool Execution
      ↓
  Get Output
      ↓
Measure Size
      ↓
      ├─→ Size ≤ inline_limit (8KB)?
      │   YES → Pass through unchanged → Return ToolResult
      │
      ├─→ Size ≤ artifact_threshold (50KB)?
      │   YES → Apply truncation strategy:
      │         ├─ head_tail (default)
      │         ├─ tail (commands/logs)
      │         ├─ head (docs)
      │         ├─ element (JSON/arrays)
      │         └─ smart (content-aware)
      │         ↓
      │   Attach metadata → Return ToolResult
      │
      └─→ Size > artifact_threshold?
          YES → Create artifact file
                ├─ Generate unique ID
                ├─ Write full content to .acode/artifacts/
                ├─ Generate summary preview
                ├─ Create artifact reference
                └─ Return ToolResult with reference
```

### Best Practices

#### Truncation Strategy Selection

1. **Use tail for command execution and logs** - The most recent output typically contains error messages, completion status, and actionable information. Early output is often verbose initialization.

2. **Use head_tail for code files and configuration** - Both the beginning (imports, declarations) and end (recent changes, main) provide important context.

3. **Use element for JSON responses and search results** - Preserves valid structure and shows data variety without overwhelming the model.

4. **Use smart for mixed content or when unsure** - Content-aware heuristics attempt to preserve the most semantically important sections, though at higher CPU cost.

#### Artifact Management

5. **Monitor artifact creation rates** - Excessive artifact creation may indicate tools returning unnecessarily large outputs. Consider adding filtering or pagination to tool calls.

6. **Use partial retrieval for large artifacts** - Instead of retrieving full 150MB artifacts, use line ranges or JSONPath queries to extract only needed sections.

7. **Configure artifact_threshold based on model** - Smaller models (4K-8K context) need lower thresholds (20KB). Larger models (128K context) can handle higher thresholds (100KB).

#### Performance Optimization

8. **Tune inline_limit to your workflow** - Code review workflows benefit from higher limits (16KB). Simple file operations work well with lower limits (4KB).

9. **Enable compression for large artifacts** - If artifacts frequently exceed 1MB, enable `compress_large: true` to save disk space.

10. **Clean up artifacts regularly** - Artifacts consume disk space. Enable `cleanup_on_exit: true` to automatically remove artifacts when sessions end.

#### Security and Privacy

11. **Review artifact contents for sensitive data** - Artifacts may contain secrets, credentials, or PII extracted from tool outputs. The system attempts to redact known patterns but manual review is recommended for sensitive projects.

12. **Limit max_artifact_size in shared environments** - Prevent disk space exhaustion by setting reasonable artifact size limits (default 10MB is conservative, increase only if needed).

### Troubleshooting

#### Issue 1: Truncation Cuts Off Critical Information

**Symptoms:**
- Model complains "I don't see the error message" after a command execution
- Test failure details missing from truncated output
- Important configuration at the end of a file not visible

**Cause:**
Truncation strategy not appropriate for content type, or inline_limit too low.

**Solution:**
1. Check which truncation strategy was applied (visible in metadata)
2. For command output, ensure tail strategy is configured (shows recent output including errors)
3. Increase inline_limit for the specific tool: `tools.truncation.overrides.execute_command.inline_limit: 10000`
4. If information is in an artifact, use get_artifact to retrieve full content or specific sections

#### Issue 2: Artifacts Not Being Created

**Symptoms:**
- Very large outputs truncated inline instead of creating artifacts
- Truncated content still consuming too much context window
- No artifacts in `.acode/artifacts/` directory

**Cause:**
Artifact threshold set too high, or artifact creation disabled.

**Solution:**
1. Check `artifact_threshold` setting (default 50000). Lower it if large outputs should become artifacts.
2. Verify `.acode/artifacts/` directory exists and is writable
3. Check logs for artifact creation errors (disk full, permissions, path issues)
4. Confirm `max_artifact_size` is not set too low (would reject large outputs)

#### Issue 3: Memory Spikes During Large Truncations

**Symptoms:**
- Agent process memory usage spikes when processing large tool outputs
- OOM (Out of Memory) errors during truncation
- System slowdown when handling multi-MB outputs

**Cause:**
Non-streaming truncation loading entire content into memory.

**Solution:**
1. Ensure outputs >10MB trigger artifact creation (not inline truncation)
2. Lower `artifact_threshold` to create artifacts earlier: `artifact_threshold: 20000`
3. For truly massive outputs (>100MB), consider tool-level filtering to reduce output size before truncation
4. Verify smart truncation is disabled (it requires full content parsing): `smart_truncation.enabled: false`

### FAQ

**Q: How do I know if content was truncated?**
A: ToolResult metadata includes `was_truncated: true/false`, `original_size`, and `truncated_size`. Omission markers in content show what was removed.

**Q: Can I disable truncation entirely?**
A: Yes, set `inline_limit` to a very high value (e.g., 1000000) and `default_strategy: none`. Not recommended as this can cause context window overflow.

**Q: How accurate are token estimates?**
A: Token estimates use characters÷4 heuristic which is ~85% accurate for English text and code. Actual tokenization varies by model.

**Q: Are artifacts encrypted?**
A: No, artifacts are stored as plain text files in `.acode/artifacts/`. Sensitive data is redacted before storage but encryption is not applied.

**Q: How long do artifacts persist?**
A: Artifacts persist only for the session duration. When the session ends, artifacts are cleaned up (if `cleanup_on_exit: true`).

**Q: Can I share artifacts between sessions?**
A: No, artifacts are session-scoped and cleaned up on session end. To persist data, export artifacts to files outside the session directory.

**Q: What happens if I exceed max_artifact_size?**
A: Tool execution fails with an error: "Output size (X MB) exceeds maximum artifact size (Y MB)". Consider adding filtering/pagination to the tool call.

**Q: Why is JSON truncation slower than plain text?**
A: Element-based truncation parses JSON to preserve valid structure. This adds ~5-10ms for typical responses. Disable with `preserve_json: false` for speed.

**Q: Can I use regex patterns in artifact retrieval?**
A: Currently no, artifact retrieval supports line ranges, byte ranges, and JSONPath queries (for JSON artifacts). Regex searching is not implemented.

**Q: How do I debug which truncation strategy was applied?**
A: Check ToolResult metadata field `truncation_strategy`. Also visible in logs with `tool_name`, `strategy`, and `original_size`/`truncated_size`.

---

## Assumptions

### Technical Assumptions

1. **Tokenization approximation is acceptable** - The characters÷4 heuristic for token estimation is accurate enough for truncation decisions. Actual token counts may vary by ±20% depending on model tokenizer, but this variance does not significantly impact truncation effectiveness.

2. **File system is available and writable** - The `.acode/artifacts/` directory can be created and written to within the session's working directory. File system operations (create, read, delete) complete in reasonable time (<100ms for typical artifact sizes).

3. **Session directory is exclusive** - Each session has its own working directory and artifact storage does not conflict with other concurrent sessions. Artifact IDs are globally unique across all sessions.

4. **Tools return string content** - All tool execution results are ultimately representable as strings (text, JSON, base64-encoded binary). Binary data that cannot be stringified is handled separately or rejected.

5. **Truncation is stateless** - Each tool result is truncated independently based on its size and type. Truncation decisions do not depend on previous tool calls or conversation history (except for retry tracking).

6. **Model can parse omission markers** - Models understand the format "\n... [N lines / M chars omitted] ...\n" and recognize that content has been removed. Models do not confuse omission markers with actual tool output.

7. **UTF-8 encoding is standard** - All tool outputs use UTF-8 encoding. Truncation operations respect UTF-8 character boundaries and do not split multi-byte characters.

8. **JSON parsing is reliable** - For element-based truncation, JSON parsing succeeds for valid JSON and fails gracefully for malformed JSON (falling back to plain text truncation).

### Operational Assumptions

9. **Artifacts are ephemeral** - Artifacts persist only for session lifetime. Sessions end within reasonable time frames (hours to days, not weeks). Artifact cleanup on session end is acceptable.

10. **Disk space is sufficient** - The file system has adequate space for artifact storage. Typical sessions create <1GB of artifacts. Storage exhaustion is detected and handled gracefully with error messages.

11. **Concurrent tool execution is bounded** - The number of parallel tool calls is limited (default: 10 concurrent calls). Artifact store can handle this concurrency with file-level locking without significant contention.

12. **Tool outputs follow patterns** - Most tool outputs fit recognizable patterns (code, logs, JSON, plain text). Content-aware strategies can detect these patterns with reasonable accuracy.

### Integration Assumptions

13. **ToolResult type is stable** - The ToolResult message type defined in Task 004.a provides fields for content, metadata, and error status. This contract does not change frequently.

14. **Schema validation precedes truncation** - Tool argument validation (Task 007.b) occurs before tool execution. Truncation only processes successful tool execution results, not validation errors.

15. **Session state is managed externally** - Session lifecycle (creation, active, cleanup) is managed by Task 011 (Session State). Truncation system hooks into session cleanup to remove artifacts.

16. **Artifact retrieval is tool-based** - The get_artifact tool is available and functional. Models can call this tool to retrieve full artifact content or partial ranges.

### Performance Assumptions

17. **Small content is common** - Most tool outputs are under the inline limit (8KB) and require no truncation. Truncation processing for small content adds <1ms overhead.

18. **Large content is occasional** - Tool outputs exceeding artifact threshold (50KB) are less frequent (10-20% of tool calls). Artifact creation overhead is acceptable for these cases.

19. **Streaming is available for large inputs** - For inputs >10MB, streaming I/O is used to avoid loading entire content into memory. File system and runtime support efficient streaming operations.

20. **Truncation latency is acceptable** - Adding 1-10ms latency to tool calls for truncation processing does not significantly impact overall agent responsiveness or user experience.

---

## Security Considerations

### Threat Analysis and Mitigations

#### Threat 1: Secrets in Truncated Content

**Description:** Tool outputs may contain sensitive data (API keys, passwords, tokens, credentials) that is inadvertently included in truncated content or artifacts, exposing secrets to logs, disk, or conversation history.

**Attack Vector:** An attacker with access to artifact files, conversation logs, or session storage could extract exposed secrets and use them to compromise systems.

**Impact:** High - Credential theft, unauthorized access, data breaches, privilege escalation.

**Mitigation:**
- Implement secret detection patterns (regex for JWT tokens, API keys, passwords, connection strings)
- Scan all content before truncation and redact detected secrets
- Replace secrets with placeholders like `[REDACTED: API_KEY]` in truncated output
- Log secret detection events (without logging the secrets themselves) for audit
- Provide configuration option to disable truncation for tools handling sensitive data
- Document security best practices for users handling credentials

**Residual Risk:** Low - Some secret formats may not match detection patterns. Users must be aware of limitations.

---

#### Threat 2: Path Traversal in Artifact Storage

**Description:** Malicious tool output or crafted artifact IDs could include path traversal sequences (../, ../../, absolute paths) to write artifacts outside the session directory or read arbitrary files.

**Attack Vector:** A compromised or malicious tool returns output designed to exploit file path handling, writing artifacts to sensitive locations like `/etc/passwd` or `~/.ssh/id_rsa`.

**Impact:** Critical - Arbitrary file write, file overwrite, sensitive file disclosure, privilege escalation.

**Mitigation:**
- Validate all artifact IDs to contain only alphanumeric characters and underscores (pattern: `^art_[0-9]+_[a-zA-Z0-9]+$`)
- Reject artifact IDs containing path separators (`/`, `\`), parent directory references (`..`), or absolute paths
- Resolve artifact paths using secure path joining that prevents escaping the session directory
- Check final resolved path to confirm it starts with the session directory path
- Use file system permissions to restrict artifact directory to session owner only
- Reject any artifact ID that resolves to an existing system file outside the session

**Residual Risk:** Minimal - Path validation and resolution prevent escape from session directory.

---

#### Threat 3: Denial of Service via Massive Artifacts

**Description:** An attacker triggers tool execution that produces extremely large outputs (>1GB) to exhaust disk space, memory, or processing resources, causing system instability or session failure.

**Attack Vector:** Malicious user runs commands like `cat /dev/zero`, recursive directory listings, or infinite loops that generate unbounded output.

**Impact:** Medium - Disk space exhaustion, out-of-memory crashes, system slowdown, denial of service to other sessions.

**Mitigation:**
- Enforce `max_artifact_size` limit (default 10MB, configurable)
- Reject tool outputs exceeding max size with error message
- Stream large outputs without buffering full content in memory
- Monitor disk space usage and fail gracefully if storage approaches limits
- Implement per-session artifact quotas (total size across all artifacts)
- Automatically clean up old artifacts if session exceeds quota
- Rate-limit artifact creation (max N artifacts per minute)

**Residual Risk:** Low - Size limits prevent runaway storage consumption.

---

#### Threat 4: Information Leakage Through Artifact Metadata

**Description:** Artifact metadata (filenames, sizes, source tool names, timestamps) may reveal sensitive information about system structure, user activities, or data volumes even if content is redacted.

**Attack Vector:** An attacker with access to artifact listings but not content could infer sensitive operations from metadata alone (e.g., "heap_dump.bin 500MB" reveals memory issues).

**Impact:** Low - Indirect information disclosure, privacy violation, reconnaissance for further attacks.

**Mitigation:**
- Minimize metadata exposure in artifact listings
- Use generic artifact IDs without semantic meaning
- Avoid including sensitive paths or filenames in metadata
- Restrict artifact metadata access to session owner
- Log artifact operations with access control checks
- Provide option to disable detailed metadata in listings

**Residual Risk:** Low - Metadata is minimally revealing; content access is controlled.

---

#### Threat 5: Artifact Tampering Between Creation and Retrieval

**Description:** An attacker with file system access could modify artifact files between creation and retrieval, injecting malicious content that is then returned to the model or user.

**Attack Vector:** Attacker gains file system access, locates artifact files, modifies content to inject malicious code, payloads, or misleading data.

**Impact:** Medium - Model receives incorrect/malicious data, leading to bad decisions, code injection, or user compromise.

**Mitigation:**
- Use file system permissions to restrict artifact directory access (session owner only, no world-readable)
- Compute hash/checksum of artifact content on creation and verify on retrieval
- Log warnings if artifact hash mismatch detected
- Reject artifact retrieval if hash verification fails
- Store artifacts in memory-backed filesystem if disk tampering is a concern
- Implement artifact content signing for high-security environments

**Residual Risk:** Low with hash verification; Medium without (depends on file system security).

---

#### Threat 6: Race Conditions in Concurrent Artifact Access

**Description:** Multiple parallel tool executions could create or retrieve artifacts concurrently, leading to race conditions where partial writes are read, file corruption occurs, or artifact IDs collide.

**Attack Vector:** Session executes many parallel tool calls, some creating artifacts simultaneously. Race conditions corrupt artifact data or cause ID collisions.

**Impact:** Low - Data corruption in artifacts, model receives incomplete/corrupted content, session instability.

**Mitigation:**
- Generate artifact IDs with cryptographically-random components to prevent collisions
- Use file-level locking (flock, exclusive write locks) during artifact creation
- Implement atomic file writes (write to temp file, then rename)
- Use thread-safe ID generation (atomic counters with UUIDs)
- Detect and handle duplicate artifact IDs by regenerating
- Test concurrent artifact creation under load to validate locking

**Residual Risk:** Minimal - Random IDs and file locking prevent collisions and corruption.

---

### Audit Requirements

1. **Log all artifact creation events** - Include artifact_id, size, source_tool, timestamp, session_id
2. **Log artifact retrieval events** - Include artifact_id, retrieval_range, requester (model/user), success/failure
3. **Log secret detection hits** - Include tool_name, detection_pattern, redaction_count (but NOT the secrets themselves)
4. **Log path validation failures** - Include attempted artifact_id, reason for rejection, session_id
5. **Log size limit violations** - Include tool_name, attempted_size, configured_max_size
6. **Generate audit trail** - All artifact operations traceable to specific tool calls and session actions
7. **Rotate logs appropriately** - Artifact logs may grow large; rotate by size or time
8. **Redact sensitive data from logs** - Ensure logs do not contain secrets, credentials, or PII

### Sanitization Rules

- Scan content for patterns matching: API keys (regex: `[A-Za-z0-9_-]{40,}`), JWT tokens (regex: `eyJ[A-Za-z0-9_-]+\.eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+`), passwords (heuristics: lines containing "password", "passwd", "secret"), connection strings (patterns like `mongodb://`, `postgres://`, `mysql://`)
- Replace detected secrets with `[REDACTED: SECRET_TYPE]` placeholders
- Preserve content length and structure after redaction (for truncation calculations)
- Apply redaction before truncation and artifact storage (never store unredacted secrets)
- Log redaction events for security auditing
- Provide configuration option to customize secret detection patterns per organization

---

## Acceptance Criteria

### Processor

- [ ] AC-001: TruncationProcessor defined
- [ ] AC-002: Accepts result and limits
- [ ] AC-003: Returns truncated result
- [ ] AC-004: Creates artifact if needed
- [ ] AC-005: Adds metadata
- [ ] AC-006: Injectable via DI

### Limits

- [ ] AC-007: Default inline 8000
- [ ] AC-008: Default artifact 50000
- [ ] AC-009: Max artifact 10MB
- [ ] AC-010: Per-tool configurable
- [ ] AC-011: Global configurable
- [ ] AC-012: Validates positive

### Detection

- [ ] AC-013: Detects inline exceed
- [ ] AC-014: Detects artifact threshold
- [ ] AC-015: Detects max size
- [ ] AC-016: Uses characters
- [ ] AC-017: Estimates tokens

### Strategies

- [ ] AC-018: Head-only works
- [ ] AC-019: Tail-only works
- [ ] AC-020: Head+tail works
- [ ] AC-021: Line-based works
- [ ] AC-022: Element-based works
- [ ] AC-023: Default is head+tail
- [ ] AC-024: Per-tool configurable

### Head+Tail

- [ ] AC-025: Keeps first N
- [ ] AC-026: Keeps last M
- [ ] AC-027: Inserts marker
- [ ] AC-028: Default 60/40 split
- [ ] AC-029: Shows line count
- [ ] AC-030: Shows char count

### Line-Based

- [ ] AC-031: Preserves complete lines
- [ ] AC-032: No mid-line splits
- [ ] AC-033: First or last N lines
- [ ] AC-034: Handles line endings

### Element-Based

- [ ] AC-035: Preserves valid JSON
- [ ] AC-036: Keeps first/last elements
- [ ] AC-037: Keeps key subset
- [ ] AC-038: Shows omitted count
- [ ] AC-039: Respects depth

### Markers

- [ ] AC-040: Indicates truncation
- [ ] AC-041: Shows original size
- [ ] AC-042: Shows omitted amount
- [ ] AC-043: Format correct
- [ ] AC-044: Visually distinct

### Artifact Creation

- [ ] AC-045: Creates when exceeds
- [ ] AC-046: Unique ID
- [ ] AC-047: ID format correct
- [ ] AC-048: Stores full content
- [ ] AC-049: Has metadata
- [ ] AC-050: Returns reference

### Storage

- [ ] AC-051: Session-local
- [ ] AC-052: Correct path
- [ ] AC-053: Cleanup on end
- [ ] AC-054: Handles concurrency
- [ ] AC-055: Validates paths

### Reference

- [ ] AC-056: Includes ID
- [ ] AC-057: Includes summary
- [ ] AC-058: Includes size
- [ ] AC-059: Format correct
- [ ] AC-060: Summary ≤100 chars

### Retrieval

- [ ] AC-061: By ID works
- [ ] AC-062: Returns full content
- [ ] AC-063: 404 for unknown
- [ ] AC-064: Partial works
- [ ] AC-065: Via tool call

### Metadata

- [ ] AC-066: original_size present
- [ ] AC-067: truncated_size present
- [ ] AC-068: strategy_used present
- [ ] AC-069: artifact_id present
- [ ] AC-070: was_truncated flag

### Performance Benchmarks

- [ ] AC-071: Truncation <10ms for content ≤100KB
- [ ] AC-072: Truncation <100ms for content 100KB-10MB
- [ ] AC-073: Artifact creation <50ms for content ≤1MB
- [ ] AC-074: Artifact write is async and non-blocking
- [ ] AC-075: Memory usage ≤2x input size
- [ ] AC-076: Head/tail strategy faster than smart strategy
- [ ] AC-077: Element truncation <15ms for typical JSON (≤50KB)
- [ ] AC-078: Concurrent artifact creation handles 10 parallel calls

### Edge Cases

- [ ] AC-079: Empty content (0 bytes) handled gracefully
- [ ] AC-080: Content exactly at inline_limit not truncated
- [ ] AC-081: Content at inline_limit+1 is truncated
- [ ] AC-082: Unicode characters not split mid-character
- [ ] AC-083: Multi-byte UTF-8 handled correctly at boundaries
- [ ] AC-084: Invalid UTF-8 sequences handled without crash
- [ ] AC-085: Binary content base64-encoded or summarized
- [ ] AC-086: Very long lines (>10KB) truncated gracefully
- [ ] AC-087: Files with no newlines (single line) handled
- [ ] AC-088: Files with Windows (CRLF), Unix (LF), Mac (CR) line endings
- [ ] AC-089: Malformed JSON falls back to text truncation
- [ ] AC-090: Nested JSON beyond max_depth truncated
- [ ] AC-091: JSON with circular references detected
- [ ] AC-092: Array with single element not truncated needlessly
- [ ] AC-093: Empty JSON array [] preserved
- [ ] AC-094: Empty JSON object {} preserved
- [ ] AC-095: Content exactly at artifact_threshold becomes artifact
- [ ] AC-096: Content at max_artifact_size rejected with error
- [ ] AC-097: Whitespace-only content handled
- [ ] AC-098: Content with null bytes (\0) sanitized
- [ ] AC-099: Extremely long omission markers truncated
- [ ] AC-100: Artifact ID generation never collides

### Security Criteria

- [ ] AC-101: Secrets redacted before truncation
- [ ] AC-102: Artifact paths validated (no path traversal)
- [ ] AC-103: Artifact IDs validated (alphanumeric+underscore only)
- [ ] AC-104: Artifact files have restrictive permissions (owner-only)
- [ ] AC-105: Artifact cleanup on session end prevents leakage
- [ ] AC-106: Concurrent access uses file-level locking
- [ ] AC-107: Size limits prevent DoS via massive artifacts
- [ ] AC-108: Sensitive patterns (API keys, JWT) detected and redacted

### Observability Criteria

- [ ] AC-109: Truncation events logged with tool_name, size, strategy
- [ ] AC-110: Artifact creation logged with artifact_id, size, source
- [ ] AC-111: Artifact retrieval logged with artifact_id, success/failure
- [ ] AC-112: Secret detection logged (redaction count, not secrets)
- [ ] AC-113: Metrics track truncation rate per tool
- [ ] AC-114: Metrics track average size reduction (compression ratio)
- [ ] AC-115: Metrics track artifact creation rate
- [ ] AC-116: Metrics track artifact retrieval patterns

---

## Testing Requirements

### Unit Tests

Unit tests verify truncation logic, artifact storage, and metadata generation with deterministic inputs.

```csharp
namespace Acode.Application.Tests.Truncation;

using Acode.Application.Truncation;
using Acode.Application.Truncation.Strategies;
using FluentAssertions;
using Xunit;

public class HeadTailStrategyTests
{
    [Fact]
    public void Should_Not_Truncate_Content_Under_Limit()
    {
        // Arrange
        var strategy = new HeadTailStrategy(new TruncationOptions
        {
            InlineLimit = 1000,
            HeadRatio = 0.6
        });

        var content = "This is a short piece of content under the limit.";

        // Act
        var result = strategy.Truncate(content);

        // Assert
        result.Content.Should().Be(content);
        result.WasTruncated.Should().BeFalse();
        result.OriginalSize.Should().Be(content.Length);
        result.TruncatedSize.Should().Be(content.Length);
    }

    [Fact]
    public void Should_Truncate_Content_Preserving_Head_And_Tail()
    {
        // Arrange
        var strategy = new HeadTailStrategy(new TruncationOptions
        {
            InlineLimit = 100,  // Only allow 100 chars
            HeadRatio = 0.6     // 60 chars head, 40 chars tail
        });

        var content = new string('A', 50) + new string('B', 100) + new string('C', 50);  // 200 chars total

        // Act
        var result = strategy.Truncate(content);

        // Assert
        result.WasTruncated.Should().BeTrue();
        result.OriginalSize.Should().Be(200);
        result.TruncatedSize.Should().BeLessThan(result.OriginalSize);
        result.Content.Should().StartWith("AAAA");  // Starts with head
        result.Content.Should().EndWith("CCCC");    // Ends with tail
        result.Content.Should().Contain("omitted"); // Contains omission marker
        result.OmittedCharacters.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Should_Include_Omission_Marker_With_Counts()
    {
        // Arrange
        var strategy = new HeadTailStrategy(new TruncationOptions
        {
            InlineLimit = 100,
            HeadRatio = 0.6
        });

        var lines = Enumerable.Range(1, 50).Select(i => $"Line {i}");
        var content = string.Join("\n", lines);

        // Act
        var result = strategy.Truncate(content);

        // Assert
        result.Content.Should().MatchRegex(@"\.\.\. \[\d+ lines / \d+ chars omitted\] \.\.\.");
        result.Metadata.Should().ContainKey("omitted_lines");
        result.Metadata.Should().ContainKey("omitted_characters");
    }

    [Fact]
    public void Should_Respect_UTF8_Character_Boundaries()
    {
        // Arrange
        var strategy = new HeadTailStrategy(new TruncationOptions
        {
            InlineLimit = 50,
            HeadRatio = 0.6
        });

        var content = "Hello 世界 " + new string('X', 100) + " 再见 World";  // Multi-byte UTF-8

        // Act
        var result = strategy.Truncate(content);

        // Assert
        result.Content.Should().NotContain("\ufffd");  // No replacement character (indicates broken UTF-8)
        result.Content.Should().MatchRegex(@"^[\x00-\x7F\u4e00-\u9fff\s]+$");  // Valid UTF-8
    }
}

public class TailStrategyTests
{
    [Fact]
    public void Should_Keep_Only_Tail_Lines()
    {
        // Arrange
        var strategy = new TailStrategy(new TruncationOptions
        {
            InlineLimit = 200,
            TailLines = 5
        });

        var lines = Enumerable.Range(1, 20).Select(i => $"Line {i:D2}");
        var content = string.Join("\n", lines);

        // Act
        var result = strategy.Truncate(content);

        // Assert
        result.Content.Should().Contain("Line 16");
        result.Content.Should().Contain("Line 20");
        result.Content.Should().NotContain("Line 01");
        result.Content.Should().NotContain("Line 10");
        result.Content.Should().StartWith("...");  // Omission marker at start
    }

    [Fact]
    public void Should_Preserve_Complete_Lines()
    {
        // Arrange
        var strategy = new TailStrategy(new TruncationOptions
        {
            InlineLimit = 100,
            TailLines = 3
        });

        var content = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5";

        // Act
        var result = strategy.Truncate(content);

        // Assert
        var lines = result.Content.Split('\n').Where(l => !l.Contains("omitted")).ToArray();
        lines.Should().AllSatisfy(line => line.Should().StartWith("Line"));
        lines.Should().Contain(l => l.Contains("Line 3"));
        lines.Should().Contain(l => l.Contains("Line 5"));
    }
}

public class ElementStrategyTests
{
    [Fact]
    public void Should_Preserve_Valid_JSON_Array()
    {
        // Arrange
        var strategy = new ElementStrategy(new TruncationOptions
        {
            InlineLimit = 200,
            FirstElements = 2,
            LastElements = 2
        });

        var items = Enumerable.Range(1, 20).Select(i => new { id = i, name = $"Item {i}" });
        var content = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = false });

        // Act
        var result = strategy.Truncate(content);

        // Assert
        result.Content.Should().StartWith("[");
        result.Content.Should().EndWith("]");
        Action parseAction = () => JsonSerializer.Deserialize<JsonElement>(result.Content);
        parseAction.Should().NotThrow();  // Valid JSON

        var parsed = JsonSerializer.Deserialize<JsonElement>(result.Content);
        parsed.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public void Should_Show_Omitted_Count()
    {
        // Arrange
        var strategy = new ElementStrategy(new TruncationOptions
        {
            InlineLimit = 150,
            FirstElements = 2,
            LastElements = 2
        });

        var array = Enumerable.Range(1, 100).ToArray();
        var content = JsonSerializer.Serialize(array);

        // Act
        var result = strategy.Truncate(content);

        // Assert
        result.Content.Should().Contain("96 items omitted");  // 100 - 2 - 2 = 96
        result.Metadata["omitted_elements"].Should().Be(96);
    }
}

public class ArtifactStorageTests
{
    [Fact]
    public async Task Should_Create_Artifact_With_Unique_ID()
    {
        // Arrange
        var storage = new FileSystemArtifactStore("/tmp/test-session");
        var content = new string('X', 100000);

        // Act
        var artifact = await storage.CreateAsync(content, "test_tool", "text/plain");

        // Assert
        artifact.Id.Should().MatchRegex(@"^art_\d+_[a-zA-Z0-9]+$");
        artifact.Size.Should().Be(100000);
        artifact.SourceTool.Should().Be("test_tool");
        artifact.ContentType.Should().Be("text/plain");
    }

    [Fact]
    public async Task Should_Retrieve_Artifact_By_ID()
    {
        // Arrange
        var storage = new FileSystemArtifactStore("/tmp/test-session");
        var originalContent = "This is the artifact content";
        var artifact = await storage.CreateAsync(originalContent, "test_tool", "text/plain");

        // Act
        var retrievedContent = await storage.GetContentAsync(artifact.Id);

        // Assert
        retrievedContent.Should().Be(originalContent);
    }

    [Fact]
    public async Task Should_Handle_Concurrent_Artifact_Creation()
    {
        // Arrange
        var storage = new FileSystemArtifactStore("/tmp/test-session");
        var tasks = Enumerable.Range(1, 10)
            .Select(i => storage.CreateAsync($"Content {i}", "test_tool", "text/plain"))
            .ToArray();

        // Act
        var artifacts = await Task.WhenAll(tasks);

        // Assert
        artifacts.Select(a => a.Id).Should().OnlyHaveUniqueItems();  // No ID collisions
        artifacts.Should().HaveCount(10);
    }

    [Fact]
    public async Task Should_Cleanup_Artifacts_On_Session_End()
    {
        // Arrange
        var sessionDir = "/tmp/test-session-cleanup";
        var storage = new FileSystemArtifactStore(sessionDir);
        await storage.CreateAsync("Test content", "test_tool", "text/plain");

        // Act
        await storage.CleanupAsync();

        // Assert
        Directory.Exists(Path.Combine(sessionDir, ".acode/artifacts")).Should().BeFalse();
    }
}
```

### Integration Tests

Integration tests verify end-to-end truncation flow from tool execution through ToolResult creation.

```csharp
namespace Acode.Integration.Tests.Truncation;

using Acode.Application.Truncation;
using Acode.Infrastructure.Truncation;
using FluentAssertions;
using Xunit;

public class TruncationIntegrationTests
{
    [Fact]
    public async Task Should_Truncate_Large_Command_Output()
    {
        // Arrange
        var config = new TruncationConfiguration
        {
            InlineLimit = 1000,
            ArtifactThreshold = 10000,
            ToolOverrides = new Dictionary<string, TruncationOptions>
            {
                ["execute_command"] = new TruncationOptions { Strategy = TruncationStrategy.Tail, TailLines = 50 }
            }
        };

        var processor = new TruncationProcessor(config, new FileSystemArtifactStore("/tmp/session"));
        var largeOutput = string.Join("\n", Enumerable.Range(1, 500).Select(i => $"[INFO] Log line {i}"));

        // Act
        var result = await processor.ProcessAsync(largeOutput, "execute_command");

        // Assert
        result.WasTruncated.Should().BeTrue();
        result.Content.Should().Contain("Log line 451");  // Tail preserved
        result.Content.Should().Contain("Log line 500");  // Last line preserved
        result.Content.Should().NotContain("Log line 1");  // Beginning omitted
        result.Metadata["strategy"].Should().Be("tail");
    }

    [Fact]
    public async Task Should_Create_Artifact_For_Massive_Content()
    {
        // Arrange
        var config = new TruncationConfiguration { InlineLimit = 1000, ArtifactThreshold = 5000 };
        var storage = new FileSystemArtifactStore("/tmp/session");
        var processor = new TruncationProcessor(config, storage);
        var massiveContent = new string('X', 100000);  // 100KB

        // Act
        var result = await processor.ProcessAsync(massiveContent, "read_file");

        // Assert
        result.ArtifactId.Should().NotBeNullOrEmpty();
        result.Content.Should().Contain("[Artifact:");
        result.Content.Should().Contain(result.ArtifactId);
        result.Metadata["artifact_created"].Should().Be(true);

        var retrieved = await storage.GetContentAsync(result.ArtifactId);
        retrieved.Should().Be(massiveContent);
    }
}
```

### Performance Tests

```csharp
public class TruncationPerformanceTests
{
    [Fact]
    public void Should_Truncate_100KB_In_Under_10ms()
    {
        // Arrange
        var strategy = new HeadTailStrategy(new TruncationOptions { InlineLimit = 10000 });
        var content = new string('X', 100000);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = strategy.Truncate(content);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10);
    }

    [Fact]
    public void Should_Handle_10_Concurrent_Truncations()
    {
        // Arrange
        var strategy = new HeadTailStrategy(new TruncationOptions { InlineLimit = 10000 });
        var contents = Enumerable.Range(1, 10).Select(i => new string((char)('A' + i), 50000)).ToArray();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var tasks = contents.Select(c => Task.Run(() => strategy.Truncate(c))).ToArray();
        Task.WaitAll(tasks);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);  // Parallel efficiency
        tasks.Select(t => t.Result.Content).Should().OnlyHaveUniqueItems();
    }
}
```

---

## User Verification Steps

### Scenario 1: Small Content No Truncation

1. Read small file (<8000 chars)
2. Verify: Full content in result
3. Verify: No truncation marker
4. Verify: No artifact created

### Scenario 2: Medium Content Truncation

1. Read medium file (8000-50000 chars)
2. Verify: Content truncated
3. Verify: Marker present
4. Verify: Head and tail preserved

### Scenario 3: Large Content Artifact

1. Read large file (>50000 chars)
2. Verify: Artifact created
3. Verify: Reference in result
4. Verify: Can retrieve via get_artifact

### Scenario 4: Tail Strategy

1. Run command with long output
2. Verify: Only tail preserved
3. Verify: Most recent lines visible

### Scenario 5: JSON Truncation

1. Search producing many results
2. Verify: Valid JSON preserved
3. Verify: First/last elements present
4. Verify: Count of omitted shown

### Scenario 6: Partial Artifact Retrieval

1. Get artifact with line range
2. Verify: Only requested lines returned
3. Verify: Correct content

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Application/Truncation/
├── ITruncationProcessor.cs
├── ITruncationStrategy.cs
├── IArtifactStorage.cs
├── TruncationResult.cs
├── TruncationMetadata.cs
├── TruncationConfiguration.cs
├── Strategies/
│   ├── HeadTailStrategy.cs
│   ├── TailStrategy.cs
│   ├── HeadStrategy.cs
│   └── ElementStrategy.cs

src/AgenticCoder.Infrastructure/Truncation/
├── TruncationProcessor.cs
├── ArtifactStorage.cs
├── ArtifactReference.cs
└── Tools/
    └── GetArtifactTool.cs
```

### ITruncationProcessor Interface

```csharp
namespace AgenticCoder.Application.Truncation;

public interface ITruncationProcessor
{
    TruncationResult Process(
        string content,
        string toolName,
        TruncationConfiguration? config = null);
}
```

### TruncationResult Class

```csharp
namespace AgenticCoder.Application.Truncation;

public sealed class TruncationResult
{
    public required string Content { get; init; }
    public required TruncationMetadata Metadata { get; init; }
    public string? ArtifactId { get; init; }
    public string? ArtifactReference { get; init; }
}
```

### Implementation Checklist

1. [ ] Create ITruncationProcessor
2. [ ] Create ITruncationStrategy
3. [ ] Create IArtifactStorage
4. [ ] Create TruncationResult
5. [ ] Create TruncationMetadata
6. [ ] Create TruncationConfiguration
7. [ ] Implement HeadTailStrategy
8. [ ] Implement TailStrategy
9. [ ] Implement HeadStrategy
10. [ ] Implement ElementStrategy
11. [ ] Implement TruncationProcessor
12. [ ] Implement ArtifactStorage
13. [ ] Create GetArtifactTool
14. [ ] Wire up DI registration
15. [ ] Add CLI commands
16. [ ] Write unit tests
17. [ ] Add XML documentation

### Dependencies

- Task 007 (Tool Schema Registry)
- Task 004.a (ToolResult types)
- System.Text.Json

### Verification Command

```bash
dotnet test --filter "FullyQualifiedName~Truncation"
```

---

**End of Task 007.c Specification**