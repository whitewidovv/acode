# Task 010.b: JSONL Event Stream Mode

**Priority:** P0 – Critical Path  
**Tier:** Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 010 (CLI Framework), Task 010.a (Command Routing)  

---

## Description

Task 010.b implements the JSONL event stream mode, providing machine-readable output for all CLI operations. JSONL (JSON Lines) format enables programmatic integration, automation, and tooling around Acode. This mode is essential for CI/CD pipelines, IDE plugins, monitoring systems, and any tooling that needs to parse CLI output. The business value is substantial: organizations automating code quality checks save an estimated $120,000 annually by eliminating manual review bottlenecks. Build pipelines integrating Acode reduce deployment cycle time by 40% through automated code generation and validation. Monitoring dashboards provide real-time visibility into agent operations, reducing incident response time from 30 minutes to 5 minutes when issues occur.

JSONL mode transforms all CLI output into a stream of newline-delimited JSON objects. Each line is a self-contained JSON object representing a discrete event: progress updates, status changes, approvals requested, actions taken, errors encountered. Consumers parse line-by-line without buffering the entire output, enabling real-time processing of long-running operations. This design choice directly supports streaming architectures where latency matters—a CI/CD pipeline can react to early failures without waiting for entire operation completion, saving compute resources by canceling failed runs immediately. The line-delimited format also enables trivial parallelization: multiple consumers can read the same stream and filter for their specific event types without coordination overhead.

The event stream architecture uses typed events. Every JSON object includes a `type` field identifying the event kind. Consumers can filter or route events based on type. Common event types include `progress`, `status`, `approval_request`, `action`, `error`, `completion`. This typing enables sophisticated handling without parsing message content. For example, a monitoring system subscribes only to `error` and `warning` events, ignoring progress noise. A deployment dashboard subscribes to `approval_request` events to surface human gates. A metrics collector subscribes to `model_event` to track token consumption and latency. This publish-subscribe pattern emerges naturally from typed events, enabling decoupled architectures where new consumers add without modifying the producer.

Schema stability is paramount for integration reliability. Event schemas are versioned and documented. Breaking changes increment the major version and are communicated with deprecation notices. Consumers can rely on schema contracts—fields don't disappear without warning, and new fields are additive only. Schema versioning follows semantic versioning: 1.0.0 is the initial release, 1.1.0 adds optional fields, 2.0.0 removes or renames fields. The `schema_version` field in every event enables consumers to validate compatibility before parsing. A consumer expecting schema 1.x can safely ignore 1.y events (backward compatible) but must reject 2.x events (breaking change). This contract enables gradual rollouts: new Acode versions introduce schema changes with deprecation warnings, giving consumers time to upgrade before old formats are removed.

Progress events provide granular visibility into long-running operations. Rather than a single completion message, consumers receive incremental updates: step started, percentage complete, estimated time remaining. This enables progress bars, monitoring dashboards, and timeout handling in automation. A typical operation emits 10-20 progress events over a 2-minute run. Each event includes `step`, `total`, `percentage`, `eta_seconds`, and `message` fields. Consumers rendering progress bars use `percentage` for display and `eta_seconds` for time remaining. Automation scripts use `step` and `total` to detect stalls—if the same step repeats for over 30 seconds, the operation is likely stuck. This granular feedback transforms opaque long-running operations into observable workflows.

Approval request events enable external approval workflows. When an action requires approval, the event includes all context needed for a decision: action type, affected files, risk level, proposed changes. External systems can display this information and submit responses via stdin or a separate approval endpoint. For example, a deployment pipeline integrating Acode can route approval requests to Slack, presenting a button UI for "approve" or "reject". When clicked, the external system writes the approval response to Acode's stdin, unblocking the agent. This pattern enables human-in-the-loop workflows while maintaining CLI simplicity—no web server, no database, just stdin/stdout communication. Approval requests include risk levels (low, medium, high) so external systems can auto-approve low-risk actions while escalating high-risk ones to senior engineers.

Error events include structured information for programmatic handling. Error code, message, affected component, stack trace (if verbose), suggested remediation. Automation can respond appropriately to different error types—retrying transient errors, failing on permanent ones, alerting on unexpected conditions. Error codes follow the pattern `ACODE-{COMPONENT}-{NUMBER}`, such as `ACODE-FILE-001` for file not found or `ACODE-MODEL-003` for inference timeout. Consumers pattern-match on codes to implement retry logic: `ACODE-MODEL-*` errors with "timeout" in the message trigger retries, while `ACODE-FILE-*` errors fail immediately. The `remediation` field provides actionable guidance, such as "Check file permissions" or "Increase timeout with --timeout flag". This structured error handling enables self-healing automation that resolves transient issues without human intervention.

The event stream integrates with all CLI subsystems. Model operations emit events for loading, inference, retries. File operations emit events for reads, writes, diffs. Agent orchestration emits events for state transitions, planning, execution. This comprehensive coverage provides complete observability. When an agent run completes, the event stream contains a complete audit trail: which files were analyzed, which model was used, how many tokens were consumed, which actions were approved, what errors occurred. This audit trail is invaluable for debugging, compliance, and performance analysis. A post-run analyzer can parse the event stream to generate reports on agent efficiency, cost per task, error rates, and approval patterns. Over time, these metrics inform optimization efforts—identifying slow operations, high-cost models, or error-prone components.

Stdout receives event lines; stderr receives logs. This separation enables clean event parsing while preserving diagnostic visibility. Consumers can pipe stdout to parsing logic while stderr provides human-readable context for debugging. The `--quiet` flag suppresses stderr if pure event output is needed. This design decision reflects Unix philosophy: stdout is for data, stderr is for diagnostics. A typical invocation pipes stdout to `jq` for filtering while stderr goes to a log file for later inspection. When debugging, a developer runs without `--quiet` to see both event stream (stdout) and diagnostic logs (stderr) side-by-side. When automating, a CI/CD script uses `--quiet` to ensure stdout contains only parseable events, with stderr redirected to build logs for audit.

Enabling JSONL mode is explicit. The `--json` flag activates event stream output. Without this flag, output remains human-readable. This explicit opt-in prevents accidental format changes that break existing workflows. A common mistake is piping human-readable output to a JSON parser, which fails cryptically. By requiring `--json`, we force intentional mode switching—users explicitly declare "I want machine-readable output". This prevents surprises and makes integration code self-documenting: `acode run --json "task"` clearly signals JSONL output, while `acode run "task"` clearly signals human output. The `ACODE_JSON=1` environment variable provides an alternative for environments where flag passing is awkward, such as Dockerfiles or CI/CD configuration files.

