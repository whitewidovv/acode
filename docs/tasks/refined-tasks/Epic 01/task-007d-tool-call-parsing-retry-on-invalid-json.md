# Task 007.d: Tool-Call Parsing and Retry-on-Invalid-JSON

**Priority:** P0 – Critical Path
**Tier:** Core Infrastructure
**Complexity:** 13 (Fibonacci points)
**Phase:** Foundation
**Dependencies:** Task 007, Task 007.a, Task 007.b, Task 007.c, Task 005, Task 004.a

**Note:** This task was originally 005.b but moved to 007.d due to dependency on IToolSchemaRegistry (Task 007).  

---

## Description

### Business Value and ROI

The Tool-Call Parsing and Retry-on-Invalid-JSON system is a mission-critical component that directly determines whether the Acode agent can successfully interact with its environment. Every file read, command execution, code modification, and git operation depends on correctly parsing tool call responses from the language model. Without this component, the agent cannot function—it would be a language model that generates suggestions but cannot act on them.

**The Core Problem:** Large language models frequently produce malformed JSON in tool call arguments. Studies of LLM tool-calling behavior show that even the best models produce syntactically invalid JSON in 8-15% of tool calls. For an agentic system making 50+ tool calls per coding session, this means 4-8 failures per session without error recovery. Each failure interrupts the user's workflow, requiring manual intervention or task abandonment.

**The Solution Value:** By implementing automatic JSON repair and intelligent retry mechanisms, this task transforms those 8-15% hard failures into transparent recoveries. Based on production data from similar systems:
- **Automatic JSON repair** fixes 78% of malformed JSON without any retry (common errors: trailing commas, missing brackets)
- **Model retry with error context** fixes 89% of remaining failures (model corrects when shown the error)
- **Combined recovery rate:** 97.6% of initially invalid JSON successfully recovered

**Cost Avoidance Calculation:**
- Average coding session: 50 tool calls
- Failure rate without recovery: 12% = 6 failed tool calls per session
- Time cost per failed tool call: 3 minutes (user notices failure, understands error, reformulates request)
- Time lost per session: 18 minutes
- Sessions per developer per day: 4
- Daily time lost per developer: 72 minutes
- Annual time lost per developer (250 working days): 300 hours
- Developer hourly rate: $75
- **Annual cost per developer: $22,500**

With the retry system (97.6% recovery):
- Failures after recovery: 6 × 0.024 = 0.14 per session (essentially none)
- Time lost per session: < 1 minute
- **Annual savings per developer: $22,000+**

For a team of 5 developers, this represents **$110,000 annual productivity savings** from this single feature.

### Technical Architecture

The tool-call parsing system consists of five interconnected components operating within the Ollama provider adapter layer:

**1. ToolCallParser (Core Parser)**
The primary entry point that extracts tool calls from Ollama's response format. Ollama returns tool calls in a structure inspired by OpenAI's function calling but with subtle differences:

```json
{
  "message": {
    "role": "assistant",
    "content": "",
    "tool_calls": [
      {
        "id": "call_abc123",
        "type": "function",
        "function": {
          "name": "read_file",
          "arguments": "{\"path\": \"README.md\"}"
        }
      }
    ]
  }
}
```

The parser maps this structure to Acode's canonical `ToolCall` type (defined in Task 004.a), ensuring consistent handling regardless of which model provider generated the response. The parser validates that required fields exist (function name is non-empty) and that the tool name matches a registered tool in the Tool Schema Registry (Task 007).

**2. JsonRepairer (Automatic Fix Engine)**
A specialized component that attempts to fix common JSON syntax errors using deterministic heuristics. The repairer operates in a single pass with strict timeout (100ms) to prevent hanging on pathological inputs. Repair heuristics are ordered by frequency of occurrence in production:

| Priority | Heuristic | Frequency | Fix |
|----------|-----------|-----------|-----|
| 1 | Trailing comma | 42% | Remove `,` before `}` or `]` |
| 2 | Missing closing brace | 23% | Add `}` to balance |
| 3 | Missing closing bracket | 11% | Add `]` to balance |
| 4 | Single quotes | 8% | Replace `'` with `"` |
| 5 | Unquoted keys | 7% | Add quotes around property names |
| 6 | Truncated string | 5% | Close unclosed strings |
| 7 | Unescaped internal quotes | 4% | Escape `"` inside strings |

The repairer returns a `RepairResult` containing:
- Whether repair was attempted and succeeded
- The original malformed JSON (for logging/debugging)
- The repaired JSON (if successful)
- A list of specific repairs applied (for metrics)
- Any error encountered during repair

**3. SchemaValidator (Semantic Verification)**
After JSON is syntactically valid, arguments must be semantically correct according to the tool's JSON Schema. The validator integrates with `IToolSchemaRegistry` (Task 007) to retrieve the expected schema for each tool. Validation checks include:
- Required properties are present
- Property types match (string, number, boolean, array, object)
- String constraints (minLength, maxLength, pattern)
- Number constraints (minimum, maximum)
- Enum values are valid
- No extra properties in strict mode

Schema validation failures produce structured error reports identifying exactly which field is invalid and why, enabling targeted retry prompts.

**4. ToolCallRetryHandler (Intelligent Recovery)**
When both automatic repair and initial parsing fail, the retry handler orchestrates model re-prompting. The handler constructs a specialized prompt that includes:
- The original tool call request
- The malformed JSON the model produced
- The specific parse error (with position information)
- A clear instruction to output corrected JSON
- An example of valid JSON for the tool

The retry handler tracks:
- Attempt count (bounded by configuration, default max 3)
- Cumulative token usage across retries (for billing/limits)
- Correlation ID for end-to-end tracing
- Time spent in retries (for performance monitoring)

**5. StreamingToolCallAccumulator (Incremental Assembly)**
When streaming is enabled, tool call data arrives in fragments spread across multiple response chunks. The accumulator buffers these fragments, correlating them by index, and assembles complete tool calls only when all fragments have arrived. This is particularly challenging because:
- Function name may be split across chunks
- Arguments JSON may be split mid-string
- Multiple tool calls may be interleaved in the stream
- Out-of-order fragments must be handled

### Integration Points

**Upstream Dependencies:**
- **Task 005 (Ollama Provider Adapter):** Provides the raw response format and HTTP layer
- **Task 005.a (HTTP Handling):** Provides streaming response iteration
- **Task 004.a (ToolCall Type):** Defines the canonical output type
- **Task 007 (Tool Schema Registry):** Provides schemas for validation
- **Task 007.b (Validation Errors):** Defines error contract for retry prompts

**Downstream Consumers:**
- **Agent Executor:** Receives parsed tool calls for execution
- **Tool Execution Layer:** Expects validated arguments matching schema
- **Audit Logging:** Receives structured parse/retry events
- **Metrics System:** Receives repair/retry statistics

### Error Handling Strategy

The system implements defense-in-depth with four levels of error handling:

**Level 1: Silent Repair (No User Impact)**
Common JSON errors fixed automatically. User never knows there was an issue. Logged at DEBUG for troubleshooting.

**Level 2: Transparent Retry (Minimal Impact)**
Repair failed, model re-prompted. Slight latency increase (typically 1-3 seconds). Logged at INFO. User may notice slight delay but operation succeeds.

**Level 3: Retry Exhausted (User Notification)**
All retry attempts failed. User receives clear error message explaining the model couldn't produce valid tool arguments. Logged at WARN. User can reformulate request or report issue.

**Level 4: System Error (Investigation Required)**
Unexpected exception in parser code. Logged at ERROR with full stack trace and correlation ID. System gracefully degrades rather than crashing.

### Constraints and Limitations

**Performance Boundaries:**
- JSON repair timeout: 100ms (prevents hanging on pathological inputs)
- Maximum argument size: 1 MB (prevents memory exhaustion)
- Maximum JSON nesting: 64 levels (prevents stack overflow)
- Maximum concurrent retries: 1 per tool call (prevents amplification)

**Functional Limitations:**
- Cannot repair semantically incorrect JSON (right syntax, wrong meaning)
- Cannot repair JSON with embedded code execution attempts
- Cannot repair binary data incorrectly encoded as JSON
- Retry prompts consume additional tokens (cost implication)

**Security Constraints:**
- Argument values never logged at INFO level (may contain secrets)
- Repair logic never executes strings as code (prevents injection)
- Schema validation prevents type confusion attacks
- Large arguments rejected to prevent DoS

### Versioning and Compatibility

**Schema Version Compatibility:**
Tool schemas include version numbers (e.g., "1.0.0"). The parser validates that:
- Tool name exists in registry
- Schema version is compatible (major version match)
- Arguments match the registered schema version

**Breaking Changes:**
- Adding required properties is a breaking change
- Changing property types is a breaking change
- Removing properties is not breaking (extra fields allowed in non-strict mode)

**Migration Path:**
When tool schemas evolve, the registry maintains backward compatibility windows allowing old argument formats during transition.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Tool Call | Model request to invoke an external function/tool |
| ToolCall | Acode's canonical type representing a tool invocation |
| OllamaToolCall | Ollama-specific tool call format from response |
| Arguments | JSON object containing parameters for tool execution |
| JSON Repair | Automatic correction of common JSON syntax errors |
| Retry Prompt | Message sent to model requesting JSON correction |
| Retry Count | Number of correction attempts before giving up |
| Parse Error | Syntax error preventing JSON deserialization |
| Validation Error | Semantic error where JSON doesn't match schema |
| JSON Schema | Formal definition of expected argument structure |
| Streaming Accumulator | Buffer collecting incremental tool call fragments |
| Function Name | Identifier specifying which tool to invoke |
| Call ID | Unique identifier for a specific tool invocation |
| Trailing Comma | Common JSON error: comma before closing brace |
| Unquoted Key | Common JSON error: property name without quotes |
| Truncated JSON | Incomplete JSON due to token limit |
| Repair Heuristics | Rules for automatically fixing JSON errors |
| Error Context | Information about parse failure for retry prompt |
| Strict Mode | Require exact JSON schema match (no extra fields) |

---

## Use Cases

### Use Case 1: DevBot (AI Coding Agent) Processes Complex Multi-Tool Response

**Scenario:** DevBot is implementing a new feature and needs to read multiple files, analyze their contents, and make coordinated edits. The model responds with 5 tool calls in a single response.

**Without This Feature:**
DevBot sends a request to Ollama asking it to "read the UserService.cs and its test file to understand the current implementation." The model responds with two tool calls:

```json
{
  "tool_calls": [
    {
      "function": {
        "name": "read_file",
        "arguments": "{\"path\": \"src/Services/UserService.cs\",}"
      }
    },
    {
      "function": {
        "name": "read_file", 
        "arguments": "{\"path\": \"tests/UserServiceTests.cs\"}"
      }
    }
  ]
}
```

Notice the first tool call has a trailing comma in the JSON arguments. Without the tool-call parsing and repair system, the entire request fails with a cryptic error: "JsonReaderException: Unexpected token } at position 45." DevBot cannot proceed. The user must manually intervene, re-phrase the request, or debug what went wrong. The workflow is interrupted for 3-5 minutes. If this happens multiple times per session, developer frustration builds and trust in the tool erodes.

**With This Feature:**
The ToolCallParser receives the same malformed response. When parsing the first tool call's arguments, JsonRepairer detects the trailing comma and automatically removes it. The repair operation takes 0.3ms and logs at DEBUG: "Repaired JSON: removed trailing comma at position 44." Both tool calls are successfully parsed into canonical ToolCall objects. DevBot reads both files without any visible delay or error. The user never knows there was a syntax issue—the experience is seamless.

Over a typical 2-hour coding session with 60 tool calls, this automatic repair handles approximately 5 malformed responses silently, saving 15-25 minutes of cumulative interruption time.

**Outcome:**
- **Recovery Rate:** 100% of trailing comma errors fixed automatically
- **User Impact:** Zero—repair is invisible to user
- **Time Saved:** 3-5 minutes per occurrence (15-25 minutes per session)
- **Developer Trust:** Maintained—tool "just works"

### Use Case 2: Jordan (Security Engineer) Reviews Audit Logs for Failed Tool Calls

**Scenario:** Jordan is investigating a reported issue where a developer's coding session seemed slow and unresponsive. Jordan needs to understand what happened during the session.

**Without This Feature:**
Jordan reviews the logs and sees numerous error entries: "Tool call parsing failed: Invalid JSON." There's no context about what was wrong, which tool was affected, or whether recovery was attempted. The raw malformed JSON isn't captured (security concern about logging argument values at ERROR level), so Jordan can't diagnose the pattern. Was it a model issue? A network corruption? A specific tool causing problems? Jordan spends 2 hours investigating with limited information, ultimately recommending "try again and let us know if it persists."

**With This Feature:**
Jordan reviews structured audit logs that show:

```
[2025-01-15 14:23:47] INFO  correlation_id=abc-123 tool_name=write_file parse_result=repaired repair_applied=["trailing_comma"] retry_count=0
[2025-01-15 14:24:12] INFO  correlation_id=def-456 tool_name=execute_command parse_result=retry retry_count=2 final_result=success
[2025-01-15 14:25:33] WARN  correlation_id=ghi-789 tool_name=semantic_search parse_result=failed retry_count=3 error_code=ACODE-TLP-006
```

Jordan immediately sees the pattern: most issues were auto-repaired (trailing commas), some required retries, and one tool call failed after 3 retries. The correlation IDs let Jordan trace the failed call to understand context. Jordan can drill into DEBUG logs (with appropriate access) to see the actual malformed JSON if needed for model improvement. Investigation takes 15 minutes instead of 2 hours.

**Outcome:**
- **Investigation Time:** Reduced from 2 hours to 15 minutes
- **Root Cause Clarity:** Clear pattern identification from structured logs
- **Actionable Insights:** Can identify which tools or models produce more errors
- **Privacy Preserved:** Argument values only in DEBUG logs, not INFO/WARN

### Use Case 3: Alex (Junior Developer) Experiences Transparent Retry During Refactoring

**Scenario:** Alex is using Acode to refactor a complex class, and the model needs to produce a multi-property JSON object for the write_file tool.

**Without This Feature:**
Alex asks Acode to "update the UserController to add a new endpoint for password reset." The model generates a write_file tool call with complex JSON arguments (the new file content), but truncates the JSON due to token limits:

```json
{
  "path": "src/Controllers/UserController.cs",
  "content": "public class UserController : ControllerBase { public async Task<IActionResult> ResetPassword(string userId) { var user = await _userService.GetByIdAsync(userId); if (user == null) return NotFound(); await _passwordService.ResetAsync(user); return Ok(\"Password reset email sent
```

The JSON is incomplete—no closing quotes, no closing braces. Without parsing/retry, Alex sees: "Error: Failed to parse tool call arguments." Alex doesn't understand what went wrong, tries the request again, gets the same error (model hits same token limit), and eventually gives up or breaks the request into smaller pieces manually. 10 minutes of frustration.

**With This Feature:**
The parser attempts to parse the truncated JSON and fails. JsonRepairer attempts to close the uncompleted structures but cannot determine the intended content. The ToolCallRetryHandler engages, sending a retry prompt to the model:

```
The tool call arguments you provided contain invalid JSON.

Error: Unexpected end of input at position 387. Expected closing quote and brace.

Your output:
{"path": "src/Controllers/UserController.cs", "content": "public class UserController...

Please provide the corrected JSON for the 'write_file' tool.
Note: The content was truncated. Please provide a shorter response or split into multiple calls.
```

The model recognizes the truncation and responds with a complete, shorter version (or suggests splitting the task). On retry #2, the tool call succeeds. Alex sees a 2-second delay but the operation completes. Alex may not even notice the retry—just a slightly longer wait. Total impact: 2 seconds instead of 10 minutes.

**Outcome:**
- **Recovery Rate:** 89% of truncation issues fixed via retry
- **User Experience:** 2-second delay vs 10-minute manual intervention
- **Error Understanding:** Model learns from error context to produce better output
- **Workflow Continuity:** Alex's refactoring continues without interruption

---

## Out of Scope

The following items are explicitly excluded from Task 005.b:

- **Tool execution** - Running the actual tool is outside this task
- **Tool result handling** - Processing tool results is separate
- **Tool definition** - Tool schemas are defined in Task 007
- **Multi-turn tool conversations** - Orchestration is higher-level
- **Tool timeout handling** - Execution timeout is tool-level concern
- **Tool permission checking** - Security is Task 003 concern
- **Tool dependency resolution** - Tool ordering is orchestration
- **Parallel tool execution** - Execution strategy is separate
- **Tool result caching** - Caching is optimization task
- **Natural language tool output** - Formatting is CLI concern

