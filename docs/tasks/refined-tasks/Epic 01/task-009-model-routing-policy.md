# Task 009: Model Routing Policy

**Priority:** P1 – High Priority  
**Tier:** Core Infrastructure  
**Complexity:** 21 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 004 (Model Provider Interface), Task 005 (Ollama Provider), Task 006 (vLLM Provider), Task 002 (.agent/config.yml)  

---

## Description

### Executive Summary

Task 009 implements the Model Routing Policy system, a critical intelligence layer that determines which model handles which type of task within the agentic workflow. This system enables optimal resource utilization by matching task requirements with appropriate model capabilities, directly addressing the fundamental challenge of local AI development: balancing output quality against computational cost and latency constraints.

The routing policy delivers measurable business value through intelligent model selection. By routing simple tasks to efficient models and complex tasks to powerful models, organizations can reduce inference costs while maintaining quality standards. In a typical enterprise coding workflow, routing policy can reduce total inference time by forty to sixty percent compared to using a single large model for all operations, while preserving ninety-five percent quality metrics.

Model routing represents a paradigm shift from monolithic model usage to heterogeneous model orchestration. Traditional AI workflows use one model for all tasks, forcing users to choose between speed and quality. The routing policy eliminates this compromise by enabling role-specific model assignment, complexity-aware selection, and automatic fallback handling. This architecture mirrors how human teams delegate work based on skill requirements—senior architects for design decisions, efficient implementers for routine coding, critical reviewers for quality assurance.

### Return on Investment Analysis

The Model Routing Policy delivers quantifiable cost savings and performance improvements for local AI deployments:

**Infrastructure Cost Reduction:** Using a 70B parameter model for all tasks requires sustained sixteen gigabyte GPU memory allocation. Routing allows degradation to 7B models for simple tasks (variable renaming, documentation updates, simple refactors), reducing memory footprint to four gigabytes during these operations. In a typical eight-hour coding session with sixty percent simple tasks and forty percent complex tasks, this reduces average GPU memory utilization from sixteen gigabytes to approximately eight gigabytes, enabling deployment on consumer hardware that costs four hundred to six hundred dollars less than professional GPUs.

**Latency Optimization:** A 70B model generates tokens at approximately fifteen to twenty tokens per second on mid-range consumer GPUs. A 7B model generates sixty to eighty tokens per second. For a coder role task generating five hundred tokens, routing to the 7B model reduces response time from twenty-five to thirty-three seconds to six to eight seconds—a seventy-five percent improvement. Over one hundred coding interactions per day, this saves approximately forty-five minutes of waiting time, representing a twelve percent productivity gain.

**Throughput Maximization:** By freeing GPU memory during simple tasks, routing enables parallel execution of multiple agent workflows. A system with thirty-two gigabytes total VRAM can run two 70B inference processes simultaneously. With routing, the same system can run one 70B process (complex tasks) plus four 7B processes (simple tasks) concurrently, increasing total throughput capacity by one hundred fifty percent.

**Energy Efficiency:** Large model inference consumes approximately two hundred fifty to three hundred fifty watts. Small model inference consumes sixty to eighty watts. Routing reduces average power consumption per task from approximately three hundred watts to approximately one hundred fifty watts (assuming sixty percent small model usage), cutting electricity costs by fifty percent and reducing thermal management requirements.

**Total Cost of Ownership:** For a development team running local AI infrastructure at scale (ten developers, eight hours per day, five days per week), routing policy can reduce annual GPU infrastructure costs from approximately twenty-five thousand dollars (ten high-end GPUs at twenty-five hundred dollars each) to approximately fifteen thousand dollars (ten mid-range GPUs at fifteen hundred dollars each), while maintaining equivalent quality outcomes. This ten thousand dollar annual saving pays for dedicated MLOps engineering time to optimize routing heuristics.

### Technical Architecture

The Model Routing Policy system consists of five core architectural components that work together to deliver intelligent model selection:

**Policy Engine (Application Layer):** The IRoutingPolicy interface defines the contract for model selection decisions. This interface accepts a routing request containing the agent role (planner, coder, reviewer), task context (complexity indicators, operating mode constraints), and user preferences (overrides, cost thresholds). The policy engine evaluates these inputs against configured routing strategies and returns a RoutingDecision containing the selected model identifier, fallback status, and decision reasoning. The policy engine is stateless and deterministic—identical inputs always produce identical outputs, enabling predictable behavior and simplified testing.

**Strategy Implementations (Infrastructure Layer):** Three concrete routing strategies implement the IRoutingPolicy interface. The SingleModelStrategy routes all requests to one configured model, providing simplicity for users with homogeneous workloads or limited GPU resources. The RoleBasedStrategy maintains a role-to-model mapping, enabling specialized model assignment per agent role—large models for planning and review, fast models for coding. The AdaptiveStrategy (future enhancement) analyzes task complexity indicators and dynamically selects models based on estimated difficulty, token budget, and latency requirements. All strategies share common fallback handling logic through composition.

**Model Registry (Infrastructure Layer):** The ModelRegistry maintains the canonical list of available models, their capabilities, current availability status, and performance characteristics. The registry integrates with model provider interfaces (Task 004) to query model availability in real-time. It tracks which models are currently loaded, which providers serve each model, and which models meet operating mode constraints (local-only, air-gapped). The registry exposes methods for availability checking, capability querying, and model enumeration. It implements caching with five-second time-to-live to minimize provider query overhead while maintaining reasonably fresh availability data.

**Fallback Handler (Infrastructure Layer):** The FallbackHandler implements escalation logic when primary model selection fails due to unavailability. It accepts a fallback chain (ordered list of model identifiers) and sequentially checks availability until finding a viable alternative. The handler logs each fallback attempt, tracks fallback frequency metrics, and fails gracefully with actionable error messages when all alternatives are exhausted. The fallback handler respects operating mode constraints—it will not escalate from a local model to a cloud model in local-only mode, even if the cloud model is technically available.

**Configuration Resolver (Infrastructure Layer):** The ConfigurationResolver reads routing configuration from the .agent/config.yml file and constructs the appropriate strategy implementation. It validates configuration schema, applies default values, and fails fast with descriptive errors when configuration is invalid. The resolver supports hot-reloading—when configuration changes, the next routing request uses the updated configuration without requiring process restart. This enables live tuning during development.

### Multi-Model Strategy Explanation

The multi-model approach fundamentally changes how developers interact with local AI systems by eliminating the forced tradeoff between quality and performance. Traditional single-model deployments require choosing one model that balances all competing concerns—a 70B model provides excellent quality but slow response times, a 7B model provides fast responses but occasionally poor quality. Neither choice is optimal for heterogeneous workloads.

Multi-model routing implements specialization by task type. The planner role requires strong reasoning capabilities, broad knowledge, and multi-step planning skills. These capabilities correlate strongly with model parameter count—70B models significantly outperform 7B models on planning benchmarks. Therefore, routing policy assigns large models to planner roles by default. The performance cost is acceptable because planning happens once per workflow, consuming a small percentage of total inference budget.

The coder role requires precise instruction following, code syntax knowledge, and pattern matching. These capabilities plateau at moderate parameter counts—13B models perform within five percent of 70B models on coding benchmarks. Therefore, routing policy assigns medium or small models to coder roles by default. The quality difference is negligible, but the performance improvement is substantial. Code generation tasks dominate inference volume (fifty to seventy percent of total requests), so optimizing coder model selection has disproportionate impact on overall system performance.

The reviewer role requires critical analysis, edge case identification, and quality assessment. These capabilities benefit from large parameter counts but less critically than planning. Routing policy assigns large models to reviewer roles when GPU resources permit, but allows degradation to medium models when resources are constrained. The quality degradation is measurable but acceptable—a 13B reviewer model catches eighty-five percent of issues a 70B reviewer catches, which maintains minimum quality bars.

The multi-model strategy extends beyond role-based assignment to complexity-aware selection. Within the coder role, not all tasks have equal complexity. Renaming a variable requires minimal reasoning. Implementing a complex algorithm requires substantial reasoning. Routing policy can analyze task context—token count, user description, dependency complexity—and select models dynamically. Simple tasks use 7B models. Medium tasks use 13B models. Complex tasks use 70B models. This granular selection maximizes efficiency without sacrificing quality on difficult tasks.

### Integration Points

**Task 004 Integration (Model Provider Interface):** The routing policy consumes the IModelProvider interface to query model availability and capabilities. When selecting a model, the policy calls provider.IsModelAvailable(modelId) to verify the model is loaded and ready for inference. This integration enables routing decisions based on real-time provider status rather than static configuration. If a model crashes or unloads, the routing policy immediately detects unavailability and triggers fallback without attempting doomed inference requests.

The provider interface also supplies model capability metadata through provider.GetModelCapabilities(modelId). This metadata includes parameter count, context window size, supported features (tool calling, function calling, structured output), and performance characteristics (tokens per second, memory requirements). The routing policy uses this metadata to validate compatibility—if a task requires tool calling but the selected model does not support it, routing fails fast with a clear error rather than attempting inference and encountering runtime failures.

**Task 008 Integration (Prompt Template Resolution):** The routing policy influences prompt template selection through model-specific template variants. Different models have different prompt format requirements—some use ChatML, others use Alpaca, others use proprietary formats. Task 008 provides a PromptTemplateResolver that accepts a model identifier and returns the appropriate template. The routing policy output (selected model ID) feeds directly into template resolution, ensuring prompts are formatted correctly for the target model.

This integration also enables model-aware prompt optimization. Large models benefit from detailed, verbose prompts that provide extensive context. Small models perform better with concise, focused prompts that minimize unnecessary tokens. The prompt template resolver can select different template variants based on model capabilities, maximizing quality for each model class. The routing policy provides the model metadata necessary for this selection.

**Task 012 Integration (Multi-Stage Agent Loop):** The multi-stage agent loop implements the planner-coder-reviewer workflow that routing policy was designed to optimize. Each stage requests a model for its designated role. Task 012 creates a RoutingRequest with the appropriate AgentRole value (Planner, Coder, Reviewer) and calls routingPolicy.GetModel(request). The routing policy returns the optimal model for that stage, and the agent loop passes this model identifier to the inference provider.

The integration enables stage-specific model assignment without tight coupling between agent loop logic and model selection logic. The agent loop remains agnostic to routing strategy—it simply requests "a model for the coder role" and receives a response. This separation allows routing strategy changes (single-model to role-based) without modifying agent loop code. It also enables A/B testing of routing strategies by swapping policy implementations while keeping the agent loop unchanged.

**Task 002 Integration (Configuration Management):** Routing policy configuration lives in the .agent/config.yml file under the models.routing section. Task 002 provides the IConfigurationProvider interface that loads and validates this YAML configuration. The routing policy implementation depends on IConfigurationProvider to read configuration at startup and optionally reload configuration when files change.

This integration enforces configuration schema validation. Task 002 uses JSON Schema validation to ensure routing configuration conforms to expected structure before the routing policy attempts to parse it. Invalid configuration (typos in strategy name, malformed model IDs, circular fallback chains) triggers validation errors with precise error messages indicating which field is invalid and why. This prevents runtime failures from configuration mistakes.

### Constraints and Design Decisions

**Constraint 001: Operating Mode Enforcement** - The routing policy MUST respect Task 001 operating mode constraints at all times. In local-only mode, the policy will never select a model hosted by external APIs (OpenAI, Anthropic, Cohere), even if such a model is configured and technically available. The policy checks model.IsLocal before adding any model to the candidate pool. This constraint is non-negotiable—local-only mode exists for privacy and security reasons, and routing policy cannot bypass it even when it would improve quality or performance.

**Constraint 002: Deterministic Selection** - Given identical inputs (same role, same context, same configuration, same model availability), the routing policy MUST always return the same model. Non-determinism in routing would make debugging impossible—users could not reproduce failures, tests would be flaky, and performance would vary unpredictably. The policy achieves determinism by avoiding random selection, using stable sorting algorithms, and eliminating time-based decisions. Even fallback selection is deterministic—the first available model in the fallback chain always wins.

**Constraint 003: Fast Decision Time** - Routing decisions MUST complete within ten milliseconds for ninety-nine percent of requests. Slow routing adds latency to every inference request, degrading user experience. The ten millisecond budget constrains implementation choices—routing cannot perform expensive operations like model benchmarking, network calls to external services, or complex optimization algorithms. The policy must rely on cached metadata, simple heuristics, and fast lookups.

**Constraint 004: Fail-Safe Defaults** - If routing policy encounters any error (invalid configuration, unavailable models, mode constraint violation), it MUST fail safely rather than silently degrading. Silent degradation would hide problems until they cause catastrophic failures. Failing safely means rejecting the routing request with a clear error message that explains what went wrong and suggests remediation. Users receive immediate feedback that configuration needs fixing rather than experiencing mysterious failures later.

**Constraint 005: Zero Network Dependency** - The routing policy MUST NOT make network calls during model selection. Network calls introduce latency, failure modes, and privacy risks. All routing decisions must be based on local data—configuration files, cached model registry, in-process availability checks. This constraint aligns with the local-first philosophy—the agent works offline by default, and routing policy cannot break that guarantee.

**Design Decision 001: Strategy Pattern for Routing Logic** - The routing policy uses the Strategy pattern to encapsulate different routing algorithms (single-model, role-based, adaptive). This design enables users to switch routing strategies without code changes—just update configuration. It also simplifies testing—each strategy can be tested independently with clear contracts. The alternative approach (monolithic routing class with conditional logic) would create fragile code that breaks when adding new strategies.

**Design Decision 002: Fallback as Separate Concern** - Fallback handling is implemented in a dedicated FallbackHandler class rather than embedded in strategy implementations. This separation improves reusability—all strategies share the same fallback logic. It also clarifies responsibilities—strategies select the primary model, fallback handler manages escalation. The alternative approach (each strategy implements its own fallback) would duplicate logic and create inconsistent fallback behavior across strategies.

**Design Decision 003: Availability Caching with TTL** - Model availability is cached for five seconds rather than queried on every routing request. This design reduces load on model providers (which may be remote services with rate limits) while maintaining reasonable freshness. The five-second TTL balances staleness risk against performance. The alternative approaches (no caching, indefinite caching) both have fatal flaws—no caching causes excessive provider load, indefinite caching misses model failures until process restart.

**Design Decision 004: Configuration-Driven Rather Than Code-Driven** - Routing behavior is controlled entirely by configuration rather than requiring code changes. This design empowers users to customize routing without forking code, submitting patches, or waiting for releases. It also enables environment-specific configuration—development environments can use different routing than production without maintaining separate codebases. The alternative approach (hardcoded routing logic) would limit flexibility and force users to modify code for simple changes.

**Design Decision 005: Explicit Role Enum Rather Than String Constants** - Agent roles are defined as an enum (AgentRole.Planner, AgentRole.Coder, AgentRole.Reviewer) rather than magic strings ("planner", "coder", "reviewer"). This design provides compile-time safety—typos in role names are caught by the compiler rather than discovered at runtime. It also enables IDE autocomplete and refactoring support. The enum is extensible—new roles can be added without breaking existing code, and future enhancements can add role metadata through attributes.

**Design Decision 006: Immutable Routing Decisions** - The RoutingDecision class is immutable (readonly properties, init-only setters). This design prevents accidental mutation after decision creation, simplifying reasoning about routing behavior. Immutable decisions can be safely cached, logged, and passed between components without defensive copying. The alternative approach (mutable decisions) would create subtle bugs from unintended modifications.

**Design Decision 007: Structured Logging with JSON** - Routing decisions are logged in structured JSON format rather than free-form text. This design enables programmatic log analysis, metric aggregation, and anomaly detection. Users can query logs for fallback frequency, role distribution, and model utilization patterns. The JSON structure includes all relevant context—role, selected model, fallback status, decision time. The alternative approach (text logs) would require brittle regex parsing to extract metrics.

**Design Decision 008: Provider-Agnostic Model IDs** - Model identifiers follow the format "name:tag" (ollama style) or "name:tag@provider" (explicit provider). This format is provider-agnostic—the same model ID can reference Ollama, vLLM, or future providers. The routing policy does not hardcode provider assumptions. The alternative approach (provider-specific IDs like "ollama://llama3.2:70b") would couple routing to specific providers and complicate provider switching.

**Design Decision 009: Graceful Degradation Over Hard Failures** - When all models in the fallback chain are unavailable, the routing policy returns an error but includes suggestions for remediation ("Start a model with 'ollama run llama3.2:7b'"). This design helps users recover from failures quickly. The alternative approach (generic error with no context) would leave users confused about how to fix the problem.

**Design Decision 010: No Automatic Model Loading** - The routing policy will not automatically load models when they are unavailable. Automatic loading would introduce unpredictable latency (model loading takes tens of seconds), consume resources without user consent, and complicate lifecycle management. The policy assumes models are pre-loaded and fails if they are not. This design keeps routing concerns separate from model lifecycle concerns.

**Design Decision 011: Synchronous API Over Async** - The IRoutingPolicy.GetModel method is synchronous rather than async. Routing decisions are fast (sub-ten-millisecond) and do not perform I/O, making async overhead unnecessary. Synchronous APIs simplify calling code (no await noise) and avoid async state machine overhead. The availability check may timeout after five seconds, but this is implemented via synchronous timeout, not async cancellation.