Buffering behavior is carefully managed. Events are flushed immediately after each line to ensure real-time streaming. This is critical for long-running operations—consumers shouldn't wait for buffer fills. Stdout is line-buffered in JSONL mode regardless of pipe status. Most systems default to block buffering when output is piped, causing events to queue in a buffer until 4KB accumulates. This defeats real-time streaming—a 2-minute operation with small events might not emit any output until completion. By forcing line buffering, we ensure each event appears immediately after emission. This is achieved via `Console.Out.Flush()` after every event write. The performance cost is negligible (< 0.1ms per flush) compared to the benefit of real-time visibility.

Timestamps use ISO 8601 format with millisecond precision. This standardization enables consistent time handling across time zones and systems. All timestamps are UTC to avoid local timezone complications. ISO 8601 format looks like `2024-01-15T10:30:00.123Z`, where `T` separates date and time, `.123` provides millisecond precision, and `Z` indicates UTC. This format is unambiguous across cultures (unlike `01/02/2024` which is January 2nd in the US but February 1st in Europe). Millisecond precision enables accurate latency measurement—a consumer can compute the time between `approval_request` and `approval_response` events to track approval latency. UTC timestamps avoid daylight saving time issues and timezone ambiguity. Consumers convert to local time for display if needed, but the event stream remains in UTC for consistency.

The integration points with Task 010 and Task 010.a are explicit. Task 010 provides the CLI framework with `--json` flag parsing via `CommandLineOptions` class. Task 010.a provides command routing through `CommandRouter` class, which identifies the executing command for inclusion in `session_start` events. The `JSONLOutputFormatter` class from this task wraps the `IOutputWriter` interface from Task 010, intercepting output calls and transforming them into events. When `--json` is enabled, `Program.cs` registers `JSONLOutputFormatter` as the `IOutputWriter` implementation, causing all output to flow through event serialization. This dependency injection pattern ensures JSONL mode integrates cleanly without modifying core command logic—commands continue writing to `IOutputWriter` without awareness of output format.

Constraints and limitations include no support for binary formats, no encryption, no compression, and no multi-language support. JSONL is text-only, which makes it human-inspectable but larger than binary formats like Protocol Buffers. We accept this trade-off for simplicity and tooling compatibility—every language and platform can parse JSON. No encryption means sensitive information must be redacted before emission. The `SecretRedactor` class handles this by detecting API keys, passwords, and tokens, replacing them with `***` or showing only the last 4 characters. No compression means large event streams consume significant disk space. For a 1-hour agent run emitting 10,000 events at 500 bytes each, the event log is 5MB uncompressed. Consumers can compress the output file after capture if needed.

Trade-offs considered include JSONL versus JSON array, stdout versus file output, and eager versus lazy serialization. JSONL was chosen over JSON array because arrays require closing the bracket at the end, preventing real-time streaming. Stdout was chosen over file output because it's pipe-friendly and lets consumers choose the destination. Eager serialization (immediate flush) was chosen over lazy buffering because real-time visibility matters more than throughput optimization. An alternative design considered was a separate event server (WebSocket or HTTP SSE), but this adds deployment complexity and doesn't integrate well with CLI tools expecting stdout/stderr.

Alternative approaches considered include structured logging to a file, binary event formats, and protocol buffers. Structured logging (e.g., via Serilog with JSON formatter) was rejected because it conflates diagnostic logs with domain events—we want a clean separation. Binary formats were rejected for reduced tooling compatibility and human inspectability. Protocol Buffers were rejected for implementation complexity and the need for code generation. JSONL hits the sweet spot of simplicity, tooling support, and real-time streaming capability.

The cost-benefit analysis strongly favors JSONL mode. Implementation cost is approximately 40 developer hours: 8 hours for event type design, 12 hours for serialization and emission logic, 10 hours for secret redaction, 10 hours for testing. Maintenance cost is low—event schemas are versioned and stable, requiring updates only when new event types are added (estimated 2-3 times per year, 4 hours each). Benefit accrues immediately upon deployment: CI/CD automation saves $120,000 annually, monitoring reduces incident response time by 80%, and audit trail compliance eliminates manual log collection (saving 20 hours per compliance cycle). ROI is achieved within 3 weeks of deployment.

Performance characteristics are well-understood. Serialization via `System.Text.Json` averages 0.4ms per event on a modest laptop (Core i5, 16GB RAM). Event emission (write + flush) adds 0.1ms, totaling 0.5ms per event. At this rate, Acode can emit 2,000 events per second, far exceeding realistic workloads (typical runs emit 10-50 events per second). Memory overhead is minimal—each event allocates ~5KB during serialization, which is immediately garbage collected. A long-running operation emitting 1,000 events consumes only 5MB of transient memory. The event emitter uses a single-threaded queue to serialize concurrent events, preventing interleaved JSON lines (which would corrupt the stream).

Observability and debugging are first-class concerns. Every event emission is logged to stderr with the event type, event ID, serialization time, and size in bytes. This logging enables performance analysis (identifying slow serialization) and debugging (confirming events were emitted). When serialization fails (e.g., a field contains unserializable data), the error is logged with full context, and a fallback error event is emitted to the stream. This ensures the stream never silently drops events. The `--verbose` flag increases logging detail, including full event JSON in logs (useful for schema debugging).

Backward compatibility is guaranteed within major versions. Schema version 1.x will never remove fields or change field types. New fields are always optional. When schema 2.0 is released, Acode will emit both 1.x and 2.x events for a transition period (e.g., 6 months), giving consumers time to upgrade. The `session_start` event will include `schema_versions_supported: ["1.0.0", "2.0.0"]`, allowing consumers to negotiate their preferred version. This gradual migration strategy prevents breaking existing integrations while enabling evolution.

Security considerations are addressed through redaction, validation, and isolation. All event content passes through `SecretRedactor` before serialization, ensuring API keys, passwords, and tokens never reach stdout. File paths are preserved (not redacted) because they're not secrets—consumers need them for context. Event validation ensures no unserializable data reaches the serializer (preventing exceptions). Isolation via stdout/stderr separation ensures events never mix with diagnostic logs, preventing log injection attacks. The `--json` flag is explicitly required, preventing accidental exposure of verbose output in machine-readable format.

Edge cases handled include concurrent event emission, stream interruption, and malformed input. Concurrent emission (from parallel operations) is serialized via a lock-free queue, ensuring events don't interleave within a line. Stream interruption (e.g., pipe closed) is detected via `IOException`, logged, and results in graceful shutdown—no crashes. Malformed input (e.g., a component emits an event with missing required fields) triggers validation errors logged to stderr and a fallback error event emitted to the stream. These edge cases are covered by integration tests simulating real-world failure scenarios.

User experience is prioritized. The `--json` flag is mnemonic and matches industry standards (e.g., `curl --json`, `aws cli --output json`). Environment variable `ACODE_JSON=1` follows the `ACODE_` prefix convention. Error messages when JSONL mode fails are actionable: "Event serialization failed for ProgressEvent: field 'step' is required. Check event emission code." Examples in documentation use realistic scenarios (CI/CD, monitoring) with copy-paste commands. Troubleshooting section covers common issues (invalid JSON, missing events) with concrete solutions.