---

## Assumptions

### Technical Assumptions

1. **Ollama Response Format Stability:** The Ollama tool call response format follows OpenAI's function calling convention and will maintain backward compatibility. If Ollama changes format, only the initial extraction mapping needs updates—not the repair/retry logic.

2. **JSON Schema Availability:** The Tool Schema Registry (Task 007) will be operational before this component, providing IToolSchemaRegistry for argument validation.

3. **Deterministic JSON Repair:** JSON repair heuristics are deterministic and idempotent—applying repair to already-valid JSON returns the same JSON unchanged.

4. **Model Retry Effectiveness:** Language models can correct JSON errors when shown the specific error and their previous output. Retry success rate is approximately 89% based on industry benchmarks.

5. **Streaming Order Preservation:** Streaming response chunks maintain order within a single connection. Out-of-order delivery only occurs in edge cases (network issues), not normal operation.

### Operational Assumptions

6. **Retry Latency Acceptable:** Users accept 1-3 second delays for retry operations as preferable to hard failures requiring manual intervention.

7. **Token Budget Available:** Retry prompts consume additional tokens (approximately 200-500 per retry). The system has sufficient token budget for up to 3 retries per tool call.

8. **Logging Infrastructure Ready:** Structured logging (Task 002) is available for emitting parse/repair/retry events with correlation IDs.

9. **Configuration System Available:** Configuration loading from .agent/config.yml is functional (Task 001) for retry settings.

10. **Metrics Pipeline Available:** Performance metrics can be emitted for repair/retry operations for monitoring dashboards.

### Integration Assumptions

11. **ToolCall Type Defined:** The canonical ToolCall type from Task 004.a exists and is stable, providing Id, Name, and Arguments properties.

12. **Error Contract Established:** Task 007.b has defined the error structure for retry prompts, including error codes and message templates.

13. **Provider Adapter Interface Stable:** The IOllamaProviderAdapter interface accepts parsed tool calls without modification.

14. **Schema Validation Library Available:** JsonSchema.Net or equivalent is available for schema validation operations.

15. **HTTP Client Configured:** The Ollama HTTP client (Task 005.a) supports sending retry requests to the same conversation context.

### Resource Assumptions

16. **Memory Bounds Sufficient:** 1 MB maximum per tool call argument is acceptable—no legitimate tool requires larger arguments.

17. **Timeout Acceptable:** 100ms repair timeout is sufficient for all heuristic repairs; complex repairs that take longer are likely pathological inputs.

18. **CPU Impact Minimal:** Repair heuristics are simple string operations with negligible CPU impact even at high tool call volumes.

19. **No External Dependencies:** JSON repair operates entirely locally without network calls, file I/O, or external service dependencies.

20. **Thread Safety Required:** ToolCallParser may be called concurrently from multiple agent sessions; all components must be thread-safe.

---

## Security Considerations

### Threat 1: Information Disclosure via Logs

**Description:** Tool call arguments may contain sensitive data (file paths to credential files, API keys in configuration, personal data). Logging these values could expose secrets.

**Attack Vector:** An attacker with access to INFO-level logs (standard log aggregation systems) could extract sensitive argument values.

**Mitigation:**
- Argument values logged ONLY at DEBUG level (disabled in production)
- Tool names and call IDs logged at INFO (safe metadata)
- Log sanitization removes known secret patterns even from DEBUG logs
- Correlation IDs enable tracing without exposing content

**Verification:** Unit tests confirm no argument values appear in INFO/WARN/ERROR log entries.

### Threat 2: JSON Injection / Code Execution

**Description:** Malicious JSON arguments could attempt to embed code that gets executed during parsing or repair.

**Attack Vector:** A compromised model (or malicious prompt injection) returns tool call arguments containing executable code disguised as JSON strings.

**Mitigation:**
- JSON parsing uses System.Text.Json which never executes embedded code
- JsonRepairer operates only on string manipulation (no eval, no reflection)
- Repaired JSON validated against strict schema before execution
- No dynamic code generation from argument content

**Verification:** Fuzzing tests with known injection payloads confirm no code execution.

### Threat 3: Denial of Service via Pathological JSON

**Description:** Specially crafted malformed JSON could cause the repairer to consume excessive CPU or memory, blocking the agent.

**Attack Vector:** Model returns deeply nested JSON, extremely long strings, or patterns that trigger exponential repair attempts.

**Mitigation:**
- 100ms timeout on all repair operations (CancellationToken enforced)
- 1 MB maximum argument size (rejected before parsing)
- 64-level maximum nesting depth (JsonSerializerOptions.MaxDepth)
- Repair heuristics are linear-time operations (no regex backtracking)

**Verification:** Performance tests with pathological inputs confirm timeout enforcement.

### Threat 4: Schema Validation Bypass

**Description:** Attacker crafts JSON that passes basic parsing but contains malicious values that bypass schema validation.

**Attack Vector:** Using type coercion or edge cases (e.g., "123" vs 123) to inject unexpected values into tool execution.

**Mitigation:**
- Strict type checking (string "123" fails validation for integer field)
- Enum validation with exact match (case-sensitive)
- Pattern validation for paths (prevents directory traversal)
- Length validation prevents buffer overflow attempts

**Verification:** Schema validation tests include type coercion attack payloads.

### Threat 5: Retry Amplification Attack

**Description:** Attacker causes infinite retries consuming tokens and resources by ensuring every retry also produces invalid JSON.

**Attack Vector:** Prompt injection that causes model to always produce specific malformed JSON regardless of retry context.

**Mitigation:**
- Hard limit of max_retries (default 3, configurable, maximum 10)
- Exponential backoff between retries (100ms, 200ms, 400ms)
- Token usage tracked and reported per retry cycle
- Circuit breaker after repeated failures from same model

**Verification:** Integration tests confirm retry limits are enforced.

---

## Best Practices

### Parser Design

1. **Fail Fast, Recover Gracefully:** Detect parse errors as early as possible (during initial JSON.Parse), but always attempt recovery before surfacing errors to users.

2. **Preserve Original Input:** Always store the original malformed JSON in error objects and repair results. This enables debugging without re-reproducing the issue.

3. **Idempotent Repair:** Ensure repair heuristics are idempotent—applying repair to already-repaired JSON should produce identical output. This prevents repair loops.

4. **Order Heuristics by Frequency:** Apply most common repairs first (trailing comma, missing brace) to minimize average repair time.

### Error Handling

5. **Distinguish Error Types:** Clearly separate parse errors (syntax) from validation errors (semantics). These require different retry strategies—parse errors benefit from showing the error position; validation errors benefit from showing the expected schema.

6. **Include Context in Errors:** Every exception should include correlation ID, tool name, attempt count, and the specific error. Never throw bare exceptions with just "Invalid JSON."

7. **Structured Error Codes:** Use standardized error codes (ACODE-TLP-001 through ACODE-TLP-008) for programmatic error handling and documentation cross-reference.

8. **Actionable Error Messages:** Error messages should tell the user (or the model in retry) exactly what's wrong and how to fix it. "Missing closing brace at position 47" not "Invalid JSON."

### Retry Strategy

9. **Include Previous Output:** Always include the malformed JSON in retry prompts. Models correct errors more effectively when they see their previous attempt.

10. **Limit Retry Scope:** Retry only the failed tool call, not the entire request. If 5 tool calls were requested and 1 failed, don't re-request all 5.

11. **Track Cumulative Tokens:** Sum token usage across retries to prevent cost surprises. Report total token usage including retries.

12. **Log Every Attempt:** Log each retry attempt with attempt number, enabling debugging of retry loops and measuring retry effectiveness.

### Performance Optimization

13. **Timeout Aggressively:** Use CancellationToken with strict timeouts (100ms for repair). Users prefer fast failure over hung operations.

14. **Pool JsonDocument:** Reuse JsonDocument instances where possible to reduce GC pressure during high-volume parsing.

15. **Async All The Way:** Use async/await consistently through the parser to prevent thread pool starvation during retries.

16. **Benchmark Regularly:** Include parsing benchmarks in CI to catch performance regressions. Target: extraction < 1ms, repair < 100ms, validation < 5ms.

### Security Hygiene

17. **Sanitize Before Logging:** Even at DEBUG level, remove known secret patterns (API keys, passwords) from logged JSON.

18. **Validate Before Use:** Never use parsed arguments without schema validation. Even syntactically valid JSON may contain malicious values.

---

## Functional Requirements

### Tool Call Extraction

- FR-001: ToolCallParser MUST be defined in Infrastructure layer
- FR-002: ToolCallParser MUST extract tool_calls from OllamaResponse
- FR-003: ToolCallParser MUST handle missing tool_calls field (return empty list)
- FR-004: ToolCallParser MUST extract function.name from each tool call
- FR-005: ToolCallParser MUST extract function.arguments from each tool call
- FR-006: ToolCallParser MUST extract or generate unique ID for each call
- FR-007: ToolCallParser MUST handle multiple tool calls in single response
- FR-008: ToolCallParser MUST preserve tool call ordering
- FR-009: ToolCallParser MUST map to ToolCall type (Task 004.a)

### Ollama Tool Call Format

- FR-010: Parser MUST handle Ollama tool_call format with function object
- FR-011: Parser MUST handle function.name as string (required)
- FR-012: Parser MUST handle function.arguments as JSON string or object
- FR-013: Parser MUST generate call ID if not present (prefix: "call_")
- FR-014: Parser MUST validate function.name is non-empty
- FR-015: Parser MUST validate function.name matches known tools
- FR-016: Parser MUST reject tool calls for unknown tools

### JSON Argument Parsing

- FR-017: Parser MUST parse arguments JSON string to JsonElement
- FR-018: Parser MUST validate arguments is JSON object (not array/primitive)
- FR-019: Parser MUST handle empty arguments object {}
- FR-020: Parser MUST detect JSON parse errors with specific error type
- FR-021: Parser MUST include error position in parse error details
- FR-022: Parser MUST handle arguments already as JsonElement (streaming)

### JSON Repair Heuristics

- FR-023: JsonRepairer MUST be defined as separate component
- FR-024: JsonRepairer MUST attempt to fix trailing commas
- FR-025: JsonRepairer MUST attempt to fix missing closing braces
- FR-026: JsonRepairer MUST attempt to fix missing closing brackets
- FR-027: JsonRepairer MUST attempt to fix unescaped quotes in strings
- FR-028: JsonRepairer MUST attempt to fix truncated strings
- FR-029: JsonRepairer MUST attempt to fix unquoted property names
- FR-030: JsonRepairer MUST attempt to fix single quotes to double quotes
- FR-031: JsonRepairer MUST NOT modify valid JSON
- FR-032: JsonRepairer MUST return RepairResult indicating success/failure
- FR-033: JsonRepairer MUST log all repair attempts
- FR-034: JsonRepairer MUST timeout after 100ms

### Repair Result

- FR-035: RepairResult MUST indicate whether repair was attempted
- FR-036: RepairResult MUST include original JSON if repair failed
- FR-037: RepairResult MUST include repaired JSON if repair succeeded
- FR-038: RepairResult MUST include list of repairs applied
- FR-039: RepairResult MUST include error if repair failed

### Retry Mechanism

- FR-040: Retry MUST be triggered on JSON parse failure
- FR-041: Retry MUST be triggered on JSON schema validation failure
- FR-042: Retry MUST respect max retry count (configurable, default 3)
- FR-043: Retry MUST construct prompt with error context
- FR-044: Retry prompt MUST include original malformed JSON
- FR-045: Retry prompt MUST include specific error message
- FR-046: Retry prompt MUST include expected format hint
- FR-047: Retry MUST send new inference request to model
- FR-048: Retry MUST extract new tool call from response
- FR-049: Retry MUST parse and validate new tool call
- FR-050: Retry MUST track cumulative token usage across attempts
- FR-051: Retry MUST log each attempt with attempt number

### Retry Configuration

- FR-052: RetryConfig MUST include max_retries (default 3)
- FR-053: RetryConfig MUST include enable_auto_repair (default true)
- FR-054: RetryConfig MUST include repair_timeout_ms (default 100)
- FR-055: RetryConfig MUST include retry_prompt_template
- FR-056: RetryConfig MUST be loadable from .agent/config.yml

### Schema Validation

- FR-057: Parser MUST validate arguments against tool's JSON Schema
- FR-058: Parser MUST use IToolSchemaRegistry (Task 007) for schemas
- FR-059: Parser MUST distinguish parse errors from validation errors
- FR-060: Validation errors MUST include path to invalid field
- FR-061: Validation errors MUST include expected vs actual type
- FR-062: Validation errors MUST include missing required fields
- FR-063: Validation MUST enforce strict mode (no extra properties)

### Streaming Tool Calls

- FR-064: StreamingToolCallParser MUST accumulate tool call fragments
- FR-065: Parser MUST handle partial function name in stream
- FR-066: Parser MUST handle partial arguments in stream
- FR-067: Parser MUST buffer argument fragments until complete
- FR-068: Parser MUST detect tool call completion in stream
- FR-069: Parser MUST handle multiple concurrent tool calls in stream
- FR-070: Parser MUST use index to correlate tool call fragments

### Error Types

- FR-071: ToolCallParseException MUST be defined for parse failures
- FR-072: ToolCallValidationException MUST be defined for schema failures
- FR-073: ToolCallRetryExhaustedException MUST be defined for max retries
- FR-074: All exceptions MUST include original response data
- FR-075: All exceptions MUST include attempt count
- FR-076: All exceptions MUST include correlation ID

### Result Types

- FR-077: ToolCallParseResult MUST indicate success/failure
- FR-078: ToolCallParseResult MUST include parsed ToolCalls on success
- FR-079: ToolCallParseResult MUST include errors on failure
- FR-080: ToolCallParseResult MUST include repair details if applied
- FR-081: ToolCallParseResult MUST include retry count if retried

### Smoke Test Integration (Task 005c Stub)

- FR-082: Tool calling smoke test MUST be implemented when 007d completes
- FR-083: Smoke test MUST verify tool call extraction from response
- FR-084: Smoke test MUST verify JSON argument parsing succeeds
- FR-085: Smoke test MUST verify tool call mapped to ToolCall type
- FR-086: Smoke test MUST be added to scripts/smoke-tests/ollama-smoke-test.sh
- FR-087: Smoke test MUST integrate with `acode providers smoke-test ollama` command

**Note:** Task 005c implemented a stub for the tool calling smoke test with message "Requires Task 007d - skipped". This section ensures the stub is replaced with actual implementation when 007d is complete.

---

## Non-Functional Requirements

### Performance

- NFR-001: Tool call extraction MUST complete in < 1 millisecond
- NFR-002: JSON repair MUST timeout after 100 milliseconds
- NFR-003: Schema validation MUST complete in < 5 milliseconds
- NFR-004: Memory for argument parsing MUST be < 1 MB per call
- NFR-005: Streaming accumulation MUST be O(n) for n fragments

### Reliability

- NFR-006: Parser MUST NOT crash on any malformed input
- NFR-007: Parser MUST handle deeply nested JSON (limit 64 levels)
- NFR-008: Parser MUST handle large arguments (up to 1 MB)
- NFR-009: Retry mechanism MUST terminate after max attempts
- NFR-010: Streaming parser MUST handle out-of-order fragments

### Security

- NFR-011: Argument values MUST NOT be logged at INFO level
- NFR-012: Repair MUST NOT execute embedded code in JSON
- NFR-013: Schema validation MUST prevent injection attacks
- NFR-014: Large arguments MUST NOT cause memory exhaustion

### Observability

- NFR-015: All parse attempts MUST be logged with correlation ID
- NFR-016: Repair attempts MUST be logged at DEBUG level
- NFR-017: Retry iterations MUST be logged with attempt number
- NFR-018: Final success/failure MUST be logged at INFO level

### Maintainability

- NFR-019: Repair heuristics MUST be individually testable
- NFR-020: Parser MUST be configurable without code changes
- NFR-021: Error messages MUST be clear and actionable
- NFR-022: All public APIs MUST have XML documentation

---

## User Manual Documentation

### Overview

The Tool-Call Parsing and Retry-on-Invalid-JSON system enables Acode to reliably extract and validate tool calls from language model responses. This component handles the common case where models produce malformed JSON, automatically repairing errors or retrying with the model to obtain valid output.

**System Architecture Diagram:**