**Design Decision 012: Policy as Singleton Service** - The routing policy is registered as a singleton service in dependency injection. All components share one policy instance for the lifetime of the application. This design enables caching optimizations (model availability cache is shared) and simplifies lifecycle management. The alternative approach (transient policy per request) would lose caching benefits and waste memory on duplicate instances.

**Design Decision 013: User Overrides via Context** - Users can override routing decisions by including explicit model IDs in the RoutingContext. This design enables power users to force specific model selection for debugging or experimentation without changing global configuration. The override bypasses routing strategies but still respects operating mode constraints and availability checks.

**Design Decision 014: Metrics Exposure** - The routing policy exposes metrics (requests per role, fallback frequency, average decision time) through an injectable IMetricsCollector interface. This design enables performance monitoring and capacity planning without coupling routing to specific metrics systems (Prometheus, StatsD, Application Insights). The metrics collector is optional—if not configured, routing proceeds without metrics overhead.

**Design Decision 015: XML Documentation on All Public APIs** - Every public interface, class, method, and property has XML documentation comments explaining purpose, behavior, preconditions, and postconditions. This design ensures IntelliSense support in IDEs and enables automated documentation generation. The XML comments include examples showing typical usage patterns. This documentation is mandatory, not optional—public APIs without documentation fail code review.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Model Routing | Mapping tasks to models |
| Routing Policy | Rules for model selection |
| Role | Agent mode (planner, coder, reviewer) |
| Planner Role | Task decomposition mode |
| Coder Role | Implementation mode |
| Reviewer Role | Verification mode |
| Task Complexity | Estimated difficulty of task |
| Routing Strategy | Approach to model selection |
| Single Model Strategy | One model for all roles |
| Role-based Strategy | Different models per role |
| Adaptive Strategy | Complexity-aware routing |
| Fallback | Alternative when primary unavailable |
| Escalation | Moving to fallback model |
| Fallback Chain | Ordered list of alternatives |
| Model Eligibility | Whether model can be used |
| Routing Decision | Result of policy evaluation |
| Policy Engine | Component that executes policy |
| Override | User-specified routing |
| Heuristic | Rule for estimating complexity |

---

## Use Cases

### Use Case 1: DevBot Routes Planning to Large Model, Coding to Fast Model

DevBot is Neil's autonomous coding assistant configured with role-based routing strategy to optimize for both quality and performance. Neil has configured the routing policy to use llama3.2:70b for planning and review tasks but llama3.2:7b for coding tasks. This configuration reflects the complexity profile of his typical workflows—planning requires deep reasoning about architecture and tradeoffs, while most coding tasks involve straightforward implementation of well-defined specifications.

Neil starts a new agent session with the command "acode run implement-user-authentication". The agent enters the planner role first, breaking down this high-level task into concrete steps. The routing policy receives a GetModel request with role=AgentRole.Planner and consults the role_models configuration. It selects llama3.2:70b and logs "Routing planner role to llama3.2:70b (role-based strategy)". The large model analyzes the authentication requirements, identifies OAuth integration needs, database schema changes, and security considerations, producing a comprehensive eight-step implementation plan. This planning phase takes forty-five seconds but produces high-quality output that saves hours of rework.

After planning completes, the agent transitions to the coder role to implement step one: "Add User table with OAuth fields". The routing policy receives a GetModel request with role=AgentRole.Coder and selects llama3.2:7b based on role mapping. The decision is logged as "Routing coder role to llama3.2:7b (role-based strategy)". The small model generates the database migration file in twelve seconds, correctly implementing the schema with appropriate constraints and indexes. The quality is excellent because the task is well-specified—the model just needs to follow patterns from the planning phase.

The agent continues through all coding steps, using llama3.2:7b for each implementation task. After all code is generated, the agent enters reviewer role to verify correctness. The routing policy selects llama3.2:70b again for review, ensuring critical analysis with the large model. The reviewer identifies a missing index on the OAuth provider column, which DevBot fixes before committing. Total session time is six minutes (forty-five seconds planning, four minutes coding, seventy-five seconds review). Using llama3.2:70b for all tasks would have taken eleven minutes. Neil achieved a forty-five percent time savings while maintaining the same quality level through intelligent routing.

### Use Case 2: Jordan Overrides Routing for Cost Optimization

Jordan manages a small development team running local AI infrastructure on a tight budget. The team has one RTX 4090 GPU with twenty-four gigabytes VRAM, limiting concurrent model capacity. Jordan configures routing policy with conservative defaults to maximize throughput across the team. The default model is mistral:7b for all roles, with role_models overrides for critical tasks only (planner and reviewer use llama3.2:13b, coder uses mistral:7b). This configuration keeps three models loaded simultaneously, supporting parallel agent sessions.

During a critical bug fix under deadline pressure, Jordan needs to override the default routing for one specific task. The bug involves complex async race condition logic that the 7B coder model has struggled with in previous attempts. Jordan invokes the agent with an explicit model override: "acode run --model llama3.2:70b fix-race-condition-in-message-queue". The routing policy receives the GetModel request with role=AgentRole.Coder and detects the user override in the RoutingContext. It logs "User override detected, forcing model llama3.2:70b for coder role (bypassing role-based strategy)" and selects the large model despite the configured coder model being mistral:7b.

The override works perfectly—the 70B model identifies the race condition immediately, implements proper locking with detailed comments explaining the synchronization strategy, and adds regression tests covering the failure scenario. The fix is production-ready without iteration. Jordan reviews the code and approves deployment within thirty minutes, meeting the deadline. After deploying the fix, Jordan returns to standard routing configuration. The temporary override solved the immediate problem without permanently consuming extra GPU resources.

This use case demonstrates routing policy's flexibility—the system provides intelligent defaults through configuration but allows expert users to override decisions when they have context the policy cannot capture. The override still respects operating mode constraints (if Jordan tried to override to a cloud model in local-only mode, the policy would reject it), maintaining safety boundaries while enabling power-user workflows.

### Use Case 3: Alex Configures Fallback When Primary Model Unavailable

Alex runs a distributed development team with developers in multiple time zones. Each developer has local Ollama installations with different model availability based on their hardware capabilities. Some developers have high-end workstations running llama3.2:70b, others have laptops running llama3.2:7b. Alex needs routing configuration that works reliably across this heterogeneous environment without requiring per-developer customization.

Alex configures a fallback chain in the global .agent/config.yml: fallback_chain: [llama3.2:70b, llama3.2:13b, llama3.2:7b, mistral:7b]. The role_models configuration specifies llama3.2:70b for all roles, but the fallback chain ensures degraded functionality when the primary model is unavailable. Developers on powerful workstations get optimal quality with 70B models. Developers on laptops automatically fall back to 13B or 7B models without configuration changes.

Sarah, a developer on Alex's team, works from a laptop with eight gigabytes VRAM running only llama3.2:7b. When Sarah runs "acode run add-input-validation", the routing policy attempts to select llama3.2:70b per the configured role_models mapping. The availability checker queries Ollama and discovers llama3.2:70b is not loaded. The policy logs "Primary model llama3.2:70b unavailable, checking fallback chain" and tries llama3.2:13b. Still unavailable. It tries llama3.2:7b and receives confirmation from Ollama that this model is loaded and ready. The policy logs "Selected fallback model llama3.2:7b (primary llama3.2:70b unavailable)" and returns this decision to the agent.

Sarah's workflow proceeds normally with the 7B model. She receives slightly lower quality output compared to teammates using 70B models, but the task completes successfully. The routing policy's fallback mechanism made the degradation transparent—Sarah did not need to modify configuration, debug errors, or understand why her laptop cannot run large models. The system automatically selected the best available option. Meanwhile, Carlos on a workstation with thirty-two gigabytes VRAM uses llama3.2:70b for the same configuration file, getting optimal quality. The same configuration works across both hardware profiles.

---

## Assumptions

### Technical Assumptions

- ASM-001: Model providers (Ollama, vLLM) expose availability checking APIs that respond within five hundred milliseconds under normal load conditions
- ASM-002: Model identifiers follow consistent naming conventions across providers (name:tag format) enabling provider-agnostic routing logic
- ASM-003: Model capability metadata (parameter count, context window, supported features) is available through provider interfaces without requiring inference
- ASM-004: Model availability state changes infrequently enough that five-second caching does not cause significant staleness issues in practice
- ASM-005: The .agent/config.yml file is readable by the application process and configuration changes are detected through file system notifications
- ASM-006: Model loading and unloading is managed by external systems (Ollama, vLLM) not by the routing policy itself
- ASM-007: Model performance characteristics (tokens per second, memory requirements) are relatively stable and can be cached based on model ID
- ASM-008: Operating mode constraints (local-only, air-gapped) are enforced at the system level and routing policy can query current mode reliably
- ASM-009: JSON Schema validation for routing configuration is performed by Task 002 before routing policy attempts to parse configuration
- ASM-010: Dependency injection container provides singleton lifecycle for IRoutingPolicy, ensuring single instance per application lifetime

### Operational Assumptions

- ASM-011: Users understand the relationship between model parameter count and quality/performance tradeoffs when configuring role-based routing
- ASM-012: Users pre-load models before starting agent workflows rather than expecting on-demand loading during routing
- ASM-013: Fallback chains are configured thoughtfully with gradually degrading quality rather than arbitrary model sequences
- ASM-014: Cost constraints refer to computational cost (GPU time, memory, latency) not monetary cost since local models have no per-inference pricing
- ASM-015: Model unavailability is an exceptional condition not a normal operating state—routing fallback is for resilience not primary workflow
- ASM-016: Users monitor routing decision logs to detect fallback frequency and tune configuration accordingly
- ASM-017: Development environments tolerate higher routing decision latency (up to ten milliseconds) compared to production requirements
- ASM-018: Model capability mismatches (task requires tool calling, model does not support it) are rare because configuration is validated during setup
- ASM-019: Users accept that routing decisions are deterministic given identical inputs, meaning performance tuning requires configuration changes not runtime adaptation
- ASM-020: Routing policy does not implement dynamic load balancing across multiple instances of the same model—single instance per model is assumed

### Integration Assumptions

- ASM-021: Task 004 (Model Provider Interface) provides IModelProvider.IsModelAvailable(modelId) method that returns boolean availability status
- ASM-022: Task 004 provides IModelProvider.GetModelCapabilities(modelId) method that returns structured metadata about model features
- ASM-023: Task 008 (Prompt Template Resolution) accepts model ID as input and returns correctly formatted prompts for that model
- ASM-024: Task 012 (Multi-Stage Agent Loop) calls routing policy separately for each stage (planner, coder, reviewer) rather than caching model selection
- ASM-025: Task 002 (Configuration Management) validates routing configuration schema before routing policy initialization preventing invalid state
- ASM-026: Task 001 (Operating Modes) exposes current operating mode through IOperatingModeProvider interface that routing policy can inject
- ASM-027: Model provider registry is populated by provider implementations (Task 005, Task 006) during application startup before routing requests
- ASM-028: Logging infrastructure supports structured JSON logging with arbitrary metadata fields for routing decision context
- ASM-029: Metrics collection infrastructure (if configured) supports counter and histogram metrics for routing telemetry
- ASM-030: CLI framework (Task 010) provides command routing to enable "acode models routing" and "acode models test" commands

---

## Security Considerations

### Threat 1: Model Confusion Attack

**Description:** An attacker with write access to .agent/config.yml modifies routing configuration to send all requests to a compromised model under attacker control. The compromised model returns malicious code instead of legitimate implementations. The routing policy correctly routes to the configured model, but the configured model is malicious.

**Attack Vectors:**
- Attacker gains file system access through separate vulnerability (misconfigured permissions, supply chain attack on development tools)
- Attacker modifies config.yml to change default_model or role_models to reference malicious model
- Attacker sets up local Ollama server hosting malicious model with legitimate-sounding name (llama3.2:70b-optimized)
- Routing policy queries availability, finds malicious model available, routes all traffic to attacker-controlled model
- Malicious model injects backdoors, credential theft, or data exfiltration into generated code

**Mitigations:**
- MITIGATION-001: Routing policy MUST validate model identifiers against allow-list when operating in security-sensitive modes
- MITIGATION-002: Configuration file MUST have restrictive permissions (user-read-write only, no group/other access)
- MITIGATION-003: Routing decisions MUST be logged with model source information (provider, endpoint) enabling audit trail
- MITIGATION-004: Model availability checking SHOULD verify model signatures/checksums when provider supports it
- MITIGATION-005: Operating system file integrity monitoring SHOULD alert on .agent/config.yml modifications

**Audit Requirements:**
- AUDIT-001: All routing decisions MUST be logged to tamper-evident audit log with timestamp, model ID, provider, selection reason
- AUDIT-002: Configuration changes MUST trigger security events in audit log before new configuration takes effect
- AUDIT-003: Fallback events MUST be logged at WARNING level to detect potential model confusion attempts
- AUDIT-004: Model availability failures MUST be logged with sufficient detail to distinguish legitimate unavailability from suspicious patterns

### Threat 2: Cost Amplification Attack

**Description:** An attacker without write access to configuration can still manipulate routing behavior by causing model unavailability, forcing expensive fallback behavior. If primary model (efficient 7B) is made unavailable, routing policy falls back to larger model (70B), amplifying computational cost by ten times. Sustained unavailability causes denial-of-service through resource exhaustion.

**Attack Vectors:**
- Attacker sends malformed requests to Ollama/vLLM causing model crashes repeatedly
- Attacker consumes GPU memory with separate process, preventing target model from loading
- Attacker exploits race condition in availability checking, causing false unavailability reports
- Routing policy repeatedly falls back to expensive models, exhausting GPU resources
- Legitimate workflows are starved of resources, causing operational denial-of-service

**Mitigations:**
- MITIGATION-006: Fallback chain MUST NOT escalate to significantly more expensive models without explicit user approval
- MITIGATION-007: Routing policy SHOULD track fallback frequency and fail-stop if fallback rate exceeds threshold (e.g., ten percent)
- MITIGATION-008: Availability checking MUST implement timeout and circuit breaker to prevent thundering herd on failed providers
- MITIGATION-009: Routing policy SHOULD enforce maximum model size constraint preventing unbounded resource consumption
- MITIGATION-010: Rate limiting on routing requests prevents rapid-fire attacks attempting to exhaust fallback chains

**Audit Requirements:**
- AUDIT-005: Fallback frequency MUST be tracked per model, per role, per time window with alerts on anomalies
- AUDIT-006: Resource consumption metrics (GPU memory, inference latency) MUST be correlated with routing decisions
- AUDIT-007: Sustained fallback conditions MUST trigger automated alerts to operations team

### Threat 3: Capability Spoofing

**Description:** An attacker operates a malicious model that advertises capabilities it does not actually support, causing routing policy to select it for tasks requiring those capabilities. The model accepts requests requiring tool calling or function calling but returns garbage output, causing downstream failures that may bypass security checks.

**Attack Vectors:**
- Attacker runs modified Ollama server that returns false capability metadata
- Routing policy queries capabilities, receives claim of tool calling support
- Policy routes tool-calling task to malicious model based on spoofed capabilities
- Malicious model ignores tool calling protocol, returns freeform text instead of structured calls
- Agent misinterprets freeform text as tool invocations, executing unintended commands

**Mitigations:**
- MITIGATION-011: Routing policy MUST validate model capabilities through trial inference before caching capability metadata
- MITIGATION-012: Model provider interface SHOULD implement capability verification through signed attestations
- MITIGATION-013: Routing policy SHOULD prefer models from trusted providers when multiple candidates support required capabilities
- MITIGATION-014: Tool calling failures MUST be detected by agent loop and attributed to model selection errors
- MITIGATION-015: Capability spoofing detection SHOULD trigger blacklisting of model from future routing decisions

**Audit Requirements:**
- AUDIT-008: Model capability checks MUST be logged with full capability metadata for forensic analysis
- AUDIT-009: Tool calling failures MUST be logged with selected model ID enabling correlation with routing decisions
- AUDIT-010: Capability verification failures MUST trigger security alerts for investigation

### Threat 4: Privacy Leakage Through Model Selection

**Description:** Routing decisions based on task complexity leak information about task content to passive observers who can see model selection but not task details. If routing always selects 70B model for sensitive tasks and 7B model for simple tasks, an observer monitoring GPU utilization can infer when sensitive work is occurring even without seeing task content.

**Attack Vectors:**
- Attacker monitors GPU memory utilization, process names, or network traffic to infer model selection
- Correlation between large model selection and sensitive task types reveals task classification
- Over time, attacker builds profile of sensitive task patterns based solely on routing behavior
- Privacy leakage occurs even when task content is encrypted or access-controlled

**Mitigations:**
- MITIGATION-016: Routing policy SHOULD support constant-time mode where all roles use same model size to prevent size-based inference
- MITIGATION-017: Model loading/unloading SHOULD be randomized or padded to prevent GPU utilization monitoring
- MITIGATION-018: Routing decisions SHOULD avoid task content analysis when operating in high-security modes
- MITIGATION-019: Operating mode constraints SHOULD enforce privacy-preserving routing strategies when configured