Integration testing validates end-to-end workflows. The test suite includes a full agent run with `--json`, capturing stdout and parsing every line with `jq`. If any line fails to parse, the test fails. The test also validates event ordering (session_start first, session_end last), required fields (all events have type and timestamp), and schema compliance (schema version is 1.0.0). Performance tests emit 1,000 events and assert total time is under 1 second. Regression tests ensure schema changes don't break existing consumers—when a new field is added, the test verifies old consumers still parse events successfully.

Deployment strategy is conservative. JSONL mode ships in Acode 1.1.0, initially as experimental (opt-in with `--json`). After 4 weeks of user testing, it becomes stable in 1.2.0. Schema version 1.0.0 is locked at this point—no breaking changes. Future schema enhancements follow the versioning protocol: minor version for new fields, major version for breaking changes. The roadmap includes schema 1.1.0 (adding `parent_event_id` for hierarchical events) and schema 2.0.0 (renaming `event_id` to `id` for consistency). Each schema change is communicated via release notes, migration guide, and deprecation warnings.

This comprehensive design ensures JSONL event stream mode is robust, performant, secure, and user-friendly. It enables the automation and integration scenarios that make Acode indispensable in modern development workflows.

---

## Use Cases

### Use Case 1: DevBot Integrates Acode into CI/CD Pipeline

**Persona:** DevBot is a junior developer on a team that recently adopted Acode for automated code reviews and refactoring. The team's CI/CD pipeline runs on GitHub Actions, and DevBot is tasked with integrating Acode to automatically validate pull requests before merge.

**Before JSONL Mode:** DevBot attempts to parse Acode's human-readable output using regex and string parsing. This approach is fragile—whenever Acode updates its output format, the CI integration breaks. DevBot spends 4 hours debugging why the pipeline suddenly stopped detecting errors, only to discover that a new version of Acode added an extra line to the progress output, breaking the regex pattern. The team experiences a failed deployment because an error went undetected, costing 3 hours of rollback and hotfix work.

**After JSONL Mode:** DevBot enables JSONL mode with `--json` flag in the GitHub Actions workflow. The CI script pipes Acode output to `jq` to filter for error events: `acode run --json "Validate PR" | jq -c 'select(.type == "error")'`. If any error events appear, the script exits with code 1, failing the build. When session ends, the script checks `exit_code` in the `session_end` event to determine overall success. This integration is robust—output format changes don't break parsing because the event schema is versioned and stable. DevBot completes the integration in 45 minutes instead of multiple days of regex debugging. Over the next 6 months, the pipeline catches 23 issues before they reach production, preventing an estimated $85,000 in incident costs.

**Metrics:** Integration time reduced from 4+ days to 45 minutes (83% reduction). Failed deployments reduced from 2 per quarter to 0. Incident prevention value: $85,000 over 6 months.

### Use Case 2: Jordan Builds Real-Time Monitoring Dashboard

**Persona:** Jordan is a senior developer responsible for infrastructure and observability. The team is running Acode extensively for automated refactoring tasks, and Jordan needs to build a dashboard showing real-time agent status, token consumption, and error rates across all active runs.

**Before JSONL Mode:** Jordan attempts to scrape Acode's human-readable output and parse it into structured data. This requires complex regex patterns and heuristics to extract information from free-form text. The dashboard frequently shows incorrect data because the parsing logic fails on edge cases (e.g., file paths containing colons, error messages with embedded newlines). Jordan spends 2 weeks building and debugging the scraper, achieving only 70% accuracy. When Acode updates its output format, the dashboard breaks entirely, requiring another week of fixes. Team members lose trust in the dashboard and stop using it.

**After JSONL Mode:** Jordan rewrites the dashboard to consume JSONL events. A Python backend subscribes to Acode event streams via `subprocess.Popen`, parsing each line with `json.loads()`. The dashboard subscribes to specific event types: `model_event` for token consumption, `error` for error rates, `progress` for current step, `status` for state transitions. Jordan filters events by type and aggregates metrics in real-time. For example, token consumption is computed by summing `tokens_used` from all `model_event` events. Error rate is computed by counting `error` events per session. The dashboard is production-ready in 2 days instead of 2 weeks, with 100% accuracy because events are already structured. When Acode adds new event types, the dashboard gracefully ignores them (backward compatibility). Team members rely on the dashboard daily for operational visibility, and it becomes the primary tool for capacity planning and cost optimization.

**Metrics:** Development time reduced from 2 weeks to 2 days (86% reduction). Data accuracy improved from 70% to 100%. Dashboard adoption rate: 100% of team (18 engineers). Capacity planning efficiency increased by 50% (fewer over-provisioned resources).

### Use Case 3: Alex Implements External Approval Workflow via Slack

**Persona:** Alex is a DevOps engineer responsible for deployment automation. The team wants to use Acode for automated deployments, but high-risk actions (e.g., database schema changes, production deployments) require approval from a senior engineer. Alex needs to route approval requests to Slack for human review.

**Before JSONL Mode:** Alex attempts to integrate Acode with Slack using a wrapper script that detects approval prompts in stdout and posts them to Slack. This approach is unreliable because approval prompts are free-form text, making detection error-prone. The script uses regex to find prompts like "Approve action?", but variations in phrasing cause false positives and false negatives. When an approval request is missed, the deployment stalls indefinitely until someone manually checks the terminal. When a false positive occurs, the script posts non-approval messages to Slack, creating noise and confusion. Alex spends 5 days building and debugging this integration, achieving only 80% reliability.

**After JSONL Mode:** Alex uses JSONL mode to reliably detect approval requests. The integration script runs `acode deploy --json "Production deployment"` and parses events line-by-line. When an `approval_request` event appears, the script extracts `action_type`, `context`, `risk_level`, and posts a Slack message with approve/reject buttons. The Slack bot includes all context from the event (affected files, risk level, proposed changes), enabling informed decision-making. When a user clicks "approve", the Slack bot writes `approve` to Acode's stdin, unblocking the deployment. When "reject" is clicked, the bot writes `reject`, causing Acode to skip the action. This integration is 100% reliable because approval requests are explicitly typed events, not heuristically detected text. Alex completes the integration in 1 day instead of 5. Over the next 3 months, the team processes 47 approval requests via Slack with zero missed prompts and zero false positives.

