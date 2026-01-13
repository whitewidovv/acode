# Task 007e (formerly 006b) - 100% Completion Checklist

## INSTRUCTIONS FOR FRESH CONTEXT AGENT

**Your Mission**: Complete task-007e (Structured Outputs Enforcement for vLLM) to 100% specification compliance - all 73 acceptance criteria must be semantically complete with tests.

**Current Status**:
- **Completion**: ~24% by file count, ~18% by AC count  
- **Major Issue**: Configuration ~80% done, but ENTIRE core subsystems missing
- **Gap Analysis**: See docs/implementation-plans/task-007e-gap-analysis.md for detailed findings
- **Dependency**: IToolSchemaRegistry from Task 007 ✅ **EXISTS AND VERIFIED**

**How to Use This File**:
1. Read ENTIRE file first (understand full scope ~1200+ lines)
2. Read the task spec: docs/tasks/refined-tasks/Epic 01/task-007e-structured-outputs-enforcement-integration.md (3597 lines)
3. Work through Phases 0-10 sequentially  
4. For each gap item:
   - Mark as [🔄] when starting work
   - Follow TDD strictly: RED → GREEN → REFACTOR
   - Run tests after each change
   - Mark as [✅] when complete with evidence
5. Update this file after EACH completed item (not batched)
6. Commit after each logical unit of work
7. When context low (<10k tokens): commit, update progress, stop

**Status Legend**:
- `[ ]` = TODO (not started)
- `[🔄]` = IN PROGRESS (actively working on this)
- `[✅]` = COMPLETE (implemented + tested + verified)

**Critical Rules** (CLAUDE.md Section 3):
- NO deferrals - implement EVERYTHING in this task
- NO placeholders - full implementations only
- NO "TODO" comments in production code
- TESTS FIRST - always RED before GREEN
- VERIFY SEMANTICALLY - presence ≠ completeness
- COMMIT FREQUENTLY - after each logical unit

**Context Management**:
- If context runs low, commit and update this file with [🔄] status
- Mark exactly what's partially done and where to resume
- Next session picks up from this file

**Key Spec Sections** (must read before coding):
- Implementation Prompt: lines 2675-3597 (file structure + code examples)
- Testing Requirements: lines 1123-2674 (all test files/methods)  
- Acceptance Criteria: lines 1010-1121 (73 ACs to verify)
- Functional Requirements: lines 161-267 (71 FRs to implement)

**Dependency from Task 007**:
- IToolSchemaRegistry ✅ EXISTS at src/Acode.Application/Tools/IToolSchemaRegistry.cs
- ToolSchemaRegistry ✅ IMPLEMENTED at src/Acode.Infrastructure/Tools/ToolSchemaRegistry.cs
- **NO BLOCKERS** - all dependencies satisfied

---

## PHASE 0: VERIFY EXISTING IMPLEMENTATION (DO FIRST)

**Goal**: Read all existing files and document semantic completeness before adding new code.

### Gap 0.1: Read and Verify StructuredOutputConfiguration.cs

**Status**: [ ]

**File**: src/Acode.Infrastructure/Vllm/StructuredOutput/Configuration/StructuredOutputConfiguration.cs (142 lines)

**What to Check**:
1. Does IsEnabled(string modelId) method exist?
2. Does GetFallbackConfig(string modelId) method exist?
3. Does Validate() method exist?
4. Does environment variable override logic exist?

**Expected from Spec** (lines 2766-2849):
```csharp
public bool IsEnabled(string modelId)
{
    if (!Enabled) return false;
    if (ModelOverrides.TryGetValue(modelId, out var config))
        return config.Enabled;
    return true;
}

public FallbackConfiguration GetFallbackConfig(string modelId)
{
    if (ModelOverrides.TryGetValue(modelId, out var config) && config.Fallback != null)
        return config.Fallback;
    return Fallback;
}

public ValidationResult Validate()
{
    // Validate MaxRetries, MaxDepth, MaxSizeBytes, etc.
}

public static StructuredOutputConfiguration FromConfiguration(IConfiguration config)
{
    // Must include environment variable override
    var envEnabled = Environment.GetEnvironmentVariable("ACODE_VLLM_STRUCTURED_OUTPUT_ENABLED");
}
```

**Commands**:
```bash
# Read file
cat src/Acode.Infrastructure/Vllm/StructuredOutput/Configuration/StructuredOutputConfiguration.cs

# Check for missing methods
grep "IsEnabled\|GetFallbackConfig\|Validate" StructuredOutputConfiguration.cs
```

**Document Findings**:
- [ ] IsEnabled method: ❌ Missing / ✅ Exists
- [ ] GetFallbackConfig method: ❌ Missing / ✅ Exists
- [ ] Validate method: ❌ Missing / ✅ Exists
- [ ] Environment override: ❌ Missing / ✅ Exists

**Success Criteria**:
- [ ] All methods documented as present or missing
- [ ] Gap analysis updated with findings

**Evidence**:
```
# Paste findings here
```

---

### Gap 0.2-0.5: Verify Other Configuration Files

**Status**: [ ]