**Audit Requirements:**
- AUDIT-011: Model selection patterns MUST be analyzed for correlation with task sensitivity classification
- AUDIT-012: Privacy-sensitive deployments MUST use constant-time routing mode with audit verification

### Threat 5: Dependency Confusion Attack

**Description:** An attacker publishes a malicious model to public registry with same name as legitimate internal model. Routing policy queries model availability across multiple providers, finds malicious external model before internal model, and routes traffic to attacker-controlled system.

**Attack Vectors:**
- Attacker registers malicious model "llama3.2:70b" on public Ollama registry
- User configuration specifies "llama3.2:70b" without explicit provider qualifier
- Routing policy queries availability across configured providers (internal first, then public fallback)
- Race condition or misconfiguration causes public model to be checked before internal model
- Routing selects attacker's malicious model, exposing all task content to attacker

**Mitigations:**
- MITIGATION-020: Model identifiers MUST use explicit provider qualifiers (llama3.2:70b@internal-ollama) when multiple providers are configured
- MITIGATION-021: Provider priority MUST be explicitly configured preventing accidental external provider usage
- MITIGATION-022: Operating mode constraints MUST prevent external provider access when local-only or air-gapped mode is active
- MITIGATION-023: Model availability checking MUST respect provider ordering and fail-stop on ambiguous model IDs

**Audit Requirements:**
- AUDIT-013: All model selections MUST log provider information enabling detection of unexpected external provider usage
- AUDIT-014: External provider access MUST trigger alerts when not explicitly configured and approved

### Threat 6: Configuration Injection Attack

**Description:** An attacker exploits YAML parsing vulnerabilities to inject malicious configuration through untrusted input channels. If routing configuration is constructed from user input without proper sanitization, attacker can inject arbitrary routing rules redirecting traffic to malicious models.

**Attack Vectors:**
- Application accepts user-provided configuration fragments for routing customization
- Attacker injects YAML bomb, billion laughs attack, or entity expansion attack
- Configuration parser consumes excessive memory/CPU causing denial-of-service
- Alternatively, attacker injects malicious routing rules through unsanitized string interpolation
- Routing policy reads poisoned configuration and routes to attacker-controlled models

**Mitigations:**
- MITIGATION-024: Configuration parsing MUST use safe YAML parser with entity expansion limits
- MITIGATION-025: User-provided configuration MUST be validated against strict schema before merging with defaults
- MITIGATION-026: Configuration sources MUST be explicitly trusted—no arbitrary file includes or network configuration sources
- MITIGATION-027: Routing configuration MUST be immutable after initialization preventing runtime injection

**Audit Requirements:**
- AUDIT-015: Configuration sources MUST be logged during initialization with integrity hashes
- AUDIT-016: Configuration validation failures MUST be logged with rejected content for forensic analysis

---

## Out of Scope

The following items are explicitly excluded from Task 009:

- **Model loading/unloading** - Covered in provider tasks
- **Model inference execution** - Covered in Task 004
- **Provider implementation** - Covered in Tasks 005, 006
- **Prompt composition** - Covered in Task 008
- **Tool calling** - Covered in Task 007
- **Load balancing across instances** - Not in MVP
- **Cost optimization** - Not applicable (local models)
- **A/B testing of models** - Post-MVP
- **Model performance benchmarking** - Separate concern
- **Automatic model selection** - Post-MVP

---

## Functional Requirements

### IRoutingPolicy Interface

- FR-001: Interface MUST be defined in Application layer
- FR-002: Interface MUST have GetModel(role, context) method
- FR-003: GetModel MUST return ModelConfiguration
- FR-004: Interface MUST have GetFallbackModel(role, context) method
- FR-005: Interface MUST have IsModelAvailable(modelId) method
- FR-006: Interface MUST have ListAvailableModels() method

### RoutingPolicy Implementation

- FR-007: Implementation MUST be in Infrastructure layer
- FR-008: Policy MUST read configuration from config file
- FR-009: Policy MUST support single model strategy
- FR-010: Policy MUST support role-based strategy
- FR-011: Policy MUST respect operating mode constraints
- FR-012: Policy MUST log routing decisions

### Role Definitions

- FR-013: MUST define "planner" role
- FR-014: MUST define "coder" role
- FR-015: MUST define "reviewer" role
- FR-016: MUST define "default" role as fallback
- FR-017: Roles MUST be extensible

### Configuration Schema

- FR-018: Config section: models.routing
- FR-019: MUST have strategy field (single, role-based, adaptive)
- FR-020: Default strategy MUST be "single"
- FR-021: MUST have default_model field
- FR-022: MAY have role_models map
- FR-023: role_models keys: planner, coder, reviewer
- FR-024: MAY have fallback_chain array

### Single Model Strategy

- FR-025: MUST use default_model for all roles
- FR-026: No role-specific configuration required
- FR-027: Fallback MUST still apply

### Role-Based Strategy

- FR-028: MUST read role_models from config
- FR-029: Each role MAY have dedicated model
- FR-030: Missing role MUST use default_model
- FR-031: Role model MUST override default

### Model Resolution

- FR-032: Resolution MUST check model availability
- FR-033: Unavailable model MUST trigger fallback
- FR-034: Resolution MUST validate model ID
- FR-035: Invalid model ID MUST fail with clear error
- FR-036: Resolution MUST cache results per session

### Fallback Chain

- FR-037: Fallback chain MUST be ordered array
- FR-038: Chain traversal MUST be sequential
- FR-039: First available model MUST be selected
- FR-040: Empty chain or all unavailable MUST fail
- FR-041: Fallback MUST be logged as WARNING

### Operating Mode Constraints

- FR-042: MUST check Task 001 operating mode
- FR-043: local-only mode MUST use local models
- FR-044: air-gapped mode MUST reject remote
- FR-045: Mode violation MUST fail with error
- FR-046: Constraint check MUST precede selection

### Model Availability

- FR-047: Availability MUST query provider registry
- FR-048: Availability MUST check model is loaded
- FR-049: Availability MUST timeout after 5s
- FR-050: Unavailable MUST trigger fallback

### CLI Integration

- FR-051: `acode models routing` MUST show config
- FR-052: MUST show current strategy
- FR-053: MUST show role assignments
- FR-054: MUST show fallback chain
- FR-055: `acode models test <role>` MUST test routing

### Logging

- FR-056: Routing request MUST be logged
- FR-057: Selected model MUST be logged
- FR-058: Selection reason MUST be logged
- FR-059: Fallback events MUST be logged WARNING
- FR-060: Mode constraint checks MUST be logged

### Error Handling

- FR-061: No available model MUST fail gracefully
- FR-062: Error MUST include attempted models
- FR-063: Error MUST suggest configuration fix
- FR-064: Error code MUST be specific

---

## Non-Functional Requirements

### Performance

- NFR-001: Routing decision MUST complete in < 10ms
- NFR-002: Availability check MUST timeout at 5s
- NFR-003: Cache MUST be used for repeated lookups
- NFR-004: Policy initialization MUST be < 100ms

### Reliability

- NFR-005: Invalid config MUST fail with clear message
- NFR-006: Unavailable model MUST trigger fallback
- NFR-007: All fallbacks exhausted MUST fail gracefully
- NFR-008: Policy MUST handle concurrent requests

### Security

- NFR-009: Model IDs MUST be validated
- NFR-010: No secrets in routing config
- NFR-011: Config parsing MUST be safe

### Observability

- NFR-012: All routing decisions MUST be logged
- NFR-013: Fallback events MUST be prominent
- NFR-014: Metrics SHOULD track routing distribution
- NFR-015: Health check SHOULD include routing status

### Maintainability

- NFR-016: Strategies MUST be pluggable
- NFR-017: New roles MUST be addable
- NFR-018: All public APIs MUST have XML docs
- NFR-019: Tests MUST cover all strategies

---

## User Manual Documentation

### Overview

Model routing determines which model handles each agent task. Different roles (planner, coder, reviewer) can use different models based on your configuration and available resources.

### Quick Start

```yaml
# .agent/config.yml - Simple single-model setup
models:
  routing:
    strategy: single
    default_model: llama3.2:70b
```

### Routing Strategies

#### Single Model Strategy

Uses one model for all roles. Simplest configuration.

```yaml
models:
  routing:
    strategy: single
    default_model: llama3.2:70b
```

#### Role-Based Strategy

Different models for different roles.

```yaml
models:
  routing:
    strategy: role-based
    default_model: llama3.2:7b
    role_models:
      planner: llama3.2:70b    # Reasoning needs large model
      coder: llama3.2:7b        # Implementation is simpler
      reviewer: llama3.2:70b    # Review needs analysis
```

### Configuration Reference

```yaml
models:
  routing:
    # Strategy: single, role-based, adaptive
    strategy: role-based
    
    # Default model for unassigned roles
    default_model: llama3.2:7b
    
    # Role-specific models (for role-based strategy)
    role_models:
      planner: llama3.2:70b
      coder: llama3.2:7b
      reviewer: llama3.2:70b
    
    # Fallback chain when primary unavailable
    fallback_chain:
      - llama3.2:70b
      - llama3.2:7b
      - mistral:7b
```

### Roles

| Role | Purpose | Typical Requirements |
|------|---------|---------------------|
| planner | Task decomposition | Strong reasoning |
| coder | Implementation | Instruction following |
| reviewer | Verification | Critical analysis |

### Fallback Behavior

When a configured model is unavailable, routing tries alternatives:

```
[INFO] Routing request for role 'planner'
[INFO] Checking primary model: llama3.2:70b
[WARN] Model llama3.2:70b unavailable, trying fallback
[INFO] Checking fallback: llama3.2:7b
[INFO] Selected model: llama3.2:7b (via fallback)
```

Configure fallback chain:

```yaml
models:
  routing:
    fallback_chain:
      - llama3.2:70b    # Try first
      - llama3.2:7b     # Then this
      - mistral:7b      # Last resort
```

### CLI Commands

```bash
# Show routing configuration
$ acode models routing
Routing Strategy: role-based
Default Model: llama3.2:7b

Role Assignments:
  planner  → llama3.2:70b (available)
  coder    → llama3.2:7b (available)
  reviewer → llama3.2:70b (available)

Fallback Chain:
  1. llama3.2:70b (available)
  2. llama3.2:7b (available)
  3. mistral:7b (not loaded)

# Test routing for specific role
$ acode models test planner
Testing routing for role 'planner'...
Primary model: llama3.2:70b
Status: Available
Selection: llama3.2:70b
```

### Operating Mode Integration

Routing respects Task 001 operating modes:

**local-only mode:**
- Only local models eligible
- Remote/cloud models rejected

**air-gapped mode:**
- No network access for models
- Must use pre-loaded local models

```yaml
# Mode set in config
operating_mode: local-only

models:
  routing:
    strategy: single
    default_model: llama3.2:7b  # Must be local
```

### Best Practices

1. **Start simple** - Use single model strategy first
2. **Large for planning** - Use big models for reasoning
3. **Fast for coding** - Smaller models often sufficient
4. **Configure fallbacks** - Ensure alternatives exist
5. **Test routing** - Verify before starting work

### Troubleshooting

#### No Available Model

```
Error: No available model for role 'coder'
  Tried: llama3.2:7b (unavailable), mistral:7b (unavailable)
  Suggestion: Start a model with 'ollama run <model>'
```

**Solution:** Start at least one model, or check fallback chain.

#### Invalid Model ID

```
Error: Invalid model ID 'llama-70b'
  Valid format: name:tag or name:tag@provider
```

**Solution:** Check model ID format.

#### Mode Constraint Violation

```
Error: Model 'gpt-4' not allowed in local-only mode
```

**Solution:** Use a local model or change operating mode.

---

## Acceptance Criteria

### Interface

- [ ] AC-001: IRoutingPolicy in Application layer
- [ ] AC-002: GetModel method exists
- [ ] AC-003: Returns ModelConfiguration
- [ ] AC-004: GetFallbackModel method exists
- [ ] AC-005: IsModelAvailable method exists
- [ ] AC-006: ListAvailableModels method exists

### Implementation

- [ ] AC-007: Infrastructure layer implementation
- [ ] AC-008: Reads from config file
- [ ] AC-009: Single model strategy works
- [ ] AC-010: Role-based strategy works
- [ ] AC-011: Respects operating mode
- [ ] AC-012: Logs decisions

### Roles

- [ ] AC-013: planner role defined
- [ ] AC-014: coder role defined
- [ ] AC-015: reviewer role defined
- [ ] AC-016: default role works

### Configuration

- [ ] AC-017: models.routing section
- [ ] AC-018: strategy field works
- [ ] AC-019: Default is "single"
- [ ] AC-020: default_model field works
- [ ] AC-021: role_models map works
- [ ] AC-022: fallback_chain works

### Single Strategy

- [ ] AC-023: Uses default_model
- [ ] AC-024: All roles same model
- [ ] AC-025: Fallback applies

### Role-Based Strategy

- [ ] AC-026: Reads role_models
- [ ] AC-027: Each role configurable
- [ ] AC-028: Missing uses default
- [ ] AC-029: Override works

### Fallback

- [ ] AC-030: Chain is ordered
- [ ] AC-031: Sequential traversal
- [ ] AC-032: First available selected
- [ ] AC-033: All unavailable fails
- [ ] AC-034: Fallback logged

### Mode Constraints

- [ ] AC-035: Checks operating mode
- [ ] AC-036: local-only enforced
- [ ] AC-037: air-gapped enforced
- [ ] AC-038: Violation fails

### Availability

- [ ] AC-039: Queries provider
- [ ] AC-040: Checks loaded state
- [ ] AC-041: 5s timeout
- [ ] AC-042: Triggers fallback

### CLI

- [ ] AC-043: routing command works
- [ ] AC-044: Shows strategy
- [ ] AC-045: Shows assignments
- [ ] AC-046: Shows chain
- [ ] AC-047: test command works

### Logging

- [ ] AC-048: Request logged
- [ ] AC-049: Selection logged
- [ ] AC-050: Reason logged
- [ ] AC-051: Fallback WARNING

---

## Testing Requirements

### Unit Tests - Routing Decision Logic

#### Test 1: Should Route Planner Role to Configured Large Model

```csharp
namespace Acode.Infrastructure.Tests.Routing;

public class RoutingPolicyTests
{
    [Fact]
    public void Should_Route_Planner_Role_To_Configured_Large_Model()
    {
        // Arrange - Create routing configuration with role-based strategy
        var configuration = new RoutingConfiguration
        {
            Strategy = RoutingStrategy.RoleBased,
            DefaultModel = "llama3.2:7b",
            RoleModels = new Dictionary<AgentRole, string>
            {
                { AgentRole.Planner, "llama3.2:70b" },
                { AgentRole.Coder, "llama3.2:7b" },
                { AgentRole.Reviewer, "llama3.2:70b" }
            }
        };

        var mockProvider = Substitute.For<IModelProvider>();
        mockProvider.IsModelAvailable("llama3.2:70b").Returns(true);
        mockProvider.GetModelCapabilities("llama3.2:70b").Returns(new ModelCapabilities
        {
            ParameterCount = 70_000_000_000,
            SupportsToolCalling = true
        });

        var modelRegistry = new ModelRegistry(new[] { mockProvider });
        var policy = new RoutingPolicy(configuration, modelRegistry, NullLogger<RoutingPolicy>.Instance);

        var context = new RoutingContext
        {
            OperatingMode = OperatingMode.LocalOnly,
            TaskComplexity = TaskComplexity.High
        };

        // Act - Request model for planner role
        var decision = policy.GetModel(AgentRole.Planner, context);

        // Assert - Should select large model for planning
        decision.ModelId.Should().Be("llama3.2:70b");
        decision.IsFallback.Should().BeFalse();
        decision.SelectionReason.Should().Contain("role-based strategy");
        decision.SelectedProvider.Should().NotBeNull();

        mockProvider.Received(1).IsModelAvailable("llama3.2:70b");
    }
}
```

#### Test 2: Should Route Coder Role to Configured Small Model

```csharp
[Fact]
public void Should_Route_Coder_Role_To_Configured_Small_Model()
{
    // Arrange - Same configuration as above but testing coder role
    var configuration = new RoutingConfiguration
    {
        Strategy = RoutingStrategy.RoleBased,
        DefaultModel = "llama3.2:7b",
        RoleModels = new Dictionary<AgentRole, string>
        {
            { AgentRole.Planner, "llama3.2:70b" },
            { AgentRole.Coder, "llama3.2:7b" },
            { AgentRole.Reviewer, "llama3.2:70b" }
        }
    };

    var mockProvider = Substitute.For<IModelProvider>();
    mockProvider.IsModelAvailable("llama3.2:7b").Returns(true);
    mockProvider.GetModelCapabilities("llama3.2:7b").Returns(new ModelCapabilities
    {
        ParameterCount = 7_000_000_000,
        SupportsToolCalling = true
    });

    var modelRegistry = new ModelRegistry(new[] { mockProvider });
    var policy = new RoutingPolicy(configuration, modelRegistry, NullLogger<RoutingPolicy>.Instance);

    var context = new RoutingContext
    {
        OperatingMode = OperatingMode.LocalOnly,
        TaskComplexity = TaskComplexity.Medium
    };

    // Act - Request model for coder role
    var decision = policy.GetModel(AgentRole.Coder, context);

    // Assert - Should select small efficient model for coding
    decision.ModelId.Should().Be("llama3.2:7b");
    decision.IsFallback.Should().BeFalse();
    decision.SelectionReason.Should().Contain("role-based strategy");
    decision.DecisionTimeMs.Should().BeLessThan(10);

    mockProvider.Received(1).IsModelAvailable("llama3.2:7b");
}
```