**Metrics:** Development time reduced from 5 days to 1 day (80% reduction). Approval reliability improved from 80% to 100%. Approval response time reduced from 45 minutes (manual terminal checks) to 5 minutes (Slack notifications). Team satisfaction increased—no more stalled deployments or missed approvals.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| JSONL | JSON Lines - newline-delimited JSON format where each line is a valid JSON object |
| Event | Single JSON object in the stream representing a discrete occurrence |
| Event Type | Category identifier for events (e.g., progress, error, action) |
| Event Schema | Structure definition for event type specifying required and optional fields |
| Schema Version | Version number for event formats following semantic versioning |
| Line Buffering | Flushing output buffer after each newline for real-time streaming |
| Event Consumer | System or script parsing the event stream (e.g., CI/CD pipeline, dashboard) |
| Event Producer | Component emitting events (e.g., CLI commands, model operations) |
| Event Router | Component directing events based on type or destination |
| Stdout | Standard output stream (events only in JSONL mode) |
| Stderr | Standard error stream (diagnostic logs in JSONL mode) |
| ISO 8601 | Standard timestamp format (e.g., 2024-01-15T10:30:00.123Z) |
| UTC | Coordinated Universal Time (timezone-neutral timestamps) |
| Pipe Detection | Determining if output is piped vs terminal using Console.IsOutputRedirected |
| Flush | Force output buffer contents to stream immediately |
| Event ID | Unique identifier for each event within a session (e.g., evt_001) |
| Correlation ID | Identifier linking related events (e.g., request and response pairs) |
| Run ID | Unique identifier for an entire CLI session |
| Secret Redaction | Process of removing or masking sensitive data before event emission |
| Event Emitter | Component responsible for serializing and writing events to stdout |

---

## Out of Scope

The following items are explicitly excluded from Task 010.b:

- **Binary output formats** - JSONL only
- **WebSocket streaming** - stdout only
- **GraphQL interface** - CLI only
- **Event persistence** - Consumer responsibility
- **Event replay** - Consumer responsibility
- **Event aggregation** - Consumer responsibility
- **Custom serializers** - Standard JSON only
- **Compression** - Plain text only
- **Encryption** - Plain text output
- **Multi-language messages** - English only

---

## Assumptions

### Technical Assumptions

- ASM-001: System.Text.Json is available for JSON serialization with high performance
- ASM-002: Console output can be reliably flushed after each line for real-time streaming
- ASM-003: Stdout and stderr can be independently controlled and buffered
- ASM-004: Console.IsOutputRedirected accurately detects piped output
- ASM-005: JSON serialization can handle all .NET types used in events
- ASM-006: Timestamps can be generated with millisecond precision

### Environmental Assumptions

- ASM-007: Consumers can parse JSONL format (one JSON object per line)
- ASM-008: Pipe buffers are large enough for typical event sizes (< 4KB per event)
- ASM-009: Consumers process events in real-time or have sufficient buffering
- ASM-010: UTF-8 encoding is supported by all consumers
- ASM-011: Newline character is LF (\n) for cross-platform compatibility

### Dependency Assumptions

- ASM-012: Task 010 CLI Framework provides the --json flag handling
- ASM-013: Task 010.a command routing is complete for command identification
- ASM-014: All subsystems (model, file, orchestrator) emit events through a central event bus
- ASM-015: Event schemas are defined and versioned before implementation

### Consumer Assumptions

- ASM-016: Consumers understand JSON Lines format (vs. JSON array)
- ASM-017: Consumers can handle events arriving out of order in edge cases
- ASM-018: Consumers implement appropriate timeout handling for long operations
- ASM-019: Consumers filter events by type rather than parsing all content
- ASM-020: Consumers expect UTC timestamps and handle timezone conversion

---

## Functional Requirements

### JSONL Output Mode

- FR-001: --json flag MUST enable JSONL mode
- FR-002: ACODE_JSON=1 env MUST enable JSONL mode
- FR-003: Events MUST be written to stdout
- FR-004: Each event MUST be one line
- FR-005: Each line MUST be valid JSON
- FR-006: Lines MUST end with newline
- FR-007: Logs MUST go to stderr in JSONL mode
- FR-008: No non-JSON output on stdout in JSONL mode

### Event Structure

- FR-009: Events MUST have "type" field
- FR-010: Events MUST have "timestamp" field
- FR-011: Timestamps MUST be ISO 8601 UTC
- FR-012: Events MUST have "event_id" field
- FR-013: Event IDs MUST be unique per session
- FR-014: Events MAY have "correlation_id" field
- FR-015: Events MUST include schema version
- FR-016: Schema version MUST use semver

### Event Types

- FR-017: "session_start" for session begin
- FR-018: "session_end" for session complete
- FR-019: "progress" for incremental updates
- FR-020: "status" for state changes
- FR-021: "approval_request" for approval needed
- FR-022: "approval_response" for approval given
- FR-023: "action" for actions taken
- FR-024: "error" for error conditions
- FR-025: "warning" for warning conditions
- FR-026: "model_event" for model operations
- FR-027: "file_event" for file operations

### Session Events

- FR-028: session_start MUST include run_id
- FR-029: session_start MUST include command
- FR-030: session_start MUST include schema_version
- FR-031: session_end MUST include exit_code
- FR-032: session_end MUST include duration_ms
- FR-033: session_end MUST include summary

### Progress Events

- FR-034: progress MUST include current step
- FR-035: progress MUST include total steps if known
- FR-036: progress MAY include percentage
- FR-037: progress MAY include eta_seconds
- FR-038: progress MUST include message

### Status Events

- FR-039: status MUST include previous_state
- FR-040: status MUST include new_state
- FR-041: status MUST include reason

### Approval Events

- FR-042: approval_request MUST include action_type
- FR-043: approval_request MUST include context
- FR-044: approval_request MUST include risk_level
- FR-045: approval_request MUST include options
- FR-046: approval_response MUST include decision
- FR-047: approval_response MUST include source

### Action Events

- FR-048: action MUST include action_type
- FR-049: action MUST include parameters
- FR-050: action MUST include result
- FR-051: action MUST include duration_ms

### Error Events

- FR-052: error MUST include code
- FR-053: error MUST include message
- FR-054: error MUST include component
- FR-055: error MAY include stack_trace (if verbose)
- FR-056: error MAY include remediation

### Model Events

- FR-057: model_event MUST include model_id
- FR-058: model_event MUST include operation
- FR-059: model_event MAY include tokens_used
- FR-060: model_event MAY include latency_ms

### File Events

- FR-061: file_event MUST include operation
- FR-062: file_event MUST include path
- FR-063: file_event MAY include diff (if write)
- FR-064: file_event MUST include result

### Streaming Behavior

- FR-065: Events MUST be flushed immediately
- FR-066: No buffering across events
- FR-067: Stdout MUST be line-buffered
- FR-068: Long-running ops MUST emit progress

### Schema Management

- FR-069: Schema version in every event
- FR-070: Breaking changes = major version bump
- FR-071: New fields = minor version bump
- FR-072: Current schema version: 1.0.0

### Secret Redaction

- FR-073: Secrets MUST be redacted in events
- FR-074: API keys MUST show only last 4 chars
- FR-075: Passwords MUST be replaced with ***
- FR-076: Paths MUST be preserved (not secrets)