**Files to Read**:
1. FallbackConfiguration.cs
2. SchemaConfiguration.cs
3. ModelStructuredOutputConfig.cs
4. ConfigurationValidationResult.cs

**For Each File, Check**:
- All properties from spec present
- Validation logic correct
- No stub implementations

**Success Criteria**:
- [ ] All 4 files read and verified
- [ ] Gaps documented

---

### Gap 0.6: Verify SchemaTransformer.cs Completeness

**Status**: [ ]

**File**: src/Acode.Infrastructure/Vllm/StructuredOutput/Schema/SchemaTransformer.cs (287 lines)

**What to Check** (FR-021 through FR-030):
- [ ] Handles object type schemas
- [ ] Handles array type schemas
- [ ] Handles primitive types
- [ ] Handles enum constraints
- [ ] Handles required fields
- [ ] Handles additionalProperties
- [ ] Handles nested objects
- [ ] Handles array item schemas
- [ ] Inlines $ref references (local only)
- [ ] Preserves field descriptions

**Commands**:
```bash
grep -E "Transform|InlineRefs|HandleEnum|HandleArray" SchemaTransformer.cs
dotnet test --filter "SchemaTransformerTests" --list-tests
```

**Success Criteria**:
- [ ] All required functionality verified present
- [ ] Test count documented (currently ~3, need ~15)
- [ ] Semantic gaps documented

---

## PHASE 1: COMPLETE CONFIGURATION LAYER

**Goal**: Finish StructuredOutputConfiguration with all missing methods and tests.
**ACs Covered**: AC-001 through AC-007

### Gap 1.1: Add IsEnabled Method to StructuredOutputConfiguration

**Status**: [ ]

**Problem**: Method missing (if found in Phase 0)

**TDD Steps**:

**RED**:
Create test file first (if missing):
```bash
touch tests/Acode.Infrastructure.Tests/Vllm/StructuredOutput/Configuration/StructuredOutputConfigurationTests.cs
```

Test from spec lines 1140-1150:
```csharp
[Fact]
public void Should_Enable_By_Default()
{
    var config = new StructuredOutputConfiguration();
    
    var enabled = config.IsEnabled("any-model");
    
    enabled.Should().BeTrue("structured output is enabled by default");
}
```