#### Test 3: Should Use Single Model Strategy When Configured

```csharp
[Fact]
public void Should_Use_Single_Model_Strategy_When_Configured()
{
    // Arrange - Single model strategy uses same model for all roles
    var configuration = new RoutingConfiguration
    {
        Strategy = RoutingStrategy.Single,
        DefaultModel = "llama3.2:70b"
    };

    var mockProvider = Substitute.For<IModelProvider>();
    mockProvider.IsModelAvailable("llama3.2:70b").Returns(true);

    var modelRegistry = new ModelRegistry(new[] { mockProvider });
    var policy = new RoutingPolicy(configuration, modelRegistry, NullLogger<RoutingPolicy>.Instance);

    var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };

    // Act - Request models for all three roles
    var plannerDecision = policy.GetModel(AgentRole.Planner, context);
    var coderDecision = policy.GetModel(AgentRole.Coder, context);
    var reviewerDecision = policy.GetModel(AgentRole.Reviewer, context);

    // Assert - All roles should get same model
    plannerDecision.ModelId.Should().Be("llama3.2:70b");
    coderDecision.ModelId.Should().Be("llama3.2:70b");
    reviewerDecision.ModelId.Should().Be("llama3.2:70b");

    // All should indicate single model strategy
    plannerDecision.SelectionReason.Should().Contain("single model strategy");
    coderDecision.SelectionReason.Should().Contain("single model strategy");
    reviewerDecision.SelectionReason.Should().Contain("single model strategy");

    // Availability should be checked exactly once per call (no caching in test)
    mockProvider.Received(3).IsModelAvailable("llama3.2:70b");
}
```

#### Test 4: Should Fallback to Secondary Model When Primary Unavailable

```csharp
[Fact]
public void Should_Fallback_To_Secondary_Model_When_Primary_Unavailable()
{
    // Arrange - Configure fallback chain
    var configuration = new RoutingConfiguration
    {
        Strategy = RoutingStrategy.RoleBased,
        DefaultModel = "llama3.2:7b",
        RoleModels = new Dictionary<AgentRole, string>
        {
            { AgentRole.Planner, "llama3.2:70b" }
        },
        FallbackChain = new List<string>
        {
            "llama3.2:70b",
            "llama3.2:13b",
            "llama3.2:7b"
        }
    };

    var mockProvider = Substitute.For<IModelProvider>();
    mockProvider.IsModelAvailable("llama3.2:70b").Returns(false); // Primary unavailable
    mockProvider.IsModelAvailable("llama3.2:13b").Returns(true);  // Fallback available

    var modelRegistry = new ModelRegistry(new[] { mockProvider });
    var policy = new RoutingPolicy(configuration, modelRegistry, NullLogger<RoutingPolicy>.Instance);

    var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };

    // Act - Request model for planner role
    var decision = policy.GetModel(AgentRole.Planner, context);

    // Assert - Should fall back to second model in chain
    decision.ModelId.Should().Be("llama3.2:13b");
    decision.IsFallback.Should().BeTrue();
    decision.FallbackReason.Should().Contain("primary unavailable");
    decision.SelectionReason.Should().Contain("fallback");

    // Should have checked primary first, then fallback
    mockProvider.Received(1).IsModelAvailable("llama3.2:70b");
    mockProvider.Received(1).IsModelAvailable("llama3.2:13b");
    mockProvider.DidNotReceive().IsModelAvailable("llama3.2:7b"); // Should stop at first available
}
```

#### Test 5: Should Respect Operating Mode Constraints

```csharp
[Fact]
public void Should_Respect_Operating_Mode_Constraints()
{
    // Arrange - Configure with cloud model (should be rejected in local-only mode)
    var configuration = new RoutingConfiguration
    {
        Strategy = RoutingStrategy.Single,
        DefaultModel = "gpt-4"
    };

    var mockCloudProvider = Substitute.For<IModelProvider>();
    mockCloudProvider.IsModelAvailable("gpt-4").Returns(true);
    mockCloudProvider.GetModelCapabilities("gpt-4").Returns(new ModelCapabilities
    {
        IsLocal = false, // Cloud model
        ParameterCount = 1_000_000_000_000
    });

    var modelRegistry = new ModelRegistry(new[] { mockCloudProvider });
    var policy = new RoutingPolicy(configuration, modelRegistry, NullLogger<RoutingPolicy>.Instance);

    var context = new RoutingContext
    {
        OperatingMode = OperatingMode.LocalOnly // Requires local models only
    };

    // Act & Assert - Should throw exception for mode constraint violation
    var exception = Assert.Throws<RoutingException>(() =>
        policy.GetModel(AgentRole.Coder, context)
    );

    exception.ErrorCode.Should().Be("ACODE-RTE-003");
    exception.Message.Should().Contain("local-only mode");
    exception.Message.Should().Contain("gpt-4");
    exception.Suggestion.Should().Contain("Use a local model");
}
```

#### Test 6: Should Throw When All Fallback Models Unavailable

```csharp
[Fact]
public void Should_Throw_When_All_Fallback_Models_Unavailable()
{
    // Arrange - Configure fallback chain where all models are unavailable
    var configuration = new RoutingConfiguration
    {
        Strategy = RoutingStrategy.Single,
        DefaultModel = "llama3.2:70b",
        FallbackChain = new List<string>
        {
            "llama3.2:70b",
            "llama3.2:13b",
            "llama3.2:7b"
        }
    };

    var mockProvider = Substitute.For<IModelProvider>();
    mockProvider.IsModelAvailable(Arg.Any<string>()).Returns(false); // All unavailable

    var modelRegistry = new ModelRegistry(new[] { mockProvider });
    var policy = new RoutingPolicy(configuration, modelRegistry, NullLogger<RoutingPolicy>.Instance);

    var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };

    // Act & Assert - Should throw with helpful error message
    var exception = Assert.Throws<RoutingException>(() =>
        policy.GetModel(AgentRole.Coder, context)
    );

    exception.ErrorCode.Should().Be("ACODE-RTE-004");
    exception.Message.Should().Contain("exhausted");
    exception.AttemptedModels.Should().ContainInOrder("llama3.2:70b", "llama3.2:13b", "llama3.2:7b");
    exception.Suggestion.Should().Contain("ollama run");
}
```

#### Test 7: Should Use Default Model When Role Not Configured

```csharp
[Fact]
public void Should_Use_Default_Model_When_Role_Not_Configured()
{
    // Arrange - Configure role-based strategy but leave reviewer unconfigured
    var configuration = new RoutingConfiguration
    {
        Strategy = RoutingStrategy.RoleBased,
        DefaultModel = "llama3.2:7b",
        RoleModels = new Dictionary<AgentRole, string>
        {
            { AgentRole.Planner, "llama3.2:70b" },
            { AgentRole.Coder, "llama3.2:13b" }
            // Reviewer not configured, should use default
        }
    };

    var mockProvider = Substitute.For<IModelProvider>();
    mockProvider.IsModelAvailable("llama3.2:7b").Returns(true);

    var modelRegistry = new ModelRegistry(new[] { mockProvider });
    var policy = new RoutingPolicy(configuration, modelRegistry, NullLogger<RoutingPolicy>.Instance);

    var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };

    // Act - Request model for unconfigured reviewer role
    var decision = policy.GetModel(AgentRole.Reviewer, context);

    // Assert - Should fall back to default model
    decision.ModelId.Should().Be("llama3.2:7b");
    decision.SelectionReason.Should().Contain("default model");
    decision.SelectionReason.Should().Contain("role not configured");
}
```

#### Test 8: Should Honor User Override in Routing Context

```csharp
[Fact]
public void Should_Honor_User_Override_In_Routing_Context()
{
    // Arrange - Configure role-based routing
    var configuration = new RoutingConfiguration
    {
        Strategy = RoutingStrategy.RoleBased,
        DefaultModel = "llama3.2:7b",
        RoleModels = new Dictionary<AgentRole, string>
        {
            { AgentRole.Coder, "llama3.2:7b" }
        }
    };

    var mockProvider = Substitute.For<IModelProvider>();
    mockProvider.IsModelAvailable("llama3.2:70b").Returns(true);

    var modelRegistry = new ModelRegistry(new[] { mockProvider });
    var policy = new RoutingPolicy(configuration, modelRegistry, NullLogger<RoutingPolicy>.Instance);

    var context = new RoutingContext
    {
        OperatingMode = OperatingMode.LocalOnly,
        UserOverride = "llama3.2:70b" // User explicitly requests large model
    };

    // Act - Request with override
    var decision = policy.GetModel(AgentRole.Coder, context);

    // Assert - Should use override model, not configured model
    decision.ModelId.Should().Be("llama3.2:70b");
    decision.SelectionReason.Should().Contain("user override");
    decision.SelectionReason.Should().NotContain("role-based");
}
```

#### Test 9: Should Cache Availability Checks Within TTL Window

```csharp
[Fact]
public void Should_Cache_Availability_Checks_Within_TTL_Window()
{
    // Arrange - Configure routing
    var configuration = new RoutingConfiguration
    {
        Strategy = RoutingStrategy.Single,
        DefaultModel = "llama3.2:7b",
        AvailabilityCacheTTLSeconds = 5
    };

    var mockProvider = Substitute.For<IModelProvider>();
    mockProvider.IsModelAvailable("llama3.2:7b").Returns(true);

    var modelRegistry = new ModelRegistry(new[] { mockProvider });
    var policy = new RoutingPolicy(configuration, modelRegistry, NullLogger<RoutingPolicy>.Instance);

    var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };

    // Act - Make multiple routing requests within cache TTL
    var decision1 = policy.GetModel(AgentRole.Coder, context);
    var decision2 = policy.GetModel(AgentRole.Coder, context);
    var decision3 = policy.GetModel(AgentRole.Coder, context);

    // Assert - Availability should be checked only once (cached for subsequent requests)
    decision1.ModelId.Should().Be("llama3.2:7b");
    decision2.ModelId.Should().Be("llama3.2:7b");
    decision3.ModelId.Should().Be("llama3.2:7b");

    mockProvider.Received(1).IsModelAvailable("llama3.2:7b");
}
```

#### Test 10: Should Validate Model ID Format Before Selection

```csharp
[Fact]
public void Should_Validate_Model_ID_Format_Before_Selection()
{
    // Arrange - Configure with invalid model ID
    var configuration = new RoutingConfiguration
    {
        Strategy = RoutingStrategy.Single,
        DefaultModel = "invalid-model-id-no-tag"
    };

    var mockProvider = Substitute.For<IModelProvider>();

    var modelRegistry = new ModelRegistry(new[] { mockProvider });
    var policy = new RoutingPolicy(configuration, modelRegistry, NullLogger<RoutingPolicy>.Instance);

    var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };

    // Act & Assert - Should throw validation exception
    var exception = Assert.Throws<RoutingException>(() =>
        policy.GetModel(AgentRole.Coder, context)
    );

    exception.ErrorCode.Should().Be("ACODE-RTE-002");
    exception.Message.Should().Contain("Invalid model ID");
    exception.Message.Should().Contain("name:tag");
}
```

### Unit Tests - Model Selection Algorithm

#### Test 11: Should Select Model Based on Task Complexity (Adaptive Strategy)

```csharp
[Fact]
public void Should_Select_Model_Based_On_Task_Complexity_Adaptive_Strategy()
{
    // Arrange - Configure adaptive strategy (future enhancement)
    var configuration = new RoutingConfiguration
    {
        Strategy = RoutingStrategy.Adaptive,
        DefaultModel = "llama3.2:7b",
        ComplexityThresholds = new ComplexityThresholds
        {
            LowThreshold = 100,   // Token count < 100 = simple
            HighThreshold = 1000  // Token count > 1000 = complex
        },
        ComplexityModelMapping = new Dictionary<TaskComplexity, string>
        {
            { TaskComplexity.Low, "llama3.2:7b" },
            { TaskComplexity.Medium, "llama3.2:13b" },
            { TaskComplexity.High, "llama3.2:70b" }
        }
    };

    var mockProvider = Substitute.For<IModelProvider>();
    mockProvider.IsModelAvailable(Arg.Any<string>()).Returns(true);

    var modelRegistry = new ModelRegistry(new[] { mockProvider });
    var policy = new RoutingPolicy(configuration, modelRegistry, NullLogger<RoutingPolicy>.Instance);

    // Act - Test simple task (low complexity)
    var simpleContext = new RoutingContext
    {
        OperatingMode = OperatingMode.LocalOnly,
        TaskComplexity = TaskComplexity.Low,
        EstimatedTokenCount = 50
    };
    var simpleDecision = policy.GetModel(AgentRole.Coder, simpleContext);

    // Act - Test complex task (high complexity)
    var complexContext = new RoutingContext
    {
        OperatingMode = OperatingMode.LocalOnly,
        TaskComplexity = TaskComplexity.High,
        EstimatedTokenCount = 2000
    };
    var complexDecision = policy.GetModel(AgentRole.Coder, complexContext);

    // Assert - Should use small model for simple task, large for complex
    simpleDecision.ModelId.Should().Be("llama3.2:7b");
    complexDecision.ModelId.Should().Be("llama3.2:70b");

    simpleDecision.SelectionReason.Should().Contain("adaptive");
    simpleDecision.SelectionReason.Should().Contain("low complexity");
    complexDecision.SelectionReason.Should().Contain("adaptive");
    complexDecision.SelectionReason.Should().Contain("high complexity");
}
```

#### Test 12: Should Prefer Models With Required Capabilities

```csharp
[Fact]
public void Should_Prefer_Models_With_Required_Capabilities()
{
    // Arrange - Multiple models available, only one supports tool calling
    var configuration = new RoutingConfiguration
    {
        Strategy = RoutingStrategy.Single,
        DefaultModel = "llama3.2:7b"
    };

    var mockProvider1 = Substitute.For<IModelProvider>();
    mockProvider1.IsModelAvailable("llama3.2:7b").Returns(true);
    mockProvider1.GetModelCapabilities("llama3.2:7b").Returns(new ModelCapabilities
    {
        SupportsToolCalling = false // Does not support tool calling
    });

    var mockProvider2 = Substitute.For<IModelProvider>();
    mockProvider2.IsModelAvailable("llama3.2:70b").Returns(true);
    mockProvider2.GetModelCapabilities("llama3.2:70b").Returns(new ModelCapabilities
    {
        SupportsToolCalling = true // Supports tool calling
    });

    var modelRegistry = new ModelRegistry(new[] { mockProvider1, mockProvider2 });
    var policy = new RoutingPolicy(configuration, modelRegistry, NullLogger<RoutingPolicy>.Instance);

    var context = new RoutingContext
    {
        OperatingMode = OperatingMode.LocalOnly,
        RequiredCapabilities = new[] { ModelCapability.ToolCalling }
    };

    // Act - Request model with tool calling requirement
    var decision = policy.GetModel(AgentRole.Coder, context);

    // Assert - Should select model that supports required capability
    decision.ModelId.Should().Be("llama3.2:70b");
    decision.SelectionReason.Should().Contain("capability match");
    decision.SelectionReason.Should().Contain("tool calling");
}
```

#### Test 13: Should Throw When No Model Supports Required Capabilities

```csharp
[Fact]
public void Should_Throw_When_No_Model_Supports_Required_Capabilities()
{
    // Arrange - No available models support required capability
    var configuration = new RoutingConfiguration
    {
        Strategy = RoutingStrategy.Single,
        DefaultModel = "llama3.2:7b"
    };

    var mockProvider = Substitute.For<IModelProvider>();
    mockProvider.IsModelAvailable("llama3.2:7b").Returns(true);
    mockProvider.GetModelCapabilities("llama3.2:7b").Returns(new ModelCapabilities
    {
        SupportsToolCalling = false,
        SupportsFunctionCalling = false,
        SupportsStructuredOutput = false
    });

    var modelRegistry = new ModelRegistry(new[] { mockProvider });
    var policy = new RoutingPolicy(configuration, modelRegistry, NullLogger<RoutingPolicy>.Instance);

    var context = new RoutingContext
    {
        OperatingMode = OperatingMode.LocalOnly,
        RequiredCapabilities = new[] { ModelCapability.StructuredOutput }
    };

    // Act & Assert - Should throw capability exception
    var exception = Assert.Throws<RoutingException>(() =>
        policy.GetModel(AgentRole.Coder, context)
    );

    exception.ErrorCode.Should().Be("ACODE-RTE-006");
    exception.Message.Should().Contain("No model supports required capabilities");
    exception.Message.Should().Contain("structured output");
}
```

### Unit Tests - Fallback Chain Traversal

#### Test 14: Should Traverse Fallback Chain Sequentially