### Event Ordering

- FR-077: session_start MUST be first event
- FR-078: session_end MUST be last event
- FR-079: Events MUST have monotonically increasing event_id
- FR-080: Timestamp order SHOULD match event_id order

### Event Correlation

- FR-081: approval_response MUST reference approval_request via correlation_id
- FR-082: error events MAY reference triggering event via correlation_id
- FR-083: Correlation chains MUST be traceable

### Event Validation

- FR-084: Events MUST be validated before emission
- FR-085: Missing required fields MUST trigger validation error
- FR-086: Invalid field types MUST trigger validation error
- FR-087: Validation errors MUST be logged to stderr

### Configuration

- FR-088: Event emission MUST respect config file settings
- FR-089: include_file_content option MUST control file content emission
- FR-090: include_stack_traces option MUST control stack trace emission

### Backwards Compatibility

- FR-091: Schema 1.x MUST NOT remove fields
- FR-092: Schema 1.x MUST NOT change field types
- FR-093: New fields MUST be optional
- FR-094: Deprecated fields MUST emit deprecation warnings

### Output Modes

- FR-095: --json and human-readable MUST be mutually exclusive
- FR-096: Default mode is human-readable
- FR-097: JSONL mode MUST be explicitly enabled
- FR-098: Mode selection MUST be deterministic (no auto-detection)

### Performance

- FR-099: Event emission MUST NOT introduce > 10% overhead to operations
- FR-100: Event queue MUST handle burst of 100 events without blocking

---

## Non-Functional Requirements

### Performance

- NFR-001: Event emission MUST NOT block operations
- NFR-002: Serialization MUST complete < 1ms per event
- NFR-003: Memory per event MUST be < 10KB
- NFR-004: Event throughput MUST support 1000/sec
- NFR-005: Event emission overhead MUST be < 10% of operation time
- NFR-006: Event queue memory MUST be bounded (max 1000 events in queue)

### Reliability

- NFR-007: Partial output MUST be parseable line-by-line
- NFR-008: Truncated lines MUST be detectable (no closing brace)
- NFR-009: Stream interruption MUST NOT corrupt valid lines
- NFR-010: Event emitter MUST handle concurrent emission safely
- NFR-011: Event emitter MUST recover from transient write failures
- NFR-012: Event IDs MUST be unique even in high-concurrency scenarios

### Security

- NFR-013: No secrets in event content (all must be redacted)
- NFR-014: File content MUST be optional (config)
- NFR-015: Stack traces MUST require verbose flag
- NFR-016: Event validation MUST prevent injection attacks
- NFR-017: Redaction MUST be applied before serialization (defense in depth)

### Compatibility

- NFR-018: JSON MUST be RFC 8259 compliant
- NFR-019: Unicode MUST be properly escaped
- NFR-020: All platforms MUST emit identical format (Windows, Linux, macOS)
- NFR-021: Schema version 1.x MUST be parseable by all consumers
- NFR-022: Event format MUST be compatible with jq, Python json module, Node.js JSON.parse

### Observability

- NFR-023: Event emission MUST be logged to stderr
- NFR-024: Serialization errors MUST be logged to stderr
- NFR-025: Performance metrics MUST be available (latency, throughput)
- NFR-026: Event emission failures MUST be counted and reported
- NFR-027: Validation failures MUST be logged with full context

### Usability

- NFR-028: JSONL mode activation MUST be obvious (explicit flag)
- NFR-029: Error messages MUST be actionable
- NFR-030: Documentation MUST include copy-paste examples

---

## User Manual Documentation

### Overview

JSONL event stream mode provides machine-readable output for all Acode CLI operations. Enable this mode for automation, CI/CD integration, and programmatic control.

### Quick Start

```bash
# Enable JSONL mode
$ acode run --json "Add validation"

# Output example (one event per line):
{"type":"session_start","timestamp":"2024-01-15T10:30:00.123Z","event_id":"evt_001","run_id":"abc123","command":"run","schema_version":"1.0.0"}
{"type":"progress","timestamp":"2024-01-15T10:30:01.456Z","event_id":"evt_002","step":1,"total":5,"message":"Analyzing task"}
{"type":"action","timestamp":"2024-01-15T10:30:05.789Z","event_id":"evt_003","action_type":"file_write","path":"src/validation.ts","result":"success"}
{"type":"session_end","timestamp":"2024-01-15T10:30:10.012Z","event_id":"evt_004","exit_code":0,"duration_ms":9889}
```

### Enabling JSONL Mode

```bash
# Via command-line flag
$ acode run --json "task"

# Via environment variable
$ export ACODE_JSON=1
$ acode run "task"

# Combine with other options
$ acode run --json --verbose "task"
```

### Event Types

#### session_start

Emitted when a command begins:

```json
{
  "type": "session_start",
  "timestamp": "2024-01-15T10:30:00.123Z",
  "event_id": "evt_001",
  "run_id": "abc123",
  "command": "run",
  "args": ["Add validation"],
  "schema_version": "1.0.0"
}
```

#### session_end

Emitted when a command completes:

```json
{
  "type": "session_end",
  "timestamp": "2024-01-15T10:30:10.012Z",
  "event_id": "evt_099",
  "exit_code": 0,
  "duration_ms": 9889,
  "summary": {
    "actions_taken": 5,
    "files_modified": 3,
    "approvals_requested": 2
  }
}
```

#### progress

Emitted for long-running operations:

```json
{
  "type": "progress",
  "timestamp": "2024-01-15T10:30:01.456Z",
  "event_id": "evt_002",
  "step": 2,
  "total": 5,
  "percentage": 40,
  "message": "Planning actions",
  "eta_seconds": 15
}
```

#### status

Emitted on state changes:

```json
{
  "type": "status",
  "timestamp": "2024-01-15T10:30:02.789Z",
  "event_id": "evt_003",
  "previous_state": "PLANNING",
  "new_state": "EXECUTING",
  "reason": "Plan approved"
}
```

#### approval_request

Emitted when approval is needed:

```json
{
  "type": "approval_request",
  "timestamp": "2024-01-15T10:30:03.012Z",
  "event_id": "evt_004",
  "action_type": "file_write",
  "context": {
    "file": "src/config.ts",
    "changes": "+15 -3 lines"
  },
  "risk_level": "medium",
  "options": ["approve", "reject", "modify"]
}
```

#### approval_response

Emitted when approval is given:

```json
{
  "type": "approval_response",
  "timestamp": "2024-01-15T10:30:05.345Z",
  "event_id": "evt_005",
  "correlation_id": "evt_004",
  "decision": "approve",
  "source": "cli_prompt"
}
```

#### action

Emitted for each action taken:

```json
{
  "type": "action",
  "timestamp": "2024-01-15T10:30:06.678Z",
  "event_id": "evt_006",
  "action_type": "file_write",
  "parameters": {
    "path": "src/validation.ts"
  },
  "result": "success",
  "duration_ms": 45
}
```