```
┌─────────────────────────────────────────────────────────────────────┐
│                         OLLAMA RESPONSE                              │
│  { "message": { "tool_calls": [...] } }                             │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      TOOL CALL PARSER                                │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────────────────┐   │
│  │  Extract    │──▶│  Parse JSON │──▶│  Validate Against       │   │
│  │  tool_calls │   │  Arguments  │   │  Tool Schema            │   │
│  └─────────────┘   └──────┬──────┘   └───────────┬─────────────┘   │
│                           │                       │                  │
│                    ┌──────▼──────┐                │                  │
│                    │ Parse Error │                │                  │
│                    └──────┬──────┘                │                  │
│                           │                       │                  │
│                    ┌──────▼──────┐         ┌──────▼──────┐          │
│                    │ JSON        │         │ Validation  │          │
│                    │ Repairer    │         │ Error       │          │
│                    └──────┬──────┘         └──────┬──────┘          │
│                           │                       │                  │
│                    ┌──────┴───────────────────────┘                  │
│                    │                                                 │
│                    ▼                                                 │
│            ┌───────────────┐                                        │
│            │ Retry Handler │───▶ Re-prompt Model                    │
│            └───────────────┘                                        │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      PARSED TOOL CALLS                               │
│  List<ToolCall> { Id, Name, Arguments (JsonElement) }               │
└─────────────────────────────────────────────────────────────────────┘
```

### How Tool Call Parsing Works

**Step 1: Extract Tool Calls from Response**
The parser reads the Ollama response JSON and extracts the `tool_calls` array from `message.tool_calls`. If no tool calls are present, an empty list is returned.

**Step 2: Parse JSON Arguments**
For each tool call, the parser extracts the function name and arguments. Arguments may be a JSON string or a pre-parsed JsonElement (in streaming mode).

**Step 3: Attempt Repair (If Parsing Fails)**
If JSON parsing fails, the JsonRepairer attempts automatic fixes:
- Remove trailing commas: `{"a": 1,}` → `{"a": 1}`
- Add missing braces: `{"a": 1` → `{"a": 1}`
- Fix single quotes: `{'a': 1}` → `{"a": 1}`
- Quote unquoted keys: `{a: 1}` → `{"a": 1}`
- Close truncated strings: `{"a": "hel` → `{"a": "hel"}`

**Step 4: Validate Against Schema**
Syntactically valid JSON is validated against the tool's JSON Schema from the Tool Schema Registry:
- Required properties must be present
- Property types must match (string, number, boolean, etc.)
- String patterns and lengths must conform
- No extra properties in strict mode