```csharp
[Fact]
public void Should_Traverse_Fallback_Chain_Sequentially()
{
    // Arrange - Configure fallback chain with specific order
    var configuration = new RoutingConfiguration
    {
        Strategy = RoutingStrategy.Single,
        DefaultModel = "llama3.2:70b",
        FallbackChain = new List<string>
        {
            "llama3.2:70b",
            "llama3.2:13b",
            "llama3.2:7b",
            "mistral:7b"
        }
    };

    var mockProvider = Substitute.For<IModelProvider>();
    mockProvider.IsModelAvailable("llama3.2:70b").Returns(false);
    mockProvider.IsModelAvailable("llama3.2:13b").Returns(false);
    mockProvider.IsModelAvailable("llama3.2:7b").Returns(true);

    var modelRegistry = new ModelRegistry(new[] { mockProvider });
    var policy = new RoutingPolicy(configuration, modelRegistry, NullLogger<RoutingPolicy>.Instance);

    var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };

    // Act - Request model, should traverse chain
    var decision = policy.GetModel(AgentRole.Coder, context);

    // Assert - Should select third model in chain (first available)
    decision.ModelId.Should().Be("llama3.2:7b");
    decision.IsFallback.Should().BeTrue();

    // Should have checked in order: 70b, 13b, 7b (stop at first available)
    Received.InOrder(() =>
    {
        mockProvider.IsModelAvailable("llama3.2:70b");
        mockProvider.IsModelAvailable("llama3.2:13b");
        mockProvider.IsModelAvailable("llama3.2:7b");
    });

    // Should not have checked fourth model (stopped at third)
    mockProvider.DidNotReceive().IsModelAvailable("mistral:7b");
}
```

#### Test 15: Should Log Fallback Events at WARNING Level

```csharp
[Fact]
public void Should_Log_Fallback_Events_At_WARNING_Level()
{
    // Arrange - Configure fallback scenario
    var configuration = new RoutingConfiguration
    {
        Strategy = RoutingStrategy.Single,
        DefaultModel = "llama3.2:70b",
        FallbackChain = new List<string> { "llama3.2:70b", "llama3.2:7b" }
    };

    var mockProvider = Substitute.For<IModelProvider>();
    mockProvider.IsModelAvailable("llama3.2:70b").Returns(false);
    mockProvider.IsModelAvailable("llama3.2:7b").Returns(true);

    var modelRegistry = new ModelRegistry(new[] { mockProvider });

    var mockLogger = Substitute.For<ILogger<RoutingPolicy>>();
    var policy = new RoutingPolicy(configuration, modelRegistry, mockLogger);

    var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };

    // Act - Trigger fallback
    var decision = policy.GetModel(AgentRole.Coder, context);

    // Assert - Should have logged fallback at WARNING level
    mockLogger.Received(1).Log(
        LogLevel.Warning,
        Arg.Any<EventId>(),
        Arg.Is<object>(o => o.ToString().Contains("fallback")),
        null,
        Arg.Any<Func<object, Exception, string>>()
    );

    decision.IsFallback.Should().BeTrue();
}
```

### Integration Tests - End-to-End Routing

#### Test 16: Should Route to Available Model with Real Configuration

```csharp
[Fact]
public async Task Should_Route_To_Available_Model_With_Real_Configuration()
{
    // Arrange - Load real configuration from test YAML file
    var configYaml = @"
models:
  routing:
    strategy: role-based
    default_model: llama3.2:7b
    role_models:
      planner: llama3.2:70b
      coder: llama3.2:7b
      reviewer: llama3.2:70b
    fallback_chain:
      - llama3.2:70b
      - llama3.2:7b
";

    var tempConfigPath = Path.Combine(Path.GetTempPath(), "test-config.yml");
    await File.WriteAllTextAsync(tempConfigPath, configYaml);

    var configProvider = new YamlConfigurationProvider(tempConfigPath);
    var config = configProvider.LoadRoutingConfiguration();

    var mockProvider = Substitute.For<IModelProvider>();
    mockProvider.IsModelAvailable(Arg.Any<string>()).Returns(true);

    var modelRegistry = new ModelRegistry(new[] { mockProvider });
    var policy = new RoutingPolicy(config, modelRegistry, NullLogger<RoutingPolicy>.Instance);

    var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };

    // Act - Request models for each role
    var plannerDecision = policy.GetModel(AgentRole.Planner, context);
    var coderDecision = policy.GetModel(AgentRole.Coder, context);
    var reviewerDecision = policy.GetModel(AgentRole.Reviewer, context);

    // Assert - Should match configuration
    plannerDecision.ModelId.Should().Be("llama3.2:70b");
    coderDecision.ModelId.Should().Be("llama3.2:7b");
    reviewerDecision.ModelId.Should().Be("llama3.2:70b");

    // Cleanup
    File.Delete(tempConfigPath);
}
```

#### Test 17: Should Integrate with Model Registry for Availability

```csharp
[Fact]
public void Should_Integrate_With_Model_Registry_For_Availability()
{
    // Arrange - Create real model registry with multiple providers
    var ollamaProvider = Substitute.For<IModelProvider>();
    ollamaProvider.Name.Returns("Ollama");
    ollamaProvider.IsModelAvailable("llama3.2:7b").Returns(true);
    ollamaProvider.GetModelCapabilities("llama3.2:7b").Returns(new ModelCapabilities
    {
        ParameterCount = 7_000_000_000,
        IsLocal = true
    });

    var vllmProvider = Substitute.For<IModelProvider>();
    vllmProvider.Name.Returns("vLLM");
    vllmProvider.IsModelAvailable("llama3.2:70b@vllm").Returns(true);
    vllmProvider.GetModelCapabilities("llama3.2:70b@vllm").Returns(new ModelCapabilities
    {
        ParameterCount = 70_000_000_000,
        IsLocal = true
    });

    var modelRegistry = new ModelRegistry(new[] { ollamaProvider, vllmProvider });

    var configuration = new RoutingConfiguration
    {
        Strategy = RoutingStrategy.RoleBased,
        DefaultModel = "llama3.2:7b",
        RoleModels = new Dictionary<AgentRole, string>
        {
            { AgentRole.Planner, "llama3.2:70b@vllm" }
        }
    };

    var policy = new RoutingPolicy(configuration, modelRegistry, NullLogger<RoutingPolicy>.Instance);
    var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };

    // Act - Request planner model from vLLM provider
    var decision = policy.GetModel(AgentRole.Planner, context);

    // Assert - Should route to vLLM provider
    decision.ModelId.Should().Be("llama3.2:70b@vllm");
    decision.SelectedProvider.Should().Be("vLLM");

    vllmProvider.Received(1).IsModelAvailable("llama3.2:70b@vllm");
}
```

### E2E Tests - Complete Workflows

#### Test 18: Should Complete Multi-Stage Workflow with Different Models per Stage

```csharp
[Fact]
public async Task Should_Complete_Multi_Stage_Workflow_With_Different_Models_Per_Stage()
{
    // Arrange - Simulate complete agent workflow
    var configuration = new RoutingConfiguration
    {
        Strategy = RoutingStrategy.RoleBased,
        DefaultModel = "llama3.2:7b",
        RoleModels = new Dictionary<AgentRole, string>
        {
            { AgentRole.Planner, "llama3.2:70b" },
            { AgentRole.Coder, "llama3.2:7b" },
            { AgentRole.Reviewer, "llama3.2:70b" }
        }
    };

    var mockProvider = Substitute.For<IModelProvider>();
    mockProvider.IsModelAvailable(Arg.Any<string>()).Returns(true);

    var modelRegistry = new ModelRegistry(new[] { mockProvider });
    var policy = new RoutingPolicy(configuration, modelRegistry, NullLogger<RoutingPolicy>.Instance);

    var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };

    var stageModels = new List<string>();

    // Act - Simulate agent stages
    // Stage 1: Planning
    var plannerDecision = policy.GetModel(AgentRole.Planner, context);
    stageModels.Add(plannerDecision.ModelId);

    // Stage 2: Coding (multiple iterations)
    for (int i = 0; i < 5; i++)
    {
        var coderDecision = policy.GetModel(AgentRole.Coder, context);
        stageModels.Add(coderDecision.ModelId);
    }

    // Stage 3: Review
    var reviewerDecision = policy.GetModel(AgentRole.Reviewer, context);
    stageModels.Add(reviewerDecision.ModelId);

    // Assert - Should use large model for planning/review, small for coding
    stageModels[0].Should().Be("llama3.2:70b"); // Planner
    stageModels[1].Should().Be("llama3.2:7b");  // Coder iteration 1
    stageModels[2].Should().Be("llama3.2:7b");  // Coder iteration 2
    stageModels[3].Should().Be("llama3.2:7b");  // Coder iteration 3
    stageModels[4].Should().Be("llama3.2:7b");  // Coder iteration 4
    stageModels[5].Should().Be("llama3.2:7b");  // Coder iteration 5
    stageModels[6].Should().Be("llama3.2:70b"); // Reviewer
}
```

#### Test 19: Should Handle Model Restart During Workflow

```csharp
[Fact]
public void Should_Handle_Model_Restart_During_Workflow()
{
    // Arrange - Simulate model becoming unavailable mid-workflow
    var configuration = new RoutingConfiguration
    {
        Strategy = RoutingStrategy.Single,
        DefaultModel = "llama3.2:7b",
        FallbackChain = new List<string> { "llama3.2:7b", "mistral:7b" },
        AvailabilityCacheTTLSeconds = 0 // Disable caching for this test
    };

    var mockProvider = Substitute.For<IModelProvider>();

    // Model available for first two calls, then crashes, then restarts
    var callCount = 0;
    mockProvider.IsModelAvailable("llama3.2:7b").Returns(info =>
    {
        callCount++;
        return callCount <= 2 || callCount > 4; // Available initially, crashes on calls 3-4, then recovers
    });
    mockProvider.IsModelAvailable("mistral:7b").Returns(true); // Fallback always available

    var modelRegistry = new ModelRegistry(new[] { mockProvider });
    var policy = new RoutingPolicy(configuration, modelRegistry, NullLogger<RoutingPolicy>.Instance);

    var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };

    // Act - Make requests before, during, and after model crash
    var decision1 = policy.GetModel(AgentRole.Coder, context); // Should use llama3.2:7b
    var decision2 = policy.GetModel(AgentRole.Coder, context); // Should use llama3.2:7b
    var decision3 = policy.GetModel(AgentRole.Coder, context); // Should fall back to mistral:7b
    var decision4 = policy.GetModel(AgentRole.Coder, context); // Should fall back to mistral:7b
    var decision5 = policy.GetModel(AgentRole.Coder, context); // Should recover to llama3.2:7b

    // Assert - Should handle crash gracefully with fallback
    decision1.ModelId.Should().Be("llama3.2:7b");
    decision1.IsFallback.Should().BeFalse();

    decision2.ModelId.Should().Be("llama3.2:7b");
    decision2.IsFallback.Should().BeFalse();

    decision3.ModelId.Should().Be("mistral:7b");
    decision3.IsFallback.Should().BeTrue();

    decision4.ModelId.Should().Be("mistral:7b");
    decision4.IsFallback.Should().BeTrue();

    decision5.ModelId.Should().Be("llama3.2:7b");
    decision5.IsFallback.Should().BeFalse();
}
```

### Performance Tests

#### Test 20: Routing Decision Should Complete in Under 10 Milliseconds

```csharp
[Fact]
public void Routing_Decision_Should_Complete_In_Under_10_Milliseconds()
{
    // Arrange - Configure routing
    var configuration = new RoutingConfiguration
    {
        Strategy = RoutingStrategy.RoleBased,
        DefaultModel = "llama3.2:7b",
        RoleModels = new Dictionary<AgentRole, string>
        {
            { AgentRole.Planner, "llama3.2:70b" },
            { AgentRole.Coder, "llama3.2:7b" }
        }
    };

    var mockProvider = Substitute.For<IModelProvider>();
    mockProvider.IsModelAvailable(Arg.Any<string>()).Returns(true);

    var modelRegistry = new ModelRegistry(new[] { mockProvider });
    var policy = new RoutingPolicy(configuration, modelRegistry, NullLogger<RoutingPolicy>.Instance);

    var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };

    // Act - Measure routing decision time
    var stopwatch = Stopwatch.StartNew();
    var decision = policy.GetModel(AgentRole.Coder, context);
    stopwatch.Stop();

    // Assert - Should complete in under 10ms
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(10);
    decision.DecisionTimeMs.Should().BeLessThan(10);
}
```

#### Test 21: Availability Check Should Timeout After 5 Seconds

```csharp
[Fact]
public void Availability_Check_Should_Timeout_After_5_Seconds()
{
    // Arrange - Configure provider with slow availability check
    var configuration = new RoutingConfiguration
    {
        Strategy = RoutingStrategy.Single,
        DefaultModel = "llama3.2:7b",
        AvailabilityTimeoutSeconds = 5
    };

    var mockProvider = Substitute.For<IModelProvider>();
    mockProvider.IsModelAvailable("llama3.2:7b").Returns(info =>
    {
        Thread.Sleep(10000); // Simulate 10 second delay (exceeds timeout)
        return true;
    });

    var modelRegistry = new ModelRegistry(new[] { mockProvider });
    var policy = new RoutingPolicy(configuration, modelRegistry, NullLogger<RoutingPolicy>.Instance);

    var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };

    // Act - Attempt routing with slow provider
    var stopwatch = Stopwatch.StartNew();
    var exception = Assert.Throws<RoutingException>(() =>
        policy.GetModel(AgentRole.Coder, context)
    );
    stopwatch.Stop();

    // Assert - Should timeout after 5 seconds, not wait full 10 seconds
    stopwatch.Elapsed.TotalSeconds.Should().BeGreaterThan(4.5).And.BeLessThan(6);
    exception.Message.Should().Contain("timeout");
    exception.Message.Should().Contain("5 seconds");
}
```

#### Test 22: Policy Initialization Should Complete in Under 100 Milliseconds

```csharp
[Fact]
public void Policy_Initialization_Should_Complete_In_Under_100_Milliseconds()
{
    // Arrange - Prepare configuration and dependencies
    var configuration = new RoutingConfiguration
    {
        Strategy = RoutingStrategy.RoleBased,
        DefaultModel = "llama3.2:7b",
        RoleModels = new Dictionary<AgentRole, string>
        {
            { AgentRole.Planner, "llama3.2:70b" },
            { AgentRole.Coder, "llama3.2:7b" },
            { AgentRole.Reviewer, "llama3.2:70b" }
        },
        FallbackChain = new List<string> { "llama3.2:70b", "llama3.2:13b", "llama3.2:7b" }
    };

    var mockProvider = Substitute.For<IModelProvider>();
    mockProvider.IsModelAvailable(Arg.Any<string>()).Returns(true);

    var modelRegistry = new ModelRegistry(new[] { mockProvider });

    // Act - Measure policy initialization time
    var stopwatch = Stopwatch.StartNew();
    var policy = new RoutingPolicy(configuration, modelRegistry, NullLogger<RoutingPolicy>.Instance);
    stopwatch.Stop();

    // Assert - Should initialize quickly
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);

    // Verify policy is functional
    var decision = policy.GetModel(AgentRole.Coder, new RoutingContext { OperatingMode = OperatingMode.LocalOnly });
    decision.ModelId.Should().Be("llama3.2:7b");
}
```

#### Test 23: Cached Routing Decisions Should Complete in Under 1 Millisecond

```csharp
[Fact]
public void Cached_Routing_Decisions_Should_Complete_In_Under_1_Millisecond()
{
    // Arrange - Configure routing with caching enabled
    var configuration = new RoutingConfiguration
    {
        Strategy = RoutingStrategy.Single,
        DefaultModel = "llama3.2:7b",
        AvailabilityCacheTTLSeconds = 300 // 5 minute cache
    };

    var mockProvider = Substitute.For<IModelProvider>();
    mockProvider.IsModelAvailable("llama3.2:7b").Returns(true);

    var modelRegistry = new ModelRegistry(new[] { mockProvider });
    var policy = new RoutingPolicy(configuration, modelRegistry, NullLogger<RoutingPolicy>.Instance);

    var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };

    // Prime the cache
    policy.GetModel(AgentRole.Coder, context);

    // Act - Measure cached routing decision time
    var stopwatch = Stopwatch.StartNew();
    var decision = policy.GetModel(AgentRole.Coder, context);
    stopwatch.Stop();

    // Assert - Cached lookup should be sub-millisecond
    stopwatch.Elapsed.TotalMilliseconds.Should().BeLessThan(1);
    decision.ModelId.Should().Be("llama3.2:7b");

    // Verify cache was used (provider not called again)
    mockProvider.Received(1).IsModelAvailable("llama3.2:7b");
}
```

---

## Best Practices

### Model Selection

- **BP-001: Start with Single Model Strategy** - Begin development with a single-model configuration to simplify debugging and establish baseline behavior. Switch to role-based or adaptive strategies only after confirming the routing system works correctly.

- **BP-002: Use Large Models for Planning** - Planning tasks benefit disproportionately from large model capabilities. Configure llama3.2:70b or equivalent for the planner role even if using smaller models for other roles. The planning phase occurs once per workflow, so performance cost is minimal while quality gains are significant.