#### error

Emitted on errors:

```json
{
  "type": "error",
  "timestamp": "2024-01-15T10:30:07.901Z",
  "event_id": "evt_007",
  "code": "ACODE-FILE-001",
  "message": "File not found: src/missing.ts",
  "component": "FileSystem",
  "remediation": "Check file path and permissions"
}
```

#### warning

Emitted on warnings:

```json
{
  "type": "warning",
  "timestamp": "2024-01-15T10:30:08.234Z",
  "event_id": "evt_008",
  "code": "ACODE-WARN-001",
  "message": "Large file detected, may be slow",
  "component": "FileSystem"
}
```

#### model_event

Emitted for model operations:

```json
{
  "type": "model_event",
  "timestamp": "2024-01-15T10:30:09.567Z",
  "event_id": "evt_009",
  "model_id": "llama3.2:7b",
  "operation": "inference",
  "tokens_used": 1500,
  "latency_ms": 2340
}
```

#### file_event

Emitted for file operations:

```json
{
  "type": "file_event",
  "timestamp": "2024-01-15T10:30:10.890Z",
  "event_id": "evt_010",
  "operation": "write",
  "path": "src/validation.ts",
  "result": "success",
  "diff": {
    "lines_added": 15,
    "lines_removed": 3
  }
}
```

### Parsing JSONL

#### Bash with jq

```bash
# Filter specific event types
$ acode run --json "task" | jq -c 'select(.type == "progress")'

# Extract error messages
$ acode run --json "task" | jq -c 'select(.type == "error") | .message'

# Get final exit code
$ acode run --json "task" | jq -c 'select(.type == "session_end") | .exit_code'
```

#### Python

```python
import json
import subprocess
import sys

proc = subprocess.Popen(
    ["acode", "run", "--json", "Add validation"],
    stdout=subprocess.PIPE,
    text=True
)

for line in proc.stdout:
    event = json.loads(line)
    
    if event["type"] == "progress":
        print(f"Progress: {event['percentage']}%")
    
    elif event["type"] == "error":
        print(f"Error: {event['message']}", file=sys.stderr)
    
    elif event["type"] == "session_end":
        sys.exit(event["exit_code"])
```

#### Node.js

```javascript
const { spawn } = require('child_process');
const readline = require('readline');

const proc = spawn('acode', ['run', '--json', 'Add validation']);

const rl = readline.createInterface({
  input: proc.stdout,
  crlfDelay: Infinity
});

rl.on('line', (line) => {
  const event = JSON.parse(line);
  
  switch (event.type) {
    case 'progress':
      console.log(`Progress: ${event.percentage}%`);
      break;
    case 'error':
      console.error(`Error: ${event.message}`);
      break;
    case 'session_end':
      process.exit(event.exit_code);
  }
});
```

### Correlation IDs

Events can be correlated using `correlation_id`:

```bash
# Find approval response for a specific request
$ acode run --json "task" | jq -c '
  select(.type == "approval_request" or .type == "approval_response")
  | {type, event_id, correlation_id}
'
```

### Schema Versioning

Event schemas are versioned. Check `schema_version` for compatibility:

```bash
$ acode run --json "task" | jq -c 'select(.type == "session_start") | .schema_version'
# Output: "1.0.0"
```

### Stdout vs Stderr

In JSONL mode:
- **stdout**: JSONL events only
- **stderr**: Human-readable logs

```bash
# Events to file, logs to terminal
$ acode run --json "task" > events.jsonl 2>&1

# Events to parser, suppress logs
$ acode run --json --quiet "task" | my-parser

# Events to parser, logs to file
$ acode run --json "task" 2>debug.log | my-parser
```

### Configuration

#### Config File

```yaml
# .agent/config.yml
output:
  jsonl:
    include_file_content: false   # Omit file content from events
    include_stack_traces: false   # Omit stack traces
    pretty_print: false           # Single-line events (default)
```

#### Environment Variables

```bash
# Enable JSONL mode
export ACODE_JSON=1

# Include file content in events
export ACODE_JSONL_INCLUDE_CONTENT=1
```

### Best Practices

1. **Use jq for exploration**: Learn event structure interactively
2. **Filter early**: Process only needed event types
3. **Handle errors gracefully**: Check for error events
4. **Check schema version**: Ensure compatibility
5. **Use correlation IDs**: Track related events
6. **Preserve stderr**: Don't discard diagnostic logs

### Troubleshooting

#### Issue 1: Invalid JSON Parse Errors

**Symptoms:**
- JSON parser throws "Unexpected token" error
- `jq` fails with "parse error: Invalid numeric literal"
- Python `json.loads()` raises `JSONDecodeError`

**Possible Causes:**
1. Mixed mode output (JSONL and human-readable text on same stream)
2. Truncated event (stream interrupted mid-line)
3. Interleaved stderr and stdout (not properly separated)
4. Event contains unescaped newline or quote character

**Solution:**
```bash
# Ensure pure JSONL with no stderr
$ acode run --json --quiet "task" 2>/dev/null | jq -c .

# Check for mixed output
$ acode run --json "task" | head -10  # Verify every line is JSON

# Validate each line individually
$ acode run --json "task" | while read line; do echo "$line" | jq . > /dev/null || echo "Invalid: $line"; done
```

#### Issue 2: Missing Expected Events

**Symptoms:**
- Expected `progress` events not appearing
- `model_event` events missing despite model operations
- `approval_request` events not emitted when approval needed

**Possible Causes:**
1. Events filtered by verbosity level (default is non-verbose)
2. Events routed to stderr instead of stdout (configuration error)
3. Buffering delay (events queued but not flushed)
4. Event type not implemented in current version

**Solution:**
```bash
# Enable verbose for all events
$ acode run --json --verbose "task"

# Check stderr for misrouted events
$ acode run --json "task" 2>&1 | grep "type"

# Force immediate flush (if buffering suspected)
$ stdbuf -oL acode run --json "task"
```

#### Issue 3: Events Out of Order

**Symptoms:**
- Timestamps not strictly increasing
- `session_end` appears before last `progress` event
- Correlation IDs reference future events

**Explanation:**
Events are emitted from concurrent operations (parallel file operations, async model calls). Timestamp order reflects real-world timing, which may differ from logical order due to concurrency.

**Solution:**
Use `event_id` for logical ordering (monotonically increasing) and `timestamp` for real-world timing. When reconstructing operation timeline, sort by `event_id` for logical flow or `timestamp` for actual chronology.

```bash
# Sort by event_id for logical order
$ acode run --json "task" | jq -s 'sort_by(.event_id)'

# Sort by timestamp for chronological order
$ acode run --json "task" | jq -s 'sort_by(.timestamp)'
```

#### Issue 4: Secret Redaction Not Applied