Run: `dotnet test --filter "Should_Enable_By_Default"`
Expected: RED (method doesn't exist)

**GREEN**:
Add method to StructuredOutputConfiguration.cs:
```csharp
public bool IsEnabled(string modelId)
{
    if (!Enabled) return false;
    
    if (Models.TryGetValue(modelId, out var modelConfig))
    {
        return modelConfig.Enabled;
    }
    
    return true;
}
```

Run test: Expected GREEN

**Success Criteria**:
- [ ] Method added
- [ ] Test passes
- [ ] AC-001, AC-002 verified

---

### Gap 1.2-1.3: Add GetFallbackConfig and Validate Methods

**Status**: [ ]

**Similar TDD pattern**: Test first, implement, verify.

**Tests to Add** (from spec lines 1152-1222):
- Should_Disable_When_Configured
- Should_Override_Per_Model
- Should_Override_From_Environment
- Should_Validate_Configuration

**Success Criteria**:
- [ ] GetFallbackConfig method complete
- [ ] Validate method complete
- [ ] Environment override working
- [ ] 6-8 config tests passing
- [ ] AC-003 through AC-007 verified

---

## PHASE 2: COMPLETE SCHEMA SUBSYSTEM

**Goal**: Add SchemaValidator and SchemaCache, expand SchemaTransformer tests.
**ACs Covered**: AC-019 through AC-028

### Gap 2.1: Create SchemaValidator.cs

**Status**: [ ]

**File to Create**: src/Acode.Infrastructure/Vllm/StructuredOutput/Schema/SchemaValidator.cs

**Required Functionality** (FR-034, NFR-013, NFR-014):
- Validate schema against JSON Schema meta-schema
- Check for external $ref references (security - only local #/definitions/ allowed)
- Check depth limits (max 10 levels)
- Check size limits (max 64KB)
- Return SchemaValidationResult with errors

**TDD Steps**:

**RED**:
```csharp
// In SchemaValidatorTests.cs
[Fact]
public void Validate_SimpleSchema_ReturnsValid()
{
    var validator = new SchemaValidator(maxDepth: 10, maxSize: 65536);
    var schema = JsonDocument.Parse(@"{""type"":""object""}").RootElement;
    
    var result = validator.Validate(schema);  // Doesn't exist yet
    
    result.IsValid.Should().BeTrue();
}
```

Run test: Expected RED

**GREEN**:
```csharp
public sealed class SchemaValidator
{
    private readonly int _maxDepth;
    private readonly int _maxSize;
    
    public SchemaValidator(int maxDepth = 10, int maxSize = 65536)
    {
        _maxDepth = maxDepth;
        _maxSize = maxSize;
    }
    
    public SchemaValidationResult Validate(JsonElement schema)
    {
        var errors = new List<string>();
        
        // Check size
        var schemaJson = schema.GetRawText();
        if (schemaJson.Length > _maxSize)
        {
            errors.Add($"Schema exceeds size limit ({schemaJson.Length} > {_maxSize} bytes)");
        }
        
        // Check depth
        var depth = CalculateDepth(schema);
        if (depth > _maxDepth)
        {
            errors.Add($"Schema exceeds depth limit ({depth} > {_maxDepth} levels)");
        }
        
        // Check for external $ref (security)
        if (HasExternalRefs(schema))
        {
            errors.Add("Schema contains external $ref references (only local references allowed)");
        }
        
        return new SchemaValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
    
    private int CalculateDepth(JsonElement element, int current = 0)
    {
        // Recursive depth calculation
    }
    
    private bool HasExternalRefs(JsonElement element)
    {
        // Check for $ref not starting with #/
    }
}
```

Run test: Expected GREEN

**More Tests to Add** (~8 tests):
- Validate_SchemaExceedsDepth_ReturnsInvalid
- Validate_SchemaExceedsSize_ReturnsInvalid
- Validate_ExternalRef_ReturnsInvalid
- Validate_LocalRef_ReturnsValid
- Validate_ComplexNestedSchema_ReturnsValid
- etc.

**Success Criteria**:
- [ ] SchemaValidator.cs created
- [ ] All validation logic implemented
- [ ] ~8 tests passing
- [ ] AC-032, AC-073 verified (security)

---

### Gap 2.2: Create SchemaCache.cs

**Status**: [ ]

**File to Create**: src/Acode.Infrastructure/Vllm/StructuredOutput/Schema/SchemaCache.cs

**Required Functionality** (NFR-004, NFR-005, FR-068):
- Cache transformed schemas
- Capacity: 100 schemas
- Memory limit: <10MB
- Thread-safe (ConcurrentDictionary)
- Key: schema hash or tool name
- Value: transformed JsonElement

**TDD Pattern**: Similar to above - test first, implement, verify

**Tests** (~5 tests):
- Should_Cache_TransformedSchema
- Should_Return_Cached_Schema
- Should_Evict_When_Full
- Should_Be_ThreadSafe
- Should_Respect_Memory_Limit

**Success Criteria**:
- [ ] SchemaCache.cs created
- [ ] Caching logic implemented
- [ ] Thread-safety verified
- [ ] ~5 tests passing
- [ ] AC-066 verified

---

### Gap 2.3: Expand SchemaTransformer Tests

**Status**: [ ]

**Current**: ~3 tests
**Required**: ~15 tests

**Tests to Add** (based on spec Testing Requirements):
- Transform_ObjectWithRequiredFields
- Transform_ArrayWithItemSchema
- Transform_EnumType
- Transform_NestedObjectMultipleLevels
- Transform_AdditionalPropertiesFalse
- Transform_PreservesDescriptions
- Transform_HandlesOptionalFields
- etc.

**Success Criteria**:
- [ ] ~12 additional tests added
- [ ] Total: ~15 tests passing
- [ ] AC-019 through AC-028 fully verified

---

## PHASE 3: IMPLEMENT RESPONSEFORMAT SUBSYSTEM

**Goal**: Build response_format and guided_* parameter construction.
**ACs Covered**: AC-008 through AC-013, AC-029 through AC-034

### Gap 3.1: Create ResponseFormat Subdirectory and ResponseFormatBuilder

**Status**: [ ]

**Directory**: src/Acode.Infrastructure/Vllm/StructuredOutput/ResponseFormat/
**File**: ResponseFormatBuilder.cs

**Required Functionality** (FR-008 through FR-013):
- Build response_format parameter for vLLM requests
- Support type: "json_object"
- Support type: "json_schema" with schema payload
- Conditional inclusion based on configuration

**TDD Steps**:

**RED**:
```csharp
[Fact]
public void Build_JsonObject_ReturnsCorrectFormat()
{
    var builder = new ResponseFormatBuilder();  // Doesn't exist yet
    
    var format = builder.Build(ResponseFormatType.JsonObject, schema: null);
    
    format.Should().NotBeNull();
    format.Type.Should().Be("json_object");
}
```

Run test: Expected RED

**GREEN**:
```csharp
public sealed class ResponseFormatBuilder
{
    public ResponseFormat Build(ResponseFormatType type, JsonElement? schema = null)
    {
        return type switch
        {
            ResponseFormatType.JsonObject => new ResponseFormat { Type = "json_object" },
            ResponseFormatType.JsonSchema => new ResponseFormat
            {
                Type = "json_schema",
                JsonSchema = schema ?? throw new ArgumentNullException(nameof(schema))
            },
            _ => throw new ArgumentException($"Unsupported type: {type}")
        };
    }
}

public enum ResponseFormatType
{
    JsonObject,
    JsonSchema
}

public sealed class ResponseFormat
{
    public string Type { get; init; } = string.Empty;
    public JsonElement? JsonSchema { get; init; }
}
```

Run test: Expected GREEN

**More Tests** (~6 tests):
- Build_JsonSchema_IncludesSchema
- Build_JsonSchema_WithoutSchema_Throws
- Build_DisabledConfiguration_ReturnsNull
- etc.

**Success Criteria**:
- [ ] ResponseFormatBuilder.cs created
- [ ] ~6 tests passing
- [ ] AC-008 through AC-013 verified

---

### Gap 3.2: Create GuidedDecodingBuilder

**Status**: [ ]

**File**: GuidedDecodingBuilder.cs

**Required Functionality** (FR-031 through FR-036):
- Build guided_json parameter
- Build guided_choice parameter (for enums)
- Build guided_regex parameter (for patterns)
- Select appropriate parameter based on schema type

**Similar TDD Pattern**: Test first, implement, verify

**Tests** (~5 tests):
- Build_GuidedJson_WithObjectSchema
- Build_GuidedChoice_WithEnumSchema
- Build_GuidedRegex_WithPatternSchema
- Build_SelectsCorrectParameter
- etc.

**Success Criteria**:
- [ ] GuidedDecodingBuilder.cs created
- [ ] ~5 tests passing
- [ ] AC-029 through AC-034 verified

---

## PHASE 4: IMPLEMENT CAPABILITY SUBSYSTEM

**Goal**: Model capability detection and caching.
**ACs Covered**: AC-035 through AC-039

### Gap 4.1: Create Capability Subdirectory and ModelCapabilities Data Class

**Status**: [ ]

**Directory**: src/Acode.Infrastructure/Vllm/StructuredOutput/Capability/
**File**: ModelCapabilities.cs

**Data Class**:
```csharp
public sealed class ModelCapabilities
{
    public bool SupportsStructuredOutput { get; init; }
    public string[] SupportedModes { get; init; } = Array.Empty<string>();
    public int? MaxSchemaDepth { get; init; }
    public int? MaxSchemaSize { get; init; }
    public string ModelId { get; init; } = string.Empty;
    public DateTime DetectedAt { get; init; }
}
```

**Success Criteria**:
- [ ] ModelCapabilities.cs created
- [ ] All properties present

---

### Gap 4.2: Create CapabilityDetector

**Status**: [ ]

**File**: CapabilityDetector.cs

**Required Functionality** (FR-037 through FR-041):
- Query vLLM /v1/models endpoint
- Parse capabilities from response
- Return ModelCapabilities
- Handle unknown models conservatively

**TDD Steps** (with mocked HTTP):
- DetectAsync_QueriesVllmEndpoint
- DetectAsync_ParsesCapabilities
- DetectAsync_UnknownModel_ReturnsConservative
- DetectAsync_HandlesErrors
- etc.

**Success Criteria**:
- [ ] CapabilityDetector.cs created
- [ ] ~8 tests passing
- [ ] AC-035, AC-036, AC-039, AC-041 verified

---

### Gap 4.3: Create CapabilityCache

**Status**: [ ]

**File**: CapabilityCache.cs

**Required Functionality**:
- Cache with 1-hour TTL (per spec assumption 11)
- Thread-safe (ConcurrentDictionary or lock)
- Invalidate on model change
- Refresh stale entries

**Tests** (~3 tests):
- Should_Cache_Capabilities
- Should_Expire_After_TTL
- Should_Be_ThreadSafe

**Success Criteria**:
- [ ] CapabilityCache.cs created
- [ ] ~3 tests passing
- [ ] AC-037, AC-038 verified

---

## PHASE 5: IMPLEMENT FALLBACK SUBSYSTEM

**Goal**: Fallback validation and retry logic.
**ACs Covered**: AC-040 through AC-046, AC-047 through AC-053

### Gap 5.1: Create Fallback Subdirectory and OutputValidator

**Status**: [ ]

**Directory**: src/Acode.Infrastructure/Vllm/StructuredOutput/Fallback/
**File**: OutputValidator.cs

**Required Functionality** (FR-049 through FR-055):
- Validate JSON syntax
- Validate against schema using JsonSchema.Net package
- Check types, required fields, null handling
- Return ValidationResult with detailed errors

**NuGet Package Needed**:
```bash
dotnet add src/Acode.Infrastructure/Acode.Infrastructure.csproj package JsonSchema.Net
```

**TDD Steps**:

**RED**:
```csharp
[Fact]
public void Validate_ValidJson_ReturnsSuccess()
{
    var validator = new OutputValidator();  // Doesn't exist yet
    var schema = JsonDocument.Parse(@"{""type"":""object"",""required"":[""name""]}").RootElement;
    var output = JsonDocument.Parse(@"{""name"":""John""}").RootElement;
    
    var result = validator.Validate(output, schema);
    
    result.IsValid.Should().BeTrue();
}
```

Run test: Expected RED

**GREEN**:
```csharp
using Json.Schema;

public sealed class OutputValidator
{
    public ValidationResult Validate(JsonElement output, JsonElement schemaElement)
    {
        // Parse schema
        var schema = JsonSchema.FromText(schemaElement.GetRawText());
        
        // Validate output against schema
        var validationResults = schema.Evaluate(output);
        
        if (validationResults.IsValid)
        {
            return ValidationResult.Success();
        }
        
        // Extract errors
        var errors = new List<string>();
        foreach (var error in validationResults.Errors)
        {
            errors.Add(error.Message);
        }
        
        return ValidationResult.Failure(errors);
    }
}
```

Run test: Expected GREEN

**More Tests** (~12 tests):
- Validate_MissingRequiredField_ReturnsInvalid
- Validate_WrongType_ReturnsInvalid
- Validate_NullValue_HandledCorrectly
- Validate_InvalidJson_ReturnsInvalid
- etc.

**Success Criteria**:
- [ ] OutputValidator.cs created
- [ ] JsonSchema.Net integrated
- [ ] ~12 tests passing
- [ ] AC-047 through AC-053 verified

---

### Gap 5.2: Create FallbackContext Data Class

**Status**: [ ]

**File**: FallbackContext.cs

**Data Class**:
```csharp
public sealed class FallbackContext
{
    public string Reason { get; init; } = string.Empty;
    public int AttemptNumber { get; init; }
    public int MaxRetries { get; init; }
    public Exception? LastError { get; init; }
    public string ModelId { get; init; } = string.Empty;
}
```

**Success Criteria**:
- [ ] FallbackContext.cs created

---

### Gap 5.3: Create FallbackHandler

**Status**: [ ]

**File**: FallbackHandler.cs

**Required Functionality** (FR-042 through FR-048):
- Detect when to fall back (capability unavailable, schema rejected, etc.)
- Execute unconstrained generation (done by VllmProvider)
- Validate output using OutputValidator
- Retry on validation failure (up to MaxRetries)
- Log fallback with reason

**Dependencies**:
- OutputValidator
- FallbackConfiguration
- ILogger<FallbackHandler>

**TDD Steps** (~10 tests):
- ShouldFallback_CapabilityUnavailable_ReturnsTrue
- ShouldFallback_SchemaRejected_ReturnsTrue
- ValidateWithRetry_FirstAttemptValid_Succeeds
- ValidateWithRetry_RetriesOnFailure
- ValidateWithRetry_ExceedsMaxRetries_Throws
- etc.

**Success Criteria**:
- [ ] FallbackHandler.cs created
- [ ] ~10 tests passing
- [ ] AC-040 through AC-046 verified

---

## PHASE 6: IMPLEMENT MAIN ORCHESTRATOR (CRITICAL)

**Goal**: StructuredOutputHandler - the core integration point.
**ACs Covered**: AC-014 through AC-018, AC-054 through AC-058, ALL orchestration ACs

**This is the MOST IMPORTANT component** - it ties everything together.

### Gap 6.1: Create StructuredOutputHandler

**Status**: [ ]

**File**: src/Acode.Infrastructure/Vllm/StructuredOutput/StructuredOutputHandler.cs

**Required Dependencies** (inject all):
- IToolSchemaRegistry (from Task 007) ✅
- SchemaTransformer
- SchemaValidator
- SchemaCache
- CapabilityDetector
- CapabilityCache
- ResponseFormatBuilder
- GuidedDecodingBuilder
- FallbackHandler
- StructuredOutputConfiguration
- ILogger<StructuredOutputHandler>

**Required Methods**:

1. `Task<VllmRequest> EnrichRequestAsync(VllmRequest request, ToolDefinition[] tools, CancellationToken ct)`
   - Check if structured output enabled for model
   - Detect model capabilities
   - Extract schemas from tools using IToolSchemaRegistry
   - Transform schemas using SchemaTransformer
   - Validate schemas using SchemaValidator
   - Build response_format or guided_* parameters
   - Enrich VllmRequest with parameters
   - Log what was done

2. `Task<ValidationResult> ValidateResponseAsync(VllmResponse response, JsonElement schema, CancellationToken ct)`
   - Used in fallback mode
   - Validate response against schema using OutputValidator
   - Return result

3. `bool ShouldUseStructuredOutput(string modelId)`
   - Check configuration
   - Check model capabilities
   - Return decision

**TDD Steps**:

**RED** (first test):
```csharp
[Fact]
public async Task EnrichRequestAsync_EnabledWithTools_AddsResponseFormat()
{
    // Arrange
    var config = new StructuredOutputConfiguration { Enabled = true };
    var schemaRegistry = Substitute.For<IToolSchemaRegistry>();
    var schemaTransformer = Substitute.For<SchemaTransformer>();
    // ... mock other dependencies
    
    var handler = new StructuredOutputHandler(
        config,
        schemaRegistry,
        schemaTransformer,
        // ... other deps
    );
    
    var request = new VllmRequest { Model = "test-model" };
    var tools = new[] { new ToolDefinition { Name = "ReadFile", /* ... */ } };
    
    // Act
    var enriched = await handler.EnrichRequestAsync(request, tools, CancellationToken.None);
    
    // Assert
    enriched.ResponseFormat.Should().NotBeNull();
    enriched.ResponseFormat.Type.Should().Be("json_schema");
}
```

Run test: Expected RED (StructuredOutputHandler doesn't exist)

**GREEN** (implement orchestrator):
```csharp
public sealed class StructuredOutputHandler
{
    private readonly IToolSchemaRegistry _schemaRegistry;
    private readonly SchemaTransformer _schemaTransformer;
    private readonly SchemaValidator _schemaValidator;
    private readonly SchemaCache _schemaCache;
    private readonly CapabilityDetector _capabilityDetector;
    private readonly CapabilityCache _capabilityCache;
    private readonly ResponseFormatBuilder _responseFormatBuilder;
    private readonly GuidedDecodingBuilder _guidedDecodingBuilder;
    private readonly FallbackHandler _fallbackHandler;
    private readonly StructuredOutputConfiguration _config;
    private readonly ILogger<StructuredOutputHandler> _logger;
    
    // Constructor injects all deps
    
    public async Task<VllmRequest> EnrichRequestAsync(
        VllmRequest request,
        ToolDefinition[] tools,
        CancellationToken ct)
    {
        var modelId = request.Model;
        
        // Check if enabled
        if (!_config.IsEnabled(modelId))
        {
            _logger.LogDebug("Structured output disabled for model {ModelId}", modelId);
            return request;
        }
        
        // Detect capabilities
        var capabilities = await _capabilityDetector.DetectAsync(modelId, ct);
        if (!capabilities.SupportsStructuredOutput)
        {
            _logger.LogWarning("Model {ModelId} does not support structured output", modelId);
            return request;
        }
        
        // Extract schemas from tools
        var toolSchemas = new List<JsonElement>();
        foreach (var tool in tools)
        {
            var schema = _schemaRegistry.GetSchema(tool.Name);
            toolSchemas.Add(schema);
        }
        
        // Merge or select schema
        var mergedSchema = MergeSchemas(toolSchemas);
        
        // Transform schema
        var transformedSchema = _schemaTransformer.Transform(mergedSchema);
        
        // Validate schema
        var validation = _schemaValidator.Validate(transformedSchema);
        if (!validation.IsValid)
        {
            _logger.LogError("Schema validation failed: {Errors}", string.Join(", ", validation.Errors));
            // Fall back to unconstrained
            return request;
        }
        
        // Build response_format parameter
        var responseFormat = _responseFormatBuilder.Build(ResponseFormatType.JsonSchema, transformedSchema);
        request.ResponseFormat = responseFormat;
        
        _logger.LogInformation("Structured output enabled for {ModelId} with {ToolCount} tools",
            modelId, tools.Length);
        
        return request;
    }
    
    public async Task<ValidationResult> ValidateResponseAsync(
        VllmResponse response,
        JsonElement schema,
        CancellationToken ct)
    {
        // Extract output from response
        var output = ExtractOutput(response);
        
        // Validate using OutputValidator
        return _fallbackHandler.OutputValidator.Validate(output, schema);
    }
    
    public bool ShouldUseStructuredOutput(string modelId)
    {
        return _config.IsEnabled(modelId);
    }
    
    private JsonElement MergeSchemas(List<JsonElement> schemas)
    {
        // Merge logic
    }
    
    private JsonElement ExtractOutput(VllmResponse response)
    {
        // Extract logic
    }
}
```

Run test: Expected GREEN

**More Tests** (~15 tests):
- EnrichRequestAsync_DisabledConfiguration_ReturnsUnchanged
- EnrichRequestAsync_UnsupportedModel_ReturnsUnchanged
- EnrichRequestAsync_SchemaValidationFails_FallsBack
- EnrichRequestAsync_MultipleTools_MergesSchemas
- ValidateResponseAsync_ValidOutput_ReturnsSuccess
- ValidateResponseAsync_InvalidOutput_ReturnsFailure
- ShouldUseStructuredOutput_EnabledGlobally_ReturnsTrue
- ShouldUseStructuredOutput_DisabledPerModel_ReturnsFalse
- etc.

**Success Criteria**:
- [ ] StructuredOutputHandler.cs created
- [ ] All dependencies injected
- [ ] EnrichRequestAsync implemented
- [ ] ValidateResponseAsync implemented
- [ ] ~15 tests passing
- [ ] AC-014 through AC-018, AC-054 through AC-058 verified

---

## PHASE 7: COMPLETE EXCEPTION HIERARCHY

**Goal**: All exception types with proper error codes.
**ACs Covered**: AC-059 through AC-065

### Gap 7.1: Create Base StructuredOutputException

**Status**: [ ]

**File**: src/Acode.Infrastructure/Vllm/StructuredOutput/Exceptions/StructuredOutputException.cs

**Implementation**:
```csharp
public class StructuredOutputException : VllmException
{
    public StructuredOutputException(string message, string errorCode)
        : base(errorCode, message)
    {
    }
    
    public StructuredOutputException(string message, string errorCode, Exception innerException)
        : base(errorCode, message, innerException)
    {
    }
}
```

**Success Criteria**:
- [ ] StructuredOutputException.cs created
- [ ] Inherits from VllmException

---

### Gap 7.2: Create ValidationFailedException

**Status**: [ ]

**File**: ValidationFailedException.cs

**Implementation**:
```csharp
public sealed class ValidationFailedException : StructuredOutputException
{
    public ValidationFailedException(string message, string[] errors)
        : base(message, "ACODE-VLM-SO-006")
    {
        Errors = errors;
    }
    
    public string[] Errors { get; }
}
```

**Success Criteria**:
- [ ] ValidationFailedException.cs created
- [ ] Error code ACODE-VLM-SO-006

---

### Gap 7.3: Verify All Error Codes Defined

**Status**: [ ]

**Error Codes from Spec** (line 792-801):
- ACODE-VLM-SO-001: Schema too complex
- ACODE-VLM-SO-002: Unsupported type
- ACODE-VLM-SO-003: Guided decoding timeout
- ACODE-VLM-SO-004: Invalid schema format
- ACODE-VLM-SO-005: Capability detection failed
- ACODE-VLM-SO-006: Validation failed

**Verify**:
- [ ] All 6 error codes exist in appropriate exceptions
- [ ] AC-064 verified

---

## PHASE 8: INTEGRATION WITH VLLMPROVIDER

**Goal**: Wire StructuredOutputHandler into VllmProvider.

### Gap 8.1: Inject StructuredOutputHandler into VllmProvider

**Status**: [ ]

**File**: src/Acode.Infrastructure/Vllm/VllmProvider.cs

**Changes Needed**:

1. Add dependency:
```csharp
private readonly StructuredOutputHandler _structuredOutputHandler;

public VllmProvider(
    // ... existing deps
    StructuredOutputHandler structuredOutputHandler)
{
    _structuredOutputHandler = structuredOutputHandler;
}
```

2. Call EnrichRequestAsync before sending to vLLM:
```csharp
public async Task<ChatResponse> ChatAsync(
    ChatRequest request,
    CancellationToken ct)
{
    var vllmRequest = BuildVllmRequest(request);
    
    // ENRICH WITH STRUCTURED OUTPUT
    if (request.Tools?.Length > 0)
    {
        vllmRequest = await _structuredOutputHandler.EnrichRequestAsync(
            vllmRequest,
            request.Tools,
            ct);
    }
    
    var response = await _httpClient.PostAsync<VllmResponse>(
        "/v1/chat/completions",
        vllmRequest,
        ct);
    
    return MapResponse(response);
}
```

3. Handle fallback validation if needed

**Tests**:
- Update VllmProviderTests to verify enrichment called
- Add integration test

**Success Criteria**:
- [ ] StructuredOutputHandler injected
- [ ] EnrichRequestAsync called before requests
- [ ] Tests updated
- [ ] Integration verified

---

### Gap 8.2: Register StructuredOutputHandler in DI

**Status**: [ ]

**File**: src/Acode.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs

**Add Registration**:
```csharp
// Structured Output components
services.AddSingleton<SchemaTransformer>();
services.AddSingleton<SchemaValidator>();
services.AddSingleton<SchemaCache>();
services.AddSingleton<CapabilityDetector>();
services.AddSingleton<CapabilityCache>();
services.AddSingleton<ResponseFormatBuilder>();
services.AddSingleton<GuidedDecodingBuilder>();
services.AddSingleton<OutputValidator>();
services.AddSingleton<FallbackHandler>();
services.AddSingleton<StructuredOutputHandler>();
```

**Success Criteria**:
- [ ] All components registered
- [ ] DI resolves StructuredOutputHandler successfully

---

## PHASE 9: INTEGRATION TESTS

**Goal**: End-to-end verification with real vLLM (conditional).

### Gap 9.1: Create StructuredOutputIntegrationTests

**Status**: [ ]

**File**: tests/Acode.Integration.Tests/Vllm/StructuredOutputIntegrationTests.cs

**Tests** (~10 tests, conditional on vLLM availability):
- Should_UseGuidedDecoding_WithRealVllm
- Should_EnforceSchema_ToolCallArguments
- Should_FallbackToRetry_WhenUnsupported
- Should_DetectCapabilities_RealModel
- Should_ValidateOutput_InFallbackMode
- etc.

**Success Criteria**:
- [ ] Integration test file created
- [ ] ~10 tests defined (may skip if vLLM unavailable)
- [ ] Tests verify end-to-end behavior

---

## PHASE 10: FINAL VERIFICATION AND AUDIT

**Goal**: 100% AC compliance and audit.

### Gap 10.1: Run All Tests

**Status**: [ ]

**Commands**:
```bash
dotnet test --filter "FullyQualifiedName~StructuredOutput" --verbosity normal

# Expected: ~80-100 tests passing
```

**Success Criteria**:
- [ ] All tests passing (0 failures)
- [ ] Test count >= 80
- [ ] No skipped tests (except conditional integration tests)

---

### Gap 10.2: Verify All Files Exist Per Spec

**Status**: [ ]

**Expected Files** (22 production + 11 test = 33 total):

**Production**:
- [ ] StructuredOutputConfiguration.cs
- [ ] FallbackConfiguration.cs
- [ ] SchemaConfiguration.cs
- [ ] ModelStructuredOutputConfig.cs
- [ ] ConfigurationValidationResult.cs
- [ ] SchemaTransformer.cs
- [ ] SchemaValidator.cs
- [ ] SchemaValidationResult.cs
- [ ] SchemaCache.cs
- [ ] ResponseFormatBuilder.cs
- [ ] GuidedDecodingBuilder.cs
- [ ] CapabilityDetector.cs
- [ ] CapabilityCache.cs
- [ ] ModelCapabilities.cs
- [ ] FallbackHandler.cs
- [ ] FallbackContext.cs
- [ ] OutputValidator.cs
- [ ] StructuredOutputHandler.cs
- [ ] StructuredOutputException.cs
- [ ] SchemaTooComplexException.cs
- [ ] ValidationFailedException.cs
- [ ] (other exception types as needed)

**Tests**:
- [ ] StructuredOutputConfigurationTests.cs
- [ ] SchemaTransformerTests.cs
- [ ] SchemaValidatorTests.cs
- [ ] SchemaCacheTests.cs
- [ ] ResponseFormatBuilderTests.cs
- [ ] GuidedDecodingBuilderTests.cs
- [ ] CapabilityDetectorTests.cs
- [ ] OutputValidatorTests.cs
- [ ] FallbackHandlerTests.cs
- [ ] StructuredOutputHandlerTests.cs
- [ ] StructuredOutputIntegrationTests.cs

---

### Gap 10.3: Verify All Acceptance Criteria

**Status**: [ ]

**All 73 ACs** from spec lines 1010-1121:

**Configuration** (7 ACs):
- [ ] AC-001: Global enable/disable works
- [ ] AC-002: Per-model enable/disable works
- [ ] AC-003: Fallback behavior configurable
- [ ] AC-004: Strict/lenient mode works
- [ ] AC-005: Config from .agent/config.yml
- [ ] AC-006: Environment override works
- [ ] AC-007: Config validated on startup

**Response Format** (6 ACs):
- [ ] AC-008 through AC-013

**Tool Schema Integration** (5 ACs):
- [ ] AC-014 through AC-018

**Schema Transformation** (10 ACs):
- [ ] AC-019 through AC-028

**Guided Decoding** (6 ACs):
- [ ] AC-029 through AC-034

**Capability Detection** (5 ACs):
- [ ] AC-035 through AC-039

**Fallback** (7 ACs):
- [ ] AC-040 through AC-046

**Output Validation** (7 ACs):
- [ ] AC-047 through AC-053

**Tool Call Arguments** (5 ACs):
- [ ] AC-054 through AC-058

**Error Handling** (7 ACs):
- [ ] AC-059 through AC-065

**Performance** (4 ACs):
- [ ] AC-066 through AC-069

**Security** (4 ACs):
- [ ] AC-070 through AC-073

---

### Gap 10.4: Build with Zero Errors/Warnings

**Status**: [ ]

**Command**:
```bash
dotnet clean
dotnet build --configuration Release
```

**Success Criteria**:
- [ ] Build: succeeded
- [ ] 0 Error(s)
- [ ] 0 Warning(s)

---

### Gap 10.5: Create Audit Report

**Status**: [ ]

**File**: docs/audits/task-007e-audit-report.md

**Template**: (similar to 006a audit report)

**Success Criteria**:
- [ ] Audit report created
- [ ] All 73 ACs documented as complete
- [ ] All verification checks passed

---

### Gap 10.6: Create PR

**Status**: [ ]

**Commands**:
```bash
git add .
git commit -m "feat(task-007e): implement structured output enforcement

- Complete StructuredOutputHandler orchestrator
- Schema transformation with validation
- Capability detection and caching
- Fallback handler with retry logic
- Response format builders
- Integration with VllmProvider
- 80+ tests covering all subsystems

All 73 ACs verified complete.

Closes #[issue-number]"

git push origin feature/task-006a-fix-gaps

gh pr create --title "Task 007e: Structured Output Enforcement" --body "..."
```

**Success Criteria**:
- [ ] All work committed
- [ ] PR created
- [ ] PR includes test results and audit report

---

## COMPLETION CRITERIA

**Task is COMPLETE when ALL of the following are true:**

- [ ] All Phase 0-10 gaps fixed
- [ ] All 73 acceptance criteria verified as ✅
- [ ] All ~80-100 tests passing (0 failures)
- [ ] Build succeeds with 0 errors, 0 warnings
- [ ] Audit report created and complete
- [ ] PR created with comprehensive description
- [ ] StructuredOutputHandler integrated with VllmProvider
- [ ] IToolSchemaRegistry dependency verified working

**DO NOT mark task complete until ALL checkboxes above are ✅**

---

## NOTES

- IToolSchemaRegistry dependency from Task 007 ✅ **EXISTS AND VERIFIED**
- Configuration layer ~80% done (4/5 files exist, missing methods)
- Schema layer ~40% done (SchemaTransformer exists, validator/cache missing)
- ALL OTHER SUBSYSTEMS 0% done (ResponseFormat, Capability, Fallback, Orchestrator)
- Test coverage ~3% (3 tests vs 80-100 expected)
- No NotImplementedException found (good foundation)
- This is a complex task with 10 phases and many interdependencies
- Follow TDD strictly - RED before GREEN always
- Commit after EACH phase completion

---

**END OF COMPLETION CHECKLIST**