- **BP-003: Use Fast Models for Coding** - Most coding tasks involve pattern matching and syntax generation rather than deep reasoning. Configure llama3.2:7b or llama3.2:13b for the coder role to maximize throughput. Reserve large models for complex algorithmic implementations.

- **BP-004: Match Reviewer Model to Risk Tolerance** - High-stakes projects (production code, security-sensitive features) should use large models (70B) for review to maximize bug detection. Low-stakes projects (prototypes, documentation) can use medium models (13B) for faster review cycles.

- **BP-005: Benchmark Before Optimizing** - Measure actual routing overhead, model performance, and quality metrics before optimizing configuration. Premature optimization often wastes time on non-bottlenecks. Use structured logging to identify which roles dominate inference time.

- **BP-006: Document Routing Rationale** - Include comments in .agent/config.yml explaining why each model was chosen for each role. Document performance vs quality tradeoffs. This context helps future maintainers understand configuration decisions.

### Cost Management

- **BP-007: Monitor GPU Memory Utilization** - Track GPU memory usage across all loaded models. If utilization exceeds eighty percent, consider consolidating to fewer model sizes or using fallback chains more aggressively to enable model unloading.

- **BP-008: Profile Inference Latency** - Measure end-to-end latency for each agent role. If coder role latency exceeds user tolerance (typically ten to twenty seconds), downgrade to smaller models. If planner quality is poor, upgrade to larger models despite latency cost.

- **BP-009: Optimize for Dominant Workload** - If your workflows are planning-heavy (research, design), invest in large planner models. If workflows are coding-heavy (implementation, refactoring), invest in fast coder models. Routing configuration should reflect actual usage patterns.

- **BP-010: Use Fallback Chains for Resource Constraints** - Teams with limited GPU resources should configure aggressive fallback chains. Primary models can be aspirational (70B), fallback chain ensures degraded functionality when resources are constrained. This enables shared infrastructure across heterogeneous hardware.

- **BP-011: Batch Similar Tasks** - When processing multiple tasks, group by complexity to minimize routing overhead and model loading/unloading. Process all simple tasks with 7B model, then all complex tasks with 70B model, rather than alternating.

- **BP-012: Avoid Premature Fallback** - Configure fallback chains thoughtfully to prevent quality degradation. Fallback should be for exceptional conditions (model crash, resource exhaustion), not normal operation. If fallback triggers frequently, configuration needs adjustment not fallback expansion.

### Fallback Strategies

- **BP-013: Order Fallback by Quality** - Fallback chains should degrade gradually from highest quality to lowest quality. Never configure a fallback chain that escalates to more expensive models—this amplifies cost under failure conditions.

- **BP-014: Include Minimal Viable Model** - Every fallback chain should terminate with a minimal viable model that works on constrained hardware. This ensures some functionality even when premium models are unavailable. Typically llama3.2:7b or mistral:7b.

- **BP-015: Test Fallback Paths** - Regularly test fallback behavior by intentionally stopping primary models. Verify fallback triggers correctly, logs warnings, and produces acceptable quality. Untested fallback paths fail when you need them most.

- **BP-016: Monitor Fallback Frequency** - Track how often fallback occurs in production. Frequent fallback indicates misconfiguration (unreliable primary model, insufficient resources, unrealistic configuration). Investigate and fix root cause rather than accepting degraded mode as normal.

- **BP-017: Configure Provider-Specific Fallbacks** - If using multiple providers (Ollama, vLLM), configure fallback chains that try alternative providers, not just alternative models. Provider-level failures (server down, network issue) require provider fallback.

- **BP-018: Document Fallback Behavior** - Include comments in configuration explaining expected fallback scenarios and acceptable quality degradation. Document decision criteria for when fallback is acceptable vs when workflows should fail-stop.

### Performance

- **BP-019: Enable Availability Caching** - Configure five-second availability cache TTL to reduce provider query overhead. Disable caching only for testing or debugging availability issues. Cached lookups are two orders of magnitude faster than live queries.

- **BP-020: Profile Routing Decision Time** - Routing decisions should complete in under ten milliseconds. If routing overhead exceeds budget, investigate slow availability checks, inefficient fallback chain traversal, or excessive logging. Use performance tests to detect regressions.

- **BP-021: Minimize Fallback Chain Length** - Long fallback chains (more than five models) add latency because each unavailable model requires availability check before trying next. Keep chains short and focused on realistic failure scenarios.

- **BP-022: Use Explicit Provider Qualifiers** - When configuration specifies models with explicit providers (llama3.2:7b@ollama), routing skips provider discovery and routes directly. This eliminates provider enumeration overhead.

- **BP-023: Avoid Dynamic Complexity Heuristics** - Task complexity estimation can be expensive if it requires parsing code, analyzing dependencies, or calling external services. Prefer static complexity classification (user-specified, simple heuristics) to minimize routing overhead.

- **BP-024: Batch Routing Decisions** - If agent workflow requires routing multiple roles sequentially, consider caching all routing decisions upfront rather than calling routing policy separately for each stage. This amortizes availability check overhead.

---

## Troubleshooting

### Issue 1: Wrong Model Selected for Role

**Symptoms:**
- Planner role uses small model instead of configured large model
- Routing decision logs show unexpected model selection
- Quality degradation in planning or review phases

**Causes:**
- Configuration typo in role_models mapping (e.g., "plannner" instead of "planner")
- Primary model unavailable, fallback chain selected alternative
- User override in routing context bypassed configuration
- Operating mode constraint rejected configured model

**Solutions:**
- Verify role_models configuration with `acode models routing` command
- Check routing logs for fallback events: `grep "fallback" .agent/logs/routing.log`
- Validate model availability: `acode models test <role>`
- Confirm operating mode allows configured model (no cloud models in local-only mode)
- Remove user overrides from routing context if not intended

**Prevention:**
- Use role-based configuration validation during startup
- Enable routing decision logging at INFO level
- Monitor fallback frequency metrics
- Include model availability checks in pre-flight validation

### Issue 2: Fallback Not Working

**Symptoms:**
- Routing fails with "no available model" error despite fallback chain configuration
- Primary model failure causes workflow failure instead of degradation
- Fallback events not logged in routing logs

**Causes:**
- Fallback chain contains only unavailable models
- Operating mode constraints reject all fallback models (e.g., all models are remote in local-only mode)
- Availability timeout too aggressive, marks available models as unavailable
- Fallback chain syntax error in configuration (not parsed correctly)

**Solutions:**
- Verify at least one fallback model is available: `ollama list`
- Check configuration syntax: `acode config validate`
- Increase availability timeout from five seconds to ten seconds if provider is slow
- Test fallback explicitly: stop primary model and verify fallback triggers
- Review operating mode configuration for compatibility with fallback models

**Prevention:**
- Include minimal viable model in every fallback chain (7B model runs on all hardware)
- Test fallback paths regularly as part of deployment validation
- Monitor fallback frequency to detect configuration drift
- Use integration tests that simulate primary model failure

### Issue 3: Routing Decisions Too Slow

**Symptoms:**
- Routing decision time exceeds ten milliseconds consistently
- User-visible latency before inference starts
- Performance test failures for routing overhead

**Causes:**
- Availability caching disabled, every routing decision queries provider
- Provider availability check slow (network latency, server overload)
- Fallback chain too long, multiple unavailability checks per decision
- Logging overhead from verbose DEBUG-level routing logs
- Complex adaptive strategy heuristics with expensive computation

**Solutions:**
- Enable availability caching with five-second TTL
- Reduce fallback chain length to three or four models
- Decrease logging verbosity to INFO level (disable DEBUG)
- Profile routing code to identify bottleneck (availability check, fallback traversal, logging)
- Simplify or disable adaptive strategy if heuristics are expensive
- Add performance regression tests to catch slowdowns early

**Prevention:**
- Maintain routing decision budget of ten milliseconds in performance tests
- Use availability caching by default
- Keep fallback chains short (three to five models maximum)
- Profile routing overhead during development, not after deployment

### Issue 4: Model Availability Check Timeouts

**Symptoms:**
- Routing fails with "availability check timeout" errors
- Five-second timeout expires before provider responds
- Models are available but routing claims they are not

**Causes:**
- Model provider slow to respond (server overload, network latency, cold start)
- Availability timeout configured too aggressively (less than five seconds)
- Provider implementation has synchronous blocking operation
- Network issues between application and provider endpoint

**Solutions:**
- Increase availability timeout to ten or fifteen seconds for slow providers
- Investigate provider performance: check server logs, CPU/GPU utilization
- Implement circuit breaker to fail fast on consistently unavailable providers
- Use local providers (Ollama on localhost) instead of remote providers to eliminate network latency
- Add retry logic with exponential backoff for transient failures

**Prevention:**
- Monitor provider response time metrics
- Use local providers for latency-sensitive deployments
- Configure reasonable availability timeout (five to ten seconds)
- Implement health checks that validate provider performance before routing

### Issue 5: Configuration Changes Not Taking Effect

**Symptoms:**
- Modified .agent/config.yml but routing still uses old configuration
- Model assignments unchanged after configuration update
- Routing logs show old model selections

**Causes:**
- Application not restarted after configuration change (hot-reload not implemented)
- Configuration caching prevents reload
- Syntax error in new configuration, system falls back to cached valid configuration
- File system notification delay, application has not detected change yet

**Solutions:**
- Restart application to force configuration reload: `acode restart`
- Validate configuration syntax: `acode config validate`
- Check configuration file permissions (must be readable by application)
- Monitor configuration reload logs: `grep "configuration reloaded" .agent/logs/app.log`
- Manually trigger reload if hot-reload supported: `acode config reload`

**Prevention:**
- Implement configuration validation on save (fail fast on syntax errors)
- Add configuration reload notification (log message confirming new config active)
- Use version hashes in routing decision logs to verify which configuration version was used
- Include configuration timestamp in routing decision metadata

---

## User Verification Steps

### Scenario 1: Single Model Strategy Configuration and Verification

**Objective:** Verify that single model strategy routes all agent roles to the same configured model, maintaining consistency across workflow stages.

**Steps:**
1. Edit .agent/config.yml and configure single model strategy:
   ```yaml
   models:
     routing:
       strategy: single
       default_model: llama3.2:70b
   ```
2. Start Ollama and load the model: `ollama run llama3.2:70b`
3. Display current routing configuration: `acode models routing`
4. Verify output shows strategy: single, default model: llama3.2:70b
5. Verify all roles (planner, coder, reviewer) map to llama3.2:70b
6. Test routing for planner role: `acode models test planner`
7. Verify output: "Selected model: llama3.2:70b (strategy: single)"
8. Test routing for coder role: `acode models test coder`
9. Verify output shows same model: llama3.2:70b
10. Test routing for reviewer role: `acode models test reviewer`
11. Verify output shows same model: llama3.2:70b
12. Run sample agent workflow: `acode run "Add hello world function"`
13. Check routing logs: `grep "routing decision" .agent/logs/routing.log`
14. Verify all routing decisions selected llama3.2:70b regardless of role
15. Confirm workflow completed successfully with consistent model usage

**Expected Outcome:** All agent roles use llama3.2:70b. No fallback events. Routing decisions logged consistently. Workflow completes without model switching.

### Scenario 2: Role-Based Strategy with Different Models per Role

**Objective:** Verify that role-based strategy assigns different models to different agent roles based on configuration, optimizing for role-specific requirements.

**Steps:**
1. Edit .agent/config.yml for role-based routing:
   ```yaml
   models:
     routing:
       strategy: role-based
       default_model: llama3.2:7b
       role_models:
         planner: llama3.2:70b
         coder: llama3.2:7b
         reviewer: llama3.2:70b
   ```
2. Start required models: `ollama run llama3.2:70b` and `ollama run llama3.2:7b`
3. Display routing configuration: `acode models routing`
4. Verify strategy shows as "role-based"
5. Verify role assignments show planner→70b, coder→7b, reviewer→70b
6. Test planner routing: `acode models test planner`
7. Verify result: "Selected model: llama3.2:70b (strategy: role-based, role: planner)"
8. Test coder routing: `acode models test coder`
9. Verify result: "Selected model: llama3.2:7b (strategy: role-based, role: coder)"
10. Test reviewer routing: `acode models test reviewer`
11. Verify result: "Selected model: llama3.2:70b (strategy: role-based, role: reviewer)"
12. Run agent workflow: `acode run "Implement user authentication"`
13. Monitor routing logs in real-time: `tail -f .agent/logs/routing.log`
14. Verify planner phase logs show llama3.2:70b selection
15. Verify coder phase logs show llama3.2:7b selection
16. Verify reviewer phase logs show llama3.2:70b selection
17. Confirm workflow completed with role-appropriate model assignments
18. Check performance: coder phase should complete faster than planner/reviewer phases

**Expected Outcome:** Different models assigned per role. Planner and reviewer use 70B. Coder uses 7B. Performance optimization visible in coder latency. Quality maintained across all phases.

### Scenario 3: Fallback Chain Activation When Primary Model Unavailable

**Objective:** Verify that routing policy gracefully degrades to fallback models when primary model is unavailable, maintaining workflow continuity with acceptable quality degradation.

**Steps:**
1. Configure routing with fallback chain:
   ```yaml
   models:
     routing:
       strategy: single
       default_model: llama3.2:70b
       fallback_chain:
         - llama3.2:70b
         - llama3.2:13b
         - llama3.2:7b
   ```
2. Start all fallback models: `ollama run llama3.2:70b`, `ollama run llama3.2:13b`, `ollama run llama3.2:7b`
3. Verify all models loaded: `ollama list`
4. Test routing with all models available: `acode models test coder`
5. Verify primary model selected: llama3.2:70b
6. Stop primary model: `ollama stop llama3.2:70b`
7. Verify primary model stopped: `ollama list` (should not show 70b)
8. Test routing again: `acode models test coder`
9. Verify fallback triggered: "Selected model: llama3.2:13b (fallback: primary unavailable)"
10. Check routing logs for fallback event: `grep "fallback" .agent/logs/routing.log`
11. Verify WARNING level log entry indicating primary unavailable
12. Run agent workflow: `acode run "Add input validation"`
13. Verify workflow completes successfully using fallback model
14. Stop second fallback model: `ollama stop llama3.2:13b`
15. Test routing: `acode models test coder`
16. Verify third model selected: llama3.2:7b
17. Restart primary model: `ollama run llama3.2:70b`
18. Wait five seconds for availability cache to expire
19. Test routing: `acode models test coder`
20. Verify routing recovered to primary model: llama3.2:70b

**Expected Outcome:** Fallback chain traversed sequentially. First available model selected. Fallback logged at WARNING level. Workflow continues with degraded quality. Recovery to primary model after restart.

### Scenario 4: Error Handling When All Models Unavailable

**Objective:** Verify that routing policy fails gracefully with helpful error messages when all configured models in fallback chain are unavailable.

**Steps:**
1. Configure routing with fallback chain:
   ```yaml
   models:
     routing:
       strategy: single
       default_model: llama3.2:70b
       fallback_chain:
         - llama3.2:70b
         - llama3.2:7b
   ```
2. Verify no models are currently running: `ollama list` (should be empty)
3. Attempt routing test: `acode models test coder`
4. Verify error message received
5. Confirm error includes: "ACODE-RTE-004: Fallback chain exhausted"
6. Confirm error lists attempted models: llama3.2:70b, llama3.2:7b
7. Confirm error includes suggestion: "Start a model with 'ollama run llama3.2:7b'"
8. Verify routing logs show all availability checks failed
9. Attempt agent workflow: `acode run "Add logging"`
10. Verify workflow fails immediately with same error
11. Follow suggestion and start minimal model: `ollama run llama3.2:7b`
12. Retry routing test: `acode models test coder`
13. Verify successful routing to llama3.2:7b
14. Retry agent workflow
15. Verify workflow now completes successfully

**Expected Outcome:** Clear error message when all models unavailable. Error includes attempted models and remediation suggestion. Workflow fails fast rather than hanging. Recovery works immediately after starting suggested model.

### Scenario 5: Operating Mode Constraint Enforcement

**Objective:** Verify that routing policy enforces operating mode constraints, rejecting models that violate mode restrictions (e.g., cloud models in local-only mode).

**Steps:**
1. Configure local-only operating mode:
   ```yaml
   operating_mode: local-only
   models:
     routing:
       strategy: single
       default_model: gpt-4
   ```
2. Attempt routing test: `acode models test coder`
3. Verify error received: "ACODE-RTE-003: Mode constraint violation"
4. Confirm error message includes: "Model 'gpt-4' not allowed in local-only mode"
5. Confirm error suggests: "Use a local model or change operating mode"
6. Check routing logs for mode constraint check
7. Modify configuration to use local model:
   ```yaml
   operating_mode: local-only
   models:
     routing:
       strategy: single
       default_model: llama3.2:7b
   ```
8. Start local model: `ollama run llama3.2:7b`
9. Retry routing test: `acode models test coder`
10. Verify successful routing to llama3.2:7b
11. Verify logs show mode constraint check passed
12. Change to air-gapped mode and configure remote Ollama:
    ```yaml
    operating_mode: air-gapped
    models:
      providers:
        - type: ollama
          endpoint: http://remote-server:11434
    ```