**Symptoms:**
- API keys visible in full in events
- Passwords appearing in error messages
- Tokens visible in action events

**Possible Causes:**
1. Secret pattern not recognized by redactor
2. Secret passed in unexpected field (not covered by redaction rules)
3. Redaction disabled in configuration

**Solution:**
```bash
# Verify redaction config
$ cat .agent/config.yml | grep -A5 "redaction"

# Check event for secrets manually
$ acode run --json "task" | jq -c 'select(.type == "error") | .message'

# Report unredacted secret as bug with pattern
```

#### Issue 5: Event Stream Stops Mid-Run

**Symptoms:**
- Events stop appearing partway through operation
- No `session_end` event emitted
- Process still running but no output

**Possible Causes:**
1. Pipe consumer closed (broken pipe)
2. Event queue full (backpressure)
3. Serialization deadlock (rare)

**Solution:**
```bash
# Check for broken pipe errors in stderr
$ acode run --json "task" 2>err.log
$ cat err.log | grep "Broken pipe"

# Increase pipe buffer size
$ ulimit -p 2048
$ acode run --json "task" | consumer

# Run without pipe to isolate issue
$ acode run --json "task" > events.jsonl
$ cat events.jsonl | consumer
```

#### Issue 6: Schema Version Mismatch

**Symptoms:**
- Consumer expects field that doesn't exist
- Event has extra fields consumer doesn't recognize
- "Schema version unsupported" error from consumer

**Possible Causes:**
1. Acode version newer than consumer expects
2. Consumer expects major version 2.x but Acode emits 1.x
3. Consumer uses strict validation (no extra fields allowed)

**Solution:**
```bash
# Check schema version in session_start
$ acode run --json "task" | head -1 | jq '.schema_version'

# Update Acode to match consumer expectations
$ acode version  # Check current version
$ acode upgrade  # Upgrade if needed

# Configure consumer for flexible parsing (ignore unknown fields)
```

---

## Acceptance Criteria

### JSONL Mode Activation

- [ ] AC-001: --json flag enables JSONL mode
- [ ] AC-002: ACODE_JSON=1 enables JSONL mode
- [ ] AC-003: Events go to stdout
- [ ] AC-004: One event per line
- [ ] AC-005: Each line is valid JSON
- [ ] AC-006: Lines end with newline
- [ ] AC-007: Logs go to stderr
- [ ] AC-008: No non-JSON on stdout

### Event Structure

- [ ] AC-009: "type" field present
- [ ] AC-010: "timestamp" field present
- [ ] AC-011: ISO 8601 UTC format
- [ ] AC-012: "event_id" field present
- [ ] AC-013: Event IDs unique per session
- [ ] AC-014: Schema version present

### Session Events

- [ ] AC-015: session_start includes run_id
- [ ] AC-016: session_start includes command
- [ ] AC-017: session_start includes schema_version
- [ ] AC-018: session_end includes exit_code
- [ ] AC-019: session_end includes duration_ms
- [ ] AC-020: session_end includes summary

### Progress Events

- [ ] AC-021: Includes current step
- [ ] AC-022: Includes total if known
- [ ] AC-023: Includes message
- [ ] AC-024: Emitted for long operations

### Status Events

- [ ] AC-025: Includes previous_state
- [ ] AC-026: Includes new_state
- [ ] AC-027: Includes reason

### Approval Events

- [ ] AC-028: Request includes action_type
- [ ] AC-029: Request includes context
- [ ] AC-030: Request includes risk_level
- [ ] AC-031: Request includes options
- [ ] AC-032: Response includes decision
- [ ] AC-033: Response includes source

### Action Events

- [ ] AC-034: Includes action_type
- [ ] AC-035: Includes parameters
- [ ] AC-036: Includes result
- [ ] AC-037: Includes duration_ms

### Error Events

- [ ] AC-038: Includes code
- [ ] AC-039: Includes message
- [ ] AC-040: Includes component
- [ ] AC-041: Stack trace only if verbose

### Model Events

- [ ] AC-042: Includes model_id
- [ ] AC-043: Includes operation
- [ ] AC-044: Includes tokens if applicable
- [ ] AC-045: Includes latency_ms

### File Events

- [ ] AC-046: Includes operation
- [ ] AC-047: Includes path
- [ ] AC-048: Includes result
- [ ] AC-049: Diff optional

### Streaming

- [ ] AC-050: Events flushed immediately
- [ ] AC-051: No cross-event buffering
- [ ] AC-052: Line-buffered stdout
- [ ] AC-053: Progress for long ops

### Schema

- [ ] AC-054: Version in every event
- [ ] AC-055: Semver format
- [ ] AC-056: Version 1.0.0 current

### Secret Redaction

- [ ] AC-057: Secrets redacted
- [ ] AC-058: API keys last 4 chars only
- [ ] AC-059: Passwords replaced

### Performance

- [ ] AC-060: Emission non-blocking
- [ ] AC-061: Serialization < 1ms
- [ ] AC-062: Memory < 10KB per event
- [ ] AC-063: 1000 events/sec supported

### Compatibility

- [ ] AC-064: RFC 8259 compliant JSON
- [ ] AC-065: Unicode properly escaped
- [ ] AC-066: Cross-platform identical

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/CLI/JSONL/
├── EventSerializerTests.cs
│   ├── Should_Serialize_All_Event_Types()
│   ├── Should_Include_Required_Fields()
│   ├── Should_Generate_Unique_EventIds()
│   ├── Should_Format_Timestamps_ISO8601()
│   └── Should_Redact_Secrets()
│
├── EventEmitterTests.cs
│   ├── Should_Write_To_Stdout()
│   ├── Should_Flush_After_Each_Event()
│   ├── Should_Emit_Newline()
│   └── Should_Handle_Concurrent_Events()
│
├── JSONLModeTests.cs
│   ├── Should_Enable_Via_Flag()
│   ├── Should_Enable_Via_EnvVar()
│   └── Should_Not_Affect_Stderr()
│
└── SecretRedactionTests.cs
    ├── Should_Redact_API_Keys()
    ├── Should_Redact_Passwords()
    └── Should_Preserve_Paths()
```

### Integration Tests

```
Tests/Integration/CLI/JSONL/
├── EventStreamTests.cs
│   ├── Should_Emit_Session_Events()
│   ├── Should_Emit_Progress_Events()
│   ├── Should_Emit_Error_Events()
│   └── Should_Correlate_Approval_Events()
│
└── ParsingTests.cs
    ├── Should_Be_Parseable_With_jq()
    ├── Should_Be_Parseable_With_Python()
    └── Should_Handle_Concurrent_Events()
```

### E2E Tests

```
Tests/E2E/CLI/JSONL/
├── FullRunTests.cs
│   ├── Should_Produce_Complete_Stream()
│   ├── Should_Handle_Errors_Gracefully()
│   └── Should_Work_With_Pipes()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| Event serialization | 0.5ms | 1ms |
| Event emission | 0.1ms | 0.5ms |
| Memory per event | 5KB | 10KB |
| Events per second | 2000 | 1000 min |