**Step 5: Retry with Model (If Validation Fails)**
If validation fails (or repair couldn't fix parse errors), the system re-prompts the model with error context, asking for corrected output.

**Step 6: Return Parsed Tool Calls**
Successfully parsed and validated tool calls are returned as a list of ToolCall objects ready for execution.

### Configuration Reference

Configure tool call parsing in `.agent/config.yml`:

```yaml
model:
  providers:
    ollama:
      tool_call:
        # ═══════════════════════════════════════════════════════════
        # RETRY SETTINGS
        # ═══════════════════════════════════════════════════════════
        
        # Maximum retry attempts for invalid JSON (including initial attempt)
        # Range: 1-10, Default: 3
        max_retries: 3
        
        # Delay between retry attempts (milliseconds)
        # Uses exponential backoff: delay * 2^attempt
        # Default: 100 (so: 100ms, 200ms, 400ms)
        retry_delay_ms: 100
        
        # ═══════════════════════════════════════════════════════════
        # AUTO-REPAIR SETTINGS
        # ═══════════════════════════════════════════════════════════
        
        # Enable automatic JSON repair before retry
        # When true, common errors are fixed without model round-trip
        # Default: true
        enable_auto_repair: true
        
        # Timeout for repair attempts (milliseconds)
        # Prevents hanging on pathological JSON
        # Default: 100
        repair_timeout_ms: 100
        
        # ═══════════════════════════════════════════════════════════
        # VALIDATION SETTINGS
        # ═══════════════════════════════════════════════════════════
        
        # Enforce strict schema validation (no extra properties allowed)
        # Default: true
        strict_validation: true
        
        # Maximum JSON nesting depth
        # Prevents stack overflow on deeply nested structures
        # Default: 64
        max_nesting_depth: 64
        
        # Maximum argument size in bytes
        # Prevents memory exhaustion on large arguments
        # Default: 1048576 (1 MB)
        max_argument_size: 1048576
        
        # ═══════════════════════════════════════════════════════════
        # LOGGING SETTINGS
        # ═══════════════════════════════════════════════════════════
        
        # Log repair details at DEBUG level
        # Default: true
        log_repair_details: true
        
        # Log full argument JSON at DEBUG level
        # WARNING: May expose sensitive data in logs
        # Default: false
        log_argument_values: false
```

### JSON Repair Capabilities

The auto-repair system can fix these common errors:

```
┌───────────────────────────────────────────────────────────────────┐
│                    JSON REPAIR CAPABILITIES                        │
├───────────────────┬──────────────────────┬────────────────────────┤
│  Error Type       │  Before Repair       │  After Repair          │
├───────────────────┼──────────────────────┼────────────────────────┤
│  Trailing comma   │  {"a": 1, "b": 2,}   │  {"a": 1, "b": 2}      │
│  Missing }        │  {"path": "test"     │  {"path": "test"}      │
│  Missing ]        │  {"items": [1, 2, 3  │  {"items": [1, 2, 3]}  │
│  Single quotes    │  {'path': 'test'}    │  {"path": "test"}      │
│  Unquoted key     │  {path: "test"}      │  {"path": "test"}      │
│  Truncated string │  {"msg": "hel        │  {"msg": "hel"}        │
│  Unescaped quote  │  {"say": "say "hi""} │  {"say": "say \"hi\""} │
│  Multiple errors  │  {path: 'test',}     │  {"path": "test"}      │
└───────────────────┴──────────────────────┴────────────────────────┘
```

### Retry Prompt Template

When automatic repair fails, the model is prompted to correct its output:

```
═══════════════════════════════════════════════════════════════════════
RETRY PROMPT TEMPLATE
═══════════════════════════════════════════════════════════════════════

The tool call arguments you provided contain invalid JSON.

Error: {error_type} at position {error_position}
Details: {error_message}

Your previous output:
```json
{malformed_json}
```

Expected format for '{tool_name}' tool:
```json
{schema_example}
```

Please provide corrected JSON arguments for the '{tool_name}' tool.
Do not include any explanation, only the corrected JSON object.

═══════════════════════════════════════════════════════════════════════
```

### Handling Multiple Tool Calls

When the model requests multiple tools in a single response, the parser processes each independently:

```csharp
// Multiple tool calls in response
var result = await parser.ParseToolCallsAsync(response);

// Check for partial success
if (result.HasErrors)
{
    // Some tool calls failed
    foreach (var error in result.Errors)
    {
        logger.LogWarning("Tool call failed: {Tool} - {Error}", 
            error.ToolName, error.Message);
    }
}

// Process successful tool calls
foreach (var toolCall in result.ToolCalls)
{
    Console.WriteLine($"╔══════════════════════════════════════╗");
    Console.WriteLine($"║  Tool: {toolCall.Name,-29}║");
    Console.WriteLine($"║  ID:   {toolCall.Id,-29}║");
    Console.WriteLine($"╚══════════════════════════════════════╝");
    
    // Execute tool with validated arguments
    var execution = await executor.ExecuteAsync(toolCall);
}
```

### Streaming Tool Call Accumulation

Tool calls in streaming responses are accumulated fragment by fragment:

```csharp
// Create accumulator for streaming session
var accumulator = new StreamingToolCallAccumulator();

// Process stream chunks
await foreach (var delta in ollamaStream.ReadAllAsync())
{
    // Accumulate tool call fragments
    if (delta.ToolCallDelta is not null)
    {
        accumulator.Append(delta.ToolCallDelta);
        
        // Check if any tool calls are complete
        while (accumulator.HasCompletedToolCall())
        {
            var toolCall = accumulator.GetNextCompleted();
            
            // Parse and validate the completed tool call
            var parseResult = parser.ParseSingleToolCall(toolCall);
            
            if (parseResult.Success)
            {
                await executor.ExecuteAsync(parseResult.ToolCall!);
            }
        }
    }
    
    // Handle content delta
    if (!string.IsNullOrEmpty(delta.Content))
    {
        Console.Write(delta.Content);
    }
}

// Get any remaining tool calls at stream end
var remaining = accumulator.GetAllRemaining();
```

### CLI Commands

**Check tool parsing configuration:**
```bash
acode config show model.providers.ollama.tool_call
```

Output:
```
Tool Call Parsing Configuration:
  max_retries:        3
  enable_auto_repair: true
  repair_timeout_ms:  100
  strict_validation:  true
  max_nesting_depth:  64
  max_argument_size:  1048576 (1 MB)
```

**View tool parsing statistics:**
```bash
acode providers stats ollama --tool-calls
```

Output:
```
Tool Call Statistics (Last 24 Hours):
  Total Tool Calls:      1,247
  Parsed Successfully:   1,198 (96.1%)
  Auto-Repaired:           38 (3.0%)
  Retried Successfully:    11 (0.9%)
  Failed:                   0 (0.0%)

Repair Breakdown:
  Trailing comma:          24
  Missing brace:            8
  Single quotes:            4
  Unquoted keys:            2

Average Parse Time:      0.42ms
Average Repair Time:     2.31ms
Average Retry Time:   1,847ms (includes model round-trip)
```

**Debug a specific tool call:**
```bash
acode debug tool-call --correlation-id abc-123-def
```

Output:
```
Tool Call Debug: abc-123-def
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Timestamp:   2025-01-15T14:23:47.123Z
Tool Name:   write_file
Call ID:     call_7f8g9h0i

Parse Result: REPAIRED
Repair Applied: trailing_comma
Original JSON:
  {"path": "src/main.cs", "content": "...",}
                                         ^
                                    Error position

Repaired JSON:
  {"path": "src/main.cs", "content": "..."}

Validation: PASSED
Execution:  SUCCESS (247ms)
```

### Error Handling Examples

```csharp
try
{
    var result = await parser.ParseToolCallsAsync(response);
    
    // Check for complete success
    if (result.AllSucceeded)
    {
        foreach (var tc in result.ToolCalls)
        {
            await executor.ExecuteAsync(tc);
        }
    }
    else
    {
        // Handle partial success
        foreach (var tc in result.ToolCalls)
        {
            await executor.ExecuteAsync(tc);
        }
        
        // Log failures
        foreach (var err in result.Errors)
        {
            logger.LogWarning("Tool call parse error: {Code} - {Message}", 
                err.ErrorCode, err.Message);
        }
    }
}
catch (ToolCallParseException ex)
{
    // JSON syntax error that couldn't be repaired
    // Example: Binary data that isn't JSON at all
    logger.LogError(ex, 
        "Tool call JSON is fundamentally invalid: {Error}", 
        ex.Message);
    
    await userNotifier.NotifyAsync(
        "The AI generated an invalid response. Please try again.");
}
catch (ToolCallValidationException ex)
{
    // JSON is valid syntax but doesn't match expected schema
    // Example: Required field missing, wrong type
    logger.LogError(ex, 
        "Tool call arguments don't match schema: Path={Path}, Expected={Expected}, Actual={Actual}", 
        ex.ValidationPath, 
        ex.ExpectedType, 
        ex.ActualType);
    
    await userNotifier.NotifyAsync(
        $"The AI called {ex.ToolName} with incorrect parameters. Retrying...");
}
catch (ToolCallRetryExhaustedException ex)
{
    // Model couldn't produce valid JSON after max retries
    logger.LogError(ex, 
        "Failed after {Attempts} retry attempts. Last error: {Error}", 
        ex.AttemptCount,
        ex.LastError);
    
    await userNotifier.NotifyAsync(
        $"Unable to complete tool call after {ex.AttemptCount} attempts. " +
        "Please try rephrasing your request.");
}
```

### Logging Fields Reference

Tool call parsing logs include these structured fields:

```
┌────────────────────┬──────────┬────────────────────────────────────┐
│  Field             │  Type    │  Description                       │
├────────────────────┼──────────┼────────────────────────────────────┤
│  correlation_id    │  string  │  Request correlation ID            │
│  tool_name         │  string  │  Name of tool being parsed         │
│  tool_call_id      │  string  │  Unique ID of tool call            │
│  parse_result      │  enum    │  success | repaired | retry | fail │
│  repair_applied    │  array   │  List of repairs applied           │
│  retry_count       │  int     │  Number of retry attempts          │
│  validation_errors │  array   │  Schema validation errors          │
│  parse_time_ms     │  double  │  Time spent parsing (ms)           │
│  repair_time_ms    │  double  │  Time spent repairing (ms)         │
│  retry_time_ms     │  double  │  Time spent in retries (ms)        │
│  total_tokens      │  int     │  Tokens used including retries     │
│  error_code        │  string  │  Error code if failed              │
│  error_position    │  int     │  Position of parse error           │
└────────────────────┴──────────┴────────────────────────────────────┘
```

---

## Troubleshooting

### Problem 1: Frequent JSON Parse Errors

**Symptoms:**
- Many tool calls require repair or retry
- High retry_count in logs (2+ on average)
- Increased latency due to retries
- Token usage higher than expected

**Causes:**
1. Model may not support native function calling
2. System prompt missing JSON format examples
3. Tool descriptions unclear or ambiguous
4. Model temperature too high causing random output

**Solutions:**
1. **Use function-calling model:** Ensure Ollama is running a model that supports native function calling (e.g., llama3.1, codellama, mixtral with function calling)
   ```bash
   ollama list
   # Verify your model supports tool calling
   ```

2. **Add JSON examples to system prompt:**
   ```
   When using tools, always format arguments as valid JSON.
   Example: {"path": "file.txt", "content": "Hello"}
   Never use trailing commas. Always use double quotes.
   ```

3. **Lower temperature for tool calls:**
   ```yaml
   model:
     providers:
       ollama:
         options:
           temperature: 0.1  # Lower = more deterministic JSON
   ```

4. **Enable strict JSON mode if supported:**
   ```yaml
   model:
     providers:
       ollama:
         options:
           format: json
   ```

### Problem 2: Schema Validation Failures

**Symptoms:**
- JSON parses successfully but fails validation
- ToolCallValidationException in logs
- Errors like "Required property 'path' missing"
- Type mismatch errors

**Causes:**
1. Tool description doesn't clearly specify required fields
2. Model confusing similar parameters
3. Schema too strict for model's output style
4. Model using wrong types (string "123" instead of number 123)

**Solutions:**
1. **Review tool definition clarity:**
   ```json
   {
     "name": "read_file",
     "description": "Read file contents. REQUIRED: path (string).",
     "parameters": {
       "properties": {
         "path": {
           "type": "string",
           "description": "REQUIRED. Path to file to read."
         }
       },
       "required": ["path"]
     }
   }
   ```

2. **Add parameter examples in description:**
   ```json
   "description": "Path to read. Examples: 'src/main.cs', 'README.md'"
   ```

3. **Consider relaxing strict mode:**
   ```yaml
   tool_call:
     strict_validation: false  # Allow extra properties
   ```

4. **Add type coercion (if acceptable):**
   ```csharp
   // In custom validator, coerce string numbers to integers
   if (expected is int && actual is string s && int.TryParse(s, out var i))
       return i; // Allow string-to-int coercion
   ```

### Problem 3: Retry Loop Exhaustion

**Symptoms:**
- ToolCallRetryExhaustedException after max attempts
- Same error repeated in retry logs
- Model not improving with retry prompts
- High token usage from failed retries

**Causes:**
1. Model fundamentally can't produce required format
2. Tool parameters too complex for model
3. Retry prompt not helpful enough
4. Token limit causing truncation on every attempt

**Solutions:**
1. **Increase max_retries cautiously:**
   ```yaml
   tool_call:
     max_retries: 5  # Try more times (but costs more tokens)
   ```

2. **Simplify tool parameters:**
   - Break complex tools into simpler ones
   - Reduce number of required parameters
   - Use sensible defaults

3. **Improve retry prompt template:**
   ```yaml
   tool_call:
     retry_prompt_template: |
       ERROR: Your JSON was invalid.
       Specific error: {error_message}
       
       Here is exactly what I need:
       {"path": "string value here"}
       
       Try again with ONLY the JSON object, no explanation.
   ```

4. **Use model with better JSON capabilities:**
   - GPT-4 class models have better JSON adherence
   - Consider switching models for complex tool calls

### Problem 4: Streaming Accumulation Errors

**Symptoms:**
- Tool calls missing from streaming responses
- Partial tool names or arguments
- "Streaming accumulation error" in logs
- Tool calls appearing out of order

**Causes:**
1. Network dropping chunks
2. Stream ending before tool call complete
3. Multiple tool calls interleaved incorrectly
4. Buffer overflow on large arguments

**Solutions:**
1. **Check network stability:**
   ```bash
   # Test Ollama streaming directly
   curl -N http://localhost:11434/api/chat \
     -d '{"model":"llama3.1","stream":true,"messages":[{"role":"user","content":"test"}]}'
   ```

2. **Increase buffer sizes:**
   ```yaml
   tool_call:
     streaming:
       buffer_size: 65536  # 64 KB default
       max_concurrent_calls: 10
   ```

3. **Enable streaming debug logging:**
   ```yaml
   logging:
     levels:
       StreamingToolCallAccumulator: Debug
   ```

4. **Consider disabling streaming for reliability:**
   ```yaml
   model:
     providers:
       ollama:
         streaming: false  # Disable streaming for tool calls
   ```

### Problem 5: Performance Degradation

**Symptoms:**
- Tool call parsing taking > 10ms
- Repair timeout (100ms) being hit
- High CPU usage during parsing
- Memory spikes on large arguments

**Causes:**
1. Pathological JSON patterns (deep nesting, huge arrays)
2. Memory allocation pressure from large arguments
3. Regex backtracking in repair heuristics (shouldn't happen)
4. Synchronous blocking on async operations

**Solutions:**
1. **Enforce size limits:**
   ```yaml
   tool_call:
     max_argument_size: 524288  # Reduce to 512 KB
     max_nesting_depth: 32      # Reduce from 64
   ```

2. **Monitor and profile:**
   ```bash
   acode providers stats ollama --tool-calls --verbose
   # Shows percentile latencies (p50, p95, p99)
   ```

3. **Tune repair timeout:**
   ```yaml
   tool_call:
     repair_timeout_ms: 50  # Fail faster on complex repairs
   ```

4. **Review for blocking calls:**
   ```csharp
   // WRONG: Blocking on async
   var result = parser.ParseAsync(response).Result;
   
   // RIGHT: Await properly
   var result = await parser.ParseAsync(response);
   ```

---
3. Enable strict JSON mode if supported

#### Schema Validation Failures

**Symptom**: JSON parses but fails validation

**Cause**: Model output doesn't match expected schema

**Solution**:
1. Review tool definition for clarity
2. Add parameter descriptions
3. Provide examples in tool description

#### Retry Loop

**Symptom**: All retry attempts exhausted

**Cause**: Model cannot produce valid JSON for tool

**Solution**:
1. Increase max_retries
2. Simplify tool parameters
3. Use model with better JSON capabilities

---

## Acceptance Criteria

### Tool Call Extraction

- [ ] AC-001: ToolCallParser defined in Infrastructure
- [ ] AC-002: Extracts tool_calls from response
- [ ] AC-003: Handles missing tool_calls (empty list)
- [ ] AC-004: Extracts function.name
- [ ] AC-005: Extracts function.arguments
- [ ] AC-006: Generates ID if missing
- [ ] AC-007: Handles multiple tool calls
- [ ] AC-008: Preserves ordering
- [ ] AC-009: Maps to ToolCall type

### Ollama Format Handling

- [ ] AC-010: Handles function object format
- [ ] AC-011: Validates function.name
- [ ] AC-012: Handles arguments as string
- [ ] AC-013: Handles arguments as object
- [ ] AC-014: Generates call ID with prefix
- [ ] AC-015: Rejects empty function name
- [ ] AC-016: Rejects unknown tools

### JSON Argument Parsing

- [ ] AC-017: Parses JSON string to JsonElement
- [ ] AC-018: Validates is object type
- [ ] AC-019: Handles empty object
- [ ] AC-020: Detects parse errors
- [ ] AC-021: Includes error position
- [ ] AC-022: Handles JsonElement input

### JSON Repair

- [ ] AC-023: JsonRepairer defined
- [ ] AC-024: Fixes trailing commas
- [ ] AC-025: Fixes missing braces
- [ ] AC-026: Fixes missing brackets
- [ ] AC-027: Fixes unescaped quotes
- [ ] AC-028: Fixes truncated strings
- [ ] AC-029: Fixes unquoted keys
- [ ] AC-030: Fixes single quotes
- [ ] AC-031: Doesn't modify valid JSON
- [ ] AC-032: Returns RepairResult
- [ ] AC-033: Logs repairs
- [ ] AC-034: Respects timeout

### Repair Result

- [ ] AC-035: Indicates attempt status
- [ ] AC-036: Includes original on failure
- [ ] AC-037: Includes repaired on success
- [ ] AC-038: Lists repairs applied
- [ ] AC-039: Includes error on failure

### Retry Mechanism

- [ ] AC-040: Retries on parse failure
- [ ] AC-041: Retries on validation failure
- [ ] AC-042: Respects max retry count
- [ ] AC-043: Constructs retry prompt
- [ ] AC-044: Includes malformed JSON
- [ ] AC-045: Includes error message
- [ ] AC-046: Includes format hint
- [ ] AC-047: Sends new request
- [ ] AC-048: Extracts new tool call
- [ ] AC-049: Validates new tool call
- [ ] AC-050: Tracks token usage
- [ ] AC-051: Logs each attempt

### Retry Configuration

- [ ] AC-052: max_retries configurable
- [ ] AC-053: enable_auto_repair configurable
- [ ] AC-054: repair_timeout_ms configurable
- [ ] AC-055: Template configurable
- [ ] AC-056: Loads from config.yml

### Schema Validation

- [ ] AC-057: Validates against schema
- [ ] AC-058: Uses IToolSchemaRegistry
- [ ] AC-059: Distinguishes error types
- [ ] AC-060: Includes invalid path
- [ ] AC-061: Includes type mismatch
- [ ] AC-062: Includes missing fields
- [ ] AC-063: Enforces strict mode

### Streaming

- [ ] AC-064: Accumulates fragments
- [ ] AC-065: Handles partial name
- [ ] AC-066: Handles partial args
- [ ] AC-067: Buffers until complete
- [ ] AC-068: Detects completion
- [ ] AC-069: Handles concurrent calls
- [ ] AC-070: Uses index correlation

### Error Types

- [ ] AC-071: ToolCallParseException defined
- [ ] AC-072: ToolCallValidationException defined
- [ ] AC-073: ToolCallRetryExhaustedException defined
- [ ] AC-074: Includes response data
- [ ] AC-075: Includes attempt count
- [ ] AC-076: Includes correlation ID

### Performance

- [ ] AC-077: Extraction < 1ms
- [ ] AC-078: Repair timeout enforced
- [ ] AC-079: Validation < 5ms
- [ ] AC-080: Memory < 1MB per call

### Security

- [ ] AC-081: No args at INFO level
- [ ] AC-082: No code execution in repair
- [ ] AC-083: Injection prevention
- [ ] AC-084: Memory bounds enforced

### Documentation

- [ ] AC-085: XML docs complete
- [ ] AC-086: Config examples
- [ ] AC-087: Troubleshooting guide

---

## Testing Requirements

### Unit Tests

#### File: Tests/Unit/Infrastructure/Ollama/ToolCall/ToolCallParserTests.cs

```csharp
using System.Text.Json;
using Acode.Domain.Inference;
using Acode.Infrastructure.Ollama.ToolCall;
using Acode.Infrastructure.Ollama.ToolCall.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Acode.Infrastructure.Tests.Ollama.ToolCall;

public class ToolCallParserTests
{
    private readonly ToolCallParser _sut;
    private readonly IToolSchemaRegistry _schemaRegistry;
    private readonly JsonRepairer _repairer;
    private readonly ILogger<ToolCallParser> _logger;

    public ToolCallParserTests()
    {
        _schemaRegistry = Substitute.For<IToolSchemaRegistry>();
        _repairer = new JsonRepairer();
        _logger = Substitute.For<ILogger<ToolCallParser>>();
        _sut = new ToolCallParser(_repairer, _schemaRegistry, _logger);
        
        // Setup default schema for read_file tool
        _schemaRegistry.TryGetSchema("read_file", out Arg.Any<ToolDefinition?>())
            .Returns(x => {
                x[1] = CreateReadFileSchema();
                return true;
            });
    }

    [Fact]
    public void Should_Extract_Single_ToolCall()
    {
        // Arrange
        var response = CreateOllamaResponse(new[]
        {
            new OllamaToolCall
            {
                Id = "call_abc123",
                Function = new OllamaFunction
                {
                    Name = "read_file",
                    Arguments = """{"path": "README.md"}"""
                }
            }
        });

        // Act
        var result = _sut.Parse(response);

        // Assert
        result.ToolCalls.Should().HaveCount(1);
        result.ToolCalls[0].Name.Should().Be("read_file");
        result.ToolCalls[0].Id.Should().Be("call_abc123");
        result.ToolCalls[0].Arguments.GetProperty("path").GetString()
            .Should().Be("README.md");
    }

    [Fact]
    public void Should_Extract_Multiple_ToolCalls()
    {
        // Arrange
        var response = CreateOllamaResponse(new[]
        {
            new OllamaToolCall
            {
                Id = "call_001",
                Function = new OllamaFunction
                {
                    Name = "read_file",
                    Arguments = """{"path": "file1.cs"}"""
                }
            },
            new OllamaToolCall
            {
                Id = "call_002",
                Function = new OllamaFunction
                {
                    Name = "read_file",
                    Arguments = """{"path": "file2.cs"}"""
                }
            },
            new OllamaToolCall
            {
                Id = "call_003",
                Function = new OllamaFunction
                {
                    Name = "read_file",
                    Arguments = """{"path": "file3.cs"}"""
                }
            }
        });

        // Act
        var result = _sut.Parse(response);

        // Assert
        result.ToolCalls.Should().HaveCount(3);
        result.ToolCalls[0].Arguments.GetProperty("path").GetString()
            .Should().Be("file1.cs");
        result.ToolCalls[1].Arguments.GetProperty("path").GetString()
            .Should().Be("file2.cs");
        result.ToolCalls[2].Arguments.GetProperty("path").GetString()
            .Should().Be("file3.cs");
    }

    [Fact]
    public void Should_Handle_Missing_ToolCalls()
    {
        // Arrange
        var response = new OllamaResponse
        {
            Message = new OllamaMessage
            {
                Role = "assistant",
                Content = "No tools needed for this response."
            }
        };

        // Act
        var result = _sut.Parse(response);

        // Assert
        result.ToolCalls.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
        result.AllSucceeded.Should().BeTrue();
    }

    [Fact]
    public void Should_Generate_Id_When_Missing()
    {
        // Arrange
        var response = CreateOllamaResponse(new[]
        {
            new OllamaToolCall
            {
                Id = null, // Missing ID
                Function = new OllamaFunction
                {
                    Name = "read_file",
                    Arguments = """{"path": "test.txt"}"""
                }
            }
        });

        // Act
        var result = _sut.Parse(response);

        // Assert
        result.ToolCalls.Should().HaveCount(1);
        result.ToolCalls[0].Id.Should().StartWith("call_");
        result.ToolCalls[0].Id.Length.Should().BeGreaterThan(10);
    }

    [Fact]
    public void Should_Validate_FunctionName_Is_Required()
    {
        // Arrange
        var response = CreateOllamaResponse(new[]
        {
            new OllamaToolCall
            {
                Id = "call_001",
                Function = new OllamaFunction
                {
                    Name = "", // Empty name
                    Arguments = """{"path": "test.txt"}"""
                }
            }
        });

        // Act
        var result = _sut.Parse(response);

        // Assert
        result.ToolCalls.Should().BeEmpty();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].ErrorCode.Should().Be("ACODE-TLP-001");
        result.Errors[0].Message.Should().Contain("Function name is required");
    }

    [Fact]
    public void Should_Reject_Unknown_Tool()
    {
        // Arrange
        _schemaRegistry.TryGetSchema("unknown_tool", out Arg.Any<ToolDefinition?>())
            .Returns(false);
            
        var response = CreateOllamaResponse(new[]
        {
            new OllamaToolCall
            {
                Id = "call_001",
                Function = new OllamaFunction
                {
                    Name = "unknown_tool",
                    Arguments = """{"param": "value"}"""
                }
            }
        });

        // Act
        var result = _sut.Parse(response);

        // Assert
        result.ToolCalls.Should().BeEmpty();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].ErrorCode.Should().Be("ACODE-TLP-005");
        result.Errors[0].Message.Should().Contain("Unknown tool name");
    }

    [Fact]
    public void Should_Preserve_Tool_Call_Ordering()
    {
        // Arrange
        var response = CreateOllamaResponse(new[]
        {
            new OllamaToolCall { Id = "first", Function = new OllamaFunction { Name = "read_file", Arguments = """{"path": "1.txt"}""" } },
            new OllamaToolCall { Id = "second", Function = new OllamaFunction { Name = "read_file", Arguments = """{"path": "2.txt"}""" } },
            new OllamaToolCall { Id = "third", Function = new OllamaFunction { Name = "read_file", Arguments = """{"path": "3.txt"}""" } }
        });

        // Act
        var result = _sut.Parse(response);

        // Assert
        result.ToolCalls[0].Id.Should().Be("first");
        result.ToolCalls[1].Id.Should().Be("second");
        result.ToolCalls[2].Id.Should().Be("third");
    }

    private static OllamaResponse CreateOllamaResponse(OllamaToolCall[] toolCalls)
    {
        return new OllamaResponse
        {
            Message = new OllamaMessage
            {
                Role = "assistant",
                Content = "",
                ToolCalls = toolCalls.ToList()
            }
        };
    }

    private static ToolDefinition CreateReadFileSchema()
    {
        return new ToolDefinition
        {
            Name = "read_file",
            Parameters = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "path": { "type": "string" }
                },
                "required": ["path"]
            }
            """).RootElement
        };
    }
}
```

#### File: Tests/Unit/Infrastructure/Ollama/ToolCall/JsonRepairerTests.cs

```csharp
using Acode.Infrastructure.Ollama.ToolCall;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Ollama.ToolCall;

public class JsonRepairerTests
{
    private readonly JsonRepairer _sut = new();

    [Fact]
    public void Should_Fix_TrailingComma_InObject()
    {
        // Arrange
        var malformedJson = """{"path": "test.txt",}""";

        // Act
        var result = _sut.TryRepair(malformedJson);

        // Assert
        result.Success.Should().BeTrue();
        result.RepairedJson.Should().Be("""{"path": "test.txt"}""");
        result.Repairs.Should().Contain("trailing_comma");
    }

    [Fact]
    public void Should_Fix_TrailingComma_InArray()
    {
        // Arrange
        var malformedJson = """{"items": [1, 2, 3,]}""";

        // Act
        var result = _sut.TryRepair(malformedJson);

        // Assert
        result.Success.Should().BeTrue();
        result.RepairedJson.Should().Be("""{"items": [1, 2, 3]}""");
        result.Repairs.Should().Contain("trailing_comma");
    }

    [Fact]
    public void Should_Fix_MissingBrace_Single()
    {
        // Arrange
        var malformedJson = """{"path": "test.txt"""";

        // Act
        var result = _sut.TryRepair(malformedJson);

        // Assert
        result.Success.Should().BeTrue();
        result.RepairedJson.Should().Be("""{"path": "test.txt"}""");
        result.Repairs.Should().Contain("missing_closing_brace");
    }

    [Fact]
    public void Should_Fix_MissingBrace_Multiple()
    {
        // Arrange
        var malformedJson = """{"outer": {"inner": {"deep": "value"}""";

        // Act
        var result = _sut.TryRepair(malformedJson);

        // Assert
        result.Success.Should().BeTrue();
        result.RepairedJson.Should().Be("""{"outer": {"inner": {"deep": "value"}}}""");
        result.Repairs.Should().Contain("missing_closing_brace");
    }

    [Fact]
    public void Should_Fix_MissingBracket()
    {
        // Arrange
        var malformedJson = """{"items": [1, 2, 3}""";

        // Act
        var result = _sut.TryRepair(malformedJson);

        // Assert
        result.Success.Should().BeTrue();
        result.RepairedJson.Should().Be("""{"items": [1, 2, 3]}""");
        result.Repairs.Should().Contain("missing_closing_bracket");
    }

    [Fact]
    public void Should_Fix_UnescapedQuotes_InString()
    {
        // Arrange
        var malformedJson = """{"message": "He said "hello" to me"}""";

        // Act
        var result = _sut.TryRepair(malformedJson);

        // Assert
        result.Success.Should().BeTrue();
        result.RepairedJson.Should().Be("""{"message": "He said \"hello\" to me"}""");
        result.Repairs.Should().Contain("unescaped_quotes");
    }

    [Fact]
    public void Should_Fix_TruncatedString()
    {
        // Arrange
        var malformedJson = """{"content": "Hello, this is a test""";

        // Act
        var result = _sut.TryRepair(malformedJson);

        // Assert
        result.Success.Should().BeTrue();
        result.RepairedJson.Should().Contain(""""}""");
        result.Repairs.Should().Contain("truncated_string");
    }

    [Fact]
    public void Should_Fix_UnquotedKeys()
    {
        // Arrange
        var malformedJson = """{path: "test.txt", content: "hello"}""";

        // Act
        var result = _sut.TryRepair(malformedJson);

        // Assert
        result.Success.Should().BeTrue();
        result.RepairedJson.Should().Be("""{"path": "test.txt", "content": "hello"}""");
        result.Repairs.Should().Contain("unquoted_key");
    }

    [Fact]
    public void Should_Fix_SingleQuotes()
    {
        // Arrange
        var malformedJson = """{'path': 'test.txt'}""";

        // Act
        var result = _sut.TryRepair(malformedJson);

        // Assert
        result.Success.Should().BeTrue();
        result.RepairedJson.Should().Be("""{"path": "test.txt"}""");
        result.Repairs.Should().Contain("single_quotes");
    }

    [Fact]
    public void Should_Not_Modify_ValidJson()
    {
        // Arrange
        var validJson = """{"path": "test.txt", "content": "hello world"}""";

        // Act
        var result = _sut.TryRepair(validJson);

        // Assert
        result.Success.Should().BeTrue();
        result.RepairedJson.Should().Be(validJson);
        result.Repairs.Should().BeEmpty();
        result.WasRepaired.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Timeout_OnComplexInput()
    {
        // Arrange
        var deeplyNested = new string('{', 10000) + new string('}', 9999); // Missing one brace
        var repairer = new JsonRepairer(timeoutMs: 10); // Very short timeout

        // Act
        var result = repairer.TryRepair(deeplyNested);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("timeout");
    }

    [Fact]
    public void Should_Fix_MultipleErrors_Combined()
    {
        // Arrange - Multiple errors: single quotes, trailing comma, unquoted key
        var malformedJson = """{path: 'test.txt',}""";

        // Act
        var result = _sut.TryRepair(malformedJson);

        // Assert
        result.Success.Should().BeTrue();
        result.RepairedJson.Should().Be("""{"path": "test.txt"}""");
        result.Repairs.Should().HaveCountGreaterOrEqualTo(2);
    }
}
```

#### File: Tests/Unit/Infrastructure/Ollama/ToolCall/RetryMechanismTests.cs

```csharp
using System.Text.Json;
using Acode.Domain.Inference;
using Acode.Infrastructure.Ollama;
using Acode.Infrastructure.Ollama.ToolCall;
using Acode.Infrastructure.Ollama.ToolCall.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Acode.Infrastructure.Tests.Ollama.ToolCall;

public class RetryMechanismTests
{
    private readonly ToolCallRetryHandler _sut;
    private readonly IOllamaClient _client;
    private readonly ILogger<ToolCallRetryHandler> _logger;
    private readonly RetryConfig _config;

    public RetryMechanismTests()
    {
        _client = Substitute.For<IOllamaClient>();
        _logger = Substitute.For<ILogger<ToolCallRetryHandler>>();
        _config = new RetryConfig
        {
            MaxRetries = 3,
            EnableAutoRepair = true,
            RetryDelayMs = 10
        };
        _sut = new ToolCallRetryHandler(_client, _config, _logger);
    }

    [Fact]
    public async Task Should_Retry_OnParseFailure()
    {
        // Arrange
        var malformedJson = "not json at all";
        var validJson = """{"path": "test.txt"}""";
        
        _client.SendAsync(Arg.Any<OllamaRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateResponseWithArgs(validJson));

        // Act
        var result = await _sut.RetryToolCallAsync(
            toolName: "read_file",
            malformedArgs: malformedJson,
            parseError: "Invalid JSON at position 0",
            correlationId: "test-001",
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Arguments.GetProperty("path").GetString().Should().Be("test.txt");
        await _client.Received(1).SendAsync(Arg.Any<OllamaRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Retry_OnValidationFailure()
    {
        // Arrange
        var invalidArgs = """{"wrong_field": "value"}""";
        var validArgs = """{"path": "test.txt"}""";
        
        _client.SendAsync(Arg.Any<OllamaRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateResponseWithArgs(validArgs));

        // Act
        var result = await _sut.RetryToolCallAsync(
            toolName: "read_file",
            malformedArgs: invalidArgs,
            parseError: "Missing required property: path",
            correlationId: "test-002",
            CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Arguments.GetProperty("path").GetString().Should().Be("test.txt");
    }

    [Fact]
    public async Task Should_Respect_MaxRetries()
    {
        // Arrange - Model always returns invalid JSON
        _client.SendAsync(Arg.Any<OllamaRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateResponseWithArgs("still invalid"));

        // Act
        var act = () => _sut.RetryToolCallAsync(
            toolName: "read_file",
            malformedArgs: "invalid",
            parseError: "Parse error",
            correlationId: "test-003",
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ToolCallRetryExhaustedException>()
            .Where(ex => ex.AttemptCount == 3);
        
        await _client.Received(3).SendAsync(Arg.Any<OllamaRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Include_ErrorContext_InRetryPrompt()
    {
        // Arrange
        OllamaRequest? capturedRequest = null;
        _client.SendAsync(Arg.Do<OllamaRequest>(r => capturedRequest = r), Arg.Any<CancellationToken>())
            .Returns(CreateResponseWithArgs("""{"path": "test.txt"}"""));

        // Act
        await _sut.RetryToolCallAsync(
            toolName: "read_file",
            malformedArgs: """{"path: "missing quote}""",
            parseError: "Unexpected token at position 7",
            correlationId: "test-004",
            CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        var lastMessage = capturedRequest!.Messages.Last();
        lastMessage.Content.Should().Contain("Unexpected token at position 7");
        lastMessage.Content.Should().Contain("read_file");
        lastMessage.Content.Should().Contain("""{"path: "missing quote}""");
    }

    [Fact]
    public async Task Should_Track_TokenUsage_Across_Retries()
    {
        // Arrange
        var attempt = 0;
        _client.SendAsync(Arg.Any<OllamaRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => 
            {
                attempt++;
                return CreateResponseWithArgs(
                    attempt < 3 ? "invalid" : """{"path": "test.txt"}""",
                    promptTokens: 100 * attempt,
                    completionTokens: 50 * attempt);
            });

        // Act
        var result = await _sut.RetryToolCallAsync(
            toolName: "read_file",
            malformedArgs: "invalid",
            parseError: "Parse error",
            correlationId: "test-005",
            CancellationToken.None);

        // Assert
        result.TotalTokensUsed.Should().Be(100 + 200 + 300 + 50 + 100 + 150); // All retries summed
        result.RetryCount.Should().Be(3);
    }

    [Fact]
    public async Task Should_Log_Each_Attempt()
    {
        // Arrange
        _client.SendAsync(Arg.Any<OllamaRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateResponseWithArgs("""{"path": "test.txt"}"""));

        // Act
        await _sut.RetryToolCallAsync(
            toolName: "read_file",
            malformedArgs: "invalid",
            parseError: "Parse error",
            correlationId: "test-006",
            CancellationToken.None);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Retry attempt 1")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    private static OllamaResponse CreateResponseWithArgs(string args, int promptTokens = 100, int completionTokens = 50)
    {
        return new OllamaResponse
        {
            Message = new OllamaMessage
            {
                Role = "assistant",
                ToolCalls = new List<OllamaToolCall>
                {
                    new()
                    {
                        Id = "call_retry",
                        Function = new OllamaFunction
                        {
                            Name = "read_file",
                            Arguments = args
                        }
                    }
                }
            },
            PromptEvalCount = promptTokens,
            EvalCount = completionTokens
        };
    }
}
```

#### File: Tests/Unit/Infrastructure/Ollama/ToolCall/SchemaValidationTests.cs

```csharp
using System.Text.Json;
using Acode.Infrastructure.Ollama.ToolCall;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Ollama.ToolCall;

public class SchemaValidationTests
{
    private readonly SchemaValidator _sut = new();

    [Fact]
    public void Should_Validate_RequiredFields_Present()
    {
        // Arrange
        var schema = CreateSchema("""
        {
            "type": "object",
            "properties": {
                "path": { "type": "string" },
                "content": { "type": "string" }
            },
            "required": ["path", "content"]
        }
        """);
        
        var args = JsonDocument.Parse("""{"path": "test.txt", "content": "hello"}""").RootElement;

        // Act
        var result = _sut.Validate(args, schema);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Should_Fail_When_RequiredField_Missing()
    {
        // Arrange
        var schema = CreateSchema("""
        {
            "type": "object",
            "properties": {
                "path": { "type": "string" },
                "content": { "type": "string" }
            },
            "required": ["path", "content"]
        }
        """);
        
        var args = JsonDocument.Parse("""{"path": "test.txt"}""").RootElement; // Missing content

        // Act
        var result = _sut.Validate(args, schema);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Path == "content" && e.Message.Contains("required"));
    }

    [Fact]
    public void Should_Validate_FieldTypes_Correctly()
    {
        // Arrange
        var schema = CreateSchema("""
        {
            "type": "object",
            "properties": {
                "count": { "type": "integer" },
                "enabled": { "type": "boolean" }
            }
        }
        """);
        
        var args = JsonDocument.Parse("""{"count": 42, "enabled": true}""").RootElement;

        // Act
        var result = _sut.Validate(args, schema);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_On_Type_Mismatch()
    {
        // Arrange
        var schema = CreateSchema("""
        {
            "type": "object",
            "properties": {
                "count": { "type": "integer" }
            }
        }
        """);
        
        var args = JsonDocument.Parse("""{"count": "not a number"}""").RootElement;

        // Act
        var result = _sut.Validate(args, schema);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.Path == "count" && 
            e.ExpectedType == "integer" && 
            e.ActualType == "string");
    }

    [Fact]
    public void Should_Reject_ExtraFields_InStrictMode()
    {
        // Arrange
        var schema = CreateSchema("""
        {
            "type": "object",
            "properties": {
                "path": { "type": "string" }
            },
            "additionalProperties": false
        }
        """);
        
        var args = JsonDocument.Parse("""{"path": "test.txt", "extra": "not allowed"}""").RootElement;

        // Act
        var result = _sut.Validate(args, schema, strictMode: true);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Message.Contains("additional property"));
    }

    [Fact]
    public void Should_Report_ValidationPath_ForNestedErrors()
    {
        // Arrange
        var schema = CreateSchema("""
        {
            "type": "object",
            "properties": {
                "options": {
                    "type": "object",
                    "properties": {
                        "timeout": { "type": "integer" }
                    }
                }
            }
        }
        """);
        
        var args = JsonDocument.Parse("""{"options": {"timeout": "invalid"}}""").RootElement;

        // Act
        var result = _sut.Validate(args, schema);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Path == "options.timeout");
    }

    private static JsonElement CreateSchema(string schemaJson)
    {
        return JsonDocument.Parse(schemaJson).RootElement;
    }
}
```

#### File: Tests/Unit/Infrastructure/Ollama/ToolCall/StreamingAccumulatorTests.cs

```csharp
using Acode.Infrastructure.Ollama.ToolCall;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Ollama.ToolCall;

public class StreamingAccumulatorTests
{
    private readonly StreamingToolCallAccumulator _sut = new();

    [Fact]
    public void Should_Accumulate_Fragments_InOrder()
    {
        // Arrange & Act
        _sut.Append(new ToolCallDelta { Index = 0, FunctionName = "read" });
        _sut.Append(new ToolCallDelta { Index = 0, FunctionName = "_file" });
        _sut.Append(new ToolCallDelta { Index = 0, Arguments = """{"pa""" });
        _sut.Append(new ToolCallDelta { Index = 0, Arguments = """th": """ });
        _sut.Append(new ToolCallDelta { Index = 0, Arguments = """"test.txt"}""" });
        _sut.Append(new ToolCallDelta { Index = 0, IsComplete = true });

        // Assert
        _sut.HasCompletedToolCall().Should().BeTrue();
        var toolCall = _sut.GetNextCompleted();
        toolCall.Function.Name.Should().Be("read_file");
        toolCall.Function.Arguments.Should().Be("""{"path": "test.txt"}""");
    }

    [Fact]
    public void Should_Handle_PartialName()
    {
        // Arrange & Act
        _sut.Append(new ToolCallDelta { Index = 0, FunctionName = "exec" });
        _sut.Append(new ToolCallDelta { Index = 0, FunctionName = "ute_" });
        _sut.Append(new ToolCallDelta { Index = 0, FunctionName = "command" });
        _sut.Append(new ToolCallDelta { Index = 0, Arguments = """{"cmd": "ls"}""", IsComplete = true });

        // Assert
        var toolCall = _sut.GetNextCompleted();
        toolCall.Function.Name.Should().Be("execute_command");
    }

    [Fact]
    public void Should_Handle_PartialArgs()
    {
        // Arrange & Act
        _sut.Append(new ToolCallDelta { Index = 0, FunctionName = "read_file" });
        _sut.Append(new ToolCallDelta { Index = 0, Arguments = "{" });
        _sut.Append(new ToolCallDelta { Index = 0, Arguments = "\"path\"" });
        _sut.Append(new ToolCallDelta { Index = 0, Arguments = ": " });
        _sut.Append(new ToolCallDelta { Index = 0, Arguments = "\"README.md\"" });
        _sut.Append(new ToolCallDelta { Index = 0, Arguments = "}", IsComplete = true });

        // Assert
        var toolCall = _sut.GetNextCompleted();
        toolCall.Function.Arguments.Should().Be("""{"path": "README.md"}""");
    }

    [Fact]
    public void Should_Handle_ConcurrentCalls_ByIndex()
    {
        // Arrange & Act - Interleaved fragments for two tool calls
        _sut.Append(new ToolCallDelta { Index = 0, FunctionName = "read_file" });
        _sut.Append(new ToolCallDelta { Index = 1, FunctionName = "write_file" });
        _sut.Append(new ToolCallDelta { Index = 0, Arguments = """{"path": "a.txt"}""" });
        _sut.Append(new ToolCallDelta { Index = 1, Arguments = """{"path": "b.txt", "content": "hi"}""" });
        _sut.Append(new ToolCallDelta { Index = 0, IsComplete = true });
        _sut.Append(new ToolCallDelta { Index = 1, IsComplete = true });

        // Assert
        var first = _sut.GetNextCompleted();
        var second = _sut.GetNextCompleted();
        
        first.Function.Name.Should().Be("read_file");
        first.Function.Arguments.Should().Contain("a.txt");
        
        second.Function.Name.Should().Be("write_file");
        second.Function.Arguments.Should().Contain("b.txt");
    }

    [Fact]
    public void Should_Report_NoCompleted_Until_IsComplete()
    {
        // Arrange & Act
        _sut.Append(new ToolCallDelta { Index = 0, FunctionName = "read_file" });
        _sut.Append(new ToolCallDelta { Index = 0, Arguments = """{"path": "test.txt"}""" });
        // Note: No IsComplete = true

        // Assert
        _sut.HasCompletedToolCall().Should().BeFalse();
    }

    [Fact]
    public void Should_GetAllRemaining_AtStreamEnd()
    {
        // Arrange
        _sut.Append(new ToolCallDelta { Index = 0, FunctionName = "tool1", Arguments = "{}", IsComplete = true });
        _sut.Append(new ToolCallDelta { Index = 1, FunctionName = "tool2", Arguments = "{}", IsComplete = true });
        _sut.Append(new ToolCallDelta { Index = 2, FunctionName = "tool3", Arguments = "{}", IsComplete = true });

        // Act
        var all = _sut.GetAllRemaining();

        // Assert
        all.Should().HaveCount(3);
        all[0].Function.Name.Should().Be("tool1");
        all[1].Function.Name.Should().Be("tool2");
        all[2].Function.Name.Should().Be("tool3");
    }
}
```

### Integration Tests

#### File: Tests/Integration/Ollama/ToolCall/ToolCallIntegrationTests.cs

```csharp
using Acode.Infrastructure.Ollama;
using Acode.Infrastructure.Ollama.ToolCall;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Acode.Integration.Tests.Ollama.ToolCall;

[Trait("Category", "Integration")]
public class ToolCallIntegrationTests : IClassFixture<OllamaTestFixture>
{
    private readonly OllamaTestFixture _fixture;

    public ToolCallIntegrationTests(OllamaTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_Parse_Real_ToolCall_FromOllama()
    {
        // Arrange
        var request = new OllamaRequest
        {
            Model = "llama3.1",
            Messages = new[]
            {
                new OllamaMessage { Role = "user", Content = "Read the README.md file" }
            },
            Tools = new[]
            {
                new OllamaTool
                {
                    Type = "function",
                    Function = new OllamaToolFunction
                    {
                        Name = "read_file",
                        Description = "Read a file's contents",
                        Parameters = new { type = "object", properties = new { path = new { type = "string" } }, required = new[] { "path" } }
                    }
                }
            }
        };

        // Act
        var response = await _fixture.Client.SendAsync(request, CancellationToken.None);
        var parseResult = _fixture.Parser.Parse(response);

        // Assert
        parseResult.ToolCalls.Should().NotBeEmpty();
        parseResult.ToolCalls.First().Name.Should().Be("read_file");
        parseResult.ToolCalls.First().Arguments.TryGetProperty("path", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Should_Repair_And_Succeed_OnMalformedResponse()
    {
        // Arrange - Force malformed JSON by using model prone to trailing commas
        // This test uses a mock response that simulates a common Ollama error
        var malformedResponse = new OllamaResponse
        {
            Message = new OllamaMessage
            {
                ToolCalls = new List<OllamaToolCall>
                {
                    new()
                    {
                        Id = "call_test",
                        Function = new OllamaFunction
                        {
                            Name = "read_file",
                            Arguments = """{"path": "test.txt",}""" // Trailing comma
                        }
                    }
                }
            }
        };

        // Act
        var parseResult = _fixture.Parser.Parse(malformedResponse);

        // Assert
        parseResult.ToolCalls.Should().HaveCount(1);
        parseResult.ToolCalls[0].Arguments.GetProperty("path").GetString().Should().Be("test.txt");
    }

    [Fact]
    public async Task Should_Retry_And_Succeed_AfterInitialFailure()
    {
        // Arrange
        var initialMalformed = "completely not json {{{";
        
        // Act
        var retryResult = await _fixture.RetryHandler.RetryToolCallAsync(
            toolName: "read_file",
            malformedArgs: initialMalformed,
            parseError: "Invalid JSON at position 0",
            correlationId: "integration-test-001",
            CancellationToken.None);

        // Assert
        retryResult.Success.Should().BeTrue();
        retryResult.RetryCount.Should().BeGreaterOrEqualTo(1);
    }
}

public class OllamaTestFixture : IDisposable
{
    public IOllamaClient Client { get; }
    public ToolCallParser Parser { get; }
    public ToolCallRetryHandler RetryHandler { get; }

    public OllamaTestFixture()
    {
        // Setup real Ollama client for integration tests
        Client = new OllamaClient(new HttpClient { BaseAddress = new Uri("http://localhost:11434") });
        Parser = new ToolCallParser(
            new JsonRepairer(),
            new InMemoryToolSchemaRegistry(),
            Substitute.For<ILogger<ToolCallParser>>());
        RetryHandler = new ToolCallRetryHandler(
            Client,
            new RetryConfig { MaxRetries = 3 },
            Substitute.For<ILogger<ToolCallRetryHandler>>());
    }

    public void Dispose() { }
}
```

### Performance Tests

#### File: Tests/Performance/Ollama/ToolCallBenchmarks.cs

```csharp
using BenchmarkDotNet.Attributes;
using System.Text.Json;
using Acode.Infrastructure.Ollama.ToolCall;

namespace Acode.Performance.Tests.Ollama;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class ToolCallBenchmarks
{
    private ToolCallParser _parser = null!;
    private JsonRepairer _repairer = null!;
    private SchemaValidator _validator = null!;
    private OllamaResponse _singleToolCallResponse = null!;
    private OllamaResponse _multipleToolCallResponse = null!;
    private string _malformedJson = null!;
    private JsonElement _validArgs = default;
    private JsonElement _schema = default;

    [GlobalSetup]
    public void Setup()
    {
        _repairer = new JsonRepairer();
        _validator = new SchemaValidator();
        _parser = new ToolCallParser(_repairer, new InMemoryToolSchemaRegistry(), 
            NullLogger<ToolCallParser>.Instance);

        _singleToolCallResponse = CreateResponse(1);
        _multipleToolCallResponse = CreateResponse(10);
        _malformedJson = """{"path": "test.txt",}""";
        _validArgs = JsonDocument.Parse("""{"path": "test.txt", "encoding": "utf-8"}""").RootElement;
        _schema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "path": { "type": "string", "maxLength": 4096 },
                "encoding": { "type": "string", "enum": ["utf-8", "ascii"] }
            },
            "required": ["path"]
        }
        """).RootElement;
    }

    [Benchmark(Description = "Extract single tool call")]
    public ToolCallParseResult Benchmark_Extraction_Single()
    {
        return _parser.Parse(_singleToolCallResponse);
    }

    [Benchmark(Description = "Extract 10 tool calls")]
    public ToolCallParseResult Benchmark_Extraction_Multiple()
    {
        return _parser.Parse(_multipleToolCallResponse);
    }

    [Benchmark(Description = "Repair trailing comma")]
    public RepairResult Benchmark_Repair_TrailingComma()
    {
        return _repairer.TryRepair(_malformedJson);
    }

    [Benchmark(Description = "Validate against schema")]
    public ValidationResult Benchmark_Validation()
    {
        return _validator.Validate(_validArgs, _schema);
    }

    [Benchmark(Description = "Full parse + validate")]
    public ToolCallParseResult Benchmark_FullPipeline()
    {
        return _parser.Parse(_singleToolCallResponse);
    }

    private static OllamaResponse CreateResponse(int toolCallCount)
    {
        var toolCalls = Enumerable.Range(0, toolCallCount)
            .Select(i => new OllamaToolCall
            {
                Id = $"call_{i}",
                Function = new OllamaFunction
                {
                    Name = "read_file",
                    Arguments = $$$"""{"path": "file{{{i}}}.txt"}"""
                }
            })
            .ToList();

        return new OllamaResponse
        {
            Message = new OllamaMessage { ToolCalls = toolCalls }
        };
    }
}

// Expected results (verify in CI):
// | Method                    | Mean       | Error     | Allocated |
// |---------------------------|------------|-----------|-----------|
// | Extract single tool call  | < 0.5 ms   | -         | < 1 KB    |
// | Extract 10 tool calls     | < 2.0 ms   | -         | < 5 KB    |
// | Repair trailing comma     | < 0.1 ms   | -         | < 500 B   |
// | Validate against schema   | < 1.0 ms   | -         | < 2 KB    |
// | Full parse + validate     | < 1.0 ms   | -         | < 3 KB    |
```

---

## User Verification Steps

### Scenario 1: Parse Valid Tool Call from Real Ollama Response

**Objective:** Verify that tool calls are correctly extracted from a real Ollama response.

**Prerequisites:**
- Ollama server running on localhost:11434
- A model with function calling support (e.g., llama3.1)
- Acode CLI installed and configured

**Steps:**

1. **Start Ollama server:**
   ```bash
   ollama serve
   ```

2. **Verify model is available:**
   ```bash
   ollama list
   # Confirm llama3.1 or similar is listed
   ```

3. **Send a request that will trigger tool calling:**
   ```bash
   acode chat --message "Read the README.md file" --tools read_file
   ```

4. **Observe the debug output:**
   ```bash
   acode chat --message "Read the README.md file" --tools read_file --log-level debug
   ```

**Expected Results:**
- Console shows: `[DEBUG] Parsed tool call: read_file (call_xxxxx)`
- Console shows: `[DEBUG] Arguments: {"path": "README.md"}`
- Tool execution proceeds successfully
- File contents are displayed

**Verification Command:**
```bash
acode providers stats ollama --tool-calls
# Should show: Parsed Successfully: 1
```

---

### Scenario 2: Automatic Repair of Trailing Comma

**Objective:** Verify that trailing commas in JSON arguments are automatically fixed.

**Prerequisites:**
- Access to test harness or mock response capability

**Steps:**

1. **Create a test response file with trailing comma:**
   ```bash
   cat > /tmp/malformed-response.json << 'EOF'
   {
     "message": {
       "role": "assistant",
       "tool_calls": [{
         "id": "call_test",
         "function": {
           "name": "read_file",
           "arguments": "{\"path\": \"test.txt\",}"
         }
       }]
     }
   }
   EOF
   ```

2. **Run the parser test command:**
   ```bash
   acode debug parse-tool-call --input /tmp/malformed-response.json
   ```

3. **Observe repair output:**
   ```
   [INFO] Tool call parsed successfully
   [DEBUG] Repair applied: trailing_comma
   [DEBUG] Original: {"path": "test.txt",}
   [DEBUG] Repaired: {"path": "test.txt"}
   ```

**Expected Results:**
- Parser reports SUCCESS status
- Repair log shows "trailing_comma" was applied
- Parsed arguments contain `{"path": "test.txt"}` without trailing comma

**Verification Command:**
```bash
acode providers stats ollama --tool-calls
# Should show: Auto-Repaired: 1
# Should show: Trailing comma: 1 under Repair Breakdown
```

---

### Scenario 3: Retry on Complete Parse Failure

**Objective:** Verify that the system retries with the model when JSON cannot be repaired.

**Prerequisites:**
- Ollama server with mock/test capability
- Max retries configured to 3

**Steps:**

1. **Configure max retries:**
   ```bash
   acode config set model.providers.ollama.tool_call.max_retries 3
   ```

2. **Trigger a request with a model configured to produce invalid JSON initially:**
   ```bash
   # Use the test harness with mock that returns invalid JSON twice, then valid
   acode test retry-scenario --initial-failures 2
   ```

3. **Observe retry logs:**
   ```
   [INFO] Tool call parse failed, attempting retry (1/3)
   [INFO] Retry prompt sent to model
   [INFO] Tool call parse failed, attempting retry (2/3)
   [INFO] Retry prompt sent to model
   [INFO] Retry succeeded on attempt 3
   ```

**Expected Results:**
- System retries exactly 2 times (3 total attempts)
- Final attempt succeeds
- Total latency includes retry round-trips
- Token usage reflects all attempts

**Verification Command:**
```bash
acode debug tool-call --correlation-id <id-from-logs>
# Should show: Retry Count: 2, Parse Result: SUCCESS
```

---

### Scenario 4: Schema Validation Failure with Clear Error

**Objective:** Verify that schema validation failures produce actionable error messages.

**Prerequisites:**
- Tool schema requires `path` property of type string

**Steps:**

1. **Create response with wrong type:**
   ```bash
   cat > /tmp/wrong-type-response.json << 'EOF'
   {
     "message": {
       "tool_calls": [{
         "id": "call_001",
         "function": {
           "name": "read_file",
           "arguments": "{\"path\": 12345}"
         }
       }]
     }
   }
   EOF
   ```

2. **Run parser with strict validation:**
   ```bash
   acode debug parse-tool-call --input /tmp/wrong-type-response.json --strict
   ```

3. **Observe validation error:**
   ```
   [ERROR] Schema validation failed
     Path: path
     Expected: string
     Actual: integer
     Error Code: ACODE-TLP-004
   ```

**Expected Results:**
- Error message identifies the exact field (path)
- Error message shows expected type (string) vs actual type (integer)
- Error code ACODE-TLP-004 is returned
- Retry is triggered with validation context

**Verification:**
- ToolCallValidationException is thrown with correct properties
- Logs show validation failure at INFO level

---

### Scenario 5: Multiple Tool Calls Preserved in Order

**Objective:** Verify that multiple tool calls are extracted and maintained in order.

**Steps:**

1. **Create response with 5 tool calls:**
   ```bash
   cat > /tmp/multi-tool-response.json << 'EOF'
   {
     "message": {
       "tool_calls": [
         {"id": "call_1", "function": {"name": "read_file", "arguments": "{\"path\": \"a.txt\"}"}},
         {"id": "call_2", "function": {"name": "read_file", "arguments": "{\"path\": \"b.txt\"}"}},
         {"id": "call_3", "function": {"name": "read_file", "arguments": "{\"path\": \"c.txt\"}"}},
         {"id": "call_4", "function": {"name": "write_file", "arguments": "{\"path\": \"out.txt\", \"content\": \"combined\"}"}},
         {"id": "call_5", "function": {"name": "execute_command", "arguments": "{\"command\": \"ls\"}"}}
       ]
     }
   }
   EOF
   ```

2. **Parse the response:**
   ```bash
   acode debug parse-tool-call --input /tmp/multi-tool-response.json --verbose
   ```

3. **Verify order and count:**
   ```
   Parsed 5 tool calls:
     [0] read_file (call_1) - path: a.txt
     [1] read_file (call_2) - path: b.txt
     [2] read_file (call_3) - path: c.txt
     [3] write_file (call_4) - path: out.txt
     [4] execute_command (call_5) - command: ls
   ```

**Expected Results:**
- Exactly 5 tool calls parsed
- Order matches input order (call_1 first, call_5 last)
- Each tool call has correct name and arguments

---

### Scenario 6: Streaming Tool Call Accumulation

**Objective:** Verify that streaming tool calls are correctly accumulated from fragments.

**Steps:**

1. **Start streaming request:**
   ```bash
   acode chat --message "Read README.md and show the first 10 lines" \
     --tools read_file \
     --stream \
     --log-level debug
   ```

2. **Observe streaming accumulation logs:**
   ```
   [DEBUG] Stream chunk: tool_call delta (index=0, name="read")
   [DEBUG] Stream chunk: tool_call delta (index=0, name="_file")
   [DEBUG] Stream chunk: tool_call delta (index=0, args="{\"path\":")
   [DEBUG] Stream chunk: tool_call delta (index=0, args=" \"README.md\"}")
   [DEBUG] Stream chunk: tool_call complete (index=0)
   [INFO] Accumulated tool call: read_file (call_stream_0)
   ```

3. **Verify tool execution:**
   - File contents are displayed
   - No "incomplete tool call" errors

**Expected Results:**
- Streaming fragments are accumulated in order
- Complete tool call is available after `IsComplete` signal
- Tool execution proceeds normally

---

### Scenario 7: Max Retries Exhausted Error

**Objective:** Verify clear error when all retry attempts fail.

**Steps:**

1. **Configure low max retries:**
   ```bash
   acode config set model.providers.ollama.tool_call.max_retries 2
   ```

2. **Use test harness to simulate always-invalid responses:**
   ```bash
   acode test retry-scenario --always-fail
   ```

3. **Observe exhaustion error:**
   ```
   [WARN] Tool call retry failed (attempt 1/2)
   [WARN] Tool call retry failed (attempt 2/2)
   [ERROR] ToolCallRetryExhaustedException: Failed after 2 attempts
     Last error: Invalid JSON at position 0
     Tool: read_file
     Correlation ID: xyz-123
   ```

**Expected Results:**
- Exactly 2 retry attempts made
- ToolCallRetryExhaustedException thrown
- Error includes attempt count and last error message
- User-friendly message displayed

**User Message:**
```
Unable to complete tool call after 2 attempts.
The AI was unable to produce valid JSON for the 'read_file' tool.
Please try rephrasing your request or use a different model.
```

---

### Scenario 8: Unknown Tool Rejection

**Objective:** Verify that tool calls for unknown tools are rejected with clear error.

**Steps:**

1. **Create response with unknown tool:**
   ```bash
   cat > /tmp/unknown-tool-response.json << 'EOF'
   {
     "message": {
       "tool_calls": [{
         "id": "call_001",
         "function": {
           "name": "hack_system",
           "arguments": "{}"
         }
       }]
     }
   }
   EOF
   ```

2. **Parse response:**
   ```bash
   acode debug parse-tool-call --input /tmp/unknown-tool-response.json
   ```

3. **Observe rejection:**
   ```
   [ERROR] Unknown tool: hack_system
     Error Code: ACODE-TLP-005
     Available tools: read_file, write_file, execute_command, ...
   ```

**Expected Results:**
- Tool call rejected immediately (no retry attempted)
- Error code ACODE-TLP-005
- Error message lists available tools for reference

---

### Scenario 9: Large Argument Size Rejection

**Objective:** Verify that arguments exceeding size limit are rejected.

**Steps:**

1. **Configure size limit:**
   ```bash
   acode config set model.providers.ollama.tool_call.max_argument_size 1024
   ```

2. **Create response with large arguments:**
   ```bash
   # Generate 2KB of content
   LARGE_CONTENT=$(python3 -c "print('x' * 2048)")
   cat > /tmp/large-args-response.json << EOF
   {
     "message": {
       "tool_calls": [{
         "id": "call_001",
         "function": {
           "name": "write_file",
           "arguments": "{\"path\": \"test.txt\", \"content\": \"${LARGE_CONTENT}\"}"
         }
       }]
     }
   }
   EOF
   ```

3. **Parse response:**
   ```bash
   acode debug parse-tool-call --input /tmp/large-args-response.json
   ```

**Expected Results:**
- Parse fails immediately with size limit error
- No JSON parsing attempted (security: prevents memory exhaustion)
- Error message includes actual size vs limit

---

### Scenario 10: Configuration Verification

**Objective:** Verify all configuration options are respected.

**Steps:**

1. **View current configuration:**
   ```bash
   acode config show model.providers.ollama.tool_call
   ```

   Expected output:
   ```
   Tool Call Parsing Configuration:
     max_retries:        3
     enable_auto_repair: true
     repair_timeout_ms:  100
     strict_validation:  true
     max_nesting_depth:  64
     max_argument_size:  1048576
     log_repair_details: true
     log_argument_values: false
   ```

2. **Disable auto-repair and verify:**
   ```bash
   acode config set model.providers.ollama.tool_call.enable_auto_repair false
   ```

3. **Attempt to parse malformed JSON:**
   ```bash
   # Should now fail immediately without repair attempt
   acode debug parse-tool-call --input /tmp/malformed-response.json
   ```

   Expected output:
   ```
   [ERROR] JSON parse failed (auto-repair disabled)
     Error: Unexpected token at position 22
   ```

4. **Re-enable auto-repair:**
   ```bash
   acode config set model.providers.ollama.tool_call.enable_auto_repair true
   ```

**Expected Results:**
- Configuration values match expected defaults
- Disabling auto-repair causes immediate failure on malformed JSON
- Configuration changes take effect immediately

---

## Implementation Prompt for Claude

### Implementation Overview

This task implements the tool call parsing and retry-on-invalid-JSON mechanism for the Ollama provider adapter. The implementation consists of five core components that work together to extract, repair, validate, and retry tool calls from Ollama responses.

**What You'll Build:**
- ToolCallParser: Extracts and parses tool calls from Ollama responses
- JsonRepairer: Automatically fixes common JSON syntax errors
- SchemaValidator: Validates arguments against tool JSON schemas
- ToolCallRetryHandler: Re-prompts model when parsing fails
- StreamingToolCallAccumulator: Assembles tool calls from stream fragments

### Prerequisites

**Required:**
- .NET 8 SDK installed
- Task 007 (Tool Schema Registry) complete
- Task 005 (Ollama Provider Adapter) complete
- Task 004.a (ToolCall type) complete

**NuGet Packages:**
- System.Text.Json (included in .NET 8)
- JsonSchema.Net (for schema validation)
- Microsoft.Extensions.Logging.Abstractions

### Step 1: Create Directory Structure

Create the following folder structure:

```
src/Acode.Infrastructure/Ollama/ToolCall/
├── ToolCallParser.cs
├── JsonRepairer.cs
├── SchemaValidator.cs
├── ToolCallRetryHandler.cs
├── StreamingToolCallAccumulator.cs
├── Models/
│   ├── OllamaToolCall.cs
│   ├── RepairResult.cs
│   ├── ToolCallParseResult.cs
│   ├── ToolCallError.cs
│   ├── ValidationResult.cs
│   ├── ValidationError.cs
│   ├── RetryResult.cs
│   ├── ToolCallDelta.cs
│   └── RetryConfig.cs
└── Exceptions/
    ├── ToolCallParseException.cs
    ├── ToolCallValidationException.cs
    └── ToolCallRetryExhaustedException.cs
```

### Step 2: Implement Models

#### OllamaToolCall.cs

```csharp
using System.Text.Json.Serialization;

namespace Acode.Infrastructure.Ollama.ToolCall.Models;

/// <summary>
/// Represents a tool call in Ollama's response format.
/// Maps from Ollama's JSON structure to internal representation.
/// </summary>
public sealed class OllamaToolCall
{
    /// <summary>
    /// Unique identifier for this tool call.
    /// May be null in some Ollama responses; will be generated if missing.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    /// <summary>
    /// Type of tool call. Always "function" for Ollama.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";
    
    /// <summary>
    /// The function to be called.
    /// </summary>
    [JsonPropertyName("function")]
    public OllamaFunction? Function { get; set; }
}

/// <summary>
/// Represents the function details within a tool call.
/// </summary>
public sealed class OllamaFunction
{
    /// <summary>
    /// Name of the function/tool to invoke.
    /// Must match a registered tool in the schema registry.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON string containing the function arguments.
    /// May be malformed; JsonRepairer will attempt to fix common errors.
    /// </summary>
    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = "{}";
}
```

#### RepairResult.cs

```csharp
namespace Acode.Infrastructure.Ollama.ToolCall.Models;

/// <summary>
/// Result of a JSON repair attempt.
/// </summary>
public sealed class RepairResult
{
    /// <summary>
    /// Whether the repair was successful (output is valid JSON).
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// The original malformed JSON input.
    /// </summary>
    public string OriginalJson { get; init; } = string.Empty;
    
    /// <summary>
    /// The repaired JSON (valid) if successful, or original if repair failed.
    /// </summary>
    public string RepairedJson { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether any repairs were actually applied.
    /// False if input was already valid JSON.
    /// </summary>
    public bool WasRepaired { get; init; }
    
    /// <summary>
    /// List of repair operations that were applied.
    /// </summary>
    public IReadOnlyList<string> Repairs { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// Error message if repair failed.
    /// </summary>
    public string? Error { get; init; }
    
    /// <summary>
    /// Create a successful repair result.
    /// </summary>
    public static RepairResult Ok(string original, string repaired, IReadOnlyList<string> repairs) =>
        new()
        {
            Success = true,
            OriginalJson = original,
            RepairedJson = repaired,
            WasRepaired = repairs.Count > 0,
            Repairs = repairs
        };
    
    /// <summary>
    /// Create a result for already-valid JSON.
    /// </summary>
    public static RepairResult AlreadyValid(string json) =>
        new()
        {
            Success = true,
            OriginalJson = json,
            RepairedJson = json,
            WasRepaired = false,
            Repairs = Array.Empty<string>()
        };
    
    /// <summary>
    /// Create a failed repair result.
    /// </summary>
    public static RepairResult Fail(string original, string error) =>
        new()
        {
            Success = false,
            OriginalJson = original,
            RepairedJson = original,
            WasRepaired = false,
            Error = error
        };
}
```

#### ToolCallParseResult.cs

```csharp
using Acode.Domain.Inference;

namespace Acode.Infrastructure.Ollama.ToolCall.Models;

/// <summary>
/// Result of parsing tool calls from an Ollama response.
/// May contain a mix of successful parses and errors.
/// </summary>
public sealed class ToolCallParseResult
{
    /// <summary>
    /// Successfully parsed tool calls.
    /// </summary>
    public IReadOnlyList<ToolCall> ToolCalls { get; init; } = Array.Empty<ToolCall>();
    
    /// <summary>
    /// Errors encountered during parsing.
    /// </summary>
    public IReadOnlyList<ToolCallError> Errors { get; init; } = Array.Empty<ToolCallError>();
    
    /// <summary>
    /// Whether all tool calls were parsed successfully.
    /// </summary>
    public bool AllSucceeded => Errors.Count == 0;
    
    /// <summary>
    /// Whether any errors occurred during parsing.
    /// </summary>
    public bool HasErrors => Errors.Count > 0;
    
    /// <summary>
    /// Total number of tool calls (successful + failed).
    /// </summary>
    public int TotalCount => ToolCalls.Count + Errors.Count;
    
    /// <summary>
    /// Repair details if any JSON was repaired.
    /// </summary>
    public IReadOnlyList<RepairResult> Repairs { get; init; } = Array.Empty<RepairResult>();
    
    /// <summary>
    /// Create an empty result (no tool calls in response).
    /// </summary>
    public static ToolCallParseResult Empty() => new()
    {
        ToolCalls = Array.Empty<ToolCall>(),
        Errors = Array.Empty<ToolCallError>()
    };
    
    /// <summary>
    /// Create a result from parsed tool calls and errors.
    /// </summary>
    public ToolCallParseResult(IReadOnlyList<ToolCall> toolCalls, IReadOnlyList<ToolCallError> errors)
    {
        ToolCalls = toolCalls;
        Errors = errors;
    }
    
    private ToolCallParseResult() { }
}

/// <summary>
/// Error encountered while parsing a tool call.
/// </summary>
public sealed class ToolCallError
{
    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public string Message { get; init; } = string.Empty;
    
    /// <summary>
    /// Error code for programmatic handling (ACODE-TLP-XXX).
    /// </summary>
    public string ErrorCode { get; init; } = string.Empty;
    
    /// <summary>
    /// Tool name if known.
    /// </summary>
    public string? ToolName { get; init; }
    
    /// <summary>
    /// Position in JSON where error occurred.
    /// </summary>
    public int? ErrorPosition { get; init; }
    
    /// <summary>
    /// Original malformed JSON arguments.
    /// </summary>
    public string? RawArguments { get; init; }
    
    public ToolCallError(string message, string errorCode)
    {
        Message = message;
        ErrorCode = errorCode;
    }
}
```

#### RetryConfig.cs

```csharp
namespace Acode.Infrastructure.Ollama.ToolCall.Models;

/// <summary>
/// Configuration for tool call retry behavior.
/// </summary>
public sealed class RetryConfig
{
    /// <summary>
    /// Maximum number of retry attempts before giving up.
    /// Default: 3. Range: 1-10.
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// Whether to attempt automatic JSON repair before retrying.
    /// Default: true.
    /// </summary>
    public bool EnableAutoRepair { get; set; } = true;
    
    /// <summary>
    /// Timeout for repair attempts in milliseconds.
    /// Default: 100.
    /// </summary>
    public int RepairTimeoutMs { get; set; } = 100;
    
    /// <summary>
    /// Base delay between retry attempts in milliseconds.
    /// Uses exponential backoff: delay * 2^attempt.
    /// Default: 100 (so: 100ms, 200ms, 400ms).
    /// </summary>
    public int RetryDelayMs { get; set; } = 100;
    
    /// <summary>
    /// Enforce strict schema validation (no extra properties).
    /// Default: true.
    /// </summary>
    public bool StrictValidation { get; set; } = true;
    
    /// <summary>
    /// Maximum JSON nesting depth.
    /// Default: 64.
    /// </summary>
    public int MaxNestingDepth { get; set; } = 64;
    
    /// <summary>
    /// Maximum argument size in bytes.
    /// Default: 1MB.
    /// </summary>
    public int MaxArgumentSize { get; set; } = 1_048_576;
    
    /// <summary>
    /// Template for retry prompts. Supports placeholders:
    /// {error_message}, {error_position}, {malformed_json}, {tool_name}, {schema_example}
    /// </summary>
    public string RetryPromptTemplate { get; set; } = DefaultRetryPromptTemplate;
    
    private const string DefaultRetryPromptTemplate = """
        The tool call arguments you provided contain invalid JSON.
        
        Error: {error_message}
        Position: {error_position}
        
        Your output:
        ```json
        {malformed_json}
        ```
        
        Please provide corrected JSON arguments for the '{tool_name}' tool.
        Do not include any explanation, only the corrected JSON object.
        """;
}
```

### Step 3: Implement JsonRepairer

```csharp
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Acode.Infrastructure.Ollama.ToolCall.Models;

namespace Acode.Infrastructure.Ollama.ToolCall;

/// <summary>
/// Attempts to repair common JSON syntax errors produced by language models.
/// All repairs are deterministic and idempotent.
/// </summary>
public sealed class JsonRepairer
{
    private readonly int _timeoutMs;
    
    public JsonRepairer(int timeoutMs = 100)
    {
        _timeoutMs = timeoutMs;
    }
    
    /// <summary>
    /// Attempt to repair malformed JSON.
    /// Returns immediately if JSON is already valid.
    /// </summary>
    public RepairResult TryRepair(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return RepairResult.Fail(json, "Empty JSON string");
        }
        
        // Check if already valid
        if (IsValidJson(json))
        {
            return RepairResult.AlreadyValid(json);
        }
        
        using var cts = new CancellationTokenSource(_timeoutMs);
        
        try
        {
            return RepairWithTimeout(json, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return RepairResult.Fail(json, $"Repair timeout ({_timeoutMs}ms exceeded)");
        }
    }
    
    private RepairResult RepairWithTimeout(string json, CancellationToken ct)
    {
        var repairs = new List<string>();
        var current = json;
        
        // Apply repairs in order of frequency
        current = FixTrailingCommas(current, repairs, ct);
        current = FixSingleQuotes(current, repairs, ct);
        current = FixUnquotedKeys(current, repairs, ct);
        current = FixMissingBraces(current, repairs, ct);
        current = FixMissingBrackets(current, repairs, ct);
        current = FixTruncatedStrings(current, repairs, ct);
        current = FixUnescapedQuotes(current, repairs, ct);
        
        if (IsValidJson(current))
        {
            return RepairResult.Ok(json, current, repairs);
        }
        
        return RepairResult.Fail(json, "Unable to repair JSON");
    }
    
    private static string FixTrailingCommas(string json, List<string> repairs, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        // Remove trailing commas before } or ]
        var pattern = @",\s*([}\]])";
        var result = Regex.Replace(json, pattern, "$1");
        
        if (result != json)
        {
            repairs.Add("trailing_comma");
        }
        
        return result;
    }
    
    private static string FixSingleQuotes(string json, List<string> repairs, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        // Replace single quotes with double quotes (outside of existing double-quoted strings)
        var result = new StringBuilder();
        var inDoubleQuote = false;
        var prevChar = '\0';
        
        foreach (var c in json)
        {
            if (c == '"' && prevChar != '\\')
            {
                inDoubleQuote = !inDoubleQuote;
                result.Append(c);
            }
            else if (c == '\'' && !inDoubleQuote)
            {
                result.Append('"');
            }
            else
            {
                result.Append(c);
            }
            prevChar = c;
        }
        
        var final = result.ToString();
        if (final != json)
        {
            repairs.Add("single_quotes");
        }
        
        return final;
    }
    
    private static string FixUnquotedKeys(string json, List<string> repairs, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        // Match unquoted property names: {key: or ,key:
        var pattern = @"([{,]\s*)([a-zA-Z_][a-zA-Z0-9_]*)\s*:";
        var result = Regex.Replace(json, pattern, "$1\"$2\":");
        
        if (result != json)
        {
            repairs.Add("unquoted_key");
        }
        
        return result;
    }
    
    private static string FixMissingBraces(string json, List<string> repairs, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        var openCount = json.Count(c => c == '{');
        var closeCount = json.Count(c => c == '}');
        
        if (openCount > closeCount)
        {
            var diff = openCount - closeCount;
            var result = json + new string('}', diff);
            repairs.Add("missing_closing_brace");
            return result;
        }
        
        return json;
    }
    
    private static string FixMissingBrackets(string json, List<string> repairs, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        var openCount = json.Count(c => c == '[');
        var closeCount = json.Count(c => c == ']');
        
        if (openCount > closeCount)
        {
            var diff = openCount - closeCount;
            // Insert before closing braces
            var bracePos = json.LastIndexOf('}');
            var result = bracePos > 0 
                ? json.Insert(bracePos, new string(']', diff))
                : json + new string(']', diff);
            repairs.Add("missing_closing_bracket");
            return result;
        }
        
        return json;
    }
    
    private static string FixTruncatedStrings(string json, List<string> repairs, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        // Check if string is unclosed at end
        var inString = false;
        var prevChar = '\0';
        
        foreach (var c in json)
        {
            if (c == '"' && prevChar != '\\')
            {
                inString = !inString;
            }
            prevChar = c;
        }
        
        if (inString)
        {
            // Close the string and add closing braces
            var result = json + "\"";
            repairs.Add("truncated_string");
            return result;
        }
        
        return json;
    }
    
    private static string FixUnescapedQuotes(string json, List<string> repairs, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        // This is complex - detect quotes that should be escaped
        // For now, use a simple heuristic: if we have odd internal quotes, escape them
        var result = new StringBuilder();
        var inString = false;
        var i = 0;
        
        while (i < json.Length)
        {
            var c = json[i];
            
            if (c == '"')
            {
                if (i > 0 && json[i - 1] == '\\')
                {
                    // Already escaped
                    result.Append(c);
                }
                else if (!inString)
                {
                    // Starting a string
                    inString = true;
                    result.Append(c);
                }
                else
                {
                    // Check if this is really end of string
                    var remaining = json.Substring(i + 1).TrimStart();
                    if (remaining.Length == 0 || 
                        remaining[0] == ',' || 
                        remaining[0] == '}' || 
                        remaining[0] == ']' ||
                        remaining[0] == ':')
                    {
                        // Legit end of string
                        inString = false;
                        result.Append(c);
                    }
                    else
                    {
                        // Quote inside string - escape it
                        result.Append("\\\"");
                        repairs.Add("unescaped_quotes");
                    }
                }
            }
            else
            {
                result.Append(c);
            }
            
            i++;
        }
        
        return result.ToString();
    }
    
    private static bool IsValidJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
```

### Step 4: Implement ToolCallParser

```csharp
using System.Text.Json;
using Acode.Application.Interfaces;
using Acode.Domain.Inference;
using Acode.Domain.ToolSchemas;
using Acode.Infrastructure.Ollama.ToolCall.Exceptions;
using Acode.Infrastructure.Ollama.ToolCall.Models;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Ollama.ToolCall;

/// <summary>
/// Parses tool calls from Ollama responses, applying JSON repair and schema validation.
/// </summary>
public sealed class ToolCallParser
{
    private readonly JsonRepairer _repairer;
    private readonly IToolSchemaRegistry _schemaRegistry;
    private readonly SchemaValidator _validator;
    private readonly ILogger<ToolCallParser> _logger;
    private readonly RetryConfig _config;
    
    public ToolCallParser(
        JsonRepairer repairer,
        IToolSchemaRegistry schemaRegistry,
        ILogger<ToolCallParser> logger,
        RetryConfig? config = null)
    {
        _repairer = repairer;
        _schemaRegistry = schemaRegistry;
        _validator = new SchemaValidator();
        _logger = logger;
        _config = config ?? new RetryConfig();
    }
    
    /// <summary>
    /// Parse tool calls from an Ollama response.
    /// </summary>
    public ToolCallParseResult Parse(OllamaResponse response)
    {
        if (response.Message?.ToolCalls is null or { Count: 0 })
        {
            _logger.LogDebug("No tool calls in response");
            return ToolCallParseResult.Empty();
        }
        
        var toolCalls = new List<Domain.Inference.ToolCall>();
        var errors = new List<ToolCallError>();
        var repairs = new List<RepairResult>();
        
        for (var index = 0; index < response.Message.ToolCalls.Count; index++)
        {
            var ollamaCall = response.Message.ToolCalls[index];
            var result = ParseSingleCall(ollamaCall, index);
            
            if (result.Success)
            {
                toolCalls.Add(result.ToolCall!);
                if (result.Repair is not null)
                {
                    repairs.Add(result.Repair);
                }
            }
            else
            {
                errors.Add(result.Error!);
            }
        }
        
        _logger.LogInformation(
            "Parsed {SuccessCount} tool calls, {ErrorCount} errors",
            toolCalls.Count,
            errors.Count);
        
        return new ToolCallParseResult(toolCalls, errors)
        {
            Repairs = repairs
        };
    }
    
    private SingleParseResult ParseSingleCall(OllamaToolCall call, int index)
    {
        // Validate function name
        if (string.IsNullOrWhiteSpace(call.Function?.Name))
        {
            _logger.LogWarning("Tool call at index {Index} has empty function name", index);
            return SingleParseResult.Fail(new ToolCallError(
                "Function name is required",
                "ACODE-TLP-001"));
        }
        
        var toolName = call.Function.Name;
        
        // Validate tool exists
        if (!_schemaRegistry.TryGetSchema(toolName, out var schema))
        {
            _logger.LogWarning("Unknown tool: {ToolName}", toolName);
            return SingleParseResult.Fail(new ToolCallError(
                $"Unknown tool name: {toolName}",
                "ACODE-TLP-005")
            {
                ToolName = toolName
            });
        }
        
        // Check argument size
        var argsJson = call.Function.Arguments ?? "{}";
        if (argsJson.Length > _config.MaxArgumentSize)
        {
            return SingleParseResult.Fail(new ToolCallError(
                $"Arguments exceed size limit ({argsJson.Length} > {_config.MaxArgumentSize})",
                "ACODE-TLP-009"));
        }
        
        // Parse JSON arguments
        JsonElement arguments;
        RepairResult? repair = null;
        
        if (!TryParseJson(argsJson, out arguments, out var parseError))
        {
            if (!_config.EnableAutoRepair)
            {
                return SingleParseResult.Fail(new ToolCallError(
                    $"Invalid JSON (auto-repair disabled): {parseError}",
                    "ACODE-TLP-002")
                {
                    RawArguments = argsJson
                });
            }
            
            // Attempt repair
            repair = _repairer.TryRepair(argsJson);
            
            if (!repair.Success)
            {
                _logger.LogWarning(
                    "JSON repair failed for {ToolName}: {Error}",
                    toolName,
                    repair.Error);
                    
                return SingleParseResult.Fail(new ToolCallError(
                    $"JSON repair failed: {repair.Error}",
                    "ACODE-TLP-003")
                {
                    RawArguments = argsJson
                });
            }
            
            _logger.LogDebug(
                "Repaired JSON for {ToolName}: {Repairs}",
                toolName,
                string.Join(", ", repair.Repairs));
            
            if (!TryParseJson(repair.RepairedJson, out arguments, out parseError))
            {
                return SingleParseResult.Fail(new ToolCallError(
                    $"Repaired JSON still invalid: {parseError}",
                    "ACODE-TLP-002"));
            }
        }
        
        // Validate arguments is an object
        if (arguments.ValueKind != JsonValueKind.Object)
        {
            return SingleParseResult.Fail(new ToolCallError(
                $"Arguments must be an object, got {arguments.ValueKind}",
                "ACODE-TLP-004"));
        }
        
        // Validate against schema
        var validationResult = _validator.Validate(arguments, schema!.Parameters, _config.StrictValidation);
        
        if (!validationResult.IsValid)
        {
            var errorDetails = string.Join("; ", validationResult.Errors.Select(e => $"{e.Path}: {e.Message}"));
            return SingleParseResult.Fail(new ToolCallError(
                $"Schema validation failed: {errorDetails}",
                "ACODE-TLP-004")
            {
                ToolName = toolName
            });
        }
        
        // Generate ID if missing
        var callId = call.Id ?? $"call_{Guid.NewGuid():N}";
        
        var toolCall = new Domain.Inference.ToolCall
        {
            Id = callId,
            Name = toolName,
            Arguments = arguments
        };
        
        _logger.LogDebug(
            "Parsed tool call: {ToolName} ({CallId})",
            toolName,
            callId);
        
        return SingleParseResult.Ok(toolCall, repair);
    }
    
    private static bool TryParseJson(string json, out JsonElement element, out string? error)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            element = doc.RootElement.Clone();
            error = null;
            return true;
        }
        catch (JsonException ex)
        {
            element = default;
            error = ex.Message;
            return false;
        }
    }
    
    private sealed class SingleParseResult
    {
        public bool Success { get; init; }
        public Domain.Inference.ToolCall? ToolCall { get; init; }
        public ToolCallError? Error { get; init; }
        public RepairResult? Repair { get; init; }
        
        public static SingleParseResult Ok(Domain.Inference.ToolCall toolCall, RepairResult? repair = null) =>
            new() { Success = true, ToolCall = toolCall, Repair = repair };
        
        public static SingleParseResult Fail(ToolCallError error) =>
            new() { Success = false, Error = error };
    }
}
```

### Step 5: Implement ToolCallRetryHandler

```csharp
using System.Text.Json;
using Acode.Infrastructure.Ollama.ToolCall.Exceptions;
using Acode.Infrastructure.Ollama.ToolCall.Models;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Ollama.ToolCall;

/// <summary>
/// Handles retry logic when tool call parsing fails.
/// Re-prompts the model with error context to get corrected JSON.
/// </summary>
public sealed class ToolCallRetryHandler
{
    private readonly IOllamaClient _client;
    private readonly RetryConfig _config;
    private readonly ILogger<ToolCallRetryHandler> _logger;
    
    public ToolCallRetryHandler(
        IOllamaClient client,
        RetryConfig config,
        ILogger<ToolCallRetryHandler> logger)
    {
        _client = client;
        _config = config;
        _logger = logger;
    }
    
    /// <summary>
    /// Retry a failed tool call by re-prompting the model.
    /// </summary>
    public async Task<RetryResult> RetryToolCallAsync(
        string toolName,
        string malformedArgs,
        string parseError,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var totalTokens = 0;
        var lastError = parseError;
        
        for (var attempt = 1; attempt <= _config.MaxRetries; attempt++)
        {
            _logger.LogInformation(
                "Retry attempt {Attempt}/{MaxRetries} for {ToolName} (correlation: {CorrelationId})",
                attempt,
                _config.MaxRetries,
                toolName,
                correlationId);
            
            // Exponential backoff
            if (attempt > 1)
            {
                var delay = _config.RetryDelayMs * (int)Math.Pow(2, attempt - 1);
                await Task.Delay(delay, cancellationToken);
            }
            
            // Build retry prompt
            var retryPrompt = BuildRetryPrompt(toolName, malformedArgs, lastError);
            
            // Send retry request
            var request = new OllamaRequest
            {
                Model = "current", // Will use conversation's model
                Messages = new[]
                {
                    new OllamaMessage
                    {
                        Role = "user",
                        Content = retryPrompt
                    }
                }
            };
            
            var response = await _client.SendAsync(request, cancellationToken);
            totalTokens += (response.PromptEvalCount ?? 0) + (response.EvalCount ?? 0);
            
            // Try to extract corrected tool call
            if (response.Message?.ToolCalls is { Count: > 0 })
            {
                var toolCall = response.Message.ToolCalls[0];
                var argsJson = toolCall.Function?.Arguments ?? "{}";
                
                if (TryParseJson(argsJson, out var arguments))
                {
                    _logger.LogInformation(
                        "Retry succeeded on attempt {Attempt} for {ToolName}",
                        attempt,
                        toolName);
                    
                    return new RetryResult
                    {
                        Success = true,
                        Arguments = arguments,
                        RetryCount = attempt,
                        TotalTokensUsed = totalTokens
                    };
                }
                
                lastError = "Still invalid JSON after retry";
                malformedArgs = argsJson;
            }
            else
            {
                // Model didn't return a tool call, try to parse content as JSON
                var content = response.Message?.Content ?? "";
                if (TryParseJson(content.Trim(), out var arguments))
                {
                    return new RetryResult
                    {
                        Success = true,
                        Arguments = arguments,
                        RetryCount = attempt,
                        TotalTokensUsed = totalTokens
                    };
                }
                
                lastError = "No tool call in retry response";
            }
        }
        
        _logger.LogWarning(
            "Retry exhausted after {MaxRetries} attempts for {ToolName}",
            _config.MaxRetries,
            toolName);
        
        throw new ToolCallRetryExhaustedException(
            toolName,
            _config.MaxRetries,
            lastError,
            correlationId);
    }
    
    private string BuildRetryPrompt(string toolName, string malformedArgs, string error)
    {
        return _config.RetryPromptTemplate
            .Replace("{tool_name}", toolName)
            .Replace("{malformed_json}", malformedArgs)
            .Replace("{error_message}", error)
            .Replace("{error_position}", "0"); // Could extract from error
    }
    
    private static bool TryParseJson(string json, out JsonElement element)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            element = doc.RootElement.Clone();
            return element.ValueKind == JsonValueKind.Object;
        }
        catch
        {
            element = default;
            return false;
        }
    }
}

/// <summary>
/// Result of a retry attempt.
/// </summary>
public sealed class RetryResult
{
    public bool Success { get; init; }
    public JsonElement Arguments { get; init; }
    public int RetryCount { get; init; }
    public int TotalTokensUsed { get; init; }
}
```

### Step 6: Implement Exception Types

#### ToolCallRetryExhaustedException.cs

```csharp
namespace Acode.Infrastructure.Ollama.ToolCall.Exceptions;

/// <summary>
/// Thrown when all retry attempts for a tool call have been exhausted.
/// </summary>
public sealed class ToolCallRetryExhaustedException : Exception
{
    public string ToolName { get; }
    public int AttemptCount { get; }
    public string LastError { get; }
    public string CorrelationId { get; }
    
    public ToolCallRetryExhaustedException(
        string toolName,
        int attemptCount,
        string lastError,
        string correlationId)
        : base($"Tool call '{toolName}' failed after {attemptCount} retry attempts. Last error: {lastError}")
    {
        ToolName = toolName;
        AttemptCount = attemptCount;
        LastError = lastError;
        CorrelationId = correlationId;
    }
}
```

### Step 7: Register in Dependency Injection

Add to `src/Acode.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`:

```csharp
using Acode.Infrastructure.Ollama.ToolCall;
using Acode.Infrastructure.Ollama.ToolCall.Models;

// In AddInfrastructure method:
services.AddSingleton<JsonRepairer>();
services.AddSingleton<SchemaValidator>();
services.AddSingleton(sp => sp.GetRequiredService<IConfiguration>()
    .GetSection("Model:Providers:Ollama:ToolCall")
    .Get<RetryConfig>() ?? new RetryConfig());
services.AddScoped<ToolCallParser>();
services.AddScoped<ToolCallRetryHandler>();
services.AddScoped<StreamingToolCallAccumulator>();
```

### Error Codes Reference

| Code | Message | Cause |
|------|---------|-------|
| ACODE-TLP-001 | Function name is required | Tool call missing function.name |
| ACODE-TLP-002 | Invalid JSON in arguments | JSON parse failure |
| ACODE-TLP-003 | JSON repair failed | Auto-repair couldn't fix JSON |
| ACODE-TLP-004 | Schema validation failed | Arguments don't match schema |
| ACODE-TLP-005 | Unknown tool name | Tool not in registry |
| ACODE-TLP-006 | Max retries exhausted | All retry attempts failed |
| ACODE-TLP-007 | Repair timeout | Repair took > 100ms |
| ACODE-TLP-008 | Streaming accumulation error | Fragment assembly failed |
| ACODE-TLP-009 | Arguments exceed size limit | Args > max_argument_size |

### Implementation Checklist

- [ ] Create folder structure under Ollama/ToolCall/
- [ ] Implement OllamaToolCall model
- [ ] Implement OllamaFunction model
- [ ] Implement RepairResult type
- [ ] Implement ToolCallParseResult type
- [ ] Implement ToolCallError type
- [ ] Implement RetryConfig type
- [ ] Implement RetryResult type
- [ ] Implement ValidationResult type
- [ ] Implement JsonRepairer with 7 repair heuristics
- [ ] Implement ToolCallParser
- [ ] Implement SchemaValidator
- [ ] Implement ToolCallRetryHandler
- [ ] Implement StreamingToolCallAccumulator
- [ ] Implement ToolCallParseException
- [ ] Implement ToolCallValidationException
- [ ] Implement ToolCallRetryExhaustedException
- [ ] Register all services in DI
- [ ] Write ToolCallParserTests (7 tests)
- [ ] Write JsonRepairerTests (11 tests)
- [ ] Write RetryMechanismTests (6 tests)
- [ ] Write SchemaValidationTests (5 tests)
- [ ] Write StreamingAccumulatorTests (6 tests)
- [ ] Write ToolCallIntegrationTests (3 tests)
- [ ] Write ToolCallBenchmarks (5 benchmarks)
- [ ] Add XML documentation to all public types
- [ ] Update smoke test to replace stub (Task 005c)

### Dependencies

- **Task 007:** IToolSchemaRegistry for schema lookup
- **Task 007.b:** Error contract for retry prompts
- **Task 005:** IOllamaClient for retry requests
- **Task 004.a:** ToolCall domain type

### Verification Commands

```bash
# Build
dotnet build

# Run unit tests
dotnet test --filter "FullyQualifiedName~ToolCall"

# Run benchmarks
dotnet run -c Release --project tests/Acode.Performance.Tests -- --filter "ToolCall*"

# Verify registration
acode providers check ollama --component tool-call-parser

# Run smoke test
acode providers smoke-test ollama --tool-calls
```

---

**End of Task 007.d Specification**