13. Attempt routing test
14. Verify error: remote provider rejected in air-gapped mode
15. Change back to local-only mode with local provider
16. Verify routing works correctly

**Expected Outcome:** Operating mode constraints enforced before routing. Cloud models rejected in local-only mode. Remote providers rejected in air-gapped mode. Clear error messages guide remediation.

### Scenario 6: CLI Routing Commands Display Configuration and Status

**Objective:** Verify that CLI commands provide visibility into routing configuration, model assignments, and availability status for operational monitoring.

**Steps:**
1. Configure role-based routing with fallback:
   ```yaml
   models:
     routing:
       strategy: role-based
       default_model: llama3.2:7b
       role_models:
         planner: llama3.2:70b
         coder: llama3.2:7b
         reviewer: llama3.2:70b
       fallback_chain:
         - llama3.2:70b
         - llama3.2:13b
         - llama3.2:7b
   ```
2. Start only 70b and 7b models (omit 13b): `ollama run llama3.2:70b`, `ollama run llama3.2:7b`
3. Run routing status command: `acode models routing`
4. Verify output shows strategy: role-based
5. Verify output shows default model: llama3.2:7b
6. Verify role assignments section shows:
   - planner → llama3.2:70b (available)
   - coder → llama3.2:7b (available)
   - reviewer → llama3.2:70b (available)
7. Verify fallback chain section shows:
   - 1. llama3.2:70b (available)
   - 2. llama3.2:13b (not loaded)
   - 3. llama3.2:7b (available)
8. Test specific role routing: `acode models test planner`
9. Verify output shows detailed routing decision:
   - Testing routing for role 'planner'
   - Primary model: llama3.2:70b
   - Status: Available
   - Selection: llama3.2:70b (strategy: role-based)
10. Stop 70b model: `ollama stop llama3.2:70b`
11. Re-run routing status: `acode models routing`
12. Verify role assignments now show (unavailable) for planner and reviewer
13. Verify fallback chain shows llama3.2:70b (not loaded)
14. Test planner routing: `acode models test planner`
15. Verify output shows fallback decision:
    - Primary model: llama3.2:70b (unavailable)
    - Fallback model: llama3.2:7b
    - Selection: llama3.2:7b (fallback: primary unavailable)

**Expected Outcome:** CLI commands provide comprehensive visibility. Configuration displayed accurately. Availability status real-time. Fallback chain shows per-model status. Test command demonstrates actual routing behavior.

### Scenario 7: User Override Forces Specific Model Selection

**Objective:** Verify that user can override routing policy decisions for specific workflows, bypassing configured strategy while still respecting operating mode constraints.

**Steps:**
1. Configure role-based routing:
   ```yaml
   models:
     routing:
       strategy: role-based
       default_model: llama3.2:7b
       role_models:
         coder: llama3.2:7b
   ```
2. Start both small and large models: `ollama run llama3.2:7b`, `ollama run llama3.2:70b`
3. Run normal workflow without override: `acode run "Add error handling"`
4. Verify routing logs show llama3.2:7b for coder role (per configuration)
5. Run workflow with explicit model override: `acode run --model llama3.2:70b "Fix complex race condition"`
6. Verify routing logs show user override detected
7. Verify llama3.2:70b used for coder role despite configuration specifying 7b
8. Verify override logged: "User override detected, forcing model llama3.2:70b"
9. Check that override applies to all roles in that workflow
10. Run another workflow without override: `acode run "Add docstrings"`
11. Verify routing reverts to configured behavior (llama3.2:7b)
12. Attempt override with cloud model in local-only mode: `acode run --model gpt-4 "test"`
13. Verify override rejected: mode constraint violation error
14. Confirm error message: "Model 'gpt-4' not allowed in local-only mode (even with user override)"
15. Verify security boundary maintained despite user override

**Expected Outcome:** User override works for forcing specific model selection. Override bypasses routing strategy but respects operating mode constraints. Override applies only to single workflow. Normal routing resumes after override workflow completes.

### Scenario 8: Performance Validation of Routing Decision Latency

**Objective:** Verify that routing decisions complete within performance budget (under ten milliseconds) to maintain acceptable user experience.

**Steps:**
1. Configure simple single-model routing:
   ```yaml
   models:
     routing:
       strategy: single
       default_model: llama3.2:7b
       availability_cache_ttl_seconds: 5
   ```
2. Start model: `ollama run llama3.2:7b`
3. Run routing performance test: `acode models test coder --performance`
4. Verify output includes decision time: "Decision time: X ms"
5. Confirm decision time under ten milliseconds
6. Run multiple iterations to warm caches: repeat test ten times
7. Verify cached decisions complete in under one millisecond
8. Configure complex role-based routing with fallback:
   ```yaml
   models:
     routing:
       strategy: role-based
       default_model: llama3.2:7b
       role_models:
         planner: llama3.2:70b
         coder: llama3.2:13b
         reviewer: llama3.2:70b
       fallback_chain:
         - llama3.2:70b
         - llama3.2:13b
         - llama3.2:7b
   ```
9. Start all models
10. Run performance test again: `acode models test planner --performance`
11. Verify decision time still under ten milliseconds despite complexity
12. Stop primary model to force fallback
13. Run performance test: `acode models test planner --performance`
14. Verify fallback decision time under fifteen milliseconds (slightly higher due to fallback chain traversal)
15. Check routing logs for decision time statistics

**Expected Outcome:** Routing decisions consistently complete within performance budget. Cached decisions sub-millisecond. Uncached decisions under ten milliseconds. Fallback decisions under fifteen milliseconds. No performance regression from configuration complexity.

### Scenario 9: Configuration Hot-Reload Without Application Restart

**Objective:** Verify that routing configuration changes are detected and applied without requiring application restart, enabling live tuning during development.

**Steps:**
1. Start application with initial configuration:
   ```yaml
   models:
     routing:
       strategy: single
       default_model: llama3.2:7b
   ```
2. Start model: `ollama run llama3.2:7b`
3. Verify routing: `acode models routing` shows single strategy with 7b model
4. Edit configuration without stopping application:
   ```yaml
   models:
     routing:
       strategy: role-based
       default_model: llama3.2:7b
       role_models:
         planner: llama3.2:70b
   ```
5. Start additional model: `ollama run llama3.2:70b`
6. Wait for file system notification (typically under one second)
7. Check application logs: `grep "configuration reloaded" .agent/logs/app.log`
8. Verify routing configuration reloaded: `acode models routing`
9. Confirm strategy now shows "role-based"
10. Confirm planner role shows llama3.2:70b
11. Test routing: `acode models test planner`
12. Verify new configuration active without restart
13. Make invalid configuration change (syntax error):
    ```yaml
    models:
      routing:
        strategy: invalid-strategy
    ```
14. Wait for reload attempt
15. Check logs for validation error: `grep "configuration validation failed" .agent/logs/app.log`
16. Verify application continues using last valid configuration
17. Run routing test: `acode models test planner`
18. Confirm still using previous valid configuration (not broken config)
19. Fix configuration syntax
20. Verify successful reload after fix

**Expected Outcome:** Configuration changes detected via file system notifications. Valid configurations applied without restart. Invalid configurations rejected, application continues with last valid config. Reload process logged for visibility.

### Scenario 10: Multi-Provider Routing with Provider Fallback

**Objective:** Verify that routing policy can select models from different providers (Ollama, vLLM) and fall back across providers when individual provider fails.

**Steps:**
1. Configure multi-provider setup:
   ```yaml
   models:
     providers:
       - type: ollama
         endpoint: http://localhost:11434
       - type: vllm
         endpoint: http://localhost:8000
     routing:
       strategy: role-based
       default_model: llama3.2:7b@ollama
       role_models:
         planner: llama3.2:70b@vllm
         coder: llama3.2:7b@ollama
       fallback_chain:
         - llama3.2:70b@vllm
         - llama3.2:70b@ollama
         - llama3.2:7b@ollama
   ```
2. Start models on both providers:
   - Ollama: `ollama run llama3.2:7b`, `ollama run llama3.2:70b`
   - vLLM: Start vLLM server with llama3.2:70b
3. Verify routing status: `acode models routing`
4. Confirm planner role shows llama3.2:70b@vllm (available)
5. Confirm coder role shows llama3.2:7b@ollama (available)
6. Test planner routing: `acode models test planner`
7. Verify selection: llama3.2:70b@vllm (provider: vLLM)
8. Stop vLLM server
9. Wait for availability cache expiry (five seconds)
10. Test planner routing again: `acode models test planner`
11. Verify fallback to llama3.2:70b@ollama (provider: Ollama)
12. Verify logs show provider-level fallback: "Primary model llama3.2:70b@vllm unavailable (provider vLLM down), trying fallback llama3.2:70b@ollama"
13. Restart vLLM server
14. Wait for cache expiry
15. Test routing: `acode models test planner`
16. Verify recovery to primary: llama3.2:70b@vllm

**Expected Outcome:** Routing works across multiple providers. Provider-specific model selection honored. Fallback works across providers when primary provider fails. Provider failover logged clearly. Recovery automatic after provider restart.

---

## Implementation Prompt

You are implementing the Model Routing Policy system for Acode (Task 009). This system determines which model handles which agent role (planner, coder, reviewer) based on configuration, availability, and operating mode constraints. Follow Test-Driven Development strictly.

### File Structure

Create the following files in the specified locations:

```
src/Acode.Application/Routing/
├── IRoutingPolicy.cs                  # Core routing interface
├── AgentRole.cs                       # Enum for agent roles
├── RoutingContext.cs                  # Context for routing decisions
├── RoutingDecision.cs                 # Result of routing decision
├── RoutingConfiguration.cs            # Configuration data class
├── RoutingStrategy.cs                 # Enum for routing strategies
├── TaskComplexity.cs                  # Enum for complexity levels
└── RoutingException.cs                # Custom exception type

src/Acode.Infrastructure/Routing/
├── RoutingPolicy.cs                   # Main routing implementation
├── SingleModelStrategy.cs             # Single model strategy
├── RoleBasedStrategy.cs               # Role-based strategy
├── AdaptiveStrategy.cs                # Adaptive strategy (future)
├── FallbackHandler.cs                 # Fallback chain logic
├── AvailabilityChecker.cs             # Model availability checks
├── ModelRegistry.cs                   # Model registry and cache
└── ConfigurationValidator.cs          # Configuration validation

tests/Acode.Infrastructure.Tests/Routing/
├── RoutingPolicyTests.cs              # Unit tests for routing
├── FallbackHandlerTests.cs            # Unit tests for fallback
├── RoutingIntegrationTests.cs         # Integration tests
└── RoutingPerformanceTests.cs         # Performance benchmarks
```

### Complete Implementation Code

#### File 1: IRoutingPolicy.cs (Application Layer)

```csharp
namespace Acode.Application.Routing;

using System.Collections.Generic;

/// <summary>
/// Defines the contract for routing agent roles to appropriate models based on
/// configuration, availability, and operating mode constraints.
/// </summary>
/// <remarks>
/// The routing policy enables heterogeneous model usage—different models for
/// different agent roles. This optimizes for both quality (large models for planning)
/// and performance (small models for coding). The policy respects operating mode
/// constraints and handles model unavailability through fallback chains.
/// </remarks>
public interface IRoutingPolicy
{
    /// <summary>
    /// Selects the appropriate model for the specified agent role and context.
    /// </summary>
    /// <param name="role">The agent role requesting a model (planner, coder, reviewer).</param>
    /// <param name="context">Context for routing decision including operating mode and complexity.</param>
    /// <returns>
    /// A RoutingDecision containing the selected model ID, fallback status, and selection reason.
    /// </returns>
    /// <exception cref="RoutingException">
    /// Thrown when no available model satisfies the routing constraints (operating mode,
    /// capabilities, availability). Error includes attempted models and remediation suggestion.
    /// </exception>
    /// <example>
    /// <code>
    /// var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };
    /// var decision = routingPolicy.GetModel(AgentRole.Planner, context);
    /// // decision.ModelId might be "llama3.2:70b"
    /// </code>
    /// </example>
    RoutingDecision GetModel(AgentRole role, RoutingContext context);

    /// <summary>
    /// Attempts to find a fallback model when the primary model is unavailable.
    /// </summary>
    /// <param name="role">The agent role requesting a fallback model.</param>
    /// <param name="context">Context for fallback decision.</param>
    /// <returns>A RoutingDecision indicating the fallback model or null if no fallback available.</returns>
    RoutingDecision? GetFallbackModel(AgentRole role, RoutingContext context);

    /// <summary>
    /// Checks whether the specified model is currently available for inference.
    /// </summary>
    /// <param name="modelId">The model identifier in name:tag or name:tag@provider format.</param>
    /// <returns>True if the model is loaded and ready for inference, false otherwise.</returns>
    /// <remarks>
    /// Availability checks are cached for performance (default 5 second TTL). This method
    /// queries the model provider registry and returns cached results when available.
    /// </remarks>
    bool IsModelAvailable(string modelId);

    /// <summary>
    /// Returns a list of all models currently available across all registered providers.
    /// </summary>
    /// <returns>Read-only list of available models with metadata (parameter count, capabilities).</returns>
    IReadOnlyList<ModelInfo> ListAvailableModels();
}

/// <summary>
/// Represents metadata about an available model.
/// </summary>
public sealed class ModelInfo
{
    /// <summary>Gets the model identifier (e.g., "llama3.2:70b").</summary>
    public required string ModelId { get; init; }

    /// <summary>Gets the provider hosting this model (e.g., "Ollama", "vLLM").</summary>
    public required string Provider { get; init; }

    /// <summary>Gets whether this model is hosted locally (true) or remotely (false).</summary>
    public required bool IsLocal { get; init; }

    /// <summary>Gets the parameter count of the model.</summary>
    public required long ParameterCount { get; init; }

    /// <summary>Gets whether this model supports tool calling.</summary>
    public required bool SupportsToolCalling { get; init; }

    /// <summary>Gets whether this model is currently loaded and available.</summary>
    public required bool IsAvailable { get; init; }
}
```

#### File 2: AgentRole.cs (Application Layer)

```csharp
namespace Acode.Application.Routing;

/// <summary>
/// Defines the agent roles supported by the routing policy.
/// </summary>
/// <remarks>
/// Each role has different model requirements. Planner requires strong reasoning,
/// coder requires precise instruction following, reviewer requires critical analysis.
/// </remarks>
public enum AgentRole
{
    /// <summary>
    /// Default role when no specific role is assigned. Uses default_model from configuration.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Planning role—breaks down high-level tasks into actionable steps. Requires strong
    /// reasoning capabilities. Typically assigned to large models (70B parameters).
    /// </summary>
    Planner = 1,

    /// <summary>
    /// Coding role—implements concrete code changes. Requires precise instruction following
    /// and code syntax knowledge. Typically assigned to medium or small models (7B-13B parameters).
    /// </summary>
    Coder = 2,

    /// <summary>
    /// Reviewer role—verifies correctness and provides feedback. Requires critical analysis
    /// and edge case identification. Typically assigned to large models (70B parameters).
    /// </summary>
    Reviewer = 3
}
```

#### File 3: RoutingContext.cs (Application Layer)

```csharp
namespace Acode.Application.Routing;

using System.Collections.Generic;
using Acode.Domain.OperatingModes;

/// <summary>
/// Provides context for routing decisions including operating mode constraints,
/// task complexity, and user overrides.
/// </summary>
public sealed class RoutingContext
{
    /// <summary>
    /// Gets or sets the operating mode constraint (local-only, air-gapped, burst).
    /// Routing policy enforces mode constraints before model selection.
    /// </summary>
    public required OperatingMode OperatingMode { get; init; }

    /// <summary>
    /// Gets or sets the estimated task complexity. Used by adaptive routing strategy
    /// to select model based on difficulty.
    /// </summary>
    public TaskComplexity? TaskComplexity { get; init; }

    /// <summary>
    /// Gets or sets the estimated token count for the task. Used for complexity estimation.
    /// </summary>
    public int? EstimatedTokenCount { get; init; }

    /// <summary>
    /// Gets or sets user-specified model override. When set, routing policy bypasses
    /// configured strategy and uses this model (subject to operating mode constraints).
    /// </summary>
    public string? UserOverride { get; init; }

    /// <summary>
    /// Gets or sets required model capabilities for this task (e.g., tool calling, structured output).
    /// Routing policy only selects models that support all required capabilities.
    /// </summary>
    public IReadOnlyList<ModelCapability>? RequiredCapabilities { get; init; }
}

/// <summary>
/// Defines model capabilities that tasks may require.
/// </summary>
public enum ModelCapability
{
    /// <summary>Model supports tool calling protocol.</summary>
    ToolCalling,

    /// <summary>Model supports function calling protocol.</summary>
    FunctionCalling,

    /// <summary>Model supports structured output (JSON mode).</summary>
    StructuredOutput
}
```

#### File 4: RoutingDecision.cs (Application Layer)