### Regression Tests

- Event format after schema update
- Performance after field additions
- Compatibility after serializer change

---

## User Verification Steps

### Scenario 1: Enable JSONL

1. Run `acode run --json "test"`
2. Verify: Output is JSONL
3. Verify: Each line parses as JSON

### Scenario 2: Event Types

1. Run `acode run --json "task" | jq -c .type`
2. Verify: session_start first
3. Verify: session_end last

### Scenario 3: Parse with jq

1. Run `acode run --json "task" | jq -c 'select(.type=="progress")'`
2. Verify: Only progress events
3. Verify: Valid JSON

### Scenario 4: Session Events

1. Run `acode run --json "task"`
2. Verify: session_start has run_id
3. Verify: session_end has exit_code

### Scenario 5: Error Event

1. Cause an error
2. Verify: error event emitted
3. Verify: Contains code and message

### Scenario 6: Progress Events

1. Run long operation with --json
2. Verify: Progress events emitted
3. Verify: Step/total included

### Scenario 7: Secret Redaction

1. Run with API key in config
2. Verify: Key not in events
3. Verify: Redacted form only

### Scenario 8: Stderr Separation

1. Run `acode run --json "task" 2>log.txt`
2. Verify: stdout is pure JSONL
3. Verify: stderr has logs

### Scenario 9: Environment Variable

1. Set ACODE_JSON=1
2. Run `acode run "task"`
3. Verify: JSONL output

### Scenario 10: Schema Version

1. Run `acode run --json "task"`
2. Verify: schema_version in session_start
3. Verify: Format is semver

### Scenario 11: Correlation IDs

1. Run operation requiring approval
2. Find approval_request event_id
3. Verify: approval_response has matching correlation_id

### Scenario 12: Concurrent Events

1. Run operation with parallel work
2. Parse all events
3. Verify: All events valid JSON

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.CLI/
├── JSONL/
│   ├── IEventEmitter.cs
│   ├── EventEmitter.cs
│   ├── IEventSerializer.cs
│   ├── EventSerializer.cs
│   ├── EventIdGenerator.cs
│   └── SecretRedactor.cs
│
├── Events/
│   ├── BaseEvent.cs
│   ├── SessionStartEvent.cs
│   ├── SessionEndEvent.cs
│   ├── ProgressEvent.cs
│   ├── StatusEvent.cs
│   ├── ApprovalRequestEvent.cs
│   ├── ApprovalResponseEvent.cs
│   ├── ActionEvent.cs
│   ├── ErrorEvent.cs
│   ├── WarningEvent.cs
│   ├── ModelEvent.cs
│   └── FileEvent.cs
│
└── Output/
    ├── JSONLOutputFormatter.cs
    └── OutputStreamManager.cs
```

### BaseEvent

```csharp
namespace AgenticCoder.CLI.Events;

public abstract record BaseEvent
{
    public required string Type { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string EventId { get; init; }
    public string? CorrelationId { get; init; }
    public string SchemaVersion => "1.0.0";
}
```

### Event Types

```csharp
public sealed record SessionStartEvent : BaseEvent
{
    public required string RunId { get; init; }
    public required string Command { get; init; }
    public IReadOnlyList<string>? Args { get; init; }
}

public sealed record SessionEndEvent : BaseEvent
{
    public required int ExitCode { get; init; }
    public required long DurationMs { get; init; }
    public required SessionSummary Summary { get; init; }
}

public sealed record ProgressEvent : BaseEvent
{
    public required int Step { get; init; }
    public int? Total { get; init; }
    public int? Percentage { get; init; }
    public int? EtaSeconds { get; init; }
    public required string Message { get; init; }
}

public sealed record ErrorEvent : BaseEvent
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public required string Component { get; init; }
    public string? StackTrace { get; init; }
    public string? Remediation { get; init; }
}
```

### IEventEmitter

```csharp
namespace AgenticCoder.CLI.JSONL;

public interface IEventEmitter
{
    void Emit(BaseEvent @event);
    void Configure(EventEmitterOptions options);
}

public sealed record EventEmitterOptions(
    bool IncludeFileContent = false,
    bool IncludeStackTraces = false,
    bool PrettyPrint = false);
```

### IEventSerializer

```csharp
namespace AgenticCoder.CLI.JSONL;

public interface IEventSerializer
{
    string Serialize(BaseEvent @event);
}
```

### EventIdGenerator

```csharp
namespace AgenticCoder.CLI.JSONL;

public sealed class EventIdGenerator
{
    private int _counter;
    private readonly string _prefix;
    
    public EventIdGenerator(string? prefix = "evt")
    {
        _prefix = prefix ?? "evt";
    }
    
    public string Next()
    {
        var count = Interlocked.Increment(ref _counter);
        return $"{_prefix}_{count:D3}";
    }
}
```

### SecretRedactor

```csharp
namespace AgenticCoder.CLI.JSONL;

public sealed class SecretRedactor
{
    public string Redact(string value, string type);
    public bool IsSecret(string key);
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-JSONL-001 | Serialization failed |
| ACODE-JSONL-002 | Event emission failed |
| ACODE-JSONL-003 | Invalid event type |

### Logging Fields

```json
{
  "event": "event_emitted",
  "event_type": "progress",
  "event_id": "evt_001",
  "serialization_ms": 0.5,
  "size_bytes": 256
}
```

### Implementation Checklist

1. [ ] Create BaseEvent abstract record
2. [ ] Create all event type records
3. [ ] Implement EventIdGenerator
4. [ ] Implement EventSerializer
5. [ ] Implement SecretRedactor
6. [ ] Implement EventEmitter
7. [ ] Add stdout line buffering
8. [ ] Implement OutputStreamManager
9. [ ] Add --json flag handling
10. [ ] Add ACODE_JSON env handling
11. [ ] Write serialization unit tests
12. [ ] Write emission unit tests
13. [ ] Write redaction tests
14. [ ] Write integration tests
15. [ ] Add performance benchmarks

### Validation Checklist Before Merge

- [ ] All event types serialize correctly
- [ ] Event IDs unique per session
- [ ] Timestamps are ISO 8601 UTC
- [ ] Schema version in all events
- [ ] Secrets properly redacted
- [ ] Events flush immediately
- [ ] Serialization < 1ms
- [ ] Parseable by jq
- [ ] Unit test coverage > 90%

### Rollout Plan

1. **Phase 1:** Event types and serialization
2. **Phase 2:** Event emitter and buffering
3. **Phase 3:** Secret redaction
4. **Phase 4:** Integration with commands
5. **Phase 5:** Performance tuning
6. **Phase 6:** Documentation and examples

---

**End of Task 010.b Specification**