```csharp
namespace Acode.Application.Routing;

/// <summary>
/// Represents the result of a routing decision, including the selected model,
/// fallback status, and decision reasoning.
/// </summary>
/// <remarks>
/// Routing decisions are immutable value objects. They can be safely logged,
/// cached, and passed between components without defensive copying.
/// </remarks>
public sealed class RoutingDecision
{
    /// <summary>
    /// Gets the selected model identifier (e.g., "llama3.2:70b" or "llama3.2:70b@ollama").
    /// </summary>
    public required string ModelId { get; init; }

    /// <summary>
    /// Gets whether this decision represents a fallback selection (primary model unavailable).
    /// </summary>
    public required bool IsFallback { get; init; }

    /// <summary>
    /// Gets the reason for fallback (only populated when IsFallback is true).
    /// Examples: "primary_unavailable", "mode_constraint_violation", "capability_mismatch".
    /// </summary>
    public string? FallbackReason { get; init; }

    /// <summary>
    /// Gets a human-readable explanation of why this model was selected.
    /// Examples: "role-based strategy", "user override", "adaptive strategy (high complexity)".
    /// </summary>
    public required string SelectionReason { get; init; }

    /// <summary>
    /// Gets the provider hosting the selected model (e.g., "Ollama", "vLLM").
    /// </summary>
    public string? SelectedProvider { get; init; }

    /// <summary>
    /// Gets the time taken to make this routing decision in milliseconds.
    /// Used for performance monitoring and optimization.
    /// </summary>
    public required long DecisionTimeMs { get; init; }

    /// <summary>
    /// Gets the timestamp when this decision was made.
    /// </summary>
    public required System.DateTime Timestamp { get; init; }
}
```

#### File 5: RoutingPolicy.cs (Infrastructure Layer - Main Implementation)

```csharp
namespace Acode.Infrastructure.Routing;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Acode.Application.Routing;
using Acode.Domain.OperatingModes;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implements the routing policy that selects appropriate models for agent roles.
/// </summary>
/// <remarks>
/// This is the main orchestrator that coordinates strategy implementations, fallback
/// handling, availability checking, and operating mode enforcement. It is registered
/// as a singleton service in dependency injection.
/// </remarks>
public sealed class RoutingPolicy : IRoutingPolicy
{
    private readonly RoutingConfiguration _configuration;
    private readonly ModelRegistry _modelRegistry;
    private readonly FallbackHandler _fallbackHandler;
    private readonly ILogger<RoutingPolicy> _logger;
    private readonly Dictionary<RoutingStrategy, IRoutingStrategy> _strategies;

    public RoutingPolicy(
        RoutingConfiguration configuration,
        ModelRegistry modelRegistry,
        ILogger<RoutingPolicy> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _modelRegistry = modelRegistry ?? throw new ArgumentNullException(nameof(modelRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _fallbackHandler = new FallbackHandler(_configuration, _modelRegistry, _logger);

        // Initialize routing strategies
        _strategies = new Dictionary<RoutingStrategy, IRoutingStrategy>
        {
            { RoutingStrategy.Single, new SingleModelStrategy(_configuration, _logger) },
            { RoutingStrategy.RoleBased, new RoleBasedStrategy(_configuration, _logger) },
            { RoutingStrategy.Adaptive, new AdaptiveStrategy(_configuration, _logger) }
        };
    }

    public RoutingDecision GetModel(AgentRole role, RoutingContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Routing request for role {Role} with strategy {Strategy}",
                role, _configuration.Strategy);

            // Handle user override first (bypasses strategy but respects operating mode)
            if (!string.IsNullOrEmpty(context.UserOverride))
            {
                return HandleUserOverride(context.UserOverride, context, stopwatch);
            }

            // Select strategy and get primary model
            var strategy = _strategies[_configuration.Strategy];
            var primaryModelId = strategy.SelectModel(role, context);

            // Validate model ID format
            if (!IsValidModelId(primaryModelId))
            {
                throw new RoutingException(
                    "ACODE-RTE-002",
                    $"Invalid model ID '{primaryModelId}'. Valid format: name:tag or name:tag@provider",
                    null);
            }

            // Check operating mode constraints
            if (!ValidateOperatingModeConstraint(primaryModelId, context.OperatingMode))
            {
                throw new RoutingException(
                    "ACODE-RTE-003",
                    $"Model '{primaryModelId}' not allowed in {context.OperatingMode} mode",
                    new[] { primaryModelId })
                {
                    Suggestion = context.OperatingMode == OperatingMode.LocalOnly
                        ? "Use a local model or change operating mode to 'burst'"
                        : "Use an air-gapped model or change operating mode"
                };
            }

            // Check model availability
            if (_modelRegistry.IsModelAvailable(primaryModelId))
            {
                stopwatch.Stop();

                var decision = new RoutingDecision
                {
                    ModelId = primaryModelId,
                    IsFallback = false,
                    SelectionReason = $"strategy: {_configuration.Strategy}, role: {role}",
                    SelectedProvider = _modelRegistry.GetProviderForModel(primaryModelId),
                    DecisionTimeMs = stopwatch.ElapsedMilliseconds,
                    Timestamp = DateTime.UtcNow
                };

                LogRoutingDecision(decision, role);
                return decision;
            }

            // Primary unavailable, try fallback
            _logger.LogWarning("Primary model {ModelId} unavailable, checking fallback chain", primaryModelId);
            return _fallbackHandler.HandleFallback(role, context, primaryModelId, stopwatch);
        }
        catch (RoutingException)
        {
            throw; // Rethrow routing exceptions as-is
        }
        catch (Exception ex)
        {
            throw new RoutingException(
                "ACODE-RTE-001",
                $"Routing failed for role {role}: {ex.Message}",
                null,
                ex);
        }
    }

    public RoutingDecision? GetFallbackModel(AgentRole role, RoutingContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        return _fallbackHandler.HandleFallback(role, context, null, stopwatch);
    }

    public bool IsModelAvailable(string modelId)
    {
        return _modelRegistry.IsModelAvailable(modelId);
    }

    public IReadOnlyList<ModelInfo> ListAvailableModels()
    {
        return _modelRegistry.ListAvailableModels();
    }

    private RoutingDecision HandleUserOverride(
        string overrideModelId,
        RoutingContext context,
        Stopwatch stopwatch)
    {
        _logger.LogInformation("User override detected: {ModelId}", overrideModelId);

        // Validate model ID
        if (!IsValidModelId(overrideModelId))
        {
            throw new RoutingException(
                "ACODE-RTE-002",
                $"Invalid model ID in user override: '{overrideModelId}'",
                null);
        }

        // Still enforce operating mode constraints
        if (!ValidateOperatingModeConstraint(overrideModelId, context.OperatingMode))
        {
            throw new RoutingException(
                "ACODE-RTE-003",
                $"Model '{overrideModelId}' not allowed in {context.OperatingMode} mode (even with user override)",
                new[] { overrideModelId });
        }

        // Check availability
        if (!_modelRegistry.IsModelAvailable(overrideModelId))
        {
            throw new RoutingException(
                "ACODE-RTE-001",
                $"User override model '{overrideModelId}' is not available",
                new[] { overrideModelId })
            {
                Suggestion = $"Start the model with 'ollama run {overrideModelId}'"
            };
        }

        stopwatch.Stop();

        return new RoutingDecision
        {
            ModelId = overrideModelId,
            IsFallback = false,
            SelectionReason = "user override",
            SelectedProvider = _modelRegistry.GetProviderForModel(overrideModelId),
            DecisionTimeMs = stopwatch.ElapsedMilliseconds,
            Timestamp = DateTime.UtcNow
        };
    }

    private bool IsValidModelId(string modelId)
    {
        // Valid formats: "name:tag" or "name:tag@provider"
        if (string.IsNullOrWhiteSpace(modelId))
            return false;

        var parts = modelId.Split('@');
        var modelPart = parts[0];

        return modelPart.Contains(':');
    }

    private bool ValidateOperatingModeConstraint(string modelId, OperatingMode operatingMode)
    {
        var modelInfo = _modelRegistry.GetModelInfo(modelId);
        if (modelInfo == null)
            return true; // Model not in registry, assume constraint checking happens elsewhere

        return operatingMode switch
        {
            OperatingMode.LocalOnly => modelInfo.IsLocal,
            OperatingMode.AirGapped => modelInfo.IsLocal,
            OperatingMode.Burst => true, // Burst allows any model
            _ => true
        };
    }

    private void LogRoutingDecision(RoutingDecision decision, AgentRole role)
    {
        _logger.LogInformation(
            "Routing decision: role={Role}, model={ModelId}, fallback={IsFallback}, " +
            "reason={Reason}, provider={Provider}, time={TimeMs}ms",
            role,
            decision.ModelId,
            decision.IsFallback,
            decision.SelectionReason,
            decision.SelectedProvider,
            decision.DecisionTimeMs);
    }
}
```

#### File 6: ModelCapabilityMatcher.cs (Complete Implementation)

```csharp
namespace Acode.Infrastructure.Routing;

using System.Collections.Generic;
using System.Linq;
using Acode.Application.Routing;

/// <summary>
/// Matches models to required capabilities for capability-aware routing.
/// </summary>
public sealed class ModelCapabilityMatcher
{
    private readonly ModelRegistry _modelRegistry;

    public ModelCapabilityMatcher(ModelRegistry modelRegistry)
    {
        _modelRegistry = modelRegistry;
    }

    /// <summary>
    /// Filters available models to those that support all required capabilities.
    /// </summary>
    public IEnumerable<string> FilterByCapabilities(
        IEnumerable<string> candidateModels,
        IReadOnlyList<ModelCapability> requiredCapabilities)
    {
        if (requiredCapabilities == null || requiredCapabilities.Count == 0)
        {
            return candidateModels; // No capability requirements
        }

        return candidateModels.Where(modelId =>
        {
            var modelInfo = _modelRegistry.GetModelInfo(modelId);
            if (modelInfo == null)
                return false;

            return requiredCapabilities.All(capability =>
                SupportsCapability(modelInfo, capability));
        });
    }

    private bool SupportsCapability(ModelInfo modelInfo, ModelCapability capability)
    {
        return capability switch
        {
            ModelCapability.ToolCalling => modelInfo.SupportsToolCalling,
            ModelCapability.FunctionCalling => modelInfo.SupportsToolCalling, // Same as tool calling
            ModelCapability.StructuredOutput => true, // Most models support JSON output
            _ => false
        };
    }
}
```

#### File 7: FallbackStrategy.cs (Complete Implementation)

```csharp
namespace Acode.Infrastructure.Routing;

using System;
using System.Diagnostics;
using System.Linq;
using Acode.Application.Routing;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implements fallback logic when primary model is unavailable.
/// </summary>
public sealed class FallbackHandler
{
    private readonly RoutingConfiguration _configuration;
    private readonly ModelRegistry _modelRegistry;
    private readonly ILogger _logger;

    public FallbackHandler(
        RoutingConfiguration configuration,
        ModelRegistry modelRegistry,
        ILogger logger)
    {
        _configuration = configuration;
        _modelRegistry = modelRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Handles fallback when primary model is unavailable.
    /// </summary>
    public RoutingDecision HandleFallback(
        AgentRole role,
        RoutingContext context,
        string? primaryModelId,
        Stopwatch stopwatch)
    {
        if (_configuration.FallbackChain == null || _configuration.FallbackChain.Count == 0)
        {
            throw new RoutingException(
                "ACODE-RTE-004",
                $"No available model for role {role} and no fallback chain configured",
                primaryModelId != null ? new[] { primaryModelId } : Array.Empty<string>())
            {
                Suggestion = "Configure a fallback_chain in routing configuration"
            };
        }

        var attemptedModels = primaryModelId != null
            ? new System.Collections.Generic.List<string> { primaryModelId }
            : new System.Collections.Generic.List<string>();

        // Traverse fallback chain sequentially
        foreach (var fallbackModelId in _configuration.FallbackChain)
        {
            if (attemptedModels.Contains(fallbackModelId))
            {
                continue; // Skip if already attempted
            }

            attemptedModels.Add(fallbackModelId);

            _logger.LogDebug("Checking fallback model {ModelId}", fallbackModelId);

            // Validate operating mode constraint
            var modelInfo = _modelRegistry.GetModelInfo(fallbackModelId);
            if (modelInfo != null && !ValidateOperatingMode(modelInfo, context.OperatingMode))
            {
                _logger.LogDebug("Fallback model {ModelId} rejected by operating mode constraint", fallbackModelId);
                continue;
            }

            // Check availability
            if (_modelRegistry.IsModelAvailable(fallbackModelId))
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "Fallback activated: primary={Primary}, fallback={Fallback}, role={Role}",
                    primaryModelId ?? "none",
                    fallbackModelId,
                    role);

                return new RoutingDecision
                {
                    ModelId = fallbackModelId,
                    IsFallback = true,
                    FallbackReason = "primary_unavailable",
                    SelectionReason = $"fallback from {primaryModelId ?? "none"}",
                    SelectedProvider = _modelRegistry.GetProviderForModel(fallbackModelId),
                    DecisionTimeMs = stopwatch.ElapsedMilliseconds,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        // All fallback models exhausted
        throw new RoutingException(
            "ACODE-RTE-004",
            $"Fallback chain exhausted for role {role}. No available models.",
            attemptedModels.ToArray())
        {
            Suggestion = $"Start a model with 'ollama run {_configuration.FallbackChain.Last()}'"
        };
    }

    private bool ValidateOperatingMode(ModelInfo modelInfo, OperatingMode operatingMode)
    {
        return operatingMode switch
        {
            OperatingMode.LocalOnly => modelInfo.IsLocal,
            OperatingMode.AirGapped => modelInfo.IsLocal,
            OperatingMode.Burst => true,
            _ => true
        };
    }
}
```

### Error Codes

| Code | Message |
|------|---------|
| ACODE-RTE-001 | No available model for role |
| ACODE-RTE-002 | Invalid model ID |
| ACODE-RTE-003 | Mode constraint violation |
| ACODE-RTE-004 | Fallback chain exhausted |
| ACODE-RTE-005 | Invalid routing configuration |
| ACODE-RTE-006 | No model supports required capabilities |

### Implementation Checklist

Follow this sequence strictly using TDD:

1. [ ] **Test:** Create AgentRole enum test (verify all roles defined)
2. [ ] **Implement:** Create AgentRole enum with four roles
3. [ ] **Test:** Create RoutingContext test (verify all properties)
4. [ ] **Implement:** Create RoutingContext class
5. [ ] **Test:** Create RoutingDecision test (verify immutability)
6. [ ] **Implement:** Create RoutingDecision class
7. [ ] **Test:** Create IRoutingPolicy interface test (verify contract)
8. [ ] **Implement:** Create IRoutingPolicy interface
9. [ ] **Test:** Single model strategy test (all roles return same model)
10. [ ] **Implement:** SingleModelStrategy class
11. [ ] **Test:** Role-based strategy test (different models per role)
12. [ ] **Implement:** RoleBasedStrategy class
13. [ ] **Test:** Fallback handler test (sequential traversal)
14. [ ] **Implement:** FallbackHandler class
15. [ ] **Test:** RoutingPolicy integration test (end-to-end)
16. [ ] **Implement:** RoutingPolicy class
17. [ ] **Test:** Operating mode constraint tests
18. [ ] **Implement:** Operating mode validation logic
19. [ ] **Test:** Model capability matching tests
20. [ ] **Implement:** ModelCapabilityMatcher class
21. [ ] **Test:** Performance tests (routing decision < 10ms)
22. [ ] **Implement:** Performance optimizations (caching)
23. [ ] **Test:** CLI command tests
24. [ ] **Implement:** CLI commands (acode models routing, acode models test)
25. [ ] **Audit:** Run full audit checklist from AUDIT-GUIDELINES.md

### Verification Commands

```bash
# Run all routing tests
dotnet test --filter "FullyQualifiedName~Routing"

# Run performance tests specifically
dotnet test --filter "FullyQualifiedName~RoutingPerformanceTests"

# Build and verify no warnings
dotnet build --no-incremental --warnaserror

# Verify test coverage
dotnet test /p:CollectCoverage=true /p:CoverageReporter=lcov
```

### Integration Points

- **Task 001:** Query IOperatingModeProvider for current operating mode
- **Task 002:** Load routing configuration from IConfigurationProvider
- **Task 004:** Query IModelProvider for model availability and capabilities
- **Task 008:** Routing decision feeds into prompt template selection
- **Task 012:** Multi-stage agent loop requests models per stage

### Expected Commits

1. `test(task-009): add AgentRole enum tests`
2. `feat(task-009): implement AgentRole enum with four roles`
3. `test(task-009): add RoutingContext tests`
4. `feat(task-009): implement RoutingContext class`
5. `test(task-009): add single model strategy tests`
6. `feat(task-009): implement SingleModelStrategy`
7. `test(task-009): add role-based strategy tests`
8. `feat(task-009): implement RoleBasedStrategy`
9. `test(task-009): add fallback handler tests`
10. `feat(task-009): implement FallbackHandler`
11. `test(task-009): add routing policy integration tests`
12. `feat(task-009): implement RoutingPolicy orchestrator`
13. `test(task-009): add performance tests`
14. `feat(task-009): optimize routing decision caching`
15. `docs(task-009): add XML documentation to all public APIs`

---

**End of Task 009 Specification**